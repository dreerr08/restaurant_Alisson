using AliGame.Movement;
using UnityEngine;

namespace AliGame.Dialogue
{
    /// <summary>
    /// Makes an NPC behave during a conversation started by its DialogueTrigger: the wander cycle is paused,
    /// it always turns toward whoever it is talking to and it settles at Conversation Distance from them
    /// (stepping back if they are too close, or toward them if too far). When the conversation ends it goes back to wandering.
    /// </summary>
    [RequireComponent(typeof(NpcWander2D), typeof(DialogueTrigger))]
    public class NpcConversationBehaviour : MonoBehaviour
    {
        [Tooltip("Distance the NPC tries to keep from the player while talking.")]
        [SerializeField, Min(0f)] private float conversationDistance = 3.5f;
        [Tooltip("How far from the ideal distance the player can be before the NPC steps to correct it.")]
        [SerializeField, Min(0f)] private float distanceTolerance = 0.6f;

        private NpcWander2D _wander;
        private DialogueTrigger _trigger;
        private Transform _target;
        private bool _adjusting;

        private void Awake()
        {
            _wander = GetComponent<NpcWander2D>();
            _trigger = GetComponent<DialogueTrigger>();
        }

        private void OnEnable()
        {
            _trigger.DialogueStarted += OnStarted;
            _trigger.DialogueEnded += OnEnded;
        }

        private void OnDisable()
        {
            _trigger.DialogueStarted -= OnStarted;
            _trigger.DialogueEnded -= OnEnded;
            if (_target != null) OnEnded(null);
        }

        private void OnStarted(DialogueContext context)
        {
            _target = context.Listener;
            _adjusting = false;
            _wander.BeginControl();
        }

        private void OnEnded(DialogueContext context)
        {
            if (_target == null) return;

            _target = null;
            _wander.EndControl();
        }

        private void Update()
        {
            if (_target == null) return;

            float dx = _target.position.x - transform.position.x;
            int toward = dx >= 0f ? 1 : -1;
            _wander.Face(toward);

            float error = Mathf.Abs(dx) - conversationDistance;
            if (!_adjusting && Mathf.Abs(error) > distanceTolerance) _adjusting = true;
            else if (_adjusting && Mathf.Abs(error) <= distanceTolerance * 0.4f) _adjusting = false;

            if (!_adjusting)
            {
                _wander.WalkControlled(0);
                return;
            }

            _wander.WalkControlled(error > 0f ? toward : -toward);
        }
    }
}
