using System;
using System.IO;
using BepInEx;
using UnityEngine;

namespace ElementalBattlegroundsMod
{
    [Serializable]
    internal sealed class WorkingLoadoutData
    {
        public int version = 1;
        public string[] choiceIds = new string[15];
        public string[] displayMirrors = new string[15];
    }

    /// <summary>
    /// Canonical persistence for the active 15-slot EB loadout. PluginConfigurator's display-name
    /// dropdowns are retained as a compatibility/debug mirror, but stable choice IDs live here.
    /// Unknown IDs are preserved so a temporarily missing future addon does not destroy a loadout.
    /// </summary>
    internal static class WorkingLoadoutStore
    {
        internal const int CurrentVersion = 1;
        private static WorkingLoadoutData data;
        private static bool loadedFromDisk;

        private static string FilePath => Path.Combine(Paths.ConfigPath, "ElementalBattlegrounds.loadout.json");

        internal static bool IsLoaded => data != null;
        internal static bool LoadedFromDisk => loadedFromDisk;

        internal static bool LoadIfPresent()
        {
            if (data != null)
                return loadedFromDisk;

            data = CreateEmpty();
            if (!File.Exists(FilePath))
                return false;

            try
            {
                WorkingLoadoutData loaded = JsonUtility.FromJson<WorkingLoadoutData>(File.ReadAllText(FilePath));
                if (loaded != null)
                {
                    data = loaded;
                    NormalizeArrays();
                    loadedFromDisk = true;
                }
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("Could not load EB working loadout IDs: " + exception.Message);
                data = CreateEmpty();
                loadedFromDisk = false;
            }
            return loadedFromDisk;
        }

        internal static void EnsureCreated()
        {
            if (data == null)
                LoadIfPresent();
        }

        internal static string GetChoiceId(WeaponFamily family, int position)
        {
            EnsureCreated();
            int index = FlatIndex(family, position);
            return index >= 0 ? data.choiceIds[index] : null;
        }

        internal static string GetDisplayMirror(WeaponFamily family, int position)
        {
            EnsureCreated();
            int index = FlatIndex(family, position);
            return index >= 0 ? data.displayMirrors[index] : null;
        }

        internal static void Set(WeaponFamily family, int position, string choiceId, string displayMirror, bool save = true)
        {
            EnsureCreated();
            int index = FlatIndex(family, position);
            if (index < 0)
                return;
            data.choiceIds[index] = choiceId;
            data.displayMirrors[index] = displayMirror;
            if (save)
                Save();
        }

        internal static void Save()
        {
            EnsureCreated();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                data.version = CurrentVersion;
                File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("Could not save EB working loadout IDs: " + exception.Message);
            }
        }

        private static WorkingLoadoutData CreateEmpty()
        {
            return new WorkingLoadoutData
            {
                version = CurrentVersion,
                choiceIds = new string[15],
                displayMirrors = new string[15]
            };
        }

        private static void NormalizeArrays()
        {
            if (data.choiceIds == null || data.choiceIds.Length != 15)
            {
                string[] resized = new string[15];
                if (data.choiceIds != null)
                    Array.Copy(data.choiceIds, resized, Math.Min(data.choiceIds.Length, resized.Length));
                data.choiceIds = resized;
            }
            if (data.displayMirrors == null || data.displayMirrors.Length != 15)
            {
                string[] resized = new string[15];
                if (data.displayMirrors != null)
                    Array.Copy(data.displayMirrors, resized, Math.Min(data.displayMirrors.Length, resized.Length));
                data.displayMirrors = resized;
            }
        }

        private static int FlatIndex(WeaponFamily family, int position)
        {
            int familyIndex = (int)family;
            if (familyIndex < 0 || familyIndex >= 5 || position < 0 || position >= 3)
                return -1;
            return familyIndex * 3 + position;
        }
    }
}
