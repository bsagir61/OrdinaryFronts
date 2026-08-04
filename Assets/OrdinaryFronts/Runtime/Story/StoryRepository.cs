using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OrdinaryFronts
{
    public sealed class StoryRepository
    {
        public const string StoryFileName = "hamburg_1943.json";

        /// <summary>Türkçe dosyanın yolu; testler ve geriye dönük başvurular için korunur.</summary>
        public const string RelativeStoryPath = "Story/tr-TR/" + StoryFileName;

        public static string RelativePathFor(string locale)
        {
            return "Story/" + LocalizationService.Normalize(locale) + "/" + StoryFileName;
        }

        private readonly string explicitPath;
        private readonly string locale;
        private readonly Dictionary<string, StoryNode> index = new Dictionary<string, StoryNode>();

        public StoryDatabase Database { get; private set; }

        public StoryRepository(string path = null, string locale = null)
        {
            explicitPath = path;
            this.locale = LocalizationService.Normalize(locale);
        }

        public StoryDatabase Load()
        {
            string path = explicitPath ?? Path.Combine(Application.streamingAssetsPath, RelativePathFor(locale));
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
