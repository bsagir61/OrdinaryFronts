using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace OrdinaryFronts.Tests.EditMode
{
    /// <summary>
    /// Bölüm kayıtları (1.6): her bölüm kendi kaydını tutar; bir bölüme başlamak öbürünü
    /// silmez; oyun yeniden açıldığında ilk bölüm dışındaki bir bölüme de devam edilebilir;
    /// 1.5 ve öncesinin tek kayıt dosyası doğru bölüme taşınır.
    /// </summary>
    public sealed class SaveSlotTests
    {
        private string tempDirectory;

        private static string StreamingRoot { get { return Path.Combine(Application.dataPath, "StreamingAssets"); } }

        [SetUp]
        public void SetUp()
        {
            tempDirectory = Path.Combine(Path.GetTempPath(), "ordinary-fronts-slot-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempDirectory)) Directory.Delete(tempDirectory, true);
        }

        private StoryController NewController(string storyId, SaveService save)
        {
            string path = Path.Combine(StreamingRoot, StoryRepository.RelativePathFor("en-US", storyId));
            StoryRepository repository = new StoryRepository(path, "en-US", storyId);
            repository.Load();
            return new StoryController(repository, save, null);
        }

        [Test]
        public void StartingAChapter_DoesNotEraseAnotherChaptersProgress()
        {
            SaveService save = new SaveService(tempDirectory);
            StoryController hamburg = NewController("hamburg_1943", save);
            hamburg.StartNew();
            hamburg.Choose(0);
            hamburg.Choose(0);
            string hamburgNode = hamburg.State.currentNodeId;

            StoryController neretva = NewController("neretva_1943", save);
            neretva.StartNew();
            neretva.Choose(1);

            Assert.That(save.HasSaveFor("hamburg_1943"), Is.True);
            Assert.That(save.HasSaveFor("neretva_1943"), Is.True);
            Assert.That(save.Peek("hamburg_1943").currentNodeId, Is.EqualTo(hamburgNode));
        }

        [Test]
        public void AfterRestart_AnyChapterCanBeContinued_AndAChapterWithoutASaveSaysSo()
        {
            SaveService first = new SaveService(tempDirectory);
            StoryController karelia = NewController("karelia_1940", first);
            karelia.StartNew();
            karelia.Choose(1);
            string node = karelia.State.currentNodeId;

            // Oyun yeniden açıldı: yeni servis, yeni denetleyici. 1.5'te bu yol "kayıt uyumsuz" veriyordu.
            SaveService second = new SaveService(tempDirectory);
            Assert.That(second.MostRecentStoryId(), Is.EqualTo("karelia_1940"));
            StoryController resumed = NewController("karelia_1940", second);
            string message;
            Assert.That(resumed.TryContinue(out message), Is.True, message);
            Assert.That(resumed.State.currentNodeId, Is.EqualTo(node));

            StoryController hamburg = NewController("hamburg_1943", second);
            Assert.That(hamburg.TryContinue(out message), Is.False);
            Assert.That(message, Is.EqualTo(UiKey.SaveNone));
        }

        [Test]
        public void MostRecent_FollowsTheLastSavedChapter()
        {
            SaveService save = new SaveService(tempDirectory);
            NewController("hamburg_1943", save).StartNew();
            NewController("amsterdam_1945", save).StartNew();
            File.SetLastWriteTimeUtc(save.PathFor("hamburg_1943"), DateTime.UtcNow.AddMinutes(-5));
            Assert.That(save.MostRecentStoryId(), Is.EqualTo("amsterdam_1945"));
            Assert.That(save.SavedStoryIds(), Is.EqualTo(new[] { "amsterdam_1945", "hamburg_1943" }));
        }

        [Test]
        public void LegacySingleFile_MovesToItsChapter()
        {
            SaveService writer = new SaveService(tempDirectory);
            StoryController neretva = NewController("neretva_1943", writer);
            neretva.StartNew();
            neretva.Choose(0);
            string node = neretva.State.currentNodeId;
            File.Move(writer.PathFor("neretva_1943"), Path.Combine(tempDirectory, SaveService.SaveFileName));

            SaveService migrated = new SaveService(tempDirectory);
            Assert.That(File.Exists(Path.Combine(tempDirectory, SaveService.SaveFileName)), Is.False);
            Assert.That(migrated.HasSaveFor("neretva_1943"), Is.True);
            Assert.That(migrated.Peek("neretva_1943").currentNodeId, Is.EqualTo(node));
        }

        [Test]
        public void UnreadableLegacyFile_IsKept_AndReportedWhenNothingElseExists()
        {
            File.WriteAllText(Path.Combine(tempDirectory, SaveService.SaveFileName), "{ broken");
            SaveService save = new SaveService(tempDirectory);
            GameState state;
            string message;
            Assert.That(save.HasSave, Is.True);
            Assert.That(save.TryLoad(out state, out message), Is.False);
            Assert.That(message, Is.EqualTo(UiKey.SaveUnreadable));
            Assert.That(File.Exists(Path.Combine(tempDirectory, SaveService.SaveFileName)), Is.True);
        }

        [Test]
        public void StoryId_CannotEscapeTheSaveFolder()
        {
            SaveService save = new SaveService(tempDirectory);
            Assert.That(SaveService.FileNameFor("../../evil"), Is.EqualTo("save-evil.json"));
            Assert.That(Path.GetDirectoryName(save.PathFor("..\\x")), Is.EqualTo(tempDirectory));
        }

        [Test]
        public void HandScenesSetting_RoundTripsAndIsClamped()
        {
            SettingsService service = new SettingsService(tempDirectory);
            service.Save(new SettingsData { handScenes = SettingsData.HandScenesFree });
            Assert.That(service.Load().handScenes, Is.EqualTo(SettingsData.HandScenesFree));
            service.Save(new SettingsData { handScenes = 9 });
            Assert.That(service.Load().handScenes, Is.EqualTo(SettingsData.HandScenesFree));
            File.WriteAllText(service.SettingsPath, "{\"schemaVersion\":2,\"locale\":\"en-US\",\"masterVolume\":0.5}");
            Assert.That(service.Load().handScenes, Is.EqualTo(SettingsData.HandScenesStandard), "eski ayar dosyası standart açılmalı");
        }
    }
}
