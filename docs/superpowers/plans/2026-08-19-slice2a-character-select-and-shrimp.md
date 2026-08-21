# 슬라이스 2a: 캐릭터 선택 + 간장새우 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 캐릭터 선택 화면에서 계란/간장새우를 골라 플레이할 수 있게 하고,
간장새우의 투사체 무기(간장 소총)를 관통까지 포함해 동작시킨다.

**Architecture:** 무기 공통부를 `WeaponBase` 추상 클래스로 올리고 각 무기는
`Attack()`만 구현한다. 플레이어는 캐릭터별 프리팹을 런타임에 생성하며,
`GameManager`가 런 상태를 들고 스폰·카메라·적 스포너를 배선한다. 순수 로직(쿨타임,
관통 판정, 무기 회전각)은 EditMode 유닛 테스트로 TDD하고, MonoBehaviour 통합
동작은 Play 모드 수동 테스트로 확인한다.

**Tech Stack:** Unity 2022.3.62f3 LTS, Built-in 2D, Input System, uGUI(이 플랜에서
설치), Unity Test Framework(EditMode, NUnit)

**Spec:** [docs/superpowers/specs/2026-08-19-slice2a-character-select-and-shrimp-design.md](../specs/2026-08-19-slice2a-character-select-and-shrimp-design.md)

## Global Constraints

- Unity 버전: 2022.3.62f3 (LTS) 고정.
- 입력: 새 Input System 패키지만 사용(레거시 Input Manager 금지).
- 렌더 파이프라인: Built-in (URP 추가 금지).
- 무기/캐릭터 수치는 코드에 하드코딩하지 않고 ScriptableObject 에셋 필드에 입력한다.
- 간장 소총 수치(고정값, CLAUDE.md 기준): Lv1 데미지12/관통0/쿨타임0.8,
  Lv2 14/1/0.75, Lv3 17/1/0.7, Lv4 20/2/0.65.
- 관통 수의 의미: `관통 0 = 1체만 타격 후 소멸`. 소멸 조건은 `적중 수 > 관통 수`.
- 투사체 초기값(기획서에 없는 값, 인스펙터 노출): 속도 10, 수명 3초.
- 간장새우의 기본 이동속도·체력은 계란과 동일(3 / 100)하게 시작한다.
- 간장새우는 대기/걷기 스프라이트 시트가 없다. `CharacterAnimator`를 붙이지 않으며
  걷기 애니메이션 없이 정지 스프라이트로 구현한다.
- 스크립트는 전부 `unity/Assets/_Project/Scripts/` 하위, 어셈블리
  `SushiSurvival.Runtime`.
- **Unity Editor가 열려 있으면 배치 모드 명령(테스트·패키지 설치)이 실패한다.**
  코드 작업 Task는 Editor를 닫고, Editor 작업 Task는 열고 진행한다.
- **테스트 실행 명령에 `-quit`을 붙이지 않는다.** `-runTests`와 같이 쓰면 결과
  파일이 생성되지 않고 조용히 종료된다. 아래 표준 명령을 그대로 쓴다:

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

- 이 플랜 시작 시점의 기존 테스트는 42개다. 각 Task 후 테스트 총계가 줄지 않아야 한다.

---

## Phase 1 — WeaponBase 리팩터

기능 변화가 없는 단계다. 계란이 이전과 똑같이 동작하는지만 확인한다.

### Task 1: WeaponCooldown 순수 로직 (TDD)

**Files:**
- Create: `unity/Assets/_Project/Scripts/Weapons/WeaponCooldown.cs`
- Test: `unity/Assets/Tests/EditMode/WeaponCooldownTests.cs`

**Interfaces:**
- Consumes: 없음.
- Produces: `class WeaponCooldown` — `bool IsReady { get; }`,
  `void Tick(float deltaTime)`, `void Reset(float cooldownSeconds)`.
  Task 2(`WeaponBase`)가 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/WeaponCooldownTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.Weapons;

namespace SushiSurvival.EditModeTests
{
    public class WeaponCooldownTests
    {
        [Test]
        public void IsReady_TrueImmediately_SoFirstAttackFiresAtOnce()
        {
            var cooldown = new WeaponCooldown();

            Assert.IsTrue(cooldown.IsReady);
        }

        [Test]
        public void IsReady_FalseRightAfterReset()
        {
            var cooldown = new WeaponCooldown();
            cooldown.Reset(1.2f);

            Assert.IsFalse(cooldown.IsReady);
        }

        [Test]
        public void IsReady_TrueAfterEnoughTimePasses()
        {
            var cooldown = new WeaponCooldown();
            cooldown.Reset(1.0f);
            cooldown.Tick(0.6f);
            cooldown.Tick(0.6f);

            Assert.IsTrue(cooldown.IsReady);
        }

        [Test]
        public void IsReady_FalseWhileTimeRemains()
        {
            var cooldown = new WeaponCooldown();
            cooldown.Reset(1.0f);
            cooldown.Tick(0.6f);

            Assert.IsFalse(cooldown.IsReady);
        }

        [Test]
        public void IsReady_TrueExactlyAtZero()
        {
            var cooldown = new WeaponCooldown();
            cooldown.Reset(1.0f);
            cooldown.Tick(1.0f);

            Assert.IsTrue(cooldown.IsReady);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Global Constraints의 표준 테스트 명령 실행.
Expected: `WeaponCooldown`이 없어서 컴파일 실패.

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/Weapons/WeaponCooldown.cs`:

```csharp
namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 무기 쿨타임 타이머. 생성 직후에는 준비 상태라 첫 공격이 즉시 나간다.
    /// </summary>
    public class WeaponCooldown
    {
        private float _remaining;

        public bool IsReady => _remaining <= 0f;

        public void Tick(float deltaTime) => _remaining -= deltaTime;

        public void Reset(float cooldownSeconds) => _remaining = cooldownSeconds;
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Expected: `WeaponCooldownTests` 5개 통과, 총계 47개.

---

### Task 2: WeaponBase 추상 클래스 + EggFanWeapon 이관

**Files:**
- Create: `unity/Assets/_Project/Scripts/Weapons/WeaponBase.cs`
- Modify: `unity/Assets/_Project/Scripts/Weapons/EggFanWeapon.cs` (전면 교체)

**Interfaces:**
- Consumes: `SushiSurvival.Weapons.WeaponCooldown` (Task 1),
  `SushiSurvival.Data.WeaponData` / `WeaponLevelStats`,
  `SushiSurvival.Player.AttackAnimator`.
- Produces: `abstract class WeaponBase : MonoBehaviour` —
  `protected WeaponLevelStats CurrentStats { get; }`,
  `protected abstract void Attack()`.
  Task 13(`ShrimpRifleWeapon`)이 상속한다.

- [ ] **Step 1: WeaponBase 작성**

`unity/Assets/_Project/Scripts/Weapons/WeaponBase.cs`:

```csharp
using UnityEngine;
using SushiSurvival.Data;
using SushiSurvival.Player;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 무기 공통부 — 쿨타임 타이머, 레벨별 수치 조회, 공격 애니메이션 트리거.
    /// 각 무기는 Attack()만 구현한다.
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        [SerializeField] protected WeaponData weaponData;
        [Tooltip("계란·간장새우는 무기 오브젝트(WeaponVisual)의 것을, 이나리는 캐릭터 본체의 것을 연결한다.")]
        [SerializeField] protected AttackAnimator attackAnimator;
        [Tooltip("1-based (1~4)")]
        [SerializeField] protected int currentLevel = 1;

        private readonly WeaponCooldown _cooldown = new WeaponCooldown();

        protected WeaponLevelStats CurrentStats => weaponData.levels[currentLevel - 1];

        private void Update()
        {
            _cooldown.Tick(Time.deltaTime);
            if (!_cooldown.IsReady) return;

            attackAnimator?.TriggerAttack();
            Attack();
            _cooldown.Reset(CurrentStats.cooldown);
        }

        protected abstract void Attack();
    }
}
```

- [ ] **Step 2: EggFanWeapon을 WeaponBase 상속으로 교체**

`unity/Assets/_Project/Scripts/Weapons/EggFanWeapon.cs` 전체를 아래로 교체:

```csharp
using UnityEngine;
using SushiSurvival.Player;
using SushiSurvival.Enemies;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 계란 양산 — 시선 방향 부채꼴 범위 안의 적을 전부 타격한다(다중 히트).
    /// </summary>
    public class EggFanWeapon : WeaponBase
    {
        [SerializeField] private FacingController facing;
        [SerializeField] private LayerMask enemyLayer;

        protected override void Attack()
        {
            var stats = CurrentStats;

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

- [ ] **Step 3: 테스트 실행해서 컴파일·통과 확인**

Global Constraints의 표준 테스트 명령 실행.
Expected: 총계 47개 전부 통과, `error CS` 없음.

- [ ] **Step 4: Unity Editor에서 직접 — 인스펙터 필드 보존 확인**

Unity를 열고 `Player`의 `Egg Fan Weapon` 컴포넌트를 확인한다. `weaponData`,
`attackAnimator`, `currentLevel`이 파생 클래스에서 기반 클래스로 이동했지만
Unity는 필드 이름으로 직렬화하므로 값이 유지되어야 한다.

- `Weapon Data` = `EggWeaponData`
- `Attack Animator` = `WeaponVisual`
- `Current Level` = 1
- `Facing` = `Player`
- `Enemy Layer` = `Enemy`

비어 있는 필드가 있으면 다시 드래그해서 채운다.

- [ ] **Step 5: Unity Editor에서 직접 — 회귀 확인**

Play 모드에서 계란이 리팩터 이전과 동일하게 동작하는지 확인:
- 자동 공격이 나가고 부채꼴 범위 안의 잡몹이 죽는가
- 공격할 때 양산이 나타나 펼쳐졌다 접히는가
- 이동/방향전환/카메라 추적이 그대로인가
- Console에 에러가 없는가

---

## Phase 2 — 캐릭터 선택 + 런타임 스폰

### Task 3: uGUI 패키지 설치

**Files:**
- Modify: `unity/Packages/manifest.json` (설치 스크립트가 자동 수정)
- Create(임시): `unity/Assets/Editor/UguiInstaller.cs` — 설치 후 삭제

**Interfaces:**
- Produces: `com.unity.ugui` 패키지. Task 9의 Canvas/Button/Image 작업에 필요하다.

현재 프로젝트에는 `com.unity.ugui`가 없어 Canvas·Button·Image·EventSystem을
만들 수 없다. Unity Hub의 빈 템플릿으로 생성했기 때문이다.

- [ ] **Step 1: Unity Editor를 닫는다**

배치 모드는 Editor가 열려 있으면 실패한다.

- [ ] **Step 2: 임시 설치 스크립트 작성**

`unity/Assets/Editor/UguiInstaller.cs`:

```csharp
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

// 임시 배치모드 전용 도구 스크립트. 설치가 끝나면 삭제한다.
public static class UguiInstaller
{
    private static AddRequest _request;

    public static void Install()
    {
        Debug.Log("UguiInstaller: com.unity.ugui 설치 시작");
        _request = Client.Add("com.unity.ugui");
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (_request == null || !_request.IsCompleted) return;

        if (_request.Status == StatusCode.Success)
            Debug.Log($"UguiInstaller: 설치 성공 ({_request.Result.packageId})");
        else
            Debug.LogError($"UguiInstaller: 설치 실패 - {_request.Error.message}");

        EditorApplication.update -= Tick;
        EditorApplication.Exit(0);
    }
}
```

- [ ] **Step 3: 설치 실행**

```bash
"/c/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" \
  -batchmode -nographics \
  -projectPath "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity" \
  -executeMethod UguiInstaller.Install \
  -logFile "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity_pkg.log"
```

확인:

```bash
grep -i "UguiInstaller" "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity_pkg.log"
grep -i "ugui" "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity/Packages/manifest.json"
```

Expected: "설치 성공" 로그, manifest.json에 `com.unity.ugui` 항목 존재.

- [ ] **Step 4: 임시 스크립트 삭제**

```bash
cd "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요"
rm -f unity/Assets/Editor/UguiInstaller.cs unity/Assets/Editor/UguiInstaller.cs.meta
rmdir unity/Assets/Editor 2>/dev/null
rm -f unity_pkg.log
```

- [ ] **Step 5: 런타임 어셈블리에 uGUI 참조 추가**

`unity/Assets/_Project/Scripts/SushiSurvival.Runtime.asmdef`의 `references`에
`"UnityEngine.UI"`를 추가한다. 최종 형태:

```json
{
    "name": "SushiSurvival.Runtime",
    "rootNamespace": "",
    "references": [
        "Unity.InputSystem",
        "UnityEngine.UI"
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

- [ ] **Step 6: 테스트 실행해서 기존 테스트가 여전히 통과하는지 확인**

Expected: 총계 47개 통과.

---

### Task 4: CharacterData 확장 + PlayerController 선택적 애니메이터

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Data/CharacterData.cs`
- Modify: `unity/Assets/_Project/Scripts/Player/PlayerController.cs`

**Interfaces:**
- Produces: `CharacterData.playerPrefab` (public GameObject 필드).
  Task 5(`PlayerSpawner`)와 Task 8(`CharacterSelectButton`)이 사용한다.

- [ ] **Step 1: CharacterData에 playerPrefab 추가**

`unity/Assets/_Project/Scripts/Data/CharacterData.cs` 전체를 아래로 교체:

```csharp
using UnityEngine;

namespace SushiSurvival.Data
{
    [CreateAssetMenu(menuName = "SushiSurvival/Character Data", fileName = "NewCharacterData")]
    public class CharacterData : ScriptableObject
    {
        public string characterName;
        public Sprite portraitSprite;
        [Tooltip("이 캐릭터로 플레이할 때 생성할 프리팹. 캐릭터마다 무기·애니메이터가 다르므로 종류별로 따로 만든다.")]
        public GameObject playerPrefab;
        public float baseMoveSpeed = 3f;
        public float baseMaxHealth = 100f;
        public WeaponData weaponData;
        public RuntimeAnimatorController animatorController;
    }
}
```

- [ ] **Step 2: PlayerController에서 CharacterAnimator를 선택적으로 변경**

`unity/Assets/_Project/Scripts/Player/PlayerController.cs`에서 두 곳을 수정한다.

먼저 `[RequireComponent(typeof(CharacterAnimator))]` 줄을 삭제한다. 수정 후
클래스 선언부:

```csharp
    [RequireComponent(typeof(FacingController))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
```

다음으로 `Update()`의 애니메이터 호출을 null 허용으로 바꾼다:

```csharp
        private void Update()
        {
            _moveInput = moveAction.action.ReadValue<Vector2>();
            _facing.UpdateFacing(_moveInput);

            // 간장새우처럼 몸통 애니메이션 아트가 없는 캐릭터는 이 컴포넌트가 없다.
            if (_animator != null)
                _animator.SetMoving(FacingLogic.IsMoving(_moveInput));
        }
```

- [ ] **Step 3: 테스트 실행해서 컴파일 확인**

Expected: 총계 47개 통과, `error CS` 없음.

---

### Task 5: PlayerSpawner

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/PlayerSpawner.cs`

**Interfaces:**
- Consumes: `SushiSurvival.Data.CharacterData` (Task 4).
- Produces: `PlayerSpawner` (MonoBehaviour) —
  `GameObject Spawn(CharacterData characterData)`. 실패 시 `null`을 돌려준다.
  Task 7(`GameManager`)이 사용한다.

- [ ] **Step 1: PlayerSpawner 작성**

`unity/Assets/_Project/Scripts/Core/PlayerSpawner.cs`:

```csharp
using UnityEngine;
using SushiSurvival.Data;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 선택된 캐릭터의 프리팹을 생성한다. 캐릭터마다 무기·애니메이터 구성이
    /// 다르므로 프리팹을 통째로 바꿔 끼우는 방식을 쓴다.
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        [Tooltip("비워두면 원점(0,0)에서 생성한다.")]
        [SerializeField] private Transform spawnPoint;

        public GameObject Spawn(CharacterData characterData)
        {
            if (characterData == null)
            {
                Debug.LogError($"{name}: characterData가 null이라 플레이어를 생성할 수 없습니다.");
                return null;
            }

            if (characterData.playerPrefab == null)
            {
                Debug.LogError($"{characterData.name}: playerPrefab이 비어 있어 플레이어를 생성할 수 없습니다.");
                return null;
            }

            Vector3 position = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            return Instantiate(characterData.playerPrefab, position, Quaternion.identity);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 컴파일 확인**

Expected: 총계 47개 통과.

---

### Task 6: CameraFollow / EnemySpawner를 런타임 스폰에 대응

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Core/CameraFollow.cs`
- Modify: `unity/Assets/_Project/Scripts/Enemies/EnemySpawner.cs`

**Interfaces:**
- Produces: `CameraFollow.SetTarget(Transform)`,
  `EnemySpawner.StartSpawning(Transform player)`, `EnemySpawner.StopSpawning()`.
  Task 7(`GameManager`)이 셋 다 호출한다.

- [ ] **Step 1: CameraFollow 수정**

`unity/Assets/_Project/Scripts/Core/CameraFollow.cs`의 `Start()`를 교체하고
`SetTarget`을 추가한다. 수정 후 전체:

```csharp
using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 카메라가 플레이어를 부드럽게 따라간다. 캐릭터 종류와 무관하므로
    /// 계란·간장새우·이나리 모두 그대로 재사용한다.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Tooltip("비워두면 시작할 때 Player 태그로 찾고, 그래도 없으면 " +
                 "GameManager가 스폰 후 SetTarget으로 알려줄 때까지 기다린다.")]
        [SerializeField] private Transform target;
        [Tooltip("클수록 빠르게 따라붙는다. 0이면 따라가지 않는다.")]
        [SerializeField] private float followSpeed = 5f;

        public void SetTarget(Transform newTarget) => target = newTarget;

        private void Start()
        {
            if (target != null) return;

            // 캐릭터 선택 화면에서는 아직 플레이어가 없는 것이 정상이므로
            // 못 찾아도 에러를 내지 않는다.
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float factor = followSpeed * Time.deltaTime;
            transform.position = CameraFollowLogic.ComputeFollowPosition(
                transform.position, target.position, factor);
        }
    }
}
```

- [ ] **Step 2: EnemySpawner 수정**

`unity/Assets/_Project/Scripts/Enemies/EnemySpawner.cs` 전체를 아래로 교체:

```csharp
using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.Enemies
{
    /// <summary>
    /// 슬라이스 1 전용 단순 스폰러 — 웨이브 타임라인 없이 잡몹을 일정 주기로
    /// 플레이어 주변 링에서 반복 스폰한다. 타임라인 연동은 이후 슬라이스에서.
    /// 캐릭터 선택 화면 동안에는 스폰하지 않는다.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObjectPool enemyPool;
        [SerializeField] private GameObjectPool xpGemPool;
        [SerializeField] private float spawnRadius = 10f;
        [SerializeField] private float spawnInterval = 1.5f;

        private Transform _player;
        private bool _spawning;
        private float _timer;

        public void StartSpawning(Transform player)
        {
            _player = player;
            _timer = spawnInterval;
            _spawning = true;
        }

        public void StopSpawning() => _spawning = false;

        private void Update()
        {
            if (!_spawning || _player == null) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            SpawnOne();
            _timer = spawnInterval;
        }

        private void SpawnOne()
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 spawnPos = SpawnRingUtility.GetPositionOnRing(_player.position, spawnRadius, angle);
            GameObject enemyObj = enemyPool.Get(spawnPos, Quaternion.identity);

            if (enemyObj.TryGetComponent<EnemyBase>(out var enemy))
                enemy.SetXpGemPool(xpGemPool);
        }
    }
}
```

`player` 필드가 인스펙터에서 사라진다 — 이제 `StartSpawning`이 항상 주입하므로
값이 두 군데 존재하지 않게 하기 위함이다.

- [ ] **Step 3: 테스트 실행해서 컴파일 확인**

Expected: 총계 47개 통과.

---

### Task 7: GameManager 런 상태 + StartRun

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Core/GameManager.cs` (전면 교체)

**Interfaces:**
- Consumes: `PlayerSpawner.Spawn` (Task 5), `CameraFollow.SetTarget` /
  `EnemySpawner.StartSpawning` / `StopSpawning` (Task 6),
  `SushiSurvival.Player.PlayerHealth.OnDeath`.
- Produces: `enum RunState { CharacterSelect, Playing, GameOver }`,
  `GameManager.CurrentState`, `GameManager.StartRun(CharacterData)`,
  기존 `Instance` / `TotalExperience` / `AddExperience` / `IsGameOver` 유지.
  Task 8(`CharacterSelectButton`)이 `StartRun`을 호출한다.
  `XPGem`이 기존대로 `GameManager.Instance.AddExperience`를 호출한다.

- [ ] **Step 1: GameManager 전면 교체**

`unity/Assets/_Project/Scripts/Core/GameManager.cs` 전체를 아래로 교체:

```csharp
using UnityEngine;
using SushiSurvival.Data;
using SushiSurvival.Enemies;
using SushiSurvival.Player;

namespace SushiSurvival.Core
{
    public enum RunState
    {
        CharacterSelect,
        Playing,
        GameOver
    }

    public class GameManager : MonoBehaviour
    {
        [SerializeField] private PlayerSpawner playerSpawner;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private CameraFollow cameraFollow;
        [Tooltip("캐릭터 선택 UI 루트. 런이 시작되면 비활성화된다.")]
        [SerializeField] private GameObject characterSelectPanel;

        public static GameManager Instance { get; private set; }

        public RunState CurrentState { get; private set; } = RunState.CharacterSelect;
        public float TotalExperience { get; private set; }
        public bool IsGameOver => CurrentState == RunState.GameOver;

        private PlayerHealth _playerHealth;

        private void Awake() => Instance = this;

        private void Start()
        {
            CurrentState = RunState.CharacterSelect;

            if (characterSelectPanel != null)
                characterSelectPanel.SetActive(true);
        }

        public void StartRun(CharacterData characterData)
        {
            // 버튼 연타로 플레이어가 두 번 생성되는 것을 막는다.
            if (CurrentState != RunState.CharacterSelect) return;

            GameObject player = playerSpawner.Spawn(characterData);
            if (player == null) return;

            _playerHealth = player.GetComponent<PlayerHealth>();
            if (_playerHealth != null)
                _playerHealth.OnDeath += HandlePlayerDeath;
            else
                Debug.LogError($"{player.name}: PlayerHealth가 없어 사망 처리를 연결할 수 없습니다.");

            cameraFollow.SetTarget(player.transform);
            enemySpawner.StartSpawning(player.transform);

            if (characterSelectPanel != null)
                characterSelectPanel.SetActive(false);

            CurrentState = RunState.Playing;
            Debug.Log($"[GameManager] 런 시작: {characterData.characterName}");
        }

        public void AddExperience(float amount)
        {
            if (CurrentState != RunState.Playing) return;

            TotalExperience += amount;
            Debug.Log($"[GameManager] 누적 경험치: {TotalExperience}");
        }

        private void HandlePlayerDeath()
        {
            CurrentState = RunState.GameOver;
            enemySpawner.StopSpawning();
            Debug.Log("GAME OVER");
        }

        private void OnDisable()
        {
            if (_playerHealth != null)
                _playerHealth.OnDeath -= HandlePlayerDeath;
        }
    }
}
```

기존의 `[SerializeField] private PlayerHealth playerHealth;` 필드는 사라진다 —
플레이어가 런타임에 생성되므로 씬에서 미리 참조할 수 없다.

- [ ] **Step 2: 테스트 실행해서 컴파일 확인**

Expected: 총계 47개 통과.

---

### Task 8: CharacterSelectButton

**Files:**
- Create: `unity/Assets/_Project/Scripts/UI/CharacterSelectButton.cs`

**Interfaces:**
- Consumes: `GameManager.StartRun` (Task 7), `CharacterData` (Task 4).
- Produces: `CharacterSelectButton` (MonoBehaviour). Task 9에서 씬 버튼에 붙인다.

- [ ] **Step 1: CharacterSelectButton 작성**

`unity/Assets/_Project/Scripts/UI/CharacterSelectButton.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 캐릭터 선택 버튼 하나. 캐릭터가 3종으로 고정이라 동적 생성 대신
    /// 씬에 미리 배치하고 인스펙터에서 CharacterData를 연결한다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CharacterSelectButton : MonoBehaviour
    {
        [SerializeField] private CharacterData characterData;
        [Tooltip("캐릭터 초상화를 표시할 Image. 비워두면 표시하지 않는다.")]
        [SerializeField] private Image portraitImage;
        [Tooltip("아직 구현되지 않은 캐릭터는 체크. 회색 처리되고 선택할 수 없다.")]
        [SerializeField] private bool locked;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClicked);
        }

        private void Start()
        {
            if (portraitImage != null && characterData != null)
                portraitImage.sprite = characterData.portraitSprite;

            _button.interactable = !locked;

            if (locked && portraitImage != null)
                portraitImage.color = Color.gray;
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            if (locked) return;

            GameManager.Instance.StartRun(characterData);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 컴파일 확인**

Expected: 총계 47개 통과, `error CS` 없음. uGUI 참조가 제대로 걸렸는지 여기서
드러난다 — `UnityEngine.UI` 관련 에러가 나면 Task 3 Step 5를 다시 확인한다.

---

### Task 9: Unity Editor 작업 — 계란 프리팹화 + 선택 화면 구성

**Files:**
- Create(Editor): `unity/Assets/_Project/Prefabs/EggPlayer.prefab`
- Modify(Editor): `unity/Assets/_Project/Data/EggCharacterData.asset`
- Modify(Editor): `unity/Assets/_Project/Scenes/Slice1.unity`

이 Task는 전부 Unity Editor GUI 작업이다. Unity를 열고 진행한다.

- [ ] **Step 1: 계란 플레이어를 프리팹으로 만들기**

1. Hierarchy의 `Player`(자식 `WeaponVisual` 포함)를 `Assets/_Project/Prefabs`
   폴더로 드래그 → 이름 `EggPlayer`
2. 프리팹이 만들어진 뒤 **씬에 남아 있는 `Player` 인스턴스를 삭제**한다
   (이제 런타임에 생성된다)

- [ ] **Step 2: EggCharacterData에 프리팹 연결**

`Assets/_Project/Data/EggCharacterData` 선택 →
- `Player Prefab`에 방금 만든 `EggPlayer` 프리팹 드래그
- `Portrait Sprite`에 `Assets/Art/캐릭터/캐릭터/계란초밥 시트/new 계란.png` 드래그

- [ ] **Step 3: 캐릭터 선택 Canvas 만들기**

1. Hierarchy 빈 곳 우클릭 → `UI > Canvas`
   (`EventSystem`이 함께 자동 생성된다 — 없으면 버튼이 눌리지 않으므로 반드시 확인)

   **바로 이어서 입력 모듈을 교체한다.** uGUI가 자동 생성하는 `EventSystem`은
   레거시 `UnityEngine.Input`을 읽는 `Standalone Input Module`을 달고 나오는데,
   이 프로젝트는 Active Input Handling이 Input System 전용이라 Play 하는 순간
   `InvalidOperationException`이 뜨고 버튼이 동작하지 않는다.

   `EventSystem` 선택 → Inspector의 `Standalone Input Module`에 있는
   **`Replace with InputSystemUIInputModule`** 버튼 클릭.

   또한 UI 버튼은 `Button (Legacy)`를 쓴다. 이 프로젝트에는 TextMeshPro가
   설치돼 있지 않고, `CharacterSelectButton`이 `UnityEngine.UI.Button` /
   `UnityEngine.UI.Image`를 참조하기 때문이다.
2. `Canvas`의 `Render Mode` = `Screen Space - Overlay`
3. `Canvas` 아래에 빈 GameObject → 이름 `CharacterSelectPanel`
   - `Rect Transform`을 화면 전체로 늘린다(Anchor Presets에서 `stretch-stretch`,
     Alt 누른 채 클릭)

- [ ] **Step 4: 버튼 3개 만들기**

`CharacterSelectPanel` 아래에 `UI > Button` 3개를 만들고 각각 이름을
`Button_Egg`, `Button_Shrimp`, `Button_Inari`로 바꾼다. 가로로 나란히 배치한다.

각 버튼에 대해:
1. 버튼의 자식 `Text (Legacy)`는 삭제하거나 캐릭터 이름을 적는다
2. 버튼에 `Character Select Button` 컴포넌트 추가
3. 버튼 자신의 `Image` 컴포넌트를 `Portrait Image` 필드에 드래그
4. `Character Data` 연결:
   - `Button_Egg` → `EggCharacterData`, `Locked` 해제
   - `Button_Shrimp` → 아직 없음. Task 14에서 연결한다. 지금은 비워두고
     `Locked` **체크**
   - `Button_Inari` → 아직 없음. 비워두고 `Locked` **체크**

- [ ] **Step 5: 씬에 매니저 오브젝트 배선**

1. 빈 GameObject `PlayerSpawner` 생성 → `Player Spawner` 컴포넌트 추가
   - `Spawn Point`는 비워둔다(원점 생성)
2. `GameManager` 선택 → 필드 연결:
   - `Player Spawner` ← 방금 만든 `PlayerSpawner`
   - `Enemy Spawner` ← 씬의 `EnemySpawner`
   - `Camera Follow` ← 씬의 `Main Camera`
   - `Character Select Panel` ← `CharacterSelectPanel`
3. `Main Camera`의 `Camera Follow` → `Target`을 **비운다**(런타임에 주입됨)

- [ ] **Step 6: 플레이테스트**

Play 버튼을 누르고 확인:
1. 시작하면 캐릭터 선택 화면이 뜨고, 계란 버튼만 활성화되어 있다
2. 잡몹이 스폰되지 않는다(선택 화면 동안 정지)
3. 계란 버튼을 누르면 선택 패널이 사라지고 플레이어가 생성된다
4. 카메라가 생성된 플레이어를 따라간다
5. 잡몹이 스폰되기 시작하고 이전과 동일하게 동작한다
6. 계란 버튼을 빠르게 두 번 눌러도 플레이어가 하나만 생성된다
7. 사망하면 `GAME OVER` 로그가 뜨고 잡몹 스폰이 멈춘다
8. Console에 에러가 없다

---

## Phase 3 — 간장새우

### Task 10: ProjectileLogic 관통 판정 (TDD)

**Files:**
- Create: `unity/Assets/_Project/Scripts/Weapons/ProjectileLogic.cs`
- Test: `unity/Assets/Tests/EditMode/ProjectileLogicTests.cs`

**Interfaces:**
- Produces: `ProjectileLogic.ShouldDespawn(int enemiesHit, int pierceCount) -> bool`.
  Task 11(`Projectile`)이 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/ProjectileLogicTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.Weapons;

namespace SushiSurvival.EditModeTests
{
    public class ProjectileLogicTests
    {
        [Test]
        public void ShouldDespawn_AfterFirstHit_WhenPierceIsZero()
        {
            // 기획서 Lv1: 관통 0 = 1체만 타격 후 소멸
            Assert.IsTrue(ProjectileLogic.ShouldDespawn(enemiesHit: 1, pierceCount: 0));
        }

        [Test]
        public void ShouldDespawn_False_BeforeAnyHit()
        {
            Assert.IsFalse(ProjectileLogic.ShouldDespawn(enemiesHit: 0, pierceCount: 0));
        }

        [Test]
        public void ShouldDespawn_False_OnFirstHit_WhenPierceIsOne()
        {
            Assert.IsFalse(ProjectileLogic.ShouldDespawn(enemiesHit: 1, pierceCount: 1));
        }

        [Test]
        public void ShouldDespawn_True_OnSecondHit_WhenPierceIsOne()
        {
            Assert.IsTrue(ProjectileLogic.ShouldDespawn(enemiesHit: 2, pierceCount: 1));
        }

        [Test]
        public void ShouldDespawn_True_OnThirdHit_WhenPierceIsTwo()
        {
            // Lv4: 관통 2 = 3체까지 타격
            Assert.IsTrue(ProjectileLogic.ShouldDespawn(enemiesHit: 3, pierceCount: 2));
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Expected: `ProjectileLogic`이 없어서 컴파일 실패.

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/Weapons/ProjectileLogic.cs`:

```csharp
namespace SushiSurvival.Weapons
{
    public static class ProjectileLogic
    {
        /// <summary>
        /// 관통 수의 의미는 기획서를 따른다 — 관통 0은 "1체만 타격 후 소멸".
        /// 즉 적중 수가 관통 수를 넘어서면 사라진다.
        /// </summary>
        public static bool ShouldDespawn(int enemiesHit, int pierceCount)
            => enemiesHit > pierceCount;
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Expected: `ProjectileLogicTests` 5개 통과, 총계 52개.

---

### Task 11: Projectile 컴포넌트

**Files:**
- Create: `unity/Assets/_Project/Scripts/Weapons/Projectile.cs`

**Interfaces:**
- Consumes: `ProjectileLogic.ShouldDespawn` (Task 10),
  `SushiSurvival.Core.GameObjectPool`, `SushiSurvival.Enemies.EnemyBase`.
- Produces: `Projectile` (MonoBehaviour) —
  `void Initialize(Vector2 direction, float damage, int pierceCount, GameObjectPool pool)`.
  Task 13(`ShrimpRifleWeapon`)이 호출한다.

- [ ] **Step 1: Projectile 작성**

`unity/Assets/_Project/Scripts/Weapons/Projectile.cs`:

```csharp
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Enemies;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 직진하는 투사체. 적중 수가 관통 수를 넘거나 수명이 다하면 풀로 돌아간다.
    /// 아무것도 맞히지 못해도 수명 타이머로 반드시 회수되므로 풀이 고갈되지 않는다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [Tooltip("기획서에 없는 값 — 플레이테스트로 조정한다.")]
        [SerializeField] private float speed = 10f;
        [Tooltip("아무것도 맞히지 못했을 때 회수되기까지의 시간(초).")]
        [SerializeField] private float lifetime = 3f;

        private Vector2 _direction;
        private float _damage;
        private int _pierceCount;
        private int _enemiesHit;
        private float _lifeTimer;
        private GameObjectPool _pool;

        public void Initialize(Vector2 direction, float damage, int pierceCount, GameObjectPool pool)
        {
            _direction = direction.normalized;
            _damage = damage;
            _pierceCount = pierceCount;
            _pool = pool;

            _enemiesHit = 0;
            _lifeTimer = lifetime;
        }

        private void Update()
        {
            transform.position += (Vector3)(_direction * speed * Time.deltaTime);

            _lifeTimer -= Time.deltaTime;
            if (_lifeTimer <= 0f)
                Despawn();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent<EnemyBase>(out var enemy)) return;

            enemy.TakeDamage(_damage);
            _enemiesHit++;

            if (ProjectileLogic.ShouldDespawn(_enemiesHit, _pierceCount))
                Despawn();
        }

        private void Despawn()
        {
            if (_pool != null)
                _pool.Release(gameObject);
            else
                Destroy(gameObject);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 컴파일 확인**

Expected: 총계 52개 통과.

---

### Task 12: 무기 회전·공전 로직 (TDD) + WeaponVisual 모드

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Weapons/WeaponVisualLogic.cs`
- Modify: `unity/Assets/_Project/Scripts/Weapons/WeaponVisual.cs`
- Test: `unity/Assets/Tests/EditMode/WeaponVisualLogicTests.cs` (테스트 추가)

**Interfaces:**
- Produces: `WeaponVisualLogic.ComputeRotationDegrees(Vector2 facing) -> float`,
  `WeaponVisualLogic.ComputeOrbitOffset(Vector2 facing, float distance) -> Vector2`,
  `enum WeaponOrientMode { FlipOnly, RotateToFacing }`.
  Task 14에서 간장새우 무기에 `RotateToFacing`을 지정한다.

- [ ] **Step 1: 실패하는 테스트 추가**

`unity/Assets/Tests/EditMode/WeaponVisualLogicTests.cs`의 클래스 안에 아래
테스트들을 추가한다(기존 3개는 그대로 둔다):

```csharp
        [Test]
        public void ComputeRotationDegrees_ZeroForFacingRight()
        {
            float result = WeaponVisualLogic.ComputeRotationDegrees(Vector2.right);

            Assert.That(result, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ComputeRotationDegrees_NinetyForFacingUp()
        {
            float result = WeaponVisualLogic.ComputeRotationDegrees(Vector2.up);

            Assert.That(result, Is.EqualTo(90f).Within(0.0001f));
        }

        [Test]
        public void ComputeRotationDegrees_HandlesDiagonal()
        {
            float result = WeaponVisualLogic.ComputeRotationDegrees(new Vector2(1f, 1f));

            Assert.That(result, Is.EqualTo(45f).Within(0.0001f));
        }

        [Test]
        public void ComputeOrbitOffset_PlacesWeaponInFacingDirection()
        {
            var result = WeaponVisualLogic.ComputeOrbitOffset(Vector2.right, 0.5f);

            Assert.That(result.x, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(result.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ComputeOrbitOffset_KeepsGivenDistance()
        {
            var result = WeaponVisualLogic.ComputeOrbitOffset(new Vector2(3f, 4f), 2f);

            Assert.That(result.magnitude, Is.EqualTo(2f).Within(0.0001f));
        }
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Expected: `ComputeRotationDegrees` / `ComputeOrbitOffset` 정의가 없어서 컴파일 실패.

- [ ] **Step 3: WeaponVisualLogic 확장**

`unity/Assets/_Project/Scripts/Weapons/WeaponVisualLogic.cs` 전체를 아래로 교체:

```csharp
using UnityEngine;

namespace SushiSurvival.Weapons
{
    public enum WeaponOrientMode
    {
        /// <summary>계란(양산) — 좌우 반전만 한다.</summary>
        FlipOnly,
        /// <summary>간장새우(라이플) — 시선 방향으로 회전하며 캐릭터 주위를 공전한다.</summary>
        RotateToFacing
    }

    public static class WeaponVisualLogic
    {
        /// <summary>
        /// 캐릭터 기준 무기 위치. 왼쪽을 볼 때는 x만 뒤집는다(y는 그대로 —
        /// 뒤집으면 무기가 위아래로 튄다).
        /// </summary>
        public static Vector2 ComputeLocalOffset(Vector2 baseOffset, bool facingRight)
            => facingRight ? baseOffset : new Vector2(-baseOffset.x, baseOffset.y);

        /// <summary>시선 방향을 스프라이트 회전각(도)으로 바꾼다. 오른쪽이 0도.</summary>
        public static float ComputeRotationDegrees(Vector2 facing)
            => Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;

        /// <summary>시선 방향으로 distance만큼 떨어진 위치(캐릭터 주위 공전).</summary>
        public static Vector2 ComputeOrbitOffset(Vector2 facing, float distance)
            => facing.normalized * distance;
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Expected: `WeaponVisualLogicTests` 8개 통과, 총계 57개.

- [ ] **Step 5: WeaponVisual에 모드 추가**

`unity/Assets/_Project/Scripts/Weapons/WeaponVisual.cs` 전체를 아래로 교체:

```csharp
using UnityEngine;
using SushiSurvival.Player;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 캐릭터 옆에 붙어 다니는 무기 그림(양산, 라이플 등)의 위치와 방향을 맡는다.
    /// 캐릭터 본체 스프라이트와 분리돼 있어야 공격 중에도 캐릭터가 그대로 보인다.
    /// </summary>
    public class WeaponVisual : MonoBehaviour
    {
        [Tooltip("FlipOnly는 좌우 반전만(계란 양산), RotateToFacing은 시선 방향 회전(간장새우 라이플).")]
        [SerializeField] private WeaponOrientMode orientMode = WeaponOrientMode.FlipOnly;
        [Tooltip("FlipOnly에서 쓰는 캐릭터 기준 무기 위치. 오른쪽을 볼 때 기준.")]
        [SerializeField] private Vector2 baseOffset = new Vector2(0.5f, 0f);
        [Tooltip("RotateToFacing에서 쓰는 캐릭터로부터의 거리.")]
        [SerializeField] private float orbitDistance = 0.5f;
        [SerializeField] private FacingController facing;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private void LateUpdate()
        {
            if (facing == null) return;

            Vector2 currentFacing = facing.CurrentFacing;

            if (orientMode == WeaponOrientMode.RotateToFacing)
            {
                transform.localPosition = WeaponVisualLogic.ComputeOrbitOffset(currentFacing, orbitDistance);
                transform.localRotation = Quaternion.Euler(0f, 0f, WeaponVisualLogic.ComputeRotationDegrees(currentFacing));

                // 왼쪽을 볼 때 스프라이트가 뒤집히지 않도록 y를 반전한다.
                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = false;
                    spriteRenderer.flipY = !FacingLogic.IsFacingRight(currentFacing);
                }
                return;
            }

            bool facingRight = FacingLogic.IsFacingRight(currentFacing);
            transform.localPosition = WeaponVisualLogic.ComputeLocalOffset(baseOffset, facingRight);
            transform.localRotation = Quaternion.identity;

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !facingRight;
                spriteRenderer.flipY = false;
            }
        }
    }
}
```

- [ ] **Step 6: 테스트 실행해서 컴파일 확인**

Expected: 총계 57개 통과.

---

### Task 13: ShrimpRifleWeapon + 투사체 풀 주입

**Files:**
- Create: `unity/Assets/_Project/Scripts/Weapons/ShrimpRifleWeapon.cs`
- Modify: `unity/Assets/_Project/Scripts/Core/PlayerSpawner.cs`

**Interfaces:**
- Consumes: `WeaponBase` (Task 2), `Projectile.Initialize` (Task 11),
  `SushiSurvival.Core.GameObjectPool`, `SushiSurvival.Player.FacingController`.
- Produces: `ShrimpRifleWeapon : WeaponBase` —
  `void SetProjectilePool(GameObjectPool pool)`.
  `PlayerSpawner`가 스폰 직후 호출한다.

`ShrimpPlayer`는 프리팹이고 `ProjectilePool`은 씬 오브젝트라 인스펙터로 직접
연결할 수 없다. 슬라이스 1의 `EnemyBase.xpGemPool`과 같은 상황이므로 같은 해법을
쓴다 — 스포너가 생성 직후 주입한다.

- [ ] **Step 1: ShrimpRifleWeapon 작성**

`unity/Assets/_Project/Scripts/Weapons/ShrimpRifleWeapon.cs`:

```csharp
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Player;

namespace SushiSurvival.Weapons
{
    /// <summary>
    /// 간장 소총 — 시선 방향으로 투사체를 발사한다. 자동 조준은 없다(기획서).
    /// </summary>
    public class ShrimpRifleWeapon : WeaponBase
    {
        [SerializeField] private FacingController facing;
        [Tooltip("투사체가 나가는 위치. 비워두면 이 오브젝트 위치에서 발사한다.")]
        [SerializeField] private Transform muzzle;

        private GameObjectPool _projectilePool;

        /// <summary>
        /// PlayerSpawner가 스폰 직후 주입한다. 프리팹 에셋은 씬에만 존재하는
        /// 풀을 Inspector로 직접 참조할 수 없기 때문.
        /// </summary>
        public void SetProjectilePool(GameObjectPool pool) => _projectilePool = pool;

        protected override void Attack()
        {
            if (_projectilePool == null)
            {
                Debug.LogError($"{name}: projectilePool이 주입되지 않아 발사할 수 없습니다.");
                return;
            }

            var stats = CurrentStats;

            Vector2 direction = facing.CurrentFacing;
            Vector3 spawnPos = muzzle != null ? muzzle.position : transform.position;
            float rotation = WeaponVisualLogic.ComputeRotationDegrees(direction);

            GameObject projectileObj = _projectilePool.Get(spawnPos, Quaternion.Euler(0f, 0f, rotation));

            if (projectileObj.TryGetComponent<Projectile>(out var projectile))
                projectile.Initialize(direction, stats.damage, stats.pierceCount, _projectilePool);
            else
                Debug.LogError($"{projectileObj.name}: Projectile 컴포넌트가 없어 발사할 수 없습니다.");
        }
    }
}
```

- [ ] **Step 2: PlayerSpawner가 풀을 주입하도록 수정**

`unity/Assets/_Project/Scripts/Core/PlayerSpawner.cs` 전체를 아래로 교체:

```csharp
using UnityEngine;
using SushiSurvival.Data;
using SushiSurvival.Weapons;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 선택된 캐릭터의 프리팹을 생성한다. 캐릭터마다 무기·애니메이터 구성이
    /// 다르므로 프리팹을 통째로 바꿔 끼우는 방식을 쓴다.
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        [Tooltip("비워두면 원점(0,0)에서 생성한다.")]
        [SerializeField] private Transform spawnPoint;
        [Tooltip("투사체 무기를 쓰는 캐릭터에게 스폰 직후 주입한다. 프리팹은 씬 오브젝트를 직접 참조할 수 없기 때문.")]
        [SerializeField] private GameObjectPool projectilePool;

        public GameObject Spawn(CharacterData characterData)
        {
            if (characterData == null)
            {
                Debug.LogError($"{name}: characterData가 null이라 플레이어를 생성할 수 없습니다.");
                return null;
            }

            if (characterData.playerPrefab == null)
            {
                Debug.LogError($"{characterData.name}: playerPrefab이 비어 있어 플레이어를 생성할 수 없습니다.");
                return null;
            }

            Vector3 position = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            GameObject player = Instantiate(characterData.playerPrefab, position, Quaternion.identity);

            if (player.TryGetComponent<ShrimpRifleWeapon>(out var rifle))
                rifle.SetProjectilePool(projectilePool);

            return player;
        }
    }
}
```

- [ ] **Step 3: 테스트 실행해서 컴파일 확인**

Expected: 총계 57개 통과, `error CS` 없음.

---

### Task 14: Unity Editor 작업 — 간장새우 구성 및 플레이테스트

**Files:**
- Create(Editor): `unity/Assets/_Project/Data/ShrimpWeaponData.asset`
- Create(Editor): `unity/Assets/_Project/Data/ShrimpCharacterData.asset`
- Create(Editor): `unity/Assets/_Project/Prefabs/SoyProjectile.prefab`
- Create(Editor): `unity/Assets/_Project/Prefabs/ShrimpPlayer.prefab`
- Modify(Editor): `unity/Assets/_Project/Scenes/Slice1.unity`

전부 Unity Editor GUI 작업이다.

- [ ] **Step 1: 스프라이트 임포트 설정**

`Assets/Art/캐릭터/캐릭터/간장새우 시트`의 세 파일(`new 간장새우.png`,
`라이플.png`, `간장 총알.png`)을 선택하고 Inspector에서:
- `Texture Type` = `Sprite (2D and UI)`
- `Sprite Mode` = `Single` (셋 다 단일 이미지다)
- Apply

- [ ] **Step 2: 간장 소총 WeaponData 생성**

`Assets/_Project/Data`에서 우클릭 → `Create > SushiSurvival > Weapon Data` →
이름 `ShrimpWeaponData`

- `Weapon Name` = `간장 소총`
- `Is Melee` = **해제**
- `Levels` 배열 크기 4, 아래 표대로 입력(기획서 수치):

  | 인덱스(Lv) | damage | cooldown | range | angleDegrees | pierceCount |
  |---|---|---|---|---|---|
  | 0 (Lv1) | 12 | 0.8 | 0 | 0 | 0 |
  | 1 (Lv2) | 14 | 0.75 | 0 | 0 | 1 |
  | 2 (Lv3) | 17 | 0.7 | 0 | 0 | 1 |
  | 3 (Lv4) | 20 | 0.65 | 0 | 0 | 2 |

  `range`와 `angleDegrees`는 근접 무기 전용이므로 0으로 둔다.

- [ ] **Step 3: 투사체 프리팹 만들기**

1. 빈 GameObject 생성 → 이름 `SoyProjectile`
2. 컴포넌트 추가:
   - `SpriteRenderer` — `Sprite`에 `간장 총알.png` 지정,
     `Order in Layer`를 캐릭터보다 크게(예: 2)
   - `CircleCollider2D` — **`Is Trigger` 체크** (`OnTriggerEnter2D`를 쓰므로 필수),
     `Radius`는 총알 크기에 맞게(예: 0.1)
   - `Rigidbody2D` — `Body Type` = `Kinematic`, `Gravity Scale` = 0,
     **`Use Full Kinematic Contacts` 체크**
     (Kinematic끼리는 이걸 켜지 않으면 충돌 콜백이 발생하지 않는다)
   - `Projectile` — `Speed` = 10, `Lifetime` = 3
3. `Assets/_Project/Prefabs`로 드래그해 프리팹화 → 씬 인스턴스 삭제

- [ ] **Step 4: 투사체 풀 만들기**

씬에 빈 GameObject `ProjectilePool` 생성 → `Game Object Pool` 컴포넌트 추가
- `Prefab` ← `SoyProjectile` 프리팹
- `Prewarm Count` = 50

- [ ] **Step 5: 간장새우 플레이어 프리팹 만들기**

1. 빈 GameObject 생성 → 이름 `ShrimpPlayer`, **Tag = `Player`**
2. 컴포넌트 추가:
   - `SpriteRenderer` — `Sprite`에 `new 간장새우.png`
   - `Rigidbody2D` — `Kinematic`, `Gravity Scale` 0,
     **`Use Full Kinematic Contacts` 체크**
   - `CircleCollider2D` — `Is Trigger` **해제**
   - `FacingController` — `Sprite Renderer`에 자기 자신의 SpriteRenderer 드래그
   - `PlayerController` — `Character Data`는 Step 6에서 만들 `ShrimpCharacterData`,
     `Move Action`은 `PlayerInputActions`의 `Player/Move`
   - `PlayerHealth` — `Character Data`도 동일
   - **`CharacterAnimator`는 붙이지 않는다** (걷기 스프라이트 시트가 없다)
3. 자식으로 빈 GameObject `WeaponVisual` 생성:
   - `SpriteRenderer` — `Sprite`에 `라이플.png`, `Order in Layer`를 캐릭터보다 크게(예: 1)
   - `Weapon Visual` — `Orient Mode` = **`RotateToFacing`**,
     `Orbit Distance` = 0.5, `Facing` ← 부모 `ShrimpPlayer`,
     `Sprite Renderer` ← 자기 자신
   - `Animator`와 `AttackAnimator`는 **붙이지 않는다**
     (라이플은 발사 애니메이션 아트가 없다. `WeaponBase`의 `attackAnimator`가
     비어 있어도 `?.`로 안전하게 건너뛴다)
4. `ShrimpPlayer` 본체에 `Shrimp Rifle Weapon` 컴포넌트 추가:
   - `Weapon Data` ← `ShrimpWeaponData`
   - `Attack Animator` ← 비워둠
   - `Current Level` = 1
   - `Facing` ← 자기 자신
   - `Muzzle` ← 자식 `WeaponVisual`
   - (투사체 풀은 인스펙터 필드가 아니다 — `PlayerSpawner`가 런타임에 주입한다)
5. `Assets/_Project/Prefabs`로 드래그해 프리팹화 → 씬 인스턴스 삭제

- [ ] **Step 6: 간장새우 CharacterData 생성**

`Assets/_Project/Data`에서 우클릭 → `Create > SushiSurvival > Character Data` →
이름 `ShrimpCharacterData`

- `Character Name` = `간장새우`
- `Portrait Sprite` ← `new 간장새우.png`
- `Player Prefab` ← `ShrimpPlayer` 프리팹
- `Base Move Speed` = 3, `Base Max Health` = 100 (계란과 동일하게 시작)
- `Weapon Data` ← `ShrimpWeaponData`
- `Animator Controller` ← 비워둠

만든 뒤 `ShrimpPlayer` 프리팹을 다시 열어 `PlayerController`와 `PlayerHealth`의
`Character Data`에 이 에셋을 연결한다(Step 5에서 아직 없었다).

- [ ] **Step 7: 선택 버튼에 간장새우 연결**

씬의 `Button_Shrimp` 선택 → `Character Select Button` 컴포넌트:
- `Character Data` ← `ShrimpCharacterData`
- `Locked` **해제**

- [ ] **Step 8: PlayerSpawner에 투사체 풀 연결**

씬의 `PlayerSpawner` 선택 → `Projectile Pool` ← 씬의 `ProjectilePool` 드래그.

(코드 쪽 주입 로직은 Task 13에서 이미 작성했다. 여기서는 인스펙터 배선만 한다.)

- [ ] **Step 9: 플레이테스트**

Play 버튼을 누르고 확인:
1. 선택 화면에 계란·간장새우 두 칸이 활성화, 이나리는 회색
2. 간장새우를 선택하면 간장새우가 생성되고 라이플이 옆에 보인다
3. 이동하면 라이플이 진행 방향을 향해 회전하며 캐릭터 주위를 돈다
4. 자동으로 간장 총알이 시선 방향으로 발사된다
5. 총알이 잡몹에 맞으면 데미지가 들어가고, Lv1에서는 **1체를 맞히면 사라진다**
6. 아무것도 못 맞힌 총알은 3초 뒤 사라진다(Hierarchy의 `ProjectilePool` 자식이
   무한정 늘어나지 않는지 확인)
7. 계란으로도 선택해보고 이전과 동일하게 동작하는지 확인
8. Console에 에러가 없다

- [ ] **Step 10: 관통 확인 (선택)**

`ShrimpWeaponData`의 `Current Level`을 임시로 4로 올리거나(프리팹의
`Shrimp Rifle Weapon` → `Current Level` = 4), Lv4 수치를 확인하려면 잡몹이
일직선으로 겹칠 때 총알이 3체까지 관통하는지 본다. 확인 후 다시 1로 되돌린다.
