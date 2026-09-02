# 인게임 HUD 설계

> 작성 2026-09-03. `UI_SPEC.md`(사용자 작성) 2장 기준. 문서 전체(메인 메뉴·HUD·
> 레벨업·호감도 대화·보스 연출·결과 화면·일시정지)를 한 번에 다루지 않고
> 인게임 HUD만 먼저 뗐다 — 나머지는 각자 별도 슬라이스.

## 목표

`UI_SPEC.md` 2장의 배치를 구현한다: 최상단 XP 게이지, 상단 중앙 생존 타이머
(보스 등장 시 아래로 밀림), 상단 우측 처치 수, 하단 좌측 캐릭터 초상화+체력바.
보스 체력바(이미 있음)는 색만 바꾼다. **무기 슬롯은 이번 스코프에서 뺀다** —
지금 캐릭터마다 무기가 하나뿐이라 슬롯 개념 자체가 아직 없다.

## 문서와 기존 구현 대조 — 확인된 사항

- `UI_SPEC.md`의 레벨업 팝업 하단 `[리롤]` 버튼은 이미 구현된 "왕의 와사비
  하사"(PR #6) 자리를 가리킨 것으로 확인됨 — 충돌 아님
- "재화 카운트"는 이번엔 안 만든다 — 이 프로젝트에 화폐/재화 개념이 없다
  (세이브 없음, 젬은 즉시 소모되는 경험치일 뿐)
- 지금 플레이어 체력바는 **캐릭터 프리팹 안에 박힌 월드스페이스 바**
  ("캐릭터 발밑에 붙는")다. HUD 코너로 완전히 이전하고 기존 것은 제거한다

## 데이터 노출 — `LevelCurve`/`LevelSystem`

`LevelCurve.GetRequiredXp`가 이미 순수 함수로 있어서 비율 계산만 추가한다.

```csharp
// LevelCurve.cs에 추가
/// <summary>다음 레벨까지의 진행률(0~1). XP 게이지에 쓴다.</summary>
public static float GetProgressRatio(float xpTowardNext, int level, float baseXp, float increment)
{
    float required = GetRequiredXp(level, baseXp, increment);
    return required <= 0f ? 0f : Mathf.Clamp01(xpTowardNext / required);
}
```

`LevelSystem`에 노출:

```csharp
public float ProgressRatio => LevelCurve.GetProgressRatio(_xpTowardNext, CurrentLevel, baseXp, xpIncrementPerLevel);
```

## 새 UI — XP 게이지 · 처치 수

둘 다 매 프레임 값을 읽어 표시하는 폴링 컴포넌트다 — `BossHealthBar`가 이미
같은 방식(이벤트 없이 `Update()`에서 직접 읽음)을 쓰고 있어서 그대로 따른다.

### `XpGaugeDisplay`

`LevelSystem`은 싱글톤이 아니라서(씬에 하나뿐인 고정 오브젝트) 인스펙터로
직접 참조한다 — `GameManager.Instance` 같은 정적 접근이 필요 없다.

```csharp
namespace SushiSurvival.UI
{
    /// <summary>화면 최상단 풀와이드 XP 게이지. 레벨업 팝업이 열려 스탯이
    /// 재계산되는 동안에도 부드럽게 채워지도록 스무딩한다.</summary>
    public class XpGaugeDisplay : MonoBehaviour
    {
        [SerializeField] private Core.LevelSystem levelSystem;
        [Tooltip("Image Type을 Filled로 설정한 채움 이미지.")]
        [SerializeField] private UnityEngine.UI.Image fillImage;
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

`HealthBarLogic.MoveTowardsFill`은 이름과 달리 범용 0~1 보간 함수라 그대로
재사용한다 — 이 프로젝트가 이미 여러 번 이렇게 써왔다(호감도 대화 버프 등).

### `KillCountDisplay`

`GameManager`는 싱글톤이라 `Instance`로 접근한다. `RunTimerDisplay`와 같은
패턴 — `RunState.Playing`이 아니면 숨긴다.

```csharp
namespace SushiSurvival.UI
{
    public class KillCountDisplay : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Text countText;

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

## 체력바 이전 — 런타임 연결이 필요한 이유

지금 체력바는 캐릭터 프리팹 안에 있어서 인스펙터로 `PlayerHealth`를 미리
연결해둘 수 있었다. HUD 코너로 옮기면 캐릭터 종류와 무관한 **씬 오브젝트**가
되므로, 플레이어가 스폰되는 런타임에 연결해야 한다 —
`CameraFollow.SetTarget(...)`이 이미 쓰는 것과 같은 패턴이다.

```csharp
// HealthBar.cs — 필드
[SerializeField] private UnityEngine.UI.Image portraitImage;

// HealthBar.cs — 신규 메서드
/// <summary>런타임에 플레이어가 스폰된 뒤 GameManager가 호출한다. 이전 대상이
/// 있으면(재시작 등) 먼저 구독을 해제해 중복 구독을 막는다.</summary>
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
```

기존 `OnEnable`/`OnDisable`의 구독 로직은 인스펙터에 미리 연결된 경우를 위해
그대로 둔다 — `SetTarget`이 덮어쓰면서 중복 구독되지 않도록 먼저 해제부터
한다.

**캐릭터 프리팹(`EggPlayer`, `ShrimpPlayer`) 안의 기존 월드스페이스
`HealthBarCanvas`는 삭제한다.**

## `GameManager` 연결

```csharp
[SerializeField] private SushiSurvival.UI.HealthBar hudHealthBar;

// StartRun() 안, cameraFollow.SetTarget(_playerTransform); 다음 줄에 추가
if (hudHealthBar != null)
    hudHealthBar.SetTarget(_playerHealth, characterData.portraitSprite);
```

## 타이머 — 보스 등장 시 아래로 밀림

새 이벤트 연결 없이, `RunTimerDisplay`가 이미 읽고 있는
`GameManager.ElapsedTime`/`BossSpawnTime`으로 스스로 판단한다.

별도 `RectTransform` 필드를 새로 만들지 않는다 — `timerText.rectTransform`이
이미 있다.

```csharp
[SerializeField] private float normalY = 12f;
[SerializeField] private float bossPhaseY = 76f;
[SerializeField] private float moveDuration = 0.3f;

private bool _bossPhaseActive;
private Coroutine _moveRoutine;

// Update() 안, 기존 텍스트 갱신 로직 다음에 추가
bool bossPhase = manager.ElapsedTime >= manager.BossSpawnTime;
if (bossPhase != _bossPhaseActive)
{
    _bossPhaseActive = bossPhase;
    if (_moveRoutine != null) StopCoroutine(_moveRoutine);
    _moveRoutine = StartCoroutine(MoveTo(bossPhase ? bossPhaseY : normalY));
}
```

```csharp
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
```

## 보스 체력바 — 코드 변경 없음

이미 있는 `BossHealthBar`는 `Fill`/`Background` Image의 색상 값만
`UI_SPEC.md`의 블루 팔레트(`#378ADD` 채움 등)로 바꾼다. 순수 에디터 작업.

## 파일 목록

**신규**

| 파일 | 책임 |
|---|---|
| `UI/XpGaugeDisplay.cs` | 최상단 XP 게이지 |
| `UI/KillCountDisplay.cs` | 상단 우측 처치 수 |

**수정**

| 파일 | 변경 |
|---|---|
| `Core/LevelCurve.cs` | `GetProgressRatio` 추가 |
| `Core/LevelSystem.cs` | `ProgressRatio` 프로퍼티 노출 |
| `UI/HealthBar.cs` | `SetTarget(PlayerHealth, Sprite)` + `portraitImage` 필드 |
| `UI/RunTimerDisplay.cs` | 보스 등장 시 위치 트윈 |
| `Core/GameManager.cs` | `hudHealthBar` 필드 + 스폰 후 `SetTarget` 호출 |

**변경 없음(에디터 작업만)**

- `UI/BossHealthBar.cs` — 색상만 교체
- 캐릭터 프리팹 2종 — 기존 월드스페이스 체력바 제거

## 스코프 밖

- 무기 슬롯 (지금 캐릭터당 무기 1개뿐이라 슬롯 시스템 자체가 없음)
- 재화/화폐 카운트 (이 프로젝트에 없는 개념)
- 메인 메뉴, 레벨업 팝업 스타일링, 호감도 대화 정밀 스타일링, 보스 등장
  포스트프로세싱 연출, 결과 화면 등급 시스템, 일시정지 메뉴 — `UI_SPEC.md`의
  나머지 장들. 각각 별도 슬라이스로 진행

## 테스트 계획

`LevelCurve.GetProgressRatio`만 순수 함수라 EditMode로 검증한다: 정상 진행률
계산, `required`가 0일 때 0 반환(0으로 나누기 방지), 경계값(진행률 0/1).

나머지(폴링 UI, 런타임 `SetTarget` 연결, 타이머 위치 트윈)는 MonoBehaviour와
Unity 생명주기에 묶여 있어 플레이테스트로 확인한다.

## 에디터 작업 (사용자가 직접)

1. `GameScene.unity`의 `Canvas`에 `XpGaugeDisplay`, `KillCountDisplay`, HUD용
   `HealthBar`(원형 초상화 + 체력바) UI 조립·배치
2. `EggPlayer`/`ShrimpPlayer` 프리팹에서 기존 `HealthBarCanvas`(월드스페이스
   체력바) 제거
3. `GameManager`의 `Hud Health Bar` 필드에 1번에서 만든 오브젝트 연결
4. `RunTimerDisplay`의 `Timer Rect`/`Normal Y`/`Boss Phase Y` 필드 연결·확인
5. `BossHealthBar`의 `Fill`/`Background` Image 색상을 블루 팔레트로 교체
