using System;
using System.Collections.Generic;

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
        public CharacterData[] characters = Array.Empty<CharacterData>();
        public StoryNode[] nodes = Array.Empty<StoryNode>();

        /// <summary>Perdelerin soruları; perde kartında adın altında durur. İsteğe bağlıdır.</summary>
        public ActData[] acts = Array.Empty<ActData>();

        /// <summary>Kırk yıl sonraki tanıklık; final kaydından önce oynanır. İsteğe bağlıdır.</summary>
        public TestimonyData testimony;

        public ActData FindAct(string name)
        {
            if (acts == null || string.IsNullOrEmpty(name)) return null;
            for (int i = 0; i < acts.Length; i++)
                if (acts[i] != null && acts[i].name == name) return acts[i];
            return null;
        }
    }

    /// <summary>
    /// Bir perdenin sorusu. Oyun soruyu sorar, cevaplamaz: cevap oyuncunun o perdede
    /// verdiği kararlardır. Soru bir slogan değil, iki seçeneğin de haklı olabildiği bir
    /// gerilimin adıdır.
    /// </summary>
    [Serializable]
    public sealed class ActData
    {
        public string name;
        public string question;
    }

    /// <summary>
    /// Tanıklık: bölüm bittikten kırk yıl sonra birisi başkarakterden o günleri anlatmasını
    /// ister. Oyuncu ne anlatılacağını seçer: yaptıklarını ya da yapmadıklarını. İkisi de
    /// doğrudur ve ikisi de eksiktir; tarih değişmez, değişen yalnız neyin aktarıldığıdır.
    /// <para>
    /// Satırlar birinci tekil kişiyle elle yazılır ve oynanışın bayraklarına bağlanır.
    /// Seçilen kipin koşulu tutan satırlarından en çok üçü, rotanın başını, ortasını ve
    /// sonunu kapsayacak biçimde seçilir; ardından o kipin kapanışı gelir.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class TestimonyData
    {
        /// <summary>Yer ve zaman, ör. "HAMBURG · KASIM 1983".</summary>
        public string kicker;

        /// <summary>Kim soruyor, nerede; kısa bir paragraf.</summary>
        public string setting;

        public string question;
        public string tellDone;
        public string tellUndone;
        public TestimonyLine[] done = Array.Empty<TestimonyLine>();
        public TestimonyLine[] undone = Array.Empty<TestimonyLine>();
        public string closeDone;
        public string closeUndone;

        public const int MaxLines = 3;

        public bool IsComplete
        {
            get
            {
                return !string.IsNullOrWhiteSpace(kicker) && !string.IsNullOrWhiteSpace(setting)
                    && !string.IsNullOrWhiteSpace(question) && !string.IsNullOrWhiteSpace(tellDone)
                    && !string.IsNullOrWhiteSpace(tellUndone) && !string.IsNullOrWhiteSpace(closeDone)
                    && !string.IsNullOrWhiteSpace(closeUndone)
                    && done != null && done.Length > 0 && undone != null && undone.Length > 0;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrWhiteSpace(kicker) && string.IsNullOrWhiteSpace(question)
                    && (done == null || done.Length == 0) && (undone == null || undone.Length == 0);
            }
        }

        /// <summary>
        /// Koşulu tutan satırlardan en çok <see cref="MaxLines"/> tanesi: ilk, orta ve son.
        /// Satırlar dosyada rota sırasıyla yazıldığı için bu seçim erken, orta ve geç bir
        /// kararı birlikte getirir; son satır hep bölümü bitiren karara aittir.
        /// </summary>
        public static List<string> Select(TestimonyLine[] lines, Func<ConditionData[], bool> holds)
        {
            List<string> result = new List<string>();
            foreach (int i in SelectIndices(lines, holds)) result.Add(lines[i].text.Trim());
            return result;
        }

        /// <summary>
        /// <see cref="Select"/> ile aynı seçim, satır dizini olarak. İki dilde satırlar aynı
        /// sırada ve aynı koşullarla yazıldığı için dizin dilden bağımsızdır; arşiv bunu saklar
        /// ve harita, son tanıklığı oyuncunun o anki dilinde yeniden okur.
        /// </summary>
        public static List<int> SelectIndices(TestimonyLine[] lines, Func<ConditionData[], bool> holds)
        {
            List<int> matching = new List<int>();
            if (lines != null)
                for (int i = 0; i < lines.Length; i++)
                    if (lines[i] != null && !string.IsNullOrWhiteSpace(lines[i].text) && holds(lines[i].conditions))
                        matching.Add(i);
            if (matching.Count <= MaxLines) return matching;
            return new List<int> { matching[0], matching[matching.Count / 2], matching[matching.Count - 1] };
        }
    }

    [Serializable]
    public sealed class TestimonyLine
    {
        public string text;
        public ConditionData[] conditions = Array.Empty<ConditionData>();
    }

    /// <summary>
    /// Bir <c>relation</c> anahtarının arkasındaki kişi. İlişki değerleri bugüne kadar
    /// birikiyor fakat hiçbir yere çıkmıyordu; final raporundaki "İnsanlar" bölümü bu
    /// tanımı kullanarak onları cümleye çevirir.
    /// <para>
    /// Sayı gösterilmez ve çubuk çizilmez: durum çubukları oyundan bilerek kaldırılmıştı,
    /// ilişkiyi bir puana çevirmek aynı hatayı geri getirirdi. Yalnız iki yön vardır ve
    /// ikisinin de cümlesi elle yazılır.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class CharacterData
    {
        public string key;
        public string name;

        /// <summary>İlişki olumluya kaydığında yazılacak satır.</summary>
        public string warm;

        /// <summary>İlişki olumsuza kaydığında yazılacak satır.</summary>
        public string cold;

        /// <summary>
        /// Bir kişinin rapora girmesi için gereken en küçük kayma. Tek bir küçük jest
        /// (±2, ±3) kimseyi "yanında" ya da "karşında" yapmaz.
        /// </summary>
        public const int Threshold = 5;

        public bool IsComplete
        {
            get
            {
                return !string.IsNullOrWhiteSpace(key)
                    && !string.IsNullOrWhiteSpace(name)
                    && !string.IsNullOrWhiteSpace(warm)
                    && !string.IsNullOrWhiteSpace(cold);
            }
        }
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

        /// <summary>
        /// Düğüme girilirken, metin gösterilmeden önce oynanan ara sahne. İsteğe bağlıdır;
        /// tanımlı değilse düğüm doğrudan kartla açılır.
        /// </summary>
        public InterludeData interlude;

        public bool IsEnding
        {
            get { return ending != null && !string.IsNullOrWhiteSpace(ending.id); }
        }

        public bool HasInterlude
        {
            get { return interlude != null && interlude.IsDefined; }
        }
    }

    /// <summary>
    /// Ara sahne: metnin anlatamadığını ellerin yaptığı kısa, animasyonlu bir an. Bir puan
    /// ya da başarı ölçüsü üretmez; oyuncunun o anda ne yaptığını bir iki bayrağa çevirir ve
    /// sonraki düğümlerin gecikmeli yankıları o bayraklara bakar. Her ara sahne atlanabilir;
    /// hareket azaltma açıkken hiç oynanmaz.
    /// </summary>
    [Serializable]
    public sealed class InterludeData
    {
        /// <summary>Bölüm içinde tekil kimlik; tamamlandığı bilgisi kayda bu adla yazılır.</summary>
        public string id;

        /// <summary>Sahnenin türü; <see cref="StoryVocabulary.InterludeKindWalk"/> gibi.</summary>
        public string kind;

        /// <summary>Sahne açılırken bir iki saniye gösterilen yer/durum satırı.</summary>
        public string caption;

        /// <summary>
        /// Karar sahnesi: düğüme girilirken değil, oyuncu seçim yapacağı anda oynar ve
        /// düğümün iki seçiminden birini oyuncunun hareketiyle verir. Bu durumda
        /// <see cref="results"/> seçim sırasıyla dizilir: ilk sonuç birinci, ikinci sonuç
        /// ikinci seçime karşılık gelir.
        /// </summary>
        public bool chooses;

        /// <summary>
        /// Sahnenin üretebileceği bayrakların tamamı. Doğrulayıcı bunları bilinen bayrak
        /// listesine ekler; böylece bir yankı henüz üretilmemiş bir bayrağa bağlanamaz.
        /// </summary>
        public string[] results = Array.Empty<string>();

        public bool IsDefined
        {
            get { return !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(kind); }
        }

        /// <summary>Hiçbir alanı dolu değil: JSON'da alan yoktu, ara sahne de yok.</summary>
        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(kind) && string.IsNullOrWhiteSpace(caption)
                    && !chooses && (results == null || results.Length == 0);
            }
        }
    }

    [Serializable]
    public sealed class ChoiceData
    {
        public string id;
        public string text;
        public string trace;
        public string nextNodeId;

        /// <summary>
        /// Bu seçim <b>alınmadığında</b> final raporunun "Yapılmayanlar" bölümünde yazılacak
        /// satır. İsteğe bağlıdır; yalnız ağırlığı olan seçeneklere yazılır. Boş bırakılanlar
        /// rapora girmez. Rapor böylece yalnız yapılanların değil, bırakılanların da kaydı olur.
        /// </summary>
        public string omission;

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
