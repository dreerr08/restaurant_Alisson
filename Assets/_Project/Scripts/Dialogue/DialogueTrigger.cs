using System;
using AliGame.Core;
using AliGame.Data;
using UnityEngine;
using UnityEngine.Events;

namespace AliGame.Dialogue
{
    /// <summary>
    /// Put on anything the player can talk to. Standing close shows "[E] Falar com ..." and pressing E starts
    /// the dialogue. Other components on the same object (e.g. NpcConversationBehaviour) react through
    /// DialogueStarted / DialogueEnded, and so can anything in the Inspector through the UnityEvents.
    /// </summary>
    public class DialogueTrigger : Interactable
    {
        [SerializeField] private DialogueSO dialogue;
        [Tooltip("Text of the prompt. Empty uses 'Falar com <speaker name>'.")]
        [SerializeField] private string promptText;
        [SerializeField] private DialogueRunner runner;
        [SerializeField] private UnityEvent onDialogueStarted;
        [SerializeField] private UnityEvent onDialogueEnded;

        public event Action<DialogueContext> DialogueStarted;

        public event Action<DialogueContext> DialogueEnded;

        public DialogueSO Dialogue
        {
            get => dialogue;
            set => dialogue = value;
        }

        protected override bool CanInteract => runner != null && !runner.IsRunning && dialogue != null && dialogue.Lines.Count > 0;

        protected override string PromptText
        {
            get
            {
                if (!string.IsNullOrEmpty(promptText)) return promptText;

                DialogueCharacterSO speaker = dialogue != null ? dialogue.DefaultSpeaker : null;
                return speaker != null ? "Falar com " + speaker.DisplayName : "Conversar";
            }
        }

        protected override void Initialize()
        {
            if (runner == null) runner = FindFirstObjectByType<DialogueRunner>();
            if (runner == null) Debug.LogWarning("DialogueTrigger: no DialogueRunner found in the scene.", this);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (runner == null) return;

            runner.Started += OnRunnerStarted;
            runner.Ended += OnRunnerEnded;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (runner == null) return;

            runner.Started -= OnRunnerStarted;
            runner.Ended -= OnRunnerEnded;
        }

        protected override void Interact(GameObject interactor)
        {
            runner.StartDialogue(dialogue, transform, interactor.transform);
        }

        // Any conversation that has this object as speaker counts, no matter who started it.
        private void OnRunnerStarted(DialogueContext context)
        {
            if (context.Speaker != transform) return;

            DialogueStarted?.Invoke(context);
            onDialogueStarted.Invoke();
        }

        private void OnRunnerEnded(DialogueContext context)
        {
            if (context.Speaker != transform) return;

            DialogueEnded?.Invoke(context);
            onDialogueEnded.Invoke();
        }
    }
}
