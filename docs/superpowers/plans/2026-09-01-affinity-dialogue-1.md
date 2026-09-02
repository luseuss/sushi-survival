# 호감도 대화 #1 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 캐릭터 선택 직후 2~3지 선택형 대화 한 세트를 넣고, 선택을 기존 증강
10종(`AugmentData`)에 매핑해 즉시 스탯 버프로 적용한다.

**Architecture:** 대화 데이터는 `AugmentData`를 가리키는 새 SO(`AffinityDialogueData`)로
캐릭터마다 하나씩 둔다. `GameManager.StartRun`을 스폰 단계와 전투 시작 단계
(`BeginCombat`)로 쪼개고, 그 사이에 새 `RunState.Intro`로 대화를 끼워 넣는다.
버프는 인게임 레벨업 증강 누적과 완전히 독립적으로 적용된다.

**Tech Stack:** Unity 2022.3.62f3 LTS, Built-in 2D, uGUI(Legacy), Unity Test
Framework(NUnit, EditMode)

**Spec:** `docs/superpowers/specs/2026-09-01-affinity-dialogue-1-design.md`

## Global Constraints

- git을 쓴다. `feature/affinity-dialogue-1` 브랜치에서 진행, 태스크마다 로컬 커밋,
  푸시는 전부 끝나고 사용자에게 물어본 뒤에 한다.
- Unity 프로젝트 루트는 `unity/` 서브폴더. 스크립트는
  `unity/Assets/_Project/Scripts/`, 테스트는 `unity/Assets/Tests/EditMode/`.
- **EditMode 테스트를 배치로 돌릴 때는 `-logFile -`(하이픈, stdout)을 쓴다.**
  ```
  & "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -projectPath "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요\unity" -runTests -testPlatform EditMode -testResults "$env:TEMP\dialogue-tests.xml" -logFile -
  ```
- 시작 시점 테스트 수는 **228개**다. 기존 테스트가 하나도 깨지면 안 된다.
- 네임스페이스: `SushiSurvival.Core` / `.Data` / `.Player` / `.UI`.
- 주석은 한국어로, "무엇"이 아니라 "왜"를 적는다.
- 수치는 하드코딩하지 않고 `[SerializeField]`로 노출한다.
- `Text`는 반드시 Legacy를 쓴다(TextMeshPro 미설치).
- **Unity Editor GUI 작업은 사용자가 직접 한다.** 에이전트는 코드와 배치
  테스트까지만 하고, 프리팹/씬 조립·인스펙터 연결·대본 데이터 입력은 Task 8로
  넘긴다.

---

## File Structure

**신규 (`unity/Assets/_Project/Scripts/`)**

| 파일 | 책임 |
|---|---|
| `Core/AffinityBuffLogic.cs` | 순수 — maxCap × 비율 계산 |
| `Data/AffinityDialogueData.cs` | 대화 질문·선택지 데이터 SO |
| `Core/AffinityBuffApplier.cs` | 선택된 증강을 `PlayerStats`/`PlayerHealth`에 적용 |
| `UI/AffinityChoiceButton.cs` | 선택지 버튼 하나 |
| `UI/AffinityDialoguePanel.cs` | 초상화·질문·선택지 3개를 보여주는 패널 |
| `Core/AffinityDialogueController.cs` | 대화 진입점 — `GameManager`가 부른다 |

**수정**

| 파일 | 변경 |
|---|---|
| `Data/CharacterData.cs` | `affinityDialogue` 필드 추가 |
| `Core/GameManager.cs` | `RunState.Intro` 추가, `StartRun`을 스폰/`BeginCombat`으로 분리 |

**변경 없음(확인만)**

`UI/RunTimerDisplay.cs`는 `CurrentState == RunState.Playing`으로만 타이머를
켠다. `Intro` 상태에서는 자동으로 꺼진 채라 손댈 필요가 없다.

**테스트 (`unity/Assets/Tests/EditMode/`)**

`AffinityBuffLogicTests.cs` 신규.

---

## Task 0: 작업 브랜치 준비

- [ ] **Step 1: 최신 main 확인 후 브랜치 생성**

```bash
git checkout main
git pull --ff-only
git checkout -b feature/affinity-dialogue-1
```

Expected: `Switched to a new branch 'feature/affinity-dialogue-1'`

---

## Task 1: 버프 수치 계산 — `AffinityBuffLogic`

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/AffinityBuffLogic.cs`
- Test: `unity/Assets/Tests/EditMode/AffinityBuffLogicTests.cs`

**Interfaces:**
- Consumes: 없음
- Produces: `SushiSurvival.Core.AffinityBuffLogic.GetBuffAmount(float maxCap, float ratio) → float`

- [ ] **Step 1: 실패하는 테스트를 작성한다**

`unity/Assets/Tests/EditMode/AffinityBuffLogicTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class AffinityBuffLogicTests
    {
        [Test]
        public void GetBuffAmount_ReturnsFractionOfCap()
        {
            Assert.AreEqual(0.25f, AffinityBuffLogic.GetBuffAmount(2f, 0.125f), 0.0001f);
        }

        [Test]
        public void GetBuffAmount_ZeroCap_ReturnsZero()
        {
            Assert.AreEqual(0f, AffinityBuffLogic.GetBuffAmount(0f, 0.125f), 0.0001f);
        }

        [Test]
        public void GetBuffAmount_NegativeCap_ClampsToZero()
        {
            // 데이터 입력 실수로 음수 maxCap이 들어와도 음수 버프를 주면 안 된다.
            Assert.AreEqual(0f, AffinityBuffLogic.GetBuffAmount(-5f, 0.125f), 0.0001f);
        }

        [Test]
        public void GetBuffAmount_RatioAboveOne_ClampsToOne()
        {
            Assert.AreEqual(2f, AffinityBuffLogic.GetBuffAmount(2f, 1.5f), 0.0001f);
        }

        [Test]
        public void GetBuffAmount_NegativeRatio_ClampsToZero()
        {
            Assert.AreEqual(0f, AffinityBuffLogic.GetBuffAmount(2f, -0.5f), 0.0001f);
        }

        [Test]
        public void GetBuffAmount_ZeroRatio_ReturnsZero()
        {
            Assert.AreEqual(0f, AffinityBuffLogic.GetBuffAmount(2f, 0f), 0.0001f);
        }
    }
}
```

- [ ] **Step 2: 테스트가 실패하는지 확인한다**

Global Constraints의 배치 명령으로 실행. 기대: 컴파일 에러 — `AffinityBuffLogic`을
찾을 수 없음.

- [ ] **Step 3: 최소 구현을 작성한다**

`unity/Assets/_Project/Scripts/Core/AffinityBuffLogic.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 호감도 대화 버프 = 증강의 누적 상한(maxCap) × 비율. 인게임 레벨업의
    /// 증강 누적(LevelSystem._accumulated)과는 완전히 별개로 친다 — 런
    /// 시작부터 작은 보너스를 받되, 이후 레벨업 선택지가 이 때문에 줄어들면
    /// 안 된다.
    /// </summary>
    public static class AffinityBuffLogic
    {
        public static float GetBuffAmount(float maxCap, float ratio)
            => Mathf.Max(0f, maxCap) * Mathf.Clamp01(ratio);
    }
}
```

- [ ] **Step 4: 테스트가 통과하는지 확인한다**

기대: 234개 통과 (기존 228 + 신규 6).

- [ ] **Step 5: 커밋**

```bash
git add unity/Assets/_Project/Scripts/Core/AffinityBuffLogic.cs unity/Assets/_Project/Scripts/Core/AffinityBuffLogic.cs.meta unity/Assets/Tests/EditMode/AffinityBuffLogicTests.cs unity/Assets/Tests/EditMode/AffinityBuffLogicTests.cs.meta
git commit -m "feat: AffinityBuffLogic으로 대화 버프 수치 계산"
```

---

## Task 2: 데이터 모델 — `AffinityDialogueData`

**Files:**
- Create: `unity/Assets/_Project/Scripts/Data/AffinityDialogueData.cs`
- Modify: `unity/Assets/_Project/Scripts/Data/CharacterData.cs`

**Interfaces:**
- Consumes: `SushiSurvival.Data.AugmentData` (기존)
- Produces:
  - `SushiSurvival.Data.AffinityDialogueChoice` — `{ string choiceText; AugmentData augment; }`
  - `SushiSurvival.Data.AffinityDialogueQuestion` — `{ string questionText; AffinityDialogueChoice[] choices; }`
  - `SushiSurvival.Data.AffinityDialogueData : ScriptableObject` — `{ AffinityDialogueQuestion question1; }`
  - `SushiSurvival.Data.CharacterData.affinityDialogue` 필드

- [ ] **Step 1: 데이터 클래스를 작성한다**

`unity/Assets/_Project/Scripts/Data/AffinityDialogueData.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Data
{
    /// <summary>선택지 하나. 서사용 대사와, 그 대사가 매핑되는 증강을 함께 든다.</summary>
    [System.Serializable]
    public class AffinityDialogueChoice
    {
        [TextArea]
        public string choiceText;
        [Tooltip("이 선택이 매핑되는 증강. 이름·아이콘·StatType·maxCap을 여기서 가져온다.")]
        public AugmentData augment;
    }

    /// <summary>질문 하나 + 선택지 2~3개.</summary>
    [System.Serializable]
    public class AffinityDialogueQuestion
    {
        [TextArea]
        public string questionText;
        [Tooltip("2~3개.")]
        public AffinityDialogueChoice[] choices;
    }

    /// <summary>캐릭터 하나가 가지는 호감도 대화. #1만 다룬다(#2는 별도 슬라이스).</summary>
    [CreateAssetMenu(menuName = "SushiSurvival/Affinity Dialogue Data", fileName = "NewAffinityDialogueData")]
    public class AffinityDialogueData : ScriptableObject
    {
        public AffinityDialogueQuestion question1;
    }
}
```

- [ ] **Step 2: `CharacterData`에 필드를 추가한다**

`unity/Assets/_Project/Scripts/Data/CharacterData.cs`의
`public RuntimeAnimatorController animatorController;` 아래에 추가한다:

```csharp
        public RuntimeAnimatorController animatorController;
        [Tooltip("호감도 대화 #1 데이터. 비워두면 대화 없이 바로 런이 시작된다.")]
        public AffinityDialogueData affinityDialogue;
```

- [ ] **Step 3: 컴파일이 통과하는지 확인한다**

기대: 234개 통과, 컴파일 에러 없음.

- [ ] **Step 4: 커밋**

```bash
git add unity/Assets/_Project/Scripts/Data/AffinityDialogueData.cs unity/Assets/_Project/Scripts/Data/AffinityDialogueData.cs.meta unity/Assets/_Project/Scripts/Data/CharacterData.cs
git commit -m "feat: AffinityDialogueData 데이터 모델과 CharacterData 연결 필드 추가"
```

---

## Task 3: 버프 적용 — `AffinityBuffApplier`

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/AffinityBuffApplier.cs`

**Interfaces:**
- Consumes:
  - `SushiSurvival.Data.AugmentData` (기존 — `statType`)
  - `SushiSurvival.Core.StatModifier` / `ModifierType` (기존)
  - `SushiSurvival.Player.PlayerStats.AddModifier(StatModifier)` (기존)
  - `SushiSurvival.Player.PlayerHealth.GrantMaxHealthIncrease(float)` (기존)
- Produces: `SushiSurvival.Core.AffinityBuffApplier.Apply(AugmentData augment, float amount, PlayerStats stats, PlayerHealth health)`

`AugmentOption.Apply()`(기존, `unity/Assets/_Project/Scripts/Core/AugmentOption.cs`)와
같은 패턴이지만 `Data.valuePerPick` 대신 매개변수로 받은 `amount`를 쓴다.

- [ ] **Step 1: 구현한다**

`unity/Assets/_Project/Scripts/Core/AffinityBuffApplier.cs`:

```csharp
using SushiSurvival.Data;
using SushiSurvival.Player;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 대화에서 고른 증강을 스탯에 적용한다. AugmentOption.Apply()와 같은
    /// 패턴(모디파이어 추가 + 최대체력이면 현재체력도 같이 올림)이지만,
    /// AugmentOption은 Data.valuePerPick을 읽는 구조라 다른 값(비율×maxCap)을
    /// 넣으려면 억지로 끼워 맞춰야 해서 별도로 둔다.
    /// </summary>
    public static class AffinityBuffApplier
    {
        public static void Apply(AugmentData augment, float amount, PlayerStats stats, PlayerHealth health)
        {
            stats.AddModifier(new StatModifier
            {
                Stat = augment.statType,
                Type = ModifierType.Additive,
                Value = amount
            });

            // 최대체력 버프는 현재 체력도 같이 올려야 체감이 된다.
            if (augment.statType == StatType.MaxHealth && health != null)
                health.GrantMaxHealthIncrease(amount);
        }
    }
}
```

- [ ] **Step 2: 컴파일이 통과하는지 확인한다**

기대: 234개 통과, 컴파일 에러 없음.

- [ ] **Step 3: 커밋**

```bash
git add unity/Assets/_Project/Scripts/Core/AffinityBuffApplier.cs unity/Assets/_Project/Scripts/Core/AffinityBuffApplier.cs.meta
git commit -m "feat: AffinityBuffApplier로 대화 선택 결과를 스탯에 적용"
```

---

## Task 4: 선택지 버튼 — `AffinityChoiceButton`

**Files:**
- Create: `unity/Assets/_Project/Scripts/UI/AffinityChoiceButton.cs`

**Interfaces:**
- Consumes: `SushiSurvival.Data.AffinityDialogueChoice` (Task 2)
- Produces:
  - `SushiSurvival.UI.AffinityChoiceButton.Bind(AffinityDialogueChoice choice, Action<AffinityDialogueChoice> onChosen)`
  - `SushiSurvival.UI.AffinityChoiceButton.Clear()`

`unity/Assets/_Project/Scripts/UI/LevelUpOptionButton.cs`(기존)와 같은 뼈대를
쓴다 — 표시할 텍스트가 증강 이름이 아니라 대사라서 그대로 재사용하지 못하고
새로 만든다.

- [ ] **Step 1: 구현한다**

`unity/Assets/_Project/Scripts/UI/AffinityChoiceButton.cs`:

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    [RequireComponent(typeof(Button))]
    public class AffinityChoiceButton : MonoBehaviour
    {
        [SerializeField] private Text choiceText;
        [Tooltip("이 선택이 매핑된 증강 아이콘. 비워두면 표시하지 않는다.")]
        [SerializeField] private Image iconImage;

        private Button _button;
        private AffinityDialogueChoice _choice;
        private Action<AffinityDialogueChoice> _onChosen;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);
        }

        public void Bind(AffinityDialogueChoice choice, Action<AffinityDialogueChoice> onChosen)
        {
            _choice = choice;
            _onChosen = onChosen;

            gameObject.SetActive(true);

            if (choiceText != null)
                choiceText.text = choice.choiceText;

            if (iconImage != null)
            {
                iconImage.sprite = choice.augment != null ? choice.augment.icon : null;
                iconImage.enabled = iconImage.sprite != null;
            }
        }

        public void Clear()
        {
            _choice = null;
            _onChosen = null;
            gameObject.SetActive(false);
        }

        private void HandleClick()
        {
            if (_choice == null) return;

            _onChosen?.Invoke(_choice);
        }
    }
}
```

- [ ] **Step 2: 컴파일이 통과하는지 확인한다**

기대: 234개 통과, 컴파일 에러 없음.

- [ ] **Step 3: 커밋**

```bash
git add unity/Assets/_Project/Scripts/UI/AffinityChoiceButton.cs unity/Assets/_Project/Scripts/UI/AffinityChoiceButton.cs.meta
git commit -m "feat: AffinityChoiceButton으로 대화 선택지 버튼 구현"
```

---

## Task 5: 대화 패널 — `AffinityDialoguePanel`

**Files:**
- Create: `unity/Assets/_Project/Scripts/UI/AffinityDialoguePanel.cs`

**Interfaces:**
- Consumes:
  - `SushiSurvival.Data.AffinityDialogueQuestion` / `AffinityDialogueChoice` (Task 2)
  - `SushiSurvival.UI.AffinityChoiceButton.Bind(...)` / `.Clear()` (Task 4)
- Produces: `SushiSurvival.UI.AffinityDialoguePanel.Show(Sprite portrait, AffinityDialogueQuestion question, Action<AffinityDialogueChoice> onChosen)`, `.Hide()`

`unity/Assets/_Project/Scripts/UI/LevelUpPanel.cs`(기존)와 같은 뼈대 —
`Root` 프로퍼티, `Awake`에서 `Hide()`.

- [ ] **Step 1: 구현한다**

`unity/Assets/_Project/Scripts/UI/AffinityDialoguePanel.cs`:

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    public class AffinityDialoguePanel : MonoBehaviour
    {
        [Tooltip("패널 루트. 비워두면 이 오브젝트 자신을 켜고 끈다.")]
        [SerializeField] private GameObject root;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text questionText;
        [Tooltip("선택지 버튼 최대 3개.")]
        [SerializeField] private AffinityChoiceButton[] choiceButtons;

        private GameObject Root => root != null ? root : gameObject;

        private void Awake() => Hide();

        public void Show(Sprite portrait, AffinityDialogueQuestion question, Action<AffinityDialogueChoice> onChosen)
        {
            Root.SetActive(true);

            if (portraitImage != null)
                portraitImage.sprite = portrait;

            if (questionText != null)
                questionText.text = question.questionText;

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (i < question.choices.Length)
                    choiceButtons[i].Bind(question.choices[i], onChosen);
                else
                    choiceButtons[i].Clear();
            }
        }

        public void Hide() => Root.SetActive(false);
    }
}
```

- [ ] **Step 2: 컴파일이 통과하는지 확인한다**

기대: 234개 통과, 컴파일 에러 없음.

- [ ] **Step 3: 커밋**

```bash
git add unity/Assets/_Project/Scripts/UI/AffinityDialoguePanel.cs unity/Assets/_Project/Scripts/UI/AffinityDialoguePanel.cs.meta
git commit -m "feat: AffinityDialoguePanel로 대화 UI 구현"
```

---

## Task 6: 진입점 — `AffinityDialogueController`

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/AffinityDialogueController.cs`

**Interfaces:**
- Consumes:
  - `SushiSurvival.Core.AffinityBuffLogic.GetBuffAmount(float, float)` (Task 1)
  - `SushiSurvival.Core.AffinityBuffApplier.Apply(...)` (Task 3)
  - `SushiSurvival.UI.AffinityDialoguePanel.Show(...)` / `.Hide()` (Task 5)
  - `SushiSurvival.Data.AffinityDialogueData` (Task 2)
- Produces: `SushiSurvival.Core.AffinityDialogueController.Show(AffinityDialogueData data, Sprite portrait, PlayerStats stats, PlayerHealth health, Action onComplete)`

- [ ] **Step 1: 구현한다**

`unity/Assets/_Project/Scripts/Core/AffinityDialogueController.cs`:

```csharp
using System;
using UnityEngine;
using SushiSurvival.Data;
using SushiSurvival.Player;
using SushiSurvival.UI;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 호감도 대화 #1의 진입점. GameManager가 캐릭터 스폰 직후 이걸 부른다.
    /// 대화 데이터가 없거나 비어 있으면 즉시 onComplete를 불러 건너뛴다 —
    /// 아직 대본이 없는 캐릭터도 런이 정상적으로 진행돼야 한다.
    /// </summary>
    public class AffinityDialogueController : MonoBehaviour
    {
        [SerializeField] private AffinityDialoguePanel panel;
        [Tooltip("버프 = 증강 maxCap × 이 비율. 기획서 권장 10~15%의 중간값.")]
        [Range(0f, 1f)]
        [SerializeField] private float buffRatio = 0.125f;

        public void Show(AffinityDialogueData data, Sprite portrait,
                         PlayerStats stats, PlayerHealth health, Action onComplete)
        {
            if (data == null || data.question1 == null ||
                data.question1.choices == null || data.question1.choices.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            if (panel == null)
            {
                Debug.LogError($"{name}: panel이 비어 있어 대화를 표시할 수 없습니다.");
                onComplete?.Invoke();
                return;
            }

            panel.Show(portrait, data.question1, choice =>
            {
                if (choice.augment != null)
                {
                    float amount = AffinityBuffLogic.GetBuffAmount(choice.augment.maxCap, buffRatio);
                    AffinityBuffApplier.Apply(choice.augment, amount, stats, health);
                }
                else
                {
                    Debug.LogWarning($"{name}: 선택지 '{choice.choiceText}'에 augment가 연결되지 않아 버프 없이 넘어갑니다.");
                }

                panel.Hide();
                onComplete?.Invoke();
            });
        }
    }
}
```

- [ ] **Step 2: 컴파일이 통과하는지 확인한다**

기대: 234개 통과, 컴파일 에러 없음.

- [ ] **Step 3: 커밋**

```bash
git add unity/Assets/_Project/Scripts/Core/AffinityDialogueController.cs unity/Assets/_Project/Scripts/Core/AffinityDialogueController.cs.meta
git commit -m "feat: AffinityDialogueController로 대화 진입점 구현"
```

---

## Task 7: `GameManager` — `RunState.Intro`와 `StartRun` 분리

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Core/GameManager.cs`

**Interfaces:**
- Consumes: `SushiSurvival.Core.AffinityDialogueController.Show(...)` (Task 6),
  `SushiSurvival.Data.CharacterData.affinityDialogue` (Task 2)

- [ ] **Step 1: `RunState`에 `Intro`를 추가한다**

기존:
```csharp
    public enum RunState
    {
        CharacterSelect,
        Playing,
        Result
    }
```

교체 후:
```csharp
    public enum RunState
    {
        CharacterSelect,
        /// <summary>플레이어는 스폰됐지만 대화 #1이 끝날 때까지 전투가 시작되지 않은 상태.</summary>
        Intro,
        Playing,
        Result
    }
```

**`RunState`를 참조하는 다른 곳은 손댈 필요가 없다** — `RunTimerDisplay.cs`가
`CurrentState == RunState.Playing`으로만 타이머를 켜고 있어서, `Intro`
상태에서는 자동으로 꺼진 채다. `AddExperience`·`RegisterKill`·
`WaveDirector.Update`도 전부 `!= RunState.Playing`으로 가드돼 있어 `Intro`
동안 저절로 잠자고 있는다.

- [ ] **Step 2: 필드를 추가한다**

`bossDirector` 필드 아래에 추가:

```csharp
        [SerializeField] private SushiSurvival.Enemies.Boss.BossDirector bossDirector;
        [Tooltip("호감도 대화 #1을 보여준다. 비워두면 대화 없이 바로 시작한다.")]
        [SerializeField] private AffinityDialogueController affinityDialogueController;
```

`_playerHealth`/`_playerStats` 필드 아래에 추가:

```csharp
        private PlayerHealth _playerHealth;
        private PlayerStats _playerStats;
        private Transform _playerTransform;
        private string _activeCharacterName;
```

- [ ] **Step 3: `StartRun`을 스폰 단계로 줄이고 `BeginCombat`을 새로 만든다**

기존 `StartRun` 전체를 아래로 교체한다:

```csharp
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
            _playerTransform = player.transform;
            _activeCharacterName = characterData.characterName;

            var weapon = player.GetComponent<WeaponBase>();
            levelSystem.SetPlayer(_playerStats, _playerHealth, weapon);

            cameraFollow.SetTarget(_playerTransform);

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

        /// <summary>
        /// 대화 #1이 끝난 뒤(또는 대화가 없으면 스폰 직후 곧바로) 실제 전투를 연다.
        /// </summary>
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
```

- [ ] **Step 4: 컴파일이 통과하는지 확인한다**

기대: 234개 통과, 컴파일 에러 없음. `StartRun(CharacterData)`의 시그니처는
그대로라 `CharacterSelectButton.OnClicked`의 호출부는 전혀 안 바뀐다.

- [ ] **Step 5: 커밋**

```bash
git add unity/Assets/_Project/Scripts/Core/GameManager.cs
git commit -m "feat: GameManager에 RunState.Intro 추가하고 StartRun을 스폰/전투로 분리"
```

---

## Task 8: Unity Editor 작업 (사용자가 직접)

에이전트는 여기까지 코드를 완성했다. 아래는 GUI·콘텐츠 입력 작업이라
사용자가 직접 한다.

### 8-1. `AffinityDialogueData` 에셋 2개 생성

Project 창 `Assets/_Project/Data/`에서 우클릭 →
**Create → SushiSurvival → Affinity Dialogue Data**. 이름은
`EggAffinityDialogue`, `ShrimpAffinityDialogue`로.

각 에셋의 `Question1` 필드에 아래 초안을 입력한다(스펙 문서
`docs/superpowers/specs/2026-09-01-affinity-dialogue-1-design.md`의
"대본 초안" 섹션과 동일 — 직접 다듬어도 된다).

**`EggAffinityDialogue`**

| 필드 | 값 |
|---|---|
| Question Text | 몰려오는 적들 앞에서, 넌 어떻게 싸울 거야? |
| Choices[0] Choice Text | 제 양산이 닿는 곳은 전부 쓸어버리겠어요. |
| Choices[0] Augment | `Aug_AttackDamage` |
| Choices[1] Choice Text | 무너지지 않는 게 먼저예요. 버틸 수 있어야 이길 수 있으니까요. |
| Choices[1] Augment | `Aug_Armor` |
| Choices[2] Choice Text | 발이 빠르면, 애초에 다칠 일이 없죠. |
| Choices[2] Augment | `Aug_MoveSpeed` |

**`ShrimpAffinityDialogue`**

| 필드 | 값 |
|---|---|
| Question Text | 전장에 나서기 전에 확인한다. 네 방식은 뭐지? |
| Choices[0] Choice Text | 한 방에 끝낸다. |
| Choices[0] Augment | `Aug_AttackDamage` |
| Choices[1] Choice Text | 먼저 버티는 쪽이 이긴다. |
| Choices[1] Augment | `Aug_Armor` |
| Choices[2] Choice Text | 맞기 전에 피한다. |
| Choices[2] Augment | `Aug_MoveSpeed` |

`Choices` 배열 크기를 먼저 3으로 지정한 뒤 각 항목을 채운다. 증강 에셋은
`Assets/_Project/Data/Augments/`에 있다.

### 8-2. `CharacterData`에 대화 연결

`Assets/_Project/Data/EggCharacterData.asset`을 선택 → **Affinity Dialogue**
필드에 `EggAffinityDialogue` 연결.

`Assets/_Project/Data/ShrimpCharacterData.asset`을 선택 → **Affinity Dialogue**
필드에 `ShrimpAffinityDialogue` 연결.

### 8-3. `AffinityDialoguePanel` UI 조립

기존 `Canvas` 아래에 만든다. `LevelUpPanel`의 구조를 참고하면 된다(같은
Canvas에 있을 것이다).

```
AffinityDialoguePanel        ← AffinityDialoguePanel 스크립트
├─ Background                ← Image (반투명 검정 등)
├─ Portrait                  ← Image (캐릭터 초상화)
├─ QuestionText               ← Text (Legacy)
└─ Choices
   ├─ Choice_0                ← AffinityChoiceButton (Button + Image + Text 자식)
   ├─ Choice_1
   └─ Choice_2
```

**`Text`는 반드시 Legacy를 쓴다.**

`AffinityDialoguePanel` 필드 연결:

| 필드 | 연결 대상 |
|---|---|
| Root | 자기 자신 또는 별도 루트(비워두면 자기 자신) |
| Portrait Image | `Portrait`의 Image |
| Question Text | `QuestionText`의 Text |
| Choice Buttons | `Choice_0`, `Choice_1`, `Choice_2` 순서대로 3개 |

각 `Choice_N`의 `AffinityChoiceButton` 필드:

| 필드 | 연결 대상 |
|---|---|
| Choice Text | 그 버튼 자식의 Text(Legacy) |
| Icon Image | 그 버튼 자식의 Image(아이콘용, 선택) |

씬 저장 전에 **`AffinityDialoguePanel`을 비활성 상태로 두지 않아도 된다** —
`Awake()`가 스스로 `Hide()`를 부른다(기존 `LevelUpPanel`과 동일한 패턴).

### 8-4. `AffinityDialogueController` 배치

Hierarchy 빈 공간에 `AffinityDialogueController` 생성 →
**Add Component → Affinity Dialogue Controller**.

| 필드 | 값 |
|---|---|
| Panel | `AffinityDialoguePanel` |
| Buff Ratio | 0.125 (기본값 그대로) |

### 8-5. `GameManager` 연결

씬의 `GameManager` 선택 → **Affinity Dialogue Controller** 필드에 방금 만든
오브젝트 연결.

---

## Task 9: 플레이테스트

- [ ] **대화 있는 캐릭터** — 계란·간장새우 선택 시 캐릭터 선택 패널이 사라지고
  대화 패널이 뜬다. 초상화·질문·선택지 3개가 올바르게 보인다
- [ ] **선택 적용** — 선택지를 고르면 패널이 닫히고 몹 스폰과 타이머가
  시작된다(화면 상단 타이머가 그때부터 움직인다)
- [ ] **버프 확인** — 공격력을 골랐으면 무기 데미지가, 방어력을 골랐으면
  피격 데미지 감소가, 이동속도를 골랐으면 이동이 체감상 더 빠르다
- [ ] **레벨업과 안 겹침** — 대화 버프를 받은 뒤에도 같은 카테고리(예: 공격력)
  레벨업 선택지가 이전처럼 계속 나온다(더 적게 나오지 않는다)
- [ ] **대화 중 입력 차단** — 대화 패널이 떠 있는 동안 몹이 안 나오고 캐릭터
  선택 버튼도 다시 눌리지 않는다
- [ ] **대화 없는 캐릭터** — `affinityDialogue`를 비워둔 캐릭터(테스트용으로
  하나 임시로 비워보고 확인)는 대화 없이 바로 몹이 스폰된다
- [ ] **재시작** — 결과 화면에서 다시 하기 → 캐릭터 선택 → 대화가 다시
  정상적으로 뜬다(두 번째 판에서도 동작)
- [ ] **회귀** — 무기 공격, 레벨업 팝업, 보스전, 게임오버가 이전과 동일하게
  동작한다

전부 통과하면 `main`으로 병합할지 사용자에게 확인한다.

---

## Self-Review 기록

**스펙 커버리지** — 데이터 모델(Task 2), 버프 계산·적용(Task 1, 3), UI(Task
4, 5, 6), 런 상태 분리(Task 7), 대본 초안 입력(Task 8-1), 독립 보너스 원칙
(Task 1 주석 + Task 9 체크리스트), 재시작 시 자동 초기화(스펙에 명시, 씬
리로드라 별도 태스크 불필요) 전부 대응됨.

**타입 일관성** — `AffinityDialogueChoice`/`AffinityDialogueQuestion`/
`AffinityDialogueData`가 Task 2에서 정의되고 Task 4·5·6에서 동일한 필드명으로
쓰인다. `AffinityBuffLogic.GetBuffAmount`/`AffinityBuffApplier.Apply`의
시그니처가 정의(Task 1, 3)와 호출부(Task 6)에서 일치한다.
`AffinityDialogueController.Show(...)`의 매개변수 순서가 Task 6 정의와
Task 7의 `GameManager` 호출부에서 동일하다.

**플레이스홀더 스캔** — 없음. 대본은 "초안" 라벨을 달았지만 실제 완성된
문장이며, Task 8-1에 그대로 입력 가능한 형태로 제공된다.

**의도적 순서** — Task 7(`GameManager`)이 Task 2(`CharacterData.affinityDialogue`)와
Task 6(`AffinityDialogueController`) 둘 다에 의존해 마지막 코드 태스크로
배치했다.
