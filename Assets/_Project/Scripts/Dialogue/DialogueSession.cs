using AliGame.Data;

namespace AliGame.Dialogue
{
    /// <summary>
    /// Plain dialogue flow: which line is current, moving forward and following choices.
    /// It knows nothing about UI, input or the scene.
    /// </summary>
    public sealed class DialogueSession
    {
        private DialogueSO _dialogue;
        private int _index;

        public bool IsFinished { get; private set; }

        public DialogueLine CurrentLine => IsFinished ? null : _dialogue.Lines[_index];

        public DialogueCharacterSO CurrentSpeaker => CurrentLine?.Speaker ?? _dialogue?.DefaultSpeaker;

        public DialogueSession(DialogueSO dialogue)
        {
            _dialogue = dialogue;
            IsFinished = dialogue == null || dialogue.Lines.Count == 0;
        }

        /// <summary>Moves to the next line. Does nothing on a line with choices (a choice must be picked) or when finished.</summary>
        public void Advance()
        {
            if (IsFinished || CurrentLine.HasChoices) return;

            _index++;
            if (_index >= _dialogue.Lines.Count) IsFinished = true;
        }

        /// <summary>Follows a choice of the current line. Returns false if the index isn't valid.</summary>
        public bool Choose(int choiceIndex)
        {
            DialogueLine line = CurrentLine;
            if (line == null || !line.HasChoices || choiceIndex < 0 || choiceIndex >= line.Choices.Count) return false;

            DialogueSO next = line.Choices[choiceIndex].Next;
            if (next == null || next.Lines.Count == 0)
            {
                IsFinished = true;
                return true;
            }

            _dialogue = next;
            _index = 0;
            return true;
        }
    }
}
