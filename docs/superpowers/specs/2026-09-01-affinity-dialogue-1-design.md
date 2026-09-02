# 호감도 대화 #1 설계

> 작성 2026-09-01. Must 스코프의 마지막 항목. 대화 #2(보스전 인터럽트)는
> 스코프 밖이며, `BossDirector.IntroSequence`에 이미 그 자리가 주석으로
> 표시돼 있다.

## 목표

캐릭터 선택 직후, 런이 실제로 시작되기 전에 2~3지 선택형 대화 한 세트를 넣는다.
선택은 즉시 스탯 버프로 이어진다. 새 버프 시스템을 만들지 않고 기존 증강
10종(`AugmentData`)을 그대로 가리켜서 적용한다.

## 왜 "런 시작 직전"이 타이밍 문제인가

버프를 적용하려면 `PlayerStats`가 있어야 하는데, 그건 플레이어가 스폰돼야
존재한다. 그래서 실제 구현은 **플레이어는 스폰하되, 적 스폰과 타이머는 대화가
끝날 때까지 미루는** 방식이다. 화면에는 캐릭터가 서 있지만 몹도 안 나오고
시간도 안 흐르므로, 플레이어 체감으로는 "아직 런이 시작 안 한" 상태다.

## 데이터 모델

`AugmentData`(증강 10종 SO)를 그대로 재사용한다 — 이름·아이콘·`StatType`·
`maxCap`이 이미 다 있다. 대화 선택지는 여기에 서사용 대사 하나만 얹는다.

```csharp
namespace SushiSurvival.Data
{
    [System.Serializable]
    public class AffinityDialogueChoice
    {
        [TextArea]
        public string choiceText;
        [Tooltip("이 선택이 매핑되는 증강. 이름·아이콘·StatType·maxCap을 여기서 가져온다.")]
        public AugmentData augment;
    }

    [System.Serializable]
    public class AffinityDialogueQuestion
    {
        [TextArea]
        public string questionText;
        [Tooltip("2~3개.")]
        public AffinityDialogueChoice[] choices;
    }

    [CreateAssetMenu(menuName = "SushiSurvival/Affinity Dialogue Data", fileName = "NewAffinityDialogueData")]
    public class AffinityDialogueData : ScriptableObject
    {
        public AffinityDialogueQuestion question1;
    }
}
```

`CharacterData`에 필드 하나만 추가한다:

```csharp
[Tooltip("호감도 대화 #1 데이터. 비워두면 대화 없이 바로 런이 시작된다.")]
public AffinityDialogueData affinityDialogue;
```

**비워두면 대화를 건너뛴다.** 아직 대본이 없는 캐릭터(이나리 등)도 시스템은
미리 켜둘 수 있고, 대본이 준비된 캐릭터부터 순차적으로 채워 넣을 수 있다.

## 런 상태 — `GameManager.StartRun`을 둘로 쪼갠다

**새 `RunState.Intro`가 필요하다.** `AddExperience`·`RegisterKill`·
`WaveDirector.Update`가 전부 `CurrentState == Playing`으로 가드돼 있어서,
대화 중에 상태를 `Playing`으로 두면 안 된다. 보스전 설계 때 "새 상태 추가
금지"를 결정했던 것과 같은 함정이지만 방향이 반대다 — 거기서는 그 시스템들이
계속 돌아야 했고, 여기서는 **잠자고 있어야** 한다.

대화 #2(보스전 인터럽트)는 여전히 `RunState.Playing`을 유지한 채로 간다 —
`Intro`는 대화 #1 전용이다.

```csharp
public enum RunState
{
    CharacterSelect,
    Intro,      // 신규 — 플레이어는 스폰됐지만 전투는 아직 시작 전
    Playing,
    Result
}
```

`StartRun`의 흐름:

```
StartRun(character)
 ├ 플레이어 스폰 + 스탯/체력/카메라 배선 (기존과 동일)
 ├ 캐릭터 선택 패널 끔 — 버튼 재입력 방지를 위해 지금보다 앞으로 당긴다
 ├ character.affinityDialogue가 있으면
 │    CurrentState = Intro
 │    affinityDialogueController.Show(대화데이터, 초상화, stats, health, BeginCombat)
 └ 없으면
      BeginCombat()

BeginCombat()  ← 대화 선택 완료 시 콜백으로도 호출된다
 ├ enemySpawner.StartSpawning(player.transform)
 ├ waveDirector.StartTimeline(player.transform)
 └ ElapsedTime = 0, KillCount = 0, CurrentState = Playing
```

`player`(스폰된 GameObject)는 지역 변수로 캡처해 `BeginCombat`이 로컬 함수/람다로
그대로 참조한다 — 새 필드를 추가할 필요가 없다.

## 버프 적용 — 독립 보너스

인게임 레벨업의 증강 픽업 누적 상한(`LevelSystem._accumulated`)과 **별개로
치며 거기 섞이지 않는다.** 런 시작부터 작은 보너스를 받고, 이후 레벨업
선택지는 이전과 동일하게 전부 뜬다(이미 캡을 채웠다고 걸러지지 않는다).

```csharp
namespace SushiSurvival.Core
{
    public static class AffinityBuffLogic
    {
        public static float GetBuffAmount(float maxCap, float ratio)
            => Mathf.Max(0f, maxCap) * Mathf.Clamp01(ratio);
    }
}
```

적용은 `AugmentOption.Apply()`와 같은 패턴(모디파이어 추가 + 최대체력이면
현재체력도 같이 올림)이지만 별도 헬퍼로 둔다 — `AugmentOption`은
`Data.valuePerPick`을 읽는 구조라 다른 값(비율×maxCap)을 넣으려면 억지로
끼워 맞춰야 한다.

```csharp
namespace SushiSurvival.Core
{
    public static class AffinityBuffApplier
    {
        public static void Apply(AugmentData augment, float amount, PlayerStats stats, PlayerHealth health)
        {
            stats.AddModifier(new StatModifier
            {
                Stat = augment.statType,
                Type = ModifierType.Additive,
                Value = amount
            });

            if (augment.statType == StatType.MaxHealth && health != null)
                health.GrantMaxHealthIncrease(amount);
        }
    }
}
```

**비율은 인스펙터 필드로 노출한다.** 기본값 **0.125**(12.5%, 기획서
10~15% 범위의 중간값). `AffinityDialogueController`가 갖는다.

## UI

`LevelUpOptionButton`과 같은 뼈대로 새 버튼을 만든다 — 다만 표시할 텍스트가
증강 이름이 아니라 대사라서 그대로 재사용할 수 없다.

```csharp
namespace SushiSurvival.UI
{
    [RequireComponent(typeof(Button))]
    public class AffinityChoiceButton : MonoBehaviour
    {
        [SerializeField] private Text choiceText;
        [Tooltip("이 선택이 매핑된 증강 아이콘. 비워두면 표시하지 않는다.")]
        [SerializeField] private Image iconImage;

        private Button _button;
        private AffinityDialogueChoice _choice;
        private Action<AffinityDialogueChoice> _onChosen;

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

        public void Bind(AffinityDialogueChoice choice, Action<AffinityDialogueChoice> onChosen)
        {
            _choice = choice;
            _onChosen = onChosen;

            gameObject.SetActive(true);

            if (choiceText != null)
                choiceText.text = choice.choiceText;

            if (iconImage != null)
            {
                iconImage.sprite = choice.augment != null ? choice.augment.icon : null;
                iconImage.enabled = iconImage.sprite != null;
            }
        }

        public void Clear()
        {
            _choice = null;
            _onChosen = null;
            gameObject.SetActive(false);
        }

        private void HandleClick()
        {
            if (_choice == null) return;

            _onChosen?.Invoke(_choice);
        }
    }
}
```

패널:

```csharp
namespace SushiSurvival.UI
{
    public class AffinityDialoguePanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text questionText;
        [Tooltip("선택지 버튼 최대 3개.")]
        [SerializeField] private AffinityChoiceButton[] choiceButtons;

        private GameObject Root => root != null ? root : gameObject;

        private void Awake() => Hide();

        public void Show(Sprite portrait, AffinityDialogueQuestion question, Action<AffinityDialogueChoice> onChosen)
        {
            Root.SetActive(true);

            if (portraitImage != null)
                portraitImage.sprite = portrait;

            if (questionText != null)
                questionText.text = question.questionText;

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (i < question.choices.Length)
                    choiceButtons[i].Bind(question.choices[i], onChosen);
                else
                    choiceButtons[i].Clear();
            }
        }

        public void Hide() => Root.SetActive(false);
    }
}
```

컨트롤러(`GameManager`가 참조하는 진입점):

```csharp
namespace SushiSurvival.Core
{
    public class AffinityDialogueController : MonoBehaviour
    {
        [SerializeField] private SushiSurvival.UI.AffinityDialoguePanel panel;
        [Tooltip("버프 = 증강 maxCap × 이 비율. 기획서 권장 10~15%의 중간값.")]
        [Range(0f, 1f)]
        [SerializeField] private float buffRatio = 0.125f;

        public void Show(AffinityDialogueData data, Sprite portrait,
                         PlayerStats stats, PlayerHealth health, Action onComplete)
        {
            if (data == null || data.question1 == null ||
                data.question1.choices == null || data.question1.choices.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            panel.Show(portrait, data.question1, choice =>
            {
                if (choice.augment != null)
                {
                    float amount = AffinityBuffLogic.GetBuffAmount(choice.augment.maxCap, buffRatio);
                    AffinityBuffApplier.Apply(choice.augment, amount, stats, health);
                }

                panel.Hide();
                onComplete?.Invoke();
            });
        }
    }
}
```

## 대본 초안 — 계란(아델린)·간장새우(카마리온)

**초안이다. 사용자가 직접 다듬는다.** 톤·워딩은 세계관 담당자가 최종 확정한다.
두 캐릭터 모두 같은 세 카테고리(공격력·방어력·이동속도)로 맞췄다 — 기획서
예시("저돌적으로 싸운다"=공격력, "신중하게 버틴다"=방어력, "재빠르게
움직인다"=이동속도)를 따른 것이며, 데이터일 뿐이라 나중에 캐릭터마다 다른
카테고리로 바꿔도 코드는 안 바뀐다.

### 계란 (아델린) — 우아하고 다정하지만 단호한 톤

> 질문: "몰려오는 적들 앞에서, 넌 어떻게 싸울 거야?"

| 선택지 대사 | 매핑 |
|---|---|
| "제 양산이 닿는 곳은 전부 쓸어버리겠어요." | 공격력 |
| "무너지지 않는 게 먼저예요. 버틸 수 있어야 이길 수 있으니까요." | 방어력 |
| "발이 빠르면, 애초에 다칠 일이 없죠." | 이동속도 |

### 간장새우 (카마리온) — 간결하고 사무적인 저격수 톤

> 질문: "전장에 나서기 전에 확인한다. 네 방식은 뭐지?"

| 선택지 대사 | 매핑 |
|---|---|
| "한 방에 끝낸다." | 공격력 |
| "먼저 버티는 쪽이 이긴다." | 방어력 |
| "맞기 전에 피한다." | 이동속도 |

## 재시작·결과 화면과의 상호작용

`GameManager.Restart()`는 씬을 통째로 리로드하므로 `RunState.Intro`를 포함한
모든 상태가 자동으로 초기화된다. 별도 처리가 필요 없다.

## 스코프 밖

- 호감도 대화 #2 (보스전 인터럽트) — 별도 슬라이스. `BossDirector`에 이미
  삽입 지점이 표시돼 있다
- 표정 배리에이션 초상화 — 아트 없음. `CharacterData.portraitSprite`(캐릭터
  선택 화면과 같은 그림) 재사용
- 이나리 — 캐릭터 자체가 아직 없음. `affinityDialogue`를 비워두면 시스템은
  아무 문제 없이 건너뛴다

## 테스트 계획

`AffinityBuffLogic.GetBuffAmount`만 순수 함수라 EditMode로 검증한다:
기본 비율에서의 계산값, `ratio`가 0/1 경계, `maxCap`이 음수일 때 0으로
클램프, `ratio`가 1을 넘거나 음수일 때 클램프.

나머지(컨트롤러·패널·버튼·`GameManager` 상태 전환)는 MonoBehaviour와
Unity 생명주기에 묶여 있어 플레이테스트로 확인한다.

## 에디터 작업 (사용자가 직접)

1. `AffinityDialogueData` 에셋 2개 생성(계란용, 간장새우용) — 위 대본 초안을
   질문·선택지 텍스트에 입력하고, 선택지마다 해당 `AugmentData` 연결
2. `EggCharacterData`/`ShrimpCharacterData`의 `Affinity Dialogue` 필드에
   각각 연결
3. `AffinityDialoguePanel` UI 조립 — 초상화 Image, 질문 Text(Legacy),
   선택지 버튼 3개(`AffinityChoiceButton`)
4. `AffinityDialogueController` 씬 오브젝트 생성, Panel 연결
5. `GameManager`의 `Affinity Dialogue Controller` 필드 연결
