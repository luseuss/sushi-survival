using System.Collections;
using UnityEngine;
using SushiSurvival.Player;
using SushiSurvival.Enemies;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 계란 양산 — 시선 방향 부채꼴 범위 안의 적을 전부 타격한다(다중 히트).
    /// 레벨이 오르면 연타 횟수가 늘고, 정해진 레벨이 되면 LevelSystem이 회전 우산으로 바꾼다.
    /// </summary>
    public class EggFanWeapon : WeaponBase
    {
        [SerializeField] private FacingController facing;
        [SerializeField] private LayerMask enemyLayer;
        [Tooltip("레벨(1~4)별 연타 횟수. 기본: Lv1 1타, Lv2 2연타.")]
        [SerializeField] private int[] hitsByLevel = { 1, 2, 2, 2 };
        [Tooltip("공격이 시작되고 첫 타격까지의 시간(초). 공격 모션에서 첫 번째 휘두름이 가장 크게 펼쳐지는 순간에 맞춘다.")]
        [SerializeField] private float firstHitDelaySeconds;
        [Tooltip("연타 사이의 간격(초). 공격 모션의 두 번째 휘두름 시점에 맞춘다.")]
        [SerializeField] private float hitGapSeconds = 0.15f;
        [Tooltip("이 레벨이 되면 회전 우산으로 바뀐다. 같은 오브젝트에 RotatingUmbrellaWeapon이 있어야 한다.")]
        [SerializeField] private int umbrellaFromLevel = 3;

        /// <summary>양산이 우산으로 바뀔 레벨에 닿았는지.</summary>
        public bool ReadyToEvolve => UmbrellaProgressionLogic.ShouldEvolve(currentLevel, umbrellaFromLevel);

        public override string DescribeUpgrade(string statText)
        {
            int next = currentLevel + 1;
            return UmbrellaProgressionLogic.DescribeEggUpgrade(
                statText,
                UmbrellaProgressionLogic.HitCount(currentLevel, hitsByLevel),
                UmbrellaProgressionLogic.HitCount(next, hitsByLevel),
                UmbrellaProgressionLogic.ShouldEvolve(next, umbrellaFromLevel));
        }

        protected override void Attack()
        {
            StartCoroutine(SwingRoutine());
        }

        private IEnumerator SwingRoutine()
        {
            int hits = UmbrellaProgressionLogic.HitCount(currentLevel, hitsByLevel);

            for (int i = 0; i < hits; i++)
            {
                float wait = i == 0 ? firstHitDelaySeconds : hitGapSeconds;
                if (wait > 0f)
                    yield return new WaitForSeconds(wait);

                Hit();
            }
        }

        private void Hit()
        {
            float range = Range;

            // 양산 그림은 좌우로만 뒤집히므로 판정도 좌우 방향으로 맞춘다(FacingLogic.HorizontalFacing 참고).
            Vector2 aim = FacingLogic.HorizontalFacing(facing.CurrentFacing);

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, enemyLayer);
            foreach (var hit in hits)
            {
                if (!hit.TryGetComponent<EnemyBase>(out var enemy)) continue;

                if (FanHitTest.IsInsideFan(transform.position, aim, range, BaseStats.angleDegrees, enemy.transform.position))
                    enemy.TakeDamage(Damage, transform.position);
            }
        }
    }
}
