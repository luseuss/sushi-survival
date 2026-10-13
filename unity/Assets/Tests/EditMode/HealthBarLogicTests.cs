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

        [TestCase(0f, 0.4f)]
        [TestCase(1f, 0.6f)]
        [TestCase(0.5f, 0.5f)]
        public void MapFill_MapsRatioIntoBarRange(float ratio, float expected)
        {
            Assert.That(HealthBarLogic.MapFill(ratio, 0.4f, 0.6f), Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void MapFill_ClampsRatio()
        {
            Assert.That(HealthBarLogic.MapFill(2f, 0.4f, 0.6f), Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(HealthBarLogic.MapFill(-1f, 0.4f, 0.6f), Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void TryFindOpaqueRangeX_FindsHorizontalSpanOfBar()
        {
            // 10x4 이미지의 가로 3~6번 칸(픽셀 4개)에만 불투명 픽셀이 있다.
            var pixels = new UnityEngine.Color32[10 * 4];
            for (int y = 1; y < 3; y++)
                for (int x = 3; x <= 6; x++)
                    pixels[y * 10 + x] = new UnityEngine.Color32(255, 0, 0, 255);

            bool found = HealthBarLogic.TryFindOpaqueRangeX(pixels, 10, 4, 16, out float start, out float end);

            Assert.IsTrue(found);
            Assert.That(start, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(end, Is.EqualTo(0.7f).Within(0.0001f));
        }

        [Test]
        public void TryFindOpaqueRangeX_IgnoresFaintPixelsBelowThreshold()
        {
            var pixels = new UnityEngine.Color32[10 * 2];
            pixels[0] = new UnityEngine.Color32(255, 255, 255, 5);
            pixels[15] = new UnityEngine.Color32(255, 0, 0, 255);

            bool found = HealthBarLogic.TryFindOpaqueRangeX(pixels, 10, 2, 16, out float start, out float end);

            Assert.IsTrue(found);
            Assert.That(start, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(end, Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void TryFindOpaqueRangeX_FullyTransparent_ReturnsFalse()
        {
            Assert.IsFalse(HealthBarLogic.TryFindOpaqueRangeX(new UnityEngine.Color32[20], 10, 2, 16, out _, out _));
        }
    }
}
