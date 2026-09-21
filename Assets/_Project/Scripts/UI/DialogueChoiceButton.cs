using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AliGame.UI
{
    /// <summary>One answer in the dialogue box: a number badge and the label. Reports hover and click.</summary>
    public class DialogueChoiceButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        private static readonly Color SelectedColor = new Color(1f, 0.94f, 0.81f, 1f);

        private Image _background;
        private Outline _outline;

        public int Index { get; private set; }

        public event Action<int> Hovered;

        public event Action<int> Clicked;

        public static DialogueChoiceButton Create(Transform parent, int index, string label, Font font, int textSize, float minHeight, float badgeSize)
        {
            Image background = UIStyle.CreateImage(parent, "Choice " + (index + 1), UIStyle.Rounded(18), UIStyle.Panel);
            background.raycastTarget = true;

            var layout = background.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 16, 8, 8);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var element = background.gameObject.AddComponent<LayoutElement>();
            element.minHeight = minHeight;

            var outline = background.gameObject.AddComponent<Outline>();
            outline.effectColor = UIStyle.Accent;
            outline.effectDistance = new Vector2(2.5f, 2.5f);
            outline.enabled = false;

            Image badge = UIStyle.CreateImage(background.transform, "Number", UIStyle.Rounded(11), UIStyle.Accent);
            var badgeElement = badge.gameObject.AddComponent<LayoutElement>();
            badgeElement.preferredWidth = badgeSize;
            badgeElement.preferredHeight = badgeSize;
            badgeElement.flexibleWidth = 0f;

            Text number = UIStyle.CreateText(badge.transform, "Text", font, Mathf.RoundToInt(badgeSize * 0.65f), FontStyle.Bold, UIStyle.TextDark, TextAnchor.MiddleCenter);
            UIStyle.Stretch(number.rectTransform);
            number.text = (index + 1).ToString();

            Text text = UIStyle.CreateText(background.transform, "Label", font, textSize, FontStyle.Bold, UIStyle.TextDark, TextAnchor.MiddleLeft);
            text.text = label;
            var textElement = text.gameObject.AddComponent<LayoutElement>();
            textElement.flexibleWidth = 1f;

            var button = background.gameObject.AddComponent<DialogueChoiceButton>();
            button.Index = index;
            button._background = background;
            button._outline = outline;
            return button;
        }

        public void SetSelected(bool selected)
        {
            _outline.enabled = selected;
            _background.color = selected ? SelectedColor : UIStyle.Panel;
        }

        public void OnPointerEnter(PointerEventData eventData) => Hovered?.Invoke(Index);

        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(Index);
    }
}
