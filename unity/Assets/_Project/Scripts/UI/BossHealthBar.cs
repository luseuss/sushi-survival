using UnityEngine;
using UnityEngine.UI;
using SushiSurvival.Enemies.Boss;

namespace SushiSurvival.UI
{
    /// <summary>
    /// 화면 상단에 고정되는 보스 체력바.
    ///
    /// EnemyBase에는 체력 변경 이벤트가 없어서 매 프레임 폴링한다. 보스는 한
    /// 마리뿐이라 비용이 문제되지 않고, EnemyBase에 이벤트를 추가하면 잡몹
    /// 수십 마리가 전부 그 비용을 내게 된다. 표시값은 목표를 향해 부드럽게
    /// 옮겨가서 스냅으로 깎이지 않는다.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [Tooltip("보이고 숨길 바 컨테이너. 반드시 이 스크립트가 붙은 오브젝트의 " +
                 "자식이어야 한다 — 자기 자신을 넣으면 스스로를 꺼서 다시 켜지 못한다.")]
        [SerializeField] private GameObject bar;
        [Tooltip("Image Type을 Filled로 설정한 채움 이미지.")]
        [SerializeField] private Image fillImage;
        [Tooltip("초당 채움 변화량. 2면 0→1이 0.5초 걸린다.")]
        [SerializeField] private float fillSpeed = 2f;

        private BossController _boss;
        private float _currentFill = 1f;

        private void Awake()
        {
            // 자기 자신을 끄면 Update가 멈춰서 Show()를 받을 수도, 체력을
            // 갱신할 수도 없게 된다. 조립 실수를 조용히 넘기지 않는다.
            if (bar == gameObject)
            {
                Debug.LogError($"{name}: bar에 자기 자신을 연결하면 체력바가 다시 켜지지 않습니다. " +
                               "자식 오브젝트를 연결하세요.");
                bar = null;
            }

            Hide();
        }

        public void Show(BossController boss)
        {
            _boss = boss;
            _currentFill = 1f; // 새 보스는 항상 가득 찬 채로 나타난다.

            if (bar != null)
                bar.SetActive(true);
        }

        public void Hide()
        {
            _boss = null;

            if (bar != null)
                bar.SetActive(false);
        }

        private void Update()
        {
            if (_boss == null || fillImage == null) return;

            float target = HealthBarLogic.ComputeFillAmount(_boss.CurrentHealth, _boss.MaxHealth);
            _currentFill = HealthBarLogic.MoveTowardsFill(_currentFill, target, fillSpeed * Time.deltaTime);
            fillImage.fillAmount = _currentFill;
        }
    }
}
