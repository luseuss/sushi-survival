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
    }
}
