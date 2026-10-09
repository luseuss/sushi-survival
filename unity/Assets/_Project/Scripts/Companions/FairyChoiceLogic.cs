using System;
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

        /// <summary>소환할 펫의 카탈로그 번호. 강화면 -1.</summary>
        public int KindIndex;
    }

    public static class FairyChoiceLogic
    {
        /// <summary>레벨업 카드가 세 장이라 선택지도 그 이상 만들지 않는다.</summary>
        public const int MaxChoices = 3;

        /// <summary>
        /// 빈 자리가 있으면 아직 안 가진 펫 중 무작위 최대 3종을 소환 후보로, 가득 차면 최대 레벨 미만인
        /// 요정마다 강화 후보를 돌려준다(둘은 섞지 않는다). 후보가 없으면 빈 목록.
        /// random이 null이면 섞지 않고 카탈로그 순서를 쓴다.
        /// </summary>
        public static List<FairyChoice> Build(IReadOnlyList<int> ownedKinds, IReadOnlyList<int> levels,
                                              int catalogSize, int maxCount, int maxLevel, Random random)
        {
            var choices = new List<FairyChoice>();
            int count = levels != null ? levels.Count : 0;

            if (count < maxCount)
            {
                var unowned = new List<int>();
                for (int kind = 0; kind < catalogSize; kind++)
                {
                    if (!Contains(ownedKinds, kind))
                        unowned.Add(kind);
                }

                if (random != null)
                    Shuffle(unowned, random);

                for (int i = 0; i < unowned.Count && choices.Count < MaxChoices; i++)
                    choices.Add(new FairyChoice { Kind = FairyChoiceKind.Summon, Index = -1, KindIndex = unowned[i] });

                return choices;
            }

            for (int i = 0; i < count && choices.Count < MaxChoices; i++)
            {
                if (levels[i] >= maxLevel) continue;

                choices.Add(new FairyChoice { Kind = FairyChoiceKind.Upgrade, Index = i, KindIndex = -1 });
            }

            return choices;
        }

        private static bool Contains(IReadOnlyList<int> list, int value)
        {
            if (list == null) return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == value) return true;
            }

            return false;
        }

        private static void Shuffle(List<int> list, Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
