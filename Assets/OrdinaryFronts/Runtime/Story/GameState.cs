using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrdinaryFronts
{
    [Serializable]
    public sealed class GameState
    {
        public int schemaVersion = SaveService.CurrentSchemaVersion;
        public string storyId;
        public string activeChapter;
        public string currentNodeId;
        public BoolStateEntry[] flags = Array.Empty<BoolStateEntry>();
        public IntStateEntry[] relations = Array.Empty<IntStateEntry>();
        public string[] seenResults = Array.Empty<string>();
        public TraceEntry[] traces = Array.Empty<TraceEntry>();
        public bool completed;

        public static GameState Create(StoryDatabase story)
        {
            return new GameState
            {
                storyId = story.storyId,
                currentNodeId = story.startNodeId,
                completed = false
            };
        }

        public bool GetFlag(string key)
        {
            if (flags == null) return false;
            for (int i = 0; i < flags.Length; i++)
            {
                if (flags[i] != null && flags[i].key == key) return flags[i].value;
            }
            return false;
        }

        public void SetFlag(string key, bool value)
        {
            List<BoolStateEntry> list = new List<BoolStateEntry>(flags ?? Array.Empty<BoolStateEntry>());
            BoolStateEntry entry = list.Find(item => item != null && item.key == key);
            if (entry == null)
            {
                entry = new BoolStateEntry { key = key };
                list.Add(entry);
            }
            entry.value = value;
            flags = list.ToArray();
        }

        public int GetRelation(string key)
        {
            if (relations == null) return 0;
            for (int i = 0; i < relations.Length; i++)
            {
                if (relations[i] != null && relations[i].key == key) return relations[i].value;
            }
            return 0;
        }

        public void SetRelation(string key, int value)
        {
            List<IntStateEntry> list = new List<IntStateEntry>(relations ?? Array.Empty<IntStateEntry>());
            IntStateEntry entry = list.Find(item => item != null && item.key == key);
            if (entry == null)
            {
                entry = new IntStateEntry { key = key };
                list.Add(entry);
            }
            entry.value = Mathf.Clamp(value, -100, 100);
            relations = list.ToArray();
        }

        public bool HasSeenResult(string id)
        {
            return Contains(seenResults, id);
        }

        public void MarkResultSeen(string id)
        {
            if (!string.IsNullOrWhiteSpace(id) && !HasSeenResult(id)) seenResults = Append(seenResults, id);
        }

        /// <summary>
        /// Kararın izini, alındığı bölümle birlikte saklar. Eskiden yalnız son sekiz iz
        /// tutuluyordu; bir rota 14-18 karardan oluştuğu için bu, oyunun belirleyici erken
        /// kararlarını final raporuna hiç ulaşmadan siliyordu. Artık rotanın tamamı korunur.
        /// </summary>
        public void AddTrace(string act, string trace)
        {
            if (string.IsNullOrWhiteSpace(trace)) return;
            string text = trace.Trim();
            if (traces != null)
                for (int i = 0; i < traces.Length; i++)
                    if (traces[i] != null && traces[i].text == text) return;

            List<TraceEntry> list = new List<TraceEntry>(traces ?? Array.Empty<TraceEntry>());
            list.Add(new TraceEntry { act = act ?? string.Empty, text = text });
            // En uzun rota 18 karardır; üst sınır yalnız bozuk veriye karşı emniyettir.
            if (list.Count > 32) list.RemoveAt(0);
            traces = list.ToArray();
        }

        private static bool Contains(string[] values, string target)
        {
            if (values == null || string.IsNullOrEmpty(target)) return false;
            for (int i = 0; i < values.Length; i++) if (values[i] == target) return true;
            return false;
        }

        private static string[] Append(string[] values, string value)
        {
            List<string> list = new List<string>(values ?? Array.Empty<string>()) { value };
            return list.ToArray();
        }
    }

    /// <summary>Final raporunda bölümlere göre gruplanabilmesi için iz, bölümüyle saklanır.</summary>
    [Serializable]
    public sealed class TraceEntry
    {
        public string act;
        public string text;
    }

    [Serializable]
    public sealed class BoolStateEntry
    {
        public string key;
        public bool value;
    }

    [Serializable]
    public sealed class IntStateEntry
    {
        public string key;
        public int value;
    }
}
