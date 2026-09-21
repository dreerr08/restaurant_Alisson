using UnityEngine;

namespace AliGame.Items
{
    /// <summary>
    /// Put this on the player. Pulls nearby ItemPickups toward the player, as long as the
    /// inventory still has room for that item. Increase Radius (e.g. from an upgrade) for a stronger magnet.
    /// </summary>
    [RequireComponent(typeof(Inventory))]
    public class ItemMagnet : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float radius = 4f;
        [SerializeField, Min(0f)] private float pullSpeed = 12f;
        [SerializeField, Min(0f)] private float acceleration = 60f;
        [SerializeField] private LayerMask itemLayers = ~0;
        [SerializeField] private Vector2 centerOffset;

        private readonly Collider2D[] _hits = new Collider2D[32];
        private Inventory _inventory;
        private Collider2D _collider;
        private ContactFilter2D _filter;

        public float Radius
        {
            get => radius;
            set => radius = Mathf.Max(0f, value);
        }

        private Vector2 Center => (_collider != null ? (Vector2)_collider.bounds.center : (Vector2)transform.position) + centerOffset;

        private void Awake()
        {
            _inventory = GetComponent<Inventory>();
            _collider = GetComponent<Collider2D>();
            _filter = new ContactFilter2D { useTriggers = true };
            _filter.SetLayerMask(itemLayers);
        }

        private void FixedUpdate()
        {
            Vector2 center = Center;
            int count = Physics2D.OverlapCircle(center, radius, _filter, _hits);

            for (int i = 0; i < count; i++)
            {
                var pickup = _hits[i].GetComponentInParent<ItemPickup>();
                if (pickup == null || !pickup.CanBeCollected || !_inventory.CanAdd(pickup.Item)) continue;

                pickup.Pull(center, pullSpeed, acceleration);
            }
        }

        private void OnDrawGizmosSelected()
        {
            var collider = GetComponent<Collider2D>();
            Vector2 center = (collider != null ? (Vector2)collider.bounds.center : (Vector2)transform.position) + centerOffset;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireSphere(center, radius);
        }
    }
}
