# 슬라이스 2b: 레벨업 3택 + 증강 + 체력바 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 경험치가 레벨업으로 이어지고, 3택 팝업에서 증강 10종과 무기 강화를 골라
실제로 강해지는 루프를 완성한다. 플레이어 체력바와 잡몹 2종화도 포함한다.

**Architecture:** `PlayerStats`가 `StatSystem`을 소유하고 모든 게임플레이 코드가
여기서 값을 읽는다. 증강은 `StatSystem`에 modifier를 추가할 뿐이며 캡 클램프는 그
한 곳에서만 일어난다. 레벨업 계산·후보 선정·비율 계산 같은 순수 로직은 EditMode
유닛 테스트로 TDD하고, MonoBehaviour 통합 동작은 Play 모드 수동 테스트로 확인한다.

**Tech Stack:** Unity 2022.3.62f3 LTS, Built-in 2D, Input System, uGUI,
Unity Test Framework(EditMode, NUnit)

**Spec:** [docs/superpowers/specs/2026-08-19-slice2b-levelup-and-augments-design.md](../specs/2026-08-19-slice2b-levelup-and-augments-design.md)

## Global Constraints

- Unity 버전: 2022.3.62f3 고정. 입력은 새 Input System만. 렌더는 Built-in.
- UI 버튼은 반드시 `Button (Legacy)`를 쓴다 (TextMeshPro 미설치).
- 수치는 코드에 하드코딩하지 않고 ScriptableObject 에셋 또는 인스펙터 필드에 둔다.
- **하드캡 (기획서 요구사항, 반드시 지킬 것):**
  - 방어력 최대 0.5 (50% 감소). 넘기면 무적 조합이 생긴다.
  - 공격속도 배율 최대 2.0 + **최소 쿨타임 절대값** 별도 적용.
  - 부활 최대 1회.
- 레벨업 필요 경험치 초기값: 기본 5, 레벨당 증가분 3 (인스펙터 노출, 튜닝 대상).
- 잡몹 수치: 검은 초밥 병사 체력12/접촉5/이속2, 캘리포니아롤 체력20/접촉5/이속1.5.
- **Unity Editor가 열려 있으면 배치 모드 명령이 실패한다.** 코드 Task는 닫고,
  Editor Task는 열고 진행한다.
- **테스트 실행 명령에 `-quit`을 붙이지 않는다.** 표준 명령:

```bash
"/c/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" \
  -batchmode -nographics \
  -projectPath "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity" \
  -runTests -testPlatform EditMode \
  -testResults "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity/TestResults.xml" \
  -logFile "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity_test.log"
```

  결과 확인:

```bash
grep -oE '<test-run[^>]*' "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity/TestResults.xml"
grep -iE "error CS" "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity_test.log" | head -10
```

- 이 플랜 시작 시점의 기존 테스트는 **63개**다. 각 Task 후 총계가 줄지 않아야 한다.

---

## Phase 1 — 스탯 배관

눈에 보이는 변화가 없는 단계다. 기존과 동일하게 동작하는지 회귀 확인만 한다.

### Task 1: ArmorLogic + CooldownLogic 순수 로직 (TDD)

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/ArmorLogic.cs`
- Create: `unity/Assets/_Project/Scripts/Weapons/CooldownLogic.cs`
- Test: `unity/Assets/Tests/EditMode/ArmorLogicTests.cs`
- Test: `unity/Assets/Tests/EditMode/CooldownLogicTests.cs`

**Interfaces:**
- Produces: `ArmorLogic.ApplyArmor(float damage, float armor) -> float`,
  `ArmorLogic.MaxArmor` (상수 0.5),
  `CooldownLogic.ApplyAttackSpeed(float baseCooldown, float attackSpeedMultiplier, float minCooldown) -> float`.
  Task 3(`PlayerHealth`, `WeaponBase`)이 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/ArmorLogicTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class ArmorLogicTests
    {
        [Test]
        public void ApplyArmor_ReturnsFullDamage_WhenNoArmor()
        {
            Assert.That(ArmorLogic.ApplyArmor(100f, 0f), Is.EqualTo(100f).Within(0.0001f));
        }

        [Test]
        public void ApplyArmor_HalvesDamage_AtMaxArmor()
        {
            Assert.That(ArmorLogic.ApplyArmor(100f, 0.5f), Is.EqualTo(50f).Within(0.0001f));
        }

        [Test]
        public void ApplyArmor_ClampsToMaxArmor_NeverGrantsInvincibility()
        {
            // 방어력이 1.0으로 잘못 들어와도 절대 0 데미지가 되면 안 된다.
            Assert.That(ArmorLogic.ApplyArmor(100f, 1f), Is.EqualTo(50f).Within(0.0001f));
        }

        [Test]
        public void ApplyArmor_IgnoresNegativeArmor()
        {
            Assert.That(ArmorLogic.ApplyArmor(100f, -2f), Is.EqualTo(100f).Within(0.0001f));
        }

        [Test]
        public void ApplyArmor_ScalesPartialArmor()
        {
            Assert.That(ArmorLogic.ApplyArmor(100f, 0.2f), Is.EqualTo(80f).Within(0.0001f));
        }
    }
}
```

`unity/Assets/Tests/EditMode/CooldownLogicTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.Weapons;

namespace SushiSurvival.EditModeTests
{
    public class CooldownLogicTests
    {
        [Test]
        public void ApplyAttackSpeed_ReturnsBase_WhenMultiplierIsOne()
        {
            Assert.That(CooldownLogic.ApplyAttackSpeed(1.2f, 1f, 0.1f), Is.EqualTo(1.2f).Within(0.0001f));
        }

        [Test]
        public void ApplyAttackSpeed_HalvesCooldown_AtDoubleSpeed()
        {
            Assert.That(CooldownLogic.ApplyAttackSpeed(1.2f, 2f, 0.1f), Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void ApplyAttackSpeed_RespectsMinimumCooldown()
        {
            // 이나리 기본 0.35초에 2배속이면 0.175초지만, 최소 쿨타임이 하한을 잡는다.
            Assert.That(CooldownLogic.ApplyAttackSpeed(0.35f, 2f, 0.25f), Is.EqualTo(0.25f).Within(0.0001f));
        }

        [Test]
        public void ApplyAttackSpeed_FallsBackToBase_WhenMultiplierIsZero()
        {
            // 0으로 나누어 무한대가 되는 것을 막는다.
            Assert.That(CooldownLogic.ApplyAttackSpeed(1.2f, 0f, 0.1f), Is.EqualTo(1.2f).Within(0.0001f));
        }

        [Test]
        public void ApplyAttackSpeed_FallsBackToBase_WhenMultiplierIsNegative()
        {
            Assert.That(CooldownLogic.ApplyAttackSpeed(1.2f, -3f, 0.1f), Is.EqualTo(1.2f).Within(0.0001f));
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Global Constraints의 표준 명령 실행.
Expected: `ArmorLogic` / `CooldownLogic`이 없어서 컴파일 실패.

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/Core/ArmorLogic.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Core
{
    public static class ArmorLogic
    {
        /// <summary>
        /// 기획서 하드캡. 이 값을 넘기면 피해 감소 100%(무적) 조합이 생긴다.
        /// StatSystem에서도 캡을 걸지만, 설정 실수로 무적이 되는 일이 없도록
        /// 실제 사용 지점에서 한 번 더 막는다.
        /// </summary>
        public const float MaxArmor = 0.5f;

        public static float ApplyArmor(float damage, float armor)
        {
            float safeArmor = Mathf.Clamp(armor, 0f, MaxArmor);
            return damage * (1f - safeArmor);
        }
    }
}
```

`unity/Assets/_Project/Scripts/Weapons/CooldownLogic.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Weapons
{
    public static class CooldownLogic
    {
        /// <summary>
        /// 공격속도 배율을 쿨타임에 적용한다. 배율이 클수록 쿨타임이 짧아지며,
        /// 무기별 최소 쿨타임 절대값이 하한을 잡는다(무한 연사 방지).
        /// </summary>
        public static float ApplyAttackSpeed(float baseCooldown, float attackSpeedMultiplier, float minCooldown)
        {
            if (attackSpeedMultiplier <= 0f)
                return baseCooldown;

            return Mathf.Max(baseCooldown / attackSpeedMultiplier, minCooldown);
        }
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Expected: 총계 **73개** 전부 통과.

---

### Task 2: PlayerStats

**Files:**
- Create: `unity/Assets/_Project/Scripts/Player/PlayerStats.cs`

**Interfaces:**
- Consumes: `SushiSurvival.Core.StatSystem` / `StatType` / `StatModifier`,
  `SushiSurvival.Data.CharacterData`.
- Produces: `PlayerStats` (MonoBehaviour) —
  `float GetValue(StatType stat)`, `void AddModifier(StatModifier modifier)`.
  Task 3의 모든 소비처와 Task 7(`AugmentOption`)이 사용한다.

- [ ] **Step 1: PlayerStats 작성**

`unity/Assets/_Project/Scripts/Player/PlayerStats.cs`:

```csharp
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.Player
{
    /// <summary>
    /// 플레이어의 모든 스탯이 지나가는 단일 창구. 증강과 호감도 대화 버프가
    /// 같은 캡 예산을 공유하므로, 클램프는 이 안의 StatSystem 한 곳에서만 한다.
    ///
    /// CharacterData를 직접 수정하지 않는 것이 중요하다 — ScriptableObject라
    /// 수정이 에셋 파일에 영구 저장되어 다음 판까지 넘어간다.
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] private CharacterData characterData;
        [Tooltip("자석 증강이 없을 때의 기본 젬 흡수 반경.")]
        [SerializeField] private float baseMagnetRange = 0.5f;

        private readonly StatSystem _stats = new StatSystem();

        private void Awake()
        {
            // 절대값 스탯 — CharacterData 값이 그대로 base가 된다.
            _stats.SetBase(StatType.MoveSpeed, characterData.baseMoveSpeed);
            _stats.SetBase(StatType.MaxHealth, characterData.baseMaxHealth);
            _stats.SetBase(StatType.MagnetRange, baseMagnetRange);

            // 배율 스탯 — 1.0이 기본이고 무기가 자기 테이블 값에 곱한다.
            _stats.SetBase(StatType.AttackDamage, 1f);
            _stats.SetBase(StatType.AttackSpeed, 1f);
            _stats.SetBase(StatType.AttackRange, 1f);
            _stats.SetBase(StatType.ExpGain, 1f);

            // 비율·횟수 스탯
            _stats.SetBase(StatType.Armor, 0f);
            _stats.SetBase(StatType.Regen, 0f);
            _stats.SetBase(StatType.Revive, 0f);

            // 하드캡 (기획서 요구사항)
            _stats.SetCap(StatType.Armor, ArmorLogic.MaxArmor);
            _stats.SetCap(StatType.AttackSpeed, 2f);
            _stats.SetCap(StatType.Revive, 1f);
        }

        public float GetValue(StatType stat) => _stats.GetValue(stat);

        public void AddModifier(StatModifier modifier) => _stats.AddModifier(modifier);
    }
}
```

- [ ] **Step 2: 테스트 실행해서 컴파일 확인**

Expected: 총계 73개 통과, `error CS` 없음.

---

### Task 3: 소비처를 PlayerStats로 연결

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Player/PlayerController.cs`
- Modify: `unity/Assets/_Project/Scripts/Player/PlayerHealth.cs` (전면 교체)
- Modify: `unity/Assets/_Project/Scripts/Weapons/WeaponBase.cs` (전면 교체)
- Modify: `unity/Assets/_Project/Scripts/Core/GameManager.cs` (일부)

**Interfaces:**
- Consumes: `PlayerStats.GetValue` (Task 2), `ArmorLogic.ApplyArmor` /
  `CooldownLogic.ApplyAttackSpeed` (Task 1).
- Produces: `PlayerHealth.CurrentHealth` / `MaxHealth` / `TakeDamage` / `OnDeath`
  (기존 유지) + `OnHealthChanged` 이벤트, `WeaponBase.CanLevelUp` /
  `LevelUp()` / `CurrentLevel` / `WeaponName`.
  Task 7(`WeaponLevelUpOption`)과 Task 11(체력바)이 사용한다.

- [ ] **Step 1: PlayerController가 이동속도를 PlayerStats에서 읽도록 수정**

`unity/Assets/_Project/Scripts/Player/PlayerController.cs`에서 세 곳을 고친다.

클래스 선언부에 `PlayerStats` 요구를 추가:

```csharp
    [RequireComponent(typeof(FacingController))]
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
```

필드에 `_stats`를 추가하고 `Awake`에서 가져온다:

```csharp
        private FacingController _facing;
        private CharacterAnimator _animator;
        private PlayerStats _stats;
        private Rigidbody2D _rigidbody;
        private Vector2 _moveInput;

        private void Awake()
        {
            _facing = GetComponent<FacingController>();
            _animator = GetComponent<CharacterAnimator>();
            _stats = GetComponent<PlayerStats>();
            _rigidbody = GetComponent<Rigidbody2D>();
        }
```

`FixedUpdate`에서 `characterData` 대신 스탯을 읽는다:

```csharp
        private void FixedUpdate()
        {
            float moveSpeed = _stats.GetValue(StatType.MoveSpeed);
            Vector2 velocity = _moveInput.normalized * moveSpeed;
            _rigidbody.MovePosition(_rigidbody.position + velocity * Time.fixedDeltaTime);
        }
```

파일 맨 위 using에 `using SushiSurvival.Core;`를 추가한다(`StatType`이 여기 있다).
`characterData` 필드는 더 이상 쓰이지 않으므로 삭제한다.

- [ ] **Step 2: PlayerHealth 전면 교체**

`unity/Assets/_Project/Scripts/Player/PlayerHealth.cs` 전체를 아래로 교체:

```csharp
using System;
using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.Player
{
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerHealth : MonoBehaviour
    {
        private PlayerStats _stats;
        private float _regenCarry;
        private int _revivesUsed;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => _stats.GetValue(StatType.MaxHealth);

        public event Action OnDeath;
        /// <summary>(현재 체력, 최대 체력) — 체력바가 구독한다.</summary>
        public event Action<float, float> OnHealthChanged;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
        }

        private void Start()
        {
            CurrentHealth = MaxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        private void Update()
        {
            TickRegen();
        }

        /// <summary>
        /// 최대체력 증강을 얻었을 때 현재 체력도 같이 올린다. 그러지 않으면
        /// 최대치만 늘고 체감상 아무 일도 일어나지 않는다.
        /// </summary>
        public void GrantMaxHealthIncrease(float amount)
        {
            CurrentHealth += amount;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void TakeDamage(float damage)
        {
            if (HealthLogic.IsDead(CurrentHealth)) return;

            float reduced = ArmorLogic.ApplyArmor(damage, _stats.GetValue(StatType.Armor));
            CurrentHealth = HealthLogic.ApplyDamage(CurrentHealth, reduced);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (!HealthLogic.IsDead(CurrentHealth)) return;

            if (TryRevive()) return;

            OnDeath?.Invoke();
        }

        private bool TryRevive()
        {
            int allowedRevives = Mathf.FloorToInt(_stats.GetValue(StatType.Revive));
            if (_revivesUsed >= allowedRevives) return false;

            _revivesUsed++;
            CurrentHealth = MaxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            Debug.Log($"[PlayerHealth] 부활! ({_revivesUsed}/{allowedRevives})");
            return true;
        }

        private void TickRegen()
        {
            if (HealthLogic.IsDead(CurrentHealth)) return;

            float regenPerSecond = _stats.GetValue(StatType.Regen);
            if (regenPerSecond <= 0f) return;

            // 초당 회복량이 작아 프레임당 값이 0에 가까우므로 누적해서 적용한다.
            _regenCarry += regenPerSecond * Time.deltaTime;
            if (_regenCarry < 0.01f) return;

            CurrentHealth = Mathf.Min(CurrentHealth + _regenCarry, MaxHealth);
            _regenCarry = 0f;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }
    }
}
```

`characterData` 필드가 사라진다 — 최대체력은 이제 `PlayerStats`에서 온다.
초기화를 `Awake`가 아닌 `Start`에서 하는 이유는, `PlayerStats.Awake`가 먼저
실행되어 base 값이 세팅된 뒤여야 최대체력을 읽을 수 있기 때문이다.

- [ ] **Step 3: WeaponBase 전면 교체**

`unity/Assets/_Project/Scripts/Weapons/WeaponBase.cs` 전체를 아래로 교체:

```csharp
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;
using SushiSurvival.Player;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 무기 공통부 — 쿨타임 타이머, 레벨별 수치 조회, 공격 애니메이션 트리거,
    /// 증강 배율 적용. 각 무기는 Attack()만 구현한다.
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        [SerializeField] protected WeaponData weaponData;
        [Tooltip("계란·간장새우는 무기 오브젝트(WeaponVisual)의 것을, 이나리는 캐릭터 본체의 것을 연결한다.")]
        [SerializeField] protected AttackAnimator attackAnimator;
        [Tooltip("1-based (1~4)")]
        [SerializeField] protected int currentLevel = 1;
        [Tooltip("공격속도 증강이 아무리 쌓여도 이 값보다 짧아지지 않는다(무한 연사 방지).")]
        [SerializeField] protected float minCooldown = 0.2f;
        [SerializeField] protected PlayerStats playerStats;

        private readonly WeaponCooldown _cooldown = new WeaponCooldown();

        public int CurrentLevel => currentLevel;
        public string WeaponName => weaponData != null ? weaponData.weaponName : string.Empty;
        public bool CanLevelUp => weaponData != null && currentLevel < weaponData.levels.Length;

        protected WeaponLevelStats BaseStats => weaponData.levels[currentLevel - 1];

        /// <summary>증강 배율이 적용된 최종 데미지.</summary>
        protected float Damage => BaseStats.damage * StatMultiplier(StatType.AttackDamage);

        /// <summary>증강 배율이 적용된 최종 사거리/반경.</summary>
        protected float Range => BaseStats.range * StatMultiplier(StatType.AttackRange);

        public void LevelUp()
        {
            if (!CanLevelUp) return;
            currentLevel++;
        }

        private float StatMultiplier(StatType stat)
            => playerStats != null ? playerStats.GetValue(stat) : 1f;

        private void Update()
        {
            _cooldown.Tick(Time.deltaTime);
            if (!_cooldown.IsReady) return;

            attackAnimator?.TriggerAttack();
            Attack();

            float cooldown = CooldownLogic.ApplyAttackSpeed(
                BaseStats.cooldown, StatMultiplier(StatType.AttackSpeed), minCooldown);
            _cooldown.Reset(cooldown);
        }

        protected abstract void Attack();
    }
}
```

`CurrentStats`가 `BaseStats`로 이름이 바뀌고, 배율이 적용된 `Damage` / `Range`가
새로 생겼다.

- [ ] **Step 4: EggFanWeapon과 ShrimpRifleWeapon을 새 프로퍼티에 맞춰 수정**

`unity/Assets/_Project/Scripts/Weapons/EggFanWeapon.cs`의 `Attack()` 전체를 교체:

```csharp
        protected override void Attack()
        {
            float range = Range;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, enemyLayer);
            foreach (var hit in hits)
            {
                if (!hit.TryGetComponent<EnemyBase>(out var enemy)) continue;

                if (FanHitTest.IsInsideFan(transform.position, facing.CurrentFacing, range, BaseStats.angleDegrees, enemy.transform.position))
                    enemy.TakeDamage(Damage);
            }
        }
```

`unity/Assets/_Project/Scripts/Weapons/ShrimpRifleWeapon.cs`의 `Attack()`에서
`var stats = CurrentStats;` 줄을 삭제하고, 아래 두 줄을 그에 맞게 고친다:

```csharp
            if (projectileObj.TryGetComponent<Projectile>(out var projectile))
                projectile.Initialize(direction, Damage, BaseStats.pierceCount, _projectilePool);
```

- [ ] **Step 5: GameManager가 경험치 배율을 적용하도록 수정**

`unity/Assets/_Project/Scripts/Core/GameManager.cs`에서 `StartRun` 안의 스폰 직후에
플레이어 스탯을 보관하도록 필드와 대입을 추가한다.

필드 추가:

```csharp
        private PlayerHealth _playerHealth;
        private PlayerStats _playerStats;
```

`StartRun`에서 `_playerHealth` 대입 바로 아래에 추가:

```csharp
            _playerStats = player.GetComponent<PlayerStats>();
```

`AddExperience`를 아래로 교체:

```csharp
        public void AddExperience(float amount)
        {
            if (CurrentState != RunState.Playing) return;

            float multiplier = _playerStats != null ? _playerStats.GetValue(StatType.ExpGain) : 1f;
            TotalExperience += amount * multiplier;
            Debug.Log($"[GameManager] 누적 경험치: {TotalExperience}");
        }
```

- [ ] **Step 6: 테스트 실행해서 컴파일·통과 확인**

Expected: 총계 73개 통과, `error CS` 없음.

---

### Task 4: Unity Editor 작업 — PlayerStats 부착 및 회귀 확인

Unity를 열고 진행한다.

- [ ] **Step 1: 두 캐릭터 프리팹에 PlayerStats 추가**

`EggPlayer`, `ShrimpPlayer` 프리팹 각각에 대해:
1. 프리팹 더블클릭
2. 루트에 `Player Stats` 컴포넌트 추가
   - `Character Data` ← 해당 캐릭터의 데이터 에셋
   - `Base Magnet Range` = 0.5
3. `Player Controller`에서 사라진 `Character Data` 필드는 신경 쓰지 않아도 된다
   (이제 `Move Action`만 남는다)
4. `Player Health`에서도 `Character Data` 필드가 사라진다
5. **`Egg Fan Weapon`(또는 `Shrimp Rifle Weapon`)의 새 필드 두 개를 채운다:**
   - `Min Cooldown` = 0.2
   - `Player Stats` ← 같은 오브젝트의 `Player Stats` 드래그

- [ ] **Step 2: 회귀 플레이테스트**

Play 모드에서 확인:
1. 계란·간장새우 둘 다 이전과 동일하게 이동·공격한다
2. 잡몹이 죽고 젬이 흡수되며 경험치 로그가 뜬다
3. 피격당하면 체력이 줄고 사망 시 `GAME OVER`가 뜬다
4. Console에 에러가 없다

증강이 아직 없으므로 모든 배율이 1.0이고, 체감상 이전과 완전히 같아야 한다.

---

## Phase 2 — 레벨업 + 3택 팝업

### Task 5: LevelCurve (TDD)

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/LevelCurve.cs`
- Test: `unity/Assets/Tests/EditMode/LevelCurveTests.cs`

**Interfaces:**
- Produces: `struct LevelProgress { float XpTowardNext; int LevelsGained; }`,
  `LevelCurve.GetRequiredXp(int level, float baseXp, float increment) -> float`,
  `LevelCurve.Resolve(float xpTowardNext, int currentLevel, float baseXp, float increment) -> LevelProgress`.
  Task 8(`LevelSystem`)이 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/LevelCurveTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class LevelCurveTests
    {
        [Test]
        public void GetRequiredXp_ReturnsBase_AtLevelOne()
        {
            Assert.That(LevelCurve.GetRequiredXp(1, 5f, 3f), Is.EqualTo(5f).Within(0.0001f));
        }

        [Test]
        public void GetRequiredXp_GrowsByIncrementPerLevel()
        {
            Assert.That(LevelCurve.GetRequiredXp(3, 5f, 3f), Is.EqualTo(11f).Within(0.0001f));
        }

        [Test]
        public void Resolve_GainsNothing_BelowThreshold()
        {
            var result = LevelCurve.Resolve(3f, 1, 5f, 3f);

            Assert.AreEqual(0, result.LevelsGained);
            Assert.That(result.XpTowardNext, Is.EqualTo(3f).Within(0.0001f));
        }

        [Test]
        public void Resolve_GainsOneLevel_ExactlyAtThreshold()
        {
            var result = LevelCurve.Resolve(5f, 1, 5f, 3f);

            Assert.AreEqual(1, result.LevelsGained);
            Assert.That(result.XpTowardNext, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void Resolve_CarriesRemainder()
        {
            var result = LevelCurve.Resolve(7f, 1, 5f, 3f);

            Assert.AreEqual(1, result.LevelsGained);
            Assert.That(result.XpTowardNext, Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void Resolve_GainsMultipleLevels_FromOneBigGem()
        {
            // Lv1 필요 5, Lv2 필요 8 → 합계 13. 15를 넣으면 2레벨 오르고 2 남는다.
            var result = LevelCurve.Resolve(15f, 1, 5f, 3f);

            Assert.AreEqual(2, result.LevelsGained);
            Assert.That(result.XpTowardNext, Is.EqualTo(2f).Within(0.0001f));
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Expected: `LevelCurve`가 없어서 컴파일 실패.

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/Core/LevelCurve.cs`:

```csharp
namespace SushiSurvival.Core
{
    public struct LevelProgress
    {
        public float XpTowardNext;
        public int LevelsGained;
    }

    public static class LevelCurve
    {
        /// <summary>다음 레벨까지 필요한 경험치. 레벨이 오를수록 선형으로 늘어난다.</summary>
        public static float GetRequiredXp(int level, float baseXp, float increment)
            => baseXp + increment * (level - 1);

        /// <summary>
        /// 누적된 경험치로 몇 레벨이 오르는지 계산한다. 황금 젬 하나로 2~3레벨이
        /// 한 번에 오를 수 있으므로 반복 처리한다.
        /// </summary>
        public static LevelProgress Resolve(float xpTowardNext, int currentLevel, float baseXp, float increment)
        {
            int gained = 0;
            int level = currentLevel;
            float remaining = xpTowardNext;

            while (true)
            {
                float required = GetRequiredXp(level, baseXp, increment);
                if (required <= 0f || remaining < required) break;

                remaining -= required;
                level++;
                gained++;
            }

            return new LevelProgress { XpTowardNext = remaining, LevelsGained = gained };
        }
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Expected: 총계 **79개** 전부 통과.

---

### Task 6: AugmentAvailability + UpgradePicker (TDD)

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/AugmentAvailability.cs`
- Create: `unity/Assets/_Project/Scripts/Core/UpgradePicker.cs`
- Test: `unity/Assets/Tests/EditMode/AugmentAvailabilityTests.cs`
- Test: `unity/Assets/Tests/EditMode/UpgradePickerTests.cs`

**Interfaces:**
- Produces: `AugmentAvailability.IsAvailable(float accumulated, float maxCap) -> bool`,
  `UpgradePicker.PickDistinct<T>(IReadOnlyList<T> candidates, int count, System.Random random) -> List<T>`.
  Task 8(`LevelSystem`)이 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/AugmentAvailabilityTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class AugmentAvailabilityTests
    {
        [Test]
        public void IsAvailable_True_WhenNothingTakenYet()
        {
            Assert.IsTrue(AugmentAvailability.IsAvailable(0f, 2f));
        }

        [Test]
        public void IsAvailable_True_WhenPartiallyTaken()
        {
            Assert.IsTrue(AugmentAvailability.IsAvailable(1.4f, 2f));
        }

        [Test]
        public void IsAvailable_False_AtCap()
        {
            Assert.IsFalse(AugmentAvailability.IsAvailable(2f, 2f));
        }

        [Test]
        public void IsAvailable_False_BeyondCap()
        {
            Assert.IsFalse(AugmentAvailability.IsAvailable(2.5f, 2f));
        }
    }
}
```

`unity/Assets/Tests/EditMode/UpgradePickerTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class UpgradePickerTests
    {
        [Test]
        public void PickDistinct_ReturnsRequestedCount()
        {
            var candidates = new List<string> { "a", "b", "c", "d", "e" };

            var result = UpgradePicker.PickDistinct(candidates, 3, new System.Random(1));

            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void PickDistinct_ReturnsNoDuplicates()
        {
            var candidates = new List<string> { "a", "b", "c", "d", "e" };

            var result = UpgradePicker.PickDistinct(candidates, 3, new System.Random(1));

            CollectionAssert.AllItemsAreUnique(result);
        }

        [Test]
        public void PickDistinct_ReturnsAll_WhenFewerCandidatesThanRequested()
        {
            var candidates = new List<string> { "a", "b" };

            var result = UpgradePicker.PickDistinct(candidates, 3, new System.Random(1));

            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void PickDistinct_ReturnsEmpty_WhenNoCandidates()
        {
            var result = UpgradePicker.PickDistinct(new List<string>(), 3, new System.Random(1));

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void PickDistinct_DoesNotModifySourceList()
        {
            var candidates = new List<string> { "a", "b", "c", "d", "e" };

            UpgradePicker.PickDistinct(candidates, 3, new System.Random(1));

            Assert.AreEqual(5, candidates.Count);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Expected: 두 클래스가 없어서 컴파일 실패.

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/Core/AugmentAvailability.cs`:

```csharp
namespace SushiSurvival.Core
{
    public static class AugmentAvailability
    {
        /// <summary>
        /// 누적값이 상한에 닿으면 후보에서 뺀다. 마지막 한 번이 상한을 살짝
        /// 넘기는 것은 허용한다 — StatSystem이 어차피 클램프한다.
        /// </summary>
        public static bool IsAvailable(float accumulated, float maxCap)
            => accumulated < maxCap;
    }
}
```

`unity/Assets/_Project/Scripts/Core/UpgradePicker.cs`:

```csharp
using System.Collections.Generic;

namespace SushiSurvival.Core
{
    public static class UpgradePicker
    {
        /// <summary>
        /// 후보에서 중복 없이 count개를 무작위로 뽑는다. 후보가 모자라면
        /// 있는 만큼만 돌려준다. 원본 리스트는 건드리지 않는다.
        /// </summary>
        public static List<T> PickDistinct<T>(IReadOnlyList<T> candidates, int count, System.Random random)
        {
            var pool = new List<T>(candidates);
            var picked = new List<T>();

            int take = count < pool.Count ? count : pool.Count;
            for (int i = 0; i < take; i++)
            {
                int index = random.Next(pool.Count);
                picked.Add(pool[index]);
                pool.RemoveAt(index);
            }

            return picked;
        }
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Expected: 총계 **88개** 전부 통과.

---

### Task 7: 업그레이드 선택지 타입

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/IUpgradeOption.cs`
- Create: `unity/Assets/_Project/Scripts/Core/AugmentOption.cs`
- Create: `unity/Assets/_Project/Scripts/Core/WeaponLevelUpOption.cs`
- Modify: `unity/Assets/_Project/Scripts/Data/AugmentData.cs`

**Interfaces:**
- Consumes: `PlayerStats.AddModifier` (Task 2), `WeaponBase.LevelUp` (Task 3),
  `PlayerHealth.GrantMaxHealthIncrease` (Task 3).
- Produces: `interface IUpgradeOption { string DisplayName { get; } Sprite Icon { get; } void Apply(); }`,
  `class AugmentOption : IUpgradeOption` (생성자 `(AugmentData, PlayerStats, PlayerHealth)`,
  프로퍼티 `AugmentData Data { get; }`),
  `class WeaponLevelUpOption : IUpgradeOption` (생성자 `(WeaponBase)`).
  Task 8(`LevelSystem`)이 사용한다.

- [ ] **Step 1: AugmentData에 아이콘과 설명 추가**

`unity/Assets/_Project/Scripts/Data/AugmentData.cs` 전체를 아래로 교체:

```csharp
using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.Data
{
    [CreateAssetMenu(menuName = "SushiSurvival/Augment Data", fileName = "NewAugmentData")]
    public class AugmentData : ScriptableObject
    {
        public string augmentName;
        public Sprite icon;
        public StatType statType;
        [Tooltip("한 번 고를 때마다 더해지는 값. 배율 스탯이면 0.2 = +20%.")]
        public float valuePerPick;
        [Tooltip("누적 상한. 기획서의 추천 최대치를 넣는다(공격력 +200%면 2.0).")]
        public float maxCap;
    }
}
```

- [ ] **Step 2: IUpgradeOption과 두 구현체 작성**

`unity/Assets/_Project/Scripts/Core/IUpgradeOption.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 레벨업 3택에 오르는 선택지. 증강과 무기 강화가 같은 풀에서 뽑히도록
    /// 하나의 인터페이스로 묶는다.
    /// </summary>
    public interface IUpgradeOption
    {
        string DisplayName { get; }
        Sprite Icon { get; }
        void Apply();
    }
}
```

`unity/Assets/_Project/Scripts/Core/AugmentOption.cs`:

```csharp
using UnityEngine;
using SushiSurvival.Data;
using SushiSurvival.Player;

namespace SushiSurvival.Core
{
    public class AugmentOption : IUpgradeOption
    {
        private readonly PlayerStats _stats;
        private readonly PlayerHealth _health;

        public AugmentData Data { get; }

        public string DisplayName => Data.augmentName;
        public Sprite Icon => Data.icon;

        public AugmentOption(AugmentData data, PlayerStats stats, PlayerHealth health)
        {
            Data = data;
            _stats = stats;
            _health = health;
        }

        public void Apply()
        {
            _stats.AddModifier(new StatModifier
            {
                Stat = Data.statType,
                Type = ModifierType.Additive,
                Value = Data.valuePerPick
            });

            // 최대체력 증강은 현재 체력도 같이 올려야 체감이 된다.
            if (Data.statType == StatType.MaxHealth && _health != null)
                _health.GrantMaxHealthIncrease(Data.valuePerPick);
        }
    }
}
```

`unity/Assets/_Project/Scripts/Core/WeaponLevelUpOption.cs`:

```csharp
using UnityEngine;
using SushiSurvival.Weapons;

namespace SushiSurvival.Core
{
    public class WeaponLevelUpOption : IUpgradeOption
    {
        private readonly WeaponBase _weapon;

        public string DisplayName => $"{_weapon.WeaponName} 강화 Lv{_weapon.CurrentLevel + 1}";
        public Sprite Icon { get; }

        public WeaponLevelUpOption(WeaponBase weapon, Sprite icon)
        {
            _weapon = weapon;
            Icon = icon;
        }

        public void Apply() => _weapon.LevelUp();
    }
}
```

- [ ] **Step 3: 테스트 실행해서 컴파일 확인**

Expected: 총계 88개 통과, `error CS` 없음.

---

### Task 8: LevelSystem

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/LevelSystem.cs`
- Modify: `unity/Assets/_Project/Scripts/Core/GameManager.cs`

**Interfaces:**
- Consumes: `LevelCurve.Resolve` (Task 5), `AugmentAvailability.IsAvailable` /
  `UpgradePicker.PickDistinct` (Task 6), `IUpgradeOption` 구현체들 (Task 7),
  `LevelUpPanel.Show` / `Hide` (Task 9).
- Produces: `LevelSystem` (MonoBehaviour) —
  `void SetPlayer(PlayerStats, PlayerHealth, WeaponBase)`, `void AddExperience(float)`,
  `int CurrentLevel { get; }`.
  `GameManager`가 호출한다.

- [ ] **Step 1: LevelSystem 작성**

`unity/Assets/_Project/Scripts/Core/LevelSystem.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Data;
using SushiSurvival.Player;
using SushiSurvival.UI;
using SushiSurvival.Weapons;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 경험치 누적 → 레벨업 → 3택 팝업 → 적용까지를 관장한다.
    /// 황금 젬으로 여러 레벨이 한 번에 오를 수 있으므로 대기 큐를 둔다.
    /// </summary>
    public class LevelSystem : MonoBehaviour
    {
        private const int OptionCount = 3;

        [SerializeField] private LevelUpPanel panel;
        [SerializeField] private AugmentData[] augments;
        [Tooltip("무기 강화 선택지에 쓸 아이콘. 비워도 동작한다.")]
        [SerializeField] private Sprite weaponUpgradeIcon;
        [Tooltip("Lv1에서 다음 레벨까지 필요한 경험치.")]
        [SerializeField] private float baseXp = 5f;
        [Tooltip("레벨이 오를 때마다 필요 경험치에 더해지는 값.")]
        [SerializeField] private float xpIncrementPerLevel = 3f;

        public int CurrentLevel { get; private set; } = 1;

        private readonly Dictionary<AugmentData, float> _accumulated = new Dictionary<AugmentData, float>();
        private readonly System.Random _random = new System.Random();

        private PlayerStats _playerStats;
        private PlayerHealth _playerHealth;
        private WeaponBase _weapon;

        private float _xpTowardNext;
        private int _pendingLevelUps;
        private bool _panelOpen;

        public void SetPlayer(PlayerStats stats, PlayerHealth health, WeaponBase weapon)
        {
            _playerStats = stats;
            _playerHealth = health;
            _weapon = weapon;
        }

        public void AddExperience(float amount)
        {
            _xpTowardNext += amount;

            var progress = LevelCurve.Resolve(_xpTowardNext, CurrentLevel, baseXp, xpIncrementPerLevel);
            _xpTowardNext = progress.XpTowardNext;

            if (progress.LevelsGained <= 0) return;

            CurrentLevel += progress.LevelsGained;
            _pendingLevelUps += progress.LevelsGained;
            Debug.Log($"[LevelSystem] 레벨업! 현재 Lv{CurrentLevel} (대기 {_pendingLevelUps})");

            if (!_panelOpen)
                ShowNext();
        }

        private void ShowNext()
        {
            while (_pendingLevelUps > 0)
            {
                _pendingLevelUps--;

                List<IUpgradeOption> options = BuildOptions();
                if (options.Count == 0)
                {
                    // 모든 증강이 최대치이고 무기도 4강이면 고를 게 없다.
                    // 빈 팝업으로 게임이 멈추지 않도록 조용히 소비한다.
                    continue;
                }

                _panelOpen = true;
                Time.timeScale = 0f;
                panel.Show(options, OnOptionChosen);
                return;
            }

            CloseAndResume();
        }

        private void OnOptionChosen(IUpgradeOption option)
        {
            option.Apply();

            if (option is AugmentOption augmentOption)
            {
                var data = augmentOption.Data;
                _accumulated.TryGetValue(data, out float current);
                _accumulated[data] = current + data.valuePerPick;
            }

            panel.Hide();
            _panelOpen = false;

            ShowNext();
        }

        private void CloseAndResume()
        {
            panel.Hide();
            _panelOpen = false;
            Time.timeScale = 1f;
        }

        private List<IUpgradeOption> BuildOptions()
        {
            var candidates = new List<IUpgradeOption>();

            if (_weapon != null && _weapon.CanLevelUp)
                candidates.Add(new WeaponLevelUpOption(_weapon, weaponUpgradeIcon));

            foreach (var augment in augments)
            {
                if (augment == null) continue;

                _accumulated.TryGetValue(augment, out float current);
                if (!AugmentAvailability.IsAvailable(current, augment.maxCap)) continue;

                candidates.Add(new AugmentOption(augment, _playerStats, _playerHealth));
            }

            return UpgradePicker.PickDistinct(candidates, OptionCount, _random);
        }
    }
}
```

- [ ] **Step 2: GameManager가 LevelSystem에 경험치를 넘기도록 수정**

`unity/Assets/_Project/Scripts/Core/GameManager.cs`에서 세 곳을 고친다.

필드 추가:

```csharp
        [SerializeField] private LevelSystem levelSystem;
```

`StartRun`의 `_playerStats` 대입 아래에 추가:

```csharp
            var weapon = player.GetComponent<WeaponBase>();
            levelSystem.SetPlayer(_playerStats, _playerHealth, weapon);
```

`AddExperience` 끝에 `levelSystem` 전달을 추가:

```csharp
        public void AddExperience(float amount)
        {
            if (CurrentState != RunState.Playing) return;

            float multiplier = _playerStats != null ? _playerStats.GetValue(StatType.ExpGain) : 1f;
            float gained = amount * multiplier;

            TotalExperience += gained;
            levelSystem.AddExperience(gained);
        }
```

`HandlePlayerDeath`에 timeScale 복구를 추가한다. 레벨업 팝업이 열린 채로 런이
끝나는 경우에도 게임이 멈춘 상태로 남지 않게 하기 위함이다:

```csharp
        private void HandlePlayerDeath()
        {
            CurrentState = RunState.GameOver;
            enemySpawner.StopSpawning();
            Time.timeScale = 1f;
            Debug.Log("GAME OVER");
        }
```

파일 맨 위 using에 `using SushiSurvival.Weapons;`를 추가한다.

- [ ] **Step 3: 테스트 실행해서 컴파일 확인**

Expected: 총계 88개 통과. `LevelUpPanel`이 아직 없으므로 이 시점에는 컴파일
실패가 정상이다 — Task 9를 완료한 뒤 이 Step으로 돌아와 재확인한다.

---

### Task 9: LevelUpPanel UI

**Files:**
- Create: `unity/Assets/_Project/Scripts/UI/LevelUpPanel.cs`
- Create: `unity/Assets/_Project/Scripts/UI/LevelUpOptionButton.cs`

**Interfaces:**
- Consumes: `IUpgradeOption` (Task 7).
- Produces: `LevelUpPanel` — `void Show(IReadOnlyList<IUpgradeOption> options, Action<IUpgradeOption> onChosen)`,
  `void Hide()`. `LevelUpOptionButton` — `void Bind(IUpgradeOption option, Action<IUpgradeOption> onChosen)`,
  `void Clear()`.

- [ ] **Step 1: LevelUpOptionButton 작성**

`unity/Assets/_Project/Scripts/UI/LevelUpOptionButton.cs`:

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    [RequireComponent(typeof(Button))]
    public class LevelUpOptionButton : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameText;

        private Button _button;
        private IUpgradeOption _option;
        private Action<IUpgradeOption> _onChosen;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);
        }

        public void Bind(IUpgradeOption option, Action<IUpgradeOption> onChosen)
        {
            _option = option;
            _onChosen = onChosen;

            gameObject.SetActive(true);

            if (nameText != null)
                nameText.text = option.DisplayName;

            if (iconImage != null)
            {
                iconImage.sprite = option.Icon;
                iconImage.enabled = option.Icon != null;
            }
        }

        public void Clear()
        {
            _option = null;
            _onChosen = null;
            gameObject.SetActive(false);
        }

        private void HandleClick()
        {
            if (_option == null) return;

            _onChosen?.Invoke(_option);
        }
    }
}
```

- [ ] **Step 2: LevelUpPanel 작성**

`unity/Assets/_Project/Scripts/UI/LevelUpPanel.cs`:

```csharp
using System;
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

        private GameObject Root => root != null ? root : gameObject;

        private void Awake() => Hide();

        public void Show(IReadOnlyList<IUpgradeOption> options, Action<IUpgradeOption> onChosen)
        {
            Root.SetActive(true);

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < options.Count)
                    optionButtons[i].Bind(options[i], onChosen);
                else
                    optionButtons[i].Clear();
            }
        }

        public void Hide() => Root.SetActive(false);
    }
}
```

- [ ] **Step 3: 테스트 실행해서 컴파일 확인**

이제 Task 8 Step 3의 컴파일도 통과해야 한다.
Expected: 총계 88개 통과, `error CS` 없음.

---

### Task 10: Unity Editor 작업 — 증강 에셋과 팝업 구성

Unity를 열고 진행한다.

- [ ] **Step 1: 증강 아이콘 임포트 설정**

`Assets/Art/캐릭터/캐릭터/증강` 폴더의 PNG 10개를 모두 선택하고
`Texture Type` = `Sprite (2D and UI)`, `Sprite Mode` = `Single` → Apply.

- [ ] **Step 2: 증강 데이터 에셋 10개 생성**

`Assets/_Project/Data`에 `Augments` 하위 폴더를 만들고, 각각
`Create > SushiSurvival > Augment Data`로 생성한다. 값은 아래 표대로 입력한다.

`Value Per Pick`은 캡을 10번에 나눠 채우도록 잡은 제안값이다(부활 제외).
플레이테스트로 조정한다.

| 에셋 이름 | augmentName | statType | valuePerPick | maxCap | icon |
|---|---|---|---|---|---|
| `Aug_AttackDamage` | 공격력 | AttackDamage | 0.2 | 2.0 | 공격력.png |
| `Aug_AttackSpeed` | 공격속도 | AttackSpeed | 0.1 | 1.0 | 공격속도.png |
| `Aug_AttackRange` | 공격범위 | AttackRange | 0.1 | 1.0 | 공격범위.png |
| `Aug_MaxHealth` | 체력 | MaxHealth | 30 | 300 | 체력.png |
| `Aug_Armor` | 방어력 | Armor | 0.05 | 0.5 | 방어력.png |
| `Aug_Regen` | 회복 | Regen | 0.5 | 5 | 회복.png |
| `Aug_MoveSpeed` | 이동속도 | MoveSpeed | 0.12 | 1.2 | 이동속도.png |
| `Aug_Magnet` | 자석 | MagnetRange | 0.15 | 1.5 | 자석.png |
| `Aug_ExpGain` | 경험치 획득량 | ExpGain | 0.06 | 0.6 | 경험치.png |
| `Aug_Revive` | 부활 | Revive | 1 | 1 | 부활.png |

수치 근거(기획서 추천 최대치):
- 공격력 +200% → 배율 base 1.0에 최대 +2.0
- 공격속도 -50% 쿨감 → 속도 배율 최대 2.0이므로 base 1.0에 최대 +1.0
- 공격범위 +100% → 최대 +1.0
- 체력 기본값 x3~4 → 기본 100 기준 +300
- 방어력 50% → 최대 +0.5 (하드캡과 동일)
- 이동속도 +40%... 단, `MoveSpeed`는 절대값 스탯이므로 기본 3의 40%인 +1.2
- 자석 +300% → 기본 반경 0.5의 300%인 +1.5
- 경험치 +60% → 최대 +0.6
- 회복 최대체력의 3~5%/초 → 기본 100 기준 초당 5까지
- 부활 1회

- [ ] **Step 3: 레벨업 팝업 UI 만들기**

1. `Canvas` 아래에 빈 GameObject → 이름 `LevelUpPanel`
   - Rect Transform을 화면 전체로 늘린다(Anchor Presets stretch-stretch)
   - 배경이 필요하면 `UI > Image`를 자식으로 깔고 색 알파를 낮춘다
2. `LevelUpPanel` 아래에 `UI > Button (Legacy)` 3개 → `Option_0`, `Option_1`,
   `Option_2`. 가로로 배치한다.
3. 각 버튼에 `Level Up Option Button` 컴포넌트 추가
   - `Icon Image` ← 버튼 자신의 `Image`
   - `Name Text` ← 버튼 자식의 `Text (Legacy)`
4. `LevelUpPanel`에 `Level Up Panel` 컴포넌트 추가
   - `Root` ← 비워둠(자기 자신을 켜고 끈다)
   - `Option Buttons` 배열 크기 3, 위 버튼 3개를 순서대로 넣는다

- [ ] **Step 4: LevelSystem 배치와 배선**

1. 빈 GameObject `LevelSystem` 생성 → `Level System` 컴포넌트 추가
   - `Panel` ← 씬의 `LevelUpPanel`
   - `Augments` 배열 크기 10, Step 2에서 만든 에셋 10개를 넣는다
   - `Weapon Upgrade Icon` ← `공격력.png`(임시) 또는 비워둠
   - `Base Xp` = 5, `Xp Increment Per Level` = 3
2. `GameManager` 선택 → 새로 생긴 `Level System` 필드에 위 오브젝트를 연결

- [ ] **Step 5: 플레이테스트**

Play 모드에서 확인:
1. 젬을 몇 개 먹으면 레벨업 로그가 뜨고 팝업이 열린다
2. **팝업이 열린 동안 게임이 멈춘다** (몹이 움직이지 않고 공격도 나가지 않음)
3. 선택지 3개에 아이콘과 이름이 보인다
4. 하나를 고르면 팝업이 닫히고 게임이 재개된다
5. 무기 강화를 골라 4번 반복하면 그 뒤로는 무기 강화가 후보에 안 나온다
6. 공격력 증강을 여러 번 고르면 잡몹이 눈에 띄게 빨리 죽는다
7. 황금 젬(10XP)으로 2레벨이 한 번에 오르면 팝업이 연달아 두 번 뜬다
8. Console에 에러가 없다

---

## Phase 3 — 특수 증강 + 체력바 + 캘리포니아롤

### Task 11: HealthBarLogic (TDD) + HealthBar 컴포넌트

**Files:**
- Create: `unity/Assets/_Project/Scripts/UI/HealthBarLogic.cs`
- Create: `unity/Assets/_Project/Scripts/UI/HealthBar.cs`
- Test: `unity/Assets/Tests/EditMode/HealthBarLogicTests.cs`

**Interfaces:**
- Consumes: `PlayerHealth.OnHealthChanged` (Task 3).
- Produces: `HealthBarLogic.ComputeFillAmount(float current, float max) -> float`,
  `HealthBar` (MonoBehaviour).

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/HealthBarLogicTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.UI;

namespace SushiSurvival.EditModeTests
{
    public class HealthBarLogicTests
    {
        [Test]
        public void ComputeFillAmount_FullAtMaxHealth()
        {
            Assert.That(HealthBarLogic.ComputeFillAmount(100f, 100f), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void ComputeFillAmount_HalfAtHalfHealth()
        {
            Assert.That(HealthBarLogic.ComputeFillAmount(50f, 100f), Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void ComputeFillAmount_ZeroWhenDead()
        {
            Assert.That(HealthBarLogic.ComputeFillAmount(0f, 100f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ComputeFillAmount_ClampsNegativeHealthToZero()
        {
            Assert.That(HealthBarLogic.ComputeFillAmount(-20f, 100f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ComputeFillAmount_ClampsOverfillToOne()
        {
            Assert.That(HealthBarLogic.ComputeFillAmount(150f, 100f), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void ComputeFillAmount_ReturnsZero_WhenMaxIsZero()
        {
            // 0으로 나누어 NaN이 되는 것을 막는다.
            Assert.That(HealthBarLogic.ComputeFillAmount(10f, 0f), Is.EqualTo(0f).Within(0.0001f));
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Expected: `HealthBarLogic`이 없어서 컴파일 실패.

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/UI/HealthBarLogic.cs`:

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
    }
}
```

`unity/Assets/_Project/Scripts/UI/HealthBar.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Player;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 캐릭터 발밑에 붙는 체력바. PlayerHealth의 변경 이벤트만 구독하므로
    /// 매 프레임 폴링하지 않는다.
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private PlayerHealth playerHealth;
        [Tooltip("Image Type을 Filled로 설정한 채움 이미지.")]
        [SerializeField] private Image fillImage;

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

        private void HandleHealthChanged(float current, float max)
        {
            if (fillImage == null) return;

            fillImage.fillAmount = HealthBarLogic.ComputeFillAmount(current, max);
        }
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Expected: 총계 **94개** 전부 통과.

---

### Task 12: 자석 증강을 XPGem에 연결

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Pickups/XPGem.cs`

**Interfaces:**
- Consumes: `PlayerStats.GetValue(StatType.MagnetRange)` (Task 2).

회복과 부활은 Task 3의 `PlayerHealth`에서 이미 구현했다. 남은 것은 자석이다.

- [ ] **Step 1: XPGem이 플레이어 자석 스탯을 읽도록 수정**

`unity/Assets/_Project/Scripts/Pickups/XPGem.cs`에서 `pickupRadius` 필드를 지우고
플레이어 스탯을 참조하도록 고친다. 수정 후 전체:

```csharp
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Player;

namespace SushiSurvival.Pickups
{
    public class XPGem : MonoBehaviour
    {
        [Tooltip("플레이어를 찾지 못했을 때 쓰는 예비 반경.")]
        [SerializeField] private float fallbackPickupRadius = 0.5f;
        [SerializeField] private float xpValue = 1f; // 슬라이스1: 기본(흰색) 등급 고정값

        private GameObjectPool _selfPool;
        private Transform _player;
        private PlayerStats _playerStats;

        private void Awake()
        {
            // GameObjectPool.CreateInstance가 Instantiate(prefab, transform)으로
            // 생성하므로, 부모를 타고 올라가면 항상 자기 풀을 찾을 수 있다.
            _selfPool = GetComponentInParent<GameObjectPool>();
        }

        private void OnEnable()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                _player = null;
                _playerStats = null;
                return;
            }

            _player = playerObj.transform;
            _playerStats = playerObj.GetComponent<PlayerStats>();
        }

        private void Update()
        {
            if (_player == null) return;

            float radius = _playerStats != null
                ? _playerStats.GetValue(StatType.MagnetRange)
                : fallbackPickupRadius;

            if (!PickupUtility.IsWithinPickupRadius(transform.position, _player.position, radius)) return;

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

- [ ] **Step 2: 테스트 실행해서 컴파일 확인**

Expected: 총계 94개 통과.

---

### Task 13: EnemySpawner 잡몹 2종 지원

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Enemies/EnemySpawner.cs`

**Interfaces:**
- Produces: `EnemySpawner`가 `enemyPools` 배열을 갖는다(기존 단일 `enemyPool` 대체).

- [ ] **Step 1: EnemySpawner를 풀 배열로 수정**

`unity/Assets/_Project/Scripts/Enemies/EnemySpawner.cs`에서 필드와 `SpawnOne`을
고친다. 필드:

```csharp
        [Tooltip("잡몹 종류별 풀. 매 스폰마다 무작위로 하나를 고른다.")]
        [SerializeField] private GameObjectPool[] enemyPools;
        [SerializeField] private GameObjectPool xpGemPool;
```

`SpawnOne` 전체 교체:

```csharp
        private void SpawnOne()
        {
            if (enemyPools == null || enemyPools.Length == 0)
            {
                Debug.LogError($"{name}: enemyPools가 비어 있어 스폰할 수 없습니다.");
                return;
            }

            GameObjectPool pool = enemyPools[Random.Range(0, enemyPools.Length)];

            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 spawnPos = SpawnRingUtility.GetPositionOnRing(_player.position, spawnRadius, angle);
            GameObject enemyObj = pool.Get(spawnPos, Quaternion.identity);

            if (enemyObj.TryGetComponent<EnemyBase>(out var enemy))
                enemy.SetXpGemPool(xpGemPool);
        }
```

- [ ] **Step 2: 테스트 실행해서 컴파일 확인**

Expected: 총계 94개 통과, `error CS` 없음.

---

### Task 14: CLAUDE.md 몬스터 표 정정

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Step 1: 몬스터 표 수정**

`CLAUDE.md`의 `## 몬스터` 표에서 잡몹 행과 중형몹 행을 아래로 교체한다:

```markdown
| 구분 | 체력 | 접촉 데미지 | 비고 |
|---|---|---|---|
| 잡몹 A | 12 | 5 | 검은 초밥 병사, 5종 색상 베리에이션(스탯 차등 여부 TBD), 단순 추적형 |
| 잡몹 B | 20 | 5 | 캘리포니아롤, 느리고 단단한 쪽(이동속도 1.5). 잡몹 A와 섞여 지속 스폰 |
| 중형몹 | 200 | 12 | 잡몹의 2배 크기 그림, 2분 간격(2:00, 4:00) 등장 |
| 보스 | 4,000 | 15~25 (패턴별) | 마녀풍 셰프 컨셉, 최소 2~3패턴(광역/소환·돌진/페이즈전환) 권장, 패턴 상세 TBD |
```

정정 근거를 표 아래에 한 줄 남긴다:

```markdown
정정(2026-08-19): 기획서 v1.3은 중형몹을 캘리포니아롤로 기술했으나, 아트 확인 결과
둘은 별개다. 캘리포니아롤은 잡몹과 프레임 규격이 동일한 50×50이고, 중형몹은 100×100의
다른 그림이다. 따라서 잡몹은 2종이며 중형몹은 자기 아트를 가진 별도 몬스터다.
```

- [ ] **Step 2: 웨이브 타임라인 표의 표현 확인**

`## 웨이브 타임라인` 표는 중형몹을 그대로 쓰므로 수정하지 않는다. 잡몹이 2종이
되었다는 사실만 `## 몬스터` 표에 반영되면 충분하다.

---

### Task 15: Unity Editor 작업 — 체력바, 캘리포니아롤, 최종 플레이테스트

Unity를 열고 진행한다.

- [ ] **Step 1: 캘리포니아롤 스프라이트 슬라이스**

`Assets/Art/캐릭터/캐릭터/몬스터 시트/캘리포니아롤-Sheet.png` 선택 →
`Texture Type` = `Sprite (2D and UI)`, `Sprite Mode` = `Multiple` → Apply →
`Sprite Editor` → `Slice > Grid By Cell Size`, `Pixel Size` = 50 × 50 →
`Slice` → Apply. (5프레임이 나와야 한다)

- [ ] **Step 2: 캘리포니아롤 MonsterData 생성**

`Assets/_Project/Data`에서 `Create > SushiSurvival > Monster Data` →
이름 `CaliforniaRollData`
- `Monster Name` = `캘리포니아롤`
- `Max Health` = 20
- `Contact Damage` = 5
- `Move Speed` = 1.5
- `Xp Gem Drop` = Basic

- [ ] **Step 3: 캘리포니아롤 프리팹 만들기**

가장 쉬운 방법은 기존 `BasicMob` 프리팹을 복제하는 것이다.
1. Project 창에서 `BasicMob` 프리팹 선택 → `Ctrl+D`로 복제 → 이름 `CaliforniaRoll`
2. 더블클릭해서 열고:
   - `SpriteRenderer`의 `Sprite`를 슬라이스한 캘리포니아롤 프레임 중 하나로 교체
   - `EnemyBase`의 `Monster Data` ← `CaliforniaRollData`
   - `EnemyAI`의 `Monster Data` ← `CaliforniaRollData`
3. Layer가 `Enemy`인지 확인한다

- [ ] **Step 4: 캘리포니아롤 풀 추가와 스포너 배선**

1. 씬에 빈 GameObject `CaliforniaRollPool` 생성 → `Game Object Pool` 추가
   - `Prefab` ← `CaliforniaRoll` 프리팹
   - `Prewarm Count` = 50
2. 씬의 `EnemySpawner` 선택 → `Enemy Pools` 배열 크기를 2로 하고:
   - 0번 ← 기존 잡몹 풀(`EnemySpawner` 자신에 붙은 `Game Object Pool`)
   - 1번 ← `CaliforniaRollPool`

- [ ] **Step 5: 체력바 만들기 (두 캐릭터 프리팹 각각)**

`EggPlayer`, `ShrimpPlayer` 프리팹 각각에 대해:
1. 프리팹을 열고 루트 아래에 `UI > Canvas`를 자식으로 추가 → 이름 `HealthBarCanvas`
   - `Render Mode` = **`World Space`**
   - `Rect Transform`: `Width` 1, `Height` 0.15, `Scale` 전부 1
   - `Pos Y` = -0.4 (캐릭터 발밑. 스프라이트 크기에 맞춰 조정)
2. `HealthBarCanvas` 아래에 `UI > Image` 2개:
   - `Background` — 색을 어둡게(예: 검정 알파 180)
   - `Fill` — 색을 빨강/초록으로, **`Image Type` = `Filled`**,
     `Fill Method` = `Horizontal`, `Fill Origin` = `Left`, `Fill Amount` = 1
   - 둘 다 Rect Transform을 `HealthBarCanvas` 전체로 늘린다
3. `HealthBarCanvas`에 `Health Bar` 컴포넌트 추가
   - `Player Health` ← 프리팹 루트의 `Player Health`
   - `Fill Image` ← 방금 만든 `Fill`

- [ ] **Step 6: 최종 플레이테스트**

Play 모드에서 확인:

**체력바**
1. 캐릭터 발밑에 체력바가 보이고 카메라를 따라 움직인다
2. 피격당하면 즉시 줄어든다
3. 회복 증강을 고르면 서서히 다시 찬다

**증강**
4. 체력 증강을 고르면 **최대치와 현재 체력이 같이 오른다**(바가 줄어들지 않는다)
5. 자석 증강을 고르면 젬이 더 먼 거리에서 빨려온다
6. 방어력 증강을 여러 번 고르면 피해가 줄지만 **0이 되지는 않는다**
7. 부활 증강을 고른 뒤 죽으면 체력이 가득 찬 채로 부활하고, 두 번째 죽음에서는
   `GAME OVER`가 뜬다
8. 공격속도 증강을 최대까지 고르면 공격이 빨라지지만 무한 연사가 되지는 않는다

**잡몹 2종**
9. 검은 초밥 병사와 캘리포니아롤이 섞여서 스폰된다
10. 캘리포니아롤이 눈에 띄게 느리고 더 오래 버틴다

**전체**
11. 레벨업 팝업이 정상 동작하고, 게임오버 후에도 게임이 멈춘 채로 남지 않는다
12. Console에 에러가 없다
