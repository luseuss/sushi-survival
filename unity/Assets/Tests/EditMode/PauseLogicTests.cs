using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class PauseLogicTests
    {
        [Test]
        public void CanPause_WhenPlayingAtNormalSpeed()
        {
            Assert.IsTrue(PauseLogic.CanPause(true, 1f));
        }

        [Test]
        public void CanPause_FalseWhenNotPlaying()
        {
            Assert.IsFalse(PauseLogic.CanPause(false, 1f));
        }

        [Test]
        public void CanPause_FalseWhilePopupOrHitstopHasStoppedTime()
        {
            Assert.IsFalse(PauseLogic.CanPause(true, 0f));
        }

        [Test]
        public void CanPause_FalseDuringSlowMotion()
        {
            Assert.IsFalse(PauseLogic.CanPause(true, 0.3f));
        }
    }
}
