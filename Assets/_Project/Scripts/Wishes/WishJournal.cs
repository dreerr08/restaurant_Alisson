using System;
using System.Collections.Generic;
using AliGame.Data;
using AliGame.Farming;
using UnityEngine;

namespace AliGame.Wishes
{
    /// <summary>
    /// Put this on the player, next to the Inventory. Holds what every character has asked for, how well they know
    /// the player, and the triggers that have been raised: the outside events characters wait for before they ask
    /// for a wish (set as a wish's Start Trigger). Triggers come from a game day set here, from a StoryTriggerZone
    /// placed in the scene, from another wish or story finishing, or from any script or UnityEvent calling RaiseTrigger.
    /// </summary>
    public class WishJournal : MonoBehaviour
    {
        [Serializable]
        public struct DayTrigger
        {
            [Tooltip("The game day (see GameClock) from which the trigger counts as raised. Day 1 is the first day.")]
            [SerializeField, Min(1)] private int day;
            [Tooltip("The trigger id, the same text a wish has as its Start Trigger.")]
            [SerializeField] private string trigger;

            public int Day => Mathf.Max(1, day);
            public string Trigger => trigger;
        }

        [Header("Time triggers")]
        [Tooltip("Triggers raised when a game day is reached. A wish whose Start Trigger is one of these is only asked for from that day on.")]
        [SerializeField] private List<DayTrigger> dayTriggers = new List<DayTrigger>();

        private WishJournalModel _model;
        private GameClock _clock;

        public WishJournalModel Model => _model ??= new WishJournalModel();

        public event Action Changed
        {
            add => Model.Changed += value;
            remove => Model.Changed -= value;
        }

        public event Action<NpcStorySO, WishSO> WishAccepted
        {
            add => Model.WishAccepted += value;
            remove => Model.WishAccepted -= value;
        }

        public event Action<NpcStorySO, WishSO> WishCompleted
        {
            add => Model.WishCompleted += value;
            remove => Model.WishCompleted -= value;
        }

        public event Action<NpcStorySO> StoryCompleted
        {
            add => Model.StoryCompleted += value;
            remove => Model.StoryCompleted -= value;
        }

        /// <summary>A trigger was raised for the first time. The argument is its id.</summary>
        public event Action<string> TriggerRaised
        {
            add => Model.TriggerRaised += value;
            remove => Model.TriggerRaised -= value;
        }

        /// <summary>Raises an outside event that characters may be waiting for. Safe to call again; it only counts once.</summary>
        public void RaiseTrigger(string id) => Model.RaiseTrigger(id);

        private void OnEnable()
        {
            _clock = GameClock.Current != null ? GameClock.Current : FindFirstObjectByType<GameClock>();
            if (_clock == null) return;

            _clock.DayStarted += ApplyDayTriggers;
            ApplyDayTriggers(_clock.Day);
        }

        private void OnDisable()
        {
            if (_clock != null) _clock.DayStarted -= ApplyDayTriggers;
            _clock = null;
        }

        // A day trigger stays raised from its day on, so a trigger for day 2 is also raised when the game is already on day 5.
        private void ApplyDayTriggers(int day)
        {
            foreach (DayTrigger dayTrigger in dayTriggers)
            {
                if (day >= dayTrigger.Day) Model.RaiseTrigger(dayTrigger.Trigger);
            }
        }
    }
}
