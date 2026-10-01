using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace ElementalBattlegroundsMod
{
    internal static class ArsenalLoadout
    {
        private static bool rebuilding;

        internal static void Rebuild(GunSetter setter, bool firstTime)
        {
            if (rebuilding || setter == null || setter.gunc == null || !EBSettings.Enabled)
                return;

            rebuilding = true;
            try
            {
                SlotResourceOwnerResolver.ClearCache();
                GunControl gc = setter.gunc;
                // ResetWeapons has just rebuilt ULTRAKILL's native slot lists in the exact
                // order selected by the player at the terminal. Capture that ordering before
                // EB replaces the objects so WeaponVariantBinds can keep referring to weapon
                // names without losing the player's terminal-position semantics.
                WeaponVariantBindsCompat.CaptureNativeOrder(gc);

                for (int familyIndex = 0; familyIndex < 5 && familyIndex < gc.slots.Count; familyIndex++)
                {
                    WeaponFamily family = (WeaponFamily)familyIndex;
                    LoadoutChoice[] selected = EBSettings.GetUniqueChoices(family);
                    List<GameObject> slot = gc.slots[familyIndex];
                    foreach (GameObject old in slot)
                    {
                        if (old != null)
                            UnityEngine.Object.Destroy(old);
                    }
                    slot.Clear();

                    for (int position = 0; position < 3; position++)
                    {
                        LoadoutChoice choice = selected[position];
                        GameObject instance;

                        if (choice.IsNone || choice.IsMissing)
                        {
                            // None is a logical arsenal position, not a hidden copy of a real gun.
                            // Using a vanilla prefab here leaves behind weapon-owned presentation
                            // components (meters, lights, HUD callbacks, etc.) even if its renderers
                            // and primary weapon script are disabled. A deliberately bare placeholder
                            // keeps GunControl's stable three-position indexing without creating any
                            // gameplay or presentation state at all.
                            instance = EmptySlotRuntime.CreatePlaceholder(setter.transform);
                        }
                        else
                        {
                            GameObject prefab = choice.IsVanilla
                                ? GetVanillaPrefab(setter, family, choice.vanillaVariant, choice.vanillaForm)
                                : ElementCatalog.GetElementPrefab(setter, family, choice.id);

                            if (prefab == null)
                            {
                                Plugin.LogSource?.LogWarning("No prefab for " + family + " position " + position + " / " + choice.displayName + ".");
                                instance = EmptySlotRuntime.CreatePlaceholder(setter.transform);
                            }
                            else
                            {
                                instance = UnityEngine.Object.Instantiate(prefab, setter.transform);
                            }
                        }

                        instance.name = "EB " + choice.displayName + " " + family + " " + (position + 1);
                        ElementalSlotMarker marker = instance.AddComponent<ElementalSlotMarker>();
                        marker.element = choice.element;
                        marker.elementId = choice.IsElement ? ElementRegistry.Canonicalize(choice.id) : null;
                        marker.family = family;
                        marker.position = position;
                        marker.choiceId = choice.id;
                        marker.choiceName = choice.displayName;
                        marker.explosiveMode = choice.explosiveMode;
                        if (choice.IsVanilla)
                            marker.hasVanillaTemplate = VanillaWeaponTemplateMap.TryFromDirectChoice(choice.id, out marker.vanillaTemplate);
                        else if (choice.IsElement)
                            marker.hasVanillaTemplate = ElementCatalog.TryGetWeaponTemplate(choice.id, family, out marker.vanillaTemplate);

                        if (!choice.IsNone && !choice.IsMissing)
                        {
                            ElementWeaponRuntime.Attach(instance, marker);
                            GrenadeLauncherCompat.Configure(instance, marker);
                        }
                        slot.Add(instance);
                    }
                }
                gc.UpdateWeaponList(firstTime);
                RefreshWeaponVariantBinds();
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogError("Failed rebuilding EB arsenal: " + exception);
            }
            finally
            {
                rebuilding = false;
            }
        }

        internal static bool IsApplied(GunSetter setter)
        {
            if (setter == null || setter.gunc == null || setter.gunc.slots == null)
                return false;

            int familyCount = Mathf.Min(5, setter.gunc.slots.Count);
            if (familyCount < 5)
                return false;

            for (int family = 0; family < familyCount; family++)
            {
                List<GameObject> slot = setter.gunc.slots[family];
                if (slot == null || slot.Count != 3)
                    return false;

                for (int position = 0; position < slot.Count; position++)
                {
                    GameObject weapon = slot[position];
                    if (weapon == null || weapon.GetComponent<ElementalSlotMarker>() == null)
                        return false;
                }
            }
            return true;
        }

        private static void RefreshWeaponVariantBinds()
        {
            try
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type pluginType = assembly.GetType("WeaponVariantBinds.Plugin", false);
                    if (pluginType == null)
                        continue;
                    FieldInfo determined = pluginType.GetField("VanillaWeaponCyclesDetermined", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    determined?.SetValue(null, false);
                    Plugin.LogSource?.LogInfo("Marked WeaponVariantBinds weapon cycles dirty after EB loadout rebuild.");
                    return;
                }
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("WeaponVariantBinds refresh failed: " + exception.Message);
            }
        }

        private static GameObject GetVanillaPrefab(GunSetter setter, WeaponFamily family, int variant, int form)
        {
            AssetReference[] refs = null;
            switch (family)
            {
                case WeaponFamily.Revolver:
                    refs = variant == 0 ? setter.revolverPierce : variant == 1 ? setter.revolverTwirl : setter.revolverRicochet;
                    break;
                case WeaponFamily.Shotgun:
                    refs = variant == 0 ? setter.shotgunGrenade : variant == 1 ? setter.shotgunPump : setter.shotgunRed;
                    break;
                case WeaponFamily.Rapid:
                    refs = variant == 0 ? setter.nailMagnet : variant == 1 ? setter.nailOverheat : setter.nailRed;
                    break;
                case WeaponFamily.Ultimate:
                    refs = variant == 0 ? setter.railCannon : variant == 1 ? setter.railHarpoon : setter.railMalicious;
                    break;
                case WeaponFamily.Explosive:
                    refs = variant == 0 ? setter.rocketBlue : variant == 1 ? setter.rocketGreen : setter.rocketRed;
                    break;
            }
            return Pick(refs, form);
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

    internal sealed class LoadoutAutoApplyRuntime : MonoBehaviour
    {
        private Coroutine pendingApply;
        private int lastAppliedSetterId = int.MinValue;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            QueueApply();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (pendingApply != null)
            {
                StopCoroutine(pendingApply);
                pendingApply = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            QueueApply();
        }

        private void QueueApply()
        {
            if (pendingApply != null)
                StopCoroutine(pendingApply);
            pendingApply = StartCoroutine(ApplyWhenReady());
        }

        private IEnumerator ApplyWhenReady()
        {
            // GunSetter.Start performs the game's first native loadout build. Give that startup
            // pass a couple of frames, then explicitly re-run it so Addressable-backed prefab
            // references are populated before EB's ResetWeapons postfix rebuilds the 15 slots.
            yield return null;
            yield return null;

            const int maxAttempts = 40;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (!EBSettings.Enabled)
                {
                    pendingApply = null;
                    yield break;
                }

                GunSetter setter = UnityEngine.Object.FindObjectOfType<GunSetter>();
                if (setter != null)
                {
                    int setterId = setter.GetInstanceID();
                    if (setterId == lastAppliedSetterId && ArsenalLoadout.IsApplied(setter))
                    {
                        pendingApply = null;
                        yield break;
                    }

                    // Fresh installs should begin from the player's real terminal loadout,
                    // not the old hard-coded Fire/Water/Grass starter arrangement. Existing
                    // EB installs are detected and preserved by the initialization helper.
                    bool initialized = false;
                    try
                    {
                        initialized = EBSettings.EnsureWorkingLoadoutInitialized();
                    }
                    catch (Exception exception)
                    {
                        Plugin.LogSource?.LogWarning("EB loadout initialization is not ready yet: " + exception.Message);
                    }
                    if (!initialized)
                    {
                        // Do not let the fresh-install None sentinels become a real empty
                        // arsenal if ULTRAKILL's preference/terminal state is still starting.
                        // The outer retry loop will try again once PrefsManager/save state exists.
                        yield return new WaitForSecondsRealtime(0.1f);
                        continue;
                    }

                    try
                    {
                        setter.ResetWeapons(false);
                    }
                    catch (Exception exception)
                    {
                        Plugin.LogSource?.LogWarning("Automatic EB loadout apply attempt failed: " + exception.Message);
                    }

                    yield return null;
                    if (ArsenalLoadout.IsApplied(setter))
                    {
                        lastAppliedSetterId = setterId;
                        Plugin.LogSource?.LogInfo("Applied Elemental Battlegrounds loadout automatically for the current level.");
                        pendingApply = null;
                        yield break;
                    }
                }

                yield return new WaitForSecondsRealtime(0.1f);
            }

            Plugin.LogSource?.LogWarning("Elemental Battlegrounds loadout was not fully available during level startup; the manual Apply loadout button remains available.");
            pendingApply = null;
        }
    }

    [HarmonyPatch(typeof(GunSetter), nameof(GunSetter.ResetWeapons))]
    internal static class GunSetterResetWeaponsPatch
    {
        private static void Postfix(GunSetter __instance, bool firstTime)
        {
            ArsenalLoadout.Rebuild(__instance, firstTime);
        }
    }
}
