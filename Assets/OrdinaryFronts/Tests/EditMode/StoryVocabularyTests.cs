using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace OrdinaryFronts.Tests.EditMode
{
    /// <summary>
    /// Hikâye verisindeki tür/operasyon/anahtar yazım hatalarının sessizce farklı bir davranışa
    /// dönüşmediğini doğrular. Bu testler gerçek hikâye dosyasını değil, kasıtlı olarak bozulmuş
    /// küçük grafikleri kullanır.
    /// </summary>
    public sealed class StoryVocabularyTests
    {
        [Test]
        public void Validator_ReportsUnknownEffectOperation()
        {
            StoryDatabase story = BuildStory(
                new[] { new EffectData { type = "relation", key = "olek", op = "ad", intValue = 5 } },
                null);
            AssertReports(story, "Bilinmeyen etki operasyonu");
        }

        [Test]
        public void Validator_ReportsUnknownEffectType()
        {
            // Kaldırılan "stat" türü artık tanınmayan bir tür olarak bildirilmelidir.
            StoryDatabase story = BuildStory(
                new[] { new EffectData { type = "stat", key = "resilience", op = "add", intValue = 5 } },
                null);
            AssertReports(story, "Bilinmeyen etki türü");
        }

        [Test]
        public void Validator_ReportsThresholdOperationOnFlagCondition()
        {
            // "atleast" yalnız sayısal türlerde anlamlıdır; bayrakta sessizce "equals" gibi davranırdı.
            StoryDatabase story = BuildStory(
                new[] { new EffectData { type = "flag", key = "bayrak", op = "set", boolValue = true } },
                new[] { new ConditionData { type = "flag", key = "bayrak", op = "atleast", intValue = 1 } });
            AssertReports(story, "Bilinmeyen koşul operasyonu");
        }

        [Test]
        public void Validator_ReportsChoiceConditionOnUngeneratedFlag()
        {
            // Seçim koşulları daha önce hiç doğrulanmıyordu.
            StoryDatabase story = BuildStory(
                null,
                new[] { new ConditionData { type = "flag", key = "hicbir_etkinin_uretmedigi_bayrak", op = "equals", boolValue = true } });
            AssertReports(story, "Üretilmeyen bayrağa bağlı");
        }

        [Test]
        public void Validator_ReportsIntroBeatWithUnknownImageKey()
        {
            StoryDatabase story = BuildStory(null, null);
            story.intro = new IntroData
            {
                beats = new[] { new IntroBeat { imageKey = "hicbir_dugumun_kullanmadigi_gorsel", line = "Açılış metni.", holdSeconds = 3f } }
            };
            AssertReports(story, "Açılış kartında bilinmeyen görsel anahtarı");
        }

        [Test]
        public void Validator_ReportsEmptyIntroBeat()
        {
            StoryDatabase story = BuildStory(null, null);
            story.intro = new IntroData
            {
                beats = new[] { new IntroBeat { imageKey = "test_gorsel", line = "   ", holdSeconds = 3f } }
            };
            AssertReports(story, "Boş açılış kartı");
        }

        [Test]
        public void IntroBeat_ClampsUnreasonableHoldDurations()
        {
            Assert.That(new IntroBeat { holdSeconds = 0f }.ResolvedHold, Is.EqualTo(3.2f).Within(0.001f), "Eksik süre varsayılana düşmeli.");
            Assert.That(new IntroBeat { holdSeconds = 900f }.ResolvedHold, Is.EqualTo(IntroBeat.MaximumHold));
            Assert.That(new IntroBeat { holdSeconds = 0.05f }.ResolvedHold, Is.EqualTo(IntroBeat.MinimumHold));
        }

        [Test]
        public void EffectResolver_IgnoresUnknownOperationInsteadOfOverwritingRelation()
        {
            GameState state = new GameState();
            state.SetRelation("olek", 50);
            LogAssert.Expect(LogType.Error, new Regex("Bilinmeyen etki operasyonu"));
            EffectResolver.Apply(new[] { new EffectData { type = "relation", key = "olek", op = "ad", intValue = 5 } }, state);
            Assert.That(state.GetRelation("olek"), Is.EqualTo(50), "Yazım hatalı operasyon ilişkiyi sessizce değiştirmemeli.");
        }

        [Test]
        public void EffectResolver_StillAppliesValidAddAndSet()
        {
            GameState state = new GameState();
            state.SetRelation("olek", 50);
            EffectResolver.Apply(new[] { new EffectData { type = "relation", key = "olek", op = "add", intValue = 5 } }, state);
            Assert.That(state.GetRelation("olek"), Is.EqualTo(55));
            EffectResolver.Apply(new[] { new EffectData { type = "relation", key = "olek", op = "set", intValue = 20 } }, state);
            Assert.That(state.GetRelation("olek"), Is.EqualTo(20));
        }

        [Test]
        public void ConditionEvaluator_FailsClosedOnUnknownOperation()
        {
            GameState state = new GameState();
            state.SetRelation("olek", 80);
            ConditionData condition = new ConditionData { type = "relation", key = "olek", op = "greaterthan", intValue = 10 };
            Assert.That(ConditionEvaluator.Evaluate(condition, state), Is.False, "Tanınmayan operasyon koşulu açmamalı.");
        }

        [Test]
        public void ConditionEvaluator_RejectsRemovedStatType()
        {
            GameState state = new GameState();
            ConditionData condition = new ConditionData { type = "stat", key = "resilience", op = "atleast", intValue = 1 };
            Assert.That(ConditionEvaluator.Evaluate(condition, state), Is.False, "Kaldırılan stat türü koşul açmamalı.");
        }

        private static void AssertReports(StoryDatabase story, string expectedFragment)
        {
            List<string> issues = StoryGraphValidator.Validate(story);
            bool found = issues.Exists(issue => issue.Contains(expectedFragment));
            Assert.That(found, Is.True,
                "'" + expectedFragment + "' bildirilmedi. Bulunan sorunlar:\n" + string.Join("\n", issues.ToArray()));
        }

        /// <summary>
        /// Tek karar düğümü ve tek finalden oluşan asgari bir grafik kurar. Doğrulayıcı bu grafik
        /// için başka sorunlar da bildirir (final sayısı gibi); testler yalnız ilgilendikleri
        /// sorunun bildirildiğini kontrol eder.
        /// </summary>
        private static StoryDatabase BuildStory(EffectData[] effects, ConditionData[] conditions)
        {
            StoryNode start = new StoryNode
            {
                id = "baslangic",
                act = "Test",
                imageKey = "test_gorsel",
                body = "Test gövdesi.",
                choices = new[]
                {
                    new ChoiceData
                    {
                        id = "sol",
                        text = "Sol seçenek",
                        nextNodeId = "son",
                        effects = effects ?? Array.Empty<EffectData>(),
                        conditions = conditions ?? Array.Empty<ConditionData>()
                    },
                    new ChoiceData { id = "sag", text = "Sağ seçenek", nextNodeId = "son" }
                }
            };
            StoryNode ending = new StoryNode
            {
                id = "son",
                act = "Test",
                ending = new EndingData
                {
                    id = "son_test",
                    title = "Test finali",
                    paragraphs = new[] { "Paragraf." },
                    traceFallbacks = new[] { "İz." }
                }
            };
            return new StoryDatabase
            {
                storyId = "test",
                locale = "tr-TR",
                startNodeId = "baslangic",
                nodes = new[] { start, ending }
            };
        }
    }
}
