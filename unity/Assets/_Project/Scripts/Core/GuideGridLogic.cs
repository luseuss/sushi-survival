using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>설명창 칸 격자의 계산 — 칸 위치와 방향키 이동. 좌표는 두루마리 한가운데가 (0,0), 위쪽이 +y다.</summary>
    public static class GuideGridLogic
    {
        public const int Columns = 4;
        public const int Rows = 4;
        public const int Capacity = Columns * Rows;

        /// <summary>index번 칸(왼쪽 위가 0, 가로로 채움)의 중심. firstCenter는 0번 칸의 중심, pitch는 칸 사이 간격(가로·세로)이다.</summary>
        public static Vector2 SlotCenter(int index, Vector2 firstCenter, Vector2 pitch, int columns)
        {
            if (columns <= 0) return firstCenter;

            int column = index % columns;
            int row = index / columns;
            return new Vector2(firstCenter.x + column * pitch.x, firstCenter.y - row * pitch.y);
        }

        /// <summary>
        /// 방향키 한 칸 이동. 가장자리를 넘어가려 하거나 도착한 칸이 비어 있으면 그대로 둔다.
        /// 선택이 없거나(−1) 범위 밖이면 첫 칸으로 간다. 칸이 하나도 없으면 −1.
        /// </summary>
        public static int Move(int current, int dx, int dy, int columns, int rows, int count)
        {
            if (count <= 0) return -1;
            if (current < 0 || current >= count) return 0;

            int column = Mathf.Clamp(current % columns + dx, 0, columns - 1);
            int row = Mathf.Clamp(current / columns + dy, 0, rows - 1);
            int target = row * columns + column;

            return target < count ? target : current;
        }
    }
}
