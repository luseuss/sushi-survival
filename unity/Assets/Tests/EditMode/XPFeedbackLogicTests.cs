using NUnit.Framework;
using SushiSurvival.Pickups;

namespace SushiSurvival.EditModeTests
{
    public class XPFeedbackLogicTests
    {
        private const float Eps = 0.0001f;

        [Test]
        public void ShrinkScale_FullAtStart_MinWhenArrived()
        {
            Assert.AreEqual(1f, XPFeedbackLogic.ShrinkScale(2f, 2f, 0.5f), Eps);
            Assert.AreEqual(0.5f, XPFeedbackLogic.ShrinkScale(0f, 2f, 0.5f), Eps);
            Assert.AreEqual(0.75f, XPFeedbackLogic.ShrinkScale(1f, 2f, 0.5f), Eps);
        }

        [Test]
        public void ShrinkScale_ClampsAndHandlesZeroStart()
        {
            Assert.AreEqual(1f, XPFeedbackLogic.ShrinkScale(5f, 2f, 0.5f), Eps);
            Assert.AreEqual(1f, XPFeedbackLogic.ShrinkScale(1f, 0f, 0.5f), Eps);
        }

        [Test]
        public void ShouldCollect_WhenCloseOrFlyingTooLong()
        {
            Assert.IsFalse(XPFeedbackLogic.ShouldCollect(1f, 0.1f, 0.15f, 0.5f));
            Assert.IsTrue(XPFeedbackLogic.ShouldCollect(0.1f, 0.1f, 0.15f, 0.5f));
            Assert.IsTrue(XPFeedbackLogic.ShouldCollect(1f, 0.5f, 0.15f, 0.5f));
        }

        [Test]
        public void GaugePulse_LevelUpIsStrongest_GainIsMedium_NoChangeIsZero()
        {
            Assert.AreEqual(XPFeedbackLogic.LevelUpPulse, XPFeedbackLogic.GaugePulse(2, 0.9f, 3, 0.1f), Eps);
            Assert.AreEqual(XPFeedbackLogic.GainPulse, XPFeedbackLogic.GaugePulse(2, 0.3f, 2, 0.4f), Eps);
            Assert.AreEqual(0f, XPFeedbackLogic.GaugePulse(2, 0.4f, 2, 0.4f), Eps);
        }

        [Test]
        public void GaugePulse_RatioDroppingWithoutLevelChange_DoesNotPulse()
        {
            Assert.AreEqual(0f, XPFeedbackLogic.GaugePulse(2, 0.5f, 2, 0.2f), Eps);
        }

        [Test]
        public void PulseDecay_FadesToZeroOverDuration()
        {
            Assert.AreEqual(0.5f, XPFeedbackLogic.PulseDecay(1f, 0.1f, 0.2f), Eps);
            Assert.AreEqual(0f, XPFeedbackLogic.PulseDecay(0.3f, 0.5f, 0.2f), Eps);
            Assert.AreEqual(0f, XPFeedbackLogic.PulseDecay(1f, 0.1f, 0f), Eps);
        }
    }
}
