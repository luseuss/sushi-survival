using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Enemies;

namespace SushiSurvival.EditModeTests
{
    public class KnockbackLogicTests
    {
        [Test]
        public void ComputeImpulse_PushesAwayFromSource()
        {
            // 공격자가 왼쪽에 있으면 적은 오른쪽으로 밀린다.
            var result = KnockbackLogic.ComputeImpulse(new Vector2(-1f, 0f), Vector2.zero, 3f, 0f);

            Assert.Greater(result.x, 0f);
        }

        [Test]
        public void ComputeImpulse_UsesFullForce_WithoutResistance()
        {
            var result = KnockbackLogic.ComputeImpulse(new Vector2(-1f, 0f), Vector2.zero, 3f, 0f);

            Assert.That(result.magnitude, Is.EqualTo(3f).Within(0.0001f));
        }

        [Test]
        public void ComputeImpulse_HalvesForce_AtHalfResistance()
        {
            var result = KnockbackLogic.ComputeImpulse(new Vector2(-1f, 0f), Vector2.zero, 3f, 0.5f);

            Assert.That(result.magnitude, Is.EqualTo(1.5f).Within(0.0001f));
        }

        [Test]
        public void ComputeImpulse_ReturnsZero_AtFullResistance()
        {
            var result = KnockbackLogic.ComputeImpulse(new Vector2(-1f, 0f), Vector2.zero, 3f, 1f);

            Assert.That(result.magnitude, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ComputeImpulse_ClampsResistanceAboveOne()
        {
            var result = KnockbackLogic.ComputeImpulse(new Vector2(-1f, 0f), Vector2.zero, 3f, 5f);

            Assert.That(result.magnitude, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ComputeImpulse_ReturnsZero_WhenExactlyOverlapping()
        {
            var result = KnockbackLogic.ComputeImpulse(Vector2.zero, Vector2.zero, 3f, 0f);

            Assert.IsFalse(float.IsNaN(result.x));
            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void Decay_ReducesSpeedOverTime()
        {
            var result = KnockbackLogic.Decay(new Vector2(3f, 0f), 12f, 0.1f);

            Assert.That(result.magnitude, Is.EqualTo(1.8f).Within(0.0001f));
        }

        [Test]
        public void Decay_ReachesExactlyZero_AndNeverReverses()
        {
            var result = KnockbackLogic.Decay(new Vector2(1f, 0f), 12f, 1f);

            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void Decay_KeepsDirection()
        {
            var result = KnockbackLogic.Decay(new Vector2(0f, 3f), 12f, 0.1f);

            Assert.That(result.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.Greater(result.y, 0f);
        }
    }
}
