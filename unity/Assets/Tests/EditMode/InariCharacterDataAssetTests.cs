using NUnit.Framework;
using UnityEditor;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    /// <summary>이나리 CharacterData 에셋이 선택 제한 흐름에 필요한 값을 갖췄는지 확인한다.</summary>
    public class InariCharacterDataAssetTests
    {
        private const string AssetPath = "Assets/_Project/Data/InariCharacterData.asset";

        private static CharacterData Load()
        {
            var data = AssetDatabase.LoadAssetAtPath<CharacterData>(AssetPath);
            Assert.IsNotNull(data, $"{AssetPath}를 불러올 수 없습니다.");
            return data;
        }

        [Test]
        public void CharacterName_IsInari()
        {
            Assert.AreEqual("이나리", Load().characterName);
        }

        [Test]
        public void LockedMessage_MatchesConfirmedText()
        {
            Assert.AreEqual("이나리는 특정 캐릭터 디자인 이슈로 선택이 제한됩니다.", Load().lockedMessage);
        }

        [Test]
        public void SelectCardSprite_IsAssigned()
        {
            Assert.IsNotNull(Load().selectCardSprite, "캐릭터 선택 화면에 쓸 카드 아트가 비어 있습니다.");
        }
    }
}
