using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class TutorialStepLogicTests
    {
        [Test]
        public void Next_WalksThroughAllStepsInOrder()
        {
            var order = new[]
            {
                TutorialStep.Move, TutorialStep.Attack, TutorialStep.LevelUp,
                TutorialStep.Wasabi, TutorialStep.Skill, TutorialStep.Done
            };

            for (int i = 0; i < order.Length - 1; i++)
                Assert.AreEqual(order[i + 1], TutorialStepLogic.Next(order[i]));

            Assert.AreEqual(TutorialStep.Done, TutorialStepLogic.Next(TutorialStep.Done), "끝에서 더 가지 않는다");
        }

        [Test]
        public void Number_StartsAtOne_AndDoneKeepsLast()
        {
            Assert.AreEqual(1, TutorialStepLogic.Number(TutorialStep.Move));
            Assert.AreEqual(5, TutorialStepLogic.Number(TutorialStep.Skill));
            Assert.AreEqual(TutorialStepLogic.TotalSteps, TutorialStepLogic.Number(TutorialStep.Done));
        }

        [Test]
        public void ReachedMarker_UsesRadius()
        {
            Assert.IsTrue(TutorialStepLogic.ReachedMarker(new Vector2(0f, 0f), new Vector2(1f, 0f), 1.2f));
            Assert.IsTrue(TutorialStepLogic.ReachedMarker(new Vector2(0f, 0f), new Vector2(1.2f, 0f), 1.2f), "경계는 닿은 것으로 본다");
            Assert.IsFalse(TutorialStepLogic.ReachedMarker(new Vector2(0f, 0f), new Vector2(2f, 0f), 1.2f));
        }

        [Test]
        public void KillGoalReached_AtGoal()
        {
            Assert.IsFalse(TutorialStepLogic.KillGoalReached(TutorialStepLogic.KillGoal - 1));
            Assert.IsTrue(TutorialStepLogic.KillGoalReached(TutorialStepLogic.KillGoal));
            Assert.IsTrue(TutorialStepLogic.KillGoalReached(TutorialStepLogic.KillGoal + 5));
        }

        [Test]
        public void Instruction_Attack_ShowsKillProgress_Clamped()
        {
            StringAssert.Contains("(1/3)", TutorialStepLogic.Instruction(TutorialStep.Attack, 1));
            StringAssert.Contains("(3/3)", TutorialStepLogic.Instruction(TutorialStep.Attack, 99));
            StringAssert.Contains("(0/3)", TutorialStepLogic.Instruction(TutorialStep.Attack, -2));
        }

        [Test]
        public void WasabiWarning_TellsToPressTheWasabiButton()
        {
            StringAssert.Contains("와사비", TutorialStepLogic.WasabiWarning);
            StringAssert.Contains("버튼", TutorialStepLogic.WasabiWarning);
        }

        [Test]
        public void Instruction_EveryStepHasText_AndMentionsItsAction()
        {
            foreach (TutorialStep step in System.Enum.GetValues(typeof(TutorialStep)))
                Assert.IsNotEmpty(TutorialStepLogic.Instruction(step, 0), step.ToString());

            StringAssert.Contains("WASD", TutorialStepLogic.Instruction(TutorialStep.Move, 0));
            StringAssert.Contains("와사비", TutorialStepLogic.Instruction(TutorialStep.Wasabi, 0));
            StringAssert.Contains("클릭", TutorialStepLogic.Instruction(TutorialStep.Skill, 0));
        }
    }
}
