using System.Collections.Generic;
using UnityEngine;

namespace AliGame.Data
{
    /// <summary>A crafting recipe: consumes the ingredients and always produces one ItemSO.</summary>
    [CreateAssetMenu(fileName = "NewRecipe", menuName = "Ali/Recipe")]
    public class RecipeSO : ScriptableObject
    {
        [System.Serializable]
        public struct Ingredient
        {
            [SerializeField] private ItemSO item;
            [SerializeField, Min(1)] private int amount;

            public ItemSO Item => item;
            public int Amount => Mathf.Max(1, amount);
        }

        [Tooltip("The only station where this recipe can be crafted.")]
        [SerializeField] private StationType station;
        [SerializeField] private ItemSO result;
        [SerializeField, Min(1)] private int resultAmount = 1;
        [SerializeField] private List<Ingredient> ingredients = new List<Ingredient>();

        [Header("Cut minigame (CutStation only)")]
        [Tooltip("At the CutStation this recipe is made by holding the interact key instead of a single click.")]
        [SerializeField] private bool useCutMinigame = true;
        [Tooltip("Seconds the key must be held for one cut to count.")]
        [SerializeField, Min(0.1f)] private float cutHoldSeconds = 3f;
        [Tooltip("After the bar is full, the seconds the player has to release the key. Holding longer misses the cut.")]
        [SerializeField, Min(0.1f)] private float cutReleaseWindow = 1f;
        [Tooltip("How many cuts are needed before the item drops.")]
        [SerializeField, Min(1)] private int cutCount = 3;

        public StationType Station => station;
        public ItemSO Result => result;
        public int ResultAmount => Mathf.Max(1, resultAmount);
        public IReadOnlyList<Ingredient> Ingredients => ingredients;
        public string DisplayName => result != null ? result.DisplayName : name;
        public bool UsesCutMinigame => useCutMinigame && station == StationType.CutStation;
        public float CutHoldSeconds => Mathf.Max(0.1f, cutHoldSeconds);
        public float CutReleaseWindow => Mathf.Max(0.1f, cutReleaseWindow);
        public int CutCount => Mathf.Max(1, cutCount);
    }
}
