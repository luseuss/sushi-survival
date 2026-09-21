using NUnit.Framework;
using UnityEditor;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    /// <summary>세계관 대본 에셋이 스펙(11줄, 표기 통일, 초상화 규칙)대로 들어갔는지 확인한다.</summary>
    public class WorldIntroStoryAssetTests
    {
        private const string AssetPath = "Assets/_Project/Data/WorldIntroStory.asset";

        private static StoryDialogueData Load()
        {
            var data = AssetDatabase.LoadAssetAtPath<StoryDialogueData>(AssetPath);
            Assert.IsNotNull(data, $"{AssetPath}를 불러올 수 없습니다.");
            return data;
        }

        [Test]
        public void Asset_HasElevenNonEmptyLines()
        {
            var data = Load();

            Assert.AreEqual(11, data.lines.Length);
            foreach (var line in data.lines)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(line.speakerName));
                Assert.IsFalse(string.IsNullOrWhiteSpace(line.text));
            }
        }

        [Test]
        public void OnlyFirstLine_DefinesBackground()
        {
            var data = Load();

            Assert.IsNotNull(data.lines[0].background);
            for (int i = 1; i < data.lines.Length; i++)
                Assert.IsNull(data.lines[i].background, $"{i + 1}번째 줄은 배경을 비워 직전 배경을 유지해야 합니다.");
        }

        [Test]
        public void Portraits_AdelineAndKamarionHaveOne_InariHasNone()
        {
            var data = Load();

            foreach (var line in data.lines)
            {
                if (line.speakerName == "이나리")
                    Assert.IsNull(line.portrait, "이나리는 초상화 없이 이름표만 나와야 합니다.");
                else
                    Assert.IsNotNull(line.portrait, $"{line.speakerName}의 초상화가 비어 있습니다.");
            }
        }

        [Test]
        public void Text_UsesUnifiedKingdomNames()
        {
            var data = Load();

            foreach (var line in data.lines)
            {
                StringAssert.DoesNotContain("초밥왕국", line.text);
                StringAssert.DoesNotContain("롤 왕국", line.text);
                StringAssert.DoesNotContain("스시 왕국", line.text);
            }
        }
    }
}
