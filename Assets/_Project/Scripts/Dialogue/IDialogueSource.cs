using AliGame.Data;

namespace AliGame.Dialogue
{
    /// <summary>
    /// Decides which dialogue a DialogueTrigger should play right now. Put one on the same object as the
    /// trigger (e.g. NpcWishGiver) to make the conversation follow the character's state instead of being fixed.
    /// </summary>
    public interface IDialogueSource
    {
        /// <summary>The dialogue to play now, or null to let the trigger fall back to its own.</summary>
        DialogueSO GetDialogue();
    }
}
