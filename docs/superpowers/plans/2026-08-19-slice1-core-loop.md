# 슬라이스 1: 코어 루프 (계란 캐릭터) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 계란 캐릭터 1종 + 양산 무기 + 잡몹 1종만으로 "이동 → 자동공격 → 몹 처치 →
XP 젬 획득 → 피격 → 사망"까지 이어지는 코어 루프를 실제로 플레이 가능한 상태로
만든다.

**Architecture:** ScriptableObject 데이터(무기/캐릭터/몬스터 수치) + 순수 C# 로직
클래스(테스트 가능) + 그 위에 얇게 얹는 MonoBehaviour 글루 코드. 순수 로직(스탯
계산, 방향 계산, 체력 계산, 부채꼴 판정, 스폰 위치 계산, 픽업 판정)은 EditMode
유닛 테스트로 TDD하고, MonoBehaviour 통합 동작(이동/애니메이션/충돌/Play 모드
전체 흐름)은 Unity Editor에서 수동 플레이테스트로 검증한다.

**Tech Stack:** Unity 2022.3.62f3 LTS, Built-in 2D 렌더 파이프라인, Input System
패키지, Unity Test Framework(EditMode, NUnit)

**Spec:** [docs/superpowers/specs/2026-08-19-core-architecture-design.md](../specs/2026-08-19-core-architecture-design.md)

## Global Constraints

- Unity 버전: 2022.3.62f3 (LTS) 고정. 다른 버전으로 프로젝트를 열지 않는다.
- 입력: 새 Input System 패키지만 사용한다(레거시 Input Manager 사용 금지).
- 렌더 파이프라인: Built-in (URP 패키지를 추가하지 않는다).
- 무기/캐릭터/몬스터 수치는 코드에 하드코딩하지 않고 ScriptableObject 에셋
  필드에 입력한다.
- 계란 양산 Lv1 수치(고정값, CLAUDE.md 기준): 데미지 8, 반경 2.0, 부채꼴 120°,
  쿨타임 1.2초.
- 잡몹 수치(고정값): 체력 12, 접촉 데미지 5.
- 프로젝트 루트: `unity/` (이 스펙 폴더의 부모 폴더 하위). Unity 프로젝트 자체가
  이 서브폴더.
- 스크립트는 전부 `unity/Assets/_Project/Scripts/` 하위, 단일 어셈블리
  `SushiSurvival.Runtime`에 속한다.
- 이 플랜은 슬라이스 1 범위만 다룬다 — 레벨업 팝업, 다른 캐릭터 2종, 중형몹/보스,
  웨이브 타임라인, 증강 실제 적용, 호감도 대화, 결과 화면은 포함하지 않는다.

---

### Task 1: Unity 프로젝트 생성 및 뼈대 세팅

**Files:**
- Create: `unity/` (Unity 프로젝트 루트 전체)
- Create: `unity/Assets/_Project/Scripts/SushiSurvival.Runtime.asmdef`
- Create: `unity/Assets/Tests/EditMode/SushiSurvival.EditModeTests.asmdef`
- Move: `캐릭터/` → `unity/Assets/Art/캐릭터/`
- Move: `환경/` → `unity/Assets/Art/환경/`

**Interfaces:**
- Produces: 이후 모든 Task가 사용할 폴더 구조, 어셈블리 정의 2개, 원본 아트 에셋의
  새 경로.

- [ ] **Step 1: Unity 프로젝트 생성 (배치 모드)**

Bash에서 실행 (경로에 공백이 있으므로 따옴표 필수):

```bash
"/c/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" \
  -batchmode -nographics \
  -createProject "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity" \
  -quit
```

완료 후 `unity/Assets`, `unity/Packages`, `unity/ProjectSettings` 폴더가 생겼는지
확인:

```bash
ls "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity"
```

- [ ] **Step 2: 폴더 구조 생성**

```bash
cd "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요"
mkdir -p unity/Assets/_Project/Scripts/Core \
         unity/Assets/_Project/Scripts/Player \
         unity/Assets/_Project/Scripts/Weapons \
         unity/Assets/_Project/Scripts/Enemies \
         unity/Assets/_Project/Scripts/Pickups \
         unity/Assets/_Project/Scripts/Data \
         unity/Assets/_Project/Scripts/UI \
         unity/Assets/_Project/Data \
         unity/Assets/_Project/Prefabs \
         unity/Assets/_Project/Scenes \
         unity/Assets/Tests/EditMode \
         unity/Assets/Art
```

- [ ] **Step 3: 기존 아트 에셋을 Assets 하위로 이동**

```bash
mv "캐릭터" "unity/Assets/Art/캐릭터"
mv "환경" "unity/Assets/Art/환경"
```

- [ ] **Step 4: 런타임 어셈블리 정의 생성**

`unity/Assets/_Project/Scripts/SushiSurvival.Runtime.asmdef` 파일을 아래 내용으로
작성 (Scripts 폴더 바로 아래에 위치해야 하위 폴더 전체를 커버함):

```json
{
    "name": "SushiSurvival.Runtime",
    "rootNamespace": "",
    "references": [
        "Unity.InputSystem"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 5: EditMode 테스트 어셈블리 정의 생성**

`unity/Assets/Tests/EditMode/SushiSurvival.EditModeTests.asmdef` 파일을 아래
내용으로 작성:

```json
{
    "name": "SushiSurvival.EditModeTests",
    "rootNamespace": "",
    "references": [
        "SushiSurvival.Runtime",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": true,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 6: Unity Editor에서 직접 — 패키지 설치 및 초기 확인**

1. Unity Hub에서 `unity/` 프로젝트를 열어 Editor 실행 (2022.3.62f3로 열림 확인)
2. `Window > Package Manager` → 좌상단 드롭다운에서 "Unity Registry" 선택 →
   검색창에 "Input System" 입력 → Install
3. "Enable Input System Backends?" 팝업에서 **Yes** 선택 (Editor 자동 재시작,
   Active Input Handling이 "Input System Package (New)"로 자동 전환됨)
4. Package Manager에서 "2D Sprite"와 "2D Animation" 패키지도 검색해 설치
   (기존 스프라이트 시트 슬라이싱/애니메이션에 필요)
5. Editor 재시작 후 콘솔에 에러가 없는지 확인 (asmdef 2개가 정상 인식되어야 함 —
   `SushiSurvival.Runtime`, `SushiSurvival.EditModeTests`가 Project 창에
   C# 아이콘 없이 어셈블리 아이콘으로 보이면 정상)

- [ ] **Step 7: 커밋 (git 사용 시에만, 선택)**

git 저장소를 쓰기로 했다면 여기서 초기 커밋. 아니라면 생략하고 다음 Task로.

---

### Task 2: StatSystem 코어 로직 (TDD)

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/StatSystem.cs`
- Test: `unity/Assets/Tests/EditMode/StatSystemTests.cs`

**Interfaces:**
- Produces: `enum StatType`, `enum ModifierType`, `struct StatModifier`,
  `class StatSystem` — `SetBase(StatType, float)`, `SetCap(StatType, float)`,
  `AddModifier(StatModifier)`, `RemoveModifier(StatModifier)`,
  `ClearModifiers()`, `GetValue(StatType) -> float`.
  Task 5(`AugmentData`)가 `StatType`을 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/StatSystemTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class StatSystemTests
    {
        [Test]
        public void GetValue_ReturnsBase_WhenNoModifiers()
        {
            var stats = new StatSystem();
            stats.SetBase(StatType.MoveSpeed, 3f);

            Assert.AreEqual(3f, stats.GetValue(StatType.MoveSpeed));
        }

        [Test]
        public void GetValue_AppliesAdditiveModifier()
        {
            var stats = new StatSystem();
            stats.SetBase(StatType.MaxHealth, 100f);
            stats.AddModifier(new StatModifier { Stat = StatType.MaxHealth, Type = ModifierType.Additive, Value = 20f });

            Assert.AreEqual(120f, stats.GetValue(StatType.MaxHealth));
        }

        [Test]
        public void GetValue_AppliesMultiplicativeModifier()
        {
            var stats = new StatSystem();
            stats.SetBase(StatType.AttackDamage, 10f);
            stats.AddModifier(new StatModifier { Stat = StatType.AttackDamage, Type = ModifierType.Multiplicative, Value = 0.5f });

            Assert.AreEqual(15f, stats.GetValue(StatType.AttackDamage));
        }

        [Test]
        public void GetValue_ClampsToCap()
        {
            var stats = new StatSystem();
            stats.SetBase(StatType.Armor, 0.4f);
            stats.SetCap(StatType.Armor, 0.5f);
            stats.AddModifier(new StatModifier { Stat = StatType.Armor, Type = ModifierType.Additive, Value = 0.5f });

            Assert.AreEqual(0.5f, stats.GetValue(StatType.Armor));
        }

        [Test]
        public void RemoveModifier_StopsApplyingIt()
        {
            var stats = new StatSystem();
            stats.SetBase(StatType.MoveSpeed, 3f);
            var modifier = new StatModifier { Stat = StatType.MoveSpeed, Type = ModifierType.Additive, Value = 2f };
            stats.AddModifier(modifier);
            stats.RemoveModifier(modifier);

            Assert.AreEqual(3f, stats.GetValue(StatType.MoveSpeed));
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인 (컴파일 에러 = 실패)**

```bash
"/c/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" \
  -batchmode -nographics \
  -projectPath "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity" \
  -runTests -testPlatform EditMode \
  -testResults "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity/TestResults-Slice1.xml" \
  -logFile "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity_test.log"
```

**주의:** `-runTests`는 `-quit`과 같이 쓰면 안 된다 — 같이 쓰면 결과 파일이 아예
생성되지 않고 조용히 종료된다(`-runTests`가 테스트 종료 후 알아서 프로세스를
끝낸다). 이후 모든 "Task 2 Step 2와 동일한 명령"은 이 `-quit` 없는 형태를
가리킨다.

Expected: `StatSystem`/`StatType`이 없어서 컴파일 실패 (테스트 자체가 돌지 못함)

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/Core/StatSystem.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace SushiSurvival.Core
{
    public enum StatType
    {
        AttackDamage,
        AttackSpeed,
        AttackRange,
        MaxHealth,
        Armor,
        Regen,
        MoveSpeed,
        MagnetRange,
        ExpGain,
        Revive
    }

    public enum ModifierType
    {
        Additive,
        Multiplicative
    }

    public struct StatModifier
    {
        public StatType Stat;
        public ModifierType Type;
        public float Value;
    }

    /// <summary>
    /// base값 + modifier 목록으로 최종 스탯을 계산하고, 설정된 상한(cap) 안에서
    /// 클램프한다. 증강 10종과 호감도 대화 버프가 이 클래스를 공유해서 스탯을
    /// 한 곳에서만 클램프한다.
    /// </summary>
    public class StatSystem
    {
        private readonly Dictionary<StatType, float> _baseValues = new Dictionary<StatType, float>();
        private readonly Dictionary<StatType, float> _caps = new Dictionary<StatType, float>();
        private readonly List<StatModifier> _modifiers = new List<StatModifier>();

        public void SetBase(StatType stat, float value) => _baseValues[stat] = value;

        public void SetCap(StatType stat, float capValue) => _caps[stat] = capValue;

        public void AddModifier(StatModifier modifier) => _modifiers.Add(modifier);

        public void RemoveModifier(StatModifier modifier) => _modifiers.Remove(modifier);

        public void ClearModifiers() => _modifiers.Clear();

        public float GetValue(StatType stat)
        {
            float baseValue = _baseValues.TryGetValue(stat, out var b) ? b : 0f;
            float additiveSum = 0f;
            float multiplicativeSum = 1f;

            foreach (var modifier in _modifiers)
            {
                if (modifier.Stat != stat) continue;
                if (modifier.Type == ModifierType.Additive) additiveSum += modifier.Value;
                else multiplicativeSum += modifier.Value;
            }

            float result = (baseValue + additiveSum) * multiplicativeSum;

            if (_caps.TryGetValue(stat, out var cap))
                result = Mathf.Min(result, cap);

            return result;
        }
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Step 2와 동일한 명령 재실행.
Expected: `TestResults-Slice1.xml`에서 `StatSystemTests`의 테스트 5개 모두
`result="Passed"`.

- [ ] **Step 5: 커밋 (git 사용 시)**

---

### Task 3: 공용 Facing(방향 유지) 로직

**Files:**
- Create: `unity/Assets/_Project/Scripts/Player/FacingLogic.cs`
- Create: `unity/Assets/_Project/Scripts/Player/FacingController.cs`
- Test: `unity/Assets/Tests/EditMode/FacingLogicTests.cs`

**Interfaces:**
- Consumes: 없음(독립).
- Produces: `FacingLogic.ComputeFacing(Vector2 currentFacing, Vector2 moveInput) -> Vector2`,
  `FacingLogic.IsFacingRight(Vector2 facing) -> bool`,
  `FacingController` (MonoBehaviour) — `Vector2 CurrentFacing { get; }`,
  `void UpdateFacing(Vector2 moveInput)`.
  Task 8(PlayerController)과 Task 9(EggFanWeapon)가 `FacingController`를 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/FacingLogicTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Player;

namespace SushiSurvival.EditModeTests
{
    public class FacingLogicTests
    {
        [Test]
        public void ComputeFacing_ReturnsNormalizedInputDirection_WhenMoving()
        {
            var result = FacingLogic.ComputeFacing(Vector2.down, new Vector2(2f, 0f));

            Assert.AreEqual(Vector2.right, result);
        }

        [Test]
        public void ComputeFacing_KeepsPreviousDirection_WhenInputIsZero()
        {
            var result = FacingLogic.ComputeFacing(Vector2.left, Vector2.zero);

            Assert.AreEqual(Vector2.left, result);
        }

        [Test]
        public void ComputeFacing_NormalizesDiagonalInput()
        {
            var result = FacingLogic.ComputeFacing(Vector2.down, new Vector2(1f, 1f));

            Assert.That(result.magnitude, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void IsFacingRight_TrueForPositiveX()
        {
            Assert.IsTrue(FacingLogic.IsFacingRight(Vector2.right));
        }

        [Test]
        public void IsFacingRight_FalseForNegativeX()
        {
            Assert.IsFalse(FacingLogic.IsFacingRight(Vector2.left));
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Task 2 Step 2와 동일한 배치 모드 명령 실행. Expected: `FacingLogic`이 없어서
컴파일 실패.

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/Player/FacingLogic.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Player
{
    /// <summary>
    /// "이동 중엔 이동 방향, 정지 시 마지막 이동 방향 유지" 로직의 순수 함수 버전.
    /// 계란·간장새우·이나리가 공통으로 사용한다.
    /// </summary>
    public static class FacingLogic
    {
        private const float MinInputSqrMagnitude = 0.0001f;

        public static Vector2 ComputeFacing(Vector2 currentFacing, Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude < MinInputSqrMagnitude)
                return currentFacing;

            return moveInput.normalized;
        }

        public static bool IsFacingRight(Vector2 facing) => facing.x >= 0f;
    }
}
```

`unity/Assets/_Project/Scripts/Player/FacingController.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Player
{
    /// <summary>
    /// FacingLogic을 감싸는 컴포넌트. 계란은 spriteRenderer.flipX만 사용하고,
    /// 간장새우/이나리는 이후 CurrentFacing 벡터를 공격 방향으로 그대로 쓴다.
    /// </summary>
    public class FacingController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public Vector2 CurrentFacing { get; private set; } = Vector2.down;

        public void UpdateFacing(Vector2 moveInput)
        {
            CurrentFacing = FacingLogic.ComputeFacing(CurrentFacing, moveInput);

            if (spriteRenderer != null)
                spriteRenderer.flipX = !FacingLogic.IsFacingRight(CurrentFacing);
        }
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Expected: `FacingLogicTests`의 테스트 5개 모두 Passed.

- [ ] **Step 5: 커밋 (git 사용 시)**

---

### Task 4: 공용 HealthLogic (체력 계산)

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/HealthLogic.cs`
- Test: `unity/Assets/Tests/EditMode/HealthLogicTests.cs`

**Interfaces:**
- Produces: `HealthLogic.ApplyDamage(float currentHealth, float damage) -> float`,
  `HealthLogic.IsDead(float currentHealth) -> bool`.
  Task 8(PlayerHealth), Task 10(EnemyBase)이 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/HealthLogicTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class HealthLogicTests
    {
        [Test]
        public void ApplyDamage_ReducesHealth()
        {
            Assert.AreEqual(70f, HealthLogic.ApplyDamage(100f, 30f));
        }

        [Test]
        public void ApplyDamage_ClampsAtZero()
        {
            Assert.AreEqual(0f, HealthLogic.ApplyDamage(10f, 999f));
        }

        [Test]
        public void ApplyDamage_IgnoresNegativeDamage()
        {
            Assert.AreEqual(100f, HealthLogic.ApplyDamage(100f, -50f));
        }

        [Test]
        public void IsDead_TrueAtZero()
        {
            Assert.IsTrue(HealthLogic.IsDead(0f));
        }

        [Test]
        public void IsDead_FalseAboveZero()
        {
            Assert.IsFalse(HealthLogic.IsDead(1f));
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Task 2 Step 2와 동일한 명령. Expected: `HealthLogic`이 없어서 컴파일 실패.

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/Core/HealthLogic.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Core
{
    public static class HealthLogic
    {
        public static float ApplyDamage(float currentHealth, float damage)
        {
            float safeDamage = Mathf.Max(0f, damage);
            return Mathf.Max(0f, currentHealth - safeDamage);
        }

        public static bool IsDead(float currentHealth) => currentHealth <= 0f;
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Expected: `HealthLogicTests`의 테스트 5개 모두 Passed.

- [ ] **Step 5: 커밋 (git 사용 시)**

---

### Task 5: 데이터(ScriptableObject) 클래스 및 계란/잡몹 에셋

**Files:**
- Create: `unity/Assets/_Project/Scripts/Data/CharacterData.cs`
- Create: `unity/Assets/_Project/Scripts/Data/WeaponData.cs`
- Create: `unity/Assets/_Project/Scripts/Data/MonsterData.cs`
- Create: `unity/Assets/_Project/Scripts/Data/AugmentData.cs`
- Create (Unity Editor에서): `unity/Assets/_Project/Data/EggWeaponData.asset`,
  `unity/Assets/_Project/Data/EggCharacterData.asset`,
  `unity/Assets/_Project/Data/BasicMobData.asset`

**Interfaces:**
- Consumes: `SushiSurvival.Core.StatType` (Task 2).
- Produces: `CharacterData`, `WeaponData`, `WeaponLevelStats`, `MonsterData`,
  `XPGemType`, `AugmentData` 클래스. Task 8이 `CharacterData`를, Task 9가
  `WeaponData`/`WeaponLevelStats`를, Task 10이 `MonsterData`/`XPGemType`을 사용.

- [ ] **Step 1: WeaponData 작성**

`unity/Assets/_Project/Scripts/Data/WeaponData.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Data
{
    [System.Serializable]
    public struct WeaponLevelStats
    {
        public float damage;
        public float cooldown;
        public float range;
        [Tooltip("근접 무기 전용 (부채꼴 전체각, 도)")]
        public float angleDegrees;
        [Tooltip("원거리 무기 전용 (관통 수)")]
        public int pierceCount;
    }

    [CreateAssetMenu(menuName = "SushiSurvival/Weapon Data", fileName = "NewWeaponData")]
    public class WeaponData : ScriptableObject
    {
        public string weaponName;
        public bool isMelee = true;
        [Tooltip("원거리 무기 전용, 근접이면 비워둔다")]
        public GameObject projectilePrefab;
        [Tooltip("인덱스 0 = Lv1 ... 인덱스 3 = Lv4(MAX)")]
        public WeaponLevelStats[] levels = new WeaponLevelStats[4];
    }
}
```

- [ ] **Step 2: CharacterData, MonsterData, AugmentData 작성**

`unity/Assets/_Project/Scripts/Data/CharacterData.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Data
{
    [CreateAssetMenu(menuName = "SushiSurvival/Character Data", fileName = "NewCharacterData")]
    public class CharacterData : ScriptableObject
    {
        public string characterName;
        public Sprite portraitSprite;
        public float baseMoveSpeed = 3f;
        public float baseMaxHealth = 100f;
        public WeaponData weaponData;
        public RuntimeAnimatorController animatorController;
    }
}
```

`unity/Assets/_Project/Scripts/Data/MonsterData.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Data
{
    public enum XPGemType
    {
        Basic,
        Five,
        Ten
    }

    [CreateAssetMenu(menuName = "SushiSurvival/Monster Data", fileName = "NewMonsterData")]
    public class MonsterData : ScriptableObject
    {
        public string monsterName;
        public float maxHealth = 12f;
        public float contactDamage = 5f;
        public float moveSpeed = 2f;
        public XPGemType xpGemDrop = XPGemType.Basic;
        public RuntimeAnimatorController animatorController;
    }
}
```

`unity/Assets/_Project/Scripts/Data/AugmentData.cs`:

```csharp
using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.Data
{
    // 슬라이스 3에서 실제 적용 로직과 연결된다. 슬라이스 1은 스키마만 선반영.
    [CreateAssetMenu(menuName = "SushiSurvival/Augment Data", fileName = "NewAugmentData")]
    public class AugmentData : ScriptableObject
    {
        public string augmentName;
        public StatType statType;
        public float valuePerPick;
        public float maxCap;
    }
}
```

- [ ] **Step 2b: 테스트 실행해서 컴파일 통과 확인**

Task 2 Step 2와 동일한 명령. 이 Task는 순수 데이터 컨테이너라 전용 유닛 테스트는
없지만, 기존 테스트(Task 2~4)가 여전히 전부 Passed로 나오는지 — 즉 컴파일 에러가
없는지 — 확인한다.

- [ ] **Step 3: Unity Editor에서 직접 — 계란/잡몹 데이터 에셋 생성**

1. `Project` 창에서 `Assets/_Project/Data` 폴더로 이동
2. 우클릭 → `Create > SushiSurvival > Weapon Data` → 이름 `EggWeaponData`
   - Inspector에서 `weaponName = "양산"`, `isMelee = true`, `projectilePrefab`은
     비움
   - `levels` 배열 크기를 4로 설정하고 아래 표 그대로 입력:

     | 인덱스(Lv) | damage | cooldown | range | angleDegrees | pierceCount |
     |---|---|---|---|---|---|
     | 0 (Lv1) | 8 | 1.2 | 2.0 | 120 | 0 |
     | 1 (Lv2) | 10 | 1.15 | 2.2 | 130 | 0 |
     | 2 (Lv3) | 12 | 1.1 | 2.4 | 140 | 0 |
     | 3 (Lv4) | 15 | 1.0 | 2.6 | 150 | 0 |

3. 같은 폴더에서 우클릭 → `Create > SushiSurvival > Character Data` → 이름
   `EggCharacterData`
   - `characterName = "계란"`, `baseMoveSpeed = 3`, `baseMaxHealth = 100`
   - `weaponData`에 방금 만든 `EggWeaponData` 드래그
   - `portraitSprite`/`animatorController`는 Task 8~9에서 애니메이션을 만든 뒤
     연결(지금은 비워둠)
4. 우클릭 → `Create > SushiSurvival > Monster Data` → 이름 `BasicMobData`
   - `monsterName = "검은 초밥 병사"`, `maxHealth = 12`, `contactDamage = 5`,
     `moveSpeed = 2`, `xpGemDrop = Basic`

- [ ] **Step 4: 커밋 (git 사용 시)**

---

### Task 6: 부채꼴 판정 로직 (근접 공격 히트박스, TDD)

**Files:**
- Create: `unity/Assets/_Project/Scripts/Weapons/FanHitTest.cs`
- Test: `unity/Assets/Tests/EditMode/FanHitTestTests.cs`

**Interfaces:**
- Produces: `FanHitTest.IsInsideFan(Vector2 origin, Vector2 facing, float radius, float angleDeg, Vector2 targetPos) -> bool`.
  Task 9(EggFanWeapon)가 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/FanHitTestTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Weapons;

namespace SushiSurvival.EditModeTests
{
    public class FanHitTestTests
    {
        [Test]
        public void IsInsideFan_TrueForTargetDirectlyAhead()
        {
            bool result = FanHitTest.IsInsideFan(Vector2.zero, Vector2.right, 2f, 120f, new Vector2(1f, 0f));

            Assert.IsTrue(result);
        }

        [Test]
        public void IsInsideFan_FalseForTargetBehind()
        {
            bool result = FanHitTest.IsInsideFan(Vector2.zero, Vector2.right, 2f, 120f, new Vector2(-1f, 0f));

            Assert.IsFalse(result);
        }

        [Test]
        public void IsInsideFan_FalseWhenBeyondRadius()
        {
            bool result = FanHitTest.IsInsideFan(Vector2.zero, Vector2.right, 2f, 120f, new Vector2(5f, 0f));

            Assert.IsFalse(result);
        }

        [Test]
        public void IsInsideFan_TrueAtHalfAngleBoundary()
        {
            // 120도 부채꼴이면 정면 기준 ±60도까지 포함
            float radians = 60f * Mathf.Deg2Rad;
            var target = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 1.5f;

            bool result = FanHitTest.IsInsideFan(Vector2.zero, Vector2.right, 2f, 120f, target);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsInsideFan_FalseJustOutsideAngleBoundary()
        {
            float radians = 61f * Mathf.Deg2Rad;
            var target = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 1.5f;

            bool result = FanHitTest.IsInsideFan(Vector2.zero, Vector2.right, 2f, 120f, target);

            Assert.IsFalse(result);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Task 2 Step 2와 동일한 명령. Expected: `FanHitTest`가 없어서 컴파일 실패.

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/Weapons/FanHitTest.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Weapons
{
    public static class FanHitTest
    {
        private const float MinDistance = 0.0001f;
        private const float MinFacingSqrMagnitude = 0.0001f;
        // Vector2.Angle은 float 정밀도 오차로 정확한 경계값(예: 60.0)이
        // 60.00001처럼 나올 수 있어 아주 작은 허용오차를 둔다.
        private const float AngleEpsilonDeg = 0.01f;

        public static bool IsInsideFan(Vector2 origin, Vector2 facing, float radius, float angleDeg, Vector2 targetPos)
        {
            Vector2 toTarget = targetPos - origin;
            float distance = toTarget.magnitude;

            if (distance > radius) return false;
            if (distance < MinDistance) return true;
            if (facing.sqrMagnitude < MinFacingSqrMagnitude) return false;

            float angleBetween = Vector2.Angle(facing.normalized, toTarget.normalized);
            return angleBetween <= angleDeg * 0.5f + AngleEpsilonDeg;
        }
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Expected: `FanHitTestTests`의 테스트 5개 모두 Passed.

- [ ] **Step 5: 커밋 (git 사용 시)**

---

### Task 7: 오브젝트 풀 (제네릭 로직 TDD + GameObject 래퍼)

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/ObjectPool.cs`
- Create: `unity/Assets/_Project/Scripts/Core/GameObjectPool.cs`
- Test: `unity/Assets/Tests/EditMode/ObjectPoolTests.cs`

**Interfaces:**
- Produces: `ObjectPool<T>` — 생성자 `(Func<T> factory, Action<T> onGet = null, Action<T> onRelease = null)`,
  `T Get()`, `void Release(T item)`, `int InactiveCount { get; }`.
  `GameObjectPool` (MonoBehaviour) — `GameObject Get(Vector3 position, Quaternion rotation)`,
  `void Release(GameObject go)`.
  Task 10(EnemyBase의 xpGemPool), Task 11(EnemySpawner의 enemyPool),
  Task 12(XPGem의 selfPool)가 `GameObjectPool`을 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/ObjectPoolTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class ObjectPoolTests
    {
        private class DummyItem
        {
            public bool Active;
        }

        [Test]
        public void Get_CreatesNewInstance_WhenPoolIsEmpty()
        {
            int createCount = 0;
            var pool = new ObjectPool<DummyItem>(() => { createCount++; return new DummyItem(); });

            pool.Get();

            Assert.AreEqual(1, createCount);
        }

        [Test]
        public void Get_ReusesReleasedInstance_InsteadOfCreatingNew()
        {
            int createCount = 0;
            var pool = new ObjectPool<DummyItem>(() => { createCount++; return new DummyItem(); });

            var item = pool.Get();
            pool.Release(item);
            var reused = pool.Get();

            Assert.AreEqual(1, createCount);
            Assert.AreSame(item, reused);
        }

        [Test]
        public void Get_InvokesOnGetCallback()
        {
            var pool = new ObjectPool<DummyItem>(() => new DummyItem(), onGet: i => i.Active = true);

            var item = pool.Get();

            Assert.IsTrue(item.Active);
        }

        [Test]
        public void Release_InvokesOnReleaseCallback()
        {
            var pool = new ObjectPool<DummyItem>(() => new DummyItem(), onRelease: i => i.Active = false);
            var item = pool.Get();
            item.Active = true;

            pool.Release(item);

            Assert.IsFalse(item.Active);
        }

        [Test]
        public void InactiveCount_TracksReleasedItems()
        {
            var pool = new ObjectPool<DummyItem>(() => new DummyItem());
            var item = pool.Get();

            pool.Release(item);

            Assert.AreEqual(1, pool.InactiveCount);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Task 2 Step 2와 동일한 명령. Expected: `ObjectPool<T>`가 없어서 컴파일 실패.

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/Core/ObjectPool.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace SushiSurvival.Core
{
    public class ObjectPool<T>
    {
        private readonly Stack<T> _inactive = new Stack<T>();
        private readonly Func<T> _factory;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;

        public int InactiveCount => _inactive.Count;

        public ObjectPool(Func<T> factory, Action<T> onGet = null, Action<T> onRelease = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _onGet = onGet;
            _onRelease = onRelease;
        }

        public T Get()
        {
            T item = _inactive.Count > 0 ? _inactive.Pop() : _factory();
            _onGet?.Invoke(item);
            return item;
        }

        public void Release(T item)
        {
            _onRelease?.Invoke(item);
            _inactive.Push(item);
        }
    }
}
```

`unity/Assets/_Project/Scripts/Core/GameObjectPool.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// ObjectPool&lt;GameObject&gt;를 감싸는 MonoBehaviour 래퍼.
    /// 몹/총알/젬처럼 동시 개체 수가 많은 프리팹에 사용한다.
    /// </summary>
    public class GameObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int prewarmCount = 20;

        private ObjectPool<GameObject> _pool;

        private void Awake()
        {
            _pool = new ObjectPool<GameObject>(
                factory: CreateInstance,
                onGet: go => go.SetActive(true),
                onRelease: go => go.SetActive(false));

            for (int i = 0; i < prewarmCount; i++)
                _pool.Release(_pool.Get());
        }

        private GameObject CreateInstance()
        {
            var go = Instantiate(prefab, transform);
            go.SetActive(false);
            return go;
        }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            var go = _pool.Get();
            go.transform.SetPositionAndRotation(position, rotation);
            return go;
        }

        public void Release(GameObject go) => _pool.Release(go);
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Expected: `ObjectPoolTests`의 테스트 5개 모두 Passed.

- [ ] **Step 5: 커밋 (git 사용 시)**

---

### Task 8: PlayerController + PlayerHealth 통합

**Files:**
- Create: `unity/Assets/_Project/Scripts/Player/PlayerController.cs`
- Create: `unity/Assets/_Project/Scripts/Player/PlayerHealth.cs`

**Interfaces:**
- Consumes: `SushiSurvival.Player.FacingController` (Task 3),
  `SushiSurvival.Core.HealthLogic` (Task 4), `SushiSurvival.Data.CharacterData` (Task 5).
- Produces: `PlayerController` (MonoBehaviour), `PlayerHealth` —
  `float CurrentHealth { get; }`, `void TakeDamage(float damage)`,
  `event Action OnDeath`. Task 10(EnemyBase가 충돌 시 호출),
  Task 13(GameManager가 OnDeath 구독)이 사용한다.

이 Task는 MonoBehaviour 통합 코드라 EditMode 유닛 테스트 대상이 아니다(순수
로직은 이미 Task 3/4에서 테스트 완료). 검증은 Unity Editor Play 모드 수동
테스트로 한다.

- [ ] **Step 1: PlayerHealth 작성**

`unity/Assets/_Project/Scripts/Player/PlayerHealth.cs`:

```csharp
using System;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private CharacterData characterData;

        public float CurrentHealth { get; private set; }
        public event Action OnDeath;

        private void Awake()
        {
            CurrentHealth = characterData.baseMaxHealth;
        }

        public void TakeDamage(float damage)
        {
            if (HealthLogic.IsDead(CurrentHealth)) return;

            CurrentHealth = HealthLogic.ApplyDamage(CurrentHealth, damage);

            if (HealthLogic.IsDead(CurrentHealth))
                OnDeath?.Invoke();
        }
    }
}
```

- [ ] **Step 2: PlayerController 작성**

`unity/Assets/_Project/Scripts/Player/PlayerController.cs`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using SushiSurvival.Data;

namespace SushiSurvival.Player
{
    [RequireComponent(typeof(FacingController))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private CharacterData characterData;
        [SerializeField] private InputActionReference moveAction;

        private FacingController _facing;
        private Rigidbody2D _rigidbody;
        private Vector2 _moveInput;

        private void Awake()
        {
            _facing = GetComponent<FacingController>();
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void OnEnable() => moveAction.action.Enable();
        private void OnDisable() => moveAction.action.Disable();

        private void Update()
        {
            _moveInput = moveAction.action.ReadValue<Vector2>();
            _facing.UpdateFacing(_moveInput);
        }

        private void FixedUpdate()
        {
            Vector2 velocity = _moveInput.normalized * characterData.baseMoveSpeed;
            _rigidbody.MovePosition(_rigidbody.position + velocity * Time.fixedDeltaTime);
        }
    }
}
```

- [ ] **Step 3: 테스트 실행해서 기존 테스트 전부 컴파일/통과 확인**

Task 2 Step 2와 동일한 명령. Expected: 지금까지 작성한 EditMode 테스트(20개)
전부 Passed (이 Task 자체는 새 테스트를 추가하지 않는다 — 컴파일 깨짐만 방지
확인).

- [ ] **Step 4: Unity Editor에서 직접 — Input Action 에셋 생성**

1. `Assets/_Project/Data` 폴더에서 우클릭 → `Create > Input Actions` → 이름
   `PlayerInputActions`
2. 더블클릭해서 열기 → Action Map 이름 `Player` 추가 → 그 안에 Action `Move`
   추가, Action Type = `Value`, Control Type = `Vector2`
3. `Move`에 바인딩 추가: `Composite > 2D Vector` → Up=W, Down=S, Left=A,
   Right=D 할당. 추가로 `Left Stick [Gamepad]` 바인딩도 추가(좌스틱 지원)
4. Save Asset

- [ ] **Step 5: Unity Editor에서 직접 — 계란 플레이어 GameObject 구성**

1. `Assets/Art/캐릭터/캐릭터/계란초밥 시트`의 대기/이동 스프라이트 시트를
   선택 → Inspector에서 Sprite Mode = `Multiple` → `Sprite Editor` 열어서
   `Slice > Automatic` 실행 → Apply
2. 새 씬 `Assets/_Project/Scenes/Slice1.unity` 생성 (아직 없다면)
3. 빈 GameObject 생성 → 이름 `Player`, Tag를 새로 만든 `Player`로 설정
4. 컴포넌트 추가: `SpriteRenderer`, `Rigidbody2D`(Body Type = Kinematic,
   Gravity Scale = 0, **`Use Full Kinematic Contacts` 체크** — Unity 2D
   물리는 Kinematic끼리는 기본적으로 충돌 콜백(`OnCollisionStay2D` 등)이
   발생하지 않아서, 이걸 켜두지 않으면 잡몹 접촉 데미지가 아예 안 들어온다),
   `CircleCollider2D`, `FacingController`,
   `PlayerController`, `PlayerHealth`, `Animator`
5. `FacingController`의 `Sprite Renderer` 필드에 방금 추가한 SpriteRenderer
   드래그
6. `PlayerController`의 `Character Data`에 Task 5에서 만든 `EggCharacterData`,
   `Move Action`에 `PlayerInputActions`의 `Player/Move` 드래그
7. `PlayerHealth`의 `Character Data`에도 `EggCharacterData` 드래그
8. Animator Controller를 새로 만들어 대기/이동 애니메이션 클립 연결(간단히
   `IsMoving` bool 파라미터로 대기↔이동 전환)

- [ ] **Step 6: Unity Editor에서 직접 — 수동 플레이테스트**

1. Play 버튼 클릭
2. WASD로 이동 시 계란이 이동 방향을 바라보는지(좌우 반전) 확인
3. 이동을 멈췄을 때 마지막 방향을 유지하는지 확인
4. Console 창에서 에러가 없는지 확인
5. (임시 확인용) `Player` GameObject에 테스트용 버튼이나 Inspector 우클릭
   `Debug > TakeDamage` 같은 컨텍스트 메뉴는 없으므로, `PlayerHealth`에
   `[ContextMenu("Test Take 200 Damage")]`를 임시로 붙여 우클릭 → 실행 →
   `OnDeath`가 트리거되는지 Console 로그로 확인 (확인 후 이 ContextMenu
   임시 코드는 제거하거나 Task 13에서 실제 사망 로직으로 대체됨)

- [ ] **Step 7: 커밋 (git 사용 시)**

---

### Task 9: EggFanWeapon (양산 근접 광역 무기)

**Files:**
- Create: `unity/Assets/_Project/Scripts/Weapons/EggFanWeapon.cs`

**Interfaces:**
- Consumes: `SushiSurvival.Weapons.FanHitTest` (Task 6),
  `SushiSurvival.Player.FacingController` (Task 3),
  `SushiSurvival.Data.WeaponData` / `WeaponLevelStats` (Task 5),
  `SushiSurvival.Enemies.EnemyBase.TakeDamage(float)` (Task 10 — 이 Task보다
  먼저 Task 10을 구현했다면 참조 가능; 순서상 이 Task가 먼저라면 Task 10 완료
  후 Step 6에서 연결).
- Produces: `EggFanWeapon` (MonoBehaviour). Task 13이 Player 프리팹에 부착한다.

이 Task도 MonoBehaviour 통합 코드라 EditMode 테스트 대상이 아니다(부채꼴 판정
로직 자체는 Task 6에서 이미 테스트 완료). Play 모드 수동 테스트로 검증한다.

- [ ] **Step 1: EggFanWeapon 작성**

`unity/Assets/_Project/Scripts/Weapons/EggFanWeapon.cs`:

```csharp
using UnityEngine;
using SushiSurvival.Data;
using SushiSurvival.Player;
using SushiSurvival.Enemies;

namespace SushiSurvival.Weapons
{
    public class EggFanWeapon : MonoBehaviour
    {
        [SerializeField] private WeaponData weaponData;
        [SerializeField] private FacingController facing;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private Animator animator;
        [SerializeField] private int currentLevel = 1; // 1-based (1~4)

        private float _cooldownTimer;

        private WeaponLevelStats CurrentStats => weaponData.levels[currentLevel - 1];

        private void Update()
        {
            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer > 0f) return;

            Attack();
            _cooldownTimer = CurrentStats.cooldown;
        }

        private void Attack()
        {
            var stats = CurrentStats;
            animator?.SetTrigger("Attack");

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, stats.range, enemyLayer);
            foreach (var hit in hits)
            {
                if (!hit.TryGetComponent<EnemyBase>(out var enemy)) continue;

                if (FanHitTest.IsInsideFan(transform.position, facing.CurrentFacing, stats.range, stats.angleDegrees, enemy.transform.position))
                    enemy.TakeDamage(stats.damage);
            }
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 기존 테스트 전부 컴파일/통과 확인**

Task 2 Step 2와 동일한 명령. (Task 10을 아직 안 했다면 `EnemyBase`가 없어
컴파일 에러가 난다 — 이 경우 Task 10을 먼저 완료한 뒤 이 Step으로 돌아온다.)

- [ ] **Step 3: Unity Editor에서 직접 — Player에 무기 부착**

1. `Player` GameObject에 `EggFanWeapon` 컴포넌트 추가
2. `Weapon Data`에 `EggWeaponData` 드래그, `Facing`에 같은 오브젝트의
   `FacingController` 드래그, `Animator`에 같은 오브젝트의 Animator 드래그
3. `Enemy Layer`는 Task 10에서 만들 "Enemy" 레이어를 선택(아직 없으면 Task 10
   완료 후 설정)
4. `Current Level = 1`

- [ ] **Step 4: 커밋 (git 사용 시, Task 10 완료 후 최종 확인과 함께)**

---

### Task 10: EnemyBase + EnemyAI (잡몹)

**Files:**
- Create: `unity/Assets/_Project/Scripts/Enemies/EnemyBase.cs`
- Create: `unity/Assets/_Project/Scripts/Enemies/EnemyAI.cs`

**Interfaces:**
- Consumes: `SushiSurvival.Core.HealthLogic` (Task 4),
  `SushiSurvival.Core.GameObjectPool` (Task 7),
  `SushiSurvival.Data.MonsterData` (Task 5),
  `SushiSurvival.Player.PlayerHealth` (Task 8).
- Produces: `EnemyBase` — `float CurrentHealth { get; }`,
  `void TakeDamage(float damage)`, `void SetXpGemPool(GameObjectPool pool)`,
  `event Action<EnemyBase> OnDeath`. `EnemyAI`. Task 9(EggFanWeapon)가
  `EnemyBase.TakeDamage`를 호출하고, Task 11(EnemySpawner)이 이 컴포넌트가
  붙은 프리팹을 스폰하면서 `SetXpGemPool`을 호출한다.

**주의:** `BasicMob`은 프리팹 에셋이고 XP 젬 풀(`XPGemPool`)은 씬에만 존재하는
오브젝트이므로, 프리팹의 Inspector 필드로 씬 오브젝트를 직접 참조할 수 없다.
그래서 `xpGemPool`은 Inspector 직렬화 필드가 아니라 스폰 시점에
`EnemySpawner`가 런타임으로 주입하는 방식을 쓴다(Task 11 참고).

- [ ] **Step 1: EnemyBase 작성**

`unity/Assets/_Project/Scripts/Enemies/EnemyBase.cs`:

```csharp
using System;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;
using SushiSurvival.Player;

namespace SushiSurvival.Enemies
{
    [RequireComponent(typeof(Collider2D))]
    public class EnemyBase : MonoBehaviour
    {
        private const float ContactDamageInterval = 0.5f;

        [SerializeField] private MonsterData monsterData;

        private GameObjectPool _xpGemPool;

        public float CurrentHealth { get; private set; }
        public event Action<EnemyBase> OnDeath;

        private float _contactTimer;

        private void OnEnable()
        {
            CurrentHealth = monsterData.maxHealth;
            _contactTimer = 0f;
        }

        private void Update() => _contactTimer -= Time.deltaTime;

        /// <summary>
        /// EnemySpawner가 Get() 직후 매번 호출해서 주입한다. 프리팹 에셋은
        /// 씬에만 존재하는 XPGemPool을 Inspector로 직접 참조할 수 없기 때문.
        /// </summary>
        public void SetXpGemPool(GameObjectPool pool) => _xpGemPool = pool;

        public void TakeDamage(float damage)
        {
            if (HealthLogic.IsDead(CurrentHealth)) return;

            CurrentHealth = HealthLogic.ApplyDamage(CurrentHealth, damage);

            if (HealthLogic.IsDead(CurrentHealth))
                Die();
        }

        private void Die()
        {
            if (_xpGemPool == null)
                Debug.LogError($"{name}: xpGemPool이 설정되지 않아 XP 젬을 드롭할 수 없습니다.");
            else
                _xpGemPool.Get(transform.position, Quaternion.identity);

            OnDeath?.Invoke(this);

            // 정정(2026-08-19): 아래 반환 코드가 원래 플랜에 빠져 있었다. 없으면 죽은
            // 적이 화면에 남아 계속 플레이어를 쫓고 접촉 데미지까지 준다.
            // _selfPool은 Awake에서 GetComponentInParent<GameObjectPool>()로 찾는다.
            if (_selfPool != null)
                _selfPool.Release(gameObject);
            else
                Destroy(gameObject);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (_contactTimer > 0f) return;
            if (!collision.collider.TryGetComponent<PlayerHealth>(out var player)) return;

            player.TakeDamage(monsterData.contactDamage);
            _contactTimer = ContactDamageInterval;
        }
    }
}
```

- [ ] **Step 2: EnemyAI 작성**

`unity/Assets/_Project/Scripts/Enemies/EnemyAI.cs`:

```csharp
using UnityEngine;
using SushiSurvival.Data;

namespace SushiSurvival.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyAI : MonoBehaviour
    {
        [SerializeField] private MonsterData monsterData;

        private Rigidbody2D _rigidbody;
        private Transform _target;

        private void Awake() => _rigidbody = GetComponent<Rigidbody2D>();

        private void OnEnable()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            _target = playerObj != null ? playerObj.transform : null;
        }

        private void FixedUpdate()
        {
            if (_target == null) return;

            Vector2 direction = ((Vector2)_target.position - _rigidbody.position).normalized;
            _rigidbody.MovePosition(_rigidbody.position + direction * monsterData.moveSpeed * Time.fixedDeltaTime);
        }
    }
}
```

- [ ] **Step 3: 테스트 실행해서 기존 테스트 전부 컴파일/통과 확인**

Task 2 Step 2와 동일한 명령. Expected: 지금까지의 EditMode 테스트 전부 Passed
(이 Task는 새 유닛 테스트를 추가하지 않음).

- [ ] **Step 4: Unity Editor에서 직접 — "Enemy" 레이어 생성 및 잡몹 프리팹 구성**

1. `Edit > Project Settings > Tags and Layers` → User Layer 슬롯에 `Enemy`
   추가
2. `Assets/Art/캐릭터/캐릭터/몬스터 시트/new 몬스터-Sheet.png`를 Sprite Mode =
   Multiple로 바꾸고 Sprite Editor에서 `Slice > Automatic` 실행
3. 빈 GameObject 생성 → 이름 `BasicMob`, Layer = `Enemy`
4. 컴포넌트 추가: `SpriteRenderer`, `Rigidbody2D`(Kinematic, Gravity Scale 0),
   `CircleCollider2D`(Is Trigger 체크 해제 — 접촉 판정에 물리 충돌 필요),
   `EnemyBase`, `EnemyAI`
5. `EnemyBase`의 `Monster Data`에 `BasicMobData` 드래그 (`xpGemPool`은
   Inspector 필드가 아니라 Task 11에서 `EnemySpawner`가 런타임에 주입하므로
   여기서 연결할 것 없음)
6. `EnemyAI`의 `Monster Data`에도 `BasicMobData` 드래그
7. `Assets/_Project/Prefabs`로 드래그해서 프리팹화 → 씬의 인스턴스는 삭제

- [ ] **Step 5: Unity Editor에서 직접 — EggFanWeapon의 Enemy Layer 설정**

`Player`의 `EggFanWeapon` 컴포넌트로 돌아가서 `Enemy Layer`에 방금 만든
`Enemy` 레이어 체크

- [ ] **Step 6: 커밋 (git 사용 시)**

---

### Task 11: EnemySpawner (잡몹 반복 스폰)

**Files:**
- Create: `unity/Assets/_Project/Scripts/Enemies/SpawnRingUtility.cs`
- Create: `unity/Assets/_Project/Scripts/Enemies/EnemySpawner.cs`
- Test: `unity/Assets/Tests/EditMode/SpawnRingUtilityTests.cs`

**Interfaces:**
- Consumes: `SushiSurvival.Core.GameObjectPool` (Task 7),
  `SushiSurvival.Enemies.EnemyBase.SetXpGemPool(GameObjectPool)` (Task 10).
- Produces: `SpawnRingUtility.GetPositionOnRing(Vector2 center, float radius, float angleRad) -> Vector2`,
  `EnemySpawner` (MonoBehaviour). Task 13이 씬에 배치하고 연결한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/SpawnRingUtilityTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Enemies;

namespace SushiSurvival.EditModeTests
{
    public class SpawnRingUtilityTests
    {
        [Test]
        public void GetPositionOnRing_AtAngleZero_IsToTheRightOfCenter()
        {
            var result = SpawnRingUtility.GetPositionOnRing(Vector2.zero, 5f, 0f);

            Assert.That(result.x, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(result.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void GetPositionOnRing_AtAnglePi_IsToTheLeftOfCenter()
        {
            var result = SpawnRingUtility.GetPositionOnRing(Vector2.zero, 5f, Mathf.PI);

            Assert.That(result.x, Is.EqualTo(-5f).Within(0.001f));
            Assert.That(result.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void GetPositionOnRing_AlwaysStaysAtGivenRadiusFromCenter()
        {
            var center = new Vector2(10f, -3f);
            var result = SpawnRingUtility.GetPositionOnRing(center, 7f, 1.234f);

            float distance = Vector2.Distance(center, result);
            Assert.That(distance, Is.EqualTo(7f).Within(0.0001f));
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Task 2 Step 2와 동일한 명령. Expected: `SpawnRingUtility`가 없어서 컴파일 실패.

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/Enemies/SpawnRingUtility.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Enemies
{
    public static class SpawnRingUtility
    {
        public static Vector2 GetPositionOnRing(Vector2 center, float radius, float angleRad)
        {
            var offset = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * radius;
            return center + offset;
        }
    }
}
```

`unity/Assets/_Project/Scripts/Enemies/EnemySpawner.cs`:

```csharp
using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.Enemies
{
    /// <summary>
    /// 슬라이스 1 전용 단순 스폰러 — 웨이브 타임라인 없이 잡몹을 일정 주기로
    /// 플레이어 주변 링에서 반복 스폰한다. 타임라인 연동은 슬라이스 2에서.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObjectPool enemyPool;
        [SerializeField] private GameObjectPool xpGemPool;
        [SerializeField] private Transform player;
        [SerializeField] private float spawnRadius = 10f;
        [SerializeField] private float spawnInterval = 1.5f;

        private float _timer;

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            SpawnOne();
            _timer = spawnInterval;
        }

        private void SpawnOne()
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 spawnPos = SpawnRingUtility.GetPositionOnRing(player.position, spawnRadius, angle);
            GameObject enemyObj = enemyPool.Get(spawnPos, Quaternion.identity);

            if (enemyObj.TryGetComponent<EnemyBase>(out var enemy))
                enemy.SetXpGemPool(xpGemPool);
        }
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Expected: `SpawnRingUtilityTests`의 테스트 3개 모두 Passed.

- [ ] **Step 5: Unity Editor에서 직접 — 스포너 배치**

1. 빈 GameObject `EnemySpawner` 생성 후 `GameObjectPool`,`EnemySpawner` 컴포넌트
   추가
2. `GameObjectPool`의 `Prefab`에 Task 10에서 만든 `BasicMob` 프리팹 드래그,
   `Prewarm Count = 100`
3. `EnemySpawner`의 `Enemy Pool`에 같은 오브젝트의 `GameObjectPool`,
   `Player`에 씬의 `Player` GameObject, `Spawn Radius = 10`,
   `Spawn Interval = 1.5` 설정. `Xp Gem Pool`은 아직 `XPGemPool`이 없으므로
   비워두고 Task 13에서 연결한다.

- [ ] **Step 6: 커밋 (git 사용 시)**

---

### Task 12: XPGem (경험치 젬 드롭 및 흡수)

**Files:**
- Create: `unity/Assets/_Project/Scripts/Pickups/PickupUtility.cs`
- Create: `unity/Assets/_Project/Scripts/Pickups/XPGem.cs`
- Test: `unity/Assets/Tests/EditMode/PickupUtilityTests.cs`

**Interfaces:**
- Consumes: `SushiSurvival.Core.GameObjectPool` (Task 7).
- Produces: `PickupUtility.IsWithinPickupRadius(Vector2 a, Vector2 b, float radius) -> bool`,
  `XPGem` (MonoBehaviour, `AddExperience` 호출을 위해 Task 13의
  `GameManager.Instance`를 참조 — Task 13 완료 전까지는 컴파일 에러이므로
  Task 13과 함께 마무리한다).

**주의:** `GameObjectPool.CreateInstance`는 `Instantiate(prefab, transform)`으로
스폰하므로, 스폰된 젬은 항상 자기 풀의 자식으로 생성된다. 그래서 `XPGem`은
자신이 속한 풀을 Inspector 직렬화 필드가 아니라 `GetComponentInParent`로
스스로 찾는다 — `BasicMob`과 달리 XP 젬은 "자기 자신의" 풀만 알면 되므로
EnemySpawner 같은 외부 주입이 필요 없다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/PickupUtilityTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Pickups;

namespace SushiSurvival.EditModeTests
{
    public class PickupUtilityTests
    {
        [Test]
        public void IsWithinPickupRadius_TrueWhenCloseEnough()
        {
            bool result = PickupUtility.IsWithinPickupRadius(Vector2.zero, new Vector2(0.3f, 0f), 0.5f);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsWithinPickupRadius_FalseWhenTooFar()
        {
            bool result = PickupUtility.IsWithinPickupRadius(Vector2.zero, new Vector2(5f, 0f), 0.5f);

            Assert.IsFalse(result);
        }

        [Test]
        public void IsWithinPickupRadius_TrueExactlyAtRadius()
        {
            bool result = PickupUtility.IsWithinPickupRadius(Vector2.zero, new Vector2(0.5f, 0f), 0.5f);

            Assert.IsTrue(result);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Task 2 Step 2와 동일한 명령. Expected: `PickupUtility`가 없어서 컴파일 실패.

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/Pickups/PickupUtility.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Pickups
{
    public static class PickupUtility
    {
        public static bool IsWithinPickupRadius(Vector2 a, Vector2 b, float radius)
            => (a - b).sqrMagnitude <= radius * radius;
    }
}
```

`unity/Assets/_Project/Scripts/Pickups/XPGem.cs`:

```csharp
using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.Pickups
{
    public class XPGem : MonoBehaviour
    {
        [SerializeField] private float pickupRadius = 0.5f;
        [SerializeField] private float xpValue = 1f; // 슬라이스1: 기본(흰색) 등급 고정값

        private GameObjectPool _selfPool;
        private Transform _player;

        private void Awake()
        {
            // GameObjectPool.CreateInstance가 Instantiate(prefab, transform)으로
            // 생성하므로, 부모를 타고 올라가면 항상 자기 풀을 찾을 수 있다.
            _selfPool = GetComponentInParent<GameObjectPool>();
        }

        private void OnEnable()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            _player = playerObj != null ? playerObj.transform : null;
        }

        private void Update()
        {
            if (_player == null) return;
            if (!PickupUtility.IsWithinPickupRadius(transform.position, _player.position, pickupRadius)) return;

            GameManager.Instance.AddExperience(xpValue);

            if (_selfPool == null)
            {
                Debug.LogError($"{name}: selfPool을 찾지 못해 풀로 반환하지 못하고 파괴합니다.");
                Destroy(gameObject);
            }
            else
            {
                _selfPool.Release(gameObject);
            }
        }
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인 (컴파일 에러 예상)**

Task 2 Step 2와 동일한 명령. Expected: `GameManager`가 아직 없어 컴파일 실패
— 정상. Task 13을 완료한 뒤 이 Step으로 돌아와 재확인한다.

- [ ] **Step 5: Unity Editor에서 직접 — XP 젬 프리팹 구성**

1. `Assets/Art/캐릭터/캐릭터/경험치/밥알(경험치).png`를 스프라이트로 임포트
2. 빈 GameObject `XPGem` 생성 → `SpriteRenderer`(위 스프라이트 지정),
   `XPGem` 컴포넌트 추가
3. `Xp Value = 1`, `Pickup Radius = 0.5`
4. `Assets/_Project/Prefabs`로 드래그해서 프리팹화 → 씬 인스턴스 삭제
5. 씬의 `EnemySpawner` 옆에 빈 GameObject `XPGemPool` 생성 → `GameObjectPool`
   추가 → `Prefab`에 `XPGem` 프리팹, `Prewarm Count = 50` (`XPGem`의 풀 참조는
   코드에서 `GetComponentInParent`로 자동 해결되므로 별도 배선 불필요)

- [ ] **Step 6: 커밋 (git 사용 시, Task 13과 함께 최종 확인)**

---

### Task 13: GameManager + 최종 통합 및 수동 플레이테스트

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/GameManager.cs`

**Interfaces:**
- Consumes: `SushiSurvival.Player.PlayerHealth.OnDeath` (Task 8),
  `SushiSurvival.Pickups.XPGem` (Task 12, `GameManager.Instance` 참조).
- Produces: `GameManager` — `static GameManager Instance`,
  `float TotalExperience { get; }`, `bool IsGameOver { get; }`,
  `void AddExperience(float amount)`.

- [ ] **Step 1: GameManager 작성**

`unity/Assets/_Project/Scripts/Core/GameManager.cs`:

```csharp
using UnityEngine;
using SushiSurvival.Player;

namespace SushiSurvival.Core
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private PlayerHealth playerHealth;

        public static GameManager Instance { get; private set; }

        public float TotalExperience { get; private set; }
        public bool IsGameOver { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            if (playerHealth != null)
                playerHealth.OnDeath += HandlePlayerDeath;
        }

        private void OnDisable()
        {
            if (playerHealth != null)
                playerHealth.OnDeath -= HandlePlayerDeath;
        }

        public void AddExperience(float amount)
        {
            if (IsGameOver) return;

            TotalExperience += amount;
            Debug.Log($"[GameManager] 누적 경험치: {TotalExperience}");
        }

        private void HandlePlayerDeath()
        {
            IsGameOver = true;
            Debug.Log("GAME OVER");
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 전체 컴파일/통과 확인**

Task 2 Step 2와 동일한 명령. Expected: 지금까지 작성한 EditMode 테스트
(StatSystem 5 + FacingLogic 5 + HealthLogic 5 + FanHitTest 5 + ObjectPool 5 +
SpawnRingUtility 3 + PickupUtility 3 = 31개) 전부 Passed, 컴파일 에러 없음.

- [ ] **Step 3: Unity Editor에서 직접 — 씬 최종 배치**

1. 빈 GameObject `GameManager` 생성 → `GameManager` 컴포넌트 추가 →
   `Player Health`에 씬의 `Player` → `PlayerHealth` 드래그
2. 씬의 `EnemySpawner`를 선택 → `Xp Gem Pool` 필드에 Task 12에서 만든
   `XPGemPool` GameObject를 드래그(둘 다 씬 오브젝트라 참조 가능)
3. 메인 카메라를 Orthographic으로, Player를 따라가도록 간단한 카메라 추적
   스크립트는 슬라이스 1 범위 밖이므로 카메라는 고정하거나 Player의 자식으로
   이동(임시)
4. `Assets/Art/환경/환경/바닥.png`를 배경으로 씬에 배치(Sorting Layer를
   Player/Enemy보다 낮게 설정)

- [ ] **Step 4: Unity Editor에서 직접 — 엔드투엔드 수동 플레이테스트**

Play 버튼을 누르고 아래를 순서대로 확인:

1. 계란이 WASD로 이동하고 방향 반전이 정상 동작
2. 몇 초 후 화면 밖에서 잡몹이 스폰되어 플레이어를 향해 다가옴
3. 플레이어가 잡몹 근처에 서 있으면 양산 무기가 자동으로 부채꼴 범위 공격을
   가함(애니메이션 4프레임 재생 확인)
4. 잡몹 체력이 0이 되면 사라지고 그 자리에 XP 젬이 드롭됨
5. 플레이어가 젬 근처로 이동하면 젬이 사라지고 Console에
   `[GameManager] 누적 경험치: N` 로그가 출력됨
6. 잡몹에게 계속 접촉당해 플레이어 체력이 0이 되면 Console에 `GAME OVER`
   로그가 출력됨
7. Console에 위 흐름과 무관한 에러(빨간 로그)가 없는지 확인

모두 통과하면 슬라이스 1 완료.

- [ ] **Step 5: 커밋 (git 사용 시)**

```bash
cd "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요"
git add unity/ docs/
git commit -m "feat: 슬라이스 1 코어 루프 (계란 캐릭터) 구현"
```
