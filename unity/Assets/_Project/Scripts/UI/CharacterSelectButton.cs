using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Core;
using SushiSurvival.Data;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 캐릭터 선택 버튼 하나. 캐릭터가 3종으로 고정이라 동적 생성 대신
    /// 씬에 미리 배치하고 인스펙터에서 CharacterData를 연결한다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CharacterSelectButton : MonoBehaviour
    {
        [SerializeField] private CharacterData characterData;
        [Tooltip("캐릭터 초상화를 표시할 Image. 비워두면 표시하지 않는다.")]
        [SerializeField] private Image portraitImage;
        [Tooltip("아직 구현되지 않은 캐릭터는 체크. 회색 처리되고 선택할 수 없다.")]
        [SerializeField] private bool locked;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClicked);
        }

        private void Start()
        {
            if (portraitImage != null && characterData != null)
                portraitImage.sprite = characterData.portraitSprite;

            _button.interactable = !locked;

            if (locked && portraitImage != null)
                portraitImage.color = Color.gray;
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            if (locked) return;

            GameManager.Instance.StartRun(characterData);
        }
    }
}
