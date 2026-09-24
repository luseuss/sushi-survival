using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class EntranceLogicTests
    {
        // ---- CardProgress ----

        [Test]
        public void CardProgress_FirstCardStartsImmediately()
        {
            Assert.AreEqual(0f, EntranceLogic.CardProgress(0f, 0, 0.15f, 0.4f), 0.0001f);
            Assert.AreEqual(0.5f, EntranceLogic.CardProgress(0.2f, 0, 0.15f, 0.4f), 0.0001f);
            Assert.AreEqual(1f, EntranceLogic.CardProgress(0.4f, 0, 0.15f, 0.4f), 0.0001f);
        }

        [Test]
        public void CardProgress_LaterCardsStartLater()
        {
            Assert.AreEqual(0f, EntranceLogic.CardProgress(0.15f, 1, 0.15f, 0.4f), 0.0001f);
            Assert.AreEqual(0f, EntranceLogic.CardProgress(0.29f, 2, 0.15f, 0.4f), 0.0001f);
            Assert.Greater(EntranceLogic.CardProgress(0.35f, 2, 0.15f, 0.4f), 0f);
        }

        [Test]
        public void CardProgress_ClampsToZeroAndOne()
        {
            Assert.AreEqual(0f, EntranceLogic.CardProgress(-1f, 0, 0.15f, 0.4f), 0.0001f);
            Assert.AreEqual(1f, EntranceLogic.CardProgress(99f, 2, 0.15f, 0.4f), 0.0001f);
        }

        [Test]
        public void CardProgress_NonPositiveDuration_IsAlreadyComplete()
        {
            Assert.AreEqual(1f, EntranceLogic.CardProgress(0f, 1, 0.15f, 0f), 0.0001f);
        }

        // ---- AnimationDuration / UnlockTime ----

        [Test]
        public void AnimationDuration_IsLastCardStartPlusDuration()
        {
            Assert.AreEqual(0.7f, EntranceLogic.AnimationDuration(3, 0.15f, 0.4f), 0.0001f);
            Assert.AreEqual(0.4f, EntranceLogic.AnimationDuration(1, 0.15f, 0.4f), 0.0001f);
        }

        [Test]
        public void AnimationDuration_NoCards_IsZero()
        {
            Assert.AreEqual(0f, EntranceLogic.AnimationDuration(0, 0.15f, 0.4f), 0.0001f);
        }

        [Test]
        public void UnlockTime_AddsHoldAfterTheAnimation()
        {
            Assert.AreEqual(1.0f, EntranceLogic.UnlockTime(3, 0.15f, 0.4f, 0.3f), 0.0001f);
        }

        [Test]
        public void UnlockTime_NegativeHold_IsTreatedAsZero()
        {
            Assert.AreEqual(0.7f, EntranceLogic.UnlockTime(3, 0.15f, 0.4f, -1f), 0.0001f);
        }

        [Test]
        public void IsUnlocked_OnlyAtOrAfterTheUnlockTime()
        {
            Assert.IsFalse(EntranceLogic.IsUnlocked(0.99f, 1.0f));
            Assert.IsTrue(EntranceLogic.IsUnlocked(1.0f, 1.0f));
            Assert.IsTrue(EntranceLogic.IsUnlocked(2.0f, 1.0f));
        }

        // ---- EaseOutCubic ----

        [Test]
        public void EaseOutCubic_GoesFromZeroToOneAndSlowsDown()
        {
            Assert.AreEqual(0f, EntranceLogic.EaseOutCubic(0f), 0.0001f);
            Assert.AreEqual(1f, EntranceLogic.EaseOutCubic(1f), 0.0001f);
            Assert.Greater(EntranceLogic.EaseOutCubic(0.5f), 0.5f);
        }
    }
}
