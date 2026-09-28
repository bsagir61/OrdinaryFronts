using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace OrdinaryFronts.Tests.EditMode
{
    /// <summary>
    /// Antolojiye eklenen her bölüm, Hamburg'un geçtiği aynı kapılardan geçmelidir.
    /// <see cref="StoryGraphTests"/> yalnız varsayılan hikâyeyi okur; katalogtan gelen
    /// bölümler orada denetlenmez ve denetimsiz bir bölüm sessizce bozuk kalabilir.
    /// Bu sınıf katalogdaki bütün oynanabilir bölümleri bütün dillerde gezer.
    /// </summary>
    public sealed class StoryCatalogTests
    {
        private static string StreamingRoot
        {
            get { return Path.Combine(Application.dataPath, "StreamingAssets"); }
        }

        private static StoryDatabase LoadStory(string locale, string storyId)
        {
            string path = Path.Combine(StreamingRoot, StoryRepository.RelativePathFor(locale, storyId));
            Assert.That(File.Exists(path), "Hikâye dosyası yok: " + path);
            StoryDatabase story = JsonUtility.FromJson<StoryDatabase>(File.ReadAllText(path));
            Assert.That(story, Is.Not.Null, path);
            return story;
        }

        private static List<StoryCatalogEntry> PlayableEntries(string locale)
        {
            StoryCatalog catalog = StoryCatalog.Load(locale, StreamingRoot);
            List<StoryCatalogEntry> playable = new List<StoryCatalogEntry>();
            for (int i = 0; i < catalog.entries.Length; i++)
                if (catalog.entries[i] != null && catalog.entries[i].IsPlayable) playable.Add(catalog.entries[i]);
            return playable;
        }

        [Test]
        public void Catalog_ListsAtLeastOnePlayableStoryInEveryLocale()
        {
            foreach (string locale in LocalizationService.SupportedLocales)
                Assert.That(PlayableEntries(locale), Is.Not.Empty, locale);
        }

        /// <summary>
        /// Katalog diller arasında aynı bölümleri aynı sırayla sunmalıdır: kart listesi
        /// dile göre değişirse dil değiştiren oyuncunun seçtiği bölüm kayar.
        /// </summary>
        [Test]
        public void Catalog_HasTheSameEntriesInEveryLocale()
        {
            string reference = LocalizationService.SupportedLocales[0];
            StoryCatalog first = StoryCatalog.Load(reference, StreamingRoot);
            foreach (string locale in LocalizationService.SupportedLocales)
            {
                StoryCatalog other = StoryCatalog.Load(locale, StreamingRoot);
                Assert.That(other.entries.Length, Is.EqualTo(first.entries.Length), locale);
                for (int i = 0; i < first.entries.Length; i++)
                {
                    Assert.That(other.entries[i].storyId, Is.EqualTo(first.entries[i].storyId), locale + " #" + i);
                    Assert.That(other.entries[i].available, Is.EqualTo(first.entries[i].available), locale + " #" + i);
                    Assert.That(other.entries[i].imageKey, Is.EqualTo(first.entries[i].imageKey), locale + " #" + i);
                    if (!first.entries[i].IsPlayable) continue;
                    Assert.That(other.entries[i].title, Is.Not.Empty, locale + " #" + i);
                    Assert.That(other.entries[i].period, Is.Not.Empty, locale + " #" + i);
                    Assert.That(other.entries[i].line, Is.Not.Empty, locale + " #" + i);
                }
            }
        }

        /// <summary>
        /// Seçim ekranı bir harita olduğundan beri oynanabilir her bölümün gerçek bir yeri
        /// olmalı ve o yer haritanın içine düşmeli. Yersiz bir bölüm ekranda hiç görünmez;
        /// oyuncu onu başlatamaz ve kimse fark etmez.
        /// </summary>
        [Test]
        public void EveryPlayableStory_HasALocationInsideTheMap()
        {
            foreach (string locale in LocalizationService.SupportedLocales)
            {
                foreach (StoryCatalogEntry entry in PlayableEntries(locale))
                {
                    string label = locale + "/" + entry.storyId;
                    Assert.That(entry.HasLocation, Is.True, label + " için enlem/boylam yok");
                    Assert.That(MapProjection.IsInside(entry.latitude, entry.longitude), Is.True,
                        label + " haritanın dışında: " + entry.latitude + ", " + entry.longitude);
                    UnityEngine.Vector2 uv = MapProjection.Project(entry.latitude, entry.longitude);
                    // Kenara yapışık bir işaret etiketiyle birlikte kadrajdan taşar.
                    Assert.That(uv.x, Is.InRange(0.03f, 0.85f), label + " işaret etiketi kadrajdan taşar (u=" + uv.x + ")");
                    Assert.That(uv.y, Is.InRange(0.03f, 0.97f), label + " (v=" + uv.y + ")");
                }
            }
        }

        [Test]
        public void EveryPlayableStory_PassesGraphValidation()
        {
            foreach (string locale in LocalizationService.SupportedLocales)
            {
                foreach (StoryCatalogEntry entry in PlayableEntries(locale))
                {
                    StoryDatabase story = LoadStory(locale, entry.storyId);
                    List<string> issues = StoryGraphValidator.Validate(story);
                    Assert.That(issues, Is.Empty, locale + "/" + entry.storyId + "\n" + string.Join("\n", issues.ToArray()));
                }
            }
        }

        /// <summary>
        /// GDD §19 kalite kapıları: bölüm başına en az 28 düğüm, en az beş final, her rotada
        /// 14-18 karar ve en az sekiz gecikmeli yankı.
        /// </summary>
        [Test]
        public void EveryPlayableStory_MeetsTheChapterQualityGates()
        {
            foreach (string locale in LocalizationService.SupportedLocales)
            {
                foreach (StoryCatalogEntry entry in PlayableEntries(locale))
                {
                    string label = locale + "/" + entry.storyId;
                    StoryDatabase story = LoadStory(locale, entry.storyId);

                    Assert.That(story.nodes.Length, Is.GreaterThanOrEqualTo(28), label);

                    int endings = 0;
                    int delayedEchoes = 0;
                    HashSet<string> acts = new HashSet<string>();
                    for (int i = 0; i < story.nodes.Length; i++)
                    {
                        StoryNode node = story.nodes[i];
                        if (node.IsEnding)
                        {
                            endings++;
                            Assert.That(node.ending.paragraphs.Length, Is.InRange(2, 4), label + " " + node.id);
                            Assert.That(node.ending.traceFallbacks.Length, Is.GreaterThanOrEqualTo(3), label + " " + node.id);
                        }
                        else
                        {
                            Assert.That(node.choices, Has.Length.EqualTo(2), label + " " + node.id);
                            Assert.That(node.body, Is.Not.Empty, label + " " + node.id);
                        }
                        if (!string.IsNullOrEmpty(node.act)) acts.Add(node.act);
                        delayedEchoes += node.echoes == null ? 0 : node.echoes.Length;
                    }

                    Assert.That(endings, Is.GreaterThanOrEqualTo(5), label);
                    Assert.That(delayedEchoes, Is.GreaterThanOrEqualTo(8), label);
                    Assert.That(acts.Count, Is.EqualTo(3), label + " bölüm sayısı: " + string.Join(", ", new List<string>(acts).ToArray()));

                    List<int> counts = StoryGraphValidator.EndingDecisionCounts(story);
                    Assert.That(counts, Is.Not.Empty, label);
                    Assert.That(counts, Has.All.InRange(14, 18), label);
                }
            }
        }

        /// <summary>
        /// Her bölüm, ürettiği bütün ilişki anahtarları için bir kişi tanımlamalıdır.
        /// İlişki değerleri yalnız final raporundaki "İnsanlar" bölümünde görünür; tanımı
        /// olmayan bir anahtar, oyuncunun bütün bir bölüm boyunca etkilediği birinin hiç
        /// görünmemesi demektir.
        /// </summary>
        [Test]
        public void EveryPlayableStory_NamesThePeopleBehindItsRelations()
        {
            foreach (string locale in LocalizationService.SupportedLocales)
            {
                foreach (StoryCatalogEntry entry in PlayableEntries(locale))
                {
                    string label = locale + "/" + entry.storyId;
                    StoryDatabase story = LoadStory(locale, entry.storyId);

                    HashSet<string> relationKeys = new HashSet<string>();
                    for (int i = 0; i < story.nodes.Length; i++)
                    {
                        StoryNode node = story.nodes[i];
                        if (node.choices == null) continue;
                        for (int c = 0; c < node.choices.Length; c++)
                        {
                            EffectData[] effects = node.choices[c].effects;
                            if (effects == null) continue;
                            for (int e = 0; e < effects.Length; e++)
                                if (effects[e] != null
                                    && StoryVocabulary.Normalize(effects[e].type) == StoryVocabulary.TypeRelation
                                    && !string.IsNullOrWhiteSpace(effects[e].key))
                                    relationKeys.Add(effects[e].key);
                        }
                    }

                    Assert.That(relationKeys, Is.Not.Empty, label + " hiç ilişki üretmiyor");
                    Assert.That(story.characters, Is.Not.Null.And.Not.Empty, label + " kişi tanımı yok");

                    HashSet<string> named = new HashSet<string>();
                    for (int i = 0; i < story.characters.Length; i++)
                    {
                        Assert.That(story.characters[i].IsComplete, Is.True, label + " eksik kişi #" + i);
                        named.Add(story.characters[i].key);
                    }
                    foreach (string key in relationKeys)
                        Assert.That(named.Contains(key), Is.True, label + " adsız ilişki: " + key);
                }
            }
        }

        [Test]
        public void EveryPlayableStory_HasAnIntroWithinTheTimeBudget()
        {
            foreach (string locale in LocalizationService.SupportedLocales)
            {
                foreach (StoryCatalogEntry entry in PlayableEntries(locale))
                {
                    string label = locale + "/" + entry.storyId;
                    StoryDatabase story = LoadStory(locale, entry.storyId);
                    Assert.That(story.intro, Is.Not.Null, label);
                    Assert.That(story.intro.HasBeats, Is.True, label);
                    Assert.That(story.intro.beats.Length, Is.InRange(3, 6), label);
                    Assert.That(story.intro.EstimatedTotalSeconds, Is.InRange(6f, 20f), label);
                    for (int i = 0; i < story.intro.beats.Length; i++)
                    {
                        Assert.That(story.intro.beats[i].line, Is.Not.Empty, label + " #" + i);
                        Assert.That(story.intro.beats[i].kicker, Is.Not.Empty, label + " #" + i);
                    }
                }
            }
        }

        /// <summary>
        /// Diller arasında yapı birebir aynı olmalıdır. Oyuncu oyunun ortasında dil
        /// değiştirdiğinde kaydı korunur ve yalnız kaynak dosya değişir; düğüm veya seçim
        /// kimlikleri kayarsa kayıt geçersizleşir ve oyun sessizce başka bir düğüme atlar.
        /// </summary>
        [Test]
        public void EveryPlayableStory_HasIdenticalStructureAcrossLocales()
        {
            string reference = LocalizationService.SupportedLocales[0];
            foreach (StoryCatalogEntry entry in PlayableEntries(reference))
            {
                StoryDatabase first = LoadStory(reference, entry.storyId);
                foreach (string locale in LocalizationService.SupportedLocales)
                {
                    if (locale == reference) continue;
                    string label = entry.storyId + " " + reference + " vs " + locale;
                    StoryDatabase other = LoadStory(locale, entry.storyId);

                    Assert.That(other.startNodeId, Is.EqualTo(first.startNodeId), label);
                    Assert.That(other.nodes.Length, Is.EqualTo(first.nodes.Length), label);

                    Dictionary<string, StoryNode> otherNodes = new Dictionary<string, StoryNode>();
                    for (int i = 0; i < other.nodes.Length; i++) otherNodes[other.nodes[i].id] = other.nodes[i];

                    for (int i = 0; i < first.nodes.Length; i++)
                    {
                        StoryNode node = first.nodes[i];
                        Assert.That(otherNodes.ContainsKey(node.id), Is.True, label + " eksik düğüm: " + node.id);
                        StoryNode twin = otherNodes[node.id];

                        Assert.That(twin.imageKey, Is.EqualTo(node.imageKey), label + " " + node.id);
                        Assert.That(twin.IsEnding, Is.EqualTo(node.IsEnding), label + " " + node.id);
                        Assert.That(twin.choices.Length, Is.EqualTo(node.choices.Length), label + " " + node.id);
                        for (int c = 0; c < node.choices.Length; c++)
                        {
                            Assert.That(twin.choices[c].id, Is.EqualTo(node.choices[c].id), label + " " + node.id);
                            Assert.That(twin.choices[c].nextNodeId, Is.EqualTo(node.choices[c].nextNodeId), label + " " + node.id);
                            Assert.That(twin.choices[c].text, Is.Not.Empty, label + " " + node.id);
                            Assert.That(twin.choices[c].trace, Is.Not.Empty, label + " " + node.id);
                        }
                        Assert.That(twin.echoes.Length, Is.EqualTo(node.echoes.Length), label + " " + node.id);
                        // Kişi listesi de diller arasında aynı olmalı; ad ve cümleler çevrilir,
                        // anahtarlar çevrilmez.
                        Assert.That(other.characters.Length, Is.EqualTo(first.characters.Length), label);
                        for (int e = 0; e < node.echoes.Length; e++)
                        {
                            Assert.That(twin.echoes[e].id, Is.EqualTo(node.echoes[e].id), label + " " + node.id);
                            Assert.That(twin.echoes[e].text, Is.Not.Empty, label + " " + node.id);
                        }
                        if (!node.IsEnding) continue;
                        Assert.That(twin.ending.id, Is.EqualTo(node.ending.id), label + " " + node.id);
                        Assert.That(twin.ending.paragraphs.Length, Is.EqualTo(node.ending.paragraphs.Length), label + " " + node.id);
                        Assert.That(twin.ending.title, Is.Not.Empty, label + " " + node.id);
                    }
                }
            }
        }

        /// <summary>
        /// Kartlarda ve düğümlerde geçen her görsel anahtarının sahnede bir karşılığı
        /// olmalıdır; yazım hatası oyunu bozmaz, sessizce boş bir arka plan bırakır.
        /// </summary>
        [Test]
        public void EveryImageKey_HasAGeneratedBackground()
        {
            string artRoot = Path.Combine(Application.dataPath, "OrdinaryFronts", "Art", "Generated");
            HashSet<string> keys = new HashSet<string>();
            foreach (string locale in LocalizationService.SupportedLocales)
            {
                StoryCatalog catalog = StoryCatalog.Load(locale, StreamingRoot);
                for (int i = 0; i < catalog.entries.Length; i++)
                    if (catalog.entries[i] != null && !string.IsNullOrWhiteSpace(catalog.entries[i].imageKey))
                        keys.Add(catalog.entries[i].imageKey);

                foreach (StoryCatalogEntry entry in PlayableEntries(locale))
                {
                    StoryDatabase story = LoadStory(locale, entry.storyId);
                    for (int i = 0; i < story.nodes.Length; i++)
                        if (!string.IsNullOrWhiteSpace(story.nodes[i].imageKey)) keys.Add(story.nodes[i].imageKey);
                    for (int i = 0; i < story.intro.beats.Length; i++)
                        if (!string.IsNullOrWhiteSpace(story.intro.beats[i].imageKey)) keys.Add(story.intro.beats[i].imageKey);
                }
            }

            Assert.That(keys, Is.Not.Empty);
            foreach (string key in keys)
                Assert.That(File.Exists(Path.Combine(artRoot, "bg_" + key + ".png")), Is.True, "Arka plan yok: bg_" + key + ".png");
        }
    }
}
