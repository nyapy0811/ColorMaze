using NUnit.Framework;

namespace ColorMaze
{
    public class ActuatorStateTests
    {
        static readonly OrbValue Orb = new(2, 1, 0);

        [Test]
        public void SingleUse_IsUsedUpOnceOnAndCannotReopen()
        {
            var state = new ActuatorState(SubmitKind.SingleUse);
            Assert.IsTrue(state.CanOpenPopup);

            state = state.Apply(new SubmitOutcome(Orb), out bool changed);

            Assert.IsTrue(changed);
            Assert.IsTrue(state.IsOn);
            Assert.IsTrue(state.IsUsedUp);
            Assert.IsFalse(state.CanOpenPopup);
        }

        [Test]
        public void Reusable_TurnsOnAndOffAndCanAlwaysReopen()
        {
            var state = new ActuatorState(SubmitKind.Reusable);

            state = state.Apply(new SubmitOutcome(Orb), out bool turnedOn);
            Assert.IsTrue(turnedOn);
            Assert.IsTrue(state.IsOn);
            Assert.IsFalse(state.IsUsedUp);
            Assert.IsTrue(state.CanOpenPopup);

            state = state.Apply(new SubmitOutcome(null), out bool turnedOff);
            Assert.IsTrue(turnedOff);
            Assert.IsFalse(state.IsOn);
        }

        [Test]
        public void Goal_CannotReopenAfterClear()
        {
            var state = new ActuatorState(SubmitKind.Goal);
            state = state.Apply(new SubmitOutcome(Orb), out _);
            Assert.IsFalse(state.CanOpenPopup);
        }

        [Test]
        public void ClosingWithoutChange_DoesNotReportChange()
        {
            var state = new ActuatorState(SubmitKind.Reusable, Orb);
            state = state.Apply(new SubmitOutcome(Orb), out bool changed);
            Assert.IsFalse(changed);
            Assert.IsTrue(state.IsOn);
        }
    }
}
