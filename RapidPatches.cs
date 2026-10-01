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
    [HarmonyPatch(typeof(Nailgun), "ShootMagnet")]
    internal static class PoisonNailgunMagnetSuppressPatch
    {
        private static bool Prefix(Nailgun __instance)
        {
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            return marker == null || (marker.element != ElementId.Grass && marker.element != ElementId.Wind);
        }
    }

    [HarmonyPatch(typeof(Nailgun), "ShootZapper")]
    internal static class PoisonNailgunZapperSuppressPatch
    {
        private static bool Prefix(Nailgun __instance)
        {
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            return marker == null || (marker.element != ElementId.Grass && marker.element != ElementId.Wind);
        }
    }

    [HarmonyPatch(typeof(Nailgun), "UpdateZapHud")]
    internal static class PoisonNailgunNativeHudSuppressPatch
    {
        private static bool Prefix(Nailgun __instance)
        {
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            return marker == null || (marker.element != ElementId.Grass && marker.element != ElementId.Wind);
        }
    }

    [HarmonyPatch(typeof(Nailgun), "Update")]
    internal static class ElementalJumpstartRapidUiPatch
    {
        private static readonly AccessTools.FieldRef<Nailgun, float> CurrentFireRate = AccessTools.FieldRefAccess<Nailgun, float>("currentFireRate");
        private static readonly AccessTools.FieldRef<Nailgun, GameObject> RechargingOverlay = AccessTools.FieldRefAccess<Nailgun, GameObject>("rechargingOverlay");
        private static readonly AccessTools.FieldRef<Nailgun, Image> RechargingMeter = AccessTools.FieldRefAccess<Nailgun, Image>("rechargingMeter");
        private static readonly AccessTools.FieldRef<Nailgun, TMP_Text> StatusText = AccessTools.FieldRefAccess<Nailgun, TMP_Text>("statusText");
        private static readonly AccessTools.FieldRef<Nailgun, Slider> DistanceMeter = AccessTools.FieldRefAccess<Nailgun, Slider>("distanceMeter");
        private static readonly AccessTools.FieldRef<Nailgun, Slider> ZapMeter = AccessTools.FieldRefAccess<Nailgun, Slider>("zapMeter");
        private static readonly AccessTools.FieldRef<Nailgun, Image> WarningX = AccessTools.FieldRefAccess<Nailgun, Image>("warningX");

        private static void Prefix(Nailgun __instance)
        {
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            if (marker != null && marker.element == ElementId.Grass)
                __instance.spread = WeaponTuning.PoisonNailSpread;
        }

        private static void Postfix(Nailgun __instance)
        {
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            if (marker == null || (marker.element != ElementId.Grass && marker.element != ElementId.Wind))
                return;

            if (marker.element == ElementId.Wind)
            {
                // Tempest intentionally uses Jumpstart Sawblade Launcher's status-screen chassis,
                // but owns the cable alt and uses infinite regular saws. Hide native cable widgets
                // and repurpose the recharge overlay/status for the six-saw burst cooldown.
                if (__instance.heatSinkImages != null)
                {
                    foreach (Image image in __instance.heatSinkImages)
                        if (image != null) image.gameObject.SetActive(false);
                }
                Slider windDistance = DistanceMeter(__instance);
                if (windDistance != null) windDistance.gameObject.SetActive(false);
                Slider windZap = ZapMeter(__instance);
                if (windZap != null) windZap.gameObject.SetActive(false);
                Image windWarning = WarningX(__instance);
                if (windWarning != null) windWarning.enabled = false;

                TempestSawbladeController tempest = __instance.GetComponent<TempestSawbladeController>();
                if (tempest == null)
                    return;
                bool windCooling = tempest.CooldownRemaining > 0f;
                GameObject windOverlay = RechargingOverlay(__instance);
                if (windOverlay != null) windOverlay.SetActive(windCooling);
                Image windMeter = RechargingMeter(__instance);
                if (windMeter != null)
                {
                    windMeter.fillAmount = tempest.CooldownReadyFraction;
                    windMeter.color = ElementPalette.Accent(ElementId.Wind);
                }
                TMP_Text windStatus = StatusText(__instance);
                if (windStatus != null)
                {
                    windStatus.gameObject.SetActive(true);
                    windStatus.color = ElementPalette.Accent(ElementId.Wind);
                    windStatus.text = windCooling
                        ? "TEMPEST " + Mathf.RoundToInt(tempest.CooldownReadyFraction * 100f) + "%"
                        : "TEMPEST READY";
                }
                return;
            }

            // Jumpstart's primary already has the slow-unattached / fast-attached cadence we want.
            // A live Poison Seed is our "attached" state, without creating or polling every seed
            // component every frame.
            CurrentFireRate(__instance) = PoisonSeedStatus.AnyLive
                ? __instance.fireRate
                : __instance.fireRate + WeaponTuning.PoisonSeedFireRateBonus;

            PoisonNailgunController controller = __instance.GetComponent<PoisonNailgunController>();
            if (controller == null)
                return;

            if (__instance.heatSinkImages != null)
            {
                foreach (Image image in __instance.heatSinkImages)
                    if (image != null) image.gameObject.SetActive(false);
            }

            Slider distance = DistanceMeter(__instance);
            if (distance != null) distance.gameObject.SetActive(false);
            Slider zap = ZapMeter(__instance);
            if (zap != null) zap.gameObject.SetActive(false);
            Image warning = WarningX(__instance);
            if (warning != null) warning.enabled = false;

            bool cooling = controller.CooldownRemaining > 0f;
            GameObject overlay = RechargingOverlay(__instance);
            if (overlay != null) overlay.SetActive(cooling);
            Image meter = RechargingMeter(__instance);
            if (meter != null)
            {
                meter.fillAmount = controller.CooldownReadyFraction;
                meter.color = ElementPalette.Accent(ElementId.Grass);
            }

            TMP_Text status = StatusText(__instance);
            if (status != null)
            {
                status.gameObject.SetActive(true);
                status.color = ElementPalette.Accent(ElementId.Grass);
                status.text = cooling
                    ? "SEED " + Mathf.RoundToInt(controller.CooldownReadyFraction * 100f) + "%"
                    : "SEED READY";
            }
        }
    }

    [HarmonyPatch(typeof(Nailgun), "RefreshHeatSinkFill")]
    internal static class ElementalNailgunReadySoundPatch
    {
        private struct ReadySoundState
        {
            internal bool customRapid;
            internal bool requested;
            internal float before;
        }

        private static readonly AccessTools.FieldRef<Nailgun, float> HeatSinkFill =
            AccessTools.FieldRefAccess<Nailgun, float>("heatSinkFill");
        private static readonly AccessTools.FieldRef<Nailgun, AudioSource> NailgunAudio =
            AccessTools.FieldRefAccess<Nailgun, AudioSource>("aud");

        private static void Prefix(Nailgun __instance, ref bool playSound, out ReadySoundState __state)
        {
            __state = default(ReadySoundState);
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            if (marker == null || marker.family != WeaponFamily.Rapid || !marker.IsCustom)
                return;

            __state.customRapid = true;
            __state.requested = playSound;
            __state.before = HeatSinkFill(__instance);

            // EB recolors the completed heatsink images. Vanilla uses that image color as part of
            // its "did this charge just finish?" test, so leaving playSound enabled makes the
            // recolored Fire/Water rapid weapons replay the ready sound every frame.
            playSound = false;
        }

        private static void Postfix(Nailgun __instance, ReadySoundState __state)
        {
            if (!__state.customRapid || !__state.requested)
                return;

            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            if (marker == null || marker.element == ElementId.Grass || marker.element == ElementId.Wind)
                return; // Poison Seed/Tempest own completely different cooldown readouts.

            GunControl gc = MonoSingleton<GunControl>.Instance;
            if (gc == null || gc.currentWeapon != __instance.gameObject || !__instance.gameObject.activeInHierarchy)
                return;

            float after = HeatSinkFill(__instance);
            if (after <= __state.before || __instance.heatSinkImages == null)
                return;

            AudioSource audio = NailgunAudio(__instance);
            if (audio == null)
                return;

            // Reproduce Nailgun.RefreshHeatSinkFill's intended one-shot sound at each whole
            // charge boundary, but key it off the numeric fill crossing instead of image color.
            for (int i = 0; i < __instance.heatSinkImages.Length; i++)
            {
                float threshold = i + 1f;
                if (__state.before < threshold && after >= threshold)
                {
                    audio.SetPitch(i * 0.5f + 1f);
                    audio.Play(tracked: true);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Nailgun), "Update")]
    internal static class CustomJumpstartNativeAltInputShieldPatch
    {
        private static void Prefix(Nailgun __instance, out float __state)
        {
            __state = float.NaN;
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            WeaponCharges charges = MonoSingleton<WeaponCharges>.Instance;
            if (marker == null || (marker.element != ElementId.Grass && marker.element != ElementId.Wind) ||
                marker.family != WeaponFamily.Rapid || charges == null)
                return;
            // Poison and Tempest borrow Jumpstart chassis/status presentation, but their RMB belongs
            // entirely to EB. Native Jumpstart checks the global cable recharge before ShootZapper
            // (which EB suppresses) and can otherwise leak Storm's recharge state/no-charge feedback
            // into these custom weapons. Lend native Update a temporary full cable charge only.
            __state = charges.naiZapperRecharge;
            charges.naiZapperRecharge = Mathf.Max(5f, charges.naiZapperRecharge);
        }

        private static void Postfix(float __state)
        {
            if (float.IsNaN(__state))
                return;
            WeaponCharges charges = MonoSingleton<WeaponCharges>.Instance;
            if (charges != null)
                charges.naiZapperRecharge = __state;
        }
    }

    [HarmonyPatch(typeof(Nailgun), "OnEnable")]
    internal static class EarthRockSawNativeHeatsinkEnableIsolationPatch
    {
        private static void Prefix(Nailgun __instance, out float __state)
        {
            __state = float.NaN;
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            WeaponCharges charges = MonoSingleton<WeaponCharges>.Instance;
            if (marker == null || marker.element != ElementId.Earth || marker.family != WeaponFamily.Rapid ||
                !__instance.altVersion || charges == null)
                return;
            __state = charges.naiSawHeatsinks;
            // Rock Sawblade borrows the Overheat Saw chassis but does not own/use vanilla heatsinks.
            // Load a full temporary value so another vanilla Sawblade's heatsink cooldown cannot slow
            // Rock's primary or alter its presentation when this weapon is equipped.
            charges.naiSawHeatsinks = 1f;
        }

        private static void Postfix(float __state)
        {
            if (float.IsNaN(__state))
                return;
            WeaponCharges charges = MonoSingleton<WeaponCharges>.Instance;
            if (charges != null)
                charges.naiSawHeatsinks = __state;
        }
    }

    [HarmonyPatch(typeof(Nailgun), "OnDisable")]
    internal static class EarthRockSawNativeHeatsinkDisableIsolationPatch
    {
        private static void Prefix(Nailgun __instance, out float __state)
        {
            __state = float.NaN;
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            WeaponCharges charges = MonoSingleton<WeaponCharges>.Instance;
            if (marker == null || marker.element != ElementId.Earth || marker.family != WeaponFamily.Rapid ||
                !__instance.altVersion || charges == null)
                return;
            __state = charges.naiSawHeatsinks;
        }

        private static void Postfix(float __state)
        {
            if (float.IsNaN(__state))
                return;
            WeaponCharges charges = MonoSingleton<WeaponCharges>.Instance;
            if (charges != null)
                charges.naiSawHeatsinks = __state;
        }
    }

    [HarmonyPatch(typeof(Nailgun), "FixedUpdate")]
    internal static class WindSawbladeFireIntervalPatch
    {
        private static readonly AccessTools.FieldRef<Nailgun, float> CurrentFireRate =
            AccessTools.FieldRefAccess<Nailgun, float>("currentFireRate");

        private static void Prefix(Nailgun __instance)
        {
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            if (marker == null || (marker.element != ElementId.Wind && marker.element != ElementId.Earth) || marker.family != WeaponFamily.Rapid || !__instance.altVersion)
                return;
            // Nailgun's fireCooldown counts down at 100 units/second. Earth deliberately uses
            // the same base cadence as the blue Sawblade Launcher, but keeps its own tuning field.
            float interval = marker.element == ElementId.Earth ? WeaponTuning.RockPrimaryInterval : WeaponTuning.Defaults.TempestPrimaryInterval;
            CurrentFireRate(__instance) = interval * 100f;
        }
    }


    [HarmonyPatch(typeof(Nailgun), "FixedUpdate")]
    internal static class TempestBurstPrimaryLockPatch
    {
        private static readonly AccessTools.FieldRef<Nailgun, bool> CanShoot =
            AccessTools.FieldRefAccess<Nailgun, bool>("canShoot");

        private static void Prefix(Nailgun __instance, out bool __state)
        {
            __state = false;
            TempestSawbladeController controller = __instance != null ? __instance.GetComponent<TempestSawbladeController>() : null;
            if (controller == null || !controller.BurstActive)
                return;
            __state = CanShoot(__instance);
            CanShoot(__instance) = false;
        }

        private static Exception Finalizer(Nailgun __instance, bool __state, Exception __exception)
        {
            if (__instance != null && __state)
                CanShoot(__instance) = true;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Nailgun), "Shoot")]
    internal static class WindSawbladeInfiniteAmmoPatch
    {
        private static void Prefix(Nailgun __instance, out float __state)
        {
            __state = float.NaN;
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            WeaponCharges charges = MonoSingleton<WeaponCharges>.Instance;
            if (marker == null || (marker.element != ElementId.Wind && marker.element != ElementId.Earth) || marker.family != WeaponFamily.Rapid ||
                !__instance.altVersion || charges == null)
                return;
            __state = charges.naiSaws;
        }

        private static void Postfix(float __state)
        {
            if (float.IsNaN(__state))
                return;
            WeaponCharges charges = MonoSingleton<WeaponCharges>.Instance;
            if (charges != null)
                charges.naiSaws = __state;
        }
    }

    [HarmonyPatch(typeof(Nail), "DamageEnemy")]
    internal static class PoisonNailHitPatch
    {
        private static void Postfix(Nail __instance, EnemyIdentifier eid)
        {
            if (__instance == null || eid == null || eid.dead || __instance.enemy)
                return;
            if (eid.GetComponent<PoisonSeedStatus>() == null)
                return;
            PoisonStatus.Apply(eid, __instance.sourceWeapon, WeaponTuning.PoisonDuration, WeaponTuning.PoisonPotency);
        }
    }

    [HarmonyPatch(typeof(Nail), "Update")]
    internal static class HeatedNailSporeIgnitionPatch
    {
        private static void Postfix(Nail __instance)
        {
            if (__instance == null || !__instance.heated || RuntimeRegistry.SporeClouds.Count == 0)
                return;
            RuntimeRegistry.IgniteSporeClouds(__instance.transform.position, 1f, __instance.sourceWeapon);
        }
    }

    [HarmonyPatch(typeof(Nail), "Start")]
    internal static class CounterBuffNailCloudIgnitionPatch
    {
        private static void Postfix(Nail __instance)
        {
            if (!RuntimeRegistry.CounterBuffActive || __instance == null || __instance.sourceWeapon == null) return;
            if (MarkerUtility.Find(__instance.sourceWeapon) == null) return;
            FireTrailCloudIgniter igniter = __instance.gameObject.GetComponent<FireTrailCloudIgniter>();
            if (igniter == null) igniter = __instance.gameObject.AddComponent<FireTrailCloudIgniter>();
            igniter.sourceWeapon = __instance.sourceWeapon;
            igniter.radius = 0.8f;
        }
    }

    [HarmonyPatch(typeof(Projectile), "Start")]
    internal static class CounterShotgunPelletBuffPatch
    {
        private static void Postfix(Projectile __instance)
        {
            if (!RuntimeRegistry.CounterBuffActive || __instance == null || __instance.sourceWeapon == null)
                return;
            ElementalSlotMarker marker = MarkerUtility.Find(__instance.sourceWeapon);
            if (marker == null)
                return;
            FireTrailCloudIgniter igniter = __instance.gameObject.GetComponent<FireTrailCloudIgniter>();
            if (igniter == null) igniter = __instance.gameObject.AddComponent<FireTrailCloudIgniter>();
            igniter.sourceWeapon = __instance.sourceWeapon;
            igniter.radius = 1.2f;
            if (marker.family == WeaponFamily.Shotgun && marker.element == ElementId.Fire &&
                !string.IsNullOrEmpty(__instance.weaponType) && __instance.weaponType.StartsWith("shotgun", StringComparison.OrdinalIgnoreCase))
                __instance.damage *= 1.3f;
        }
    }


    [HarmonyPatch(typeof(Nailgun), "Update")]
    internal static class ElementalRapidSharedHeatUpdatePatch
    {
        private struct RapidHeatState
        {
            internal bool active;
            internal bool earthIsolation;
            internal ElementId element;
            internal float sharedHeat;
        }

        private static readonly AccessTools.FieldRef<Nailgun, float> HeatUp =
            AccessTools.FieldRefAccess<Nailgun, float>("heatUp");
        private static readonly AccessTools.FieldRef<Nailgun, bool> CanShoot =
            AccessTools.FieldRefAccess<Nailgun, bool>("canShoot");
        private static readonly AccessTools.FieldRef<Nailgun, bool> BurnOut =
            AccessTools.FieldRefAccess<Nailgun, bool>("burnOut");
        private static readonly AccessTools.FieldRef<Nailgun, float> HeatSinks =
            AccessTools.FieldRefAccess<Nailgun, float>("heatSinks");
        private static readonly AccessTools.FieldRef<Nailgun, float> FireCooldown =
            AccessTools.FieldRefAccess<Nailgun, float>("fireCooldown");

        internal static void SetSharedHeat(Nailgun nailgun, float heat)
        {
            if (nailgun != null)
                HeatUp(nailgun) = Mathf.Clamp01(heat);
        }

        private static void Prefix(Nailgun __instance, out RapidHeatState __state)
        {
            __state = default;
            if (__instance == null)
                return;

            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            if (marker == null || marker.family != WeaponFamily.Rapid || !marker.IsCustom)
                return;

            bool earthIsolation = marker.element == ElementId.Earth && __instance.altVersion;
            if (!earthIsolation)
                return;

            __state.active = true;
            __state.element = marker.element;
            __state.sharedHeat = HeatUp(__instance);

            if (earthIsolation)
            {
                // Rock Sawblade borrows the Overheat Saw chassis only for its model/animations
                // and authored SuperShoot presentation. Hide shared Rapid heat from vanilla
                // Update so it cannot drive glow, spin, fire-rate, or native Overheat behavior.
                __state.earthIsolation = true;
                __instance.GetComponent<RockSawbladeController>()?.BeginNativeHeatIsolation();
                HeatUp(__instance) = 0f;
                HeatSinks(__instance) = 1f;
                BurnOut(__instance) = false;
            }
        }

        private static void Postfix(Nailgun __instance, RapidHeatState __state)
        {
            if (!__state.active || __instance == null)
                return;

            if (__state.earthIsolation)
            {
                RockSawbladeController controller = __instance.GetComponent<RockSawbladeController>();
                float heat;
                if (controller != null && controller.TryConsumePrimaryHeatWrite(out float authoredHeat))
                {
                    // A real primary shot happened inside this native Update. Preserve its authored
                    // SET value (including an intentional 0) instead of restoring the pre-Update heat.
                    heat = authoredHeat;
                }
                else
                {
                    heat = __state.sharedHeat;
                    if (heat > 0f)
                    {
                        if (BurnOut(__instance) || HeatSinks(__instance) < 1f)
                        {
                            heat = Mathf.MoveTowards(heat, 0f, Time.deltaTime);
                        }
                        else
                        {
                            InputManager input = MonoSingleton<InputManager>.Instance;
                            bool primaryHeld = input != null && input.InputSource != null && input.InputSource.Fire1 != null &&
                                               input.InputSource.Fire1.IsPressed;
                            if (!CanShoot(__instance) || !primaryHeld)
                            {
                                // Match vanilla Sawblade shared-heat decay while keeping the heat
                                // invisible to the borrowed Overheat chassis itself.
                                float decay = FireCooldown(__instance) <= 0f ? 0.2f : 0.03f;
                                heat = Mathf.MoveTowards(heat, 0f, Time.deltaTime * decay);
                            }
                        }
                    }
                }

                HeatUp(__instance) = Mathf.Clamp01(heat);
                return;
            }
        }
    }

    [HarmonyPatch(typeof(Nailgun), "Shoot")]
    internal static class ElementalRapidShotHeatPatch
    {
        private struct ShotHeatState
        {
            internal bool active;
            internal bool earthPrimary;
            internal ElementId element;
        }

        private static readonly AccessTools.FieldRef<Nailgun, float> HeatUp =
            AccessTools.FieldRefAccess<Nailgun, float>("heatUp");
        private static void Prefix(Nailgun __instance, out ShotHeatState __state)
        {
            __state = default;
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            if (marker == null || marker.family != WeaponFamily.Rapid || !marker.IsCustom)
                return;
            if (marker.element != ElementId.Grass && marker.element != ElementId.Wind && marker.element != ElementId.Earth)
                return;

            // Earth only owns the alternate Overheat-Saw chassis. Guard this explicitly in case
            // the loadout registry changes later instead of accidentally normalizing another form.
            if (marker.element == ElementId.Earth && !__instance.altVersion)
                return;

            __state.active = true;
            __state.element = marker.element;

            if (marker.element == ElementId.Earth)
            {
                __state.earthPrimary = true;
                // Earth uses the Overheat Saw prefab as a chassis, but its primary projectile is a
                // regular non-silver saw. Spawn it from a cold native state so heat never changes
                // the projectile's spread/multi-hit behavior.
                HeatUp(__instance) = 0f;
            }
        }

        private static void Postfix(Nailgun __instance, ShotHeatState __state)
        {
            if (!__state.active || __instance == null)
                return;

            float heat;
            switch (__state.element)
            {
                case ElementId.Grass:
                    heat = PoisonSeedStatus.AnyLive
                        ? WeaponTuning.GrassRapidHeatWithLiveSeed
                        : WeaponTuning.GrassRapidHeatWhileFiring;
                    break;
                case ElementId.Wind:
                    heat = WeaponTuning.WindRapidHeatWhileFiring;
                    break;
                case ElementId.Earth:
                    if (!__state.earthPrimary)
                        return;
                    heat = WeaponTuning.EarthRapidHeatOnPrimaryShot;
                    break;
                default:
                    return;
            }

            heat = Mathf.Clamp01(heat);
            HeatUp(__instance) = heat;

            if (__state.element == ElementId.Earth)
                __instance.GetComponent<RockSawbladeController>()?.NotifyPrimaryHeatWrite(heat);
        }
    }

    [HarmonyPatch(typeof(Nail), "Start")]
    internal static class ElementalCustomSawHitAmountPatch
    {
        private static void Postfix(Nail __instance)
        {
            if (__instance == null || !__instance.sawblade || __instance.sourceWeapon == null)
                return;
            ElementalSlotMarker marker = MarkerUtility.Find(__instance.sourceWeapon);
            if (marker == null || marker.family != WeaponFamily.Rapid || !marker.IsCustom)
                return;

            switch (marker.element)
            {
                case ElementId.Wind:
                    // Tempest is a custom Jumpstart-saw weapon. Vanilla Jumpstart hard-codes
                    // hitAmount=1; EB owns this custom weapon's durability instead.
                    __instance.hitAmount = Mathf.Max(1f, WeaponTuning.WindSawHitAmount);
                    break;
                case ElementId.Earth:
                    // Rock Sawblade borrows the Overheat Saw chassis, but its projectiles are
                    // intentionally ordinary non-silver saws with an authored durability budget.
                    __instance.hitAmount = Mathf.Max(1f, WeaponTuning.EarthSawHitAmount);
                    __instance.multiHitAmount = 1;
                    __instance.heated = false;
                    break;
            }
        }
    }

    [HarmonyPatch(typeof(Railcannon), "Update")]
    internal static class CustomUltimateNativeUpdatePatch
    {
        private struct ChargeState
        {
            internal bool tracked;
            internal float realNativeCharge;
            internal float realLogicalCharge;
            internal float cost;
        }

        private static bool Prefix(Railcannon __instance, out ChargeState __state)
        {
            __state = default(ChargeState);
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            if (marker == null || !marker.IsCustom || marker.element == ElementId.Storm)
                return true;
            if (!string.IsNullOrEmpty(marker.elementId) && ElementRegistry.TryGet(marker.elementId, out ElementRegistryRecord registration) && registration.external)
                return true;

            // Grass has a bespoke repeated-fire ultimate and already owns its complete update loop.
            if (marker.element == ElementId.Grass)
                return false;

            WeaponCharges charges = MonoSingleton<WeaponCharges>.Instance;
            UltimateController controller = __instance.GetComponent<UltimateController>();
            if (charges == null || controller == null)
                return true;

            __state.tracked = true;
            __state.realNativeCharge = charges.raicharge;
            __state.realLogicalCharge = UltimateChargeRules.LogicalFromNative(charges.raicharge);
            __state.cost = Mathf.Clamp(WeaponTuning.UltimateCost(marker.element), 0.05f, WeaponTuning.Defaults.UltimateChargeMax);

            // Let the stock Railcannon own input, zoom, animations, ready audio and firing. Native
            // raicharge fills 0..4 and then snaps to the ready sentinel 5, so compare against EB's
            // normalized logical charge rather than subtracting raw raicharge units. When enough
            // logical charge exists, temporarily present a native-ready bar. Postfix converts the
            // remaining logical charge back to vanilla raw-meter space.
            if (!NoWeaponCooldown.NoCooldown &&
                __state.realLogicalCharge + 0.0001f >= __state.cost &&
                __state.realNativeCharge < WeaponTuning.Defaults.UltimateChargeMax - 0.0001f)
            {
                charges.raicharge = WeaponTuning.Defaults.UltimateChargeMax;
            }

            return true;
        }

        private static void Postfix(ChargeState __state)
        {
            if (!__state.tracked || NoWeaponCooldown.NoCooldown)
                return;

            WeaponCharges charges = MonoSingleton<WeaponCharges>.Instance;
            if (charges == null)
                return;

            // Native Railcannon.Update sets the presented charge to zero on the frame it accepts
            // Fire1, before calling (or scheduling) Shoot(). Detect that state directly instead of
            // waiting for the custom Shoot prefix, because wid.delay can defer Shoot to a later
            // frame. This keeps fractional custom costs correct even in delayed/dual-wield paths.
            bool hadEnoughCharge = __state.realLogicalCharge + 0.0001f >= __state.cost;
            bool nativeAcceptedShot = hadEnoughCharge && charges.raicharge <= 0.0001f;
            if (nativeAcceptedShot)
            {
                float remainingLogical = Mathf.Max(0f, __state.realLogicalCharge - __state.cost);
                charges.raicharge = UltimateChargeRules.NativeFromLogical(remainingLogical);
                charges.railChargePlayed = false;
            }
            else
            {
                charges.raicharge = __state.realNativeCharge;
            }
        }
    }

    [HarmonyPatch(typeof(Railcannon), "Shoot")]
    internal static class CustomUltimateShootPatch
    {
        private static bool Prefix(Railcannon __instance)
        {
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            if (marker == null || !marker.IsCustom || marker.element == ElementId.Grass || marker.element == ElementId.Storm)
                return true;
            if (!string.IsNullOrEmpty(marker.elementId) && ElementRegistry.TryGet(marker.elementId, out ElementRegistryRecord registration) && registration.external)
                return true;

            UltimateController controller = __instance.GetComponent<UltimateController>();
            if (controller == null)
                return true;

            controller.ActivateFromFullCharge();
            return false;
        }
    }



    [HarmonyPatch(typeof(Nailgun), "Update")]
    internal static class EarthRockCooldownUiPatch
    {
        private static void Postfix(Nailgun __instance)
        {
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            RockSawbladeController controller = __instance != null ? __instance.GetComponent<RockSawbladeController>() : null;
            if (marker == null || marker.element != ElementId.Earth || marker.family != WeaponFamily.Rapid || controller == null)
                return;
            float ready = controller.CooldownReadyFraction;
            Color accent = ElementPalette.Accent(ElementId.Earth);
            if (__instance.heatSinkImages != null)
            {
                foreach (Image image in __instance.heatSinkImages)
                {
                    if (image == null) continue;
                    image.fillAmount = ready;
                    image.color = ready >= 0.999f ? accent : Color.Lerp(new Color(0.12f, 0.08f, 0.03f, image.color.a), accent, ready);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Nailgun), "SuperSaw")]
    internal static class EarthRockNativeSuperSawSuppressPatch
    {
        private static bool Prefix(Nailgun __instance)
        {
            ElementalSlotMarker marker = MarkerUtility.Find(__instance);
            return marker == null || marker.element != ElementId.Earth || marker.family != WeaponFamily.Rapid;
        }
    }

    [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Respawn))]
    internal static class ElementalRespawnCooldownResetPatch
    {
        private static void Postfix()
        {
            // Match the lifecycle used by Grenade Launcher 2.0.2: death/respawn should clear
            // mod-authored cooldowns rather than carrying them into the fresh life.
            foreach (ActiveWeaponController controller in Resources.FindObjectsOfTypeAll<ActiveWeaponController>())
                controller?.ResetCooldown();
            foreach (UltimateController controller in Resources.FindObjectsOfTypeAll<UltimateController>())
                controller?.ResetOnRespawn();
            foreach (ElementalJackhammerPrimaryCooldownState state in Resources.FindObjectsOfTypeAll<ElementalJackhammerPrimaryCooldownState>())
                state?.Clear();
            foreach (ElementalAltRevolverPrimaryCooldownState state in Resources.FindObjectsOfTypeAll<ElementalAltRevolverPrimaryCooldownState>())
                state?.Clear();

            RuntimeRegistry.ResetPlayerTransientState();
        }
    }

    [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.GetHurt))]
    internal static class EarthFortifyHardDamagePatch
    {
        private static void Prefix(ref float hardDamageMultiplier)
        {
            if (RuntimeRegistry.FortifyActive)
                hardDamageMultiplier *= WeaponTuning.FortifyHardDamageMultiplier;
        }
    }

    [HarmonyPatch(typeof(WeaponCharges), nameof(WeaponCharges.Charge))]
    internal static class InfernoRailChargeFreezePatch
    {
        private static void Prefix(WeaponCharges __instance, out float __state)
        {
            __state = __instance.raicharge;
        }

        private static void Postfix(WeaponCharges __instance, float __state)
        {
            if (RuntimeRegistry.FreezeRailCharge)
                __instance.raicharge = __state;
        }
    }

}
