using System;
using System.Collections.Generic;
using UnityEngine;

namespace AliGame.Data
{
    /// <summary>An answer the player can pick after a line. Next is the dialogue that continues; empty ends the conversation.</summary>
    [Serializable]
    public class DialogueChoice
    {
        [SerializeField] private string label;
        [SerializeField] private DialogueSO next;
        [Tooltip("Optional id sent to DialogueRunner.EventTriggered when this choice is picked (give an item, start a quest...).")]
        [SerializeField] private string eventId;

        public string Label => label;
        public DialogueSO Next => next;
        public string EventId => eventId;
    }

    [Serializable]
    public class DialogueLine
    {
        [Tooltip("Who says it. Empty uses the dialogue's Default Speaker.")]
        [SerializeField] private DialogueCharacterSO speaker;
        [SerializeField, TextArea(2, 5)] private string text;
        [Tooltip("If not empty, the conversation waits for the player to pick one of these after the text.")]
        [SerializeField] private List<DialogueChoice> choices = new List<DialogueChoice>();
        [Tooltip("Optional id sent to DialogueRunner.EventTriggered when this line is shown.")]
        [SerializeField] private string eventId;

        public DialogueCharacterSO Speaker => speaker;
        public string Text => text ?? string.Empty;
        public IReadOnlyList<DialogueChoice> Choices => choices;
        public bool HasChoices => choices != null && choices.Count > 0;
        public string EventId => eventId;
    }

    /// <summary>A conversation: a list of lines, optionally branching through choices into other DialogueSOs.</summary>
    [CreateAssetMenu(fileName = "NewDialogue", menuName = "Ali/Dialogue")]
    public class DialogueSO : ScriptableObject
    {
        [SerializeField] private DialogueCharacterSO defaultSpeaker;
        [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

        public DialogueCharacterSO DefaultSpeaker => defaultSpeaker;
        public IReadOnlyList<DialogueLine> Lines => lines;
    }
}
