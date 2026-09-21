using AliGame.Data;
using UnityEngine;

namespace AliGame.Dialogue
{
    /// <summary>Who is talking to whom in a running conversation.</summary>
    public sealed class DialogueContext
    {
        public DialogueSO Dialogue { get; }

        /// <summary>The character being talked to (the NPC).</summary>
        public Transform Speaker { get; }

        /// <summary>The character talking to them (the player).</summary>
        public Transform Listener { get; }

        public DialogueContext(DialogueSO dialogue, Transform speaker, Transform listener)
        {
            Dialogue = dialogue;
            Speaker = speaker;
            Listener = listener;
        }
    }
}
