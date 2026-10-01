using System.Text;
using UnityEngine;

namespace ElementalBattlegroundsMod
{
    internal static class ElementGuideContent
    {
        internal static string Build(ElementId element, bool advanced)
        {
            const string vanillaBadge = "  <size=70%><color=#AEB8C2>(VANILLA)</color></size>";
            StringBuilder b = new StringBuilder();

            if (element == ElementId.None)
            {
                b.Append("<b>NONE</b>\n\n");
                b.Append("Leaves this EB arsenal position empty. Use this when you deliberately want fewer than three weapons in a family.");
                return b.ToString();
            }

            if (!advanced)
            {
                switch (element)
                {
                    case ElementId.Fire:
                        AddGuideWeapon(b, "REVOLVER", "Fire Column Revolver",
                            "Slab Revolver primary. Alt-Fire fires a hitscan that plants a damaging vertical fire column at the target or floor beneath it. Coins can route and strengthen the column.");
                        AddGuideWeapon(b, "CLOSE", "Counter Shotgun",
                            "Shotgun primary. Alt-Fire performs a Jackhammer-style counter/parry. A true melee parry grants Combustion, temporarily letting EB attacks apply weak fire. Its remaining time appears as a Fire-colored ring around the crosshair.");
                        AddGuideWeapon(b, "RAPID", "Overheat Nailgun" + vanillaBadge,
                            "Vanilla Overheat Nailgun.");
                        AddGuideWeapon(b, "ULTIMATE", "Inferno",
                            "Consumes the full Ultimate charge bar, heavily slows you during a windup, then releases a huge enemy-only fire explosion. Weapon swapping and Whiplash are locked during the cast.");
                        AddGuideWeapon(b, "EXPLOSIVE", "Firestarter Rocket Launcher" + vanillaBadge,
                            "Vanilla Firestarter Rocket Launcher.");
                        break;

                    case ElementId.Water:
                        AddGuideWeapon(b, "REVOLVER", "Piercer Revolver" + vanillaBadge,
                            "Vanilla Piercer Revolver.");
                        AddGuideWeapon(b, "CLOSE", "Water Jackhammer",
                            "Jackhammer primary. Alt-Fire redirects your current momentum into the exact aim direction with a guaranteed high-speed dash. Alt-Fire does not need to be fully drawn to use.");
                        AddGuideWeapon(b, "RAPID", "Attractor Nailgun" + vanillaBadge,
                            "Vanilla Attractor Nailgun.");
                        AddGuideWeapon(b, "ULTIMATE", "Water Dragon",
                            "Consumes the full Ultimate charge bar to summon a homing dragon. When it attaches to an enemy, kill that enemy before the slam to cancel the slam, build the dragon's chain, and send it after another target. The final slam grows stronger with the chain.");
                        AddGuideWeapon(b, "EXPLOSIVE", "Geyser Launcher",
                            "Rocket Launcher primary. Alt-Fire creates a geyser on the floor that launches nearby enemies and can launch you upward while preserving horizontal momentum. You must be close enough to the floor for your own geyser to launch you. Alt-Fire does not need to be fully drawn to use.");
                        break;

                    case ElementId.Grass:
                        AddGuideWeapon(b, "REVOLVER", "Vine Revolver",
                            "Revolver primary. Alt-Fire creates a shrinking ring at the aim point that captures movable enemies and drags them toward the center.");
                        AddGuideWeapon(b, "CLOSE", "Cyclone Shotgun",
                            "Shotgun primary. Alt-Fire launches a damaging cyclone that pushes enemies around and can be redirected by shooting it with the shotgun. Alt-Fire does not need to be fully drawn to use.");
                        AddGuideWeapon(b, "RAPID", "Poison Nailgun",
                            "Jumpstart Nailgun primary. Alt-Fire fires a Poison Seed; while a live seed exists, the Nailgun fires faster and nails can build poison on the seeded enemy. Alt-Fire does not need to be fully drawn to use.");
                        AddGuideWeapon(b, "ULTIMATE", "Spore Bombardment",
                            "Uses part of the Ultimate charge to launch a spore projectile that becomes a poison cloud. Fire and explosions can ignite the cloud for a stronger area blast.");
                        AddGuideWeapon(b, "EXPLOSIVE", "Grass Rocket Launcher",
                            "Rocket Launcher primary. Alt-Fire fires a quick three-rocket burst with reduced damage per rocket. Alt-Fire does not need to be fully drawn to use.");
                        break;

                    case ElementId.Wind:
                        AddGuideWeapon(b, "REVOLVER", "Pressure Revolver",
                            "Revolver primary. Alt-Fire releases a Knuckleblaster-style shockwave that deflects projectiles and launches enemies forward and upward.");
                        AddGuideWeapon(b, "CLOSE", "Updraft Shotgun",
                            "Shotgun primary. Alt-Fire launches you upward and strongly lifts nearby enemies, setting up aerial follow-ups. Alt-Fire does not need to be fully drawn to use.");
                        AddGuideWeapon(b, "RAPID", "Tempest Sawblade Launcher",
                            "Sawblade Launcher primary with infinite ammo. Alt-Fire fires three staged waves: one saw, then two, then three. Alt-Fire does not need to be fully drawn to use.");
                        AddGuideWeapon(b, "ULTIMATE", "Grand Cyclone",
                            "Consumes the full Ultimate charge bar to create a persistent tornado that pulls enemies into its three-dimensional center so they can be kept airborne.");
                        AddGuideWeapon(b, "EXPLOSIVE", "Slipstream Grenade Launcher",
                            "Grenade Launcher primary. Alt-Fire gives a short steerable burst of flight; repeated use shares ULTRAKILL's rocket-ride limit and rapidly loses duration. Alt-Fire does not need to be fully drawn to use.");
                        break;

                    case ElementId.Storm:
                        AddGuideWeapon(b, "REVOLVER", "Storm Standard Revolver",
                            "Revolver primary. Alt-Fire charges like the Piercer and keeps its Coin splitshot behavior, firing a piercing hitscan that damages each enemy once before creating a small blue explosion. Coins strengthen both the hitscan and explosion.");
                        AddGuideWeapon(b, "CLOSE", "Flashstep Jackhammer",
                            "Jackhammer primary. Alt-Fire teleports you toward the point under your crosshair, preserving your existing velocity and stopping safely at solid geometry. Enemies passed through take electric damage that can trigger Conduction. Alt-Fire does not need to be fully drawn to use.");
                        AddGuideWeapon(b, "RAPID", "Jumpstart Nailgun" + vanillaBadge,
                            "Vanilla Jumpstart Nailgun.");
                        AddGuideWeapon(b, "ULTIMATE", "Electric Railcannon" + vanillaBadge,
                            "Vanilla Electric Railcannon.");
                        AddGuideWeapon(b, "EXPLOSIVE", "Thunderline Grenade Launcher",
                            "Grenade Launcher primary. Alt-Fire creates five sequential lightning strikes along the aimed ground path; large targets can be caught by multiple explosions. Once started, the line finishes even if you swap weapons. Alt-Fire does not need to be fully drawn to use.");
                        break;

                    case ElementId.Earth:
                        AddGuideWeapon(b, "REVOLVER", "Faultline Revolver",
                            "Slab Revolver primary. Alt-Fire sends a line of stone spikes along terrain; later spikes grow taller, lightly launch enemies, and briefly block appropriate projectiles.");
                        AddGuideWeapon(b, "CLOSE", "Stoneguard Jackhammer",
                            "Jackhammer primary. Alt-Fire is a defensive melee parry; a true parry grants Fortify, reducing incoming hard-damage buildup rather than health damage. Its remaining time appears as an Earth-colored ring around the crosshair; simultaneous elemental parry buffs use larger concentric rings.");
                        AddGuideWeapon(b, "RAPID", "Rock Sawblade Launcher",
                            "Sawblade Launcher primary. Alt-Fire releases seven saws at once in a wide spread after the initial weapon draw finishes.");
                        AddGuideWeapon(b, "ULTIMATE", "Meteor Shower",
                            "Uses Ultimate charge to call down a meteor shower around the aimed point. Each impact damages enemies without knocking them away and can heavily damage you if you stand in the blast. The meteors land in sequence across the target area.");
                        AddGuideWeapon(b, "EXPLOSIVE", "S.R.S. Cannon" + vanillaBadge,
                            "Vanilla S.R.S. Cannon.");
                        break;

                    case ElementId.Physical:
                        b.Append("Physical is a planned element and does not have a finalized weapon kit yet.");
                        break;
                    case ElementId.PhysicalAlt:
                        b.Append("Physical Alt is planned as a separate element/weapon set. It currently reuses the Physical icon until dedicated artwork is made.");
                        break;
                    case ElementId.Sans:
                        b.Append("Sans is planned as a cheat-exclusive element. It will only be usable while ULTRAKILL cheats are enabled.");
                        break;
                    default:
                        b.Append("This element has artwork and a reserved position, but its weapon kit has not been implemented or documented yet.");
                        break;
                }
                return b.ToString();
            }

            // Advanced-guide numbers intentionally come from the code defaults, never the
            // player's Plugin Configurator overrides. The guide describes stock EB balance.
            switch (element)
            {
                case ElementId.Fire:
                    AddGuideWeapon(b, "REVOLVER", "Fire Column Revolver",
                        "Slab Revolver primary. Alt-Fire cooldown " + N(WeaponTuning.Defaults.FireColumnCooldown) +
                        "s. The column lasts " + N(WeaponTuning.Defaults.FireColumnLifetime) + "s and deals " +
                        N(WeaponTuning.Defaults.FireColumnDamage) + " damage every " + N(WeaponTuning.Defaults.FireColumnTick) +
                        "s. Coins strengthen it; a full four-coin chain targets " +
                        N(WeaponTuning.Defaults.FireColumnFourCoinTotalDamage) + " total column damage.");
                    AddGuideWeapon(b, "CLOSE", "Counter Shotgun",
                        "Shotgun primary. Alt-Fire cooldown " + N(WeaponTuning.Defaults.CounterCooldown) +
                        "s; counter hit damage " + N(WeaponTuning.Defaults.CounterDamage) + "; hitstop " +
                        N(WeaponTuning.Defaults.CounterHitstop) + "s. A true melee parry grants Combustion for " +
                        N(WeaponTuning.Defaults.CounterBuffDuration) +
                        "s. Counter can use normal Jackhammer projectile tech on your own valid projectiles, including freeze-frame rocket setups. Combustion time appears as a Fire-colored crosshair ring; simultaneous elemental parry buffs use larger concentric rings.");
                    AddGuideWeapon(b, "RAPID", "Overheat Nailgun" + vanillaBadge,
                        "Vanilla Overheat Nailgun.");
                    AddGuideWeapon(b, "ULTIMATE", "Inferno",
                        "Costs " + UltimatePercent(WeaponTuning.Defaults.InfernoCost) + " Ultimate charge. Windup " + N(WeaponTuning.Defaults.InfernoWindup) +
                        "s and final enemy-only explosion damage " + N(WeaponTuning.Defaults.InfernoExplosionDamage) +
                        ". Movement is heavily suppressed through the windup; weapon swapping and Whiplash are locked until it ends.");
                    AddGuideWeapon(b, "EXPLOSIVE", "Firestarter Rocket Launcher" + vanillaBadge,
                        "Vanilla Firestarter Rocket Launcher.");
                    break;

                case ElementId.Water:
                    AddGuideWeapon(b, "REVOLVER", "Piercer Revolver" + vanillaBadge,
                        "Vanilla Piercer Revolver.");
                    AddGuideWeapon(b, "CLOSE", "Water Jackhammer",
                        "Jackhammer primary. Alt-Fire cooldown " + N(WeaponTuning.Defaults.WaterDashCooldown) +
                        "s. Alt-Fire redirects your current momentum into the exact aim direction with a guaranteed high-speed dash and does not need to be fully drawn to use.");
                    AddGuideWeapon(b, "RAPID", "Attractor Nailgun" + vanillaBadge,
                        "Vanilla Attractor Nailgun.");
                    AddGuideWeapon(b, "ULTIMATE", "Water Dragon",
                        "Costs " + UltimatePercent(WeaponTuning.Defaults.DragonCost) + " Ultimate charge. Maximum lifetime " + N(WeaponTuning.Defaults.DragonMaxLifetime) +
                        "s and maximum chain " + WeaponTuning.Defaults.DragonMaxChain + ". When the dragon attaches to an enemy, killing that enemy before the slam cancels the slam, adds one chain, and sends the dragon after another target. Final slam damage starts at " +
                        N(WeaponTuning.Defaults.DragonBaseDamage) + " and gains " +
                        N(WeaponTuning.Defaults.DragonDamagePerChain) + " per chain before falloff.");
                    AddGuideWeapon(b, "EXPLOSIVE", "Geyser Launcher",
                        "Rocket Launcher primary. Alt-Fire cooldown " + N(WeaponTuning.Defaults.GeyserCooldown) +
                        "s and geyser lifetime " + N(WeaponTuning.Defaults.GeyserLifetime) +
                        "s. The geyser launches enemies and can launch you while preserving horizontal momentum, but you must be close enough to the floor for your own geyser to affect you. Alt-Fire does not need to be fully drawn to use.");
                    break;

                case ElementId.Grass:
                    AddGuideWeapon(b, "REVOLVER", "Vine Revolver",
                        "Revolver primary. Alt-Fire cooldown " + N(WeaponTuning.Defaults.VineCooldown) +
                        "s. The ring closes and pulls for " + N(WeaponTuning.Defaults.VinePullDuration) +
                        "s, then holds captured enemies for " + N(WeaponTuning.Defaults.VineHoldDuration) + "s.");
                    AddGuideWeapon(b, "CLOSE", "Cyclone Shotgun",
                        "Shotgun primary. Alt-Fire cooldown " + N(WeaponTuning.Defaults.CycloneCooldown) +
                        "s; cyclone lifetime " + N(WeaponTuning.Defaults.CycloneLifetime) + "s; contact damage " +
                        N(WeaponTuning.Defaults.CycloneDamage) + ". Shotgun hits can redirect the cyclone. Alt-Fire does not need to be fully drawn to use.");
                    AddGuideWeapon(b, "RAPID", "Poison Nailgun",
                        "Jumpstart Nailgun primary. Poison Seed cooldown " + N(WeaponTuning.Defaults.PoisonOrbCooldown) +
                        "s and attached-seed lifetime " + N(WeaponTuning.Defaults.PoisonSeedLifetime) +
                        "s. A live seed increases fire rate; each qualifying nail adds " +
                        N(WeaponTuning.Defaults.PoisonDuration) + "s poison duration and " +
                        N(WeaponTuning.Defaults.PoisonPotency) + " potency, capped at " +
                        N(WeaponTuning.Defaults.PoisonMaxPotency) + " damage per poison tick. Each primary nail sets shared Overheat heat to " +
                        N(WeaponTuning.Defaults.GrassRapidHeatWhileFiring * 100f) + "% normally or " +
                        N(WeaponTuning.Defaults.GrassRapidHeatWithLiveSeed * 100f) + "% while any Poison Seed is live. Alt-Fire does not need to be fully drawn to use.");
                    AddGuideWeapon(b, "ULTIMATE", "Spore Bombardment",
                        "Costs " + UltimatePercent(WeaponTuning.Defaults.SporeCost) +
                        " Ultimate charge per shot with a " + N(WeaponTuning.Defaults.SporeFireLockout) +
                        "s repeat lockout. The cloud lasts " + N(WeaponTuning.Defaults.SporeCloudLifetime) +
                        "s. Igniting it deals roughly " + N(WeaponTuning.Defaults.SporeIgniteBaseDamage) + "–" +
                        N(WeaponTuning.Defaults.SporeIgniteMaxDamage) + " damage depending on remaining cloud life.");
                    AddGuideWeapon(b, "EXPLOSIVE", "Grass Rocket Launcher",
                        "Rocket Launcher primary. Alt-Fire fires three rockets " +
                        N(WeaponTuning.Defaults.GrassRocketInterval) + "s apart. Burst cooldown " +
                        N(WeaponTuning.Defaults.GrassRocketCooldown) + "s; each burst rocket deals " +
                        N(WeaponTuning.Defaults.GrassRocketDamageMultiplier * 100f) + "% normal total damage. Alt-Fire does not need to be fully drawn to use.");
                    break;

                case ElementId.Wind:
                    AddGuideWeapon(b, "REVOLVER", "Pressure Revolver",
                        "Revolver primary. Alt-Fire cooldown " + N(WeaponTuning.Defaults.PressureCooldown) +
                        "s and damage " + N(WeaponTuning.Defaults.PressureDamage) +
                        ". Uses a Knuckleblaster-style shockwave for projectile deflection and feedback while launching enemies forward and upward.");
                    AddGuideWeapon(b, "CLOSE", "Updraft Shotgun",
                        "Shotgun primary. Alt-Fire cooldown " + N(WeaponTuning.Defaults.UpdraftCooldown) +
                        "s and enemy damage " + N(WeaponTuning.Defaults.UpdraftDamage) +
                        ". Alt-Fire launches you upward and strongly lifts nearby enemies. Alt-Fire does not need to be fully drawn to use.");
                    AddGuideWeapon(b, "RAPID", "Tempest Sawblade Launcher",
                        "Sawblade Launcher primary fires every " + N(WeaponTuning.Defaults.TempestPrimaryInterval) + "s with infinite ammo. Alt-Fire sends 1 → 2 → 3 saws with " +
                        N(WeaponTuning.Defaults.TempestWaveInterval) + "s between waves. Alt-Fire cooldown " +
                        N(WeaponTuning.Defaults.TempestCooldown) + "s; primary fire is blocked during the burst. Each primary saw sets shared Overheat heat to " +
                        N(WeaponTuning.Defaults.WindRapidHeatWhileFiring * 100f) + "%; each Tempest wave sets it to " +
                        N(WeaponTuning.Defaults.WindRapidHeatOnAltFire * 100f) + "%. Saw hitAmount is " +
                        N(WeaponTuning.Defaults.WindSawHitAmount) + ". Alt-Fire does not need to be fully drawn to use.");
                    AddGuideWeapon(b, "ULTIMATE", "Grand Cyclone",
                        "Costs " + UltimatePercent(WeaponTuning.Defaults.GrandCycloneCost) + " Ultimate charge. Duration " + N(WeaponTuning.Defaults.GrandCycloneDuration) +
                        "s; deals " + N(WeaponTuning.Defaults.GrandCycloneDamage) + " damage every " +
                        N(WeaponTuning.Defaults.GrandCycloneTick) +
                        "s while pulling enemies toward the visible cyclone midpoint, including vertically.");
                    AddGuideWeapon(b, "EXPLOSIVE", "Slipstream Grenade Launcher",
                        "Grenade Launcher primary. Alt-Fire cooldown " + N(WeaponTuning.Defaults.SlipstreamCooldown) +
                        "s and base flight duration " + N(WeaponTuning.Defaults.SlipstreamDuration) +
                        "s. Each use consumes the same rocket-ride budget, so repeated use rapidly shortens the available flight time. Alt-Fire does not need to be fully drawn to use.");
                    break;

                case ElementId.Storm:
                    AddGuideWeapon(b, "REVOLVER", "Storm Standard Revolver",
                        "Revolver primary. Alt-Fire uses the Piercer charge, piercing and Coin splitshot behavior while damaging each enemy only once. The charged hit creates a small explosion for " +
                        N(WeaponTuning.Defaults.StormRevolverExplosionDamage) + " base damage; Coin bonuses strengthen the explosion too. Self-damage is " +
                        N(WeaponTuning.Defaults.StormRevolverSelfDamage) + ".");
                    AddGuideWeapon(b, "CLOSE", "Flashstep Jackhammer",
                        "Jackhammer primary. Alt-Fire cooldown " + N(WeaponTuning.Defaults.FlashstepCooldown) +
                        "s. Flashstep teleports toward the surface under your crosshair up to its maximum distance while preserving existing velocity. Enemies passed through take " +
                        N(WeaponTuning.Defaults.FlashstepContactDamage) + " electric damage and use ULTRAKILL's native Electricity attribute, allowing Conduction. Alt-Fire does not need to be fully drawn to use.");
                    AddGuideWeapon(b, "RAPID", "Jumpstart Nailgun" + vanillaBadge,
                        "Vanilla Jumpstart Nailgun.");
                    AddGuideWeapon(b, "ULTIMATE", "Electric Railcannon" + vanillaBadge,
                        "Vanilla Electric Railcannon.");
                    AddGuideWeapon(b, "EXPLOSIVE", "Thunderline Grenade Launcher",
                        "Grenade Launcher primary. Alt-Fire cooldown " + N(WeaponTuning.Defaults.ThunderlineCooldown) +
                        "s. Fires " + WeaponTuning.Defaults.ThunderlineStrikeCount + " sequential strikes " +
                        N(WeaponTuning.Defaults.ThunderlineStrikeInterval) + "s apart, each creating a " +
                        N(WeaponTuning.Defaults.ThunderlineDamage) + " damage explosion. First-strike distance is " +
                        N(WeaponTuning.Defaults.ThunderlineFirstDistance) + "; distance between strikes is " +
                        N(WeaponTuning.Defaults.ThunderlineSpacing) + ". Each explosion has " +
                        N(WeaponTuning.Defaults.ThunderlineSelfDamage) + " self-damage. Once started, the line finishes even if you swap weapons. Alt-Fire does not need to be fully drawn to use.");
                    break;

                case ElementId.Earth:
                    AddGuideWeapon(b, "REVOLVER", "Faultline Revolver",
                        "Slab Revolver primary. Alt-Fire has a " + N(WeaponTuning.Defaults.FaultlineCooldown) +
                        "s cooldown and creates " + WeaponTuning.Defaults.FaultlineSpikeCount + " sequential terrain spikes " +
                        N(WeaponTuning.Defaults.FaultlineSpikeInterval) + "s apart. Each active spike can deal " +
                        N(WeaponTuning.Defaults.FaultlineDamage) + " damage every " + N(WeaponTuning.Defaults.FaultlineDamageTick) +
                        "s, but overlapping spikes share the same enemy hit timer so one tick cannot double-hit. Alt-Fire follows normal Revolver readiness.");
                    AddGuideWeapon(b, "CLOSE", "Stoneguard Jackhammer",
                        "Jackhammer primary. Alt-Fire cooldown " + N(WeaponTuning.Defaults.CounterCooldown) +
                        "s; parry hit damage " + N(WeaponTuning.Defaults.CounterDamage) + "; hitstop " +
                        N(WeaponTuning.Defaults.CounterHitstop) + "s. It can parry valid projectiles as well as melee attacks. A true melee parry grants Fortify for " +
                        N(WeaponTuning.Defaults.FortifyDuration) + "s; Fortify leaves health damage unchanged but multiplies hard-damage buildup by " +
                        N(WeaponTuning.Defaults.FortifyHardDamageMultiplier * 100f) + "%. The initial Jackhammer draw must finish before Alt-Fire can be used. Active elemental parry buffs show as concentric colored rings around the crosshair.");
                    AddGuideWeapon(b, "RAPID", "Rock Sawblade Launcher",
                        "Sawblade Launcher primary with infinite ammo and a " + N(WeaponTuning.Defaults.RockPrimaryInterval) +
                        "s firing interval. Alt-Fire releases " + WeaponTuning.Defaults.RockBurstSawCount +
                        " non-silver saws simultaneously in a wide spread, then recharges for " +
                        N(WeaponTuning.Defaults.RockBurstCooldown) + "s. Primary saws set shared Overheat heat to " +
                        N(WeaponTuning.Defaults.EarthRapidHeatOnPrimaryShot * 100f) + "% and Rock Burst sets it to " +
                        N(WeaponTuning.Defaults.EarthRapidHeatOnAltFire * 100f) + "%. Each saw can damage roughly two enemies before breaking, with some durability also spent by ricochets. The initial Sawblade Launcher draw must finish before Alt-Fire can activate.");
                    AddGuideWeapon(b, "ULTIMATE", "Meteor Shower",
                        "Costs " + UltimatePercent(WeaponTuning.Defaults.MeteorCost) + " Ultimate charge. Calls down " + WeaponTuning.Defaults.MeteorCount +
                        " meteors across a " + N(WeaponTuning.Defaults.MeteorAreaRadius) + "-unit radius, with impacts arriving " +
                        N(WeaponTuning.Defaults.MeteorInterval) + "s apart. Each meteor deals " +
                        N(WeaponTuning.Defaults.MeteorDamage) + " enemy damage with no enemy knockback and deals " +
                        N(WeaponTuning.Defaults.MeteorSelfDamage) + " self-damage if the player is caught in the impact.");
                    AddGuideWeapon(b, "EXPLOSIVE", "S.R.S. Cannon" + vanillaBadge,
                        "Vanilla S.R.S. Cannon.");
                    break;
            }

            return b.ToString();
        }

        private static string UltimatePercent(float charge)
        {
            return N(Mathf.Clamp01(charge / WeaponTuning.Defaults.UltimateChargeMax) * 100f) + "%";
        }

        private static void AddGuideWeapon(StringBuilder b, string family, string name, string description)
        {
            if (b.Length > 0)
                b.Append("\n\n");
            b.Append("<size=82%><color=#A8B5C2><b>");
            b.Append(family);
            b.Append("</b></color></size>\n");
            b.Append("<size=118%><b>");
            b.Append(name);
            b.Append("</b></size>\n");
            b.Append(description);
        }

        private static string N(float value)
        {
            return value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }

    }
}
