using System.Collections.Generic;
using UnityEngine;

namespace AliGame.Data
{
    /// <summary>
    /// One step of an NPC's story: they ask for items, you bring them, the story moves on.
    /// The item is the vehicle; the dialogues carry why it matters.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWish", menuName = "Ali/Wish")]
    public class WishSO : ScriptableObject
    {
        [Tooltip("Short line shown in the journal, e.g. 'O molho da avó'.")]
        [SerializeField] private string title = "Novo desejo";
        [Tooltip("Why they want it. Shown under the title in the journal.")]
        [SerializeField, TextArea(2, 4)] private string note;
        [SerializeField] private List<ItemAmount> requirements = new List<ItemAmount>();

        [Header("Dialogues")]
        [Tooltip("Played the first time they ask. When it ends the wish goes into the journal.")]
        [SerializeField] private DialogueSO offerDialogue;
        [Tooltip("Played when you talk to them without the items. They rotate, so talking again is not identical.")]
        [SerializeField] private List<DialogueSO> reminderDialogues = new List<DialogueSO>();
        [Tooltip("Played when you arrive with everything. When it ends the items are handed over.")]
        [SerializeField] private DialogueSO deliveryDialogue;

        [Header("Reward")]
        [Tooltip("Spawned in the world by the NPC when the wish is completed.")]
        [SerializeField] private List<ItemAmount> rewards = new List<ItemAmount>();

        public string Title => string.IsNullOrEmpty(title) ? name : title;
        public string Note => note ?? string.Empty;
        public IReadOnlyList<ItemAmount> Requirements => requirements;
        public DialogueSO OfferDialogue => offerDialogue;
        public IReadOnlyList<DialogueSO> ReminderDialogues => reminderDialogues;
        public DialogueSO DeliveryDialogue => deliveryDialogue;
        public IReadOnlyList<ItemAmount> Rewards => rewards;

        /// <summary>False when the wish could never be completed (no items asked, or an empty item slot).</summary>
        public bool IsValid
        {
            get
            {
                if (requirements == null || requirements.Count == 0) return false;
                foreach (ItemAmount requirement in requirements)
                {
                    if (requirement.Item == null) return false;
                }
                return deliveryDialogue != null;
            }
        }
    }
}
