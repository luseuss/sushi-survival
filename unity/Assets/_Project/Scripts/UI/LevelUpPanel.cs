using System;
using System.Collections.Generic;
using UnityEngine;
using SushiSurvival.Core;

namespace SushiSurvival.UI
{
    public class LevelUpPanel : MonoBehaviour
    {
        [Tooltip("팝업 루트. 비워두면 이 오브젝트 자신을 켜고 끈다.")]
        [SerializeField] private GameObject root;
        [Tooltip("선택지 버튼 3개.")]
        [SerializeField] private LevelUpOptionButton[] optionButtons;

        private GameObject Root => root != null ? root : gameObject;

        private void Awake() => Hide();

        public void Show(IReadOnlyList<IUpgradeOption> options, Action<IUpgradeOption> onChosen)
        {
            Root.SetActive(true);

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < options.Count)
                    optionButtons[i].Bind(options[i], onChosen);
                else
                    optionButtons[i].Clear();
            }
        }

        public void Hide() => Root.SetActive(false);
    }
}
