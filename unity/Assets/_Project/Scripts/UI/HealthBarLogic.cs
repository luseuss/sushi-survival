using UnityEngine;

namespace SushiSurvival.UI
{
    public static class HealthBarLogic
    {
        public static float ComputeFillAmount(float current, float max)
        {
            if (max <= 0f) return 0f;

            return Mathf.Clamp01(current / max);
        }

        /// <summary>current를 target 쪽으로 maxDelta만큼만 옮긴다. 스냅 대신
        /// 부드럽게 줄어드는 체력바에 쓴다.</summary>
        public static float MoveTowardsFill(float current, float target, float maxDelta)
        {
            return Mathf.MoveTowards(Mathf.Clamp01(current), Mathf.Clamp01(target), Mathf.Max(0f, maxDelta));
        }

        /// <summary>
        /// 체력 비율(0~1)을 채움 이미지의 fillAmount로 바꾼다. 화면 전체 크기의 이미지에 막대가 일부 구간에만 그려져
        /// 있으면, fillAmount를 그 구간(start~end, 이미지 너비 기준 0~1)에 맞춰야 체력 0%에서 막대가 비고 100%에서 꽉 찬다.
        /// </summary>
        public static float MapFill(float ratio, float rangeStart, float rangeEnd)
        {
            return Mathf.Lerp(rangeStart, rangeEnd, Mathf.Clamp01(ratio));
        }

        /// <summary>
        /// 픽셀 배열에서 불투명한 부분이 가로로 차지하는 구간을 이미지 너비 기준(0~1)으로 찾는다. 불투명한 픽셀이
        /// 하나도 없으면 false. 막대 그림이 화면 전체 크기 이미지의 일부에만 있을 때 채움 범위를 자동으로 잡는 데 쓴다.
        /// </summary>
        public static bool TryFindOpaqueRangeX(Color32[] pixels, int width, int height, byte alphaThreshold,
                                               out float rangeStart, out float rangeEnd)
        {
            rangeStart = 0f;
            rangeEnd = 1f;
            if (pixels == null || width <= 0 || height <= 0 || pixels.Length < width * height) return false;

            int minX = width;
            int maxX = -1;

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    if (pixels[row + x].a <= alphaThreshold) continue;

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                }
            }

            if (maxX < 0) return false;

            rangeStart = (float)minX / width;
            rangeEnd = (float)(maxX + 1) / width;
            return true;
        }
    }
}
