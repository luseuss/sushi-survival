using NUnit.Framework;
using SushiSurvival.Pickups;

namespace SushiSurvival.EditModeTests
{
    public class XPVacuumLogicTests
    {
        [TestCase(0f, 0f, false)]
        [TestCase(0f, 0.5f, false)]
        [TestCase(0.01f, 0.005f, true)]
        [TestCase(0.01f, 0.01f, false)]
        [TestCase(0.01f, 0.5f, false)]
        [TestCase(1f, 0.999f, true)]
        public void ShouldDrop_RollMustBeBelowChance(float chance, float roll, bool expected)
        {
            Assert.AreEqual(expected, XPVacuumLogic.ShouldDrop(chance, roll));
        }

        [Test]
        public void ShouldDrop_ZeroChanceNeverDrops_EvenWithZeroRoll()
        {
            Assert.IsFalse(XPVacuumLogic.ShouldDrop(0f, 0f));
        }

        [TestCase(0, 3, true)]
        [TestCase(2, 3, true)]
        [TestCase(3, 3, false)]
        [TestCase(5, 3, false)]
        [TestCase(10, 0, true)]
        public void CanSpawn_BelowLimit(int active, int max, bool expected)
        {
            Assert.AreEqual(expected, XPVacuumLogic.CanSpawn(active, max));
        }

        [Test]
        public void AttractSpeed_StartsSlowAndSpeedsUpToMax()
        {
            Assert.AreEqual(8f, XPVacuumLogic.AttractSpeed(0f, 8f, 30f, 25f), 0.0001f);
            Assert.AreEqual(17f, XPVacuumLogic.AttractSpeed(0.3f, 8f, 30f, 25f), 0.0001f);
            Assert.AreEqual(25f, XPVacuumLogic.AttractSpeed(5f, 8f, 30f, 25f), 0.0001f);
        }

        [Test]
        public void AttractSpeed_NegativeElapsedIsTreatedAsZero()
        {
            Assert.AreEqual(8f, XPVacuumLogic.AttractSpeed(-1f, 8f, 30f, 25f), 0.0001f);
        }
    }
}
