using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class DamageNumberLogicTests
    {
        // ---- Format ----

        [Test]
        public void Format_RoundsToNearestInteger()
        {
            Assert.AreEqual("8", DamageNumberLogic.Format(8f));
            Assert.AreEqual("13", DamageNumberLogic.Format(12.6f));
            Assert.AreEqual("12", DamageNumberLogic.Format(12.4f));
        }

        [Test]
        public void Format_TinyOrNegativeDamage_ShowsAtLeastOne()
        {
            Assert.AreEqual("1", DamageNumberLogic.Format(0.2f));
            Assert.AreEqual("1", DamageNumberLogic.Format(0f));
            Assert.AreEqual("1", DamageNumberLogic.Format(-5f));
        }

        // ---- PopScale ----

        [Test]
        public void PopScale_StartsAtZeroAndSettlesAtOne()
        {
            Assert.AreEqual(0f, DamageNumberLogic.PopScale(0f), 0.0001f);
            Assert.AreEqual(1f, DamageNumberLogic.PopScale(0.25f), 0.0001f);
            Assert.AreEqual(1f, DamageNumberLogic.PopScale(1f), 0.0001f);
        }

        [Test]
        public void PopScale_OvershootsOneBeforeSettling()
        {
            float peak = 0f;
            for (float t = 0f; t <= 0.25f; t += 0.01f)
                peak = System.Math.Max(peak, DamageNumberLogic.PopScale(t));

            Assert.Greater(peak, 1f);
        }

        // ---- Alpha ----

        [Test]
        public void Alpha_StaysOpaqueThenFadesToZero()
        {
            Assert.AreEqual(1f, DamageNumberLogic.Alpha(0f), 0.0001f);
            Assert.AreEqual(1f, DamageNumberLogic.Alpha(0.6f), 0.0001f);
            Assert.AreEqual(0.5f, DamageNumberLogic.Alpha(0.8f), 0.0001f);
            Assert.AreEqual(0f, DamageNumberLogic.Alpha(1f), 0.0001f);
        }

        // ---- Rise ----

        [Test]
        public void Rise_GoesFromZeroToOneAndSlowsDown()
        {
            Assert.AreEqual(0f, DamageNumberLogic.Rise(0f), 0.0001f);
            Assert.AreEqual(1f, DamageNumberLogic.Rise(1f), 0.0001f);

            float early = DamageNumberLogic.Rise(0.2f) - DamageNumberLogic.Rise(0f);
            float late = DamageNumberLogic.Rise(1f) - DamageNumberLogic.Rise(0.8f);
            Assert.Greater(early, late);
        }

        [Test]
        public void Rise_ClampsOutOfRange()
        {
            Assert.AreEqual(0f, DamageNumberLogic.Rise(-1f), 0.0001f);
            Assert.AreEqual(1f, DamageNumberLogic.Rise(5f), 0.0001f);
        }
    }
}
