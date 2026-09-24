using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class SpawnRampLogicTests
    {
        // ---- Progress ----

        [Test]
        public void Progress_GrowsLinearlyThenClamps()
        {
            Assert.AreEqual(0f, SpawnRampLogic.Progress(0f, 100f), 0.0001f);
            Assert.AreEqual(0.5f, SpawnRampLogic.Progress(50f, 100f), 0.0001f);
            Assert.AreEqual(1f, SpawnRampLogic.Progress(500f, 100f), 0.0001f);
        }

        [Test]
        public void Progress_NegativeElapsed_IsZero()
        {
            Assert.AreEqual(0f, SpawnRampLogic.Progress(-5f, 100f), 0.0001f);
        }

        [Test]
        public void Progress_NonPositiveRamp_IsAlreadyComplete()
        {
            Assert.AreEqual(1f, SpawnRampLogic.Progress(0f, 0f), 0.0001f);
            Assert.AreEqual(1f, SpawnRampLogic.Progress(10f, -3f), 0.0001f);
        }

        // ---- Interval ----

        [Test]
        public void Interval_ShrinksFromStartToEnd()
        {
            Assert.AreEqual(1.5f, SpawnRampLogic.Interval(0f, 1.5f, 0.5f, 100f), 0.0001f);
            Assert.AreEqual(1.0f, SpawnRampLogic.Interval(50f, 1.5f, 0.5f, 100f), 0.0001f);
            Assert.AreEqual(0.5f, SpawnRampLogic.Interval(100f, 1.5f, 0.5f, 100f), 0.0001f);
            Assert.AreEqual(0.5f, SpawnRampLogic.Interval(999f, 1.5f, 0.5f, 100f), 0.0001f);
        }

        [Test]
        public void Interval_NeverDropsBelowFloor()
        {
            Assert.GreaterOrEqual(SpawnRampLogic.Interval(100f, 1f, 0f, 100f), 0.05f);
            Assert.GreaterOrEqual(SpawnRampLogic.Interval(100f, 1f, -2f, 100f), 0.05f);
        }

        // ---- BatchSize ----

        [Test]
        public void BatchSize_RoundsBetweenStartAndEnd()
        {
            Assert.AreEqual(1, SpawnRampLogic.BatchSize(0f, 1, 3, 100f));
            Assert.AreEqual(2, SpawnRampLogic.BatchSize(50f, 1, 3, 100f));
            Assert.AreEqual(3, SpawnRampLogic.BatchSize(100f, 1, 3, 100f));
        }

        [Test]
        public void BatchSize_AlwaysAtLeastOne()
        {
            Assert.AreEqual(1, SpawnRampLogic.BatchSize(0f, 0, 0, 100f));
            Assert.AreEqual(1, SpawnRampLogic.BatchSize(100f, -3, -1, 100f));
        }
    }
}
