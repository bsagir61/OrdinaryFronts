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
        private readonly Image[] statusFills = new Image[4];
        private readonly RectTransform[] statusFillRects = new RectTransform[4];
        private Slider masterSlider;
        private Slider ambientSlider;
        private Slider effectsSlider;
        private AppScreen settingsReturnScreen = AppScreen.MainMenu;
        private bool transitionBusy;
        private bool commandLineSmoke;
        private string commandLineCaptureDirectory;
        private Coroutine cardRoutine;
        private Coroutine statsRoutine;
        private GameObject firstSelection;
        private string initializationError;

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
            ShowMainMenu(false);
            if (!string.IsNullOrEmpty(initializationError)) ShowError(initializationError);
            else if (commandLineSmoke) StartCoroutine(RunCommandLineSmoke());
        }

        private void Update()
        {
            UpdateParallax();
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
            CreateButton("Credits", screen.transform, "Künye", ShowCredits,
                new Vector2(0.08f, 0.255f), new Vector2(0.385f, 0.325f));
            CreateButton("Exit", screen.transform, "Çıkış", ExitApplication,
                new Vector2(0.08f, 0.165f), new Vector2(0.385f, 0.235f));
            firstSelection = newButton.gameObject;

            TMP_Text hint = CreateText("Input Hint", screen.transform, "Fare veya yön tuşlarıyla gezin · Enter ile onayla", 18f, FontStyles.Normal,
                new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.7f), new Vector2(0.08f, 0.105f), new Vector2(0.385f, 0.145f), Vector2.zero, Vector2.zero, TextAlignmentOptions.BottomLeft);
            hint.enableAutoSizing = true;
            hint.fontSizeMin = 14f;
        }

        private void BuildGameplay(Transform parent)
        {
            GameObject screen = CreateScreen("Gameplay", parent);
            router.Register(AppScreen.Gameplay, screen);

            GameObject header = CreateRect("Chapter Header", screen.transform, new Vector2(0f, 0.86f), Vector2.one, Vector2.zero, Vector2.zero);
            AddImage(header, new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.94f));
            AddImage(CreateRect("Header Rule", header.transform, new Vector2(0f, 0f), new Vector2(1f, 0.035f), Vector2.zero, Vector2.zero), theme.rust);
            chapterText = CreateText("Chapter", header.transform, string.Empty, 25f, FontStyles.Bold, theme.agedPaper,
                new Vector2(0.045f, 0.47f), new Vector2(0.28f, 0.88f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            chapterText.characterSpacing = 2f;
            dateLocationText = CreateText("Date and Location", header.transform, string.Empty, 18f, FontStyles.Normal, theme.mustard,
                new Vector2(0.045f, 0.12f), new Vector2(0.38f, 0.48f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);

            string[] labels = { "DAYANIKLILIK", "ERZAK", "BAĞLAR", "GÖZETİM" };
            Color[] fills = { theme.petrol, theme.mustard, theme.rust, new Color32(0x8B, 0x55, 0x53, 0xFF) };
            for (int i = 0; i < 4; i++)
            {
                float x0 = 0.43f + i * 0.135f;
                statusFills[i] = CreateStatusBar(header.transform, labels[i], fills[i], new Vector2(x0, 0.24f), new Vector2(x0 + 0.115f, 0.76f));
                statusFillRects[i] = statusFills[i].rectTransform;
            }

            GameObject card = CreateRect("Narrative Card", screen.transform, new Vector2(0.055f, 0.045f), new Vector2(0.945f, 0.61f), Vector2.zero, Vector2.zero);
            Image cardImage = AddImage(card, new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.97f), theme.paperPanel);
            cardImage.type = Image.Type.Sliced;
            storyCardRect = card.GetComponent<RectTransform>();
            storyCardGroup = card.AddComponent<CanvasGroup>();
            AddImage(CreateRect("Card Rule", card.transform, new Vector2(0f, 0.982f), Vector2.one, Vector2.zero, Vector2.zero), theme.rust);
            echoText = CreateText("Decision Echo", card.transform, string.Empty, 19f, FontStyles.Italic, theme.rust,
                new Vector2(0.045f, 0.80f), new Vector2(0.955f, 0.95f), Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            echoText.enableAutoSizing = true;
            echoText.fontSizeMin = 15f;
            storyBodyText = CreateText("Narrative", card.transform, string.Empty, 31f, FontStyles.Normal, theme.ink,
                new Vector2(0.045f, 0.315f), new Vector2(0.955f, 0.82f), Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            storyBodyText.enableAutoSizing = true;
            storyBodyText.fontSizeMin = 21f;
            storyBodyText.fontSizeMax = 31f;
            storyBodyText.lineSpacing = 7f;
            scalableBodyTexts.Add(storyBodyText);

            choiceButtons[0] = CreateButton("Left Choice", card.transform, string.Empty, () => SelectChoice(0),
                new Vector2(0.045f, 0.065f), new Vector2(0.49f, 0.285f), true);
            choiceButtons[1] = CreateButton("Right Choice", card.transform, string.Empty, () => SelectChoice(1),
                new Vector2(0.51f, 0.065f), new Vector2(0.955f, 0.285f), true);
            choiceLabels[0] = choiceButtons[0].GetComponentInChildren<TMP_Text>();
            choiceLabels[1] = choiceButtons[1].GetComponentInChildren<TMP_Text>();
            scalableBodyTexts.Add(choiceLabels[0]);
            scalableBodyTexts.Add(choiceLabels[1]);

            TMP_Text escape = CreateText("Pause Hint", screen.transform, "ESC  DURAKLAT", 15f, FontStyles.Normal,
                new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.7f), new Vector2(0.82f, 0.825f), new Vector2(0.95f, 0.855f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Right);
            escape.characterSpacing = 2f;
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
                "TASARIM VE GELİŞTİRME\nÖzgün bir dikey kesit çalışması\n\nANLATI ÇERÇEVESİ\nBütün karakterler ve kişisel olaylar kurgusaldır. Tarihsel bağlam; Hamburg şehir tarihi kaynakları, müze koleksiyonları ve eğitim materyalleriyle sınanmıştır.\n\nGÖRSEL VE SES\nArşiv kâğıdı, linol baskı ve gölge tiyatrosu yaklaşımıyla bu proje için üretilmiştir. İnternetten alınmış fotoğraf, telifli müzik veya başka bir oyundan içerik kullanılmamıştır.\n\nAYRINTILI KAYNAKLAR\nProje içindeki Docs/HISTORICAL_NOTES.md dosyasındadır.",
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
            try
            {
                StoryNode node = storyController.StartNew();
                UpdateStats(storyController.State.stats, false);
                RenderNode(node, true);
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                ShowError("Yeni oyun başlatılamadı.\n\n" + exception.Message);
            }
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
            UpdateStats(storyController.State.stats, false);
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

            if (statsRoutine != null) StopCoroutine(statsRoutine);
            statsRoutine = StartCoroutine(AnimateStats(outcome.before, outcome.after));
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
            echoText.text = echo;
            echoText.gameObject.SetActive(!string.IsNullOrWhiteSpace(echo));
            for (int i = 0; i < 2; i++)
            {
                ChoiceData choice = node.choices != null && i < node.choices.Length ? node.choices[i] : null;
                bool available = choice != null && ConditionEvaluator.EvaluateAll(choice.conditions, storyController.State);
                choiceButtons[i].interactable = available;
                string keys = i == 0 ? "A  /  ←" : "D  /  →";
                choiceLabels[i].text = keys + "\n" + (choice == null ? "—" : choice.text);
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

        private void UpdateStats(StatBlock stats, bool animate)
        {
            if (stats == null) return;
            float[] values = { stats.resilience / 100f, stats.supplies / 100f, stats.bonds / 100f, stats.surveillance / 100f };
            for (int i = 0; i < statusFillRects.Length; i++) SetStatusWidth(i, values[i]);
        }

        private IEnumerator AnimateStats(StatBlock before, StatBlock after)
        {
            float[] starts = { before.resilience / 100f, before.supplies / 100f, before.bonds / 100f, before.surveillance / 100f };
            float[] ends = { after.resilience / 100f, after.supplies / 100f, after.bonds / 100f, after.surveillance / 100f };
            float duration = settings.reduceMotion ? 0f : 0.3f;
            if (duration <= 0f)
            {
                UpdateStats(after, false);
                yield break;
            }
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float p = Mathf.SmoothStep(0f, 1f, t / duration);
                for (int i = 0; i < 4; i++) SetStatusWidth(i, Mathf.Lerp(starts[i], ends[i], p));
                yield return null;
            }
            UpdateStats(after, false);
            statsRoutine = null;
        }

        private void SetStatusWidth(int index, float normalized)
        {
            if (index < 0 || index >= statusFillRects.Length || statusFillRects[index] == null) return;
            RectTransform rect = statusFillRects[index];
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(Mathf.Clamp01(normalized), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void ExitApplication()
        {
#if UNITY_EDITOR
            Type editorApplication = Type.GetType("UnityEditor.EditorApplication, UnityEditor");
            if (editorApplication != null)
            {
                System.Reflection.PropertyInfo property = editorApplication.GetProperty("isPlaying", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (property != null) property.SetValue(null, false, null);
            }
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

        private Image CreateStatusBar(Transform parent, string label, Color fillColor, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject root = CreateRect(label, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            TMP_Text text = CreateText("Label", root.transform, label, 14f, FontStyles.Normal, theme.agedPaper,
                new Vector2(0f, 0.52f), Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.BottomLeft);
            text.characterSpacing = 1.5f;
            GameObject track = CreateRect("Track", root.transform, new Vector2(0f, 0.12f), new Vector2(1f, 0.38f), Vector2.zero, Vector2.zero);
            AddImage(track, new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.18f));
            GameObject fill = CreateRect("Fill", track.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image image = AddImage(fill, fillColor);
            image.type = Image.Type.Simple;
            RectTransform fillRect = image.rectTransform;
            fillRect.anchorMax = new Vector2(0.5f, 1f);
            fillRect.offsetMax = Vector2.zero;
            return image;
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

        public void StartNewGameForTests()
        {
            if (settings != null) settings.contentNoteSeen = true;
            StartNewGame();
        }

        public void ChooseForTests(int index)
        {
            if (storyController == null || storyController.CurrentNode == null) throw new InvalidOperationException("Hikâye hazır değil.");
            ChoiceOutcome outcome = storyController.Choose(index);
            UpdateStats(outcome.after, false);
            if (outcome.destination.IsEnding) ShowEnding(outcome.destination);
            else
            {
                RenderNodeContent(outcome.destination);
                router.Show(AppScreen.Gameplay);
            }
        }
    }
}
