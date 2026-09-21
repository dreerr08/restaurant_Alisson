using AliGame.Items;
using UnityEngine;
using UnityEngine.UI;

namespace AliGame.UI
{
    /// <summary>
    /// The bar shown above the player while cutting: how long the key still has to be held, and which cut it is.
    /// Turns green with "Solte!" while the release window is open (the bar then drains), and red with "Errou!" if it is missed. Put it next to the CutMinigame. It is a world-space canvas in
    /// its own child object, positioned over the player each frame (not parented to the player, so flipping the
    /// player never mirrors the text).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CutMinigame))]
    public class CutProgressUI : MonoBehaviour
    {
        private const string CanvasName = "CutProgress";
        private const float CanvasWidth = 240f;
        private const float CanvasHeight = 76f;

        [Header("Placement")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 3.4f);
        [Tooltip("Width of the bar in world units.")]
        [SerializeField, Min(0.2f)] private float width = 2.6f;

        [Header("Look")]
        [SerializeField] private Color fillColor = new Color(0.94f, 0.66f, 0.23f, 1f);
        [SerializeField] private Color readyColor = new Color(0.31f, 0.62f, 0.25f, 1f);
        [SerializeField] private Color missColor = new Color(0.69f, 0.27f, 0.18f, 1f);
        [SerializeField, Min(8)] private int textSize = 24;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.15f;

        [Header("Editor preview")]
        [Tooltip("Draws the bar in the Game view outside Play mode so you can place it. Never saved in the scene.")]
        [SerializeField] private bool previewInEditor = true;
        [SerializeField, Range(0f, 1f)] private float previewProgress = 0.6f;
        [SerializeField] private bool previewReady;

        private CutMinigame _minigame;
        private Transform _target;
        private Transform _canvas;
        private CanvasGroup _group;
        private RectTransform _fill;
        private Image _fillImage;
        private Text _label;
        private Text _counter;
        private float _t;
        private bool _rebuildQueued;

        private void Awake()
        {
            if (!Application.isPlaying) return;

            _minigame = GetComponent<CutMinigame>();
            DestroyBuilt();
            Build();
            Apply(0f, fillColor, "", "");
            _group.alpha = 0f;
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
            Apply(previewProgress, previewReady ? readyColor : fillColor, previewReady ? "Solte!" : "Segure E  1,8s", "2/3");
            _group.alpha = 1f;
            foreach (Transform child in transform)
            {
                if (child.name == CanvasName) SetPreviewFlags(child);
            }
            PlaceAtTarget();
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
            _canvas = null;
        }

        private void LateUpdate()
        {
            if (_canvas == null) return;

            if (!Application.isPlaying)
            {
                PlaceAtTarget();
                return;
            }

            float wanted = _minigame.IsRunning ? 1f : 0f;
            _t = Mathf.MoveTowards(_t, wanted, Time.unscaledDeltaTime / fadeDuration);
            _group.alpha = _t;

            if (_minigame.IsRunning)
            {
                bool ready = _minigame.ReadyToRelease;
                bool missed = _minigame.Missed;
                int shown = Mathf.Min(_minigame.CutsDone + 1, _minigame.RequiredCuts);
                string label = missed ? "Errou!" : ready ? "Solte!" : "Segure E  " + _minigame.SecondsLeft.ToString("0.0") + "s";
                Apply(_minigame.Progress, missed ? missColor : ready ? readyColor : fillColor, label, shown + "/" + _minigame.RequiredCuts);
            }

            PlaceAtTarget();
        }

        private void PlaceAtTarget()
        {
            if (_target == null)
            {
                _target = Application.isPlaying ? _minigame.PlayerTransform : null;
                if (_target == null)
                {
                    var inventory = FindFirstObjectByType<Inventory>();
                    if (inventory != null) _target = inventory.transform;
                }
            }
            if (_target == null) return;

            _canvas.position = new Vector3(_target.position.x + offset.x, _target.position.y + offset.y, 0f);
            _canvas.localScale = Vector3.one * (width / CanvasWidth);
        }

        private void Apply(float progress, Color color, string label, string counter)
        {
            _fill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            _fillImage.color = color;
            _label.text = label;
            _counter.text = counter;
        }

        private void Build()
        {
            var canvasObject = new GameObject(CanvasName, typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.transform;

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = 25;

            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
            _group = canvasObject.AddComponent<CanvasGroup>();

            Font font = UIStyle.ResolveFont(null);
            Image background = UIStyle.CreateImage(canvasRect, "Background", UIStyle.Rounded(22), UIStyle.Panel);
            UIStyle.Stretch((RectTransform)background.transform);

            var shadow = background.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(UIStyle.ShadowTint.r, UIStyle.ShadowTint.g, UIStyle.ShadowTint.b, 0.35f);
            shadow.effectDistance = new Vector2(0f, -4f);

            _label = UIStyle.CreateText(background.transform, "Label", font, textSize, FontStyle.Bold, UIStyle.TextDark, TextAnchor.MiddleLeft);
            UIStyle.Stretch(_label.rectTransform, 16f, 34f, 90f, 6f);

            _counter = UIStyle.CreateText(background.transform, "Counter", font, textSize, FontStyle.Bold, UIStyle.TextSoft, TextAnchor.MiddleRight);
            UIStyle.Stretch(_counter.rectTransform, 150f, 34f, 16f, 6f);

            Image track = UIStyle.CreateImage(background.transform, "Track", UIStyle.Rounded(10), UIStyle.SlotEmpty);
            UIStyle.Stretch((RectTransform)track.transform, 16f, 12f, 16f, 46f);

            _fillImage = UIStyle.CreateImage(track.transform, "Fill", UIStyle.Rounded(10), fillColor);
            _fill = (RectTransform)_fillImage.transform;
            UIStyle.Stretch(_fill);
        }
    }
}
