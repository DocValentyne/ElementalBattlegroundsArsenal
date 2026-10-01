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
    [HarmonyPatch(typeof(Revolver), "Update")]
    internal static class RevolverCustomAltSuppressPatch
    {
        private static readonly AccessTools.FieldRef<Revolver, bool> PierceReady = AccessTools.FieldRefAccess<Revolver, bool>("pierceReady");
        private static readonly AccessTools.FieldRef<Revolver, float> PierceCharge = AccessTools.FieldRefAccess<Revolver, float>("pierceCharge");
        private static readonly AccessTools.FieldRef<Revolver, float> PierceShotCharge = AccessTools.FieldRefAccess<Revolver, float>("pierceShotCharge");
        private static readonly AccessTools.FieldRef<Revolver, bool> ChargingPierce = AccessTools.FieldRefAccess<Revolver, bool>("chargingPierce");
        private static readonly AccessTools.FieldRef<Revolver, bool> ShootReady = AccessTools.FieldRefAccess<Revolver, bool>("shootReady");
        private static readonly AccessTools.FieldRef<Revolver, float> ShootCharge = AccessTools.FieldRefAccess<Revolver, float>("shootCharge");

        private static void Prefix(Revolver __instance)
        {
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            if (marker == null || marker.family != WeaponFamily.Revolver ||
                (marker.element != ElementId.Fire && marker.element != ElementId.Grass && marker.element != ElementId.Wind && marker.element != ElementId.Earth))
                return;
            ActiveWeaponController active = __instance.GetComponent<ActiveWeaponController>();
            bool ready = active == null || active.CooldownRemaining <= 0f;
            float readyFraction = active == null ? 1f : active.CooldownReadyFraction;
            PierceReady(__instance) = ready;
            // Keep vanilla from repeatedly crossing 100 and replaying its charge-complete SFX.
            PierceCharge(__instance) = ready ? 100f : Mathf.Min(98f, readyFraction * 98f);
            PierceShotCharge(__instance) = 0f;
            ChargingPierce(__instance) = false;
            if (ready)
            {
                CustomRevolverController elemental = __instance.GetComponent<CustomRevolverController>();
                if (elemental != null)
                    elemental.TryFireCustomAlt();
                else
                {
                    PressureRevolverController pressure = __instance.GetComponent<PressureRevolverController>();
                    if (pressure != null) pressure.TryFireCustomAlt();
                    else __instance.GetComponent<FaultlineRevolverController>()?.TryFireCustomAlt();
                }
            }
        }
    }

    [HarmonyPatch(typeof(Revolver), "OnDisable")]
    internal static class CustomRevolverGlobalChargeIsolationPatch
    {
        private struct ChargeState
        {
            internal bool custom;
            internal float charge;
            internal bool alt;
        }

        private static void Prefix(Revolver __instance, out ChargeState __state)
        {
            __state = default(ChargeState);
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            WeaponCharges wc = MonoSingleton<WeaponCharges>.Instance;
            if (marker == null || marker.family != WeaponFamily.Revolver ||
                (marker.element != ElementId.Fire && marker.element != ElementId.Grass && marker.element != ElementId.Wind && marker.element != ElementId.Earth) ||
                __instance.gunVariation != 0 || wc == null)
                return;

            __state.custom = true;
            __state.charge = wc.rev0charge;
            __state.alt = wc.rev0alt;
        }

        private static void Postfix(ChargeState __state)
        {
            if (!__state.custom)
                return;
            WeaponCharges wc = MonoSingleton<WeaponCharges>.Instance;
            if (wc == null)
                return;

            // Revolver.OnDisable normally persists gunVariation 0 into one global bucket.
            // EB can have several independent Piercer-backed weapons, so letting Fire/Grass write
            // here makes Water inherit their custom cooldown state and even their alt recharge rate.
            wc.rev0charge = __state.charge;
            wc.rev0alt = __state.alt;
        }
    }

    [HarmonyPatch(typeof(Revolver), "OnEnable")]
    internal static class ElementalAltRevolverPrimaryCooldownEnablePatch
    {
        private static void Prefix(Revolver __instance)
        {
            __instance?.GetComponent<ElementalAltRevolverPrimaryCooldownState>()?.BeginNative(__instance);
        }

        private static void Postfix(Revolver __instance)
        {
            __instance?.GetComponent<ElementalAltRevolverPrimaryCooldownState>()?.EndNative(__instance, false);
        }
    }

    [HarmonyPatch(typeof(Revolver), "Shoot")]
    internal static class ElementalAltRevolverPrimaryCooldownShootIsolationPatch
    {
        private static void Prefix(Revolver __instance)
        {
            __instance?.GetComponent<ElementalAltRevolverPrimaryCooldownState>()?.BeginNative(__instance);
        }

        private static void Postfix(Revolver __instance)
        {
            __instance?.GetComponent<ElementalAltRevolverPrimaryCooldownState>()?.EndNative(__instance, true);
        }
    }

    [HarmonyPatch(typeof(Revolver), "Click")]
    internal static class ElementalAltRevolverPrimaryCooldownClickIsolationPatch
    {
        private static void Prefix(Revolver __instance)
        {
            __instance?.GetComponent<ElementalAltRevolverPrimaryCooldownState>()?.BeginNative(__instance);
        }

        private static void Postfix(Revolver __instance)
        {
            __instance?.GetComponent<ElementalAltRevolverPrimaryCooldownState>()?.EndNative(__instance, true);
        }
    }

    [HarmonyPatch(typeof(Revolver), "InstaClick")]
    internal static class ElementalAltRevolverPrimaryCooldownInstaClickResetPatch
    {
        private static void Postfix(Revolver __instance)
        {
            if (__instance == null || !__instance.altVersion)
                return;

            // Preserve vanilla's intentional all-Slab quick-reset reward. Ordinary post-shot
            // recovery is isolated per duplicate EB weapon, but InstaClick explicitly clears
            // every alternate-revolver pickup timer in vanilla, so mirror that for EB-local state
            // even when the reset was triggered by an explicitly equipped vanilla Slab.
            foreach (ElementalAltRevolverPrimaryCooldownState state in Resources.FindObjectsOfTypeAll<ElementalAltRevolverPrimaryCooldownState>())
                state?.Clear();
        }
    }

    [HarmonyPatch(typeof(Revolver), "OnEnable")]
    internal static class StormRevolverChargeRestorePatch
    {
        private static void Postfix(Revolver __instance)
        {
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            if (marker == null || marker.element != ElementId.Storm || marker.family != WeaponFamily.Revolver || __instance.gunVariation != 0)
                return;
            __instance.GetComponent<StormRevolverChargeState>()?.Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(Revolver), "OnDisable")]
    internal static class StormRevolverChargeIsolationPatch
    {
        private struct ChargeState
        {
            internal bool storm;
            internal float globalCharge;
            internal bool globalAlt;
        }

        private static void Prefix(Revolver __instance, out ChargeState __state)
        {
            __state = default(ChargeState);
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            WeaponCharges wc = MonoSingleton<WeaponCharges>.Instance;
            if (marker == null || marker.element != ElementId.Storm || marker.family != WeaponFamily.Revolver ||
                __instance.gunVariation != 0 || wc == null)
                return;

            __state.storm = true;
            __state.globalCharge = wc.rev0charge;
            __state.globalAlt = wc.rev0alt;
            __instance.GetComponent<StormRevolverChargeState>()?.Capture(__instance);
        }

        private static void Postfix(ChargeState __state)
        {
            if (!__state.storm)
                return;
            WeaponCharges wc = MonoSingleton<WeaponCharges>.Instance;
            if (wc == null)
                return;
            wc.rev0charge = __state.globalCharge;
            wc.rev0alt = __state.globalAlt;
        }
    }

    [HarmonyPatch(typeof(RevolverBeam), "Start")]
    internal static class StormChargedBeamTagPatch
    {
        private static void Prefix(RevolverBeam __instance)
        {
            if (__instance == null || __instance.sourceWeapon == null ||
                (!__instance.strongAlt && __instance.hitAmount <= 1))
                return;
            ElementalSlotMarker marker = MarkerUtility.Find(__instance.sourceWeapon);
            if (marker == null || marker.element != ElementId.Storm || marker.family != WeaponFamily.Revolver)
                return;

            // Keep the Piercer's charged piercing/coin routing, but only one hitscan damage event
            // may land on each enemy. Vanilla's standard charged Piercer allows three hits on one
            // target; leaving that intact would make Storm a strict Water upgrade before its blast.
            __instance.maxHitsPerTarget = 1;

            StormChargedBeamExplosionTag tag = __instance.GetComponent<StormChargedBeamExplosionTag>();
            if (tag == null)
            {
                tag = __instance.gameObject.AddComponent<StormChargedBeamExplosionTag>();
                tag.baseBeamDamage = Mathf.Max(0.0001f, __instance.damage);
            }
            else if (tag.baseBeamDamage <= 0f)
            {
                tag.baseBeamDamage = Mathf.Max(0.0001f, __instance.damage - __instance.addedDamage);
            }
        }
    }

    [HarmonyPatch(typeof(RevolverBeam), nameof(RevolverBeam.ExecuteHits))]
    internal static class StormChargedBeamExplosionPatch
    {
        private static void Postfix(RevolverBeam __instance, PhysicsCastResult currentHit)
        {
            if (__instance == null || currentHit.transform == null)
                return;
            StormChargedBeamExplosionTag tag = __instance.GetComponent<StormChargedBeamExplosionTag>();
            if (tag == null || tag.exploded)
                return;
            Coin coin = currentHit.transform.GetComponent<Coin>() ?? currentHit.transform.GetComponentInParent<Coin>();
            if (coin != null)
                return;

            tag.exploded = true;
            float baseDamage = Mathf.Max(0.0001f, tag.baseBeamDamage);
            float coinScale = Mathf.Max(0f, __instance.damage / baseDamage);
            StormEffects.SpawnExplosion(currentHit.point, WeaponTuning.StormRevolverExplosionScale,
                WeaponTuning.StormRevolverExplosionDamage * coinScale, __instance.sourceWeapon,
                WeaponTuning.StormRevolverSelfDamage);
        }
    }

    [HarmonyPatch(typeof(Revolver), "Shoot")]
    internal static class RevolverShootPatch
    {
        private static void Postfix(Revolver __instance, int shotType)
        {
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            if (marker == null) return;
            if (RuntimeRegistry.CounterBuffActive && shotType == 1 && MonoSingleton<CameraController>.Instance != null)
            {
                CameraController cc = MonoSingleton<CameraController>.Instance;
                RuntimeRegistry.IgniteSporeCloudsAlongRay(cc.GetDefaultPos(), cc.transform.forward, 150f, 2.5f, __instance.gameObject);
            }
        }
    }

    [HarmonyPatch(typeof(RevolverBeam), nameof(RevolverBeam.ExecuteHits))]
    internal static class FireColumnCoinBeamPatch
    {
        private static bool Prefix(RevolverBeam __instance, PhysicsCastResult currentHit)
        {
            if (__instance == null || currentHit.transform == null)
                return true;
            FireColumnCoinBeamTag tag = __instance.GetComponent<FireColumnCoinBeamTag>();
            if (tag == null)
                return true;

            Coin coin = currentHit.transform.GetComponent<Coin>() ?? currentHit.transform.GetComponentInParent<Coin>();
            if (coin != null)
            {
                // RevolverBeam.ExecuteHits normally gives the coin a fresh default beam. Hand it
                // a clone of this tagged zero-damage beam instead; Coin.ReflectRevolver still
                // performs its normal target selection, obstruction checks and multi-coin power.
                GameObject continuation = UnityEngine.Object.Instantiate(__instance.gameObject, currentHit.point, __instance.transform.rotation);
                continuation.SetActive(false);
                RevolverBeam continuationBeam = continuation.GetComponent<RevolverBeam>();
                if (continuationBeam != null)
                {
                    continuationBeam.bodiesPierced = 0;
                    continuationBeam.noMuzzleflash = true;
                    continuationBeam.alternateStartPoint = Vector3.zero;
                    continuationBeam.hitEids.Clear();
                    continuationBeam.previouslyHitTransform = null;
                    continuationBeam.damage = 0f;
                    continuationBeam.coinDamageBonusMultiplier = 0f;
                }
                FireColumnCoinBeamTag continuationTag = continuation.GetComponent<FireColumnCoinBeamTag>();
                if (continuationTag != null)
                {
                    continuationTag.sourceWeapon = tag.sourceWeapon;
                    continuationTag.columnSpawned = false;
                    continuationTag.coinCount = tag.coinCount + 1;
                }
                coin.DelayedReflectRevolver(currentHit.point, continuation);
                return false;
            }

            EnemyIdentifierIdentifier eidid = currentHit.transform.GetComponentInParent<EnemyIdentifierIdentifier>();
            EnemyIdentifier eid = eidid != null ? eidid.eid : currentHit.transform.GetComponentInParent<EnemyIdentifier>();
            if (eid != null && !eid.dead && !tag.columnSpawned)
            {
                tag.columnSpawned = true;
                // Native Coin routing still decides where the shot goes, but Fire Column now
                // rewards every additional coin explicitly instead of translating Coin.addedDamage
                // (which made 1-coin and 4-coin shots far too similar in practice).
                int coins = Mathf.Clamp(tag.coinCount, 1, 4);
                float multiplier = WeaponTuning.FireColumnCoinMultiplier(coins);
                CustomRevolverController.SpawnFireColumn(currentHit.point, multiplier, tag.sourceWeapon);
                // The tagged beam exists only to drive Coin's normal path. Suppress its zero-damage
                // enemy hit so it cannot create fake headshot/style bookkeeping beside the column.
                return false;
            }

            return true;
        }
    }

}
