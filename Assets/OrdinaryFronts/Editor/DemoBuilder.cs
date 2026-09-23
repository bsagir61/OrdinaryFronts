using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        /// <summary>
        /// Dağıtıma gidecek sürüm. Development build ile aradaki fark güvenlik açısından
        /// önemlidir: development çıktısı yönetilen derlemelerin yanına <c>.pdb</c> sembol
        /// dosyaları koyar ve derlemelere hata ayıklama bilgisi gömer. Bu dosyalar
        /// geliştiricinin gerçek adını ve Windows kullanıcı adını içeren mutlak kaynak
        /// yollarını taşır (ör. <c>C:\Users\...\OrdinaryFronts\Assets\...</c>), yani
        /// oyunu indiren herkes bu bilgiyi görebilir.
        /// </summary>
        [MenuItem("Ordinary Fronts/Build Windows Release")]
        public static void BuildWindowsRelease()
        {
            AssertBuildPathCarriesNoPersonalInformation();
            // Çalıştırılabilir dosyanın adı ve yanındaki "<ad>_Data" klasörü oyuncunun gördüğü
            // ilk şeydir; "Release" gibi yapı hattı etiketleri buraya sızmamalıdır.
            BuildWindows(BuildOptions.None, "OrdinaryFronts.exe");
        }

        /// <summary>
        /// Yönetilen derlemeler, yanlarındaki <c>.pdb</c> silinse bile PE hata ayıklama
        /// dizininde derlendikleri mutlak yolu taşır. Proje kullanıcı profilinin altındaysa
        /// bu yol Windows kullanıcı adını içerir; çoğu kurulumda bu kişinin gerçek adıdır ve
        /// oyunu indiren herkes dosyaların içinde görebilir. Ölçüldü: bu depo mevcut konumdan
        /// derlendiğinde 15 yönetilen derleme geliştiricinin tam yolunu taşıyor.
        ///
        /// Unity bu gömmeyi bir ayarla kapatmaz; tek kesin çözüm nötr bir yoldan derlemektir.
        /// Bu yüzden yayın derlemesi sessizce sızdırmak yerine burada durur.
        /// </summary>
        private static void AssertBuildPathCarriesNoPersonalInformation()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string normalized = projectRoot.Replace('\\', '/');
            bool underUserProfile =
                normalized.IndexOf("/Users/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                normalized.IndexOf("/home/", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!underUserProfile) return;

            throw new BuildFailedException(
                "Yayın derlemesi kullanıcı profili altındaki bir yoldan alınamaz.\n" +
                "Geçerli konum: " + projectRoot + "\n\n" +
                "Yönetilen derlemeler bu yolu içlerine gömer ve oyunu indiren herkes " +
                "kullanıcı adını görebilir. Projeyi kişisel bilgi içermeyen bir yola " +
                "kopyalayıp oradan derleyin, örneğin C:\\Build\\OrdinaryFronts.\n" +
                "Geliştirme derlemesi (Build Windows Development Demo) bu kısıttan etkilenmez.");
        }

        /// <summary>
        /// Yol denetimi geçse bile üretilen dosyaları fiilen tarar. Yol dışında bir kaynaktan
        /// (paket önbelleği, Burst çıktısı, üçüncü taraf araç) kullanıcı adı sızarsa derleme
        /// burada durur; varsayıma değil çıktının kendisine bakar.
        /// </summary>
        private static void AssertOutputCarriesNoPersonalInformation(string outputRoot)
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(userProfile)) return;
            userProfile = userProfile.TrimEnd('\\', '/');

            // Aranan şey çıplak kullanıcı adı değil, kullanıcı profilinin tam yoludur.
            // İki sebeple: (1) şirket adı oyuncuya gösterilmek üzere kasıtlı olarak çıktıda
            // bulunur ve kullanıcı adıyla aynı olabilir — çıplak adı aramak bu meşru
            // metadatayı sızıntı sanardı; (2) aranan şey zaten yol sızıntısıdır.
            //
            // Arama bayt düzeyinde yapılır. Metni ASCII'ye çözmek, adında ASCII dışı harf
            // bulunan kullanıcılarda (ğ, ı, ö, ü ...) eşleşmeyi sessizce kaçırır.
            byte[][] needles =
            {
                Encoding.UTF8.GetBytes(userProfile),
                Encoding.Unicode.GetBytes(userProfile)
            };

            List<string> leaking = new List<string>();
            string[] files = Directory.GetFiles(outputRoot, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                if (new FileInfo(files[i]).Length > 120L * 1024L * 1024L) continue;
                byte[] content = File.ReadAllBytes(files[i]);
                for (int n = 0; n < needles.Length; n++)
                {
                    if (!ContainsSequence(content, needles[n])) continue;
                    leaking.Add(files[i].Substring(outputRoot.Length).TrimStart('\\', '/'));
                    break;
                }
            }

            if (leaking.Count == 0) return;
            throw new BuildFailedException(
                "Yayın çıktısında geliştiricinin kullanıcı profili yolu geçen " + leaking.Count +
                " dosya var. Bu dosyalar oyunu indiren herkese açıktır:\n  " +
                string.Join("\n  ", leaking.ToArray()));
        }

        private static bool ContainsSequence(byte[] haystack, byte[] needle)
        {
            if (needle.Length == 0 || haystack.Length < needle.Length) return false;
            int limit = haystack.Length - needle.Length;
            for (int i = 0; i <= limit; i++)
            {
                if (haystack[i] != needle[0]) continue;
                int j = 1;
                while (j < needle.Length && haystack[i + j] == needle[j]) j++;
                if (j == needle.Length) return true;
            }
            return false;
        }

        [MenuItem("Ordinary Fronts/Build Windows Development Demo")]
        public static void BuildWindowsDevelopment()
        {
            BuildWindows(BuildOptions.Development, "OrdinaryFrontsDemo.exe");
        }

        private static void BuildWindows(BuildOptions buildOptions, string executableName)
        {
            BuildAll();
            string support = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "WindowsStandaloneSupport");
            if (!Directory.Exists(support))
                throw new BuildFailedException("Windows Standalone Support modülü kurulu değil: " + support);
            string outputRoot = Path.Combine("Builds", buildOptions == BuildOptions.None ? "WindowsRelease" : "Windows");
            Directory.CreateDirectory(outputRoot);
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(outputRoot, executableName),
                target = BuildTarget.StandaloneWindows64,
                options = buildOptions
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Windows build başarısız: " + report.summary.result + ", hata sayısı: " + report.summary.totalErrors);

            if (buildOptions == BuildOptions.None)
            {
                WriteThirdPartyNotices(outputRoot);
                StripShippingArtifacts(outputRoot);
                AssertOutputCarriesNoPersonalInformation(outputRoot);
            }
            Debug.Log("ORDINARY_FRONTS_WINDOWS_BUILD_SUCCESS: " + Path.GetFullPath(options.locationPathName));
        }

        /// <summary>
        /// Üçüncü taraf lisans bildirimini çıktı köküne yazar. Oyun iki yazı tipini gömülü
        /// olarak dağıtıyor ve SIL Open Font License, lisans metninin yazı tipiyle birlikte
        /// dağıtılmasını şart koşuyor; bu dosya o yükümlülüğü karşılar.
        /// </summary>
        private static void WriteThirdPartyNotices(string outputRoot)
        {
            string[] sources =
            {
                FontAssetFactory.FontRoot + "/PTSerif-OFL.txt",
                FontAssetFactory.FontRoot + "/CourierPrime-OFL.txt"
            };
            string[] titles = { "PT Serif (ParaType)", "Courier Prime (Quote-Unquote Apps)" };

            StringBuilder notice = new StringBuilder();
            notice.AppendLine("Ordinary Fronts - Third-party notices");
            notice.AppendLine();
            notice.AppendLine("This product embeds the following typefaces, each licensed under the");
            notice.AppendLine("SIL Open Font License, Version 1.1. The full licence text follows.");
            notice.AppendLine();
            for (int i = 0; i < sources.Length; i++)
            {
                if (!File.Exists(sources[i])) throw new BuildFailedException("Lisans metni bulunamadı: " + sources[i]);
                notice.AppendLine(new string('-', 72));
                notice.AppendLine(titles[i]);
                notice.AppendLine(new string('-', 72));
                notice.AppendLine();
                notice.AppendLine(File.ReadAllText(sources[i]));
                notice.AppendLine();
            }
            File.WriteAllText(Path.Combine(outputRoot, "THIRD-PARTY-NOTICES.txt"), notice.ToString(), new UTF8Encoding(false));
        }

        /// <summary>
        /// Yayın çıktısından dağıtılmaması gereken kalıntıları siler: Unity'nin adını birebir
        /// "DoNotShip" koyduğu Burst hata ayıklama klasörü ve artakalan <c>.pdb</c> sembol
        /// dosyaları. Bu dosyalar geliştiricinin mutlak kaynak yollarını taşır.
        /// </summary>
        private static void StripShippingArtifacts(string outputRoot)
        {
            int removed = 0;
            string[] doNotShip = Directory.GetDirectories(outputRoot, "*DoNotShip*", SearchOption.AllDirectories);
            for (int i = 0; i < doNotShip.Length; i++)
            {
                Directory.Delete(doNotShip[i], true);
                removed++;
            }

            string[] symbols = Directory.GetFiles(outputRoot, "*.pdb", SearchOption.AllDirectories);
            for (int i = 0; i < symbols.Length; i++)
            {
                File.Delete(symbols[i]);
                removed++;
            }
            Debug.Log("ORDINARY_FRONTS_RELEASE_STRIP: kaldırılan hata ayıklama kalıntısı = " + removed);
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

            asset.europeMap = AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedAssetFactory.UiRoot + "/" + EuropeMapFactory.FileName);
            if (asset.europeMap == null) throw new InvalidOperationException("Avrupa haritası yüklenemedi: " + EuropeMapFactory.FileName);
            asset.mapMarker = AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedAssetFactory.UiRoot + "/map_marker.png");
            if (asset.mapMarker == null) throw new InvalidOperationException("Harita işareti yüklenemedi.");

            // Yazı tipleri sessizce boş kalmamalı: atanmadıklarında oyun TMP'nin varsayılan
            // fontuna düşer ve tipografi ayrımı hiç görünmeden kaybolur.
            asset.serifFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(FontAssetFactory.SerifAssetPath);
            if (asset.serifFont == null) throw new InvalidOperationException("Serif yazı tipi yüklenemedi: " + FontAssetFactory.SerifAssetPath);
            asset.monoFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(FontAssetFactory.MonoAssetPath);
            if (asset.monoFont == null) throw new InvalidOperationException("Daktilo yazı tipi yüklenemedi: " + FontAssetFactory.MonoAssetPath);

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
            audio.ConfigureInterludeClips(LoadClip("wind_dike_ambience.wav"), LoadClip("wind_gust.wav"),
                LoadClip("spark_crackle.wav"), LoadClip("plank_creak.wav"), LoadClip("river_ambience.wav"));
            audio.ConfigurePresentationClips(LoadClip("stamp_thud.wav"), LoadClip("act_tone.wav"));
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(audio);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static SceneArtEntry[] BuildArtEntries()
        {
            // Anahtarlar hikâye JSON'larındaki imageKey alanlarıyla birebir eşleşir; bir
            // bölüm eklendiğinde sahneleri buraya da yazmak gerekir, yoksa arka plan sessizce
            // boş kalır. Sıra önemsizdir, isimler önemlidir.
            string[] keys =
            {
                "shipyard_evening", "shelter_stairs", "bombed_street", "aid_registry", "train_platform", "harbor_dawn",
                "mountain_column", "burned_village", "typhus_barn", "river_gorge", "broken_bridge",
                "frozen_canal", "polder_road", "afsluitdijk", "frisian_farm", "canal_night"
            };
            string[] files =
            {
                "bg_shipyard_evening.png", "bg_shelter_stairs.png", "bg_bombed_street.png", "bg_aid_registry.png", "bg_train_platform.png", "bg_harbor_dawn.png",
                "bg_mountain_column.png", "bg_burned_village.png", "bg_typhus_barn.png", "bg_river_gorge.png", "bg_broken_bridge.png",
                "bg_frozen_canal.png", "bg_polder_road.png", "bg_afsluitdijk.png", "bg_frisian_farm.png", "bg_canal_night.png"
            };
            string[] interludeKeys = InterludeSpriteFactory.Keys;
            SceneArtEntry[] entries = new SceneArtEntry[keys.Length + interludeKeys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                entries[i] = new SceneArtEntry
                {
                    key = keys[i],
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedAssetFactory.ArtRoot + "/" + files[i])
                };
                if (entries[i].sprite == null) throw new InvalidOperationException("Arka plan sprite'ı yüklenemedi: " + files[i]);
            }
            // Ara sahne parçaları aynı dizine girer; sahne kodu onlara anahtarla erişir.
            for (int i = 0; i < interludeKeys.Length; i++)
            {
                string path = GeneratedAssetFactory.ArtRoot + "/" + InterludeSpriteFactory.Folder + "/" + interludeKeys[i] + ".png";
                entries[keys.Length + i] = new SceneArtEntry
                {
                    key = interludeKeys[i],
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path)
                };
                if (entries[keys.Length + i].sprite == null) throw new InvalidOperationException("Ara sahne sprite'ı yüklenemedi: " + path);
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
            // Şirket ve uygulama adı kayıt yolunu belirler:
            // %USERPROFILE%\AppData\LocalLow\<şirket>\<uygulama>
            // Yayından sonra değişirlerse mevcut oyuncuların kayıtları erişilemez hâle gelir.
            PlayerSettings.companyName = brand.CompanyName;
            PlayerSettings.productName = brand.ApplicationName;
            PlayerSettings.bundleVersion = brand.Version;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground = false;
            ConfigureApplicationIcon();
        }

        /// <summary>
        /// Uygulama ikonu her boyut için ayrı çizilir. Unity tek bir büyük ikonu küçültebilir,
        /// fakat linol dilindeki kalın kenarlar 16-32 piksele indirildiğinde bulanıklaşır;
        /// her boyutu kendi çözünürlüğünde üretmek görev çubuğunda ve Steam kütüphanesinde
        /// belirgin biçimde daha temiz görünür.
        /// </summary>
        private static void ConfigureApplicationIcon()
        {
            int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Standalone);
            if (sizes == null || sizes.Length == 0) return;

            Texture2D[] icons = new Texture2D[sizes.Length];
            for (int i = 0; i < sizes.Length; i++)
            {
                string path = GeneratedAssetFactory.IconPath(sizes[i]);
                icons[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (icons[i] == null) throw new InvalidOperationException("Uygulama ikonu yüklenemedi: " + path);
            }
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, icons);
        }
    }
}
