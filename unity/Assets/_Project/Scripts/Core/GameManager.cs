using SushiSurvival.Data;
using SushiSurvival.Enemies;
using SushiSurvival.Player;
using SushiSurvival.Weapons;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SushiSurvival.Core
{
    public enum RunState
    {
        CharacterSelect,
        /// <summary>플레이어는 스폰됐지만 대화 #1이 끝날 때까지 전투가 시작되지 않은 상태.</summary>
        Intro,
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
        [SerializeField] private SushiSurvival.UI.GameOverPanel gameOverPanel;
        [SerializeField] private PlayerSpawner playerSpawner;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private WaveDirector waveDirector;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private LevelSystem levelSystem;
        [Tooltip("캐릭터 선택 UI 루트. 런이 시작되면 비활성화된다.")]
        [SerializeField] private GameObject characterSelectPanel;
        [SerializeField] private SushiSurvival.UI.ResultPanel resultPanel;
        [Tooltip("HUD 코너의 플레이어 체력바. 스폰 직후 연결된다.")]
        [SerializeField] private SushiSurvival.UI.HealthBar hudHealthBar;
        [SerializeField] private SushiSurvival.Enemies.Boss.BossDirector bossDirector;
        [Tooltip("호감도 대화 #1을 보여준다. 비워두면 대화 없이 바로 시작한다.")]
        [SerializeField] private AffinityDialogueController affinityDialogueController;
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

        public float EnemyHealthMultiplier =>
            DifficultyCurve.GetMultiplier(ElapsedTime, enemyHealthScalePerMinute, maxEnemyHealthScale);
        public int KillCount { get; private set; }

        private PlayerHealth _playerHealth;
        private PlayerStats _playerStats;
        private Transform _playerTransform;
        private string _activeCharacterName;
        private CharacterData _selectedCharacterData;

        private void Awake() => Instance = this;

        private void Start()
        {
            // 보스 씬으로 진입한 경우 캐릭터 선택창을 건너뛰고 바로 스폰 및 시작
            if (SceneManager.GetActiveScene().name == "BossScene")
            {
                InitializeBossSceneRun();
            }
            else
            {
                CurrentState = RunState.CharacterSelect;

                if (characterSelectPanel != null)
                    characterSelectPanel.SetActive(true);
            }
        }

        private void InitializeBossSceneRun()
        {
            CurrentState = RunState.Playing;
            Time.timeScale = 1f;

            CharacterData selectedCharacter = RunResultCarrier.SelectedCharacterData;
            if (selectedCharacter == null)
            {
                Debug.LogWarning("[GameManager] RunResultCarrier에 선택된 캐릭터 데이터가 없습니다.");
                return;
            }

            GameObject player = playerSpawner.Spawn(selectedCharacter);
            if (player == null) return;

            _playerHealth = player.GetComponent<PlayerHealth>();
            if (_playerHealth != null)
                _playerHealth.OnDeath += HandlePlayerDeath;
            else
                Debug.LogError($"{player.name}: PlayerHealth가 없어 사망 처리를 연결할 수 없습니다.");

            _playerStats = player.GetComponent<PlayerStats>();
            _playerTransform = player.transform;
            _activeCharacterName = selectedCharacter.characterName;

            var weapon = player.GetComponent<WeaponBase>();
            if (levelSystem != null)
                levelSystem.SetPlayer(_playerStats, _playerHealth, weapon, selectedCharacter.portraitSprite);

            if (cameraFollow != null)
                cameraFollow.SetTarget(_playerTransform);

            if (hudHealthBar != null)
            {
                hudHealthBar.gameObject.SetActive(true);
                hudHealthBar.SetTarget(_playerHealth, selectedCharacter.portraitSprite);
            }

            if (characterSelectPanel != null)
                characterSelectPanel.SetActive(false);

            ElapsedTime = 0f;
            KillCount = 0;
            Debug.Log($"[GameManager] 보스 씬 런 자동 시작: {_activeCharacterName}");
        }

        private void Update()
        {
            if (CurrentState != RunState.Playing) return;

            ElapsedTime += Time.deltaTime;

            // 만약 일반 게임 씬("Game")이라면 보스 스폰 시간을 체크한다.
            // 보스 씬("BossScene")에서는 이미 보스전이 시작된 상태이므로 이 체크를 건너 뛴다.
            if (SceneManager.GetActiveScene().name == "GameScene")
            {
                if (ElapsedTime >= bossSpawnTime)
                {
                    EnterBossFight();
                    return;
                }
            }
        }

        public void StartRun(CharacterData characterData)
        {
            // 버튼 연타로 플레이어가 두 번 생성되는 것을 막는다.
            if (CurrentState != RunState.CharacterSelect) return;

            _selectedCharacterData = characterData;

            GameObject player = playerSpawner.Spawn(characterData);
            if (player == null) return;

            _playerHealth = player.GetComponent<PlayerHealth>();
            if (_playerHealth != null)
                _playerHealth.OnDeath += HandlePlayerDeath;
            else
                Debug.LogError($"{player.name}: PlayerHealth가 없어 사망 처리를 연결할 수 없습니다.");

            _playerStats = player.GetComponent<PlayerStats>();
            _playerTransform = player.transform;
            _activeCharacterName = characterData.characterName;

            var weapon = player.GetComponent<WeaponBase>();
            levelSystem.SetPlayer(_playerStats, _playerHealth, weapon, characterData.portraitSprite);

            cameraFollow.SetTarget(_playerTransform);

            if (hudHealthBar != null)
                hudHealthBar.SetTarget(_playerHealth, characterData.portraitSprite);

            // 대화 중에 다시 누르지 못하도록 여기서 바로 끈다.
            if (characterSelectPanel != null)
                characterSelectPanel.SetActive(false);

            if (characterData.affinityDialogue != null && affinityDialogueController != null)
            {
                CurrentState = RunState.Intro;
                affinityDialogueController.Show(
                    characterData.affinityDialogue, characterData.portraitSprite,
                    _playerStats, _playerHealth, BeginCombat);
            }
            else
            {
                BeginCombat();
            }
        }

        private void BeginCombat()
        {
            enemySpawner.StartSpawning(_playerTransform);

            if (waveDirector != null)
                waveDirector.StartTimeline(_playerTransform);

            ElapsedTime = 0f;
            KillCount = 0;
            CurrentState = RunState.Playing;
            Debug.Log($"[GameManager] 런 시작: {_activeCharacterName}");
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

        public void Restart()
        {
            // 실제 씬 파일/Build Settings에 등록된 이름은 "GameScene"이다
            // ("Game"이라는 씬은 존재하지 않아 로드가 조용히 실패했다).
            Time.timeScale = 1f;
            SceneManager.LoadScene("GameScene");
        }

        private void HandlePlayerDeath()
        {
            // 1. 런 결과를 미리 저장해 둘 수 있습니다 (선택 사항)
            RunResultCarrier.Outcome = RunOutcome.Defeat;
            RunResultCarrier.ElapsedTime = ElapsedTime;
            RunResultCarrier.Level = levelSystem.CurrentLevel;
            RunResultCarrier.KillCount = KillCount;
            RunResultCarrier.Augments = AugmentTally.Summarize(levelSystem.PickedAugments);

            // 2. 게임 일시 정지
            Time.timeScale = 0f;

            // 3. 패배 패널 활성화
            if (gameOverPanel != null)
            {
                gameOverPanel.gameObject.SetActive(true);
                // 만약 패널 내부에 별도의 Show 메서드가 있다면 아래와 같이 호출할 수도 있습니다.
                // gameOverPanel.Show(); 
            }
            else
            {
                // 패널이 연결되어 있지 않다면 기존처럼 바로 결과 씬으로 이동
                FinishRun(RunOutcome.Defeat);
            }
        }

        public void FinishRun(RunOutcome outcome)
        {
            // 1. RunResultCarrier에 최종 결과를 담는다
            RunResultCarrier.Outcome = outcome;
            RunResultCarrier.ElapsedTime = ElapsedTime;
            RunResultCarrier.Level = levelSystem.CurrentLevel;
            RunResultCarrier.KillCount = KillCount;
            RunResultCarrier.Augments = AugmentTally.Summarize(levelSystem.PickedAugments);

            // 2. 씬을 넘기기 전 시간 정지를 반드시 해제한다
            Time.timeScale = 1f;

            // 3. 결과 씬으로 넘어간다
            SceneManager.LoadScene("ResultScene");
        }

        public void EnterBossFight()
        {
            RunResultCarrier.SelectedCharacterData = _selectedCharacterData;
            RunResultCarrier.ElapsedTime = ElapsedTime;
            RunResultCarrier.KillCount = KillCount;

            if (_playerHealth != null)
            {
                RunResultCarrier.PlayerCurrentHealth = _playerHealth.CurrentHealth;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene("BossScene"); // 또는 해당 보스 씬 이름
        }
        private void OnDisable()
        {
            if (_playerHealth != null)
                _playerHealth.OnDeath -= HandlePlayerDeath;
        }
    }
}