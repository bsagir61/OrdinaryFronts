using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OrdinaryFronts.Editor
{
    public static class DemoBuilder
    {
        public const string ScenePath = "Assets/OrdinaryFronts/Scenes/Main.unity";
        private const string DataRoot = "Assets/OrdinaryFronts/Data";
        private const string BrandPath = DataRoot + "/BrandConfig.asset";
        private const string ThemePath = DataRoot + "/ThemeConfig.asset";

        private static readonly string[] RequiredFolders =
        {
            "Assets/OrdinaryFronts/Runtime",
            "Assets/OrdinaryFronts/Runtime/Core",
            "Assets/OrdinaryFronts/Runtime/Story",
            "Assets/OrdinaryFronts/Runtime/Save",
            "Assets/OrdinaryFronts/Runtime/UI",
            "Assets/OrdinaryFronts/Runtime/Audio",
            "Assets/OrdinaryFronts/Editor",
            "Assets/OrdinaryFronts/Tests/EditMode",
            "Assets/OrdinaryFronts/Tests/PlayMode",
            "Assets/OrdinaryFronts/Art/Generated",
            "Assets/OrdinaryFronts/Audio/Generated",
            "Assets/OrdinaryFronts/Scenes",
            "Assets/OrdinaryFronts/Data",
            "Assets/StreamingAssets/Story/tr-TR",
            "Logs",
            "Builds/Windows"
        };

        [MenuItem("Ordinary Fronts/Build Demo Assets and Scene")]
        public static void BuildAll()
        {
            try
            {
                EnsureFolders();
                EnsureTmpEssentials();
                GeneratedAssetFactory.EnsureAll();
                BrandConfig brand = BuildBrand();
                ThemeConfig theme = BuildTheme();
                BuildMainScene(brand, theme);
                ConfigureBuildSettings();
                ConfigurePlayer(brand);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("ORDINARY_FRONTS_BUILD_ALL_SUCCESS: Main scene, generated assets, config assets and build settings are ready.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        [MenuItem("Ordinary Fronts/Build Windows Development Demo")]
        public static void BuildWindowsDevelopment()
        {
            BuildAll();
            string support = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "WindowsStandaloneSupport");
            if (!Directory.Exists(support))
                throw new BuildFailedException("Windows Standalone Support modülü kurulu değil: " + support);
            Directory.CreateDirectory("Builds/Windows");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/OrdinaryFrontsDemo.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Windows build başarısız: " + report.summary.result + ", hata sayısı: " + report.summary.totalErrors);
            Debug.Log("ORDINARY_FRONTS_WINDOWS_BUILD_SUCCESS: " + Path.GetFullPath(options.locationPathName));
        }

        private static void EnsureFolders()
        {
            for (int i = 0; i < RequiredFolders.Length; i++) Directory.CreateDirectory(RequiredFolders[i]);
            AssetDatabase.Refresh();
        }

        private static void EnsureTmpEssentials()
        {
            const string settingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
            if (File.Exists(settingsPath)) return;
            UnityEditor.PackageManager.PackageInfo package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.textmeshpro");
            if (package == null) throw new InvalidOperationException("TextMeshPro paketi çözümlenemedi.");
            string packagePath = Path.Combine(package.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            if (!File.Exists(packagePath)) throw new FileNotFoundException("TMP Essential Resources paketi bulunamadı.", packagePath);
            UnityPackageExtractor.Extract(packagePath, "Assets/TextMesh Pro/");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (!File.Exists(settingsPath)) throw new InvalidOperationException("TMP Essentials çıkarıldı ancak TMP Settings oluşmadı.");
            Debug.Log("TMP Essentials batch-safe olarak içe aktarıldı: " + packagePath);
        }

        private static BrandConfig BuildBrand()
        {
            BrandConfig asset = AssetDatabase.LoadAssetAtPath<BrandConfig>(BrandPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<BrandConfig>();
                AssetDatabase.CreateAsset(asset, BrandPath);
            }
            asset.ApplyCanonicalDefaults();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static ThemeConfig BuildTheme()
        {
            ThemeConfig asset = AssetDatabase.LoadAssetAtPath<ThemeConfig>(ThemePath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ThemeConfig>();
                AssetDatabase.CreateAsset(asset, ThemePath);
            }
            asset.ApplyCanonicalDefaults();
            asset.paperPanel = AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedAssetFactory.UiRoot + "/paper_panel.png");
            asset.buttonPanel = AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedAssetFactory.UiRoot + "/button_panel.png");
            asset.vignette = AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedAssetFactory.UiRoot + "/vignette.png");
            Sprite[] grain = new Sprite[3];
            for (int i = 0; i < grain.Length; i++)
            {
                string grainPath = GeneratedAssetFactory.UiRoot + "/intro_grain_" + i + ".png";
                grain[i] = AssetDatabase.LoadAssetAtPath<Sprite>(grainPath);
                if (grain[i] == null) throw new InvalidOperationException("Açılış greni yüklenemedi: " + grainPath);
            }
            asset.introGrain = grain;
            asset.introTextScrim = AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedAssetFactory.UiRoot + "/intro_text_scrim.png");
            if (asset.introTextScrim == null) throw new InvalidOperationException("Açılış metin gradyanı yüklenemedi.");
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void BuildMainScene(BrandConfig brand, ThemeConfig theme)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = theme.sootNavy;
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            GameObject systems = new GameObject("Systems");
            GameObject appObject = new GameObject("App Controller");
            appObject.transform.SetParent(systems.transform, false);
            AppController controller = appObject.AddComponent<AppController>();
            GameObject audioObject = new GameObject("Audio Manager");
            audioObject.transform.SetParent(systems.transform, false);
            AudioManager audio = audioObject.AddComponent<AudioManager>();

            GameObject interfaceRoot = new GameObject("Interface", typeof(RectTransform));
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(interfaceRoot.transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject eventSystem = new GameObject("Event System", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetSiblingIndex(3);

            SceneArtEntry[] entries = BuildArtEntries();
            controller.Configure(canvas, audio, brand, theme, entries);
            audio.ConfigureAssets(
                LoadClip("paper_transition.wav"),
                LoadClip("choice_confirm.wav"),
                LoadClip("menu_back.wav"),
                LoadClip("city_harbor_ambience.wav"),
                LoadClip("shelter_ambience.wav"),
                LoadClip("train_platform_ambience.wav"));
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(audio);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static SceneArtEntry[] BuildArtEntries()
        {
            string[] keys = { "shipyard_evening", "shelter_stairs", "bombed_street", "aid_registry", "train_platform", "harbor_dawn" };
            string[] files = { "bg_shipyard_evening.png", "bg_shelter_stairs.png", "bg_bombed_street.png", "bg_aid_registry.png", "bg_train_platform.png", "bg_harbor_dawn.png" };
            SceneArtEntry[] entries = new SceneArtEntry[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                entries[i] = new SceneArtEntry
                {
                    key = keys[i],
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedAssetFactory.ArtRoot + "/" + files[i])
                };
                if (entries[i].sprite == null) throw new InvalidOperationException("Arka plan sprite'ı yüklenemedi: " + files[i]);
            }
            return entries;
        }

        private static AudioClip LoadClip(string fileName)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(GeneratedAssetFactory.AudioRoot + "/" + fileName);
            if (clip == null) throw new InvalidOperationException("Ses klibi yüklenemedi: " + fileName);
            return clip;
        }

        private static void ConfigureBuildSettings()
        {
            // Demo tek sahnelidir. Önceden listede kalan sahneleri korumak, şablondan gelen
            // örnek sahnelerin sessizce derlemeye girmesine yol açıyordu; liste artık tam olarak
            // bu üreticinin ürettiği sahneden ibarettir.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static void ConfigurePlayer(BrandConfig brand)
        {
            PlayerSettings.productName = brand.ProductName;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground = false;
        }
    }
}
