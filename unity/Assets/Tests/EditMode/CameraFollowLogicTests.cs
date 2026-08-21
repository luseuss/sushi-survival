using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class CameraFollowLogicTests
    {
        [Test]
        public void ComputeFollowPosition_KeepsCameraZ_IgnoringTargetZ()
        {
            var camera = new Vector3(0f, 0f, -10f);
            var target = new Vector3(5f, 3f, 0f);

            var result = CameraFollowLogic.ComputeFollowPosition(camera, target, 1f);

            Assert.AreEqual(-10f, result.z);
        }

        [Test]
        public void ComputeFollowPosition_SnapsToTargetXY_WhenFactorIsOne()
        {
            var camera = new Vector3(0f, 0f, -10f);
            var target = new Vector3(5f, 3f, 0f);

            var result = CameraFollowLogic.ComputeFollowPosition(camera, target, 1f);

            Assert.That(result.x, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(result.y, Is.EqualTo(3f).Within(0.0001f));
        }

        [Test]
        public void ComputeFollowPosition_StaysPut_WhenFactorIsZero()
        {
            var camera = new Vector3(1f, 2f, -10f);
            var target = new Vector3(5f, 3f, 0f);

            var result = CameraFollowLogic.ComputeFollowPosition(camera, target, 0f);

            Assert.That(result.x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(result.y, Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void ComputeFollowPosition_MovesHalfway_WhenFactorIsHalf()
        {
            var camera = new Vector3(0f, 0f, -10f);
            var target = new Vector3(10f, 4f, 0f);

            var result = CameraFollowLogic.ComputeFollowPosition(camera, target, 0.5f);

            Assert.That(result.x, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(result.y, Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void ComputeFollowPosition_DoesNotOvershoot_WhenFactorExceedsOne()
        {
            var camera = new Vector3(0f, 0f, -10f);
            var target = new Vector3(10f, 4f, 0f);

            var result = CameraFollowLogic.ComputeFollowPosition(camera, target, 2.5f);

            Assert.That(result.x, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(result.y, Is.EqualTo(4f).Within(0.0001f));
        }
    }
}
