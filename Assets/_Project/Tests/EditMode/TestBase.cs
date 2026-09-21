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

        protected T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
