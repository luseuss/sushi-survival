using NUnit.Framework;
using SushiSurvival.World;

namespace SushiSurvival.EditModeTests
{
    public class RoadNetworkLogicTests
    {
        private const int Seed = 4242;

        private static RoadConfig Config(float bandChance = 1f, float segmentChance = 1f)
        {
            RoadConfig config = RoadNetworkLogic.DefaultConfig;
            config.bandChance = bandChance;
            config.segmentChance = segmentChance;
            return config;
        }

        [Test]
        public void NoRoads_WhenBandChanceIsZero()
        {
            RoadConfig config = Config(0f);
            for (int y = -30; y < 30; y++)
                for (int x = -30; x < 30; x++)
                    Assert.IsFalse(RoadNetworkLogic.IsRoadBlock(x, y, Seed, config));
        }

        [Test]
        public void IsRoadBlock_IsDeterministic()
        {
            RoadConfig config = Config(0.6f, 0.85f);
            for (int y = -20; y < 20; y++)
                for (int x = -20; x < 20; x++)
                    Assert.AreEqual(RoadNetworkLogic.IsRoadBlock(x, y, Seed, config),
                                    RoadNetworkLogic.IsRoadBlock(x, y, Seed, config));
        }

        [Test]
        public void EveryBand_HasOneHorizontalRowPerBand_WhenAlwaysOn()
        {
            RoadConfig config = Config();
            int band = config.bandSize;

            // 세로 길과 겹치지 않도록 x 하나를 고정해 y 방향 개수를 센다.
            for (int bandIndex = -3; bandIndex < 3; bandIndex++)
            {
                int rows = 0;
                for (int y = bandIndex * band; y < (bandIndex + 1) * band; y++)
                {
                    if (RoadNetworkLogic.IsRoadBlock(0, y, Seed, config)) rows++;
                }
                // x=0 열 자체가 세로 길일 수 있어 가로 길 한 줄 이상이면 된다.
                Assert.GreaterOrEqual(rows, 1, $"띠 {bandIndex}");
            }
        }

        [Test]
        public void ChoosePiece_IsNone_OffRoad()
        {
            RoadConfig config = Config(0f);
            Assert.AreEqual(RoadPiece.None, RoadNetworkLogic.ChoosePiece(3, 3, Seed, config));
        }

        [Test]
        public void ChoosePiece_SegmentEndsGetCaps()
        {
            RoadConfig config = Config(1f, 0.5f);
            bool sawLeftCap = false, sawRightCap = false;

            for (int y = -30; y < 30; y++)
            {
                for (int x = -60; x < 60; x++)
                {
                    RoadPiece piece = RoadNetworkLogic.ChoosePiece(x, y, Seed, config);
                    if (piece == RoadPiece.CapLeft)
                    {
                        sawLeftCap = true;
                        Assert.IsTrue(RoadNetworkLogic.IsRoadBlock(x + 1, y, Seed, config));
                        Assert.IsFalse(RoadNetworkLogic.IsRoadBlock(x - 1, y, Seed, config));
                    }
                    if (piece == RoadPiece.CapRight)
                    {
                        sawRightCap = true;
                        Assert.IsTrue(RoadNetworkLogic.IsRoadBlock(x - 1, y, Seed, config));
                        Assert.IsFalse(RoadNetworkLogic.IsRoadBlock(x + 1, y, Seed, config));
                    }
                }
            }

            Assert.IsTrue(sawLeftCap && sawRightCap);
        }

        [Test]
        public void ChoosePiece_CrossingBlocks_AreCross()
        {
            RoadConfig config = Config();
            int crosses = 0;

            for (int y = -30; y < 30; y++)
            {
                for (int x = -30; x < 30; x++)
                {
                    if (RoadNetworkLogic.ChoosePiece(x, y, Seed, config) != RoadPiece.Cross) continue;

                    crosses++;
                    Assert.IsTrue(RoadNetworkLogic.IsRoadBlock(x - 1, y, Seed, config));
                    Assert.IsTrue(RoadNetworkLogic.IsRoadBlock(x + 1, y, Seed, config));
                    Assert.IsTrue(RoadNetworkLogic.IsRoadBlock(x, y - 1, Seed, config));
                    Assert.IsTrue(RoadNetworkLogic.IsRoadBlock(x, y + 1, Seed, config));
                }
            }

            Assert.Greater(crosses, 0);
        }

        [Test]
        public void ChoosePiece_StraightRunsAreHorizontalOrVertical()
        {
            RoadConfig config = Config();
            bool sawHorizontal = false, sawVertical = false;

            for (int y = -30; y < 30; y++)
            {
                for (int x = -30; x < 30; x++)
                {
                    RoadPiece piece = RoadNetworkLogic.ChoosePiece(x, y, Seed, config);
                    sawHorizontal |= piece == RoadPiece.Horizontal;
                    sawVertical |= piece == RoadPiece.Vertical;
                }
            }

            Assert.IsTrue(sawHorizontal && sawVertical);
        }

        [TestCase(0, 0, 3, 6)]
        [TestCase(1, 0, 3, 7)]
        [TestCase(2, 2, 3, 2)]
        [TestCase(-1, -1, 3, 2)]
        [TestCase(3, 3, 3, 6)]
        public void SpriteIndex_FlipsWorldYAndWrapsPerBlock(int x, int y, int size, int expected)
        {
            Assert.AreEqual(expected, RoadNetworkLogic.SpriteIndex(x, y, size));
        }

        [TestCase(0, 0)]
        [TestCase(2, 0)]
        [TestCase(3, 1)]
        [TestCase(-1, -1)]
        [TestCase(-3, -1)]
        [TestCase(-4, -2)]
        public void ToBlock_FloorsNegatives(int cell, int expected)
        {
            Assert.AreEqual(expected, RoadNetworkLogic.ToBlock(cell, 3));
        }
    }
}
