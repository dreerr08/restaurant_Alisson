using AliGame.Data;
using UnityEngine;

namespace AliGame.Items
{
    /// <summary>
    /// An item lying in the world. Give it a solid Collider2D so it rests on the ground
    /// (physics), and set Collector Layers to the player's layer: anything on those layers
    /// with an Inventory within Pickup Radius collects it. The collection check is a
    /// physics query, so it works even when the item and the player don't collide.
    /// An ItemMagnet can pull it toward the player.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class ItemPickup : MonoBehaviour
    {
        [SerializeField] private ItemSO item;
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField] private LayerMask collectorLayers = ~0;
        [SerializeField, Min(0f)] private float pickupRadius = 0.6f;
        [SerializeField] private Vector2 pickupOffset;

        private readonly Collider2D[] _hits = new Collider2D[8];
        private ContactFilter2D _filter;
        private Rigidbody2D _rb;
        private bool _isPulled;
        private float _defaultGravity;
        private float _pullExpireTime;
        private float _lastPullTime = -1f;
        private float _collectableAt;

        public ItemSO Item => item;

        /// <summary>False for a short moment after spawning, so the item is visible before it is collected.</summary>
        public bool CanBeCollected => Time.time >= _collectableAt;

        /// <summary>
        /// Spawns an item in the world: the item's Pickup Prefab if it has one, otherwise a basic pickup
        /// built from its icon. The pickup then behaves like any other (physics, magnet, collection).
        /// </summary>
        public static ItemPickup Spawn(ItemSO item, int amount, Vector2 position, Vector2 velocity, float collectDelay)
        {
            GameObject instance = item.PickupPrefab != null
                ? Instantiate(item.PickupPrefab, position, Quaternion.identity)
                : CreateBasicPickup(item, position);

            var pickup = instance.GetComponent<ItemPickup>();
            if (pickup == null) pickup = instance.AddComponent<ItemPickup>();

            pickup.item = item;
            pickup.amount = Mathf.Max(1, amount);
            pickup._collectableAt = Time.time + collectDelay;
            pickup.GetComponent<Rigidbody2D>().linearVelocity = velocity;
            return pickup;
        }

        private static GameObject CreateBasicPickup(ItemSO item, Vector2 position)
        {
            var go = new GameObject("Pickup " + item.DisplayName);
            go.transform.position = position;

            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = item.Icon;
            spriteRenderer.color = item.Tint;
            spriteRenderer.sortingOrder = 1;

            go.AddComponent<CircleCollider2D>().radius = 0.3f;
            return go;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _filter = new ContactFilter2D { useTriggers = true };
            _filter.SetLayerMask(collectorLayers);
        }

        private void OnDisable()
        {
            StopPull();
        }

        /// <summary>Called every physics step by an ItemMagnet while this item is in range.</summary>
        public void Pull(Vector2 target, float maxSpeed, float acceleration)
        {
            if (Mathf.Approximately(_lastPullTime, Time.fixedTime)) return;
            _lastPullTime = Time.fixedTime;

            if (!_isPulled)
            {
                _isPulled = true;
                _defaultGravity = _rb.gravityScale;
                _rb.gravityScale = 0f;
            }
            _pullExpireTime = Time.fixedTime + Time.fixedDeltaTime * 1.5f;

            Vector2 direction = (target - _rb.position).normalized;
            _rb.linearVelocity = Vector2.MoveTowards(_rb.linearVelocity, direction * maxSpeed, acceleration * Time.fixedDeltaTime);
        }

        private void StopPull()
        {
            if (!_isPulled) return;
            _isPulled = false;
            if (_rb != null) _rb.gravityScale = _defaultGravity;
        }

        private void FixedUpdate()
        {
            if (_isPulled && Time.fixedTime > _pullExpireTime) StopPull();
            if (item == null || !CanBeCollected) return;

            Vector2 center = (Vector2)transform.position + pickupOffset;
            int count = Physics2D.OverlapCircle(center, pickupRadius, _filter, _hits);

            for (int i = 0; i < count; i++)
            {
                var inventory = _hits[i].GetComponentInParent<Inventory>();
                if (inventory == null) continue;

                amount -= inventory.Add(item, amount);
                if (amount <= 0)
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.94f, 0.66f, 0.23f, 0.9f);
            Gizmos.DrawWireSphere((Vector2)transform.position + pickupOffset, pickupRadius);
        }
    }
}
