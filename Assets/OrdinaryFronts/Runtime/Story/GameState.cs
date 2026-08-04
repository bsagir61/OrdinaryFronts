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
        public string[] traces = Array.Empty<string>();
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

        public void AddTrace(string trace)
        {
            if (string.IsNullOrWhiteSpace(trace) || Contains(traces, trace)) return;
            List<string> list = new List<string>(traces ?? Array.Empty<string>());
            list.Add(trace.Trim());
            if (list.Count > 8) list.RemoveAt(0);
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
