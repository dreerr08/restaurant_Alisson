using AliGame.Data;
using AliGame.Wishes;
using NUnit.Framework;

namespace AliGame.Tests
{
    public class WishDialogueResolverTests : TestBase
    {
        private ItemSO _flour;
        private DialogueSO _offer;
        private DialogueSO _reminder;
        private DialogueSO _delivery;
        private DialogueSO _idle;
        private DialogueSO _farewell;
        private WishSO _wish;
        private NpcStorySO _story;
        private WishJournalModel _journal;
        private FakeItemCounter _items;

        [SetUp]
        public void CreateStory()
        {
            _flour = Item("Flour");
            _offer = Dialogue(null, "bring me flour");
            _reminder = Dialogue(null, "still waiting on that flour");
            _delivery = Dialogue(null, "you brought it!");
            _idle = Dialogue(null, "nice weather");
            _farewell = Dialogue(null, "goodbye");
            _wish = Wish("flour", _offer, _delivery, new[] { _reminder }, (_flour, 2));
            _story = Story(Character("Ben"), new[] { _wish }, new[] { _idle }, _farewell);
            _journal = new WishJournalModel();
            _items = new FakeItemCounter();
        }

        [Test]
        public void BeforeBeingAsked_TheCharacterOffersTheWish()
        {
            WishDialogueResult result = WishDialogueResolver.Resolve(_story, _journal, _items);

            Assert.AreEqual(WishDialogueKind.Offer, result.Kind);
            Assert.AreSame(_offer, result.Dialogue);
            Assert.AreSame(_wish, result.Wish);
        }

        [Test]
        public void WithTheWishActiveAndNoItems_TheCharacterReminds()
        {
            _journal.Offer(_story);

            WishDialogueResult result = WishDialogueResolver.Resolve(_story, _journal, _items);

            Assert.AreEqual(WishDialogueKind.Reminder, result.Kind);
            Assert.AreSame(_reminder, result.Dialogue);
        }

        [Test]
        public void WithSomeButNotAllItems_TheCharacterStillReminds()
        {
            _journal.Offer(_story);
            _items.Set(_flour, 1);

            Assert.AreEqual(WishDialogueKind.Reminder, WishDialogueResolver.Resolve(_story, _journal, _items).Kind);
        }

        [Test]
        public void CarryingEverything_TheDeliveryDialoguePlays()
        {
            _journal.Offer(_story);
            _items.Set(_flour, 2);

            WishDialogueResult result = WishDialogueResolver.Resolve(_story, _journal, _items);

            Assert.AreEqual(WishDialogueKind.Delivery, result.Kind);
            Assert.AreSame(_delivery, result.Dialogue);
            Assert.AreSame(_wish, result.Wish);
        }

        [Test]
        public void CarryingEverythingBeforeBeingAsked_TheOfferStillComesFirst()
        {
            _items.Set(_flour, 5);

            Assert.AreEqual(WishDialogueKind.Offer, WishDialogueResolver.Resolve(_story, _journal, _items).Kind);
        }

        [Test]
        public void WithTheArcOver_TheFarewellPlaysOnceAndThenSmallTalk()
        {
            _journal.Offer(_story);
            _journal.CompleteCurrent(_story);

            WishDialogueResult farewell = WishDialogueResolver.Resolve(_story, _journal, _items);
            Assert.AreEqual(WishDialogueKind.Farewell, farewell.Kind);
            Assert.AreSame(_farewell, farewell.Dialogue);

            _journal.MarkFarewellSeen(_story);

            WishDialogueResult after = WishDialogueResolver.Resolve(_story, _journal, _items);
            Assert.AreEqual(WishDialogueKind.Idle, after.Kind);
            Assert.AreSame(_idle, after.Dialogue);
        }

        [Test]
        public void WithoutReminderDialogues_TheCharacterFallsBackToSmallTalk()
        {
            WishSO wish = Wish("flour", _offer, _delivery, null, (_flour, 1));
            NpcStorySO story = Story(Character("Ben"), new[] { wish }, new[] { _idle });
            var journal = new WishJournalModel();
            journal.Offer(story);

            WishDialogueResult result = WishDialogueResolver.Resolve(story, journal, _items);

            Assert.AreEqual(WishDialogueKind.Idle, result.Kind);
        }

        [Test]
        public void AStoryWithNoWishes_OnlyEverSmallTalks()
        {
            NpcStorySO story = Story(Character("Ben"), new WishSO[0], new[] { _idle });

            WishDialogueResult result = WishDialogueResolver.Resolve(story, new WishJournalModel(), _items);

            Assert.AreEqual(WishDialogueKind.Idle, result.Kind);
            Assert.AreSame(_idle, result.Dialogue);
        }

        [Test]
        public void WithNothingAuthored_TheResultIsNoneAndNotAnError()
        {
            NpcStorySO empty = Story(Character("Ben"), new WishSO[0]);

            Assert.AreEqual(WishDialogueKind.None, WishDialogueResolver.Resolve(empty, new WishJournalModel(), _items).Kind);
            Assert.AreEqual(WishDialogueKind.None, WishDialogueResolver.Resolve(null, _journal, _items).Kind);
            Assert.AreEqual(WishDialogueKind.None, WishDialogueResolver.Resolve(_story, null, _items).Kind);
        }

        [Test]
        public void HasEverything_RequiresEveryItemAndRejectsBrokenWishes()
        {
            ItemSO cheese = Item("Cheese");
            WishSO two = Wish("two", _offer, _delivery, null, (_flour, 2), (cheese, 1));
            _items.Set(_flour, 2);

            Assert.IsFalse(WishDialogueResolver.HasEverything(two, _items), "cheese is missing");

            _items.Set(cheese, 1);
            Assert.IsTrue(WishDialogueResolver.HasEverything(two, _items));

            WishSO empty = Wish("empty", _offer, _delivery, null);
            Assert.IsFalse(WishDialogueResolver.HasEverything(empty, _items), "a wish that asks for nothing is broken");
            Assert.IsFalse(WishDialogueResolver.HasEverything(null, _items));
        }
    }
}
