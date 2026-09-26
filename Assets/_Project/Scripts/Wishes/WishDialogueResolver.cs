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
        Idle,
        /// <summary>A talk that builds the relationship. The character asks for nothing until these are done.</summary>
        Intro
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
    /// player is carrying. Before their first wish they play their introduction talks in order; a wish with a
    /// start trigger is only asked for once that trigger has been raised (until then they make small talk).
    /// Plain logic: no scene, no UI, no input.
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
                // First they get to know the player; only then can an outside event make them ask.
                DialogueSO intro = journal.PeekIntro(story);
                if (intro != null)
                    return new WishDialogueResult(intro, WishDialogueKind.Intro);

                if (current.HasStartTrigger && !journal.IsTriggerRaised(current.StartTrigger))
                    return Idle(story, journal);

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
