# 슬라이스 2c: 웨이브 타임라인 + 중형몹 + 결과 화면 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 5분 타이머와 2:00·4:00 중형몹 등장, 승리/패배 결과 화면, 씬 리로드
재시작을 붙여 한 판을 완결시킨다.

**Architecture:** `GameManager`가 런 경과 시간과 승패 전환을 소유하고,
`WaveDirector`가 그 시각을 보며 예약된 스폰 이벤트를 발화한다. 젬은 3종으로 늘려
`XPGemPoolSet`이 등급별 풀을 관리한다. 시각 포맷·이벤트 발화 판정·증강 집계 같은
순수 로직은 EditMode 유닛 테스트로 TDD하고, MonoBehaviour 통합 동작은 Play 모드
수동 테스트로 확인한다.

**Tech Stack:** Unity 2022.3.62f3 LTS, Built-in 2D, Input System, uGUI,
Unity Test Framework(EditMode, NUnit)

**Spec:** [docs/superpowers/specs/2026-08-20-slice2c-run-structure-design.md](../specs/2026-08-20-slice2c-run-structure-design.md)

## Global Constraints

- Unity 버전: 2022.3.62f3 고정. 입력은 새 Input System만. 렌더는 Built-in.
- UI 버튼·텍스트는 반드시 `Button (Legacy)` / `Text (Legacy)`를 쓴다
  (TextMeshPro 미설치).
- 수치는 코드에 하드코딩하지 않고 ScriptableObject 에셋 또는 인스펙터 필드에 둔다.
- 런 길이 기본값: **300초(5:00)**. 인스펙터 노출.
- 웨이브 이벤트 기본값: **2:00 중형몹 1마리, 4:00 중형몹 1마리**.
- 몬스터 수치: 잡몹A 12/5/2(흰 젬), 잡몹B 20/5/1.5(흰 젬),
  **중형몹 200/12/1.2(금 젬)**.
- 젬 값: 흰 1XP / 갈 5XP / 금 10XP.
- **런 타이머는 `Time.deltaTime`(스케일 적용)을 쓴다.** 레벨업 팝업이 열려 있는
  동안(`timeScale = 0`) 타이머가 멈추는 것은 의도된 동작이다. 기획서가 "1회차
  플레이타임 약 5~7분"이라고 적은 것은 5:00 타이머에 선택 시간이 더해지기 때문이다.
- **`Time.timeScale`은 씬을 다시 로드해도 초기화되지 않는다.** 재시작 직전에
  반드시 직접 1로 되돌린다.
- **Unity Editor가 열려 있으면 배치 모드 명령이 실패한다.** 코드 Task는 닫고,
  Editor Task는 열고 진행한다.
- **테스트 실행 명령에 `-quit`을 붙이지 않는다.** 표준 명령:

```bash
"/c/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" \
  -batchmode -nographics \
  -projectPath "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity" \
  -runTests -testPlatform EditMode \
  -testResults "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity/TestResults.xml" \
  -logFile "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity_test.log"
```

  결과 확인:

```bash
grep -oE '<test-run[^>]*' "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity/TestResults.xml"
grep -iE "error CS" "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity_test.log" | head -10
```

- 이 플랜 시작 시점의 기존 테스트는 **94개**다. 각 Task 후 총계가 줄지 않아야 한다.

---

## Phase 1 — 타이머 + 런 종료

### Task 1: RunClock 시각 포맷 (TDD)

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/RunClock.cs`
- Test: `unity/Assets/Tests/EditMode/RunClockTests.cs`

**Interfaces:**
- Produces: `RunClock.FormatRemaining(float elapsed, float duration) -> string`,
  `RunClock.FormatElapsed(float seconds) -> string`.
  Task 3(`RunTimerDisplay`)과 Task 8(`ResultPanel`)이 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/RunClockTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class RunClockTests
    {
        [Test]
        public void FormatRemaining_ShowsFullDuration_AtStart()
        {
            Assert.AreEqual("5:00", RunClock.FormatRemaining(0f, 300f));
        }

        [Test]
        public void FormatRemaining_CountsDownByMinute()
        {
            Assert.AreEqual("4:00", RunClock.FormatRemaining(60f, 300f));
        }

        [Test]
        public void FormatRemaining_RollsOverAtSixtySeconds()
        {
            // 남은 60초가 "0:60"으로 나오면 실패다.
            Assert.AreEqual("1:00", RunClock.FormatRemaining(240f, 300f));
        }

        [Test]
        public void FormatRemaining_RoundsUp_SoLastSecondIsVisible()
        {
            Assert.AreEqual("0:01", RunClock.FormatRemaining(299.5f, 300f));
        }

        [Test]
        public void FormatRemaining_ZeroAtExactEnd()
        {
            Assert.AreEqual("0:00", RunClock.FormatRemaining(300f, 300f));
        }

        [Test]
        public void FormatRemaining_NeverGoesNegative()
        {
            Assert.AreEqual("0:00", RunClock.FormatRemaining(350f, 300f));
        }

        [Test]
        public void FormatRemaining_ClampsToDuration_WhenElapsedIsNegative()
        {
            Assert.AreEqual("5:00", RunClock.FormatRemaining(-5f, 300f));
        }

        [Test]
        public void FormatElapsed_ZeroAtStart()
        {
            Assert.AreEqual("0:00", RunClock.FormatElapsed(0f));
        }

        [Test]
        public void FormatElapsed_FloorsPartialSecond()
        {
            Assert.AreEqual("0:59", RunClock.FormatElapsed(59.9f));
        }

        [Test]
        public void FormatElapsed_RollsOverAtSixtySeconds()
        {
            Assert.AreEqual("1:00", RunClock.FormatElapsed(60f));
        }

        [Test]
        public void FormatElapsed_FormatsMinutesAndSeconds()
        {
            Assert.AreEqual("3:42", RunClock.FormatElapsed(222f));
        }

        [Test]
        public void FormatElapsed_ClampsNegativeToZero()
        {
            Assert.AreEqual("0:00", RunClock.FormatElapsed(-5f));
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Global Constraints의 표준 명령 실행.
Expected: `RunClock`이 없어서 컴파일 실패.

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/Core/RunClock.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Core
{
    public static class RunClock
    {
        /// <summary>
        /// 남은 시간을 "4:23" 형식으로. 올림이라 마지막 1초가 화면에 보인다.
        /// </summary>
        public static string FormatRemaining(float elapsed, float duration)
        {
            float remaining = Mathf.Clamp(duration - elapsed, 0f, duration);
            return Format(Mathf.CeilToInt(remaining));
        }

        /// <summary>
        /// 경과 시간을 "3:42" 형식으로. 내림이라 지나간 시간만 표시된다.
        /// </summary>
        public static string FormatElapsed(float seconds)
        {
            float safe = Mathf.Max(0f, seconds);
            return Format(Mathf.FloorToInt(safe));
        }

        private static string Format(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes}:{seconds:00}";
        }
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Expected: 총계 **106개** 전부 통과.

---

### Task 2: GameManager 런 상태·타이머·승패

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Core/GameManager.cs` (전면 교체)

**Interfaces:**
- Consumes: `PlayerSpawner.Spawn`, `CameraFollow.SetTarget`,
  `EnemySpawner.StartSpawning` / `StopSpawning`, `LevelSystem.SetPlayer` /
  `AddExperience` / `CurrentLevel` (모두 기존).
- Produces: `enum RunState { CharacterSelect, Playing, Result }`,
  `enum RunOutcome { Victory, Defeat }`,
  `GameManager.ElapsedTime` / `RunDuration` / `KillCount` (프로퍼티),
  `GameManager.RegisterKill()`, `GameManager.Restart()`.
  Task 3(타이머 UI), Task 7(`EnemyBase`), Task 8(`ResultPanel`),
  Task 13(`WaveDirector`)이 사용한다.

이 Task에서는 결과 화면 연결을 아직 하지 않는다(Task 9에서 붙인다). 승패가
갈리면 로그만 남긴다.

- [ ] **Step 1: GameManager 전면 교체**

`unity/Assets/_Project/Scripts/Core/GameManager.cs` 전체를 아래로 교체:

```csharp
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
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private LevelSystem levelSystem;
        [Tooltip("캐릭터 선택 UI 루트. 런이 시작되면 비활성화된다.")]
        [SerializeField] private GameObject characterSelectPanel;
        [Tooltip("한 판 길이(초). 5:00 = 300")]
        [SerializeField] private float runDuration = 300f;

        public static GameManager Instance { get; private set; }

        public RunState CurrentState { get; private set; } = RunState.CharacterSelect;
        public float TotalExperience { get; private set; }
        public float ElapsedTime { get; private set; }
        public float RunDuration => runDuration;
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

            if (ElapsedTime >= runDuration)
                FinishRun(RunOutcome.Victory);
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

        private void FinishRun(RunOutcome outcome)
        {
            // 승리와 패배가 같은 프레임에 성립할 수 있다. 먼저 성립한 것만 처리한다.
            if (CurrentState != RunState.Playing) return;

            CurrentState = RunState.Result;
            enemySpawner.StopSpawning();

            // 결과 화면 동안에는 적도 젬도 움직이지 않게 멈춘다.
            Time.timeScale = 0f;

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
```

주요 변경점:
- `RunState.GameOver` → `Result`, `RunOutcome` 추가
- 아무데서도 쓰이지 않던 죽은 코드 `IsGameOver` 프로퍼티 제거
- `ElapsedTime` / `RunDuration` / `KillCount` 추가
- `FinishRun`이 승패 양쪽을 한 곳에서 처리(중복 진입 차단 포함)
- 결과 상태에서 `timeScale = 0`, 재시작 직전에 1로 복구

- [ ] **Step 2: 테스트 실행해서 컴파일 확인**

Expected: 총계 106개 통과, `error CS` 없음.

---

### Task 3: RunTimerDisplay

**Files:**
- Create: `unity/Assets/_Project/Scripts/UI/RunTimerDisplay.cs`

**Interfaces:**
- Consumes: `GameManager.Instance.ElapsedTime` / `RunDuration` / `CurrentState`
  (Task 2), `RunClock.FormatRemaining` (Task 1).

- [ ] **Step 1: RunTimerDisplay 작성**

`unity/Assets/_Project/Scripts/UI/RunTimerDisplay.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 화면 상단에 남은 시간을 표시한다. 캐릭터 선택 중과 결과 화면에서는 숨긴다.
    /// </summary>
    public class RunTimerDisplay : MonoBehaviour
    {
        [SerializeField] private Text timerText;

        private void Update()
        {
            if (timerText == null) return;

            var manager = GameManager.Instance;
            if (manager == null) return;

            bool playing = manager.CurrentState == RunState.Playing;
            timerText.enabled = playing;

            if (!playing) return;

            timerText.text = RunClock.FormatRemaining(manager.ElapsedTime, manager.RunDuration);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 컴파일 확인**

Expected: 총계 106개 통과, `error CS` 없음.

---

### Task 4: Unity Editor 작업 — 타이머 UI 배치와 확인

Unity를 열고 진행한다.

- [ ] **Step 1: 타이머 텍스트 만들기**

1. Hierarchy의 `Canvas` 우클릭 → `UI` → `Text (Legacy)` → 이름 `TimerText`
2. Rect Transform: Anchor Presets에서 **top-center** 선택, `Pos Y` = -40
3. `Width` 200, `Height` 50
4. Text 컴포넌트: `Font Size` 36, `Alignment` 가운데 정렬, `Color` 흰색
   (배경이 밝으면 검정)
5. `Text` 내용은 아무거나 — 런타임에 덮어쓴다

- [ ] **Step 2: RunTimerDisplay 붙이기**

`TimerText` 선택 → `Add Component` → `Run Timer Display`
- `Timer Text` ← 자기 자신의 `Text` 컴포넌트 드래그

- [ ] **Step 3: GameManager 새 필드 확인**

`GameManager` 선택 → `Run Duration`이 **300**인지 확인.
빠르게 테스트하려면 임시로 `20` 정도로 낮춰도 된다(확인 후 300으로 복구).

- [ ] **Step 4: 플레이테스트**

1. 캐릭터 선택 화면에서는 타이머가 **안 보인다**
2. 캐릭터를 고르면 타이머가 나타나 `5:00`부터 줄어든다
3. 레벨업 팝업이 열린 동안 타이머가 **멈춘다**(의도된 동작)
4. `Run Duration`을 20으로 낮춰 두면 20초 뒤 Console에
   `[GameManager] 런 종료: Victory / 생존 0:20 / ...` 로그가 뜨고 게임이 멈춘다
5. 죽으면 `런 종료: Defeat` 로그가 뜬다
6. Console에 에러가 없다

확인 후 `Run Duration`을 **300으로 되돌린다.**

---

## Phase 2 — 결과 화면 + 재시작

### Task 5: AugmentTally 증강 집계 (TDD)

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/AugmentTally.cs`
- Test: `unity/Assets/Tests/EditMode/AugmentTallyTests.cs`

**Interfaces:**
- Consumes: `SushiSurvival.Data.AugmentData`.
- Produces: `struct AugmentCount { AugmentData Data; int Count; }`,
  `AugmentTally.Summarize(IReadOnlyList<AugmentData> picked) -> List<AugmentCount>`.
  Task 8(`ResultPanel`)과 Task 9(`GameManager`)가 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/AugmentTallyTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    public class AugmentTallyTests
    {
        private static AugmentData MakeAugment(string augmentName)
        {
            var data = ScriptableObject.CreateInstance<AugmentData>();
            data.augmentName = augmentName;
            return data;
        }

        [Test]
        public void Summarize_ReturnsEmpty_ForEmptyInput()
        {
            var result = AugmentTally.Summarize(new List<AugmentData>());

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Summarize_CountsSinglePickAsOne()
        {
            var attack = MakeAugment("공격력");

            var result = AugmentTally.Summarize(new List<AugmentData> { attack });

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(1, result[0].Count);
            Assert.AreSame(attack, result[0].Data);
        }

        [Test]
        public void Summarize_GroupsRepeatedPicks()
        {
            var attack = MakeAugment("공격력");

            var result = AugmentTally.Summarize(new List<AugmentData> { attack, attack, attack });

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(3, result[0].Count);
        }

        [Test]
        public void Summarize_KeepsDistinctAugmentsSeparate()
        {
            var attack = MakeAugment("공격력");
            var armor = MakeAugment("방어력");

            var result = AugmentTally.Summarize(new List<AugmentData> { attack, armor, attack });

            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void Summarize_PreservesFirstPickOrder()
        {
            var attack = MakeAugment("공격력");
            var armor = MakeAugment("방어력");

            var result = AugmentTally.Summarize(new List<AugmentData> { armor, attack, armor });

            Assert.AreSame(armor, result[0].Data);
            Assert.AreSame(attack, result[1].Data);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Expected: `AugmentTally`가 없어서 컴파일 실패.

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/Core/AugmentTally.cs`:

```csharp
using System.Collections.Generic;
using SushiSurvival.Data;

namespace SushiSurvival.Core
{
    public struct AugmentCount
    {
        public AugmentData Data;
        public int Count;
    }

    public static class AugmentTally
    {
        /// <summary>
        /// 고른 증강 목록을 (데이터, 개수)로 묶는다. 같은 증강을 열 번 고르면
        /// 아이콘이 열 개 늘어서므로 결과 화면에서는 묶어서 보여준다.
        /// 처음 고른 순서를 유지한다.
        /// </summary>
        public static List<AugmentCount> Summarize(IReadOnlyList<AugmentData> picked)
        {
            var order = new List<AugmentData>();
            var counts = new Dictionary<AugmentData, int>();

            foreach (var data in picked)
            {
                if (data == null) continue;

                if (counts.TryGetValue(data, out int current))
                {
                    counts[data] = current + 1;
                }
                else
                {
                    counts[data] = 1;
                    order.Add(data);
                }
            }

            var result = new List<AugmentCount>();
            foreach (var data in order)
                result.Add(new AugmentCount { Data = data, Count = counts[data] });

            return result;
        }
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Expected: 총계 **111개** 전부 통과.

---

### Task 6: LevelSystem이 획득 증강 목록을 노출

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Core/LevelSystem.cs`

**Interfaces:**
- Produces: `LevelSystem.PickedAugments` (`IReadOnlyList<AugmentData>`).
  Task 9(`GameManager`)가 사용한다.

- [ ] **Step 1: 고른 증강을 순서대로 보관하는 필드 추가**

`unity/Assets/_Project/Scripts/Core/LevelSystem.cs`에서
`private readonly Dictionary<AugmentData, float> _accumulated = ...` 줄 **바로 아래**에
아래 두 줄을 추가한다:

```csharp
        private readonly List<AugmentData> _pickedAugments = new List<AugmentData>();

        public IReadOnlyList<AugmentData> PickedAugments => _pickedAugments;
```

- [ ] **Step 2: 선택 시 목록에 기록**

같은 파일의 `OnOptionChosen` 안, `_accumulated[data] = current + data.valuePerPick;`
줄 **바로 아래**에 한 줄을 추가한다:

```csharp
                _pickedAugments.Add(data);
```

수정 후 그 블록은 아래 모습이 된다:

```csharp
            if (option is AugmentOption augmentOption)
            {
                var data = augmentOption.Data;
                _accumulated.TryGetValue(data, out float current);
                _accumulated[data] = current + data.valuePerPick;
                _pickedAugments.Add(data);
            }
```

- [ ] **Step 3: 테스트 실행해서 컴파일 확인**

Expected: 총계 111개 통과, `error CS` 없음.

---

### Task 7: EnemyBase가 처치를 알림

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Enemies/EnemyBase.cs`

**Interfaces:**
- Consumes: `GameManager.Instance.RegisterKill()` (Task 2).

- [ ] **Step 1: Die()에서 처치를 등록**

`unity/Assets/_Project/Scripts/Enemies/EnemyBase.cs`의 `Die()` 안,
`OnDeath?.Invoke(this);` 줄 **바로 위**에 아래를 추가한다:

```csharp
            if (GameManager.Instance != null)
                GameManager.Instance.RegisterKill();
```

`XPGem`이 이미 `GameManager.Instance.AddExperience`를 쓰고 있으므로 같은 패턴이다.

- [ ] **Step 2: 테스트 실행해서 컴파일 확인**

Expected: 총계 111개 통과, `error CS` 없음.

---

### Task 8: ResultPanel + ResultAugmentEntry

**Files:**
- Create: `unity/Assets/_Project/Scripts/UI/ResultAugmentEntry.cs`
- Create: `unity/Assets/_Project/Scripts/UI/ResultPanel.cs`

**Interfaces:**
- Consumes: `RunOutcome` (Task 2), `AugmentCount` (Task 5),
  `RunClock.FormatElapsed` (Task 1), `GameManager.Instance.Restart()` (Task 2).
- Produces: `ResultPanel.Show(RunOutcome outcome, float elapsed, int level, int kills, IReadOnlyList<AugmentCount> augments)`,
  `ResultPanel.Hide()`, `ResultAugmentEntry.Bind(AugmentCount)`.
  Task 9(`GameManager`)가 `Show`를 호출한다.

- [ ] **Step 1: ResultAugmentEntry 작성**

`unity/Assets/_Project/Scripts/UI/ResultAugmentEntry.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 결과 화면의 증강 항목 하나 — 아이콘과 "x3" 개수 표시.
    /// </summary>
    public class ResultAugmentEntry : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text countText;

        public void Bind(AugmentCount entry)
        {
            if (iconImage != null)
            {
                iconImage.sprite = entry.Data != null ? entry.Data.icon : null;
                iconImage.enabled = iconImage.sprite != null;
            }

            if (countText != null)
                countText.text = $"x{entry.Count}";
        }
    }
}
```

- [ ] **Step 2: ResultPanel 작성**

`unity/Assets/_Project/Scripts/UI/ResultPanel.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    public class ResultPanel : MonoBehaviour
    {
        [Tooltip("결과 화면 루트. 비워두면 이 오브젝트 자신을 켜고 끈다.")]
        [SerializeField] private GameObject root;
        [SerializeField] private Text outcomeText;
        [SerializeField] private Text survivalTimeText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text killCountText;
        [Tooltip("증강 항목이 생성될 부모. Horizontal Layout Group을 붙여두면 자동 정렬된다.")]
        [SerializeField] private Transform augmentListRoot;
        [SerializeField] private ResultAugmentEntry augmentEntryPrefab;
        [SerializeField] private Button restartButton;

        private readonly List<ResultAugmentEntry> _spawnedEntries = new List<ResultAugmentEntry>();

        private GameObject Root => root != null ? root : gameObject;

        private void Awake()
        {
            Hide();

            if (restartButton != null)
                restartButton.onClick.AddListener(HandleRestart);
        }

        private void OnDestroy()
        {
            if (restartButton != null)
                restartButton.onClick.RemoveListener(HandleRestart);
        }

        public void Show(RunOutcome outcome, float elapsed, int level, int kills,
                         IReadOnlyList<AugmentCount> augments)
        {
            Root.SetActive(true);

            if (outcomeText != null)
                outcomeText.text = outcome == RunOutcome.Victory ? "생존 성공!" : "패배";

            if (survivalTimeText != null)
                survivalTimeText.text = $"생존 시간  {RunClock.FormatElapsed(elapsed)}";

            if (levelText != null)
                levelText.text = $"도달 레벨  {level}";

            if (killCountText != null)
                killCountText.text = $"처치 수  {kills}";

            BuildAugmentList(augments);
        }

        public void Hide() => Root.SetActive(false);

        private void BuildAugmentList(IReadOnlyList<AugmentCount> augments)
        {
            if (augmentListRoot == null || augmentEntryPrefab == null) return;

            foreach (var entry in _spawnedEntries)
            {
                if (entry != null)
                    Destroy(entry.gameObject);
            }
            _spawnedEntries.Clear();

            foreach (var augment in augments)
            {
                ResultAugmentEntry entry = Instantiate(augmentEntryPrefab, augmentListRoot);
                entry.Bind(augment);
                _spawnedEntries.Add(entry);
            }
        }

        private void HandleRestart()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.Restart();
        }
    }
}
```

- [ ] **Step 3: 테스트 실행해서 컴파일 확인**

Expected: 총계 111개 통과, `error CS` 없음.

---

### Task 9: GameManager가 결과 화면을 띄우도록 연결

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Core/GameManager.cs`

**Interfaces:**
- Consumes: `ResultPanel.Show` (Task 8), `AugmentTally.Summarize` (Task 5),
  `LevelSystem.PickedAugments` (Task 6).

- [ ] **Step 1: ResultPanel 필드 추가**

`unity/Assets/_Project/Scripts/Core/GameManager.cs`의
`[SerializeField] private GameObject characterSelectPanel;` 줄 **바로 아래**에 추가:

```csharp
        [SerializeField] private SushiSurvival.UI.ResultPanel resultPanel;
```

- [ ] **Step 2: FinishRun에서 결과 화면 표시**

같은 파일의 `FinishRun` 메서드 안, `Debug.Log(...)` 줄 **바로 위**에 추가:

```csharp
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
```

- [ ] **Step 3: 테스트 실행해서 컴파일 확인**

Expected: 총계 111개 통과, `error CS` 없음.

---

### Task 10: Unity Editor 작업 — 결과 화면 구성

Unity를 열고 진행한다.

- [ ] **Step 1: 증강 항목 프리팹 만들기**

1. Hierarchy의 `Canvas` 아래에 임시로 `UI > Image` 생성 → 이름 `ResultAugmentEntry`
   - `Source Image`를 증강 아이콘 아무거나로 지정(런타임에 바뀐다)
   - `Width` 64, `Height` 64
2. 그 아래 자식으로 `UI > Text (Legacy)` → 이름 `CountText`
   - Rect를 아이콘 우하단에 작게 배치, `Font Size` 20
3. `ResultAugmentEntry`에 `Result Augment Entry` 컴포넌트 추가
   - `Icon Image` ← 자기 자신의 `Image`
   - `Count Text` ← 자식 `CountText`
4. `Assets/_Project/Prefabs`로 드래그해 프리팹화 → **씬의 인스턴스는 삭제**

- [ ] **Step 2: 결과 패널 만들기**

1. `Canvas` 아래 빈 GameObject → 이름 `ResultPanel`
   - Rect Transform을 Anchor Presets에서 Alt+stretch-stretch로 화면 전체
2. 배경: `ResultPanel` 아래 `UI > Image` → 검정, 알파 200 정도, 역시 전체 stretch
3. 텍스트 4개를 `UI > Text (Legacy)`로 만들고 세로로 배치:
   - `OutcomeText` (`Font Size` 48)
   - `SurvivalTimeText`
   - `LevelText`
   - `KillCountText`
4. 증강 목록 부모: 빈 GameObject → 이름 `AugmentListRoot`
   - `Add Component` → **`Horizontal Layout Group`** (자동 가로 정렬)
   - `Child Force Expand`의 Width/Height 체크 해제
5. 버튼: `UI > Button (Legacy)` → 이름 `RestartButton`, 자식 텍스트를 "다시 하기"로

- [ ] **Step 3: ResultPanel 컴포넌트 배선**

`ResultPanel` 선택 → `Add Component` → `Result Panel`
- `Root` ← 비워둠(자기 자신을 켜고 끈다)
- `Outcome Text` ← `OutcomeText`
- `Survival Time Text` ← `SurvivalTimeText`
- `Level Text` ← `LevelText`
- `Kill Count Text` ← `KillCountText`
- `Augment List Root` ← `AugmentListRoot`
- `Augment Entry Prefab` ← Step 1의 `ResultAugmentEntry` **프리팹**
- `Restart Button` ← `RestartButton`

- [ ] **Step 4: GameManager에 연결**

`GameManager` 선택 → 새로 생긴 `Result Panel` 필드 ← 씬의 `ResultPanel`

- [ ] **Step 5: 씬을 빌드 설정에 등록**

`SceneManager.LoadScene`이 동작하려면 씬이 Build Settings에 있어야 한다.
`File > Build Settings...` → **`Add Open Scenes`** 클릭 → `Slice1` 씬이 목록에
체크된 상태로 들어갔는지 확인 → 창 닫기.

이 단계를 빼먹으면 "다시 하기"를 눌렀을 때
`Scene couldn't be loaded because it isn't added to the build settings` 에러가 난다.

- [ ] **Step 6: 플레이테스트**

빠른 확인을 위해 `GameManager`의 `Run Duration`을 임시로 **20**으로 낮춘다.

1. 캐릭터를 고르고 증강을 몇 개 골라둔다
2. 20초가 지나면 결과 화면이 뜨고 **"생존 성공!"** 이 보인다
3. 생존 시간·도달 레벨·처치 수가 실제 플레이와 맞는다
4. 고른 증강이 아이콘 + `x2` 형태로 묶여 나온다
5. 결과 화면 뒤에서 적이 **움직이지 않는다**(정지 상태)
6. "다시 하기"를 누르면 캐릭터 선택 화면으로 돌아가고, **레벨·증강·처치 수가
   전부 초기화**되어 있다
7. 일부러 죽어보면 **"패배"** 문구로 결과 화면이 뜬다
8. 재시작 후 게임이 정상 속도로 돌아간다(느려지거나 멈추지 않는다)
9. Console에 에러가 없다

확인 후 `Run Duration`을 **300으로 되돌린다.**

---

## Phase 3 — 젬 3종 + 중형몹 + 웨이브 타임라인

### Task 11: WaveSchedule 이벤트 발화 판정 (TDD)

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/WaveSchedule.cs`
- Test: `unity/Assets/Tests/EditMode/WaveScheduleTests.cs`

**Interfaces:**
- Produces: `WaveSchedule.GetDueIndices(IReadOnlyList<float> eventTimes, float previousTime, float currentTime) -> List<int>`.
  Task 13(`WaveDirector`)이 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/WaveScheduleTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class WaveScheduleTests
    {
        [Test]
        public void GetDueIndices_ReturnsEmpty_WhenNothingReachedYet()
        {
            var times = new List<float> { 120f, 240f };

            var result = WaveSchedule.GetDueIndices(times, 10f, 11f);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetDueIndices_ReturnsEventInsideWindow()
        {
            var times = new List<float> { 120f, 240f };

            var result = WaveSchedule.GetDueIndices(times, 119.9f, 120.1f);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(0, result[0]);
        }

        [Test]
        public void GetDueIndices_ExcludesEventAtPreviousTime_SoItNeverFiresTwice()
        {
            var times = new List<float> { 120f };

            var result = WaveSchedule.GetDueIndices(times, 120f, 120.5f);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetDueIndices_IncludesEventExactlyAtCurrentTime()
        {
            var times = new List<float> { 120f };

            var result = WaveSchedule.GetDueIndices(times, 119.5f, 120f);

            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void GetDueIndices_ReturnsMultipleEvents_WhenFrameSpansBoth()
        {
            // 에디터 멈춤 등으로 델타가 크게 튀는 경우
            var times = new List<float> { 120f, 240f };

            var result = WaveSchedule.GetDueIndices(times, 100f, 300f);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(0, result[0]);
            Assert.AreEqual(1, result[1]);
        }

        [Test]
        public void GetDueIndices_FiresZeroSecondEvent_WhenPreviousTimeIsNegative()
        {
            // WaveDirector는 previousTime을 -1로 시작해 0초 이벤트를 놓치지 않는다.
            var times = new List<float> { 0f };

            var result = WaveSchedule.GetDueIndices(times, -1f, 0.016f);

            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void GetDueIndices_IgnoresAlreadyPassedEvents()
        {
            var times = new List<float> { 120f };

            var result = WaveSchedule.GetDueIndices(times, 200f, 201f);

            Assert.AreEqual(0, result.Count);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Expected: `WaveSchedule`이 없어서 컴파일 실패.

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/Core/WaveSchedule.cs`:

```csharp
using System.Collections.Generic;

namespace SushiSurvival.Core
{
    public static class WaveSchedule
    {
        /// <summary>
        /// (previousTime, currentTime] 구간에 걸린 이벤트의 인덱스를 돌려준다.
        /// 시작을 열린 구간으로 두어 같은 이벤트가 두 번 발화하지 않게 하고,
        /// 끝을 닫힌 구간으로 두어 프레임 사이에 낀 이벤트를 놓치지 않게 한다.
        /// </summary>
        public static List<int> GetDueIndices(IReadOnlyList<float> eventTimes, float previousTime, float currentTime)
        {
            var due = new List<int>();

            for (int i = 0; i < eventTimes.Count; i++)
            {
                float time = eventTimes[i];
                if (time > previousTime && time <= currentTime)
                    due.Add(i);
            }

            return due;
        }
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Expected: 총계 **118개** 전부 통과.

---

### Task 12: XPGemPoolSet + 젬 등급 연결

**Files:**
- Create: `unity/Assets/_Project/Scripts/Pickups/XPGemPoolSet.cs`
- Modify: `unity/Assets/_Project/Scripts/Enemies/EnemyBase.cs`
- Modify: `unity/Assets/_Project/Scripts/Enemies/EnemySpawner.cs`

**Interfaces:**
- Consumes: `SushiSurvival.Data.XPGemType` (슬라이스 1부터 존재),
  `MonsterData.xpGemDrop` (지금까지 사용되지 않던 필드).
- Produces: `XPGemPoolSet.GetPool(XPGemType type) -> GameObjectPool`,
  `EnemyBase.SetXpGemPools(XPGemPoolSet set)` (기존 `SetXpGemPool` 대체).
  Task 13(`WaveDirector`)도 `SetXpGemPools`를 호출한다.

- [ ] **Step 1: XPGemPoolSet 작성**

`unity/Assets/_Project/Scripts/Pickups/XPGemPoolSet.cs`:

```csharp
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.Pickups
{
    /// <summary>
    /// 젬 등급별 풀 묶음. 몬스터는 MonsterData.xpGemDrop에 적힌 등급의 젬을 떨군다.
    /// </summary>
    public class XPGemPoolSet : MonoBehaviour
    {
        [Tooltip("흰색 밥알 — 1XP")]
        [SerializeField] private GameObjectPool basicPool;
        [Tooltip("갈색 밥알 — 5XP")]
        [SerializeField] private GameObjectPool fivePool;
        [Tooltip("황금 밥알 — 10XP")]
        [SerializeField] private GameObjectPool tenPool;

        public GameObjectPool GetPool(XPGemType type)
        {
            switch (type)
            {
                case XPGemType.Five: return fivePool;
                case XPGemType.Ten: return tenPool;
                default: return basicPool;
            }
        }
    }
}
```

- [ ] **Step 2: EnemyBase가 풀 세트를 쓰도록 수정**

`unity/Assets/_Project/Scripts/Enemies/EnemyBase.cs`에서 네 곳을 고친다.

파일 맨 위 using에 추가:

```csharp
using SushiSurvival.Pickups;
```

필드 `private GameObjectPool _xpGemPool;`를 아래로 교체:

```csharp
        private XPGemPoolSet _xpGemPools;
```

`SetXpGemPool` 메서드 전체를 아래로 교체:

```csharp
        /// <summary>
        /// 스포너가 Get() 직후 매번 호출해서 주입한다. 프리팹 에셋은 씬에만
        /// 존재하는 풀을 Inspector로 직접 참조할 수 없기 때문.
        /// </summary>
        public void SetXpGemPools(XPGemPoolSet pools) => _xpGemPools = pools;
```

`Die()` 안의 젬 드롭 블록(`if (_xpGemPool == null) ... else ...`)을 아래로 교체:

```csharp
            if (_xpGemPools == null)
            {
                Debug.LogError($"{name}: xpGemPools가 설정되지 않아 XP 젬을 드롭할 수 없습니다.");
            }
            else
            {
                GameObjectPool gemPool = _xpGemPools.GetPool(monsterData.xpGemDrop);
                if (gemPool == null)
                    Debug.LogError($"{name}: {monsterData.xpGemDrop} 등급 젬 풀이 비어 있습니다.");
                else
                    gemPool.Get(transform.position, Quaternion.identity);
            }
```

- [ ] **Step 3: EnemySpawner가 풀 세트를 주입하도록 수정**

`unity/Assets/_Project/Scripts/Enemies/EnemySpawner.cs`에서 세 곳을 고친다.

파일 맨 위 using에 추가:

```csharp
using SushiSurvival.Pickups;
```

필드 `[SerializeField] private GameObjectPool xpGemPool;`를 아래로 교체:

```csharp
        [SerializeField] private XPGemPoolSet xpGemPools;
```

`SpawnOne` 마지막 줄 `enemy.SetXpGemPool(xpGemPool);`을 아래로 교체:

```csharp
                enemy.SetXpGemPools(xpGemPools);
```

- [ ] **Step 4: 테스트 실행해서 컴파일 확인**

Expected: 총계 118개 통과, `error CS` 없음.

---

### Task 13: WaveDirector

**Files:**
- Create: `unity/Assets/_Project/Scripts/Enemies/WaveDirector.cs`
- Modify: `unity/Assets/_Project/Scripts/Core/GameManager.cs`

**Interfaces:**
- Consumes: `WaveSchedule.GetDueIndices` (Task 11),
  `XPGemPoolSet` / `EnemyBase.SetXpGemPools` (Task 12),
  `SpawnRingUtility.GetPositionOnRing` (슬라이스 1),
  `GameManager.Instance.ElapsedTime` / `RunDuration` (Task 2).
- Produces: `WaveDirector.StartTimeline(Transform player)`,
  `WaveDirector.StopTimeline()`.
  `GameManager`가 호출한다.

- [ ] **Step 1: WaveDirector 작성**

`unity/Assets/_Project/Scripts/Enemies/WaveDirector.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Pickups;

namespace SushiSurvival.Enemies
{
    [System.Serializable]
    public class WaveEvent
    {
        [Tooltip("런 시작 후 이 시각(초)에 발화한다. 2:00 = 120, 4:00 = 240")]
        public float timeSeconds;
        [Tooltip("스폰할 몬스터의 풀.")]
        public GameObjectPool pool;
        [Min(1)]
        public int count = 1;
    }

    /// <summary>
    /// 런 경과 시간을 보며 예약된 스폰 이벤트를 발화한다. 잡몹 지속 스폰은
    /// EnemySpawner가 따로 담당하고, 여기서는 시각이 정해진 등장만 다룬다.
    /// </summary>
    public class WaveDirector : MonoBehaviour
    {
        [SerializeField] private WaveEvent[] events;
        [SerializeField] private XPGemPoolSet xpGemPools;
        [Tooltip("플레이어로부터 이 거리의 링 위에 등장시킨다.")]
        [SerializeField] private float spawnRadius = 8f;

        private readonly List<float> _eventTimes = new List<float>();

        private Transform _player;
        private bool _running;
        // 0초 이벤트도 놓치지 않도록 음수에서 시작한다.
        private float _previousTime = -1f;

        public void StartTimeline(Transform player)
        {
            _player = player;
            _previousTime = -1f;
            _running = true;

            _eventTimes.Clear();
            foreach (var waveEvent in events)
                _eventTimes.Add(waveEvent.timeSeconds);

            WarnAboutUnreachableEvents();
        }

        public void StopTimeline() => _running = false;

        private void Update()
        {
            if (!_running || _player == null) return;

            var manager = GameManager.Instance;
            if (manager == null) return;

            float now = manager.ElapsedTime;
            List<int> due = WaveSchedule.GetDueIndices(_eventTimes, _previousTime, now);
            _previousTime = now;

            foreach (int index in due)
                SpawnEvent(events[index]);
        }

        private void SpawnEvent(WaveEvent waveEvent)
        {
            if (waveEvent.pool == null)
            {
                Debug.LogError($"{name}: {waveEvent.timeSeconds}초 이벤트의 pool이 비어 있습니다.");
                return;
            }

            for (int i = 0; i < waveEvent.count; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector2 spawnPos = SpawnRingUtility.GetPositionOnRing(_player.position, spawnRadius, angle);
                GameObject spawned = waveEvent.pool.Get(spawnPos, Quaternion.identity);

                if (spawned.TryGetComponent<EnemyBase>(out var enemy))
                    enemy.SetXpGemPools(xpGemPools);
            }

            Debug.Log($"[WaveDirector] {waveEvent.timeSeconds}초 이벤트 발화 — {waveEvent.count}마리");
        }

        private void WarnAboutUnreachableEvents()
        {
            float duration = GameManager.Instance != null ? GameManager.Instance.RunDuration : float.MaxValue;

            foreach (var waveEvent in events)
            {
                if (waveEvent.timeSeconds > duration)
                    Debug.LogWarning($"{name}: {waveEvent.timeSeconds}초 이벤트는 런 길이({duration}초)보다 뒤라 발화되지 않습니다.");
            }
        }
    }
}
```

- [ ] **Step 2: GameManager가 타임라인을 시작·정지시키도록 수정**

`unity/Assets/_Project/Scripts/Core/GameManager.cs`에서 세 곳을 고친다.

필드 추가 — `[SerializeField] private EnemySpawner enemySpawner;` 줄 **바로 아래**:

```csharp
        [SerializeField] private WaveDirector waveDirector;
```

`StartRun` 안의 `enemySpawner.StartSpawning(player.transform);` 줄 **바로 아래**:

```csharp
            if (waveDirector != null)
                waveDirector.StartTimeline(player.transform);
```

`FinishRun` 안의 `enemySpawner.StopSpawning();` 줄 **바로 아래**:

```csharp
            if (waveDirector != null)
                waveDirector.StopTimeline();
```

- [ ] **Step 3: 테스트 실행해서 컴파일 확인**

Expected: 총계 118개 통과, `error CS` 없음.

---

### Task 14: Unity Editor 작업 — 젬 3종, 중형몹, 타임라인 배선

Unity를 열고 진행한다.

- [ ] **Step 1: 젬 스프라이트 임포트**

`Assets/Art/캐릭터/캐릭터/경험치`의 세 파일을 모두 선택 →
`Texture Type` = `Sprite (2D and UI)`, `Sprite Mode` = `Single` → Apply
- `밥알(경험치).png` — 흰색
- `밥알(경험치)5xp.png` — 갈색
- `밥알(경험치)10xp.png` — 황금

- [ ] **Step 2: 젬 프리팹 2개 추가**

기존 `XPGem` 프리팹을 복제해서 만든다.
1. Project 창에서 `XPGem` 프리팹 선택 → `Ctrl+D` → 이름 `XPGem5`
   - `SpriteRenderer`의 `Sprite` ← `밥알(경험치)5xp`
   - `XPGem` 컴포넌트의 `Xp Value` = **5**
2. 다시 `XPGem` 프리팹을 `Ctrl+D` → 이름 `XPGem10`
   - `Sprite` ← `밥알(경험치)10xp`
   - `Xp Value` = **10**

- [ ] **Step 3: 젬 풀 2개 추가하고 세트 만들기**

1. 씬에 빈 GameObject `XPGem5Pool` → `Game Object Pool`
   (`Prefab` = `XPGem5`, `Prewarm Count` = 30)
2. 빈 GameObject `XPGem10Pool` → `Game Object Pool`
   (`Prefab` = `XPGem10`, `Prewarm Count` = 20)
3. 빈 GameObject `XPGemPoolSet` → `X P Gem Pool Set` 컴포넌트 추가
   - `Basic Pool` ← 기존 `XPGemPool`
   - `Five Pool` ← `XPGem5Pool`
   - `Ten Pool` ← `XPGem10Pool`
4. `EnemySpawner` 선택 → 이름이 바뀐 `Xp Gem Pools` 필드 ← `XPGemPoolSet`
   (기존 `Xp Gem Pool` 단일 필드는 사라진다)

- [ ] **Step 4: 잡몹 데이터의 젬 등급 확인**

`BasicMobData`와 `CaliforniaRollData` 둘 다 `Xp Gem Drop` = **`Basic`** 인지 확인한다.

- [ ] **Step 5: 중형몹 스프라이트 슬라이스**

`Assets/Art/캐릭터/캐릭터/몬스터 시트/중형몹 이동-Sheet.png` 선택 →
`Texture Type` = `Sprite (2D and UI)`, `Sprite Mode` = `Multiple` → Apply →
`Sprite Editor` → `Slice > Grid By Cell Size`, `Pixel Size` = **100 × 100** →
`Slice` → Apply. (5프레임이 나와야 한다)

- [ ] **Step 6: 중형몹 MonsterData 생성**

`Assets/_Project/Data`에서 `Create > SushiSurvival > Monster Data` →
이름 `MidBossData`
- `Monster Name` = `중형몹`
- `Max Health` = **200**
- `Contact Damage` = **12**
- `Move Speed` = **1.2**
- `Xp Gem Drop` = **`Ten`**

- [ ] **Step 7: 중형몹 프리팹 만들기**

`BasicMob` 프리팹을 복제해서 만든다.
1. `BasicMob` 선택 → `Ctrl+D` → 이름 `MidBoss`
2. 더블클릭해서 열고:
   - `SpriteRenderer`의 `Sprite` ← 슬라이스한 중형몹 프레임 중 하나
   - `EnemyBase`의 `Monster Data` ← `MidBossData`
   - `EnemyAI`의 `Monster Data` ← `MidBossData`
   - `CircleCollider2D`의 `Radius`를 키운다(잡몹의 2배 크기이므로, 예: 0.5)
   - Layer가 `Enemy`인지 확인

- [ ] **Step 8: 중형몹 풀 만들기**

씬에 빈 GameObject `MidBossPool` → `Game Object Pool`
- `Prefab` ← `MidBoss` 프리팹
- `Prewarm Count` = **4** (한 판에 2마리만 나오므로 넉넉하다)

- [ ] **Step 9: WaveDirector 배치와 배선**

1. 씬에 빈 GameObject `WaveDirector` → `Wave Director` 컴포넌트 추가
2. `Events` 배열 크기를 **2**로 하고:

   | 인덱스 | Time Seconds | Pool | Count |
   |---|---|---|---|
   | 0 | **120** (2:00) | `MidBossPool` | 1 |
   | 1 | **240** (4:00) | `MidBossPool` | 1 |

3. `Xp Gem Pools` ← `XPGemPoolSet`
4. `Spawn Radius` = 8
5. `GameManager` 선택 → 새로 생긴 `Wave Director` 필드 ← 이 오브젝트

- [ ] **Step 10: 최종 플레이테스트**

먼저 짧게 확인하기 위해 임시로 값을 낮춘다:
- `GameManager`의 `Run Duration` = **40**
- `WaveDirector`의 이벤트 시각을 **10**과 **20**으로

1. 10초에 중형몹이 등장하고 Console에 `[WaveDirector] 10초 이벤트 발화` 로그가 뜬다
2. 중형몹이 **눈에 띄게 크고 느리며** 잡몹보다 훨씬 오래 버틴다
3. 중형몹이 죽으면 **황금 젬**이 떨어지고, 먹으면 경험치가 크게 오른다
   (여러 레벨이 한 번에 올라 팝업이 연달아 뜰 수 있다)
4. 잡몹은 여전히 **흰색 젬**을 떨어뜨린다
5. 20초에 두 번째 중형몹이 등장한다
6. 40초에 결과 화면이 뜨고 처치 수에 중형몹이 포함되어 있다
7. **같은 이벤트가 두 번 발화하지 않는다**(중형몹이 한 번에 2마리씩 나오지 않음)
8. "다시 하기" 후 새 런에서 타임라인이 **처음부터 다시** 동작한다
9. Console에 에러가 없다

확인 후 실제 값으로 되돌린다:
- `Run Duration` = **300**
- 이벤트 시각 = **120**, **240**
