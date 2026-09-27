using System.Collections.Generic;
using SushiSurvival.Data;

namespace SushiSurvival.Core
{
    /// <summary>선형 대화 중 어느 줄 앞에 질문이 끼는지 찾는다.</summary>
    public static class StoryChoiceLogic
    {
        /// <summary>beforeLineIndex가 lineIndex와 같은 첫 질문의 인덱스. 없으면 -1.</summary>
        public static int FindPointIndex(IReadOnlyList<StoryChoicePoint> points, int lineIndex)
        {
            if (points == null) return -1;

            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] != null && points[i].beforeLineIndex == lineIndex)
                    return i;
            }

            return -1;
        }
    }
}
