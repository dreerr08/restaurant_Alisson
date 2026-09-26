using AliGame.Data;
using AliGame.Wishes;
using NUnit.Framework;

namespace AliGame.Tests
{
    /// <summary>
    /// The relationship before the first request (introduction talks) and the outside trigger that lets a
    /// character start asking.
    /// </summary>
    public class RelationshipTests : TestBase
    {
        private ItemSO _flour;
        private DialogueSO _offer;
        private DialogueSO _delivery;
        private DialogueSO _idle;
        private DialogueSO _intro1;
        private DialogueSO _intro2;
        private WishSO _wish;
        private WishJournalModel _journal;
        private FakeItemCounter _items;

        [SetUp]
        public void CreateParts()
        {
            _flour = Item("Flour");
            _offer = Dialogue(null, "bring me flour");
            _delivery = Dialogue(null, "you brought it!");
            _idle = Dialogue(null, "nice weather");
            _intro1 = Dialogue(null, "nice to meet you");
            _intro2 = Dialogue(null, "tell me about yourself");
            _wish = Wish("flour", _offer, _delivery, null, (_flour, 2));
            _journal = new WishJournalModel();
            _items = new FakeItemCounter();
        }

        private NpcStorySO StoryWithIntros(params DialogueSO[] intros)
        {
            return Story(Character("Ben"), new[] { _wish }, new[] { _idle }, null, intros);
        }

        private WishDialogueResult Resolve(NpcStorySO story) => WishDialogueResolver.Resolve(story, _journal, _items);

        [Test]
        public void WithoutIntroductions_TheCharacterAsksAtOnce()
        {
            NpcStorySO story = StoryWithIntros();

            Assert.IsTrue(_journal.IsAcquainted(story));
            Assert.AreEqual(WishDialogueKind.Offer, Resolve(story).Kind);
        }

        [Test]
        public void BeforeTheIntroductionsAreDone_TheCharacterOnlyTalksAndDoesNotAsk()
        {
            NpcStorySO story = StoryWithIntros(_intro1, _intro2);

            WishDialogueResult result = Resolve(story);

            Assert.IsFalse(_journal.IsAcquainted(story));
            Assert.AreEqual(WishDialogueKind.Intro, result.Kind);
            Assert.AreSame(_intro1, result.Dialogue);
        }

        [Test]
        public void EachFinishedTalk_MovesToTheNextIntroduction()
        {
            NpcStorySO story = StoryWithIntros(_intro1, _intro2);

            _journal.MarkIntroSeen(story);

            WishDialogueResult result = Resolve(story);
            Assert.AreEqual(WishDialogueKind.Intro, result.Kind);
            Assert.AreSame(_intro2, result.Dialogue);
        }

        [Test]
        public void AfterTheLastIntroduction_TheCharacterAsks_AndStaysAcquainted()
        {
            NpcStorySO story = StoryWithIntros(_intro1, _intro2);
            int acquainted = 0;
            _journal.StoryAcquainted += s => acquainted++;

            _journal.MarkIntroSeen(story);
            _journal.MarkIntroSeen(story);

            Assert.IsTrue(_journal.IsAcquainted(story));
            Assert.AreEqual(1, acquainted);
            Assert.AreEqual(WishDialogueKind.Offer, Resolve(story).Kind);

            _journal.MarkIntroSeen(story);
            Assert.AreEqual(1, acquainted, "marking past the end changes nothing");
        }

        [Test]
        public void AnEmptyIntroductionSlot_IsSkippedInsteadOfBlockingTheStory()
        {
            NpcStorySO story = StoryWithIntros(null, _intro1);

            Assert.AreSame(_intro1, Resolve(story).Dialogue);

            _journal.MarkIntroSeen(story);

            Assert.IsTrue(_journal.IsAcquainted(story));
        }

        [Test]
        public void AWishWithAStartTrigger_IsNotAskedForUntilTheTriggerIsRaised()
        {
            SetStartTrigger(_wish, "day-2");
            NpcStorySO story = StoryWithIntros();

            WishDialogueResult waiting = Resolve(story);
            Assert.AreEqual(WishDialogueKind.Idle, waiting.Kind, "they make small talk while waiting");
            Assert.AreSame(_idle, waiting.Dialogue);

            _journal.RaiseTrigger("day-2");

            Assert.AreEqual(WishDialogueKind.Offer, Resolve(story).Kind);
        }

        [Test]
        public void TheIntroductionsComeBeforeTheTrigger_EvenWhenTheTriggerWasRaisedEarly()
        {
            SetStartTrigger(_wish, "day-2");
            NpcStorySO story = StoryWithIntros(_intro1);
            _journal.RaiseTrigger("day-2");

            Assert.AreEqual(WishDialogueKind.Intro, Resolve(story).Kind, "they still have to get to know the player");

            _journal.MarkIntroSeen(story);

            Assert.AreEqual(WishDialogueKind.Offer, Resolve(story).Kind);
        }

        [Test]
        public void RaisingATrigger_IsRememberedAndOnlyAnnouncedOnce()
        {
            int raised = 0;
            _journal.TriggerRaised += id => raised++;

            _journal.RaiseTrigger("garden");
            _journal.RaiseTrigger("  garden ");

            Assert.AreEqual(1, raised);
            Assert.IsTrue(_journal.IsTriggerRaised("garden"));
            Assert.IsFalse(_journal.IsTriggerRaised("kitchen"));
        }

        [Test]
        public void ABlankTrigger_IsNeverRaised()
        {
            int raised = 0;
            _journal.TriggerRaised += id => raised++;

            _journal.RaiseTrigger(null);
            _journal.RaiseTrigger("   ");

            Assert.AreEqual(0, raised);
            Assert.IsFalse(_journal.IsTriggerRaised(""));
            Assert.IsFalse(_journal.IsTriggerRaised(null));
        }

        [Test]
        public void DeliveringAWish_RaisesTheTriggerOfThatWish()
        {
            NpcStorySO story = StoryWithIntros();
            _journal.Offer(story);

            _journal.CompleteCurrent(story);

            Assert.IsTrue(_journal.IsTriggerRaised(WishJournalModel.WishCompleteTrigger(_wish)));
        }

        [Test]
        public void FinishingAStory_RaisesItsTrigger_SoAnotherCharacterCanStartAsking()
        {
            NpcStorySO first = StoryWithIntros();
            first.name = "FirstStory";
            DialogueSO offer2 = Dialogue(null, "my turn");
            WishSO wish2 = Wish("second", offer2, _delivery, null, (_flour, 1));
            SetStartTrigger(wish2, WishJournalModel.StoryCompleteTrigger(first));
            NpcStorySO second = Story(Character("Mia"), new[] { wish2 }, new[] { _idle });

            Assert.AreEqual(WishDialogueKind.Idle, Resolve(second).Kind, "the first story is not done yet");

            _journal.Offer(first);
            _journal.CompleteCurrent(first);

            Assert.AreEqual(WishDialogueKind.Offer, Resolve(second).Kind);
        }

        [Test]
        public void TheLaterWishes_DoNotNeedTheIntroductionsAgain()
        {
            DialogueSO offer2 = Dialogue(null, "one more thing");
            WishSO wish2 = Wish("second", offer2, _delivery, null, (_flour, 1));
            NpcStorySO story = Story(Character("Ben"), new[] { _wish, wish2 }, new[] { _idle }, null, new[] { _intro1 });

            _journal.MarkIntroSeen(story);
            _journal.Offer(story);
            _journal.CompleteCurrent(story);

            Assert.AreEqual(WishDialogueKind.Offer, Resolve(story).Kind);
            Assert.AreSame(offer2, Resolve(story).Dialogue);
        }
    }
}
