using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace OrdinaryFronts.Tests.EditMode
{
    public sealed class StoryGraphTests
    {
        private StoryDatabase story;
        private Dictionary<string, StoryNode> nodes;

        [SetUp]
        public void SetUp()
        {
            string path = Path.Combine(Application.dataPath, "StreamingAssets", StoryRepository.RelativeStoryPath);
            Assert.That(File.Exists(path), "Hikâye JSON'u bulunamadı: " + path);
            story = JsonUtility.FromJson<StoryDatabase>(File.ReadAllText(path));
            Assert.That(story, Is.Not.Null);
            nodes = new Dictionary<string, StoryNode>();
            for (int i = 0; i < story.nodes.Length; i++)
            {
                StoryNode node = story.nodes[i];
                Assert.That(node, Is.Not.Null);
                Assert.That(nodes.ContainsKey(node.id), Is.False, "Yinelenen düğüm: " + node.id);
                nodes.Add(node.id, node);
            }
        }

        [Test]
        public void StoryGraph_HasNoValidationIssues()
        {
            List<string> issues = StoryGraphValidator.Validate(story);
            Assert.That(issues, Is.Empty, string.Join("\n", issues.ToArray()));
        }

        [Test]
        public void StoryGraph_MeetsNodeChoiceAndEndingCounts()
        {
            Assert.That(story.nodes.Length, Is.GreaterThanOrEqualTo(28));
            int endings = 0;
            int delayedEchoes = 0;
            for (int i = 0; i < story.nodes.Length; i++)
            {
                StoryNode node = story.nodes[i];
                if (node.IsEnding)
                {
                    endings++;
                    Assert.That(node.ending.paragraphs.Length, Is.InRange(2, 4), node.id);
                    Assert.That(node.ending.traceFallbacks.Length, Is.GreaterThanOrEqualTo(3), node.id);
                }
                else
                {
                    Assert.That(node.choices, Has.Length.EqualTo(2), node.id);
                    Assert.That(node.body, Is.Not.Empty, node.id);
                    for (int choice = 0; choice < 2; choice++)
                    {
                        Assert.That(node.choices[choice].text, Is.Not.Empty, node.id);
                        Assert.That(nodes.ContainsKey(node.choices[choice].nextNodeId), Is.True, node.id);
                    }
                }
                delayedEchoes += node.echoes == null ? 0 : node.echoes.Length;
            }
            Assert.That(endings, Is.GreaterThanOrEqualTo(5));
            Assert.That(delayedEchoes, Is.GreaterThanOrEqualTo(8));
        }

        [Test]
        public void StoryGraph_AllNodesAndAtLeastFiveEndingsAreReachable()
        {
            HashSet<string> reachable = StoryGraphValidator.ReachableNodeIds(story);
            Assert.That(reachable.Count, Is.EqualTo(nodes.Count));
            HashSet<string> endings = new HashSet<string>();
            foreach (string id in reachable) if (nodes[id].IsEnding) endings.Add(nodes[id].ending.id);
            Assert.That(endings.Count, Is.GreaterThanOrEqualTo(5));
        }

        [Test]
        public void StoryGraph_AllPlayableRoutesHaveFourteenToEighteenDecisions()
        {
            List<int> counts = StoryGraphValidator.EndingDecisionCounts(story);
            Assert.That(counts, Is.Not.Empty);
            Assert.That(counts, Has.All.InRange(14, 18), "Karar uzunlukları: " + string.Join(", ", counts));
        }

        [Test]
        public void StoryGraph_FirstDelayedEchoCanAppearByFourthNode()
        {
            Queue<NodeDepth> queue = new Queue<NodeDepth>();
            HashSet<string> visited = new HashSet<string>();
            queue.Enqueue(new NodeDepth(story.startNodeId, 1));
            bool found = false;
            while (queue.Count > 0)
            {
                NodeDepth item = queue.Dequeue();
                if (!visited.Add(item.id) || item.depth > 4) continue;
                StoryNode node = nodes[item.id];
                if (node.echoes != null && node.echoes.Length > 0) { found = true; break; }
                if (node.choices == null) continue;
                for (int i = 0; i < node.choices.Length; i++) queue.Enqueue(new NodeDepth(node.choices[i].nextNodeId, item.depth + 1));
            }
            Assert.That(found, Is.True, "İlk dört düğüm içinde gecikmeli yankı yok.");
        }

        [Test]
        public void StoryGraph_ConditionsAndEffectsReachAtLeastFiveEndings()
        {
            HashSet<string> endings = new HashSet<string>();
            GameState initial = GameState.Create(story);
            Explore(story.startNodeId, initial, 0, endings);
            Assert.That(endings.Count, Is.GreaterThanOrEqualTo(5), "Koşullar uygulandığında erişilebilen final sayısı düşük.");
        }

        [Test]
        public void Intro_IsDefinedWithKnownImageKeysAndReadableLines()
        {
            Assert.That(story.intro, Is.Not.Null, "Yeni oyun için açılış kurgusu tanımlı olmalı.");
            Assert.That(story.intro.HasBeats, Is.True);
            Assert.That(story.intro.beats.Length, Is.InRange(3, 6));

            HashSet<string> imageKeys = new HashSet<string>();
            foreach (StoryNode node in nodes.Values)
                if (!string.IsNullOrWhiteSpace(node.imageKey)) imageKeys.Add(node.imageKey);

            for (int i = 0; i < story.intro.beats.Length; i++)
            {
                IntroBeat beat = story.intro.beats[i];
                Assert.That(beat.line, Is.Not.Empty, "Açılış kartı " + i + " boş.");
                Assert.That(beat.kicker, Is.Not.Empty, "Açılış kartı " + i + " etiketi boş.");
                Assert.That(imageKeys.Contains(beat.imageKey), Is.True, "Bilinmeyen görsel anahtarı: " + beat.imageKey);
                Assert.That(beat.ResolvedHold, Is.InRange(IntroBeat.MinimumHold, IntroBeat.MaximumHold));
            }
        }

        [Test]
        public void Intro_StaysWithinFirstChoiceTimeBudget()
        {
            // GDD §16 kalite kapısı: ilk seçim en geç 45 saniyede erişilebilir olmalı.
            // Yalnız holdSeconds toplamına bakmak yanıltıcıdır; kart başına geçiş, belirme ve
            // kararma yükü gerçek süreyi belirgin biçimde artırır. Bu yüzden tahmini toplam
            // ekran süresi ölçülür.
            float total = story.intro.EstimatedTotalSeconds;
            Assert.That(total, Is.LessThan(20f), "Açılış kurgusunun tahmini toplam süresi çok uzun: " + total);
            Assert.That(total, Is.GreaterThan(6f), "Açılış kurgusu anlam taşıyamayacak kadar kısa: " + total);
        }

        [Test]
        public void Relations_AreAlwaysClampedToPlusMinusOneHundred()
        {
            GameState state = new GameState();
            EffectResolver.Apply(new[] { new EffectData { type = "relation", key = "olek", op = "add", intValue = 5000 } }, state);
            Assert.That(state.GetRelation("olek"), Is.EqualTo(100));
            EffectResolver.Apply(new[] { new EffectData { type = "relation", key = "olek", op = "set", intValue = -5000 } }, state);
            Assert.That(state.GetRelation("olek"), Is.EqualTo(-100));
        }

        [Test]
        public void Story_HasNoLeftoverStatMechanic()
        {
            // Görünür durum çubukları kaldırıldı. Hikâye verisinde artık hiçbir "stat"
            // etkisi veya koşulu kalmamalıdır; kalırsa sessizce yok sayılırdı.
            string raw = File.ReadAllText(Path.Combine(Application.dataPath, "StreamingAssets", StoryRepository.RelativeStoryPath));
            Assert.That(raw, Does.Not.Contain("\"stat\""), "Hikâye verisinde artakalan stat girdisi var.");
            Assert.That(raw, Does.Not.Contain("initialStats"), "Hikâye verisinde artakalan initialStats bloğu var.");
        }

        [Test]
        public void SaveService_RoundTripsCompleteState()
        {
            string directory = NewTestDirectory();
            try
            {
                SaveService service = new SaveService(directory);
                GameState original = GameState.Create(story);
                original.currentNodeId = story.nodes[2].id;
                original.activeChapter = story.nodes[2].act;
                original.SetFlag("documents_kept", true);
                original.SetRelation("olek_trust", 14);
                original.MarkResultSeen("echo_documents");
                original.AddTrace("Belgeleri yanında tuttu.");
                service.Save(original);

                GameState loaded;
                string message;
                Assert.That(service.TryLoad(out loaded, out message), Is.True, message);
                Assert.That(loaded.currentNodeId, Is.EqualTo(original.currentNodeId));
                Assert.That(loaded.GetFlag("documents_kept"), Is.True);
                Assert.That(loaded.GetRelation("olek_trust"), Is.EqualTo(14));
                Assert.That(loaded.HasSeenResult("echo_documents"), Is.True);
                Assert.That(loaded.traces, Has.Member("Belgeleri yanında tuttu."));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void SaveService_HandlesCorruptJsonWithoutThrowing()
        {
            string directory = NewTestDirectory();
            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, SaveService.SaveFileName), "{ this is not valid json");
                SaveService service = new SaveService(directory);
                GameState loaded;
                string message;
                Assert.DoesNotThrow(() => service.TryLoad(out loaded, out message));
                Assert.That(service.TryLoad(out loaded, out message), Is.False);
                Assert.That(loaded, Is.Null);
                Assert.That(message, Does.Contain("okunamadı"));
                Assert.That(File.Exists(service.SavePath), Is.True, "Bozuk kayıt kanıt için korunmalı.");
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void SettingsService_RoundTripsPersistentOptions()
        {
            string directory = NewTestDirectory();
            try
            {
                SettingsService service = new SettingsService(directory);
                SettingsData expected = new SettingsData
                {
                    masterVolume = 0.41f,
                    ambientVolume = 0.52f,
                    effectsVolume = 0.63f,
                    fullscreen = true,
                    largeText = true,
                    reduceMotion = true,
                    contentNoteSeen = true
                };
                service.Save(expected);
                SettingsData actual = service.Load();
                Assert.That(actual.masterVolume, Is.EqualTo(0.41f).Within(0.001f));
                Assert.That(actual.ambientVolume, Is.EqualTo(0.52f).Within(0.001f));
                Assert.That(actual.effectsVolume, Is.EqualTo(0.63f).Within(0.001f));
                Assert.That(actual.fullscreen, Is.True);
                Assert.That(actual.largeText, Is.True);
                Assert.That(actual.reduceMotion, Is.True);
                Assert.That(actual.contentNoteSeen, Is.True);
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        private void Explore(string nodeId, GameState state, int depth, HashSet<string> endings)
        {
            if (depth > 24 || !nodes.ContainsKey(nodeId)) return;
            StoryNode node = nodes[nodeId];
            if (node.IsEnding)
            {
                endings.Add(node.ending.id);
                return;
            }
            for (int i = 0; i < node.choices.Length; i++)
            {
                ChoiceData choice = node.choices[i];
                if (!ConditionEvaluator.EvaluateAll(choice.conditions, state)) continue;
                GameState branch = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(state));
                EffectResolver.Apply(choice.effects, branch);
                branch.currentNodeId = choice.nextNodeId;
                Explore(choice.nextNodeId, branch, depth + 1, endings);
            }
        }

        private static string NewTestDirectory()
        {
            return Path.Combine(Application.temporaryCachePath, "OrdinaryFrontsTests", Guid.NewGuid().ToString("N"));
        }

        private struct NodeDepth
        {
            public readonly string id;
            public readonly int depth;
            public NodeDepth(string id, int depth) { this.id = id; this.depth = depth; }
        }
    }
}
