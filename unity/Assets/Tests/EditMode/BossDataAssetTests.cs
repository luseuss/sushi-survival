using NUnit.Framework;
using UnityEditor;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    /// <summary>보스 소환 단계 데이터가 구조적으로 올바른지 확인한다. 마릿수·임계의 절대값은 플레이로 계속 바뀌어서 검증하지 않는다.</summary>
    public class BossDataAssetTests
    {
        private const string AssetPath = "Assets/_Project/Data/BossData.asset";

        private static BossData Load()
        {
            var data = AssetDatabase.LoadAssetAtPath<BossData>(AssetPath);
            Assert.IsNotNull(data, $"{AssetPath}를 불러올 수 없습니다.");
            return data;
        }

        [Test]
        public void HasSixSummonStages()
        {
            var stages = Load().summonStages;
            Assert.IsNotNull(stages, "summonStages가 비어 있습니다.");
            Assert.AreEqual(6, stages.Length);
        }

        [Test]
        public void Thresholds_AreInsideZeroAndOne_AndStrictlyDescending()
        {
            var stages = Load().summonStages;
            Assert.IsNotNull(stages);

            for (int i = 0; i < stages.Length; i++)
            {
                Assert.Greater(stages[i].healthThreshold, 0f, $"{i + 1}단계 임계는 0보다 커야 합니다.");
                Assert.Less(stages[i].healthThreshold, 1f, $"{i + 1}단계 임계는 1보다 작아야 합니다.");

                if (i > 0)
                    Assert.Less(stages[i].healthThreshold, stages[i - 1].healthThreshold,
                        $"{i + 1}단계 임계는 {i}단계보다 낮아야 합니다.");
            }
        }

        [Test]
        public void EveryStage_SummonsAtLeastOneMob()
        {
            var stages = Load().summonStages;
            Assert.IsNotNull(stages);

            for (int i = 0; i < stages.Length; i++)
                Assert.Greater(stages[i].Total, 0, $"{i + 1}단계가 아무것도 소환하지 않습니다.");
        }
    }
}
