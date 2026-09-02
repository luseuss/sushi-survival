# 왕의 와사비 하사 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 레벨업 3택 화면에 4번째 선택지("와사비를 하사받으러 간다")를 추가한다.
10% 확률로 주요 스탯 4종을 크게 강화하고, 실패하면 그 레벨업을 그냥 잃는다.

**Architecture:** `RoyalWasabiController`가 확률 판정과 버프 적용을 담당하는
진입점이고, `RoyalWasabiPanel`이 왕궁 연출과 결과 표시를 맡는다. 버프 계산은
새로 만들지 않고 호감도 대화 #1의 `AffinityBuffLogic`/`AffinityBuffApplier`를
비율만 바꿔 재사용한다. `LevelUpPanel`에 4번째 버튼을 추가하고, `LevelSystem`이
기존 카드 선택 콜백과 같은 자리에 이 새 경로를 끼워 넣는다.

**Tech Stack:** Unity 2022.3.62f3 LTS, Built-in 2D, uGUI(Legacy), Unity Test
Framework(NUnit, EditMode)

**Spec:** `docs/superpowers/specs/2026-09-03-royal-wasabi-design.md`

## Global Constraints

- git을 쓴다. `feature/royal-wasabi` 브랜치에서 진행, 태스크마다 로컬 커밋,
  푸시는 전부 끝나고 사용자에게 물어본 뒤에 한다. 로컬 동기화는 **merge**로
  한다(`git merge origin/main`) — rebase 아님, `docs/COLLABORATION.md` 참고.
- Unity 프로젝트 루트는 `unity/` 서브폴더. 스크립트는
  `unity/Assets/_Project/Scripts/`, 테스트는 `unity/Assets/Tests/EditMode/`.
- **EditMode 테스트를 배치로 돌릴 때는 `-logFile -`(하이픈, stdout)을 쓴다.**
  ```
  & "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -projectPath "C:\Users\wnsdn\Desktop\와사비를 먹으면 강해지는 군요\unity" -runTests -testPlatform EditMode -testResults "$env:TEMP\wasabi-tests.xml" -logFile -
  ```
- 시작 시점 테스트 수는 **234개**다. 이 슬라이스는 새 순수 로직이 없다(기존
  `AffinityBuffLogic`을 재사용) — 234개가 그대로 유지되면 정상이다.
- 네임스페이스: `SushiSurvival.Core` / `.UI`.
- 주석은 한국어로, "무엇"이 아니라 "왜"를 적는다.
- 수치는 하드코딩하지 않고 `[SerializeField]`로 노출한다.
- `Text`는 반드시 Legacy를 쓴다(TextMeshPro 미설치).
- **Unity Editor GUI 작업은 사용자가 직접 한다.** 에이전트는 코드와 배치
  테스트까지만 하고, 프리팹/씬 조립·인스펙터 연결은 Task 5로 넘긴다.
- **씬 파일(`GameScene.unity`) 충돌은 텍스트로 직접 풀지 않는다.** 상대(main)
  버전을 채택하고 사람이 에디터에서 재적용한다 — `docs/COLLABORATION.md` 0장.

---

## File Structure

**신규 (`unity/Assets/_Project/Scripts/`)**

| 파일 | 책임 |
|---|---|
| `Core/RoyalWasabiController.cs` | 확률 판정 + 스탯 4종 버프 적용, 진입점 |
| `UI/RoyalWasabiPanel.cs` | 왕궁 배경·대사·결과 표시 |

**수정**

| 파일 | 변경 |
|---|---|
| `UI/LevelUpPanel.cs` | 4번째 버튼 필드 추가, `Show()`에 `onRoyalWasabi` 콜백 매개변수 추가 |
| `Core/LevelSystem.cs` | `royalWasabiController` 필드 + `HandleRoyalWasabiRequested()` 추가 |

**신규 로직 없음** — `Core/AffinityBuffLogic.cs`, `Core/AffinityBuffApplier.cs`(둘 다
기존 파일, 무수정)를 그대로 재사용한다. 새 EditMode 테스트가 없다.

---

## Task 0: 작업 브랜치 준비

- [ ] **Step 1: 최신 main 확인 후 브랜치 생성**

```bash
git checkout main
git pull --ff-only
git checkout -b feature/royal-wasabi
```

Expected: `Switched to a new branch 'feature/royal-wasabi'`

---

## Task 1: 확률 판정과 버프 적용 — `RoyalWasabiController`

순수 로직이 없는 MonoBehaviour라 TDD 대상이 아니다(확률 계산은 기존
`AffinityBuffLogic.GetBuffAmount`를 그대로 호출한다). 컴파일 확인으로
검증한다.

**Files:**
- Create: `unity/Assets/_Project/Scripts/Core/RoyalWasabiController.cs`

**Interfaces:**
- Consumes:
  - `SushiSurvival.Core.AffinityBuffLogic.GetBuffAmount(float maxCap, float ratio) → float` (기존)
  - `SushiSurvival.Core.AffinityBuffApplier.Apply(AugmentData augment, float amount, PlayerStats stats, PlayerHealth health)` (기존)
  - `SushiSurvival.UI.RoyalWasabiPanel.Show(bool success, Action onConfirm)` (Task 2)
- Produces: `SushiSurvival.Core.RoyalWasabiController.Show(PlayerStats stats, PlayerHealth health, Action onComplete)`

- [ ] **Step 1: 구현한다**

`unity/Assets/_Project/Scripts/Core/RoyalWasabiController.cs`:

```csharp
using System;
using UnityEngine;
using SushiSurvival.Data;
using SushiSurvival.Player;
using SushiSurvival.UI;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 레벨업 카드 대신 고르는 도박. 10% 확률로 스탯 4종을 크게 강화하고,
    /// 실패하면 그 레벨업에서 아무것도 못 얻는다 — 보너스가 아니라 대체라서
    /// 위로 보상이 없다.
    ///
    /// 버프 계산은 새로 만들지 않는다. 호감도 대화 #1의 AffinityBuffLogic/
    /// AffinityBuffApplier를 비율만 다르게(대화 12.5% → 이건 50%) 재사용한다.
    /// </summary>
    public class RoyalWasabiController : MonoBehaviour
    {
        [SerializeField] private RoyalWasabiPanel panel;
        [Range(0f, 1f)]
        [SerializeField] private float successChance = 0.1f;
        [Range(0f, 1f)]
        [Tooltip("성공 시 각 증강 maxCap의 이 비율만큼 강화한다.")]
        [SerializeField] private float buffRatio = 0.5f;
        [SerializeField] private AugmentData attackDamageAugment;
        [SerializeField] private AugmentData attackSpeedAugment;
        [SerializeField] private AugmentData moveSpeedAugment;
        [SerializeField] private AugmentData maxHealthAugment;

        private readonly System.Random _random = new System.Random();

        public void Show(PlayerStats stats, PlayerHealth health, Action onComplete)
        {
            if (panel == null)
            {
                Debug.LogError($"{name}: panel이 비어 있어 왕궁 연출을 표시할 수 없습니다.");
                onComplete?.Invoke();
                return;
            }

            bool success = _random.NextDouble() < successChance;

            if (success)
            {
                Apply(attackDamageAugment, stats, health);
                Apply(attackSpeedAugment, stats, health);
                Apply(moveSpeedAugment, stats, health);
                Apply(maxHealthAugment, stats, health);
            }

            panel.Show(success, () =>
            {
                panel.Hide();
                onComplete?.Invoke();
            });
        }

        private void Apply(AugmentData augment, PlayerStats stats, PlayerHealth health)
        {
            if (augment == null)
            {
                Debug.LogError($"{name}: 증강 필드 하나가 비어 있어 그 스탯은 강화되지 않습니다.");
                return;
            }

            float amount = AffinityBuffLogic.GetBuffAmount(augment.maxCap, buffRatio);
            AffinityBuffApplier.Apply(augment, amount, stats, health);
        }
    }
}
```

- [ ] **Step 2: 컴파일 상태를 확인한다**

`RoyalWasabiPanel`(Task 2)이 아직 없어 이 시점엔 컴파일 에러가 나는 게 정상이다.
다음 태스크에서 해소된다.

---

## Task 2: 왕궁 연출 — `RoyalWasabiPanel`

**Files:**
- Create: `unity/Assets/_Project/Scripts/UI/RoyalWasabiPanel.cs`

**Interfaces:**
- Consumes: 없음
- Produces: `SushiSurvival.UI.RoyalWasabiPanel.Show(bool success, Action onConfirm)`, `.Hide()`

`LevelUpPanel`/`AffinityDialoguePanel`과 같은 뼈대 — `Root` 프로퍼티,
`Awake`에서 `Hide()`.

- [ ] **Step 1: 구현한다**

`unity/Assets/_Project/Scripts/UI/RoyalWasabiPanel.cs`:

```csharp
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 왕궁 배경 위에 대사를 잠깐 보여준 뒤 성공/실패 결과를 표시한다.
    /// </summary>
    public class RoyalWasabiPanel : MonoBehaviour
    {
        [Tooltip("패널 루트. 비워두면 이 오브젝트 자신을 켜고 끈다.")]
        [SerializeField] private GameObject root;
        [SerializeField] private Text flavorText;
        [SerializeField] private Text resultText;
        [Tooltip("결과 확인 버튼. 처음엔 숨겨져 있다가 결과와 함께 나타난다.")]
        [SerializeField] private GameObject confirmButtonRoot;
        [SerializeField] private Button confirmButton;

        [Tooltip("대사만 보여주는 실시간 대기(초). Show() 시점에 이미 timeScale이 " +
                 "0이라 반드시 실시간으로 진행한다.")]
        [SerializeField] private float flavorDuration = 1.2f;
        [SerializeField] private string flavorMessage = "와사비를 하사받으러 왕을 알현합니다...";
        [SerializeField] private string successMessage = "빛나는 와사비를 하사받았다!";
        [SerializeField] private string failureMessage = "오늘은 빈손으로 돌아왔다...";

        private GameObject Root => root != null ? root : gameObject;
        private Action _onConfirm;
        private Coroutine _routine;

        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(HandleConfirmClicked);

            Hide();
        }

        private void OnDestroy()
        {
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        }

        public void Show(bool success, Action onConfirm)
        {
            _onConfirm = onConfirm;

            Root.SetActive(true);

            if (flavorText != null)
                flavorText.text = flavorMessage;

            if (resultText != null)
                resultText.text = string.Empty;

            if (confirmButtonRoot != null)
                confirmButtonRoot.SetActive(false);

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(RevealResult(success));
        }

        public void Hide() => Root.SetActive(false);

        private IEnumerator RevealResult(bool success)
        {
            yield return new WaitForSecondsRealtime(flavorDuration);

            if (resultText != null)
                resultText.text = success ? successMessage : failureMessage;

            if (confirmButtonRoot != null)
                confirmButtonRoot.SetActive(true);

            _routine = null;
        }

        private void HandleConfirmClicked() => _onConfirm?.Invoke();
    }
}
```

- [ ] **Step 2: 컴파일이 통과하는지 확인한다**

Global Constraints의 배치 명령으로 실행. 기대: 234개 통과, 컴파일 에러 없음
(Task 1의 `RoyalWasabiController`도 이제 함께 컴파일된다).

- [ ] **Step 3: 커밋**

```bash
git add unity/Assets/_Project/Scripts/Core/RoyalWasabiController.cs unity/Assets/_Project/Scripts/Core/RoyalWasabiController.cs.meta unity/Assets/_Project/Scripts/UI/RoyalWasabiPanel.cs unity/Assets/_Project/Scripts/UI/RoyalWasabiPanel.cs.meta
git commit -m "feat: RoyalWasabiController·RoyalWasabiPanel로 왕의 와사비 하사 구현"
```

---

## Task 3: `LevelUpPanel`에 4번째 버튼 추가

**Files:**
- Modify: `unity/Assets/_Project/Scripts/UI/LevelUpPanel.cs`

**Interfaces:**
- Produces: `SushiSurvival.UI.LevelUpPanel.Show(IReadOnlyList<IUpgradeOption> options, Action<IUpgradeOption> onChosen, Action onRoyalWasabi)` — 기존 `Show(options, onChosen)`를 대체(매개변수 추가)

- [ ] **Step 1: 필드를 추가한다**

기존:
```csharp
        [Tooltip("선택지 버튼 3개.")]
        [SerializeField] private LevelUpOptionButton[] optionButtons;
        [Tooltip("스케일인에 걸리는 실시간(초). Show() 직후 timeScale이 0이 되므로 " +
                 "반드시 실시간으로 진행한다.")]
        [SerializeField] private float showDuration = 0.15f;

        private GameObject Root => root != null ? root : gameObject;
        private Coroutine _showRoutine;
```

교체 후:
```csharp
        [Tooltip("선택지 버튼 3개.")]
        [SerializeField] private LevelUpOptionButton[] optionButtons;
        [Tooltip("카드 3장과 무관하게 항상 켜져 있는 4번째 선택지. 증강 풀이 " +
                 "고갈돼도 도박은 언제나 가능하다.")]
        [SerializeField] private UnityEngine.UI.Button royalWasabiButton;
        [Tooltip("스케일인에 걸리는 실시간(초). Show() 직후 timeScale이 0이 되므로 " +
                 "반드시 실시간으로 진행한다.")]
        [SerializeField] private float showDuration = 0.15f;

        private GameObject Root => root != null ? root : gameObject;
        private Coroutine _showRoutine;
        private Action _onRoyalWasabi;
```

- [ ] **Step 2: `Show()`에 콜백 매개변수를 추가한다**

기존:
```csharp
        public void Show(IReadOnlyList<IUpgradeOption> options, Action<IUpgradeOption> onChosen)
        {
            Root.SetActive(true);
            Root.transform.localScale = Vector3.zero;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < options.Count)
                    optionButtons[i].Bind(options[i], onChosen);
                else
                    optionButtons[i].Clear();
            }

            if (_showRoutine != null) StopCoroutine(_showRoutine);
            _showRoutine = StartCoroutine(ScaleIn());
        }
```

교체 후:
```csharp
        public void Show(IReadOnlyList<IUpgradeOption> options, Action<IUpgradeOption> onChosen,
                         Action onRoyalWasabi)
        {
            Root.SetActive(true);
            Root.transform.localScale = Vector3.zero;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < options.Count)
                    optionButtons[i].Bind(options[i], onChosen);
                else
                    optionButtons[i].Clear();
            }

            _onRoyalWasabi = onRoyalWasabi;
            if (royalWasabiButton != null)
            {
                royalWasabiButton.onClick.RemoveAllListeners();
                royalWasabiButton.onClick.AddListener(HandleRoyalWasabiClicked);
            }

            if (_showRoutine != null) StopCoroutine(_showRoutine);
            _showRoutine = StartCoroutine(ScaleIn());
        }

        private void HandleRoyalWasabiClicked() => _onRoyalWasabi?.Invoke();
```

- [ ] **Step 3: 컴파일 상태를 확인한다**

`LevelSystem`(Task 4)이 아직 옛 시그니처 `panel.Show(options, OnOptionChosen)`로
부르고 있어 이 시점엔 컴파일 에러가 나는 게 정상이다(인자 개수 불일치).

---

## Task 4: `LevelSystem` — 도박 경로 연결

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Core/LevelSystem.cs`

**Interfaces:**
- Consumes:
  - `SushiSurvival.UI.LevelUpPanel.Show(IReadOnlyList<IUpgradeOption>, Action<IUpgradeOption>, Action)` (Task 3)
  - `SushiSurvival.Core.RoyalWasabiController.Show(PlayerStats, PlayerHealth, Action)` (Task 1)

- [ ] **Step 1: 필드를 추가한다**

기존:
```csharp
        [SerializeField] private LevelUpPanel panel;
        [SerializeField] private AugmentData[] augments;
```

교체 후:
```csharp
        [SerializeField] private LevelUpPanel panel;
        [SerializeField] private RoyalWasabiController royalWasabiController;
        [SerializeField] private AugmentData[] augments;
```

- [ ] **Step 2: `panel.Show` 호출을 새 시그니처로 바꾸고 핸들러를 추가한다**

기존:
```csharp
                _panelOpen = true;
                Time.timeScale = 0f;
                panel.Show(options, OnOptionChosen);
                return;
            }

            CloseAndResume();
        }

        private void OnOptionChosen(IUpgradeOption option)
```

교체 후:
```csharp
                _panelOpen = true;
                Time.timeScale = 0f;
                panel.Show(options, OnOptionChosen, HandleRoyalWasabiRequested);
                return;
            }

            CloseAndResume();
        }

        /// <summary>
        /// "와사비를 하사받으러 간다"를 눌렀을 때. _panelOpen은 여기서 건드리지
        /// 않는다 — 왕궁 연출이 끝나기 전까지 게임이 재개되면 안 되기 때문이다.
        /// CloseAndResume()에서만 false로 돌아간다.
        /// </summary>
        private void HandleRoyalWasabiRequested()
        {
            panel.Hide();

            if (royalWasabiController == null)
            {
                Debug.LogError($"{name}: royalWasabiController가 비어 있어 도박을 진행할 수 없습니다.");
                ShowNext();
                return;
            }

            royalWasabiController.Show(_playerStats, _playerHealth, ShowNext);
        }

        private void OnOptionChosen(IUpgradeOption option)
```

- [ ] **Step 3: 컴파일이 통과하는지 확인한다**

기대: 234개 통과, 컴파일 에러 없음. 시그니처가 바뀐 `panel.Show`의 유일한
호출부가 `ShowNext()` 안이라 다른 곳은 안 바뀐다.

- [ ] **Step 4: 커밋**

```bash
git add unity/Assets/_Project/Scripts/UI/LevelUpPanel.cs unity/Assets/_Project/Scripts/Core/LevelSystem.cs
git commit -m "feat: LevelUpPanel·LevelSystem에 왕의 와사비 도박 경로 연결"
```

---

## Task 5: Unity Editor 작업 (사용자가 직접)

에이전트는 여기까지 코드를 완성했다. 아래는 GUI 전용 작업이라 사용자가 직접 한다.

### 5-1. 왕궁 배경 이미지 임포트

공유해주신 궁전 복도 이미지를 `Assets/Art/환경/`에 넣고 Texture Type을
**Sprite (2D and UI)**로 설정한다.

### 5-2. `RoyalWasabiPanel` UI 조립

`GameScene.unity`의 `Canvas` 아래, `AffinityDialoguePanel`과 나란히 만든다.

```
RoyalWasabiPanel             ← RoyalWasabiPanel 스크립트
├─ Background                ← Image (왕궁 배경 스프라이트)
├─ FlavorText                ← Text (Legacy)
├─ ResultText                ← Text (Legacy)
└─ ConfirmButtonRoot
   └─ ConfirmButton          ← Button (Legacy) + 자식 Text "확인"
```

필드 연결:

| 필드 | 연결 대상 |
|---|---|
| Flavor Text | `FlavorText` |
| Result Text | `ResultText` |
| Confirm Button Root | `ConfirmButtonRoot` |
| Confirm Button | `ConfirmButton`의 Button |
| Flavor Duration / 나머지 문구 필드 | 기본값 그대로 두거나 원하는 대사로 수정 |

### 5-3. `RoyalWasabiController` 오브젝트 생성

Hierarchy 빈 공간에 `RoyalWasabiController` 생성 →
**Add Component → Royal Wasabi Controller**.

| 필드 | 값 |
|---|---|
| Panel | 5-2의 `RoyalWasabiPanel` |
| Success Chance | 0.1 |
| Buff Ratio | 0.5 |
| Attack Damage Augment | `Aug_AttackDamage` |
| Attack Speed Augment | `Aug_AttackSpeed` |
| Move Speed Augment | `Aug_MoveSpeed` |
| Max Health Augment | `Aug_MaxHealth` |

### 5-4. `LevelUpPanel`에 4번째 버튼 추가

기존 3장 카드 UI를 열어서, 그 아래 새 버튼을 하나 추가한다 — **두루마리
카드와는 다른 스타일**(금색·초록 등)로 눈에 띄게. 이미 공유해주신 두루마리
이미지를 카드 3장 배경으로 쓰고 있다면, 이 버튼은 그것과 확실히 구분되는
색으로 만든다.

`LevelUpPanel`(`LevelSystem` 오브젝트에 붙어 있음, 지난 슬라이스에서 확인한
그 구조) 선택 → **Royal Wasabi Button** 필드에 새 버튼 연결.

### 5-5. `LevelSystem` 연결

`LevelSystem` 오브젝트(같은 오브젝트) → **Royal Wasabi Controller** 필드에
5-3의 오브젝트 연결.

---

## Task 6: 플레이테스트

- [ ] **카드 선택 회귀** — 카드 3장 중 하나를 고르는 기존 동작이 그대로 된다
- [ ] **도박 진입** — "와사비를 하사받으러 간다" 클릭 시 카드 패널이 사라지고
  왕궁 배경 + 대사가 뜬다
- [ ] **도박 중 정지** — 왕궁 연출이 떠 있는 동안 몹 스폰·타이머가 멈춰 있다
- [ ] **성공** — (확률을 임시로 1.0으로 올려서 확인 권장) 성공 문구가 뜨고
  공격력·공격속도·이동속도·최대체력이 실제로 오른다(무기 데미지·이동속도
  체감, 체력바 최대치 상승)
- [ ] **실패** — (Success Chance를 임시로 0으로 내려서 확인) 실패 문구가 뜨고
  아무 스탯도 안 바뀐다
- [ ] **확인 버튼** — 결과 확인 후 정상적으로 게임이 재개된다
- [ ] **대기 큐** — 황금 젬으로 레벨이 여러 번 한꺼번에 오른 상태에서 도박을
  선택해도, 도박 후 다음 레벨업 팝업이 이어서 뜬다(마지막이면 바로 재개)
- [ ] **회귀** — 무기 강화 선택지, 보스전, 결과 화면이 이전과 동일하게 동작한다

플레이테스트 전 임시로 바꾼 **Success Chance는 반드시 0.1로 되돌려놓는다.**

전부 통과하면 `main`으로 병합할지 사용자에게 확인한다.

---

## Self-Review 기록

**스펙 커버리지** — 도박 흐름(Task 1, 3, 4), 버프 재사용(Task 1), 왕궁 연출
(Task 2), 4번째 버튼 항상 노출(Task 3), `_panelOpen` 유지(Task 4 주석),
실시간 대기(Task 2), 에디터 배선(Task 5) 전부 대응됨. 결과 화면 미표시·도박
횟수 무제한은 스펙의 "스코프 밖"에 이미 명시돼 있어 별도 태스크 불필요.

**타입 일관성** — `RoyalWasabiController.Show(PlayerStats, PlayerHealth, Action)`이
Task 1 정의와 Task 4 호출부에서 일치. `RoyalWasabiPanel.Show(bool, Action)`이
Task 2 정의와 Task 1 호출부에서 일치. `LevelUpPanel.Show(...)`의 3번째 매개변수
`onRoyalWasabi`가 Task 3 정의와 Task 4의 `panel.Show(options, OnOptionChosen,
HandleRoyalWasabiRequested)` 호출부에서 이름·순서까지 일치.

**의도적 순서** — Task 1은 Task 2(`RoyalWasabiPanel`)가 끝나야 컴파일된다.
Task 3은 Task 4가 끝나야 컴파일된다(옛 `panel.Show` 2-인자 호출이 남아있는
동안은 에러). 둘 다 각 태스크에 그 사실을 명시했다.

**플레이스홀더 스캔** — 없음. 모든 코드 블록이 실제 완성된 내용.
