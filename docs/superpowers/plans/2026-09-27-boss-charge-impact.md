# 보스 돌진 임팩트 연출 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 보스 돌진에 예고선(붉은 선)·잔상과 먼지·정지 순간 충격파와 화면 흔들림을 얹는다. 판정과 이동은 손대지 않는 순수 연출이다.

**Architecture:** 판정 없는 순수 계산(`ChargeEffectsLogic`)과, 잔상·먼지·충격파가 공통으로 쓰는 풀링 효과(`FadingSpriteEffect`), 이를 총괄하는 `BossChargeEffects` 컴포넌트를 새로 만들고, `BossController.ChargeRoutine`이 각 단계에서 호출한다. 컴포넌트가 없으면(`null`) 연출 호출을 전부 건너뛰어 돌진은 지금과 같다. 아트·프리팹 없이 전부 런타임 생성이다.

**Tech Stack:** Unity 2022.3.62f3, Built-in 2D, NUnit EditMode 테스트. 관례: 판정/좌표 로직은 `XxxLogic` 정적 클래스 + EditMode 테스트, MonoBehaviour는 컴파일+회귀 확인만.

**Spec:** `docs/superpowers/specs/2026-09-27-boss-charge-impact-design.md`

## Global Constraints

- `main` 직접 커밋·푸시·병합 절대 금지 — **문서 파일도 예외 없음.** 모든 태스크는 새 브랜치에서 시작하고 PR로 올린다. 병합 버튼은 사람만 누른다.
- `git add .` 금지 — 파일을 명시하고 `.meta`를 항상 같이 커밋한다.
- 씬(`.unity`)·프리팹(`.prefab`) 편집은 사람이 Unity Editor GUI로 한다.
- 새 아트·사운드 없음. 충격파는 피해 없는 순수 시각 효과, 히트스톱은 넣지 않는다.
- 게임플레이 변화는 하나뿐: 방향 확정이 예고 종료 `chargeLockSeconds`(기본 0.15초) 전으로 당겨진다. 0이거나 `BossChargeEffects`가 없으면 기존과 같다.
- 스펙과 다른 점 둘(스펙을 쓴 뒤 코드를 다시 읽고 바로잡음):
  1. `ChargeEffectsLogic.FadeAlpha`·`Progress`는 만들지 않는다 — 기존 `BossEntranceLogic.Progress`(Core)가 같은 일을 하고, 선형 페이드는 `FadingSpriteEffect` 안에서 한 줄로 쓴다.
  2. 예고선은 스펙 표의 `EndTrail`이 아니라 **`BeginTrail`에서** 끈다(스펙 흐름도의 "돌진이 시작되면 선을 끈다"가 맞다).

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
**baseline은 태스크 시작 직전 `main`에서 처음 돌린 개수**다(2026-09-27 기준 405개, 404 통과). 기대 개수는 그 baseline에 이 태스크가 추가한 만큼을 더한 값으로 본다.

### 이미 존재하는(이번 계획이 그대로 쓰는) 타입

- `SushiSurvival.Core.BossEntranceLogic.Progress(float elapsed, float duration)` → 0~1 클램프, `duration <= 0`이면 1
- `SushiSurvival.Core.ObjectPool<T>(Func<T> factory, Action<T> onGet, Action<T> onRelease)` — `Get()`, `Release(T)`
- `SushiSurvival.Core.CircleTextureFactory.CreateSprite(int size, float innerRatio, Color color)`, 상수 `RingInnerRatio`(0.85) — 스프라이트 PPU는 100이라 `size` 픽셀이 `size/100` 월드 유닛이다
- `SushiSurvival.Core.JuiceDirector.Instance`, private `TriggerShake(float magnitude, float duration)`
- `SushiSurvival.Enemies.Boss.BossAimLogic.ChargeDirection(Vector2 from, Vector2 to, Vector2 fallback)` → 단위 벡터
- `BossData : MonsterData` — `moveSpeed`(1.6), `GetPhaseValues(int)`; `BossPhaseValues.chargeWindup/chargeDuration/chargeSpeedScale/chargeRecovery`

---

## 브랜치 / PR 분할

| PR | 브랜치 | 태스크 | 시작 시점 | 의존 |
|---|---|---|---|---|
| **A** | `feature/boss-charge-effects-logic` | Task 1 (`ChargeEffectsLogic` + 테스트) | `main`에서 지금 | 없음 |
| **B** | `feature/fading-sprite-effect` | Task 2 (`FadingSpriteEffect` + `JuiceDirector.Shake`) | `main`에서 지금 (A와 병렬) | 없음 |
| **C** | `feature/boss-charge-effects` | Task 3 (`BossChargeEffects` + `ChargeRoutine` 연결) | **A·B 병합 후** | A의 `ChargeDistance`·`LockDelay`, B의 `FadingSpriteEffect`·`Shake` |
| **D** | `feature/boss-charge-effects-wiring` | Task 4 (`Boss.prefab`에 컴포넌트 추가, 사용자 에디터 작업) | **C 병합 후** | C |

A와 B는 파일이 안 겹치고 서로를 참조하지 않아 동시에 진행할 수 있다(`FadingSpriteEffect`가 A의 함수를 안 쓰도록 페이드를 인라인했다). 이번 작업은 **삭제나 시그니처 변경이 없고 전부 추가**라 이전 소환 작업 같은 원자적 묶음이 필요 없다. C의 `ChargeRoutine` 수정은 컴포넌트가 없으면 기존 동작이라 D보다 먼저 병합돼도 안전하다.

---

## File Structure

| 파일 | 역할 |
|---|---|
| `unity/Assets/_Project/Scripts/Enemies/Boss/ChargeEffectsLogic.cs` (신규) | 돌진 거리, 방향 확정 지연 |
| `unity/Assets/_Project/Scripts/Core/FadingSpriteEffect.cs` (신규) | 스프라이트를 놓고 커지며 사라지는 풀링 효과 |
| `unity/Assets/_Project/Scripts/Core/JuiceDirector.cs` (수정) | 공개 `Shake(float, float)` 추가 |
| `unity/Assets/_Project/Scripts/Enemies/Boss/BossChargeEffects.cs` (신규) | 예고선·잔상·먼지·충격파 총괄 |
| `unity/Assets/_Project/Scripts/Enemies/Boss/BossController.cs` (수정) | `ChargeRoutine`에서 효과 호출 |
| `unity/Assets/Tests/EditMode/ChargeEffectsLogicTests.cs` (신규) | Task 1 검증 |
| `unity/Assets/_Project/Prefabs/Boss.prefab` (수정, PR D) | 루트에 `Boss Charge Effects` 컴포넌트 추가 |

---

# PR A — `ChargeEffectsLogic` (`main`에서 지금 시작)

### Task 1: 돌진 거리·방향 확정 지연 계산 (TDD)

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git checkout -b feature/boss-charge-effects-logic
```

**Files:**
- Create: `unity/Assets/_Project/Scripts/Enemies/Boss/ChargeEffectsLogic.cs`
- Test: `unity/Assets/Tests/EditMode/ChargeEffectsLogicTests.cs`

**Interfaces:**
- Produces (Task 3이 사용):
  - `ChargeEffectsLogic.ChargeDistance(float moveSpeed, float speedScale, float duration)` → `float`
  - `ChargeEffectsLogic.LockDelay(float windup, float lockSeconds)` → `float`

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/ChargeEffectsLogicTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.Enemies.Boss;

namespace SushiSurvival.EditModeTests
{
    public class ChargeEffectsLogicTests
    {
        [Test]
        public void ChargeDistance_PhaseOneValues()
        {
            // 이동속도 1.6 × 속도배율 4.5 × 돌진 0.55초
            Assert.AreEqual(3.96f, ChargeEffectsLogic.ChargeDistance(1.6f, 4.5f, 0.55f), 0.01f);
        }

        [Test]
        public void ChargeDistance_PhaseTwoValues()
        {
            Assert.AreEqual(5.28f, ChargeEffectsLogic.ChargeDistance(1.6f, 5.5f, 0.6f), 0.01f);
        }

        [Test]
        public void ChargeDistance_NegativeInputs_ClampToZero()
        {
            Assert.AreEqual(0f, ChargeEffectsLogic.ChargeDistance(1.6f, 4.5f, -1f), 0.001f);
            Assert.AreEqual(0f, ChargeEffectsLogic.ChargeDistance(-1f, 4.5f, 0.55f), 0.001f);
        }

        [Test]
        public void LockDelay_SubtractsLockFromWindup()
        {
            Assert.AreEqual(0.65f, ChargeEffectsLogic.LockDelay(0.8f, 0.15f), 0.001f);
            Assert.AreEqual(0.45f, ChargeEffectsLogic.LockDelay(0.6f, 0.15f), 0.001f);
        }

        [Test]
        public void LockDelay_ZeroLock_EqualsWindup()
        {
            Assert.AreEqual(0.8f, ChargeEffectsLogic.LockDelay(0.8f, 0f), 0.001f);
        }

        [Test]
        public void LockDelay_LockLongerThanWindup_ClampsToZero()
        {
            Assert.AreEqual(0f, ChargeEffectsLogic.LockDelay(0.1f, 0.5f), 0.001f);
        }

        [Test]
        public void LockDelay_NegativeLock_IsTreatedAsZero()
        {
            Assert.AreEqual(0.8f, ChargeEffectsLogic.LockDelay(0.8f, -1f), 0.001f);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해 실패 확인**

"테스트 실행"을 돌린다. Expected: `error CS0103` — `ChargeEffectsLogic`이 없어 컴파일 에러(정상, Step 3에서 해결).

- [ ] **Step 3: 구현**

`unity/Assets/_Project/Scripts/Enemies/Boss/ChargeEffectsLogic.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Enemies.Boss
{
    /// <summary>돌진 연출이 쓰는 순수 계산. 화면 반영과 시간 진행은 BossChargeEffects·BossController가 맡는다.</summary>
    public static class ChargeEffectsLogic
    {
        /// <summary>
        /// 돌진이 가는 최대 거리 = 이동속도 × 속도배율 × 돌진 시간. 예고선 길이가 이 값이다.
        /// 실제 이동은 플레이어에게 막힐 수 있어서 "최대 도달 거리"다.
        /// </summary>
        public static float ChargeDistance(float moveSpeed, float speedScale, float duration)
            => Mathf.Max(0f, moveSpeed) * Mathf.Max(0f, speedScale) * Mathf.Max(0f, duration);

        /// <summary>
        /// 예고가 시작된 뒤 돌진 방향을 확정하고 예고선을 고정하는 시점(초) = 예고 시간 − lockSeconds.
        /// lockSeconds가 0이면 예고 종료 순간(기존 동작)이고, 예고보다 길면 0(즉시 확정)이다.
        /// </summary>
        public static float LockDelay(float windup, float lockSeconds)
        {
            float clampedWindup = Mathf.Max(0f, windup);
            return Mathf.Clamp(clampedWindup - Mathf.Max(0f, lockSeconds), 0f, clampedWindup);
        }
    }
}
```

- [ ] **Step 4: 테스트 실행해 통과 확인**

"테스트 실행"을 돌린다. Expected: `error CS` 없음, baseline + 7, 실패는 알려진 기존 1건뿐. `.meta` 확인:

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git status --short unity/Assets/_Project/Scripts/Enemies/Boss/ChargeEffectsLogic.cs.meta unity/Assets/Tests/EditMode/ChargeEffectsLogicTests.cs.meta
```

- [ ] **Step 5: 커밋, 푸시, PR A**

이 계획서 파일(`docs/superpowers/plans/2026-09-27-boss-charge-impact.md`)이 아직 커밋 전이면 이 PR에 같이 넣는다.

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/Enemies/Boss/ChargeEffectsLogic.cs unity/Assets/_Project/Scripts/Enemies/Boss/ChargeEffectsLogic.cs.meta unity/Assets/Tests/EditMode/ChargeEffectsLogicTests.cs unity/Assets/Tests/EditMode/ChargeEffectsLogicTests.cs.meta docs/superpowers/plans/2026-09-27-boss-charge-impact.md
git commit -m "$(cat <<'EOF'
feat: 보스 돌진 연출용 순수 계산(ChargeEffectsLogic)

돌진 최대 거리와 방향 확정 지연 시점을 계산한다. 아직 아무도 안 불러
게임 동작은 바뀌지 않는다. 구현 계획서도 같이 커밋.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/boss-charge-effects-logic
gh pr create --title "feat: 돌진 연출 계산 로직 (보스 돌진 A)" --body "$(cat <<'EOF'
## 요약
보스 돌진 임팩트 연출의 첫 조각. 예고선 길이가 될 돌진 최대 거리(`ChargeDistance`)와 방향 확정 시점(`LockDelay`)을 계산하는 순수 함수입니다. 아직 아무도 안 불러서 게임 동작은 바뀌지 않습니다.

- 스펙: `docs/superpowers/specs/2026-09-27-boss-charge-impact-design.md`
- 계획: `docs/superpowers/plans/2026-09-27-boss-charge-impact.md` (PR A)

## 테스트
- EditMode baseline + 7 통과 (알려진 기존 실패 1건 제외)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

**A를 올린 뒤 멈추지 않고 곧바로 Task 2(B)를 `main`에서 새로 판다.**

---

# PR B — `FadingSpriteEffect` + `JuiceDirector.Shake` (`main`에서 지금, A와 병렬)

### Task 2: 공통 페이드 효과와 공개 흔들림

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git checkout -b feature/fading-sprite-effect
```

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/FadingSpriteEffect.cs`
- Modify: `unity/Assets/_Project/Scripts/Core/JuiceDirector.cs`

**Interfaces:**
- Produces (Task 3이 사용):
  - `FadingSpriteEffect.Create(Transform parent, Action<FadingSpriteEffect> release)` → `FadingSpriteEffect` (정적 팩토리, `SpriteRenderer` 포함)
  - `FadingSpriteEffect.Play(Sprite sprite, Vector3 position, float rotationDegrees, Color color, float startScale, float endScale, float lifetime, int sortingLayerId, int sortingOrder, bool flipX = false)`
  - `JuiceDirector.Shake(float magnitude, float duration)`

MonoBehaviour라 전용 테스트는 없다(컴파일+회귀). 풀 반환 경로부터 확인한다: 수명이 끝나면 `release` 콜백을 불러 스스로 풀에 돌려준다.

- [ ] **Step 1: `FadingSpriteEffect.cs` 작성**

`unity/Assets/_Project/Scripts/Core/FadingSpriteEffect.cs`:

```csharp
using System;
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 스프라이트 하나를 놓고, 커지거나 작아지며 투명해졌다가 스스로 풀에 반환하는 효과.
    /// 돌진 잔상·먼지·충격파 링이 공통으로 쓴다. Time.deltaTime을 쓰므로 timeScale이 0이면 함께 멈춘다.
    /// </summary>
    public class FadingSpriteEffect : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Action<FadingSpriteEffect> _release;

        private Color _color;
        private float _startScale;
        private float _endScale;
        private float _lifetime;
        private float _age;

        public static FadingSpriteEffect Create(Transform parent, Action<FadingSpriteEffect> release)
        {
            var go = new GameObject("FadingSpriteEffect");
            go.transform.SetParent(parent, false);

            var effect = go.AddComponent<FadingSpriteEffect>();
            effect._renderer = go.AddComponent<SpriteRenderer>();
            effect._release = release;
            return effect;
        }

        public void Play(Sprite sprite, Vector3 position, float rotationDegrees, Color color,
                         float startScale, float endScale, float lifetime,
                         int sortingLayerId, int sortingOrder, bool flipX = false)
        {
            _renderer.sprite = sprite;
            _renderer.flipX = flipX;
            _renderer.sortingLayerID = sortingLayerId;
            _renderer.sortingOrder = sortingOrder;

            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, rotationDegrees));

            _color = color;
            _startScale = startScale;
            _endScale = endScale;
            _lifetime = lifetime;
            _age = 0f;

            Apply(0f);
        }

        private void Update()
        {
            _age += Time.deltaTime;

            if (_age >= _lifetime)
            {
                _release?.Invoke(this);
                return;
            }

            Apply(BossEntranceLogic.Progress(_age, _lifetime));
        }

        private void Apply(float progress)
        {
            transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, progress);

            Color color = _color;
            color.a = _color.a * (1f - progress);
            _renderer.color = color;
        }
    }
}
```

- [ ] **Step 2: `JuiceDirector.Shake` 추가**

`unity/Assets/_Project/Scripts/Core/JuiceDirector.cs`의 `PlayerHit()` 메서드 바로 위에 추가한다:

```csharp
        /// <summary>
        /// 다른 연출(보스 돌진 정지 등)이 원하는 세기·시간으로 화면을 흔든다. 이미 흔들리는 중이면
        /// 기존 규칙대로 합쳐진다(지속시간은 늘리고 진폭은 큰 쪽이 우선).
        /// </summary>
        public void Shake(float magnitude, float duration) => TriggerShake(magnitude, duration);

        public void PlayerHit()
```

- [ ] **Step 3: 컴파일 + 회귀 확인**

"테스트 실행"을 돌린다. Expected: `error CS` 없음, baseline과 같은 개수(새 테스트 없음), 실패는 알려진 기존 1건뿐.
새 `.meta` 확인:

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git status --short unity/Assets/_Project/Scripts/Core/FadingSpriteEffect.cs unity/Assets/_Project/Scripts/Core/FadingSpriteEffect.cs.meta
```

- [ ] **Step 4: 커밋, 푸시, PR B**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/Core/FadingSpriteEffect.cs unity/Assets/_Project/Scripts/Core/FadingSpriteEffect.cs.meta unity/Assets/_Project/Scripts/Core/JuiceDirector.cs
git commit -m "$(cat <<'EOF'
feat: 공통 페이드 스프라이트 효과(FadingSpriteEffect)와 JuiceDirector.Shake

잔상·먼지·충격파 링이 공통으로 쓸 풀링 효과와, 외부 연출이 화면을
흔들 수 있는 공개 Shake를 추가한다. 추가만 하는 변경이라 게임 동작은
바뀌지 않는다.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/fading-sprite-effect
gh pr create --title "feat: 페이드 스프라이트 효과·공개 Shake (보스 돌진 B)" --body "$(cat <<'EOF'
## 요약
보스 돌진 연출에 쓸 부품 두 개. `FadingSpriteEffect`는 스프라이트를 놓고 커지며 사라지다가 스스로 풀에 반환하는 효과이고(잔상·먼지·충격파 링이 공통으로 씀), `JuiceDirector.Shake`는 기존 private `TriggerShake`를 감싼 공개 메서드입니다. 아직 아무도 안 불러서 게임 동작은 바뀌지 않습니다.

- 스펙: `docs/superpowers/specs/2026-09-27-boss-charge-impact-design.md`
- 계획: `docs/superpowers/plans/2026-09-27-boss-charge-impact.md` (PR B)

## 협업자 확인 부탁
`JuiceDirector.cs`에 공개 메서드 한 줄이 추가됩니다. 기존 코드는 안 건드립니다.

## 테스트
- EditMode baseline 유지 (컴파일 확인, MonoBehaviour라 신규 테스트 없음)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

---

# PR C — `BossChargeEffects` + `ChargeRoutine` 연결 (A·B 병합 후)

### Task 3: 연출 총괄 컴포넌트와 돌진 루틴 연결

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git log --oneline -4   # A, B 병합 커밋이 보여야 한다
git checkout -b feature/boss-charge-effects
```

**Files:**
- Create: `unity/Assets/_Project/Scripts/Enemies/Boss/BossChargeEffects.cs`
- Modify: `unity/Assets/_Project/Scripts/Enemies/Boss/BossController.cs`

**Interfaces:**
- Consumes (A): `ChargeEffectsLogic.ChargeDistance`, `ChargeEffectsLogic.LockDelay`
- Consumes (B): `FadingSpriteEffect.Create/Play`, `JuiceDirector.Shake`
- Consumes (기존): `ObjectPool<T>`, `CircleTextureFactory`, `BossAimLogic.ChargeDirection`
- Produces (Task 4가 씬에서 붙임): `BossChargeEffects`(공개 클래스, `MonoBehaviour`) — 공개 API: `ChargeLockSeconds`, `BeginTelegraph(float)`, `AimTelegraph(Vector2, Vector2)`, `LockTelegraph()`, `BeginTrail()`, `EndTrail()`, `PlayImpact(Vector2)`

MonoBehaviour라 전용 테스트는 없다(컴파일+회귀).

- [ ] **Step 1: `BossChargeEffects.cs` 작성**

`unity/Assets/_Project/Scripts/Enemies/Boss/BossChargeEffects.cs`:

```csharp
using System.Collections;
using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.Enemies.Boss
{
    /// <summary>
    /// 보스 돌진의 시각 연출(예고선·잔상·먼지·충격파)을 총괄한다. 새 아트 없이 전부 런타임으로
    /// 만든다. 판정과 이동은 모르고 BossController.ChargeRoutine이 단계마다 부르기만 한다.
    /// </summary>
    public class BossChargeEffects : MonoBehaviour
    {
        // 64픽셀 원판·링 스프라이트의 월드 크기(CircleTextureFactory의 PPU 100 기준).
        private const float DiscWorldSize = 0.64f;

        [Header("방향 확정")]
        [Tooltip("예고가 끝나기 이만큼 전에 돌진 방향을 확정하고 예고선을 고정한다. " +
                 "0이면 예고 종료 순간에 확정한다(연출 추가 전의 기존 동작).")]
        [SerializeField] private float chargeLockSeconds = 0.15f;

        [Header("예고선")]
        [SerializeField] private float lineWidth = 0.12f;
        [SerializeField] private Color lineColor = new Color(1f, 0.15f, 0.15f, 0.35f);
        [SerializeField] private Color lineLockedColor = new Color(1f, 0.2f, 0.2f, 0.8f);
        [Tooltip("고정되는 순간 선이 이 배율만큼 굵어진다.")]
        [SerializeField] private float lineLockedWidthScale = 1.5f;

        [Header("잔상")]
        [SerializeField] private float ghostInterval = 0.05f;
        [SerializeField] private Color ghostColor = new Color(1f, 0.35f, 0.35f, 0.5f);
        [SerializeField] private float ghostLifetime = 0.25f;

        [Header("먼지")]
        [SerializeField] private float dustInterval = 0.04f;
        [SerializeField] private Color dustColor = new Color(0.85f, 0.75f, 0.55f, 0.6f);
        [Tooltip("먼지 원판의 지름(월드 단위).")]
        [SerializeField] private float dustStartSize = 0.15f;
        [SerializeField] private float dustEndSize = 0.4f;
        [SerializeField] private float dustLifetime = 0.35f;
        [Tooltip("먼지가 발치에서 좌우로 흩어지는 폭.")]
        [SerializeField] private float dustSpread = 0.25f;
        [Tooltip("보스 위치에서 발치까지 내려가는 거리.")]
        [SerializeField] private float dustFootOffset = 0.3f;

        [Header("충격파 / 화면 흔들림")]
        [SerializeField] private Color shockwaveColor = new Color(1f, 0.85f, 0.85f, 0.8f);
        [SerializeField] private float shockwaveStartRadius = 0.3f;
        [SerializeField] private float shockwaveEndRadius = 2.2f;
        [SerializeField] private float shockwaveLifetime = 0.35f;
        [SerializeField] private float shakeMagnitude = 0.25f;
        [SerializeField] private float shakeDuration = 0.25f;

        public float ChargeLockSeconds => chargeLockSeconds;

        private SpriteRenderer _bossRenderer;
        private Transform _root;
        private SpriteRenderer _line;
        private Sprite _pixelSprite;
        private Sprite _discSprite;
        private Sprite _ringSprite;
        private ObjectPool<FadingSpriteEffect> _pool;
        private Coroutine _trailRoutine;
        private float _lineLength;
        private bool _lineLocked;

        private void Awake()
        {
            _bossRenderer = GetComponent<SpriteRenderer>();
            if (_bossRenderer == null) _bossRenderer = GetComponentInChildren<SpriteRenderer>();

            // 보스를 따라 움직이면 안 되므로 효과는 씬 루트 아래에 둔다.
            _root = new GameObject("ChargeEffectsRoot").transform;

            _pixelSprite = CreatePixelSprite();
            _discSprite = CircleTextureFactory.CreateSprite(64, 0f, Color.white);
            _ringSprite = CircleTextureFactory.CreateSprite(64, CircleTextureFactory.RingInnerRatio, Color.white);

            var lineObject = new GameObject("ChargeTelegraph");
            lineObject.transform.SetParent(_root, false);
            _line = lineObject.AddComponent<SpriteRenderer>();
            _line.sprite = _pixelSprite;
            lineObject.SetActive(false);

            _pool = new ObjectPool<FadingSpriteEffect>(
                factory: () => FadingSpriteEffect.Create(_root, effect => _pool.Release(effect)),
                onGet: effect => effect.gameObject.SetActive(true),
                onRelease: effect => effect.gameObject.SetActive(false));
        }

        private void OnDisable()
        {
            // 예고나 돌진 도중에 보스가 죽어도 선이 남거나 코루틴이 이어지지 않게 한다.
            EndTrail();
            HideTelegraph();
        }

        private void OnDestroy()
        {
            if (_root != null) Destroy(_root.gameObject);
            DestroySprite(_pixelSprite);
            DestroySprite(_discSprite);
            DestroySprite(_ringSprite);
        }

        // ---- 예고선 ----

        public void BeginTelegraph(float length)
        {
            _lineLength = length;
            _lineLocked = false;
            ApplyLine(_line.transform.position, _line.transform.eulerAngles.z);
            _line.gameObject.SetActive(true);
        }

        /// <summary>선의 시작점·방향을 갱신한다. 고정된 뒤에는 무시한다. 방향 계산은 실제 돌진과 같은 함수를 쓴다.</summary>
        public void AimTelegraph(Vector2 from, Vector2 toward)
        {
            if (_lineLocked) return;

            Vector2 direction = BossAimLogic.ChargeDirection(from, toward, Vector2.right);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            ApplyLine(from, angle);
        }

        public void LockTelegraph()
        {
            _lineLocked = true;
            ApplyLine(_line.transform.position, _line.transform.eulerAngles.z);
        }

        private void ApplyLine(Vector3 position, float angleDegrees)
        {
            float width = _lineLocked ? lineWidth * lineLockedWidthScale : lineWidth;

            _line.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angleDegrees));
            _line.transform.localScale = new Vector3(_lineLength, width, 1f);
            _line.color = _lineLocked ? lineLockedColor : lineColor;
            SyncSorting(_line);
        }

        private void HideTelegraph()
        {
            if (_line != null) _line.gameObject.SetActive(false);
        }

        // ---- 돌진 중 ----

        /// <summary>예고선을 끄고 잔상·먼지를 뿌리기 시작한다.</summary>
        public void BeginTrail()
        {
            HideTelegraph();

            if (_trailRoutine == null)
                _trailRoutine = StartCoroutine(TrailRoutine());
        }

        public void EndTrail()
        {
            if (_trailRoutine == null) return;

            StopCoroutine(_trailRoutine);
            _trailRoutine = null;
        }

        private IEnumerator TrailRoutine()
        {
            float ghostTimer = 0f;
            float dustTimer = 0f;

            while (true)
            {
                ghostTimer -= Time.deltaTime;
                dustTimer -= Time.deltaTime;

                if (ghostTimer <= 0f)
                {
                    SpawnGhost();
                    ghostTimer += ghostInterval;
                }

                if (dustTimer <= 0f)
                {
                    SpawnDust();
                    dustTimer += dustInterval;
                }

                yield return null;
            }
        }

        private void SpawnGhost()
        {
            if (_bossRenderer == null || _bossRenderer.sprite == null) return;

            Transform boss = _bossRenderer.transform;
            float scale = Mathf.Abs(boss.lossyScale.x);

            _pool.Get().Play(_bossRenderer.sprite, boss.position, boss.eulerAngles.z, ghostColor,
                             scale, scale, ghostLifetime,
                             _bossRenderer.sortingLayerID, _bossRenderer.sortingOrder - 1, _bossRenderer.flipX);
        }

        private void SpawnDust()
        {
            Vector3 position = transform.position
                + new Vector3(Random.Range(-dustSpread, dustSpread), -dustFootOffset, 0f);

            _pool.Get().Play(_discSprite, position, 0f, dustColor,
                             dustStartSize / DiscWorldSize, dustEndSize / DiscWorldSize, dustLifetime,
                             SortingLayerId(), SortingOrder() - 1);
        }

        // ---- 정지 순간 ----

        /// <summary>돌진이 멈추는 지점에 충격파 링을 퍼뜨리고 화면을 흔든다. 링은 피해가 없는 시각 효과다.</summary>
        public void PlayImpact(Vector2 position)
        {
            _pool.Get().Play(_ringSprite, position, 0f, shockwaveColor,
                             shockwaveStartRadius * 2f / DiscWorldSize, shockwaveEndRadius * 2f / DiscWorldSize,
                             shockwaveLifetime, SortingLayerId(), SortingOrder() - 1);

            if (JuiceDirector.Instance != null)
                JuiceDirector.Instance.Shake(shakeMagnitude, shakeDuration);
        }

        // ---- 공통 ----

        private int SortingLayerId() => _bossRenderer != null ? _bossRenderer.sortingLayerID : 0;
        private int SortingOrder() => _bossRenderer != null ? _bossRenderer.sortingOrder : 0;

        private void SyncSorting(SpriteRenderer target)
        {
            target.sortingLayerID = SortingLayerId();
            target.sortingOrder = SortingOrder() - 1;
        }

        private static Sprite CreatePixelSprite()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            // 피벗을 왼쪽 가운데에 두고 PPU 1로 만들어, 스케일 x가 곧 선의 길이가 되게 한다.
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0f, 0.5f), 1f);
        }

        private static void DestroySprite(Sprite sprite)
        {
            if (sprite == null) return;

            Destroy(sprite.texture);
            Destroy(sprite);
        }
    }
}
```

- [ ] **Step 2: `BossController`에 효과 필드와 조회 추가**

`unity/Assets/_Project/Scripts/Enemies/Boss/BossController.cs`의 `_ai` 필드 아래에:

```csharp
        private EnemyBase _enemy;
        private EnemyAI _ai;
        private BossChargeEffects _effects;
```

`Awake()`를:

```csharp
        private void Awake()
        {
            _enemy = GetComponent<EnemyBase>();
            _ai = GetComponent<EnemyAI>();
            _effects = GetComponent<BossChargeEffects>();
        }
```

`Activate()`의 `if (_ai == null) _ai = GetComponent<EnemyAI>();` 바로 아래에:

```csharp
            if (_effects == null) _effects = GetComponent<BossChargeEffects>();
```

- [ ] **Step 3: `ChargeRoutine` 교체**

`ChargeRoutine()`과 그 위 요약 주석 전체를 다음으로 교체한다:

```csharp
        /// <summary>
        /// 돌진: ① 멈춰서 붉게 번쩍이며 예고(예고선이 플레이어를 따라간다) → ② 예고 종료 직전에 방향을
        /// 확정하고 예고선을 고정 → ③ 그 방향으로 돌진(잔상·먼지) → ④ 멈춰서 충격파 → ⑤ 무방비로 서 있는다(반격 기회).
        /// 방향을 확정하기 전까지 움직여서 각을 만들어 두면 피할 수 있다. 피해는 보스의 접촉 데미지가 그대로 준다.
        /// 연출(_effects)이 없으면 방향을 예고 종료 순간에 확정하는 기존 동작과 같다.
        /// </summary>
        private IEnumerator ChargeRoutine()
        {
            BossPhaseValues values = bossData.GetPhaseValues(_phase);

            float chargeDistance = ChargeEffectsLogic.ChargeDistance(
                bossData.moveSpeed, values.chargeSpeedScale, values.chargeDuration);
            float lockSeconds = _effects != null ? _effects.ChargeLockSeconds : 0f;
            float lockDelay = ChargeEffectsLogic.LockDelay(values.chargeWindup, lockSeconds);

            if (spriteFlasher != null)
                spriteFlasher.Flash(Color.red, values.chargeWindup);

            if (_effects != null)
                _effects.BeginTelegraph(chargeDistance);

            // ① 방향을 확정하기 전까지 예고선이 플레이어를 실시간으로 따라간다.
            float elapsed = 0f;
            while (elapsed < lockDelay)
            {
                if (_effects != null && _player != null)
                    _effects.AimTelegraph(transform.position, _player.transform.position);

                yield return null;
                elapsed += Time.deltaTime;
            }

            // ② 방향 확정. 예고선 고정과 같은 시점이라 선이 곧 실제 돌진 방향이다.
            Vector2 from = transform.position;
            Vector2 to = _player != null ? (Vector2)_player.transform.position : from;
            Vector2 direction = BossAimLogic.ChargeDirection(from, to, Vector2.right);

            if (_effects != null)
            {
                _effects.AimTelegraph(from, to);
                _effects.LockTelegraph();
            }

            float remainingWindup = values.chargeWindup - lockDelay;
            if (remainingWindup > 0f)
                yield return new WaitForSeconds(remainingWindup);

            // ③ 돌진
            _ai.LockedDirection = direction;
            _ai.MoveScale = values.chargeSpeedScale;

            if (_effects != null)
                _effects.BeginTrail();

            yield return new WaitForSeconds(values.chargeDuration);

            // ④ 정지
            _ai.LockedDirection = Vector2.zero;
            _ai.MoveScale = 0f;

            if (_effects != null)
            {
                _effects.EndTrail();
                _effects.PlayImpact(transform.position);
            }

            // ⑤ 반격 기회
            yield return new WaitForSeconds(values.chargeRecovery);
        }
```

- [ ] **Step 4: 컴파일 + 회귀 확인**

"테스트 실행"을 돌린다. Expected: `error CS` 없음, baseline과 같은 개수(새 테스트 없음), 실패는 알려진 기존 1건뿐.
새 `.meta` 확인:

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git status --short unity/Assets/_Project/Scripts/Enemies/Boss/BossChargeEffects.cs unity/Assets/_Project/Scripts/Enemies/Boss/BossChargeEffects.cs.meta
```

- [ ] **Step 5: 커밋, 푸시, PR C**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/Enemies/Boss/BossChargeEffects.cs unity/Assets/_Project/Scripts/Enemies/Boss/BossChargeEffects.cs.meta unity/Assets/_Project/Scripts/Enemies/Boss/BossController.cs
git commit -m "$(cat <<'EOF'
feat: 보스 돌진 임팩트 연출(예고선·잔상·먼지·충격파) 추가

BossChargeEffects가 붉은 예고선(플레이어를 따라가다 종료 0.15초 전에
고정), 돌진 중 잔상·먼지, 정지 순간 충격파 링과 화면 흔들림을 런타임으로
만든다. ChargeRoutine이 단계마다 호출하고, 컴포넌트가 없으면 기존 돌진과
같다.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/boss-charge-effects
gh pr create --title "feat: 보스 돌진 임팩트 연출 (보스 돌진 C)" --body "$(cat <<'EOF'
## 요약
보스 돌진에 연출을 얹습니다. 예고 동안 붉은 선이 플레이어를 따라가다 예고 종료 0.15초 전에 굵어지며 고정되고, 돌진 중 잔상과 먼지가 남고, 멈추는 순간 충격파 링과 화면 흔들림이 나옵니다. 전부 런타임 생성이라 새 아트가 없고, 충격파는 피해가 없는 시각 효과입니다.

`BossChargeEffects` 컴포넌트가 프리팹에 없으면(PR D 전) 연출 호출을 전부 건너뛰어 돌진은 지금과 완전히 같습니다.

- 스펙: `docs/superpowers/specs/2026-09-27-boss-charge-impact-design.md`
- 계획: `docs/superpowers/plans/2026-09-27-boss-charge-impact.md` (PR C)
- 선행 PR: A(계산 로직), B(`FadingSpriteEffect`·`Shake`)

## 게임플레이 변화 (하나)
방향 확정이 예고 종료 `chargeLockSeconds`(기본 0.15초) 전으로 당겨집니다. 예고선 고정과 실제 방향 확정을 같은 시점으로 맞추기 위함입니다. 0으로 두면 기존과 같습니다.

## 협업자 확인 부탁
`BossController.ChargeRoutine`(보스 코드)을 수정합니다.

## 테스트
- EditMode baseline 유지 (컴파일 확인, MonoBehaviour라 신규 테스트 없음)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

---

# PR D — `Boss.prefab`에 컴포넌트 추가 (C 병합 후, 사용자 에디터 작업)

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git log --oneline -3   # C 병합 커밋이 보여야 한다
git checkout -b feature/boss-charge-effects-wiring
```

### Task 4: `Boss.prefab`에 `Boss Charge Effects` 붙이기

에이전트는 프리팹 Inspector 값을 대신 편집하지 않는다(프로젝트 관례). 사용자가 아래를 따라 하고, 에이전트는 저장된 파일을 읽어 검증한다.

**1.** Project 창에서 `Assets/_Project/Prefabs/Boss`를 더블클릭해 프리팹 편집 모드로 들어간다.

**2.** 루트 `Boss` 오브젝트(이미 `Boss Controller`·`Sprite Flasher`가 붙어 있는 그 오브젝트)를 선택 → **Add Component → Boss Charge Effects**. 필드는 전부 기본값 그대로 둔다(연결할 것이 없다).

**3.** 프리팹 저장(Ctrl+S), 편집 모드를 나간다.

### 에이전트 검증

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요\unity"
git status --short Assets/_Project/Prefabs/Boss.prefab
g=$(grep -m1 'guid:' Assets/_Project/Scripts/Enemies/Boss/BossChargeEffects.cs.meta | awk '{print $2}')
echo "--- Boss.prefab에 BossChargeEffects 컴포넌트 (1이어야 함) ---"
grep -c "guid: $g" Assets/_Project/Prefabs/Boss.prefab
echo "--- 컴포넌트가 켜져 있는지, 기본값이 들어갔는지 ---"
grep -n -B8 -A6 "guid: $g" Assets/_Project/Prefabs/Boss.prefab | grep -E "m_Enabled|chargeLockSeconds|lineWidth"
```

Expected: 개수 1, `m_Enabled: 1`, `chargeLockSeconds: 0.15`, `lineWidth: 0.12`.

- [ ] **Step 1: 컴파일 + 회귀 확인**

"테스트 실행"을 돌린다(프리팹만 바뀌어 컴파일엔 영향 없지만 습관대로). Expected: `error CS` 없음, 실패는 알려진 기존 1건뿐.

- [ ] **Step 2: Play 모드 확인 (사용자)**

보스전까지 가서(테스트용으로 GameScene의 `bossSpawnTime`을 20초로 낮춰도 된다 — 확인 후 300으로 복구) 돌진 패턴이 나올 때까지 지켜본다.

- [ ] 예고 동안 보스에서 플레이어 쪽으로 **붉은 반투명 선**이 뻗고, 움직이면 **선이 따라온다**
- [ ] 예고가 끝나기 직전(약 0.15초 전)에 선이 **굵고 밝게 고정**되고, 그 뒤로는 움직여도 선이 안 따라온다
- [ ] 돌진이 그 **고정된 선 방향**으로 나간다(선과 실제 돌진 방향이 어긋나지 않는지)
- [ ] 돌진이 시작되면 예고선이 사라진다
- [ ] 돌진 중 보스 뒤로 **붉은 잔상**이 남고 발치에 **모래색 먼지**가 일어난다
- [ ] 멈추는 순간 **충격파 링**이 퍼지고 화면이 짧게 흔들린다(플레이어 피격 흔들림보다 세고 보스 착지보다 약한지)
- [ ] 1페이즈·2페이즈 모두 예고선 길이가 실제 돌진 거리와 대략 맞는지
- [ ] 예고 도중 또는 돌진 도중에 보스를 처치해도 선·잔상이 남지 않는지
- [ ] 레벨업 팝업 등으로 시간이 멈춘 동안 효과도 같이 멈추는지
- [ ] 소환(체력 임계)·메테오 시전은 이전과 똑같이 동작하는지

- [ ] **Step 3: 커밋, 푸시, PR D**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Prefabs/Boss.prefab
git status --short
git commit -m "$(cat <<'EOF'
feat: Boss 프리팹에 돌진 연출 컴포넌트(BossChargeEffects) 추가

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/boss-charge-effects-wiring
gh pr create --title "feat: Boss 프리팹에 돌진 연출 추가 (보스 돌진 D)" --body "$(cat <<'EOF'
## 요약
보스 돌진 임팩트 연출의 마지막 조각. `Boss.prefab` 루트에 `Boss Charge Effects` 컴포넌트를 붙였습니다(필드는 기본값). 이제 돌진 패턴에 예고선·잔상·먼지·충격파가 나옵니다.

- 스펙: `docs/superpowers/specs/2026-09-27-boss-charge-impact-design.md`
- 계획: `docs/superpowers/plans/2026-09-27-boss-charge-impact.md` (PR D)
- 선행 PR: A, B, C 전부 병합됨

## 협업자 확인 부탁
`Boss.prefab`(협업자가 만든 보스 프리팹)에 컴포넌트 하나를 추가했습니다. 기존 컴포넌트는 안 건드렸습니다.

## 테스트
- EditMode baseline 유지 (알려진 기존 실패 1건 제외)
- Play 모드로 예고선 추적·고정, 돌진 방향 일치, 잔상·먼지, 충격파·화면 흔들림, 보스 사망 시 잔여물 없음, 1·2페이즈 확인 완료(사용자)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

**여기서 멈추고 사용자가 PR D를 병합하길 기다린다.** 병합 후 `main` 동기화, 브랜치 정리, 메모리(`sushi-survival-project.md`) 업데이트로 마무리한다. 기획 문서 3번(보스전)이 이걸로 끝나고, 남는 건 4번(세계관 선택 대화, 대본 필요)과 와사비 연출 강화(사운드 시스템 필요)다.
