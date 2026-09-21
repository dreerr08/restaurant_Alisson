using AliGame.Data;
using AliGame.Items;
using NUnit.Framework;

namespace AliGame.Tests
{
    public class CraftingTests : TestBase
    {
        [Test]
        public void CanCraft_IsFalseAtTheWrongStation()
        {
            var inventory = new InventoryModel(4);
            var flour = Item("Flour");
            var dough = Item("Dough");
            var recipe = Recipe(StationType.MixStation, dough, 1, (flour, 2));
            inventory.Add(flour, 5);

            Assert.IsTrue(inventory.CanCraft(recipe, StationType.MixStation));
            Assert.IsFalse(inventory.CanCraft(recipe, StationType.Stove));
            Assert.IsFalse(inventory.Craft(recipe, StationType.CutStation));
            Assert.AreEqual(5, inventory.Count(flour), "a refused craft must not consume anything");
        }

        [Test]
        public void Craft_ConsumesIngredientsButDoesNotAddTheResult()
        {
            var inventory = new InventoryModel(4);
            var flour = Item("Flour");
            var dough = Item("Dough");
            var recipe = Recipe(StationType.MixStation, dough, 1, (flour, 2));
            inventory.Add(flour, 5);

            Assert.IsTrue(inventory.Craft(recipe, StationType.MixStation));

            Assert.AreEqual(3, inventory.Count(flour));
            Assert.AreEqual(0, inventory.Count(dough), "the result is spawned in the world by the station, not added here");
        }

        [Test]
        public void Craft_RaisesCraftedWithTheRecipe()
        {
            var inventory = new InventoryModel(4);
            var flour = Item("Flour");
            var recipe = Recipe(StationType.MixStation, Item("Dough"), 1, (flour, 1));
            inventory.Add(flour, 1);
            RecipeSO crafted = null;
            inventory.Crafted += r => crafted = r;

            inventory.Craft(recipe, StationType.MixStation);

            Assert.AreSame(recipe, crafted);
        }

        [Test]
        public void Craft_FailsWithoutEnoughIngredients()
        {
            var inventory = new InventoryModel(4);
            var flour = Item("Flour");
            var cheese = Item("Cheese");
            var recipe = Recipe(StationType.Stove, Item("Pizza"), 1, (flour, 2), (cheese, 1));
            inventory.Add(flour, 2);

            Assert.IsFalse(inventory.CanCraft(recipe, StationType.Stove));
            Assert.IsFalse(inventory.Craft(recipe, StationType.Stove));
            Assert.AreEqual(2, inventory.Count(flour));
        }

        [Test]
        public void Craft_SumsTheSameIngredientListedTwice()
        {
            var inventory = new InventoryModel(4);
            var tomato = Item("Tomato");
            var recipe = Recipe(StationType.Stove, Item("Sauce"), 1, (tomato, 2), (tomato, 1));
            inventory.Add(tomato, 2);

            Assert.IsFalse(inventory.CanCraft(recipe, StationType.Stove), "needs 3 in total");

            inventory.Add(tomato, 1);
            Assert.IsTrue(inventory.Craft(recipe, StationType.Stove));
            Assert.AreEqual(0, inventory.Count(tomato));
        }

        [Test]
        public void CanCraft_IsFalseForIncompleteRecipes()
        {
            var inventory = new InventoryModel(4);
            var flour = Item("Flour");
            inventory.Add(flour, 5);

            var noResult = Recipe(StationType.Stove, null, 1, (flour, 1));
            var noIngredients = Recipe(StationType.Stove, Item("Dough"), 1);
            var emptyIngredient = Recipe(StationType.Stove, Item("Dough2"), 1, (null, 1));

            Assert.IsFalse(inventory.CanCraft(noResult, StationType.Stove));
            Assert.IsFalse(inventory.CanCraft(noIngredients, StationType.Stove));
            Assert.IsFalse(inventory.CanCraft(emptyIngredient, StationType.Stove));
            Assert.IsFalse(inventory.CanCraft(null, StationType.Stove));
        }
    }
}
