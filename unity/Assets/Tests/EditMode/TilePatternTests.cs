using NUnit.Framework;
using UnityEngine;
using SushiSurvival.World;

namespace SushiSurvival.EditModeTests
{
    public class TilePatternTests
    {
        [Test]
        public void Wrap_KeepsCell_InsidePattern()
        {
            // 칠한 자리 안쪽은 칠한 그대로 나와야 한다.
            Assert.AreEqual(3, TilePattern.Wrap(3, -5, 10));
        }

        [Test]
        public void Wrap_RepeatsPattern_PastRightEdge()
        {
            // 패턴 [-5, 5) 오른쪽 끝을 넘으면 왼쪽 끝부터 다시 시작한다.
            Assert.AreEqual(-5, TilePattern.Wrap(5, -5, 10));
            Assert.AreEqual(-4, TilePattern.Wrap(16, -5, 10));
        }

        [Test]
        public void Wrap_RepeatsPattern_PastLeftEdge()
        {
            // 음수 나머지를 그대로 쓰면 패턴 밖 인덱스가 나와 배열 범위를 벗어난다.
            Assert.AreEqual(4, TilePattern.Wrap(-6, -5, 10));
            Assert.AreEqual(-5, TilePattern.Wrap(-25, -5, 10));
        }

        [Test]
        public void Wrap_ReturnsMin_WhenSizeIsZero()
        {
            Assert.AreEqual(2, TilePattern.Wrap(99, 2, 0));
        }

        [Test]
        public void IndexOf_FollowsXThenYOrder()
        {
            var bounds = new BoundsInt(-2, -1, 0, 4, 3, 1);

            Assert.AreEqual(0, TilePattern.IndexOf(-2, -1, bounds));
            Assert.AreEqual(3, TilePattern.IndexOf(1, -1, bounds));
            Assert.AreEqual(4, TilePattern.IndexOf(-2, 0, bounds));
            Assert.AreEqual(11, TilePattern.IndexOf(1, 1, bounds));
        }

        [Test]
        public void IndexOf_WrapsBothAxes_OutsidePattern()
        {
            var bounds = new BoundsInt(-2, -1, 0, 4, 3, 1);

            // (2, 2)는 한 바퀴 넘어간 (-2, -1)과 같은 칸이다.
            Assert.AreEqual(0, TilePattern.IndexOf(2, 2, bounds));
            Assert.AreEqual(11, TilePattern.IndexOf(-3, -2, bounds));
        }
    }
}
