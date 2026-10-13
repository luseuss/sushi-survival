using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class HitVignetteLogicTests
    {
        [Test]
        public void EdgeIntensity_CenterIsClear()
        {
            Assert.AreEqual(0f, HitVignetteLogic.EdgeIntensity(0f, 0f), 0.0001f);
        }

        [Test]
        public void EdgeIntensity_CornerIsFull()
        {
            Assert.AreEqual(1f, HitVignetteLogic.EdgeIntensity(1f, 1f), 0.0001f);
            Assert.AreEqual(1f, HitVignetteLogic.EdgeIntensity(-1f, 1f), 0.0001f);
        }

        [Test]
        public void EdgeIntensity_GrowsTowardEdges()
        {
            float near = HitVignetteLogic.EdgeIntensity(0.3f, 0f);
            float mid = HitVignetteLogic.EdgeIntensity(0.7f, 0.2f);
            float far = HitVignetteLogic.EdgeIntensity(1f, 0.6f);

            Assert.Less(near, mid);
            Assert.Less(mid, far);
        }

        [Test]
        public void Alpha_StartsAtPeakAndFadesToZero()
        {
            Assert.AreEqual(0.65f, HitVignetteLogic.Alpha(0f, 0.65f), 0.0001f);
            Assert.AreEqual(0f, HitVignetteLogic.Alpha(1f, 0.65f), 0.0001f);
        }

        [Test]
        public void Alpha_DropsQuicklyThenLingers()
        {
            // 제곱으로 줄어서 중간 지점에서는 이미 4분의 1이다.
            Assert.AreEqual(0.25f * 0.8f, HitVignetteLogic.Alpha(0.5f, 0.8f), 0.0001f);
        }

        [Test]
        public void Alpha_ClampsProgress()
        {
            Assert.AreEqual(0f, HitVignetteLogic.Alpha(3f, 0.65f), 0.0001f);
            Assert.AreEqual(0.65f, HitVignetteLogic.Alpha(-1f, 0.65f), 0.0001f);
        }
    }
}
