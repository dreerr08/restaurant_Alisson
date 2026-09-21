using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AliGame.Core
{
    /// <summary>
    /// The one place gameplay code reads input from. Bindings live in Resources/GameInput.inputactions
    /// (edit them there or rebind at runtime through GameInput.Asset), so keyboard, mouse and gamepad support
    /// and future rebinding never touch the scripts that use the input.
    /// </summary>
    public static class GameInput
    {
        private const string ResourceName = "GameInput";
        private const int MaxChoices = 9;

        private static InputActionAsset _asset;
        private static InputAction _move;
        private static InputAction _jump;
        private static InputAction _interact;
        private static InputAction _inventory;
        private static InputAction _journal;
        private static InputAction _confirm;
        private static InputAction _cancel;
        private static InputAction _navigateUp;
        private static InputAction _navigateDown;
        private static InputAction _click;
        private static InputAction[] _choices;

        /// <summary>A runtime copy of the input asset. Rebind on this copy; the project asset is never modified.</summary>
        public static InputActionAsset Asset
        {
            get
            {
                EnsureLoaded();
                return _asset;
            }
        }

        /// <summary>Horizontal movement, -1 (left) to 1 (right). Held.</summary>
        public static float Move
        {
            get
            {
                EnsureLoaded();
                return Mathf.Clamp(_move.ReadValue<float>(), -1f, 1f);
            }
        }

        public static bool JumpPressed => Pressed(ref _jump);

        public static bool InteractPressed => Pressed(ref _interact);

        /// <summary>True while the interact key is held down.</summary>
        public static bool InteractHeld
        {
            get
            {
                EnsureLoaded();
                return _interact.IsPressed();
            }
        }

        public static bool InventoryPressed => Pressed(ref _inventory);

        /// <summary>Opens or closes the wish journal.</summary>
        public static bool JournalPressed => Pressed(ref _journal);

        /// <summary>Continue / select in dialogue and menus.</summary>
        public static bool ConfirmPressed => Pressed(ref _confirm);

        /// <summary>Close the current panel or leave the conversation.</summary>
        public static bool CancelPressed => Pressed(ref _cancel);

        public static bool NavigateUpPressed => Pressed(ref _navigateUp);

        public static bool NavigateDownPressed => Pressed(ref _navigateDown);

        /// <summary>Primary pointer click (left mouse button).</summary>
        public static bool ClickPressed => Pressed(ref _click);

        /// <summary>True if one of the number shortcuts for the first <paramref name="count"/> choices was pressed this frame.</summary>
        public static bool TryGetChoicePressed(int count, out int index)
        {
            EnsureLoaded();
            int limit = Mathf.Min(count, MaxChoices);
            for (int i = 0; i < limit; i++)
            {
                if (_choices[i].WasPressedThisFrame())
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        /// <summary>Drops the loaded actions; the next access reloads them from the asset (used by tests).</summary>
        public static void Reset()
        {
            if (_asset != null)
            {
                _asset.Disable();
                UnityEngine.Object.DestroyImmediate(_asset);
            }

            _asset = null;
            _move = _jump = _interact = _inventory = _journal = null;
            _confirm = _cancel = _navigateUp = _navigateDown = _click = null;
            _choices = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Reset();

        private static bool Pressed(ref InputAction action)
        {
            EnsureLoaded();
            return action.WasPressedThisFrame();
        }

        private static void EnsureLoaded()
        {
            if (_asset != null) return;

            var source = Resources.Load<InputActionAsset>(ResourceName);
            if (source == null)
                throw new InvalidOperationException("GameInput: Resources/" + ResourceName + ".inputactions is missing.");

            _asset = UnityEngine.Object.Instantiate(source);
            _asset.Enable();

            _move = _asset.FindAction("Gameplay/Move", true);
            _jump = _asset.FindAction("Gameplay/Jump", true);
            _interact = _asset.FindAction("Gameplay/Interact", true);
            _inventory = _asset.FindAction("Gameplay/Inventory", true);
            _journal = _asset.FindAction("Gameplay/Journal", true);
            _confirm = _asset.FindAction("UI/Confirm", true);
            _cancel = _asset.FindAction("UI/Cancel", true);
            _navigateUp = _asset.FindAction("UI/NavigateUp", true);
            _navigateDown = _asset.FindAction("UI/NavigateDown", true);
            _click = _asset.FindAction("UI/Click", true);

            _choices = new InputAction[MaxChoices];
            for (int i = 0; i < MaxChoices; i++)
                _choices[i] = _asset.FindAction("UI/Choice" + (i + 1), true);
        }
    }
}
