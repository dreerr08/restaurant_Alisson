using Unity.Cinemachine;
using UnityEngine;

namespace AliGame.Dialogue
{
    /// <summary>
    /// While a conversation runs, swaps to a second Cinemachine camera that keeps the speaker and the listener
    /// centered on screen (it follows the midpoint between them, so it keeps working when the NPC steps back).
    /// When the conversation ends the gameplay camera takes over again; the Brain blends between the two.
    /// Needs a CinemachineCamera for the dialogue (kept inactive in the scene) and the gameplay one.
    /// </summary>
    public class DialogueCameraFocus : MonoBehaviour
    {
        [Header("Cameras")]
        [Tooltip("The camera used during a conversation. Keep its GameObject inactive; this component turns it on and off.")]
        [SerializeField] private CinemachineCamera dialogueCamera;
        [Tooltip("The normal follow camera. Its zoom is copied so the dialogue camera starts at the same distance.")]
        [SerializeField] private CinemachineCamera gameplayCamera;
        [SerializeField] private DialogueRunner runner;

        [Header("Framing")]
        [Tooltip("1 keeps the gameplay zoom. Below 1 zooms in on the pair, above 1 zooms out.")]
        [SerializeField, Min(0.1f)] private float zoom = 1f;
        [Tooltip("Where the pair sits on screen. (0, 0) is the exact center; positive Y moves them up.")]
        [SerializeField] private Vector2 screenOffset;

        [Header("Motion")]
        [Tooltip("Seconds the Brain takes to blend between the gameplay and the dialogue camera (applies to every camera blend).")]
        [SerializeField, Min(0f)] private float blendTime = 0.7f;
        [Tooltip("How softly the dialogue camera follows the midpoint while the NPC steps back.")]
        [SerializeField, Min(0f)] private float followDamping = 0.35f;

        private const int PriorityAboveGameplay = 10;

        private Transform _target;
        private DialogueContext _context;
        private Collider2D _speakerCollider;
        private Collider2D _listenerCollider;

        private void Awake()
        {
            if (runner == null) runner = FindFirstObjectByType<DialogueRunner>();
            if (dialogueCamera == null || runner == null)
            {
                Debug.LogWarning("DialogueCameraFocus: needs a dialogue CinemachineCamera and a DialogueRunner.", this);
                enabled = false;
                return;
            }

            var targetObject = new GameObject("DialogueCameraTarget");
            targetObject.transform.SetParent(transform, false);
            _target = targetObject.transform;

            dialogueCamera.Follow = _target;
            dialogueCamera.LookAt = null;

            PrioritySettings priority = dialogueCamera.Priority;
            priority.Enabled = true;
            priority.Value = (gameplayCamera != null ? gameplayCamera.Priority.Value : 10) + PriorityAboveGameplay;
            dialogueCamera.Priority = priority;

            dialogueCamera.gameObject.SetActive(false);
            ApplyBlendTime();
        }

        private void OnEnable()
        {
            if (runner == null) return;
            runner.Started += OnStarted;
            runner.Ended += OnEnded;
        }

        private void OnDisable()
        {
            if (runner != null)
            {
                runner.Started -= OnStarted;
                runner.Ended -= OnEnded;
            }
            if (_context != null) StopFocus();
        }

        private void LateUpdate()
        {
            if (_context != null) UpdateTarget();
        }

        private void OnStarted(DialogueContext context)
        {
            if (context.Speaker == null || context.Listener == null) return;

            _context = context;
            _speakerCollider = context.Speaker.GetComponentInChildren<Collider2D>();
            _listenerCollider = context.Listener.GetComponentInChildren<Collider2D>();

            UpdateTarget();
            ConfigureCamera();
            dialogueCamera.gameObject.SetActive(true);
        }

        private void OnEnded(DialogueContext context)
        {
            if (_context != null) StopFocus();
        }

        private void StopFocus()
        {
            _context = null;
            if (dialogueCamera != null) dialogueCamera.gameObject.SetActive(false);
        }

        private void UpdateTarget()
        {
            Vector2 speaker = CenterOf(_context.Speaker, _speakerCollider);
            Vector2 listener = CenterOf(_context.Listener, _listenerCollider);
            _target.position = new Vector3((speaker.x + listener.x) * 0.5f, (speaker.y + listener.y) * 0.5f, 0f);
        }

        private static Vector2 CenterOf(Transform character, Collider2D collider)
        {
            if (collider != null)
            {
                Vector3 center = collider.bounds.center;
                return new Vector2(center.x, center.y);
            }
            return new Vector2(character.position.x, character.position.y);
        }

        private void ConfigureCamera()
        {
            if (gameplayCamera != null)
            {
                LensSettings lens = dialogueCamera.Lens;
                lens.OrthographicSize = gameplayCamera.Lens.OrthographicSize * zoom;
                dialogueCamera.Lens = lens;
            }

            var composer = dialogueCamera.GetComponent<CinemachinePositionComposer>();
            if (composer == null) return;

            composer.Damping = new Vector3(followDamping, followDamping, 0f);
            var composition = composer.Composition;
            composition.ScreenPosition = screenOffset;
            composition.DeadZone.Enabled = false;
            composer.Composition = composition;
            composer.Lookahead.Enabled = false;
        }

        private void ApplyBlendTime()
        {
            CinemachineBrain brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
            if (brain == null) brain = FindFirstObjectByType<CinemachineBrain>();
            if (brain == null) return;

            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blendTime);
        }
    }
}
