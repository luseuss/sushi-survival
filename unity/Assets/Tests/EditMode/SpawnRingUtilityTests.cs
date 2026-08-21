using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Enemies;

namespace SushiSurvival.EditModeTests
{
    public class SpawnRingUtilityTests
    {
        [Test]
        public void GetPositionOnRing_AtAngleZero_IsToTheRightOfCenter()
        {
            var result = SpawnRingUtility.GetPositionOnRing(Vector2.zero, 5f, 0f);

            Assert.That(result.x, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(result.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void GetPositionOnRing_AtAnglePi_IsToTheLeftOfCenter()
        {
            var result = SpawnRingUtility.GetPositionOnRing(Vector2.zero, 5f, Mathf.PI);

            Assert.That(result.x, Is.EqualTo(-5f).Within(0.001f));
            Assert.That(result.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void GetPositionOnRing_AlwaysStaysAtGivenRadiusFromCenter()
        {
            var center = new Vector2(10f, -3f);
            var result = SpawnRingUtility.GetPositionOnRing(center, 7f, 1.234f);

            float distance = Vector2.Distance(center, result);
            Assert.That(distance, Is.EqualTo(7f).Within(0.0001f));
        }
    }
}
