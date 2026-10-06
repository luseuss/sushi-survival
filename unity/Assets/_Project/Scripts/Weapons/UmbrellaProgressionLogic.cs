using System;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 아델린 무기 성장 규칙 — 계란 양산(Lv1~2)이 정해진 레벨에서 회전 우산으로 바뀌는 흐름의
    /// 순수 계산부. 연타 횟수, 전환 시점, 레벨업 카드에 덧붙일 문구를 만든다.
    /// </summary>
    public static class UmbrellaProgressionLogic
    {
        /// <summary>레벨(1-based)별 연타 횟수. 표가 비었으면 1회, 레벨이 표보다 크면 마지막 값.</summary>
        public static int HitCount(int level, int[] hitsByLevel)
        {
            if (hitsByLevel == null || hitsByLevel.Length == 0) return 1;

            int index = Math.Clamp(level - 1, 0, hitsByLevel.Length - 1);
            return Math.Max(1, hitsByLevel[index]);
        }

        /// <summary>이 레벨이 되면 양산을 우산으로 바꿀지.</summary>
        public static bool ShouldEvolve(int level, int evolveLevel) => level >= evolveLevel;

        /// <summary>
        /// 양산 강화 카드 문구. 다음 레벨에서 우산으로 바뀌면 수치 비교 대신 변신 안내만 보여준다
        /// (수치는 우산 데이터로 바뀌므로 양산 수치와 비교하면 틀린 정보가 된다).
        /// 연타가 늘면 수치 줄 뒤에 연타 안내를 붙인다.
        /// </summary>
        public static string DescribeEggUpgrade(string statText, int currentHits, int nextHits, bool evolvesNext)
        {
            if (evolvesNext) return "회전 우산으로 변화!\n우산이 몸 주위를 돈다";

            if (nextHits <= currentHits) return statText;

            string note = $"{nextHits}연타";
            return string.IsNullOrEmpty(statText) ? note : statText + "\n" + note;
        }

        /// <summary>우산 개수가 늘 때만 "우산 3개 → 5개" 줄을 만든다.</summary>
        public static string DescribeUmbrellaCount(string statText, int currentCount, int nextCount)
        {
            if (nextCount == currentCount) return statText;

            string note = $"우산 {currentCount}개 → {nextCount}개";
            return string.IsNullOrEmpty(statText) ? note : statText + "\n" + note;
        }
    }
}
