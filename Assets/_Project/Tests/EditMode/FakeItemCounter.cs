using System.Collections.Generic;
using AliGame.Data;
using AliGame.Items;

namespace AliGame.Tests
{
    /// <summary>A stand-in for the inventory in tests that only need "how many of X do I have".</summary>
    public sealed class FakeItemCounter : IItemCounter
    {
        private readonly Dictionary<ItemSO, int> _counts = new Dictionary<ItemSO, int>();

        public FakeItemCounter Set(ItemSO item, int amount)
        {
            _counts[item] = amount;
            return this;
        }

        public int Count(ItemSO item)
        {
            return item != null && _counts.TryGetValue(item, out int amount) ? amount : 0;
        }
    }
}
