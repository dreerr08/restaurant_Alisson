using System;

namespace AliGame.Farming
{
    public enum PlotState
    {
        Empty,
        /// <summary>Planted, waiting for water. It waits as long as needed; nothing dies.</summary>
        NeedsWater,
        /// <summary>Watered: it advances when the next day starts.</summary>
        Growing,
        Ready
    }

    /// <summary>
    /// The rules of one garden plot, without any Unity types. Plant a seed, water it, and every new day the watered
    /// plant gets one day closer to the end of its stage. When the last stage ends it is ready to harvest.
    /// A stage that takes several days keeps growing on its own after one watering.
    /// </summary>
    public class PlotModel
    {
        private int[] _stageDays = Array.Empty<int>();
        private bool _waterEveryStage = true;
        private int _daysLeft;

        public PlotState State { get; private set; } = PlotState.Empty;

        /// <summary>Index of the stage being grown; equals the stage count when Ready.</summary>
        public int Stage { get; private set; }

        public int StageCount => _stageDays.Length;

        /// <summary>Days left in the current stage while Growing.</summary>
        public int DaysLeft => State == PlotState.Growing ? _daysLeft : 0;

        /// <summary>0 to 1 across the whole plant, for drawing it bigger as it grows.</summary>
        public float Progress
        {
            get
            {
                if (State == PlotState.Empty || _stageDays.Length == 0) return 0f;
                if (State == PlotState.Ready) return 1f;
                return (float)Stage / _stageDays.Length;
            }
        }

        public event Action Changed;

        /// <summary>Plants a crop with the given stage lengths. False if the plot is taken or the crop has no stages.</summary>
        public bool Plant(int[] stageDays, bool waterEveryStage)
        {
            if (State != PlotState.Empty || stageDays == null || stageDays.Length == 0) return false;

            _stageDays = (int[])stageDays.Clone();
            for (int i = 0; i < _stageDays.Length; i++) _stageDays[i] = Math.Max(1, _stageDays[i]);

            _waterEveryStage = waterEveryStage;
            Stage = 0;
            State = PlotState.NeedsWater;
            Changed?.Invoke();
            return true;
        }

        public bool Water()
        {
            if (State != PlotState.NeedsWater) return false;

            State = PlotState.Growing;
            _daysLeft = _stageDays[Stage];
            Changed?.Invoke();
            return true;
        }

        /// <summary>Call once for every day that starts. Only a watered (Growing) plant advances.</summary>
        public void NewDay()
        {
            if (State != PlotState.Growing) return;

            _daysLeft--;
            if (_daysLeft > 0)
            {
                Changed?.Invoke();
                return;
            }

            Stage++;
            if (Stage >= _stageDays.Length)
            {
                State = PlotState.Ready;
            }
            else if (_waterEveryStage)
            {
                State = PlotState.NeedsWater;
            }
            else
            {
                _daysLeft = _stageDays[Stage];
            }
            Changed?.Invoke();
        }

        /// <summary>Takes the crop out of a Ready plot and empties it.</summary>
        public bool Harvest()
        {
            if (State != PlotState.Ready) return false;

            _stageDays = Array.Empty<int>();
            Stage = 0;
            State = PlotState.Empty;
            Changed?.Invoke();
            return true;
        }
    }
}
