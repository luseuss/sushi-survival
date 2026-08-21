using UnityEngine;

namespace SushiSurvival.Data
{
    /// <summary>페이즈별로 달라지는 패턴 수치 한 벌.</summary>
    [System.Serializable]
    public struct BossPhaseValues
    {
        [Tooltip("패턴과 패턴 사이의 간격(초).")]
        public float patternInterval;
        [Tooltip("EnemyAI.MoveScale에 넣을 값. 1이 기본 이동속도.")]
        public float moveScale;

        [Header("메테오")]
        public int meteorCount;
        [Tooltip("발과 발 사이 간격(초).")]
        public float meteorSpacing;
        [Tooltip("예고 표시부터 낙하까지의 시간(초).")]
        public float meteorWarningTime;
        public float meteorDamage;
        public float meteorRadius;

        [Header("소환")]
        public int summonCount;
        [Tooltip("플레이어로부터의 소환 링 반경.")]
        public float summonRadius;
    }

    /// <summary>
    /// MonsterData를 상속하므로 EnemyBase의 monsterData 필드에 그대로 꽂힌다.
    /// 체력·접촉데미지·이동속도·넉백저항은 부모 필드를 그대로 쓰고,
    /// 여기서는 패턴 수치만 더한다.
    /// </summary>
    [CreateAssetMenu(menuName = "SushiSurvival/Boss Data", fileName = "NewBossData")]
    public class BossData : MonsterData
    {
        public BossPhaseValues phaseOne;
        public BossPhaseValues phaseTwo;

        [Range(0f, 1f)]
        [Tooltip("현재 체력 비율이 이 값 아래로 내려가면 페이즈 2로 전환한다.")]
        public float phaseTwoThreshold = 0.5f;

        public BossPhaseValues GetPhaseValues(int phase)
            => phase >= 2 ? phaseTwo : phaseOne;
    }
}
