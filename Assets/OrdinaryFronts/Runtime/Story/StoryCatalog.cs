using System;
using System.IO;
using UnityEngine;

namespace OrdinaryFronts
{
    /// <summary>
    /// Antolojinin bölüm listesi. Oyun tek bir hikâyeden ibaret olmadığı için ana menü
    /// doğrudan Hamburg'u başlatmaz; önce bu katalogtan bir bölüm seçilir.
    /// Katalog dile göre ayrıdır, çünkü başlık ve tanıtım satırı çevrilir.
    /// </summary>
    [Serializable]
    public sealed class StoryCatalog
    {
        public const string FileName = "catalog.json";

        public string locale;
        public StoryCatalogEntry[] entries = Array.Empty<StoryCatalogEntry>();

        public static string RelativePathFor(string locale)
        {
            return "Story/" + LocalizationService.Normalize(locale) + "/" + FileName;
        }

        public static StoryCatalog Load(string locale, string streamingAssetsRootOverride = null)
        {
            string root = streamingAssetsRootOverride ?? Application.streamingAssetsPath;
            string path = Path.Combine(root, RelativePathFor(locale));
            if (!File.Exists(path)) throw new FileNotFoundException("Hikâye kataloğu bulunamadı.", path);

            StoryCatalog catalog = JsonUtility.FromJson<StoryCatalog>(File.ReadAllText(path));
            if (catalog == null || catalog.entries == null || catalog.entries.Length == 0)
                throw new InvalidDataException("Hikâye kataloğu okunamadı veya boş: " + path);
            return catalog;
        }

        /// <summary>Oynanabilir ilk bölüm; kayıt yokken varsayılan olarak bu kullanılır.</summary>
        public StoryCatalogEntry FirstAvailable()
        {
            for (int i = 0; i < entries.Length; i++)
                if (entries[i] != null && entries[i].IsPlayable) return entries[i];
            return null;
        }
    }

    [Serializable]
    public sealed class StoryCatalogEntry
    {
        public string storyId;
        public string title;
        public string period;
        public string line;

        /// <summary>
        /// Kartta gösterilecek sahne görselinin anahtarı. Hazırlanmakta olan bölümlerin
        /// görseli yoktur; kartları yalnız bir soru işareti taşır, çünkü hangi hikâyenin
        /// geleceği henüz belli değildir.
        /// </summary>
        public string imageKey;
        public bool available;

        /// <summary>
        /// Bölümün haritadaki yeri, gerçek enlem/boylam. Seçim ekranı kartlardan haritaya
        /// geçtiğinden beri oynanabilir her bölümün bir yeri olmalıdır; projeksiyon
        /// <see cref="MapProjection"/> ile yapılır.
        /// </summary>
        public float latitude;
        public float longitude;

        public bool HasLocation { get { return latitude != 0f || longitude != 0f; } }

        /// <summary>
        /// Bölümün geçtiği ay, "YYYY-AA" (1.7). Harita altındaki zaman şeridi bölümleri buna
        /// göre dizer; aynı sıra, bölümler arasında zamanda gezinmeyi de belirler.
        /// </summary>
        public string date;

        public bool TryGetDate(out int year, out int month)
        {
            year = 0;
            month = 0;
            if (string.IsNullOrWhiteSpace(date) || date.Length != 7 || date[4] != '-') return false;
            return int.TryParse(date.Substring(0, 4), out year) && int.TryParse(date.Substring(5, 2), out month)
                && month >= 1 && month <= 12;
        }

        /// <summary>Ay cinsinden sıra anahtarı; tarihi olmayan bölüm sona düşer.</summary>
        public int MonthIndex
        {
            get
            {
                int y, m;
                return TryGetDate(out y, out m) ? y * 12 + (m - 1) : int.MaxValue;
            }
        }

        /// <summary>
        /// Yalnız işaretlenmiş <em>ve</em> bir hikâye dosyasına bağlı bölümler oynanabilir.
        /// Hazırlanmakta olan bölümler kartta görünür fakat başlatılamaz.
        /// </summary>
        public bool IsPlayable
        {
            get { return available && !string.IsNullOrWhiteSpace(storyId); }
        }
    }
}
