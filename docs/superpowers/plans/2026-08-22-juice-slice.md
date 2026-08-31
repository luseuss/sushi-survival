# 타격감 슬라이스 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 새 아트·사운드 없이 히트 플래시·히트스톱·화면흔들림·사망 파티클·UI
애니메이션을 넣어 전투 루프에 반응을 입힌다.

**Architecture:** `JuiceDirector`가 히트스톱·화면흔들림·사망 파티클을 한 곳에서
조정하는 씬 싱글톤이다. `EnemyBase`/`PlayerHealth`는 이벤트가 일어난 순간
`JuiceDirector.Instance`에 짧게 통보만 하고, 실제 정지·흔들림·파티클 생성은
전부 그 안에서 처리한다. 색 플래시는 `SpriteFlasher` 하나가 유일하게 소유해서
`BossController`의 기존 페이즈 플래시와 충돌하지 않게 한다.

**Tech Stack:** Unity 2022.3.62f3 LTS, Built-in 2D, Unity Test Framework(NUnit, EditMode)

**Spec:** `docs/superpowers/specs/2026-08-22-juice-slice-design.md`

## Global Constraints

- **git을 쓴다.** 작업은 `feature/juice-slice` 브랜치에서 진행한다(아직 없으면
  Task 0에서 만든다). **각 태스크 끝에 로컬 커밋만 한다 — push는 전부 끝나고
  플레이테스트까지 통과한 뒤 사용자에게 물어보고 한다.**
- Unity 프로젝트 루트는 `unity/` 서브폴더. 스크립트는
  `unity/Assets/_Project/Scripts/`, 테스트는 `unity/Assets/Tests/EditMode/`.
- **EditMode 테스트를 배치로 돌릴 때는 `-logFile -`(하이픈, stdout)을 쓴다.**
  `-logFile <파일경로>`로 주면 에셋 임포트 중 도메인 리로드 때 테스트 실행이
  조용히 취소되고 결과 파일이 안 생긴다. `-quit`도 `-runTests`와 같이 쓰면 안
  된다(결과 파일이 안 생김).
  ```
  & "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -projectPath "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요\unity" -runTests -testPlatform EditMode -testResults "$env:TEMP\juice-tests.xml" -logFile -
  ```
- 시작 시점 테스트 수는 **212개**다. 기존 테스트가 하나도 깨지면 안 된다.
- 네임스페이스: `SushiSurvival.Core` / `.Data` / `.Player` / `.Enemies` /
  `.Enemies.Boss` / `.UI`.
- 주석은 한국어로, "무엇"이 아니라 "왜"를 적는다.
- 수치는 하드코딩하지 않고 `[SerializeField]`로 노출한다 — 플레이테스트로 조정한다.
- **Unity Editor GUI 작업은 사용자가 직접 한다.** 에이전트는 코드와 배치
  테스트까지만 하고, 프리팹 조립·인스펙터 필드 연결은 Task 12로 넘긴다.

---

## File Structure

**신규 (`unity/Assets/_Project/Scripts/`)**

| 파일 | 책임 |
|---|---|
| `Core/DurationExtension.cs` | 순수 — 남은 시간을 합치지 않고 최댓값으로 늘림 |
| `Core/CameraShakeLogic.cs` | 순수 — 남은 시간 기준 흔들림 진폭 감쇠 |
| `Core/SpriteFlasher.cs` | 색 플래시의 유일한 소유자 |
| `Core/PooledParticlePlayer.cs` | 풀 재사용 시 파티클 재생 보장 |
| `Core/JuiceDirector.cs` | 히트스톱·흔들림·사망파티클 중앙 조정 |

**수정**

| 파일 | 변경 |
|---|---|
| `UI/HealthBarLogic.cs` | `MoveTowardsFill` 추가 |
| `Core/CameraFollow.cs` | 흔들리지 않는 기준 위치 + `SetShakeOffset` |
| `Enemies/EnemyBase.cs` | 피격 시 흰색 플래시, 사망 시(보스 제외) `JuiceDirector.EnemyDied()` |
| `Player/PlayerHealth.cs` | 피격 시 빨간 플래시 + `JuiceDirector.PlayerHit()` |
| `Enemies/Boss/BossController.cs` | 페이즈 플래시를 `SpriteFlasher`로 위임 |
| `UI/HealthBar.cs` | 스냅 대신 부드러운 보간 |
| `UI/BossHealthBar.cs` | 스냅 대신 부드러운 보간 |
| `UI/LevelUpPanel.cs` | 스케일인 연출 |

**테스트 (`unity/Assets/Tests/EditMode/`)**

`DurationExtensionTests.cs`, `CameraShakeLogicTests.cs` 신규. `HealthBarLogicTests.cs`에
케이스 추가(기존 파일).

---

## Task 0: 작업 브랜치 준비

**Files:** 없음(git만)

- [ ] **Step 1: 최신 main 확인 후 브랜치 생성**

```bash
git checkout main
git pull --ff-only
git checkout -b feature/juice-slice
```

Expected: `Switched to a new branch 'feature/juice-slice'`

---

## Task 1: 시간 합치기 — `DurationExtension`

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/DurationExtension.cs`
- Test: `unity/Assets/Tests/EditMode/DurationExtensionTests.cs`

**Interfaces:**
- Consumes: 없음
- Produces: `SushiSurvival.Core.DurationExtension.Extend(float remaining, float requested) → float`

- [ ] **Step 1: 실패하는 테스트를 작성한다**

`unity/Assets/Tests/EditMode/DurationExtensionTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class DurationExtensionTests
    {
        [Test]
        public void Extend_ReturnsRequested_WhenLongerThanRemaining()
        {
            Assert.AreEqual(0.5f, DurationExtension.Extend(0.1f, 0.5f), 0.0001f);
        }

        [Test]
        public void Extend_KeepsRemaining_WhenRequestedIsShorter()
        {
            // 짧은 히트가 나중에 들어와도 이미 걸린 긴 정지를 줄이면 안 된다.
            Assert.AreEqual(0.5f, DurationExtension.Extend(0.5f, 0.1f), 0.0001f);
        }

        [Test]
        public void Extend_ReturnsRequested_WhenRemainingIsZero()
        {
            Assert.AreEqual(0.3f, DurationExtension.Extend(0f, 0.3f), 0.0001f);
        }

        [Test]
        public void Extend_HandlesEqualValues()
        {
            Assert.AreEqual(0.2f, DurationExtension.Extend(0.2f, 0.2f), 0.0001f);
        }

        [Test]
        public void Extend_IgnoresNegativeRequest()
        {
            Assert.AreEqual(0.4f, DurationExtension.Extend(0.4f, -1f), 0.0001f);
        }
    }
}
```

- [ ] **Step 2: 테스트가 실패하는지 확인한다**

Global Constraints의 배치 명령으로 실행. 기대: 컴파일 에러 — `DurationExtension`을 찾을 수 없음.

- [ ] **Step 3: 최소 구현을 작성한다**

`unity/Assets/_Project/Scripts/Core/DurationExtension.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>여러 이벤트가 겹칠 때 정지·흔들림이 쌓여 버벅이지 않도록,
    /// 남은 시간을 새 요청과 합치지 않고 더 긴 쪽으로 늘린다.</summary>
    public static class DurationExtension
    {
        public static float Extend(float remaining, float requested)
            => Mathf.Max(remaining, requested);
    }
}
```

- [ ] **Step 4: 테스트가 통과하는지 확인한다**

기대: 217개 통과 (기존 212 + 신규 5).

- [ ] **Step 5: 커밋**

```bash
git add unity/Assets/_Project/Scripts/Core/DurationExtension.cs unity/Assets/_Project/Scripts/Core/DurationExtension.cs.meta unity/Assets/Tests/EditMode/DurationExtensionTests.cs unity/Assets/Tests/EditMode/DurationExtensionTests.cs.meta
git commit -m "feat: DurationExtension으로 정지·흔들림 합치기 규칙 추가"
```

(`.meta` 파일은 Unity Editor가 다음에 열릴 때 자동 생성된다. 이미 있으면 그대로 add한다.)

---

## Task 2: 화면흔들림 감쇠 — `CameraShakeLogic`

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/CameraShakeLogic.cs`
- Test: `unity/Assets/Tests/EditMode/CameraShakeLogicTests.cs`

**Interfaces:**
- Consumes: 없음
- Produces:
  - `SushiSurvival.Core.CameraShakeLogic.GetMagnitude(float remaining, float peakMagnitude) → float`
  - `SushiSurvival.Core.CameraShakeLogic.FalloffTail = 0.05f`

- [ ] **Step 1: 실패하는 테스트를 작성한다**

`unity/Assets/Tests/EditMode/CameraShakeLogicTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class CameraShakeLogicTests
    {
        [Test]
        public void GetMagnitude_ReturnsPeak_WhenRemainingAboveTail()
        {
            Assert.AreEqual(0.2f, CameraShakeLogic.GetMagnitude(1f, 0.2f), 0.0001f);
        }

        [Test]
        public void GetMagnitude_ReturnsPeak_ExactlyAtTail()
        {
            Assert.AreEqual(0.2f, CameraShakeLogic.GetMagnitude(CameraShakeLogic.FalloffTail, 0.2f), 0.0001f);
        }

        [Test]
        public void GetMagnitude_DecaysLinearly_InsideTail()
        {
            float half = CameraShakeLogic.FalloffTail / 2f;
            Assert.AreEqual(0.1f, CameraShakeLogic.GetMagnitude(half, 0.2f), 0.0001f);
        }

        [Test]
        public void GetMagnitude_IsZero_AtZeroRemaining()
        {
            Assert.AreEqual(0f, CameraShakeLogic.GetMagnitude(0f, 0.2f), 0.0001f);
        }

        [Test]
        public void GetMagnitude_IsZero_WhenRemainingIsNegative()
        {
            Assert.AreEqual(0f, CameraShakeLogic.GetMagnitude(-0.1f, 0.2f), 0.0001f);
        }
    }
}
```

- [ ] **Step 2: 테스트가 실패하는지 확인한다**

기대: 컴파일 에러 — `CameraShakeLogic`을 찾을 수 없음.

- [ ] **Step 3: 최소 구현을 작성한다**

`unity/Assets/_Project/Scripts/Core/CameraShakeLogic.cs`:

```csharp
namespace SushiSurvival.Core
{
    /// <summary>
    /// 흔들림 지속시간은 여러 히트가 겹치며 도중에 늘어날 수 있어(DurationExtension),
    /// "경과시간 대비 진폭"으로 감쇠 곡선을 그리면 기준점이 계속 바뀐다. 대신
    /// 남은 시간이 짧은 구간에서만 0으로 선형 감쇠한다.
    /// </summary>
    public static class CameraShakeLogic
    {
        /// <summary>남은 시간이 이 값 아래로 내려가면 그 구간에서 0으로 선형 감쇠한다.</summary>
        public const float FalloffTail = 0.05f;

        public static float GetMagnitude(float remaining, float peakMagnitude)
        {
            if (remaining <= 0f) return 0f;
            if (remaining >= FalloffTail) return peakMagnitude;

            return peakMagnitude * (remaining / FalloffTail);
        }
    }
}
```

- [ ] **Step 4: 테스트가 통과하는지 확인한다**

기대: 222개 통과 (217 + 5).

- [ ] **Step 5: 커밋**

```bash
git add unity/Assets/_Project/Scripts/Core/CameraShakeLogic.cs unity/Assets/_Project/Scripts/Core/CameraShakeLogic.cs.meta unity/Assets/Tests/EditMode/CameraShakeLogicTests.cs unity/Assets/Tests/EditMode/CameraShakeLogicTests.cs.meta
git commit -m "feat: CameraShakeLogic으로 화면흔들림 감쇠 계산 추가"
```

---

## Task 3: 체력바 보간 — `HealthBarLogic.MoveTowardsFill`

**Files:**
- Modify: `unity/Assets/_Project/Scripts/UI/HealthBarLogic.cs`
- Test: `unity/Assets/Tests/EditMode/HealthBarLogicTests.cs` (기존 파일에 추가)

**Interfaces:**
- Consumes: 없음
- Produces: `SushiSurvival.UI.HealthBarLogic.MoveTowardsFill(float current, float target, float maxDelta) → float`

- [ ] **Step 1: 실패하는 테스트를 기존 파일 끝에 추가한다**

`unity/Assets/Tests/EditMode/HealthBarLogicTests.cs`의 마지막 `}` (클래스 닫는 중괄호)
바로 앞에 아래 메서드들을 추가한다:

```csharp
        [Test]
        public void MoveTowardsFill_MovesPartway_TowardLowerTarget()
        {
            Assert.That(HealthBarLogic.MoveTowardsFill(1f, 0f, 0.3f), Is.EqualTo(0.7f).Within(0.0001f));
        }

        [Test]
        public void MoveTowardsFill_MovesPartway_TowardHigherTarget()
        {
            Assert.That(HealthBarLogic.MoveTowardsFill(0f, 1f, 0.3f), Is.EqualTo(0.3f).Within(0.0001f));
        }

        [Test]
        public void MoveTowardsFill_DoesNotOvershootTarget()
        {
            // maxDelta가 남은 거리보다 크면 목표에서 멈춰야지 지나치면 안 된다.
            Assert.That(HealthBarLogic.MoveTowardsFill(0.1f, 0f, 0.5f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void MoveTowardsFill_StaysAtTarget_WhenAlreadyThere()
        {
            Assert.That(HealthBarLogic.MoveTowardsFill(0.5f, 0.5f, 0.3f), Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void MoveTowardsFill_ClampsInputsToZeroOneRange()
        {
            Assert.That(HealthBarLogic.MoveTowardsFill(1.5f, 0f, 100f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void MoveTowardsFill_TreatsNegativeMaxDeltaAsZero()
        {
            Assert.That(HealthBarLogic.MoveTowardsFill(0.5f, 0f, -1f), Is.EqualTo(0.5f).Within(0.0001f));
        }
```

- [ ] **Step 2: 테스트가 실패하는지 확인한다**

기대: 컴파일 에러 — `HealthBarLogic.MoveTowardsFill`을 찾을 수 없음.

- [ ] **Step 3: 구현을 추가한다**

`unity/Assets/_Project/Scripts/UI/HealthBarLogic.cs` 전체를 아래로 교체한다:

```csharp
using UnityEngine;

namespace SushiSurvival.UI
{
    public static class HealthBarLogic
    {
        public static float ComputeFillAmount(float current, float max)
        {
            if (max <= 0f) return 0f;

            return Mathf.Clamp01(current / max);
        }

        /// <summary>current를 target 쪽으로 maxDelta만큼만 옮긴다. 스냅 대신
        /// 부드럽게 줄어드는 체력바에 쓴다.</summary>
        public static float MoveTowardsFill(float current, float target, float maxDelta)
        {
            return Mathf.MoveTowards(Mathf.Clamp01(current), Mathf.Clamp01(target), Mathf.Max(0f, maxDelta));
        }
    }
}
```

- [ ] **Step 4: 테스트가 통과하는지 확인한다**

기대: 228개 통과 (222 + 6).

- [ ] **Step 5: 커밋**

```bash
git add unity/Assets/_Project/Scripts/UI/HealthBarLogic.cs unity/Assets/Tests/EditMode/HealthBarLogicTests.cs
git commit -m "feat: HealthBarLogic에 MoveTowardsFill 추가"
```

---

## Task 4: 색 플래시의 유일한 소유자 — `SpriteFlasher`

순수 로직이 없는 MonoBehaviour라 TDD 대상이 아니다. 컴파일 확인으로 검증한다.

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/SpriteFlasher.cs`

**Interfaces:**
- Consumes: 없음
- Produces: `SushiSurvival.Core.SpriteFlasher.Flash(Color color, float duration)` (public MonoBehaviour 메서드)

- [ ] **Step 1: 구현한다**

`unity/Assets/_Project/Scripts/Core/SpriteFlasher.cs`:

```csharp
using System.Collections;
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// SpriteRenderer.color를 바꾸는 유일한 곳. 피격 플래시와 보스의 페이즈
    /// 플래시가 각자 color를 직접 건드리면 같은 프레임에 부딪혀 색이 꼬인다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFlasher : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Color _baseColor;
        private Coroutine _routine;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _baseColor = _renderer.color;
        }

        /// <summary>진행 중이던 플래시를 취소하고 새로 시작한다 — 연속 타격
        /// 중에는 계속 번쩍인 상태로 보이는 게 맞다.</summary>
        public void Flash(Color color, float duration)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FlashRoutine(color, duration));
        }

        private IEnumerator FlashRoutine(Color color, float duration)
        {
            _renderer.color = color;

            // 실시간으로 진행한다 — 맞는 순간이 곧 히트스톱이 걸리는 순간이라,
            // scaled 시간을 쓰면 timeScale이 0인 동안 거의 진행되지 않아 정지가
            // 풀릴 때까지 계속 하얗게 남는다.
            yield return new WaitForSecondsRealtime(duration);

            _renderer.color = _baseColor;
            _routine = null;
        }
    }
}
```

- [ ] **Step 2: 컴파일이 통과하는지 확인한다**

기대: 228개 통과, 컴파일 에러 없음.

- [ ] **Step 3: 커밋**

```bash
git add unity/Assets/_Project/Scripts/Core/SpriteFlasher.cs unity/Assets/_Project/Scripts/Core/SpriteFlasher.cs.meta
git commit -m "feat: SpriteFlasher로 색 플래시를 한 곳에서 관리"
```

---

## Task 5: 화면흔들림을 받는 카메라 — `CameraFollow` 리팩터

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Core/CameraFollow.cs`

**Interfaces:**
- Consumes: `SushiSurvival.Core.CameraFollowLogic.ComputeFollowPosition(Vector3, Vector3, float) → Vector3` (기존, 변경 없음)
- Produces: `SushiSurvival.Core.CameraFollow.SetShakeOffset(Vector2 offset)` (public 메서드)

- [ ] **Step 1: 전체 파일을 교체한다**

`unity/Assets/_Project/Scripts/Core/CameraFollow.cs` 전체를 아래로 교체한다.
기존 `transform.position`을 직접 목표로 보간하면, 흔들림 오프셋을 더했을 때
다음 프레임이 흔들린 위치를 기준으로 다시 보간해서 드리프트가 생긴다. 그래서
흔들리지 않는 기준 위치(`_basePosition`)를 따로 두고, 매 프레임 그 위에
오프셋을 얹은 값만 `transform.position`에 쓴다.

```csharp
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 카메라가 플레이어를 부드럽게 따라간다. 캐릭터 종류와 무관하므로
    /// 계란·간장새우·이나리 모두 그대로 재사용한다.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Tooltip("비워두면 시작할 때 Player 태그로 찾고, 그래도 없으면 " +
                 "GameManager가 스폰 후 SetTarget으로 알려줄 때까지 기다린다.")]
        [SerializeField] private Transform target;
        [Tooltip("클수록 빠르게 따라붙는다. 0이면 따라가지 않는다.")]
        [SerializeField] private float followSpeed = 5f;

        // 흔들리지 않는 실제 추적 위치. transform.position은 여기에 흔들림
        // 오프셋을 얹은 값이라, 흔들림이 다음 프레임의 추적 기준을 오염시키지 않는다.
        private Vector3 _basePosition;
        private Vector2 _shakeOffset;

        public void SetTarget(Transform newTarget) => target = newTarget;

        /// <summary>JuiceDirector가 매 프레임 흔들림 오프셋을 여기로 밀어넣는다.</summary>
        public void SetShakeOffset(Vector2 offset) => _shakeOffset = offset;

        private void Awake()
        {
            _basePosition = transform.position;
        }

        private void Start()
        {
            if (target != null) return;

            // 캐릭터 선택 화면에서는 아직 플레이어가 없는 것이 정상이므로
            // 못 찾아도 에러를 내지 않는다.
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float factor = followSpeed * Time.deltaTime;
            _basePosition = CameraFollowLogic.ComputeFollowPosition(_basePosition, target.position, factor);
            transform.position = _basePosition + (Vector3)_shakeOffset;
        }
    }
}
```

- [ ] **Step 2: 기존 테스트가 전부 통과하는지 확인한다**

`CameraFollowLogicTests.cs`는 `CameraFollowLogic`(순수 함수)만 테스트하므로 이
변경으로 깨지지 않는다. 기대: 228개 통과.

- [ ] **Step 3: 커밋**

```bash
git add unity/Assets/_Project/Scripts/Core/CameraFollow.cs
git commit -m "refactor: CameraFollow에 흔들림 오프셋 지원 추가"
```

---

## Task 6: 풀링된 파티클 재생 보장 — `PooledParticlePlayer`

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/PooledParticlePlayer.cs`

**Interfaces:**
- Consumes: 없음
- Produces: 없음(자동 동작 — `OnEnable`에서 스스로 재생)

- [ ] **Step 1: 구현한다**

`unity/Assets/_Project/Scripts/Core/PooledParticlePlayer.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 풀링된 오브젝트는 SetActive(true)로 재활성화되는데, ParticleSystem의
    /// "Play On Awake"는 최초 생성 때 한 번만 불리는 Awake에만 반응한다.
    /// 그래서 두 번째 재사용부터 파티클이 안 나올 수 있다. OnEnable에서
    /// 명시적으로 재생해 이 문제를 피한다.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class PooledParticlePlayer : MonoBehaviour
    {
        private ParticleSystem _particles;

        private void Awake() => _particles = GetComponent<ParticleSystem>();

        private void OnEnable()
        {
            // Clear를 먼저 하지 않으면 이전 위치에서 남은 파티클이 새 위치로
            // 순간이동한 것처럼 한 프레임 보인다.
            _particles.Clear();
            _particles.Play();
        }
    }
}
```

- [ ] **Step 2: 컴파일이 통과하는지 확인한다**

기대: 228개 통과, 컴파일 에러 없음.

- [ ] **Step 3: 커밋**

```bash
git add unity/Assets/_Project/Scripts/Core/PooledParticlePlayer.cs unity/Assets/_Project/Scripts/Core/PooledParticlePlayer.cs.meta
git commit -m "feat: PooledParticlePlayer로 풀 재사용 시 파티클 재생 보장"
```

---

## Task 7: 중앙 조정자 — `JuiceDirector`

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/JuiceDirector.cs`

**Interfaces:**
- Consumes:
  - `SushiSurvival.Core.DurationExtension.Extend(float, float) → float` (Task 1)
  - `SushiSurvival.Core.CameraShakeLogic.GetMagnitude(float, float) → float` (Task 2)
  - `SushiSurvival.Core.CameraFollow.SetShakeOffset(Vector2)` (Task 5)
  - `SushiSurvival.Core.GameObjectPool.Get(Vector3, Quaternion) → GameObject` (기존)
- Produces:
  - `SushiSurvival.Core.JuiceDirector.Instance` (static)
  - `SushiSurvival.Core.JuiceDirector.PlayerHit()`
  - `SushiSurvival.Core.JuiceDirector.EnemyDied(Vector3 position)`

- [ ] **Step 1: 구현한다**

`unity/Assets/_Project/Scripts/Core/JuiceDirector.cs`:

```csharp
using System.Collections;
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 히트스톱·화면흔들림·사망 파티클을 한 곳에서 조정한다. 여러 히트가
    /// 같은 프레임에 겹쳐도(계란 양산이 한 번에 여러 마리를 죽이는 경우 등)
    /// DurationExtension으로 합쳐서 한 번의 반응으로 보이게 한다.
    /// </summary>
    public class JuiceDirector : MonoBehaviour
    {
        public static JuiceDirector Instance { get; private set; }

        [Header("참조")]
        [SerializeField] private CameraFollow cameraFollow;
        [Tooltip("사망 파티클 풀.")]
        [SerializeField] private GameObjectPool deathBurstPool;

        [Header("히트스톱")]
        [SerializeField] private float playerHitStopDuration = 0.08f;
        [SerializeField] private float enemyDeathStopDuration = 0.03f;

        [Header("화면 흔들림")]
        [SerializeField] private float playerHitShakeMagnitude = 0.15f;
        [SerializeField] private float playerHitShakeDuration = 0.2f;
        [SerializeField] private float enemyDeathShakeMagnitude = 0.05f;
        [SerializeField] private float enemyDeathShakeDuration = 0.1f;

        private Coroutine _hitstopRoutine;
        private float _hitstopResumeScale = 1f;
        private float _hitstopRemaining;

        private Coroutine _shakeRoutine;
        private float _shakeRemaining;
        private float _shakeMagnitude;

        private void Awake() => Instance = this;

        public void PlayerHit()
        {
            TriggerHitstop(playerHitStopDuration);
            TriggerShake(playerHitShakeMagnitude, playerHitShakeDuration);
        }

        public void EnemyDied(Vector3 position)
        {
            TriggerHitstop(enemyDeathStopDuration);
            TriggerShake(enemyDeathShakeMagnitude, enemyDeathShakeDuration);

            if (deathBurstPool != null)
                deathBurstPool.Get(position, Quaternion.identity);
        }

        private void TriggerHitstop(float duration)
        {
            if (duration <= 0f) return;

            if (_hitstopRoutine == null)
            {
                // 시작 시점의 timeScale을 캡처한다 — 팝업(0)이나 보스 연출(0.3)
                // 중이었다면 그 값으로 복구해야 한다. 1을 하드코딩하면 안 된다.
                _hitstopResumeScale = Time.timeScale;
                _hitstopRemaining = 0f;
                _hitstopRoutine = StartCoroutine(HitstopRoutine());
            }

            _hitstopRemaining = DurationExtension.Extend(_hitstopRemaining, duration);
        }

        private IEnumerator HitstopRoutine()
        {
            Time.timeScale = 0f;

            while (_hitstopRemaining > 0f)
            {
                _hitstopRemaining -= Time.unscaledDeltaTime;
                yield return null;
            }

            Time.timeScale = _hitstopResumeScale;
            _hitstopRoutine = null;
        }

        private void TriggerShake(float magnitude, float duration)
        {
            if (magnitude <= 0f || duration <= 0f) return;

            if (_shakeRoutine == null)
            {
                _shakeRemaining = 0f;
                _shakeMagnitude = 0f;
                _shakeRoutine = StartCoroutine(ShakeRoutine());
            }

            _shakeRemaining = DurationExtension.Extend(_shakeRemaining, duration);
            // 더 큰 진폭이 우선한다 — 늘어난 지속시간에 비해 진폭이 작으면 약해 보인다.
            _shakeMagnitude = Mathf.Max(_shakeMagnitude, magnitude);
        }

        private IEnumerator ShakeRoutine()
        {
            while (_shakeRemaining > 0f)
            {
                _shakeRemaining -= Time.unscaledDeltaTime;

                float magnitude = CameraShakeLogic.GetMagnitude(_shakeRemaining, _shakeMagnitude);
                Vector2 offset = Random.insideUnitCircle * magnitude;

                if (cameraFollow != null)
                    cameraFollow.SetShakeOffset(offset);

                yield return null;
            }

            if (cameraFollow != null)
                cameraFollow.SetShakeOffset(Vector2.zero);

            _shakeRoutine = null;
        }
    }
}
```

- [ ] **Step 2: 컴파일이 통과하는지 확인한다**

기대: 228개 통과, 컴파일 에러 없음.

- [ ] **Step 3: 커밋**

```bash
git add unity/Assets/_Project/Scripts/Core/JuiceDirector.cs unity/Assets/_Project/Scripts/Core/JuiceDirector.cs.meta
git commit -m "feat: JuiceDirector로 히트스톱·흔들림·사망파티클 중앙 조정"
```

---

## Task 8: 적·플레이어를 JuiceDirector와 SpriteFlasher에 연결

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Enemies/EnemyBase.cs`
- Modify: `unity/Assets/_Project/Scripts/Player/PlayerHealth.cs`

**Interfaces:**
- Consumes:
  - `SushiSurvival.Core.SpriteFlasher.Flash(Color, float)` (Task 4)
  - `SushiSurvival.Core.JuiceDirector.Instance` / `.PlayerHit()` / `.EnemyDied(Vector3)` (Task 7)
  - `SushiSurvival.Data.BossData` (기존, `SushiSurvival.Data.MonsterData` 상속)

- [ ] **Step 1: `EnemyBase`에 필드와 피격 플래시를 추가한다**

`monsterData` 필드 선언 아래에 추가:

```csharp
        [SerializeField] private MonsterData monsterData;
        [Tooltip("피격 시 흰색으로 번쩍인다. 비워두면 플래시 없이 조용히 넘어간다.")]
        [SerializeField] private SpriteFlasher spriteFlasher;
```

`TakeDamage` 메서드 안, 넉백 적용 직후에 플래시를 추가한다:

```csharp
        public void TakeDamage(float damage, Vector2 sourcePosition)
        {
            if (HealthLogic.IsDead(CurrentHealth)) return;

            KnockbackVelocity += KnockbackLogic.ComputeImpulse(
                sourcePosition, transform.position, knockbackForce, monsterData.knockbackResistance);

            if (spriteFlasher != null)
                spriteFlasher.Flash(Color.white, 0.08f);

            CurrentHealth = HealthLogic.ApplyDamage(CurrentHealth, damage);

            if (HealthLogic.IsDead(CurrentHealth))
                Die();
        }
```

- [ ] **Step 2: `Die()`에 사망 알림을 추가한다**

`OnDeath?.Invoke(this);` 바로 아래에 추가한다:

```csharp
            OnDeath?.Invoke(this);

            // 보스는 BossDirector.DeathSequence가 이미 자기만의 슬로모션 연출을
            // 한다. 여기서도 히트스톱을 걸면 같은 프레임에 두 코루틴이
            // timeScale을 서로 다른 값으로 건드리다가, 히트스톱이 먼저 끝나며
            // BossDirector가 설정한 슬로모션을 조기에 지워버릴 수 있다.
            if (!(monsterData is BossData) && JuiceDirector.Instance != null)
                JuiceDirector.Instance.EnemyDied(transform.position);
```

- [ ] **Step 3: `PlayerHealth`에 필드와 피격 반응을 추가한다**

`_stats` 필드 선언 아래에 추가:

```csharp
        private PlayerStats _stats;
        [Tooltip("피격 시 빨간색으로 번쩍인다. 비워두면 플래시 없이 조용히 넘어간다.")]
        [SerializeField] private SpriteFlasher spriteFlasher;
        private float _regenCarry;
        private int _revivesUsed;
```

`TakeDamage` 메서드 안, `HealthLogic.ApplyDamage` 호출 직전에 추가한다:

```csharp
        public void TakeDamage(float damage)
        {
            if (HealthLogic.IsDead(CurrentHealth)) return;

            if (spriteFlasher != null)
                spriteFlasher.Flash(Color.red, 0.15f);

            if (JuiceDirector.Instance != null)
                JuiceDirector.Instance.PlayerHit();

            float reduced = ArmorLogic.ApplyArmor(damage, _stats.GetValue(StatType.Armor));
            CurrentHealth = HealthLogic.ApplyDamage(CurrentHealth, reduced);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (!HealthLogic.IsDead(CurrentHealth)) return;

            if (TryRevive()) return;

            OnDeath?.Invoke();
        }
```

`PlayerHealth.cs` 파일 상단의 using에 `SushiSurvival.Core`가 이미 있는지 확인한다
(있다 — `Core` 네임스페이스의 `HealthLogic`/`ArmorLogic`을 이미 쓰고 있어서
`JuiceDirector`와 `SpriteFlasher`도 추가 using 없이 바로 참조된다).

- [ ] **Step 4: 컴파일이 통과하는지 확인한다**

기대: 228개 통과, 컴파일 에러 없음. `TakeDamage(float, Vector2)`와
`TakeDamage(float)`는 각각 `EnemyBase`/`PlayerHealth`의 기존 시그니처 그대로라
호출부(무기, 몹 접촉 데미지, 메테오)는 전혀 안 바뀐다.

- [ ] **Step 5: 커밋**

```bash
git add unity/Assets/_Project/Scripts/Enemies/EnemyBase.cs unity/Assets/_Project/Scripts/Player/PlayerHealth.cs
git commit -m "feat: 피격 플래시와 JuiceDirector 알림을 EnemyBase·PlayerHealth에 연결"
```

---

## Task 9: 보스 페이즈 플래시를 SpriteFlasher로 위임

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Enemies/Boss/BossController.cs`

**Interfaces:**
- Consumes: `SushiSurvival.Core.SpriteFlasher.Flash(Color, float)` (Task 4)

기존 `spriteRenderer`/`_baseColor`/`FlashPhaseChange` 코루틴을 지우고
`SpriteFlasher` 하나로 대체한다. `SpriteFlasher`가 이미 base color 백업과 복구를
전담하므로 `BossController`가 색을 직접 다룰 이유가 없어진다.

- [ ] **Step 1: 필드를 교체한다**

기존:
```csharp
        [SerializeField] private BossData bossData;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private MeteorPattern meteorPattern;
        [SerializeField] private SummonPattern summonPattern;
```

교체 후:
```csharp
        [SerializeField] private BossData bossData;
        [SerializeField] private Animator animator;
        [SerializeField] private SushiSurvival.Core.SpriteFlasher spriteFlasher;
        [SerializeField] private MeteorPattern meteorPattern;
        [SerializeField] private SummonPattern summonPattern;
```

- [ ] **Step 2: `_baseColor` 필드와 `Awake`의 백업 로직을 지운다**

기존:
```csharp
        private EnemyBase _enemy;
        private EnemyAI _ai;
        private Color _baseColor = Color.white;

        private BossPatternType _previousPattern;
        private int _phase = BossPhaseLogic.PhaseOne;
        private float _patternTimer;
        private bool _casting;
        private bool _active;

        private void Awake()
        {
            _enemy = GetComponent<EnemyBase>();
            _ai = GetComponent<EnemyAI>();

            if (spriteRenderer != null)
                _baseColor = spriteRenderer.color;
        }
```

교체 후:
```csharp
        private EnemyBase _enemy;
        private EnemyAI _ai;

        private BossPatternType _previousPattern;
        private int _phase = BossPhaseLogic.PhaseOne;
        private float _patternTimer;
        private bool _casting;
        private bool _active;

        private void Awake()
        {
            _enemy = GetComponent<EnemyBase>();
            _ai = GetComponent<EnemyAI>();
        }
```

- [ ] **Step 3: `FlashPhaseChange`를 지우고 호출부를 한 줄로 바꾼다**

기존:
```csharp
            Debug.Log($"[BossController] 페이즈 {_phase} 전환");
            StartCoroutine(FlashPhaseChange());
        }
```

교체 후:
```csharp
            Debug.Log($"[BossController] 페이즈 {_phase} 전환");

            if (spriteFlasher != null)
                spriteFlasher.Flash(Color.red, phaseFlashDuration);
        }
```

파일 맨 아래의 `FlashPhaseChange` 코루틴 전체를 지운다:

```csharp
        private IEnumerator FlashPhaseChange()
        {
            if (spriteRenderer == null) yield break;

            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(phaseFlashDuration);
            spriteRenderer.color = _baseColor;
        }
```

이 코루틴이 유일하게 `Color`를 참조하던 곳이라, 지운 뒤 `using UnityEngine;`은
`Animator`/`Vector2` 등 다른 타입이 계속 쓰이므로 그대로 둔다.

- [ ] **Step 4: 컴파일이 통과하는지 확인한다**

기대: 228개 통과, 컴파일 에러 없음.

- [ ] **Step 5: 커밋**

```bash
git add unity/Assets/_Project/Scripts/Enemies/Boss/BossController.cs
git commit -m "refactor: BossController 페이즈 플래시를 SpriteFlasher로 위임"
```

---

## Task 10: 체력바 부드럽게 줄이기

**Files:**
- Modify: `unity/Assets/_Project/Scripts/UI/HealthBar.cs`
- Modify: `unity/Assets/_Project/Scripts/UI/BossHealthBar.cs`

**Interfaces:**
- Consumes: `SushiSurvival.UI.HealthBarLogic.MoveTowardsFill(float, float, float) → float` (Task 3)

- [ ] **Step 1: `HealthBar.cs` 전체를 교체한다**

```csharp
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Player;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 캐릭터 발밑에 붙는 체력바. PlayerHealth의 변경 이벤트로 목표값만 받고,
    /// 실제 표시는 매 프레임 그 쪽으로 부드럽게 옮겨간다 — 스냅으로 바뀌면
    /// 얼마나 깎였는지 체감이 안 된다.
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private PlayerHealth playerHealth;
        [Tooltip("Image Type을 Filled로 설정한 채움 이미지.")]
        [SerializeField] private Image fillImage;
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

- [ ] **Step 2: `BossHealthBar.cs` 전체를 교체한다**

```csharp
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Enemies.Boss;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 화면 상단에 고정되는 보스 체력바.
    ///
    /// EnemyBase에는 체력 변경 이벤트가 없어서 매 프레임 폴링한다. 보스는 한
    /// 마리뿐이라 비용이 문제되지 않고, EnemyBase에 이벤트를 추가하면 잡몹
    /// 수십 마리가 전부 그 비용을 내게 된다. 표시값은 목표를 향해 부드럽게
    /// 옮겨가서 스냅으로 깎이지 않는다.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [Tooltip("보이고 숨길 바 컨테이너. 반드시 이 스크립트가 붙은 오브젝트의 " +
                 "자식이어야 한다 — 자기 자신을 넣으면 스스로를 꺼서 다시 켜지 못한다.")]
        [SerializeField] private GameObject bar;
        [Tooltip("Image Type을 Filled로 설정한 채움 이미지.")]
        [SerializeField] private Image fillImage;
        [Tooltip("초당 채움 변화량. 2면 0→1이 0.5초 걸린다.")]
        [SerializeField] private float fillSpeed = 2f;

        private BossController _boss;
        private float _currentFill = 1f;

        private void Awake()
        {
            // 자기 자신을 끄면 Update가 멈춰서 Show()를 받을 수도, 체력을
            // 갱신할 수도 없게 된다. 조립 실수를 조용히 넘기지 않는다.
            if (bar == gameObject)
            {
                Debug.LogError($"{name}: bar에 자기 자신을 연결하면 체력바가 다시 켜지지 않습니다. " +
                               "자식 오브젝트를 연결하세요.");
                bar = null;
            }

            Hide();
        }

        public void Show(BossController boss)
        {
            _boss = boss;
            _currentFill = 1f; // 새 보스는 항상 가득 찬 채로 나타난다.

            if (bar != null)
                bar.SetActive(true);
        }

        public void Hide()
        {
            _boss = null;

            if (bar != null)
                bar.SetActive(false);
        }

        private void Update()
        {
            if (_boss == null || fillImage == null) return;

            float target = HealthBarLogic.ComputeFillAmount(_boss.CurrentHealth, _boss.MaxHealth);
            _currentFill = HealthBarLogic.MoveTowardsFill(_currentFill, target, fillSpeed * Time.deltaTime);
            fillImage.fillAmount = _currentFill;
        }
    }
}
```

- [ ] **Step 3: 컴파일이 통과하는지 확인한다**

기대: 228개 통과, 컴파일 에러 없음.

- [ ] **Step 4: 커밋**

```bash
git add unity/Assets/_Project/Scripts/UI/HealthBar.cs unity/Assets/_Project/Scripts/UI/BossHealthBar.cs
git commit -m "feat: 체력바가 스냅 대신 부드럽게 줄어들도록 변경"
```

---

## Task 11: 레벨업 팝업 스케일인

**Files:**
- Modify: `unity/Assets/_Project/Scripts/UI/LevelUpPanel.cs`

**Interfaces:**
- Consumes: 없음(코루틴만 추가)

`LevelSystem.ShowNext()`가 `panel.Show(...)`를 부르는 바로 그 프레임에
`Time.timeScale = 0f`를 설정한다. 스케일인은 **반드시 실시간**으로 진행해야 한다 —
scaled 시간을 쓰면 애니메이션이 사실상 멈춰서 팝업이 크기 0인 채로 안 보이게 된다.

- [ ] **Step 1: 전체 파일을 교체한다**

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    public class LevelUpPanel : MonoBehaviour
    {
        [Tooltip("팝업 루트. 비워두면 이 오브젝트 자신을 켜고 끈다.")]
        [SerializeField] private GameObject root;
        [Tooltip("선택지 버튼 3개.")]
        [SerializeField] private LevelUpOptionButton[] optionButtons;
        [Tooltip("스케일인에 걸리는 실시간(초). Show() 직후 timeScale이 0이 되므로 " +
                 "반드시 실시간으로 진행한다.")]
        [SerializeField] private float showDuration = 0.15f;

        private GameObject Root => root != null ? root : gameObject;
        private Coroutine _showRoutine;

        private void Awake() => Hide();

        public void Show(IReadOnlyList<IUpgradeOption> options, Action<IUpgradeOption> onChosen)
        {
            Root.SetActive(true);
            Root.transform.localScale = Vector3.zero;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < options.Count)
                    optionButtons[i].Bind(options[i], onChosen);
                else
                    optionButtons[i].Clear();
            }

            if (_showRoutine != null) StopCoroutine(_showRoutine);
            _showRoutine = StartCoroutine(ScaleIn());
        }

        public void Hide() => Root.SetActive(false);

        private IEnumerator ScaleIn()
        {
            Transform t = Root.transform;
            float elapsed = 0f;

            while (elapsed < showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / showDuration));
                t.localScale = Vector3.one * p;
                yield return null;
            }

            t.localScale = Vector3.one;
            _showRoutine = null;
        }
    }
}
```

- [ ] **Step 2: 컴파일이 통과하는지 확인한다**

기대: 228개 통과, 컴파일 에러 없음.

- [ ] **Step 3: 커밋**

```bash
git add unity/Assets/_Project/Scripts/UI/LevelUpPanel.cs
git commit -m "feat: 레벨업 팝업이 스케일인으로 나타나도록 변경"
```

---

## Task 12: Unity Editor 작업 (사용자가 직접)

에이전트는 여기까지 코드를 완성했다. 아래는 GUI 전용 작업이라 사용자가 직접 한다.

### 12-1. 사망 파티클 프리팹

**1.** Hierarchy 빈 공간 우클릭 → **Effects → Particle System** → 이름을
`DeathBurst`로.

**2.** Inspector에서 Particle System 설정:

| 모듈 | 필드 | 값 |
|---|---|---|
| 상단 | Duration | 0.15 |
| 상단 | **Looping** | **해제** |
| 상단 | **Play On Awake** | **해제** (PooledParticlePlayer가 대신 재생) |
| Start Lifetime | | 0.25 |
| Start Speed | | 2~3 |
| Start Size | | 0.08~0.12 |
| Emission → Rate over Time | | 0 |
| Emission → **Bursts** | Time 0, Count 8 | 한 번에 8개가 퍼진다 |
| Shape | | Sphere, Radius 0.05 |

머티리얼·텍스처는 손대지 않는다 — Unity 기본 파티클 머티리얼 그대로 쓴다.

**3.** `DeathBurst`에 **Add Component → Pooled Particle Player**

**4.** **Add Component → One Shot Effect** → **Duration = 0.4**(파티클 수명보다
넉넉하게)

**5.** Project의 `Assets/_Project/Prefabs/`로 드래그 → 프리팹화 → **Hierarchy에서
삭제** (풀이 프리팹에서 계속 찍어내므로 씬에 원본이 남으면 안 된다 — 보스와
반대로, 이건 다른 이펙트 프리팹들과 같은 규칙이다)

**6.** Hierarchy에 기존 풀들 옆에 빈 오브젝트 `DeathBurstPool` 생성 →
**Add Component → Game Object Pool** → Prefab = `DeathBurst`, Prewarm Count = 10

### 12-2. `SpriteFlasher` 부착

아래 프리팹들을 열어서 **SpriteRenderer가 있는 오브젝트**(캐릭터 몸통/보스 본체)에
**Add Component → Sprite Flasher**를 추가한다:

- `EggPlayer`, `ShrimpPlayer`
- `BasicMob`, `CaliforniaRoll`, `MidBos`
- `Boss`

### 12-3. `Sprite Flasher` 필드 연결

| 프리팹/오브젝트 | 컴포넌트 | 필드 | 연결 |
|---|---|---|---|
| `EggPlayer`, `ShrimpPlayer` | `PlayerHealth` | Sprite Flasher | 방금 추가한 것 |
| `BasicMob`, `CaliforniaRoll`, `MidBos`, `Boss` | `EnemyBase` | Sprite Flasher | 방금 추가한 것 |
| `Boss` | `BossController` | Sprite Flasher | 같은 것(재사용) |

`BossController`의 기존 `Sprite Renderer` 필드는 코드에서 지워졌으므로 인스펙터에
더 이상 안 보인다 — 정상이다.

### 12-4. `JuiceDirector` 배치

**1.** Hierarchy 빈 공간에 `JuiceDirector` 생성 → **Add Component → Juice Director**

**2.** 필드 연결:

| 필드 | 연결 대상 |
|---|---|
| Camera Follow | 씬의 `CameraFollow`(메인 카메라) |
| Death Burst Pool | `DeathBurstPool` |

**3.** 나머지 수치는 기본값(0.08 / 0.03 / 0.15 / 0.2 / 0.05 / 0.1) 그대로 두고
플레이테스트하며 조정한다.

### 12-5. 체력바 `Fill Speed` 확인

`HealthBar`(플레이어), `BossHealthBar` 둘 다 새 `Fill Speed` 필드가 생겼다.
기본값 2로 두면 된다.

### 12-6. `LevelUpPanel`의 `Show Duration` 확인

기본값 0.15초로 두면 된다.

---

## Task 13: 플레이테스트

각 항목을 확인하고 어긋나면 어느 항목인지 알린다.

- [ ] **피격 플래시** — 적을 맞히면 흰색으로 짧게 번쩍인다. 계란 양산으로
  여러 마리를 동시에 맞혀도 전부 번쩍인다
- [ ] **플레이어 피격 플래시** — 몹에게 맞으면 캐릭터가 빨갛게 번쩍인다
- [ ] **보스 페이즈 플래시** — 체력 50% 아래로 내려가면 여전히 빨갛게 번쩍인다
  (리팩터 전과 동일하게 동작해야 한다)
- [ ] **히트스톱 — 플레이어 피격** — 맞는 순간 화면이 아주 짧게 멈춘다
- [ ] **히트스톱 — 잔몹 사망** — 몹이 죽는 순간 짧게 멈춘다. 계란으로 여러
  마리를 한 번에 죽여도 한 번의 짧은 정지로 합쳐진다(끊기지 않는다)
- [ ] **히트스톱 — 보스 사망** — 보스가 죽을 때 기존 슬로모션 연출이 그대로
  재생된다(멈췄다가 정상 속도보다 빨리 돌아오지 않는다)
- [ ] **히트스톱 — 레벨업 팝업과 겹침** — 젬을 먹어 레벨업하는 순간과 몹이
  죽는 히트스톱이 겹쳐도, 팝업이 정상적으로 뜨고 팝업이 열린 채 게임이
  재개되는 사고가 없다
- [ ] **화면흔들림** — 플레이어 피격 시 화면이 짧게 흔들리다 부드럽게 잦아든다.
  카메라가 흔들린 뒤에도 플레이어를 계속 잘 따라간다(드리프트 없음)
- [ ] **보스 메테오 착탄** — 메테오에 맞아도 플레이어 피격과 동일하게 정지·흔들림이
  걸린다
- [ ] **사망 파티클** — 잔몹이 죽으면 작은 입자가 흩어진다. 여러 마리가 연속으로
  죽어도 파티클이 계속 잘 나온다(두 번째부터 안 나오면 Play On Awake 문제)
- [ ] **보스 등장 시 필드 정리** — 5:00에 남은 적이 한꺼번에 처치될 때 화면이
  발작하듯 여러 번 흔들리지 않고 한 번의 펄스로 느껴진다
- [ ] **플레이어 체력바** — 데미지를 받으면 순간이동이 아니라 부드럽게 줄어든다
- [ ] **보스 체력바** — 데미지를 받으면 부드럽게 줄어든다
- [ ] **레벨업 팝업 연출** — 팝업이 뜰 때 순간적으로 나타나지 않고 작았다가
  커지며 나타난다
- [ ] **회귀 — 기존 전투** — 무기 공격, 넉백, 경험치 획득, 게임오버, 재시작이
  전부 이전과 동일하게 동작한다
- [ ] **프레임** — 몹 여러 마리가 동시에 죽고 파티클·흔들림이 겹쳐도 눈에 띄게
  느려지지 않는다

플레이테스트 전부 통과하면 `feature/juice-slice` 브랜치를 `main`으로 병합하고
푸시할지 사용자에게 확인한다.

---

## Self-Review 기록

**스펙 커버리지** — 스펙의 모든 항목이 태스크에 대응한다. 히트 플래시(Task 4, 8, 9),
히트스톱·화면흔들림(Task 1, 2, 5, 7), 사망 파티클(Task 6, 7, 12-1), 체력바(Task 3, 10),
레벨업 팝업(Task 11). 보스 제외 로직(Task 8 Step 2), timeScale 캡처(Task 7),
Play On Awake 문제(Task 6) 전부 반영됨.

**타입 일관성** — `DurationExtension.Extend`, `CameraShakeLogic.GetMagnitude`,
`HealthBarLogic.MoveTowardsFill`의 시그니처가 정의(Task 1~3)와 사용처(Task 7, 10)에서
동일하다. `SpriteFlasher.Flash(Color, float)`가 Task 4에서 정의되고 Task 8·9에서
같은 시그니처로 호출된다. `JuiceDirector.EnemyDied(Vector3)` / `PlayerHit()`가
Task 7 정의와 Task 8 호출부에서 일치한다.

**플레이스홀더 스캔** — "TBD", "나중에" 등 없음. 모든 코드 블록이 실제 완성된 내용.

**의도적 순서** — Task 8은 Task 4(`SpriteFlasher`)와 Task 7(`JuiceDirector`) 둘 다
끝나야 컴파일된다 — 두 태스크 뒤에 배치했다. Task 9(`BossController` 리팩터)는
Task 4 이후 아무 때나 가능하지만 Task 8과 묶어 순서를 지켰다.
