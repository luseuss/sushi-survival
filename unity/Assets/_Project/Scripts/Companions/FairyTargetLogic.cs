using System.Collections.Generic;
using UnityEngine;

namespace SushiSurvival.Companions
{
    public static class FairyTargetLogic
    {
        /// <summary>사거리(maxRange 이하) 안에서 가장 가까운 위치의 인덱스. 없으면 -1, 거리가 같으면 앞 인덱스.</summary>
        public static int NearestIndex(Vector2 origin, IReadOnlyList<Vector2> positions, float maxRange)
        {
            if (positions == null) return -1;

            float maxSqr = maxRange * maxRange;
            float bestSqr = float.MaxValue;
            int best = -1;

            for (int i = 0; i < positions.Count; i++)
            {
                float sqr = (positions[i] - origin).sqrMagnitude;
                if (sqr > maxSqr || sqr >= bestSqr) continue;

                bestSqr = sqr;
                best = i;
            }

            return best;
        }
    }
}
