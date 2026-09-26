using System.Collections.Generic;
using AliGame.Core;
using AliGame.Data;
using AliGame.Items;
using AliGame.Movement;
using AliGame.UI;
using UnityEngine;

namespace AliGame.Farming
{
    /// <summary>
    /// One garden plot in the scene. Stand close and press E: plant a seed you carry, water the plant (needs the watering can
    /// item, if one is set), and harvest it when it is ready; the harvest drops as pickups. Plants grow one day at a time
    /// (see GameClock) and only after being watered; they never wither. Until a crop has its own sprites it is drawn as
    /// a colored placeholder that gets taller as it grows, with a blue mark while it wants water.
    /// Carrying seeds for more than one of this plot's crops opens a small popup (PlantSeedUI) to pick which one to plant.
    /// </summary>
    public class Plot : Interactable
    {
        private static Sprite _square;

        [Header("Crops")]
        [Tooltip("The crops this plot can grow. With one seed carried it plants that one directly; with more than one it asks.")]
        [SerializeField] private List<CropSO> crops = new List<CropSO>();
        [Tooltip("The item needed to water. It is not used up. Leave empty to water without one.")]
        [SerializeField] private ItemSO wateringCan;
        [Tooltip("Popup used to pick a seed when more than one is carried. Found in the scene if left empty.")]
        [SerializeField] private PlantSeedUI plantSeedUI;

        [Header("Harvest drop")]
        [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 1.2f);
        [SerializeField, Min(0f)] private float launchSpeed = 4f;
        [SerializeField, Min(0f)] private float launchSpreadX = 1.2f;
        [SerializeField, Min(0f)] private float collectDelay = 0.8f;

        [Header("Placeholder look (used while a crop has no sprites)")]
        [SerializeField] private Vector2 plantOffset = new Vector2(0f, 0.1f);
        [SerializeField, Min(0.1f)] private float plantWidth = 0.7f;
        [SerializeField, Min(0.1f)] private float plantMinHeight = 0.35f;
        [SerializeField, Min(0.1f)] private float plantMaxHeight = 1.5f;
        [SerializeField] private Color thirstColor = new Color(0.25f, 0.55f, 0.9f, 1f);
        [SerializeField] private Vector2 thirstOffset = new Vector2(0f, 1.9f);
        [SerializeField, Min(0.05f)] private float thirstSize = 0.35f;

        private readonly PlotModel _model = new PlotModel();
        private Inventory _inventory;
        private GameClock _clock;
        private PlayerMovement2D _playerMovement;
        private CropSO _crop;
        private SpriteRenderer _plantRenderer;
        private SpriteRenderer _thirstRenderer;

        public PlotModel Model => _model;

        public CropSO Crop => _crop;

        protected override string PromptText
        {
            get
            {
                switch (_model.State)
                {
                    case PlotState.Empty:
                        CropSO plantable = FirstPlantable();
                        if (plantable == null) return "Sem sementes";
                        return HasMultiplePlantable() ? "Escolher semente" : "Plantar " + plantable.DisplayName;
                    case PlotState.NeedsWater:
                        return CanWater() ? "Regar " + _crop.DisplayName : "Precisa de regador";
                    case PlotState.Ready:
                        return "Colher " + _crop.DisplayName;
                    default:
                        return string.Empty;
                }
            }
        }

        protected override bool CanInteract => _model.State != PlotState.Growing;

        protected override void Initialize()
        {
            _inventory = FindFirstObjectByType<Inventory>();
            if (_inventory != null) _playerMovement = _inventory.GetComponent<PlayerMovement2D>();
            if (plantSeedUI == null) plantSeedUI = FindFirstObjectByType<PlantSeedUI>();
            BuildRenderers();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _model.Changed += UpdateVisual;

            if (_clock == null) _clock = GameClock.Current != null ? GameClock.Current : FindFirstObjectByType<GameClock>();
            if (_clock != null) _clock.DayStarted += OnDayStarted;
            else Debug.LogWarning("Plot: no GameClock in the scene, so plants will never grow.", this);

            UpdateVisual();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _model.Changed -= UpdateVisual;
            if (_clock != null) _clock.DayStarted -= OnDayStarted;
        }

        protected override void Interact(GameObject interactor)
        {
            if (_inventory == null)
            {
                _inventory = interactor.GetComponent<Inventory>();
                _playerMovement = interactor.GetComponent<PlayerMovement2D>();
            }

            switch (_model.State)
            {
                case PlotState.Empty:
                    TryPlant();
                    break;
                case PlotState.NeedsWater:
                    if (CanWater()) _model.Water();
                    break;
                case PlotState.Ready:
                    Harvest();
                    break;
            }
        }

        private void OnDayStarted(int day)
        {
            _model.NewDay();
        }

        private CropSO FirstPlantable()
        {
            if (_inventory == null) return null;

            foreach (CropSO crop in crops)
            {
                if (crop != null && crop.IsValid && _inventory.Has(crop.Seed)) return crop;
            }
            return null;
        }

        /// <summary>True when the player carries seeds for more than one of this plot's crops.</summary>
        private bool HasMultiplePlantable()
        {
            if (_inventory == null) return false;

            int count = 0;
            foreach (CropSO crop in crops)
            {
                if (crop == null || !crop.IsValid || !_inventory.Has(crop.Seed)) continue;
                count++;
                if (count > 1) return true;
            }
            return false;
        }

        private List<CropSO> PlantableCrops()
        {
            var options = new List<CropSO>();
            if (_inventory == null) return options;

            foreach (CropSO crop in crops)
            {
                if (crop != null && crop.IsValid && _inventory.Has(crop.Seed)) options.Add(crop);
            }
            return options;
        }

        private bool CanWater()
        {
            return wateringCan == null || (_inventory != null && _inventory.Has(wateringCan));
        }

        private void TryPlant()
        {
            List<CropSO> options = PlantableCrops();
            if (options.Count == 0) return;

            if (options.Count == 1)
            {
                PlantWith(options[0]);
                return;
            }

            if (plantSeedUI == null) plantSeedUI = FindFirstObjectByType<PlantSeedUI>();
            if (plantSeedUI != null) plantSeedUI.Open(options, _playerMovement, PlantWith);
            else PlantWith(options[0]);
        }

        private void PlantWith(CropSO crop)
        {
            // The popup can close a moment after the player walked away or watered another plant by other means;
            // re-check everything instead of trusting the state from when it was opened.
            if (crop == null || !crop.IsValid || _model.State != PlotState.Empty) return;
            if (_inventory == null || !_inventory.Remove(crop.Seed)) return;

            _crop = crop;
            _model.Plant(crop.StageDays(), crop.WaterEveryStage);
        }

        private void Harvest()
        {
            CropSO crop = _crop;
            if (!_model.Harvest()) return;

            Vector2 position = (Vector2)transform.position + spawnOffset;
            var velocity = new Vector2(Random.Range(-launchSpreadX, launchSpreadX), launchSpeed);
            ItemPickup.Spawn(crop.Harvest, crop.HarvestAmount, position, velocity, collectDelay);
            _crop = null;
        }

        private void BuildRenderers()
        {
            _plantRenderer = CreateRenderer("Plant", 2);
            _thirstRenderer = CreateRenderer("Thirst", 3);
        }

        private SpriteRenderer CreateRenderer(string childName, int order)
        {
            Transform existing = transform.Find(childName);
            if (existing != null) Destroy(existing.gameObject);

            var child = new GameObject(childName);
            child.transform.SetParent(transform, false);

            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            renderer.enabled = false;
            return renderer;
        }

        private void UpdateVisual()
        {
            if (_plantRenderer == null) return;

            bool planted = _model.State != PlotState.Empty && _crop != null;
            _plantRenderer.enabled = planted;
            _thirstRenderer.enabled = planted && _model.State == PlotState.NeedsWater;
            if (!planted) return;

            Sprite sprite = SpriteForState();
            if (sprite != null)
            {
                _plantRenderer.sprite = sprite;
                _plantRenderer.color = Color.white;
                _plantRenderer.transform.localPosition = plantOffset;
                _plantRenderer.transform.localScale = Vector3.one;
            }
            else
            {
                float height = Mathf.Lerp(plantMinHeight, plantMaxHeight, _model.Progress);
                Color color = _model.State == PlotState.Ready ? _crop.ReadyPlaceholderColor : _crop.PlaceholderColor;
                if (_model.State == PlotState.NeedsWater) color = Color.Lerp(color, new Color(0.55f, 0.5f, 0.3f, 1f), 0.5f);

                _plantRenderer.sprite = Square();
                _plantRenderer.color = color;
                _plantRenderer.transform.localPosition = plantOffset;
                _plantRenderer.transform.localScale = new Vector3(plantWidth, height, 1f);
            }

            _thirstRenderer.sprite = Square();
            _thirstRenderer.color = thirstColor;
            _thirstRenderer.transform.localPosition = thirstOffset;
            _thirstRenderer.transform.localScale = new Vector3(thirstSize, thirstSize, 1f);
        }

        private Sprite SpriteForState()
        {
            if (_model.State == PlotState.Ready) return _crop.ReadySprite;

            int index = Mathf.Clamp(_model.Stage, 0, _crop.Stages.Count - 1);
            return _crop.Stages[index].Sprite;
        }

        /// <summary>A 1 x 1 unit white square whose pivot is at the bottom, so scaling Y grows it upward.</summary>
        private static Sprite Square()
        {
            if (_square == null)
                _square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0f), 4f);
            return _square;
        }
    }
}
