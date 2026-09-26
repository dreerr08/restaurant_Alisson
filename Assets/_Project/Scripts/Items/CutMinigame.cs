using System;
using AliGame.Core;
using AliGame.Data;
using AliGame.Movement;
using AliGame.UI;
using UnityEngine;

namespace AliGame.Items
{
    /// <summary>
    /// The cutting minigame at the CutStation. Started by the crafting panel when a recipe uses it, for one or more
    /// items in a row (Quantity): each item is its own full minigame, one after another, with nothing consumed until
    /// its own last cut succeeds. If the station has a Minigame Spot, the player first walks there on their own
    /// (IsApproaching) before the first cut can start.
    /// Hold the interact key (Cut Start plays once) until the bar is full: a release window opens (WindowOpened).
    /// Release inside the window (Cut Release Window seconds on the recipe) and the cut counts (CutSucceeded), playing
    /// Cut Final. Hold past the window and the cut is missed (CutMissed); releasing too early just resets the bar.
    /// After the recipe's Cut Count good cuts (AllFinished), once the last Cut Final has played, that item's
    /// ingredients are consumed and it drops from the station; if more items are queued, the next one starts right away.
    /// Esc cancels the whole queue; only cuts already dropped are kept.
    /// Needs the player's PlayerAnimation2D for the Cut Start / Cut Final clips. Has no juice of its own: put a
    /// CutFeedbackFx next to it for the camera shake, screen flash and particles.
    /// </summary>
    public class CutMinigame : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;
        [Tooltip("Horizontal distance to the station's Minigame Spot that counts as arrived.")]
        [SerializeField, Min(0.02f)] private float approachTolerance = 0.15f;
        [SerializeField, Min(0.5f)] private float approachSpeed = 1f;

        private CutMinigameModel _model;
        private RecipeSO _recipe;
        private CraftingStation _station;
        private PlayerMovement2D _player;
        private PlayerAnimation2D _animation;
        private Transform _approachTarget;
        private bool _approaching;
        private bool _locked;
        private float _idleAt = -1f;
        private float _dropAt = -1f;

        /// <summary>True from Begin until the whole queue has finished dropping or been cancelled.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>True while the player is walking to the station's Minigame Spot, before the first cut.</summary>
        public bool IsApproaching => _approaching;

        /// <summary>The player being controlled; feedback effects follow it.</summary>
        public Transform PlayerTransform => inventory != null ? inventory.transform : null;

        /// <summary>How many items were queued when Begin was called.</summary>
        public int BatchTotal { get; private set; }

        /// <summary>How many items, including the one being cut now, are left to drop.</summary>
        public int BatchRemaining { get; private set; }

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

        /// <summary>The bar just filled and the release window opened: the signal to let go.</summary>
        public event Action WindowOpened;

        /// <summary>A cut counted. The argument is how many cuts are done now on the current item.</summary>
        public event Action<int> CutSucceeded;

        /// <summary>The key was held past the window: the cut did not count.</summary>
        public event Action CutMissed;

        /// <summary>The current item's last cut just succeeded (fires once per item in the batch).</summary>
        public event Action AllFinished;

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
        }

        private void OnEnable() => UIPanels.Opened += OnOtherPanelOpened;

        private void OnDisable()
        {
            UIPanels.Opened -= OnOtherPanelOpened;
            if (IsRunning) End();
        }

        /// <summary>
        /// Queues Quantity items of the recipe. Nothing is consumed until each item's own last cut succeeds.
        /// If the station has a Minigame Spot the player is walked there first.
        /// </summary>
        public bool Begin(RecipeSO recipe, CraftingStation station, int quantity = 1)
        {
            if (IsRunning || !isActiveAndEnabled || recipe == null) return false;

            quantity = Mathf.Max(1, quantity);
            if (!inventory.CanCraft(recipe, recipe.Station, quantity)) return false;

            _recipe = recipe;
            _station = station;
            BatchTotal = quantity;
            BatchRemaining = quantity;
            _idleAt = _dropAt = -1f;

            IsRunning = true;
            if (_player != null && !_locked)
            {
                _locked = true;
                _player.LockInput();
            }

            Transform spot = station != null ? station.MinigameSpot : null;
            if (spot != null && _player != null && Mathf.Abs(spot.position.x - _player.transform.position.x) > approachTolerance)
            {
                _approaching = true;
                _approachTarget = spot;
                _player.BeginExternalControl();
            }
            else
            {
                StartCut();
            }
            return true;
        }

        private void Update()
        {
            if (!IsRunning) return;

            if (_approaching)
            {
                UpdateApproach();
                return;
            }

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

        private void UpdateApproach()
        {
            if (GameInput.CancelPressed)
            {
                End();
                return;
            }

            if (_approachTarget == null)
            {
                ArriveAndStartCut();
                return;
            }

            float dx = _approachTarget.position.x - _player.transform.position.x;
            if (Mathf.Abs(dx) <= approachTolerance)
            {
                ArriveAndStartCut();
                return;
            }

            _player.SetExternalMove(Mathf.Sign(dx) * approachSpeed);
        }

        private void ArriveAndStartCut()
        {
            _approaching = false;
            _approachTarget = null;
            if (_player != null) _player.EndExternalControl();
            StartCut();
        }

        private void StartCut()
        {
            _model = new CutMinigameModel(_recipe.CutHoldSeconds, _recipe.CutCount, _recipe.CutReleaseWindow);
            _model.ChargeStarted += OnChargeStarted;
            _model.ChargeAborted += OnChargeAborted;
            _model.WindowOpened += HandleWindowOpened;
            _model.CutSucceeded += HandleCutSucceeded;
            _model.CutMissed += HandleCutMissed;
            _model.FinishedAll += HandleFinishedAll;
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

        private void HandleWindowOpened()
        {
            WindowOpened?.Invoke();
        }

        private void HandleCutSucceeded(int cutsDone)
        {
            CutSucceeded?.Invoke(cutsDone);
            if (_animation == null) return;

            _animation.PlayCutFinal();
            _idleAt = Time.time + _animation.CutFinalLength;
        }

        private void HandleCutMissed()
        {
            CutMissed?.Invoke();
            if (_animation != null) _animation.StopCut();
        }

        private void HandleFinishedAll()
        {
            AllFinished?.Invoke();
            float wait = _animation != null ? _animation.CutFinalLength : 0f;
            _idleAt = -1f;
            _dropAt = Time.time + wait;
        }

        private void DropResult()
        {
            _dropAt = -1f;
            UnsubscribeModel();

            bool crafted = inventory.Craft(_recipe, _recipe.Station);
            if (crafted && _station != null) _station.SpawnCrafted(_recipe);

            if (!crafted)
            {
                End();
                return;
            }

            BatchRemaining--;
            if (BatchRemaining > 0) StartCut();
            else End();
        }

        private void End()
        {
            IsRunning = false;
            _approaching = false;
            _approachTarget = null;
            _idleAt = _dropAt = -1f;
            BatchRemaining = 0;

            UnsubscribeModel();

            if (_animation != null) _animation.StopCut();
            if (_player != null)
            {
                _player.EndExternalControl();
                if (_locked)
                {
                    _locked = false;
                    _player.UnlockInput();
                }
            }
        }

        private void UnsubscribeModel()
        {
            if (_model == null) return;

            _model.ChargeStarted -= OnChargeStarted;
            _model.ChargeAborted -= OnChargeAborted;
            _model.WindowOpened -= HandleWindowOpened;
            _model.CutSucceeded -= HandleCutSucceeded;
            _model.CutMissed -= HandleCutMissed;
            _model.FinishedAll -= HandleFinishedAll;
            _model = null;
        }

        private void OnOtherPanelOpened(object panel)
        {
            if (IsRunning) End();
        }
    }
}
