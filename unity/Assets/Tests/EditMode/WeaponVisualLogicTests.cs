using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Weapons;

namespace SushiSurvival.EditModeTests
{
    public class WeaponVisualLogicTests
    {
        [Test]
        public void ComputeLocalOffset_KeepsOffset_WhenFacingRight()
        {
            var result = WeaponVisualLogic.ComputeLocalOffset(new Vector2(0.5f, 0.2f), facingRight: true);

            Assert.That(result.x, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(result.y, Is.EqualTo(0.2f).Within(0.0001f));
        }

        [Test]
        public void ComputeLocalOffset_MirrorsX_WhenFacingLeft()
        {
            var result = WeaponVisualLogic.ComputeLocalOffset(new Vector2(0.5f, 0.2f), facingRight: false);

            Assert.That(result.x, Is.EqualTo(-0.5f).Within(0.0001f));
        }

        [Test]
        public void ComputeLocalOffset_LeavesYUntouched_WhenFacingLeft()
        {
            var result = WeaponVisualLogic.ComputeLocalOffset(new Vector2(0.5f, 0.2f), facingRight: false);

            Assert.That(result.y, Is.EqualTo(0.2f).Within(0.0001f));
        }

        [Test]
        public void ComputeRotationDegrees_ZeroForFacingRight()
        {
            float result = WeaponVisualLogic.ComputeRotationDegrees(Vector2.right);

            Assert.That(result, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ComputeRotationDegrees_NinetyForFacingUp()
        {
            float result = WeaponVisualLogic.ComputeRotationDegrees(Vector2.up);

            Assert.That(result, Is.EqualTo(90f).Within(0.0001f));
        }

        [Test]
        public void ComputeRotationDegrees_HandlesDiagonal()
        {
            float result = WeaponVisualLogic.ComputeRotationDegrees(new Vector2(1f, 1f));

            Assert.That(result, Is.EqualTo(45f).Within(0.0001f));
        }

        [Test]
        public void ComputeOrbitOffset_PlacesWeaponInFacingDirection()
        {
            var result = WeaponVisualLogic.ComputeOrbitOffset(Vector2.right, 0.5f);

            Assert.That(result.x, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(result.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ComputeOrbitOffset_KeepsGivenDistance()
        {
            var result = WeaponVisualLogic.ComputeOrbitOffset(new Vector2(3f, 4f), 2f);

            Assert.That(result.magnitude, Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void ShouldFlipVertically_False_WhenAimingRight()
        {
            Assert.IsFalse(WeaponVisualLogic.ShouldFlipVertically(0f));
        }

        [Test]
        public void ShouldFlipVertically_True_WhenAimingLeft()
        {
            Assert.IsTrue(WeaponVisualLogic.ShouldFlipVertically(180f));
        }

        [Test]
        public void ShouldFlipVertically_True_ForUpperLeftDiagonal()
        {
            Assert.IsTrue(WeaponVisualLogic.ShouldFlipVertically(135f));
        }

        [Test]
        public void ShouldFlipVertically_True_ForLowerLeftDiagonal()
        {
            Assert.IsTrue(WeaponVisualLogic.ShouldFlipVertically(-135f));
        }

        [Test]
        public void ShouldFlipVertically_False_ForUpperRightDiagonal()
        {
            Assert.IsFalse(WeaponVisualLogic.ShouldFlipVertically(45f));
        }

        [Test]
        public void ShouldFlipVertically_HandlesAnglesBeyondFullTurn()
        {
            // 회전각에 스프라이트 기본 방향 오프셋을 더하면 360도를 넘길 수 있다.
            Assert.IsTrue(WeaponVisualLogic.ShouldFlipVertically(360f + 180f));
        }
    }
}
