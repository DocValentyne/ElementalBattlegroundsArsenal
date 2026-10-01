using System;
using System.Collections.Generic;
using ElementalBattlegroundsMod.Api;
using UnityEngine;

namespace ElementalBattlegroundsMod
{
    /// <summary>
    /// Internal immutable snapshot for one registered element. Built-in gameplay may keep using
    /// ElementId, while persistence/UI/public addon identity always uses stableId.
    /// </summary>
    internal sealed class ElementRegistryRecord
    {
        internal readonly string stableId;
        internal readonly string displayName;
        internal readonly ElementId builtInId;
        internal readonly bool selectable;
        internal readonly string iconResource;
        internal readonly string[] legacyIds;

        internal readonly string author;
        internal readonly Sprite iconSprite;
        internal readonly Color accent;
        internal readonly GunColorPreset gunColors;
        internal readonly string simpleGuide;
        internal readonly string advancedGuide;
        internal readonly Dictionary<WeaponFamily, ExternalElementWeaponRecord> externalWeapons;
        internal readonly bool external;

        internal ElementRegistryRecord(
            string stableId,
            string displayName,
            ElementId builtInId,
            bool selectable,
            string iconResource,
            params string[] legacyIds)
            : this(
                stableId,
                displayName,
                builtInId,
                selectable,
                iconResource,
                legacyIds,
                null,
                null,
                Color.white,
                new GunColorPreset(Color.white, Color.white, Color.white),
                null,
                null,
                null,
                false)
        {
        }

        internal ElementRegistryRecord(
            string stableId,
            string displayName,
            ElementId builtInId,
            bool selectable,
            string iconResource,
            string[] legacyIds,
            string author,
            Sprite iconSprite,
            Color accent,
            GunColorPreset gunColors,
            string simpleGuide,
            string advancedGuide,
            Dictionary<WeaponFamily, ExternalElementWeaponRecord> externalWeapons,
            bool external)
        {
            this.stableId = stableId;
            this.displayName = displayName;
            this.builtInId = builtInId;
            this.selectable = selectable;
            this.iconResource = iconResource;
            this.legacyIds = legacyIds ?? Array.Empty<string>();
            this.author = author;
            this.iconSprite = iconSprite;
            this.accent = accent;
            this.gunColors = gunColors;
            this.simpleGuide = simpleGuide;
            this.advancedGuide = advancedGuide;
            this.externalWeapons = externalWeapons;
            this.external = external;
        }

        internal bool TryGetWeapon(WeaponFamily family, out ExternalElementWeaponRecord weapon)
        {
            weapon = null;
            return externalWeapons != null && externalWeapons.TryGetValue(family, out weapon);
        }
    }

    /// <summary>
    /// Canonical element identity registry. Public API v1 delegates here; external addons never
    /// receive this type directly.
    /// </summary>
    internal static class ElementRegistry
    {
        internal const string OfficialNamespace = "elementalbattlegrounds";

        private static readonly List<ElementRegistryRecord> ordered = new List<ElementRegistryRecord>();
        private static readonly Dictionary<string, ElementRegistryRecord> byStableId =
            new Dictionary<string, ElementRegistryRecord>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> aliases =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<ElementId, ElementRegistryRecord> byBuiltInId =
            new Dictionary<ElementId, ElementRegistryRecord>();

        internal static event Action Changed;

        static ElementRegistry()
        {
            // Keep this order equal to the Element Battlegrounds directory/menu order. Identity
            // does not depend on enum ordinals.
            RegisterOfficial("fire", "Fire", ElementId.Fire, true, "fire");
            RegisterOfficial("water", "Water", ElementId.Water, true, "water");
            RegisterOfficial("grass", "Grass", ElementId.Grass, true, "grass");
            RegisterOfficial("wind", "Wind", ElementId.Wind, true, "wind");
            RegisterOfficial("storm", "Storm", ElementId.Storm, true, "storm");
            RegisterOfficial("earth", "Earth", ElementId.Earth, true, "earth");
            RegisterOfficial("lava", "Lava", ElementId.Lava, false, "lava");
            RegisterOfficial("ice", "Ice", ElementId.Ice, false, "ice");
            RegisterOfficial("nature", "Nature", ElementId.Nature, false, "nature");
            RegisterOfficial("sand", "Sand", ElementId.Sand, false, "sand");
            RegisterOfficial("metal", "Metal", ElementId.Metal, false, "metal");
            RegisterOfficial("plasma", "Plasma", ElementId.Plasma, false, "plasma");
            RegisterOfficial("crystal", "Crystal", ElementId.Crystal, false, "crystal");
            RegisterOfficial("spirit", "Spirit", ElementId.Spirit, false, "spirit");
            RegisterOfficial("sound", "Sound", ElementId.Sound, false, "sound");
            RegisterOfficial("light", "Light", ElementId.Light, false, "light");
            RegisterOfficial("darkness", "Darkness", ElementId.Darkness, false, "darkness");
            RegisterOfficial("gravity", "Gravity", ElementId.Gravity, false, "gravity");
            RegisterOfficial("explosion", "Explosion", ElementId.Explosion, false, "explosion");
            RegisterOfficial("technology", "Technology", ElementId.Technology, false, "technology");
            RegisterOfficial("phoenix", "Phoenix", ElementId.Phoenix, false, "phoenix");
            RegisterOfficial("dragon", "Dragon", ElementId.Dragon, false, "dragon");
            RegisterOfficial("acid", "Acid", ElementId.Acid, false, "acid");
            RegisterOfficial("aurora", "Aurora", ElementId.Aurora, false, "aurora");
            RegisterOfficial("nightmare", "Nightmare", ElementId.Nightmare, false, "nightmare");
            RegisterOfficial("time", "Time", ElementId.Time, false, "time");
            RegisterOfficial("void", "Void", ElementId.Void, false, "void");
            RegisterOfficial("spectrum", "Spectrum", ElementId.Spectrum, false, "spectrum");
            RegisterOfficial("chaos", "Chaos", ElementId.Chaos, false, "chaos");
            RegisterOfficial("angel", "Angel", ElementId.Angel, false, "angel");
            RegisterOfficial("space", "Space", ElementId.Space, false, "space");
            RegisterOfficial("reaper", "Reaper", ElementId.Reaper, false, "reaper");
            RegisterOfficial("illusion", "Illusion", ElementId.Illusion, false, "illusion");
            RegisterOfficial("slime", "Slime", ElementId.Slime, false, "slime");
            RegisterOfficial("creation", "Creation", ElementId.Creation, false, "creation");
            RegisterOfficial("solar", "Solar", ElementId.Solar, false, "solar");
            RegisterOfficial("physical", "Physical", ElementId.Physical, false, "physical");
            RegisterOfficial("physical-alt", "Physical Alt", ElementId.PhysicalAlt, false, "physical", "physical_alt");
            RegisterOfficial("sans", "Sans", ElementId.Sans, false, "sans");
        }

        internal static IReadOnlyList<ElementRegistryRecord> Entries => ordered;

        internal static bool TryGet(string idOrAlias, out ElementRegistryRecord record)
        {
            record = null;
            if (string.IsNullOrWhiteSpace(idOrAlias))
                return false;

            string key = idOrAlias.Trim();
            if (aliases.TryGetValue(key, out string canonical))
                key = canonical;
            return byStableId.TryGetValue(key, out record);
        }

        internal static bool TryGet(ElementId builtInId, out ElementRegistryRecord record)
        {
            return byBuiltInId.TryGetValue(builtInId, out record);
        }

        internal static string StableId(ElementId builtInId)
        {
            return TryGet(builtInId, out ElementRegistryRecord record) ? record.stableId : null;
        }

        internal static string DisplayName(ElementId builtInId)
        {
            if (builtInId == ElementId.None)
                return "None";
            if (builtInId == ElementId.Vanilla)
                return "Vanilla";
            return TryGet(builtInId, out ElementRegistryRecord record) ? record.displayName : "Unknown";
        }

        internal static string Canonicalize(string idOrAlias)
        {
            return TryGet(idOrAlias, out ElementRegistryRecord record) ? record.stableId : idOrAlias;
        }

        /// <summary>
        /// Shared registration boundary for built-ins and validated public API snapshots. Duplicate
        /// IDs and aliases are rejected and the first registration always wins.
        /// </summary>
        internal static bool Register(ElementRegistryRecord record, out string error)
        {
            error = null;
            if (record == null)
            {
                error = "Registration is null.";
                return false;
            }
            if (!IsNamespacedId(record.stableId))
            {
                error = "Element ID must be namespaced as 'author:name': " + (record.stableId ?? "<null>");
                return false;
            }
            if (string.IsNullOrWhiteSpace(record.displayName))
            {
                error = "Element display name is empty for " + record.stableId + ".";
                return false;
            }
            if (byStableId.ContainsKey(record.stableId) || aliases.ContainsKey(record.stableId))
            {
                error = "Element ID is already registered: " + record.stableId;
                return false;
            }
            if (record.builtInId != ElementId.None && record.builtInId != ElementId.Vanilla && byBuiltInId.ContainsKey(record.builtInId))
            {
                error = "Built-in ElementId is already registered: " + record.builtInId;
                return false;
            }

            HashSet<string> localAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string alias in record.legacyIds)
            {
                if (string.IsNullOrWhiteSpace(alias))
                    continue;
                string trimmed = alias.Trim();
                if (string.Equals(trimmed, record.stableId, StringComparison.OrdinalIgnoreCase) || !localAliases.Add(trimmed) ||
                    byStableId.ContainsKey(trimmed) || aliases.ContainsKey(trimmed))
                {
                    error = "Element legacy ID/alias is duplicated or already registered: " + trimmed;
                    return false;
                }
            }

            ordered.Add(record);
            byStableId.Add(record.stableId, record);
            if (record.builtInId != ElementId.None && record.builtInId != ElementId.Vanilla)
                byBuiltInId.Add(record.builtInId, record);
            foreach (string alias in record.legacyIds)
            {
                if (!string.IsNullOrWhiteSpace(alias))
                    aliases.Add(alias.Trim(), record.stableId);
            }

            NotifyChanged();
            return true;
        }

        internal static bool Unregister(string stableId, out string error)
        {
            error = null;
            if (!TryGet(stableId, out ElementRegistryRecord record))
            {
                error = "Element is not registered: " + (stableId ?? "<null>");
                return false;
            }
            if (!record.external)
            {
                error = "Only addon registrations can be unregistered.";
                return false;
            }

            byStableId.Remove(record.stableId);
            ordered.Remove(record);
            foreach (string alias in record.legacyIds)
            {
                if (!string.IsNullOrWhiteSpace(alias))
                    aliases.Remove(alias.Trim());
            }
            NotifyChanged();
            return true;
        }

        private static void NotifyChanged()
        {
            Action handlers = Changed;
            if (handlers == null)
                return;
            foreach (Action handler in handlers.GetInvocationList())
            {
                try
                {
                    handler();
                }
                catch (Exception exception)
                {
                    Plugin.LogSource?.LogError("Element registry change listener failed: " + exception);
                }
            }
        }

        private static void RegisterOfficial(
            string slug,
            string displayName,
            ElementId builtInId,
            bool selectable,
            string iconSlug,
            params string[] extraLegacyIds)
        {
            string stableId = OfficialNamespace + ":" + slug;
            List<string> legacy = new List<string>
            {
                "eb_" + slug.Replace('-', '_')
            };
            if (extraLegacyIds != null)
                legacy.AddRange(extraLegacyIds);

            ElementRegistryRecord record = new ElementRegistryRecord(
                stableId,
                displayName,
                builtInId,
                selectable,
                "ElementalBattlegroundsMod.element." + iconSlug,
                legacy.ToArray());
            if (!Register(record, out string error))
                throw new InvalidOperationException("Failed registering built-in element " + stableId + ": " + error);
        }

        internal static bool IsNamespacedId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;
            int colon = id.IndexOf(':');
            return colon > 0 && colon < id.Length - 1 && id.IndexOf(':', colon + 1) < 0;
        }
    }
}
