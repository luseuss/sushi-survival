using System.Collections.Generic;
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 보스가 착지하는 순간 초록 땅이 착지점에서부터 물결처럼 걷혀 사막 땅이 드러나는 연출의 계산.
    /// 타일을 착지점에서의 거리순으로 정렬해 두고, 반경이 커지는 만큼 앞에서부터 지운다.
    /// </summary>
    public static class GroundTransitionLogic
    {
        /// <summary>경과 시간에 따른 걷힘 반경. 일정한 속도로 퍼져 물결이 화면을 가로지르는 게 눈에 보이게 한다.</summary>
        public static float RadiusAt(float elapsed, float duration, float maxDistance)
        {
            if (maxDistance <= 0f) return 0f;
            if (duration <= 0f) return maxDistance;

            float t = Mathf.Clamp01(elapsed / duration);
            return maxDistance * t;
        }

        /// <summary>
        /// 거리 오름차순 목록에서 반경 안에 들어온 만큼 index를 앞으로 옮겨 돌려준다.
        /// index 앞의 타일은 이미 지워진 것이다.
        /// </summary>
        public static int AdvanceIndex(IReadOnlyList<float> sortedDistances, int index, float radius)
        {
            while (index < sortedDistances.Count && sortedDistances[index] <= radius)
                index++;

            return index;
        }
    }
}
