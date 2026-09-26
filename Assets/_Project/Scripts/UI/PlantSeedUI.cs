using System;
using System.Collections.Generic;
using AliGame.Core;
using AliGame.Data;
using AliGame.Movement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AliGame.UI
{
    /// <summary>
    /// Small popup a Plot opens when the player carries more than one kind of seed it could grow: pick a row and
    /// that crop is planted. Esc, the X button or a click outside closes it without planting anything.
    /// One of these is enough for every Plot in the scene; a Plot finds it if the field is left empty.
    /// </summary>
    public class PlantSeedUI : MonoBehaviour
    {
        [SerializeField] private Font font;
        [SerializeField, Min(0.5f)] private float uiScale = 1.45f;
        [SerializeField] private float animationDuration = 0.2f;

        private const float PanelWidth = 460f;
        private const float PanelPadding = 24f;
        private const float HeaderHeight = 58f;
        private const float Gap = 14f;
        private const float TrayPadding = 14f;
        private const float RowHeight = 84f;
        private const float RowGap = 10f;

        private GameObject _root;
        private CanvasGroup _group;
        private RectTransform _window;
        private Font _font;
        private PlayerMovement2D _player;
        private bool _playerLocked;
        private bool _open;
        private float _t;
        private Action<CropSO> _onPicked;

        public bool IsOpen => _open;

        private void Awake()
        {
            _font = UIStyle.ResolveFont(font);
            EnsureEventSystem();
            BuildShell();

            ApplyAnimation();
            _root.SetActive(false);
        }

        private void OnEnable() => UIPanels.Opened += OnOtherPanelOpened;

        private void OnDisable()
        {
            UIPanels.Opened -= OnOtherPanelOpened;
            SetPlayerLocked(false);
        }

        private void Update()
        {
            if (_open && GameInput.CancelPressed) Close();

            float target = _open ? 1f : 0f;
            if (Mathf.Approximately(_t, target)) return;

            _t = Mathf.MoveTowards(_t, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, animationDuration));
            ApplyAnimation();
            if (!_open && _t <= 0f) _root.SetActive(false);
        }

        /// <summary>Opens with one row per option. onPicked is called with the chosen crop, after the popup has closed.</summary>
        public void Open(IReadOnlyList<CropSO> options, PlayerMovement2D player, Action<CropSO> onPicked)
        {
            _onPicked = onPicked;
            _player = player;
            _open = true;

            UIPanels.NotifyOpened(this);
            _root.SetActive(true);
            SetPlayerLocked(true);
            BuildPanel(options);
        }

        public void Close()
        {
            _open = false;
            _onPicked = null;
            SetPlayerLocked(false);
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

        private void Pick(CropSO crop)
        {
            Action<CropSO> callback = _onPicked;
            Close();
            callback?.Invoke(crop);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private void BuildShell()
        {
            var canvasObject = new GameObject("PlantSeedCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
            _group.blocksRaycasts = true;
            _group.interactable = true;

            Image backdrop = UIStyle.CreateImage(_root.transform, "Backdrop", null, UIStyle.Backdrop);
            backdrop.raycastTarget = true;
            UIStyle.Stretch((RectTransform)backdrop.transform);
            backdrop.gameObject.AddComponent<Button>().onClick.AddListener(Close);

            var windowObject = new GameObject("Window", typeof(RectTransform));
            windowObject.transform.SetParent(_root.transform, false);
            _window = (RectTransform)windowObject.transform;
            UIStyle.Stretch(_window);
        }

        private void BuildPanel(IReadOnlyList<CropSO> options)
        {
            for (int i = _window.childCount - 1; i >= 0; i--)
            {
                Transform old = _window.GetChild(i);
                old.SetParent(null, false);
                Destroy(old.gameObject);
            }

            float trayWidth = PanelWidth - PanelPadding * 2f;
            float rowWidth = trayWidth - TrayPadding * 2f;
            int count = Mathf.Max(1, options.Count);
            float trayHeight = count * RowHeight + (count - 1) * RowGap + TrayPadding * 2f;
            float panelHeight = PanelPadding * 2f + HeaderHeight + Gap + trayHeight;

            Image panel = UIStyle.CreateImage(_window, "Panel", UIStyle.Rounded(26), UIStyle.Panel);
            panel.raycastTarget = true;
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(PanelWidth, panelHeight);
            UIStyle.AddShadowBehind(panelRect, 40f, new Vector2(0f, -12f), 0.5f);

            BuildHeader(panelRect);

            Image tray = UIStyle.CreateImage(panelRect, "Tray", UIStyle.Rounded(20), UIStyle.Tray);
            var trayRect = (RectTransform)tray.transform;
            trayRect.anchorMin = trayRect.anchorMax = trayRect.pivot = new Vector2(0.5f, 1f);
            trayRect.sizeDelta = new Vector2(trayWidth, trayHeight);
            trayRect.anchoredPosition = new Vector2(0f, -(PanelPadding + HeaderHeight + Gap));

            for (int i = 0; i < options.Count; i++)
                BuildRow(trayRect, options[i], i, rowWidth);
        }

        private void BuildHeader(RectTransform panel)
        {
            Image header = UIStyle.CreateImage(panel, "Header", UIStyle.Rounded(18), UIStyle.Header);
            var rect = (RectTransform)header.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-PanelPadding * 2f, HeaderHeight);
            rect.anchoredPosition = new Vector2(0f, -PanelPadding);

            Text title = UIStyle.CreateText(rect, "Title", _font, 26, FontStyle.Bold, UIStyle.HeaderText, TextAnchor.MiddleLeft);
            UIStyle.Stretch(title.rectTransform, 20f, 0f, 70f, 0f);
            title.text = "O que plantar?";

            Image close = UIStyle.CreateImage(rect, "CloseButton", UIStyle.Rounded(18), Color.white);
            close.raycastTarget = true;
            var closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.sizeDelta = new Vector2(38f, 38f);
            closeRect.anchoredPosition = new Vector2(-10f, 0f);

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

            Text x = UIStyle.CreateText(closeRect, "X", _font, 22, FontStyle.Bold, UIStyle.HeaderText, TextAnchor.MiddleCenter);
            UIStyle.Stretch(x.rectTransform);
            x.text = "X";
        }

        private void BuildRow(RectTransform tray, CropSO crop, int index, float rowWidth)
        {
            var rowObject = new GameObject("Seed " + crop.DisplayName, typeof(RectTransform));
            rowObject.transform.SetParent(tray, false);
            var rowRect = (RectTransform)rowObject.transform;
            rowRect.anchorMin = rowRect.anchorMax = rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.sizeDelta = new Vector2(rowWidth, RowHeight);
            rowRect.anchoredPosition = new Vector2(0f, -(TrayPadding + index * (RowHeight + RowGap)));

            Image background = UIStyle.CreateImage(rowRect, "Background", UIStyle.Rounded(16), UIStyle.SlotFilled);
            background.raycastTarget = true;
            UIStyle.Stretch((RectTransform)background.transform);
            UIStyle.AddShadowBehind((RectTransform)background.transform, 6f, new Vector2(0f, -2f), 0.25f);

            ItemSO seed = crop.Seed;
            Image tile = UIStyle.CreateImage(rowRect, "IconTile", UIStyle.Rounded(14), UIStyle.Tray);
            var tileRect = (RectTransform)tile.transform;
            tileRect.anchorMin = tileRect.anchorMax = tileRect.pivot = new Vector2(0f, 0.5f);
            tileRect.sizeDelta = new Vector2(62f, 62f);
            tileRect.anchoredPosition = new Vector2(11f, 0f);

            Image icon = UIStyle.CreateImage(tileRect, "Icon", null, Color.white);
            icon.sprite = seed != null ? seed.Icon : null;
            icon.enabled = icon.sprite != null;
            icon.preserveAspect = true;
            UIStyle.Stretch((RectTransform)icon.transform, 7f, 7f, 7f, 7f);

            if (icon.sprite == null)
            {
                Text initial = UIStyle.CreateText(tileRect, "Initial", _font, 24, FontStyle.Bold, UIStyle.TextDark, TextAnchor.MiddleCenter);
                UIStyle.Stretch(initial.rectTransform);
                initial.text = crop.DisplayName.Length > 0 ? crop.DisplayName.Substring(0, 1).ToUpperInvariant() : "?";
            }

            Text nameText = UIStyle.CreateText(rowRect, "Name", _font, 24, FontStyle.Bold, UIStyle.TextDark, TextAnchor.MiddleLeft);
            var nameRect = nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(86f, 0f);
            nameRect.offsetMax = new Vector2(-16f, 0f);
            nameText.text = crop.DisplayName;

            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            var colors = button.colors;
            colors.normalColor = UIStyle.SlotFilled;
            colors.highlightedColor = UIStyle.Hex("#FFF3D9");
            colors.pressedColor = UIStyle.Accent;
            colors.selectedColor = UIStyle.SlotFilled;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(() => Pick(crop));
        }
    }
}
