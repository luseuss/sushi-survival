using System.Collections;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;
using SushiSurvival.Pickups;
using SushiSurvival.Player;

namespace SushiSurvival.Enemies.Boss
{
    /// <summary>
    /// 보스의 행동을 관장한다. 이동·피격·사망은 EnemyBase/EnemyAI가 이미
    /// 처리하므로 여기서는 패턴 발동과 페이즈 전환만 다룬다.
    ///
    /// 플레이어 캐릭터와 달리 보스는 공격 모션을 몸통 애니메이터에 넣는다.
    /// 보스의 시전 시트는 무기 단독 그림이 아니라 보스 본체가 그려진 그림이라,
    /// 캐릭터 3종에서 겪었던 "공격 중 캐릭터가 무기 그림으로 교체되는" 문제가 없다.
    /// </summary>
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

        [Tooltip("시전 애니메이션 길이(초). 13프레임 @12FPS = 약 1.08초.")]
        [SerializeField] private float castDuration = 1.08f;
        [Tooltip("페이즈 전환 시 붉게 번쩍이는 시간(초).")]
        [SerializeField] private float phaseFlashDuration = 0.3f;

        public float MaxHealth => bossData != null ? bossData.maxHealth : 0f;
        public float CurrentHealth => _enemy != null ? _enemy.CurrentHealth : 0f;

        private EnemyBase _enemy;
        private EnemyAI _ai;

        private BossPatternType _previousPattern;
        private int _phase = BossPhaseLogic.PhaseOne;
        private float _patternTimer;
        private bool _casting;
        private bool _active;

        private void Awake()
        {
            _enemy = GetComponent<EnemyBase>();
            _ai = GetComponent<EnemyAI>();
        }

        public void Activate(PlayerHealth player, GameObjectPool meteorPool,
                             GameObjectPool mobPool, GameObjectPool summonEffectPool,
                             XPGemPoolSet gemPools)
        {
            if (bossData == null)
            {
                Debug.LogError($"{name}: bossData가 비어 있어 보스를 활성화할 수 없습니다.");
                return;
            }

            if (meteorPattern != null)
                meteorPattern.SetDependencies(player, meteorPool);

            if (summonPattern != null)
                summonPattern.SetDependencies(
                    player != null ? player.transform : null, mobPool, summonEffectPool, gemPools);

            _phase = BossPhaseLogic.PhaseOne;

            // 직전 패턴을 소환으로 두면 첫 패턴이 메테오가 된다.
            // 등장하자마자 잡몹을 뿌리면 등장 연출이 묻힌다.
            _previousPattern = BossPatternType.Summon;

            BossPhaseValues values = bossData.GetPhaseValues(_phase);
            _patternTimer = values.patternInterval;
            _ai.MoveScale = values.moveScale;

            _casting = false;
            _active = true;
        }

        private void Update()
        {
            if (!_active || _casting) return;

            UpdatePhase();

            if (animator != null)
                animator.SetBool(IsMovingHash, true);

            _patternTimer -= Time.deltaTime;
            if (_patternTimer > 0f) return;

            StartCoroutine(Cast(BossPatternScheduler.SelectNext(_previousPattern)));
        }

        private void UpdatePhase()
        {
            int phase = BossPhaseLogic.GetPhase(
                _enemy.CurrentHealth, bossData.maxHealth, bossData.phaseTwoThreshold);

            if (phase == _phase) return;

            _phase = phase;
            _ai.MoveScale = bossData.GetPhaseValues(_phase).moveScale;

            Debug.Log($"[BossController] 페이즈 {_phase} 전환");

            if (spriteFlasher != null)
                spriteFlasher.Flash(Color.red, phaseFlashDuration);
        }

        private IEnumerator Cast(BossPatternType pattern)
        {
            _casting = true;
            _previousPattern = pattern;

            // 시전 중에는 제자리에 선다. 넉백은 MoveScale과 무관하게 계속 먹는다.
            _ai.MoveScale = 0f;

            if (animator != null)
            {
                animator.SetBool(IsMovingHash, false);
                animator.SetTrigger(pattern == BossPatternType.Meteor ? CastMeteorHash : CastSummonHash);
            }

            yield return new WaitForSeconds(castDuration);

            // 시전 애니가 끝나는 프레임 = 구슬이 화면 위로 사라지는 프레임이다.
            BossPhaseValues values = bossData.GetPhaseValues(_phase);

            if (pattern == BossPatternType.Meteor)
            {
                if (meteorPattern != null) meteorPattern.Fire(values);
            }
            else
            {
                if (summonPattern != null) summonPattern.Fire(values);
            }

            _ai.MoveScale = values.moveScale;
            _patternTimer = values.patternInterval;
            _casting = false;
        }
    }
}
