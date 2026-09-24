using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Player;
using SushiSurvival.Weapons;

namespace SushiSurvival.EditModeTests
{
    public class FanHitTestTests
    {
        [Test]
        public void IsInsideFan_TrueForTargetDirectlyAhead()
        {
            bool result = FanHitTest.IsInsideFan(Vector2.zero, Vector2.right, 2f, 120f, new Vector2(1f, 0f));

            Assert.IsTrue(result);
        }

        [Test]
        public void IsInsideFan_FalseForTargetBehind()
        {
            bool result = FanHitTest.IsInsideFan(Vector2.zero, Vector2.right, 2f, 120f, new Vector2(-1f, 0f));

            Assert.IsFalse(result);
        }

        [Test]
        public void IsInsideFan_FalseWhenBeyondRadius()
        {
            bool result = FanHitTest.IsInsideFan(Vector2.zero, Vector2.right, 2f, 120f, new Vector2(5f, 0f));

            Assert.IsFalse(result);
        }

        [Test]
        public void IsInsideFan_TrueAtHalfAngleBoundary()
        {
            float radians = 60f * Mathf.Deg2Rad;
            var target = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 1.5f;

            bool result = FanHitTest.IsInsideFan(Vector2.zero, Vector2.right, 2f, 120f, target);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsInsideFan_FalseJustOutsideAngleBoundary()
        {
            float radians = 61f * Mathf.Deg2Rad;
            var target = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 1.5f;

            bool result = FanHitTest.IsInsideFan(Vector2.zero, Vector2.right, 2f, 120f, target);

            Assert.IsFalse(result);
        }

        [Test]
        public void EggFan_WhileMovingUp_HitsEnemyOnTheSideTheUmbrellaSwings()
        {
            // 위로 이동 중이면 양산 그림은 오른쪽으로 휘둘린다(IsFacingRight는 x=0에서 오른쪽).
            // 판정도 그 방향이어야 스윙 앞의 적이 맞는다.
            var enemy = new Vector2(1.5f, 0.2f);
            Vector2 aim = FacingLogic.HorizontalFacing(Vector2.up);

            Assert.IsTrue(FanHitTest.IsInsideFan(Vector2.zero, aim, 2f, 120f, enemy));
        }

        [Test]
        public void EggFan_UsingRawMoveDirection_MissesThatSameEnemy()
        {
            // 예전 방식(이동 방향 벡터를 그대로 사용)의 버그 재현: 같은 적이 부채꼴 밖이 된다.
            var enemy = new Vector2(1.5f, 0.2f);

            Assert.IsFalse(FanHitTest.IsInsideFan(Vector2.zero, Vector2.up, 2f, 120f, enemy));
        }
    }
}
