using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    /// <summary>보스 소환 단계 데이터가 구조적으로 올바른지 확인한다. 마릿수·임계의 절대값은 플레이로 계속 바뀌어서 검증하지 않는다(단, 중간보스는 체력 50%에서 2마리, 20%에서 4마리만 소환한다는 기획 결정은 확인한다).</summary>
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
        public void HasSevenSummonStages()
        {
            var stages = Load().summonStages;
            Assert.IsNotNull(stages, "summonStages가 비어 있습니다.");
            Assert.AreEqual(7, stages.Length);
        }

        [Test]
        public void MidBosses_AreSummonedOnlyAtHalfAndTwentyPercent()
        {
            var stages = Load().summonStages;
            Assert.IsNotNull(stages);

            foreach (var stage in stages)
            {
                if (Mathf.Approximately(stage.healthThreshold, 0.5f))
                    Assert.AreEqual(2, stage.midCount, "체력 50%에서 중간보스 2마리");
                else if (Mathf.Approximately(stage.healthThreshold, 0.2f))
                    Assert.AreEqual(4, stage.midCount, "체력 20%에서 중간보스 4마리");
                else
                    Assert.AreEqual(0, stage.midCount, $"체력 {stage.healthThreshold * 100f}% 단계에서는 중간보스를 소환하지 않는다(10%에서도 소환하지 않는다)");
            }
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
