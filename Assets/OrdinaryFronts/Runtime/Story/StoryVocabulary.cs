namespace OrdinaryFronts
{
    /// <summary>
    /// Hikâye verisindeki tür ve operasyon adları için tek doğruluk kaynağı.
    /// ConditionEvaluator, EffectResolver ve StoryGraphValidator aynı sözlüğü kullanır;
    /// böylece JSON'daki bir yazım hatası sessiz davranış değişikliği yerine doğrulama hatası üretir.
    /// </summary>
    public static class StoryVocabulary
    {
        public const string TypeFlag = "flag";
        public const string TypeEcho = "echo";
        public const string TypeRelation = "relation";

        public const string OperationAdd = "add";
        public const string OperationSet = "set";
        public const string OperationEquals = "equals";
        public const string OperationNotEquals = "notequals";
        public const string OperationAtLeast = "atleast";
        public const string OperationAtMost = "atmost";

        public static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        /// <summary>Boş bırakılan koşul operasyonu "equals" sayılır.</summary>
        public static string ConditionOperationOrDefault(string op)
        {
            string normalized = Normalize(op);
            return normalized.Length == 0 ? OperationEquals : normalized;
        }

        /// <summary>Boş bırakılan etki operasyonu "set" sayılır.</summary>
        public static string EffectOperationOrDefault(string op)
        {
            string normalized = Normalize(op);
            return normalized.Length == 0 ? OperationSet : normalized;
        }

        public static bool IsFlagType(string type)
        {
            string normalized = Normalize(type);
            return normalized == TypeFlag || normalized == TypeEcho;
        }

        /// <summary>Sayısal karşılaştırma yapılabilen tek tür ilişkilerdir.</summary>
        public static bool IsNumericType(string type)
        {
            return Normalize(type) == TypeRelation;
        }

        public static bool IsKnownType(string type)
        {
            return IsFlagType(type) || IsNumericType(type);
        }

        /// <summary>Bayrak koşulları yalnız eşitlik karşılaştırır; sayısal koşullar ayrıca eşik kullanabilir.</summary>
        public static bool IsKnownConditionOperation(string type, string op)
        {
            string operation = ConditionOperationOrDefault(op);
            if (operation == OperationEquals || operation == OperationNotEquals) return true;
            return IsNumericType(type) && (operation == OperationAtLeast || operation == OperationAtMost);
        }

        /// <summary>Bütün etki türleri yalnız "add" ve "set" kullanır.</summary>
        public static bool IsKnownEffectOperation(string op)
        {
            string operation = EffectOperationOrDefault(op);
            return operation == OperationAdd || operation == OperationSet;
        }
    }
}
