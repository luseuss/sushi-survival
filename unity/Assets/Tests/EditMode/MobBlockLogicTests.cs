using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Enemies;

namespace SushiSurvival.EditModeTests
{
    public class MobBlockLogicTests
    {
        private const float MinDistance = 0.3f;

        [Test]
        public void Resolve_FarAway_MovesFreely()
        {
            var result = MobBlockLogic.Resolve(new Vector2(2f, 0f), new Vector2(1.9f, 0f), Vector2.zero, MinDistance, 0.01f);

            Assert.AreEqual(1.9f, result.x, 0.0001f);
            Assert.AreEqual(0f, result.y, 0.0001f);
        }

        [Test]
        public void Resolve_IntoNeighbor_StopsAtBoundary()
        {
            // 바깥(0.5)에서 이웃 중심 쪽으로 곧장 들어가면 경계(0.3)에서 멈춘다.
            var result = MobBlockLogic.Resolve(new Vector2(0.5f, 0f), new Vector2(0.1f, 0f), Vector2.zero, MinDistance, 0.01f);

            Assert.AreEqual(0.3f, result.x, 0.0001f);
            Assert.AreEqual(0f, result.y, 0.0001f);
        }

        [Test]
        public void Resolve_NeverEndsCloserThanBoundary_WhenStartingOutside()
        {
            var result = MobBlockLogic.Resolve(new Vector2(0.4f, 0.1f), new Vector2(0.15f, 0.05f), Vector2.zero, MinDistance, 0.01f);

            Assert.GreaterOrEqual(result.magnitude, MinDistance - 0.0001f);
        }

        [Test]
        public void Resolve_AlreadyOverlapping_SeparatesOnlyByResolveStep()
        {
            // 거리 0.1로 겹친 채로 더 가까이 가려 하면 그 자리에서 resolveStep만큼만 벌어진다.
            var result = MobBlockLogic.Resolve(new Vector2(0.1f, 0f), new Vector2(0.05f, 0f), Vector2.zero, MinDistance, 0.012f);

            Assert.AreEqual(0.112f, result.x, 0.0001f);
        }

        [Test]
        public void Resolve_AlreadyOverlapping_MovingApart_IsNotHeldBack()
        {
            var result = MobBlockLogic.Resolve(new Vector2(0.1f, 0f), new Vector2(0.4f, 0f), Vector2.zero, MinDistance, 0.012f);

            Assert.AreEqual(0.4f, result.x, 0.0001f);
        }

        [Test]
        public void Resolve_ExactlySameSpot_DoesNotProduceNaN()
        {
            var result = MobBlockLogic.Resolve(Vector2.zero, Vector2.zero, Vector2.zero, MinDistance, 0.01f);

            Assert.IsFalse(float.IsNaN(result.x) || float.IsNaN(result.y));
        }

        [Test]
        public void Resolve_ZeroMinDistance_ReturnsNext()
        {
            var result = MobBlockLogic.Resolve(Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, 0f, 0.01f);

            Assert.AreEqual(new Vector2(0.5f, 0.5f), result);
        }
    }
}
