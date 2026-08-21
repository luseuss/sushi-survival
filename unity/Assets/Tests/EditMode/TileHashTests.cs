using NUnit.Framework;
using SushiSurvival.World;

namespace SushiSurvival.EditModeTests
{
    public class TileHashTests
    {
        [Test]
        public void Hash_IsDeterministic_ForSameInput()
        {
            uint first = TileHash.Hash(12, -7, 999);
            uint second = TileHash.Hash(12, -7, 999);

            Assert.AreEqual(first, second);
        }

        [Test]
        public void Hash_DiffersForNeighbouringCoordinates()
        {
            // 인접 좌표가 같은 값이면 바닥이 줄무늬처럼 보인다.
            uint a = TileHash.Hash(10, 10, 1);
            uint b = TileHash.Hash(11, 10, 1);
            uint c = TileHash.Hash(10, 11, 1);

            Assert.AreNotEqual(a, b);
            Assert.AreNotEqual(a, c);
            Assert.AreNotEqual(b, c);
        }

        [Test]
        public void Hash_DiffersForDifferentSeeds()
        {
            Assert.AreNotEqual(TileHash.Hash(5, 5, 1), TileHash.Hash(5, 5, 2));
        }

        [Test]
        public void Hash_HandlesNegativeCoordinates()
        {
            // 플레이어가 원점 왼쪽·아래로 가면 음수 좌표가 나온다.
            Assert.DoesNotThrow(() => TileHash.Hash(-1000, -1000, 7));
            Assert.AreEqual(TileHash.Hash(-3, -4, 7), TileHash.Hash(-3, -4, 7));
        }

        [Test]
        public void Normalized_StaysWithinZeroToOne()
        {
            for (int x = -50; x <= 50; x += 7)
            {
                float value = TileHash.Normalized(x, x * 3, 42);
                Assert.GreaterOrEqual(value, 0f);
                Assert.LessOrEqual(value, 1f);
            }
        }

        [Test]
        public void Index_StaysWithinRange()
        {
            for (int x = -50; x <= 50; x += 3)
            {
                int index = TileHash.Index(x, -x, 42, 16);
                Assert.GreaterOrEqual(index, 0);
                Assert.Less(index, 16);
            }
        }

        [Test]
        public void Index_ReturnsZero_WhenCountIsZero()
        {
            // 스프라이트 배열이 비어 있어도 0으로 나누지 않는다.
            Assert.AreEqual(0, TileHash.Index(3, 4, 5, 0));
        }
    }
}
