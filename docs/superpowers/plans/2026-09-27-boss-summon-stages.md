# 보스 소환 6단계(체력 임계 연동) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 보스 소환을 "가중치 무작위 패턴"에서 "보스 체력이 임계 6개에 닿을 때마다 나오는 6단계 사건"으로 바꾼다. 무작위 패턴은 메테오·돌진 둘만 남는다.

**Architecture:** 소환 단계(임계·마릿수)는 `BossData`의 인스펙터 배열로 두고, "이번 프레임에 새로 넘은 단계 수"는 순수 함수 `BossSummonLogic`이 계산한다. `BossController`는 넘은 단계를 큐에 쌓아 시전이 끝난 뒤 하나씩 소환하고, `SummonPattern`은 일반·캘리포니아·중형몹 풀 세 개에서 단계 데이터대로 소환한다. 추가만 하는 변경(데이터·로직·풀 필드)을 먼저 병합하고, 기존 소환 패턴 삭제는 소비자 수정과 한 PR에 묶어 원자적으로 전환한다.

**Tech Stack:** Unity 2022.3.62f3, Built-in 2D, NUnit EditMode 테스트. 관례: 판정 로직은 `XxxLogic` 정적 클래스 + EditMode 테스트, MonoBehaviour는 컴파일+회귀 확인만.

**Spec:** `docs/superpowers/specs/2026-09-27-boss-summon-stages-design.md`

## Global Constraints

- `main` 직접 커밋·푸시·병합 절대 금지 — **문서 파일도 예외 없음.** 모든 태스크는 새 브랜치에서 시작하고 PR로 올린다. 병합 버튼은 사람만 누른다.
- `git add .` 금지 — 파일을 명시하고 `.meta`를 항상 같이 커밋한다.
- 씬(`.unity`)·프리팹 편집은 사람이 Unity Editor GUI로 한다. 에이전트는 새 데이터 에셋을 만들거나 기존 `.asset`에 새 필드를 **덧붙이는** 스크립트만 쓴다.
- 데이터 에셋 테스트는 마릿수·임계의 **절대값을 assert하지 않는다**(플레이로 계속 바뀜). 개수·순서·범위 같은 구조만 검증한다.
- 몹은 기존 프리팹(`BasicMob`·`CaliforniaRoll`·`MidBos`)을 그대로 쓴다. 새 아트 없음.
- 스펙과 다른 점 하나: `BossController.Activate`의 풀 인자를 재정렬하지 않고 **기존 인자 뒤에 선택 인자 두 개를 덧붙인다**(`californiaMobPool`, `midMobPool`). 호출부 두 곳이 그대로 컴파일돼 PR B가 순수 추가가 된다.

### 배치 테스트 실행 명령 (이 문서에서 "테스트 실행"이라 부르는 것)

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요\unity"
"C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" -batchmode -projectPath "$(pwd -W)" -runTests -testPlatform EditMode -testResults "$(pwd -W)/TestResults.xml" -logFile - > "$TEMP/unity_test.log" 2>&1
grep -E "error CS" "$TEMP/unity_test.log" | head -20
grep -c "HandleProjectAlreadyOpenInAnotherInstance" "$TEMP/unity_test.log"
head -c 400 TestResults.xml
```

성공 기준: `error CS` 없음, `HandleProjectAlreadyOpenInAnotherInstance` 카운트 0(에디터가 열려 있으면 사용자에게 닫아달라고 요청), `failed`가 **알려진 기존 실패 1건**(`WorldIntroStoryAssetTests`의 이나리 초상화 테스트) 뿐. 끝나면 `rm -f TestResults.xml`.
**baseline은 태스크 시작 직전 `main`에서 처음 돌린 통과 개수**다. 기대 개수는 그 baseline에 이 태스크가 추가한 만큼을 더한 값으로 본다.

### 이미 존재하는(이번 계획이 그대로 쓰는) 타입

- `SushiSurvival.Data.BossData : MonsterData` — `maxHealth`, `phaseTwoThreshold`, `GetPhaseValues(int)`; `BossPhaseValues { ..., int summonCount; float summonRadius; }` (`Data/BossData.cs`)
- `SushiSurvival.Enemies.Boss.SummonPlacement.GetPositions(Vector2 center, int count, float radius, float startAngleRad)` → `List<Vector2>`
- `SushiSurvival.Core.GameObjectPool.Get(Vector3, Quaternion)` → `GameObject` (풀 하나당 프리팹 하나)
- `SushiSurvival.Enemies.EnemyBase.CurrentHealth`, `SetXpGemPools(XPGemPoolSet)`

---

## 브랜치 / PR 분할

| PR | 브랜치 | 태스크 | 시작 시점 | 의존 |
|---|---|---|---|---|
| **A** | `feature/boss-summon-stage-logic` | Task 1 (단계 구조체·`BossData` 필드·`BossSummonLogic`) | `main`에서 지금 | 없음 |
| **B** | `feature/boss-summon-pool-plumbing` | Task 2 (`SummonPattern.FireStage`·풀 배선, 추가만) | **A 병합 후** | A의 `BossSummonStage` |
| **D** | `feature/boss-summon-stage-data` | Task 4 (`BossData.asset`에 6단계 덧붙임) | **A 병합 후** (B와 병렬) | A의 `summonStages` 필드 |
| **C** | `feature/boss-summon-by-health` | Task 3 (소환 전환 — 원자적) | **A·B 병합 후** | A의 로직, B의 `FireStage`·`SetStagePools` |
| **E** | `feature/boss-summon-scene-wiring` | Task 5 (BossScene 풀·필드 배선, 사용자 에디터 작업) | **B·C·D 모두 병합 후** | 전부 |

B와 D는 파일이 안 겹쳐 A만 병합되면 동시에 진행할 수 있다. **C는 `BossPatternType.Summon`과 `BossPhaseValues.summonCount`를 지우기 때문에**(기존 소비자인 `BossController`·`SummonPattern`·`BossPatternSchedulerTests`가 같이 바뀌어야 컴파일된다) 스케줄러·컨트롤러·소환 패턴·데이터 필드 수정을 **한 PR에 묶는다.** D가 C보다 먼저 병합돼도 안전하도록 D는 에셋에 새 배열만 **덧붙이고** 기존 `summonCount` 줄은 지우지 않는다(지우면 C 병합 전 기존 소환이 0마리가 된다). 남은 `summonCount` 줄은 Unity가 다음 저장 때 조용히 버린다.

---

## File Structure

| 파일 | 역할 |
|---|---|
| `unity/Assets/_Project/Scripts/Data/BossData.cs` (수정) | `BossSummonStage` 구조체, `summonStages` 배열 추가(A) / `summonCount` 제거(C) |
| `unity/Assets/_Project/Scripts/Enemies/Boss/BossSummonLogic.cs` (신규) | 새로 넘은 소환 단계 수 계산 |
| `unity/Assets/_Project/Scripts/Enemies/Boss/SummonPattern.cs` (수정) | `FireStage`·`SetStagePools` 추가(B) / 기존 `Fire` 제거(C) |
| `unity/Assets/_Project/Scripts/Enemies/Boss/BossController.cs` (수정) | 풀 인자 추가(B) / 체력 임계 소환 큐·연속 횟수 추적(C) |
| `unity/Assets/_Project/Scripts/Enemies/Boss/BossPatternScheduler.cs` (수정) | `Summon` 제거, 연속 횟수 규칙(C) |
| `unity/Assets/_Project/Scripts/Core/BossFightDirector.cs` (수정) | 새 풀 필드 두 개·`Activate` 전달(B) |
| `unity/Assets/_Project/Scripts/Enemies/Boss/BossDirector.cs` (수정) | 위와 동일(예전 GameScene용, 컴파일 유지용)(B) |
| `unity/Assets/_Project/Data/BossData.asset` (수정) | `summonStages` 6개 덧붙임(D) |
| `unity/Assets/Tests/EditMode/BossSummonLogicTests.cs` (신규) | Task 1 검증 |
| `unity/Assets/Tests/EditMode/BossPatternSchedulerTests.cs` (수정) | Task 3 검증 — 재작성 |
| `unity/Assets/Tests/EditMode/BossDataAssetTests.cs` (신규) | Task 4 검증 |
| `unity/Assets/_Project/Scenes/BossScene.unity` (수정, PR E) | 풀 2개 추가, 디렉터 필드 연결, prewarm |

---

# PR A — 단계 구조체·`BossData` 필드·`BossSummonLogic` (`main`에서 지금 시작)

### Task 1: 소환 단계 데이터 구조와 판정 로직 (TDD)

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git checkout -b feature/boss-summon-stage-logic
```

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Data/BossData.cs`
- Create: `unity/Assets/_Project/Scripts/Enemies/Boss/BossSummonLogic.cs`
- Test: `unity/Assets/Tests/EditMode/BossSummonLogicTests.cs`

**Interfaces:**
- Produces (Task 2·3·4가 사용):
  - `SushiSurvival.Data.BossSummonStage { float healthThreshold; int basicCount; int californiaCount; int midCount; int Total }`
  - `BossData.summonStages` (`BossSummonStage[]`)
  - `BossSummonLogic.CrossedStageCount(float currentHealth, float maxHealth, IReadOnlyList<BossSummonStage> stages, int nextStage)` → `int`

추가만 하는 변경이라 기존 코드는 영향이 없다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/BossSummonLogicTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.Data;
using SushiSurvival.Enemies.Boss;

namespace SushiSurvival.EditModeTests
{
    public class BossSummonLogicTests
    {
        private static BossSummonStage[] Stages(params float[] thresholds)
        {
            var stages = new BossSummonStage[thresholds.Length];
            for (int i = 0; i < thresholds.Length; i++)
                stages[i] = new BossSummonStage { healthThreshold = thresholds[i], basicCount = 1 };
            return stages;
        }

        [Test]
        public void AboveFirstThreshold_ReturnsZero()
        {
            Assert.AreEqual(0, BossSummonLogic.CrossedStageCount(86f, 100f, Stages(0.85f, 0.7f, 0.55f), 0));
        }

        [Test]
        public void ExactlyAtThreshold_Counts()
        {
            Assert.AreEqual(1, BossSummonLogic.CrossedStageCount(85f, 100f, Stages(0.85f, 0.7f, 0.55f), 0));
        }

        [Test]
        public void BelowFirstThreshold_ReturnsOne()
        {
            Assert.AreEqual(1, BossSummonLogic.CrossedStageCount(84f, 100f, Stages(0.85f, 0.7f, 0.55f), 0));
        }

        [Test]
        public void BigHit_CrossesSeveralStagesAtOnce()
        {
            Assert.AreEqual(3, BossSummonLogic.CrossedStageCount(50f, 100f, Stages(0.85f, 0.7f, 0.55f), 0));
        }

        [Test]
        public void AlreadyCrossedStages_AreSkipped()
        {
            Assert.AreEqual(2, BossSummonLogic.CrossedStageCount(50f, 100f, Stages(0.85f, 0.7f, 0.55f), 1));
        }

        [Test]
        public void NextStagePastTheEnd_ReturnsZero()
        {
            Assert.AreEqual(0, BossSummonLogic.CrossedStageCount(1f, 100f, Stages(0.85f, 0.7f, 0.55f), 3));
        }

        [Test]
        public void ZeroOrNegativeMaxHealth_ReturnsZero()
        {
            Assert.AreEqual(0, BossSummonLogic.CrossedStageCount(0f, 0f, Stages(0.85f), 0));
            Assert.AreEqual(0, BossSummonLogic.CrossedStageCount(0f, -5f, Stages(0.85f), 0));
        }

        [Test]
        public void NullStages_ReturnsZero()
        {
            Assert.AreEqual(0, BossSummonLogic.CrossedStageCount(10f, 100f, null, 0));
        }

        [Test]
        public void NegativeNextStage_ReturnsZero()
        {
            Assert.AreEqual(0, BossSummonLogic.CrossedStageCount(10f, 100f, Stages(0.85f), -1));
        }

        [Test]
        public void Stage_Total_SumsAllMobKinds()
        {
            var stage = new BossSummonStage { basicCount = 3, californiaCount = 3, midCount = 2 };
            Assert.AreEqual(8, stage.Total);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해 실패 확인**

"테스트 실행"을 돌린다. Expected: `error CS0246`/`CS0103` — `BossSummonStage`·`BossSummonLogic`이 없어 컴파일 에러(정상, Step 3에서 해결).

- [ ] **Step 3: 구현**

`unity/Assets/_Project/Scripts/Data/BossData.cs`의 `BossPhaseValues` 구조체 앞에 새 구조체를 추가한다:

```csharp
    /// <summary>보스 체력이 임계에 닿으면 나오는 소환 한 차수.</summary>
    [System.Serializable]
    public struct BossSummonStage
    {
        [Range(0f, 1f)]
        [Tooltip("보스 체력 비율이 이 값 이하로 내려가면 이 단계가 나온다. 단계 순서대로 내림차순으로 적는다.")]
        public float healthThreshold;
        public int basicCount;
        public int californiaCount;
        public int midCount;

        public int Total => basicCount + californiaCount + midCount;
    }
```

같은 파일의 `BossData` 클래스에서 `phaseTwoThreshold` 바로 아래에 필드를 추가한다:

```csharp
        [Range(0f, 1f)]
        [Tooltip("현재 체력 비율이 이 값 아래로 내려가면 페이즈 2로 전환한다.")]
        public float phaseTwoThreshold = 0.5f;

        [Tooltip("체력 임계별 소환 단계. 임계는 내림차순(높은 체력부터)으로 적는다.")]
        public BossSummonStage[] summonStages;
```

`unity/Assets/_Project/Scripts/Enemies/Boss/BossSummonLogic.cs`:

```csharp
using System.Collections.Generic;
using SushiSurvival.Data;

namespace SushiSurvival.Enemies.Boss
{
    /// <summary>
    /// 보스 체력이 소환 임계에 닿았는지 판정한다. 한 번의 큰 피해로 임계를 여러 개 넘을 수 있어서,
    /// "이번에 새로 넘은 단계 수"를 돌려주고 호출자가 그만큼 순서대로 소환한다(단계를 건너뛰지 않는다).
    /// </summary>
    public static class BossSummonLogic
    {
        /// <param name="nextStage">아직 소환하지 않은 첫 단계의 인덱스.</param>
        public static int CrossedStageCount(float currentHealth, float maxHealth,
                                            IReadOnlyList<BossSummonStage> stages, int nextStage)
        {
            if (maxHealth <= 0f || stages == null || nextStage < 0) return 0;

            float ratio = currentHealth / maxHealth;

            int count = 0;
            for (int k = nextStage; k < stages.Count; k++)
            {
                // "닿는 즉시"라서 이하로 비교한다.
                if (ratio > stages[k].healthThreshold) break;
                count++;
            }

            return count;
        }
    }
}
```

- [ ] **Step 4: 테스트 실행해 통과 확인**

"테스트 실행"을 돌린다. Expected: `error CS` 없음, baseline + 10, 실패는 알려진 기존 1건뿐. 끝나면 `rm -f TestResults.xml`. `.meta` 확인:

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git status --short unity/Assets/_Project/Scripts/Enemies/Boss/BossSummonLogic.cs unity/Assets/_Project/Scripts/Enemies/Boss/BossSummonLogic.cs.meta unity/Assets/Tests/EditMode/BossSummonLogicTests.cs unity/Assets/Tests/EditMode/BossSummonLogicTests.cs.meta
```

- [ ] **Step 5: 커밋, 푸시, PR A**

이 계획서 파일(`docs/superpowers/plans/2026-09-27-boss-summon-stages.md`)이 아직 커밋 전이면 이 PR에 같이 넣는다.

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/Data/BossData.cs unity/Assets/_Project/Scripts/Enemies/Boss/BossSummonLogic.cs unity/Assets/_Project/Scripts/Enemies/Boss/BossSummonLogic.cs.meta unity/Assets/Tests/EditMode/BossSummonLogicTests.cs unity/Assets/Tests/EditMode/BossSummonLogicTests.cs.meta docs/superpowers/plans/2026-09-27-boss-summon-stages.md
git commit -m "$(cat <<'EOF'
feat: 보스 소환 단계 데이터 구조와 체력 임계 판정 로직

BossSummonStage 구조체와 BossData.summonStages 배열을 추가하고,
BossSummonLogic.CrossedStageCount가 이번에 새로 넘은 단계 수를 계산한다.
추가만 하는 변경이라 게임 동작은 바뀌지 않는다. 구현 계획서도 같이 커밋.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/boss-summon-stage-logic
gh pr create --title "feat: 보스 소환 단계 데이터·판정 로직 (보스 소환 A)" --body "$(cat <<'EOF'
## 요약
보스 소환을 체력 임계 6단계로 바꾸는 작업의 첫 조각. `BossSummonStage` 구조체와 `BossData.summonStages` 배열, 새로 넘은 단계 수를 계산하는 순수 함수 `BossSummonLogic`을 추가했습니다. 추가만 하는 변경이고 아직 아무도 쓰지 않아 게임 동작은 바뀌지 않습니다.

- 스펙: `docs/superpowers/specs/2026-09-27-boss-summon-stages-design.md`
- 계획: `docs/superpowers/plans/2026-09-27-boss-summon-stages.md` (PR A)

## 협업자 확인 부탁
`BossData.cs`(보스 데이터 구조)에 필드가 추가됩니다. 기존 필드는 안 건드립니다.

## 테스트
- EditMode baseline + 10 통과 (알려진 기존 실패 1건 제외)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

---

# PR B — 소환 풀 배선, 추가만 (A 병합 후)

### Task 2: `SummonPattern.FireStage`와 풀 전달

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git log --oneline -3   # A 병합 커밋이 보여야 한다
git checkout -b feature/boss-summon-pool-plumbing
```

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Enemies/Boss/SummonPattern.cs`
- Modify: `unity/Assets/_Project/Scripts/Enemies/Boss/BossController.cs`
- Modify: `unity/Assets/_Project/Scripts/Core/BossFightDirector.cs`
- Modify: `unity/Assets/_Project/Scripts/Enemies/Boss/BossDirector.cs`

**Interfaces:**
- Consumes (A): `SushiSurvival.Data.BossSummonStage`
- Produces (Task 3·5가 사용):
  - `SummonPattern.SetStagePools(GameObjectPool californiaPool, GameObjectPool midPool)`
  - `SummonPattern.FireStage(BossSummonStage stage, float radius)`
  - `BossController.Activate(PlayerHealth, GameObjectPool meteorPool, GameObjectPool mobPool, GameObjectPool summonEffectPool, XPGemPoolSet gemPools, GameObjectPool californiaMobPool = null, GameObjectPool midMobPool = null)`
  - `BossFightDirector`/`BossDirector`의 직렬화 필드 `summonCaliforniaPool`, `summonMidMobPool` — Task 5가 BossScene에서 연결

MonoBehaviour라 전용 테스트는 없다(컴파일+회귀). 기존 `Fire(BossPhaseValues)`와 기존 `Activate` 호출은 **그대로 동작**한다(추가만).

- [ ] **Step 1: `SummonPattern.cs` 수정**

`unity/Assets/_Project/Scripts/Enemies/Boss/SummonPattern.cs`의 필드·`SetDependencies` 부분을:

```csharp
        private Transform _player;
        private GameObjectPool _mobPool;
        private GameObjectPool _californiaPool;
        private GameObjectPool _midPool;
        private GameObjectPool _effectPool;
        private XPGemPoolSet _gemPools;

        public void SetDependencies(Transform playerTransform, GameObjectPool mobPool,
                                    GameObjectPool effectPool, XPGemPoolSet gemPools)
        {
            _player = playerTransform;
            _mobPool = mobPool;
            _effectPool = effectPool;
            _gemPools = gemPools;
        }

        /// <summary>체력 임계 소환 단계에서 쓸 캘리포니아롤·중형몹 풀. 일반 몹 풀은 SetDependencies의 mobPool을 쓴다.</summary>
        public void SetStagePools(GameObjectPool californiaPool, GameObjectPool midPool)
        {
            _californiaPool = californiaPool;
            _midPool = midPool;
        }
```

기존 `Fire(...)` 메서드는 그대로 두고, 그 바로 아래에 새 메서드를 추가한다:

```csharp
        /// <summary>
        /// 소환 한 단계를 발동한다. 일반·캘리·중형몹을 섞어서 플레이어를 둘러싼 링 위에 균등하게 놓는다
        /// (종류별로 묶어 두면 한쪽에 같은 몹이 몰린다). 풀이 비어 있는 종류는 그 몹만 건너뛴다.
        /// </summary>
        public void FireStage(BossSummonStage stage, float radius)
        {
            if (_mobPool == null || _player == null)
            {
                Debug.LogError($"{name}: mobPool 또는 player가 주입되지 않아 소환할 수 없습니다.");
                return;
            }

            var pools = new List<GameObjectPool>();
            AddPools(pools, _mobPool, stage.basicCount, "일반");
            AddPools(pools, _californiaPool, stage.californiaCount, "캘리포니아");
            AddPools(pools, _midPool, stage.midCount, "중형몹");

            for (int i = pools.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pools[i], pools[j]) = (pools[j], pools[i]);
            }

            float startAngle = Random.Range(0f, Mathf.PI * 2f);
            List<Vector2> positions = SummonPlacement.GetPositions(_player.position, pools.Count, radius, startAngle);

            for (int i = 0; i < positions.Count; i++)
                StartCoroutine(SummonAt(positions[i], pools[i]));
        }

        private void AddPools(List<GameObjectPool> list, GameObjectPool pool, int count, string label)
        {
            if (count <= 0) return;

            if (pool == null)
            {
                Debug.LogError($"{name}: {label} 몹 풀이 비어 있어 {count}마리를 건너뜁니다.");
                return;
            }

            for (int i = 0; i < count; i++)
                list.Add(pool);
        }
```

기존 `SummonAt`을 풀을 인자로 받도록 바꾸고, 기존 `Fire`의 호출부도 맞춘다:

```csharp
            foreach (Vector2 position in positions)
                StartCoroutine(SummonAt(position, _mobPool));
        }

        private IEnumerator SummonAt(Vector2 position, GameObjectPool pool)
        {
            float delay = summonDelayOverride;

            if (_effectPool != null)
            {
                GameObject effect = _effectPool.Get(position, Quaternion.identity);

                if (delay <= 0f && effect.TryGetComponent<OneShotEffect>(out var oneShot))
                    delay = oneShot.Duration;
            }

            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            if (pool == null) yield break;

            GameObject mob = pool.Get(position, Quaternion.identity);
            if (mob == null) yield break;
```

(그 아래 `if (mob.TryGetComponent<EnemyBase>(out var enemy)) { ... }` 블록은 그대로 둔다. 바뀐 건 시그니처, `_mobPool` → `pool` 두 곳, 그리고 `Fire` 안의 호출뿐이다.)

- [ ] **Step 2: `BossController.Activate`에 선택 인자 추가**

`unity/Assets/_Project/Scripts/Enemies/Boss/BossController.cs`:

```csharp
        public void Activate(PlayerHealth player, GameObjectPool meteorPool,
                             GameObjectPool mobPool, GameObjectPool summonEffectPool,
                             XPGemPoolSet gemPools,
                             GameObjectPool californiaMobPool = null, GameObjectPool midMobPool = null)
```

그리고 같은 메서드 안의 소환 의존성 주입 블록을:

```csharp
            if (summonPattern != null && mobPool != null && summonEffectPool != null)
            {
                summonPattern.SetDependencies(
                    player != null ? player.transform : null, mobPool, summonEffectPool, gemPools);
                summonPattern.SetStagePools(californiaMobPool, midMobPool);
            }
```

- [ ] **Step 3: 두 디렉터에 새 풀 필드와 전달 추가**

`unity/Assets/_Project/Scripts/Core/BossFightDirector.cs`의 `summonMobPool` 필드 바로 아래에:

```csharp
        [SerializeField] private GameObjectPool summonMobPool;
        [Tooltip("소환 단계에서 나올 캘리포니아롤 풀.")]
        [SerializeField] private GameObjectPool summonCaliforniaPool;
        [Tooltip("소환 단계에서 나올 중형몹 풀.")]
        [SerializeField] private GameObjectPool summonMidMobPool;
```

같은 파일의 `boss.Activate(...)` 호출을:

```csharp
            boss.Activate(playerHealthComp, meteorPool, summonMobPool, summonEffectPool, gemPools,
                          summonCaliforniaPool, summonMidMobPool);
```

`unity/Assets/_Project/Scripts/Enemies/Boss/BossDirector.cs`(예전 GameScene용)도 똑같이:

```csharp
        [SerializeField] private GameObjectPool summonMobPool;
        [Tooltip("소환 단계에서 나올 캘리포니아롤 풀.")]
        [SerializeField] private GameObjectPool summonCaliforniaPool;
        [Tooltip("소환 단계에서 나올 중형몹 풀.")]
        [SerializeField] private GameObjectPool summonMidMobPool;
```
```csharp
            boss.Activate(player, meteorPool, summonMobPool, summonEffectPool, gemPools,
                          summonCaliforniaPool, summonMidMobPool);
```

`summonMobPool` 필드는 **이름을 바꾸지 않는다** — 바꾸면 BossScene에 연결된 BasicMob 풀 참조가 끊긴다.

- [ ] **Step 4: 컴파일 + 회귀 확인**

"테스트 실행"을 돌린다. Expected: `error CS` 없음, baseline과 같은 개수(새 테스트 없음), 실패는 알려진 기존 1건뿐. 끝나면 `rm -f TestResults.xml`.

- [ ] **Step 5: 커밋, 푸시, PR B**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/Enemies/Boss/SummonPattern.cs unity/Assets/_Project/Scripts/Enemies/Boss/BossController.cs unity/Assets/_Project/Scripts/Core/BossFightDirector.cs unity/Assets/_Project/Scripts/Enemies/Boss/BossDirector.cs
git commit -m "$(cat <<'EOF'
feat: 소환 단계 발동(FireStage)과 캘리포니아·중형몹 풀 배선

SummonPattern.FireStage가 일반·캘리·중형몹을 섞어 링 위에 소환한다.
BossController.Activate에 선택 인자 두 개를, 두 디렉터에 풀 필드 두 개를
추가했다. 추가만 하는 변경이라 기존 소환은 그대로 동작한다.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/boss-summon-pool-plumbing
gh pr create --title "feat: 소환 단계 발동·풀 배선 (보스 소환 B)" --body "$(cat <<'EOF'
## 요약
체력 임계 소환 전환(PR C)에 앞서, 소환 한 단계를 발동하는 `SummonPattern.FireStage`와 캘리포니아롤·중형몹 풀을 전달하는 배선을 추가했습니다. `Activate`는 기존 인자 뒤에 선택 인자로만 늘렸고, 두 디렉터에는 새 풀 필드 두 개가 생겼습니다. 기존 `Fire`와 기존 소환 패턴은 그대로라 게임 동작은 바뀌지 않습니다. `summonMobPool` 필드 이름은 유지해서 BossScene의 BasicMob 풀 연결이 끊기지 않습니다.

- 스펙: `docs/superpowers/specs/2026-09-27-boss-summon-stages-design.md`
- 계획: `docs/superpowers/plans/2026-09-27-boss-summon-stages.md` (PR B)
- 선행 PR: A(소환 단계 구조체) — D와는 독립이라 병렬로 올립니다.

## 협업자 확인 부탁
`BossController`·`SummonPattern`·`BossFightDirector`·`BossDirector`(보스 코드)를 수정합니다. 새 필드는 씬에서 아직 비어 있고 PR E에서 연결합니다.

## 테스트
- EditMode baseline 유지 (컴파일 확인, MonoBehaviour라 신규 테스트 없음)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

---

# PR D — `BossData.asset`에 6단계 덧붙이기 (A 병합 후, B와 병렬)

### Task 4: 소환 6단계 데이터 (TDD)

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git checkout -b feature/boss-summon-stage-data
```

**Files:**
- Create: `unity/Assets/Tests/EditMode/BossDataAssetTests.cs`
- Modify: `unity/Assets/_Project/Data/BossData.asset` (임시 스크립트로 새 배열만 덧붙임)

**Interfaces:**
- Consumes (A): `BossData.summonStages`, `BossSummonStage.Total`
- Produces (Task 5가 사용): `BossData.asset`의 `summonStages` 6개

이 에셋은 기존 손 입력 데이터(패턴 수치)가 들어 있어 통째로 다시 만들지 않는다. **새 키 하나만 파일 끝에 덧붙이고**, 기존 `summonCount` 줄은 지우지 않는다(C가 병합되기 전에는 기존 소환이 이 값을 읽는다).

- [ ] **Step 1: 실패하는 무결성 테스트 작성**

`unity/Assets/Tests/EditMode/BossDataAssetTests.cs`:

```csharp
using NUnit.Framework;
using UnityEditor;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    /// <summary>보스 소환 단계 데이터가 구조적으로 올바른지 확인한다. 마릿수·임계의 절대값은 플레이로 계속 바뀌어서 검증하지 않는다.</summary>
    public class BossDataAssetTests
    {
        private const string AssetPath = "Assets/_Project/Data/BossData.asset";

        private static BossData Load()
        {
            var data = AssetDatabase.LoadAssetAtPath<BossData>(AssetPath);
            Assert.IsNotNull(data, $"{AssetPath}를 불러올 수 없습니다.");
            return data;
        }

        [Test]
        public void HasSixSummonStages()
        {
            var stages = Load().summonStages;
            Assert.IsNotNull(stages, "summonStages가 비어 있습니다.");
            Assert.AreEqual(6, stages.Length);
        }

        [Test]
        public void Thresholds_AreInsideZeroAndOne_AndStrictlyDescending()
        {
            var stages = Load().summonStages;
            Assert.IsNotNull(stages);

            for (int i = 0; i < stages.Length; i++)
            {
                Assert.Greater(stages[i].healthThreshold, 0f, $"{i + 1}단계 임계는 0보다 커야 합니다.");
                Assert.Less(stages[i].healthThreshold, 1f, $"{i + 1}단계 임계는 1보다 작아야 합니다.");

                if (i > 0)
                    Assert.Less(stages[i].healthThreshold, stages[i - 1].healthThreshold,
                        $"{i + 1}단계 임계는 {i}단계보다 낮아야 합니다.");
            }
        }

        [Test]
        public void EveryStage_SummonsAtLeastOneMob()
        {
            var stages = Load().summonStages;
            Assert.IsNotNull(stages);

            for (int i = 0; i < stages.Length; i++)
                Assert.Greater(stages[i].Total, 0, $"{i + 1}단계가 아무것도 소환하지 않습니다.");
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해 실패 확인**

"테스트 실행"을 돌린다. Expected: 컴파일 통과, 새 3개가 `summonStages가 비어 있습니다`로 실패.

- [ ] **Step 3: 덧붙이기 스크립트 작성·실행**

일회용이라 저장소에 커밋하지 않고 스크래치패드에 둔다. `append_boss_summon_stages.js`:

```javascript
const fs = require('fs');
const path = 'C:/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity/Assets/_Project/Data/BossData.asset';

let y = fs.readFileSync(path, 'utf8');

if (y.includes('summonStages:')) {
  console.log('summonStages가 이미 있어 아무것도 하지 않습니다.');
  process.exit(0);
}
if (!y.includes('phaseTwoThreshold:')) {
  console.error('phaseTwoThreshold를 찾지 못했습니다. 에셋 구조가 예상과 다릅니다.');
  process.exit(1);
}

// 임계(내림차순)와 마릿수: 기획서 확정 표. 임계는 시작값이다.
const stages = [
  { t: 0.85, basic: 4, california: 0, mid: 0 },
  { t: 0.70, basic: 0, california: 4, mid: 0 },
  { t: 0.55, basic: 3, california: 3, mid: 0 },
  { t: 0.40, basic: 0, california: 6, mid: 0 },
  { t: 0.25, basic: 8, california: 8, mid: 0 },
  { t: 0.10, basic: 0, california: 0, mid: 2 },
];

const nl = y.includes('\r\n') ? '\r\n' : '\n';
if (!y.endsWith(nl)) y += nl;

y += `  summonStages:${nl}`;
for (const s of stages) {
  y += `  - healthThreshold: ${s.t}${nl}`;
  y += `    basicCount: ${s.basic}${nl}`;
  y += `    californiaCount: ${s.california}${nl}`;
  y += `    midCount: ${s.mid}${nl}`;
}

fs.writeFileSync(path, y);
console.log('BossData.asset에 summonStages 6개를 덧붙였습니다.');
```

스크립트 파일은 스크래치패드 디렉터리(`C:\Users\wnsdn\AppData\Local\Temp\claude\C--Users-wnsdn-Desktop-----------------\f31f848d-0d53-46ab-875f-6943ac14e312\scratchpad\append_boss_summon_stages.js`)에 저장하고 실행한다:

```bash
node "C:\Users\wnsdn\AppData\Local\Temp\claude\C--Users-wnsdn-Desktop-----------------\f31f848d-0d53-46ab-875f-6943ac14e312\scratchpad\append_boss_summon_stages.js"
```

Expected: `BossData.asset에 summonStages 6개를 덧붙였습니다.`

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git diff --stat unity/Assets/_Project/Data/BossData.asset   # 추가된 줄만 있어야 한다(삭제 0)
```

- [ ] **Step 4: 테스트 실행해 통과 확인**

"테스트 실행"을 돌린다. Expected: `error CS` 없음, baseline + 3, 실패는 알려진 기존 1건뿐. 끝나면 `rm -f TestResults.xml`.
`.meta` 확인:

```bash
git status --short unity/Assets/Tests/EditMode/BossDataAssetTests.cs unity/Assets/Tests/EditMode/BossDataAssetTests.cs.meta
```

**실패 시 대처:** 에셋 로드는 되는데 `summonStages`가 안 읽히면 스크립트를 고치지 말고, 사용자가 에디터에서 `BossData` 에셋의 `Summon Stages`를 Size 6으로 직접 입력한다(표는 스크립트의 `stages` 배열 그대로).

- [ ] **Step 5: 커밋, 푸시, PR D**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Data/BossData.asset unity/Assets/Tests/EditMode/BossDataAssetTests.cs unity/Assets/Tests/EditMode/BossDataAssetTests.cs.meta
git commit -m "$(cat <<'EOF'
feat: 보스 소환 6단계 데이터(BossData.summonStages) 추가

기획서 확정 표(4/4/6/6/16/2마리)와 임계 85·70·55·40·25·10%를 시작값으로
BossData.asset 끝에 덧붙였다. 기존 필드는 건드리지 않는다.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/boss-summon-stage-data
gh pr create --title "feat: 보스 소환 6단계 데이터 (보스 소환 D)" --body "$(cat <<'EOF'
## 요약
`BossData.asset`에 소환 6단계(`summonStages`)를 덧붙였습니다. 기획서 확정 마릿수와 임계 85/70/55/40/25/10%가 시작값이고 인스펙터에서 플레이하며 조정할 수 있습니다. 기존 필드는 안 건드렸고, 아직 이 배열을 읽는 코드가 없어 게임 동작은 바뀌지 않습니다.

- 스펙: `docs/superpowers/specs/2026-09-27-boss-summon-stages-design.md`
- 계획: `docs/superpowers/plans/2026-09-27-boss-summon-stages.md` (PR D)
- 선행 PR: A(`summonStages` 필드) — B·C와는 독립이라 병렬로 올립니다.

## 테스트
- EditMode baseline + 3 통과 (알려진 기존 실패 1건 제외)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

---

# PR C — 소환 전환, 원자적 (A·B 병합 후)

### Task 3: 체력 임계 소환 + 무작위 패턴에서 소환 제거

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git log --oneline -4   # A, B 병합 커밋이 보여야 한다
git checkout -b feature/boss-summon-by-health
```

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Enemies/Boss/BossPatternScheduler.cs`
- Modify: `unity/Assets/Tests/EditMode/BossPatternSchedulerTests.cs`
- Modify: `unity/Assets/_Project/Scripts/Enemies/Boss/BossController.cs`
- Modify: `unity/Assets/_Project/Scripts/Enemies/Boss/SummonPattern.cs`
- Modify: `unity/Assets/_Project/Scripts/Data/BossData.cs`

**Interfaces:**
- Consumes (A): `BossSummonLogic.CrossedStageCount`, `BossData.summonStages`, `BossSummonStage`
- Consumes (B): `SummonPattern.FireStage(BossSummonStage, float)`
- Produces: `BossPatternType { Meteor, Charge }`(`Summon` 제거), `BossPatternScheduler.MaxConsecutive`(= 2),
  `BossPatternScheduler.SelectNext(BossPatternType previous, int consecutiveCount, int phase, float roll)`,
  `BossPhaseValues.summonCount` 제거, `SummonPattern.Fire(BossPhaseValues)` 제거

**이 태스크는 한 PR로 원자적으로 간다.** `BossPatternType.Summon`, `summonCount`, `SummonPattern.Fire`는 `BossController`·테스트가 동시에 바뀌어야 컴파일된다. 아래 순서로 전부 고친 뒤 한 번에 컴파일을 확인한다.

- [ ] **Step 1: 지울 것을 쓰는 곳을 전부 확인**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요\unity"
grep -rn "summonCount\|BossPatternType.Summon\|\.Fire(fired)\|SelectNext(" Assets --include=*.cs
```

Expected: `BossController.cs`(Summon 사용 3곳, `SelectNext` 호출 1곳), `SummonPattern.cs`(`summonCount`), `BossData.cs`(`summonCount`), `BossPatternScheduler.cs`, `BossPatternSchedulerTests.cs`. **이 밖에 나오는 파일이 있으면 그것도 이 PR에서 같이 고친다.**

- [ ] **Step 2: 스케줄러 테스트를 새 규칙으로 재작성 (실패하는 테스트)**

`unity/Assets/Tests/EditMode/BossPatternSchedulerTests.cs` 전체를:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using SushiSurvival.Enemies.Boss;

namespace SushiSurvival.EditModeTests
{
    public class BossPatternSchedulerTests
    {
        private static readonly BossPatternType[] All =
        {
            BossPatternType.Meteor, BossPatternType.Charge
        };

        [Test]
        public void AfterMaxConsecutive_NeverRepeatsThePreviousPattern()
        {
            foreach (var previous in All)
            {
                for (int phase = 1; phase <= 2; phase++)
                {
                    for (float roll = 0f; roll < 1f; roll += 0.05f)
                        Assert.AreNotEqual(previous,
                            BossPatternScheduler.SelectNext(previous, BossPatternScheduler.MaxConsecutive, phase, roll));
                }
            }
        }

        [Test]
        public void BelowMaxConsecutive_CanRepeatThePreviousPattern()
        {
            Assert.AreEqual(BossPatternType.Meteor,
                BossPatternScheduler.SelectNext(BossPatternType.Meteor, 1, 1, 0f));
            Assert.AreEqual(BossPatternType.Charge,
                BossPatternScheduler.SelectNext(BossPatternType.Charge, 1, 2, 0.99f));
        }

        [Test]
        public void PhaseOne_SplitsMeteorAndCharge()
        {
            // 1페이즈 가중치는 메테오 5 : 돌진 2라서 roll 5/7(약 0.714) 미만이 메테오다.
            Assert.AreEqual(BossPatternType.Meteor,
                BossPatternScheduler.SelectNext(BossPatternType.Charge, 1, 1, 0.0f));
            Assert.AreEqual(BossPatternType.Meteor,
                BossPatternScheduler.SelectNext(BossPatternType.Charge, 1, 1, 0.7f));
            Assert.AreEqual(BossPatternType.Charge,
                BossPatternScheduler.SelectNext(BossPatternType.Charge, 1, 1, 0.75f));
            Assert.AreEqual(BossPatternType.Charge,
                BossPatternScheduler.SelectNext(BossPatternType.Charge, 1, 1, 0.999f));
        }

        [Test]
        public void PhaseTwo_FavorsCharge()
        {
            // 2페이즈 가중치는 메테오 3 : 돌진 5라서 돌진이 더 넓은 구간을 차지한다.
            int charge = 0;
            int meteor = 0;
            for (int i = 0; i < 100; i++)
            {
                var next = BossPatternScheduler.SelectNext(BossPatternType.Meteor, 1, 2, i / 100f);
                if (next == BossPatternType.Charge) charge++;
                if (next == BossPatternType.Meteor) meteor++;
            }

            Assert.AreEqual(100, charge + meteor);
            Assert.Greater(charge, meteor);
        }

        [Test]
        public void OutOfRangeRoll_StillReturnsAValidPattern()
        {
            var max = BossPatternScheduler.MaxConsecutive;
            Assert.AreNotEqual(BossPatternType.Meteor,
                BossPatternScheduler.SelectNext(BossPatternType.Meteor, max, 1, -1f));
            Assert.AreNotEqual(BossPatternType.Meteor,
                BossPatternScheduler.SelectNext(BossPatternType.Meteor, max, 1, 5f));
        }

        [Test]
        public void EveryPatternIsReachable()
        {
            foreach (int phase in new[] { 1, 2 })
            {
                var seen = new HashSet<BossPatternType>();
                foreach (var previous in All)
                {
                    for (float roll = 0f; roll < 1f; roll += 0.01f)
                        seen.Add(BossPatternScheduler.SelectNext(previous, 1, phase, roll));
                }

                Assert.AreEqual(2, seen.Count);
            }
        }
    }
}
```

- [ ] **Step 3: 스케줄러 구현**

`unity/Assets/_Project/Scripts/Enemies/Boss/BossPatternScheduler.cs` 전체를:

```csharp
namespace SushiSurvival.Enemies.Boss
{
    public enum BossPatternType
    {
        /// <summary>빨간 구슬 — 메테오 낙하 광역기.</summary>
        Meteor,
        /// <summary>붉게 번쩍이며 멈춘 뒤 플레이어 쪽으로 곧장 돌진.</summary>
        Charge
    }

    /// <summary>
    /// 다음 무작위 패턴을 고른다. 소환은 체력 임계로 따로 발동하므로 여기엔 없다. 같은 패턴은
    /// 최대 <see cref="MaxConsecutive"/>번까지 연속으로 나올 수 있고, 그 뒤엔 다른 패턴이 나온다 —
    /// 후보가 둘뿐이라 연속을 아예 막으면 순서가 완전히 고정돼 외워서 피할 수 있게 된다.
    /// 1페이즈는 메테오 위주, 2페이즈는 돌진 위주로 가중치를 둔다.
    /// </summary>
    public static class BossPatternScheduler
    {
        public const int MaxConsecutive = 2;

        private static readonly BossPatternType[] All =
        {
            BossPatternType.Meteor, BossPatternType.Charge
        };

        // All와 같은 순서: 메테오, 돌진.
        private static readonly float[] PhaseOneWeights = { 5f, 2f };
        private static readonly float[] PhaseTwoWeights = { 3f, 5f };

        /// <param name="consecutiveCount">previous가 지금까지 연속으로 나온 횟수.</param>
        /// <param name="roll">0 이상 1 미만의 난수. 밖에서 받아 테스트가 결과를 고정할 수 있게 한다.</param>
        public static BossPatternType SelectNext(BossPatternType previous, int consecutiveCount, int phase, float roll)
        {
            float[] weights = phase >= 2 ? PhaseTwoWeights : PhaseOneWeights;
            bool excludePrevious = consecutiveCount >= MaxConsecutive;

            float total = 0f;
            for (int i = 0; i < All.Length; i++)
            {
                if (excludePrevious && All[i] == previous) continue;
                total += weights[i];
            }

            float target = System.Math.Max(0f, System.Math.Min(roll, 0.999999f)) * total;

            float running = 0f;
            BossPatternType last = All[0];
            for (int i = 0; i < All.Length; i++)
            {
                if (excludePrevious && All[i] == previous) continue;

                last = All[i];
                running += weights[i];
                if (target < running) return All[i];
            }

            return last;
        }
    }
}
```

- [ ] **Step 4: `SummonPattern.cs`에서 기존 `Fire` 제거**

`unity/Assets/_Project/Scripts/Enemies/Boss/SummonPattern.cs`에서 `public void Fire(BossPhaseValues values) { ... }` 메서드 전체를 지운다(`FireStage`는 남긴다). `using SushiSurvival.Data;`는 `BossSummonStage` 때문에 계속 필요하다.

- [ ] **Step 5: `BossData.cs`에서 `summonCount` 제거**

`unity/Assets/_Project/Scripts/Data/BossData.cs`의 `BossPhaseValues`에서:

```csharp
        [Header("소환")]
        public int summonCount;
        [Tooltip("플레이어로부터의 소환 링 반경.")]
        public float summonRadius;
```
를 다음으로 바꾼다:

```csharp
        [Header("소환")]
        [Tooltip("플레이어로부터의 소환 링 반경.")]
        public float summonRadius;
```

- [ ] **Step 6: `BossController.cs` 수정**

필드 영역(`_patternTimer` 근처)에 추가:

```csharp
        private int _consecutiveCount;
        private int _nextSummonStage;
        private readonly Queue<int> _summonQueue = new Queue<int>();
```

`Activate` 안의 이 부분을:

```csharp
            _previousPattern = BossPatternType.Summon;
            // 등장하자마자 잡몹을 뿌리거나 돌진하면 등장 연출이 묻히므로 첫 패턴은 항상 메테오다.
            _firstCast = true;
```
다음으로 바꾼다:

```csharp
            _previousPattern = BossPatternType.Charge;
            _consecutiveCount = 0;
            _nextSummonStage = 0;
            _summonQueue.Clear();
            // 등장하자마자 돌진하면 등장 연출이 묻히므로 첫 패턴은 항상 메테오다.
            _firstCast = true;
```

`Update()`와 `Cast()` 전체를 다음으로 교체하고, 새 메서드 두 개(`QueueCrossedSummons`, `CastSummonStage`)를 `UpdatePhase()` 아래에 둔다:

```csharp
        private void Update()
        {
            if (!_active || _casting) return;

            UpdatePhase();
            QueueCrossedSummons();

            if (animator != null)
                animator.SetBool(IsMovingHash, true);

            // 소환은 체력 임계로 발동하는 사건이라 패턴 타이머보다 먼저 처리한다.
            // 시전 중이었다면 위의 _casting 가드 덕에 그 시전이 끝난 뒤에야 여기까지 온다.
            if (_summonQueue.Count > 0)
            {
                StartCoroutine(CastSummonStage(_summonQueue.Dequeue()));
                return;
            }

            _patternTimer -= Time.deltaTime;
            if (_patternTimer > 0f) return;

            BossPatternType next = _firstCast
                ? BossPatternType.Meteor
                : BossPatternScheduler.SelectNext(_previousPattern, _consecutiveCount, _phase, Random.value);
            _firstCast = false;

            StartCoroutine(Cast(next));
        }

        private void UpdatePhase()
        {
            int phase = BossPhaseLogic.GetPhase(
                _enemy.CurrentHealth, bossData.maxHealth, bossData.phaseTwoThreshold);

            if (phase == _phase) return;

            _phase = phase;
            _ai.MoveScale = bossData.GetPhaseValues(_phase).moveScale;

            if (spriteFlasher != null)
                spriteFlasher.Flash(Color.red, phaseFlashDuration);
        }

        /// <summary>한 번의 큰 피해로 임계를 여러 개 넘어도 단계를 건너뛰지 않고 전부 큐에 쌓는다.</summary>
        private void QueueCrossedSummons()
        {
            var stages = bossData.summonStages;
            if (stages == null) return;

            int crossed = BossSummonLogic.CrossedStageCount(
                _enemy.CurrentHealth, bossData.maxHealth, stages, _nextSummonStage);

            for (int i = 0; i < crossed; i++)
                _summonQueue.Enqueue(_nextSummonStage + i);

            _nextSummonStage += crossed;
        }

        private IEnumerator Cast(BossPatternType pattern)
        {
            _casting = true;
            _consecutiveCount = pattern == _previousPattern ? _consecutiveCount + 1 : 1;
            _previousPattern = pattern;

            _ai.MoveScale = 0f;

            if (animator != null)
                animator.SetBool(IsMovingHash, false);

            if (pattern == BossPatternType.Charge)
            {
                yield return ChargeRoutine();
            }
            else
            {
                if (animator != null)
                    animator.SetTrigger(CastMeteorHash);

                yield return new WaitForSeconds(castDuration);

                if (meteorPattern != null)
                    meteorPattern.Fire(bossData.GetPhaseValues(_phase));
            }

            BossPhaseValues values = bossData.GetPhaseValues(_phase);

            _ai.LockedDirection = Vector2.zero;
            _ai.MoveScale = values.moveScale;
            _patternTimer = values.patternInterval;
            _casting = false;
        }

        /// <summary>
        /// 체력 임계 소환 한 단계. 무작위 패턴 순서(_previousPattern·연속 횟수)와 패턴 타이머는
        /// 건드리지 않는다 — 소환은 그 순서의 일부가 아니다. 시전 중엔 Update가 멈춰 있어서 타이머도 흐르지 않는다.
        /// </summary>
        private IEnumerator CastSummonStage(int stageIndex)
        {
            _casting = true;
            _ai.MoveScale = 0f;

            if (animator != null)
            {
                animator.SetBool(IsMovingHash, false);
                animator.SetTrigger(CastSummonHash);
            }

            yield return new WaitForSeconds(castDuration);

            BossPhaseValues values = bossData.GetPhaseValues(_phase);

            if (summonPattern != null)
                summonPattern.FireStage(bossData.summonStages[stageIndex], values.summonRadius);

            _ai.LockedDirection = Vector2.zero;
            _ai.MoveScale = values.moveScale;
            _casting = false;
        }
```

(`ChargeRoutine()`은 이 태스크에서 손 안 댄다 — 돌진 연출은 별도 작업이다.)

- [ ] **Step 7: 컴파일 + 회귀 확인**

"테스트 실행"을 돌린다. Expected: `error CS` 없음, 스케줄러 테스트 6개 통과, 실패는 알려진 기존 1건뿐. 개수는 baseline − 5(기존 스케줄러 테스트 5개) + 6(새 6개) = baseline + 1. `error CS`가 나면 Step 1의 grep 결과에서 놓친 소비자가 있다는 뜻이니 그 파일을 같이 고친다. 끝나면 `rm -f TestResults.xml`.

- [ ] **Step 8: 커밋, 푸시, PR C**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/Enemies/Boss/BossPatternScheduler.cs unity/Assets/Tests/EditMode/BossPatternSchedulerTests.cs unity/Assets/_Project/Scripts/Enemies/Boss/BossController.cs unity/Assets/_Project/Scripts/Enemies/Boss/SummonPattern.cs unity/Assets/_Project/Scripts/Data/BossData.cs
git status --short
git commit -m "$(cat <<'EOF'
feat: 보스 소환을 체력 임계 6단계로 전환, 무작위 패턴은 메테오·돌진만

BossController가 체력이 소환 임계에 닿을 때마다 단계를 큐에 쌓아
시전이 끝난 뒤 순서대로 소환한다. 기존 무작위 소환 패턴과
BossPhaseValues.summonCount, SummonPattern.Fire를 제거했다.
스케줄러는 같은 패턴을 최대 2연속까지 허용한다.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/boss-summon-by-health
gh pr create --title "feat: 보스 소환 체력 임계 전환 (보스 소환 C)" --body "$(cat <<'EOF'
## 요약
보스 소환을 "가중치 무작위 패턴"에서 "체력 임계 6단계 사건"으로 바꿉니다. 보스 체력이 각 임계(`BossData.summonStages`)에 닿으면 소환 단계가 큐에 쌓이고, 진행 중인 시전이 끝난 뒤 순서대로 하나씩 발동합니다. 큰 피해로 임계를 여러 개 넘어도 단계를 건너뛰지 않습니다. 무작위 패턴은 메테오·돌진 둘만 남고, 후보가 둘이라 같은 패턴을 최대 2연속까지 허용합니다.

기존 무작위 소환 패턴·`BossPhaseValues.summonCount`·`SummonPattern.Fire`는 소비자(`BossController`, 스케줄러 테스트)와 함께 이 PR에서 한꺼번에 제거했습니다(따로 지우면 컴파일이 깨집니다).

- 스펙: `docs/superpowers/specs/2026-09-27-boss-summon-stages-design.md`
- 계획: `docs/superpowers/plans/2026-09-27-boss-summon-stages.md` (PR C)
- 선행 PR: A(판정 로직), B(`FireStage`·풀 배선)

## 협업자 확인 부탁
`BossController`·`BossPatternScheduler`·`SummonPattern`·`BossData`(보스 코드·데이터)를 수정합니다. 씬 연결(풀 2개 추가)은 PR E에서 합니다 — **PR D(소환 6단계 데이터)와 PR E가 병합되기 전에는 소환이 발동하지 않습니다.**

## 테스트
- EditMode baseline + 1 통과 (스케줄러 테스트 5개 → 6개 재작성, 알려진 기존 실패 1건 제외)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

---

# PR E — BossScene 배선 (B·C·D 모두 병합 후, 사용자 에디터 작업)

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git log --oneline -8   # B, C, D 병합 커밋이 다 보여야 한다
git checkout -b feature/boss-summon-scene-wiring
```

### Task 5: `BossScene`에 캘리포니아롤·중형몹 풀 추가 (사용자 에디터 작업)

에이전트는 씬 Inspector 값을 대신 편집하지 않는다(프로젝트 관례). 아래 순서를 사용자가 따라 하고, 에이전트는 저장된 파일을 읽어 배선을 검증한다. **풀 하나당 GameObject 하나**여야 한다(한 오브젝트에 `GameObjectPool`이 둘이면 반환된 몹이 전부 첫 풀로 들어가 프리팹이 섞인다).

#### A. 풀 오브젝트 두 개 만들기

**1.** `BossScene`을 연다. Hierarchy에서 기존 BasicMob 소환 풀 오브젝트를 찾는다(`Boss Fight Director`의 `Summon Mob Pool` 필드가 가리키는 오브젝트를 클릭하면 빠르다).

**2.** 그 오브젝트를 선택 → **Ctrl+D**로 복제 → 이름을 **`CaliforniaRollPool`**로 바꾼다. Inspector의 `Game Object Pool` 컴포넌트에서 **Prefab**을 `Assets/_Project/Prefabs/CaliforniaRoll`로 바꾼다.

**3.** 같은 방식으로 한 번 더 복제해 이름을 **`MidBossPool`**로, **Prefab**을 `Assets/_Project/Prefabs/MidBos`로 바꾼다.

**4.** 풀 세 개(`BasicMob` 소환 풀, `CaliforniaRollPool`, `MidBossPool`) 모두 **Prewarm Count를 16**으로 올린다(5단계에서 일반 8 + 캘리 8이 한꺼번에 나온다).

#### B. 디렉터에 연결

**1.** `BossFightDirector` 컴포넌트가 붙은 오브젝트를 선택한다.

**2.** 새로 생긴 필드에 연결한다:
   - **Summon California Pool** = `CaliforniaRollPool`
   - **Summon Mid Mob Pool** = `MidBossPool`
   - (기존 **Summon Mob Pool**은 이미 BasicMob 풀에 연결돼 있으니 그대로 둔다.)

**3.** 씬 저장(Ctrl+S).

---

### 에이전트 검증

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요\unity"
git status --short Assets/_Project/Scenes/BossScene.unity
echo "--- 디렉터 필드 (0이면 안 됨) ---"
grep -n "summonMobPool:\|summonCaliforniaPool:\|summonMidMobPool:" Assets/_Project/Scenes/BossScene.unity
echo "--- 풀이 참조하는 프리팹 (BasicMob 75fbc066, CaliforniaRoll ec018489, MidBos b3538ca2) ---"
grep -n "75fbc066937e41c458bbf25de5923ed3\|ec01848928873f548b448778de3e6190\|b3538ca2802d9d649904d499a17dabc8" Assets/_Project/Scenes/BossScene.unity
echo "--- prewarm ---"
grep -n "prewarmCount:" Assets/_Project/Scenes/BossScene.unity
```

Expected: `summonCaliforniaPool`/`summonMidMobPool`이 `{fileID: 0}`이 아니다. 세 프리팹 guid가 각각 풀에 연결돼 있다. `prewarmCount`가 세 풀 모두 16 이상이다. 비어 있는 필드가 있으면 사용자에게 알려준다.
예전 `BossDirector`(GameScene용)가 실제로 씬에 있는지도 확인한다(있으면 그 새 필드는 비어 있어도 컴파일에는 무관하지만, 있다면 사용자에게 알려준다):

```bash
grep -c "guid: $(grep -m1 'guid:' Assets/_Project/Scripts/Enemies/Boss/BossDirector.cs.meta | awk '{print $2}')" Assets/_Project/Scenes/GameScene.unity Assets/_Project/Scenes/BossScene.unity
```

- [ ] **Step 1: 컴파일 + 회귀 확인**

"테스트 실행"을 돌린다(씬만 바뀌어 컴파일에 영향은 없지만 습관대로). Expected: `error CS` 없음, 실패는 알려진 기존 1건뿐.

- [ ] **Step 2: Play 모드 전체 흐름 확인 (사용자)**

보스전까지 가서(테스트용으로 GameScene의 `bossSpawnTime`을 20초로 낮춰도 된다 — 확인 후 300으로 복구) 보스 체력을 깎으며 확인한다:

- [ ] 체력 85% 근처에서 보스가 소환 시전 → **일반 몹 4마리**가 플레이어를 둘러싼 링에 등장
- [ ] 70% → **캘리 4마리**, 55% → 일반 3 + 캘리 3(섞여서 등장), 40% → 캘리 6
- [ ] 25% → **일반 8 + 캘리 8(16마리)**이 한꺼번에 나와도 프레임이 심하게 안 떨어지는지
- [ ] 10% → **중형몹 2마리**
- [ ] 메테오·돌진 시전 중에 임계가 와도 시전이 끝난 뒤에 소환되는지
- [ ] 강한 공격으로 한 번에 임계를 두 개 이상 넘었을 때 소환이 **순서대로 연달아** 나오는지(단계가 빠지지 않는지)
- [ ] 소환 외의 무작위 패턴이 메테오·돌진만 나오고, 같은 패턴이 3연속으로는 안 나오는지
- [ ] 50% 부근에서 2페이즈로 넘어가 이동이 빨라지는 것이 소환과 무관하게 정상 동작하는지
- [ ] 소환된 몹을 잡으면 젬이 떨어지고 풀로 정상 반환되는지(같은 몹이 계속 쌓이지 않는지)

- [ ] **Step 3: 커밋, 푸시, PR E**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scenes/BossScene.unity
git status --short
git commit -m "$(cat <<'EOF'
feat: BossScene에 캘리포니아롤·중형몹 소환 풀 추가 및 연결

CaliforniaRollPool, MidBossPool을 만들어 BossFightDirector에 연결하고
세 소환 풀의 prewarm을 16으로 올렸다(5단계 16마리 동시 소환 대비).

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/boss-summon-scene-wiring
gh pr create --title "feat: BossScene 소환 풀 배선 (보스 소환 E)" --body "$(cat <<'EOF'
## 요약
보스 소환 6단계의 마지막 조각. `BossScene`에 캘리포니아롤·중형몹 풀을 추가해 `BossFightDirector`에 연결하고, 세 소환 풀의 prewarm을 16으로 올렸습니다(5단계에서 16마리가 동시에 나옵니다).

- 스펙: `docs/superpowers/specs/2026-09-27-boss-summon-stages-design.md`
- 계획: `docs/superpowers/plans/2026-09-27-boss-summon-stages.md` (PR E)
- 선행 PR: A, B, C, D 전부 병합됨

## 협업자 확인 부탁
`BossScene.unity`(협업자가 만든 씬)에 풀 오브젝트 2개를 추가하고 디렉터 필드 2개를 연결했습니다. 기존 오브젝트는 안 건드렸고, 기존 BasicMob 풀은 prewarm 값만 올렸습니다.

## 테스트
- EditMode baseline 유지 (알려진 기존 실패 1건 제외)
- Play 모드로 6단계 소환(임계별 마릿수·종류), 시전 중 임계 대기, 여러 임계 동시 통과, 메테오·돌진 연속 규칙, 16마리 동시 소환 성능 확인 완료(사용자)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

**여기서 멈추고 사용자가 PR E를 병합하길 기다린다.** 병합 후 `main` 동기화, 브랜치 정리, 메모리(`sushi-survival-project.md`) 업데이트로 마무리하고, 이어서 3-2(돌진 임팩트) 스펙으로 넘어간다.
