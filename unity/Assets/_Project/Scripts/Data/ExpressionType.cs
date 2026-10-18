namespace SushiSurvival.Data
{
    /// <summary>대사 한 줄에서 캐릭터가 짓는 표정. 표정 그림이 없어도 연출(움직임·색·말풍선)로 보여준다.</summary>
    public enum ExpressionType
    {
        Neutral = 0,
        /// <summary>기쁨 — 통통 뛰며 ♪</summary>
        Happy,
        /// <summary>놀람 — 위로 튀어 오르며 !</summary>
        Surprised,
        /// <summary>화남 — 떨며 붉어지고 #</summary>
        Angry,
        /// <summary>슬픔 — 처지며 어두워지고 …</summary>
        Sad,
        /// <summary>당황 — 좌우로 흔들리고 ;;</summary>
        Embarrassed,
    }
}
