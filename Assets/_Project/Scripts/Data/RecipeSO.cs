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

        public StationType Station => station;
        public ItemSO Result => result;
        public int ResultAmount => Mathf.Max(1, resultAmount);
        public IReadOnlyList<Ingredient> Ingredients => ingredients;
        public string DisplayName => result != null ? result.DisplayName : name;
    }
}
