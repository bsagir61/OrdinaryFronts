using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace OrdinaryFronts.Tests.EditMode
{
    /// <summary>
    /// Ara sahneler: düğüme girilirken oynanan, sonucu bayrak olan kısa hareketli anlar.
    /// Testler veri sözleşmesini sınar — türü kodda karşılığı olan, sonuçları ilan edilmiş,
    /// iki dilde aynı — ve kayıt davranışını: bir kez oynanır, sonucu bayrağa yazılır,
    /// ilan edilmemiş sonuç yazılmaz.
    /// </summary>
    public sealed class InterludeTests
    {
        private string tempDirectory;

        private static string StreamingRoot { get { return Path.Combine(Application.dataPath, "StreamingAssets"); } }

        [SetUp]
        public void SetUp()
        {
            tempDirectory = Path.Combine(Path.GetTempPath(), "ordinary-fronts-interlude-test-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempDirectory)) Directory.Delete(tempDirectory, true);
        }

        private static StoryDatabase LoadStory(string locale, string storyId)
        {
            string path = Path.Combine(StreamingRoot, StoryRepository.RelativePathFor(locale, storyId));
            return JsonUtility.FromJson<StoryDatabase>(File.ReadAllText(path));
        }

        private static List<StoryCatalogEntry> PlayableEntries(string locale)
        {
            StoryCatalog catalog = StoryCatalog.Load(locale, StreamingRoot);
            List<StoryCatalogEntry> result = new List<StoryCatalogEntry>();
            for (int i = 0; i < catalog.entries.Length; i++)
                if (catalog.entries[i] != null && catalog.entries[i].IsPlayable) result.Add(catalog.entries[i]);
            return result;
        }

        private static StoryDatabase MinimalStory()
        {
            // Beş finalli, iki seçimli en küçük geçerli graf; ara sahne testleri buna eklenir.
            List<StoryNode> nodes = new List<StoryNode>();
            StoryNode start = new StoryNode { id = "n0", act = "A", body = "x" };
            StoryNode mid = new StoryNode { id = "n1", act = "B", body = "x" };
            StoryNode mid2 = new StoryNode { id = "n2", act = "C", body = "x" };
            start.choices = new[] { Choice("c0", "n1"), Choice("c1", "n2") };
            mid.choices = new[] { Choice("c2", "e1"), Choice("c3", "e2") };
            mid2.choices = new[] { Choice("c4", "e3"), Choice("c5", "n3") };
            StoryNode mid3 = new StoryNode { id = "n3", act = "C", body = "x" };
            mid3.choices = new[] { Choice("c6", "e4"), Choice("c7", "e5") };
            nodes.Add(start); nodes.Add(mid); nodes.Add(mid2); nodes.Add(mid3);
            for (int i = 1; i <= 5; i++)
                nodes.Add(new StoryNode { id = "e" + i, act = "C", ending = new EndingData { id = "e" + i, title = "E", paragraphs = new[] { "a", "b" }, traceFallbacks = new[] { "1", "2", "3" } } });
            return new StoryDatabase { storyId = "test", startNodeId = "n0", nodes = nodes.ToArray() };
        }

        private static ChoiceData Choice(string id, string next)
        {
            return new ChoiceData { id = id, text = "t", trace = "t", nextNodeId = next };
        }

        // -------------------------------------------------------------- doğrulayıcı

        [Test]
        public void Validator_RejectsUnknownKindAndRegistersDeclaredResults()
        {
            StoryDatabase story = MinimalStory();
            story.nodes[1].interlude = new InterludeData { id = "wind", kind = "walk", caption = "c", results = new[] { "il_a", "il_b" } };
            story.nodes[2].echoes = new[] { new EchoData { id = "e", text = "t", conditions = new[] { new ConditionData { type = "flag", key = "il_a", op = "equals", boolValue = true } } } };
            Assert.That(StoryGraphValidator.Validate(story), Is.Empty, "ilan edilen sonuç bilinen bayrak sayılmalı");

            story.nodes[1].interlude.kind = "dance";
            Assert.That(StoryGraphValidator.Validate(story), Has.Some.Contains("Bilinmeyen ara sahne türü"));

            story.nodes[1].interlude.kind = "walk";
            story.nodes[1].interlude.results = new string[0];
            Assert.That(StoryGraphValidator.Validate(story), Has.Some.Contains("Sonuç üretmeyen ara sahne"));
        }

        [Test]
        public void Validator_RejectsInterludeOnEndingAndDuplicateIds()
        {
            StoryDatabase story = MinimalStory();
            story.nodes[1].interlude = new InterludeData { id = "same", kind = "walk", results = new[] { "il_a" } };
            story.nodes[2].interlude = new InterludeData { id = "same", kind = "walk", results = new[] { "il_b" } };
            story.nodes[4].interlude = new InterludeData { id = "end", kind = "walk", results = new[] { "il_c" } };
            List<string> issues = StoryGraphValidator.Validate(story);
            Assert.That(issues, Has.Some.Contains("Yinelenen ara sahne kimliği"));
            Assert.That(issues, Has.Some.Contains("Final düğümünde ara sahne"));
        }

        [Test]
        public void Validator_RequiresTwoResultsForChoosingInterlude()
        {
            StoryDatabase story = MinimalStory();
            story.nodes[1].interlude = new InterludeData { id = "lamp", kind = "lamp", chooses = true, results = new[] { "il_only" } };
            Assert.That(StoryGraphValidator.Validate(story), Has.Some.Contains("iki seçime iki sonuç"));
            story.nodes[1].interlude.results = new[] { "il_a", "il_b" };
            Assert.That(StoryGraphValidator.Validate(story), Is.Empty);
        }

        // -------------------------------------------------------------- içerik

        [Test]
        public void EveryInterlude_HasKnownKindCaptionAndIdenticalShapeAcrossLocales()
        {
            int total = 0;
            foreach (StoryCatalogEntry entry in PlayableEntries("tr-TR"))
            {
                StoryDatabase tr = LoadStory("tr-TR", entry.storyId);
                StoryDatabase en = LoadStory("en-US", entry.storyId);
                Dictionary<string, StoryNode> enNodes = new Dictionary<string, StoryNode>();
                for (int i = 0; i < en.nodes.Length; i++) enNodes[en.nodes[i].id] = en.nodes[i];
                for (int i = 0; i < tr.nodes.Length; i++)
                {
                    StoryNode node = tr.nodes[i];
                    StoryNode other = enNodes[node.id];
                    Assert.That(other.HasInterlude, Is.EqualTo(node.HasInterlude), entry.storyId + "/" + node.id);
                    if (!node.HasInterlude) continue;
                    total++;
                    Assert.That(StoryVocabulary.IsKnownInterludeKind(node.interlude.kind), Is.True, node.id);
                    Assert.That(node.interlude.caption, Is.Not.Empty, node.id + " tr caption");
                    Assert.That(other.interlude.caption, Is.Not.Empty, node.id + " en caption");
                    Assert.That(other.interlude.id, Is.EqualTo(node.interlude.id));
                    Assert.That(other.interlude.kind, Is.EqualTo(node.interlude.kind));
                    Assert.That(other.interlude.results, Is.EqualTo(node.interlude.results));
                    Assert.That(other.interlude.chooses, Is.EqualTo(node.interlude.chooses));
                    if (node.interlude.chooses)
                        Assert.That(node.interlude.results.Length, Is.GreaterThanOrEqualTo(2), node.id + " karar sahnesi iki sonuç ister");
                }
            }
            Assert.That(total, Is.GreaterThanOrEqualTo(3), "her bölümde en az bir ara sahne olmalı");
        }

        // -------------------------------------------------------------- kayıt

        [Test]
        public void RecordInterlude_MarksSeenSetsDeclaredFlagAndPersists()
        {
            string path = Path.Combine(StreamingRoot, StoryRepository.RelativePathFor("en-US", "amsterdam_1945"));
            StoryRepository repository = new StoryRepository(path, "en-US", "amsterdam_1945");
            repository.Load();
            SaveService save = new SaveService(tempDirectory);
            StoryController controller = new StoryController(repository, save);
            controller.StartNew();

            InterludeData data = new InterludeData { id = "afsluitdijk_wind", kind = "walk", results = new[] { "il_wind_pushed", "il_wind_waited" } };
            controller.RecordInterlude(data, "il_wind_waited");
            Assert.That(controller.State.HasSeenResult(StoryVocabulary.InterludeSeenKey("afsluitdijk_wind")), Is.True);
            Assert.That(controller.State.GetFlag("il_wind_waited"), Is.True);
            Assert.That(controller.State.GetFlag("il_wind_pushed"), Is.False);

            GameState loaded;
            string message;
            Assert.That(save.TryLoad(out loaded, out message), Is.True, message);
            Assert.That(loaded.GetFlag("il_wind_waited"), Is.True);
            Assert.That(loaded.HasSeenResult(StoryVocabulary.InterludeSeenKey("afsluitdijk_wind")), Is.True);
        }

        [Test]
        public void RecordInterlude_IgnoresUndeclaredResultAndAcceptsSkip()
        {
            string path = Path.Combine(StreamingRoot, StoryRepository.RelativePathFor("en-US", "amsterdam_1945"));
            StoryRepository repository = new StoryRepository(path, "en-US", "amsterdam_1945");
            repository.Load();
            StoryController controller = new StoryController(repository, new SaveService(tempDirectory));
            controller.StartNew();

            InterludeData data = new InterludeData { id = "x", kind = "walk", results = new[] { "il_a" } };
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("ilan edilmemiş"));
            controller.RecordInterlude(data, "il_zzz");
            Assert.That(controller.State.GetFlag("il_zzz"), Is.False);
            Assert.That(controller.State.HasSeenResult(StoryVocabulary.InterludeSeenKey("x")), Is.True);

            // Geçilen sahne: sonuç yok, ama oynandı sayılır ve yeniden tetiklenmez.
            InterludeData skipped = new InterludeData { id = "y", kind = "walk", results = new[] { "il_b" } };
            controller.RecordInterlude(skipped, null);
            Assert.That(controller.State.HasSeenResult(StoryVocabulary.InterludeSeenKey("y")), Is.True);
            Assert.That(controller.State.GetFlag("il_b"), Is.False);
        }
    }
}
