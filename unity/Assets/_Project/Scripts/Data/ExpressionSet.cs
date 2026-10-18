using System;
using UnityEngine;

namespace SushiSurvival.Data
{
    /// <summary>
    /// 캐릭터 하나의 표정별 그림 모음. 대사 줄이나 호감도 대화가 이 모음을 가리키면, 줄의 표정에 맞는 그림으로 바뀐다.
    /// 비어 있는 표정은 줄에 직접 넣은 기본 그림을 그대로 쓰고 연출만 입힌다 — 그림이 한 장씩 늘어나도 데이터를 고칠 필요가 없다.
    /// </summary>
    [CreateAssetMenu(menuName = "SushiSurvival/Expression Set", fileName = "NewExpressionSet")]
    public class ExpressionSet : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public ExpressionType expression;
            [Tooltip("대사창 안 작은 초상화. 비우면 기본 그림을 쓴다.")]
            public Sprite portrait;
            [Tooltip("화면에 크게 서 있는 입상. 비우면 기본 그림을 쓴다.")]
            public Sprite standing;
        }

        public Entry[] entries;

        /// <summary>표정에 맞는 초상화. 세트가 없거나 해당 표정 그림이 없으면 fallback.</summary>
        public static Sprite PortraitOrDefault(ExpressionSet set, ExpressionType expression, Sprite fallback)
        {
            Entry entry = Find(set, expression);
            return entry != null && entry.portrait != null ? entry.portrait : fallback;
        }

        /// <summary>표정에 맞는 입상. 세트가 없거나 해당 표정 그림이 없으면 fallback.</summary>
        public static Sprite StandingOrDefault(ExpressionSet set, ExpressionType expression, Sprite fallback)
        {
            Entry entry = Find(set, expression);
            return entry != null && entry.standing != null ? entry.standing : fallback;
        }

        private static Entry Find(ExpressionSet set, ExpressionType expression)
        {
            if (set == null || set.entries == null) return null;

            foreach (Entry entry in set.entries)
            {
                if (entry != null && entry.expression == expression) return entry;
            }

            return null;
        }
    }
}
