using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class IdleResetLogicTests
    {
        // ---- ContextFor ----

        [Test]
        public void ContextFor_IntroScene_IsIgnored()
        {
            Assert.AreEqual(IdleContext.Ignore, IdleResetLogic.ContextFor("IntroScene", false, RunState.CharacterSelect));
        }

        [Test]
        public void ContextFor_StoryScene_IsMenu()
        {
            Assert.AreEqual(IdleContext.Menu, IdleResetLogic.ContextFor("StoryScene", false, RunState.CharacterSelect));
        }

        [Test]
        public void ContextFor_GameScene_FollowsRunState()
        {
            Assert.AreEqual(IdleContext.Menu, IdleResetLogic.ContextFor("GameScene", true, RunState.CharacterSelect));
            Assert.AreEqual(IdleContext.Menu, IdleResetLogic.ContextFor("GameScene", true, RunState.Intro));
            Assert.AreEqual(IdleContext.Playing, IdleResetLogic.ContextFor("GameScene", true, RunState.Playing));
            Assert.AreEqual(IdleContext.Result, IdleResetLogic.ContextFor("GameScene", true, RunState.Result));
        }

        [Test]
        public void ContextFor_BossScene_FollowsRunState()
        {
            Assert.AreEqual(IdleContext.Playing, IdleResetLogic.ContextFor("BossScene", true, RunState.Playing));
            Assert.AreEqual(IdleContext.Result, IdleResetLogic.ContextFor("BossScene", true, RunState.Result));
        }

        [Test]
        public void ContextFor_GameSceneWithoutManagerYet_IsMenu()
        {
            // 씬이 막 로드돼 GameManager가 아직 없을 때 state 값에 휘둘리지 않는다.
            Assert.AreEqual(IdleContext.Menu, IdleResetLogic.ContextFor("GameScene", false, RunState.Result));
        }

        [Test]
        public void ContextFor_UnknownScene_IsIgnored()
        {
            Assert.AreEqual(IdleContext.Ignore, IdleResetLogic.ContextFor("SomethingElse", true, RunState.Playing));
        }

        // ---- TimeoutFor ----

        [Test]
        public void TimeoutFor_PicksTheMatchingValue()
        {
            var timeouts = new IdleTimeouts(60f, 45f, 30f);

            Assert.AreEqual(60f, IdleResetLogic.TimeoutFor(IdleContext.Menu, timeouts));
            Assert.AreEqual(45f, IdleResetLogic.TimeoutFor(IdleContext.Playing, timeouts));
            Assert.AreEqual(30f, IdleResetLogic.TimeoutFor(IdleContext.Result, timeouts));
            Assert.AreEqual(0f, IdleResetLogic.TimeoutFor(IdleContext.Ignore, timeouts));
        }

        // ---- IsExpired / RemainingSeconds ----

        [Test]
        public void IsExpired_TrueAtOrAfterTimeout()
        {
            Assert.IsFalse(IdleResetLogic.IsExpired(29.9f, 30f));
            Assert.IsTrue(IdleResetLogic.IsExpired(30f, 30f));
            Assert.IsTrue(IdleResetLogic.IsExpired(99f, 30f));
        }

        [Test]
        public void IsExpired_NonPositiveTimeoutNeverExpires()
        {
            Assert.IsFalse(IdleResetLogic.IsExpired(9999f, 0f));
            Assert.IsFalse(IdleResetLogic.IsExpired(9999f, -5f));
        }

        [Test]
        public void RemainingSeconds_CountsDownAndIsNegativeWhenDisabled()
        {
            Assert.AreEqual(20f, IdleResetLogic.RemainingSeconds(10f, 30f), 0.0001f);
            Assert.Less(IdleResetLogic.RemainingSeconds(10f, 0f), 0f);
        }

        // ---- ShouldWarn / CountdownNumber ----

        [Test]
        public void ShouldWarn_OnlyInsideTheWarningWindow()
        {
            Assert.IsFalse(IdleResetLogic.ShouldWarn(30f, 10f));
            Assert.IsFalse(IdleResetLogic.ShouldWarn(10.01f, 10f));
            Assert.IsTrue(IdleResetLogic.ShouldWarn(10f, 10f));
            Assert.IsTrue(IdleResetLogic.ShouldWarn(3f, 10f));
            Assert.IsTrue(IdleResetLogic.ShouldWarn(0f, 10f));
        }

        [Test]
        public void ShouldWarn_NeverForDisabledScreensOrDisabledWarning()
        {
            Assert.IsFalse(IdleResetLogic.ShouldWarn(-1f, 10f));   // 리셋하지 않는 화면
            Assert.IsFalse(IdleResetLogic.ShouldWarn(3f, 0f));     // 안내 끔
            Assert.IsFalse(IdleResetLogic.ShouldWarn(3f, -5f));
        }

        [Test]
        public void CountdownNumber_RoundsUpSoItNeverShowsZeroWhileWaiting()
        {
            Assert.AreEqual(10, IdleResetLogic.CountdownNumber(10f));
            Assert.AreEqual(10, IdleResetLogic.CountdownNumber(9.2f));
            Assert.AreEqual(1, IdleResetLogic.CountdownNumber(0.3f));
            Assert.AreEqual(0, IdleResetLogic.CountdownNumber(0f));
            Assert.AreEqual(0, IdleResetLogic.CountdownNumber(-1f));
        }
    }
}
