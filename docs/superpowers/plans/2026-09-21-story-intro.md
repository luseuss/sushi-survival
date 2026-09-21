# 세계관 설명 단계(StoryScene) + 범용 선형 대화 엔진 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `IntroScene`의 시작 버튼 뒤에 캐릭터 3인의 세계관 대화 장면(`StoryScene`)을 넣고, 이를 위해 "대사 넘기기 + 배경 전환" 선형 대화 엔진을 범용으로 만든다.

**Architecture:** 데이터(`StoryDialogueData` ScriptableObject) / 순수 로직(`StoryDialogueLogic`, EditMode 테스트) / 뷰(`StoryPanel`) / 진입점(`StorySceneController`)으로 분리한다. 프로젝트 컨벤션대로 판정 로직은 Unity 의존 없는 정적 클래스로 빼서 테스트하고, MonoBehaviour는 얇은 접착제만 둔다. 씬 배치는 사용자가 에디터에서 한다.

**Tech Stack:** Unity 2022.3.62f3, Built-in 2D, 새 Input System(`Keyboard.current`/`Mouse.current`), uGUI **Legacy** `Text`/`Button` (TextMeshPro 미설치), NUnit EditMode 테스트.

**Spec:** `docs/superpowers/specs/2026-09-21-story-intro-design.md`

## 브랜치 / PR 분할

| PR | 브랜치 | 태스크 | 성격 |
|---|---|---|---|
| **PR 1** | `feature/story-intro-scene` (현재 브랜치, 스펙 커밋 있음) | Task 1, 2 | 코드만. 씬이 아직 없어 게임 동작은 안 바뀜 |
| **PR 2** | `feature/story-scene-wiring` (PR 1 병합 후 `main`에서 새로) | Task 3, 4, 5 | 대본 에셋 + `StoryScene` 씬 + `IntroSceneController` 한 줄 + 빌드 세팅 |

`IntroSceneController` 수정은 **반드시 씬과 같은 PR 2에 넣는다** — 따로 병합하면 시작 버튼이 존재하지 않는 씬으로 이동한다.

## Global Constraints

- 스펙 원문 규칙: "**Legacy Text/Button만 사용**"(TextMeshPro 미설치), 배경 전환 등 UI 연출은 "**실시간(`Time.unscaledDeltaTime`)**", 씬 이름은 "**정확히 `StoryScene`**", `EditorBuildSettings`에 `IntroScene` 다음으로 등록.
- 협업 규칙(`docs/COLLABORATION.md`): `main` 직접 커밋·푸시·머지 금지(브랜치 + PR, 머지 버튼은 사람만). `git add .` 금지 — **파일을 명시**하고 `.meta`를 항상 함께 넣는다.
- 판정 로직은 `XxxLogic` 정적 클래스 + EditMode 테스트로 분리한다. 새 순수 로직마다 테스트가 있어야 한다.
- 씬 이름 문자열 오타는 이 프로젝트에서 이미 한 번 실제 버그(`"Game"` vs `"GameScene"`)였다 → Play 모드에서 `Intro → Story → Game` 전환을 직접 확인한다.
- 배치 테스트 실행 규칙: **Unity 에디터를 완전히 닫은 상태**에서, `-runTests`와 `-quit`을 **같이 쓰지 않고**, `-logFile`은 **파일 경로가 아니라 `-`(stdout)** 로 준다(파일로 주면 도메인 리로드 때 결과가 조용히 사라진다).

### 배치 테스트 실행 명령 (이 문서에서 "테스트 실행"이라 부르는 것)

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요\unity"
"C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" -batchmode -projectPath "$(pwd -W)" -runTests -testPlatform EditMode -testResults "$(pwd -W)/TestResults.xml" -logFile - > "$TEMP/unity_test.log" 2>&1
grep -E "error CS" "$TEMP/unity_test.log" | head -20
head -c 300 TestResults.xml
```

성공 기준: `error CS`가 없고 `TestResults.xml`의 `result="Passed"`, `failed="0"`. 끝나면 `rm -f TestResults.xml`. (`TestResults.xml`이 안 생기고 로그에 `HandleProjectAlreadyOpenInAnotherInstance`가 있으면 에디터가 열려 있는 것이다.)

---

## File Structure

| 파일 | 역할 |
|---|---|
| `unity/Assets/_Project/Scripts/Core/StoryDialogueLogic.cs` (신규) | 줄 진행·끝 판정·배경 유지/전환 판정(순수 로직) |
| `unity/Assets/Tests/EditMode/StoryDialogueLogicTests.cs` (신규) | 위 로직 테스트 16개 |
| `unity/Assets/_Project/Scripts/Data/StoryDialogueData.cs` (신규) | `StoryLine` + `StoryDialogueData` ScriptableObject |
| `unity/Assets/_Project/Scripts/UI/StoryPanel.cs` (신규) | 배경 크로스페이드, 이름표/초상화/대사 표시(뷰) |
| `unity/Assets/_Project/Scripts/UI/StorySceneController.cs` (신규) | 입력 처리, 줄 진행, 건너뛰기, 다음 씬 이동 |
| `unity/Assets/_Project/Data/WorldIntroStory.asset` (신규, PR 2) | 4페이지 대본 11줄 |
| `unity/Assets/Tests/EditMode/WorldIntroStoryAssetTests.cs` (신규, PR 2) | 대본 에셋 무결성 테스트 |
| `unity/Assets/_Project/Scenes/StoryScene.unity` (신규, PR 2) | 사용자가 에디터에서 구성 |
| `unity/Assets/_Project/Scripts/UI/IntroSceneController.cs` (수정, PR 2) | 이동 대상 `"GameScene"` → `"StoryScene"` (협업자 파일) |
| `unity/ProjectSettings/EditorBuildSettings.asset` (수정, PR 2) | `StoryScene` 등록 |

---

# PR 1 — 대화 엔진 코드

### Task 1: `StoryDialogueLogic` (TDD)

**Files:**
- Create: `unity/Assets/Tests/EditMode/StoryDialogueLogicTests.cs`
- Create: `unity/Assets/_Project/Scripts/Core/StoryDialogueLogic.cs`

**Interfaces:**
- Consumes: 없음
- Produces (Task 2가 사용):
  - `static int NextIndex(int current, int count)` — 다음 줄 번호. 끝을 넘으면 `count`, 음수는 "첫 줄 이전"으로 취급해 0
  - `static bool IsFinished(int index, int count)` — `count <= 0 || index >= count`
  - `static int ResolveBackgroundIndex(IReadOnlyList<bool> hasBackground, int index)` — `index` 이하에서 가장 가까운 "배경 지정 줄" 번호, 없거나 잘못된 입력이면 -1
  - `static bool IsBackgroundChange(IReadOnlyList<bool> hasBackground, int index)` — 범위 안이고 그 줄이 배경을 지정하면 true

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/StoryDialogueLogicTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class StoryDialogueLogicTests
    {
        // ---- NextIndex ----

        [Test]
        public void NextIndex_MovesToNextLine()
        {
            Assert.AreEqual(1, StoryDialogueLogic.NextIndex(0, 3));
        }

        [Test]
        public void NextIndex_ReturnsCount_WhenAdvancingPastLastLine()
        {
            Assert.AreEqual(3, StoryDialogueLogic.NextIndex(2, 3));
        }

        [Test]
        public void NextIndex_ClampsToCount_WhenAlreadyPastEnd()
        {
            Assert.AreEqual(3, StoryDialogueLogic.NextIndex(3, 3));
            Assert.AreEqual(3, StoryDialogueLogic.NextIndex(5, 3));
        }

        [Test]
        public void NextIndex_ReturnsZero_ForEmptyData()
        {
            Assert.AreEqual(0, StoryDialogueLogic.NextIndex(0, 0));
            Assert.AreEqual(0, StoryDialogueLogic.NextIndex(-1, 0));
        }

        [Test]
        public void NextIndex_TreatsNegativeCurrentAsBeforeFirst()
        {
            Assert.AreEqual(0, StoryDialogueLogic.NextIndex(-1, 3));
            Assert.AreEqual(0, StoryDialogueLogic.NextIndex(-5, 3));
        }

        // ---- IsFinished ----

        [Test]
        public void IsFinished_False_WhileLinesRemain()
        {
            Assert.IsFalse(StoryDialogueLogic.IsFinished(0, 3));
            Assert.IsFalse(StoryDialogueLogic.IsFinished(2, 3));
        }

        [Test]
        public void IsFinished_True_AtOrPastEnd()
        {
            Assert.IsTrue(StoryDialogueLogic.IsFinished(3, 3));
            Assert.IsTrue(StoryDialogueLogic.IsFinished(4, 3));
        }

        [Test]
        public void IsFinished_True_ForEmptyData()
        {
            Assert.IsTrue(StoryDialogueLogic.IsFinished(0, 0));
            Assert.IsTrue(StoryDialogueLogic.IsFinished(-1, 0));
        }

        // ---- ResolveBackgroundIndex ----

        [Test]
        public void ResolveBackgroundIndex_ReturnsMinusOne_WhenNoBackgroundDefined()
        {
            Assert.AreEqual(-1, StoryDialogueLogic.ResolveBackgroundIndex(new[] { false, false }, 1));
        }

        [Test]
        public void ResolveBackgroundIndex_ReturnsSameIndex_WhenThatLineDefinesOne()
        {
            Assert.AreEqual(0, StoryDialogueLogic.ResolveBackgroundIndex(new[] { true, false, false }, 0));
        }

        [Test]
        public void ResolveBackgroundIndex_ReturnsNearestPrecedingDefinition()
        {
            var has = new[] { true, false, true, false };

            Assert.AreEqual(0, StoryDialogueLogic.ResolveBackgroundIndex(has, 1));
            Assert.AreEqual(2, StoryDialogueLogic.ResolveBackgroundIndex(has, 3));
        }

        [Test]
        public void ResolveBackgroundIndex_IgnoresLaterDefinitions()
        {
            Assert.AreEqual(-1, StoryDialogueLogic.ResolveBackgroundIndex(new[] { false, false, true }, 1));
        }

        [Test]
        public void ResolveBackgroundIndex_ClampsIndexBeyondEnd()
        {
            Assert.AreEqual(0, StoryDialogueLogic.ResolveBackgroundIndex(new[] { true, false }, 9));
        }

        [Test]
        public void ResolveBackgroundIndex_ReturnsMinusOne_ForInvalidInput()
        {
            Assert.AreEqual(-1, StoryDialogueLogic.ResolveBackgroundIndex(null, 0));
            Assert.AreEqual(-1, StoryDialogueLogic.ResolveBackgroundIndex(new bool[0], 0));
            Assert.AreEqual(-1, StoryDialogueLogic.ResolveBackgroundIndex(new[] { true }, -1));
        }

        // ---- IsBackgroundChange ----

        [Test]
        public void IsBackgroundChange_True_OnlyOnLinesThatDefineOne()
        {
            var has = new[] { true, false, true };

            Assert.IsTrue(StoryDialogueLogic.IsBackgroundChange(has, 0));
            Assert.IsFalse(StoryDialogueLogic.IsBackgroundChange(has, 1));
            Assert.IsTrue(StoryDialogueLogic.IsBackgroundChange(has, 2));
        }

        [Test]
        public void IsBackgroundChange_False_ForOutOfRangeOrNull()
        {
            var has = new[] { true, true, true };

            Assert.IsFalse(StoryDialogueLogic.IsBackgroundChange(has, -1));
            Assert.IsFalse(StoryDialogueLogic.IsBackgroundChange(has, 3));
            Assert.IsFalse(StoryDialogueLogic.IsBackgroundChange(null, 0));
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해 실패 확인**

"배치 테스트 실행 명령"을 실행한다.
Expected: `error CS0103: The name 'StoryDialogueLogic' does not exist in the current context` 류의 컴파일 에러가 로그에 나오고 `TestResults.xml`이 생기지 않는다(= FAIL).

- [ ] **Step 3: 최소 구현**

`unity/Assets/_Project/Scripts/Core/StoryDialogueLogic.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 선형 대화(StoryDialogueData)의 진행 판정. Unity 오브젝트에 의존하지 않도록
    /// "이 줄이 배경을 지정하는가"만 bool 목록으로 받는다.
    /// </summary>
    public static class StoryDialogueLogic
    {
        /// <summary>다음 줄 번호. 끝을 넘으면 count를 돌려준다(그 값이 "끝"이다).</summary>
        public static int NextIndex(int current, int count)
        {
            int safeCount = Mathf.Max(0, count);
            return Mathf.Clamp(current + 1, 0, safeCount);
        }

        /// <summary>줄이 하나도 없거나 index가 끝에 닿았으면 true.</summary>
        public static bool IsFinished(int index, int count)
            => count <= 0 || index >= count;

        /// <summary>
        /// index 이하에서 가장 가까운 "배경을 지정한 줄"의 번호. 배경이 비어 있는 줄은
        /// 직전 배경을 유지하므로, 지금 화면에 깔려 있어야 할 배경이 어느 줄 것인지 알려준다.
        /// 없거나 입력이 잘못됐으면 -1.
        /// </summary>
        public static int ResolveBackgroundIndex(IReadOnlyList<bool> hasBackground, int index)
        {
            if (hasBackground == null || hasBackground.Count == 0 || index < 0) return -1;

            for (int i = Mathf.Min(index, hasBackground.Count - 1); i >= 0; i--)
            {
                if (hasBackground[i]) return i;
            }

            return -1;
        }

        /// <summary>이 줄에서 배경이 새로 바뀌는가(= 이 줄이 배경을 지정했는가).</summary>
        public static bool IsBackgroundChange(IReadOnlyList<bool> hasBackground, int index)
            => hasBackground != null && index >= 0 && index < hasBackground.Count && hasBackground[index];
    }
}
```

- [ ] **Step 4: 테스트 실행해 통과 확인**

"배치 테스트 실행 명령"을 실행한다.
Expected: `error CS` 없음, `total="255" passed="255" failed="0"` (기존 239 + 새 16).
끝나면 `rm -f TestResults.xml`. 이때 Unity가 새 파일의 `.meta`를 만들었는지 확인한다:

```bash
ls "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요\unity\Assets\_Project\Scripts\Core\StoryDialogueLogic.cs.meta" "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요\unity\Assets\Tests\EditMode\StoryDialogueLogicTests.cs.meta"
```

(`.meta`가 없으면 에디터를 한 번 열었다가 닫은 뒤 다시 확인한다.)

- [ ] **Step 5: 커밋**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/Core/StoryDialogueLogic.cs unity/Assets/_Project/Scripts/Core/StoryDialogueLogic.cs.meta unity/Assets/Tests/EditMode/StoryDialogueLogicTests.cs unity/Assets/Tests/EditMode/StoryDialogueLogicTests.cs.meta
git commit -m "feat: 선형 대화 진행 판정 로직 StoryDialogueLogic 추가

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 2: 데이터 · 뷰 · 컨트롤러

**Files:**
- Create: `unity/Assets/_Project/Scripts/Data/StoryDialogueData.cs`
- Create: `unity/Assets/_Project/Scripts/UI/StoryPanel.cs`
- Create: `unity/Assets/_Project/Scripts/UI/StorySceneController.cs`

**Interfaces:**
- Consumes (Task 1): `StoryDialogueLogic.NextIndex / IsFinished / ResolveBackgroundIndex / IsBackgroundChange` (시그니처는 Task 1 참고)
- Produces (Task 3~5가 사용):
  - `StoryLine { string speakerName; Sprite portrait; string text; Sprite background; }` (`[Serializable]`)
  - `StoryDialogueData : ScriptableObject { StoryLine[] lines; }`, 메뉴 `SushiSurvival/Story Dialogue Data`
  - `StoryPanel` 직렬화 필드: `backgroundFront`, `backgroundBack`(둘 다 `Image`), `nameplateRoot`(`GameObject`), `nameText`(`Text`), `miniPortrait`(`Image`), `bodyText`(`Text`)
  - `StorySceneController` 직렬화 필드: `data`(`StoryDialogueData`), `panel`(`StoryPanel`), `skipButton`(`Button`), `nextSceneName`(`string`, 기본 `"GameScene"`), `backgroundFadeSeconds`(`float`, 기본 0.35)

이 세 파일은 MonoBehaviour/ScriptableObject라 기존 관례대로 EditMode 테스트 대상이 아니다. 검증은 컴파일 + 기존 테스트 회귀 없음으로 한다.

- [ ] **Step 1: 데이터 클래스 작성**

`unity/Assets/_Project/Scripts/Data/StoryDialogueData.cs`:

```csharp
using System;
using UnityEngine;

namespace SushiSurvival.Data
{
    /// <summary>대사 한 줄. 호감도 대화와 달리 선택지·스탯 버프는 없다.</summary>
    [Serializable]
    public class StoryLine
    {
        [Tooltip("이름표에 표시. 비우면 이름표를 숨긴다(내레이션용).")]
        public string speakerName;
        [Tooltip("대사창 안 작은 초상화. 비우면 초상화 창을 숨긴다.")]
        public Sprite portrait;
        [TextArea]
        public string text;
        [Tooltip("이 줄에서 바뀔 배경. 비우면 직전 배경을 유지한다.")]
        public Sprite background;
    }

    /// <summary>선형 대화 한 세트. StorySceneController가 순서대로 넘긴다.</summary>
    [CreateAssetMenu(menuName = "SushiSurvival/Story Dialogue Data", fileName = "NewStoryDialogueData")]
    public class StoryDialogueData : ScriptableObject
    {
        public StoryLine[] lines;
    }
}
```

- [ ] **Step 2: 뷰 작성**

`unity/Assets/_Project/Scripts/UI/StoryPanel.cs`:

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 선형 대화 화면의 뷰. 배경 두 장으로 크로스페이드하고, 이름표·초상화·대사를
    /// 표시한다. 무엇을 언제 보여줄지는 StorySceneController가 정한다.
    /// </summary>
    public class StoryPanel : MonoBehaviour
    {
        [Header("배경 (크로스페이드용 2장)")]
        [Tooltip("현재 배경. 하이어라키에서 Back보다 아래(나중에 그려지는 쪽)에 둔다.")]
        [SerializeField] private Image backgroundFront;
        [Tooltip("전환 대상 배경. Front 바로 뒤에 깔린다.")]
        [SerializeField] private Image backgroundBack;

        [Header("대사창")]
        [SerializeField] private GameObject nameplateRoot;
        [SerializeField] private Text nameText;
        [SerializeField] private Image miniPortrait;
        [SerializeField] private Text bodyText;

        private Coroutine _fade;
        private Sprite _pending;

        public void ShowLine(StoryLine line)
        {
            bool hasName = !string.IsNullOrEmpty(line.speakerName);

            if (nameplateRoot != null)
                nameplateRoot.SetActive(hasName);

            if (nameText != null)
                nameText.text = hasName ? line.speakerName : string.Empty;

            if (miniPortrait != null)
            {
                miniPortrait.sprite = line.portrait;
                miniPortrait.gameObject.SetActive(line.portrait != null);
            }

            if (bodyText != null)
                bodyText.text = line.text;
        }

        /// <summary>페이드 없이 즉시 배경을 바꾼다. null이면 배경을 끈다(카메라 배경색이 보인다).</summary>
        public void SetBackground(Sprite sprite)
        {
            StopFade();
            ApplyBackground(sprite);
        }

        /// <summary>
        /// 배경을 페이드로 바꾼다. 이미 페이드 중이면 그것부터 즉시 끝낸다 — 빠르게 연타해도
        /// 중간 화면이 튀지 않게 하기 위해서다. 실시간(unscaled)으로 진행한다.
        /// </summary>
        public void FadeToBackground(Sprite sprite, float seconds)
        {
            if (_fade != null)
            {
                StopCoroutine(_fade);
                _fade = null;
                ApplyBackground(_pending);
            }

            if (seconds <= 0f || !isActiveAndEnabled)
            {
                ApplyBackground(sprite);
                return;
            }

            _pending = sprite;
            _fade = StartCoroutine(FadeRoutine(sprite, seconds));
        }

        private IEnumerator FadeRoutine(Sprite next, float seconds)
        {
            if (backgroundBack != null)
            {
                backgroundBack.sprite = next;
                backgroundBack.enabled = next != null;
                SetAlpha(backgroundBack, 1f);
            }

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                if (backgroundFront != null)
                    SetAlpha(backgroundFront, 1f - Mathf.Clamp01(elapsed / seconds));
                yield return null;
            }

            _fade = null;
            ApplyBackground(next);
        }

        private void StopFade()
        {
            if (_fade == null) return;

            StopCoroutine(_fade);
            _fade = null;
        }

        private void ApplyBackground(Sprite sprite)
        {
            if (backgroundFront != null)
            {
                backgroundFront.sprite = sprite;
                backgroundFront.enabled = sprite != null;
                SetAlpha(backgroundFront, 1f);
            }

            if (backgroundBack != null)
                backgroundBack.enabled = false;
        }

        private static void SetAlpha(Image image, float alpha)
        {
            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }
    }
}
```

- [ ] **Step 3: 컨트롤러 작성**

`unity/Assets/_Project/Scripts/UI/StorySceneController.cs`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 선형 대화 장면의 진입점. 클릭/Space/Enter로 한 줄씩 넘기고, 끝나거나 건너뛰면
    /// 다음 씬으로 이동한다. 데이터가 없으면 경고만 남기고 바로 넘어간다 — 대본이 아직
    /// 없어도 게임 흐름은 끊기면 안 된다(호감도 대화 컨트롤러와 같은 방침).
    /// </summary>
    public class StorySceneController : MonoBehaviour
    {
        [SerializeField] private StoryDialogueData data;
        [SerializeField] private StoryPanel panel;
        [SerializeField] private Button skipButton;
        [Tooltip("대화가 끝나거나 건너뛰면 이동할 씬. Build Settings 등록명과 정확히 같아야 한다.")]
        [SerializeField] private string nextSceneName = "GameScene";
        [Tooltip("배경 전환 페이드 시간(초, 실시간).")]
        [SerializeField] private float backgroundFadeSeconds = 0.35f;

        private bool[] _hasBackground;
        private int _index;
        private bool _leaving;
        private RectTransform _skipRect;

        private void Awake()
        {
            if (skipButton == null) return;

            skipButton.onClick.AddListener(Leave);
            _skipRect = skipButton.transform as RectTransform;
        }

        private void OnDestroy()
        {
            if (skipButton != null)
                skipButton.onClick.RemoveListener(Leave);
        }

        private void Start()
        {
            // 결과 화면 등에서 정지된 채 넘어오는 경우를 막는다.
            Time.timeScale = 1f;

            if (data == null || data.lines == null || data.lines.Length == 0 || panel == null)
            {
                Debug.LogWarning($"{name}: 대화 데이터나 패널이 비어 있어 스토리를 건너뜁니다.");
                Leave();
                return;
            }

            _hasBackground = new bool[data.lines.Length];
            for (int i = 0; i < data.lines.Length; i++)
                _hasBackground[i] = data.lines[i].background != null;

            _index = 0;
            ShowCurrent(immediateBackground: true);
        }

        private void Update()
        {
            if (_leaving || _hasBackground == null) return;

            if (AdvancePressed())
                Advance();
        }

        private void Advance()
        {
            _index = StoryDialogueLogic.NextIndex(_index, data.lines.Length);

            if (StoryDialogueLogic.IsFinished(_index, data.lines.Length))
            {
                Leave();
                return;
            }

            ShowCurrent(immediateBackground: false);
        }

        private void ShowCurrent(bool immediateBackground)
        {
            StoryLine line = data.lines[_index];
            panel.ShowLine(line);

            if (immediateBackground)
            {
                // 첫 화면은 페이드 없이, 지금 깔려 있어야 할 배경(없으면 null=단색)을 바로 둔다.
                int backgroundLine = StoryDialogueLogic.ResolveBackgroundIndex(_hasBackground, _index);
                panel.SetBackground(backgroundLine >= 0 ? data.lines[backgroundLine].background : null);
            }
            else if (StoryDialogueLogic.IsBackgroundChange(_hasBackground, _index))
            {
                panel.FadeToBackground(line.background, backgroundFadeSeconds);
            }
        }

        private bool AdvancePressed()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !IsPointerOnSkipButton(mouse))
                return true;

            Keyboard keyboard = Keyboard.current;
            return keyboard != null
                && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame);
        }

        // 건너뛰기 버튼을 누른 클릭이 "다음 줄" 입력으로도 세어지지 않게 한다.
        private bool IsPointerOnSkipButton(Mouse mouse)
            => _skipRect != null
               && RectTransformUtility.RectangleContainsScreenPoint(_skipRect, mouse.position.ReadValue());

        private void Leave()
        {
            if (_leaving) return;

            _leaving = true;
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
```

- [ ] **Step 4: 컴파일 + 회귀 확인**

"배치 테스트 실행 명령"을 실행한다.
Expected: `error CS` 없음, `total="255" passed="255" failed="0"`. 끝나면 `rm -f TestResults.xml`.
새 `.meta` 3개가 생겼는지 확인한다:

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git status --short unity/Assets/_Project/Scripts
```

Expected: `StoryDialogueData.cs`, `StoryPanel.cs`, `StorySceneController.cs`와 각 `.meta`가 `??`로 나온다.

- [ ] **Step 5: 커밋, 푸시, PR 1**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/Data/StoryDialogueData.cs unity/Assets/_Project/Scripts/Data/StoryDialogueData.cs.meta unity/Assets/_Project/Scripts/UI/StoryPanel.cs unity/Assets/_Project/Scripts/UI/StoryPanel.cs.meta unity/Assets/_Project/Scripts/UI/StorySceneController.cs unity/Assets/_Project/Scripts/UI/StorySceneController.cs.meta
git commit -m "feat: 선형 대화 엔진(데이터·뷰·컨트롤러) 추가

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
git push -u origin feature/story-intro-scene
gh pr create --title "feat: 선형 대화 엔진 (세계관 설명 단계 1/2)" --body "$(cat <<'EOF'
## 요약
세계관 설명 단계(`StoryScene`)를 위한 범용 선형 대화 엔진. 이 PR은 코드만 담고, 씬·대본·연결은 다음 PR(2/2)에서 합니다. 이 PR만 병합돼도 게임 동작은 바뀌지 않습니다(아직 이 코드를 쓰는 씬이 없음).

- 스펙: `docs/superpowers/specs/2026-09-21-story-intro-design.md`
- 계획: `docs/superpowers/plans/2026-09-21-story-intro.md`

## 변경 사항
- `StoryDialogueLogic`(순수 로직) + EditMode 테스트 16개
- `StoryDialogueData`(ScriptableObject), `StoryPanel`(뷰), `StorySceneController`(진입점)

## 테스트
- EditMode 255/255 통과 (기존 239 + 신규 16)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

**여기서 멈추고 사용자가 PR 1을 병합하길 기다린다.** (병합 버튼은 사람만 누른다.)

---

# PR 2 — 대본 · 씬 · 연결

PR 1 병합 후 시작한다:

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git branch -d feature/story-intro-scene && git push origin --delete feature/story-intro-scene
git checkout -b feature/story-scene-wiring
```

### Task 3: 대본 데이터 에셋 (TDD)

**Files:**
- Create: `unity/Assets/Tests/EditMode/WorldIntroStoryAssetTests.cs`
- Create: `unity/Assets/_Project/Data/WorldIntroStory.asset` (임시 스크립트로 생성)

**Interfaces:**
- Consumes (Task 2): `StoryDialogueData`, `StoryLine` — 필드 `speakerName`, `portrait`, `text`, `background`
- Produces (Task 4가 사용): 에셋 경로 `Assets/_Project/Data/WorldIntroStory.asset`, 11줄

- [ ] **Step 1: 실패하는 무결성 테스트 작성**

`unity/Assets/Tests/EditMode/WorldIntroStoryAssetTests.cs`:

```csharp
using NUnit.Framework;
using UnityEditor;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    /// <summary>세계관 대본 에셋이 스펙(11줄, 표기 통일, 초상화 규칙)대로 들어갔는지 확인한다.</summary>
    public class WorldIntroStoryAssetTests
    {
        private const string AssetPath = "Assets/_Project/Data/WorldIntroStory.asset";

        private static StoryDialogueData Load()
        {
            var data = AssetDatabase.LoadAssetAtPath<StoryDialogueData>(AssetPath);
            Assert.IsNotNull(data, $"{AssetPath}를 불러올 수 없습니다.");
            return data;
        }

        [Test]
        public void Asset_HasElevenNonEmptyLines()
        {
            var data = Load();

            Assert.AreEqual(11, data.lines.Length);
            foreach (var line in data.lines)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(line.speakerName));
                Assert.IsFalse(string.IsNullOrWhiteSpace(line.text));
            }
        }

        [Test]
        public void OnlyFirstLine_DefinesBackground()
        {
            var data = Load();

            Assert.IsNotNull(data.lines[0].background);
            for (int i = 1; i < data.lines.Length; i++)
                Assert.IsNull(data.lines[i].background, $"{i + 1}번째 줄은 배경을 비워 직전 배경을 유지해야 합니다.");
        }

        [Test]
        public void Portraits_AdelineAndKamarionHaveOne_InariHasNone()
        {
            var data = Load();

            foreach (var line in data.lines)
            {
                if (line.speakerName == "이나리")
                    Assert.IsNull(line.portrait, "이나리는 초상화 없이 이름표만 나와야 합니다.");
                else
                    Assert.IsNotNull(line.portrait, $"{line.speakerName}의 초상화가 비어 있습니다.");
            }
        }

        [Test]
        public void Text_UsesUnifiedKingdomNames()
        {
            var data = Load();

            foreach (var line in data.lines)
            {
                StringAssert.DoesNotContain("초밥왕국", line.text);
                StringAssert.DoesNotContain("롤 왕국", line.text);
                StringAssert.DoesNotContain("스시 왕국", line.text);
            }
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해 실패 확인**

"배치 테스트 실행 명령"을 실행한다.
Expected: 컴파일은 통과하고 새 4개 테스트가 `Assert.IsNotNull failed ... WorldIntroStory.asset를 불러올 수 없습니다`로 실패한다(`failed="4"`). 기존 255개는 통과.

- [ ] **Step 3: 에셋 생성 스크립트 작성·실행**

Task 2에서 만들어진 `StoryDialogueData.cs.meta`의 GUID를 읽어 에셋 YAML을 생성한다. 이 스크립트는 **일회용이라 저장소에 커밋하지 않고** `$TEMP`에 둔다. 초상화/배경 GUID는 기존 에셋에서 그대로 가져온 값이다(`EggCharacterData.asset`의 `portraitSprite`, `ShrimpCharacterData.asset`의 `portraitSprite`, `IntroScene`이 쓰는 왕궁 배경).

`$TEMP/gen_story_asset.js`:

```javascript
const fs = require('fs');
const root = 'C:/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity/Assets/_Project';

const scriptMeta = fs.readFileSync(`${root}/Scripts/Data/StoryDialogueData.cs.meta`, 'utf8');
const scriptGuid = scriptMeta.match(/guid:\s*([0-9a-f]{32})/)[1];

const sprite = (guid) => `{fileID: 21300000, guid: ${guid}, type: 3}`;
const PORTRAIT = {
  '아델린': sprite('b100fb8bffdece446a6415dc441b02c9'),   // EggCharacterData.portraitSprite
  '카마리온': sprite('34c8a59fb576e2943b03f20d9221f163'), // ShrimpCharacterData.portraitSprite
  '이나리': '{fileID: 0}',                                 // 초상화 없음
};
const PALACE = sprite('7d6d771bfc41ae14a9f9f61a592defbb'); // 왕에게 빌기 배경.png

const lines = [
  ['이나리',  '공주님 롤왕국에서 엄청난 수의 적군이 몰려오고 있습니다'],
  ['아델린',  '....역시 소문대로군요'],
  ['카마리온', '병력의 규모는 얼마나 되지?'],
  ['이나리',  '정확한 숫자는 모른다 하지만, 내가 파악한 바로는 롤왕국의 총병력이 출동했다고 한다'],
  ['아델린',  '롤왕국이 정말 작정을 했군요'],
  ['카마리온', '걱정마십시오 공주님 제 손에서 만들어진 무기들은 어떤 왕국보다도 더 훌륭하니까요'],
  ['아델린',  '그럼요 알고있습니다. 카마리온님이 만들어준 제 무기도 훌륭하니까요.. 하지만 저 많은 병력을 우리가 감당할 수 있을까 걱정입니다'],
  ['이나리',  '우리는 전투를 하면 할수록 강해지는 능력이 있으니 걱정마세요'],
  ['카마리온', '그래요 저 유부 고양이 말이 맞습니다'],
  ['이나리',  '난 여우다!!!!!'],
  ['아델린',  '좋습니다. 든든한 지원군이 있으니 안심이 되는군요. 스시왕국을 지키러 다함께 전장으로 향할 때가 되었군요'],
];

// Unity YAML 큰따옴표 문자열: 따옴표/역슬래시를 이스케이프하고 비ASCII는 \uXXXX로.
const q = (s) => '"' + JSON.stringify(s).slice(1, -1)
  .replace(/[^\x20-\x7e]/g, (c) => '\\u' + c.charCodeAt(0).toString(16).padStart(4, '0')) + '"';

let y = `%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${scriptGuid}, type: 3}
  m_Name: WorldIntroStory
  m_EditorClassIdentifier: 
  lines:
`;
lines.forEach(([speaker, text], i) => {
  y += `  - speakerName: ${q(speaker)}
    portrait: ${PORTRAIT[speaker]}
    text: ${q(text)}
    background: ${i === 0 ? PALACE : '{fileID: 0}'}
`;
});

fs.writeFileSync(`${root}/Data/WorldIntroStory.asset`, y);
console.log('wrote WorldIntroStory.asset, script guid', scriptGuid);
```

실행:

```bash
node "$TEMP/gen_story_asset.js"
```

Expected: `wrote WorldIntroStory.asset, script guid <32자리>`

- [ ] **Step 4: 테스트 실행해 통과 확인**

"배치 테스트 실행 명령"을 실행한다. 이 실행이 새 에셋을 임포트하며 `.meta`도 만든다.
Expected: `error CS` 없음, `total="259" passed="259" failed="0"` (255 + 새 4).
끝나면 `rm -f TestResults.xml`. `.meta` 확인:

```bash
ls "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요\unity\Assets\_Project\Data\WorldIntroStory.asset.meta" "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요\unity\Assets\Tests\EditMode\WorldIntroStoryAssetTests.cs.meta"
```

**실패 시 대처:** 에셋 로드 자체가 실패하면(YAML 형식 문제 등) 생성 스크립트를 고치지 말고, 사용자가 에디터에서 `Create → SushiSurvival → Story Dialogue Data`로 에셋을 만들어 위 표의 11줄을 Inspector에 직접 입력한다(테스트는 그대로 검증해 준다).

- [ ] **Step 5: 커밋**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Data/WorldIntroStory.asset unity/Assets/_Project/Data/WorldIntroStory.asset.meta unity/Assets/Tests/EditMode/WorldIntroStoryAssetTests.cs unity/Assets/Tests/EditMode/WorldIntroStoryAssetTests.cs.meta
git commit -m "feat: 세계관 설명 대본 에셋(WorldIntroStory) 추가

사용자 대본 PDF 4페이지 11줄. 표기는 스시왕국/롤왕국으로 통일,
이나리는 초상화 없이 이름표만.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 4: `StoryScene` 씬 구성 (사용자 에디터 작업)

**Files:**
- Create: `unity/Assets/_Project/Scenes/StoryScene.unity` (+ `.meta`)
- Modify: `unity/ProjectSettings/EditorBuildSettings.asset`

에이전트는 씬을 대신 편집하지 않는다(프로젝트 관례). 아래 순서를 사용자가 따라 하고, 에이전트는 저장된 씬 파일을 읽어 배선을 검증한다. **모든 텍스트·버튼은 Legacy(`UI → Text`, `UI → Button (Legacy)`)만 쓴다.**

- [ ] **Step 1: 씬 복제**

Project 창에서 `Assets/_Project/Scenes/IntroScene`을 선택 → **Ctrl+D** → 이름을 `StoryScene`으로 변경 → 더블클릭해서 연다. (Canvas·EventSystem·카메라 배경색이 그대로 복제된다.)

- [ ] **Step 2: 필요 없는 오브젝트 정리**

Hierarchy에서 삭제: `Canvas` 안의 `TitleText`, `ControlsText`, `StartButton`, 그리고 씬 루트의 `IntroSceneController`.

- [ ] **Step 3: 배경 2장 구성**

1. 남아 있는 `background`(소문자)의 이름을 `BackgroundFront`로 변경. **Source Image를 None으로** 비운다.
2. `BackgroundFront`를 **Ctrl+D**로 복제 → 이름 `BackgroundBack`.
3. Hierarchy에서 `BackgroundBack`을 `BackgroundFront` **위로** 드래그(먼저 그려져야 뒤에 깔린다).
4. 최종 순서(위→아래): `BackgroundBack`, `BackgroundFront`, `Dim`. `Dim`은 그대로 둔다(글자 가독성용).

- [ ] **Step 4: 대사창**

1. `Canvas` 우클릭 → **UI → Image** → 이름 `TextBar`. Source Image = `Assets/Art/UI/대화창`(**`Art/UI` 쪽** — `Art/캐릭터/…/대화창`은 옛 저해상도판).
2. Anchor Presets에서 **Alt+클릭으로 하단 중앙**. **Width 1400, Height 420, Pos Y 30.** (원본 1000:300 비율 유지)
3. `TextBar` 자식으로 아래를 만들고, 수치는 시작값이니 **Scene 뷰에서 그림의 창/탭/글자 영역에 맞게 눈으로 미세 조정**한다(호감도 대화창 때와 같은 방식):

| 오브젝트 | 종류 | 시작값(TextBar 중앙 기준) | 설정 |
|---|---|---|---|
| `MiniPortrait` | UI → Image | Pos (-498, -10), 157×175 | **Preserve Aspect 체크**, Source는 비움 |
| `NameplateRoot` | Create Empty(+RectTransform) | Pos (-244, 126), 311×56 | 이름표 탭 위치 |
| `NameText` | `NameplateRoot`의 자식, UI → Text | 부모를 꽉 채움(stretch) | Alignment 가운데, Font Size 24, Color `#F5E9C8` |
| `BodyText` | UI → Text | Pos (91, -14), 900×170 | Alignment 왼쪽 위, Font Size 34, Color 흰색, Horizontal Overflow **Wrap**, Vertical Overflow **Truncate** |

이나리 줄처럼 초상화가 없으면 `MiniPortrait`(이미지)만 숨겨지고 그림에 그려진 빈 창 테두리는 남는다 — 스펙상 허용되는 외관이다.

- [ ] **Step 5: 건너뛰기 버튼**

`Canvas` 우클릭 → **UI → Button (Legacy)** → 이름 `SkipButton`. Anchor **Alt+클릭으로 우상단**, Width 200 / Height 60 / Pos (-30, -30). Image Source = `Assets/Art/UI/대화 선택`, Color `#F2D966`. 자식 Text를 **"건너뛰기"**, Font Size 26, Color 검정으로.

- [ ] **Step 6: 컴포넌트 연결**

1. Hierarchy 루트에서 우클릭 → **Create Empty** → 이름 `StoryRoot` (Canvas의 자식이 아니라 씬 루트).
2. `StoryRoot`에 **Add Component → Story Panel**, **Add Component → Story Scene Controller**.
3. **Story Panel** 필드: Background Front = `BackgroundFront`, Background Back = `BackgroundBack`, Nameplate Root = `NameplateRoot`, Name Text = `NameText`, Mini Portrait = `MiniPortrait`, Body Text = `BodyText`.
4. **Story Scene Controller** 필드: Data = `Assets/_Project/Data/WorldIntroStory`, Panel = `StoryRoot`의 Story Panel(같은 오브젝트를 드래그), Skip Button = `SkipButton`, **Next Scene Name = `GameScene`**(기본값 그대로), Background Fade Seconds = 0.35.

- [ ] **Step 7: 빌드 세팅 등록과 저장**

**File → Build Settings…** → `StoryScene`이 열린 상태에서 **Add Open Scenes** → 목록에서 `IntroScene` 바로 아래로 드래그 → 창 닫기. **Ctrl+S**로 씬 저장.
(이전에 저장을 깜빡해서 변경이 통째로 안 들어간 적이 있다 — 저장 후 반드시 알려준다.)

- [ ] **Step 8: 에이전트 검증**

저장 후 에이전트가 확인한다:

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요\unity"
git status --short Assets/_Project/Scenes ProjectSettings
grep -n "StoryScene" ProjectSettings/EditorBuildSettings.asset
grep -n "nextSceneName\|skipButton\|backgroundFront\|backgroundBack\|nameplateRoot\|miniPortrait\|bodyText\|nameText\|data:\|panel:" Assets/_Project/Scenes/StoryScene.unity
```

Expected: `StoryScene.unity`(+`.meta`)와 `EditorBuildSettings.asset`이 변경/신규로 보이고, 빌드 세팅에 `StoryScene.unity`가 `IntroScene` 다음으로 있으며, 컨트롤러·패널 필드가 전부 `{fileID: 0}`이 아닌 값이고 `nextSceneName: GameScene`이다. 비어 있는 필드가 있으면 사용자에게 해당 필드를 알려준다.

---

### Task 5: `IntroSceneController` 연결 + 전체 흐름 확인 + PR 2

**Files:**
- Modify: `unity/Assets/_Project/Scripts/UI/IntroSceneController.cs` (협업자 파일 — 한 줄)

**Interfaces:**
- Consumes (Task 4): 빌드 세팅에 등록된 씬 이름 `StoryScene`

- [ ] **Step 1: 이동 대상 변경**

`OnStartClicked()`를 수정한다:

```csharp
        private void OnStartClicked()
        {
            SceneManager.LoadScene("StoryScene");
        }
```

(기존: `SceneManager.LoadScene("GameScene");`)

- [ ] **Step 2: 컴파일 + 회귀 확인**

"배치 테스트 실행 명령"을 실행한다. Expected: `error CS` 없음, `total="259" passed="259" failed="0"`. 끝나면 `rm -f TestResults.xml`.

- [ ] **Step 3: Play 모드 전체 흐름 확인 (사용자)**

`IntroScene`을 열고 Play. 다음을 확인한다:

- [ ] 시작 버튼 → `StoryScene`으로 이동(콘솔에 씬 로드 에러 없음)
- [ ] 첫 화면: 왕궁 배경 + 이나리 이름표 + 대사, **초상화 창은 비어 있음**
- [ ] 클릭 / Space / Enter로 한 줄씩 넘어감(총 11줄), 아델린·카마리온 줄엔 초상화가 나옴
- [ ] 마지막 줄 다음 클릭 → `GameScene`(캐릭터 선택)으로 이동
- [ ] 다시 Intro부터 시작해 **건너뛰기** 버튼 → 즉시 `GameScene`, 이때 건너뛰기를 누른 클릭이 다음 줄로도 넘어가지 않음(마지막 줄에서 눌러도 씬이 두 번 로드되지 않음)
- [ ] 결과 화면 "다시 하기" → `IntroScene` → 다시 `StoryScene`을 거침

- [ ] **Step 4: 커밋, 푸시, PR 2**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git status --short
git add unity/Assets/_Project/Scenes/StoryScene.unity unity/Assets/_Project/Scenes/StoryScene.unity.meta unity/ProjectSettings/EditorBuildSettings.asset unity/Assets/_Project/Scripts/UI/IntroSceneController.cs
git commit -m "feat: IntroScene 다음에 세계관 설명 장면(StoryScene) 추가

시작 버튼의 이동 대상을 GameScene에서 StoryScene으로 바꾸고, 대화가 끝나거나
건너뛰면 GameScene으로 넘어간다. StoryScene을 빌드 세팅에 등록했다.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
git push -u origin feature/story-scene-wiring
gh pr create --title "feat: 세계관 설명 단계 StoryScene (2/2)" --body "$(cat <<'EOF'
## 요약
`IntroScene`의 시작 버튼 뒤에 캐릭터 3인의 세계관 대화 장면을 추가합니다. 엔진 코드는 이전 PR(1/2)에 있습니다.

- 스펙: `docs/superpowers/specs/2026-09-21-story-intro-design.md`
- 계획: `docs/superpowers/plans/2026-09-21-story-intro.md`

## 협업자 확인 부탁
`IntroSceneController.OnStartClicked()`의 이동 대상을 `"GameScene"` → `"StoryScene"`으로 **한 줄 수정**했습니다(`IntroScene`을 만드신 쪽 파일).

## 변경 사항
- `WorldIntroStory.asset` — 대본 PDF 4페이지 11줄(표기는 스시왕국/롤왕국으로 통일, 이나리는 초상화 없이 이름표만) + 무결성 테스트 4개
- `StoryScene.unity` 신설, `EditorBuildSettings`에 등록
- 흐름: `IntroScene → StoryScene → GameScene`. 클릭/Space/Enter로 넘기고, 건너뛰기 버튼 제공

## 테스트
- EditMode 259/259 통과
- Play 모드로 Intro → Story → Game, 건너뛰기, 결과 화면 다시 하기 흐름 확인

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

**여기서 멈추고 사용자가 PR 2를 병합하길 기다린다.** 병합 후 `main` 동기화, 브랜치 정리, 메모리(`sushi-survival-project.md`) 업데이트, `docs/game-flow-roadmap.md`의 2번 항목 상태를 ✅로 갱신하는 작은 문서 PR을 이어서 한다.
