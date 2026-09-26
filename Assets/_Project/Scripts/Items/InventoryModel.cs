using System;
using System.Collections.Generic;
using AliGame.Data;

namespace AliGame.Items
{
    public sealed class ItemStack
    {
        public ItemSO Item { get; }
        public int Amount { get; internal set; }

        internal ItemStack(ItemSO item, int amount)
        {
            Item = item;
            Amount = amount;
        }
    }

    /// <summary>
    /// Plain inventory logic: one stack per item type, each stack capped at the item's
    /// MaxStack, and at most Capacity different item types.
    /// </summary>
    public sealed class InventoryModel : IItemCounter
    {
        private readonly List<ItemStack> _stacks = new List<ItemStack>();

        public int Capacity { get; }
        public IReadOnlyList<ItemStack> Stacks => _stacks;

        public event Action Changed;

        /// <summary>Raised after items are added, with the amount that actually fit.</summary>
        public event Action<ItemSO, int> ItemAdded;

        public event Action<RecipeSO> Crafted;

        public InventoryModel(int capacity)
        {
            Capacity = Math.Max(1, capacity);
        }

        public bool CanAdd(ItemSO item)
        {
            if (item == null) return false;

            ItemStack stack = Find(item);
            return stack != null ? stack.Amount < item.MaxStack : _stacks.Count < Capacity;
        }

        /// <summary>Adds as many as fit and returns how many were actually added.</summary>
        public int Add(ItemSO item, int amount)
        {
            if (item == null || amount <= 0) return 0;

            ItemStack stack = Find(item);
            if (stack == null)
            {
                if (_stacks.Count >= Capacity) return 0;
                stack = new ItemStack(item, 0);
                _stacks.Add(stack);
            }

            int added = Math.Min(amount, item.MaxStack - stack.Amount);
            if (added <= 0) return 0;

            stack.Amount += added;
            Changed?.Invoke();
            ItemAdded?.Invoke(item, added);
            return added;
        }

        /// <summary>Removes the amount only if the inventory has all of it.</summary>
        public bool Remove(ItemSO item, int amount)
        {
            if (item == null || amount <= 0) return false;

            ItemStack stack = Find(item);
            if (stack == null || stack.Amount < amount) return false;

            stack.Amount -= amount;
            if (stack.Amount == 0) _stacks.Remove(stack);
            Changed?.Invoke();
            return true;
        }

        /// <summary>True if the recipe belongs to this station and all its ingredients are in the inventory.</summary>
        public bool CanCraft(RecipeSO recipe, StationType station) => CanCraft(recipe, station, 1);

        /// <summary>True if the recipe belongs to this station and the inventory has enough for Quantity crafts at once.</summary>
        public bool CanCraft(RecipeSO recipe, StationType station, int quantity)
        {
            if (recipe == null || recipe.Station != station || quantity < 1) return false;
            if (recipe.Result == null || recipe.Ingredients.Count == 0) return false;

            Dictionary<ItemSO, int> needs = SumIngredients(recipe);
            if (needs == null) return false;

            foreach (KeyValuePair<ItemSO, int> need in needs)
            {
                if (Count(need.Key) < need.Value * quantity) return false;
            }
            return true;
        }

        /// <summary>How many times in a row this recipe could be crafted with what is in the inventory right now.</summary>
        public int MaxCraftable(RecipeSO recipe)
        {
            if (recipe == null || recipe.Result == null || recipe.Ingredients.Count == 0) return 0;

            Dictionary<ItemSO, int> needs = SumIngredients(recipe);
            if (needs == null || needs.Count == 0) return 0;

            int max = int.MaxValue;
            foreach (KeyValuePair<ItemSO, int> need in needs)
                max = Math.Min(max, Count(need.Key) / need.Value);
            return max;
        }

        /// <summary>
        /// Consumes the ingredients and raises Crafted. The result is NOT added to the inventory: whoever
        /// listens to Crafted (the station) spawns it in the world as an ItemPickup.
        /// Returns false and does nothing if it can't be crafted at this station.
        /// </summary>
        public bool Craft(RecipeSO recipe, StationType station)
        {
            if (!CanCraft(recipe, station)) return false;

            foreach (KeyValuePair<ItemSO, int> need in SumIngredients(recipe))
                Remove(need.Key, need.Value);

            Crafted?.Invoke(recipe);
            return true;
        }

        private static Dictionary<ItemSO, int> SumIngredients(RecipeSO recipe)
        {
            var needs = new Dictionary<ItemSO, int>();
            foreach (RecipeSO.Ingredient ingredient in recipe.Ingredients)
            {
                if (ingredient.Item == null) return null;
                needs.TryGetValue(ingredient.Item, out int current);
                needs[ingredient.Item] = current + ingredient.Amount;
            }
            return needs;
        }

        public int Count(ItemSO item)
        {
            ItemStack stack = Find(item);
            return stack != null ? stack.Amount : 0;
        }

        public bool Has(ItemSO item, int amount = 1)
        {
            return Count(item) >= amount;
        }

        private ItemStack Find(ItemSO item)
        {
            if (item == null) return null;
            foreach (ItemStack stack in _stacks)
            {
                if (stack.Item == item) return stack;
            }
            return null;
        }
    }
}
