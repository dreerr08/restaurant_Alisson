using AliGame.Data;
using AliGame.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AliGame.Wishes
{
    /// <summary>
    /// The bubble above a character: a mark when they have something new to ask for, the icon of what they
    /// are waiting for once you are carrying it, and a quiet "..." while they still want to talk and get to know
    /// you. Put it on the NPC, next to the NpcWishGiver.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(NpcWishGiver))]
    public class WishIndicator : MonoBehaviour
    {
        private const string CanvasName = "WishBubble";

        private enum Mode { Hidden, NewWish, ReadyToDeliver, WantsToTalk }

        [Header("Placement")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 2.8f);
        [SerializeField, Min(0.1f)] private float size = 0.9f;

        [Header("Look")]
        [SerializeField] private Color newWishColor = new Color(0.94f, 0.66f, 0.23f, 1f);
        [SerializeField] private Color readyColor = new Color(0.31f, 0.62f, 0.25f, 1f);
        [SerializeField] private Color talkColor = new Color(0.55f, 0.72f, 0.88f, 1f);
        [SerializeField] private string newWishSymbol = "!";
        [SerializeField] private string talkSymbol = "...";
        [Tooltip("Show a bubble while they are waiting to get to know you, so it is clear they want to talk.")]
        [SerializeField] private bool showWhenWantsToTalk = true;
        [SerializeField, Min(8)] private int symbolSize = 40;

        [Header("Motion")]
        [SerializeField] private float bobAmplitude = 0.12f;
        [SerializeField] private float bobSpeed = 2.5f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.18f;

        [Header("Editor preview")]
        [Tooltip("Draws the bubble in the Game view outside Play mode so you can place it. Never saved in the scene.")]
        [SerializeField] private bool previewInEditor = true;
        [SerializeField] private Mode previewMode = Mode.NewWish;

        private NpcWishGiver _giver;
        private RectTransform _bubble;
        private CanvasGroup _group;
        private Image _background;
        private Image _icon;
        private Text _symbol;
        private Mode _mode = Mode.Hidden;
        private float _t;
        private bool _rebuildQueued;

        private void Awake()
        {
            if (!Application.isPlaying) return;

            _giver = GetComponent<NpcWishGiver>();
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
            if (!previewInEditor || !isActiveAndEnabled || previewMode == Mode.Hidden) return;

            Build();
            SetMode(previewMode, null);
            _t = 1f;
            Apply();

            foreach (Transform child in transform)
            {
                if (child.name == CanvasName) SetPreviewFlags(child);
            }
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

            WishSO wish = _giver.ActiveWish;
            Mode wanted = _giver.CanDeliver ? Mode.ReadyToDeliver
                : _giver.HasNewWish ? Mode.NewWish
                : showWhenWantsToTalk && _giver.WantsToTalk ? Mode.WantsToTalk
                : Mode.Hidden;

            if (wanted != _mode) SetMode(wanted, wish);

            float target = _mode == Mode.Hidden ? 0f : 1f;
            _t = Mathf.MoveTowards(_t, target, Time.deltaTime / fadeDuration);
            Apply();
        }

        private void SetMode(Mode mode, WishSO wish)
        {
            _mode = mode;
            if (mode == Mode.Hidden) return;

            bool ready = mode == Mode.ReadyToDeliver;
            bool talk = mode == Mode.WantsToTalk;
            _background.color = ready ? readyColor : talk ? talkColor : newWishColor;

            ItemSO first = null;
            if (ready && wish != null && wish.Requirements.Count > 0) first = wish.Requirements[0].Item;

            bool showIcon = first != null && first.Icon != null;
            _icon.enabled = showIcon;
            if (showIcon)
            {
                _icon.sprite = first.Icon;
                _icon.color = Color.white;
            }

            _symbol.enabled = !showIcon;
            _symbol.text = ready ? "!" : talk ? talkSymbol : newWishSymbol;
        }

        private void Apply()
        {
            float eased = _t * _t * (3f - 2f * _t);
            _group.alpha = eased;

            float bob = Application.isPlaying ? Mathf.Sin(Time.time * bobSpeed) * bobAmplitude : 0f;
            _bubble.parent.localPosition = new Vector3(offset.x, offset.y + bob, 0f);
            _bubble.parent.localScale = Vector3.one * (size / 100f) * Mathf.LerpUnclamped(0.6f, 1f, eased);
        }

        private void Build()
        {
            var canvasObject = new GameObject(CanvasName, typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
            canvasObject.transform.localScale = Vector3.one * (size / 100f);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = 20;

            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = new Vector2(100f, 100f);

            _background = UIStyle.CreateImage(canvasRect, "Bubble", UIStyle.Rounded(48), newWishColor);
            _bubble = (RectTransform)_background.transform;
            UIStyle.Stretch(_bubble);
            _group = _background.gameObject.AddComponent<CanvasGroup>();

            var shadow = _background.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(UIStyle.ShadowTint.r, UIStyle.ShadowTint.g, UIStyle.ShadowTint.b, 0.35f);
            shadow.effectDistance = new Vector2(0f, -4f);

            _icon = UIStyle.CreateImage(_bubble, "Icon", null, Color.white);
            _icon.preserveAspect = true;
            _icon.enabled = false;
            UIStyle.Stretch((RectTransform)_icon.transform, 22f, 22f, 22f, 22f);

            _symbol = UIStyle.CreateText(_bubble, "Symbol", UIStyle.ResolveFont(null), symbolSize, FontStyle.Bold, UIStyle.TextDark, TextAnchor.MiddleCenter);
            UIStyle.Stretch(_symbol.rectTransform);
            _symbol.text = newWishSymbol;
        }
    }
}
