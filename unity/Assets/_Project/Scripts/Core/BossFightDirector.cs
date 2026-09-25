using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using SushiSurvival.Core;
using SushiSurvival.Data;
using SushiSurvival.Pickups;
using SushiSurvival.Player;
using SushiSurvival.Weapons;

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
        [Tooltip("레벨업 팝업이 이 씬에서도 뜨려면 스폰된 플레이어를 여기 넘겨야 한다.")]
        [SerializeField] private LevelSystem levelSystem;
        [Tooltip("보스 등장 직후 보스·캐릭터 대화를 재생할 대사창. 비우면 대화 없이 바로 전투.")]
        [SerializeField] private AffinityDialogueController encounterDialogue;

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
        [Tooltip("낙하 시작 위치가 화면 위 끝보다 이만큼 더 위에 있다(월드 단위).")]
        [SerializeField] private float dropStartMargin = 2f;
        [Tooltip("카메라가 보스 착지점으로 움직이는 속도(클수록 빠름)와 기다리는 시간(초, 실시간).")]
        [SerializeField] private float cameraPanSpeed = 4f;
        [SerializeField] private float cameraPanSeconds = 1f;
        [Tooltip("보스가 떨어지는 시간(초, 실시간).")]
        [SerializeField] private float dropSeconds = 0.5f;
        [Tooltip("착지 충격으로 화면이 흔들리는 세기(월드 단위)와 시간(초, 실시간).")]
        [SerializeField] private float impactShakeMagnitude = 0.5f;
        [SerializeField] private float impactShakeSeconds = 0.6f;
        [Tooltip("착지 순간 걷혀 나갈 초록 땅(사막 땅 위에 덮여 있다). 비우면 이 연출 없이 처음부터 사막이다.")]
        [SerializeField] private Tilemap groundGrass;
        [Tooltip("초록 땅을 채우는 스트리머. 걷힘이 시작되면 꺼서, 지운 땅이 다시 채워지지 않게 한다.")]
        [SerializeField] private MonoBehaviour groundGrassStreamer;
        [Tooltip("초록 땅이 착지점에서 끝까지 걷히는 시간(초, 실시간).")]
        [SerializeField] private float groundTransitionSeconds = 1.2f;
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
            var weapon = PlayerWeaponResolver.GetActive(playerObj);

            if (cameraFollow != null)
                cameraFollow.SetTarget(playerTransform);

            // 레벨업 팝업(3택)이 이 씬에서도 뜨려면 LevelSystem에 플레이어를
            // 알려줘야 한다 — GameManager는 더 이상 이 씬에서 플레이어를
            // 스폰하지 않으므로(중복 스폰 버그 수정) 여기서 직접 배선한다.
            if (levelSystem != null)
            {
                levelSystem.SetPlayer(playerStats, _playerHealth, weapon, selectedCharacter.portraitSprite);

                // GameScene에서 쌓은 레벨·경험치·증강을 복원한다. 안 하면
                // 5분간의 성장이 전부 사라지고 Lv1 기본 스탯으로 다시 시작한다.
                if (RunResultCarrier.CurrentLevel >= 1)
                {
                    levelSystem.RestoreProgress(
                        RunResultCarrier.CurrentLevel,
                        RunResultCarrier.CurrentExperience,
                        RunResultCarrier.PickedAugments,
                        RunResultCarrier.ExternalBuffs);
                }
            }

            // 이전 씬(GameScene)에서 가져온 체력을 그대로 반영한다. 안 하면
            // 항상 풀피로 보스전이 시작돼 GameScene에서 입은 피해가 사라진다.
            // 최대체력 증강이 RestoreProgress로 복원된 뒤에 해야 한다 — 먼저 하면
            // 최대치가 아직 낮아서 그 값으로 잘려 나간다.
            if (_playerHealth != null && RunResultCarrier.PlayerCurrentHealth > 0f)
                _playerHealth.SetHealth(RunResultCarrier.PlayerCurrentHealth);

            // 무기 강화 레벨도 같은 이유로 복원한다. 와사비로 우산으로 바뀐
            // 상태였다면 먼저 우산을 켜야 레벨 복원 대상이 우산이 된다.
            if (weapon != null)
            {
                if (RunResultCarrier.WasabiWeaponConverted && weapon is EggFanWeapon eggWeapon)
                {
                    var umbrella = eggWeapon.GetComponent<RotatingUmbrellaWeapon>();
                    if (umbrella != null)
                    {
                        eggWeapon.enabled = false;
                        umbrella.enabled = true;
                        weapon = umbrella;
                    }
                    else
                    {
                        Debug.LogError($"{eggWeapon.name}: RotatingUmbrellaWeapon 컴포넌트가 없어 우산 상태를 복원할 수 없습니다.");
                    }
                }

                while (weapon.CurrentLevel < RunResultCarrier.WeaponLevel && weapon.CanLevelUp)
                    weapon.LevelUp();
            }

            if (hudHealthBar != null && _playerHealth != null)
            {
                hudHealthBar.gameObject.SetActive(true);
                hudHealthBar.SetTarget(_playerHealth, selectedCharacter.portraitSprite);
            }

            if (_playerHealth != null)
                _playerHealth.OnDeath += HandlePlayerDeath;

            if (bossIntroBanner != null)
                bossIntroBanner.SetActive(false);

            StartCoroutine(IntroSequence(playerTransform, selectedCharacter.affinityDialogue?.bossEncounterLines));
        }

        /// <summary>
        /// 등장 연출: 카메라가 보스 착지점으로 이동 → 보스가 하늘에서 낙하 → 착지 충격(화면 흔들림)과
        /// 배너 → 보스·캐릭터 대화 → 카메라가 플레이어로 돌아오며 전투 시작.
        /// 연출 내내 게임은 정지(timeScale 0)이고 모든 시간은 실시간으로 잰다.
        /// </summary>
        private IEnumerator IntroSequence(Transform playerTransform, StoryLine[] encounterLines)
        {
            // 다른 컴포넌트의 Start가 끝나길 한 프레임 기다린다.
            yield return null;

            Time.timeScale = 0f;

            Vector3 landing = playerTransform != null
                ? playerTransform.position + Vector3.up * bossSpawnDistance
                : Vector3.zero;

            Camera cam = cameraFollow != null ? cameraFollow.GetComponent<Camera>() : Camera.main;
            float startHeight = BossEntranceLogic.DropStartHeight(cam != null ? cam.orthographicSize : 5f, dropStartMargin);

            PlaceBoss(landing + Vector3.up * startHeight);

            if (cameraFollow != null)
                cameraFollow.FocusOn(landing, cameraPanSpeed);

            yield return new WaitForSecondsRealtime(cameraPanSeconds);

            float elapsed = 0f;
            while (elapsed < dropSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                boss.transform.position = BossEntranceLogic.DropPosition(
                    landing, startHeight, BossEntranceLogic.Progress(elapsed, dropSeconds));
                yield return null;
            }

            boss.transform.position = landing;

            if (groundGrass != null)
                StartCoroutine(GroundTransition(landing));

            if (bossIntroBanner != null)
                bossIntroBanner.SetActive(true);

            elapsed = 0f;
            while (elapsed < introBannerSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                if (cameraFollow != null)
                    cameraFollow.SetShakeOffset(BossEntranceLogic.ShakeOffset(
                        elapsed, impactShakeSeconds, impactShakeMagnitude, Random.insideUnitCircle));
                yield return null;
            }

            if (cameraFollow != null)
                cameraFollow.SetShakeOffset(Vector2.zero);

            if (bossIntroBanner != null)
                bossIntroBanner.SetActive(false);

            if (encounterDialogue != null && encounterLines != null && encounterLines.Length > 0)
            {
                bool done = false;
                encounterDialogue.PlayNarration(encounterLines, () => done = true);
                while (!done) yield return null;
            }

            if (cameraFollow != null)
                cameraFollow.ClearFocus();

            Time.timeScale = 1f;

            ActivateBoss(playerTransform);
        }

        /// <summary>착지점에서부터 초록 땅 타일을 물결처럼 걷어 아래의 사막 땅을 드러낸다.</summary>
        private IEnumerator GroundTransition(Vector3 center)
        {
            if (groundGrassStreamer != null)
                groundGrassStreamer.enabled = false;

            var positions = new List<Vector3Int>();
            var distances = new List<float>();

            foreach (Vector3Int cell in groundGrass.cellBounds.allPositionsWithin)
            {
                if (!groundGrass.HasTile(cell)) continue;

                positions.Add(cell);
                distances.Add(Vector3.Distance(groundGrass.GetCellCenterWorld(cell), center));
            }

            var order = new Vector3Int[positions.Count];
            var sortedDistances = new float[positions.Count];
            var keys = distances.ToArray();
            var indices = new int[positions.Count];
            for (int i = 0; i < indices.Length; i++) indices[i] = i;
            System.Array.Sort(keys, indices);
            for (int i = 0; i < indices.Length; i++)
            {
                order[i] = positions[indices[i]];
                sortedDistances[i] = keys[i];
            }

            float maxDistance = sortedDistances.Length > 0 ? sortedDistances[sortedDistances.Length - 1] : 0f;
            int next = 0;
            float elapsed = 0f;

            while (next < order.Length)
            {
                elapsed += Time.unscaledDeltaTime;

                float radius = GroundTransitionLogic.RadiusAt(elapsed, groundTransitionSeconds, maxDistance);
                int end = GroundTransitionLogic.AdvanceIndex(sortedDistances, next, radius);

                for (; next < end; next++)
                    groundGrass.SetTile(order[next], null);

                if (elapsed >= groundTransitionSeconds) end = order.Length;
                for (; next < end; next++)
                    groundGrass.SetTile(order[next], null);

                yield return null;
            }
        }

        private void PlaceBoss(Vector3 position)
        {
            if (boss == null)
            {
                Debug.LogError("[BossFightDirector] boss 레퍼런스가 비어 있습니다.");
                return;
            }

            boss.transform.position = position;
            boss.gameObject.SetActive(true);

            _bossEnemy = boss.GetComponent<EnemyBase>();
            if (_bossEnemy != null)
                _bossEnemy.OnDeath += HandleBossDeath;
        }

        private void ActivateBoss(Transform playerTransform)
        {
            if (boss == null) return;

            if (_bossEnemy != null)
                _bossEnemy.SetXpGemPools(gemPools);

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
                Debug.LogError("[BossFightDirector] GameManager.Instance가 없어 결과를 표시할 수 없습니다.");
            }
        }

        private void HandlePlayerDeath()
        {
            // RunResultCarrier.Level/Augments는 FinishRun()에서만 채워진다.
            // EnterBossFight()가 채우는 건 ElapsedTime/KillCount/PlayerCurrentHealth
            // /CurrentLevel/CurrentExperience/PickedAugments뿐이라("Level"·
            // "Augments"와는 다른 필드), 여기서 직접 안 채우면 ResultPanel이
            // Augments를 null로 순회하다 죽는다.
            RunResultCarrier.Outcome = RunOutcome.Defeat;

            if (GameManager.Instance != null)
            {
                RunResultCarrier.ElapsedTime = GameManager.Instance.ElapsedTime;
                RunResultCarrier.KillCount = GameManager.Instance.KillCount;
            }

            if (levelSystem != null)
            {
                RunResultCarrier.Level = levelSystem.CurrentLevel;
                RunResultCarrier.Augments = AugmentTally.Summarize(levelSystem.PickedAugments);
            }

            // 게임 시간 정지
            Time.timeScale = 0f;

            // 3. 게임 오버 패널 활성화
            if (gameOverPanel != null)
            {
                gameOverPanel.gameObject.SetActive(true);
            }
            else
            {
                // 패널이 연결되어 있지 않다면 바로 결과를 표시한다
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.FinishRun(RunOutcome.Defeat);
                }
                else
                {
                    Debug.LogError("[BossFightDirector] GameManager.Instance가 없어 결과를 표시할 수 없습니다.");
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