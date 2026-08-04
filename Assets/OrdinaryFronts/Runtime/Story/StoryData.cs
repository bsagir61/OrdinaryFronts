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
        public IntroData intro;
        public StoryNode[] nodes = Array.Empty<StoryNode>();
    }

    /// <summary>
    /// Yeni oyun başlarken ilk anlatı düğümünden önce oynatılan açılış kurgusu.
    /// Boş bırakılırsa oyun doğrudan ilk düğümle başlar.
    /// </summary>
    [Serializable]
    public sealed class IntroData
    {
        public IntroBeat[] beats = Array.Empty<IntroBeat>();

        public bool HasBeats { get { return beats != null && beats.Length > 0; } }

        /// <summary>
        /// Kart başına çapraz geçiş + belirme + kararma yükü. Gerçek değerler AppController'daki
        /// geçiş sabitlerinden gelir; buradaki yaklaşıklık, kurgunun toplam süresini otomatik
        /// olarak sınayabilmek içindir. Geçiş süreleri değişirse burası da güncellenmelidir.
        /// </summary>
        public const float PerBeatOverheadSeconds = 1.1f;
        public const float OpeningOverheadSeconds = 0.45f;

        /// <summary>Kurgunun atlanmadığı durumda yaklaşık toplam ekran süresi.</summary>
        public float EstimatedTotalSeconds
        {
            get
            {
                if (!HasBeats) return 0f;
                float total = OpeningOverheadSeconds;
                for (int i = 0; i < beats.Length; i++)
                    if (beats[i] != null) total += beats[i].ResolvedHold + PerBeatOverheadSeconds;
                return total;
            }
        }
    }

    [Serializable]
    public sealed class IntroBeat
    {
        public string imageKey;
        public string kicker;
        public string line;
        public float holdSeconds = 3.2f;

        public const float MinimumHold = 1.2f;
        public const float MaximumHold = 8f;

        /// <summary>JSON'da eksik veya saçma bir süre verilse bile okunabilir bir aralıkta kalır.</summary>
        public float ResolvedHold
        {
            get { return holdSeconds <= 0f ? 3.2f : (holdSeconds < MinimumHold ? MinimumHold : (holdSeconds > MaximumHold ? MaximumHold : holdSeconds)); }
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
