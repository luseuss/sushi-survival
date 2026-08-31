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

        [Test]
        public void MoveTowardsFill_MovesPartway_TowardLowerTarget()
        {
            Assert.That(HealthBarLogic.MoveTowardsFill(1f, 0f, 0.3f), Is.EqualTo(0.7f).Within(0.0001f));
        }

        [Test]
        public void MoveTowardsFill_MovesPartway_TowardHigherTarget()
        {
            Assert.That(HealthBarLogic.MoveTowardsFill(0f, 1f, 0.3f), Is.EqualTo(0.3f).Within(0.0001f));
        }

        [Test]
        public void MoveTowardsFill_DoesNotOvershootTarget()
        {
            // maxDelta가 남은 거리보다 크면 목표에서 멈춰야지 지나치면 안 된다.
            Assert.That(HealthBarLogic.MoveTowardsFill(0.1f, 0f, 0.5f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void MoveTowardsFill_StaysAtTarget_WhenAlreadyThere()
        {
            Assert.That(HealthBarLogic.MoveTowardsFill(0.5f, 0.5f, 0.3f), Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void MoveTowardsFill_ClampsInputsToZeroOneRange()
        {
            Assert.That(HealthBarLogic.MoveTowardsFill(1.5f, 0f, 100f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void MoveTowardsFill_TreatsNegativeMaxDeltaAsZero()
        {
            Assert.That(HealthBarLogic.MoveTowardsFill(0.5f, 0f, -1f), Is.EqualTo(0.5f).Within(0.0001f));
        }
    }
}
