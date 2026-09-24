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
    }
}
