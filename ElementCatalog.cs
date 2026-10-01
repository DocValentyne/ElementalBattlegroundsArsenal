using System.Collections.Generic;
using ElementalBattlegroundsMod.Api;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ElementalBattlegroundsMod
{
    internal enum ElementPrefabSource
    {
        RevolverPierce,
        RevolverTwirl,
        RevolverRicochet,
        ShotgunGrenade,
        ShotgunPump,
        ShotgunRed,
        NailMagnet,
        NailOverheat,
        NailRed,
        RailCannon,
        RailHarpoon,
        RailMalicious,
        RocketBlue,
        RocketGreen,
        RocketRed
    }

    internal readonly struct ElementWeaponSpec
    {
        internal readonly ElementPrefabSource source;
        internal readonly int form;

        internal ElementWeaponSpec(ElementPrefabSource source, int form = 0)
        {
            this.source = source;
            this.form = form;
        }
    }

    internal readonly struct ElementDefinition
    {
        internal readonly ElementId id;
        internal readonly string name;
        internal readonly Color accent;
        internal readonly GunColorPreset gunColors;
        internal readonly Color grenadeOrange;
        internal readonly Color grenadeSilver;
        internal readonly Color grenadeBlack;
        internal readonly Color grenadeDeepBlack;

        internal ElementDefinition(
            ElementId id,
            string name,
            Color accent,
            GunColorPreset gunColors,
            Color grenadeOrange,
            Color grenadeSilver,
            Color grenadeBlack,
            Color grenadeDeepBlack)
        {
            this.id = id;
            this.name = name;
            this.accent = accent;
            this.gunColors = gunColors;
            this.grenadeOrange = grenadeOrange;
            this.grenadeSilver = grenadeSilver;
            this.grenadeBlack = grenadeBlack;
            this.grenadeDeepBlack = grenadeDeepBlack;
        }
    }

    /// <summary>
    /// One registry for element identity, visual palette, and backing vanilla prefabs.
    /// Mechanics still live in their controllers; this class prevents every subsystem from
    /// growing another Fire/Water/Grass/Wind/Storm/Earth switch ladder.
    /// </summary>
    internal static class ElementCatalog
    {
        private static readonly Dictionary<ElementId, ElementDefinition> Definitions =
            new Dictionary<ElementId, ElementDefinition>
            {
                [ElementId.Fire] = new ElementDefinition(
                    ElementId.Fire, "Fire",
                    new Color(1f, 0.28f, 0.12f),
                    new GunColorPreset(new Color(0.55f, 0.05f, 0.02f), new Color(1f, 0.28f, 0.05f), new Color(1f, 0.72f, 0.35f)),
                    new Color(0.90f, 0.20f, 0.06f), new Color(0.56f, 0.20f, 0.08f), new Color(0.20f, 0.035f, 0.02f), new Color(0.07f, 0.012f, 0.008f)),
                [ElementId.Water] = new ElementDefinition(
                    ElementId.Water, "Water",
                    new Color(0.2f, 0.55f, 1f),
                    new GunColorPreset(new Color(0.03f, 0.15f, 0.55f), new Color(0.1f, 0.55f, 1f), new Color(0.65f, 0.9f, 1f)),
                    new Color(0.12f, 0.48f, 0.96f), new Color(0.30f, 0.66f, 0.92f), new Color(0.025f, 0.12f, 0.28f), new Color(0.01f, 0.045f, 0.11f)),
                [ElementId.Grass] = new ElementDefinition(
                    ElementId.Grass, "Grass",
                    new Color(0.25f, 0.9f, 0.3f),
                    new GunColorPreset(new Color(0.03f, 0.35f, 0.08f), new Color(0.12f, 0.75f, 0.2f), new Color(0.65f, 1f, 0.45f)),
                    new Color(0.24f, 0.80f, 0.18f), new Color(0.42f, 0.66f, 0.22f), new Color(0.035f, 0.20f, 0.055f), new Color(0.012f, 0.075f, 0.02f)),
                // The Part 2 icon palettes are registered now so every future weapon/UI path
                // reads the same colors even before the new mechanics are exposed in settings.
                [ElementId.Wind] = new ElementDefinition(
                    ElementId.Wind, "Wind",
                    new Color(0.82f, 0.92f, 0.52f),
                    new GunColorPreset(new Color(0.22f, 0.30f, 0.07f), new Color(0.68f, 0.80f, 0.30f), new Color(0.94f, 0.98f, 0.76f)),
                    new Color(0.80f, 0.90f, 0.42f), new Color(0.88f, 0.92f, 0.72f), new Color(0.20f, 0.27f, 0.055f), new Color(0.07f, 0.095f, 0.018f)),
                [ElementId.Storm] = new ElementDefinition(
                    ElementId.Storm, "Storm",
                    new Color(0.12f, 0.80f, 0.90f),
                    new GunColorPreset(new Color(0.015f, 0.24f, 0.33f), new Color(0.08f, 0.69f, 0.80f), new Color(0.58f, 0.96f, 1f)),
                    new Color(0.10f, 0.79f, 0.88f), new Color(0.28f, 0.68f, 0.76f), new Color(0.015f, 0.20f, 0.27f), new Color(0.005f, 0.075f, 0.105f)),
                [ElementId.Earth] = new ElementDefinition(
                    ElementId.Earth, "Earth",
                    new Color(0.88f, 0.62f, 0.18f),
                    new GunColorPreset(new Color(0.31f, 0.14f, 0.025f), new Color(0.76f, 0.46f, 0.08f), new Color(1f, 0.82f, 0.42f)),
                    new Color(0.85f, 0.56f, 0.12f), new Color(0.68f, 0.48f, 0.20f), new Color(0.23f, 0.105f, 0.02f), new Color(0.08f, 0.035f, 0.006f))
            };

        private static readonly Dictionary<ElementId, ElementWeaponSpec[]> WeaponSpecs =
            new Dictionary<ElementId, ElementWeaponSpec[]>
            {
                [ElementId.Fire] = new[]
                {
                    new ElementWeaponSpec(ElementPrefabSource.RevolverPierce, 1),
                    new ElementWeaponSpec(ElementPrefabSource.ShotgunGrenade, 0),
                    new ElementWeaponSpec(ElementPrefabSource.NailOverheat, 0),
                    new ElementWeaponSpec(ElementPrefabSource.RailMalicious, 0),
                    new ElementWeaponSpec(ElementPrefabSource.RocketRed, 0)
                },
                [ElementId.Water] = new[]
                {
                    new ElementWeaponSpec(ElementPrefabSource.RevolverPierce, 0),
                    new ElementWeaponSpec(ElementPrefabSource.ShotgunPump, 1),
                    new ElementWeaponSpec(ElementPrefabSource.NailMagnet, 0),
                    new ElementWeaponSpec(ElementPrefabSource.RailCannon, 0),
                    new ElementWeaponSpec(ElementPrefabSource.RocketBlue, 0)
                },
                [ElementId.Grass] = new[]
                {
                    new ElementWeaponSpec(ElementPrefabSource.RevolverPierce, 0),
                    new ElementWeaponSpec(ElementPrefabSource.ShotgunGrenade, 0),
                    // Poison uses Jumpstart's chassis/status screen; native zapper is suppressed.
                    new ElementWeaponSpec(ElementPrefabSource.NailRed, 0),
                    new ElementWeaponSpec(ElementPrefabSource.RailHarpoon, 0),
                    new ElementWeaponSpec(ElementPrefabSource.RocketBlue, 0)
                }
,
                [ElementId.Wind] = new[]
                {
                    new ElementWeaponSpec(ElementPrefabSource.RevolverPierce, 0),
                    new ElementWeaponSpec(ElementPrefabSource.ShotgunGrenade, 0),
                    // Tempest uses Jumpstart Sawblade Launcher as its chassis: regular (non-silver)
                    // saws, no Attractor ammo/magnet HUD, and a status screen we can repurpose.
                    new ElementWeaponSpec(ElementPrefabSource.NailRed, 1),
                    new ElementWeaponSpec(ElementPrefabSource.RailCannon, 0),
                    new ElementWeaponSpec(ElementPrefabSource.RocketBlue, 0)
                },
                [ElementId.Storm] = new[]
                {
                    // Storm keeps the standard Piercer's charge/coin behavior and layers a small
                    // elemental blast onto the charged beam impact.
                    new ElementWeaponSpec(ElementPrefabSource.RevolverPierce, 0),
                    new ElementWeaponSpec(ElementPrefabSource.ShotgunGrenade, 1),
                    // Jumpstart Nailgun and Electric Railcannon are deliberately vanilla homes.
                    new ElementWeaponSpec(ElementPrefabSource.NailRed, 0),
                    new ElementWeaponSpec(ElementPrefabSource.RailCannon, 0),
                    // Grenade Launcher 2.0.2 converts this blue launcher instance to GL primary fire.
                    new ElementWeaponSpec(ElementPrefabSource.RocketBlue, 0)
                },
                [ElementId.Earth] = new[]
                {
                    new ElementWeaponSpec(ElementPrefabSource.RevolverPierce, 1),
                    new ElementWeaponSpec(ElementPrefabSource.ShotgunGrenade, 1),
                    new ElementWeaponSpec(ElementPrefabSource.NailOverheat, 1),
                    new ElementWeaponSpec(ElementPrefabSource.RailCannon, 0),
                    new ElementWeaponSpec(ElementPrefabSource.RocketGreen, 0)
                }
            };

        internal static ElementDefinition Get(ElementId element)
        {
            if (Definitions.TryGetValue(element, out ElementDefinition definition))
                return definition;
            return new ElementDefinition(ElementId.Vanilla, "Vanilla", Color.white,
                new GunColorPreset(Color.white, Color.white, Color.white),
                Color.white, Color.white, Color.black, Color.black);
        }

        internal static Color Accent(ElementId element) => Get(element).accent;
        internal static GunColorPreset GunColors(ElementId element) => Get(element).gunColors;

        internal static GameObject GetElementPrefab(GunSetter setter, WeaponFamily family, string stableElementId)
        {
            if (!ElementRegistry.TryGet(stableElementId, out ElementRegistryRecord registration))
                return null;

            if (registration.external)
            {
                if (!registration.TryGetWeapon(family, out ExternalElementWeaponRecord weapon))
                    return null;
                return GetTemplatePrefab(setter, weapon.template);
            }

            if (registration.builtInId == ElementId.None || registration.builtInId == ElementId.Vanilla)
                return null;
            return GetElementPrefab(setter, family, registration.builtInId);
        }

        internal static GameObject GetElementPrefab(GunSetter setter, WeaponFamily family, ElementId element)
        {
            if (setter == null || !WeaponSpecs.TryGetValue(element, out ElementWeaponSpec[] specs))
                return null;
            int index = (int)family;
            if (index < 0 || index >= specs.Length)
                return null;
            ElementWeaponSpec spec = specs[index];
            return Pick(GetReferences(setter, spec.source), spec.form);
        }

        internal static bool TryGetWeaponTemplate(string stableElementId, WeaponFamily family, out VanillaWeaponTemplate template)
        {
            template = default(VanillaWeaponTemplate);
            if (!ElementRegistry.TryGet(stableElementId, out ElementRegistryRecord registration))
                return false;

            if (registration.external)
            {
                if (!registration.TryGetWeapon(family, out ExternalElementWeaponRecord weapon))
                    return false;
                template = weapon.template;
                return true;
            }

            if (!WeaponSpecs.TryGetValue(registration.builtInId, out ElementWeaponSpec[] specs))
                return false;
            int index = (int)family;
            if (index < 0 || index >= specs.Length)
                return false;
            ElementWeaponSpec spec = specs[index];
            return VanillaWeaponTemplateMap.TryFromSource(spec.source, spec.form, out template);
        }

        internal static GameObject GetTemplatePrefab(GunSetter setter, VanillaWeaponTemplate template)
        {
            if (setter == null || !VanillaWeaponTemplateMap.TryGet(template, out _, out ElementPrefabSource source, out int form))
                return null;
            return Pick(GetReferences(setter, source), form);
        }

        private static AssetReference[] GetReferences(GunSetter setter, ElementPrefabSource source)
        {
            switch (source)
            {
                case ElementPrefabSource.RevolverPierce: return setter.revolverPierce;
                case ElementPrefabSource.RevolverTwirl: return setter.revolverTwirl;
                case ElementPrefabSource.RevolverRicochet: return setter.revolverRicochet;
                case ElementPrefabSource.ShotgunGrenade: return setter.shotgunGrenade;
                case ElementPrefabSource.ShotgunPump: return setter.shotgunPump;
                case ElementPrefabSource.ShotgunRed: return setter.shotgunRed;
                case ElementPrefabSource.NailMagnet: return setter.nailMagnet;
                case ElementPrefabSource.NailOverheat: return setter.nailOverheat;
                case ElementPrefabSource.NailRed: return setter.nailRed;
                case ElementPrefabSource.RailCannon: return setter.railCannon;
                case ElementPrefabSource.RailHarpoon: return setter.railHarpoon;
                case ElementPrefabSource.RailMalicious: return setter.railMalicious;
                case ElementPrefabSource.RocketBlue: return setter.rocketBlue;
                case ElementPrefabSource.RocketGreen: return setter.rocketGreen;
                case ElementPrefabSource.RocketRed: return setter.rocketRed;
                default: return null;
            }
        }

        private static GameObject Pick(AssetReference[] refs, int index)
        {
            if (refs == null || refs.Length == 0)
                return null;
            index = Mathf.Clamp(index, 0, refs.Length - 1);
            AssetReference reference = refs[index];
            if (reference == null || !reference.RuntimeKeyIsValid())
                return null;
            return reference.ToAsset();
        }
    }
}
