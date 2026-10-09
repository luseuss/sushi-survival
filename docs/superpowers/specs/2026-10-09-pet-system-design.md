# 펫 9종 — 요정 시스템의 아트·종류 확장 설계

> 이전 스펙: `docs/superpowers/specs/2026-10-06-fairy-system-design.md`(요정 시스템, 구현·병합 완료).
> 이번 작업은 노란 원 플레이스홀더였던 요정을 **사용자가 준 펫 아트 9종**으로 바꾸고,
> 펫마다 성능이 다르게 한다. "요정"과 "펫"은 같은 대상이다(코드 이름은 `Fairy*`를 유지한다).

## 확정된 결정

| 항목 | 결정 |
|---|---|
| 아트 | `C:\Users\wnsdn\Desktop\펫`의 시트 9장. 각 **125×25**, **25×25 프레임 5장**, 둥실 떠 있는 대기 애니메이션 |
| 종류 | 간장새우·계란초밥·광어초밥·문어초밥·새우초밥·연어초밥·유부초밥·장어초밥·참치초밥 |
| 최대 마릿수 | 3마리(그대로). 같은 종류를 두 마리 가질 수 없다 |
| 소환 | 와사비 성공 후 빈 자리가 있으면 **소환 카드 3장** = 아직 안 가진 펫 중 **랜덤 3종**(남은 종류가 3개 미만이면 그만큼) |
| 강화 | 3마리가 다 차면 **강화 카드만**(별도 경로, 지금까지의 강화 동작 그대로). 소환 카드와 강화 카드는 섞지 않는다 |
| 성능 | **펫마다 다르다**(피해·쿨타임·사거리·관통). 레벨 1~4 수치는 펫 데이터에 있다 |
| 수치 정하기 | 내가 역할 초안을 짜고(아래 표) 사용자가 인스펙터에서 조정한다 |
| 투사체 | 모든 펫이 기존 `FairyShot`을 쏜다(펫별 투사체 구분은 이번 범위 밖) |
| 아트 가져오기 | 사용자가 시트를 임포트·슬라이스하고, `FairyData` 에셋 9개는 슬라이스된 스프라이트 ID를 읽어 YAML로 생성한다 |

## 알게 된 결과(감수한 점)

- 소환 카드는 빈 자리가 있을 때만 나오고 강화는 3마리가 찬 뒤에 나온다. 와사비 성공이 40%라서 강화를 보려면
  성공이 세 번 필요하다. 5분 판에서는 소환만 하고 끝나는 판이 많을 수 있다 — 플레이테스트로 보고
  소환 카드 중 한 장을 강화로 대체하는 안을 나중에 열어 둔다(`FairyChoiceLogic`만 바꾸면 된다).
- 펫 프레임은 25px = 월드 0.25유닛이라 플레이어(약 0.5)의 절반 크기다. `FairyData.visualScale`로 키울 수 있게 둔다.
- 펫 그림이 바라보는 방향은 시트마다 같다고 가정하고, 한 값(`FairyData.facesRight`, 기본 false)으로 뒤집기 기준을 정한다.

## 1. 구성

| 파일 | 역할 | 구분 |
|---|---|---|
| `Data/FairyData.cs` | 펫 한 종: 표시 이름, 역할 한 줄, 프레임 `Sprite[]`, FPS, 레벨 4단계 수치, `visualScale`, `facesRight` | 신규(SO) |
| `Companions/FrameAnimLogic.cs` | 시간 → 프레임 번호(반복) | 신규(순수) |
| `Companions/FairyFacingLogic.cs` | 이동 방향 → 좌우 뒤집기 여부(작은 움직임은 이전 방향 유지) | 신규(순수) |
| `Companions/FairyChoiceLogic.cs` | 빈 자리면 안 가진 펫 중 랜덤 3종 소환, 가득이면 강화 | 수정(시그니처 변경) |
| `Companions/Fairy.cs` | `FairyData`를 받아 프레임 재생·뒤집기·수치 읽기 | 수정 |
| `Companions/FairyController.cs` | `FairyData[] catalog`, 요정마다 (종류 번호, 레벨) | 수정 |
| `Core/FairyOption.cs` | 카드에 펫 이름·아이콘·역할·수치 | 수정 |
| `Core/LevelSystem.cs`, `Core/GameManager.cs`, `Core/BossFightDirector.cs`, `UI/RunResultCarrier.cs` | 종류 배열도 이월 | 수정 |
| `Assets/Art/펫/*.png` (+`.meta`) | 시트 9장 | 신규(사용자 슬라이스) |
| `Assets/_Project/Data/Fairies/*.asset` | 펫 9종 데이터 | 신규(내가 YAML 생성) |
| 씬(사용자 에디터) | `FairyController`에 카탈로그 9칸 연결 | 사용자 |

기존 `FairyWeaponData.asset`은 `FairyData` 카탈로그로 대체된다. 씬의 옛 `fairyData` 필드는 고아 값으로 남지만 무해하다.
카탈로그가 비어 있으면(씬 배선 전) `FairyChoiceLogic`이 빈 목록을 돌려주고 `LevelSystem`이 기존 **스탯 버프 대체 보상**으로
넘어간다 — 코드 PR이 씬 배선보다 먼저 머지돼도 안전하다.

## 2. 데이터 — `FairyData`

```csharp
[CreateAssetMenu(menuName = "SushiSurvival/Fairy Data", fileName = "NewFairyData")]
public class FairyData : ScriptableObject
{
    public string displayName;                 // "참치초밥"
    [TextArea] public string roleLine;         // 카드 설명 첫 줄: "고화력 느림"
    public Sprite[] frames;                    // 5장
    public float framesPerSecond = 8f;
    public bool facesRight;                    // 그림이 오른쪽을 보면 true
    public float visualScale = 1f;
    public WeaponLevelStats[] levels = new WeaponLevelStats[4];   // damage/cooldown/range/pierceCount만 쓴다
}
```

## 3. 펫 역할 초안(Lv1 기준, 사용자가 조정)

모든 펫의 DPS가 대략 4~6 근처다. 레벨 성장은 펫 공통 규칙으로 에셋에 미리 계산해 넣는다:
**피해 ×(1, 1.25, 1.5, 2.0), 쿨타임 ×(1, 0.92, 0.85, 0.75), 관통 +1(Lv3부터), 사거리 불변.**

| 펫 | 역할 | 피해 | 쿨타임 | 사거리 | 관통 |
|---|---|---|---|---|---|
| 계란초밥 | 균형형(아델린) | 5 | 1.0 | 6 | 0 |
| 간장새우 | 관통 저격(카마리온) | 6 | 1.3 | 7 | 1 |
| 유부초밥 | 빠른 연사(이나리) | 3 | 0.6 | 5 | 0 |
| 참치초밥 | 고화력 느림 | 10 | 1.8 | 6 | 0 |
| 연어초밥 | 중간 연사 | 4 | 0.8 | 6 | 0 |
| 광어초밥 | 장거리 | 5 | 1.2 | 9 | 0 |
| 문어초밥 | 다관통 | 4 | 1.4 | 6 | 2 |
| 새우초밥 | 단거리 연사 | 4 | 0.7 | 4 | 0 |
| 장어초밥 | 중관통 | 5 | 1.1 | 6 | 1 |

## 4. 순수 로직

```csharp
// FrameAnimLogic
public static int FrameIndex(float time, float framesPerSecond, int frameCount)
// time*fps를 내림해 frameCount로 나눈 나머지. frameCount <= 0 이면 0, fps <= 0 이면 0, 음수 시간은 0으로.

// FairyFacingLogic
public static bool FlipX(float horizontalVelocity, bool currentlyFlipped, bool spriteFacesRight, float deadZone = 0.02f)
// |속도| < deadZone 이면 currentlyFlipped 유지. 오른쪽으로 가는데 그림이 왼쪽을 보면(또는 그 반대) true.

// FairyChoice (확장)
public struct FairyChoice { public FairyChoiceKind Kind; public int Index; public int KindIndex; }
// Summon: Index = -1, KindIndex = 카탈로그 번호. Upgrade: Index = 요정 번호, KindIndex = -1.

// FairyChoiceLogic.Build(IReadOnlyList<int> ownedKinds, IReadOnlyList<int> levels,
//                        int catalogSize, int maxCount, int maxLevel, System.Random random)
// count < maxCount: 안 가진 종류를 무작위로 섞어 앞에서 MaxChoices(3)개까지 Summon. 안 가진 종류가 없으면 빈 목록.
// count >= maxCount: 최대 레벨 미만인 요정마다 Upgrade(최대 3개). 없으면 빈 목록.
```

## 5. 동작

- **`FairyController.Summon(int kindIndex)`**: 가득 찼거나 이미 가진 종류거나 번호가 범위 밖이면 false. 성공하면 `Fairy.Initialize(…, FairyData data, …)`.
- **`Fairy`**: 수치는 `data.levels[level-1]`(기존 `WeaponData` 대신). 스프라이트는 `FrameAnimLogic.FrameIndex(time, fps, frames.Length)`로 갱신하고
  `FairyFacingLogic.FlipX`로 뒤집으며 `visualScale`을 적용한다. `frames`가 비면 노란 원 플레이스홀더로 대체한다(아트 연결 전 안전).
- **`FairyController.Restore(IReadOnlyList<int> kinds, IReadOnlyList<int> levels)`**: 종류와 레벨 배열이 같은 길이일 때만(아니면 무시) 같은 펫을 같은 레벨로 되살린다.
- **카드**: 소환은 제목 `"{펫 이름} 소환"`, 설명 `"{역할}\n피해 {d} · 쿨타임 {c}초 · 사거리 {r}"`, 아이콘 = 펫 첫 프레임.
  강화는 제목 `"{펫 이름} 강화 Lv{n}"`, 설명은 기존 수치 비교.
- **이월**: `RunResultCarrier.FairyKinds`(`int[]`)를 `FairyLevels`와 같은 길이로 쌓는다. `LevelSystem.FairyKinds`·`RestoreFairies(kinds, levels)`가 위임한다.

## 6. 테스트

- `FrameAnimLogicTests`: 0초=0프레임, fps에 따른 프레임 진행, 반복(끝에서 처음으로), 음수/0 입력 안전
- `FairyFacingLogicTests`: 오른쪽/왼쪽 이동 × 그림 방향 4조합, 데드존 안에서는 이전 값 유지
- `FairyChoiceLogicTests`(개정): 안 가진 종류만 소환 후보, 최대 3종, 안 가진 종류가 2개면 2장, 가득 차면 강화만, 만렙 제외, 전부 만렙이면 빈 목록,
  같은 시드면 같은 결과(결정론), 카탈로그가 비면 빈 목록
- 데이터 에셋 테스트는 구조만: 9종 모두 프레임 5장·레벨 4단계·이름이 비지 않음(수치는 assert하지 않음 — 밸런스는 자주 바뀐다)
- MonoBehaviour(`Fairy`, `FairyController`, `FairyOption`, `LevelSystem` 연결)는 컴파일 + 회귀 + 플레이 확인

## 7. PR 분할(의존 순서)

| PR | 내용 | 의존 |
|---|---|---|
| 0 | 이 스펙 문서 | — |
| A | `FairyData` + `FrameAnimLogic` + `FairyFacingLogic` + 테스트(전부 추가만) | 없음 |
| B | 아트 임포트: 시트 9장을 `Assets/Art/펫/`에 복사(사용자가 슬라이스 25×25 후 `.meta` 포함 커밋) | 없음 |
| C | **원자적 전환**: `FairyChoiceLogic` 개정 + `Fairy`/`FairyController`/`FairyOption`/`LevelSystem`/`GameManager`/`BossFightDirector`/`RunResultCarrier` + 테스트 | A |
| D | `FairyData` 에셋 9개 생성(슬라이스 ID 읽어 YAML) + 데이터 구조 테스트 | A, B |
| E | 씬 배선(사용자): 두 씬 `FairyController`의 Catalog에 9종 연결 | C, D |

`FairyChoiceLogic` 시그니처를 바꾸면 호출하는 `FairyController`·`FairyOption`·`LevelSystem`이 같이 바뀌어야 컴파일되므로 C는 **한 PR로 묶는다**
(삭제·시그니처 변경은 소비자 수정과 원자적으로). 기존 코드를 지우기 전에 `FairyChoiceLogic`/`BuildChoices`를 부르는 곳을 grep으로 전부 확인한다.
A·B는 파일이 겹치지 않아 병렬이고, C는 A 뒤, D는 A·B 뒤, E는 C·D 뒤다.

## 8. 범위 밖 / 미결

- 펫별 투사체 모양·색 구분(지금은 전부 `FairyShot`)
- 펫 종류별 특수 능력(독·빙결 등) — 이번엔 수치 차이만
- 소환 카드 중 한 장을 강화로 바꾸는 안(플레이테스트로 판단)
- 펫 소환·공격 사운드(오디오 에셋 없음)
