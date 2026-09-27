using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Player;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 간장 소총 — 시선 방향으로 투사체를 발사한다. 자동 조준은 없다(기획서).
    /// </summary>
    public class ShrimpRifleWeapon : WeaponBase
    {
        [SerializeField] private FacingController facing;
        [Tooltip("투사체가 나가는 위치. 비워두면 이 오브젝트 위치에서 발사한다.")]
        [SerializeField] private Transform muzzle;

        [Header("샷건 (와사비 알현 보상)")]
        [Tooltip("샷건 모드에서 한 번에 나가는 산탄 수.")]
        [SerializeField] private int shotgunPelletCount = 5;
        [Tooltip("산탄이 퍼지는 전체 각도(도).")]
        [SerializeField] private float shotgunSpreadDegrees = 40f;
        [Tooltip("산탄 한 발의 피해 = 소총 피해 × 이 값. 다 맞으면 소총 한 발보다 세지만 멀면 몇 발만 맞는다.")]
        [SerializeField] private float shotgunDamageRatio = 0.5f;

        private GameObjectPool _projectilePool;

        /// <summary>와사비 알현 성공으로 샷건으로 바뀐 상태인지. 보스 씬 이월에도 쓴다.</summary>
        public bool IsShotgun { get; private set; }

        public void EnableShotgun() => IsShotgun = true;

        /// <summary>
        /// PlayerSpawner가 스폰 직후 주입한다. 프리팹 에셋은 씬에만 존재하는
        /// 풀을 Inspector로 직접 참조할 수 없기 때문.
        /// </summary>
        public void SetProjectilePool(GameObjectPool pool) => _projectilePool = pool;

        protected override void Attack()
        {
            if (_projectilePool == null)
            {
                Debug.LogError($"{name}: projectilePool이 주입되지 않아 발사할 수 없습니다.");
                return;
            }

            Vector2 direction = facing.CurrentFacing;
            Vector3 spawnPos = muzzle != null ? muzzle.position : transform.position;

            if (!IsShotgun)
            {
                Fire(spawnPos, direction, Damage);
                return;
            }

            int pellets = Mathf.Max(1, shotgunPelletCount);
            float pelletDamage = Damage * shotgunDamageRatio;

            for (int i = 0; i < pellets; i++)
            {
                float offset = ShotgunSpreadLogic.OffsetDegrees(i, pellets, shotgunSpreadDegrees);
                Fire(spawnPos, ShotgunSpreadLogic.Rotate(direction, offset), pelletDamage);
            }
        }

        private void Fire(Vector3 spawnPos, Vector2 direction, float damage)
        {
            float rotation = WeaponVisualLogic.ComputeRotationDegrees(direction);

            GameObject projectileObj = _projectilePool.Get(spawnPos, Quaternion.Euler(0f, 0f, rotation));

            if (projectileObj.TryGetComponent<Projectile>(out var projectile))
                projectile.Initialize(direction, damage, BaseStats.pierceCount, _projectilePool);
            else
                Debug.LogError($"{projectileObj.name}: Projectile 컴포넌트가 없어 발사할 수 없습니다.");
        }
    }
}
