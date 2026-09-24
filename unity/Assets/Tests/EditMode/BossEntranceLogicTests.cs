using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class BossEntranceLogicTests
    {
        [Test]
        public void DropStartHeight_IsAboveTheScreenEdge()
        {
            Assert.AreEqual(7f, BossEntranceLogic.DropStartHeight(5f, 2f), 0.0001f);
            Assert.AreEqual(2f, BossEntranceLogic.DropStartHeight(-1f, 2f), 0.0001f);
        }

        [Test]
        public void Progress_IsClampedAndZeroDurationFinishesAtOnce()
        {
            Assert.AreEqual(0.5f, BossEntranceLogic.Progress(1f, 2f), 0.0001f);
            Assert.AreEqual(1f, BossEntranceLogic.Progress(9f, 2f), 0.0001f);
            Assert.AreEqual(1f, BossEntranceLogic.Progress(0f, 0f), 0.0001f);
        }

        [Test]
        public void EaseIn_StartsSlowAndEndsAtOne()
        {
            Assert.AreEqual(0f, BossEntranceLogic.EaseIn(0f), 0.0001f);
            Assert.AreEqual(0.25f, BossEntranceLogic.EaseIn(0.5f), 0.0001f);
            Assert.AreEqual(1f, BossEntranceLogic.EaseIn(2f), 0.0001f);
        }

        [Test]
        public void DropPosition_FallsFromAboveToTheLandingPoint()
        {
            var landing = new Vector3(1f, 8f, 0f);

            Assert.AreEqual(new Vector3(1f, 15f, 0f), BossEntranceLogic.DropPosition(landing, 7f, 0f));
            Assert.AreEqual(landing, BossEntranceLogic.DropPosition(landing, 7f, 1f));
        }

        [Test]
        public void ShakeOffset_DecaysToZero()
        {
            Vector2 unit = Vector2.one;

            Assert.AreEqual(new Vector2(0.4f, 0.4f), BossEntranceLogic.ShakeOffset(0f, 1f, 0.4f, unit));
            Assert.AreEqual(new Vector2(0.2f, 0.2f), BossEntranceLogic.ShakeOffset(0.5f, 1f, 0.4f, unit));
            Assert.AreEqual(Vector2.zero, BossEntranceLogic.ShakeOffset(1f, 1f, 0.4f, unit));
            Assert.AreEqual(Vector2.zero, BossEntranceLogic.ShakeOffset(0f, 0f, 0.4f, unit));
        }
    }
}
