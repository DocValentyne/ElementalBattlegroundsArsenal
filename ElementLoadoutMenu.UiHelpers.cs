using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        private Button CloneNativeButton(Transform parent, string name, Vector2 size, Vector2 position)
        {
            GameObject clone = UnityEngine.Object.Instantiate(nativeButtonTemplate.gameObject, parent);
            clone.name = "EB " + name;
            Button button = clone.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            RectTransform rt = clone.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
            return button;
        }

        private TMP_InputField CreateSearchField(Transform parent, Vector2 size, Vector2 position)
        {
            TMP_InputField field = CreateTextInputField(parent, size, position, "Search elements...");
            field.characterLimit = 40;
            return field;
        }

        private TMP_InputField CreateTextInputField(Transform parent, Vector2 size, Vector2 position, string placeholderText)
        {
            GameObject root = new GameObject("EB Text Input", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            root.transform.SetParent(parent, false);
            RectTransform rt = root.GetComponent<RectTransform>();
            SetRect(rt, size, position);
            Image image = root.GetComponent<Image>();
            image.color = new Color(0.075f, 0.09f, 0.115f, 1f);

            GameObject viewportObject = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            viewportObject.transform.SetParent(root.transform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            Stretch(viewport);
            viewport.offsetMin = new Vector2(12f, 4f);
            viewport.offsetMax = new Vector2(-12f, -4f);

            TMP_Text placeholder = CreateText(viewport, placeholderText, 18f, TextAlignmentOptions.MidlineLeft, FontStyles.Italic);
            Stretch(placeholder.rectTransform);
            placeholder.color = new Color(0.55f, 0.6f, 0.66f, 0.8f);
            placeholder.enableWordWrapping = false;

            TMP_Text text = CreateText(viewport, string.Empty, 18f, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            Stretch(text.rectTransform);
            text.enableWordWrapping = false;

            TMP_InputField field = root.GetComponent<TMP_InputField>();
            field.textViewport = viewport;
            field.textComponent = text as TextMeshProUGUI;
            field.placeholder = placeholder;
            field.lineType = TMP_InputField.LineType.SingleLine;
            return field;
        }

        private TMP_Text CreateText(Transform parent, string text, float fontSize, TextAlignmentOptions alignment, FontStyles style)
        {
            GameObject obj = new GameObject(text.Length > 32 ? "Text" : text, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.fontStyle = style;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = true;
            if (nativeFontAsset != null)
                tmp.font = nativeFontAsset;
            else if (TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
            return tmp;
        }

        private static TMP_Text GetButtonTmpText(Button button)
        {
            if (button == null)
                return null;
            TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
                return tmp;

            Text legacy = button.GetComponentInChildren<Text>(true);
            if (legacy == null)
                return null;

            // Current ULTRAKILL uses TMP for modern menus, but if another UI replacement
            // gives us a legacy Text clone, add a TMP label rather than depending on it.
            legacy.enabled = false;
            GameObject label = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(button.transform, false);
            RectTransform rt = label.GetComponent<RectTransform>();
            Stretch(rt);
            TextMeshProUGUI created = label.GetComponent<TextMeshProUGUI>();
            created.alignment = TextAlignmentOptions.Center;
            created.color = legacy.color;
            created.fontSize = Mathf.Max(14f, legacy.fontSize);
            created.raycastTarget = false;
            return created;
        }

        private static void SetButtonText(Button button, string text)
        {
            TMP_Text tmp = GetButtonTmpText(button);
            if (tmp != null)
            {
                tmp.text = text;
                return;
            }
            Text legacy = button == null ? null : button.GetComponentInChildren<Text>(true);
            if (legacy != null)
                legacy.text = text;
        }

        private static void SetButtonFontSize(Button button, float fontSize)
        {
            TMP_Text tmp = GetButtonTmpText(button);
            if (tmp != null)
            {
                tmp.fontSize = fontSize;
                tmp.enableAutoSizing = false;
            }
            Text legacy = button == null ? null : button.GetComponentInChildren<Text>(true);
            if (legacy != null)
                legacy.fontSize = Mathf.RoundToInt(fontSize);
        }

        private static Button FindButtonNamed(Transform root, string name)
        {
            if (root == null)
                return null;
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
                if (button != null && string.Equals(button.gameObject.name, name, StringComparison.OrdinalIgnoreCase))
                    return button;
            return null;
        }

        private static GameObject FindSceneObject(string name)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            GameObject[] roots;
            try
            {
                roots = scene.GetRootGameObjects();
            }
            catch (ArgumentException)
            {
                // BepInEx can enable the runtime before Unity has a valid active scene.
                // PatchWhenReady will retry on a later frame / sceneLoaded event.
                return null;
            }

            foreach (GameObject root in roots)
            {
                if (root.name == name)
                    return root;
                Transform found = FindRecursive(root.transform, name);
                if (found != null)
                    return found.gameObject;
            }
            return null;
        }

        private static Transform FindRecursive(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindRecursive(root.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rt, Vector2 size, Vector2 position)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
        }

        private static void SetTopRect(RectTransform rt, float height, float y, float x)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(-20f, height);
            rt.anchoredPosition = new Vector2(x, y);
        }

        private static Sprite CreateNoneSprite()
        {
            const int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "EB None Icon";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.33f;
            float ringHalfWidth = size * 0.035f;
            float slashHalfWidth = size * 0.04f;
            Color clear = new Color(1f, 1f, 1f, 0f);
            Color ink = Color.white;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 delta = new Vector2(x, y) - center;
                    float ringDistance = Mathf.Abs(delta.magnitude - radius);
                    // Slash runs bottom-left to top-right. Normalize by sqrt(2) to make
                    // its thickness independent of angle.
                    float slashDistance = Mathf.Abs(delta.y - delta.x) * 0.70710678f;
                    bool inSlashBounds = Mathf.Abs(delta.x) <= radius * 0.85f && Mathf.Abs(delta.y) <= radius * 0.85f;
                    float coverage = 0f;
                    if (ringDistance <= ringHalfWidth + 1.2f)
                        coverage = Mathf.Max(coverage, Mathf.Clamp01(ringHalfWidth + 1.2f - ringDistance));
                    if (inSlashBounds && slashDistance <= slashHalfWidth + 1.2f)
                        coverage = Mathf.Max(coverage, Mathf.Clamp01(slashHalfWidth + 1.2f - slashDistance));
                    texture.SetPixel(x, y, coverage > 0f ? new Color(ink.r, ink.g, ink.b, coverage) : clear);
                }
            }
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite LoadSprite(string resource)
        {
            try
            {
                using (Stream stream = typeof(Plugin).Assembly.GetManifestResourceStream(resource))
                {
                    if (stream == null)
                        return null;
                    byte[] data = new byte[stream.Length];
                    int offset = 0;
                    while (offset < data.Length)
                    {
                        int read = stream.Read(data, offset, data.Length - offset);
                        if (read <= 0)
                            break;
                        offset += read;
                    }
                    if (offset != data.Length)
                        return null;
                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!ImageConversion.LoadImage(texture, data, false))
                    {
                        UnityEngine.Object.Destroy(texture);
                        return null;
                    }
                    texture.name = "EB Menu " + resource;
                    return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("Could not load EB menu icon: " + exception.Message);
                return null;
            }
        }

    }
}
