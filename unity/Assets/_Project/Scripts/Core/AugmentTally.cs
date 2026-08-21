using System.Collections.Generic;
using SushiSurvival.Data;

namespace SushiSurvival.Core
{
    public struct AugmentCount
    {
        public AugmentData Data;
        public int Count;
    }

    public static class AugmentTally
    {
        /// <summary>
        /// 고른 증강 목록을 (데이터, 개수)로 묶는다. 같은 증강을 열 번 고르면
        /// 아이콘이 열 개 늘어서므로 결과 화면에서는 묶어서 보여준다.
        /// 처음 고른 순서를 유지한다.
        /// </summary>
        public static List<AugmentCount> Summarize(IReadOnlyList<AugmentData> picked)
        {
            var order = new List<AugmentData>();
            var counts = new Dictionary<AugmentData, int>();

            foreach (var data in picked)
            {
                if (data == null) continue;

                if (counts.TryGetValue(data, out int current))
                {
                    counts[data] = current + 1;
                }
                else
                {
                    counts[data] = 1;
                    order.Add(data);
                }
            }

            var result = new List<AugmentCount>();
            foreach (var data in order)
                result.Add(new AugmentCount { Data = data, Count = counts[data] });

            return result;
        }
    }
}
