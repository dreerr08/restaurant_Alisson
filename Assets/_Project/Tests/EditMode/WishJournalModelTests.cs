using System.Collections.Generic;
using AliGame.Data;
using AliGame.Wishes;
using NUnit.Framework;

namespace AliGame.Tests
{
    public class WishJournalModelTests : TestBase
    {
        private WishSO _first;
        private WishSO _second;
        private NpcStorySO _story;
        private WishJournalModel _journal;

        [SetUp]
        public void CreateStory()
        {
            DialogueSO delivery = Dialogue(null, "thanks");
            _first = Wish("first", Dialogue(null, "please"), delivery, null, (Item("Flour"), 2));
            _second = Wish("second", Dialogue(null, "one more"), delivery, null, (Item("Cheese"), 1));
            _story = Story(Character("Ben"), new[] { _first, _second });
            _journal = new WishJournalModel();
        }

        [Test]
        public void CurrentWish_WalksTheChainAsWishesAreCompleted()
        {
            Assert.AreSame(_first, _journal.CurrentWish(_story));

            _journal.Offer(_story);
            _journal.CompleteCurrent(_story);
            Assert.AreSame(_second, _journal.CurrentWish(_story));

            _journal.Offer(_story);
            _journal.CompleteCurrent(_story);
            Assert.IsNull(_journal.CurrentWish(_story), "the arc is over");
            Assert.IsTrue(_journal.IsStoryComplete(_story));
        }

        [Test]
        public void Offer_MakesTheWishActiveAndRaisesAccepted()
        {
            WishSO accepted = null;
            _journal.WishAccepted += (story, wish) => accepted = wish;

            Assert.IsFalse(_journal.IsOffered(_story), "not asked for yet");
            _journal.Offer(_story);

            Assert.IsTrue(_journal.IsOffered(_story));
            Assert.AreSame(_first, accepted);
        }

        [Test]
        public void Offer_TwiceRaisesAcceptedOnlyOnce()
        {
            int accepted = 0;
            _journal.WishAccepted += (story, wish) => accepted++;

            _journal.Offer(_story);
            _journal.Offer(_story);

            Assert.AreEqual(1, accepted);
        }

        [Test]
        public void CompleteCurrent_RaisesCompletedAndClearsTheOfferedFlag()
        {
            WishSO completed = null;
            _journal.WishCompleted += (story, wish) => completed = wish;
            _journal.Offer(_story);

            Assert.AreSame(_first, _journal.CompleteCurrent(_story));

            Assert.AreSame(_first, completed);
            Assert.AreEqual(1, _journal.CompletedCount(_story));
            Assert.IsFalse(_journal.IsOffered(_story), "the next wish still has to be asked for");
        }

        [Test]
        public void StoryCompleted_IsRaisedOnlyOnTheLastWish()
        {
            int storyCompleted = 0;
            _journal.StoryCompleted += story => storyCompleted++;

            _journal.Offer(_story);
            _journal.CompleteCurrent(_story);
            Assert.AreEqual(0, storyCompleted);

            _journal.Offer(_story);
            _journal.CompleteCurrent(_story);
            Assert.AreEqual(1, storyCompleted);
        }

        [Test]
        public void CompleteCurrent_OnAFinishedStoryDoesNothing()
        {
            _journal.Offer(_story);
            _journal.CompleteCurrent(_story);
            _journal.Offer(_story);
            _journal.CompleteCurrent(_story);

            Assert.IsNull(_journal.CompleteCurrent(_story));
            Assert.AreEqual(2, _journal.CompletedCount(_story));
        }

        [Test]
        public void ActiveWishes_ListsOnlyWhatWasAskedForAndNotDelivered()
        {
            _journal.Register(_story);
            Assert.AreEqual(0, _journal.ActiveWishes().Count, "nothing has been asked for yet");

            _journal.Offer(_story);
            List<(NpcStorySO Story, WishSO Wish)> active = _journal.ActiveWishes();
            Assert.AreEqual(1, active.Count);
            Assert.AreSame(_first, active[0].Wish);

            _journal.CompleteCurrent(_story);
            Assert.AreEqual(0, _journal.ActiveWishes().Count, "delivered, and the next one was not asked for yet");
        }

        [Test]
        public void Reminders_RotateOnlyWhenAdvanced()
        {
            DialogueSO a = Dialogue(null, "a");
            DialogueSO b = Dialogue(null, "b");
            WishSO wish = Wish("w", Dialogue(null, "please"), Dialogue(null, "thanks"), new[] { a, b }, (Item("Flour"), 1));
            NpcStorySO story = Story(Character("Ben"), new[] { wish });
            var journal = new WishJournalModel();
            journal.Offer(story);

            Assert.AreSame(a, journal.PeekReminder(story, wish));
            Assert.AreSame(a, journal.PeekReminder(story, wish), "peeking does not consume");

            journal.AdvanceReminder(story);
            Assert.AreSame(b, journal.PeekReminder(story, wish));

            journal.AdvanceReminder(story);
            Assert.AreSame(a, journal.PeekReminder(story, wish), "wraps around");
        }

        [Test]
        public void MarkFarewellSeen_IsRemembered()
        {
            Assert.IsFalse(_journal.FarewellSeen(_story));

            _journal.MarkFarewellSeen(_story);

            Assert.IsTrue(_journal.FarewellSeen(_story));
        }

        [Test]
        public void NullStory_IsHandledEverywhere()
        {
            Assert.IsNull(_journal.CurrentWish(null));
            Assert.IsFalse(_journal.IsOffered(null));
            Assert.IsFalse(_journal.FarewellSeen(null));
            Assert.AreEqual(0, _journal.CompletedCount(null));
            Assert.IsNull(_journal.CompleteCurrent(null));
            Assert.DoesNotThrow(() => _journal.Offer(null));
            Assert.DoesNotThrow(() => _journal.Register(null));
        }
    }
}
