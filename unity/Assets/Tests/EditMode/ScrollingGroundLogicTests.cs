using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class ScrollingGroundLogicTests
    {
        [Test]
        public void Advance_AddsDelta_WithinOneTile()
        {
            Assert.AreEqual(150f, ScrollingGroundLogic.Advance(100f, 50f, 1000f), 0.0001f);
        }

        [Test]
        public void Advance_WrapsBackToStart_WhenPastTileWidth()
        {
            Assert.AreEqual(50f, ScrollingGroundLogic.Advance(980f, 70f, 1000f), 0.0001f);
        }

        [Test]
        public void Advance_ExactlyOneTile_ReturnsZero()
        {
            Assert.AreEqual(0f, ScrollingGroundLogic.Advance(0f, 1000f, 1000f), 0.0001f);
        }

        [Test]
        public void Advance_NegativeDelta_StaysInRange()
        {
            Assert.AreEqual(950f, ScrollingGroundLogic.Advance(20f, -70f, 1000f), 0.0001f);
        }

        [Test]
        public void Advance_ZeroWidth_ReturnsZero()
        {
            Assert.AreEqual(0f, ScrollingGroundLogic.Advance(5f, 10f, 0f), 0.0001f);
        }
    }
}
