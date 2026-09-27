using System.Collections.Generic;
using SushiSurvival.Data;

namespace SushiSurvival.Core
{
    /// <summary>호감도 대화에서 대표 성향에 맞는 선택지를 맨 위로 올리는 순서 계산.</summary>
    public static class AffinityChoiceOrderLogic
    {
        /// <summary>대표 성향과 같은 첫 선택지의 인덱스. 없거나 성향이 None이면 -1.</summary>
        public static int RecommendedIndex(IReadOnlyList<PlayerTrait> choiceTraits, PlayerTrait dominant)
        {
            if (choiceTraits == null || dominant == PlayerTrait.None) return -1;

            for (int i = 0; i < choiceTraits.Count; i++)
            {
                if (choiceTraits[i] == dominant) return i;
            }

            return -1;
        }

        /// <summary>추천 인덱스를 맨 앞으로 옮긴 표시 순서(원래 인덱스 배열). 나머지는 원래 상대 순서를 지킨다.</summary>
        public static int[] DisplayOrder(int count, int recommendedIndex)
        {
            int size = count < 0 ? 0 : count;
            var order = new int[size];

            if (recommendedIndex < 0 || recommendedIndex >= size)
            {
                for (int i = 0; i < size; i++) order[i] = i;
                return order;
            }

            order[0] = recommendedIndex;

            int next = 1;
            for (int i = 0; i < size; i++)
            {
                if (i == recommendedIndex) continue;
                order[next++] = i;
            }

            return order;
        }
    }
}
