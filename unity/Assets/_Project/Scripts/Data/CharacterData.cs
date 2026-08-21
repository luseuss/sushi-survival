using UnityEngine;

namespace SushiSurvival.Data
{
    [CreateAssetMenu(menuName = "SushiSurvival/Character Data", fileName = "NewCharacterData")]
    public class CharacterData : ScriptableObject
    {
        public string characterName;
        public Sprite portraitSprite;
        [Tooltip("이 캐릭터로 플레이할 때 생성할 프리팹. 캐릭터마다 무기·애니메이터가 다르므로 종류별로 따로 만든다.")]
        public GameObject playerPrefab;
        public float baseMoveSpeed = 3f;
        public float baseMaxHealth = 100f;
        public WeaponData weaponData;
        public RuntimeAnimatorController animatorController;
    }
}
