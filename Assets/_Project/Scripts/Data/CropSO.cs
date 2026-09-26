using System.Collections.Generic;
using UnityEngine;

namespace AliGame.Data
{
    /// <summary>
    /// A plant: which seed grows it, how many days each growth stage takes, and what it yields. A new plant is just a
    /// new asset of this type; no code. Stages and days are counted in game days (see GameClock).
    /// </summary>
    [CreateAssetMenu(fileName = "NewCrop", menuName = "Ali/Crop")]
    public class CropSO : ScriptableObject
    {
        [System.Serializable]
        public struct Stage
        {
            [Tooltip("Drawn while the plant is in this stage. Empty draws a colored placeholder.")]
            [SerializeField] private Sprite sprite;
            [Tooltip("Game days this stage takes to grow once it has been watered.")]
            [SerializeField, Min(1)] private int days;

            public Sprite Sprite => sprite;
            public int Days => Mathf.Max(1, days);
        }

        [SerializeField] private string displayName = "Nova Planta";

        [Header("Seed and harvest")]
        [Tooltip("The item consumed to plant this crop.")]
        [SerializeField] private ItemSO seed;
        [Tooltip("The item that drops when the plant is harvested.")]
        [SerializeField] private ItemSO harvest;
        [SerializeField, Min(1)] private int harvestAmount = 1;

        [Header("Growth")]
        [Tooltip("The growth stages, in order. When the last one finishes the plant is ready to harvest.")]
        [SerializeField] private List<Stage> stages = new List<Stage> { new Stage(), new Stage() };
        [Tooltip("On: every stage needs its own watering. Off: only the first stage does, then it grows by itself.")]
        [SerializeField] private bool waterEveryStage = true;

        [Header("Look")]
        [Tooltip("Drawn when the plant is ready to harvest. Empty draws a colored placeholder.")]
        [SerializeField] private Sprite readySprite;
        [Tooltip("Color of the placeholder drawn while there is no sprite.")]
        [SerializeField] private Color placeholderColor = new Color(0.31f, 0.62f, 0.25f, 1f);
        [Tooltip("Color of the placeholder when the plant is ready to harvest.")]
        [SerializeField] private Color readyPlaceholderColor = new Color(0.86f, 0.25f, 0.2f, 1f);

        public string DisplayName => displayName;
        public ItemSO Seed => seed;
        public ItemSO Harvest => harvest;
        public int HarvestAmount => Mathf.Max(1, harvestAmount);
        public IReadOnlyList<Stage> Stages => stages;
        public bool WaterEveryStage => waterEveryStage;
        public Sprite ReadySprite => readySprite;
        public Color PlaceholderColor => placeholderColor;
        public Color ReadyPlaceholderColor => readyPlaceholderColor;

        /// <summary>Days each stage takes, in order.</summary>
        public int[] StageDays()
        {
            var days = new int[stages.Count];
            for (int i = 0; i < days.Length; i++) days[i] = stages[i].Days;
            return days;
        }

        /// <summary>True when the crop has a seed, something to harvest and at least one stage.</summary>
        public bool IsValid => seed != null && harvest != null && stages.Count > 0;
    }
}
