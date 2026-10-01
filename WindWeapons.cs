using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using ULTRAKILL.Cheats;
using UnityEngine;

namespace ElementalBattlegroundsMod
{
    internal static class WindForceRules
    {
        internal static bool IsSuperHeavy(EnemyIdentifier enemy)
        {
            if (enemy == null || enemy.stationary)
                return true;
            switch (enemy.enemyType)
            {
                case EnemyType.HideousMass:
                case EnemyType.Leviathan:
                case EnemyType.Minotaur:
                case EnemyType.FleshPrison:
                case EnemyType.FleshPanopticon:
                case EnemyType.MinosPrime:
                case EnemyType.SisyphusPrime:
                case EnemyType.Geryon:
                case EnemyType.Providence:
                case EnemyType.Idol:
                case EnemyType.Deathcatcher:
                    return true;
            }
            return false;
        }

        internal static float PushSpeed(EnemyIdentifier enemy, float normal)
        {
            if (enemy == null || enemy.bigEnemy || IsSuperHeavy(enemy))
                return 0f;
            return normal;
        }

        internal static void Push(Rigidbody body, Vector3 direction, float speed)
        {
            if (body == null || speed <= 0f || direction.sqrMagnitude < 0.0001f)
                return;
            direction.Normalize();
            body.WakeUp();
            float current = Vector3.Dot(body.velocity, direction);
            if (current < speed)
                body.velocity += direction * (speed - current);
        }
    }

    internal sealed class PressureShockwaveMarker : MonoBehaviour
    {
    }

    [HarmonyPatch(typeof(Explosion), "Collide")]
    internal static class PressureShockwaveCollisionPatch
    {
        private static bool Prefix(Explosion __instance, Collider other)
        {
            if (__instance == null || __instance.GetComponent<PressureShockwaveMarker>() == null || other == null)
                return true;

            // Pressure supplies its own enemy damage/static launch and must never knock the player.
            // Let the real Knuckleblaster Explosion process everything else so projectiles,
            // thrown swords, breakables and other deflectable rigidbodies keep native behavior.
            if (other.gameObject.CompareTag("Player") || other.gameObject.layer == 10 || other.gameObject.layer == 11)
                return false;
            return true;
        }
    }

    internal sealed class PressureRevolverController : ActiveWeaponController
    {
        private Revolver revolver;
        private Animator animator;
        private static GameObject cachedKnuckleblastWave;

        protected override void Awake()
        {
            base.Awake();
            revolver = GetComponent<Revolver>();
            animator = GetComponentInChildren<Animator>();
        }

        internal bool TryFireCustomAlt()
        {
            // Pressure is intentionally usable immediately after swapping. Unlike a charged
            // Revolver alternate, nothing about the mechanic depends on the draw animation.
            if (revolver == null || !CanUseSecondary() || !SecondaryInputTiming.IsPressed || CooldownRemaining > 0f)
                return false;

            animator?.SetTrigger("ChargeShoot");
            MonoSingleton<PlayerAnimations>.Instance?.Shoot();
            FirePressureBurst();
            StartCooldown(WeaponTuning.PressureCooldown);
            return true;
        }

        private void FirePressureBurst()
        {
            CameraController cc = MonoSingleton<CameraController>.Instance;
            if (cc == null)
                return;

            Vector3 origin = cc.GetDefaultPos();
            Vector3 forward = cc.transform.forward.normalized;
            Vector3 wavePosition = origin + forward * 2f;
            if (Physics.Raycast(origin, forward, out RaycastHit wallHit, 2f,
                LayerMaskDefaults.Get(LMD.EnvironmentAndBigEnemies), QueryTriggerInteraction.Ignore))
                wavePosition = wallHit.point - forward * 0.1f;

            // Use the actual Knuckleblaster shockwave prefab for its authored visual, sound and
            // native non-enemy interactions. A tiny marker patch suppresses only its enemy/player
            // collision path; EB applies enemy damage/launch below so knockback is fixed forward+up.
            GameObject nativeWave = FindKnuckleblasterBlastWave();
            if (nativeWave != null)
            {
                GameObject wave = UnityEngine.Object.Instantiate(nativeWave, wavePosition, cc.transform.rotation);
                foreach (Explosion explosion in wave.GetComponentsInChildren<Explosion>(true))
                {
                    if (explosion == null)
                        continue;
                    explosion.sourceWeapon = gameObject;
                    explosion.hitterWeapon = "ebpressure";
                    if (explosion.GetComponent<PressureShockwaveMarker>() == null)
                        explosion.gameObject.AddComponent<PressureShockwaveMarker>();
                }
            }

            NewMovement movement = MonoSingleton<NewMovement>.Instance;
            Vector3 up = movement != null && movement.rb != null
                ? -movement.rb.GetGravityDirection().normalized
                : Vector3.up;
            Vector3 staticForward = Vector3.ProjectOnPlane(forward, up);
            if (staticForward.sqrMagnitude < 0.001f && movement != null)
                staticForward = Vector3.ProjectOnPlane(movement.transform.forward, up);
            if (staticForward.sqrMagnitude < 0.001f)
                staticForward = Vector3.forward;
            staticForward.Normalize();

            int mask = LayerMaskDefaults.Get(LMD.Enemies);
            RaycastHit[] hits = Physics.SphereCastAll(origin, WeaponTuning.PressureRadius, forward,
                WeaponTuning.PressureRange, mask, QueryTriggerInteraction.Collide);
            HashSet<EnemyIdentifier> enemies = new HashSet<EnemyIdentifier>();

            using (RuntimeRegistry.BeginAttack())
            {
                foreach (RaycastHit hit in hits)
                {
                    EnemyIdentifierIdentifier eidid = hit.collider.GetComponentInParent<EnemyIdentifierIdentifier>();
                    EnemyIdentifier enemy = eidid != null ? eidid.eid : hit.collider.GetComponentInParent<EnemyIdentifier>();
                    if (enemy != null && !enemy.dead && enemies.Add(enemy))
                    {
                        enemy.hitter = "ebpressure";
                        Vector3 launchDirection = (staticForward + up * 0.75f).normalized;
                        // Supply the same fixed direction to ULTRAKILL's damage reaction too; some
                        // enemy controllers need a real hit force before their rigidbody can move.
                        enemy.DeliverDamage(enemy.gameObject, launchDirection * 1100f, enemy.bodyTransform.position,
                            WeaponTuning.PressureDamage, false, 0f, gameObject);

                        Rigidbody body = RuntimeVisuals.EnemyBody(enemy);
                        float speed = WindForceRules.PushSpeed(enemy, WeaponTuning.PressureForce);
                        if (body != null && speed > 0f && !body.isKinematic)
                        {
                            body.WakeUp();
                            // Fixed launch vector: enemy position and camera pitch do not change
                            // the result. Looking down still sends enemies forward and upward.
                            body.velocity = staticForward * speed + up * (speed * 0.75f);
                        }
                    }

                }
            }
        }

        private static GameObject FindKnuckleblasterBlastWave()
        {
            if (cachedKnuckleblastWave != null)
                return cachedKnuckleblastWave;

            foreach (Punch punch in Resources.FindObjectsOfTypeAll<Punch>())
            {
                if (punch != null && punch.type == FistType.Heavy && punch.blastWave != null)
                {
                    cachedKnuckleblastWave = punch.blastWave;
                    break;
                }
            }
            return cachedKnuckleblastWave;
        }
    }

    internal sealed class UpdraftShotgunController : ActiveWeaponController
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

            NewMovement movement = MonoSingleton<NewMovement>.Instance;
            if (movement == null || movement.rb == null)
                return;
            if (shotgun == null || ShotgunGunReady(shotgun))
                animator?.Play("FireNoReload", -1, 0f);
            MonoSingleton<PlayerAnimations>.Instance?.Shoot();
            RuntimeAudio.PlayBoostPadLaunch(movement);

            Vector3 up = -movement.rb.GetGravityDirection().normalized;
            Vector3 velocity = movement.rb.velocity;
            Vector3 planar = Vector3.ProjectOnPlane(velocity, up);
            float currentUp = Vector3.Dot(velocity, up);
            // Match Water Dash/Geyser: Source's grounded movement can erase a velocity-only
            // launch on the same physics tick. Explicitly enter the airborne state first,
            // then apply the intended velocity and lift clear of the floor by a tiny amount.
            movement.Launch(up, 0.01f, true);
            movement.rb.velocity = planar + up * Mathf.Max(currentUp, WeaponTuning.UpdraftPlayerSpeed);
            movement.transform.position += up * 0.08f;

            Vector3 center = movement.transform.position + up * 1.5f;
            using (RuntimeRegistry.BeginAttack())
            {
                foreach (EnemyIdentifier enemy in RuntimeVisuals.EnemiesInSphere(center, WeaponTuning.UpdraftRadius))
                {
                    enemy.hitter = "ebupdraft";
                    Vector3 outward = enemy.bodyTransform.position - movement.transform.position;
                    Vector3 planarOut = Vector3.ProjectOnPlane(outward, up).normalized;
                    // Updraft is primarily a launch, not a lateral shove. Give the vertical
                    // component enough authority to visibly throw normal enemies into the air.
                    outward = planarOut.sqrMagnitude < 0.01f
                        ? up
                        : planarOut * 0.35f + up * 1.6f;
                    enemy.DeliverDamage(enemy.gameObject, outward.normalized * 1400f, enemy.bodyTransform.position,
                        WeaponTuning.UpdraftDamage, false, 0f, gameObject);
                    float speed = WindForceRules.PushSpeed(enemy, WeaponTuning.UpdraftEnemySpeed);
                    WindForceRules.Push(RuntimeVisuals.EnemyBody(enemy), outward, speed);
                }
            }

            GameObject visual = RuntimeVisuals.Primitive(PrimitiveType.Sphere, "Updraft Burst", center,
                Vector3.one * WeaponTuning.UpdraftRadius * 1.5f, new Color(0.82f, 0.92f, 0.52f, 0.18f));
            if (visual != null) Destroy(visual, 0.18f);
            StartCooldown(WeaponTuning.UpdraftCooldown);
        }
    }

    internal sealed class TempestSawbladeController : ActiveWeaponController
    {
        private Nailgun nailgun;
        private bool initialDrawFinished;
        private bool firing;
        internal bool BurstActive => firing;
        private static readonly AccessTools.FieldRef<Nailgun, bool> NailgunCanShoot =
            AccessTools.FieldRefAccess<Nailgun, bool>("canShoot");
        private static readonly AccessTools.FieldRef<Nailgun, string[]> ProjectileVariationTypes =
            AccessTools.FieldRefAccess<Nailgun, string[]>("projectileVariationTypes");

        protected override void Awake()
        {
            base.Awake();
            nailgun = GetComponent<Nailgun>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            initialDrawFinished = false;
            firing = false;
        }

        protected override void Update()
        {
            base.Update();
            if (nailgun != null && !initialDrawFinished && NailgunCanShoot(nailgun))
                initialDrawFinished = true;
            if (firing || !CanUseSecondary() || !SecondaryInputTiming.AllowsInstantFreshPress(initialDrawFinished))
                return;
            StartCoroutine(FirePattern());
        }

        private IEnumerator FirePattern()
        {
            firing = true;
            StartCooldown(WeaponTuning.TempestCooldown);
            PlayBurstFeedback();
            SpawnSaw(0f);
            yield return new WaitForSeconds(WeaponTuning.TempestWaveInterval);
            PlayBurstFeedback();
            SpawnSaw(-WeaponTuning.TempestSpreadAngle);
            SpawnSaw(WeaponTuning.TempestSpreadAngle);
            yield return new WaitForSeconds(WeaponTuning.TempestWaveInterval);
            PlayBurstFeedback();
            SpawnSaw(-WeaponTuning.TempestSpreadAngle * 1.5f);
            SpawnSaw(0f);
            SpawnSaw(WeaponTuning.TempestSpreadAngle * 1.5f);
            firing = false;
        }

        private void PlayBurstFeedback()
        {
            // Each Tempest wave is an authored Alt-Fire state and can assign shared Rapid heat
            // independently from ordinary primary saws.
            ElementalRapidSharedHeatUpdatePatch.SetSharedHeat(nailgun, WeaponTuning.WindRapidHeatOnAltFire);
            Animator animator = GetComponentInChildren<Animator>();
            animator?.SetTrigger("SuperShoot");
            MonoSingleton<PlayerAnimations>.Instance?.Shoot();
            MonoSingleton<RumbleManager>.Instance?.SetVibration(RumbleProperties.SuperSaw);
            CameraController cc = MonoSingleton<CameraController>.Instance;
            cc?.CameraShake(0.35f);
            if (nailgun != null && nailgun.muzzleFlash2 != null && nailgun.shootPoints != null && nailgun.shootPoints.Length > 0)
                UnityEngine.Object.Instantiate(nailgun.muzzleFlash2, nailgun.shootPoints[0].transform);
        }

        private void SpawnSaw(float yaw)
        {
            if (nailgun == null || nailgun.nail == null)
                return;
            CameraController cc = MonoSingleton<CameraController>.Instance;
            if (cc == null)
                return;
            Vector3 direction = Quaternion.AngleAxis(yaw, cc.transform.up) * cc.transform.forward;
            GameObject projectile = UnityEngine.Object.Instantiate(nailgun.nail,
                cc.transform.position + direction.normalized, Quaternion.LookRotation(direction, cc.transform.up));
            projectile.transform.forward = direction.normalized;
            Rigidbody body = projectile.GetComponent<Rigidbody>();
            if (body != null) body.velocity = direction.normalized * 200f;
            Nail nail = projectile.GetComponent<Nail>();
            if (nail != null)
            {
                nail.sourceWeapon = gameObject;
                string[] types = ProjectileVariationTypes(nailgun);
                if (types != null && nailgun.variation >= 0 && nailgun.variation < types.Length)
                    nail.weaponType = types[nailgun.variation];
                nail.ForceCheckSawbladeRicochet();
            }
        }
    }

    internal sealed class SlipstreamLauncherController : ActiveWeaponController
    {
        private RocketLauncher launcher;
        private bool flying;
        private GameObject flightVisual;
        private static readonly AccessTools.FieldRef<RocketLauncher, TimeSince> SinceEquipped =
            AccessTools.FieldRefAccess<RocketLauncher, TimeSince>("sinceEquipped");
        private const float NativeDrawDelay = 0.25f;

        protected override void Awake()
        {
            base.Awake();
            launcher = GetComponent<RocketLauncher>();
        }

        protected override void Update()
        {
            base.Update();
            // Grenade Launcher owns the imported viewmodel and dial; EB owns this secondary's
            // cooldown. Feed its progress back through the 2.0.2 integration API every frame.
            GrenadeLauncherCompat.SyncExternalSecondaryCooldown(launcher, CooldownReadyFraction);
            bool drawFinished = launcher != null && (float)SinceEquipped(launcher) >= NativeDrawDelay;
            if (flying || !CanUseSecondary() || !SecondaryInputTiming.AllowsInstantFreshPress(drawFinished))
                return;
            StartCoroutine(Fly());
        }

        private IEnumerator Fly()
        {
            NewMovement movement = MonoSingleton<NewMovement>.Instance;
            CameraController cc = MonoSingleton<CameraController>.Instance;
            if (movement == null || movement.rb == null || cc == null)
                yield break;

            flying = true;
            StartCooldown(WeaponTuning.SlipstreamCooldown);
            GrenadeLauncherCompat.PlayExternalSecondaryAnimation(launcher);
            MonoSingleton<PlayerAnimations>.Instance?.Shoot();

            // Slipstream consumes the same escalating mobility budget as rocket riding.
            // Use the count that existed before this activation to scale the available
            // flight time, then consume one ride. At five already-used rides the ability
            // still produces one rendered frame instead of silently doing nothing.
            int existingRocketRides = Mathf.Max(0, movement.rocketRides);
            movement.rocketRides = existingRocketRides + 1;
            bool unlimitedRide = NoWeaponCooldown.NoCooldown ||
                (MonoSingleton<WeaponCharges>.Instance != null && MonoSingleton<WeaponCharges>.Instance.infiniteRocketRide) ||
                (MonoSingleton<UnderwaterController>.Instance != null && MonoSingleton<UnderwaterController>.Instance.inWater);
            float rideFraction = unlimitedRide ? 1f : Mathf.Clamp01((5f - existingRocketRides) / 5f);
            float effectiveDuration = Mathf.Max(Time.deltaTime, WeaponTuning.SlipstreamDuration * rideFraction);

            // Same grounded-state protection used by Water movement tools. Without this,
            // the first grounded physics tick can flatten/cancel Slipstream before flight begins.
            Vector3 gravityUp = -movement.rb.GetGravityDirection().normalized;
            Vector3 initialDirection = cc.transform.forward.normalized;
            movement.Launch(initialDirection, 0.01f, true);
            movement.transform.position += gravityUp * 0.08f;
            movement.windState = Mathf.Max(movement.windState, 0.18f);

            float end = Time.time + effectiveDuration;
            flightVisual = RuntimeVisuals.Primitive(PrimitiveType.Sphere, "Slipstream Wind", movement.transform.position,
                new Vector3(3f, 4f, 3f), new Color(0.82f, 0.92f, 0.52f, 0.16f));
            while (Time.time < end && movement != null && movement.rb != null && cc != null)
            {
                Vector3 direction = cc.transform.forward.normalized;
                movement.rb.velocity = direction * WeaponTuning.SlipstreamSpeed;
                if (flightVisual != null)
                {
                    flightVisual.transform.position = movement.transform.position;
                    flightVisual.transform.Rotate(Vector3.up, 360f * Time.deltaTime, Space.World);
                }
                yield return null;
            }
            if (flightVisual != null) Destroy(flightVisual);
            flightVisual = null;
            flying = false;
        }

        private void OnDisable()
        {
            flying = false;
            if (flightVisual != null)
                Destroy(flightVisual);
            flightVisual = null;
        }
    }

    internal sealed class GrandCycloneRuntime : MonoBehaviour
    {
        private sealed class PullState
        {
            internal Rigidbody body;
            internal bool previousGravity;
            internal GroundCheckEnemy groundCheck;
        }

        private static readonly System.Reflection.FieldInfo EnemyBodyField = AccessTools.Field(typeof(EnemyIdentifier), "rb");

        public GameObject sourceWeapon;
        private float age;
        private float tick;
        private int attackId;
        private GameObject visual;
        private readonly Dictionary<EnemyIdentifier, PullState> pulled = new Dictionary<EnemyIdentifier, PullState>();
        private readonly List<EnemyIdentifier> restoreScratch = new List<EnemyIdentifier>();

        private void Start()
        {
            attackId = RuntimeRegistry.NewAttackId();
            visual = RuntimeVisuals.Primitive(PrimitiveType.Cylinder, "Grand Cyclone", transform.position + Vector3.up * 7f,
                new Vector3(WeaponTuning.GrandCycloneRadius * 0.8f, 14f, WeaponTuning.GrandCycloneRadius * 0.8f),
                new Color(0.82f, 0.92f, 0.52f, 0.25f));
        }

        private void FixedUpdate()
        {
            age += Time.fixedDeltaTime;
            if (visual != null)
            {
                visual.transform.position = transform.position + Vector3.up * 7f;
                visual.transform.Rotate(Vector3.up, 280f * Time.fixedDeltaTime, Space.World);
            }

            // Grounded enemy controllers can immediately cancel an upward velocity pull. Force
            // affected targets into their airborne state while they are inside the cyclone, then
            // move their actual EnemyIdentifier rigidbody toward the visible mid-air center. This
            // is the same approach that made the Grenade Launcher pink pull work vertically from
            // rest instead of requiring the enemy to be hit first.
            Vector3 cycloneCenter = transform.position + Vector3.up * 7f;
            HashSet<EnemyIdentifier> active = RuntimeVisuals.EnemiesInSphere(cycloneCenter, WeaponTuning.GrandCycloneRadius);
            foreach (EnemyIdentifier enemy in active)
            {
                if (enemy == null || enemy.dead || WindForceRules.IsSuperHeavy(enemy))
                    continue;

                Rigidbody body = GetEnemyBody(enemy);
                if (body == null)
                    continue;

                EnsurePullState(enemy, body);
                Vector3 toward = cycloneCenter - body.position;
                float distance = toward.magnitude;
                if (distance < 0.5f)
                {
                    body.velocity = Vector3.zero;
                    continue;
                }

                float desired = WeaponTuning.GrandCyclonePullSpeed *
                    Mathf.Clamp(distance / WeaponTuning.GrandCycloneRadius, 0.4f, 1f);
                if (enemy.bigEnemy)
                    desired *= 0.45f;

                body.velocity = Vector3.zero;
                body.MovePosition(Vector3.MoveTowards(body.position, cycloneCenter, desired * Time.fixedDeltaTime));
            }
            RestoreTargetsOutside(active);

            tick -= Time.fixedDeltaTime;
            if (tick <= 0f)
            {
                tick = WeaponTuning.GrandCycloneTick;
                using (RuntimeRegistry.BeginAttack(attackId))
                {
                    foreach (EnemyIdentifier enemy in RuntimeVisuals.EnemiesInSphere(cycloneCenter, WeaponTuning.GrandCycloneRadius * 0.75f))
                    {
                        enemy.hitter = "ebgrandcyclone";
                        enemy.DeliverDamage(enemy.gameObject, Vector3.zero, enemy.bodyTransform.position,
                            WeaponTuning.GrandCycloneDamage, false, 0f, sourceWeapon);
                    }
                }
            }

            if (age >= WeaponTuning.GrandCycloneDuration)
            {
                // Release on the exact expiry tick rather than waiting for Unity's delayed
                // Destroy/OnDestroy pass. OnDisable/OnDestroy remain as safety nets.
                RestoreAllTargets();
                Destroy(gameObject);
            }
        }

        private static Rigidbody GetEnemyBody(EnemyIdentifier enemy)
        {
            if (enemy == null)
                return null;
            return EnemyBodyField?.GetValue(enemy) as Rigidbody ?? RuntimeVisuals.EnemyBody(enemy);
        }

        private void EnsurePullState(EnemyIdentifier enemy, Rigidbody body)
        {
            if (pulled.ContainsKey(enemy))
                return;

            PullState state = new PullState
            {
                body = body,
                previousGravity = body.useGravity,
                groundCheck = enemy.gce
            };
            pulled.Add(enemy, state);
            body.useGravity = false;
            if (state.groundCheck != null)
                state.groundCheck.ForceOff();
        }

        private void RestoreTargetsOutside(HashSet<EnemyIdentifier> active)
        {
            restoreScratch.Clear();
            foreach (KeyValuePair<EnemyIdentifier, PullState> pair in pulled)
            {
                if (pair.Key == null || pair.Key.dead || !active.Contains(pair.Key) || WindForceRules.IsSuperHeavy(pair.Key))
                    restoreScratch.Add(pair.Key);
            }
            foreach (EnemyIdentifier enemy in restoreScratch)
                RestoreTarget(enemy);
        }

        private void RestoreTarget(EnemyIdentifier enemy)
        {
            PullState state;
            if (!pulled.TryGetValue(enemy, out state))
                return;
            pulled.Remove(enemy);

            bool ebReleasedFinalForceOff = state.groundCheck == null;
            if (state.groundCheck != null)
            {
                state.groundCheck.StopForceOff();

                // StopForceOff immediately copies cached touchingGround back into onGround. The
                // cyclone has physically moved this enemy away from those colliders, so clear the
                // stale cache when EB was the final ForceOff owner.
                if (state.groundCheck.forcedOff <= 0)
                {
                    ebReleasedFinalForceOff = true;
                    state.groundCheck.onGround = false;
                    state.groundCheck.touchingGround = false;
                    state.groundCheck.fallSuppressed = false;
                    if (state.groundCheck.cols != null)
                        state.groundCheck.cols.Clear();
                }
            }

            if (state.body == null)
                return;

            // Do not fight another mechanic that still owns ForceOff, and do not turn native
            // flying enemies into falling physics bodies.
            if (!ebReleasedFinalForceOff || enemy == null || enemy.flying)
            {
                state.body.useGravity = state.previousGravity;
                state.body.WakeUp();
                return;
            }

            Vector3 gravityDirection = Physics.gravity.sqrMagnitude > 0.001f
                ? Physics.gravity.normalized
                : Vector3.down;

            // Grounded ULTRAKILL enemies commonly use a kinematic Rigidbody + NavMeshAgent with
            // gravity disabled. Merely restoring GroundCheckEnemy therefore leaves them hanging in
            // mid-air until player knockback invokes the enemy's native KnockBack() transition.
            // Enter that same airborne state directly, with no damage/style event, then enforce the
            // authored downward release speed.
            bool nativeFallStarted = TryStartNativeFall(enemy, state.body, gravityDirection * 70f);
            if (!nativeFallStarted)
            {
                state.body.isKinematic = false;
                state.body.useGravity = true;
            }

            Vector3 velocity = state.body.velocity;
            float fallSpeed = Vector3.Dot(velocity, gravityDirection);
            const float releaseFallSpeed = 6f;
            if (fallSpeed < releaseFallSpeed)
                velocity += gravityDirection * (releaseFallSpeed - fallSpeed);
            state.body.velocity = velocity;
            state.body.WakeUp();
        }

        private static bool TryStartNativeFall(EnemyIdentifier enemy, Rigidbody body, Vector3 releaseForce)
        {
            if (enemy == null || body == null)
                return false;

            switch (enemy.enemyClass)
            {
                case EnemyClass.Husk:
                {
                    Zombie zombie = enemy.GetComponent<Zombie>();
                    if (zombie != null)
                        zombie.KnockBack(releaseForce);
                    break;
                }
                case EnemyClass.Machine:
                {
                    Machine machine = enemy.GetComponent<Machine>();
                    if (machine != null)
                        machine.KnockBack(releaseForce);
                    break;
                }
                case EnemyClass.Demon:
                {
                    Statue statue = enemy.GetComponent<Statue>();
                    if (statue != null)
                        statue.KnockBack(releaseForce);
                    break;
                }
            }

            return !body.isKinematic && body.useGravity;
        }

        private void RestoreAllTargets()
        {
            restoreScratch.Clear();
            restoreScratch.AddRange(pulled.Keys);
            foreach (EnemyIdentifier enemy in restoreScratch)
                RestoreTarget(enemy);
            restoreScratch.Clear();
        }

        private void OnDisable()
        {
            RestoreAllTargets();
        }

        private void OnDestroy()
        {
            RestoreAllTargets();
            if (visual != null)
                Destroy(visual);
        }
    }

}
