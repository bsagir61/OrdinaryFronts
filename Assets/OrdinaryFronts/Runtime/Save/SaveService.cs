using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OrdinaryFronts
{
    /// <summary>
    /// Oynanış kaydı, bölüm başına bir dosya (1.6): <c>save-&lt;bölüm&gt;.json</c>.
    /// <para>
    /// 1.5'e kadar bütün bölümler tek bir dosyayı paylaşıyordu. Bir bölüme başlamak öbürünün
    /// ilerlemesini siliyordu; daha kötüsü, oyun her açılışta ilk bölümü yüklediği için
    /// başka bir bölümün kaydından "Devam Et" denince kayıt uyumsuz sayılıyordu. Artık her
    /// bölüm kendi kaydını tutar; "Devam Et" en son oynanan bölümü açar.
    /// </para>
    /// <para>
    /// Eski tek dosya (<see cref="SaveFileName"/>) ilk açılışta içindeki bölümün adıyla yeni
    /// dosyaya taşınır. Okunamıyorsa yerinde bırakılır ve başka kayıt yoksa oyuncuya
    /// "okunamadı" olarak gösterilir; bozuk kayıt kanıt için silinmez.
    /// </para>
    /// </summary>
    public sealed class SaveService
    {
        // Sürüm 2: görünür durum çubukları (StatBlock) kaldırıldı.
        // Sürüm 3: izler bölüm etiketiyle saklanmaya başladı (final raporu için).
        // Eski sürüm kayıtları uyumsuz sayılır ve oyuncuya yeni oyun yolu sunulur.
        public const int CurrentSchemaVersion = 3;

        /// <summary>1.6'dan önceki tek kayıt dosyası; yalnız taşıma için okunur.</summary>
        public const string SaveFileName = "hamburg-demo-save.json";

        private const string SlotPrefix = "save-";
        private const string SlotSuffix = ".json";

        private readonly string directory;

        /// <summary>
        /// Kaydın ait olduğu bölüm. Kayıt yazıldığında bu bölüm olur; boşsa en son yazılan
        /// bölümün kaydı kullanılır.
        /// </summary>
        public string ActiveStoryId { get; set; }

        public SaveService(string customDirectory = null)
        {
            directory = customDirectory ?? Application.persistentDataPath;
            MigrateLegacy();
        }

        public static string FileNameFor(string storyId)
        {
            return SlotPrefix + StoryRepository.SanitizeStoryId(storyId) + SlotSuffix;
        }

        public string PathFor(string storyId)
        {
            return Path.Combine(directory, FileNameFor(storyId));
        }

        private string LegacyPath { get { return Path.Combine(directory, SaveFileName); } }

        /// <summary>Etkin bölümün kayıt yolu; bölüm seçilmemişse en son kayıt (ya da taşınamamış eski dosya).</summary>
        public string SavePath
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(ActiveStoryId)) return PathFor(ActiveStoryId);
                string recent = MostRecentStoryId();
                if (recent != null) return PathFor(recent);
                return LegacyPath;
            }
        }

        /// <summary>Herhangi bir bölümün kaydı var mı? Ana menüdeki "Devam Et" buna bakar.</summary>
        public bool HasSave { get { return MostRecentStoryId() != null || File.Exists(LegacyPath); } }

        public bool HasSaveFor(string storyId)
        {
            return File.Exists(PathFor(storyId));
        }

        /// <summary>Kayıtlı bölümler, en son yazılandan başlayarak.</summary>
        public List<string> SavedStoryIds()
        {
            List<string> ids = new List<string>();
            if (!Directory.Exists(directory)) return ids;
            List<KeyValuePair<DateTime, string>> found = new List<KeyValuePair<DateTime, string>>();
            foreach (string path in Directory.GetFiles(directory, SlotPrefix + "*" + SlotSuffix))
            {
                string name = Path.GetFileName(path);
                string id = name.Substring(SlotPrefix.Length, name.Length - SlotPrefix.Length - SlotSuffix.Length);
                if (string.IsNullOrEmpty(id) || StoryRepository.SanitizeStoryId(id) != id) continue;
                found.Add(new KeyValuePair<DateTime, string>(File.GetLastWriteTimeUtc(path), id));
            }
            found.Sort((a, b) => b.Key.CompareTo(a.Key));
            for (int i = 0; i < found.Count; i++) ids.Add(found[i].Value);
            return ids;
        }

        public string MostRecentStoryId()
        {
            List<string> ids = SavedStoryIds();
            return ids.Count == 0 ? null : ids[0];
        }

        public void Save(GameState state)
        {
            if (state == null) throw new ArgumentNullException("state");
            state.schemaVersion = CurrentSchemaVersion;
            ActiveStoryId = StoryRepository.SanitizeStoryId(state.storyId);
            string json = JsonUtility.ToJson(state, true);
            AtomicJsonFile.Write(PathFor(ActiveStoryId), json);
        }

        /// <summary>
        /// <paramref name="userMessageKey"/> doğrudan gösterilecek metni değil, arayüz dil
        /// tablosundaki anahtarı döndürür; böylece mesaj oyuncunun seçtiği dilde görünür.
        /// </summary>
        public bool TryLoad(out GameState state, out string userMessageKey)
        {
            return TryLoadPath(SavePath, out state, out userMessageKey);
        }

        /// <summary>Bir bölümün kaydına dokunmadan bakar (harita kartı için). Okunamıyorsa null.</summary>
        public GameState Peek(string storyId)
        {
            GameState state;
            string message;
            return TryLoadPath(PathFor(storyId), out state, out message) ? state : null;
        }

        private static bool TryLoadPath(string path, out GameState state, out string userMessageKey)
        {
            state = null;
            userMessageKey = string.Empty;
            if (!File.Exists(path))
            {
                userMessageKey = UiKey.SaveNone;
                return false;
            }
            try
            {
                string json = File.ReadAllText(path);
                state = JsonUtility.FromJson<GameState>(json);
                if (state == null || state.schemaVersion != CurrentSchemaVersion || string.IsNullOrWhiteSpace(state.currentNodeId))
                    throw new InvalidDataException("Kayıt şeması veya aktif düğüm geçersiz.");
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is InvalidDataException)
            {
                state = null;
                userMessageKey = UiKey.SaveUnreadable;
                return false;
            }
        }

        /// <summary>
        /// Eski tek kayıt dosyasını bölümünün dosyasına taşır. O bölümün yeni bir kaydı zaten
        /// varsa eski dosya daha eskidir ve yerinde bırakılır (başka kayıt varken kullanılmaz).
        /// </summary>
        private void MigrateLegacy()
        {
            try
            {
                if (!File.Exists(LegacyPath)) return;
                GameState state;
                string message;
                if (!TryLoadPath(LegacyPath, out state, out message) || string.IsNullOrWhiteSpace(state.storyId)) return;
                string target = PathFor(state.storyId);
                if (File.Exists(target)) return;
                File.Move(LegacyPath, target);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning("Eski kayıt taşınamadı: " + exception.Message);
            }
        }
    }
}
