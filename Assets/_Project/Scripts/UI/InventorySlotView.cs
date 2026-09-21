using System;
using AliGame.Data;
using AliGame.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AliGame.UI
{
    public class InventorySlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HoverScale = 1.07f;
        private const float PunchScale = 1.25f;

        private RectTransform _visual;
        private Image _shadow;
        private Image _highlight;
        private Image _background;
        private Image _icon;
        private Text _label;
        private Image _countPill;
        private Text _count;
        private ItemSO _lastItem;
        private int _lastAmount;
        private bool _hovered;
        private float _scale = 1f;

        public ItemSO Item { get; private set; }

        public event Action<ItemSO> Hovered;

        public static InventorySlotView Create(Transform parent, int index, Font font)
        {
            var root = new GameObject("Slot " + index, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            root.GetComponent<Image>().color = Color.clear;

            var view = root.AddComponent<InventorySlotView>();
            view.Build(font);
            return view;
        }

        private void Build(Font font)
        {
            var visualObject = new GameObject("Visual", typeof(RectTransform));
            visualObject.transform.SetParent(transform, false);
            _visual = (RectTransform)visualObject.transform;
            UIStyle.Stretch(_visual);

            _background = UIStyle.CreateImage(_visual, "Background", UIStyle.Rounded(20), UIStyle.SlotEmpty);
            UIStyle.Stretch((RectTransform)_background.transform);

            _shadow = UIStyle.AddShadowBehind((RectTransform)_background.transform, 10f, new Vector2(0f, -4f), 0.35f);

            _highlight = UIStyle.CreateImage(_visual, "Highlight", UIStyle.Rounded(24), UIStyle.Accent);
            UIStyle.Stretch((RectTransform)_highlight.transform, -4f, -4f, -4f, -4f);
            _highlight.transform.SetSiblingIndex(_background.transform.GetSiblingIndex());

            _icon = UIStyle.CreateImage(_visual, "Icon", null, Color.white);
            _icon.preserveAspect = true;
            UIStyle.Stretch((RectTransform)_icon.transform, 16f, 16f, 16f, 16f);
            var iconShadow = _icon.gameObject.AddComponent<Shadow>();
            iconShadow.effectColor = new Color(0.16f, 0.08f, 0.02f, 0.28f);
            iconShadow.effectDistance = new Vector2(0f, -4f);

            _label = UIStyle.CreateText(_visual, "Label", font, 18, FontStyle.Bold, UIStyle.TextDark, TextAnchor.MiddleCenter);
            UIStyle.Stretch(_label.rectTransform, 8f, 8f, 8f, 8f);

            _countPill = UIStyle.CreateImage(_visual, "CountPill", UIStyle.Rounded(13), UIStyle.HeaderPill);
            var pillRect = (RectTransform)_countPill.transform;
            pillRect.anchorMin = pillRect.anchorMax = pillRect.pivot = new Vector2(1f, 0f);
            pillRect.anchoredPosition = new Vector2(-8f, 8f);

            _count = UIStyle.CreateText(_countPill.transform, "Count", font, 18, FontStyle.Bold, UIStyle.HeaderText, TextAnchor.MiddleCenter);
            UIStyle.Stretch(_count.rectTransform);

            Show(null);
        }

        public void Show(ItemStack stack)
        {
            Item = stack?.Item;
            int amount = stack != null ? stack.Amount : 0;
            bool filled = Item != null;

            bool grew = filled && (Item != _lastItem || amount > _lastAmount);
            _lastItem = Item;
            _lastAmount = amount;
            if (grew && isActiveAndEnabled) _scale = PunchScale;

            _background.color = filled ? UIStyle.SlotFilled : UIStyle.SlotEmpty;
            _shadow.enabled = filled;
            _highlight.enabled = filled && _hovered;

            _icon.enabled = filled && Item.Icon != null;
            if (filled)
            {
                _icon.sprite = Item.Icon;
                _icon.color = Item.Tint;
            }

            _label.text = filled && Item.Icon == null ? Item.DisplayName : string.Empty;

            bool showCount = amount > 1;
            _countPill.enabled = showCount;
            _count.text = showCount ? amount.ToString() : string.Empty;
            if (showCount)
                ((RectTransform)_countPill.transform).sizeDelta = new Vector2(20f + 11f * _count.text.Length, 26f);
        }

        private void OnEnable()
        {
            _hovered = false;
            _scale = 1f;
            if (_visual != null) _visual.localScale = Vector3.one;
        }

        private void Update()
        {
            float target = _hovered && Item != null ? HoverScale : 1f;
            _scale = Mathf.Lerp(_scale, target, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
            _visual.localScale = Vector3.one * _scale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            _highlight.enabled = Item != null;
            Hovered?.Invoke(Item);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _highlight.enabled = false;
            Hovered?.Invoke(null);
        }
    }
}
