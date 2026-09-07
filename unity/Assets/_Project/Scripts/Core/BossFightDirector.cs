using System.Collections;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;
using SushiSurvival.Pickups;
using SushiSurvival.Player;
using SushiSurvival.Weapons;
using UnityEngine.SceneManagement;

namespace SushiSurvival.Enemies.Boss
{
    /// <summary>
    /// BossScene 전용 매니저. 일반 잡몹 웨이브 없이 오직 보스전 등장,
    /// 패턴 구동, 체력바 연동 및 격파 시 결과(Victory) 처리만 담당한다.
    /// </summary>
    public class BossFightDirector : MonoBehaviour
    {
        [Header("스포너 및 플레이어 관련")]
        [SerializeField] private PlayerSpawner playerSpawner;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private BossController boss;

        [Header("보스 패턴 및 연출용 풀")]
        [SerializeField] private XPGemPoolSet gemPools;
        [SerializeField] private GameObjectPool meteorPool;
        [SerializeField] private GameObjectPool summonMobPool;
        [SerializeField] private GameObjectPool summonEffectPool;
        [SerializeField] private GameObjectPool deathExplosionPool;

        [Header("UI")]
        [SerializeField] private GameObject bossIntroBanner;
        [SerializeField] private SushiSurvival.UI.BossHealthBar bossHealthBar;
        [Tooltip("HUD 코너의 플레이어 체력바")]
        [SerializeField] private SushiSurvival.UI.HealthBar hudHealthBar;

        [Header("UI - 패배 시")]
        [SerializeField] private SushiSurvival.UI.GameOverPanel gameOverPanel;

        [Header("연출 설정")]
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
        private PlayerHealth _playerHealth;

        private void Start()
        {
            Time.timeScale = 1f;

            CharacterData selectedCharacter = RunResultCarrier.SelectedCharacterData;
            if (selectedCharacter == null)
            {
                Debug.LogWarning("[BossFightDirector] RunResultCarrier에 선택된 캐릭터 데이터가 없습니다.");
                return;
            }

            GameObject playerObj = playerSpawner != null ? playerSpawner.Spawn(selectedCharacter) : null;
            if (playerObj == null)
            {
                Debug.LogError("[BossFightDirector] 플레이어 스폰에 실패했습니다.");
                return;
            }

            Transform playerTransform = playerObj.transform;
            _playerHealth = playerObj.GetComponent<PlayerHealth>();
            var playerStats = playerObj.GetComponent<PlayerStats>();
            var weapon = playerObj.GetComponent<WeaponBase>();

            // 👇 이전 씬에서 가져온 체력이 있다면 복원
            if (_playerHealth != null && RunResultCarrier.PlayerCurrentHealth > 0f)
            {
                float healthDifference = RunResultCarrier.PlayerCurrentHealth - _playerHealth.CurrentHealth;
                if (healthDifference > 0f)
                {
                    // PlayerHealth 클래스의 기존 회복 메서드 활용 또는 차이만큼 보정
                    // (만약 PlayerHealth에 SetHealth나 Heal 방식이 있다면 그에 맞춤)
                }
            }

            if (cameraFollow != null)
                cameraFollow.SetTarget(playerTransform);

            if (hudHealthBar != null && _playerHealth != null)
            {
                hudHealthBar.gameObject.SetActive(true);
                hudHealthBar.SetTarget(_playerHealth, selectedCharacter.portraitSprite);
            }

            if (_playerHealth != null)
                _playerHealth.OnDeath += HandlePlayerDeath;

            if (bossIntroBanner != null)
                bossIntroBanner.SetActive(false);

            SpawnBoss(playerTransform);
        }
        private IEnumerator IntroSequence(Transform playerTransform)
        {
            // 연출 전 잠시 대기
            yield return null;

            Time.timeScale = slowMotionScale;

            if (bossIntroBanner != null)
                bossIntroBanner.SetActive(true);

            yield return new WaitForSecondsRealtime(introBannerSeconds);

            if (bossIntroBanner != null)
                bossIntroBanner.SetActive(false);

            SpawnBoss(playerTransform);

            Time.timeScale = 1f;
        }

        private void SpawnBoss(Transform playerTransform)
        {
            if (boss == null)
            {
                Debug.LogError("[BossFightDirector] boss 레퍼런스가 비어 있습니다.");
                return;
            }

            Vector3 spawnPoint = playerTransform != null
                ? playerTransform.position + Vector3.up * bossSpawnDistance
                : Vector3.zero;

            boss.transform.position = spawnPoint;
            boss.gameObject.SetActive(true);

            _bossEnemy = boss.GetComponent<EnemyBase>();
            if (_bossEnemy != null)
            {
                _bossEnemy.SetXpGemPools(gemPools);
                _bossEnemy.OnDeath += HandleBossDeath;
            }

            // 보스 AI 및 패턴 활성화
            var playerHealthComp = playerTransform != null ? playerTransform.GetComponent<PlayerHealth>() : null;
            boss.Activate(playerHealthComp, meteorPool, summonMobPool, summonEffectPool, gemPools);

            if (bossHealthBar != null)
                bossHealthBar.Show(boss);

            Debug.Log("[BossFightDirector] 보스 등장 및 전투 개시 완료");
        }

        private void HandleBossDeath(EnemyBase _) => StartCoroutine(DeathSequence());

        private IEnumerator DeathSequence()
        {
            Vector3 position = boss != null ? boss.transform.position : Vector3.zero;

            Time.timeScale = slowMotionScale;

            if (bossHealthBar != null)
                bossHealthBar.Hide();

            if (deathExplosionPool != null)
            {
                GameObject explosion = deathExplosionPool.Get(position, Quaternion.identity);
                explosion.transform.localScale = Vector3.one * deathExplosionScale;
            }

            yield return new WaitForSecondsRealtime(deathSeconds);

            // 보스 처치 성공 -> 승리(Victory)로 런 종료 처리
            if (GameManager.Instance != null)
            {
                GameManager.Instance.FinishRun(RunOutcome.Victory);
            }
            else
            {
                // GameManager가 씬에 없을 경우 직접 결과 씬으로 전환
                SceneManager.LoadScene("ResultScene");
            }
        }

        private void HandlePlayerDeath()
        {
            // 1. 런 결과 상태만 패배(Defeat)로 변경 (기존에 백업된 ElapsedTime, Level, KillCount 등은 유지)
            RunResultCarrier.Outcome = RunOutcome.Defeat;

            // 2. 게임 시간 정지
            Time.timeScale = 0f;

            // 3. 게임 오버 패널 활성화
            if (gameOverPanel != null)
            {
                gameOverPanel.gameObject.SetActive(true);
            }
            else
            {
                // 패널이 연결되어 있지 않다면 기존처럼 바로 결과 씬으로 이동
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.FinishRun(RunOutcome.Defeat);
                }
                else
                {
                    SceneManager.LoadScene("ResultScene");
                }
            }
        }
        private void OnDisable()
        {
            if (_bossEnemy != null)
                _bossEnemy.OnDeath -= HandleBossDeath;

            if (_playerHealth != null)
                _playerHealth.OnDeath -= HandlePlayerDeath;
        }
    }
}