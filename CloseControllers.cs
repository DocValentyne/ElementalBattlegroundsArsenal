using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using ULTRAKILL.Cheats;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBattlegroundsMod
{
    internal sealed class CounterShotgunController : ActiveWeaponController
    {
        private Shotgun shotgun;
        private Animator animator;
        private bool initialDrawFinished;
        private float storedSpeed;
        private float speedStorageAge = 1f;
        private float jackhammerSpeed;
        private int jackhammerTier;
        private float tierDownAge = 1f;
        private static readonly AccessTools.FieldRef<Shotgun, bool> ShotgunGunReady =
            AccessTools.FieldRefAccess<Shotgun, bool>("gunReady");

        protected override void Awake()
        {
            base.Awake();
            shotgun = GetComponent<Shotgun>();
            animator = GetComponentInChildren<Animator>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            initialDrawFinished = false;
            storedSpeed = 0f;
            speedStorageAge = 1f;
            jackhammerSpeed = 0f;
            jackhammerTier = 0;
            tierDownAge = 1f;
        }

        protected override void Update()
        {
            base.Update();
            UpdateJackhammerTier();
            if (shotgun == null)
                return;
            if (!initialDrawFinished && ShotgunGunReady(shotgun))
                initialDrawFinished = true;
            if (!CanUseSecondary() || !initialDrawFinished || !SecondaryInputTiming.IsPressed)
                return;

            // FireNoReload is the native chainsaw-launch style animation we want, but forcing it
            // while the shotgun's primary FireWithReload animation is still waiting on ReadyGun
            // destroys that animation event and leaves the shotgun jammed. Counter itself is
            // intentionally usable during primary recovery, so only touch the weapon animator
            // when vanilla already considers the shotgun ready.
            if (ShotgunGunReady(shotgun))
                animator?.Play("FireNoReload", -1, 0f);
            MonoSingleton<PlayerAnimations>.Instance?.Shoot();
            Counter();
            StartCooldown(WeaponTuning.CounterCooldown);
        }

        private void Counter()
        {
            CameraController cc = MonoSingleton<CameraController>.Instance;
            Vector3 origin = cc.GetDefaultPos();
            Vector3 direction = cc.transform.forward;
            int mask = 16384 | LayerMaskDefaults.Get(LMD.Enemies) | LayerMaskDefaults.Get(LMD.Environment);
            RaycastHit[] hits = Physics.SphereCastAll(origin, 0.75f, direction, 4.5f, mask, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                Transform target = hit.transform;
                ParryHelper helper = target.GetComponent<ParryHelper>();
                if (helper != null && helper.target != null)
                    target = helper.target;

                Cannonball cannonball = target.GetComponent<Cannonball>() ?? target.GetComponentInParent<Cannonball>();
                if (cannonball != null && cannonball.launchable)
                {
                    // Match Feedbacker's cannonball redirection instead of treating the
                    // Insurrectionist boulder as a generic enemy. The Sisyphus boulder is the
                    // explicit projectile-shaped exception that counts as a successful Counter.
                    Vector3 parryTarget = Punch.GetParryLookTarget();
                    if (Vector3.Distance(cannonball.transform.position, parryTarget) < 10f)
                    {
                        if (Physics.Raycast(origin, direction, out RaycastHit redirectHit, 5f, LayerMaskDefaults.Get(LMD.EnemiesAndEnvironment)))
                            cannonball.transform.position = redirectHit.point;
                        else
                            cannonball.transform.position = origin + direction * 5f;
                        cannonball.transform.forward = direction;
                    }
                    else
                    {
                        cannonball.transform.LookAt(parryTarget);
                    }

                    EnemyIdentifier insurrectionist = cannonball.sisy != null
                        ? cannonball.sisy.GetComponent<EnemyIdentifier>()
                        : null;
                    if (insurrectionist != null)
                    {
                        AwardVanillaParry(insurrectionist);
                        RuntimeRegistry.GrantCounterBuff(WeaponTuning.CounterBuffDuration);
                    }
                    else
                    {
                        MonoSingleton<TimeController>.Instance?.ParryFlash();
                    }
                    cannonball.Launch();
                    CounterEnemyImpactFeedback(true);
                    ApplyCounterEnemyRecoil(direction);
                    return;
                }

                ParryReceiver receiver = target.GetComponent<ParryReceiver>() ?? target.GetComponentInParent<ParryReceiver>();
                if (receiver != null && receiver.enabled)
                {
                    EnemyIdentifier receiverEnemy = target.GetComponentInParent<EnemyIdentifier>();
                    receiver.Parry();
                    // ParryReceiver is also used by non-melee interruption objects. Only award
                    // Counter's combustion reward when the receiver belongs to an enemy that is
                    // currently in a melee-parry window.
                    if (receiverEnemy != null && IsNativeMeleeParryWindow(receiverEnemy, hit.transform))
                    {
                        TriggerMeleeParry(receiverEnemy);
                        RuntimeRegistry.GrantCounterBuff(WeaponTuning.CounterBuffDuration);
                    }
                    CounterEnemyImpactFeedback(true);
                    ApplyCounterEnemyRecoil(direction);
                    return;
                }

                Grenade grenade = target.GetComponentInParent<Grenade>();
                if (grenade != null && !grenade.enemy)
                {
                    Vector3 targetPoint = origin + direction * 1000f;
                    if (Physics.Raycast(origin, direction, out RaycastHit beamHit, 1000f, LayerMaskDefaults.Get(LMD.EnemiesAndEnvironment)))
                        targetPoint = beamHit.point;
                    grenade.GrenadeBeam(targetPoint, gameObject);
                    // Native Jackhammer marks grenades as hitGrenade and explicitly skips its
                    // player recoil path. Counter should do the same for player-created grenades.
                    CounterEnemyImpactFeedback(true);
                    return;
                }

                Landmine landmine = target.GetComponentInParent<Landmine>();
                if (landmine != null)
                {
                    landmine.transform.LookAt(Punch.GetParryLookTarget());
                    landmine.Parry();
                    // Landmines are player utility projectiles in the native hammer path, not an
                    // enemy body impact, so they do not launch V1 backwards.
                    CounterEnemyImpactFeedback(true);
                    return;
                }

                Chainsaw saw = target.GetComponentInParent<Chainsaw>();
                if (saw != null)
                {
                    saw.GetPunched();
                    saw.transform.position = origin + direction;
                    saw.rb.velocity = (Punch.GetParryLookTarget() - saw.transform.position).normalized * 105f;
                    // Same as vanilla Jackhammer: redirecting your own chainsaw is not an enemy
                    // impact and therefore should not recoil the player.
                    CounterEnemyImpactFeedback(true);
                    return;
                }

                EnemyIdentifierIdentifier eidid = hit.collider.GetComponentInParent<EnemyIdentifierIdentifier>();
                EnemyIdentifier enemy = eidid != null ? eidid.eid : target.GetComponentInParent<EnemyIdentifier>();
                if (enemy != null && !enemy.dead)
                {
                    Gutterman gutterman = enemy.GetComponent<Gutterman>();
                    bool shieldParry = gutterman != null && gutterman.hasShield;
                    bool meleeParry = IsNativeMeleeParryWindow(enemy, hit.transform);

                    if (shieldParry)
                    {
                        gutterman.ShieldBreak(true, false);
                        AwardVanillaParry(enemy);
                        RuntimeRegistry.GrantCounterBuff(WeaponTuning.CounterBuffDuration);
                    }
                    else if (meleeParry)
                    {
                        TriggerMeleeParry(enemy);
                        RuntimeRegistry.GrantCounterBuff(WeaponTuning.CounterBuffDuration);
                    }

                    // Idols and active Deathcatchers use the same melee-only kill gate as
                    // vanilla Impact Hammer. Keep EB's custom hitter for ordinary enemies, but
                    // identify these special breakables as a native hammer impact so the game's
                    // own Idol/Deathcatcher logic decides whether the hit is effective. This is
                    // not an attack-parry window and therefore does not grant Combustion/Fortify.
                    bool hammerOnlyBreakable = enemy.enemyType == EnemyType.Idol ||
                                               enemy.enemyType == EnemyType.Deathcatcher;
                    enemy.hitter = hammerOnlyBreakable ? "hammer" : "ebcounter";
                    if (!enemy.hitterWeapons.Contains("ebcounter"))
                        enemy.hitterWeapons.Add("ebcounter");
                    using (RuntimeRegistry.BeginAttack())
                        enemy.DeliverDamage(hit.collider.gameObject, direction * 50000f, hit.point, WeaponTuning.CounterDamage, false, 0f, gameObject);
                    CounterEnemyImpactFeedback(true);
                    ApplyCounterEnemyRecoil(direction);
                    return;
                }

                if (LayerMaskDefaults.IsMatchingLayer(hit.collider.gameObject.layer, LMD.Environment))
                {
                    CounterTerrainImpactFeedback(hit, direction);
                    ApplyCounterTerrainRecoil(direction);
                    return;
                }
            }

            // Native Jackhammer still plays its weak impact feedback on a complete whiff.
            SpawnCounterImpactEffect(origin + direction * 2.5f);
        }

        private static readonly System.Reflection.FieldInfo HammerHitSoundField = AccessTools.Field(typeof(ShotgunHammer), "hitSound");
        private static readonly System.Reflection.FieldInfo HammerHitImpactParticleField = AccessTools.Field(typeof(ShotgunHammer), "hitImpactParticle");

        private void FixedUpdate()
        {
            PlayerTracker tracker = MonoSingleton<PlayerTracker>.Instance;
            if (tracker == null)
                return;

            speedStorageAge += Time.fixedDeltaTime;
            float magnitude = tracker.GetPlayerVelocity().magnitude;
            if (magnitude >= storedSpeed - 5f || speedStorageAge > 0.5f)
            {
                storedSpeed = magnitude;
                speedStorageAge = 0f;
            }
        }

        private void UpdateJackhammerTier()
        {
            PlayerTracker tracker = MonoSingleton<PlayerTracker>.Instance;
            if (tracker == null)
                return;

            float targetSpeed = Mathf.Min(tracker.GetPlayerVelocity().magnitude / 60f, 1f);
            jackhammerSpeed = Mathf.MoveTowards(jackhammerSpeed, targetSpeed, Time.deltaTime * 2f);
            HookArm hook = MonoSingleton<HookArm>.Instance;
            if (hook != null && hook.beingPulled)
                jackhammerSpeed = Mathf.Min(jackhammerSpeed, 0.5f);

            int proposed = jackhammerSpeed > 0.66f ? 2 : (jackhammerSpeed > 0.33f ? 1 : 0);
            if (hook != null && hook.beingPulled && jackhammerTier == 2)
                proposed = Mathf.Min(proposed, 1);

            if (proposed < jackhammerTier)
            {
                tierDownAge += Time.deltaTime;
                if (tierDownAge <= 0.5f)
                    proposed = jackhammerTier;
            }
            else
            {
                tierDownAge = 0f;
            }
            jackhammerTier = proposed;
        }

        private static void CounterEnemyImpactFeedback(bool playHammerSound)
        {
            if (WeaponTuning.CounterHitstop > 0f && MonoSingleton<TimeController>.Instance != null)
                MonoSingleton<TimeController>.Instance.HitStop(WeaponTuning.CounterHitstop);
            if (MonoSingleton<CameraController>.Instance != null)
                MonoSingleton<CameraController>.Instance.CameraShake(0.55f);
            if (!playHammerSound)
                return;
            ShotgunHammer[] hammers = Resources.FindObjectsOfTypeAll<ShotgunHammer>();
            foreach (ShotgunHammer hammer in hammers)
            {
                AudioSource sound = HammerHitSoundField?.GetValue(hammer) as AudioSource;
                if (sound != null)
                {
                    UnityEngine.Object.Instantiate(sound, MonoSingleton<CameraController>.Instance.GetDefaultPos(), Quaternion.identity);
                    break;
                }
            }
        }

        private void CounterTerrainImpactFeedback(RaycastHit hit, Vector3 direction)
        {
            CameraController cc = MonoSingleton<CameraController>.Instance;
            if (cc != null)
                cc.CameraShake(0.35f);
            if (hit.collider != null && SceneHelper.IsStaticEnvironment(hit))
                MonoSingleton<SceneHelper>.Instance?.CreateEnviroGibs(hit.point - direction, direction, 8f, 10, 2f);

            SpawnCounterImpactEffect(hit.point - direction.normalized);
        }

        private void SpawnCounterImpactEffect(Vector3 position)
        {
            // Reuse the Jackhammer's own weak/medium/red impact prefab. It carries the small
            // contact flash and surface impact audio that our hand-built branch lacked.
            CameraController cc = MonoSingleton<CameraController>.Instance;
            ShotgunHammer[] hammers = Resources.FindObjectsOfTypeAll<ShotgunHammer>();
            foreach (ShotgunHammer hammer in hammers)
            {
                GameObject[] impacts = HammerHitImpactParticleField?.GetValue(hammer) as GameObject[];
                if (impacts == null || impacts.Length == 0)
                    continue;
                // Native Jackhammer forces the weak impact prefab for terrain and complete whiffs
                // regardless of movement tier.
                GameObject prefab = impacts[0];
                if (prefab != null)
                    UnityEngine.Object.Instantiate(prefab, position, cc != null ? cc.transform.rotation : Quaternion.identity);
                break;
            }
        }

        private void ApplyCounterEnemyRecoil(Vector3 direction)
        {
            NewMovement movement = MonoSingleton<NewMovement>.Instance;
            if (movement == null) return;

            movement.Launch(-direction * (300f * jackhammerTier + 100f));
            if (storedSpeed > 0f && movement.rb.velocity.magnitude < storedSpeed)
                movement.rb.velocity = -direction.normalized * storedSpeed;
        }

        private void ApplyCounterTerrainRecoil(Vector3 direction)
        {
            NewMovement movement = MonoSingleton<NewMovement>.Instance;
            if (movement == null) return;

            float divisor = ((float)movement.hammerJumps + 3f) / 3f;
            movement.Launch(-direction * ((100f * jackhammerTier + 300f) / divisor));
            movement.hammerJumps++;
            movement.explosionLaunchResistance = 0.5f;
        }

        private static readonly AccessTools.FieldRef<Sisyphus, bool> InsurrectionistSwinging =
            AccessTools.FieldRefAccess<Sisyphus, bool>("swinging");
        private static readonly System.Reflection.FieldInfo EnemyScriptField = AccessTools.Field(typeof(Enemy), "script");

        private static bool IsNativeMeleeParryWindow(EnemyIdentifier eid, Transform hitTransform)
        {
            if (eid == null)
                return false;

            // The Insurrectionist's flail attacks are treated as a deliberate Counter exception.
            // They are interruption-style attacks in vanilla rather than the normal melee-parry path.
            if (eid.enemyType == EnemyType.Sisyphus)
            {
                Sisyphus insurrectionist = eid.GetComponent<Sisyphus>();
                if (insurrectionist != null && InsurrectionistSwinging(insurrectionist))
                    return true;
            }

            Enemy enemy = eid.machine ?? eid.zombie ?? eid.statue;
            if (enemy == null)
                enemy = eid.GetComponent<Enemy>();
            if (enemy == null)
                return false;
            if (enemy.parryable)
                return true;
            if (!enemy.partiallyParryable)
                return false;
            if (enemy.parryables != null && hitTransform != null)
            {
                Transform current = hitTransform;
                while (current != null && current != eid.transform.parent)
                {
                    if (enemy.parryables.Contains(current))
                        return true;
                    current = current.parent;
                }
            }
            return enemy.parryFramesLeft > 0;
        }

        private static void TriggerMeleeParry(EnemyIdentifier eid)
        {
            if (eid == null)
                return;

            Enemy enemy = eid.machine ?? eid.zombie ?? eid.statue;
            if (enemy == null)
                enemy = eid.GetComponent<Enemy>();

            // Keep Counter's fixed 1 damage separate from vanilla's shotgun/hammer parry damage,
            // but still run the enemy script's actual OnParry callback before consuming the window.
            // This is the general "yes, this really was a parry" signal used by enemies such as
            // Swordsmachine, rather than only awarding style on the player side.
            bool swordsmachineHandledByScript = false;
            if (enemy != null)
            {
                EnemyScript script = EnemyScriptField?.GetValue(enemy) as EnemyScript;
                if (script != null)
                {
                    DamageData parryData = new DamageData
                    {
                        hitTarget = enemy.chest != null ? enemy.chest : eid.gameObject,
                        hitter = "hammerzone",
                        damage = 0f,
                        sourceWeapon = null,
                        force = Vector3.zero,
                        fromExplosion = false
                    };
                    script.OnParry(ref parryData, true);
                    swordsmachineHandledByScript = script is SwordsMachine;
                }

                enemy.parryable = false;
                enemy.partiallyParryable = false;
                enemy.parryFramesLeft = 0;
                if (enemy.parryables != null)
                    enemy.parryables.Clear();
                if (enemy.parryChallenge != null)
                    enemy.parryChallenge.Done();
            }

            AwardVanillaParry(eid);
            if (!swordsmachineHandledByScript)
                eid.gameObject.SendMessage("GotParried", SendMessageOptions.DontRequireReceiver);
        }

        private static void AwardVanillaParry(EnemyIdentifier eid)
        {
            NewMovement movement = MonoSingleton<NewMovement>.Instance;
            if (movement != null)
                movement.Parry(eid);
        }
    }

    internal sealed class WaterDashController : ActiveWeaponController
    {
        private ShotgunHammer hammer;
        private bool initialDrawFinished;
        private static readonly AccessTools.FieldRef<ShotgunHammer, bool> HammerGunReady =
            AccessTools.FieldRefAccess<ShotgunHammer, bool>("gunReady");

        protected override void Awake()
        {
            base.Awake();
            hammer = GetComponent<ShotgunHammer>();
            ElementalJackhammerHudRegistry.Register(this, hammer, ElementId.Water);
        }

        private void OnDestroy()
        {
            ElementalJackhammerHudRegistry.Unregister(this);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            initialDrawFinished = false;
        }

        protected override void Update()
        {
            base.Update();
            if (hammer != null && !initialDrawFinished && HammerGunReady(hammer))
                initialDrawFinished = true;
            if (!CanUseSecondary() || !SecondaryInputTiming.AllowsInstantFreshPress(initialDrawFinished))
                return;

            NewMovement movement = MonoSingleton<NewMovement>.Instance;
            RuntimeAudio.PlayWaterDash(movement);
            GetComponent<Animator>()?.Play("Fire", -1, 0f);
            Vector3 direction = MonoSingleton<CameraController>.Instance.transform.forward.normalized;
            GameObject host = new GameObject("EB Water Dash");
            WaterDashRuntime dash = host.AddComponent<WaterDashRuntime>();
            dash.movement = movement;
            dash.direction = direction;
            dash.minimumSpeed = WeaponTuning.WaterDashMinSpeed;
            dash.maximumSpeed = WeaponTuning.WaterDashMaxSpeed;
            StartCooldown(WeaponTuning.WaterDashCooldown);
        }
    }

    internal sealed class CycloneShotgunController : ActiveWeaponController
    {
        private Shotgun shotgun;
        private Animator animator;
        private bool initialDrawFinished;
        private static readonly AccessTools.FieldRef<Shotgun, bool> ShotgunGunReady =
            AccessTools.FieldRefAccess<Shotgun, bool>("gunReady");

        protected override void Awake()
        {
            base.Awake();
            shotgun = GetComponent<Shotgun>();
            animator = GetComponentInChildren<Animator>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            initialDrawFinished = false;
        }

        protected override void Update()
        {
            base.Update();
            if (shotgun != null && !initialDrawFinished && ShotgunGunReady(shotgun))
                initialDrawFinished = true;
            if (!CanUseSecondary() || !SecondaryInputTiming.AllowsInstantFreshPress(initialDrawFinished))
                return;

            CameraController cc = MonoSingleton<CameraController>.Instance;
            // Same protection as Counter Shotgun: the Cyclone may fire during primary recovery,
            // but forcing FireNoReload over FireWithReload destroys vanilla's ReadyGun animation
            // event and jams the primary until the weapon is redrawn.
            if (shotgun == null || ShotgunGunReady(shotgun))
                animator?.Play("FireNoReload", -1, 0f);
            MonoSingleton<PlayerAnimations>.Instance?.Shoot();
            RuntimeAudio.PlaySawedOnLaunch(shotgun);
            GameObject host = new GameObject("EB Cyclone");
            host.transform.position = cc.GetDefaultPos() + cc.transform.forward * 2f - cc.transform.up * 1.1f;
            CycloneRuntime cyclone = host.AddComponent<CycloneRuntime>();
            cyclone.velocity = cc.transform.forward * WeaponTuning.CycloneSpeed;
            cyclone.sourceWeapon = gameObject;
            StartCooldown(WeaponTuning.CycloneCooldown);
        }
    }

}
