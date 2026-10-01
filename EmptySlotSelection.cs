using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ElementalBattlegroundsMod
{
    /// <summary>
    /// Empty EB positions remain present in GunControl.slots so the 15-position loadout has stable
    /// indexes, but they are not weapons and must never become GunControl.currentWeapon.
    ///
    /// ULTRAKILL's current SwitchWeapon implementation assumes every object in a non-empty list is
    /// selectable. EB therefore resolves a requested logical position to the next real position
    /// before vanilla mutates currentSlot/currentVariation. ForceWeapon is guarded as a final safety
    /// net for external callers such as WeaponVariantBinds.
    /// </summary>
    [HarmonyPatch]
    internal static class EmptySlotSwitchWeaponPatch
    {
        private static readonly FieldInfo RetainedVariationsField = AccessTools.Field(typeof(GunControl), "retainedVariations");

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(GunControl),
                "SwitchWeapon",
                new[] { typeof(int), typeof(int?), typeof(bool), typeof(bool), typeof(bool) });
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(
            GunControl __instance,
            ref int __0,
            ref int? __1,
            bool __2,
            ref bool __3,
            ref bool __4)
        {
            if (!EBSettings.Enabled || __instance == null || __instance.slots == null || __instance.slots.Count == 0)
                return true;

            int slotCount = __instance.slots.Count;
            int effectiveSlot = __3
                ? Loop(__0 - 1, slotCount) + 1
                : Mathf.Clamp(__0, 1, slotCount);

            // Slot 6 is ULTRAKILL's miscellaneous slot and is not rebuilt by EB. Only interfere
            // with a slot if it actually contains at least one EB marker/placeholder.
            List<GameObject> slot = __instance.slots[effectiveSlot - 1];
            if (!EmptySlotRuntime.IsEbManagedSlot(slot))
                return true;

            int firstSelectable = EmptySlotRuntime.FindSelectablePosition(slot, 0, 1);
            if (firstSelectable < 0)
            {
                // An all-None family is not a weapon family. Keep the currently held weapon rather
                // than letting vanilla activate an empty placeholder.
                return false;
            }

            int requested;
            int direction = 1;

            if (__1.HasValue)
            {
                int rawRequested = __1.Value;
                requested = Loop(rawRequested, slot.Count);

                // cycleVariation is used by Next/Previous Variation. The raw value is deliberately
                // current +/- 1, so it preserves direction even at the wrap boundary.
                if (__4 && effectiveSlot == __instance.currentSlotIndex && rawRequested < __instance.currentVariationIndex)
                    direction = -1;
            }
            else if (effectiveSlot == __instance.currentSlotIndex)
            {
                int redrawBehaviour = MonoSingleton<PrefsManager>.Instance != null
                    ? MonoSingleton<PrefsManager>.Instance.GetInt("WeaponRedrawBehaviour")
                    : 0;

                switch (redrawBehaviour)
                {
                    case 1:
                        requested = 0;
                        break;
                    case 2:
                        requested = __instance.currentVariationIndex;
                        break;
                    default:
                        requested = __instance.currentVariationIndex + 1;
                        break;
                }
                requested = Loop(requested, slot.Count);
            }
            else
            {
                requested = 0;

                // Preserve ULTRAKILL's retained-variation / variation-memory semantics. If the
                // remembered logical position is now None, start there and walk forward to the
                // next real EB weapon instead of discarding the preference outright.
                if ((__2 || __instance.variationMemory) &&
                    RetainedVariationsField?.GetValue(__instance) is Dictionary<int, int> retained &&
                    retained.TryGetValue(effectiveSlot - 1, out int remembered) &&
                    remembered >= 0 && remembered < slot.Count)
                {
                    requested = remembered;
                }
            }

            int resolved = EmptySlotRuntime.FindSelectablePosition(slot, requested, direction);
            if (resolved < 0)
                return false;

            // Feed vanilla an explicit, real position. Clearing the cycle flags prevents vanilla
            // from wrapping the already-resolved indexes a second time.
            __0 = effectiveSlot;
            __1 = resolved;
            __3 = false;
            __4 = false;
            return true;
        }

        private static int Loop(int value, int modulus)
        {
            if (modulus <= 0)
                return 0;
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }

    [HarmonyPatch(typeof(GunControl), "ForceWeapon")]
    internal static class EmptySlotForceWeaponPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(GameObject __0)
        {
            // ForceWeapon is the path used directly by WeaponVariantBinds 1.2.x. WVB's normal
            // conversions are remapped in WeaponVariantBindsCompat, but this guard guarantees that
            // no other external caller can ever make an EB placeholder the active weapon.
            return !EBSettings.Enabled || !EmptySlotRuntime.IsPlaceholder(__0);
        }
    }
}

namespace ElementalBattlegroundsMod
{
    [HarmonyPatch(typeof(GunControl), "YesWeapon")]
    internal static class EmptySlotYesWeaponPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(GunControl __instance)
        {
            if (!EBSettings.Enabled || __instance == null || __instance.slots == null || __instance.slots.Count == 0)
                return true;

            int currentSlot = Mathf.Clamp(__instance.currentSlotIndex, 1, __instance.slots.Count) - 1;
            List<GameObject> slot = __instance.slots[currentSlot];
            if (!EmptySlotRuntime.IsEbManagedSlot(slot))
                return true;

            int currentVariation = __instance.currentVariationIndex;
            if (currentVariation >= 0 && currentVariation < slot.Count && !EmptySlotRuntime.IsPlaceholder(slot[currentVariation]))
                return true;

            int resolved = EmptySlotRuntime.FindSelectablePosition(slot, currentVariation, 1);
            if (resolved >= 0)
            {
                __instance.currentVariationIndex = resolved;
                __instance.currentWeapon = slot[resolved];
                return true;
            }

            // If the whole remembered family became None, resume with the next real weapon in any
            // family rather than re-enabling a placeholder after cutscenes or other NoWeapon states.
            for (int offset = 1; offset <= __instance.slots.Count; offset++)
            {
                int family = (currentSlot + offset) % __instance.slots.Count;
                List<GameObject> candidateSlot = __instance.slots[family];
                int candidate = EmptySlotRuntime.FindSelectablePosition(candidateSlot, 0, 1);
                if (candidate < 0)
                    continue;

                __instance.currentSlotIndex = family + 1;
                __instance.currentVariationIndex = candidate;
                __instance.currentWeapon = candidateSlot[candidate];
                return true;
            }

            // A deliberately all-empty arsenal is genuinely unarmed.
            __instance.currentWeapon = null;
            __instance.noWeapons = true;
            __instance.activated = true;
            WeaponHUD hud = MonoSingleton<WeaponHUD>.Instance;
            if (hud != null)
                hud.UpdateImage(null, null, 0);
            return false;
        }
    }
}
