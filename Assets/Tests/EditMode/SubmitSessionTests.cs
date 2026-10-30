using NUnit.Framework;

namespace ColorMaze
{
    public class SubmitSessionTests
    {
        static readonly OrbValue Goal = new(2, 1, 0);

        static OrbInventory Inventory(int capacity, params OrbValue[] values)
        {
            var inventory = new OrbInventory(capacity, new OrbValue(5, 5, 5));
            foreach (var value in values) inventory.TryAdd(value, out _);
            return inventory;
        }

        static SubmitSession Session(OrbInventory inventory, SubmitKind kind, OrbValue? stored = null, bool canRelease = true) =>
            new(inventory, Goal, value => value == Goal, () => canRelease, kind, stored);

        [Test]
        public void Orbs_ListsInventoryInArrivalOrder()
        {
            var inventory = Inventory(5, new OrbValue(1, 0, 0), Goal);
            var session = Session(inventory, SubmitKind.SingleUse);
            Assert.AreEqual(2, session.Orbs.Count);
            Assert.AreSame(inventory.Items[1], session.Orbs[1]);
            Assert.AreEqual(Goal, session.Required);
        }

        [Test]
        public void Submit_RightValueMovesOrbIntoSlot()
        {
            var inventory = Inventory(5, Goal);
            var session = Session(inventory, SubmitKind.SingleUse);

            Assert.IsTrue(session.TrySubmit(inventory.Items[0], out _));
            Assert.AreEqual(Goal, session.SlotOrb);
            Assert.AreEqual(0, inventory.Count);
        }

        [Test]
        public void Submit_WrongValueIsRejected()
        {
            var inventory = Inventory(5, new OrbValue(1, 1, 0));
            var session = Session(inventory, SubmitKind.SingleUse);

            Assert.IsFalse(session.TrySubmit(inventory.Items[0], out var reason));
            Assert.AreEqual(SubmitRejectReason.WrongValue, reason);
            Assert.IsNull(session.SlotOrb);
            Assert.AreEqual(1, inventory.Count);
        }

        [Test]
        public void Submit_WhenSlotIsFullIsRejected()
        {
            var inventory = Inventory(5, Goal, Goal);
            var session = Session(inventory, SubmitKind.Reusable);
            session.TrySubmit(inventory.Items[0], out _);

            Assert.IsFalse(session.TrySubmit(inventory.Items[0], out var reason));
            Assert.AreEqual(SubmitRejectReason.SlotFull, reason);
        }

        [TestCase(SubmitKind.SingleUse)]
        [TestCase(SubmitKind.Goal)]
        public void Retrieve_BeforeCloseReturnsOrbToEnd(SubmitKind kind)
        {
            var inventory = Inventory(5, Goal, new OrbValue(0, 1, 0));
            var session = Session(inventory, kind);
            session.TrySubmit(inventory.Items[0], out _);

            Assert.IsTrue(session.TryRetrieve(out _));
            Assert.IsNull(session.SlotOrb);
            CollectionAssert.AreEqual(new[] { new OrbValue(0, 1, 0), Goal }, inventory.ExportValues());
        }

        [Test]
        public void Retrieve_ReusableOrbStoredBeforeOpening()
        {
            var inventory = Inventory(5);
            var session = Session(inventory, SubmitKind.Reusable, Goal);

            Assert.IsTrue(session.CanRetrieve);
            Assert.IsTrue(session.TryRetrieve(out _));
            CollectionAssert.AreEqual(new[] { Goal }, inventory.ExportValues());
        }

        [Test]
        public void Retrieve_WhenInventoryIsFullIsRejected()
        {
            var inventory = Inventory(1, new OrbValue(0, 1, 0));
            var session = Session(inventory, SubmitKind.Reusable, Goal);

            Assert.IsFalse(session.TryRetrieve(out var reason));
            Assert.AreEqual(SubmitRejectReason.InventoryFull, reason);
            Assert.AreEqual(Goal, session.SlotOrb);
        }

        [Test]
        public void Retrieve_WhenReleaseIsBlockedIsRejected()
        {
            var inventory = Inventory(5);
            var session = Session(inventory, SubmitKind.Reusable, Goal, canRelease: false);

            Assert.IsFalse(session.TryRetrieve(out var reason));
            Assert.AreEqual(SubmitRejectReason.GateOccupied, reason);
            Assert.AreEqual(Goal, session.SlotOrb);
            Assert.AreEqual(0, inventory.Count);
        }

        [Test]
        public void Close_ReturnsSlotOrb()
        {
            var inventory = Inventory(5, Goal);
            var session = Session(inventory, SubmitKind.Goal);
            Assert.IsNull(session.Close().StoredOrb);

            session.TrySubmit(inventory.Items[0], out _);
            Assert.AreEqual(Goal, session.Close().StoredOrb);
        }
    }
}
