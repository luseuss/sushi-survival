# 인게임 HUD 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `UI_SPEC.md` 2장의 인게임 HUD를 구현한다 — 최상단 XP 게이지, 상단
중앙 생존 타이머(보스 등장 시 아래로 밀림), 상단 우측 처치 수, 하단 좌측
캐릭터 초상화+체력바(HUD 코너로 완전 이전).

**Architecture:** `XpGaugeDisplay`/`KillCountDisplay`는 `BossHealthBar`와
같은 폴링 방식(매 프레임 값을 읽어 표시)의 신규 컴포넌트다. 체력바는 기존
`HealthBar`를 캐릭터 프리팹에서 씬 HUD로 옮기고, `CameraFollow.SetTarget`과
같은 패턴으로 `GameManager`가 스폰 직후 런타임에 연결한다. 타이머는 새 이벤트
없이 이미 읽고 있는 `GameManager.ElapsedTime`/`BossSpawnTime`으로 스스로
판단해 위치를 옮긴다.

**Tech Stack:** Unity 2022.3.62f3 LTS, Built-in 2D, uGUI(Legacy), Unity Test
Framework(NUnit, EditMode)

**Spec:** `docs/superpowers/specs/2026-09-03-ingame-hud-design.md`

## Global Constraints

- git을 쓴다. `feature/ingame-hud` 브랜치에서 진행, 태스크마다 로컬 커밋,
  푸시는 전부 끝나고 사용자에게 물어본 뒤에 한다. 로컬 동기화는 **merge**로
  한다(`git merge origin/main`) — rebase 아님, `docs/COLLABORATION.md` 참고.
- Unity 프로젝트 루트는 `unity/` 서브폴더. 스크립트는
  `unity/Assets/_Project/Scripts/`, 테스트는 `unity/Assets/Tests/EditMode/`.
- **EditMode 테스트를 배치로 돌릴 때는 `-logFile -`(하이픈, stdout)을 쓴다.**
  ```
  & "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -projectPath "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요\unity" -runTests -testPlatform EditMode -testResults "$env:TEMP\hud-tests.xml" -logFile -
  ```
- 시작 시점 테스트 수는 **234개**다.
- 네임스페이스: `SushiSurvival.Core` / `.UI`.
- 주석은 한국어로, "무엇"이 아니라 "왜"를 적는다.
- 수치는 하드코딩하지 않고 `[SerializeField]`로 노출한다.
- `Text`는 반드시 Legacy를 쓴다(TextMeshPro 미설치).
- **Unity Editor GUI 작업은 사용자가 직접 한다.** 에이전트는 코드와 배치
  테스트까지만 하고, UI 조립·프리팹 수정은 Task 6으로 넘긴다.
- **씬 파일(`GameScene.unity`) 충돌은 텍스트로 직접 풀지 않는다.** 상대(main)
  버전을 채택하고 사람이 에디터에서 재적용한다 — `docs/COLLABORATION.md` 0장.

---

## File Structure

**신규**

| 파일 | 책임 |
|---|---|
| `Core/LevelCurve.cs`(수정) | `GetProgressRatio` 순수 함수 추가 |
| `UI/XpGaugeDisplay.cs` | 최상단 XP 게이지 |
| `UI/KillCountDisplay.cs` | 상단 우측 처치 수 |

**수정**

| 파일 | 변경 |
|---|---|
| `Core/LevelSystem.cs` | `ProgressRatio` 프로퍼티 노출 |
| `UI/HealthBar.cs` | `SetTarget(PlayerHealth, Sprite)` + `portraitImage` 필드 |
| `UI/RunTimerDisplay.cs` | 보스 등장 시 위치 트윈 |
| `Core/GameManager.cs` | `hudHealthBar` 필드 + 스폰 후 `SetTarget` 호출 |

**테스트 (`unity/Assets/Tests/EditMode/`)**

기존 `LevelCurveTests.cs`에 `GetProgressRatio` 케이스 5개 추가.

---

## Task 0: 작업 브랜치 준비

- [ ] **Step 1: 최신 main 확인 후 브랜치 생성**

```bash
git checkout main
git pull --ff-only
git checkout -b feature/ingame-hud
```

Expected: `Switched to a new branch 'feature/ingame-hud'`

---

## Task 1: XP 진행률 계산 — `LevelCurve.GetProgressRatio`

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Core/LevelCurve.cs`
- Test: `unity/Assets/Tests/EditMode/LevelCurveTests.cs` (기존 파일이면 케이스 추가, 없으면 신규 생성)

**Interfaces:**
- Consumes: `SushiSurvival.Core.LevelCurve.GetRequiredXp(int level, float baseXp, float increment) → float` (기존)
- Produces: `SushiSurvival.Core.LevelCurve.GetProgressRatio(float xpTowardNext, int level, float baseXp, float increment) → float`

- [ ] **Step 1: 기존 테스트 파일 확인**

`unity/Assets/Tests/EditMode/LevelCurveTests.cs`는 이미 존재한다
(`GetRequiredXp`/`Resolve` 테스트 5개). 그 파일의 마지막 `}` 두 개(메서드
닫기 + 클래스 닫기) 사이, 즉 `Resolve_GainsMultipleLevels_FromOneBigGem`
메서드 다음에 아래 테스트들을 추가한다.

- [ ] **Step 2: 실패하는 테스트를 추가한다**

`unity/Assets/Tests/EditMode/LevelCurveTests.cs`에 추가(기존
`Resolve_GainsMultipleLevels_FromOneBigGem` 메서드 뒤, 클래스를 닫는 `}`
앞):

```csharp
        [Test]
        public void GetProgressRatio_ReturnsHalf_WhenHalfwayToNextLevel()
        {
            // baseXp=5, increment=3, level=1 → 필요 경험치 5. 2.5는 절반.
            Assert.That(LevelCurve.GetProgressRatio(2.5f, 1, 5f, 3f), Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void GetProgressRatio_ReturnsZero_AtRunStart()
        {
            Assert.That(LevelCurve.GetProgressRatio(0f, 1, 5f, 3f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void GetProgressRatio_ClampsToOne_WhenOverfull()
        {
            // AddExperience가 넘친 경험치를 즉시 레벨업으로 소비하므로 실제로는
            // 잘 안 생기지만, 방어적으로 클램프한다.
            Assert.That(LevelCurve.GetProgressRatio(999f, 1, 5f, 3f), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void GetProgressRatio_ReturnsZero_WhenRequiredXpIsZero()
        {
            // 0으로 나누어 NaN이 되는 것을 막는다.
            Assert.That(LevelCurve.GetProgressRatio(1f, 1, 0f, 0f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void GetProgressRatio_ScalesWithLevel()
        {
            // baseXp=5, increment=3, level=3 → 필요 경험치 5+3*2=11. 절반은 5.5.
            Assert.That(LevelCurve.GetProgressRatio(5.5f, 3, 5f, 3f), Is.EqualTo(0.5f).Within(0.0001f));
        }
```

- [ ] **Step 3: 테스트가 실패하는지 확인한다**

Global Constraints의 배치 명령으로 실행. 기대: 컴파일 에러 —
`LevelCurve.GetProgressRatio`를 찾을 수 없음.

- [ ] **Step 4: 구현을 추가한다**

`unity/Assets/_Project/Scripts/Core/LevelCurve.cs` 전체를 아래로 교체한다:

```csharp
namespace SushiSurvival.Core
{
    public struct LevelProgress
    {
        public float XpTowardNext;
        public int LevelsGained;
    }

    public static class LevelCurve
    {
        /// <summary>다음 레벨까지 필요한 경험치. 레벨이 오를수록 선형으로 늘어난다.</summary>
        public static float GetRequiredXp(int level, float baseXp, float increment)
            => baseXp + increment * (level - 1);

        /// <summary>
        /// 누적된 경험치로 몇 레벨이 오르는지 계산한다. 황금 젬 하나로 2~3레벨이
        /// 한 번에 오를 수 있으므로 반복 처리한다.
        /// </summary>
        public static LevelProgress Resolve(float xpTowardNext, int currentLevel, float baseXp, float increment)
        {
            int gained = 0;
            int level = currentLevel;
            float remaining = xpTowardNext;

            while (true)
            {
                float required = GetRequiredXp(level, baseXp, increment);
                if (required <= 0f || remaining < required) break;

                remaining -= required;
                level++;
                gained++;
            }

            return new LevelProgress { XpTowardNext = remaining, LevelsGained = gained };
        }

        /// <summary>다음 레벨까지의 진행률(0~1). XP 게이지에 쓴다.</summary>
        public static float GetProgressRatio(float xpTowardNext, int level, float baseXp, float increment)
        {
            float required = GetRequiredXp(level, baseXp, increment);
            return required <= 0f ? 0f : UnityEngine.Mathf.Clamp01(xpTowardNext / required);
        }
    }
}
```

- [ ] **Step 5: 테스트가 통과하는지 확인한다**

기대: 239개 통과 (기존 234 + 신규 5).

- [ ] **Step 6: 커밋**

```bash
git add unity/Assets/_Project/Scripts/Core/LevelCurve.cs unity/Assets/Tests/EditMode/LevelCurveTests.cs
git commit -m "feat: LevelCurve에 GetProgressRatio 추가"
```

---

## Task 2: `LevelSystem`에 진행률 노출

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Core/LevelSystem.cs`

**Interfaces:**
- Consumes: `SushiSurvival.Core.LevelCurve.GetProgressRatio(...)` (Task 1)
- Produces: `SushiSurvival.Core.LevelSystem.ProgressRatio` (public float 프로퍼티)

- [ ] **Step 1: 프로퍼티를 추가한다**

`public int CurrentLevel { get; private set; } = 1;` 아래에 추가:

```csharp
        public int CurrentLevel { get; private set; } = 1;

        /// <summary>다음 레벨까지의 진행률(0~1). XpGaugeDisplay가 매 프레임 읽는다.</summary>
        public float ProgressRatio => LevelCurve.GetProgressRatio(_xpTowardNext, CurrentLevel, baseXp, xpIncrementPerLevel);
```

- [ ] **Step 2: 컴파일이 통과하는지 확인한다**

기대: 239개 통과, 컴파일 에러 없음.

- [ ] **Step 3: 커밋**

```bash
git add unity/Assets/_Project/Scripts/Core/LevelSystem.cs
git commit -m "feat: LevelSystem에 ProgressRatio 노출"
```

---

## Task 3: XP 게이지·처치 수 UI

**Files:**
- Create: `unity/Assets/_Project/Scripts/UI/XpGaugeDisplay.cs`
- Create: `unity/Assets/_Project/Scripts/UI/KillCountDisplay.cs`

**Interfaces:**
- Consumes:
  - `SushiSurvival.Core.LevelSystem.ProgressRatio` (Task 2)
  - `SushiSurvival.UI.HealthBarLogic.MoveTowardsFill(float, float, float) → float` (기존)
  - `SushiSurvival.Core.GameManager.Instance` / `.CurrentState` / `.KillCount` (기존)
- Produces: 없음(둘 다 자기 완결적인 표시 컴포넌트)

- [ ] **Step 1: `XpGaugeDisplay`를 구현한다**

`unity/Assets/_Project/Scripts/UI/XpGaugeDisplay.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 화면 최상단 풀와이드 XP 게이지. 레벨업 팝업이 열려 스탯이 재계산되는
    /// 동안에도 부드럽게 채워지도록 스무딩한다.
    /// </summary>
    public class XpGaugeDisplay : MonoBehaviour
    {
        [SerializeField] private Core.LevelSystem levelSystem;
        [Tooltip("Image Type을 Filled로 설정한 채움 이미지.")]
        [SerializeField] private Image fillImage;
        [Tooltip("초당 채움 변화량.")]
        [SerializeField] private float fillSpeed = 3f;

        private float _currentFill;

        private void Update()
        {
            if (levelSystem == null || fillImage == null) return;

            _currentFill = HealthBarLogic.MoveTowardsFill(_currentFill, levelSystem.ProgressRatio, fillSpeed * Time.deltaTime);
            fillImage.fillAmount = _currentFill;
        }
    }
}
```

- [ ] **Step 2: `KillCountDisplay`를 구현한다**

`unity/Assets/_Project/Scripts/UI/KillCountDisplay.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace SushiSurvival.UI
{
    /// <summary>화면 상단 우측 처치 수. 캐릭터 선택·결과 화면에서는 숨긴다.</summary>
    public class KillCountDisplay : MonoBehaviour
    {
        [SerializeField] private Text countText;

        private void Update()
        {
            if (countText == null) return;

            var manager = Core.GameManager.Instance;
            if (manager == null) return;

            bool playing = manager.CurrentState == Core.RunState.Playing;
            countText.enabled = playing;
            if (!playing) return;

            countText.text = manager.KillCount.ToString();
        }
    }
}
```

- [ ] **Step 3: 컴파일이 통과하는지 확인한다**

기대: 239개 통과, 컴파일 에러 없음.

- [ ] **Step 4: 커밋**

```bash
git add unity/Assets/_Project/Scripts/UI/XpGaugeDisplay.cs unity/Assets/_Project/Scripts/UI/XpGaugeDisplay.cs.meta unity/Assets/_Project/Scripts/UI/KillCountDisplay.cs unity/Assets/_Project/Scripts/UI/KillCountDisplay.cs.meta
git commit -m "feat: XpGaugeDisplay·KillCountDisplay HUD 컴포넌트 추가"
```

---

## Task 4: 체력바 — HUD 이전을 위한 런타임 연결

**Files:**
- Modify: `unity/Assets/_Project/Scripts/UI/HealthBar.cs`
- Modify: `unity/Assets/_Project/Scripts/Core/GameManager.cs`

**Interfaces:**
- Produces: `SushiSurvival.UI.HealthBar.SetTarget(PlayerHealth health, Sprite portrait)`

- [ ] **Step 1: `HealthBar.cs` 전체를 교체한다**

```csharp
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Player;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 플레이어 체력바. 캐릭터 종류와 무관한 HUD 코너 오브젝트라 인스펙터로
    /// 미리 연결할 수 없다 — GameManager가 스폰 직후 SetTarget으로 알려준다
    /// (CameraFollow.SetTarget과 같은 패턴).
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private PlayerHealth playerHealth;
        [Tooltip("Image Type을 Filled로 설정한 채움 이미지.")]
        [SerializeField] private Image fillImage;
        [Tooltip("캐릭터 초상화. 비워두면 표시하지 않는다.")]
        [SerializeField] private Image portraitImage;
        [Tooltip("초당 채움 변화량. 2면 0→1이 0.5초 걸린다.")]
        [SerializeField] private float fillSpeed = 2f;

        private float _currentFill = 1f;
        private float _targetFill = 1f;

        private void OnEnable()
        {
            if (playerHealth == null) return;

            playerHealth.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            if (playerHealth == null) return;

            playerHealth.OnHealthChanged -= HandleHealthChanged;
        }

        /// <summary>
        /// 런타임에 플레이어가 스폰된 뒤 GameManager가 호출한다. 이전 대상이
        /// 있으면(재시작 등) 먼저 구독을 해제해 중복 구독을 막는다.
        /// </summary>
        public void SetTarget(PlayerHealth health, Sprite portrait)
        {
            if (playerHealth != null)
                playerHealth.OnHealthChanged -= HandleHealthChanged;

            playerHealth = health;

            if (playerHealth != null)
                playerHealth.OnHealthChanged += HandleHealthChanged;

            if (portraitImage != null)
                portraitImage.sprite = portrait;
        }

        private void Update()
        {
            if (fillImage == null) return;

            _currentFill = HealthBarLogic.MoveTowardsFill(_currentFill, _targetFill, fillSpeed * Time.deltaTime);
            fillImage.fillAmount = _currentFill;
        }

        private void HandleHealthChanged(float current, float max)
        {
            _targetFill = HealthBarLogic.ComputeFillAmount(current, max);
        }
    }
}
```

- [ ] **Step 2: `GameManager`에 필드와 호출을 추가한다**

`[SerializeField] private SushiSurvival.UI.ResultPanel resultPanel;` 아래에
추가:

```csharp
        [SerializeField] private SushiSurvival.UI.ResultPanel resultPanel;
        [Tooltip("HUD 코너의 플레이어 체력바. 스폰 직후 연결된다.")]
        [SerializeField] private SushiSurvival.UI.HealthBar hudHealthBar;
```

`StartRun`의 `cameraFollow.SetTarget(_playerTransform);` 바로 다음 줄에 추가:

```csharp
            cameraFollow.SetTarget(_playerTransform);

            if (hudHealthBar != null)
                hudHealthBar.SetTarget(_playerHealth, characterData.portraitSprite);
```

- [ ] **Step 3: 컴파일이 통과하는지 확인한다**

기대: 239개 통과, 컴파일 에러 없음.

- [ ] **Step 4: 커밋**

```bash
git add unity/Assets/_Project/Scripts/UI/HealthBar.cs unity/Assets/_Project/Scripts/Core/GameManager.cs
git commit -m "feat: HealthBar를 HUD 코너로 이전할 수 있게 SetTarget 추가"
```

---

## Task 5: 타이머 — 보스 등장 시 위치 트윈

**Files:**
- Modify: `unity/Assets/_Project/Scripts/UI/RunTimerDisplay.cs`

**Interfaces:**
- Consumes: `SushiSurvival.Core.GameManager.ElapsedTime` / `.BossSpawnTime` (기존, 이미 쓰고 있음)

- [ ] **Step 1: 전체 파일을 교체한다**

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 화면 상단에 남은 시간을 표시한다. 캐릭터 선택 중과 결과 화면에서는 숨긴다.
    /// 보스가 등장하면(ElapsedTime이 BossSpawnTime을 넘으면) 보스 체력바에게
    /// 자리를 내주고 아래로 밀려난다 — 새 이벤트 연결 없이 이미 읽고 있는
    /// 값으로 스스로 판단한다.
    /// </summary>
    public class RunTimerDisplay : MonoBehaviour
    {
        [SerializeField] private Text timerText;
        [SerializeField] private float normalY = 12f;
        [SerializeField] private float bossPhaseY = 76f;
        [SerializeField] private float moveDuration = 0.3f;

        private bool _bossPhaseActive;
        private Coroutine _moveRoutine;

        private void Update()
        {
            if (timerText == null) return;

            var manager = GameManager.Instance;
            if (manager == null) return;

            bool playing = manager.CurrentState == RunState.Playing;
            timerText.enabled = playing;

            if (!playing) return;

            // 보스 등장 전에는 남은 시간을 세고, 등장 후에는 생존 시간을 센다.
            // 그대로 두면 보스전 내내 0:00에 멈춰 있어 시계가 고장난 것처럼 보인다.
            timerText.text = manager.ElapsedTime < manager.BossSpawnTime
                ? RunClock.FormatRemaining(manager.ElapsedTime, manager.BossSpawnTime)
                : RunClock.FormatElapsed(manager.ElapsedTime);

            bool bossPhase = manager.ElapsedTime >= manager.BossSpawnTime;
            if (bossPhase != _bossPhaseActive)
            {
                _bossPhaseActive = bossPhase;
                if (_moveRoutine != null) StopCoroutine(_moveRoutine);
                _moveRoutine = StartCoroutine(MoveTo(bossPhase ? bossPhaseY : normalY));
            }
        }

        private IEnumerator MoveTo(float targetY)
        {
            RectTransform rect = timerText.rectTransform;
            Vector2 start = rect.anchoredPosition;
            var target = new Vector2(start.x, targetY);
            float elapsed = 0f;

            // 게임 진행 중의 연출이라 timeScale이 항상 1이다 — 레벨업 팝업
            // 스케일인과 달리 실시간(unscaled)을 쓸 필요가 없다.
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                rect.anchoredPosition = Vector2.Lerp(start, target, Mathf.Clamp01(elapsed / moveDuration));
                yield return null;
            }

            rect.anchoredPosition = target;
            _moveRoutine = null;
        }
    }
}
```

- [ ] **Step 2: 컴파일이 통과하는지 확인한다**

기대: 239개 통과, 컴파일 에러 없음.

- [ ] **Step 3: 커밋**

```bash
git add unity/Assets/_Project/Scripts/UI/RunTimerDisplay.cs
git commit -m "feat: RunTimerDisplay가 보스 등장 시 아래로 밀려나도록 변경"
```

---

## Task 6: Unity Editor 작업 (사용자가 직접)

에이전트는 여기까지 코드를 완성했다. 아래는 GUI 전용 작업이라 사용자가 직접 한다.

### 6-1. XP 게이지 UI 조립

`GameScene.unity`의 `Canvas` 최상단에 풀와이드 바 하나 생성.

```
XpGauge
├─ Background     ← Image (rgba(255,255,255,0.08) 등 트랙 색)
└─ Fill           ← Image, Image Type = Filled, Fill Method = Horizontal
```

높이 5~6px, 앵커를 화면 상단 전체 너비로.

`XpGauge`에 **Add Component → Xp Gauge Display**:

| 필드 | 연결 대상 |
|---|---|
| Level System | 씬의 `LevelSystem` 오브젝트 |
| Fill Image | `Fill`의 Image |

### 6-2. 처치 수 UI 조립

상단 우측에 아이콘 + `Text`(Legacy) 배치.

`Add Component → Kill Count Display` → **Count Text** 필드에 그 Text 연결.

### 6-3. HUD 체력바 UI 조립

화면 하단 좌측에 원형 초상화 + 체력바.

```
HudHealthBar
├─ Portrait        ← Image (원형 마스크, 테두리 별도 Image로 겹쳐도 됨)
└─ Bar
   ├─ Background   ← Image
   └─ Fill         ← Image, Image Type = Filled, Fill Method = Horizontal
```

`HudHealthBar`에 **Add Component → Health Bar**:

| 필드 | 값 |
|---|---|
| Player Health | **비워둔다** — 런타임에 `GameManager`가 연결한다 |
| Fill Image | `Fill`의 Image |
| Portrait Image | `Portrait`의 Image |
| Fill Speed | 2 (기본값) |

### 6-4. 캐릭터 프리팹에서 기존 체력바 제거

`Assets/_Project/Prefabs/EggPlayer.prefab`, `ShrimpPlayer.prefab`을 각각 열어
`HealthBarCanvas`(월드스페이스 체력바) 오브젝트를 찾아 **삭제**한다.

⚠️ **주의:** 이 오브젝트 안의 `HealthBar` 컴포넌트가 `PlayerHealth`를
인스펙터로 직접 참조하고 있었다. 삭제 전에 다른 곳에서 이 오브젝트를
참조하고 있지 않은지 확인한다(현재 구조상 참조하는 곳 없음).

### 6-5. `GameManager` 연결

씬의 `GameManager` 선택 → **Hud Health Bar** 필드에 6-3의 `HudHealthBar`
오브젝트 연결.

### 6-6. `RunTimerDisplay` 필드 확인

기존 타이머 오브젝트 선택 → `Run Timer Display` 컴포넌트의 `Normal Y`/
`Boss Phase Y`/`Move Duration`이 기본값(12 / 76 / 0.3)으로 들어있는지 확인.
필요하면 실제 화면에 맞게 조정.

### 6-7. `BossHealthBar` 색상 교체

기존 `BossHealthBarUI`의 `Fill`/`Background` Image 색상을 `UI_SPEC.md` 5장
블루 팔레트로 교체:

| 요소 | 값 |
|---|---|
| Fill | `#378ADD` |
| 라벨 텍스트 | `#85B7EB` |

---

## Task 7: 플레이테스트

- [ ] **XP 게이지** — 젬을 먹을 때마다 최상단 바가 부드럽게 채워진다. 레벨업
  하는 순간 게이지가 비워지고 다시 채워지기 시작한다
- [ ] **처치 수** — 몹을 죽일 때마다 숫자가 올라간다
- [ ] **HUD 체력바** — 캐릭터가 스폰된 직후부터 초상화와 체력이 정상 표시된다
  (계란·간장새우 둘 다 확인). 피격 시 부드럽게 줄어든다
- [ ] **월드스페이스 체력바 제거 확인** — 캐릭터 발밑에 예전 체력바가 더 이상
  안 뜬다
- [ ] **타이머 이동** — 5:00(또는 테스트용으로 낮춘 `Boss Spawn Time`)에
  도달하면 타이머가 아래로 부드럽게 밀려나고, 보스 체력바가 그 자리에 뜬다
- [ ] **재시작** — 결과 화면에서 다시 하기 → 두 번째 판에서도 HUD 체력바가
  새로 스폰된 캐릭터에 정상 연결된다(중복 구독으로 두 번 반응하지 않는다)
- [ ] **회귀** — 레벨업 팝업, 왕의 와사비 하사, 보스전, 게임오버가 이전과
  동일하게 동작한다

전부 통과하면 `main`으로 병합할지 사용자에게 확인한다.

---

## Self-Review 기록

**스펙 커버리지** — XP 게이지(Task 1, 2, 3), 처치 수(Task 3), 체력바 HUD
이전(Task 4, 6-3, 6-4), 타이머 위치 트윈(Task 5), 보스 체력바 색상(Task
6-7) 전부 대응됨. "재화 카운트"·무기 슬롯은 스펙의 스코프 밖에 이미 명시돼
있어 태스크 없음.

**타입 일관성** — `LevelCurve.GetProgressRatio(float, int, float, float)`가
Task 1 정의와 Task 2 호출부에서 일치. `HealthBar.SetTarget(PlayerHealth,
Sprite)`가 Task 4 정의와 Task 4의 `GameManager` 호출부에서 일치.
`LevelSystem.ProgressRatio`가 Task 2 정의와 Task 3의 `XpGaugeDisplay`
호출부에서 일치.

**플레이스홀더 스캔** — 없음. 모든 코드 블록이 실제 완성된 내용.

**의도적 순서** — Task 1(순수 로직) → Task 2(노출) → Task 3(소비하는 UI)
순서로, 뒤 태스크가 앞 태스크의 산출물을 그대로 쓴다. Task 4·5는 서로
독립적이라 순서 무관하지만 계획 가독성을 위해 순서대로 배치했다.
