using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class FrameAnimationLogicTests
    {
        [Test]
        public void FrameAt_AdvancesWithTime()
        {
            Assert.AreEqual(0, FrameAnimationLogic.FrameAt(0f, 10f, 5, true));
            Assert.AreEqual(0, FrameAnimationLogic.FrameAt(0.09f, 10f, 5, true));
            Assert.AreEqual(1, FrameAnimationLogic.FrameAt(0.1f, 10f, 5, true));
            Assert.AreEqual(4, FrameAnimationLogic.FrameAt(0.45f, 10f, 5, true));
        }

        [Test]
        public void FrameAt_Loops_WrapsToStart()
        {
            Assert.AreEqual(0, FrameAnimationLogic.FrameAt(0.5f, 10f, 5, true));
            Assert.AreEqual(2, FrameAnimationLogic.FrameAt(0.7f, 10f, 5, true));
        }

        [Test]
        public void FrameAt_NoLoop_HoldsLastFrame()
        {
            Assert.AreEqual(4, FrameAnimationLogic.FrameAt(0.5f, 10f, 5, false));
            Assert.AreEqual(4, FrameAnimationLogic.FrameAt(99f, 10f, 5, false));
        }

        [Test]
        public void FrameAt_InvalidInput_ReturnsZero()
        {
            Assert.AreEqual(0, FrameAnimationLogic.FrameAt(1f, 10f, 0, true));
            Assert.AreEqual(0, FrameAnimationLogic.FrameAt(1f, 0f, 5, true));
            Assert.AreEqual(0, FrameAnimationLogic.FrameAt(-1f, 10f, 5, true));
        }
    }
}
