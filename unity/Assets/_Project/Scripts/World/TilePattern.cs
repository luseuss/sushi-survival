using UnityEngine;

namespace SushiSurvival.World
{
    /// <summary>
    /// 손으로 칠한 타일 영역을 바둑판처럼 반복시키는 좌표 계산.
    /// 칠한 원래 자리에서는 칠한 그대로 나오고, 그 바깥은 같은 무늬가 이어진다.
    /// </summary>
    public static class TilePattern
    {
        /// <summary>임의의 칸 좌표를 패턴 영역 [min, min + size) 안의 좌표로 접는다.</summary>
        public static int Wrap(int cell, int min, int size)
        {
            if (size <= 0) return min;

            int offset = (cell - min) % size;
            if (offset < 0) offset += size;

            return min + offset;
        }

        /// <summary>
        /// 칸 좌표가 GetTilesBlock 결과 배열에서 몇 번째인지 돌려준다.
        /// GetTilesBlock은 x가 먼저 돌고 그다음 y가 도는 순서다.
        /// </summary>
        public static int IndexOf(int cellX, int cellY, BoundsInt bounds)
        {
            int x = Wrap(cellX, bounds.xMin, bounds.size.x) - bounds.xMin;
            int y = Wrap(cellY, bounds.yMin, bounds.size.y) - bounds.yMin;

            return y * bounds.size.x + x;
        }
    }
}
