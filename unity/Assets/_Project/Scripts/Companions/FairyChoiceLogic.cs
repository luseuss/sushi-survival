using System.Collections.Generic;

namespace SushiSurvival.Companions
{
    public enum FairyChoiceKind
    {
        Summon,
        Upgrade
    }

    public struct FairyChoice
    {
        public FairyChoiceKind Kind;

        /// <summary>강화할 요정의 번호(0부터). 소환이면 -1.</summary>
        public int Index;
    }

    public static class FairyChoiceLogic
    {
        /// <summary>레벨업 카드가 세 장이라 선택지도 그 이상 만들지 않는다.</summary>
        public const int MaxChoices = 3;

        /// <summary>소환 가능하면 Summon이 맨 앞, 이어서 최대 레벨 미만인 요정마다 Upgrade. 없으면 빈 목록.</summary>
        public static List<FairyChoice> Build(IReadOnlyList<int> levels, int maxCount, int maxLevel)
        {
            var choices = new List<FairyChoice>();
            int count = levels != null ? levels.Count : 0;

            if (count < maxCount)
                choices.Add(new FairyChoice { Kind = FairyChoiceKind.Summon, Index = -1 });

            for (int i = 0; i < count && choices.Count < MaxChoices; i++)
            {
                if (levels[i] >= maxLevel) continue;

                choices.Add(new FairyChoice { Kind = FairyChoiceKind.Upgrade, Index = i });
            }

            return choices;
        }
    }
}
