using System.Collections.Generic;
using System.Text;
using AliGame.Core;
using AliGame.Data;
using AliGame.Items;
using AliGame.Movement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AliGame.UI
{
    /// <summary>
    /// Crafting panel built at runtime. Recipes can only be crafted at their own station, so the panel is
    /// opened by a CraftingStation (Open(StationType)) and lists just that station's recipes.
    /// Esc, the X button or a click outside closes it.
    /// </summary>
    public class CraftingUI : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;
        [Tooltip("Runs recipes that use the cut minigame. Found in the scene if left empty.")]
        [SerializeField] private CutMinigame cutMinigame;
        [SerializeField] private Font font;
        [SerializeField] private List<RecipeSO> recipes = new List<RecipeSO>();
        [SerializeField, Min(0.5f)] private float uiScale = 1.45f;
        [SerializeField] private float animationDuration = 0.22f;

        private const float PanelWidth = 640f;
        private const float PanelPadding = 28f;
        private const float HeaderHeight = 64f;
        private const float Gap = 16f;
        private const float TrayPadding = 16f;
        private const float RowHeight = 104f;
        private const float RowGap = 12f;

        private const string EnoughColor = "#4E8B3A";
        private const string MissingColor = "#B0442E";

        private sealed class RecipeRow
        {
            public RecipeSO Recipe;
            public Text Ingredients;
            public Button Button;
            public Text ButtonLabel;
        }

        private readonly List<RecipeRow> _rows = new List<RecipeRow>();
        private GameObject _root;
        private CanvasGroup _group;
        private RectTransform _window;
        private Font _font;
        private PlayerMovement2D _player;
        private StationType _station;
        private CraftingStation _source;
        private bool _playerLocked;
        private bool _open;
        private float _t;

        public bool IsOpen => _open;

        /// <summary>True while the panel is open or a cut minigame started from it is running.</summary>
        public bool IsBusy => _open || (cutMinigame != null && cutMinigame.IsRunning);

        public StationType CurrentStation => _station;

        private void Awake()
        {
            if (inventory == null) inventory = FindFirstObjectByType<Inventory>();
            if (inventory == null)
            {
                Debug.LogWarning("CraftingUI: no Inventory found in the scene.", this);
                enabled = false;
                return;
            }

            if (cutMinigame == null) cutMinigame = FindFirstObjectByType<CutMinigame>();
            _font = UIStyle.ResolveFont(font);
            _player = inventory.GetComponent<PlayerMovement2D>();
            EnsureEventSystem();
            BuildShell();

            ApplyAnimation();
            _root.SetActive(false);
        }

        private void OnEnable()
        {
            if (inventory == null) return;
            inventory.Changed += Refresh;
            UIPanels.Opened += OnOtherPanelOpened;
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.Changed -= Refresh;
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

        /// <summary>
        /// Opens the panel with the recipes of the given station. Crafted items spawn from the source
        /// station, or from the player if none is given.
        /// </summary>
        public void Open(StationType station, CraftingStation source = null)
        {
            _station = station;
            _source = source;
            _open = true;
            _group.blocksRaycasts = true;
            _group.interactable = true;
            SetPlayerLocked(true);

            UIPanels.NotifyOpened(this);
            _root.SetActive(true);
            BuildPanel();
            Refresh();
        }

        public void Close()
        {
            _open = false;
            _group.blocksRaycasts = false;
            _group.interactable = false;
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

        private void Refresh()
        {
            foreach (RecipeRow row in _rows)
            {
                bool canCraft = inventory.CanCraft(row.Recipe, _station);
                var text = new StringBuilder();

                foreach (RecipeSO.Ingredient ingredient in row.Recipe.Ingredients)
                {
                    int have = inventory.Count(ingredient.Item);
                    bool enough = have >= ingredient.Amount;

                    if (text.Length > 0) text.Append("    ");
                    text.Append("<color=").Append(enough ? EnoughColor : MissingColor).Append('>')
                        .Append(ingredient.Item.DisplayName).Append(' ').Append(have).Append('/').Append(ingredient.Amount)
                        .Append("</color>");
                }

                row.Ingredients.text = text.ToString();
                row.Button.interactable = canCraft;
                row.ButtonLabel.color = canCraft ? UIStyle.TextDark : UIStyle.TextHint;
            }
        }

        private void Craft(RecipeSO recipe)
        {
            if (recipe.UsesCutMinigame && cutMinigame != null)
            {
                if (cutMinigame.Begin(recipe, _source)) Close();
                return;
            }

            if (!inventory.Craft(recipe, _station)) return;

            if (_source != null)
            {
                _source.SpawnCrafted(recipe);
                return;
            }

            Vector2 position = (Vector2)inventory.transform.position + Vector2.up * 1.5f;
            ItemPickup.Spawn(recipe.Result, recipe.ResultAmount, position, new Vector2(0f, 4f), 0.8f);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private List<RecipeSO> RecipesForStation()
        {
            var valid = new List<RecipeSO>();
            foreach (RecipeSO recipe in recipes)
            {
                if (recipe == null || recipe.Result == null || recipe.Station != _station) continue;

                bool complete = recipe.Ingredients.Count > 0;
                foreach (RecipeSO.Ingredient ingredient in recipe.Ingredients)
                    complete &= ingredient.Item != null;

                if (complete) valid.Add(recipe);
                else Debug.LogWarning("CraftingUI: recipe '" + recipe.name + "' has no ingredients or an empty ingredient slot; skipping it.", recipe);
            }
            return valid;
        }

        private void BuildShell()
        {
            var canvasObject = new GameObject("CraftingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
        }

        private void BuildPanel()
        {
            _rows.Clear();
            for (int i = _window.childCount - 1; i >= 0; i--)
            {
                Transform old = _window.GetChild(i);
                old.SetParent(null, false);
                Destroy(old.gameObject);
            }

            List<RecipeSO> valid = RecipesForStation();
            float trayWidth = PanelWidth - PanelPadding * 2f;
            float rowWidth = trayWidth - TrayPadding * 2f;
            float rowsHeight = valid.Count > 0 ? valid.Count * RowHeight + (valid.Count - 1) * RowGap : 70f;
            float trayHeight = rowsHeight + TrayPadding * 2f;
            float panelHeight = PanelPadding * 2f + HeaderHeight + Gap + trayHeight;

            Image panel = UIStyle.CreateImage(_window, "Panel", UIStyle.Rounded(28), UIStyle.Panel);
            panel.raycastTarget = true;
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(PanelWidth, panelHeight);
            UIStyle.AddShadowBehind(panelRect, 40f, new Vector2(0f, -12f), 0.5f);

            BuildHeader(panelRect, _station.DisplayName());

            Image tray = UIStyle.CreateImage(panelRect, "Tray", UIStyle.Rounded(22), UIStyle.Tray);
            var trayRect = (RectTransform)tray.transform;
            trayRect.anchorMin = trayRect.anchorMax = trayRect.pivot = new Vector2(0.5f, 1f);
            trayRect.sizeDelta = new Vector2(trayWidth, trayHeight);
            trayRect.anchoredPosition = new Vector2(0f, -(PanelPadding + HeaderHeight + Gap));

            if (valid.Count == 0)
            {
                Text empty = UIStyle.CreateText(trayRect, "Empty", _font, 22, FontStyle.Normal, UIStyle.TextSoft, TextAnchor.MiddleCenter);
                UIStyle.Stretch(empty.rectTransform);
                empty.text = "Nenhuma receita para esta bancada.";
                return;
            }

            for (int i = 0; i < valid.Count; i++)
                _rows.Add(BuildRow(trayRect, valid[i], i, rowWidth));
        }

        private void BuildHeader(RectTransform panel, string titleText)
        {
            Image header = UIStyle.CreateImage(panel, "Header", UIStyle.Rounded(20), UIStyle.Header);
            var rect = (RectTransform)header.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-PanelPadding * 2f, HeaderHeight);
            rect.anchoredPosition = new Vector2(0f, -PanelPadding);

            Text title = UIStyle.CreateText(rect, "Title", _font, 32, FontStyle.Bold, UIStyle.HeaderText, TextAnchor.MiddleLeft);
            UIStyle.Stretch(title.rectTransform, 24f, 0f, 80f, 0f);
            title.text = titleText;

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

        private RecipeRow BuildRow(RectTransform tray, RecipeSO recipe, int index, float rowWidth)
        {
            var rowObject = new GameObject("Recipe " + recipe.DisplayName, typeof(RectTransform));
            rowObject.transform.SetParent(tray, false);
            var rowRect = (RectTransform)rowObject.transform;
            rowRect.anchorMin = rowRect.anchorMax = rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.sizeDelta = new Vector2(rowWidth, RowHeight);
            rowRect.anchoredPosition = new Vector2(0f, -(TrayPadding + index * (RowHeight + RowGap)));

            Image background = UIStyle.CreateImage(rowRect, "Background", UIStyle.Rounded(18), UIStyle.SlotFilled);
            UIStyle.Stretch((RectTransform)background.transform);
            UIStyle.AddShadowBehind((RectTransform)background.transform, 8f, new Vector2(0f, -3f), 0.3f);

            ItemSO result = recipe.Result;
            Image tile = UIStyle.CreateImage(rowRect, "IconTile", UIStyle.Rounded(16), UIStyle.Tray);
            var tileRect = (RectTransform)tile.transform;
            tileRect.anchorMin = tileRect.anchorMax = tileRect.pivot = new Vector2(0f, 0.5f);
            tileRect.sizeDelta = new Vector2(76f, 76f);
            tileRect.anchoredPosition = new Vector2(14f, 0f);

            Image icon = UIStyle.CreateImage(tileRect, "Icon", null, result.Tint);
            icon.sprite = result.Icon;
            icon.enabled = result.Icon != null;
            icon.preserveAspect = true;
            UIStyle.Stretch((RectTransform)icon.transform, 9f, 9f, 9f, 9f);

            if (result.Icon == null)
            {
                Text initial = UIStyle.CreateText(tileRect, "Initial", _font, 30, FontStyle.Bold, UIStyle.TextDark, TextAnchor.MiddleCenter);
                UIStyle.Stretch(initial.rectTransform);
                initial.text = result.DisplayName.Length > 0 ? result.DisplayName.Substring(0, 1).ToUpperInvariant() : "?";
            }

            Text nameText = UIStyle.CreateText(rowRect, "Name", _font, 26, FontStyle.Bold, UIStyle.TextDark, TextAnchor.LowerLeft);
            var nameRect = nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(106f, 0f);
            nameRect.offsetMax = new Vector2(-160f, -10f);
            nameText.text = recipe.ResultAmount > 1 ? recipe.DisplayName + " x" + recipe.ResultAmount : recipe.DisplayName;

            Text ingredients = UIStyle.CreateText(rowRect, "Ingredients", _font, 20, FontStyle.Bold, UIStyle.TextSoft, TextAnchor.UpperLeft);
            ingredients.supportRichText = true;
            var ingredientsRect = ingredients.rectTransform;
            ingredientsRect.anchorMin = new Vector2(0f, 0f);
            ingredientsRect.anchorMax = new Vector2(1f, 0.5f);
            ingredientsRect.offsetMin = new Vector2(106f, 10f);
            ingredientsRect.offsetMax = new Vector2(-160f, -2f);

            Image buttonImage = UIStyle.CreateImage(rowRect, "CraftButton", UIStyle.Rounded(18), Color.white);
            buttonImage.raycastTarget = true;
            var buttonRect = (RectTransform)buttonImage.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(1f, 0.5f);
            buttonRect.sizeDelta = new Vector2(132f, 56f);
            buttonRect.anchoredPosition = new Vector2(-16f, 0f);

            var button = buttonImage.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            var colors = button.colors;
            colors.normalColor = UIStyle.Accent;
            colors.highlightedColor = UIStyle.Hex("#FFC15E");
            colors.pressedColor = UIStyle.Hex("#D48E22");
            colors.selectedColor = UIStyle.Accent;
            colors.disabledColor = UIStyle.SlotEmpty;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(() => Craft(recipe));

            Text label = UIStyle.CreateText(buttonRect, "Label", _font, 24, FontStyle.Bold, UIStyle.TextDark, TextAnchor.MiddleCenter);
            UIStyle.Stretch(label.rectTransform);
            label.text = "Criar";

            return new RecipeRow { Recipe = recipe, Ingredients = ingredients, Button = button, ButtonLabel = label };
        }
    }
}
