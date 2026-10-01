using System;
using System.Collections.Generic;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using PluginConfig.API.Functionals;
using UnityEngine;

namespace ElementalBattlegroundsMod
{
    internal static class WeaponTuning
    {
        internal static class Defaults
        {
            internal const float UltimateChargeMax = 5f;
            internal const float FireColumnCooldown = 5f;
            internal const float FireColumnLifetime = 1.35f;
            internal const float FireColumnTick = 0.2f;
            internal const float FireColumnDamage = 1.08f;
            internal const float FireColumnRadius = 4.25f;
            internal const float FireColumnHeight = 14f;
            internal const float FireColumnFourCoinTotalDamage = 25f;
            internal const float VineCooldown = 3f;
            internal const float VinePullDuration = 1.35f;
            internal const float VineHoldDuration = 0.65f;
            internal const float VineStartRadius = 15f;
            internal const float VineEndRadius = 2.6f;
            internal const float VineMinPullSpeed = 10f;
            internal const float VineMaxPullSpeed = 32f;
            internal const float CounterCooldown = 1f;
            internal const float CounterBuffDuration = 8f;
            internal const float CounterDamage = 1f;
            internal const float CounterHitstop = 0.25f;
            internal const float WaterDashCooldown = 4f;
            internal const float WaterDashMinSpeed = 50f;
            internal const float WaterDashMaxSpeed = 150f;
            internal const float CycloneCooldown = 3.5f;
            internal const float CycloneSpeed = 50f;
            internal const float CycloneLifetime = 2.2f;
            internal const float CycloneDamage = 2f;
            internal const float CycloneRadius = 4.5f;
            internal const float PoisonOrbCooldown = 4f;
            internal const float PoisonOrbSpeed = 75f;
            internal const float PoisonOrbLifetime = 1.75f;
            internal const float PoisonSeedLifetime = 8f;
            internal const float PoisonNailSpread = 5f;
            internal const float PoisonSeedFireRateBonus = 3.5f;
            internal const float PoisonDuration = 0.2f;
            internal const float PoisonPotency = 0.01f;
            internal const float PoisonMaxPotency = 0.75f;
            internal const float InfernoCost = 5f;
            internal const float InfernoWindup = 4f;
            internal const float InfernoExplosionDamage = 25f;
            internal const float InfernoExplosionScale = 3.2f;
            internal const float DragonCost = 5f;
            internal const float DragonMaxLifetime = 60f;
            internal const int DragonMaxChain = 6;
            internal const float DragonPickupDelay = 1.7f;
            internal const float DragonBaseRadius = 7f;
            internal const float DragonRadiusPerChain = 1.5f;
            internal const float DragonBaseDamage = 7f;
            internal const float DragonDamagePerChain = 2.5f;
            internal const float SporeCost = 2.6f;
            internal const float SporeFireLockout = 1f;
            internal const float SporeProjectileSpeed = 64f;
            internal const float SporeCloudLifetime = 8f;
            internal const float SporeCloudRadius = 8f;
            internal const float SporeIgniteBaseDamage = 5f;
            internal const float SporeIgniteMaxDamage = 7f;
            internal const float GeyserCooldown = 4f;
            internal const float GeyserPlayerSpeed = 40f;
            internal const float GeyserEnemySpeed = 30f;
            internal const float GeyserLifetime = 1.2f;
            internal const float GrassRocketCooldown = 2.8f;
            internal const float GrassRocketInterval = 0.15f;
            internal const float GrassRocketDamageMultiplier = 0.72f;

            // Custom Rapid-family heat sharing. Vanilla Rapid homes are never rewritten by EB;
            // only authored custom Rapid weapons/states assign the shared Overheat resource.
            internal const float GrassRapidHeatWhileFiring = 0.33f;
            internal const float GrassRapidHeatWithLiveSeed = 1f;
            internal const float WindRapidHeatWhileFiring = 0.33f;
            internal const float WindRapidHeatOnAltFire = 1f;
            internal const float WindSawHitAmount = 2.9f;
            internal const float EarthRapidHeatOnPrimaryShot = 0.33f;
            internal const float EarthRapidHeatOnAltFire = 1f;
            internal const float EarthSawHitAmount = 2.9f;

            internal const float PressureCooldown = 1f;
            internal const float PressureDamage = 1.5f;
            internal const float PressureRange = 8f;
            internal const float PressureRadius = 2.6f;
            internal const float PressureForce = 34f;
            internal const float UpdraftCooldown = 5f;
            internal const float UpdraftPlayerSpeed = 45f;
            internal const float UpdraftEnemySpeed = 40f;
            internal const float UpdraftDamage = 1.5f;
            internal const float UpdraftRadius = 7f;
            internal const float TempestPrimaryInterval = 0.25f;
            internal const float TempestCooldown = 6f;
            internal const float TempestWaveInterval = 0.15f;
            internal const float TempestSpreadAngle = 8f;
            internal const float GrandCycloneCost = 5f;
            internal const float GrandCycloneDuration = 4f;
            internal const float GrandCycloneRadius = 20f;
            internal const float GrandCyclonePullSpeed = 28f;
            internal const float GrandCycloneTick = 0.5f;
            internal const float GrandCycloneDamage = 0.1f;
            internal const float SlipstreamCooldown = 6.5f;
            internal const float SlipstreamDuration = 1.5f;
            internal const float SlipstreamSpeed = 52f;

            internal const float StormRevolverExplosionDamage = 3.75f;
            internal const float StormRevolverExplosionScale = 0.75f;
            internal const float StormRevolverSelfDamage = 25f;
            internal const float FlashstepCooldown = 5f;
            internal const float FlashstepDistance = 30f;
            internal const float FlashstepContactDamage = 1f;
            internal const int ThunderlineStrikeCount = 5;
            internal const float ThunderlineCooldown = 6f;
            internal const float ThunderlineStrikeInterval = 0.16f;
            internal const float ThunderlineFirstDistance = 10f;
            internal const float ThunderlineSpacing = 10f;
            internal const float ThunderlineGroundSearchDistance = 300f;
            internal const float ThunderlineStrikeHeight = 100f;
            internal const float ThunderlineDamage = 3f;
            internal const float ThunderlineExplosionScale = 0.75f;
            internal const float ThunderlineSelfDamage = 25f;

            internal const int FaultlineSpikeCount = 15;
            internal const float FaultlineCooldown = 5f;
            internal const float FaultlineSpikeInterval = 0.09f;
            internal const float FaultlineFirstDistance = 4f;
            internal const float FaultlineSpacing = 3.2f;
            internal const float FaultlineGroundSearchDistance = 300f;
            internal const float FaultlineDamage = 1f;
            internal const float FaultlineDamageTick = 0.22f;
            internal const float FaultlineLifetime = 1.5f;
            internal const float FaultlineRadius = 1.5f;
            internal const float FaultlineBaseHeight = 2.5f;
            internal const float FaultlineMaxHeight = 20f;
            internal const float FaultlineLaunchSpeed = 12f;
            internal const float FortifyDuration = 8f;
            internal const float FortifyHardDamageMultiplier = 0.60f;
            internal const float RockPrimaryInterval = 0.25f;
            internal const float RockBurstCooldown = 8f;
            internal const float RockBurstSpreadMultiplier = 0.6f;
            internal const int RockBurstSawCount = 7;
            internal const float MeteorCost = 3.75f;
            internal const int MeteorCount = 6;
            internal const float MeteorInterval = 0.45f;
            internal const float MeteorTravelDuration = 0.65f;
            internal const float MeteorAreaRadius = 4f;
            internal const float MeteorDamage = 2.75f;
            internal const float MeteorExplosionScale = 1f;
            internal const float MeteorSelfDamage = 35f;
        }

        private static FloatField fireColumnCooldown, fireColumnLifetime, fireColumnTick, fireColumnDamage, fireColumnRadius, fireColumnHeight, fireColumnFourCoinTotalDamage;
        private static FloatField vineCooldown, vinePullDuration, vineHoldDuration, vineStartRadius, vineEndRadius, vineMinPullSpeed, vineMaxPullSpeed;
        private static FloatField counterCooldown, counterBuffDuration, counterDamage, counterHitstop;
        private static FloatField waterDashCooldown, waterDashMinSpeed, waterDashMaxSpeed;
        private static FloatField cycloneCooldown, cycloneSpeed, cycloneLifetime, cycloneDamage, cycloneRadius;
        private static FloatField poisonOrbCooldown, poisonOrbSpeed, poisonOrbLifetime, poisonSeedLifetime, poisonNailSpread, poisonSeedFireRateBonus, poisonDuration, poisonPotency, poisonMaxPotency;
        private static FloatField infernoCost, infernoWindup, infernoExplosionDamage, infernoExplosionScale;
        private static FloatField dragonCost, dragonMaxLifetime, dragonPickupDelay, dragonBaseRadius, dragonRadiusPerChain, dragonBaseDamage, dragonDamagePerChain;
        private static FloatField sporeCost, sporeFireLockout, sporeProjectileSpeed, sporeCloudLifetime, sporeCloudRadius, sporeIgniteBaseDamage, sporeIgniteMaxDamage;
        private static FloatField geyserCooldown, geyserPlayerSpeed, geyserEnemySpeed, geyserLifetime;
        private static FloatField grassRocketCooldown, grassRocketInterval, grassRocketDamageMultiplier;
        private static FloatField grassRapidHeatWhileFiring, grassRapidHeatWithLiveSeed, windRapidHeatWhileFiring, windRapidHeatOnAltFire, windSawHitAmount, earthRapidHeatOnPrimaryShot, earthRapidHeatOnAltFire, earthSawHitAmount;

        private static FloatField pressureCooldown, pressureDamage, pressureRange, pressureRadius, pressureForce;
        private static FloatField updraftCooldown, updraftPlayerSpeed, updraftEnemySpeed, updraftDamage, updraftRadius;
        private static FloatField tempestCooldown, tempestWaveInterval, tempestSpreadAngle;
        private static FloatField grandCycloneCost, grandCycloneDuration, grandCycloneRadius, grandCyclonePullSpeed, grandCycloneTick, grandCycloneDamage;
        private static FloatField slipstreamCooldown, slipstreamDuration, slipstreamSpeed;
        private static FloatField stormRevolverExplosionDamage, stormRevolverExplosionScale, stormRevolverSelfDamage;
        private static FloatField flashstepCooldown, flashstepDistance, flashstepContactDamage;
        private static IntField thunderlineStrikeCount;
        private static FloatField thunderlineCooldown, thunderlineStrikeInterval, thunderlineFirstDistance, thunderlineSpacing, thunderlineGroundSearchDistance, thunderlineStrikeHeight, thunderlineDamage, thunderlineExplosionScale, thunderlineSelfDamage;
        private static IntField faultlineSpikeCount;
        private static FloatField faultlineCooldown, faultlineSpikeInterval, faultlineFirstDistance, faultlineSpacing, faultlineGroundSearchDistance, faultlineDamage, faultlineDamageTick, faultlineLifetime, faultlineRadius, faultlineBaseHeight, faultlineMaxHeight, faultlineLaunchSpeed;
        private static FloatField fortifyDuration, fortifyHardDamageMultiplier;
        private static FloatField rockPrimaryInterval, rockBurstCooldown, rockBurstSpreadMultiplier;
        private static FloatField meteorCost, meteorInterval, meteorTravelDuration, meteorAreaRadius, meteorDamage, meteorExplosionScale, meteorSelfDamage;

        private static readonly List<Action> resetActions = new List<Action>();

        internal static float FireColumnCooldown => V(fireColumnCooldown, Defaults.FireColumnCooldown);
        internal static float FireColumnLifetime => V(fireColumnLifetime, Defaults.FireColumnLifetime);
        internal static float FireColumnTick => V(fireColumnTick, Defaults.FireColumnTick);
        internal static float FireColumnDamage => V(fireColumnDamage, Defaults.FireColumnDamage);
        internal static float FireColumnRadius => V(fireColumnRadius, Defaults.FireColumnRadius);
        internal static float FireColumnHeight => V(fireColumnHeight, Defaults.FireColumnHeight);
        internal static float FireColumnFourCoinTotalDamage => V(fireColumnFourCoinTotalDamage, Defaults.FireColumnFourCoinTotalDamage);

        internal static float FireColumnCoinMultiplier(int coins)
        {
            coins = Mathf.Clamp(coins, 1, 4);
            float interval = Mathf.Max(0.001f, FireColumnTick);
            int expectedTicks = Mathf.Max(1, Mathf.CeilToInt(FireColumnLifetime / interval));
            float unbonusedTotal = Mathf.Max(0.0001f, FireColumnDamage * expectedTicks);
            float fourCoinMultiplier = FireColumnFourCoinTotalDamage / unbonusedTotal;

            // Keep the deliberately modest first-coin reward, then scale linearly so a full
            // four-coin chain lands on the configured TOTAL column damage rather than merely
            // receiving the same small bonus several times.
            return Mathf.Lerp(1.20f, fourCoinMultiplier, (coins - 1) / 3f);
        }

        internal static float VineCooldown => V(vineCooldown, Defaults.VineCooldown);
        internal static float VinePullDuration => V(vinePullDuration, Defaults.VinePullDuration);
        internal static float VineHoldDuration => V(vineHoldDuration, Defaults.VineHoldDuration);
        internal static float VineStartRadius => V(vineStartRadius, Defaults.VineStartRadius);
        internal static float VineEndRadius => V(vineEndRadius, Defaults.VineEndRadius);
        internal static float VineMinPullSpeed => V(vineMinPullSpeed, Defaults.VineMinPullSpeed);
        internal static float VineMaxPullSpeed => V(vineMaxPullSpeed, Defaults.VineMaxPullSpeed);

        internal static float CounterCooldown => V(counterCooldown, Defaults.CounterCooldown);
        internal static float CounterBuffDuration => V(counterBuffDuration, Defaults.CounterBuffDuration);
        internal static float CounterDamage => V(counterDamage, Defaults.CounterDamage);
        internal static float CounterHitstop => V(counterHitstop, Defaults.CounterHitstop);

        internal static float WaterDashCooldown => V(waterDashCooldown, Defaults.WaterDashCooldown);
        internal static float WaterDashMinSpeed => V(waterDashMinSpeed, Defaults.WaterDashMinSpeed);
        internal static float WaterDashMaxSpeed => V(waterDashMaxSpeed, Defaults.WaterDashMaxSpeed);

        internal static float CycloneCooldown => V(cycloneCooldown, Defaults.CycloneCooldown);
        internal static float CycloneSpeed => V(cycloneSpeed, Defaults.CycloneSpeed);
        internal static float CycloneLifetime => V(cycloneLifetime, Defaults.CycloneLifetime);
        internal static float CycloneDamage => V(cycloneDamage, Defaults.CycloneDamage);
        internal static float CycloneRadius => V(cycloneRadius, Defaults.CycloneRadius);

        internal static float PoisonOrbCooldown => V(poisonOrbCooldown, Defaults.PoisonOrbCooldown);
        internal static float PoisonOrbSpeed => V(poisonOrbSpeed, Defaults.PoisonOrbSpeed);
        internal static float PoisonOrbLifetime => V(poisonOrbLifetime, Defaults.PoisonOrbLifetime);
        internal static float PoisonSeedLifetime => V(poisonSeedLifetime, Defaults.PoisonSeedLifetime);
        internal static float PoisonNailSpread => V(poisonNailSpread, Defaults.PoisonNailSpread);
        internal static float PoisonSeedFireRateBonus => V(poisonSeedFireRateBonus, Defaults.PoisonSeedFireRateBonus);
        internal static float PoisonDuration => V(poisonDuration, Defaults.PoisonDuration);
        internal static float PoisonPotency => V(poisonPotency, Defaults.PoisonPotency);
        internal static float PoisonMaxPotency => V(poisonMaxPotency, Defaults.PoisonMaxPotency);

        internal static float InfernoCost => V(infernoCost, Defaults.InfernoCost);
        internal static float InfernoWindup => V(infernoWindup, Defaults.InfernoWindup);
        internal static float InfernoExplosionDamage => V(infernoExplosionDamage, Defaults.InfernoExplosionDamage);
        internal static float InfernoExplosionScale => V(infernoExplosionScale, Defaults.InfernoExplosionScale);

        internal static float DragonCost => V(dragonCost, Defaults.DragonCost);
        internal static float DragonMaxLifetime => V(dragonMaxLifetime, Defaults.DragonMaxLifetime);
        internal static float DragonPickupDelay => V(dragonPickupDelay, Defaults.DragonPickupDelay);
        internal static float DragonBaseRadius => V(dragonBaseRadius, Defaults.DragonBaseRadius);
        internal static float DragonRadiusPerChain => V(dragonRadiusPerChain, Defaults.DragonRadiusPerChain);
        internal static float DragonBaseDamage => V(dragonBaseDamage, Defaults.DragonBaseDamage);
        internal static float DragonDamagePerChain => V(dragonDamagePerChain, Defaults.DragonDamagePerChain);

        internal static float SporeCost => V(sporeCost, Defaults.SporeCost);
        internal static float SporeFireLockout => V(sporeFireLockout, Defaults.SporeFireLockout);
        internal static float SporeProjectileSpeed => V(sporeProjectileSpeed, Defaults.SporeProjectileSpeed);
        internal static float SporeCloudLifetime => V(sporeCloudLifetime, Defaults.SporeCloudLifetime);
        internal static float SporeCloudRadius => V(sporeCloudRadius, Defaults.SporeCloudRadius);
        internal static float SporeIgniteBaseDamage => V(sporeIgniteBaseDamage, Defaults.SporeIgniteBaseDamage);
        internal static float SporeIgniteMaxDamage => V(sporeIgniteMaxDamage, Defaults.SporeIgniteMaxDamage);

        internal static float GeyserCooldown => V(geyserCooldown, Defaults.GeyserCooldown);
        internal static float GeyserPlayerSpeed => V(geyserPlayerSpeed, Defaults.GeyserPlayerSpeed);
        internal static float GeyserEnemySpeed => V(geyserEnemySpeed, Defaults.GeyserEnemySpeed);
        internal static float GeyserLifetime => V(geyserLifetime, Defaults.GeyserLifetime);

        internal static float GrassRocketCooldown => V(grassRocketCooldown, Defaults.GrassRocketCooldown);
        internal static float GrassRocketInterval => V(grassRocketInterval, Defaults.GrassRocketInterval);
        internal static float GrassRocketDamageMultiplier => V(grassRocketDamageMultiplier, Defaults.GrassRocketDamageMultiplier);

        internal static float GrassRapidHeatWhileFiring => V(grassRapidHeatWhileFiring, Defaults.GrassRapidHeatWhileFiring);
        internal static float GrassRapidHeatWithLiveSeed => V(grassRapidHeatWithLiveSeed, Defaults.GrassRapidHeatWithLiveSeed);
        internal static float WindRapidHeatWhileFiring => V(windRapidHeatWhileFiring, Defaults.WindRapidHeatWhileFiring);
        internal static float WindRapidHeatOnAltFire => V(windRapidHeatOnAltFire, Defaults.WindRapidHeatOnAltFire);
        internal static float WindSawHitAmount => V(windSawHitAmount, Defaults.WindSawHitAmount);
        internal static float EarthRapidHeatOnPrimaryShot => V(earthRapidHeatOnPrimaryShot, Defaults.EarthRapidHeatOnPrimaryShot);
        internal static float EarthRapidHeatOnAltFire => V(earthRapidHeatOnAltFire, Defaults.EarthRapidHeatOnAltFire);
        internal static float EarthSawHitAmount => V(earthSawHitAmount, Defaults.EarthSawHitAmount);

        internal static float PressureCooldown => V(pressureCooldown, Defaults.PressureCooldown);
        internal static float PressureDamage => V(pressureDamage, Defaults.PressureDamage);
        internal static float PressureRange => V(pressureRange, Defaults.PressureRange);
        internal static float PressureRadius => V(pressureRadius, Defaults.PressureRadius);
        internal static float PressureForce => V(pressureForce, Defaults.PressureForce);


        internal static float UpdraftCooldown => V(updraftCooldown, Defaults.UpdraftCooldown);
        internal static float UpdraftPlayerSpeed => V(updraftPlayerSpeed, Defaults.UpdraftPlayerSpeed);
        internal static float UpdraftEnemySpeed => V(updraftEnemySpeed, Defaults.UpdraftEnemySpeed);
        internal static float UpdraftDamage => V(updraftDamage, Defaults.UpdraftDamage);
        internal static float UpdraftRadius => V(updraftRadius, Defaults.UpdraftRadius);

        internal static float TempestCooldown => V(tempestCooldown, Defaults.TempestCooldown);
        internal static float TempestWaveInterval => V(tempestWaveInterval, Defaults.TempestWaveInterval);
        internal static float TempestSpreadAngle => V(tempestSpreadAngle, Defaults.TempestSpreadAngle);

        internal static float GrandCycloneCost => V(grandCycloneCost, Defaults.GrandCycloneCost);
        internal static float GrandCycloneDuration => V(grandCycloneDuration, Defaults.GrandCycloneDuration);
        internal static float GrandCycloneRadius => V(grandCycloneRadius, Defaults.GrandCycloneRadius);
        internal static float GrandCyclonePullSpeed => V(grandCyclonePullSpeed, Defaults.GrandCyclonePullSpeed);
        internal static float GrandCycloneTick => V(grandCycloneTick, Defaults.GrandCycloneTick);
        internal static float GrandCycloneDamage => V(grandCycloneDamage, Defaults.GrandCycloneDamage);

        internal static float SlipstreamCooldown => V(slipstreamCooldown, Defaults.SlipstreamCooldown);
        internal static float SlipstreamDuration => V(slipstreamDuration, Defaults.SlipstreamDuration);
        internal static float SlipstreamSpeed => V(slipstreamSpeed, Defaults.SlipstreamSpeed);

        internal static float StormRevolverExplosionDamage => V(stormRevolverExplosionDamage, Defaults.StormRevolverExplosionDamage);
        internal static float StormRevolverExplosionScale => V(stormRevolverExplosionScale, Defaults.StormRevolverExplosionScale);
        internal static float StormRevolverSelfDamage => V(stormRevolverSelfDamage, Defaults.StormRevolverSelfDamage);
        internal static float FlashstepCooldown => V(flashstepCooldown, Defaults.FlashstepCooldown);
        internal static float FlashstepDistance => V(flashstepDistance, Defaults.FlashstepDistance);
        internal static float FlashstepContactDamage => V(flashstepContactDamage, Defaults.FlashstepContactDamage);
        internal static int ThunderlineStrikeCount => V(thunderlineStrikeCount, Defaults.ThunderlineStrikeCount);
        internal static float ThunderlineCooldown => V(thunderlineCooldown, Defaults.ThunderlineCooldown);
        internal static float ThunderlineStrikeInterval => V(thunderlineStrikeInterval, Defaults.ThunderlineStrikeInterval);
        internal static float ThunderlineFirstDistance => V(thunderlineFirstDistance, Defaults.ThunderlineFirstDistance);
        internal static float ThunderlineSpacing => V(thunderlineSpacing, Defaults.ThunderlineSpacing);
        internal static float ThunderlineGroundSearchDistance => V(thunderlineGroundSearchDistance, Defaults.ThunderlineGroundSearchDistance);
        internal static float ThunderlineStrikeHeight => V(thunderlineStrikeHeight, Defaults.ThunderlineStrikeHeight);
        internal static float ThunderlineDamage => V(thunderlineDamage, Defaults.ThunderlineDamage);
        internal static float ThunderlineExplosionScale => V(thunderlineExplosionScale, Defaults.ThunderlineExplosionScale);
        internal static float ThunderlineSelfDamage => V(thunderlineSelfDamage, Defaults.ThunderlineSelfDamage);

        internal static int FaultlineSpikeCount => V(faultlineSpikeCount, Defaults.FaultlineSpikeCount);
        internal static float FaultlineCooldown => V(faultlineCooldown, Defaults.FaultlineCooldown);
        internal static float FaultlineSpikeInterval => V(faultlineSpikeInterval, Defaults.FaultlineSpikeInterval);
        internal static float FaultlineFirstDistance => V(faultlineFirstDistance, Defaults.FaultlineFirstDistance);
        internal static float FaultlineSpacing => V(faultlineSpacing, Defaults.FaultlineSpacing);
        internal static float FaultlineGroundSearchDistance => V(faultlineGroundSearchDistance, Defaults.FaultlineGroundSearchDistance);
        internal static float FaultlineDamage => V(faultlineDamage, Defaults.FaultlineDamage);
        internal static float FaultlineDamageTick => V(faultlineDamageTick, Defaults.FaultlineDamageTick);
        internal static float FaultlineLifetime => V(faultlineLifetime, Defaults.FaultlineLifetime);
        internal static float FaultlineRadius => V(faultlineRadius, Defaults.FaultlineRadius);
        internal static float FaultlineBaseHeight => V(faultlineBaseHeight, Defaults.FaultlineBaseHeight);
        internal static float FaultlineMaxHeight => V(faultlineMaxHeight, Defaults.FaultlineMaxHeight);
        internal static float FaultlineLaunchSpeed => V(faultlineLaunchSpeed, Defaults.FaultlineLaunchSpeed);
        internal static float FortifyDuration => V(fortifyDuration, Defaults.FortifyDuration);
        internal static float FortifyHardDamageMultiplier => V(fortifyHardDamageMultiplier, Defaults.FortifyHardDamageMultiplier);
        internal static float RockPrimaryInterval => V(rockPrimaryInterval, Defaults.RockPrimaryInterval);
        internal static float RockBurstCooldown => V(rockBurstCooldown, Defaults.RockBurstCooldown);
        internal static float RockBurstSpreadMultiplier => V(rockBurstSpreadMultiplier, Defaults.RockBurstSpreadMultiplier);
        internal static float MeteorCost => V(meteorCost, Defaults.MeteorCost);
        internal static float MeteorInterval => V(meteorInterval, Defaults.MeteorInterval);
        internal static float MeteorTravelDuration => V(meteorTravelDuration, Defaults.MeteorTravelDuration);
        internal static float MeteorAreaRadius => V(meteorAreaRadius, Defaults.MeteorAreaRadius);
        internal static float MeteorDamage => V(meteorDamage, Defaults.MeteorDamage);
        internal static float MeteorExplosionScale => V(meteorExplosionScale, Defaults.MeteorExplosionScale);
        internal static float MeteorSelfDamage => V(meteorSelfDamage, Defaults.MeteorSelfDamage);

        internal static float UltimateCost(ElementId element)
        {
            switch (element)
            {
                case ElementId.Fire: return InfernoCost;
                case ElementId.Water: return DragonCost;
                case ElementId.Grass: return SporeCost;
                case ElementId.Wind: return GrandCycloneCost;
                case ElementId.Storm: return Defaults.UltimateChargeMax; // Vanilla Electric Railcannon owns its charge behavior.
                case ElementId.Earth: return MeteorCost;
                default: return Defaults.UltimateChargeMax;
            }
        }

        private static float V(FloatField field, float fallback) => field == null ? fallback : field.value;
        private static int V(IntField field, int fallback) => field == null ? fallback : field.value;
        private static string N(float value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        internal static void Initialize(PluginConfigurator config)
        {
            resetActions.Clear();
            ConfigPanel root = new ConfigPanel(config.rootPanel, "Edit weapon values", "weaponValues");
            new ConfigHeader(root, "Prototype balance values for EB-authored mechanics only. Vanilla weapon homes remain mechanically owned by ULTRAKILL. Changes are read live by the custom weapons; most do not require a level restart.");
            ButtonField resetAll = new ButtonField(root, "Reset ALL weapon values to defaults", "resetAllWeaponValues");
            resetAll.onClick += ResetAllToDefaults;

            ConfigPanel fire = new ConfigPanel(root, "Fire", "weaponValuesFire");
            ConfigPanel water = new ConfigPanel(root, "Water", "weaponValuesWater");
            ConfigPanel grass = new ConfigPanel(root, "Grass", "weaponValuesGrass");
            ConfigPanel wind = new ConfigPanel(root, "Wind", "weaponValuesWind");
            ConfigPanel storm = new ConfigPanel(root, "Storm", "weaponValuesStorm");
            ConfigPanel earth = new ConfigPanel(root, "Earth", "weaponValuesEarth");

            InitializeFire(fire);
            InitializeWater(water);
            InitializeGrass(grass);
            InitializeWind(wind);
            InitializeStorm(storm);
            InitializeEarth(earth);

            ApplyBalanceDefaultMigration();
        }

        private static void InitializeFire(ConfigPanel fire)
        {
            ConfigPanel p = new ConfigPanel(fire, "Revolver — Fire Column", "valuesFireRevolver");
            fireColumnCooldown = F(p, "Alt cooldown (seconds; Fire slab minimum is 2s)", "fireColumnCooldown", Defaults.FireColumnCooldown, 0f, 20f);
            fireColumnLifetime = F(p, "Column lifetime", "fireColumnLifetime", Defaults.FireColumnLifetime, 0.1f, 10f);
            fireColumnTick = F(p, "Damage tick interval", "fireColumnTick", Defaults.FireColumnTick, 0.02f, 2f);
            fireColumnDamage = F(p, "Damage per tick", "fireColumnDamage", Defaults.FireColumnDamage, 0f, 50f);
            fireColumnRadius = F(p, "Column radius", "fireColumnRadius", Defaults.FireColumnRadius, 0.5f, 30f);
            fireColumnHeight = F(p, "Column height", "fireColumnHeight", Defaults.FireColumnHeight, 1f, 50f);
            fireColumnFourCoinTotalDamage = F(p, "4-coin total column damage", "fireColumnFourCoinTotalDamage", Defaults.FireColumnFourCoinTotalDamage, 0f, 200f);

            p = new ConfigPanel(fire, "Shotgun — Counter", "valuesCounter");
            counterCooldown = F(p, "Counter cooldown", "counterCooldown", Defaults.CounterCooldown, 0f, 10f);
            counterBuffDuration = F(p, "Combustion buff duration", "counterBuffDuration", Defaults.CounterBuffDuration, 0f, 60f);
            counterDamage = F(p, "Counter hit damage", "counterDamage", Defaults.CounterDamage, 0f, 100f);
            counterHitstop = F(p, "Counter impact hitstop", "counterHitstop", Defaults.CounterHitstop, 0f, 1f);

            p = new ConfigPanel(fire, "Ultimate — Inferno", "valuesInferno");
            infernoCost = F(p, "Ultimate charge cost (full bar = " + N(Defaults.UltimateChargeMax) + ")", "infernoCost", Defaults.InfernoCost, 0.05f, Defaults.UltimateChargeMax);
            infernoWindup = F(p, "Windup duration", "infernoWindup", Defaults.InfernoWindup, 0.1f, 10f);
            infernoExplosionDamage = F(p, "Explosion damage", "infernoExplosionDamage", Defaults.InfernoExplosionDamage, 0f, 500f);
            infernoExplosionScale = F(p, "Explosion size multiplier", "infernoExplosionScale", Defaults.InfernoExplosionScale, 0.1f, 10f);

        }

        private static void InitializeWater(ConfigPanel water)
        {
            ConfigPanel p = new ConfigPanel(water, "Jackhammer — Dash", "valuesWaterDash");
            waterDashCooldown = F(p, "Dash cooldown", "waterDashCooldown", Defaults.WaterDashCooldown, 0f, 20f);
            waterDashMinSpeed = F(p, "Minimum dash speed", "waterDashMinSpeed", Defaults.WaterDashMinSpeed, 0f, 100f);
            waterDashMaxSpeed = F(p, "Maximum redirected speed", "waterDashMaxSpeed", Defaults.WaterDashMaxSpeed, 1f, 150f);

            p = new ConfigPanel(water, "Ultimate — Dragon", "valuesDragon");
            dragonCost = F(p, "Ultimate charge cost (full bar = " + N(Defaults.UltimateChargeMax) + ")", "dragonCost", Defaults.DragonCost, 0.05f, Defaults.UltimateChargeMax);
            dragonMaxLifetime = F(p, "Maximum lifetime", "dragonMaxLifetime", Defaults.DragonMaxLifetime, 1f, 60f);
            dragonPickupDelay = F(p, "Time holding target before slam", "dragonPickupDelay", Defaults.DragonPickupDelay, 0f, 10f);
            dragonBaseRadius = F(p, "Base slam radius", "dragonBaseRadius", Defaults.DragonBaseRadius, 1f, 40f);
            dragonRadiusPerChain = F(p, "Radius per stolen kill", "dragonRadiusPerChain", Defaults.DragonRadiusPerChain, 0f, 10f);
            dragonBaseDamage = F(p, "Base slam damage", "dragonBaseDamage", Defaults.DragonBaseDamage, 0f, 100f);
            dragonDamagePerChain = F(p, "Damage per stolen kill", "dragonDamagePerChain", Defaults.DragonDamagePerChain, 0f, 50f);

            p = new ConfigPanel(water, "Rocket Launcher — Geyser", "valuesGeyser");
            geyserCooldown = F(p, "Geyser cooldown", "geyserCooldown", Defaults.GeyserCooldown, 0f, 30f);
            geyserPlayerSpeed = F(p, "Player launch speed", "geyserPlayerSpeed", Defaults.GeyserPlayerSpeed, 0f, 100f);
            geyserEnemySpeed = F(p, "Enemy launch speed", "geyserEnemySpeed", Defaults.GeyserEnemySpeed, 0f, 100f);
            geyserLifetime = F(p, "Geyser lifetime", "geyserLifetime", Defaults.GeyserLifetime, 0.1f, 10f);

        }

        private static void InitializeGrass(ConfigPanel grass)
        {
            ConfigPanel p = new ConfigPanel(grass, "Revolver — Vine", "valuesGrassRevolver");
            vineCooldown = F(p, "Alt cooldown", "vineCooldown", Defaults.VineCooldown, 0f, 20f);
            vinePullDuration = F(p, "Ring close / pull duration", "vinePullDuration", Defaults.VinePullDuration, 0.1f, 10f);
            vineHoldDuration = F(p, "Hold duration", "vineHoldDuration", Defaults.VineHoldDuration, 0f, 10f);
            vineStartRadius = F(p, "Starting radius", "vineStartRadius", Defaults.VineStartRadius, 1f, 50f);
            vineEndRadius = F(p, "Ending radius", "vineEndRadius", Defaults.VineEndRadius, 0.25f, 20f);
            vineMinPullSpeed = F(p, "Minimum pull speed", "vineMinPullSpeed", Defaults.VineMinPullSpeed, 0f, 100f);
            vineMaxPullSpeed = F(p, "Maximum pull speed", "vineMaxPullSpeed", Defaults.VineMaxPullSpeed, 0f, 150f);

            p = new ConfigPanel(grass, "Shotgun — Cyclone", "valuesCyclone");
            cycloneCooldown = F(p, "Cyclone cooldown", "cycloneCooldown", Defaults.CycloneCooldown, 0f, 20f);
            cycloneSpeed = F(p, "Cyclone travel speed", "cycloneSpeed", Defaults.CycloneSpeed, 0f, 150f);
            cycloneLifetime = F(p, "Cyclone lifetime", "cycloneLifetime", Defaults.CycloneLifetime, 0.1f, 20f);
            cycloneDamage = F(p, "Cyclone contact damage", "cycloneDamage", Defaults.CycloneDamage, 0f, 50f);
            cycloneRadius = F(p, "Cyclone radius", "cycloneRadius", Defaults.CycloneRadius, 0.5f, 30f);

            p = new ConfigPanel(grass, "Nailgun — Poison", "valuesPoison");
            grassRapidHeatWhileFiring = F(p, "Shared Overheat heat set by each primary nail, no live Seed (0-1)", "grassRapidHeatWhileFiring", Defaults.GrassRapidHeatWhileFiring, 0f, 1f);
            grassRapidHeatWithLiveSeed = F(p, "Shared Overheat heat set by each primary nail while any Seed is live (0-1)", "grassRapidHeatWithLiveSeed", Defaults.GrassRapidHeatWithLiveSeed, 0f, 1f);
            poisonOrbCooldown = F(p, "Poison orb cooldown", "poisonOrbCooldown", Defaults.PoisonOrbCooldown, 0f, 30f);
            poisonOrbSpeed = F(p, "Poison orb speed", "poisonOrbSpeed", Defaults.PoisonOrbSpeed, 1f, 250f);
            poisonOrbLifetime = F(p, "Poison orb flight lifetime", "poisonOrbLifetime", Defaults.PoisonOrbLifetime, 0.1f, 10f);
            poisonSeedLifetime = F(p, "Attached orb lifetime", "poisonSeedLifetime", Defaults.PoisonSeedLifetime, 0.1f, 60f);
            poisonNailSpread = F(p, "Primary nail spread", "poisonNailSpread", Defaults.PoisonNailSpread, 0f, 45f);
            poisonSeedFireRateBonus = F(p, "Normal fire-rate slowdown vs seeded", "poisonSeedFireRateBonus", Defaults.PoisonSeedFireRateBonus, 0f, 20f);
            poisonDuration = F(p, "Poison duration added per nail", "poisonDuration", Defaults.PoisonDuration, 0f, 30f);
            poisonPotency = F(p, "Poison potency added per nail", "poisonPotency", Defaults.PoisonPotency, 0f, 2f);
            poisonMaxPotency = F(p, "Maximum poison potency per tick", "poisonMaxPotency", Defaults.PoisonMaxPotency, 0.25f, 2f);

            p = new ConfigPanel(grass, "Ultimate — Spore Bombardment", "valuesSpore");
            sporeCost = F(p, "Rail charge cost (full bar = " + N(Defaults.UltimateChargeMax) + ")", "sporeCost", Defaults.SporeCost, 0f, Defaults.UltimateChargeMax);
            sporeFireLockout = F(p, "Fire lockout", "sporeFireLockout", Defaults.SporeFireLockout, 0f, 10f);
            sporeProjectileSpeed = F(p, "Projectile speed", "sporeProjectileSpeed", Defaults.SporeProjectileSpeed, 1f, 150f);
            sporeCloudLifetime = F(p, "Cloud lifetime", "sporeCloudLifetime", Defaults.SporeCloudLifetime, 0.5f, 60f);
            sporeCloudRadius = F(p, "Cloud poison radius", "sporeCloudRadius", Defaults.SporeCloudRadius, 0.5f, 30f);
            sporeIgniteBaseDamage = F(p, "Ignition damage at near-expiry", "sporeIgniteBaseDamage", Defaults.SporeIgniteBaseDamage, 0f, 100f);
            sporeIgniteMaxDamage = F(p, "Ignition damage when fresh", "sporeIgniteMaxDamage", Defaults.SporeIgniteMaxDamage, 0f, 100f);

            p = new ConfigPanel(grass, "Rocket Launcher — Burst", "valuesGrassRocket");
            grassRocketCooldown = F(p, "Burst cooldown", "grassRocketCooldown", Defaults.GrassRocketCooldown, 0f, 30f);
            grassRocketInterval = F(p, "Time between rockets", "grassRocketInterval", Defaults.GrassRocketInterval, 0.01f, 2f);
            grassRocketDamageMultiplier = F(p, "Rocket damage multiplier", "grassRocketDamageMultiplier", Defaults.GrassRocketDamageMultiplier, 0f, 5f);

        }

        private static void InitializeWind(ConfigPanel wind)
        {
            ConfigPanel p = new ConfigPanel(wind, "Revolver — Pressure", "valuesWindPressure");
            pressureCooldown = F(p, "Airblast cooldown", "pressureCooldown", Defaults.PressureCooldown, 0f, 10f);
            pressureDamage = F(p, "Airblast damage", "pressureDamage", Defaults.PressureDamage, 0f, 50f);
            pressureRange = F(p, "Airblast range", "pressureRange", Defaults.PressureRange, 1f, 30f);
            pressureRadius = F(p, "Airblast width", "pressureRadius", Defaults.PressureRadius, 0.5f, 10f);
            pressureForce = F(p, "Normal enemy push speed", "pressureForce", Defaults.PressureForce, 0f, 100f);

            p = new ConfigPanel(wind, "Shotgun — Updraft", "valuesWindUpdraft");
            updraftCooldown = F(p, "Updraft cooldown", "updraftCooldown", Defaults.UpdraftCooldown, 0f, 30f);
            updraftPlayerSpeed = F(p, "Player upward speed", "updraftPlayerSpeed", Defaults.UpdraftPlayerSpeed, 0f, 120f);
            updraftEnemySpeed = F(p, "Enemy launch speed", "updraftEnemySpeed", Defaults.UpdraftEnemySpeed, 0f, 100f);
            updraftDamage = F(p, "Enemy damage", "updraftDamage", Defaults.UpdraftDamage, 0f, 50f);
            updraftRadius = F(p, "Effect radius", "updraftRadius", Defaults.UpdraftRadius, 0.5f, 30f);

            p = new ConfigPanel(wind, "Sawblade Launcher — Tempest", "valuesWindTempest");
            windRapidHeatWhileFiring = F(p, "Shared Overheat heat set by each primary saw (0-1)", "windRapidHeatWhileFiring", Defaults.WindRapidHeatWhileFiring, 0f, 1f);
            windRapidHeatOnAltFire = F(p, "Shared Overheat heat set by each Tempest Alt-Fire wave (0-1)", "windRapidHeatOnAltFire", Defaults.WindRapidHeatOnAltFire, 0f, 1f);
            windSawHitAmount = F(p, "Saw hitAmount / durability budget", "windSawHitAmount", Defaults.WindSawHitAmount, 1f, 20f);
            tempestCooldown = F(p, "Tempest cooldown", "tempestCooldown", Defaults.TempestCooldown, 0f, 30f);
            tempestWaveInterval = F(p, "Time between waves", "tempestWaveInterval", Defaults.TempestWaveInterval, 0.01f, 2f);
            tempestSpreadAngle = F(p, "Wave spread angle", "tempestSpreadAngle", Defaults.TempestSpreadAngle, 0f, 45f);

            p = new ConfigPanel(wind, "Ultimate — Grand Cyclone", "valuesWindUltimate");
            grandCycloneCost = F(p, "Ultimate charge cost (full bar = " + N(Defaults.UltimateChargeMax) + ")", "grandCycloneCost", Defaults.GrandCycloneCost, 0.05f, Defaults.UltimateChargeMax);
            grandCycloneDuration = F(p, "Cyclone duration", "grandCycloneDuration", Defaults.GrandCycloneDuration, 0.5f, 30f);
            grandCycloneRadius = F(p, "Pull radius", "grandCycloneRadius", Defaults.GrandCycloneRadius, 1f, 50f);
            grandCyclonePullSpeed = F(p, "Pull speed", "grandCyclonePullSpeed", Defaults.GrandCyclonePullSpeed, 0f, 100f);
            grandCycloneTick = F(p, "Damage tick interval", "grandCycloneTick", Defaults.GrandCycloneTick, 0.05f, 5f);
            grandCycloneDamage = F(p, "Damage per tick", "grandCycloneDamage", Defaults.GrandCycloneDamage, 0f, 20f);

            p = new ConfigPanel(wind, "Grenade Launcher — Slipstream", "valuesWindSlipstream");
            slipstreamCooldown = F(p, "Slipstream cooldown", "slipstreamCooldown", Defaults.SlipstreamCooldown, 0f, 30f);
            slipstreamDuration = F(p, "Flight duration", "slipstreamDuration", Defaults.SlipstreamDuration, 0.1f, 10f);
            slipstreamSpeed = F(p, "Flight speed", "slipstreamSpeed", Defaults.SlipstreamSpeed, 1f, 150f);

        }

        private static void InitializeStorm(ConfigPanel storm)
        {
            ConfigPanel p = new ConfigPanel(storm, "Revolver — Charged Storm Blast", "valuesStormRevolver");
            stormRevolverExplosionDamage = F(p, "Charged-shot explosion damage", "stormRevolverExplosionDamage", Defaults.StormRevolverExplosionDamage, 0f, 50f);
            stormRevolverExplosionScale = F(p, "Charged-shot explosion size", "stormRevolverExplosionScale", Defaults.StormRevolverExplosionScale, 0.1f, 5f);
            stormRevolverSelfDamage = F(p, "Charged-shot self-damage", "stormRevolverSelfDamage", Defaults.StormRevolverSelfDamage, 0f, 100f);

            p = new ConfigPanel(storm, "Jackhammer — Flashstep", "valuesStormFlashstep");
            flashstepCooldown = F(p, "Flashstep cooldown", "flashstepCooldown", Defaults.FlashstepCooldown, 0f, 30f);
            flashstepDistance = F(p, "Maximum teleport distance", "flashstepDistance", Defaults.FlashstepDistance, 1f, 80f);
            flashstepContactDamage = F(p, "Damage to enemies passed through", "flashstepContactDamage", Defaults.FlashstepContactDamage, 0f, 50f);

            p = new ConfigPanel(storm, "Grenade Launcher — Thunderline", "valuesStormThunderline");
            thunderlineStrikeCount = I(p, "Number of lightning strikes", "thunderlineStrikeCount", Defaults.ThunderlineStrikeCount, 1, 32);
            thunderlineCooldown = F(p, "Thunderline cooldown", "thunderlineCooldown", Defaults.ThunderlineCooldown, 0f, 30f);
            thunderlineStrikeInterval = F(p, "Time between lightning strikes", "thunderlineStrikeInterval", Defaults.ThunderlineStrikeInterval, 0.02f, 2f);
            thunderlineFirstDistance = F(p, "Distance to first strike", "thunderlineFirstDistance", Defaults.ThunderlineFirstDistance, 0f, 40f);
            thunderlineSpacing = F(p, "Distance between strikes", "thunderlineSpacing", Defaults.ThunderlineSpacing, 1f, 20f);
            thunderlineGroundSearchDistance = F(p, "Maximum ground search depth", "thunderlineGroundSearchDistance", Defaults.ThunderlineGroundSearchDistance, 20f, 600f);
            thunderlineStrikeHeight = F(p, "Lightning start height", "thunderlineStrikeHeight", Defaults.ThunderlineStrikeHeight, 10f, 250f);
            thunderlineDamage = F(p, "Explosion damage per strike", "thunderlineDamage", Defaults.ThunderlineDamage, 0f, 50f);
            thunderlineExplosionScale = F(p, "Explosion size", "thunderlineExplosionScale", Defaults.ThunderlineExplosionScale, 0.1f, 5f);
            thunderlineSelfDamage = F(p, "Self-damage per strike", "thunderlineSelfDamage", Defaults.ThunderlineSelfDamage, 0f, 100f);

        }

        private static void InitializeEarth(ConfigPanel earth)
        {
            ConfigPanel p = new ConfigPanel(earth, "Revolver — Faultline", "valuesEarthFaultline");
            faultlineSpikeCount = I(p, "Number of spikes", "faultlineSpikeCount", Defaults.FaultlineSpikeCount, 1, 32);
            faultlineCooldown = F(p, "Faultline cooldown", "faultlineCooldown", Defaults.FaultlineCooldown, 0f, 30f);
            faultlineSpikeInterval = F(p, "Time between spikes", "faultlineSpikeInterval", Defaults.FaultlineSpikeInterval, 0.01f, 1f);
            faultlineFirstDistance = F(p, "Distance to first spike", "faultlineFirstDistance", Defaults.FaultlineFirstDistance, 0f, 20f);
            faultlineSpacing = F(p, "Distance between spikes", "faultlineSpacing", Defaults.FaultlineSpacing, 0.5f, 12f);
            faultlineGroundSearchDistance = F(p, "Maximum ground search depth", "faultlineGroundSearchDistance", Defaults.FaultlineGroundSearchDistance, 20f, 600f);
            faultlineDamage = F(p, "Damage per tick", "faultlineDamage", Defaults.FaultlineDamage, 0f, 50f);
            faultlineDamageTick = F(p, "Damage tick interval", "faultlineDamageTick", Defaults.FaultlineDamageTick, 0.03f, 2f);
            faultlineLifetime = F(p, "Spike lifetime", "faultlineLifetime", Defaults.FaultlineLifetime, 0.2f, 8f);
            faultlineRadius = F(p, "Spike damage radius", "faultlineRadius", Defaults.FaultlineRadius, 0.25f, 6f);
            faultlineBaseHeight = F(p, "First spike height", "faultlineBaseHeight", Defaults.FaultlineBaseHeight, 0.5f, 15f);
            faultlineMaxHeight = F(p, "Final spike height", "faultlineMaxHeight", Defaults.FaultlineMaxHeight, 0.5f, 30f);
            faultlineLaunchSpeed = F(p, "Light-enemy upward launch", "faultlineLaunchSpeed", Defaults.FaultlineLaunchSpeed, 0f, 60f);

            p = new ConfigPanel(earth, "Jackhammer — Stoneguard / Fortify", "valuesEarthStoneguard");
            fortifyDuration = F(p, "Fortify duration", "fortifyDuration", Defaults.FortifyDuration, 0f, 60f);
            fortifyHardDamageMultiplier = F(p, "Hard-damage multiplier while Fortified", "fortifyHardDamageMultiplier", Defaults.FortifyHardDamageMultiplier, 0f, 1f);

            p = new ConfigPanel(earth, "Sawblade Launcher — Rock Burst", "valuesEarthRock");
            earthRapidHeatOnPrimaryShot = F(p, "Shared Overheat heat set by each primary saw (0-1)", "earthRapidHeatOnPrimaryShot", Defaults.EarthRapidHeatOnPrimaryShot, 0f, 1f);
            earthRapidHeatOnAltFire = F(p, "Shared Overheat heat set by Rock Burst Alt-Fire (0-1)", "earthRapidHeatOnAltFire", Defaults.EarthRapidHeatOnAltFire, 0f, 1f);
            earthSawHitAmount = F(p, "Saw hitAmount / durability budget", "earthSawHitAmount", Defaults.EarthSawHitAmount, 1f, 20f);
            rockPrimaryInterval = F(p, "Primary fire interval", "rockPrimaryInterval", Defaults.RockPrimaryInterval, 0.03f, 2f);
            rockBurstCooldown = F(p, "Seven-saw Alt-Fire cooldown", "rockBurstCooldown", Defaults.RockBurstCooldown, 0f, 30f);
            rockBurstSpreadMultiplier = F(p, "Alt-Fire spread multiplier (1 = vanilla Overheat Nailgun spread)", "rockBurstSpreadMultiplier", Defaults.RockBurstSpreadMultiplier, 0f, 4f);

            p = new ConfigPanel(earth, "Ultimate — Meteor Shower", "valuesEarthMeteor");
            meteorCost = F(p, "Ultimate charge cost (full bar = " + N(Defaults.UltimateChargeMax) + ")", "meteorCost", Defaults.MeteorCost, 0.05f, Defaults.UltimateChargeMax);
            meteorInterval = F(p, "Time between meteor impacts", "meteorInterval", Defaults.MeteorInterval, 0.05f, 3f);
            meteorTravelDuration = F(p, "Meteor fall time", "meteorTravelDuration", Defaults.MeteorTravelDuration, 0.1f, 3f);
            meteorAreaRadius = F(p, "Target-area radius", "meteorAreaRadius", Defaults.MeteorAreaRadius, 0f, 30f);
            meteorDamage = F(p, "Explosion damage per meteor", "meteorDamage", Defaults.MeteorDamage, 0f, 50f);
            meteorExplosionScale = F(p, "Meteor explosion size", "meteorExplosionScale", Defaults.MeteorExplosionScale, 0.1f, 5f);
            meteorSelfDamage = F(p, "Self-damage per meteor explosion", "meteorSelfDamage", Defaults.MeteorSelfDamage, 0f, 100f);

        }

        private static void ApplyBalanceDefaultMigration()
        {
            if (Plugin.Instance == null)
                return;

            // PluginConfigurator correctly preserves user values. Advance only exact values from
            // previously released defaults; genuinely customized values remain user-owned.
            BepInEx.Configuration.ConfigEntry<int> migration = Plugin.Instance.Config.Bind(
                "Internal", "BalanceDefaultsVersion", 0,
                "Internal one-time migration marker for Elemental Battlegrounds balance defaults.");
            const int CurrentBalanceDefaultsVersion = 23;
            if (migration.Value >= CurrentBalanceDefaultsVersion)
                return;

            if (migration.Value < 9)
            {
                if (counterCooldown != null && Mathf.Approximately(counterCooldown.value, 2f))
                    counterCooldown.value = 1f;
                if (infernoExplosionDamage != null && Mathf.Approximately(infernoExplosionDamage.value, 125f))
                    infernoExplosionDamage.value = 25f;
            }
            if (migration.Value < 16)
            {
                if (updraftEnemySpeed != null && Mathf.Approximately(updraftEnemySpeed.value, 24f))
                    updraftEnemySpeed.value = 40f;
                if (slipstreamCooldown != null && Mathf.Approximately(slipstreamCooldown.value, 5.5f))
                    slipstreamCooldown.value = 6.5f;
            }

            if (migration.Value < 17)
            {
                if (counterHitstop != null && Mathf.Approximately(counterHitstop.value, 0.1f))
                    counterHitstop.value = Defaults.CounterHitstop;
            }
            if (migration.Value < 18)
            {
                // 0 was the released Flashstep pass-through default before Storm's electric
                // contact hit became part of the intended kit. Preserve nonzero custom values.
                if (flashstepContactDamage != null && Mathf.Approximately(flashstepContactDamage.value, 0f))
                    flashstepContactDamage.value = Defaults.FlashstepContactDamage;
            }
            if (migration.Value < 19)
            {
                // 0.0.39 lowered Meteor's released default from 3.0 to 2.75. As with earlier
                // migrations, only the exact previous default is advanced; other custom values stay.
                if (meteorDamage != null && Mathf.Approximately(meteorDamage.value, 3f))
                    meteorDamage.value = Defaults.MeteorDamage;
            }
            if (migration.Value < 20)
            {
                // 0.0.40 promotes the playtest values explicitly confirmed as intentional into
                // the source defaults. Advance only fields still sitting on the exact 0.0.39
                // defaults so genuine user customizations survive the update.
                if (pressureCooldown != null && Mathf.Approximately(pressureCooldown.value, 0.75f))
                    pressureCooldown.value = Defaults.PressureCooldown;
                if (tempestCooldown != null && Mathf.Approximately(tempestCooldown.value, 4f))
                    tempestCooldown.value = Defaults.TempestCooldown;
                if (tempestWaveInterval != null && Mathf.Approximately(tempestWaveInterval.value, 0.12f))
                    tempestWaveInterval.value = Defaults.TempestWaveInterval;
                if (grandCycloneRadius != null && Mathf.Approximately(grandCycloneRadius.value, 16f))
                    grandCycloneRadius.value = Defaults.GrandCycloneRadius;
                if (grandCycloneDamage != null && Mathf.Approximately(grandCycloneDamage.value, 0.5f))
                    grandCycloneDamage.value = Defaults.GrandCycloneDamage;
                if (windSawHitAmount != null && Mathf.Approximately(windSawHitAmount.value, 1f))
                    windSawHitAmount.value = Defaults.WindSawHitAmount;

                if (stormRevolverExplosionDamage != null && Mathf.Approximately(stormRevolverExplosionDamage.value, 2f))
                    stormRevolverExplosionDamage.value = Defaults.StormRevolverExplosionDamage;
                if (stormRevolverExplosionScale != null && Mathf.Approximately(stormRevolverExplosionScale.value, 0.65f))
                    stormRevolverExplosionScale.value = Defaults.StormRevolverExplosionScale;
                if (flashstepCooldown != null && Mathf.Approximately(flashstepCooldown.value, 4f))
                    flashstepCooldown.value = Defaults.FlashstepCooldown;
                if (flashstepDistance != null && Mathf.Approximately(flashstepDistance.value, 18f))
                    flashstepDistance.value = Defaults.FlashstepDistance;
                if (thunderlineCooldown != null && Mathf.Approximately(thunderlineCooldown.value, 5.5f))
                    thunderlineCooldown.value = Defaults.ThunderlineCooldown;
                if (thunderlineFirstDistance != null && Mathf.Approximately(thunderlineFirstDistance.value, 5f))
                    thunderlineFirstDistance.value = Defaults.ThunderlineFirstDistance;
                if (thunderlineSpacing != null && Mathf.Approximately(thunderlineSpacing.value, 5f))
                    thunderlineSpacing.value = Defaults.ThunderlineSpacing;

                if (faultlineSpikeCount != null && faultlineSpikeCount.value == 7)
                    faultlineSpikeCount.value = Defaults.FaultlineSpikeCount;
                if (faultlineCooldown != null && Mathf.Approximately(faultlineCooldown.value, 3.5f))
                    faultlineCooldown.value = Defaults.FaultlineCooldown;
                if (faultlineMaxHeight != null && Mathf.Approximately(faultlineMaxHeight.value, 8f))
                    faultlineMaxHeight.value = Defaults.FaultlineMaxHeight;
                if (rockBurstCooldown != null && Mathf.Approximately(rockBurstCooldown.value, 4f))
                    rockBurstCooldown.value = Defaults.RockBurstCooldown;
                if (rockBurstSpreadMultiplier != null && Mathf.Approximately(rockBurstSpreadMultiplier.value, 1f))
                    rockBurstSpreadMultiplier.value = Defaults.RockBurstSpreadMultiplier;
                if (earthSawHitAmount != null && Mathf.Approximately(earthSawHitAmount.value, 3.9f))
                    earthSawHitAmount.value = Defaults.EarthSawHitAmount;
                if (meteorCost != null && Mathf.Approximately(meteorCost.value, 5f))
                    meteorCost.value = Defaults.MeteorCost;
                if (meteorAreaRadius != null && Mathf.Approximately(meteorAreaRadius.value, 8f))
                    meteorAreaRadius.value = Defaults.MeteorAreaRadius;
                if (meteorExplosionScale != null && Mathf.Approximately(meteorExplosionScale.value, 0.85f))
                    meteorExplosionScale.value = Defaults.MeteorExplosionScale;
            }

            if (migration.Value < 21)
            {
                // 0.0.41 extends both elemental counter buffs and slightly lengthens Inferno's
                // windup. Advance only exact 0.0.40 defaults so custom balance edits survive.
                if (counterBuffDuration != null && Mathf.Approximately(counterBuffDuration.value, 6f))
                    counterBuffDuration.value = Defaults.CounterBuffDuration;
                if (fortifyDuration != null && Mathf.Approximately(fortifyDuration.value, 6f))
                    fortifyDuration.value = Defaults.FortifyDuration;
                if (infernoWindup != null && Mathf.Approximately(infernoWindup.value, 3.5f))
                    infernoWindup.value = Defaults.InfernoWindup;
            }
            if (migration.Value < 22)
            {
                // 0.0.43 promotes the confirmed Grass balance pass. Only exact released 0.0.42
                // defaults advance; existing custom Spore values remain user-owned. PoisonMaxPotency
                // is a new field and therefore naturally starts at its 0.75 source default.
                if (sporeCost != null && Mathf.Approximately(sporeCost.value, 2f))
                    sporeCost.value = Defaults.SporeCost;
                if (sporeIgniteMaxDamage != null && Mathf.Approximately(sporeIgniteMaxDamage.value, 8f))
                    sporeIgniteMaxDamage.value = Defaults.SporeIgniteMaxDamage;
            }
            if (migration.Value < 23)
            {
                // 0.0.44 shortens Grand Cyclone from the previous released 8-second default.
                // Preserve genuinely customized durations.
                if (grandCycloneDuration != null && Mathf.Approximately(grandCycloneDuration.value, 8f))
                    grandCycloneDuration.value = Defaults.GrandCycloneDuration;
            }

            migration.Value = CurrentBalanceDefaultsVersion;
            Plugin.Instance.Config.Save();
        }

        private static void ResetAllToDefaults()
        {
            foreach (Action reset in resetActions)
                reset();
            Plugin.LogSource?.LogInfo("Reset all Elemental Battlegrounds custom weapon values to source defaults.");
        }

        private static FloatField F(ConfigPanel panel, string name, string guid, float value, float min, float max)
        {
            FloatField field = new FloatField(panel, name, guid, value, min, max);
            resetActions.Add(() => field.value = value);
            return field;
        }

        private static IntField I(ConfigPanel panel, string name, string guid, int value, int min, int max)
        {
            IntField field = new IntField(panel, name, guid, value, min, max);
            resetActions.Add(() => field.value = value);
            return field;
        }
    }
}
