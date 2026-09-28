using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OrdinaryFronts
{
    /// <summary>
    /// Antolojinin kalıcı belleği. Tek bir oynanışın kaydından (<see cref="GameState"/>)
    /// farklı olarak bu dosya yeni oyunla silinmez; bölümler ve oynanışlar boyunca birikir.
    /// <para>
    /// Üç şeyi taşır ve üçü de oyunun tezine hizmet eder — <i>tarih değişmez, insanların
    /// kaderi değişebilir</i>:
    /// </para>
    /// <list type="bullet">
    /// <item><b>Tamamlanan bölümlerin bayrakları.</b> Bir bölümde yaptığın, başka bir
    /// bölümde yankı bulur. Nedensellik değil, örüntü: bir defteri Hamburg'da kurtaran
    /// oyuncuya Neretva'da biri defter uzattığında oyun bunu hatırlar.</item>
    /// <item><b>Düğüm başına son seçim.</b> Bir bölümü yeniden oynayan oyuncu, aynı düğüme
    /// geldiğinde geçen sefer ne yaptığını defterinde okur. İpucu değil, kayıt.</item>
    /// <item><b>Ulaşılan finaller.</b> Koleksiyon listesi değil; hangi finallerin bu
    /// oyuncunun eline geçtiğinin belleği.</item>
    /// </list>
    /// <para>
    /// Sayaç değildir, hiçbir ekranda yüzde ya da tamamlanma oranı göstermez. Bozuk dosya
    /// oyunu durdurmaz; arşiv boş sayılır ve oyun onsuz da eksiksiz oynanır.
    /// </para>
    /// </summary>
    public sealed class ArchiveService
    {
        public const int CurrentSchemaVersion = 1;
        public const string FileName = "ordinary-fronts-archive.json";

        private readonly string directory;
        private ArchiveData data;

        public string ArchivePath { get { return Path.Combine(directory, FileName); } }

        public ArchiveService(string customDirectory = null)
        {
            directory = customDirectory ?? Application.persistentDataPath;
        }

        /// <summary>Diskten okur; okunamıyorsa boş bir arşivle devam eder.</summary>
        public void Load()
        {
            data = null;
            if (File.Exists(ArchivePath))
            {
                try
                {
                    ArchiveData loaded = JsonUtility.FromJson<ArchiveData>(File.ReadAllText(ArchivePath));
                    if (loaded != null && loaded.schemaVersion == CurrentSchemaVersion) data = loaded;
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
                {
                    data = null;
                }
            }
            if (data == null) data = new ArchiveData();
        }

        private ArchiveData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        private ArchiveChapter ChapterFor(string storyId, bool create)
        {
            if (string.IsNullOrWhiteSpace(storyId)) return null;
            ArchiveChapter[] chapters = Data.chapters ?? Array.Empty<ArchiveChapter>();
            for (int i = 0; i < chapters.Length; i++)
                if (chapters[i] != null && chapters[i].storyId == storyId) return chapters[i];
            if (!create) return null;

            ArchiveChapter chapter = new ArchiveChapter { storyId = storyId };
            List<ArchiveChapter> list = new List<ArchiveChapter>(chapters) { chapter };
            Data.chapters = list.ToArray();
            return chapter;
        }

        // ------------------------------------------------------------- yazma

        /// <summary>Bir düğümde verilen kararı hatırlar; her seçimde çağrılır.</summary>
        public void RememberChoice(string storyId, string nodeId, string choiceId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || string.IsNullOrWhiteSpace(choiceId)) return;
            ArchiveChapter chapter = ChapterFor(storyId, true);
            if (chapter == null) return;
            chapter.lastChoices = Upsert(chapter.lastChoices, nodeId, choiceId);
            // Yol haritası için bütün oynanışların birleşimi: bir adım bir kez yazılır.
            string step = RouteMap.Step(nodeId, choiceId);
            if (Array.IndexOf(chapter.walked ?? Array.Empty<string>(), step) < 0)
            {
                List<string> walked = new List<string>(chapter.walked ?? Array.Empty<string>()) { step };
                chapter.walked = walked.ToArray();
            }
            Persist();
        }

        /// <summary>
        /// Bir bölüm tamamlandığında çağrılır: o oynanışın bayraklarını ve ulaşılan finali
        /// saklar. Bayraklar yalnız tamamlanan oynanışlardan alınır; yarım bırakılan bir
        /// oynanış oyuncunun "ne yaptığı" sayılmaz.
        /// </summary>
        public void RememberCompletion(GameState state, string endingId)
        {
            if (state == null) return;
            ArchiveChapter chapter = ChapterFor(state.storyId, true);
            if (chapter == null) return;

            List<BoolStateEntry> flags = new List<BoolStateEntry>();
            if (state.flags != null)
                for (int i = 0; i < state.flags.Length; i++)
                    if (state.flags[i] != null && !string.IsNullOrWhiteSpace(state.flags[i].key))
                        flags.Add(new BoolStateEntry { key = state.flags[i].key, value = state.flags[i].value });
            chapter.flags = flags.ToArray();

            if (!string.IsNullOrWhiteSpace(endingId) && Array.IndexOf(chapter.endingsReached ?? Array.Empty<string>(), endingId) < 0)
            {
                List<string> endings = new List<string>(chapter.endingsReached ?? Array.Empty<string>()) { endingId };
                chapter.endingsReached = endings.ToArray();
            }
            chapter.completedRuns++;
            Persist();
        }

        /// <summary>Bir kesişme yankısı ekranda göründü; iplik olarak hatırlanır.</summary>
        public void RememberCrossing(string storyId, string echoId)
        {
            if (string.IsNullOrWhiteSpace(storyId) || string.IsNullOrWhiteSpace(echoId)) return;
            string key = storyId + ">" + echoId;
            string[] seen = Data.crossings ?? Array.Empty<string>();
            if (Array.IndexOf(seen, key) >= 0) return;
            List<string> list = new List<string>(seen) { key };
            Data.crossings = list.ToArray();
            Persist();
        }

        /// <summary>Görülmüş kesişmeler, görülme sırasıyla.</summary>
        public string[] SeenCrossings()
        {
            return (string[])(Data.crossings ?? Array.Empty<string>()).Clone();
        }

        public void RememberTestimony(string storyId, string mode, int[] lines)
        {
            ArchiveChapter chapter = ChapterFor(storyId, true);
            if (chapter == null) return;
            chapter.testimonyMode = mode;
            chapter.testimonyLines = lines ?? Array.Empty<int>();
            Persist();
        }

        public bool TryGetTestimony(string storyId, out string mode, out int[] lines)
        {
            ArchiveChapter chapter = ChapterFor(storyId, false);
            mode = chapter == null ? null : chapter.testimonyMode;
            lines = chapter == null ? null : chapter.testimonyLines;
            return !string.IsNullOrWhiteSpace(mode) && lines != null && lines.Length > 0;
        }

        private void Persist()
        {
            try
            {
                Data.schemaVersion = CurrentSchemaVersion;
                AtomicJsonFile.Write(ArchivePath, JsonUtility.ToJson(Data, true));
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                // Arşiv yazılamazsa oyun durmaz; bellek yalnız bu oturumda kalır.
                Debug.LogWarning("Arşiv yazılamadı: " + exception.Message);
            }
        }

        // ------------------------------------------------------------- okuma

        /// <summary>Başka (ya da aynı) bölümün tamamlanmış oynanışındaki bayrak.</summary>
        public bool GetChapterFlag(string storyId, string flagKey)
        {
            ArchiveChapter chapter = ChapterFor(storyId, false);
            if (chapter == null || chapter.flags == null) return false;
            for (int i = 0; i < chapter.flags.Length; i++)
                if (chapter.flags[i] != null && chapter.flags[i].key == flagKey) return chapter.flags[i].value;
            return false;
        }

        public bool HasCompleted(string storyId)
        {
            ArchiveChapter chapter = ChapterFor(storyId, false);
            return chapter != null && chapter.completedRuns > 0;
        }

        /// <summary>Bu düğümde geçen sefer verilen karar; hiç verilmemişse boş.</summary>
        public string PreviousChoice(string storyId, string nodeId)
        {
            ArchiveChapter chapter = ChapterFor(storyId, false);
            if (chapter == null || chapter.lastChoices == null) return null;
            for (int i = 0; i < chapter.lastChoices.Length; i++)
                if (chapter.lastChoices[i] != null && chapter.lastChoices[i].key == nodeId) return chapter.lastChoices[i].value;
            return null;
        }

        /// <summary>Bu bölümde herhangi bir oynanışta atılmış bütün "düğüm>seçim" adımları.</summary>
        public string[] WalkedSteps(string storyId)
        {
            ArchiveChapter chapter = ChapterFor(storyId, false);
            if (chapter == null) return Array.Empty<string>();
            // 1.2 öncesi arşivlerde "walked" yoktur; son oynanışın kararları yine de yürünmüş sayılır.
            List<string> steps = new List<string>(chapter.walked ?? Array.Empty<string>());
            if (chapter.lastChoices != null)
                for (int i = 0; i < chapter.lastChoices.Length; i++)
                {
                    StringStateEntry entry = chapter.lastChoices[i];
                    if (entry == null) continue;
                    string step = RouteMap.Step(entry.key, entry.value);
                    if (!steps.Contains(step)) steps.Add(step);
                }
            return steps.ToArray();
        }

        public int CompletedRuns(string storyId)
        {
            ArchiveChapter chapter = ChapterFor(storyId, false);
            return chapter == null ? 0 : chapter.completedRuns;
        }

        private static StringStateEntry[] Upsert(StringStateEntry[] entries, string key, string value)
        {
            List<StringStateEntry> list = new List<StringStateEntry>(entries ?? Array.Empty<StringStateEntry>());
            StringStateEntry entry = list.Find(item => item != null && item.key == key);
            if (entry == null)
            {
                entry = new StringStateEntry { key = key };
                list.Add(entry);
            }
            entry.value = value;
            return list.ToArray();
        }
    }

    [Serializable]
    public sealed class ArchiveData
    {
        public int schemaVersion = ArchiveService.CurrentSchemaVersion;
        public ArchiveChapter[] chapters = Array.Empty<ArchiveChapter>();

        /// <summary>
        /// Oyuncunun gerçekten gördüğü kesişmeler, "bölüm>yankı" (1.7). Haritadaki kırmızı
        /// iplikler buradan çizilir. Eski arşivlerde alan yoktur; boş dizi kalır.
        /// </summary>
        public string[] crossings = Array.Empty<string>();
    }

    [Serializable]
    public sealed class ArchiveChapter
    {
        public string storyId;
        public int completedRuns;
        public string[] endingsReached = Array.Empty<string>();
        public BoolStateEntry[] flags = Array.Empty<BoolStateEntry>();
        public StringStateEntry[] lastChoices = Array.Empty<StringStateEntry>();

        /// <summary>Bütün oynanışlarda atılmış adımlar ("düğüm>seçim"); yol haritası için.</summary>
        public string[] walked = Array.Empty<string>();

        /// <summary>Son tanıklığın kipi ("done"/"undone") ve seçilen satırların dizinleri (1.7).</summary>
        public string testimonyMode;
        public int[] testimonyLines = Array.Empty<int>();
    }

    [Serializable]
    public sealed class StringStateEntry
    {
        public string key;
        public string value;
    }
}
