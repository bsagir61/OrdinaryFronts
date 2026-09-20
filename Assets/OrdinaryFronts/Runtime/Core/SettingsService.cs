using System;
using System.IO;
using UnityEngine;

namespace OrdinaryFronts
{
    [Serializable]
    public sealed class SettingsData
    {
        // Sürüm 2: dil tercihi eklendi. Sürüm 1 ayarları varsayılanlara döner, yani mevcut
        // kurulumlar da yeni kurulumlar gibi İngilizce başlar.
        public int schemaVersion = 2;
        public string locale = LocalizationService.DefaultLocale;
        public float masterVolume = 0.82f;
        public float ambientVolume = 0.56f;
        public float effectsVolume = 0.68f;
        public bool fullscreen;
        public bool largeText;
        public bool reduceMotion;

        public void Clamp()
        {
            masterVolume = Mathf.Clamp01(masterVolume);
            ambientVolume = Mathf.Clamp01(ambientVolume);
            effectsVolume = Mathf.Clamp01(effectsVolume);
            locale = LocalizationService.Normalize(locale);
        }
    }

    public sealed class SettingsService
    {
        public const string SettingsFileName = "settings.json";
        private readonly string directory;
        public string SettingsPath { get { return Path.Combine(directory, SettingsFileName); } }

        public SettingsService(string customDirectory = null)
        {
            directory = customDirectory ?? Application.persistentDataPath;
        }

        public SettingsData Load()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return new SettingsData { fullscreen = Screen.fullScreen };
                SettingsData data = JsonUtility.FromJson<SettingsData>(File.ReadAllText(SettingsPath));
                if (data == null || data.schemaVersion != 2) return new SettingsData { fullscreen = Screen.fullScreen };
                data.Clamp();
                return data;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                return new SettingsData { fullscreen = Screen.fullScreen };
            }
        }

        public void Save(SettingsData data)
        {
            if (data == null) return;
            data.Clamp();
            AtomicJsonFile.Write(SettingsPath, JsonUtility.ToJson(data, true));
        }
    }
}
