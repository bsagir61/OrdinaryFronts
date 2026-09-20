using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace OrdinaryFronts.Editor
{
    internal static class GeneratedAssetFactory
    {
        internal const string ArtRoot = "Assets/OrdinaryFronts/Art/Generated";
        internal const string UiRoot = ArtRoot + "/UI";
        internal const string AudioRoot = "Assets/OrdinaryFronts/Audio/Generated";

        private static readonly Color32 Soot = new Color32(0x17, 0x1B, 0x21, 0xFF);
        private static readonly Color32 Paper = new Color32(0xD8, 0xCF, 0xB6, 0xFF);
        private static readonly Color32 Rust = new Color32(0x9E, 0x44, 0x34, 0xFF);
        private static readonly Color32 Petrol = new Color32(0x3F, 0x64, 0x68, 0xFF);
        private static readonly Color32 Mustard = new Color32(0xA8, 0x8B, 0x4A, 0xFF);
        private static readonly Color32 Ink = new Color32(0x26, 0x25, 0x22, 0xFF);

        internal static void EnsureAll()
        {
            Directory.CreateDirectory(ArtRoot);
            Directory.CreateDirectory(UiRoot);
            Directory.CreateDirectory(AudioRoot);
            GenerateUiTextures();
            GenerateIntroGrain();
            GenerateIntroTextScrim();
            GenerateApplicationIcon();
            GenerateMissingBackgrounds();
            GenerateAudio();
            FontAssetFactory.EnsureAll();
            GenerateEuropeMap();
            GenerateMapMarker();
            GenerateInterludeSprites();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureTextureImports();
            ConfigureAudioImports();
            AssetDatabase.SaveAssets();
        }

        private static void GenerateUiTextures()
        {
            const int size = 256;
            Texture2D paper = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];
            System.Random random = new System.Random(19430724);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float edge = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
                    int grain = random.Next(-10, 11);
                    Color32 value = Shift(Paper, grain);
                    if (edge < 7f) value = Blend(value, Ink, (7f - edge) / 18f);
                    if ((x + y * 3) % 47 == 0) value = Blend(value, Mustard, 0.13f);
                    pixels[y * size + x] = value;
                }
            }
            paper.SetPixels32(pixels);
            paper.Apply(false, false);
            WritePng(UiRoot + "/paper_panel.png", paper);

            Texture2D button = NewTexture(size, size);
            pixels = new Color32[size * size];
            random = new System.Random(19430803);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int grain = random.Next(-6, 7);
                    Color32 value = Shift(Soot, grain);
                    int borderDistance = Math.Min(Math.Min(x, size - 1 - x), Math.Min(y, size - 1 - y));
                    if (borderDistance < 5) value = Rust;
                    else if ((x * 7 + y * 11) % 89 == 0) value = Blend(value, Paper, 0.06f);
                    pixels[y * size + x] = value;
                }
            }
            button.SetPixels32(pixels);
            button.Apply(false, false);
            WritePng(UiRoot + "/button_panel.png", button);

            Texture2D vignette = NewTexture(512, 256);
            Color32[] vignettePixels = new Color32[512 * 256];
            for (int y = 0; y < 256; y++)
            {
                for (int x = 0; x < 512; x++)
                {
                    float nx = Mathf.Abs(x / 511f * 2f - 1f);
                    float ny = Mathf.Abs(y / 255f * 2f - 1f);
                    float a = Mathf.Clamp01((Mathf.Max(nx, ny) - 0.35f) / 0.65f) * 0.56f;
                    vignettePixels[y * 512 + x] = new Color32(Soot.r, Soot.g, Soot.b, (byte)(a * 255f));
                }
            }
            vignette.SetPixels32(vignettePixels);
            vignette.Apply(false, false);
            WritePng(UiRoot + "/vignette.png", vignette);
            UnityEngine.Object.DestroyImmediate(paper);
            UnityEngine.Object.DestroyImmediate(button);
            UnityEngine.Object.DestroyImmediate(vignette);
        }

        /// <summary>
        /// Açılış kurgusu için üç kare arşiv greni. Kareler çok seyrek ve düşük opaklıktadır;
        /// amaç okunabilirliği bozmadan taranmış film dokusu hissi vermektir.
        /// </summary>
        private static void GenerateIntroGrain()
        {
            const int size = 256;
            for (int frame = 0; frame < 3; frame++)
            {
                Texture2D texture = NewTexture(size, size);
                Color32[] pixels = new Color32[size * size];
                System.Random random = new System.Random(19430724 + frame * 613);
                for (int i = 0; i < pixels.Length; i++)
                {
                    int roll = random.Next(0, 100);
                    if (roll < 4) pixels[i] = new Color32(Paper.r, Paper.g, Paper.b, (byte)random.Next(24, 70));
                    else if (roll < 7) pixels[i] = new Color32(Ink.r, Ink.g, Ink.b, (byte)random.Next(20, 60));
                    else pixels[i] = new Color32(0, 0, 0, 0);
                }
                // Seyrek dikey çizikler: taranmış film kenarı çağrışımı, metnin üstünden geçmeyecek yoğunlukta.
                int scratches = random.Next(1, 3);
                for (int s = 0; s < scratches; s++)
                {
                    int x = random.Next(0, size);
                    int top = random.Next(0, size);
                    int length = random.Next(size / 6, size / 2);
                    for (int y = top; y < Mathf.Min(size, top + length); y++)
                        pixels[y * size + x] = new Color32(Paper.r, Paper.g, Paper.b, 38);
                }
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                WritePng(UiRoot + "/intro_grain_" + frame + ".png", texture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        /// <summary>
        /// Açılış metninin arkasına serilen dikey gradyan. Üstte ve altta tamamen saydam,
        /// ortada yumuşak biçimde koyulaşır. Amaç, tüm görüntüyü karartmadan yalnız metin
        /// bandında kontrast kazanmaktır; sert kenarlı bir bant gibi okunmamalıdır.
        /// </summary>
        private static void GenerateIntroTextScrim()
        {
            const int width = 64;
            const int height = 256;
            const float peak = 0.62f;
            Texture2D texture = NewTexture(width, height);
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float n = y / (float)(height - 1);
                float bump = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(n - 0.5f) * 2f);
                byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt(peak * bump * 255f), 0, 255);
                for (int x = 0; x < width; x++) pixels[y * width + x] = new Color32(Soot.r, Soot.g, Soot.b, alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            WritePng(UiRoot + "/intro_text_scrim.png", texture);
            UnityEngine.Object.DestroyImmediate(texture);
        }

        /// <summary>
        /// Uygulama ikonu. 16 piksele kadar okunabilmesi gerektiği için sahne görsellerinin
        /// ayrıntı dilini kullanmaz: is lacivert zemin, tek bir kalın vinç silüeti (dikey
        /// direk + çapraz bom) ve altta pas rengi bir ufuk çizgisi. Üç şekil, üç renk.
        /// Koordinatlar normalize edilir; her boyut kendi çözünürlüğünde çizilir, böylece
        /// küçük boyutlarda ölçek küçültmeden gelen bulanıklık oluşmaz.
        /// </summary>
        internal static Texture2D CreateApplicationIcon(int size)
        {
            Texture2D texture = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];
            System.Random random = new System.Random(19430724);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int grain = size >= 64 ? random.Next(-5, 6) : 0;
                    pixels[y * size + x] = Shift(Soot, grain);
                }
            }
            texture.SetPixels32(pixels);

            int thick = Mathf.Max(2, Mathf.RoundToInt(size * 0.085f));
            int P(float n) { return Mathf.RoundToInt(n * (size - 1)); }

            // Ufuk: pas çizgisi, alt üçte birde.
            FillRect(texture, 0, P(0.26f), size, P(0.26f) + Mathf.Max(1, thick / 2), Rust);

            // Vinç direği ve bomu: ikonun tanınan silüeti.
            DrawLine(texture, P(0.34f), P(0.26f), P(0.34f), P(0.82f), Paper, thick);
            DrawLine(texture, P(0.34f), P(0.78f), P(0.78f), P(0.60f), Paper, thick);
            // Kısa askı hattı; büyük boyutlarda derinlik verir, küçükte kaybolur.
            if (size >= 48) DrawLine(texture, P(0.72f), P(0.62f), P(0.72f), P(0.44f), Paper, Mathf.Max(1, thick / 2));

            texture.Apply(false, false);
            return texture;
        }

        internal const string IconRoot = ArtRoot + "/Icon";

        internal static string IconPath(int size)
        {
            return IconRoot + "/app_icon_" + size + ".png";
        }

        /// <summary>
        /// Her ikon boyutu ayrı bir asset olarak yazılır. PlayerSettings bellekte üretilmiş
        /// dokuları kabul etmez; yalnız GUID'i olan asset referanslarını saklar, aksi hâlde
        /// ikon slotları sessizce boş kalır.
        /// </summary>
        private static void GenerateApplicationIcon()
        {
            Directory.CreateDirectory(IconRoot);
            int[] sizes = UnityEditor.PlayerSettings.GetIconSizesForTargetGroup(UnityEditor.BuildTargetGroup.Standalone);
            if (sizes == null || sizes.Length == 0) sizes = new[] { 1024, 512, 256, 128, 64, 48, 32, 16 };
            for (int i = 0; i < sizes.Length; i++)
            {
                Texture2D icon = CreateApplicationIcon(sizes[i]);
                WritePng(IconPath(sizes[i]), icon);
                UnityEngine.Object.DestroyImmediate(icon);
            }
        }

        /// <summary>Ara sahnelerin saydam sprite'ları; dosya varsa yeniden üretilmez.</summary>
        private static void GenerateInterludeSprites()
        {
            string folder = ArtRoot + "/" + InterludeSpriteFactory.Folder;
            Directory.CreateDirectory(folder);
            for (int i = 0; i < InterludeSpriteFactory.Keys.Length; i++)
            {
                string path = folder + "/" + InterludeSpriteFactory.Keys[i] + ".png";
                if (File.Exists(path)) continue;
                Texture2D texture = InterludeSpriteFactory.Render(InterludeSpriteFactory.Keys[i]);
                WritePng(path, texture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        /// <summary>Bölüm seçim haritası; dosya varsa yeniden üretilmez.</summary>
        private static void GenerateEuropeMap()
        {
            string path = UiRoot + "/" + EuropeMapFactory.FileName;
            if (File.Exists(path)) return;
            Texture2D texture = EuropeMapFactory.Render();
            WritePng(path, texture);
            UnityEngine.Object.DestroyImmediate(texture);
        }

        /// <summary>
        /// Harita işaretlerinin yuvarlak diski: kenarı yumuşatılmış beyaz bir daire. Halka,
        /// iç halka ve nokta aynı diskin farklı boyut ve renklerde üst üste konmasıyla
        /// çizilir; dosya varsa yeniden üretilmez.
        /// </summary>
        private static void GenerateMapMarker()
        {
            string path = UiRoot + "/map_marker.png";
            if (File.Exists(path)) return;
            const int size = 64;
            Texture2D texture = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float radius = size * 0.5f - 1.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            WritePng(path, texture);
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void GenerateMissingBackgrounds()
        {
            string[] names =
            {
                "bg_shipyard_evening.png", "bg_shelter_stairs.png", "bg_bombed_street.png",
                "bg_aid_registry.png", "bg_train_platform.png", "bg_harbor_dawn.png"
            };
            for (int i = 0; i < names.Length; i++)
            {
                string path = ArtRoot + "/" + names[i];
                if (File.Exists(path)) continue;
                Texture2D texture = GenerateFallbackScene(i);
                WritePng(path, texture);
                UnityEngine.Object.DestroyImmediate(texture);
            }

            // Yugoslavya bölümünün sahneleri ayrı bir üreticiden gelir: bunlar yer tutucu
            // değil, palete ve tekniğe uyacak biçimde çizilmiş katmanlı siluet sahneleridir.
            // Dosya varsa dokunulmaz; elle hazırlanmış bir illüstrasyon aynı ada konduğunda
            // kod değişikliği gerekmeden onun yerini alır.
            for (int i = 0; i < LinocutSceneFactory.FileNames.Length; i++)
            {
                string path = ArtRoot + "/" + LinocutSceneFactory.FileNames[i];
                if (File.Exists(path)) continue;
                Texture2D texture = LinocutSceneFactory.Render(i);
                WritePng(path, texture);
                UnityEngine.Object.DestroyImmediate(texture);
            }

            // Amsterdam bölümünün sahneleri: aynı teknik, düz arazi konuları.
            for (int i = 0; i < HungerWinterSceneFactory.FileNames.Length; i++)
            {
                string path = ArtRoot + "/" + HungerWinterSceneFactory.FileNames[i];
                if (File.Exists(path)) continue;
                Texture2D texture = HungerWinterSceneFactory.Render(i);
                WritePng(path, texture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static Texture2D GenerateFallbackScene(int scene)
        {
            const int width = 960;
            const int height = 540;
            Texture2D texture = NewTexture(width, height);
            Color32[] pixels = new Color32[width * height];
            System.Random random = new System.Random(19430000 + scene * 97);
            for (int y = 0; y < height; y++)
            {
                float vertical = y / (float)(height - 1);
                Color32 baseColor = Blend(Soot, scene == 5 ? Mustard : Petrol, vertical * (scene == 5 ? 0.68f : 0.28f));
                for (int x = 0; x < width; x++)
                {
                    int grain = random.Next(-9, 10);
                    Color32 color = Shift(baseColor, grain);
                    if ((x * 13 + y * 7 + scene * 31) % 113 == 0) color = Blend(color, Paper, 0.15f);
                    pixels[y * width + x] = color;
                }
            }
            texture.SetPixels32(pixels);

            if (scene == 0) DrawShipyard(texture);
            else if (scene == 1) DrawShelter(texture);
            else if (scene == 2) DrawStreet(texture);
            else if (scene == 3) DrawRegistry(texture);
            else if (scene == 4) DrawTrain(texture);
            else DrawHarbor(texture);
            AddHalftone(texture, scene);
            texture.Apply(false, false);
            return texture;
        }

        private static void DrawShipyard(Texture2D t)
        {
            FillRect(t, 0, 0, 960, 145, Ink);
            FillRect(t, 95, 120, 450, 210, Soot);
            DrawLine(t, 550, 135, 550, 455, Ink, 10);
            DrawLine(t, 550, 440, 790, 380, Ink, 9);
            DrawLine(t, 660, 410, 660, 230, Ink, 4);
            DrawLine(t, 790, 380, 824, 284, Ink, 4);
            DrawLine(t, 110, 145, 310, 295, Rust, 6);
            DrawLine(t, 310, 295, 455, 145, Rust, 6);
            FillRect(t, 690, 85, 705, 145, Mustard);
        }

        private static void DrawShelter(Texture2D t)
        {
            FillRect(t, 0, 0, 960, 540, Ink);
            for (int i = 0; i < 8; i++)
            {
                int x0 = 110 + i * 72;
                int y0 = 70 + i * 48;
                FillRect(t, x0, y0, x0 + 245, y0 + 34, Paper);
                FillRect(t, x0 + 215, y0 + 34, x0 + 245, y0 + 85, Petrol);
            }
            FillRect(t, 665, 95, 830, 420, Soot);
            FillRect(t, 705, 145, 790, 350, Mustard);
            DrawCircle(t, 748, 365, 22, Paper);
        }

        private static void DrawStreet(Texture2D t)
        {
            FillRect(t, 0, 0, 960, 95, Ink);
            FillRect(t, 40, 95, 255, 430, Soot);
            FillRect(t, 690, 95, 940, 455, Soot);
            FillRect(t, 285, 95, 500, 365, Petrol);
            for (int y = 150; y < 380; y += 78)
                for (int x = 75; x < 900; x += 125) FillRect(t, x, y, x + 45, y + 42, Mustard);
            DrawLine(t, 430, 95, 590, 245, Rust, 16);
            DrawLine(t, 590, 245, 720, 95, Rust, 16);
            DrawCircle(t, 560, 68, 24, Paper);
        }

        private static void DrawRegistry(Texture2D t)
        {
            FillRect(t, 0, 0, 960, 120, Ink);
            FillRect(t, 115, 115, 845, 250, Paper);
            FillRect(t, 155, 250, 810, 290, Soot);
            for (int x = 170; x < 780; x += 110)
            {
                DrawCircle(t, x, 335, 24, Ink);
                FillRect(t, x - 22, 185, x + 22, 315, Petrol);
            }
            FillRect(t, 575, 270, 715, 420, Mustard);
            DrawLine(t, 590, 305, 690, 305, Rust, 5);
            DrawLine(t, 590, 345, 680, 345, Rust, 5);
        }

        private static void DrawTrain(Texture2D t)
        {
            FillRect(t, 0, 0, 960, 115, Ink);
            FillRect(t, 85, 145, 860, 340, Soot);
            for (int x = 125; x < 790; x += 120) FillRect(t, x, 215, x + 68, 300, Mustard);
            DrawCircle(t, 205, 130, 39, Ink);
            DrawCircle(t, 715, 130, 39, Ink);
            DrawLine(t, 0, 80, 960, 80, Rust, 6);
            DrawLine(t, 0, 58, 960, 58, Paper, 3);
            DrawLine(t, 120, 540, 305, 340, Ink, 8);
            DrawLine(t, 835, 540, 650, 340, Ink, 8);
        }

        private static void DrawHarbor(Texture2D t)
        {
            FillRect(t, 0, 0, 960, 120, Ink);
            FillRect(t, 0, 120, 960, 165, Petrol);
            for (int x = 65; x < 900; x += 95) FillRect(t, x, 165, x + 55, 215 + (x % 4) * 25, Soot);
            DrawLine(t, 185, 165, 185, 400, Ink, 8);
            DrawLine(t, 185, 385, 365, 340, Ink, 7);
            DrawLine(t, 675, 165, 675, 440, Ink, 8);
            DrawLine(t, 675, 425, 835, 370, Ink, 7);
            DrawCircle(t, 770, 425, 58, Mustard);
            FillRect(t, 0, 110, 960, 125, Rust);
        }

        private static void AddHalftone(Texture2D texture, int seed)
        {
            for (int y = 12 + seed; y < texture.height; y += 18)
                for (int x = 12 + seed * 3; x < texture.width; x += 18)
                    if (((x / 18) + (y / 18) + seed) % 3 == 0) DrawCircle(texture, x, y, 2, new Color32(Paper.r, Paper.g, Paper.b, 32), true);
        }

        private static void GenerateAudio()
        {
            WriteWave(AudioRoot + "/paper_transition.wav", 0.34f, (i, t, random) =>
            {
                float envelope = Mathf.Exp(-t * 13f) * Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t * 80f));
                return ((float)random.NextDouble() * 2f - 1f) * envelope * 0.18f + Mathf.Sin(t * 530f) * envelope * 0.025f;
            }, 11);
            WriteWave(AudioRoot + "/choice_confirm.wav", 0.22f, (i, t, random) =>
                (Mathf.Sin(t * Mathf.PI * 2f * 164f) + 0.45f * Mathf.Sin(t * Mathf.PI * 2f * 246f)) * Mathf.Exp(-t * 16f) * 0.12f, 12);
            WriteWave(AudioRoot + "/menu_back.wav", 0.24f, (i, t, random) =>
                Mathf.Sin(t * Mathf.PI * 2f * (190f - t * 190f)) * Mathf.Exp(-t * 15f) * 0.11f, 13);
            WriteWave(AudioRoot + "/city_harbor_ambience.wav", 12f, (i, t, random) =>
            {
                float fade = LoopFade(t, 12f);
                float rumble = Mathf.Sin(t * Mathf.PI * 2f * 43f) * 0.012f + Mathf.Sin(t * Mathf.PI * 2f * 57f) * 0.007f;
                float air = ((float)random.NextDouble() * 2f - 1f) * 0.006f;
                float horn = Mathf.Sin(t * Mathf.PI * 2f * 92f) * Mathf.Clamp01(1f - Mathf.Abs(t - 7.4f) / 1.2f) * 0.018f;
                return (rumble + air + horn) * fade;
            }, 1943);
            WriteWave(AudioRoot + "/shelter_ambience.wav", 12f, (i, t, random) =>
            {
                float fade = LoopFade(t, 12f);
                float pulse = Mathf.Sin(t * Mathf.PI * 2f * 37f) * 0.016f + Mathf.Sin(t * Mathf.PI * 2f * 61f) * 0.008f;
                float air = ((float)random.NextDouble() * 2f - 1f) * 0.0035f;
                return (pulse + air) * fade;
            }, 1944);
            WriteWave(AudioRoot + "/train_platform_ambience.wav", 12f, (i, t, random) =>
            {
                float fade = LoopFade(t, 12f);
                float bed = Mathf.Sin(t * Mathf.PI * 2f * 48f) * 0.012f + ((float)random.NextDouble() * 2f - 1f) * 0.005f;
                float beat = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * Mathf.PI * 2f * 1.65f)), 24f) * 0.028f;
                return (bed + beat) * fade;
            }, 1945);
            GenerateWindAudio();
        }

        /// <summary>
        /// Set rüzgârı: iki katmanlı gürültü; yavaş bir salınım şiddeti taşır, hızlı bir
        /// çırpıntı yüzeyi verir. Döngü kenarları sessize iner.
        /// </summary>
        private static void GenerateWindAudio()
        {
            WriteWave(AudioRoot + "/wind_dike_ambience.wav", 14f, (i, t, random) =>
            {
                float fade = LoopFade(t, 14f);
                float swell = 0.55f + 0.45f * Mathf.Sin(t * Mathf.PI * 2f * 0.11f) * Mathf.Sin(t * Mathf.PI * 2f * 0.07f + 1.3f);
                float body = ((float)random.NextDouble() * 2f - 1f) * 0.018f * swell;
                float low = Mathf.Sin(t * Mathf.PI * 2f * 31f) * 0.006f * swell;
                return (body + low) * fade;
            }, 1945_01);
            WriteWave(AudioRoot + "/wind_gust.wav", 3.2f, (i, t, random) =>
            {
                float envelope = Mathf.Sin(Mathf.Clamp01(t / 3.2f) * Mathf.PI);
                envelope *= envelope;
                float body = ((float)random.NextDouble() * 2f - 1f) * 0.16f;
                float whistle = Mathf.Sin(t * Mathf.PI * 2f * (640f + 90f * Mathf.Sin(t * 2.1f))) * 0.012f;
                return (body + whistle) * envelope;
            }, 1945_02);
            // Kıvılcım: üç kısa gürültü patlaması, tizde.
            WriteWave(AudioRoot + "/spark_crackle.wav", 0.6f, (i, t, random) =>
            {
                float burst = 0f;
                for (int k = 0; k < 3; k++)
                {
                    float centre = 0.06f + k * 0.17f;
                    burst += Mathf.Exp(-Mathf.Abs(t - centre) * 60f);
                }
                return ((float)random.NextDouble() * 2f - 1f) * 0.22f * burst;
            }, 1943_03);
            // Kiriş gıcırtısı: aşağı kayan bir ton ve ahşap gürültüsü.
            WriteWave(AudioRoot + "/plank_creak.wav", 0.9f, (i, t, random) =>
            {
                float envelope = Mathf.Sin(Mathf.Clamp01(t / 0.9f) * Mathf.PI);
                float tone = Mathf.Sin(t * Mathf.PI * 2f * (210f - t * 120f)) * 0.08f;
                float wood = ((float)random.NextDouble() * 2f - 1f) * 0.05f * Mathf.PerlinNoise(t * 40f, 0.5f);
                return (tone + wood) * envelope;
            }, 1943_04);
            // Nehir: sürekli beyaz gürültü, yavaş dalgalanma.
            WriteWave(AudioRoot + "/river_ambience.wav", 12f, (i, t, random) =>
            {
                float fade = LoopFade(t, 12f);
                float swell = 0.7f + 0.3f * Mathf.Sin(t * Mathf.PI * 2f * 0.23f);
                return ((float)random.NextDouble() * 2f - 1f) * 0.014f * swell * fade;
            }, 1943_05);
        }

        private static float LoopFade(float time, float duration)
        {
            return Mathf.Clamp01(time / 0.45f) * Mathf.Clamp01((duration - time) / 0.45f);
        }

        private delegate float SampleFunction(int index, float time, System.Random random);

        private static void WriteWave(string path, float duration, SampleFunction sampleFunction, int seed)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            using (MemoryStream memory = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(memory))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + sampleCount * 2);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(sampleRate);
                writer.Write(sampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(sampleCount * 2);
                System.Random random = new System.Random(seed);
                for (int i = 0; i < sampleCount; i++)
                {
                    float sample = Mathf.Clamp(sampleFunction(i, i / (float)sampleRate, random), -0.92f, 0.92f);
                    writer.Write((short)(sample * short.MaxValue));
                }
                File.WriteAllBytes(path, memory.ToArray());
            }
        }

        private static void ConfigureTextureImports()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                // Uygulama ikonları sprite değildir ve sıkıştırılmamalıdır: PlayerSettings
                // bunları doğrudan doku olarak kullanır, sıkıştırma küçük boyutlarda kenarları
                // bozar.
                if (path.Contains("/Icon/"))
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.mipmapEnabled = false;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.maxTextureSize = 1024;
                    importer.alphaIsTransparency = false;
                    importer.SaveAndReimport();
                    continue;
                }

                // Ara sahne sprite'ları saydam zeminlidir ve tek tek ölçeklenir; arayüz
                // dokuları gibi işlenir ama 512 sınırı figür karelerini bulanıklaştırırdı.
                bool interlude = path.Contains("/" + InterludeSpriteFactory.Folder + "/");
                bool ui = path.Contains("/UI/") || interlude;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = ui;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                // Harita bir arayüz dokusu ama okunması gereken kıyı çizgileri taşır; 512'ye
                // küçültülürse bulanır.
                bool map = path.EndsWith(EuropeMapFactory.FileName, StringComparison.OrdinalIgnoreCase);
                importer.maxTextureSize = interlude ? 1024 : (ui && !map ? 512 : 2048);
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                if (path.EndsWith("paper_panel.png", StringComparison.OrdinalIgnoreCase) || path.EndsWith("button_panel.png", StringComparison.OrdinalIgnoreCase))
                    importer.spriteBorder = new Vector4(18f, 18f, 18f, 18f);
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureAudioImports()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioRoot });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;
                AudioImporterSampleSettings sample = importer.defaultSampleSettings;
                sample.loadType = path.Contains("ambience") ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                sample.compressionFormat = AudioCompressionFormat.Vorbis;
                sample.quality = path.Contains("ambience") ? 0.55f : 0.72f;
                importer.defaultSampleSettings = sample;
                importer.preloadAudioData = !path.Contains("ambience");
                importer.SaveAndReimport();
            }
        }

        private static Texture2D NewTexture(int width, int height)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false, false) { name = "Generated Ordinary Fronts Texture" };
        }

        private static void WritePng(string path, Texture2D texture)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }

        private static Color32 Shift(Color32 color, int amount)
        {
            return new Color32(ClampByte(color.r + amount), ClampByte(color.g + amount), ClampByte(color.b + amount), color.a);
        }

        private static byte ClampByte(int value)
        {
            return (byte)Mathf.Clamp(value, 0, 255);
        }

        private static Color32 Blend(Color32 a, Color32 b, float amount)
        {
            return (Color32)Color.Lerp(a, b, Mathf.Clamp01(amount));
        }

        private static void FillRect(Texture2D texture, int x0, int y0, int x1, int y1, Color32 color)
        {
            x0 = Mathf.Clamp(x0, 0, texture.width);
            x1 = Mathf.Clamp(x1, 0, texture.width);
            y0 = Mathf.Clamp(y0, 0, texture.height);
            y1 = Mathf.Clamp(y1, 0, texture.height);
            for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++) texture.SetPixel(x, y, color);
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color32 color, int thickness)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = -Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;
            while (true)
            {
                FillRect(texture, x0 - thickness / 2, y0 - thickness / 2, x0 + thickness / 2 + 1, y0 + thickness / 2 + 1, color);
                if (x0 == x1 && y0 == y1) break;
                int twice = 2 * error;
                if (twice >= dy) { error += dy; x0 += sx; }
                if (twice <= dx) { error += dx; y0 += sy; }
            }
        }

        private static void DrawCircle(Texture2D texture, int centerX, int centerY, int radius, Color32 color, bool blend = false)
        {
            int radiusSquared = radius * radius;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y > radiusSquared) continue;
                    int px = centerX + x;
                    int py = centerY + y;
                    if (px < 0 || px >= texture.width || py < 0 || py >= texture.height) continue;
                    if (blend)
                    {
                        Color existing = texture.GetPixel(px, py);
                        Color overlay = color;
                        texture.SetPixel(px, py, Color.Lerp(existing, overlay, overlay.a));
                    }
                    else texture.SetPixel(px, py, color);
                }
            }
        }
    }
}
