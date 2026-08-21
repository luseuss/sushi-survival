using UnityEngine;
using UnityEngine.SceneManagement;
using SushiSurvival.Data;
using SushiSurvival.Enemies;
using SushiSurvival.Player;
using SushiSurvival.Weapons;

namespace SushiSurvival.Core
{
    public enum RunState
    {
        CharacterSelect,
        Playing,
        Result
    }

    public enum RunOutcome
    {
        Victory,
        Defeat
    }

    public class GameManager : MonoBehaviour
    {
        [SerializeField] private PlayerSpawner playerSpawner;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private WaveDirector waveDirector;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private LevelSystem levelSystem;
        [Tooltip("캐릭터 선택 UI 루트. 런이 시작되면 비활성화된다.")]
        [SerializeField] private GameObject characterSelectPanel;
        [SerializeField] private SushiSurvival.UI.ResultPanel resultPanel;
        [SerializeField] private SushiSurvival.Enemies.Boss.BossDirector bossDirector;
        [Tooltip("보스가 등장하는 시각(초). 5:00 = 300")]
        [SerializeField] private float bossSpawnTime = 300f;

        [Header("시간 경과 난이도")]
        [Tooltip("1분마다 잡몹 체력에 더해지는 비율. 0.6이면 1분에 +60%.")]
        [SerializeField] private float enemyHealthScalePerMinute = 0.6f;
        [Tooltip("잡몹 체력 배율의 상한. 보스전이 길어져도 여기서 멈춘다.")]
        [SerializeField] private float maxEnemyHealthScale = 4f;

        public static GameManager Instance { get; private set; }

        public RunState CurrentState { get; private set; } = RunState.CharacterSelect;
        public float TotalExperience { get; private set; }
        public float ElapsedTime { get; private set; }
        public float BossSpawnTime => bossSpawnTime;

        /// <summary>
        /// 지금 스폰되는 잡몹에 곱할 체력 배율. EnemyBase가 OnEnable에서 읽는다.
        /// </summary>
        public float EnemyHealthMultiplier =>
            DifficultyCurve.GetMultiplier(ElapsedTime, enemyHealthScalePerMinute, maxEnemyHealthScale);
        public int KillCount { get; private set; }

        private PlayerHealth _playerHealth;
        private PlayerStats _playerStats;

        private void Awake() => Instance = this;

        private void Start()
        {
            CurrentState = RunState.CharacterSelect;

            if (characterSelectPanel != null)
                characterSelectPanel.SetActive(true);
        }

        private void Update()
        {
            if (CurrentState != RunState.Playing) return;

            // 스케일 적용 시간을 쓰므로 레벨업 팝업이 열린 동안에는 타이머가 멈춘다.
            ElapsedTime += Time.deltaTime;

            // 5:00은 이제 승리가 아니라 보스 등장 시각이다. 승리는 오직 보스
            // 처치에서만 나온다. BeginIntro는 스스로 중복 호출을 막는다.
            if (ElapsedTime >= bossSpawnTime && bossDirector != null)
                bossDirector.BeginIntro(_playerHealth);
        }

        public void StartRun(CharacterData characterData)
        {
            // 버튼 연타로 플레이어가 두 번 생성되는 것을 막는다.
            if (CurrentState != RunState.CharacterSelect) return;

            GameObject player = playerSpawner.Spawn(characterData);
            if (player == null) return;

            _playerHealth = player.GetComponent<PlayerHealth>();
            if (_playerHealth != null)
                _playerHealth.OnDeath += HandlePlayerDeath;
            else
                Debug.LogError($"{player.name}: PlayerHealth가 없어 사망 처리를 연결할 수 없습니다.");

            _playerStats = player.GetComponent<PlayerStats>();

            var weapon = player.GetComponent<WeaponBase>();
            levelSystem.SetPlayer(_playerStats, _playerHealth, weapon);

            cameraFollow.SetTarget(player.transform);
            enemySpawner.StartSpawning(player.transform);

            if (waveDirector != null)
                waveDirector.StartTimeline(player.transform);

            if (characterSelectPanel != null)
                characterSelectPanel.SetActive(false);

            ElapsedTime = 0f;
            KillCount = 0;
            CurrentState = RunState.Playing;
            Debug.Log($"[GameManager] 런 시작: {characterData.characterName}");
        }

        public void AddExperience(float amount)
        {
            if (CurrentState != RunState.Playing) return;

            float multiplier = _playerStats != null ? _playerStats.GetValue(StatType.ExpGain) : 1f;
            float gained = amount * multiplier;

            TotalExperience += gained;
            levelSystem.AddExperience(gained);
        }

        public void RegisterKill()
        {
            if (CurrentState != RunState.Playing) return;

            KillCount++;
        }

        /// <summary>
        /// 씬을 다시 열어 런의 모든 흔적을 지운다. 세이브가 없는 원런 구조라
        /// 다음 판으로 넘길 상태가 하나도 없어서 이 방식이 가장 안전하다.
        /// </summary>
        public void Restart()
        {
            // timeScale은 씬을 다시 로드해도 초기화되지 않는다. 직접 되돌린다.
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void HandlePlayerDeath() => FinishRun(RunOutcome.Defeat);

        /// <summary>
        /// 런을 끝낸다. 패배는 PlayerHealth.OnDeath에서, 승리는 BossDirector의
        /// 격파 연출이 끝난 뒤에 호출한다.
        /// </summary>
        public void FinishRun(RunOutcome outcome)
        {
            // 승리와 패배가 같은 프레임에 성립할 수 있다. 먼저 성립한 것만 처리한다.
            if (CurrentState != RunState.Playing) return;

            CurrentState = RunState.Result;
            enemySpawner.StopSpawning();

            if (waveDirector != null)
                waveDirector.StopTimeline();

            // 결과 화면 동안에는 적도 젬도 움직이지 않게 멈춘다.
            Time.timeScale = 0f;

            if (resultPanel != null)
            {
                resultPanel.Show(
                    outcome,
                    ElapsedTime,
                    levelSystem.CurrentLevel,
                    KillCount,
                    AugmentTally.Summarize(levelSystem.PickedAugments));
            }
            else
            {
                Debug.LogError($"{name}: resultPanel이 비어 있어 결과 화면을 띄울 수 없습니다.");
            }

            Debug.Log($"[GameManager] 런 종료: {outcome} / 생존 {RunClock.FormatElapsed(ElapsedTime)} / " +
                      $"Lv{levelSystem.CurrentLevel} / 처치 {KillCount}");
        }

        private void OnDisable()
        {
            if (_playerHealth != null)
                _playerHealth.OnDeath -= HandlePlayerDeath;
        }
    }
}
