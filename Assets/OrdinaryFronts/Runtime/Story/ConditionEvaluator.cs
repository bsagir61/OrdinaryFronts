namespace OrdinaryFronts
{
    public static class ConditionEvaluator
    {
        public static bool EvaluateAll(ConditionData[] conditions, GameState state)
        {
            return EvaluateAll(conditions, state, null);
        }

        /// <summary>
        /// <paramref name="archive"/> verilmezse arşiv koşulları sağlanmamış sayılır. Bu,
        /// kapalı-güvenli davranıştır: arşivsiz bir ortamda (testler, bozuk dosya) hiçbir
        /// kesişme yanlışlıkla tetiklenmez.
        /// </summary>
        public static bool EvaluateAll(ConditionData[] conditions, GameState state, ArchiveService archive)
        {
            if (conditions == null || conditions.Length == 0) return true;
            for (int i = 0; i < conditions.Length; i++)
            {
                if (!Evaluate(conditions[i], state, archive)) return false;
            }
            return true;
        }

        public static bool Evaluate(ConditionData condition, GameState state)
        {
            return Evaluate(condition, state, null);
        }

        public static bool Evaluate(ConditionData condition, GameState state, ArchiveService archive)
        {
            if (condition == null || state == null) return false;
            string type = StoryVocabulary.Normalize(condition.type);
            string operation = StoryVocabulary.ConditionOperationOrDefault(condition.op);
            // Tanınmayan operasyonda kapalı-güvenli davranırız: koşul sağlanmamış sayılır.
            if (!StoryVocabulary.IsKnownConditionOperation(type, operation)) return false;

            if (StoryVocabulary.IsArchiveType(type))
            {
                string storyId, flagKey;
                if (archive == null || !StoryVocabulary.TrySplitArchiveKey(condition.key, out storyId, out flagKey)) return false;
                // Kesişme yalnız tamamlanmış bir bölüme dayanır; yarım kalan oynanış sayılmaz.
                if (!archive.HasCompleted(storyId)) return false;
                bool actualFlag = archive.GetChapterFlag(storyId, flagKey);
                return operation == StoryVocabulary.OperationNotEquals ? actualFlag != condition.boolValue : actualFlag == condition.boolValue;
            }

            if (StoryVocabulary.IsFlagType(type))
            {
                bool actual = state.GetFlag(condition.key);
                return operation == StoryVocabulary.OperationNotEquals ? actual != condition.boolValue : actual == condition.boolValue;
            }

            if (type != StoryVocabulary.TypeRelation) return false;
            int value = state.GetRelation(condition.key);

            switch (operation)
            {
                case StoryVocabulary.OperationAtLeast: return value >= condition.intValue;
                case StoryVocabulary.OperationAtMost: return value <= condition.intValue;
                case StoryVocabulary.OperationNotEquals: return value != condition.intValue;
                case StoryVocabulary.OperationEquals: return value == condition.intValue;
                default: return false;
            }
        }
    }
}
