using NUnit.Framework;
using SushiSurvival.Audio;

namespace SushiSurvival.EditModeTests
{
    public class ExternalBgmPathTests
    {
        [Test]
        public void StoryBgm_UsesTheDocumentedFileName()
        {
            // README.txt와 .gitignore가 설명하는 이름과 같아야 한다.
            Assert.AreEqual("Audio/External/Bgm_Story", AudioLogic.ExternalBgmPath(BgmId.Story));
        }

        [Test]
        public void EveryBgmId_GetsItsOwnPathUnderTheExternalFolder()
        {
            var seen = new System.Collections.Generic.HashSet<string>();

            foreach (BgmId id in System.Enum.GetValues(typeof(BgmId)))
            {
                string path = AudioLogic.ExternalBgmPath(id);

                StringAssert.StartsWith("Audio/External/Bgm_", path);
                Assert.IsTrue(seen.Add(path), id + ": 다른 BGM과 경로가 겹침");
            }
        }
    }
}
