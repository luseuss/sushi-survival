using NUnit.Framework;
using SushiSurvival.Audio;

namespace SushiSurvival.EditModeTests
{
    public class AudioLogicTests
    {
        [Test]
        public void EffectiveVolume_MultipliesMasterCategoryAndEntry()
        {
            Assert.AreEqual(0.25f, AudioLogic.EffectiveVolume(0.5f, 1f, 0.5f, false), 0.0001f);
            Assert.AreEqual(0.18f, AudioLogic.EffectiveVolume(0.6f, 0.5f, 0.6f, false), 0.0001f);
        }

        [Test]
        public void EffectiveVolume_MutedIsZero()
        {
            Assert.AreEqual(0f, AudioLogic.EffectiveVolume(1f, 1f, 1f, true));
        }

        [Test]
        public void EffectiveVolume_ClampsEachPartToZeroOne()
        {
            Assert.AreEqual(1f, AudioLogic.EffectiveVolume(3f, 2f, 5f, false), 0.0001f);
            Assert.AreEqual(0f, AudioLogic.EffectiveVolume(-1f, 1f, 1f, false), 0.0001f);
        }

        [TestCase(0, 0.5f, -1)]
        [TestCase(1, 0.99f, 0)]
        [TestCase(4, 0f, 0)]
        [TestCase(4, 0.26f, 1)]
        [TestCase(4, 0.99f, 3)]
        [TestCase(4, 1f, 3)]
        public void PickIndex_MapsRollToIndex(int count, float roll, int expected)
        {
            Assert.AreEqual(expected, AudioLogic.PickIndex(count, roll));
        }

        [Test]
        public void CanPlay_FirstTimeAlwaysAllowed()
        {
            Assert.IsTrue(AudioLogic.CanPlay(-1f, 0f, 10f));
        }

        [TestCase(1f, 1.04f, 0.05f, false)]
        [TestCase(1f, 1.06f, 0.05f, true)]
        [TestCase(1f, 2f, 0.05f, true)]
        [TestCase(1f, 1f, 0f, true)]
        public void CanPlay_RespectsCooldown(float last, float now, float cooldown, bool expected)
        {
            Assert.AreEqual(expected, AudioLogic.CanPlay(last, now, cooldown));
        }

        [Test]
        public void PickPitch_InterpolatesWithinRange()
        {
            Assert.AreEqual(0.9f, AudioLogic.PickPitch(0.9f, 1.1f, 0f), 0.0001f);
            Assert.AreEqual(1f, AudioLogic.PickPitch(0.9f, 1.1f, 0.5f), 0.0001f);
            Assert.AreEqual(1.1f, AudioLogic.PickPitch(0.9f, 1.1f, 1f), 0.0001f);
        }

        [Test]
        public void PickPitch_ReversedRangeStillWorks()
        {
            Assert.AreEqual(0.9f, AudioLogic.PickPitch(1.1f, 0.9f, 0f), 0.0001f);
        }

        [Test]
        public void FadeToward_MovesByDeltaOverFadeSeconds()
        {
            // 1초짜리 페이드에서 0.25초가 지나면 0.25만큼 간다.
            Assert.AreEqual(0.25f, AudioLogic.FadeToward(0f, 1f, 1f, 0.25f), 0.0001f);
            Assert.AreEqual(0.5f, AudioLogic.FadeToward(1f, 0f, 2f, 1f), 0.0001f);
        }

        [Test]
        public void FadeToward_DoesNotOvershootTarget()
        {
            Assert.AreEqual(1f, AudioLogic.FadeToward(0.9f, 1f, 1f, 5f), 0.0001f);
        }

        [Test]
        public void FadeToward_ZeroFadeJumpsToTarget()
        {
            Assert.AreEqual(1f, AudioLogic.FadeToward(0f, 1f, 0f, 0.016f));
            Assert.AreEqual(0f, AudioLogic.FadeToward(1f, 0f, -1f, 0.016f));
        }

        [Test]
        public void PickVoice_PrefersFirstIdleVoice()
        {
            Assert.AreEqual(1, AudioLogic.PickVoice(new[] { true, false, false }, new[] { 1f, 2f, 3f }));
        }

        [Test]
        public void PickVoice_AllBusy_StealsOldest()
        {
            Assert.AreEqual(2, AudioLogic.PickVoice(new[] { true, true, true }, new[] { 5f, 3f, 1f }));
        }

        [Test]
        public void PickVoice_NoVoices_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, AudioLogic.PickVoice(new bool[0], new float[0]));
        }

        [TestCase(0f, 2f, 0f)]
        [TestCase(-1f, 2f, 0f)]
        [TestCase(0.15f, 2f, 0.15f)]
        [TestCase(1.99f, 2f, 1.99f)]
        [TestCase(2f, 2f, 0f)]
        [TestCase(5f, 2f, 0f)]
        public void ClampStartTime_IgnoresInvalidValues(float start, float length, float expected)
        {
            Assert.AreEqual(expected, AudioLogic.ClampStartTime(start, length), 0.0001f);
        }
    }
}
