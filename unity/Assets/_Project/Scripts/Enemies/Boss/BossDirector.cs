using System.Collections;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Pickups;
using SushiSurvival.Player;

namespace SushiSurvival.Enemies.Boss
{
    /// <summary>
    /// 보스의 등장과 격파를 연출하고 승리로 넘긴다. 보스 자신의 행동은
    /// BossController가 맡고, 여기서는 판 전체의 흐름만 다룬다.
    /// </summary>
    public class BossDirector : MonoBehaviour
    {
        [SerializeField] private BossController boss;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private LevelSystem levelSystem;
        [SerializeField] private XPGemPoolSet gemPools;

        [Header("패턴이 쓸 풀")]
        [SerializeField] private GameObjectPool meteorPool;
        [Tooltip("소환으로 나올 잡몹 풀. 기존 BasicMob 풀을 그대로 쓴다.")]
        [SerializeField] private GameObjectPool summonMobPool;
        [SerializeField] private GameObjectPool summonEffectPool;
        [SerializeField] private GameObjectPool deathExplosionPool;

        [Header("UI")]
        [SerializeField] private GameObject bossIntroBanner;
        [SerializeField] private SushiSurvival.UI.BossHealthBar bossHealthBar;

        [Header("연출")]
        [Tooltip("등장 배너를 띄우는 시간(초, 실시간).")]
        [SerializeField] private float introBannerSeconds = 1.5f;
        [Tooltip("격파 후 결과 화면까지의 시간(초, 실시간).")]
        [SerializeField] private float deathSeconds = 1.2f;
        [Tooltip("연출 중 느려지는 정도. 0.3이면 30% 속도.")]
        [SerializeField] private float slowMotionScale = 0.3f;
        [Tooltip("플레이어로부터 이 거리 위쪽에 보스가 등장한다.")]
        [SerializeField] private float bossSpawnDistance = 8f;
        [Tooltip("격파 폭발을 이 배율로 키운다.")]
        [SerializeField] private float deathExplosionScale = 2f;

        private EnemyBase _bossEnemy;
        private bool _started;

        public void BeginIntro(PlayerHealth player)
        {
            // 5:00을 여러 프레임에 걸쳐 넘길 수 있으므로 한 번만 받는다.
            if (_started) return;
            _started = true;

            StartCoroutine(IntroSequence(player));
        }

        private IEnumerator IntroSequence(PlayerHealth player)
        {
            if (enemySpawner != null)
                enemySpawner.StopSpawning();

            ClearField();

            // 필드 정리로 젬이 쏟아지면 레벨업 팝업이 연달아 뜬다. 팝업은
            // timeScale 0이고 등장 연출은 0.3이라, 겹치면 팝업이 닫히면서
            // LevelSystem이 timeScale을 1로 되돌려 연출이 깨진다.
            while (levelSystem != null && levelSystem.IsShowingPopup)
                yield return null;

            // ─── 호감도 대화 #2가 들어올 자리 ───
            // 대화 패널을 띄우고 닫힐 때까지 여기서 기다리면 된다.

            Time.timeScale = slowMotionScale;

            if (bossIntroBanner != null)
                bossIntroBanner.SetActive(true);

            yield return new WaitForSecondsRealtime(introBannerSeconds);

            if (bossIntroBanner != null)
                bossIntroBanner.SetActive(false);

            SpawnBoss(player);

            Time.timeScale = 1f;
        }

        /// <summary>
        /// 남은 적을 전부 처치 처리한다. 중형몹을 못 잡고 도망만 다녔다면 그만큼
        /// 성장이 덜 된 채로 보스를 만나 급격히 어려워지므로, 그 성장분을
        /// 보장해 준다. 아레나가 비워져 소환 패턴이 위협으로 읽히는 효과도 있다.
        ///
        /// 기존 TakeDamage 경로를 타므로 젬 드롭·처치 수 반영·풀 반환이 전부
        /// 공짜로 따라온다. 한 판에 한 번뿐이라 FindObjectsOfType이면 충분하다.
        /// </summary>
        private void ClearField()
        {
            EnemyBase[] alive = Object.FindObjectsOfType<EnemyBase>();

            foreach (EnemyBase enemy in alive)
                enemy.TakeDamage(float.MaxValue, enemy.transform.position);

            Debug.Log($"[BossDirector] 필드 정리 — {alive.Length}마리");
        }

        private void SpawnBoss(PlayerHealth player)
        {
            if (boss == null)
            {
                Debug.LogError($"{name}: boss가 비어 있어 보스를 등장시킬 수 없습니다.");
                return;
            }

            // 반드시 ClearField 이후에 배치한다. 먼저 두면 즉사 데미지에
            // 보스 자신이 휩쓸려 등장하자마자 죽는다.
            Vector3 spawnPoint = player != null
                ? player.transform.position + Vector3.up * bossSpawnDistance
                : Vector3.zero;

            boss.transform.position = spawnPoint;
            boss.gameObject.SetActive(true);

            _bossEnemy = boss.GetComponent<EnemyBase>();

            // 보스는 풀링하지 않으므로 스포너가 젬 풀을 넣어주지 않는다.
            // 빠뜨리면 보스가 죽는 바로 그 순간에 에러가 난다.
            _bossEnemy.SetXpGemPools(gemPools);
            _bossEnemy.OnDeath += HandleBossDeath;

            boss.Activate(player, meteorPool, summonMobPool, summonEffectPool, gemPools);

            if (bossHealthBar != null)
                bossHealthBar.Show(boss);
        }

        private void HandleBossDeath(EnemyBase _) => StartCoroutine(DeathSequence());

        private IEnumerator DeathSequence()
        {
            Vector3 position = boss != null ? boss.transform.position : Vector3.zero;

            Time.timeScale = slowMotionScale;

            if (bossHealthBar != null)
                bossHealthBar.Hide();

            // 보스 오브젝트는 EnemyBase.Die()가 정리하므로, 폭발은 별개의
            // 오브젝트로 띄운다. 보스가 사라지고 그 자리에서 폭발이 이어진다.
            if (deathExplosionPool != null)
            {
                GameObject explosion = deathExplosionPool.Get(position, Quaternion.identity);
                explosion.transform.localScale = Vector3.one * deathExplosionScale;
            }

            yield return new WaitForSecondsRealtime(deathSeconds);

            // FinishRun이 timeScale을 0으로 만든다. 여기서 1로 되돌릴 필요 없다.
            if (GameManager.Instance != null)
                GameManager.Instance.FinishRun(RunOutcome.Victory);
        }

        private void OnDisable()
        {
            if (_bossEnemy != null)
                _bossEnemy.OnDeath -= HandleBossDeath;
        }
    }
}
