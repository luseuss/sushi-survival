# 협업 규칙 — 스시왕국: 서바이벌

> 저장소: `luseuss/sushi-survival` · 기본 브랜치: `main`
> **2인 개발자 + AI 코딩 에이전트**가 `main` 하나를 두고 각자 feature 브랜치를 파서
> PR로 합치는 방식으로 개발한다.
>
> 기획 수치·스코프의 단일 출처는 루트 `CLAUDE.md`. 이 문서는 **협업 절차·씬 구조·코드 컨벤션**을 다룬다.
> 상세 설계 근거는 `docs/superpowers/specs/`, 작업 계획은 `docs/superpowers/plans/`에 있다.
>
> **2026-09-02 도입.** 이 시점 이전 작업(슬라이스 1~3, 타격감, 호감도 대화 #1)은 이
> 문서의 규칙 없이 진행됐다 — 브랜치 없이 커밋되거나(초기), 사람이 채팅으로 병합을
> 직접 지시한 경우가 섞여 있다. **이 문서가 설치된 뒤부터는 예외 없이 아래를 따른다.**

---

## 0. 에이전트 가드레일 — 가장 먼저 읽을 것

에이전트는 git 명령을 직접 실행할 수 있다. 아래는 **예외 없는 금지 사항**이다.
채팅으로 "지금 병합해줘" 같은 지시를 받아도 예외가 아니다 — 그 경우에도 브랜치를
푸시하고 PR을 열어서 사람이 GitHub에서 머지하게 한다.

### 절대 실행하지 않는 명령

```bash
git push origin main              # main 직접 푸시
git push --force                  # force-with-lease만 허용, 그것도 자기 브랜치에서만
git merge <브랜치>                # main으로의 머지는 사람이 GitHub에서
gh pr merge                       # PR 머지는 사람이 버튼을 누른다
git checkout -- <상대 트랙 파일>   # 남의 작업 되돌리기
git reset --hard                  # 커밋되지 않은 남의 작업이 날아갈 수 있다
git clean -fdx                    # 위와 동일
```

### 실행 전 반드시 사람에게 확인받는 것

- 씬 파일(`.unity`) 수정 — 소유권은 6-6 표 참고
- `*.asmdef`, `ProjectSettings/EditorBuildSettings.asset` 수정 (공유 파일)
- `Scripts/Core/`, `Scripts/Data/` 수정 (두 트랙이 공유)
- 새 외부 패키지·에셋 추가
- 기존 파일을 크게 재구성하는 리팩터링
- `CLAUDE.md`의 밸런스 수치 변경

### 작업 시작 시 항상

```bash
git status                        # 남의 미커밋 작업이 있는지 먼저 확인
git branch --show-current         # 지금 어느 브랜치인지 확인
```

`main`에 있는 상태로 파일을 수정하기 시작하지 않는다. 브랜치부터 판다.

### 커밋할 때

`git add .`를 쓰지 않는다. **의도한 파일만 명시적으로 추가하고, `.meta`를 함께 넣는다.**

```bash
git add unity/Assets/_Project/Scripts/Weapons/InariClawWeapon.cs \
        unity/Assets/_Project/Scripts/Weapons/InariClawWeapon.cs.meta
```

`git add .`는 Unity가 에디터를 열기만 해도 dirty로 만든 씬 파일이나,
상대 트랙의 미커밋 작업까지 끌어들인다.

### 씬 파일이 충돌하면

`.unity` 충돌은 텍스트 병합기로 직접 풀지 않는다. GUID 참조가 조용히 끊길 수
있고, 그 손상은 diff 리뷰로도 안 잡힌다. 대신:

```bash
# rebase 중이라면 방향(ours/theirs)이 헷갈리니 브랜치 이름으로 명시한다
git checkout main -- unity/Assets/_Project/Scenes/GameScene.unity
git add unity/Assets/_Project/Scenes/GameScene.unity
```

즉 **충돌한 씬은 상대(주로 main) 버전을 그대로 채택하고, 내가 그 씬에 하려던
작업은 병합 후 에디터에서 손으로 다시 한다.** 실제로 호감도 대화 #1(PR #2)이
이 경로로 처리됐다 — `Slice1.unity`가 `GameScene.unity`로 재구성되는 동안 만든
UI 배치가 통째로 사라져서, 머지 후 새 씬에 다시 배치해야 했다.

### 하지 않는 것

- 사람 승인 없이 PR 머지
- 씬을 가로지르는 인스펙터 참조 시도 (Unity가 직렬화하지 못한다)
- `StatSystem`을 두고 별도 버프 시스템 만들기 ← **가장 흔한 실수**
- `WeaponBase.Damage`/`Range`에 스탯 배율을 **다시** 곱하기 (이미 반영됨)
- `ObjectPool`을 두고 런타임 `Instantiate`/`Destroy` 쓰기
- 씬 분리 브랜치가 머지되기 전에 다른 기능 브랜치 시작하기
- 8장의 TBD 항목을 임의로 결정하기

### 불확실하면

추측해서 구현하지 말고 **질문한다.** 이 프로젝트는 이미 84개 스크립트가 있고,
대부분의 "새로 만들어야 할 것 같은 기능"은 이미 재사용 가능한 형태로 존재한다.
먼저 6-4 재사용 표를 확인하고, 관련 기존 파일을 읽는다.

---

## 1. 프로젝트 현황

- Unity **2022.3.62f3 LTS** / **Built-in 2D** / **새 Input System**
- 스크립트 84개, EditMode 테스트 234개
- 아키텍처: ScriptableObject 데이터 기반 + 중앙 매니저 + 오브젝트 풀링
- 완료 슬라이스: slice1 → 2a → 2b → 2c → 2d → 3(보스) → juice(타격감) →
  Game/Result 씬 분리(#1) → 호감도 대화 #1(PR #2)

**이미 많이 만들어진 프로젝트다. 처음부터 짓는 게 아니라 붙이는 작업이다.**

### 남은 것
| # | 항목 | 스코프 | 확인 |
|---|---|---|---|
| 0 | **IntroScene 분리** | 구조 | Game/Result는 완료(#1). IntroScene만 남음 |
| 1 | 이나리 캐릭터 + 발톱 할퀴기 무기 | **Must** | 코드에 `Inari`/`Claw` **0건**. 씬에 `Button_Inari`는 이미 있음(잠금 상태) |
| 2 | 호감도 대화 #1 씬 재배치 | **Must** | PR #2로 코드·데이터 완료. `GameScene.unity`에 UI 오브젝트 재배치만 남음 |
| 3 | 사운드 / 오디오 | Should | `AudioSource` 사용처 **0건** |
| 4 | 부스용 무입력 자동 리셋 | Must(전시) | 미구현 |
| 5 | 호감도 대화 #2 (보스전 인터럽트) | Should | 2번 이후 |
| 6 | 밸런스 튜닝 + TBD 확정 | Must | 8장 |

---

## 2. 브랜치 전략

```
main ────●────────●────────●────────●──────►   항상 빌드 가능한 상태
          \      /          \      /
           ●────●            ●────●
      feature/inari-claw   feature/affinity-dialogue-2
```

| 항목 | 규칙 |
|---|---|
| `main` 직접 푸시 | **금지** |
| 브랜치 생성 기준 | **항상 최신 `main`에서** |
| 브랜치 수명 | 3~5일 안에 머지되는 크기 |
| 머지 방식 | GitHub PR → **Squash merge** |
| 머지 권한 | **사람만** |
| 머지 후 | 브랜치 삭제 |

### 이름
```
feature/<기능>    fix/<문제>    chore/<작업>    balance/<대상>
```
영문 소문자 + 하이픈. **사람 이름(`feature/jim`)은 쓰지 않는다.**

### 하루 작업 사이클

```bash
git checkout main && git pull origin main
git checkout -b feature/inari-claw

# 작업 → 커밋 (자주, 작게, .meta 함께)
git add <파일> <파일>.meta
git commit -m "feat: 이나리 발톱 할퀴기 무기 로직"

# 하루 1회 이상 main 끌어오기 — 충돌을 매일 조금씩
git fetch origin && git rebase origin/main

git push -u origin feature/inari-claw
git push --force-with-lease      # rebase 이후
# → PR 생성 → 상대 리뷰 → 사람이 Squash merge
```

### 알아둘 것

**브랜치는 충돌을 없애지 않는다. 미룰 뿐이다.** 같은 파일을 둘 다 고치면 각자
브랜치에선 멀쩡하다가 머지하는 순간 터진다. → 하루 한 번 이상 rebase.

**깨끗하게 머지된다고 빌드되는 건 아니다.** Git은 텍스트만 본다. 둘 다
`Core/GameManager.cs`에 메서드를 추가하면 충돌 없이 머지되지만 컴파일에서 터질 수 있다.
→ PR 전 빌드 + EditMode 테스트 확인.

**로컬 폴더는 브랜치가 하나뿐이다.** 브랜치 전환 시 Unity 에디터를 반드시 닫는다.
`unity/Library/`는 브랜치별로 나뉘지 않고 하나를 공유하므로, 증상이 이상하면
`rm -rf unity/Library` (재생성되는 캐시라 안전).

### 공용 파일을 머지하기 전/후에는 알린다

브랜치 규율(3~5일 안에 머지, 하루 한 번 이상 `main` 당겨오기)은 충돌을 작게
유지하는 것이지, 충돌 자체를 막지는 못한다. **"머지했다"는 사실이 저절로
전달되지 않으면, 상대는 자기 브랜치가 언제 뒤처졌는지 모른다.**

실제로 겪은 사례(3-2절): 호감도 대화 #1 브랜치가 며칠 열려 있는 사이 씬 분리
작업이 조용히 `main`에 병합됐고, 그 사실을 모른 채 옛 씬 위에서 계속 작업하다가
병합 시점에야 충돌을 발견했다. 미리 알렸다면 그 타이밍에 바로 `rebase`해서
충돌이 훨씬 작을 때 잡을 수 있었다.

**두 타이밍에 짧게 알린다 — 거창한 절차는 필요 없다.**

- **머지 직전** — "지금 `main`에 씬/공용 파일 건드리는 거 올린다." 특히 아래
  "공용 — 건드리기 전 상의" 목록(`Scripts/Core/`, `Scripts/Data/`, 씬 파일,
  `EditorBuildSettings.asset` 등)에 해당하면 반드시.
- **머지 직후** — "올라갔다, 작업 중이면 rebase해라."

---

## 3. 씬 분리 — Game/Result 완료, IntroScene 남음

### 3-1. 현재 상태

**완료:** `GameScene.unity`(캐릭터 선택 + 인게임), `ResultScene.unity`(결과 화면)로
분리됐다. `RunResultCarrier`(static 클래스)가 씬 전환 사이에 런 결과를 실어 나른다.
`GameManager.FinishRun()`이 `RunResultCarrier`를 채우고 `GameOverPanel`을 보여준 뒤
`ResultScene`으로 넘어간다.

**남은 것:** `IntroScene.unity`. 타이틀, 시작 버튼, 조작 설명만 있으면 된다 —
의존성이 없어 가장 쉬운 작업이다. **이름은 `GameScene`/`ResultScene`과 맞춘
`IntroScene`으로 통일한다** (기획서·구 버전 초안의 "Title.unity" 표기는 폐기).

> **캐릭터 선택은 `GameScene.unity`에 남아 있다.** `CharacterSelectButton`이
> `GameManager.Instance.StartRun()`을 직접 호출하고 `PlayerSpawner`·`LevelSystem`·
> `CameraFollow` 배선이 전부 여기 물려 있다. 떼면 배선 재작업이 이득보다 크다.

### 3-2. 씬이 재구성되며 생긴 함정 — 실제로 겪음

호감도 대화 #1(PR #2)이 옛 `Slice1.unity`에 UI를 배치한 채로 며칠 열려 있었는데,
그 사이 `Slice1.unity`가 `GameScene.unity`로 재구성되면서 그 UI 배치가 병합 시점에
통째로 충돌났다. **브랜치가 3~5일을 넘기지 말아야 하는 이유가 바로 이거다** —
씬처럼 구조가 바뀌는 파일은 오래 묵을수록 되돌릴 수 없는 충돌이 된다.

### 3-3. 빌드 설정

`ProjectSettings/EditorBuildSettings.asset`에 `GameScene`, `ResultScene`이 이미
등록돼 있다. `IntroScene.unity`를 만들면 여기에 추가하는 작은 PR을 먼저
머지해둔다 — 공유 파일이라 나중에 합치면 충돌한다.

### 3-4. `IntroScene`을 만들 때 같이 해야 하는 코드 변경

씬만 만들고 끝나지 않는다. **`ResultPanel.HandleRestart()`가 지금
`GameScene`으로 곧장 되돌아가는데, 이걸 `IntroScene`으로 바꿔야 부스 데모의
"다음 관람객 대기" 흐름(결과 → 인트로 → 다시 캐릭터 선택)이 완성된다.**

```csharp
// unity/Assets/_Project/Scripts/UI/ResultPanel.cs
public void HandleRestart()
{
    // 씬 분리 이후 Result 씬에서 Intro 씬으로 명시적 이동 —
    // 결과 화면 다음은 곧장 캐릭터 선택이 아니라 부스 대기 화면(인트로)이다.
    Time.timeScale = 1f;
    SceneManager.LoadScene("IntroScene");
}
```

**씬이 아직 없는 상태에서 이 코드를 먼저 넣지 않는다.** `LoadScene`에 존재하지
않는 씬 이름을 넣으면 컴파일은 되지만 재시작 버튼을 누르는 순간 런타임 에러가
난다 — `IntroScene.unity`를 만들고 빌드 설정에 등록하는 것과 **같은 PR/같은
커밋**으로 묶는다.

---

## 4. 충돌 방지

### 4-1. `.gitattributes` 보강 — **최우선**
씬·프리팹·에셋에 Unity의 구조적 병합 도구가 붙도록 등록한다:
```
*.unity   merge=unityyamlmerge
*.prefab  merge=unityyamlmerge
*.asset   merge=unityyamlmerge
```
각자 로컬에도 등록해야 동작한다:
```bash
git config --global merge.unityyamlmerge.name "Unity YAML Merge"
# Windows
git config --global merge.unityyamlmerge.driver \
  '"C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Data\Tools\UnityYAMLMerge.exe" merge -p "%BASE%" "%REMOTE%" "%LOCAL%" "%MERGED%"'
# macOS
git config --global merge.unityyamlmerge.driver \
  '"/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/Tools/UnityYAMLMerge" merge -p "$BASE" "$REMOTE" "$LOCAL" "$MERGED"'
```
**이 도구도 완벽하지 않다.** 자동 병합 결과를 반드시 에디터에서 열어 확인한다 —
안 그러면 위 3-2에서 겪은 것과 같은 방식으로, 텍스트는 합쳐졌는데 참조는 끊긴
씬이 커밋될 수 있다.

### 4-2. 의도치 않은 씬 변경은 커밋하지 않는다
Unity는 씬을 열기만 해도 dirty로 만든다. 커밋 전 `git status`로 확인하고,
의도한 변경이 아니면 되돌린다. (단, 남의 작업일 수 있으니 에이전트는 사람에게 확인)

### 4-3. `.meta` 파일
**항상 함께 커밋한다.** `.cs`만 커밋하고 `.cs.meta`를 빼먹는 게 가장 흔한 실수이며,
상대 프로젝트에서 컴포넌트 참조가 전부 끊긴다.

### 4-4. 한글 경로
`캐릭터/`, `환경/` 한글 폴더가 있다. macOS ↔ Windows 협업 시 유니코드 정규화 차이로
같은 파일이 둘로 보일 수 있다. macOS 쪽 필수:
```bash
git config core.precomposeunicode true
```

### 4-5. 충돌 유형별 대응
| 파일 | 대응 |
|---|---|
| `.cs` | 평범한 텍스트 충돌. 해결 후 EditMode 테스트 실행 |
| `.asset` | 값 하나 차이가 대부분. 팀 확인 후 수동 |
| `.prefab` | 자동 병합 신뢰 금지. 한쪽 통째로 채택 후 나머지 재적용 |
| `.unity` | 씬 오너 버전 채택 후 에디터에서 수동 재적용 — 0장 "씬 파일이 충돌하면" 참고 |
| `EditorBuildSettings.asset` | 3-3대로 미리 등록해두면 발생하지 않음 |
| `*.asmdef` | 6-8 참고 — 사전 상의 필수 |

---

## 5. 작업 절차 — 기존 슬라이스 방식 유지

이 저장소는 `docs/superpowers/`에 **스펙 → 계획 → 구현** 3단계를 기록한다.
새 작업도 같은 형식을 따른다. 코드부터 쓰지 않는다.

1. `docs/superpowers/specs/YYYY-MM-DD-<이름>-design.md` — 설계 근거, 대안 비교, 승인 여부
2. `docs/superpowers/plans/YYYY-MM-DD-<이름>.md` — 체크박스(`- [ ]`) 단위 작업 계획
3. 브랜치를 파고 계획대로 구현 → PR

가장 최근 예시: `docs/superpowers/plans/2026-09-01-affinity-dialogue-1.md`

### 테스트 실행
EditMode 테스트는 Unity 에디터의 Test Runner에서 돌린다.
CLI로 돌릴 경우(에디터가 닫혀 있어야 함):
```bash
Unity -batchmode -projectPath unity -runTests -testPlatform EditMode \
      -testResults TestResults.xml -logFile -
```
**`-logFile -`(stdout)을 쓴다.** 파일 경로로 주면 에셋 임포트 중 도메인 리로드
때 테스트 실행이 조용히 취소되고 결과 파일이 안 생긴다. `-quit`도 `-runTests`와
같이 쓰지 않는다(같은 이유로 결과 파일이 안 생김).

`TestResults*.xml`은 `.gitignore`에 있으므로 커밋되지 않는다.

---

## 6. 코드 구조

### 6-1. 폴더와 네임스페이스

```
unity/Assets/
  _Project/
    Scripts/                      ← asmdef: SushiSurvival.Runtime
      Core/       중앙 매니저·공용 로직 (호감도 대화 로직·컨트롤러도 여기)
      Data/       ScriptableObject 정의
      Player/     이동·방향·체력·애니메이션
      Weapons/    무기 베이스·구현·투사체
      Enemies/    적 AI·스포너·웨이브   (Boss/ 하위)
      Pickups/    XP 젬
      UI/         HUD·팝업·결과화면 (호감도 대화 패널·버튼도 여기)
      World/      무한 타일 스트리밍
    Data/         ScriptableObject 에셋. 밸런스는 여기서 만진다
    Prefabs/
    Scenes/       GameScene / ResultScene / (IntroScene 예정)
  Tests/EditMode/ ← asmdef: SushiSurvival.EditModeTests
  캐릭터/ 환경/    아트 원본 (한글 경로)
```

네임스페이스: **`SushiSurvival.<폴더명>`**

**새 기능 전용 폴더를 미리 만들지 않는다.** 호감도 대화 #1을 예로, 처음엔
`Scripts/Dialogue/` 같은 전용 폴더를 고려했지만 실제로는 로직은 `Core/`,
UI는 `UI/`에 기존 컨벤션대로 넣었다 — 그래야 6-4 재사용 표에서 계속 찾기 쉽다.

### 6-2. 핵심 패턴 — 순수 로직 / MonoBehaviour 분리

테스트 가능한 계산 로직을 MonoBehaviour에서 떼어낸다. 새 코드도 **반드시** 이 형태로.

```
FacingLogic.cs           (static, Unity 의존 최소)  ←→  FacingController.cs        (MonoBehaviour)
HealthLogic.cs                                      ←→  PlayerHealth.cs
CooldownLogic.cs                                     ←→  WeaponCooldown.cs
CameraFollowLogic.cs                                 ←→  CameraFollow.cs
WeaponVisualLogic.cs                                 ←→  WeaponVisual.cs
AffinityBuffLogic.cs (maxCap × 비율 계산)             ←→  AffinityDialogueController.cs
```

```csharp
namespace SushiSurvival.Player
{
    public static class FacingLogic
    {
        private const float MinInputSqrMagnitude = 0.0001f;

        public static bool IsMoving(Vector2 moveInput)
            => moveInput.sqrMagnitude >= MinInputSqrMagnitude;

        public static Vector2 ComputeFacing(Vector2 currentFacing, Vector2 moveInput)
        {
            if (!IsMoving(moveInput)) return currentFacing;
            return moveInput.normalized;
        }
    }
}
```

계산식·판정·상태 전이는 `XxxLogic` static 클래스로. MonoBehaviour는 입력을 모아
Logic에 넘기고 결과를 씬에 반영만 한다.
새 Logic에는 `Tests/EditMode/XxxLogicTests.cs`를 **함께** 만든다.
기존 대부분이 이 형식이며, **테스트 없는 새 Logic 클래스는 PR을 올리지 않는다.**

### 6-3. 주석
한국어로, "무엇을"이 아니라 **"왜"**를 남긴다. 기존 코드 전부 이 스타일이다.

### 6-4. 재사용해야 하는 기존 시스템 — 새로 만들지 말 것

| 하려는 일 | 반드시 쓸 것 | 위치 |
|---|---|---|
| 스탯 버프·증강·대화 선택 효과 | **`StatSystem`** | `Core/StatSystem.cs` |
| 부채꼴 범위 판정 (근접 광역) | **`FanHitTest.IsInsideFan`** | `Weapons/FanHitTest.cs` |
| 캐릭터 바라보는 방향 | **`FacingController.CurrentFacing`** | `Player/FacingController.cs` |
| 적·투사체·젬·파티클 생성 | **`ObjectPool<T>` / `GameObjectPool`** | `Core/` |
| 레벨업 3택 선택지 추가 | **`IUpgradeOption`** | `Core/IUpgradeOption.cs` |
| 히트스톱·화면흔들림·사망 파티클 | **`JuiceDirector.Instance`** | `Core/JuiceDirector.cs` |
| 색 플래시 | **`SpriteFlasher`** (유일 소유자) | `Core/SpriteFlasher.cs` |
| 런 상태·경과 시간·경험치·킬 | **`GameManager.Instance`** | `Core/GameManager.cs` |
| 씬 전환 간 결과 전달 | **`RunResultCarrier`** (static, 일회성) | `Core/RunResultCarrier.cs` |
| 무기 쿨타임 | **`CooldownLogic` / `WeaponCooldown`** | `Weapons/` |
| 경과 시간 문자열 포맷 | **`RunClock.FormatElapsed`** | `Core/RunClock.cs` |
| 대화 선택 → 스탯 버프 적용 | **`AffinityBuffLogic` / `AffinityBuffApplier`** | `Core/` |

### 6-5. StatSystem — 모든 버프가 지나가는 단일 통로

증강 10종과 호감도 대화 버프가 **같은 스탯 캡 예산을 공유**한다.
클램프는 여기 한 곳에서만 한다. **별도 버프 시스템을 만들면 캡이 깨진다.**

```csharp
public enum StatType { /* AttackDamage, AttackSpeed, AttackRange, MaxHealth, Armor, ExpGain, ... */ }
public enum ModifierType { /* 가산 / 승산 */ }

public struct StatModifier { public StatType Stat; public ModifierType Type; public float Value; }

public class StatSystem
{
    public void SetBase(StatType stat, float value);
    public void SetCap(StatType stat, float capValue);
    public void AddModifier(StatModifier modifier);
    public void RemoveModifier(StatModifier modifier);
    public void ClearModifiers();
    public float GetValue(StatType stat);      // base + modifier 조합 후 cap 클램프
}
```

**호감도 대화 선택지도 `AddModifier` 호출로 끝난다.** 다만 대화 버프는 증강
누적 상한(`LevelSystem._accumulated`)과는 **독립적으로** 친다 — 런 시작부터 작은
보너스를 받아도, 이후 레벨업 선택지가 그만큼 줄어들면 안 된다는 게 기획 의도라서다.
(`docs/superpowers/specs/2026-09-01-affinity-dialogue-1-design.md` 참고.)

### 6-6. 무기 시스템 — 새 무기 추가

`WeaponBase`가 데이터 로딩·쿨타임·스탯 곱을 전부 처리한다. 구현체는 `Attack()`만 채운다.

```csharp
public abstract class WeaponBase : MonoBehaviour
{
    [SerializeField] protected WeaponData weaponData;
    [SerializeField] protected AttackAnimator attackAnimator;
    [SerializeField] protected int currentLevel = 1;
    [SerializeField] protected float minCooldown = 0.2f;   // 무한 연사 방지 하드값
    [SerializeField] protected PlayerStats playerStats;

    protected WeaponLevelStats BaseStats { get; }   // 현재 레벨의 SO 수치
    protected float Damage { get; }   // ★ StatSystem 배율이 이미 곱해진 값
    protected float Range  { get; }   // ★ 마찬가지

    public void LevelUp();
    protected abstract void Attack();  // ← 구현체가 채우는 유일한 지점
}
```

> **주의:** `Damage`·`Range`에 이미 `StatSystem` 배율이 반영되어 있다.
> 구현체에서 다시 곱하면 **이중 적용**된다.

기존 구현 전문 (`Weapons/EggFanWeapon.cs`):
```csharp
public class EggFanWeapon : WeaponBase
{
    [SerializeField] private FacingController facing;
    [SerializeField] private LayerMask enemyLayer;

    protected override void Attack()
    {
        float range = Range;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, enemyLayer);
        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<EnemyBase>(out var enemy)) continue;

            if (FanHitTest.IsInsideFan(transform.position, facing.CurrentFacing, range,
                                       BaseStats.angleDegrees, enemy.transform.position))
                enemy.TakeDamage(Damage, transform.position);
        }
    }
}
```

**이나리 발톱 할퀴기는 이 구조를 거의 그대로 쓴다** (부채꼴 근접, 각도·범위만 좁고 빠름).
`EggFanWeapon`을 복사해 시작하는 게 가장 안전하다.

#### 새 무기 체크리스트
- [ ] `Weapons/InariClawWeapon.cs` — `WeaponBase` 상속, `Attack()`만 오버라이드
- [ ] `_Project/Data/InariWeaponData.asset` — `WeaponLevelStats[4]` (`CLAUDE.md` 표 수치)
- [ ] `_Project/Data/InariCharacterData.asset` — `playerPrefab`, `animatorController`
- [ ] `Prefabs/InariPlayer.prefab` — `EggPlayer` / `ShrimpPlayer` 구조 참고
- [ ] `GameScene.unity`의 `CharacterSelectButton`(`Button_Inari`, 이미 있음)에
      `characterData` 연결, `locked` 해제 (**사람 확인 필요**)
- [ ] 순수 판정 로직이 생기면 `XxxLogic` + EditMode 테스트
- [ ] `.meta` 파일 전부 커밋

### 6-7. 캐릭터 선택 구조
`CharacterSelectButton`은 씬에 3개가 미리 배치되어 인스펙터에서 `CharacterData`를 연결한다.
동적 생성이 아니다. `locked` 체크박스가 미구현 캐릭터를 회색 처리한다 —
**이나리는 현재 locked 상태다.**

### 6-8. ScriptableObject 스키마
```csharp
// CharacterData:  characterName, portraitSprite, playerPrefab,
//                 baseMoveSpeed(3), baseMaxHealth(100), weaponData, animatorController,
//                 affinityDialogue (AffinityDialogueData, 비우면 대화 없이 바로 시작)
// WeaponData:     weaponName, isMelee, projectilePrefab,
//                 WeaponLevelStats[4] { damage, cooldown, range, angleDegrees, pierceCount }
// AugmentData:    augmentName, icon, statType(StatType), valuePerPick, maxCap
// AffinityDialogueData: question1 { questionText, choices[] { choiceText, augment(AugmentData) } }
```
현재 에셋: `EggCharacterData`, `ShrimpCharacterData`, `EggWeaponData`, `ShrimpWeaponData`,
`BasicMobData`, `CaliforniaRollData`, `MidBossData`, `BossData`, `Aug_*` 10종,
`EggAffinityDialogue`, `ShrimpAffinityDialogue`.

### 6-9. asmdef — 공유 파일, 상의 후 수정
```
Scripts/SushiSurvival.Runtime.asmdef
  references: Unity.InputSystem, UnityEngine.UI
Tests/EditMode/SushiSurvival.EditModeTests.asmdef
  references: SushiSurvival.Runtime, UnityEngine.TestRunner, UnityEditor.TestRunner
```
`Scripts/` 아래 새 폴더는 자동 포함된다. **단, 새 패키지(TextMeshPro, Cinemachine 등)를
쓰려면 `references` 수정이 필요하고 이건 공유 파일이다.** 먼저 상의하고 작은 PR로 머지.

### 6-10. 성능
- 런타임 루프에서 `Instantiate`/`Destroy` 금지 → 풀 경유
- `Update()` 안에서 `GetComponent`, `Find`, `Camera.main` 호출 금지

---

## 7. 트랙 분담 & PR

### 트랙 A — 캐릭터 / 전투 / 연출
`Scripts/Weapons/`, `Scripts/Player/`, `Prefabs/`, **`Scenes/GameScene.unity`(단독 소유)**,
`캐릭터/`·`환경/`
→ 주 작업: 이나리 캐릭터 + 발톱 할퀴기

### 트랙 B — 시스템 / 흐름 / 오디오
**`Scenes/IntroScene.unity`(예정)·`Scenes/ResultScene.unity`(단독 소유)**,
`Scripts/UI/`, `Scripts/Core/AudioDirector.cs`(신규), `_Project/Data/*.asset`
→ 주 작업: IntroScene → 호감도 대화 #2

**호감도 대화 #1(PR #2)은 `GameScene.unity`에 UI를 배치해야 해서 트랙 A 소유
씬을 건드린다.** 이런 경우 작업 전 상대 트랙에 알리고, 가능하면 트랙 A가 그 부분을
대신 배선한다.

### 공용 — 건드리기 전 상의
`Scripts/Core/`, `Scripts/Data/`, `Tests/EditMode/`, `*.asmdef`, `EditorBuildSettings.asset`
작게 잘라 먼저 머지하고, 각자 브랜치에서 rebase로 받아간다.

### 커밋 메시지
기존 형식 유지 (`chore: 타격감 슬라이스 배선 — SpriteFlasher·JuiceDirector·DeathBurstPool`)
```
feat: 새 기능    fix: 버그    chore: 설정·정리
test: 테스트     balance: SO 수치    docs: 문서
```

### PR 템플릿
```markdown
## 무엇을 / ## 왜
## 체크
- [ ] 어떤 씬 파일을 수정했는지 (없으면 "없음")
- [ ] .meta 파일 전부 포함
- [ ] 새 Logic 클래스에 EditMode 테스트 추가
- [ ] EditMode 테스트 전체 통과
- [ ] 에디터에서 실제 플레이 확인 (씬 전환 포함)
## 확인 방법
```

리뷰어 최소 1명 승인. 승인 후에도 **머지 버튼은 사람이 누른다.**

---

## 8. 백로그

### 1단계: 구조 정리
| # | 브랜치 | 트랙 | 내용 | 상태 |
|---|---|---|---|---|
| 1 | `chore/gitattributes-yamlmerge` | 공용 | 병합 도구 설정 | 이 문서와 함께 설치 |
| 2 | `chore/build-settings-scenes` | 공용 | 씬 빌드 등록 | GameScene·ResultScene 완료, IntroScene 남음 |
| 3 | `feature/intro-scene` | B | `IntroScene` 분리 + `ResultPanel.HandleRestart()` 수정 | 남음 — 의존 없음, 다음 우선순위 |
| 4 | `feature/result-scene` | B | 결과 씬 + `RunResultCarrier` | **완료** |

### 2단계: 기능 (병렬 가능)
| # | 브랜치 | 트랙 | 내용 | 상태 |
|---|---|---|---|---|
| 5 | `feature/inari-claw` | A | 이나리 무기 로직 + 테스트 + SO | 남음 |
| 6 | `feature/inari-anim` | A | 6프레임 할퀴기, 3갈래 슬래시 이펙트 | 남음 |
| 7 | `feature/affinity-dialogue-1` | B | 호감도 대화 #1 (Must) | **PR #2 — 코드 완료, `GameScene.unity` UI 재배치 필요** |
| 8 | `feature/audio-director` | B | 오디오 기반 시스템 | 남음 |
| 9 | `feature/booth-autoreset` | B | 무입력 자동 복귀 (전시 필수) | 남음 |
| 10 | `feature/affinity-dialogue-2` | B | 보스전 인터럽트 대화 (Should) | 7번 이후 |
| 11 | `balance/playtest-pass-1` | B | 플레이테스트 후 SO 수치 조정 | 남음 |

### 부스 데모 체크리스트
- [ ] 결과 씬에서 무입력 N초 → 타이틀 자동 복귀 (관람객 회전율)
- [ ] 조작 설명이 타이틀에 표시 (WASD / 게임패드 양쪽)
- [ ] 1회차 5~7분 유지
- [ ] 사운드 없이도 이해되는 시각 피드백 (부스 소음)
- [ ] 게임패드 단독으로 끝까지 플레이 가능
- [ ] 씬 전환 시 `Time.timeScale`이 항상 1로 복귀하는지 확인
- [ ] 크래시 시 자동 재실행 스크립트
- [ ] 한글 텍스트 오타·폰트 깨짐 점검

### TBD — 사람이 정한다 (출처: `CLAUDE.md`)
- 보스전 진입 시 기존 잡몹 스폰 유지 여부 (밸런스 영향 큼)
- 잡몹 A 5종 색상 베리에이션의 스탯 차등 여부
- 기본 흰색 밥알의 정확한 XP 값
- 각 증강의 레벨당 증가폭, 등장 가중치, 부활 중복 처리
- 계란 = 아델린 매핑 확정 여부
- 보스전 인터럽트 시 타이머 정지 여부 (대화 #2 설계 시 결정)
- 간장새우·이나리 대화용 표정 초상화 필요 여부 (대화 #2 설계 시 결정)

---

_최종 수정: 2026-09-02_
