using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBattlegroundsMod
{
    internal static class RuntimeRegistry
    {
        internal static readonly HashSet<SporeCloudRuntime> SporeClouds = new HashSet<SporeCloudRuntime>();
        internal static float CounterBuffUntil;
        internal static float FortifyUntil;
        internal static bool FreezeRailCharge;
        internal static bool InfernoActive;
        internal static int CurrentAttackId;
        private static int nextAttackId = 1;
        internal static bool CounterBuffActive => Time.time < CounterBuffUntil;
        internal static bool FortifyActive => Time.time < FortifyUntil;

        internal static void GrantCounterBuff(float seconds)
        {
            GrantElementalParryBuff(ElementId.Fire, seconds);
        }

        internal static void GrantElementalParryBuff(ElementId element, float seconds)
        {
            float now = Time.time;
            // A true elemental parry refreshes every OTHER parry buff that was already earned,
            // but never grants a buff the player did not already have.
            if (element != ElementId.Fire && CounterBuffUntil > now)
                CounterBuffUntil = now + WeaponTuning.CounterBuffDuration;
            if (element != ElementId.Earth && FortifyUntil > now)
                FortifyUntil = now + WeaponTuning.FortifyDuration;

            if (element == ElementId.Fire)
                CounterBuffUntil = Mathf.Max(CounterBuffUntil, now + Mathf.Max(0f, seconds));
            else if (element == ElementId.Earth)
                FortifyUntil = Mathf.Max(FortifyUntil, now + Mathf.Max(0f, seconds));
        }

        internal static int NewAttackId()
        {
            if (nextAttackId == int.MaxValue)
                nextAttackId = 1;
            return nextAttackId++;
        }

        internal static AttackScope BeginAttack()
        {
            return new AttackScope(NewAttackId());
        }

        internal static AttackScope BeginAttack(int attackId)
        {
            return new AttackScope(attackId);
        }

        internal static void IgniteSporeClouds(Vector3 position, float radius, GameObject sourceWeapon)
        {
            // Avoid allocating a copy on every projectile/explosion check. Ignite removes the
            // cloud from this registry immediately, so selecting one candidate at a time is safe
            // even when the resulting explosion recursively checks nearby clouds.
            while (SporeClouds.Count > 0)
            {
                SporeCloudRuntime candidate = null;
                foreach (SporeCloudRuntime cloud in SporeClouds)
                {
                    if (cloud != null && Vector3.Distance(position, cloud.transform.position) <= radius)
                    {
                        candidate = cloud;
                        break;
                    }
                }
                if (candidate == null)
                    break;
                candidate.Ignite(sourceWeapon);
            }
        }

        internal static void IgniteSporeCloudsAlongRay(Vector3 origin, Vector3 direction, float range, float width, GameObject sourceWeapon)
        {
            direction = direction.normalized;
            while (SporeClouds.Count > 0)
            {
                SporeCloudRuntime candidate = null;
                foreach (SporeCloudRuntime cloud in SporeClouds)
                {
                    if (cloud == null) continue;
                    Vector3 to = cloud.transform.position - origin;
                    float along = Vector3.Dot(to, direction);
                    if (along < 0f || along > range) continue;
                    Vector3 closest = origin + direction * along;
                    if (Vector3.Distance(closest, cloud.transform.position) <= width + 6f)
                    {
                        candidate = cloud;
                        break;
                    }
                }
                if (candidate == null)
                    break;
                candidate.Ignite(sourceWeapon);
            }
        }

        internal static void ResetPlayerTransientState()
        {
            // Do not clear SporeClouds here: live world effects can survive a player respawn.
            CounterBuffUntil = 0f;
            FortifyUntil = 0f;
            FreezeRailCharge = false;
            InfernoActive = false;
            CurrentAttackId = 0;
        }

        internal static void Clear()
        {
            SporeClouds.Clear();
            ResetPlayerTransientState();
        }
    }

    internal readonly struct AttackScope : IDisposable
    {
        private readonly int previous;

        internal AttackScope(int attackId)
        {
            previous = RuntimeRegistry.CurrentAttackId;
            RuntimeRegistry.CurrentAttackId = attackId;
        }

        public void Dispose()
        {
            RuntimeRegistry.CurrentAttackId = previous;
        }
    }

    internal static class CooldownReadyAudio
    {
        private static AudioClip cachedClip;

        internal static void Play(GameObject weapon)
        {
            if (weapon == null || !weapon.activeInHierarchy)
                return;

            GunControl gc = MonoSingleton<GunControl>.Instance;
            if (gc == null || gc.currentWeapon != weapon)
                return;

            AudioClip clip = FindClip(weapon);
            if (clip == null)
                return;

            GameObject host = new GameObject("EB Local Cooldown Ready SFX");
            host.hideFlags = HideFlags.HideAndDontSave;
            host.transform.SetParent(weapon.transform, false);

            AudioSource audio = host.AddComponent<AudioSource>();
            AudioSource template = weapon.GetComponent<AudioSource>();
            if (template != null)
            {
                audio.outputAudioMixerGroup = template.outputAudioMixerGroup;
                audio.ignoreListenerPause = template.ignoreListenerPause;
            }
            audio.playOnAwake = false;
            audio.loop = false;
            audio.spatialBlend = 0f;
            audio.clip = clip;
            audio.volume = 0.35f;
            float pitch = UnityEngine.Random.Range(1f, 1.1f);
            audio.SetPitch(pitch);
            audio.Play(tracked: true);
            UnityEngine.Object.Destroy(host, Mathf.Max(0.25f, clip.length / Mathf.Max(0.01f, pitch) + 0.1f));
        }

        private static AudioClip FindClip(GameObject weapon)
        {
            Revolver ownRevolver = weapon.GetComponent<Revolver>();
            if (ownRevolver != null && ownRevolver.chargedSound != null)
                return ownRevolver.chargedSound;

            if (cachedClip != null)
                return cachedClip;

            foreach (Revolver revolver in Resources.FindObjectsOfTypeAll<Revolver>())
            {
                if (revolver != null && revolver.chargedSound != null)
                {
                    cachedClip = revolver.chargedSound;
                    return cachedClip;
                }
            }
            return null;
        }
    }


    internal static class RuntimeAudio
    {
        private static readonly AccessTools.FieldRef<NewMovement, AudioSource> MovementAudio =
            AccessTools.FieldRefAccess<NewMovement, AudioSource>("aud");
        private static AudioClip boostPadClip;

        internal static void PlayWaterDash(NewMovement movement)
        {
            if (movement == null || movement.dodgeSound == null)
                return;
            AudioSource audio = MovementAudio(movement);
            if (audio == null)
                return;
            // These are the same values used by NewMovement.TryDash.
            audio.clip = movement.dodgeSound;
            audio.volume = 1f;
            audio.SetPitch(1f);
            audio.Play(tracked: true);
            MonoSingleton<RumbleManager>.Instance?.SetVibration(RumbleProperties.Dash);
        }

        internal static void PlayBoostPadLaunch(NewMovement movement)
        {
            if (movement == null)
                return;
            AudioClip clip = FindBoostPadClip();
            if (clip == null)
                return;

            GameObject host = new GameObject("EB Geyser Boost SFX");
            host.hideFlags = HideFlags.HideAndDontSave;
            host.transform.SetParent(movement.transform, false);
            AudioSource source = host.AddComponent<AudioSource>();
            AudioSource template = MovementAudio(movement);
            if (template != null)
            {
                source.outputAudioMixerGroup = template.outputAudioMixerGroup;
                source.ignoreListenerPause = template.ignoreListenerPause;
            }
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.clip = clip;
            source.volume = 0.8f;
            float pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            source.SetPitch(pitch);
            source.Play(tracked: true);
            UnityEngine.Object.Destroy(host, Mathf.Max(0.25f, clip.length / Mathf.Max(0.01f, pitch) + 0.1f));
        }

        private static AudioClip FindBoostPadClip()
        {
            if (boostPadClip != null)
                return boostPadClip;
            foreach (JumpPad pad in Resources.FindObjectsOfTypeAll<JumpPad>())
            {
                if (pad != null && pad.launchSound != null)
                {
                    boostPadClip = pad.launchSound;
                    return boostPadClip;
                }
            }
            return null;
        }


        internal static void PlaySawedOnLaunch(Shotgun shotgun)
        {
            if (shotgun == null || shotgun.grenadeSoundBubble == null)
                return;
            GameObject bubble = UnityEngine.Object.Instantiate(shotgun.grenadeSoundBubble);
            AudioSource source = bubble != null ? bubble.GetComponent<AudioSource>() : null;
            if (source != null)
            {
                source.volume = Mathf.Max(source.volume, 0.45f);
                source.SetPitch(UnityEngine.Random.Range(0.9f, 1.05f));
            }
        }

        internal static void PlayJumpstartCable(Nailgun nailgun)
        {
            if (nailgun == null || nailgun.magnetShotSound == null)
                return;
            AudioSource source = UnityEngine.Object.Instantiate(nailgun.magnetShotSound);
            if (source != null)
                source.SetPitch(UnityEngine.Random.Range(0.95f, 1.05f));
        }

    }

    internal sealed class GlobalCombatRuntime : MonoBehaviour
    {
        private ElementalParryBuffHud buffHud;

        private void Awake()
        {
            buffHud = gameObject.AddComponent<ElementalParryBuffHud>();
        }

        private void Update()
        {
            if (RuntimeRegistry.CounterBuffUntil > 0f && Time.time >= RuntimeRegistry.CounterBuffUntil)
                RuntimeRegistry.CounterBuffUntil = 0f;
            if (RuntimeRegistry.FortifyUntil > 0f && Time.time >= RuntimeRegistry.FortifyUntil)
                RuntimeRegistry.FortifyUntil = 0f;
        }
    }

    /// <summary>
    /// Elemental parry buffs intentionally reuse the visual language of ULTRAKILL's native
    /// PowerUpMeter ring. Each simultaneous buff is placed on a larger concentric ring so the
    /// timers stay readable, and an active vanilla powerup reserves the innermost ring.
    /// </summary>
    internal sealed class ElementalParryBuffHud : MonoBehaviour
    {
        private Image fireRing;
        private Image earthRing;
        private Image template;
        private RectTransform templateRect;
        private const float RingStep = 12f;

        private void Update()
        {
            PowerUpMeter meter = MonoSingleton<PowerUpMeter>.Instance;
            if (meter == null)
            {
                HideRings();
                return;
            }
            if (template == null)
            {
                template = meter.GetComponent<Image>();
                templateRect = template != null ? template.rectTransform : null;
                if (template == null || templateRect == null)
                    return;
            }

            bool fire = RuntimeRegistry.CounterBuffActive;
            bool earth = RuntimeRegistry.FortifyActive;
            int ordinal = meter.juice > 0f ? 1 : 0;
            if (fire)
            {
                EnsureRing(ref fireRing, "EB Combustion Ring");
                UpdateRing(fireRing, ElementPalette.Accent(ElementId.Fire),
                    Mathf.Clamp01((RuntimeRegistry.CounterBuffUntil - Time.time) / Mathf.Max(0.01f, WeaponTuning.CounterBuffDuration)), ordinal++);
            }
            else if (fireRing != null)
                fireRing.gameObject.SetActive(false);

            if (earth)
            {
                EnsureRing(ref earthRing, "EB Fortify Ring");
                UpdateRing(earthRing, ElementPalette.Accent(ElementId.Earth),
                    Mathf.Clamp01((RuntimeRegistry.FortifyUntil - Time.time) / Mathf.Max(0.01f, WeaponTuning.FortifyDuration)), ordinal++);
            }
            else if (earthRing != null)
                earthRing.gameObject.SetActive(false);
        }

        private void EnsureRing(ref Image ring, string name)
        {
            if (ring != null || template == null || templateRect == null)
                return;
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(template.transform.parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = templateRect.anchorMin;
            rt.anchorMax = templateRect.anchorMax;
            rt.pivot = templateRect.pivot;
            rt.anchoredPosition = templateRect.anchoredPosition;
            rt.localRotation = templateRect.localRotation;
            rt.localScale = templateRect.localScale;
            rt.sizeDelta = templateRect.sizeDelta;
            ring = obj.GetComponent<Image>();
            ring.sprite = template.sprite;
            ring.material = template.material;
            ring.type = template.type;
            ring.fillMethod = template.fillMethod;
            ring.fillOrigin = template.fillOrigin;
            ring.fillClockwise = template.fillClockwise;
            ring.preserveAspect = template.preserveAspect;
            ring.pixelsPerUnitMultiplier = template.pixelsPerUnitMultiplier;
            ring.raycastTarget = false;
        }

        private void UpdateRing(Image ring, Color color, float fill, int ordinal)
        {
            if (ring == null || templateRect == null) return;
            ring.gameObject.SetActive(!ULTRAKILL.Cheats.HideUI.Active);
            color.a = template != null ? template.color.a : 1f;
            ring.color = color;
            ring.fillAmount = fill;
            ring.rectTransform.sizeDelta = templateRect.sizeDelta + Vector2.one * (RingStep * ordinal);
            ring.rectTransform.anchoredPosition = templateRect.anchoredPosition;
        }

        private void HideRings()
        {
            if (fireRing != null) fireRing.gameObject.SetActive(false);
            if (earthRing != null) earthRing.gameObject.SetActive(false);
        }
    }

    internal sealed class FireTrailCloudIgniter : MonoBehaviour
    {
        public GameObject sourceWeapon;
        public float radius = 1f;

        private void Update()
        {
            if (sourceWeapon == null)
            {
                Destroy(this);
                return;
            }
            RuntimeRegistry.IgniteSporeClouds(transform.position, radius, sourceWeapon);
        }
    }


    internal sealed class CounterBuffWeaponVisual : MonoBehaviour
    {
        private readonly List<GameObject> fires = new List<GameObject>();
        private readonly List<Renderer> visibleRenderers = new List<Renderer>();
        private Renderer[] renderers;
        private ElementalSlotMarker marker;
        private Revolver revolver;

        private void Awake()
        {
            marker = GetComponent<ElementalSlotMarker>();
            revolver = GetComponent<Revolver>();
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void Update()
        {
            if (!RuntimeRegistry.CounterBuffActive)
            {
                ClearFires();
                return;
            }
            EnsureFires();
            BuildVisibleRendererList();
            CameraController cc = MonoSingleton<CameraController>.Instance;
            Vector3 up = cc != null ? cc.transform.up : Vector3.up;
            Vector3 forward = cc != null ? cc.transform.forward : Vector3.forward;
            bool slabRevolver = marker != null && marker.family == WeaponFamily.Revolver && revolver != null && revolver.altVersion;
            bool standardShotgun = marker != null && marker.family == WeaponFamily.Shotgun && GetComponent<Shotgun>() != null;

            for (int i = 0; i < fires.Count; i++)
            {
                GameObject fire = fires[i];
                if (fire == null) continue;
                if (i >= visibleRenderers.Count)
                {
                    fire.SetActive(false);
                    continue;
                }
                fire.SetActive(true);
                Renderer renderer = visibleRenderers[i];
                Bounds b = renderer.bounds;
                float along = visibleRenderers.Count <= 1 ? 0f : ((float)i / (visibleRenderers.Count - 1) - 0.5f);
                float lift = Mathf.Max(0.025f, b.extents.magnitude * (standardShotgun ? 0.36f : 0.20f));
                fire.transform.position = b.center + up * lift + forward * 0.015f + renderer.transform.right * along * b.extents.x * 0.6f;
                // The original buff flame size was already readable on most weapons. Keep that
                // baseline everywhere and only apply the requested +50% size to slab revolvers.
                // Standard shotguns are made visible by lifting the flames, not enlarging them.
                float scale = slabRevolver ? 0.21f : 0.14f;
                fire.transform.localScale = Vector3.one * scale;
            }
        }

        private void BuildVisibleRendererList()
        {
            visibleRenderers.Clear();
            if (renderers == null) return;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || renderer is ParticleSystemRenderer) continue;
                if (renderer.bounds.extents.sqrMagnitude < 0.00005f) continue;
                visibleRenderers.Add(renderer);
            }
            visibleRenderers.Sort((a, b) => b.bounds.extents.sqrMagnitude.CompareTo(a.bounds.extents.sqrMagnitude));
            if (visibleRenderers.Count > fires.Count)
                visibleRenderers.RemoveRange(fires.Count, visibleRenderers.Count - fires.Count);
        }

        private void EnsureFires()
        {
            if (fires.Count > 0 || MonoSingleton<FireObjectPool>.Instance == null) return;
            int count = marker != null && marker.family == WeaponFamily.Shotgun ? 4 : 3;
            for (int i = 0; i < count; i++)
            {
                GameObject fire = MonoSingleton<FireObjectPool>.Instance.GetFire(true);
                fire.transform.SetParent(null);
                fire.transform.localScale = Vector3.one * 0.14f;
                fires.Add(fire);
            }
        }

        private void OnDisable() => ClearFires();
        private void OnDestroy() => ClearFires();

        private void ClearFires()
        {
            if (fires.Count == 0) return;
            FireObjectPool pool = MonoSingleton<FireObjectPool>.Instance;
            foreach (GameObject fire in fires)
                if (fire != null && pool != null) pool.ReturnFire(fire, true);
            fires.Clear();
        }
    }

    internal static class ElementPalette
    {
        internal static Color Accent(ElementId element) => ElementCatalog.Accent(element);
        internal static GunColorPreset GunColors(ElementId element) => ElementCatalog.GunColors(element);

        internal static Color Accent(string stableId, ElementId builtInFallback)
        {
            if (!string.IsNullOrEmpty(stableId) && ElementRegistry.TryGet(stableId, out ElementRegistryRecord record) && record.external)
                return record.accent;
            return Accent(builtInFallback);
        }

        internal static GunColorPreset GunColors(string stableId, ElementId builtInFallback)
        {
            if (!string.IsNullOrEmpty(stableId) && ElementRegistry.TryGet(stableId, out ElementRegistryRecord record) && record.external)
                return record.gunColors;
            return GunColors(builtInFallback);
        }
    }

    internal sealed class ElementalColorDriver : MonoBehaviour
    {
        private ElementalSlotMarker marker;
        private GunColorGetter[] colorGetters;
        private ColorBlindGet[] variationGetters;
        private Railcannon rail;
        private Revolver revolver;
        private MaterialPropertyBlock block;
        private float nextRefresh;

        private static readonly AccessTools.FieldRef<GunColorGetter, Renderer> GunColorRenderer =
            AccessTools.FieldRefAccess<GunColorGetter, Renderer>("rend");
        private static readonly AccessTools.FieldRef<GunColorGetter, Material[]> GunColorColoredMaterials =
            AccessTools.FieldRefAccess<GunColorGetter, Material[]>("coloredMaterials");
        private static readonly AccessTools.FieldRef<Railcannon, SkinnedMeshRenderer[]> RailPips =
            AccessTools.FieldRefAccess<Railcannon, SkinnedMeshRenderer[]>("pips");
        private static readonly AccessTools.FieldRef<Railcannon, SkinnedMeshRenderer> RailBody =
            AccessTools.FieldRefAccess<Railcannon, SkinnedMeshRenderer>("body");
        private static readonly AccessTools.FieldRef<Revolver, MeshRenderer> RevolverScreen =
            AccessTools.FieldRefAccess<Revolver, MeshRenderer>("screenMR");

        private void Awake()
        {
            marker = GetComponent<ElementalSlotMarker>();
            block = new MaterialPropertyBlock();
            Refresh();
        }

        private void Refresh()
        {
            colorGetters = GetComponentsInChildren<GunColorGetter>(true);
            variationGetters = GetComponentsInChildren<ColorBlindGet>(true);
            rail = GetComponent<Railcannon>();
            revolver = GetComponent<Revolver>();
            nextRefresh = Time.unscaledTime + 2f;
        }

        private void LateUpdate()
        {
            ApplyNow();
        }

        internal void ApplyNow()
        {
            if (marker == null || !marker.IsCustom)
                return;
            if (Time.unscaledTime >= nextRefresh)
                Refresh();

            GunColorPreset colors = ElementPalette.GunColors(marker.elementId, marker.element);
            Color accent = ElementPalette.Accent(marker.elementId, marker.element);

            if (colorGetters != null)
            {
                foreach (GunColorGetter getter in colorGetters)
                {
                    if (getter == null)
                        continue;
                    Renderer renderer = GunColorRenderer(getter);
                    if (renderer == null)
                        renderer = getter.GetComponent<Renderer>();
                    Material[] colored = GunColorColoredMaterials(getter);
                    if (renderer == null)
                        continue;
                    if (colored != null && colored.Length > 0)
                        renderer.materials = colored;
                    renderer.GetPropertyBlock(block);
                    block.SetColor("_CustomColor1", colors.color1);
                    block.SetColor("_CustomColor2", colors.color2);
                    block.SetColor("_CustomColor3", colors.color3);
                    renderer.SetPropertyBlock(block);
                }
            }

            if (variationGetters != null)
            {
                foreach (ColorBlindGet getter in variationGetters)
                {
                    if (getter == null || !getter.variationColor)
                        continue;
                    Image image = getter.GetComponent<Image>();
                    if (image != null) image.color = new Color(accent.r, accent.g, accent.b, image.color.a);
                    SpriteRenderer sprite = getter.GetComponent<SpriteRenderer>();
                    if (sprite != null) sprite.color = new Color(accent.r, accent.g, accent.b, sprite.color.a);
                    Light light = getter.GetComponent<Light>();
                    if (light != null) light.color = accent;
                }
            }

            if (rail != null)
            {
                SkinnedMeshRenderer body = RailBody(rail);
                if (body != null)
                {
                    body.GetPropertyBlock(block);
                    block.SetColor("_EmissiveColor", accent);
                    body.SetPropertyBlock(block);
                }
                SkinnedMeshRenderer[] pips = RailPips(rail);
                if (pips != null)
                {
                    foreach (SkinnedMeshRenderer pip in pips)
                    {
                        if (pip == null) continue;
                        pip.GetPropertyBlock(block);
                        block.SetColor("_EmissiveColor", accent);
                        pip.SetPropertyBlock(block);
                    }
                }
            }

            if (revolver != null)
            {
                MeshRenderer screen = RevolverScreen(revolver);
                if (screen != null)
                {
                    screen.GetPropertyBlock(block);
                    block.SetColor("_Color", accent);
                    screen.SetPropertyBlock(block);
                }
            }

            Shotgun shotgun = GetComponent<Shotgun>();
            if (shotgun != null && shotgun.sliderFill != null)
                shotgun.sliderFill.color = accent;

            Nailgun nailgun = GetComponent<Nailgun>();
            if (nailgun != null && nailgun.heatSinkImages != null)
            {
                foreach (Image image in nailgun.heatSinkImages)
                    if (image != null) image.color = accent;
                if (nailgun.ammoText != null) nailgun.ammoText.color = accent;
            }

            WeaponIcon icon = GetComponentInChildren<WeaponIcon>(true);
            if (icon != null && icon.isActiveAndEnabled && MonoSingleton<WeaponHUD>.Instance != null)
            {
                Image hud = MonoSingleton<WeaponHUD>.Instance.GetComponent<Image>();
                if (hud != null) hud.color = accent;
                if (MonoSingleton<WeaponHUD>.Instance.transform.childCount > 0)
                {
                    Image glow = MonoSingleton<WeaponHUD>.Instance.transform.GetChild(0).GetComponent<Image>();
                    if (glow != null) glow.color = accent;
                }
            }
        }
    }
}

namespace ElementalBattlegroundsMod
{
    /// <summary>
    /// Keeps a logical 15-position loadout even when the player chooses None. GunControl requires
    /// a non-null GameObject at a selectable variation index, but it does not require that object
    /// to be a real weapon. Empty positions therefore use a deliberately bare placeholder instead
    /// of cloning and then trying to disable a vanilla gun.
    /// </summary>
    internal static class EmptySlotRuntime
    {
        internal static GameObject CreatePlaceholder(Transform parent)
        {
            GameObject placeholder = new GameObject("EB Empty Slot");
            placeholder.SetActive(false);
            if (parent != null)
                placeholder.transform.SetParent(parent, false);
            placeholder.AddComponent<EmptySlotPresentation>();
            return placeholder;
        }

        internal static bool IsPlaceholder(GameObject weapon)
        {
            return weapon == null || weapon.GetComponent<EmptySlotPresentation>() != null;
        }

        internal static bool IsEbManagedSlot(List<GameObject> slot)
        {
            if (slot == null)
                return false;

            foreach (GameObject weapon in slot)
            {
                if (weapon != null &&
                    (weapon.GetComponent<ElementalSlotMarker>() != null || weapon.GetComponent<EmptySlotPresentation>() != null))
                    return true;
            }
            return false;
        }

        internal static int FindSelectablePosition(List<GameObject> slot, int start, int direction)
        {
            if (slot == null || slot.Count == 0)
                return -1;

            direction = direction < 0 ? -1 : 1;
            start = Loop(start, slot.Count);

            for (int offset = 0; offset < slot.Count; offset++)
            {
                int index = Loop(start + offset * direction, slot.Count);
                if (!IsPlaceholder(slot[index]))
                    return index;
            }
            return -1;
        }

        private static int Loop(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }

    internal sealed class EmptySlotPresentation : MonoBehaviour
    {
        private void OnEnable()
        {
            // WeaponIcon normally replaces these sprites when a real weapon is drawn. An empty
            // placeholder intentionally has no WeaponIcon, so explicitly clear the previous gun's
            // HUD image instead of leaving a stale icon/glow behind.
            WeaponHUD hud = MonoSingleton<WeaponHUD>.Instance;
            if (hud != null)
                hud.UpdateImage(null, null, 0);
        }
    }
}
