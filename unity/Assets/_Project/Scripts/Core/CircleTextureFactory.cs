using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 원형 스프라이트를 런타임에 만든다. 메테오 예고 마커 아트가 없어서
    /// 도입했다. 마커 아트가 생기면 SpriteRenderer의 스프라이트만 갈아끼우면
    /// 되고 이 클래스는 지워도 된다.
    ///
    /// 채움 애니메이션에 텍스처를 다시 쓰지 않는다 — 링과 원판 두 장을 만들어
    /// 원판의 스케일만 키운다. SetPixels를 매 프레임 호출하면 2페이즈에서
    /// 메테오 5발이 동시에 떠 있을 때 프레임이 떨어진다.
    /// </summary>
    public static class CircleTextureFactory
    {
        /// <summary>테두리 링의 안쪽 경계. 반지름 대비 비율.</summary>
        public const float RingInnerRatio = 0.85f;

        private const float PixelsPerUnit = 100f;

        /// <summary>텍스처 좌표를 중심 기준 정규화 거리로 바꾼다. 1이 반지름.</summary>
        public static float GetNormalizedDistance(int x, int y, int size)
        {
            float half = size * 0.5f;
            float dx = (x + 0.5f - half) / half;
            float dy = (y + 0.5f - half) / half;

            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>innerRatio가 0이면 꽉 찬 원판, 0.85면 테두리 링이 된다.</summary>
        public static bool IsInsideBand(float normalizedDistance, float innerRatio)
            => normalizedDistance <= 1f && normalizedDistance >= innerRatio;

        public static Sprite CreateSprite(int size, float innerRatio, Color color)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                // 픽셀아트 게임이므로 보간하지 않는다.
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[size * size];
            Color32 opaque = color;
            var transparent = new Color32(0, 0, 0, 0);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = GetNormalizedDistance(x, y, size);
                    pixels[y * size + x] = IsInsideBand(distance, innerRatio) ? opaque : transparent;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size),
                                 new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }
    }
}
