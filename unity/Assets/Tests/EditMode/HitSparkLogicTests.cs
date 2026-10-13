using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class HitSparkLogicTests
    {
        [Test]
        public void InsideStar_Center_IsInside()
        {
            Assert.IsTrue(HitSparkLogic.InsideStar(0f, 0f));
        }

        [Test]
        public void InsideStar_OutsideUnitCircle_IsOutside()
        {
            Assert.IsFalse(HitSparkLogic.InsideStar(0.9f, 0.9f));
            Assert.IsFalse(HitSparkLogic.InsideStar(1.1f, 0f));
        }

        [Test]
        public void InsideStar_LongSpikes_ReachFarOnAxes()
        {
            Assert.IsTrue(HitSparkLogic.InsideStar(0.85f, 0f));
            Assert.IsTrue(HitSparkLogic.InsideStar(0f, -0.85f));
        }

        [Test]
        public void InsideStar_GapBetweenSpikes_IsEmpty()
        {
            // 상하좌우 가시와 대각선 가시 사이의 빈 곳은 가운데 원보다 바깥에서 비어 있어야 별 모양이 된다.
            Assert.IsFalse(HitSparkLogic.InsideStar(0.5f, 0.2f));
        }

        [Test]
        public void Scale_GrowsFromStartToEnd_EasingOut()
        {
            Assert.AreEqual(0.35f, HitSparkLogic.Scale(0f, 0.35f, 1f), 0.0001f);
            Assert.AreEqual(1f, HitSparkLogic.Scale(1f, 0.35f, 1f), 0.0001f);
            Assert.Greater(HitSparkLogic.Scale(0.5f, 0.35f, 1f), 0.675f, "ease-out이라 중간 지점에서 이미 절반 넘게 커져 있다");
        }

        [Test]
        public void Alpha_StaysOpaqueEarlyThenFades()
        {
            Assert.AreEqual(1f, HitSparkLogic.Alpha(0f), 0.0001f);
            Assert.Greater(HitSparkLogic.Alpha(0.3f), 0.9f);
            Assert.AreEqual(0f, HitSparkLogic.Alpha(1f), 0.0001f);
        }

        [Test]
        public void ScaleAndAlpha_ClampProgress()
        {
            Assert.AreEqual(1f, HitSparkLogic.Scale(2f, 0.35f, 1f), 0.0001f);
            Assert.AreEqual(0f, HitSparkLogic.Alpha(2f), 0.0001f);
        }
    }
}
