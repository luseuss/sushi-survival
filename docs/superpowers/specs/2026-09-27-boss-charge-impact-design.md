# 보스 돌진(대쉬) 임팩트 연출 설계

> 원 기획: `스시왕국_기획정리_2026-09-26.md`(사용자 로컬 문서) 3-2절. 3-1(소환 6단계)은 이미 완료·병합됨.
> 기존 예고 → 돌진 → 정지(반격 기회) 구조 위에 **연출만** 얹는다.

## 배경

`BossController.ChargeRoutine`은 ① 붉게 번쩍이며 예고(`chargeWindup`) → ② 예고가 끝나는 순간의
플레이어 방향으로 고정해 돌진(`chargeDuration`, `chargeSpeedScale`) → ③ 멈춰서 무방비(`chargeRecovery`)
순서다. 피해는 보스의 접촉 데미지가 그대로 준다. 지금은 붉은 번쩍임 외에 돌진 경로·속도감·정지 순간을
알려 주는 연출이 없다.

## 확정된 결정

| 항목 | 결정 |
|---|---|
| 아트·사운드 | **새 아트·새 프리팹 없음.** 전부 런타임 생성(메테오 예고 마커 `CircleTextureFactory`의 선례). 사운드 시스템은 프로젝트에 아직 없어 범위 밖 |
| 예고선 동작 | 예고 동안 플레이어를 **실시간으로 따라가다 마지막에 고정** |
| 충격파 | **피해 없는 순수 시각 효과** |
| "충돌 시" 진동 | 새로 만들지 않는다 — 플레이어가 맞으면 `JuiceDirector.PlayerHit()`이 이미 히트스톱·흔들림을 준다. 새 진동은 "돌진이 멈추는 순간" 하나 |
| 히트스톱 | 넣지 않는다(반격 기회 구간에서 시간이 멈추면 어색함) |

## 게임플레이 변화 (하나)

예고선이 정직하려면 실제 방향 확정 시점이 시각적 고정 시점과 같아야 한다. 그래서 방향 확정이 예고
종료 **`chargeLockSeconds`(기본 0.15초) 전**으로 당겨진다. `chargeLockSeconds = 0`이거나
`BossChargeEffects`가 붙어 있지 않으면 **기존과 완전히 같다**(예고 종료 순간에 확정).

## 1. 구성

| 파일 | 역할 |
|---|---|
| `Enemies/Boss/ChargeEffectsLogic.cs` (신규, 순수 함수) | 돌진 거리, 방향 확정 지연, 페이드 알파, 충격파 반경 |
| `Core/FadingSpriteEffect.cs` (신규) | 스프라이트를 놓고 커지며 사라지는 풀링 효과. 잔상·먼지·충격파가 **공통으로** 쓴다 |
| `Core/JuiceDirector.cs` (수정) | private `TriggerShake`를 감싸는 공개 `Shake(float magnitude, float duration)` 추가 |
| `Enemies/Boss/BossChargeEffects.cs` (신규 MonoBehaviour) | 예고선·잔상·먼지·충격파 총괄 |
| `Enemies/Boss/BossController.cs` (수정) | `ChargeRoutine` 각 단계에서 효과 호출. 판정·이동 로직은 그대로 |
| `Boss.prefab` (사용자 에디터 작업) | 루트에 `Boss Charge Effects` 컴포넌트 추가 |

`BossController`는 `Awake`에서 `GetComponent<BossChargeEffects>()`로 효과를 찾는다(직렬화 필드 연결 없음).
없으면(`null`) 연출 호출을 전부 건너뛰고 돌진은 지금처럼 동작한다.

## 2. 순수 로직 — `ChargeEffectsLogic`

```csharp
public static float ChargeDistance(float moveSpeed, float speedScale, float duration)  // 이동속도 × 배율 × 시간
public static float LockDelay(float windup, float lockSeconds)                          // 방향을 확정하는 시점 = windup − lockSeconds, 0~windup으로 클램프
public static float FadeAlpha(float age, float lifetime, float startAlpha)              // startAlpha에서 0으로 선형 감소, lifetime 이상이면 0
public static float Progress(float age, float lifetime)                                 // 0~1 클램프, lifetime <= 0이면 1
```
돌진 거리는 1페이즈 `1.6 × 4.5 × 0.55 ≈ 3.96`, 2페이즈 `1.6 × 5.5 × 0.6 ≈ 5.28`. 예고선 길이가 이 값이다.
(실제 이동은 `BlockAgainstPlayer`로 플레이어에게 막힐 수 있어 선은 "최대 도달 거리"다.)

## 3. `FadingSpriteEffect`

`SpriteRenderer` 하나를 가진 풀링 오브젝트. `Play(sprite, position, rotation, color, startScale, endScale, lifetime, sortingOrder, flipX)`로
켜지면 매 프레임 `ChargeEffectsLogic.Progress`로 스케일을 보간하고 `FadeAlpha`로 투명도를 줄이다가
수명이 끝나면 스스로 풀에 반환한다(풀 반환 경로부터 확인하라는 기존 교훈). `Time.deltaTime`을 써서
`timeScale` 0이면 함께 멈춘다. 효과 오브젝트는 씬 루트의 `ChargeEffectsRoot` 아래에 만들어 보스를 따라
움직이지 않는다. 풀은 `BossChargeEffects`가 `ObjectPool<FadingSpriteEffect>`로 들고 있다.

## 4. `BossChargeEffects`

`Awake`에서 스프라이트를 한 번만 만들어 둔다(효과마다 텍스처를 새로 할당하지 않는다): 1×1 흰 픽셀(예고선),
`CircleTextureFactory.CreateSprite(16, 0f, …)` 원판(먼지), `CreateSprite(64, 0.85f, …)` 링(충격파).
보스의 `SpriteRenderer`는 `GetComponent`로 찾는다(잔상 복제용).

`BossController`가 부르는 진입점:

| 메서드 | 하는 일 |
|---|---|
| `BeginTelegraph(float length)` | 예고선을 켠다 |
| `AimTelegraph(Vector2 from, Vector2 toward)` | 선의 시작점·방향·길이를 갱신(예고 동안 매 프레임) |
| `LockTelegraph()` | 선을 굵고 밝게 바꾸고 갱신을 멈춘다 |
| `BeginTrail()` / `EndTrail()` | 돌진 중 잔상(0.05초마다)·먼지(0.04초마다) 생성 코루틴 시작/정지. `EndTrail`이 예고선도 끈다 |
| `PlayImpact(Vector2 position)` | 충격파 링 + `JuiceDirector.Instance.Shake(...)` |

`chargeLockSeconds`(기본 0.15)도 이 컴포넌트의 인스펙터 값이다(`BossData.asset`을 안 건드리려고).

**시작값(전부 인스펙터):**

| 항목 | 값 |
|---|---|
| 예고선 너비 / 색 | 0.12 유닛 / 붉은색 알파 0.35. 고정되면 너비 ×1.5, 알파 0.8 |
| 잔상 | 간격 0.05초, 붉은 톤 알파 0.5, 수명 0.25초 |
| 먼지 | 간격 0.04초, 모래색 알파 0.6, 스케일 0.15→0.4, 수명 0.35초, 발치에서 좌우로 조금 흩뿌림 |
| 충격파 | 링 반지름 0.3→2.2, 수명 0.35초, 알파 0.8 |
| 화면 흔들림 | 세기 0.25, 0.25초(플레이어 피격 0.15/0.2보다 세고 보스 착지 0.5/0.6보다 약함) |

정렬 순서는 모두 보스 `SpriteRenderer`보다 한 칸 아래(잔상·먼지·충격파·예고선이 보스를 가리지 않게).

## 5. `BossController.ChargeRoutine` 변경

```
① 예고:   effects.BeginTelegraph(길이)  ·  spriteFlasher.Flash(붉게, chargeWindup)  (기존)
          LockDelay 시점까지 매 프레임 effects.AimTelegraph(보스 위치, 플레이어 위치)
② 확정:   그 시점의 플레이어 방향을 캡처(기존 BossAimLogic.ChargeDirection)  ·  effects.LockTelegraph()
          남은 (windup − LockDelay) 동안 대기
③ 돌진:   _ai.LockedDirection/MoveScale 설정(기존)  ·  effects.BeginTrail()  ·  chargeDuration 대기
④ 정지:   _ai 정지(기존)  ·  effects.EndTrail()  ·  effects.PlayImpact(보스 위치)
⑤ 반격:   chargeRecovery 대기(기존)
```
`effects`가 `null`이면 ①~④의 효과 호출을 건너뛰고, 방향 확정은 예고 종료 시점(기존)에 한다.

## 6. 테스트

- `ChargeEffectsLogicTests`(신규): `ChargeDistance`(두 페이즈 값), `LockDelay`(정상·0·windup보다 큰 lock·음수), `FadeAlpha`(시작·중간·끝·수명 0), `Progress`(경계·수명 0).
- `FadingSpriteEffect`·`BossChargeEffects`·`BossController`·`JuiceDirector.Shake`는 MonoBehaviour라 컴파일+회귀 확인만.
- 실제 모양·타이밍은 Play 모드로 확인한다(사용자).

## 스코프 밖

- 돌진 사운드 / 사운드 시스템
- 충격파 피해, 히트스톱
- 다른 패턴(메테오·소환) 연출
- 새 아트로의 교체(생기면 `FadingSpriteEffect`에 넣는 스프라이트만 바꾸면 됨)
