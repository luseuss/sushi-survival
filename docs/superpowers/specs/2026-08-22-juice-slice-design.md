# 타격감 슬라이스 설계 — 피격 반응 · 히트스톱 · 화면흔들림 · 사망 파티클 · UI 애니메이션

> 작성 2026-08-22. 슬라이스 3(보스전) 다음 단계. 새 캐릭터나 몬스터가 아니라
> 기존 전투 루프에 반응을 입히는 순수 폴리시 작업이다.

## 목표

로직은 다 도는데 "게임처럼 안 보인다"는 피드백에서 시작했다. 원인은 타격·피격·사망
순간에 아무 반응이 없다는 것 — 체력만 숫자로 줄고 화면은 무반응이다. 새 아트나
사운드 없이, 지금 있는 스프라이트와 코드 패턴만으로 타격감을 만든다.

## 스코프

| 항목 | 범위 |
|---|---|
| 히트 플래시 | 모든 피격(적·플레이어) — 색이 짧게 번쩍 |
| 히트스톱 + 화면흔들림 | **플레이어 피격**(몹 접촉 + 보스 메테오 포함) + **잔몹/중형몹 사망**만. 무기의 일반 타격에는 안 걸린다 |
| 사망 파티클 | 잔몹·중형몹 사망 시 터짐 (보스 제외 — 이미 폭발 이펙트가 있음) |
| 체력바 | 플레이어·보스 둘 다 부드럽게 줄어듦 |
| 레벨업 팝업 | 스케일인으로 나타남 |

**스코프 밖:** 사운드(에셋 준비 필요, 별도), XP 획득 파티클, 피격 시 피 튀김, 데미지
숫자 팝업. 전부 이번 라운드에서 뺐다.

## 왜 "일반 타격"에는 히트스톱·흔들림이 없는가

계란 양산은 한 번 휘두를 때 여러 마리를 동시에 맞히고, 간장새우도 0.65~0.8초마다
쏜다. 타격마다 정지·흔들림을 걸면 초당 여러 번 발동해서 오히려 버벅이는 느낌이
된다. 대신 **사망** 순간에만 걸어서 "몹을 지워버렸다"는 확인 신호로 쓴다. 플레이어
피격은 위험 신호라 항상 걸린다.

## 아키텍처

### 중앙 조정자 — `Core/JuiceDirector.cs`

`GameManager`와 같은 씬 싱글톤 패턴(`Instance`)을 쓴다. 호출부는 이 두 줄이 전부다:

```csharp
JuiceDirector.Instance.PlayerHit();
JuiceDirector.Instance.EnemyDied(transform.position);
```

히트스톱·화면흔들림·사망 파티클 풀을 전부 여기서 소유한다. `EnemyBase`와
`PlayerHealth`가 각자 풀 참조를 들고 다닐 필요가 없다 — 다른 director들(`WaveDirector`,
`BossDirector`)도 자기 관심사의 풀을 직접 소유하는 것과 같은 패턴이다.

`GameManager.Instance`처럼 씬에 없을 수 있는 상황을 가정하고, 호출부는 전부
`if (JuiceDirector.Instance != null)` 가드를 쓴다(기존 `GameManager.Instance` 체크와
동일한 스타일).

### 동시 발동은 합치지 않고 최댓값으로 늘린다

계란 양산 한 번으로 5마리가 동시에 죽을 수 있다. 죽음마다 히트스톱을 새로 걸면
겹쳐서 끊긴다. 그래서 "이미 진행 중이면 남은 시간을 `Max(기존, 신규)`로 늘리기만"
한다 — 여러 마리가 한 프레임에 죽어도 짧은 정지 한 번으로 합쳐진다. 보스 등장 시
`BossDirector.ClearField()`가 필드의 적을 한꺼번에 처치할 때도 같은 원리로 한 번의
작은 펄스가 된다(특별 취급 불필요).

이 "합치지 않고 늘리기" 규칙은 히트스톱과 화면흔들림 둘 다에 쓰이므로 공용 순수
함수로 뺀다:

```csharp
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

### 히트스톱이 기존 `timeScale` 조작과 안 부딪히는 이유

이 프로젝트는 이미 `LevelSystem`(팝업 중 0), `BossDirector`(연출 중 0.3)가 `timeScale`을
건드린다. 히트스톱이 복구값으로 `1`을 하드코딩하면, 레벨업 팝업이 열려 있는 도중
몹이 죽어 히트스톱이 걸렸다가 풀리면서 팝업이 열린 채로 게임이 다시 돌아가는 사고가
난다.

**히트스톱 시작 시점의 `timeScale`을 그대로 캡처했다가 그 값으로 복구한다.** 0이든
0.3이든 1이든 상관없이 원래 있던 값으로 돌아간다.

```csharp
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
```

### 보스는 자기만의 죽음 연출이 있어서 `EnemyDied()`에서 제외한다

보스가 죽으면 `BossDirector.DeathSequence`가 이미 `timeScale = 0.3`으로 슬로모션
연출을 한다. `EnemyBase.Die()`가 보스 죽음에도 반응해서 `JuiceDirector.EnemyDied()`를
부르면, 두 코루틴이 같은 프레임에 `timeScale`을 서로 다른 값으로 건드리다가 히트스톱이
먼저 끝나며 `DeathSequence`가 설정한 0.3을 조기에 지워버릴 수 있다.

`EnemyBase`는 이미 `SushiSurvival.Data`를 참조하고 있으므로 새 필드 없이 타입
검사로 거른다:

```csharp
// Die() 안, OnDeath 호출 근처
if (!(monsterData is BossData) && JuiceDirector.Instance != null)
    JuiceDirector.Instance.EnemyDied(transform.position);
```

### 화면흔들림 — `CameraFollow` 리팩터

지금 `CameraFollow`는 `transform.position`을 직접 목표로 보간한다. 여기에 흔들림
오프셋을 그냥 더하면, 다음 프레임이 흔들린 위치를 기준으로 다시 보간해서 드리프트가
생긴다. 흔들리지 않는 기준 위치를 따로 두고, 매 프레임 그 기준 위치에 오프셋을
더한 값을 최종 위치로 쓴다.

```csharp
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float followSpeed = 5f;

    private Vector3 _basePosition;
    private Vector2 _shakeOffset;

    public void SetTarget(Transform newTarget) => target = newTarget;
    public void SetShakeOffset(Vector2 offset) => _shakeOffset = offset;

    private void Awake() => _basePosition = transform.position;

    private void Start() { /* 기존 그대로 — target 자동 탐색 */ }

    private void LateUpdate()
    {
        if (target == null) return;

        float factor = followSpeed * Time.deltaTime;
        _basePosition = CameraFollowLogic.ComputeFollowPosition(_basePosition, target.position, factor);
        transform.position = _basePosition + (Vector3)_shakeOffset;
    }
}
```

`CameraFollowLogic.ComputeFollowPosition`은 그대로 재사용 — 입력이 "흔들리지 않는
기준 위치"로 바뀌었을 뿐 함수 자체는 순수하고 이미 테스트돼 있다.

흔들림은 **실시간**(`Time.unscaledDeltaTime`)으로 진행한다. 히트스톱으로 화면이
거의 멈춘 순간에도 카메라만 떨리는 "임팩트" 연출이 가능하다.

### 화면흔들림 감쇠 — `Core/CameraShakeLogic.cs`

지속시간이 도중에 늘어날 수 있어서(합치지 않고 최댓값으로 늘리는 규칙) "경과시간
대비 진폭"으로 감쇠 곡선을 그리면 곡선의 기준점이 계속 바뀐다. 대신 **남은 시간이
짧은 구간에서만** 0으로 선형 감쇠한다 — 끝에서 뚝 끊기면 화면이 갑자기 멈춘 것처럼
보인다.

```csharp
namespace SushiSurvival.Core
{
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

## 히트 플래시 — `Core/SpriteFlasher.cs`

보스는 이미 페이즈 전환 때 `spriteRenderer.color`를 직접 만졌다 되돌리는 코드가
있다(`BossController.FlashPhaseChange`). 여기에 "맞을 때 흰색 번쩍"까지 각자
`color`를 건드리면 같은 프레임에 두 코드가 부딪혀 색이 꼬인다. `SpriteFlasher`
하나가 `color`의 유일한 소유자가 된다.

```csharp
namespace SushiSurvival.Core
{
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

            // 실시간으로 진행한다 — 히트스톱과 같은 프레임에 걸리면(맞는 순간이
            // 곧 정지가 걸리는 순간이므로 흔하다) scaled 시간으로는 timeScale이
            // 0인 동안 거의 진행되지 않아, 정지가 풀릴 때까지 계속 하얗게 남는다.
            yield return new WaitForSecondsRealtime(duration);

            _renderer.color = _baseColor;
            _routine = null;
        }
    }
}
```

### 연결 지점 세 곳

| 호출부 | 색 | 지속시간 |
|---|---|---|
| `EnemyBase.TakeDamage` | 흰색 | 0.08초 |
| `PlayerHealth.TakeDamage` | 빨간색 | 0.15초 |
| `BossController.FlashPhaseChange` | 빨간색(기존과 동일) | 0.3초(기존과 동일) |

`EnemyBase`와 `PlayerHealth`에 `[SerializeField] private SpriteFlasher spriteFlasher;`를
추가하고 null 가드 후 `Flash()`를 호출한다. `BossController`는 기존의 `spriteRenderer`
필드와 `_baseColor` 백업 로직, `FlashPhaseChange` 코루틴을 전부 지우고
`spriteFlasher.Flash(Color.red, phaseFlashDuration)` 한 줄로 대체한다 — 중복 로직
제거.

## 사망 파티클 — 풀링된 `ParticleSystem`

커스텀 아트는 필요 없다. Unity가 새 Particle System에 기본으로 붙이는 원형
스프라이트를 그대로 쓴다.

### 풀에서 재사용될 때 "Play On Awake"가 안 먹는 문제

풀링된 오브젝트는 `SetActive(true)`로 재활성화되는데, `Awake()`는 최초 생성 때 한
번만 불린다. `ParticleSystem`의 "Play On Awake"에 의존하면 두 번째 재사용부터
파티클이 안 나올 수 있다. `Core/PooledParticlePlayer.cs`가 `OnEnable`에서 명시적으로
재생한다:

```csharp
namespace SushiSurvival.Core
{
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

반환은 기존 `Core/OneShotEffect.cs`를 그대로 재사용한다 — Duration을 파티클
수명(예: 0.4초)에 맞추면 타이머로 알아서 풀에 돌아간다. 새 반환 로직을 만들
필요가 없다.

## UI 애니메이션

### 체력바 — `HealthBarLogic`에 순수 함수 추가

```csharp
/// <summary>current를 target 쪽으로 maxDelta만큼만 옮긴다. 스냅 대신 부드럽게
/// 줄어드는 체력바에 쓴다.</summary>
public static float MoveTowardsFill(float current, float target, float maxDelta)
{
    return Mathf.MoveTowards(Mathf.Clamp01(current), Mathf.Clamp01(target), Mathf.Max(0f, maxDelta));
}
```

`HealthBar`(플레이어)와 `BossHealthBar` 둘 다 같은 방식으로 고친다 — 이벤트나
폴링으로 받은 값을 곧장 `fillImage.fillAmount`에 넣지 않고 `_targetFill`에 저장한
뒤, `Update()`에서 `_currentFill`을 그 쪽으로 매 프레임 조금씩 옮긴다.

```csharp
[SerializeField] private float fillSpeed = 2f; // 초당 채움 변화량. 2면 0→1이 0.5초.
private float _currentFill;
private float _targetFill;

private void Update()
{
    _currentFill = HealthBarLogic.MoveTowardsFill(_currentFill, _targetFill, fillSpeed * Time.deltaTime);
    fillImage.fillAmount = _currentFill;
}
```

`HealthBar`는 `OnHealthChanged` 콜백에서 `_targetFill`만 갱신하도록 바꾸고,
`BossHealthBar`는 기존 폴링 로직에서 `fillImage.fillAmount` 직접 대입을
`_targetFill` 갱신으로 바꾼다.

### 레벨업 팝업 — 스케일인

`LevelSystem`이 `panel.Show(...)`를 부르는 바로 그 프레임에 `Time.timeScale = 0f`를
설정한다. 그래서 스케일인 애니메이션은 **반드시 실시간**으로 진행해야 한다 — scaled
시간을 쓰면 timeScale이 0인 동안 애니메이션이 사실상 멈춰서, 팝업이 크기 0인 채로
안 보이는 상태가 이어진다.

```csharp
[Tooltip("스케일인에 걸리는 실시간(초).")]
[SerializeField] private float showDuration = 0.15f;

private Coroutine _showRoutine;

public void Show(IReadOnlyList<IUpgradeOption> options, Action<IUpgradeOption> onChosen)
{
    Root.SetActive(true);
    Root.transform.localScale = Vector3.zero;

    for (int i = 0; i < optionButtons.Length; i++)
    {
        if (i < options.Count) optionButtons[i].Bind(options[i], onChosen);
        else optionButtons[i].Clear();
    }

    if (_showRoutine != null) StopCoroutine(_showRoutine);
    _showRoutine = StartCoroutine(ScaleIn());
}

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
```

`Hide()`는 그대로 `Root.SetActive(false)`만 하면 된다 — 다음 `Show()`가
`localScale = Vector3.zero`로 다시 초기화하므로, 애니메이션이 중간에 잘렸어도
다음 번엔 항상 작은 크기에서 시작한다.

## 파일 목록

**신규**

| 파일 | 책임 | 순수 로직 |
|---|---|---|
| `Core/JuiceDirector.cs` | 히트스톱·흔들림·사망파티클 중앙 조정 | |
| `Core/DurationExtension.cs` | 남은 시간을 합치지 않고 최댓값으로 늘림 | ✅ |
| `Core/CameraShakeLogic.cs` | 남은 시간 기준 흔들림 진폭 감쇠 | ✅ |
| `Core/SpriteFlasher.cs` | 색 플래시의 유일한 소유자 | |
| `Core/PooledParticlePlayer.cs` | 풀 재사용 시 파티클 재생 보장 | |

**수정**

| 파일 | 변경 |
|---|---|
| `Core/CameraFollow.cs` | 흔들리지 않는 기준 위치 + `SetShakeOffset` |
| `Enemies/EnemyBase.cs` | 피격 시 흰색 플래시, 사망 시(보스 제외) `JuiceDirector.EnemyDied()` |
| `Player/PlayerHealth.cs` | 피격 시 빨간 플래시 + `JuiceDirector.PlayerHit()` |
| `Enemies/Boss/BossController.cs` | 페이즈 플래시를 `SpriteFlasher`로 위임(중복 로직 제거) |
| `UI/HealthBarLogic.cs` | `MoveTowardsFill` 추가 |
| `UI/HealthBar.cs` | 스냅 대신 부드러운 보간 |
| `UI/BossHealthBar.cs` | 스냅 대신 부드러운 보간 |
| `UI/LevelUpPanel.cs` | 스케일인 연출 |

**테스트**

| 파일 | 대상 |
|---|---|
| `Tests/EditMode/DurationExtensionTests.cs` | 신규 |
| `Tests/EditMode/CameraShakeLogicTests.cs` | 신규 |
| `Tests/EditMode/HealthBarLogicTests.cs` | 기존 파일에 `MoveTowardsFill` 케이스 추가 |

`JuiceDirector`·`SpriteFlasher`·`PooledParticlePlayer`·`CameraFollow`는 코루틴과
Unity 라이프사이클에 묶여 있어 플레이테스트로 확인한다.

## 에디터 작업 (사용자가 직접)

1. **`JuiceDirector`** — 씬에 빈 오브젝트로 배치, `Camera Follow`·`Death Burst Pool`
   연결
2. **사망 파티클 프리팹** — 빈 오브젝트에 `Particle System`(기본 원형 스프라이트,
   Duration 0.15초, Start Lifetime 0.2~0.3초 정도, Play On Awake 끔 — `PooledParticlePlayer`가
   대신 재생) + `PooledParticlePlayer` + `OneShotEffect`(Duration 0.4초) 추가 →
   프리팹화 → 전용 풀 오브젝트(`DeathBurstPool`, Prewarm 10)에 연결
3. **`SpriteFlasher`** — 플레이어 2종 프리팹, 잔몹·중형몹·보스 프리팹 전부에 추가
   (SpriteRenderer가 이미 있는 오브젝트에 붙이면 된다)
4. **`EnemyBase`/`PlayerHealth`의 `Sprite Flasher` 필드** — 방금 추가한 컴포넌트 연결
5. **`BossController`의 `Sprite Flasher` 필드** — 연결, 기존 `spriteRenderer` 필드는
   코드에서 제거되므로 인스펙터에도 안 보이게 됨
6. **`HealthBar`/`BossHealthBar`의 `Fill Speed`** — 기본값 2로 두고 플레이테스트하며
   조정
7. **`LevelUpPanel`의 `Show Duration`** — 기본값 0.15초

## 테스트 계획

| 대상 | 검증 내용 |
|---|---|
| `DurationExtension.Extend` | 더 긴 값으로 늘어남, 더 짧은 요청은 무시됨, 0/음수 안전 |
| `CameraShakeLogic.GetMagnitude` | remaining이 tail 이상이면 최대치, tail 안에서 선형 감쇠, 0 이하는 0 |
| `HealthBarLogic.MoveTowardsFill` | 목표를 향해 부분 이동, 초과분 클램프, 0/1 경계, 음수 maxDelta 안전 |

기존 테스트 212개가 전부 통과 상태를 유지해야 한다.

## 스코프 밖 확인

- 사운드 — 에셋 준비가 필요해 별도 라운드
- XP 젬 획득 파티클, 피격 시 피 튀김, 데미지 숫자 팝업 — 이번엔 뺌
- 히트스톱·흔들림 수치(0.08초, 0.15 유닛 등)는 전부 제안값 — 플레이테스트로 조정
