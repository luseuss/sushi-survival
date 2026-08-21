using System.Collections.Generic;
using NUnit.Framework;
using SushiSurvival.Enemies.Boss;

namespace SushiSurvival.EditModeTests
{
    public class MeteorVolleyTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void GetLaunchDelays_PhaseOne_ReturnsThreeEvenlySpaced()
        {
            IReadOnlyList<float> delays = MeteorVolley.GetLaunchDelays(3, 0.35f);

            Assert.AreEqual(3, delays.Count);
            Assert.AreEqual(0f, delays[0], Tolerance);
            Assert.AreEqual(0.35f, delays[1], Tolerance);
            Assert.AreEqual(0.70f, delays[2], Tolerance);
        }

        [Test]
        public void GetLaunchDelays_PhaseTwo_ReturnsFive()
        {
            IReadOnlyList<float> delays = MeteorVolley.GetLaunchDelays(5, 0.3f);

            Assert.AreEqual(5, delays.Count);
            Assert.AreEqual(1.2f, delays[4], Tolerance);
        }

        [Test]
        public void GetLaunchDelays_FirstShotIsAlwaysImmediate()
        {
            Assert.AreEqual(0f, MeteorVolley.GetLaunchDelays(1, 5f)[0], Tolerance);
        }

        [Test]
        public void GetLaunchDelays_ReturnsEmpty_WhenCountIsZeroOrNegative()
        {
            Assert.AreEqual(0, MeteorVolley.GetLaunchDelays(0, 0.35f).Count);
            Assert.AreEqual(0, MeteorVolley.GetLaunchDelays(-2, 0.35f).Count);
        }

        [Test]
        public void GetLaunchDelays_ClampsNegativeSpacingToZero()
        {
            // 데이터 입력 실수로 음수가 들어와도 발사 순서가 뒤집히지 않게 한다.
            IReadOnlyList<float> delays = MeteorVolley.GetLaunchDelays(3, -1f);

            foreach (float delay in delays)
                Assert.AreEqual(0f, delay, Tolerance);
        }

        [Test]
        public void GetTotalDuration_CoversLastShotWarning()
        {
            // 마지막 발이 터질 때까지가 패턴의 전체 길이다.
            Assert.AreEqual(0.70f + 0.7f,
                MeteorVolley.GetTotalDuration(3, 0.35f, 0.7f), Tolerance);
        }

        [Test]
        public void GetTotalDuration_IsZero_WhenNoShots()
        {
            Assert.AreEqual(0f, MeteorVolley.GetTotalDuration(0, 0.35f, 0.7f), Tolerance);
        }
    }
}
