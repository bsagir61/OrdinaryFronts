using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace OrdinaryFronts.Tests.EditMode
{
    /// <summary>
    /// Tanıklık ve perde soruları (1.3): her bölümün her rotası, iki kipte de en az iki
    /// satırlık bir tanıklık üretmeli; iki dilin tanıklığı aynı koşullara bağlı olmalı;
    /// tanıklık yalnız bir kez sorulmalı ve kayda yazılmalı. Tanıklık hiçbir bayrak
    /// üretmez: tarihte ve rotada hiçbir şeyi değiştirmez.
    /// </summary>
    public sealed class TestimonyTests
    {
        private string tempDirectory;

        private static string StreamingRoot { get { return Path.Combine(Application.dataPath, "StreamingAssets"); } }

        [SetUp]
        public void SetUp()
        {
            tempDirectory = Path.Combine(Path.GetTempPath(), "ordinary-fronts-testimony-test-" + System.Guid.NewGuid().ToString("N"));
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

        private static IEnumerable<string> PlayableIds()
        {
            StoryCatalog catalog = StoryCatalog.Load("en-US", StreamingRoot);
            for (int i = 0; i < catalog.entries.Length; i++)
                if (catalog.entries[i] != null && catalog.entries[i].IsPlayable) yield return catalog.entries[i].storyId;
        }

        [Test]
        public void EveryChapter_HasACompleteTestimonyAndAQuestionForEveryAct()
        {
            foreach (string locale in LocalizationService.SupportedLocales)
                foreach (string id in PlayableIds())
                {
                    StoryDatabase story = LoadStory(locale, id);
                    string label = locale + "/" + id;
                    Assert.That(story.testimony, Is.Not.Null, label);
                    Assert.That(story.testimony.IsComplete, Is.True, label);
                    Assert.That(StoryGraphValidator.Validate(story), Is.Empty, label);
                    HashSet<string> acts = new HashSet<string>();
                    foreach (StoryNode node in story.nodes) if (!string.IsNullOrEmpty(node.act)) acts.Add(node.act);
                    foreach (string act in acts)
                    {
                        ActData data = story.FindAct(act);
                        Assert.That(data, Is.Not.Null, label + " perde sorusu yok: " + act);
                        Assert.That(data.question, Does.EndWith("?"), label + " " + act);
                    }
                }
        }

        [Test]
        public void BothLanguages_BindTheSameLinesToTheSameFlags()
        {
            foreach (string id in PlayableIds())
            {
                TestimonyData tr = LoadStory("tr-TR", id).testimony;
                TestimonyData en = LoadStory("en-US", id).testimony;
                AssertSameKeys(id + "/done", tr.done, en.done);
                AssertSameKeys(id + "/undone", tr.undone, en.undone);
            }
        }

        private static void AssertSameKeys(string label, TestimonyLine[] a, TestimonyLine[] b)
        {
            Assert.That(b.Length, Is.EqualTo(a.Length), label);
            for (int i = 0; i < a.Length; i++)
            {
                Assert.That(b[i].conditions.Length, Is.EqualTo(a[i].conditions.Length), label + " #" + i);
                for (int c = 0; c < a[i].conditions.Length; c++)
                    Assert.That(b[i].conditions[c].key, Is.EqualTo(a[i].conditions[c].key), label + " #" + i);
            }
        }

        /// <summary>
        /// Bütün rotalar yürünür (bölüm başına on binlerce): hiçbir oyuncu tek satırlık ya da
        /// boş bir tanıklıkla karşılaşmamalı.
        /// </summary>
        [Test]
        public void EveryRoute_GivesAtLeastTwoLines_InBothModes()
        {
            foreach (string id in PlayableIds())
            {
                StoryDatabase story = LoadStory("tr-TR", id);
                Dictionary<string, StoryNode> nodes = new Dictionary<string, StoryNode>();
                foreach (StoryNode n in story.nodes) nodes[n.id] = n;
                int routes = 0;
                int fewest = int.MaxValue;
                Walk(story, nodes, story.startNodeId, new List<string>(), ref routes, ref fewest);
                Assert.That(routes, Is.GreaterThan(100), id);
                Assert.That(fewest, Is.GreaterThanOrEqualTo(2), id);
            }
        }

        private static void Walk(StoryDatabase story, Dictionary<string, StoryNode> nodes, string id, List<string> flags, ref int routes, ref int fewest)
        {
            StoryNode node = nodes[id];
            if (node.IsEnding)
            {
                routes++;
                GameState state = new GameState();
                BoolStateEntry[] entries = new BoolStateEntry[flags.Count];
                for (int i = 0; i < flags.Count; i++) entries[i] = new BoolStateEntry { key = flags[i], value = true };
                state.flags = entries;
                System.Func<ConditionData[], bool> holds = conditions => ConditionEvaluator.EvaluateAll(conditions, state);
                fewest = Mathf.Min(fewest, TestimonyData.Select(story.testimony.done, holds).Count);
                fewest = Mathf.Min(fewest, TestimonyData.Select(story.testimony.undone, holds).Count);
                return;
            }
            foreach (ChoiceData choice in node.choices)
            {
                int mark = flags.Count;
                if (choice.effects != null)
                    foreach (EffectData effect in choice.effects)
                        if (StoryVocabulary.Normalize(effect.type) == StoryVocabulary.TypeFlag && (effect.boolValue || StoryVocabulary.EffectOperationOrDefault(effect.op) == StoryVocabulary.OperationAdd))
                            flags.Add(effect.key);
                Walk(story, nodes, choice.nextNodeId, flags, ref routes, ref fewest);
                flags.RemoveRange(mark, flags.Count - mark);
            }
        }

        [Test]
        public void Select_TakesFirstMiddleAndLast_WhenMoreThanThreeMatch()
        {
            TestimonyLine[] lines = new TestimonyLine[5];
            for (int i = 0; i < lines.Length; i++) lines[i] = new TestimonyLine { text = "L" + i };
            List<string> picked = TestimonyData.Select(lines, c => true);
            Assert.That(picked, Is.EqualTo(new[] { "L0", "L2", "L4" }));
            Assert.That(TestimonyData.Select(lines, c => false), Is.Empty);
        }

        [Test]
        public void Testimony_IsAskedOnceAfterTheEnding_AndSaved()
        {
            string path = Path.Combine(StreamingRoot, StoryRepository.RelativePathFor("tr-TR", "hamburg_1943"));
            StoryRepository repository = new StoryRepository(path, "tr-TR", "hamburg_1943");
            repository.Load();
            SaveService save = new SaveService(tempDirectory);
            ArchiveService archive = new ArchiveService(tempDirectory);
            archive.Load();
            StoryController controller = new StoryController(repository, save, archive);

            controller.StartNew();
            Assert.That(controller.TestimonyPending, Is.False, "tanıklık final'den önce sorulmaz");
            int guard = 0;
            while (!controller.CurrentNode.IsEnding && guard++ < 40) controller.Choose(1);
            Assert.That(controller.CurrentNode.IsEnding, Is.True);
            Assert.That(controller.TestimonyPending, Is.True);

            int flagsBefore = controller.State.flags.Length;
            List<string> lines = controller.BuildTestimony(false);
            Assert.That(lines.Count, Is.InRange(2, TestimonyData.MaxLines));
            controller.RecordTestimony(false);
            Assert.That(controller.TestimonyPending, Is.False);
            Assert.That(controller.State.flags.Length, Is.EqualTo(flagsBefore), "tanıklık bayrak üretmez");

            GameState loaded;
            string message;
            Assert.That(save.TryLoad(out loaded, out message), Is.True, message);
            Assert.That(loaded.testimony, Is.EqualTo(StoryController.TestimonyUndone));
        }
    }
}
