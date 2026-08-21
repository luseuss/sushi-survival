using System.Collections.Generic;
using UnityEngine;

namespace SushiSurvival.Enemies.Boss
{
    /// <summary>
    /// 플레이어를 둘러싸는 링 위에 소환 위치를 균등 배치한다. 무작위로 흩뿌리면
    /// 한쪽에 뭉쳐 나와 반대편으로 걸어나가면 그만인 패턴이 된다.
    /// </summary>
    public static class SummonPlacement
    {
        public static List<Vector2> GetPositions(Vector2 center, int count, float radius, float startAngleRad)
        {
            var positions = new List<Vector2>();
            if (count <= 0) return positions;

            float step = Mathf.PI * 2f / count;

            for (int i = 0; i < count; i++)
                positions.Add(SpawnRingUtility.GetPositionOnRing(center, radius, startAngleRad + step * i));

            return positions;
        }
    }
}
