using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SushiSurvival.World;

namespace SushiSurvival.EditModeTests
{
    public class ChunkStreamLogicTests
    {
        private static void Plan(Vector2Int center, HashSet<Vector2Int> loaded, List<Vector2Int> fill, List<Vector2Int> clear, int radius = 2)
            => ChunkStreamLogic.Plan(center, radius, loaded, fill, clear);

        [Test]
        public void FirstPlan_QueuesEveryRequiredChunk_NearestFirst()
        {
            var fill = new List<Vector2Int>();
            var clear = new List<Vector2Int>();

            Plan(Vector2Int.zero, new HashSet<Vector2Int>(), fill, clear);

            Assert.AreEqual(25, fill.Count);
            Assert.AreEqual(Vector2Int.zero, fill[0]);
            Assert.IsEmpty(clear);
            // 가장 먼 모서리는 맨 뒤쪽이다.
            Assert.AreEqual(2, Mathf.Max(Mathf.Abs(fill[24].x), Mathf.Abs(fill[24].y)));
        }

        [Test]
        public void MovingOneChunk_QueuesFiveFillsAndFiveClears()
        {
            var loaded = new HashSet<Vector2Int>(ChunkGrid.GetRequiredChunks(Vector2Int.zero, 2));
            var fill = new List<Vector2Int>();
            var clear = new List<Vector2Int>();

            Plan(new Vector2Int(1, 0), loaded, fill, clear);

            Assert.AreEqual(5, fill.Count);
            Assert.AreEqual(5, clear.Count);
            foreach (Vector2Int chunk in fill) Assert.AreEqual(3, chunk.x);
            foreach (Vector2Int chunk in clear) Assert.AreEqual(-2, chunk.x);
        }

        [Test]
        public void StandingStill_QueuesNothing()
        {
            var loaded = new HashSet<Vector2Int>(ChunkGrid.GetRequiredChunks(Vector2Int.zero, 2));
            var fill = new List<Vector2Int>();
            var clear = new List<Vector2Int>();

            Plan(Vector2Int.zero, loaded, fill, clear);

            Assert.IsEmpty(fill);
            Assert.IsEmpty(clear);
        }

        [Test]
        public void ChangingDirectionBeforeWorkFinishes_CancelsStaleFills_AndDoesNotDuplicate()
        {
            var loaded = new HashSet<Vector2Int>(ChunkGrid.GetRequiredChunks(Vector2Int.zero, 2));
            var fill = new List<Vector2Int>();
            var clear = new List<Vector2Int>();

            Plan(new Vector2Int(1, 0), loaded, fill, clear);   // 오른쪽으로 → x=3 열을 칠할 예정
            Plan(Vector2Int.zero, loaded, fill, clear);         // 곧바로 돌아옴 → 예약 전부 취소

            Assert.IsEmpty(fill);
            Assert.IsEmpty(clear);

            Plan(new Vector2Int(1, 0), loaded, fill, clear);
            Plan(new Vector2Int(1, 0), loaded, fill, clear);    // 같은 자리에서 또 불려도 중복 없음
            Assert.AreEqual(5, fill.Count);
            Assert.AreEqual(5, clear.Count);
        }

        [Test]
        public void TryTakeNext_FillsFirstThenClears_ThenReportsEmpty()
        {
            var fill = new List<Vector2Int> { new Vector2Int(1, 1), new Vector2Int(2, 2) };
            var clear = new List<Vector2Int> { new Vector2Int(-1, -1) };

            Assert.IsTrue(ChunkStreamLogic.TryTakeNext(fill, clear, out Vector2Int a, out bool aFill));
            Assert.AreEqual(new Vector2Int(1, 1), a);
            Assert.IsTrue(aFill);

            Assert.IsTrue(ChunkStreamLogic.TryTakeNext(fill, clear, out Vector2Int b, out bool bFill));
            Assert.AreEqual(new Vector2Int(2, 2), b);
            Assert.IsTrue(bFill);

            Assert.IsTrue(ChunkStreamLogic.TryTakeNext(fill, clear, out Vector2Int c, out bool cFill));
            Assert.AreEqual(new Vector2Int(-1, -1), c);
            Assert.IsFalse(cFill);

            Assert.IsFalse(ChunkStreamLogic.TryTakeNext(fill, clear, out _, out _));
        }
    }
}
