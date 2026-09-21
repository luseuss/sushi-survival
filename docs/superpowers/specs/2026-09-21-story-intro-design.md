# 세계관 설명 단계(StoryScene) + 범용 선형 대화 엔진 설계

> 로드맵(`docs/game-flow-roadmap.md`) 2번 항목. 사용자가 준비한 대본 PDF
> `캐릭터들간의 대화 2.pdf`(4페이지)를 근거로 한다.

## 목표

`IntroScene`의 시작 버튼을 누르면 **캐릭터 세 명이 롤왕국의 침공을 이야기하는
짧은 대화 장면**(4페이지 대본)이 나온 뒤 `GameScene`(캐릭터 선택)으로 넘어간다.

이 장면을 위해 만드는 "**대사를 한 줄씩 넘기고, 필요하면 배경이 바뀌는**" 선형 대화
엔진은 범용으로 설계한다. 같은 PDF의 1~3페이지(캐릭터 개별 대화)와 보스전 직전 대사도
결국 같은 형태라, 이번 엔진 위에 대본과 배선만 얹으면 되게 한다.

## 결정 기록

| 항목 | 결정 | 출처 |
|---|---|---|
| 연출 방식 | **대사 넘기기 + 배경 전환** | 사용자 선택 |
| 대본 | PDF 4페이지를 그대로 사용(제가 초안 안 씀) | 사용자 제공 |
| 이나리 | **말하는 인물로 등장**, 초상화는 없음(이름표+대사만). 선택 제한 사유 문구는 "이나리는 특정 캐릭터 디자인 이슈로 선택이 제한됩니다." | 사용자 확정 |
| 표기 통일 | "스시왕국", "롤왕국"으로 통일 (PDF엔 "초밥왕국", "롤 왕국"이 섞여 있음) | **기본값** — 이견 시 수정 |
| "플레이어" 대사 | 이름표만 "플레이어"로 달고 초상화 없이 표시 (이번 4페이지 대본엔 없음, 후속 작업용) | **기본값** |
| 배경 | 왕궁 배경 한 장으로 시작. 줄마다 배경을 지정할 수 있는 필드는 만들어 둠 | **기본값** |

## 범위

**이번에 만든다:** `StoryScene` 신설, 선형 대화 엔진(데이터 + 순수 로직 + UI/컨트롤러),
4페이지 대본 데이터 에셋, `IntroScene` → `StoryScene` 연결, 건너뛰기.

**이번에 안 만든다 (YAGNI):** 타자기 효과, 음성/BGM, "이미 봤는지" 기억(세이브),
선택지 분기, 화면 흔들림/컷 연출. 1~3페이지 캐릭터 개별 대화, 이나리 선택 제한 UI,
보스전 직전 대사는 **후속 작업**(각자 별도 스펙)이다.

## 흐름

```
IntroScene ──[시작]──▶ StoryScene ──[마지막 대사 / 건너뛰기]──▶ GameScene(캐릭터 선택)
```

부스에서 "다시 하기"(ResultScene → IntroScene)를 하면 매번 이 장면을 다시 거치므로,
**건너뛰기 버튼은 필수**다.

## 구성

### 1. 데이터 — `Data/StoryDialogueData.cs` (ScriptableObject)

```csharp
[Serializable] public class StoryLine {
    public string speakerName;        // 이름표에 표시
    public Sprite portrait;           // null이면 미니 초상화 창을 숨김
    [TextArea] public string text;
    public Sprite background;         // null이면 직전 배경 유지
}
public class StoryDialogueData : ScriptableObject { public StoryLine[] lines; }
```

호감도 대화와 달리 선택지·스탯 버프는 없다.

### 2. 순수 로직 — `Core/StoryDialogueLogic.cs` (정적 클래스, EditMode 테스트 대상)

Unity 타입에 의존하지 않도록 "배경이 지정된 줄인가"만 `bool` 목록으로 받는다.

- `NextIndex(current, count)` — 다음 줄 번호. 끝을 넘으면 `count`를 돌려준다
- `IsFinished(index, count)` — 끝 도달 판정(빈 데이터·음수 포함)
- `ResolveBackgroundIndex(hasBackground, index)` — `index` 이하에서 가장 가까운
  "배경이 지정된 줄"의 번호. 없으면 -1
- `IsBackgroundChange(hasBackground, index)` — 이 줄에서 배경이 새로 바뀌는가

### 3. UI — `UI/StoryPanel.cs` + `UI/StorySceneController.cs`

- **`StoryPanel`(뷰):** 전체 화면 배경 `Image` 2장(크로스페이드용), 기존 대화창 아트
  (`대화창.png` — 이름표 탭 + 미니 초상화 + 텍스트 영역) 재사용, 이름표 `Text`, 대사 `Text`.
  `portrait`가 null이면 미니 초상화 창을 끈다.
- **`StorySceneController`(진입점):** `StoryDialogueData`를 들고 있고, 한 줄씩 패널에
  넘긴다. 다음 줄로 넘기는 입력은 **마우스 클릭 / Space / Enter**(새 Input System의
  `Mouse.current`·`Keyboard.current`), **건너뛰기 버튼**은 즉시 `GameScene`으로 이동.
  마지막 줄 다음에도 `GameScene`으로 이동.
- 배경 전환은 짧은 페이드이며 **실시간(`Time.unscaledDeltaTime`)** 으로 진행한다
  (이 프로젝트의 UI 연출 공통 규칙).
- 데이터가 없거나 `lines`가 비어 있으면 경고만 찍고 곧바로 `GameScene`으로 넘어간다
  (호감도 대화 컨트롤러의 "비어 있으면 건너뜀"과 같은 방침).

### 4. 씬과 연결

- `Scenes/StoryScene.unity` 신설 — 씬 배치는 사용자가 에디터에서 한다. 씬 이름은
  정확히 `StoryScene`. `EditorBuildSettings`에 `IntroScene` 다음으로 등록한다.
- `IntroSceneController.OnStartClicked()`의 이동 대상 `"GameScene"` → `"StoryScene"`.
  **협업자가 만든 파일의 한 줄 수정**이라 PR 설명에 명시한다.

### 5. 대본 데이터 — `Data/WorldIntroStory.asset`

PDF 4페이지를 그대로 옮기되 표기만 통일한다(총 11줄). 첫 줄에만 왕궁 배경을 지정하고
나머지는 비워 유지한다. 초상화는 아델린·카마리온은 각 `CharacterData.portraitSprite`,
이나리는 비움.

| # | 화자 | 대사 |
|---|---|---|
| 1 | 이나리 | 공주님 롤왕국에서 엄청난 수의 적군이 몰려오고 있습니다 |
| 2 | 아델린 | ....역시 소문대로군요 |
| 3 | 카마리온 | 병력의 규모는 얼마나 되지? |
| 4 | 이나리 | 정확한 숫자는 모른다 하지만, 내가 파악한 바로는 롤왕국의 총병력이 출동했다고 한다 |
| 5 | 아델린 | 롤왕국이 정말 작정을 했군요 |
| 6 | 카마리온 | 걱정마십시오 공주님 제 손에서 만들어진 무기들은 어떤 왕국보다도 더 훌륭하니까요 |
| 7 | 아델린 | 그럼요 알고있습니다. 카마리온님이 만들어준 제 무기도 훌륭하니까요.. 하지만 저 많은 병력을 우리가 감당할 수 있을까 걱정입니다 |
| 8 | 이나리 | 우리는 전투를 하면 할수록 강해지는 능력이 있으니 걱정마세요 |
| 9 | 카마리온 | 그래요 저 유부 고양이 말이 맞습니다 |
| 10 | 이나리 | 난 여우다!!!!! |
| 11 | 아델린 | 좋습니다. 든든한 지원군이 있으니 안심이 되는군요. 스시왕국을 지키러 다함께 전장으로 향할 때가 되었군요 |

## 오류 처리 / 엣지 케이스

- 데이터 미연결·빈 `lines` → 경고 후 즉시 `GameScene`
- `speakerName` 비어 있음 → 이름표를 숨김(내레이션 용도)
- 배경이 첫 줄에 없음 → 어두운 남색 단색(`IntroScene` 카메라 배경색 `#1A1F2E` 근사)으로
  진행하고, 이후 배경이 지정된 줄에서 처음 표시
- `portrait`가 null → 미니 초상화 창 숨김
- 씬 이름 오타는 이 프로젝트에서 이미 한 번 실제 버그(`"Game"` vs `"GameScene"`)로 났으므로,
  구현 계획에 **Play 모드에서 Intro → Story → Game 전환을 직접 확인**하는 단계를 넣는다

## 테스트

- `StoryDialogueLogic` EditMode 테스트: 진행/끝 판정(빈 데이터·음수·경계),
  배경 유지·전환 판정(첫 줄에 배경 없음, 중간에 바뀜, 연속 미지정)
- UI/컨트롤러/씬 배선은 EditMode 대상이 아니므로(기존 관례) Play 모드로 확인

## 후속 작업 (이 엔진을 재사용)

1. **1~3p 캐릭터 개별 대화 + 이나리 선택 제한** — 선형 대사 뒤에 기존 증강 선택으로
   이어지는 구조, "이전으로 돌아가기/계속 진행하기" 선택창. 이나리 제한 문구는 위
   결정 기록의 문구를 사용한다. 카드 클릭=즉시 런 시작이라는 현재 구조를 바꿔야 해서
   별도 스펙이 필요하다.
2. **보스전 직전 대사** — PDF의 "쿠쿵!! … 차원이 다르니 조심하셔야 합니다"는 선택지가
   없는 선형 대사다. 현재 구현(PR #27)의 `question2`는 제가 쓴 임시 초안이라 대본과
   다르다. 선형 대사 → 기존 선택지 순으로 이어붙일지 별도로 정한다.
