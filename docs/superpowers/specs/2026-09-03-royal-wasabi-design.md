# 왕의 와사비 하사 — 레벨업 도박 시스템 설계

> 작성 2026-09-03. 레벨업 3택 화면에 4번째 선택지를 추가한다. 게임 제목
> ("와사비를 먹으면 강해지는 군요")과 직결되는 핵심 기믹.

## 목표

레벨업 3택 카드 아래에 항상 떠 있는 4번째 버튼 — **"와사비를 하사받으러 간다"**.
누르면 이번 레벨업의 카드 선택을 포기하고 대신 도박한다. 10% 확률로 주요 스탯
4종을 크게(각 maxCap의 50%) 강화하는 "빛나는 와사비"를 받고, 90%는 아무것도
못 얻은 채 그 레벨업을 날린다.

## 왜 "대체"이지 "보너스"가 아닌가

보너스로 만들면 매번 눌러도 손해가 없는 공짜 복권이 된다. **대체로 만들어야
"안정적인 카드 하나를 포기하고 도박할 것인가"라는 진짜 선택이 되고, 그래야
긴장감이 생긴다.** 실패하면 그 레벨업에서 정말 아무것도 못 얻는다 — 위로 보상
없음.

## 흐름

```
LEVEL UP 화면 (카드 3장 + "와사비를 하사받으러 간다" 버튼, 항상 노출)
 │
 ├─ 카드 하나 선택 → 기존 그대로. 변경 없음.
 │
 └─ "하사받으러 간다" 클릭
      ├─ 카드 3장 패널 숨김
      ├─ 왕궁 배경(궁전 복도 이미지) + 짧은 대사 한 줄, 실시간 대기(연출)
      ├─ 10% 확률 판정
      │    성공 → "빛나는 와사비를 하사받았다!" + 스탯 4종 강화
      │    실패 → "오늘은 빈손으로 돌아왔다" (레벨업 소모, 보상 없음)
      ├─ 확인 버튼
      └─ 기존 레벨업 흐름으로 복귀 — 대기 중인 레벨업이 더 있으면 다음 팝업,
         없으면 게임 재개
```

**도박 횟수 제한 없음.** 매 레벨업마다 원하면 계속 시도할 수 있다.

## 버프 계산 — 새 로직을 만들지 않는다

호감도 대화 #1에서 만든 `AffinityBuffLogic.GetBuffAmount(maxCap, 비율)` +
`AffinityBuffApplier.Apply(augment, amount, stats, health)`를 그대로 재사용한다.
비율만 다르게(대화는 12.5%, 이건 **50%**) 기존 증강 SO 4개에 순서대로 적용한다.

```csharp
private static readonly StatType[] Targets = /* AttackDamage, AttackSpeed, MoveSpeed, MaxHealth 순서 */;

foreach (var augment in fourAugments)
{
    float amount = AffinityBuffLogic.GetBuffAmount(augment.maxCap, buffRatio); // buffRatio = 0.5
    AffinityBuffApplier.Apply(augment, amount, stats, health);
}
```

**실제 수치(현재 증강 SO 기준):**

| 증강 | maxCap | 50% 적용 시 |
|---|---|---|
| `Aug_AttackDamage` | 2.0 | +1.0 (배율 1.0→2.0, 데미지 약 2배) |
| `Aug_AttackSpeed` | 1.0 | +0.5 (StatSystem 하드캡 2.0으로 최종 클램프) |
| `Aug_MoveSpeed` | 1.2 | +0.6 (이동속도 약 1.6배) |
| `Aug_MaxHealth` | 300 | +150 (기본 체력 100 기준 최대체력 2.5배) |

`AttackSpeed`는 `PlayerStats.Awake`가 이미 하드캡 2.0을 걸어두고 있어서, 다른
증강을 이미 많이 찍은 상태에서 이 버프까지 받아도 `StatSystem`이 최종값을
안전하게 클램프한다 — 새로 막을 것이 없다.

**인게임 레벨업 증강 누적과 완전히 독립이다.** 호감도 대화 버프와 같은 이유 —
와사비로 받은 강화가 이후 레벨업 선택지 등장을 갉아먹으면 안 된다.

## 새 컴포넌트 — `AffinityDialogueController`와 같은 모양

| 파일 | 책임 | 신규/수정 |
|---|---|---|
| `Core/RoyalWasabiController.cs` | 확률 판정 + 4종 버프 적용, 진입점 | 신규 |
| `UI/RoyalWasabiPanel.cs` | 왕궁 배경 + 대사 + 결과 표시 + 확인 버튼 | 신규 |
| `UI/LevelUpPanel.cs` | 4번째 버튼 추가, 클릭 시 카드 숨기고 컨트롤러 호출 | 수정 |
| `Core/LevelSystem.cs` | 도박 완료 후 기존 흐름(다음 대기 or 종료)으로 복귀 | 수정 |

`LevelUpPanel`은 확률을 직접 굴리지 않는다 — 버튼 클릭을 relay만 하고, 로직은
`RoyalWasabiController`가 갖는다. 지금 `IUpgradeOption` 선택 콜백과 정확히 같은
구조다.

### `RoyalWasabiController`

```csharp
namespace SushiSurvival.Core
{
    /// <summary>
    /// 레벨업 카드 대신 고르는 도박. 10% 확률로 스탯 4종을 크게 강화하고,
    /// 실패하면 그 레벨업에서 아무것도 못 얻는다 — 보너스가 아니라 대체라서
    /// 위로 보상이 없다.
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
            if (augment == null) return;

            float amount = AffinityBuffLogic.GetBuffAmount(augment.maxCap, buffRatio);
            AffinityBuffApplier.Apply(augment, amount, stats, health);
        }
    }
}
```

### `RoyalWasabiPanel`

왕궁 배경 Image + 대사 Text(고정 문구, 실시간 대기로 잠깐 보여줌) + 결과
Text(성공/실패 문구 전환) + 확인 버튼. `AffinityDialoguePanel`과 같은 뼈대 —
`Show(bool success, Action onConfirm)` 하나로 성공/실패 문구를 갈아끼우고
확인 버튼 클릭을 relay한다.

**연출은 반드시 실시간(`WaitForSecondsRealtime`)으로 진행한다.** 레벨업 팝업이
뜨는 순간 이미 `Time.timeScale = 0`이라, scaled 시간을 쓰면 대사 대기 자체가
멈춘다 — 레벨업 팝업 스케일인 애니메이션(타격감 슬라이스)과 같은 함정이다.

### `LevelUpPanel` 변경

```csharp
[SerializeField] private Button royalWasabiButton; // 신규

public void Show(IReadOnlyList<IUpgradeOption> options, Action<IUpgradeOption> onChosen, Action onRoyalWasabi)
{
    // 기존 카드 바인딩 그대로...

    royalWasabiButton?.onClick.RemoveAllListeners();
    royalWasabiButton?.onClick.AddListener(() => onRoyalWasabi?.Invoke());
}
```

카드 3장과 무관하게 **항상 활성화**돼 있다 — 증강 풀이 고갈되든 말든 도박은
언제나 가능하다.

### `LevelSystem` 변경

```csharp
[SerializeField] private RoyalWasabiController royalWasabiController;

private void ShowNext()
{
    // ...기존 while 루프에서 panel.Show(options, OnOptionChosen)를
    panel.Show(options, OnOptionChosen, HandleRoyalWasabiRequested);
    // ...
}

private void HandleRoyalWasabiRequested()
{
    panel.Hide();
    // _panelOpen은 true로 유지한다 — 왕궁 연출 중에도 게임은 계속 정지 상태.

    royalWasabiController.Show(_playerStats, _playerHealth, ShowNext);
}
```

`_panelOpen`은 `CloseAndResume()`에서만 `false`로 돌아간다 — 왕궁 연출이
끝나기 전까지 게임이 재개되면 안 되기 때문이다.

## 스코프 밖

- 결과 화면(승리/패배 후 요약)에 와사비 획득 여부가 안 뜬다 — 호감도 대화
  버프도 지금 그 화면 증강 목록에 안 잡히는 것과 같은 이유(같은 독립 적용
  경로를 씀). 나중에 같이 손볼 수 있다
- 도박 횟수 제한, 쿨타임 없음
- 작은 아이콘 바 이미지(이번 대화에서 같이 공유된 것)는 이번 설계에 안 씀

## 에디터 작업 (사용자가 직접)

1. 왕궁 배경 이미지 임포트, `RoyalWasabiPanel` UI 조립(배경 Image + 대사
   Text + 결과 Text + 확인 Button, 전부 Legacy)
2. `RoyalWasabiController` 오브젝트 생성, Panel 연결, 4개 `AugmentData`
   필드(`Aug_AttackDamage`/`Aug_AttackSpeed`/`Aug_MoveSpeed`/`Aug_MaxHealth`)
   연결, Success Chance 0.1 / Buff Ratio 0.5
3. `LevelUpPanel`에 4번째 버튼 추가(두루마리 배경과는 다른 스타일 — 금색/초록
   등으로 눈에 띄게), 카드 3장과 함께 항상 보이게 배치
4. `LevelSystem`의 `Royal Wasabi Controller` 필드 연결

## 테스트 계획

`RoyalWasabiController`의 확률 판정과 버프 적용은 `System.Random` + Unity
컴포넌트 호출이 섞여 있어 순수 단위 테스트로 분리하기 애매하다 — 대신
`AffinityBuffLogic.GetBuffAmount`가 이미 검증돼 있으므로, 여기서는 새 로직이
없다(비율만 다른 재호출). 플레이테스트로 확인:

- 카드 선택은 기존과 동일하게 동작
- "하사받으러 간다" 클릭 시 카드 패널이 사라지고 왕궁 연출이 뜬다
- 성공 시 스탯 4종이 실제로 오르는지(무기 데미지·이동속도 체감, 체력바 최대치 증가)
- 실패 시 아무 스탯도 안 바뀌고 정상적으로 게임이 재개된다
- 도박 중에도 몹 스폰·타이머가 멈춰 있다(대화 #1과 동일한 정지 보장)
- 대기 중인 레벨업이 여러 개일 때, 도박 후 다음 레벨업 팝업이 이어서 뜬다
