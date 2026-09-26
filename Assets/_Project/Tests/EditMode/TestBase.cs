using System.Collections.Generic;
using AliGame.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AliGame.Tests
{
    /// <summary>Creates throwaway ScriptableObjects for tests and destroys them afterwards.</summary>
    public abstract class TestBase
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void DestroyCreatedObjects()
        {
            foreach (Object created in _created)
            {
                if (created != null) Object.DestroyImmediate(created);
            }
            _created.Clear();
        }

        protected ItemSO Item(string name, int maxStack = 99)
        {
            var item = Track(ScriptableObject.CreateInstance<ItemSO>());
            item.name = name;

            var so = new SerializedObject(item);
            so.FindProperty("displayName").stringValue = name;
            so.FindProperty("maxStack").intValue = maxStack;
            so.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        protected RecipeSO Recipe(StationType station, ItemSO result, int resultAmount, params (ItemSO item, int amount)[] ingredients)
        {
            var recipe = Track(ScriptableObject.CreateInstance<RecipeSO>());
            var so = new SerializedObject(recipe);
            so.FindProperty("station").enumValueIndex = (int)station;
            so.FindProperty("result").objectReferenceValue = result;
            so.FindProperty("resultAmount").intValue = resultAmount;

            SerializedProperty list = so.FindProperty("ingredients");
            list.arraySize = ingredients.Length;
            for (int i = 0; i < ingredients.Length; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("item").objectReferenceValue = ingredients[i].item;
                element.FindPropertyRelative("amount").intValue = ingredients[i].amount;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return recipe;
        }

        protected DialogueCharacterSO Character(string displayName)
        {
            var character = Track(ScriptableObject.CreateInstance<DialogueCharacterSO>());
            var so = new SerializedObject(character);
            so.FindProperty("displayName").stringValue = displayName;
            so.ApplyModifiedPropertiesWithoutUndo();
            return character;
        }

        protected DialogueSO Dialogue(DialogueCharacterSO defaultSpeaker, params string[] lines)
        {
            var dialogue = Track(ScriptableObject.CreateInstance<DialogueSO>());
            var so = new SerializedObject(dialogue);
            so.FindProperty("defaultSpeaker").objectReferenceValue = defaultSpeaker;

            SerializedProperty list = so.FindProperty("lines");
            list.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                SerializedProperty line = list.GetArrayElementAtIndex(i);
                line.FindPropertyRelative("text").stringValue = lines[i];
                line.FindPropertyRelative("choices").arraySize = 0;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return dialogue;
        }

        protected void AddChoices(DialogueSO dialogue, int lineIndex, params (string label, DialogueSO next)[] choices)
        {
            var so = new SerializedObject(dialogue);
            SerializedProperty list = so.FindProperty("lines").GetArrayElementAtIndex(lineIndex).FindPropertyRelative("choices");
            list.arraySize = choices.Length;
            for (int i = 0; i < choices.Length; i++)
            {
                SerializedProperty choice = list.GetArrayElementAtIndex(i);
                choice.FindPropertyRelative("label").stringValue = choices[i].label;
                choice.FindPropertyRelative("next").objectReferenceValue = choices[i].next;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        protected WishSO Wish(string title, DialogueSO offer, DialogueSO delivery, DialogueSO[] reminders, params (ItemSO item, int amount)[] requirements)
        {
            var wish = Track(ScriptableObject.CreateInstance<WishSO>());
            wish.name = title;

            var so = new SerializedObject(wish);
            so.FindProperty("title").stringValue = title;
            so.FindProperty("offerDialogue").objectReferenceValue = offer;
            so.FindProperty("deliveryDialogue").objectReferenceValue = delivery;

            SerializedProperty list = so.FindProperty("requirements");
            list.arraySize = requirements.Length;
            for (int i = 0; i < requirements.Length; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("item").objectReferenceValue = requirements[i].item;
                element.FindPropertyRelative("amount").intValue = requirements[i].amount;
            }

            SerializedProperty reminderList = so.FindProperty("reminderDialogues");
            reminderList.arraySize = reminders != null ? reminders.Length : 0;
            for (int i = 0; i < reminderList.arraySize; i++)
                reminderList.GetArrayElementAtIndex(i).objectReferenceValue = reminders[i];

            so.ApplyModifiedPropertiesWithoutUndo();
            return wish;
        }

        protected NpcStorySO Story(DialogueCharacterSO character, WishSO[] wishes, DialogueSO[] idleDialogues = null, DialogueSO farewell = null, DialogueSO[] introDialogues = null)
        {
            var story = Track(ScriptableObject.CreateInstance<NpcStorySO>());
            var so = new SerializedObject(story);
            so.FindProperty("character").objectReferenceValue = character;
            so.FindProperty("farewellDialogue").objectReferenceValue = farewell;

            SerializedProperty wishList = so.FindProperty("wishes");
            wishList.arraySize = wishes != null ? wishes.Length : 0;
            for (int i = 0; i < wishList.arraySize; i++)
                wishList.GetArrayElementAtIndex(i).objectReferenceValue = wishes[i];

            SerializedProperty idleList = so.FindProperty("idleDialogues");
            idleList.arraySize = idleDialogues != null ? idleDialogues.Length : 0;
            for (int i = 0; i < idleList.arraySize; i++)
                idleList.GetArrayElementAtIndex(i).objectReferenceValue = idleDialogues[i];

            SerializedProperty introList = so.FindProperty("introDialogues");
            introList.arraySize = introDialogues != null ? introDialogues.Length : 0;
            for (int i = 0; i < introList.arraySize; i++)
                introList.GetArrayElementAtIndex(i).objectReferenceValue = introDialogues[i];

            so.ApplyModifiedPropertiesWithoutUndo();
            return story;
        }

        protected void SetStartTrigger(WishSO wish, string trigger)
        {
            var so = new SerializedObject(wish);
            so.FindProperty("startTrigger").stringValue = trigger;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        protected T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
