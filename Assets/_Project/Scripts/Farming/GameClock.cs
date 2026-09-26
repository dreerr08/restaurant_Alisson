using System;
using UnityEngine;

namespace AliGame.Farming
{
    /// <summary>
    /// The game's calendar. A day lasts Day Minutes of play time (15 by default) and then the next one starts; things that
    /// grow (plants) count in days. Only one should exist in the scene. It runs on game time, so it keeps going while
    /// dialogue or panels are open and stops only when the game itself is paused.
    /// </summary>
    public class GameClock : MonoBehaviour
    {
        [Header("Day")]
        [Tooltip("How long a day lasts, in minutes of play.")]
        [SerializeField, Min(0.1f)] private float dayMinutes = 15f;
        [SerializeField, Min(1)] private int startDay = 1;
        [Tooltip("Where in the day the game starts: 0 is the beginning, 0.5 the middle.")]
        [SerializeField, Range(0f, 0.99f)] private float startTime = 0f;

        private DayClockModel _model;

        public static GameClock Current { get; private set; }

        public int Day => Model.Day;

        /// <summary>How far through the day it is, 0 to 1.</summary>
        public float Time01 => Model.Time01;

        /// <summary>Raised when a new day starts, with its number.</summary>
        public event Action<int> DayStarted
        {
            add => Model.DayStarted += value;
            remove => Model.DayStarted -= value;
        }

        private DayClockModel Model => _model ??= new DayClockModel(dayMinutes * 60f, startDay, startTime);

        private void Awake()
        {
            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void Update()
        {
            Model.Advance(Time.deltaTime);
        }

        /// <summary>Testing shortcut: jumps to the start of the next day (right-click the component title).</summary>
        [ContextMenu("Skip to next day")]
        public void SkipToNextDay()
        {
            Model.SkipToNextDay();
        }
    }
}
