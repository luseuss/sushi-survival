using System;
using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.Player
{
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerHealth : MonoBehaviour
    {
        private PlayerStats _stats;
        private float _regenCarry;
        private int _revivesUsed;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => _stats.GetValue(StatType.MaxHealth);

        public event Action OnDeath;
        /// <summary>(현재 체력, 최대 체력) — 체력바가 구독한다.</summary>
        public event Action<float, float> OnHealthChanged;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
        }

        private void Start()
        {
            CurrentHealth = MaxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        private void Update()
        {
            TickRegen();
        }

        /// <summary>
        /// 최대체력 증강을 얻었을 때 현재 체력도 같이 올린다. 그러지 않으면
        /// 최대치만 늘고 체감상 아무 일도 일어나지 않는다.
        /// </summary>
        public void GrantMaxHealthIncrease(float amount)
        {
            CurrentHealth += amount;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void TakeDamage(float damage)
        {
            if (HealthLogic.IsDead(CurrentHealth)) return;

            float reduced = ArmorLogic.ApplyArmor(damage, _stats.GetValue(StatType.Armor));
            CurrentHealth = HealthLogic.ApplyDamage(CurrentHealth, reduced);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (!HealthLogic.IsDead(CurrentHealth)) return;

            if (TryRevive()) return;

            OnDeath?.Invoke();
        }

        private bool TryRevive()
        {
            int allowedRevives = Mathf.FloorToInt(_stats.GetValue(StatType.Revive));
            if (_revivesUsed >= allowedRevives) return false;

            _revivesUsed++;
            CurrentHealth = MaxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            Debug.Log($"[PlayerHealth] 부활! ({_revivesUsed}/{allowedRevives})");
            return true;
        }

        private void TickRegen()
        {
            if (HealthLogic.IsDead(CurrentHealth)) return;

            float regenPerSecond = _stats.GetValue(StatType.Regen);
            if (regenPerSecond <= 0f) return;

            // 초당 회복량이 작아 프레임당 값이 0에 가까우므로 누적해서 적용한다.
            _regenCarry += regenPerSecond * Time.deltaTime;
            if (_regenCarry < 0.01f) return;

            CurrentHealth = Mathf.Min(CurrentHealth + _regenCarry, MaxHealth);
            _regenCarry = 0f;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }
    }
}
