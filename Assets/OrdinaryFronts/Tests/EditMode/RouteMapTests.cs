using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace OrdinaryFronts.Tests.EditMode
{
    /// <summary>
    /// Yol haritası: yerleşimin her bölümde okunur olduğu (her kenar sağa gider), haritanın
    /// yalnız yürünenleri ve bir adım ötesini gösterdiği, adımların kayıt ve arşivde
    /// saklandığı. Harita spoiler vermemeli: yürünmemiş bir durağın ötesi hiç görünmez.
    /// </summary>
    public sealed class RouteMapTests
    {
        private string tempDirectory;

        private static string StreamingRoot { get { return Path.Combine(Application.dataPath, "StreamingAssets"); } }

        [SetUp]
        public void SetUp()
        {
            tempDirectory = Path.Combine(Path.GetTempPath(), "ordinary-fronts-route-test-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempDirectory)) Directory.Delete(tempDirectory, true);
        }

        private static StoryDatabase LoadStory(string storyId)
        {
            string path = Path.Combine(StreamingRoot, StoryRepository.RelativePathFor("en-US", storyId));
            return JsonUtility.FromJson<StoryDatabase>(File.ReadAllText(path));
        }

        private static IEnumerable<string> PlayableIds()
        {
            StoryCatalog catalog = StoryCatalog.Load("en-US", StreamingRoot);
            for (int i = 0; i < catalog.entries.Length; i++)
                if (catalog.entries[i] != null && catalog.entries[i].IsPlayable) yield return catalog.entries[i].storyId;
        }

        [Test]
        public void EveryEdgeRunsLeftToRight_InEveryChapter()
        {
            foreach (string id in PlayableIds())
            {
                StoryDatabase story = LoadStory(id);
                Dictionary<string, int> depth = RouteMap.Depths(story);
                Assert.That(depth[story.startNodeId], Is.EqualTo(0), id);
                foreach (StoryNode node in story.nodes)
                {
                    if (node.choices == null || !depth.ContainsKey(node.id)) continue;
                    foreach (ChoiceData choice in node.choices)
                        Assert.That(depth[choice.nextNodeId], Is.GreaterThan(depth[node.id]), id + ": " + node.id + " -> " + choice.nextNodeId);
                }
            }
        }

        [Test]
        public void FreshChapter_ShowsOnlyTheStartAndTwoUntakenRoads()
        {
            StoryDatabase story = LoadStory("hamburg_1943");
            RouteMap map = RouteMap.Build(story, null, null, story.startNodeId);
            Assert.That(map.stops.Count, Is.EqualTo(3));
            Assert.That(map.stops.FindAll(s => s.state == RouteStopState.Untaken).Count, Is.EqualTo(2));
            Assert.That(map.edges.Count, Is.EqualTo(2));
            Assert.That(map.edges.TrueForAll(e => !e.walked && !e.current), Is.True);
        }

        [Test]
        public void WalkedRoute_ShowsOneStepBeyond_AndNothingFurther()
        {
            foreach (string id in PlayableIds())
            {
                StoryDatabase story = LoadStory(id);
                Dictionary<string, StoryNode> nodes = new Dictionary<string, StoryNode>();
                foreach (StoryNode n in story.nodes) nodes[n.id] = n;

                // Hep ilk seçenekle bir final'e kadar yürü.
                List<string> steps = new List<string>();
                StoryNode cursor = nodes[story.startNodeId];
                while (!cursor.IsEnding)
                {
                    steps.Add(RouteMap.Step(cursor.id, cursor.choices[0].id));
                    cursor = nodes[cursor.choices[0].nextNodeId];
                }
                RouteMap map = RouteMap.Build(story, null, steps, cursor.id);

                Assert.That(map.stops.FindAll(s => s.state == RouteStopState.Current).Count, Is.EqualTo(steps.Count + 1), id);
                HashSet<RouteStop> fromVisited = new HashSet<RouteStop>();
                foreach (RouteEdge edge in map.edges)
                {
                    Assert.That(edge.from.state, Is.Not.EqualTo(RouteStopState.Untaken), id + ": yürünmemiş duraktan kenar çıkmamalı");
                    fromVisited.Add(edge.to);
                }
                foreach (RouteStop stop in map.stops)
                    if (stop.state == RouteStopState.Untaken)
                        Assert.That(fromVisited.Contains(stop), Is.True, id + ": " + stop.node.id + " bir adım ötede değil");
                Assert.That(map.edges.FindAll(e => e.current).Count, Is.EqualTo(steps.Count), id);
            }
        }

        [Test]
        public void Steps_AreRecordedInSaveAndArchive_AcrossRuns()
        {
            string path = Path.Combine(StreamingRoot, StoryRepository.RelativePathFor("en-US", "hamburg_1943"));
            StoryRepository repository = new StoryRepository(path, "en-US", "hamburg_1943");
            repository.Load();
            ArchiveService archive = new ArchiveService(tempDirectory);
            archive.Load();
            SaveService save = new SaveService(tempDirectory);
            StoryController controller = new StoryController(repository, save, archive);

            controller.StartNew();
            string first = controller.CurrentNode.id;
            string firstChoice = controller.CurrentNode.choices[0].id;
            controller.Choose(0);
            Assert.That(controller.State.path, Is.EqualTo(new[] { RouteMap.Step(first, firstChoice) }));

            GameState loaded;
            string message;
            Assert.That(save.TryLoad(out loaded, out message), Is.True, message);
            Assert.That(loaded.path, Has.Length.EqualTo(1));

            // İkinci oynanış öteki yolu yürür; arşiv ikisini birden tutar.
            controller.StartNew();
            string secondChoice = controller.CurrentNode.choices[1].id;
            controller.Choose(1);
            ArchiveService reloaded = new ArchiveService(tempDirectory);
            reloaded.Load();
            string[] walked = reloaded.WalkedSteps("hamburg_1943");
            Assert.That(walked, Does.Contain(RouteMap.Step(first, firstChoice)));
            Assert.That(walked, Does.Contain(RouteMap.Step(first, secondChoice)));
        }
    }
}
