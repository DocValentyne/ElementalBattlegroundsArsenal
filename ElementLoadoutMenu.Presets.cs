using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ElementalBattlegroundsMod
{
    internal sealed partial class ElementLoadoutMenuRuntime : MonoBehaviour
    {
        private void CreatePresetControls()
        {
            const float rowY = 378f;
            TMP_Text presetLabel = CreateText(frame, "LOADOUT PRESET", 20f, TextAlignmentOptions.Left, FontStyles.Bold);
            SetRect(presetLabel.rectTransform, new Vector2(150f, 30f), new Vector2(-810f, rowY));

            TMP_Dropdown existing = canvasRect.GetComponentInChildren<TMP_Dropdown>(true);
            if (existing != null)
            {
                presetDropdown = UnityEngine.Object.Instantiate(existing, frame);
                presetDropdown.gameObject.name = "EB Preset Dropdown";
                RectTransform rt = presetDropdown.GetComponent<RectTransform>();
                SetRect(rt, new Vector2(250f, 44f), new Vector2(-600f, rowY));
                presetDropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
                presetDropdown.onValueChanged.AddListener(OnPresetSelectionChanged);
            }
            else
            {
                GameObject fallback = new GameObject("EB Preset Dropdown", typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
                fallback.transform.SetParent(frame, false);
                presetDropdown = fallback.GetComponent<TMP_Dropdown>();
                SetRect(fallback.GetComponent<RectTransform>(), new Vector2(250f, 44f), new Vector2(-600f, rowY));
            }

            Button saveNew = CloneNativeButton(frame, "Save New", new Vector2(108f, 44f), new Vector2(-412f, rowY));
            SetButtonText(saveNew, "SAVE NEW");
            SetButtonFontSize(saveNew, 14.5f);
            saveNew.onClick.AddListener(() =>
            {
                selectedPreset = LoadoutPresetStore.SaveNew();
                RefreshPresetDropdown();
                SetStatus("Saved new preset: " + selectedPreset + ".");
            });

            saveCurrentButton = CloneNativeButton(frame, "Save Current", new Vector2(132f, 44f), new Vector2(-286f, rowY));
            SetButtonText(saveCurrentButton, "SAVE CURRENT");
            SetButtonFontSize(saveCurrentButton, 14f);
            saveCurrentButton.onClick.AddListener(() =>
            {
                if (LoadoutPresetStore.SaveCurrent(selectedPreset))
                    SetStatus("Updated preset: " + selectedPreset + ".");
            });

            renameButton = CloneNativeButton(frame, "Rename", new Vector2(100f, 44f), new Vector2(-165f, rowY));
            SetButtonText(renameButton, "RENAME");
            SetButtonFontSize(renameButton, 14.5f);
            renameButton.onClick.AddListener(OpenRenamePreset);

            loadButton = CloneNativeButton(frame, "Load", new Vector2(82f, 44f), new Vector2(-66f, rowY));
            SetButtonText(loadButton, "LOAD");
            SetButtonFontSize(loadButton, 14.5f);
            loadButton.onClick.AddListener(() =>
            {
                if (LoadoutPresetStore.Load(selectedPreset))
                {
                    RefreshAll();
                    SetStatus("Loaded preset into the working loadout. It will take effect next level.");
                }
            });

            deleteButton = CloneNativeButton(frame, "Delete", new Vector2(88f, 44f), new Vector2(24f, rowY));
            SetButtonText(deleteButton, "DELETE");
            SetButtonFontSize(deleteButton, 14.5f);
            deleteButton.onClick.AddListener(() =>
            {
                if (!LoadoutPresetStore.Delete(selectedPreset))
                    return;
                selectedPreset = null;
                RefreshPresetDropdown();
                SetStatus("Preset deleted. The working loadout was not changed.");
            });

            Button terminal = CloneNativeButton(frame, "Terminal", new Vector2(238f, 44f), new Vector2(205f, rowY));
            SetButtonText(terminal, "RESTORE TERMINAL LOADOUT");
            SetButtonFontSize(terminal, 14f);
            terminal.onClick.AddListener(() =>
            {
                EBSettings.RestoreTerminalLoadout();
                RefreshAll();
                SetStatus("Copied the terminal loadout. Vanilla weapons with an implemented element home were assigned through that element. Applies next level.");
            });

            CreateRenamePresetOverlay();
        }

        private void CreateRenamePresetOverlay()
        {
            renameOverlay = new GameObject("Rename Preset Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            renameOverlay.transform.SetParent(frame, false);
            RectTransform overlayRt = renameOverlay.GetComponent<RectTransform>();
            Stretch(overlayRt);
            Image overlayImage = renameOverlay.GetComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.72f);
            overlayImage.raycastTarget = true;

            GameObject box = new GameObject("Rename Box", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            box.transform.SetParent(renameOverlay.transform, false);
            RectTransform boxRt = box.GetComponent<RectTransform>();
            SetRect(boxRt, new Vector2(610f, 225f), Vector2.zero);
            box.GetComponent<Image>().color = new Color(0.035f, 0.045f, 0.06f, 1f);

            TMP_Text title = CreateText(box.transform, "RENAME PRESET", 26f, TextAlignmentOptions.Center, FontStyles.Bold);
            SetRect(title.rectTransform, new Vector2(520f, 36f), new Vector2(0f, 75f));

            renameInput = CreateTextInputField(box.transform, new Vector2(500f, 46f), new Vector2(0f, 22f), "Preset name...");
            renameInput.characterLimit = 64;

            renameMessage = CreateText(box.transform, string.Empty, 14f, TextAlignmentOptions.Center, FontStyles.Normal);
            renameMessage.color = new Color(0.82f, 0.84f, 0.88f, 1f);
            SetRect(renameMessage.rectTransform, new Vector2(500f, 24f), new Vector2(0f, -22f));

            Button confirm = CloneNativeButton(box.transform, "Confirm Rename", new Vector2(150f, 44f), new Vector2(-90f, -72f));
            SetButtonText(confirm, "RENAME");
            SetButtonFontSize(confirm, 15f);
            confirm.onClick.AddListener(ConfirmRenamePreset);

            Button cancel = CloneNativeButton(box.transform, "Cancel Rename", new Vector2(150f, 44f), new Vector2(90f, -72f));
            SetButtonText(cancel, "CANCEL");
            SetButtonFontSize(cancel, 15f);
            cancel.onClick.AddListener(() => renameOverlay.SetActive(false));

            renameOverlay.SetActive(false);
        }

        private void OpenRenamePreset()
        {
            if (renameOverlay == null || string.IsNullOrEmpty(selectedPreset))
                return;

            renameMessage.text = string.Empty;
            renameInput.SetTextWithoutNotify(selectedPreset);
            renameOverlay.transform.SetAsLastSibling();
            renameOverlay.SetActive(true);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(renameInput.gameObject);
                renameInput.ActivateInputField();
                renameInput.Select();
            }
        }

        private void ConfirmRenamePreset()
        {
            if (renameInput == null || string.IsNullOrEmpty(selectedPreset))
                return;

            string requested = (renameInput.text ?? string.Empty).Trim();
            if (requested.Length == 0)
            {
                renameMessage.text = "NAME CANNOT BE EMPTY";
                return;
            }

            string oldName = selectedPreset;
            if (!LoadoutPresetStore.Rename(oldName, requested))
            {
                renameMessage.text = "THAT NAME IS ALREADY IN USE";
                return;
            }

            selectedPreset = requested;
            renameOverlay.SetActive(false);
            RefreshPresetDropdown();
            SetStatus("Renamed preset: " + oldName + " → " + requested + ".");
        }


        private void RefreshPresetDropdown()
        {
            if (presetDropdown == null)
                return;
            IReadOnlyList<LoadoutPresetData> presets = LoadoutPresetStore.Presets;
            List<string> names = new List<string>();
            for (int i = 0; i < presets.Count; i++)
                names.Add(presets[i].name);
            if (names.Count == 0)
                names.Add("<no presets>");

            int target = 0;
            if (!string.IsNullOrEmpty(selectedPreset))
            {
                int found = names.FindIndex(name => string.Equals(name, selectedPreset, StringComparison.OrdinalIgnoreCase));
                if (found >= 0)
                    target = found;
            }
            presetDropdown.ClearOptions();
            presetDropdown.AddOptions(names);
            presetDropdown.SetValueWithoutNotify(target);
            presetDropdown.RefreshShownValue();
            selectedPreset = presets.Count > 0 ? names[target] : null;

            bool hasPreset = presets.Count > 0;
            if (saveCurrentButton != null) saveCurrentButton.interactable = hasPreset;
            if (renameButton != null) renameButton.interactable = hasPreset;
            if (loadButton != null) loadButton.interactable = hasPreset;
            if (deleteButton != null) deleteButton.interactable = hasPreset;
        }

        private void OnPresetSelectionChanged(int index)
        {
            IReadOnlyList<LoadoutPresetData> presets = LoadoutPresetStore.Presets;
            if (index >= 0 && index < presets.Count)
                selectedPreset = presets[index].name;
        }


    }
}
