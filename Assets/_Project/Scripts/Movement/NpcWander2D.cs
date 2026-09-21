using UnityEngine;

namespace AliGame.Movement
{
    /// <summary>
    /// Makes an NPC wander: walks in a random direction for a while, stands still for a while, and repeats.
    /// It stays within Wander Radius of where it started, and stops instead of walking into a wall or off a ledge.
    /// Needs a Rigidbody2D and a solid Collider2D; set Ground Layers to the layers it should treat as floor and walls.
    /// Other scripts (conversations, cutscenes) can take over with BeginControl / EndControl: while controlled the
    /// wander cycle is paused and the NPC only does what Face and WalkControlled tell it.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class NpcWander2D : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float walkSpeed = 1.5f;
        [SerializeField] private Vector2 walkDuration = new Vector2(2f, 4f);
        [SerializeField] private Vector2 idleDuration = new Vector2(2f, 5f);
        [SerializeField, Min(0f)] private float wanderRadius = 6f;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField, Min(0f)] private float ledgeCheckDepth = 0.6f;
        [SerializeField, Min(0f)] private float wallCheckDistance = 0.15f;
        [SerializeField] private bool spriteFacesRight = true;

        private readonly RaycastHit2D[] _hits = new RaycastHit2D[8];

        private Rigidbody2D _rb;
        private Collider2D _collider;
        private ContactFilter2D _filter;
        private Vector3 _baseScale;
        private float _homeX;
        private float _timer;
        private int _direction = 1;
        private int _controlLocks;
        private bool _walking;

        public bool IsWalking => _walking;

        /// <summary>True while something else (a conversation...) has taken over from the wander cycle.</summary>
        public bool IsControlled => _controlLocks > 0;

        public float WalkSpeed => walkSpeed;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _baseScale = transform.localScale;
            _homeX = transform.position.x;

            _filter = new ContactFilter2D { useTriggers = false };
            _filter.SetLayerMask(groundLayers);

            _timer = RandomIn(idleDuration);
        }

        /// <summary>Pauses the wander cycle and stops the NPC. Every BeginControl needs a matching EndControl.</summary>
        public void BeginControl()
        {
            _controlLocks++;
            _walking = false;
        }

        /// <summary>Gives control back; the NPC stands still for a moment and then resumes wandering.</summary>
        public void EndControl()
        {
            _controlLocks = Mathf.Max(0, _controlLocks - 1);
            if (_controlLocks > 0) return;

            _walking = false;
            _timer = RandomIn(idleDuration);
        }

        /// <summary>Turns the sprite to face right (+1) or left (-1) without moving.</summary>
        public void Face(int direction)
        {
            if (direction == 0) return;

            transform.localScale = new Vector3(
                Mathf.Abs(_baseScale.x) * Mathf.Sign(direction) * (spriteFacesRight ? 1f : -1f),
                _baseScale.y,
                _baseScale.z);
        }

        /// <summary>
        /// While controlled: walks right (+1) or left (-1) at the wander speed, or stops with 0. It does not
        /// change which way the NPC faces, and it still stops at walls and ledges.
        /// </summary>
        public void WalkControlled(int direction)
        {
            if (!IsControlled) return;

            if (direction == 0 || IsBlocked(direction))
            {
                _walking = false;
                return;
            }

            _direction = direction;
            _walking = true;
        }

        private void Update()
        {
            if (IsControlled) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            if (_walking) StartIdle();
            else StartWalking();
        }

        private void FixedUpdate()
        {
            if (_walking && (IsBlocked(_direction) || (!IsControlled && IsHeadingOutOfRange()))) StartIdle();

            Vector2 velocity = _rb.linearVelocity;
            velocity.x = _walking ? _direction * walkSpeed : 0f;
            _rb.linearVelocity = velocity;
        }

        private void StartIdle()
        {
            _walking = false;
            _timer = RandomIn(idleDuration);
        }

        private void StartWalking()
        {
            float offset = transform.position.x - _homeX;
            int direction = Random.value < 0.5f ? -1 : 1;
            if (Mathf.Abs(offset) > wanderRadius) direction = offset > 0f ? -1 : 1;

            if (IsBlocked(direction)) direction = -direction;
            if (IsBlocked(direction))
            {
                StartIdle();
                return;
            }

            _direction = direction;
            _walking = true;
            _timer = RandomIn(walkDuration);
            Face(direction);
        }

        private bool IsHeadingOutOfRange()
        {
            float offset = transform.position.x - _homeX;
            return Mathf.Abs(offset) > wanderRadius && Mathf.Sign(offset) == _direction;
        }

        private bool IsBlocked(int direction)
        {
            Bounds bounds = _collider.bounds;

            var wallOrigin = new Vector2(bounds.center.x, bounds.center.y);
            if (CastHitsOther(wallOrigin, new Vector2(direction, 0f), bounds.extents.x + wallCheckDistance)) return true;

            var ledgeOrigin = new Vector2(bounds.center.x + direction * (bounds.extents.x + 0.1f), bounds.min.y + 0.1f);
            return !CastHitsOther(ledgeOrigin, Vector2.down, ledgeCheckDepth + 0.1f);
        }

        private bool CastHitsOther(Vector2 origin, Vector2 direction, float distance)
        {
            int count = Physics2D.Raycast(origin, direction, _filter, _hits, distance);
            for (int i = 0; i < count; i++)
            {
                if (_hits[i].collider != _collider) return true;
            }
            return false;
        }

        private static float RandomIn(Vector2 range)
        {
            return Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));
        }

        private void OnDrawGizmosSelected()
        {
            float home = Application.isPlaying ? _homeX : transform.position.x;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
            Gizmos.DrawLine(new Vector3(home - wanderRadius, transform.position.y, 0f), new Vector3(home + wanderRadius, transform.position.y, 0f));
            Gizmos.DrawWireSphere(new Vector3(home - wanderRadius, transform.position.y, 0f), 0.15f);
            Gizmos.DrawWireSphere(new Vector3(home + wanderRadius, transform.position.y, 0f), 0.15f);
        }
    }
}
