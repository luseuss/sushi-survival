using UnityEngine;

namespace SushiSurvival.Data
{
    /// <summary>
    /// 펫(요정) 한 종. 그림(프레임·재생 속도·바라보는 방향·크기)과 레벨 1~4 공격 수치를 한 에셋에 담는다.
    /// damage/cooldown/range(사거리)/pierceCount만 쓰고 angleDegrees는 쓰지 않는다.
    /// </summary>
    [CreateAssetMenu(menuName = "SushiSurvival/Fairy Data", fileName = "NewFairyData")]
    public class FairyData : ScriptableObject
    {
        public string displayName;
        [Tooltip("소환 카드 설명 첫 줄(펫의 역할). 예: 고화력 느림")]
        [TextArea] public string roleLine;
        [Tooltip("대기 애니메이션 프레임. 시트를 슬라이스한 순서대로.")]
        public Sprite[] frames;
        public float framesPerSecond = 8f;
        [Tooltip("그림이 오른쪽을 보고 있으면 체크. 안 하면 왼쪽을 본다고 보고, 오른쪽으로 갈 때 뒤집는다.")]
        public bool facesRight;
        [Tooltip("그림이 작을 때 키우는 배율(25px 프레임은 월드 0.25유닛).")]
        public float visualScale = 1f;
        [Tooltip("인덱스 0 = Lv1 ... 인덱스 3 = Lv4(MAX)")]
        public WeaponLevelStats[] levels = new WeaponLevelStats[4];
    }
}
