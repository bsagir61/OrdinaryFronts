using System;
using System.Collections.Generic;

namespace OrdinaryFronts
{
    /// <summary>Haritadaki bir durağın durumu.</summary>
    public enum RouteStopState
    {
        /// <summary>Hiç yürünmemiş ama yürünmüş bir durağın bir adım ötesinde: "yürünmemiş yol".</summary>
        Untaken,
        /// <summary>Önceki oynanışlardan birinde yürünmüş.</summary>
        Walked,
        /// <summary>Bu oynanışın yolunda.</summary>
        Current
    }

    public sealed class RouteStop
    {
        public StoryNode node;
        public int depth;
        public int row;
        public int rowCount;
        public RouteStopState state;
        public bool IsEnding { get { return node != null && node.IsEnding; } }
    }

    public sealed class RouteEdge
    {
        public RouteStop from;
        public RouteStop to;
        public int choiceIndex;
        /// <summary>Herhangi bir oynanışta seçilmiş.</summary>
        public bool walked;
        /// <summary>Bu oynanışta seçilmiş.</summary>
        public bool current;
    }

    /// <summary>
    /// Bölümün yol haritası: oyuncunun yürüdüğü duraklar, bu oynanışın izi ve yürünmüş her
    /// durağın bir adım ötesindeki yürünmemiş yollar. Daha ötesi gösterilmez.
    /// <para>
    /// Bu bir tamamlama listesi değildir. Kaç final olduğu, yüzde, sayaç ya da kilit simgesi
    /// yoktur; yürünmemiş bir yolun ne olduğu, nereye çıktığı söylenmez. Harita yalnız
    /// oyuncunun kendi yaptığını ve yapmadığını gösterir: "Yapılmayanlar" raporunun grafik
    /// karşılığı. GDD §12'deki ilke (tekrar oynama merakı koleksiyondan değil, bedelleri
    /// karşılaştırmaktan gelir) böylece korunur.
    /// </para>
    /// <para>
    /// Yerleşim: bir durağın sütunu, başlangıçtan ona giden <b>en uzun</b> yolun karar
    /// sayısıdır. En kısa yol kullanılsaydı yeniden birleşen dallarda kenarlar geriye (sola)
    /// giderdi; en uzun yolda her kenar sağa gider.
    /// </para>
    /// </summary>
    public sealed class RouteMap
    {
        public const char StepSeparator = '>';

        public readonly List<RouteStop> stops = new List<RouteStop>();
        public readonly List<RouteEdge> edges = new List<RouteEdge>();
        public int maxDepth;

        public static string Step(string nodeId, string choiceId)
        {
            return (nodeId ?? string.Empty) + StepSeparator + (choiceId ?? string.Empty);
        }

        /// <summary>Başlangıçtan erişilebilen her düğüm için en uzun yol derinliği.</summary>
        public static Dictionary<string, int> Depths(StoryDatabase story)
        {
            Dictionary<string, StoryNode> nodes = Index(story);
            Dictionary<string, int> depth = new Dictionary<string, int>();
            if (story == null || string.IsNullOrEmpty(story.startNodeId) || !nodes.ContainsKey(story.startNodeId)) return depth;

            HashSet<string> reachable = StoryGraphValidator.ReachableNodeIds(story);
            Dictionary<string, int> incoming = new Dictionary<string, int>();
            foreach (string id in reachable) incoming[id] = 0;
            foreach (string id in reachable)
                foreach (string next in Children(nodes[id]))
                    if (reachable.Contains(next)) incoming[next]++;

            Queue<string> queue = new Queue<string>();
            foreach (KeyValuePair<string, int> pair in incoming)
                if (pair.Value == 0) { queue.Enqueue(pair.Key); depth[pair.Key] = 0; }
            while (queue.Count > 0)
            {
                string id = queue.Dequeue();
                foreach (string next in Children(nodes[id]))
                {
                    if (!reachable.Contains(next)) continue;
                    int candidate = depth[id] + 1;
                    int known;
                    if (!depth.TryGetValue(next, out known) || candidate > known) depth[next] = candidate;
                    incoming[next]--;
                    if (incoming[next] == 0) queue.Enqueue(next);
                }
            }
            // Döngü olursa (doğrulayıcı izin vermese de) kalan düğümler yine yerleştirilir.
            foreach (string id in reachable)
                if (!depth.ContainsKey(id)) depth[id] = 0;
            return depth;
        }

        /// <param name="walkedSteps">Arşivdeki bütün oynanışlardan "düğüm>seçim" adımları.</param>
        /// <param name="currentSteps">Bu oynanışın adımları, sırasıyla.</param>
        /// <param name="currentNodeId">Oyuncunun şu anki düğümü (final olabilir).</param>
        public static RouteMap Build(StoryDatabase story, IEnumerable<string> walkedSteps, IEnumerable<string> currentSteps, string currentNodeId)
        {
            RouteMap map = new RouteMap();
            Dictionary<string, StoryNode> nodes = Index(story);
            if (story == null || !nodes.ContainsKey(story.startNodeId ?? string.Empty)) return map;
            Dictionary<string, int> depths = Depths(story);

            HashSet<string> walked = new HashSet<string>(walkedSteps ?? Array.Empty<string>());
            HashSet<string> current = new HashSet<string>(currentSteps ?? Array.Empty<string>());
            walked.UnionWith(current);

            HashSet<string> visited = new HashSet<string> { story.startNodeId };
            HashSet<string> onCurrentPath = new HashSet<string> { story.startNodeId };
            foreach (StoryNode node in nodes.Values)
            {
                if (node.choices == null) continue;
                for (int i = 0; i < node.choices.Length; i++)
                {
                    ChoiceData choice = node.choices[i];
                    if (choice == null) continue;
                    string step = Step(node.id, choice.id);
                    if (walked.Contains(step)) visited.Add(choice.nextNodeId);
                    if (current.Contains(step)) onCurrentPath.Add(choice.nextNodeId);
                }
            }
            if (!string.IsNullOrEmpty(currentNodeId) && nodes.ContainsKey(currentNodeId))
            {
                visited.Add(currentNodeId);
                onCurrentPath.Add(currentNodeId);
            }
            // Yalnız başlangıçtan gerçekten yürünerek varılabilen duraklar: yarım kalmış eski
            // kayıtlardan gelen kopuk bir adım haritada havada asılı durmamalı.
            visited.IntersectWith(ConnectedFromStart(story, nodes, walked, currentNodeId));
            onCurrentPath.IntersectWith(visited);

            Dictionary<string, RouteStop> byId = new Dictionary<string, RouteStop>();
            RouteStop Stop(string id, RouteStopState state)
            {
                RouteStop existing;
                if (byId.TryGetValue(id, out existing))
                {
                    if (state > existing.state) existing.state = state;
                    return existing;
                }
                int d;
                RouteStop stop = new RouteStop { node = nodes[id], depth = depths.TryGetValue(id, out d) ? d : 0, state = state };
                byId[id] = stop;
                map.stops.Add(stop);
                return stop;
            }

            foreach (StoryNode node in story.nodes)
            {
                if (node == null || !visited.Contains(node.id)) continue;
                Stop(node.id, onCurrentPath.Contains(node.id) ? RouteStopState.Current : RouteStopState.Walked);
            }
            foreach (StoryNode node in story.nodes)
            {
                if (node == null || !visited.Contains(node.id) || node.choices == null) continue;
                RouteStop from = byId[node.id];
                for (int i = 0; i < node.choices.Length; i++)
                {
                    ChoiceData choice = node.choices[i];
                    if (choice == null || !nodes.ContainsKey(choice.nextNodeId ?? string.Empty)) continue;
                    string step = Step(node.id, choice.id);
                    bool isWalked = walked.Contains(step);
                    bool isCurrent = current.Contains(step);
                    RouteStop to = visited.Contains(choice.nextNodeId)
                        ? byId[choice.nextNodeId]
                        : Stop(choice.nextNodeId, RouteStopState.Untaken);
                    map.edges.Add(new RouteEdge { from = from, to = to, choiceIndex = i, walked = isWalked, current = isCurrent });
                }
            }

            // Sütun içi sıra: dosyadaki sıraya göre; boş sütunlar olabilir, sorun değil.
            Dictionary<int, List<RouteStop>> columns = new Dictionary<int, List<RouteStop>>();
            List<string> order = new List<string>();
            for (int i = 0; i < story.nodes.Length; i++) if (story.nodes[i] != null) order.Add(story.nodes[i].id);
            map.stops.Sort((a, b) => a.depth != b.depth ? a.depth.CompareTo(b.depth) : order.IndexOf(a.node.id).CompareTo(order.IndexOf(b.node.id)));
            foreach (RouteStop stop in map.stops)
            {
                List<RouteStop> column;
                if (!columns.TryGetValue(stop.depth, out column)) columns[stop.depth] = column = new List<RouteStop>();
                stop.row = column.Count;
                column.Add(stop);
                if (stop.depth > map.maxDepth) map.maxDepth = stop.depth;
            }
            foreach (List<RouteStop> column in columns.Values)
                foreach (RouteStop stop in column) stop.rowCount = column.Count;
            return map;
        }

        private static HashSet<string> ConnectedFromStart(StoryDatabase story, Dictionary<string, StoryNode> nodes, HashSet<string> walked, string currentNodeId)
        {
            HashSet<string> seen = new HashSet<string>();
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(story.startNodeId);
            while (queue.Count > 0)
            {
                string id = queue.Dequeue();
                if (!seen.Add(id)) continue;
                StoryNode node = nodes[id];
                if (node.choices == null) continue;
                for (int i = 0; i < node.choices.Length; i++)
                {
                    ChoiceData choice = node.choices[i];
                    if (choice != null && walked.Contains(Step(node.id, choice.id)) && nodes.ContainsKey(choice.nextNodeId ?? string.Empty))
                        queue.Enqueue(choice.nextNodeId);
                }
            }
            if (!string.IsNullOrEmpty(currentNodeId)) seen.Add(currentNodeId);
            return seen;
        }

        private static IEnumerable<string> Children(StoryNode node)
        {
            if (node == null || node.choices == null) yield break;
            for (int i = 0; i < node.choices.Length; i++)
                if (node.choices[i] != null && !string.IsNullOrEmpty(node.choices[i].nextNodeId)) yield return node.choices[i].nextNodeId;
        }

        private static Dictionary<string, StoryNode> Index(StoryDatabase story)
        {
            Dictionary<string, StoryNode> nodes = new Dictionary<string, StoryNode>();
            if (story == null || story.nodes == null) return nodes;
            for (int i = 0; i < story.nodes.Length; i++)
                if (story.nodes[i] != null && !string.IsNullOrEmpty(story.nodes[i].id) && !nodes.ContainsKey(story.nodes[i].id))
                    nodes.Add(story.nodes[i].id, story.nodes[i]);
            return nodes;
        }
    }
}
