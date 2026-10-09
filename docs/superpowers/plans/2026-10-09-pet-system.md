# 펫 9종 시스템 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 노란 원 플레이스홀더였던 요정을 펫 아트 9종으로 바꾸고, 소환 카드는 "안 가진 펫 중 랜덤 3종", 펫마다 다른 성능으로 만든다.

**Architecture:** 펫 한 종 = `FairyData` ScriptableObject 하나(프레임 5장·FPS·레벨 1~4 수치). `FairyController`가 `FairyData[] catalog`와 요정별 (종류 번호, 레벨)을 관리하고, 선택지·프레임 재생·좌우 뒤집기·카드 문구는 순수 정적 로직으로 분리해 EditMode 테스트한다. 아트 시트의 `.meta`와 `FairyData` 에셋 9개는 스크립트로 생성해 에디터 수작업(슬라이스·에셋 채우기)을 없앤다.

**Tech Stack:** Unity 2022.3.62f3, C#, Legacy uGUI, NUnit EditMode, Node(v24, 에셋·메타 생성 스크립트).

**Spec:** `docs/superpowers/specs/2026-10-09-pet-system-design.md` (실행자는 스펙도 같이 읽는다)

## Global Constraints

- `main`에 직접 커밋·푸시·머지 금지. PR마다 **최신 `main`에서** 새 브랜치를 판다. 머지 버튼은 사람만 누른다.
- 씬 편집은 사용자가 에디터에서 한다. 코드 PR(A·C)과 생성 PR(B·D)은 씬을 건드리지 않는다.
- 버프·스탯은 `Core/StatSystem`(`PlayerStats.GetValue`)을 재사용한다. 별도 버프 시스템 금지.
- 수치는 전부 에셋/인스펙터로 노출한다(코드에 펫별 수치 하드코딩 금지 — 단, D의 생성 스크립트의 역할 표는 에셋을 만드는 도구라 예외).
- UI는 Legacy `UnityEngine.UI`만. 새 코드 네임스페이스: 펫 로직 `SushiSurvival.Companions`, 데이터 `SushiSurvival.Data`. 테스트는 `Assets/Tests/EditMode/`, 네임스페이스 `SushiSurvival.EditModeTests`.
- 새 `.cs`·폴더의 `.meta`(폴더 바깥에 생기는 `<폴더>.meta` 포함)를 함께 커밋한다. 커밋 전 `git status`로 예상 밖 파일, `git diff`로 임시값 확인.
- 삭제·시그니처 변경은 소비자 수정과 **같은 PR**에서(원자적). 시그니처를 바꾸기 전에 그 이름을 부르는 곳을 grep으로 전부 확인한다.
- 배치 테스트 명령(에디터를 **닫은 상태**에서만 동작 — 열려 있으면 `HandleProjectAlreadyOpenInAnotherInstance`로 결과 파일이 안 생긴다):
  ```bash
  cd unity && "C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" -batchmode -projectPath "$(pwd -W)" -runTests -testPlatform EditMode -testResults "$(pwd -W)/TestResults.xml" -logFile - > "$TEMP/unity_test.log" 2>&1
  grep -c "error CS" "$TEMP/unity_test.log"
  grep -o '<test-run[^>]*' TestResults.xml | grep -o 'total="[0-9]*"\|passed="[0-9]*"\|failed="[0-9]*"'
  rm -f TestResults.xml
  ```
  `-runTests`에 `-quit`을 같이 쓰지 말 것. 기준: 현재 **544개 중 543 통과**, 알려진 기존 실패는 `Portraits_AdelineAndKamarionHaveOne_InariHasNone` 1건뿐.
- 긴 셸 명령에 heredoc으로 파일 여러 개를 쓰면 파싱 오류가 날 수 있다 — 파일은 Write 도구로 하나씩 만든다.
- 커밋 메시지 끝에 `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`, PR 본문 끝에 `🤖 Generated with [Claude Code](https://claude.com/claude-code)`.

## PR 분할과 순서

| PR | 브랜치 | 내용 | 의존 |
|---|---|---|---|
| A | `feature/pet-data-logic` | `FairyData` + `FrameAnimLogic` + `FairyFacingLogic` + `FairyDescriptionLogic` + 테스트(추가만) | 없음 |
| B | `feature/pet-art-import` | 시트 9장 복사 + 25×25 슬라이스 `.meta` 생성(스크립트) | 없음 |
| C | `feature/pet-catalog-switch` | 원자적 전환: 선택지 개정 + `Fairy`/`FairyController`/`FairyOption`/`LevelSystem`/이월 | A |
| D | `feature/pet-data-assets` | `FairyData` 에셋 9개 생성 + 구조 테스트 | A, B |
| E | (사용자 에디터) | 두 씬 `FairyController`에 카탈로그 9칸 연결 | C, D |

A와 B는 파일이 겹치지 않아 **병렬**로 진행할 수 있다. C는 A가 머지된 뒤, D는 A·B가 머지된 뒤 시작한다.
C가 머지됐는데 E 전이면 카탈로그가 비어 `BuildChoices()`가 빈 목록을 돌려주고, `LevelSystem`이 기존 **스탯 버프 대체 보상**으로 넘어가므로 게임은 깨지지 않는다(씬의 옛 `fairyData` 필드는 고아 값으로 남아 무해).
C 시작 전 `git log --oneline origin/main -- unity/Assets/_Project/Scripts/Core/LevelSystem.cs unity/Assets/_Project/Scripts/Core/GameManager.cs unity/Assets/_Project/Scripts/Core/BossFightDirector.cs`로 협업자 최근 변경을 확인하고 아래 앵커가 그대로인지 grep한다.

---

## PR A — 데이터와 순수 로직 (추가만)

### Task 1: `FairyData`

**Files:**
- Create: `unity/Assets/_Project/Scripts/Data/FairyData.cs`

**Interfaces:**
- Produces: `SushiSurvival.Data.FairyData : ScriptableObject` — 필드 `string displayName`, `string roleLine`, `Sprite[] frames`, `float framesPerSecond`, `bool facesRight`, `float visualScale`, `WeaponLevelStats[] levels`(`WeaponLevelStats`는 `Data/WeaponData.cs`의 기존 구조체: `damage`, `cooldown`, `range`, `angleDegrees`, `pierceCount`).

- [ ] **Step 1: 구현**

```csharp
using UnityEngine;

namespace SushiSurvival.Data
{
    /// <summary>
    /// 펫(요정) 한 종. 그림(프레임·재생 속도·바라보는 방향·크기)과 레벨 1~4 공격 수치를 한 에셋에 담는다.
    /// damage/cooldown/range(사거리)/pierceCount만 쓰고 angleDegrees는 쓰지 않는다.
    /// </summary>
    [CreateAssetMenu(menuName = "SushiSurvival/Fairy Data", fileName = "NewFairyData")]
    public class FairyData : ScriptableObject
    {
        public string displayName;
        [Tooltip("소환 카드 설명 첫 줄(펫의 역할). 예: 고화력 느림")]
        [TextArea] public string roleLine;
        [Tooltip("대기 애니메이션 프레임. 시트를 슬라이스한 순서대로.")]
        public Sprite[] frames;
        public float framesPerSecond = 8f;
        [Tooltip("그림이 오른쪽을 보고 있으면 체크. 안 하면 왼쪽을 본다고 보고, 오른쪽으로 갈 때 뒤집는다.")]
        public bool facesRight;
        [Tooltip("그림이 작을 때 키우는 배율(25px 프레임은 월드 0.25유닛).")]
        public float visualScale = 1f;
        [Tooltip("인덱스 0 = Lv1 ... 인덱스 3 = Lv4(MAX)")]
        public WeaponLevelStats[] levels = new WeaponLevelStats[4];
    }
}
```

- [ ] **Step 2: 컴파일 확인** — 에디터를 열면 `.meta`가 생긴다(또는 다음 Task의 배치 테스트가 만든다).

### Task 2: `FrameAnimLogic`

**Files:**
- Create: `unity/Assets/_Project/Scripts/Companions/FrameAnimLogic.cs`
- Create: `unity/Assets/Tests/EditMode/FrameAnimLogicTests.cs`

**Interfaces:**
- Produces: `public static int FrameAnimLogic.FrameIndex(float time, float framesPerSecond, int frameCount)` — `floor(time × fps)`를 `frameCount`로 나눈 나머지. `frameCount <= 0`, `fps <= 0`, `time <= 0`이면 0.

- [ ] **Step 1: 실패하는 테스트 작성**

```csharp
using NUnit.Framework;
using SushiSurvival.Companions;

namespace SushiSurvival.EditModeTests
{
    public class FrameAnimLogicTests
    {
        [Test]
        public void TimeZero_IsFirstFrame()
        {
            Assert.AreEqual(0, FrameAnimLogic.FrameIndex(0f, 8f, 5));
        }

        [TestCase(0.00f, 0)]
        [TestCase(0.12f, 0)]
        [TestCase(0.13f, 1)]
        [TestCase(0.26f, 2)]
        [TestCase(0.51f, 4)]
        public void AdvancesByFramesPerSecond(float time, int expected)
        {
            // 8fps → 프레임 하나가 0.125초
            Assert.AreEqual(expected, FrameAnimLogic.FrameIndex(time, 8f, 5));
        }

        [Test]
        public void Loops_BackToFirstFrame()
        {
            // 8fps, 5프레임 → 0.625초에 한 바퀴
            Assert.AreEqual(0, FrameAnimLogic.FrameIndex(0.63f, 8f, 5));
            Assert.AreEqual(1, FrameAnimLogic.FrameIndex(0.76f, 8f, 5));
        }

        [Test]
        public void InvalidInputs_AreSafe()
        {
            Assert.AreEqual(0, FrameAnimLogic.FrameIndex(1f, 8f, 0));
            Assert.AreEqual(0, FrameAnimLogic.FrameIndex(1f, 0f, 5));
            Assert.AreEqual(0, FrameAnimLogic.FrameIndex(-3f, 8f, 5));
            Assert.AreEqual(0, FrameAnimLogic.FrameIndex(1f, -8f, 5));
        }

        [Test]
        public void SingleFrame_AlwaysZero()
        {
            Assert.AreEqual(0, FrameAnimLogic.FrameIndex(12.3f, 8f, 1));
        }
    }
}
```

- [ ] **Step 2: 실행해 실패 확인** — 컴파일 에러(`FrameAnimLogic` 없음).

- [ ] **Step 3: 구현**

```csharp
using UnityEngine;

namespace SushiSurvival.Companions
{
    public static class FrameAnimLogic
    {
        /// <summary>시간(초)에 맞는 프레임 번호. 끝에 닿으면 처음으로 돌아가 반복한다.</summary>
        public static int FrameIndex(float time, float framesPerSecond, int frameCount)
        {
            if (frameCount <= 0 || framesPerSecond <= 0f || time <= 0f) return 0;

            return Mathf.FloorToInt(time * framesPerSecond) % frameCount;
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

- [ ] **Step 5: 커밋** (브랜치 `feature/pet-data-logic`, 최신 main에서 생성)

```bash
git add unity/Assets/_Project/Scripts/Data/FairyData.cs unity/Assets/_Project/Scripts/Data/FairyData.cs.meta unity/Assets/_Project/Scripts/Companions/FrameAnimLogic.cs unity/Assets/_Project/Scripts/Companions/FrameAnimLogic.cs.meta unity/Assets/Tests/EditMode/FrameAnimLogicTests.cs unity/Assets/Tests/EditMode/FrameAnimLogicTests.cs.meta
git commit -m "feat: 펫 데이터(FairyData)와 프레임 애니메이션 로직"
```

### Task 3: `FairyFacingLogic`

**Files:**
- Create: `unity/Assets/_Project/Scripts/Companions/FairyFacingLogic.cs`
- Create: `unity/Assets/Tests/EditMode/FairyFacingLogicTests.cs`

**Interfaces:**
- Produces: `public static bool FairyFacingLogic.FlipX(float horizontalVelocity, bool currentlyFlipped, bool spriteFacesRight, float deadZone = 0.02f)` — 속도 크기가 `deadZone` 미만이면 `currentlyFlipped` 유지. 오른쪽으로 가는데 그림이 왼쪽을 보면(또는 왼쪽으로 가는데 오른쪽을 보면) true.

- [ ] **Step 1: 실패하는 테스트 작성**

```csharp
using NUnit.Framework;
using SushiSurvival.Companions;

namespace SushiSurvival.EditModeTests
{
    public class FairyFacingLogicTests
    {
        [Test]
        public void ArtFacesLeft_MovingRight_Flips()
        {
            Assert.IsTrue(FairyFacingLogic.FlipX(1f, false, false));
        }

        [Test]
        public void ArtFacesLeft_MovingLeft_DoesNotFlip()
        {
            Assert.IsFalse(FairyFacingLogic.FlipX(-1f, true, false));
        }

        [Test]
        public void ArtFacesRight_MovingRight_DoesNotFlip()
        {
            Assert.IsFalse(FairyFacingLogic.FlipX(1f, true, true));
        }

        [Test]
        public void ArtFacesRight_MovingLeft_Flips()
        {
            Assert.IsTrue(FairyFacingLogic.FlipX(-1f, false, true));
        }

        [Test]
        public void InsideDeadZone_KeepsPreviousFlip()
        {
            Assert.IsTrue(FairyFacingLogic.FlipX(0.01f, true, false));
            Assert.IsFalse(FairyFacingLogic.FlipX(-0.01f, false, false));
            Assert.IsTrue(FairyFacingLogic.FlipX(0f, true, true));
        }

        [Test]
        public void CustomDeadZone_IsRespected()
        {
            Assert.IsFalse(FairyFacingLogic.FlipX(0.5f, false, false, 1f));
            Assert.IsTrue(FairyFacingLogic.FlipX(1.5f, false, false, 1f));
        }
    }
}
```

- [ ] **Step 2: 실행해 실패 확인** — 컴파일 에러.

- [ ] **Step 3: 구현**

```csharp
using UnityEngine;

namespace SushiSurvival.Companions
{
    public static class FairyFacingLogic
    {
        /// <summary>
        /// 이동 방향에 맞춰 그림을 뒤집을지. 거의 안 움직이면(데드존 안) 이전 방향을 그대로 둬서
        /// 떨림으로 좌우가 깜빡이지 않게 한다.
        /// </summary>
        public static bool FlipX(float horizontalVelocity, bool currentlyFlipped, bool spriteFacesRight,
                                 float deadZone = 0.02f)
        {
            if (Mathf.Abs(horizontalVelocity) < deadZone) return currentlyFlipped;

            bool movingRight = horizontalVelocity > 0f;
            return movingRight != spriteFacesRight;
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

- [ ] **Step 5: 커밋**

```bash
git add unity/Assets/_Project/Scripts/Companions/FairyFacingLogic.cs unity/Assets/_Project/Scripts/Companions/FairyFacingLogic.cs.meta unity/Assets/Tests/EditMode/FairyFacingLogicTests.cs unity/Assets/Tests/EditMode/FairyFacingLogicTests.cs.meta
git commit -m "feat: 펫 이동 방향에 따른 좌우 뒤집기 로직"
```

### Task 4: `FairyDescriptionLogic` — 소환 카드 문구

**Files:**
- Create: `unity/Assets/_Project/Scripts/Companions/FairyDescriptionLogic.cs`
- Create: `unity/Assets/Tests/EditMode/FairyDescriptionLogicTests.cs`

**Interfaces:**
- Consumes: `SushiSurvival.Data.WeaponLevelStats`
- Produces: `public static string FairyDescriptionLogic.DescribeSummon(string roleLine, WeaponLevelStats level1)` — `"{역할}\n피해 {d} · 쿨타임 {c}초 · 사거리 {r}"`(역할이 비면 둘째 줄만). 숫자는 소수 둘째 자리까지 반올림하고 불필요한 0을 뺀다(`5` → "5", `1.2` → "1.2").

- [ ] **Step 1: 실패하는 테스트 작성**

```csharp
using NUnit.Framework;
using SushiSurvival.Companions;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    public class FairyDescriptionLogicTests
    {
        private static WeaponLevelStats Stats(float damage, float cooldown, float range)
            => new WeaponLevelStats { damage = damage, cooldown = cooldown, range = range };

        [Test]
        public void IncludesRoleThenStats()
        {
            string text = FairyDescriptionLogic.DescribeSummon("고화력 느림", Stats(10f, 1.8f, 6f));

            Assert.AreEqual("고화력 느림\n피해 10 · 쿨타임 1.8초 · 사거리 6", text);
        }

        [Test]
        public void EmptyRole_ShowsOnlyStatsLine()
        {
            string text = FairyDescriptionLogic.DescribeSummon("", Stats(5f, 1f, 6f));

            Assert.AreEqual("피해 5 · 쿨타임 1초 · 사거리 6", text);
        }

        [Test]
        public void NullRole_IsTreatedAsEmpty()
        {
            string text = FairyDescriptionLogic.DescribeSummon(null, Stats(5f, 1f, 6f));

            Assert.AreEqual("피해 5 · 쿨타임 1초 · 사거리 6", text);
        }

        [Test]
        public void RoundsToTwoDecimals()
        {
            string text = FairyDescriptionLogic.DescribeSummon("x", Stats(4.126f, 0.6f, 5f));

            StringAssert.Contains("피해 4.13", text);
            StringAssert.Contains("쿨타임 0.6초", text);
        }
    }
}
```

- [ ] **Step 2: 실행해 실패 확인** — 컴파일 에러.

- [ ] **Step 3: 구현**

```csharp
using System.Globalization;
using SushiSurvival.Data;

namespace SushiSurvival.Companions
{
    public static class FairyDescriptionLogic
    {
        public static string DescribeSummon(string roleLine, WeaponLevelStats level1)
        {
            string stats = $"피해 {Number(level1.damage)} · 쿨타임 {Number(level1.cooldown)}초 · 사거리 {Number(level1.range)}";

            return string.IsNullOrEmpty(roleLine) ? stats : roleLine + "\n" + stats;
        }

        // 정수면 "10", 소수면 "1.8"처럼 불필요한 0 없이 보여준다.
        private static string Number(float value)
            => System.Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);
    }
}
```

- [ ] **Step 4: 배치 테스트** — 컴파일 에러 0, 전체 통과(기존 실패 1건 제외), 신규 19개.

- [ ] **Step 5: 커밋·PR**

```bash
git add unity/Assets/_Project/Scripts/Companions/FairyDescriptionLogic.cs unity/Assets/_Project/Scripts/Companions/FairyDescriptionLogic.cs.meta unity/Assets/Tests/EditMode/FairyDescriptionLogicTests.cs unity/Assets/Tests/EditMode/FairyDescriptionLogicTests.cs.meta
git commit -m "feat: 펫 소환 카드 설명 문구 로직"
git push -u origin feature/pet-data-logic
gh pr create --base main --title "feat: 펫 데이터·순수 로직(프레임·뒤집기·카드 문구)" --body "<요약·테스트 결과>"
```

PR 본문: 추가만 하고 호출하는 곳이 없다는 점, 폴더 `.meta` 확인.

---

## PR B — 아트 임포트

### Task 5: 시트 9장 복사와 25×25 슬라이스 `.meta` 생성

> 사용자가 에디터에서 시트 9장을 하나씩 슬라이스하는 대신, 기존 슬라이스된 시트의 `.meta`(`unity/Assets/Art/환경/환경/유적바닥세로(깨짐).png.meta`)를 틀로 삼아 스크립트가 `.meta`를 만든다.
> 스크립트는 저장소에 커밋하지 않는다(작업용, 임시 디렉터리에 둔다).

**Files:**
- Create: `unity/Assets/Art/펫/*.png` 9장 + 각 `.png.meta`, `unity/Assets/Art/펫.meta`
- Create(임시, 커밋 안 함): `$TEMP/gen-pet-meta.js`, `unity/Assets/Editor/PetImportCheck.cs`(+`.meta`) — 검증 후 삭제

**Interfaces:**
- Produces: 펫 시트마다 스프라이트 5개(`<PNG 이름 확장자 없이>_0` ~ `_4`, 각 25×25, 피벗 중앙, PPU 100, Point 필터). 각 `.meta`의 `nameFileIdTable`이 D에서 `FairyData.frames`를 채울 ID(`fileID`)를 준다. `.png.meta`의 `guid`가 D의 `guid`다.

- [ ] **Step 1: 시트 복사** — 원본은 `C:\Users\wnsdn\Desktop\펫\`(한글 파일명 NFC/NFD 문제가 있을 수 있어 Node로 목록을 읽어 복사한다).

`$TEMP/copy-pets.js`:

```javascript
const fs = require('fs');
const path = require('path');

const src = 'C:\\Users\\wnsdn\\Desktop\\펫';
const dst = path.join(process.argv[2], 'unity', 'Assets', 'Art', '펫');

fs.mkdirSync(dst, { recursive: true });
const files = fs.readdirSync(src).filter(f => f.toLowerCase().endsWith('.png'));
for (const f of files) {
  fs.copyFileSync(path.join(src, f), path.join(dst, f.normalize('NFC')));
  console.log('copied', f.normalize('NFC'));
}
console.log(files.length + ' files');
```

실행: `node "$TEMP/copy-pets.js" "$(pwd -W)"` (저장소 루트에서). 기대: `9 files`.

- [ ] **Step 2: `.meta` 생성 스크립트** — `$TEMP/gen-pet-meta.js`

```javascript
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const root = process.argv[2];
const dir = path.join(root, 'unity', 'Assets', 'Art', '펫');
const templatePath = path.join(root, 'unity', 'Assets', 'Art', '환경', '환경', '유적바닥세로(깨짐).png.meta');

const FRAME = 25;
const FRAMES = 5;

const md5 = s => crypto.createHash('md5').update(s).digest('hex');
const escapeUnicode = s => [...s].map(c => (c.charCodeAt(0) > 127
  ? '\\u' + c.charCodeAt(0).toString(16).toUpperCase().padStart(4, '0') : c)).join('');
const signedInt32 = hex => (parseInt(hex.slice(0, 8), 16) | 0);

const template = fs.readFileSync(templatePath, 'utf8').replace(/\r\n/g, '\n');
const head = template.slice(0, template.indexOf('  spriteSheet:\n'));
const tail = template.slice(template.indexOf('  mipmapLimitGroupName:'));

if (!head || !tail) throw new Error('템플릿 .meta 구조를 찾지 못했습니다.');

for (const file of fs.readdirSync(dir).filter(f => f.endsWith('.png'))) {
  const baseName = path.basename(file, '.png');
  const guid = md5('pet:' + baseName);

  let meta = head
    .replace(/^guid: .*$/m, 'guid: ' + guid)
    .replace(/spritePixelsToUnits: \d+/, 'spritePixelsToUnits: 100')
    .replace(/spritePivot: \{[^}]*\}/, 'spritePivot: {x: 0.5, y: 0.5}');

  const ids = [];
  let sprites = '  spriteSheet:\n    serializedVersion: 2\n    sprites:\n';
  for (let i = 0; i < FRAMES; i++) {
    const name = `${baseName}_${i}`;
    const spriteHex = md5('sprite:' + name);
    let internalId = signedInt32(md5('id:' + name));
    if (internalId === 0) internalId = 1;
    ids.push([name, internalId]);

    sprites +=
      `    - serializedVersion: 2\n` +
      `      name: "${escapeUnicode(name)}"\n` +
      `      rect:\n` +
      `        serializedVersion: 2\n` +
      `        x: ${i * FRAME}\n` +
      `        y: 0\n` +
      `        width: ${FRAME}\n` +
      `        height: ${FRAME}\n` +
      `      alignment: 0\n` +
      `      pivot: {x: 0.5, y: 0.5}\n` +
      `      border: {x: 0, y: 0, z: 0, w: 0}\n` +
      `      outline: []\n` +
      `      physicsShape: []\n` +
      `      tessellationDetail: 0\n` +
      `      bones: []\n` +
      `      spriteID: ${spriteHex}\n` +
      `      internalID: ${internalId}\n` +
      `      vertices: []\n` +
      `      indices: \n` +
      `      edges: []\n` +
      `      weights: []\n`;
  }

  sprites +=
    `    outline: []\n` +
    `    physicsShape: []\n` +
    `    bones: []\n` +
    `    spriteID: ${md5('sheet:' + baseName).slice(0, 16)}0000000000000000\n` +
    `    internalID: 0\n` +
    `    vertices: []\n` +
    `    indices: \n` +
    `    edges: []\n` +
    `    weights: []\n` +
    `    secondaryTextures: []\n` +
    `    nameFileIdTable:\n` +
    ids.map(([name, id]) => `      "${escapeUnicode(name)}": ${id}\n`).join('');

  fs.writeFileSync(path.join(dir, file + '.meta'), meta + sprites + tail);
  console.log('meta', file, guid);
}
```

- [ ] **Step 3: 실행** — `node "$TEMP/gen-pet-meta.js" "$(pwd -W)"`. 기대: 9개 `meta ...` 줄.

- [ ] **Step 4: 임포트 검증용 임시 에디터 스크립트** — `unity/Assets/Editor/PetImportCheck.cs`(커밋하지 않는다)

```csharp
using UnityEditor;
using UnityEngine;

public static class PetImportCheck
{
    public static void Run()
    {
        AssetDatabase.Refresh();
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/펫" });
        Debug.Log($"[PetImportCheck] textures={guids.Length}");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            int sprites = 0;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (o is Sprite s && s.rect.width == 25f && s.rect.height == 25f) sprites++;
            }
            Debug.Log($"[PetImportCheck] {path} sprites25={sprites}");
        }
    }
}
```

- [ ] **Step 5: 배치 임포트·검증** (에디터를 닫은 상태)

```bash
cd unity && "C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" -batchmode -quit -projectPath "$(pwd -W)" -executeMethod PetImportCheck.Run -logFile - > "$TEMP/pet_import.log" 2>&1
grep "PetImportCheck" "$TEMP/pet_import.log"
```

기대: `textures=9`와 아홉 줄 모두 `sprites25=5`. 5가 아니면 `.meta` 구조를 템플릿과 비교해 고친다(안 되면 사용자가 Sprite Editor로 슬라이스하는 수동 경로로 대체: 시트 선택 → Sprite Mode Multiple → Sprite Editor → Slice → Grid By Cell Size 25×25 → Apply).

- [ ] **Step 6: 임시 파일 정리와 `.meta` 확인** — `unity/Assets/Editor/PetImportCheck.cs`와 그 `.meta`(폴더 `Editor`를 이 때문에 만들었다면 폴더와 `Editor.meta`까지)를 삭제한다. `git status`로 `Assets/Art/펫/*.png`, `*.png.meta`, `Assets/Art/펫.meta` 외에 다른 새 파일이 없는지 확인한다. 임포트가 `.meta`를 다시 썼다면(필드 순서·줄바꿈 정규화) 그 결과를 커밋한다.

- [ ] **Step 7: 커밋·PR** (브랜치 `feature/pet-art-import`)

```bash
git add "unity/Assets/Art/펫" "unity/Assets/Art/펫.meta"
git commit -m "feat: 펫 시트 9장 임포트(25×25 슬라이스 5프레임)"
git push -u origin feature/pet-art-import
gh pr create --base main --title "feat: 펫 아트 9종 임포트" --body "<요약>"
```

PR 본문: 시트 규격(125×25, 25×25 프레임 5장, PPU 100, Point), 슬라이스 `.meta`를 스크립트로 생성했다는 점, 에디터에서 한 번 열어 스프라이트가 5개씩 보이는지 확인 부탁.

---

## PR C — 원자적 전환 (선택지·컨트롤러·카드·이월)

> A가 `main`에 머지된 뒤 최신 `main`에서 `feature/pet-catalog-switch`를 판다.
> **Task 6~10은 서로 컴파일 의존이라 중간에 컴파일하지 말고 모두 고친 뒤 Task 11에서 한꺼번에 검증·커밋한다.**
> 시작 전 확인: `grep -rn "BuildChoices\|FairyChoiceLogic\|FairyChoice\b\|\.Summon()\|\.Restore(\|RestoreFairies\|FairyLevels\|fairyData" unity/Assets --include=*.cs`로 소비자를 전부 센다 — 기대: `FairyController`·`FairyOption`·`LevelSystem`·`GameManager`·`BossFightDirector`·`RunResultCarrier`·`FairyChoiceLogicTests`뿐.

### Task 6: `FairyChoiceLogic` 개정과 테스트 교체

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Companions/FairyChoiceLogic.cs` (전체 교체)
- Modify: `unity/Assets/Tests/EditMode/FairyChoiceLogicTests.cs` (전체 교체)

**Interfaces:**
- Produces:
  - `FairyChoice`에 `int KindIndex` 추가(Summon: 카탈로그 번호, Upgrade: -1). Summon의 `Index`는 -1.
  - `public static List<FairyChoice> FairyChoiceLogic.Build(IReadOnlyList<int> ownedKinds, IReadOnlyList<int> levels, int catalogSize, int maxCount, int maxLevel, System.Random random)` — `ownedKinds`와 `levels`는 같은 길이(요정 순서). `random`이 null이면 섞지 않고 카탈로그 순서.
  - `FairyChoiceLogic.MaxChoices = 3`(그대로)

- [ ] **Step 1: 테스트 교체** — `FairyChoiceLogicTests.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SushiSurvival.Companions;

namespace SushiSurvival.EditModeTests
{
    public class FairyChoiceLogicTests
    {
        private const int Catalog = 9;
        private static readonly int[] None = new int[0];

        private static List<FairyChoice> Build(int[] owned, int[] levels, int seed = 1, int catalog = Catalog)
            => FairyChoiceLogic.Build(owned, levels, catalog, 3, 4, new Random(seed));

        [Test]
        public void NoFairies_OffersThreeDistinctSummons()
        {
            List<FairyChoice> choices = Build(None, None);

            Assert.AreEqual(3, choices.Count);
            foreach (FairyChoice choice in choices)
            {
                Assert.AreEqual(FairyChoiceKind.Summon, choice.Kind);
                Assert.AreEqual(-1, choice.Index);
                Assert.That(choice.KindIndex, Is.InRange(0, Catalog - 1));
            }
            Assert.AreEqual(3, choices.Select(c => c.KindIndex).Distinct().Count());
        }

        [Test]
        public void OwnedKinds_AreNeverOffered()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                List<FairyChoice> choices = Build(new[] { 0, 4 }, new[] { 1, 2 }, seed);

                Assert.AreEqual(3, choices.Count);
                foreach (FairyChoice choice in choices)
                {
                    Assert.AreNotEqual(0, choice.KindIndex);
                    Assert.AreNotEqual(4, choice.KindIndex);
                }
            }
        }

        [Test]
        public void FewUnownedKinds_OffersOnlyThatMany()
        {
            List<FairyChoice> choices = Build(new[] { 0, 1 }, new[] { 1, 1 }, 1, 3);

            Assert.AreEqual(1, choices.Count);
            Assert.AreEqual(2, choices[0].KindIndex);
        }

        [Test]
        public void EmptyCatalog_ReturnsEmpty()
        {
            Assert.IsEmpty(Build(None, None, 1, 0));
        }

        [Test]
        public void SameSeed_GivesSameChoices()
        {
            List<int> a = Build(None, None, 7).Select(c => c.KindIndex).ToList();
            List<int> b = Build(None, None, 7).Select(c => c.KindIndex).ToList();

            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentChoicesSometimes()
        {
            var firsts = new HashSet<string>();
            for (int seed = 0; seed < 20; seed++)
                firsts.Add(string.Join(",", Build(None, None, seed).Select(c => c.KindIndex)));

            Assert.Greater(firsts.Count, 1);
        }

        [Test]
        public void NullRandom_UsesCatalogOrder()
        {
            List<FairyChoice> choices = FairyChoiceLogic.Build(new[] { 1 }, new[] { 1 }, Catalog, 3, 4, null);

            CollectionAssert.AreEqual(new[] { 0, 2, 3 }, choices.Select(c => c.KindIndex).ToArray());
        }

        [Test]
        public void Full_OffersOnlyUpgrades_NeverMoreThanThree()
        {
            List<FairyChoice> choices = Build(new[] { 0, 1, 2 }, new[] { 1, 1, 1 });

            Assert.AreEqual(3, choices.Count);
            for (int i = 0; i < choices.Count; i++)
            {
                Assert.AreEqual(FairyChoiceKind.Upgrade, choices[i].Kind);
                Assert.AreEqual(i, choices[i].Index);
                Assert.AreEqual(-1, choices[i].KindIndex);
            }
        }

        [Test]
        public void Full_MaxLevelFairy_IsNotOfferedAnUpgrade()
        {
            List<FairyChoice> choices = Build(new[] { 0, 1, 2 }, new[] { 4, 2, 4 });

            Assert.AreEqual(1, choices.Count);
            Assert.AreEqual(1, choices[0].Index);
        }

        [Test]
        public void Full_EverythingMaxed_ReturnsEmpty()
        {
            Assert.IsEmpty(Build(new[] { 0, 1, 2 }, new[] { 4, 4, 4 }));
        }

        [Test]
        public void NotFull_NeverMixesUpgradesIn()
        {
            List<FairyChoice> choices = Build(new[] { 0 }, new[] { 1 });

            foreach (FairyChoice choice in choices)
                Assert.AreEqual(FairyChoiceKind.Summon, choice.Kind);
        }

        [Test]
        public void NullLists_AreTreatedAsNoFairies()
        {
            List<FairyChoice> choices = FairyChoiceLogic.Build(null, null, Catalog, 3, 4, new Random(1));

            Assert.AreEqual(3, choices.Count);
        }
    }
}
```

- [ ] **Step 2: `FairyChoiceLogic.cs` 전체 교체**

```csharp
using System;
using System.Collections.Generic;

namespace SushiSurvival.Companions
{
    public enum FairyChoiceKind
    {
        Summon,
        Upgrade
    }

    public struct FairyChoice
    {
        public FairyChoiceKind Kind;

        /// <summary>강화할 요정의 번호(0부터). 소환이면 -1.</summary>
        public int Index;

        /// <summary>소환할 펫의 카탈로그 번호. 강화면 -1.</summary>
        public int KindIndex;
    }

    public static class FairyChoiceLogic
    {
        /// <summary>레벨업 카드가 세 장이라 선택지도 그 이상 만들지 않는다.</summary>
        public const int MaxChoices = 3;

        /// <summary>
        /// 빈 자리가 있으면 아직 안 가진 펫 중 무작위 최대 3종을 소환 후보로, 가득 차면 최대 레벨 미만인
        /// 요정마다 강화 후보를 돌려준다(둘은 섞지 않는다). 후보가 없으면 빈 목록.
        /// random이 null이면 섞지 않고 카탈로그 순서를 쓴다.
        /// </summary>
        public static List<FairyChoice> Build(IReadOnlyList<int> ownedKinds, IReadOnlyList<int> levels,
                                              int catalogSize, int maxCount, int maxLevel, Random random)
        {
            var choices = new List<FairyChoice>();
            int count = levels != null ? levels.Count : 0;

            if (count < maxCount)
            {
                var unowned = new List<int>();
                for (int kind = 0; kind < catalogSize; kind++)
                {
                    if (!Contains(ownedKinds, kind))
                        unowned.Add(kind);
                }

                if (random != null)
                    Shuffle(unowned, random);

                for (int i = 0; i < unowned.Count && choices.Count < MaxChoices; i++)
                    choices.Add(new FairyChoice { Kind = FairyChoiceKind.Summon, Index = -1, KindIndex = unowned[i] });

                return choices;
            }

            for (int i = 0; i < count && choices.Count < MaxChoices; i++)
            {
                if (levels[i] >= maxLevel) continue;

                choices.Add(new FairyChoice { Kind = FairyChoiceKind.Upgrade, Index = i, KindIndex = -1 });
            }

            return choices;
        }

        private static bool Contains(IReadOnlyList<int> list, int value)
        {
            if (list == null) return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == value) return true;
            }

            return false;
        }

        private static void Shuffle(List<int> list, Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
```

### Task 7: `Fairy` — `FairyData`·프레임·뒤집기

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Companions/Fairy.cs` (전체 교체)

**Interfaces:**
- Consumes: `FairyData`, `FrameAnimLogic.FrameIndex`, `FairyFacingLogic.FlipX`, 기존 `FairySlotLogic`·`FairyTargetLogic`·`CooldownLogic`·`WeaponCooldown`·`Projectile`
- Produces: `Fairy.Initialize(Transform player, PlayerStats stats, FairyData data, GameObjectPool projectilePool, LayerMask enemyLayer, FairyMotion motion, int level, int slotIndex, int slotCount)`, `public FairyData Data { get; }`, 그 외 `Level`·`SetLevel`·`SetSlot`·`SetPlayer`는 그대로. `FairyMotion` 구조체도 이 파일에 그대로 둔다.

- [ ] **Step 1: 전체 교체** — `Fairy.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;
using SushiSurvival.Enemies;
using SushiSurvival.Player;
using SushiSurvival.Weapons;

namespace SushiSurvival.Companions
{
    [Serializable]
    public struct FairyMotion
    {
        [Tooltip("플레이어 중심에서 슬롯까지 거리.")]
        public float radius;
        [Tooltip("요정 사이의 각도 간격(도). 머리 위(90°)를 중심으로 부채꼴로 선다.")]
        public float arcSpacingDegrees;
        [Tooltip("위아래로 둥둥 떠다니는 폭.")]
        public float bobAmplitude;
        public float bobSpeed;
        [Tooltip("클수록 슬롯을 빨리 따라간다.")]
        public float followSharpness;

        public static FairyMotion Default => new FairyMotion
        {
            radius = 1.2f,
            arcSpacingDegrees = 60f,
            bobAmplitude = 0.12f,
            bobSpeed = 3f,
            followSharpness = 6f
        };
    }

    /// <summary>
    /// 펫 한 마리. 플레이어 주변 슬롯을 부드럽게 따라다니고, 쿨타임마다 사거리 안의 가장 가까운 적에게
    /// 기존 Projectile을 쏜다. 수치와 그림은 FairyData에서 읽고(레벨 표 + 프레임 애니메이션) 플레이어 증강 배율을 곱한다.
    /// 프레임이 비어 있으면 스프라이트를 건드리지 않아 컨트롤러의 플레이스홀더 원이 그대로 보인다.
    /// </summary>
    public class Fairy : MonoBehaviour
    {
        [Tooltip("공격속도 증강이 아무리 쌓여도 이 값보다 짧아지지 않는다(무한 연사 방지).")]
        [SerializeField] private float minCooldown = 0.2f;

        private readonly WeaponCooldown _cooldown = new WeaponCooldown();
        private readonly List<Vector2> _positions = new List<Vector2>();
        private readonly List<Transform> _targets = new List<Transform>();

        private Transform _player;
        private PlayerStats _stats;
        private FairyData _data;
        private GameObjectPool _pool;
        private LayerMask _enemyLayer;
        private FairyMotion _motion;
        private SpriteRenderer _renderer;
        private int _level = 1;
        private int _slotIndex;
        private int _slotCount = 1;
        private float _time;
        private float _animTime;
        private bool _flipped;

        public int Level => _level;
        public FairyData Data => _data;

        public void Initialize(Transform player, PlayerStats stats, FairyData data, GameObjectPool projectilePool,
                               LayerMask enemyLayer, FairyMotion motion, int level, int slotIndex, int slotCount)
        {
            _player = player;
            _stats = stats;
            _data = data;
            _pool = projectilePool;
            _enemyLayer = enemyLayer;
            _motion = motion;
            _slotIndex = slotIndex;
            _slotCount = slotCount;
            _renderer = GetComponent<SpriteRenderer>();
            SetLevel(level);

            if (_data != null)
                transform.localScale = Vector3.one * Mathf.Max(0.01f, _data.visualScale);

            // 소환되는 순간 플레이어 옆에서 시작하게 해 화면 구석에서 날아오지 않게 한다.
            if (_player != null)
                transform.position = TargetPosition();

            ApplyVisual(0f);
        }

        public void SetPlayer(Transform player, PlayerStats stats)
        {
            _player = player;
            _stats = stats;
        }

        public void SetLevel(int level)
        {
            int max = _data != null && _data.levels != null ? _data.levels.Length : 1;
            _level = Mathf.Clamp(level, 1, Mathf.Max(1, max));
        }

        public void SetSlot(int slotIndex, int slotCount)
        {
            _slotIndex = slotIndex;
            _slotCount = slotCount;
        }

        private void Update()
        {
            if (_player == null || _data == null || _data.levels == null || _data.levels.Length == 0) return;

            float dt = Time.deltaTime;
            _time += dt;
            _animTime += dt;

            float beforeX = transform.position.x;
            transform.position = FairySlotLogic.Follow(
                transform.position, TargetPosition(), _motion.followSharpness, dt);
            float velocityX = dt > 0f ? (transform.position.x - beforeX) / dt : 0f;

            ApplyVisual(velocityX);

            _cooldown.Tick(dt);
            if (!_cooldown.IsReady) return;

            WeaponLevelStats stats = _data.levels[_level - 1];
            if (!TryFire(stats)) return;

            _cooldown.Reset(CooldownLogic.ApplyAttackSpeed(
                stats.cooldown, StatMultiplier(StatType.AttackSpeed), minCooldown));
        }

        /// <summary>프레임을 시간에 맞춰 바꾸고 이동 방향에 따라 좌우를 뒤집는다. 프레임이 없으면 아무것도 안 한다.</summary>
        private void ApplyVisual(float velocityX)
        {
            if (_renderer == null || _data == null || _data.frames == null || _data.frames.Length == 0) return;

            int frame = FrameAnimLogic.FrameIndex(_animTime, _data.framesPerSecond, _data.frames.Length);
            _renderer.sprite = _data.frames[frame];

            _flipped = FairyFacingLogic.FlipX(velocityX, _flipped, _data.facesRight);
            _renderer.flipX = _flipped;
        }

        private Vector2 TargetPosition()
            => (Vector2)_player.position + FairySlotLogic.SlotOffset(
                _slotIndex, _slotCount, _motion.radius, _motion.arcSpacingDegrees,
                _time, _motion.bobAmplitude, _motion.bobSpeed);

        /// <summary>타깃이 없으면 쏘지 않고 false — 쿨타임도 소모하지 않아 적이 나타나는 즉시 쏜다.</summary>
        private bool TryFire(WeaponLevelStats stats)
        {
            if (_pool == null)
            {
                Debug.LogError($"{name}: projectilePool이 없어 발사할 수 없습니다.");
                return false;
            }

            float range = stats.range * StatMultiplier(StatType.AttackRange);
            Vector2 origin = transform.position;

            _positions.Clear();
            _targets.Clear();

            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, _enemyLayer);
            foreach (Collider2D hit in hits)
            {
                if (!hit.TryGetComponent<EnemyBase>(out _)) continue;

                _positions.Add(hit.transform.position);
                _targets.Add(hit.transform);
            }

            int index = FairyTargetLogic.NearestIndex(origin, _positions, range);
            if (index < 0) return false;

            Vector2 direction = ((Vector2)_targets[index].position - origin).normalized;
            float rotation = WeaponVisualLogic.ComputeRotationDegrees(direction);

            GameObject projectileObj = _pool.Get(origin, Quaternion.Euler(0f, 0f, rotation));
            if (!projectileObj.TryGetComponent<Projectile>(out var projectile))
            {
                Debug.LogError($"{projectileObj.name}: Projectile 컴포넌트가 없어 발사할 수 없습니다.");
                _pool.Release(projectileObj);
                return false;
            }

            projectile.Initialize(direction, stats.damage * StatMultiplier(StatType.AttackDamage),
                                  stats.pierceCount, _pool);
            return true;
        }

        private float StatMultiplier(StatType stat) => _stats != null ? _stats.GetValue(stat) : 1f;
    }
}
```

### Task 8: `FairyController` — 카탈로그와 종류 관리

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Companions/FairyController.cs` (전체 교체)

**Interfaces:**
- Consumes: `Fairy.Initialize(…, FairyData …)`, `FairyChoiceLogic.Build(...)`(Task 6), `FairyDescriptionLogic.DescribeSummon`(PR A), `UpgradeDescriptionLogic.DescribeWeaponUpgrade`, `CircleTextureFactory.CreateSprite(int, float, Color)`
- Produces:
  - `public int Count`, `MaxCount`, `MaxLevel`(카탈로그 펫들의 `levels` 길이 중 최솟값, 카탈로그가 비면 1)
  - `public IReadOnlyList<int> Levels`, `Kinds`(요정 순서, 호출마다 내부 리스트를 다시 채워 돌려주므로 보관하려면 복사)
  - `public void SetPlayer(Transform player, PlayerStats stats)`
  - `public bool Summon(int kindIndex)` — 가득 찼거나 이미 가진 종류거나 번호가 범위 밖이거나 `FairyData`가 null이면 false
  - `public bool Upgrade(int index)`
  - `public void Restore(IReadOnlyList<int> kinds, IReadOnlyList<int> levels)` — 둘 중 하나가 null이거나 길이가 다르면 아무것도 안 한다
  - `public List<FairyChoice> BuildChoices()`
  - `public string NameOfKind(int kindIndex)`, `public Sprite IconOfKind(int kindIndex)`(첫 프레임, 없으면 null), `public string DescribeSummon(int kindIndex)`, `public string DescribeUpgrade(int index)`, `public string NameOfFairy(int index)`

- [ ] **Step 1: 전체 교체** — `FairyController.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;
using SushiSurvival.Player;

namespace SushiSurvival.Companions
{
    /// <summary>
    /// 펫(요정) 시스템 총괄 — 씬 오브젝트(캐릭터 프리팹에 붙이지 않는다). 카탈로그(펫 종류 목록)를 들고
    /// 요정마다 (종류 번호, 레벨)을 관리하며, 소환·강화·보스 씬 복원·카드 문구를 제공한다.
    /// </summary>
    public class FairyController : MonoBehaviour
    {
        [Tooltip("펫 종류 목록. 소환 카드는 이 중 아직 안 가진 종류에서 무작위로 뽑는다. 비어 있으면 요정 대신 스탯 버프 보상이 나온다.")]
        [SerializeField] private FairyData[] catalog;
        [Tooltip("요정 투사체 풀. 풀 하나당 GameObject 하나(같은 오브젝트에 풀을 둘 붙이지 말 것).")]
        [SerializeField] private GameObjectPool projectilePool;
        [SerializeField] private LayerMask enemyLayer;
        [Tooltip("요정 프리팹(SpriteRenderer + Fairy). 비워두면 SpriteRenderer를 런타임에 만든다.")]
        [SerializeField] private Fairy fairyPrefab;
        [SerializeField] private int maxFairies = 3;
        [SerializeField] private FairyMotion motion = FairyMotion.Default;

        private readonly List<Fairy> _fairies = new List<Fairy>();
        private readonly List<int> _kinds = new List<int>();
        private readonly List<int> _levelsCache = new List<int>();
        private readonly System.Random _random = new System.Random();
        private Transform _player;
        private PlayerStats _stats;

        public int Count => _fairies.Count;
        public int MaxCount => maxFairies;

        public int MaxLevel
        {
            get
            {
                if (catalog == null || catalog.Length == 0) return 1;

                int min = int.MaxValue;
                foreach (FairyData data in catalog)
                {
                    if (data == null || data.levels == null) continue;
                    min = Mathf.Min(min, data.levels.Length);
                }

                return min == int.MaxValue ? 1 : min;
            }
        }

        /// <summary>요정마다의 현재 레벨(소환 순서). 호출마다 내부 리스트를 다시 채워 돌려주므로 보관하려면 복사한다.</summary>
        public IReadOnlyList<int> Levels
        {
            get
            {
                _levelsCache.Clear();
                foreach (Fairy fairy in _fairies)
                    _levelsCache.Add(fairy.Level);
                return _levelsCache;
            }
        }

        /// <summary>요정마다의 카탈로그 번호(소환 순서).</summary>
        public IReadOnlyList<int> Kinds => _kinds;

        public void SetPlayer(Transform player, PlayerStats stats)
        {
            _player = player;
            _stats = stats;

            foreach (Fairy fairy in _fairies)
                fairy.SetPlayer(player, stats);
        }

        /// <summary>kindIndex번 펫을 Lv1로 소환한다. 가득 찼거나 이미 가졌거나 잘못된 번호면 false.</summary>
        public bool Summon(int kindIndex)
        {
            if (_fairies.Count >= maxFairies) return false;
            if (_kinds.Contains(kindIndex)) return false;

            FairyData data = DataOf(kindIndex);
            if (data == null)
            {
                Debug.LogError($"{name}: 카탈로그 {kindIndex}번이 비어 있어 펫을 소환할 수 없습니다.");
                return false;
            }

            Fairy fairy = CreateFairy(data);
            _fairies.Add(fairy);
            _kinds.Add(kindIndex);
            fairy.Initialize(_player, _stats, data, projectilePool, enemyLayer, motion,
                             1, _fairies.Count - 1, _fairies.Count);

            RefreshSlots();
            return true;
        }

        /// <summary>index번 요정을 한 단계 강화한다. 최대 레벨이거나 없는 번호면 false.</summary>
        public bool Upgrade(int index)
        {
            if (index < 0 || index >= _fairies.Count) return false;

            Fairy fairy = _fairies[index];
            if (fairy.Level >= MaxLevel) return false;

            fairy.SetLevel(fairy.Level + 1);
            return true;
        }

        /// <summary>
        /// 기존 요정을 비우고 (종류, 레벨) 배열대로 다시 만든다(보스 씬 복원용). 둘 중 하나가 null이거나
        /// 길이가 다르면 아무것도 하지 않는다. SetPlayer가 먼저 불려 있어야 요정이 플레이어 곁에서 시작한다.
        /// </summary>
        public void Restore(IReadOnlyList<int> kinds, IReadOnlyList<int> levels)
        {
            if (kinds == null || levels == null || kinds.Count != levels.Count) return;

            foreach (Fairy fairy in _fairies)
            {
                if (fairy != null) Destroy(fairy.gameObject);
            }
            _fairies.Clear();
            _kinds.Clear();

            for (int i = 0; i < kinds.Count; i++)
            {
                if (!Summon(kinds[i])) continue;

                _fairies[_fairies.Count - 1].SetLevel(levels[i]);
            }
        }

        public List<FairyChoice> BuildChoices()
            => FairyChoiceLogic.Build(_kinds, Levels, catalog != null ? catalog.Length : 0,
                                      maxFairies, MaxLevel, _random);

        public string NameOfKind(int kindIndex)
        {
            FairyData data = DataOf(kindIndex);
            return data != null ? data.displayName : string.Empty;
        }

        /// <summary>index번 요정의 펫 이름.</summary>
        public string NameOfFairy(int index)
            => index >= 0 && index < _kinds.Count ? NameOfKind(_kinds[index]) : string.Empty;

        /// <summary>카드 아이콘으로 쓸 펫의 첫 프레임. 프레임이 없으면 null.</summary>
        public Sprite IconOfKind(int kindIndex)
        {
            FairyData data = DataOf(kindIndex);
            return data != null && data.frames != null && data.frames.Length > 0 ? data.frames[0] : null;
        }

        public string DescribeSummon(int kindIndex)
        {
            FairyData data = DataOf(kindIndex);
            if (data == null || data.levels == null || data.levels.Length == 0) return string.Empty;

            return FairyDescriptionLogic.DescribeSummon(data.roleLine, data.levels[0]);
        }

        /// <summary>다음 레벨과의 수치 비교 문구. 최대 레벨이거나 없는 번호면 빈 문자열.</summary>
        public string DescribeUpgrade(int index)
        {
            if (index < 0 || index >= _fairies.Count) return string.Empty;

            FairyData data = DataOf(_kinds[index]);
            int level = _fairies[index].Level;
            if (data == null || data.levels == null || level >= data.levels.Length) return string.Empty;

            return UpgradeDescriptionLogic.DescribeWeaponUpgrade(data.levels[level - 1], data.levels[level]);
        }

        private FairyData DataOf(int kindIndex)
            => catalog != null && kindIndex >= 0 && kindIndex < catalog.Length ? catalog[kindIndex] : null;

        private void RefreshSlots()
        {
            for (int i = 0; i < _fairies.Count; i++)
                _fairies[i].SetSlot(i, _fairies.Count);
        }

        private Fairy CreateFairy(FairyData data)
        {
            Fairy fairy;

            if (fairyPrefab != null)
            {
                fairy = Instantiate(fairyPrefab, transform);
            }
            else
            {
                var go = new GameObject($"Fairy ({data.displayName})");
                go.transform.SetParent(transform, false);
                go.AddComponent<SpriteRenderer>();
                fairy = go.AddComponent<Fairy>();
            }

            var spriteRenderer = fairy.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = fairy.gameObject.AddComponent<SpriteRenderer>();

            // 프레임이 있으면 첫 프레임, 아직 없으면 노란 원으로 대신해 아트 연결 전에도 보이게 한다.
            spriteRenderer.sprite = data.frames != null && data.frames.Length > 0
                ? data.frames[0]
                : CircleTextureFactory.CreateSprite(24, 0f, new Color(1f, 0.85f, 0.4f));
            spriteRenderer.sortingOrder = 50;

            return fairy;
        }
    }
}
```

### Task 9: `FairyOption` — 펫 이름·아이콘·역할

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Core/FairyOption.cs` (전체 교체)

**Interfaces:**
- Consumes: `FairyController.NameOfKind/NameOfFairy/IconOfKind/DescribeSummon/DescribeUpgrade/Levels/Summon/Upgrade`, `FairyChoice`
- Produces: `public FairyOption(FairyController controller, FairyChoice choice, Sprite fallbackIcon)` — 펫 아이콘이 없으면 `fallbackIcon`을 쓴다.

- [ ] **Step 1: 전체 교체**

```csharp
using UnityEngine;
using SushiSurvival.Companions;

namespace SushiSurvival.Core
{
    /// <summary>와사비 성공 뒤 레벨업 카드로 뜨는 "{펫} 소환 / {펫} 강화" 선택지.</summary>
    public class FairyOption : IUpgradeOption
    {
        private readonly FairyController _controller;
        private readonly FairyChoice _choice;

        public Sprite Icon { get; }

        public string DisplayName => _choice.Kind == FairyChoiceKind.Summon
            ? $"{_controller.NameOfKind(_choice.KindIndex)} 소환"
            : $"{_controller.NameOfFairy(_choice.Index)} 강화 Lv{_controller.Levels[_choice.Index] + 1}";

        public string Description => _choice.Kind == FairyChoiceKind.Summon
            ? _controller.DescribeSummon(_choice.KindIndex)
            : _controller.DescribeUpgrade(_choice.Index);

        public FairyOption(FairyController controller, FairyChoice choice, Sprite fallbackIcon)
        {
            _controller = controller;
            _choice = choice;

            Sprite petIcon = choice.Kind == FairyChoiceKind.Summon
                ? controller.IconOfKind(choice.KindIndex)
                : null;
            Icon = petIcon != null ? petIcon : fallbackIcon;
        }

        public void Apply()
        {
            if (_choice.Kind == FairyChoiceKind.Summon)
                _controller.Summon(_choice.KindIndex);
            else
                _controller.Upgrade(_choice.Index);
        }
    }
}
```

> 강화 카드의 아이콘도 해당 펫이면 좋지만, 요정 번호→종류 변환이 필요해 이번 범위에서는 `fallbackIcon`(`LevelSystem.fairyIcon`)을 쓴다.

### Task 10: 이월과 `LevelSystem` 연결

**Files:**
- Modify: `unity/Assets/_Project/Scripts/UI/RunResultCarrier.cs` (`FairyLevels` 필드 위/아래)
- Modify: `unity/Assets/_Project/Scripts/Core/GameManager.cs` (`RunResultCarrier.FairyLevels = ...` 줄)
- Modify: `unity/Assets/_Project/Scripts/Core/BossFightDirector.cs` (`levelSystem.RestoreFairies(...)` 줄)
- Modify: `unity/Assets/_Project/Scripts/Core/LevelSystem.cs` (`FairyLevels`·`RestoreFairies` 부근)

**Interfaces:**
- Produces: `RunResultCarrier.FairyKinds`(`int[]`, `FairyLevels`와 같은 길이), `LevelSystem.FairyKinds`(`IReadOnlyList<int>`), `LevelSystem.RestoreFairies(IReadOnlyList<int> kinds, IReadOnlyList<int> levels)`.

- [ ] **Step 1: `RunResultCarrier`** — `FairyLevels` 필드 바로 아래에 추가

```csharp
        // FairyLevels와 같은 길이·같은 순서로, 요정마다의 펫 종류(카탈로그 번호).
        public static int[] FairyKinds;
```

- [ ] **Step 2: `GameManager`** — `RunResultCarrier.FairyLevels = new List<int>(levelSystem.FairyLevels).ToArray();` 바로 아래에 추가

```csharp
                RunResultCarrier.FairyKinds = new List<int>(levelSystem.FairyKinds).ToArray();
```

- [ ] **Step 3: `BossFightDirector`** — 아래 줄을 교체

```csharp
                    levelSystem.RestoreFairies(RunResultCarrier.FairyKinds, RunResultCarrier.FairyLevels);
```

- [ ] **Step 4: `LevelSystem`** — `FairyLevels`/`RestoreFairies`를 다음으로 교체(앞뒤 문맥은 그대로)

```csharp
        /// <summary>현재 요정들의 레벨(소환 순서). 요정 시스템이 없으면 빈 목록. 보스 씬 이월용.</summary>
        public IReadOnlyList<int> FairyLevels
            => fairyController != null ? fairyController.Levels : (IReadOnlyList<int>)Array.Empty<int>();

        /// <summary>현재 요정들의 펫 종류(카탈로그 번호, FairyLevels와 같은 순서). 보스 씬 이월용.</summary>
        public IReadOnlyList<int> FairyKinds
            => fairyController != null ? fairyController.Kinds : (IReadOnlyList<int>)Array.Empty<int>();

        /// <summary>보스 씬에서 이전 씬의 펫을 같은 종류·레벨로 되살린다. SetPlayer 뒤에 불러야 한다.</summary>
        public void RestoreFairies(IReadOnlyList<int> kinds, IReadOnlyList<int> levels)
        {
            if (fairyController != null)
                fairyController.Restore(kinds, levels);
        }
```

`ShowFairyChoice`의 `new FairyOption(fairyController, choice, fairyIcon)`와 `HandleRoyalWasabiRequested`의 `hasFairyReward` 계산(`fairyController.BuildChoices().Count > 0`)은 시그니처가 그대로라 손대지 않는다. 카탈로그가 비면 `BuildChoices()`가 빈 목록이라 기존 **스탯 버프 대체 보상**으로 넘어간다.

### Task 11: 컴파일·테스트·커밋·PR

- [ ] **Step 1: 소비자 재확인** — `grep -rn "fairyData\|\.Summon()\|\.Restore(\|RestoreFairies(\|BuildChoices" unity/Assets --include=*.cs`. 남은 옛 호출(`Summon()` 인자 없음, `Restore(` 단일 인자)이 없어야 한다.

- [ ] **Step 2: 배치 테스트** — 컴파일 에러 0, 전체 통과(기존 실패 1건 제외). `FairyChoiceLogicTests`가 새 11개로 바뀌므로 총 개수는 A 머지 직후 값에서 약 +3.

- [ ] **Step 3: 커밋·PR** (한 커밋)

```bash
git add unity/Assets/_Project/Scripts/Companions/FairyChoiceLogic.cs unity/Assets/_Project/Scripts/Companions/Fairy.cs unity/Assets/_Project/Scripts/Companions/FairyController.cs unity/Assets/_Project/Scripts/Core/FairyOption.cs unity/Assets/_Project/Scripts/Core/LevelSystem.cs unity/Assets/_Project/Scripts/Core/GameManager.cs unity/Assets/_Project/Scripts/Core/BossFightDirector.cs unity/Assets/_Project/Scripts/UI/RunResultCarrier.cs unity/Assets/Tests/EditMode/FairyChoiceLogicTests.cs
git commit -m "feat: 펫 카탈로그 전환 — 종류별 소환 카드·프레임 애니메이션·종류 이월"
git push -u origin feature/pet-catalog-switch
gh pr create --base main --title "feat: 펫 카탈로그 전환(소환 랜덤 3종·프레임·이월)" --body "<요약·테스트 결과>"
```

PR 본문: 원자적 전환이라는 점, 카탈로그가 비어 있는 동안(씬 배선 전) 와사비 성공은 스탯 버프 대체 보상으로 안전하게 동작한다는 점, 씬의 옛 `fairyData` 필드는 고아 값으로 무해하다는 점, 씬 변경 없음.

---

## PR D — `FairyData` 에셋 9개와 구조 테스트

> A와 B가 `main`에 머지된 뒤 최신 `main`에서 `feature/pet-data-assets`를 판다. 시트 `.meta`가 `main`에 있어야 `nameFileIdTable`을 읽을 수 있다.

### Task 12: 에셋 생성 스크립트

**Files:**
- Create: `unity/Assets/_Project/Data/Fairies/<펫>.asset` 9개 + `.asset.meta` + `Fairies.meta`
- Create(임시, 커밋 안 함): `$TEMP/gen-pet-assets.js`

**Interfaces:**
- Consumes: `Assets/Art/펫/<이름>-Sheet.png.meta`의 `guid`·`nameFileIdTable`, `FairyData.cs.meta`의 `guid`(MonoScript 참조), 아래 역할 표와 성장 규칙(피해 ×1/1.25/1.5/2.0, 쿨타임 ×1/0.92/0.85/0.75, 관통 Lv3부터 +1, 사거리 불변).
- Produces: 카탈로그 순서(씬 연결 순서이기도 함) — 0 계란초밥, 1 간장새우, 2 유부초밥, 3 참치초밥, 4 연어초밥, 5 광어초밥, 6 문어초밥, 7 새우초밥, 8 장어초밥.

- [ ] **Step 1: 스크립트** — `$TEMP/gen-pet-assets.js`

```javascript
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const root = process.argv[2];
const artDir = path.join(root, 'unity', 'Assets', 'Art', '펫');
const outDir = path.join(root, 'unity', 'Assets', '_Project', 'Data', 'Fairies');
const scriptMeta = fs.readFileSync(
  path.join(root, 'unity', 'Assets', '_Project', 'Scripts', 'Data', 'FairyData.cs.meta'), 'utf8');
const scriptGuid = /^guid: (\w+)/m.exec(scriptMeta)[1];

const GROWTH_DAMAGE = [1, 1.25, 1.5, 2.0];
const GROWTH_COOLDOWN = [1, 0.92, 0.85, 0.75];

// 파일명에 포함되는 키워드, 표시 이름, 역할, Lv1 피해·쿨타임·사거리·관통. 배열 순서 = 카탈로그 번호.
const PETS = [
  { key: '계란초밥', name: '계란초밥', role: '균형형',          damage: 5,  cooldown: 1.0, range: 6, pierce: 0 },
  { key: '간장새우', name: '간장새우', role: '관통 저격',        damage: 6,  cooldown: 1.3, range: 7, pierce: 1 },
  { key: '유부초밥', name: '유부초밥', role: '빠른 연사',        damage: 3,  cooldown: 0.6, range: 5, pierce: 0 },
  { key: '참치초밥', name: '참치초밥', role: '고화력 느림',      damage: 10, cooldown: 1.8, range: 6, pierce: 0 },
  { key: '연어초밥', name: '연어초밥', role: '중간 연사',        damage: 4,  cooldown: 0.8, range: 6, pierce: 0 },
  { key: '광어초밥', name: '광어초밥', role: '장거리',          damage: 5,  cooldown: 1.2, range: 9, pierce: 0 },
  { key: '문어초밥', name: '문어초밥', role: '다관통',          damage: 4,  cooldown: 1.4, range: 6, pierce: 2 },
  { key: '새우초밥', name: '새우초밥', role: '단거리 연사',      damage: 4,  cooldown: 0.7, range: 4, pierce: 0 },
  { key: '장어초밥', name: '장어초밥', role: '중관통',          damage: 5,  cooldown: 1.1, range: 6, pierce: 1 },
];

const md5 = s => crypto.createHash('md5').update(s).digest('hex');
const escapeUnicode = s => [...s].map(c => (c.charCodeAt(0) > 127
  ? '\\u' + c.charCodeAt(0).toString(16).toUpperCase().padStart(4, '0') : c)).join('');
const round = v => Math.round(v * 100) / 100;

const sheets = fs.readdirSync(artDir).filter(f => f.endsWith('.png.meta')).map(f => f.normalize('NFC'));
fs.mkdirSync(outDir, { recursive: true });

for (const pet of PETS) {
  const sheet = sheets.find(f => f.includes(pet.key));
  if (!sheet) throw new Error('시트를 찾지 못했습니다: ' + pet.key);

  const meta = fs.readFileSync(path.join(artDir, sheet), 'utf8');
  const pngGuid = /^guid: (\w+)/m.exec(meta)[1];

  // nameFileIdTable의 "<이름>_N": <id> 줄을 N 순서대로.
  const table = [...meta.matchAll(/^\s+"[^"]*_(\d+)": (-?\d+)$/gm)]
    .map(m => ({ n: Number(m[1]), id: m[2] }))
    .sort((a, b) => a.n - b.n);
  if (table.length !== 5) throw new Error(`${pet.key}: 프레임이 ${table.length}개입니다(5개여야 함).`);

  const frames = table.map(t => `  - {fileID: ${t.id}, guid: ${pngGuid}, type: 3}`).join('\n');

  const levels = [0, 1, 2, 3].map(i => {
    const pierce = pet.pierce + (i >= 2 ? 1 : 0);
    return `  - damage: ${round(pet.damage * GROWTH_DAMAGE[i])}\n` +
           `    cooldown: ${round(pet.cooldown * GROWTH_COOLDOWN[i])}\n` +
           `    range: ${pet.range}\n` +
           `    angleDegrees: 0\n` +
           `    pierceCount: ${pierce}`;
  }).join('\n');

  const asset =
`%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${scriptGuid}, type: 3}
  m_Name: ${escapeUnicode(pet.name)}
  m_EditorClassIdentifier: 
  displayName: "${escapeUnicode(pet.name)}"
  roleLine: "${escapeUnicode(pet.role)}"
  frames:
${frames}
  framesPerSecond: 8
  facesRight: 0
  visualScale: 1
  levels:
${levels}
`;

  const fileName = `${pet.name}.asset`;
  fs.writeFileSync(path.join(outDir, fileName), asset);
  fs.writeFileSync(path.join(outDir, fileName + '.meta'),
`fileFormatVersion: 2
guid: ${md5('fairydata:' + pet.name)}
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`);
  console.log('asset', fileName);
}
```

> `m_Name: ${escapeUnicode(...)}`은 Unity가 한글 이름을 `\uXXXX`로 쓰는 방식과 같다. 에디터가 임포트하면서 표기를 다시 쓰면 그 결과를 커밋한다.

- [ ] **Step 2: 실행** — `node "$TEMP/gen-pet-assets.js" "$(pwd -W)"`. 기대: `asset <이름>.asset` 아홉 줄.

- [ ] **Step 3: 임포트 확인** — 에디터를 열어(또는 배치로 `-quit` 임포트 한 번) 에러 없이 임포트되는지, `Assets/_Project/Data/Fairies/`에서 아홉 에셋 각각 Frames 5칸이 채워지고 Levels 4칸 수치가 표와 맞는지 확인한다. Unity가 `.asset`/`.meta`를 다시 쓰면 그 결과를 커밋한다.

### Task 13: 데이터 구조 테스트

**Files:**
- Create: `unity/Assets/Tests/EditMode/FairyDataAssetTests.cs`

**Interfaces:**
- Consumes: `FairyData`, `AssetDatabase`

- [ ] **Step 1: 테스트 작성**(밸런스 수치는 assert하지 않는다 — 자주 바뀐다)

```csharp
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    /// <summary>펫 9종 데이터가 구조(프레임·레벨 수·이름)대로 들어갔는지 확인한다. 수치 자체는 밸런스로 바뀌므로 보지 않는다.</summary>
    public class FairyDataAssetTests
    {
        private const string Folder = "Assets/_Project/Data/Fairies";

        private static List<FairyData> LoadAll()
        {
            return AssetDatabase.FindAssets("t:FairyData", new[] { Folder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<FairyData>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToList();
        }

        [Test]
        public void Folder_HasNinePets()
        {
            Assert.AreEqual(9, LoadAll().Count);
        }

        [Test]
        public void EveryPet_HasNameAndRole()
        {
            foreach (FairyData data in LoadAll())
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(data.displayName), data.name);
                Assert.IsFalse(string.IsNullOrWhiteSpace(data.roleLine), data.name);
            }
        }

        [Test]
        public void EveryPet_HasFiveFramesWithoutGaps()
        {
            foreach (FairyData data in LoadAll())
            {
                Assert.AreEqual(5, data.frames.Length, data.name);
                foreach (var frame in data.frames)
                    Assert.IsNotNull(frame, $"{data.name}에 빈 프레임이 있습니다.");
            }
        }

        [Test]
        public void EveryPet_HasFourLevelsWithPositiveStats()
        {
            foreach (FairyData data in LoadAll())
            {
                Assert.AreEqual(4, data.levels.Length, data.name);
                foreach (var level in data.levels)
                {
                    Assert.Greater(level.damage, 0f, data.name);
                    Assert.Greater(level.cooldown, 0f, data.name);
                    Assert.Greater(level.range, 0f, data.name);
                }
            }
        }

        [Test]
        public void PetNames_AreUnique()
        {
            List<FairyData> all = LoadAll();

            Assert.AreEqual(all.Count, all.Select(d => d.displayName).Distinct().Count());
        }

        [Test]
        public void Stats_GrowAcrossLevels()
        {
            foreach (FairyData data in LoadAll())
            {
                Assert.Greater(data.levels[3].damage, data.levels[0].damage, data.name);
                Assert.Less(data.levels[3].cooldown, data.levels[0].cooldown, data.name);
            }
        }
    }
}
```

- [ ] **Step 2: 배치 테스트** — 컴파일 에러 0, 신규 6개 통과.

- [ ] **Step 3: 커밋·PR**

```bash
git add unity/Assets/_Project/Data/Fairies unity/Assets/_Project/Data/Fairies.meta unity/Assets/Tests/EditMode/FairyDataAssetTests.cs unity/Assets/Tests/EditMode/FairyDataAssetTests.cs.meta
git commit -m "feat: 펫 9종 데이터 에셋과 구조 테스트"
git push -u origin feature/pet-data-assets
gh pr create --base main --title "feat: 펫 9종 FairyData 에셋" --body "<요약·테스트 결과·역할 표>"
```

PR 본문: 역할 표(스펙 3장)와 성장 규칙, 수치는 제안값이라 인스펙터에서 조정한다는 점.

---

## PR E — 씬 배선 (사용자 에디터 작업)

C와 D가 머지된 뒤 사용자가 `GameScene`과 `BossScene`에 각각 한다.

- [ ] `FairySystem` 오브젝트의 `FairyController` → **Catalog** Size를 9로 두고 `Assets/_Project/Data/Fairies/`의 에셋 9개를 이 순서로 끌어다 놓는다: 계란초밥, 간장새우, 유부초밥, 참치초밥, 연어초밥, 광어초밥, 문어초밥, 새우초밥, 장어초밥. **두 씬의 순서가 같아야 한다**(보스 씬 이월이 번호로 복원한다).
- [ ] 기존 `FairyWeaponData` 에셋과 옛 Fairy Data 필드는 더 이상 쓰이지 않는다(지워도 되고 두어도 된다).
- [ ] 선택: 펫 아트 크기가 작으면 에셋의 **Visual Scale**을 키운다(25px = 0.25유닛).

**플레이 확인**
- 와사비에 성공하면 카드 3장이 모두 서로 다른 펫의 "소환" 카드로 뜬다(이름·아이콘·역할·수치 표시).
- 하나를 고르면 그 펫이 머리 위에서 프레임 애니메이션으로 둥실 떠 있고, 움직이는 방향에 맞춰 좌우가 뒤집힌다(방향이 거꾸로면 에셋의 **Faces Right**를 켠다).
- 다시 성공하면 이미 가진 펫은 카드에 안 나오고, 3마리가 차면 강화 카드만 나온다.
- 보스 씬으로 넘어가도 같은 종류·레벨이 따라온다.
- 펫마다 쏘는 템포와 사거리가 다르게 느껴진다.

**커밋 전 확인**: `git status`로 예상 밖 파일, `git diff`로 임시값(`winChance` 등)이 섞이지 않았는지. 씬 변경은 `GameScene.unity`가 협업자 소유라 PR 본문에 명시한다.

---

## Self-Review

- **스펙 커버리지:** 확정 결정 표 — 아트 9종·5프레임(Task 5, 12), 최대 3마리·중복 불가(Task 6, 8), 소환 랜덤 3종(Task 6), 강화는 가득 찬 뒤 별도(Task 6 `NotFull_NeverMixesUpgradesIn`·`Full_*`), 성능 차이(Task 12 표·에셋), 수치 정하기(표는 스펙과 동일, 인스펙터 조정), 투사체 그대로(`Fairy` 미변경), 아트 가져오기 자동화(Task 5, 12). 구성 표의 모든 파일에 Task가 있다(`FairyData` 1, `FrameAnimLogic` 2, `FairyFacingLogic` 3, `FairyChoiceLogic` 6, `Fairy` 7, `FairyController` 8, `FairyOption` 9, 이월 10, 아트 5, 에셋 12). 스펙 4장의 `FairyDescriptionLogic`은 스펙에 파일 표로는 없었으나 카드 설명(5장 "카드")을 테스트 가능하게 하려고 Task 4로 추가했다 — PR A에 포함. 스펙 테스트 절의 `FairyDataAssetTests`는 Task 13.
- **플레이스홀더 점검:** TBD·"적절히 처리" 류 없음. 모든 코드 단계에 코드 있음. 단 Task 5의 `.meta` 생성은 템플릿 기반 추정이라 Step 5 임포트 검증과 수동 슬라이스 대체 경로를 명시했다.
- **타입·이름 일관성:** `FairyChoice.KindIndex`(Task 6) → `FairyOption`(9)의 `_choice.KindIndex`. `FairyController.Summon(int)`·`Restore(kinds, levels)`(8) → `FairyOption.Apply`(9)·`LevelSystem.RestoreFairies(kinds, levels)`(10). `Fairy.Initialize`의 인자(7) = `FairyController.Summon`의 호출(8). `FairyDescriptionLogic.DescribeSummon(string, WeaponLevelStats)`(4) = 컨트롤러 호출(8). `FrameAnimLogic.FrameIndex`·`FairyFacingLogic.FlipX` 시그니처(2, 3) = `Fairy.ApplyVisual`(7). 카탈로그 순서(Task 12) = E의 연결 순서.
- **실행자 주의:** Task 6~10은 서로 컴파일 의존이라 중간 컴파일 금지, Task 11에서 한 번에 검증. `FairyController.Levels`/`Kinds`는 내부 리스트라 보관하려면 복사(`new List<int>(...)`). 카탈로그가 비어도 게임은 깨지지 않는다(스탯 버프 대체 보상).
