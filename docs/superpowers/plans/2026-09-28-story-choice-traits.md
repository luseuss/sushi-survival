# 세계관 대화 선택 → 성향 태그 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 세계관 대화(StoryScene) 중 질문 2번의 답이 성향 태그(용감함·신중함·다정함)로 쌓이고, 그 성향이 호감도 대화 선택지 순서(★ 추천)·보스 등장 대사 한 줄·결과 화면 한마디에 반영되게 한다.

**Architecture:** 성향은 `PlayerTraitState`(static)가 씬 전환을 넘어 들고 있고, 대표 성향은 순수 함수가 계산한다. 스토리 안의 질문은 `StoryDialogueData.choicePoints`(별도 클래스, `StoryLine`은 안 건드림)와 새 `StoryChoicePanel`로 처리하고, 세 소비처는 각자 한 곳씩만 고친다. 성향이 `None`이면 세 곳 모두 기존 동작 그대로다. 전부 추가만 하는 변경이다.

**Tech Stack:** Unity 2022.3.62f3, Built-in 2D, uGUI **Legacy Text/Button만**(TextMeshPro 미설치), NUnit EditMode 테스트. 관례: 판정 로직은 `XxxLogic` 정적 클래스 + EditMode 테스트, MonoBehaviour는 컴파일+회귀 확인만.

**Spec:** `docs/superpowers/specs/2026-09-28-story-choice-traits-design.md`

## Global Constraints

- `main` 직접 커밋·푸시·병합 절대 금지 — **문서 파일도 예외 없음.** 모든 태스크는 새 브랜치에서 시작하고 PR로 올린다. 병합 버튼은 사람만 누른다.
- `git add .` 금지 — 파일을 명시하고 `.meta`를 항상 같이 커밋한다.
- 씬(`.unity`)·프리팹(`.prefab`) 편집은 사람이 Unity Editor GUI로 한다. 에이전트는 새 `.asset`을 만들거나 기존 `.asset`의 **최상위에 새 키를 덧붙이는** 스크립트만 쓴다(기존 에셋 안쪽 필드는 스크립트로 안 건드린다).
- 데이터 에셋 테스트는 문구·줄 번호 같은 **절대값을 assert하지 않는다.** 개수·존재·비어 있지 않음 같은 구조만 검증한다.
- **커밋 전에 항상 `git status`로 예상 밖 파일이 없는지, `git diff`로 테스트용 임시값(`bossSpawnTime` 등)이 섞이지 않았는지 확인한다**(씬·프리팹 PR).
- 성향 표시명(용감함/신중함/다정함)은 코드에 박지 않고 `PlayerTraitLines` 데이터에 둔다.

### 배치 테스트 실행 명령 (이 문서에서 "테스트 실행"이라 부르는 것)

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요\unity"
"C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" -batchmode -projectPath "$(pwd -W)" -runTests -testPlatform EditMode -testResults "$(pwd -W)/TestResults.xml" -logFile - > "$TEMP/unity_test.log" 2>&1
grep -E "error CS" "$TEMP/unity_test.log" | sort -u | head
grep -c "HandleProjectAlreadyOpenInAnotherInstance" "$TEMP/unity_test.log"
head -c 260 TestResults.xml | grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"'
grep -o 'test-case[^>]*result="Failed"' TestResults.xml | grep -o 'methodname="[^"]*"'
rm -f TestResults.xml
```

성공 기준: `error CS` 없음, 에디터 잠금 카운트 0(에디터가 열려 있으면 사용자에게 닫아달라고 요청), 실패는 **알려진 기존 1건**(`Portraits_AdelineAndKamarionHaveOne_InariHasNone`)뿐.
**baseline은 태스크 시작 직전 `main`에서 처음 돌린 개수**다(2026-09-27 기준 412개, 411 통과). 기대 개수는 그 baseline에 이 태스크가 추가한 만큼을 더한 값으로 본다.

### 이미 존재하는(이번 계획이 그대로 쓰는) 타입

- `SushiSurvival.Data.StoryLine { string speakerName; Sprite portrait; Sprite standing; string text; Sprite background; }` (`Data/StoryDialogueData.cs`)
- `SushiSurvival.Data.AffinityDialogueChoice { string choiceText; AugmentData augment; }`, `AffinityDialogueQuestion { string questionText; AffinityDialogueChoice[] choices; }`, `AffinityDialogueData { introLines, question1, bossIntroLines, bossEncounterLines }` (`Data/AffinityDialogueData.cs`)
- `SushiSurvival.Core.StoryDialogueLogic.NextIndex/IsFinished/ResolveBackgroundIndex/IsBackgroundChange`
- `SushiSurvival.UI.StoryPanel.ShowLine(StoryLine)`, `IsTyping`, `CompleteTyping()`, `SetBackground`, `FadeToBackground`
- `SushiSurvival.UI.AffinityDialoguePanel.Show(Sprite portrait, Sprite standing, AffinityDialogueQuestion, Action<AffinityDialogueChoice>)`
- `SushiSurvival.Core.RunOutcome { Victory, Defeat }`

---

## 브랜치 / PR 분할

| PR | 브랜치 | 태스크 | 시작 시점 | 의존 |
|---|---|---|---|---|
| **A** | `feature/trait-state-logic` | Task 1 (`PlayerTrait`·`PlayerTraitState`·순수 로직 + 테스트) | `main`에서 지금 | 없음 |
| **B** | `feature/trait-data-schema` | Task 2 (데이터 스키마 필드·`PlayerTraitLines`) | **A 병합 후** | A의 `PlayerTrait` |
| **C** | `feature/story-choice-scene` | Task 3 (`StoryChoiceLogic`·`StoryChoicePanel`·`StorySceneController`) | **A·B 병합 후** | A의 `PlayerTraitState`, B의 `StoryChoicePoint` |
| **D** | `feature/trait-consumers` | Task 4 (`TraitLineLogic` + 세 소비처) | **A·B 병합 후** (C와 병렬) | A의 로직, B의 `TraitLine`·`PlayerTraitLines` |
| **E** | `feature/trait-draft-data` | Task 5 (대본 초안 데이터 + 구조 테스트) | **B 병합 후** (C·D와 병렬) | B |
| **F** | `feature/trait-scene-wiring` | Task 6 (씬 배선, 사용자 에디터 작업) | **C·D·E 모두 병합 후** | 전부 |

이번 작업은 **삭제나 시그니처 변경이 전혀 없고 전부 추가**라서 원자적으로 묶을 PR이 없다. C·D·E는 서로 파일이 안 겹쳐 B 이후 동시에 진행할 수 있다. 성향이 `None`(기본)이면 어떤 PR이 먼저 병합돼도 게임 동작이 바뀌지 않는다.

---

## File Structure

| 파일 | 역할 |
|---|---|
| `unity/Assets/_Project/Scripts/Data/PlayerTrait.cs` (신규) | 성향 enum |
| `unity/Assets/_Project/Scripts/Core/PlayerTraitLogic.cs` (신규) | 대표 성향 계산 |
| `unity/Assets/_Project/Scripts/Core/PlayerTraitState.cs` (신규) | 고른 성향을 씬 전환을 넘어 보관 |
| `unity/Assets/_Project/Scripts/Core/AffinityChoiceOrderLogic.cs` (신규) | 추천 선택지 인덱스·표시 순서 |
| `unity/Assets/_Project/Scripts/Data/StoryDialogueData.cs` (수정) | `StoryChoice`·`StoryChoicePoint`·`choicePoints` |
| `unity/Assets/_Project/Scripts/Data/AffinityDialogueData.cs` (수정) | `AffinityDialogueChoice.trait`, `TraitLine`, `bossTraitLines` |
| `unity/Assets/_Project/Scripts/Data/PlayerTraitLines.cs` (신규) | 성향별 표시명·결과 한마디 데이터(Resources) |
| `unity/Assets/_Project/Scripts/Core/StoryChoiceLogic.cs` (신규) | 어느 줄 앞에 질문이 있는지 |
| `unity/Assets/_Project/Scripts/UI/StoryChoicePanel.cs` (신규) | 선택지 버튼 뷰 |
| `unity/Assets/_Project/Scripts/UI/StorySceneController.cs` (수정) | 질문 처리, 시작 시 성향 초기화 |
| `unity/Assets/_Project/Scripts/Core/TraitLineLogic.cs` (신규) | 보스 대사 끝에 성향 한 줄 붙이기 |
| `unity/Assets/_Project/Scripts/Core/AffinityDialogueController.cs` (수정) | 추천 선택지를 맨 위로 + ★ |
| `unity/Assets/_Project/Scripts/Core/BossFightDirector.cs` (수정) | 보스 등장 대사에 성향 한 줄 |
| `unity/Assets/_Project/Scripts/UI/ResultPanel.cs` (수정) | 성향 한마디 표시 |
| `unity/Assets/_Project/Data/WorldIntroStory.asset` (수정) | `choicePoints` 덧붙임 |
| `unity/Assets/_Project/Data/EggAffinityDialogue.asset`, `ShrimpAffinityDialogue.asset` (수정) | `bossTraitLines` 덧붙임 |
| `unity/Assets/_Project/Resources/PlayerTraitLines.asset` (신규) | 성향 3개 × (표시명, 승리·패배 한마디) |
| `unity/Assets/Tests/EditMode/*Tests.cs` (신규) | 각 태스크의 테스트 |
| `StoryScene.unity`·`GameScene.unity`·`BossScene.unity`·Egg/Shrimp 호감도 에셋 (수정, PR F) | 선택 UI·결과 텍스트·선택지 성향 지정 |

---

# PR A — 성향 상태와 순수 로직 (`main`에서 지금 시작)

### Task 1: `PlayerTrait`·`PlayerTraitState`·순수 로직 (TDD)

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git checkout -b feature/trait-state-logic
```

**Files:**
- Create: `unity/Assets/_Project/Scripts/Data/PlayerTrait.cs`
- Create: `unity/Assets/_Project/Scripts/Core/PlayerTraitLogic.cs`
- Create: `unity/Assets/_Project/Scripts/Core/PlayerTraitState.cs`
- Create: `unity/Assets/_Project/Scripts/Core/AffinityChoiceOrderLogic.cs`
- Test: `unity/Assets/Tests/EditMode/PlayerTraitLogicTests.cs`, `PlayerTraitStateTests.cs`, `AffinityChoiceOrderLogicTests.cs`

**Interfaces:**
- Produces (Task 2~5가 사용):
  - `SushiSurvival.Data.PlayerTrait { None = 0, Bold, Careful, Kind }`
  - `PlayerTraitLogic.Dominant(IReadOnlyList<PlayerTrait> picks)` → `PlayerTrait`
  - `PlayerTraitState.Picks`(`IReadOnlyList<PlayerTrait>`), `.Dominant`(`PlayerTrait`), `.Record(PlayerTrait)`, `.Reset()`
  - `AffinityChoiceOrderLogic.RecommendedIndex(IReadOnlyList<PlayerTrait> choiceTraits, PlayerTrait dominant)` → `int`(없으면 -1)
  - `AffinityChoiceOrderLogic.DisplayOrder(int count, int recommendedIndex)` → `int[]`

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/PlayerTraitLogicTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    public class PlayerTraitLogicTests
    {
        [Test]
        public void Dominant_EmptyList_IsNone()
        {
            Assert.AreEqual(PlayerTrait.None, PlayerTraitLogic.Dominant(new List<PlayerTrait>()));
        }

        [Test]
        public void Dominant_NullList_IsNone()
        {
            Assert.AreEqual(PlayerTrait.None, PlayerTraitLogic.Dominant(null));
        }

        [Test]
        public void Dominant_MostChosenWins()
        {
            var picks = new List<PlayerTrait> { PlayerTrait.Kind, PlayerTrait.Bold, PlayerTrait.Bold };
            Assert.AreEqual(PlayerTrait.Bold, PlayerTraitLogic.Dominant(picks));
        }

        [Test]
        public void Dominant_TwoWayTie_LatestPickWins()
        {
            var picks = new List<PlayerTrait> { PlayerTrait.Bold, PlayerTrait.Careful };
            Assert.AreEqual(PlayerTrait.Careful, PlayerTraitLogic.Dominant(picks));
        }

        [Test]
        public void Dominant_ThreeWayTie_LatestPickWins()
        {
            var picks = new List<PlayerTrait> { PlayerTrait.Bold, PlayerTrait.Careful, PlayerTrait.Kind };
            Assert.AreEqual(PlayerTrait.Kind, PlayerTraitLogic.Dominant(picks));
        }

        [Test]
        public void Dominant_IgnoresNone()
        {
            var picks = new List<PlayerTrait> { PlayerTrait.None, PlayerTrait.Bold, PlayerTrait.None };
            Assert.AreEqual(PlayerTrait.Bold, PlayerTraitLogic.Dominant(picks));

            var onlyNone = new List<PlayerTrait> { PlayerTrait.None, PlayerTrait.None };
            Assert.AreEqual(PlayerTrait.None, PlayerTraitLogic.Dominant(onlyNone));
        }
    }
}
```

`unity/Assets/Tests/EditMode/PlayerTraitStateTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    public class PlayerTraitStateTests
    {
        // 정적 상태라 테스트끼리 서로 영향을 주지 않도록 매번 비운다.
        [SetUp]
        public void SetUp() => PlayerTraitState.Reset();

        [TearDown]
        public void TearDown() => PlayerTraitState.Reset();

        [Test]
        public void Record_AddsPicksInOrder()
        {
            PlayerTraitState.Record(PlayerTrait.Bold);
            PlayerTraitState.Record(PlayerTrait.Kind);

            Assert.AreEqual(2, PlayerTraitState.Picks.Count);
            Assert.AreEqual(PlayerTrait.Bold, PlayerTraitState.Picks[0]);
            Assert.AreEqual(PlayerTrait.Kind, PlayerTraitState.Picks[1]);
        }

        [Test]
        public void Record_IgnoresNone()
        {
            PlayerTraitState.Record(PlayerTrait.None);

            Assert.AreEqual(0, PlayerTraitState.Picks.Count);
        }

        [Test]
        public void Dominant_UsesTheLogic()
        {
            PlayerTraitState.Record(PlayerTrait.Bold);
            PlayerTraitState.Record(PlayerTrait.Careful);

            // 동수라 가장 나중에 고른 성향.
            Assert.AreEqual(PlayerTrait.Careful, PlayerTraitState.Dominant);
        }

        [Test]
        public void Reset_ClearsPicks()
        {
            PlayerTraitState.Record(PlayerTrait.Bold);
            PlayerTraitState.Reset();

            Assert.AreEqual(0, PlayerTraitState.Picks.Count);
            Assert.AreEqual(PlayerTrait.None, PlayerTraitState.Dominant);
        }
    }
}
```

`unity/Assets/Tests/EditMode/AffinityChoiceOrderLogicTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    public class AffinityChoiceOrderLogicTests
    {
        private static readonly PlayerTrait[] Traits = { PlayerTrait.Bold, PlayerTrait.Careful, PlayerTrait.Kind };

        [Test]
        public void RecommendedIndex_ReturnsTheMatchingIndex()
        {
            Assert.AreEqual(2, AffinityChoiceOrderLogic.RecommendedIndex(Traits, PlayerTrait.Kind));
        }

        [Test]
        public void RecommendedIndex_NoMatch_ReturnsMinusOne()
        {
            var traits = new List<PlayerTrait> { PlayerTrait.Bold, PlayerTrait.Careful };
            Assert.AreEqual(-1, AffinityChoiceOrderLogic.RecommendedIndex(traits, PlayerTrait.Kind));
        }

        [Test]
        public void RecommendedIndex_DominantNone_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, AffinityChoiceOrderLogic.RecommendedIndex(Traits, PlayerTrait.None));
        }

        [Test]
        public void RecommendedIndex_DuplicateTraits_ReturnsTheFirst()
        {
            var traits = new List<PlayerTrait> { PlayerTrait.Kind, PlayerTrait.Bold, PlayerTrait.Bold };
            Assert.AreEqual(1, AffinityChoiceOrderLogic.RecommendedIndex(traits, PlayerTrait.Bold));
        }

        [Test]
        public void RecommendedIndex_NullTraits_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, AffinityChoiceOrderLogic.RecommendedIndex(null, PlayerTrait.Bold));
        }

        [Test]
        public void DisplayOrder_MovesRecommendedToFront_KeepingOthersInOrder()
        {
            Assert.AreEqual(new[] { 2, 0, 1 }, AffinityChoiceOrderLogic.DisplayOrder(3, 2));
            Assert.AreEqual(new[] { 1, 0, 2 }, AffinityChoiceOrderLogic.DisplayOrder(3, 1));
        }

        [Test]
        public void DisplayOrder_NoRecommendation_IsIdentity()
        {
            Assert.AreEqual(new[] { 0, 1, 2 }, AffinityChoiceOrderLogic.DisplayOrder(3, -1));
            Assert.AreEqual(new[] { 0, 1, 2 }, AffinityChoiceOrderLogic.DisplayOrder(3, 0));
        }

        [Test]
        public void DisplayOrder_IndexOutOfRange_IsIdentity()
        {
            Assert.AreEqual(new[] { 0, 1, 2 }, AffinityChoiceOrderLogic.DisplayOrder(3, 5));
        }

        [Test]
        public void DisplayOrder_ZeroCount_IsEmpty()
        {
            Assert.AreEqual(0, AffinityChoiceOrderLogic.DisplayOrder(0, 0).Length);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해 실패 확인**

"테스트 실행"을 돌린다. Expected: `error CS0246`/`CS0103` — 새 타입이 없어 컴파일 에러(정상, Step 3에서 해결).

- [ ] **Step 3: 구현**

`unity/Assets/_Project/Scripts/Data/PlayerTrait.cs`:

```csharp
namespace SushiSurvival.Data
{
    /// <summary>
    /// 세계관 대화에서 쌓이는 성향. None이 0이라, 이 필드가 새로 생겨도 기존 에셋은 자동으로 None이 된다.
    /// 화면에 보이는 이름(용감함·신중함·다정함)은 PlayerTraitLines 데이터에 있다.
    /// </summary>
    public enum PlayerTrait
    {
        None = 0,
        Bold,
        Careful,
        Kind
    }
}
```

`unity/Assets/_Project/Scripts/Core/PlayerTraitLogic.cs`:

```csharp
using System.Collections.Generic;
using SushiSurvival.Data;

namespace SushiSurvival.Core
{
    /// <summary>고른 성향들에서 대표 성향을 뽑는다.</summary>
    public static class PlayerTraitLogic
    {
        /// <summary>
        /// 가장 많이 고른 성향. 동수면 picks에서 가장 뒤에 나온(가장 나중에 고른) 성향이 이긴다 —
        /// 질문이 2번뿐이라 1대1 동률이 흔한데, 마지막 답이 이기는 쪽이 자연스럽다. None은 세지 않는다.
        /// </summary>
        public static PlayerTrait Dominant(IReadOnlyList<PlayerTrait> picks)
        {
            if (picks == null || picks.Count == 0) return PlayerTrait.None;

            var counts = new Dictionary<PlayerTrait, int>();
            int max = 0;

            foreach (PlayerTrait pick in picks)
            {
                if (pick == PlayerTrait.None) continue;

                counts.TryGetValue(pick, out int count);
                counts[pick] = ++count;
                if (count > max) max = count;
            }

            if (max == 0) return PlayerTrait.None;

            for (int i = picks.Count - 1; i >= 0; i--)
            {
                if (picks[i] != PlayerTrait.None && counts[picks[i]] == max)
                    return picks[i];
            }

            return PlayerTrait.None;
        }
    }
}
```

`unity/Assets/_Project/Scripts/Core/PlayerTraitState.cs`:

```csharp
using System.Collections.Generic;
using SushiSurvival.Data;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 세계관 대화에서 고른 성향을 씬 전환을 넘어 들고 있는다(RunResultCarrier와 같은 방식).
    /// StorySceneController가 시작할 때 Reset하고, 호감도 대화·보스 대사·결과 화면이 Dominant를 읽는다.
    /// 건너뛰기나 에디터에서 GameScene으로 직행하면 비어 있어 Dominant가 None이고, 그러면 모두 기존 동작이다.
    /// </summary>
    public static class PlayerTraitState
    {
        private static readonly List<PlayerTrait> _picks = new List<PlayerTrait>();

        public static IReadOnlyList<PlayerTrait> Picks => _picks;

        public static PlayerTrait Dominant => PlayerTraitLogic.Dominant(_picks);

        public static void Record(PlayerTrait trait)
        {
            if (trait == PlayerTrait.None) return;

            _picks.Add(trait);
        }

        public static void Reset() => _picks.Clear();
    }
}
```

`unity/Assets/_Project/Scripts/Core/AffinityChoiceOrderLogic.cs`:

```csharp
using System.Collections.Generic;
using SushiSurvival.Data;

namespace SushiSurvival.Core
{
    /// <summary>호감도 대화에서 대표 성향에 맞는 선택지를 맨 위로 올리는 순서 계산.</summary>
    public static class AffinityChoiceOrderLogic
    {
        /// <summary>대표 성향과 같은 첫 선택지의 인덱스. 없거나 성향이 None이면 -1.</summary>
        public static int RecommendedIndex(IReadOnlyList<PlayerTrait> choiceTraits, PlayerTrait dominant)
        {
            if (choiceTraits == null || dominant == PlayerTrait.None) return -1;

            for (int i = 0; i < choiceTraits.Count; i++)
            {
                if (choiceTraits[i] == dominant) return i;
            }

            return -1;
        }

        /// <summary>추천 인덱스를 맨 앞으로 옮긴 표시 순서(원래 인덱스 배열). 나머지는 원래 상대 순서를 지킨다.</summary>
        public static int[] DisplayOrder(int count, int recommendedIndex)
        {
            int size = count < 0 ? 0 : count;
            var order = new int[size];

            if (recommendedIndex < 0 || recommendedIndex >= size)
            {
                for (int i = 0; i < size; i++) order[i] = i;
                return order;
            }

            order[0] = recommendedIndex;

            int next = 1;
            for (int i = 0; i < size; i++)
            {
                if (i == recommendedIndex) continue;
                order[next++] = i;
            }

            return order;
        }
    }
}
```

- [ ] **Step 4: 테스트 실행해 통과 확인**

"테스트 실행"을 돌린다. Expected: `error CS` 없음, baseline + 19(6 + 4 + 9), 실패는 알려진 기존 1건뿐. 끝나면 `.meta` 확인:

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git status --short unity/Assets/_Project/Scripts/Data/PlayerTrait.cs.meta unity/Assets/_Project/Scripts/Core/PlayerTraitLogic.cs.meta unity/Assets/_Project/Scripts/Core/PlayerTraitState.cs.meta unity/Assets/_Project/Scripts/Core/AffinityChoiceOrderLogic.cs.meta unity/Assets/Tests/EditMode/PlayerTraitLogicTests.cs.meta unity/Assets/Tests/EditMode/PlayerTraitStateTests.cs.meta unity/Assets/Tests/EditMode/AffinityChoiceOrderLogicTests.cs.meta
```

- [ ] **Step 5: 커밋, 푸시, PR A**

이 계획서 파일(`docs/superpowers/plans/2026-09-28-story-choice-traits.md`)이 아직 커밋 전이면 이 PR에 같이 넣는다.

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/Data/PlayerTrait.cs unity/Assets/_Project/Scripts/Data/PlayerTrait.cs.meta unity/Assets/_Project/Scripts/Core/PlayerTraitLogic.cs unity/Assets/_Project/Scripts/Core/PlayerTraitLogic.cs.meta unity/Assets/_Project/Scripts/Core/PlayerTraitState.cs unity/Assets/_Project/Scripts/Core/PlayerTraitState.cs.meta unity/Assets/_Project/Scripts/Core/AffinityChoiceOrderLogic.cs unity/Assets/_Project/Scripts/Core/AffinityChoiceOrderLogic.cs.meta unity/Assets/Tests/EditMode/PlayerTraitLogicTests.cs unity/Assets/Tests/EditMode/PlayerTraitLogicTests.cs.meta unity/Assets/Tests/EditMode/PlayerTraitStateTests.cs unity/Assets/Tests/EditMode/PlayerTraitStateTests.cs.meta unity/Assets/Tests/EditMode/AffinityChoiceOrderLogicTests.cs unity/Assets/Tests/EditMode/AffinityChoiceOrderLogicTests.cs.meta docs/superpowers/plans/2026-09-28-story-choice-traits.md
git commit -m "$(cat <<'EOF'
feat: 성향 태그 상태와 순수 로직(PlayerTrait·PlayerTraitState)

대표 성향(최다, 동수면 가장 나중에 고른 것)과 호감도 선택지 추천 순서를
계산하는 순수 함수, 씬 전환을 넘어 성향을 들고 있는 static 상태를 추가한다.
아직 아무도 안 불러 게임 동작은 바뀌지 않는다. 구현 계획서도 같이 커밋.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/trait-state-logic
gh pr create --title "feat: 성향 태그 상태·순수 로직 (성향 태그 A)" --body "$(cat <<'EOF'
## 요약
세계관 대화 선택 → 성향 태그 작업의 첫 조각. 성향 enum(`PlayerTrait`: 용감함·신중함·다정함), 대표 성향을 뽑는 `PlayerTraitLogic`(최다, 동수면 가장 나중에 고른 것), 씬 전환을 넘어 보관하는 `PlayerTraitState`, 호감도 선택지 추천 순서를 계산하는 `AffinityChoiceOrderLogic`을 추가했습니다. 아직 아무도 안 불러서 게임 동작은 바뀌지 않습니다.

- 스펙: `docs/superpowers/specs/2026-09-28-story-choice-traits-design.md`
- 계획: `docs/superpowers/plans/2026-09-28-story-choice-traits.md` (PR A)

## 테스트
- EditMode baseline + 19 통과 (알려진 기존 실패 1건 제외)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

---

# PR B — 데이터 스키마 (A 병합 후)

### Task 2: 질문 구조·선택지 성향·보스 성향 줄·결과 한마디 데이터

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git log --oneline -3   # A 병합 커밋이 보여야 한다
git checkout -b feature/trait-data-schema
```

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Data/StoryDialogueData.cs`
- Modify: `unity/Assets/_Project/Scripts/Data/AffinityDialogueData.cs`
- Create: `unity/Assets/_Project/Scripts/Data/PlayerTraitLines.cs`
- Test: `unity/Assets/Tests/EditMode/PlayerTraitLinesTests.cs`

**Interfaces:**
- Consumes (A): `PlayerTrait`
- Produces (Task 3·4·5가 사용):
  - `StoryChoice { string choiceText; PlayerTrait trait; StoryLine reply; }`
  - `StoryChoicePoint { int beforeLineIndex; StoryLine prompt; StoryChoice[] choices; }`, `StoryDialogueData.choicePoints`
  - `AffinityDialogueChoice.trait`(`PlayerTrait`)
  - `TraitLine { PlayerTrait trait; StoryLine line; }`, `AffinityDialogueData.bossTraitLines`(`TraitLine[]`)
  - `PlayerTraitEntry { PlayerTrait trait; string displayName; string victoryLine; string defeatLine; }`
  - `PlayerTraitLines.entries`, `PlayerTraitLines.Find(PlayerTrait)` → `PlayerTraitEntry`(없으면 `null`), `PlayerTraitLines.Load()` → `PlayerTraitLines`(`Resources`, 없으면 `null`)

필드를 **추가만** 하므로 기존 에셋은 그대로 열린다(새 필드는 기본값). `StoryLine`은 건드리지 않는다 — `StoryLine`이 자기 자신을 품으면 Unity 직렬화가 깨진다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/PlayerTraitLinesTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    public class PlayerTraitLinesTests
    {
        private PlayerTraitLines _lines;

        [SetUp]
        public void SetUp()
        {
            _lines = ScriptableObject.CreateInstance<PlayerTraitLines>();
            _lines.entries = new[]
            {
                new PlayerTraitEntry { trait = PlayerTrait.Bold, displayName = "용감함", victoryLine = "v", defeatLine = "d" },
                null,
                new PlayerTraitEntry { trait = PlayerTrait.Kind, displayName = "다정함", victoryLine = "v", defeatLine = "d" },
            };
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_lines);

        [Test]
        public void Find_ReturnsTheMatchingEntry()
        {
            Assert.AreEqual("다정함", _lines.Find(PlayerTrait.Kind).displayName);
        }

        [Test]
        public void Find_None_ReturnsNull()
        {
            Assert.IsNull(_lines.Find(PlayerTrait.None));
        }

        [Test]
        public void Find_MissingTrait_ReturnsNull()
        {
            Assert.IsNull(_lines.Find(PlayerTrait.Careful));
        }

        [Test]
        public void Find_NullEntries_ReturnsNull()
        {
            _lines.entries = null;
            Assert.IsNull(_lines.Find(PlayerTrait.Bold));
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해 실패 확인**

"테스트 실행"을 돌린다. Expected: `error CS0246` — `PlayerTraitLines`·`PlayerTraitEntry`가 없어 컴파일 에러(정상).

- [ ] **Step 3: 구현**

`unity/Assets/_Project/Scripts/Data/StoryDialogueData.cs` 전체를:

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
        [Tooltip("화면에 크게 서 있는 입상 일러스트. 비우면 입상을 숨긴다.")]
        public Sprite standing;
        [TextArea]
        public string text;
        [Tooltip("이 줄에서 바뀔 배경. 비우면 직전 배경을 유지한다.")]
        public Sprite background;
    }

    /// <summary>질문의 선택지 하나. 고르면 성향이 쌓이고 반응 대사 한 줄이 나온다.</summary>
    [Serializable]
    public class StoryChoice
    {
        [TextArea]
        [Tooltip("버튼에 표시할 문구.")]
        public string choiceText;
        [Tooltip("이 선택이 쌓는 성향.")]
        public PlayerTrait trait;
        [Tooltip("고른 직후 나오는 반응 대사 한 줄. 비우면 반응 없이 바로 다음 대사로 넘어간다.")]
        public StoryLine reply;
    }

    /// <summary>
    /// 선형 대화 중간에 끼는 질문 하나. StoryLine이 자기 자신을 품으면 Unity 직렬화가 깨져서
    /// 질문은 이렇게 별도 클래스로 둔다.
    /// </summary>
    [Serializable]
    public class StoryChoicePoint
    {
        [Tooltip("이 번호의 대사 줄 바로 앞에 질문을 끼운다(0부터). 같은 번호에 여러 개면 첫 번째만 나온다.")]
        public int beforeLineIndex;
        [Tooltip("질문 대사. 화자·초상화·입상을 지정할 수 있다.")]
        public StoryLine prompt;
        [Tooltip("2~3개.")]
        public StoryChoice[] choices;
    }

    /// <summary>선형 대화 한 세트. StorySceneController가 순서대로 넘긴다.</summary>
    [CreateAssetMenu(menuName = "SushiSurvival/Story Dialogue Data", fileName = "NewStoryDialogueData")]
    public class StoryDialogueData : ScriptableObject
    {
        public StoryLine[] lines;
        [Tooltip("대화 중간에 끼는 질문. 비우면 질문 없이 선형으로만 재생한다.")]
        public StoryChoicePoint[] choicePoints;
    }
}
```

`unity/Assets/_Project/Scripts/Data/AffinityDialogueData.cs` 전체를:

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
        [Tooltip("이 선택지에 대응하는 성향. 세계관 대화에서 쌓인 대표 성향과 같으면 맨 위로 올라가고 ★가 붙는다. None이면 영향 없음.")]
        public PlayerTrait trait;
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

    /// <summary>성향별로 다르게 재생되는 대사 한 줄.</summary>
    [System.Serializable]
    public class TraitLine
    {
        public PlayerTrait trait;
        public StoryLine line;
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
        [Tooltip("BossScene에서 보스가 떨어져 등장한 직후 보스와 캐릭터가 주고받는 대사. 비우면 대화 없이 바로 전투.")]
        public StoryLine[] bossEncounterLines;
        [Tooltip("bossEncounterLines 끝에 붙는, 세계관 대화에서 쌓인 대표 성향별 대사 한 줄. 해당 성향의 줄이 없으면 안 붙는다.")]
        public TraitLine[] bossTraitLines;
    }
}
```

`unity/Assets/_Project/Scripts/Data/PlayerTraitLines.cs`:

```csharp
using System;
using UnityEngine;

namespace SushiSurvival.Data
{
    [Serializable]
    public class PlayerTraitEntry
    {
        public PlayerTrait trait;
        [Tooltip("화면에 보이는 성향 이름(용감함 등).")]
        public string displayName;
        [TextArea] public string victoryLine;
        [TextArea] public string defeatLine;
    }

    /// <summary>
    /// 성향별 표시명과 결과 화면 한마디. Resources 폴더에 두어 씬 배선 없이 어디서든 불러 쓴다
    /// (BoothResetSettings와 같은 방식).
    /// </summary>
    [CreateAssetMenu(menuName = "SushiSurvival/Player Trait Lines", fileName = "PlayerTraitLines")]
    public class PlayerTraitLines : ScriptableObject
    {
        public const string ResourcePath = "PlayerTraitLines";

        public PlayerTraitEntry[] entries;

        /// <summary>해당 성향의 항목. None이거나 없으면 null.</summary>
        public PlayerTraitEntry Find(PlayerTrait trait)
        {
            if (trait == PlayerTrait.None || entries == null) return null;

            foreach (PlayerTraitEntry entry in entries)
            {
                if (entry != null && entry.trait == trait) return entry;
            }

            return null;
        }

        /// <summary>Resources에서 불러온다. 에셋이 없으면 null.</summary>
        public static PlayerTraitLines Load() => Resources.Load<PlayerTraitLines>(ResourcePath);
    }
}
```

- [ ] **Step 4: 테스트 실행해 통과 확인**

"테스트 실행"을 돌린다. Expected: `error CS` 없음, baseline + 4, 실패는 알려진 기존 1건뿐(기존 `WorldIntroStoryAssetTests` 등이 새 필드 추가에도 그대로 통과해야 한다). `.meta` 확인:

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git status --short unity/Assets/_Project/Scripts/Data/PlayerTraitLines.cs.meta unity/Assets/Tests/EditMode/PlayerTraitLinesTests.cs.meta
```

- [ ] **Step 5: 커밋, 푸시, PR B**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/Data/StoryDialogueData.cs unity/Assets/_Project/Scripts/Data/AffinityDialogueData.cs unity/Assets/_Project/Scripts/Data/PlayerTraitLines.cs unity/Assets/_Project/Scripts/Data/PlayerTraitLines.cs.meta unity/Assets/Tests/EditMode/PlayerTraitLinesTests.cs unity/Assets/Tests/EditMode/PlayerTraitLinesTests.cs.meta
git commit -m "$(cat <<'EOF'
feat: 성향 태그용 데이터 스키마 추가

StoryDialogueData.choicePoints(스토리 중간 질문), AffinityDialogueChoice.trait,
AffinityDialogueData.bossTraitLines, PlayerTraitLines(성향별 표시명·결과
한마디, Resources)를 추가한다. 필드 추가만이라 기존 에셋은 그대로 열리고
아직 읽는 코드가 없어 게임 동작은 바뀌지 않는다.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/trait-data-schema
gh pr create --title "feat: 성향 태그 데이터 스키마 (성향 태그 B)" --body "$(cat <<'EOF'
## 요약
성향 태그 작업의 데이터 스키마. 스토리 중간 질문(`StoryDialogueData.choicePoints`), 호감도 선택지의 성향(`AffinityDialogueChoice.trait`), 보스 등장 대사 뒤에 붙는 성향별 한 줄(`AffinityDialogueData.bossTraitLines`), 결과 화면 한마디 데이터(`PlayerTraitLines`, Resources)를 추가했습니다. **필드 추가만** 있어서 기존 에셋은 그대로 열리고(새 필드는 기본값), 아직 이 필드를 읽는 코드가 없어 게임 동작은 바뀌지 않습니다. `StoryLine`은 안 건드렸습니다(자기 자신을 품으면 Unity 직렬화가 깨짐).

- 스펙: `docs/superpowers/specs/2026-09-28-story-choice-traits-design.md`
- 계획: `docs/superpowers/plans/2026-09-28-story-choice-traits.md` (PR B)
- 선행 PR: A(`PlayerTrait`)

## 협업자 확인 부탁
`StoryDialogueData.cs`·`AffinityDialogueData.cs`(대화 데이터 구조)에 필드가 추가됩니다. 기존 필드는 안 건드립니다.

## 테스트
- EditMode baseline + 4 통과 (알려진 기존 실패 1건 제외)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

---

# PR C — 스토리 질문 처리 (A·B 병합 후)

### Task 3: `StoryChoiceLogic` + `StoryChoicePanel` + `StorySceneController`

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git log --oneline -4   # A, B 병합 커밋이 보여야 한다
git checkout -b feature/story-choice-scene
```

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/StoryChoiceLogic.cs`
- Create: `unity/Assets/_Project/Scripts/UI/StoryChoicePanel.cs`
- Modify: `unity/Assets/_Project/Scripts/UI/StorySceneController.cs`
- Test: `unity/Assets/Tests/EditMode/StoryChoiceLogicTests.cs`

**Interfaces:**
- Consumes (A): `PlayerTraitState.Reset()`, `PlayerTraitState.Record(PlayerTrait)`
- Consumes (B): `StoryChoicePoint`, `StoryChoice`, `StoryDialogueData.choicePoints`
- Produces (Task 6이 씬에 붙임): `StoryChoicePanel`(공개 `MonoBehaviour`) — 직렬화 필드 `root`(선택), `buttons`(`Button[]`); `StorySceneController`의 새 직렬화 필드 `choicePanel`

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/StoryChoiceLogicTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    public class StoryChoiceLogicTests
    {
        private static StoryChoicePoint Point(int beforeLineIndex)
            => new StoryChoicePoint { beforeLineIndex = beforeLineIndex };

        [Test]
        public void FindPointIndex_ReturnsThePointBeforeThatLine()
        {
            var points = new[] { Point(4), Point(8) };

            Assert.AreEqual(0, StoryChoiceLogic.FindPointIndex(points, 4));
            Assert.AreEqual(1, StoryChoiceLogic.FindPointIndex(points, 8));
        }

        [Test]
        public void FindPointIndex_NoPointForThatLine_ReturnsMinusOne()
        {
            var points = new[] { Point(4), Point(8) };

            Assert.AreEqual(-1, StoryChoiceLogic.FindPointIndex(points, 5));
        }

        [Test]
        public void FindPointIndex_SeveralPointsOnTheSameLine_ReturnsTheFirst()
        {
            var points = new[] { Point(3), Point(4), Point(4) };

            Assert.AreEqual(1, StoryChoiceLogic.FindPointIndex(points, 4));
        }

        [Test]
        public void FindPointIndex_EmptyList_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, StoryChoiceLogic.FindPointIndex(new StoryChoicePoint[0], 0));
        }

        [Test]
        public void FindPointIndex_NullList_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, StoryChoiceLogic.FindPointIndex(null, 0));
        }

        [Test]
        public void FindPointIndex_SkipsNullEntries()
        {
            var points = new[] { null, Point(4) };

            Assert.AreEqual(1, StoryChoiceLogic.FindPointIndex(points, 4));
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해 실패 확인**

"테스트 실행"을 돌린다. Expected: `error CS0103` — `StoryChoiceLogic`이 없어 컴파일 에러(정상).

- [ ] **Step 3: 순수 로직 구현**

`unity/Assets/_Project/Scripts/Core/StoryChoiceLogic.cs`:

```csharp
using System.Collections.Generic;
using SushiSurvival.Data;

namespace SushiSurvival.Core
{
    /// <summary>선형 대화 중 어느 줄 앞에 질문이 끼는지 찾는다.</summary>
    public static class StoryChoiceLogic
    {
        /// <summary>beforeLineIndex가 lineIndex와 같은 첫 질문의 인덱스. 없으면 -1.</summary>
        public static int FindPointIndex(IReadOnlyList<StoryChoicePoint> points, int lineIndex)
        {
            if (points == null) return -1;

            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] != null && points[i].beforeLineIndex == lineIndex)
                    return i;
            }

            return -1;
        }
    }
}
```

- [ ] **Step 4: `StoryChoicePanel.cs` 작성**

`unity/Assets/_Project/Scripts/UI/StoryChoicePanel.cs`:

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 스토리 중간 질문의 선택지 버튼들. Legacy Button 최대 3개를 쓰고, 각 버튼의 자식 Text에 문구를 넣는다.
    /// 씬에서 켜져 있든 꺼져 있든 상관없다 — 첫 Show가 켜면서 버튼 리스너가 붙는다.
    /// </summary>
    public class StoryChoicePanel : MonoBehaviour
    {
        [Tooltip("패널 루트. 비워두면 이 오브젝트 자신을 켜고 끈다.")]
        [SerializeField] private GameObject root;
        [Tooltip("선택지 버튼 최대 3개. 각 버튼의 자식 Text에 문구가 들어간다.")]
        [SerializeField] private Button[] buttons;

        private Action<int> _onChosen;

        private GameObject Root => root != null ? root : gameObject;

        private void Awake()
        {
            if (buttons == null) return;

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;

                int index = i;
                buttons[i].onClick.AddListener(() => HandleClicked(index));
            }
        }

        public void Show(StoryChoice[] choices, Action<int> onChosen)
        {
            if (buttons == null || buttons.Length == 0)
            {
                Debug.LogError($"{name}: buttons가 비어 있어 선택지를 표시할 수 없습니다.");
                return;
            }

            _onChosen = onChosen;
            Root.SetActive(true);

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;

                bool used = choices != null && i < choices.Length && choices[i] != null;
                buttons[i].gameObject.SetActive(used);
                if (!used) continue;

                Text label = buttons[i].GetComponentInChildren<Text>();
                if (label != null) label.text = choices[i].choiceText;
            }
        }

        public void Hide()
        {
            _onChosen = null;
            Root.SetActive(false);
        }

        private void HandleClicked(int index)
        {
            Action<int> callback = _onChosen;
            if (callback == null) return;

            // 같은 클릭이 두 번 처리되지 않게 먼저 비운다.
            _onChosen = null;
            callback(index);
        }
    }
}
```

- [ ] **Step 5: `StorySceneController.cs` 교체**

`unity/Assets/_Project/Scripts/UI/StorySceneController.cs` 전체를:

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
    ///
    /// 대화 중간에 질문(choicePoints)이 끼면 질문 대사 → 선택지 → 고른 성향 기록 → 반응 대사 한 줄을
    /// 거쳐 원래 대사로 돌아온다. choicePanel이 비어 있거나 질문이 없으면 기존 선형 재생과 같다.
    /// </summary>
    public class StorySceneController : MonoBehaviour
    {
        [SerializeField] private StoryDialogueData data;
        [SerializeField] private StoryPanel panel;
        [Tooltip("질문 선택지를 보여주는 패널. 비워두면 질문 없이 선형으로만 재생한다.")]
        [SerializeField] private StoryChoicePanel choicePanel;
        [SerializeField] private Button skipButton;
        [Tooltip("대화가 끝나거나 건너뛰면 이동할 씬. Build Settings 등록명과 정확히 같아야 한다.")]
        [SerializeField] private string nextSceneName = "GameScene";
        [Tooltip("배경 전환 페이드 시간(초, 실시간).")]
        [SerializeField] private float backgroundFadeSeconds = 0.35f;

        private bool[] _hasBackground;
        private bool[] _pointDone;
        private int _index;
        private bool _leaving;
        private bool _awaitingChoice;
        private bool _showingReply;
        private int _choiceFrame = -1;
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
            // 판마다 처음부터. "다시 하기"는 IntroScene → StoryScene을 거치므로 여기서 비우면 된다.
            PlayerTraitState.Reset();

            // 결과 화면 등에서 정지된 채 넘어오는 경우를 막는다.
            Time.timeScale = 1f;

            if (choicePanel != null)
                choicePanel.Hide();

            if (data == null || data.lines == null || data.lines.Length == 0 || panel == null)
            {
                Debug.LogWarning($"{name}: 대화 데이터나 패널이 비어 있어 스토리를 건너뜁니다.");
                Leave();
                return;
            }

            _hasBackground = new bool[data.lines.Length];
            for (int i = 0; i < data.lines.Length; i++)
                _hasBackground[i] = data.lines[i].background != null;

            _pointDone = new bool[data.choicePoints != null ? data.choicePoints.Length : 0];

            _index = 0;
            ShowCurrent(immediateBackground: true);
        }

        private void Update()
        {
            if (_leaving || _hasBackground == null) return;

            // 선택 중에는 클릭·Space·Enter로 넘어가지 않는다. 버튼으로만 진행한다.
            if (_awaitingChoice) return;

            // 선택 버튼을 누른 같은 프레임의 마우스 떼기가 "다음 줄" 입력으로도 세어지지 않게 한다.
            // EventSystem과 이 Update의 실행 순서가 어느 쪽이어도 안전하도록 프레임으로 막는다.
            if (_choiceFrame == Time.frameCount) return;

            if (!AdvancePressed()) return;

            // 아직 찍히는 중이면 다음 줄로 넘기지 않고 전체 문장부터 보여준다.
            if (panel.IsTyping)
            {
                panel.CompleteTyping();
                return;
            }

            // 반응 대사를 다 본 뒤엔 원래 대사 줄로 돌아온다.
            if (_showingReply)
            {
                _showingReply = false;
                ShowLineAt(_index, immediateBackground: false);
                return;
            }

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

        // 지금 줄 앞에 아직 안 한 질문이 있으면 그것부터, 없으면 대사 줄을 보여준다.
        private void ShowCurrent(bool immediateBackground)
        {
            int pointIndex = FindPendingPoint();
            if (pointIndex >= 0)
            {
                // 첫 화면이 질문이어도 깔려 있어야 할 배경은 바로 둔다.
                if (immediateBackground)
                    ApplyImmediateBackground();

                BeginChoice(pointIndex);
                return;
            }

            ShowLineAt(_index, immediateBackground);
        }

        private int FindPendingPoint()
        {
            if (choicePanel == null || data.choicePoints == null) return -1;

            int pointIndex = StoryChoiceLogic.FindPointIndex(data.choicePoints, _index);
            return pointIndex >= 0 && !_pointDone[pointIndex] ? pointIndex : -1;
        }

        private void BeginChoice(int pointIndex)
        {
            StoryChoicePoint point = data.choicePoints[pointIndex];
            _pointDone[pointIndex] = true;

            if (point.choices == null || point.choices.Length == 0)
            {
                ShowLineAt(_index, immediateBackground: false);
                return;
            }

            _awaitingChoice = true;

            if (point.prompt != null)
                panel.ShowLine(point.prompt);

            choicePanel.Show(point.choices, choiceIndex => OnChoiceMade(point, choiceIndex));
        }

        private void OnChoiceMade(StoryChoicePoint point, int choiceIndex)
        {
            StoryChoice choice = point.choices[choiceIndex];
            PlayerTraitState.Record(choice.trait);

            choicePanel.Hide();
            _awaitingChoice = false;
            _choiceFrame = Time.frameCount;

            bool hasReply = choice.reply != null && !string.IsNullOrEmpty(choice.reply.text);
            if (hasReply)
            {
                _showingReply = true;
                panel.ShowLine(choice.reply);
            }
            else
            {
                ShowLineAt(_index, immediateBackground: false);
            }
        }

        private void ShowLineAt(int index, bool immediateBackground)
        {
            StoryLine line = data.lines[index];
            panel.ShowLine(line);

            if (immediateBackground)
            {
                ApplyImmediateBackground();
            }
            else if (StoryDialogueLogic.IsBackgroundChange(_hasBackground, index))
            {
                panel.FadeToBackground(line.background, backgroundFadeSeconds);
            }
        }

        // 첫 화면은 페이드 없이, 지금 깔려 있어야 할 배경(없으면 null=단색)을 바로 둔다.
        private void ApplyImmediateBackground()
        {
            int backgroundLine = StoryDialogueLogic.ResolveBackgroundIndex(_hasBackground, _index);
            panel.SetBackground(backgroundLine >= 0 ? data.lines[backgroundLine].background : null);
        }

        // 누르는 순간(wasPressedThisFrame)이 아니라 떼는 순간을 본다. 마지막 줄에서
        // 넘기면 그 자리에서 다음 씬(주로 GameScene)이 로드되는데, 누르는 순간에
        // 반응하면 그 시점엔 버튼이 아직 물리적으로 눌린 상태라 새로 생긴
        // EventSystem이 그 위치의 UI(캐릭터 선택 버튼 등)에 유령 클릭을 일으킨다.
        private bool AdvancePressed()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasReleasedThisFrame && !IsPointerOnSkipButton(mouse))
                return true;

            Keyboard keyboard = Keyboard.current;
            return keyboard != null
                && (keyboard.spaceKey.wasReleasedThisFrame || keyboard.enterKey.wasReleasedThisFrame);
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

- [ ] **Step 6: 테스트 실행해 통과 확인**

"테스트 실행"을 돌린다. Expected: `error CS` 없음, baseline + 6, 실패는 알려진 기존 1건뿐. `.meta` 확인:

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git status --short unity/Assets/_Project/Scripts/Core/StoryChoiceLogic.cs.meta unity/Assets/_Project/Scripts/UI/StoryChoicePanel.cs.meta unity/Assets/Tests/EditMode/StoryChoiceLogicTests.cs.meta
```

- [ ] **Step 7: 커밋, 푸시, PR C**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/Core/StoryChoiceLogic.cs unity/Assets/_Project/Scripts/Core/StoryChoiceLogic.cs.meta unity/Assets/_Project/Scripts/UI/StoryChoicePanel.cs unity/Assets/_Project/Scripts/UI/StoryChoicePanel.cs.meta unity/Assets/_Project/Scripts/UI/StorySceneController.cs unity/Assets/Tests/EditMode/StoryChoiceLogicTests.cs unity/Assets/Tests/EditMode/StoryChoiceLogicTests.cs.meta
git commit -m "$(cat <<'EOF'
feat: 스토리 중간 질문(선택지)과 성향 기록 처리

StorySceneController가 choicePoints의 질문을 대사 줄 앞에 끼워 재생하고,
고른 선택지의 성향을 PlayerTraitState에 기록한 뒤 반응 대사 한 줄을 보여준다.
choicePanel이 비어 있거나 질문이 없으면 기존 선형 재생과 같다.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/story-choice-scene
gh pr create --title "feat: 스토리 질문 처리 (성향 태그 C)" --body "$(cat <<'EOF'
## 요약
세계관 대화 중간에 질문을 끼우는 처리. `StorySceneController`가 `choicePoints`의 질문을 해당 대사 줄 앞에 재생하고, 새 `StoryChoicePanel`의 버튼으로 답을 받으면 성향을 `PlayerTraitState`에 기록한 뒤 반응 대사 한 줄을 보여주고 원래 대사로 돌아옵니다. 시작할 때 성향을 초기화합니다. **`choicePanel` 필드가 비어 있거나 `choicePoints`가 없으면 지금과 완전히 같은 선형 재생**이라, 씬 배선(PR F) 전에는 게임 동작이 바뀌지 않습니다.

- 스펙: `docs/superpowers/specs/2026-09-28-story-choice-traits-design.md`
- 계획: `docs/superpowers/plans/2026-09-28-story-choice-traits.md` (PR C)
- 선행 PR: A(`PlayerTraitState`), B(`StoryChoicePoint`) — D·E와는 독립이라 병렬로 올립니다.

## 신경 쓴 점
- 선택 대기 중에는 클릭·Space·Enter로 넘어가지 않고, 선택 버튼을 누른 같은 프레임의 마우스 떼기가 "다음 줄" 입력으로도 세어지지 않게 프레임으로 막았습니다(건너뛰기 버튼 때 겪은 유령 클릭과 같은 유형).
- 건너뛰기 버튼은 선택 대기 중에도 동작하고, 그러면 성향은 `None`으로 남습니다.

## 협업자 확인 부탁
`StorySceneController.cs`(스토리 씬 흐름)를 수정합니다.

## 테스트
- EditMode baseline + 6 통과 (알려진 기존 실패 1건 제외, MonoBehaviour는 컴파일 확인)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

---

# PR D — 성향 반영 세 곳 (A·B 병합 후, C와 병렬)

### Task 4: `TraitLineLogic` + 호감도 컨트롤러·`BossFightDirector`·`ResultPanel`

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git checkout -b feature/trait-consumers
```

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/TraitLineLogic.cs`
- Modify: `unity/Assets/_Project/Scripts/Core/AffinityDialogueController.cs`
- Modify: `unity/Assets/_Project/Scripts/Core/BossFightDirector.cs`
- Modify: `unity/Assets/_Project/Scripts/UI/ResultPanel.cs`
- Test: `unity/Assets/Tests/EditMode/TraitLineLogicTests.cs`

**Interfaces:**
- Consumes (A): `PlayerTraitState.Dominant`, `AffinityChoiceOrderLogic.RecommendedIndex/DisplayOrder`
- Consumes (B): `TraitLine`, `AffinityDialogueData.bossTraitLines`, `AffinityDialogueChoice.trait`, `PlayerTraitLines.Load()/Find()`, `PlayerTraitEntry`
- Produces (Task 6이 씬에서 연결): `ResultPanel`의 새 직렬화 필드 `traitText`(`Text`, 선택)
- `TraitLineLogic.AppendTraitLine(StoryLine[] baseLines, IReadOnlyList<TraitLine> traitLines, PlayerTrait dominant)` → `StoryLine[]`

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/TraitLineLogicTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    public class TraitLineLogicTests
    {
        private static StoryLine Line(string text) => new StoryLine { text = text };

        private static TraitLine Trait(PlayerTrait trait, string text)
            => new TraitLine { trait = trait, line = Line(text) };

        [Test]
        public void AppendsTheDominantTraitLineAtTheEnd()
        {
            var baseLines = new[] { Line("a"), Line("b") };
            var traitLines = new[] { Trait(PlayerTrait.Bold, "bold"), Trait(PlayerTrait.Kind, "kind") };

            StoryLine[] result = TraitLineLogic.AppendTraitLine(baseLines, traitLines, PlayerTrait.Kind);

            Assert.AreEqual(3, result.Length);
            Assert.AreEqual("kind", result[2].text);
            Assert.AreEqual("a", result[0].text);
        }

        [Test]
        public void DoesNotChangeTheOriginalArray()
        {
            var baseLines = new[] { Line("a") };

            TraitLineLogic.AppendTraitLine(baseLines, new[] { Trait(PlayerTrait.Bold, "bold") }, PlayerTrait.Bold);

            Assert.AreEqual(1, baseLines.Length);
        }

        [Test]
        public void NoMatchingTrait_ReturnsTheSameArray()
        {
            var baseLines = new[] { Line("a") };

            StoryLine[] result = TraitLineLogic.AppendTraitLine(
                baseLines, new[] { Trait(PlayerTrait.Bold, "bold") }, PlayerTrait.Kind);

            Assert.AreSame(baseLines, result);
        }

        [Test]
        public void DominantNone_ReturnsTheSameArray()
        {
            var baseLines = new[] { Line("a") };

            StoryLine[] result = TraitLineLogic.AppendTraitLine(
                baseLines, new[] { Trait(PlayerTrait.Bold, "bold") }, PlayerTrait.None);

            Assert.AreSame(baseLines, result);
        }

        [Test]
        public void EmptyOrNullBaseLines_AreReturnedAsIs_SoATraitLineNeverPlaysAlone()
        {
            var empty = new StoryLine[0];
            var traitLines = new[] { Trait(PlayerTrait.Bold, "bold") };

            Assert.AreSame(empty, TraitLineLogic.AppendTraitLine(empty, traitLines, PlayerTrait.Bold));
            Assert.IsNull(TraitLineLogic.AppendTraitLine(null, traitLines, PlayerTrait.Bold));
        }

        [Test]
        public void NullTraitLines_OrNullEntries_AreIgnored()
        {
            var baseLines = new[] { Line("a") };

            Assert.AreSame(baseLines, TraitLineLogic.AppendTraitLine(baseLines, null, PlayerTrait.Bold));

            var withNulls = new TraitLine[] { null, new TraitLine { trait = PlayerTrait.Bold, line = null } };
            Assert.AreSame(baseLines, TraitLineLogic.AppendTraitLine(baseLines, withNulls, PlayerTrait.Bold));
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해 실패 확인**

"테스트 실행"을 돌린다. Expected: `error CS0103` — `TraitLineLogic`이 없어 컴파일 에러(정상).

- [ ] **Step 3: `TraitLineLogic` 구현**

`unity/Assets/_Project/Scripts/Core/TraitLineLogic.cs`:

```csharp
using System.Collections.Generic;
using SushiSurvival.Data;

namespace SushiSurvival.Core
{
    /// <summary>기존 대사 끝에 대표 성향의 한 줄을 붙인다.</summary>
    public static class TraitLineLogic
    {
        /// <summary>
        /// baseLines가 한 줄 이상이고 dominant의 줄이 있으면 끝에 그 한 줄을 붙인 새 배열을, 아니면 baseLines를
        /// 그대로 돌려준다. 기존 대사가 없으면 성향 한 줄만 단독으로 재생하지 않는다(대화 없이 바로 전투하는
        /// 캐릭터에 갑자기 대화가 생기지 않게).
        /// </summary>
        public static StoryLine[] AppendTraitLine(StoryLine[] baseLines, IReadOnlyList<TraitLine> traitLines,
                                                  PlayerTrait dominant)
        {
            if (baseLines == null || baseLines.Length == 0 || traitLines == null || dominant == PlayerTrait.None)
                return baseLines;

            for (int i = 0; i < traitLines.Count; i++)
            {
                TraitLine entry = traitLines[i];
                if (entry == null || entry.trait != dominant || entry.line == null) continue;

                var result = new StoryLine[baseLines.Length + 1];
                baseLines.CopyTo(result, 0);
                result[baseLines.Length] = entry.line;
                return result;
            }

            return baseLines;
        }
    }
}
```

- [ ] **Step 4: 호감도 컨트롤러 — 추천 선택지를 맨 위로 + ★**

`unity/Assets/_Project/Scripts/Core/AffinityDialogueController.cs`의 `ShowQuestion` 안에서 `panel.Show(portrait, standing, question, choice =>` 줄을 다음으로 바꾼다:

```csharp
            AffinityDialogueQuestion displayed = ApplyTraitRecommendation(question);

            panel.Show(portrait, standing, displayed, choice =>
```

(뒤의 `{ ... }` 콜백 본문은 그대로 둔다.) 그리고 클래스 맨 끝(`ShowQuestion` 메서드 뒤)에 새 메서드를 추가한다:

```csharp
        /// <summary>
        /// 세계관 대화에서 쌓인 대표 성향과 같은 선택지를 맨 위로 올리고 문구 앞에 ★를 붙인다. 원본 데이터(ScriptableObject)는
        /// 건드리지 않고 표시용 복사본을 만든다 — 증강 참조는 그대로라 버프 적용은 변함이 없다.
        /// 성향이 없거나 맞는 선택지가 없으면 원본을 그대로 돌려준다.
        /// </summary>
        private static AffinityDialogueQuestion ApplyTraitRecommendation(AffinityDialogueQuestion question)
        {
            PlayerTrait dominant = PlayerTraitState.Dominant;
            if (dominant == PlayerTrait.None) return question;

            var traits = new PlayerTrait[question.choices.Length];
            for (int i = 0; i < traits.Length; i++)
                traits[i] = question.choices[i] != null ? question.choices[i].trait : PlayerTrait.None;

            int recommended = AffinityChoiceOrderLogic.RecommendedIndex(traits, dominant);
            if (recommended < 0) return question;

            int[] order = AffinityChoiceOrderLogic.DisplayOrder(question.choices.Length, recommended);
            var reordered = new AffinityDialogueChoice[order.Length];

            for (int i = 0; i < order.Length; i++)
            {
                AffinityDialogueChoice source = question.choices[order[i]];

                reordered[i] = order[i] == recommended
                    ? new AffinityDialogueChoice
                    {
                        choiceText = "★ " + source.choiceText,
                        augment = source.augment,
                        trait = source.trait,
                    }
                    : source;
            }

            return new AffinityDialogueQuestion { questionText = question.questionText, choices = reordered };
        }
```

- [ ] **Step 5: `BossFightDirector` — 보스 등장 대사에 성향 한 줄**

`unity/Assets/_Project/Scripts/Core/BossFightDirector.cs`에서:

```csharp
            StartCoroutine(IntroSequence(playerTransform, selectedCharacter.affinityDialogue?.bossEncounterLines));
```
를 다음으로 바꾼다:

```csharp
            AffinityDialogueData dialogue = selectedCharacter.affinityDialogue;
            StoryLine[] encounterLines = TraitLineLogic.AppendTraitLine(
                dialogue?.bossEncounterLines, dialogue?.bossTraitLines, PlayerTraitState.Dominant);

            StartCoroutine(IntroSequence(playerTransform, encounterLines));
```

(`using SushiSurvival.Core;`와 `using SushiSurvival.Data;`는 이 파일에 이미 있다.)

- [ ] **Step 6: `ResultPanel` — 성향 한마디**

`unity/Assets/_Project/Scripts/UI/ResultPanel.cs`:

상단 `using`에 추가: `using SushiSurvival.Data;`

필드(`killCountText` 아래)에 추가:

```csharp
        [Tooltip("세계관 대화에서 쌓인 성향에 맞는 한마디를 보여줄 Text. 비워두면 표시하지 않는다.")]
        [SerializeField] private Text traitText;
```

`Show` 안의 `BuildAugmentList(augments);` 바로 아래에 추가:

```csharp
            ShowTraitLine(outcome);
```

`Hide()` 아래에 새 메서드를 추가:

```csharp
        private void ShowTraitLine(RunOutcome outcome)
        {
            if (traitText == null) return;

            PlayerTraitLines table = PlayerTraitLines.Load();
            PlayerTraitEntry entry = table != null ? table.Find(PlayerTraitState.Dominant) : null;

            string line = entry == null
                ? null
                : (outcome == RunOutcome.Victory ? entry.victoryLine : entry.defeatLine);

            bool hasLine = !string.IsNullOrEmpty(line);
            traitText.gameObject.SetActive(hasLine);

            if (hasLine)
                traitText.text = $"[{entry.displayName}] {line}";
        }
```

- [ ] **Step 7: 테스트 실행해 통과 확인**

"테스트 실행"을 돌린다. Expected: `error CS` 없음, baseline + 6, 실패는 알려진 기존 1건뿐. `.meta` 확인:

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git status --short unity/Assets/_Project/Scripts/Core/TraitLineLogic.cs.meta unity/Assets/Tests/EditMode/TraitLineLogicTests.cs.meta
```

- [ ] **Step 8: 커밋, 푸시, PR D**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/Core/TraitLineLogic.cs unity/Assets/_Project/Scripts/Core/TraitLineLogic.cs.meta unity/Assets/_Project/Scripts/Core/AffinityDialogueController.cs unity/Assets/_Project/Scripts/Core/BossFightDirector.cs unity/Assets/_Project/Scripts/UI/ResultPanel.cs unity/Assets/Tests/EditMode/TraitLineLogicTests.cs unity/Assets/Tests/EditMode/TraitLineLogicTests.cs.meta
git commit -m "$(cat <<'EOF'
feat: 세계관 대화 성향을 호감도 대화·보스 대사·결과 화면에 반영

대표 성향과 같은 호감도 선택지를 맨 위로 올리고 ★를 붙이고, 보스 등장 대사
끝에 성향별 한 줄을 붙이고, 결과 화면에 성향 한마디를 표시한다. 성향이
None이면 세 곳 모두 기존 동작 그대로다.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/trait-consumers
gh pr create --title "feat: 성향 반영 세 곳 (성향 태그 D)" --body "$(cat <<'EOF'
## 요약
세계관 대화에서 쌓인 대표 성향을 세 곳에 작게 반영합니다.
- **호감도 대화:** 대표 성향과 같은 선택지를 맨 위로 올리고 문구 앞에 ★를 붙입니다(표시용 복사본이라 증강 참조·버프 적용은 그대로, 패널·씬은 안 건드림).
- **보스 등장 대사:** 기존 `bossEncounterLines` 끝에 성향별 한 줄(`bossTraitLines`)을 붙입니다. 기존 대사가 없는 캐릭터는 성향 한 줄만 단독으로 나오지 않습니다.
- **결과 화면:** `ResultPanel`에 선택 필드 `traitText`를 추가해 성향 한마디를 표시합니다.

성향이 `None`(건너뛰기·GameScene 직행)이면 세 곳 모두 지금과 완전히 같습니다. 데이터가 아직 비어 있어(PR E) 이 PR만으로는 게임 동작이 바뀌지 않습니다.

- 스펙: `docs/superpowers/specs/2026-09-28-story-choice-traits-design.md`
- 계획: `docs/superpowers/plans/2026-09-28-story-choice-traits.md` (PR D)
- 선행 PR: A(순수 로직), B(데이터 스키마) — C·E와는 독립이라 병렬로 올립니다.

## 협업자 확인 부탁
`AffinityDialogueController.cs`·`BossFightDirector.cs`·`ResultPanel.cs`를 수정합니다. 각각 한 곳씩만 바뀝니다.

## 테스트
- EditMode baseline + 6 통과 (알려진 기존 실패 1건 제외, MonoBehaviour는 컴파일 확인)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

---

# PR E — 대본 초안 데이터 (B 병합 후, C·D와 병렬)

### Task 5: 질문 2개·보스 성향 줄·결과 한마디 초안 (TDD)

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git checkout -b feature/trait-draft-data
```

**Files:**
- Create: `unity/Assets/Tests/EditMode/TraitDraftDataTests.cs`
- Modify: `unity/Assets/_Project/Data/WorldIntroStory.asset` (최상위에 `choicePoints:` 덧붙임)
- Modify: `unity/Assets/_Project/Data/EggAffinityDialogue.asset`, `ShrimpAffinityDialogue.asset` (최상위에 `bossTraitLines:` 덧붙임)
- Create: `unity/Assets/_Project/Resources/PlayerTraitLines.asset`

**Interfaces:**
- Consumes (B): `StoryChoicePoint`, `TraitLine`, `PlayerTraitLines`, `PlayerTrait`
- Produces (Task 6이 검증·플레이): 위 데이터

**이 대본은 초안이다.** 사용자가 이 PR에서 문구를 읽고 고칠 수 있다(전부 인스펙터 데이터라 코드 변경 없이 교체된다). 기존 에셋은 **새 최상위 키만 덧붙이고** 안쪽 필드는 안 건드린다. 호감도 선택지의 성향 태그는 기존 에셋 안쪽 필드라 스크립트로 넣지 않고 PR F에서 사용자가 인스펙터로 지정한다.

- [ ] **Step 1: 실패하는 구조 테스트 작성**

`unity/Assets/Tests/EditMode/TraitDraftDataTests.cs`:

```csharp
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    /// <summary>
    /// 성향 태그 대본 초안이 구조적으로 갖춰졌는지 확인한다. 문구·줄 번호 같은 절대값은
    /// 대본을 고칠 때마다 바뀌므로 검증하지 않는다.
    /// </summary>
    public class TraitDraftDataTests
    {
        private static readonly PlayerTrait[] AllTraits = { PlayerTrait.Bold, PlayerTrait.Careful, PlayerTrait.Kind };

        // ---- 결과 화면 한마디 ----

        [Test]
        public void PlayerTraitLines_CanBeLoadedFromResources()
        {
            Assert.IsNotNull(PlayerTraitLines.Load(), "Resources/PlayerTraitLines.asset을 불러올 수 없습니다.");
        }

        [Test]
        public void PlayerTraitLines_HasAllThreeTraits()
        {
            PlayerTraitLines lines = PlayerTraitLines.Load();
            Assert.IsNotNull(lines);

            foreach (PlayerTrait trait in AllTraits)
                Assert.IsNotNull(lines.Find(trait), $"{trait} 항목이 없습니다.");
        }

        [Test]
        public void PlayerTraitLines_EveryEntryHasAllTexts()
        {
            PlayerTraitLines lines = PlayerTraitLines.Load();
            Assert.IsNotNull(lines);

            foreach (PlayerTrait trait in AllTraits)
            {
                PlayerTraitEntry entry = lines.Find(trait);
                Assert.IsNotNull(entry);
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.displayName), $"{trait} 표시명이 비어 있습니다.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.victoryLine), $"{trait} 승리 한마디가 비어 있습니다.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.defeatLine), $"{trait} 패배 한마디가 비어 있습니다.");
            }
        }

        // ---- 세계관 대화 질문 ----

        private static StoryDialogueData LoadStory()
        {
            var story = AssetDatabase.LoadAssetAtPath<StoryDialogueData>("Assets/_Project/Data/WorldIntroStory.asset");
            Assert.IsNotNull(story, "WorldIntroStory.asset을 불러올 수 없습니다.");
            return story;
        }

        [Test]
        public void WorldIntroStory_HasTwoChoicePoints()
        {
            StoryDialogueData story = LoadStory();

            Assert.IsNotNull(story.choicePoints, "choicePoints가 비어 있습니다.");
            Assert.AreEqual(2, story.choicePoints.Length);
        }

        [Test]
        public void WorldIntroStory_ChoicePointsSitInsideTheLines()
        {
            StoryDialogueData story = LoadStory();
            Assert.IsNotNull(story.choicePoints);

            foreach (StoryChoicePoint point in story.choicePoints)
            {
                Assert.GreaterOrEqual(point.beforeLineIndex, 0);
                Assert.Less(point.beforeLineIndex, story.lines.Length, "질문이 대사 줄 범위 밖에 있습니다.");
            }
        }

        [Test]
        public void WorldIntroStory_EveryQuestionOffersAllThreeTraitsWithTexts()
        {
            StoryDialogueData story = LoadStory();
            Assert.IsNotNull(story.choicePoints);

            for (int i = 0; i < story.choicePoints.Length; i++)
            {
                StoryChoicePoint point = story.choicePoints[i];

                Assert.IsNotNull(point.prompt, $"질문 {i + 1}의 질문 대사가 없습니다.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(point.prompt.text), $"질문 {i + 1}의 질문 문구가 비어 있습니다.");
                Assert.IsNotNull(point.choices);

                foreach (PlayerTrait trait in AllTraits)
                    Assert.IsTrue(point.choices.Any(c => c.trait == trait), $"질문 {i + 1}에 {trait} 선택지가 없습니다.");

                foreach (StoryChoice choice in point.choices)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(choice.choiceText), $"질문 {i + 1}의 선택지 문구가 비어 있습니다.");
                    Assert.IsNotNull(choice.reply, $"질문 {i + 1}의 반응 대사가 없습니다.");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(choice.reply.text), $"질문 {i + 1}의 반응 문구가 비어 있습니다.");
                }
            }
        }

        // ---- 보스 등장 대사 성향 한 줄 ----

        [TestCase("Assets/_Project/Data/EggAffinityDialogue.asset")]
        [TestCase("Assets/_Project/Data/ShrimpAffinityDialogue.asset")]
        public void AffinityDialogue_HasABossTraitLineForEveryTrait(string path)
        {
            var dialogue = AssetDatabase.LoadAssetAtPath<AffinityDialogueData>(path);
            Assert.IsNotNull(dialogue, $"{path}를 불러올 수 없습니다.");
            Assert.IsNotNull(dialogue.bossTraitLines, "bossTraitLines가 비어 있습니다.");

            foreach (PlayerTrait trait in AllTraits)
            {
                TraitLine entry = dialogue.bossTraitLines.FirstOrDefault(t => t != null && t.trait == trait);
                Assert.IsNotNull(entry, $"{trait} 성향 줄이 없습니다.");
                Assert.IsNotNull(entry.line);
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.line.text), $"{trait} 성향 줄 문구가 비어 있습니다.");
            }
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해 실패 확인**

"테스트 실행"을 돌린다. Expected: 컴파일은 통과하고 새 8개가 실패(에셋/데이터가 아직 없음). 알려진 기존 1건은 그대로.

- [ ] **Step 3: 초안 생성 스크립트 작성·실행**

일회용이라 저장소에 커밋하지 않고 스크래치패드 디렉터리에 둔다. 저장 경로: `C:\Users\wnsdn\AppData\Local\Temp\claude\C--Users-wnsdn-Desktop-----------------\f31f848d-0d53-46ab-875f-6943ac14e312\scratchpad\gen_trait_draft_data.js`

```javascript
const fs = require('fs');
const root = 'C:/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity/Assets/_Project';

// Unity가 쓰는 형식과 같은 대문자 \uXXXX 이스케이프(기존 에셋 안의 문자열과 대조할 때도 필요).
const BS = String.fromCharCode(92);
const q = (s) => '"' + JSON.stringify(s).slice(1, -1)
  .replace(/[^\x20-\x7e]/g, (c) => BS + 'u' + c.charCodeAt(0).toString(16).toUpperCase().padStart(4, '0')) + '"';

const nl = '\n';
const TRAIT = { Bold: 1, Careful: 2, Kind: 3 };

// 세계관 대화(WorldIntroStory)의 등장인물 초상화·입상 스프라이트(기존 줄들이 이미 쓰는 것).
const SPRITE = (guid) => `{fileID: 21300000, guid: ${guid}, type: 3}`;
const WHO = {
  '이나리':   { portrait: SPRITE('3ad0cb01ba4f48229f03a84cfc5a6c6d'), standing: SPRITE('8938b1b9917c482493629b6f1a9048a8') },
  '아델린':   { portrait: SPRITE('9d869ea5b7b241978f2a32f14ee0556b'), standing: SPRITE('2a1db57f751b4c14a607567bc9365eeb') },
  '카마리온': { portrait: SPRITE('5b84c9f527aa4e6899d968b44a37c7a9'), standing: SPRITE('563fd5785a544cb4b74469a00d599e12') },
};

// 들여쓰기(공백 수)를 받아 StoryLine 하나를 YAML로 만든다.
function storyLine(indent, speaker, text, sprites) {
  const p = ' '.repeat(indent);
  const s = sprites || WHO[speaker];
  return [
    `${p}speakerName: ${q(speaker)}`,
    `${p}portrait: ${s ? s.portrait : '{fileID: 0}'}`,
    `${p}standing: ${s ? s.standing : '{fileID: 0}'}`,
    `${p}text: ${q(text)}`,
    `${p}background: {fileID: 0}`,
  ].join(nl);
}

function appendTopLevelKey(file, key, body) {
  let y = fs.readFileSync(file, 'utf8');
  if (y.includes(`\n  ${key}:`)) { console.log(`${file}: ${key}가 이미 있어 건너뜁니다.`); return; }
  if (!y.endsWith('\n')) y += '\n';
  fs.writeFileSync(file, y + `  ${key}:${nl}${body}${nl}`);
  console.log(`${file}: ${key}를 덧붙였습니다.`);
}

// ---------------------------------------------------------------- 1) 세계관 대화 질문 2개
// 질문 1은 줄 4 앞(아델린 "롤왕국이 정말 작정을 했군요" 직전), 질문 2는 줄 8 앞(카마리온 "그래요 저 유부 고양이…" 직전).
const points = [
  {
    before: 4,
    prompt: ['아델린', '총병력이 전부 출동했다니... 당신이라면 이 상황에서 무엇을 먼저 하겠어요?'],
    choices: [
      ['정면으로 맞서 싸운다.',            'Bold',    ['카마리온', '하하, 배짱 좋군! 그런 녀석이 있어야 전장이 살지.']],
      ['먼저 상황부터 살핀다.',            'Careful', ['이나리',   '그래 맞다, 서두르면 발 빠진다 아이가. 잘 봤다.']],
      ['백성들부터 안전하게 피신시킨다.',   'Kind',    ['아델린',   '...따뜻한 분이시군요. 저도 같은 마음입니다.']],
    ],
  },
  {
    before: 8,
    prompt: ['이나리', '니는 우짤끼고? 다 같이 싸울 때, 니는 뭘 제일 먼저 챙길 기고?'],
    choices: [
      ['제일 앞에서 길을 연다.',           'Bold',    ['이나리',   '오오, 겁대가리 없는 기 마음에 든다카이!']],
      ['뒤에서 전체 흐름을 지켜본다.',      'Careful', ['카마리온', '듬직하군. 전장엔 그런 눈이 꼭 필요하지.']],
      ['다친 동료부터 챙긴다.',            'Kind',    ['아델린',   '그 마음, 잊지 않겠습니다. 다 같이 살아서 돌아와요.']],
    ],
  },
];

const storyBody = points.map((pt) => {
  const choices = pt.choices.map(([text, trait, [who, reply]]) => [
    `    - choiceText: ${q(text)}`,
    `      trait: ${TRAIT[trait]}`,
    `      reply:`,
    storyLine(8, who, reply),
  ].join(nl)).join(nl);

  return [
    `  - beforeLineIndex: ${pt.before}`,
    `    prompt:`,
    storyLine(6, pt.prompt[0], pt.prompt[1]),
    `    choices:`,
    choices,
  ].join(nl);
}).join(nl);

appendTopLevelKey(`${root}/Data/WorldIntroStory.asset`, 'choicePoints', storyBody);

// ---------------------------------------------------------------- 2) 보스 등장 대사 성향 한 줄
// 초상화·입상은 각 에셋의 bossEncounterLines에서 그 캐릭터가 이미 쓰는 것을 그대로 가져온다.
function spritesFromBossLines(file, speaker) {
  const y = fs.readFileSync(file, 'utf8');
  const start = y.indexOf('bossEncounterLines:');
  if (start < 0) throw new Error(`${file}: bossEncounterLines를 찾지 못했습니다.`);

  const re = new RegExp(`speakerName: ${q(speaker).replace(/\\/g, '\\\\')}\\r?\\n\\s+portrait: (.*)\\r?\\n\\s+standing: (.*)`);
  const m = y.slice(start).match(re);
  if (!m) throw new Error(`${file}: ${speaker}의 초상화 줄을 찾지 못했습니다.`);
  return { portrait: m[1].trim(), standing: m[2].trim() };
}

const bossLines = {
  'EggAffinityDialogue.asset': ['아델린', {
    Bold:    '이번에도 정면승부예요. 물러설 생각은 없어요!',
    Careful: '서두르지 않겠어요. 틈을 보고, 확실하게 갑니다.',
    Kind:    '여러분이 다치지 않게... 제가 반드시 막아내겠어요.',
  }],
  'ShrimpAffinityDialogue.asset': ['카마리온', {
    Bold:    '한 방이면 돼. 처음부터 끝까지 정면으로 간다!',
    Careful: '탄창은 넉넉해. 침착하게, 한 발씩 확실히 맞춘다.',
    Kind:    '뒤에 있는 녀석들은 신경 쓰지 마. 내가 다 막아 줄 테니까.',
  }],
};

for (const [file, [speaker, byTrait]] of Object.entries(bossLines)) {
  const path = `${root}/Data/${file}`;
  const sprites = spritesFromBossLines(path, speaker);
  const body = Object.entries(byTrait).map(([trait, text]) => [
    `  - trait: ${TRAIT[trait]}`,
    `    line:`,
    storyLine(6, speaker, text, sprites),
  ].join(nl)).join(nl);

  appendTopLevelKey(path, 'bossTraitLines', body);
}

// ---------------------------------------------------------------- 3) 결과 화면 한마디(신규 에셋)
const scriptMeta = fs.readFileSync(`${root}/Scripts/Data/PlayerTraitLines.cs.meta`, 'utf8');
const scriptGuid = scriptMeta.match(/guid:\s*([0-9a-f]{32})/)[1];

const results = [
  ['Bold',    '용감함', '정면으로 맞선 용기가 승리를 만들었어요!',   '용기는 충분했어요. 다음엔 한 걸음만 더 신중하게.'],
  ['Careful', '신중함', '끝까지 침착했던 당신이 이겼습니다!',        '잘 버텼어요. 다음엔 기회를 조금 더 과감히 잡아봐요.'],
  ['Kind',    '다정함', '곁의 모두를 지켜낸 당신, 정말 멋졌어요!',   '그 다정함은 헛되지 않을 거예요. 한 번 더 도전해요.'],
];

const entriesYaml = results.map(([trait, name, win, lose]) => [
  `  - trait: ${TRAIT[trait]}`,
  `    displayName: ${q(name)}`,
  `    victoryLine: ${q(win)}`,
  `    defeatLine: ${q(lose)}`,
].join(nl)).join(nl);

const assetPath = `${root}/Resources/PlayerTraitLines.asset`;
if (fs.existsSync(assetPath)) {
  console.log('PlayerTraitLines.asset이 이미 있어 건너뜁니다.');
} else {
  fs.mkdirSync(`${root}/Resources`, { recursive: true });
  fs.writeFileSync(assetPath, [
    '%YAML 1.1',
    '%TAG !u! tag:unity3d.com,2011:',
    '--- !u!114 &11400000',
    'MonoBehaviour:',
    '  m_ObjectHideFlags: 0',
    '  m_CorrespondingSourceObject: {fileID: 0}',
    '  m_PrefabInstance: {fileID: 0}',
    '  m_PrefabAsset: {fileID: 0}',
    '  m_GameObject: {fileID: 0}',
    '  m_Enabled: 1',
    '  m_EditorHideFlags: 0',
    `  m_Script: {fileID: 11500000, guid: ${scriptGuid}, type: 3}`,
    '  m_Name: PlayerTraitLines',
    '  m_EditorClassIdentifier: ',
    '  entries:',
    entriesYaml,
    '',
  ].join(nl));
  console.log('PlayerTraitLines.asset을 만들었습니다.');
}
```

실행:

```bash
node "C:\Users\wnsdn\AppData\Local\Temp\claude\C--Users-wnsdn-Desktop-----------------\f31f848d-0d53-46ab-875f-6943ac14e312\scratchpad\gen_trait_draft_data.js"
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git diff --stat unity/Assets/_Project/Data/
git diff unity/Assets/_Project/Data/ | grep -E "^-[^-]" | head   # 삭제 줄이 없어야 한다(추가만)
```

Expected: 세 에셋에 각각 새 키가 덧붙었다는 메시지 3줄 + `PlayerTraitLines.asset을 만들었습니다.`, `git diff`에 삭제(`-`) 줄이 없다.

- [ ] **Step 4: 테스트 실행해 통과 확인**

"테스트 실행"을 돌린다. Expected: `error CS` 없음, baseline + 8, 실패는 알려진 기존 1건뿐. `.meta` 확인:

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git status --short unity/Assets/_Project/Resources unity/Assets/Tests/EditMode/TraitDraftDataTests.cs.meta
```

`Resources.meta`(폴더)가 이미 있다면 새로 안 생긴다. 새로 생겼으면 같이 커밋한다.

**실패 시 대처:** 에셋 로드는 되는데 새 필드가 안 읽히면 스크립트를 고치지 말고 그 에셋만 사용자가 에디터에서 직접 채운다(값은 스크립트의 표 그대로). `WorldIntroStory` 줄 번호가 대본과 안 맞아 보이면 사용자에게 알리고 `beforeLineIndex`를 인스펙터에서 조정하게 한다.

- [ ] **Step 5: 커밋, 푸시, PR E**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Data/WorldIntroStory.asset unity/Assets/_Project/Data/EggAffinityDialogue.asset unity/Assets/_Project/Data/ShrimpAffinityDialogue.asset unity/Assets/_Project/Resources/PlayerTraitLines.asset unity/Assets/_Project/Resources/PlayerTraitLines.asset.meta unity/Assets/Tests/EditMode/TraitDraftDataTests.cs unity/Assets/Tests/EditMode/TraitDraftDataTests.cs.meta
git status --short
git commit -m "$(cat <<'EOF'
feat: 성향 태그 대본 초안(질문 2개·보스 성향 줄·결과 한마디)

WorldIntroStory에 질문 2개, Egg/Shrimp 호감도 데이터에 보스 등장 대사용
성향별 한 줄, Resources에 결과 화면 한마디 데이터를 넣었다. 기존 에셋에는
새 최상위 키만 덧붙였다. 문구는 초안이라 인스펙터에서 교체한다.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/trait-draft-data
gh pr create --title "feat: 성향 태그 대본 초안 (성향 태그 E)" --body "$(cat <<'EOF'
## 요약
성향 태그의 **대본 초안** 데이터입니다. 문구는 전부 초안이라 인스펙터에서 자유롭게 고치면 됩니다(코드 변경 없음).
- **세계관 대화 질문 2개**(`WorldIntroStory`): 질문 1은 줄 4 앞(아델린), 질문 2는 줄 8 앞(이나리). 질문마다 용감함·신중함·다정함 선택지와 반응 대사 한 줄. 이나리는 경상도 사투리 톤을 유지했습니다.
- **보스 등장 대사 성향 한 줄**(아델린·카마리온 각 3개)
- **결과 화면 한마디**(`Resources/PlayerTraitLines.asset`, 성향 3 × 승리·패배)

기존 에셋에는 **새 최상위 키만 덧붙였고**(삭제 0줄) 안쪽 필드는 안 건드렸습니다. 아직 이 데이터를 읽는 쪽(PR C·D)이나 씬 배선(PR F)이 없어도 게임 동작은 바뀌지 않습니다.

- 스펙: `docs/superpowers/specs/2026-09-28-story-choice-traits-design.md`
- 계획: `docs/superpowers/plans/2026-09-28-story-choice-traits.md` (PR E)
- 선행 PR: B(데이터 스키마) — C·D와는 독립이라 병렬로 올립니다.

## 확인 부탁
대본 문구가 세계관 톤에 맞는지 읽어봐 주세요. 호감도 선택지의 성향 태그는 기존 에셋 안쪽 필드라 PR F에서 인스펙터로 지정합니다.

## 테스트
- EditMode baseline + 8 통과 (구조만 검증, 알려진 기존 실패 1건 제외)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

---

# PR F — 씬 배선 (C·D·E 모두 병합 후, 사용자 에디터 작업)

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git log --oneline -8   # C, D, E 병합 커밋이 다 보여야 한다
git checkout -b feature/trait-scene-wiring
```

### Task 6: StoryScene 선택 UI · 결과 화면 텍스트 · 호감도 선택지 성향 지정 (사용자 에디터 작업)

에이전트는 씬/에셋 Inspector 값을 대신 편집하지 않는다(프로젝트 관례). 사용자가 아래를 따라 하고, 에이전트는 저장된 파일을 읽어 배선을 검증한다.

#### A. StoryScene — 선택지 패널 만들기

**1.** `StoryScene`을 연다. Hierarchy에서 대사창이 들어 있는 Canvas를 찾는다(`StoryRoot`가 붙은 오브젝트나 `TextBar`가 있는 곳).

**2.** Canvas 아래에 **빈 오브젝트를 하나 만든다**(우클릭 → **Create Empty**) → 이름 **`ChoicePanel`**. **Canvas의 자식 중 맨 아래(마지막)**에 두어 대사창·딤 위에 그려지게 한다. RectTransform은 화면 가운데 위쪽에 세로로 3개 버튼이 들어갈 만한 크기로 잡는다(예: 폭 700, 높이 300, 대사창 바로 위).

**3.** `ChoicePanel` 안에 버튼 3개를 만든다. `ChoicePanel` 우클릭 → **UI → Button (Legacy)** → 이름 **`Choice_0`**. 자식 `Text`의 글자 크기·색을 대사창 글씨와 어울리게 맞춘다(폰트는 프로젝트 기본 그대로). 같은 방식으로 `Choice_1`, `Choice_2`를 만들고 세로로 나란히 배치한다(Vertical Layout Group을 붙이면 편하다).

**4.** `ChoicePanel`에 **Add Component → Story Choice Panel** → **Buttons**의 Size를 **3**으로 하고 `Choice_0`~`Choice_2`를 순서대로 넣는다. **Root**는 비워둔다.

**5.** `StorySceneController`(`StoryRoot` 오브젝트)를 선택 → **Choice Panel** 필드에 `ChoicePanel`을 연결한다. (`ChoicePanel`은 켜져 있어도 꺼져 있어도 된다 — 시작할 때 코드가 알아서 숨긴다.)

**6.** 씬 저장(Ctrl+S).

#### B. 결과 화면 — 성향 한마디 Text (GameScene, BossScene 각각)

**1.** `GameScene`을 연다. `ResultPanel` 오브젝트 아래에서 결과 문구가 있는 곳(`Outcome Text` 근처)을 찾는다.

**2.** `ResultPanel` 아래에 **UI → Text (Legacy)**를 하나 추가 → 이름 **`TraitText`** → 결과 화면에서 보이는 적당한 위치(예: 증강 목록 위)에 배치하고 글자 크기·색·정렬을 맞춘다. 긴 문장이 들어가니 가로 폭을 넉넉히, **Horizontal Overflow는 Wrap**으로 둔다.

**3.** `ResultPanel` 컴포넌트의 **Trait Text** 필드에 `TraitText`를 연결한다. 씬 저장.

**4.** **`BossScene`에서도 똑같이 한다**(결과 패널이 두 씬에 각각 있다).

#### C. 호감도 선택지에 성향 지정 (Egg / Shrimp)

**1.** `Assets/_Project/Data/EggAffinityDialogue`를 선택 → `Question1`의 `Choices` 세 원소에서 **Trait**을 지정한다:
   - Element 0("제 양산이 닿는 곳은 전부…", 공격력) → **Bold**
   - Element 1("무너지지 않는 게 먼저예요…", 방어력) → **Careful**
   - Element 2("발이 빠르면, 애초에 다칠 일이 없죠.", 이동속도) → **Kind**

**2.** `ShrimpAffinityDialogue`도 같은 순서로: 0("한 방에 끝낸다.") → **Bold**, 1("먼저 버티는 쪽이 이긴다.") → **Careful**, 2("맞기 전에 피한다.") → **Kind**.

**3.** (선택) "다정함"과 이동속도 선택지는 뜻이 안 이어지니, 세 번째 선택지 문구를 다정한 톤으로 손봐도 된다. 예: 아델린 "발이 빠르면, 곁의 모두를 데리고 물러설 수도 있죠." / 카마리온 "다 같이 안 맞고 끝내는 게 제일이지." 이건 이번 작업 범위 밖이라 원하실 때만 한다.

**4.** 에셋 저장(Ctrl+S).

---

### 에이전트 검증

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요\unity"
git status --short Assets/_Project/Scenes Assets/_Project/Data
echo "--- StoryScene: 선택 패널 컴포넌트와 컨트롤러 연결 ---"
gp=$(grep -m1 'guid:' Assets/_Project/Scripts/UI/StoryChoicePanel.cs.meta | awk '{print $2}')
grep -c "guid: $gp" Assets/_Project/Scenes/StoryScene.unity
grep -n "choicePanel:" Assets/_Project/Scenes/StoryScene.unity
echo "--- StoryChoicePanel buttons 3개 연결 ---"
grep -n -A6 "guid: $gp" Assets/_Project/Scenes/StoryScene.unity | grep -E "buttons:|- \{fileID"
echo "--- 결과 화면 traitText (GameScene, BossScene 둘 다 0이 아니어야 함) ---"
grep -n "traitText:" Assets/_Project/Scenes/GameScene.unity Assets/_Project/Scenes/BossScene.unity
echo "--- 호감도 선택지 trait (Egg/Shrimp 각 1,2,3) ---"
grep -n "    trait:\|      trait:" Assets/_Project/Data/EggAffinityDialogue.asset Assets/_Project/Data/ShrimpAffinityDialogue.asset
echo "--- 임시값 점검: bossSpawnTime은 300이어야 함 ---"
grep -n "bossSpawnTime:" Assets/_Project/Scenes/GameScene.unity
```

Expected: `StoryScene`에 `StoryChoicePanel` 컴포넌트 1개, `choicePanel:`이 `{fileID: 0}`이 아님, `buttons` 세 원소가 0이 아님. `traitText:`가 두 씬 모두 `{fileID: 0}`이 아님. Egg/Shrimp `question1` 선택지에 `trait: 1`, `2`, `3`이 각각. `bossSpawnTime: 300`. **예상 밖 파일이 `git status`에 있거나 `bossSpawnTime`이 300이 아니면 사용자에게 알리고 커밋하지 않는다.**

- [ ] **Step 1: 컴파일 + 회귀 확인**

"테스트 실행"을 돌린다(씬·에셋만 바뀌어 컴파일엔 영향 없지만 습관대로). Expected: `error CS` 없음, 실패는 알려진 기존 1건뿐.

- [ ] **Step 2: Play 모드 전체 흐름 확인 (사용자)**

`IntroScene`에서 시작해 처음부터 끝까지 한 판을 돌린다.

- [ ] 시작 → 세계관 대화가 나오고 **줄 4 앞에서 질문**(아델린)과 **선택지 3개**가 뜬다. 이 동안 클릭·Space·Enter로 넘어가지 **않는다**
- [ ] 선택지를 누르면 **반응 대사 한 줄**이 나오고, 다시 클릭하면 원래 대사가 **이어진다**(선택지를 누른 클릭이 반응 대사를 건너뛰게 만들지 않는지)
- [ ] **줄 8 앞에서 두 번째 질문**(이나리)이 같은 방식으로 나오고, 전체가 1분 안팎인지
- [ ] 캐릭터 선택 → 호감도 대화 증강 3택에서 **내 성향에 맞는 선택지가 맨 위 ★**로 올라오고, 그걸 골라도 버프가 정상 적용된다(두 질문에서 1:1로 갈리면 두 번째 답이 이기는지)
- [ ] 첫 질문에서 **건너뛰기**를 눌렀을 때 문제없이 GameScene으로 넘어가고, 호감도 대화가 **성향 없이 원래 순서·★ 없음**으로 나온다
- [ ] BossScene 보스 등장 대사 **끝에 내 성향의 한 줄**이 붙는다(아델린·카마리온 각각). 성향이 없을 때(건너뛰기)는 안 붙는다
- [ ] 결과 화면에 `[용감함] …` 형태의 **성향 한마디**가 나오고, 승리와 패배가 다른 문구인지(GameScene에서 죽었을 때, BossScene에서 이겼을 때 모두)
- [ ] "다시 하기" → 인트로 → 다시 세계관 대화에서 **성향이 초기화**되어 새로 고를 수 있는지
- [ ] 에디터에서 GameScene으로 직행해도(성향 없음) 에러 없이 이전과 똑같이 동작하는지

- [ ] **Step 3: 커밋, 푸시, PR F**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git status --short
git diff --stat
git add unity/Assets/_Project/Scenes/StoryScene.unity unity/Assets/_Project/Scenes/GameScene.unity unity/Assets/_Project/Scenes/BossScene.unity unity/Assets/_Project/Data/EggAffinityDialogue.asset unity/Assets/_Project/Data/ShrimpAffinityDialogue.asset
git status --short
git commit -m "$(cat <<'EOF'
feat: 성향 태그 씬 배선(선택 UI·결과 텍스트·호감도 선택지 성향)

StoryScene에 선택지 패널(버튼 3개)을 만들어 컨트롤러에 연결하고,
GameScene·BossScene 결과 패널에 성향 한마디 Text를 연결하고,
아델린·카마리온 호감도 선택지에 성향을 지정했다.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/trait-scene-wiring
gh pr create --title "feat: 성향 태그 씬 배선 (성향 태그 F)" --body "$(cat <<'EOF'
## 요약
세계관 대화 선택 → 성향 태그 작업의 마지막 조각. `StoryScene`에 선택지 패널(버튼 3개)을 만들어 `StorySceneController`에 연결하고, `GameScene`·`BossScene`의 결과 패널에 성향 한마디 Text를 연결하고, 아델린·카마리온 호감도 선택지에 성향(용감함/신중함/다정함)을 지정했습니다. 이제 한 판을 돌리면 세계관 대화의 질문 2번이 나오고 그 답이 호감도 대화·보스 등장 대사·결과 화면에 반영됩니다.

- 스펙: `docs/superpowers/specs/2026-09-28-story-choice-traits-design.md`
- 계획: `docs/superpowers/plans/2026-09-28-story-choice-traits.md` (PR F)
- 선행 PR: A, B, C, D, E 전부 병합됨

## 협업자 확인 부탁
`StoryScene.unity`·`GameScene.unity`·`BossScene.unity`를 수정했습니다. StoryScene에는 `ChoicePanel` 오브젝트(버튼 3개)를 추가했고, 두 게임 씬의 결과 패널에는 `TraitText`를 하나씩 추가했습니다. 기존 오브젝트는 안 건드렸습니다.

## 테스트
- EditMode baseline 유지 (알려진 기존 실패 1건 제외)
- Play 모드로 질문 2번(넘기기 차단·반응 대사·이어짐), 호감도 ★ 추천, 건너뛰기 폴백, 보스 성향 한 줄, 결과 한마디(승·패), "다시 하기" 시 성향 초기화, GameScene 직행 폴백 확인 완료(사용자)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

**여기서 멈추고 사용자가 PR F를 병합하길 기다린다.** 병합 후 `main` 동기화, 브랜치 정리, 메모리(`sushi-survival-project.md`) 업데이트로 마무리한다. 기획 문서 4번(세계관 선택 대화)이 이걸로 끝나고, 남는 건 와사비 연출 강화(사운드 시스템 필요)와 카마리온 와사비 보상(사용자가 결정)이다.
