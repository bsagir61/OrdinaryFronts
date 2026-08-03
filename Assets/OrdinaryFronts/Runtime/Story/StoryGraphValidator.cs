using System;
using System.Collections.Generic;

namespace OrdinaryFronts
{
    public static class StoryGraphValidator
    {
        public static List<string> Validate(StoryDatabase story)
        {
            List<string> issues = new List<string>();
            if (story == null)
            {
                issues.Add("Hikâye verisi yok.");
                return issues;
            }

            Dictionary<string, StoryNode> nodes = new Dictionary<string, StoryNode>();
            if (story.nodes == null) story.nodes = Array.Empty<StoryNode>();
            for (int i = 0; i < story.nodes.Length; i++)
            {
                StoryNode node = story.nodes[i];
                if (node == null || string.IsNullOrWhiteSpace(node.id))
                {
                    issues.Add("Boş kimlikli düğüm var.");
                    continue;
                }
                if (nodes.ContainsKey(node.id)) issues.Add("Yinelenen düğüm kimliği: " + node.id);
                else nodes.Add(node.id, node);
            }

            if (string.IsNullOrWhiteSpace(story.startNodeId) || !nodes.ContainsKey(story.startNodeId))
                issues.Add("Başlangıç düğümü bulunamadı: " + story.startNodeId);

            HashSet<string> knownFlags = new HashSet<string>();
            HashSet<string> knownRelations = new HashSet<string>();
            int endingCount = 0;
            foreach (KeyValuePair<string, StoryNode> pair in nodes)
            {
                StoryNode node = pair.Value;
                if (string.IsNullOrWhiteSpace(node.body) && !node.IsEnding) issues.Add("Boş gövdeli düğüm: " + node.id);
                if (node.IsEnding)
                {
                    endingCount++;
                    if (node.choices != null && node.choices.Length > 0) issues.Add("Final düğümünde seçim var: " + node.id);
                    if (string.IsNullOrWhiteSpace(node.ending.title)) issues.Add("Başlıksız final: " + node.id);
                    continue;
                }
                if (node.choices == null || node.choices.Length != 2)
                {
                    issues.Add("Karar düğümünde tam iki seçim yok: " + node.id);
                    continue;
                }
                for (int i = 0; i < node.choices.Length; i++)
                {
                    ChoiceData choice = node.choices[i];
                    if (choice == null || string.IsNullOrWhiteSpace(choice.text)) issues.Add("Boş seçim: " + node.id + " #" + i);
                    if (choice == null || string.IsNullOrWhiteSpace(choice.nextNodeId) || !nodes.ContainsKey(choice.nextNodeId))
                        issues.Add("Geçersiz next-node: " + node.id + " -> " + (choice == null ? "null" : choice.nextNodeId));
                    if (choice != null && choice.effects != null)
                    {
                        for (int e = 0; e < choice.effects.Length; e++)
                        {
                            EffectData effect = choice.effects[e];
                            if (effect == null || string.IsNullOrWhiteSpace(effect.key)) continue;
                            if (effect.type == "flag" || effect.type == "echo") knownFlags.Add(effect.key);
                            if (effect.type == "relation") knownRelations.Add(effect.key);
                        }
                    }
                }
            }

            if (endingCount < 5) issues.Add("Beşten az final var: " + endingCount);
            HashSet<string> reachable = ReachableNodeIds(story, nodes);
            foreach (string id in nodes.Keys) if (!reachable.Contains(id)) issues.Add("Ulaşılamayan düğüm: " + id);
            HashSet<string> reachableEndings = new HashSet<string>();
            foreach (string id in reachable) if (nodes[id].IsEnding) reachableEndings.Add(nodes[id].ending.id);
            if (reachableEndings.Count < 5) issues.Add("Erişilebilir final sayısı beşten az: " + reachableEndings.Count);

            foreach (StoryNode node in nodes.Values)
            {
                ValidateConditions(node.id, node.conditions, knownFlags, knownRelations, issues);
                if (node.echoes == null) continue;
                for (int i = 0; i < node.echoes.Length; i++)
                {
                    EchoData echo = node.echoes[i];
                    if (echo == null || string.IsNullOrWhiteSpace(echo.id) || string.IsNullOrWhiteSpace(echo.text))
                        issues.Add("Geçersiz gecikmeli yankı: " + node.id);
                    else
                        ValidateConditions(node.id + "/" + echo.id, echo.conditions, knownFlags, knownRelations, issues);
                }
            }
            return issues;
        }

        public static HashSet<string> ReachableNodeIds(StoryDatabase story)
        {
            Dictionary<string, StoryNode> nodes = new Dictionary<string, StoryNode>();
            if (story != null && story.nodes != null)
                for (int i = 0; i < story.nodes.Length; i++)
                    if (story.nodes[i] != null && !string.IsNullOrWhiteSpace(story.nodes[i].id) && !nodes.ContainsKey(story.nodes[i].id))
                        nodes.Add(story.nodes[i].id, story.nodes[i]);
            return ReachableNodeIds(story, nodes);
        }

        public static List<int> EndingDecisionCounts(StoryDatabase story)
        {
            Dictionary<string, StoryNode> nodes = new Dictionary<string, StoryNode>();
            List<int> result = new List<int>();
            if (story == null || story.nodes == null) return result;
            for (int i = 0; i < story.nodes.Length; i++)
                if (story.nodes[i] != null && !string.IsNullOrWhiteSpace(story.nodes[i].id) && !nodes.ContainsKey(story.nodes[i].id))
                    nodes.Add(story.nodes[i].id, story.nodes[i]);
            Walk(story.startNodeId, 0, nodes, new HashSet<string>(), result);
            return result;
        }

        private static HashSet<string> ReachableNodeIds(StoryDatabase story, Dictionary<string, StoryNode> nodes)
        {
            HashSet<string> visited = new HashSet<string>();
            if (story == null || !nodes.ContainsKey(story.startNodeId)) return visited;
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(story.startNodeId);
            while (queue.Count > 0)
            {
                string id = queue.Dequeue();
                if (!visited.Add(id)) continue;
                StoryNode node = nodes[id];
                if (node.choices == null) continue;
                for (int i = 0; i < node.choices.Length; i++)
                {
                    ChoiceData choice = node.choices[i];
                    if (choice != null && nodes.ContainsKey(choice.nextNodeId) && !visited.Contains(choice.nextNodeId)) queue.Enqueue(choice.nextNodeId);
                }
            }
            return visited;
        }

        private static void Walk(string id, int decisions, Dictionary<string, StoryNode> nodes, HashSet<string> path, List<int> result)
        {
            if (!nodes.ContainsKey(id) || path.Contains(id)) return;
            StoryNode node = nodes[id];
            if (node.IsEnding)
            {
                result.Add(decisions);
                return;
            }
            path.Add(id);
            if (node.choices != null)
                for (int i = 0; i < node.choices.Length; i++)
                    if (node.choices[i] != null) Walk(node.choices[i].nextNodeId, decisions + 1, nodes, path, result);
            path.Remove(id);
        }

        private static void ValidateConditions(string owner, ConditionData[] conditions, HashSet<string> flags, HashSet<string> relations, List<string> issues)
        {
            if (conditions == null) return;
            for (int i = 0; i < conditions.Length; i++)
            {
                ConditionData condition = conditions[i];
                if (condition == null || string.IsNullOrWhiteSpace(condition.key))
                {
                    issues.Add("Boş koşul: " + owner);
                    continue;
                }
                if ((condition.type == "flag" || condition.type == "echo") && !flags.Contains(condition.key))
                    issues.Add("Üretilmeyen bayrağa bağlı yankı/koşul: " + owner + " -> " + condition.key);
                if (condition.type == "relation" && !relations.Contains(condition.key))
                    issues.Add("Üretilmeyen ilişkiye bağlı koşul: " + owner + " -> " + condition.key);
                if (condition.type != "flag" && condition.type != "echo" && condition.type != "relation" && condition.type != "stat")
                    issues.Add("Bilinmeyen koşul türü: " + owner + " -> " + condition.type);
            }
        }
    }
}
