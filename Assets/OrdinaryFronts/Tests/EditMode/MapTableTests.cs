using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace OrdinaryFronts.Tests.EditMode
{
    /// <summary>
    /// Harita masası (1.7): zaman şeridi için her bölümün geçerli bir tarihi olmalı; oyuncunun
    /// gördüğü kesişmeler (iplikler) ve son tanıklığı arşive yazılmalı ve yeniden açılışta
    /// korunmalı; tanıklık satırları dilden bağımsız dizinlerle saklanmalı.
    /// </summary>
    public sealed class MapTableTests
    {
        private string tempDirectory;

        private static string StreamingRoot { get { return Path.Combine(Application.dataPath, "StreamingAssets"); } }

        [SetUp]
        public void SetUp()
        {
            tempDirectory = Path.Combine(Path.GetTempPath(), "ordinary-fronts-map-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempDirectory)) Directory.Delete(tempDirectory, true);
        }

        private StoryController NewController(string storyId, SaveService save, ArchiveService archive, string locale = "en-US")
        {
            string path = Path.Combine(StreamingRoot, StoryRepository.RelativePathFor(locale, storyId));
            StoryRepository repository = new StoryRepository(path, locale, storyId);
            repository.Load();
            return new StoryController(repository, save, archive);
        }

        [Test]
        public void EveryChapter_HasADateInsideTheTimeline_SameInEveryLocale()
        {
            Dictionary<string, string> reference = new Dictionary<string, string>();
            foreach (string locale in LocalizationService.SupportedLocales)
            {
                StoryCatalog catalog = StoryCatalog.Load(locale, StreamingRoot);
                foreach (StoryCatalogEntry entry in catalog.entries)
                {
                    if (entry == null || !entry.IsPlayable) continue;
                    int year, month;
                    Assert.That(entry.TryGetDate(out year, out month), Is.True, locale + "/" + entry.storyId + " tarih yok: " + entry.date);
                    int index = year * 12 + month - 1;
                    Assert.That(index, Is.InRange(1939 * 12 + 8, 1945 * 12 + 4), entry.storyId + " zaman şeridinin dışında");
                    Assert.That(entry.period, Does.Contain(year.ToString()), entry.storyId + " tarih ile dönem satırı uyuşmuyor");
                    string seen;
                    if (reference.TryGetValue(entry.storyId, out seen)) Assert.That(entry.date, Is.EqualTo(seen), entry.storyId);
                    else reference[entry.storyId] = entry.date;
                }
            }
        }

        [Test]
        public void SeenCrossing_BecomesAThread_AndSurvivesRestart()
        {
            SaveService save = new SaveService(tempDirectory);
            ArchiveService archive = new ArchiveService(tempDirectory);
            archive.Load();
            StoryController hamburg = NewController("hamburg_1943", save, archive);
            hamburg.StartNew();
            int guard = 0;
            while (!hamburg.CurrentNode.IsEnding && guard++ < 40) hamburg.Choose(0);
            Assert.That(archive.HasCompleted("hamburg_1943"), Is.True);
            Assert.That(archive.SeenCrossings(), Is.Empty, "kesişme görülmeden iplik olmaz");

            StoryController neretva = NewController("neretva_1943", save, archive);
            string[] lines = neretva.ConsumeEchoes(neretva.StartNew());
            Assert.That(lines, Is.Not.Empty, "Hamburg'da şalteri güvene alan oyuncuya Neretva'nın ilk sahnesi bir şey hatırlatmalı");
            Assert.That(archive.SeenCrossings(), Does.Contain("neretva_1943>cross_switchgear"));

            ArchiveService reopened = new ArchiveService(tempDirectory);
            reopened.Load();
            Assert.That(reopened.SeenCrossings(), Does.Contain("neretva_1943>cross_switchgear"));
        }

        [Test]
        public void LocalEcho_IsNotAThread()
        {
            SaveService save = new SaveService(tempDirectory);
            ArchiveService archive = new ArchiveService(tempDirectory);
            archive.Load();
            StoryController hamburg = NewController("hamburg_1943", save, archive);
            hamburg.StartNew();
            for (int i = 0; i < 4; i++)
            {
                hamburg.Choose(0);
                hamburg.ConsumeEchoes(hamburg.CurrentNode);
            }
            Assert.That(archive.SeenCrossings(), Is.Empty, "bölüm içi yankı iplik sayılmaz");
        }

        [Test]
        public void LastTestimony_IsStoredAsIndices_AndReadsBackInEitherLanguage()
        {
            SaveService save = new SaveService(tempDirectory);
            ArchiveService archive = new ArchiveService(tempDirectory);
            archive.Load();
            StoryController karelia = NewController("karelia_1940", save, archive);
            karelia.StartNew();
            int guard = 0;
            while (!karelia.CurrentNode.IsEnding && guard++ < 40) karelia.Choose(1);
            List<string> told = karelia.BuildTestimony(true);
            karelia.RecordTestimony(true);

            string mode;
            int[] indices;
            Assert.That(archive.TryGetTestimony("karelia_1940", out mode, out indices), Is.True);
            Assert.That(mode, Is.EqualTo(StoryController.TestimonyDone));
            Assert.That(indices.Length, Is.EqualTo(told.Count));
            TestimonyData en = karelia.Story.testimony;
            for (int i = 0; i < indices.Length; i++) Assert.That(en.done[indices[i]].text.Trim(), Is.EqualTo(told[i]));

            // Aynı dizinler Türkçe dosyada da aynı koşullu satırı gösterir.
            TestimonyData tr = NewController("karelia_1940", save, archive, "tr-TR").Story.testimony;
            for (int i = 0; i < indices.Length; i++)
                Assert.That(tr.done[indices[i]].conditions[0].key, Is.EqualTo(en.done[indices[i]].conditions[0].key));
        }

        [Test]
        public void OldArchiveWithoutNewFields_LoadsWithEmptyThreads()
        {
            File.WriteAllText(Path.Combine(tempDirectory, ArchiveService.FileName),
                "{\"schemaVersion\":1,\"chapters\":[{\"storyId\":\"hamburg_1943\",\"completedRuns\":1}]}");
            ArchiveService archive = new ArchiveService(tempDirectory);
            archive.Load();
            Assert.That(archive.HasCompleted("hamburg_1943"), Is.True);
            Assert.That(archive.SeenCrossings(), Is.Empty);
            string mode;
            int[] lines;
            Assert.That(archive.TryGetTestimony("hamburg_1943", out mode, out lines), Is.False);
        }
    }
}
