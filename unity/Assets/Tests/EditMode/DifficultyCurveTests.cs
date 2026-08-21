using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class DifficultyCurveTests
    {
        private const float PerMinute = 0.6f;
        private const float Max = 4f;
        private const float Tolerance = 0.0001f;

        [Test]
        public void GetMultiplier_IsOne_AtRunStart()
        {
            Assert.AreEqual(1f, DifficultyCurve.GetMultiplier(0f, PerMinute, Max), Tolerance);
        }

        [Test]
        public void GetMultiplier_GrowsByRatePerMinute()
        {
            Assert.AreEqual(1.6f, DifficultyCurve.GetMultiplier(60f, PerMinute, Max), Tolerance);
            Assert.AreEqual(2.2f, DifficultyCurve.GetMultiplier(120f, PerMinute, Max), Tolerance);
        }

        [Test]
        public void GetMultiplier_GrowsSmoothlyBetweenMinutes()
        {
            // 1분 단위로 계단처럼 뛰면 특정 순간에 몹이 갑자기 안 죽는 느낌이 난다.
            Assert.AreEqual(1.3f, DifficultyCurve.GetMultiplier(30f, PerMinute, Max), Tolerance);
        }

        [Test]
        public void GetMultiplier_ReachesCapAtFiveMinutes()
        {
            Assert.AreEqual(4f, DifficultyCurve.GetMultiplier(300f, PerMinute, Max), Tolerance);
        }

        [Test]
        public void GetMultiplier_StopsAtCap()
        {
            // 보스전이 길어져도 소환된 잡몹이 계속 불어나면 안 된다.
            Assert.AreEqual(4f, DifficultyCurve.GetMultiplier(900f, PerMinute, Max), Tolerance);
        }

        [Test]
        public void GetMultiplier_NeverGoesBelowOne()
        {
            // 상한을 실수로 0으로 두면 잡몹 체력이 0이 되어 즉사한다.
            Assert.AreEqual(1f, DifficultyCurve.GetMultiplier(300f, PerMinute, 0f), Tolerance);
            Assert.AreEqual(1f, DifficultyCurve.GetMultiplier(-10f, PerMinute, Max), Tolerance);
            Assert.AreEqual(1f, DifficultyCurve.GetMultiplier(300f, -1f, Max), Tolerance);
        }

        [Test]
        public void GetMultiplier_IsFlat_WhenRateIsZero()
        {
            Assert.AreEqual(1f, DifficultyCurve.GetMultiplier(300f, 0f, Max), Tolerance);
        }
    }
}
