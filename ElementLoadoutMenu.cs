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
        private sealed class ElementEntry
        {
            internal string id;
            internal string name;
            internal ElementId element;
            internal bool selectable;
            internal string iconResource;
            internal string author;
            internal bool external;
        }

        private static readonly string[] FamilyNames = { "REVOLVER", "CLOSE", "RAPID", "ULTIMATE", "EXPLOSIVE" };

        private static ElementLoadoutMenuRuntime instance;
        private static readonly FieldInfo CheatConsentScreen = AccessTools.Field(typeof(CheatsController), "consentScreen");

        private RectTransform canvasRect;
        private GameObject pauseMenu;
        private Button nativeButtonTemplate;
        private GameObject pauseButton;
        private GameObject panel;
        private GameObject directoryPanel;
        private GameObject elementGuidePanel;
        private TMP_Text elementGuideTitle;
        private TMP_Text elementGuideStatus;
        private TMP_Text elementGuideText;
        private Image elementGuideIcon;
        private Button elementGuideModeButton;
        private ScrollRect elementGuideScroll;
        private ElementId elementGuideElement = ElementId.None;
        private string elementGuideId;
        private string elementGuideName;
        private bool elementGuideAdvanced;
        private RectTransform frame;
        private TMP_Dropdown presetDropdown;
        private TMP_InputField searchInput;
        private TMP_Text editingText;
        private TMP_Text statusText;
        private Button applyButton;
        private Button cheatButton;
        private Button saveCurrentButton;
        private Button renameButton;
        private Button loadButton;
        private Button deleteButton;
        private GameObject renameOverlay;
        private TMP_InputField renameInput;
        private TMP_Text renameMessage;
        private readonly Button[,] slotButtons = new Button[5, 3];
        private readonly TMP_Text[,] slotButtonTexts = new TMP_Text[5, 3];
        private readonly TMP_Text[,] slotButtonVanillaBadges = new TMP_Text[5, 3];
        private readonly Image[,] slotButtonIcons = new Image[5, 3];
        private readonly List<GameObject> elementButtonObjects = new List<GameObject>();
        private readonly List<ElementEntry> elementButtonEntries = new List<ElementEntry>();
        private RectTransform elementGrid;
        private RectTransform directGrid;
        private RectTransform directHeaderRect;
        private RectTransform selectorContent;
        private readonly List<GameObject> directButtonObjects = new List<GameObject>();
        private readonly Dictionary<string, Sprite> elementSprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private Sprite menuIcon;
        private Sprite noneSprite;
        private TMP_FontAsset nativeFontAsset;
        private int selectedFamily;
        private int selectedPosition;
        private bool menuOpen;
        private Coroutine patchRoutine;
        private string selectedPreset;
        private Vector2 lastCanvasSize = new Vector2(-1f, -1f);

        internal static void OpenFromAnywhere()
        {
            if (!EBSettings.Enabled)
                return;
            if (instance == null)
                instance = UnityEngine.Object.FindObjectOfType<ElementLoadoutMenuRuntime>();
            if (instance != null)
                instance.StartCoroutine(instance.OpenWhenReady());
        }

        private void Awake()
        {
            instance = this;
            LoadoutPresetStore.EnsureLoaded();
            menuIcon = LoadSprite("ElementalBattlegroundsMod.icon.png");
            noneSprite = CreateNoneSprite();

            // Icons are keyed by stable registry IDs rather than ElementId ordinals. This is
            // required for future addon elements whose identity cannot live in the built-in enum.
            foreach (ElementRegistryRecord element in ElementRegistry.Entries)
                GetOrLoadElementSprite(element.stableId, element.iconResource);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            ElementRegistry.Changed += OnElementRegistryChanged;
            QueuePatch();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            ElementRegistry.Changed -= OnElementRegistryChanged;
            if (patchRoutine != null)
                StopCoroutine(patchRoutine);
            patchRoutine = null;
            menuOpen = false;
            if (instance == this)
                instance = null;
        }

        private void Update()
        {
            SyncEnabledAvailability();

            if (frame != null && canvasRect != null)
            {
                Vector2 currentCanvasSize = canvasRect.rect.size;
                if ((currentCanvasSize - lastCanvasSize).sqrMagnitude > 0.25f)
                    FitFrameToCanvas();
            }

            if (!menuOpen)
                return;

            UpdateCheatState();
            InputManager input = MonoSingleton<InputManager>.Instance;
            if (input != null && input.InputSource.Pause.WasPerformedThisFrame)
                CloseToPause();
        }

        private void SyncEnabledAvailability()
        {
            bool enabled = EBSettings.Enabled;
            EBSettings.SyncLoadoutMenuAvailability();

            if (pauseButton != null && pauseButton.activeSelf != enabled)
                pauseButton.SetActive(enabled);

            if (!enabled && (menuOpen || (panel != null && panel.activeSelf)))
                CloseToPause();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            canvasRect = null;
            pauseMenu = null;
            nativeButtonTemplate = null;
            pauseButton = null;
            panel = null;
            directoryPanel = null;
            elementGuidePanel = null;
            menuOpen = false;
            lastCanvasSize = new Vector2(-1f, -1f);
            QueuePatch();
        }

        private void QueuePatch()
        {
            if (patchRoutine != null)
                StopCoroutine(patchRoutine);
            patchRoutine = StartCoroutine(PatchWhenReady());
        }

        private IEnumerator PatchWhenReady()
        {
            for (int attempt = 0; attempt < 120; attempt++)
            {
                if (TryPatchPauseMenu())
                {
                    patchRoutine = null;
                    yield break;
                }
                yield return null;
            }
            patchRoutine = null;
        }

        private IEnumerator OpenWhenReady()
        {
            if (!EBSettings.Enabled)
                yield break;

            if (panel == null)
            {
                QueuePatch();
                for (int i = 0; i < 120 && panel == null && EBSettings.Enabled; i++)
                    yield return null;
            }
            if (panel != null && EBSettings.Enabled)
                Open();
        }

        private bool TryPatchPauseMenu()
        {
            if (panel != null && pauseButton != null)
                return true;

            CanvasController canvasController = UnityEngine.Object.FindObjectOfType<CanvasController>();
            GameObject canvasObject = canvasController != null ? canvasController.gameObject : FindSceneObject("Canvas");
            if (canvasObject == null)
                return false;
            canvasRect = canvasObject.GetComponent<RectTransform>();
            if (canvasRect == null)
                return false;

            Transform pauseTransform = FindRecursive(canvasObject.transform, "PauseMenu");
            Transform resumeTransform = pauseTransform == null ? null : FindRecursive(pauseTransform, "Resume");
            if (pauseTransform == null || resumeTransform == null)
                return false;

            pauseMenu = pauseTransform.gameObject;
            nativeButtonTemplate = resumeTransform.GetComponent<Button>();
            if (nativeButtonTemplate == null)
                return false;

            TMP_Text nativeLabel = nativeButtonTemplate.GetComponentInChildren<TMP_Text>(true);
            if (nativeLabel != null && nativeLabel.font != null)
                nativeFontAsset = nativeLabel.font;

            CreatePauseButton();
            CreatePanel();
            return true;
        }

        private void CreatePauseButton()
        {
            if (pauseButton != null)
                return;

            Button optionsButton = FindButtonNamed(pauseMenu.transform, "Options") ?? nativeButtonTemplate;
            pauseButton = UnityEngine.Object.Instantiate(nativeButtonTemplate.gameObject, pauseMenu.transform);
            pauseButton.name = "EBElementLoadoutButton";
            Button button = pauseButton.GetComponent<Button>();
            pauseButton.SetActive(EBSettings.Enabled);
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(Open);
            SetButtonText(button, string.Empty);
            if (menuIcon != null)
            {
                GameObject iconObject = new GameObject("EB Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(pauseButton.transform, false);
                RectTransform iconRt = iconObject.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.18f, 0.18f);
                iconRt.anchorMax = new Vector2(0.82f, 0.82f);
                iconRt.offsetMin = Vector2.zero;
                iconRt.offsetMax = Vector2.zero;
                Image icon = iconObject.GetComponent<Image>();
                icon.sprite = menuIcon;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }

            RectTransform rt = pauseButton.GetComponent<RectTransform>();
            RectTransform optionsRt = optionsButton.GetComponent<RectTransform>();
            float height = Mathf.Max(42f, optionsRt.rect.height > 1f ? optionsRt.rect.height : optionsRt.sizeDelta.y);
            rt.sizeDelta = new Vector2(height, height);
            Vector2 basePosition = optionsRt.anchoredPosition;
            float optionWidth = optionsRt.rect.width > 1f ? optionsRt.rect.width : Mathf.Abs(optionsRt.sizeDelta.x);
            float x = basePosition.x + optionWidth * 0.5f + height * 0.5f + 6f;
            Vector2 candidate = new Vector2(x, basePosition.y);

            // Configgy, Thorn and other mods may also put square controls beside Options.
            // Stay close to the native menu instead of wandering ever farther to the right:
            // try one slot right, one left, then fan outward symmetrically.
            float step = height + 6f;
            Vector2 rightBase = candidate;
            Vector2 leftBase = new Vector2(basePosition.x - optionWidth * 0.5f - height * 0.5f - 6f, basePosition.y);
            Vector2 chosen = rightBase;
            bool found = false;
            for (int ring = 0; ring < 4 && !found; ring++)
            {
                Vector2[] candidates =
                {
                    new Vector2(rightBase.x + ring * step, rightBase.y),
                    new Vector2(leftBase.x - ring * step, leftBase.y)
                };
                foreach (Vector2 test in candidates)
                {
                    bool occupied = false;
                    foreach (Button other in pauseMenu.GetComponentsInChildren<Button>(true))
                    {
                        if (other == null || other.gameObject == pauseButton || other == optionsButton ||
                            other.transform.parent != pauseButton.transform.parent)
                            continue;
                        RectTransform otherRt = other.GetComponent<RectTransform>();
                        if (otherRt == null)
                            continue;
                        if (Vector2.Distance(otherRt.anchoredPosition, test) < height * 0.8f)
                        {
                            occupied = true;
                            break;
                        }
                    }
                    if (!occupied)
                    {
                        chosen = test;
                        found = true;
                        break;
                    }
                }
            }
            rt.anchoredPosition = chosen;
        }

        private void CreatePanel()
        {
            if (panel != null)
                return;

            panel = new GameObject("EB Element Loadout Menu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(canvasRect, false);
            RectTransform panelRt = panel.GetComponent<RectTransform>();
            Stretch(panelRt);
            Image panelImage = panel.GetComponent<Image>();
            // Keep the native pause menu alive behind this panel so ULTRAKILL's pause
            // GameState remains registered (and therefore keeps the mouse unlocked). The
            // EB panel is deliberately opaque and blocks raycasts to the menu underneath.
            panelImage.color = new Color(0.015f, 0.02f, 0.028f, 1f);

            GameObject frameObject = new GameObject("Frame", typeof(RectTransform));
            frameObject.transform.SetParent(panel.transform, false);
            frame = frameObject.GetComponent<RectTransform>();
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.sizeDelta = new Vector2(1780f, 990f);
            frame.anchoredPosition = Vector2.zero;
            FitFrameToCanvas();

            TMP_Text title = CreateText(frame, "ELEMENT LOADOUT", 40f, TextAlignmentOptions.Center, FontStyles.Bold);
            SetRect(title.rectTransform, new Vector2(850f, 54f), new Vector2(0f, 438f));

            Button back = CloneNativeButton(frame, "Back", new Vector2(150f, 50f), new Vector2(-795f, 438f));
            SetButtonText(back, "BACK");
            SetButtonFontSize(back, 17f);
            back.onClick.AddListener(CloseToPause);

            Button directory = CloneNativeButton(frame, "Directory", new Vector2(205f, 50f), new Vector2(765f, 438f));
            SetButtonText(directory, "WEAPON DIRECTORY");
            SetButtonFontSize(directory, 16f);
            directory.onClick.AddListener(OpenDirectory);

            cheatButton = CloneNativeButton(frame, "Cheats", new Vector2(160f, 48f), new Vector2(565f, 378f));
            SetButtonFontSize(cheatButton, 15f);
            cheatButton.onClick.AddListener(OpenNativeCheatConsent);
            applyButton = CloneNativeButton(frame, "Apply", new Vector2(210f, 48f), new Vector2(760f, 378f));
            SetButtonFontSize(applyButton, 16f);
            applyButton.onClick.AddListener(ApplyNow);

            CreatePresetControls();
            CreateSlotCards();

            editingText = CreateText(frame, "EDITING: REVOLVER 1", 23f, TextAlignmentOptions.Left, FontStyles.Bold);
            SetRect(editingText.rectTransform, new Vector2(790f, 32f), new Vector2(-430f, 78f));

            statusText = CreateText(frame,
                "Changes edit the working loadout immediately and apply automatically on the next level. Mid-level Apply requires cheats.",
                18f, TextAlignmentOptions.Center, FontStyles.Normal);
            statusText.color = new Color(0.74f, 0.80f, 0.86f, 1f);
            SetRect(statusText.rectTransform, new Vector2(1500f, 26f), new Vector2(0f, 46f));

            searchInput = CreateSearchField(frame, new Vector2(360f, 38f), new Vector2(685f, 12f));
            searchInput.onValueChanged.AddListener(_ => RefreshElementFilter());

            CreateSelectorScroll();
            CreateDirectoryPanel();
            CreateElementGuidePanel();

            panel.SetActive(false);
            RefreshAll();
        }

        private void CreateSlotCards()
        {
            float startX = -708f;
            const float columnSpacing = 355f;
            for (int family = 0; family < 5; family++)
            {
                float x = startX + family * columnSpacing;
                TMP_Text header = CreateText(frame, FamilyNames[family], 23f, TextAlignmentOptions.Center, FontStyles.Bold);
                SetRect(header.rectTransform, new Vector2(310f, 30f), new Vector2(x, 306f));

                for (int position = 0; position < 3; position++)
                {
                    int capturedFamily = family;
                    int capturedPosition = position;
                    Button button = CloneNativeButton(frame, "Slot " + family + " " + position,
                        new Vector2(320f, 55f), new Vector2(x, 245f - position * 62f));
                    button.onClick.AddListener(() => SelectSlot(capturedFamily, capturedPosition));
                    slotButtons[family, position] = button;

                    TMP_Text nativeLabel = GetButtonTmpText(button);
                    if (nativeLabel != null)
                    {
                        SetRect(nativeLabel.rectTransform, new Vector2(205f, 52f), new Vector2(38f, 0f));
                        nativeLabel.fontSize = 19f;
                        nativeLabel.alignment = TextAlignmentOptions.MidlineLeft;
                        nativeLabel.margin = Vector4.zero;
                        nativeLabel.enableWordWrapping = false;
                        nativeLabel.fontStyle = FontStyles.Normal;
                        nativeLabel.overflowMode = TextOverflowModes.Ellipsis;
                    }
                    slotButtonTexts[family, position] = nativeLabel;

                    TMP_Text vanillaBadge = CreateText(button.transform, "(VANILLA)", 11.5f,
                        TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                    SetRect(vanillaBadge.rectTransform, new Vector2(86f, 24f), new Vector2(112f, 0f));
                    vanillaBadge.color = new Color(0.68f, 0.73f, 0.79f, 1f);
                    vanillaBadge.enableWordWrapping = false;
                    vanillaBadge.gameObject.SetActive(false);
                    slotButtonVanillaBadges[family, position] = vanillaBadge;

                    TMP_Text number = CreateText(button.transform, (position + 1).ToString(), 16f,
                        TextAlignmentOptions.Center, FontStyles.Bold);
                    SetRect(number.rectTransform, new Vector2(28f, 52f), new Vector2(-145f, 0f));

                    GameObject iconObject = new GameObject("Assignment Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    iconObject.transform.SetParent(button.transform, false);
                    RectTransform iconRt = iconObject.GetComponent<RectTransform>();
                    SetRect(iconRt, new Vector2(38f, 38f), new Vector2(-112f, 0f));
                    Image icon = iconObject.GetComponent<Image>();
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                    icon.enabled = false;
                    slotButtonIcons[family, position] = icon;
                }
            }
        }

        private void CreateSelectorScroll()
        {
            GameObject scrollObject = new GameObject("Selector Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(frame, false);
            RectTransform scrollRt = scrollObject.GetComponent<RectTransform>();
            SetRect(scrollRt, new Vector2(1660f, 430f), new Vector2(0f, -235f));
            scrollObject.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.048f, 0.82f);

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(12f, 10f);
            viewport.offsetMax = new Vector2(-28f, -10f);

            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewport, false);
            selectorContent = contentObject.GetComponent<RectTransform>();
            selectorContent.anchorMin = new Vector2(0f, 1f);
            selectorContent.anchorMax = new Vector2(1f, 1f);
            selectorContent.pivot = new Vector2(0.5f, 1f);
            selectorContent.anchoredPosition = Vector2.zero;
            selectorContent.sizeDelta = new Vector2(0f, 730f);

            TMP_Text elementsHeader = CreateText(selectorContent, "ELEMENTS", 21f, TextAlignmentOptions.Left, FontStyles.Bold);
            SetTopRect(elementsHeader.rectTransform, 32f, -6f, 10f);
            TMP_Text elementsHint = CreateText(selectorContent, "RIGHT-CLICK AN ELEMENT FOR WEAPON DETAILS", 14f,
                TextAlignmentOptions.Right, FontStyles.Normal);
            elementsHint.color = new Color(0.58f, 0.65f, 0.72f, 1f);
            SetTopRect(elementsHint.rectTransform, 30f, -8f, -10f);

            GameObject elementGridObject = new GameObject("Element Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            elementGridObject.transform.SetParent(selectorContent, false);
            elementGrid = elementGridObject.GetComponent<RectTransform>();
            elementGrid.anchorMin = elementGrid.anchorMax = new Vector2(0f, 1f);
            elementGrid.pivot = new Vector2(0f, 1f);
            elementGrid.anchoredPosition = new Vector2(8f, -46f);
            elementGrid.sizeDelta = new Vector2(1585f, 430f);
            GridLayoutGroup grid = elementGridObject.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(190f, 76f);
            grid.spacing = new Vector2(9f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 8;
            grid.childAlignment = TextAnchor.UpperLeft;

            RebuildElementButtonsFromRegistry();

            TMP_Text directHeader = CreateText(selectorContent, "VANILLA / GRENADE LAUNCHER WEAPONS", 21f, TextAlignmentOptions.Left, FontStyles.Bold);
            directHeaderRect = directHeader.rectTransform;
            SetTopRect(directHeaderRect, 32f, -490f, 10f);

            GameObject directGridObject = new GameObject("Direct Weapon Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            directGridObject.transform.SetParent(selectorContent, false);
            directGrid = directGridObject.GetComponent<RectTransform>();
            directGrid.anchorMin = directGrid.anchorMax = new Vector2(0f, 1f);
            directGrid.pivot = new Vector2(0f, 1f);
            directGrid.anchoredPosition = new Vector2(8f, -532f);
            directGrid.sizeDelta = new Vector2(1585f, 180f);
            GridLayoutGroup directLayout = directGridObject.GetComponent<GridLayoutGroup>();
            directLayout.cellSize = new Vector2(255f, 72f);
            directLayout.spacing = new Vector2(9f, 10f);
            directLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            directLayout.constraintCount = 6;
            directLayout.childAlignment = TextAnchor.UpperLeft;

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = selectorContent;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 34f;

            GameObject scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarObject.transform.SetParent(scrollObject.transform, false);
            RectTransform scrollbarRt = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRt.anchorMin = new Vector2(1f, 0f);
            scrollbarRt.anchorMax = new Vector2(1f, 1f);
            scrollbarRt.pivot = new Vector2(1f, 0.5f);
            scrollbarRt.sizeDelta = new Vector2(14f, 0f);
            scrollbarRt.anchoredPosition = new Vector2(-5f, 0f);
            scrollbarObject.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.13f, 0.9f);

            GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObject.transform.SetParent(scrollbarObject.transform, false);
            RectTransform handleRt = handleObject.GetComponent<RectTransform>();
            Stretch(handleRt);
            handleObject.GetComponent<Image>().color = new Color(0.55f, 0.62f, 0.70f, 0.9f);
            Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.handleRect = handleRt;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            RebuildDirectButtons();
        }

        private void OnElementRegistryChanged()
        {
            RebuildElementButtonsFromRegistry();
            if (elementGuidePanel != null && elementGuidePanel.activeSelf &&
                !string.IsNullOrEmpty(elementGuideId) && !string.Equals(elementGuideId, "none", StringComparison.OrdinalIgnoreCase))
            {
                if (ElementRegistry.TryGet(elementGuideId, out _))
                    RefreshElementGuide();
                else
                    elementGuidePanel.SetActive(false);
            }
            if (menuOpen)
                RefreshAll();
        }

        private void RebuildElementButtonsFromRegistry()
        {
            if (elementGrid == null)
                return;
            foreach (GameObject old in elementButtonObjects)
            {
                if (old != null)
                    UnityEngine.Object.Destroy(old);
            }
            elementButtonObjects.Clear();
            elementButtonEntries.Clear();
            foreach (ElementEntry entry in BuildElementEntries())
                CreateElementButton(entry);
            RefreshElementFilter();
        }

        private List<ElementEntry> BuildElementEntries()
        {
            List<ElementEntry> entries = new List<ElementEntry>
            {
                new ElementEntry
                {
                    id = "none",
                    name = "None",
                    element = ElementId.None,
                    selectable = true,
                    iconResource = null,
                    author = null,
                    external = false
                }
            };

            // Keep usable elements together at the top. External registrations are appended to
            // the registry after startup, so a raw registry order would bury them below every
            // not-yet-implemented official element.
            for (int pass = 0; pass < 2; pass++)
            {
                bool wantSelectable = pass == 0;
                foreach (ElementRegistryRecord element in ElementRegistry.Entries)
                {
                    if (element.selectable != wantSelectable)
                        continue;
                    entries.Add(new ElementEntry
                    {
                        id = element.stableId,
                        name = element.displayName,
                        element = element.builtInId,
                        selectable = element.selectable,
                        iconResource = element.iconResource,
                        author = element.author,
                        external = element.external
                    });
                }
            }
            return entries;
        }

        private void CreateElementButton(ElementEntry entry)
        {
            Button button = CloneNativeButton(elementGrid, "Element " + entry.name, new Vector2(190f, 76f), Vector2.zero);
            TMP_Text label = GetButtonTmpText(button);
            if (label != null)
            {
                if (entry.external && entry.selectable && !string.IsNullOrEmpty(entry.author))
                {
                    label.text = entry.name.ToUpperInvariant() + "\nBY " + entry.author.ToUpperInvariant();
                    label.fontSize = 14f;
                }
                else
                {
                    label.text = entry.selectable ? entry.name.ToUpperInvariant() : entry.name.ToUpperInvariant() + "\nNOT IMPLEMENTED";
                    label.fontSize = entry.selectable ? 18f : 14f;
                }
                label.alignment = TextAlignmentOptions.Center;
            }

            Sprite entrySprite = entry.id == "none"
                ? noneSprite
                : ElementSprite(entry.id, entry.element);
            if (label != null)
                label.margin = entrySprite != null ? new Vector4(52f, 2f, 4f, 2f) : new Vector4(4f, 2f, 4f, 2f);

            if (entrySprite != null)
            {
                GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(button.transform, false);
                RectTransform iconRt = iconObject.GetComponent<RectTransform>();
                iconRt.anchorMin = iconRt.anchorMax = new Vector2(0f, 0.5f);
                iconRt.pivot = new Vector2(0f, 0.5f);
                iconRt.sizeDelta = new Vector2(48f, 48f);
                iconRt.anchoredPosition = new Vector2(8f, 0f);
                Image icon = iconObject.GetComponent<Image>();
                icon.sprite = entrySprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.color = entry.selectable ? Color.white : new Color(0.28f, 0.30f, 0.32f, 0.55f);
            }

            button.interactable = entry.selectable;
            if (entry.selectable)
            {
                string choiceId = entry.id;
                button.onClick.AddListener(() => AssignChoice(choiceId));
            }

            ElementGuidePointerHandler guideHandler = button.gameObject.AddComponent<ElementGuidePointerHandler>();
            guideHandler.runtime = this;
            guideHandler.elementId = entry.id;
            guideHandler.element = entry.element;
            guideHandler.displayName = entry.name;

            elementButtonObjects.Add(button.gameObject);
            elementButtonEntries.Add(entry);
        }

        private void RebuildDirectButtons()
        {
            foreach (GameObject old in directButtonObjects)
                if (old != null)
                    UnityEngine.Object.Destroy(old);
            directButtonObjects.Clear();

            foreach (LoadoutChoice choice in EBSettings.GetChoices((WeaponFamily)selectedFamily))
            {
                if (!choice.IsDirectWeapon)
                    continue;
                LoadoutChoice captured = choice;
                Button button = CloneNativeButton(directGrid, choice.id, new Vector2(255f, 72f), Vector2.zero);
                string text = choice.displayName.Replace("Vanilla — ", string.Empty).Replace("Grenade Launcher — ", "GL — ");
                bool requiresGl = choice.explosiveMode == ExplosiveIntegrationMode.ForceGrenade;
                if (requiresGl && !GrenadeLauncherCompat.Available)
                    text += "\nGL 2.0.2 REQUIRED";
                SetButtonText(button, text.ToUpperInvariant());
                button.interactable = !requiresGl || GrenadeLauncherCompat.Available;
                TMP_Text label = GetButtonTmpText(button);
                if (label != null)
                {
                    label.fontSize = 15f;
                    label.enableWordWrapping = true;
                }
                button.onClick.AddListener(() =>
                {
                    WeaponFamily family = (WeaponFamily)selectedFamily;
                    if (!TryAssignElementHomeForDirectWeapon(captured, family))
                        EBSettings.SetChoice(family, selectedPosition, captured, true);
                    RefreshAll();
                    SetStatus("Working loadout changed. It will take effect automatically next level.");
                });
                directButtonObjects.Add(button.gameObject);
            }

            int count = directButtonObjects.Count;
            int rows = Mathf.Max(1, Mathf.CeilToInt(count / 6f));
            directGrid.sizeDelta = new Vector2(directGrid.sizeDelta.x, rows * 82f);
            selectorContent.sizeDelta = new Vector2(0f, 535f + rows * 82f + 16f);
        }

        private bool TryAssignElementHomeForDirectWeapon(LoadoutChoice directChoice, WeaponFamily family)
        {
            if (!WeaponHomeRegistry.TryResolveImplementedElementChoice(family, directChoice, out LoadoutChoice elementChoice))
                return false;

            EBSettings.SetChoice(family, selectedPosition, elementChoice, true);
            return true;
        }

        private void Open()
        {
            if (!EBSettings.Enabled || panel == null)
                return;

            OptionsManager options = MonoSingleton<OptionsManager>.Instance;
            if (options != null && !options.paused && !options.mainMenu)
                options.Pause();
            if (options != null)
                options.dontUnpause = true;

            EBSettings.EnsureWorkingLoadoutInitialized();
            FitFrameToCanvas();
            panel.SetActive(true);
            // Do NOT deactivate PauseMenu here. OptionsManager's "pause" GameState tracks
            // that object; disabling it made GameStateManager discard the pause state and
            // relock/hide the cursor. This opaque full-screen panel blocks the menu beneath.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (directoryPanel != null)
                directoryPanel.SetActive(false);
            if (elementGuidePanel != null)
                elementGuidePanel.SetActive(false);
            menuOpen = true;
            RefreshAll();
            if (EventSystem.current != null && slotButtons[selectedFamily, selectedPosition] != null)
                EventSystem.current.SetSelectedGameObject(slotButtons[selectedFamily, selectedPosition].gameObject);
        }

        private void CloseToPause()
        {
            if (!menuOpen && (panel == null || !panel.activeSelf))
                return;
            if (renameOverlay != null && renameOverlay.activeSelf)
            {
                renameOverlay.SetActive(false);
                return;
            }
            if (elementGuidePanel != null && elementGuidePanel.activeSelf)
            {
                elementGuidePanel.SetActive(false);
                return;
            }
            if (directoryPanel != null && directoryPanel.activeSelf)
            {
                directoryPanel.SetActive(false);
                return;
            }

            menuOpen = false;
            if (panel != null)
                panel.SetActive(false);
            OptionsManager options = MonoSingleton<OptionsManager>.Instance;
            if (options != null)
            {
                // Keep this guard through the rest of the current frame. If this method was
                // reached from the Pause/Escape input before OptionsManager.Update runs, clearing
                // it immediately would make the same keypress close EB *and* unpause the game.
                options.dontUnpause = true;
                StartCoroutine(ReleaseDontUnpauseNextFrame(options));
            }
            if (pauseMenu != null)
                pauseMenu.SetActive(true);
        }

        private IEnumerator ReleaseDontUnpauseNextFrame(OptionsManager options)
        {
            yield return null;
            if (options != null && !menuOpen)
                options.dontUnpause = false;
        }

        private void SelectSlot(int family, int position)
        {
            selectedFamily = Mathf.Clamp(family, 0, 4);
            selectedPosition = Mathf.Clamp(position, 0, 2);
            RefreshAll();
        }

        private void AssignChoice(string choiceId)
        {
            if (!EBSettings.SetChoiceId((WeaponFamily)selectedFamily, selectedPosition, choiceId, true))
                return;
            RefreshAll();
            SetStatus("Working loadout changed. It will take effect automatically next level.");
        }

        private void ApplyNow()
        {
            CheatsController cheats = MonoSingleton<CheatsController>.Instance;
            if (cheats == null || !cheats.cheatsEnabled)
            {
                SetStatus("Mid-level loadout swapping is disabled without cheats. Your changes will apply next level.");
                return;
            }
            GunSetter setter = UnityEngine.Object.FindObjectOfType<GunSetter>();
            if (setter == null)
            {
                SetStatus("No active weapon loadout is available in this scene.");
                return;
            }
            setter.ResetWeapons(false);
            SetStatus("Working loadout applied immediately because cheats are enabled.");
        }

        private void OpenNativeCheatConsent()
        {
            CheatsController cheats = MonoSingleton<CheatsController>.Instance;
            if (cheats == null || cheats.cheatsEnabled)
                return;
            GameObject consent = CheatConsentScreen?.GetValue(cheats) as GameObject;
            if (consent == null)
            {
                SetStatus("Could not find ULTRAKILL's cheat consent screen. Use the Konami code normally.");
                return;
            }

            // The Konami-code path pauses the game and then shows this exact native consent
            // screen. We are already paused, so hand control back to the pause canvas and
            // show the same screen rather than bypassing ULTRAKILL's confirmation.
            menuOpen = false;
            panel.SetActive(false);
            OptionsManager options = MonoSingleton<OptionsManager>.Instance;
            if (options != null)
                options.dontUnpause = false;
            if (pauseMenu != null)
                pauseMenu.SetActive(true);
            consent.SetActive(true);
        }

        private void RefreshAll()
        {
            RefreshSlots();
            RefreshSelectedSummary();
            RebuildDirectButtons();
            RefreshPresetDropdown();
            RefreshElementFilter();
            UpdateCheatState();
        }

        private void RefreshSlots()
        {
            for (int family = 0; family < 5; family++)
            {
                WeaponFamily weaponFamily = (WeaponFamily)family;
                for (int position = 0; position < 3; position++)
                {
                    LoadoutChoice choice = EBSettings.GetChoice(weaponFamily, position);
                    bool vanillaElement = IsElementVanillaAssignment(choice, weaponFamily);

                    TMP_Text text = slotButtonTexts[family, position];
                    if (text != null)
                    {
                        text.text = SlotAssignmentDisplay(choice);
                        text.fontStyle = family == selectedFamily && position == selectedPosition ? FontStyles.Bold : FontStyles.Normal;
                        if (choice.IsVanilla)
                        {
                            SetRect(text.rectTransform, new Vector2(205f, 52f), new Vector2(38f, 0f));
                            text.fontSize = 15f;
                            text.enableWordWrapping = true;
                            text.overflowMode = TextOverflowModes.Ellipsis;
                        }
                        else
                        {
                            SetRect(text.rectTransform,
                                new Vector2(vanillaElement ? 132f : 205f, 52f),
                                new Vector2(vanillaElement ? 2f : 38f, 0f));
                            text.fontSize = 19f;
                            text.enableWordWrapping = false;
                            text.overflowMode = TextOverflowModes.Ellipsis;
                        }
                    }

                    TMP_Text vanillaBadge = slotButtonVanillaBadges[family, position];
                    if (vanillaBadge != null)
                        vanillaBadge.gameObject.SetActive(vanillaElement);

                    Image assignmentIcon = slotButtonIcons[family, position];
                    if (assignmentIcon != null)
                    {
                        Sprite sprite = ChoiceIcon(choice);
                        assignmentIcon.sprite = sprite;
                        assignmentIcon.enabled = sprite != null;
                        assignmentIcon.color = Color.white;
                    }

                    Button button = slotButtons[family, position];
                    if (button != null)
                    {
                        ColorBlock colors = button.colors;
                        colors.colorMultiplier = family == selectedFamily && position == selectedPosition ? 1.35f : 1f;
                        button.colors = colors;
                    }
                }
            }
        }

        private void RefreshSelectedSummary()
        {
            if (editingText != null)
                editingText.text = "EDITING: " + FamilyNames[selectedFamily] + " " + (selectedPosition + 1);
        }

        private string SlotAssignmentDisplay(LoadoutChoice choice)
        {
            if (choice.IsNone)
                return "NONE";
            if (choice.IsMissing)
                return choice.displayName.ToUpperInvariant();
            if (choice.IsVanilla)
            {
                return choice.displayName
                    .Replace("Vanilla — ", string.Empty)
                    .Replace("Grenade Launcher — ", "GL — ")
                    .ToUpperInvariant();
            }
            return choice.displayName.ToUpperInvariant();
        }

        private Sprite ChoiceIcon(LoadoutChoice choice)
        {
            if (choice.IsNone || choice.IsMissing)
                return noneSprite;
            if (choice.IsVanilla)
                return null;
            return ElementSprite(choice.id, choice.element);
        }

        private Sprite ElementSprite(ElementId element)
        {
            string stableId = ElementRegistry.StableId(element);
            return ElementSprite(stableId, element);
        }

        private Sprite ElementSprite(string stableId, ElementId builtInFallback = ElementId.None)
        {
            if (!string.IsNullOrEmpty(stableId) && ElementRegistry.TryGet(stableId, out ElementRegistryRecord record))
                return record.iconSprite != null ? record.iconSprite : GetOrLoadElementSprite(record.stableId, record.iconResource);
            if (builtInFallback != ElementId.None && ElementRegistry.TryGet(builtInFallback, out record))
                return record.iconSprite != null ? record.iconSprite : GetOrLoadElementSprite(record.stableId, record.iconResource);
            return null;
        }

        private Sprite GetOrLoadElementSprite(string stableId, string resourceName)
        {
            if (string.IsNullOrEmpty(stableId) || string.IsNullOrEmpty(resourceName))
                return null;
            if (elementSprites.TryGetValue(stableId, out Sprite existing))
                return existing;
            Sprite loaded = LoadSprite(resourceName);
            elementSprites[stableId] = loaded;
            return loaded;
        }

        private static string ElementName(ElementId element)
        {
            return ElementRegistry.DisplayName(element);
        }

        private static bool IsElementVanillaAssignment(LoadoutChoice choice, WeaponFamily family)
        {
            return !choice.IsNone &&
                   !choice.IsMissing &&
                   !choice.IsVanilla &&
                   WeaponHomeRegistry.HasElementHome(family, choice.element);
        }

        private string AssignmentDisplay(LoadoutChoice choice, WeaponFamily family)
        {
            if (choice.IsNone)
                return "NONE";
            if (choice.IsMissing)
                return choice.displayName.ToUpperInvariant();
            if (choice.IsVanilla)
                return choice.displayName.Replace("Vanilla — ", string.Empty).ToUpperInvariant();
            return choice.displayName.ToUpperInvariant() + " — " + GetRegisteredWeaponName(choice.id, choice.element, family).ToUpperInvariant();
        }

        internal static string GetRegisteredWeaponName(string stableId, ElementId element, WeaponFamily family)
        {
            if (!string.IsNullOrEmpty(stableId) && ElementRegistry.TryGet(stableId, out ElementRegistryRecord record) &&
                record.external && record.TryGetWeapon(family, out ExternalElementWeaponRecord externalWeapon))
                return externalWeapon.displayName;

            switch (element)
            {
                case ElementId.Fire:
                    switch (family)
                    {
                        case WeaponFamily.Revolver: return "Fire Column Revolver";
                        case WeaponFamily.Shotgun: return "Counter Shotgun";
                        case WeaponFamily.Rapid: return "Overheat Nailgun";
                        case WeaponFamily.Ultimate: return "Inferno Ultimate";
                        case WeaponFamily.Explosive: return "Firestarter Rocket Launcher";
                    }
                    break;
                case ElementId.Water:
                    switch (family)
                    {
                        case WeaponFamily.Revolver: return "Piercer Revolver";
                        case WeaponFamily.Shotgun: return "Water Jackhammer";
                        case WeaponFamily.Rapid: return "Attractor Nailgun";
                        case WeaponFamily.Ultimate: return "Water Dragon Ultimate";
                        case WeaponFamily.Explosive: return "Geyser Launcher";
                    }
                    break;
                case ElementId.Grass:
                    switch (family)
                    {
                        case WeaponFamily.Revolver: return "Vine Revolver";
                        case WeaponFamily.Shotgun: return "Cyclone Shotgun";
                        case WeaponFamily.Rapid: return "Poison Nailgun";
                        case WeaponFamily.Ultimate: return "Spore Bombardment Ultimate";
                        case WeaponFamily.Explosive: return "Grass Rocket Launcher";
                    }
                    break;
                case ElementId.Wind:
                    switch (family)
                    {
                        case WeaponFamily.Revolver: return "Pressure Revolver";
                        case WeaponFamily.Shotgun: return "Updraft Shotgun";
                        case WeaponFamily.Rapid: return "Tempest Sawblade Launcher";
                        case WeaponFamily.Ultimate: return "Grand Cyclone Ultimate";
                        case WeaponFamily.Explosive: return "Slipstream Grenade Launcher";
                    }
                    break;
                case ElementId.Storm:
                    switch (family)
                    {
                        case WeaponFamily.Revolver: return "Storm Standard Revolver";
                        case WeaponFamily.Shotgun: return "Flashstep Jackhammer";
                        case WeaponFamily.Rapid: return "Jumpstart Nailgun";
                        case WeaponFamily.Ultimate: return "Electric Railcannon";
                        case WeaponFamily.Explosive: return "Thunderline Grenade Launcher";
                    }
                    break;
                case ElementId.Earth:
                    switch (family)
                    {
                        case WeaponFamily.Revolver: return "Faultline Revolver";
                        case WeaponFamily.Shotgun: return "Stoneguard Jackhammer";
                        case WeaponFamily.Rapid: return "Rock Sawblade Launcher";
                        case WeaponFamily.Ultimate: return "Meteor Shower Ultimate";
                        case WeaponFamily.Explosive: return "S.R.S. Cannon";
                    }
                    break;
            }
            return "Element Weapon";
        }

        private void RefreshElementFilter()
        {
            // This method can be reached while Unity is rebuilding/destroying UI children
            // (especially when opening the panel immediately after a scene transition).
            // Treat any incomplete piece as "not ready yet" instead of throwing and leaving
            // the whole loadout menu half-refreshed.
            if (elementGrid == null || directGrid == null || selectorContent == null)
                return;

            string query = string.Empty;
            if (searchInput != null)
            {
                try { query = (searchInput.text ?? string.Empty).Trim(); }
                catch { query = string.Empty; }
            }

            int visible = 0;
            int count = Mathf.Min(elementButtonObjects.Count, elementButtonEntries.Count);
            for (int i = 0; i < count; i++)
            {
                GameObject buttonObject = elementButtonObjects[i];
                ElementEntry entry = elementButtonEntries[i];
                if (buttonObject == null || entry == null)
                    continue;
                string entryName = entry.name ?? string.Empty;
                string entryAuthor = entry.author ?? string.Empty;
                string entryId = entry.id ?? string.Empty;
                bool show = string.IsNullOrEmpty(query) ||
                            entryName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            entryAuthor.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            entryId.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
                buttonObject.SetActive(show);
                if (show) visible++;
            }

            int rows = Mathf.Max(1, Mathf.CeilToInt(visible / 8f));
            float height = rows * 86f;
            elementGrid.sizeDelta = new Vector2(elementGrid.sizeDelta.x, height);
            directGrid.anchoredPosition = new Vector2(8f, -(102f + height));
            if (directHeaderRect != null)
                directHeaderRect.anchoredPosition = new Vector2(10f, -(60f + height));
            int directRows = Mathf.Max(1, Mathf.CeilToInt(directButtonObjects.Count / 6f));
            selectorContent.sizeDelta = new Vector2(0f, 125f + height + directRows * 82f);
        }

        private void UpdateCheatState()
        {
            CheatsController cheats = MonoSingleton<CheatsController>.Instance;
            bool enabled = cheats != null && cheats.cheatsEnabled;
            if (applyButton != null)
            {
                applyButton.interactable = enabled;
                SetButtonText(applyButton, enabled ? "APPLY NOW" : "APPLY NOW — CHEATS REQUIRED");
            }
            if (cheatButton != null)
            {
                cheatButton.interactable = !enabled;
                SetButtonText(cheatButton, enabled ? "CHEATS ENABLED" : "ENABLE CHEATS");
            }
        }

        private void FitFrameToCanvas()
        {
            if (frame == null || canvasRect == null)
                return;

            Vector2 canvasSize = canvasRect.rect.size;
            if (canvasSize.x <= 1f || canvasSize.y <= 1f)
                return;

            // The menu is authored at 1780x990, then uniformly fitted into the actual
            // ULTRAKILL canvas. This avoids assuming that canvas units equal physical pixels
            // (the cause of the 0.0.19 clipping at 1440p) while keeping one predictable layout.
            const float designWidth = 1780f;
            const float designHeight = 990f;
            const float screenFill = 0.96f;
            float scale = Mathf.Min(canvasSize.x / designWidth, canvasSize.y / designHeight) * screenFill;
            scale = Mathf.Max(0.1f, scale);
            frame.localScale = Vector3.one * scale;
            frame.anchoredPosition = Vector2.zero;
            lastCanvasSize = canvasSize;
        }

    }

}
