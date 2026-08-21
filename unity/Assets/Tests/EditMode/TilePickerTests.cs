using NUnit.Framework;
using SushiSurvival.World;

namespace SushiSurvival.EditModeTests
{
    public class TilePickerTests
    {
        /// <summary>칸마다 완전 무작위로 뽑던 예전 동작(구역 크기 1, 변형 100%).</summary>
        private static TileMixConfig Config(float sandChance, float ruinChance)
        {
            return new TileMixConfig
            {
                grassCount = 16,
                sandCount = 4,
                ruinSize = 3,
                ruinSetCount = 2,
                sandPatchSize = 1,
                regionSize = 1,
                grassVariantChance = 1f,
                grassDetailCount = 2,
                grassDetailChance = 0f,
                sandChance = sandChance,
                ruinChance = ruinChance
            };
        }

        /// <summary>실제로 쓰는 구성 — 구역 단위로 뭉치고 가끔만 변형.</summary>
        private static TileMixConfig CalmConfig(float sandChance, float ruinChance,
                                                int regionSize, float variantChance, int sandPatchSize)
        {
            TileMixConfig config = Config(sandChance, ruinChance);
            config.regionSize = regionSize;
            config.grassVariantChance = variantChance;
            config.sandPatchSize = sandPatchSize;
            return config;
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

        [Test]
        public void Pick_RuinVariantIsSameAcrossOnePatch()
        {
            // 한 패치 안에서 세트가 섞이면 깨진 유적과 멀쩡한 유적이
            // 반씩 붙어 구조물이 깨져 보인다.
            var config = Config(0f, 1f);
            var variants = new System.Collections.Generic.HashSet<int>();

            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                    variants.Add(TilePicker.Pick(x, y, 1, config).Variant);

            Assert.AreEqual(1, variants.Count);
        }

        [Test]
        public void Pick_RuinVariantStaysWithinSetCount()
        {
            var config = Config(0f, 1f);

            for (int patch = 0; patch < 40; patch++)
            {
                int variant = TilePicker.Pick(patch * 3, patch * 3, 1, config).Variant;
                Assert.GreaterOrEqual(variant, 0);
                Assert.Less(variant, 2);
            }
        }

        [Test]
        public void Pick_GrassRegionUsesOneTile_WhenNoVariants()
        {
            // 구역 안이 전부 같은 타일이어야 색이 뭉쳐 보이고 눈이 편하다.
            var config = CalmConfig(0f, 0f, regionSize: 4, variantChance: 0f, sandPatchSize: 2);
            var seen = new System.Collections.Generic.HashSet<int>();

            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    seen.Add(TilePicker.Pick(x, y, 9, config).Index);

            Assert.AreEqual(1, seen.Count);
        }

        [Test]
        public void Pick_GrassRegionsDifferFromEachOther()
        {
            // 모든 구역이 같은 타일이면 바닥이 단색이 되어 이동감이 사라진다.
            var config = CalmConfig(0f, 0f, regionSize: 4, variantChance: 0f, sandPatchSize: 2);
            var seen = new System.Collections.Generic.HashSet<int>();

            for (int region = 0; region < 30; region++)
                seen.Add(TilePicker.Pick(region * 4, 0, 9, config).Index);

            Assert.Greater(seen.Count, 1);
        }

        [Test]
        public void Pick_GrassRegionAlignsOnNegativeCoordinates()
        {
            var config = CalmConfig(0f, 0f, regionSize: 4, variantChance: 0f, sandPatchSize: 2);
            var seen = new System.Collections.Generic.HashSet<int>();

            for (int y = -4; y < 0; y++)
                for (int x = -4; x < 0; x++)
                    seen.Add(TilePicker.Pick(x, y, 9, config).Index);

            Assert.AreEqual(1, seen.Count);
        }

        [Test]
        public void Pick_SandFormsPatches_InsteadOfSingleSpecks()
        {
            // 사막이 낱개로 흩뿌려지면 노이즈처럼 보인다.
            var config = CalmConfig(1f, 0f, regionSize: 4, variantChance: 0f, sandPatchSize: 2);

            for (int y = 0; y < 2; y++)
                for (int x = 0; x < 2; x++)
                    Assert.AreEqual(TileKind.Sand, TilePicker.Pick(x, y, 9, config).Kind);
        }
        [Test]
        public void Pick_NeverReturnsDetail_WhenChanceIsZero()
        {
            var config = Config(0f, 0f);

            for (int x = 0; x < 200; x++)
                Assert.AreNotEqual(TileKind.GrassDetail, TilePicker.Pick(x, x * 3, 4, config).Kind);
        }

        [Test]
        public void Pick_AlwaysReturnsDetail_WhenChanceIsOne()
        {
            TileMixConfig config = Config(0f, 0f);
            config.grassDetailChance = 1f;

            for (int x = 0; x < 50; x++)
                Assert.AreEqual(TileKind.GrassDetail, TilePicker.Pick(x, x * 3, 4, config).Kind);
        }

        [Test]
        public void Pick_DetailIndexStaysWithinDetailCount()
        {
            TileMixConfig config = Config(0f, 0f);
            config.grassDetailChance = 1f;

            for (int x = -50; x < 50; x++)
            {
                int index = TilePicker.Pick(x, x + 2, 4, config).Index;
                Assert.GreaterOrEqual(index, 0);
                Assert.Less(index, 2);
            }
        }

    }
}
