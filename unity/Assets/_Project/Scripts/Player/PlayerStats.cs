using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.Player
{
    /// <summary>
    /// 플레이어의 모든 스탯이 지나가는 단일 창구. 증강과 호감도 대화 버프가
    /// 같은 캡 예산을 공유하므로, 클램프는 이 안의 StatSystem 한 곳에서만 한다.
    ///
    /// CharacterData를 직접 수정하지 않는 것이 중요하다 — ScriptableObject라
    /// 수정이 에셋 파일에 영구 저장되어 다음 판까지 넘어간다.
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] private CharacterData characterData;
        [Tooltip("자석 증강이 없을 때의 기본 젬 흡수 반경.")]
        [SerializeField] private float baseMagnetRange = 0.5f;

        private readonly StatSystem _stats = new StatSystem();

        private void Awake()
        {
            // 절대값 스탯 — CharacterData 값이 그대로 base가 된다.
            _stats.SetBase(StatType.MoveSpeed, characterData.baseMoveSpeed);
            _stats.SetBase(StatType.MaxHealth, characterData.baseMaxHealth);
            _stats.SetBase(StatType.MagnetRange, baseMagnetRange);

            // 배율 스탯 — 1.0이 기본이고 무기가 자기 테이블 값에 곱한다.
            _stats.SetBase(StatType.AttackDamage, 1f);
            _stats.SetBase(StatType.AttackSpeed, 1f);
            _stats.SetBase(StatType.AttackRange, 1f);
            _stats.SetBase(StatType.ExpGain, 1f);

            // 비율·횟수 스탯
            _stats.SetBase(StatType.Armor, 0f);
            _stats.SetBase(StatType.Regen, 0f);
            _stats.SetBase(StatType.Revive, 0f);

            // 하드캡 (기획서 요구사항)
            _stats.SetCap(StatType.Armor, ArmorLogic.MaxArmor);
            _stats.SetCap(StatType.AttackSpeed, 2f);
            _stats.SetCap(StatType.Revive, 1f);
        }

        public float GetValue(StatType stat) => _stats.GetValue(stat);

        public void AddModifier(StatModifier modifier) => _stats.AddModifier(modifier);
    }
}
