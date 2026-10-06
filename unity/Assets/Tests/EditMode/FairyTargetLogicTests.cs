using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Companions;

namespace SushiSurvival.EditModeTests
{
    public class FairyTargetLogicTests
    {
        [Test]
        public void PicksNearestWithinRange()
        {
            var positions = new List<Vector2> { new Vector2(5f, 0f), new Vector2(2f, 0f), new Vector2(3f, 0f) };

            Assert.AreEqual(1, FairyTargetLogic.NearestIndex(Vector2.zero, positions, 10f));
        }

        [Test]
        public void OutsideRange_ReturnsMinusOne()
        {
            var positions = new List<Vector2> { new Vector2(8f, 0f) };

            Assert.AreEqual(-1, FairyTargetLogic.NearestIndex(Vector2.zero, positions, 5f));
        }

        [Test]
        public void ExactlyAtRange_IsIncluded()
        {
            var positions = new List<Vector2> { new Vector2(5f, 0f) };

            Assert.AreEqual(0, FairyTargetLogic.NearestIndex(Vector2.zero, positions, 5f));
        }

        [Test]
        public void EmptyOrNull_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, FairyTargetLogic.NearestIndex(Vector2.zero, new List<Vector2>(), 5f));
            Assert.AreEqual(-1, FairyTargetLogic.NearestIndex(Vector2.zero, null, 5f));
        }

        [Test]
        public void Tie_PrefersEarlierIndex()
        {
            var positions = new List<Vector2> { new Vector2(0f, 3f), new Vector2(3f, 0f) };

            Assert.AreEqual(0, FairyTargetLogic.NearestIndex(Vector2.zero, positions, 5f));
        }

        [Test]
        public void MeasuresFromOrigin_NotFromWorldZero()
        {
            var positions = new List<Vector2> { new Vector2(10f, 10f), new Vector2(-10f, -10f) };

            Assert.AreEqual(1, FairyTargetLogic.NearestIndex(new Vector2(-9f, -9f), positions, 5f));
        }
    }
}
