using AliGame.Data;
using UnityEngine;
using UnityEngine.UI;

namespace AliGame.UI
{
    /// <summary>One "item collected" notification: slides in, waits, fades out, collapses.</summary>
    public class ItemToast : MonoBehaviour
    {
        private const float Width = 330f;
        private const float Height = 76f;
        private const float InDuration = 0.28f;
        private const float OutDuration = 0.3f;
        private const float CollapseDuration = 0.18f;

        private enum Phase { In, Hold, Out, Collapse }

        private RectTransform _visual;
        private CanvasGroup _group;
        private LayoutElement _layout;
        private Text _amountText;
        private float _slideOffset;
        private float _holdDuration;
        private float _timer;
        private float _punch;
        private int _amount;
        private Phase _phase = Phase.In;

        public ItemSO Item { get; private set; }

        public bool IsAlive => _phase != Phase.Collapse;

        public static ItemToast Create(Transform parent, ItemSO item, int amount, Font font, float slideOffset, float holdDuration)
        {
            var root = new GameObject("Toast " + item.DisplayName, typeof(RectTransform), typeof(LayoutElement));
            root.transform.SetParent(parent, false);
            var toast = root.AddComponent<ItemToast>();
            toast.Build(item, amount, font, slideOffset, holdDuration);
            return toast;
        }

        private void Build(ItemSO item, int amount, Font font, float slideOffset, float holdDuration)
        {
            Item = item;
            _amount = amount;
            _slideOffset = slideOffset;
            _holdDuration = holdDuration;

            _layout = GetComponent<LayoutElement>();
            _layout.preferredWidth = Width;
            _layout.preferredHeight = Height;

            var visualObject = new GameObject("Visual", typeof(RectTransform), typeof(CanvasGroup));
            visualObject.transform.SetParent(transform, false);
            _visual = (RectTransform)visualObject.transform;
            UIStyle.Stretch(_visual);
            _group = visualObject.GetComponent<CanvasGroup>();

            Image background = UIStyle.CreateImage(_visual, "Background", UIStyle.Rounded(20), UIStyle.Panel);
            UIStyle.Stretch((RectTransform)background.transform);
            UIStyle.AddShadowBehind((RectTransform)background.transform, 12f, new Vector2(0f, -5f), 0.4f);

            Image tile = UIStyle.CreateImage(_visual, "IconTile", UIStyle.Rounded(14), UIStyle.Tray);
            var tileRect = (RectTransform)tile.transform;
            tileRect.anchorMin = tileRect.anchorMax = tileRect.pivot = new Vector2(0f, 0.5f);
            tileRect.sizeDelta = new Vector2(56f, 56f);
            tileRect.anchoredPosition = new Vector2(12f, 0f);

            Image icon = UIStyle.CreateImage(tileRect, "Icon", null, item.Tint);
            icon.sprite = item.Icon;
            icon.enabled = item.Icon != null;
            icon.preserveAspect = true;
            UIStyle.Stretch((RectTransform)icon.transform, 7f, 7f, 7f, 7f);

            if (item.Icon == null)
            {
                Text initial = UIStyle.CreateText(tileRect, "Initial", font, 28, FontStyle.Bold, UIStyle.TextDark, TextAnchor.MiddleCenter);
                UIStyle.Stretch(initial.rectTransform);
                initial.text = item.DisplayName.Length > 0 ? item.DisplayName.Substring(0, 1).ToUpperInvariant() : "?";
            }

            Text nameText = UIStyle.CreateText(_visual, "Name", font, 26, FontStyle.Bold, UIStyle.TextDark, TextAnchor.LowerLeft);
            var nameRect = nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(82f, 0f);
            nameRect.offsetMax = new Vector2(-16f, -6f);
            nameText.text = item.DisplayName;

            _amountText = UIStyle.CreateText(_visual, "Amount", font, 21, FontStyle.Bold, UIStyle.TextSoft, TextAnchor.UpperLeft);
            var amountRect = _amountText.rectTransform;
            amountRect.anchorMin = new Vector2(0f, 0f);
            amountRect.anchorMax = new Vector2(1f, 0.5f);
            amountRect.offsetMin = new Vector2(82f, 6f);
            amountRect.offsetMax = new Vector2(-16f, 0f);

            RefreshText();
            Apply(0f);
        }

        /// <summary>Adds to the shown amount and brings the toast back if it was fading out.</summary>
        public void Add(int extra)
        {
            if (!IsAlive) return;

            _amount += extra;
            RefreshText();
            _punch = 1f;

            if (_phase == Phase.Out)
            {
                _phase = Phase.Hold;
                Apply(1f);
            }
            if (_phase == Phase.Hold) _timer = 0f;
        }

        public void Dismiss()
        {
            if (_phase == Phase.In || _phase == Phase.Hold) StartPhase(Phase.Out);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _timer += dt;

            switch (_phase)
            {
                case Phase.In:
                    Apply(Mathf.Clamp01(_timer / InDuration));
                    if (_timer >= InDuration) StartPhase(Phase.Hold);
                    break;

                case Phase.Hold:
                    if (_timer >= _holdDuration) StartPhase(Phase.Out);
                    break;

                case Phase.Out:
                    float outProgress = Mathf.Clamp01(_timer / OutDuration);
                    _group.alpha = 1f - outProgress;
                    _visual.anchoredPosition = new Vector2(_slideOffset * 0.4f * outProgress, 0f);
                    if (_timer >= OutDuration) StartPhase(Phase.Collapse);
                    break;

                case Phase.Collapse:
                    _layout.preferredHeight = Mathf.Lerp(Height, 0f, Mathf.Clamp01(_timer / CollapseDuration));
                    if (_timer >= CollapseDuration) Destroy(gameObject);
                    break;
            }

            _punch = Mathf.MoveTowards(_punch, 0f, dt * 5f);
            if (_phase != Phase.Collapse) _visual.localScale = Vector3.one * (1f + 0.07f * _punch);
        }

        private void StartPhase(Phase phase)
        {
            _phase = phase;
            _timer = 0f;
            if (phase == Phase.Out || phase == Phase.Collapse) _group.alpha = phase == Phase.Out ? 1f : 0f;
        }

        private void Apply(float progress)
        {
            float eased = EaseOutBack(progress);
            _group.alpha = Mathf.Clamp01(progress * 1.6f);
            _visual.anchoredPosition = new Vector2(_slideOffset * (1f - eased), 0f);
        }

        private void RefreshText()
        {
            _amountText.text = "+" + _amount + " coletado";
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }
    }
}
