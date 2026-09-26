using System;

namespace AliGame.Farming
{
    /// <summary>The calendar without any Unity types: a day is DaySeconds long, and Advance tells how many days started.</summary>
    public class DayClockModel
    {
        private float _elapsed;

        public DayClockModel(float daySeconds, int startDay = 1, float startTime01 = 0f)
        {
            DaySeconds = Math.Max(1f, daySeconds);
            Day = Math.Max(1, startDay);
            _elapsed = Math.Max(0f, Math.Min(0.9999f, startTime01)) * DaySeconds;
        }

        public float DaySeconds { get; }

        /// <summary>The current day, starting at 1.</summary>
        public int Day { get; private set; }

        /// <summary>How far through the current day it is, 0 to 1.</summary>
        public float Time01 => _elapsed / DaySeconds;

        /// <summary>Raised for every day that starts, with its number.</summary>
        public event Action<int> DayStarted;

        /// <summary>Moves time forward. Returns how many days started (more than one only after a very long step).</summary>
        public int Advance(float seconds)
        {
            if (seconds <= 0f) return 0;

            _elapsed += seconds;
            int started = 0;
            while (_elapsed >= DaySeconds)
            {
                _elapsed -= DaySeconds;
                Day++;
                started++;
                DayStarted?.Invoke(Day);
            }
            return started;
        }

        /// <summary>Skips to the start of the next day.</summary>
        public void SkipToNextDay()
        {
            Advance(DaySeconds - _elapsed);
        }
    }
}
