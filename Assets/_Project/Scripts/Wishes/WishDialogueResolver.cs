using AliGame.Data;
using AliGame.Items;

namespace AliGame.Wishes
{
    public enum WishDialogueKind
    {
        None,
        Offer,
        Reminder,
        Delivery,
        Farewell,
        Idle
    }

    public readonly struct WishDialogueResult
    {
        public readonly DialogueSO Dialogue;
        public readonly WishDialogueKind Kind;
        public readonly WishSO Wish;

        public WishDialogueResult(DialogueSO dialogue, WishDialogueKind kind, WishSO wish = null)
        {
            Dialogue = dialogue;
            Kind = kind;
            Wish = wish;
        }

        public static WishDialogueResult None => new WishDialogueResult(null, WishDialogueKind.None);
    }

    /// <summary>
    /// Picks which dialogue a character should say right now, from their story, the journal and what the
    /// player is carrying. Plain logic: no scene, no UI, no input.
    /// </summary>
    public static class WishDialogueResolver
    {
        public static WishDialogueResult Resolve(NpcStorySO story, WishJournalModel journal, IItemCounter items)
        {
            if (story == null || journal == null) return WishDialogueResult.None;

            WishSO current = journal.CurrentWish(story);

            if (current != null && journal.IsOffered(story))
            {
                if (HasEverything(current, items) && current.DeliveryDialogue != null)
                    return new WishDialogueResult(current.DeliveryDialogue, WishDialogueKind.Delivery, current);

                DialogueSO reminder = journal.PeekReminder(story, current);
                if (reminder != null)
                    return new WishDialogueResult(reminder, WishDialogueKind.Reminder, current);

                return Idle(story, journal);
            }

            if (current != null)
            {
                if (current.OfferDialogue != null)
                    return new WishDialogueResult(current.OfferDialogue, WishDialogueKind.Offer, current);

                return Idle(story, journal);
            }

            if (!journal.FarewellSeen(story) && story.FarewellDialogue != null)
                return new WishDialogueResult(story.FarewellDialogue, WishDialogueKind.Farewell);

            return Idle(story, journal);
        }

        /// <summary>True when the player carries everything the wish asks for.</summary>
        public static bool HasEverything(WishSO wish, IItemCounter items)
        {
            if (wish == null || items == null || wish.Requirements.Count == 0) return false;

            foreach (ItemAmount requirement in wish.Requirements)
            {
                if (requirement.Item == null) return false;
                if (items.Count(requirement.Item) < requirement.Amount) return false;
            }
            return true;
        }

        private static WishDialogueResult Idle(NpcStorySO story, WishJournalModel journal)
        {
            DialogueSO idle = journal.PeekIdle(story);
            return idle != null
                ? new WishDialogueResult(idle, WishDialogueKind.Idle)
                : WishDialogueResult.None;
        }
    }
}
