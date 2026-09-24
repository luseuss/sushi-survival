using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 씬이 로드될 때마다 모든 Button(비활성 포함)에 ButtonJuice를 붙인다. 버튼이 씬 여러 개와
    /// 프리팹에 흩어져 있어서 하나씩 배선하는 대신 한 곳에서 일괄 처리한다.
    /// LevelUpOptionButton은 자체 호버 연출이 있어 제외한다.
    /// </summary>
    public static class ButtonJuiceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            AttachAll();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AttachAll();

        private static void AttachAll()
        {
            foreach (Button button in Object.FindObjectsOfType<Button>(true))
            {
                if (button.GetComponent<ButtonJuice>() != null) continue;
                if (button.GetComponent<LevelUpOptionButton>() != null) continue;

                button.gameObject.AddComponent<ButtonJuice>();
            }
        }
    }
}
