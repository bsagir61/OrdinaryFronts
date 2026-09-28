using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OrdinaryFronts
{
    /// <summary>
    /// Tanıklık (1.3): bölümün son kararından sonra, final kaydından önce, kırk yıl sonraki
    /// bir masada biri başkaraktere o günleri sorar. Oyuncu ne anlatılacağını seçer:
    /// yaptıklarını ya da yapmadıklarını.
    /// <para>
    /// Bu bir karar değildir; tarihte ve rotada hiçbir şeyi değiştirmez, hiçbir bayrak
    /// üretmez. Yalnız neyin aktarıldığını seçer. İki kip de doğrudur ve ikisi de eksiktir:
    /// kayıt ekranı (final) ardından gelir ve tanıklıkla kayıt arasındaki boşluk oyuncuya
    /// kalır.
    /// </para>
    /// </summary>
    public sealed partial class AppController
    {
        private TMP_Text testimonyKickerText;
        private TMP_Text testimonySettingText;
        private TMP_Text testimonyQuestionText;
        private TMP_Text testimonyBodyText;
        private TMP_Text testimonyHintText;
        private readonly TMP_Text[] testimonyKeyLabels = new TMP_Text[2];
        private readonly TMP_Text[] testimonyChoiceLabels = new TMP_Text[2];
        private GameObject testimonyAskGroupObject;
        private GameObject testimonyTellGroupObject;
        private CanvasGroup testimonyAskGroup;
        private CanvasGroup testimonyTellGroup;
        private Button testimonyContinueButton;
        private StoryNode testimonyEndingNode;
        private bool testimonyAsking;
        private float testimonyArmTime;
        private Coroutine testimonyRoutine;

        private void BuildTestimony(Transform parent)
        {
            GameObject screen = CreateScreen("Testimony", parent);
            router.Register(AppScreen.Testimony, screen);
            AddImage(CreateRect("Veil", screen.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero),
                new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.90f)).raycastTarget = false;
            AddImage(CreateRect("Vignette", screen.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), Color.white, theme.vignette).raycastTarget = false;

            testimonyKickerText = (TMP_Text)AsDocument(CreateText("Kicker", screen.transform, string.Empty, 22f, FontStyles.Bold, theme.mustard,
                new Vector2(0.1f, 0.845f), new Vector2(0.9f, 0.895f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center));
            testimonyKickerText.characterSpacing = 10f;
            AddImage(CreateRect("Rule", screen.transform, new Vector2(0.5f, 0.83f), new Vector2(0.5f, 0.83f), new Vector2(-70f, -2f), new Vector2(70f, 2f)),
                theme.rust).raycastTarget = false;

            testimonyAskGroupObject = CreateRect("Ask", screen.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            testimonyAskGroup = testimonyAskGroupObject.AddComponent<CanvasGroup>();
            Color softPaper = new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.84f);
            testimonySettingText = CreateText("Setting", testimonyAskGroupObject.transform, string.Empty, 26f, FontStyles.Normal, softPaper,
                new Vector2(0.19f, 0.55f), new Vector2(0.81f, 0.80f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            testimonySettingText.enableAutoSizing = true;
            testimonySettingText.fontSizeMin = 18f;
            testimonySettingText.fontSizeMax = 26f;
            testimonySettingText.lineSpacing = 6f;
            testimonyQuestionText = CreateText("Question", testimonyAskGroupObject.transform, string.Empty, 44f, FontStyles.Bold | FontStyles.Italic, theme.agedPaper,
                new Vector2(0.1f, 0.40f), new Vector2(0.9f, 0.53f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            testimonyQuestionText.enableAutoSizing = true;
            testimonyQuestionText.fontSizeMin = 30f;
            testimonyQuestionText.fontSizeMax = 44f;

            CreateChoiceButton("Tell Done", testimonyAskGroupObject.transform, InputGlyphs.Left(inputDevice, null), () => ChooseTestimony(true),
                new Vector2(0.16f, 0.16f), new Vector2(0.49f, 0.33f), out testimonyKeyLabels[0], out testimonyChoiceLabels[0]);
            CreateChoiceButton("Tell Undone", testimonyAskGroupObject.transform, InputGlyphs.Right(inputDevice, null), () => ChooseTestimony(false),
                new Vector2(0.51f, 0.16f), new Vector2(0.84f, 0.33f), out testimonyKeyLabels[1], out testimonyChoiceLabels[1]);
            testimonyHintText = (TMP_Text)AsDocument(CreateText("Hint", testimonyAskGroupObject.transform, string.Empty, 18f, FontStyles.Normal,
                new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.55f),
                new Vector2(0.55f, 0.035f), new Vector2(0.97f, 0.085f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Right));

            testimonyTellGroupObject = CreateRect("Tell", screen.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            testimonyTellGroup = testimonyTellGroupObject.AddComponent<CanvasGroup>();
            testimonyBodyText = CreateText("Testimony", testimonyTellGroupObject.transform, string.Empty, 29f, FontStyles.Normal, theme.agedPaper,
                new Vector2(0.17f, 0.20f), new Vector2(0.83f, 0.80f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            testimonyBodyText.enableAutoSizing = true;
            testimonyBodyText.fontSizeMin = 19f;
            testimonyBodyText.fontSizeMax = 29f;
            testimonyBodyText.lineSpacing = 8f;
            testimonyContinueButton = CreateButton("See Record", testimonyTellGroupObject.transform, T(UiKey.TestimonyContinue), FinishTestimony,
                new Vector2(0.38f, 0.065f), new Vector2(0.62f, 0.145f));
            testimonyTellGroupObject.SetActive(false);
        }

        /// <summary>Final düğümüne varıldı: tanıklık bekliyorsa önce o, yoksa doğrudan kayıt.</summary>
        private void ShowEnding(StoryNode node)
        {
            if (storyController != null && storyController.TestimonyPending && testimonyAskGroupObject != null) ShowTestimony(node);
            else ShowEndingScreen(node);
        }

        private void ShowTestimony(StoryNode node)
        {
            TestimonyData testimony = storyController.Story.testimony;
            testimonyEndingNode = node;
            SetBackground(node.imageKey);
            audioManager.PlayAmbienceFor(node.imageKey);
            StopBodyReveal();
            if (testimonyRoutine != null) StopCoroutine(testimonyRoutine);
            testimonyRoutine = null;

            testimonyKickerText.text = localization == null ? testimony.kicker : localization.ToUpper(testimony.kicker);
            testimonySettingText.text = testimony.setting;
            testimonyQuestionText.text = testimony.question;
            testimonyChoiceLabels[0].text = testimony.tellDone;
            testimonyChoiceLabels[1].text = testimony.tellUndone;
            RefreshTestimonyGlyphs();

            testimonyAskGroupObject.SetActive(true);
            testimonyAskGroup.alpha = 1f;
            testimonyAskGroup.interactable = true;
            testimonyTellGroupObject.SetActive(false);
            testimonyAsking = true;
            testimonyArmTime = Time.unscaledTime + 0.6f;
            router.Show(AppScreen.Testimony);
            if (inputDevice == InputDevice.Gamepad) SelectFirstButton(router.Get(AppScreen.Testimony));
            if (MotionAllowed && testimonyRoutine == null) testimonyRoutine = StartCoroutine(FadeGroup(testimonyAskGroup, 0f, 1f, 0.8f, null));
        }

        /// <summary>Oynanış tuşlarıyla aynı dil: sol "yaptıklarım", sağ "yapmadıklarım".</summary>
        private void UpdateTestimonyInput()
        {
            if (!testimonyAsking || Time.unscaledTime < testimonyArmTime) return;
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) || PadLeftDown) ChooseTestimony(true);
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || PadRightDown) ChooseTestimony(false);
        }

        private void ChooseTestimony(bool done)
        {
            if (!testimonyAsking || storyController == null || Time.unscaledTime < testimonyArmTime) return;
            testimonyAsking = false;
            audioManager.PlayConfirm();
            storyController.RecordTestimony(done);
            testimonyBodyText.text = ComposeTestimony(done);
            if (testimonyRoutine != null) StopCoroutine(testimonyRoutine);
            testimonyRoutine = StartCoroutine(RevealTestimony());
        }

        private string ComposeTestimony(bool done)
        {
            TestimonyData testimony = storyController.Story.testimony;
            List<string> lines = storyController.BuildTestimony(done);
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < lines.Count; i++)
                builder.Append('“').Append(lines[i]).Append('”').Append("\n\n");
            string close = done ? testimony.closeDone : testimony.closeUndone;
            // Kapanış, kipin sesi değil anlatıcınınkidir: eğik ve biraz soluk.
            string soft = ColorUtility.ToHtmlStringRGB(theme.agedPaper) + "B4";
            builder.Append("<i><color=#").Append(soft).Append('>').Append(close.Trim()).Append("</color></i>");
            return builder.ToString();
        }

        private IEnumerator RevealTestimony()
        {
            bool motion = MotionAllowed;
            testimonyAskGroup.interactable = false;
            if (motion) yield return FadeGroup(testimonyAskGroup, 1f, 0f, 0.4f, null);
            testimonyAskGroupObject.SetActive(false);
            testimonyTellGroupObject.SetActive(true);
            testimonyTellGroup.interactable = false;
            if (audioManager != null) audioManager.PlayActTone();
            if (motion) yield return FadeGroup(testimonyTellGroup, 0f, 1f, 1.1f, null);
            testimonyTellGroup.alpha = 1f;
            testimonyTellGroup.interactable = true;
            if (testimonyContinueButton != null && UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(testimonyContinueButton.gameObject);
            testimonyRoutine = null;
        }

        private static IEnumerator FadeGroup(CanvasGroup group, float from, float to, float seconds, System.Action done)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
                yield return null;
            }
            group.alpha = to;
            if (done != null) done();
        }

        /// <summary>Kayda geç. Tanıklık verilmeden geçildiyse kayıttan devam edildiğinde yeniden sorulur.</summary>
        private void FinishTestimony()
        {
            if (router.Current != AppScreen.Testimony) return;
            if (testimonyRoutine != null) StopCoroutine(testimonyRoutine);
            testimonyRoutine = null;
            testimonyAsking = false;
            audioManager.PlayConfirm();
            StoryNode node = testimonyEndingNode ?? (storyController == null ? null : storyController.CurrentNode);
            if (node != null && node.IsEnding) ShowEndingScreen(node);
            else ShowMainMenu(true);
        }

        private void RefreshTestimonyGlyphs()
        {
            if (testimonyKeyLabels[0] != null) testimonyKeyLabels[0].text = InputGlyphs.Left(inputDevice, null);
            if (testimonyKeyLabels[1] != null) testimonyKeyLabels[1].text = InputGlyphs.Right(inputDevice, null);
            if (testimonyHintText != null) testimonyHintText.text = TG(UiKey.TestimonySkipHint);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Görsel QA: tanıklığı hareketsiz ver ve metni yerleştir.</summary>
        private void ChooseTestimonyForQa(bool done)
        {
            testimonyArmTime = 0f;
            ChooseTestimony(done);
            if (testimonyRoutine != null) StopCoroutine(testimonyRoutine);
            testimonyRoutine = null;
            testimonyAskGroupObject.SetActive(false);
            testimonyTellGroupObject.SetActive(true);
            testimonyTellGroup.alpha = 1f;
            testimonyTellGroup.interactable = true;
        }
#endif
    }
}
