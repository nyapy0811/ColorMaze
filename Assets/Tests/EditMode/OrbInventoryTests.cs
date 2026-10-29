using System.Linq;
using NUnit.Framework;

namespace ColorMaze
{
    public class OrbInventoryTests
    {
        static readonly OrbValue Limit = new(5, 5, 5);

        [Test]
        public void TryAdd_KeepsArrivalOrder()
        {
            var inventory = new OrbInventory(5, Limit);
            inventory.TryAdd(new OrbValue(1, 0, 0), out _);
            inventory.TryAdd(new OrbValue(0, 1, 0), out _);
            CollectionAssert.AreEqual(new[] { new OrbValue(1, 0, 0), new OrbValue(0, 1, 0) }, inventory.ExportValues());
        }

        [Test]
        public void TryAdd_FailsWhenFull()
        {
            var inventory = new OrbInventory(2, Limit);
            Assert.IsTrue(inventory.TryAdd(new OrbValue(1, 0, 0), out _));
            Assert.IsTrue(inventory.TryAdd(new OrbValue(1, 0, 0), out _));
            Assert.IsTrue(inventory.IsFull);
            Assert.IsFalse(inventory.TryAdd(new OrbValue(1, 0, 0), out var added));
            Assert.IsNull(added);
            Assert.AreEqual(2, inventory.Count);
        }

        [Test]
        public void SameValues_AreDifferentHeldOrbs()
        {
            var inventory = new OrbInventory(5, Limit);
            inventory.TryAdd(new OrbValue(1, 0, 0), out var first);
            inventory.TryAdd(new OrbValue(1, 0, 0), out var second);
            Assert.AreNotEqual(first.Id, second.Id);
        }

        [Test]
        public void Remove_ShiftsLaterOrbsForward()
        {
            var inventory = new OrbInventory(5, Limit);
            inventory.TryAdd(new OrbValue(1, 0, 0), out _);
            inventory.TryAdd(new OrbValue(0, 1, 0), out var middle);
            inventory.TryAdd(new OrbValue(0, 0, 1), out _);
            Assert.IsTrue(inventory.Remove(middle));
            CollectionAssert.AreEqual(new[] { new OrbValue(1, 0, 0), new OrbValue(0, 0, 1) }, inventory.ExportValues());
        }

        [Test]
        public void Changed_IsRaisedOnAddAndRemove()
        {
            var inventory = new OrbInventory(5, Limit);
            int raised = 0;
            inventory.Changed += () => raised++;
            inventory.TryAdd(new OrbValue(1, 0, 0), out var orb);
            inventory.Remove(orb);
            Assert.AreEqual(2, raised);
        }

        [Test]
        public void ExportThenImport_RestoresValuesAndLimit()
        {
            var source = new OrbInventory(5, new OrbValue(8, 7, 9));
            source.TryAdd(new OrbValue(2, 0, 0), out _);
            source.TryAdd(new OrbValue(0, 3, 1), out _);

            var restored = new OrbInventory(5, Limit);
            restored.Import(source.ExportValues(), source.ChannelLimit);

            CollectionAssert.AreEqual(source.ExportValues(), restored.ExportValues());
            Assert.AreEqual(new OrbValue(8, 7, 9), restored.ChannelLimit);
            Assert.AreEqual(2, restored.Items.Select(orb => orb.Id).Distinct().Count());
        }
    }
}
