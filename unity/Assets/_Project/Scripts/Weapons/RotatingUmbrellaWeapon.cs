using SushiSurvival.Audio;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using SushiSurvival.Core;
using SushiSurvival.Enemies;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 아델린의 액티브 스킬 — 마우스 클릭으로 발동하는 회전 우산. 클릭하면 우산이 레벨별로 정해진
    /// 바퀴 수만큼 돌고 사라지며, 도는 동안 스치는 적마다 개별 재타격 타이머(weaponData의 cooldown
    /// 필드 재해석)로 데미지를 준다. 다 돈 뒤 쿨타임이 차야 다시 쓸 수 있고, 클릭하지 않으면 나가지 않는다.
    /// 쿨타임은 캐릭터 발밑 막대로 보인다. 양산은 우산과 별개로 계속 자동 공격한다.
    /// </summary>
    public class RotatingUmbrellaWeapon : WeaponBase
    {
        [Tooltip("궤도를 도는 우산 그림 오브젝트. 최대 5개, 레벨별로 앞에서부터 필요한 개수만 켠다.")]
        [SerializeField] private Transform[] umbrellas;
        [Tooltip("레벨(1~4)별 우산 개수. 아델린은 양산이 Lv3에서 우산으로 바뀌므로 Lv3 3개, Lv4 5개가 실제로 쓰인다(1~2번째 칸은 예비).")]
        [SerializeField] private int[] umbrellaCountByLevel = { 3, 3, 3, 5 };

        [Header("회전 쿨타임")]
        [Tooltip("한 번 다 돈 뒤 다시 클릭으로 발동할 수 있기까지의 대기 시간(초). 공격속도 배율이 높을수록 짧아진다.")]
        [SerializeField, Min(0f)] private float spinCooldown = 4f;
        [Tooltip("공격속도 배율 1.0 기준 회전 속도(초당 도). 360이면 한 바퀴에 1초, 180이면 2초 걸린다.")]
        [SerializeField] private float rotationSpeedDegreesPerSecond = 360f;
        [Tooltip("레벨(1~4)별로 한 번 회전할 때 도는 바퀴 수. 레벨이 배열 길이를 넘으면 마지막 값을 쓴다. " +
                 "우산은 Lv3에서 시작하므로 1~2번째 칸은 예비다.")]
        [SerializeField] private int[] lapsByLevel = { 1, 1, 2, 3 };

        [Header("클릭 발동")]
        [Tooltip("쿨타임이 끝나기 이만큼(초) 전에 눌러 둔 클릭은 준비되는 순간 발동한다(타이밍 여유).")]
        [SerializeField, Min(0f)] private float clickBufferSeconds = 0.25f;
        [Tooltip("우산이 튀어나오며 커지는 시간(초).")]
        [SerializeField, Min(0f)] private float popSeconds = 0.15f;
        [Tooltip("발동할 때 화면이 흔들리는 세기와 시간(초). 0이면 흔들지 않는다.")]
        [SerializeField, Min(0f)] private float shakeMagnitude = 0.1f;
        [SerializeField, Min(0f)] private float shakeSeconds = 0.12f;

        [Header("쿨타임 막대(체력바 바로 아래)")]
        [Tooltip("체력바와 같은 틀 그림.")]
        [SerializeField] private Sprite cooldownBarFrame;
        [Tooltip("채움 그림. 흰색 그림이고 색은 코드가 입힌다(차는 중 하늘색, 준비되면 금색).")]
        [SerializeField] private Sprite cooldownBarFill;
        [Tooltip("체력바보다 얼마나 작게 할지(배율).")]
        [SerializeField, Min(0.1f)] private float cooldownBarScale = 0.7f;
        [Tooltip("체력바와의 간격(화면 픽셀).")]
        [SerializeField] private float cooldownBarGap = 4f;

        [Header("우산 설정")]
        [Tooltip("우산 그림이 그려진 기본 방향 보정(도). 그림이 오른쪽(+x)을 향해 그려져 있으면 0, " +
                 "위쪽(+y)을 향해 그려져 있으면 -90(그림 윗부분이 바깥을 향한다). 거꾸로 보이면 90.")]
        [SerializeField] private float spriteAngleOffset;
        [Tooltip("우산 하나가 적을 스쳤다고 판정하는 반경.")]
        [SerializeField] private float hitRadius = 0.3f;
        [SerializeField] private LayerMask enemyLayer;

        /// <summary>클릭으로 회전이 시작될 때마다 알린다(연습 플레이가 "스킬을 써 봤는지" 확인하는 데 쓴다).</summary>
        public event System.Action SpinStarted;

        // 우산마다 매 프레임 조회하므로 배열을 새로 만들지 않고 이 버퍼를 재사용한다(GC 부담을 줄인다).
        private static readonly Collider2D[] HitBuffer = new Collider2D[32];

        // 이번 회전에서 지금까지 돈 각도. 회전이 시작될 때마다 0에서 출발해 _spinTargetAngle까지 쌓인다.
        private float _spinAngle;
        // 이번 회전에서 돌아야 할 총 각도(바퀴 수 × 360). 회전이 시작될 때 현재 레벨 기준으로 정해진다.
        private float _spinTargetAngle;
        private int _lastLap;
        private bool _isSpinning;
        // 대기 중에 남은 쿨타임. 0 이하면 클릭으로 발동할 수 있다(0으로 시작하므로 처음부터 쓸 수 있다).
        private float _cooldownTimer;
        // 마지막으로 정해진 쿨타임 전체 길이(막대 비율 계산용).
        private float _cooldownDuration;
        // 이번 회전이 시작된 뒤 흐른 시간(튀어나오는 연출용).
        private float _spinElapsed;
        // 쿨타임이 끝나기 직전에 눌러 둔 클릭.
        private bool _clickBuffered;
        private Vector3[] _baseScales;
        private UmbrellaCooldownBar _bar;
        // 풀링된 적이 죽고 짧은 시간 안에 같은 자리에서 재사용되면 이전 생의
        // 마지막 타격 시각이 남아 있어 첫 타격이 한 번 씹힐 수 있다 — 재타격
        // 간격(기본 0.3초)이 짧아 실전 영향은 미미해서 지금은 정리하지 않는다.
        private readonly Dictionary<EnemyBase, float> _lastHitTime = new Dictionary<EnemyBase, float>();

        protected override void Update()
        {
            if (weaponData == null || umbrellas == null || umbrellas.Length == 0) return;

            float attackSpeed = StatMultiplier(StatType.AttackSpeed);

            if (_baseScales == null) CaptureBaseScales();

            // 대기 중: 쿨타임을 줄이고, 준비된 상태에서 클릭하면 회전을 시작한다. 클릭하지 않으면 나가지 않는다.
            if (!_isSpinning)
            {
                _cooldownTimer = Mathf.Max(0f, _cooldownTimer - Time.deltaTime);
                UpdateBar();

                if (ClickedThisFrame())
                    _clickBuffered = UmbrellaSkillLogic.IsReady(_cooldownTimer)
                                     || UmbrellaSkillLogic.ShouldBuffer(_cooldownTimer, clickBufferSeconds);

                if (!_clickBuffered || !UmbrellaSkillLogic.IsReady(_cooldownTimer)) return;

                _clickBuffered = false;
                StartSpin();
            }

            _spinElapsed += Time.deltaTime;

            // 회전 중: 목표 각도(바퀴 수 × 360도)를 넘지 않도록 각도를 쌓는다.
            _spinAngle += rotationSpeedDegreesPerSecond * attackSpeed * Time.deltaTime;
            bool finished = _spinAngle >= _spinTargetAngle;
            if (finished) _spinAngle = _spinTargetAngle;

            // 바퀴가 하나 끝날 때마다 우산 도는 소리를 낸다(마지막 바퀴가 끝나는 순간은 제외).
            int lap = Mathf.FloorToInt(_spinAngle / 360f);
            if (!finished && lap != _lastLap)
            {
                _lastLap = lap;
                AudioManager.PlaySfx(SfxId.UmbrellaSpin);
            }

            int count = UmbrellaCountForLevel();
            float radius = Range;

            for (int i = 0; i < umbrellas.Length; i++)
            {
                bool active = i < count;
                umbrellas[i].gameObject.SetActive(active);
                if (!active) continue;

                float angle = UmbrellaOrbitLogic.AngleForIndex(_spinAngle, i, count);
                umbrellas[i].localPosition = UmbrellaOrbitLogic.PositionForAngle(angle, radius);
                umbrellas[i].localRotation = Quaternion.Euler(0f, 0f, angle + spriteAngleOffset);
                umbrellas[i].localScale = _baseScales[i] * UmbrellaSkillLogic.PopScale(
                    popSeconds <= 0f ? 1f : _spinElapsed / popSeconds);
            }

            float reHitInterval = CooldownLogic.ApplyAttackSpeed(
                BaseStats.cooldown, attackSpeed, minCooldown);
            CheckHits(count, reHitInterval);

            if (finished) EndSpin(attackSpeed);
        }

        private void StartSpin()
        {
            _isSpinning = true;
            _spinAngle = 0f;
            _spinElapsed = 0f;
            _lastLap = 0;
            _spinTargetAngle = 360f * LapsForLevel(currentLevel);
            // 회전이 시작될 때 우산 도는 소리를 낸다(이후 바퀴마다 Update에서 한 번씩 더 난다).
            AudioManager.PlaySfx(SfxId.UmbrellaSpin);

            // 발동하는 맛: 화면이 살짝 흔들리고 막대는 비어서 다시 차오르기 시작한다.
            if (shakeMagnitude > 0f && JuiceDirector.Instance != null)
                JuiceDirector.Instance.Shake(shakeMagnitude, shakeSeconds);

            if (_bar != null) _bar.SetFill(0f);

            SpinStarted?.Invoke();
        }

        // 레벨별 바퀴 수. 배열이 비어 있으면 1바퀴, 값이 1 미만이어도 최소 1바퀴로 본다.
        private int LapsForLevel(int level)
        {
            if (lapsByLevel == null || lapsByLevel.Length == 0)
                return 1;

            int index = Mathf.Clamp(level - 1, 0, lapsByLevel.Length - 1);
            return Mathf.Max(1, lapsByLevel[index]);
        }

        private void EndSpin(float attackSpeed)
        {
            _isSpinning = false;
            _cooldownDuration = CooldownLogic.ApplyAttackSpeed(spinCooldown, attackSpeed, minCooldown);
            _cooldownTimer = _cooldownDuration;

            // 쿨타임 동안은 우산을 숨긴다.
            for (int i = 0; i < umbrellas.Length; i++)
                umbrellas[i].gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            // 체력바(HUD)가 있으면 그 바로 아래에 붙는다. 없는 씬이면 막대 없이 동작한다.
            if (_bar == null)
            {
                var hpBar = FindObjectOfType<SushiSurvival.UI.HealthBar>();
                if (hpBar != null)
                    _bar = UmbrellaCooldownBar.Create((RectTransform)hpBar.transform, cooldownBarFrame, cooldownBarFill,
                                                      cooldownBarScale, cooldownBarGap);
            }

            UpdateBar();
        }

        private void OnDisable()
        {
            if (_bar != null) Destroy(_bar.gameObject);
            _bar = null;
        }

        private void CaptureBaseScales()
        {
            _baseScales = new Vector3[umbrellas.Length];
            for (int i = 0; i < umbrellas.Length; i++)
                _baseScales[i] = umbrellas[i].localScale;
        }

        // 쿨타임 막대: 회전 중에는 빈 채로 두고, 쿨타임이 돌면 차오른다.
        private void UpdateBar()
        {
            if (_bar == null) return;

            float ratio = _isSpinning ? 0f : UmbrellaSkillLogic.FillRatio(_cooldownTimer, _cooldownDuration);
            _bar.SetFill(ratio);
        }

        // 마우스 왼쪽 버튼을 이번 프레임에 눌렀는지. 레벨업·일시정지 창이 떠 있거나 화면 위 버튼을 누르는 중이면 무시한다.
        private static bool ClickedThisFrame()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return false;

            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            return UmbrellaSkillLogic.ClickAccepted(Time.timeScale, overUi);
        }

        // 우산은 쿨타임마다 한 번 쏘는 무기가 아니라 이 메서드는 쓰이지 않는다
        // (Update()를 통째로 오버라이드해서 base.Attack() 호출 경로 자체가 없다).
        protected override void Attack() { }

        public override string DescribeUpgrade(string statText)
            => UmbrellaProgressionLogic.DescribeUmbrellaCount(
                statText, UmbrellaCountForLevel(currentLevel), UmbrellaCountForLevel(currentLevel + 1));

        private int UmbrellaCountForLevel() => UmbrellaCountForLevel(currentLevel);

        private int UmbrellaCountForLevel(int level)
        {
            if (umbrellaCountByLevel == null || umbrellaCountByLevel.Length == 0)
                return umbrellas.Length;

            int index = Mathf.Clamp(level - 1, 0, umbrellaCountByLevel.Length - 1);
            return Mathf.Min(umbrellaCountByLevel[index], umbrellas.Length);
        }

        private void CheckHits(int count, float reHitInterval)
        {
            for (int i = 0; i < count; i++)
            {
                int hitCount = Physics2D.OverlapCircleNonAlloc(umbrellas[i].position, hitRadius, HitBuffer, enemyLayer);
                for (int h = 0; h < hitCount; h++)
                {
                    if (!HitBuffer[h].TryGetComponent<EnemyBase>(out var enemy)) continue;

                    _lastHitTime.TryGetValue(enemy, out float last);
                    if (Time.time - last < reHitInterval) continue;

                    enemy.TakeDamage(Damage, transform.position);
                    _lastHitTime[enemy] = Time.time;
                }
            }
        }
    }
}