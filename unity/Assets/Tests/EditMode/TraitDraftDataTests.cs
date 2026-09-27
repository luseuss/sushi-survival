using System.Linq;
using NUnit.Framework;
using UnityEditor;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    /// <summary>
    /// 성향 태그 대본 초안이 구조적으로 갖춰졌는지 확인한다. 문구·줄 번호 같은 절대값은
    /// 대본을 고칠 때마다 바뀌므로 검증하지 않는다.
    /// </summary>
    public class TraitDraftDataTests
    {
        private static readonly PlayerTrait[] AllTraits = { PlayerTrait.Bold, PlayerTrait.Careful, PlayerTrait.Kind };

        // ---- 결과 화면 한마디 ----

        [Test]
        public void PlayerTraitLines_CanBeLoadedFromResources()
        {
            Assert.IsNotNull(PlayerTraitLines.Load(), "Resources/PlayerTraitLines.asset을 불러올 수 없습니다.");
        }

        [Test]
        public void PlayerTraitLines_HasAllThreeTraits()
        {
            PlayerTraitLines lines = PlayerTraitLines.Load();
            Assert.IsNotNull(lines);

            foreach (PlayerTrait trait in AllTraits)
                Assert.IsNotNull(lines.Find(trait), $"{trait} 항목이 없습니다.");
        }

        [Test]
        public void PlayerTraitLines_EveryEntryHasAllTexts()
        {
            PlayerTraitLines lines = PlayerTraitLines.Load();
            Assert.IsNotNull(lines);

            foreach (PlayerTrait trait in AllTraits)
            {
                PlayerTraitEntry entry = lines.Find(trait);
                Assert.IsNotNull(entry);
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.displayName), $"{trait} 표시명이 비어 있습니다.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.victoryLine), $"{trait} 승리 한마디가 비어 있습니다.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.defeatLine), $"{trait} 패배 한마디가 비어 있습니다.");
            }
        }

        // ---- 세계관 대화 질문 ----

        private static StoryDialogueData LoadStory()
        {
            var story = AssetDatabase.LoadAssetAtPath<StoryDialogueData>("Assets/_Project/Data/WorldIntroStory.asset");
            Assert.IsNotNull(story, "WorldIntroStory.asset을 불러올 수 없습니다.");
            return story;
        }

        [Test]
        public void WorldIntroStory_HasTwoChoicePoints()
        {
            StoryDialogueData story = LoadStory();

            Assert.IsNotNull(story.choicePoints, "choicePoints가 비어 있습니다.");
            Assert.AreEqual(2, story.choicePoints.Length);
        }

        [Test]
        public void WorldIntroStory_ChoicePointsSitInsideTheLines()
        {
            StoryDialogueData story = LoadStory();
            Assert.IsNotNull(story.choicePoints);

            foreach (StoryChoicePoint point in story.choicePoints)
            {
                Assert.GreaterOrEqual(point.beforeLineIndex, 0);
                Assert.Less(point.beforeLineIndex, story.lines.Length, "질문이 대사 줄 범위 밖에 있습니다.");
            }
        }

        [Test]
        public void WorldIntroStory_EveryQuestionOffersAllThreeTraitsWithTexts()
        {
            StoryDialogueData story = LoadStory();
            Assert.IsNotNull(story.choicePoints);

            for (int i = 0; i < story.choicePoints.Length; i++)
            {
                StoryChoicePoint point = story.choicePoints[i];

                Assert.IsNotNull(point.prompt, $"질문 {i + 1}의 질문 대사가 없습니다.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(point.prompt.text), $"질문 {i + 1}의 질문 문구가 비어 있습니다.");
                Assert.IsNotNull(point.choices);

                foreach (PlayerTrait trait in AllTraits)
                    Assert.IsTrue(point.choices.Any(c => c.trait == trait), $"질문 {i + 1}에 {trait} 선택지가 없습니다.");

                foreach (StoryChoice choice in point.choices)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(choice.choiceText), $"질문 {i + 1}의 선택지 문구가 비어 있습니다.");
                    Assert.IsNotNull(choice.reply, $"질문 {i + 1}의 반응 대사가 없습니다.");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(choice.reply.text), $"질문 {i + 1}의 반응 문구가 비어 있습니다.");
                }
            }
        }

        // ---- 보스 등장 대사 성향 한 줄 ----

        [TestCase("Assets/_Project/Data/EggAffinityDialogue.asset")]
        [TestCase("Assets/_Project/Data/ShrimpAffinityDialogue.asset")]
        public void AffinityDialogue_HasABossTraitLineForEveryTrait(string path)
        {
            var dialogue = AssetDatabase.LoadAssetAtPath<AffinityDialogueData>(path);
            Assert.IsNotNull(dialogue, $"{path}를 불러올 수 없습니다.");
            Assert.IsNotNull(dialogue.bossTraitLines, "bossTraitLines가 비어 있습니다.");

            foreach (PlayerTrait trait in AllTraits)
            {
                TraitLine entry = dialogue.bossTraitLines.FirstOrDefault(t => t != null && t.trait == trait);
                Assert.IsNotNull(entry, $"{trait} 성향 줄이 없습니다.");
                Assert.IsNotNull(entry.line);
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.line.text), $"{trait} 성향 줄 문구가 비어 있습니다.");
            }
        }
    }
}
