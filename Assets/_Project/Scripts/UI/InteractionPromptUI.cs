using UnityEngine;
using UnityEngine.UI;

namespace AliGame.UI
{
    /// <summary>
    /// The "[E] Use something" hint at the bottom of the screen. Anything interactable calls
    /// InteractionPromptUI.Show / Hide with itself as owner; the UI creates itself on first use.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        private static InteractionPromptUI _instance;

        private CanvasGroup _group;
        private RectTransform _pill;
        private Text _keyText;
        private Text _label;
        private object _owner;
        private bool _visible;
        private float _t;

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
                if (_instance == null)
                    _instance = new GameObject("InteractionPrompt").AddComponent<InteractionPromptUI>();
                return _instance;
            }
        }

        private void Awake()
        {
            _instance = this;
            Build(UIStyle.ResolveFont(null));
            Apply();
        }

        private void Update()
        {
            float target = _visible ? 1f : 0f;
            if (Mathf.Approximately(_t, target)) return;

            _t = Mathf.MoveTowards(_t, target, Time.unscaledDeltaTime / 0.15f);
            Apply();
        }

        private void Apply()
        {
            float eased = _t * _t * (3f - 2f * _t);
            _group.alpha = eased;
            _pill.anchoredPosition = new Vector2(0f, 90f - 14f * (1f - eased));
        }

        private void Build(Font font)
        {
            var canvasObject = new GameObject("PromptCanvas", typeof(Canvas), typeof(CanvasScaler));
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
            scaler.referenceResolution = new Vector2(1920f, 1080f) / 1.3f;
            scaler.matchWidthOrHeight = 0.5f;

            Image pill = UIStyle.CreateImage(canvasObject.transform, "Pill", UIStyle.Rounded(26), UIStyle.Panel);
            _pill = (RectTransform)pill.transform;
            _pill.anchorMin = _pill.anchorMax = _pill.pivot = new Vector2(0.5f, 0f);
            _group = pill.gameObject.AddComponent<CanvasGroup>();
            _group.interactable = false;
            _group.blocksRaycasts = false;

            var shadow = pill.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(UIStyle.ShadowTint.r, UIStyle.ShadowTint.g, UIStyle.ShadowTint.b, 0.35f);
            shadow.effectDistance = new Vector2(0f, -5f);

            var layout = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 26, 10, 10);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = pill.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Image badge = UIStyle.CreateImage(_pill, "KeyBadge", UIStyle.Rounded(14), UIStyle.Accent);
            var badgeElement = badge.gameObject.AddComponent<LayoutElement>();
            badgeElement.preferredWidth = 46f;
            badgeElement.preferredHeight = 46f;

            _keyText = UIStyle.CreateText(badge.transform, "Key", font, 28, FontStyle.Bold, UIStyle.TextDark, TextAnchor.MiddleCenter);
            UIStyle.Stretch(_keyText.rectTransform);

            _label = UIStyle.CreateText(_pill, "Label", font, 28, FontStyle.Bold, UIStyle.TextDark, TextAnchor.MiddleLeft);
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;
        }
    }
}
