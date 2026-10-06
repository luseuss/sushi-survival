# 보조 요정 시스템 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 와사비 알현 성공 보상을 "무기 교체/스탯 버프"에서 **보조 요정(최대 3마리, 가까운 적에게 자동 투사체, 성공마다 소환/강화 선택)**으로 바꾸고, 가위바위보 승률을 40%로 낮춘다.

**Architecture:** 판정·좌표 로직은 순수 정적 클래스(`FairySlotLogic`·`FairyTargetLogic`·`FairyChoiceLogic`·`RpsOpponentLogic`)로 만들어 EditMode 테스트한다. `FairyController`(씬 오브젝트)가 요정 목록을 들고, `Fairy`(MonoBehaviour)가 슬롯을 따라다니며 기존 `Projectile`+`GameObjectPool`로 발사한다. 요정 선택은 기존 레벨업 카드(`LevelUpPanel`)를 `FairyOption : IUpgradeOption`으로 재사용한다.

**Tech Stack:** Unity 2022.3.62f3, C#, Legacy uGUI, NUnit EditMode 테스트(`SushiSurvival.EditModeTests` 어셈블리, 런타임은 `SushiSurvival.Runtime`).

**Spec:** `docs/superpowers/specs/2026-10-06-fairy-system-design.md` (실행자는 스펙도 같이 읽는다)

## Global Constraints

- `main`에 직접 커밋·푸시·머지 금지. PR마다 브랜치를 새로 판다(**최신 `main`에서**). 머지 버튼은 사람만 누른다.
- 씬(`GameScene.unity`·`BossScene.unity`)·프리팹 편집은 **사용자가 에디터에서** 한다. 이 계획서의 코드 PR(A~D)은 씬을 건드리지 않는다.
- 버프·스탯은 `Core/StatSystem`(`PlayerStats.GetValue`)을 재사용한다. 별도 버프 시스템 금지.
- 수치(요정 피해·쿨타임·사거리·모션)는 전부 인스펙터/`WeaponData`로 노출한다(하드코딩 금지).
- UI는 Legacy `UnityEngine.UI`만 쓴다(TextMeshPro 미설치).
- 새 코드의 네임스페이스: 요정은 `SushiSurvival.Companions`(폴더 `Scripts/Companions/` — 스펙의 `Fairy/`에서 변경: 네임스페이스와 클래스명 `Fairy` 충돌 회피), 가위바위보 로직은 `SushiSurvival.Core`.
- 테스트는 `Assets/Tests/EditMode/`, 네임스페이스 `SushiSurvival.EditModeTests`.
- 새 `.cs` 파일은 `.meta`까지 함께 커밋한다(Unity 에디터를 한 번 열어 생성). 커밋 전 `git status`로 예상 밖 파일, `git diff`로 테스트용 임시값 확인.
- 배치 테스트 명령(에디터를 **닫은 상태**에서만 동작 — 열려 있으면 `HandleProjectAlreadyOpenInAnotherInstance`로 결과 파일이 안 생긴다):
  ```bash
  cd unity && "C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" -batchmode -projectPath "$(pwd -W)" -runTests -testPlatform EditMode -testResults "$(pwd -W)/TestResults.xml" -logFile - > "$TEMP/unity_test.log" 2>&1
  grep -c "error CS" "$TEMP/unity_test.log"
  grep -o '<test-run[^>]*' TestResults.xml | grep -o 'total="[0-9]*"\|passed="[0-9]*"\|failed="[0-9]*"'
  rm -f TestResults.xml
  ```
  `-quit`을 같이 쓰지 말 것. 기준: **484개 중 483 통과**, 알려진 기존 실패는 `Portraits_AdelineAndKamarionHaveOne_InariHasNone` 1건뿐.
- 커밋 메시지 끝에 `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`, PR 본문 끝에 `🤖 Generated with [Claude Code](https://claude.com/claude-code)`.

## PR 분할과 순서

| PR | 브랜치 | 내용 | 의존 |
|---|---|---|---|
| A | `feature/rps-win-chance` | `RpsOpponentLogic` + 패널 `winChance` 0.4 | 없음 |
| B | `feature/fairy-logic` | 순수 로직 3종 + 테스트 | 없음 |
| C | `feature/fairy-components` | `Fairy`·`FairyController`·`FairyOption` (추가만) | B 머지 후 |
| D | `feature/fairy-wasabi-flow` | `LevelSystem` 와사비 흐름 전환·이월 | A, C 머지 후 |
| E | (사용자 에디터) | 씬 배선·에셋 | D 머지 후 |

A와 B는 파일이 겹치지 않아 **병렬로 진행**할 수 있다. C는 B의 순수 로직을 쓰고, D는 A·C가 먼저 머지돼 있어야 한다.
C까지는 새 코드를 아무도 부르지 않으므로(기본값이 "영향 없음") 먼저 머지돼도 게임 동작은 그대로다.
D 시작 전에 `LevelSystem.cs`·`GameManager.cs`·`BossFightDirector.cs`를 협업자 최근 커밋과 대조한다
(`git fetch` 후 `git log --oneline origin/main -- <파일>`).

---

## PR A — 가위바위보 승률 40%

### Task 1: `RpsOpponentLogic`과 패널 연결

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/RpsOpponentLogic.cs`
- Create: `unity/Assets/Tests/EditMode/RpsOpponentLogicTests.cs`
- Modify: `unity/Assets/_Project/Scripts/UI/RockPaperScissorsPanel.cs` (필드 추가, `ResolveRound`의 상대 손 선택 한 줄)

**Interfaces:**
- Produces: `public static RpsHand RpsOpponentLogic.PickOpponentHand(RpsHand player, float winChance, float roll)` — `roll`은 0 이상 1 미만. 1/3은 비김(플레이어와 같은 손), 나머지는 `winChance`만큼 플레이어가 이기는 손, 아니면 지는 손.

- [ ] **Step 1: 실패하는 테스트 작성** — `RpsOpponentLogicTests.cs`

```csharp
using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class RpsOpponentLogicTests
    {
        private static readonly RpsHand[] AllHands = { RpsHand.Rock, RpsHand.Paper, RpsHand.Scissors };

        [Test]
        public void RollInFirstThird_IsAlwaysADraw()
        {
            foreach (RpsHand player in AllHands)
            {
                Assert.AreEqual(RpsOutcome.Draw,
                    RpsRules.Resolve(player, RpsOpponentLogic.PickOpponentHand(player, 0.4f, 0f)));
                Assert.AreEqual(RpsOutcome.Draw,
                    RpsRules.Resolve(player, RpsOpponentLogic.PickOpponentHand(player, 0.4f, 0.33f)));
            }
        }

        [Test]
        public void WinChanceOne_DecisiveRollsAlwaysWin()
        {
            foreach (RpsHand player in AllHands)
            {
                foreach (float roll in new[] { 0.34f, 0.6f, 0.99f })
                {
                    Assert.AreEqual(RpsOutcome.Win,
                        RpsRules.Resolve(player, RpsOpponentLogic.PickOpponentHand(player, 1f, roll)));
                }
            }
        }

        [Test]
        public void WinChanceZero_DecisiveRollsAlwaysLose()
        {
            foreach (RpsHand player in AllHands)
            {
                foreach (float roll in new[] { 0.34f, 0.6f, 0.99f })
                {
                    Assert.AreEqual(RpsOutcome.Lose,
                        RpsRules.Resolve(player, RpsOpponentLogic.PickOpponentHand(player, 0f, roll)));
                }
            }
        }

        [Test]
        public void EvenlySpacedRolls_MatchExpectedRatios()
        {
            const int n = 9000;
            const float winChance = 0.4f;
            int draws = 0, wins = 0, losses = 0;

            for (int i = 0; i < n; i++)
            {
                float roll = (i + 0.5f) / n;
                switch (RpsRules.Resolve(RpsHand.Rock, RpsOpponentLogic.PickOpponentHand(RpsHand.Rock, winChance, roll)))
                {
                    case RpsOutcome.Draw: draws++; break;
                    case RpsOutcome.Win: wins++; break;
                    default: losses++; break;
                }
            }

            Assert.AreEqual(1f / 3f, draws / (float)n, 0.01f);
            // 비기면 다시 하므로 최종 승률 = 판정이 갈린 것 중 이긴 비율.
            Assert.AreEqual(winChance, wins / (float)(wins + losses), 0.01f);
        }

        [Test]
        public void WinChance_OutOfRange_IsClamped()
        {
            Assert.AreEqual(RpsOutcome.Win,
                RpsRules.Resolve(RpsHand.Paper, RpsOpponentLogic.PickOpponentHand(RpsHand.Paper, 5f, 0.9f)));
            Assert.AreEqual(RpsOutcome.Lose,
                RpsRules.Resolve(RpsHand.Paper, RpsOpponentLogic.PickOpponentHand(RpsHand.Paper, -2f, 0.9f)));
        }
    }
}
```

- [ ] **Step 2: 실행해 실패 확인** — 배치 테스트(위 Global Constraints) 또는 에디터 Test Runner. 기대: `RpsOpponentLogic` 미정의 컴파일 에러.

- [ ] **Step 3: 구현** — `RpsOpponentLogic.cs`

```csharp
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 상대 손을 승률에 맞춰 뽑는다. 비김은 1/3로 그대로 두고(재대결이라 승률에 영향 없음),
    /// 판정이 갈릴 때만 플레이어가 이길 비율을 winChance로 맞춘다.
    /// </summary>
    public static class RpsOpponentLogic
    {
        private const float DrawShare = 1f / 3f;

        /// <param name="roll">0 이상 1 미만의 난수.</param>
        public static RpsHand PickOpponentHand(RpsHand player, float winChance, float roll)
        {
            if (roll < DrawShare) return player;

            float decisive = (roll - DrawShare) / (1f - DrawShare);
            bool playerWins = decisive < Mathf.Clamp01(winChance);

            return playerWins ? HandBeatenBy(player) : HandThatBeats(player);
        }

        private static RpsHand HandBeatenBy(RpsHand hand) => hand switch
        {
            RpsHand.Rock => RpsHand.Scissors,
            RpsHand.Paper => RpsHand.Rock,
            _ => RpsHand.Paper
        };

        private static RpsHand HandThatBeats(RpsHand hand) => hand switch
        {
            RpsHand.Rock => RpsHand.Paper,
            RpsHand.Paper => RpsHand.Scissors,
            _ => RpsHand.Rock
        };
    }
}
```

- [ ] **Step 4: 패널 연결** — `RockPaperScissorsPanel.cs`

필드 추가(`drawRetryDelay` 바로 아래):

```csharp
        [Range(0f, 1f)]
        [Tooltip("플레이어가 최종적으로 이길 확률. 비기면 다시 하므로 가위바위보 자체의 승률이다. 0.4면 열 번 중 네 번.")]
        [SerializeField] private float winChance = 0.4f;
```

`ResolveRound`의 첫 줄을 교체:

```csharp
            RpsHand opponentHand = RpsOpponentLogic.PickOpponentHand(playerHand, winChance, (float)_random.NextDouble());
```

(`_random`은 기존 `System.Random` 필드를 그대로 쓴다. `NextDouble()`은 0 이상 1 미만.)

- [ ] **Step 5: 테스트 통과 확인** — 배치 테스트. 기대: 컴파일 에러 0, 신규 5개 포함 전체 통과(기존 실패 1건만 제외).

- [ ] **Step 6: 커밋·PR**

```bash
git checkout -b feature/rps-win-chance   # 최신 main에서
git add unity/Assets/_Project/Scripts/Core/RpsOpponentLogic.cs unity/Assets/_Project/Scripts/Core/RpsOpponentLogic.cs.meta unity/Assets/Tests/EditMode/RpsOpponentLogicTests.cs unity/Assets/Tests/EditMode/RpsOpponentLogicTests.cs.meta unity/Assets/_Project/Scripts/UI/RockPaperScissorsPanel.cs
git commit -m "feat: 가위바위보 승률을 winChance(기본 0.4)로 조절"
git push -u origin feature/rps-win-chance
gh pr create --base main --title "feat: 와사비 가위바위보 승률 40%" --body "<요약·테스트 결과>"
```

PR 본문에 적을 것: 현재 승률이 50%였다는 점, 씬에 저장된 패널은 `winChance` 필드가 없던 값이라 코드 기본값 0.4가 쓰이지만 인스펙터에서 한 번 확인.

---

## PR B — 요정 순수 로직

### Task 2: `FairySlotLogic`

**Files:**
- Create: `unity/Assets/_Project/Scripts/Companions/FairySlotLogic.cs`
- Create: `unity/Assets/Tests/EditMode/FairySlotLogicTests.cs`

**Interfaces:**
- Produces:
  - `public static Vector2 FairySlotLogic.SlotOffset(int index, int count, float radius, float arcSpacingDegrees, float time, float bobAmplitude, float bobSpeed)` — 플레이어 중심 기준 슬롯 오프셋. 요정은 플레이어 **위쪽(90°) 기준 부채꼴**로 늘어선다. `count <= 0`이면 `Vector2.zero`, `index`는 `[0,count-1]`로 클램프.
  - `public static Vector2 FairySlotLogic.Follow(Vector2 current, Vector2 target, float sharpness, float deltaTime)` — 프레임레이트 무관 지수 보간.

- [ ] **Step 1: 실패하는 테스트 작성**

```csharp
using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Companions;

namespace SushiSurvival.EditModeTests
{
    public class FairySlotLogicTests
    {
        private const float Radius = 1.2f;
        private const float Arc = 60f;

        [Test]
        public void SingleFairy_SitsDirectlyAbovePlayer()
        {
            Vector2 offset = FairySlotLogic.SlotOffset(0, 1, Radius, Arc, 0f, 0f, 1f);

            Assert.AreEqual(0f, offset.x, 0.001f);
            Assert.AreEqual(Radius, offset.y, 0.001f);
        }

        [Test]
        public void ThreeFairies_FanOutSymmetricallyAroundTop()
        {
            Vector2 left = FairySlotLogic.SlotOffset(0, 3, Radius, Arc, 0f, 0f, 1f);
            Vector2 middle = FairySlotLogic.SlotOffset(1, 3, Radius, Arc, 0f, 0f, 1f);
            Vector2 right = FairySlotLogic.SlotOffset(2, 3, Radius, Arc, 0f, 0f, 1f);

            Assert.AreEqual(0f, middle.x, 0.001f);
            Assert.AreEqual(-left.x, right.x, 0.001f);
            Assert.AreEqual(left.y, right.y, 0.001f);
            Assert.Greater(right.x, 0f);
        }

        [Test]
        public void SlotsAreDistinct()
        {
            for (int count = 2; count <= 3; count++)
            {
                for (int i = 0; i < count; i++)
                {
                    for (int j = i + 1; j < count; j++)
                    {
                        Vector2 a = FairySlotLogic.SlotOffset(i, count, Radius, Arc, 0f, 0f, 1f);
                        Vector2 b = FairySlotLogic.SlotOffset(j, count, Radius, Arc, 0f, 0f, 1f);
                        Assert.Greater((a - b).magnitude, 0.3f, $"count={count}, {i} vs {j}");
                    }
                }
            }
        }

        [Test]
        public void Bob_StaysWithinAmplitude()
        {
            Vector2 rest = FairySlotLogic.SlotOffset(0, 2, Radius, Arc, 0f, 0f, 2f);

            for (float t = 0f; t < 10f; t += 0.1f)
            {
                Vector2 bobbing = FairySlotLogic.SlotOffset(0, 2, Radius, Arc, t, 0.15f, 2f);
                Assert.LessOrEqual(Mathf.Abs(bobbing.y - rest.y), 0.15f + 0.0001f);
                Assert.AreEqual(rest.x, bobbing.x, 0.0001f);
            }
        }

        [Test]
        public void NoFairies_IsZero()
        {
            Assert.AreEqual(Vector2.zero, FairySlotLogic.SlotOffset(0, 0, Radius, Arc, 0f, 0f, 1f));
        }

        [Test]
        public void OutOfRangeIndex_IsClamped()
        {
            Vector2 last = FairySlotLogic.SlotOffset(2, 3, Radius, Arc, 0f, 0f, 1f);
            Assert.AreEqual(last, FairySlotLogic.SlotOffset(9, 3, Radius, Arc, 0f, 0f, 1f));
            Assert.AreEqual(FairySlotLogic.SlotOffset(0, 3, Radius, Arc, 0f, 0f, 1f),
                            FairySlotLogic.SlotOffset(-4, 3, Radius, Arc, 0f, 0f, 1f));
        }

        [Test]
        public void Follow_ZeroDelta_StaysPut()
        {
            Assert.AreEqual(new Vector2(1f, 2f),
                FairySlotLogic.Follow(new Vector2(1f, 2f), new Vector2(9f, 9f), 8f, 0f));
        }

        [Test]
        public void Follow_HugeSharpness_ReachesTarget()
        {
            Vector2 result = FairySlotLogic.Follow(Vector2.zero, new Vector2(3f, -2f), 1000f, 1f);
            Assert.AreEqual(3f, result.x, 0.001f);
            Assert.AreEqual(-2f, result.y, 0.001f);
        }

        [Test]
        public void Follow_IsFrameRateIndependent()
        {
            Vector2 target = new Vector2(4f, 0f);

            Vector2 oneStep = FairySlotLogic.Follow(Vector2.zero, target, 6f, 0.2f);
            Vector2 twoSteps = FairySlotLogic.Follow(
                FairySlotLogic.Follow(Vector2.zero, target, 6f, 0.1f), target, 6f, 0.1f);

            Assert.AreEqual(oneStep.x, twoSteps.x, 0.0001f);
        }
    }
}
```

- [ ] **Step 2: 실행해 실패 확인** — 컴파일 에러(`FairySlotLogic` 없음).

- [ ] **Step 3: 구현**

```csharp
using UnityEngine;

namespace SushiSurvival.Companions
{
    public static class FairySlotLogic
    {
        private const float TopAngleDegrees = 90f;

        /// <summary>
        /// 요정은 플레이어 머리 위(90°)를 중심으로 arcSpacingDegrees 간격으로 부채꼴로 선다.
        /// bob은 마릿수와 무관하게 슬롯마다 위상을 달리해 둥둥 떠다니게 한다(y만).
        /// </summary>
        public static Vector2 SlotOffset(int index, int count, float radius, float arcSpacingDegrees,
                                         float time, float bobAmplitude, float bobSpeed)
        {
            if (count <= 0) return Vector2.zero;

            int clamped = Mathf.Clamp(index, 0, count - 1);
            float angle = (TopAngleDegrees + (clamped - (count - 1) * 0.5f) * -arcSpacingDegrees) * Mathf.Deg2Rad;

            float bob = Mathf.Sin(time * bobSpeed + clamped * 1.7f) * bobAmplitude;

            return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius + bob);
        }

        /// <summary>1 - e^(-k·dt) 보간. 프레임레이트가 달라도 같은 시간엔 같은 거리만큼 따라온다.</summary>
        public static Vector2 Follow(Vector2 current, Vector2 target, float sharpness, float deltaTime)
        {
            float t = 1f - Mathf.Exp(-Mathf.Max(0f, sharpness) * Mathf.Max(0f, deltaTime));
            return Vector2.Lerp(current, target, t);
        }
    }
}
```

> 각도 부호: `-arcSpacingDegrees`로 인덱스가 커질수록 오른쪽(+x)에 서게 한다(테스트 `ThreeFairies_FanOut…`가 이를 확인).

- [ ] **Step 4: 테스트 통과 확인** — 신규 9개 통과.

- [ ] **Step 5: 커밋** (PR B 브랜치 `feature/fairy-logic`, 최신 main에서 생성)

```bash
git add unity/Assets/_Project/Scripts/Companions/FairySlotLogic.cs unity/Assets/_Project/Scripts/Companions/FairySlotLogic.cs.meta unity/Assets/Tests/EditMode/FairySlotLogicTests.cs unity/Assets/Tests/EditMode/FairySlotLogicTests.cs.meta
git commit -m "feat: 요정 슬롯 위치·따라가기 순수 로직"
```

### Task 3: `FairyTargetLogic`

**Files:**
- Create: `unity/Assets/_Project/Scripts/Companions/FairyTargetLogic.cs`
- Create: `unity/Assets/Tests/EditMode/FairyTargetLogicTests.cs`

**Interfaces:**
- Produces: `public static int FairyTargetLogic.NearestIndex(Vector2 origin, IReadOnlyList<Vector2> positions, float maxRange)` — 사거리(`<= maxRange`) 안에서 가장 가까운 위치의 인덱스, 없으면 -1. 거리가 같으면 앞 인덱스.

- [ ] **Step 1: 실패하는 테스트 작성**

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Companions;

namespace SushiSurvival.EditModeTests
{
    public class FairyTargetLogicTests
    {
        [Test]
        public void PicksNearestWithinRange()
        {
            var positions = new List<Vector2> { new Vector2(5f, 0f), new Vector2(2f, 0f), new Vector2(3f, 0f) };

            Assert.AreEqual(1, FairyTargetLogic.NearestIndex(Vector2.zero, positions, 10f));
        }

        [Test]
        public void OutsideRange_ReturnsMinusOne()
        {
            var positions = new List<Vector2> { new Vector2(8f, 0f) };

            Assert.AreEqual(-1, FairyTargetLogic.NearestIndex(Vector2.zero, positions, 5f));
        }

        [Test]
        public void ExactlyAtRange_IsIncluded()
        {
            var positions = new List<Vector2> { new Vector2(5f, 0f) };

            Assert.AreEqual(0, FairyTargetLogic.NearestIndex(Vector2.zero, positions, 5f));
        }

        [Test]
        public void EmptyOrNull_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, FairyTargetLogic.NearestIndex(Vector2.zero, new List<Vector2>(), 5f));
            Assert.AreEqual(-1, FairyTargetLogic.NearestIndex(Vector2.zero, null, 5f));
        }

        [Test]
        public void Tie_PrefersEarlierIndex()
        {
            var positions = new List<Vector2> { new Vector2(0f, 3f), new Vector2(3f, 0f) };

            Assert.AreEqual(0, FairyTargetLogic.NearestIndex(Vector2.zero, positions, 5f));
        }

        [Test]
        public void MeasuresFromOrigin_NotFromWorldZero()
        {
            var positions = new List<Vector2> { new Vector2(10f, 10f), new Vector2(-10f, -10f) };

            Assert.AreEqual(1, FairyTargetLogic.NearestIndex(new Vector2(-9f, -9f), positions, 5f));
        }
    }
}
```

- [ ] **Step 2: 실행해 실패 확인** — 컴파일 에러.

- [ ] **Step 3: 구현**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace SushiSurvival.Companions
{
    public static class FairyTargetLogic
    {
        public static int NearestIndex(Vector2 origin, IReadOnlyList<Vector2> positions, float maxRange)
        {
            if (positions == null) return -1;

            float maxSqr = maxRange * maxRange;
            float bestSqr = float.MaxValue;
            int best = -1;

            for (int i = 0; i < positions.Count; i++)
            {
                float sqr = (positions[i] - origin).sqrMagnitude;
                if (sqr > maxSqr || sqr >= bestSqr) continue;

                bestSqr = sqr;
                best = i;
            }

            return best;
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인** — 신규 6개 통과.

- [ ] **Step 5: 커밋**

```bash
git add unity/Assets/_Project/Scripts/Companions/FairyTargetLogic.cs unity/Assets/_Project/Scripts/Companions/FairyTargetLogic.cs.meta unity/Assets/Tests/EditMode/FairyTargetLogicTests.cs unity/Assets/Tests/EditMode/FairyTargetLogicTests.cs.meta
git commit -m "feat: 요정 가장 가까운 적 고르기 순수 로직"
```

### Task 4: `FairyChoiceLogic`

**Files:**
- Create: `unity/Assets/_Project/Scripts/Companions/FairyChoiceLogic.cs`
- Create: `unity/Assets/Tests/EditMode/FairyChoiceLogicTests.cs`

**Interfaces:**
- Produces:
  - `public enum FairyChoiceKind { Summon, Upgrade }`
  - `public struct FairyChoice { public FairyChoiceKind Kind; public int Index; }` (`Summon`일 때 `Index`는 -1)
  - `public static List<FairyChoice> FairyChoiceLogic.Build(IReadOnlyList<int> levels, int maxCount, int maxLevel)` — `levels`는 현재 요정들의 레벨. 소환 가능하면 `Summon`이 맨 앞, 이어서 레벨이 `maxLevel` 미만인 요정마다 `Upgrade(index)`. **최대 3개**(`MaxChoices`). 선택지가 없으면 빈 목록.
  - `public const int FairyChoiceLogic.MaxChoices = 3;`

- [ ] **Step 1: 실패하는 테스트 작성**

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using SushiSurvival.Companions;

namespace SushiSurvival.EditModeTests
{
    public class FairyChoiceLogicTests
    {
        [Test]
        public void NoFairies_OffersOnlySummon()
        {
            List<FairyChoice> choices = FairyChoiceLogic.Build(new int[0], 3, 4);

            Assert.AreEqual(1, choices.Count);
            Assert.AreEqual(FairyChoiceKind.Summon, choices[0].Kind);
            Assert.AreEqual(-1, choices[0].Index);
        }

        [Test]
        public void OneFairy_OffersSummonThenUpgrade()
        {
            List<FairyChoice> choices = FairyChoiceLogic.Build(new[] { 2 }, 3, 4);

            Assert.AreEqual(2, choices.Count);
            Assert.AreEqual(FairyChoiceKind.Summon, choices[0].Kind);
            Assert.AreEqual(FairyChoiceKind.Upgrade, choices[1].Kind);
            Assert.AreEqual(0, choices[1].Index);
        }

        [Test]
        public void TwoFairies_SummonPlusTwoUpgrades_IsThreeCards()
        {
            List<FairyChoice> choices = FairyChoiceLogic.Build(new[] { 1, 3 }, 3, 4);

            Assert.AreEqual(3, choices.Count);
            Assert.AreEqual(FairyChoiceKind.Summon, choices[0].Kind);
            Assert.AreEqual(0, choices[1].Index);
            Assert.AreEqual(1, choices[2].Index);
        }

        [Test]
        public void ThreeFairies_OnlyUpgrades_NeverMoreThanThree()
        {
            List<FairyChoice> choices = FairyChoiceLogic.Build(new[] { 1, 1, 1 }, 3, 4);

            Assert.AreEqual(3, choices.Count);
            foreach (FairyChoice choice in choices)
                Assert.AreEqual(FairyChoiceKind.Upgrade, choice.Kind);
        }

        [Test]
        public void MaxLevelFairy_IsNotOfferedAnUpgrade()
        {
            List<FairyChoice> choices = FairyChoiceLogic.Build(new[] { 4, 2, 4 }, 3, 4);

            Assert.AreEqual(1, choices.Count);
            Assert.AreEqual(FairyChoiceKind.Upgrade, choices[0].Kind);
            Assert.AreEqual(1, choices[0].Index);
        }

        [Test]
        public void EverythingMaxed_ReturnsEmpty()
        {
            Assert.IsEmpty(FairyChoiceLogic.Build(new[] { 4, 4, 4 }, 3, 4));
        }

        [Test]
        public void NullLevels_IsTreatedAsNoFairies()
        {
            List<FairyChoice> choices = FairyChoiceLogic.Build(null, 3, 4);

            Assert.AreEqual(1, choices.Count);
            Assert.AreEqual(FairyChoiceKind.Summon, choices[0].Kind);
        }

        [Test]
        public void LargerMaxCount_StillCapsAtThreeCards()
        {
            List<FairyChoice> choices = FairyChoiceLogic.Build(new[] { 1, 1, 1, 1 }, 6, 4);

            Assert.AreEqual(FairyChoiceLogic.MaxChoices, choices.Count);
        }
    }
}
```

- [ ] **Step 2: 실행해 실패 확인** — 컴파일 에러.

- [ ] **Step 3: 구현**

```csharp
using System.Collections.Generic;

namespace SushiSurvival.Companions
{
    public enum FairyChoiceKind
    {
        Summon,
        Upgrade
    }

    public struct FairyChoice
    {
        public FairyChoiceKind Kind;

        /// <summary>강화할 요정의 번호(0부터). 소환이면 -1.</summary>
        public int Index;
    }

    public static class FairyChoiceLogic
    {
        /// <summary>레벨업 카드가 세 장이라 선택지도 그 이상 만들지 않는다.</summary>
        public const int MaxChoices = 3;

        public static List<FairyChoice> Build(IReadOnlyList<int> levels, int maxCount, int maxLevel)
        {
            var choices = new List<FairyChoice>();
            int count = levels != null ? levels.Count : 0;

            if (count < maxCount)
                choices.Add(new FairyChoice { Kind = FairyChoiceKind.Summon, Index = -1 });

            for (int i = 0; i < count && choices.Count < MaxChoices; i++)
            {
                if (levels[i] >= maxLevel) continue;

                choices.Add(new FairyChoice { Kind = FairyChoiceKind.Upgrade, Index = i });
            }

            return choices;
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인** — 신규 8개 통과, PR B 전체(23개) 포함 컴파일 에러 0.

- [ ] **Step 5: 커밋·PR**

```bash
git add unity/Assets/_Project/Scripts/Companions/FairyChoiceLogic.cs unity/Assets/_Project/Scripts/Companions/FairyChoiceLogic.cs.meta unity/Assets/Tests/EditMode/FairyChoiceLogicTests.cs unity/Assets/Tests/EditMode/FairyChoiceLogicTests.cs.meta
git commit -m "feat: 요정 소환/강화 선택지 구성 순수 로직"
git push -u origin feature/fairy-logic
gh pr create --base main --title "feat: 요정 순수 로직 3종(슬롯·타깃·선택지)" --body "<요약·테스트 결과>"
```

---

## PR C — 요정 컴포넌트 (추가만, 아직 아무도 호출하지 않음)

> B가 `main`에 머지된 뒤 최신 `main`에서 `feature/fairy-components` 브랜치를 판다.
> MonoBehaviour는 컴파일 + 회귀 확인만 한다(테스트 없음). 에디터를 한 번 열어 `.meta`를 생성한다.

### Task 5: `Fairy` — 요정 한 마리

**Files:**
- Create: `unity/Assets/_Project/Scripts/Companions/Fairy.cs`

**Interfaces:**
- Consumes: `FairySlotLogic.SlotOffset/Follow`, `FairyTargetLogic.NearestIndex`, `CooldownLogic.ApplyAttackSpeed`, `WeaponCooldown`, `WeaponVisualLogic.ComputeRotationDegrees`, `Projectile.Initialize(Vector2 direction, float damage, int pierceCount, GameObjectPool pool)`, `PlayerStats.GetValue(StatType)`, `WeaponData.levels`
- Produces:
  - `[Serializable] public struct FairyMotion { public float radius; public float arcSpacingDegrees; public float bobAmplitude; public float bobSpeed; public float followSharpness; }` (기본값은 `FairyMotion.Default`)
  - `public int Fairy.Level { get; }`
  - `public void Fairy.Initialize(Transform player, PlayerStats stats, WeaponData data, GameObjectPool projectilePool, LayerMask enemyLayer, FairyMotion motion, int level, int slotIndex, int slotCount)`
  - `public void Fairy.SetLevel(int level)` — `1~data.levels.Length`로 클램프
  - `public void Fairy.SetSlot(int slotIndex, int slotCount)`
  - `public void Fairy.SetPlayer(Transform player, PlayerStats stats)`

- [ ] **Step 1: 구현** — `Fairy.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;
using SushiSurvival.Enemies;
using SushiSurvival.Player;
using SushiSurvival.Weapons;

namespace SushiSurvival.Companions
{
    [Serializable]
    public struct FairyMotion
    {
        [Tooltip("플레이어 중심에서 슬롯까지 거리.")]
        public float radius;
        [Tooltip("요정 사이의 각도 간격(도). 머리 위(90°)를 중심으로 부채꼴로 선다.")]
        public float arcSpacingDegrees;
        [Tooltip("위아래로 둥둥 떠다니는 폭.")]
        public float bobAmplitude;
        public float bobSpeed;
        [Tooltip("클수록 슬롯을 빨리 따라간다.")]
        public float followSharpness;

        public static FairyMotion Default => new FairyMotion
        {
            radius = 1.2f,
            arcSpacingDegrees = 60f,
            bobAmplitude = 0.12f,
            bobSpeed = 3f,
            followSharpness = 6f
        };
    }

    /// <summary>
    /// 요정 한 마리. 플레이어 주변 슬롯을 부드럽게 따라다니고, 쿨타임마다 사거리 안의 가장 가까운
    /// 적에게 기존 Projectile을 쏜다. 수치는 WeaponData의 레벨 표에서 읽고 플레이어 증강 배율을 곱한다.
    /// </summary>
    public class Fairy : MonoBehaviour
    {
        [Tooltip("공격속도 증강이 아무리 쌓여도 이 값보다 짧아지지 않는다(무한 연사 방지).")]
        [SerializeField] private float minCooldown = 0.2f;

        private readonly WeaponCooldown _cooldown = new WeaponCooldown();
        private readonly List<Vector2> _positions = new List<Vector2>();
        private readonly List<Transform> _targets = new List<Transform>();

        private Transform _player;
        private PlayerStats _stats;
        private WeaponData _data;
        private GameObjectPool _pool;
        private LayerMask _enemyLayer;
        private FairyMotion _motion;
        private int _level = 1;
        private int _slotIndex;
        private int _slotCount = 1;
        private float _time;

        public int Level => _level;

        public void Initialize(Transform player, PlayerStats stats, WeaponData data, GameObjectPool projectilePool,
                               LayerMask enemyLayer, FairyMotion motion, int level, int slotIndex, int slotCount)
        {
            _player = player;
            _stats = stats;
            _data = data;
            _pool = projectilePool;
            _enemyLayer = enemyLayer;
            _motion = motion;
            _slotIndex = slotIndex;
            _slotCount = slotCount;
            SetLevel(level);

            // 소환되는 순간 플레이어 옆에서 시작하게 해 화면 구석에서 날아오지 않게 한다.
            if (_player != null)
                transform.position = TargetPosition();
        }

        public void SetPlayer(Transform player, PlayerStats stats)
        {
            _player = player;
            _stats = stats;
        }

        public void SetLevel(int level)
        {
            int max = _data != null && _data.levels != null ? _data.levels.Length : 1;
            _level = Mathf.Clamp(level, 1, Mathf.Max(1, max));
        }

        public void SetSlot(int slotIndex, int slotCount)
        {
            _slotIndex = slotIndex;
            _slotCount = slotCount;
        }

        private void Update()
        {
            if (_player == null || _data == null || _data.levels == null || _data.levels.Length == 0) return;

            _time += Time.deltaTime;
            transform.position = FairySlotLogic.Follow(
                transform.position, TargetPosition(), _motion.followSharpness, Time.deltaTime);

            _cooldown.Tick(Time.deltaTime);
            if (!_cooldown.IsReady) return;

            WeaponLevelStats stats = _data.levels[_level - 1];
            if (!TryFire(stats)) return;

            _cooldown.Reset(CooldownLogic.ApplyAttackSpeed(
                stats.cooldown, StatMultiplier(StatType.AttackSpeed), minCooldown));
        }

        private Vector2 TargetPosition()
            => (Vector2)_player.position + FairySlotLogic.SlotOffset(
                _slotIndex, _slotCount, _motion.radius, _motion.arcSpacingDegrees,
                _time, _motion.bobAmplitude, _motion.bobSpeed);

        /// <summary>타깃이 없으면 쏘지 않고 false — 쿨타임도 소모하지 않아 적이 나타나는 즉시 쏜다.</summary>
        private bool TryFire(WeaponLevelStats stats)
        {
            if (_pool == null)
            {
                Debug.LogError($"{name}: projectilePool이 없어 발사할 수 없습니다.");
                return false;
            }

            float range = stats.range * StatMultiplier(StatType.AttackRange);
            Vector2 origin = transform.position;

            _positions.Clear();
            _targets.Clear();

            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, _enemyLayer);
            foreach (Collider2D hit in hits)
            {
                if (!hit.TryGetComponent<EnemyBase>(out _)) continue;

                _positions.Add(hit.transform.position);
                _targets.Add(hit.transform);
            }

            int index = FairyTargetLogic.NearestIndex(origin, _positions, range);
            if (index < 0) return false;

            Vector2 direction = ((Vector2)_targets[index].position - origin).normalized;
            float rotation = WeaponVisualLogic.ComputeRotationDegrees(direction);

            GameObject projectileObj = _pool.Get(origin, Quaternion.Euler(0f, 0f, rotation));
            if (!projectileObj.TryGetComponent<Projectile>(out var projectile))
            {
                Debug.LogError($"{projectileObj.name}: Projectile 컴포넌트가 없어 발사할 수 없습니다.");
                _pool.Release(projectileObj);
                return false;
            }

            projectile.Initialize(direction, stats.damage * StatMultiplier(StatType.AttackDamage),
                                  stats.pierceCount, _pool);
            return true;
        }

        private float StatMultiplier(StatType stat) => _stats != null ? _stats.GetValue(stat) : 1f;
    }
}
```

- [ ] **Step 2: 컴파일 확인** — 에디터를 열어 콘솔에 에러가 없는지, 또는 배치 테스트로 `error CS` 0 확인.

- [ ] **Step 3: 커밋**

```bash
git add unity/Assets/_Project/Scripts/Companions/Fairy.cs unity/Assets/_Project/Scripts/Companions/Fairy.cs.meta
git commit -m "feat: 요정 한 마리 — 슬롯 따라가기와 자동 투사체"
```

### Task 6: `FairyController` — 요정 목록 관리

**Files:**
- Create: `unity/Assets/_Project/Scripts/Companions/FairyController.cs`

**Interfaces:**
- Consumes: `Fairy.Initialize/SetLevel/SetSlot/SetPlayer/Level`, `FairyChoiceLogic.Build`, `CircleTextureFactory.CreateSprite(int size, float innerRatio, Color color)`, `UpgradeDescriptionLogic.DescribeWeaponUpgrade(WeaponLevelStats current, WeaponLevelStats next, bool isUmbrella = false)`
- Produces:
  - `public int Count { get; }`, `public int MaxCount { get; }`, `public int MaxLevel { get; }`
  - `public IReadOnlyList<int> Levels { get; }` — 요정마다의 현재 레벨(소환 순서)
  - `public void SetPlayer(Transform player, PlayerStats stats)`
  - `public bool Summon()` — 새 요정(Lv1). 가득 찼으면 false.
  - `public bool Upgrade(int index)` — 최대 레벨이면 false.
  - `public void Restore(IReadOnlyList<int> levels)` — 기존 요정을 비우고 레벨 배열대로 다시 만든다(`null`이면 아무것도 안 함).
  - `public List<FairyChoice> BuildChoices()`
  - `public string DescribeUpgrade(int index)` — 다음 레벨과의 수치 비교 문구.

- [ ] **Step 1: 구현** — `FairyController.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;
using SushiSurvival.Player;

namespace SushiSurvival.Companions
{
    /// <summary>
    /// 요정 시스템 총괄 — 씬 오브젝트(캐릭터 프리팹에 붙이지 않는다). 요정 목록을 들고 소환·강화하며,
    /// 보스 씬으로 레벨 배열을 넘기고 복원할 수 있게 한다. 요정 수치는 WeaponData(레벨 1~4)에서 읽는다.
    /// </summary>
    public class FairyController : MonoBehaviour
    {
        [Tooltip("요정 레벨별 수치. damage/cooldown/range(사거리)/pierceCount를 쓴다.")]
        [SerializeField] private WeaponData fairyData;
        [Tooltip("요정 투사체 풀. 풀 하나당 GameObject 하나(같은 오브젝트에 풀을 둘 붙이지 말 것).")]
        [SerializeField] private GameObjectPool projectilePool;
        [SerializeField] private LayerMask enemyLayer;
        [Tooltip("요정 프리팹. 비워두면 노란 원 플레이스홀더를 런타임에 만든다(아트가 들어오면 프리팹으로 교체).")]
        [SerializeField] private Fairy fairyPrefab;
        [SerializeField] private int maxFairies = 3;
        [SerializeField] private FairyMotion motion = FairyMotion.Default;

        private readonly List<Fairy> _fairies = new List<Fairy>();
        private readonly List<int> _levels = new List<int>();
        private Transform _player;
        private PlayerStats _stats;

        public int Count => _fairies.Count;
        public int MaxCount => maxFairies;
        public int MaxLevel => fairyData != null && fairyData.levels != null ? fairyData.levels.Length : 1;

        public IReadOnlyList<int> Levels
        {
            get
            {
                _levels.Clear();
                foreach (Fairy fairy in _fairies)
                    _levels.Add(fairy.Level);
                return _levels;
            }
        }

        public void SetPlayer(Transform player, PlayerStats stats)
        {
            _player = player;
            _stats = stats;

            foreach (Fairy fairy in _fairies)
                fairy.SetPlayer(player, stats);
        }

        public bool Summon()
        {
            if (_fairies.Count >= maxFairies) return false;
            if (fairyData == null)
            {
                Debug.LogError($"{name}: fairyData가 비어 있어 요정을 소환할 수 없습니다.");
                return false;
            }

            Fairy fairy = CreateFairy();
            _fairies.Add(fairy);
            fairy.Initialize(_player, _stats, fairyData, projectilePool, enemyLayer, motion,
                             1, _fairies.Count - 1, _fairies.Count);

            RefreshSlots();
            return true;
        }

        public bool Upgrade(int index)
        {
            if (index < 0 || index >= _fairies.Count) return false;

            Fairy fairy = _fairies[index];
            if (fairy.Level >= MaxLevel) return false;

            fairy.SetLevel(fairy.Level + 1);
            return true;
        }

        public void Restore(IReadOnlyList<int> levels)
        {
            if (levels == null) return;

            foreach (Fairy fairy in _fairies)
            {
                if (fairy != null) Destroy(fairy.gameObject);
            }
            _fairies.Clear();

            foreach (int level in levels)
            {
                if (!Summon()) break;

                _fairies[_fairies.Count - 1].SetLevel(level);
            }
        }

        public List<FairyChoice> BuildChoices() => FairyChoiceLogic.Build(Levels, maxFairies, MaxLevel);

        public string DescribeUpgrade(int index)
        {
            if (fairyData == null || index < 0 || index >= _fairies.Count) return string.Empty;

            int level = _fairies[index].Level;
            if (level >= fairyData.levels.Length) return string.Empty;

            return UpgradeDescriptionLogic.DescribeWeaponUpgrade(fairyData.levels[level - 1], fairyData.levels[level]);
        }

        private void RefreshSlots()
        {
            for (int i = 0; i < _fairies.Count; i++)
                _fairies[i].SetSlot(i, _fairies.Count);
        }

        private Fairy CreateFairy()
        {
            if (fairyPrefab != null)
                return Instantiate(fairyPrefab, transform);

            var go = new GameObject("Fairy (placeholder)");
            go.transform.SetParent(transform, false);

            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CircleTextureFactory.CreateSprite(24, 0f, new Color(1f, 0.85f, 0.4f));
            spriteRenderer.sortingOrder = 50;

            return go.AddComponent<Fairy>();
        }
    }
}
```

> 주의: `Restore`는 `Summon()`을 거치므로 `SetPlayer`가 먼저 불려 있어야 요정이 플레이어 곁에서 시작한다.
> `Levels`는 호출마다 내부 리스트를 다시 채워 돌려주므로, 호출 쪽이 보관하려면 복사한다(`new List<int>(Levels)`).

- [ ] **Step 2: 컴파일 확인**

- [ ] **Step 3: 커밋**

```bash
git add unity/Assets/_Project/Scripts/Companions/FairyController.cs unity/Assets/_Project/Scripts/Companions/FairyController.cs.meta
git commit -m "feat: 요정 목록 관리 — 소환·강화·복원·선택지"
```

### Task 7: `FairyOption` — 카드로 뜨는 선택지

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/FairyOption.cs`

**Interfaces:**
- Consumes: `IUpgradeOption`(`DisplayName`, `Description`, `Icon`, `Apply()`), `FairyController.Summon/Upgrade/Count/MaxCount/Levels/DescribeUpgrade`, `FairyChoice`
- Produces: `public FairyOption(FairyController controller, FairyChoice choice, Sprite icon)`

- [ ] **Step 1: 구현**

```csharp
using UnityEngine;
using SushiSurvival.Companions;

namespace SushiSurvival.Core
{
    /// <summary>와사비 성공 뒤 레벨업 카드로 뜨는 "요정 소환 / 요정 N 강화" 선택지.</summary>
    public class FairyOption : IUpgradeOption
    {
        private readonly FairyController _controller;
        private readonly FairyChoice _choice;

        public Sprite Icon { get; }

        public string DisplayName => _choice.Kind == FairyChoiceKind.Summon
            ? "요정 소환"
            : $"요정 {_choice.Index + 1} 강화 Lv{_controller.Levels[_choice.Index] + 1}";

        public string Description => _choice.Kind == FairyChoiceKind.Summon
            ? $"곁을 따라다니며 가까운 적을 공격한다 ({_controller.Count + 1}/{_controller.MaxCount})"
            : _controller.DescribeUpgrade(_choice.Index);

        public FairyOption(FairyController controller, FairyChoice choice, Sprite icon)
        {
            _controller = controller;
            _choice = choice;
            Icon = icon;
        }

        public void Apply()
        {
            if (_choice.Kind == FairyChoiceKind.Summon)
                _controller.Summon();
            else
                _controller.Upgrade(_choice.Index);
        }
    }
}
```

- [ ] **Step 2: 배치 테스트로 회귀 확인** — 컴파일 에러 0, 전체 기준(484/483, PR B가 머지됐으면 그만큼 추가).

- [ ] **Step 3: 커밋·PR**

```bash
git add unity/Assets/_Project/Scripts/Core/FairyOption.cs unity/Assets/_Project/Scripts/Core/FairyOption.cs.meta
git commit -m "feat: 요정 소환/강화 레벨업 카드 선택지"
git push -u origin feature/fairy-components
gh pr create --base main --title "feat: 요정 컴포넌트 3종(아직 호출 안 함)" --body "<요약>"
```

PR 본문: "추가만 한다. D에서 `LevelSystem`이 연결하기 전에는 게임 동작에 영향이 없다."

---

## PR D — 와사비 흐름 전환과 보스 씬 이월

> A와 C가 `main`에 머지된 뒤 최신 `main`에서 `feature/fairy-wasabi-flow`를 판다.
> 시작 전 `git log --oneline origin/main -- unity/Assets/_Project/Scripts/Core/LevelSystem.cs unity/Assets/_Project/Scripts/Core/GameManager.cs unity/Assets/_Project/Scripts/Core/BossFightDirector.cs`로 협업자의 최근 변경을 확인하고, 아래 앵커 문자열이 그대로 있는지 grep한다.

### Task 8: `RunResultCarrier`·`GameManager`·`BossFightDirector` 이월

**Files:**
- Modify: `unity/Assets/_Project/Scripts/UI/RunResultCarrier.cs` (`WasabiCount` 필드 아래에 추가)
- Modify: `unity/Assets/_Project/Scripts/Core/GameManager.cs` (`RunResultCarrier.WasabiCount = levelSystem.WasabiCount;` 바로 아래)
- Modify: `unity/Assets/_Project/Scripts/Core/BossFightDirector.cs` (`levelSystem.RestoreProgress(...)` 호출 바로 아래)
- Modify: `unity/Assets/_Project/Scripts/Core/LevelSystem.cs` (`FairyLevels`·`RestoreFairies` — Task 9와 같은 파일이라 한 커밋으로)

**Interfaces:**
- Consumes: `FairyController.Levels`, `FairyController.Restore(IReadOnlyList<int>)`
- Produces: `RunResultCarrier.FairyLevels` (`public static int[] FairyLevels;`), `LevelSystem.FairyLevels` (`IReadOnlyList<int>`), `LevelSystem.RestoreFairies(IReadOnlyList<int> levels)`

- [ ] **Step 1: `RunResultCarrier`에 필드 추가**

```csharp
        // 요정 한 마리당 하나씩, 소환 순서대로의 레벨. 보스 씬에서 같은 요정을 되살린다.
        public static int[] FairyLevels;
```

- [ ] **Step 2: `GameManager` 기록** — `BossScene`으로 넘어가기 직전 블록(`RunResultCarrier.WasabiCount = levelSystem.WasabiCount;`) 아래:

```csharp
                RunResultCarrier.FairyLevels = new List<int>(levelSystem.FairyLevels).ToArray();
```

(`using System.Collections.Generic;`은 이미 있다.)

- [ ] **Step 3: `BossFightDirector` 복원** — `levelSystem.RestoreProgress(...)` 호출(`if (RunResultCarrier.CurrentLevel >= 1)` 블록 안) 아래에 추가:

```csharp
                levelSystem.RestoreFairies(RunResultCarrier.FairyLevels);
```

- [ ] **Step 4: `LevelSystem`에 위임 멤버 추가** (Task 9 Step 1과 함께)

```csharp
        public IReadOnlyList<int> FairyLevels
            => fairyController != null ? fairyController.Levels : (IReadOnlyList<int>)Array.Empty<int>();

        public void RestoreFairies(IReadOnlyList<int> levels)
        {
            if (fairyController != null)
                fairyController.Restore(levels);
        }
```

- [ ] **Step 5: 컴파일 확인** (Task 9와 합쳐 확인해도 된다)

### Task 9: `LevelSystem` 와사비 흐름 전환

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Core/LevelSystem.cs`

**Interfaces:**
- Consumes: `FairyController`(`SetPlayer`, `BuildChoices`), `FairyOption`, `FairyChoice`, `LevelUpPanel.Show(IReadOnlyList<IUpgradeOption>, Action<IUpgradeOption>, Action)`
- Produces: 와사비 성공 → 요정 카드 선택 흐름. `royalWasabiEnabled` 기본값 true.

- [ ] **Step 1: 필드 추가·기본값 변경** — `royalWasabiController` 필드 근처

```csharp
        [Tooltip("와사비 성공 보상으로 소환·강화하는 요정 시스템. 비워두면 요정 대신 스탯 버프를 준다.")]
        [SerializeField] private FairyController fairyController;
        [Tooltip("요정 선택 카드에 쓸 아이콘. 비워도 동작한다.")]
        [SerializeField] private Sprite fairyIcon;
        private bool _fairyRewardPending;
```

기존 `royalWasabiEnabled` 필드의 기본값을 바꾼다:

```csharp
        [SerializeField] private bool royalWasabiEnabled = true;
```

(툴팁의 "새 보상(요정) 작업이 끝날 때까지 꺼둔다" 문구는 "꺼두면 와사비 버튼이 숨겨진다."로 고친다. 씬에 이미 저장된 값이 있으면 그 값이 우선하므로 E에서 확인한다.)

- [ ] **Step 2: `SetPlayer`에서 요정 시스템에 플레이어 연결**

```csharp
        public void SetPlayer(PlayerStats stats, PlayerHealth health, WeaponBase weapon, Sprite portrait)
        {
            _playerStats = stats;
            _playerHealth = health;
            _weapon = weapon;
            _portrait = portrait;

            if (fairyController != null && stats != null)
                fairyController.SetPlayer(stats.transform, stats);
        }
```

- [ ] **Step 3: 와사비 성공 콜백 교체** — `HandleRoyalWasabiRequested`의 `onSuccess` 선택과 `royalWasabiController.Show(...)` 호출을 아래로 바꾼다

```csharp
            bool hasFairyReward = fairyController != null && fairyController.BuildChoices().Count > 0;
            _fairyRewardPending = false;

            royalWasabiController.Show(_portrait, () =>
            {
                WasabiCount++;

                if (hasFairyReward)
                {
                    _fairyRewardPending = true;
                    return new[] { "요정의 힘을 얻었다!" };
                }

                // 요정 시스템이 없거나 모두 최대 레벨이면 성공이 헛되지 않게 스탯 버프를 준다.
                return ApplyRoyalWasabiStatBuffs();
            }, HandleRoyalWasabiFinished);
```

기존 `Func<string[]> onSuccess = _weapon switch { ... }` 블록은 **삭제**한다(`ConvertToUmbrella`는 레벨업 성장 경로 `OnOptionChosen`이 계속 쓴다. `ConvertToShotgun`은 호출만 끊고 메서드는 남긴다).

- [ ] **Step 4: 요정 선택 흐름 메서드 추가**

```csharp
        private void HandleRoyalWasabiFinished()
        {
            if (_fairyRewardPending)
            {
                _fairyRewardPending = false;
                ShowFairyChoice();
                return;
            }

            ShowNext();
        }

        /// <summary>
        /// 와사비 성공 직후 "요정 소환/강화"를 레벨업 카드로 고르게 한다. _panelOpen과 timeScale 0은
        /// 와사비 연출 때부터 그대로 이어지고, 선택이 끝나면 기존 대기 레벨업 큐(ShowNext)로 합류한다.
        /// </summary>
        private void ShowFairyChoice()
        {
            var options = new List<IUpgradeOption>();
            if (fairyController != null)
            {
                foreach (FairyChoice choice in fairyController.BuildChoices())
                    options.Add(new FairyOption(fairyController, choice, fairyIcon));
            }

            if (options.Count == 0)
            {
                ShowNext();
                return;
            }

            panel.Show(options, OnFairyChosen, null);
        }

        private void OnFairyChosen(IUpgradeOption option)
        {
            option.Apply();

            panel.Hide();
            _panelOpen = false;

            ShowNext();
        }
```

`LevelSystem.cs` 맨 위에 `using SushiSurvival.Companions;`를 추가한다.

- [ ] **Step 5: Task 8 Step 4의 `FairyLevels`·`RestoreFairies`도 이 파일에 추가한다.**

- [ ] **Step 6: 배치 테스트** — 컴파일 에러 0, 전체 기준 유지(A·B 머지분 포함해 기존 실패 1건만).

- [ ] **Step 7: `git status`/`git diff` 확인 후 커밋·PR**

```bash
git add unity/Assets/_Project/Scripts/Core/LevelSystem.cs unity/Assets/_Project/Scripts/Core/GameManager.cs unity/Assets/_Project/Scripts/Core/BossFightDirector.cs unity/Assets/_Project/Scripts/UI/RunResultCarrier.cs
git commit -m "feat: 와사비 성공 보상을 보조 요정으로 전환하고 보스 씬으로 이월"
git push -u origin feature/fairy-wasabi-flow
gh pr create --base main --title "feat: 와사비 보상 → 보조 요정 (LevelSystem 전환·보스 이월)" --body "<요약>"
```

PR 본문에 적을 것: 씬 변경은 E에서 사용자가 한다 / `royalWasabiEnabled`가 씬에 `0`으로 저장돼 있으니 에디터에서 켜야 한다 / `ConvertToShotgun`·스탯 버프 코드는 삭제 없이 남겼다(스탯 버프는 요정 만렙 시 대체 보상으로 쓰인다).

---

## PR E — 씬 배선 (사용자 에디터 작업)

D 머지 후 사용자가 `GameScene`과 `BossScene`에 각각 한다. 코드는 건드리지 않는다.

- [ ] **에셋 1: 요정 `WeaponData`** — `Assets/_Project/Data/`에서 우클릭 → Create → SushiSurvival → Weapon Data, 이름 `FairyWeaponData`. Weapon Name `요정`, Is Melee 꺼짐, Levels 4개. 제안 시작값(플레이테스트로 조정):

  | Lv | damage | cooldown | range | pierceCount |
  |---|---|---|---|---|
  | 1 | 4 | 1.2 | 6 | 0 |
  | 2 | 5 | 1.1 | 6 | 0 |
  | 3 | 6 | 1.0 | 7 | 1 |
  | 4 | 8 | 0.9 | 7 | 1 |

  (`range`는 요정이 적을 찾는 사거리, `angleDegrees`는 쓰지 않는다.)

- [ ] **프리팹 1: 요정 투사체** — `Assets/_Project/Prefabs/SpriteRenderer.prefab`(`Projectile` 컴포넌트가 붙은 기존 투사체)을 복제해 `FairyShot.prefab`으로 만들고, 필요하면 색을 바꾼다.

- [ ] **씬 오브젝트 2개** (각 씬마다):
  1. 빈 GameObject `FairyProjectilePool` + `GameObjectPool` 컴포넌트(Prefab=`FairyShot`, Prewarm Count=20). **풀은 오브젝트 하나당 하나만** 붙인다.
  2. 빈 GameObject `FairySystem` + `FairyController` 컴포넌트: Fairy Data=`FairyWeaponData`, Projectile Pool=`FairyProjectilePool`, Enemy Layer=기존 무기들과 같은 적 레이어, Fairy Prefab은 비워둔다(노란 원 플레이스홀더).

- [ ] **`LevelSystem` 연결** (각 씬): Fairy Controller에 `FairySystem`을 연결하고, **Royal Wasabi Enabled를 체크**한다. 요정 카드 아이콘(Fairy Icon)은 있으면 연결한다.

- [ ] **`RockPaperScissorsPanel`**: Win Chance가 0.4인지 확인한다.

- [ ] **플레이 확인 체크리스트**
  - 레벨업 팝업에 와사비 버튼이 보인다.
  - 와사비 성공 시 연출 뒤 "요정 소환 / (요정 N 강화)" 카드가 뜨고, 고르면 요정이 플레이어 머리 위에 나타나 가까운 적에게 쏜다.
  - 두 번째 성공에서 요정이 늘어나거나 강화된다(최대 3마리).
  - 요정 3마리가 전부 최대 레벨이면 성공 시 스탯 버프가 대신 적용된다.
  - 보스 씬으로 넘어가도 같은 요정이 같은 레벨로 따라온다.
  - 와사비를 열 번쯤 해봤을 때 성공이 대략 열 번 중 네 번 정도다.
- [ ] **커밋 전 확인**: `git status`로 예상 밖 파일, `git diff`로 테스트용 임시값(`bossSpawnTime` 등)이 섞이지 않았는지. 씬 변경은 `GameScene.unity`가 협업자 소유라 PR 본문에 명시한다.

---

## Self-Review

- **스펙 커버리지:** 확정 결정 표(모든 캐릭터·3마리·소환/강화·자동 투사체·슬롯 이동·플레이스홀더·WeaponData·Projectile 재사용·카드 UI·40%·보스 이월) → Task 1~9 + E. 성공 확률 → Task 1. 순수 로직 4종 → Task 1~4. 요정 동작 → Task 5~6. 와사비 흐름(선택지 있음/없음/실패) → Task 9. 보스 이월 → Task 8. 테스트 절 → Task 1~4. PR 분할 → 상단 표. 스펙의 `Fairy/` 폴더는 `Companions/`로 바꿨다(위 Global Constraints에 명시).
- **플레이스홀더 점검:** TBD·"적절히 처리" 류 없음. 코드가 필요한 모든 단계에 코드를 넣었다. 에디터 작업(E)의 수치는 제안값이며 플레이테스트로 조정한다고 명시했다.
- **타입·이름 일관성:** `FairyChoice`/`FairyChoiceKind`(Task 4) → `FairyController.BuildChoices`(6) → `FairyOption`(7)·`LevelSystem`(9). `FairyMotion`(5) → `FairyController.motion`(6). `Levels`는 `IReadOnlyList<int>`로 통일(6, 8, 9). `Restore(IReadOnlyList<int>)`/`RestoreFairies`(8)와 `RunResultCarrier.FairyLevels`(`int[]`)가 `IReadOnlyList<int>`로 암묵 변환된다. `FairySlotLogic.SlotOffset`의 인자 순서(index, count, radius, arcSpacingDegrees, time, bobAmplitude, bobSpeed)가 Task 2 테스트·Task 5 호출에서 같다.
- **주의할 점(실행자용):** `Levels`는 호출마다 내부 리스트를 갱신해 돌려준다 — `GameManager`는 복사해서(`new List<int>(...)`) 보관한다. `LevelSystem.FairyLevels`가 빈 배열일 수 있다(요정이 없을 때).
