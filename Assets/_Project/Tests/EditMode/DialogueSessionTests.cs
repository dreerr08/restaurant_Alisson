using AliGame.Dialogue;
using NUnit.Framework;

namespace AliGame.Tests
{
    public class DialogueSessionTests : TestBase
    {
        [Test]
        public void Advance_WalksThroughLinesAndFinishes()
        {
            var session = new DialogueSession(Dialogue(null, "one", "two"));

            Assert.AreEqual("one", session.CurrentLine.Text);
            session.Advance();
            Assert.AreEqual("two", session.CurrentLine.Text);
            Assert.IsFalse(session.IsFinished);
            session.Advance();
            Assert.IsTrue(session.IsFinished);
            Assert.IsNull(session.CurrentLine);
        }

        [Test]
        public void EmptyOrMissingDialogue_IsFinishedImmediately()
        {
            Assert.IsTrue(new DialogueSession(null).IsFinished);
            Assert.IsTrue(new DialogueSession(Dialogue(null)).IsFinished);
        }

        [Test]
        public void Advance_DoesNothingOnALineWithChoices()
        {
            var dialogue = Dialogue(null, "question");
            AddChoices(dialogue, 0, ("yes", null));
            var session = new DialogueSession(dialogue);

            session.Advance();

            Assert.IsFalse(session.IsFinished);
            Assert.AreEqual("question", session.CurrentLine.Text);
        }

        [Test]
        public void Choose_FollowsTheChosenDialogue()
        {
            var tips = Dialogue(null, "tip one", "tip two");
            var bye = Dialogue(null, "bye");
            var root = Dialogue(null, "question");
            AddChoices(root, 0, ("tips", tips), ("bye", bye));
            var session = new DialogueSession(root);

            Assert.IsTrue(session.Choose(0));

            Assert.AreEqual("tip one", session.CurrentLine.Text);
            session.Advance();
            Assert.AreEqual("tip two", session.CurrentLine.Text);
        }

        [Test]
        public void Choose_WithNoNextDialogueEndsTheConversation()
        {
            var root = Dialogue(null, "question");
            AddChoices(root, 0, ("leave", null));
            var session = new DialogueSession(root);

            Assert.IsTrue(session.Choose(0));

            Assert.IsTrue(session.IsFinished);
        }

        [Test]
        public void Choose_RejectsInvalidIndexesAndLinesWithoutChoices()
        {
            var root = Dialogue(null, "question");
            AddChoices(root, 0, ("a", null));
            var session = new DialogueSession(root);

            Assert.IsFalse(session.Choose(-1));
            Assert.IsFalse(session.Choose(1));
            Assert.IsFalse(session.IsFinished);

            var plain = new DialogueSession(Dialogue(null, "no choices here"));
            Assert.IsFalse(plain.Choose(0));
        }

        [Test]
        public void CurrentSpeaker_FallsBackToTheDialoguesDefaultSpeaker()
        {
            var speaker = Character("Tio Ben");
            var session = new DialogueSession(Dialogue(speaker, "hello"));

            Assert.AreSame(speaker, session.CurrentSpeaker);
        }
    }
}
