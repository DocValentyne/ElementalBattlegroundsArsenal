using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using ULTRAKILL.Cheats;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBattlegroundsMod
{
    [HarmonyPatch(typeof(Shotgun), "Update")]
    internal static class ShotgunCustomAltSuppressPatch
    {
        private static readonly AccessTools.FieldRef<Shotgun, bool> ResettingCores = AccessTools.FieldRefAccess<Shotgun, bool>("resettingCores");

        private static readonly AccessTools.FieldRef<Shotgun, Slider> ChargeSlider = AccessTools.FieldRefAccess<Shotgun, Slider>("chargeSlider");

        private static void Prefix(Shotgun __instance)
        {
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            if (marker == null) return;
            if (marker.element == ElementId.Fire || marker.element == ElementId.Grass || marker.element == ElementId.Wind)
                ResettingCores(__instance) = true;
        }

        private static void Postfix(Shotgun __instance)
        {
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            if (marker == null || (marker.element != ElementId.Fire && marker.element != ElementId.Grass && marker.element != ElementId.Wind && marker.element != ElementId.Earth)) return;
            ActiveWeaponController controller = __instance.GetComponent<ActiveWeaponController>();
            Slider slider = ChargeSlider(__instance);
            if (controller != null && slider != null)
                slider.value = slider.maxValue * controller.CooldownReadyFraction;
            if (__instance.sliderFill != null)
                __instance.sliderFill.color = ElementPalette.Accent(marker.element);
        }
    }

    [HarmonyPatch(typeof(Shotgun), "Shoot")]
    internal static class ShotgunShootPatch
    {
        private static void Postfix(Shotgun __instance)
        {
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            CameraController cc = MonoSingleton<CameraController>.Instance;
            if (marker != null && marker.element == ElementId.Grass && cc != null)
                CycloneRuntime.PushFromShot(cc.GetDefaultPos(), cc.transform.forward);
            if (marker != null && RuntimeRegistry.CounterBuffActive && cc != null)
                RuntimeRegistry.IgniteSporeCloudsAlongRay(cc.GetDefaultPos(), cc.transform.forward, 20f, 5f, __instance.gameObject);
        }
    }

    [HarmonyPatch(typeof(ShotgunHammer), "Update")]
    internal static class WaterJackhammerSecondarySuppressPatch
    {
        private static readonly AccessTools.FieldRef<ShotgunHammer, bool> AboutToSecondary = AccessTools.FieldRefAccess<ShotgunHammer, bool>("aboutToSecondary");
        private static readonly AccessTools.FieldRef<ShotgunHammer, Image> SecondaryMeter = AccessTools.FieldRefAccess<ShotgunHammer, Image>("secondaryMeter");

        private static void Prefix(ShotgunHammer __instance)
        {
            ElementalJackhammerPrimaryCooldownState primaryState = __instance != null
                ? __instance.GetComponent<ElementalJackhammerPrimaryCooldownState>()
                : null;
            primaryState?.BeginNative(__instance);

            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            if (marker != null && (marker.element == ElementId.Water || marker.element == ElementId.Storm || marker.element == ElementId.Earth))
                AboutToSecondary(__instance) = true;
        }

        private static void Postfix(ShotgunHammer __instance)
        {
            ElementalJackhammerPrimaryCooldownState primaryState = __instance != null
                ? __instance.GetComponent<ElementalJackhammerPrimaryCooldownState>()
                : null;
            primaryState?.EndNative(__instance);

            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            if (marker == null || (marker.element != ElementId.Water && marker.element != ElementId.Storm && marker.element != ElementId.Earth)) return;
            ActiveWeaponController controller = __instance.GetComponent<ActiveWeaponController>();
            Image meter = SecondaryMeter(__instance);
            if (meter != null && controller != null)
            {
                float ready = controller.CooldownReadyFraction;
                // ShotgunHammer's meter graphic only uses 27.5%-62.5% for an in-progress
                // recharge, then snaps to 100% when actually ready. Raw 0..1 fill made EB's
                // dial visually finish well before the controller cooldown did.
                meter.fillAmount = ready >= 0.999f
                    ? 1f
                    : (ready <= 0f ? 0f : Mathf.Lerp(0.275f, 0.625f, ready));
                meter.color = ElementPalette.Accent(marker.element);
            }
        }
    }

    [HarmonyPatch(typeof(ShotgunHammer), "LateUpdate")]
    internal static class ElementalJackhammerPrimaryCooldownLateUpdatePatch
    {
        private static void Prefix(ShotgunHammer __instance)
        {
            __instance?.GetComponent<ElementalJackhammerPrimaryCooldownState>()?.BeginNative(__instance);
        }

        private static void Postfix(ShotgunHammer __instance)
        {
            __instance?.GetComponent<ElementalJackhammerPrimaryCooldownState>()?.EndNative(__instance);
        }
    }

    [HarmonyPatch(typeof(ShotgunHammer), "DeliverDamage")]
    internal static class ElementalJackhammerPrimaryCooldownDamagePatch
    {
        private static void Prefix(ShotgunHammer __instance)
        {
            __instance?.GetComponent<ElementalJackhammerPrimaryCooldownState>()?.BeginNative(__instance);
        }

        private static void Postfix(ShotgunHammer __instance)
        {
            __instance?.GetComponent<ElementalJackhammerPrimaryCooldownState>()?.EndNative(__instance);
        }
    }

}
