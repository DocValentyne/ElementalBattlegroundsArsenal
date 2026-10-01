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
    [HarmonyPatch]
    internal static class InfernoWeaponSwapLockPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(GunControl)))
            {
                if (method.Name == "SwitchWeapon" || method.Name == "ForceWeapon")
                    yield return method;
            }
        }

        private static bool Prefix()
        {
            return !RuntimeRegistry.InfernoActive;
        }
    }

    [HarmonyPatch(typeof(NewMovement), "TryStartSlam")]
    internal static class InfernoSlamSuppressPatch
    {
        private static bool Prefix() => !RuntimeRegistry.InfernoActive;
    }

    [HarmonyPatch(typeof(HookArm), "Update")]
    internal static class InfernoWhiplashSuppressPatch
    {
        private static bool Prefix(HookArm __instance)
        {
            if (!RuntimeRegistry.InfernoActive)
                return true;

            // Inferno is intentionally a near-total commitment state. Cancel any hook that was
            // already in flight when Inferno begins, then skip Whiplash input/update processing
            // until the ultimate ends. FistControl itself remains untouched, so Feedbacker and
            // Knuckleblaster continue to work.
            __instance?.Cancel();
            return false;
        }
    }

    [HarmonyPatch(typeof(WeaponIcon), nameof(WeaponIcon.UpdateIcon))]
    internal static class ElementWeaponHudColorPatch
    {
        private static readonly AccessTools.FieldRef<WeaponIcon, Renderer[]> Renderers = AccessTools.FieldRefAccess<WeaponIcon, Renderer[]>("variationColoredRenderers");
        private static readonly AccessTools.FieldRef<WeaponIcon, Image[]> Images = AccessTools.FieldRefAccess<WeaponIcon, Image[]>("variationColoredImages");

        private static void Postfix(WeaponIcon __instance)
        {
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            if (marker == null || !marker.IsCustom) return;
            Color accent = ElementPalette.Accent(marker.elementId, marker.element);
            Image[] images = Images(__instance);
            if (images != null)
                foreach (Image image in images)
                    if (image != null) image.color = new Color(accent.r, accent.g, accent.b, image.color.a);
            Renderer[] renderers = Renderers(__instance);
            if (renderers != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null) continue;
                    renderer.GetPropertyBlock(block);
                    if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_EmissiveColor")) block.SetColor("_EmissiveColor", accent);
                    else block.SetColor("_Color", accent);
                    renderer.SetPropertyBlock(block);
                }
            }
            if (MonoSingleton<WeaponHUD>.Instance != null)
            {
                Image hud = MonoSingleton<WeaponHUD>.Instance.GetComponent<Image>();
                if (hud != null) hud.color = accent;
                if (MonoSingleton<WeaponHUD>.Instance.transform.childCount > 0)
                {
                    Image glow = MonoSingleton<WeaponHUD>.Instance.transform.GetChild(0).GetComponent<Image>();
                    if (glow != null) glow.color = accent;
                }
            }
        }
    }

    [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.AddFlammable))]
    internal static class WeakBurnGasolineUpgradePatch
    {
        private static void Prefix(EnemyIdentifier __instance, float amount)
        {
            if (amount > 0f && !WeakBurnStatus.ApplyingOwnFire)
                WeakBurnStatus.NotifyExternalFire(__instance);
        }
    }

    [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.StartBurning))]
    internal static class WeakBurnFireUpgradePatch
    {
        private static void Prefix(EnemyIdentifier __instance, float heat)
        {
            if (heat > 0f && !WeakBurnStatus.ApplyingOwnFire)
                WeakBurnStatus.NotifyExternalFire(__instance);
        }
    }

    [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.DeliverDamage))]
    internal static class WeakBurnNativeDamagePatch
    {
        private static void Prefix(EnemyIdentifier __instance, ref float multiplier)
        {
            if (__instance == null || __instance.hitter != "fire")
                return;
            WeakBurnStatus status = __instance.GetComponent<WeakBurnStatus>();
            if (status != null && status.CombustionActive)
                multiplier = status.NativeFireDamage;
        }
    }

    internal struct EBDamageState
    {
        internal float healthBefore;
        internal string hitter;
        internal GameObject sourceWeapon;
        internal int attackId;
        internal bool eligible;
    }

    [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.DeliverDamage))]
    internal static class CounterBuffIgnitionPatch
    {
        private static void Prefix(EnemyIdentifier __instance, GameObject sourceWeapon, out EBDamageState __state)
        {
            __state = new EBDamageState
            {
                healthBefore = __instance.health,
                hitter = __instance.hitter,
                sourceWeapon = sourceWeapon,
                attackId = RuntimeRegistry.CurrentAttackId,
                eligible = false
            };
            if (!RuntimeRegistry.CounterBuffActive || sourceWeapon == null)
                return;
            ElementalSlotMarker marker = MarkerUtility.Find(sourceWeapon);
            if (marker == null)
                return;
            if (__state.hitter == "fire" || __state.hitter == "ebpoison" || __state.hitter == "ebweakfire" || __state.hitter == "ebcounter")
                return;
            __state.eligible = true;
        }

        private static void Postfix(EnemyIdentifier __instance, EBDamageState __state)
        {
            if (!__state.eligible || __instance == null)
                return;
            float dealt = Mathf.Max(0f, __state.healthBefore - __instance.health);
            if (dealt <= 0f)
                return;
            WeakBurnStatus.Apply(__instance, __state.sourceWeapon, dealt, __state.hitter, __state.attackId);
        }
    }
}
