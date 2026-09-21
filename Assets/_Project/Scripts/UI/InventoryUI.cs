using AliGame.Data;
using AliGame.Items;
using AliGame.Movement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AliGame.UI
{
    /// <summary>
    /// Builds the inventory panel at runtime. I or Tab toggles it, Esc closes it.
    /// Hover a slot to read the item's name and description.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;
        [SerializeField] private Font font;
        [SerializeField, Min(1)] private int columns = 4;
        [SerializeField] private float slotSize = 96f;
        [SerializeField] private float spacing = 12f;
        [SerializeField, Min(0.5f)] private float uiScale = 1.45f;
        [SerializeField] private float animationDuration = 0.22f;
        [SerializeField] private bool startOpen;

        private const float PanelPadding = 28f;
        private const float HeaderHeight = 64f;
        private const float Gap = 16f;
        private const float TrayPadding = 16f;
        private const float DetailHeight = 112f;

        private GameObject _root;
        private CanvasGroup _group;
        private RectTransform _window;
        private InventorySlotView[] _slots;
        private Text _capacityText;
        private Text _detailName;
        private Text _detailDescription;
        private Font _font;
        private PlayerMovement2D _player;
        private bool _playerLocked;
        private bool _open;
        private float _t;

        public bool IsOpen => _open;

        private void Awake()
        {
            if (inventory == null) inventory = FindFirstObjectByType<Inventory>();
            if (inventory == null)
            {
                Debug.LogWarning("InventoryUI: no Inventory found in the scene.", this);
                enabled = false;
                return;
            }

            _font = UIStyle.ResolveFont(font);
            _player = inventory.GetComponent<PlayerMovement2D>();
            EnsureEventSystem();
            BuildUI();

            _open = startOpen;
            SetPlayerLocked(startOpen);
            _t = startOpen ? 1f : 0f;
            ApplyAnimation();
            _root.SetActive(startOpen);
        }

        private void OnEnable()
        {
            if (inventory == null) return;
            inventory.Changed += Refresh;
            UIPanels.Opened += OnOtherPanelOpened;
            Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.Changed -= Refresh;
            UIPanels.Opened -= OnOtherPanelOpened;
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

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.iKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame) Toggle();
                else if (_open && keyboard.escapeKey.wasPressedThisFrame) Close();
            }

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
            ShowDetails(null);
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

        private void Refresh()
        {
            if (_slots == null) return;

            var stacks = inventory.Model.Stacks;
            for (int i = 0; i < _slots.Length; i++)
                _slots[i].Show(i < stacks.Count ? stacks[i] : null);

            _capacityText.text = stacks.Count + " / " + _slots.Length;
        }

        private void ShowDetails(ItemSO item)
        {
            if (item != null)
            {
                _detailName.text = item.DisplayName;
                _detailDescription.text = item.Description;
                _detailDescription.color = UIStyle.TextSoft;
                return;
            }

            _detailName.text = string.Empty;
            _detailDescription.color = UIStyle.TextHint;
            _detailDescription.text = inventory.Model.Stacks.Count == 0
                ? "Sua mochila está vazia. Pegue itens pelo cenário!"
                : "Passe o mouse sobre um item para ver os detalhes.";
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private void BuildUI()
        {
            int capacity = inventory.Model.Capacity;
            int rows = Mathf.CeilToInt(capacity / (float)columns);
            float gridWidth = columns * slotSize + (columns - 1) * spacing;
            float gridHeight = rows * slotSize + (rows - 1) * spacing;
            float trayWidth = gridWidth + TrayPadding * 2f;
            float trayHeight = gridHeight + TrayPadding * 2f;
            float panelWidth = trayWidth + PanelPadding * 2f;
            float panelHeight = PanelPadding * 2f + HeaderHeight + Gap + trayHeight + Gap + DetailHeight;

            var canvasObject = new GameObject("InventoryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

            BuildBackdrop(_root.transform);

            var windowObject = new GameObject("Window", typeof(RectTransform));
            windowObject.transform.SetParent(_root.transform, false);
            _window = (RectTransform)windowObject.transform;
            UIStyle.Stretch(_window);

            Image panel = UIStyle.CreateImage(_window, "Panel", UIStyle.Rounded(28), UIStyle.Panel);
            panel.raycastTarget = true;
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
            UIStyle.AddShadowBehind(panelRect, 40f, new Vector2(0f, -12f), 0.5f);

            BuildHeader(panelRect);
            BuildTray(panelRect, trayWidth, trayHeight, capacity);
            BuildDetails(panelRect);
        }

        private void BuildBackdrop(Transform parent)
        {
            Image backdrop = UIStyle.CreateImage(parent, "Backdrop", null, UIStyle.Backdrop);
            backdrop.raycastTarget = true;
            UIStyle.Stretch((RectTransform)backdrop.transform);
            backdrop.gameObject.AddComponent<Button>().onClick.AddListener(Close);
        }

        private void BuildHeader(RectTransform panel)
        {
            Image header = UIStyle.CreateImage(panel, "Header", UIStyle.Rounded(20), UIStyle.Header);
            var rect = (RectTransform)header.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-PanelPadding * 2f, HeaderHeight);
            rect.anchoredPosition = new Vector2(0f, -PanelPadding);

            Text title = UIStyle.CreateText(rect, "Title", _font, 32, FontStyle.Bold, UIStyle.HeaderText, TextAnchor.MiddleLeft);
            UIStyle.Stretch(title.rectTransform, 24f, 0f, 170f, 0f);
            title.text = "Inventário";

            Image pill = UIStyle.CreateImage(rect, "CapacityPill", UIStyle.Rounded(17), UIStyle.HeaderPill);
            var pillRect = (RectTransform)pill.transform;
            pillRect.anchorMin = pillRect.anchorMax = pillRect.pivot = new Vector2(1f, 0.5f);
            pillRect.sizeDelta = new Vector2(92f, 34f);
            pillRect.anchoredPosition = new Vector2(-64f, 0f);

            _capacityText = UIStyle.CreateText(pillRect, "Capacity", _font, 20, FontStyle.Bold, UIStyle.HeaderText, TextAnchor.MiddleCenter);
            UIStyle.Stretch(_capacityText.rectTransform);

            Image close = UIStyle.CreateImage(rect, "CloseButton", UIStyle.Rounded(20), Color.white);
            close.type = Image.Type.Sliced;
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

        private void BuildTray(RectTransform panel, float width, float height, int capacity)
        {
            Image tray = UIStyle.CreateImage(panel, "Tray", UIStyle.Rounded(22), UIStyle.Tray);
            var rect = (RectTransform)tray.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0f, -(PanelPadding + HeaderHeight + Gap));

            var gridObject = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridObject.transform.SetParent(rect, false);
            UIStyle.Stretch((RectTransform)gridObject.transform, TrayPadding, TrayPadding, TrayPadding, TrayPadding);

            var layout = gridObject.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(slotSize, slotSize);
            layout.spacing = new Vector2(spacing, spacing);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = columns;
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.childAlignment = TextAnchor.UpperLeft;

            _slots = new InventorySlotView[capacity];
            for (int i = 0; i < capacity; i++)
            {
                _slots[i] = InventorySlotView.Create(gridObject.transform, i, _font);
                _slots[i].Hovered += ShowDetails;
            }
        }

        private void BuildDetails(RectTransform panel)
        {
            Image box = UIStyle.CreateImage(panel, "Details", UIStyle.Rounded(20), UIStyle.DetailBox);
            var rect = (RectTransform)box.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(-PanelPadding * 2f, DetailHeight);
            rect.anchoredPosition = new Vector2(0f, PanelPadding);

            _detailName = UIStyle.CreateText(rect, "Name", _font, 28, FontStyle.Bold, UIStyle.TextDark, TextAnchor.UpperLeft);
            var nameRect = _detailName.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.offsetMin = new Vector2(20f, -50f);
            nameRect.offsetMax = new Vector2(-20f, -12f);

            _detailDescription = UIStyle.CreateText(rect, "Description", _font, 21, FontStyle.Normal, UIStyle.TextSoft, TextAnchor.UpperLeft);
            UIStyle.Stretch(_detailDescription.rectTransform, 20f, 12f, 20f, 50f);
        }
    }
}
