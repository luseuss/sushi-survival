# 캐릭터 탐색→선택 흐름 + 이나리 선택 제한 설계

> 로드맵(`docs/game-flow-roadmap.md`) 3번 항목. 사용자가 제공한 대본 PDF
> `캐릭터들간의 대화 2.pdf`(1~3페이지) 근거. 2번(세계관 설명, `StoryScene`)에서
> 만든 범용 선형 대화 엔진을 재사용한다.

## 목표

지금은 캐릭터 카드를 클릭하면 곧바로 그 캐릭터로 런이 시작되고, 대화는 한 줄짜리
질문(증강 3택)뿐이다. 이번 작업으로:

1. 아델린·카마리온은 클릭 시 **여러 줄짜리 자기소개 대화**(PDF 1~2페이지)가 먼저
   재생된 뒤, 기존 증강 3택으로 이어진다.
2. 이나리는 클릭 가능해지고(지금은 아예 안 눌림), **선택 제한 안내**와
   **뒤로가기/계속 진행하기** 화면으로 이어진다.
3. 보스전 직전 대화(호감도 대화 #2)를 PDF의 "보스몹 전 말"(대사만, 선택지 없음)로
   교체한다 — 지금 들어있는 임시 증강 3택 문구를 대체한다.

## 결정 기록

| 항목 | 결정 |
|---|---|
| 탐색 방식 | **A안** — 카드 클릭 = 그 캐릭터로 즉시 확정(PDF 그대로). `GameManager.StartRun()`의 스폰 시점은 지금처럼 클릭 즉시 유지. "미리 둘러보고 되돌아가기"는 이나리 전용 특수 케이스로만 존재 |
| 보스전 직전 대화(#2) | PDF대로 **순수 나레이션으로 교체**, 기존 증강 3택(스탯 버프) 기능은 제거 |
| 이나리 카드 외관 | 회색 처리 유지, **클릭은 가능**해야 함 |
| 이나리 제한 문구 | "이나리는 특정 캐릭터 디자인 이슈로 선택이 제한됩니다." (지난 세션에 확정) |

## 흐름

```
카드 클릭 (아델린/카마리온)
  → StartRun()이 즉시 스폰 (지금과 동일한 시점)
  → introLines(여러 줄 나레이션) 클릭/Space/Enter로 순서대로 진행
  → question1(증강 3택, 기존 그대로)
  → 전투 시작

카드 클릭 (이나리, 회색 처리 유지)
  → 캐릭터 선택 패널을 숨기고 제한 안내 표시 (플레이어 스폰 없음)
  → "이전으로 돌아가기" → 캐릭터 선택 패널 복귀
  → "계속 진행하기" → 같은 안내를 다시 보여줌 (실제로는 진행되지 않음)

보스전 직전 (호감도 대화 #2)
  → bossIntroLines(대사만) 순서대로 진행 → 곧바로 보스전
```

## 구성

### 1. 데이터

**`Data/AffinityDialogueData.cs` 변경**

```csharp
public StoryLine[] introLines;              // 신규 — question1 앞에 재생
public AffinityDialogueQuestion question1;   // 유지 — 증강 3택
public StoryLine[] bossIntroLines;           // 신규 — question2(AffinityDialogueQuestion) 대체
```

`question2` 필드는 제거한다. `EggAffinityDialogue.asset`/`ShrimpAffinityDialogue.asset`에
남아있는 기존 `question2` 데이터는 필드 삭제 시 다음 저장에서 조용히 사라지고,
그 자리를 PDF의 "보스몹 전 말" 대사로 새로 채운다. `StoryLine`은 2번 작업에서 만든
타입(`화자, 초상화, 대사, 배경`)을 그대로 재사용한다.

**`Data/CharacterData.cs` 변경**

```csharp
[Tooltip("locked 캐릭터를 클릭했을 때 보여줄 안내 문구.")]
public string lockedMessage;
```

이나리 에셋의 `lockedMessage`에 확정 문구를 넣는다.

### 2. `Core/AffinityDialogueController.cs` 변경

- 새 필드: `[SerializeField] private StoryPanel introPanel;`
- `Show()`(런 시작): `data.introLines`가 있으면 먼저 재생(클릭/Space/Enter로 진행,
  2번에서 만든 `StoryDialogueLogic.NextIndex`/`IsFinished`로 순서 판정) → 끝나면
  기존처럼 `question1`을 `AffinityDialoguePanel`에 표시. `introLines`가 비어 있으면
  지금처럼 바로 `question1`로 간다(호환 유지).
- `ShowSecond()`(보스전 직전): 이제 `data.bossIntroLines`만 재생하고 끝난다.
  증강 판정·`recordBuff` 호출이 없어진다. 시그니처는 그대로 유지한다
  (`GameManager.TriggerBossIntroDialogue()`의 호출부를 안 건드리기 위해 —
  `stats`/`health`/`recordBuff` 인자는 내부적으로 안 쓰이게 된다).
- 두 경우 다 데이터/줄이 비어 있으면 기존 방침대로 즉시 `onComplete`를 불러
  건너뛴다(대본이 아직 없어도 게임 흐름이 끊기면 안 된다).

### 3. 신규 `Core/LockedCharacterController.cs`

- 필드: `characterSelectPanel`(`GameObject`), `introPanel`(`StoryPanel`, 대화창 아트만
  쓰고 배경은 안 건드림), `backButton`(`Button`), `continueButton`(`Button`).
- `Show(CharacterData data)`: `characterSelectPanel.SetActive(false)`, `introPanel`에
  `{ speakerName = data.characterName, text = data.lockedMessage }` 표시.
- `backButton` 클릭 → `introPanel` 숨기고 `characterSelectPanel.SetActive(true)`.
- `continueButton` 클릭 → 같은 문구를 다시 표시(패널을 닫지 않음 — "계속 진행하기"가
  실제로는 진행되지 않는다는 걸 보여준다).
- 씬 전환도, 플레이어 스폰도 없다. `GameManager.CurrentState`는 안 건드린다.

### 4. `UI/CharacterSelectButton.cs` 변경

- `_button.interactable = !locked;` → 항상 `true`(이나리도 클릭 가능해야 함).
  `locked`일 때 `portraitImage.color = Color.gray`는 그대로 유지.
- `OnClicked()`: `locked`면 `GameManager.Instance.StartRun(characterData)` 대신
  `lockedCharacterController.Show(characterData)`를 부른다. 새 필드
  `[SerializeField] private LockedCharacterController lockedCharacterController;` 추가.

### 5. 씬 (`GameScene`)

`docs/COLLABORATION.md` 기준 `GameScene.unity`는 협업자(트랙 A) 단독 소유 씬이다.
이번 작업은 기존 UI를 안 건드리고 새 오브젝트(대화창 재사용 패널, 버튼 2개)만
추가하지만, PR을 올릴 때 협업자에게 알린다(스펙 4번 섹션에 명시).

### 6. 대본 (PDF 1~2페이지 원문)

**아델린 `introLines`** (마지막 줄까지 끝나면 바로 `question1` 증강 3택 표시 —
"1. 공격력 업 2. 방어력 업 3. 스피드 업"은 기존 `question1.choices`로 이미 구현돼
있으므로 텍스트로 따로 안 넣는다):

| # | 화자 | 대사 |
|---|---|---|
| 1 | 아델린 | 만나서 반갑습니다. 저는 스시왕국의 9번째 왕녀, 아델린이라고 합니다. |
| 2 | 플레이어 | 지금 무슨 일이 벌어지고 있는거죠? |
| 3 | 아델린 | 지금 롤왕국의 군대가 우리 왕국에 들이닥치고 있습니다. |
| 4 | 플레이어 | 공주님의 무기는 무엇이죠? |
| 5 | 아델린 | 제 무기는 계란 양산입니다. |
| 6 | 아델린 | 자, 이제 선택해주세요. |

**아델린 `bossIntroLines`:**

| # | 화자 | 대사 |
|---|---|---|
| 1 | 아델린 | 지금 이 소리 들리시나요? 쿠쿵!! 쿠쿵!! 쿠쿵!! |
| 2 | 아델린 | 저 존재는 롤왕국이 길러온 전투 병기입니다. 지금까지 마주했던 적들과 차원이 다르니 조심하셔야 합니다. |

**카마리온 `introLines`:**

| # | 화자 | 대사 |
|---|---|---|
| 1 | 카마리온 | 난 스시왕국 최고의 대장장이 가문의 7대 후계자, 카마리온이라고 한다. 스시왕국의 모든 무기들은 다 우리 가문이 만들었다고 보면 되지, 후훗. |
| 2 | 플레이어 | ...당신이 들고 있는 것은 무엇이죠? |
| 3 | 카마리온 | 이건 나의 무기 간장 권총이지, 요번에 내가 발명한 아주 강력한 무기지. 하하. |
| 4 | 플레이어 | ...멋있네요. |
| 5 | 카마리온 | 그렇게 언제까지 겁쟁이처럼 질문만 하고 있을 거지? 자, 선택해. |

**카마리온 `bossIntroLines`:**

| # | 화자 | 대사 |
|---|---|---|
| 1 | 카마리온 | 지금 이 소리 들리지? 쿠쿵쿠쿵 쿠쿵. |
| 2 | 카마리온 | 드디어 나오는군, 저 자식. 드디어 나의 무기로 저 녀석을 없애버릴 시간이군. 저 녀석은 아까 녀석들이랑 차원이 다르니 조심하라고! |

표기는 2번 작업과 동일하게 "스시왕국"/"롤왕국"으로 통일했다(PDF 원문은 "스시 왕국"이
섞여 있음). "플레이어" 줄은 `speakerName = "플레이어"`, `portrait = null`로 넣는다
(이름표만 뜨고 초상화 창은 숨겨짐 — `StoryPanel`이 이미 지원하는 동작).

## 오류 처리 / 엣지 케이스

- `introLines`/`bossIntroLines`가 비어 있음 → 기존 방침대로 즉시 다음 단계로
  건너뜀(경고 로그만 남김)
- `lockedMessage`가 비어 있음 → `StoryPanel.ShowLine`은 이름표만 뜨고 대사가
  빈 줄로 보인다(크래시는 안 남). 이나리 에셋에는 반드시 채운다
- 이나리를 연타해도 `characterSelectPanel`이 중복으로 꺼지지 않게
  `LockedCharacterController.Show()`는 이미 열려 있으면 문구만 갱신한다
- `AffinityDialogueController.introPanel`이 비어 있으면(에디터 배선 누락)
  `introLines`를 건너뛰고 바로 `question1`로 간다 — 패널 하나 안 꽂았다고
  런 시작 자체가 막히면 안 된다

## 테스트

`AffinityDialogueController`/`LockedCharacterController`/`CharacterSelectButton`은
MonoBehaviour라 기존 관례대로 EditMode 테스트 대상이 아니다. 줄 진행 판정은 이미
`StoryDialogueLogicTests`로 커버돼 있어 새 순수 로직이 없다. 검증은 컴파일 +
Play 모드로:

- 아델린 클릭 → 여러 줄 나레이션 → 증강 3택 → 전투 시작
- 카마리온도 동일
- 이나리 클릭 → 제한 안내 → 뒤로가기 → 캐릭터 선택 복귀 확인
- 이나리 클릭 → 제한 안내 → 계속 진행하기 → 같은 안내 유지(다른 화면으로 안 넘어감) 확인
- 보스전 진입 직전 나레이션이 새 대사로 나오고, 증강 선택 창이 더 이상 안 뜨는지 확인

## 범위 밖 (YAGNI)

- `introLines`/`bossIntroLines`용 건너뛰기 버튼 — 다른 캐릭터를 미리 봤다가 되돌아오는
  구조가 아니라서(A안) 급하지 않다
- 이나리 외 다른 잠금 캐릭터 대비 — `lockedMessage`가 문자열 필드라 이미 확장 가능,
  추가 설계 불필요
- 이나리 전용 초상화 — 2번 작업 때와 동일하게 이름표만 사용, 아트 대기
