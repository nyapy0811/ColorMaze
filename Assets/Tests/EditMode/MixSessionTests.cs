using NUnit.Framework;

namespace ColorMaze
{
    public class MixSessionTests
    {
        static OrbInventory Inventory(params OrbValue[] values)
        {
            var inventory = new OrbInventory(5, new OrbValue(5, 5, 5));
            foreach (var value in values) inventory.TryAdd(value, out _);
            return inventory;
        }

        [Test]
        public void Toggle_SelectsAndCancels()
        {
            var inventory = Inventory(new OrbValue(1, 0, 0));
            var session = new MixSession(inventory);
            var orb = inventory.Items[0];
            session.Toggle(orb);
            Assert.IsTrue(session.IsSelected(orb));
            session.Toggle(orb);
            Assert.IsFalse(session.IsSelected(orb));
            Assert.IsFalse(session.CanMix);
        }

        [Test]
        public void Orbs_ListsInventoryInArrivalOrder()
        {
            var inventory = Inventory(new OrbValue(1, 0, 0), new OrbValue(0, 2, 0));
            var session = new MixSession(inventory);
            Assert.AreEqual(2, session.Orbs.Count);
            Assert.AreSame(inventory.Items[1], session.Orbs[1]);
        }

        [Test]
        public void Preview_SumOverLimitIsClamped()
        {
            var inventory = Inventory(new OrbValue(4, 1, 0), new OrbValue(3, 0, 2));
            var session = new MixSession(inventory);
            session.Toggle(inventory.Items[0]);
            session.Toggle(inventory.Items[1]);
            Assert.AreEqual(new OrbValue(5, 1, 2), session.Preview);
        }

        [Test]
        public void Mix_RemovesMaterialsAndAddsResultAtEnd()
        {
            var inventory = Inventory(new OrbValue(1, 0, 0), new OrbValue(0, 0, 1), new OrbValue(0, 1, 0));
            var session = new MixSession(inventory);
            session.Toggle(inventory.Items[0]);
            session.Toggle(inventory.Items[2]);

            var result = session.Mix();

            Assert.AreEqual(new OrbValue(1, 1, 0), result.Value);
            CollectionAssert.AreEqual(new[] { new OrbValue(0, 0, 1), new OrbValue(1, 1, 0) }, inventory.ExportValues());
            Assert.AreEqual(0, session.Selected.Count);
        }

        [Test]
        public void Mix_SingleOrbIsAllowed()
        {
            var inventory = Inventory(new OrbValue(2, 0, 0), new OrbValue(0, 1, 0));
            var session = new MixSession(inventory);
            session.Toggle(inventory.Items[0]);

            var result = session.Mix();

            Assert.AreEqual(new OrbValue(2, 0, 0), result.Value);
            CollectionAssert.AreEqual(new[] { new OrbValue(0, 1, 0), new OrbValue(2, 0, 0) }, inventory.ExportValues());
        }

        [Test]
        public void Mix_ZeroOrbChangesNothingAndVanishes()
        {
            var inventory = Inventory(new OrbValue(2, 1, 0), OrbValue.Zero);
            var session = new MixSession(inventory);
            session.Toggle(inventory.Items[0]);
            session.Toggle(inventory.Items[1]);

            Assert.AreEqual(new OrbValue(2, 1, 0), session.Mix().Value);
            Assert.AreEqual(1, inventory.Count);
        }

        [Test]
        public void Mix_OnlyZeroOrbsLeavesNoResult()
        {
            var inventory = Inventory(OrbValue.Zero);
            var session = new MixSession(inventory);
            session.Toggle(inventory.Items[0]);

            Assert.IsNull(session.Mix());
            Assert.AreEqual(0, inventory.Count);
        }

        [Test]
        public void Close_ClearsSelectionWithoutChangingInventory()
        {
            var inventory = Inventory(new OrbValue(1, 0, 0), new OrbValue(0, 1, 0));
            var session = new MixSession(inventory);
            session.Toggle(inventory.Items[0]);
            session.Close();
            Assert.AreEqual(0, session.Selected.Count);
            Assert.AreEqual(2, inventory.Count);
        }
    }
}
