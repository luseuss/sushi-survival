using NUnit.Framework;
using SushiSurvival.Core;

namespace SushiSurvival.EditModeTests
{
    public class TypewriterLogicTests
    {
        // ---- RevealedCount ----

        [Test]
        public void RevealedCount_GrowsWithTime()
        {
            Assert.AreEqual(0, TypewriterLogic.RevealedCount(0f, 40f, 100));
            Assert.AreEqual(20, TypewriterLogic.RevealedCount(0.5f, 40f, 100));
        }

        [Test]
        public void RevealedCount_NeverExceedsTotal()
        {
            Assert.AreEqual(10, TypewriterLogic.RevealedCount(99f, 40f, 10));
        }

        [Test]
        public void RevealedCount_NonPositiveSpeed_ShowsEverythingAtOnce()
        {
            Assert.AreEqual(10, TypewriterLogic.RevealedCount(0f, 0f, 10));
            Assert.AreEqual(10, TypewriterLogic.RevealedCount(0f, -5f, 10));
        }

        [Test]
        public void RevealedCount_NegativeElapsed_IsZero()
        {
            Assert.AreEqual(0, TypewriterLogic.RevealedCount(-1f, 40f, 10));
        }

        [Test]
        public void RevealedCount_EmptyText_IsZero()
        {
            Assert.AreEqual(0, TypewriterLogic.RevealedCount(5f, 40f, 0));
        }

        // ---- CountVisible ----

        [Test]
        public void CountVisible_PlainText()
        {
            Assert.AreEqual(5, TypewriterLogic.CountVisible("안녕하세요"));
        }

        [Test]
        public void CountVisible_IgnoresRichTextTags()
        {
            Assert.AreEqual(4, TypewriterLogic.CountVisible("<color=#ff0000>빨강</color>색!"));
            Assert.AreEqual(2, TypewriterLogic.CountVisible("<b>굵게</b>"));
        }

        [Test]
        public void CountVisible_TreatsLoneAngleBracketAsText()
        {
            Assert.AreEqual(5, TypewriterLogic.CountVisible("a < b"));
            Assert.AreEqual(3, TypewriterLogic.CountVisible("1<2"));
        }

        [Test]
        public void CountVisible_NullOrEmpty_IsZero()
        {
            Assert.AreEqual(0, TypewriterLogic.CountVisible(null));
            Assert.AreEqual(0, TypewriterLogic.CountVisible(string.Empty));
        }

        // ---- Substring ----

        [Test]
        public void Substring_PlainText_TakesPrefix()
        {
            Assert.AreEqual("안녕", TypewriterLogic.Substring("안녕하세요", 2));
        }

        [Test]
        public void Substring_CountAtLeastLength_ReturnsWholeText()
        {
            Assert.AreEqual("안녕", TypewriterLogic.Substring("안녕", 2));
            Assert.AreEqual("안녕", TypewriterLogic.Substring("안녕", 99));
        }

        [Test]
        public void Substring_ZeroOrNegative_IsEmpty()
        {
            Assert.AreEqual(string.Empty, TypewriterLogic.Substring("안녕", 0));
            Assert.AreEqual(string.Empty, TypewriterLogic.Substring("안녕", -3));
        }

        [Test]
        public void Substring_KeepsTagsIntactAndNeverSplitsThem()
        {
            const string text = "<color=#ff0000>빨강</color>색";

            Assert.AreEqual("<color=#ff0000>빨", TypewriterLogic.Substring(text, 1));
            Assert.AreEqual("<color=#ff0000>빨강</color>", TypewriterLogic.Substring(text, 2));
            Assert.AreEqual(text, TypewriterLogic.Substring(text, 3));
        }

        [Test]
        public void Substring_LoneAngleBracketCountsAsCharacter()
        {
            Assert.AreEqual("1<", TypewriterLogic.Substring("1<2", 2));
        }

        [Test]
        public void Substring_NullOrEmpty_IsEmpty()
        {
            Assert.AreEqual(string.Empty, TypewriterLogic.Substring(null, 3));
            Assert.AreEqual(string.Empty, TypewriterLogic.Substring(string.Empty, 3));
        }
    }
}
