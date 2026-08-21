using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.Pickups
{
    /// <summary>
    /// 젬 등급별 풀 묶음. 몬스터는 MonsterData.xpGemDrop에 적힌 등급의 젬을 떨군다.
    /// </summary>
    public class XPGemPoolSet : MonoBehaviour
    {
        [Tooltip("흰색 밥알 — 1XP")]
        [SerializeField] private GameObjectPool basicPool;
        [Tooltip("갈색 밥알 — 5XP")]
        [SerializeField] private GameObjectPool fivePool;
        [Tooltip("황금 밥알 — 10XP")]
        [SerializeField] private GameObjectPool tenPool;

        public GameObjectPool GetPool(XPGemType type)
        {
            switch (type)
            {
                case XPGemType.Five: return fivePool;
                case XPGemType.Ten: return tenPool;
                default: return basicPool;
            }
        }
    }
}
