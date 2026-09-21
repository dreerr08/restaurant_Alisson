using System.Collections;
using System.Collections.Generic;
using AliGame.Data;
using AliGame.Items;
using UnityEngine;
using UnityEngine.UI;

namespace AliGame.UI
{
    /// <summary>
    /// Shows a small toast in a screen corner whenever the inventory gains an item.
    /// Picking up the same item again while its toast is visible just raises the count.
    /// </summary>
    public class ItemNotificationUI : MonoBehaviour
    {
        public enum Corner { TopRight, TopLeft, BottomRight, BottomLeft }

        [SerializeField] private Inventory inventory;
        [SerializeField] private Font font;
        [SerializeField] private Corner corner = Corner.TopRight;
        [Tooltip("Seconds between collecting an item and its notification appearing. Items collected during the delay are summed.")]
        public float showDelay = 0.5f;
        [SerializeField] private float margin = 32f;
        [SerializeField] private float spacing = 10f;
        [SerializeField] private float duration = 2.5f;
        [SerializeField, Min(1)] private int maxVisible = 5;
        [SerializeField, Min(0.5f)] private float uiScale = 1.3f;

        private const float SlideDistance = 90f;

        private readonly Dictionary<ItemSO, ItemToast> _active = new Dictionary<ItemSO, ItemToast>();
        private readonly Dictionary<ItemSO, int> _pending = new Dictionary<ItemSO, int>();
        private readonly List<ItemToast> _order = new List<ItemToast>();
        private RectTransform _container;
        private Font _font;

        private bool IsTop => corner == Corner.TopRight || corner == Corner.TopLeft;

        private bool IsRight => corner == Corner.TopRight || corner == Corner.BottomRight;

        private void Awake()
        {
            if (inventory == null) inventory = FindFirstObjectByType<Inventory>();
            if (inventory == null)
            {
                Debug.LogWarning("ItemNotificationUI: no Inventory found in the scene.", this);
                enabled = false;
                return;
            }

            _font = UIStyle.ResolveFont(font);
            BuildCanvas();
        }

        private void OnEnable()
        {
            if (inventory != null) inventory.ItemAdded += OnItemAdded;
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.ItemAdded -= OnItemAdded;
            _pending.Clear();
        }

        private void OnItemAdded(ItemSO item, int amount)
        {
            if (showDelay <= 0f)
            {
                ShowToast(item, amount);
                return;
            }

            if (_pending.ContainsKey(item))
            {
                _pending[item] += amount;
                return;
            }

            _pending[item] = amount;
            StartCoroutine(ShowAfterDelay(item));
        }

        private IEnumerator ShowAfterDelay(ItemSO item)
        {
            yield return new WaitForSecondsRealtime(showDelay);

            if (_pending.TryGetValue(item, out int total))
            {
                _pending.Remove(item);
                ShowToast(item, total);
            }
        }

        private void ShowToast(ItemSO item, int amount)
        {
            if (_active.TryGetValue(item, out ItemToast existing) && existing != null && existing.IsAlive)
            {
                existing.Add(amount);
                return;
            }

            ItemToast toast = ItemToast.Create(_container, item, amount, _font, IsRight ? SlideDistance : -SlideDistance, duration);
            if (IsTop) toast.transform.SetAsFirstSibling();

            _active[item] = toast;
            _order.Add(toast);
            TrimOldest();
        }

        private void TrimOldest()
        {
            _order.RemoveAll(t => t == null || !t.IsAlive);
            while (_order.Count > maxVisible)
            {
                _order[0].Dismiss();
                _order.RemoveAt(0);
            }
        }

        private void BuildCanvas()
        {
            var canvasObject = new GameObject("NotificationCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.sortingOrder = 90;
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = mainCamera;
                canvas.planeDistance = 1f;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f) / uiScale;
            scaler.matchWidthOrHeight = 0.5f;

            var containerObject = new GameObject("Toasts", typeof(RectTransform), typeof(VerticalLayoutGroup));
            containerObject.transform.SetParent(canvasObject.transform, false);
            _container = (RectTransform)containerObject.transform;

            Vector2 anchor = new Vector2(IsRight ? 1f : 0f, IsTop ? 1f : 0f);
            _container.anchorMin = _container.anchorMax = _container.pivot = anchor;
            _container.sizeDelta = new Vector2(330f, 900f);
            _container.anchoredPosition = new Vector2(IsRight ? -margin : margin, IsTop ? -margin : margin);

            var layout = containerObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = IsTop
                ? (IsRight ? TextAnchor.UpperRight : TextAnchor.UpperLeft)
                : (IsRight ? TextAnchor.LowerRight : TextAnchor.LowerLeft);
        }
    }
}
