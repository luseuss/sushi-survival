using NUnit.Framework;
using SushiSurvival.Enemies;

namespace SushiSurvival.EditModeTests
{
    public class EnemyFlipLogicTests
    {
        [Test]
        public void ShouldFlip_ArtFacesRight_MovingLeft_Flips()
        {
            Assert.IsTrue(EnemyFlipLogic.ShouldFlip(false, -0.8f, 0.15f, true));
        }

        [Test]
        public void ShouldFlip_ArtFacesRight_MovingRight_DoesNotFlip()
        {
            Assert.IsFalse(EnemyFlipLogic.ShouldFlip(true, 0.8f, 0.15f, true));
        }

        [Test]
        public void ShouldFlip_ArtFacesLeft_IsOpposite()
        {
            Assert.IsFalse(EnemyFlipLogic.ShouldFlip(true, -0.8f, 0.15f, false));
            Assert.IsTrue(EnemyFlipLogic.ShouldFlip(false, 0.8f, 0.15f, false));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ShouldFlip_NearlyVertical_KeepsCurrentState(bool current)
        {
            Assert.AreEqual(current, EnemyFlipLogic.ShouldFlip(current, 0.05f, 0.15f, true));
            Assert.AreEqual(current, EnemyFlipLogic.ShouldFlip(current, -0.05f, 0.15f, true));
        }

        [Test]
        public void ShouldFlip_ExactlyAtDeadZoneEdge_Decides()
        {
            Assert.IsTrue(EnemyFlipLogic.ShouldFlip(false, -0.15f, 0.15f, true));
        }
    }
}
