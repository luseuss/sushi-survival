using UnityEngine;
using SushiSurvival.Player;
using SushiSurvival.Enemies;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 계란 양산 — 시선 방향 부채꼴 범위 안의 적을 전부 타격한다(다중 히트).
    /// </summary>
    public class EggFanWeapon : WeaponBase
    {
        [SerializeField] private FacingController facing;
        [SerializeField] private LayerMask enemyLayer;

        protected override void Attack()
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
