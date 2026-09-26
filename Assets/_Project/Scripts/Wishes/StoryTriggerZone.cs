using AliGame.Items;
using UnityEngine;

namespace AliGame.Wishes
{
    /// <summary>
    /// An area that raises a story trigger the first time the player walks into it (e.g. reaching the garden, or the
    /// back door). Characters waiting on that trigger as a wish's Start Trigger can then ask. Needs a Collider2D, which
    /// is turned into a trigger; the player is anything with an Inventory.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class StoryTriggerZone : MonoBehaviour
    {
        [Tooltip("The trigger id to raise, the same text a wish has as its Start Trigger.")]
        [SerializeField] private string trigger;
        [SerializeField] private WishJournal journal;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void Awake()
        {
            if (journal == null) journal = FindFirstObjectByType<WishJournal>();
            if (journal == null) Debug.LogWarning("StoryTriggerZone: no WishJournal found in the scene.", this);
            if (string.IsNullOrWhiteSpace(trigger)) Debug.LogWarning("StoryTriggerZone: no trigger id set.", this);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (journal == null || other.GetComponentInParent<Inventory>() == null) return;
            journal.RaiseTrigger(trigger);
        }

        private void OnDrawGizmos()
        {
            var box = GetComponent<Collider2D>();
            if (box == null) return;

            Gizmos.color = new Color(0.55f, 0.72f, 0.88f, 0.25f);
            Gizmos.DrawCube(box.bounds.center, box.bounds.size);
        }
    }
}
