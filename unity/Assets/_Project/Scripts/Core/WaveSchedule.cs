using System.Collections.Generic;

namespace SushiSurvival.Core
{
    public static class WaveSchedule
    {
        /// <summary>
        /// (previousTime, currentTime] 구간에 걸린 이벤트의 인덱스를 돌려준다.
        /// 시작을 열린 구간으로 두어 같은 이벤트가 두 번 발화하지 않게 하고,
        /// 끝을 닫힌 구간으로 두어 프레임 사이에 낀 이벤트를 놓치지 않게 한다.
        /// </summary>
        public static List<int> GetDueIndices(IReadOnlyList<float> eventTimes, float previousTime, float currentTime)
        {
            var due = new List<int>();

            for (int i = 0; i < eventTimes.Count; i++)
            {
                float time = eventTimes[i];
                if (time > previousTime && time <= currentTime)
                    due.Add(i);
            }

            return due;
        }
    }
}
