using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;
using SushiSurvival.Pickups;
using SushiSurvival.Player;

namespace SushiSurvival.Enemies.Boss
{
    [RequireComponent(typeof(EnemyBase))]
    [RequireComponent(typeof(EnemyAI))]
    public class BossController : MonoBehaviour
    {
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int CastMeteorHash = Animator.StringToHash("CastMeteor");
        private static readonly int CastSummonHash = Animator.StringToHash("CastSummon");

        [SerializeField] private BossData bossData;
        [SerializeField] private Animator animator;
        [SerializeField] private SushiSurvival.Core.SpriteFlasher spriteFlasher;
        [SerializeField] private MeteorPattern meteorPattern;
        [SerializeField] private SummonPattern summonPattern;

        [Tooltip("시전 애니메이션 길이(초).")]
        [SerializeField] private float castDuration = 1.08f;
        [Tooltip("페이즈 전환 시 붉게 번쩍이는 시간(초).")]
        [SerializeField] private float phaseFlashDuration = 0.3f;

        public float MaxHealth => bossData != null ? bossData.maxHealth : 0f;
        public float CurrentHealth => _enemy != null ? _enemy.CurrentHealth : 0f;

        private EnemyBase _enemy;
        private EnemyAI _ai;
        private BossChargeEffects _effects;

        private PlayerHealth _player;
        private bool _firstCast;

        private BossPatternType _previousPattern;
        private int _consecutiveCount;
        private int _nextSummonStage;
        private readonly Queue<int> _summonQueue = new Queue<int>();
        private int _phase = BossPhaseLogic.PhaseOne;
        private float _patternTimer;
        private bool _casting;
        private bool _active;

        private void Awake()
        {
            _enemy = GetComponent<EnemyBase>();
            _ai = GetComponent<EnemyAI>();
            _effects = GetComponent<BossChargeEffects>();
        }

        public void Activate(PlayerHealth player, GameObjectPool meteorPool,
                             GameObjectPool mobPool, GameObjectPool summonEffectPool,
                             XPGemPoolSet gemPools,
                             GameObjectPool californiaMobPool = null, GameObjectPool midMobPool = null)
        {
            if (bossData == null)
            {
                Debug.LogError($"{name}: bossData가 비어 있어 보스를 활성화할 수 없습니다.");
                return;
            }

            if (_enemy == null) _enemy = GetComponent<EnemyBase>();
            if (_ai == null) _ai = GetComponent<EnemyAI>();
            if (_effects == null) _effects = GetComponent<BossChargeEffects>();
            if (summonPattern == null) summonPattern = GetComponentInChildren<SummonPattern>();
            if (meteorPattern == null) meteorPattern = GetComponentInChildren<MeteorPattern>();

            if (meteorPattern != null && meteorPool != null)
                meteorPattern.SetDependencies(player, meteorPool);

            if (summonPattern != null && mobPool != null && summonEffectPool != null)
            {
                summonPattern.SetDependencies(
                    player != null ? player.transform : null, mobPool, summonEffectPool, gemPools);
                summonPattern.SetStagePools(californiaMobPool, midMobPool);
            }

            _player = player;
            _phase = BossPhaseLogic.PhaseOne;
            _previousPattern = BossPatternType.Charge;
            _consecutiveCount = 0;
            _nextSummonStage = 0;
            _summonQueue.Clear();
            // 등장하자마자 돌진하면 등장 연출이 묻히므로 첫 패턴은 항상 메테오다.
            _firstCast = true;

            BossPhaseValues values = bossData.GetPhaseValues(_phase);
            _patternTimer = values.patternInterval;

            if (_ai != null)
                _ai.MoveScale = values.moveScale;

            _casting = false;
            _active = true;
        }

        private void Update()
        {
            if (!_active || _casting) return;

            UpdatePhase();
            QueueCrossedSummons();

            if (animator != null)
                animator.SetBool(IsMovingHash, true);

            // 소환은 체력 임계로 발동하는 사건이라 패턴 타이머보다 먼저 처리한다.
            // 시전 중이었다면 위의 _casting 가드 덕에 그 시전이 끝난 뒤에야 여기까지 온다.
            if (_summonQueue.Count > 0)
            {
                StartCoroutine(CastSummonStage(_summonQueue.Dequeue()));
                return;
            }

            _patternTimer -= Time.deltaTime;
            if (_patternTimer > 0f) return;

            BossPatternType next = _firstCast
                ? BossPatternType.Meteor
                : BossPatternScheduler.SelectNext(_previousPattern, _consecutiveCount, _phase, Random.value);
            _firstCast = false;

            StartCoroutine(Cast(next));
        }

        private void UpdatePhase()
        {
            int phase = BossPhaseLogic.GetPhase(
                _enemy.CurrentHealth, bossData.maxHealth, bossData.phaseTwoThreshold);

            if (phase == _phase) return;

            _phase = phase;
            _ai.MoveScale = bossData.GetPhaseValues(_phase).moveScale;

            if (spriteFlasher != null)
                spriteFlasher.Flash(Color.red, phaseFlashDuration);
        }

        /// <summary>한 번의 큰 피해로 임계를 여러 개 넘어도 단계를 건너뛰지 않고 전부 큐에 쌓는다.</summary>
        private void QueueCrossedSummons()
        {
            var stages = bossData.summonStages;
            if (stages == null) return;

            int crossed = BossSummonLogic.CrossedStageCount(
                _enemy.CurrentHealth, bossData.maxHealth, stages, _nextSummonStage);

            for (int i = 0; i < crossed; i++)
                _summonQueue.Enqueue(_nextSummonStage + i);

            _nextSummonStage += crossed;
        }

        private IEnumerator Cast(BossPatternType pattern)
        {
            _casting = true;
            _consecutiveCount = pattern == _previousPattern ? _consecutiveCount + 1 : 1;
            _previousPattern = pattern;

            _ai.MoveScale = 0f;

            if (animator != null)
                animator.SetBool(IsMovingHash, false);

            if (pattern == BossPatternType.Charge)
            {
                yield return ChargeRoutine();
            }
            else
            {
                if (animator != null)
                    animator.SetTrigger(CastMeteorHash);

                yield return new WaitForSeconds(castDuration);

                if (meteorPattern != null)
                    meteorPattern.Fire(bossData.GetPhaseValues(_phase));
            }

            BossPhaseValues values = bossData.GetPhaseValues(_phase);

            _ai.LockedDirection = Vector2.zero;
            _ai.MoveScale = values.moveScale;
            _patternTimer = values.patternInterval;
            _casting = false;
        }

        /// <summary>
        /// 체력 임계 소환 한 단계. 무작위 패턴 순서(_previousPattern·연속 횟수)와 패턴 타이머는
        /// 건드리지 않는다 — 소환은 그 순서의 일부가 아니다. 시전 중엔 Update가 멈춰 있어서 타이머도 흐르지 않는다.
        /// </summary>
        private IEnumerator CastSummonStage(int stageIndex)
        {
            _casting = true;
            _ai.MoveScale = 0f;

            if (animator != null)
            {
                animator.SetBool(IsMovingHash, false);
                animator.SetTrigger(CastSummonHash);
            }

            yield return new WaitForSeconds(castDuration);

            BossPhaseValues values = bossData.GetPhaseValues(_phase);

            if (summonPattern != null)
                summonPattern.FireStage(bossData.summonStages[stageIndex], values.summonRadius);

            _ai.LockedDirection = Vector2.zero;
            _ai.MoveScale = values.moveScale;
            _casting = false;
        }

        /// <summary>
        /// 돌진: ① 멈춰서 붉게 번쩍이며 예고(예고선이 플레이어를 따라간다) → ② 예고 종료 직전에 방향을
        /// 확정하고 예고선을 고정 → ③ 그 방향으로 돌진(잔상·먼지) → ④ 멈춰서 충격파 → ⑤ 무방비로 서 있는다(반격 기회).
        /// 방향을 확정하기 전까지 움직여서 각을 만들어 두면 피할 수 있다. 피해는 보스의 접촉 데미지가 그대로 준다.
        /// 연출(_effects)이 없으면 방향을 예고 종료 순간에 확정하는 기존 동작과 같다.
        /// </summary>
        private IEnumerator ChargeRoutine()
        {
            BossPhaseValues values = bossData.GetPhaseValues(_phase);

            float chargeDistance = ChargeEffectsLogic.ChargeDistance(
                bossData.moveSpeed, values.chargeSpeedScale, values.chargeDuration);
            float lockSeconds = _effects != null ? _effects.ChargeLockSeconds : 0f;
            float lockDelay = ChargeEffectsLogic.LockDelay(values.chargeWindup, lockSeconds);

            if (spriteFlasher != null)
                spriteFlasher.Flash(Color.red, values.chargeWindup);

            if (_effects != null)
                _effects.BeginTelegraph(chargeDistance);

            // ① 방향을 확정하기 전까지 예고선이 플레이어를 실시간으로 따라간다.
            float elapsed = 0f;
            while (elapsed < lockDelay)
            {
                if (_effects != null && _player != null)
                    _effects.AimTelegraph(transform.position, _player.transform.position);

                yield return null;
                elapsed += Time.deltaTime;
            }

            // ② 방향 확정. 예고선 고정과 같은 시점이라 선이 곧 실제 돌진 방향이다.
            Vector2 from = transform.position;
            Vector2 to = _player != null ? (Vector2)_player.transform.position : from;
            Vector2 direction = BossAimLogic.ChargeDirection(from, to, Vector2.right);

            if (_effects != null)
            {
                _effects.AimTelegraph(from, to);
                _effects.LockTelegraph();
            }

            float remainingWindup = values.chargeWindup - lockDelay;
            if (remainingWindup > 0f)
                yield return new WaitForSeconds(remainingWindup);

            // ③ 돌진
            _ai.LockedDirection = direction;
            _ai.MoveScale = values.chargeSpeedScale;

            if (_effects != null)
                _effects.BeginTrail();

            yield return new WaitForSeconds(values.chargeDuration);

            // ④ 정지
            _ai.LockedDirection = Vector2.zero;
            _ai.MoveScale = 0f;

            if (_effects != null)
            {
                _effects.EndTrail();
                _effects.PlayImpact(transform.position);
            }

            // ⑤ 반격 기회
            yield return new WaitForSeconds(values.chargeRecovery);
        }
    }
}