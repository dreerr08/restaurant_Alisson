using AliGame.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AliGame.Tests
{
    /// <summary>Checks the bindings in Resources/GameInput.inputactions with simulated devices.</summary>
    public class GameInputTests : InputTestFixture
    {
        private Keyboard _keyboard;

        public override void Setup()
        {
            base.Setup();
            GameInput.Reset();
            _keyboard = InputSystem.AddDevice<Keyboard>();

            // The actions are enabled on first access; the game does that in its first frame, before any press.
            Assert.IsNotNull(GameInput.Asset);
        }

        public override void TearDown()
        {
            GameInput.Reset();
            base.TearDown();
        }

        [Test]
        public void Move_IsZeroWhenNothingIsPressed()
        {
            Assert.AreEqual(0f, GameInput.Move, 0.001f);
        }

        [Test]
        public void Move_ReadsAAndDKeys()
        {
            Press(_keyboard.dKey);
            Assert.AreEqual(1f, GameInput.Move, 0.001f);
            Release(_keyboard.dKey);

            Press(_keyboard.aKey);
            Assert.AreEqual(-1f, GameInput.Move, 0.001f);
        }

        [Test]
        public void Move_ReadsTheArrowKeys()
        {
            Press(_keyboard.rightArrowKey);
            Assert.AreEqual(1f, GameInput.Move, 0.001f);
            Release(_keyboard.rightArrowKey);

            Press(_keyboard.leftArrowKey);
            Assert.AreEqual(-1f, GameInput.Move, 0.001f);
        }

        [Test]
        public void Move_ReadsTheGamepadStickWithADeadzone()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();

            Set(gamepad.leftStick, new Vector2(0.1f, 0f));
            Assert.AreEqual(0f, GameInput.Move, 0.001f, "small drift is ignored");

            Set(gamepad.leftStick, new Vector2(1f, 0f));
            Assert.AreEqual(1f, GameInput.Move, 0.01f);
        }

        [Test]
        public void Jump_IsPressedWithSpaceAndGamepadSouth()
        {
            Assert.IsFalse(GameInput.JumpPressed);
            Press(_keyboard.spaceKey);
            Assert.IsTrue(GameInput.JumpPressed);
            Release(_keyboard.spaceKey);

            var gamepad = InputSystem.AddDevice<Gamepad>();
            Press(gamepad.buttonSouth);
            Assert.IsTrue(GameInput.JumpPressed);
        }

        [Test]
        public void Interact_IsPressedWithEAndGamepadWest()
        {
            Press(_keyboard.eKey);
            Assert.IsTrue(GameInput.InteractPressed);
            Release(_keyboard.eKey);

            var gamepad = InputSystem.AddDevice<Gamepad>();
            Press(gamepad.buttonWest);
            Assert.IsTrue(GameInput.InteractPressed);
        }

        [Test]
        public void Inventory_IsPressedWithIAndTab()
        {
            Press(_keyboard.iKey);
            Assert.IsTrue(GameInput.InventoryPressed);
            Release(_keyboard.iKey);

            Press(_keyboard.tabKey);
            Assert.IsTrue(GameInput.InventoryPressed);
        }

        [Test]
        public void Confirm_AndCancel_MatchTheirKeys()
        {
            Press(_keyboard.enterKey);
            Assert.IsTrue(GameInput.ConfirmPressed);
            Assert.IsFalse(GameInput.CancelPressed);
            Release(_keyboard.enterKey);

            Press(_keyboard.escapeKey);
            Assert.IsTrue(GameInput.CancelPressed);
        }

        [Test]
        public void Navigate_UsesWSAndArrows()
        {
            Press(_keyboard.wKey);
            Assert.IsTrue(GameInput.NavigateUpPressed);
            Assert.IsFalse(GameInput.NavigateDownPressed);
            Release(_keyboard.wKey);

            Press(_keyboard.downArrowKey);
            Assert.IsTrue(GameInput.NavigateDownPressed);
        }

        [Test]
        public void Click_UsesTheLeftMouseButton()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            Assert.IsFalse(GameInput.ClickPressed);

            Press(mouse.leftButton);

            Assert.IsTrue(GameInput.ClickPressed);
        }

        [Test]
        public void ChoiceShortcuts_ReturnTheIndexAndRespectTheCount()
        {
            Press(_keyboard.digit2Key);
            Assert.IsTrue(GameInput.TryGetChoicePressed(3, out int index));
            Assert.AreEqual(1, index);
            Release(_keyboard.digit2Key);

            Press(_keyboard.digit3Key);
            Assert.IsFalse(GameInput.TryGetChoicePressed(2, out _), "there is no third choice");
        }

        [Test]
        public void Asset_IsARuntimeCopyOfTheProjectAsset()
        {
            var projectAsset = Resources.Load<InputActionAsset>("GameInput");

            Assert.IsNotNull(projectAsset);
            Assert.AreNotSame(projectAsset, GameInput.Asset);
        }
    }
}
