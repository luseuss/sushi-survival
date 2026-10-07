using UnityEngine;
using UnityEngine.InputSystem;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 테스트용 치트 키 입력. 씬에 배치하지 않고 첫 씬이 로드될 때 스스로 하나만 만들어 씬이 바뀌어도 남는다.
    /// ` 키를 누르면 지금 씬의 LevelSystem을 한 레벨 올린다.
    /// </summary>
    public class CheatInput : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<CheatInput>() != null) return;

            var host = new GameObject("CheatInput");
            DontDestroyOnLoad(host);
            host.AddComponent<CheatInput>();
            Debug.Log("[CheatInput] 준비됨 — ` 키: 레벨업");
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.backquoteKey.wasPressedThisFrame) return;

            // 레벨업 패널이 LevelSystem과 같은 오브젝트라 평소엔 꺼져 있다 — 꺼진 것도 찾아야 한다.
            LevelSystem levelSystem = FindObjectOfType<LevelSystem>(true);
            if (levelSystem == null)
            {
                Debug.Log("[CheatInput] 이 씬에 LevelSystem이 없어 레벨업할 수 없습니다.");
                return;
            }

            levelSystem.CheatLevelUp();
        }
    }
}
