using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class CameraShakeLogicTests
    {
        [Test]
        public void GetMagnitude_ReturnsPeak_WhenRemainingAboveTail()
        {
            Assert.AreEqual(0.2f, CameraShakeLogic.GetMagnitude(1f, 0.2f), 0.0001f);
        }

        [Test]
        public void GetMagnitude_ReturnsPeak_ExactlyAtTail()
        {
            Assert.AreEqual(0.2f, CameraShakeLogic.GetMagnitude(CameraShakeLogic.FalloffTail, 0.2f), 0.0001f);
        }

        [Test]
        public void GetMagnitude_DecaysLinearly_InsideTail()
        {
            float half = CameraShakeLogic.FalloffTail / 2f;
            Assert.AreEqual(0.1f, CameraShakeLogic.GetMagnitude(half, 0.2f), 0.0001f);
        }

        [Test]
        public void GetMagnitude_IsZero_AtZeroRemaining()
        {
            Assert.AreEqual(0f, CameraShakeLogic.GetMagnitude(0f, 0.2f), 0.0001f);
        }

        [Test]
        public void GetMagnitude_IsZero_WhenRemainingIsNegative()
        {
            Assert.AreEqual(0f, CameraShakeLogic.GetMagnitude(-0.1f, 0.2f), 0.0001f);
        }
    }
}
