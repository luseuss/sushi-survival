# 캐릭터 탐색→선택 흐름 + 이나리 선택 제한 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 아델린·카마리온 클릭 시 여러 줄짜리 자기소개 대화 뒤 증강 3택으로 이어지게 하고, 이나리 클릭 시 선택 제한 안내(뒤로가기/계속 진행하기)로 이어지게 하며, 보스전 직전 대화를 증강 선택 없는 순수 나레이션으로 교체한다.

**Architecture:** `AffinityDialogueData`에 나레이션 줄 배열(`introLines`/`bossIntroLines`)을 추가하고, `AffinityDialogueController`가 2번 작업(`StoryScene`)에서 만든 `StoryPanel`/`StoryDialogueLogic`을 재사용해 그 줄들을 재생한 뒤 기존 증강 3택으로 넘어간다. 이나리 전용으로 새 `LockedCharacterController`를 만들어 같은 `StoryPanel`을 재사용하되 씬 전환·플레이어 스폰 없이 캐릭터 선택 화면 안에서만 오가게 한다.

**Tech Stack:** Unity 2022.3.62f3, Built-in 2D, 새 Input System(`Keyboard.current`/`Mouse.current`), uGUI **Legacy** `Text`/`Button`(TextMeshPro 미설치), NUnit EditMode 테스트.

**Spec:** `docs/superpowers/specs/2026-09-22-character-select-flow-design.md`

## Global Constraints

- 스펙 결정: 카드 클릭 = 즉시 확정(A안, 스폰 시점 안 바뀜), 보스전 직전 대화는 증강 선택 없는 순수 나레이션, 이나리 카드는 회색 유지하되 클릭 가능, 이나리 제한 문구는 **"이나리는 특정 캐릭터 디자인 이슈로 선택이 제한됩니다."**
- 협업 규칙(`docs/COLLABORATION.md`): `main` 직접 커밋·푸시·머지 금지(브랜치 + PR, 머지 버튼은 사람만). `git add .` 금지 — 파일을 명시하고 `.meta`를 항상 함께 넣는다. `GameScene.unity`는 협업자(트랙 A) 단독 소유 씬 — 이번 작업이 건드리므로 PR D·E·F에서 명시한다.
- 판정 로직은 `XxxLogic` 정적 클래스 + EditMode 테스트로 분리한다. 이번 작업은 새 판정 로직이 없다 — 2번 작업의 `StoryDialogueLogic`을 그대로 재사용한다(새 테스트 불필요).
- Legacy Text/Button만 사용(TextMeshPro 미설치).
- **현재 `main` 기준 EditMode 테스트 263/263 통과가 baseline이다.** 아래 각 태스크의 기대 개수는 이 baseline에 더해진 값이다. 실행 시점에 협업자 커밋이 더 들어와 있으면, 기대 개수 대신 "회귀 없음(실패 0, 이전 대비 통과 개수 그대로 유지 또는 이번 태스크가 추가한 만큼만 증가)"으로 판단한다.

### 배치 테스트 실행 명령 (이 문서에서 "테스트 실행"이라 부르는 것)

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요\unity"
"C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" -batchmode -projectPath "$(pwd -W)" -runTests -testPlatform EditMode -testResults "$(pwd -W)/TestResults.xml" -logFile - > "$TEMP/unity_test.log" 2>&1
grep -E "error CS" "$TEMP/unity_test.log" | head -20
grep -c "HandleProjectAlreadyOpenInAnotherInstance" "$TEMP/unity_test.log"
head -c 300 TestResults.xml
```

성공 기준: `error CS` 없음, `HandleProjectAlreadyOpenInAnotherInstance` 카운트 0(에디터가 열려있으면 이게 찍히고 결과 파일이 안 생긴다 — 그러면 에디터를 닫아달라고 요청), `result="Passed"`, `failed="0"`. 끝나면 `rm -f TestResults.xml`.

### 이미 존재하는(이번 계획이 그대로 쓰는) 타입

- `SushiSurvival.Data.StoryLine { string speakerName; Sprite portrait; string text; Sprite background; }` (`Data/StoryDialogueData.cs`)
- `SushiSurvival.Core.StoryDialogueLogic.NextIndex(int, int) / IsFinished(int, int)` (`Core/StoryDialogueLogic.cs`)
- `SushiSurvival.UI.StoryPanel.ShowLine(StoryLine)` — `nameplateRoot`가 있으면 `speakerName`이 비었을 때 숨기고, `miniPortrait`가 있으면 `portrait`가 `null`일 때 숨긴다(2번 작업에서 이미 구현·검증됨)

---

## 브랜치 / PR 분할

| PR | 브랜치 | 태스크 | 시작 시점 | 의존 |
|---|---|---|---|---|
| **A** | `feature/affinity-data-schema` | Task 1 (`AffinityDialogueData`/`CharacterData` 스키마) | `main`에서 지금 | 없음 |
| **B** | `feature/affinity-dialogue-controller` | Task 2 (`AffinityDialogueController`) | **A 병합 후** | A의 `introLines`/`bossIntroLines`, 기존 `StoryPanel`/`StoryDialogueLogic` |
| **C** | `feature/locked-character-controller` | Task 3 (`LockedCharacterController` 신규) | **A 병합 후** (B와 병렬) | A의 `CharacterData.lockedMessage`, 기존 `StoryPanel` |
| **D** | `feature/character-select-button-locked` | Task 4 (`CharacterSelectButton`) | **C 병합 후** | C의 `LockedCharacterController` |
| **E** | `feature/inari-character-data` | Task 5 (`InariCharacterData.asset` 생성) | **A 병합 후** (B·C·D와 병렬) | A의 `lockedMessage` |
| **F** | `feature/character-select-flow-wiring` | Task 6 (씬/데이터 에셋 배선, 사용자 에디터 작업) | **B·D·E 모두 병합 후** | B·C·D·E 전부 |

B, C, E는 서로 파일이 안 겹쳐 A만 병합되면 동시에 진행할 수 있다. D는 C의 `LockedCharacterController` 타입이 있어야 컴파일되므로 C를 기다린다(B·E는 안 기다려도 됨).

---

## File Structure

| 파일 | 역할 |
|---|---|
| `unity/Assets/_Project/Scripts/Data/AffinityDialogueData.cs` (수정) | `introLines`/`bossIntroLines` 추가, `question2` 제거 |
| `unity/Assets/_Project/Scripts/Data/CharacterData.cs` (수정) | `lockedMessage` 추가 |
| `unity/Assets/_Project/Scripts/Core/AffinityDialogueController.cs` (수정) | `introLines`→`question1`, `bossIntroLines` 재생으로 전면 교체 |
| `unity/Assets/_Project/Scripts/Core/LockedCharacterController.cs` (신규) | 이나리 클릭 시 안내/뒤로가기/계속하기 |
| `unity/Assets/_Project/Scripts/UI/CharacterSelectButton.cs` (수정) | `locked`여도 클릭 가능, `LockedCharacterController` 호출 |
| `unity/Assets/_Project/Data/InariCharacterData.asset` (신규) | 이나리용 최소 `CharacterData`(이름 + 제한 문구 + 카드 아트) |
| `unity/Assets/Tests/EditMode/InariCharacterDataAssetTests.cs` (신규) | 위 에셋 무결성 테스트 |
| `unity/Assets/_Project/Data/EggAffinityDialogue.asset`, `ShrimpAffinityDialogue.asset` (수정, PR F) | `introLines`/`bossIntroLines` 채움 — 사용자가 에디터에서 입력 |
| `unity/Assets/_Project/Scenes/GameScene.unity` (수정, PR F) | 새 `StoryPanel` 2개(도입부용, 이나리용), 버튼 배선, `Button_Inari.characterData` 연결 |

---

# PR A — 데이터 스키마 (`main`에서 지금 시작)

### Task 1: `AffinityDialogueData` + `CharacterData` 스키마 변경

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git checkout -b feature/affinity-data-schema
```

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Data/AffinityDialogueData.cs`
- Modify: `unity/Assets/_Project/Scripts/Data/CharacterData.cs`

**Interfaces:**
- Consumes: `SushiSurvival.Data.StoryLine`(기존 타입, `Data/StoryDialogueData.cs`)
- Produces (Task 2·3·5·6이 사용):
  - `AffinityDialogueData.introLines`(`StoryLine[]`) — `question1` 앞에 재생
  - `AffinityDialogueData.question1`(`AffinityDialogueQuestion`, 타입·필드 이름 불변)
  - `AffinityDialogueData.bossIntroLines`(`StoryLine[]`) — 기존 `question2` 자리를 대체
  - `CharacterData.lockedMessage`(`string`)

ScriptableObject/데이터 클래스라 기존 관례대로 EditMode 테스트 대상이 아니다. 검증은 컴파일 + 회귀 없음으로 한다.

- [ ] **Step 1: `AffinityDialogueData.cs` 수정**

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

    /// <summary>
    /// 캐릭터 하나가 가지는 호감도 대화.
    /// introLines → question1(증강 3택)은 런 시작 직전에, bossIntroLines는
    /// 5:00 보스전 진입 직전에 쓴다. 각각 비어 있으면 그 단계를 건너뛴다.
    /// </summary>
    [CreateAssetMenu(menuName = "SushiSurvival/Affinity Dialogue Data", fileName = "NewAffinityDialogueData")]
    public class AffinityDialogueData : ScriptableObject
    {
        [Tooltip("question1 앞에 순서대로 재생되는 자기소개 나레이션. 비우면 곧바로 question1이 뜬다.")]
        public StoryLine[] introLines;
        public AffinityDialogueQuestion question1;
        [Tooltip("보스전 진입 직전 재생되는 나레이션(선택지 없음). 비우면 인터럽트 없이 바로 보스전.")]
        public StoryLine[] bossIntroLines;
    }
}
```

(기존 `question2` 필드는 제거됐다. `EggAffinityDialogue.asset`/`ShrimpAffinityDialogue.asset`에
남아있는 옛 `question2` 데이터는 이 필드가 사라지면서 다음 저장 때 조용히 사라진다 — PR F에서
`bossIntroLines`로 새로 채운다.)

- [ ] **Step 2: `CharacterData.cs` 수정**

`unity/Assets/_Project/Scripts/Data/CharacterData.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Data
{
    [CreateAssetMenu(menuName = "SushiSurvival/Character Data", fileName = "NewCharacterData")]
    public class CharacterData : ScriptableObject
    {
        public string characterName;
        public Sprite portraitSprite;
        [Tooltip("캐릭터 선택 화면 버튼 전용 카드 아트. 비워두면 portraitSprite로 대신 표시한다.")]
        public Sprite selectCardSprite;
        [Tooltip("이 캐릭터로 플레이할 때 생성할 프리팹. 캐릭터마다 무기·애니메이터가 다르므로 종류별로 따로 만든다.")]
        public GameObject playerPrefab;
        public float baseMoveSpeed = 3f;
        public float baseMaxHealth = 100f;
        public WeaponData weaponData;
        public RuntimeAnimatorController animatorController;
        [Tooltip("호감도 대화 #1(런 시작 직전)·#2(보스전 진입 직전) 데이터. introLines/question1이 " +
                 "비어 있으면 대화 없이 바로 런이 시작되고, bossIntroLines가 비어 있으면 " +
                 "인터럽트 없이 바로 보스전으로 넘어간다.")]
        public AffinityDialogueData affinityDialogue;
        [Tooltip("locked 캐릭터를 클릭했을 때 보여줄 안내 문구. locked가 아니면 안 쓴다.")]
        public string lockedMessage;
    }
}
```

- [ ] **Step 3: 컴파일 + 회귀 확인**

"배치 테스트 실행 명령"을 실행한다.
Expected: `error CS` 없음, `total="263" passed="263" failed="0"`(스키마만 바뀌고 새 테스트는 없음). 끝나면 `rm -f TestResults.xml`.

- [ ] **Step 4: 커밋, 푸시, PR A**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/Data/AffinityDialogueData.cs unity/Assets/_Project/Scripts/Data/CharacterData.cs
git commit -m "feat: 캐릭터 선택 흐름용 대화 스키마 확장(introLines/bossIntroLines/lockedMessage)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
git push -u origin feature/affinity-data-schema
gh pr create --title "feat: 대화 스키마 확장 (캐릭터 탐색-선택 흐름 A)" --body "$(cat <<'EOF'
## 요약
캐릭터 탐색→선택 흐름의 데이터 스키마. `AffinityDialogueData`에 `introLines`/`bossIntroLines`(나레이션 줄 배열) 추가, `question2`는 제거하고 `bossIntroLines`로 대체. `CharacterData`에 `lockedMessage` 추가. 아직 이 필드들을 읽는 코드가 없어 게임 동작은 바뀌지 않습니다.

- 스펙: `docs/superpowers/specs/2026-09-22-character-select-flow-design.md`
- 계획: `docs/superpowers/plans/2026-09-22-character-select-flow.md` (PR A)

## 참고
기존 `EggAffinityDialogue.asset`/`ShrimpAffinityDialogue.asset`의 `question2` 데이터는 필드 제거로 다음 저장 시 사라집니다 — PR F에서 `bossIntroLines`로 새로 채웁니다.

## 테스트
- EditMode 263/263 통과 (컴파일 확인, 신규 테스트 없음 — 데이터 스키마만 변경)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

**A를 올린 뒤 멈추지 않고 곧바로 Task 2(B), Task 3(C), Task 5(E)를 각각 `main`에서 새로 판다.** 병합 버튼은 사람만 누른다.

---

# PR B — `AffinityDialogueController` (A 병합 후)

### Task 2: `AffinityDialogueController`를 나레이션 재생 지원으로 교체

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git log --oneline -3   # A 병합 커밋이 보여야 한다
git checkout -b feature/affinity-dialogue-controller
```

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Core/AffinityDialogueController.cs`

**Interfaces:**
- Consumes (A): `AffinityDialogueData.introLines/question1/bossIntroLines`
- Consumes (기존): `SushiSurvival.UI.StoryPanel.ShowLine(StoryLine)`, `SushiSurvival.Core.StoryDialogueLogic.NextIndex(int,int)/IsFinished(int,int)`
- Produces: `AffinityDialogueController` 직렬화 필드에 `introPanel`(`StoryPanel`)이 새로 추가됨(기존 `panel`은 그대로 유지) — PR F가 씬에서 연결

MonoBehaviour라 기존 관례대로 EditMode 테스트 대상이 아니다. 검증은 컴파일 + 회귀 없음으로 한다.

- [ ] **Step 1: 컨트롤러 전면 교체**

`unity/Assets/_Project/Scripts/Core/AffinityDialogueController.cs`:

```csharp
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using SushiSurvival.Data;
using SushiSurvival.Player;
using SushiSurvival.UI;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 호감도 대화 #1(런 시작 직전)·#2(5:00 보스전 진입 직전)의 진입점.
    /// #1은 introLines(자기소개 나레이션) → question1(증강 3택) 순으로,
    /// #2는 bossIntroLines(나레이션만)만 재생한다. 대화 데이터가 없거나
    /// 줄이 비어 있으면 즉시 다음 단계로 건너뛴다 — 아직 대본이 없는
    /// 캐릭터/시점도 런이 정상적으로 진행돼야 한다.
    /// </summary>
    public class AffinityDialogueController : MonoBehaviour
    {
        [SerializeField] private AffinityDialoguePanel panel;
        [Tooltip("introLines/bossIntroLines를 재생하는 나레이션 패널(StoryScene에서 만든 것과 같은 컴포넌트).")]
        [SerializeField] private StoryPanel introPanel;
        [Tooltip("버프 = 증강 maxCap × 이 비율. 기획서 권장 10~15%의 중간값.")]
        [Range(0f, 1f)]
        [SerializeField] private float buffRatio = 0.125f;

        private Coroutine _lineRoutine;

        /// <param name="recordBuff">
        /// 적용된 증강·수치를 호출자(LevelSystem)에 되돌려준다. 보스 씬으로 넘어갈 때
        /// 이 버프를 다시 적용할 수 있도록 LevelSystem.RecordExternalBuff를 넘겨받는다 —
        /// 안 넘기면 GameScene→BossScene 전환 시 대화로 받은 버프가 사라진다.
        /// </param>
        public void Show(AffinityDialogueData data, Sprite portrait, PlayerStats stats, PlayerHealth health,
                         Action<AugmentData, float> recordBuff, Action onComplete)
        {
            PlayLines(data?.introLines,
                () => ShowQuestion(data?.question1, portrait, stats, health, recordBuff, onComplete));
        }

        /// <summary>보스전 직전 나레이션만 재생한다. 증강 선택은 없다.</summary>
        public void ShowSecond(AffinityDialogueData data, Sprite portrait, PlayerStats stats, PlayerHealth health,
                         Action<AugmentData, float> recordBuff, Action onComplete)
        {
            PlayLines(data?.bossIntroLines, onComplete);
        }

        private void PlayLines(StoryLine[] lines, Action onDone)
        {
            if (lines == null || lines.Length == 0 || introPanel == null)
            {
                onDone?.Invoke();
                return;
            }

            if (_lineRoutine != null) StopCoroutine(_lineRoutine);
            _lineRoutine = StartCoroutine(PlayLinesRoutine(lines, onDone));
        }

        private IEnumerator PlayLinesRoutine(StoryLine[] lines, Action onDone)
        {
            introPanel.gameObject.SetActive(true);

            int index = 0;
            introPanel.ShowLine(lines[index]);

            while (!StoryDialogueLogic.IsFinished(index, lines.Length))
            {
                yield return null;

                if (!AdvancePressed()) continue;

                index = StoryDialogueLogic.NextIndex(index, lines.Length);
                if (StoryDialogueLogic.IsFinished(index, lines.Length)) break;

                introPanel.ShowLine(lines[index]);
            }

            introPanel.gameObject.SetActive(false);
            _lineRoutine = null;
            onDone?.Invoke();
        }

        private bool AdvancePressed()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame);
        }

        private void ShowQuestion(AffinityDialogueQuestion question, Sprite portrait, PlayerStats stats,
                         PlayerHealth health, Action<AugmentData, float> recordBuff, Action onComplete)
        {
            if (question == null || question.choices == null || question.choices.Length == 0)
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

            panel.Show(portrait, question, choice =>
            {
                if (choice.augment != null)
                {
                    float amount = AffinityBuffLogic.GetBuffAmount(choice.augment.maxCap, buffRatio);
                    AffinityBuffApplier.Apply(choice.augment, amount, stats, health);
                    recordBuff?.Invoke(choice.augment, amount);
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

`ShowSecond`의 `stats`/`health`/`recordBuff` 매개변수는 더 이상 안에서 쓰이지 않지만, 시그니처는 그대로
둔다 — `GameManager.TriggerBossIntroDialogue()`의 호출부를 이번 계획에서 안 건드리기 위해서다(스펙 2번
섹션 그대로).

- [ ] **Step 2: 컴파일 + 회귀 확인**

"배치 테스트 실행 명령"을 실행한다.
Expected: `error CS` 없음, `total="263" passed="263" failed="0"`. 끝나면 `rm -f TestResults.xml`.

- [ ] **Step 3: 커밋, 푸시, PR B**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/Core/AffinityDialogueController.cs
git commit -m "feat: AffinityDialogueController가 introLines/bossIntroLines 나레이션을 재생하도록 교체

- Show(): introLines를 먼저 재생한 뒤 question1(증강 3택)으로 이어짐
- ShowSecond(): bossIntroLines만 재생(증강 선택 로직 제거)
- 줄 넘기기는 StoryScene 작업 때 만든 StoryDialogueLogic/StoryPanel 재사용

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
git push -u origin feature/affinity-dialogue-controller
gh pr create --title "feat: AffinityDialogueController 나레이션 재생 (캐릭터 탐색-선택 흐름 B)" --body "$(cat <<'EOF'
## 요약
호감도 대화 #1(런 시작 직전)에 `introLines` 자기소개 나레이션을 먼저 재생하고, #2(보스전 직전)는 `bossIntroLines`만 재생하도록(증강 선택 제거) `AffinityDialogueController`를 바꿨습니다. 줄 넘기기는 `StoryScene` 작업 때 만든 `StoryDialogueLogic`/`StoryPanel`을 재사용합니다. `introPanel` 필드는 아직 씬에서 안 꽂혀 있어(`null`), introLines/bossIntroLines가 있어도 지금은 바로 건너뜁니다 — 씬 연결은 PR F에서 합니다.

- 스펙: `docs/superpowers/specs/2026-09-22-character-select-flow-design.md`
- 계획: `docs/superpowers/plans/2026-09-22-character-select-flow.md` (PR B)
- 선행 PR: A(스키마)

## 테스트
- EditMode 263/263 통과 (컴파일 확인, MonoBehaviour라 신규 테스트 없음)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

---

# PR C — `LockedCharacterController` (A 병합 후, B와 병렬)

### Task 3: 이나리 선택 제한 컨트롤러 신규 작성

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git checkout -b feature/locked-character-controller
```

(B가 아직 병합 전이어도 `main` 기준이라 문제없다 — B와 파일이 안 겹친다.)

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/LockedCharacterController.cs`

**Interfaces:**
- Consumes (A): `CharacterData.characterName`, `CharacterData.lockedMessage`
- Consumes (기존): `SushiSurvival.Data.StoryLine`, `SushiSurvival.UI.StoryPanel.ShowLine(StoryLine)`
- Produces (Task 4·6이 사용): `public void Show(CharacterData data)` — 직렬화 필드 `characterSelectPanel`(`GameObject`), `introPanel`(`StoryPanel`), `backButton`(`Button`), `continueButton`(`Button`)

MonoBehaviour라 EditMode 테스트 대상이 아니다. 검증은 컴파일 + 회귀 없음.

- [ ] **Step 1: 컨트롤러 작성**

`unity/Assets/_Project/Scripts/Core/LockedCharacterController.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Data;
using SushiSurvival.UI;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 선택이 제한된 캐릭터(이나리)를 클릭했을 때의 안내 화면. 플레이어를 스폰하지도
    /// GameManager.CurrentState를 바꾸지도 않는다 — 캐릭터 선택 화면 안에서만 오간다.
    /// </summary>
    public class LockedCharacterController : MonoBehaviour
    {
        [SerializeField] private GameObject characterSelectPanel;
        [Tooltip("안내 문구를 보여주는 나레이션 패널(StoryScene에서 만든 것과 같은 컴포넌트).")]
        [SerializeField] private StoryPanel introPanel;
        [SerializeField] private Button backButton;
        [SerializeField] private Button continueButton;

        private CharacterData _current;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(HandleBackClicked);
            if (continueButton != null) continueButton.onClick.AddListener(HandleContinueClicked);

            if (introPanel != null) introPanel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (backButton != null) backButton.onClick.RemoveListener(HandleBackClicked);
            if (continueButton != null) continueButton.onClick.RemoveListener(HandleContinueClicked);
        }

        public void Show(CharacterData data)
        {
            _current = data;

            if (characterSelectPanel != null)
                characterSelectPanel.SetActive(false);

            ShowMessage();
        }

        private void ShowMessage()
        {
            if (introPanel == null || _current == null) return;

            introPanel.gameObject.SetActive(true);
            introPanel.ShowLine(new StoryLine
            {
                speakerName = _current.characterName,
                text = _current.lockedMessage,
            });
        }

        private void HandleBackClicked()
        {
            if (introPanel != null) introPanel.gameObject.SetActive(false);
            if (characterSelectPanel != null) characterSelectPanel.SetActive(true);
            _current = null;
        }

        // "계속 진행하기"는 실제로 진행되지 않는다 — 같은 안내를 다시 보여줄 뿐이다.
        private void HandleContinueClicked() => ShowMessage();
    }
}
```

- [ ] **Step 2: 컴파일 + 회귀 확인**

"배치 테스트 실행 명령"을 실행한다.
Expected: `error CS` 없음, `total="263" passed="263" failed="0"`. 끝나면 `rm -f TestResults.xml`.
새 `.meta`가 생겼는지 확인한다:

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git status --short unity/Assets/_Project/Scripts/Core/LockedCharacterController.cs unity/Assets/_Project/Scripts/Core/LockedCharacterController.cs.meta
```

- [ ] **Step 3: 커밋, 푸시, PR C**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/Core/LockedCharacterController.cs unity/Assets/_Project/Scripts/Core/LockedCharacterController.cs.meta
git commit -m "feat: 이나리 선택 제한 안내 컨트롤러 LockedCharacterController 추가

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
git push -u origin feature/locked-character-controller
gh pr create --title "feat: 이나리 선택 제한 컨트롤러 (캐릭터 탐색-선택 흐름 C)" --body "$(cat <<'EOF'
## 요약
잠긴 캐릭터(이나리)를 클릭했을 때 캐릭터 선택 패널을 숨기고 제한 안내를 보여주는 새 컨트롤러. 뒤로가기는 캐릭터 선택으로 복귀, 계속 진행하기는 같은 안내를 다시 보여줄 뿐 실제로 진행되지 않습니다. 플레이어 스폰이나 `GameManager` 상태 변경은 없습니다. 아직 아무도 이 컨트롤러를 안 불러서 게임 동작은 바뀌지 않습니다.

- 스펙: `docs/superpowers/specs/2026-09-22-character-select-flow-design.md`
- 계획: `docs/superpowers/plans/2026-09-22-character-select-flow.md` (PR C)
- 선행 PR: A(스키마) — B와는 독립이라 병렬로 올립니다.

## 테스트
- EditMode 263/263 통과 (컴파일 확인, MonoBehaviour라 신규 테스트 없음)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

---

# PR D — `CharacterSelectButton` (C 병합 후)

### Task 4: 잠긴 카드도 클릭되게 변경

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git log --oneline -3   # C 병합 커밋이 보여야 한다
git checkout -b feature/character-select-button-locked
```

**Files:**
- Modify: `unity/Assets/_Project/Scripts/UI/CharacterSelectButton.cs`

**Interfaces:**
- Consumes (C): `SushiSurvival.Core.LockedCharacterController.Show(CharacterData)`
- Produces (Task 6이 사용): `CharacterSelectButton` 직렬화 필드에 `lockedCharacterController`(`LockedCharacterController`)가 새로 추가됨

MonoBehaviour라 EditMode 테스트 대상이 아니다.

- [ ] **Step 1: 버튼 스크립트 수정**

`unity/Assets/_Project/Scripts/UI/CharacterSelectButton.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 캐릭터 선택 버튼 하나. 캐릭터가 3종으로 고정이라 동적 생성 대신
    /// 씬에 미리 배치하고 인스펙터에서 CharacterData를 연결한다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CharacterSelectButton : MonoBehaviour
    {
        [SerializeField] private CharacterData characterData;
        [Tooltip("캐릭터 초상화를 표시할 Image. 비워두면 표시하지 않는다.")]
        [SerializeField] private Image portraitImage;
        [Tooltip("아직 구현되지 않은 캐릭터는 체크. 회색 처리되고, 클릭하면 선택 제한 안내로 이어진다.")]
        [SerializeField] private bool locked;
        [Tooltip("locked일 때 클릭하면 보여줄 안내 컨트롤러. locked가 아니면 안 쓴다.")]
        [SerializeField] private LockedCharacterController lockedCharacterController;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClicked);
        }

        private void Start()
        {
            if (portraitImage != null && characterData != null)
                portraitImage.sprite = characterData.selectCardSprite != null
                    ? characterData.selectCardSprite
                    : characterData.portraitSprite;

            if (locked && portraitImage != null)
                portraitImage.color = Color.gray;
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            if (locked)
            {
                if (lockedCharacterController != null)
                    lockedCharacterController.Show(characterData);
                else
                    Debug.LogError($"{name}: lockedCharacterController가 비어 있어 제한 안내를 표시할 수 없습니다.");
                return;
            }

            GameManager.Instance.StartRun(characterData);
        }
    }
}
```

(`_button.interactable = !locked;` 줄은 제거했다 — 이나리도 클릭돼야 하니 버튼은 항상 interactable이고,
회색 처리는 `Start()`의 색상 변경만으로 이미 표현된다.)

- [ ] **Step 2: 컴파일 + 회귀 확인**

"배치 테스트 실행 명령"을 실행한다.
Expected: `error CS` 없음, `total="263" passed="263" failed="0"`. 끝나면 `rm -f TestResults.xml`.

- [ ] **Step 3: 커밋, 푸시, PR D**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/UI/CharacterSelectButton.cs
git commit -m "feat: 잠긴 캐릭터 카드도 클릭 가능하게 하고 LockedCharacterController로 연결

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
git push -u origin feature/character-select-button-locked
gh pr create --title "feat: 잠긴 캐릭터 카드 클릭 처리 (캐릭터 탐색-선택 흐름 D)" --body "$(cat <<'EOF'
## 요약
지금은 `locked` 캐릭터(이나리) 카드가 `Button.interactable = false`라 아예 안 눌립니다. 이제 항상 클릭 가능하게 하고, `locked`면 런을 시작하는 대신 `LockedCharacterController.Show()`를 부릅니다. `lockedCharacterController` 필드가 아직 씬에서 안 꽂혀 있으면 에러 로그만 남기고 아무 일도 안 일어납니다 — 씬 연결은 PR F에서 합니다.

- 스펙: `docs/superpowers/specs/2026-09-22-character-select-flow-design.md`
- 계획: `docs/superpowers/plans/2026-09-22-character-select-flow.md` (PR D)
- 선행 PR: A(스키마), C(`LockedCharacterController`)

## 테스트
- EditMode 263/263 통과 (컴파일 확인, MonoBehaviour라 신규 테스트 없음)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

---

# PR E — `InariCharacterData.asset` (A 병합 후, B·C·D와 병렬)

### Task 5: 이나리용 최소 `CharacterData` 에셋 생성 (TDD)

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git checkout -b feature/inari-character-data
```

**Files:**
- Create: `unity/Assets/Tests/EditMode/InariCharacterDataAssetTests.cs`
- Create: `unity/Assets/_Project/Data/InariCharacterData.asset` (임시 스크립트로 생성)

**Interfaces:**
- Consumes (A): `CharacterData.characterName`, `CharacterData.lockedMessage`, `CharacterData.selectCardSprite`
- Produces (Task 6이 사용): 에셋 경로 `Assets/_Project/Data/InariCharacterData.asset`

이나리는 실제로 스폰되지 않으므로(`locked` 분기는 `GameManager.StartRun`을 안 부른다) `playerPrefab`/`weaponData`/`animatorController`/`portraitSprite`/`affinityDialogue`는 비워둔다. `characterName`·`lockedMessage`·`selectCardSprite`만 채운다.

- [ ] **Step 1: 실패하는 무결성 테스트 작성**

`unity/Assets/Tests/EditMode/InariCharacterDataAssetTests.cs`:

```csharp
using NUnit.Framework;
using UnityEditor;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    /// <summary>이나리 CharacterData 에셋이 선택 제한 흐름에 필요한 값을 갖췄는지 확인한다.</summary>
    public class InariCharacterDataAssetTests
    {
        private const string AssetPath = "Assets/_Project/Data/InariCharacterData.asset";

        private static CharacterData Load()
        {
            var data = AssetDatabase.LoadAssetAtPath<CharacterData>(AssetPath);
            Assert.IsNotNull(data, $"{AssetPath}를 불러올 수 없습니다.");
            return data;
        }

        [Test]
        public void CharacterName_IsInari()
        {
            Assert.AreEqual("이나리", Load().characterName);
        }

        [Test]
        public void LockedMessage_MatchesConfirmedText()
        {
            Assert.AreEqual("이나리는 특정 캐릭터 디자인 이슈로 선택이 제한됩니다.", Load().lockedMessage);
        }

        [Test]
        public void SelectCardSprite_IsAssigned()
        {
            Assert.IsNotNull(Load().selectCardSprite, "캐릭터 선택 화면에 쓸 카드 아트가 비어 있습니다.");
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해 실패 확인**

"배치 테스트 실행 명령"을 실행한다.
Expected: 컴파일은 통과하고 새 3개 테스트가 `... InariCharacterData.asset를 불러올 수 없습니다`로 실패한다(`failed="3"`). 기존 263개는 통과.

- [ ] **Step 3: 에셋 생성 스크립트 작성·실행**

`CharacterData.cs.meta`의 GUID와, 이미 씬에 들어있는 이나리 카드 아트(`Assets/Art/캐릭터/캐릭터/캐릭터선택/이나리.png`, guid `13421d0012f84374998c1f654bea96f2`)를 참조해 에셋 YAML을 만든다. 일회용 스크립트라 저장소에 커밋하지 않고 `$TEMP`에 둔다.

`$TEMP/gen_inari_character_data.js`:

```javascript
const fs = require('fs');
const root = 'C:/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity/Assets/_Project';

const scriptMeta = fs.readFileSync(`${root}/Scripts/Data/CharacterData.cs.meta`, 'utf8');
const scriptGuid = scriptMeta.match(/guid:\s*([0-9a-f]{32})/)[1];

const CARD_SPRITE_GUID = '13421d0012f84374998c1f654bea96f2'; // 이나리.png (캐릭터선택 카드 아트)

const BS = String.fromCharCode(92);
const q = (s) => '"' + JSON.stringify(s).slice(1, -1)
  .replace(/[^\x20-\x7e]/g, (c) => BS + 'u' + c.charCodeAt(0).toString(16).padStart(4, '0')) + '"';

const y = `%YAML 1.1
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
  m_Name: InariCharacterData
  m_EditorClassIdentifier: 
  characterName: ${q('이나리')}
  portraitSprite: {fileID: 0}
  selectCardSprite: {fileID: 21300000, guid: ${CARD_SPRITE_GUID}, type: 3}
  playerPrefab: {fileID: 0}
  baseMoveSpeed: 3
  baseMaxHealth: 100
  weaponData: {fileID: 0}
  animatorController: {fileID: 0}
  affinityDialogue: {fileID: 0}
  lockedMessage: ${q('이나리는 특정 캐릭터 디자인 이슈로 선택이 제한됩니다.')}
`;

fs.writeFileSync(`${root}/Data/InariCharacterData.asset`, y);
console.log('wrote InariCharacterData.asset, script guid', scriptGuid);
```

실행:

```bash
node "$TEMP/gen_inari_character_data.js"
```

Expected: `wrote InariCharacterData.asset, script guid <32자리>`

- [ ] **Step 4: 테스트 실행해 통과 확인**

"배치 테스트 실행 명령"을 실행한다.
Expected: `error CS` 없음, `total="266" passed="266" failed="0"`(263 + 새 3). 끝나면 `rm -f TestResults.xml`.
`.meta` 확인:

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git status --short unity/Assets/_Project/Data/InariCharacterData.asset unity/Assets/_Project/Data/InariCharacterData.asset.meta unity/Assets/Tests/EditMode/InariCharacterDataAssetTests.cs unity/Assets/Tests/EditMode/InariCharacterDataAssetTests.cs.meta
```

**실패 시 대처:** 에셋 로드 자체가 실패하면 스크립트를 고치지 말고, 사용자가 에디터에서
`Create → SushiSurvival → Character Data`로 에셋을 만들어 `characterName`/`lockedMessage`를
위 테스트 문자열 그대로, `selectCardSprite`에 `Assets/Art/캐릭터/캐릭터/캐릭터선택/이나리`를
직접 연결한다(테스트는 그대로 검증해 준다).

- [ ] **Step 5: 커밋, 푸시, PR E**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Data/InariCharacterData.asset unity/Assets/_Project/Data/InariCharacterData.asset.meta unity/Assets/Tests/EditMode/InariCharacterDataAssetTests.cs unity/Assets/Tests/EditMode/InariCharacterDataAssetTests.cs.meta
git commit -m "feat: 이나리 CharacterData 에셋 추가(이름/제한 문구/카드 아트만)

실제로 스폰되지 않는 캐릭터라 플레이어 프리팹·무기·애니메이터·대화 데이터는
비워둔다.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
git push -u origin feature/inari-character-data
gh pr create --title "feat: 이나리 CharacterData 에셋 (캐릭터 탐색-선택 흐름 E)" --body "$(cat <<'EOF'
## 요약
이나리 선택 제한 흐름에 쓸 최소 `CharacterData` 에셋. 실제로 스폰되지 않는 캐릭터라 이름·제한 문구·카드 아트만 채웠습니다. 아직 씬의 `Button_Inari`가 이 에셋을 참조하지 않아 게임 동작은 바뀌지 않습니다 — 연결은 PR F에서 합니다.

- 스펙: `docs/superpowers/specs/2026-09-22-character-select-flow-design.md`
- 계획: `docs/superpowers/plans/2026-09-22-character-select-flow.md` (PR E)
- 선행 PR: A(스키마) — B·C·D와는 독립이라 병렬로 올립니다.

## 테스트
- EditMode 266/266 통과 (263 + 신규 3)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

---

# PR F — 씬 · 데이터 콘텐츠 배선 (B·D·E 모두 병합 후, 사용자 에디터 작업)

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git log --oneline -6   # B, C, D, E 병합 커밋이 다 보여야 한다
git checkout -b feature/character-select-flow-wiring
```

### Task 6: `GameScene` 배선 + 대사 데이터 입력 (사용자 에디터 작업)

에이전트는 씬/에셋 Inspector 값을 대신 편집하지 않는다(프로젝트 관례). 아래 순서를
사용자가 따라 하고, 에이전트는 저장된 파일을 읽어 배선을 검증한다.

#### A. 자기소개 나레이션 데이터 입력

**1.** Project 창에서 `Assets/_Project/Data/EggCharacterData`를 선택 → Inspector에서
   `Affinity Dialogue` 필드가 가리키는 에셋(`EggAffinityDialogue`)을 더블클릭

**2.** `Intro Lines` 배열을 펼쳐서 **Size를 6**으로 → 아래 표대로 각 원소의
   `Speaker Name`/`Portrait`/`Text`를 채운다(`Background`는 전부 비워둠):

   | # | Speaker Name | Portrait | Text |
   |---|---|---|---|
   | 0 | 아델린 | (아델린 초상화) | 만나서 반갑습니다. 저는 스시왕국의 9번째 왕녀, 아델린이라고 합니다. |
   | 1 | 플레이어 | (비움) | 지금 무슨 일이 벌어지고 있는거죠? |
   | 2 | 아델린 | (아델린 초상화) | 지금 롤왕국의 군대가 우리 왕국에 들이닥치고 있습니다. |
   | 3 | 플레이어 | (비움) | 공주님의 무기는 무엇이죠? |
   | 4 | 아델린 | (아델린 초상화) | 제 무기는 계란 양산입니다. |
   | 5 | 아델린 | (아델린 초상화) | 자, 이제 선택해주세요. |

**3.** `Boss Intro Lines`도 **Size 2**로:

   | # | Speaker Name | Portrait | Text |
   |---|---|---|---|
   | 0 | 아델린 | (아델린 초상화) | 지금 이 소리 들리시나요? 쿠쿵!! 쿠쿵!! 쿠쿵!! |
   | 1 | 아델린 | (아델린 초상화) | 저 존재는 롤왕국이 길러온 전투 병기입니다. 지금까지 마주했던 적들과 차원이 다르니 조심하셔야 합니다. |

   (아까 있던 `Question2` 필드는 더 이상 안 보일 겁니다 — 정상입니다, 코드에서 지운 자리입니다.)

**4.** 같은 방식으로 `ShrimpCharacterData` → `ShrimpAffinityDialogue`를 열어서 `Intro Lines`
   **Size 5**:

   | # | Speaker Name | Portrait | Text |
   |---|---|---|---|
   | 0 | 카마리온 | (카마리온 초상화) | 난 스시왕국 최고의 대장장이 가문의 7대 후계자, 카마리온이라고 한다. 스시왕국의 모든 무기들은 다 우리 가문이 만들었다고 보면 되지, 후훗. |
   | 1 | 플레이어 | (비움) | ...당신이 들고 있는 것은 무엇이죠? |
   | 2 | 카마리온 | (카마리온 초상화) | 이건 나의 무기 간장 권총이지, 요번에 내가 발명한 아주 강력한 무기지. 하하. |
   | 3 | 플레이어 | (비움) | ...멋있네요. |
   | 4 | 카마리온 | (카마리온 초상화) | 그렇게 언제까지 겁쟁이처럼 질문만 하고 있을 거지? 자, 선택해. |

**5.** `Boss Intro Lines` **Size 2**:

   | # | Speaker Name | Portrait | Text |
   |---|---|---|---|
   | 0 | 카마리온 | (카마리온 초상화) | 지금 이 소리 들리지? 쿠쿵쿠쿵 쿠쿵. |
   | 1 | 카마리온 | (카마리온 초상화) | 드디어 나오는군, 저 자식. 드디어 나의 무기로 저 녀석을 없애버릴 시간이군. 저 녀석은 아까 녀석들이랑 차원이 다르니 조심하라고! |

**6.** 두 에셋 다 저장(Ctrl+S 또는 에디터 저장)

#### B. `introPanel`용 나레이션 UI 오브젝트 만들기

이미 있는 호감도 대화창(`AffinityDialoguePanel`)의 `TextBar`와 같은 자리에, 나레이션
전용 `StoryPanel`을 하나 더 만든다(겹쳐 있어도 평소엔 하나만 활성화되니 문제없다).

**1.** `GameScene`을 연다.

**2.** Hierarchy에서 기존 `AffinityDialoguePanel`을 찾아 그 자식 `TextBar`를 선택 →
   **Ctrl+D**로 복제 → 복제본을 **`AffinityDialoguePanel`의 형제**(같은 부모, 즉 `Canvas`
   바로 아래)로 옮기고 이름을 **`IntroNarrationPanel`**로 바꾼다.

**3.** `IntroNarrationPanel` 안에 있던 자식(선택지 버튼 등, `AffinityDialoguePanel` 고유
   요소)은 지우고 **이름표(NameText)·미니 초상화(MiniPortrait)·본문(BodyText)만** 남긴다.
   (이름이 다를 수 있으니, `TextBar`를 복제했을 때 안에 있던 자식 구조를 그대로 보고
   판단 — 2번 작업(`StoryScene`)에서 만든 `NameplateRoot`/`NameText`/`MiniPortrait`/
   `BodyText` 구조와 대응된다.)

**4.** `IntroNarrationPanel`에 **Add Component → Story Panel** → 필드를 그 자식들로 연결
   (Background Front/Back은 비워둠 — 배경은 안 씀). 기본적으로 꺼둔다: Inspector 좌측 위
   체크박스를 해제(비활성)해서 평소엔 안 보이게 한다.

#### C. `AffinityDialogueController`에 연결

**1.** Hierarchy에서 `AffinityDialogueController` 컴포넌트가 있는 오브젝트를 찾는다
   (`GameManager`의 `Affinity Dialogue Controller` 필드가 가리키는 오브젝트를 클릭하면
   빠르다).

**2.** **Intro Panel** 필드에 방금 만든 `IntroNarrationPanel`을 연결.

#### D. 이나리 안내 화면 UI 만들기

**1.** `Canvas` 우클릭 → `IntroNarrationPanel`을 다시 **Ctrl+D** 복제 → 이름
   **`LockedCharacterPanel`**로 변경(같은 구조: 이름표/미니 초상화/본문, Story Panel
   컴포넌트도 그대로 복제돼 있을 것).

**2.** `LockedCharacterPanel` 안에 버튼 2개 추가:
   - `LockedCharacterPanel` 우클릭 → **UI → Button (Legacy)** → 이름 `BackButton`,
     자식 Text를 **"이전으로 돌아가기"**로
   - 같은 방식으로 `ContinueButton`, 자식 Text **"계속 진행하기"**
   - 두 버튼을 나란히 배치(예: `BodyText` 아래쪽, 좌우로)

**3.** Hierarchy 빈 곳에서 우클릭 → **Create Empty** → 이름 **`LockedCharacterController`**
   (씬 루트, `Canvas`의 자식 아님) → **Add Component → Locked Character Controller**

**4.** **Locked Character Controller** 필드 연결:
   - Character Select Panel = 캐릭터 선택 화면 전체 패널(지금 카드 3장이 들어있는 그
     오브젝트 — `CharacterSelectButton`들의 부모)
   - Intro Panel = `LockedCharacterPanel`의 Story Panel
   - Back Button = `BackButton`
   - Continue Button = `ContinueButton`

**5.** `LockedCharacterPanel`도 기본적으로 꺼둔다(비활성 체크).

#### E. 이나리 버튼 연결

**1.** Hierarchy에서 `Button_Inari` 선택 → **Character Select Button** 컴포넌트에서:
   - **Character Data** 필드에 `Assets/_Project/Data/InariCharacterData` 연결
     (지금 비어 있을 것)
   - **Locked** 체크가 이미 돼 있는지 확인(돼 있어야 정상)
   - **Locked Character Controller** 필드에 방금 만든 `LockedCharacterController` 연결

**2.** 씬 저장(Ctrl+S)

---

### 에이전트 검증

저장 후 다음을 확인한다:

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요\unity"
git status --short Assets/_Project/Scenes Assets/_Project/Data
echo "--- Egg/Shrimp 대화 데이터 introLines/bossIntroLines 개수 ---"
grep -c "speakerName:" Assets/_Project/Data/EggAffinityDialogue.asset Assets/_Project/Data/ShrimpAffinityDialogue.asset
echo "--- GameScene: 새 컴포넌트/필드 확인 ---"
grep -n "introPanel:\|lockedCharacterController:\|characterSelectPanel:\|backButton:\|continueButton:" Assets/_Project/Scenes/GameScene.unity
echo "--- Button_Inari characterData ---"
grep -n "m_Name: Button_Inari" -A 40 Assets/_Project/Scenes/GameScene.unity | grep "characterData:"
```

Expected: `EggAffinityDialogue.asset`/`ShrimpAffinityDialogue.asset`가 변경으로 뜨고,
`speakerName:` 개수가 각각 8개(Egg: intro 6 + boss 2) / 7개(Shrimp: intro 5 + boss 2).
`GameScene.unity`에서 위 필드들이 전부 `{fileID: 0}`이 아니어야 한다.
`Button_Inari`의 `characterData`가 `InariCharacterData` 에셋을 가리켜야 한다(더 이상
`{fileID: 0}`이 아님). 비어 있는 필드가 있으면 사용자에게 해당 필드를 알려준다.

- [ ] **Step 1: 컴파일 + 회귀 확인**

"배치 테스트 실행 명령"을 실행한다(씬/에셋만 바뀌어서 컴파일에 영향은 없지만,
습관대로 확인한다).
Expected: `error CS` 없음, `total="266" passed="266" failed="0"`. 끝나면 `rm -f TestResults.xml`.

- [ ] **Step 2: Play 모드 전체 흐름 확인 (사용자)**

`GameScene`을 Play해서 캐릭터 선택 화면까지 간 뒤:

- [ ] **아델린** 클릭 → 6줄 나레이션이 클릭/Space/Enter로 순서대로 넘어감(마지막 "자,
      이제 선택해주세요" 포함) → 증강 3택 화면으로 자연스럽게 이어짐 → 아무거나 선택 →
      전투 시작
- [ ] **카마리온**도 동일하게 5줄 → 증강 3택 → 전투 시작
- [ ] **이나리** 클릭 → 캐릭터 선택 화면이 사라지고 "이나리는 특정 캐릭터 디자인
      이슈로 선택이 제한됩니다." 안내가 뜸(이름표에 "이나리")
- [ ] 이나리 화면에서 **"이전으로 돌아가기"** → 캐릭터 선택 화면(카드 3장)으로 복귀
- [ ] 다시 이나리 클릭 → **"계속 진행하기"** → 화면이 안 바뀌고 같은 안내가 유지됨
      (전투로 넘어가지 않음)
- [ ] 아델린 또는 카마리온으로 런을 계속해서 5:00(또는 테스트용으로 `bossSpawnTime`을
      잠깐 낮춰서) 보스전 진입 직전까지 가서, **증강 선택 없이 2줄 나레이션만** 뜨고
      바로 보스전으로 넘어가는지 확인(테스트용으로 값을 바꿨다면 확인 후 300으로 복구)

- [ ] **Step 3: 커밋, 푸시, PR F**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scenes/GameScene.unity unity/Assets/_Project/Data/EggAffinityDialogue.asset unity/Assets/_Project/Data/ShrimpAffinityDialogue.asset
git status --short
git commit -m "feat: 캐릭터 탐색-선택 흐름 씬 배선 + 자기소개/보스전 직전 대사 데이터

- 아델린/카마리온 introLines(자기소개)·bossIntroLines(보스전 직전) 대본을
  PDF 원문 그대로 입력
- IntroNarrationPanel/LockedCharacterPanel 신설, AffinityDialogueController·
  LockedCharacterController에 연결
- Button_Inari를 InariCharacterData·LockedCharacterController에 연결

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
git push -u origin feature/character-select-flow-wiring
gh pr create --title "feat: 캐릭터 탐색→선택 흐름 씬 배선 (F)" --body "$(cat <<'EOF'
## 요약
캐릭터 탐색→선택 흐름의 마지막 단계. 아델린·카마리온 자기소개/보스전 직전 대사 데이터를 채우고, `GameScene`에 나레이션용 `StoryPanel`(도입부·이나리 안내)을 추가해 코드(PR A~E)와 연결했습니다.

- 스펙: `docs/superpowers/specs/2026-09-22-character-select-flow-design.md`
- 계획: `docs/superpowers/plans/2026-09-22-character-select-flow.md` (PR F)
- 선행 PR: A, B, C, D, E 전부 병합됨

## 협업자 확인 부탁
`GameScene.unity`(트랙 A 소유 씬)에 새 UI 오브젝트 2개(`IntroNarrationPanel`, `LockedCharacterPanel`+버튼 2개)와 `LockedCharacterController` 오브젝트를 추가했습니다. 기존 UI 요소는 안 건드렸습니다.

## 변경 사항
- `EggAffinityDialogue`/`ShrimpAffinityDialogue`에 `introLines`(자기소개)·`bossIntroLines`(보스전 직전) 대본 입력, 기존 `question2`(증강 3택) 데이터는 제거됨
- `IntroNarrationPanel`(도입부 나레이션), `LockedCharacterPanel`+뒤로가기/계속하기 버튼(이나리 안내) 신설
- `Button_Inari`를 `InariCharacterData`와 `LockedCharacterController`에 연결

## 테스트
- EditMode 266/266 통과
- Play 모드로 아델린/카마리온 전체 흐름(나레이션→증강 3택→전투), 이나리 뒤로가기/계속하기, 보스전 직전 나레이션(증강 선택 없음) 확인 완료(사용자)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

**여기서 멈추고 사용자가 PR F를 병합하길 기다린다.** 병합 후 `main` 동기화, 브랜치 정리,
메모리(`sushi-survival-project.md`) 업데이트, `docs/game-flow-roadmap.md`의 3번 항목을
✅로 갱신하는 작은 문서 PR을 이어서 한다.
