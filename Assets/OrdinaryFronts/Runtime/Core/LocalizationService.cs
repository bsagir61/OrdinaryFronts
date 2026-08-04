using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OrdinaryFronts
{
    /// <summary>
    /// Arayüz metinlerinde kullanılan anahtarlar. Sabit olarak tutulurlar ki koddaki her
    /// kullanım derleme zamanında denetlensin; testler bu listeyi gezip her dil dosyasında
    /// karşılığının bulunduğunu doğrular.
    /// </summary>
    public static class UiKey
    {
        public const string MenuNewGame = "menu.newGame";
        public const string MenuContinue = "menu.continue";
        public const string MenuSettings = "menu.settings";
        public const string MenuCredits = "menu.credits";
        public const string MenuExit = "menu.exit";
        public const string MenuContext = "menu.context";
        public const string MenuInputHint = "menu.inputHint";

        public const string GameplayPauseHint = "gameplay.pauseHint";
        public const string GameplayNoChoice = "gameplay.noChoice";
        public const string EchoPrefix = "gameplay.echoPrefix";

        public const string IntroSkipHint = "intro.skipHint";

        public const string SettingsTitle = "settings.title";
        public const string SettingsMasterVolume = "settings.masterVolume";
        public const string SettingsAmbientVolume = "settings.ambientVolume";
        public const string SettingsEffectsVolume = "settings.effectsVolume";
        public const string SettingsFullscreen = "settings.fullscreen";
        public const string SettingsTextSize = "settings.textSize";
        public const string SettingsReduceMotion = "settings.reduceMotion";
        public const string SettingsLanguage = "settings.language";
        public const string SettingsBack = "settings.back";
        public const string SettingsTextSizeNormal = "settings.textSizeNormal";
        public const string SettingsTextSizeLarge = "settings.textSizeLarge";

        public const string CommonOn = "common.on";
        public const string CommonOff = "common.off";
        public const string CommonMainMenu = "common.mainMenu";

        public const string CreditsBody = "credits.body";

        public const string ContentNoteTitle = "contentNote.title";
        public const string ContentNoteBody = "contentNote.body";
        public const string ContentNoteContinue = "contentNote.continue";

        public const string PauseTitle = "pause.title";
        public const string PauseResume = "pause.resume";

        public const string EndingTracesTitle = "ending.tracesTitle";
        public const string EndingTracesFallback = "ending.tracesFallback";
        public const string EndingReplay = "ending.replay";

        public const string ErrorTitle = "error.title";
        public const string ErrorGeneric = "error.generic";
        public const string ErrorStoryUnavailable = "error.storyUnavailable";
        public const string ErrorInitFailed = "error.initFailed";
        public const string ErrorNewGameFailed = "error.newGameFailed";
        public const string ErrorChoiceFailed = "error.choiceFailed";
        public const string ErrorStoryValidation = "error.storyValidation";

        public const string SaveNone = "save.none";
        public const string SaveUnreadable = "save.unreadable";
        public const string SaveIncompatible = "save.incompatible";

        /// <summary>Her dil dosyasında bulunması gereken anahtarların tamamı.</summary>
        public static string[] All()
        {
            return new[]
            {
                MenuNewGame, MenuContinue, MenuSettings, MenuCredits, MenuExit, MenuContext, MenuInputHint,
                GameplayPauseHint, GameplayNoChoice, EchoPrefix,
                IntroSkipHint,
                SettingsTitle, SettingsMasterVolume, SettingsAmbientVolume, SettingsEffectsVolume,
                SettingsFullscreen, SettingsTextSize, SettingsReduceMotion, SettingsLanguage, SettingsBack,
                SettingsTextSizeNormal, SettingsTextSizeLarge,
                CommonOn, CommonOff, CommonMainMenu,
                CreditsBody,
                ContentNoteTitle, ContentNoteBody, ContentNoteContinue,
                PauseTitle, PauseResume,
                EndingTracesTitle, EndingTracesFallback, EndingReplay,
                ErrorTitle, ErrorGeneric, ErrorStoryUnavailable, ErrorInitFailed, ErrorNewGameFailed,
                ErrorChoiceFailed, ErrorStoryValidation,
                SaveNone, SaveUnreadable, SaveIncompatible
            };
        }
    }

    [Serializable]
    public sealed class UiStringEntry
    {
        public string key;
        public string value;
    }

    [Serializable]
    public sealed class UiStringTable
    {
        public string locale;
        public UiStringEntry[] entries = Array.Empty<UiStringEntry>();
    }

    /// <summary>
    /// Arayüz metinlerini <c>StreamingAssets/Localization/&lt;locale&gt;.json</c> dosyalarından
    /// okur. Yeni bir dil eklemek için kod değişikliği gerekmez: bir arayüz tablosu ve bir
    /// hikâye dosyası eklemek yeterlidir.
    /// </summary>
    public sealed class LocalizationService
    {
        public const string DefaultLocale = "en-US";
        public const string RelativeFolder = "Localization";

        /// <summary>Ayarlar ekranında sunulan diller; sıralama listedeki sıradır.</summary>
        public static readonly string[] SupportedLocales = { "en-US", "tr-TR" };

        private readonly Dictionary<string, string> entries = new Dictionary<string, string>();
        private readonly string rootOverride;

        public string Locale { get; private set; }

        public LocalizationService(string streamingAssetsRootOverride = null)
        {
            rootOverride = streamingAssetsRootOverride;
        }

        public static bool IsSupported(string locale)
        {
            if (string.IsNullOrWhiteSpace(locale)) return false;
            for (int i = 0; i < SupportedLocales.Length; i++)
                if (string.Equals(SupportedLocales[i], locale, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static string Normalize(string locale)
        {
            for (int i = 0; i < SupportedLocales.Length; i++)
                if (string.Equals(SupportedLocales[i], locale, StringComparison.OrdinalIgnoreCase)) return SupportedLocales[i];
            return DefaultLocale;
        }

        /// <summary>
        /// Dilin kendi dilindeki adı. Ayarlarda her zaman böyle gösterilir ki oyuncu
        /// anlamadığı bir dilde açtığında da seçeneği tanıyabilsin.
        /// </summary>
        public static string DisplayName(string locale)
        {
            switch (Normalize(locale))
            {
                case "tr-TR": return "Türkçe";
                default: return "English";
            }
        }

        /// <summary>Sıradaki dile geçer; ayarlardaki tek düğmeli seçim bunu kullanır.</summary>
        public static string NextLocale(string current)
        {
            string normalized = Normalize(current);
            for (int i = 0; i < SupportedLocales.Length; i++)
                if (SupportedLocales[i] == normalized) return SupportedLocales[(i + 1) % SupportedLocales.Length];
            return DefaultLocale;
        }

        public static string PathFor(string locale, string streamingAssetsRootOverride = null)
        {
            string root = streamingAssetsRootOverride ?? Application.streamingAssetsPath;
            return Path.Combine(root, RelativeFolder, Normalize(locale) + ".json");
        }

        public void Load(string locale)
        {
            string normalized = Normalize(locale);
            string path = PathFor(normalized, rootOverride);
            entries.Clear();
            Locale = normalized;

            if (!File.Exists(path)) throw new FileNotFoundException("Arayüz dil dosyası bulunamadı.", path);
            UiStringTable table = JsonUtility.FromJson<UiStringTable>(File.ReadAllText(path));
            if (table == null || table.entries == null || table.entries.Length == 0)
                throw new InvalidDataException("Arayüz dil dosyası okunamadı veya boş: " + path);

            for (int i = 0; i < table.entries.Length; i++)
            {
                UiStringEntry entry = table.entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.key)) continue;
                entries[entry.key] = entry.value ?? string.Empty;
            }
        }

        /// <summary>
        /// Eksik anahtarda oyunu metinsiz bırakmamak için anahtarın kendisi döner ve hata
        /// bırakılır; testler zaten her anahtarın her dilde bulunmasını zorunlu kılar.
        /// </summary>
        public string Get(string key)
        {
            string value;
            if (entries.TryGetValue(key, out value)) return value;
            Debug.LogError("Eksik arayüz metni: " + key + " (" + Locale + ")");
            return key;
        }

        public bool Has(string key)
        {
            return entries.ContainsKey(key);
        }
    }
}
