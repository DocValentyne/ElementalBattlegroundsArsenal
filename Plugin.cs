using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ElementalBattlegroundsMod
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency("com.eternalUnion.pluginConfigurator")]
    [BepInDependency("ali.uk.conductionfix", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("docvalentyne.ultrakill.grenadelauncher", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.github.end-4.thornClient", BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = Api.ElementalBattlegroundsApi.PluginGuid;
        public const string Name = "Elemental Battlegrounds Arsenal";
        public const string Version = "0.1.0";

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource LogSource { get; private set; }
        internal static Harmony Patcher { get; private set; }

        private GameObject runtimeHost;

        private void Awake()
        {
            Instance = this;
            LogSource = Logger;

            EBSettings.Initialize();
            Patcher = new Harmony(Guid);
            Patcher.PatchAll(typeof(Plugin).Assembly);
            WeaponVariantBindsCompat.TryInstall(Patcher);
            StormThornHammerStatsCompat.TryInstall(Patcher);

            runtimeHost = new GameObject("Elemental Battlegrounds Runtime");
            runtimeHost.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(runtimeHost);
            runtimeHost.AddComponent<GlobalCombatRuntime>();
            runtimeHost.AddComponent<LoadoutAutoApplyRuntime>();
            runtimeHost.AddComponent<ElementLoadoutMenuRuntime>();
            runtimeHost.AddComponent<StormThornHammerStatsInstallRetry>();

            Logger.LogInfo(Name + " v" + Version + " loaded.");
        }

        private void OnDestroy()
        {
            Patcher?.UnpatchSelf();
            if (runtimeHost != null)
                Destroy(runtimeHost);
            RuntimeRegistry.Clear();
            if (Instance == this)
                Instance = null;
            if (LogSource == Logger)
                LogSource = null;
        }
    }
}
