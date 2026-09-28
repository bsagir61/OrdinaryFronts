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
                        ValidateEffects(node.id + " #" + i, choice.effects, issues);
                        for (int e = 0; e < choice.effects.Length; e++)
                        {
                            EffectData effect = choice.effects[e];
                            if (effect == null || string.IsNullOrWhiteSpace(effect.key)) continue;
                            if (StoryVocabulary.IsFlagType(effect.type)) knownFlags.Add(effect.key);
                            if (StoryVocabulary.Normalize(effect.type) == StoryVocabulary.TypeRelation) knownRelations.Add(effect.key);
                        }
                    }
                }
            }

            ValidateInterludes(nodes, knownFlags, issues);
            if (endingCount < 5) issues.Add("Beşten az final var: " + endingCount);
            HashSet<string> reachable = ReachableNodeIds(story, nodes);
            foreach (string id in nodes.Keys) if (!reachable.Contains(id)) issues.Add("Ulaşılamayan düğüm: " + id);
            HashSet<string> reachableEndings = new HashSet<string>();
            foreach (string id in reachable) if (nodes[id].IsEnding) reachableEndings.Add(nodes[id].ending.id);
            if (reachableEndings.Count < 5) issues.Add("Erişilebilir final sayısı beşten az: " + reachableEndings.Count);

            foreach (StoryNode node in nodes.Values)
            {
                ValidateConditions(node.id, node.conditions, knownFlags, knownRelations, issues);
                if (node.choices != null)
                {
                    for (int i = 0; i < node.choices.Length; i++)
                    {
                        ChoiceData choice = node.choices[i];
                        if (choice != null)
                            ValidateConditions(node.id + " #" + i, choice.conditions, knownFlags, knownRelations, issues);
                    }
                }
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

            ValidateIntro(story.intro, nodes, issues);
            ValidateCharacters(story, knownRelations, issues);
            ValidateActs(story, nodes, issues);
            ValidateTestimony(story.testimony, knownFlags, knownRelations, issues);
            return issues;
        }

        /// <summary>
        /// Perde soruları isteğe bağlıdır; tanımlıysa her biri gerçekten var olan bir perdeye
        /// bağlanmalı ve bir soru taşımalıdır. Yazım hatası, kartta sorunun sessizce
        /// görünmemesi demek olurdu.
        /// </summary>
        private static void ValidateActs(StoryDatabase story, Dictionary<string, StoryNode> nodes, List<string> issues)
        {
            if (story.acts == null || story.acts.Length == 0) return;
            HashSet<string> actNames = new HashSet<string>();
            foreach (StoryNode node in nodes.Values) if (!string.IsNullOrWhiteSpace(node.act)) actNames.Add(node.act);
            HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < story.acts.Length; i++)
            {
                ActData act = story.acts[i];
                if (act == null || string.IsNullOrWhiteSpace(act.name) || string.IsNullOrWhiteSpace(act.question))
                {
                    issues.Add("Eksik perde sorusu: #" + i);
                    continue;
                }
                if (!seen.Add(act.name)) issues.Add("Yinelenen perde sorusu: " + act.name);
                if (!actNames.Contains(act.name)) issues.Add("Hiçbir düğümde geçmeyen perde: " + act.name);
            }
        }

        /// <summary>
        /// Tanıklık isteğe bağlıdır; tanımlıysa eksiksiz olmalı ve satırları yalnız bu
        /// bölümde gerçekten üretilen bayraklara bağlanmalıdır.
        /// </summary>
        private static void ValidateTestimony(TestimonyData testimony, HashSet<string> flags, HashSet<string> relations, List<string> issues)
        {
            if (testimony == null || testimony.IsEmpty) return;
            if (!testimony.IsComplete)
            {
                issues.Add("Eksik tanıklık tanımı.");
                return;
            }
            ValidateTestimonyLines("tanıklık/yapılanlar", testimony.done, flags, relations, issues);
            ValidateTestimonyLines("tanıklık/yapılmayanlar", testimony.undone, flags, relations, issues);
        }

        private static void ValidateTestimonyLines(string owner, TestimonyLine[] lines, HashSet<string> flags, HashSet<string> relations, List<string> issues)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                TestimonyLine line = lines[i];
                if (line == null || string.IsNullOrWhiteSpace(line.text))
                {
                    issues.Add("Boş tanıklık satırı: " + owner + " #" + i);
                    continue;
                }
                if (line.conditions == null || line.conditions.Length == 0)
                    issues.Add("Koşulsuz tanıklık satırı: " + owner + " #" + i);
                ValidateConditions(owner + " #" + i, line.conditions, flags, relations, issues);
            }
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
                if (!StoryVocabulary.IsKnownType(condition.type))
                {
                    issues.Add("Bilinmeyen koşul türü: " + owner + " -> " + condition.type);
                    continue;
                }
                if (!StoryVocabulary.IsKnownConditionOperation(condition.type, condition.op))
                    issues.Add("Bilinmeyen koşul operasyonu: " + owner + " -> " + condition.type + "/" + condition.op);

                string type = StoryVocabulary.Normalize(condition.type);
                if (StoryVocabulary.IsArchiveType(type))
                {
                    // Arşiv koşulu başka bir bölümün bayrağına bakar; bu dosyanın bayrak
                    // listesiyle denetlenemez, ama anahtar biçimi denetlenir.
                    string archiveStory, archiveFlag;
                    if (!StoryVocabulary.TrySplitArchiveKey(condition.key, out archiveStory, out archiveFlag))
                        issues.Add("Arşiv anahtarı 'bölüm:bayrak' biçiminde olmalı: " + owner + " -> " + condition.key);
                    else if (archiveStory == null || StoryRepository.SanitizeStoryId(archiveStory) != archiveStory)
                        issues.Add("Arşiv anahtarındaki bölüm kimliği geçersiz: " + owner + " -> " + condition.key);
                    continue;
                }
                if (StoryVocabulary.IsFlagType(type) && !flags.Contains(condition.key))
                    issues.Add("Üretilmeyen bayrağa bağlı yankı/koşul: " + owner + " -> " + condition.key);
                if (type == StoryVocabulary.TypeRelation && !relations.Contains(condition.key))
                    issues.Add("Üretilmeyen ilişkiye bağlı koşul: " + owner + " -> " + condition.key);
            }
        }

        /// <summary>
        /// Açılış kurgusu isteğe bağlıdır, fakat tanımlıysa her kartın metni ve düğümlerde
        /// fiilen kullanılan bir görsel anahtarı olmalıdır; yazım hatası sessizce boş bir
        /// arka plana düşmemelidir.
        /// </summary>
        private static void ValidateIntro(IntroData intro, Dictionary<string, StoryNode> nodes, List<string> issues)
        {
            if (intro == null || !intro.HasBeats) return;
            HashSet<string> imageKeys = new HashSet<string>();
            foreach (StoryNode node in nodes.Values)
                if (!string.IsNullOrWhiteSpace(node.imageKey)) imageKeys.Add(node.imageKey);

            for (int i = 0; i < intro.beats.Length; i++)
            {
                IntroBeat beat = intro.beats[i];
                string owner = "açılış #" + i;
                if (beat == null || string.IsNullOrWhiteSpace(beat.line))
                {
                    issues.Add("Boş açılış kartı: " + owner);
                    continue;
                }
                if (string.IsNullOrWhiteSpace(beat.imageKey) || !imageKeys.Contains(beat.imageKey))
                    issues.Add("Açılış kartında bilinmeyen görsel anahtarı: " + owner + " -> " + beat.imageKey);
            }
        }

        /// <summary>
        /// Kişi tanımları isteğe bağlıdır, fakat tanımlıysa eksiksiz olmalı ve hikâyede
        /// fiilen kullanılan bir ilişki anahtarına bağlanmalıdır. Yazım hatası, final
        /// raporunda o kişiyi sessizce hiç göstermezdi.
        /// </summary>
        private static void ValidateCharacters(StoryDatabase story, HashSet<string> knownRelations, List<string> issues)
        {
            if (story.characters == null || story.characters.Length == 0) return;
            HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < story.characters.Length; i++)
            {
                CharacterData character = story.characters[i];
                if (character == null || !character.IsComplete)
                {
                    issues.Add("Eksik kişi tanımı: #" + i);
                    continue;
                }
                if (!seen.Add(character.key)) issues.Add("Yinelenen kişi anahtarı: " + character.key);
                if (!knownRelations.Contains(character.key))
                    issues.Add("Hiçbir seçimde üretilmeyen ilişkiye bağlı kişi: " + character.key);
            }
        }

        /// <summary>
        /// Ara sahneler isteğe bağlıdır; tanımlıysa türü kodda karşılığı olan bir tür, kimliği
        /// bölüm içinde tekil olmalı ve ürettiği bayraklar bilinen bayraklara katılmalıdır.
        /// Yazım hatası olan bir tür, oyunda sahnenin sessizce atlanması demek olurdu.
        /// </summary>
        private static void ValidateInterludes(Dictionary<string, StoryNode> nodes, HashSet<string> knownFlags, List<string> issues)
        {
            HashSet<string> ids = new HashSet<string>();
            foreach (StoryNode node in nodes.Values)
            {
                // JsonUtility, alan JSON'da hiç yoksa bile boş bir nesne üretir; boş nesne
                // "ara sahne yok" demektir, yarım doldurulmuş nesne ise hata.
                if (node.interlude == null || node.interlude.IsEmpty) continue;
                InterludeData interlude = node.interlude;
                if (!interlude.IsDefined)
                {
                    issues.Add("Eksik ara sahne tanımı: " + node.id);
                    continue;
                }
                if (node.IsEnding) issues.Add("Final düğümünde ara sahne: " + node.id);
                if (!StoryVocabulary.IsKnownInterludeKind(interlude.kind))
                    issues.Add("Bilinmeyen ara sahne türü: " + node.id + " -> " + interlude.kind);
                if (!ids.Add(interlude.id)) issues.Add("Yinelenen ara sahne kimliği: " + interlude.id);
                if (interlude.results == null || interlude.results.Length == 0)
                    issues.Add("Sonuç üretmeyen ara sahne: " + node.id);
                else if (interlude.chooses && interlude.results.Length < 2)
                    issues.Add("Karar sahnesi iki seçime iki sonuç ister: " + node.id);
                else
                    for (int i = 0; i < interlude.results.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(interlude.results[i])) issues.Add("Boş ara sahne sonucu: " + node.id);
                        else knownFlags.Add(interlude.results[i]);
                    }
            }
        }

        private static void ValidateEffects(string owner, EffectData[] effects, List<string> issues)
        {
            if (effects == null) return;
            for (int i = 0; i < effects.Length; i++)
            {
                EffectData effect = effects[i];
                if (effect == null || string.IsNullOrWhiteSpace(effect.key))
                {
                    issues.Add("Boş etki: " + owner);
                    continue;
                }
                if (!StoryVocabulary.IsKnownType(effect.type))
                {
                    issues.Add("Bilinmeyen etki türü: " + owner + " -> " + effect.type);
                    continue;
                }
                if (!StoryVocabulary.IsKnownEffectOperation(effect.op))
                    issues.Add("Bilinmeyen etki operasyonu: " + owner + " -> " + effect.type + "/" + effect.op);
            }
        }
    }
}
