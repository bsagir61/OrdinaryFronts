using System;
using System.IO;
using UnityEngine;

namespace OrdinaryFronts
{
    public sealed class SaveService
    {
        public const int CurrentSchemaVersion = 1;
        public const string SaveFileName = "hamburg-demo-save.json";

        private readonly string directory;
        public string SavePath { get { return Path.Combine(directory, SaveFileName); } }
        public bool HasSave { get { return File.Exists(SavePath); } }

        public SaveService(string customDirectory = null)
        {
            directory = customDirectory ?? Application.persistentDataPath;
        }

        public void Save(GameState state)
        {
            if (state == null) throw new ArgumentNullException("state");
            state.schemaVersion = CurrentSchemaVersion;
            state.stats.Clamp();
            string json = JsonUtility.ToJson(state, true);
            AtomicJsonFile.Write(SavePath, json);
        }

        public bool TryLoad(out GameState state, out string userMessage)
        {
            state = null;
            userMessage = string.Empty;
            if (!HasSave)
            {
                userMessage = "Henüz devam edilebilecek bir kayıt yok.";
                return false;
            }
            try
            {
                string json = File.ReadAllText(SavePath);
                state = JsonUtility.FromJson<GameState>(json);
                if (state == null || state.schemaVersion != CurrentSchemaVersion || string.IsNullOrWhiteSpace(state.currentNodeId))
                    throw new InvalidDataException("Kayıt şeması veya aktif düğüm geçersiz.");
                if (state.stats == null) throw new InvalidDataException("Kayıt durum değerlerini içermiyor.");
                state.stats.Clamp();
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is InvalidDataException)
            {
                state = null;
                userMessage = "Kayıt dosyası okunamadı. Dosyanız korunuyor; yeni bir oyun başlatabilirsiniz.";
                return false;
            }
        }
    }
}
