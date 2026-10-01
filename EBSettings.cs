using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using PluginConfig.API.Functionals;
using UnityEngine;

namespace ElementalBattlegroundsMod
{
    internal enum ExplosiveIntegrationMode
    {
        Default,
        ForceNative,
        ForceGrenade
    }

    internal readonly struct LoadoutChoice
    {
        internal readonly string id;
        internal readonly string displayName;
        internal readonly ElementId element;
        internal readonly int vanillaVariant;
        internal readonly int vanillaForm;
        internal readonly ExplosiveIntegrationMode explosiveMode;
        internal readonly bool elementChoice;
        internal readonly bool missing;

        internal LoadoutChoice(
            string id,
            string displayName,
            ElementId element,
            int vanillaVariant = -1,
            int vanillaForm = 0,
            ExplosiveIntegrationMode explosiveMode = ExplosiveIntegrationMode.Default,
            bool elementChoice = false,
            bool missing = false)
        {
            this.id = id;
            this.displayName = displayName;
            this.element = element;
            this.vanillaVariant = vanillaVariant;
            this.vanillaForm = vanillaForm;
            this.explosiveMode = explosiveMode;
            this.elementChoice = elementChoice;
            this.missing = missing;
        }

        internal bool IsVanilla => !missing && !elementChoice && element == ElementId.Vanilla;
        internal bool IsNone => !missing && !elementChoice && element == ElementId.None;
        internal bool IsElement => !missing && elementChoice;
        internal bool IsMissing => missing;
        internal bool IsDirectWeapon => IsVanilla && vanillaVariant >= 0;
    }

    internal readonly struct WeaponHomeInfo
    {
        internal readonly ElementId element;
        internal readonly WeaponFamily family;
        internal readonly bool planned;

        internal WeaponHomeInfo(ElementId element, WeaponFamily family, bool planned = false)
        {
            this.element = element;
            this.family = family;
            this.planned = planned;
        }
    }

    internal static class WeaponHomeRegistry
    {
        private static readonly Dictionary<string, WeaponHomeInfo> Homes =
            new Dictionary<string, WeaponHomeInfo>(StringComparer.OrdinalIgnoreCase)
            {
                ["rev_piercer_std"] = new WeaponHomeInfo(ElementId.Water, WeaponFamily.Revolver),
                ["nai_over_std"] = new WeaponHomeInfo(ElementId.Fire, WeaponFamily.Rapid),
                ["nai_attr_std"] = new WeaponHomeInfo(ElementId.Water, WeaponFamily.Rapid),
                ["rock_fire_std"] = new WeaponHomeInfo(ElementId.Fire, WeaponFamily.Explosive),
                ["nai_jump_std"] = new WeaponHomeInfo(ElementId.Storm, WeaponFamily.Rapid),
                ["rai_electric_std"] = new WeaponHomeInfo(ElementId.Storm, WeaponFamily.Ultimate),
                ["rock_srs_std"] = new WeaponHomeInfo(ElementId.Earth, WeaponFamily.Explosive)
            };

        internal static bool TryGet(string choiceId, out WeaponHomeInfo info)
        {
            return Homes.TryGetValue(choiceId ?? string.Empty, out info);
        }

        internal static bool TryResolveImplementedElementChoice(
            WeaponFamily family,
            LoadoutChoice directChoice,
            out LoadoutChoice elementChoice)
        {
            elementChoice = default(LoadoutChoice);
            if (!directChoice.IsDirectWeapon ||
                !TryGet(directChoice.id, out WeaponHomeInfo home) ||
                home.family != family)
                return false;

            string choiceId = ElementRegistry.StableId(home.element);
            return !string.IsNullOrEmpty(choiceId) &&
                   EBSettings.TryGetChoiceById(family, choiceId, out elementChoice);
        }

        internal static bool HasElementHome(WeaponFamily family, ElementId element)
        {
            foreach (WeaponHomeInfo home in Homes.Values)
                if (home.family == family && home.element == element)
                    return true;
            return false;
        }
    }

    internal static class EBSettings
    {
        private static readonly StringListField[,] fields = new StringListField[5, 3];
        private static readonly Dictionary<WeaponFamily, LoadoutChoice[]> choices = new Dictionary<WeaponFamily, LoadoutChoice[]>();
        private static readonly HashSet<string>[] fallbackDisplayOptions = new HashSet<string>[5];
        private static BoolField enabled;
        private static ButtonField openElementLoadoutButton;
        private static Texture2D configIconTexture;
        private static Sprite configIconSprite;
        private static bool workingLoadoutInitialized;

        internal static bool Enabled => enabled == null || enabled.value;

        internal static void Initialize()
        {
            BuildChoices();
            ElementRegistry.Changed -= OnElementRegistryChanged;
            ElementRegistry.Changed += OnElementRegistryChanged;
            PluginConfigurator config = PluginConfigurator.Create(Plugin.Name, Plugin.Guid);
            ApplyConfiguratorIcon(config);
            new ConfigHeader(config.rootPanel,
                "Use the in-game ELEMENT LOADOUT menu for the 15-slot arsenal. " +
                "These selectors remain as a compatibility/debug fallback.");

            enabled = new BoolField(config.rootPanel, "Enable Elemental Battlegrounds arsenal", "enabled", true);

            openElementLoadoutButton = new ButtonField(config.rootPanel, "Open Element Loadout", "openElementLoadout");
            openElementLoadoutButton.onClick += () =>
            {
                if (Enabled)
                    ElementLoadoutMenuRuntime.OpenFromAnywhere();
            };
            SyncLoadoutMenuAvailability();

            WeaponTuning.Initialize(config);

            CreateFamilyPanel(config, WeaponFamily.Revolver, "Revolvers (fallback)");
            CreateFamilyPanel(config, WeaponFamily.Shotgun, "Close weapons (fallback)");
            CreateFamilyPanel(config, WeaponFamily.Rapid, "Rapid weapons (fallback)");
            CreateFamilyPanel(config, WeaponFamily.Ultimate, "Ultimates (fallback)");
            CreateFamilyPanel(config, WeaponFamily.Explosive, "Explosives (fallback)");

            ButtonField apply = new ButtonField(config.rootPanel, "Apply loadout now (cheats only)", "applyLoadout");
            apply.onClick += ApplyNowIfCheats;
        }

        internal static void SyncLoadoutMenuAvailability()
        {
            if (openElementLoadoutButton != null)
                openElementLoadoutButton.interactable = Enabled;
        }

        private static void ApplyNowIfCheats()
        {
            CheatsController cheats = MonoSingleton<CheatsController>.Instance;
            if (cheats == null || !cheats.cheatsEnabled)
            {
                Plugin.LogSource?.LogWarning("Mid-level EB loadout swapping requires ULTRAKILL cheats. Changes will apply automatically next level.");
                return;
            }

            GunSetter setter = UnityEngine.Object.FindObjectOfType<GunSetter>();
            if (setter != null)
                setter.ResetWeapons(false);
        }

        private static void OnElementRegistryChanged()
        {
            BuildChoices();
        }

        private static void BuildChoices()
        {
            choices.Clear();
            choices[WeaponFamily.Revolver] = ComposeChoices(
                V("rev_piercer_std", "Vanilla — Piercer Revolver", 0, 0),
                V("rev_marksman_std", "Vanilla — Marksman Revolver", 2, 0),
                V("rev_sharp_std", "Vanilla — Sharpshooter Revolver", 1, 0),
                V("rev_piercer_alt", "Vanilla — Piercer Slab Revolver", 0, 1),
                V("rev_marksman_alt", "Vanilla — Marksman Slab Revolver", 2, 1),
                V("rev_sharp_alt", "Vanilla — Sharpshooter Slab Revolver", 1, 1));
            choices[WeaponFamily.Shotgun] = ComposeChoices(
                V("sho_core_std", "Vanilla — Core Eject Shotgun", 0, 0),
                V("sho_pump_std", "Vanilla — Pump Charge Shotgun", 1, 0),
                V("sho_saw_std", "Vanilla — Sawed-On Shotgun", 2, 0),
                V("sho_core_alt", "Vanilla — Core Eject Jackhammer", 0, 1),
                V("sho_pump_alt", "Vanilla — Pump Charge Jackhammer", 1, 1),
                V("sho_saw_alt", "Vanilla — Sawed-On Jackhammer", 2, 1));
            choices[WeaponFamily.Rapid] = ComposeChoices(
                V("nai_attr_std", "Vanilla — Attractor Nailgun", 0, 0),
                V("nai_over_std", "Vanilla — Overheat Nailgun", 1, 0),
                V("nai_jump_std", "Vanilla — Jumpstart Nailgun", 2, 0),
                V("nai_attr_alt", "Vanilla — Attractor Sawblade Launcher", 0, 1),
                V("nai_over_alt", "Vanilla — Overheat Sawblade Launcher", 1, 1),
                V("nai_jump_alt", "Vanilla — Jumpstart Sawblade Launcher", 2, 1));
            choices[WeaponFamily.Ultimate] = ComposeChoices(
                V("rai_electric_std", "Vanilla — Electric Railcannon", 0, 0),
                V("rai_screw_std", "Vanilla — Screwdriver Railcannon", 1, 0),
                V("rai_mal_std", "Vanilla — Malicious Railcannon", 2, 0));
            choices[WeaponFamily.Explosive] = ComposeChoices(
                V("rock_freeze_std", "Vanilla — Freezeframe Rocket Launcher", 0, 0, ExplosiveIntegrationMode.ForceNative),
                V("rock_srs_std", "Vanilla — S.R.S. Rocket Launcher", 1, 0, ExplosiveIntegrationMode.ForceNative),
                V("rock_fire_std", "Vanilla — Firestarter Rocket Launcher", 2, 0, ExplosiveIntegrationMode.ForceNative),
                V("gl_blue", "Grenade Launcher — Blue", 0, 0, ExplosiveIntegrationMode.ForceGrenade),
                V("gl_green", "Grenade Launcher — Green", 1, 0, ExplosiveIntegrationMode.ForceGrenade),
                V("gl_red", "Grenade Launcher — Red", 2, 0, ExplosiveIntegrationMode.ForceGrenade));
        }

        private static LoadoutChoice[] ComposeChoices(params LoadoutChoice[] directChoices)
        {
            List<LoadoutChoice> result = new List<LoadoutChoice> { None() };
            foreach (ElementRegistryRecord element in ElementRegistry.Entries)
            {
                if (element.selectable)
                    result.Add(EB(element));
            }
            if (directChoices != null)
                result.AddRange(directChoices);
            return result.ToArray();
        }

        private static LoadoutChoice None()
        {
            return new LoadoutChoice("none", "None", ElementId.None);
        }

        private static LoadoutChoice Missing(string id)
        {
            return new LoadoutChoice(id, "MISSING: " + id, ElementId.None, missing: true);
        }

        private static LoadoutChoice EB(ElementRegistryRecord element)
        {
            return new LoadoutChoice(element.stableId, element.displayName, element.builtInId, elementChoice: true);
        }

        private static LoadoutChoice V(
            string id,
            string display,
            int variant,
            int form,
            ExplosiveIntegrationMode explosiveMode = ExplosiveIntegrationMode.Default)
        {
            return new LoadoutChoice(id, display, ElementId.Vanilla, variant, form, explosiveMode);
        }

        private static void ApplyConfiguratorIcon(PluginConfigurator config)
        {
            try
            {
                using (Stream stream = typeof(Plugin).Assembly.GetManifestResourceStream("ElementalBattlegroundsMod.icon.png"))
                {
                    if (stream == null)
                        return;
                    byte[] data = new byte[stream.Length];
                    int offset = 0;
                    while (offset < data.Length)
                    {
                        int read = stream.Read(data, offset, data.Length - offset);
                        if (read <= 0)
                            break;
                        offset += read;
                    }
                    if (offset != data.Length)
                        return;

                    configIconTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!ImageConversion.LoadImage(configIconTexture, data, false))
                    {
                        UnityEngine.Object.Destroy(configIconTexture);
                        configIconTexture = null;
                        return;
                    }
                    configIconTexture.name = "Elemental Battlegrounds Icon";
                    configIconSprite = Sprite.Create(configIconTexture,
                        new Rect(0f, 0f, configIconTexture.width, configIconTexture.height),
                        new Vector2(0.5f, 0.5f));
                    configIconSprite.name = "Elemental Battlegrounds Icon";
                    config.icon = configIconSprite;
                }
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("Could not load Plugin Configurator icon: " + exception.Message);
            }
        }

        private static void CreateFamilyPanel(PluginConfigurator config, WeaponFamily family, string title)
        {
            ConfigPanel panel = new ConfigPanel(config.rootPanel, title, "family_" + family.ToString().ToLowerInvariant());
            LoadoutChoice[] familyChoices = choices[family];
            string[] names = new string[familyChoices.Length];
            for (int i = 0; i < names.Length; i++)
                names[i] = familyChoices[i].displayName;
            fallbackDisplayOptions[(int)family] = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < 3; i++)
            {
                // Fresh installs start with a neutral sentinel value. Once ULTRAKILL's
                // terminal/save data is available, EnsureWorkingLoadoutInitialized copies
                // the player's actual terminal loadout into these fields. Existing installs
                // retain their already-saved values.
                fields[(int)family, i] = new StringListField(
                    panel,
                    "Position " + (i + 1),
                    family.ToString().ToLowerInvariant() + "_" + i,
                    names,
                    "None");
            }
        }

        private static string InitializationMarkerPath =>
            Path.Combine(Paths.ConfigPath, "ElementalBattlegrounds.loadout-initialized");

        internal static bool EnsureWorkingLoadoutInitialized()
        {
            if (workingLoadoutInitialized)
                return true;

            bool hadIdStore = WorkingLoadoutStore.LoadIfPresent();
            if (hadIdStore)
            {
                ReconcileFallbackFieldsWithIdStore();
                workingLoadoutInitialized = true;
                return true;
            }

            // Fresh installs need ULTRAKILL's terminal/save state before the initial 15 choices
            // can be captured. Existing installs can migrate their old display-name fields.
            bool markerExists = File.Exists(InitializationMarkerPath);
            bool hasLegacySelections = HasAnyLegacyFallbackSelection();
            if (!markerExists && !hasLegacySelections && MonoSingleton<PrefsManager>.Instance == null)
                return false;

            WorkingLoadoutStore.EnsureCreated();
            workingLoadoutInitialized = true;
            if (markerExists || hasLegacySelections)
            {
                MigrateLegacyFallbackFieldsToIds();
                Plugin.LogSource?.LogInfo("Migrated EB working loadout to stable choice IDs.");
            }
            else
            {
                RestoreTerminalLoadout();
                Plugin.LogSource?.LogInfo("Initialized EB working loadout from the current ULTRAKILL terminal loadout.");
            }
            WorkingLoadoutStore.Save();

            try
            {
                File.WriteAllText(InitializationMarkerPath, "initialized");
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("Could not write EB loadout initialization marker: " + exception.Message);
            }
            return true;
        }

        private static bool HasAnyLegacyFallbackSelection()
        {
            for (int family = 0; family < 5; family++)
            {
                for (int position = 0; position < 3; position++)
                {
                    string value = fields[family, position]?.value;
                    if (!string.IsNullOrEmpty(value) && !string.Equals(value, "None", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private static void MigrateLegacyFallbackFieldsToIds()
        {
            for (int family = 0; family < 5; family++)
            {
                WeaponFamily weaponFamily = (WeaponFamily)family;
                for (int position = 0; position < 3; position++)
                {
                    string value = fields[family, position]?.value;
                    LoadoutChoice choice = ResolveChoiceFromDisplay(weaponFamily, value, position);
                    bool mirrored = SetFallbackFieldOnly(weaponFamily, position, choice.displayName);
                    WorkingLoadoutStore.Set(weaponFamily, position, choice.id, mirrored ? choice.displayName : null, false);
                }
            }
        }

        private static void ReconcileFallbackFieldsWithIdStore()
        {
            for (int family = 0; family < 5; family++)
            {
                WeaponFamily weaponFamily = (WeaponFamily)family;
                for (int position = 0; position < 3; position++)
                {
                    string storedId = WorkingLoadoutStore.GetChoiceId(weaponFamily, position);
                    string storedMirror = WorkingLoadoutStore.GetDisplayMirror(weaponFamily, position);
                    string fieldValue = fields[family, position]?.value;

                    if (string.IsNullOrEmpty(storedId))
                    {
                        LoadoutChoice migrated = ResolveChoiceFromDisplay(weaponFamily, fieldValue, position);
                        bool mirrored = SetFallbackFieldOnly(weaponFamily, position, migrated.displayName);
                        WorkingLoadoutStore.Set(weaponFamily, position, migrated.id, mirrored ? migrated.displayName : null, false);
                        continue;
                    }

                    // Unknown IDs are intentionally preserved. The built-in fallback dropdown
                    // cannot represent a missing future addon, so its current value becomes a
                    // mirror only: it cannot erase the missing ID merely because the addon is
                    // absent, but a later deliberate dropdown edit can still replace that ID.
                    if (!TryGetChoiceById(weaponFamily, storedId, out LoadoutChoice storedChoice))
                    {
                        if (string.IsNullOrEmpty(storedMirror))
                            WorkingLoadoutStore.Set(weaponFamily, position, storedId, fieldValue, false);
                        else if (!string.Equals(fieldValue, storedMirror, StringComparison.OrdinalIgnoreCase) &&
                                 TryResolveChoiceFromDisplay(weaponFamily, fieldValue, out LoadoutChoice replacement))
                            WorkingLoadoutStore.Set(weaponFamily, position, replacement.id, replacement.displayName, false);
                        continue;
                    }

                    // If the user changed PluginConfigurator's compatibility dropdown since the
                    // last mirror write, honor that edit and migrate it back to the ID store.
                    if (!string.IsNullOrEmpty(storedMirror) &&
                        !string.Equals(fieldValue, storedMirror, StringComparison.OrdinalIgnoreCase) &&
                        TryResolveChoiceFromDisplay(weaponFamily, fieldValue, out LoadoutChoice edited))
                    {
                        WorkingLoadoutStore.Set(weaponFamily, position, edited.id, edited.displayName, false);
                        continue;
                    }

                    bool storedMirrored = SetFallbackFieldOnly(weaponFamily, position, storedChoice.displayName);
                    WorkingLoadoutStore.Set(weaponFamily, position, storedChoice.id, storedMirrored ? storedChoice.displayName : null, false);
                }
            }
            WorkingLoadoutStore.Save();
        }

        internal static LoadoutChoice[] GetChoices(WeaponFamily family)
        {
            return choices.TryGetValue(family, out LoadoutChoice[] result) ? result : Array.Empty<LoadoutChoice>();
        }

        internal static LoadoutChoice[] GetUniqueChoices(WeaponFamily family)
        {
            LoadoutChoice[] result = new LoadoutChoice[3];
            HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < 3; i++)
            {
                LoadoutChoice selected = GetChoice(family, i);
                if (selected.IsNone || selected.IsMissing || used.Add(selected.id))
                {
                    result[i] = selected;
                    continue;
                }

                LoadoutChoice fallback = FindFirstUnused(family, used, selected.IsVanilla);
                result[i] = fallback;
                if (!fallback.IsNone)
                    used.Add(fallback.id);
                SetField(family, i, fallback);
                Plugin.LogSource?.LogWarning("Duplicate " + family + " choice '" + selected.displayName + "' replaced with '" + fallback.displayName + "'.");
            }
            return result;
        }

        private static LoadoutChoice FindFirstUnused(WeaponFamily family, HashSet<string> used, bool preferVanilla)
        {
            foreach (LoadoutChoice choice in choices[family])
                if (!choice.IsNone && choice.IsVanilla == preferVanilla && !used.Contains(choice.id))
                    return choice;
            foreach (LoadoutChoice choice in choices[family])
                if (!choice.IsNone && !used.Contains(choice.id))
                    return choice;
            return choices[family][0];
        }

        internal static LoadoutChoice GetChoice(WeaponFamily family, int position)
        {
            if (!Enabled)
                return FirstVanilla(family);
            if (position < 0 || position >= 3)
                return FirstVanilla(family);

            if (!workingLoadoutInitialized && !EnsureWorkingLoadoutInitialized())
                return ResolveChoiceFromDisplay(family, fields[(int)family, position]?.value, position);

            string storedId = WorkingLoadoutStore.GetChoiceId(family, position);
            if (!string.IsNullOrEmpty(storedId))
            {
                if (!TryGetChoiceById(family, storedId, out LoadoutChoice storedChoice))
                    return Missing(storedId);

                // Keep the PluginConfigurator fallback usable while the stable-ID file remains
                // canonical. A field edit made during this session is detected by its mirror.
                string mirror = WorkingLoadoutStore.GetDisplayMirror(family, position);
                string fieldValue = fields[(int)family, position]?.value;
                if (!string.IsNullOrEmpty(mirror) &&
                    !string.Equals(fieldValue, mirror, StringComparison.OrdinalIgnoreCase) &&
                    TryResolveChoiceFromDisplay(family, fieldValue, out LoadoutChoice edited))
                {
                    WorkingLoadoutStore.Set(family, position, edited.id, edited.displayName);
                    return edited;
                }
                return storedChoice;
            }

            LoadoutChoice fallback = ResolveChoiceFromDisplay(family, fields[(int)family, position]?.value, position);
            WorkingLoadoutStore.Set(family, position, fallback.id, fallback.displayName);
            return fallback;
        }

        private static LoadoutChoice ResolveChoiceFromDisplay(WeaponFamily family, string value, int position)
        {
            if (TryResolveChoiceFromDisplay(family, value, out LoadoutChoice choice))
                return choice;

            // Older builds defaulted directly to Fire/Water/Grass by array position. Retain the
            // historical migration only when no recognizable display value exists.
            ElementId fallbackElement = position == 0 ? ElementId.Fire : position == 1 ? ElementId.Water : ElementId.Grass;
            string fallbackId = ElementRegistry.StableId(fallbackElement);
            if (!string.IsNullOrEmpty(fallbackId) && TryGetChoiceById(family, fallbackId, out choice))
                return choice;
            return FirstVanilla(family);
        }

        private static bool TryResolveChoiceFromDisplay(WeaponFamily family, string value, out LoadoutChoice result)
        {
            if (string.Equals(value, "Vanilla", StringComparison.OrdinalIgnoreCase))
            {
                result = FirstVanilla(family);
                return true;
            }

            // Preserve the three 0.0.18 Revolver-alt display strings after giving each
            // alternate form a proper individual weapon name in the new menu.
            if (family == WeaponFamily.Revolver)
            {
                if (string.Equals(value, "Vanilla — Alternate Piercer / Slab", StringComparison.OrdinalIgnoreCase))
                    value = "Vanilla — Piercer Slab Revolver";
                else if (string.Equals(value, "Vanilla — Alternate Marksman / Slab", StringComparison.OrdinalIgnoreCase))
                    value = "Vanilla — Marksman Slab Revolver";
                else if (string.Equals(value, "Vanilla — Alternate Sharpshooter / Slab", StringComparison.OrdinalIgnoreCase))
                    value = "Vanilla — Sharpshooter Slab Revolver";
            }

            foreach (LoadoutChoice choice in GetChoices(family))
            {
                if (string.Equals(choice.displayName, value, StringComparison.OrdinalIgnoreCase))
                {
                    result = choice;
                    return true;
                }
            }
            result = default(LoadoutChoice);
            return false;
        }

        internal static string GetChoiceId(WeaponFamily family, int position)
        {
            return GetChoice(family, position).id;
        }

        internal static bool TryGetChoiceById(WeaponFamily family, string id, out LoadoutChoice result)
        {
            string canonical = ElementRegistry.Canonicalize(id);
            foreach (LoadoutChoice choice in GetChoices(family))
            {
                if (string.Equals(choice.id, canonical, StringComparison.OrdinalIgnoreCase))
                {
                    result = choice;
                    return true;
                }
            }
            result = default(LoadoutChoice);
            return false;
        }

        internal static void SetChoice(WeaponFamily family, int position, LoadoutChoice choice, bool swapDuplicate = true)
        {
            if (position < 0 || position >= 3)
                return;

            if (swapDuplicate && !choice.IsNone)
            {
                for (int other = 0; other < 3; other++)
                {
                    if (other == position)
                        continue;
                    LoadoutChoice otherChoice = GetChoice(family, other);
                    if (!string.Equals(otherChoice.id, choice.id, StringComparison.OrdinalIgnoreCase))
                        continue;

                    LoadoutChoice oldChoice = GetChoice(family, position);
                    SetField(family, other, oldChoice);
                    break;
                }
            }

            SetField(family, position, choice);
        }

        internal static bool SetChoiceId(WeaponFamily family, int position, string id, bool swapDuplicate = false)
        {
            if (TryGetChoiceById(family, id, out LoadoutChoice choice))
            {
                SetChoice(family, position, choice, swapDuplicate);
                return true;
            }

            // Preserve a namespaced choice even when its future addon is currently missing.
            // Runtime construction will make that slot inert without destroying the saved ID.
            string canonical = ElementRegistry.Canonicalize(id);
            if (ElementRegistry.IsNamespacedId(canonical))
            {
                SetChoice(family, position, Missing(canonical), false);
                return true;
            }
            return false;
        }

        private static void SetField(WeaponFamily family, int position, LoadoutChoice choice)
        {
            bool mirrored = SetFallbackFieldOnly(family, position, choice.displayName);
            if (workingLoadoutInitialized || WorkingLoadoutStore.IsLoaded)
            {
                // Missing addon choices cannot be represented in PluginConfigurator's fixed
                // dropdown. Preserve whatever value the dropdown currently shows as the mirror
                // so a later deliberate edit can still replace the missing stable ID.
                string displayMirror = mirrored
                    ? choice.displayName
                    : choice.IsMissing ? fields[(int)family, position]?.value : null;
                WorkingLoadoutStore.Set(family, position, choice.id, displayMirror);
            }
        }

        private static bool SetFallbackFieldOnly(WeaponFamily family, int position, string displayName)
        {
            StringListField field = fields[(int)family, position];
            HashSet<string> supported = fallbackDisplayOptions[(int)family];
            if (field == null || string.IsNullOrEmpty(displayName) || supported == null || !supported.Contains(displayName))
                return false;
            field.value = displayName;
            return true;
        }

        internal static void RestoreTerminalLoadout()
        {
            string[] prefixes = { "rev", "sho", "nai", "rai", "rock" };
            for (int familyIndex = 0; familyIndex < 5; familyIndex++)
            {
                WeaponFamily family = (WeaponFamily)familyIndex;
                string prefix = prefixes[familyIndex];
                string defaultOrder = prefix == "rev" ? "1324" : "1234";
                string order = MonoSingleton<PrefsManager>.Instance != null
                    ? MonoSingleton<PrefsManager>.Instance.GetString("weapon." + prefix + ".order", defaultOrder)
                    : defaultOrder;
                if (string.IsNullOrEmpty(order) || order.Length < 3)
                    order = defaultOrder;

                List<Tuple<int, int>> ranked = new List<Tuple<int, int>>();
                for (int variant = 0; variant < 3; variant++)
                {
                    int rank = variant < order.Length && char.IsDigit(order[variant]) ? order[variant] - '0' : variant + 1;
                    ranked.Add(Tuple.Create(rank, variant));
                }
                ranked.Sort((a, b) => a.Item1.CompareTo(b.Item1));

                int output = 0;
                foreach (Tuple<int, int> item in ranked)
                {
                    int variant = item.Item2;
                    string weaponKey = prefix + variant;
                    int equipped = MonoSingleton<PrefsManager>.Instance != null
                        ? MonoSingleton<PrefsManager>.Instance.GetInt("weapon." + weaponKey, 1)
                        : 1;
                    if (equipped <= 0 || GameProgressSaver.CheckGear(weaponKey) <= 0)
                        continue;

                    int form = equipped > 1 && SupportsAlternateForm(family) &&
                               GameProgressSaver.CheckGear(prefix + "alt") > 0 ? 1 : 0;
                    ExplosiveIntegrationMode mode = family == WeaponFamily.Explosive && GrenadeLauncherCompat.IsTerminalGrenadeModeEnabled(variant)
                        ? ExplosiveIntegrationMode.ForceGrenade
                        : ExplosiveIntegrationMode.ForceNative;
                    LoadoutChoice choice = FindDirectChoice(family, variant, form, mode);
                    if (!string.IsNullOrEmpty(choice.id))
                    {
                        // Keep the menu's "vanilla weapon belongs to an element" rule
                        // consistent when restoring from the terminal too. Implemented homes
                        // resolve to their selectable element identity; unimplemented/planned
                        // homes remain direct vanilla choices.
                        if (WeaponHomeRegistry.TryResolveImplementedElementChoice(family, choice, out LoadoutChoice elementChoice))
                            choice = elementChoice;
                        SetChoice(family, output++, choice, false);
                    }
                }

                while (output < 3)
                {
                    SetChoiceId(family, output, "none", false);
                    output++;
                }
            }
        }

        private static bool SupportsAlternateForm(WeaponFamily family)
        {
            return family == WeaponFamily.Revolver || family == WeaponFamily.Shotgun || family == WeaponFamily.Rapid;
        }

        private static LoadoutChoice FindDirectChoice(
            WeaponFamily family,
            int variant,
            int form,
            ExplosiveIntegrationMode explosiveMode)
        {
            foreach (LoadoutChoice choice in GetChoices(family))
            {
                if (!choice.IsVanilla || choice.vanillaVariant != variant || choice.vanillaForm != form)
                    continue;
                if (family != WeaponFamily.Explosive || choice.explosiveMode == explosiveMode)
                    return choice;
            }
            return default(LoadoutChoice);
        }

        private static LoadoutChoice FirstVanilla(WeaponFamily family)
        {
            foreach (LoadoutChoice choice in choices[family])
                if (choice.IsVanilla)
                    return choice;
            return choices[family][0];
        }
    }
}
