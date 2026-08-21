using NUnit.Framework;
using SushiSurvival.Enemies.Boss;

namespace SushiSurvival.EditModeTests
{
    public class BossPhaseLogicTests
    {
        private const float Threshold = 0.5f;

        [Test]
        public void GetPhase_ReturnsOne_AtFullHealth()
        {
            Assert.AreEqual(1, BossPhaseLogic.GetPhase(1800f, 1800f, Threshold));
        }

        [Test]
        public void GetPhase_ReturnsOne_JustAboveThreshold()
        {
            Assert.AreEqual(1, BossPhaseLogic.GetPhase(919f, 1800f, Threshold));
        }

        [Test]
        public void GetPhase_ReturnsOne_ExactlyAtThreshold()
        {
            // "50% 아래로 내려가면" 전환이므로 정확히 50%는 아직 1페이즈다.
            Assert.AreEqual(1, BossPhaseLogic.GetPhase(900f, 1800f, Threshold));
        }

        [Test]
        public void GetPhase_ReturnsTwo_JustBelowThreshold()
        {
            Assert.AreEqual(2, BossPhaseLogic.GetPhase(899f, 1800f, Threshold));
        }

        [Test]
        public void GetPhase_ReturnsTwo_AtZeroHealth()
        {
            Assert.AreEqual(2, BossPhaseLogic.GetPhase(0f, 1800f, Threshold));
        }

        [Test]
        public void GetPhase_ReturnsTwo_WhenMaxHealthIsZero()
        {
            // 0으로 나누지 않는다. 데이터가 비어 있으면 안전한 쪽(2페이즈)으로.
            Assert.AreEqual(2, BossPhaseLogic.GetPhase(0f, 0f, Threshold));
        }

        [Test]
        public void GetPhase_RespectsCustomThreshold()
        {
            Assert.AreEqual(1, BossPhaseLogic.GetPhase(800f, 1000f, 0.75f));
            Assert.AreEqual(2, BossPhaseLogic.GetPhase(700f, 1000f, 0.75f));
        }
    }
}
