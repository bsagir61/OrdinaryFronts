using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OrdinaryFronts
{
    public sealed class StoryRepository
    {
        public const string DefaultStoryId = "hamburg_1943";
        public const string StoryFileName = DefaultStoryId + ".json";

        /// <summary>Türkçe dosyanın yolu; testler ve geriye dönük başvurular için korunur.</summary>
        public const string RelativeStoryPath = "Story/tr-TR/" + StoryFileName;

        /// <summary>
        /// Hikâye kimliği katalogtan gelir ve dosya yoluna girer. Katalog oyunla birlikte
        /// dağıtılsa da kimlik yalnız harf, rakam ve alt çizgiye indirgenir: yazım hatası
        /// veya kurcalanmış bir katalog dosya sisteminde gezinmeye dönüşmemelidir.
        /// </summary>
        public static string SanitizeStoryId(string storyId)
        {
            if (string.IsNullOrWhiteSpace(storyId)) return DefaultStoryId;
            System.Text.StringBuilder safe = new System.Text.StringBuilder(storyId.Length);
            for (int i = 0; i < storyId.Length; i++)
            {
                char c = storyId[i];
                bool allowed = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-';
                if (allowed) safe.Append(c);
            }
            return safe.Length == 0 ? DefaultStoryId : safe.ToString();
        }

        public static string RelativePathFor(string locale, string storyId = DefaultStoryId)
        {
            return "Story/" + LocalizationService.Normalize(locale) + "/" + SanitizeStoryId(storyId) + ".json";
        }

        private readonly string explicitPath;
        private readonly string locale;
        private readonly string storyId;
        private readonly Dictionary<string, StoryNode> index = new Dictionary<string, StoryNode>();

        public StoryDatabase Database { get; private set; }

        public StoryRepository(string path = null, string locale = null, string storyId = DefaultStoryId)
        {
            explicitPath = path;
            this.locale = LocalizationService.Normalize(locale);
            this.storyId = SanitizeStoryId(storyId);
        }

        public StoryDatabase Load()
        {
            string path = explicitPath ?? Path.Combine(Application.streamingAssetsPath, RelativePathFor(locale, storyId));
            if (!File.Exists(path)) throw new FileNotFoundException("Hikâye verisi bulunamadı.", path);
            string json = File.ReadAllText(path);
            StoryDatabase data = JsonUtility.FromJson<StoryDatabase>(json);
            if (data == null || data.nodes == null || data.nodes.Length == 0)
                throw new InvalidDataException("Hikâye verisi okunamadı veya boş.");

            index.Clear();
            for (int i = 0; i < data.nodes.Length; i++)
            {
                StoryNode node = data.nodes[i];
                if (node != null && !string.IsNullOrWhiteSpace(node.id) && !index.ContainsKey(node.id))
                    index.Add(node.id, node);
            }
            Database = data;
            return data;
        }

        public StoryNode GetNode(string id)
        {
            StoryNode node;
            return id != null && index.TryGetValue(id, out node) ? node : null;
        }
    }
}
