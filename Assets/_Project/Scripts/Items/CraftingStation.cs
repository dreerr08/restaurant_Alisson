using AliGame.Core;
using AliGame.Data;
using AliGame.UI;
using UnityEngine;

namespace AliGame.Items
{
    /// <summary>
    /// A workbench (Stove, CutStation, MixStation). Stand near it and press E (or gamepad West) to open the
    /// crafting panel with this station's recipes. Crafted items spawn above the station.
    /// </summary>
    public class CraftingStation : Interactable
    {
        [SerializeField] private StationType stationType;
        [SerializeField] private CraftingUI craftingUI;

        [Header("Crafted item spawn")]
        [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 1.4f);
        [SerializeField, Min(0f)] private float launchSpeed = 4f;
        [SerializeField, Min(0f)] private float launchSpreadX = 1.5f;
        [SerializeField, Min(0f)] private float collectDelay = 0.8f;

        public StationType Type => stationType;

        protected override string PromptText => "Usar " + stationType.DisplayName();

        protected override bool CanInteract => craftingUI != null && !craftingUI.IsBusy;

        protected override void Initialize()
        {
            if (craftingUI == null) craftingUI = FindFirstObjectByType<CraftingUI>();
            if (craftingUI == null) Debug.LogWarning("CraftingStation: no CraftingUI found in the scene.", this);
        }

        protected override void Interact(GameObject interactor)
        {
            craftingUI.Open(stationType, this);
        }

        /// <summary>Spawns a crafted item in the world above the station, launched a little so it pops out.</summary>
        public void SpawnCrafted(RecipeSO recipe)
        {
            Vector2 position = (Vector2)transform.position + spawnOffset;
            var velocity = new Vector2(Random.Range(-launchSpreadX, launchSpreadX), launchSpeed);
            ItemPickup.Spawn(recipe.Result, recipe.ResultAmount, position, velocity, collectDelay);
        }
    }
}
