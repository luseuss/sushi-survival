using System;
using System.Collections.Generic;
using UnityEngine;

namespace SushiSurvival.World
{
    /// <summary>
    /// 바닥 청크 스트리밍의 계획. 플레이어가 청크 경계를 넘으면 한 번에 5개 칠하고 5개 지우는 일을 한 프레임에 몰아서 하면
    /// 그 프레임이 끊긴다 — 그래서 "무엇을 칠하고 지울지"만 목록으로 만들고, 실제 작업은 프레임마다 조금씩 꺼내 처리한다.
    /// 가까운 청크부터 칠해서 화면 가까이는 먼저 채워지고, 먼 청크가 몇 프레임 늦는 것은 화면 밖이라 보이지 않는다.
    /// </summary>
    public static class ChunkStreamLogic
    {
        /// <summary>
        /// center 기준 radius 안이 필요한 청크다. 이미 칠했거나 칠할 예정인 것은 그대로 두고, 필요 없어진 예약은 취소한다.
        /// pendingFill은 가까운 순으로 정렬되고, pendingClear에는 범위를 벗어난 청크가 쌓인다(다시 범위에 들면 취소).
        /// </summary>
        public static void Plan(Vector2Int center, int radius, HashSet<Vector2Int> loaded,
                                List<Vector2Int> pendingFill, List<Vector2Int> pendingClear)
        {
            var required = new HashSet<Vector2Int>(ChunkGrid.GetRequiredChunks(center, radius));

            pendingFill.RemoveAll(chunk => !required.Contains(chunk));
            pendingClear.RemoveAll(chunk => required.Contains(chunk));

            foreach (Vector2Int chunk in required)
            {
                if (!loaded.Contains(chunk) && !pendingFill.Contains(chunk))
                    pendingFill.Add(chunk);
            }

            foreach (Vector2Int chunk in loaded)
            {
                if (!required.Contains(chunk) && !pendingClear.Contains(chunk))
                    pendingClear.Add(chunk);
            }

            pendingFill.Sort((a, b) => Distance(a, center).CompareTo(Distance(b, center)));
        }

        /// <summary>
        /// 이번 프레임에 처리할 작업. 칠할 것이 있으면 가장 가까운 것부터, 없으면 지울 것을 꺼낸다.
        /// 꺼낸 것은 목록에서 빠진다. 할 일이 없으면 false.
        /// </summary>
        public static bool TryTakeNext(List<Vector2Int> pendingFill, List<Vector2Int> pendingClear,
                                       out Vector2Int chunk, out bool isFill)
        {
            if (pendingFill.Count > 0)
            {
                chunk = pendingFill[0];
                pendingFill.RemoveAt(0);
                isFill = true;
                return true;
            }

            if (pendingClear.Count > 0)
            {
                chunk = pendingClear[0];
                pendingClear.RemoveAt(0);
                isFill = false;
                return true;
            }

            chunk = default;
            isFill = false;
            return false;
        }

        // 체비쇼프 거리(칸 단위 최대 차이)가 같으면 맨해튼 거리로 가른다 — 정면 청크가 모서리보다 먼저 칠해진다.
        private static int Distance(Vector2Int chunk, Vector2Int center)
        {
            int dx = Math.Abs(chunk.x - center.x);
            int dy = Math.Abs(chunk.y - center.y);
            return Math.Max(dx, dy) * 100 + dx + dy;
        }
    }
}
