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
