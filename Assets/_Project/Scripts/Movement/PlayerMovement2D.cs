using AliGame.Core;
using UnityEngine;

namespace AliGame.Movement
{
    /// <summary>
    /// Walk and jump from GameInput, flipping toward the movement direction. Grounded is detected from the
    /// collider's own contacts: touching a solid collider whose contact normal points up.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerMovement2D : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 7f;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField, Range(0f, 1f)] private float minGroundNormalY = 0.7f;

        private readonly ContactPoint2D[] _contacts = new ContactPoint2D[8];

        private Rigidbody2D _rb;
        private Collider2D _collider;
        private ContactFilter2D _groundFilter;
        private Vector3 _baseScale;
        private float _moveInput;
        private bool _jumpRequested;

        public bool IsGrounded { get; private set; }

        private int _inputLocks;

        /// <summary>False while any lock is held: walking and jumping input is ignored (the player still falls and lands).</summary>
        public bool InputEnabled => _inputLocks == 0;

        public void LockInput() => _inputLocks++;

        public void UnlockInput() => _inputLocks = Mathf.Max(0, _inputLocks - 1);

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _baseScale = transform.localScale;

            _groundFilter = new ContactFilter2D();
            _groundFilter.SetLayerMask(groundLayers);
            _groundFilter.useTriggers = false;
        }

        private void Update()
        {
            _moveInput = InputEnabled ? GameInput.Move : 0f;

            if (!InputEnabled) _jumpRequested = false;
            else if (GameInput.JumpPressed) _jumpRequested = true;

            if (_moveInput > 0.01f) Face(1f);
            else if (_moveInput < -0.01f) Face(-1f);
        }

        private void FixedUpdate()
        {
            IsGrounded = CheckGrounded();

            Vector2 velocity = _rb.linearVelocity;
            velocity.x = _moveInput * moveSpeed;

            if (_jumpRequested && IsGrounded)
                velocity.y = jumpForce;

            _jumpRequested = false;
            _rb.linearVelocity = velocity;
        }

        private bool CheckGrounded()
        {
            int count = _collider.GetContacts(_groundFilter, _contacts);
            for (int i = 0; i < count; i++)
            {
                if (_contacts[i].normal.y >= minGroundNormalY)
                    return true;
            }
            return false;
        }

        private void Face(float direction)
        {
            transform.localScale = new Vector3(_baseScale.x * direction, _baseScale.y, _baseScale.z);
        }

    }
}
