using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class AugmentAvailabilityTests
    {
        [Test]
        public void IsAvailable_True_WhenNothingTakenYet()
        {
            Assert.IsTrue(AugmentAvailability.IsAvailable(0f, 2f));
        }

        [Test]
        public void IsAvailable_True_WhenPartiallyTaken()
        {
            Assert.IsTrue(AugmentAvailability.IsAvailable(1.4f, 2f));
        }

        [Test]
        public void IsAvailable_False_AtCap()
        {
            Assert.IsFalse(AugmentAvailability.IsAvailable(2f, 2f));
        }

        [Test]
        public void IsAvailable_False_BeyondCap()
        {
            Assert.IsFalse(AugmentAvailability.IsAvailable(2.5f, 2f));
        }
    }
}
