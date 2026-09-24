using UnityEngine;

namespace SushiSurvival.Data
{
    [CreateAssetMenu(menuName = "SushiSurvival/Character Data", fileName = "NewCharacterData")]
    public class CharacterData : ScriptableObject
    {
        public string characterName;
        [Tooltip("HUD·대화창 등에 쓰는 작은 정사각 초상화(180x180).")]
        public Sprite portraitSprite;
        [Tooltip("호감도 대화에서 크게 서 있는 입상 일러스트. 비워두면 portraitSprite로 대신 표시한다.")]
        public Sprite standingSprite;
        [Tooltip("캐릭터 선택 화면 버튼 전용 카드 아트. 비워두면 portraitSprite로 대신 표시한다.")]
        public Sprite selectCardSprite;
        [Tooltip("이 캐릭터로 플레이할 때 생성할 프리팹. 캐릭터마다 무기·애니메이터가 다르므로 종류별로 따로 만든다.")]
        public GameObject playerPrefab;
        public float baseMoveSpeed = 3f;
        public float baseMaxHealth = 100f;
        public WeaponData weaponData;
        public RuntimeAnimatorController animatorController;
        [Tooltip("호감도 대화 #1(런 시작 직전)·#2(보스전 진입 직전) 데이터. introLines/question1이 " +
                 "비어 있으면 대화 없이 바로 런이 시작되고, bossIntroLines가 비어 있으면 " +
                 "인터럽트 없이 바로 보스전으로 넘어간다.")]
        public AffinityDialogueData affinityDialogue;
        [Tooltip("locked 캐릭터를 클릭했을 때 보여줄 안내 문구. locked가 아니면 안 쓴다.")]
        public string lockedMessage;
    }
}
