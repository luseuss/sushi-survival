# 세계관 대화 선택 → 성향 태그 설계

> 원 기획: `스시왕국_기획정리_2026-09-26.md`(사용자 로컬 문서) 4절. 3번(보스전)까지는 이미 완료·병합됨.
> 세계관 대화 중 질문 2번의 답이 **성향 태그**로 쌓이고, 그 성향이 호감도 대화·보스 등장 대사·결과 화면
> 세 곳에 작게 반영된다. 분기형 스토리·여러 엔딩은 만들지 않는다(기획서 Won't).

## 확정된 결정

| 항목 | 결정 |
|---|---|
| 성향 3종 | **용감함(Bold) / 신중함(Careful) / 다정함(Kind)** (기획서 예시 그대로) |
| 질문 수 | 스토리 중 2번. 선택지는 질문마다 3개(성향당 하나) |
| 선택의 영향 | 분기 대본 없이 **선택지마다 반응 대사 한 줄**만 달라진다 |
| 대표 성향 | 가장 많이 고른 성향. 동수면 **가장 나중에 고른 성향** |
| 대본 | **제가 초안을 써서** 데이터로 넣고 사용자가 나중에 교체(호감도 대화 #1과 같은 방식) |
| 표정 초상화 | **코드 변경 없음.** `StoryLine`이 이미 줄마다 `portrait`·`standing`을 받는다. 표정 에셋이 임포트되면 인스펙터에서 줄마다 스프라이트만 바꿔 끼운다 |
| 성향 없음(`None`) | 건너뛰기·에디터에서 GameScene 직행 등. 세 곳 모두 **지금 동작 그대로** |

## 스토리 길이

기존 세계관 11줄 + 질문 2줄 + 반응 2줄 = 15줄. 줄당 약 4초라 1분 안에 들어온다.

## 1. 성향 상태

`SushiSurvival.Data.PlayerTrait { None = 0, Bold, Careful, Kind }` — `None`이 0이라 기존 에셋에 새 필드가
없어도 자동으로 `None`이다. 화면 표시명(용감함/신중함/다정함)은 코드에 박지 않고 `PlayerTraitLines` 데이터에 둔다.

`SushiSurvival.Core.PlayerTraitState`(static, 씬 전환을 넘어 유지 — `RunResultCarrier`와 같은 방식):

```csharp
public static class PlayerTraitState
{
    public static IReadOnlyList<PlayerTrait> Picks { get; }
    public static PlayerTrait Dominant { get; }      // PlayerTraitLogic.Dominant(Picks)
    public static void Record(PlayerTrait trait);    // None은 기록하지 않는다
    public static void Reset();
}
```

`SushiSurvival.Core.PlayerTraitLogic.Dominant(IReadOnlyList<PlayerTrait> picks)`(순수 함수):
`None`을 뺀 뒤 성향별 횟수를 세고, 최댓값인 성향이 여럿이면 **picks에서 가장 뒤에 나온 것**을 돌려준다.
고른 것이 없으면 `None`.

`StorySceneController.Start()`가 맨 처음 `PlayerTraitState.Reset()`을 부른다. "다시 하기"가
IntroScene → StoryScene을 거치므로 판마다 초기화된다.

## 2. 스토리 안의 질문

`Data/StoryDialogueData.cs`에 추가(기존 `StoryLine`은 **건드리지 않는다** — `StoryLine`이 자기 자신을 품는
구조는 Unity 직렬화 재귀 문제가 있어 질문 구조를 별도 클래스로 뺀다):

```csharp
[Serializable] public class StoryChoice
{
    [TextArea] public string choiceText;   // 버튼에 표시
    public PlayerTrait trait;
    public StoryLine reply;                // 고른 직후 나오는 반응 대사 한 줄
}

[Serializable] public class StoryChoicePoint
{
    [Tooltip("이 번호의 대사 줄 바로 앞에 질문을 끼운다(0부터).")]
    public int beforeLineIndex;
    public StoryLine prompt;               // 질문 대사(화자·초상화 지정 가능)
    public StoryChoice[] choices;          // 2~3개
}

// StoryDialogueData
public StoryChoicePoint[] choicePoints;
```
필드를 추가만 하므로 기존 `WorldIntroStory.asset`은 그대로 열린다.

`Core/StoryChoiceLogic.cs`(순수 함수): `FindPointIndex(IReadOnlyList<StoryChoicePoint> points, int lineIndex)` —
`beforeLineIndex == lineIndex`인 첫 항목의 인덱스, 없으면 -1.

`UI/StoryChoicePanel.cs`(신규 뷰): 패널 루트 + Legacy `Button` 최대 3개(각각 자식 `Text`).
`Show(StoryChoice[] choices, Action<int> onChosen)`, `Hide()`. 선택지가 3개보다 적으면 남는 버튼은 숨긴다.

`UI/StorySceneController.cs` 수정. 대사 줄 `i`를 보여주기 직전에 `FindPointIndex`로 질문이 있는지 보고
(이미 한 질문은 건너뜀):

```
질문 있음 → panel.ShowLine(point.prompt) + choicePanel.Show(point.choices, k => …)
            [선택 대기 중: 클릭/Space/Enter로 넘기기 무시]
선택 k    → PlayerTraitState.Record(choices[k].trait) → choicePanel.Hide() → panel.ShowLine(choices[k].reply)
            [반응 대사는 평소처럼 클릭으로 넘김]
다음 입력 → 원래 대사 줄 i를 이어서 보여준다
```
- `choicePanel`이 비어 있거나 `choicePoints`가 없으면 지금과 완전히 같은 선형 재생이다.
- 선택 버튼을 누른 같은 프레임의 마우스 떼기가 "다음 줄" 입력으로도 세어지지 않도록, 선택한 프레임(`Time.frameCount`)의
  입력은 무시한다(기존 건너뛰기 버튼 클릭 제외 처리와 같은 문제).
- 건너뛰기 버튼은 선택 대기 중에도 동작한다(성향 `None`으로 진행).
- 배경 전환은 원래 대사 줄이 보일 때 그대로 처리된다(질문·반응 줄은 배경을 바꾸지 않는다).

## 3. 성향이 반영되는 세 곳

### ① 호감도 대화 — 성향에 맞는 선택지를 맨 위로 + ★

- `AffinityDialogueChoice`에 `PlayerTrait trait` 필드 추가(사용자가 인스펙터에서 지정).
  아델린 시작값: 공격력 = 용감함, 방어력 = 신중함, 이동속도 = 다정함.
- `Core/AffinityChoiceOrderLogic.cs`(순수 함수):
  `RecommendedIndex(IReadOnlyList<PlayerTrait> choiceTraits, PlayerTrait dominant)` — `dominant`와 같은 첫 인덱스,
  없거나 `dominant`가 `None`이면 -1. `DisplayOrder(int count, int recommendedIndex)` — 추천 인덱스를 맨 앞으로,
  나머지는 원래 상대 순서 유지. 범위 밖이면 그대로(0..count-1).
- `AffinityDialogueController.ShowQuestion`이 이 로직으로 순서를 바꾸고, 추천 선택지는 `choiceText` 앞에 `"★ "`를 붙인
  **표시용 복사본**을 만들어 패널에 넘긴다. 증강 참조는 그대로라 버프 적용은 변함이 없고 **패널·씬은 안 건드린다.**

### ② 보스 등장 대사 — 성향별 한 줄

- `Data/AffinityDialogueData.cs`: `[Serializable] class TraitLine { PlayerTrait trait; StoryLine line; }`와
  `public TraitLine[] bossTraitLines;` 추가.
- `Core/TraitLineLogic.cs`(순수 함수):
  `AppendTraitLine(StoryLine[] baseLines, IReadOnlyList<TraitLine> traitLines, PlayerTrait dominant)` —
  `baseLines`가 한 줄 이상이고 `dominant`의 줄이 있으면 **끝에 그 한 줄을 붙인 새 배열**을, 아니면 `baseLines`를
  그대로 돌려준다(기존 대사가 없으면 성향 한 줄만 단독으로 재생하지 않는다).
- `BossFightDirector.Start`가 `IntroSequence`에 넘기는 `bossEncounterLines` 자리에서 이 함수를 거친다.

### ③ 결과 화면 — 한마디

- `Data/PlayerTraitLines.cs`(ScriptableObject, `Assets/_Project/Resources/PlayerTraitLines.asset`):
  성향별 `{ PlayerTrait trait; string displayName; string victoryLine; string defeatLine; }` 배열과
  `Find(PlayerTrait)`, 정적 `Load()`(`Resources.Load`, `BoothResetSettings`와 같은 선례라 **씬 배선이 필요 없다**).
- `UI/ResultPanel.cs`: 선택 필드 `[SerializeField] private Text traitText;` 추가. `Show`에서 대표 성향의 항목을 찾아
  `$"[{displayName}] {한마디}"`를 표시하고, 항목이 없거나 `None`이면 그 텍스트를 숨긴다. 필드가 비어 있으면 아무것도 안 한다.
- 결과 패널은 GameScene과 BossScene 양쪽에 있어서 **두 씬에 Text를 하나씩** 추가한다.

## 4. 대본 초안 (데이터, 사용자가 나중에 교체)

| 대상 | 내용 |
|---|---|
| `WorldIntroStory.asset` | `choicePoints` 2개. 질문 1은 줄 4 앞, 질문 2는 줄 8 앞이 시작값(대본을 쓸 때 실제 문맥에 맞게 조정). 질문은 세계관 대화 속 인물이 묻고, 선택지 3개(용감함·신중함·다정함)마다 반응 한 줄 |
| `EggAffinityDialogue.asset`, `ShrimpAffinityDialogue.asset` | `bossTraitLines` 3개씩(성향별 한 줄). 기존 데이터에는 새 배열만 **덧붙인다** |
| `PlayerTraitLines.asset` (신규) | 성향 3개 × (표시명, 승리 한마디, 패배 한마디) |
| 호감도 선택지 성향 태그 | 기존 에셋 안쪽 필드라 스크립트로 건드리지 않고 **사용자가 인스펙터에서 지정**(캐릭터당 3개) |

대본 텍스트는 데이터 PR에서 사용자가 읽고 고칠 수 있다. 데이터 에셋 테스트는 **구조만** 검증한다(질문 2개, 각 질문의
선택지에 세 성향이 모두 있는지, 반응 대사가 비어 있지 않은지, 성향 3개의 결과 한마디가 있는지). 문구·줄 번호 같은
절대값은 assert하지 않는다.

## 5. 씬 배선 (사용자 에디터 작업, 마지막 PR)

- **StoryScene:** `StoryChoicePanel` 오브젝트(버튼 3개 + 각 Text)를 만들고 `StorySceneController`의 `Choice Panel`에 연결.
- **GameScene·BossScene:** 각 `ResultPanel`에 Text를 추가하고 `Trait Text`에 연결.
- **Egg/Shrimp 호감도 선택지:** 각 선택지의 `Trait` 드롭다운 지정.

## 6. 테스트

- `PlayerTraitLogicTests`: 단독 최다, 동수 시 가장 나중 선택, 3자 동수(마지막 것), `None` 무시, 빈 목록.
- `AffinityChoiceOrderLogicTests`: 일치 인덱스, 일치 없음, `dominant == None`, 중복 성향(첫 것), `DisplayOrder`(맨 앞 이동·상대 순서 유지·범위 밖).
- `TraitLineLogicTests`: 성향 줄 붙임, 성향 줄 없음, `baseLines`가 비어 있음(단독 재생 안 함), `None`.
- `StoryChoiceLogicTests`: 해당 줄 앞 질문 찾기, 없음, 같은 줄에 여러 개면 첫 것, 빈/null 목록.
- 데이터 에셋 구조 테스트(위 4절).
- `StoryChoicePanel`·`StorySceneController`·`AffinityDialogueController`·`BossFightDirector`·`ResultPanel`은
  MonoBehaviour라 컴파일+회귀만 확인하고, 실제 흐름은 Play 모드로 확인한다(사용자).

## 7. 구현 분할 (계획서에서 확정)

전부 **추가만 하는 변경**이라 원자적으로 묶을 PR은 없다. 순수 로직은 자기가 쓰는 데이터 타입이 생기는 PR에 같이 둔다
(`StoryChoiceLogic`은 `StoryChoicePoint`가 있는 쪽, `TraitLineLogic`은 `TraitLine`이 있는 쪽).

| PR | 내용 | 의존 |
|---|---|---|
| A | `PlayerTrait`·`PlayerTraitState`·`PlayerTraitLogic`·`AffinityChoiceOrderLogic` + 테스트 | 없음 |
| B | 데이터 스키마(`StoryChoicePoint`·`AffinityDialogueChoice.trait`·`TraitLine`·`PlayerTraitLines`) | A |
| C | `StoryChoiceLogic` + `StoryChoicePanel` + `StorySceneController` 질문 처리 | A·B |
| D | `TraitLineLogic` + 세 소비처(호감도 컨트롤러·`BossFightDirector`·`ResultPanel`) | A·B |
| E | 대본 초안 데이터 + 구조 테스트 | B |
| F | 씬 배선(사용자 에디터 작업) | C·D·E |

C, D, E는 서로 파일이 안 겹쳐 B 이후 병렬로 진행한다.

## 스코프 밖

- 표정 초상화 에셋 임포트와 줄별 지정(에셋이 준비되면 인스펙터 작업만)
- 성향에 따른 스토리 분기·엔딩, 성향에 따른 스탯 보너스
- 효과음, 선택 연출(강조 애니메이션 등)
- 보스 직전 대화(`bossIntroLines`)에 성향 반영(기획서에는 "등장 직후 대사"만 있다)
