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

    public sealed partial class AppController : MonoBehaviour
    {
        // Test ve QA yüzeyinin tamamı yalnız Editor ve Development build'de derlenir. Yayın
        // sürümünde bu üyeler ulaşılamaz olmakla kalmaz, montaja hiç girmez: tüketiciye giden
        // ikilide ekran görüntüsü yazan, oyunu kendi kendine oynatan veya kayıt yolunu
        // değiştiren hiçbir kod bulunmamalıdır.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static string TestSaveDirectoryOverride;
#endif

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
        private ArchiveService archiveService;
        private SettingsService settingsService;
        private SettingsData settings;
        private StoryController storyController;
        private LocalizationService localization;
        private TMP_Text languageValueText;
        private StoryCatalog catalog;
        private GameObject storySelectFirstSelection;
        private readonly List<StoryMarker> storyMarkers = new List<StoryMarker>();
        private StoryCatalogEntry selectedStory;
        private Image storySelectArt;
        private TMP_Text storySelectTitle;
        private TMP_Text storySelectPeriod;
        private TMP_Text storySelectLine;
        private Button storySelectBegin;
        private string activeStoryId = StoryRepository.DefaultStoryId;

        /// <summary>Arayüz metni kısayolu.</summary>
        private string T(string key)
        {
            return localization == null ? key : localization.Get(key);
        }

        /// <summary>
        /// Seçili dilin hikâye dosyasını yükler ve doğrular. Oyun sürerken çağrılırsa
        /// oyuncunun ilerlemesi korunur; düğüm kimlikleri diller arasında aynıdır.
        /// </summary>
        private void LoadStoryForLocale(string locale)
        {
            catalog = StoryCatalog.Load(locale);
            StoryCatalogEntry first = catalog.FirstAvailable();
            if (first != null && string.IsNullOrWhiteSpace(activeStoryId)) activeStoryId = first.storyId;

            StoryRepository repository = new StoryRepository(null, locale, activeStoryId);
            repository.Load();
            List<string> issues = StoryGraphValidator.Validate(repository.Database);
            if (issues.Count > 0)
                throw new InvalidDataException(T(UiKey.ErrorStoryValidation) + string.Join(" | ", issues.ToArray()));

            if (storyController == null)
            {
                storyController = new StoryController(repository, saveService, archiveService);
                storyController.Initialize();
            }
            else
            {
                storyController.SwapRepository(repository);
            }
        }

        private Image backgroundArt;
        private RectTransform backgroundRect;
        private string currentBackgroundKey;
        private CanvasGroup storyCardGroup;
        private RectTransform storyCardRect;
        private TMP_Text chapterText;
        private TMP_Text dateLocationText;
        private TMP_Text storyBodyText;
        private TMP_Text echoText;
        private Image echoRule;
        private TMP_Text endingTitleText;
        private TMP_Text endingBodyText;
        private TMP_Text endingTracesText;
        private TMP_Text journalText;
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool commandLineSmoke;
        private string commandLineCaptureDirectory;
#endif
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
            string storage = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ReadCommandLineQaOptions();
            if (commandLineSmoke) Application.runInBackground = true;
            if (!string.IsNullOrWhiteSpace(TestSaveDirectoryOverride)) storage = TestSaveDirectoryOverride;
#endif
            EnsureDependencies();
            IndexArt();
            saveService = new SaveService(storage);
            archiveService = new ArchiveService(storage);
            archiveService.Load();
            settingsService = new SettingsService(storage);
            settings = settingsService.Load();
            ApplySettings(false);

            localization = new LocalizationService();
            try
            {
                localization.Load(settings.locale);
            }
            catch (Exception exception)
            {
                // Arayüz metni olmadan hiçbir hata bile gösterilemez; varsayılan dile düşülür.
                Debug.LogError(exception);
                settings.locale = LocalizationService.DefaultLocale;
                localization.Load(LocalizationService.DefaultLocale);
            }

            try
            {
                LoadStoryForLocale(settings.locale);
            }
            catch (Exception exception)
            {
                initializationError = localization.Get(UiKey.ErrorInitFailed) + "\n\n" + exception.Message;
                Debug.LogError(exception);
            }

            BuildInterface();
            // Metin ölçeği yalnız BuildInterface sonrasında uygulanabilir; Awake başındaki
            // ApplySettings çağrısı henüz hiç metin oluşmadığı için erken dönüyor.
            ApplyTextScale();
            ShowMainMenu(false);
            if (!string.IsNullOrEmpty(initializationError)) ShowError(initializationError);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            else if (commandLineSmoke) StartCoroutine(RunCommandLineSmoke());
#endif
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
            if (interludeActive)
            {
                // Ara sahnede tek arayüz komutu geçmektir; itme tuşları sahnenin kendi
                // döngüsünde okunur.
                if (Input.GetKeyDown(KeyCode.Escape)) interludeSkipRequested = true;
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
            if (previous != null)
            {
                // Destroy bir kare gecikir; eski arayüz o kare boyunca yenisiyle üst üste
                // çizilmesin diye hemen kapatılır.
                previous.gameObject.SetActive(false);
                Destroy(previous.gameObject);
            }
            GameObject runtimeRoot = CreateRect("Runtime Interface", rootCanvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject background = CreateRect("Background Illustration", runtimeRoot.transform, Vector2.zero, Vector2.one, new Vector2(-28f, -28f), new Vector2(28f, 28f));
            backgroundArt = background.AddComponent<Image>();
            // Sprite henüz atanmadı; beyaz bırakılırsa SetBackground çağrılana kadar ekranda
            // düz beyaz bir yüzey kalır. Koyu zemin hem güvenli hem palete uygun başlangıçtır.
            backgroundArt.color = theme.sootNavy;
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
            BuildStorySelect(runtimeRoot.transform);
            BuildIntro(runtimeRoot.transform);
            BuildGameplay(runtimeRoot.transform);
            BuildInterludeScreen(runtimeRoot.transform);
            BuildSettings(runtimeRoot.transform);
            BuildPause(runtimeRoot.transform);
            BuildJournal(runtimeRoot.transform);
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
            // Tek bir şehre ve yıla bağlı bağlam satırı kaldırıldı: oyun bir antoloji ve
            // dönem bilgisi artık bölüm kartında duruyor.
            Button newButton = CreateButton("New Game", screen.transform, T(UiKey.MenuNewGame), ShowStorySelect,
                new Vector2(0.08f, 0.525f), new Vector2(0.385f, 0.595f));
            continueButton = CreateButton("Continue", screen.transform, T(UiKey.MenuContinue), ContinueGame,
                new Vector2(0.08f, 0.435f), new Vector2(0.385f, 0.505f));
            CreateButton("Settings", screen.transform, T(UiKey.MenuSettings), () => OpenSettings(AppScreen.MainMenu),
                new Vector2(0.08f, 0.345f), new Vector2(0.385f, 0.415f));
            CreateButton("Exit", screen.transform, T(UiKey.MenuExit), ExitApplication,
                new Vector2(0.08f, 0.255f), new Vector2(0.385f, 0.325f));
            firstSelection = newButton.gameObject;
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

            CreateText("Skip Hint", screen.transform, T(UiKey.IntroSkipHint), 17f, FontStyles.Normal,
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
            dateLocationText = (TMP_Text)AsDocument(CreateText("Date and Location", header.transform, string.Empty, 17f, FontStyles.Normal, theme.mustard,
                new Vector2(0.40f, 0.12f), new Vector2(0.875f, 1f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Right));
            TMP_Text escape = CreateText("Pause Hint", header.transform, T(UiKey.GameplayPauseHint), 15f, FontStyles.Bold,
                new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.55f),
                new Vector2(0.89f, 0.12f), new Vector2(0.965f, 1f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Right);
            escape.characterSpacing = 3f;

            GameObject card = CreateRect("Narrative Card", screen.transform, new Vector2(0.09f, 0.05f), new Vector2(0.91f, 0.50f), Vector2.zero, Vector2.zero);
            // Kâğıt panel yarı saydam: arkadaki illüstrasyon kartın içinden okunur, mürekkep
            // gövde metni yine de yeterli kontrastta kalır (koyu arka planda ~6:1).
            Image cardImage = AddImage(card, new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.72f), theme.paperPanel);
            cardImage.type = Image.Type.Sliced;
            storyCardRect = card.GetComponent<RectTransform>();
            storyCardGroup = card.AddComponent<CanvasGroup>();

            // Sol kenardaki pas şeridi, arşiv dosya sekmesi çağrışımı kurar (ART_DIRECTION §3).
            AddImage(CreateRect("File Tab", card.transform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(9f, 0f)),
                theme.rust).raycastTarget = false;

            // Yankı bandı. Eskiden pas kırmızısı, italik ve gövdeden küçüktü: bu üçü bir arada
            // dokulu kâğıt üzerindeki en okunmaz bileşimdir. Şimdi yankı cümlesi anlatı
            // gövdesiyle aynı mürekkep renginde ve daha büyük; ayrımı renk değil, üstündeki
            // küçük büyük-harfli etiket ile altındaki ince çizgi kuruyor.
            echoText = (TMP_Text)AsDocument(CreateText("Decision Echo", card.transform, string.Empty, 25f, FontStyles.Normal, theme.ink,
                new Vector2(0.04f, 0.832f), new Vector2(0.96f, 0.976f), Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft));
            echoText.enableAutoSizing = true;
            echoText.fontSizeMin = 20f;
            echoText.fontSizeMax = 25f;
            echoText.richText = true;
            echoText.lineSpacing = 6f;

            echoRule = AddImage(CreateRect("Echo Rule", card.transform,
                new Vector2(0.04f, 0.820f), new Vector2(0.96f, 0.824f), Vector2.zero, Vector2.zero),
                new Color(ArchiveLabel.r, ArchiveLabel.g, ArchiveLabel.b, 0.45f));
            echoRule.raycastTarget = false;

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

        /// <summary>
        /// Yankı bandının metni: üstte küçük, büyük harfli ve harf aralıklı bir etiket,
        /// altında yankı cümleleri. Etiket yerelleştirmeden gelir; sondaki ayraç temizlenir,
        /// çünkü etiket artık cümlenin başında değil ayrı bir satırdadır.
        /// </summary>
        private string ComposeEchoBlock(string[] lines)
        {
            string label = T(UiKey.EchoPrefix).TrimEnd(' ', '—', '–', '-', ':', '·');
            if (localization != null) label = localization.ToUpper(label);

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(ArchiveLabel))
                   .Append("><size=68%><cspace=0.16em><b>").Append(label)
                   .Append("</b></cspace></size></color>");
            for (int i = 0; i < lines.Length; i++)
            {
                builder.Append('\n');
                // Tek yankıda madde imi gürültü olurdu; birden fazlaysa hangisinin nerede
                // bittiği ancak imle okunuyor.
                if (lines.Length > 1) builder.Append("• ");
                builder.Append(lines[i]);
            }
            return builder.ToString();
        }

        /// <summary>
        /// Kâğıt üzerindeki ikincil metin: eskimiş daktilo mürekkebi. Petrol mavisi bu zeminde
        /// soğuk ve modern duruyordu; sahne illüstrasyonlarının sıcak tonuyla da çakışıyordu.
        /// Renk paletin dışından gelmez, mürekkebin pasa doğru kırılmasıyla üretilir.
        /// </summary>
        private Color ArchiveInk { get { return Color.Lerp(theme.ink, theme.rust, 0.22f); } }

        /// <summary>Kâğıt üzerindeki başlık ve etiket: soluk damga kırmızısı.</summary>
        private Color ArchiveLabel { get { return Color.Lerp(theme.rust, theme.ink, 0.30f); } }

        /// <summary>
        /// Bir metni belge katmanına alır: kayıt defteri, final raporu, etiketler ve
        /// tarih/konum satırı daktilo yazı tipiyle çizilir. Ayrım oyunun kendi kurgusundan
        /// gelir; anlatılan şey serif, kayda geçen şey daktilodur.
        /// </summary>
        private TMP_Text AsDocument(TMP_Text text)
        {
            if (text != null && theme != null && theme.monoFont != null) text.font = theme.monoFont;
            return text;
        }

        /// <summary>Başlıkları küçük, harf aralıklı ve damga renginde veren ortak sarmalayıcı.</summary>
        private string LabelMarkup(string text)
        {
            return "<color=#" + ColorUtility.ToHtmlStringRGB(ArchiveLabel)
                 + "><size=84%><cspace=0.14em><b>" + text + "</b></cspace></size></color>";
        }

        private const float EchoVisibleBodyTop = 0.806f;
        private const float EchoHiddenBodyTop = 0.965f;

        /// <summary>
        /// Seçim düğmesi: iki katmanlı nötr çerçeve, üstte küçük tuş etiketi, altında eylem
        /// metni. Çerçeve artık <c>buttonPanel</c> sprite'ından gelmiyor; o dokunun kenarına
        /// pas rengi gömülü olduğu için düğmeler kırmızı çerçeveli görünüyordu. Dış katman
        /// sabit kalır, iç dolgu düğmenin hedef grafiğidir; böylece üzerine gelme ve seçim
        /// vurguları çerçeveyi değil yalnızca dolguyu değiştirir.
        /// </summary>
        private Button CreateChoiceButton(string name, Transform parent, string keyHint, UnityEngine.Events.UnityAction action,
            Vector2 anchorMin, Vector2 anchorMax, out TMP_Text keyLabel, out TMP_Text bodyLabel)
        {
            GameObject buttonObject = CreateRect(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            AddImage(buttonObject, new Color(theme.ink.r, theme.ink.g, theme.ink.b, 0.55f));

            GameObject fillObject = CreateRect("Fill", buttonObject.transform, Vector2.zero, Vector2.one,
                new Vector2(2f, 2f), new Vector2(-2f, -2f));
            Image fill = AddImage(fillObject, Color.white);
            fill.raycastTarget = false;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = fill;
            ColorBlock colors = button.colors;
            // Dolgu doğrudan bu renklerle çizilir (sprite yok, çarpım yok). Alfa 1'in altında
            // tutulur ki arkadaki illüstrasyon kutunun içinden de bir parça okunsun.
            //
            // Üzerine gelme rengi kasten hafif tutulur. Dolu petrol mavisi, kutuyu "seçilmiş
            // cevap" gibi gösteriyordu: fareyle karar verildikten sonra imleç aynı yerde
            // kaldığı için bir sonraki soruda da o taraf işaretli görünüyordu.
            Color hover = Color.Lerp(theme.sootNavy, theme.petrol, 0.34f);
            colors.normalColor = new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.88f);
            colors.highlightedColor = new Color(hover.r, hover.g, hover.b, 0.94f);
            colors.pressedColor = new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.98f);
            colors.selectedColor = new Color(hover.r, hover.g, hover.b, 0.94f);
            colors.disabledColor = new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.35f);
            colors.fadeDuration = 0.16f;
            button.colors = colors;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Automatic;
            button.navigation = navigation;
            if (action != null) button.onClick.AddListener(action);

            keyLabel = CreateText("Key", buttonObject.transform, keyHint, 19f, FontStyles.Bold, theme.mustard,
                new Vector2(0f, 0.60f), new Vector2(1f, 0.93f), new Vector2(22f, 0f), new Vector2(-18f, 0f), TextAlignmentOptions.TopLeft);
            keyLabel.characterSpacing = 3f;

            bodyLabel = CreateText("Label", buttonObject.transform, string.Empty, 29f, FontStyles.Bold, theme.agedPaper,
                new Vector2(0f, 0.07f), new Vector2(1f, 0.60f), new Vector2(22f, 0f), new Vector2(-18f, 0f), TextAlignmentOptions.TopLeft);
            bodyLabel.enableAutoSizing = true;
            bodyLabel.fontSizeMin = 21f;
            bodyLabel.fontSizeMax = 29f;
            return button;
        }

        private void BuildSettings(Transform parent)
        {
            GameObject screen = CreateScreen("Settings", parent);
            router.Register(AppScreen.Settings, screen);
            GameObject panel = CreatePaperPanel("Settings Panel", screen.transform, new Vector2(0.22f, 0.08f), new Vector2(0.78f, 0.92f));
            CreateText("Title", panel.transform, T(UiKey.SettingsTitle), 48f, FontStyles.Bold, theme.ink,
                new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.95f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            masterSlider = CreateSliderRow(panel.transform, T(UiKey.SettingsMasterVolume), 0.745f, value => { settings.masterVolume = value; SaveAndApplySettings(); });
            ambientSlider = CreateSliderRow(panel.transform, T(UiKey.SettingsAmbientVolume), 0.655f, value => { settings.ambientVolume = value; SaveAndApplySettings(); });
            effectsSlider = CreateSliderRow(panel.transform, T(UiKey.SettingsEffectsVolume), 0.565f, value => { settings.effectsVolume = value; SaveAndApplySettings(); });

            // Dil satırı en üstte durur: oyuncu oyunu anlamadığı bir dilde açtıysa ilk aradığı
            // ayar budur ve diğer etiketleri okumadan bulabilmelidir.
            languageValueText = CreateSettingsToggleRow(panel.transform, T(UiKey.SettingsLanguage), 0.455f, "Language Toggle", ToggleLanguage);
            fullscreenValueText = CreateSettingsToggleRow(panel.transform, T(UiKey.SettingsFullscreen), 0.355f, "Fullscreen Toggle", ToggleFullscreen);
            textSizeValueText = CreateSettingsToggleRow(panel.transform, T(UiKey.SettingsTextSize), 0.255f, "Text Size Toggle", ToggleTextSize);
            motionValueText = CreateSettingsToggleRow(panel.transform, T(UiKey.SettingsReduceMotion), 0.155f, "Motion Toggle", ToggleMotion);

            CreateButton("Back", panel.transform, T(UiKey.SettingsBack), CloseSettings, new Vector2(0.08f, 0.035f), new Vector2(0.35f, 0.11f));
        }

        private TMP_Text CreateSettingsToggleRow(Transform panel, string label, float centerY, string buttonName, UnityEngine.Events.UnityAction action)
        {
            CreateText(label + " Label", panel, label, 22f, FontStyles.Bold, theme.ink,
                new Vector2(0.08f, centerY - 0.03f), new Vector2(0.43f, centerY + 0.03f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            Button button = CreateButton(buttonName, panel, string.Empty, action,
                new Vector2(0.55f, centerY - 0.038f), new Vector2(0.91f, centerY + 0.038f));
            return button.GetComponentInChildren<TMP_Text>();
        }

        /// <summary>
        /// Antolojinin bölüm seçimi: bir Avrupa haritası. Her oynanabilir bölüm, geçtiği
        /// yerin gerçek enlem/boylamında bir işaret olarak durur; işaret seçildiğinde sağdaki
        /// arşiv panosu bölümün görselini ve tanıtımını gösterir, oradan başlatılır.
        /// <para>
        /// Kartlar yerine harita seçildi, çünkü antolojinin iddiası tek bir savaşın farklı
        /// yerlerdeki sıradan insanlarını anlatmak; yerin kendisi bu iddianın görünür hâli.
        /// Hazırlanmakta olan bölümlerin coğrafyası olmadığı için haritada yerleri yoktur;
        /// uydurma bir işaret yanlış beklenti yaratırdı.
        /// </para>
        /// </summary>
        private void BuildStorySelect(Transform parent)
        {
            GameObject screen = CreateScreen("Story Select", parent);
            router.Register(AppScreen.StorySelect, screen);
            AddImage(CreateRect("Dim", screen.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero),
                new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.94f)).raycastTarget = false;

            TMP_Text title = CreateText("Title", screen.transform, T(UiKey.StorySelectTitle), 44f, FontStyles.Bold, theme.agedPaper,
                new Vector2(0.05f, 0.885f), new Vector2(0.6f, 0.96f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            title.characterSpacing = 2f;
            AddImage(CreateRect("Rule", screen.transform, new Vector2(0.05f, 0.875f), new Vector2(0.17f, 0.882f), Vector2.zero, Vector2.zero),
                theme.rust).raycastTarget = false;

            // Harita alanı: doku kendi en/boy oranında bu alana sığdırılır; işaretler haritanın
            // kendi dikdörtgenine çapalanır ki projeksiyon birebir tutsun.
            GameObject mapArea = CreateRect("Map Area", screen.transform, new Vector2(0.05f, 0.11f), new Vector2(0.66f, 0.86f), Vector2.zero, Vector2.zero);
            GameObject mapObject = CreateRect("Map", mapArea.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image mapImage = AddImage(mapObject, Color.white, theme.europeMap);
            mapImage.raycastTarget = false;
            mapImage.preserveAspect = true;
            AspectRatioFitter fitter = mapObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = theme.europeMap != null && theme.europeMap.rect.height > 0f
                ? theme.europeMap.rect.width / theme.europeMap.rect.height
                : MapProjection.Aspect;
            // Harita çerçevesi: dört ince şerit. Dolu bir dikdörtgen çocuk olarak haritanın
            // üstüne çizilir ve onu örterdi.
            Color frame = new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.45f);
            AddImage(CreateRect("Frame Top", mapObject.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(-3f, 0f), new Vector2(3f, 3f)), frame).raycastTarget = false;
            AddImage(CreateRect("Frame Bottom", mapObject.transform, Vector2.zero, new Vector2(1f, 0f), new Vector2(-3f, -3f), new Vector2(3f, 0f)), frame).raycastTarget = false;
            AddImage(CreateRect("Frame Left", mapObject.transform, Vector2.zero, new Vector2(0f, 1f), new Vector2(-3f, 0f), Vector2.zero), frame).raycastTarget = false;
            AddImage(CreateRect("Frame Right", mapObject.transform, new Vector2(1f, 0f), Vector2.one, Vector2.zero, new Vector2(3f, 0f)), frame).raycastTarget = false;

            // Sağdaki arşiv panosu: seçili bölümün görseli ve tanıtımı.
            GameObject panel = CreatePaperPanel("Story Panel", screen.transform, new Vector2(0.70f, 0.11f), new Vector2(0.95f, 0.86f));
            GameObject artObject = CreateRect("Art", panel.transform, new Vector2(0.06f, 0.60f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero);
            artObject.AddComponent<RectMask2D>();
            GameObject artInner = CreateRect("Art Image", artObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            storySelectArt = AddImage(artInner, Color.white);
            storySelectArt.raycastTarget = false;
            AspectRatioFitter artFitter = artInner.AddComponent<AspectRatioFitter>();
            artFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            artFitter.aspectRatio = 16f / 9f;
            AddImage(CreateRect("Art Scrim", artObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero),
                new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.22f)).raycastTarget = false;

            storySelectPeriod = (TMP_Text)AsDocument(CreateText("Period", panel.transform, string.Empty, 16f, FontStyles.Bold, ArchiveLabel,
                new Vector2(0.08f, 0.53f), new Vector2(0.92f, 0.585f), Vector2.zero, Vector2.zero, TextAlignmentOptions.BottomLeft));
            storySelectPeriod.characterSpacing = 3f;
            storySelectTitle = CreateText("Story Title", panel.transform, string.Empty, 40f, FontStyles.Bold, theme.ink,
                new Vector2(0.08f, 0.43f), new Vector2(0.92f, 0.53f), Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            storySelectTitle.enableAutoSizing = true;
            storySelectTitle.fontSizeMin = 28f;
            storySelectTitle.fontSizeMax = 40f;
            storySelectLine = CreateText("Story Line", panel.transform, string.Empty, 21f, FontStyles.Normal, theme.ink,
                new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.43f), Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            storySelectLine.enableAutoSizing = true;
            storySelectLine.fontSizeMin = 16f;
            storySelectLine.fontSizeMax = 21f;
            storySelectLine.lineSpacing = 4f;
            storySelectBegin = CreateButton("Begin", panel.transform, T(UiKey.StorySelectBegin), BeginSelectedStory,
                new Vector2(0.08f, 0.09f), new Vector2(0.92f, 0.185f));

            // İşaretler.
            storyMarkers.Clear();
            storySelectFirstSelection = null;
            StoryCatalogEntry[] entries = catalog == null ? Array.Empty<StoryCatalogEntry>() : catalog.entries;
            StoryCatalogEntry firstPlayable = null;
            for (int i = 0; i < entries.Length; i++)
            {
                StoryCatalogEntry entry = entries[i];
                if (entry == null || !entry.IsPlayable || !entry.HasLocation) continue;
                if (!MapProjection.IsInside(entry.latitude, entry.longitude))
                {
                    Debug.LogWarning("Bölüm haritanın dışında, işaretlenmedi: " + entry.storyId);
                    continue;
                }
                Button marker = BuildStoryMarker(mapObject.transform, entry, LabelGoesLeft(entry, entries));
                storyMarkers.Add(new StoryMarker { entry = entry, button = marker });
                if (firstPlayable == null)
                {
                    firstPlayable = entry;
                    storySelectFirstSelection = marker.gameObject;
                }
            }

            CreateButton("Back", screen.transform, T(UiKey.CommonMainMenu), () => ShowMainMenu(true),
                new Vector2(0.05f, 0.025f), new Vector2(0.24f, 0.095f));
            TMP_Text intro = CreateText("Intro", screen.transform, T(UiKey.StorySelectIntro), 17f, FontStyles.Normal,
                new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.62f),
                new Vector2(0.27f, 0.025f), new Vector2(0.66f, 0.095f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            intro.enableAutoSizing = true;
            intro.fontSizeMin = 13f;
            intro.fontSizeMax = 17f;

            SelectStory(firstPlayable);
        }

        /// <summary>
        /// Harita işareti: pas dolgulu bir nokta, ince bir halka ve yanında bölüm adıyla tarih.
        /// Çapa noktası projeksiyondan gelir; boyutlar pikseldir ki harita ölçeklenirken
        /// işaret büyümesin.
        /// </summary>
        private Button BuildStoryMarker(Transform mapTransform, StoryCatalogEntry entry, bool labelLeft)
        {
            Vector2 uv = MapProjection.Project(entry.latitude, entry.longitude);
            GameObject anchor = CreateRect("Marker " + entry.storyId, mapTransform, uv, uv, new Vector2(-22f, -22f), new Vector2(22f, 22f));

            // Tıklanabilir alan işaretten geniştir: 44 px, parmakla ve fareyle rahat.
            Image hit = AddImage(anchor, new Color(0f, 0f, 0f, 0f));
            hit.raycastTarget = true;
            Button button = anchor.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Automatic;
            button.navigation = navigation;
            button.onClick.AddListener(() => { audioManager.PlayConfirm(); SelectStory(entry); });

            AddImage(CreateRect("Ring", anchor.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-13f, -13f), new Vector2(13f, 13f)),
                new Color(theme.ink.r, theme.ink.g, theme.ink.b, 0.85f), theme.mapMarker).raycastTarget = false;
            AddImage(CreateRect("Ring Inner", anchor.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-10f, -10f), new Vector2(10f, 10f)),
                theme.agedPaper, theme.mapMarker).raycastTarget = false;
            AddImage(CreateRect("Dot", anchor.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-6f, -6f), new Vector2(6f, 6f)),
                theme.rust, theme.mapMarker).raycastTarget = false;

            // Etiket: ad serif, tarih daktilo. Noktanın sağına, biraz yukarıya. Etiket
            // kutuları tek satırdan yüksek tutulur; üç nokta kipinde satır sığmazsa TMP
            // metni tümüyle düşürür.
            // Sağında başka bir işaret varsa etiket sola alınır; iki bölüm adı üst üste
            // binmesin diye.
            Vector2 side = labelLeft ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f);
            Vector2 nameMin = labelLeft ? new Vector2(-246f, -4f) : new Vector2(6f, -4f);
            Vector2 nameMax = labelLeft ? new Vector2(-6f, 34f) : new Vector2(246f, 34f);
            Vector2 periodMin = labelLeft ? new Vector2(-246f, -28f) : new Vector2(7f, -28f);
            Vector2 periodMax = labelLeft ? new Vector2(-7f, -4f) : new Vector2(246f, -4f);
            TMP_Text name = CreateText("Name", anchor.transform, entry.title, 22f, FontStyles.Bold, theme.ink,
                side, side, nameMin, nameMax, labelLeft ? TextAlignmentOptions.BottomRight : TextAlignmentOptions.BottomLeft);
            name.overflowMode = TextOverflowModes.Overflow;
            name.enableWordWrapping = false;
            name.raycastTarget = false;
            TMP_Text period = (TMP_Text)AsDocument(CreateText("Period", anchor.transform,
                localization == null ? entry.period : localization.ToUpper(entry.period), 13f, FontStyles.Bold, ArchiveLabel,
                side, side, periodMin, periodMax, labelLeft ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft));
            period.characterSpacing = 2f;
            period.overflowMode = TextOverflowModes.Overflow;
            period.enableWordWrapping = false;
            period.raycastTarget = false;
            return button;
        }

        /// <summary>
        /// Etiket sağa yazıldığında başka bir işaretin üstüne düşer mi? Harita boyutu yerleşimden
        /// önce bilinmediği için ölçü projeksiyon birimindedir: etiket genişliği haritanın
        /// yaklaşık dörtte biri, yüksekliği yaklaşık yirmide biri.
        /// </summary>
        private static bool LabelGoesLeft(StoryCatalogEntry entry, StoryCatalogEntry[] entries)
        {
            Vector2 self = MapProjection.Project(entry.latitude, entry.longitude);
            for (int i = 0; i < entries.Length; i++)
            {
                StoryCatalogEntry other = entries[i];
                if (other == null || other == entry || !other.IsPlayable || !other.HasLocation) continue;
                Vector2 uv = MapProjection.Project(other.latitude, other.longitude);
                if (uv.x > self.x && uv.x - self.x < 0.28f && Mathf.Abs(uv.y - self.y) < 0.06f) return true;
            }
            return false;
        }

        /// <summary>Seçili bölümü panoya yazar ve işaretleri seçili/seçili değil olarak boyar.</summary>
        private void SelectStory(StoryCatalogEntry entry)
        {
            selectedStory = entry;
            bool has = entry != null;
            if (storySelectBegin != null) storySelectBegin.interactable = has;
            if (storySelectTitle != null) storySelectTitle.text = has ? entry.title : string.Empty;
            if (storySelectPeriod != null) storySelectPeriod.text = has ? (localization == null ? entry.period : localization.ToUpper(entry.period)) : string.Empty;
            if (storySelectLine != null) storySelectLine.text = has ? entry.line : string.Empty;
            if (storySelectArt != null)
            {
                Sprite art;
                bool found = has && !string.IsNullOrEmpty(entry.imageKey) && artIndex.TryGetValue(entry.imageKey, out art);
                storySelectArt.sprite = found ? artIndex[entry.imageKey] : null;
                storySelectArt.color = found ? Color.white : new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.9f);
            }
            for (int i = 0; i < storyMarkers.Count; i++)
            {
                bool selected = storyMarkers[i].entry == entry;
                Transform t = storyMarkers[i].button.transform;
                Image dot = t.Find("Dot").GetComponent<Image>();
                Image ring = t.Find("Ring").GetComponent<Image>();
                dot.color = selected ? theme.rust : new Color(theme.rust.r, theme.rust.g, theme.rust.b, 0.55f);
                ring.color = selected ? theme.rust : new Color(theme.ink.r, theme.ink.g, theme.ink.b, 0.6f);
                t.Find("Name").GetComponent<TMP_Text>().color = selected ? theme.ink : new Color(theme.ink.r, theme.ink.g, theme.ink.b, 0.62f);
            }
        }

        private void BeginSelectedStory()
        {
            if (selectedStory == null) return;
            RequestNewGame(selectedStory.storyId);
        }

        private sealed class StoryMarker
        {
            public StoryCatalogEntry entry;
            public Button button;
        }

        private void BuildPause(Transform parent)
        {
            GameObject screen = CreateScreen("Pause", parent);
            router.Register(AppScreen.Pause, screen);
            AddImage(CreateRect("Dim", screen.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.9f));
            GameObject panel = CreatePaperPanel("Pause Panel", screen.transform, new Vector2(0.35f, 0.2f), new Vector2(0.65f, 0.8f));
            CreateText("Title", panel.transform, T(UiKey.PauseTitle), 44f, FontStyles.Bold, theme.ink,
                new Vector2(0.1f, 0.76f), new Vector2(0.9f, 0.9f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            CreateButton("Resume", panel.transform, T(UiKey.PauseResume), ResumeGame, new Vector2(0.14f, 0.62f), new Vector2(0.86f, 0.73f));
            CreateButton("Journal", panel.transform, T(UiKey.PauseJournal), OpenJournal, new Vector2(0.14f, 0.475f), new Vector2(0.86f, 0.585f));
            CreateButton("Settings", panel.transform, T(UiKey.MenuSettings), () => OpenSettings(AppScreen.Pause), new Vector2(0.14f, 0.33f), new Vector2(0.86f, 0.44f));
            CreateButton("Main Menu", panel.transform, T(UiKey.CommonMainMenu), () => ShowMainMenu(true), new Vector2(0.14f, 0.185f), new Vector2(0.86f, 0.295f));
        }

        /// <summary>
        /// Kayıt defteri. Bir rota 16 karardır ve gecikmeli yankılar oyuncunun saatler önce
        /// verdiği bir karara gönderme yapar; araya bir gün girdiğinde o bağ kopuyordu.
        /// Defter, final raporundaki izlerin aynısını oyunun ortasında da okunur kılar.
        /// Yeni bir kurgu değil: Milena'nın hikâyede zaten tuttuğu defterin karşılığıdır.
        /// </summary>
        private void BuildJournal(Transform parent)
        {
            GameObject screen = CreateScreen("Journal", parent);
            router.Register(AppScreen.Journal, screen);
            AddImage(CreateRect("Dim", screen.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero),
                new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.92f));
            GameObject panel = CreatePaperPanel("Journal Panel", screen.transform, new Vector2(0.18f, 0.07f), new Vector2(0.82f, 0.93f));
            CreateText("Title", panel.transform, T(UiKey.JournalTitle), 42f, FontStyles.Bold, theme.ink,
                new Vector2(0.06f, 0.87f), new Vector2(0.94f, 0.95f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            AddImage(CreateRect("Rule", panel.transform, new Vector2(0.06f, 0.855f), new Vector2(0.94f, 0.862f), Vector2.zero, Vector2.zero),
                theme.rust).raycastTarget = false;

            journalText = (TMP_Text)AsDocument(CreateText("Journal Body", panel.transform, string.Empty, 22f, FontStyles.Normal, theme.ink,
                new Vector2(0.06f, 0.17f), new Vector2(0.94f, 0.84f), Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft));
            journalText.enableAutoSizing = true;
            journalText.fontSizeMin = 15f;
            journalText.fontSizeMax = 22f;
            journalText.lineSpacing = 6f;

            CreateButton("Back", panel.transform, T(UiKey.SettingsBack), CloseJournal,
                new Vector2(0.06f, 0.045f), new Vector2(0.40f, 0.13f));
        }

        private void OpenJournal()
        {
            journalText.text = FormatJournal();
            router.Show(AppScreen.Journal);
            SelectFirstButton(router.Get(AppScreen.Journal));
        }

        private void CloseJournal()
        {
            audioManager.PlayBack();
            router.Show(AppScreen.Pause);
            SelectFirstButton(router.Get(AppScreen.Pause));
        }

        /// <summary>
        /// Defterin gövdesi: o ana kadar verilmiş kararlar, bölüm başlıklarıyla. Final
        /// raporuyla aynı veriden okunur, çünkü ikisi de aynı şeyi anlatır.
        /// </summary>
        private string FormatJournal()
        {
            GameState state = storyController == null ? null : storyController.State;
            TraceEntry[] entries = state == null ? null : state.traces;
            string previous = storyController == null ? null : storyController.PreviousChoiceText(storyController.CurrentNode);
            if ((entries == null || entries.Length == 0) && string.IsNullOrEmpty(previous)) return T(UiKey.JournalEmpty);

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            // Önceki oynanış izi en üstte durur: oyuncu bu düğümde geçen sefer ne yaptığını,
            // bu seferki kararını vermeden önce görür. İpucu değil, kayıt; hangi seçeneğin
            // "iyi" olduğuna dair hiçbir işaret taşımaz.
            if (!string.IsNullOrEmpty(previous))
            {
                builder.Append(LabelMarkup(localization == null ? T(UiKey.JournalPreviousRun) : localization.ToUpper(T(UiKey.JournalPreviousRun))))
                       .Append("\n").Append(previous).Append("\n");
            }
            if (entries == null || entries.Length == 0) return builder.ToString().TrimEnd('\n');
            string currentAct = null;
            for (int i = 0; i < entries.Length; i++)
            {
                TraceEntry entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.text)) continue;
                string act = entry.act ?? string.Empty;
                if (act.Length > 0 && act != currentAct)
                {
                    currentAct = act;
                    if (builder.Length > 0) builder.Append('\n');
                    builder.Append('\n').Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(ArchiveLabel))
                           .Append("><size=82%><cspace=0.12em><b>")
                           .Append(localization == null ? act : localization.ToUpper(act))
                           .Append("</b></cspace></size></color>\n");
                }
                builder.Append("\n• ").Append(entry.text);
            }
            return builder.ToString().TrimStart('\n');
        }

        private void BuildEnding(Transform parent)
        {
            GameObject screen = CreateScreen("Ending", parent);
            router.Register(AppScreen.Ending, screen);
            GameObject panel = CreatePaperPanel("Ending Panel", screen.transform, new Vector2(0.12f, 0.06f), new Vector2(0.88f, 0.94f));
            endingTitleText = CreateText("Ending Title", panel.transform, string.Empty, 50f, FontStyles.Bold, theme.ink,
                new Vector2(0.07f, 0.82f), new Vector2(0.93f, 0.94f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            // Rapor artık üç bölümü de kapsadığı için sol sütuna alındı; final paragrafları
            // sağda kalır. Tek sütunda alt alta dizmek raporu ya kırpıyor ya da okunmayacak
            // kadar küçültüyordu.
            endingBodyText = CreateText("Ending Body", panel.transform, string.Empty, 25f, FontStyles.Normal, theme.ink,
                new Vector2(0.07f, 0.15f), new Vector2(0.505f, 0.79f), Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            endingBodyText.enableAutoSizing = true;
            endingBodyText.fontSizeMin = 17f;
            endingBodyText.fontSizeMax = 25f;
            endingBodyText.lineSpacing = 7f;

            AddImage(CreateRect("Report Rule", panel.transform, new Vector2(0.527f, 0.15f), new Vector2(0.5305f, 0.79f), Vector2.zero, Vector2.zero),
                new Color(theme.ink.r, theme.ink.g, theme.ink.b, 0.22f)).raycastTarget = false;

            endingTracesText = (TMP_Text)AsDocument(CreateText("Traces", panel.transform, string.Empty, 20f, FontStyles.Normal, ArchiveInk,
                new Vector2(0.555f, 0.15f), new Vector2(0.93f, 0.79f), Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft));
            endingTracesText.enableAutoSizing = true;
            endingTracesText.fontSizeMin = 15f;
            endingTracesText.fontSizeMax = 20f;
            endingTracesText.lineSpacing = 4f;
            CreateButton("Replay", panel.transform, T(UiKey.EndingReplay), RequestNewGame,
                new Vector2(0.07f, 0.035f), new Vector2(0.43f, 0.12f));
            CreateButton("Main Menu", panel.transform, T(UiKey.CommonMainMenu), () => ShowMainMenu(true),
                new Vector2(0.57f, 0.035f), new Vector2(0.93f, 0.12f));
        }

        private void BuildError(Transform parent)
        {
            GameObject screen = CreateScreen("Error", parent);
            router.Register(AppScreen.Error, screen);
            AddImage(CreateRect("Dim", screen.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), new Color(0f, 0f, 0f, 0.78f));
            GameObject panel = CreatePaperPanel("Error Panel", screen.transform, new Vector2(0.27f, 0.25f), new Vector2(0.73f, 0.75f));
            CreateText("Title", panel.transform, T(UiKey.ErrorTitle), 38f, FontStyles.Bold, theme.rust,
                new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.88f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            errorText = CreateText("Message", panel.transform, string.Empty, 25f, FontStyles.Normal, theme.ink,
                new Vector2(0.1f, 0.30f), new Vector2(0.9f, 0.70f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            errorText.enableAutoSizing = true;
            errorText.fontSizeMin = 18f;
            errorText.fontSizeMax = 25f;
            CreateButton("New Game", panel.transform, T(UiKey.MenuNewGame), RequestNewGame,
                new Vector2(0.1f, 0.10f), new Vector2(0.46f, 0.24f));
            CreateButton("Main Menu", panel.transform, T(UiKey.CommonMainMenu), () => ShowMainMenu(true),
                new Vector2(0.54f, 0.10f), new Vector2(0.9f, 0.24f));
        }

        private void ShowStorySelect()
        {
            audioManager.PlayConfirm();
            router.Show(AppScreen.StorySelect);
            if (storySelectFirstSelection != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(storySelectFirstSelection);
            else SelectFirstButton(router.Get(AppScreen.StorySelect));
        }

        /// <summary>Final ekranındaki "Yeniden Oyna" o anki bölümü baştan başlatır.</summary>
        private void RequestNewGame()
        {
            RequestNewGame(activeStoryId);
        }

        private void RequestNewGame(string storyId)
        {
            if (storyController == null)
            {
                ShowError(initializationError ?? T(UiKey.ErrorStoryUnavailable));
                return;
            }
            audioManager.PlayConfirm();

            string requested = StoryRepository.SanitizeStoryId(storyId);
            if (requested != activeStoryId)
            {
                string previous = activeStoryId;
                try
                {
                    activeStoryId = requested;
                    LoadStoryForLocale(settings.locale);
                }
                catch (Exception exception)
                {
                    activeStoryId = previous;
                    Debug.LogError(exception);
                    ShowError(T(UiKey.ErrorStoryUnavailable) + "\n\n" + exception.Message);
                    return;
                }
            }
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
                ShowError(T(UiKey.ErrorNewGameFailed) + "\n\n" + exception.Message);
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
            string messageKey;
            if (!storyController.TryContinue(out messageKey))
            {
                ShowError(T(messageKey));
                return;
            }
            audioManager.PlayConfirm();
            RenderNode(storyController.CurrentNode, true);
        }

        private void SelectChoice(int index)
        {
            if (transitionBusy || storyController == null || storyController.CurrentNode == null) return;
            if (index < 0 || index > 1 || choiceButtons[index] == null || !choiceButtons[index].interactable) return;
            // Karar sahnesi bekleyen düğümde hangi tuşa basıldığı fark etmez: seçimi sahne verir.
            if (ShouldPlayChoosingInterlude(storyController.CurrentNode))
            {
                StartCoroutine(RunChoosingInterlude(storyController.CurrentNode));
                return;
            }
            StartCoroutine(AdvanceChoice(index));
        }

        private IEnumerator AdvanceChoice(int index)
        {
            transitionBusy = true;
            // Fareyle seçim yapıldığında EventSystem düğmeyi seçili tutuyor ve seçim rengi
            // sonraki düğümde de duruyordu: yeni karar geldiğinde bir önceki seçimin tarafı
            // hâlâ mavi görünüyor, oyuncuya olmayan bir "işaretli seçenek" gösteriyordu.
            // Oynanışta A/D ve yön tuşları doğrudan seçim yaptığı için burada odak
            // tutulmasına gerek yoktur.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
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
                ShowError(T(UiKey.ErrorChoiceFailed) + "\n\n" + exception.Message);
                yield break;
            }

            // Ara sahne metinden önce oynanır; ürettiği bayraklar bu düğümün yankılarını
            // etkileyebildiği için yankılar sahneden sonra tüketilir.
            if (ShouldPlayInterlude(outcome.destination)) yield return RunInterlude(outcome.destination);

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
            if (ShouldPlayInterlude(node))
            {
                if (interludeRoutine != null) StopCoroutine(interludeRoutine);
                interludeRoutine = StartCoroutine(RunInterludeThenRender(node, animate));
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
            chapterText.text = localization == null
                ? (node.act ?? string.Empty)
                : localization.ToUpper(node.act ?? string.Empty);
            dateLocationText.text = (node.date ?? string.Empty) + "   ·   " + (node.location ?? string.Empty);
            storyBodyText.text = node.body ?? string.Empty;
            string[] echoLines = storyController.ConsumeEchoes(node);
            bool hasEcho = echoLines.Length > 0;
            echoText.text = hasEcho ? ComposeEchoBlock(echoLines) : string.Empty;
            echoText.gameObject.SetActive(hasEcho);
            if (echoRule != null) echoRule.gameObject.SetActive(hasEcho);
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
                bool available = choice != null && ConditionEvaluator.EvaluateAll(choice.conditions, storyController.State, archiveService);
                choiceButtons[i].interactable = available;
                choiceLabels[i].text = choice == null ? T(UiKey.GameplayNoChoice) : choice.text;
                if (choiceKeyLabels[i] != null) choiceKeyLabels[i].gameObject.SetActive(choice != null);
            }
            RefreshChoiceKeyLabels(node);
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
            endingTracesText.text = FormatEndingReport(storyController.BuildEndingTraces(ending)) + FormatOmissionsReport() + FormatPeopleReport();
            router.Show(AppScreen.Ending);
            SelectFirstButton(router.Get(AppScreen.Ending));
        }

        /// <summary>
        /// "Devam Et" düğmesinin durumu yalnız ShowMainMenu içinde ayarlanıyordu. Arayüz
        /// yeniden kurulduğunda (dil değişimi) düğme sıfırdan yaratılıp varsayılan olarak
        /// etkin kalıyor, Ayarlar'dan geri dönüldüğünde de kayıt yokken tıklanabilir
        /// görünüyordu. Bu yüzden durum ayrı bir yerden uygulanır.
        /// </summary>
        private void RefreshContinueButton()
        {
            if (continueButton == null) return;
            continueButton.interactable = saveService != null && saveService.HasSave && string.IsNullOrEmpty(initializationError);
            ColorBlock colors = continueButton.colors;
            colors.disabledColor = new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.38f);
            continueButton.colors = colors;
        }

        /// <summary>
        /// Final raporunu bölüm başlıklarıyla biçimlendirir. Başlıklar hikâye verisinden
        /// geldiği için ayrıca çevrilmez; oyuncunun seçtiği dilin hikâye dosyasında zaten
        /// o dilde yazılıdır.
        /// </summary>
        /// <summary>
        /// Final raporunun "İnsanlar" bölümü. Oyunun sözü "tarih değişmez, insanların kaderi
        /// değişebilir" olduğu hâlde ilişki değerleri bugüne kadar hiçbir yere çıkmıyordu:
        /// iki hikâyede toplam 135 ilişki etkisi birikiyor ve hepsi kayıtta kalıyordu.
        /// <para>
        /// Bilinçli olarak sayı ya da çubuk gösterilmez; durum çubukları oyundan kaldırılmıştı
        /// ve ilişkiyi puana çevirmek aynı hatayı geri getirirdi. Yalnız belirgin biçimde
        /// kaymış kişiler görünür, cümleleri de hikâye dosyasında elle yazılmıştır.
        /// </para>
        /// </summary>
        private string FormatPeopleReport()
        {
            if (storyController == null || storyController.Story == null || storyController.State == null) return string.Empty;
            CharacterData[] characters = storyController.Story.characters;
            if (characters == null || characters.Length == 0) return string.Empty;

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < characters.Length; i++)
            {
                CharacterData character = characters[i];
                if (character == null || !character.IsComplete) continue;
                int value = storyController.State.GetRelation(character.key);
                if (value > -CharacterData.Threshold && value < CharacterData.Threshold) continue;
                builder.Append("\n• ").Append(character.name).Append(" — ")
                       .Append(value >= CharacterData.Threshold ? character.warm : character.cold);
            }
            if (builder.Length == 0) return string.Empty;
            return "\n\n" + LabelMarkup(T(UiKey.EndingPeopleTitle)) + builder;
        }

        /// <summary>
        /// "Yapılmayanlar": rota boyunca bilinçle alınmamış ve ağırlığı olan seçenekler.
        /// Rapor yalnız yapılanların değil, bırakılanların da kaydı olur; oyunun bedel
        /// vurgusu buradan gelir. Hikâye dosyasında <c>omission</c> yazılmamış seçenekler
        /// hiç görünmez, yani liste kısa ve seçilmiş kalır.
        /// </summary>
        private string FormatOmissionsReport()
        {
            if (storyController == null) return string.Empty;
            string[] omissions = storyController.BuildOmissions();
            if (omissions == null || omissions.Length == 0) return string.Empty;
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("\n\n").Append(LabelMarkup(T(UiKey.EndingOmissionsTitle)));
            int shown = 0;
            for (int i = 0; i < omissions.Length && shown < 4; i++)
            {
                if (string.IsNullOrWhiteSpace(omissions[i])) continue;
                builder.Append("\n• ").Append(omissions[i]);
                shown++;
            }
            return builder.ToString();
        }

        private string FormatEndingReport(TraceEntry[] entries)
        {
            if (entries == null || entries.Length == 0)
                return T(UiKey.EndingTracesTitle) + "\n" + T(UiKey.EndingTracesFallback);

            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.Append(LabelMarkup(T(UiKey.EndingTracesTitle)));
            string currentAct = null;
            for (int i = 0; i < entries.Length; i++)
            {
                TraceEntry entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.text)) continue;
                string act = entry.act ?? string.Empty;
                if (act.Length > 0 && act != currentAct)
                {
                    currentAct = act;
                    report.Append("\n\n").Append(LabelMarkup(localization == null ? act : localization.ToUpper(act)));
                }
                report.Append("\n• ").Append(entry.text);
            }
            return report.ToString();
        }

        private void ShowMainMenu(bool playBack)
        {
            transitionBusy = false;
            if (playBack) audioManager.PlayBack();
            SetBackground("harbor_dawn");
            audioManager.PlayAmbienceFor("harbor_dawn");
            RefreshContinueButton();
            router.Show(AppScreen.MainMenu);
            if (firstSelection != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(firstSelection);
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
            if (errorText != null) errorText.text = string.IsNullOrWhiteSpace(message) ? T(UiKey.ErrorGeneric) : message;
            router.Show(AppScreen.Error);
            SelectFirstButton(router.Get(AppScreen.Error));
        }

        private void HandleEscape()
        {
            switch (router.Current)
            {
                case AppScreen.Gameplay: ShowPause(); break;
                case AppScreen.Pause: ResumeGame(); break;
                case AppScreen.Journal: CloseJournal(); break;
                case AppScreen.Settings: CloseSettings(); break;
                case AppScreen.StorySelect: ShowMainMenu(true); break;
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

        /// <summary>
        /// Dili değiştirir, o dilin arayüz tablosunu ve hikâye dosyasını yükler, ardından
        /// arayüzü yeniden kurar. Oyuncunun ilerlemesi korunur; düğüm kimlikleri diller
        /// arasında aynı olduğu için sürmekte olan oyun kaldığı yerden okunur.
        /// </summary>
        private void ToggleLanguage()
        {
            string previous = settings.locale;
            string next = LocalizationService.NextLocale(previous);
            audioManager.PlayConfirm();

            try
            {
                localization.Load(next);
                if (storyController != null) LoadStoryForLocale(next);
            }
            catch (Exception exception)
            {
                // Yeni dil yüklenemezse eskisine geri dönülür; oyuncu metinsiz kalmaz.
                Debug.LogError(exception);
                localization.Load(previous);
                if (storyController != null) LoadStoryForLocale(previous);
                ShowError(T(UiKey.ErrorInitFailed) + "\n\n" + exception.Message);
                return;
            }

            settings.locale = next;
            settingsService.Save(settings);

            string currentNodeId = storyController == null || storyController.State == null ? null : storyController.State.currentNodeId;
            bool wasInGameplay = router.Current == AppScreen.Gameplay;

            RebuildInterfaceForLocale();

            if (wasInGameplay && currentNodeId != null)
            {
                StoryNode node = storyController.CurrentNode;
                if (node != null && !node.IsEnding)
                {
                    RenderNodeContent(node);
                    if (storyCardGroup != null) storyCardGroup.alpha = 1f;
                }
            }
            OpenSettings(settingsReturnScreen);
        }

        /// <summary>
        /// Arayüz metinleri kuruluşta yazıldığı için dil değişiminde ekranlar yeniden üretilir.
        /// Yeniden kurulum, tek tek metin güncellemekten hem daha kısa hem de eksik kalma
        /// riski taşımayan yoldur.
        /// </summary>
        private void RebuildInterfaceForLocale()
        {
            if (cardRoutine != null) { StopCoroutine(cardRoutine); cardRoutine = null; }
            if (introRoutine != null) { StopCoroutine(introRoutine); introRoutine = null; }
            introActive = false;
            transitionBusy = false;
            scalableBodyTexts.Clear();
            storySelectFirstSelection = null;
            BuildInterface();
            ApplyTextScale();
            // Yeni kurulan arka plan Image'ı sprite'sızdır; etkin sahne görseli geri yüklenir.
            SetBackground(currentBackgroundKey);
            RefreshContinueButton();
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
            fullscreenValueText.text = settings.fullscreen ? T(UiKey.CommonOn) : T(UiKey.CommonOff);
            textSizeValueText.text = settings.largeText ? T(UiKey.SettingsTextSizeLarge) : T(UiKey.SettingsTextSizeNormal);
            motionValueText.text = settings.reduceMotion ? T(UiKey.CommonOn) : T(UiKey.CommonOff);
            // Dil adı her zaman kendi dilinde yazılır; oyuncu anlamadığı bir dilde bile
            // hangi seçeneğin ne olduğunu tanıyabilmelidir.
            if (languageValueText != null) languageValueText.text = LocalizationService.DisplayName(settings.locale);
        }

        /// <summary>
        /// Etkin arka plan anahtarı saklanır: arayüz yeniden kurulduğunda (dil değişimi)
        /// aynı görsel geri yüklenebilsin diye. Aksi hâlde yeni oluşturulan Image sprite'sız
        /// kalır ve ekranda düz bir yüzey görünür.
        /// </summary>
        private void SetBackground(string key)
        {
            if (!string.IsNullOrEmpty(key)) currentBackgroundKey = key;
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// QA duman koşusu seçenekleri yalnız Editor ve Development build'de okunur.
        /// Yayın sürümünde bu bayraklar hiç dinlenmez: aksi hâlde dağıtılan oyun, komut
        /// satırından verilen bir yola klasör açıp dosya yazabilir ve kendini otomatik
        /// oynatıp kapatabilirdi. Yerel komut satırına erişimi olan biri zaten program
        /// çalıştırabilir, yani yükseltme değil; ama tüketiciye giden bir üründe gereksiz
        /// bir yüzeydir ve dosya yazan davranış virüs tarayıcılarında da gürültü yaratır.
        /// </summary>
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
                yield return CaptureLanguageSwitchForQa();
                yield return CaptureStorySelectForQa();
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
                // Kayıt defteri ancak birkaç karar verildikten sonra bir şey gösterir;
                // yakalama bu yüzden oynanış çekildikten sonra yapılır.
                OpenJournal();
                yield return null;
                Canvas.ForceUpdateCanvases();
                CaptureInterfaceOffscreen(Path.Combine(commandLineCaptureDirectory, "journal_" + Screen.width + "x" + Screen.height + ".png"));
                yield return null;
                router.Show(AppScreen.Gameplay);
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
                settings.largeText = false;
                ApplyTextScale();
                yield return CaptureEndingForQa();
                yield return CaptureArchiveForQa();
                yield return CaptureInterludeForQa();
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
        /// <summary>
        /// Dil değişimi arayüzü sıfırdan kurduğu için görsel bozulmaya en açık işlemdir.
        /// İkinci dile geçilip ana menü yakalanır, ardından başlangıç diline dönülür.
        /// </summary>
        private IEnumerator CaptureLanguageSwitchForQa()
        {
            ToggleLanguage();
            yield return null;
            ShowMainMenu(false);
            yield return null;
            Canvas.ForceUpdateCanvases();
            CaptureInterfaceOffscreen(Path.Combine(commandLineCaptureDirectory,
                "menu_" + settings.locale + "_" + Screen.width + "x" + Screen.height + ".png"));
            yield return null;

            ToggleLanguage();
            yield return null;
            ShowMainMenu(false);
            yield return null;
            Canvas.ForceUpdateCanvases();
            CaptureInterfaceOffscreen(Path.Combine(commandLineCaptureDirectory,
                "menu_" + settings.locale + "_back_" + Screen.width + "x" + Screen.height + ".png"));
            yield return null;
        }

        private IEnumerator CaptureStorySelectForQa()
        {
            ShowStorySelect();
            yield return null;
            Canvas.ForceUpdateCanvases();
            CaptureInterfaceOffscreen(Path.Combine(commandLineCaptureDirectory,
                "storyselect_" + Screen.width + "x" + Screen.height + ".png"));
            yield return null;
            yield return CaptureEveryChapterForQa();
            ShowMainMenu(false);
            yield return null;
        }

        /// <summary>
        /// Katalogdaki her oynanabilir bölümü sırayla başlatır, iki karar oynar ve yakalar.
        /// Duman koşusu yalnız ilk bölümü başlatıyordu; ikinci bir bölüm eklendiğinde verisi
        /// testlerden geçse bile oyun içinde bir kez bile açılmamış oluyordu. Bölüm geçişi
        /// hikâye deposunu değiştirdiği için asıl sınanan şey de burasıdır.
        /// </summary>
        private IEnumerator CaptureEveryChapterForQa()
        {
            StoryCatalogEntry[] entries = catalog == null ? Array.Empty<StoryCatalogEntry>() : catalog.entries;
            string original = activeStoryId;
            for (int i = 0; i < entries.Length; i++)
            {
                StoryCatalogEntry entry = entries[i];
                if (entry == null || !entry.IsPlayable) continue;

                activeStoryId = StoryRepository.SanitizeStoryId(entry.storyId);
                LoadStoryForLocale(settings.locale);
                StartNewGameForTests();
                yield return null;
                ChooseForTests(0);
                yield return null;
                ChooseForTests(1);
                yield return null;
                Canvas.ForceUpdateCanvases();
                CaptureInterfaceOffscreen(Path.Combine(commandLineCaptureDirectory,
                    "chapter_" + entry.storyId + "_" + Screen.width + "x" + Screen.height + ".png"));
                yield return null;
            }
            activeStoryId = original;
            LoadStoryForLocale(settings.locale);
            yield return null;
        }

        /// <summary>
        /// Final raporunu yakalar. Rapor oyunun asıl kapanış ekranı olduğu için, bir rotayı
        /// sonuna kadar oynayıp gerçek izlerle üretilmiş hâlini görmek gerekir.
        /// </summary>
        /// <summary>
        /// Arşiv mekaniklerini yakalar. Bir önceki adım Hamburg'u ilk seçeneklerle tamamladı,
        /// yani arşivde artık tamamlanmış bir bölüm var. Bu adım (1) Neretva'yı başlatıp ilk
        /// düğümde Hamburg'a bağlı kesişmenin göründüğünü, (2) Hamburg'u yeniden başlatıp
        /// defterde önceki oynanış izinin yazıldığını kaydeder.
        /// </summary>
        /// <summary>
        /// Her bölümün her ara sahnesini yakalar: bölümü başlatır, grafta hedef düğüme giden
        /// en kısa seçim yolunu bulup o yolu oynar, sahneyi zorlanmış girdiyle birkaç saniye
        /// oynatır (kare alır), sonra geçer. Karar sahneleri de sahnenin kendisi olarak
        /// oynatılır; seçim verilmez, yalnız görüntü ve kayıt denetlenir.
        /// </summary>
        private IEnumerator CaptureInterludeForQa()
        {
            string original = activeStoryId;
            StoryCatalogEntry[] entries = catalog == null ? Array.Empty<StoryCatalogEntry>() : catalog.entries;
            for (int e = 0; e < entries.Length; e++)
            {
                StoryCatalogEntry entry = entries[e];
                if (entry == null || !entry.IsPlayable) continue;
                activeStoryId = StoryRepository.SanitizeStoryId(entry.storyId);
                LoadStoryForLocale(settings.locale);
                StoryDatabase story = storyController.Story;
                if (story == null || story.nodes == null) continue;
                for (int n = 0; n < story.nodes.Length; n++)
                {
                    StoryNode target = story.nodes[n];
                    if (target == null || !target.HasInterlude) continue;
                    StartNewGameForTests();
                    yield return null;
                    List<int> route = RouteForQa(story, story.startNodeId, target.id);
                    if (route == null)
                    {
                        Debug.LogError("ORDINARY_FRONTS_PLAYER_SMOKE_FAILED: ara sahne düğümüne yol yok: " + target.id);
                        continue;
                    }
                    for (int i = 0; i < route.Count; i++)
                    {
                        ChooseForTests(route[i]);
                        yield return null;
                    }
                    StoryNode node = storyController.CurrentNode;
                    if (node == null || node.id != target.id)
                    {
                        Debug.LogError("ORDINARY_FRONTS_PLAYER_SMOKE_FAILED: ara sahne düğümüne ulaşılamadı: " + target.id);
                        continue;
                    }
                    interludeForcePush = true;
                    StartCoroutine(RunInterlude(node));
                    float waited = 0f;
                    while (waited < 6.4f && interludeActive)
                    {
                        waited += Time.unscaledDeltaTime;
                        yield return null;
                    }
                    Canvas.ForceUpdateCanvases();
                    CaptureInterfaceOffscreen(Path.Combine(commandLineCaptureDirectory, "interlude_" + node.interlude.id + "_" + Screen.width + "x" + Screen.height + ".png"));
                    yield return null;
                    interludeSkipRequested = true;
                    while (interludeActive) yield return null;
                    interludeForcePush = false;
                    Debug.Log("ORDINARY_FRONTS_INTERLUDE: story=" + entry.storyId + " node=" + node.id + " id=" + node.interlude.id +
                        " seen=" + storyController.State.HasSeenResult(StoryVocabulary.InterludeSeenKey(node.interlude.id)));
                    RenderNodeContent(node);
                    router.Show(AppScreen.Gameplay);
                    yield return null;
                }
            }
            activeStoryId = original;
            LoadStoryForLocale(settings.locale);
            yield return null;
        }

        /// <summary>Başlangıçtan hedefe en kısa seçim dizisi (genişlik öncelikli arama); yol yoksa null.</summary>
        private static List<int> RouteForQa(StoryDatabase story, string from, string to)
        {
            Dictionary<string, StoryNode> nodes = new Dictionary<string, StoryNode>();
            for (int i = 0; i < story.nodes.Length; i++) if (story.nodes[i] != null) nodes[story.nodes[i].id] = story.nodes[i];
            Dictionary<string, KeyValuePair<string, int>> previous = new Dictionary<string, KeyValuePair<string, int>>();
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(from);
            previous[from] = new KeyValuePair<string, int>(null, -1);
            while (queue.Count > 0)
            {
                string id = queue.Dequeue();
                if (id == to) break;
                StoryNode node;
                if (!nodes.TryGetValue(id, out node) || node.choices == null) continue;
                for (int i = 0; i < node.choices.Length; i++)
                {
                    string next = node.choices[i] == null ? null : node.choices[i].nextNodeId;
                    if (string.IsNullOrEmpty(next) || previous.ContainsKey(next)) continue;
                    previous[next] = new KeyValuePair<string, int>(id, i);
                    queue.Enqueue(next);
                }
            }
            if (!previous.ContainsKey(to)) return null;
            List<int> route = new List<int>();
            string cursor = to;
            while (previous[cursor].Key != null)
            {
                route.Insert(0, previous[cursor].Value);
                cursor = previous[cursor].Key;
            }
            return route;
        }

        private IEnumerator CaptureArchiveForQa()
        {
            string original = activeStoryId;

            activeStoryId = StoryRepository.SanitizeStoryId("neretva_1943");
            LoadStoryForLocale(settings.locale);
            StartNewGameForTests();
            yield return null;
            Canvas.ForceUpdateCanvases();
            CaptureInterfaceOffscreen(Path.Combine(commandLineCaptureDirectory,
                "archive_crossing_" + Screen.width + "x" + Screen.height + ".png"));
            Debug.Log("ORDINARY_FRONTS_CROSSING: " + (echoText != null && echoText.gameObject.activeSelf ? echoText.text.Replace("\n", " | ") : "(yok)"));
            yield return null;

            activeStoryId = StoryRepository.SanitizeStoryId("hamburg_1943");
            LoadStoryForLocale(settings.locale);
            StartNewGameForTests();
            yield return null;
            OpenJournal();
            yield return null;
            Canvas.ForceUpdateCanvases();
            CaptureInterfaceOffscreen(Path.Combine(commandLineCaptureDirectory,
                "archive_previous_" + Screen.width + "x" + Screen.height + ".png"));
            Debug.Log("ORDINARY_FRONTS_PREVIOUS: " + (journalText != null ? journalText.text.Replace("\n", " | ") : "(yok)"));
            yield return null;
            router.Show(AppScreen.Gameplay);

            activeStoryId = original;
            LoadStoryForLocale(settings.locale);
            yield return null;
        }

        private IEnumerator CaptureEndingForQa()
        {
            StartNewGameForTests();
            yield return null;
            int guard = 0;
            while (router.Current == AppScreen.Gameplay && guard < 40)
            {
                ChooseForTests(0);
                guard++;
                yield return null;
            }
            Canvas.ForceUpdateCanvases();
            yield return null;
            if (router.Current == AppScreen.Ending)
            {
                CaptureInterfaceOffscreen(Path.Combine(commandLineCaptureDirectory,
                    "ending_" + Screen.width + "x" + Screen.height + ".png"));
                Debug.Log("ORDINARY_FRONTS_ENDING_REPORT: " + endingTracesText.text.Replace("\n", " | "));
            }
            else Debug.LogError("ORDINARY_FRONTS_PLAYER_SMOKE_FAILED: finale ulaşılamadı, ekran=" + router.Current);
            yield return null;
        }

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
#endif

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
            // Varsayılan katman anlatıdır: serif. Belge katmanına giren metinler çağrı
            // yerinde AsDocument() ile daktiloya çevrilir.
            if (theme != null && theme.serifFont != null) text.font = theme.serifFont;
            text.text = value;
            text.fontSize = size;
            // Otomatik boyutlandırma yalnız fontSizeMin verilip fontSizeMax bırakıldığında
            // metni tamamen görünmez yapıyordu: final paragrafları, hata mesajları ve menü
            // ipucu bu yüzden hiç çizilmiyordu. Tasarım boyutu baştan üst sınır olarak
            // yazılır; ihtiyaç duyan çağrı yeri bunu ayrıca büyütebilir.
            text.fontSizeMax = size;
            text.fontSizeMin = size;
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Testler ve komut satırı duman koşusu açılış kurgusunu atlar; doğrulanmak istenen şey
        /// oynanış döngüsüdür ve kurgu ölçümü zamana bağımlı hâle getirirdi.
        /// Kurgunun kendisi <see cref="PlayIntroForTests"/> ile ayrıca sınanır.
        /// </summary>
        public void StartNewGameForTests()
        {
            StartNewGame(false);
        }

        public void PlayIntroForTests()
        {
            StartNewGame(true);
        }

        public void RequestIntroSkipForTests()
        {
            introSkipRequested = true;
        }

        public void ToggleLanguageForTests()
        {
            ToggleLanguage();
        }

        public string CurrentLocale { get { return settings == null ? null : settings.locale; } }

        /// <summary>Arayüz yeniden kurulduktan sonra sahne görselinin korunduğunu sınamak için.</summary>
        public bool BackgroundHasSprite { get { return backgroundArt != null && backgroundArt.sprite != null; } }

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
#endif
    }
}
