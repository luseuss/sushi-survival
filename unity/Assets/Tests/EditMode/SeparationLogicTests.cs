using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Enemies;

namespace SushiSurvival.EditModeTests
{
    public class SeparationLogicTests
    {
        [Test]
        public void ComputeSeparation_ReturnsZero_WithNoNeighbors()
        {
            var result = SeparationLogic.ComputeSeparation(Vector2.zero, new List<Vector2>(), 0.6f);

            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void ComputeSeparation_PushesAwayFromNeighbor()
        {
            var neighbors = new List<Vector2> { new Vector2(0.3f, 0f) };

            var result = SeparationLogic.ComputeSeparation(Vector2.zero, neighbors, 0.6f);

            Assert.Less(result.x, 0f);
        }

        [Test]
        public void ComputeSeparation_IgnoresNeighborsBeyondRadius()
        {
            var neighbors = new List<Vector2> { new Vector2(5f, 0f) };

            var result = SeparationLogic.ComputeSeparation(Vector2.zero, neighbors, 0.6f);

            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void ComputeSeparation_ReturnsZero_WhenExactlyOverlapping()
        {
            // 0으로 나누면 NaN이 나오고, 한 번 NaN이 되면 적 위치가 영원히 망가진다.
            var neighbors = new List<Vector2> { Vector2.zero };

            var result = SeparationLogic.ComputeSeparation(Vector2.zero, neighbors, 0.6f);

            Assert.IsFalse(float.IsNaN(result.x));
            Assert.IsFalse(float.IsNaN(result.y));
            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void ComputeSeparation_CancelsOut_ForSymmetricNeighbors()
        {
            var neighbors = new List<Vector2> { new Vector2(0.3f, 0f), new Vector2(-0.3f, 0f) };

            var result = SeparationLogic.ComputeSeparation(Vector2.zero, neighbors, 0.6f);

            Assert.That(result.magnitude, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ComputeSeparation_ReturnsUnitLength_WhenPushed()
        {
            var neighbors = new List<Vector2> { new Vector2(0.1f, 0.1f) };

            var result = SeparationLogic.ComputeSeparation(Vector2.zero, neighbors, 0.6f);

            Assert.That(result.magnitude, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void ComputeSeparation_ReturnsZero_ForNonPositiveRadius()
        {
            var neighbors = new List<Vector2> { new Vector2(0.1f, 0f) };

            var result = SeparationLogic.ComputeSeparation(Vector2.zero, neighbors, 0f);

            Assert.AreEqual(Vector2.zero, result);
        }
    }
}
