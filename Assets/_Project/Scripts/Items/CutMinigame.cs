using AliGame.Core;
using AliGame.Data;
using AliGame.Movement;
using AliGame.UI;
using Unity.Cinemachine;
using UnityEngine;

namespace AliGame.Items
{
    /// <summary>
    /// The cutting minigame at the CutStation. Started by the crafting panel when a recipe uses it.
    /// Hold the interact key (Cut Start plays once) until the bar is full: the screen shakes and a release window opens.
    /// Release inside the window (Cut Release Window seconds on the recipe) and the cut counts, playing Cut Final.
    /// Hold past the window and the cut is missed; releasing too early just resets the bar. After the recipe's Cut Count
    /// good cuts, once the last Cut Final has played, the ingredients are consumed and the result drops from the station.
    /// Esc cancels and consumes nothing. Needs the player's PlayerAnimation2D for the Cut Start / Cut Final clips.
    /// </summary>
    public class CutMinigame : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;

        [Header("Screen shake: when")]
        [Tooltip("Shake when the bar fills and the release window opens (the signal to let go).")]
        [SerializeField] private bool shakeWhenWindowOpens = true;
        [Tooltip("Shake again when a cut counts.")]
        [SerializeField] private bool shakeOnSuccess;
        [Tooltip("Shake when the window is missed.")]
        [SerializeField] private bool shakeOnMiss;

        [Header("Screen shake: how")]
        [Tooltip("Rumble is a continuous tremble, Bump one push, Recoil a kick that settles, Explosion a big hit that fades.")]
        [SerializeField] private CinemachineImpulseDefinition.ImpulseShapes shakeShape = CinemachineImpulseDefinition.ImpulseShapes.Rumble;
        [Tooltip("Strength in world units. With the current zoom, 0.3 is subtle and 1 is heavy.")]
        [SerializeField, Min(0f)] private float shakeForce = 0.6f;
        [Tooltip("How long the shake lasts, in seconds.")]
        [SerializeField, Min(0.05f)] private float shakeDuration = 0.4f;
        [Tooltip("Speed of the tremble. Higher is a faster, buzzier shake.")]
        [SerializeField, Min(0.1f)] private float shakeFrequency = 1f;
        [Tooltip("Which axes shake. (1, 1) shakes both; (1, 0) only sideways; (0, 1) only up and down.")]
        [SerializeField] private Vector2 shakeAxes = Vector2.one;

        private CutMinigameModel _model;
        private RecipeSO _recipe;
        private CraftingStation _station;
        private PlayerMovement2D _player;
        private PlayerAnimation2D _animation;
        private CinemachineImpulseSource _impulse;
        private bool _locked;
        private float _idleAt = -1f;
        private float _dropAt = -1f;

        /// <summary>True from Begin until the last cut has finished playing or the minigame is cancelled.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>The player being controlled; the progress bar follows it.</summary>
        public Transform PlayerTransform => inventory != null ? inventory.transform : null;

        public int CutsDone => _model != null ? _model.CutsDone : 0;

        public int RequiredCuts => _model != null ? _model.RequiredCuts : 0;

        public CutPhase Phase => _model != null ? _model.Phase : CutPhase.Idle;

        /// <summary>Filling toward the cut (0 to 1); during the release window it drains from 1 to 0.</summary>
        public float Progress
        {
            get
            {
                if (_model == null) return 0f;
                switch (_model.Phase)
                {
                    case CutPhase.Window: return 1f - Mathf.Clamp01(_model.WindowElapsed / _model.ReleaseWindow);
                    case CutPhase.Missed: return 0f;
                    default: return Mathf.Clamp01(_model.Charge / _model.HoldSeconds);
                }
            }
        }

        /// <summary>Seconds of holding left until the bar is full.</summary>
        public float SecondsLeft => _model == null ? 0f : Mathf.Max(0f, _model.HoldSeconds - _model.Charge);

        /// <summary>True while the bar is full and releasing the key counts.</summary>
        public bool ReadyToRelease => _model != null && _model.Phase == CutPhase.Window;

        /// <summary>True after the window was missed, until the key is released.</summary>
        public bool Missed => _model != null && _model.Phase == CutPhase.Missed;

        private void Awake()
        {
            if (inventory == null) inventory = FindFirstObjectByType<Inventory>();
            if (inventory == null)
            {
                Debug.LogWarning("CutMinigame: no Inventory found in the scene.", this);
                enabled = false;
                return;
            }

            _player = inventory.GetComponent<PlayerMovement2D>();
            _animation = inventory.GetComponent<PlayerAnimation2D>();
            SetupShake();
        }

        private void OnEnable() => UIPanels.Opened += OnOtherPanelOpened;

        private void OnDisable()
        {
            UIPanels.Opened -= OnOtherPanelOpened;
            if (IsRunning) End();
        }

        /// <summary>Starts cutting for the recipe. Nothing is consumed until the last cut.</summary>
        public bool Begin(RecipeSO recipe, CraftingStation station)
        {
            if (IsRunning || !isActiveAndEnabled || recipe == null) return false;
            if (!inventory.CanCraft(recipe, recipe.Station)) return false;

            _recipe = recipe;
            _station = station;
            _idleAt = _dropAt = -1f;
            _model = new CutMinigameModel(recipe.CutHoldSeconds, recipe.CutCount, recipe.CutReleaseWindow);
            _model.ChargeStarted += OnChargeStarted;
            _model.ChargeAborted += OnChargeAborted;
            _model.WindowOpened += OnWindowOpened;
            _model.CutSucceeded += OnCutSucceeded;
            _model.CutMissed += OnCutMissed;
            _model.FinishedAll += OnFinishedAll;

            IsRunning = true;
            if (_player != null && !_locked)
            {
                _locked = true;
                _player.LockInput();
            }
            return true;
        }

        private void Update()
        {
            if (!IsRunning) return;

            if (_dropAt >= 0f)
            {
                if (Time.time >= _dropAt) DropResult();
                return;
            }

            if (_idleAt >= 0f && Time.time >= _idleAt)
            {
                _idleAt = -1f;
                if (_animation != null) _animation.StopCut();
            }

            if (GameInput.CancelPressed)
            {
                _model.Cancel();
                End();
                return;
            }

            _model.Update(GameInput.InteractHeld, Time.deltaTime);
        }

        private void OnChargeStarted()
        {
            _idleAt = -1f;
            if (_animation != null) _animation.PlayCutStart();
        }

        private void OnChargeAborted()
        {
            if (_animation != null) _animation.StopCut();
        }

        private void OnWindowOpened()
        {
            if (shakeWhenWindowOpens) Shake();
        }

        private void OnCutSucceeded(int cutsDone)
        {
            if (shakeOnSuccess) Shake();
            if (_animation == null) return;

            _animation.PlayCutFinal();
            _idleAt = Time.time + _animation.CutFinalLength;
        }

        private void OnCutMissed()
        {
            if (shakeOnMiss) Shake();
            if (_animation != null) _animation.StopCut();
        }

        private void OnFinishedAll()
        {
            float wait = _animation != null ? _animation.CutFinalLength : 0f;
            _idleAt = -1f;
            _dropAt = Time.time + wait;
        }

        private void DropResult()
        {
            _dropAt = -1f;
            if (inventory.Craft(_recipe, _recipe.Station) && _station != null)
                _station.SpawnCrafted(_recipe);
            End();
        }

        private void End()
        {
            IsRunning = false;
            _idleAt = _dropAt = -1f;

            if (_model != null)
            {
                _model.ChargeStarted -= OnChargeStarted;
                _model.ChargeAborted -= OnChargeAborted;
                _model.WindowOpened -= OnWindowOpened;
                _model.CutSucceeded -= OnCutSucceeded;
                _model.CutMissed -= OnCutMissed;
                _model.FinishedAll -= OnFinishedAll;
            }

            if (_animation != null) _animation.StopCut();
            if (_locked && _player != null)
            {
                _locked = false;
                _player.UnlockInput();
            }
        }

        private void OnOtherPanelOpened(object panel)
        {
            if (IsRunning) End();
        }

        private void SetupShake()
        {
            _impulse = gameObject.AddComponent<CinemachineImpulseSource>();

            foreach (CinemachineCamera cam in FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var listener = cam.GetComponent<CinemachineImpulseListener>();
                if (listener == null) listener = cam.gameObject.AddComponent<CinemachineImpulseListener>();

                // A listener added from code starts with Gain 0 and no channel, which silently ignores every impulse.
                listener.Gain = 1f;
                listener.ChannelMask = 1;
                listener.Use2DDistance = true;
                listener.ReactionSettings.AmplitudeGain = 1f;
                listener.ReactionSettings.FrequencyGain = 1f;
            }
        }

        private void Shake()
        {
            if (_impulse == null || shakeForce <= 0f) return;

            // Applied on every shake so the Inspector values can be tuned while playing.
            CinemachineImpulseDefinition definition = _impulse.ImpulseDefinition;
            definition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            definition.ImpulseShape = shakeShape;
            definition.ImpulseDuration = shakeDuration;
            definition.FrequencyGain = shakeFrequency;
            definition.AmplitudeGain = 1f;
            definition.TimeEnvelope.AttackTime = 0.02f;
            definition.TimeEnvelope.SustainTime = 0f;
            definition.TimeEnvelope.DecayTime = shakeDuration;

            _impulse.GenerateImpulseWithVelocity(new Vector3(shakeForce * shakeAxes.x, shakeForce * shakeAxes.y, 0f));
        }
    }
}
