using AliGame.Items;
using NUnit.Framework;

namespace AliGame.Tests
{
    public class InventoryModelTests : TestBase
    {
        [Test]
        public void Add_StacksUpToMaxStack()
        {
            var inventory = new InventoryModel(4);
            var tomato = Item("Tomato", maxStack: 99);

            Assert.AreEqual(5, inventory.Add(tomato, 5));
            Assert.AreEqual(94, inventory.Add(tomato, 200));
            Assert.AreEqual(99, inventory.Count(tomato));
            Assert.AreEqual(0, inventory.Add(tomato, 1));
        }

        [Test]
        public void Add_ReturnsZeroWhenAllSlotsAreTaken()
        {
            var inventory = new InventoryModel(2);
            inventory.Add(Item("A"), 1);
            inventory.Add(Item("B"), 1);
            var third = Item("C");

            Assert.AreEqual(0, inventory.Add(third, 1));
            Assert.AreEqual(0, inventory.Count(third));
            Assert.AreEqual(2, inventory.Stacks.Count);
        }

        [Test]
        public void Add_IgnoresNullAndNonPositiveAmounts()
        {
            var inventory = new InventoryModel(2);
            var item = Item("A");

            Assert.AreEqual(0, inventory.Add(null, 1));
            Assert.AreEqual(0, inventory.Add(item, 0));
            Assert.AreEqual(0, inventory.Add(item, -3));
            Assert.AreEqual(0, inventory.Stacks.Count);
        }

        [Test]
        public void Remove_FailsWithoutEnoughAndChangesNothing()
        {
            var inventory = new InventoryModel(2);
            var item = Item("A");
            inventory.Add(item, 2);

            Assert.IsFalse(inventory.Remove(item, 3));
            Assert.AreEqual(2, inventory.Count(item));
        }

        [Test]
        public void Remove_EmptyingAStackFreesItsSlot()
        {
            var inventory = new InventoryModel(1);
            var first = Item("A");
            var second = Item("B");
            inventory.Add(first, 2);

            Assert.IsTrue(inventory.Remove(first, 2));
            Assert.AreEqual(0, inventory.Stacks.Count);
            Assert.AreEqual(1, inventory.Add(second, 1));
        }

        [Test]
        public void CanAdd_ReflectsRoomInStackAndFreeSlots()
        {
            var inventory = new InventoryModel(1);
            var small = Item("Small", maxStack: 2);
            var other = Item("Other");

            Assert.IsTrue(inventory.CanAdd(small));
            inventory.Add(small, 1);
            Assert.IsTrue(inventory.CanAdd(small));
            inventory.Add(small, 1);
            Assert.IsFalse(inventory.CanAdd(small), "stack is full");
            Assert.IsFalse(inventory.CanAdd(other), "no free slot");
            Assert.IsFalse(inventory.CanAdd(null));
        }

        [Test]
        public void Events_AreRaisedWithTheAmountThatFit()
        {
            var inventory = new InventoryModel(2);
            var item = Item("A", maxStack: 3);
            int changed = 0;
            int addedAmount = 0;
            inventory.Changed += () => changed++;
            inventory.ItemAdded += (added, amount) => addedAmount += amount;

            inventory.Add(item, 5);

            Assert.AreEqual(1, changed);
            Assert.AreEqual(3, addedAmount);
        }
    }
}
