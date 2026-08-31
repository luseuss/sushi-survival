using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class DurationExtensionTests
    {
        [Test]
        public void Extend_ReturnsRequested_WhenLongerThanRemaining()
        {
            Assert.AreEqual(0.5f, DurationExtension.Extend(0.1f, 0.5f), 0.0001f);
        }

        [Test]
        public void Extend_KeepsRemaining_WhenRequestedIsShorter()
        {
            // 짧은 히트가 나중에 들어와도 이미 걸린 긴 정지를 줄이면 안 된다.
            Assert.AreEqual(0.5f, DurationExtension.Extend(0.5f, 0.1f), 0.0001f);
        }

        [Test]
        public void Extend_ReturnsRequested_WhenRemainingIsZero()
        {
            Assert.AreEqual(0.3f, DurationExtension.Extend(0f, 0.3f), 0.0001f);
        }

        [Test]
        public void Extend_HandlesEqualValues()
        {
            Assert.AreEqual(0.2f, DurationExtension.Extend(0.2f, 0.2f), 0.0001f);
        }

        [Test]
        public void Extend_IgnoresNegativeRequest()
        {
            Assert.AreEqual(0.4f, DurationExtension.Extend(0.4f, -1f), 0.0001f);
        }
    }
}
