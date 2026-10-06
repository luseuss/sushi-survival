using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Weapons;

namespace SushiSurvival.EditModeTests
{
    public class ShotgunSpreadLogicTests
    {
        [Test]
        public void OffsetDegrees_SinglePellet_GoesStraight()
        {
            Assert.AreEqual(0f, ShotgunSpreadLogic.OffsetDegrees(0, 1, 40f), 0.001f);
        }

        [Test]
        public void OffsetDegrees_SpreadsEvenlyAroundCenter()
        {
            // 5발 40° → -20, -10, 0, 10, 20
            Assert.AreEqual(-20f, ShotgunSpreadLogic.OffsetDegrees(0, 5, 40f), 0.001f);
            Assert.AreEqual(-10f, ShotgunSpreadLogic.OffsetDegrees(1, 5, 40f), 0.001f);
            Assert.AreEqual(0f, ShotgunSpreadLogic.OffsetDegrees(2, 5, 40f), 0.001f);
            Assert.AreEqual(20f, ShotgunSpreadLogic.OffsetDegrees(4, 5, 40f), 0.001f);
        }

        [Test]
        public void OffsetDegrees_EvenCount_HasNoCenterPellet()
        {
            // 4발 30° → -15, -5, 5, 15
            Assert.AreEqual(-5f, ShotgunSpreadLogic.OffsetDegrees(1, 4, 30f), 0.001f);
            Assert.AreEqual(5f, ShotgunSpreadLogic.OffsetDegrees(2, 4, 30f), 0.001f);
        }

        [Test]
        public void Rotate_NinetyDegrees_TurnsRightIntoUp()
        {
            Vector2 result = ShotgunSpreadLogic.Rotate(Vector2.right, 90f);

            Assert.AreEqual(0f, result.x, 0.001f);
            Assert.AreEqual(1f, result.y, 0.001f);
        }

        [Test]
        public void Rotate_KeepsLength()
        {
            Vector2 result = ShotgunSpreadLogic.Rotate(new Vector2(0.6f, 0.8f), 33f);

            Assert.AreEqual(1f, result.magnitude, 0.001f);
        }
    }
}
