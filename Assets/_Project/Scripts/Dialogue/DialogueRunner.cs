using System;
using AliGame.Data;
using AliGame.Movement;
using AliGame.UI;
using UnityEngine;

namespace AliGame.Dialogue
{
    /// <summary>
    /// Runs one conversation at a time: drives a DialogueSession, tells the view what to show and locks the
    /// player's input while it lasts. Put one in the scene next to a view (the default is DialogueUI).
    /// Other systems can react through Started / Ended / LineShown / EventTriggered.
    /// </summary>
    public class DialogueRunner : MonoBehaviour
    {
        [Tooltip("Any component implementing IDialogueView. If empty, one is found in the scene.")]
        [SerializeField] private MonoBehaviour viewBehaviour;
        [SerializeField] private bool lockPlayerInput = true;

        private IDialogueView _view;
        private DialogueSession _session;
        private DialogueContext _context;
        private PlayerMovement2D _player;
        private bool _playerLocked;

        public bool IsRunning => _session != null;

        public DialogueContext Context => _context;

        public event Action<DialogueContext> Started;

        public event Action<DialogueContext> Ended;

        public event Action<DialogueContext, DialogueLine> LineShown;

        /// <summary>Raised with the event id of a line when it is shown, or of a choice when it is picked.</summary>
        public event Action<DialogueContext, string> EventTriggered;

        private void Awake()
        {
            _view = viewBehaviour as IDialogueView ?? FindView();
            if (_view != null) return;

            Debug.LogWarning("DialogueRunner: no IDialogueView found in the scene (add a DialogueUI).", this);
            enabled = false;
        }

        private void OnEnable()
        {
            if (_view == null) return;
            _view.AdvanceRequested += OnAdvance;
            _view.ChoiceSelected += OnChoice;
            _view.CancelRequested += End;
            UIPanels.Opened += OnOtherPanelOpened;
        }

        private void OnDisable()
        {
            if (_view != null)
            {
                _view.AdvanceRequested -= OnAdvance;
                _view.ChoiceSelected -= OnChoice;
                _view.CancelRequested -= End;
            }
            UIPanels.Opened -= OnOtherPanelOpened;
            if (IsRunning) End();
        }

        /// <summary>Starts a conversation. Returns its context, or null if one is already running or there is nothing to say.</summary>
        public DialogueContext StartDialogue(DialogueSO dialogue, Transform speaker, Transform listener)
        {
            if (!enabled || IsRunning || dialogue == null || dialogue.Lines.Count == 0) return null;

            UIPanels.NotifyOpened(this);

            _session = new DialogueSession(dialogue);
            _context = new DialogueContext(dialogue, speaker, listener);

            _player = listener != null ? listener.GetComponent<PlayerMovement2D>() : null;
            if (lockPlayerInput && _player != null)
            {
                _player.LockInput();
                _playerLocked = true;
            }

            _view.Open();
            Started?.Invoke(_context);
            ShowCurrentLine();
            return _context;
        }

        public void End()
        {
            if (!IsRunning) return;

            DialogueContext finished = _context;
            _session = null;
            _context = null;

            _view.Close();
            if (_playerLocked && _player != null) _player.UnlockInput();
            _playerLocked = false;
            _player = null;

            Ended?.Invoke(finished);
        }

        private void ShowCurrentLine()
        {
            DialogueLine line = _session.CurrentLine;
            _view.ShowLine(_session.CurrentSpeaker, line);
            LineShown?.Invoke(_context, line);
            RaiseEvent(line.EventId);
        }

        private void OnAdvance()
        {
            if (!IsRunning) return;

            _session.Advance();
            if (_session.IsFinished) End();
            else ShowCurrentLine();
        }

        private void OnChoice(int index)
        {
            if (!IsRunning) return;

            DialogueLine line = _session.CurrentLine;
            string eventId = line.HasChoices && index >= 0 && index < line.Choices.Count ? line.Choices[index].EventId : null;

            if (!_session.Choose(index)) return;

            RaiseEvent(eventId);
            if (!IsRunning) return;

            if (_session.IsFinished) End();
            else ShowCurrentLine();
        }

        private void OnOtherPanelOpened(object panel)
        {
            if (panel != (object)this && IsRunning) End();
        }

        private void RaiseEvent(string eventId)
        {
            if (!string.IsNullOrEmpty(eventId)) EventTriggered?.Invoke(_context, eventId);
        }

        private static IDialogueView FindView()
        {
            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (behaviour is IDialogueView view) return view;
            }
            return null;
        }
    }
}
