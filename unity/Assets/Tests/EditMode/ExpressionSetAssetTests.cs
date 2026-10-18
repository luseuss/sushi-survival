using System.Linq;
using NUnit.Framework;
using UnityEditor;
using SushiSurvival.Data;

namespace SushiSurvival.EditModeTests
{
    /// <summary>캐릭터별 표정 세트가 구조대로 들어갔는지 확인한다(그림 연결, 평상시 그림 존재, 표정 중복 없음).</summary>
    public class ExpressionSetAssetTests
    {
        private const string Folder = "Assets/_Project/Data/Expressions";

        private static ExpressionSet[] LoadAll()
        {
            return AssetDatabase.FindAssets("t:ExpressionSet", new[] { Folder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<ExpressionSet>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToArray();
        }

        [Test]
        public void Folder_HasASetForEachPlayableCharacter()
        {
            Assert.AreEqual(3, LoadAll().Length);
        }

        [Test]
        public void EverySet_HasNeutralArtAndNoEmptyOrDuplicateEntries()
        {
            foreach (ExpressionSet set in LoadAll())
            {
                Assert.IsNotNull(set.entries, set.name);
                Assert.IsTrue(set.entries.Any(e => e != null && e.expression == ExpressionType.Neutral), set.name + ": 평상시 그림 없음");

                foreach (ExpressionSet.Entry entry in set.entries)
                {
                    Assert.IsNotNull(entry.portrait, set.name + " " + entry.expression + ": 초상화 없음");
                    Assert.IsNotNull(entry.standing, set.name + " " + entry.expression + ": 입상 없음");
                }

                Assert.AreEqual(set.entries.Length, set.entries.Select(e => e.expression).Distinct().Count(),
                                set.name + ": 같은 표정이 두 번 들어 있음");
            }
        }
    }
}
