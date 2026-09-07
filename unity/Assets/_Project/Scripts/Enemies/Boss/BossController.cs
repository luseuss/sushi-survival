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

            if (_enemy == null) _enemy = GetComponent<EnemyBase>();
            if (_ai == null) _ai = GetComponent<EnemyAI>();
            if (summonPattern == null) summonPattern = GetComponentInChildren<SummonPattern>();
            if (meteorPattern == null) meteorPattern = GetComponentInChildren<MeteorPattern>();

            if (meteorPattern != null && meteorPool != null)
                meteorPattern.SetDependencies(player, meteorPool);

            if (summonPattern != null && mobPool != null && summonEffectPool != null)
                summonPattern.SetDependencies(
                    player != null ? player.transform : null, mobPool, summonEffectPool, gemPools);

            _phase = BossPhaseLogic.PhaseOne;
            _previousPattern = BossPatternType.Summon;

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

            if (spriteFlasher != null)
                spriteFlasher.Flash(Color.red, phaseFlashDuration);
        }

        private IEnumerator Cast(BossPatternType pattern)
        {
            _casting = true;
            _previousPattern = pattern;

            _ai.MoveScale = 0f;

            if (animator != null)
            {
                animator.SetBool(IsMovingHash, false);
                animator.SetTrigger(pattern == BossPatternType.Meteor ? CastMeteorHash : CastSummonHash);
            }

            yield return new WaitForSeconds(castDuration);

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