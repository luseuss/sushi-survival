# 슬라이스 2d: 무한 타일 맵 + 적 뭉침 방지·넉백 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 카메라를 따라 무한히 이어지는 타일 바닥을 깔고, 적이 서로 겹치지 않게
밀어내며 피격 시 뒤로 밀리도록 만든다.

**Architecture:** 타일은 좌표 해시로 결정론적으로 고르고, Unity Tilemap에
청크 단위로 채웠다 비운다. 적 이동은 `EnemyAI` 한 곳에서 추격·분리·넉백 세 벡터를
합쳐 한 번만 수행한다. 해시·타일 선택·청크 계산·분리·넉백 같은 순수 로직은
EditMode 유닛 테스트로 TDD하고, MonoBehaviour 통합 동작은 Play 모드 수동 테스트로
확인한다.

**Tech Stack:** Unity 2022.3.62f3 LTS, Built-in 2D, `com.unity.modules.tilemap`(설치됨),
Input System, uGUI, Unity Test Framework(EditMode, NUnit)

**Spec:** [docs/superpowers/specs/2026-08-21-slice2d-map-and-enemy-feel-design.md](../specs/2026-08-21-slice2d-map-and-enemy-feel-design.md)

## Global Constraints

- Unity 버전: 2022.3.62f3 고정. 입력은 새 Input System만. 렌더는 Built-in.
- 타일 스프라이트는 32×32px, PPU 100 → **0.32 월드 유닛**.
  Grid의 `Cell Size`도 반드시 **0.32**로 맞춘다.
- 기본 잔디는 **`바닥 테두리 x.png`**(테두리 없는 16종)를 쓴다.
  `바닥.png`(테두리 있는 쪽)는 이번에 쓰지 않는다.
- 유적은 **3×3 한 덩어리**로 이어지므로 패치 단위로 배치한다. 사막은 낱개.
- 적은 **Kinematic Rigidbody2D + `Use Full Kinematic Contacts`** 설정을 유지한다
  (슬라이스 1에서 접촉 데미지 버그를 겪으며 맞춘 설정이다. Dynamic으로 바꾸지 않는다).
- **`MovePosition`을 호출하는 곳은 `EnemyAI` 한 곳뿐이어야 한다.** 둘 이상이면
  서로 덮어쓴다.
- **0으로 나누기 방어 필수.** 두 적이 정확히 겹치거나 공격자와 적이 정확히 겹치면
  방향 계산에서 NaN이 나오고, 한 번 NaN이 되면 그 적의 위치가 영원히 망가진다.
  이런 경우 영벡터를 돌려준다.
- 수치 초기값(전부 인스펙터 노출): 청크 16×16, 유지 반경 2청크, 사막 8%, 유적 4%,
  분리 반경 0.6, 분리 세기 1.5, 분리 갱신 0.1초, 넉백 세기 3, 넉백 감쇠 12,
  넉백 저항 잡몹 0 / 중형몹 0.7.
- **Unity Editor가 열려 있으면 배치 모드 명령이 실패한다.** 코드 Task는 닫고,
  Editor Task는 열고 진행한다.
- **테스트 실행 명령에 `-quit`을 붙이지 않는다.** 표준 명령:

```bash
"/c/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" \
  -batchmode -nographics \
  -projectPath "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity" \
  -runTests -testPlatform EditMode \
  -testResults "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity/TestResults.xml" \
  -logFile "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity_test.log"
```

  결과 확인:

```bash
grep -oE '<test-run[^>]*' "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity/TestResults.xml"
grep -iE "error CS" "/c/Users/wnsdn/Desktop/와사비를 먹으면 강해지는 군요/unity_test.log" | head -10
```

- 이 플랜 시작 시점의 기존 테스트는 **118개**다. 각 Task 후 총계가 줄지 않아야 한다.

---

## Phase 1 — 준비

### Task 1: Unity Editor 작업 — 카메라 전환과 타일 스프라이트 슬라이스

Unity를 열고 진행한다. 코드 변경이 없는 준비 단계다.

- [ ] **Step 1: 카메라를 Orthographic으로 전환**

Hierarchy에서 `Main Camera` 선택 → Inspector의 `Camera` 컴포넌트:
- **`Projection`을 `Perspective` → `Orthographic`으로 변경**
- `Size` = **5**

지금까지 원근 투영이라 화면 가장자리로 갈수록 스프라이트가 미묘하게 기울어져
있었다. 타일을 격자로 깔면 이 왜곡이 눈에 띄게 되므로 먼저 고친다.

Play해서 캐릭터와 적이 이전과 비슷한 크기로 보이는지 확인한다. 크기가 많이
달라졌으면 `Size`를 조정한다(값이 작을수록 확대된다).

- [ ] **Step 2: 잔디 타일 슬라이스**

`Assets/Art/환경/환경/바닥 테두리 x.png` 선택 → Inspector:
- `Texture Type` = `Sprite (2D and UI)`
- `Sprite Mode` = **`Multiple`**
- `Pixels Per Unit` = **100**
- `Filter Mode` = **`Point (no filter)`** ← 픽셀아트가 뭉개지지 않게
- `Compression` = **`None`**
- **Apply**

그다음 `Sprite Editor` 열기 → `Slice` → `Grid By Cell Size`,
`Pixel Size` = **32 × 32** → `Slice` → **Apply**.
16개 프레임이 나와야 한다.

- [ ] **Step 3: 사막 타일 슬라이스**

`사막 바닥.png`에 대해 Step 2와 동일하게 설정하고 32×32로 슬라이스한다.
**4개** 프레임이 나와야 한다.

- [ ] **Step 4: 유적 타일 슬라이스**

`유적 바닥 (깨짐).png`에 대해 Step 2와 동일하게 설정하고 32×32로 슬라이스한다.
**9개** 프레임이 나와야 한다.

슬라이스된 프레임의 순서를 확인해 둔다. Unity의 자동 이름은 보통
`유적 바닥 (깨짐)_0` ~ `_8`이며 **좌측 상단부터 오른쪽으로, 그다음 아래줄**
순서다. Task 6에서 이 순서대로 배열에 넣는다.

- [ ] **Step 5: Tilemap 오브젝트 만들기**

1. Hierarchy 빈 곳 우클릭 → `2D Object` → `Tilemap` → `Rectangular`
   - `Grid` 오브젝트와 그 자식 `Tilemap`이 함께 생성된다
2. **`Grid` 오브젝트 선택** → `Grid` 컴포넌트의 `Cell Size`를
   **X = 0.32, Y = 0.32, Z = 0**으로 변경
   (32px ÷ PPU 100 = 0.32. 이 값이 안 맞으면 타일 사이에 틈이 생기거나 겹친다)
3. **`Tilemap` 자식 선택** → `Tilemap Renderer` 컴포넌트:
   - `Order in Layer`를 **-10**으로 (캐릭터·적보다 뒤에 그려지도록)

---

## Phase 2 — 무한 타일 맵

### Task 2: TileHash 결정론적 해시 (TDD)

**Files:**
- Create: `unity/Assets/_Project/Scripts/World/TileHash.cs`
- Test: `unity/Assets/Tests/EditMode/TileHashTests.cs`

**Interfaces:**
- Produces: `TileHash.Hash(int x, int y, int seed) -> uint`,
  `TileHash.Normalized(int x, int y, int seed) -> float` (0~1),
  `TileHash.Index(int x, int y, int seed, int count) -> int` (0~count-1).
  Task 3(`TilePicker`)이 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/TileHashTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.World;

namespace SushiSurvival.EditModeTests
{
    public class TileHashTests
    {
        [Test]
        public void Hash_IsDeterministic_ForSameInput()
        {
            uint first = TileHash.Hash(12, -7, 999);
            uint second = TileHash.Hash(12, -7, 999);

            Assert.AreEqual(first, second);
        }

        [Test]
        public void Hash_DiffersForNeighbouringCoordinates()
        {
            // 인접 좌표가 같은 값이면 바닥이 줄무늬처럼 보인다.
            uint a = TileHash.Hash(10, 10, 1);
            uint b = TileHash.Hash(11, 10, 1);
            uint c = TileHash.Hash(10, 11, 1);

            Assert.AreNotEqual(a, b);
            Assert.AreNotEqual(a, c);
            Assert.AreNotEqual(b, c);
        }

        [Test]
        public void Hash_DiffersForDifferentSeeds()
        {
            Assert.AreNotEqual(TileHash.Hash(5, 5, 1), TileHash.Hash(5, 5, 2));
        }

        [Test]
        public void Hash_HandlesNegativeCoordinates()
        {
            // 플레이어가 원점 왼쪽·아래로 가면 음수 좌표가 나온다.
            Assert.DoesNotThrow(() => TileHash.Hash(-1000, -1000, 7));
            Assert.AreEqual(TileHash.Hash(-3, -4, 7), TileHash.Hash(-3, -4, 7));
        }

        [Test]
        public void Normalized_StaysWithinZeroToOne()
        {
            for (int x = -50; x <= 50; x += 7)
            {
                float value = TileHash.Normalized(x, x * 3, 42);
                Assert.GreaterOrEqual(value, 0f);
                Assert.LessOrEqual(value, 1f);
            }
        }

        [Test]
        public void Index_StaysWithinRange()
        {
            for (int x = -50; x <= 50; x += 3)
            {
                int index = TileHash.Index(x, -x, 42, 16);
                Assert.GreaterOrEqual(index, 0);
                Assert.Less(index, 16);
            }
        }

        [Test]
        public void Index_ReturnsZero_WhenCountIsZero()
        {
            // 스프라이트 배열이 비어 있어도 0으로 나누지 않는다.
            Assert.AreEqual(0, TileHash.Index(3, 4, 5, 0));
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Global Constraints의 표준 명령 실행.
Expected: `TileHash`가 없어서 컴파일 실패.

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/World/TileHash.cs`:

```csharp
namespace SushiSurvival.World
{
    /// <summary>
    /// 좌표를 결정론적으로 해시한다. 같은 좌표는 언제나 같은 값을 내놓아야
    /// 청크를 버렸다 다시 만들어도 바닥이 그대로 유지된다.
    /// </summary>
    public static class TileHash
    {
        public static uint Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)seed;
                h ^= (uint)x * 2654435761u;
                h = (h << 13) | (h >> 19);
                h ^= (uint)y * 2246822519u;
                h = (h << 17) | (h >> 15);
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return h;
            }
        }

        /// <summary>0~1 범위로 정규화한 값.</summary>
        public static float Normalized(int x, int y, int seed)
            => Hash(x, y, seed) / (float)uint.MaxValue;

        /// <summary>0~count-1 범위의 인덱스. count가 0 이하면 0을 돌려준다.</summary>
        public static int Index(int x, int y, int seed, int count)
            => count <= 0 ? 0 : (int)(Hash(x, y, seed) % (uint)count);
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Expected: 총계 **125개** 전부 통과.

---

### Task 3: TilePicker 타일 선택 (TDD)

**Files:**
- Create: `unity/Assets/_Project/Scripts/World/TilePicker.cs`
- Test: `unity/Assets/Tests/EditMode/TilePickerTests.cs`

**Interfaces:**
- Consumes: `TileHash` (Task 2).
- Produces: `enum TileKind { Grass, Sand, Ruin }`,
  `struct TileChoice { TileKind Kind; int Index; }`,
  `struct TileMixConfig { int grassCount; int sandCount; int ruinSize; float sandChance; float ruinChance; }`,
  `TilePicker.Pick(int x, int y, int seed, TileMixConfig config) -> TileChoice`.
  Task 5(`TileMapStreamer`)가 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/TilePickerTests.cs`:

```csharp
using NUnit.Framework;
using SushiSurvival.World;

namespace SushiSurvival.EditModeTests
{
    public class TilePickerTests
    {
        private static TileMixConfig Config(float sandChance, float ruinChance)
        {
            return new TileMixConfig
            {
                grassCount = 16,
                sandCount = 4,
                ruinSize = 3,
                sandChance = sandChance,
                ruinChance = ruinChance
            };
        }

        [Test]
        public void Pick_IsDeterministic_ForSameCoordinate()
        {
            var config = Config(0.08f, 0.04f);

            TileChoice first = TilePicker.Pick(17, -23, 5, config);
            TileChoice second = TilePicker.Pick(17, -23, 5, config);

            Assert.AreEqual(first.Kind, second.Kind);
            Assert.AreEqual(first.Index, second.Index);
        }

        [Test]
        public void Pick_ReturnsGrass_WhenNoSandOrRuin()
        {
            var config = Config(0f, 0f);

            for (int x = 0; x < 20; x++)
                Assert.AreEqual(TileKind.Grass, TilePicker.Pick(x, x * 2, 1, config).Kind);
        }

        [Test]
        public void Pick_ReturnsSand_WhenSandChanceIsCertain()
        {
            var config = Config(1f, 0f);

            Assert.AreEqual(TileKind.Sand, TilePicker.Pick(4, 9, 1, config).Kind);
        }

        [Test]
        public void Pick_ReturnsRuin_WhenRuinChanceIsCertain()
        {
            var config = Config(0f, 1f);

            Assert.AreEqual(TileKind.Ruin, TilePicker.Pick(4, 9, 1, config).Kind);
        }

        [Test]
        public void Pick_RuinBeatsSand_WhenBothCertain()
        {
            // 유적은 이어진 구조물이라 사막에 잘려서는 안 된다.
            var config = Config(1f, 1f);

            Assert.AreEqual(TileKind.Ruin, TilePicker.Pick(4, 9, 1, config).Kind);
        }

        [Test]
        public void Pick_RuinPatchCoversThreeByThree_WithDistinctIndices()
        {
            var config = Config(0f, 1f);
            var seen = new System.Collections.Generic.HashSet<int>();

            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    TileChoice choice = TilePicker.Pick(x, y, 1, config);
                    Assert.AreEqual(TileKind.Ruin, choice.Kind);
                    seen.Add(choice.Index);
                }
            }

            // 9칸이 서로 다른 조각이어야 구조물로 이어져 보인다.
            Assert.AreEqual(9, seen.Count);
        }

        [Test]
        public void Pick_RuinPatchAlignsOnNegativeCoordinates()
        {
            // 음수 좌표에서 패치 격자가 어긋나면 유적이 잘려 보인다.
            var config = Config(0f, 1f);
            var seen = new System.Collections.Generic.HashSet<int>();

            for (int y = -3; y < 0; y++)
                for (int x = -3; x < 0; x++)
                    seen.Add(TilePicker.Pick(x, y, 1, config).Index);

            Assert.AreEqual(9, seen.Count);
        }

        [Test]
        public void Pick_GrassIndexStaysWithinSpriteCount()
        {
            var config = Config(0f, 0f);

            for (int x = -30; x <= 30; x++)
            {
                int index = TilePicker.Pick(x, x + 5, 3, config).Index;
                Assert.GreaterOrEqual(index, 0);
                Assert.Less(index, 16);
            }
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Expected: `TilePicker`가 없어서 컴파일 실패.

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/World/TilePicker.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.World
{
    public enum TileKind
    {
        Grass,
        Sand,
        Ruin
    }

    public struct TileChoice
    {
        public TileKind Kind;
        /// <summary>해당 종류의 스프라이트 배열 안에서의 인덱스.</summary>
        public int Index;
    }

    [System.Serializable]
    public struct TileMixConfig
    {
        public int grassCount;
        public int sandCount;
        [Tooltip("유적 패치 한 변의 타일 수. 아트가 3×3이므로 3.")]
        public int ruinSize;
        public float sandChance;
        public float ruinChance;
    }

    public static class TilePicker
    {
        // 종류마다 다른 해시 계열을 쓰기 위한 오프셋. 같은 시드로 여러 판정을
        // 할 때 값이 상관관계를 갖지 않도록 서로 다른 소수를 더한다.
        private const int RuinSeedOffset = 7919;
        private const int SandSeedOffset = 104729;
        private const int SandIndexSeedOffset = 15485863;

        public static TileChoice Pick(int x, int y, int seed, TileMixConfig config)
        {
            int ruinSize = config.ruinSize > 0 ? config.ruinSize : 1;

            // 유적은 3×3이 이어진 하나의 구조물이므로 패치 격자 단위로 판정한다.
            int patchX = FloorDiv(x, ruinSize);
            int patchY = FloorDiv(y, ruinSize);

            if (TileHash.Normalized(patchX, patchY, seed + RuinSeedOffset) < config.ruinChance)
            {
                int localX = Mod(x, ruinSize);
                int localY = Mod(y, ruinSize);
                return new TileChoice
                {
                    Kind = TileKind.Ruin,
                    Index = localY * ruinSize + localX
                };
            }

            if (TileHash.Normalized(x, y, seed + SandSeedOffset) < config.sandChance)
            {
                return new TileChoice
                {
                    Kind = TileKind.Sand,
                    Index = TileHash.Index(x, y, seed + SandIndexSeedOffset, config.sandCount)
                };
            }

            return new TileChoice
            {
                Kind = TileKind.Grass,
                Index = TileHash.Index(x, y, seed, config.grassCount)
            };
        }

        /// <summary>음수에서도 아래로 내림하는 나눗셈. C#의 / 는 0쪽으로 자른다.</summary>
        private static int FloorDiv(int a, int b) => Mathf.FloorToInt(a / (float)b);

        /// <summary>항상 0 이상을 돌려주는 나머지.</summary>
        private static int Mod(int a, int b)
        {
            int r = a % b;
            return r < 0 ? r + b : r;
        }
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Expected: 총계 **133개** 전부 통과.

---

### Task 4: ChunkGrid 청크 계산 (TDD)

**Files:**
- Create: `unity/Assets/_Project/Scripts/World/ChunkGrid.cs`
- Test: `unity/Assets/Tests/EditMode/ChunkGridTests.cs`

**Interfaces:**
- Produces: `ChunkGrid.WorldToChunk(Vector2 worldPos, int chunkSize, float tileSize) -> Vector2Int`,
  `ChunkGrid.GetRequiredChunks(Vector2Int center, int radius) -> List<Vector2Int>`.
  Task 5(`TileMapStreamer`)가 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/ChunkGridTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using SushiSurvival.World;

namespace SushiSurvival.EditModeTests
{
    public class ChunkGridTests
    {
        [Test]
        public void WorldToChunk_ReturnsOrigin_AtWorldZero()
        {
            var result = ChunkGrid.WorldToChunk(Vector2.zero, 16, 0.32f);

            Assert.AreEqual(new Vector2Int(0, 0), result);
        }

        [Test]
        public void WorldToChunk_StaysInFirstChunk_WithinItsExtent()
        {
            // 청크 16타일 × 0.32유닛 = 5.12유닛
            var result = ChunkGrid.WorldToChunk(new Vector2(5f, 5f), 16, 0.32f);

            Assert.AreEqual(new Vector2Int(0, 0), result);
        }

        [Test]
        public void WorldToChunk_AdvancesToNextChunk_PastExtent()
        {
            var result = ChunkGrid.WorldToChunk(new Vector2(5.5f, 0f), 16, 0.32f);

            Assert.AreEqual(new Vector2Int(1, 0), result);
        }

        [Test]
        public void WorldToChunk_HandlesNegativeWorldPositions()
        {
            // 원점 왼쪽은 -1번 청크여야 한다. 0으로 잘리면 맵이 겹쳐 보인다.
            var result = ChunkGrid.WorldToChunk(new Vector2(-0.5f, -0.5f), 16, 0.32f);

            Assert.AreEqual(new Vector2Int(-1, -1), result);
        }

        [Test]
        public void GetRequiredChunks_ReturnsOnlyCenter_AtRadiusZero()
        {
            var result = ChunkGrid.GetRequiredChunks(new Vector2Int(3, -2), 0);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(new Vector2Int(3, -2), result[0]);
        }

        [Test]
        public void GetRequiredChunks_ReturnsNineChunks_AtRadiusOne()
        {
            var result = ChunkGrid.GetRequiredChunks(Vector2Int.zero, 1);

            Assert.AreEqual(9, result.Count);
        }

        [Test]
        public void GetRequiredChunks_IncludesCenterAndCorners_AtRadiusTwo()
        {
            var result = ChunkGrid.GetRequiredChunks(Vector2Int.zero, 2);

            Assert.AreEqual(25, result.Count);
            CollectionAssert.Contains(result, Vector2Int.zero);
            CollectionAssert.Contains(result, new Vector2Int(-2, -2));
            CollectionAssert.Contains(result, new Vector2Int(2, 2));
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Expected: `ChunkGrid`가 없어서 컴파일 실패.

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/World/ChunkGrid.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace SushiSurvival.World
{
    public static class ChunkGrid
    {
        public static Vector2Int WorldToChunk(Vector2 worldPos, int chunkSize, float tileSize)
        {
            if (chunkSize <= 0 || tileSize <= 0f) return Vector2Int.zero;

            int tileX = Mathf.FloorToInt(worldPos.x / tileSize);
            int tileY = Mathf.FloorToInt(worldPos.y / tileSize);

            return new Vector2Int(FloorDiv(tileX, chunkSize), FloorDiv(tileY, chunkSize));
        }

        public static List<Vector2Int> GetRequiredChunks(Vector2Int center, int radius)
        {
            var chunks = new List<Vector2Int>();

            for (int y = center.y - radius; y <= center.y + radius; y++)
                for (int x = center.x - radius; x <= center.x + radius; x++)
                    chunks.Add(new Vector2Int(x, y));

            return chunks;
        }

        private static int FloorDiv(int a, int b) => Mathf.FloorToInt(a / (float)b);
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Expected: 총계 **140개** 전부 통과.

---

### Task 5: TileMapStreamer

**Files:**
- Create: `unity/Assets/_Project/Scripts/World/TileMapStreamer.cs`

**Interfaces:**
- Consumes: `TilePicker.Pick` (Task 3), `ChunkGrid.WorldToChunk` /
  `GetRequiredChunks` (Task 4).
- Produces: `TileMapStreamer` (MonoBehaviour). Task 6에서 씬에 배치한다.

- [ ] **Step 1: TileMapStreamer 작성**

`unity/Assets/_Project/Scripts/World/TileMapStreamer.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SushiSurvival.World
{
    /// <summary>
    /// 카메라 주변으로 타일을 채우고 멀어진 영역은 비운다. 타일은 좌표 해시로
    /// 결정론적으로 고르므로, 같은 자리로 돌아오면 같은 바닥이 나온다.
    /// </summary>
    public class TileMapStreamer : MonoBehaviour
    {
        [SerializeField] private Tilemap tilemap;
        [Tooltip("따라갈 대상. 비워두면 메인 카메라를 따라간다.")]
        [SerializeField] private Transform followTarget;

        [Header("스프라이트")]
        [Tooltip("테두리 없는 잔디 16종.")]
        [SerializeField] private Sprite[] grassSprites;
        [Tooltip("사막 4종.")]
        [SerializeField] private Sprite[] sandSprites;
        [Tooltip("유적 9종. 좌측 상단부터 오른쪽으로, 그다음 아래줄 순서.")]
        [SerializeField] private Sprite[] ruinSprites;

        [Header("생성 규칙")]
        [Tooltip("타일 한 변의 월드 크기. Grid의 Cell Size와 반드시 같아야 한다.")]
        [SerializeField] private float tileSize = 0.32f;
        [SerializeField] private int chunkSize = 16;
        [Tooltip("중심 청크로부터 이 반경만큼 유지한다.")]
        [SerializeField] private int chunkRadius = 2;
        [SerializeField] private float sandChance = 0.08f;
        [SerializeField] private float ruinChance = 0.04f;

        [Header("시드")]
        [Tooltip("켜면 매 판 다른 맵이 나온다. 끄면 아래 시드로 고정된다.")]
        [SerializeField] private bool randomSeedEachRun = true;
        [SerializeField] private int seed = 12345;

        private readonly HashSet<Vector2Int> _loadedChunks = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> _toRemove = new List<Vector2Int>();

        private Tile[] _grassTiles;
        private Tile[] _sandTiles;
        private Tile[] _ruinTiles;

        private TileMixConfig _config;
        private int _activeSeed;
        private Vector2Int _lastCenterChunk;
        private bool _hasStreamedOnce;

        private void Awake()
        {
            if (tilemap == null)
            {
                Debug.LogError($"{name}: tilemap이 비어 있어 바닥을 그릴 수 없습니다.");
                enabled = false;
                return;
            }

            if (grassSprites == null || grassSprites.Length == 0)
            {
                Debug.LogError($"{name}: grassSprites가 비어 있어 바닥을 그릴 수 없습니다.");
                enabled = false;
                return;
            }

            if (followTarget == null && Camera.main != null)
                followTarget = Camera.main.transform;

            _activeSeed = randomSeedEachRun ? Random.Range(int.MinValue, int.MaxValue) : seed;

            _grassTiles = BuildTiles(grassSprites);
            _sandTiles = BuildTiles(sandSprites);
            _ruinTiles = BuildTiles(ruinSprites);

            _config = new TileMixConfig
            {
                grassCount = _grassTiles.Length,
                sandCount = _sandTiles.Length,
                ruinSize = 3,
                sandChance = _sandTiles.Length > 0 ? sandChance : 0f,
                ruinChance = _ruinTiles.Length >= 9 ? ruinChance : 0f
            };
        }

        private void LateUpdate()
        {
            if (followTarget == null) return;

            Vector2Int centerChunk = ChunkGrid.WorldToChunk(followTarget.position, chunkSize, tileSize);
            if (_hasStreamedOnce && centerChunk == _lastCenterChunk) return;

            Stream(centerChunk);

            _lastCenterChunk = centerChunk;
            _hasStreamedOnce = true;
        }

        private void Stream(Vector2Int centerChunk)
        {
            List<Vector2Int> required = ChunkGrid.GetRequiredChunks(centerChunk, chunkRadius);
            var requiredSet = new HashSet<Vector2Int>(required);

            _toRemove.Clear();
            foreach (Vector2Int loaded in _loadedChunks)
            {
                if (!requiredSet.Contains(loaded))
                    _toRemove.Add(loaded);
            }

            foreach (Vector2Int chunk in _toRemove)
            {
                ClearChunk(chunk);
                _loadedChunks.Remove(chunk);
            }

            foreach (Vector2Int chunk in required)
            {
                if (_loadedChunks.Add(chunk))
                    FillChunk(chunk);
            }
        }

        private void FillChunk(Vector2Int chunk)
        {
            int originX = chunk.x * chunkSize;
            int originY = chunk.y * chunkSize;

            var bounds = new BoundsInt(originX, originY, 0, chunkSize, chunkSize, 1);
            var tiles = new TileBase[chunkSize * chunkSize];

            for (int y = 0; y < chunkSize; y++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    TileChoice choice = TilePicker.Pick(originX + x, originY + y, _activeSeed, _config);
                    tiles[y * chunkSize + x] = ResolveTile(choice);
                }
            }

            tilemap.SetTilesBlock(bounds, tiles);
        }

        private void ClearChunk(Vector2Int chunk)
        {
            var bounds = new BoundsInt(chunk.x * chunkSize, chunk.y * chunkSize, 0, chunkSize, chunkSize, 1);
            tilemap.SetTilesBlock(bounds, new TileBase[chunkSize * chunkSize]);
        }

        private TileBase ResolveTile(TileChoice choice)
        {
            switch (choice.Kind)
            {
                case TileKind.Ruin:
                    return Pick(_ruinTiles, choice.Index);
                case TileKind.Sand:
                    return Pick(_sandTiles, choice.Index);
                default:
                    return Pick(_grassTiles, choice.Index);
            }
        }

        private Tile Pick(Tile[] tiles, int index)
        {
            if (tiles == null || tiles.Length == 0) return null;

            return tiles[Mathf.Clamp(index, 0, tiles.Length - 1)];
        }

        /// <summary>
        /// 스프라이트마다 Tile 에셋을 런타임에 만든다. 이렇게 하면 에디터용
        /// Tile Palette 패키지를 따로 설치하지 않아도 된다.
        /// </summary>
        private static Tile[] BuildTiles(Sprite[] sprites)
        {
            if (sprites == null) return new Tile[0];

            var tiles = new Tile[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprites[i];
                tiles[i] = tile;
            }

            return tiles;
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 컴파일 확인**

Expected: 총계 140개 통과, `error CS` 없음.

---

### Task 6: Unity Editor 작업 — Tilemap 배선과 확인

Unity를 열고 진행한다.

- [ ] **Step 1: TileMapStreamer 붙이기**

Task 1에서 만든 **`Tilemap` 자식 오브젝트**를 선택 → `Add Component` →
`Tile Map Streamer`

- [ ] **Step 2: 필드 채우기**

- `Tilemap` ← 자기 자신의 `Tilemap` 컴포넌트
- `Follow Target` ← **비워둔다** (메인 카메라를 자동으로 따라간다)
- `Grass Sprites` — 배열 크기 **16**. Project 창에서 `바닥 테두리 x.png`의
  **▶ 화살표를 눌러 펼치고**, 슬라이스된 16개 프레임을 순서대로 드래그
  (여러 개를 한 번에 선택해 배열 헤더에 드롭하면 한꺼번에 들어간다)
- `Sand Sprites` — 배열 크기 **4**, `사막 바닥` 프레임 4개
- `Ruin Sprites` — 배열 크기 **9**, `유적 바닥 (깨짐)` 프레임을
  **`_0` ~ `_8` 순서 그대로** (좌상단 → 오른쪽 → 아랫줄)
- `Tile Size` = **0.32** (Grid의 Cell Size와 같아야 한다)
- `Chunk Size` = 16, `Chunk Radius` = 2
- `Sand Chance` = 0.08, `Ruin Chance` = 0.04
- `Random Seed Each Run` = **체크**

- [ ] **Step 3: 기존 배경 스프라이트 제거**

슬라이스 1에서 배경으로 깔아둔 `바닥.png` 스프라이트 오브젝트가 씬에 있으면
**삭제하거나 비활성화**한다. 타일맵과 겹쳐 보인다.

- [ ] **Step 4: 플레이테스트**

1. 캐릭터를 고르면 바닥이 잔디 타일로 채워져 있다
2. **이동하면 바닥이 계속 이어진다** — 끝에 도달해 검은 영역이 보이지 않는다
3. 사막 타일이 드문드문, 유적이 **3×3 덩어리로 이어져** 나타난다
4. **왔던 자리로 되돌아가면 바닥이 그대로다** (타일이 바뀌지 않는다)
5. 타일 사이에 틈이나 겹침이 없다
   (있으면 `Tile Size`와 Grid `Cell Size`가 어긋난 것)
6. 캐릭터·적·젬이 타일 **위에** 그려진다
   (묻히면 `Tilemap Renderer`의 `Order in Layer`를 더 낮춘다)
7. 다시 하기로 새 판을 시작하면 **다른 맵**이 나온다
8. Console에 에러가 없다

---

## Phase 3 — 적 뭉침 방지 + 넉백

### Task 7: SeparationLogic (TDD)

**Files:**
- Create: `unity/Assets/_Project/Scripts/Enemies/SeparationLogic.cs`
- Test: `unity/Assets/Tests/EditMode/SeparationLogicTests.cs`

**Interfaces:**
- Produces: `SeparationLogic.ComputeSeparation(Vector2 self, IReadOnlyList<Vector2> neighbors, float radius) -> Vector2`
  (정규화된 방향 또는 영벡터).
  Task 10(`EnemyAI`)이 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/SeparationLogicTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Enemies;

namespace SushiSurvival.EditModeTests
{
    public class SeparationLogicTests
    {
        [Test]
        public void ComputeSeparation_ReturnsZero_WithNoNeighbors()
        {
            var result = SeparationLogic.ComputeSeparation(Vector2.zero, new List<Vector2>(), 0.6f);

            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void ComputeSeparation_PushesAwayFromNeighbor()
        {
            var neighbors = new List<Vector2> { new Vector2(0.3f, 0f) };

            var result = SeparationLogic.ComputeSeparation(Vector2.zero, neighbors, 0.6f);

            Assert.Less(result.x, 0f);
        }

        [Test]
        public void ComputeSeparation_IgnoresNeighborsBeyondRadius()
        {
            var neighbors = new List<Vector2> { new Vector2(5f, 0f) };

            var result = SeparationLogic.ComputeSeparation(Vector2.zero, neighbors, 0.6f);

            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void ComputeSeparation_ReturnsZero_WhenExactlyOverlapping()
        {
            // 0으로 나누면 NaN이 나오고, 한 번 NaN이 되면 적 위치가 영원히 망가진다.
            var neighbors = new List<Vector2> { Vector2.zero };

            var result = SeparationLogic.ComputeSeparation(Vector2.zero, neighbors, 0.6f);

            Assert.IsFalse(float.IsNaN(result.x));
            Assert.IsFalse(float.IsNaN(result.y));
            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void ComputeSeparation_CancelsOut_ForSymmetricNeighbors()
        {
            var neighbors = new List<Vector2> { new Vector2(0.3f, 0f), new Vector2(-0.3f, 0f) };

            var result = SeparationLogic.ComputeSeparation(Vector2.zero, neighbors, 0.6f);

            Assert.That(result.magnitude, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ComputeSeparation_ReturnsUnitLength_WhenPushed()
        {
            var neighbors = new List<Vector2> { new Vector2(0.1f, 0.1f) };

            var result = SeparationLogic.ComputeSeparation(Vector2.zero, neighbors, 0.6f);

            Assert.That(result.magnitude, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void ComputeSeparation_ReturnsZero_ForNonPositiveRadius()
        {
            var neighbors = new List<Vector2> { new Vector2(0.1f, 0f) };

            var result = SeparationLogic.ComputeSeparation(Vector2.zero, neighbors, 0f);

            Assert.AreEqual(Vector2.zero, result);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Expected: `SeparationLogic`이 없어서 컴파일 실패.

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/Enemies/SeparationLogic.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace SushiSurvival.Enemies
{
    public static class SeparationLogic
    {
        private const float MinDistanceSqr = 0.0001f;

        /// <summary>
        /// 주변 이웃에게서 멀어지는 방향을 돌려준다(정규화). 이웃이 없거나
        /// 힘이 상쇄되면 영벡터.
        /// </summary>
        public static Vector2 ComputeSeparation(Vector2 self, IReadOnlyList<Vector2> neighbors, float radius)
        {
            if (radius <= 0f || neighbors == null) return Vector2.zero;

            float radiusSqr = radius * radius;
            Vector2 push = Vector2.zero;

            foreach (Vector2 neighbor in neighbors)
            {
                Vector2 away = self - neighbor;
                float distSqr = away.sqrMagnitude;

                // 정확히 겹치면 방향을 정할 수 없다. 나누면 NaN이 되고
                // 한 번 NaN이 된 위치는 회복되지 않는다.
                if (distSqr < MinDistanceSqr) continue;
                if (distSqr > radiusSqr) continue;

                float dist = Mathf.Sqrt(distSqr);
                // 가까울수록 강하게 민다.
                push += (away / dist) * (1f - dist / radius);
            }

            return push.sqrMagnitude > MinDistanceSqr ? push.normalized : Vector2.zero;
        }
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Expected: 총계 **147개** 전부 통과.

---

### Task 8: KnockbackLogic (TDD)

**Files:**
- Create: `unity/Assets/_Project/Scripts/Enemies/KnockbackLogic.cs`
- Test: `unity/Assets/Tests/EditMode/KnockbackLogicTests.cs`

**Interfaces:**
- Produces: `KnockbackLogic.ComputeImpulse(Vector2 sourcePos, Vector2 targetPos, float force, float resistance) -> Vector2`,
  `KnockbackLogic.Decay(Vector2 velocity, float decayPerSecond, float deltaTime) -> Vector2`.
  Task 9(`EnemyBase`)가 사용한다.

- [ ] **Step 1: 실패하는 테스트 작성**

`unity/Assets/Tests/EditMode/KnockbackLogicTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Enemies;

namespace SushiSurvival.EditModeTests
{
    public class KnockbackLogicTests
    {
        [Test]
        public void ComputeImpulse_PushesAwayFromSource()
        {
            // 공격자가 왼쪽에 있으면 적은 오른쪽으로 밀린다.
            var result = KnockbackLogic.ComputeImpulse(new Vector2(-1f, 0f), Vector2.zero, 3f, 0f);

            Assert.Greater(result.x, 0f);
        }

        [Test]
        public void ComputeImpulse_UsesFullForce_WithoutResistance()
        {
            var result = KnockbackLogic.ComputeImpulse(new Vector2(-1f, 0f), Vector2.zero, 3f, 0f);

            Assert.That(result.magnitude, Is.EqualTo(3f).Within(0.0001f));
        }

        [Test]
        public void ComputeImpulse_HalvesForce_AtHalfResistance()
        {
            var result = KnockbackLogic.ComputeImpulse(new Vector2(-1f, 0f), Vector2.zero, 3f, 0.5f);

            Assert.That(result.magnitude, Is.EqualTo(1.5f).Within(0.0001f));
        }

        [Test]
        public void ComputeImpulse_ReturnsZero_AtFullResistance()
        {
            var result = KnockbackLogic.ComputeImpulse(new Vector2(-1f, 0f), Vector2.zero, 3f, 1f);

            Assert.That(result.magnitude, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ComputeImpulse_ClampsResistanceAboveOne()
        {
            var result = KnockbackLogic.ComputeImpulse(new Vector2(-1f, 0f), Vector2.zero, 3f, 5f);

            Assert.That(result.magnitude, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ComputeImpulse_ReturnsZero_WhenExactlyOverlapping()
        {
            var result = KnockbackLogic.ComputeImpulse(Vector2.zero, Vector2.zero, 3f, 0f);

            Assert.IsFalse(float.IsNaN(result.x));
            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void Decay_ReducesSpeedOverTime()
        {
            var result = KnockbackLogic.Decay(new Vector2(3f, 0f), 12f, 0.1f);

            Assert.That(result.magnitude, Is.EqualTo(1.8f).Within(0.0001f));
        }

        [Test]
        public void Decay_ReachesExactlyZero_AndNeverReverses()
        {
            var result = KnockbackLogic.Decay(new Vector2(1f, 0f), 12f, 1f);

            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void Decay_KeepsDirection()
        {
            var result = KnockbackLogic.Decay(new Vector2(0f, 3f), 12f, 0.1f);

            Assert.That(result.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.Greater(result.y, 0f);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 실패 확인**

Expected: `KnockbackLogic`이 없어서 컴파일 실패.

- [ ] **Step 3: 최소 구현 작성**

`unity/Assets/_Project/Scripts/Enemies/KnockbackLogic.cs`:

```csharp
using UnityEngine;

namespace SushiSurvival.Enemies
{
    public static class KnockbackLogic
    {
        private const float MinDistanceSqr = 0.0001f;

        /// <summary>
        /// 공격자 반대 방향으로 밀어내는 속도. 저항이 1이면 밀리지 않는다.
        /// 공격자와 적이 정확히 겹치면 방향을 정할 수 없으므로 영벡터.
        /// </summary>
        public static Vector2 ComputeImpulse(Vector2 sourcePos, Vector2 targetPos, float force, float resistance)
        {
            Vector2 away = targetPos - sourcePos;
            if (away.sqrMagnitude < MinDistanceSqr) return Vector2.zero;

            float effective = force * (1f - Mathf.Clamp01(resistance));
            return away.normalized * effective;
        }

        /// <summary>속도를 일정 비율로 줄인다. 0 아래로 내려가 반대로 튀지 않는다.</summary>
        public static Vector2 Decay(Vector2 velocity, float decayPerSecond, float deltaTime)
        {
            float speed = velocity.magnitude;
            if (speed <= 0f) return Vector2.zero;

            float reduced = Mathf.Max(0f, speed - decayPerSecond * deltaTime);
            return reduced <= 0f ? Vector2.zero : velocity / speed * reduced;
        }
    }
}
```

- [ ] **Step 4: 테스트 재실행해서 통과 확인**

Expected: 총계 **156개** 전부 통과.

---

### Task 9: MonsterData 확장 + TakeDamage 시그니처 변경

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Data/MonsterData.cs`
- Modify: `unity/Assets/_Project/Scripts/Enemies/EnemyBase.cs`
- Modify: `unity/Assets/_Project/Scripts/Weapons/EggFanWeapon.cs`
- Modify: `unity/Assets/_Project/Scripts/Weapons/Projectile.cs`

**Interfaces:**
- Consumes: `KnockbackLogic.ComputeImpulse` / `Decay` (Task 8).
- Produces: `MonsterData.knockbackResistance` (float),
  `EnemyBase.TakeDamage(float damage, Vector2 sourcePosition)` (기존 1인자 버전 대체),
  `EnemyBase.KnockbackVelocity` (Vector2 프로퍼티).
  Task 10(`EnemyAI`)이 `KnockbackVelocity`를 읽는다.

- [ ] **Step 1: MonsterData에 넉백 저항 추가**

`unity/Assets/_Project/Scripts/Data/MonsterData.cs`의
`public XPGemType xpGemDrop = XPGemType.Basic;` 줄 **바로 위**에 추가:

```csharp
        [Range(0f, 1f)]
        [Tooltip("피격 시 밀려나는 정도를 줄인다. 0이면 그대로 밀리고 1이면 꿈쩍도 않는다. 중형몹·보스는 높게.")]
        public float knockbackResistance;
```

- [ ] **Step 2: EnemyBase에 넉백 상태 추가**

`unity/Assets/_Project/Scripts/Enemies/EnemyBase.cs`에서 네 곳을 고친다.

필드 추가 — `[SerializeField] private MonsterData monsterData;` 줄 **바로 아래**:

```csharp
        [Tooltip("피격 시 밀려나는 초기 속도.")]
        [SerializeField] private float knockbackForce = 3f;
        [Tooltip("넉백 속도가 초당 이만큼씩 줄어든다.")]
        [SerializeField] private float knockbackDecay = 12f;
```

프로퍼티 추가 — `public float CurrentHealth { get; private set; }` 줄 **바로 아래**:

```csharp
        /// <summary>EnemyAI가 이동에 더한다. 여기서 직접 위치를 옮기지 않는다.</summary>
        public Vector2 KnockbackVelocity { get; private set; }
```

`OnEnable`에서 넉백을 초기화한다. 풀에서 재사용되므로 이전 판의 값이 남으면
새로 나온 적이 갑자기 튄다. `_contactTimer = 0f;` 줄 **바로 아래**에 추가:

```csharp
            KnockbackVelocity = Vector2.zero;
```

`Update`를 아래로 교체(기존은 `private void Update() => _contactTimer -= Time.deltaTime;`):

```csharp
        private void Update()
        {
            _contactTimer -= Time.deltaTime;
            KnockbackVelocity = KnockbackLogic.Decay(KnockbackVelocity, knockbackDecay, Time.deltaTime);
        }
```

- [ ] **Step 3: TakeDamage가 공격자 위치를 받도록 변경**

같은 파일의 `TakeDamage` 메서드 전체를 아래로 교체:

```csharp
        public void TakeDamage(float damage, Vector2 sourcePosition)
        {
            if (HealthLogic.IsDead(CurrentHealth)) return;

            KnockbackVelocity += KnockbackLogic.ComputeImpulse(
                sourcePosition, transform.position, knockbackForce, monsterData.knockbackResistance);

            CurrentHealth = HealthLogic.ApplyDamage(CurrentHealth, damage);

            if (HealthLogic.IsDead(CurrentHealth))
                Die();
        }
```

- [ ] **Step 4: 호출처 두 곳 수정**

`unity/Assets/_Project/Scripts/Weapons/EggFanWeapon.cs`에서
`enemy.TakeDamage(Damage);`를 아래로 교체:

```csharp
                    enemy.TakeDamage(Damage, transform.position);
```

`unity/Assets/_Project/Scripts/Weapons/Projectile.cs`에서
`enemy.TakeDamage(_damage);`를 아래로 교체:

```csharp
            enemy.TakeDamage(_damage, transform.position);
```

- [ ] **Step 5: 테스트 실행해서 컴파일 확인**

Expected: 총계 156개 통과, `error CS` 없음.

---

### Task 10: EnemyAI에 분리·넉백 통합

**Files:**
- Modify: `unity/Assets/_Project/Scripts/Enemies/EnemyAI.cs` (전면 교체)

**Interfaces:**
- Consumes: `SeparationLogic.ComputeSeparation` (Task 7),
  `EnemyBase.KnockbackVelocity` (Task 9).

- [ ] **Step 1: EnemyAI 전면 교체**

`unity/Assets/_Project/Scripts/Enemies/EnemyAI.cs` 전체를 아래로 교체:

```csharp
using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Data;

namespace SushiSurvival.Enemies
{
    /// <summary>
    /// 적의 유일한 이동 주체. 추격·분리·넉백 세 벡터를 합쳐 한 번만 움직인다.
    /// MovePosition을 부르는 곳이 둘 이상이면 서로 덮어쓰므로 여기서만 호출한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyAI : MonoBehaviour
    {
        private const int MaxNeighbors = 8;

        // FixedUpdate는 단일 스레드이고 결과를 즉시 소비하므로 공유해도 안전하다.
        private static readonly Collider2D[] NeighborBuffer = new Collider2D[MaxNeighbors];

        [SerializeField] private MonsterData monsterData;
        [Tooltip("다른 적을 찾을 레이어. Enemy 레이어를 지정한다.")]
        [SerializeField] private LayerMask enemyLayer;
        [Tooltip("이 반경 안의 다른 적에게서 밀려난다.")]
        [SerializeField] private float separationRadius = 0.6f;
        [Tooltip("이동속도 대비 분리 힘의 배율.")]
        [SerializeField] private float separationStrength = 1.5f;
        [Tooltip("분리 벡터 갱신 주기(초). 매 프레임 물리 쿼리를 돌리지 않기 위함.")]
        [SerializeField] private float separationInterval = 0.1f;

        private readonly List<Vector2> _neighborPositions = new List<Vector2>(MaxNeighbors);

        private Rigidbody2D _rigidbody;
        private EnemyBase _enemy;
        private Transform _target;
        private Vector2 _separation;
        private float _separationTimer;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _enemy = GetComponent<EnemyBase>();
        }

        private void OnEnable()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            _target = playerObj != null ? playerObj.transform : null;

            // 풀에서 재사용되므로 이전 판의 상태를 지운다.
            _separation = Vector2.zero;
            _separationTimer = 0f;
        }

        private void FixedUpdate()
        {
            if (_target == null) return;

            UpdateSeparation();

            Vector2 chase = ((Vector2)_target.position - _rigidbody.position).normalized * monsterData.moveSpeed;
            Vector2 separation = _separation * (monsterData.moveSpeed * separationStrength);
            Vector2 knockback = _enemy != null ? _enemy.KnockbackVelocity : Vector2.zero;

            Vector2 move = (chase + separation + knockback) * Time.fixedDeltaTime;
            _rigidbody.MovePosition(_rigidbody.position + move);
        }

        private void UpdateSeparation()
        {
            _separationTimer -= Time.fixedDeltaTime;
            if (_separationTimer > 0f) return;

            _separationTimer = separationInterval;

            int count = Physics2D.OverlapCircleNonAlloc(
                _rigidbody.position, separationRadius, NeighborBuffer, enemyLayer);

            _neighborPositions.Clear();
            for (int i = 0; i < count; i++)
            {
                Collider2D other = NeighborBuffer[i];
                if (other == null) continue;
                // 자기 자신은 제외한다.
                if (other.attachedRigidbody == _rigidbody) continue;

                _neighborPositions.Add(other.transform.position);
            }

            _separation = SeparationLogic.ComputeSeparation(
                _rigidbody.position, _neighborPositions, separationRadius);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행해서 컴파일 확인**

Expected: 총계 156개 통과, `error CS` 없음.

---

### Task 11: Unity Editor 작업 — 수치 설정과 최종 플레이테스트

Unity를 열고 진행한다.

- [ ] **Step 1: 몬스터 데이터에 넉백 저항 설정**

- `BasicMobData` → `Knockback Resistance` = **0**
- `CaliforniaRollData` → `Knockback Resistance` = **0**
- `MidBossData` → `Knockback Resistance` = **0.7**

- [ ] **Step 2: 적 프리팹의 새 필드 채우기**

`BasicMob`, `CaliforniaRoll`, `MidBoss` **세 프리팹 각각**에 대해:

`EnemyBase` 컴포넌트:
- `Knockback Force` = **3**
- `Knockback Decay` = **12**

`EnemyAI` 컴포넌트:
- **`Enemy Layer` ← `Enemy` 레이어만 체크** ⚠️ 이걸 비워두면 분리가 전혀
  동작하지 않는다(아무 이웃도 못 찾는다)
- `Separation Radius` = **0.6** (중형몹은 몸집이 2배이므로 **1.2**)
- `Separation Strength` = **1.5**
- `Separation Interval` = **0.1**

- [ ] **Step 3: 플레이테스트 — 뭉침 방지**

1. 잡몹이 여러 마리 몰려와도 **서로 겹치지 않고 간격을 유지**한다
2. 한 덩어리로 뭉치지 않고 플레이어를 둘러싸는 모양이 된다
3. 적이 부들부들 떨거나 제자리에서 튀지 않는다
   (떨리면 `Separation Strength`를 낮춘다)
4. 적이 플레이어에게 도달은 한다
   (너무 밀려나 접근을 못 하면 `Separation Strength`를 낮춘다)

- [ ] **Step 4: 플레이테스트 — 넉백**

5. 계란 양산으로 때리면 잡몹이 **바깥쪽으로 살짝 밀린다**
6. 간장새우 총알에 맞아도 밀린다
7. **중형몹은 눈에 띄게 덜 밀린다**
8. 밀린 뒤 곧 원래 속도로 다시 다가온다
   (너무 오래 밀려 있으면 `Knockback Decay`를 올린다)
9. 적이 밀려서 화면 밖으로 날아가지 않는다
   (날아가면 `Knockback Force`를 낮춘다)

- [ ] **Step 5: 플레이테스트 — 전체 회귀**

10. 접촉 데미지가 여전히 들어온다(적에게 닿으면 체력이 줄어든다)
11. 적이 죽으면 사라지고 젬을 떨어뜨린다
12. 중형몹이 2:00·4:00에 등장하고 황금 젬을 떨군다
13. 바닥 타일이 이동에 따라 계속 이어진다
14. 결과 화면과 다시 하기가 정상 동작한다
15. 적이 많을 때(30마리 이상) 프레임이 눈에 띄게 떨어지지 않는다
    (떨어지면 `Separation Interval`을 0.15~0.2로 올린다)
16. Console에 에러가 없다
