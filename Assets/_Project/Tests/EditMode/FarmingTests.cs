using AliGame.Farming;
using NUnit.Framework;

namespace AliGame.Tests
{
    public class PlotModelTests
    {
        [Test]
        public void NewPlot_IsEmpty_AndDaysDoNothing()
        {
            var plot = new PlotModel();

            plot.NewDay();

            Assert.AreEqual(PlotState.Empty, plot.State);
            Assert.IsFalse(plot.Water());
            Assert.IsFalse(plot.Harvest());
        }

        [Test]
        public void Plant_WaitsForWater()
        {
            var plot = new PlotModel();

            Assert.IsTrue(plot.Plant(new[] { 1, 1 }, true));

            Assert.AreEqual(PlotState.NeedsWater, plot.State);
            Assert.IsFalse(plot.Plant(new[] { 1 }, true), "a taken plot cannot be planted again");
        }

        [Test]
        public void Plant_RejectsACropWithoutStages()
        {
            var plot = new PlotModel();

            Assert.IsFalse(plot.Plant(new int[0], true));
            Assert.AreEqual(PlotState.Empty, plot.State);
        }

        [Test]
        public void UnwateredPlant_NeverGrowsAndNeverDies()
        {
            var plot = new PlotModel();
            plot.Plant(new[] { 1, 1 }, true);

            for (int i = 0; i < 10; i++) plot.NewDay();

            Assert.AreEqual(PlotState.NeedsWater, plot.State);
            Assert.AreEqual(0, plot.Stage);
        }

        [Test]
        public void WateredPlant_AdvancesWhenTheNextDayStarts()
        {
            var plot = new PlotModel();
            plot.Plant(new[] { 1, 1 }, true);

            Assert.IsTrue(plot.Water());
            Assert.AreEqual(PlotState.Growing, plot.State);
            Assert.AreEqual(0, plot.Stage, "watering alone does not grow it");

            plot.NewDay();

            Assert.AreEqual(1, plot.Stage);
            Assert.AreEqual(PlotState.NeedsWater, plot.State);
        }

        [Test]
        public void EveryStageNeedsItsOwnWatering_WhenConfigured()
        {
            var plot = new PlotModel();
            plot.Plant(new[] { 1, 1, 1 }, true);

            plot.Water();
            plot.NewDay();
            plot.NewDay();
            Assert.AreEqual(1, plot.Stage, "the second day passed without water");

            plot.Water();
            plot.NewDay();
            plot.Water();
            plot.NewDay();

            Assert.AreEqual(PlotState.Ready, plot.State);
        }

        [Test]
        public void WaterOnlyOnce_WhenTheCropDoesNotNeedWateringEveryStage()
        {
            var plot = new PlotModel();
            plot.Plant(new[] { 1, 2 }, false);

            plot.Water();
            plot.NewDay();
            Assert.AreEqual(PlotState.Growing, plot.State);

            plot.NewDay();
            plot.NewDay();

            Assert.AreEqual(PlotState.Ready, plot.State);
        }

        [Test]
        public void AStageOfSeveralDays_GrowsOnItsOwnAfterOneWatering()
        {
            var plot = new PlotModel();
            plot.Plant(new[] { 3 }, true);
            plot.Water();

            plot.NewDay();
            plot.NewDay();
            Assert.AreEqual(PlotState.Growing, plot.State);
            Assert.AreEqual(1, plot.DaysLeft);

            plot.NewDay();
            Assert.AreEqual(PlotState.Ready, plot.State);
        }

        [Test]
        public void Harvest_EmptiesThePlotSoItCanBePlantedAgain()
        {
            var plot = new PlotModel();
            plot.Plant(new[] { 1 }, true);
            plot.Water();
            plot.NewDay();

            Assert.IsTrue(plot.Harvest());

            Assert.AreEqual(PlotState.Empty, plot.State);
            Assert.IsTrue(plot.Plant(new[] { 1 }, true));
        }

        [Test]
        public void Changed_IsRaisedOnEveryStep()
        {
            var plot = new PlotModel();
            int changes = 0;
            plot.Changed += () => changes++;

            plot.Plant(new[] { 1 }, true);
            plot.Water();
            plot.NewDay();
            plot.Harvest();

            Assert.AreEqual(4, changes);
        }

        [Test]
        public void Progress_GrowsFromZeroToOne()
        {
            var plot = new PlotModel();
            Assert.AreEqual(0f, plot.Progress);

            plot.Plant(new[] { 1, 1 }, true);
            Assert.AreEqual(0f, plot.Progress);

            plot.Water();
            plot.NewDay();
            Assert.AreEqual(0.5f, plot.Progress, 0.001f);

            plot.Water();
            plot.NewDay();
            Assert.AreEqual(1f, plot.Progress);
        }
    }

    public class DayClockModelTests
    {
        [Test]
        public void Advance_RaisesDayStartedWhenTheDayEnds()
        {
            var clock = new DayClockModel(900f);
            int started = -1;
            clock.DayStarted += day => started = day;

            clock.Advance(899f);
            Assert.AreEqual(1, clock.Day);
            Assert.AreEqual(-1, started);

            clock.Advance(2f);
            Assert.AreEqual(2, clock.Day);
            Assert.AreEqual(2, started);
        }

        [Test]
        public void ALongStep_StartsEveryDayItCrosses()
        {
            var clock = new DayClockModel(100f);
            int count = 0;
            clock.DayStarted += day => count++;

            int started = clock.Advance(250f);

            Assert.AreEqual(2, started);
            Assert.AreEqual(2, count);
            Assert.AreEqual(3, clock.Day);
            Assert.AreEqual(0.5f, clock.Time01, 0.001f);
        }

        [Test]
        public void SkipToNextDay_StartsExactlyOneDay()
        {
            var clock = new DayClockModel(900f, 1, 0.4f);

            clock.SkipToNextDay();

            Assert.AreEqual(2, clock.Day);
            Assert.AreEqual(0f, clock.Time01, 0.001f);
        }

        [Test]
        public void StartTime_IsRespected()
        {
            var clock = new DayClockModel(900f, 5, 0.25f);

            Assert.AreEqual(5, clock.Day);
            Assert.AreEqual(0.25f, clock.Time01, 0.001f);
        }
    }
}
