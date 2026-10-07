using NUnit.Framework;
using SushiSurvival.Enemies;

namespace SushiSurvival.EditModeTests
{
    public class MidBossThrowLogicTests
    {
        [TestCase(2.9f, false)]
        [TestCase(3f, true)]
        [TestCase(6f, true)]
        [TestCase(9f, true)]
        [TestCase(9.1f, false)]
        public void InThrowRange_IncludesBothBounds(float distance, bool expected)
        {
            Assert.AreEqual(expected, MidBossThrowLogic.InThrowRange(distance, 3f, 9f));
        }

        [Test]
        public void ArcHeight_StartsAndEndsOnTheGround()
        {
            Assert.AreEqual(0f, MidBossThrowLogic.ArcHeight(0f, 1.5f), 0.0001f);
            Assert.AreEqual(0f, MidBossThrowLogic.ArcHeight(1f, 1.5f), 0.0001f);
        }

        [Test]
        public void ArcHeight_PeaksAtMidpoint()
        {
            Assert.AreEqual(1.5f, MidBossThrowLogic.ArcHeight(0.5f, 1.5f), 0.0001f);
        }

        [Test]
        public void ArcHeight_IsSymmetric()
        {
            Assert.AreEqual(MidBossThrowLogic.ArcHeight(0.25f, 2f), MidBossThrowLogic.ArcHeight(0.75f, 2f), 0.0001f);
        }

        [Test]
        public void ArcHeight_ClampsProgressOutsideZeroToOne()
        {
            Assert.AreEqual(0f, MidBossThrowLogic.ArcHeight(-0.5f, 2f), 0.0001f);
            Assert.AreEqual(0f, MidBossThrowLogic.ArcHeight(1.5f, 2f), 0.0001f);
        }
    }
}
