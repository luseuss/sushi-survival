using System.Collections.Generic;
using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class WaveScheduleTests
    {
        [Test]
        public void GetDueIndices_ReturnsEmpty_WhenNothingReachedYet()
        {
            var times = new List<float> { 120f, 240f };

            var result = WaveSchedule.GetDueIndices(times, 10f, 11f);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetDueIndices_ReturnsEventInsideWindow()
        {
            var times = new List<float> { 120f, 240f };

            var result = WaveSchedule.GetDueIndices(times, 119.9f, 120.1f);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(0, result[0]);
        }

        [Test]
        public void GetDueIndices_ExcludesEventAtPreviousTime_SoItNeverFiresTwice()
        {
            var times = new List<float> { 120f };

            var result = WaveSchedule.GetDueIndices(times, 120f, 120.5f);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetDueIndices_IncludesEventExactlyAtCurrentTime()
        {
            var times = new List<float> { 120f };

            var result = WaveSchedule.GetDueIndices(times, 119.5f, 120f);

            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void GetDueIndices_ReturnsMultipleEvents_WhenFrameSpansBoth()
        {
            // 에디터 멈춤 등으로 델타가 크게 튀는 경우
            var times = new List<float> { 120f, 240f };

            var result = WaveSchedule.GetDueIndices(times, 100f, 300f);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(0, result[0]);
            Assert.AreEqual(1, result[1]);
        }

        [Test]
        public void GetDueIndices_FiresZeroSecondEvent_WhenPreviousTimeIsNegative()
        {
            // WaveDirector는 previousTime을 -1로 시작해 0초 이벤트를 놓치지 않는다.
            var times = new List<float> { 0f };

            var result = WaveSchedule.GetDueIndices(times, -1f, 0.016f);

            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void GetDueIndices_IgnoresAlreadyPassedEvents()
        {
            var times = new List<float> { 120f };

            var result = WaveSchedule.GetDueIndices(times, 200f, 201f);

            Assert.AreEqual(0, result.Count);
        }
    }
}
