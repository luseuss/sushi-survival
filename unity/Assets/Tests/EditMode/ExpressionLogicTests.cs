using NUnit.Framework;
using UnityEngine;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    public class ExpressionLogicTests
    {
        private static readonly ExpressionType[] Acting =
        {
            ExpressionType.Happy, ExpressionType.Surprised, ExpressionType.Angry,
            ExpressionType.Sad, ExpressionType.Embarrassed,
        };

        [Test]
        public void Neutral_IsAlwaysIdentity_AndHasNoBubble()
        {
            ExpressionPose pose = ExpressionLogic.Evaluate(ExpressionType.Neutral, 0.2f);

            Assert.AreEqual(Vector2.zero, pose.Offset);
            Assert.AreEqual(1f, pose.Scale);
            Assert.AreEqual(0f, pose.TintStrength);
            Assert.AreEqual(0f, ExpressionLogic.Duration(ExpressionType.Neutral));
            Assert.AreEqual(string.Empty, ExpressionLogic.Glyph(ExpressionType.Neutral));
        }

        [Test]
        public void EveryActingExpression_HasDurationAndGlyph()
        {
            foreach (ExpressionType expression in Acting)
            {
                Assert.Greater(ExpressionLogic.Duration(expression), 0f, expression.ToString());
                Assert.IsNotEmpty(ExpressionLogic.Glyph(expression), expression.ToString());
            }
        }

        [Test]
        public void AfterDuration_PoseReturnsToIdentity()
        {
            foreach (ExpressionType expression in Acting)
            {
                ExpressionPose pose = ExpressionLogic.Evaluate(expression, ExpressionLogic.Duration(expression));

                Assert.AreEqual(Vector2.zero, pose.Offset, expression.ToString());
                Assert.AreEqual(1f, pose.Scale, expression.ToString());
                Assert.AreEqual(0f, pose.RotationDegrees, expression.ToString());
                Assert.AreEqual(0f, pose.TintStrength, expression.ToString());
            }
        }

        [Test]
        public void NegativeTime_IsIdentity()
        {
            Assert.AreEqual(Vector2.zero, ExpressionLogic.Evaluate(ExpressionType.Happy, -0.1f).Offset);
        }

        [Test]
        public void Happy_HopsUpward()
        {
            ExpressionPose pose = ExpressionLogic.Evaluate(ExpressionType.Happy, 0.2f);
            Assert.Greater(pose.Offset.y, 0f);
        }

        [Test]
        public void Surprised_JumpsUpAndGrows()
        {
            ExpressionPose pose = ExpressionLogic.Evaluate(ExpressionType.Surprised, 0.15f);

            Assert.Greater(pose.Offset.y, 20f);
            Assert.Greater(pose.Scale, 1f);
        }

        [Test]
        public void Angry_ShakesAndReddens()
        {
            ExpressionPose pose = ExpressionLogic.Evaluate(ExpressionType.Angry, 0.05f);

            Assert.AreNotEqual(0f, pose.Offset.x);
            Assert.Greater(pose.TintStrength, 0f);
            Assert.Greater(pose.Tint.r, pose.Tint.b);
        }

        [Test]
        public void Sad_SinksAndDarkens()
        {
            ExpressionPose pose = ExpressionLogic.Evaluate(ExpressionType.Sad, 0.5f);

            Assert.Less(pose.Offset.y, 0f);
            Assert.Greater(pose.TintStrength, 0f);
        }

        [Test]
        public void Embarrassed_SwaysSideToSideWithTilt()
        {
            ExpressionPose pose = ExpressionLogic.Evaluate(ExpressionType.Embarrassed, 0.05f);

            Assert.AreNotEqual(0f, pose.Offset.x);
            Assert.AreNotEqual(0f, pose.RotationDegrees);
        }

        [Test]
        public void Bubble_PopsInHoldsThenFades()
        {
            Assert.AreEqual(0f, ExpressionLogic.BubbleScale(0f), 0.0001f);
            Assert.Greater(ExpressionLogic.BubbleScale(0.09f), 1f);
            Assert.AreEqual(1f, ExpressionLogic.BubbleScale(0.5f), 0.0001f);

            Assert.AreEqual(1f, ExpressionLogic.BubbleAlpha(0.5f), 0.0001f);
            Assert.Less(ExpressionLogic.BubbleAlpha(ExpressionLogic.BubbleDuration - 0.1f), 1f);
            Assert.AreEqual(0f, ExpressionLogic.BubbleAlpha(ExpressionLogic.BubbleDuration), 0.0001f);
        }
    }

    public class ExpressionSetTests
    {
        private static Sprite NewSprite()
        {
            var texture = new Texture2D(2, 2);
            return Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
        }

        [Test]
        public void NoSet_ReturnsFallback()
        {
            Sprite fallback = NewSprite();

            Assert.AreSame(fallback, ExpressionSet.PortraitOrDefault(null, ExpressionType.Happy, fallback));
            Assert.AreSame(fallback, ExpressionSet.StandingOrDefault(null, ExpressionType.Happy, fallback));
        }

        [Test]
        public void MatchingEntry_ReturnsItsSprites_OthersFallBack()
        {
            Sprite fallback = NewSprite();
            Sprite happyPortrait = NewSprite();

            var set = ScriptableObject.CreateInstance<ExpressionSet>();
            set.entries = new[]
            {
                new ExpressionSet.Entry { expression = ExpressionType.Happy, portrait = happyPortrait },
            };

            Assert.AreSame(happyPortrait, ExpressionSet.PortraitOrDefault(set, ExpressionType.Happy, fallback));
            // 입상은 비워 뒀으니 기본 그림.
            Assert.AreSame(fallback, ExpressionSet.StandingOrDefault(set, ExpressionType.Happy, fallback));
            // 다른 표정은 항목이 없으니 기본 그림.
            Assert.AreSame(fallback, ExpressionSet.PortraitOrDefault(set, ExpressionType.Sad, fallback));

            Object.DestroyImmediate(set);
        }
    }
}
