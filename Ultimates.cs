using System;
using System.Collections;
using System.Collections.Generic;
using ULTRAKILL.Cheats;
using UnityEngine;

namespace ElementalBattlegroundsMod
{
    // ULTRAKILL's native Railcannon meter is not a linear 0..5 timer. While charging it
    // advances through raw raicharge 0..4, then snaps to the ready sentinel 5. The HUD likewise
    // renders raw/4 until ready. EB exposes custom Ultimate costs on a logical 0..5 scale so a
    // configured 3.75 truly means 75% of the visible/recharge bar rather than 3.75 raw units
    // (which would actually be 93.75% of the native charging span).
    internal static class UltimateChargeRules
    {
        internal const float NativeChargingSpan = 4f;
        private const float Epsilon = 0.0001f;

        internal static float LogicalFromNative(float rawCharge)
        {
            if (rawCharge > NativeChargingSpan + Epsilon)
                return WeaponTuning.Defaults.UltimateChargeMax;
            return Mathf.Clamp(rawCharge, 0f, NativeChargingSpan) *
                (WeaponTuning.Defaults.UltimateChargeMax / NativeChargingSpan);
        }

        internal static float NativeFromLogical(float logicalCharge)
        {
            float logical = Mathf.Clamp(logicalCharge, 0f, WeaponTuning.Defaults.UltimateChargeMax);
            if (logical >= WeaponTuning.Defaults.UltimateChargeMax - Epsilon)
                return WeaponTuning.Defaults.UltimateChargeMax;
            return logical * (NativeChargingSpan / WeaponTuning.Defaults.UltimateChargeMax);
        }

        internal static bool HasCost(float rawCharge, float logicalCost)
        {
            return LogicalFromNative(rawCharge) + Epsilon >=
                Mathf.Clamp(logicalCost, 0f, WeaponTuning.Defaults.UltimateChargeMax);
        }

        internal static float Spend(float rawCharge, float logicalCost)
        {
            float remaining = Mathf.Max(0f, LogicalFromNative(rawCharge) -
                Mathf.Clamp(logicalCost, 0f, WeaponTuning.Defaults.UltimateChargeMax));
            return NativeFromLogical(remaining);
        }
    }

    internal sealed class UltimateController : MonoBehaviour
    {
        private ElementalSlotMarker marker;
        private Railcannon rail;
        private float grassReadyAt;
        private float grassFireBufferedUntil;
        private bool grassFireHeldBeforeEnable;
        private const float GrassInputBufferSeconds = 0.15f;
        private readonly MaterialPropertyBlock railBlock = new MaterialPropertyBlock();
        private static readonly int EmissiveIntensityID = Shader.PropertyToID("_EmissiveIntensity");

        private void Awake()
        {
            marker = GetComponent<ElementalSlotMarker>();
            rail = GetComponent<Railcannon>();
        }

        private void OnEnable()
        {
            grassFireBufferedUntil = 0f;
            grassFireHeldBeforeEnable = false;
            if (marker == null || marker.element != ElementId.Grass)
                return;

            InputManager manager = MonoSingleton<InputManager>.Instance;
            if (manager == null || manager.InputSource == null || manager.InputSource.Fire1 == null)
                return;

            bool fresh = manager.InputSource.Fire1.WasPerformedThisFrame;
            grassFireHeldBeforeEnable = manager.InputSource.Fire1.IsPressed && !fresh;
            if (fresh)
                grassFireBufferedUntil = Time.time + GrassInputBufferSeconds;
        }

        private void OnDisable()
        {
            grassFireBufferedUntil = 0f;
            grassFireHeldBeforeEnable = false;
        }

        private void Update()
        {
            if (marker == null)
                return;

            if (marker.element == ElementId.Grass)
            {
                UpdateGrassChargeDisplay();

                InputManager manager = MonoSingleton<InputManager>.Instance;
                if (manager == null || manager.InputSource == null || manager.InputSource.Fire1 == null)
                    return;

                bool firePressed = manager.InputSource.Fire1.IsPressed;
                bool freshPress = manager.InputSource.Fire1.WasPerformedThisFrame;
                if (grassFireHeldBeforeEnable)
                {
                    if (!firePressed)
                        grassFireHeldBeforeEnable = false;
                    else if (!freshPress)
                        return;
                }
                if (freshPress)
                {
                    grassFireHeldBeforeEnable = false;
                    grassFireBufferedUntil = Time.time + GrassInputBufferSeconds;
                }

                // Grass owns a short repeated-fire lockout. Store it as an absolute ready time so
                // it keeps elapsing while the weapon is holstered instead of freezing whenever
                // this GameObject is inactive. The small fresh-press buffer also survives Unity's
                // weapon-activation/update ordering on the draw frame.
                if (Time.time > grassFireBufferedUntil ||
                    (!NoWeaponCooldown.NoCooldown && Time.time < grassReadyAt) ||
                    GameStateManager.Instance.PlayerInputLocked || MonoSingleton<GunControl>.Instance == null ||
                    !MonoSingleton<GunControl>.Instance.activated)
                    return;

                WeaponCharges charges = MonoSingleton<WeaponCharges>.Instance;
                if (charges == null || (!UltimateChargeRules.HasCost(charges.raicharge, WeaponTuning.SporeCost) && !NoWeaponCooldown.NoCooldown))
                    return;

                if (!NoWeaponCooldown.NoCooldown)
                    charges.raicharge = UltimateChargeRules.Spend(charges.raicharge, WeaponTuning.SporeCost);
                charges.railChargePlayed = false;
                grassReadyAt = Time.time + Mathf.Max(0f, WeaponTuning.SporeFireLockout);
                grassFireBufferedUntil = 0f;
                SpawnSporeProjectile();
                return;
            }

        }

        internal void ResetOnRespawn()
        {
            grassReadyAt = 0f;
            grassFireBufferedUntil = 0f;
            grassFireHeldBeforeEnable = false;
        }

        private void UpdateGrassChargeDisplay()
        {
            if (rail == null || MonoSingleton<WeaponCharges>.Instance == null) return;
            float charge = UltimateChargeRules.LogicalFromNative(MonoSingleton<WeaponCharges>.Instance.raicharge);
            Color accent = ElementPalette.Accent(ElementId.Grass);
            if (rail.body != null)
            {
                rail.body.GetPropertyBlock(railBlock);
                railBlock.SetColor("_EmissiveColor", accent);
                railBlock.SetFloat(EmissiveIntensityID, charge / WeaponTuning.Defaults.UltimateChargeMax);
                rail.body.SetPropertyBlock(railBlock);
            }
            if (rail.pips != null)
            {
                float pipCharge = rail.pips.Length > 0
                    ? (charge / WeaponTuning.Defaults.UltimateChargeMax) * rail.pips.Length
                    : 0f;
                for (int i = 0; i < rail.pips.Length; i++)
                {
                    if (rail.pips[i] == null) continue;
                    float fill = Mathf.Clamp01(pipCharge - i);
                    rail.pips[i].GetPropertyBlock(railBlock);
                    railBlock.SetColor("_EmissiveColor", accent);
                    railBlock.SetFloat(EmissiveIntensityID, fill);
                    rail.pips[i].SetPropertyBlock(railBlock);
                }
            }
        }

        internal void ActivateFromFullCharge()
        {
            if (marker == null)
                return;
            if (marker.element == ElementId.Fire)
            {
                // Lock weapon switching on the same frame the shot is accepted, rather than waiting
                // for InfernoRuntime.Start on the next Unity frame.
                RuntimeRegistry.FreezeRailCharge = true;
                RuntimeRegistry.InfernoActive = true;
                GameObject host = new GameObject("EB Inferno Ultimate");
                InfernoRuntime runtime = host.AddComponent<InfernoRuntime>();
                runtime.sourceWeapon = gameObject;
            }
            else if (marker.element == ElementId.Water)
            {
                GameObject host = new GameObject("EB Water Dragon Ultimate");
                WaterDragonRuntime runtime = host.AddComponent<WaterDragonRuntime>();
                runtime.sourceWeapon = gameObject;
            }
            else if (marker.element == ElementId.Wind)
            {
                CameraController cc = MonoSingleton<CameraController>.Instance;
                if (cc == null)
                    return;
                Vector3 origin = cc.GetDefaultPos();
                Vector3 target = origin + cc.transform.forward * 45f;
                if (Physics.Raycast(origin, cc.transform.forward, out RaycastHit hit, 120f,
                    LayerMaskDefaults.Get(LMD.EnemiesAndEnvironment), QueryTriggerInteraction.Collide))
                    target = hit.point;
                if (Physics.Raycast(target + Vector3.up * 20f, Vector3.down, out RaycastHit floor, 80f,
                    LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
                    target = floor.point;
                GameObject host = new GameObject("EB Grand Cyclone Ultimate");
                host.transform.position = target;
                GrandCycloneRuntime runtime = host.AddComponent<GrandCycloneRuntime>();
                runtime.sourceWeapon = gameObject;
            }
            else if (marker.element == ElementId.Earth)
            {
                CameraController cc = MonoSingleton<CameraController>.Instance;
                NewMovement movement = MonoSingleton<NewMovement>.Instance;
                if (cc == null || movement == null || movement.rb == null)
                    return;
                Vector3 up = -movement.rb.GetGravityDirection().normalized;
                Vector3 origin = cc.GetDefaultPos();
                // Meteor Shower has no arbitrary cast range. If the crosshair ray reaches a
                // surface/enemy anywhere in the level, use it; the finite fallback exists only
                // for aiming into empty sky where there is literally no hit point to target.
                Vector3 target = origin + cc.transform.forward * 500f;
                if (Physics.Raycast(origin, cc.transform.forward, out RaycastHit hit, float.PositiveInfinity,
                    LayerMaskDefaults.Get(LMD.EnemiesAndEnvironment), QueryTriggerInteraction.Collide))
                    target = hit.point;
                // Pick the intended destination now. Individual meteors ignore environment while
                // falling, so roofs between their start point and this destination cannot eat them.
                if (Physics.Raycast(target + up * 3f, -up, out RaycastHit floor, 1000f,
                    LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
                    target = floor.point;
                GameObject host = new GameObject("EB Meteor Shower Ultimate");
                MeteorShowerRuntime runtime = host.AddComponent<MeteorShowerRuntime>();
                runtime.sourceWeapon = gameObject;
                runtime.target = target;
                runtime.up = up;
            }
        }

        private void SpawnSporeProjectile()
        {
            CameraController cc = MonoSingleton<CameraController>.Instance;
            GameObject host = new GameObject("EB Spore Bombardment Projectile");
            host.transform.position = cc.GetDefaultPos() + cc.transform.forward;
            SporeProjectileRuntime projectile = host.AddComponent<SporeProjectileRuntime>();
            projectile.sourceWeapon = gameObject;
            projectile.velocity = cc.transform.forward * WeaponTuning.SporeProjectileSpeed;
        }
    }

    internal sealed class InfernoRuntime : MonoBehaviour
    {
        public GameObject sourceWeapon;
        private NewMovement movement;
        private float originalWalkSpeed;
        private float originalAirAcceleration;
        private bool restored;

        private void Start()
        {
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            movement = MonoSingleton<NewMovement>.Instance;
            if (movement == null)
            {
                Destroy(gameObject);
                yield break;
            }
            originalWalkSpeed = movement.walkSpeed;
            originalAirAcceleration = movement.airAcceleration;
            RuntimeRegistry.FreezeRailCharge = true;
            RuntimeRegistry.InfernoActive = true;
            movement.modNoDashSlide = true;

            float duration = WeaponTuning.InfernoWindup;
            float elapsed = 0f;
            float startingSpeed = movement.rb != null ? movement.rb.velocity.magnitude : 0f;
            Vector3 initialGravity = movement.rb != null ? movement.rb.GetGravityDirection().normalized : Vector3.down;
            float startingFall = movement.rb != null ? Mathf.Max(0f, Vector3.Dot(movement.rb.velocity, initialGravity)) : 0f;
            GameObject visual = RuntimeVisuals.Primitive(PrimitiveType.Sphere, "Inferno Windup", movement.transform.position,
                Vector3.one * 1.5f, new Color(1f, 0.12f, 0.02f, 0.45f));
            while (elapsed < duration && movement != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                float moveFactor = Mathf.Lerp(1f, 0.08f, t);
                movement.walkSpeed = originalWalkSpeed * moveFactor;
                movement.airAcceleration = originalAirAcceleration * moveFactor;
                movement.modNoDashSlide = true;

                Vector3 gravityDirection = movement.rb.GetGravityDirection().normalized;
                Vector3 velocity = movement.rb.velocity;
                // Air acceleration alone does not slow already-stored Source velocity. Enforce a
                // continuously shrinking speed ceiling so a fast straight-line launch cannot hit
                // a plateau simply because the player never changes direction.
                float speedCeiling = Mathf.Lerp(Mathf.Max(startingSpeed, 8f), 2.5f, t);
                if (velocity.magnitude > speedCeiling)
                    velocity = velocity.normalized * speedCeiling;

                float fall = Mathf.Max(0f, Vector3.Dot(velocity, gravityDirection));
                float fallCeiling = Mathf.Lerp(Mathf.Max(startingFall, 8f), 2.5f, t);
                if (fall > fallCeiling)
                    velocity += gravityDirection * (fallCeiling - fall);
                movement.rb.velocity = velocity;

                if (visual != null)
                {
                    visual.transform.position = movement.transform.position;
                    visual.transform.localScale = Vector3.one * Mathf.Lerp(1.5f, 7f, t);
                    visual.transform.Rotate(Vector3.up, 240f * Time.deltaTime, Space.World);
                }
                yield return null;
            }

            if (visual != null) Destroy(visual);
            if (movement != null)
            {
                Vector3 position = movement.transform.position;
                using (RuntimeRegistry.BeginAttack())
                    RuntimeVisuals.SpawnEnemyOnlyExplosion(position, WeaponTuning.InfernoExplosionScale, Mathf.RoundToInt(WeaponTuning.InfernoExplosionDamage), sourceWeapon, true);
                RuntimeRegistry.IgniteSporeClouds(position, 28f, sourceWeapon);
            }
            RestoreMovement();
            Destroy(gameObject);
        }

        private void RestoreMovement()
        {
            if (restored) return;
            restored = true;
            RuntimeRegistry.FreezeRailCharge = false;
            RuntimeRegistry.InfernoActive = false;
            if (movement != null)
            {
                movement.walkSpeed = originalWalkSpeed;
                movement.airAcceleration = originalAirAcceleration;
                movement.modNoDashSlide = false;
            }
        }

        private void OnDestroy()
        {
            RestoreMovement();
        }
    }

    internal sealed class WaterDragonRuntime : MonoBehaviour
    {
        public GameObject sourceWeapon;
        private EnemyIdentifier target;
        private GameObject visual;
        private float totalAge;
        private float targetAge;
        private int chain;
        private bool pickupApplied;
        private bool pickupKilledTarget;

        private void Start()
        {
            CameraController cc = MonoSingleton<CameraController>.Instance;
            transform.position = cc.GetDefaultPos() + cc.transform.forward * 3f + Vector3.up * 1.5f;
            visual = RuntimeVisuals.Primitive(PrimitiveType.Capsule, "Water Dragon Placeholder", transform.position,
                new Vector3(1.5f, 4f, 1.5f), new Color(0.15f, 0.55f, 1f, 0.65f));
            AcquireTarget();
        }

        private void Update()
        {
            totalAge += Time.deltaTime;
            if (totalAge >= WeaponTuning.DragonMaxLifetime)
            {
                if (target != null && !target.dead)
                    FinishAttack();
                else
                    Destroy(gameObject);
                return;
            }

            if (target == null)
            {
                AcquireTarget();
                if (target == null)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            if (target.dead)
            {
                if (!pickupKilledTarget && chain < WeaponTuning.Defaults.DragonMaxChain)
                    chain++;
                AcquireTarget();
                return;
            }

            targetAge += Time.deltaTime;
            Vector3 mouth = target.bodyTransform.position + Vector3.up * 1.2f;
            transform.position = Vector3.MoveTowards(transform.position, mouth, 48f * Time.deltaTime);
            if (visual != null)
            {
                visual.transform.position = transform.position;
                if (target != null)
                    visual.transform.rotation = Quaternion.LookRotation((target.bodyTransform.position - transform.position).normalized);
            }

            if (!pickupApplied && Vector3.Distance(transform.position, mouth) < 1.2f)
            {
                pickupApplied = true;
                float damage = target.enemyType == EnemyType.Filth ? 20f : 0.1f;
                bool wasDead = target.dead;
                target.hitter = "ebwaterdragonpickup";
                target.DeliverDamage(target.gameObject, Vector3.zero, target.bodyTransform.position, damage, false, 0f, sourceWeapon);
                pickupKilledTarget = !wasDead && target.dead;
                if (pickupKilledTarget)
                {
                    AcquireTarget();
                    return;
                }
            }

            if (pickupApplied && targetAge >= WeaponTuning.DragonPickupDelay)
                FinishAttack();
        }

        private void AcquireTarget()
        {
            target = FindTarget();
            targetAge = 0f;
            pickupApplied = false;
            pickupKilledTarget = false;
        }

        private EnemyIdentifier FindTarget()
        {
            EnemyIdentifier[] enemies = UnityEngine.Object.FindObjectsOfType<EnemyIdentifier>();
            if (enemies == null || enemies.Length == 0)
                return null;
            CameraController cc = MonoSingleton<CameraController>.Instance;
            Vector3 origin = cc.GetDefaultPos();
            Vector3 forward = cc.transform.forward;
            EnemyIdentifier best = null;
            float bestScore = float.PositiveInfinity;
            foreach (EnemyIdentifier eid in enemies)
            {
                if (eid == null || eid.dead || eid.dontCountAsKills)
                    continue;
                Vector3 to = eid.bodyTransform.position - origin;
                float distance = to.magnitude;
                if (distance > 120f)
                    continue;
                float anglePenalty = 1f - Mathf.Clamp01(Vector3.Dot(forward, to.normalized));
                float score = distance + anglePenalty * 35f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = eid;
                }
            }
            return best;
        }

        private void FinishAttack()
        {
            if (target == null || target.dead)
            {
                AcquireTarget();
                return;
            }
            Vector3 impact = target.bodyTransform.position;
            if (Physics.Raycast(impact + Vector3.up * 4f, Vector3.down, out RaycastHit hit, 30f, LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
                impact = hit.point;

            float radius = WeaponTuning.DragonBaseRadius + chain * WeaponTuning.DragonRadiusPerChain;
            float damage = WeaponTuning.DragonBaseDamage + chain * WeaponTuning.DragonDamagePerChain;
            using (RuntimeRegistry.BeginAttack())
            {
                foreach (EnemyIdentifier eid in RuntimeVisuals.EnemiesInSphere(impact, radius))
                {
                    float falloff = 1f - Mathf.Clamp01(Vector3.Distance(impact, eid.bodyTransform.position) / radius) * 0.35f;
                    eid.hitter = "ebwaterdragon";
                    eid.DeliverDamage(eid.gameObject, Vector3.up * 2500f, eid.bodyTransform.position,
                        damage * falloff, false, 0f, sourceWeapon);
                }
            }
            RuntimeVisuals.SpawnEnemyOnlyExplosion(impact, 1.3f + chain * 0.14f, 60 + chain * 14, sourceWeapon, true);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (visual != null) Destroy(visual);
        }
    }

    internal sealed class SporeProjectileRuntime : MonoBehaviour
    {
        public GameObject sourceWeapon;
        public Vector3 velocity;
        private float age;
        private GameObject visual;

        private void Start()
        {
            visual = RuntimeVisuals.Primitive(PrimitiveType.Sphere, "Spore Projectile Visual", transform.position,
                Vector3.one * 1.2f, new Color(0.65f, 0.18f, 0.95f, 0.72f));
        }

        private void Update()
        {
            float step = velocity.magnitude * Time.deltaTime;
            Vector3 direction = velocity.normalized;
            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, step + 0.5f, LayerMaskDefaults.Get(LMD.EnemiesAndEnvironment), QueryTriggerInteraction.Collide))
            {
                transform.position = hit.point;
                CreateCloud();
                return;
            }
            transform.position += velocity * Time.deltaTime;
            if (visual != null) visual.transform.position = transform.position;
            age += Time.deltaTime;
            if (age >= 2.2f)
                CreateCloud();
        }

        private void CreateCloud()
        {
            GameObject cloudObj = new GameObject("EB Spore Cloud");
            cloudObj.transform.position = transform.position;
            SporeCloudRuntime cloud = cloudObj.AddComponent<SporeCloudRuntime>();
            cloud.sourceWeapon = sourceWeapon;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (visual != null) Destroy(visual);
        }
    }

    internal sealed class SporeCloudRuntime : MonoBehaviour
    {
        public GameObject sourceWeapon;
        private float remaining;
        private float tick;
        private bool ignited;
        private GameObject visual;

        private void Start()
        {
            remaining = WeaponTuning.SporeCloudLifetime;
            RuntimeRegistry.SporeClouds.Add(this);
            visual = RuntimeVisuals.Primitive(PrimitiveType.Sphere, "Spore Cloud Visual", transform.position + Vector3.up * 2f,
                new Vector3(11f, 6f, 11f), new Color(0.55f, 0.12f, 0.75f, 0.28f));
        }

        private void Update()
        {
            if (ignited) return;
            remaining -= Time.deltaTime;
            tick -= Time.deltaTime;
            if (tick <= 0f)
            {
                tick = 0.5f;
                foreach (EnemyIdentifier eid in RuntimeVisuals.EnemiesInSphere(transform.position + Vector3.up * 2f, WeaponTuning.SporeCloudRadius))
                    PoisonStatus.Apply(eid, sourceWeapon, 1.5f, 0.05f);
            }
            if (remaining <= 0f)
                Destroy(gameObject);
        }

        internal void Ignite(GameObject fireSource)
        {
            if (ignited) return;
            ignited = true;
            RuntimeRegistry.SporeClouds.Remove(this);
            float strength = Mathf.Clamp01(remaining / Mathf.Max(0.01f, WeaponTuning.SporeCloudLifetime));
            float radius = 5.5f + 1.5f * strength;
            float damage = Mathf.Lerp(WeaponTuning.SporeIgniteBaseDamage, WeaponTuning.SporeIgniteMaxDamage, strength);
            using (RuntimeRegistry.BeginAttack())
            {
                foreach (EnemyIdentifier eid in RuntimeVisuals.EnemiesInSphere(transform.position + Vector3.up * 2f, radius))
                {
                    eid.hitter = "ebsporeignite";
                    eid.DeliverDamage(eid.gameObject, (eid.bodyTransform.position - transform.position).normalized * 3000f,
                        eid.bodyTransform.position, damage, true, 0f, fireSource ?? sourceWeapon);
                }
            }
            RuntimeVisuals.SpawnEnemyOnlyExplosion(transform.position + Vector3.up * 2f, 0.9f + strength * 0.35f,
                Mathf.RoundToInt(35f + strength * 25f), fireSource ?? sourceWeapon, true);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            RuntimeRegistry.SporeClouds.Remove(this);
            if (visual != null) Destroy(visual);
        }
    }
}
