using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace OrdinaryFronts.Tests.EditMode
{
    /// <summary>
    /// Yerelleştirmede en büyük risk bir dilde eksik ya da yapısal olarak kaymış içerik
    /// kalmasıdır: oyun derlenir, testler geçer, fakat oyuncu boş bir düğme veya yanlış
    /// dalda bir hikâye görür. Bu testler her iki riski de kapatır.
    /// </summary>
    public sealed class LocalizationTests
    {
        private static string StreamingRoot { get { return Path.Combine(Application.dataPath, "StreamingAssets"); } }

        [Test]
        public void DefaultLocale_IsEnglish()
        {
            Assert.That(LocalizationService.DefaultLocale, Is.EqualTo("en-US"),
                "Yeni kurulum İngilizce başlamalıdır.");
            Assert.That(new SettingsData().locale, Is.EqualTo("en-US"),
                "Ayar dosyası olmayan bir kurulumda dil İngilizce olmalıdır.");
        }

        [Test]
        public void SupportedLocales_AreNormalizedAndCycle()
        {
            Assert.That(LocalizationService.SupportedLocales, Has.Length.GreaterThanOrEqualTo(2));
            Assert.That(LocalizationService.Normalize("EN-us"), Is.EqualTo("en-US"));
            Assert.That(LocalizationService.Normalize("hiçbir-dil"), Is.EqualTo(LocalizationService.DefaultLocale),
                "Tanınmayan dil varsayılana düşmelidir.");

            // Sıradaki dil döngüsü bütün dilleri dolaşıp başa dönmelidir.
            string locale = LocalizationService.DefaultLocale;
            HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < LocalizationService.SupportedLocales.Length; i++)
            {
                Assert.That(seen.Add(locale), Is.True, "Dil döngüsü erken tekrar etti: " + locale);
                locale = LocalizationService.NextLocale(locale);
            }
            Assert.That(locale, Is.EqualTo(LocalizationService.DefaultLocale), "Dil döngüsü başa dönmelidir.");
        }

        [Test]
        public void EveryLocale_ProvidesEveryUiKey()
        {
            string[] keys = UiKey.All();
            Assert.That(keys, Is.Not.Empty);

            foreach (string locale in LocalizationService.SupportedLocales)
            {
                string path = LocalizationService.PathFor(locale, StreamingRoot);
                Assert.That(File.Exists(path), Is.True, "Dil dosyası yok: " + path);

                LocalizationService service = new LocalizationService(StreamingRoot);
                service.Load(locale);

                List<string> missing = new List<string>();
                foreach (string key in keys)
                    if (!service.Has(key)) missing.Add(key);
                Assert.That(missing, Is.Empty, locale + " için eksik anahtarlar: " + string.Join(", ", missing.ToArray()));

                foreach (string key in keys)
                    Assert.That(service.Get(key), Is.Not.Empty, locale + " içinde boş metin: " + key);
            }
        }

        /// <summary>
        /// Kültürden bağımsız büyütme Türkçede yanlıştır: "Sirenler" → "SIRENLER" (noktasız I)
        /// verir, doğrusu "SİRENLER"dir. Bölüm başlığı ve final raporu bu dönüşümü kullandığı
        /// için hata doğrudan ekranda görünürdü.
        /// </summary>
        [Test]
        public void Uppercase_FollowsTheActiveLocaleRules()
        {
            LocalizationService turkish = new LocalizationService(StreamingRoot);
            turkish.Load("tr-TR");
            Assert.That(turkish.ToUpper("Sirenler"), Is.EqualTo("SİRENLER"),
                "Türkçede 'i' harfi noktalı 'İ' olmalı.");

            LocalizationService english = new LocalizationService(StreamingRoot);
            english.Load("en-US");
            Assert.That(english.ToUpper("Sirens"), Is.EqualTo("SIRENS"));
        }

        [Test]
        public void EveryLocale_HasACatalogWhoseCardsPointAtRealChapters()
        {
            foreach (string locale in LocalizationService.SupportedLocales)
            {
                StoryCatalog catalog = StoryCatalog.Load(locale, StreamingRoot);
                Assert.That(catalog.entries, Is.Not.Empty, locale);

                int playable = 0;
                for (int i = 0; i < catalog.entries.Length; i++)
                {
                    StoryCatalogEntry entry = catalog.entries[i];
                    // Hazırlanmakta olan bölümlerin başlığı bilerek boştur: kartları yalnız bir
                    // soru işareti taşır, uydurma bir ad yanlış beklenti yaratırdı.
                    if (!entry.IsPlayable)
                    {
                        Assert.That(entry.title, Is.Empty, locale + " kilitli kart adsız olmalı");
                        continue;
                    }
                    playable++;
                    Assert.That(entry.title, Is.Not.Empty, locale + " başlıksız oynanabilir kart");
                    Assert.That(entry.line, Is.Not.Empty, locale + " tanıtımsız oynanabilir kart");

                    // Oynanabilir bir kartın hikâye dosyası gerçekten bulunmalıdır; aksi hâlde
                    // oyuncu karta basınca hata ekranıyla karşılaşır.
                    string storyPath = Path.Combine(StreamingRoot, StoryRepository.RelativePathFor(locale, entry.storyId));
                    Assert.That(File.Exists(storyPath), Is.True, "Kart dosyasız: " + storyPath);

                    // Kart görseli, kartın kendi bölümünde fiilen kullanılan bir sahne olmalıdır.
                    // Antoloji büyüdüğü için bunu tek bir hikâyenin anahtarlarına bakarak
                    // denetlemek yanlış olurdu: her kart kendi dosyasına karşı doğrulanır.
                    StoryDatabase story = JsonUtility.FromJson<StoryDatabase>(File.ReadAllText(storyPath));
                    HashSet<string> imageKeys = new HashSet<string>();
                    for (int n = 0; n < story.nodes.Length; n++)
                        if (!string.IsNullOrWhiteSpace(story.nodes[n].imageKey)) imageKeys.Add(story.nodes[n].imageKey);
                    Assert.That(imageKeys.Contains(entry.imageKey), Is.True,
                        locale + "/" + entry.storyId + " kartında bilinmeyen görsel anahtarı: " + entry.imageKey);
                }
                Assert.That(playable, Is.GreaterThanOrEqualTo(1), locale + " için oynanabilir bölüm sayısı");
            }
        }

        [Test]
        public void Catalogs_ShareTheSameChapterOrderAcrossLocales()
        {
            StoryCatalog reference = StoryCatalog.Load(LocalizationService.DefaultLocale, StreamingRoot);
            foreach (string locale in LocalizationService.SupportedLocales)
            {
                if (locale == LocalizationService.DefaultLocale) continue;
                StoryCatalog other = StoryCatalog.Load(locale, StreamingRoot);
                Assert.That(other.entries.Length, Is.EqualTo(reference.entries.Length), locale + " kart sayısı");
                for (int i = 0; i < reference.entries.Length; i++)
                {
                    Assert.That(other.entries[i].storyId, Is.EqualTo(reference.entries[i].storyId), locale + " kart kimliği #" + i);
                    Assert.That(other.entries[i].available, Is.EqualTo(reference.entries[i].available), locale + " kart erişimi #" + i);
                }
            }
        }

        [Test]
        public void StoryId_CannotEscapeTheStoryFolder()
        {
            // Kimlik katalog verisinden gelir ve dosya yoluna girer.
            Assert.That(StoryRepository.SanitizeStoryId("../../../windows/system32"), Does.Not.Contain(".."));
            Assert.That(StoryRepository.SanitizeStoryId("../../evil"), Does.Not.Contain("/"));
            Assert.That(StoryRepository.SanitizeStoryId(""), Is.EqualTo(StoryRepository.DefaultStoryId));
            Assert.That(StoryRepository.SanitizeStoryId("hamburg_1943"), Is.EqualTo("hamburg_1943"));
        }

        [Test]
        public void EveryLocale_HasAValidStoryGraph()
        {
            foreach (string locale in LocalizationService.SupportedLocales)
            {
                string path = Path.Combine(StreamingRoot, StoryRepository.RelativePathFor(locale));
                Assert.That(File.Exists(path), Is.True, "Hikâye dosyası yok: " + path);

                StoryDatabase story = JsonUtility.FromJson<StoryDatabase>(File.ReadAllText(path));
                Assert.That(story, Is.Not.Null, locale);
                List<string> issues = StoryGraphValidator.Validate(story);
                Assert.That(issues, Is.Empty, locale + " doğrulama sorunları:\n" + string.Join("\n", issues.ToArray()));
            }
        }

        /// <summary>
        /// Diller yalnız metinde ayrışmalıdır. Kimlikler, hedef düğümler veya etkiler
        /// kayarsa oyuncu dili değiştirdiğinde kaydı geçersizleşir ya da farklı bir dalda
        /// devam eder; bu, çeviri sırasında yapılması en kolay hatadır.
        /// </summary>
        [Test]
        public void AllLocales_ShareTheSameGraphTopology()
        {
            StoryDatabase reference = LoadStory(LocalizationService.DefaultLocale);
            Dictionary<string, StoryNode> referenceNodes = Index(reference);

            foreach (string locale in LocalizationService.SupportedLocales)
            {
                if (locale == LocalizationService.DefaultLocale) continue;
                StoryDatabase other = LoadStory(locale);

                Assert.That(other.storyId, Is.EqualTo(reference.storyId), locale);
                Assert.That(other.startNodeId, Is.EqualTo(reference.startNodeId), locale);
                Assert.That(other.nodes.Length, Is.EqualTo(reference.nodes.Length), locale + " düğüm sayısı");
                Assert.That(other.intro.beats.Length, Is.EqualTo(reference.intro.beats.Length), locale + " açılış kartı sayısı");

                Dictionary<string, StoryNode> otherNodes = Index(other);
                foreach (KeyValuePair<string, StoryNode> pair in referenceNodes)
                {
                    StoryNode expected = pair.Value;
                    Assert.That(otherNodes.ContainsKey(pair.Key), Is.True, locale + " içinde eksik düğüm: " + pair.Key);
                    StoryNode actual = otherNodes[pair.Key];

                    Assert.That(actual.imageKey, Is.EqualTo(expected.imageKey), pair.Key + " görsel anahtarı");
                    Assert.That(actual.choices.Length, Is.EqualTo(expected.choices.Length), pair.Key + " seçim sayısı");
                    for (int i = 0; i < expected.choices.Length; i++)
                    {
                        Assert.That(actual.choices[i].id, Is.EqualTo(expected.choices[i].id), pair.Key + " seçim kimliği");
                        Assert.That(actual.choices[i].nextNodeId, Is.EqualTo(expected.choices[i].nextNodeId), pair.Key + " hedef düğüm");
                        AssertSameEffects(pair.Key + " #" + i, expected.choices[i].effects, actual.choices[i].effects);
                    }

                    Assert.That(actual.echoes.Length, Is.EqualTo(expected.echoes.Length), pair.Key + " yankı sayısı");
                    for (int i = 0; i < expected.echoes.Length; i++)
                        Assert.That(actual.echoes[i].id, Is.EqualTo(expected.echoes[i].id), pair.Key + " yankı kimliği");

                    Assert.That(actual.IsEnding, Is.EqualTo(expected.IsEnding), pair.Key + " final durumu");
                    if (expected.IsEnding)
                        Assert.That(actual.ending.id, Is.EqualTo(expected.ending.id), pair.Key + " final kimliği");
                }
            }
        }

        private static void AssertSameEffects(string owner, EffectData[] expected, EffectData[] actual)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length), owner + " etki sayısı");
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(actual[i].type, Is.EqualTo(expected[i].type), owner + " etki türü");
                Assert.That(actual[i].key, Is.EqualTo(expected[i].key), owner + " etki anahtarı");
                Assert.That(actual[i].op, Is.EqualTo(expected[i].op), owner + " etki operasyonu");
                Assert.That(actual[i].intValue, Is.EqualTo(expected[i].intValue), owner + " etki değeri");
            }
        }

        private static StoryDatabase LoadStory(string locale)
        {
            string path = Path.Combine(StreamingRoot, StoryRepository.RelativePathFor(locale));
            return JsonUtility.FromJson<StoryDatabase>(File.ReadAllText(path));
        }

        private static Dictionary<string, StoryNode> Index(StoryDatabase story)
        {
            Dictionary<string, StoryNode> nodes = new Dictionary<string, StoryNode>();
            for (int i = 0; i < story.nodes.Length; i++) nodes[story.nodes[i].id] = story.nodes[i];
            return nodes;
        }
    }
}
