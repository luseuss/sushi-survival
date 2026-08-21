using UnityEngine;
using SushiSurvival.Data;
using SushiSurvival.Weapons;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 선택된 캐릭터의 프리팹을 생성한다. 캐릭터마다 무기·애니메이터 구성이
    /// 다르므로 프리팹을 통째로 바꿔 끼우는 방식을 쓴다.
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        [Tooltip("비워두면 원점(0,0)에서 생성한다.")]
        [SerializeField] private Transform spawnPoint;
        [Tooltip("투사체 무기를 쓰는 캐릭터에게 스폰 직후 주입한다. 프리팹은 씬 오브젝트를 직접 참조할 수 없기 때문.")]
        [SerializeField] private GameObjectPool projectilePool;

        public GameObject Spawn(CharacterData characterData)
        {
            if (characterData == null)
            {
                Debug.LogError($"{name}: characterData가 null이라 플레이어를 생성할 수 없습니다.");
                return null;
            }

            if (characterData.playerPrefab == null)
            {
                Debug.LogError($"{characterData.name}: playerPrefab이 비어 있어 플레이어를 생성할 수 없습니다.");
                return null;
            }

            Vector3 position = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            GameObject player = Instantiate(characterData.playerPrefab, position, Quaternion.identity);

            if (player.TryGetComponent<ShrimpRifleWeapon>(out var rifle))
                rifle.SetProjectilePool(projectilePool);

            return player;
        }
    }
}
