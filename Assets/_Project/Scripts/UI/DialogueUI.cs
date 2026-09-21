using System;
using System.Collections.Generic;
using AliGame.Core;
using AliGame.Data;
using AliGame.Dialogue;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AliGame.UI
{
    /// <summary>
    /// The default dialogue screen (an IDialogueView): a box at the bottom with the speaker's portrait and name
    /// plate, text that types itself out, a "continue" hint and, when a line has choices, a list of answers.
    /// Continue with E, Space, Enter, click or gamepad South/West (the first press finishes typing). Pick answers
    /// with the mouse, W/S or arrows + E/Enter, or the number keys. Esc leaves the conversation.
    /// </summary>
    public class DialogueUI : MonoBehaviour, IDialogueView
    {
        [SerializeField] private Font font;
        [SerializeField, Min(0.5f)] private float uiScale = 1.3f;
        [Tooltip("Characters revealed per second (a DialogueCharacterSO can override it).")]
        [SerializeField, Min(1f)] private float charactersPerSecond = 42f;
        [SerializeField] private float animationDuration = 0.25f;

        private const float BoxWidth = 1180f;
        private const float BoxHeight = 210f;
        private const float BoxBottomMargin = 44f;
        private const float PortraitSize = 172f;
        private const float ChoicesWidth = 600f;

        private RectTransform _window;
        private CanvasGroup _group;
        private GameObject _root;
        private RectTransform _choicesContainer;
        private RectTransform _indicator;
        private Image _portraitTile;
        private Image _portraitImage;
        private Image _namePlate;
        private RectTransform _namePlateRect;
        private Text _nameText;
        private Text _bodyText;
        private Font _font;

        private readonly List<DialogueChoiceButton> _choices = new List<DialogueChoiceButton>();
        private int _selectedChoice;
        private bool _open;
        private float _t;
        private int _openedFrame;

        private string _fullText = string.Empty;
        private int _shown;
        private bool _typing;
        private float _charTimer;
        private float _nextDelay;
        private float _speed;
        private DialogueLine _line;

        public event Action AdvanceRequested;

        public event Action<int> ChoiceSelected;

        public event Action CancelRequested;

        /// <summary>Raised for every character revealed; hook a typing blip sound here.</summary>
        public event Action<char> CharacterRevealed;

        public bool IsOpen => _open;

        private void Awake()
        {
            _font = UIStyle.ResolveFont(font);
            EnsureEventSystem();
            BuildUI();
            Apply();
            _root.SetActive(false);
        }

        public void Open()
        {
            _open = true;
            _openedFrame = Time.frameCount;
            _root.SetActive(true);
            _group.blocksRaycasts = true;
            _typing = false;
            _bodyText.text = string.Empty;
            ClearChoices();
            _indicator.gameObject.SetActive(false);
        }

        public void Close()
        {
            _open = false;
            _typing = false;
            _group.blocksRaycasts = false;
            ClearChoices();
        }

        public void ShowLine(DialogueCharacterSO speaker, DialogueLine line)
        {
            _line = line;
            ClearChoices();
            _indicator.gameObject.SetActive(false);

            bool hasPortrait = speaker != null && speaker.Portrait != null;
            _portraitTile.gameObject.SetActive(hasPortrait);
            if (hasPortrait) _portraitImage.sprite = speaker.Portrait;

            bool hasName = speaker != null && !string.IsNullOrEmpty(speaker.DisplayName);
            _namePlate.gameObject.SetActive(hasName);
            if (hasName)
            {
                _nameText.text = speaker.DisplayName;
                _namePlate.color = speaker.AccentColor;
            }

            _namePlateRect.anchoredPosition = new Vector2(hasPortrait ? PortraitSize + 60f : 40f, 0f);
            UIStyle.Stretch(_bodyText.rectTransform, hasPortrait ? PortraitSize + 56f : 44f, 26f, 96f, 48f);

            _speed = speaker != null && speaker.TypingSpeed > 0f ? speaker.TypingSpeed : charactersPerSecond;
            _fullText = line.Text;
            _shown = 0;
            _charTimer = 0f;
            _nextDelay = 0f;
            _bodyText.text = string.Empty;
            _typing = true;

            if (_fullText.Length == 0) FinishTyping();
        }

        private void Update()
        {
            AnimateWindow();
            if (!_open) return;

            if (_typing) UpdateTyping(Time.unscaledDeltaTime);
            if (_indicator.gameObject.activeSelf)
                _indicator.anchoredPosition = new Vector2(-24f, 20f + Mathf.Sin(Time.unscaledTime * 6f) * 4f);

            if (Time.frameCount <= _openedFrame) return;

            if (GameInput.CancelPressed)
            {
                CancelRequested?.Invoke();
                return;
            }

            if (_choices.Count > 0) HandleChoiceInput();
            else if (GameInput.ConfirmPressed || GameInput.ClickPressed) OnAdvance();
        }

        private void OnAdvance()
        {
            if (_typing)
            {
                _shown = _fullText.Length;
                FinishTyping();
                return;
            }

            if (_line != null && !_line.HasChoices) AdvanceRequested?.Invoke();
        }

        private void UpdateTyping(float dt)
        {
            _charTimer += dt;
            while (_typing && _charTimer >= _nextDelay)
            {
                _charTimer -= _nextDelay;
                char revealed = _fullText[_shown];
                _shown++;
                CharacterRevealed?.Invoke(revealed);

                _nextDelay = 1f / _speed + PauseAfter(revealed);
                if (_shown >= _fullText.Length) FinishTyping();
            }

            if (_typing) _bodyText.text = _fullText.Substring(0, _shown);
        }

        private void FinishTyping()
        {
            _typing = false;
            _bodyText.text = _fullText;

            if (_line != null && _line.HasChoices) BuildChoices(_line.Choices);
            else _indicator.gameObject.SetActive(true);
        }

        private static float PauseAfter(char c)
        {
            switch (c)
            {
                case '.':
                case '!':
                case '?':
                    return 0.16f;
                case ',':
                case ';':
                case ':':
                    return 0.07f;
                default:
                    return 0f;
            }
        }

        private void BuildChoices(IReadOnlyList<DialogueChoice> choices)
        {
            for (int i = 0; i < choices.Count; i++)
            {
                DialogueChoiceButton button = DialogueChoiceButton.Create(_choicesContainer, i, choices[i].Label, _font);
                button.Hovered += SetSelectedChoice;
                button.Clicked += PickChoice;
                _choices.Add(button);
            }
            SetSelectedChoice(0);
        }

        private void ClearChoices()
        {
            foreach (DialogueChoiceButton button in _choices)
            {
                button.transform.SetParent(null, false);
                Destroy(button.gameObject);
            }
            _choices.Clear();
        }

        private void SetSelectedChoice(int index)
        {
            _selectedChoice = Mathf.Clamp(index, 0, Mathf.Max(0, _choices.Count - 1));
            for (int i = 0; i < _choices.Count; i++) _choices[i].SetSelected(i == _selectedChoice);
        }

        private void PickChoice(int index)
        {
            if (index < 0 || index >= _choices.Count) return;

            ClearChoices();
            ChoiceSelected?.Invoke(index);
        }

        private void HandleChoiceInput()
        {
            if (GameInput.NavigateUpPressed) SetSelectedChoice((_selectedChoice - 1 + _choices.Count) % _choices.Count);
            if (GameInput.NavigateDownPressed) SetSelectedChoice((_selectedChoice + 1) % _choices.Count);

            if (GameInput.TryGetChoicePressed(_choices.Count, out int shortcut))
            {
                PickChoice(shortcut);
                return;
            }

            if (GameInput.ConfirmPressed) PickChoice(_selectedChoice);
        }

        private void AnimateWindow()
        {
            float target = _open ? 1f : 0f;
            if (Mathf.Approximately(_t, target)) return;

            _t = Mathf.MoveTowards(_t, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, animationDuration));
            Apply();
            if (!_open && _t <= 0f) _root.SetActive(false);
        }

        private void Apply()
        {
            float eased = _open ? EaseOutBack(_t) : _t * _t * (3f - 2f * _t);
            _group.alpha = Mathf.Clamp01(_t * 1.6f);
            _window.anchoredPosition = new Vector2(0f, -Mathf.LerpUnclamped(BoxHeight + 110f, 0f, eased));
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private void BuildUI()
        {
            var canvasObject = new GameObject("DialogueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.sortingOrder = 95;
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

            var windowObject = new GameObject("Window", typeof(RectTransform));
            windowObject.transform.SetParent(_root.transform, false);
            _window = (RectTransform)windowObject.transform;
            UIStyle.Stretch(_window);

            Image box = UIStyle.CreateImage(_window, "Box", UIStyle.Rounded(30), UIStyle.Panel);
            box.raycastTarget = true;
            var boxRect = (RectTransform)box.transform;
            boxRect.anchorMin = boxRect.anchorMax = boxRect.pivot = new Vector2(0.5f, 0f);
            boxRect.sizeDelta = new Vector2(BoxWidth, BoxHeight);
            boxRect.anchoredPosition = new Vector2(0f, BoxBottomMargin);
            UIStyle.AddShadowBehind(boxRect, 34f, new Vector2(0f, -10f), 0.45f);

            _portraitTile = UIStyle.CreateImage(boxRect, "PortraitTile", UIStyle.Rounded(26), UIStyle.Tray);
            var tileRect = (RectTransform)_portraitTile.transform;
            tileRect.anchorMin = tileRect.anchorMax = tileRect.pivot = new Vector2(0f, 0.5f);
            tileRect.sizeDelta = new Vector2(PortraitSize, PortraitSize);
            tileRect.anchoredPosition = new Vector2(26f, 0f);

            _portraitImage = UIStyle.CreateImage(tileRect, "Portrait", null, Color.white);
            _portraitImage.preserveAspect = true;
            UIStyle.Stretch((RectTransform)_portraitImage.transform, 8f, 8f, 8f, 8f);

            _bodyText = UIStyle.CreateText(boxRect, "Body", _font, 32, FontStyle.Normal, UIStyle.TextDark, TextAnchor.UpperLeft);
            UIStyle.Stretch(_bodyText.rectTransform, 44f, 26f, 96f, 48f);

            Image indicator = UIStyle.CreateImage(boxRect, "ContinueHint", UIStyle.Rounded(14), UIStyle.Accent);
            _indicator = (RectTransform)indicator.transform;
            _indicator.anchorMin = _indicator.anchorMax = _indicator.pivot = new Vector2(1f, 0f);
            _indicator.sizeDelta = new Vector2(46f, 46f);
            _indicator.anchoredPosition = new Vector2(-24f, 20f);
            Text hintKey = UIStyle.CreateText(_indicator, "Key", _font, 28, FontStyle.Bold, UIStyle.TextDark, TextAnchor.MiddleCenter);
            UIStyle.Stretch(hintKey.rectTransform);
            hintKey.text = "E";

            _namePlate = UIStyle.CreateImage(boxRect, "NamePlate", UIStyle.Rounded(20), UIStyle.Header);
            _namePlateRect = (RectTransform)_namePlate.transform;
            _namePlateRect.anchorMin = _namePlateRect.anchorMax = new Vector2(0f, 1f);
            _namePlateRect.pivot = new Vector2(0f, 0.5f);

            var plateLayout = _namePlate.gameObject.AddComponent<HorizontalLayoutGroup>();
            plateLayout.padding = new RectOffset(30, 30, 8, 10);
            plateLayout.childAlignment = TextAnchor.MiddleCenter;
            plateLayout.childControlWidth = true;
            plateLayout.childControlHeight = true;
            plateLayout.childForceExpandWidth = false;
            plateLayout.childForceExpandHeight = false;

            var plateFitter = _namePlate.gameObject.AddComponent<ContentSizeFitter>();
            plateFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            plateFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _nameText = UIStyle.CreateText(_namePlateRect, "Name", _font, 30, FontStyle.Bold, UIStyle.HeaderText, TextAnchor.MiddleCenter);
            _nameText.horizontalOverflow = HorizontalWrapMode.Overflow;

            var choicesObject = new GameObject("Choices", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            choicesObject.transform.SetParent(boxRect, false);
            _choicesContainer = (RectTransform)choicesObject.transform;
            _choicesContainer.anchorMin = _choicesContainer.anchorMax = new Vector2(1f, 1f);
            _choicesContainer.pivot = new Vector2(1f, 0f);
            _choicesContainer.sizeDelta = new Vector2(ChoicesWidth, 0f);
            _choicesContainer.anchoredPosition = new Vector2(-16f, 24f);

            var choicesLayout = choicesObject.GetComponent<VerticalLayoutGroup>();
            choicesLayout.spacing = 10f;
            choicesLayout.childAlignment = TextAnchor.LowerRight;
            choicesLayout.childControlWidth = true;
            choicesLayout.childControlHeight = true;
            choicesLayout.childForceExpandWidth = true;
            choicesLayout.childForceExpandHeight = false;

            var choicesFitter = choicesObject.GetComponent<ContentSizeFitter>();
            choicesFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            choicesFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
}
