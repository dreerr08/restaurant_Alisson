using System;
using System.Collections.Generic;
using AliGame.Data;

namespace AliGame.Wishes
{
    /// <summary>
    /// Tracks how far each character's story has gone: which wish is current, whether it has been asked for yet,
    /// and which reminder comes next. Plain logic, so it can be tested and saved without the scene.
    /// </summary>
    public sealed class WishJournalModel
    {
        private sealed class Entry
        {
            public int Completed;
            public bool Offered;
            public int ReminderIndex;
            public bool FarewellSeen;
        }

        private readonly Dictionary<NpcStorySO, Entry> _entries = new Dictionary<NpcStorySO, Entry>();
        private readonly List<NpcStorySO> _order = new List<NpcStorySO>();

        public event Action Changed;

        /// <summary>The character asked for something and it went into the journal.</summary>
        public event Action<NpcStorySO, WishSO> WishAccepted;

        /// <summary>The items were handed over and the wish is done.</summary>
        public event Action<NpcStorySO, WishSO> WishCompleted;

        /// <summary>The last wish of a character's arc was delivered.</summary>
        public event Action<NpcStorySO> StoryCompleted;

        /// <summary>Makes the journal aware of a story (its wishes only show up once they are asked for).</summary>
        public void Register(NpcStorySO story)
        {
            if (story == null) return;
            GetOrCreate(story);
        }

        /// <summary>The wish being asked for or waited on, or null when the arc is over.</summary>
        public WishSO CurrentWish(NpcStorySO story)
        {
            if (story == null) return null;

            Entry entry = GetOrCreate(story);
            return entry.Completed < story.Wishes.Count ? story.Wishes[entry.Completed] : null;
        }

        /// <summary>True once the character has asked for the current wish.</summary>
        public bool IsOffered(NpcStorySO story)
        {
            return story != null && GetOrCreate(story).Offered && CurrentWish(story) != null;
        }

        /// <summary>True while the player owes this character something.</summary>
        public bool IsActive(NpcStorySO story) => IsOffered(story);

        public bool IsStoryComplete(NpcStorySO story) => story != null && CurrentWish(story) == null;

        public bool FarewellSeen(NpcStorySO story) => story != null && GetOrCreate(story).FarewellSeen;

        public int CompletedCount(NpcStorySO story) => story != null ? GetOrCreate(story).Completed : 0;

        /// <summary>Marks the current wish as asked for, so from now on the character reminds instead of offering.</summary>
        public void Offer(NpcStorySO story)
        {
            WishSO wish = CurrentWish(story);
            if (wish == null) return;

            Entry entry = GetOrCreate(story);
            if (entry.Offered) return;

            entry.Offered = true;
            entry.ReminderIndex = 0;
            WishAccepted?.Invoke(story, wish);
            Changed?.Invoke();
        }

        /// <summary>Hands the current wish in and moves the arc forward. Returns the wish that was completed.</summary>
        public WishSO CompleteCurrent(NpcStorySO story)
        {
            WishSO wish = CurrentWish(story);
            if (wish == null) return null;

            Entry entry = GetOrCreate(story);
            entry.Completed++;
            entry.Offered = false;
            entry.ReminderIndex = 0;

            WishCompleted?.Invoke(story, wish);
            if (CurrentWish(story) == null) StoryCompleted?.Invoke(story);
            Changed?.Invoke();
            return wish;
        }

        public void MarkFarewellSeen(NpcStorySO story)
        {
            if (story == null) return;

            Entry entry = GetOrCreate(story);
            if (entry.FarewellSeen) return;

            entry.FarewellSeen = true;
            Changed?.Invoke();
        }

        /// <summary>The reminder that should play next, without consuming it.</summary>
        public DialogueSO PeekReminder(NpcStorySO story, WishSO wish)
        {
            if (story == null || wish == null || wish.ReminderDialogues.Count == 0) return null;

            Entry entry = GetOrCreate(story);
            return wish.ReminderDialogues[entry.ReminderIndex % wish.ReminderDialogues.Count];
        }

        /// <summary>Moves to the next reminder, so talking again is not identical.</summary>
        public void AdvanceReminder(NpcStorySO story)
        {
            if (story == null) return;
            GetOrCreate(story).ReminderIndex++;
        }

        /// <summary>The small talk that should play next, without consuming it.</summary>
        public DialogueSO PeekIdle(NpcStorySO story)
        {
            if (story == null || story.IdleDialogues.Count == 0) return null;

            Entry entry = GetOrCreate(story);
            return story.IdleDialogues[entry.ReminderIndex % story.IdleDialogues.Count];
        }

        /// <summary>Every wish the player has been asked for and has not delivered yet, in the order they were asked.</summary>
        public List<(NpcStorySO Story, WishSO Wish)> ActiveWishes()
        {
            var active = new List<(NpcStorySO, WishSO)>();
            foreach (NpcStorySO story in _order)
            {
                if (!IsOffered(story)) continue;

                WishSO wish = CurrentWish(story);
                if (wish != null) active.Add((story, wish));
            }
            return active;
        }

        private Entry GetOrCreate(NpcStorySO story)
        {
            if (_entries.TryGetValue(story, out Entry entry)) return entry;

            entry = new Entry();
            _entries[story] = entry;
            _order.Add(story);
            return entry;
        }
    }
}
