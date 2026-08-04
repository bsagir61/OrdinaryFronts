using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OrdinaryFronts
{
    [Serializable]
    public sealed class SceneArtEntry
    {
        public string key;
        public Sprite sprite;
    }

    public sealed class AppController : MonoBehaviour
    {
        public static string TestSaveDirectoryOverride;

        [Header("Scene references")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private BrandConfig brand;
        [SerializeField] private ThemeConfig theme;
        [SerializeField] private SceneArtEntry[] sceneArt = Array.Empty<SceneArtEntry>();

        private readonly ScreenRouter router = new ScreenRouter();
        private readonly Dictionary<string, Sprite> artIndex = new Dictionary<string, Sprite>();
        private readonly List<TMP_Text> scalableBodyTexts = new List<TMP_Text>();

        private SaveService saveService;
        private SettingsService settingsService;
        private SettingsData settings;
        private StoryController storyController;

        private Image backgroundArt;
        private RectTransform backgroundRect;
        private CanvasGroup storyCardGroup;
        private RectTransform storyCardRect;
        private TMP_Text chapterText;
        private TMP_Text dateLocationText;
        private TMP_Text storyBodyText;
        private TMP_Text echoText;
        private TMP_Text endingTitleText;
        private TMP_Text endingBodyText;
        private TMP_Text endingTracesText;
        private TMP_Text errorText;
        private TMP_Text fullscreenValueText;
        private TMP_Text textSizeValueText;
        private TMP_Text motionValueText;
        private Button continueButton;
        private readonly Button[] choiceButtons = new Button[2];
        private readonly TMP_Text[] choiceLabels = new TMP_Text[2];
        private readonly TMP_Text[] choiceKeyLabels = new TMP_Text[2];
        private RectTransform storyBodyRect;
        private Slider masterSlider;
        private Slider ambientSlider;
        private Slider effectsSlider;
        private AppScreen settingsReturnScreen = AppScreen.MainMenu;
        private bool transitionBusy;
        private bool commandLineSmoke;
        private string commandLineCaptureDirectory;
        private Coroutine cardRoutine;
        private GameObject firstSelection;
        private string initializationError;

        private Image introBackdropA;
        private Image introBackdropB;
        private RectTransform introBackdropARect;
        private RectTransform introBackdropBRect;
        private Image introGrainImage;
        private CanvasGroup introTextGroup;
        private TMP_Text introKickerText;
        private TMP_Text introLineText;
        private RectTransform introLetterboxTop;
        private RectTransform introLetterboxBottom;
        private Coroutine introRoutine;
        private bool introActive;
        private bool introSkipRequested;
        private float introSkipArmTime;
        private float introGrainTimer;
        private int introGrainFrame;

        public bool IntroPlaying { get { return introActive; } }

        public AppScreen CurrentScreen { get { return router.Current; } }
        public string CurrentNodeId { get { return storyController != null && storyController.State != null ? storyController.State.currentNodeId : null; } }
        public string ActiveSavePath { get { return saveService == null ? null : saveService.SavePath; } }

        public void Configure(Canvas canvas, AudioManager audio, BrandConfig brandConfig, ThemeConfig themeConfig, SceneArtEntry[] art)
        {
            rootCanvas = canvas;
            audioManager = audio;
            brand = brandConfig;
            theme = themeConfig;
            sceneArt = art ?? Array.Empty<SceneArtEntry>();
        }

        private void Awake()
        {
            ReadCommandLineQaOptions();
            if (commandLineSmoke) Application.runInBackground = true;
            EnsureDependencies();
            IndexArt();
            string storage = string.IsNullOrWhiteSpace(TestSaveDirectoryOverride) ? null : TestSaveDirectoryOverride;
            saveService = new SaveService(storage);
            settingsService = new SettingsService(storage);
            settings = settingsService.Load();
            ApplySettings(false);

            try
            {
                StoryRepository repository = new StoryRepository();
                repository.Load();
                List<string> issues = StoryGraphValidator.Validate(repository.Database);
                if (issues.Count > 0) throw new InvalidDataException("Hikâye doğrulaması başarısız: " + string.Join(" | ", issues.ToArray()));
                storyController = new StoryController(repository, saveService);
                storyController.Initialize();
            }
            catch (Exception exception)
            {
                initializationError = "Demo verileri hazırlanamadı.\n\n" + exception.Message;
                Debug.LogError(exception);
            }

            BuildInterface();
            // Metin ölçeği yalnız BuildInterface sonrasında uygulanabilir; Awake başındaki
            // ApplySettings çağrısı henüz hiç metin oluşmadığı için erken dönüyor.
            ApplyTextScale();
            ShowMainMenu(false);
            if (!string.IsNullOrEmpty(initializationError)) ShowError(initializationError);
            else if (commandLineSmoke) StartCoroutine(RunCommandLineSmoke());
        }

        private void Update()
        {
            UpdateParallax();
            if (introActive)
            {
                UpdateIntroGrain();
                // Açılış sırasında tek etkileşim atlamaktır; Escape de buraya düşer.
                if (Time.unscaledTime >= introSkipArmTime && Input.anyKeyDown) introSkipRequested = true;
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape)) HandleEscape();
            if (transitionBusy || router.Current != AppScreen.Gameplay) return;
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) SelectChoice(0);
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) SelectChoice(1);
        }

        private void EnsureDependencies()
        {
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<ThemeConfig>();
                theme.ApplyCanonicalDefaults();
            }
            if (brand == null)
            {
                brand = ScriptableObject.CreateInstance<BrandConfig>();
                brand.ApplyCanonicalDefaults();
            }
            if (rootCanvas == null)
            {
                GameObject canvasObject = new GameObject("Interface Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                rootCanvas = canvasObject.GetComponent<Canvas>();
                rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }
            if (audioManager == null)
            {
                GameObject audioObject = new GameObject("Audio Manager");
                audioManager = audioObject.AddComponent<AudioManager>();
            }
        }

        private void IndexArt()
        {
            artIndex.Clear();
            if (sceneArt == null) return;
            for (int i = 0; i < sceneArt.Length; i++)
            {
                SceneArtEntry entry = sceneArt[i];
                if (entry != null && !string.IsNullOrEmpty(entry.key) && entry.sprite != null) artIndex[entry.key] = entry.sprite;
            }
        }

        private void BuildInterface()
        {
            Transform previous = rootCanvas.transform.Find("Runtime Interface");
            if (previous != null) Destroy(previous.gameObject);
            GameObject runtimeRoot = CreateRect("Runtime Interface", rootCanvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject background = CreateRect("Background Illustration", runtimeRoot.transform, Vector2.zero, Vector2.one, new Vector2(-28f, -28f), new Vector2(28f, 28f));
            backgroundArt = background.AddComponent<Image>();
            backgroundArt.color = Color.white;
            backgroundArt.raycastTarget = false;
            backgroundRect = background.GetComponent<RectTransform>();
            AspectRatioFitter backgroundFitter = background.AddComponent<AspectRatioFitter>();
            backgroundFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            backgroundFitter.aspectRatio = 16f / 9f;

            Image wash = AddImage(CreateRect("Ink Wash", runtimeRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.34f));
            wash.raycastTarget = false;
            Image vignette = AddImage(CreateRect("Vignette", runtimeRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), Color.white, theme.vignette);
            vignette.raycastTarget = false;

            BuildMainMenu(runtimeRoot.transform);
            BuildIntro(runtimeRoot.transform);
            BuildGameplay(runtimeRoot.transform);
            BuildSettings(runtimeRoot.transform);
            BuildCredits(runtimeRoot.transform);
            BuildContentNote(runtimeRoot.transform);
            BuildPause(runtimeRoot.transform);
            BuildEnding(runtimeRoot.transform);
            BuildError(runtimeRoot.transform);
        }

        private void BuildMainMenu(Transform parent)
        {
            GameObject screen = CreateScreen("Main Menu", parent);
            router.Register(AppScreen.MainMenu, screen);
            AddImage(CreateRect("Archive Block", screen.transform, new Vector2(0.055f, 0.09f), new Vector2(0.42f, 0.92f), Vector2.zero, Vector2.zero), new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.88f));
            AddImage(CreateRect("Rust Rule", screen.transform, new Vector2(0.055f, 0.895f), new Vector2(0.42f, 0.905f), Vector2.zero, Vector2.zero), theme.rust);
            TMP_Text title = CreateText("Product Title", screen.transform, brand.ProductName, 74f, FontStyles.Bold, theme.agedPaper,
                new Vector2(0.08f, 0.68f), new Vector2(0.385f, 0.87f), Vector2.zero, Vector2.zero, TextAlignmentOptions.BottomLeft);
            title.enableAutoSizing = true;
            title.fontSizeMin = 54f;
            title.fontSizeMax = 82f;
            TMP_Text context = CreateText("Context", screen.transform, "HAMBURG · TEMMUZ 1943", 21f, FontStyles.Normal, theme.mustard,
                new Vector2(0.08f, 0.63f), new Vector2(0.385f, 0.675f), Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            context.characterSpacing = 4f;

            Button newButton = CreateButton("New Game", screen.transform, "Yeni Oyun", () => RequestNewGame(),
                new Vector2(0.08f, 0.525f), new Vector2(0.385f, 0.595f));
            continueButton = CreateButton("Continue", screen.transform, "Devam Et", ContinueGame,
                new Vector2(0.08f, 0.435f), new Vector2(0.385f, 0.505f));
            CreateButton("Settings", screen.transform, "Ayarlar", () => OpenSettings(AppScreen.MainMenu),
                new Vector2(0.08f, 0.345f), new Vector2(0.385f, 0.415f));
            CreateButton("Credits", screen.transform, "Emeği Geçenler", ShowCredits,
                new Vector2(0.08f, 0.255f), new Vector2(0.385f, 0.325f));
            CreateButton("Exit", screen.transform, "Çıkış", ExitApplication,
                new Vector2(0.08f, 0.165f), new Vector2(0.385f, 0.235f));
            firstSelection = newButton.gameObject;

            TMP_Text hint = CreateText("Input Hint", screen.transform, "Fare veya yön tuşlarıyla gezin · Enter ile onayla", 18f, FontStyles.Normal,
                new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.7f), new Vector2(0.08f, 0.105f), new Vector2(0.385f, 0.145f), Vector2.zero, Vector2.zero, TextAlignmentOptions.BottomLeft);
            hint.enableAutoSizing = true;
            hint.fontSizeMin = 14f;
        }

        /// <summary>
        /// Açılış kurgusu ekranı: iki çapraz geçişli arka plan katmanı, okunabilirlik için sabit
        /// bir is perdesi, çok düşük opaklıkta arşiv greni, sinematik letterbox ve tek satırlık
        /// anlatı bloğu. Ürün adı veya logo bilinçli olarak yoktur (ART_DIRECTION §12).
        /// </summary>
        private void BuildIntro(Transform parent)
        {
            GameObject screen = CreateScreen("Intro", parent);
            router.Register(AppScreen.Intro, screen);

            introBackdropA = CreateIntroBackdrop("Backdrop A", screen.transform, out introBackdropARect);
            introBackdropB = CreateIntroBackdrop("Backdrop B", screen.transform, out introBackdropBRect);

            // İki katmanlı karartma: hafif bir genel perde tonu birleştirir, metin bandındaki
            // yumuşak gradyan ise kontrastı yalnız gerektiği yerde yükseltir. Tek başına güçlü
            // bir genel perde, illüstrasyonun değer katmanlarını düzleştiriyordu.
            AddImage(CreateRect("Scrim", screen.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero),
                new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.55f)).raycastTarget = false;
            AddImage(CreateRect("Text Scrim", screen.transform, new Vector2(0f, 0.10f), new Vector2(1f, 0.72f), Vector2.zero, Vector2.zero),
                Color.white, theme.introTextScrim).raycastTarget = false;

            Sprite firstGrain = theme.introGrain != null && theme.introGrain.Length > 0 ? theme.introGrain[0] : null;
            introGrainImage = AddImage(CreateRect("Grain", screen.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero),
                new Color(1f, 1f, 1f, 0.05f), firstGrain);
            introGrainImage.raycastTarget = false;

            GameObject top = CreateRect("Letterbox Top", screen.transform, new Vector2(0f, 1f), Vector2.one, Vector2.zero, Vector2.zero);
            AddImage(top, theme.sootNavy).raycastTarget = false;
            introLetterboxTop = top.GetComponent<RectTransform>();
            GameObject bottom = CreateRect("Letterbox Bottom", screen.transform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, Vector2.zero);
            AddImage(bottom, theme.sootNavy).raycastTarget = false;
            introLetterboxBottom = bottom.GetComponent<RectTransform>();

            // Blok genişliği, uzun satırların tek satıra sıkışmak yerine iki dengeli satıra
            // bölünmesi için sınırlanır (ART_DIRECTION §9: yaklaşık 55-85 karakter).
            GameObject textBlock = CreateRect("Intro Text", screen.transform, new Vector2(0.205f, 0.235f), new Vector2(0.795f, 0.56f), Vector2.zero, Vector2.zero);
            introTextGroup = textBlock.AddComponent<CanvasGroup>();
            introTextGroup.alpha = 0f;

            introKickerText = CreateText("Kicker", textBlock.transform, string.Empty, 26f, FontStyles.Bold, theme.mustard,
                new Vector2(0f, 0.80f), Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Bottom);
            introKickerText.characterSpacing = 6f;

            AddImage(CreateRect("Kicker Rule", textBlock.transform, new Vector2(0.455f, 0.752f), new Vector2(0.545f, 0.762f), Vector2.zero, Vector2.zero),
                theme.rust).raycastTarget = false;

            introLineText = CreateText("Line", textBlock.transform, string.Empty, 34f, FontStyles.Normal, theme.agedPaper,
                Vector2.zero, new Vector2(1f, 0.70f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Top);
            introLineText.enableAutoSizing = true;
            introLineText.fontSizeMin = 22f;
            introLineText.fontSizeMax = 36f;
            introLineText.lineSpacing = 10f;
            scalableBodyTexts.Add(introLineText);

            CreateText("Skip Hint", screen.transform, "Atlamak için herhangi bir tuşa bas", 17f, FontStyles.Normal,
                new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.62f),
                new Vector2(0.55f, 0.092f), new Vector2(0.95f, 0.13f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Right);
        }

        private Image CreateIntroBackdrop(string name, Transform parent, out RectTransform rect)
        {
            GameObject backdrop = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image image = AddImage(backdrop, new Color(1f, 1f, 1f, 0f));
            image.raycastTarget = false;
            rect = backdrop.GetComponent<RectTransform>();
            AspectRatioFitter fitter = backdrop.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            return image;
        }

        /// <summary>
        /// Oynanış ekranı üç bölgeden oluşur: ince bir editoryal başlık şeridi, arkada nefes
        /// alan sahne illüstrasyonu ve altta arşiv kâğıdı anlatı kartı. Görünür durum çubukları
        /// kaldırıldığı için başlık tek satıra indi ve illüstrasyona belirgin biçimde yer açıldı.
        /// </summary>
        private void BuildGameplay(Transform parent)
        {
            GameObject screen = CreateScreen("Gameplay", parent);
            router.Register(AppScreen.Gameplay, screen);

            GameObject header = CreateRect("Chapter Header", screen.transform, new Vector2(0f, 0.915f), Vector2.one, Vector2.zero, Vector2.zero);
            AddImage(header, new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.95f)).raycastTarget = false;
            AddImage(CreateRect("Header Rule", header.transform, Vector2.zero, new Vector2(1f, 0.045f), Vector2.zero, Vector2.zero), theme.rust).raycastTarget = false;

            chapterText = CreateText("Chapter", header.transform, string.Empty, 23f, FontStyles.Bold, theme.agedPaper,
                new Vector2(0.035f, 0.12f), new Vector2(0.40f, 1f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            chapterText.characterSpacing = 6f;
            dateLocationText = CreateText("Date and Location", header.transform, string.Empty, 17f, FontStyles.Normal, theme.mustard,
                new Vector2(0.40f, 0.12f), new Vector2(0.875f, 1f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Right);
            TMP_Text escape = CreateText("Pause Hint", header.transform, "ESC", 15f, FontStyles.Bold,
                new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.55f),
                new Vector2(0.89f, 0.12f), new Vector2(0.965f, 1f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Right);
            escape.characterSpacing = 3f;

            GameObject card = CreateRect("Narrative Card", screen.transform, new Vector2(0.09f, 0.05f), new Vector2(0.91f, 0.50f), Vector2.zero, Vector2.zero);
            Image cardImage = AddImage(card, new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.98f), theme.paperPanel);
            cardImage.type = Image.Type.Sliced;
            storyCardRect = card.GetComponent<RectTransform>();
            storyCardGroup = card.AddComponent<CanvasGroup>();

            // Sol kenardaki pas şeridi, arşiv dosya sekmesi çağrışımı kurar (ART_DIRECTION §3).
            AddImage(CreateRect("File Tab", card.transform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(9f, 0f)),
                theme.rust).raycastTarget = false;

            echoText = CreateText("Decision Echo", card.transform, string.Empty, 19f, FontStyles.Italic, theme.rust,
                new Vector2(0.04f, 0.86f), new Vector2(0.96f, 0.965f), Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            echoText.enableAutoSizing = true;
            echoText.fontSizeMin = 15f;
            echoText.fontSizeMax = 20f;

            // Dikey ortalama: düğüm gövdeleri 55-110 kelime arasında değiştiği için üstten
            // hizalamak kısa kartlarda düğmelerin üzerinde büyük bir boşluk bırakıyordu.
            storyBodyText = CreateText("Narrative", card.transform, string.Empty, 30f, FontStyles.Normal, theme.ink,
                new Vector2(0.04f, 0.40f), new Vector2(0.96f, EchoVisibleBodyTop), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            storyBodyText.enableAutoSizing = true;
            storyBodyText.fontSizeMin = 21f;
            storyBodyText.fontSizeMax = 30f;
            storyBodyText.lineSpacing = 8f;
            storyBodyRect = storyBodyText.rectTransform;
            scalableBodyTexts.Add(storyBodyText);

            choiceButtons[0] = CreateChoiceButton("Left Choice", card.transform, "A  /  ←", () => SelectChoice(0),
                new Vector2(0.04f, 0.05f), new Vector2(0.487f, 0.36f), out choiceKeyLabels[0], out choiceLabels[0]);
            choiceButtons[1] = CreateChoiceButton("Right Choice", card.transform, "D  /  →", () => SelectChoice(1),
                new Vector2(0.513f, 0.05f), new Vector2(0.96f, 0.36f), out choiceKeyLabels[1], out choiceLabels[1]);
            scalableBodyTexts.Add(choiceLabels[0]);
            scalableBodyTexts.Add(choiceLabels[1]);
        }

        private const float EchoVisibleBodyTop = 0.84f;
        private const float EchoHiddenBodyTop = 0.965f;

        /// <summary>
        /// Seçim düğmesi: sol kenarda pas vurgu şeridi, üstte küçük tuş etiketi, altında eylem
        /// metni. Arka plan sprite'ı beyaz tint ile çizilir; koyu tint, 9-slice kenarındaki pas
        /// çizgisini karartıp düğmeyi düz siyah bir bloğa çeviriyordu.
        /// </summary>
        private Button CreateChoiceButton(string name, Transform parent, string keyHint, UnityEngine.Events.UnityAction action,
            Vector2 anchorMin, Vector2 anchorMax, out TMP_Text keyLabel, out TMP_Text bodyLabel)
        {
            GameObject buttonObject = CreateRect(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            Image image = AddImage(buttonObject, Color.white, theme.buttonPanel);
            image.type = Image.Type.Sliced;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.80f, 1f);
            colors.pressedColor = new Color(0.72f, 0.75f, 0.71f, 1f);
            colors.selectedColor = new Color(0.94f, 0.86f, 0.70f, 1f);
            colors.disabledColor = new Color(0.42f, 0.42f, 0.42f, 0.5f);
            colors.fadeDuration = 0.16f;
            button.colors = colors;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Automatic;
            button.navigation = navigation;
            if (action != null) button.onClick.AddListener(action);

            AddImage(CreateRect("Accent", buttonObject.transform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(6f, 0f)),
                theme.rust).raycastTarget = false;

            keyLabel = CreateText("Key", buttonObject.transform, keyHint, 16f, FontStyles.Bold, theme.mustard,
                new Vector2(0f, 0.60f), new Vector2(1f, 0.93f), new Vector2(26f, 0f), new Vector2(-20f, 0f), TextAlignmentOptions.TopLeft);
            keyLabel.characterSpacing = 3f;

            bodyLabel = CreateText("Label", buttonObject.transform, string.Empty, 24f, FontStyles.Bold, theme.agedPaper,
                new Vector2(0f, 0.07f), new Vector2(1f, 0.60f), new Vector2(26f, 0f), new Vector2(-20f, 0f), TextAlignmentOptions.TopLeft);
            bodyLabel.enableAutoSizing = true;
            bodyLabel.fontSizeMin = 17f;
            bodyLabel.fontSizeMax = 24f;
            return button;
        }

        private void BuildSettings(Transform parent)
        {
            GameObject screen = CreateScreen("Settings", parent);
            router.Register(AppScreen.Settings, screen);
            GameObject panel = CreatePaperPanel("Settings Panel", screen.transform, new Vector2(0.22f, 0.08f), new Vector2(0.78f, 0.92f));
            CreateText("Title", panel.transform, "Ayarlar", 48f, FontStyles.Bold, theme.ink,
                new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            masterSlider = CreateSliderRow(panel.transform, "ANA SES", 0.69f, value => { settings.masterVolume = value; SaveAndApplySettings(); });
            ambientSlider = CreateSliderRow(panel.transform, "ORTAM SESİ", 0.57f, value => { settings.ambientVolume = value; SaveAndApplySettings(); });
            effectsSlider = CreateSliderRow(panel.transform, "EFEKT SESİ", 0.45f, value => { settings.effectsVolume = value; SaveAndApplySettings(); });

            CreateText("Fullscreen Label", panel.transform, "TAM EKRAN", 22f, FontStyles.Bold, theme.ink,
                new Vector2(0.08f, 0.35f), new Vector2(0.43f, 0.41f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            Button fullscreenButton = CreateButton("Fullscreen Toggle", panel.transform, string.Empty, ToggleFullscreen,
                new Vector2(0.55f, 0.34f), new Vector2(0.91f, 0.415f));
            fullscreenValueText = fullscreenButton.GetComponentInChildren<TMP_Text>();

            CreateText("Text Size Label", panel.transform, "METİN BOYUTU", 22f, FontStyles.Bold, theme.ink,
                new Vector2(0.08f, 0.25f), new Vector2(0.43f, 0.31f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            Button textButton = CreateButton("Text Size Toggle", panel.transform, string.Empty, ToggleTextSize,
                new Vector2(0.55f, 0.24f), new Vector2(0.91f, 0.315f));
            textSizeValueText = textButton.GetComponentInChildren<TMP_Text>();

            CreateText("Motion Label", panel.transform, "HAREKETİ AZALT", 22f, FontStyles.Bold, theme.ink,
                new Vector2(0.08f, 0.15f), new Vector2(0.43f, 0.21f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            Button motionButton = CreateButton("Motion Toggle", panel.transform, string.Empty, ToggleMotion,
                new Vector2(0.55f, 0.14f), new Vector2(0.91f, 0.215f));
            motionValueText = motionButton.GetComponentInChildren<TMP_Text>();

            CreateButton("Back", panel.transform, "Geri", CloseSettings, new Vector2(0.08f, 0.035f), new Vector2(0.35f, 0.11f));
        }

        private void BuildCredits(Transform parent)
        {
            GameObject screen = CreateScreen("Credits", parent);
            router.Register(AppScreen.Credits, screen);
            GameObject panel = CreatePaperPanel("Credits Panel", screen.transform, new Vector2(0.19f, 0.09f), new Vector2(0.81f, 0.91f));
            CreateText("Product Title", panel.transform, brand.ProductName, 50f, FontStyles.Bold, theme.ink,
                new Vector2(0.08f, 0.79f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            TMP_Text copy = CreateText("Credits Copy", panel.transform,
                "TASARIM VE GELİŞTİRME\nBerat Sağır\n\nANLATI ÇERÇEVESİ\nBütün karakterler ve kişisel olaylar kurgusaldır. Tarihsel bağlam; Hamburg şehir tarihi kaynakları, müze koleksiyonları ve eğitim materyalleriyle sınanmıştır.\n\nGÖRSEL VE SES\nArşiv kâğıdı, linol baskı ve gölge tiyatrosu yaklaşımıyla bu proje için üretilmiştir. İnternetten alınmış fotoğraf, telifli müzik veya başka bir oyundan içerik kullanılmamıştır.\n\nAYRINTILI KAYNAKLAR\nProje içindeki Docs/HISTORICAL_NOTES.md dosyasındadır.",
                25f, FontStyles.Normal, theme.ink, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.77f), Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            copy.enableAutoSizing = true;
            copy.fontSizeMin = 18f;
            CreateButton("Back", panel.transform, "Ana Menü", () => ShowMainMenu(true), new Vector2(0.08f, 0.045f), new Vector2(0.36f, 0.13f));
        }

        private void BuildContentNote(Transform parent)
        {
            GameObject screen = CreateScreen("Content Note", parent);
            router.Register(AppScreen.ContentNote, screen);
            AddImage(CreateRect("Dim", screen.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), new Color(0f, 0f, 0f, 0.72f));
            GameObject panel = CreatePaperPanel("Content Note Panel", screen.transform, new Vector2(0.28f, 0.27f), new Vector2(0.72f, 0.73f));
            CreateText("Title", panel.transform, "İçerik Notu", 42f, FontStyles.Bold, theme.ink,
                new Vector2(0.1f, 0.70f), new Vector2(0.9f, 0.88f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            TMP_Text note = CreateText("Note", panel.transform,
                "Savaş, bombardıman, zorunlu çalışma ve devlet baskısı temaları içerir. Grafik şiddet içermez.", 29f, FontStyles.Normal, theme.ink,
                new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.67f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            note.enableAutoSizing = true;
            note.fontSizeMin = 22f;
            CreateButton("Continue", panel.transform, "Devam Et", ConfirmContentNote,
                new Vector2(0.25f, 0.10f), new Vector2(0.75f, 0.27f));
        }

        private void BuildPause(Transform parent)
        {
            GameObject screen = CreateScreen("Pause", parent);
            router.Register(AppScreen.Pause, screen);
            AddImage(CreateRect("Dim", screen.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.9f));
            GameObject panel = CreatePaperPanel("Pause Panel", screen.transform, new Vector2(0.35f, 0.2f), new Vector2(0.65f, 0.8f));
            CreateText("Title", panel.transform, "Duraklatıldı", 44f, FontStyles.Bold, theme.ink,
                new Vector2(0.1f, 0.76f), new Vector2(0.9f, 0.9f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            CreateButton("Resume", panel.transform, "Devam Et", ResumeGame, new Vector2(0.14f, 0.56f), new Vector2(0.86f, 0.69f));
            CreateButton("Settings", panel.transform, "Ayarlar", () => OpenSettings(AppScreen.Pause), new Vector2(0.14f, 0.38f), new Vector2(0.86f, 0.51f));
            CreateButton("Main Menu", panel.transform, "Ana Menü", () => ShowMainMenu(true), new Vector2(0.14f, 0.20f), new Vector2(0.86f, 0.33f));
        }

        private void BuildEnding(Transform parent)
        {
            GameObject screen = CreateScreen("Ending", parent);
            router.Register(AppScreen.Ending, screen);
            GameObject panel = CreatePaperPanel("Ending Panel", screen.transform, new Vector2(0.12f, 0.06f), new Vector2(0.88f, 0.94f));
            endingTitleText = CreateText("Ending Title", panel.transform, string.Empty, 50f, FontStyles.Bold, theme.ink,
                new Vector2(0.07f, 0.82f), new Vector2(0.93f, 0.94f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            endingBodyText = CreateText("Ending Body", panel.transform, string.Empty, 27f, FontStyles.Normal, theme.ink,
                new Vector2(0.07f, 0.40f), new Vector2(0.93f, 0.80f), Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            endingBodyText.enableAutoSizing = true;
            endingBodyText.fontSizeMin = 19f;
            endingBodyText.lineSpacing = 7f;
            endingTracesText = CreateText("Traces", panel.transform, string.Empty, 22f, FontStyles.Normal, theme.petrol,
                new Vector2(0.07f, 0.15f), new Vector2(0.93f, 0.38f), Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            endingTracesText.enableAutoSizing = true;
            endingTracesText.fontSizeMin = 17f;
            CreateButton("Replay", panel.transform, "Yeniden Oyna", RequestNewGame,
                new Vector2(0.07f, 0.035f), new Vector2(0.43f, 0.12f));
            CreateButton("Main Menu", panel.transform, "Ana Menü", () => ShowMainMenu(true),
                new Vector2(0.57f, 0.035f), new Vector2(0.93f, 0.12f));
        }

        private void BuildError(Transform parent)
        {
            GameObject screen = CreateScreen("Error", parent);
            router.Register(AppScreen.Error, screen);
            AddImage(CreateRect("Dim", screen.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), new Color(0f, 0f, 0f, 0.78f));
            GameObject panel = CreatePaperPanel("Error Panel", screen.transform, new Vector2(0.27f, 0.25f), new Vector2(0.73f, 0.75f));
            CreateText("Title", panel.transform, "Kayıt / Veri Uyarısı", 38f, FontStyles.Bold, theme.rust,
                new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.88f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            errorText = CreateText("Message", panel.transform, string.Empty, 25f, FontStyles.Normal, theme.ink,
                new Vector2(0.1f, 0.30f), new Vector2(0.9f, 0.70f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            errorText.enableAutoSizing = true;
            errorText.fontSizeMin = 18f;
            CreateButton("New Game", panel.transform, "Yeni Oyun", RequestNewGame,
                new Vector2(0.1f, 0.10f), new Vector2(0.46f, 0.24f));
            CreateButton("Main Menu", panel.transform, "Ana Menü", () => ShowMainMenu(true),
                new Vector2(0.54f, 0.10f), new Vector2(0.9f, 0.24f));
        }

        private void RequestNewGame()
        {
            if (storyController == null)
            {
                ShowError(initializationError ?? "Hikâye verisi kullanılamıyor.");
                return;
            }
            audioManager.PlayConfirm();
            if (!settings.contentNoteSeen)
            {
                router.Show(AppScreen.ContentNote);
                SelectFirstButton(router.Get(AppScreen.ContentNote));
                return;
            }
            StartNewGame();
        }

        private void ConfirmContentNote()
        {
            settings.contentNoteSeen = true;
            settingsService.Save(settings);
            StartNewGame();
        }

        private void StartNewGame()
        {
            StartNewGame(true);
        }

        private void StartNewGame(bool playIntro)
        {
            try
            {
                StoryNode node = storyController.StartNew();
                IntroData intro = storyController.Story == null ? null : storyController.Story.intro;
                if (playIntro && intro != null && intro.HasBeats)
                {
                    if (introRoutine != null) StopCoroutine(introRoutine);
                    introRoutine = StartCoroutine(PlayIntro(intro, node));
                    return;
                }
                RenderNode(node, true);
            }
            catch (Exception exception)
            {
                introActive = false;
                transitionBusy = false;
                Debug.LogError(exception);
                ShowError("Yeni oyun başlatılamadı.\n\n" + exception.Message);
            }
        }

        /// <summary>
        /// Açılış kurgusunu oynatır ve bittiğinde ilk anlatı düğümüne devreder. Herhangi bir tuş
        /// ya da tıklama kurguyu atlar; atlama, açılışı başlatan tıklamanın kazara sayılmaması
        /// için kısa bir süre sonra etkinleşir.
        /// </summary>
        private IEnumerator PlayIntro(IntroData intro, StoryNode firstNode)
        {
            introActive = true;
            introSkipRequested = false;
            introSkipArmTime = Time.unscaledTime + 0.35f;
            transitionBusy = true;
            introGrainFrame = 0;
            introGrainTimer = 0f;

            bool motion = settings == null || !settings.reduceMotion;
            float fade = motion ? theme.introFadeDuration : 0.12f;

            introTextGroup.alpha = 0f;
            introKickerText.text = string.Empty;
            introLineText.text = string.Empty;
            introBackdropA.color = new Color(1f, 1f, 1f, 0f);
            introBackdropB.color = new Color(1f, 1f, 1f, 0f);
            introBackdropARect.localScale = Vector3.one;
            introBackdropBRect.localScale = Vector3.one;
            SetLetterbox(0f);

            router.Show(AppScreen.Intro);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            if (motion) yield return AnimateLetterbox(0f, 1f, 0.45f);
            else SetLetterbox(1f);

            bool useA = true;
            for (int i = 0; i < intro.beats.Length && !introSkipRequested; i++)
            {
                IntroBeat beat = intro.beats[i];
                if (beat == null) continue;

                Image incoming = useA ? introBackdropA : introBackdropB;
                Image outgoing = useA ? introBackdropB : introBackdropA;
                RectTransform incomingRect = useA ? introBackdropARect : introBackdropBRect;
                useA = !useA;

                Sprite sprite;
                incoming.sprite = !string.IsNullOrEmpty(beat.imageKey) && artIndex.TryGetValue(beat.imageKey, out sprite) ? sprite : null;
                incomingRect.localScale = Vector3.one;

                audioManager.PlayAmbienceFor(beat.imageKey);
                audioManager.PlayPaper();

                yield return CrossfadeBackdrops(incoming, outgoing, fade);

                introKickerText.text = beat.kicker ?? string.Empty;
                introKickerText.ForceMeshUpdate();
                int kickerLength = introKickerText.textInfo.characterCount;
                introKickerText.maxVisibleCharacters = motion ? 0 : kickerLength;
                introLineText.text = beat.line ?? string.Empty;

                // Etiket harf harf belirirken satır aynı anda kararmadan çıkar; sıralı çalıştırmak
                // kart başına yarım saniye ekliyor ve etiketi satırın gerisinde bırakıyordu.
                Coroutine reveal = motion ? StartCoroutine(RevealKicker(kickerLength)) : null;
                yield return FadeIntroText(0f, 1f, fade * 0.6f);
                if (reveal != null) yield return reveal;
                else introKickerText.maxVisibleCharacters = kickerLength;

                yield return HoldBeat(incomingRect, beat.ResolvedHold, motion);
                yield return FadeIntroText(introTextGroup.alpha, 0f, fade * 0.55f);
            }

            introActive = false;
            introTextGroup.alpha = 0f;
            transitionBusy = false;
            introRoutine = null;
            RenderNode(firstNode, true);
        }

        private IEnumerator CrossfadeBackdrops(Image incoming, Image outgoing, float duration)
        {
            float startAlpha = outgoing.color.a;
            for (float t = 0f; t < duration && !introSkipRequested; t += Time.unscaledDeltaTime)
            {
                float p = Mathf.Clamp01(t / duration);
                incoming.color = new Color(1f, 1f, 1f, p);
                outgoing.color = new Color(1f, 1f, 1f, startAlpha * (1f - p));
                yield return null;
            }
            incoming.color = Color.white;
            outgoing.color = new Color(1f, 1f, 1f, 0f);
        }

        private IEnumerator FadeIntroText(float from, float to, float duration)
        {
            introTextGroup.alpha = from;
            for (float t = 0f; t < duration && !introSkipRequested; t += Time.unscaledDeltaTime)
            {
                introTextGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }
            introTextGroup.alpha = to;
        }

        private IEnumerator RevealKicker(int characterCount)
        {
            if (characterCount <= 0) yield break;
            const float duration = 0.4f;
            for (float t = 0f; t < duration && !introSkipRequested; t += Time.unscaledDeltaTime)
            {
                introKickerText.maxVisibleCharacters = Mathf.RoundToInt(Mathf.Lerp(0f, characterCount, t / duration));
                yield return null;
            }
            introKickerText.maxVisibleCharacters = characterCount;
        }

        /// <summary>Kartı okunacak süre kadar tutar; hareket açıkken çok yavaş bir yakınlaşma uygular.</summary>
        private IEnumerator HoldBeat(RectTransform rect, float seconds, bool motion)
        {
            const float zoom = 1.055f;
            for (float t = 0f; t < seconds && !introSkipRequested; t += Time.unscaledDeltaTime)
            {
                if (motion) rect.localScale = Vector3.one * Mathf.Lerp(1f, zoom, Mathf.Clamp01(t / seconds));
                yield return null;
            }
        }

        private void SetLetterbox(float amount)
        {
            float height = 0.075f * Mathf.Clamp01(amount);
            introLetterboxTop.anchorMin = new Vector2(0f, 1f - height);
            introLetterboxTop.anchorMax = Vector2.one;
            introLetterboxBottom.anchorMin = Vector2.zero;
            introLetterboxBottom.anchorMax = new Vector2(1f, height);
        }

        private IEnumerator AnimateLetterbox(float from, float to, float duration)
        {
            for (float t = 0f; t < duration && !introSkipRequested; t += Time.unscaledDeltaTime)
            {
                SetLetterbox(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration))));
                yield return null;
            }
            SetLetterbox(to);
        }

        private void UpdateIntroGrain()
        {
            if (introGrainImage == null || theme.introGrain == null || theme.introGrain.Length == 0) return;
            if (settings != null && settings.reduceMotion) return;
            introGrainTimer += Time.unscaledDeltaTime;
            if (introGrainTimer < 0.14f) return;
            introGrainTimer = 0f;
            introGrainFrame = (introGrainFrame + 1) % theme.introGrain.Length;
            introGrainImage.sprite = theme.introGrain[introGrainFrame];
        }

        private void ContinueGame()
        {
            if (storyController == null) return;
            string message;
            if (!storyController.TryContinue(out message))
            {
                ShowError(message);
                return;
            }
            audioManager.PlayConfirm();
            RenderNode(storyController.CurrentNode, true);
        }

        private void SelectChoice(int index)
        {
            if (transitionBusy || storyController == null || storyController.CurrentNode == null) return;
            if (index < 0 || index > 1 || choiceButtons[index] == null || !choiceButtons[index].interactable) return;
            StartCoroutine(AdvanceChoice(index));
        }

        private IEnumerator AdvanceChoice(int index)
        {
            transitionBusy = true;
            audioManager.PlayConfirm();
            float duration = settings.reduceMotion ? 0f : theme.transitionDuration;
            if (duration > 0f)
            {
                Vector2 origin = storyCardRect.anchoredPosition;
                for (float t = 0f; t < duration * 0.45f; t += Time.unscaledDeltaTime)
                {
                    float p = t / (duration * 0.45f);
                    storyCardGroup.alpha = 1f - p;
                    storyCardRect.anchoredPosition = origin + Vector2.left * (22f * p);
                    yield return null;
                }
                storyCardRect.anchoredPosition = origin;
            }

            ChoiceOutcome outcome;
            try
            {
                outcome = storyController.Choose(index);
            }
            catch (Exception exception)
            {
                transitionBusy = false;
                Debug.LogError(exception);
                ShowError("Seçim uygulanamadı.\n\n" + exception.Message);
                yield break;
            }

            RenderNodeContent(outcome.destination);
            if (outcome.destination.IsEnding)
            {
                transitionBusy = false;
                ShowEnding(outcome.destination);
                yield break;
            }
            router.Show(AppScreen.Gameplay);
            storyCardGroup.alpha = duration > 0f ? 0f : 1f;
            if (duration > 0f)
            {
                Vector2 origin = storyCardRect.anchoredPosition;
                storyCardRect.anchoredPosition = origin + Vector2.right * 22f;
                for (float t = 0f; t < duration * 0.55f; t += Time.unscaledDeltaTime)
                {
                    float p = t / (duration * 0.55f);
                    storyCardGroup.alpha = p;
                    storyCardRect.anchoredPosition = Vector2.Lerp(origin + Vector2.right * 22f, origin, p);
                    yield return null;
                }
                storyCardRect.anchoredPosition = origin;
                storyCardGroup.alpha = 1f;
            }
            transitionBusy = false;
        }

        private void RenderNode(StoryNode node, bool animate)
        {
            if (node == null) return;
            if (node.IsEnding)
            {
                ShowEnding(node);
                return;
            }
            RenderNodeContent(node);
            router.Show(AppScreen.Gameplay);
            if (cardRoutine != null) StopCoroutine(cardRoutine);
            cardRoutine = StartCoroutine(ShowCard(animate));
        }

        private void RenderNodeContent(StoryNode node)
        {
            SetBackground(node.imageKey);
            audioManager.PlayAmbienceFor(node.imageKey);
            audioManager.PlayPaper();
            chapterText.text = (node.act ?? string.Empty).ToUpperInvariant();
            dateLocationText.text = (node.date ?? string.Empty) + "   ·   " + (node.location ?? string.Empty);
            storyBodyText.text = node.body ?? string.Empty;
            string echo = storyController.ConsumeEchoes(node);
            bool hasEcho = !string.IsNullOrWhiteSpace(echo);
            echoText.text = echo;
            echoText.gameObject.SetActive(hasEcho);
            // Yankı yoksa anlatı gövdesi o alanı da kullanır; sabit bırakmak kartın üstünde
            // düğüm başına değişen bir boşluk bırakıyordu.
            if (storyBodyRect != null)
            {
                Vector2 anchorMax = storyBodyRect.anchorMax;
                anchorMax.y = hasEcho ? EchoVisibleBodyTop : EchoHiddenBodyTop;
                storyBodyRect.anchorMax = anchorMax;
            }

            for (int i = 0; i < 2; i++)
            {
                ChoiceData choice = node.choices != null && i < node.choices.Length ? node.choices[i] : null;
                bool available = choice != null && ConditionEvaluator.EvaluateAll(choice.conditions, storyController.State);
                choiceButtons[i].interactable = available;
                choiceLabels[i].text = choice == null ? "—" : choice.text;
                if (choiceKeyLabels[i] != null) choiceKeyLabels[i].gameObject.SetActive(choice != null);
            }
        }

        private IEnumerator ShowCard(bool animate)
        {
            transitionBusy = animate && !settings.reduceMotion;
            Vector2 origin = storyCardRect.anchoredPosition;
            float duration = settings.reduceMotion || !animate ? 0f : theme.transitionDuration;
            if (duration <= 0f)
            {
                storyCardGroup.alpha = 1f;
                transitionBusy = false;
                yield break;
            }
            storyCardGroup.alpha = 0f;
            storyCardRect.anchoredPosition = origin + Vector2.right * 24f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float p = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 3f);
                storyCardGroup.alpha = p;
                storyCardRect.anchoredPosition = Vector2.Lerp(origin + Vector2.right * 24f, origin, p);
                yield return null;
            }
            storyCardGroup.alpha = 1f;
            storyCardRect.anchoredPosition = origin;
            transitionBusy = false;
            cardRoutine = null;
        }

        private void ShowEnding(StoryNode node)
        {
            EndingData ending = node.ending;
            SetBackground(node.imageKey);
            audioManager.PlayAmbienceFor(node.imageKey);
            endingTitleText.text = ending.title;
            endingBodyText.text = string.Join("\n\n", ending.paragraphs ?? Array.Empty<string>());
            string[] traces = storyController.BuildEndingTraces(ending);
            endingTracesText.text = "İZLER\n" + (traces.Length == 0 ? "• Bu yolun ayrıntıları kayda geçti." : "• " + string.Join("\n• ", traces));
            router.Show(AppScreen.Ending);
            SelectFirstButton(router.Get(AppScreen.Ending));
        }

        private void ShowMainMenu(bool playBack)
        {
            transitionBusy = false;
            if (playBack) audioManager.PlayBack();
            SetBackground("harbor_dawn");
            audioManager.PlayAmbienceFor("harbor_dawn");
            if (continueButton != null)
            {
                continueButton.interactable = saveService != null && saveService.HasSave && string.IsNullOrEmpty(initializationError);
                ColorBlock colors = continueButton.colors;
                colors.disabledColor = new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.38f);
                continueButton.colors = colors;
            }
            router.Show(AppScreen.MainMenu);
            if (firstSelection != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(firstSelection);
        }

        private void ShowCredits()
        {
            audioManager.PlayConfirm();
            router.Show(AppScreen.Credits);
            SelectFirstButton(router.Get(AppScreen.Credits));
        }

        private void OpenSettings(AppScreen returnScreen)
        {
            audioManager.PlayConfirm();
            settingsReturnScreen = returnScreen;
            RefreshSettingsControls();
            router.Show(AppScreen.Settings);
            SelectFirstButton(router.Get(AppScreen.Settings));
        }

        private void CloseSettings()
        {
            audioManager.PlayBack();
            router.Show(settingsReturnScreen);
            SelectFirstButton(router.Get(settingsReturnScreen));
        }

        private void ShowPause()
        {
            router.Show(AppScreen.Pause);
            SelectFirstButton(router.Get(AppScreen.Pause));
        }

        private void ResumeGame()
        {
            audioManager.PlayBack();
            router.Show(AppScreen.Gameplay);
        }

        private void ShowError(string message)
        {
            if (errorText != null) errorText.text = string.IsNullOrWhiteSpace(message) ? "Beklenmeyen bir sorun oluştu." : message;
            router.Show(AppScreen.Error);
            SelectFirstButton(router.Get(AppScreen.Error));
        }

        private void HandleEscape()
        {
            switch (router.Current)
            {
                case AppScreen.Gameplay: ShowPause(); break;
                case AppScreen.Pause: ResumeGame(); break;
                case AppScreen.Settings: CloseSettings(); break;
                case AppScreen.Credits:
                case AppScreen.ContentNote:
                case AppScreen.Error: ShowMainMenu(true); break;
                case AppScreen.Ending: ShowMainMenu(true); break;
            }
        }

        private void ToggleFullscreen()
        {
            settings.fullscreen = !settings.fullscreen;
            SaveAndApplySettings();
            RefreshSettingsControls();
        }

        private void ToggleTextSize()
        {
            settings.largeText = !settings.largeText;
            SaveAndApplySettings();
            RefreshSettingsControls();
        }

        private void ToggleMotion()
        {
            settings.reduceMotion = !settings.reduceMotion;
            SaveAndApplySettings();
            RefreshSettingsControls();
        }

        private void SaveAndApplySettings()
        {
            settingsService.Save(settings);
            ApplySettings(true);
        }

        private void ApplySettings(bool applyDisplay)
        {
            if (settings == null) settings = new SettingsData();
            settings.Clamp();
            if (Screen.fullScreen != settings.fullscreen && (applyDisplay || !Application.isEditor)) Screen.fullScreen = settings.fullscreen;
            if (audioManager != null) audioManager.ApplySettings(settings);
            ApplyTextScale();
        }

        private void ApplyTextScale()
        {
            if (scalableBodyTexts.Count == 0) return;
            for (int i = 0; i < scalableBodyTexts.Count; i++)
            {
                TMP_Text text = scalableBodyTexts[i];
                if (text == null) continue;
                bool choice = text == choiceLabels[0] || text == choiceLabels[1];
                text.fontSizeMax = settings.largeText ? (choice ? 29f : 37f) : (choice ? 24f : 31f);
                text.fontSizeMin = settings.largeText ? (choice ? 19f : 24f) : (choice ? 17f : 21f);
            }
        }

        private void RefreshSettingsControls()
        {
            if (masterSlider == null) return;
            masterSlider.SetValueWithoutNotify(settings.masterVolume);
            ambientSlider.SetValueWithoutNotify(settings.ambientVolume);
            effectsSlider.SetValueWithoutNotify(settings.effectsVolume);
            fullscreenValueText.text = settings.fullscreen ? "Açık" : "Kapalı";
            textSizeValueText.text = settings.largeText ? "Büyük" : "Normal";
            motionValueText.text = settings.reduceMotion ? "Açık" : "Kapalı";
        }

        private void SetBackground(string key)
        {
            Sprite sprite;
            if (!string.IsNullOrEmpty(key) && artIndex.TryGetValue(key, out sprite)) backgroundArt.sprite = sprite;
            backgroundArt.color = backgroundArt.sprite == null ? theme.sootNavy : Color.white;
        }

        private void UpdateParallax()
        {
            if (backgroundRect == null) return;
            if (settings == null || settings.reduceMotion)
            {
                backgroundRect.anchoredPosition = Vector2.Lerp(backgroundRect.anchoredPosition, Vector2.zero, Time.unscaledDeltaTime * 10f);
                return;
            }
            Vector2 normalized = new Vector2(Input.mousePosition.x / Mathf.Max(1f, Screen.width) - 0.5f, Input.mousePosition.y / Mathf.Max(1f, Screen.height) - 0.5f);
            Vector2 target = normalized * -18f;
            backgroundRect.anchoredPosition = Vector2.Lerp(backgroundRect.anchoredPosition, target, Time.unscaledDeltaTime * 1.8f);
        }

        private void ExitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ReadCommandLineQaOptions()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (string.Equals(arguments[i], "-ordinaryFrontsSmoke", StringComparison.OrdinalIgnoreCase)) commandLineSmoke = true;
                if (string.Equals(arguments[i], "-ordinaryFrontsSmokePath", StringComparison.OrdinalIgnoreCase) && i + 1 < arguments.Length)
                {
                    TestSaveDirectoryOverride = arguments[++i];
                }
                if (string.Equals(arguments[i], "-ordinaryFrontsCaptureDirectory", StringComparison.OrdinalIgnoreCase) && i + 1 < arguments.Length)
                {
                    commandLineCaptureDirectory = arguments[++i];
                }
            }
        }

        private IEnumerator RunCommandLineSmoke()
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(TestSaveDirectoryOverride ?? Application.temporaryCachePath);
            if (!string.IsNullOrWhiteSpace(commandLineCaptureDirectory))
            {
                Directory.CreateDirectory(commandLineCaptureDirectory);
                CaptureInterfaceOffscreen(Path.Combine(commandLineCaptureDirectory, "menu_" + Screen.width + "x" + Screen.height + ".png"));
                yield return null;
                yield return CaptureIntroForQa();
            }

            Exception smokeFailure = null;
            try { StartNewGameForTests(); }
            catch (Exception exception) { smokeFailure = exception; }
            yield return null;
            if (smokeFailure == null)
            {
                try { ChooseForTests(0); }
                catch (Exception exception) { smokeFailure = exception; }
            }
            yield return null;
            if (smokeFailure == null)
            {
                try { ChooseForTests(1); }
                catch (Exception exception) { smokeFailure = exception; }
            }
            yield return null;
            Canvas.ForceUpdateCanvases();

            List<string> overflow = new List<string>();
            GameObject gameplay = router.Get(AppScreen.Gameplay);
            TMP_Text[] texts = gameplay == null ? Array.Empty<TMP_Text>() : gameplay.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++) if (texts[i].isTextOverflowing) overflow.Add(texts[i].name);
            if (!string.IsNullOrWhiteSpace(commandLineCaptureDirectory))
            {
                CaptureInterfaceOffscreen(Path.Combine(commandLineCaptureDirectory, "gameplay_" + Screen.width + "x" + Screen.height + ".png"));
                yield return null;
            }
            settings.largeText = true;
            ApplyTextScale();
            Canvas.ForceUpdateCanvases();
            for (int i = 0; i < texts.Length; i++)
                if (texts[i].isTextOverflowing && !overflow.Contains("Büyük/" + texts[i].name)) overflow.Add("Büyük/" + texts[i].name);
            if (!string.IsNullOrWhiteSpace(commandLineCaptureDirectory))
            {
                CaptureInterfaceOffscreen(Path.Combine(commandLineCaptureDirectory, "gameplay_large_" + Screen.width + "x" + Screen.height + ".png"));
                yield return null;
            }
            if (smokeFailure != null)
                Debug.LogError("ORDINARY_FRONTS_PLAYER_SMOKE_FAILED: " + smokeFailure);
            else if (overflow.Count > 0)
                Debug.LogError("ORDINARY_FRONTS_PLAYER_SMOKE_FAILED: overflowing TMP elements: " + string.Join(", ", overflow.ToArray()));
            else if (!File.Exists(saveService.SavePath))
                Debug.LogError("ORDINARY_FRONTS_PLAYER_SMOKE_FAILED: autosave file was not created.");
            else
                Debug.Log("ORDINARY_FRONTS_PLAYER_SMOKE_SUCCESS: node=" + CurrentNodeId + " save=" + saveService.SavePath + " resolution=" + Screen.width + "x" + Screen.height);
            yield return null;
            Application.Quit();
        }

        /// <summary>
        /// Görsel QA için açılış kurgusundan iki kare yakalar: ilk kart yerleştiğinde ve
        /// bir sonraki kart okunurken. Ardından kurguyu atlayıp duman testine devreder.
        /// </summary>
        private IEnumerator CaptureIntroForQa()
        {
            PlayIntroForTests();
            yield return null;
            // Her kartın tutma süresinin ortasına denk gelecek biçimde seçildi; kartların
            // süreleri değişirse burası da güncellenmelidir.
            float[] captureAt = { 2.5f, 6.5f, 10.4f, 14.5f };
            int index = 0;
            float elapsed = 0f;
            while (introActive && index < captureAt.Length)
            {
                elapsed += Time.unscaledDeltaTime;
                if (elapsed >= captureAt[index])
                {
                    Canvas.ForceUpdateCanvases();
                    CaptureInterfaceOffscreen(Path.Combine(commandLineCaptureDirectory,
                        "intro_" + (index + 1) + "_" + Screen.width + "x" + Screen.height + ".png"));
                    index++;
                }
                yield return null;
            }
            RequestIntroSkipForTests();
            float guard = 6f;
            while (introActive && guard > 0f)
            {
                guard -= Time.unscaledDeltaTime;
                yield return null;
            }
            yield return null;
        }

        private void CaptureInterfaceOffscreen(string path)
        {
            Camera captureCamera = Camera.main;
            if (captureCamera == null) throw new InvalidOperationException("Offscreen QA için Main Camera bulunamadı.");
            int width = Mathf.Max(640, Screen.width);
            int height = Mathf.Max(360, Screen.height);
            RenderMode previousMode = rootCanvas.renderMode;
            Camera previousCanvasCamera = rootCanvas.worldCamera;
            RenderTexture previousTarget = captureCamera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false, false);
            try
            {
                rootCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                rootCanvas.worldCamera = captureCamera;
                rootCanvas.planeDistance = 1f;
                Canvas.ForceUpdateCanvases();
                captureCamera.targetTexture = target;
                captureCamera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                image.Apply(false, false);
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                captureCamera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                rootCanvas.renderMode = previousMode;
                rootCanvas.worldCamera = previousCanvasCamera;
                target.Release();
                Destroy(target);
                Destroy(image);
            }
        }

        private GameObject CreateScreen(string name, Transform parent)
        {
            return CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private GameObject CreatePaperPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject panel = CreateRect(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            Image image = AddImage(panel, new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.98f), theme.paperPanel);
            image.type = Image.Type.Sliced;
            AddImage(CreateRect("Rust Rule", panel.transform, new Vector2(0f, 0.985f), Vector2.one, Vector2.zero, Vector2.zero), theme.rust);
            return panel;
        }

        private Slider CreateSliderRow(Transform parent, string label, float centerY, UnityEngine.Events.UnityAction<float> callback)
        {
            CreateText(label + " Label", parent, label, 22f, FontStyles.Bold, theme.ink,
                new Vector2(0.08f, centerY - 0.025f), new Vector2(0.43f, centerY + 0.035f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            GameObject sliderObject = CreateRect(label + " Slider", parent, new Vector2(0.55f, centerY - 0.015f), new Vector2(0.91f, centerY + 0.025f), Vector2.zero, Vector2.zero);
            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;
            GameObject background = CreateRect("Background", sliderObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddImage(background, new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.22f));
            GameObject fillArea = CreateRect("Fill Area", sliderObject.transform, Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(-10f, 0f));
            GameObject fill = CreateRect("Fill", fillArea.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image fillImage = AddImage(fill, theme.petrol);
            GameObject handleArea = CreateRect("Handle Slide Area", sliderObject.transform, Vector2.zero, Vector2.one, new Vector2(8f, -6f), new Vector2(-8f, 6f));
            GameObject handle = CreateRect("Handle", handleArea.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-9f, -18f), new Vector2(9f, 18f));
            Image handleImage = AddImage(handle, theme.rust);
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handleImage;
            slider.onValueChanged.AddListener(callback);
            return slider;
        }

        private Button CreateButton(string name, Transform parent, string label, UnityEngine.Events.UnityAction action, Vector2 anchorMin, Vector2 anchorMax, bool narrative = false)
        {
            GameObject buttonObject = CreateRect(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            Image image = AddImage(buttonObject, narrative ? theme.sootNavy : new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.96f), theme.buttonPanel);
            image.type = Image.Type.Sliced;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.76f, 1f);
            colors.pressedColor = new Color(0.73f, 0.76f, 0.72f, 1f);
            colors.selectedColor = new Color(0.88f, 0.79f, 0.63f, 1f);
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.55f);
            colors.fadeDuration = 0.18f;
            button.colors = colors;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Automatic;
            button.navigation = navigation;
            if (action != null) button.onClick.AddListener(action);
            TMP_Text text = CreateText("Label", buttonObject.transform, label, narrative ? 24f : 25f, FontStyles.Bold, theme.agedPaper,
                new Vector2(0.045f, 0.08f), new Vector2(0.955f, 0.92f), Vector2.zero, Vector2.zero, narrative ? TextAlignmentOptions.Left : TextAlignmentOptions.Center);
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = narrative ? 17f : 18f;
            text.fontSizeMax = narrative ? 24f : 27f;
            return button;
        }

        private TMP_Text CreateText(string name, Transform parent, string value, float size, FontStyles style, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, TextAlignmentOptions alignment)
        {
            GameObject textObject = CreateRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.margin = new Vector4(2f, 2f, 2f, 2f);
            return text;
        }

        private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
            return gameObject;
        }

        private static Image AddImage(GameObject gameObject, Color color, Sprite sprite = null)
        {
            Image image = gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            return image;
        }

        private static void SelectFirstButton(GameObject screen)
        {
            if (screen == null || EventSystem.current == null) return;
            Button button = screen.GetComponentInChildren<Button>(true);
            if (button != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        /// <summary>
        /// Testler ve komut satırı duman koşusu açılış kurgusunu atlar; doğrulanmak istenen şey
        /// oynanış döngüsüdür ve kurgu ölçümü zamana bağımlı hâle getirirdi.
        /// Kurgunun kendisi <see cref="PlayIntroForTests"/> ile ayrıca sınanır.
        /// </summary>
        public void StartNewGameForTests()
        {
            if (settings != null) settings.contentNoteSeen = true;
            StartNewGame(false);
        }

        public void PlayIntroForTests()
        {
            if (settings != null) settings.contentNoteSeen = true;
            StartNewGame(true);
        }

        public void RequestIntroSkipForTests()
        {
            introSkipRequested = true;
        }

        public void ChooseForTests(int index)
        {
            if (storyController == null || storyController.CurrentNode == null) throw new InvalidOperationException("Hikâye hazır değil.");
            ChoiceOutcome outcome = storyController.Choose(index);
            if (outcome.destination.IsEnding) ShowEnding(outcome.destination);
            else
            {
                RenderNodeContent(outcome.destination);
                router.Show(AppScreen.Gameplay);
                // Bu yol kart geçiş animasyonunu atlar. Süren ShowCard coroutine'i durdurulmazsa
                // her karede alfayı geri yazar; kart yarı saydam kalır ve görsel QA yakalamaları
                // kartı şeffaf gösterir.
                if (cardRoutine != null)
                {
                    StopCoroutine(cardRoutine);
                    cardRoutine = null;
                }
                transitionBusy = false;
                if (storyCardGroup != null) storyCardGroup.alpha = 1f;
            }
        }
    }
}
