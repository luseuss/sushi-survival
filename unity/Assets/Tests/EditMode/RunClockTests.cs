using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class RunClockTests
    {
        [Test]
        public void FormatRemaining_ShowsFullDuration_AtStart()
        {
            Assert.AreEqual("5:00", RunClock.FormatRemaining(0f, 300f));
        }

        [Test]
        public void FormatRemaining_CountsDownByMinute()
        {
            Assert.AreEqual("4:00", RunClock.FormatRemaining(60f, 300f));
        }

        [Test]
        public void FormatRemaining_RollsOverAtSixtySeconds()
        {
            // 남은 60초가 "0:60"으로 나오면 실패다.
            Assert.AreEqual("1:00", RunClock.FormatRemaining(240f, 300f));
        }

        [Test]
        public void FormatRemaining_RoundsUp_SoLastSecondIsVisible()
        {
            Assert.AreEqual("0:01", RunClock.FormatRemaining(299.5f, 300f));
        }

        [Test]
        public void FormatRemaining_ZeroAtExactEnd()
        {
            Assert.AreEqual("0:00", RunClock.FormatRemaining(300f, 300f));
        }

        [Test]
        public void FormatRemaining_NeverGoesNegative()
        {
            Assert.AreEqual("0:00", RunClock.FormatRemaining(350f, 300f));
        }

        [Test]
        public void FormatRemaining_ClampsToDuration_WhenElapsedIsNegative()
        {
            Assert.AreEqual("5:00", RunClock.FormatRemaining(-5f, 300f));
        }

        [Test]
        public void FormatElapsed_ZeroAtStart()
        {
            Assert.AreEqual("0:00", RunClock.FormatElapsed(0f));
        }

        [Test]
        public void FormatElapsed_FloorsPartialSecond()
        {
            Assert.AreEqual("0:59", RunClock.FormatElapsed(59.9f));
        }

        [Test]
        public void FormatElapsed_RollsOverAtSixtySeconds()
        {
            Assert.AreEqual("1:00", RunClock.FormatElapsed(60f));
        }

        [Test]
        public void FormatElapsed_FormatsMinutesAndSeconds()
        {
            Assert.AreEqual("3:42", RunClock.FormatElapsed(222f));
        }

        [Test]
        public void FormatElapsed_ClampsNegativeToZero()
        {
            Assert.AreEqual("0:00", RunClock.FormatElapsed(-5f));
        }
    }
}
