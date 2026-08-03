using System;
using System.IO;
using UnityEngine;

namespace OrdinaryFronts
{
    [Serializable]
    public sealed class SettingsData
    {
        public int schemaVersion = 1;
        public float masterVolume = 0.82f;
        public float ambientVolume = 0.56f;
        public float effectsVolume = 0.68f;
        public bool fullscreen;
        public bool largeText;
        public bool reduceMotion;
        public bool contentNoteSeen;

        public void Clamp()
        {
            masterVolume = Mathf.Clamp01(masterVolume);
            ambientVolume = Mathf.Clamp01(ambientVolume);
            effectsVolume = Mathf.Clamp01(effectsVolume);
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
                if (data == null || data.schemaVersion != 1) return new SettingsData { fullscreen = Screen.fullScreen };
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
