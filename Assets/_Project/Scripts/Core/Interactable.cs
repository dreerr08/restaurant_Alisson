using System.Collections.Generic;
using AliGame.Items;
using AliGame.Movement;
using AliGame.UI;
using UnityEngine;

namespace AliGame.Core
{
    /// <summary>
    /// Base for anything the player uses by standing close and pressing the Interact button: crafting stations,
    /// NPCs to talk to... It detects the player (anything with an Inventory) within Interact Radius, shows the
    /// "[E] ..." prompt and calls Interact. When several overlap, only the nearest one reacts.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        private static readonly List<Interactable> All = new List<Interactable>();

        [SerializeField] private LayerMask interactorLayers = ~0;
        [SerializeField, Min(0f)] private float interactRadius = 2.5f;
        [SerializeField] private Vector2 interactOffset;

        private readonly Collider2D[] _hits = new Collider2D[8];
        private ContactFilter2D _filter;
        private Inventory _interactor;
        private PlayerMovement2D _player;
        private bool _inRange;
        private float _distance;

        /// <summary>Text after the key in the prompt, e.g. "Falar com Tio Ben".</summary>
        protected abstract string PromptText { get; }

        /// <summary>Extra condition, e.g. not while a panel or a conversation is already open.</summary>
        protected virtual bool CanInteract => true;

        protected abstract void Interact(GameObject interactor);

        protected virtual void Initialize()
        {
        }

        protected virtual void Awake()
        {
            _filter = new ContactFilter2D { useTriggers = true };
            _filter.SetLayerMask(interactorLayers);
            Initialize();
        }

        protected virtual void OnEnable() => All.Add(this);

        protected virtual void OnDisable()
        {
            All.Remove(this);
            _inRange = false;
            InteractionPromptUI.Hide(this);
        }

        protected virtual void Update()
        {
            DetectPlayer();

            bool canUse = _inRange
                && CanInteract
                && IsNearest()
                && (_player == null || _player.InputEnabled);

            if (!canUse)
            {
                InteractionPromptUI.Hide(this);
                return;
            }

            InteractionPromptUI.Show(this, "E", PromptText);
            if (GameInput.InteractPressed) Interact(_interactor.gameObject);
        }

        private void DetectPlayer()
        {
            _inRange = false;
            _interactor = null;
            _player = null;

            Vector2 center = (Vector2)transform.position + interactOffset;
            int count = Physics2D.OverlapCircle(center, interactRadius, _filter, _hits);

            for (int i = 0; i < count; i++)
            {
                var inventory = _hits[i].GetComponentInParent<Inventory>();
                if (inventory == null) continue;

                _inRange = true;
                _interactor = inventory;
                _player = inventory.GetComponent<PlayerMovement2D>();
                _distance = Vector2.Distance(center, inventory.transform.position);
                return;
            }
        }

        private bool IsNearest()
        {
            foreach (Interactable other in All)
            {
                if (other == this || !other._inRange) continue;
                if (other._distance < _distance) return false;
                if (Mathf.Approximately(other._distance, _distance) && other.GetInstanceID() < GetInstanceID()) return false;
            }
            return true;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.94f, 0.66f, 0.23f, 0.9f);
            Gizmos.DrawWireSphere((Vector2)transform.position + interactOffset, interactRadius);
        }
    }
}
