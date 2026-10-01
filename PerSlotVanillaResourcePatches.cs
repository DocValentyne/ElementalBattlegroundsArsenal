using System;
using System.Collections.Generic;
using HarmonyLib;
using ULTRAKILL.Cheats;
using UnityEngine;

namespace ElementalBattlegroundsMod
{
    // Revolver resources ------------------------------------------------------

    [HarmonyPatch(typeof(Revolver), "OnEnable")]
    internal static class PerSlotRevolverOnEnablePatch
    {
        private static void Prefix(Revolver __instance) => SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotRevolverSecondaryState>())?.BeginNative();
        private static void Postfix(Revolver __instance) => SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotRevolverSecondaryState>())?.EndNative();
    }

    [HarmonyPatch(typeof(Revolver), "OnDisable")]
    internal static class PerSlotRevolverOnDisablePatch
    {
        private static void Prefix(Revolver __instance) => SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotRevolverSecondaryState>())?.BeginNative();
        private static void Postfix(Revolver __instance) => SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotRevolverSecondaryState>())?.EndNative();
    }

    [HarmonyPatch(typeof(Revolver), "Update")]
    internal static class PerSlotRevolverUpdatePatch
    {
        private static void Prefix(Revolver __instance)
        {
            PerSlotRevolverSecondaryState state = SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotRevolverSecondaryState>());
            if (state != null && state.UsesGlobalDuringUpdate)
                state.BeginNative();
        }
        private static void Postfix(Revolver __instance)
        {
            PerSlotRevolverSecondaryState state = SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotRevolverSecondaryState>());
            if (state != null && state.UsesGlobalDuringUpdate)
                state.EndNative();
        }
    }

    [HarmonyPatch(typeof(Revolver), "Shoot")]
    internal static class PerSlotRevolverShootPatch
    {
        private static void Prefix(Revolver __instance)
        {
            PerSlotRevolverSecondaryState state = SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotRevolverSecondaryState>());
            if (state != null && state.UsesGlobalDuringUpdate)
                state.BeginNative();
        }
        private static void Postfix(Revolver __instance)
        {
            PerSlotRevolverSecondaryState state = SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotRevolverSecondaryState>());
            if (state != null && state.UsesGlobalDuringUpdate)
                state.EndNative();
        }
    }

    [HarmonyPatch(typeof(Revolver), "ThrowCoin")]
    internal static class PerSlotRevolverThrowCoinPatch
    {
        private static void Prefix(Revolver __instance) => SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotRevolverSecondaryState>())?.BeginNative();
        private static void Postfix(Revolver __instance) => SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotRevolverSecondaryState>())?.EndNative();
    }

    [HarmonyPatch(typeof(Revolver), "MaxCharge")]
    internal static class PerSlotRevolverMaxChargePatch
    {
        private static void Prefix(Revolver __instance) => SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotRevolverSecondaryState>())?.BeginNative();
        private static void Postfix(Revolver __instance) => SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotRevolverSecondaryState>())?.EndNative();
    }

    // Sawed-On form resources -------------------------------------------------

    [HarmonyPatch(typeof(Shotgun), "OnEnable")]
    internal static class PerSlotSawShotgunEnablePatch
    {
        private static void Prefix(Shotgun __instance)
        {
            PerSlotSawedOnState state = SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotSawedOnState>());
            state?.BeginChargeNative();
            state?.BeginWearNative();
        }
        private static void Postfix(Shotgun __instance)
        {
            PerSlotSawedOnState state = SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotSawedOnState>());
            state?.EndWearNative();
            state?.EndChargeNative();
        }
    }

    [HarmonyPatch(typeof(Shotgun), "Update")]
    internal static class PerSlotSawShotgunUpdatePatch
    {
        private static void Prefix(Shotgun __instance) => SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotSawedOnState>())?.BeginChargeNative();
        private static void Postfix(Shotgun __instance) => SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotSawedOnState>())?.EndChargeNative();
    }

    [HarmonyPatch(typeof(ShotgunHammer), "OnEnable")]
    internal static class PerSlotSawHammerEnablePatch
    {
        private static void Prefix(ShotgunHammer __instance)
        {
            PerSlotSawedOnState state = SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotSawedOnState>());
            state?.BeginChargeNative();
            state?.BeginWearNative();
        }
        private static void Postfix(ShotgunHammer __instance)
        {
            PerSlotSawedOnState state = SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotSawedOnState>());
            state?.EndWearNative();
            state?.EndChargeNative();
        }
    }

    [HarmonyPatch(typeof(ShotgunHammer), "Update")]
    internal static class PerSlotSawHammerUpdatePatch
    {
        private static void Prefix(ShotgunHammer __instance) => SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotSawedOnState>())?.BeginChargeNative();
        private static void Postfix(ShotgunHammer __instance) => SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotSawedOnState>())?.EndChargeNative();
    }

    [HarmonyPatch(typeof(HurtZone), "OnEnable")]
    internal static class PerSlotSawHurtZoneEnablePatch
    {
        private static void Prefix(HurtZone __instance) => SlotResourceOwnerResolver.Resolve(__instance?.sourceWeapon?.GetComponent<PerSlotSawedOnState>())?.BeginWearNative();
        private static void Postfix(HurtZone __instance) => SlotResourceOwnerResolver.Resolve(__instance?.sourceWeapon?.GetComponent<PerSlotSawedOnState>())?.EndWearNative();
    }

    [HarmonyPatch(typeof(HurtZone), "OnDisable")]
    internal static class PerSlotSawHurtZoneDisablePatch
    {
        private static void Prefix(HurtZone __instance) => SlotResourceOwnerResolver.Resolve(__instance?.sourceWeapon?.GetComponent<PerSlotSawedOnState>())?.BeginWearNative();
        private static void Postfix(HurtZone __instance) => SlotResourceOwnerResolver.Resolve(__instance?.sourceWeapon?.GetComponent<PerSlotSawedOnState>())?.EndWearNative();
    }

    // Nailgun / Sawblade mutually-exclusive form resources -------------------

    [HarmonyPatch(typeof(Nailgun), "OnEnable")]
    internal static class PerSlotNailgunEnablePatch
    {
        private static void Prefix(Nailgun __instance)
        {
            SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotOverheatHeatsinkState>())?.BeginNative();
            SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotAttractorMagnetState>())?.BeginNative();
            SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotJumpstartRechargeState>())?.BeginNative();
        }
        private static void Postfix(Nailgun __instance)
        {
            SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotJumpstartRechargeState>())?.EndNative();
            SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotAttractorMagnetState>())?.EndNative();
            SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotOverheatHeatsinkState>())?.EndNative();
        }
    }

    [HarmonyPatch(typeof(Nailgun), "OnDisable")]
    internal static class PerSlotNailgunDisablePatch
    {
        private static void Prefix(Nailgun __instance) => SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotOverheatHeatsinkState>())?.BeginNative();
        private static void Postfix(Nailgun __instance) => SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotOverheatHeatsinkState>())?.EndNative();
    }

    [HarmonyPatch(typeof(Nailgun), "Update")]
    internal static class PerSlotNailgunUpdatePatch
    {
        private static void Prefix(Nailgun __instance)
        {
            SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotAttractorMagnetState>())?.BeginNative();
            SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotJumpstartRechargeState>())?.BeginNative();
        }
        private static void Postfix(Nailgun __instance)
        {
            SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotJumpstartRechargeState>())?.EndNative();
            SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotAttractorMagnetState>())?.EndNative();
        }
    }

    [HarmonyPatch(typeof(Nailgun), "ShootMagnet")]
    internal static class PerSlotNailgunShootMagnetPatch
    {
        private static void Prefix(Nailgun __instance) => SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotAttractorMagnetState>())?.BeginNative();
        private static void Postfix(Nailgun __instance)
        {
            PerSlotAttractorMagnetState state = SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotAttractorMagnetState>());
            state?.ClaimNewestMagnet();
            state?.EndNative();
        }
    }

    [HarmonyPatch(typeof(Zapper), "Zap")]
    internal static class PerSlotJumpstartZapPatch
    {
        private static void Prefix(Zapper __instance) => SlotResourceOwnerResolver.Resolve(__instance?.sourceWeapon?.GetComponent<PerSlotJumpstartRechargeState>())?.BeginNative();
        private static void Postfix(Zapper __instance) => SlotResourceOwnerResolver.Resolve(__instance?.sourceWeapon?.GetComponent<PerSlotJumpstartRechargeState>())?.EndNative();
    }

    // Native S.R.S. / Firestarter duplicate resources ------------------------

    [HarmonyPatch(typeof(RocketLauncher), "Update")]
    internal static class PerSlotRocketSecondaryUpdatePatch
    {
        private static void Prefix(RocketLauncher __instance) => SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotRocketSecondaryState>())?.BeginNative();
        private static void Postfix(RocketLauncher __instance) => SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotRocketSecondaryState>())?.EndNative();
    }

    [HarmonyPatch(typeof(RocketLauncher), "FixedUpdate")]
    internal static class PerSlotRocketSecondaryFixedUpdatePatch
    {
        private static void Prefix(RocketLauncher __instance) => SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotRocketSecondaryState>())?.BeginNative();
        private static void Postfix(RocketLauncher __instance) => SlotResourceOwnerResolver.Resolve(__instance?.GetComponent<PerSlotRocketSecondaryState>())?.EndNative();
    }

    [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Respawn))]
    internal static class PerSlotVanillaResourceRespawnPatch
    {
        private static void Postfix()
        {
            SlotResourceOwnerResolver.ClearCache();
            foreach (PerSlotRevolverSecondaryState state in Resources.FindObjectsOfTypeAll<PerSlotRevolverSecondaryState>())
                state?.ResetFull();
            foreach (PerSlotSawedOnState state in Resources.FindObjectsOfTypeAll<PerSlotSawedOnState>())
                state?.ResetFull();
            foreach (PerSlotOverheatHeatsinkState state in Resources.FindObjectsOfTypeAll<PerSlotOverheatHeatsinkState>())
                state?.ResetFull();
            foreach (PerSlotAttractorMagnetState state in Resources.FindObjectsOfTypeAll<PerSlotAttractorMagnetState>())
                state?.ResetFull();
            foreach (PerSlotJumpstartRechargeState state in Resources.FindObjectsOfTypeAll<PerSlotJumpstartRechargeState>())
                state?.ResetFull();
            foreach (PerSlotRocketSecondaryState state in Resources.FindObjectsOfTypeAll<PerSlotRocketSecondaryState>())
                state?.ResetFull();
        }
    }
}
