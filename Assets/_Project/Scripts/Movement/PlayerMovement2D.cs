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
        private bool _externalControl;
        private float _externalMoveInput;

        public bool IsGrounded { get; private set; }

        private int _inputLocks;

        /// <summary>False while any lock is held: walking and jumping input is ignored (the player still falls and lands).</summary>
        public bool InputEnabled => _inputLocks == 0;

        public void LockInput() => _inputLocks++;

        public void UnlockInput() => _inputLocks = Mathf.Max(0, _inputLocks - 1);

        /// <summary>
        /// While active, horizontal movement comes from SetExternalMove instead of GameInput, so code can walk the
        /// player to a spot (e.g. before a minigame). Jumping stays off, independent of LockInput. Call EndExternalControl
        /// when done.
        /// </summary>
        public void BeginExternalControl()
        {
            _externalControl = true;
            _externalMoveInput = 0f;
        }

        public void EndExternalControl()
        {
            _externalControl = false;
            _externalMoveInput = 0f;
        }

        public bool IsExternallyControlled => _externalControl;

        /// <summary>-1 to 1. Only has an effect between BeginExternalControl and EndExternalControl.</summary>
        public void SetExternalMove(float direction) => _externalMoveInput = Mathf.Clamp(direction, -1f, 1f);

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
            _moveInput = _externalControl ? _externalMoveInput : (InputEnabled ? GameInput.Move : 0f);

            if (_externalControl || !InputEnabled) _jumpRequested = false;
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
