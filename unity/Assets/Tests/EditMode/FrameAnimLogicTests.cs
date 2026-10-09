using NUnit.Framework;
using SushiSurvival.Companions;

namespace SushiSurvival.EditModeTests
{
    public class FrameAnimLogicTests
    {
        [Test]
        public void TimeZero_IsFirstFrame()
        {
            Assert.AreEqual(0, FrameAnimLogic.FrameIndex(0f, 8f, 5));
        }

        [TestCase(0.00f, 0)]
        [TestCase(0.12f, 0)]
        [TestCase(0.13f, 1)]
        [TestCase(0.26f, 2)]
        [TestCase(0.51f, 4)]
        public void AdvancesByFramesPerSecond(float time, int expected)
        {
            // 8fps → 프레임 하나가 0.125초
            Assert.AreEqual(expected, FrameAnimLogic.FrameIndex(time, 8f, 5));
        }

        [Test]
        public void Loops_BackToFirstFrame()
        {
            // 8fps, 5프레임 → 0.625초에 한 바퀴
            Assert.AreEqual(0, FrameAnimLogic.FrameIndex(0.63f, 8f, 5));
            Assert.AreEqual(1, FrameAnimLogic.FrameIndex(0.76f, 8f, 5));
        }

        [Test]
        public void InvalidInputs_AreSafe()
        {
            Assert.AreEqual(0, FrameAnimLogic.FrameIndex(1f, 8f, 0));
            Assert.AreEqual(0, FrameAnimLogic.FrameIndex(1f, 0f, 5));
            Assert.AreEqual(0, FrameAnimLogic.FrameIndex(-3f, 8f, 5));
            Assert.AreEqual(0, FrameAnimLogic.FrameIndex(1f, -8f, 5));
        }

        [Test]
        public void SingleFrame_AlwaysZero()
        {
            Assert.AreEqual(0, FrameAnimLogic.FrameIndex(12.3f, 8f, 1));
        }
    }
}
