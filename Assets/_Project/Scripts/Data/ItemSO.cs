using UnityEngine;

namespace AliGame.Data
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "Ali/Item")]
    public class ItemSO : ScriptableObject
    {
        [SerializeField] private string displayName = "Novo Item";
        [SerializeField, TextArea] private string description;
        [Tooltip("Shown only in the inventory UI. The pickup in the world uses its own SpriteRenderer.")]
        [SerializeField] private Sprite icon;
        [Tooltip("Prefab spawned in the world when this item is crafted. Should have an ItemPickup; if empty a basic pickup using the icon is created.")]
        [SerializeField] private GameObject pickupPrefab;
        [SerializeField, Min(1)] private int maxStack = 99;

        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public GameObject PickupPrefab => pickupPrefab;
        public int MaxStack => Mathf.Max(1, maxStack);
    }
}
