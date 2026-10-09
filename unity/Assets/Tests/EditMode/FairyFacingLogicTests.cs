using NUnit.Framework;
using SushiSurvival.Companions;

namespace SushiSurvival.EditModeTests
{
    public class FairyFacingLogicTests
    {
        [Test]
        public void ArtFacesLeft_MovingRight_Flips()
        {
            Assert.IsTrue(FairyFacingLogic.FlipX(1f, false, false));
        }

        [Test]
        public void ArtFacesLeft_MovingLeft_DoesNotFlip()
        {
            Assert.IsFalse(FairyFacingLogic.FlipX(-1f, true, false));
        }

        [Test]
        public void ArtFacesRight_MovingRight_DoesNotFlip()
        {
            Assert.IsFalse(FairyFacingLogic.FlipX(1f, true, true));
        }

        [Test]
        public void ArtFacesRight_MovingLeft_Flips()
        {
            Assert.IsTrue(FairyFacingLogic.FlipX(-1f, false, true));
        }

        [Test]
        public void InsideDeadZone_KeepsPreviousFlip()
        {
            Assert.IsTrue(FairyFacingLogic.FlipX(0.01f, true, false));
            Assert.IsFalse(FairyFacingLogic.FlipX(-0.01f, false, false));
            Assert.IsTrue(FairyFacingLogic.FlipX(0f, true, true));
        }

        [Test]
        public void CustomDeadZone_IsRespected()
        {
            Assert.IsFalse(FairyFacingLogic.FlipX(0.5f, false, false, 1f));
            Assert.IsTrue(FairyFacingLogic.FlipX(1.5f, false, false, 1f));
        }
    }
}
