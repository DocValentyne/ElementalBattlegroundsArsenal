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
    internal static class WeaponVariantBindsCompat
    {
        private static bool installed;
        // WeaponVariantBinds' WeaponEnum values are 1..15. Cache the terminal/native
        // position that each named weapon occupied immediately before EB rebuilds the slots.
        private static readonly int[] nativePositionByEnum = CreateEmptyPositionMap();
        private static readonly int[,] nativeEnumByPosition = new int[5, 3];
        private static bool nativeOrderCaptured;
        private static bool warnedMissingNativeOrder;

        private static int[] CreateEmptyPositionMap()
        {
            int[] result = new int[16];
            for (int i = 0; i < result.Length; i++)
                result[i] = -1;
            return result;
        }

        internal static void TryInstall(Harmony harmony)
        {
            if (installed || harmony == null) return;
            try
            {
                Type configType = AccessTools.TypeByName("WeaponVariantBinds.PluginConfig");
                if (configType == null) return;

                MethodInfo enumToSlot = FindStatic(configType, "convertWeaponEnumToSlotVariation", 1);
                MethodInfo determineVanilla = FindStatic(configType, "DetermineVanillaWeaponCycles", 0);

                if (enumToSlot != null)
                    harmony.Patch(enumToSlot, prefix: new HarmonyMethod(typeof(WeaponVariantBindsCompat), nameof(ConvertPrefix)));
                if (determineVanilla != null)
                    harmony.Patch(determineVanilla, postfix: new HarmonyMethod(typeof(WeaponVariantBindsCompat), nameof(DetermineVanillaPostfix)));

                Type smoothType = AccessTools.TypeByName("WeaponVariantBinds.ActionSmoothening");
                MethodInfo attemptSecondary = smoothType == null ? null : FindStatic(smoothType, "AttemptSecondaryShoot", 1);
                if (attemptSecondary != null)
                    harmony.Patch(attemptSecondary, prefix: new HarmonyMethod(typeof(WeaponVariantBindsCompat), nameof(AttemptSecondaryPrefix)));

                installed = enumToSlot != null || determineVanilla != null || attemptSecondary != null;
                if (installed)
                    Plugin.LogSource?.LogInfo("Installed WeaponVariantBinds compatibility: named variants follow ULTRAKILL's captured terminal order and EB empty positions are skipped.");
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("WeaponVariantBinds compatibility patch failed: " + exception.Message);
            }
        }

        private static MethodInfo FindStatic(Type type, string name, int parameterCount)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                if (method.Name == name && method.GetParameters().Length == parameterCount)
                    return method;
            return null;
        }

        internal static void CaptureNativeOrder(GunControl gc)
        {
            if (gc == null || gc.slots == null)
                return;

            for (int i = 0; i < nativePositionByEnum.Length; i++)
                nativePositionByEnum[i] = -1;
            Array.Clear(nativeEnumByPosition, 0, nativeEnumByPosition.Length);

            int familyCount = Mathf.Min(5, gc.slots.Count);
            for (int family = 0; family < familyCount; family++)
            {
                List<GameObject> slot = gc.slots[family];
                if (slot == null)
                    continue;

                for (int position = 0; position < Mathf.Min(3, slot.Count); position++)
                {
                    int enumValue = IdentifyWeaponVariant(slot[position], family);
                    if (enumValue <= 0 || enumValue >= nativePositionByEnum.Length)
                        continue;
                    nativeEnumByPosition[family, position] = enumValue;
                    nativePositionByEnum[enumValue] = position;
                }
            }

            nativeOrderCaptured = true;
            warnedMissingNativeOrder = false;
        }

        private static int IdentifyWeaponVariant(GameObject weapon, int family)
        {
            if (weapon == null)
                return 0;

            switch (family)
            {
                case 0:
                {
                    Revolver revolver = weapon.GetComponent<Revolver>();
                    return revolver != null && revolver.gunVariation >= 0 && revolver.gunVariation <= 2
                        ? 1 + revolver.gunVariation : 0;
                }
                case 1:
                {
                    Shotgun shotgun = weapon.GetComponent<Shotgun>();
                    int variation = shotgun != null ? shotgun.variation : -1;
                    if (variation < 0)
                    {
                        ShotgunHammer hammer = weapon.GetComponent<ShotgunHammer>();
                        variation = hammer != null ? hammer.variation : -1;
                    }
                    return variation >= 0 && variation <= 2 ? 4 + variation : 0;
                }
                case 2:
                {
                    Nailgun nailgun = weapon.GetComponent<Nailgun>();
                    if (nailgun == null) return 0;
                    // WeaponVariantBinds names these by gameplay variant, while ULTRAKILL's
                    // Nailgun variation field has Attractor/Overheat reversed internally.
                    if (nailgun.variation == 1) return 7; // Attractor
                    if (nailgun.variation == 0) return 8; // Overheat
                    if (nailgun.variation == 2) return 9; // Jumpstart
                    return 0;
                }
                case 3:
                {
                    Railcannon rail = weapon.GetComponent<Railcannon>();
                    return rail != null && rail.variation >= 0 && rail.variation <= 2
                        ? 10 + rail.variation : 0;
                }
                case 4:
                {
                    RocketLauncher rocket = weapon.GetComponent<RocketLauncher>();
                    return rocket != null && rocket.variation >= 0 && rocket.variation <= 2
                        ? 13 + rocket.variation : 0;
                }
                default:
                    return 0;
            }
        }

        private static bool ConvertPrefix(object __0, ref int[] __result)
        {
            try
            {
                if (!EBSettings.Enabled || __0 == null)
                    return true;
                int enumValue = Convert.ToInt32(__0);
                if (enumValue == 0)
                {
                    // WVB can still ask to convert None after walking an all-empty cycle. Its own
                    // converter assumes a real weapon component exists at every physical position,
                    // which is not true for EB placeholders. Use WVB's empty sentinel instead.
                    __result = new[] { 500, 500 };
                    return false;
                }
                if (enumValue < 1 || enumValue > 15)
                    return true;

                int family = (enumValue - 1) / 3;
                int position = nativeOrderCaptured ? nativePositionByEnum[enumValue] : -1;
                if (position < 0 || position > 2)
                {
                    // Startup should always capture the native list in ResetWeapons' postfix.
                    // Keep a safe fallback rather than breaking a bind if another mod invokes
                    // WeaponVariantBinds unusually early.
                    position = (enumValue - 1) % 3;
                    if (!warnedMissingNativeOrder)
                    {
                        warnedMissingNativeOrder = true;
                        Plugin.LogSource?.LogWarning("WeaponVariantBinds requested a variant before EB captured ULTRAKILL's native terminal order; using canonical position fallback for this session.");
                    }
                }

                GunControl gc = MonoSingleton<GunControl>.Instance;
                if (gc == null || family < 0 || family >= gc.slots.Count || position >= gc.slots[family].Count)
                    return true;

                // WVB custom cycles can still contain a named vanilla enum whose captured terminal
                // position is currently None in EB. Resolve that logical position forward to the
                // next real EB weapon. If the whole family is empty, return WVB's own sentinel so
                // its switch logic treats the request as an empty weapon instead of ForceWeapon'ing
                // a placeholder.
                int resolved = EmptySlotRuntime.FindSelectablePosition(gc.slots[family], position, 1);
                if (resolved < 0)
                {
                    __result = new[] { 500, 500 };
                    return false;
                }

                __result = new[] { family, resolved };
                return false;
            }
            catch
            {
                return true;
            }
        }

        private static void DetermineVanillaPostfix()
        {
            if (!EBSettings.Enabled)
                return;

            try
            {
                Type configType = AccessTools.TypeByName("WeaponVariantBinds.PluginConfig");
                FieldInfo cyclesField = configType?.GetField("vanillaWeaponCycles", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                Array cycles = cyclesField?.GetValue(null) as Array;
                if (cycles == null)
                    return;

                for (int family = 0; family < Mathf.Min(5, cycles.Length); family++)
                {
                    object cycle = cycles.GetValue(family);
                    if (cycle == null)
                        continue;
                    FieldInfo enumsField = cycle.GetType().GetField("weaponEnums", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    Array enums = enumsField?.GetValue(cycle) as Array;
                    Type enumType = enums?.GetType().GetElementType();
                    if (enums == null || enumType == null)
                        continue;

                    GunControl gc = MonoSingleton<GunControl>.Instance;
                    for (int position = 0; position < Mathf.Min(3, enums.Length); position++)
                    {
                        // WVB already skips WeaponEnum.None in its vanilla-cycle, direct-variant,
                        // scroll and next/previous-variation paths. Advertise EB None positions as
                        // exactly that instead of assigning the captured vanilla name to an inert
                        // placeholder.
                        bool ebEmpty = gc != null &&
                                       family < gc.slots.Count &&
                                       position < gc.slots[family].Count &&
                                       EmptySlotRuntime.IsPlaceholder(gc.slots[family][position]);
                        if (ebEmpty)
                        {
                            enums.SetValue(Enum.ToObject(enumType, 0), position);
                            continue;
                        }

                        int enumValue = nativeOrderCaptured ? nativeEnumByPosition[family, position] : 0;
                        if (enumValue <= 0)
                            enumValue = family * 3 + position + 1;
                        enums.SetValue(Enum.ToObject(enumType, enumValue), position);
                    }
                }
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("WeaponVariantBinds vanilla-cycle remap failed: " + exception.Message);
            }
        }

        private static bool AttemptSecondaryPrefix(ref bool __result)
        {
            if (!EBSettings.Enabled)
                return true;

            GunControl gc = MonoSingleton<GunControl>.Instance;
            GameObject weapon = gc == null ? null : gc.currentWeapon;
            ElementalSlotMarker marker = weapon == null ? null : weapon.GetComponent<ElementalSlotMarker>();
            if (marker == null || !marker.IsCustom)
                return true;

            // Only suppress WVB's direct vanilla-alt invocation when this EB weapon actually has a
            // custom secondary. Unchanged EB weapons (Water Piercer, Fire Overheat, Water Attractor,
            // Fire Firestarter, etc.) must retain WVB's normal action-smoothing behavior.
            bool customSecondary = weapon.GetComponent<ElementalSecondaryOwner>() != null;
            if (!customSecondary)
                return true;

            // These controllers already implement held-Fire2 buffering themselves. Letting WVB call
            // the backing prefab's vanilla alt is what makes a position alias behave like whichever
            // vanilla color happened to supply the model.
            __result = true;
            return false;
        }
    }

}
