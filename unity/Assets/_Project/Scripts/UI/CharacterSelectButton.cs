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
        [Tooltip("아직 구현되지 않은 캐릭터는 체크. 회색 처리되고, 클릭하면 선택 제한 안내로 이어진다.")]
        [SerializeField] private bool locked;
        [Tooltip("locked일 때 클릭하면 보여줄 안내 컨트롤러. locked가 아니면 안 쓴다.")]
        [SerializeField] private LockedCharacterController lockedCharacterController;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClicked);
        }

        private void Start()
        {
            if (portraitImage != null && characterData != null)
                portraitImage.sprite = characterData.selectCardSprite != null
                    ? characterData.selectCardSprite
                    : characterData.portraitSprite;

            if (locked && portraitImage != null)
                portraitImage.color = Color.gray;

            // 영상이 있는 캐릭터만 호버 재생을 붙인다. 없으면 지금처럼 그림만 보인다.
            if (characterData != null && characterData.hoverVideo != null && portraitImage != null
                && !TryGetComponent<HoverVideoPreview>(out _))
            {
                gameObject.AddComponent<HoverVideoPreview>().Init(portraitImage, characterData.hoverVideo);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            if (locked)
            {
                if (lockedCharacterController != null)
                    lockedCharacterController.Show(characterData);
                else
                    Debug.LogError($"{name}: lockedCharacterController가 비어 있어 제한 안내를 표시할 수 없습니다.");
                return;
            }

            GameManager.Instance.StartRun(characterData);
        }
    }
}
