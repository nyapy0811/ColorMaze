using NUnit.Framework;
using UnityEngine;

namespace ColorMaze
{
    public class OrbRulesTests
    {
        static readonly OrbValue Limit = new(5, 5, 5);

        [Test]
        public void IsWithin_ChecksEachChannelAgainstItsLimit()
        {
            Assert.IsTrue(OrbRules.IsWithin(new OrbValue(5, 0, 3), Limit));
            Assert.IsFalse(OrbRules.IsWithin(new OrbValue(6, 0, 0), Limit));
            Assert.IsFalse(OrbRules.IsWithin(new OrbValue(-1, 0, 0), Limit));
            Assert.IsTrue(OrbRules.IsWithin(new OrbValue(8, 7, 9), new OrbValue(8, 7, 9)));
        }

        [Test]
        public void Clamp_NegativeBecomesZeroAndOverflowBecomesLimit()
        {
            Assert.AreEqual(new OrbValue(0, 5, 3), OrbRules.Clamp(new OrbValue(-2, 9, 3), Limit));
            Assert.AreEqual(new OrbValue(8, 7, 9), OrbRules.Clamp(new OrbValue(10, 10, 10), new OrbValue(8, 7, 9)));
        }

        [Test]
        public void IsValidGoal_NeedsEveryChannelAtLeastOneAndNotAllEqual()
        {
            Assert.IsTrue(OrbRules.IsValidGoal(new OrbValue(1, 2, 3)));
            Assert.IsFalse(OrbRules.IsValidGoal(new OrbValue(2, 0, 0)));
            Assert.IsFalse(OrbRules.IsValidGoal(new OrbValue(2, 2, 2)));
            Assert.IsTrue(OrbRules.IsValidGoal(new OrbValue(8, 7, 9)), "기본 한도를 넘어도 된다");
        }

        [Test]
        public void IsValidSourceValue_RejectsZeroAndAllEqual()
        {
            Assert.IsTrue(OrbRules.IsValidSourceValue(new OrbValue(2, 0, 0)));
            Assert.IsFalse(OrbRules.IsValidSourceValue(OrbValue.Zero));
            Assert.IsFalse(OrbRules.IsValidSourceValue(new OrbValue(1, 1, 1)));
        }

        [Test]
        public void ToColor_IsNormalizedSoScaledValuesLookTheSame()
        {
            Assert.AreEqual(OrbRules.ToColor(new OrbValue(1, 0, 0)), OrbRules.ToColor(new OrbValue(2, 0, 0)));
            Assert.AreEqual(new Color(1f, 0.5f, 0f), OrbRules.ToColor(new OrbValue(4, 2, 0)));
        }

        [Test]
        public void GlowIntensity_IsProportionalToLargestChannel()
        {
            Assert.AreEqual(1.5f, OrbRules.GlowIntensity(new OrbValue(1, 3, 2), 0.5f));
        }
    }
}
