using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElementalBattlegroundsMod
{
    internal sealed partial class ElementLoadoutMenuRuntime : MonoBehaviour
    {
        private void CreateDirectoryPanel()
        {
            directoryPanel = new GameObject("Weapon Directory", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            directoryPanel.transform.SetParent(frame, false);
            RectTransform rt = directoryPanel.GetComponent<RectTransform>();
            Stretch(rt);
            directoryPanel.GetComponent<Image>().color = new Color(0.015f, 0.02f, 0.028f, 0.995f);

            TMP_Text title = CreateText(rt, "WEAPON DIRECTORY", 34f, TextAlignmentOptions.Center, FontStyles.Bold);
            SetRect(title.rectTransform, new Vector2(800f, 50f), new Vector2(0f, 430f));
            TMP_Text subtitle = CreateText(rt,
                "ELEMENT HOME shows which element contains that vanilla weapon. ASSIGNED SLOT includes both direct vanilla picks and their element equivalent.",
                16f, TextAlignmentOptions.Center, FontStyles.Normal);
            subtitle.color = new Color(0.72f, 0.79f, 0.86f, 1f);
            SetRect(subtitle.rectTransform, new Vector2(1450f, 30f), new Vector2(0f, 390f));

            Button close = CloneNativeButton(rt, "Close Directory", new Vector2(150f, 50f), new Vector2(-790f, 430f));
            SetButtonText(close, "BACK");
            close.onClick.AddListener(() => directoryPanel.SetActive(false));

            GameObject scrollObject = new GameObject("Directory Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(directoryPanel.transform, false);
            RectTransform scrollRt = scrollObject.GetComponent<RectTransform>();
            SetRect(scrollRt, new Vector2(1600f, 790f), new Vector2(0f, -42f));
            scrollObject.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.048f, 0.82f);

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            Stretch(viewport);
            viewport.offsetMin = new Vector2(15f, 12f);
            viewport.offsetMax = new Vector2(-30f, -12f);

            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportObject.transform, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            // A fresh RectTransform starts with a non-zero width. With horizontal stretch
            // anchors that made the directory content wider than its viewport and clipped
            // the first/last characters. Zero width means "exactly match the viewport".
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 35f;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            directoryPanel.SetActive(false);
        }

        private void OpenDirectory()
        {
            if (directoryPanel == null)
                return;
            RebuildDirectoryRows();
            directoryPanel.SetActive(true);
        }

        private void RebuildDirectoryRows()
        {
            Transform content = FindRecursive(directoryPanel.transform, "Content");
            if (content == null)
                return;
            for (int i = content.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(content.GetChild(i).gameObject);

            GameObject columns = new GameObject("Directory Columns", typeof(RectTransform), typeof(LayoutElement));
            columns.transform.SetParent(content, false);
            columns.GetComponent<LayoutElement>().preferredHeight = 24f;
            CreateDirectoryColumnText(columns.transform, "WEAPON", 0f, 0.43f);
            CreateDirectoryColumnText(columns.transform, "ELEMENT HOME", 0.44f, 0.79f);
            CreateDirectoryColumnText(columns.transform, "ASSIGNED SLOT", 0.80f, 1f);

            for (int family = 0; family < 5; family++)
            {
                TMP_Text familyHeader = CreateText(content as RectTransform, FamilyNames[family], 21f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                LayoutElement familyLayout = familyHeader.gameObject.AddComponent<LayoutElement>();
                familyLayout.preferredHeight = 28f;
                familyHeader.margin = new Vector4(12f, 0f, 0f, 0f);

                foreach (LoadoutChoice choice in EBSettings.GetChoices((WeaponFamily)family))
                {
                    if (!choice.IsDirectWeapon)
                        continue;

                    GameObject row = new GameObject("Directory " + choice.id, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                    row.transform.SetParent(content, false);
                    row.GetComponent<Image>().color = new Color(0.065f, 0.08f, 0.105f, 0.95f);
                    row.GetComponent<LayoutElement>().preferredHeight = 46f;
                    RectTransform rowRt = row.GetComponent<RectTransform>();

                    string weaponName = choice.displayName
                        .Replace("Vanilla — ", string.Empty)
                        ;
                    TMP_Text weapon = CreateText(rowRt, weaponName.ToUpperInvariant(), 17f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                    weapon.rectTransform.anchorMin = new Vector2(0f, 0f);
                    weapon.rectTransform.anchorMax = new Vector2(0.43f, 1f);
                    weapon.rectTransform.offsetMin = new Vector2(14f, 2f);
                    weapon.rectTransform.offsetMax = new Vector2(-8f, -2f);
                    weapon.enableWordWrapping = false;
                    weapon.enableAutoSizing = true;
                    weapon.fontSizeMin = 12f;
                    weapon.fontSizeMax = 17f;
                    weapon.overflowMode = TextOverflowModes.Ellipsis;

                    if (WeaponHomeRegistry.TryGet(choice.id, out WeaponHomeInfo home))
                    {
                        Sprite homeSprite = ElementSprite(home.element);
                        if (homeSprite != null)
                        {
                            GameObject iconObject = new GameObject("Element Home Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                            iconObject.transform.SetParent(row.transform, false);
                            RectTransform iconRt = iconObject.GetComponent<RectTransform>();
                            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.44f, 0.5f);
                            iconRt.pivot = new Vector2(0f, 0.5f);
                            iconRt.sizeDelta = new Vector2(34f, 34f);
                            iconRt.anchoredPosition = new Vector2(8f, 0f);
                            Image icon = iconObject.GetComponent<Image>();
                            icon.sprite = homeSprite;
                            icon.preserveAspect = true;
                            icon.raycastTarget = false;
                        }

                        string homeName = ElementName(home.element).ToUpperInvariant();
                        if (home.planned)
                            homeName += "  ·  PLANNED";
                        TMP_Text homeText = CreateText(rowRt, homeName, 16f, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
                        homeText.rectTransform.anchorMin = new Vector2(0.44f, 0f);
                        homeText.rectTransform.anchorMax = new Vector2(0.79f, 1f);
                        homeText.rectTransform.offsetMin = new Vector2(homeSprite != null ? 50f : 12f, 2f);
                        homeText.rectTransform.offsetMax = new Vector2(-8f, -2f);
                        homeText.color = new Color(0.78f, 0.86f, 0.94f, 1f);
                        homeText.enableWordWrapping = false;
                    }
                    else
                    {
                        TMP_Text homeText = CreateText(rowRt, "NOT PLACED YET", 15f, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
                        homeText.rectTransform.anchorMin = new Vector2(0.44f, 0f);
                        homeText.rectTransform.anchorMax = new Vector2(0.79f, 1f);
                        homeText.rectTransform.offsetMin = new Vector2(12f, 2f);
                        homeText.rectTransform.offsetMax = new Vector2(-8f, -2f);
                        homeText.color = new Color(0.62f, 0.67f, 0.72f, 1f);
                        homeText.enableWordWrapping = false;
                    }

                    TMP_Text assigned = CreateText(rowRt, CurrentAssignments(choice),
                        16f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                    assigned.rectTransform.anchorMin = new Vector2(0.80f, 0f);
                    assigned.rectTransform.anchorMax = new Vector2(1f, 1f);
                    assigned.rectTransform.offsetMin = new Vector2(12f, 2f);
                    assigned.rectTransform.offsetMax = new Vector2(-14f, -2f);
                    assigned.color = new Color(0.94f, 0.95f, 0.98f, 1f);
                    assigned.enableWordWrapping = false;
                }
            }
        }

        private void CreateDirectoryColumnText(Transform parent, string text, float minX, float maxX)
        {
            TMP_Text label = CreateText(parent, text, 14f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            label.rectTransform.anchorMin = new Vector2(minX, 0f);
            label.rectTransform.anchorMax = new Vector2(maxX, 1f);
            label.rectTransform.offsetMin = new Vector2(12f, 0f);
            label.rectTransform.offsetMax = new Vector2(-8f, 0f);
            label.color = new Color(0.62f, 0.69f, 0.76f, 1f);
            label.enableWordWrapping = false;
        }

        private string CurrentAssignments(LoadoutChoice directChoice)
        {
            List<string> locations = new List<string>();
            for (int family = 0; family < 5; family++)
            {
                WeaponFamily weaponFamily = (WeaponFamily)family;
                for (int position = 0; position < 3; position++)
                {
                    LoadoutChoice assigned = EBSettings.GetChoice(weaponFamily, position);
                    bool exact = string.Equals(assigned.id, directChoice.id, StringComparison.OrdinalIgnoreCase);
                    bool elementEquivalent = !assigned.IsVanilla &&
                        WeaponHomeRegistry.TryGet(directChoice.id, out WeaponHomeInfo home) &&
                        home.family == weaponFamily &&
                        assigned.element == home.element;
                    if (exact || elementEquivalent)
                        locations.Add(FamilyNames[family] + " " + (position + 1));
                }
            }
            return locations.Count == 0 ? "—" : string.Join(", ", locations.ToArray());
        }


    }
}
