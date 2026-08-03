using System;

namespace OrdinaryFronts
{
    [Serializable]
    public sealed class StoryDatabase
    {
        public int schemaVersion = 1;
        public string storyId;
        public string locale;
        public string startNodeId;
        public StatBlock initialStats = new StatBlock();
        public StoryNode[] nodes = Array.Empty<StoryNode>();
    }

    [Serializable]
    public sealed class StatBlock
    {
        public int resilience = 62;
        public int supplies = 46;
        public int bonds = 42;
        public int surveillance = 18;

        public StatBlock Clone()
        {
            return new StatBlock
            {
                resilience = resilience,
                supplies = supplies,
                bonds = bonds,
                surveillance = surveillance
            };
        }

        public void Clamp()
        {
            resilience = ClampValue(resilience);
            supplies = ClampValue(supplies);
            bonds = ClampValue(bonds);
            surveillance = ClampValue(surveillance);
        }

        public int Get(string key)
        {
            switch (key)
            {
                case "resilience": return resilience;
                case "supplies": return supplies;
                case "bonds": return bonds;
                case "surveillance": return surveillance;
                default: return 0;
            }
        }

        public void Set(string key, int value)
        {
            value = ClampValue(value);
            switch (key)
            {
                case "resilience": resilience = value; break;
                case "supplies": supplies = value; break;
                case "bonds": bonds = value; break;
                case "surveillance": surveillance = value; break;
            }
        }

        private static int ClampValue(int value)
        {
            return value < 0 ? 0 : value > 100 ? 100 : value;
        }
    }

    [Serializable]
    public sealed class StoryNode
    {
        public string id;
        public string act;
        public string date;
        public string location;
        public string imageKey;
        public string body;
        public ConditionData[] conditions = Array.Empty<ConditionData>();
        public EchoData[] echoes = Array.Empty<EchoData>();
        public ChoiceData[] choices = Array.Empty<ChoiceData>();
        public EndingData ending;

        public bool IsEnding
        {
            get { return ending != null && !string.IsNullOrWhiteSpace(ending.id); }
        }
    }

    [Serializable]
    public sealed class ChoiceData
    {
        public string id;
        public string text;
        public string trace;
        public string nextNodeId;
        public ConditionData[] conditions = Array.Empty<ConditionData>();
        public EffectData[] effects = Array.Empty<EffectData>();
    }

    [Serializable]
    public sealed class ConditionData
    {
        public string type;
        public string key;
        public string op;
        public bool boolValue;
        public int intValue;
    }

    [Serializable]
    public sealed class EffectData
    {
        public string type;
        public string key;
        public string op;
        public bool boolValue;
        public int intValue;
        public string text;
    }

    [Serializable]
    public sealed class EchoData
    {
        public string id;
        public string text;
        public ConditionData[] conditions = Array.Empty<ConditionData>();
    }

    [Serializable]
    public sealed class EndingData
    {
        public string id;
        public string title;
        public string[] paragraphs = Array.Empty<string>();
        public string[] traceFallbacks = Array.Empty<string>();
    }
}
