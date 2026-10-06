# 와사비 보상: 보조 요정 시스템 설계

> 와사비 알현(가위바위보)의 성공 보상을 "무기 교체/스탯 버프"에서 **보조 요정**으로 바꾼다.
> 아델린의 회전 우산은 이미 와사비와 무관한 레벨업 성장 경로로 옮겼다(PR #124).
> 와사비 버튼은 지금 `LevelSystem.royalWasabiEnabled = false`로 숨겨져 있고, 이 작업이 끝나면 다시 켠다.

## 확정된 결정

| 항목 | 결정 |
|---|---|
| 획득 | 와사비 알현 성공 보상. **모든 캐릭터** 공통 |
| 마릿수 | 최대 **3마리** |
| 성공할 때마다 | 새 요정 소환 **또는** 기존 요정 하나 강화를 카드로 고른다 |
| 공격 | 가장 가까운 적에게 **자동 투사체**. 종류는 한 가지(개성 차등은 시간 남으면, 구조만 열어 둔다) |
| 이동 | 플레이어 주변 슬롯을 부드럽게 따라다니며 위아래로 둥둥 |
| 아트 | 나중에 넣는다. 지금은 **플레이스홀더**(런타임 생성 원 스프라이트) |
| 데이터 | 레벨별 수치는 **`WeaponData` 재사용**(피해·쿨타임·관통·사거리) |
| 투사체 | 기존 `Projectile` + `GameObjectPool` 재사용 |
| 선택 UI | 기존 **레벨업 카드(`LevelUpPanel`) 재사용** — 새 UI·씬 작업 없음 |
| 성공 확률 | **40%**로 낮춘다(아래 "성공 확률" 참고) |
| 보스 씬 | 요정 레벨 배열을 `RunResultCarrier`로 이월해 복원 |

## 성공 확률 (사전 발견)

와사비는 이제 **가위바위보**다(`RockPaperScissorsPanel`) — 처음 설계의 "10% 확률 판정"은 이미 사라졌다.
상대 손을 `_random.Next(3)`으로 뽑고 비기면 다시 하므로 지금 승률은 정확히 **50%**다.
40%로 만들려면 상대 손을 편향시켜야 한다. 비김 확률(1/3)은 그대로 두고 판정이 갈릴 때만 승률을 맞춘다:

- 비김 1/3, 이김 `2/3 × winChance`, 짐 `2/3 × (1 − winChance)` → 비김을 다시 하면 최종 승률 = `winChance`
- 플레이어가 낸 손에 대해 이기는/지는 상대 손은 하나씩 정해져 있으므로 순수 함수로 뽑는다:
  `RpsOpponentLogic.PickOpponentHand(playerHand, winChance, roll)`
- `RockPaperScissorsPanel`에 `[Range(0,1)] float winChance = 0.4f` 필드를 둔다(인스펙터 조정, 플레이테스트로 튜닝)

## 1. 구성

| 파일 | 역할 | 구분 |
|---|---|---|
| `Core/RpsOpponentLogic.cs` | 편향된 상대 손 뽑기 | 신규(순수) |
| `UI/RockPaperScissorsPanel.cs` | `winChance` 필드, 상대 손을 `RpsOpponentLogic`로 | 수정 |
| `Fairy/FairySlotLogic.cs` | 마릿수·시간별 슬롯 위치, 둥둥 오프셋, 따라가기 보간 | 신규(순수) |
| `Fairy/FairyTargetLogic.cs` | 사거리 안 가장 가까운 적 고르기 | 신규(순수) |
| `Fairy/FairyChoiceLogic.cs` | 현재 요정 레벨들로부터 선택지(소환/강화) 목록 만들기 | 신규(순수) |
| `Fairy/Fairy.cs` | 요정 한 마리 — 슬롯 따라가기 + 쿨타임마다 발사 | 신규 |
| `Fairy/FairyController.cs` | 요정 목록 관리, 소환/강화, 이월·복원 | 신규 |
| `Core/FairyOption.cs` | `IUpgradeOption` 구현 — 카드로 뜨는 소환/강화 | 신규 |
| `Core/LevelSystem.cs` | 와사비 성공 → 요정 선택 흐름, 스위치 켬 | 수정 |
| `Core/RunResultCarrier.cs` | `FairyLevels` 필드 | 수정 |
| `Core/GameManager.cs`, `BossFightDirector.cs` | 보스 씬 이월/복원 | 수정 |
| 씬(사용자 에디터) | 요정 시스템 오브젝트·풀·프리팹 연결 | 사용자 |

## 2. 순수 로직

```csharp
// FairySlotLogic
public static Vector2 SlotOffset(int index, int count, float radius, float time)   // 플레이어 중심 기준 슬롯 위치 + 둥둥(sin) 오프셋
public static Vector2 Follow(Vector2 current, Vector2 target, float sharpness, float dt) // 1 - exp(-k dt) 지수 보간 (프레임레이트 무관)

// FairyTargetLogic
public static int NearestIndex(Vector2 origin, IReadOnlyList<Vector2> positions, float maxRange) // 없으면 -1

// FairyChoiceLogic  — 선택지는 최대 3개(카드 3장)
public enum FairyChoiceKind { Summon, Upgrade }
public struct FairyChoice { public FairyChoiceKind Kind; public int Index; }
public static List<FairyChoice> Build(IReadOnlyList<int> levels, int maxCount, int maxLevel)
// count < maxCount 이면 Summon 1개 + 각 요정 중 level < maxLevel 이면 Upgrade(i). 3장 초과 안 됨(요정 3마리일 땐 Upgrade만 최대 3).
// 선택지가 하나도 없으면(3마리 전부 최대 레벨) 빈 목록 → 호출 쪽이 보상 대체(아래)

// RpsOpponentLogic
public static RpsHand PickOpponentHand(RpsHand player, float winChance, float roll)  // roll 0~1: <1/3 비김, 그다음 2/3×winChance 구간 이김, 나머지 짐
```

## 3. 요정 동작

- `FairyController`는 **씬 오브젝트**(플레이어 프리팹에 붙이지 않음 — 캐릭터 프리팹 수정이 없다).
  `SetPlayer(Transform, PlayerStats)`를 `GameManager`가 스폰 직후 부른다(`LevelSystem.SetPlayer`와 같은 패턴).
- 요정마다 레벨(1~`weaponData.levels.Length`)이 있고, 발사 수치는 `weaponData.levels[level-1]`에서 읽는다.
  피해는 `PlayerStats`의 공격력 배율을 곱한다(증강이 요정에도 먹는다 — `StatSystem` 재사용, 새 버프 시스템 없음).
  쿨타임은 공격속도 배율과 `CooldownLogic.ApplyAttackSpeed`(최소 쿨타임 하한)를 쓴다.
- `Fairy`는 매 프레임 `FairySlotLogic.SlotOffset` 목표를 향해 `Follow`로 움직이고, 쿨타임이 되면
  사거리 안 적을 `FairyTargetLogic.NearestIndex`로 골라 `Projectile`을 쏜다. 적 목록은
  `Physics2D.OverlapCircleAll(요정 위치, 사거리, enemyLayer)`(무기와 같은 방식).
  타깃이 없으면 쏘지 않고 쿨타임도 소모하지 않는다.
- 투사체 풀은 `FairyController`의 인스펙터 필드(씬의 풀 오브젝트)에서 요정에게 주입한다
  (`ShrimpRifleWeapon.SetProjectilePool`과 같은 패턴 — 프리팹이 씬 오브젝트를 참조 못 하는 문제의 기존 해법).
- 플레이스홀더: 요정/투사체 프리팹이 비어 있어도 시험할 수 있게, 요정 프리팹이 없으면 `CircleTextureFactory`로
  작은 원 스프라이트 오브젝트를 만든다. 투사체는 기존 `Projectile` 프리팹을 복제해 쓴다.

## 4. 와사비 흐름

1. 와사비 알현 → 가위바위보. 성공(40%)이면 `onSuccess`가 호출된다. 이 콜백은 `FairyChoiceLogic.Build`로 선택지를 만든다.
2. **선택지가 있으면:** 결과 줄에 "요정의 힘을 얻었다!"를 보여주고, 결과 화면 확인 후(`onComplete`)
   `LevelUpPanel.Show(요정 카드 목록, OnFairyChosen, null)`로 카드 선택을 띄운다. 선택하면 적용하고 `ShowNext`로 이어진다.
   (`_panelOpen`과 `timeScale=0`은 연출이 끝날 때까지 유지되는 기존 규칙 그대로.)
3. **선택지가 없으면**(3마리 모두 최대 레벨): 기존 `ApplyRoyalWasabiStatBuffs`를 대체 보상으로 적용한다 — 성공이 헛되지 않게.
4. 실패하면 지금처럼 위로 보상 없음(그 레벨업의 카드 선택은 날아간다).
5. 카마리온 샷건(`ConvertToShotgun`)과 아델린 `ConvertToUmbrella`의 와사비 경로 호출은 없앤다.
   `ConvertToUmbrella`는 레벨업 성장 경로에서 계속 쓰이고, `ConvertToShotgun`·`EnableShotgun` 코드는 삭제하지 않고 남긴다(협업자 작업, 호출만 끊음).
6. `royalWasabiEnabled` 기본값을 **true**로 되돌린다.

## 5. 보스 씬 이월

- `RunResultCarrier.FairyLevels`(`int[]`)를 추가한다. `GameManager`가 `BossScene`을 열기 직전에 현재 요정 레벨 배열을 기록한다.
- `BossFightDirector`가 플레이어를 스폰한 뒤 `FairyController.RestoreFairies(levels)`로 같은 레벨의 요정을 만든다.
  BossScene에도 `FairyController`(요정 시스템 오브젝트)를 사용자가 추가해야 한다.
- `WasabiWeaponConverted`는 아델린 우산·카마리온 샷건 복원용으로 그대로 둔다(아델린 우산 복원은 레벨업 경로에서도 필요).
  `WasabiCount`는 결과 화면의 와사비 아이콘 개수로 계속 이어진다.

## 6. 테스트

- `FairySlotLogicTests`: 마릿수별 슬롯이 서로 겹치지 않음, 둥둥 오프셋 범위, `Follow`가 프레임레이트와 무관하게 목표로 수렴(두 번 반 dt vs 한 번 dt)
- `FairyTargetLogicTests`: 가장 가까운 적 선택, 사거리 밖은 -1, 빈 목록 -1, 같은 거리 안정성
- `FairyChoiceLogicTests`: 0마리 → Summon만, 1마리 → Summon+Upgrade, 3마리 → Upgrade 최대 3, 최대 레벨 요정은 Upgrade에서 제외, 전부 최대 → 빈 목록, 선택지 3장 초과 없음
- `RpsOpponentLogicTests`: 전 구간에서 비김/이김/짐이 정확한 손으로 나옴, `winChance` 0·1에서 판정이 갈릴 때 항상 짐/이김, 대량 롤 평균이 기대 비율에 수렴
- 데이터 에셋 테스트는 구조만(레벨 수 4, 수치는 assert하지 않음 — 밸런스는 자주 바뀐다)
- MonoBehaviour(`Fairy`, `FairyController`, `FairyOption`, `LevelSystem` 연결)는 컴파일 + 회귀 + 플레이 확인

## 7. PR 분할(의존 순서)

| PR | 내용 | 의존 |
|---|---|---|
| 0 | 이 스펙 문서 | — |
| A | `RpsOpponentLogic` + 패널 `winChance` 0.4 | 없음 |
| B | 순수 로직 3종 + 테스트(`FairySlotLogic`, `FairyTargetLogic`, `FairyChoiceLogic`) | 없음 |
| C | `Fairy`, `FairyController`, `FairyOption`(전부 추가만, 아직 아무도 안 부름) | B |
| D | `LevelSystem` 와사비 흐름 전환 + `royalWasabiEnabled` true + `RunResultCarrier`/`GameManager`/`BossFightDirector` 이월 | A, C |
| E | 씬 배선(사용자 에디터): `GameScene`·`BossScene`에 요정 시스템 오브젝트·풀·`WeaponData`, 요정 `WeaponData` 에셋 | D |

A와 B는 서로 독립이라 병렬로 진행할 수 있다. 모든 새 코드는 추가만 하고 D에서 비로소 호출되므로
(기본값이 "아무 영향 없음"인 설계 — 과거 PR 분할 교훈), C까지 먼저 머지돼도 안전하다.
D 전에 `LevelSystem.cs`·`BossFightDirector.cs`를 협업자 최근 커밋과 대조한다(씬 병합 충돌 대비).

## 8. 범위 밖 / 미결

- **요정 종류별 개성**(색·공격 방식 차등)은 이번에 넣지 않는다. `Fairy`가 `WeaponData` 하나에 묶여 있어서
  나중에 요정마다 다른 `WeaponData`/프리팹을 주는 확장이 쉽다(`FairyController`의 슬롯별 데이터 배열화).
- 요정 아트·사운드는 에셋이 들어오면 프리팹 교체만 한다.
- 요정이 적에게 피격되거나 죽는 개념은 없다(무적, 판정 없음).
