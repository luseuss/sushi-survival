using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class GroundTransitionLogicTests
    {
        [Test]
        public void RadiusAt_StartsAtZeroAndEndsAtMax()
        {
            Assert.AreEqual(0f, GroundTransitionLogic.RadiusAt(0f, 1f, 20f), 0.0001f);
            Assert.AreEqual(20f, GroundTransitionLogic.RadiusAt(1f, 1f, 20f), 0.0001f);
            Assert.AreEqual(20f, GroundTransitionLogic.RadiusAt(5f, 1f, 20f), 0.0001f);
        }

        [Test]
        public void RadiusAt_SpreadsAtConstantSpeed()
        {
            float half = GroundTransitionLogic.RadiusAt(0.5f, 1f, 20f);

            Assert.AreEqual(10f, half, 0.0001f);
        }

        [Test]
        public void RadiusAt_ZeroDurationFinishesAtOnce_NoDistanceStaysZero()
        {
            Assert.AreEqual(20f, GroundTransitionLogic.RadiusAt(0f, 0f, 20f), 0.0001f);
            Assert.AreEqual(0f, GroundTransitionLogic.RadiusAt(1f, 1f, 0f), 0.0001f);
        }

        [Test]
        public void AdvanceIndex_ConsumesOnlyTilesInsideTheRadius()
        {
            var distances = new[] { 1f, 2f, 2f, 5f, 9f };

            Assert.AreEqual(0, GroundTransitionLogic.AdvanceIndex(distances, 0, 0.5f));
            Assert.AreEqual(3, GroundTransitionLogic.AdvanceIndex(distances, 0, 2f));
            Assert.AreEqual(5, GroundTransitionLogic.AdvanceIndex(distances, 3, 100f));
        }

        [Test]
        public void AdvanceIndex_NeverMovesBackwards()
        {
            var distances = new[] { 1f, 2f, 3f };

            Assert.AreEqual(2, GroundTransitionLogic.AdvanceIndex(distances, 2, 0.1f));
        }
    }
}
