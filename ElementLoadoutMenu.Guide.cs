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
        private void CreateElementGuidePanel()
        {
            elementGuidePanel = new GameObject("Element Guide", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            elementGuidePanel.transform.SetParent(frame, false);
            RectTransform rt = elementGuidePanel.GetComponent<RectTransform>();
            Stretch(rt);
            elementGuidePanel.GetComponent<Image>().color = new Color(0.012f, 0.017f, 0.024f, 0.998f);

            Button back = CloneNativeButton(rt, "Close Element Guide", new Vector2(150f, 50f), new Vector2(-790f, 430f));
            SetButtonText(back, "BACK");
            SetButtonFontSize(back, 17f);
            back.onClick.AddListener(() => elementGuidePanel.SetActive(false));

            GameObject iconObject = new GameObject("Element Guide Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(elementGuidePanel.transform, false);
            RectTransform iconRt = iconObject.GetComponent<RectTransform>();
            SetRect(iconRt, new Vector2(76f, 76f), new Vector2(-500f, 420f));
            elementGuideIcon = iconObject.GetComponent<Image>();
            elementGuideIcon.preserveAspect = true;
            elementGuideIcon.raycastTarget = false;

            elementGuideTitle = CreateText(rt, "ELEMENT", 38f, TextAlignmentOptions.Center, FontStyles.Bold);
            SetRect(elementGuideTitle.rectTransform, new Vector2(820f, 50f), new Vector2(0f, 432f));

            elementGuideStatus = CreateText(rt, string.Empty, 16f, TextAlignmentOptions.Center, FontStyles.Bold);
            elementGuideStatus.color = new Color(0.66f, 0.74f, 0.82f, 1f);
            SetRect(elementGuideStatus.rectTransform, new Vector2(820f, 28f), new Vector2(0f, 395f));

            elementGuideModeButton = CloneNativeButton(rt, "Element Guide Mode", new Vector2(245f, 50f), new Vector2(730f, 430f));
            SetButtonText(elementGuideModeButton, "ADVANCED EXPLANATION");
            SetButtonFontSize(elementGuideModeButton, 14f);
            elementGuideModeButton.onClick.AddListener(ToggleElementGuideMode);

            GameObject scrollObject = new GameObject("Element Guide Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(elementGuidePanel.transform, false);
            RectTransform scrollRt = scrollObject.GetComponent<RectTransform>();
            SetRect(scrollRt, new Vector2(1600f, 780f), new Vector2(0f, -45f));
            scrollObject.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.048f, 0.82f);

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            Stretch(viewport);
            viewport.offsetMin = new Vector2(24f, 18f);
            viewport.offsetMax = new Vector2(-34f, -18f);

            elementGuideText = CreateText(viewport, string.Empty, 19f, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            RectTransform textRt = elementGuideText.rectTransform;
            textRt.anchorMin = new Vector2(0f, 1f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.pivot = new Vector2(0.5f, 1f);
            textRt.anchoredPosition = Vector2.zero;
            textRt.sizeDelta = new Vector2(0f, 1200f);
            elementGuideText.margin = new Vector4(12f, 10f, 18f, 16f);
            elementGuideText.enableWordWrapping = true;
            elementGuideText.richText = true;
            elementGuideText.color = new Color(0.94f, 0.96f, 0.99f, 1f);

            elementGuideScroll = scrollObject.GetComponent<ScrollRect>();
            elementGuideScroll.viewport = viewport;
            elementGuideScroll.content = textRt;
            elementGuideScroll.horizontal = false;
            elementGuideScroll.vertical = true;
            elementGuideScroll.scrollSensitivity = 36f;
            elementGuideScroll.movementType = ScrollRect.MovementType.Clamped;

            elementGuidePanel.SetActive(false);
        }

        internal void OpenElementGuide(string stableId, ElementId element, string displayName)
        {
            if (elementGuidePanel == null)
                return;

            elementGuideId = stableId;
            elementGuideElement = element;
            elementGuideName = string.IsNullOrEmpty(displayName) ? ElementName(element) : displayName;
            elementGuideAdvanced = false;
            RefreshElementGuide();
            if (directoryPanel != null)
                directoryPanel.SetActive(false);
            elementGuidePanel.transform.SetAsLastSibling();
            elementGuidePanel.SetActive(true);
        }

        private void ToggleElementGuideMode()
        {
            if (!GuideHasAdvanced(elementGuideId, elementGuideElement))
                return;
            elementGuideAdvanced = !elementGuideAdvanced;
            RefreshElementGuide();
        }

        private static bool GuideHasAdvanced(string stableId, ElementId element)
        {
            if (!string.IsNullOrEmpty(stableId) && ElementRegistry.TryGet(stableId, out ElementRegistryRecord record) && record.external)
                return !string.IsNullOrWhiteSpace(record.advancedGuide);
            return element == ElementId.Fire ||
                   element == ElementId.Water ||
                   element == ElementId.Grass ||
                   element == ElementId.Wind ||
                   element == ElementId.Storm ||
                   element == ElementId.Earth;
        }

        private void RefreshElementGuide()
        {
            if (elementGuidePanel == null || elementGuideText == null)
                return;

            elementGuideTitle.text = (elementGuideName ?? ElementName(elementGuideElement)).ToUpperInvariant();

            ElementRegistryRecord guideRecord = null;
            bool external = !string.IsNullOrEmpty(elementGuideId) &&
                            ElementRegistry.TryGet(elementGuideId, out guideRecord) && guideRecord.external;
            bool implemented = external ? guideRecord.selectable : GuideHasAdvanced(elementGuideId, elementGuideElement);
            bool planned = false;
            if (elementGuideElement == ElementId.None && !external)
                elementGuideStatus.text = "EMPTY SLOT OPTION";
            else if (external && implemented)
                elementGuideStatus.text = string.IsNullOrEmpty(guideRecord.author)
                    ? "ADDON ELEMENT — LEFT-CLICK TO ASSIGN"
                    : "ADDON ELEMENT — BY " + guideRecord.author.ToUpperInvariant() + " — LEFT-CLICK TO ASSIGN";
            else if (implemented)
                elementGuideStatus.text = "IMPLEMENTED — LEFT-CLICK TO ASSIGN";
            else if (planned)
                elementGuideStatus.text = "PLANNED — NOT SELECTABLE YET";
            else if (elementGuideElement == ElementId.Sans)
                elementGuideStatus.text = "PLANNED — CHEAT EXCLUSIVE";
            else
                elementGuideStatus.text = "NOT IMPLEMENTED YET";

            Sprite sprite = elementGuideElement == ElementId.None && !external
                ? noneSprite
                : ElementSprite(elementGuideId, elementGuideElement);
            elementGuideIcon.sprite = sprite;
            elementGuideIcon.enabled = sprite != null;
            elementGuideIcon.color = implemented || (elementGuideElement == ElementId.None && !external)
                ? Color.white
                : new Color(0.62f, 0.66f, 0.70f, 0.85f);

            bool canAdvanced = GuideHasAdvanced(elementGuideId, elementGuideElement);
            elementGuideModeButton.interactable = canAdvanced;
            SetButtonText(elementGuideModeButton,
                canAdvanced
                    ? (elementGuideAdvanced ? "SIMPLE EXPLANATION" : "ADVANCED EXPLANATION")
                    : "ADVANCED — N/A");

            elementGuideText.text = BuildElementGuideText(elementGuideId, elementGuideElement, elementGuideAdvanced);
            elementGuideText.ForceMeshUpdate();
            float height = Mathf.Max(735f, elementGuideText.preferredHeight + 36f);
            elementGuideText.rectTransform.sizeDelta = new Vector2(0f, height);
            elementGuideText.rectTransform.anchoredPosition = Vector2.zero;
            if (elementGuideScroll != null)
                elementGuideScroll.verticalNormalizedPosition = 1f;
        }

        private static string BuildElementGuideText(string stableId, ElementId element, bool advanced)
        {
            if (!string.IsNullOrEmpty(stableId) && ElementRegistry.TryGet(stableId, out ElementRegistryRecord record) && record.external)
            {
                if (advanced && !string.IsNullOrWhiteSpace(record.advancedGuide))
                    return record.advancedGuide;
                if (!string.IsNullOrWhiteSpace(record.simpleGuide))
                    return record.simpleGuide;
                return "No guide text was provided by this element addon.";
            }
            return ElementGuideContent.Build(element, advanced);
        }


    }
}
