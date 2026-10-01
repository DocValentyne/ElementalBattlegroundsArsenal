using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ElementalBattlegroundsMod
{
    /// <summary>
    /// Runtime-only bridge to Grenade Launcher 2.0.2. Reflection keeps EB source/build
    /// independent from GrenadeLauncher.dll while the soft BepInEx dependency guarantees
    /// correct load order when the companion mod is installed.
    /// </summary>
    internal static class GrenadeLauncherCompat
    {
        private static bool resolved;
        private static bool available;
        private static bool warnedUnavailable;
        private static Type integrationType;
        private static Type paletteType;
        private static Type projectileAppearanceType;
        private static Type pluginType;
        private static MethodInfo registerNative;
        private static MethodInfo registerGrenade;
        private static MethodInfo registerGrenadeDefault;
        private static MethodInfo playExternalSecondaryAnimation;
        private static MethodInfo setExternalSecondaryCooldownProgress;
        private static MethodInfo setExternalPrimaryProjectileAppearance;
        private static MethodInfo setNextExternalProjectileAppearance;
        private static ConstructorInfo paletteConstructor;
        private static PropertyInfo pluginInstanceProperty;
        private static MethodInfo terminalGrenadeModeMethod;

        internal static bool Available
        {
            get
            {
                Resolve();
                return available;
            }
        }

        internal static void Configure(GameObject weapon, ElementalSlotMarker marker)
        {
            if (weapon == null || marker == null || marker.family != WeaponFamily.Explosive || marker.IsEmpty)
                return;

            RocketLauncher launcher = weapon.GetComponent<RocketLauncher>();
            if (launcher == null)
                return;

            Resolve();
            if (!available)
            {
                WarnUnavailable();
                return;
            }

            bool ownsSecondary = weapon.GetComponent<ElementalSecondaryOwner>() != null;
            try
            {
                // Direct weapon choices in the loadout menu are exact. Picking a Rocket
                // Launcher must stay a rocket even if the terminal globally selected GL;
                // picking Blue/Green/Red Grenade Launcher must force GL behavior without
                // stealing its own alternate fire.
                if (marker.explosiveMode == ExplosiveIntegrationMode.ForceNative)
                {
                    registerNative.Invoke(null, new object[] { launcher, false });
                    return;
                }
                if (marker.explosiveMode == ExplosiveIntegrationMode.ForceGrenade)
                {
                    registerGrenadeDefault.Invoke(null, new object[] { launcher, false });
                    return;
                }

                // API v1 exposes native Rocket Launcher chassis only. An external element must
                // therefore stay native even when the player's terminal has Grenade Launcher mode
                // enabled for this variation. A future API can expose GL templates explicitly.
                if (!string.IsNullOrEmpty(marker.elementId) &&
                    ElementRegistry.TryGet(marker.elementId, out ElementRegistryRecord registration) &&
                    registration.external)
                {
                    registerNative.Invoke(null, new object[] { launcher, ownsSecondary });
                    return;
                }

                switch (marker.element)
                {
                    case ElementId.Fire:
                    case ElementId.Water:
                    case ElementId.Grass:
                    case ElementId.Earth:
                        registerNative.Invoke(null, new object[] { launcher, ownsSecondary });
                        break;

                    case ElementId.Wind:
                    case ElementId.Storm:
                        object palette = CreatePalette(marker.element);
                        registerGrenade.Invoke(null, new[] { (object)launcher, palette, true });
                        break;
                }
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("Grenade Launcher 2.0.2 integration failed for " + marker.choiceName + ": " + Unwrap(exception).Message);
            }
        }


        internal static bool IsTerminalGrenadeModeEnabled(int variation)
        {
            Resolve();
            if (pluginInstanceProperty == null || terminalGrenadeModeMethod == null)
                return false;
            try
            {
                object plugin = pluginInstanceProperty.GetValue(null, null);
                return plugin != null && (bool)terminalGrenadeModeMethod.Invoke(plugin, new object[] { variation });
            }
            catch
            {
                return false;
            }
        }

        internal static void PlayExternalSecondaryAnimation(RocketLauncher launcher)
        {
            if (launcher == null)
                return;
            Resolve();
            if (!available || playExternalSecondaryAnimation == null)
                return;
            try
            {
                playExternalSecondaryAnimation.Invoke(null, new object[] { launcher });
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("Grenade Launcher external alt animation failed: " + Unwrap(exception).Message);
            }
        }

        internal static void SyncExternalSecondaryCooldown(RocketLauncher launcher, float progress)
        {
            if (launcher == null)
                return;
            Resolve();
            if (!available || setExternalSecondaryCooldownProgress == null)
                return;
            try
            {
                setExternalSecondaryCooldownProgress.Invoke(null, new object[] { launcher, Mathf.Clamp01(progress) });
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("Grenade Launcher external cooldown dial sync failed: " + Unwrap(exception).Message);
            }
        }

        internal static void SetPrimaryProjectileAppearance(RocketLauncher launcher, string appearanceName)
        {
            InvokeProjectileAppearance(setExternalPrimaryProjectileAppearance, launcher, appearanceName);
        }

        internal static void SetNextProjectileAppearance(RocketLauncher launcher, string appearanceName)
        {
            InvokeProjectileAppearance(setNextExternalProjectileAppearance, launcher, appearanceName);
        }

        private static void InvokeProjectileAppearance(MethodInfo method, RocketLauncher launcher, string appearanceName)
        {
            if (launcher == null || string.IsNullOrEmpty(appearanceName))
                return;
            Resolve();
            if (!available || projectileAppearanceType == null || method == null)
                return;
            try
            {
                object appearance = Enum.Parse(projectileAppearanceType, appearanceName, true);
                method.Invoke(null, new[] { (object)launcher, appearance });
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("Grenade Launcher projectile appearance integration failed: " + Unwrap(exception).Message);
            }
        }

        private static void Resolve()
        {
            if (resolved)
                return;
            resolved = true;

            integrationType = AccessTools.TypeByName("GrenadeLauncherMod.GrenadeLauncherIntegration");
            paletteType = AccessTools.TypeByName("GrenadeLauncherMod.GrenadeLauncherPaintPalette");
            projectileAppearanceType = AccessTools.TypeByName("GrenadeLauncherMod.GrenadeLauncherProjectileAppearance");
            pluginType = AccessTools.TypeByName("GrenadeLauncherMod.Plugin");
            if (pluginType != null)
            {
                pluginInstanceProperty = pluginType.GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                terminalGrenadeModeMethod = pluginType.GetMethod("IsGrenadeModeEnabled", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, new[] { typeof(int) }, null);
            }
            if (integrationType == null || paletteType == null || projectileAppearanceType == null)
                return;

            registerNative = integrationType.GetMethod(
                "RegisterExternalNativeLauncher",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(RocketLauncher), typeof(bool) },
                null);
            registerGrenadeDefault = integrationType.GetMethod(
                "RegisterExternalLauncher",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(RocketLauncher), typeof(bool) },
                null);

            foreach (MethodInfo method in integrationType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != "RegisterExternalLauncher")
                    continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 3 && parameters[0].ParameterType == typeof(RocketLauncher) &&
                    parameters[1].ParameterType == paletteType && parameters[2].ParameterType == typeof(bool))
                {
                    registerGrenade = method;
                    break;
                }
            }

            playExternalSecondaryAnimation = integrationType.GetMethod(
                "PlayExternalSecondaryAnimation",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(RocketLauncher) },
                null);
            setExternalSecondaryCooldownProgress = integrationType.GetMethod(
                "SetExternalSecondaryCooldownProgress",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(RocketLauncher), typeof(float) },
                null);
            setExternalPrimaryProjectileAppearance = integrationType.GetMethod(
                "SetExternalPrimaryProjectileAppearance",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(RocketLauncher), projectileAppearanceType },
                null);
            setNextExternalProjectileAppearance = integrationType.GetMethod(
                "SetNextExternalProjectileAppearance",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(RocketLauncher), projectileAppearanceType },
                null);

            paletteConstructor = paletteType.GetConstructor(new[] { typeof(Color), typeof(Color), typeof(Color), typeof(Color) });
            available = registerNative != null && registerGrenade != null && registerGrenadeDefault != null && paletteConstructor != null &&
                        playExternalSecondaryAnimation != null && setExternalSecondaryCooldownProgress != null &&
                        setExternalPrimaryProjectileAppearance != null && setNextExternalProjectileAppearance != null;
            if (available)
                Plugin.LogSource?.LogInfo("Grenade Launcher 2.0.2 per-instance integration API detected.");
        }

        private static object CreatePalette(ElementId element)
        {
            ElementDefinition definition = ElementCatalog.Get(element);
            return paletteConstructor.Invoke(new object[]
            {
                definition.grenadeOrange,
                definition.grenadeSilver,
                definition.grenadeBlack,
                definition.grenadeDeepBlack
            });
        }

        private static void WarnUnavailable()
        {
            if (warnedUnavailable)
                return;
            warnedUnavailable = true;
            Plugin.LogSource?.LogWarning(
                "Grenade Launcher 2.0.2 integration API was not found. Elemental rocket launchers may inherit the standalone Grenade Launcher mode until the revised 2.0.2 development build is installed.");
        }

        private static Exception Unwrap(Exception exception)
        {
            return exception is TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : exception;
        }
    }


}
