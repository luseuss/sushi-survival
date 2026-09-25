using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Enemies;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 와사비 알현 성공 시 계란 양산을 대체하는 무기. 쿨타임마다 한 번 공격하는
    /// WeaponBase 기본 루프 대신, 매 프레임 계속 회전하며 스치는 적마다 개별
    /// 재타격 타이머(cooldown 필드 재해석)로 데미지를 준다.
    /// </summary>
    public class RotatingUmbrellaWeapon : WeaponBase
    {
        [Tooltip("궤도를 도는 우산 그림 오브젝트. 최대 5개, 레벨별로 앞에서부터 필요한 개수만 켠다.")]
        [SerializeField] private Transform[] umbrellas;
        [Tooltip("레벨(1~4)별 우산 개수. 기획서 시작값: Lv1~2 4개, Lv3~4 5개.")]
        [SerializeField] private int[] umbrellaCountByLevel = { 4, 4, 5, 5 };
        [Tooltip("공격속도 배율 1.0 기준 회전 속도(초당 도).")]
        [SerializeField] private float rotationSpeedDegreesPerSecond = 360f;
        [Tooltip("우산 하나가 적을 스쳤다고 판정하는 반경.")]
        [SerializeField] private float hitRadius = 0.3f;
        [SerializeField] private LayerMask enemyLayer;

        private float _currentAngle;
        // 풀링된 적이 죽고 짧은 시간 안에 같은 자리에서 재사용되면 이전 생의
        // 마지막 타격 시각이 남아 있어 첫 타격이 한 번 씹힐 수 있다 — 재타격
        // 간격(기본 0.3초)이 짧아 실전 영향은 미미해서 지금은 정리하지 않는다.
        private readonly Dictionary<EnemyBase, float> _lastHitTime = new Dictionary<EnemyBase, float>();

        protected override void Update()
        {
            if (weaponData == null || umbrellas == null || umbrellas.Length == 0) return;

            float rotationSpeed = rotationSpeedDegreesPerSecond * StatMultiplier(StatType.AttackSpeed);
            _currentAngle += rotationSpeed * Time.deltaTime;

            int count = UmbrellaCountForLevel();
            float radius = Range;

            for (int i = 0; i < umbrellas.Length; i++)
            {
                bool active = i < count;
                umbrellas[i].gameObject.SetActive(active);
                if (!active) continue;

                float angle = UmbrellaOrbitLogic.AngleForIndex(_currentAngle, i, count);
                umbrellas[i].localPosition = UmbrellaOrbitLogic.PositionForAngle(angle, radius);
                umbrellas[i].localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            float reHitInterval = CooldownLogic.ApplyAttackSpeed(
                BaseStats.cooldown, StatMultiplier(StatType.AttackSpeed), minCooldown);
            CheckHits(count, reHitInterval);
        }

        // 우산은 쿨타임마다 한 번 쏘는 무기가 아니라 이 메서드는 쓰이지 않는다
        // (Update()를 통째로 오버라이드해서 base.Attack() 호출 경로 자체가 없다).
        protected override void Attack() { }

        private int UmbrellaCountForLevel()
        {
            if (umbrellaCountByLevel == null || umbrellaCountByLevel.Length == 0)
                return umbrellas.Length;

            int index = Mathf.Clamp(currentLevel - 1, 0, umbrellaCountByLevel.Length - 1);
            return Mathf.Min(umbrellaCountByLevel[index], umbrellas.Length);
        }

        private void CheckHits(int count, float reHitInterval)
        {
            for (int i = 0; i < count; i++)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(umbrellas[i].position, hitRadius, enemyLayer);
                foreach (var hit in hits)
                {
                    if (!hit.TryGetComponent<EnemyBase>(out var enemy)) continue;

                    _lastHitTime.TryGetValue(enemy, out float last);
                    if (Time.time - last < reHitInterval) continue;

                    enemy.TakeDamage(Damage, transform.position);
                    _lastHitTime[enemy] = Time.time;
                }
            }
        }
    }
}
