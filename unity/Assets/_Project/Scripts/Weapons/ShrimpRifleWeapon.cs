using SushiSurvival.Audio;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Player;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 간장 소총 — 시선 방향으로 투사체를 발사한다. 자동 조준은 없다(기획서).
    /// 레벨이 정해진 값에 닿으면 LevelSystem이 샷건으로 바꾼다(무기 강화 카드의 성장 경로).
    /// </summary>
    public class ShrimpRifleWeapon : WeaponBase
    {
        [SerializeField] private FacingController facing;
        [Tooltip("투사체가 나가는 위치. 비워두면 이 오브젝트 위치에서 발사한다.")]
        [SerializeField] private Transform muzzle;

        [Header("샷건 (무기 강화로 변신)")]
        [Tooltip("이 레벨이 되면 소총이 샷건으로 바뀐다.")]
        [SerializeField] private int shotgunFromLevel = 3;
        [Tooltip("샷건 모드에서 한 번에 나가는 산탄 수.")]
        [SerializeField] private int shotgunPelletCount = 5;
        [Tooltip("산탄이 퍼지는 전체 각도(도).")]
        [SerializeField] private float shotgunSpreadDegrees = 40f;
        [Tooltip("산탄 한 발의 피해 = 소총 피해 × 이 값. 다 맞으면 소총 한 발보다 세지만 멀면 몇 발만 맞는다.")]
        [SerializeField] private float shotgunDamageRatio = 0.5f;

        [Header("샷건 그림 (비우면 소총 그림 그대로)")]
        [Tooltip("무기 그림을 그리는 SpriteRenderer(WeaponVisual).")]
        [SerializeField] private SpriteRenderer weaponRenderer;
        [SerializeField] private Sprite shotgunSprite;
        [Tooltip("샷건 산탄의 그림.")]
        [SerializeField] private Sprite shotgunBulletSprite;
        [Tooltip("샷건 그림을 그릴 때의 크기 배율(소총 그림 대비). 샷건 그림이 100px라 0.5면 소총과 같은 크기로 보인다.")]
        [SerializeField] private float shotgunVisualScale = 0.7f;
        [Tooltip("샷건 그림일 때 캐릭터로부터의 거리(소총은 WeaponVisual의 값). 작을수록 몸에 붙는다.")]
        [SerializeField] private float shotgunOrbitDistance = 0.2f;
        [SerializeField] private WeaponVisual weaponVisual;
        [Tooltip("샷건으로 바뀐 뒤 레벨업 창의 무기 강화 카드에 쓰는 아이콘. 비우면 소총 아이콘 그대로.")]
        [SerializeField] private Sprite shotgunUpgradeIcon;
        [Tooltip("샷건 산탄 그림에 곱하는 색. 흰색보다 어둡게 하면 칙칙해진다.")]
        [SerializeField] private Color shotgunBulletTint = new Color(0.75f, 0.75f, 0.75f, 1f);

        private GameObjectPool _projectilePool;

        /// <summary>샷건으로 바뀐 상태인지. 보스 씬 이월에도 쓴다.</summary>
        public bool IsShotgun { get; private set; }

        /// <summary>소총이 샷건으로 바뀔 레벨에 닿았는데 아직 안 바뀐 상태인지.</summary>
        public bool ReadyToEvolve =>
            !IsShotgun && ShotgunProgressionLogic.ShouldEvolve(currentLevel, shotgunFromLevel);

        public override Sprite UpgradeIcon => IsShotgun && shotgunUpgradeIcon != null ? shotgunUpgradeIcon : base.UpgradeIcon;

        public void EnableShotgun()
        {
            bool alreadyShotgun = IsShotgun;
            IsShotgun = true;

            if (alreadyShotgun || weaponRenderer == null || shotgunSprite == null) return;
            weaponRenderer.sprite = shotgunSprite;
            weaponRenderer.transform.localScale *= shotgunVisualScale;
            if (weaponVisual != null) weaponVisual.OrbitDistance = shotgunOrbitDistance;
        }

        public override string DescribeUpgrade(string statText)
        {
            bool evolvesNext = !IsShotgun && ShotgunProgressionLogic.ShouldEvolve(currentLevel + 1, shotgunFromLevel);
            return ShotgunProgressionLogic.DescribeRifleUpgrade(statText, evolvesNext, Mathf.Max(1, shotgunPelletCount));
        }

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

            AudioManager.PlaySfx(IsShotgun ? SfxId.ShotgunShoot : SfxId.WeaponShoot);

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
            {
                projectile.SetLook(IsShotgun ? shotgunBulletSprite : null, shotgunBulletTint);
                projectile.Initialize(direction, damage, BaseStats.pierceCount, _projectilePool);
            }
            else
                Debug.LogError($"{projectileObj.name}: Projectile 컴포넌트가 없어 발사할 수 없습니다.");
        }
    }
}
