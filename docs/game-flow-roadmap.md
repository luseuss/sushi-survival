# 게임 진행 흐름 정리 (2026-09-15, 2026-09-22, 2026-09-26 갱신)

> 사용자가 정리한 전체 게임 진행 순서와, 현재 구현 상태를 대조한 문서.
> 이 저장소를 보는 협업자를 위한 공유 메모 — 기획 확정 문서는 아니고, 다음 작업
> 우선순위를 잡기 위한 갭 분석이다.

## 의도한 전체 흐름

```
메인 인트로
  → 캐릭터들이 플레이어에게 세계관 설명
  → 캐릭터 하나하나 들어가 보고 대화 후 캐릭터 선택 (이나리는 선택 제한 안내로 처리)
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
| 3 | 캐릭터 하나하나 들어가 대화 후 선택 | ✅ 완료 | 카드 클릭 시(아델린/카마리온) 자기소개 나레이션(`introLines`, 6줄/5줄, 대본 PDF 그대로) → 증강 3택으로 이어지고, 이나리는 클릭하면 캐릭터 선택 화면 대신 제한 안내("이나리는 특정 캐릭터 디자인 이슈로 선택이 제한됩니다.")가 뜨며 뒤로가기/계속하기(무한반복)로 오감. 2번에서 만든 선형 대화 엔진(`StoryDialogueLogic`/`StoryPanel`) 재사용. `LockedCharacterController`(이나리 전용), `AffinityDialogueController`(나레이션 재생 지원으로 교체). PR #38~#40, #42, #44. 스펙: `docs/superpowers/specs/2026-09-22-character-select-flow-design.md` |
| 4 | 메인 페이즈(인게임) | ✅ 완료 | `GameScene.unity` |
| 5 | 보스전 직전 대화 | ✅ 완료 | 원 기획서 "호감도 대화 #2"(Should 우선순위). `AffinityDialogueController.ShowSecond()`가 증강 선택 없이 `bossIntroLines`(대본 PDF의 "보스몹 전 말" 그대로, PR #44에서 교체)만 재생하고 `GameManager.EnterBossFight()` 직전에 연결됨 |
| 6 | 보스전 | ✅ 완료 | `BossScene.unity` — PR #14(협업자 신설) + PR #19(버그 수정/정리) + 이후 협업자의 연출·패턴 작업 다수 |
| 7 | 게임 종료(결과 화면) | ✅ 완료 | `ResultScene.unity`는 없어지고 `ResultPanel`이 `GameScene`/`BossScene` 안 오버레이로 통합됨(PR #43, 협업자) |

## 남은 작업

현재 의도한 전체 흐름(1~7번) 전부 구현 완료. 이후 작업은 대부분 아트/연출/밸런스 다듬기(부스 데모 대비 아이들 리셋, 버튼 호버 영상, 보스 연출 등) 위주로 이 문서의 갭 분석 범위를 벗어남 — 필요하면 새 갭이 생길 때 이 문서에 추가.

## 참고 문서

- `CLAUDE.md` — 기획 수치 단일 출처, MoSCoW 스코프(호감도 대화 #2는 Should)
- `docs/COLLABORATION.md` — 씬 소유권, 브랜치/PR 규칙
- 관련 완료 스펙: `docs/superpowers/specs/2026-09-01-affinity-dialogue-1-design.md`, `docs/superpowers/specs/2026-09-21-story-intro-design.md`, `docs/superpowers/specs/2026-09-22-character-select-flow-design.md`
