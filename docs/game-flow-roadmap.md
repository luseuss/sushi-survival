# 게임 진행 흐름 정리 (2026-09-15, 2026-09-22 갱신)

> 사용자가 정리한 전체 게임 진행 순서와, 현재 구현 상태를 대조한 문서.
> 이 저장소를 보는 협업자를 위한 공유 메모 — 기획 확정 문서는 아니고, 다음 작업
> 우선순위를 잡기 위한 갭 분석이다.

## 의도한 전체 흐름

```
메인 인트로
  → 캐릭터들이 플레이어에게 세계관 설명
  → 캐릭터 하나하나 들어가 보고 대화 후 캐릭터 선택 (이나리는 구현 안 함)
  → 메인 페이즈 (인게임 전투)
  → 보스전 직전 대화
  → 보스전
  → 게임 종료
```

## 현재 구현 상태 대조

| # | 단계 | 상태 | 비고 |
|---|---|---|---|
| 1 | 메인 인트로 | ✅ 완료 | `IntroScene.unity` — 타이틀 + 시작 버튼. PR #21(신설, 협업자), PR #22(스타일링) |
| 2 | 캐릭터들이 세계관 설명 | ✅ 완료 | `StoryScene.unity` — `IntroScene` 시작 버튼 뒤에 추가. 이나리·아델린·카마리온이 롤왕국 침공을 이야기하는 11줄 선형 대화(사용자 대본 PDF 그대로), 클릭/Space/Enter로 넘기고 건너뛰기 가능. 범용 선형 대화 엔진(`StoryDialogueLogic`/`StoryDialogueData`/`StoryPanel`/`StorySceneController`)으로 만들어서 3번·5번에도 재사용 가능. PR #31~#33(엔진), #36(대본·씬·연결). 스펙: `docs/superpowers/specs/2026-09-21-story-intro-design.md` |
| 3 | 캐릭터 하나하나 들어가 대화 후 선택 | ⚠️ 부분 구현 | 지금은 캐릭터 선택 화면에서 카드 하나를 클릭하면 **그 캐릭터의 호감도 대화 #1이 바로 뜨고 런이 시작**됨(`GameManager.StartRun`). "여러 캐릭터를 미리 둘러보고 대화해본 뒤 최종 선택"하는 **탐색형 흐름은 없음** — 선택과 동시에 런이 확정되는 구조라 되돌릴 수 없음. 이나리는 선택 제한(대본 PDF 3p: "정찰병 임무 때문에 참가 못 함", 확정 문구 "이나리는 특정 캐릭터 디자인 이슈로 선택이 제한됩니다.") UI도 아직 없음 |
| 4 | 메인 페이즈(인게임) | ✅ 완료 | `GameScene.unity` |
| 5 | 보스전 직전 대화 | ✅ 완료 | 원 기획서 "호감도 대화 #2"(Should 우선순위). `AffinityDialogueController.ShowSecond()` + `GameManager.EnterBossFight()` 직전 연결. PR #27(협업자, feature/boss-intro-dialogue). 다만 대본 PDF의 "보스몹 전 말"(1~2p, "쿠쿵!! 차원이 다르니 조심하셔야 합니다")과는 다른 임시 문구가 들어가 있을 수 있음 — 3번 작업 때 같이 확인 |
| 6 | 보스전 | ✅ 완료 | `BossScene.unity` — PR #14(협업자 신설) + PR #19(버그 수정/정리, 이번 세션) |
| 7 | 게임 종료(결과 화면) | ✅ 완료 | `ResultScene.unity` |

## 남은 작업

1. **캐릭터 탐색→선택 흐름 (3번)** — 지금의 "클릭=즉시 런 시작" 구조를 "캐릭터 하나하나 들어가 대화해본 뒤 최종 선택" 구조로 바꿔야 해서, `GameManager.StartRun`/캐릭터 선택 UI 쪽을 다시 설계해야 함. 사용자가 제공한 대본 PDF(`캐릭터들간의 대화 2.pdf`) 1~3페이지에 아델린·카마리온 개별 대화와 이나리 선택 제한(무한반복 선택창) 구조가 이미 있음. 2번에서 만든 선형 대화 엔진(`StoryDialogueLogic`/`StoryDialogueData`/`StoryPanel`)을 재사용할 수 있음
2. **보스전 직전 대화 문구 교체** — 5번은 구조상 완료됐지만, 현재 텍스트(`EggAffinityDialogue`/`ShrimpAffinityDialogue`의 `question2`)는 제가 쓴 임시 초안. 대본 PDF 1~2페이지의 "보스몹 전 말"("지금 이소리 들리시나요? 쿠쿵!! ... 차원이 다르니 조심하셔야 합니다" 등)로 교체 필요 — 3번 작업 때 같이 처리하면 효율적

## 참고 문서

- `CLAUDE.md` — 기획 수치 단일 출처, MoSCoW 스코프(호감도 대화 #2는 Should)
- `docs/COLLABORATION.md` — 씬 소유권, 브랜치/PR 규칙
- 관련 완료 스펙: `docs/superpowers/specs/2026-09-01-affinity-dialogue-1-design.md`, `docs/superpowers/specs/2026-09-21-story-intro-design.md`
