using UnityEngine;
using UnityEngine.UI;

namespace AliGame.UI
{
    /// <summary>
    /// The "[E] Use something" hint at the bottom of the screen. Anything interactable calls
    /// InteractionPromptUI.Show / Hide with itself as owner. Put one in the scene to tune it: every size below
    /// is a field, and with Preview In Editor on the hint is drawn in the Game view without pressing Play
    /// (the preview is never saved in the scene). Scenes without one get a default created on first use.
    /// </summary>
    [ExecuteAlways]
    public class InteractionPromptUI : MonoBehaviour
    {
        private const string CanvasName = "PromptCanvas";

        private static InteractionPromptUI _instance;

        [Header("General")]
        [SerializeField] private Font font;
        [SerializeField, Min(0.5f)] private float uiScale = 1.3f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.15f;
        [Tooltip("How far the hint slides up while it fades in.")]
        [SerializeField] private float slideDistance = 14f;

        [Header("Position")]
        [Tooltip("Distance from the bottom of the screen.")]
        [SerializeField] private float bottomMargin = 90f;
        [SerializeField] private float horizontalOffset;

        [Header("Pill")]
        [SerializeField, Min(0)] private int cornerRadius = 26;
        [SerializeField] private float paddingLeft = 12f;
        [SerializeField] private float paddingRight = 26f;
        [SerializeField] private float paddingTop = 10f;
        [SerializeField] private float paddingBottom = 10f;
        [Tooltip("Space between the key badge and the text.")]
        [SerializeField] private float spacing = 14f;

        [Header("Key badge")]
        [SerializeField, Min(16f)] private float badgeSize = 46f;
        [SerializeField, Min(0)] private int badgeCornerRadius = 14;
        [SerializeField, Min(8)] private int keyTextSize = 28;

        [Header("Text")]
        [SerializeField, Min(8)] private int labelTextSize = 28;

        [Header("Editor preview")]
        [Tooltip("Draws the hint in the Game view outside Play mode so you can adjust it. Never saved in the scene.")]
        [SerializeField] private bool previewInEditor = true;
        [SerializeField] private string previewKey = "E";
        [SerializeField] private string previewText = "Falar com Tio Ben";

        private CanvasGroup _group;
        private RectTransform _pill;
        private Text _keyText;
        private Text _label;
        private object _owner;
        private bool _visible;
        private float _t;
        private bool _rebuildQueued;

        public static void Show(object owner, string key, string text)
        {
            InteractionPromptUI prompt = Instance;
            prompt._owner = owner;
            prompt._visible = true;
            if (prompt._keyText.text != key) prompt._keyText.text = key;
            if (prompt._label.text != text) prompt._label.text = text;
        }

        public static void Hide(object owner)
        {
            if (_instance == null || _instance._owner != owner) return;
            _instance._visible = false;
            _instance._owner = null;
        }

        private static InteractionPromptUI Instance
        {
            get
            {
                if (_instance == null) _instance = FindFirstObjectByType<InteractionPromptUI>();
                if (_instance == null) _instance = new GameObject("InteractionPrompt").AddComponent<InteractionPromptUI>();
                return _instance;
            }
        }

        private void Awake()
        {
            if (!Application.isPlaying) return;

            _instance = this;
            DestroyBuilt();
            Build();
            Apply();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) RebuildPreview();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying) DestroyBuilt();
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

            Build();
            _keyText.text = previewKey ?? string.Empty;
            _label.text = previewText ?? string.Empty;
            _visible = true;
            _t = 1f;
            Apply();

            foreach (Transform child in transform)
            {
                if (child.name == CanvasName) SetPreviewFlags(child);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_pill);
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

            float target = _visible ? 1f : 0f;
            if (Mathf.Approximately(_t, target)) return;

            _t = Mathf.MoveTowards(_t, target, Time.unscaledDeltaTime / fadeDuration);
            Apply();
        }

        private void Apply()
        {
            float eased = _t * _t * (3f - 2f * _t);
            _group.alpha = eased;
            _pill.anchoredPosition = new Vector2(horizontalOffset, bottomMargin - slideDistance * (1f - eased));
        }

        private void Build()
        {
            Font resolvedFont = UIStyle.ResolveFont(font);

            var canvasObject = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.sortingOrder = 80;
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

            Image pill = UIStyle.CreateImage(canvasObject.transform, "Pill", UIStyle.Rounded(cornerRadius), UIStyle.Panel);
            _pill = (RectTransform)pill.transform;
            _pill.anchorMin = _pill.anchorMax = _pill.pivot = new Vector2(0.5f, 0f);
            _group = pill.gameObject.AddComponent<CanvasGroup>();
            _group.interactable = false;
            _group.blocksRaycasts = false;

            var shadow = pill.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(UIStyle.ShadowTint.r, UIStyle.ShadowTint.g, UIStyle.ShadowTint.b, 0.35f);
            shadow.effectDistance = new Vector2(0f, -5f);

            var layout = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(Mathf.RoundToInt(paddingLeft), Mathf.RoundToInt(paddingRight), Mathf.RoundToInt(paddingTop), Mathf.RoundToInt(paddingBottom));
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = pill.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Image badge = UIStyle.CreateImage(_pill, "KeyBadge", UIStyle.Rounded(badgeCornerRadius), UIStyle.Accent);
            var badgeElement = badge.gameObject.AddComponent<LayoutElement>();
            badgeElement.preferredWidth = badgeSize;
            badgeElement.preferredHeight = badgeSize;

            _keyText = UIStyle.CreateText(badge.transform, "Key", resolvedFont, keyTextSize, FontStyle.Bold, UIStyle.TextDark, TextAnchor.MiddleCenter);
            UIStyle.Stretch(_keyText.rectTransform);

            _label = UIStyle.CreateText(_pill, "Label", resolvedFont, labelTextSize, FontStyle.Bold, UIStyle.TextDark, TextAnchor.MiddleLeft);
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;
        }
    }
}
