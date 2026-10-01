using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnityEngine;

namespace ElementalBattlegroundsMod
{
    [Serializable]
    internal sealed class LoadoutPresetData
    {
        public string name;
        public string[] choices = new string[15];
    }

    [Serializable]
    internal sealed class LoadoutPresetCollection
    {
        public List<LoadoutPresetData> presets = new List<LoadoutPresetData>();
    }

    internal static class LoadoutPresetStore
    {
        private static LoadoutPresetCollection data;
        private static string FilePath => Path.Combine(Paths.ConfigPath, "ElementalBattlegrounds.presets.json");

        internal static IReadOnlyList<LoadoutPresetData> Presets
        {
            get
            {
                EnsureLoaded();
                return data.presets;
            }
        }

        internal static void EnsureLoaded()
        {
            if (data != null)
                return;
            data = new LoadoutPresetCollection();
            try
            {
                if (!File.Exists(FilePath))
                    return;
                LoadoutPresetCollection loaded = JsonUtility.FromJson<LoadoutPresetCollection>(File.ReadAllText(FilePath));
                if (loaded != null && loaded.presets != null)
                {
                    data = loaded;
                    bool migrated = CanonicalizeKnownElementIds();
                    if (migrated)
                        SaveFile();
                }
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("Could not load EB presets: " + exception.Message);
            }
        }

        internal static string SaveNew()
        {
            EnsureLoaded();
            int number = 1;
            string name;
            do
            {
                name = "Loadout " + number++;
            }
            while (Find(name) >= 0);

            data.presets.Add(new LoadoutPresetData { name = name, choices = Capture() });
            SaveFile();
            return name;
        }

        internal static bool SaveCurrent(string name)
        {
            EnsureLoaded();
            int index = Find(name);
            if (index < 0)
                return false;
            data.presets[index].choices = Capture();
            SaveFile();
            return true;
        }

        internal static bool Rename(string oldName, string newName)
        {
            EnsureLoaded();
            int index = Find(oldName);
            if (index < 0)
                return false;

            newName = (newName ?? string.Empty).Trim();
            if (newName.Length == 0)
                return false;

            int existing = Find(newName);
            if (existing >= 0 && existing != index)
                return false;

            data.presets[index].name = newName;
            SaveFile();
            return true;
        }

        internal static bool Load(string name)
        {
            EnsureLoaded();
            int index = Find(name);
            if (index < 0)
                return false;
            string[] values = data.presets[index].choices;
            if (values == null)
                return false;

            for (int family = 0; family < 5; family++)
            {
                for (int position = 0; position < 3; position++)
                {
                    int flat = family * 3 + position;
                    if (flat < values.Length && !string.IsNullOrEmpty(values[flat]))
                        EBSettings.SetChoiceId((WeaponFamily)family, position, values[flat], false);
                }
            }
            return true;
        }

        internal static bool Delete(string name)
        {
            EnsureLoaded();
            int index = Find(name);
            if (index < 0)
                return false;
            data.presets.RemoveAt(index);
            SaveFile();
            return true;
        }


        private static bool CanonicalizeKnownElementIds()
        {
            bool changed = false;
            foreach (LoadoutPresetData preset in data.presets)
            {
                if (preset?.choices == null)
                    continue;
                for (int i = 0; i < preset.choices.Length; i++)
                {
                    string original = preset.choices[i];
                    string canonical = ElementRegistry.Canonicalize(original);
                    if (!string.Equals(original, canonical, StringComparison.Ordinal))
                    {
                        preset.choices[i] = canonical;
                        changed = true;
                    }
                }
            }
            return changed;
        }

        private static string[] Capture()
        {
            string[] result = new string[15];
            for (int family = 0; family < 5; family++)
                for (int position = 0; position < 3; position++)
                    result[family * 3 + position] = EBSettings.GetChoiceId((WeaponFamily)family, position);
            return result;
        }

        private static int Find(string name)
        {
            for (int i = 0; i < data.presets.Count; i++)
                if (string.Equals(data.presets[i].name, name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        private static void SaveFile()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("Could not save EB presets: " + exception.Message);
            }
        }
    }
}
