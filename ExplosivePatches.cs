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
    [HarmonyPatch(typeof(RocketLauncher), "Update")]
    internal static class CustomRocketCooldownUiPatch
    {
        private static readonly AccessTools.FieldRef<RocketLauncher, Image> TimerMeter = AccessTools.FieldRefAccess<RocketLauncher, Image>("timerMeter");
        private static readonly AccessTools.FieldRef<RocketLauncher, RectTransform> TimerArm = AccessTools.FieldRefAccess<RocketLauncher, RectTransform>("timerArm");
        private static readonly AccessTools.FieldRef<RocketLauncher, Image[]> VariationColorables = AccessTools.FieldRefAccess<RocketLauncher, Image[]>("variationColorables");

        private static void Postfix(RocketLauncher __instance)
        {
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            if (marker == null || (marker.element != ElementId.Water && marker.element != ElementId.Grass && marker.element != ElementId.Wind && marker.element != ElementId.Storm)) return;
            ActiveWeaponController controller = __instance.GetComponent<ActiveWeaponController>();
            if (controller == null) return;
            float ready = controller.CooldownReadyFraction;
            Image meter = TimerMeter(__instance);
            if (meter != null) meter.fillAmount = ready;
            RectTransform arm = TimerArm(__instance);
            if (arm != null) arm.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(360f, 0f, ready));
            Color accent = ElementPalette.Accent(marker.element);
            Image[] images = VariationColorables(__instance);
            if (images != null) foreach (Image image in images) if (image != null) image.color = new Color(accent.r, accent.g, accent.b, image.color.a);
        }
    }

    [HarmonyPatch(typeof(RocketLauncher), nameof(RocketLauncher.FreezeRockets))]
    internal static class CustomRocketFreezeSuppressPatch
    {
        private static bool Prefix(RocketLauncher __instance)
        {
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            return marker == null || (marker.element != ElementId.Water && marker.element != ElementId.Grass);
        }
    }

    [HarmonyPatch(typeof(RocketLauncher), nameof(RocketLauncher.UnfreezeRockets))]
    internal static class CustomRocketUnfreezeSuppressPatch
    {
        private static bool Prefix(RocketLauncher __instance)
        {
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            return marker == null || (marker.element != ElementId.Water && marker.element != ElementId.Grass);
        }
    }

    internal sealed class GrassBurstRocketTag : MonoBehaviour
    {
        public bool multiplierApplied;
    }

    [HarmonyPatch(typeof(Grenade), "Awake")]
    internal static class GrassBurstGrenadeSpawnPatch
    {
        private static void Postfix(Grenade __instance)
        {
            if (GrassBurstContext.Active && __instance != null && __instance.rocket)
                __instance.gameObject.AddComponent<GrassBurstRocketTag>();
        }
    }

    [HarmonyPatch(typeof(Grenade), nameof(Grenade.Explode))]
    internal static class GrenadeElementPatch
    {
        private static void Prefix(Grenade __instance)
        {
            GrassBurstRocketTag tag = __instance.GetComponent<GrassBurstRocketTag>();
            if (tag != null && !tag.multiplierApplied)
            {
                tag.multiplierApplied = true;
                __instance.totalDamageMultiplier *= WeaponTuning.GrassRocketDamageMultiplier;
            }

        }
    }

    internal sealed class CounterBuffGrenadeFireTag : MonoBehaviour { }

    [HarmonyPatch(typeof(Grenade), "Update")]
    internal static class CounterBuffGrenadeCloudIgnitionPatch
    {
        private static void Postfix(Grenade __instance)
        {
            if (__instance == null || __instance.sourceWeapon == null) return;
            CounterBuffGrenadeFireTag tag = __instance.GetComponent<CounterBuffGrenadeFireTag>();
            if (tag == null && RuntimeRegistry.CounterBuffActive && MarkerUtility.Find(__instance.sourceWeapon) != null)
                tag = __instance.gameObject.AddComponent<CounterBuffGrenadeFireTag>();
            if (tag != null)
                RuntimeRegistry.IgniteSporeClouds(__instance.transform.position, 1.5f, __instance.sourceWeapon);
        }
    }

    [HarmonyPatch(typeof(Explosion), "Start")]
    internal static class ExplosionSporeIgnitionPatch
    {
        private static void Postfix(Explosion __instance)
        {
            if (__instance == null || RuntimeRegistry.SporeClouds.Count == 0)
                return;
            // Explosion maxSize is the eventual blast radius, so checking it immediately at
            // spawn also covers rockets that miss the cloud center but whose blast reaches it.
            float radius = Mathf.Max(1.5f, __instance.maxSize);
            RuntimeRegistry.IgniteSporeClouds(__instance.transform.position, radius, __instance.sourceWeapon);
        }
    }


}
