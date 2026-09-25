# 와사비 알현 보상 교체(아델린 회전 우산) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 아델린이 와사비 알현에서 이기면 스탯 버프 대신 기존 공격(양산 부채꼴)이 캐릭터 주위를 도는 회전 우산으로 바뀌게 한다. 카마리온은 기존 스탯 버프 보상을 그대로 유지한다.

**Architecture:** `EggPlayer` 프리팹에 `EggFanWeapon`과 새 `RotatingUmbrellaWeapon`을 처음부터 같이 붙여두고 항상 하나만 활성화한다. `WeaponBase.Update()`를 `protected virtual`로 바꿔 우산이 "쿨타임마다 한 번 공격" 대신 "매 프레임 회전 + 적별 재타격 타이머" 루프를 가질 수 있게 한다. 보상 적용(스탯 버프 vs 무기 교체)은 `RoyalWasabiController`에서 `LevelSystem`으로 옮겨, `RoyalWasabiController`는 순수 연출만 맡는다.

**Tech Stack:** Unity 2022.3.62f3, Built-in 2D, NUnit EditMode 테스트. 기존 관례: 판정/좌표 로직은 `XxxLogic` 정적 클래스 + EditMode 테스트, MonoBehaviour는 컴파일+회귀 확인만(전용 테스트 없음).

**Spec:** `docs/superpowers/specs/2026-09-26-royal-wasabi-umbrella-design.md`

## Global Constraints

- `main` 직접 커밋·푸시·병합 절대 금지. 모든 태스크는 새 브랜치에서 시작하고 PR로 올린다. 병합 버튼은 사람만 누른다.
- `git add .` 금지 — 파일을 명시하고 `.meta`를 항상 같이 커밋한다.
- 우산 아트는 새로 안 만든다 — 기존 `계란 공격-Sheet_0`(계란 공격 시트의 첫 프레임, 접힌 우산 모양) 스프라이트를 그대로 쓴다.
- 우산 밸런스 시작값(전부 인스펙터 노출, 플레이테스트로 조정): 궤도 반경 1.6, 재타격 간격 0.3초, 1타 피해 8(양산 Lv1과 동일), 회전 속도 초당 1바�이(360°/초), 개수는 Lv1~2 4개·Lv3~4 5개.
- 씬(`.unity`)·프리팹(`.prefab`) 편집은 사람이 Unity Editor GUI로 한다. 에이전트는 스크립트로 새 데이터 에셋(ScriptableObject `.asset`)만 생성한다.

### 배치 테스트 실행 명령 (이 문서에서 "테스트 실행"이라 부르는 것)

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요\unity"
"C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" -batchmode -projectPath "$(pwd -W)" -runTests -testPlatform EditMode -testResults "$(pwd -W)/TestResults.xml" -logFile - > "$TEMP/unity_test.log" 2>&1
grep -E "error CS" "$TEMP/unity_test.log" | head -20
grep -c "HandleProjectAlreadyOpenInAnotherInstance" "$TEMP/unity_test.log"
head -c 400 TestResults.xml
```

성공 기준: `error CS` 없음, `HandleProjectAlreadyOpenInAnotherInstance` 카운트 0(에디터가 열려있으면 사용자에게 닫아달라고 요청), `result="Passed"`, `failed="0"`. 끝나면 `rm -f TestResults.xml`. **현재 `main` 기준 EditMode 테스트 개수가 baseline이다** — 실행 전에 `git log --oneline origin/main -1`로 최신 여부를 확인하고, 이 문서의 기대 개수는 그 baseline에 더해진 값으로 해석한다(협업자 커밋이 더 들어와 있으면 "실패 0 + 이번 태스크가 추가한 만큼만 증가"로 판단).

### 이미 존재하는(이번 계획이 그대로 쓰는) 타입

- `SushiSurvival.Weapons.WeaponBase` (`Weapons/WeaponBase.cs`) — `CurrentLevel`, `WeaponName`, `CanLevelUp`, `TryGetNextLevelStats(out WeaponLevelStats, out WeaponLevelStats)`, `LevelUp()`, `protected WeaponLevelStats BaseStats`, `protected float Damage`, `protected float Range`
- `SushiSurvival.Weapons.CooldownLogic.ApplyAttackSpeed(float baseCooldown, float attackSpeedMultiplier, float minCooldown)` (`Weapons/CooldownLogic.cs`)
- `SushiSurvival.Data.WeaponLevelStats { float damage; float cooldown; float range; float angleDegrees; int pierceCount; }` (`Data/WeaponData.cs`)
- `SushiSurvival.Core.StatType.AttackDamage/AttackSpeed/AttackRange` (`Core/StatSystem.cs`)
- `SushiSurvival.Enemies.EnemyBase.TakeDamage(float damage, Vector3 sourcePosition)` (`Enemies/EnemyBase.cs`)
- `SushiSurvival.Core.AffinityBuffLogic.GetBuffAmount(float maxCap, float ratio)` / `AffinityBuffApplier.Apply(AugmentData, float, PlayerStats, PlayerHealth)` — 왕궁 와사비 스탯 버프가 지금 쓰는 것, `LevelSystem`으로 옮겨서 그대로 재사용

---

## 브랜치 / PR 분할

| PR | 브랜치 | 태스크 | 시작 시점 | 의존 |
|---|---|---|---|---|
| **A** | `feature/weapon-resolver-and-base-changes` | Task 1 (무기 해석 헬퍼 + `WeaponBase` 확장) | `main`에서 지금 | 없음 |
| **B** | `feature/umbrella-orbit-logic` | Task 2 (궤도 좌표 순수 로직) | `main`에서 지금 (A와 병렬) | 없음 |
| **C** | `feature/rotating-umbrella-weapon` | Task 3 (`RotatingUmbrellaWeapon`) | **A·B 병합 후** | A의 `WeaponBase` 변경, B의 `UmbrellaOrbitLogic` |
| **D** | `feature/umbrella-upgrade-description` | Task 4 (카드 설명 문구 분기) | **C 병합 후** | C의 `RotatingUmbrellaWeapon` 타입 |
| **E** | `feature/wasabi-boss-scene-carryover` | Task 5 (보스 씬 이월) | **A·C 병합 후** (D와 병렬) | A의 헬퍼, C의 타입 |
| **F** | `feature/wasabi-weapon-conversion-reward` | Task 6 (보상 적용 흐름 분리) | **A·C 병합 후** (D·E와 병렬) | A의 `SetLevel`, C의 타입 |
| **G** | `feature/egg-umbrella-weapon-data` | Task 7 (우산 무기 수치 에셋) | `main`에서 지금 (전부와 병렬) | 없음 |
| **H** | `feature/umbrella-prefab-wiring` | Task 8 (프리팹·씬 배선, 사용자 에디터 작업) | **D·E·F·G 모두 병합 후** | 전부 |

D, E, F는 서로 파일이 안 겹쳐 C만 병합되면 동시에 진행할 수 있다(D는 `Core/UpgradeDescriptionLogic.cs`+`Core/WeaponLevelUpOption.cs`, E는 `UI/RunResultCarrier.cs`+`Core/GameManager.cs`+`Core/BossFightDirector.cs`, F는 `Core/RoyalWasabiController.cs`+`Core/LevelSystem.cs`). G는 코드 의존이 전혀 없어 아무 때나 진행 가능.

---

## File Structure

| 파일 | 역할 |
|---|---|
| `unity/Assets/_Project/Scripts/Weapons/PlayerWeaponResolver.cs` (신규) | 플레이어 오브젝트에서 현재 "켜진" 무기 하나를 찾는다(무기 두 개가 공존할 때 모호함 해소) |
| `unity/Assets/_Project/Scripts/Weapons/WeaponBase.cs` (수정) | `Update()`·`StatMultiplier()`를 `protected virtual`/`protected`로, `SetLevel(int)` 추가 |
| `unity/Assets/_Project/Scripts/Weapons/UmbrellaOrbitLogic.cs` (신규) | 우산 개수·인덱스로부터 각도·좌표를 계산하는 순수 함수 |
| `unity/Assets/_Project/Scripts/Weapons/RotatingUmbrellaWeapon.cs` (신규) | 회전 우산 무기 본체 — 매 프레임 회전, 적별 재타격 타이머 |
| `unity/Assets/_Project/Scripts/Core/UpgradeDescriptionLogic.cs` (수정) | `DescribeWeaponUpgrade`에 `isUmbrella` 분기 추가 |
| `unity/Assets/_Project/Scripts/Core/WeaponLevelUpOption.cs` (수정) | 무기가 우산이면 `isUmbrella: true`로 넘김 |
| `unity/Assets/_Project/Scripts/UI/RunResultCarrier.cs` (수정) | `WasabiWeaponConverted` bool 필드 추가 |
| `unity/Assets/_Project/Scripts/Core/GameManager.cs` (수정) | `GetComponent<WeaponBase>()`→`PlayerWeaponResolver`, `EnterBossFight()`에서 변환 플래그 기록 |
| `unity/Assets/_Project/Scripts/Core/BossFightDirector.cs` (수정) | `GetComponent<WeaponBase>()`→`PlayerWeaponResolver`, 스폰 직후 변환 플래그 있으면 우산 켜기 |
| `unity/Assets/_Project/Scripts/Core/RoyalWasabiController.cs` (수정) | 연출 전담으로 축소 — `Show(Sprite, Action onSuccess, Action onComplete)` |
| `unity/Assets/_Project/Scripts/Core/LevelSystem.cs` (수정) | `HandleRoyalWasabiRequested()`가 무기 타입으로 분기, `ConvertToUmbrella()`/`ApplyRoyalWasabiStatBuffs()` 신설 |
| `unity/Assets/_Project/Data/EggUmbrellaWeaponData.asset` (신규) | 우산 전용 `WeaponData`(Lv1~4 수치) |
| `unity/Assets/Tests/EditMode/UmbrellaOrbitLogicTests.cs` (신규) | Task 2 검증 |
| `unity/Assets/Tests/EditMode/UpgradeDescriptionLogicTests.cs` (수정) | Task 4 검증 — 기존 파일에 케이스 추가 |
| `unity/Assets/_Project/Prefabs/EggPlayer.prefab` (수정, PR H) | `RotatingUmbrellaWeapon` 컴포넌트 + 우산 자식 오브젝트 5개 추가 |
| `unity/Assets/_Project/Scenes/GameScene.unity` (수정, PR H) | `LevelSystem`에 증강 4종 필드 재배선(`RoyalWasabiController`에서 옮겨감) |

---

# PR A — 무기 해석 헬퍼 + `WeaponBase` 확장 (`main`에서 지금 시작)

### Task 1: `PlayerWeaponResolver` 신설 + `WeaponBase` 확장

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git checkout -b feature/weapon-resolver-and-base-changes
```

**Files:**
- Create: `unity/Assets/_Project/Scripts/Weapons/PlayerWeaponResolver.cs`
- Modify: `unity/Assets/_Project/Scripts/Weapons/WeaponBase.cs`
- Modify: `unity/Assets/_Project/Scripts/Core/GameManager.cs`
- Modify: `unity/Assets/_Project/Scripts/Core/BossFightDirector.cs`

**Interfaces:**
- Produces (Task 3·5·6이 사용):
  - `SushiSurvival.Weapons.PlayerWeaponResolver.GetActive(GameObject player)` → `WeaponBase`(없으면 `null`)
  - `WeaponBase.SetLevel(int level)` — 1~4로 클램프해서 직접 대입
  - `WeaponBase.Update()`가 `protected virtual`(전에는 `private`) — Task 3에서 override
  - `WeaponBase.StatMultiplier(StatType)`가 `protected`(전에는 `private`) — Task 3에서 회전속도 계산에 씀

무기 두 개(`EggFanWeapon`+`RotatingUmbrellaWeapon`)가 같은 오브젝트에 공존하게 되므로, 기존
`player.GetComponent<WeaponBase>()`는 어느 쪽이 잡힐지 불명확해진다. `GetComponents<WeaponBase>()`
중 `enabled`인 것만 고르는 헬퍼로 교체한다. MonoBehaviour·정적 헬퍼라 기존 관례대로 전용 테스트는
없다(컴파일+회귀로 검증) — `GetActive`는 한 줄짜리 LINQ라 별도 테스트를 둘 만큼 복잡하지 않다.

- [ ] **Step 1: `PlayerWeaponResolver.cs` 작성**

`unity/Assets/_Project/Scripts/Weapons/PlayerWeaponResolver.cs`:

```csharp
using System.Linq;
using UnityEngine;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 플레이어 오브젝트에 WeaponBase 파생 컴포넌트가 여러 개(예: 계란 양산 +
    /// 회전 우산) 있을 수 있어서 GetComponent&lt;WeaponBase&gt;()만으로는 어느 쪽이
    /// 잡힐지 불명확하다. 항상 정확히 하나만 enabled라는 전제 하에 그것만 고른다.
    /// </summary>
    public static class PlayerWeaponResolver
    {
        public static WeaponBase GetActive(GameObject player)
            => player == null ? null : player.GetComponents<WeaponBase>().FirstOrDefault(w => w.enabled);
    }
}
```

- [ ] **Step 2: `WeaponBase.cs` 수정**

`unity/Assets/_Project/Scripts/Weapons/WeaponBase.cs`의 `StatMultiplier`/`Update`/`LevelUp` 부분을:

```csharp
        public void LevelUp()
        {
            if (!CanLevelUp) return;
            currentLevel++;
        }

        /// <summary>레벨을 직접 지정한다(승계용). 1~weaponData.levels.Length로 클램프한다.</summary>
        public void SetLevel(int level)
        {
            if (weaponData == null) return;
            currentLevel = Mathf.Clamp(level, 1, weaponData.levels.Length);
        }

        protected float StatMultiplier(StatType stat)
            => playerStats != null ? playerStats.GetValue(stat) : 1f;

        protected virtual void Update()
        {
            _cooldown.Tick(Time.deltaTime);
            if (!_cooldown.IsReady) return;

            attackAnimator?.TriggerAttack();
            Attack();

            float cooldown = CooldownLogic.ApplyAttackSpeed(
                BaseStats.cooldown, StatMultiplier(StatType.AttackSpeed), minCooldown);
            _cooldown.Reset(cooldown);
        }
```

(`private float StatMultiplier` → `protected float StatMultiplier`, `private void Update` →
`protected virtual void Update`, `SetLevel` 신규 추가. 나머지 파일은 그대로.)

- [ ] **Step 3: `GameManager.cs`의 `GetComponent<WeaponBase>()` 두 곳 교체**

`unity/Assets/_Project/Scripts/Core/GameManager.cs:186`:
```csharp
            var weapon = PlayerWeaponResolver.GetActive(player);
```
(기존: `var weapon = player.GetComponent<WeaponBase>();`)

`unity/Assets/_Project/Scripts/Core/GameManager.cs:325`:
```csharp
                var weapon = PlayerWeaponResolver.GetActive(_playerTransform.gameObject);
```
(기존: `var weapon = _playerTransform.GetComponent<WeaponBase>();`)

- [ ] **Step 4: `BossFightDirector.cs`의 `GetComponent<WeaponBase>()` 한 곳 교체**

`unity/Assets/_Project/Scripts/Core/BossFightDirector.cs:96`:
```csharp
            var weapon = PlayerWeaponResolver.GetActive(playerObj);
```
(기존: `var weapon = playerObj.GetComponent<WeaponBase>();`)

- [ ] **Step 5: 컴파일 + 회귀 확인**

"배치 테스트 실행 명령"을 실행한다. Expected: `error CS` 없음, `failed="0"`, baseline과 같은
통과 개수(새 테스트 없음 — 지금은 무기가 하나뿐이라 `GetActive`도 기존 `GetComponent`와 동일하게
동작해야 한다). 끝나면 `rm -f TestResults.xml`.

- [ ] **Step 6: 커밋, 푸시, PR A**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/Weapons/PlayerWeaponResolver.cs unity/Assets/_Project/Scripts/Weapons/PlayerWeaponResolver.cs.meta unity/Assets/_Project/Scripts/Weapons/WeaponBase.cs unity/Assets/_Project/Scripts/Core/GameManager.cs unity/Assets/_Project/Scripts/Core/BossFightDirector.cs
git commit -m "$(cat <<'EOF'
feat: 무기 다중 컴포넌트 공존을 위한 해석 헬퍼 + WeaponBase 확장

PlayerWeaponResolver.GetActive()가 enabled인 WeaponBase만 고른다.
WeaponBase.Update()/StatMultiplier()를 protected virtual/protected로,
SetLevel(int)을 추가해 회전 우산(다음 PR)이 자기만의 루프와 레벨
승계를 가질 수 있게 한다. 지금은 무기가 하나뿐이라 동작 변화 없음.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/weapon-resolver-and-base-changes
gh pr create --title "feat: 무기 해석 헬퍼 + WeaponBase 확장 (와사비 우산 A)" --body "$(cat <<'EOF'
## 요약
아델린이 와사비로 회전 우산을 받으면 EggFanWeapon과 RotatingUmbrellaWeapon이 같은 오브젝트에 공존하게 됩니다(항상 하나만 enabled). 기존 `GetComponent<WeaponBase>()`는 이때 어느 쪽이 잡힐지 불명확해서, enabled인 것만 고르는 `PlayerWeaponResolver`로 3곳(GameManager 2곳, BossFightDirector 1곳)을 교체했습니다. `WeaponBase`에는 다음 PR(회전 우산 본체)이 쓸 `SetLevel`과 `protected virtual Update`도 미리 추가했습니다. 지금은 무기가 하나뿐이라 게임 동작은 바뀌지 않습니다.

- 스펙: `docs/superpowers/specs/2026-09-26-royal-wasabi-umbrella-design.md`
- 계획: `docs/superpowers/plans/2026-09-26-royal-wasabi-umbrella.md` (PR A)

## 테스트
- EditMode 전체 통과 (컴파일 확인, 회귀 없음 — 신규 테스트 없음)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

**A를 올린 뒤 멈추지 않고 곧바로 Task 2(B), Task 7(G)를 각각 `main`에서 새로 판다.**

---

# PR B — 궤도 좌표 순수 로직 (`main`에서 지금, A와 병렬)

### Task 2: `UmbrellaOrbitLogic`

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git checkout -b feature/umbrella-orbit-logic
```

**Files:**
- Create: `unity/Assets/_Project/Scripts/Weapons/UmbrellaOrbitLogic.cs`
- Test: `unity/Assets/Tests/EditMode/UmbrellaOrbitLogicTests.cs`

**Interfaces:**
- Produces (Task 3이 사용):
  - `UmbrellaOrbitLogic.AngleStepDegrees(int count)` → `float`
  - `UmbrellaOrbitLogic.AngleForIndex(float baseAngleDegrees, int index, int count)` → `float`
  - `UmbrellaOrbitLogic.PositionForAngle(float angleDegrees, float radius)` → `Vector2`

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/UmbrellaOrbitLogicTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Weapons;

namespace SushiSurvival.EditModeTests
{
    public class UmbrellaOrbitLogicTests
    {
        [Test]
        public void AngleStepDegrees_DividesFullCircleEvenly()
        {
            Assert.AreEqual(90f, UmbrellaOrbitLogic.AngleStepDegrees(4), 0.001f);
            Assert.AreEqual(72f, UmbrellaOrbitLogic.AngleStepDegrees(5), 0.001f);
        }

        [Test]
        public void AngleStepDegrees_ZeroCount_ReturnsZero()
        {
            Assert.AreEqual(0f, UmbrellaOrbitLogic.AngleStepDegrees(0), 0.001f);
        }

        [Test]
        public void AngleForIndex_SpreadsEvenlyFromBaseAngle()
        {
            Assert.AreEqual(0f, UmbrellaOrbitLogic.AngleForIndex(0f, 0, 4), 0.001f);
            Assert.AreEqual(90f, UmbrellaOrbitLogic.AngleForIndex(0f, 1, 4), 0.001f);
            Assert.AreEqual(270f, UmbrellaOrbitLogic.AngleForIndex(90f, 2, 4), 0.001f);
        }

        [Test]
        public void PositionForAngle_ZeroDegrees_IsAlongPositiveX()
        {
            Vector2 pos = UmbrellaOrbitLogic.PositionForAngle(0f, 2f);
            Assert.AreEqual(2f, pos.x, 0.001f);
            Assert.AreEqual(0f, pos.y, 0.001f);
        }

        [Test]
        public void PositionForAngle_NinetyDegrees_IsAlongPositiveY()
        {
            Vector2 pos = UmbrellaOrbitLogic.PositionForAngle(90f, 2f);
            Assert.AreEqual(0f, pos.x, 0.001f);
            Assert.AreEqual(2f, pos.y, 0.001f);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해 실패 확인**

"배치 테스트 실행 명령"을 실행한다. Expected: `error CS` 없음(테스트 코드 자체는 컴파일되지만
`UmbrellaOrbitLogic` 타입이 없어 컴파일 에러가 난다 — 이게 정상이다. Step 3에서 타입을 만들면 해결).

- [ ] **Step 3: 구현**

`unity/Assets/_Project/Scripts/Weapons/UmbrellaOrbitLogic.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Weapons
{
    /// <summary>회전 우산이 균등한 각도로 캐릭터를 도는 좌표를 계산하는 순수 함수.</summary>
    public static class UmbrellaOrbitLogic
    {
        public static float AngleStepDegrees(int count) => count > 0 ? 360f / count : 0f;

        public static float AngleForIndex(float baseAngleDegrees, int index, int count)
            => baseAngleDegrees + AngleStepDegrees(count) * index;

        public static Vector2 PositionForAngle(float angleDegrees, float radius)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
        }
    }
}
```

- [ ] **Step 4: 테스트 실행해 통과 확인**

"배치 테스트 실행 명령"을 실행한다. Expected: `error CS` 없음, baseline + 5(새 테스트), `failed="0"`.
끝나면 `rm -f TestResults.xml`.

- [ ] **Step 5: 커밋, 푸시, PR B**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/Weapons/UmbrellaOrbitLogic.cs unity/Assets/_Project/Scripts/Weapons/UmbrellaOrbitLogic.cs.meta unity/Assets/Tests/EditMode/UmbrellaOrbitLogicTests.cs unity/Assets/Tests/EditMode/UmbrellaOrbitLogicTests.cs.meta
git commit -m "$(cat <<'EOF'
feat: 회전 우산 궤도 좌표 계산 순수 로직(UmbrellaOrbitLogic)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/umbrella-orbit-logic
gh pr create --title "feat: 우산 궤도 좌표 로직 (와사비 우산 B)" --body "$(cat <<'EOF'
## 요약
회전 우산 개수·인덱스로부터 균등 분배 각도와 좌표를 계산하는 순수 함수. 다음 PR(회전 우산 본체)이 이걸로 자식 오브젝트들을 매 프레임 배치합니다. 아직 아무도 안 불러서 게임 동작은 바뀌지 않습니다.

- 스펙: `docs/superpowers/specs/2026-09-26-royal-wasabi-umbrella-design.md`
- 계획: `docs/superpowers/plans/2026-09-26-royal-wasabi-umbrella.md` (PR B)

## 테스트
- EditMode baseline + 5개 통과

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

---

# PR C — `RotatingUmbrellaWeapon` (A·B 병합 후)

### Task 3: 회전 우산 무기 본체

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git log --oneline -4   # A, B 병합 커밋이 보여야 한다
git checkout -b feature/rotating-umbrella-weapon
```

**Files:**
- Create: `unity/Assets/_Project/Scripts/Weapons/RotatingUmbrellaWeapon.cs`

**Interfaces:**
- Consumes (A): `WeaponBase.SetLevel(int)`, `protected virtual Update()`, `protected StatMultiplier(StatType)`,
  `protected BaseStats`/`Damage`/`Range`
- Consumes (B): `UmbrellaOrbitLogic.AngleForIndex(float,int,int)`, `UmbrellaOrbitLogic.PositionForAngle(float,float)`
- Consumes (기존): `SushiSurvival.Weapons.CooldownLogic.ApplyAttackSpeed`, `SushiSurvival.Enemies.EnemyBase.TakeDamage`
- Produces (Task 4·6·8이 사용):
  - `RotatingUmbrellaWeapon`(공개 클래스, `WeaponBase` 파생) — `Task 6`이 `GetComponent<RotatingUmbrellaWeapon>()`로 찾고 `SetLevel`/`enabled`를 씀
  - 직렬화 필드: `umbrellas`(`Transform[]`, 5칸), `umbrellaCountByLevel`(`int[]`, 기본 `{4,4,5,5}`),
    `rotationSpeedDegreesPerSecond`(`float`, 기본 360), `hitRadius`(`float`, 기본 0.3), `enemyLayer`(`LayerMask`)
    — Task 8이 프리팹에서 이 필드들을 연결

MonoBehaviour라 기존 관례대로 전용 테스트는 없다(컴파일+회귀로 검증). 좌표·타이머 계산의 핵심
로직은 이미 Task 2에서 테스트했다.

- [ ] **Step 1: `RotatingUmbrellaWeapon.cs` 작성**

`unity/Assets/_Project/Scripts/Weapons/RotatingUmbrellaWeapon.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Enemies;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 와사비 알현 성공 시 계란 양산을 대체하는 무기. 쿨타임마다 한 번 공격하는
    /// WeaponBase 기본 루프 대신, 매 프레임 계속 회전하며 스치는 적마다 개별
    /// 재타격 타이머(cooldown 필드 재해석)로 데미지를 준다.
    /// </summary>
    public class RotatingUmbrellaWeapon : WeaponBase
    {
        [Tooltip("궤도를 도는 우산 그림 오브젝트. 최대 5개, 레벨별로 앞에서부터 필요한 개수만 켠다.")]
        [SerializeField] private Transform[] umbrellas;
        [Tooltip("레벨(1~4)별 우산 개수. 기획서 시작값: Lv1~2 4개, Lv3~4 5개.")]
        [SerializeField] private int[] umbrellaCountByLevel = { 4, 4, 5, 5 };
        [Tooltip("공격속도 배율 1.0 기준 회전 속도(초당 도).")]
        [SerializeField] private float rotationSpeedDegreesPerSecond = 360f;
        [Tooltip("우산 하나가 적을 스쳤다고 판정하는 반경.")]
        [SerializeField] private float hitRadius = 0.3f;
        [SerializeField] private LayerMask enemyLayer;

        private float _currentAngle;
        // 풀링된 적이 죽고 짧은 시간 안에 같은 자리에서 재사용되면 이전 생의
        // 마지막 타격 시각이 남아 있어 첫 타격이 한 번 씹힐 수 있다 — 재타격
        // 간격(기본 0.3초)이 짧아 실전 영향은 미미해서 지금은 정리하지 않는다.
        private readonly Dictionary<EnemyBase, float> _lastHitTime = new Dictionary<EnemyBase, float>();

        protected override void Update()
        {
            if (weaponData == null || umbrellas == null || umbrellas.Length == 0) return;

            float rotationSpeed = rotationSpeedDegreesPerSecond * StatMultiplier(StatType.AttackSpeed);
            _currentAngle += rotationSpeed * Time.deltaTime;

            int count = UmbrellaCountForLevel();
            float radius = Range;

            for (int i = 0; i < umbrellas.Length; i++)
            {
                bool active = i < count;
                umbrellas[i].gameObject.SetActive(active);
                if (!active) continue;

                float angle = UmbrellaOrbitLogic.AngleForIndex(_currentAngle, i, count);
                umbrellas[i].localPosition = UmbrellaOrbitLogic.PositionForAngle(angle, radius);
                umbrellas[i].localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            float reHitInterval = CooldownLogic.ApplyAttackSpeed(
                BaseStats.cooldown, StatMultiplier(StatType.AttackSpeed), minCooldown);
            CheckHits(count, reHitInterval);
        }

        // 우산은 쿨타임마다 한 번 쏘는 무기가 아니라 이 메서드는 쓰이지 않는다
        // (Update()를 통째로 오버라이드해서 base.Attack() 호출 경로 자체가 없다).
        protected override void Attack() { }

        private int UmbrellaCountForLevel()
        {
            if (umbrellaCountByLevel == null || umbrellaCountByLevel.Length == 0)
                return umbrellas.Length;

            int index = Mathf.Clamp(currentLevel - 1, 0, umbrellaCountByLevel.Length - 1);
            return Mathf.Min(umbrellaCountByLevel[index], umbrellas.Length);
        }

        private void CheckHits(int count, float reHitInterval)
        {
            for (int i = 0; i < count; i++)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(umbrellas[i].position, hitRadius, enemyLayer);
                foreach (var hit in hits)
                {
                    if (!hit.TryGetComponent<EnemyBase>(out var enemy)) continue;

                    _lastHitTime.TryGetValue(enemy, out float last);
                    if (Time.time - last < reHitInterval) continue;

                    enemy.TakeDamage(Damage, transform.position);
                    _lastHitTime[enemy] = Time.time;
                }
            }
        }
    }
}
```

- [ ] **Step 2: 컴파일 + 회귀 확인**

"배치 테스트 실행 명령"을 실행한다. Expected: `error CS` 없음, `failed="0"`, baseline과 같은 개수
(MonoBehaviour 신규라 새 테스트 없음). 끝나면 `rm -f TestResults.xml`. 새 `.meta` 확인:

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git status --short unity/Assets/_Project/Scripts/Weapons/RotatingUmbrellaWeapon.cs unity/Assets/_Project/Scripts/Weapons/RotatingUmbrellaWeapon.cs.meta
```

- [ ] **Step 3: 커밋, 푸시, PR C**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/Weapons/RotatingUmbrellaWeapon.cs unity/Assets/_Project/Scripts/Weapons/RotatingUmbrellaWeapon.cs.meta
git commit -m "$(cat <<'EOF'
feat: 회전 우산 무기 RotatingUmbrellaWeapon 신규

매 프레임 회전하며 스치는 적마다 개별 재타격 타이머로 데미지를 준다.
WeaponBase를 상속해 공격력/공격속도/공격범위 증강을 그대로 물려받는다.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/rotating-umbrella-weapon
gh pr create --title "feat: RotatingUmbrellaWeapon 신규 (와사비 우산 C)" --body "$(cat <<'EOF'
## 요약
와사비 알현 성공 시 아델린 무기를 대체할 회전 우산 본체. WeaponBase를 상속하므로 기존 공격력/공격속도/공격범위 증강을 코드 변경 없이 그대로 적용받습니다(스탯 시스템은 안 건드림). 아직 아무 프리팹에도 안 붙어 있고 아무도 활성화하지 않아 게임 동작은 바뀌지 않습니다.

- 스펙: `docs/superpowers/specs/2026-09-26-royal-wasabi-umbrella-design.md`
- 계획: `docs/superpowers/plans/2026-09-26-royal-wasabi-umbrella.md` (PR C)
- 선행 PR: A(무기 해석 헬퍼), B(궤도 좌표 로직)

## 테스트
- EditMode baseline 유지 (컴파일 확인, MonoBehaviour라 신규 테스트 없음)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

**C를 올린 뒤 멈추지 않고 곧바로 Task 4(D), Task 5(E), Task 6(F)를 각각 `main`에서 새로 판다.**

---

# PR D — 카드 설명 문구 분기 (C 병합 후)

### Task 4: `UpgradeDescriptionLogic`/`WeaponLevelUpOption`에 우산 라벨 추가

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git log --oneline -3   # C 병합 커밋이 보여야 한다
git checkout -b feature/umbrella-upgrade-description
```

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Core/UpgradeDescriptionLogic.cs`
- Modify: `unity/Assets/_Project/Scripts/Core/WeaponLevelUpOption.cs`
- Test: `unity/Assets/Tests/EditMode/UpgradeDescriptionLogicTests.cs` (기존 파일에 케이스 추가)

**Interfaces:**
- Consumes (C): `SushiSurvival.Weapons.RotatingUmbrellaWeapon` (타입 체크용)
- Consumes (기존): `WeaponLevelStats`
- 기존 `DescribeWeaponUpgrade(WeaponLevelStats, WeaponLevelStats)` 2-인자 호출부는 그대로 컴파일된다
  (새 인자는 기본값 `false`).

- [ ] **Step 1: 실패하는 테스트 추가**

`unity/Assets/Tests/EditMode/UpgradeDescriptionLogicTests.cs`의 `DescribeWeaponUpgrade_NothingChanged_IsEmpty`
테스트 바로 뒤에 추가:

```csharp
        [Test]
        public void DescribeWeaponUpgrade_Umbrella_UsesUmbrellaLabels()
        {
            var text = UpgradeDescriptionLogic.DescribeWeaponUpgrade(
                Stats(8, 0.3f, 1.6f, 0, 0), Stats(10, 0.28f, 1.7f, 0, 0), isUmbrella: true);

            Assert.AreEqual("우산 피해 8 → 10\n재타격 간격 0.3 → 0.28초\n궤도 반경 1.6 → 1.7", text);
        }
```

- [ ] **Step 2: 테스트 실행해 실패 확인**

"배치 테스트 실행 명령"을 실행한다. Expected: `error CS` — `DescribeWeaponUpgrade`에 3번째 인자
`isUmbrella`가 없어 컴파일 에러(정상, Step 3에서 해결).

- [ ] **Step 3: `UpgradeDescriptionLogic.DescribeWeaponUpgrade` 수정**

`unity/Assets/_Project/Scripts/Core/UpgradeDescriptionLogic.cs`의 `DescribeWeaponUpgrade` 메서드를:

```csharp
        public static string DescribeWeaponUpgrade(WeaponLevelStats current, WeaponLevelStats next, bool isUmbrella = false)
        {
            var lines = new List<string>();

            string damageLabel = isUmbrella ? "우산 피해" : "공격력";
            string cooldownLabel = isUmbrella ? "재타격 간격" : "쿨타임";
            string rangeLabel = isUmbrella ? "궤도 반경" : "범위";

            if (Changed(current.damage, next.damage))
                lines.Add($"{damageLabel} {Number(current.damage)} → {Number(next.damage)}");

            if (Changed(current.cooldown, next.cooldown))
                lines.Add($"{cooldownLabel} {Number(current.cooldown)} → {Number(next.cooldown)}초");

            if (Changed(current.range, next.range) && next.range > 0f)
                lines.Add($"{rangeLabel} {Number(current.range)} → {Number(next.range)}");

            if (Changed(current.angleDegrees, next.angleDegrees) && next.angleDegrees > 0f)
                lines.Add($"각도 {Number(current.angleDegrees)}° → {Number(next.angleDegrees)}°");

            if (current.pierceCount != next.pierceCount)
                lines.Add($"관통 {current.pierceCount} → {next.pierceCount}");

            if (lines.Count > MaxWeaponLines)
                lines.RemoveRange(MaxWeaponLines, lines.Count - MaxWeaponLines);

            return string.Join("\n", lines);
        }
```

(라벨 3개만 변수로 빼고 나머지는 그대로 — `angleDegrees`/`pierceCount`는 우산 수치를 전부 0으로
두므로 실제로는 안 나온다.)

- [ ] **Step 4: 테스트 실행해 통과 확인**

"배치 테스트 실행 명령"을 실행한다. Expected: `error CS` 없음, baseline + 1, `failed="0"`.
끝나면 `rm -f TestResults.xml`.

- [ ] **Step 5: `WeaponLevelUpOption.cs`에서 우산이면 `isUmbrella: true` 전달**

`unity/Assets/_Project/Scripts/Core/WeaponLevelUpOption.cs`의 `Description` 프로퍼티를:

```csharp
        public string Description =>
            _weapon.TryGetNextLevelStats(out var current, out var next)
                ? UpgradeDescriptionLogic.DescribeWeaponUpgrade(current, next, _weapon is SushiSurvival.Weapons.RotatingUmbrellaWeapon)
                : string.Empty;
```

- [ ] **Step 6: 컴파일 + 회귀 확인**

"배치 테스트 실행 명령"을 다시 실행한다(Step 4와 같은 기대치 — `WeaponLevelUpOption`은
MonoBehaviour가 아니라 순수 클래스지만 이미 Task 4의 로직 테스트로 간접 커버되고, 이 자체를
호출하려면 씬 컨텍스트가 필요해 전용 테스트는 안 둔다). Expected: `error CS` 없음, `failed="0"`.

- [ ] **Step 7: 커밋, 푸시, PR D**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/Core/UpgradeDescriptionLogic.cs unity/Assets/_Project/Scripts/Core/WeaponLevelUpOption.cs unity/Assets/Tests/EditMode/UpgradeDescriptionLogicTests.cs
git commit -m "$(cat <<'EOF'
feat: 무기 강화 카드 설명이 회전 우산일 때 우산 전용 라벨을 쓰도록 분기

공격력/쿨타임/범위 라벨을 우산 피해/재타격 간격/궤도 반경으로 바꾼다.
기존 호출부(2-인자)는 그대로 동작한다(기본값 false).

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/umbrella-upgrade-description
gh pr create --title "feat: 우산 강화 카드 라벨 분기 (와사비 우산 D)" --body "$(cat <<'EOF'
## 요약
회전 우산으로 전환된 뒤 무기 강화 카드가 "공격력/쿨타임/범위" 대신 "우산 피해/재타격 간격/궤도 반경"으로 표시되도록 `UpgradeDescriptionLogic.DescribeWeaponUpgrade`에 `isUmbrella` 분기를 추가했습니다. 기존 2-인자 호출부는 그대로 컴파일됩니다.

- 스펙: `docs/superpowers/specs/2026-09-26-royal-wasabi-umbrella-design.md`
- 계획: `docs/superpowers/plans/2026-09-26-royal-wasabi-umbrella.md` (PR D)
- 선행 PR: A, B, C

## 테스트
- EditMode baseline + 1 통과

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

---

# PR E — 보스 씬 이월 (A·C 병합 후, D·F와 병렬)

### Task 5: `RunResultCarrier`/`GameManager`/`BossFightDirector`에 변환 상태 이월

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git checkout -b feature/wasabi-boss-scene-carryover
```

(D가 아직 병합 전이어도 `main` 기준이라 문제없다 — D와 파일이 안 겹친다.)

**Files:**
- Modify: `unity/Assets/_Project/Scripts/UI/RunResultCarrier.cs`
- Modify: `unity/Assets/_Project/Scripts/Core/GameManager.cs`
- Modify: `unity/Assets/_Project/Scripts/Core/BossFightDirector.cs`

**Interfaces:**
- Consumes (A): `PlayerWeaponResolver.GetActive`
- Consumes (C): `SushiSurvival.Weapons.RotatingUmbrellaWeapon` (타입 체크·`GetComponent`용)
- Produces: `RunResultCarrier.WasabiWeaponConverted`(`bool`) — PR H의 씬 배선 검증 때 참고

MonoBehaviour·정적 필드라 전용 테스트는 없다(컴파일+회귀).

- [ ] **Step 1: `RunResultCarrier.cs`에 필드 추가**

`unity/Assets/_Project/Scripts/UI/RunResultCarrier.cs`의 `WeaponLevel` 필드 바로 아래에 추가:

```csharp
        public static int WeaponLevel;
        [Tooltip("와사비 알현 성공으로 아델린 무기가 회전 우산으로 바뀐 상태인지.")]
        public static bool WasabiWeaponConverted;
```

(`RunResultCarrier`는 `static class`라 `[Tooltip]`은 실제로 인스펙터에 안 뜨지만, 기존 파일
관례상 다른 필드들도 주석 없이 나열돼 있으니 짧은 `//` 주석으로 대체해도 된다: 중요한 건 필드
자체다.)

- [ ] **Step 2: `GameManager.EnterBossFight()`에서 플래그 기록**

`unity/Assets/_Project/Scripts/Core/GameManager.cs:323-328`(`if (_playerTransform != null)` 블록)을:

```csharp
            if (_playerTransform != null)
            {
                var weapon = PlayerWeaponResolver.GetActive(_playerTransform.gameObject);
                if (weapon != null)
                {
                    RunResultCarrier.WeaponLevel = weapon.CurrentLevel;
                    RunResultCarrier.WasabiWeaponConverted = weapon is SushiSurvival.Weapons.RotatingUmbrellaWeapon;
                }
            }
```

(이 블록은 PR A의 Step 3에서 이미 `PlayerWeaponResolver.GetActive`로 바뀌어 있다 — 여기서는
`WasabiWeaponConverted` 대입 한 줄만 추가한다.)

- [ ] **Step 3: `BossFightDirector.cs`에서 스폰 직후 변환 적용**

`unity/Assets/_Project/Scripts/Core/BossFightDirector.cs:127-132`(무기 레벨 복원 블록)를:

```csharp
            // 무기 강화 레벨도 같은 이유로 복원한다. 와사비로 우산으로 바뀐
            // 상태였다면 먼저 우산을 켜야 레벨 복원 대상이 우산이 된다.
            if (weapon != null)
            {
                if (RunResultCarrier.WasabiWeaponConverted && weapon is EggFanWeapon eggWeapon)
                {
                    var umbrella = eggWeapon.GetComponent<RotatingUmbrellaWeapon>();
                    if (umbrella != null)
                    {
                        eggWeapon.enabled = false;
                        umbrella.enabled = true;
                        weapon = umbrella;
                    }
                    else
                    {
                        Debug.LogError($"{eggWeapon.name}: RotatingUmbrellaWeapon 컴포넌트가 없어 우산 상태를 복원할 수 없습니다.");
                    }
                }

                while (weapon.CurrentLevel < RunResultCarrier.WeaponLevel && weapon.CanLevelUp)
                    weapon.LevelUp();
            }
```

파일 상단 `using` 목록에 `using SushiSurvival.Weapons;`가 이미 있는지 확인한다(있다 — `WeaponBase`를
이미 쓰고 있으므로).

- [ ] **Step 4: 컴파일 + 회귀 확인**

"배치 테스트 실행 명령"을 실행한다. Expected: `error CS` 없음, `failed="0"`, baseline과 같은 개수.
끝나면 `rm -f TestResults.xml`.

- [ ] **Step 5: 커밋, 푸시, PR E**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/UI/RunResultCarrier.cs unity/Assets/_Project/Scripts/Core/GameManager.cs unity/Assets/_Project/Scripts/Core/BossFightDirector.cs
git commit -m "$(cat <<'EOF'
feat: 와사비 우산 전환 상태를 GameScene→BossScene으로 이월

RunResultCarrier.WasabiWeaponConverted를 EnterBossFight()에서 기록하고,
BossFightDirector가 스폰 직후 이 플래그를 보고 우산을 켠 뒤 기존 무기
레벨 복원 루프를 그대로 돌린다.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/wasabi-boss-scene-carryover
gh pr create --title "feat: 와사비 우산 전환 보스씬 이월 (와사비 우산 E)" --body "$(cat <<'EOF'
## 요약
GameScene에서 와사비로 회전 우산으로 바뀐 상태가 BossScene 재스폰 시에도 유지되도록, `RunResultCarrier.WasabiWeaponConverted` 플래그를 추가하고 `BossFightDirector`가 스폰 직후 이걸 보고 우산을 켭니다. 지금은 이 플래그를 세팅하는 쪽(무기 전환 자체, PR F)이 아직 없어서 항상 false — 게임 동작은 안 바뀝니다.

- 스펙: `docs/superpowers/specs/2026-09-26-royal-wasabi-umbrella-design.md`
- 계획: `docs/superpowers/plans/2026-09-26-royal-wasabi-umbrella.md` (PR E)
- 선행 PR: A(무기 해석 헬퍼), C(RotatingUmbrellaWeapon 타입) — D·F와는 독립이라 병렬로 올립니다.

## 테스트
- EditMode baseline 유지 (컴파일 확인, MonoBehaviour라 신규 테스트 없음)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

---

# PR F — 보상 적용 흐름 분리 (A·C 병합 후, D·E와 병렬)

### Task 6: `RoyalWasabiController` 연출 전담화 + `LevelSystem` 보상 분기

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git checkout -b feature/wasabi-weapon-conversion-reward
```

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Core/RoyalWasabiController.cs`
- Modify: `unity/Assets/_Project/Scripts/Core/LevelSystem.cs`

**Interfaces:**
- Consumes (A): `WeaponBase.SetLevel(int)`
- Consumes (C): `SushiSurvival.Weapons.RotatingUmbrellaWeapon`
- Consumes (기존): `SushiSurvival.Weapons.EggFanWeapon`, `AffinityBuffLogic.GetBuffAmount`, `AffinityBuffApplier.Apply`
- Produces (Task 8이 씬에서 재배선): `LevelSystem`에 새 직렬화 필드
  `attackDamageAugment`/`attackSpeedAugment`/`moveSpeedAugment`/`maxHealthAugment`/`royalWasabiBuffRatio`
  (`RoyalWasabiController`에서 옮겨옴 — **PR H에서 씬의 인스펙터 연결을 RoyalWasabiController에서
  LevelSystem으로 다시 해야 한다**)
- `RoyalWasabiController.Show`의 새 시그니처: `Show(Sprite portrait, Action onSuccess, Action onComplete)`
  (기존 `Show(PlayerStats, PlayerHealth, Sprite, Action<AugmentData,float>, Action)`에서 변경 — 호출부는
  `LevelSystem` 한 곳뿐이라 이 PR 안에서 같이 고친다)

MonoBehaviour라 전용 테스트는 없다(컴파일+회귀).

- [ ] **Step 1: `RoyalWasabiController.cs` 축소**

`unity/Assets/_Project/Scripts/Core/RoyalWasabiController.cs` 전체를:

```csharp
using System;
using UnityEngine;
using SushiSurvival.UI;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 레벨업 카드 대신 고르는 도박의 연출(대사→가위바위보→결과 공개)만 맡는다.
    /// 성공/완료 시 무엇을 할지는 모른다 — 캐릭터마다 보상이 달라서(아델린은
    /// 무기 교체, 그 외는 스탯 버프) 호출자(LevelSystem)가 콜백으로 정한다.
    /// </summary>
    public class RoyalWasabiController : MonoBehaviour
    {
        [SerializeField] private RockPaperScissorsPanel rpsPanel;
        [SerializeField] private RoyalWasabiPanel panel;

        public void Show(Sprite portrait, Action onSuccess, Action onComplete)
        {
            if (panel == null || rpsPanel == null)
            {
                Debug.LogError($"{name}: panel 또는 rpsPanel이 비어 있어 왕궁 연출을 표시할 수 없습니다.");
                onComplete?.Invoke();
                return;
            }

            // "와사비를 받으러 왔습니다" 대사를 먼저 보여준 뒤에야 가위바위보로
            // 넘어간다 — 결과 확인용 패널을 도입부 연출로도 재사용한다.
            panel.ShowFlavor(portrait, () =>
            {
                // panel.Hide()가 아니라 HideDialogueBox() — 왕궁 배경은
                // 가위바위보 도중에도 계속 보여야 한다.
                panel.HideDialogueBox();

                rpsPanel.Show(success =>
                {
                    rpsPanel.Hide();

                    if (success)
                        onSuccess?.Invoke();

                    panel.ShowResult(success, portrait, () =>
                    {
                        panel.Hide();
                        onComplete?.Invoke();
                    });
                });
            });
        }
    }
}
```

- [ ] **Step 2: `LevelSystem.cs`에 보상 분기 추가**

`unity/Assets/_Project/Scripts/Core/LevelSystem.cs` 상단 `using` 목록에 `using System;` 추가.

`[SerializeField] private RoyalWasabiController royalWasabiController;` 바로 아래에 필드 추가:

```csharp
        [SerializeField] private RoyalWasabiController royalWasabiController;
        [Tooltip("아델린이 아닌 캐릭터의 와사비 성공 보상(스탯 버프) 대상 증강 4종.")]
        [SerializeField] private AugmentData attackDamageAugment;
        [SerializeField] private AugmentData attackSpeedAugment;
        [SerializeField] private AugmentData moveSpeedAugment;
        [SerializeField] private AugmentData maxHealthAugment;
        [Range(0f, 1f)]
        [Tooltip("성공 시 각 증강 maxCap의 이 비율만큼 강화한다(스탯 버프 보상 캐릭터 전용).")]
        [SerializeField] private float royalWasabiBuffRatio = 0.5f;
```

`HandleRoyalWasabiRequested()`를:

```csharp
        private void HandleRoyalWasabiRequested()
        {
            panel.Hide();

            if (royalWasabiController == null)
            {
                Debug.LogError($"{name}: royalWasabiController가 비어 있어 도박을 진행할 수 없습니다.");
                ShowNext();
                return;
            }

            Action onSuccess = _weapon is Weapons.EggFanWeapon
                ? (Action)ConvertToUmbrella
                : ApplyRoyalWasabiStatBuffs;

            royalWasabiController.Show(_portrait, onSuccess, ShowNext);
        }

        /// <summary>아델린 전용 보상 — 계란 양산을 회전 우산으로 바꾼다.</summary>
        private void ConvertToUmbrella()
        {
            if (_weapon is not Weapons.EggFanWeapon eggWeapon) return;

            var umbrella = eggWeapon.GetComponent<Weapons.RotatingUmbrellaWeapon>();
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

        /// <summary>아델린 외 캐릭터의 기존 보상 — 스탯 4종 강화.</summary>
        private void ApplyRoyalWasabiStatBuffs()
        {
            ApplyRoyalWasabiAugment(attackDamageAugment);
            ApplyRoyalWasabiAugment(attackSpeedAugment);
            ApplyRoyalWasabiAugment(moveSpeedAugment);
            ApplyRoyalWasabiAugment(maxHealthAugment);
        }

        private void ApplyRoyalWasabiAugment(AugmentData augment)
        {
            if (augment == null)
            {
                Debug.LogError($"{name}: 왕궁 와사비 증강 필드 하나가 비어 있어 그 스탯은 강화되지 않습니다.");
                return;
            }

            float amount = AffinityBuffLogic.GetBuffAmount(augment.maxCap, royalWasabiBuffRatio);
            AffinityBuffApplier.Apply(augment, amount, _playerStats, _playerHealth);
            RecordExternalBuff(augment, amount);
        }
```

(`_weapon`은 `WeaponBase` 타입 private 필드로 이미 있다 — 클래스 전체를 고칠 필요 없이 이
메서드들만 바뀐다. `Weapons.EggFanWeapon`/`Weapons.RotatingUmbrellaWeapon`처럼 네임스페이스를
붙인 건 `LevelSystem.cs`가 이미 `using SushiSurvival.Weapons;`를 갖고 있어 `EggFanWeapon`/
`RotatingUmbrellaWeapon`로 짧게 써도 된다 — 접두어 없이 써도 무방하다.)

- [ ] **Step 3: 컴파일 + 회귀 확인**

"배치 테스트 실행 명령"을 실행한다. Expected: `error CS` 없음, `failed="0"`, baseline과 같은 개수.
끝나면 `rm -f TestResults.xml`.

- [ ] **Step 4: 커밋, 푸시, PR F**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Scripts/Core/RoyalWasabiController.cs unity/Assets/_Project/Scripts/Core/LevelSystem.cs
git commit -m "$(cat <<'EOF'
feat: 와사비 보상 적용을 RoyalWasabiController에서 LevelSystem으로 분리

RoyalWasabiController는 연출(대사→가위바위보→결과)만 맡고, 성공 시
효과는 LevelSystem이 무기 타입으로 분기한다 — 아델린(EggFanWeapon)은
회전 우산으로 전환, 그 외는 기존 스탯 4종 버프.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/wasabi-weapon-conversion-reward
gh pr create --title "feat: 와사비 보상 캐릭터별 분기 (와사비 우산 F)" --body "$(cat <<'EOF'
## 요약
`RoyalWasabiController`를 연출 전담으로 줄이고, 성공 시 보상(스탯 버프 vs 무기 교체)은 `LevelSystem.HandleRoyalWasabiRequested()`가 현재 무기 타입(`EggFanWeapon`인지)으로 분기하도록 옮겼습니다. 스탯 버프 대상 증강 4종 필드도 `RoyalWasabiController`에서 `LevelSystem`으로 옮겨졌습니다 — **씬의 인스펙터 연결을 다시 해야 합니다(PR H에서 처리)**, 지금 이 PR만으로는 필드가 비어 있어 스탯 버프 보상(카마리온 등)이 에러 로그만 남기고 조용히 아무 효과 없이 넘어갑니다.

- 스펙: `docs/superpowers/specs/2026-09-26-royal-wasabi-umbrella-design.md`
- 계획: `docs/superpowers/plans/2026-09-26-royal-wasabi-umbrella.md` (PR F)
- 선행 PR: A(SetLevel), C(RotatingUmbrellaWeapon 타입) — D·E와는 독립이라 병렬로 올립니다.

## 테스트
- EditMode baseline 유지 (컴파일 확인, MonoBehaviour라 신규 테스트 없음)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

---

# PR G — 우산 무기 수치 에셋 (`main`에서 지금, 전부와 병렬)

### Task 7: `EggUmbrellaWeaponData.asset` 생성 (TDD)

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git checkout -b feature/egg-umbrella-weapon-data
```

**Files:**
- Create: `unity/Assets/Tests/EditMode/EggUmbrellaWeaponDataAssetTests.cs`
- Create: `unity/Assets/_Project/Data/EggUmbrellaWeaponData.asset` (임시 스크립트로 생성)

**Interfaces:**
- Consumes: 기존 `WeaponData`/`WeaponLevelStats` (변경 없음)
- Produces (Task 8이 사용): 에셋 경로 `Assets/_Project/Data/EggUmbrellaWeaponData.asset`

코드 의존이 전혀 없어 아무 때나 시작 가능. 스펙 5번 섹션의 시작값을 그대로 4레벨에 채운다 —
정확한 상승 곡선은 미정이라, Lv1을 시작값으로 두고 Lv2~4는 계란 양산의 레벨별 상승 비율을 참고해
완만하게 늘리는 정도로 채운다(플레이테스트로 다시 조정될 값이라 정밀할 필요 없음).

- [ ] **Step 1: 실패하는 무결성 테스트 작성**

`unity/Assets/Tests/EditMode/EggUmbrellaWeaponDataAssetTests.cs`:

```csharp
using NUnit.Framework;
using UnityEditor;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    /// <summary>우산 WeaponData 에셋이 4레벨 다 채워져 있고 스펙 시작값과 맞는지 확인한다.</summary>
    public class EggUmbrellaWeaponDataAssetTests
    {
        private const string AssetPath = "Assets/_Project/Data/EggUmbrellaWeaponData.asset";

        private static WeaponData Load()
        {
            var data = AssetDatabase.LoadAssetAtPath<WeaponData>(AssetPath);
            Assert.IsNotNull(data, $"{AssetPath}를 불러올 수 없습니다.");
            return data;
        }

        [Test]
        public void HasFourLevels()
        {
            Assert.AreEqual(4, Load().levels.Length);
        }

        [Test]
        public void Level1_MatchesSpecStartingValues()
        {
            var lv1 = Load().levels[0];
            Assert.AreEqual(8f, lv1.damage, 0.01f);
            Assert.AreEqual(0.3f, lv1.cooldown, 0.01f);
            Assert.AreEqual(1.6f, lv1.range, 0.01f);
        }

        [Test]
        public void AngleAndPierceAreUnusedForUmbrella()
        {
            foreach (var level in Load().levels)
            {
                Assert.AreEqual(0f, level.angleDegrees, 0.01f);
                Assert.AreEqual(0, level.pierceCount);
            }
        }

        [Test]
        public void DamageCooldownRange_ImproveEachLevel()
        {
            var levels = Load().levels;
            for (int i = 1; i < levels.Length; i++)
            {
                Assert.Greater(levels[i].damage, levels[i - 1].damage, $"Lv{i + 1} 피해가 Lv{i}보다 커야 합니다.");
                Assert.Less(levels[i].cooldown, levels[i - 1].cooldown, $"Lv{i + 1} 재타격 간격이 Lv{i}보다 짧아야 합니다.");
                Assert.GreaterOrEqual(levels[i].range, levels[i - 1].range, $"Lv{i + 1} 궤도 반경이 Lv{i}보다 같거나 커야 합니다.");
            }
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해 실패 확인**

"배치 테스트 실행 명령"을 실행한다. Expected: 컴파일은 통과하고 새 4개 테스트가
`... EggUmbrellaWeaponData.asset를 불러올 수 없습니다`로 실패(`failed="4"`). 기존은 통과.

- [ ] **Step 3: 에셋 생성 스크립트 작성·실행**

`WeaponData.cs.meta`의 GUID를 참조해 에셋 YAML을 만든다. 일회용 스크립트라 저장소에 커밋하지
않고 스크래치패드에 둔다.

`gen_egg_umbrella_weapon_data.js`(스크래치패드 디렉터리에 작성):

```javascript
const fs = require('fs');
const root = 'C:/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity/Assets/_Project';

const scriptMeta = fs.readFileSync(`${root}/Scripts/Data/WeaponData.cs.meta`, 'utf8');
const scriptGuid = scriptMeta.match(/guid:\s*([0-9a-f]{32})/)[1];

// Lv1~4: damage 8→10→12→14, cooldown 0.3→0.27→0.24→0.2, range 1.6→1.7→1.8→1.9
const levels = [
  { damage: 8,  cooldown: 0.3,  range: 1.6 },
  { damage: 10, cooldown: 0.27, range: 1.7 },
  { damage: 12, cooldown: 0.24, range: 1.8 },
  { damage: 14, cooldown: 0.2,  range: 1.9 },
];

const levelYaml = levels.map(l => `  - damage: ${l.damage}
    cooldown: ${l.cooldown}
    range: ${l.range}
    angleDegrees: 0
    pierceCount: 0`).join('\n');

const y = `%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${scriptGuid}, type: 3}
  m_Name: EggUmbrellaWeaponData
  m_EditorClassIdentifier: 
  weaponName: \xEC\x9A\xB0\xEC\x82\xB0
  isMelee: 1
  projectilePrefab: {fileID: 0}
  levels:
${levelYaml}
`;

fs.writeFileSync(`${root}/Data/EggUmbrellaWeaponData.asset`, y);
console.log('wrote EggUmbrellaWeaponData.asset, script guid', scriptGuid);
```

(`weaponName`의 `\xEC\x9A\xB0\xEC\x82\xB0`는 "우산"의 UTF-8 바이트를 그대로 문자열 리터럴에 박아
넣은 것 — 이전 세션에서 확립한 패턴대로, JS 파일 자체의 소스 인코딩 문제를 피하려면 유니코드
이스케이프(`\uXXXX`)를 쓰는 게 더 안전하다: `'\uC6B0\uC0B0'`로 대체해도 된다.)

실행:

```bash
node <스크래치패드 경로>/gen_egg_umbrella_weapon_data.js
```

Expected: `wrote EggUmbrellaWeaponData.asset, script guid <32자리>`

- [ ] **Step 4: 테스트 실행해 통과 확인**

"배치 테스트 실행 명령"을 실행한다. Expected: `error CS` 없음, baseline + 4, `failed="0"`.
끝나면 `rm -f TestResults.xml`. `.meta` 확인:

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git status --short unity/Assets/_Project/Data/EggUmbrellaWeaponData.asset unity/Assets/_Project/Data/EggUmbrellaWeaponData.asset.meta unity/Assets/Tests/EditMode/EggUmbrellaWeaponDataAssetTests.cs unity/Assets/Tests/EditMode/EggUmbrellaWeaponDataAssetTests.cs.meta
```

**실패 시 대처:** 에셋 로드 자체가 실패하면 스크립트를 고치지 말고, 사용자가 에디터에서
`Create → SushiSurvival → Weapon Data`로 에셋을 만들어 이름을 `EggUmbrellaWeaponData`로,
`Levels` 배열 Size 4로 위 표의 수치를 그대로 입력한다(테스트는 그대로 검증해 준다).

- [ ] **Step 5: 커밋, 푸시, PR G**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Data/EggUmbrellaWeaponData.asset unity/Assets/_Project/Data/EggUmbrellaWeaponData.asset.meta unity/Assets/Tests/EditMode/EggUmbrellaWeaponDataAssetTests.cs unity/Assets/Tests/EditMode/EggUmbrellaWeaponDataAssetTests.cs.meta
git commit -m "$(cat <<'EOF'
feat: 회전 우산 전용 WeaponData 에셋(EggUmbrellaWeaponData) 추가

스펙 시작값(Lv1: 피해8/재타격0.3초/반경1.6)을 기준으로 Lv2~4를
완만하게 늘렸다. 플레이테스트로 조정될 값이라 정밀하지 않다.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/egg-umbrella-weapon-data
gh pr create --title "feat: 우산 WeaponData 에셋 (와사비 우산 G)" --body "$(cat <<'EOF'
## 요약
회전 우산 전용 `WeaponData` 에셋. 스펙 5번 섹션의 시작값을 Lv1으로 두고 Lv2~4는 완만하게 늘렸습니다 — 전부 인스펙터에서 나중에 플레이테스트로 조정 가능합니다. 아직 아무 프리팹도 이 에셋을 참조하지 않아 게임 동작은 바뀌지 않습니다.

- 스펙: `docs/superpowers/specs/2026-09-26-royal-wasabi-umbrella-design.md`
- 계획: `docs/superpowers/plans/2026-09-26-royal-wasabi-umbrella.md` (PR G)

## 테스트
- EditMode baseline + 4 통과

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
git checkout main
```

---

# PR H — 프리팹·씬 배선 (D·E·F·G 모두 병합 후, 사용자 에디터 작업)

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git checkout main && git fetch origin && git merge origin/main --ff-only
git log --oneline -8   # D, E, F, G 병합 커밋이 다 보여야 한다
git checkout -b feature/umbrella-prefab-wiring
```

### Task 8: `EggPlayer` 프리팹에 우산 붙이기 + 씬 재배선 (사용자 에디터 작업)

에이전트는 프리팹/씬 Inspector 값을 대신 편집하지 않는다(프로젝트 관례). 아래 순서를 사용자가
따라 하고, 에이전트는 저장된 파일을 읽어 배선을 검증한다.

#### A. `EggPlayer` 프리팹에 회전 우산 붙이기

**1.** Project 창에서 `Assets/_Project/Prefabs/EggPlayer`를 더블클릭해 프리팹 편집 모드로 들어간다.

**2.** 루트 `EggPlayer` 오브젝트(이미 `EggFanWeapon` 등 컴포넌트가 붙어 있는 그 오브젝트) 선택 →
   **Add Component → Rotating Umbrella Weapon**.

**3.** 우산 궤도용 자식 오브젝트 5개를 만든다. `EggPlayer` 우클릭 → **Create Empty** → 이름
   `Umbrella_0` — 여기에 **Add Component → Sprite Renderer** 추가하고, Sprite에
   `Assets/Art/캐릭터/캐릭터/계란초밥 시트/계란 공격-Sheet`의 `_0` 서브스프라이트(접힌 우산
   모양 프레임)를 연결한다. **처음엔 비활성 상태로 둔다**(Inspector 좌측 위 체크박스 해제).
   같은 방식으로 `Umbrella_1`~`Umbrella_4`까지 4개 더 복제(Ctrl+D 후 이름만 바꾸면 스프라이트도
   같이 복제된다).

**4.** `Rotating Umbrella Weapon` 컴포넌트 필드 연결:
   - **Weapon Data** = `Assets/_Project/Data/EggUmbrellaWeaponData`
   - **Attack Animator** = 비워둠(우산은 트리거 애니메이션을 안 씀)
   - **Player Stats** = 같은 오브젝트의 `Player Stats` 컴포넌트
   - **Umbrellas** = Size 5, `Umbrella_0`~`Umbrella_4`를 순서대로
   - **Umbrella Count By Level** = 기본값 `{4,4,5,5}` 그대로(이미 코드 기본값)
   - **Rotation Speed Degrees Per Second** = 기본값 360 그대로
   - **Hit Radius** = 기본값 0.3 그대로
   - **Enemy Layer** = `EggFanWeapon`의 Enemy Layer와 동일하게(같은 레이어 선택)

**5.** `Rotating Umbrella Weapon` 컴포넌트를 **비활성화**(Inspector 좌측 위 체크박스 해제) —
   평소엔 꺼져 있고 와사비 성공 시 코드가 켠다.

**6.** 프리팹 저장(Ctrl+S), 프리팹 편집 모드 나가기.

#### B. `GameScene`에서 `LevelSystem`에 증강 4종 재배선

`RoyalWasabiController`에 있던 필드들이 `LevelSystem`으로 옮겨졌다.

**1.** `GameScene`을 연다. Hierarchy에서 `RoyalWasabiController` 컴포넌트가 붙은 오브젝트를 찾는다
   (`LevelSystem`의 `Royal Wasabi Controller` 필드가 가리키는 오브젝트를 클릭하면 빠르다).

**2.** 그 오브젝트의 Inspector에서 **지금 `Royal Wasabi Controller` 컴포넌트에 연결돼 있던**
   `Attack Damage Augment`/`Attack Speed Augment`/`Move Speed Augment`/`Max Health Augment`가
   무엇인지 각각 이름을 적어둔다(예: `AttackDamageAugment`, `AttackSpeedAugment` 등 — 스크립트가
   바뀌면서 이 필드들이 `Royal Wasabi Controller`에서는 사라져 있을 것이다).

**3.** `LevelSystem` 컴포넌트가 붙은 오브젝트를 선택 → 새로 생긴 `Attack Damage Augment`/
   `Attack Speed Augment`/`Move Speed Augment`/`Max Health Augment`/`Royal Wasabi Buff Ratio`
   필드에 2번에서 적어둔 것과 같은 에셋을 연결(비율은 기존과 같은 0.5로).

**4.** 씬 저장(Ctrl+S).

---

### 에이전트 검증

저장 후 다음을 확인한다:

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요\unity"
git status --short Assets/_Project/Prefabs/EggPlayer.prefab Assets/_Project/Scenes/GameScene.unity
echo "--- EggPlayer 프리팹: RotatingUmbrellaWeapon 컴포넌트 존재 + Umbrellas 5개 연결 ---"
grep -n "RotatingUmbrellaWeapon\|m_enabled: 0" Assets/_Project/Prefabs/EggPlayer.prefab | head -20
echo "--- GameScene: LevelSystem 증강 필드 확인 ---"
grep -n "attackDamageAugment:\|attackSpeedAugment:\|moveSpeedAugment:\|maxHealthAugment:\|royalWasabiBuffRatio:" Assets/_Project/Scenes/GameScene.unity
```

Expected: `EggPlayer.prefab`/`GameScene.unity`가 변경으로 뜸. `RotatingUmbrellaWeapon` 컴포넌트가
프리팹에 있고 비활성 상태. `LevelSystem` 쪽 증강 필드 4개가 전부 `{fileID: 0}`이 아니어야 한다.
비어 있는 필드가 있으면 사용자에게 알려준다.

- [ ] **Step 1: 컴파일 + 회귀 확인**

"배치 테스트 실행 명령"을 실행한다(프리팹/씬만 바뀌어서 컴파일엔 영향 없지만 습관대로 확인).
Expected: `error CS` 없음, `failed="0"`.

- [ ] **Step 2: Play 모드 전체 흐름 확인 (사용자)**

`GameScene`을 Play해서 아델린으로 시작 → 레벨업 팝업에서 "와사비 하사받기" 선택 →

- [ ] 가위바위보 성공 시: 계란 양산 공격이 사라지고 우산 4개가 아델린 주위를 돌기 시작. 잡몹을
      스치면 데미지가 들어감(체력 줄어드는 것 확인). 다음 무기 강화 카드가 "우산 피해/재타격
      간격/궤도 반경"으로 표시되는지 확인
- [ ] 무기 강화 카드를 몇 번 뽑아서 Lv3~4까지 올려 우산이 5개로 늘어나는지 확인
- [ ] 가위바위보 실패 시: 아무 변화 없이(계란 양산 그대로) 다음 레벨업으로 넘어가는지 확인
- [ ] 우산 상태로 5:00까지 진행해 보스전으로 넘어갔을 때도 우산이 유지되고 레벨도 그대로인지
      확인(테스트용으로 `bossSpawnTime`을 잠깐 낮춰도 된다 — 확인 후 300으로 복구)
- [ ] 카마리온으로 별도 런을 시작해 와사비 성공 시 기존처럼 스탯 4종이 오르는지(우산으로 안
      바뀌는지) 확인

- [ ] **Step 3: 커밋, 푸시, PR H**

```bash
cd "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요"
git add unity/Assets/_Project/Prefabs/EggPlayer.prefab unity/Assets/_Project/Scenes/GameScene.unity
git status --short
git commit -m "$(cat <<'EOF'
feat: 아델린 프리팹에 회전 우산 + LevelSystem 증강 재배선

EggPlayer 프리팹에 RotatingUmbrellaWeapon 컴포넌트와 우산 자식
오브젝트 5개(비활성 시작)를 추가. RoyalWasabiController에서
LevelSystem으로 옮겨간 스탯 버프 증강 4종 필드를 GameScene에서
다시 연결.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
git push -u origin feature/umbrella-prefab-wiring
gh pr create --title "feat: 와사비 회전 우산 프리팹·씬 배선 (와사비 우산 H)" --body "$(cat <<'EOF'
## 요약
와사비 알현 보상 교체(아델린 회전 우산)의 마지막 단계. `EggPlayer` 프리팹에 `RotatingUmbrellaWeapon` + 우산 자식 오브젝트 5개를 추가하고, `RoyalWasabiController`에서 `LevelSystem`으로 옮겨간 스탯 버프 증강 4종 필드를 `GameScene`에서 재배선했습니다.

- 스펙: `docs/superpowers/specs/2026-09-26-royal-wasabi-umbrella-design.md`
- 계획: `docs/superpowers/plans/2026-09-26-royal-wasabi-umbrella.md` (PR H)
- 선행 PR: A~G 전부 병합됨

## 테스트
- EditMode baseline 유지
- Play 모드로 아델린 와사비 성공(우산 전환·강화 카드 라벨·5개로 증가)/실패, 보스씬 이월, 카마리온 기존 보상 유지 확인 완료(사용자)

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

**여기서 멈추고 사용자가 PR H를 병합하길 기다린다.** 병합 후 `main` 동기화, 브랜치 정리,
메모리(`sushi-survival-project.md`) 업데이트로 마무리한다.
