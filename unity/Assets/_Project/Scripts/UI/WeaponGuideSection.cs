using System;
using UnityEngine;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    /// <summary>설명창에 보여줄 전용 무기 하나. 레벨업 카드와 같은 규칙으로 레벨별 강화 내용을 만든다.</summary>
    [Serializable]
    public class WeaponGuideSection
    {
        [Tooltip("목록 머리말. 예: 아델린 — 양산")]
        public string title;
        public WeaponData weapon;
        [Tooltip("이 레벨이 되면 무기가 다른 무기로 변한다(양산→우산, 소총→샷건). 0이면 변하지 않는다.")]
        public int evolveFromLevel;
        [TextArea]
        [Tooltip("변하는 레벨에 보여줄 안내. 수치 비교 대신 이 문구가 나온다.")]
        public string evolveNote;
        [Tooltip("변한 뒤 무기의 수치표. 비우면 같은 수치표를 계속 쓴다.")]
        public WeaponData evolvedWeapon;
        [Tooltip("변한 뒤 무기가 우산이면 켠다. 수치 이름이 우산용으로 바뀐다(우산 피해·재타격 간격 등).")]
        public bool evolvedIsUmbrella;
    }
}
