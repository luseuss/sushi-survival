# 와사비 알현 보상 교체 — 아델린 회전 우산 설계

> 원 기획: `C:\Users\wnsdn\Desktop\스시왕국_기획정리_2026-09-26.md` 2번 섹션(와사비 알현 — 임팩트 강화 + 보상 교체).
> 이 스펙은 그 중 **2-2/2-3(아델린 보상 교체 = 회전 우산)** 만 다룬다. 2-1(연출 강화, 사운드 필요)과
> 2-4의 카마리온 보상은 이번 범위 밖 — 카마리온은 기존 스탯 강화 보상을 그대로 유지한다.

## 배경

지금 `RoyalWasabiController`는 가위바위보에 이기면 캐릭터 무관하게 공격력·공격속도·이동속도·
최대체력 4종 스탯을 크게 강화한다. 아델린만 이 보상을 "원래 공격(양산 부채꼴)이 우산 회전으로
바뀐다"로 바꾼다 — 스탯이 오르는 게 아니라 **무기 자체가 교체**된다. 카마리온은 그대로 스탯 강화.

## 1. 무기 교체 구조

`EggPlayer` 프리팹 하나에 `EggFanWeapon`과 `RotatingUmbrellaWeapon`을 **처음부터 같이** 붙여둔다
(프리팹을 별도로 복제하지 않는다). 항상 정확히 하나만 `enabled`다:

- 평소: `EggFanWeapon` 켜짐, `RotatingUmbrellaWeapon`과 우산 궤도용 자식 오브젝트 5개는 꺼진 채 대기.
- 와사비 성공 시: `EggFanWeapon.enabled = false`, `RotatingUmbrellaWeapon.enabled = true` + 레벨 승계.

`GameManager`·`BossFightDirector`·`LevelSystem`이 지금 쓰는 `player.GetComponent<WeaponBase>()`는
같은 오브젝트에 `WeaponBase` 파생 컴포넌트가 두 개 있으면 어느 쪽이 잡힐지 불명확해진다. 새 정적
헬퍼 `SushiSurvival.Weapons.PlayerWeaponResolver.GetActive(GameObject player)`를 만들어
`GetComponents<WeaponBase>()` 중 `enabled`인 것만 반환하도록 하고, 기존 `GetComponent<WeaponBase>()`
호출 3곳(`GameManager.cs` 2곳, `BossFightDirector.cs` 1곳)을 이걸로 바꾼다.

**보스 씬 이월:** `RunResultCarrier`에 `public static bool WasabiWeaponConverted;` 필드를 추가.
`GameManager.EnterBossFight()`가 기존 `RunResultCarrier.WeaponLevel` 기록 옆에 이 플래그도 같이
기록한다(현재 활성 무기가 `RotatingUmbrellaWeapon`인지로 판단). `BossFightDirector`가 플레이어를
스폰한 직후, 이 플래그가 true면 스폰된 인스턴스에서도 우산 켜기/양산 끄기를 한 번 실행한 뒤 기존
`WeaponLevel` 승계 루프(`while (weapon.CurrentLevel < RunResultCarrier.WeaponLevel) weapon.LevelUp()`)를
그대로 돌린다 — 이 루프 자체는 손 안 댄다(`PlayerWeaponResolver.GetActive`가 반환한 무기가 이미
우산이므로 우산 레벨이 오른다).

## 2. 보상 적용 흐름 — `RoyalWasabiController` 책임 분리

`RoyalWasabiController.Show()`가 지금은 연출(대사→가위바위보→결과 공개)과 "성공 시 스탯 4종
버프 적용"을 한 메서드 안에서 같이 한다. 이걸 분리한다:

- `Show()` 시그니처에서 "성공 시 무엇을 할지"를 `Action onSuccess` 콜백으로 뺀다. `RoyalWasabiController`는
  연출만 전담하고, 실제 보상 적용(스탯 버프든 무기 교체든)은 몰라도 된다.
- 새 시그니처(제안): `Show(Sprite portrait, Action onSuccess, Action onComplete)` — 기존
  `stats`/`health`/`recordBuff` 인자는 필요 없어진다(둘 다 `onSuccess` 클로저 안에서 호출자가 직접
  캡처해서 쓴다).
- 호출자는 `LevelSystem.HandleRoyalWasabiRequested()`. 여기서 `_weapon is EggFanWeapon`으로 분기:
  - `EggFanWeapon`이면 → `onSuccess = () => ConvertToUmbrella()` (아래 새 메서드, 1번의 컴포넌트
    교체 + 레벨 승계 + `LevelSystem._weapon` 참조 갱신까지 한 번에)
  - 그 외(간장새우 등, 아직 이 표기 그대로 유지)면 → `onSuccess = () => { 기존 4종 스탯 버프 적용 }`
    (지금 `RoyalWasabiController.Apply(...)` 안에 있는 로직을 `LevelSystem`으로 옮긴다)
- 실패 시 동작(위로 보상 없음)은 그대로.

`LevelSystem`에 새 메서드:
```csharp
private void ConvertToUmbrella()
{
    if (_weapon is not EggFanWeapon eggWeapon) return;

    var umbrella = eggWeapon.GetComponent<RotatingUmbrellaWeapon>();
    if (umbrella == null)
    {
        Debug.LogError($"{eggWeapon.name}: RotatingUmbrellaWeapon 컴포넌트가 없어 우산으로 전환할 수 없습니다.");
        return;
    }

    umbrella.SetLevel(eggWeapon.CurrentLevel);
    eggWeapon.enabled = false;
    umbrella.enabled = true;
    _weapon = umbrella;
}
```
`WeaponBase`에 레벨을 외부에서 직접 주입하는 `public void SetLevel(int level)`이 없으므로 추가한다
(지금은 `LevelUp()`으로 1씩만 올릴 수 있다 — 승계 시 반복 호출도 되지만 명시적 setter가 더 명확하다).

## 3. `RotatingUmbrellaWeapon` 전투 모델

`WeaponBase.Update()`는 "쿨타임 끝나면 `Attack()` 한 번" 구조라 우산의 "항상 돌면서 스치는 적마다
개별 재타격 타이머로 데미지" 모델과 안 맞는다. `WeaponBase.Update()`를 `private` → `protected virtual`로
바꾼다(기존 계란 양산·간장 소총·이나리는 override 안 해서 동작 그대로).

`RotatingUmbrellaWeapon : WeaponBase`가 `Update()`를 override:
1. 공유 회전각을 `Time.deltaTime × 회전속도`만큼 누적. 회전속도는 `StatMultiplier(StatType.AttackSpeed)`에
   비례(공격속도 증강이 "재타격 간격 단축"과 "회전 빨라짐"을 동시에 준다).
2. 레벨별 우산 개수(아래 4번)만큼 자식 오브젝트를 균등 각도로 궤도 위치에 배치하고 바깥을 향해 회전.
   각도 분배·좌표 계산은 순수 로직(`UmbrellaOrbitLogic.GetPositions(count, radius, baseAngle)` 등,
   `Core/UmbrellaOrbitLogic.cs` + EditMode 테스트)으로 뺀다 — 이 프로젝트 관례(판정/좌표 로직은
   `XxxLogic` 정적 클래스로 분리)를 따른다.
3. 각 우산 위치에서 `Physics2D.OverlapCircleAll`로 적을 찾고, 적 하나당 마지막 타격 시각을
   `Dictionary<EnemyBase, float>`로 기억해 `재타격 간격`(`BaseStats.cooldown` 재해석, 증강 없이는
   그대로, `AttackSpeed` 배율로 단축)이 지난 적만 `Damage`(`BaseStats.damage` × `AttackDamage` 배율)로
   타격한다.
4. `attackAnimator?.TriggerAttack()`은 부르지 않는다 — 계속 도는 그림 자체가 "공격 중" 상태라 트리거
   타이밍이 없다.

시각 표현: 확정된 대로 계란 공격 시트의 정적 프레임(`계란 공격-Sheet_0`)을 그대로 쓴다. 우산 자식
오브젝트 5개를 프리팹에 미리 배치해두고(전부 비활성 시작), 레벨에 따라 필요한 개수만 활성화한다.

## 4. 데이터 구조

새 `WeaponData` 에셋 `EggUmbrellaWeaponData.asset`(기존 `EggWeaponData.asset`과 나란히, 같은 폴더)을
만든다. `WeaponLevelStats[4]`에 우산 전용 수치(Lv1~4)를 담되:
- `damage` = 우산 1타 피해
- `cooldown` = 같은 적 재타격 간격
- `range` = 궤도 반경
- `angleDegrees`/`pierceCount`는 우산에 의미 없어 0으로 비워둔다(`UpgradeDescriptionLogic`이 변화
  없으면 그 줄을 자동으로 생략하므로 손 안 대도 됨)

우산 개수는 `WeaponLevelStats`를 억지로 늘리지 않고, `RotatingUmbrellaWeapon`에 별도 인스펙터 배열
`int[] umbrellaCountByLevel = {4, 4, 5, 5}`(기획서: Lv1~2는 4개, Lv3~4는 5개)로 관리한다.

**카드 설명 문구 분기:** `UpgradeDescriptionLogic.DescribeWeaponUpgrade(current, next)`에
`bool isUmbrella = false` 파라미터를 추가해서, true일 때만 라벨을 바꾼다:

| 라벨(기존) | 라벨(우산) |
|---|---|
| 공격력 | 우산 피해 |
| 쿨타임 | 재타격 간격 |
| 범위 | 궤도 반경 |

`WeaponLevelUpOption.Description`이 이 값을 부르는 자리에서, `_weapon is RotatingUmbrellaWeapon`이면
`isUmbrella: true`로 넘긴다.

## 5. 밸런스 시작값 (전부 인스펙터 노출, 플레이테스트로 조정)

기획서 2-3절 그대로, 정확한 수치는 미정이고 이게 시작점이다:
- 궤도 반경(`range`): 약 1.6 (기존 양산 사거리 2.0~2.6보다 짧게)
- 재타격 간격(`cooldown`): 약 0.3초
- 1타 피해(`damage`): 원래 양산 Lv1(8)과 비슷하게 시작
- 회전 속도: 초당 1바퀴 시작값
- 우산 개수: Lv1~2 4개, Lv3~4 5개
- 방어·체력 보너스 없음(증강 카드 해석에도 없음 — 4종 매핑 표에 방어/체력이 없다)

## 6. 증강 카드 재사용 — 코드 변경 불필요

공격력/공격속도/공격범위 증강과 무기 강화 카드는 `WeaponBase.Damage`/`Range` getter가 이미
`StatType.AttackDamage`/`AttackRange`/`AttackSpeed` 배율을 `playerStats`에서 읽어 곱하는 방식이라,
`RotatingUmbrellaWeapon`도 `WeaponBase`를 상속하는 것만으로 **자동으로 같은 증강을 적용받는다**.
`StatSystem`/`AugmentData`는 전혀 건드리지 않는다 — 기획서의 "StatSystem은 그대로" 요구사항이 구조상
저절로 만족된다.

## 스코프 밖 (이번 스펙에 없음)

- 2-1(연출 강화: 조명·진동·효과음) — 오디오 시스템 부재로 별도 작업 필요, 이번엔 손 안 댐
- 카마리온 와사비 보상 — 기존 스탯 버프 유지, 변경 없음
- 이나리 — 범위 밖(잠금 유지)
- 정확한 밸런스 수치 — 시작값만, 플레이테스트로 조정
