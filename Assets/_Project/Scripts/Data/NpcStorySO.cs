using System.Collections.Generic;
using UnityEngine;

namespace AliGame.Data
{
    /// <summary>
    /// A character's whole arc: how they get to know the player, their wishes in order, the small talk for when
    /// nothing is pending, and the dialogue that closes the arc once every wish is done.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNpcStory", menuName = "Ali/NPC Story")]
    public class NpcStorySO : ScriptableObject
    {
        [SerializeField] private DialogueCharacterSO character;
        [Header("Getting to know each other")]
        [Tooltip("Conversations that build the relationship, in order: each time the player talks to them, the next one plays. They ask for nothing until the player has been through all of them. Empty means no introduction is needed.")]
        [SerializeField] private List<DialogueSO> introDialogues = new List<DialogueSO>();

        [Header("Wishes")]
        [Tooltip("Wishes in the order they are asked. Each one unlocks when the previous is delivered.")]
        [SerializeField] private List<WishSO> wishes = new List<WishSO>();
        [Tooltip("Small talk for when there is nothing pending. They rotate.")]
        [SerializeField] private List<DialogueSO> idleDialogues = new List<DialogueSO>();
        [Tooltip("Played once when the last wish is delivered.")]
        [SerializeField] private DialogueSO farewellDialogue;

        public DialogueCharacterSO Character => character;
        public IReadOnlyList<DialogueSO> IntroDialogues => introDialogues;
        public IReadOnlyList<WishSO> Wishes => wishes;
        public IReadOnlyList<DialogueSO> IdleDialogues => idleDialogues;
        public DialogueSO FarewellDialogue => farewellDialogue;

        /// <summary>Identifies this story in a save file.</summary>
        public string Id => name;
    }
}
