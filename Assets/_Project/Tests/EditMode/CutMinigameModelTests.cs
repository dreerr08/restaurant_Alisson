using AliGame.Items;
using NUnit.Framework;

namespace AliGame.Tests
{
    public class CutMinigameModelTests
    {
        private static CutMinigameModel Started(float hold = 3f, int cuts = 3, float window = 1f)
        {
            var model = new CutMinigameModel(hold, cuts, window);
            model.Update(false, 0.1f);
            return model;
        }

        [Test]
        public void KeyHeldWhenOpening_DoesNotStartCharging()
        {
            var model = new CutMinigameModel(3f, 3);

            model.Update(true, 1f);
            Assert.AreEqual(CutPhase.WaitingRelease, model.Phase);

            model.Update(false, 0.1f);
            Assert.AreEqual(CutPhase.Idle, model.Phase);
        }

        [Test]
        public void FullBar_OpensTheWindowButDoesNotCountYet()
        {
            var model = Started();
            int opened = 0;
            int succeeded = 0;
            model.WindowOpened += () => opened++;
            model.CutSucceeded += n => succeeded++;

            model.Update(true, 1f);
            model.Update(true, 1f);
            Assert.AreEqual(CutPhase.Charging, model.Phase);
            model.Update(true, 1f);

            Assert.AreEqual(1, opened);
            Assert.AreEqual(0, succeeded);
            Assert.AreEqual(0, model.CutsDone);
            Assert.AreEqual(CutPhase.Window, model.Phase);
        }

        [Test]
        public void ReleasingInsideTheWindow_CountsTheCut()
        {
            var model = Started(1f, 3, 1f);
            int cuts = -1;
            model.CutSucceeded += n => cuts = n;

            model.Update(true, 1f);
            model.Update(true, 0.6f);
            model.Update(false, 0.1f);

            Assert.AreEqual(1, cuts);
            Assert.AreEqual(1, model.CutsDone);
            Assert.AreEqual(CutPhase.Idle, model.Phase);
        }

        [Test]
        public void HoldingPastTheWindow_MissesAndDoesNotCount()
        {
            var model = Started(1f, 3, 1f);
            int missed = 0;
            model.CutMissed += () => missed++;

            model.Update(true, 1f);
            model.Update(true, 0.6f);
            model.Update(true, 0.6f);

            Assert.AreEqual(1, missed);
            Assert.AreEqual(CutPhase.Missed, model.Phase);

            model.Update(false, 0.1f);
            Assert.AreEqual(0, model.CutsDone, "releasing after the window must not give the point");
            Assert.AreEqual(CutPhase.Idle, model.Phase);
        }

        [Test]
        public void AfterAMiss_KeepingTheKeyDown_DoesNotStartTheNextCharge()
        {
            var model = Started(1f, 3, 1f);
            model.Update(true, 1f);
            model.Update(true, 1.5f);

            model.Update(true, 5f);

            Assert.AreEqual(CutPhase.Missed, model.Phase);
            Assert.AreEqual(0f, model.Charge);
        }

        [Test]
        public void OneBigFrame_PastTheWholeWindow_MissesAtOnce()
        {
            var model = Started(1f, 3, 1f);
            int missed = 0;
            model.CutMissed += () => missed++;

            model.Update(true, 5f);

            Assert.AreEqual(1, missed);
            Assert.AreEqual(CutPhase.Missed, model.Phase);
        }

        [Test]
        public void ReleasingEarly_DoesNotCountAndKeepsEarlierCuts()
        {
            var model = Started(1f, 3, 1f);
            model.Update(true, 1f);
            model.Update(false, 0.1f);
            Assert.AreEqual(1, model.CutsDone);

            bool aborted = false;
            model.ChargeAborted += () => aborted = true;
            model.Update(true, 0.5f);
            model.Update(true, 0.3f);
            model.Update(false, 0.1f);

            Assert.IsTrue(aborted);
            Assert.AreEqual(1, model.CutsDone);
            Assert.AreEqual(0f, model.Charge);
            Assert.AreEqual(CutPhase.Idle, model.Phase);
        }

        [Test]
        public void MissedCuts_DoNotLoseTheOnesAlreadyMade()
        {
            var model = Started(1f, 3, 1f);
            model.Update(true, 1f);
            model.Update(false, 0.1f);
            Assert.AreEqual(1, model.CutsDone);

            model.Update(true, 1f);
            model.Update(true, 2f);
            model.Update(false, 0.1f);

            Assert.AreEqual(1, model.CutsDone);
        }

        [Test]
        public void GoodReleaseOnTheLastCut_FinishesOnce()
        {
            var model = Started(1f, 2, 1f);
            int finished = 0;
            model.FinishedAll += () => finished++;

            model.Update(true, 1f);
            model.Update(false, 0.1f);
            model.Update(true, 1f);
            Assert.AreEqual(0, finished, "still holding: nothing finishes before the release");
            model.Update(false, 0.1f);

            Assert.AreEqual(1, finished);
            Assert.AreEqual(CutPhase.Finished, model.Phase);
            Assert.IsTrue(model.IsOver);
        }

        [Test]
        public void MissingTheLastCut_DoesNotFinish()
        {
            var model = Started(1f, 1, 1f);
            int finished = 0;
            model.FinishedAll += () => finished++;

            model.Update(true, 1f);
            model.Update(true, 1.5f);
            model.Update(false, 0.1f);

            Assert.AreEqual(0, finished);
            Assert.IsFalse(model.IsOver);
        }

        [Test]
        public void Cancel_StopsTheMinigame()
        {
            var model = Started(1f, 2, 1f);
            model.Update(true, 0.5f);

            model.Cancel();
            model.Update(true, 5f);

            Assert.AreEqual(CutPhase.Cancelled, model.Phase);
            Assert.AreEqual(0, model.CutsDone);
        }

        [Test]
        public void InvalidSettings_AreClamped()
        {
            var model = new CutMinigameModel(0f, 0, 0f);

            Assert.Greater(model.HoldSeconds, 0f);
            Assert.AreEqual(1, model.RequiredCuts);
            Assert.Greater(model.ReleaseWindow, 0f);
        }
    }
}
