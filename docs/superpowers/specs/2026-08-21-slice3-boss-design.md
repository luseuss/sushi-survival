# 슬라이스 3 — 보스전 설계

> 작성 2026-08-21. 슬라이스 2d(무한 맵 + 적 뭉침 방지) 다음 단계.

## 목표

5:00에 등장하는 보스를 만들어 **한 판을 기획서대로 완결시킨다.** 지금은 5:00에
도달하면 자동 승리하는 임시 조건이 걸려 있는데, 이걸 "보스 처치 = 승리"로 교체한다.

보스전은 약 1분 내외를 목표로 한다. 총 플레이타임 5~7분이라는 기획 목표 안에
들어와야 하며, 팝업 부스 데모라 한 판이 길어지는 것보다 짧게 끝나는 쪽이 낫다.

## 아트 실측

작업 전 아트를 전부 실측했다. 설계가 여기서 도출됐으므로 먼저 기록한다.

| 파일 | 크기 | 프레임 | 내용 |
|---|---|---|---|
| `보스몹 .png` | 100×100 | 1 | 마녀모자 셰프. 정지 이미지 |
| `보스몹 대기-Sheet.png` | 500×100 | 5 | 대기 |
| `보스몹 이동모션)-Sheet.png` | 600×100 | 6 | 이동 |
| `보스 공격 패턴 1-Sheet.png` | 1300×100 | 13 | 시전 — **빨간** 구슬 |
| `보스몹 소환 패턴-Sheet.png` | 1300×100 | 13 | 시전 — **초록** 구슬 |
| `보스공격 이펙트.png` | 500×150 | 10 (50×150) | 불덩이 낙하 → 지면 폭발 → 연기 |
| `보스 소환 이펙트.png` | 400×150 | 8 (50×150) | 검은 초밥 병사가 땅에서 솟아오름 |

**두 시전 시트는 같은 그림에 구슬 색만 다르다.** 지팡이를 들어 구슬을 만들고 위로
쏘아 올리는 13프레임이 공통이고, 공격은 빨강, 소환은 초록이다.

이펙트 두 장이 그 구슬의 착지 지점을 설명한다. 공격 이펙트는 하늘에서 떨어져
폭발하는 **메테오 광역기**이고, 소환 이펙트는 잡몹의 **등장 모션**이다. 즉 패턴
두 종의 성격이 아트 단계에서 이미 확정돼 있다.

**보스 스프라이트가 100×100으로 중형몹과 같다.** 그대로 임포트하면 보스가 중형몹과
똑같은 크기로 보여 위압감이 없다. 보스만 **Pixels Per Unit을 약 30% 낮춰** 1.4배로
렌더되게 한다. 프로젝트 규약상 Transform Scale이 아니라 PPU로 맞춘다.

## 확정된 설계 결정

| 항목 | 결정 | 근거 |
|---|---|---|
| 보스전 중 잡몹 스폰 | **완전 정지** | 소환 패턴이 잡몹 공급을 대신한다. 스폰까지 겹치면 메테오 예고가 잡몹에 묻힌다 |
| 보스 체력 | **1,800** (기획서 4,000에서 하향) | 아래 DPS 역산 참고 |
| 페이즈 | **2페이즈, 수치만 강화** | 새 아트 없이 난이도 상승. 50% 임계, 되돌아가지 않음 |
| 메테오 조준 | **예고 마커 생성 시점의 플레이어 위치** | 움직이면 피해지고 멈추면 겹쳐 맞는다 |
| 메테오 발수 | **1페이즈 3발 / 2페이즈 5발** | |
| 연출 범위 | **체력바 + 등장 + 격파** | 등장 자리에 나중에 호감도 대화 #2가 들어간다 |
| 보스 등장 시 필드 | **남은 적 전부 처치 처리** | 아래 참고 |

### 보스 체력을 1,800으로 낮춘 이유

기획서 v1.3의 4,000을 그대로 쓰면 캐릭터에 따라 보스전 길이가 4배 넘게 벌어진다.
5:00 시점의 현실적인 빌드(무기 Lv4 + 공격력·공격속도 증강 약간)로 역산한 값:

| 캐릭터 | 단일 대상 DPS | 4,000 격파 | 1,800 격파 |
|---|---|---|---|
| 간장새우 Lv4 (20 / 0.65초) | 약 60/초 | 1분 7초 | **30초** |
| 계란 Lv4 (15 / 1.0초) | 약 29/초 | 2분 18초 | **1분 2초** |
| 계란, 증강 운 나쁨 | 15/초 | 4분 30초 | **2분** |

계란은 다중 히트 무기라 **보스 한 마리 앞에서는 광역이 전부 낭비된다.** 기획서상으로도
계란의 단일 DPS가 최저(6.7/초)로 설계돼 있다. 4,000을 유지하면 계란으로 고른 관람객의
한 판이 9분을 넘긴다.

1,800은 SO 값이므로 플레이테스트에서 인스펙터로 즉시 조정 가능하다.

### 보스 등장 시 남은 적을 전부 처치하는 이유

중형몹은 200 체력에 금젬(10 XP)을 떨어뜨린다. 플레이어가 중형몹 둘을 못 잡고 도망만
다녔다면 그만큼 성장이 덜 된 채로 1,800 체력의 보스를 만나게 되어 보스전이 급격히
어려워진다. 등장 시점에 남은 적을 전부 처치 처리하면 그 성장분이 보장되고, 아레나가
한 번 비워져 소환 패턴이 위협으로 읽힌다.

잡몹도 함께 정리한다. 중형몹만 골라내면 보스전 시작과 동시에 잡몹 십수 마리와 뒤엉킨
채 메테오를 피해야 해서, 잡몹 스폰을 멈추기로 한 결정 자체가 무의미해진다.

## 아키텍처

### 런 상태 — `RunState`는 늘리지 않는다

`GameManager`가 `ElapsedTime >= runDuration`에서 곧바로 승리 처리하던 것을 **보스 등장
트리거**로 바꾼다. `runDuration`은 `bossSpawnTime`(300초)으로 이름을 바꾸고, 승리는
오직 보스 사망에서만 발생한다.

**`RunState`에 `BossFight`를 추가하지 않는다.** 현재 코드 곳곳이
`CurrentState != RunState.Playing`으로 가드하고 있어서(`GameManager.AddExperience`,
`GameManager.RegisterKill`, `WaveDirector.Update`), 상태를 늘리면 보스전 중에 경험치가
들어오지 않고 처치 수가 세지지 않는 버그가 **에러 없이 조용히** 생긴다. 보스 진행은
새 `BossDirector`가 자기 안에서만 관리하고 `CurrentState`는 보스전 내내 `Playing`으로
유지한다.

`WaveDirector.WarnAboutUnreachableEvents`가 `RunDuration`을 참조하므로 함께 수정한다.

### 보스는 `EnemyBase`와 `EnemyAI`를 재사용한다

플레이어 무기들이 `TryGetComponent<EnemyBase>()`로 대상을 찾는다
(`Projectile.OnTriggerEnter2D`, `EggFanWeapon`). **보스에 `EnemyBase`가 없으면 아예
때릴 수 없다.** 따라서 보스도 `EnemyBase` + `EnemyAI`를 그대로 얹고 보스 고유 행동만
새 컴포넌트로 붙인다. 체력·피격·넉백·접촉 데미지·사망 처리가 전부 공짜로 따라온다.

`EnemyAI`에 확장 하나만 추가한다:

```csharp
/// <summary>추격 속도 배율. 0이면 제자리에 선다(시전 중). 넉백은 이 값과 무관하게 적용된다.</summary>
public float MoveScale { get; set; } = 1f;
```

시전 중 정지와 페이즈 2 가속을 이 하나로 처리한다. 넉백을 `MoveScale`에서 제외하는
이유는, 시전 중에도 총에 맞으면 조금은 밀려야 타격감이 살기 때문이다.

### 새 파일

| 파일 | 책임 | 순수 로직 |
|---|---|---|
| `Enemies/Boss/BossDirector.cs` | 등장·격파 시퀀스, 필드 정리, 승리 전환. 씬 오브젝트 | |
| `Enemies/Boss/BossController.cs` | 대기/이동/시전 상태 기계, 패턴 발동, 페이즈 전환 | |
| `Enemies/Boss/BossPhaseLogic.cs` | 체력 비율 → 페이즈 번호 | ✅ |
| `Enemies/Boss/BossPatternScheduler.cs` | 다음 패턴 선택 + 쿨타임 경과 | ✅ |
| `Enemies/Boss/MeteorVolley.cs` | 연발 타이밍(발수·간격) 계산 | ✅ |
| `Enemies/Boss/MeteorPattern.cs` | 연발 시퀀스 실행, 메테오 생성 | |
| `Enemies/Boss/Meteor.cs` | 예고 → 낙하 → 폭발 → 광역 데미지 | |
| `Enemies/Boss/SummonPlacement.cs` | 링 위 소환 위치 계산 | ✅ |
| `Enemies/Boss/SummonPattern.cs` | 등장 이펙트 재생 후 잡몹 활성화 | |
| `Core/CircleTextureFactory.cs` | 런타임 원형 마커 텍스처 생성 | ✅ |
| `Data/BossData.cs` | `MonsterData` 상속 + 페이즈별 패턴 수치 | |
| `UI/BossHealthBar.cs` | 상단 체력바 (`HealthBarLogic` 재사용) | |

### 수정할 기존 파일

| 파일 | 변경 |
|---|---|
| `Core/GameManager.cs` | `runDuration` → `bossSpawnTime`, 5:00 자동 승리 제거, `BossDirector` 배선 |
| `Core/LevelSystem.cs` | `public bool IsShowingPopup` 노출 |
| `Enemies/EnemyAI.cs` | `MoveScale` 프로퍼티 추가 |
| `Enemies/WaveDirector.cs` | `RunDuration` 참조를 `BossSpawnTime`으로 |

## 데이터 — `BossData`

`MonsterData`를 상속해 만든다. Unity의 ScriptableObject 상속은 정상 동작하며,
`EnemyBase`의 `MonsterData` 필드에 그대로 꽂힌다.

```csharp
[CreateAssetMenu(menuName = "SushiSurvival/Boss Data", fileName = "NewBossData")]
public class BossData : MonsterData
{
    public BossPhaseValues phaseOne;
    public BossPhaseValues phaseTwo;

    [Range(0f, 1f)]
    [Tooltip("현재 체력 비율이 이 값 아래로 내려가면 페이즈 2로 전환한다.")]
    public float phaseTwoThreshold = 0.5f;
}

[System.Serializable]
public struct BossPhaseValues
{
    public float patternInterval;   // 패턴 사이 간격(초)
    public float moveScale;         // EnemyAI.MoveScale에 넣을 값
    public int meteorCount;
    public float meteorSpacing;     // 발 사이 간격(초)
    public float meteorWarningTime; // 예고 → 낙하까지(초)
    public float meteorDamage;
    public float meteorRadius;
    public int summonCount;
    public float summonRadius;      // 플레이어로부터의 소환 링 반경
}
```

### 수치 (제안값 — 전부 인스펙터에서 조정 가능)

상속받은 `MonsterData` 필드:

| 필드 | 값 |
|---|---|
| `maxHealth` | 1800 |
| `contactDamage` | 15 |
| `moveSpeed` | 1.6 |
| `knockbackResistance` | 0.9 |
| `xpGemDrop` | Ten |

페이즈별:

| 필드 | 페이즈 1 | 페이즈 2 |
|---|---|---|
| `patternInterval` | 4.0 | 2.5 |
| `moveScale` | 1.0 | 1.3 |
| `meteorCount` | 3 | 5 |
| `meteorSpacing` | 0.35 | 0.3 |
| `meteorWarningTime` | 0.7 | 0.6 |
| `meteorDamage` | 20 | 25 |
| `meteorRadius` | 1.2 | 1.2 |
| `summonCount` | 4 | 6 |
| `summonRadius` | 4.0 | 4.0 |

`phaseTwoThreshold` = 0.5

## 보스 상태 기계 — `BossController`

```
        ┌──────────────────────────────┐
        │            Idle              │  대기 애니
        │  (패턴 쿨타임 소진 대기)      │
        └───────────┬──────────────────┘
                    │ 플레이어와 멀면
                    ▼
        ┌──────────────────────────────┐
        │            Move              │  이동 애니, MoveScale = 페이즈 값
        └───────────┬──────────────────┘
                    │ 쿨타임 만료
                    ▼
        ┌──────────────────────────────┐
        │           Casting            │  시전 애니 13프레임(약 1.1초)
        │      MoveScale = 0 (정지)     │  구슬 색으로 패턴 구분
        └───────────┬──────────────────┘
                    │ 애니 종료 = 구슬이 하늘로
                    ▼
              패턴 발동 → Idle
```

- `Idle`과 `Move`는 `EnemyAI`가 이미 하는 추격을 그대로 쓰고, 애니메이터의 `IsMoving`
  bool만 갱신한다. 캐릭터의 `CharacterAnimator`와 같은 규약이다.
- 시전 애니는 12FPS 기준 13프레임 = 약 1.1초. `Loop Time` 해제.
- 시전 시작 시 `MoveScale = 0`, 종료 시 페이즈 값으로 복귀.
- 패턴 발동 시점은 **애니 종료 시점**(구슬이 화면 위로 사라진 프레임)이다.

### 페이즈 전환

`BossPhaseLogic.GetPhase(currentHealth, maxHealth, threshold)`가 1 또는 2를 돌려준다.
`BossController`가 매 프레임 확인하고 값이 바뀐 프레임에 한 번만 전환 처리한다.
**되돌아가지 않는다** — 회복 수단이 없으므로 단순 임계 비교로 충분하고, 경계에서
떨거나 하는 문제도 없다.

전환 연출: 보스 스프라이트를 0.3초간 붉게 틴트했다 복구. 새 아트가 필요 없다.

## 패턴 1 — 메테오 (빨간 구슬)

시전이 끝나면 `meteorCount`발을 `meteorSpacing` 간격으로 순차 발사한다.

각 발의 수명:

1. **예고** — 낙하 지점에 원형 마커 생성. 위치는 **이 순간의 플레이어 위치**를 잡는다.
2. `meteorWarningTime` 동안 마커가 안쪽부터 차오른다. 동시에 불덩이 낙하 프레임(1~4)이
   위에서 내려온다.
3. **폭발** — 마커 중심 반경 `meteorRadius` 안의 플레이어에게 `meteorDamage`.
   폭발·연기 프레임(5~10) 재생 후 풀 반환.

**메테오는 플레이어만 때린다.** 소환된 잡몹까지 휩쓸면 보스가 자기 소환물을 지워
패턴 둘이 서로를 무효화한다. 판정은 `PlayerHealth` 하나만 찾는다.

**연발이 같은 자리에 겹치지 않는 이유:** 각 발이 자기 마커 생성 시점의 위치를 잡으므로
플레이어가 움직이면 자연히 흩어진다. 멈춰 있으면 3발이 한자리에 겹쳐 60 데미지가
들어온다 — 뱀서류의 "멈추면 죽는다" 규칙과 일치하며, 별도의 산개 로직이 필요 없다.

`MeteorVolley`(순수)가 발수와 간격만 계산하고, 실제 생성은 `MeteorPattern`이 한다.

### 예고 마커 — 런타임 생성 텍스처

마커 아트가 없다. `CircleTextureFactory`가 런타임에 원형 텍스처를 만든다. 타일 에셋을
`ScriptableObject.CreateInstance<Tile>()`로 만든 것과 같은 접근이다(슬라이스 2d).

- 바깥 테두리 링은 불투명한 붉은색, 안쪽은 반투명
- 채움 정도를 0→1로 올려 남은 시간을 표현
- 나중에 마커 아트가 생기면 `SpriteRenderer`의 스프라이트만 교체하면 된다

순수 함수 `CircleTextureFactory.GetPixelAlpha(x, y, size, fillRatio)`로 분리해
테스트한다.

## 패턴 2 — 소환 (초록 구슬)

`summonCount`마리를 플레이어 중심 반경 `summonRadius`의 링 위에 균등 배치한다.
`SummonPlacement`(순수)가 위치를 계산하고, 기존 `SpawnRingUtility`를 재사용한다.
시작 각도는 매번 무작위로 회전시켜 같은 자리에만 나오지 않게 한다.

**등장 이펙트 8프레임(약 0.65초)이 끝나야 잡몹이 움직이고 접촉 판정이 켜진다.**
솟아오르는 도중에 접촉 데미지를 주면 플레이어가 피할 방법이 없어 억울하다.

소환된 잡몹은 **기존 `BasicMob` 풀을 그대로 쓴다.** `EnemyBase.SetXpGemPools`로
젬 풀을 주입하므로 젬도 정상 드롭되고, 보스전 중에도 레벨업이 가능하다.

## 패턴 교대 — `BossPatternScheduler`

메테오 → 소환 → 메테오 순으로 **번갈아** 발동한다. 무작위로 뽑으면 소환이 연달아
나와 화면이 잡몹으로 덮이거나, 메테오만 연달아 나와 단조로워진다.

```csharp
public static BossPatternType SelectNext(BossPatternType previous)
    => previous == BossPatternType.Meteor ? BossPatternType.Summon : BossPatternType.Meteor;
```

첫 패턴은 메테오다. 보스가 등장하자마자 잡몹을 뿌리면 등장 연출이 묻힌다. 구현상
직전 패턴의 초기값을 `Summon`으로 두면 첫 호출이 자연히 `Meteor`를 돌려준다.

## 연출

### 보스 인스턴스 취급

보스는 **풀링하지 않는다.** 한 판에 한 마리뿐이고 재사용할 일이 없다. 씬에 비활성
상태로 미리 두거나 `BossDirector`가 프리팹에서 하나만 생성한다.

그 결과 `EnemyBase._selfPool`이 null이 되어 `Die()`가 `Destroy(gameObject)`로 빠지는데,
이게 의도한 동작이다. 다만 **`BossDirector`가 보스를 활성화하기 전에 반드시
`EnemyBase.SetXpGemPools()`를 호출해야 한다** — 빠뜨리면 보스가 죽는 순간
"xpGemPools가 설정되지 않아 XP 젬을 드롭할 수 없습니다" 에러가 뜬다. 스포너가 아니라
`BossDirector`가 주입 책임을 갖는 유일한 경우다.

### 등장 시퀀스 — `BossDirector.BeginIntro()`

```
5:00 도달 (GameManager가 1회만 호출)
 │
 ├ 1. EnemySpawner.StopSpawning()
 │
 ├ 2. 필드 정리
 │      Object.FindObjectsOfType<EnemyBase>()로 살아 있는 적을 모아
 │      각각에 float.MaxValue 데미지를 준다. Die()를 경유하므로
 │      젬 드롭·처치 수 반영·풀 반환이 전부 기존 경로로 처리된다.
 │      한 판에 한 번뿐이라 FindObjectsOfType의 비용은 문제되지 않는다.
 │
 ├ 3. 레벨업 팝업이 전부 닫힐 때까지 대기 (LevelSystem.IsShowingPopup)
 │
 ├ 4. [ 호감도 대화 #2 삽입 지점 — 이번 슬라이스에서는 비워둔다 ]
 │
 ├ 5. Time.timeScale = 0.3
 │      화면 상단에 "보스 등장" 1.5초 (실시간 대기)
 │
 ├ 6. 플레이어 위쪽 거리 8에 보스 배치, BossController 활성화
 │
 └ 7. Time.timeScale = 1, 보스 체력바 표시
```

**보스는 반드시 2단계 이후에 배치한다.** 먼저 배치하면 필드 정리의 즉사 데미지에
보스 자신이 휩쓸려 등장하자마자 죽는다.

**2단계와 3단계의 순서가 중요하다.** 필드 정리로 젬이 한꺼번에 쏟아지면 레벨업 팝업이
여러 번 연달아 뜬다. 팝업은 `Time.timeScale = 0`이고 등장 연출은 `0.3`이라, 순서를
지키지 않으면 팝업이 열린 채 연출이 진행되거나, 연출이 끝나며 `timeScale`을 1로
되돌려 **팝업이 열린 채 보스전이 시작된다.** 슬라이스 2c에서 겪은 "결과 화면
`timeScale` 초기화 누락"과 같은 계열의 함정이다.

`LevelSystem.IsShowingPopup`은 호감도 대화 #2가 들어올 때도 똑같이 필요한 가드다.

부수 효과로 보스전 직전에 증강을 몇 개 몰아서 고르는 순간이 생기는데, 이건 오히려
좋다 — 보스를 앞두고 빌드를 마무리하는 리듬이 된다.

### 격파 시퀀스

```
EnemyBase.OnDeath 수신 (BossDirector가 구독)
 ├ Time.timeScale = 0.3
 ├ 보스 위치에 공격 이펙트의 폭발 프레임(5~10)을 2배 크기로 재생
 ├ 1.2초 실시간 대기
 └ GameManager.FinishRun(RunOutcome.Victory)
```

보스 전용 사망 아트가 없어서 폭발로 덮는 방식이다. `EnemyBase.Die()`가 `OnDeath`를
쏜 뒤 자기 오브젝트를 정리하므로, 폭발 이펙트는 **보스와 별개의 오브젝트**로 생성해야
한다. 보스가 사라지고 그 자리에서 폭발이 이어지는 그림이 된다.

`FinishRun`이 `Time.timeScale = 0`을 설정하므로 여기서 1로 되돌릴 필요는 없다.
`Restart()`가 이미 1로 복구한다.

### 보스 체력바 — `UI/BossHealthBar.cs`

화면 상단 가로 바. 기존 `HealthBarLogic`(비율 계산)과 `HealthBar`의 구조를 그대로
따른다. 다른 점은 월드 공간이 아니라 화면 고정이라는 것뿐이다.

`EnemyBase`에는 체력 변경 이벤트가 없으므로 `CurrentHealth`를 매 프레임 읽는다.
보스 하나뿐이라 비용이 문제되지 않고, `EnemyBase`에 이벤트를 추가하면 잡몹 수십
마리가 전부 그 비용을 내게 된다.

## 테스트 계획

순수 로직만 EditMode로 검증한다. 상태 기계와 코루틴 연출은 플레이테스트로 확인한다.

| 대상 | 검증 내용 |
|---|---|
| `BossPhaseLogic` | 100%·51%·50%·49%·0%에서의 페이즈. 임계 경계값 포함 |
| `BossPatternScheduler` | 메테오/소환이 정확히 번갈아 나오는가 |
| `MeteorVolley` | 발수만큼 발사되는가, 간격이 정확한가, 0발/음수 간격 방어 |
| `SummonPlacement` | 링 위 균등 분포, 요청한 개수만큼, 서로 겹치지 않는 최소 간격 |
| `CircleTextureFactory` | 중심·테두리·바깥의 알파, 채움 비율 0/0.5/1 |

기존 테스트 168개가 전부 통과 상태를 유지해야 한다. 특히 `EnemyAI`에 `MoveScale`을
추가하는 변경이 기존 적 이동을 깨지 않는지 확인한다(기본값 1이므로 동작 불변).

## 스코프 밖 (이번 슬라이스에서 만들지 않는 것)

- **호감도 대화 #2** — 등장 시퀀스에 자리만 비워둔다. 대본이 아직 없다
- **보스 3번째 패턴** — 아트가 2종뿐이다
- **페이즈 3** — 1분짜리 싸움에 3페이즈는 과하다
- **보스 사망 전용 아트** — 폭발 이펙트 재사용으로 대체
- **예고 마커 아트** — 런타임 생성 텍스처로 대체

## 에디터 작업 (사용자가 직접)

1. 보스 스프라이트 6장 슬라이스 (100×100 격자, 이펙트 2장은 50×150)
2. 보스만 PPU를 30% 낮춰 임포트
3. 애니메이션 클립 5종 — 대기(5f), 이동(6f), 공격시전(13f), 소환시전(13f) +
   Animator Controller. 시전 클립은 `Loop Time` 해제
4. 이펙트 클립 2종 — 메테오(10f), 소환등장(8f)
5. `BossData` 에셋 생성 및 위 수치 입력
6. 보스 프리팹 조립 — `EnemyBase` + `EnemyAI` + `BossController` + Rigidbody2D
   (Kinematic, **Use Full Kinematic Contacts 켬**) + Collider2D, Layer = Enemy
7. 메테오·소환이펙트 프리팹 + 각각의 풀
8. 씬에 `BossDirector`, 상단 `BossHealthBar` 배치 및 배선

## 참고 — 앞선 슬라이스에서 얻은 함정

- **Play 모드 중에는 스크립트 수정이 반영되지 않는다.** 고쳤는데 그대로면 정지 후 재컴파일
- **풀링 오브젝트는 반드시 스스로 풀에 반환해야 한다.** 메테오와 소환 이펙트 둘 다
  수명 타이머로 반드시 회수되는 경로를 갖는다
- **프리팹 에셋은 씬 오브젝트(풀)를 참조할 수 없다.** 런타임 주입 패턴을 쓴다
- **`Time.timeScale`은 씬을 다시 로드해도 초기화되지 않는다**
- **EditMode 테스트 배치 실행 시 `-runTests`와 `-quit`을 같이 쓰면 결과 파일이 생기지 않는다**
