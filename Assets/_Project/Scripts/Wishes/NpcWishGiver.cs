using AliGame.Data;
using AliGame.Dialogue;
using AliGame.Items;
using UnityEngine;

namespace AliGame.Wishes
{
    /// <summary>
    /// Puts a character's story behind their DialogueTrigger: what they say follows how well they know the player,
    /// what they have asked for and what the player is carrying. Each introduction talk that ends brings them
    /// closer; only after all of them, and once the wish's start trigger has been raised, do they ask. When the
    /// delivery dialogue ends, the items change hands, the wish is completed and any reward is spawned in the world.
    /// </summary>
    [RequireComponent(typeof(DialogueTrigger))]
    public class NpcWishGiver : MonoBehaviour, IDialogueSource
    {
        [SerializeField] private NpcStorySO story;
        [SerializeField] private WishJournal journal;
        [SerializeField] private Inventory inventory;

        [Header("Reward spawn")]
        [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 1.6f);
        [SerializeField, Min(0f)] private float launchSpeed = 4f;
        [SerializeField, Min(0f)] private float launchSpreadX = 1.2f;
        [SerializeField, Min(0f)] private float collectDelay = 0.8f;

        private DialogueRunner _runner;
        private WishDialogueResult _pending;

        public NpcStorySO Story => story;

        /// <summary>They have something new to ask for.</summary>
        public bool HasNewWish => Resolve().Kind == WishDialogueKind.Offer;

        /// <summary>They are still getting to know the player: there is an introduction talk waiting.</summary>
        public bool WantsToTalk => Resolve().Kind == WishDialogueKind.Intro;

        /// <summary>The player is carrying everything this character is waiting for.</summary>
        public bool CanDeliver => Resolve().Kind == WishDialogueKind.Delivery;

        /// <summary>The wish this character is waiting on, or null.</summary>
        public WishSO ActiveWish => journal != null && journal.Model.IsOffered(story) ? journal.Model.CurrentWish(story) : null;

        public DialogueSO GetDialogue() => Resolve().Dialogue;

        private void Awake()
        {
            if (journal == null) journal = FindFirstObjectByType<WishJournal>();
            if (inventory == null) inventory = FindFirstObjectByType<Inventory>();
            _runner = FindFirstObjectByType<DialogueRunner>();

            if (journal != null) journal.Model.Register(story);
            if (story == null) Debug.LogWarning("NpcWishGiver: no story assigned.", this);
        }

        private void OnEnable()
        {
            if (_runner == null) return;
            _runner.Started += OnDialogueStarted;
            _runner.Ended += OnDialogueEnded;
        }

        private void OnDisable()
        {
            if (_runner == null) return;
            _runner.Started -= OnDialogueStarted;
            _runner.Ended -= OnDialogueEnded;
        }

        private void OnDialogueStarted(DialogueContext context)
        {
            if (context.Speaker != transform || journal == null) return;

            _pending = Resolve();
            if (_pending.Kind == WishDialogueKind.Reminder) journal.Model.AdvanceReminder(story);
        }

        private void OnDialogueEnded(DialogueContext context)
        {
            if (context.Speaker != transform || journal == null) return;

            switch (_pending.Kind)
            {
                case WishDialogueKind.Intro:
                    journal.Model.MarkIntroSeen(story);
                    break;

                case WishDialogueKind.Offer:
                    journal.Model.Offer(story);
                    break;

                case WishDialogueKind.Delivery:
                    Deliver(_pending.Wish);
                    break;

                case WishDialogueKind.Farewell:
                    journal.Model.MarkFarewellSeen(story);
                    break;

                case WishDialogueKind.Idle:
                    journal.Model.AdvanceReminder(story);
                    break;
            }

            _pending = WishDialogueResult.None;
        }

        private void Deliver(WishSO wish)
        {
            if (wish == null || inventory == null) return;
            if (!WishDialogueResolver.HasEverything(wish, inventory.Model)) return;

            foreach (ItemAmount requirement in wish.Requirements)
                inventory.Remove(requirement.Item, requirement.Amount);

            journal.Model.CompleteCurrent(story);

            foreach (ItemAmount reward in wish.Rewards)
            {
                if (reward.Item == null) continue;

                Vector2 position = (Vector2)transform.position + spawnOffset;
                var velocity = new Vector2(Random.Range(-launchSpreadX, launchSpreadX), launchSpeed);
                ItemPickup.Spawn(reward.Item, reward.Amount, position, velocity, collectDelay);
            }
        }

        private WishDialogueResult Resolve()
        {
            if (journal == null || inventory == null) return WishDialogueResult.None;
            return WishDialogueResolver.Resolve(story, journal.Model, inventory.Model);
        }
    }
}
