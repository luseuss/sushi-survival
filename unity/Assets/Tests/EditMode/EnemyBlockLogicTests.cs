using NUnit.Framework;
using SushiSurvival.Enemies;
using UnityEngine;

namespace SushiSurvival.EditModeTests
{
    public class EnemyBlockLogicTests
    {
        [Test]
        public void Resolve_OutsideMinDistance_IsUnchanged()
        {
            Vector2 result = EnemyBlockLogic.Resolve(new Vector2(2f, 0f), Vector2.zero, 0.4f, Vector2.right);

            Assert.AreEqual(2f, result.x, 0.0001f);
            Assert.AreEqual(0f, result.y, 0.0001f);
        }

        [Test]
        public void Resolve_ExactlyAtMinDistance_IsUnchanged()
        {
            Vector2 result = EnemyBlockLogic.Resolve(new Vector2(0.4f, 0f), Vector2.zero, 0.4f, Vector2.right);

            Assert.AreEqual(0.4f, result.x, 0.0001f);
        }

        [Test]
        public void Resolve_InsideMinDistance_IsPushedOutToTheBoundaryAlongTheSameDirection()
        {
            // 위(0,1) 쪽에서 파고들어 온 적은 위쪽 경계에 멈춘다.
            Vector2 result = EnemyBlockLogic.Resolve(new Vector2(0f, 0.1f), Vector2.zero, 0.4f, Vector2.right);

            Assert.AreEqual(0f, result.x, 0.0001f);
            Assert.AreEqual(0.4f, result.y, 0.0001f);
        }

        [Test]
        public void Resolve_PushedPositionIsExactlyMinDistanceAway()
        {
            var blocker = new Vector2(3f, -2f);
            Vector2 result = EnemyBlockLogic.Resolve(new Vector2(3.1f, -1.9f), blocker, 0.5f, Vector2.right);

            Assert.AreEqual(0.5f, (result - blocker).magnitude, 0.0001f);
        }

        [Test]
        public void Resolve_KeepsTheApproachDirection_SoNoSidewaysTeleport()
        {
            Vector2 next = new Vector2(-0.1f, -0.1f);
            Vector2 result = EnemyBlockLogic.Resolve(next, Vector2.zero, 0.4f, Vector2.right);

            Assert.Less(result.x, 0f);
            Assert.Less(result.y, 0f);
            Assert.AreEqual(result.x, result.y, 0.0001f);
        }

        [Test]
        public void Resolve_SameCenter_UsesFallbackDirection()
        {
            Vector2 result = EnemyBlockLogic.Resolve(Vector2.zero, Vector2.zero, 0.4f, new Vector2(0f, -5f));

            Assert.AreEqual(0f, result.x, 0.0001f);
            Assert.AreEqual(-0.4f, result.y, 0.0001f);
        }

        [Test]
        public void Resolve_SameCenterWithNoFallback_DefaultsToRight()
        {
            Vector2 result = EnemyBlockLogic.Resolve(Vector2.zero, Vector2.zero, 0.4f, Vector2.zero);

            Assert.AreEqual(0.4f, result.x, 0.0001f);
            Assert.AreEqual(0f, result.y, 0.0001f);
        }

        [Test]
        public void Resolve_NonPositiveMinDistance_DoesNothing()
        {
            Vector2 result = EnemyBlockLogic.Resolve(new Vector2(0.01f, 0f), Vector2.zero, 0f, Vector2.right);

            Assert.AreEqual(0.01f, result.x, 0.0001f);
        }
    }
}
