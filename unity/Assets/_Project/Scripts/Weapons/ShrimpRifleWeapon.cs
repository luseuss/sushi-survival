using SushiSurvival.Audio;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Player;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 간장 소총 — 시선 방향으로 투사체를 발사한다. 자동 조준은 없다(기획서).
    /// 레벨이 정해진 값에 닿거나 와사비 가위바위보에 이기면 "와사비 샷건 스킬"을 얻는다(무기 강화 카드의 성장 경로).
    /// 스킬은 마우스 클릭으로 발동해 일정 시간 동안 소총 대신 와사비 샷건(산탄 부채꼴)을 쏘고, 끝나면 쿨타임이 돈다.
    /// 클릭하지 않으면 소총이 계속 자동으로 나간다. 쿨타임·남은 시간은 체력바 아래 막대로 보인다.
    /// </summary>
    public class ShrimpRifleWeapon : WeaponBase
    {
        [SerializeField] private FacingController facing;
        [Tooltip("투사체가 나가는 위치. 비워두면 이 오브젝트 위치에서 발사한다.")]
        [SerializeField] private Transform muzzle;

        [Header("와사비 샷건 스킬 (얻는 시점)")]
        [Tooltip("이 레벨이 되면 와사비 샷건 스킬을 얻는다(와사비 가위바위보에 이겨도 얻는다).")]
        [SerializeField] private int shotgunFromLevel = 3;

        [Header("와사비 샷건 스킬 (클릭 발동)")]
        [Tooltip("스킬을 켜면 와사비 샷건이 나가는 시간(초).")]
        [SerializeField, Min(0.1f)] private float skillDuration = 5f;
        [Tooltip("스킬이 끝난 뒤 다시 쓸 수 있기까지의 대기 시간(초). 공격속도 배율이 높을수록 짧아진다.")]
        [SerializeField, Min(0f)] private float skillCooldown = 10f;
        [Tooltip("샷건이 켜져 있는 동안의 발사 간격(초). 소총보다 짧게 둬서 스킬이 강하게 느껴지게 한다.")]
        [SerializeField, Min(0.05f)] private float shotgunFireInterval = 0.5f;
        [Tooltip("쿨타임이 끝나기 이만큼(초) 전에 눌러 둔 클릭은 준비되는 순간 발동한다(타이밍 여유).")]
        [SerializeField, Min(0f)] private float clickBufferSeconds = 0.25f;
        [Tooltip("발동할 때 화면이 흔들리는 세기와 시간(초). 0이면 흔들지 않는다.")]
        [SerializeField, Min(0f)] private float shakeMagnitude = 0.1f;
        [SerializeField, Min(0f)] private float shakeSeconds = 0.12f;

        [Header("샷건 모양")]
        [Tooltip("샷건이 켜져 있는 동안 한 번에 나가는 산탄 수.")]
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
        [Tooltip("스킬을 얻은 뒤 레벨업 창의 무기 강화 카드에 쓰는 아이콘. 비우면 소총 아이콘 그대로.")]
        [SerializeField] private Sprite shotgunUpgradeIcon;
        [Tooltip("샷건 산탄 그림에 곱하는 색. 흰색보다 어둡게 하면 칙칙해진다.")]
        [SerializeField] private Color shotgunBulletTint = new Color(0.75f, 0.75f, 0.75f, 1f);

        [Header("쿨타임 막대(체력바 바로 아래)")]
        [Tooltip("체력바와 같은 틀 그림.")]
        [SerializeField] private Sprite cooldownBarFrame;
        [Tooltip("채움 그림. 흰색 그림이고 색은 코드가 입힌다.")]
        [SerializeField] private Sprite cooldownBarFill;
        [Tooltip("체력바보다 얼마나 작게 할지(배율).")]
        [SerializeField, Min(0.1f)] private float cooldownBarScale = 0.7f;
        [Tooltip("체력바와의 간격(화면 픽셀).")]
        [SerializeField] private float cooldownBarGap = 4f;

        private GameObjectPool _projectilePool;

        // 스킬 상태: 켜져 있는 남은 시간 / 쿨타임 남은 시간과 전체 길이 / 미리 누른 클릭.
        private float _activeRemaining;
        private float _cooldownTimer;
        private float _cooldownDuration;
        private bool _clickBuffered;
        private UmbrellaCooldownBar _bar;

        // 소총 모습(샷건이 끝나면 되돌린다).
        private bool _lookCaptured;
        private Sprite _rifleSprite;
        private Vector3 _rifleScale;
        private float _rifleOrbit;

        /// <summary>와사비 샷건 스킬을 얻은 상태인지. 보스 씬 이월과 와사비 보상 판정에 쓴다.</summary>
        public bool HasShotgunSkill { get; private set; }

        /// <summary>샷건이 지금 켜져 있는지(스킬 지속 중).</summary>
        public bool IsSkillActive => _activeRemaining > 0f;

        /// <summary>스킬을 얻을 레벨에 닿았는데 아직 못 얻은 상태인지.</summary>
        public bool ReadyToEvolve =>
            !HasShotgunSkill && ShotgunProgressionLogic.ShouldEvolve(currentLevel, shotgunFromLevel);

        public override Sprite UpgradeIcon => HasShotgunSkill && shotgunUpgradeIcon != null ? shotgunUpgradeIcon : base.UpgradeIcon;

        /// <summary>샷건이 켜져 있는 동안은 짧은 발사 간격을 쓴다.</summary>
        protected override float BaseCooldown => IsSkillActive ? shotgunFireInterval : base.BaseCooldown;

        /// <summary>와사비 샷건 스킬을 얻는다. 이미 얻었으면 아무 일도 없다. 처음부터 쓸 수 있는 상태로 시작한다.</summary>
        public void UnlockShotgunSkill()
        {
            if (HasShotgunSkill) return;

            HasShotgunSkill = true;
            CaptureRifleLook();
            CreateBar();
        }

        public override string DescribeUpgrade(string statText)
        {
            bool evolvesNext = !HasShotgunSkill && ShotgunProgressionLogic.ShouldEvolve(currentLevel + 1, shotgunFromLevel);
            return ShotgunProgressionLogic.DescribeRifleUpgrade(statText, evolvesNext, Mathf.Max(1, shotgunPelletCount));
        }

        /// <summary>
        /// PlayerSpawner가 스폰 직후 주입한다. 프리팹 에셋은 씬에만 존재하는
        /// 풀을 Inspector로 직접 참조할 수 없기 때문.
        /// </summary>
        public void SetProjectilePool(GameObjectPool pool) => _projectilePool = pool;

        private void OnEnable()
        {
            // 컴포넌트가 꺼졌다 켜져도 스킬을 얻은 상태면 막대를 다시 만든다.
            if (HasShotgunSkill) CreateBar();
        }

        private void OnDisable()
        {
            if (_bar != null) Destroy(_bar.gameObject);
            _bar = null;
        }

        protected override void Update()
        {
            if (HasShotgunSkill) UpdateSkill();
            base.Update();
        }

        // 스킬: 켜져 있는 동안은 남은 시간을 줄이고, 아니면 쿨타임을 줄이며 클릭을 기다린다.
        private void UpdateSkill()
        {
            if (IsSkillActive)
            {
                _activeRemaining -= Time.deltaTime;
                if (_activeRemaining <= 0f)
                    EndSkill();
                else if (_bar != null)
                    _bar.SetActiveRatio(ShotgunSkillLogic.ActiveRatio(_activeRemaining, skillDuration));
                return;
            }

            _cooldownTimer = Mathf.Max(0f, _cooldownTimer - Time.deltaTime);
            if (_bar != null) _bar.SetFill(UmbrellaSkillLogic.FillRatio(_cooldownTimer, _cooldownDuration));

            if (SkillInput.ClickedThisFrame())
                _clickBuffered = UmbrellaSkillLogic.IsReady(_cooldownTimer)
                                 || UmbrellaSkillLogic.ShouldBuffer(_cooldownTimer, clickBufferSeconds);

            if (!ShotgunSkillLogic.ShouldStart(HasShotgunSkill, IsSkillActive, _cooldownTimer, _clickBuffered)) return;

            _clickBuffered = false;
            StartSkill();
        }

        private void StartSkill()
        {
            _activeRemaining = skillDuration;
            ApplyShotgunLook(true);

            // 발동하는 맛: 바로 한 발 나가도록 자동 공격 대기를 비우고, 화면이 살짝 흔들린다.
            ResetAttackCooldown();
            if (shakeMagnitude > 0f && JuiceDirector.Instance != null)
                JuiceDirector.Instance.Shake(shakeMagnitude, shakeSeconds);

            if (_bar != null) _bar.SetActiveRatio(1f);
        }

        private void EndSkill()
        {
            _activeRemaining = 0f;
            ApplyShotgunLook(false);

            float attackSpeed = StatMultiplier(StatType.AttackSpeed);
            _cooldownDuration = CooldownLogic.ApplyAttackSpeed(skillCooldown, attackSpeed, minCooldown);
            _cooldownTimer = _cooldownDuration;

            // 소총은 바로 이어서 나가지 않고 평소 간격을 다시 센다.
            ResetAttackCooldown();
            if (_bar != null) _bar.SetFill(0f);
        }

        private void CreateBar()
        {
            if (_bar != null) return;

            // 체력바(HUD)가 있으면 그 바로 아래에 붙는다. 없는 씬이면 막대 없이 동작한다.
            var hpBar = FindObjectOfType<SushiSurvival.UI.HealthBar>();
            if (hpBar == null) return;

            _bar = UmbrellaCooldownBar.Create((RectTransform)hpBar.transform, cooldownBarFrame, cooldownBarFill,
                                              cooldownBarScale, cooldownBarGap);
        }

        private void CaptureRifleLook()
        {
            if (_lookCaptured || weaponRenderer == null) return;

            _lookCaptured = true;
            _rifleSprite = weaponRenderer.sprite;
            _rifleScale = weaponRenderer.transform.localScale;
            _rifleOrbit = weaponVisual != null ? weaponVisual.OrbitDistance : 0f;
        }

        // 샷건이 켜지면 무기 그림·크기·거리를 샷건으로, 끝나면 소총으로 되돌린다.
        private void ApplyShotgunLook(bool shotgun)
        {
            if (!_lookCaptured || weaponRenderer == null || shotgunSprite == null) return;

            weaponRenderer.sprite = shotgun ? shotgunSprite : _rifleSprite;
            weaponRenderer.transform.localScale = shotgun ? _rifleScale * shotgunVisualScale : _rifleScale;
            if (weaponVisual != null) weaponVisual.OrbitDistance = shotgun ? shotgunOrbitDistance : _rifleOrbit;
        }

        protected override void Attack()
        {
            if (_projectilePool == null)
            {
                Debug.LogError($"{name}: projectilePool이 주입되지 않아 발사할 수 없습니다.");
                return;
            }

            Vector2 direction = facing.CurrentFacing;
            Vector3 spawnPos = muzzle != null ? muzzle.position : transform.position;

            AudioManager.PlaySfx(IsSkillActive ? SfxId.ShotgunShoot : SfxId.WeaponShoot);

            if (!IsSkillActive)
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
                projectile.SetLook(IsSkillActive ? shotgunBulletSprite : null, shotgunBulletTint);
                projectile.Initialize(direction, damage, BaseStats.pierceCount, _projectilePool);
            }
            else
                Debug.LogError($"{projectileObj.name}: Projectile 컴포넌트가 없어 발사할 수 없습니다.");
        }
    }
}
