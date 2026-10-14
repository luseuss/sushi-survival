using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class GuideAnimLogicTests
    {
        private const float Eps = 0.0001f;

        [Test]
        public void Progress_ClampsAndTreatsZeroDurationAsDone()
        {
            Assert.AreEqual(0f, GuideAnimLogic.Progress(-1f, 0.2f), Eps);
            Assert.AreEqual(0.5f, GuideAnimLogic.Progress(0.1f, 0.2f), Eps);
            Assert.AreEqual(1f, GuideAnimLogic.Progress(5f, 0.2f), Eps);
            Assert.AreEqual(1f, GuideAnimLogic.Progress(0f, 0f), Eps);
        }

        [Test]
        public void Open_GrowsFromNinetyPercentToFull()
        {
            Assert.AreEqual(0.9f, GuideAnimLogic.OpenScale(0f), Eps);
            Assert.AreEqual(1f, GuideAnimLogic.OpenScale(1f), Eps);
            Assert.AreEqual(0f, GuideAnimLogic.OpenAlpha(0f), Eps);
            Assert.AreEqual(1f, GuideAnimLogic.OpenAlpha(1f), Eps);
        }

        [Test]
        public void Close_ShrinksAndFades()
        {
            Assert.AreEqual(1f, GuideAnimLogic.CloseScale(0f), Eps);
            Assert.AreEqual(0.95f, GuideAnimLogic.CloseScale(1f), Eps);
            Assert.AreEqual(1f, GuideAnimLogic.CloseAlpha(0f), Eps);
            Assert.AreEqual(0f, GuideAnimLogic.CloseAlpha(1f), Eps);
        }

        [Test]
        public void Tab_RisesEightPixelsAndEndsAtRest()
        {
            Assert.AreEqual(8f, GuideAnimLogic.TabOffset(0f), Eps);
            Assert.AreEqual(0f, GuideAnimLogic.TabOffset(1f), Eps);
            Assert.AreEqual(1f, GuideAnimLogic.TabAlpha(1f), Eps);
        }

        [Test]
        public void RowDelay_StaggersAndIsCapped()
        {
            Assert.AreEqual(0f, GuideAnimLogic.RowDelay(0), Eps);
            Assert.AreEqual(0.03f, GuideAnimLogic.RowDelay(1), Eps);
            Assert.AreEqual(GuideAnimLogic.MaxRowDelay, GuideAnimLogic.RowDelay(99), Eps);
        }

        [Test]
        public void RowProgress_WaitsForItsDelay_AndAllRowsFinishWithinTotal()
        {
            Assert.AreEqual(0f, GuideAnimLogic.RowProgress(0.02f, 2), Eps);
            Assert.AreEqual(1f, GuideAnimLogic.RowProgress(GuideAnimLogic.TabTotalDuration, 99), Eps);
        }

        [Test]
        public void Bounce_PeaksInTheMiddleAndReturnsToOne()
        {
            Assert.AreEqual(1f, GuideAnimLogic.Bounce(0f), Eps);
            Assert.AreEqual(1.15f, GuideAnimLogic.Bounce(0.5f), Eps);
            Assert.AreEqual(1f, GuideAnimLogic.Bounce(1f), Eps);
        }

        [Test]
        public void FrameIndex_LoopsAndHandlesEmpty()
        {
            Assert.AreEqual(-1, GuideAnimLogic.FrameIndex(1f, 8f, 0));
            Assert.AreEqual(0, GuideAnimLogic.FrameIndex(0f, 8f, 5));
            Assert.AreEqual(1, GuideAnimLogic.FrameIndex(0.13f, 8f, 5));
            Assert.AreEqual(0, GuideAnimLogic.FrameIndex(5f / 8f, 8f, 5));
            Assert.AreEqual(0, GuideAnimLogic.FrameIndex(1f, 0f, 5));
        }
    }
}
