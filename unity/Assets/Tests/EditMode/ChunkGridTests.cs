using NUnit.Framework;
using UnityEngine;
using SushiSurvival.World;

namespace SushiSurvival.EditModeTests
{
    public class ChunkGridTests
    {
        [Test]
        public void WorldToChunk_ReturnsOrigin_AtWorldZero()
        {
            var result = ChunkGrid.WorldToChunk(Vector2.zero, 16, 0.32f);

            Assert.AreEqual(new Vector2Int(0, 0), result);
        }

        [Test]
        public void WorldToChunk_StaysInFirstChunk_WithinItsExtent()
        {
            // 청크 16타일 × 0.32유닛 = 5.12유닛
            var result = ChunkGrid.WorldToChunk(new Vector2(5f, 5f), 16, 0.32f);

            Assert.AreEqual(new Vector2Int(0, 0), result);
        }

        [Test]
        public void WorldToChunk_AdvancesToNextChunk_PastExtent()
        {
            var result = ChunkGrid.WorldToChunk(new Vector2(5.5f, 0f), 16, 0.32f);

            Assert.AreEqual(new Vector2Int(1, 0), result);
        }

        [Test]
        public void WorldToChunk_HandlesNegativeWorldPositions()
        {
            // 원점 왼쪽은 -1번 청크여야 한다. 0으로 잘리면 맵이 겹쳐 보인다.
            var result = ChunkGrid.WorldToChunk(new Vector2(-0.5f, -0.5f), 16, 0.32f);

            Assert.AreEqual(new Vector2Int(-1, -1), result);
        }

        [Test]
        public void GetRequiredChunks_ReturnsOnlyCenter_AtRadiusZero()
        {
            var result = ChunkGrid.GetRequiredChunks(new Vector2Int(3, -2), 0);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(new Vector2Int(3, -2), result[0]);
        }

        [Test]
        public void GetRequiredChunks_ReturnsNineChunks_AtRadiusOne()
        {
            var result = ChunkGrid.GetRequiredChunks(Vector2Int.zero, 1);

            Assert.AreEqual(9, result.Count);
        }

        [Test]
        public void GetRequiredChunks_IncludesCenterAndCorners_AtRadiusTwo()
        {
            var result = ChunkGrid.GetRequiredChunks(Vector2Int.zero, 2);

            Assert.AreEqual(25, result.Count);
            CollectionAssert.Contains(result, Vector2Int.zero);
            CollectionAssert.Contains(result, new Vector2Int(-2, -2));
            CollectionAssert.Contains(result, new Vector2Int(2, 2));
        }
    }
}
