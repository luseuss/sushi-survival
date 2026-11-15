using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class TutorialStateTests
    {
        [SetUp]
        public void SetUp() => TutorialState.ResetForNewVisitor();

        [TearDown]
        public void TearDown() => TutorialState.ResetForNewVisitor();

        [Test]
        public void NewVisitor_ShouldBePrompted()
        {
            Assert.IsTrue(TutorialState.ShouldPrompt);
            Assert.IsFalse(TutorialState.SeenThisVisitor);
            Assert.IsFalse(TutorialState.Requested);
        }

        [Test]
        public void MarkSeen_StopsPrompting()
        {
            TutorialState.MarkSeen();

            Assert.IsFalse(TutorialState.ShouldPrompt);
            Assert.IsFalse(TutorialState.Requested, "건너뛰기는 연습 요청이 아니다");
        }

        [Test]
        public void RequestTutorial_IsConsumedOnce_AndCountsAsSeen()
        {
            TutorialState.RequestTutorial();

            Assert.IsTrue(TutorialState.Requested);
            Assert.IsFalse(TutorialState.ShouldPrompt);
            Assert.IsTrue(TutorialState.ConsumeRequest());
            Assert.IsFalse(TutorialState.ConsumeRequest(), "한 번 읽으면 요청이 사라진다");
            Assert.IsFalse(TutorialState.ShouldPrompt, "읽어도 이 방문자는 본 것으로 남는다");
        }

        [Test]
        public void Active_BeginsWhenRequestIsConsumed_AndEndsOnEnd()
        {
            Assert.IsFalse(TutorialState.Active);

            TutorialState.RequestTutorial();
            Assert.IsFalse(TutorialState.Active, "요청만으로는 아직 진행 중이 아니다");

            TutorialState.ConsumeRequest();
            Assert.IsTrue(TutorialState.Active);

            TutorialState.EndTutorial();
            Assert.IsFalse(TutorialState.Active);
        }

        [Test]
        public void ConsumeRequest_WithoutRequest_DoesNotStartTutorial()
        {
            Assert.IsFalse(TutorialState.ConsumeRequest());
            Assert.IsFalse(TutorialState.Active);
        }

        [Test]
        public void ResetForNewVisitor_EndsActiveTutorial()
        {
            TutorialState.RequestTutorial();
            TutorialState.ConsumeRequest();

            TutorialState.ResetForNewVisitor();

            Assert.IsFalse(TutorialState.Active);
        }

        [Test]
        public void ResetForNewVisitor_PromptsAgain()
        {
            TutorialState.RequestTutorial();
            TutorialState.ConsumeRequest();

            TutorialState.ResetForNewVisitor();

            Assert.IsTrue(TutorialState.ShouldPrompt);
            Assert.IsFalse(TutorialState.Requested);
        }
    }
}
