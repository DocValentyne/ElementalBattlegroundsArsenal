using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBattlegroundsMod
{
    internal sealed class StormChargedBeamExplosionTag : MonoBehaviour
    {
        // Public so Unity copies the values when vanilla clones this beam for Coin routing.
        public bool exploded;
        public float baseBeamDamage;
    }

    // The vanilla Piercer stores variation-0 charge in one global WeaponCharges bucket. EB can put
    // Water and Storm Piercers in separate arsenal positions at the same time, so Storm keeps its
    // own per-weapon copy and only borrows vanilla's actual charge/fire implementation while held.
    internal sealed class StormRevolverChargeState : MonoBehaviour
    {
        private static readonly AccessTools.FieldRef<Revolver, float> PierceCharge =
            AccessTools.FieldRefAccess<Revolver, float>("pierceCharge");
        private static readonly AccessTools.FieldRef<Revolver, bool> PierceReady =
            AccessTools.FieldRefAccess<Revolver, bool>("pierceReady");
        private static readonly AccessTools.FieldRef<Revolver, float> PierceShotCharge =
            AccessTools.FieldRefAccess<Revolver, float>("pierceShotCharge");
        private static readonly AccessTools.FieldRef<Revolver, bool> ChargingPierce =
            AccessTools.FieldRefAccess<Revolver, bool>("chargingPierce");

        private float storedCharge = 100f;
        private float storedAt;
        private bool initialized;

        private void Awake()
        {
            initialized = true;
            storedCharge = 100f;
            storedAt = Time.time;
        }

        private void OnEnable()
        {
            Revolver revolver = GetComponent<Revolver>();
            if (revolver != null)
                Apply(revolver);
        }

        internal void Capture(Revolver revolver)
        {
            StormRevolverChargeState owner = SlotResourceOwnerResolver.Resolve(this);
            if (owner != null && owner != this)
            {
                owner.Capture(revolver);
                return;
            }
            if (revolver == null)
                return;
            storedCharge = Mathf.Clamp(PierceCharge(revolver), 0f, 100f);
            storedAt = Time.time;
            initialized = true;
        }

        internal void Apply(Revolver revolver)
        {
            StormRevolverChargeState owner = SlotResourceOwnerResolver.Resolve(this);
            if (owner != null && owner != this)
            {
                owner.Apply(revolver);
                return;
            }
            if (revolver == null)
                return;
            if (!initialized)
            {
                storedCharge = 100f;
                storedAt = Time.time;
                initialized = true;
            }
            else
            {
                // Standard Piercer recharges at 40 charge/second. Storm owns its own bucket, so
                // reproduce that recharge while this weapon is holstered instead of freezing it.
                storedCharge = Mathf.Min(100f, storedCharge + Mathf.Max(0f, Time.time - storedAt) * 40f);
                storedAt = Time.time;
            }
            PierceCharge(revolver) = storedCharge;
            PierceReady(revolver) = storedCharge >= 99.999f;
            PierceShotCharge(revolver) = 0f;
            ChargingPierce(revolver) = false;
        }
    }

    // Configure the mechanical Storm blast only after ULTRAKILL's ExplosionController has chosen
    // which authored explosion branch is actually active. This preserves the stock expanding
    // collider instead of letting several quality/visual shells create an effectively instant
    // full-size hit radius.
    [DefaultExecutionOrder(10000)]
    internal sealed class StormExplosionRuntime : MonoBehaviour
    {
        internal float enemyDamage;
        internal float selfDamage;
        internal GameObject sourceWeapon;
        internal readonly HashSet<int> authoredMechanicalIds = new HashSet<int>();

        private void Start()
        {
            Explosion[] explosions = GetComponentsInChildren<Explosion>(true);
            Explosion mechanical = null;

            foreach (Explosion explosion in explosions)
            {
                if (explosion == null)
                    continue;
                if (authoredMechanicalIds.Contains(explosion.GetInstanceID()) && explosion.gameObject.activeInHierarchy)
                {
                    mechanical = explosion;
                    break;
                }
            }
            if (mechanical == null)
            {
                foreach (Explosion explosion in explosions)
                {
                    if (explosion == null)
                        continue;
                    if (authoredMechanicalIds.Contains(explosion.GetInstanceID()))
                    {
                        mechanical = explosion;
                        break;
                    }
                }
            }
            if (mechanical == null && explosions.Length > 0)
                mechanical = explosions[0];

            HurtCooldownCollection sharedCooldown = new HurtCooldownCollection();
            foreach (Explosion explosion in explosions)
            {
                if (explosion == null)
                    continue;

                explosion.sourceWeapon = sourceWeapon;
                explosion.hitterWeapon = "ebstorm";
                explosion.enabled = true;
                if (explosion == mechanical)
                {
                    explosion.harmless = false;
                    explosion.enemy = false;
                    explosion.canHit = selfDamage > 0f ? AffectedSubjects.All : AffectedSubjects.EnemiesOnly;
                    explosion.damage = Mathf.Max(0, Mathf.RoundToInt(enemyDamage * 10f));
                    // Preserve the configured damage while the stock collider expands. Vanilla
                    // normally divides damage by 1.5 after the shell reaches half size.
                    explosion.halved = true;
                    explosion.playerDamageOverride = selfDamage > 0f
                        ? Mathf.Max(0, Mathf.RoundToInt(selfDamage))
                        : -1;
                    explosion.HurtCooldownCollection = sharedCooldown;
                }
                else
                {
                    // Keep every authored visual shell enabled so it can grow/fade/destroy itself,
                    // but only the one active mechanical shell may deal Storm damage.
                    explosion.damage = 0;
                    explosion.playerDamageOverride = -1;
                    explosion.canHit = AffectedSubjects.EnemiesOnly;
                    explosion.harmless = true;
                    explosion.HurtCooldownCollection = null;
                }
            }
        }
    }

    internal static class StormEffects
    {
        private static readonly Color StormBlue = new Color(0.18f, 0.72f, 1f, 1f);
        private static GameObject electricFireSound;
        private static GameObject electricRailBeamPrefab;
        private static bool searchedElectricAssets;

        internal static void SpawnExplosion(Vector3 position, float sizeMultiplier, float enemyDamage,
            GameObject sourceWeapon, float selfDamage = 0f)
        {
            DefaultReferenceManager drm = MonoSingleton<DefaultReferenceManager>.Instance;
            if (drm == null || drm.explosion == null)
                return;

            GameObject explosionObject = UnityEngine.Object.Instantiate(drm.explosion, position, Quaternion.identity);
            Explosion[] explosions = explosionObject.GetComponentsInChildren<Explosion>(true);
            foreach (Explosion explosion in explosions)
            {
                if (explosion == null)
                    continue;

                explosion.sourceWeapon = sourceWeapon;
                explosion.hitterWeapon = "ebstorm";
                explosion.maxSize *= sizeMultiplier;
                explosion.speed *= sizeMultiplier;
                explosion.enabled = true;
            }

            StormExplosionRuntime runtime = explosionObject.AddComponent<StormExplosionRuntime>();
            foreach (Explosion explosion in explosions)
                if (explosion != null && explosion.damage > 0 && !explosion.harmless)
                    runtime.authoredMechanicalIds.Add(explosion.GetInstanceID());
            runtime.enemyDamage = Mathf.Max(0f, enemyDamage);
            runtime.selfDamage = Mathf.Max(0f, selfDamage);
            runtime.sourceWeapon = sourceWeapon;

            foreach (Light light in explosionObject.GetComponentsInChildren<Light>(true))
                if (light != null) light.color = StormBlue;
            foreach (ParticleSystem particle in explosionObject.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (particle == null) continue;
                ParticleSystem.MainModule main = particle.main;
                main.startColor = StormBlue;
            }
        }

        internal static void SpawnLightning(Vector3 start, Vector3 end, float lifetime = 0.13f)
        {
            LineRenderer line = RuntimeVisuals.Lightning("Storm Lightning", start, end,
                new Color(0.25f, 0.82f, 1f, 0.95f), 0.13f, 9, 0.5f);
            if (line != null)
            {
                StormTimedVisual runtime = line.gameObject.AddComponent<StormTimedVisual>();
                runtime.lifetime = lifetime;
            }
        }

        internal static void SpawnFlashstepTrail(Vector3 start, Vector3 end)
        {
            ResolveElectricAssets();
            Vector3 delta = end - start;
            if (delta.sqrMagnitude < 0.001f)
                return;

            bool spawnedNativeRail = false;
            if (electricRailBeamPrefab != null)
            {
                Quaternion rotation = Quaternion.LookRotation(delta.normalized);
                GameObject beamObject = UnityEngine.Object.Instantiate(electricRailBeamPrefab, start, rotation);
                if (beamObject != null)
                {
                    // Configure the real Electric Railcannon beam as a fake visual before its Start
                    // method can execute a damaging shot.
                    beamObject.SetActive(false);
                    RevolverBeam beam = beamObject.GetComponent<RevolverBeam>();
                    if (beam != null)
                    {
                        beam.fake = true;
                        beam.noMuzzleflash = false;
                        beam.alternateStartPoint = start;
                        StormElectricRailVisual driver = beamObject.AddComponent<StormElectricRailVisual>();
                        driver.target = end;
                        beamObject.SetActive(true);
                        spawnedNativeRail = true;
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(beamObject);
                    }
                }
            }

            if (!spawnedNativeRail)
                SpawnLightning(start, end, 0.24f);

            // Screen-space feedback follows the camera instead of being a world sphere that the
            // player immediately leaves after teleporting.
            GameObject flash = new GameObject("Storm Flashstep Screen Flash");
            flash.hideFlags = HideFlags.HideAndDontSave;
            StormScreenFlash overlay = flash.AddComponent<StormScreenFlash>();
            overlay.lifetime = 0.16f;
            overlay.peakAlpha = 0.16f;
        }

        internal static void PlayElectricFireSound()
        {
            ResolveElectricAssets();
            if (electricFireSound != null)
                UnityEngine.Object.Instantiate(electricFireSound);
        }

        private static void ResolveElectricAssets()
        {
            if (searchedElectricAssets)
                return;
            searchedElectricAssets = true;
            foreach (Railcannon rail in Resources.FindObjectsOfTypeAll<Railcannon>())
            {
                if (rail == null || rail.variation != 0)
                    continue;
                if (electricFireSound == null && rail.fireSound != null)
                    electricFireSound = rail.fireSound;
                if (electricRailBeamPrefab == null && rail.beam != null)
                    electricRailBeamPrefab = rail.beam;
                if (electricFireSound != null && electricRailBeamPrefab != null)
                    break;
            }
        }
    }

    internal sealed class StormElectricRailVisual : MonoBehaviour
    {
        internal Vector3 target;
        private RevolverBeam beam;
        private bool placed;

        private void Awake()
        {
            beam = GetComponent<RevolverBeam>();
        }

        private void LateUpdate()
        {
            if (placed || beam == null)
                return;
            // RevolverBeam.Start has initialized the actual Electric Railcannon LineRenderer by
            // LateUpdate. FakeShoot places that authored effect without executing any damage.
            beam.FakeShoot(target);
            placed = true;
            Destroy(this);
        }
    }

    internal sealed class StormScreenFlash : MonoBehaviour
    {
        internal float lifetime = 0.16f;
        internal float peakAlpha = 0.16f;
        private float age;
        private Image overlayImage;

        private void Awake()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            GameObject imageObject = new GameObject("Flash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(transform, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            overlayImage = imageObject.GetComponent<Image>();
            overlayImage.raycastTarget = false;
            overlayImage.color = new Color(0.12f, 0.72f, 1f, peakAlpha);
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(age / Mathf.Max(0.01f, lifetime));
            if (overlayImage != null)
                overlayImage.color = new Color(0.12f, 0.72f, 1f, peakAlpha * (1f - t));
            if (age >= lifetime)
                Destroy(gameObject);
        }
    }

    internal sealed class StormTimedVisual : MonoBehaviour
    {
        internal float lifetime = 0.13f;
        private float age;

        private void Update()
        {
            age += Time.unscaledDeltaTime;
            if (age >= lifetime)
                Destroy(gameObject);
        }
    }

    internal readonly struct ElementalJackhammerHudState
    {
        internal readonly ElementId element;

        internal ElementalJackhammerHudState(ElementId element)
        {
            this.element = element;
        }
    }

    internal static class ElementalJackhammerHudRegistry
    {
        private sealed class Entry
        {
            internal ActiveWeaponController controller;
            internal ShotgunHammer hammer;
            internal ElementId element;
        }

        private static readonly List<Entry> Entries = new List<Entry>();

        internal static void Register(ActiveWeaponController controller, ShotgunHammer hammer, ElementId element)
        {
            if (controller == null || hammer == null)
                return;
            Unregister(controller);
            Entries.Add(new Entry { controller = controller, hammer = hammer, element = element });
        }

        internal static void Unregister(ActiveWeaponController controller)
        {
            if (controller == null)
                return;
            for (int i = Entries.Count - 1; i >= 0; i--)
                if (Entries[i] == null || Entries[i].controller == null || Entries[i].controller == controller)
                    Entries.RemoveAt(i);
        }

        internal static bool TryGet(ShotgunHammer hammer, out ElementalJackhammerHudState state)
        {
            state = default(ElementalJackhammerHudState);
            if (hammer == null)
                return false;
            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                Entry entry = Entries[i];
                if (entry == null || entry.controller == null || entry.hammer == null)
                {
                    Entries.RemoveAt(i);
                    continue;
                }
                if (entry.hammer != hammer)
                    continue;
                state = new ElementalJackhammerHudState(entry.element);
                return true;
            }
            return false;
        }

    }

    internal sealed class FlashstepJackhammerController : ActiveWeaponController
    {
        private ShotgunHammer hammer;
        private bool initialDrawFinished;
        private static readonly AccessTools.FieldRef<ShotgunHammer, bool> HammerGunReady =
            AccessTools.FieldRefAccess<ShotgunHammer, bool>("gunReady");

        protected override void Awake()
        {
            base.Awake();
            hammer = GetComponent<ShotgunHammer>();
            ElementalJackhammerHudRegistry.Register(this, hammer, ElementId.Storm);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            initialDrawFinished = false;
        }

        private void OnDestroy()
        {
            ElementalJackhammerHudRegistry.Unregister(this);
        }

        protected override void Update()
        {
            base.Update();
            if (hammer == null)
                return;
            if (!initialDrawFinished && HammerGunReady(hammer))
                initialDrawFinished = true;
            if (!CanUseSecondary() || !SecondaryInputTiming.AllowsInstantFreshPress(initialDrawFinished))
                return;
            TryFlashstep();
        }

        private void TryFlashstep()
        {
            NewMovement movement = MonoSingleton<NewMovement>.Instance;
            CameraController camera = MonoSingleton<CameraController>.Instance;
            if (movement == null || movement.rb == null || camera == null || movement.playerCollider == null)
                return;

            Vector3 aimDirection = camera.transform.forward.normalized;
            if (aimDirection.sqrMagnitude < 0.001f)
                return;

            CapsuleCollider collider = movement.playerCollider;
            Transform player = movement.transform;
            Vector3 scale = player.lossyScale;
            float radius = collider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float halfHeight = Mathf.Max(radius, collider.height * Mathf.Abs(scale.y) * 0.5f);
            Vector3 up = player.up.normalized;
            Vector3 center = player.TransformPoint(collider.center);
            Vector3 centerOffset = center - movement.rb.position;
            float segment = Mathf.Max(0f, halfHeight - radius);
            Vector3 p1 = center + up * segment;
            Vector3 p2 = center - up * segment;
            float castRadius = Mathf.Max(0.05f, radius * 0.92f);
            float maxDistance = Mathf.Max(0f, WeaponTuning.FlashstepDistance);
            int environmentMask = LayerMaskDefaults.Get(LMD.Environment);

            // Flashstep is crosshair-targeted. If the crosshair is on solid geometry within range,
            // place V1 flush against that exact surface. This is especially important on the floor:
            // a slightly downward aim should land at the bit of floor under the crosshair rather
            // than either cancelling at distance zero or flattening into a much longer horizontal dash.
            Vector3 cameraOrigin = camera.GetDefaultPos();
            bool hasAimSurface = Physics.Raycast(cameraOrigin, aimDirection, out RaycastHit aimHit,
                maxDistance, environmentMask, QueryTriggerInteraction.Ignore);

            Vector3 desiredPlayerPosition;
            bool aimedAtWalkableSurface = false;
            if (hasAimSurface)
            {
                Vector3 normal = aimHit.normal.sqrMagnitude > 0.001f ? aimHit.normal.normalized : -aimDirection;
                float upDot = Vector3.Dot(normal, up);
                aimedAtWalkableSurface = upDot > 0.45f;
                Vector3 desiredCenter;
                if (aimedAtWalkableSurface)
                {
                    // Keep the horizontal destination exactly under the crosshair and place the
                    // capsule bottom just above the floor/slope.
                    desiredCenter = aimHit.point + up * (halfHeight + 0.035f);
                }
                else
                {
                    // Capsule support distance along an arbitrary wall/ceiling normal.
                    float support = radius + segment * Mathf.Abs(Vector3.Dot(normal, up));
                    desiredCenter = aimHit.point + normal * (support + 0.035f);
                }
                desiredPlayerPosition = desiredCenter - centerOffset;
            }
            else
            {
                desiredPlayerPosition = movement.rb.position + aimDirection * maxDistance;
            }

            Vector3 displacement = desiredPlayerPosition - movement.rb.position;
            float distance = displacement.magnitude;
            if (distance <= 0.05f)
                return;
            Vector3 travelDirection = displacement / distance;

            // Sweep the player's real capsule to the computed destination. When aiming at the same
            // floor V1 is already standing on, ignore only that floor's upward-facing zero-distance
            // contacts; walls and all other blockers still shorten the teleport normally.
            float safeDistance = distance;
            RaycastHit[] blockers = Physics.CapsuleCastAll(p1, p2, castRadius, travelDirection, distance,
                environmentMask, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit blocker in blockers)
            {
                if (blocker.collider == null)
                    continue;
                if (hasAimSurface && aimedAtWalkableSurface && blocker.collider == aimHit.collider &&
                    blocker.distance <= 0.12f &&
                    (blocker.normal.sqrMagnitude < 0.001f || Vector3.Dot(blocker.normal, up) > 0.25f))
                    continue;
                safeDistance = Mathf.Min(safeDistance, Mathf.Max(0f, blocker.distance - 0.06f));
            }
            if (safeDistance <= 0.05f)
                return;

            if (safeDistance < distance)
            {
                distance = safeDistance;
                desiredPlayerPosition = movement.rb.position + travelDirection * distance;
            }

            DamageEnemiesPassedThrough(p1, p2, radius, travelDirection, distance);

            Vector3 velocity = movement.rb.velocity;
            Vector3 oldCameraPosition = camera.GetDefaultPos();
            Vector3 oldPlayerPosition = movement.rb.position;

            // Mark the player airborne so a vertical/upward Flashstep is not immediately flattened
            // by the grounded movement state. Launch may touch velocity, so restore the exact value
            // immediately afterward; Flashstep changes position, not momentum.
            movement.Launch(travelDirection, 0.01f, true);
            movement.rb.position = desiredPlayerPosition;
            movement.transform.position = desiredPlayerPosition;
            movement.rb.velocity = velocity;
            Physics.SyncTransforms();

            Vector3 actualOffset = desiredPlayerPosition - oldPlayerPosition;
            Vector3 newCameraPosition = oldCameraPosition + actualOffset;
            StormEffects.SpawnFlashstepTrail(oldCameraPosition, newCameraPosition);
            StormEffects.PlayElectricFireSound();
            camera.CameraShake(0.18f);
            if (initialDrawFinished)
                GetComponentInChildren<Animator>()?.Play("Fire", -1, 0f);
            MonoSingleton<PlayerAnimations>.Instance?.Shoot();
            StartCooldown(WeaponTuning.FlashstepCooldown);
        }

        private void DamageEnemiesPassedThrough(Vector3 p1, Vector3 p2, float radius, Vector3 direction, float distance)
        {
            float damage = Mathf.Max(0f, WeaponTuning.FlashstepContactDamage);
            if (damage <= 0f || distance <= 0f)
                return;

            HashSet<EnemyIdentifier> victims = new HashSet<EnemyIdentifier>();
            RaycastHit[] hits = Physics.CapsuleCastAll(p1, p2, Mathf.Max(0.05f, radius * 0.92f), direction, distance,
                LayerMaskDefaults.Get(LMD.Enemies), QueryTriggerInteraction.Collide);
            foreach (RaycastHit hit in hits)
                AddVictim(hit.collider, victims);
            foreach (Collider overlap in Physics.OverlapCapsule(p1, p2, Mathf.Max(0.05f, radius * 0.92f),
                LayerMaskDefaults.Get(LMD.Enemies), QueryTriggerInteraction.Collide))
                AddVictim(overlap, victims);

            foreach (EnemyIdentifier enemy in victims)
            {
                if (enemy == null || enemy.dead)
                    continue;
                EnemyIdentifierIdentifier eidid = enemy.GetComponentInChildren<EnemyIdentifierIdentifier>();
                GameObject target = eidid != null ? eidid.gameObject : enemy.gameObject;
                enemy.hitter = "ebflashstep";
                // Use ULTRAKILL's native electric-hit attribute. EnemyIdentifier consumes this
                // during DeliverDamage, which is the same path used by Electric Railcannon hits
                // to trigger Conduction/aftershock from nails, magnets, and water.
                enemy.hitterAttributes.Add(HitterAttribute.Electricity);
                enemy.DeliverDamage(target, direction * 600f, enemy.bodyTransform.position, damage, false, 0f, gameObject);
            }
        }

        private static void AddVictim(Collider collider, HashSet<EnemyIdentifier> victims)
        {
            if (collider == null)
                return;
            EnemyIdentifierIdentifier eidid = collider.GetComponentInParent<EnemyIdentifierIdentifier>();
            EnemyIdentifier enemy = eidid != null ? eidid.eid : collider.GetComponentInParent<EnemyIdentifier>();
            if (enemy != null && !enemy.dead)
                victims.Add(enemy);
        }
    }

    [DefaultExecutionOrder(10000)]
    internal sealed class StormJumpstartScreenColorController : MonoBehaviour
    {
        private static readonly AccessTools.FieldRef<Nailgun, TMP_Text> StatusText =
            AccessTools.FieldRefAccess<Nailgun, TMP_Text>("statusText");
        private static readonly AccessTools.FieldRef<Nailgun, Slider> DistanceMeter =
            AccessTools.FieldRefAccess<Nailgun, Slider>("distanceMeter");
        private static readonly AccessTools.FieldRef<Nailgun, Slider> ZapMeter =
            AccessTools.FieldRefAccess<Nailgun, Slider>("zapMeter");
        private static readonly AccessTools.FieldRef<Nailgun, Image> WarningX =
            AccessTools.FieldRefAccess<Nailgun, Image>("warningX");
        private static readonly AccessTools.FieldRef<Nailgun, Image> RechargingMeter =
            AccessTools.FieldRefAccess<Nailgun, Image>("rechargingMeter");

        private Nailgun nailgun;

        private void Awake()
        {
            nailgun = GetComponent<Nailgun>();
        }

        private void LateUpdate()
        {
            if (nailgun == null || nailgun.variation != 2)
                return;

            Color accent = ElementPalette.Accent(ElementId.Storm);
            Slider distance = DistanceMeter(nailgun);
            Slider zap = ZapMeter(nailgun);
            TMP_Text status = StatusText(nailgun);
            Image warning = WarningX(nailgun);
            Image recharge = RechargingMeter(nailgun);

            if (zap != null)
            {
                SetImage(zap.transform.Find("Background"), accent);
                if (zap.fillRect != null)
                    SetImage(zap.fillRect, Shade(accent, 0.72f, 1f));
            }
            if (distance != null)
            {
                if (distance.handleRect != null)
                    SetImage(distance.handleRect, accent);
                if (distance.fillRect != null)
                    SetImage(distance.fillRect, Shade(accent, 0.50f, 1f));
                SetImage(distance.transform.Find("Handle (3)"), Shade(accent, 0.40f, 1f));
                SetImage(distance.transform.Find("Handle (2)"), Shade(accent, 0.40f, 0.50f));
            }
            if (recharge != null)
                recharge.color = accent;

            if (warning != null && warning.enabled)
            {
                // Preserve the native white half of the warning blink; replace only its red half.
                bool whiteFrame = warning.color.r > 0.92f && warning.color.g > 0.92f && warning.color.b > 0.92f;
                warning.color = whiteFrame ? Color.white : accent;
            }

            if (status != null)
            {
                string text = status.text ?? string.Empty;
                if (text == "TOO FAR" || text == "OUT OF RANGE" || text == "BLOCKED")
                    status.color = accent;
                else if (text == "READY")
                    status.color = Color.white;
                else if (text == "NULL" || text == "NO TARGET" || text.Length == 0)
                {
                    // Leave native gray/blank text alone.
                }
                else if (distance != null)
                    status.color = Color.Lerp(accent, Color.white, 1f - Mathf.Clamp01(distance.value));
            }
        }

        private static void SetImage(Transform transform, Color color)
        {
            if (transform == null)
                return;
            Image image = transform.GetComponent<Image>();
            if (image != null)
                image.color = new Color(color.r, color.g, color.b, image.color.a);
        }

        private static Color Shade(Color source, float valueMultiplier, float saturationMultiplier)
        {
            Color.RGBToHSV(source, out float h, out float s, out float v);
            Color result = Color.HSVToRGB(h, Mathf.Clamp01(s * saturationMultiplier), Mathf.Clamp01(v * valueMultiplier));
            result.a = source.a;
            return result;
        }
    }

    internal sealed class ThunderlineSequenceRuntime : MonoBehaviour
    {
        private Vector3 origin;
        private Vector3 up;
        private Vector3 direction;
        private GameObject sourceWeapon;

        internal static void Begin(Vector3 origin, Vector3 up, Vector3 direction, GameObject sourceWeapon)
        {
            GameObject host = new GameObject("EB Thunderline Sequence");
            host.hideFlags = HideFlags.HideAndDontSave;
            ThunderlineSequenceRuntime runtime = host.AddComponent<ThunderlineSequenceRuntime>();
            runtime.origin = origin;
            runtime.up = up;
            runtime.direction = direction;
            runtime.sourceWeapon = sourceWeapon;
            runtime.StartCoroutine(runtime.FireLine());
        }

        private IEnumerator FireLine()
        {
            int strikeCount = Mathf.Clamp(WeaponTuning.ThunderlineStrikeCount, 1, 32);
            for (int i = 0; i < strikeCount; i++)
            {
                Vector3 candidate = origin + direction * (WeaponTuning.ThunderlineFirstDistance + WeaponTuning.ThunderlineSpacing * i);
                Vector3 destination = ResolveGroundDestination(candidate);
                Strike(destination);
                if (i + 1 < strikeCount)
                    yield return new WaitForSeconds(WeaponTuning.ThunderlineStrikeInterval);
            }
            Destroy(gameObject);
        }

        private Vector3 ResolveGroundDestination(Vector3 candidate)
        {
            Vector3 start = candidate + up * 1.5f;
            if (Physics.Raycast(start, -up, out RaycastHit ground, WeaponTuning.ThunderlineGroundSearchDistance,
                LayerMaskDefaults.Get(LMD.Environment), QueryTriggerInteraction.Ignore))
                return ground.point;
            return candidate;
        }

        private void Strike(Vector3 destination)
        {
            float playerHeightAboveDestination = Mathf.Max(0f, Vector3.Dot(origin - destination, up));
            Vector3 sky = destination + up * (WeaponTuning.ThunderlineStrikeHeight + playerHeightAboveDestination);
            Vector3 impact = destination;
            float path = Vector3.Distance(sky, destination);
            if (Physics.SphereCast(sky, 0.55f, -up, out RaycastHit enemyHit, path,
                LayerMaskDefaults.Get(LMD.Enemies), QueryTriggerInteraction.Collide))
                impact = enemyHit.point;

            StormEffects.SpawnLightning(sky, impact);
            StormEffects.SpawnExplosion(impact, WeaponTuning.ThunderlineExplosionScale,
                WeaponTuning.ThunderlineDamage, sourceWeapon, WeaponTuning.ThunderlineSelfDamage);
        }
    }

    internal sealed class ThunderlineLauncherController : ActiveWeaponController
    {
        private RocketLauncher launcher;
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
            GrenadeLauncherCompat.SyncExternalSecondaryCooldown(launcher, CooldownReadyFraction);
            bool drawFinished = launcher != null && (float)SinceEquipped(launcher) >= NativeDrawDelay;
            if (!CanUseSecondary() || !SecondaryInputTiming.AllowsInstantFreshPress(drawFinished))
                return;
            FireLine();
        }

        private void FireLine()
        {
            NewMovement movement = MonoSingleton<NewMovement>.Instance;
            CameraController camera = MonoSingleton<CameraController>.Instance;
            if (movement == null || camera == null)
                return;

            StartCooldown(WeaponTuning.ThunderlineCooldown);
            GrenadeLauncherCompat.PlayExternalSecondaryAnimation(launcher);
            MonoSingleton<PlayerAnimations>.Instance?.Shoot();
            StormEffects.PlayElectricFireSound();

            Vector3 up = movement.transform.up.normalized;
            Vector3 direction = Vector3.ProjectOnPlane(camera.transform.forward, up).normalized;
            if (direction.sqrMagnitude < 0.01f)
                direction = Vector3.ProjectOnPlane(movement.transform.forward, up).normalized;
            if (direction.sqrMagnitude < 0.01f)
                direction = Vector3.forward;

            ThunderlineSequenceRuntime.Begin(movement.transform.position, up, direction, gameObject);
        }
    }

}
