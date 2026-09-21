using System;
using AliGame.Data;

namespace AliGame.Dialogue
{
    /// <summary>
    /// What the DialogueRunner needs from a dialogue screen. Implement it to replace the default UI
    /// (a different look, a world-space bubble...) without touching the rest of the system.
    /// </summary>
    public interface IDialogueView
    {
        /// <summary>The player asked to continue (only raised when the line has no choices and finished typing).</summary>
        event Action AdvanceRequested;

        /// <summary>The player picked a choice of the current line.</summary>
        event Action<int> ChoiceSelected;

        /// <summary>The player wants to leave the conversation.</summary>
        event Action CancelRequested;

        void Open();

        void Close();

        void ShowLine(DialogueCharacterSO speaker, DialogueLine line);
    }
}
