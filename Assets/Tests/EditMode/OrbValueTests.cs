using NUnit.Framework;

namespace ColorMaze
{
    public class OrbValueTests
    {
        [Test]
        public void Add_SumsEachChannel()
        {
            Assert.AreEqual(new OrbValue(3, 2, 4), new OrbValue(1, 2, 0) + new OrbValue(2, 0, 4));
        }

        [Test]
        public void Equality_ComparesAllChannels()
        {
            Assert.IsTrue(new OrbValue(1, 2, 3) == new OrbValue(1, 2, 3));
            Assert.IsTrue(new OrbValue(1, 2, 3) != new OrbValue(1, 2, 4));
        }

        [Test]
        public void Max_IsLargestChannel()
        {
            Assert.AreEqual(4, new OrbValue(1, 4, 2).Max);
        }

        [Test]
        public void ToString_IsThreeNumbers()
        {
            Assert.AreEqual("2 0 0", new OrbValue(2, 0, 0).ToString());
        }
    }
}
