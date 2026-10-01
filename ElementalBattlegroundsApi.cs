using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElementalBattlegroundsMod.Api
{
    /// <summary>
    /// Stable public weapon-family names used by the Elemental Battlegrounds addon API.
    /// These values are part of API v1 and do not expose EB's internal WeaponFamily enum.
    /// </summary>
    public enum ElementWeaponFamily
    {
        Unspecified = -1,
        Revolver = 0,
        Close = 1,
        Rapid = 2,
        Ultimate = 3,
        Explosive = 4
    }

    /// <summary>
    /// Stable public descriptors for vanilla weapon chassis. Addons choose one of these instead
    /// of depending on GunSetter fields or EB's internal prefab-source enum.
    /// </summary>
    public enum VanillaWeaponTemplate
    {
        // API v1 values are explicit for binary stability. Append new templates; never renumber
        // or insert values into the existing range.
        Unspecified = -1,
        PiercerRevolver = 0,
        SlabPiercerRevolver = 1,
        MarksmanRevolver = 2,
        SlabMarksmanRevolver = 3,
        SharpshooterRevolver = 4,
        SlabSharpshooterRevolver = 5,

        CoreEjectShotgun = 6,
        CoreEjectJackhammer = 7,
        PumpChargeShotgun = 8,
        PumpChargeJackhammer = 9,
        SawedOnShotgun = 10,
        SawedOnJackhammer = 11,

        AttractorNailgun = 12,
        AttractorSawbladeLauncher = 13,
        OverheatNailgun = 14,
        OverheatSawbladeLauncher = 15,
        JumpstartNailgun = 16,
        JumpstartSawbladeLauncher = 17,

        ElectricRailcannon = 18,
        ScrewdriverRailcannon = 19,
        MaliciousRailcannon = 20,

        FreezeframeRocketLauncher = 21,
        SrsRocketLauncher = 22,
        FirestarterRocketLauncher = 23
    }

    /// <summary>
    /// Visual palette applied by EB to every weapon belonging to a registered element.
    /// Use FromAccent for a sensible default or provide all three gun colors explicitly.
    /// </summary>
    public sealed class ElementColorPalette
    {
        public Color Accent { get; set; }
        public Color WeaponColor1 { get; set; }
        public Color WeaponColor2 { get; set; }
        public Color WeaponColor3 { get; set; }

        public ElementColorPalette()
        {
            Accent = Color.white;
            WeaponColor1 = new Color(0.25f, 0.25f, 0.25f);
            WeaponColor2 = Color.white;
            WeaponColor3 = Color.white;
        }

        public ElementColorPalette(Color accent, Color weaponColor1, Color weaponColor2, Color weaponColor3)
        {
            Accent = accent;
            WeaponColor1 = weaponColor1;
            WeaponColor2 = weaponColor2;
            WeaponColor3 = weaponColor3;
        }

        public static ElementColorPalette FromAccent(Color accent)
        {
            return new ElementColorPalette(
                accent,
                Color.Lerp(Color.black, accent, 0.48f),
                Color.Lerp(Color.black, accent, 0.90f),
                Color.Lerp(accent, Color.white, 0.58f));
        }
    }

    /// <summary>
    /// Immutable context passed to an addon's weapon attach callback and returned by
    /// TryGetWeaponContext. EB owns the cloned weapon and slot marker; addons only receive this
    /// stable view plus the cloned GameObject.
    /// </summary>
    public sealed class ElementWeaponContext
    {
        public GameObject Weapon { get; }
        public string ElementId { get; }
        public string ElementDisplayName { get; }
        public ElementWeaponFamily Family { get; }
        /// <summary>Zero-based position inside the family (0, 1, or 2).</summary>
        public int SlotIndex { get; }
        public VanillaWeaponTemplate Template { get; }
        public string WeaponDisplayName { get; }
        public bool IsDualWieldClone { get; }

        internal ElementWeaponContext(
            GameObject weapon,
            string elementId,
            string elementDisplayName,
            ElementWeaponFamily family,
            int slotIndex,
            VanillaWeaponTemplate template,
            string weaponDisplayName,
            bool isDualWieldClone)
        {
            Weapon = weapon;
            ElementId = elementId;
            ElementDisplayName = elementDisplayName;
            Family = family;
            SlotIndex = slotIndex;
            Template = template;
            WeaponDisplayName = weaponDisplayName;
            IsDualWieldClone = isDualWieldClone;
        }
    }

    /// <summary>
    /// Defines one of the five weapons supplied by a custom element.
    /// Attach may be null when the addon only wants the selected vanilla chassis with EB coloring.
    /// Components attached here are cloned naturally by ULTRAKILL's Dual Wield system.
    /// </summary>
    public sealed class ElementWeaponRegistration
    {
        public ElementWeaponRegistration()
        {
            Family = ElementWeaponFamily.Unspecified;
            Template = VanillaWeaponTemplate.Unspecified;
        }

        public ElementWeaponFamily Family { get; set; }
        public VanillaWeaponTemplate Template { get; set; }
        public string DisplayName { get; set; }
        /// <summary>
        /// Set true when the addon replaces this chassis's normal secondary action. EB uses this
        /// for compatibility layers such as Weapon Variant Binds; it does not implement input for you.
        /// </summary>
        public bool OwnsSecondary { get; set; }
        public Action<ElementWeaponContext> Attach { get; set; }
    }

    /// <summary>
    /// Complete registration record for one addon element. IDs are canonicalized to lowercase and
    /// must be namespaced (for example "myname:prism"). API v1 requires exactly one weapon for
    /// each of the five weapon families.
    /// </summary>
    public sealed class ElementRegistration
    {
        /// <summary>API contract targeted by this registration. For this release, use 1.</summary>
        public int ApiVersion { get; set; }

        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Author { get; set; }
        public ElementColorPalette Palette { get; set; }
        public Sprite Icon { get; set; }
        public string SimpleGuide { get; set; }
        public string AdvancedGuide { get; set; }
        public string[] LegacyIds { get; set; }
        public ElementWeaponRegistration[] Weapons { get; set; }
    }

    /// <summary>
    /// Supported public addon facade. Addons should depend only on types in
    /// ElementalBattlegroundsMod.Api, not EB's internal registry/marker/Harmony classes.
    /// </summary>
    public static class ElementalBattlegroundsApi
    {
        /// <summary>BepInEx GUID of the host mod. Useful for a compile-time dependency attribute.</summary>
        public const string PluginGuid = "docvalentyne.ultrakill.elementalbattlegrounds";

        /// <summary>Newest public custom-element API contract supported by this build.</summary>
        public const int ApiVersion = 1;

        /// <summary>Oldest public custom-element API contract still accepted by this build.</summary>
        public const int MinimumSupportedApiVersion = 1;

        /// <summary>Raised after an addon element is registered or unregistered.</summary>
        public static event Action RegistryChanged
        {
            add { ElementRegistry.Changed += value; }
            remove { ElementRegistry.Changed -= value; }
        }

        public static bool RegisterElement(ElementRegistration registration, out string error)
        {
            return CustomElementApiBridge.Register(registration, out error);
        }

        public static bool UnregisterElement(string elementId, out string error)
        {
            return CustomElementApiBridge.Unregister(elementId, out error);
        }

        public static bool IsElementRegistered(string elementId)
        {
            return ElementRegistry.TryGet(elementId, out _);
        }

        /// <summary>
        /// Returns EB slot identity for an addon-registered element weapon, including a Dual Wield
        /// clone. This is optional; most addons only need the context supplied to their Attach callback.
        /// </summary>
        public static bool TryGetWeaponContext(GameObject weapon, out ElementWeaponContext context)
        {
            return CustomElementApiBridge.TryGetWeaponContext(weapon, out context);
        }
    }
}

namespace ElementalBattlegroundsMod
{
    using ElementalBattlegroundsMod.Api;

    internal sealed class ExternalElementWeaponRecord
    {
        internal readonly WeaponFamily family;
        internal readonly VanillaWeaponTemplate template;
        internal readonly string displayName;
        internal readonly bool ownsSecondary;
        internal readonly Action<ElementWeaponContext> attach;

        internal ExternalElementWeaponRecord(
            WeaponFamily family,
            VanillaWeaponTemplate template,
            string displayName,
            bool ownsSecondary,
            Action<ElementWeaponContext> attach)
        {
            this.family = family;
            this.template = template;
            this.displayName = displayName;
            this.ownsSecondary = ownsSecondary;
            this.attach = attach;
        }
    }

    internal static class CustomElementApiBridge
    {
        internal static bool Register(ElementRegistration registration, out string error)
        {
            error = null;
            if (registration == null)
            {
                error = "Element registration is null.";
                return false;
            }
            if (registration.ApiVersion < ElementalBattlegroundsApi.MinimumSupportedApiVersion ||
                registration.ApiVersion > ElementalBattlegroundsApi.ApiVersion)
            {
                error = "Unsupported Elemental Battlegrounds API version " + registration.ApiVersion +
                        "; this build supports API " + ElementalBattlegroundsApi.MinimumSupportedApiVersion +
                        " through " + ElementalBattlegroundsApi.ApiVersion + ".";
                return false;
            }

            string stableId = NormalizePublicId(registration.Id);
            if (!IsPublicIdValid(stableId))
            {
                error = "Element ID must be a lowercase namespaced ID using letters, digits, '.', '_', or '-' (example: author:prism).";
                return false;
            }
            if (stableId.StartsWith(ElementRegistry.OfficialNamespace + ":", StringComparison.OrdinalIgnoreCase))
            {
                error = "The namespace '" + ElementRegistry.OfficialNamespace + "' is reserved for built-in EB elements.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(registration.DisplayName))
            {
                error = "Element display name is required for " + stableId + ".";
                return false;
            }
            if (registration.Palette == null)
            {
                error = "Element palette is required for " + stableId + ".";
                return false;
            }
            if (registration.Weapons == null || registration.Weapons.Length != 5)
            {
                error = "API v1 requires exactly five weapon registrations (Revolver, Close, Rapid, Ultimate, Explosive).";
                return false;
            }

            Dictionary<WeaponFamily, ExternalElementWeaponRecord> weapons =
                new Dictionary<WeaponFamily, ExternalElementWeaponRecord>();
            foreach (ElementWeaponRegistration weapon in registration.Weapons)
            {
                if (weapon == null)
                {
                    error = "Weapon registration is null for " + stableId + ".";
                    return false;
                }
                if (!TryMapFamily(weapon.Family, out WeaponFamily family))
                {
                    error = "Unknown weapon family " + weapon.Family + " for " + stableId + ".";
                    return false;
                }
                if (!VanillaWeaponTemplateMap.BelongsToFamily(weapon.Template, family))
                {
                    error = "Template " + weapon.Template + " does not belong to family " + weapon.Family + ".";
                    return false;
                }
                if (weapons.ContainsKey(family))
                {
                    error = "Element " + stableId + " registers family " + weapon.Family + " more than once.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(weapon.DisplayName))
                {
                    error = "Weapon display name is required for " + stableId + " / " + weapon.Family + ".";
                    return false;
                }
                weapons.Add(family, new ExternalElementWeaponRecord(
                    family,
                    weapon.Template,
                    weapon.DisplayName.Trim(),
                    weapon.OwnsSecondary,
                    weapon.Attach));
            }

            if (weapons.Count != 5)
            {
                error = "API v1 requires one weapon for every weapon family.";
                return false;
            }

            string[] legacyIds = CopyAliases(registration.LegacyIds);
            foreach (string legacyId in legacyIds)
            {
                if (!IsPublicIdValid(legacyId))
                {
                    error = "Legacy IDs must use the same namespaced ID format as current IDs: " + legacyId;
                    return false;
                }
                if (legacyId.StartsWith(ElementRegistry.OfficialNamespace + ":", StringComparison.OrdinalIgnoreCase))
                {
                    error = "The namespace '" + ElementRegistry.OfficialNamespace + "' is reserved and cannot be used by addon legacy IDs.";
                    return false;
                }
            }

            ElementRegistryRecord record = new ElementRegistryRecord(
                stableId,
                registration.DisplayName.Trim(),
                ElementId.None,
                true,
                null,
                legacyIds,
                registration.Author?.Trim(),
                registration.Icon,
                registration.Palette.Accent,
                new GunColorPreset(
                    registration.Palette.WeaponColor1,
                    registration.Palette.WeaponColor2,
                    registration.Palette.WeaponColor3),
                registration.SimpleGuide,
                registration.AdvancedGuide,
                weapons,
                true);

            bool registered = ElementRegistry.Register(record, out error);
            if (!registered)
                return false;

            Plugin.LogSource?.LogInfo("Registered custom EB element " + stableId +
                                      (string.IsNullOrEmpty(record.author) ? "." : " by " + record.author + "."));
            return true;
        }

        internal static bool Unregister(string elementId, out string error)
        {
            string canonical = NormalizePublicId(elementId);
            if (!ElementRegistry.TryGet(canonical, out ElementRegistryRecord record))
            {
                error = "Element is not registered: " + (elementId ?? "<null>");
                return false;
            }
            if (!record.external)
            {
                error = "Built-in EB elements cannot be unregistered through the public API: " + record.stableId;
                return false;
            }
            if (!ElementRegistry.Unregister(record.stableId, out error))
                return false;

            Plugin.LogSource?.LogInfo("Unregistered custom EB element " + record.stableId + ". Existing live weapon clones remain until the arsenal is rebuilt.");
            return true;
        }

        internal static bool TryGetWeaponContext(GameObject weapon, out ElementWeaponContext context)
        {
            context = null;
            ElementalSlotMarker marker = MarkerUtility.Find(weapon);
            if (marker == null || string.IsNullOrEmpty(marker.elementId) || !marker.hasVanillaTemplate)
                return false;
            if (!ElementRegistry.TryGet(marker.elementId, out ElementRegistryRecord record) ||
                !record.external ||
                !record.TryGetWeapon(marker.family, out ExternalElementWeaponRecord externalWeapon))
                return false;

            string weaponName = externalWeapon.displayName;
            GameObject weaponRoot = marker.gameObject;
            WeaponIdentifier wid = weaponRoot != null ? weaponRoot.GetComponent<WeaponIdentifier>() : null;
            context = new ElementWeaponContext(
                weaponRoot,
                record.stableId,
                record.displayName,
                ToPublicFamily(marker.family),
                marker.position,
                marker.vanillaTemplate,
                weaponName,
                wid != null && wid.duplicate);
            return true;
        }

        internal static ElementWeaponContext CreateContext(GameObject weapon, ElementalSlotMarker marker, ElementRegistryRecord record, ExternalElementWeaponRecord weaponRecord)
        {
            WeaponIdentifier wid = weapon != null ? weapon.GetComponent<WeaponIdentifier>() : null;
            return new ElementWeaponContext(
                weapon,
                record.stableId,
                record.displayName,
                ToPublicFamily(marker.family),
                marker.position,
                weaponRecord.template,
                weaponRecord.displayName,
                wid != null && wid.duplicate);
        }

        internal static ElementWeaponFamily ToPublicFamily(WeaponFamily family)
        {
            switch (family)
            {
                case WeaponFamily.Revolver: return ElementWeaponFamily.Revolver;
                case WeaponFamily.Shotgun: return ElementWeaponFamily.Close;
                case WeaponFamily.Rapid: return ElementWeaponFamily.Rapid;
                case WeaponFamily.Ultimate: return ElementWeaponFamily.Ultimate;
                default: return ElementWeaponFamily.Explosive;
            }
        }

        internal static bool TryMapFamily(ElementWeaponFamily family, out WeaponFamily result)
        {
            switch (family)
            {
                case ElementWeaponFamily.Revolver: result = WeaponFamily.Revolver; return true;
                case ElementWeaponFamily.Close: result = WeaponFamily.Shotgun; return true;
                case ElementWeaponFamily.Rapid: result = WeaponFamily.Rapid; return true;
                case ElementWeaponFamily.Ultimate: result = WeaponFamily.Ultimate; return true;
                case ElementWeaponFamily.Explosive: result = WeaponFamily.Explosive; return true;
                default: result = default(WeaponFamily); return false;
            }
        }

        private static string[] CopyAliases(string[] aliases)
        {
            if (aliases == null || aliases.Length == 0)
                return Array.Empty<string>();
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string alias in aliases)
            {
                if (string.IsNullOrWhiteSpace(alias))
                    continue;
                string trimmed = NormalizePublicId(alias);
                if (!string.IsNullOrEmpty(trimmed) && seen.Add(trimmed))
                    result.Add(trimmed);
            }
            return result.ToArray();
        }

        private static string NormalizePublicId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? null : id.Trim().ToLowerInvariant();
        }

        private static bool IsPublicIdValid(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;
            int colon = id.IndexOf(':');
            if (colon <= 0 || colon >= id.Length - 1 || id.IndexOf(':', colon + 1) >= 0)
                return false;
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                if (c == ':')
                    continue;
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '.' || c == '_' || c == '-')
                    continue;
                return false;
            }
            return true;
        }
    }

    internal static class VanillaWeaponTemplateMap
    {
        internal static bool BelongsToFamily(VanillaWeaponTemplate template, WeaponFamily family)
        {
            return TryGet(template, out WeaponFamily templateFamily, out _, out _) && templateFamily == family;
        }

        internal static bool TryGet(
            VanillaWeaponTemplate template,
            out WeaponFamily family,
            out ElementPrefabSource source,
            out int form)
        {
            form = 0;
            switch (template)
            {
                case VanillaWeaponTemplate.PiercerRevolver:
                    family = WeaponFamily.Revolver; source = ElementPrefabSource.RevolverPierce; return true;
                case VanillaWeaponTemplate.SlabPiercerRevolver:
                    family = WeaponFamily.Revolver; source = ElementPrefabSource.RevolverPierce; form = 1; return true;
                case VanillaWeaponTemplate.MarksmanRevolver:
                    family = WeaponFamily.Revolver; source = ElementPrefabSource.RevolverRicochet; return true;
                case VanillaWeaponTemplate.SlabMarksmanRevolver:
                    family = WeaponFamily.Revolver; source = ElementPrefabSource.RevolverRicochet; form = 1; return true;
                case VanillaWeaponTemplate.SharpshooterRevolver:
                    family = WeaponFamily.Revolver; source = ElementPrefabSource.RevolverTwirl; return true;
                case VanillaWeaponTemplate.SlabSharpshooterRevolver:
                    family = WeaponFamily.Revolver; source = ElementPrefabSource.RevolverTwirl; form = 1; return true;

                case VanillaWeaponTemplate.CoreEjectShotgun:
                    family = WeaponFamily.Shotgun; source = ElementPrefabSource.ShotgunGrenade; return true;
                case VanillaWeaponTemplate.CoreEjectJackhammer:
                    family = WeaponFamily.Shotgun; source = ElementPrefabSource.ShotgunGrenade; form = 1; return true;
                case VanillaWeaponTemplate.PumpChargeShotgun:
                    family = WeaponFamily.Shotgun; source = ElementPrefabSource.ShotgunPump; return true;
                case VanillaWeaponTemplate.PumpChargeJackhammer:
                    family = WeaponFamily.Shotgun; source = ElementPrefabSource.ShotgunPump; form = 1; return true;
                case VanillaWeaponTemplate.SawedOnShotgun:
                    family = WeaponFamily.Shotgun; source = ElementPrefabSource.ShotgunRed; return true;
                case VanillaWeaponTemplate.SawedOnJackhammer:
                    family = WeaponFamily.Shotgun; source = ElementPrefabSource.ShotgunRed; form = 1; return true;

                case VanillaWeaponTemplate.AttractorNailgun:
                    family = WeaponFamily.Rapid; source = ElementPrefabSource.NailMagnet; return true;
                case VanillaWeaponTemplate.AttractorSawbladeLauncher:
                    family = WeaponFamily.Rapid; source = ElementPrefabSource.NailMagnet; form = 1; return true;
                case VanillaWeaponTemplate.OverheatNailgun:
                    family = WeaponFamily.Rapid; source = ElementPrefabSource.NailOverheat; return true;
                case VanillaWeaponTemplate.OverheatSawbladeLauncher:
                    family = WeaponFamily.Rapid; source = ElementPrefabSource.NailOverheat; form = 1; return true;
                case VanillaWeaponTemplate.JumpstartNailgun:
                    family = WeaponFamily.Rapid; source = ElementPrefabSource.NailRed; return true;
                case VanillaWeaponTemplate.JumpstartSawbladeLauncher:
                    family = WeaponFamily.Rapid; source = ElementPrefabSource.NailRed; form = 1; return true;

                case VanillaWeaponTemplate.ElectricRailcannon:
                    family = WeaponFamily.Ultimate; source = ElementPrefabSource.RailCannon; return true;
                case VanillaWeaponTemplate.ScrewdriverRailcannon:
                    family = WeaponFamily.Ultimate; source = ElementPrefabSource.RailHarpoon; return true;
                case VanillaWeaponTemplate.MaliciousRailcannon:
                    family = WeaponFamily.Ultimate; source = ElementPrefabSource.RailMalicious; return true;

                case VanillaWeaponTemplate.FreezeframeRocketLauncher:
                    family = WeaponFamily.Explosive; source = ElementPrefabSource.RocketBlue; return true;
                case VanillaWeaponTemplate.SrsRocketLauncher:
                    family = WeaponFamily.Explosive; source = ElementPrefabSource.RocketGreen; return true;
                case VanillaWeaponTemplate.FirestarterRocketLauncher:
                    family = WeaponFamily.Explosive; source = ElementPrefabSource.RocketRed; return true;
                default:
                    family = default(WeaponFamily); source = default(ElementPrefabSource); return false;
            }
        }

        internal static bool TryFromSource(ElementPrefabSource source, int form, out VanillaWeaponTemplate template)
        {
            switch (source)
            {
                case ElementPrefabSource.RevolverPierce:
                    template = form > 0 ? VanillaWeaponTemplate.SlabPiercerRevolver : VanillaWeaponTemplate.PiercerRevolver; return true;
                case ElementPrefabSource.RevolverRicochet:
                    template = form > 0 ? VanillaWeaponTemplate.SlabMarksmanRevolver : VanillaWeaponTemplate.MarksmanRevolver; return true;
                case ElementPrefabSource.RevolverTwirl:
                    template = form > 0 ? VanillaWeaponTemplate.SlabSharpshooterRevolver : VanillaWeaponTemplate.SharpshooterRevolver; return true;
                case ElementPrefabSource.ShotgunGrenade:
                    template = form > 0 ? VanillaWeaponTemplate.CoreEjectJackhammer : VanillaWeaponTemplate.CoreEjectShotgun; return true;
                case ElementPrefabSource.ShotgunPump:
                    template = form > 0 ? VanillaWeaponTemplate.PumpChargeJackhammer : VanillaWeaponTemplate.PumpChargeShotgun; return true;
                case ElementPrefabSource.ShotgunRed:
                    template = form > 0 ? VanillaWeaponTemplate.SawedOnJackhammer : VanillaWeaponTemplate.SawedOnShotgun; return true;
                case ElementPrefabSource.NailMagnet:
                    template = form > 0 ? VanillaWeaponTemplate.AttractorSawbladeLauncher : VanillaWeaponTemplate.AttractorNailgun; return true;
                case ElementPrefabSource.NailOverheat:
                    template = form > 0 ? VanillaWeaponTemplate.OverheatSawbladeLauncher : VanillaWeaponTemplate.OverheatNailgun; return true;
                case ElementPrefabSource.NailRed:
                    template = form > 0 ? VanillaWeaponTemplate.JumpstartSawbladeLauncher : VanillaWeaponTemplate.JumpstartNailgun; return true;
                case ElementPrefabSource.RailCannon:
                    template = VanillaWeaponTemplate.ElectricRailcannon; return true;
                case ElementPrefabSource.RailHarpoon:
                    template = VanillaWeaponTemplate.ScrewdriverRailcannon; return true;
                case ElementPrefabSource.RailMalicious:
                    template = VanillaWeaponTemplate.MaliciousRailcannon; return true;
                case ElementPrefabSource.RocketBlue:
                    template = VanillaWeaponTemplate.FreezeframeRocketLauncher; return true;
                case ElementPrefabSource.RocketGreen:
                    template = VanillaWeaponTemplate.SrsRocketLauncher; return true;
                case ElementPrefabSource.RocketRed:
                    template = VanillaWeaponTemplate.FirestarterRocketLauncher; return true;
                default:
                    template = default(VanillaWeaponTemplate); return false;
            }
        }

        internal static bool TryFromDirectChoice(string choiceId, out VanillaWeaponTemplate template)
        {
            switch (choiceId)
            {
                case "rev_piercer_std": template = VanillaWeaponTemplate.PiercerRevolver; return true;
                case "rev_piercer_alt": template = VanillaWeaponTemplate.SlabPiercerRevolver; return true;
                case "rev_marksman_std": template = VanillaWeaponTemplate.MarksmanRevolver; return true;
                case "rev_marksman_alt": template = VanillaWeaponTemplate.SlabMarksmanRevolver; return true;
                case "rev_sharp_std": template = VanillaWeaponTemplate.SharpshooterRevolver; return true;
                case "rev_sharp_alt": template = VanillaWeaponTemplate.SlabSharpshooterRevolver; return true;
                case "sho_core_std": template = VanillaWeaponTemplate.CoreEjectShotgun; return true;
                case "sho_core_alt": template = VanillaWeaponTemplate.CoreEjectJackhammer; return true;
                case "sho_pump_std": template = VanillaWeaponTemplate.PumpChargeShotgun; return true;
                case "sho_pump_alt": template = VanillaWeaponTemplate.PumpChargeJackhammer; return true;
                case "sho_saw_std": template = VanillaWeaponTemplate.SawedOnShotgun; return true;
                case "sho_saw_alt": template = VanillaWeaponTemplate.SawedOnJackhammer; return true;
                case "nai_attr_std": template = VanillaWeaponTemplate.AttractorNailgun; return true;
                case "nai_attr_alt": template = VanillaWeaponTemplate.AttractorSawbladeLauncher; return true;
                case "nai_over_std": template = VanillaWeaponTemplate.OverheatNailgun; return true;
                case "nai_over_alt": template = VanillaWeaponTemplate.OverheatSawbladeLauncher; return true;
                case "nai_jump_std": template = VanillaWeaponTemplate.JumpstartNailgun; return true;
                case "nai_jump_alt": template = VanillaWeaponTemplate.JumpstartSawbladeLauncher; return true;
                case "rai_electric_std": template = VanillaWeaponTemplate.ElectricRailcannon; return true;
                case "rai_screw_std": template = VanillaWeaponTemplate.ScrewdriverRailcannon; return true;
                case "rai_mal_std": template = VanillaWeaponTemplate.MaliciousRailcannon; return true;
                case "rock_freeze_std":
                case "gl_blue": template = VanillaWeaponTemplate.FreezeframeRocketLauncher; return true;
                case "rock_srs_std":
                case "gl_green": template = VanillaWeaponTemplate.SrsRocketLauncher; return true;
                case "rock_fire_std":
                case "gl_red": template = VanillaWeaponTemplate.FirestarterRocketLauncher; return true;
                default: template = default(VanillaWeaponTemplate); return false;
            }
        }
    }
}
