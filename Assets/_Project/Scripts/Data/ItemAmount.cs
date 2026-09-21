using System;
using UnityEngine;

namespace AliGame.Data
{
    /// <summary>An item and how many of it.</summary>
    [Serializable]
    public struct ItemAmount
    {
        [SerializeField] private ItemSO item;
        [SerializeField, Min(1)] private int amount;

        public ItemSO Item => item;
        public int Amount => Mathf.Max(1, amount);
    }
}
