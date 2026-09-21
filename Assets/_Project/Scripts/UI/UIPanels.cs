using System;

namespace AliGame.UI
{
    /// <summary>Lets full-screen panels (inventory, crafting...) close each other so only one is open at a time.</summary>
    public static class UIPanels
    {
        public static event Action<object> Opened;

        public static void NotifyOpened(object panel) => Opened?.Invoke(panel);
    }
}
