using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ElementalBattlegroundsMod
{
    internal sealed class FaultlineRevolverController : ActiveWeaponController
    {
        private Revolver revolver;
        private Animator animator;
        private static readonly AccessTools.FieldRef<Revolver, bool> ShootReady =
            AccessTools.FieldRefAccess<Revolver, bool>("shootReady");
        private static readonly AccessTools.FieldRef<Revolver, bool> GunReady =
            AccessTools.FieldRefAccess<Revolver, bool>("gunReady");
        private static readonly AccessTools.FieldRef<Revolver, float> ShootCharge =
            AccessTools.FieldRefAccess<Revolver, float>("shootCharge");

        protected override void Awake()
        {
            base.Awake();
            revolver = GetComponent<Revolver>();
            animator = GetComponentInChildren<Animator>();
        }

        internal bool TryFireCustomAlt()
        {
            if (revolver == null || !CanUseSecondary() || !SecondaryInputTiming.IsPressed || CooldownRemaining > 0f ||
                !ShootReady(revolver) || !GunReady(revolver))
                return false;

            CameraController cc = MonoSingleton<CameraController>.Instance;
            NewMovement movement = MonoSingleton<NewMovement>.Instance;
            if (cc == null || movement == null || movement.rb == null)
                return false;

            Vector3 up = -movement.rb.GetGravityDirection().normalized;
            Vector3 forward = Vector3.ProjectOnPlane(cc.transform.forward, up);
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.ProjectOnPlane(movement.transform.forward, up);
            if (forward.sqrMagnitude < 0.001f)
                return false;
            forward.Normalize();

            // Faultline is a terrain-bound technique. Do not spend the cooldown if the first
            // intended spike has no ground under it.
            Vector3 firstCandidate = movement.transform.position + forward * WeaponTuning.FaultlineFirstDistance;
            if (!TryFindGround(firstCandidate, up, out Vector3 firstGround, out int groundLayer))
                return false;

            ShootReady(revolver) = false;
            GunReady(revolver) = false;
            ShootCharge(revolver) = 0f;
            animator?.SetTrigger("ChargeShoot");
            MonoSingleton<PlayerAnimations>.Instance?.Shoot(revolver.altVersion ? 0.5f : 1f);
            GameObject host = new GameObject("EB Earth Faultline");
            FaultlineRuntime runtime = host.AddComponent<FaultlineRuntime>();
            runtime.sourceWeapon = gameObject;
            runtime.origin = movement.transform.position;
            runtime.direction = forward;
            runtime.up = up;
            runtime.firstGround = firstGround;
            runtime.firstGroundLayer = groundLayer;
            StartCooldown(WeaponTuning.FaultlineCooldown);
            return true;
        }

        internal static bool TryFindGround(Vector3 candidate, Vector3 up, out Vector3 ground, out int groundLayer)
        {
            // Match Thunderline's ability to resolve terrain from far above it instead of
            // accidentally requiring the player to stay within a few body-heights of the floor.
            Vector3 origin = candidate + up * 1.5f;
            if (Physics.Raycast(origin, -up, out RaycastHit hit, WeaponTuning.FaultlineGroundSearchDistance,
                LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
            {
                ground = hit.point;
                groundLayer = hit.collider != null ? hit.collider.gameObject.layer : 0;
                return true;
            }
            ground = default(Vector3);
            groundLayer = 0;
            return false;
        }
    }

    internal sealed class FaultlineRuntime : MonoBehaviour
    {
        internal GameObject sourceWeapon;
        internal Vector3 origin;
        internal Vector3 direction;
        internal Vector3 up;
        internal Vector3 firstGround;
        internal int firstGroundLayer;

        private readonly List<FaultlineSpike> spikes = new List<FaultlineSpike>();
        private float nextDamageTick;
        private int spawned;
        private int spikeCount;
        private float nextSpawn;
        private float endAt;

        private static Mesh spikeMesh;
        private static Material spikeMaterial;

        private void Start()
        {
            spikeCount = Mathf.Clamp(WeaponTuning.FaultlineSpikeCount, 1, 32);
            SpawnSpike(firstGround, firstGroundLayer, 0);
            spawned = 1;
            nextSpawn = Time.time + WeaponTuning.FaultlineSpikeInterval;
            nextDamageTick = Time.time;
            endAt = Time.time + WeaponTuning.FaultlineLifetime +
                    Mathf.Max(0, spikeCount - 1) * WeaponTuning.FaultlineSpikeInterval;
        }

        private void Update()
        {
            while (spawned < spikeCount && Time.time >= nextSpawn)
            {
                Vector3 candidate = origin + direction * (WeaponTuning.FaultlineFirstDistance + WeaponTuning.FaultlineSpacing * spawned);
                if (!FaultlineRevolverController.TryFindGround(candidate, up, out Vector3 ground, out int layer))
                {
                    spawned = spikeCount;
                    break;
                }
                SpawnSpike(ground, layer, spawned);
                spawned++;
                nextSpawn += WeaponTuning.FaultlineSpikeInterval;
            }

            if (Time.time >= nextDamageTick)
            {
                DamageTick();
                nextDamageTick = Time.time + Mathf.Max(0.03f, WeaponTuning.FaultlineDamageTick);
            }

            for (int i = spikes.Count - 1; i >= 0; i--)
            {
                FaultlineSpike spike = spikes[i];
                if (spike.visual == null || Time.time >= spike.expireAt)
                {
                    if (spike.visual != null) Destroy(spike.visual);
                    spikes.RemoveAt(i);
                }
            }

            if (Time.time >= endAt && spikes.Count == 0)
                Destroy(gameObject);
        }

        private void SpawnSpike(Vector3 ground, int layer, int index)
        {
            float t = spikeCount <= 1 ? 0f : (float)index / (spikeCount - 1);
            float curved = Mathf.Pow(t, 1.7f);
            float height = Mathf.Lerp(WeaponTuning.FaultlineBaseHeight, WeaponTuning.FaultlineMaxHeight, curved);
            float width = WeaponTuning.FaultlineRadius * Mathf.Lerp(1.15f, 1.75f, curved);

            GameObject spike = new GameObject();
            spike.name = "Earth Faultline Spike " + (index + 1);
            spike.transform.position = ground;
            spike.transform.localScale = new Vector3(width, height, width);
            spike.transform.rotation = Quaternion.FromToRotation(Vector3.up, up) * Quaternion.AngleAxis(45f + index * 11f, Vector3.up);
            if (layer >= 0)
                spike.layer = layer;

            Mesh mesh = GetSpikeMesh();
            MeshFilter filter = spike.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = spike.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GetSpikeMaterial();
            MeshCollider collider = spike.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = true;

            spikes.Add(new FaultlineSpike
            {
                visual = spike,
                ground = ground,
                height = height,
                radius = Mathf.Max(0.25f, WeaponTuning.FaultlineRadius),
                expireAt = Time.time + WeaponTuning.FaultlineLifetime
            });
        }

        private static Mesh GetSpikeMesh()
        {
            if (spikeMesh != null)
                return spikeMesh;

            // A deliberately faceted, slightly irregular six-sided rock spire.  The tiny flat
            // cap keeps the accidental-but-fun "stand on the Faultline" behavior possible while
            // reading visually as a point instead of the old rectangular pillar.
            const int sides = 6;
            Vector3[] vertices = new Vector3[sides * 2];
            int[] triangles = new int[sides * 12];
            for (int i = 0; i < sides; i++)
            {
                float angle = Mathf.PI * 2f * i / sides;
                float baseRadius = (i % 2 == 0) ? 0.5f : 0.44f;
                float topRadius = (i % 2 == 0) ? 0.055f : 0.04f;
                vertices[i] = new Vector3(Mathf.Cos(angle) * baseRadius, 0f, Mathf.Sin(angle) * baseRadius);
                vertices[sides + i] = new Vector3(Mathf.Cos(angle) * topRadius, 1f, Mathf.Sin(angle) * topRadius);
            }

            int t = 0;
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                // Side quad.
                triangles[t++] = i;
                triangles[t++] = sides + i;
                triangles[t++] = sides + next;
                triangles[t++] = i;
                triangles[t++] = sides + next;
                triangles[t++] = next;

                // Bottom and tiny top caps. A fan around vertex 0/top-0 is sufficient for this
                // simple convex shape and avoids introducing a center vertex.
                if (i >= 1 && i < sides - 1)
                {
                    triangles[t++] = 0;
                    triangles[t++] = next;
                    triangles[t++] = i;
                    // Reverse the top-cap winding so its normals face upward.  The 0.0.34
                    // fan existed geometrically but was back-face culled when viewed from above,
                    // which looked like a literal hole through the spike.
                    triangles[t++] = sides;
                    triangles[t++] = sides + next;
                    triangles[t++] = sides + i;
                }
            }

            if (t != triangles.Length)
                Array.Resize(ref triangles, t);

            spikeMesh = new Mesh();
            spikeMesh.name = "EB Earth Faultline Rock Spike";
            spikeMesh.hideFlags = HideFlags.HideAndDontSave;
            spikeMesh.vertices = vertices;
            spikeMesh.triangles = triangles;
            spikeMesh.RecalculateNormals();
            spikeMesh.RecalculateBounds();
            return spikeMesh;
        }

        private static Material GetSpikeMaterial()
        {
            if (spikeMaterial != null)
                return spikeMaterial;
            // Use the same shader-selection/tint path as EB's other runtime visuals.  Unity's
            // built-in Standard shader can resolve as an unusable black material in ULTRAKILL's
            // render setup even though Shader.Find returns it successfully.
            spikeMaterial = RuntimeVisuals.CreatePlaceholderMaterial(ElementPalette.Accent(ElementId.Earth), 1f);
            spikeMaterial.name = "EB Earth Faultline Material";
            spikeMaterial.hideFlags = HideFlags.HideAndDontSave;
            return spikeMaterial;
        }

        private void DamageTick()
        {
            HashSet<EnemyIdentifier> hitThisTick = new HashSet<EnemyIdentifier>();
            using (RuntimeRegistry.BeginAttack())
            {
                foreach (FaultlineSpike spike in spikes)
                {
                    Vector3 bottom = spike.ground + up * Mathf.Min(spike.height * 0.2f, 0.75f);
                    Vector3 top = spike.ground + up * Mathf.Max(spike.height * 0.8f, 0.8f);
                    Collider[] colliders = Physics.OverlapCapsule(bottom, top, spike.radius,
                        LayerMaskDefaults.Get(LMD.Enemies), QueryTriggerInteraction.Collide);
                    foreach (Collider collider in colliders)
                    {
                        EnemyIdentifierIdentifier eidid = collider.GetComponentInParent<EnemyIdentifierIdentifier>();
                        EnemyIdentifier enemy = eidid != null ? eidid.eid : collider.GetComponentInParent<EnemyIdentifier>();
                        if (enemy == null || enemy.dead || !hitThisTick.Add(enemy))
                            continue;

                        enemy.hitter = "ebfaultline";
                        enemy.DeliverDamage(collider.gameObject, up * 700f, enemy.bodyTransform.position,
                            WeaponTuning.FaultlineDamage, false, 0f, sourceWeapon);
                        Rigidbody body = RuntimeVisuals.EnemyBody(enemy);
                        if (body != null && !body.isKinematic && !enemy.bigEnemy && !enemy.stationary &&
                            !WindForceRules.IsSuperHeavy(enemy))
                        {
                            float upward = Vector3.Dot(body.velocity, up);
                            if (upward < WeaponTuning.FaultlineLaunchSpeed)
                                body.velocity += up * (WeaponTuning.FaultlineLaunchSpeed - upward);
                        }
                    }
                }
            }
        }

        private struct FaultlineSpike
        {
            internal GameObject visual;
            internal Vector3 ground;
            internal float height;
            internal float radius;
            internal float expireAt;
        }
    }

    internal sealed class StoneguardJackhammerController : ActiveWeaponController
    {
        private ShotgunHammer hammer;
        private Animator animator;
        private bool initialDrawFinished;
        private float storedSpeed;
        private float speedStorageAge = 1f;
        private float jackhammerSpeed;
        private int jackhammerTier;
        private float tierDownAge = 1f;
        private static readonly AccessTools.FieldRef<ShotgunHammer, bool> HammerGunReady =
            AccessTools.FieldRefAccess<ShotgunHammer, bool>("gunReady");
        private static readonly System.Reflection.FieldInfo HammerHitSoundField = AccessTools.Field(typeof(ShotgunHammer), "hitSound");
        private static readonly System.Reflection.FieldInfo HammerHitImpactParticleField = AccessTools.Field(typeof(ShotgunHammer), "hitImpactParticle");
        private static readonly AccessTools.FieldRef<Sisyphus, bool> InsurrectionistSwinging =
            AccessTools.FieldRefAccess<Sisyphus, bool>("swinging");
        private static readonly System.Reflection.FieldInfo EnemyScriptField = AccessTools.Field(typeof(Enemy), "script");

        protected override void Awake()
        {
            base.Awake();
            hammer = GetComponent<ShotgunHammer>();
            animator = GetComponentInChildren<Animator>();
            ElementalJackhammerHudRegistry.Register(this, hammer, ElementId.Earth);
        }

        private void OnDestroy()
        {
            ElementalJackhammerHudRegistry.Unregister(this);
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
            if (hammer == null)
                return;
            if (!initialDrawFinished && HammerGunReady(hammer))
                initialDrawFinished = true;
            if (!CanUseSecondary() || !initialDrawFinished || !SecondaryInputTiming.IsPressed)
                return;

            if (HammerGunReady(hammer))
                animator?.Play("Fire", -1, 0f);
            MonoSingleton<PlayerAnimations>.Instance?.Shoot();
            Counter();
            StartCooldown(WeaponTuning.CounterCooldown);
        }

        private void Counter()
        {
            CameraController cc = MonoSingleton<CameraController>.Instance;
            if (cc == null) return;
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
                        cannonball.transform.LookAt(parryTarget);

                    EnemyIdentifier insurrectionist = cannonball.sisy != null ? cannonball.sisy.GetComponent<EnemyIdentifier>() : null;
                    if (insurrectionist != null)
                    {
                        AwardVanillaParry(insurrectionist);
                        GrantFortify();
                    }
                    else
                        MonoSingleton<TimeController>.Instance?.ParryFlash();
                    cannonball.Launch();
                    ImpactFeedback(true);
                    ApplyEnemyRecoil(direction);
                    return;
                }

                ParryReceiver receiver = target.GetComponent<ParryReceiver>() ?? target.GetComponentInParent<ParryReceiver>();
                if (receiver != null && receiver.enabled)
                {
                    EnemyIdentifier receiverEnemy = target.GetComponentInParent<EnemyIdentifier>();
                    receiver.Parry();
                    if (receiverEnemy != null && IsNativeMeleeParryWindow(receiverEnemy, hit.transform))
                    {
                        TriggerMeleeParry(receiverEnemy);
                        GrantFortify();
                    }
                    ImpactFeedback(true);
                    ApplyEnemyRecoil(direction);
                    return;
                }

                Grenade grenade = target.GetComponentInParent<Grenade>();
                if (grenade != null && !grenade.enemy)
                {
                    Vector3 targetPoint = origin + direction * 1000f;
                    if (Physics.Raycast(origin, direction, out RaycastHit beamHit, 1000f, LayerMaskDefaults.Get(LMD.EnemiesAndEnvironment)))
                        targetPoint = beamHit.point;
                    grenade.GrenadeBeam(targetPoint, gameObject);
                    ImpactFeedback(true);
                    return;
                }

                Landmine landmine = target.GetComponentInParent<Landmine>();
                if (landmine != null)
                {
                    landmine.transform.LookAt(Punch.GetParryLookTarget());
                    landmine.Parry();
                    ImpactFeedback(true);
                    return;
                }

                Chainsaw saw = target.GetComponentInParent<Chainsaw>();
                if (saw != null)
                {
                    saw.GetPunched();
                    saw.transform.position = origin + direction;
                    saw.rb.velocity = (Punch.GetParryLookTarget() - saw.transform.position).normalized * 105f;
                    ImpactFeedback(true);
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
                        GrantFortify();
                    }
                    else if (meleeParry)
                    {
                        TriggerMeleeParry(enemy);
                        GrantFortify();
                    }

                    // Idols and active Deathcatchers use the same melee-only kill gate as
                    // vanilla Impact Hammer. Keep EB's custom hitter for ordinary enemies, but
                    // identify these special breakables as a native hammer impact so the game's
                    // own Idol/Deathcatcher logic decides whether the hit is effective. This is
                    // not an attack-parry window and therefore does not grant Combustion/Fortify.
                    bool hammerOnlyBreakable = enemy.enemyType == EnemyType.Idol ||
                                               enemy.enemyType == EnemyType.Deathcatcher;
                    enemy.hitter = hammerOnlyBreakable ? "hammer" : "ebstoneguard";
                    if (!enemy.hitterWeapons.Contains("ebstoneguard"))
                        enemy.hitterWeapons.Add("ebstoneguard");
                    using (RuntimeRegistry.BeginAttack())
                        enemy.DeliverDamage(hit.collider.gameObject, direction * 50000f, hit.point,
                            WeaponTuning.CounterDamage, false, 0f, gameObject);
                    ImpactFeedback(true);
                    ApplyEnemyRecoil(direction);
                    return;
                }

                if (LayerMaskDefaults.IsMatchingLayer(hit.collider.gameObject.layer, LMD.Environment))
                {
                    TerrainImpactFeedback(hit, direction);
                    ApplyTerrainRecoil(direction);
                    return;
                }
            }

            SpawnImpactEffect(origin + direction * 2.5f);
        }

        private static void GrantFortify()
        {
            RuntimeRegistry.GrantElementalParryBuff(ElementId.Earth, WeaponTuning.FortifyDuration);
        }

        private void FixedUpdate()
        {
            PlayerTracker tracker = MonoSingleton<PlayerTracker>.Instance;
            if (tracker == null) return;
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
            if (tracker == null) return;
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
                if (tierDownAge <= 0.5f) proposed = jackhammerTier;
            }
            else tierDownAge = 0f;
            jackhammerTier = proposed;
        }

        private void ImpactFeedback(bool sound)
        {
            if (WeaponTuning.CounterHitstop > 0f && MonoSingleton<TimeController>.Instance != null)
                MonoSingleton<TimeController>.Instance.HitStop(WeaponTuning.CounterHitstop);
            CameraController cc = MonoSingleton<CameraController>.Instance;
            if (cc != null) cc.CameraShake(0.55f);
            if (!sound || hammer == null) return;
            AudioSource sfx = HammerHitSoundField?.GetValue(hammer) as AudioSource;
            if (sfx != null)
                UnityEngine.Object.Instantiate(sfx, cc != null ? cc.GetDefaultPos() : Vector3.zero, Quaternion.identity);
        }

        private void TerrainImpactFeedback(RaycastHit hit, Vector3 direction)
        {
            CameraController cc = MonoSingleton<CameraController>.Instance;
            if (cc != null) cc.CameraShake(0.35f);
            if (hit.collider != null && SceneHelper.IsStaticEnvironment(hit))
                MonoSingleton<SceneHelper>.Instance?.CreateEnviroGibs(hit.point - direction, direction, 8f, 10, 2f);
            SpawnImpactEffect(hit.point - direction.normalized);
        }

        private void SpawnImpactEffect(Vector3 position)
        {
            CameraController cc = MonoSingleton<CameraController>.Instance;
            if (hammer == null) return;
            GameObject[] impacts = HammerHitImpactParticleField?.GetValue(hammer) as GameObject[];
            if (impacts == null || impacts.Length == 0 || impacts[0] == null) return;
            UnityEngine.Object.Instantiate(impacts[0], position, cc != null ? cc.transform.rotation : Quaternion.identity);
        }

        private void ApplyEnemyRecoil(Vector3 direction)
        {
            NewMovement movement = MonoSingleton<NewMovement>.Instance;
            if (movement == null) return;
            movement.Launch(-direction * (300f * jackhammerTier + 100f));
            if (storedSpeed > 0f && movement.rb.velocity.magnitude < storedSpeed)
                movement.rb.velocity = -direction.normalized * storedSpeed;
        }

        private void ApplyTerrainRecoil(Vector3 direction)
        {
            NewMovement movement = MonoSingleton<NewMovement>.Instance;
            if (movement == null) return;
            float divisor = ((float)movement.hammerJumps + 3f) / 3f;
            movement.Launch(-direction * ((100f * jackhammerTier + 300f) / divisor));
            movement.hammerJumps++;
            movement.explosionLaunchResistance = 0.5f;
        }

        private static bool IsNativeMeleeParryWindow(EnemyIdentifier eid, Transform hitTransform)
        {
            if (eid == null) return false;
            if (eid.enemyType == EnemyType.Sisyphus)
            {
                Sisyphus insurrectionist = eid.GetComponent<Sisyphus>();
                if (insurrectionist != null && InsurrectionistSwinging(insurrectionist)) return true;
            }
            Enemy enemy = eid.machine ?? eid.zombie ?? eid.statue;
            if (enemy == null) enemy = eid.GetComponent<Enemy>();
            if (enemy == null) return false;
            if (enemy.parryable) return true;
            if (!enemy.partiallyParryable) return false;
            if (enemy.parryables != null && hitTransform != null)
            {
                Transform current = hitTransform;
                while (current != null && current != eid.transform.parent)
                {
                    if (enemy.parryables.Contains(current)) return true;
                    current = current.parent;
                }
            }
            return enemy.parryFramesLeft > 0;
        }

        private static void TriggerMeleeParry(EnemyIdentifier eid)
        {
            if (eid == null) return;
            Enemy enemy = eid.machine ?? eid.zombie ?? eid.statue;
            if (enemy == null) enemy = eid.GetComponent<Enemy>();
            bool swordsmachineHandledByScript = false;
            if (enemy != null)
            {
                EnemyScript script = EnemyScriptField?.GetValue(enemy) as EnemyScript;
                if (script != null)
                {
                    DamageData data = new DamageData
                    {
                        hitTarget = enemy.chest != null ? enemy.chest : eid.gameObject,
                        hitter = "hammerzone",
                        damage = 0f,
                        sourceWeapon = null,
                        force = Vector3.zero,
                        fromExplosion = false
                    };
                    script.OnParry(ref data, true);
                    swordsmachineHandledByScript = script is SwordsMachine;
                }
                enemy.parryable = false;
                enemy.partiallyParryable = false;
                enemy.parryFramesLeft = 0;
                if (enemy.parryables != null) enemy.parryables.Clear();
                if (enemy.parryChallenge != null) enemy.parryChallenge.Done();
            }
            AwardVanillaParry(eid);
            if (!swordsmachineHandledByScript)
                eid.gameObject.SendMessage("GotParried", SendMessageOptions.DontRequireReceiver);
        }

        private static void AwardVanillaParry(EnemyIdentifier eid)
        {
            NewMovement movement = MonoSingleton<NewMovement>.Instance;
            if (movement != null) movement.Parry(eid);
        }
    }

    internal sealed class RockSawbladeController : ActiveWeaponController
    {
        private Nailgun nailgun;
        private Animator animator;
        private bool initialDrawFinished;
        private bool primaryHeatWritePending;
        private float primaryHeatWriteValue;
        private static float cachedOverheatNailgunSpread = -1f;
        private static readonly AccessTools.FieldRef<Nailgun, bool> CanShoot = AccessTools.FieldRefAccess<Nailgun, bool>("canShoot");

        protected override void Awake()
        {
            base.Awake();
            nailgun = GetComponent<Nailgun>();
            animator = GetComponentInChildren<Animator>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            initialDrawFinished = false;
            primaryHeatWritePending = false;
        }

        internal void BeginNativeHeatIsolation()
        {
            // Only a primary Shoot that occurs inside the currently isolated Nailgun.Update
            // should override the pre-Update shared value at the end of that same call.
            primaryHeatWritePending = false;
        }

        internal void NotifyPrimaryHeatWrite(float heat)
        {
            primaryHeatWritePending = true;
            primaryHeatWriteValue = Mathf.Clamp01(heat);
        }

        internal bool TryConsumePrimaryHeatWrite(out float heat)
        {
            heat = primaryHeatWriteValue;
            if (!primaryHeatWritePending)
                return false;
            primaryHeatWritePending = false;
            return true;
        }

        protected override void Update()
        {
            base.Update();
            if (nailgun == null) return;
            if (!initialDrawFinished && CanShoot(nailgun)) initialDrawFinished = true;
            // Rock Burst intentionally does NOT use EB's fresh-press-during-draw rule.  This is
            // one of the secondaries whose authored heavy burst should only exist after the
            // Sawblade Launcher has completed its initial draw.
            if (!CanUseSecondary() || !initialDrawFinished || !SecondaryInputTiming.IsPressed) return;
            FireBurst();
            StartCooldown(WeaponTuning.RockBurstCooldown);
        }

        private void FireBurst()
        {
            CameraController cc = MonoSingleton<CameraController>.Instance;
            if (cc == null || nailgun == null || nailgun.nail == null) return;
            animator?.SetLayerWeight(1, 0f);
            animator?.SetTrigger("SuperShoot");
            MonoSingleton<RumbleManager>.Instance?.SetVibration(RumbleProperties.SuperSaw);
            Transform muzzle = nailgun.shootPoints != null && nailgun.shootPoints.Length > 0 && nailgun.shootPoints[0] != null
                ? nailgun.shootPoints[0].transform : transform;
            if (nailgun.muzzleFlash2 != null)
                UnityEngine.Object.Instantiate(nailgun.muzzleFlash2, muzzle);

            // The Overheat Sawblade prefab does not carry the standard Overheat Nailgun's burst
            // spread value. Borrow it from the actual standard Overheat Nailgun prefab so seven
            // simultaneous saws use the same cone instead of all leaving through the crosshair.
            float spread = ResolveOverheatNailgunSpread() * Mathf.Max(0f, WeaponTuning.RockBurstSpreadMultiplier);
            for (int i = 0; i < WeaponTuning.Defaults.RockBurstSawCount; i++)
            {
                Vector3 position = cc.transform.position + cc.transform.forward;
                GameObject saw = UnityEngine.Object.Instantiate(nailgun.nail, position, cc.transform.rotation);
                Vector3 euler = new Vector3(
                    UnityEngine.Random.Range(-spread / 3f, spread / 3f),
                    UnityEngine.Random.Range(-spread / 3f, spread / 3f),
                    UnityEngine.Random.Range(-spread / 3f, spread / 3f));
                saw.transform.Rotate(euler);
                Rigidbody rb = saw.GetComponent<Rigidbody>();
                if (rb != null) rb.velocity = saw.transform.forward * 200f;
                Nail nail = saw.GetComponent<Nail>();
                if (nail != null)
                {
                    nail.sourceWeapon = gameObject;
                    nail.weaponType = "nailgun0";
                    nail.heated = false;
                    if (nail.sawblade) nail.ForceCheckSawbladeRicochet();
                }
            }

            // Rock Burst is a distinct authored fire mode. Unlike genuine Overheat it SETS the
            // shared Rapid heat resource, with its own tuning value independent from primary.
            ElementalRapidSharedHeatUpdatePatch.SetSharedHeat(nailgun, WeaponTuning.EarthRapidHeatOnAltFire);
            MonoSingleton<CameraController>.Instance?.CameraShake(0.5f);
        }

        private static float ResolveOverheatNailgunSpread()
        {
            if (cachedOverheatNailgunSpread >= 0f)
                return cachedOverheatNailgunSpread;

            cachedOverheatNailgunSpread = 30f;
            GunSetter setter = UnityEngine.Object.FindObjectOfType<GunSetter>();
            GameObject overheatPrefab = ElementCatalog.GetElementPrefab(setter, WeaponFamily.Rapid, ElementId.Fire);
            Nailgun overheat = overheatPrefab != null ? overheatPrefab.GetComponent<Nailgun>() : null;
            if (overheat != null && overheat.spread > 0.001f)
                cachedOverheatNailgunSpread = overheat.spread;
            return cachedOverheatNailgunSpread;
        }
    }

    internal sealed class MeteorShowerRuntime : MonoBehaviour
    {
        internal GameObject sourceWeapon;
        internal Vector3 target;
        internal Vector3 up;

        private void Start()
        {
            // Spawn the full shower immediately. Later meteors begin progressively higher, so
            // their impact schedule is the same as the old delayed-spawn sequence while every
            // meteor exists from frame one and can intersect high-altitude enemies on the way in.
            float baseDuration = Mathf.Max(0.1f, WeaponTuning.MeteorTravelDuration);
            float interval = Mathf.Max(0.05f, WeaponTuning.MeteorInterval);
            float baseHeight = 42f;
            float fallSpeed = baseHeight / baseDuration;
            for (int i = 0; i < WeaponTuning.Defaults.MeteorCount; i++)
            {
                float duration = baseDuration + interval * i;
                SpawnMeteor(fallSpeed * duration, duration);
            }
            Destroy(gameObject);
        }

        private void SpawnMeteor(float startHeight, float travelDuration)
        {
            Vector2 disk = UnityEngine.Random.insideUnitCircle * WeaponTuning.MeteorAreaRadius;
            Vector3 right = Vector3.Cross(up, Vector3.forward);
            if (right.sqrMagnitude < 0.01f) right = Vector3.Cross(up, Vector3.right);
            right.Normalize();
            Vector3 forward = Vector3.Cross(right, up).normalized;
            Vector3 candidate = target + right * disk.x + forward * disk.y;
            Vector3 destination = candidate;
            if (Physics.Raycast(candidate + up * 3f, -up, out RaycastHit floor, 1000f,
                LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
                destination = floor.point;

            GameObject meteor = new GameObject("EB Earth Meteor");
            EarthMeteorRuntime runtime = meteor.AddComponent<EarthMeteorRuntime>();
            runtime.sourceWeapon = sourceWeapon;
            runtime.destination = destination;
            runtime.up = up;
            runtime.startHeight = startHeight;
            runtime.travelDuration = travelDuration;
        }
    }

    internal static class EarthAudio
    {
        private static bool resolved;
        private static GameObject cannonballBreakEffect;
        private static readonly AccessTools.FieldRef<Cannonball, GameObject> CannonballBreakEffect =
            AccessTools.FieldRefAccess<Cannonball, GameObject>("breakEffect");

        internal static void PlaySrsCannonballBreak(Vector3 position)
        {
            ResolveSrsBreakEffect();
            if (cannonballBreakEffect == null)
                return;

            // Instantiate the real S.R.S. cannonball break-effect prefab so its authored audio
            // setup/randomization runs, but suppress its visuals because Meteor already owns an
            // explosion effect of its own.
            GameObject effect = UnityEngine.Object.Instantiate(cannonballBreakEffect, position, Quaternion.identity);
            if (effect == null)
                return;
            foreach (Renderer renderer in effect.GetComponentsInChildren<Renderer>(true))
                if (renderer != null) renderer.enabled = false;
            foreach (Light light in effect.GetComponentsInChildren<Light>(true))
                if (light != null) light.enabled = false;
            foreach (Collider collider in effect.GetComponentsInChildren<Collider>(true))
                if (collider != null) collider.enabled = false;
            UnityEngine.Object.Destroy(effect, 6f);
        }

        private static void ResolveSrsBreakEffect()
        {
            if (resolved)
                return;
            resolved = true;
            GunSetter setter = UnityEngine.Object.FindObjectOfType<GunSetter>();
            GameObject srsPrefab = ElementCatalog.GetElementPrefab(setter, WeaponFamily.Explosive, ElementId.Earth);
            RocketLauncher launcher = srsPrefab != null ? srsPrefab.GetComponent<RocketLauncher>() : null;
            Cannonball cannonball = launcher != null && launcher.cannonBall != null
                ? launcher.cannonBall.GetComponent<Cannonball>()
                : null;
            if (cannonball != null)
                cannonballBreakEffect = CannonballBreakEffect(cannonball);
        }
    }

    internal sealed class EarthMeteorRuntime : MonoBehaviour
    {
        internal GameObject sourceWeapon;
        internal Vector3 destination;
        internal Vector3 up;
        internal float startHeight = 42f;
        internal float travelDuration;
        private Vector3 start;
        private GameObject visual;
        private float born;

        private void Start()
        {
            born = Time.time;
            start = destination + up * Mathf.Max(1f, startHeight);
            transform.position = start;
            visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Earth Meteor Visual";
            Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = Vector3.one * 1.8f;
            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                Material material = new Material(shader);
                if (material.HasProperty("_Color")) material.color = ElementPalette.Accent(ElementId.Earth);
                renderer.material = material;
            }
        }

        private void Update()
        {
            float duration = travelDuration > 0f ? travelDuration : Mathf.Max(0.1f, WeaponTuning.MeteorTravelDuration);
            float t0 = Mathf.Clamp01((Time.time - born) / duration);
            Vector3 previous = transform.position;
            Vector3 next = Vector3.Lerp(start, destination, t0);
            Vector3 delta = next - previous;
            if (delta.sqrMagnitude > 0.0001f)
            {
                RaycastHit[] hits = Physics.SphereCastAll(previous, 0.75f, delta.normalized, delta.magnitude,
                    LayerMaskDefaults.Get(LMD.Enemies), QueryTriggerInteraction.Collide);
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (RaycastHit hit in hits)
                {
                    EnemyIdentifier enemy = hit.collider.GetComponentInParent<EnemyIdentifierIdentifier>()?.eid ??
                                            hit.collider.GetComponentInParent<EnemyIdentifier>();
                    if (enemy != null && !enemy.dead)
                    {
                        Impact(hit.point);
                        return;
                    }
                }
            }
            transform.position = next;
            if (t0 >= 1f) Impact(destination);
        }

        private void Impact(Vector3 point)
        {
            float radius = 6f * WeaponTuning.MeteorExplosionScale;
            using (RuntimeRegistry.BeginAttack())
            {
                foreach (EnemyIdentifier enemy in RuntimeVisuals.EnemiesInSphere(point, radius))
                {
                    enemy.hitter = "ebmeteor";
                    // Meteor Shower is intentionally a pure damage field for enemies. The old
                    // outward force made repeated impacts scatter targets out of the shower.
                    enemy.DeliverDamage(enemy.gameObject, Vector3.zero, enemy.bodyTransform.position,
                        WeaponTuning.MeteorDamage, false, 0f, sourceWeapon);
                }
            }

            // Self-damage is authored separately from the stock Explosion component so we can
            // keep the explosion visual/player hazard without allowing that component to push
            // enemy rigidbodies. Each meteor may hurt the player once if they are inside its AoE.
            NewMovement movement = MonoSingleton<NewMovement>.Instance;
            if (movement != null && WeaponTuning.MeteorSelfDamage > 0f &&
                Vector3.Distance(movement.transform.position, point) <= radius)
            {
                movement.GetHurt(Mathf.RoundToInt(WeaponTuning.MeteorSelfDamage), true, 0, true);
            }

            EarthAudio.PlaySrsCannonballBreak(point);
            GameObject explosion = RuntimeVisuals.SpawnEnemyOnlyExplosion(point, WeaponTuning.MeteorExplosionScale, 0, sourceWeapon);
            if (explosion != null)
            {
                // damage=0 stock explosions can still impart generic Rigidbody push. Make every
                // mechanical shell harmless; the custom enemy damage/self-damage above is final.
                foreach (Explosion shell in explosion.GetComponentsInChildren<Explosion>(true))
                {
                    if (shell == null) continue;
                    shell.damage = 0;
                    shell.playerDamageOverride = -1;
                    shell.harmless = true;
                }
                foreach (Light light in explosion.GetComponentsInChildren<Light>(true))
                    if (light != null) light.color = ElementPalette.Accent(ElementId.Earth);
            }
            Destroy(gameObject);
        }
    }
}
