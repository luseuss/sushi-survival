using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;
using SushiSurvival.Player;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 무기 공통부 — 쿨타임 타이머, 레벨별 수치 조회, 공격 애니메이션 트리거,
    /// 증강 배율 적용. 각 무기는 Attack()만 구현한다.
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        [SerializeField] protected WeaponData weaponData;
        [Tooltip("계란·간장새우는 무기 오브젝트(WeaponVisual)의 것을, 이나리는 캐릭터 본체의 것을 연결한다.")]
        [SerializeField] protected AttackAnimator attackAnimator;
        [Tooltip("1-based (1~4)")]
        [SerializeField] protected int currentLevel = 1;
        [Tooltip("공격속도 증강이 아무리 쌓여도 이 값보다 짧아지지 않는다(무한 연사 방지).")]
        [SerializeField] protected float minCooldown = 0.2f;
        [SerializeField] protected PlayerStats playerStats;

        private readonly WeaponCooldown _cooldown = new WeaponCooldown();

        public int CurrentLevel => currentLevel;
        public string WeaponName => weaponData != null ? weaponData.weaponName : string.Empty;
        public bool CanLevelUp => weaponData != null && currentLevel < weaponData.levels.Length;

        protected WeaponLevelStats BaseStats => weaponData.levels[currentLevel - 1];

        /// <summary>증강 배율이 적용된 최종 데미지.</summary>
        protected float Damage => BaseStats.damage * StatMultiplier(StatType.AttackDamage);

        /// <summary>증강 배율이 적용된 최종 사거리/반경.</summary>
        protected float Range => BaseStats.range * StatMultiplier(StatType.AttackRange);

        /// <summary>
        /// 다음 레벨과의 수치 비교용. 이미 최대 레벨이면 false. 강화 카드가 "공격력 8 → 10"처럼 무엇이 얼마나
        /// 바뀌는지 적는 데 쓴다.
        /// </summary>
        public bool TryGetNextLevelStats(out WeaponLevelStats current, out WeaponLevelStats next)
        {
            current = default;
            next = default;

            if (weaponData == null || !CanLevelUp) return false;

            current = weaponData.levels[currentLevel - 1];
            next = weaponData.levels[currentLevel];
            return true;
        }

        public void LevelUp()
        {
            if (!CanLevelUp) return;
            currentLevel++;
        }

        /// <summary>레벨을 직접 지정한다(승계용). 1~weaponData.levels.Length로 클램프한다.</summary>
        public void SetLevel(int level)
        {
            if (weaponData == null) return;
            currentLevel = Mathf.Clamp(level, 1, weaponData.levels.Length);
        }

        protected float StatMultiplier(StatType stat)
            => playerStats != null ? playerStats.GetValue(stat) : 1f;

        protected virtual void Update()
        {
            _cooldown.Tick(Time.deltaTime);
            if (!_cooldown.IsReady) return;

            attackAnimator?.TriggerAttack();
            Attack();

            float cooldown = CooldownLogic.ApplyAttackSpeed(
                BaseStats.cooldown, StatMultiplier(StatType.AttackSpeed), minCooldown);
            _cooldown.Reset(cooldown);
        }

        protected abstract void Attack();
    }
}
