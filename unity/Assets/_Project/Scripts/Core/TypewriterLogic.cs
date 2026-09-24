using System;
using System.Text;

namespace SushiSurvival.Core
{
    /// <summary>
    /// 대사 타자기 연출의 계산. 리치 텍스트 태그(&lt;color=...&gt; 등)는 글자로 세지 않고
    /// 통째로 통과시킨다 — 태그 한가운데서 잘리면 화면에 태그 문자열이 그대로 보이기 때문이다.
    /// </summary>
    public static class TypewriterLogic
    {
        /// <summary>경과 시간에 맞춰 드러낼 글자 수. 속도가 0 이하면 전부 한 번에 보인다.</summary>
        public static int RevealedCount(float elapsedSeconds, float charsPerSecond, int totalVisible)
        {
            if (totalVisible <= 0) return 0;
            if (charsPerSecond <= 0f) return totalVisible;
            if (elapsedSeconds <= 0f) return 0;

            int count = (int)(elapsedSeconds * charsPerSecond);
            return Math.Min(count, totalVisible);
        }

        /// <summary>태그를 뺀, 실제로 화면에 찍히는 글자 수.</summary>
        public static int CountVisible(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            int count = 0;
            int i = 0;
            while (i < text.Length)
            {
                if (TryGetTagEnd(text, i, out int end))
                {
                    i = end + 1;
                    continue;
                }

                count++;
                i++;
            }

            return count;
        }

        /// <summary>앞에서부터 visibleCount글자만 보이는 문자열. 그 앞에 나온 태그는 모두 포함한다.</summary>
        public static string Substring(string text, int visibleCount)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (visibleCount <= 0) return string.Empty;

            var builder = new StringBuilder(text.Length);
            int shown = 0;
            int i = 0;
            while (i < text.Length)
            {
                if (TryGetTagEnd(text, i, out int end))
                {
                    builder.Append(text, i, end - i + 1);
                    i = end + 1;
                    continue;
                }

                if (shown >= visibleCount) break;

                builder.Append(text[i]);
                shown++;
                i++;
            }

            return builder.ToString();
        }

        // "a < b" 같은 부등호를 태그로 오인하지 않도록 <b>, </b>, <color=#fff>, <size=20> 꼴만 태그로 본다.
        private static bool TryGetTagEnd(string text, int start, out int end)
        {
            end = -1;
            if (text[start] != '<') return false;

            int close = text.IndexOf('>', start + 1);
            if (close < 0 || close == start + 1) return false;

            int nameStart = start + 1;
            if (text[nameStart] == '/') nameStart++;
            if (nameStart >= close || !char.IsLetter(text[nameStart])) return false;

            for (int k = nameStart; k < close; k++)
            {
                char c = text[k];
                if (c == '<' || c == '\n') return false;
            }

            end = close;
            return true;
        }
    }
}
