using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Pickups;

namespace SushiSurvival.EditModeTests
{
    public class PickupUtilityTests
    {
        [Test]
        public void IsWithinPickupRadius_TrueWhenCloseEnough()
        {
            bool result = PickupUtility.IsWithinPickupRadius(Vector2.zero, new Vector2(0.3f, 0f), 0.5f);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsWithinPickupRadius_FalseWhenTooFar()
        {
            bool result = PickupUtility.IsWithinPickupRadius(Vector2.zero, new Vector2(5f, 0f), 0.5f);

            Assert.IsFalse(result);
        }

        [Test]
        public void IsWithinPickupRadius_TrueExactlyAtRadius()
        {
            bool result = PickupUtility.IsWithinPickupRadius(Vector2.zero, new Vector2(0.5f, 0f), 0.5f);

            Assert.IsTrue(result);
        }
    }
}
