using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class ReviveLogicTests
    {
        [Test]
        public void CanRevive_False_WhenNoReviveAugment()
        {
            Assert.IsFalse(ReviveLogic.CanRevive(0, 0));
        }

        [Test]
        public void CanRevive_True_WhenAugmentAllowsAndNoneUsed()
        {
            Assert.IsTrue(ReviveLogic.CanRevive(0, 1));
        }

        [Test]
        public void CanRevive_False_OnceAllowedRevivesAreUsedUp()
        {
            Assert.IsFalse(ReviveLogic.CanRevive(1, 1));
        }

        [Test]
        public void CanRevive_StaysFalse_WhenUsedCountIsCarriedIntoNextScene()
        {
            // 보스 씬은 새 플레이어(사용 0회)에 부활 증강을 다시 입히므로, GameScene에서 쓴 횟수를
            // 이월해 복원하지 않으면 여기서 true가 되어 부활이 한 번 더 작동한다.
            int carriedUsed = 1;
            Assert.IsFalse(ReviveLogic.CanRevive(carriedUsed, 1));
            Assert.IsTrue(ReviveLogic.CanRevive(0, 1));
        }
    }
}
