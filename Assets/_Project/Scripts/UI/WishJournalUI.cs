using System.Collections.Generic;
using System.Text;
using AliGame.Core;
using AliGame.Data;
using AliGame.Items;
using AliGame.Movement;
using AliGame.Wishes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AliGame.UI
{
    /// <summary>
    /// The wish journal: who asked for what, and how close you are to being able to deliver. J opens and closes it,
    /// Esc closes. Every size is a field, and with Preview In Editor on the panel is drawn in the Game view without
    /// pressing Play (the preview is never saved in the scene).
    /// </summary>
    [ExecuteAlways]
    public class WishJournalUI : MonoBehaviour
    {
        private const string CanvasName = "JournalCanvas";
        private const string EnoughColor = "#4E8B3A";
        private const string MissingColor = "#B0442E";

        [Header("General")]
        [SerializeField] private WishJournal journal;
        [SerializeField] private Inventory inventory;
        [SerializeField] private Font font;
        [SerializeField, Min(0.5f)] private float uiScale = 1.45f;
        [SerializeField] private float animationDuration = 0.22f;

        [Header("Panel")]
        [SerializeField, Min(200f)] private float panelWidth = 640f;
        [SerializeField] private float panelPadding = 28f;
        [SerializeField] private float headerHeight = 64f;
        [SerializeField] private float headerGap = 16f;
        [SerializeField, Min(8)] private int titleSize = 32;

        [Header("Entries")]
        [SerializeField, Min(40f)] private float rowHeight = 142f;
        [SerializeField] private float rowGap = 12f;
        [SerializeField] private float trayPadding = 16f;
        [SerializeField, Min(0f)] private float portraitSize = 84f;
        [SerializeField, Min(8)] private int wishTitleSize = 26;
        [SerializeField, Min(8)] private int noteSize = 19;
        [SerializeField, Min(8)] private int requirementSize = 20;

        [Header("Editor preview")]
        [Tooltip("Draws the journal in the Game view outside Play mode so you can adjust it. Never saved in the scene.")]
        [SerializeField] private bool previewInEditor = true;
        [SerializeField] private DialogueCharacterSO previewCharacter;
        [SerializeField, Range(0, 4)] private int previewEntries = 2;

        private GameObject _root;
        private CanvasGroup _group;
        private RectTransform _window;
        private RectTransform _tray;
        private Text _countText;
        private Font _font;
        private bool _open;
        private float _t;
        private bool _rebuildQueued;
        private PlayerMovement2D _player;
        private bool _playerLocked;

        public bool IsOpen => _open;

        private void Awake()
        {
            if (!Application.isPlaying) return;

            DestroyBuilt();
            if (journal == null) journal = FindFirstObjectByType<WishJournal>();
            if (inventory == null) inventory = FindFirstObjectByType<Inventory>();
            _player = inventory != null ? inventory.GetComponent<PlayerMovement2D>() : null;

            _font = UIStyle.ResolveFont(font);
            EnsureEventSystem();
            BuildShell();
            ApplyAnimation();
            _root.SetActive(false);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                RebuildPreview();
                return;
            }

            UIPanels.Opened += OnOtherPanelOpened;
            if (journal != null) journal.Changed += RefreshIfOpen;
            if (inventory != null) inventory.Changed += RefreshIfOpen;
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                DestroyBuilt();
                return;
            }

            UIPanels.Opened -= OnOtherPanelOpened;
            if (journal != null) journal.Changed -= RefreshIfOpen;
            if (inventory != null) inventory.Changed -= RefreshIfOpen;
            SetPlayerLocked(false);
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (Application.isPlaying || _rebuildQueued) return;

            _rebuildQueued = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                _rebuildQueued = false;
                if (this != null) RebuildPreview();
            };
#endif
        }

        [ContextMenu("Refresh preview")]
        private void RebuildPreview()
        {
            if (Application.isPlaying) return;

            DestroyBuilt();
            if (!previewInEditor || !isActiveAndEnabled) return;

            _font = UIStyle.ResolveFont(font);
            BuildShell();
            _open = true;
            _t = 1f;
            ApplyAnimation();
            _root.SetActive(true);
            BuildPreviewEntries();

            foreach (Transform child in transform)
            {
                if (child.name == CanvasName) SetPreviewFlags(child);
            }

            Canvas.ForceUpdateCanvases();
#if UNITY_EDITOR
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
#endif
        }

        private static void SetPreviewFlags(Transform root)
        {
            root.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            foreach (Transform child in root) SetPreviewFlags(child);
        }

        private void DestroyBuilt()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name == CanvasName) DestroyImmediate(child.gameObject);
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (GameInput.JournalPressed) Toggle();
            else if (_open && GameInput.CancelPressed) Close();

            float target = _open ? 1f : 0f;
            if (Mathf.Approximately(_t, target)) return;

            _t = Mathf.MoveTowards(_t, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, animationDuration));
            ApplyAnimation();
            if (!_open && _t <= 0f) _root.SetActive(false);
        }

        public void Toggle() => SetOpen(!_open);

        public void Open() => SetOpen(true);

        public void Close() => SetOpen(false);

        private void SetOpen(bool open)
        {
            _open = open;
            _group.blocksRaycasts = open;
            _group.interactable = open;
            SetPlayerLocked(open);

            if (!open) return;

            UIPanels.NotifyOpened(this);
            _root.SetActive(true);
            Refresh();
        }

        private void OnOtherPanelOpened(object panel)
        {
            if (panel != (object)this && _open) Close();
        }

        private void SetPlayerLocked(bool locked)
        {
            if (_player == null || _playerLocked == locked) return;

            _playerLocked = locked;
            if (locked) _player.LockInput();
            else _player.UnlockInput();
        }

        private void RefreshIfOpen()
        {
            if (_open) Refresh();
        }

        private void Refresh()
        {
            ClearEntries();
            if (journal == null) return;

            List<(NpcStorySO Story, WishSO Wish)> active = journal.Model.ActiveWishes();
            _countText.text = active.Count.ToString();

            if (active.Count == 0)
            {
                BuildEmptyState("Ninguém pediu nada ainda.\nConverse com as pessoas do restaurante.");
                ResizePanel(1);
                return;
            }

            for (int i = 0; i < active.Count; i++)
            {
                DialogueCharacterSO character = active[i].Story.Character;
                BuildRow(i, character, active[i].Wish.Title, active[i].Wish.Note, RequirementText(active[i].Wish));
            }
            ResizePanel(active.Count);
        }

        private string RequirementText(WishSO wish)
        {
            var text = new StringBuilder();
            foreach (ItemAmount requirement in wish.Requirements)
            {
                if (requirement.Item == null) continue;

                int have = inventory != null ? inventory.Count(requirement.Item) : 0;
                bool enough = have >= requirement.Amount;

                if (text.Length > 0) text.Append("    ");
                text.Append("<color=").Append(enough ? EnoughColor : MissingColor).Append('>')
                    .Append(requirement.Item.DisplayName).Append(' ').Append(have).Append('/').Append(requirement.Amount)
                    .Append("</color>");
            }
            return text.ToString();
        }

        private void BuildPreviewEntries()
        {
            _countText.text = previewEntries.ToString();

            if (previewEntries == 0)
            {
                BuildEmptyState("Ninguém pediu nada ainda.\nConverse com as pessoas do restaurante.");
                ResizePanel(1);
                return;
            }

            for (int i = 0; i < previewEntries; i++)
            {
                string requirements = "<color=" + (i == 0 ? EnoughColor : MissingColor) + ">Item de exemplo " + (i == 0 ? "1/1" : "0/2") + "</color>";
                BuildRow(i, previewCharacter, "Desejo de exemplo " + (i + 1), "A razão pela qual esse item importa para essa pessoa.", requirements);
            }
            ResizePanel(previewEntries);
        }

        private void ClearEntries()
        {
            for (int i = _tray.childCount - 1; i >= 0; i--)
                DestroyImmediate(_tray.GetChild(i).gameObject);
        }

        private void ResizePanel(int rows)
        {
            float traySize = rows * rowHeight + Mathf.Max(0, rows - 1) * rowGap + trayPadding * 2f;
            _tray.sizeDelta = new Vector2(panelWidth - panelPadding * 2f, traySize);

            var panel = (RectTransform)_tray.parent;
            panel.sizeDelta = new Vector2(panelWidth, panelPadding * 2f + headerHeight + headerGap + traySize);
        }

        private void BuildEmptyState(string message)
        {
            Text empty = UIStyle.CreateText(_tray, "Empty", _font, noteSize + 3, FontStyle.Normal, UIStyle.TextSoft, TextAnchor.MiddleCenter);
            UIStyle.Stretch(empty.rectTransform, trayPadding, trayPadding, trayPadding, trayPadding);
            empty.text = message;
        }

        private void BuildRow(int index, DialogueCharacterSO character, string title, string note, string requirements)
        {
            var rowObject = new GameObject("Wish " + (index + 1), typeof(RectTransform));
            rowObject.transform.SetParent(_tray, false);
            var rowRect = (RectTransform)rowObject.transform;
            rowRect.anchorMin = rowRect.anchorMax = rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.sizeDelta = new Vector2(panelWidth - panelPadding * 2f - trayPadding * 2f, rowHeight);
            rowRect.anchoredPosition = new Vector2(0f, -(trayPadding + index * (rowHeight + rowGap)));

            Image background = UIStyle.CreateImage(rowRect, "Background", UIStyle.Rounded(18), UIStyle.SlotFilled);
            UIStyle.Stretch((RectTransform)background.transform);
            UIStyle.AddShadowBehind((RectTransform)background.transform, 8f, new Vector2(0f, -3f), 0.3f);

            Image tile = UIStyle.CreateImage(rowRect, "PortraitTile", UIStyle.Rounded(16), UIStyle.Tray);
            var tileRect = (RectTransform)tile.transform;
            tileRect.anchorMin = tileRect.anchorMax = tileRect.pivot = new Vector2(0f, 0.5f);
            tileRect.sizeDelta = new Vector2(portraitSize, portraitSize);
            tileRect.anchoredPosition = new Vector2(14f, 0f);

            if (character != null && character.Portrait != null)
            {
                Image portrait = UIStyle.CreateImage(tileRect, "Portrait", null, Color.white);
                portrait.sprite = character.Portrait;
                portrait.preserveAspect = true;
                UIStyle.Stretch((RectTransform)portrait.transform, 6f, 6f, 6f, 6f);
            }
            else
            {
                Text initial = UIStyle.CreateText(tileRect, "Initial", _font, wishTitleSize + 4, FontStyle.Bold, UIStyle.TextDark, TextAnchor.MiddleCenter);
                UIStyle.Stretch(initial.rectTransform);
                string displayName = character != null ? character.DisplayName : "?";
                initial.text = displayName.Length > 0 ? displayName.Substring(0, 1).ToUpperInvariant() : "?";
            }

            // Title on top, requirements pinned to the bottom, and the note fills whatever is left between them.
            float textLeft = portraitSize + 28f;
            const float edge = 12f;
            const float gap = 4f;
            float titleHeight = wishTitleSize * 1.45f;
            float requirementHeight = requirementSize * 1.6f;

            Text titleText = UIStyle.CreateText(rowRect, "Title", _font, wishTitleSize, FontStyle.Bold, UIStyle.TextDark, TextAnchor.UpperLeft);
            var titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(textLeft, -(edge + titleHeight));
            titleRect.offsetMax = new Vector2(-16f, -edge);
            titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleText.text = character != null ? character.DisplayName + " — " + title : title;

            Text noteText = UIStyle.CreateText(rowRect, "Note", _font, noteSize, FontStyle.Normal, UIStyle.TextSoft, TextAnchor.UpperLeft);
            var noteRect = noteText.rectTransform;
            noteRect.anchorMin = new Vector2(0f, 0f);
            noteRect.anchorMax = new Vector2(1f, 1f);
            noteRect.offsetMin = new Vector2(textLeft, edge + requirementHeight + gap);
            noteRect.offsetMax = new Vector2(-16f, -(edge + titleHeight + gap));
            noteText.text = note;

            Text requirementText = UIStyle.CreateText(rowRect, "Requirements", _font, requirementSize, FontStyle.Bold, UIStyle.TextSoft, TextAnchor.LowerLeft);
            requirementText.supportRichText = true;
            requirementText.horizontalOverflow = HorizontalWrapMode.Overflow;
            var requirementRect = requirementText.rectTransform;
            requirementRect.anchorMin = new Vector2(0f, 0f);
            requirementRect.anchorMax = new Vector2(1f, 0f);
            requirementRect.pivot = new Vector2(0.5f, 0f);
            requirementRect.offsetMin = new Vector2(textLeft, edge);
            requirementRect.offsetMax = new Vector2(-16f, edge + requirementHeight);
            requirementText.text = requirements;
        }

        private void ApplyAnimation()
        {
            float eased = _open ? EaseOutBack(_t) : _t * _t * (3f - 2f * _t);
            _group.alpha = Mathf.Clamp01(_t * 1.4f);
            _window.localScale = Vector3.one * Mathf.LerpUnclamped(0.88f, 1f, eased);
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private void BuildShell()
        {
            var canvasObject = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.sortingOrder = 100;
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = mainCamera;
                canvas.planeDistance = 1f;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f) / uiScale;
            scaler.matchWidthOrHeight = 0.5f;

            _root = new GameObject("Root", typeof(RectTransform), typeof(CanvasGroup));
            _root.transform.SetParent(canvasObject.transform, false);
            UIStyle.Stretch((RectTransform)_root.transform);
            _group = _root.GetComponent<CanvasGroup>();

            Image backdrop = UIStyle.CreateImage(_root.transform, "Backdrop", null, UIStyle.Backdrop);
            backdrop.raycastTarget = true;
            UIStyle.Stretch((RectTransform)backdrop.transform);
            backdrop.gameObject.AddComponent<Button>().onClick.AddListener(Close);

            var windowObject = new GameObject("Window", typeof(RectTransform));
            windowObject.transform.SetParent(_root.transform, false);
            _window = (RectTransform)windowObject.transform;
            UIStyle.Stretch(_window);

            Image panel = UIStyle.CreateImage(_window, "Panel", UIStyle.Rounded(28), UIStyle.Panel);
            panel.raycastTarget = true;
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(panelWidth, 300f);
            UIStyle.AddShadowBehind(panelRect, 40f, new Vector2(0f, -12f), 0.5f);

            BuildHeader(panelRect);

            Image tray = UIStyle.CreateImage(panelRect, "Tray", UIStyle.Rounded(22), UIStyle.Tray);
            _tray = (RectTransform)tray.transform;
            _tray.anchorMin = _tray.anchorMax = _tray.pivot = new Vector2(0.5f, 1f);
            _tray.anchoredPosition = new Vector2(0f, -(panelPadding + headerHeight + headerGap));
            ResizePanel(1);
        }

        private void BuildHeader(RectTransform panel)
        {
            Image header = UIStyle.CreateImage(panel, "Header", UIStyle.Rounded(20), UIStyle.Header);
            var rect = (RectTransform)header.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-panelPadding * 2f, headerHeight);
            rect.anchoredPosition = new Vector2(0f, -panelPadding);

            Text title = UIStyle.CreateText(rect, "Title", _font, titleSize, FontStyle.Bold, UIStyle.HeaderText, TextAnchor.MiddleLeft);
            UIStyle.Stretch(title.rectTransform, 24f, 0f, 150f, 0f);
            title.text = "Desejos";

            Image pill = UIStyle.CreateImage(rect, "CountPill", UIStyle.Rounded(17), UIStyle.HeaderPill);
            var pillRect = (RectTransform)pill.transform;
            pillRect.anchorMin = pillRect.anchorMax = pillRect.pivot = new Vector2(1f, 0.5f);
            pillRect.sizeDelta = new Vector2(52f, 34f);
            pillRect.anchoredPosition = new Vector2(-64f, 0f);

            _countText = UIStyle.CreateText(pillRect, "Count", _font, 20, FontStyle.Bold, UIStyle.HeaderText, TextAnchor.MiddleCenter);
            UIStyle.Stretch(_countText.rectTransform);
            _countText.text = "0";

            Image close = UIStyle.CreateImage(rect, "CloseButton", UIStyle.Rounded(20), Color.white);
            close.raycastTarget = true;
            var closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.sizeDelta = new Vector2(42f, 42f);
            closeRect.anchoredPosition = new Vector2(-12f, 0f);

            var button = close.gameObject.AddComponent<Button>();
            button.targetGraphic = close;
            var colors = button.colors;
            colors.normalColor = UIStyle.HeaderButton;
            colors.highlightedColor = UIStyle.Accent;
            colors.pressedColor = UIStyle.HeaderPill;
            colors.selectedColor = UIStyle.HeaderButton;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(Close);

            Text x = UIStyle.CreateText(closeRect, "X", _font, 24, FontStyle.Bold, UIStyle.HeaderText, TextAnchor.MiddleCenter);
            UIStyle.Stretch(x.rectTransform);
            x.text = "X";
        }
    }
}
