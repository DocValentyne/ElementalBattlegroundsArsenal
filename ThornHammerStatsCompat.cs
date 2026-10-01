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
    internal static class StormThornHammerStatsCompat
    {
        private const string ModuleManagerTypeName = "ThornClient.Managers.ModuleManager";

        private static Type moduleManagerType;
        private static PropertyInfo moduleItemsProperty;
        private static Type hammerStatsType;
        private static FieldInfo contentObjectField;
        private static GameObject attachedRoot;
        private static bool managerResolutionAttempted;
        private static readonly HashSet<string> LoggedStages = new HashSet<string>();

        internal static bool Installed => attachedRoot != null;

        internal static void TryInstall(Harmony harmony)
        {
            LogOnce("retry-active", "diagnostics enabled; Hammer Stats attach retry is active.");
            ResolveModuleManagerOnce();
            TryAttachToLiveHammerStats();
        }

        internal static void TryAttachToLiveHammerStats()
        {
            if (attachedRoot != null)
                return;

            ResolveModuleManagerOnce();
            if (moduleItemsProperty == null)
                return;

            IEnumerable items;
            try
            {
                items = moduleItemsProperty.GetValue(null, null) as IEnumerable;
            }
            catch (Exception exception)
            {
                LogOnce("module-items-read-failed", "ModuleManager.Items read failed: " + exception.Message, true);
                return;
            }

            if (items == null)
            {
                LogOnce("module-items-null", "ModuleManager.Items exists but currently returned null.", true);
                return;
            }

            object hammerStatsModule = null;
            int itemCount = 0;
            foreach (object item in items)
            {
                if (item == null)
                    continue;
                itemCount++;
                Type itemType = item.GetType();
                string typeName = itemType.Name ?? string.Empty;
                string fullName = itemType.FullName ?? string.Empty;
                if (typeName == "HammerStats" || fullName.EndsWith(".HammerStats", StringComparison.Ordinal))
                {
                    hammerStatsModule = item;
                    hammerStatsType = itemType;
                    break;
                }
            }

            LogOnce("module-list", "ModuleManager.Items scanned; count at first Hammer Stats probe=" + itemCount + ".");
            if (hammerStatsModule == null)
            {
                LogOnce("module-missing", "Hammer Stats was not found in ModuleManager.Items yet.");
                return;
            }

            LogOnce("module-found", "found live Hammer Stats module type " +
                (hammerStatsType.FullName ?? hammerStatsType.Name) + " in assembly " +
                hammerStatsType.Assembly.GetName().Name + ".");

            if (contentObjectField == null)
            {
                contentObjectField = hammerStatsType.GetField("_contentObject",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                LogOnce("content-field", "HammerStats._contentObject field " +
                    (contentObjectField != null ? "resolved." : "NOT found."),
                    contentObjectField == null);
            }

            if (contentObjectField == null)
                return;

            GameObject root;
            try
            {
                root = contentObjectField.GetValue(hammerStatsModule) as GameObject;
            }
            catch (Exception exception)
            {
                LogOnce("content-read-failed", "Hammer Stats _contentObject read failed: " + exception.Message, true);
                return;
            }

            if (root == null)
            {
                LogOnce("content-null", "Hammer Stats module exists, but _contentObject is still null; retrying after Thorn creates the HUD.");
                return;
            }

            TryAttach(root, "runtime HammerStats._contentObject");
        }

        private static bool TryAttach(GameObject root, string route)
        {
            if (root == null)
                return false;

            Transform variants = root.transform.Find("Variants");
            if (variants == null)
            {
                LogOnce("variants-missing-" + route, route + " found root '" + root.name +
                    "', but it has no Variants child.", true);
                return false;
            }

            int cellCount = 0;
            for (int i = 0; i < 3; i++)
            {
                if (variants.Find(i.ToString()) != null)
                    cellCount++;
            }

            if (cellCount < 3)
            {
                LogOnce("cells-missing-" + route, route + " found Variants, but only " +
                    cellCount + "/3 expected cells exist.", true);
                return false;
            }

            StormThornHammerStatsBridge bridge = root.GetComponent<StormThornHammerStatsBridge>();
            if (bridge == null)
                bridge = root.AddComponent<StormThornHammerStatsBridge>();
            bridge.Initialize();
            attachedRoot = root;
            LogOnce("attached", "attached EB Close-slot integration through " + route +
                " on root '" + root.name + "'.");
            return true;
        }

        private static void ResolveModuleManagerOnce()
        {
            if (managerResolutionAttempted)
                return;
            managerResolutionAttempted = true;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<string> thornAssemblies = new List<string>();
            foreach (Assembly assembly in assemblies)
            {
                string assemblyName = assembly.GetName().Name ?? string.Empty;
                if (assemblyName.IndexOf("thorn", StringComparison.OrdinalIgnoreCase) >= 0)
                    thornAssemblies.Add(assemblyName);

                if (moduleManagerType == null)
                    moduleManagerType = assembly.GetType(ModuleManagerTypeName, false);
            }

            LogOnce("assemblies", "loaded assemblies containing 'Thorn': " +
                (thornAssemblies.Count == 0 ? "<none>" : string.Join(", ", thornAssemblies.ToArray())));

            if (moduleManagerType == null)
            {
                LogOnce("manager-type-missing", "could not resolve " + ModuleManagerTypeName +
                    "; Thorn Hammer Stats integration is disabled for this session.", true);
                return;
            }

            LogOnce("manager-type", "resolved " + ModuleManagerTypeName + " from " +
                moduleManagerType.Assembly.GetName().Name + ".");
            moduleItemsProperty = moduleManagerType.GetProperty("Items",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            LogOnce("items-property", "ModuleManager.Items property " +
                (moduleItemsProperty != null ? "resolved." : "NOT found."), moduleItemsProperty == null);
        }

        private static void LogOnce(string key, string message, bool warning = false)
        {
            if (!LoggedStages.Add(key))
                return;
            if (warning)
                Plugin.LogSource?.LogWarning("[EB Thorn] " + message);
            else
                Plugin.LogSource?.LogInfo("[EB Thorn] " + message);
        }

        internal static void LogBridgeMapping(string signature)
        {
            if (string.IsNullOrEmpty(signature))
                return;
            Plugin.LogSource?.LogInfo("[EB Thorn] applied EB Close mapping: " + signature);
        }

        internal static void RestoreTerminalLayout(GameObject root)
        {
            if (root == null)
                return;

            try
            {
                MonoBehaviour[] components = root.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour component in components)
                {
                    if (component == null || component.GetType().Name != "HammerCooldownController")
                        continue;

                    MethodInfo sync = component.GetType().GetMethod("SyncVariantLayout",
                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    if (sync == null)
                    {
                        LogOnce("sync-method-missing", "live HammerCooldownController has no SyncVariantLayout method.", true);
                        return;
                    }

                    sync.Invoke(component, null);
                    LogOnce("terminal-restored", "EB disabled; Thorn terminal-based Hammer Stats layout restored.");
                    return;
                }

                LogOnce("restore-controller-missing", "could not find HammerCooldownController on the live Hammer Stats root while restoring terminal layout.", true);
            }
            catch (Exception exception)
            {
                LogOnce("restore-failed", "terminal-layout restore failed: " + exception.Message, true);
            }
        }

        internal static void NotifyBridgeDestroyed(GameObject root)
        {
            if (attachedRoot == root)
            {
                attachedRoot = null;
                contentObjectField = null;
                hammerStatsType = null;
                LogOnce("bridge-destroyed", "attached Hammer Stats root was destroyed; retry loop will look for its replacement.");
            }
        }
    }

    internal sealed class StormThornHammerStatsInstallRetry : MonoBehaviour
    {
        private float nextTry;

        private void Update()
        {
            if (Time.unscaledTime < nextTry)
                return;
            nextTry = Time.unscaledTime + 1f;
            // Cheap once-per-second optional integration probe. Prefer Thorn's active controller,
            // then fall back to its small module registry; never scan all Resources.
            StormThornHammerStatsCompat.TryAttachToLiveHammerStats();
        }
    }

    internal sealed class StormThornHammerStatsBridge : MonoBehaviour
    {
        private bool wasEbEnabled;
        private bool loggedMissingVariants;
        private string lastMappingSignature;

        internal void Initialize()
        {
            wasEbEnabled = false;
        }

        private void OnDestroy()
        {
            StormThornHammerStatsCompat.NotifyBridgeDestroyed(gameObject);
        }

        private void LateUpdate()
        {
            if (!EBSettings.Enabled)
            {
                if (wasEbEnabled)
                    StormThornHammerStatsCompat.RestoreTerminalLayout(gameObject);
                wasEbEnabled = false;
                return;
            }
            wasEbEnabled = true;

            Transform variants = transform.Find("Variants");
            if (variants == null)
            {
                if (!loggedMissingVariants)
                {
                    loggedMissingVariants = true;
                    Plugin.LogSource?.LogWarning("[EB Thorn] bridge is attached, but the live HammerLayout no longer has a Variants child.");
                }
                return;
            }

            GunControl gunControl = MonoSingleton<GunControl>.Instance;
            List<GameObject> closeSlot = gunControl != null && gunControl.slots != null &&
                gunControl.slots.Count > (int)WeaponFamily.Shotgun
                ? gunControl.slots[(int)WeaponFamily.Shotgun]
                : null;
            WeaponCharges charges = MonoSingleton<WeaponCharges>.Instance;
            bool layoutChanged = false;
            string[] mapping = new string[3];

            // Thorn normally maps these cells from terminal weapon.sho* preferences. While EB owns
            // the loadout, reinterpret the same three cells as EB Close positions 1/2/3. This is
            // intentionally based on GunControl's live slot objects, not terminal configuration.
            for (int displayIndex = 0; displayIndex < 3; displayIndex++)
            {
                Transform cellTransform = variants.Find(displayIndex.ToString());
                if (cellTransform == null)
                    continue;
                GameObject cell = cellTransform.gameObject;

                GameObject weapon = closeSlot != null && displayIndex < closeSlot.Count
                    ? closeSlot[displayIndex]
                    : null;
                ShotgunHammer hammer = weapon != null ? weapon.GetComponent<ShotgunHammer>() : null;
                ElementalSlotMarker marker = weapon != null ? MarkerUtility.Find(weapon) : null;
                bool show = hammer != null;
                mapping[displayIndex] = weapon == null
                    ? (displayIndex + 1) + "=<empty>"
                    : (displayIndex + 1) + "=" + (marker != null ? marker.element.ToString() : "Vanilla") +
                        (hammer != null ? "/Jackhammer-v" + hammer.variation : "/Shotgun");
                if (cell.activeSelf != show)
                {
                    cell.SetActive(show);
                    layoutChanged = true;
                }
                if (!show)
                    continue;

                int variation = Mathf.Clamp(hammer.variation, 0, 2);
                float nativeReady = 1f;
                if (charges != null && charges.shoaltcooldowns != null && variation < charges.shoaltcooldowns.Length)
                    nativeReady = 1f - Mathf.Clamp01(charges.shoaltcooldowns[variation] / 7f);

                ElementalJackhammerPrimaryCooldownState primaryState =
                    weapon != null ? weapon.GetComponent<ElementalJackhammerPrimaryCooldownState>() : null;
                float ready = primaryState != null ? primaryState.ReadyFraction : nativeReady;
                // Color identity comes from the loadout marker immediately, not from the custom
                // controller registry. Controllers may not have ever been enabled yet when Thorn
                // creates its HUD, which previously left cells blue/red/green until first equip.
                Color accent = marker != null && marker.IsCustom
                    ? ElementPalette.Accent(marker.elementId, marker.element)
                    : GetNativeVariationColor(variation);
                if (ElementalJackhammerHudRegistry.TryGet(hammer, out ElementalJackhammerHudState state))
                {
                    // Thorn's Hammer Stats bar is a Jackhammer primary/red-hit heat meter.
                    // EB only remaps which live Close-slot weapon the cell represents and its
                    // element color; custom Alt-Fire cooldowns must not replace that native meter.
                    accent = ElementPalette.Accent(state.element);
                }

                Image fill = cellTransform.Find("Fill")?.GetComponent<Image>();
                if (fill != null)
                {
                    fill.fillAmount = ready;
                    fill.color = WithAlpha(accent, fill.color.a);
                }
                Image background = cell.GetComponent<Image>();
                if (background != null)
                    background.color = WithAlpha(Shade(accent, 0.20f), background.color.a);
            }

            if (layoutChanged && variants is RectTransform variantsRoot)
                LayoutRebuilder.ForceRebuildLayoutImmediate(variantsRoot);

            string signature = string.Join(" | ", mapping);
            if (signature != lastMappingSignature)
            {
                lastMappingSignature = signature;
                StormThornHammerStatsCompat.LogBridgeMapping(signature);
            }
        }

        private static Color GetNativeVariationColor(int variation)
        {
            ColorBlindSettings settings = MonoSingleton<ColorBlindSettings>.Instance;
            if (settings != null && settings.variationColors != null && variation >= 0 && variation < settings.variationColors.Length)
                return settings.variationColors[variation];
            return Color.white;
        }

        private static Color Shade(Color source, float multiplier)
        {
            return new Color(source.r * multiplier, source.g * multiplier, source.b * multiplier, source.a);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }

}
