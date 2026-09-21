using System;
using AliGame.Data;
using UnityEngine;

namespace AliGame.Items
{
    /// <summary>Put this on the player. Wraps InventoryModel so other scripts and the UI can use it.</summary>
    public class Inventory : MonoBehaviour
    {
        [SerializeField, Min(1)] private int capacity = 12;

        private InventoryModel _model;

        public InventoryModel Model => _model ??= new InventoryModel(capacity);

        public event Action Changed
        {
            add => Model.Changed += value;
            remove => Model.Changed -= value;
        }

        public event Action<ItemSO, int> ItemAdded
        {
            add => Model.ItemAdded += value;
            remove => Model.ItemAdded -= value;
        }

        public event Action<RecipeSO> Crafted
        {
            add => Model.Crafted += value;
            remove => Model.Crafted -= value;
        }

        public bool CanCraft(RecipeSO recipe, StationType station) => Model.CanCraft(recipe, station);

        public bool Craft(RecipeSO recipe, StationType station) => Model.Craft(recipe, station);

        public bool CanAdd(ItemSO item) => Model.CanAdd(item);

        public int Add(ItemSO item, int amount = 1) => Model.Add(item, amount);

        public bool Remove(ItemSO item, int amount = 1) => Model.Remove(item, amount);

        public bool Has(ItemSO item, int amount = 1) => Model.Has(item, amount);

        public int Count(ItemSO item) => Model.Count(item);
    }
}
