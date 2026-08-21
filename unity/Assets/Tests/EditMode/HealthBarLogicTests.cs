using NUnit.Framework;
using SushiSurvival.UI;

namespace SushiSurvival.EditModeTests
{
    public class HealthBarLogicTests
    {
        [Test]
        public void ComputeFillAmount_FullAtMaxHealth()
        {
            Assert.That(HealthBarLogic.ComputeFillAmount(100f, 100f), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void ComputeFillAmount_HalfAtHalfHealth()
        {
            Assert.That(HealthBarLogic.ComputeFillAmount(50f, 100f), Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void ComputeFillAmount_ZeroWhenDead()
        {
            Assert.That(HealthBarLogic.ComputeFillAmount(0f, 100f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ComputeFillAmount_ClampsNegativeHealthToZero()
        {
            Assert.That(HealthBarLogic.ComputeFillAmount(-20f, 100f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ComputeFillAmount_ClampsOverfillToOne()
        {
            Assert.That(HealthBarLogic.ComputeFillAmount(150f, 100f), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void ComputeFillAmount_ReturnsZero_WhenMaxIsZero()
        {
            // 0으로 나누어 NaN이 되는 것을 막는다.
            Assert.That(HealthBarLogic.ComputeFillAmount(10f, 0f), Is.EqualTo(0f).Within(0.0001f));
        }
    }
}
