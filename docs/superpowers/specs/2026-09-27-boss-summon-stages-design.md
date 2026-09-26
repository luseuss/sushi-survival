# 보스 소환 6단계(체력 임계 연동) 설계

> 원 기획: `스시왕국_기획정리_2026-09-26.md`(사용자 로컬 문서) 3-1절.
> 3-2(돌진 임팩트 연출)는 이 소환 작업과 파일이 거의 안 겹쳐서 **별도 스펙·계획**으로 진행한다.

## 배경

지금 보스는 메테오·소환·돌진 3패턴을 가중치 무작위로 뽑는다(직전과 같은 패턴은 다시 안 뽑음).
소환은 `summonMobPool` 하나(BasicMob 프리팹 하나)에서 페이즈별로 4~6마리를 뽑는다.
기획은 소환을 "보스 체력이 임계에 닿는 즉시 6단계로 나오는 사건"으로 바꾼다.

## 확정된 결정

| 항목 | 결정 |
|---|---|
| 기존 무작위 소환 패턴 | **삭제.** 소환은 체력 임계 6회로만 나온다 |
| 메테오·돌진 순서 | 가중치 무작위, **같은 패턴 최대 2연속까지 허용**(1페이즈 메테오 위주, 2페이즈 돌진 위주) |
| 임계 도달 시점이 시전 중일 때 | 그 시전이 끝난 뒤에 소환(`_casting` 중엔 `Update`가 멈추므로 자연스럽게 성립) |
| 한 번에 여러 임계를 넘을 때 | 단계를 하나도 건너뛰지 않고 순서대로 이어서 시전 |
| 2페이즈 전환(`phaseTwoThreshold` 0.5) | 소환 임계와 **독립**. 3단계(55%)와 4단계(40%) 사이에서 시작 |
| 소환 임계·마릿수 | 인스펙터 데이터(기획서 표 + 85/70/55/40/25/10%가 시작값, 플레이로 조정) |

## 소환 6단계 (기획서 확정 표)

| 단계 | 임계(시작값) | 일반 | 캘리 | 중형몹 | 합계 |
|---|---|---|---|---|---|
| 1 | 85% | 4 | 0 | 0 | 4 |
| 2 | 70% | 0 | 4 | 0 | 4 |
| 3 | 55% | 3 | 3 | 0 | 6 |
| 4 | 40% | 0 | 6 | 0 | 6 |
| 5 | 25% | 8 | 8 | 0 | 16 |
| 6 | 10% | 0 | 0 | 2 | 2 |

몹은 기존 BasicMob·CaliforniaRoll·MidBos 프리팹을 그대로 쓴다(새 아트 없음).

## 1. 데이터

`Data/BossData.cs`:
- 새 `[Serializable] struct BossSummonStage { float healthThreshold; int basicCount; int californiaCount; int midCount; }`
- `BossData.summonStages`(`BossSummonStage[]`) 추가. 임계는 내림차순(첫 단계가 가장 높은 체력)이어야 한다.
- `BossPhaseValues.summonCount` 제거(소환 마릿수는 이제 단계 데이터가 정한다). `summonRadius`는 그대로 쓴다.

`BossData.asset`은 기존 손 입력 데이터가 있는 에셋이라, 새 배열만 **덧붙이는** 스크립트로 채운다
(기존 필드는 안 건드림, `summonCount` 두 줄 제거).

## 2. 판정 로직 (순수 함수, EditMode 테스트)

`Enemies/Boss/BossSummonLogic.cs`:

```csharp
public static int CrossedStageCount(float currentHealth, float maxHealth,
                                    IReadOnlyList<float> thresholds, int nextStage)
```
`nextStage`부터 시작해 `현재체력/최대체력 <= thresholds[k]`인 단계를 연속으로 세어 개수를 돌려준다
("닿는 즉시"라서 이하 비교). `maxHealth <= 0`이거나 `nextStage`가 배열 밖이면 0.

## 3. 패턴 스케줄러

`BossPatternType`에서 `Summon`을 뺀다(메테오·돌진만). `BossPatternScheduler.SelectNext`는
연속 횟수를 받는다:

```csharp
public static BossPatternType SelectNext(BossPatternType previous, int consecutiveCount, int phase, float roll)
```
`consecutiveCount >= 2`이면 `previous`를 후보에서 제외하고, 그 미만이면 포함한다.
가중치는 기존 값 그대로: 1페이즈 `{메테오 5, 돌진 2}`, 2페이즈 `{메테오 3, 돌진 5}`.

## 4. `BossController`

- `_nextSummonStage`, `_pendingSummons` 추가. `Activate`에서 0으로 초기화.
- `Update()`: `UpdatePhase()` 다음에 `BossSummonLogic.CrossedStageCount(...)`로 새로 넘은 단계 수를
  `_pendingSummons`에 더하고 `_nextSummonStage`를 그만큼 올린다.
- 대기 중인 소환이 있으면 패턴 타이머보다 먼저 `CastSummonStage(단계 인덱스)`를 시작한다.
- `CastSummonStage`: 기존 소환 시전과 같은 모양(이동 정지 → `CastSummon` 트리거 → `castDuration` 대기 →
  `summonPattern.Fire(stage, radius)` → 이동 복구). **`_previousPattern`·연속 횟수·패턴 타이머는 건드리지
  않는다**(소환은 무작위 패턴 순서의 일부가 아니다).
- `Cast(pattern)`은 메테오·돌진만 처리한다. 연속 횟수는 `pattern == _previousPattern`이면 +1, 아니면 1로 센다.
  첫 패턴은 지금처럼 항상 메테오다.
- `Activate`의 시그니처: 풀 인자가 `(meteorPool, mobPool, effectPool, gemPools)` → `(meteorPool, basicMobPool,
  californiaMobPool, midMobPool, effectPool, gemPools)`로 늘어난다.

## 5. `SummonPattern`

`Fire(BossPhaseValues)` → `Fire(BossSummonStage stage, float radius)`. 단계의 (일반×n, 캘리×m, 중형몹×k)로
풀 참조 목록을 만들고 섞은 뒤(같은 종류가 한쪽에 몰리지 않게), 기존 `SummonPlacement.GetPositions`로
플레이어를 둘러싼 링 위 위치를 잡아 하나씩 `SummonAt(position, pool)`한다. 풀이 비어 있는 종류는
`LogError`를 한 번 남기고 그 몹만 건너뛴다. 등장 이펙트·젬 풀 주입은 기존 그대로.

## 6. 두 디렉터

`BossFightDirector`(BossScene)와 `BossDirector`(예전 GameScene용) **둘 다** `boss.Activate(...)`를 부르므로
둘 다 고친다. 기존 `summonMobPool` 필드는 **이름을 유지한다**(BasicMob 풀 참조가 씬에서 끊기지 않게).
새 필드 `summonCaliforniaPool`, `summonMidMobPool`을 추가한다.

## 7. BossScene 배선 (사용자 에디터 작업)

- `CaliforniaRoll` 풀 오브젝트와 `MidBos` 풀 오브젝트를 새로 만든다(풀 하나당 GameObject 하나 — 기존 함정).
- `BossFightDirector`의 `Summon California Pool` / `Summon Mid Mob Pool`에 연결.
- 세 풀의 prewarm을 16 이상으로(5단계 16마리 동시 소환 대비). 풀이 비었을 때 새로 만드는 동작은 기존 그대로다.
- `BossScene.unity`는 협업자가 만든 씬이라 PR 본문에 명시한다.

## 8. 테스트

- `BossSummonLogicTests`(신규): 경계값(정확히 임계), 여러 단계 동시 통과, 이미 지난 단계 무시, `maxHealth<=0`,
  `nextStage` 범위 밖.
- `BossPatternSchedulerTests`(수정): 후보가 둘이 되므로 재작성 — 1연속 뒤엔 반복 가능, 2연속 뒤엔 반복
  불가, 페이즈별 가중치 구간, 범위 밖 난수, 두 패턴 모두 도달 가능.
- `BossDataAssetTests`(신규 파일): `summonStages` 6개, 임계가 (0,1) 안에서
  엄격히 내림차순, 각 단계 합계 > 0. **마릿수·임계의 절대값은 assert하지 않는다**(플레이로 계속 바뀜).
- `BossController`/`SummonPattern`/디렉터는 MonoBehaviour라 컴파일+회귀 확인만.

## 스코프 밖

- 3-2 돌진 임팩트 연출(별도 스펙)
- 소환 몹의 스탯 조정, 소환 등장 이펙트 교체
- 보스 사망 시 남은 소환 몹 정리(지금 동작 그대로)
