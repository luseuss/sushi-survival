using NUnit.Framework;
using SushiSurvival.Enemies;

namespace SushiSurvival.EditModeTests
{
    public class MidBossThrowLogicTests
    {
        [TestCase(200f, false)]
        [TestCase(61f, false)]
        [TestCase(60f, true)]
        [TestCase(10f, true)]
        public void HealthBelowThreshold_TriggersAtOrBelowThreshold(float current, bool expected)
        {
            Assert.AreEqual(expected, MidBossThrowLogic.HealthBelowThreshold(current, 200f, 0.3f));
        }

        [Test]
        public void HealthBelowThreshold_ZeroMaxHealthNeverTriggers()
        {
            Assert.IsFalse(MidBossThrowLogic.HealthBelowThreshold(0f, 0f, 0.3f));
        }
    }
}
