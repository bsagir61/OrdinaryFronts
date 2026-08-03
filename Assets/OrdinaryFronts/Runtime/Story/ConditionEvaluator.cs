using System;

namespace OrdinaryFronts
{
    public static class ConditionEvaluator
    {
        public static bool EvaluateAll(ConditionData[] conditions, GameState state)
        {
            if (conditions == null || conditions.Length == 0) return true;
            for (int i = 0; i < conditions.Length; i++)
            {
                if (!Evaluate(conditions[i], state)) return false;
            }
            return true;
        }

        public static bool Evaluate(ConditionData condition, GameState state)
        {
            if (condition == null || state == null) return false;
            string type = (condition.type ?? string.Empty).Trim().ToLowerInvariant();
            string operation = (condition.op ?? "equals").Trim().ToLowerInvariant();
            if (type == "flag" || type == "echo")
            {
                bool actual = state.GetFlag(condition.key);
                return operation == "notequals" ? actual != condition.boolValue : actual == condition.boolValue;
            }

            int value;
            if (type == "relation") value = state.GetRelation(condition.key);
            else if (type == "stat") value = state.stats.Get(condition.key);
            else return false;

            switch (operation)
            {
                case "atleast": return value >= condition.intValue;
                case "atmost": return value <= condition.intValue;
                case "notequals": return value != condition.intValue;
                case "equals": return value == condition.intValue;
                default: return false;
            }
        }
    }
}
