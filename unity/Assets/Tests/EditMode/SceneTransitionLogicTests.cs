using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class SceneTransitionLogicTests
    {
        private const float Eps = 0.0001f;

        [Test]
        public void Out_StartsUnchanged_AndEndsZoomedAndBlack()
        {
            Assert.AreEqual(1f, SceneTransitionLogic.ZoomScale(0f), Eps);
            Assert.AreEqual(0f, SceneTransitionLogic.OutAlpha(0f), Eps);

            Assert.AreEqual(SceneTransitionLogic.MaxZoom, SceneTransitionLogic.ZoomScale(1f), Eps);
            Assert.AreEqual(1f, SceneTransitionLogic.OutAlpha(1f), Eps);
        }

        [Test]
        public void Out_IsMonotonic()
        {
            float previousZoom = 0f, previousAlpha = -1f;
            for (int i = 0; i <= 10; i++)
            {
                float t = i / 10f;
                float zoom = SceneTransitionLogic.ZoomScale(t);
                float alpha = SceneTransitionLogic.OutAlpha(t);

                Assert.GreaterOrEqual(zoom, previousZoom - Eps);
                Assert.GreaterOrEqual(alpha, previousAlpha - Eps);
                previousZoom = zoom;
                previousAlpha = alpha;
            }
        }

        [Test]
        public void LensDistortion_StartsFlat_RampsUp_AndEndsAtMax()
        {
            Assert.AreEqual(0f, SceneTransitionLogic.LensDistortion(0f), Eps);
            Assert.AreEqual(SceneTransitionLogic.MaxDistortion, SceneTransitionLogic.LensDistortion(1f), Eps);

            // 처음 절반은 잔잔하고(최대의 4분의 1 이하) 뒤로 갈수록 급격히 세진다.
            Assert.LessOrEqual(SceneTransitionLogic.LensDistortion(0.5f), SceneTransitionLogic.MaxDistortion * 0.26f);
            Assert.Greater(SceneTransitionLogic.LensDistortion(0.9f), SceneTransitionLogic.LensDistortion(0.5f));
        }

        [Test]
        public void In_StartsBlack_AndEndsClear()
        {
            Assert.AreEqual(1f, SceneTransitionLogic.InAlpha(0f), Eps);
            Assert.AreEqual(0f, SceneTransitionLogic.InAlpha(1f), Eps);
        }

        [Test]
        public void Progress_ClampsAndTreatsZeroDurationAsDone()
        {
            Assert.AreEqual(0f, SceneTransitionLogic.Progress(-1f, 0.5f), Eps);
            Assert.AreEqual(0.5f, SceneTransitionLogic.Progress(0.25f, 0.5f), Eps);
            Assert.AreEqual(1f, SceneTransitionLogic.Progress(9f, 0.5f), Eps);
            Assert.AreEqual(1f, SceneTransitionLogic.Progress(0f, 0f), Eps);
        }

        [Test]
        public void TotalDuration_IsAboutOneSecond()
        {
            float total = SceneTransitionLogic.OutDuration + SceneTransitionLogic.InDuration;
            Assert.AreEqual(1f, total, 0.2f);
        }
    }
}
