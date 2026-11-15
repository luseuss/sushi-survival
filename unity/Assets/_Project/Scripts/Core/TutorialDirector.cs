using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Data;
using SushiSurvival.Enemies;
using SushiSurvival.Player;
using SushiSurvival.UI;
using SushiSurvival.Weapons;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 연습 플레이(튜토리얼) 진행자. GameScene에 두면, 메인화면에서 연습을 골랐을 때(TutorialState.Requested)만 동작한다.
    /// 아델린으로 시작해 이동 → 자동 공격 → 레벨업 선택 → 와사비 → 스킬 클릭 순서로 직접 해 보게 하고,
    /// 각 단계가 끝나면 다음 단계의 적·레벨업 창을 내보낸다. 건너뛰기나 끝에서 스토리 장면으로 넘어간다.
    /// </summary>
    public class TutorialDirector : MonoBehaviour
    {
        [Tooltip("연습에 쓸 캐릭터. 스킬 클릭 단계가 아델린의 우산이라 아델린을 연결한다.")]
        [SerializeField] private CharacterData characterData;
        [Tooltip("이동 단계에서 캐릭터로부터 목표 표시까지의 거리.")]
        [SerializeField] private float moveMarkerDistance = 5f;
        [Tooltip("공격 단계에서 적이 나타나는 캐릭터 둘레 거리.")]
        [SerializeField] private float attackSpawnRadius = 5f;
        [Tooltip("안내 띠의 버튼(건너뛰기·시작하기)에 쓸 게임의 기본 버튼 그림. 비우면 단색 버튼이 된다.")]
        [SerializeField] private Sprite buttonSprite;
        [Tooltip("연습이 끝나면 넘어갈 장면.")]
        [SerializeField] private string nextScene = "StoryScene";
        [Tooltip("끝 단계에서 아무것도 누르지 않아도 이 시간(초) 뒤에 넘어간다.")]
        [SerializeField] private float autoFinishSeconds = 8f;

        private GameManager _gameManager;
        private LevelSystem _levelSystem;
        private EnemySpawner _spawner;
        private RotatingUmbrellaWeapon _umbrella;
        private Transform _player;
        private TutorialOverlay _overlay;
        private GameObject _marker;

        private TutorialStep _step;
        private bool _active;
        private bool _finished;
        private bool _sawPopup;
        private bool _warningOpen;
        private int _killsAtStart;
        private int _shownKills = -1;
        private float _doneAt;

        private static Sprite _ringSprite;

        private IEnumerator Start()
        {
            // 연습을 고르지 않은 평소 판에서는 아무 일도 하지 않는다.
            if (!TutorialState.ConsumeRequest())
            {
                enabled = false;
                yield break;
            }

            // GameManager.Start가 캐릭터 선택창을 켠 뒤에 연습을 시작한다.
            yield return null;
            Begin();
        }

        private void Begin()
        {
            _gameManager = GameManager.Instance;
            if (_gameManager == null || characterData == null)
            {
                Debug.LogError($"{name}: GameManager 또는 characterData가 없어 연습 플레이를 시작할 수 없습니다.");
                enabled = false;
                return;
            }

            _gameManager.StartTutorialRun(characterData);
            if (!_gameManager.IsTutorial)
            {
                enabled = false;
                return;
            }

            _levelSystem = FindObjectOfType<LevelSystem>(true);
            _spawner = FindObjectOfType<EnemySpawner>(true);

            var health = FindObjectOfType<PlayerHealth>();
            if (health != null)
            {
                _player = health.transform;
                _umbrella = health.GetComponent<RotatingUmbrellaWeapon>();
                if (_umbrella != null) _umbrella.SpinStarted += OnUmbrellaSpun;
            }

            // 가위바위보는 연습에서 한 번은 꼭 이기게 한다(이겨야 무기가 변신하는 걸 볼 수 있다).
            var rps = FindObjectOfType<RockPaperScissorsPanel>(true);
            if (rps != null) rps.SetWinChance(1f);

            Text anyText = FindObjectOfType<Text>(true);
            _overlay = TutorialOverlay.Create(anyText != null ? anyText.font : null, buttonSprite, Finish, Finish);

            _active = true;
            EnterStep(TutorialStep.Move);
        }

        private void OnDestroy()
        {
            if (_umbrella != null) _umbrella.SpinStarted -= OnUmbrellaSpun;
            if (_marker != null) Destroy(_marker);
            if (_overlay != null) Destroy(_overlay.gameObject);
        }

        private void Update()
        {
            if (!_active || _finished || _player == null) return;

            // 레벨업 카드 팝업이 떠 있는 동안에는 안내 띠를 아래로 내려 팝업 글자를 가리지 않게 한다.
            _overlay.SetDock(LevelUpPanel.Instance != null && LevelUpPanel.Instance.IsOpen);

            switch (_step)
            {
                case TutorialStep.Move: UpdateMove(); break;
                case TutorialStep.Attack: UpdateAttack(); break;
                case TutorialStep.LevelUp: UpdateLevelUp(); break;
                case TutorialStep.Wasabi: UpdateWasabi(); break;
                case TutorialStep.Done:
                    if (Time.unscaledTime - _doneAt >= autoFinishSeconds) Finish();
                    break;
            }
        }

        private void EnterStep(TutorialStep step)
        {
            _step = step;
            _sawPopup = false;
            _warningOpen = false;
            _shownKills = -1;
            _overlay.SetStep(TutorialStepLogic.Number(step), TutorialStepLogic.TotalSteps,
                             TutorialStepLogic.Instruction(step, 0));

            switch (step)
            {
                case TutorialStep.Move:
                    _marker = CreateMarker(_player.position + (Vector3)(new Vector2(1f, 0.6f).normalized * moveMarkerDistance));
                    break;
                case TutorialStep.Attack:
                    _killsAtStart = _gameManager.KillCount;
                    if (_spawner != null) _spawner.SpawnBurst(_player, TutorialStepLogic.KillGoal, attackSpawnRadius);
                    break;
                case TutorialStep.Skill:
                    // 우산이 맞힐 적을 몇 마리 내보낸다.
                    if (_spawner != null) _spawner.SpawnBurst(_player, 4, 4f);
                    break;
                case TutorialStep.Done:
                    _overlay.SetStep(0, TutorialStepLogic.TotalSteps, TutorialStepLogic.Instruction(step, 0));
                    _overlay.ShowDone();
                    _doneAt = Time.unscaledTime;
                    break;
            }
        }

        private void UpdateMove()
        {
            if (_marker == null) return;

            float pulse = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 6f);
            _marker.transform.localScale = Vector3.one * (TutorialStepLogic.MarkerRadius * 2f * pulse);

            if (!TutorialStepLogic.ReachedMarker(_player.position, _marker.transform.position, TutorialStepLogic.MarkerRadius))
                return;

            Destroy(_marker);
            _marker = null;
            EnterStep(TutorialStepLogic.Next(_step));
        }

        private void UpdateAttack()
        {
            int kills = _gameManager.KillCount - _killsAtStart;
            if (kills != _shownKills)
            {
                _shownKills = kills;
                _overlay.SetStep(TutorialStepLogic.Number(_step), TutorialStepLogic.TotalSteps,
                                 TutorialStepLogic.Instruction(_step, kills));
            }

            if (TutorialStepLogic.KillGoalReached(kills))
                EnterStep(TutorialStepLogic.Next(_step));
        }

        // 레벨업 단계: 팝업을 띄우고, 카드를 골라 팝업이 닫히면 다음 단계로 간다.
        private void UpdateLevelUp()
        {
            if (!ShowLevelUpPopup()) return;

            if (!_levelSystem.IsShowingPopup)
                EnterStep(TutorialStepLogic.Next(_step));
        }

        // 와사비 단계: 와사비 버튼으로 가위바위보를 해서 무기가 변하면(우산이 켜지면) 다음 단계로 간다.
        // 카드를 골라 버렸다면 한 번 더 레벨업 창을 띄워 다시 안내한다.
        private void UpdateWasabi()
        {
            if (_warningOpen) return;
            if (!ShowLevelUpPopup()) return;
            if (_levelSystem.IsShowingPopup) return;

            if (_umbrella != null && _umbrella.enabled)
            {
                EnterStep(TutorialStepLogic.Next(_step));
                return;
            }

            // 와사비 대신 카드를 골랐다: 경고창으로 알리고, 확인을 누르면 레벨업 창을 다시 띄워 준다.
            _warningOpen = true;
            Time.timeScale = 0f;
            _overlay.ShowWarning(TutorialStepLogic.WasabiWarning, OnWarningConfirmed);
        }

        private void OnWarningConfirmed()
        {
            Time.timeScale = 1f;
            _warningOpen = false;
            _sawPopup = false; // 다음 Update에서 레벨업 창을 다시 띄운다.
        }

        // 레벨업 팝업을 띄우고 떠 있는 것을 확인했으면 true. 아직이면 띄우기를 시도한다(적이 있어 시간이 흐르는 중에도 된다).
        private bool ShowLevelUpPopup()
        {
            if (_levelSystem == null) return false;

            if (!_sawPopup)
            {
                if (!_levelSystem.IsShowingPopup) _levelSystem.CheatLevelUp();
                if (_levelSystem.IsShowingPopup) _sawPopup = true;
            }

            return _sawPopup;
        }

        private void OnUmbrellaSpun()
        {
            if (_step == TutorialStep.Skill) EnterStep(TutorialStep.Done);
        }

        /// <summary>건너뛰기나 시작하기: 이 방문자가 안내를 본 것으로 기록하고 다음 장면으로 간다.</summary>
        private void Finish()
        {
            if (_finished) return;
            _finished = true;

            TutorialState.MarkSeen();
            TutorialState.EndTutorial();
            Time.timeScale = 1f;
            SceneTransition.Go(nextScene);
        }

        private GameObject CreateMarker(Vector3 position)
        {
            var go = new GameObject("TutorialMarker");
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = RingSprite();
            renderer.color = new Color(1f, 0.85f, 0.25f, 0.9f);
            renderer.sortingOrder = 5;
            go.transform.localScale = Vector3.one * (TutorialStepLogic.MarkerRadius * 2f);
            return go;
        }

        // 속이 비치는 금색 고리 그림(지름 1유닛). 그림 파일 없이 코드로 만든다.
        private static Sprite RingSprite()
        {
            if (_ringSprite != null) return _ringSprite;

            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius)) / radius;
                    float alpha = d > 1f ? 0f : d > 0.82f ? 1f : 0.2f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            _ringSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            return _ringSprite;
        }
    }
}
