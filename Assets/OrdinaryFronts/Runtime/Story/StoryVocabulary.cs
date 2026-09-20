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

        /// <summary>
        /// Başka bir bölümün tamamlanmış oynanışındaki bayrak. Anahtar biçimi
        /// <c>storyId:flagKey</c>; örneğin <c>hamburg_1943:worker_notebook_saved</c>.
        /// Kesişmeler bu türle yazılır: bir bölümde yapılan, diğerinde yankı bulur.
        /// </summary>
        public const string TypeArchive = "archive";
        public const char ArchiveKeySeparator = ':';

        /// <summary>
        /// Ara sahne türleri. "walk": rüzgâra karşı yük itme; oyuncu itmeyi sürdürür ya da
        /// bırakır, sonuç iki bayraktan biridir.
        /// </summary>
        public const string InterludeKindWalk = "walk";

        /// <summary>Karar sahnesi: sığınak merdiveninde lambayı onarmak ya da sırayı indirmek.</summary>
        public const string InterludeKindLamp = "lamp";

        /// <summary>Karar sahnesi: ıslak kirişte denge ve kayma anında tutmak ya da bırakmak.</summary>
        public const string InterludeKindPlank = "plank";

        public static bool IsKnownInterludeKind(string kind)
        {
            string normalized = Normalize(kind);
            return normalized == InterludeKindWalk || normalized == InterludeKindLamp || normalized == InterludeKindPlank;
        }

        /// <summary>Ara sahnenin tamamlandığını kayda yazan sonuç kimliği.</summary>
        public static string InterludeSeenKey(string interludeId)
        {
            return "interlude:" + (interludeId ?? string.Empty).Trim();
        }

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

        public static bool IsArchiveType(string type)
        {
            return Normalize(type) == TypeArchive;
        }

        public static bool IsKnownType(string type)
        {
            return IsFlagType(type) || IsNumericType(type) || IsArchiveType(type);
        }

        /// <summary>Arşiv anahtarını bölüm ve bayrak olarak ayırır; biçim bozuksa false.</summary>
        public static bool TrySplitArchiveKey(string key, out string storyId, out string flagKey)
        {
            storyId = null;
            flagKey = null;
            if (string.IsNullOrWhiteSpace(key)) return false;
            int index = key.IndexOf(ArchiveKeySeparator);
            if (index <= 0 || index >= key.Length - 1) return false;
            storyId = key.Substring(0, index).Trim();
            flagKey = key.Substring(index + 1).Trim();
            return storyId.Length > 0 && flagKey.Length > 0;
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
