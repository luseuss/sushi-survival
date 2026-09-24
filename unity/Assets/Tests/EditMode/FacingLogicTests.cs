using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Player;

namespace SushiSurvival.EditModeTests
{
    public class FacingLogicTests
    {
        [Test]
        public void ComputeFacing_ReturnsNormalizedInputDirection_WhenMoving()
        {
            var result = FacingLogic.ComputeFacing(Vector2.down, new Vector2(2f, 0f));

            Assert.AreEqual(Vector2.right, result);
        }

        [Test]
        public void ComputeFacing_KeepsPreviousDirection_WhenInputIsZero()
        {
            var result = FacingLogic.ComputeFacing(Vector2.left, Vector2.zero);

            Assert.AreEqual(Vector2.left, result);
        }

        [Test]
        public void ComputeFacing_NormalizesDiagonalInput()
        {
            var result = FacingLogic.ComputeFacing(Vector2.down, new Vector2(1f, 1f));

            Assert.That(result.magnitude, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void IsMoving_TrueForRealInput()
        {
            Assert.IsTrue(FacingLogic.IsMoving(new Vector2(0.5f, 0f)));
        }

        [Test]
        public void IsMoving_FalseForZeroInput()
        {
            Assert.IsFalse(FacingLogic.IsMoving(Vector2.zero));
        }

        [Test]
        public void IsMoving_FalseForNegligibleDrift()
        {
            Assert.IsFalse(FacingLogic.IsMoving(new Vector2(0.000001f, 0.000001f)));
        }

        [Test]
        public void IsFacingRight_TrueForPositiveX()
        {
            Assert.IsTrue(FacingLogic.IsFacingRight(Vector2.right));
        }

        [Test]
        public void IsFacingRight_FalseForNegativeX()
        {
            Assert.IsFalse(FacingLogic.IsFacingRight(Vector2.left));
        }

        // ---- HorizontalFacing ----

        [Test]
        public void HorizontalFacing_RightSide_ReturnsRight()
        {
            Assert.AreEqual(Vector2.right, FacingLogic.HorizontalFacing(new Vector2(0.3f, 0.9f)));
        }

        [Test]
        public void HorizontalFacing_LeftSide_ReturnsLeft()
        {
            Assert.AreEqual(Vector2.left, FacingLogic.HorizontalFacing(new Vector2(-0.3f, -0.9f)));
        }

        [Test]
        public void HorizontalFacing_StraightUpOrDown_MatchesVisualWhichFacesRight()
        {
            // 그림(IsFacingRight)이 x가 0일 때 오른쪽을 보므로 판정도 오른쪽이어야 어긋나지 않는다.
            Assert.AreEqual(Vector2.right, FacingLogic.HorizontalFacing(Vector2.up));
            Assert.AreEqual(Vector2.right, FacingLogic.HorizontalFacing(Vector2.down));
        }

        [Test]
        public void HorizontalFacing_AlwaysAgreesWithIsFacingRight()
        {
            for (float x = -1f; x <= 1f; x += 0.25f)
            {
                var facing = new Vector2(x, 0.5f);
                bool right = FacingLogic.IsFacingRight(facing);
                Assert.AreEqual(right ? Vector2.right : Vector2.left, FacingLogic.HorizontalFacing(facing));
            }
        }
    }
}
