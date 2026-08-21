using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Weapons;

namespace SushiSurvival.EditModeTests
{
    public class FanHitTestTests
    {
        [Test]
        public void IsInsideFan_TrueForTargetDirectlyAhead()
        {
            bool result = FanHitTest.IsInsideFan(Vector2.zero, Vector2.right, 2f, 120f, new Vector2(1f, 0f));

            Assert.IsTrue(result);
        }

        [Test]
        public void IsInsideFan_FalseForTargetBehind()
        {
            bool result = FanHitTest.IsInsideFan(Vector2.zero, Vector2.right, 2f, 120f, new Vector2(-1f, 0f));

            Assert.IsFalse(result);
        }

        [Test]
        public void IsInsideFan_FalseWhenBeyondRadius()
        {
            bool result = FanHitTest.IsInsideFan(Vector2.zero, Vector2.right, 2f, 120f, new Vector2(5f, 0f));

            Assert.IsFalse(result);
        }

        [Test]
        public void IsInsideFan_TrueAtHalfAngleBoundary()
        {
            float radians = 60f * Mathf.Deg2Rad;
            var target = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 1.5f;

            bool result = FanHitTest.IsInsideFan(Vector2.zero, Vector2.right, 2f, 120f, target);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsInsideFan_FalseJustOutsideAngleBoundary()
        {
            float radians = 61f * Mathf.Deg2Rad;
            var target = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 1.5f;

            bool result = FanHitTest.IsInsideFan(Vector2.zero, Vector2.right, 2f, 120f, target);

            Assert.IsFalse(result);
        }
    }
}
