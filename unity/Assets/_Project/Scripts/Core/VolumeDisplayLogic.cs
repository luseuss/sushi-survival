using UnityEngine;

namespace SushiSurvival.Core
{
    /// <summary>설정창 볼륨 슬라이더의 표시 값 계산.</summary>
    public static class VolumeDisplayLogic
    {
        /// <summary>0~1 볼륨을 "70%" 같은 글자로. 범위를 넘으면 0~100으로 맞춘다.</summary>
        public static string PercentText(float volume)
        {
            int percent = Mathf.RoundToInt(Mathf.Clamp01(volume) * 100f);
            return percent + "%";
        }
    }
}
