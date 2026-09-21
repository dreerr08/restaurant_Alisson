using AliGame.Data;

namespace AliGame.Items
{
    /// <summary>Anything that can say how many of an item is held. Lets other systems ask without knowing about the inventory.</summary>
    public interface IItemCounter
    {
        int Count(ItemSO item);
    }
}
