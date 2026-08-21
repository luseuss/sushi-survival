using NUnit.Framework;
using SushiSurvival.World;

namespace SushiSurvival.EditModeTests
{
    /// <summary>
    /// 해시가 한쪽 축으로 상관관계를 가지면 바닥에 줄무늬가 생긴다.
    /// 눈으로 보기 전에 수치로 잡아내기 위한 검사.
    /// </summary>
    public class TileHashDistributionTests
    {
        private const int Count = 4;
        private const int Span = 200;
        private const float Expected = 1f / Count;
        private const float Tolerance = 0.06f;

        [Test]
        public void Index_DoesNotRepeatAlongX_MoreThanChance()
        {
            int same = 0;
            int total = 0;

            for (int y = -Span / 2; y < Span / 2; y++)
            {
                for (int x = -Span / 2; x < Span / 2 - 1; x++)
                {
                    if (TileHash.Index(x, y, 12345, Count) == TileHash.Index(x + 1, y, 12345, Count))
                        same++;
                    total++;
                }
            }

            float rate = same / (float)total;
            Assert.That(rate, Is.EqualTo(Expected).Within(Tolerance),
                $"가로로 인접한 타일이 같을 확률이 {rate:P1} — 줄무늬가 생긴다는 뜻이다.");
        }

        [Test]
        public void Index_DoesNotRepeatAlongY_MoreThanChance()
        {
            int same = 0;
            int total = 0;

            for (int y = -Span / 2; y < Span / 2 - 1; y++)
            {
                for (int x = -Span / 2; x < Span / 2; x++)
                {
                    if (TileHash.Index(x, y, 12345, Count) == TileHash.Index(x, y + 1, 12345, Count))
                        same++;
                    total++;
                }
            }

            float rate = same / (float)total;
            Assert.That(rate, Is.EqualTo(Expected).Within(Tolerance),
                $"세로로 인접한 타일이 같을 확률이 {rate:P1} — 줄무늬가 생긴다는 뜻이다.");
        }

        [Test]
        public void Index_UsesEveryValueRoughlyEqually()
        {
            var counts = new int[Count];

            for (int y = -Span / 2; y < Span / 2; y++)
                for (int x = -Span / 2; x < Span / 2; x++)
                    counts[TileHash.Index(x, y, 12345, Count)]++;

            int total = Span * Span;
            for (int i = 0; i < Count; i++)
            {
                float share = counts[i] / (float)total;
                Assert.That(share, Is.EqualTo(Expected).Within(Tolerance),
                    $"{i}번 타일이 전체의 {share:P1}를 차지한다 — 특정 무늬만 자주 나온다는 뜻이다.");
            }
        }
    }
}
