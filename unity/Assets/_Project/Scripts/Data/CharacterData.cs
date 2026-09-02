using UnityEngine;

namespace SushiSurvival.Data
{
    [CreateAssetMenu(menuName = "SushiSurvival/Character Data", fileName = "NewCharacterData")]
    public class CharacterData : ScriptableObject
    {
        public string characterName;
        public Sprite portraitSprite;
        [Tooltip("캐릭터 선택 화면 버튼 전용 카드 아트. 비워두면 portraitSprite로 대신 표시한다.")]
        public Sprite selectCardSprite;
        [Tooltip("이 캐릭터로 플레이할 때 생성할 프리팹. 캐릭터마다 무기·애니메이터가 다르므로 종류별로 따로 만든다.")]
        public GameObject playerPrefab;
        public float baseMoveSpeed = 3f;
        public float baseMaxHealth = 100f;
        public WeaponData weaponData;
        public RuntimeAnimatorController animatorController;
        [Tooltip("호감도 대화 #1 데이터. 비워두면 대화 없이 바로 런이 시작된다.")]
        public AffinityDialogueData affinityDialogue;
    }
}
