using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OrdinaryFronts
{
    /// <summary>
    /// Bölüm kayıtları (1.6): her bölüm kendi kaydını tutar. Ana menüdeki "Devam Et" en son
    /// oynanan bölümü açar ve adını taşır; harita kartı, yarım kalmış bir bölüm için
    /// "Kaldığın yerden devam et" düğmesini gösterir ve baştan başlatmayı ikinci sıraya alır.
    /// </summary>
    public sealed partial class AppController
    {
        private Button storySelectContinue;

        /// <summary>Bölüm yüklü değilse onu yükler. Başarısızsa hata ekranı gösterir ve false döner.</summary>
        private bool SwitchToStory(string storyId)
        {
            string requested = StoryRepository.SanitizeStoryId(storyId);
            if (requested == activeStoryId && storyController != null && storyController.Story != null) return true;
            string previous = activeStoryId;
            try
            {
                activeStoryId = requested;
                LoadStoryForLocale(settings.locale);
                return true;
            }
            catch (Exception exception)
            {
                activeStoryId = previous;
                Debug.LogError(exception);
                ShowError(T(UiKey.ErrorStoryUnavailable) + "\n\n" + exception.Message);
                return false;
            }
        }

        /// <summary>Ana menü: en son oynanan bölüme devam.</summary>
        private void ContinueGame()
        {
            if (storyController == null) return;
            string recent = saveService.MostRecentStoryId();
            if (recent != null && !SwitchToStory(recent)) return;
            ContinueLoadedStory();
        }

        /// <summary>Harita kartı: seçili bölüme kaldığı yerden devam.</summary>
        private void ContinueSelectedStory()
        {
            if (selectedStory == null || storyController == null) return;
            if (!SwitchToStory(selectedStory.storyId)) return;
            ContinueLoadedStory();
        }

        private void ContinueLoadedStory()
        {
            string messageKey;
            if (!storyController.TryContinue(out messageKey))
            {
                ShowError(T(messageKey));
                return;
            }
            audioManager.PlayConfirm();
            RenderNode(storyController.CurrentNode, true);
        }

        /// <summary>Kaydı olan ve bitmemiş bölüm: kaldığı yerden devam edilebilir.</summary>
        private bool HasUnfinishedSave(string storyId)
        {
            if (saveService == null || string.IsNullOrWhiteSpace(storyId) || !saveService.HasSaveFor(storyId)) return false;
            GameState state = saveService.Peek(storyId);
            return state != null && !state.completed;
        }

        private string CatalogTitle(string storyId)
        {
            if (catalog == null || catalog.entries == null) return null;
            for (int i = 0; i < catalog.entries.Length; i++)
                if (catalog.entries[i] != null && catalog.entries[i].storyId == storyId) return catalog.entries[i].title;
            return null;
        }

        /// <summary>"Devam Et · Karelya": oyuncu hangi bölüme döneceğini bilerek basar.</summary>
        private void RefreshContinueLabel()
        {
            if (continueButton == null) return;
            TMP_Text label = continueButton.GetComponentInChildren<TMP_Text>();
            if (label == null) return;
            string recent = saveService == null ? null : saveService.MostRecentStoryId();
            string title = recent == null ? null : CatalogTitle(recent);
            label.text = string.IsNullOrEmpty(title) ? T(UiKey.MenuContinue) : T(UiKey.MenuContinue) + "  ·  " + title;
        }

        /// <summary>Harita kartının düğmeleri: yarım kalmış bölümde devam önde, baştan başlatma arkada.</summary>
        private void RefreshStorySelectButtons()
        {
            if (storySelectBegin == null) return;
            bool has = selectedStory != null;
            bool resume = has && HasUnfinishedSave(selectedStory.storyId);
            TMP_Text beginLabel = storySelectBegin.GetComponentInChildren<TMP_Text>();
            if (beginLabel != null) beginLabel.text = resume ? T(UiKey.StorySelectRestart) : T(UiKey.StorySelectBegin);
            if (storySelectContinue != null)
            {
                storySelectContinue.gameObject.SetActive(resume);
                storySelectContinue.interactable = resume;
            }
        }
    }
}
