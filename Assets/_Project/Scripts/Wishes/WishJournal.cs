using System;
using AliGame.Data;
using UnityEngine;

namespace AliGame.Wishes
{
    /// <summary>Put this on the player, next to the Inventory. Holds what every character has asked for.</summary>
    public class WishJournal : MonoBehaviour
    {
        private WishJournalModel _model;

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
    }
}
