using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace OrdinaryFronts.Tests.EditMode
{
    /// <summary>
    /// Arşiv: antolojinin kalıcı belleği. Üç mekanik buradan beslenir — kesişmeler, önceki
    /// oynanış izi ve yapılmayanlar. Testler hem servisi hem de içeriğin iki bölüm arasında
    /// gerçek bayraklara bağlandığını sınar; yazım hatası bir kesişmeyi sessizce öldürürdü.
    /// </summary>
    public sealed class ArchiveTests
    {
        private string tempDirectory;

        private static string StreamingRoot { get { return Path.Combine(Application.dataPath, "StreamingAssets"); } }

        [SetUp]
        public void SetUp()
        {
            tempDirectory = Path.Combine(Path.GetTempPath(), "ordinary-fronts-archive-test-" + System.Guid.NewGuid().ToString("N"));
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

        private StoryController NewController(string storyId, ArchiveService archive)
        {
            string path = Path.Combine(StreamingRoot, StoryRepository.RelativePathFor("en-US", storyId));
            StoryRepository repository = new StoryRepository(path, "en-US", storyId);
            repository.Load();
            return new StoryController(repository, new SaveService(tempDirectory), archive);
        }

        // ------------------------------------------------------------ servis

        [Test]
        public void Archive_SurvivesReloadAndIgnoresCorruptFile()
        {
            ArchiveService archive = new ArchiveService(tempDirectory);
            archive.Load();
            archive.RememberChoice("hamburg_1943", "sir_01_vardiya_sonu", "sir_01_a");

            ArchiveService reloaded = new ArchiveService(tempDirectory);
            reloaded.Load();
            Assert.That(reloaded.PreviousChoice("hamburg_1943", "sir_01_vardiya_sonu"), Is.EqualTo("sir_01_a"));

            // Bozuk dosya oyunu durdurmaz; arşiv boş sayılır.
            File.WriteAllText(reloaded.ArchivePath, "{ bu json değil");
            ArchiveService corrupt = new ArchiveService(tempDirectory);
            corrupt.Load();
            Assert.That(corrupt.PreviousChoice("hamburg_1943", "sir_01_vardiya_sonu"), Is.Null);
            Assert.That(corrupt.HasCompleted("hamburg_1943"), Is.False);
        }

        [Test]
        public void Archive_FlagsAreRememberedOnlyFromCompletedRuns()
        {
            ArchiveService archive = new ArchiveService(tempDirectory);
            archive.Load();
            StoryController controller = NewController("hamburg_1943", archive);
            controller.StartNew();
            controller.Choose(0);
            controller.Choose(0);

            // Yarım kalan oynanış: bayraklar henüz arşivde değil.
            Assert.That(archive.HasCompleted("hamburg_1943"), Is.False);
            Assert.That(archive.GetChapterFlag("hamburg_1943", "switchgear_secured"), Is.False);

            int guard = 0;
            while (!controller.CurrentNode.IsEnding && guard++ < 40) controller.Choose(0);
            Assert.That(controller.CurrentNode.IsEnding, Is.True);
            Assert.That(archive.HasCompleted("hamburg_1943"), Is.True);
            Assert.That(archive.GetChapterFlag("hamburg_1943", "switchgear_secured"), Is.True,
                "İlk seçim şalteri güvene alır; tamamlanan oynanışın bayrağı arşivde olmalı.");
            Assert.That(archive.CompletedRuns("hamburg_1943"), Is.EqualTo(1));
        }

        // ------------------------------------------------------------ kesişmeler

        [Test]
        public void Crossing_FiresInNeretvaAfterHamburgIsCompleted_AndNeverWithoutIt()
        {
            ArchiveService archive = new ArchiveService(tempDirectory);
            archive.Load();

            // Hamburg tamamlanmadan: Neretva'nın ilk düğümünde kesişme yok.
            StoryController fresh = NewController("neretva_1943", archive);
            StoryNode first = fresh.StartNew();
            string[] before = fresh.ConsumeEchoes(first);
            Assert.That(before, Is.Empty, "Arşiv boşken kesişme tetiklenmemeli.");

            // Hamburg'u ilk seçeneklerle tamamla (şalteri güvene alır).
            StoryController hamburg = NewController("hamburg_1943", archive);
            hamburg.StartNew();
            int guard = 0;
            while (!hamburg.CurrentNode.IsEnding && guard++ < 40) hamburg.Choose(0);

            // Şimdi Neretva'nın ilk düğümü Hamburg'u hatırlamalı.
            StoryController neretva = NewController("neretva_1943", archive);
            StoryNode node = neretva.StartNew();
            string[] after = neretva.ConsumeEchoes(node);
            Assert.That(after, Is.Not.Empty, "Hamburg tamamlandıktan sonra Neretva'nın ilk düğümünde kesişme beklenir.");
            Assert.That(after[0], Does.Contain("switch").Or.Contain("yard"), "Şalter kesişmesi tetiklenmeli: " + after[0]);
        }

        [Test]
        public void Crossing_ArchiveConditionIsClosedSafeWithoutArchive()
        {
            GameState state = new GameState();
            ConditionData condition = new ConditionData { type = "archive", key = "hamburg_1943:switchgear_secured", op = "equals", boolValue = true };
            Assert.That(ConditionEvaluator.Evaluate(condition, state), Is.False);
            Assert.That(ConditionEvaluator.Evaluate(condition, state, null), Is.False);

            ArchiveService empty = new ArchiveService(tempDirectory);
            empty.Load();
            Assert.That(ConditionEvaluator.Evaluate(condition, state, empty), Is.False, "Tamamlanmamış bölüm sayılmaz.");
        }

        /// <summary>
        /// Her kesişme, gerçekten var olan bir bölümün gerçekten üretilen bir bayrağına
        /// işaret etmeli. Aksi hâlde kesişme hiç tetiklenmez ve kimse fark etmez.
        /// </summary>
        [Test]
        public void Crossings_PointAtRealFlagsInRealChapters()
        {
            Dictionary<string, HashSet<string>> producedFlags = new Dictionary<string, HashSet<string>>();
            StoryCatalog catalog = StoryCatalog.Load("en-US", StreamingRoot);
            List<string> playable = new List<string>();
            for (int i = 0; i < catalog.entries.Length; i++)
                if (catalog.entries[i] != null && catalog.entries[i].IsPlayable) playable.Add(catalog.entries[i].storyId);

            foreach (string storyId in playable)
            {
                StoryDatabase story = LoadStory("en-US", storyId);
                HashSet<string> flags = new HashSet<string>();
                for (int n = 0; n < story.nodes.Length; n++)
                    for (int c = 0; story.nodes[n].choices != null && c < story.nodes[n].choices.Length; c++)
                        for (int e = 0; story.nodes[n].choices[c].effects != null && e < story.nodes[n].choices[c].effects.Length; e++)
                            if (StoryVocabulary.IsFlagType(story.nodes[n].choices[c].effects[e].type))
                                flags.Add(story.nodes[n].choices[c].effects[e].key);
                producedFlags[storyId] = flags;
            }

            int crossings = 0;
            foreach (string locale in LocalizationService.SupportedLocales)
            {
                foreach (string storyId in playable)
                {
                    StoryDatabase story = LoadStory(locale, storyId);
                    for (int n = 0; n < story.nodes.Length; n++)
                    {
                        if (story.nodes[n].echoes == null) continue;
                        for (int e = 0; e < story.nodes[n].echoes.Length; e++)
                        {
                            EchoData echo = story.nodes[n].echoes[e];
                            if (echo.conditions == null) continue;
                            for (int c = 0; c < echo.conditions.Length; c++)
                            {
                                if (!StoryVocabulary.IsArchiveType(echo.conditions[c].type)) continue;
                                crossings++;
                                string otherStory, flag;
                                Assert.That(StoryVocabulary.TrySplitArchiveKey(echo.conditions[c].key, out otherStory, out flag), Is.True,
                                    locale + "/" + storyId + "/" + echo.id + " anahtar biçimi bozuk");
                                Assert.That(otherStory, Is.Not.EqualTo(storyId), locale + "/" + storyId + "/" + echo.id + " kendi bölümüne bakıyor");
                                Assert.That(producedFlags.ContainsKey(otherStory), Is.True, locale + "/" + storyId + "/" + echo.id + " bilinmeyen bölüm: " + otherStory);
                                Assert.That(producedFlags[otherStory].Contains(flag), Is.True,
                                    locale + "/" + storyId + "/" + echo.id + " " + otherStory + " bu bayrağı üretmiyor: " + flag);
                            }
                        }
                    }
                }
            }
            Assert.That(crossings, Is.GreaterThanOrEqualTo(16), "Her iki yönde de kesişme bulunmalı; sayılan: " + crossings);
        }

        // ------------------------------------------------------------ önceki oynanış

        [Test]
        public void PreviousChoice_IsReportedOnReplay_AndOnlyForVisitedNodes()
        {
            ArchiveService archive = new ArchiveService(tempDirectory);
            archive.Load();
            StoryController first = NewController("hamburg_1943", archive);
            StoryNode start = first.StartNew();
            Assert.That(first.PreviousChoiceText(start), Is.Null, "İlk oynanışta önceki iz olmamalı.");
            first.Choose(1);

            StoryController replay = NewController("hamburg_1943", archive);
            StoryNode again = replay.StartNew();
            Assert.That(replay.PreviousChoiceText(again), Is.EqualTo(start.choices[1].text));
            // Hiç uğranmamış bir düğümde iz yok.
            StoryNode unvisited = replay.Story.nodes[replay.Story.nodes.Length - 2];
            if (!unvisited.IsEnding) Assert.That(replay.PreviousChoiceText(unvisited), Is.Null);
        }

        // ------------------------------------------------------------ yapılmayanlar

        [Test]
        public void Omissions_ListOnlyAuthoredUntakenChoicesOnThisRoute()
        {
            ArchiveService archive = new ArchiveService(tempDirectory);
            archive.Load();
            StoryController controller = NewController("neretva_1943", archive);
            controller.StartNew();
            // İlk seçenek rotası: kol_04_koy_kalintisi'nde "unu al" alınır, "ölüleri göm" alınmaz.
            int guard = 0;
            while (!controller.CurrentNode.IsEnding && guard++ < 40) controller.Choose(0);

            string[] omissions = controller.BuildOmissions();
            Assert.That(omissions, Is.Not.Empty);
            Assert.That(omissions, Has.Some.Contains("bury"), "Gömülmeyen aile yapılmayanlar listesinde olmalı.");
            // Alınmış seçeneğin metni (iz) yapılmayanlar arasında olamaz.
            foreach (string line in omissions)
                Assert.That(line, Does.StartWith("You did not"), "Yapılmayan satırı olumsuz olmalı: " + line);
        }

        [Test]
        public void Omissions_AreAuthoredInBothLocalesForTheSameChoices()
        {
            string reference = LocalizationService.SupportedLocales[0];
            foreach (string storyId in new[] { "hamburg_1943", "neretva_1943" })
            {
                StoryDatabase first = LoadStory(reference, storyId);
                HashSet<string> authored = new HashSet<string>();
                for (int n = 0; n < first.nodes.Length; n++)
                    for (int c = 0; first.nodes[n].choices != null && c < first.nodes[n].choices.Length; c++)
                        if (!string.IsNullOrWhiteSpace(first.nodes[n].choices[c].omission)) authored.Add(first.nodes[n].choices[c].id);
                Assert.That(authored.Count, Is.GreaterThanOrEqualTo(4), storyId + " için en az dört yapılmayan yazılmalı");

                foreach (string locale in LocalizationService.SupportedLocales)
                {
                    StoryDatabase other = LoadStory(locale, storyId);
                    for (int n = 0; n < other.nodes.Length; n++)
                        for (int c = 0; other.nodes[n].choices != null && c < other.nodes[n].choices.Length; c++)
                        {
                            ChoiceData choice = other.nodes[n].choices[c];
                            bool has = !string.IsNullOrWhiteSpace(choice.omission);
                            Assert.That(has, Is.EqualTo(authored.Contains(choice.id)), locale + "/" + storyId + " " + choice.id + " yapılmayan eşitsizliği");
                        }
                }
            }
        }
    }
}
