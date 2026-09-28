using System.Collections.Generic;
using UnityEngine;
using static OrdinaryFronts.Editor.LinocutSceneFactory;
using Kind = OrdinaryFronts.Editor.HungerWinterSceneFactory.Kind;
using Carry = OrdinaryFronts.Editor.HungerWinterSceneFactory.Carry;

namespace OrdinaryFronts.Editor
{
    /// <summary>
    /// Boyalı sahne ressamı: Neretva 1943 (1.4) ve Karelya 1940 (1.5) bölümlerinin arka planları.
    /// Motor bu dosyada, Karelya sahneleri <c>PaintedSceneFactory.Karelia.cs</c> içindedir.
    /// <para>
    /// İlk üretici beş sahneyle bütün bölümü taşıyordu; sahneler düz bloklardan, gökte
    /// arayüz hatasına benzeyen kesik çizgilerden ve yırtık bir çerçeveden oluşuyordu. Bu
    /// üretici on dört sahne çizer ve her düğüm kendi yerinde geçer: yanmış köy, Rama yolu,
    /// açık sırt, orman, yanmış çiftlik, dere geçidi, gece molası, ahır avlusu, ahırın içi,
    /// gece kuyu başı, kanyon kenarı, yıkık köprü, gece sol yaka ve şafakta karşı yaka.
    /// </para>
    /// <list type="bullet">
    /// <item><b>Kenar yumuşatma.</b> Her sahne iki kat çözünürlükte çizilip küçültülür.</item>
    /// <item><b>Dinar dağları.</b> Sırtlar gürültüden türeyen keskin profillerdir; kaya
    /// olukları, sırta yakın kar, ışığa bakan yüzde kenar ışığı ve uzaklıkla artan pus.</item>
    /// <item><b>Aynı insanlar.</b> Figürler Amsterdam bölümünün sivil silüetini kullanır;
    /// sedye taşıyan çift, oturan, uzanan ve diz çöken figürler burada eklenir.</item>
    /// <item><b>Şiddet yok.</b> Hiçbir sahnede silah, yaralanma ya da ölü beden çizilmez;
    /// yaralılar battaniye altındaki kütlelerdir.</item>
    /// </list>
    /// Koordinatlar tasarım uzayındadır (1672×941, y yukarı); çizim yardımcıları iki kata
    /// kendileri ölçekler.
    /// </summary>
    internal static partial class PaintedSceneFactory
    {
        private const int S = 2;

        internal static readonly string[] Keys =
        {
            "burned_village", "mountain_column", "snow_ridge", "pine_forest", "burned_farm", "stream_ford", "night_camp",
            "typhus_barn", "barn_interior", "barn_night", "river_gorge", "broken_bridge", "bank_night", "far_bank",
            // Karelya 1940
            "village_exchange", "karelian_farm", "orthodox_chapel", "churchyard", "evacuation_road", "school_night", "frozen_bay", "border_station"
        };

        internal static string FileName(int scene) { return "bg_" + Keys[scene] + ".png"; }

        /// <summary>
        /// Geliştirme önizlemesi: sahneleri proje dışına alınmayan Logs/ScenePreview dizinine
        /// yazar; oyun varlıklarına dokunmaz. <c>-scenes a,b</c> ile yalnız seçilenler.
        /// </summary>
        public static void Preview()
        {
            string dir = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, "Logs", "ScenePreview");
            System.IO.Directory.CreateDirectory(dir);
            string only = null;
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-scenes") only = "," + args[i + 1] + ",";
            for (int i = 0; i < Keys.Length; i++)
            {
                if (only != null && !only.Contains("," + Keys[i] + ",")) continue;
                System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
                Texture2D texture = Render(i);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, FileName(i)), texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                Debug.Log("SCENE_PREVIEW " + Keys[i] + " " + watch.ElapsedMilliseconds + " ms");
            }
        }

        private static Board b;
        private static System.Random rng;
        private static int seed;

        // Palet: LinocutSceneFactory'nin altı tonundan türer; kar ve gece için soğuk ekler.
        private static readonly Color32 Snow = new Color32(0xE9, 0xE6, 0xDC, 0xFF);
        private static readonly Color32 SnowShade = new Color32(0xA9, 0xB4, 0xB8, 0xFF);
        private static readonly Color32 Night = new Color32(0x10, 0x16, 0x22, 0xFF);
        private static readonly Color32 Moon = new Color32(0xC9, 0xD2, 0xD0, 0xFF);
        private static readonly Color32 Lamp = new Color32(0xF0, 0xC0, 0x72, 0xFF);
        private static readonly Color32 River = new Color32(0x3C, 0x6B, 0x66, 0xFF);

        internal static Texture2D Render(int scene)
        {
            b = new Board(Width * S, Height * S);
            seed = 1943 + scene * 7919;
            rng = new System.Random(19430306 + scene * 911);
            lightDir = 1f;
            lightTone = Blend(Mustard, Paper, 0.45f);
            skyTone = Paper;

            switch (Keys[scene])
            {
                case "burned_village": BurnedVillage(); break;
                case "mountain_column": MountainColumn(); break;
                case "snow_ridge": SnowRidge(); break;
                case "pine_forest": PineForest(); break;
                case "burned_farm": BurnedFarm(); break;
                case "stream_ford": StreamFord(); break;
                case "night_camp": NightCamp(); break;
                case "typhus_barn": TyphusBarn(); break;
                case "barn_interior": BarnInterior(); break;
                case "barn_night": BarnNight(); break;
                case "river_gorge": RiverGorge(); break;
                case "broken_bridge": BrokenBridge(); break;
                case "bank_night": BankNight(); break;
                case "far_bank": FarBank(); break;
                default: KareliaScene(Keys[scene]); break;
            }

            Board final = Downsample(b);
            Finish(final);
            Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
            texture.SetPixels32(final.Pixels);
            texture.Apply(false, false);
            b = null;
            return texture;
        }

        // ================================================================== sahneler

        /// <summary>Prozor'un kuzeyi, şafak. Yanmış taş evler, ayakta kalan bacalar; kafile toplanıyor.</summary>
        private static void BurnedVillage()
        {
            lightDir = 1f;
            lightTone = Blend(Mustard, Rust, 0.25f);
            Color32 haze = Blend(Paper, Mustard, 0.22f);
            Sky(Blend(Paper, Petrol, 0.45f), Blend(Paper, Mustard, 0.28f), 520f, Blend(Mustard, Rust, 0.35f), 1380f, 560f, 520f, 0.55f, 0.55f);
            Range(Profile(600f, 150f, 0.0022f, 1, 0.8f), 330f, Blend(haze, Petrol, 0.30f), Blend(haze, Paper, 0.35f), Snow, Blend(SnowShade, haze, 0.4f), 90f, 0.55f, haze, 0.62f, haze, 90f, 1);
            float[] mid = Profile(480f, 110f, 0.003f, 2, 0.55f);
            Range(mid, 300f, Blend(Ink, Petrol, 0.35f), Blend(Paper, Mustard, 0.25f), Snow, SnowShade, 60f, 0.45f, haze, 0.40f, haze, 70f, 2, 1f, 55f);
            Forest(mid, 0.05f, 16f, 30f, Blend(Blend(Ink, Petrol, 0.5f), haze, 0.45f), 0.5f, 3, 6f);
            float[] ground = Profile(310f, 22f, 0.004f, 4, 0.2f);
            Snowfield(ground, 0f, Snow, SnowShade, 5);
            Track(new[] { new Vector2(360f, 300f), new Vector2(760f, 250f), new Vector2(1180f, 180f), new Vector2(1500f, 120f), new Vector2(1720f, 90f) }, 26f, 4f);

            // Köy: uzakta küçük taş evler, bacalardan ince duman.
            Color32 stone = Blend(Blend(Paper, Ink, 0.45f), haze, 0.25f);
            Color32 stoneDark = Blend(Ink, haze, 0.25f);
            float[] xs = { 520f, 610f, 720f, 830f, 960f };
            for (int i = 0; i < xs.Length; i++)
            {
                float y = At(ground, xs[i]) - 4f;
                float w = 58f + (float)rng.NextDouble() * 30f;
                Ruin(xs[i], y, w, 36f + (float)rng.NextDouble() * 12f, 22f, stone, stoneDark, 0.8f, 20 + i);
                Smoke(xs[i] + w * 0.3f, y + 60f, 0.5f + 0.2f * i % 2);
            }
            Bare(470f, At(ground, 470f) - 4f, 120f, Blend(Ink, haze, 0.35f));
            Bare(1080f, At(ground, 1080f) - 4f, 95f, Blend(Ink, haze, 0.40f));

            // Kafile: kağnı, yere konmuş sedye, yük alanlar.
            float gy = 150f;
            OxCart(1230f, gy + 18f, 150f, 1, Blend(Ink, Soot, 0.5f), true, true);
            Person(1130f, gy + 12f, 150f, Blend(Ink, Soot, 0.4f), Kind.Man, Carry.None, 2);
            StretcherOnGround(900f, gy - 10f, 150f, Blend(Ink, Soot, 0.35f), Blend(Rust, Ink, 0.45f));
            Person(1010f, gy - 18f, 160f, Blend(Ink, Soot, 0.45f), Kind.Woman, Carry.None, 0);
            Column(new[] { new Vector2(1320f, 170f), new Vector2(1480f, 140f), new Vector2(1640f, 118f), new Vector2(1760f, 100f) }, 18, 110f, 60f, Blend(Ink, Soot, 0.4f), Blend(Ink, haze, 0.25f), 5);

            // Ön plan: kadrajın solundan giren büyük yanmış ev ve Milena.
            Ruin(-40f, 30f, 360f, 300f, 140f, Blend(Paper, Ink, 0.62f), Blend(Ink, Soot, 0.55f), 1f, 40);
            Person(640f, 20f, 300f, Blend(Soot, Ink, 0.30f), Kind.Woman, Carry.Bag, 1);
            Drift(0f, 0f, 1672f, 70f, 7);
        }

        /// <summary>Rama yolu: dağın yamacını çapraz kesen yol ve üzerinde upuzun kafile.</summary>
        private static void MountainColumn()
        {
            lightDir = -1f;
            lightTone = Blend(Paper, Mustard, 0.25f);
            Color32 haze = Blend(Paper, Petrol, 0.18f);
            Sky(Blend(Paper, Petrol, 0.42f), Blend(Paper, Petrol, 0.10f), 560f, Blend(Paper, Mustard, 0.25f), 300f, 700f, 600f, 0.35f, 0.7f);
            Range(Profile(690f, 170f, 0.0019f, 10, 0.9f), 400f, Blend(haze, Petrol, 0.35f), Blend(haze, Paper, 0.45f), Snow, Blend(SnowShade, haze, 0.35f), 140f, 0.7f, haze, 0.55f, haze, 100f, 10);
            float[] mid = Profile(560f, 140f, 0.0024f, 11, 0.75f);
            Range(mid, 250f, Blend(Ink, Petrol, 0.40f), Blend(Paper, Ink, 0.25f), Snow, SnowShade, 110f, 0.6f, haze, 0.32f, haze, 80f, 11, 1f, 120f);
            Forest(mid, 0.035f, 14f, 26f, Blend(Blend(Ink, Petrol, 0.55f), haze, 0.35f), 0.6f, 12, 30f);

            // Yol: yamacı sol alttan sağ üste kesen açık bir şerit; kafile üzerinde.
            Vector2[] road = { new Vector2(-20f, 250f), new Vector2(300f, 300f), new Vector2(640f, 372f), new Vector2(980f, 420f), new Vector2(1300f, 470f), new Vector2(1700f, 520f) };
            Road(road, 16f, 4f, Blend(Snow, Paper, 0.3f), Blend(SnowShade, Ink, 0.15f));
            Column(road, 150, 30f, 9f, Blend(Soot, Ink, 0.3f), Blend(Ink, haze, 0.35f), 7);

            // Ön plan: sağdan giren kayalık yamaç, çamlar ve üç yakın figür.
            float[] fore = Line2(new Vector2(700f, -10f), new Vector2(1720f, 260f), 13, 18f);
            Range(fore, 0f, Blend(Soot, Ink, 0.4f), Blend(Ink, Paper, 0.30f), Snow, SnowShade, 40f, 0.55f, haze, 0f, haze, 0f, 13);
            for (int i = 0; i < 6; i++)
            {
                float x = 1180f + i * 90f + (float)rng.NextDouble() * 30f;
                Pine(x, At(fore, x) - 6f, 170f + (float)rng.NextDouble() * 90f, Blend(Soot, Petrol, 0.25f), 0.7f);
            }
            Snowfield(Line2(new Vector2(-10f, 170f), new Vector2(760f, 110f), 14, 10f), 0f, Snow, SnowShade, 14);
            Stretcher(430f, 60f, 230f, Blend(Soot, Ink, 0.3f), -1, Blend(Rust, Ink, 0.45f));
            Person(170f, 78f, 215f, Blend(Soot, Ink, 0.35f), Kind.Woman, Carry.Walk, 0);
            Drift(0f, 0f, 1672f, 50f, 15);
        }

        /// <summary>Açık sırt: kısa yol, gündüz ve her yerden görünür. Rüzgâr karı sırttan savurur.</summary>
        private static void SnowRidge()
        {
            lightDir = 1f;
            lightTone = Blend(Paper, Mustard, 0.30f);
            Color32 haze = Blend(Paper, Petrol, 0.15f);
            Sky(Blend(Paper, Petrol, 0.55f), Blend(Paper, Petrol, 0.12f), 300f, Paper, 1300f, 780f, 700f, 0.4f, 0.85f);
            Range(Profile(420f, 120f, 0.002f, 20, 0.9f), 200f, Blend(haze, Petrol, 0.35f), Blend(haze, Paper, 0.5f), Snow, Blend(SnowShade, haze, 0.3f), 110f, 0.8f, haze, 0.55f, haze, 80f, 20);

            // Sırt: soldan sağa yükselen, kadrajı ikiye bölen kar kütlesi.
            float[] crest = new float[Width + 2];
            for (int x = 0; x < crest.Length; x++)
            {
                float t = x / (float)Width;
                crest[x] = 300f + 250f * t + 30f * Mathf.Sin(t * 5.2f) + 14f * (Fbm(x * 0.01f, 0.5f, seed + 21, 3) - 0.5f);
            }
            Snowfield(crest, 0f, Snow, Blend(SnowShade, Petrol, 0.1f), 21);
            // Rüzgârın oyduğu uzun, yatık gölge bantları.
            for (int i = 0; i < 9; i++)
            {
                float y0 = 60f + i * 50f + (float)rng.NextDouble() * 20f;
                float x0 = -40f + (float)rng.NextDouble() * 500f;
                float len = 500f + (float)rng.NextDouble() * 700f;
                for (float t = 0f; t < 1f; t += 0.004f)
                {
                    float px = x0 + t * len, py = y0 + t * len * 0.16f;
                    if (py > At(crest, px) - 8f) continue;
                    Ellipse(px, py, 16f, 5f + 5f * Mathf.Sin(t * Mathf.PI), Blend(SnowShade, Petrol, 0.15f), 0.05f);
                }
            }
            Spindrift(crest, 22);

            // Kafile sırtın üstünde, gökyüzüne karşı koyu silüet.
            List<Vector2> path = new List<Vector2>();
            for (int x = 140; x <= 1560; x += 60) path.Add(new Vector2(x, At(crest, x) - 3f));
            Column(path.ToArray(), 70, 22f, 26f, Blend(Soot, Ink, 0.2f), Blend(Soot, Ink, 0.3f), 6);

            // Ön plan: rüzgârın oyduğu kar ve eğilerek yürüyen iki kişi.
            Drift(0f, 0f, 1672f, 150f, 23);
            WindStreaks(0f, 300f, 22);
            Person(300f, 70f, 250f, Blend(Soot, Ink, 0.3f), Kind.Man, Carry.Push, 2, 0.6f);
            Person(470f, 50f, 230f, Blend(Soot, Ink, 0.35f), Kind.Woman, Carry.Walk, 1, 0.8f);
        }

        /// <summary>Ağaç sınırının altı, alacakaranlık. Dar yol, bir fener ve kafilenin ortasında duran bir sedye.</summary>
        private static void PineForest()
        {
            lightDir = -1f;
            lightTone = Blend(Lamp, Paper, 0.3f);
            Color32 dusk = Blend(Night, Petrol, 0.45f);
            Sky(Blend(Night, Petrol, 0.25f), Blend(Petrol, Paper, 0.25f), 420f, Blend(Paper, Mustard, 0.2f), 840f, 520f, 420f, 0.4f, 0.2f);
            for (int layer = 0; layer < 4; layer++)
            {
                float depth = layer / 3f;
                Color32 tone = Blend(Blend(Night, Petrol, 0.35f), Blend(Petrol, Paper, 0.30f), 0.75f - depth * 0.7f);
                float baseY = 330f - layer * 40f;
                int count = 14 + layer * 6;
                for (int i = 0; i < count; i++)
                {
                    float x = (float)rng.NextDouble() * 1760f - 40f;
                    float h = (220f + layer * 140f) * (0.8f + 0.4f * (float)rng.NextDouble());
                    Pine(x, baseY - (float)rng.NextDouble() * 20f, h, tone, 0.35f - depth * 0.15f);
                }
                if (layer < 3) Mist(baseY + 30f, 60f, Blend(Petrol, Paper, 0.35f), 0.35f, 30 + layer);
            }
            float[] ground = Profile(190f, 14f, 0.004f, 34, 0.2f);
            Snowfield(ground, 0f, Blend(Snow, Petrol, 0.30f), Blend(SnowShade, Night, 0.45f), 34);
            Track(new[] { new Vector2(820f, 200f), new Vector2(760f, 150f), new Vector2(560f, 90f), new Vector2(300f, 20f), new Vector2(200f, -20f) }, 70f, 16f);
            Column(new[] { new Vector2(840f, 205f), new Vector2(900f, 230f), new Vector2(990f, 256f) }, 14, 60f, 30f, Blend(Night, Soot, 0.5f), Blend(Night, Petrol, 0.4f), 4);

            // Kafilenin ortasında durmuş bir sedye, yanında diz çökmüş bir kadın, fener.
            Glow(640f, 150f, 260f, Lamp, 0.45f);
            StretcherOnGround(620f, 110f, 190f, Blend(Night, Soot, 0.5f), Blend(Rust, Night, 0.35f));
            Kneel(760f, 108f, 180f, Blend(Night, Soot, 0.55f), -1);
            Lantern2(560f, 130f, 26f);
            Person(470f, 92f, 210f, Blend(Night, Soot, 0.6f), Kind.Man, Carry.None, 1);
            Person(1180f, 60f, 330f, Blend(Night, Soot, 0.7f), Kind.Man, Carry.Walk, 0, 0.7f);
            // Ön plan gövdeleri kadrajı çerçeveler.
            Trunk(40f, 70f, Blend(Night, Soot, 0.6f));
            Trunk(1560f, 95f, Blend(Night, Soot, 0.6f));
            Trunk(1420f, 46f, Blend(Night, Soot, 0.55f));
            Drift(0f, 0f, 1672f, 40f, 35);
        }

        /// <summary>Yolun kenarında yanmış bir çiftlik: ayakta kalan taş duvar, sağlam bir ambar, karda iki kürek.</summary>
        private static void BurnedFarm()
        {
            lightDir = 1f;
            lightTone = Blend(Paper, Mustard, 0.2f);
            Color32 haze = Blend(Paper, Ink, 0.12f);
            Sky(Blend(Paper, Ink, 0.30f), Blend(Paper, Ink, 0.08f), 450f, Paper, 1200f, 700f, 600f, 0.25f, 0.8f);
            Range(Profile(560f, 110f, 0.0024f, 40, 0.7f), 300f, Blend(haze, Ink, 0.30f), Blend(haze, Paper, 0.45f), Snow, Blend(SnowShade, haze, 0.3f), 80f, 0.6f, haze, 0.55f, haze, 90f, 40);
            float[] ground = Profile(300f, 16f, 0.003f, 41, 0.2f);
            Snowfield(ground, 0f, Snow, SnowShade, 41);
            Fence(new Vector2(-10f, 250f), new Vector2(560f, 205f), 12, 40f, Blend(Ink, haze, 0.25f));

            // Yanmış ev: iki katlı taş gövde, kömürleşmiş kirişler, boş pencereler.
            Ruin(780f, 150f, 420f, 250f, 120f, Blend(Paper, Ink, 0.48f), Blend(Ink, Soot, 0.5f), 1f, 42);
            Hambar(1320f, 175f, 150f, Blend(Ink, Rust, 0.18f));
            Bare(650f, 170f, 230f, Blend(Ink, Soot, 0.35f));
            // Ambarın altında üç çuval.
            for (int i = 0; i < 3; i++) Ellipse(1300f + i * 34f, 183f, 17f, 13f, Blend(Paper, Mustard, 0.35f));

            // Karda iki kürek ve bir kazma; etrafta duran üç kişi.
            Line(420f, 70f, 440f, 190f, 5f, Blend(Ink, Rust, 0.3f));
            Rect(426f, 60f, 454f, 86f, Blend(Ink, Soot, 0.3f));
            Line(500f, 60f, 486f, 176f, 5f, Blend(Ink, Rust, 0.3f));
            Rect(488f, 50f, 516f, 76f, Blend(Ink, Soot, 0.3f));
            Person(270f, 40f, 280f, Blend(Soot, Ink, 0.35f), Kind.Man, Carry.None, 1);
            Person(1050f, 80f, 240f, Blend(Soot, Ink, 0.45f), Kind.Woman, Carry.None, 0);
            Person(1170f, 88f, 230f, Blend(Soot, Ink, 0.5f), Kind.HatMan, Carry.None, 3);
            OxCart(1540f, 210f, 90f, -1, Blend(Ink, haze, 0.2f), true, false);
            Drift(0f, 0f, 1672f, 60f, 43);
        }

        /// <summary>Donmamış dere, buzlu taşlar; sedyeyi taşıyanlar belden aşağı suyun içinde.</summary>
        private static void StreamFord()
        {
            lightDir = -1f;
            lightTone = Blend(Paper, Petrol, 0.15f);
            Color32 haze = Blend(Paper, Petrol, 0.22f);
            Sky(Blend(Paper, Petrol, 0.50f), Blend(Paper, Petrol, 0.15f), 520f, Paper, 380f, 760f, 600f, 0.3f, 0.75f);
            Range(Profile(640f, 140f, 0.0021f, 50, 0.85f), 380f, Blend(haze, Petrol, 0.35f), Blend(haze, Paper, 0.45f), Snow, Blend(SnowShade, haze, 0.35f), 120f, 0.7f, haze, 0.55f, haze, 80f, 50);
            float[] far = Profile(420f, 40f, 0.004f, 51, 0.3f);
            Snowfield(far, 330f, Blend(Snow, haze, 0.2f), Blend(SnowShade, haze, 0.2f), 51);
            Forest(far, 0.07f, 40f, 80f, Blend(Blend(Soot, Petrol, 0.4f), haze, 0.35f), 0.6f, 52, 4f);
            Column(new[] { new Vector2(900f, 400f), new Vector2(1100f, 390f), new Vector2(1320f, 404f) }, 22, 44f, 34f, Blend(Ink, haze, 0.15f), Blend(Ink, haze, 0.35f), 5);

            // Dere: kadrajı soldan sağa kesen koyu şerit, kıyılarında buz.
            float[] waterTop = Line2(new Vector2(-10f, 360f), new Vector2(1700f, 330f), 53, 8f);
            Water(waterTop, 120f, Blend(River, Night, 0.25f), Blend(River, Paper, 0.25f), Snow, 0.8f, 53);
            float[] near = Profile(150f, 26f, 0.004f, 54, 0.2f);
            Snowfield(near, 0f, Snow, SnowShade, 54);
            IceEdge(near, 55);
            IceEdge(waterTop, 56, true);
            for (int i = 0; i < 9; i++)
            {
                float x = 120f + i * 170f + (float)rng.NextDouble() * 60f;
                float y = 170f + (float)rng.NextDouble() * 150f;
                Stone(x, y, 22f + (float)rng.NextDouble() * 24f, Blend(Ink, Petrol, 0.3f));
            }

            // Suyun içinde sedye: taşıyanların bacakları suya gömülü.
            Stretcher(860f, 205f, 230f, Blend(Soot, Ink, 0.35f), 1, Blend(Rust, Ink, 0.45f));
            WaterOver(640f, 1100f, 205f, 262f, Blend(River, Night, 0.2f), 0.7f, 57);
            Person(1280f, 150f, 240f, Blend(Soot, Ink, 0.4f), Kind.Woman, Carry.None, 0);
            Person(260f, 40f, 300f, Blend(Soot, Ink, 0.3f), Kind.Man, Carry.None, 1);
            Drift(0f, 0f, 1672f, 40f, 58);
        }

        /// <summary>Gece molası: rüzgâr almayan yamaç, ateş yok. Tek fenerin yanında bir defter el değiştiriyor.</summary>
        private static void NightCamp()
        {
            lightDir = 1f;
            lightTone = Blend(Moon, Paper, 0.3f);
            Sky(Night, Blend(Night, Petrol, 0.45f), 380f, Moon, 1330f, 760f, 260f, 0.35f, 0.35f);
            Stars(240, 420f);
            Ellipse(1330f, 760f, 34f, 34f, Blend(Moon, Paper, 0.4f), 0.9f);
            Range(Profile(520f, 150f, 0.0022f, 60, 0.85f), 250f, Blend(Night, Petrol, 0.35f), Blend(Night, Moon, 0.45f), Blend(Moon, Snow, 0.3f), Blend(Night, SnowShade, 0.4f), 120f, 0.7f, Blend(Night, Petrol, 0.4f), 0.35f, Blend(Night, Petrol, 0.4f), 60f, 60);
            float[] slope = Line2(new Vector2(-10f, 420f), new Vector2(1700f, 230f), 61, 20f);
            Snowfield(slope, 0f, Blend(Moon, SnowShade, 0.35f), Blend(Night, Petrol, 0.45f), 61);
            Forest(slope, 0.03f, 70f, 120f, Blend(Night, Soot, 0.4f), 0.35f, 62, 2f);

            // Uyuyanlar: karın içinde battaniye kümeleri ve sırt sırta oturan insanlar.
            Color32 dark = Blend(Night, Soot, 0.6f);
            Color32 blanket = Blend(Night, Rust, 0.25f);
            for (int i = 0; i < 12; i++)
            {
                float x = 120f + i * 125f + (float)rng.NextDouble() * 40f;
                float y = At(slope, x) - 70f - (float)rng.NextDouble() * 90f;
                if (x > 620f && x < 1000f) continue;
                if (i % 3 == 0) Seated(x, y, 90f, dark, true, i % 2 == 0 ? 1 : -1, blanket);
                else Lying(x, y, 110f, dark, blanket, i % 2 == 0 ? 1 : -1);
            }
            // Fenerin yanında Perić defteri uzatıyor.
            Glow(800f, 150f, 300f, Lamp, 0.5f);
            Lantern2(800f, 120f, 30f);
            Seated(700f, 90f, 200f, Blend(Night, Soot, 0.7f), true, 1, Blend(Night, Rust, 0.3f), true);
            Seated(910f, 92f, 210f, Blend(Night, Soot, 0.75f), false, -1, Blend(Night, Petrol, 0.3f), true);
            Drift(0f, 0f, 1672f, 40f, 63, 0.4f);
        }

        /// <summary>Ahır avlusu, alacakaranlık: açık kapıdan sıcak ışık, sundurma, kuyu çatalı.</summary>
        private static void TyphusBarn()
        {
            lightDir = -1f;
            lightTone = Blend(Lamp, Paper, 0.4f);
            Color32 dusk = Blend(Blend(Petrol, Rust, 0.2f), Paper, 0.3f);
            Sky(Blend(Night, Petrol, 0.4f), dusk, 420f, Blend(Rust, Mustard, 0.4f), 200f, 470f, 500f, 0.45f, 0.55f);
            Range(Profile(520f, 110f, 0.0023f, 70, 0.8f), 300f, Blend(Night, Petrol, 0.4f), Blend(dusk, Paper, 0.2f), Blend(Snow, dusk, 0.3f), Blend(SnowShade, Night, 0.35f), 90f, 0.6f, dusk, 0.35f, dusk, 70f, 70);
            float[] ground = Profile(300f, 14f, 0.004f, 71, 0.2f);
            Snowfield(ground, 0f, Blend(Snow, dusk, 0.25f), Blend(SnowShade, Night, 0.35f), 71);

            Barn(760f, 1260f, 285f, 560f, 880f, true, Blend(Night, Rust, 0.25f), 72);
            LeanTo(110f, 470f, 260f, 430f, Blend(Night, Soot, 0.4f), 73, true);
            for (int i = 0; i < 4; i++) Lying(170f + i * 80f, 262f, 78f, Blend(Night, Soot, 0.55f), Blend(Night, Petrol, 0.3f), i % 2 == 0 ? 1 : -1);
            SweepWell(560f, 110f, 300f, Blend(Night, Soot, 0.6f));
            Person(1380f, 150f, 230f, Blend(Night, Soot, 0.65f), Kind.Woman, Carry.Pot, 0);
            Person(300f, 40f, 300f, Blend(Night, Soot, 0.7f), Kind.Man, Carry.None, 1);
            Track(new[] { new Vector2(1200f, 290f), new Vector2(1000f, 200f), new Vector2(700f, 120f), new Vector2(400f, 30f) }, 50f, 14f);
            Drift(0f, 0f, 1672f, 40f, 74);
        }

        /// <summary>
        /// Ahırın içi, tek kaçış noktalı perspektif: arka duvarda karlı geceye açılan kapı,
        /// iki yanda derinliğe kaçan direkler ve tahtalar, tavanda kirişler. Ahırı boydan boya
        /// ikiye bölen asılı battaniye; iki yanda battaniye altında yatanlar. Fener tek ışık.
        /// </summary>
        private static void BarnInterior()
        {
            lightDir = 1f;
            lightTone = Lamp;
            Vector2 vp = new Vector2(836f, 430f);
            // Arka duvar dikdörtgeni; kenarları kaçış noktasına bağlanır.
            float bx0 = 560f, bx1 = 1110f, by0 = 190f, by1 = 610f;
            Color32 wood = Blend(Night, Rust, 0.32f);
            Color32 woodDark = Blend(Night, Soot, 0.35f);
            Color32 straw = Blend(Blend(Mustard, Rust, 0.3f), Night, 0.35f);
            for (int Y = 0; Y < b.H; Y++)
            for (int X = 0; X < b.W; X++)
            {
                float x = X / (float)S, y = Y / (float)S;
                Color32 c;
                bool back = x >= bx0 && x <= bx1 && y >= by0 && y <= by1;
                if (back)
                {
                    float plank = Mathf.Repeat(x - bx0, 30f) < 1.6f ? 0.5f : 0f;
                    c = Blend(Blend(woodDark, wood, 0.45f + 0.4f * Noise(x * 0.02f, y * 0.004f, seed + 1)), Soot, plank);
                }
                else
                {
                    // Hangi yüzeydeyiz: sol/sağ duvar, tavan, zemin (kaçış noktasına göre).
                    float dx = x - vp.x, dy = y - vp.y;
                    float sx = dx / (dx > 0 ? (bx1 - vp.x) : (bx0 - vp.x));
                    float sy = dy / (dy > 0 ? (by1 - vp.y) : (by0 - vp.y));
                    if (sy >= sx && dy < 0)
                    {
                        float n = Noise(dx / -dy * 22f, 1f / -dy * 900f, seed + 3);
                        c = Blend(straw, Blend(Mustard, Paper, 0.25f), n * 0.4f);
                        c = Blend(c, Night, Mathf.Clamp01(0.25f - dy / -600f));
                    }
                    else if (sy >= sx)
                    {
                        float u = dx / dy;
                        float plank = Mathf.Repeat(u * 40f, 6f) < 0.35f ? 0.5f : 0f;
                        c = Blend(Blend(Night, woodDark, 0.7f), Soot, plank);
                    }
                    else
                    {
                        float v = dy / Mathf.Abs(dx);
                        float depth = Mathf.Abs(dx);
                        float plank = Mathf.Repeat(v * 30f, 3.2f) < 0.16f ? 0.5f : 0f;
                        c = Blend(Blend(woodDark, wood, 0.35f + 0.35f * Noise(v * 10f, depth * 0.01f, seed + 4)), Soot, plank);
                        c = Blend(c, Night, Mathf.Clamp01(0.45f - depth / 1400f));
                    }
                }
                b.Pixels[Y * b.W + X] = c;
            }
            // Arka kapı: dışarıda karlı gece.
            float dx0 = 770f, dx1 = 900f, dy1 = 440f;
            Rect(dx0, by0, dx1, dy1, Blend(Night, Petrol, 0.55f));
            Rect(dx0, by0, dx1, by0 + 60f, Blend(Moon, SnowShade, 0.4f));
            for (int i = 0; i < 40; i++) Ellipse(dx0 + (float)rng.NextDouble() * (dx1 - dx0), by0 + 60f + (float)rng.NextDouble() * (dy1 - by0 - 60f), 1.3f, 1.3f, Moon, 0.6f);
            Line(dx0, by0, dx0, dy1, 7f, woodDark);
            Line(dx1, by0, dx1, dy1, 7f, woodDark);
            Line(dx0, dy1, dx1, dy1, 8f, woodDark);
            Poly(new[] { new Vector2(dx0, by0), new Vector2(dx1, by0), new Vector2(dx1 + 70f, by0 - 120f), new Vector2(dx0 - 90f, by0 - 120f) }, Blend(Moon, Petrol, 0.3f), 0.18f);

            // Derinliğe kaçan direkler ve tavan kirişleri.
            float[] depths = { 1f, 1.45f, 2.2f, 3.6f };
            for (int i = depths.Length - 1; i >= 0; i--)
            {
                float k = depths[i];
                float lx = vp.x + (bx0 - vp.x) * k, rx = vp.x + (bx1 - vp.x) * k;
                float fy = vp.y + (by0 - vp.y) * k, cy = vp.y + (by1 - vp.y) * k;
                float pw = 10f * k;
                Color32 post = Blend(woodDark, Night, 0.35f / k);
                Line(lx, fy, lx, cy, pw, post);
                Line(rx, fy, rx, cy, pw, post);
                Line(lx, cy, rx, cy, pw * 1.1f, post);
                Line(lx + pw * 0.3f, fy, lx + pw * 0.3f, cy, pw * 0.2f, Blend(post, Lamp, 0.3f), 0.35f);
                Line(lx, cy - pw * 1.6f, lx + (rx - lx) * 0.18f, cy, pw * 0.6f, post);
                Line(rx, cy - pw * 1.6f, rx - (rx - lx) * 0.18f, cy, pw * 0.6f, post);
            }

            // Ahırı boydan ikiye bölen battaniye: ipten sarkan, derinliğe kaçan bir perde.
            Vector2 nearTop = new Vector2(700f, 800f), farTop = new Vector2(800f, 590f);
            Vector2 nearBot = new Vector2(700f, 250f), farBot = new Vector2(800f, 290f);
            Line(nearTop.x - 60f, nearTop.y + 30f, vp.x - 10f, vp.y + 150f, 2.5f, Soot);
            List<Vector2> curtain = new List<Vector2> { nearTop, farTop };
            for (int i = 0; i <= 10; i++)
            {
                float t = 1f - i / 10f;
                Vector2 p = Vector2.Lerp(nearBot, farBot, t);
                curtain.Add(p + new Vector2(0f, (Noise(i * 1.7f, 3f, seed + 9) - 0.5f) * 40f * (1f - t * 0.6f)));
            }
            Poly(curtain, Blend(Rust, Night, 0.42f));
            for (int i = 1; i < 7; i++)
            {
                float t = i / 7f;
                Vector2 a = Vector2.Lerp(nearTop, farTop, t), c = Vector2.Lerp(nearBot, farBot, t);
                Line(a.x, a.y, c.x, c.y, 2.5f * (1f - t * 0.6f), Blend(Rust, Night, 0.62f), 0.7f);
                Line(a.x + 3f, a.y, c.x + 3f, c.y, 1.2f, Blend(Rust, Lamp, 0.3f), 0.25f);
            }

            // İki yanda yatanlar; uzaktakiler küçük.
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 6; i++)
                {
                    float k = 1.02f + i * 0.18f;
                    float x = vp.x + side * 170f * k;
                    float y = vp.y + (by0 - vp.y) * k + 16f;
                    if (y < 10f) continue;
                    float len = 150f * k;
                    Color32 cover = Blend(Blend(side < 0 ? Rust : Petrol, Night, 0.3f), Paper, 0.06f * i);
                    Lying(x, y, len, Blend(Night, Soot, 0.5f), cover, side);
                }

            // Fener ve ışık.
            Line(836f, 941f, 836f, 560f, 2.5f, Soot);
            Glow(836f, 500f, 700f, Lamp, 0.5f);
            Lantern2(836f, 520f, 36f);
            // Milena, ön planda, defteri elinde diz çökmüş.
            Kneel(1180f, 30f, 330f, Blend(Night, Soot, 0.8f), -1, true);
            Lying(1440f, 40f, 300f, Blend(Night, Soot, 0.7f), Blend(Petrol, Night, 0.4f), -1);
            Motes(560f, 300f, 1110f, 700f, 120, Blend(Lamp, Paper, 0.3f));
        }

        /// <summary>Gece kuyu başı ve sundurma: iki battaniye, on dört kişi, tek kova.</summary>
        private static void BarnNight()
        {
            lightDir = 1f;
            lightTone = Blend(Moon, Paper, 0.2f);
            Sky(Night, Blend(Night, Petrol, 0.50f), 420f, Moon, 1400f, 800f, 300f, 0.3f, 0.45f);
            Stars(180, 480f);
            Range(Profile(520f, 100f, 0.0025f, 90, 0.8f), 300f, Blend(Night, Petrol, 0.3f), Blend(Night, Moon, 0.4f), Blend(Moon, Snow, 0.25f), Blend(Night, SnowShade, 0.35f), 100f, 0.65f, Blend(Night, Petrol, 0.45f), 0.4f, Blend(Night, Petrol, 0.4f), 60f, 90);
            float[] ground = Profile(290f, 12f, 0.004f, 91, 0.2f);
            Snowfield(ground, 0f, Blend(Moon, SnowShade, 0.3f), Blend(Night, Petrol, 0.45f), 91);
            // Sağda ahır duvarı: kapı aralığından ince bir ışık.
            Barn(1260f, 1640f, 280f, 640f, 920f, false, Blend(Night, Soot, 0.5f), 92);
            Rect(1446f, 280f, 1456f, 555f, Lamp, 0.85f);
            Glow(1451f, 300f, 200f, Lamp, 0.25f);
            // Sundurma: altında yatanlar.
            LeanTo(120f, 900f, 230f, 470f, Blend(Night, Soot, 0.55f), 93, false);
            for (int i = 0; i < 7; i++)
                Lying(190f + i * 100f, 250f + (i % 2) * 10f, 95f, Blend(Night, Soot, 0.6f), i == 1 || i == 4 ? Blend(Rust, Night, 0.4f) : Blend(Night, Petrol, 0.3f), i % 2 == 0 ? 1 : -1);
            // Ön planda kuyu, kova ve kovayı taşıyan kadın.
            SweepWell(760f, 70f, 380f, Blend(Night, Soot, 0.7f));
            Person(1000f, 40f, 290f, Blend(Night, Soot, 0.75f), Kind.Woman, Carry.Pot, 0);
            Drift(0f, 0f, 1672f, 40f, 94, 0.4f);
        }

        /// <summary>Kanyonun kenarı: aşağıda ince bir şerit hâlinde Neretva, zikzak patika, bırakılan kağnı.</summary>
        private static void RiverGorge()
        {
            lightDir = 1f;
            lightTone = Blend(Paper, Mustard, 0.3f);
            Color32 haze = Blend(Paper, Petrol, 0.25f);
            Sky(Blend(Paper, Petrol, 0.45f), Blend(Paper, Petrol, 0.12f), 600f, Paper, 1200f, 820f, 600f, 0.35f, 0.75f);
            Range(Profile(780f, 130f, 0.002f, 100, 0.9f), 80f, Blend(haze, Petrol, 0.35f), Blend(haze, Paper, 0.45f), Snow, Blend(SnowShade, haze, 0.35f), 130f, 0.75f, haze, 0.55f, haze, 260f, 100);
            Range(Profile(560f, 120f, 0.0026f, 108, 0.8f), 80f, Blend(Ink, Petrol, 0.4f), Blend(Paper, Ink, 0.3f), Snow, SnowShade, 90f, 0.55f, haze, 0.42f, haze, 200f, 108, 1f, 120f);
            // İki yamaç derin bir V çizer; dipte nehir.
            float[] valley = new float[Width + 2];
            for (int x = 0; x < valley.Length; x++)
            {
                float t = (x - 1050f) / 640f;
                valley[x] = 110f + 560f * Mathf.Pow(Mathf.Abs(t), 0.9f) + 40f * (Fbm(x * 0.012f, 1.3f, seed + 101, 4) - 0.5f);
            }
            Range(valley, 0f, Blend(Ink, Petrol, 0.45f), Blend(Paper, Ink, 0.30f), Snow, SnowShade, 60f, 0.45f, haze, 0.22f, Blend(haze, Petrol, 0.2f), 60f, 103, 1.3f, 140f);
            // Vadi dibinde görünen nehir şeridi.
            Poly(new[] { new Vector2(1000f, 124f), new Vector2(1100f, 124f), new Vector2(1160f, 60f), new Vector2(940f, 60f) }, Blend(River, Paper, 0.3f));
            for (int i = 0; i < 18; i++)
            {
                float y = 64f + i * 3.3f;
                float half = 110f - i * 3f;
                Line(1050f - half * (0.3f + 0.6f * (float)rng.NextDouble()), y, 1050f + half * (0.3f + 0.6f * (float)rng.NextDouble()), y, 1f, Snow, 0.25f);
            }
            // Zikzak patika sol yamaçta; üzerinde küçük figürler.
            Vector2[] zig = { new Vector2(640f, 470f), new Vector2(840f, 410f), new Vector2(700f, 360f), new Vector2(900f, 290f), new Vector2(780f, 240f), new Vector2(960f, 170f), new Vector2(1000f, 128f) };
            Road(zig, 6f, 2f, Blend(Paper, Snow, 0.4f), Blend(Paper, Ink, 0.4f));
            Column(zig, 70, 12f, 9f, Blend(Soot, Ink, 0.3f), Blend(Ink, Soot, 0.3f), 8);
            Mist(200f, 50f, haze, 0.25f, 105);

            // Ön plan: karlı sahanlık, kenarından vadiye düşen kaya. Kağnı bırakılmış, sedye inmeye hazır.
            float[] ledge = new float[Width + 2];
            for (int x = 0; x < ledge.Length; x++)
                ledge[x] = x < 820f ? 150f + 18f * (Fbm(x * 0.01f, 7.3f, seed + 104, 3) - 0.5f) : 150f - (x - 820f) * 1.6f;
            Range(ledge, 0f, Blend(Soot, Ink, 0.35f), Blend(Ink, Paper, 0.3f), Snow, SnowShade, 40f, 0.7f, haze, 0f, haze, 0f, 104, 2.2f);
            float[] ledgeSnow = Line2(new Vector2(-10f, 140f), new Vector2(800f, 132f), 107, 6f);
            for (int x = 0; x < ledgeSnow.Length; x++) if (x > 800f) ledgeSnow[x] = Mathf.Min(ledgeSnow[x], 132f - (x - 800f) * 2f);
            Snowfield(ledgeSnow, 0f, Snow, SnowShade, 107);
            OxCart(200f, 120f, 150f, 1, Blend(Soot, Ink, 0.35f), false, true);
            Stretcher(590f, 70f, 250f, Blend(Soot, Ink, 0.3f), 1, Blend(Rust, Ink, 0.4f));
            Person(380f, 40f, 290f, Blend(Soot, Ink, 0.35f), Kind.Woman, Carry.Bag, 1);
        }

        /// <summary>Yıkık köprü: kanyonun içinden; orta açıklık suya sarkmış, üstüne tahta yol atılmış, tek sıra geçiliyor.</summary>
        private static void BrokenBridge()
        {
            lightDir = -1f;
            lightTone = Blend(Paper, Petrol, 0.2f);
            Color32 haze = Blend(Paper, Petrol, 0.28f);
            Sky(Blend(Paper, Petrol, 0.55f), Blend(Paper, Petrol, 0.2f), 560f, Paper, 760f, 860f, 620f, 0.3f, 0.85f);
            Range(Profile(840f, 150f, 0.0021f, 110, 0.9f), 560f, Blend(haze, Petrol, 0.35f), Blend(haze, Paper, 0.45f), Snow, Blend(SnowShade, haze, 0.35f), 120f, 0.7f, haze, 0.6f, haze, 70f, 110);
            Water(Line2(new Vector2(-10f, 240f), new Vector2(1700f, 240f), 111, 3f), 0f, Blend(River, Night, 0.3f), Blend(River, Paper, 0.28f), Snow, 1f, 111);
            // Uzak kanyon duvarları: ortada nehre inen V.
            float[] far = new float[Width + 2];
            for (int x = 0; x < far.Length; x++)
            {
                float d = Mathf.Abs(x - 836f) / 836f;
                far[x] = 236f + 720f * Mathf.Pow(d, 1.3f) + 60f * (Fbm(x * 0.012f, 3.3f, seed + 112, 4) - 0.5f);
            }
            Range(far, 200f, Blend(Ink, Petrol, 0.4f), Blend(Paper, Ink, 0.3f), Snow, SnowShade, 90f, 0.35f, haze, 0.35f, Blend(haze, Petrol, 0.2f), 110f, 112, 1.8f, 160f);
            Bridge(330f, 1340f, 560f, 1f, Blend(Soot, Ink, 0.35f), true);
            Mist(270f, 60f, Blend(Paper, Petrol, 0.2f), 0.55f, 114);
            Foam(360f, 1300f, 120f, 240f, 115);
            // Yakın duvarlar: iki yandan kadraja giren dik kayalar.
            float[] near = new float[Width + 2];
            for (int x = 0; x < near.Length; x++)
                near[x] = Mathf.Max(1100f - x * 2.6f, 1100f - (Width - x) * 2.6f) + 50f * (Fbm(x * 0.02f, 2.1f, seed + 113, 4) - 0.5f);
            Range(near, 0f, Blend(Soot, Ink, 0.35f), Blend(Ink, Paper, 0.12f), Snow, SnowShade, 90f, 0.3f, haze, 0.05f, Blend(haze, Petrol, 0.3f), 120f, 113, 2.4f);
        }

        /// <summary>Gece sol yaka: köprünün silüeti, ateşsiz bekleyiş, kıyıda sedyeler.</summary>
        private static void BankNight()
        {
            lightDir = 1f;
            lightTone = Blend(Moon, Paper, 0.2f);
            Sky(Night, Blend(Night, Petrol, 0.4f), 470f, Moon, 1250f, 820f, 280f, 0.3f, 0.5f);
            Stars(160, 560f);
            Range(Profile(700f, 130f, 0.0022f, 120, 0.9f), 430f, Blend(Night, Petrol, 0.3f), Blend(Night, Moon, 0.35f), Blend(Moon, Snow, 0.2f), Blend(Night, SnowShade, 0.3f), 110f, 0.6f, Blend(Night, Petrol, 0.4f), 0.4f, Blend(Night, Petrol, 0.4f), 60f, 120);
            Water(Line2(new Vector2(-10f, 360f), new Vector2(1700f, 350f), 121, 3f), 170f, Night, Blend(Night, Moon, 0.3f), Moon, 0.5f, 121);
            Bridge(200f, 1672f, 520f, 0.8f, Blend(Night, Soot, 0.6f), true);
            float[] bank = Profile(200f, 24f, 0.004f, 122, 0.3f);
            Snowfield(bank, 0f, Blend(Moon, SnowShade, 0.3f), Blend(Night, Petrol, 0.45f), 122);
            for (int i = 0; i < 16; i += 2) Stone(60f + i * 105f + (float)rng.NextDouble() * 40f, At(bank, 60f + i * 105f) - 10f - (float)rng.NextDouble() * 40f, 18f + (float)rng.NextDouble() * 20f, Blend(Night, Petrol, 0.35f));
            Color32 dark = Blend(Night, Soot, 0.75f);
            StretcherOnGround(460f, 110f, 190f, dark, Blend(Rust, Night, 0.4f));
            StretcherOnGround(860f, 90f, 200f, dark, Blend(Night, Petrol, 0.3f));
            Seated(320f, 80f, 180f, dark, true, 1, Blend(Night, Rust, 0.3f));
            Seated(640f, 60f, 190f, dark, false, 1, Blend(Night, Petrol, 0.3f));
            Seated(1080f, 70f, 200f, dark, true, -1, Blend(Night, Rust, 0.25f));
            Person(1350f, 40f, 300f, Blend(Night, Soot, 0.8f), Kind.Woman, Carry.None, 0);
            Drift(0f, 0f, 1672f, 30f, 123, 0.4f);
        }

        /// <summary>Şafakta karşı yaka: çakıllı kıyı, sıralanmış sedyeler, defterine yazan bir kadın; nehrin öbür yakasında köprü.</summary>
        private static void FarBank()
        {
            lightDir = 1f;
            lightTone = Blend(Mustard, Paper, 0.35f);
            Color32 haze = Blend(Paper, Mustard, 0.15f);
            Sky(Blend(Paper, Petrol, 0.40f), Blend(Paper, Mustard, 0.30f), 520f, Blend(Mustard, Rust, 0.3f), 1420f, 600f, 520f, 0.5f, 0.55f);
            Range(Profile(720f, 170f, 0.0021f, 130, 0.9f), 470f, Blend(haze, Petrol, 0.35f), Blend(haze, Paper, 0.5f), Snow, Blend(SnowShade, haze, 0.3f), 130f, 0.7f, haze, 0.42f, haze, 90f, 130);
            // Öbür yaka: gelinen taraf, ormanlı yamaç ve köprü.
            float[] opposite = Profile(520f, 90f, 0.003f, 134, 0.6f);
            Range(opposite, 380f, Blend(Ink, Petrol, 0.4f), Blend(Paper, Ink, 0.3f), Snow, SnowShade, 50f, 0.5f, haze, 0.25f, haze, 40f, 134, 1f, 40f);
            Water(Line2(new Vector2(-10f, 400f), new Vector2(1700f, 392f), 131, 3f), 250f, Blend(River, Night, 0.1f), Blend(River, Paper, 0.45f), Snow, 0.45f, 131);
            Bridge(980f, 1672f, 470f, 0.42f, Blend(Ink, haze, 0.3f), true);
            Mist(400f, 40f, haze, 0.45f, 132);
            // Çakıllı kıyı.
            float[] shore = Profile(262f, 16f, 0.004f, 133, 0.2f);
            Pebbles(shore, 133);
            Color32 dark = Blend(Soot, Ink, 0.35f);
            for (int i = 0; i < 5; i++)
                StretcherOnGround(250f + i * 190f, 185f - i * 6f, 150f, dark, Blend(Blend(Rust, Petrol, i * 0.25f), Ink, 0.35f));
            Seated(1120f, 150f, 170f, dark, false, -1, Blend(Petrol, Ink, 0.3f));
            Kneel(610f, 150f, 170f, dark, 1);
            // Milena, alçak bir taşın üstünde, defteri dizinde.
            Ellipse(1350f, 58f, 80f, 26f, Blend(Ink, Petrol, 0.3f));
            Ellipse(1360f, 70f, 60f, 10f, Blend(Paper, Ink, 0.4f), 0.6f);
            Seated(1350f, 70f, 270f, Blend(Soot, Ink, 0.25f), true, -1, Blend(Rust, Ink, 0.4f), true);
            Person(200f, 30f, 310f, Blend(Soot, Ink, 0.35f), Kind.Man, Carry.None, 1);
        }

        // ================================================================== gökyüzü ve arazi

        private static void Sky(Color32 top, Color32 horizon, float horizonY, Color32 glow, float gx, float gy, float gr, float glowAmt, float cloud)
        {
            Color32 cloudLit = Blend(horizon, Paper, 0.45f);
            Color32 cloudDark = Blend(top, Ink, 0.12f);
            for (int y = 0; y < b.H; y++)
            {
                float fy = y / (float)S;
                float t = Mathf.Clamp01((fy - horizonY * 0.6f) / (Height - horizonY * 0.6f));
                Color32 baseTone = Blend(horizon, top, Mathf.Pow(Smooth(0f, 1f, t), 0.9f));
                for (int x = 0; x < b.W; x++)
                {
                    float fx = x / (float)S;
                    Color32 c = baseTone;
                    float dx = (fx - gx) / gr, dy = (fy - gy) / (gr * 0.6f);
                    c = Blend(c, glow, glowAmt * Mathf.Exp(-(dx * dx + dy * dy) * 1.6f));
                    if (cloud > 0f && fy > horizonY * 0.55f)
                    {
                        float n = Fbm(fx * 0.0024f, fy * 0.0075f, seed + 900, 5);
                        float cover = Smooth(0.62f - cloud * 0.22f, 0.86f - cloud * 0.12f, n);
                        if (cover > 0.001f)
                        {
                            float shade = Fbm(fx * 0.0024f, (fy - 14f) * 0.0075f, seed + 900, 5);
                            float light = Mathf.Clamp01(0.5f + (n - shade) * 7f + 0.15f * lightDir * (fx - gx) / Width * -2f);
                            Color32 cc = Blend(cloudDark, cloudLit, light);
                            cc = Blend(cc, glow, glowAmt * 0.6f * Mathf.Exp(-(dx * dx + dy * dy) * 1.2f));
                            c = Blend(c, cc, cover * 0.75f);
                        }
                    }
                    b.Pixels[y * b.W + x] = c;
                }
            }
        }

        private static void Stars(int count, float minY)
        {
            for (int i = 0; i < count; i++)
            {
                float x = (float)rng.NextDouble() * Width;
                float y = minY + (float)rng.NextDouble() * (Height - minY);
                float r = 0.6f + (float)rng.NextDouble() * ((float)rng.NextDouble() < 0.1 ? 1.6f : 0.7f);
                Ellipse(x, y, r, r, Moon, 0.35f + (float)rng.NextDouble() * 0.55f);
            }
        }

        /// <summary>
        /// Sırt profili: yumuşak gürültü ile tepe gürültüsünün karışımı ve üstüne birkaç baskın
        /// zirve. Baskın zirve olmadan sıradağ bir duvar gibi okunuyordu.
        /// </summary>
        private static float[] Profile(float baseY, float amp, float freq, int s, float sharp)
        {
            float[] top = new float[Width + 2];
            System.Random local = new System.Random(seed * 31 + s);
            int peaks = 3 + local.Next(3);
            float[] px = new float[peaks], pw = new float[peaks], ph = new float[peaks];
            for (int k = 0; k < peaks; k++)
            {
                px[k] = (float)local.NextDouble() * Width;
                pw[k] = 140f + (float)local.NextDouble() * 320f;
                ph[k] = amp * (0.35f + 0.75f * (float)local.NextDouble());
            }
            for (int x = 0; x < top.Length; x++)
            {
                float soft = Fbm(x * freq, 0.37f, seed + s * 13, 5);
                float r = 0f, a = 0.5f, f = freq * 1.3f;
                for (int o = 0; o < 4; o++)
                {
                    float n = Noise(x * f, 3.1f + o, seed + s * 17 + o);
                    r += a * (1f - Mathf.Abs(2f * n - 1f));
                    a *= 0.5f;
                    f *= 2.1f;
                }
                float y = baseY + amp * 0.7f * (Mathf.Lerp(soft, r * 1.1f, sharp) - 0.45f);
                for (int k = 0; k < peaks; k++)
                {
                    float d = Mathf.Abs(x - px[k]) / pw[k];
                    if (d < 1f) y += ph[k] * Mathf.Pow(1f - d, 1.7f);
                }
                top[x] = y;
            }
            return top;
        }

        private static float[] Line2(Vector2 a, Vector2 c, int s, float wobble)
        {
            float[] top = new float[Width + 2];
            for (int x = 0; x < top.Length; x++)
            {
                float t = Mathf.InverseLerp(a.x, c.x, x);
                top[x] = Mathf.Lerp(a.y, c.y, t) + wobble * (Fbm(x * 0.008f, 0.9f, seed + s * 7, 4) - 0.5f) * 2f;
            }
            return top;
        }

        private static float At(float[] top, float x)
        {
            if (x <= 0f) return top[0];
            if (x >= top.Length - 1) return top[top.Length - 1];
            int i = (int)x;
            return Mathf.Lerp(top[i], top[i + 1], x - i);
        }

        /// <summary>Kaburga gürültüsü: 1'e yakın değerler sırt kaburgası, 0'a yakın değerler oluk.</summary>
        private static float Ridged(float x, float y, int s)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int o = 0; o < 4; o++)
            {
                float n = 1f - Mathf.Abs(2f * Noise(x, y, s + o * 57) - 1f);
                sum += amp * n * n;
                norm += amp;
                amp *= 0.5f;
                x *= 2.1f;
                y *= 2.1f;
            }
            return sum / norm;
        }

        /// <summary>
        /// Dağ katmanı. Yamaç, zirveden aşağı inen kaburgalar ve aralarındaki oluklardan kurulur;
        /// ışık bu iki boyutlu alanın eğiminden gelir, sütunun sırt eğiminden değil (o yaklaşım
        /// dağları perde gibi dikey şeritlere bölüyordu). Kar önce oluklara ve zirveye yakın
        /// yerlere yağar; kaburgalar koyu kaya olarak kalır. İsteğe bağlı orman bandı, ağaç
        /// uçlarıyla kesilen bir üst çizgiyle yamacın alt kısmını kaplar. Uzaklıkla pus, dipte sis.
        /// Renk tasarım pikseli başına bir kez hesaplanır; yalnız sırt çizgisi iki kat
        /// çözünürlükte kenar yumuşatılır.
        /// </summary>
        private static void Range(float[] top, float bottom, Color32 rockShade, Color32 rockLit, Color32 snowLit, Color32 snowShade,
            float snowDepth, float snowCover, Color32 haze, float hazeAmt, Color32 mist, float mistH, int s, float detail = 1f, float forest = 0f)
        {
            Color32 rim = Blend(lightTone, Paper, 0.3f);
            Color32 forestTone = Blend(Blend(rockShade, Soot, 0.35f), Petrol, 0.12f);
            float ribFreq = 0.011f * detail;
            for (int xd = 0; xd < Width; xd++)
            {
                float x = xd + 0.5f;
                float ty = At(top, x);
                float slope = (At(top, x + 6f) - At(top, x - 6f)) / 12f;
                float crestLit = Mathf.Clamp(-slope * lightDir * 2f, -1f, 1f);
                float sd = snowDepth * (0.55f + 0.9f * Noise(x * 0.012f, 0.2f, seed + s + 3));
                float forestLine = forest * (0.75f + 0.5f * Noise(x * 0.008f, 9.1f, seed + s + 4));
                int yTop = Mathf.Min(Height - 1, Mathf.FloorToInt(ty));
                for (int yd = Mathf.Max(0, Mathf.FloorToInt(bottom)); yd <= yTop; yd++)
                {
                    float y = yd + 0.5f;
                    float depth = ty - y;
                    float warp = (Fbm(x * 0.004f, y * 0.004f, seed + s + 40, 3) - 0.5f) * 90f;
                    float u = (x + warp + depth * 0.25f * lightDir) * ribFreq;
                    float v = y * 0.0068f;
                    float rib = Ridged(u, v, seed + s);
                    float ribR = Ridged(u + 0.05f, v + 0.02f, seed + s);
                    float fine = Noise(x * 0.06f, y * 0.05f, seed + s + 7);
                    float lit = Mathf.Clamp((ribR - rib) * 9f * Mathf.Min(1f, detail) * lightDir + crestLit * 0.5f * Mathf.Exp(-depth / 60f) + (fine - 0.5f) * 0.12f, -1f, 1f);
                    // Lino baskı: ışık dört düz tona ayrılır, tonlar arası geçiş dar.
                    Color32 c = Blend(rockShade, rockLit, Quant(0.5f + 0.5f * lit, 4));
                    // Kar: zirveye yakın her yer, aşağıda yalnız oluklar.
                    float alt = 1f - Smooth(sd * 0.2f, sd, depth);
                    float gully = 1f - rib;
                    if (snowCover > 0f && gully * 0.95f + (fine - 0.5f) * 0.2f > 1.02f - snowCover * 0.45f - alt * 0.75f)
                        c = Blend(snowShade, snowLit, Quant(0.5f + 0.65f * lit, 3));
                    // Orman bandı: ağaç uçlarıyla kesilen üst çizgi, koyu ve tanecikli.
                    if (forest > 0f)
                    {
                        float tips = Noise(x * 0.45f, 1.7f, seed + s + 8) * 5f + Noise(x * 0.09f, 2.7f, seed + s + 9) * 8f;
                        if (depth > forestLine + tips)
                        {
                            // Ağaç tepeleri: küçük, yumuşak lekeler; aralarında karlı açıklıklar.
                            float crowns = Fbm(x * 0.11f, y * 0.075f, seed + s + 10, 2);
                            float clearing = Fbm(x * 0.012f, y * 0.02f, seed + s + 12, 3);
                            c = Blend(forestTone, Blend(forestTone, snowShade, 0.55f), Smooth(0.55f, 0.72f, crowns) * 0.55f);
                            c = Blend(c, Soot, Smooth(0.45f, 0.25f, crowns) * 0.25f);
                            float clearing2 = Fbm(x * 0.03f, y * 0.05f, seed + s + 13, 3);
                            if (clearing > 0.6f && clearing2 > 0.62f) c = Blend(c, Blend(snowShade, snowLit, 0.4f), Smooth(0.62f, 0.68f, clearing2) * 0.6f);
                            c = Blend(c, rockLit, Mathf.Clamp01(lit) * 0.10f);
                        }
                    }
                    if (depth < 1.4f && crestLit > 0.1f) c = Blend(c, rim, 0.6f * crestLit);
                    if (hazeAmt > 0f) c = Blend(c, haze, hazeAmt);
                    if (mistH > 0f) c = Blend(c, mist, 0.85f * Smooth(bottom + mistH, bottom, y));
                    // 2×2 alt pikseller; sırt çizgisinde kısmi örtüş.
                    for (int sx = 0; sx < S; sx++)
                    {
                        float tyS = At(top, xd + (sx + 0.5f) / S) * S;
                        for (int sy = 0; sy < S; sy++)
                        {
                            int X = xd * S + sx, Y = yd * S + sy;
                            float cover = Mathf.Clamp01(tyS - Y);
                            if (cover > 0f) Put(X, Y, c, cover);
                        }
                    }
                }
            }
        }

        /// <summary>Kar alanı: ışıkta sıcak beyaz, çukurlarda mavi gölge; rüzgârın bıraktığı ince izler.</summary>
        private static void Snowfield(float[] top, float bottom, Color32 lit, Color32 shade, int s)
        {
            for (int xd = 0; xd < Width; xd++)
            {
                float x = xd + 0.5f;
                float ty = At(top, x);
                float slope = (At(top, x + 4f) - At(top, x - 4f)) / 8f;
                int yTop = Mathf.Min(Height - 1, Mathf.FloorToInt(ty));
                for (int yd = Mathf.Max(0, Mathf.FloorToInt(bottom)); yd <= yTop; yd++)
                {
                    float y = yd + 0.5f;
                    float n = Fbm(x * 0.004f, y * 0.016f, seed + s, 4);
                    float ripple = Noise(x * 0.03f + n * 4f, y * 0.28f, seed + s + 5);
                    float lip = Smooth(10f, 0f, ty - y) * 0.25f;
                    float edgeShade = Mathf.Exp(-(ty - y) / 22f);
                    float l = Mathf.Clamp01(0.66f + (n - 0.5f) * 0.75f - slope * lightDir * 1.5f * edgeShade + (ripple - 0.5f) * 0.10f - lip);
                    Color32 c = Blend(shade, lit, l);
                    for (int sx = 0; sx < S; sx++)
                    {
                        float tyS = At(top, xd + (sx + 0.5f) / S) * S;
                        for (int sy = 0; sy < S; sy++)
                        {
                            int X = xd * S + sx, Y = yd * S + sy;
                            float cover = Mathf.Clamp01(tyS - Y);
                            if (cover > 0f) Put(X, Y, c, cover);
                        }
                    }
                }
            }
        }

        /// <summary>Ön planda kadrajın altını kapatan kar tümseği.</summary>
        private static void Drift(float x0, float y0, float x1, float h, int s, float dim = 0f)
        {
            float[] top = new float[Width + 2];
            for (int x = 0; x < top.Length; x++) top[x] = y0 + h * (0.55f + 0.45f * Fbm(x * 0.004f, 5.5f, seed + s, 4));
            Color32 lit = Blend(Snow, Night, dim);
            Color32 shade = Blend(SnowShade, Night, dim + 0.1f);
            Snowfield(top, y0, lit, shade, s);
        }

        private static void Mist(float cy, float h, Color32 tone, float alpha, int s)
        {
            int y0 = Mathf.Max(0, (int)((cy - h * 2.2f) * S));
            int y1 = Mathf.Min(b.H - 1, (int)((cy + h * 2.2f) * S));
            for (int Y = y0; Y <= y1; Y++)
            {
                float y = Y / (float)S;
                float band = Mathf.Exp(-((y - cy) / h) * ((y - cy) / h));
                for (int X = 0; X < b.W; X++)
                {
                    float x = X / (float)S;
                    float n = Fbm(x * 0.0035f, y * 0.02f, seed + s, 4);
                    Put(X, Y, tone, alpha * band * Mathf.Clamp01(n * 1.6f - 0.25f));
                }
            }
        }

        /// <summary>Sırttan savrulan kar: tepe çizgisinin rüzgâraltına doğru uzayan saydam tüyler.</summary>
        private static void Spindrift(float[] crest, int s)
        {
            for (int X = 0; X < b.W; X++)
            {
                float x = X / (float)S;
                float ty = At(crest, x);
                for (int Y = Mathf.FloorToInt(ty * S); Y < Mathf.Min(b.H, (int)((ty + 110f) * S)); Y++)
                {
                    float y = Y / (float)S;
                    float up = y - ty;
                    float n = Fbm(x * 0.006f - up * 0.02f, up * 0.03f, seed + s, 4);
                    float a = Mathf.Clamp01(n * 1.8f - 0.55f) * Mathf.Exp(-up / 45f) * 0.8f;
                    Put(X, Y, Snow, a);
                }
            }
        }

        private static void WindStreaks(float y0, float y1, int s)
        {
            for (int i = 0; i < 70; i++)
            {
                float x = (float)rng.NextDouble() * Width;
                float y = y0 + (float)rng.NextDouble() * (y1 - y0);
                float len = 60f + (float)rng.NextDouble() * 180f;
                for (int k = 0; k < 3; k++) Line(x + k * len * 0.2f, y + k * 0.8f, x + len * (0.6f + k * 0.2f), y + len * 0.05f + k * 0.8f, 2.5f - k * 0.6f, Snow, (0.05f + (float)rng.NextDouble() * 0.07f));
            }
        }

        private static void Forest(float[] top, float density, float hMin, float hMax, Color32 tone, float snow, int s, float sink)
        {
            for (float x = 0f; x < Width; x += 1f)
            {
                if (Noise(x * 0.01f, 7.7f, seed + s) < 0.45f) continue;
                if (rng.NextDouble() > density) continue;
                float h = Mathf.Lerp(hMin, hMax, (float)rng.NextDouble());
                Pine(x, At(top, x) - sink - (float)rng.NextDouble() * sink * 2f, h, tone, snow);
            }
        }

        /// <summary>Katlı, dalları kar tutmuş çam; karı ışık tarafında taşır.</summary>
        private static void Pine(float x, float baseY, float h, Color32 tone, float snow)
        {
            int tiers = Mathf.Clamp((int)(h / 22f), 3, 9);
            float w = h * 0.30f;
            Line(x, baseY - 2f, x, baseY + h * 0.2f, Mathf.Max(1.2f, h * 0.035f), tone);
            for (int i = 0; i < tiers; i++)
            {
                float t = i / (float)tiers;
                float yb = baseY + h * (0.12f + 0.78f * t);
                float th = h * 0.30f * (1f - t * 0.35f);
                float tw = w * (1f - t * 0.82f) * (0.9f + 0.2f * (float)rng.NextDouble());
                float droop = th * 0.18f;
                Poly(new[] { new Vector2(x - tw, yb - droop), new Vector2(x - tw * 0.35f, yb + th * 0.15f), new Vector2(x, yb + th),
                    new Vector2(x + tw * 0.35f, yb + th * 0.15f), new Vector2(x + tw, yb - droop), new Vector2(x, yb + th * 0.02f) }, tone);
                if (snow > 0f && rng.NextDouble() < 0.45f + snow)
                {
                    // Kar yalnız dalın üst kenarında ince bir şerit; ışık tarafında daha dolgun.
                    float d = lightDir;
                    float th2 = Mathf.Min(th * (0.10f + 0.08f * snow), 4f + h * 0.006f);
                    Poly(new[] { new Vector2(x, yb + th), new Vector2(x + d * tw * 0.96f, yb - droop * 0.9f), new Vector2(x + d * tw * 0.8f, yb - droop * 0.9f + th2 * 0.6f),
                        new Vector2(x + d * tw * 0.12f, yb + th - th2 * 1.6f) }, Blend(tone, Snow, 0.6f), Mathf.Clamp01(snow));
                    Poly(new[] { new Vector2(x, yb + th), new Vector2(x - d * tw * 0.9f, yb - droop * 0.9f), new Vector2(x - d * tw * 0.8f, yb - droop * 0.9f + th2 * 0.4f),
                        new Vector2(x - d * tw * 0.1f, yb + th - th2 * 1.2f) }, Blend(SnowShade, tone, 0.35f), Mathf.Clamp01(snow * 0.8f));
                }
            }
        }

        private static void Trunk(float x, float w, Color32 tone)
        {
            Poly(new[] { new Vector2(x - w * 0.5f, -10f), new Vector2(x + w * 0.5f, -10f), new Vector2(x + w * 0.42f, 960f), new Vector2(x - w * 0.42f, 960f) }, tone);
            for (int i = 0; i < 8; i++)
            {
                float y = 80f + i * 110f + (float)rng.NextDouble() * 40f;
                Line(x - w * 0.4f, y, x + w * 0.4f, y + 8f, 1.5f, Blend(tone, Moon, 0.15f), 0.35f);
            }
            Line(x + lightDir * w * 0.38f, -10f, x + lightDir * w * 0.34f, 960f, 3f, Blend(tone, lightTone, 0.35f), 0.5f);
        }

        private static void Bare(float x, float baseY, float h, Color32 tone)
        {
            HungerWinterSceneFactory.Elm(b, rng, P(x), P(baseY), (int)(h * S), tone);
        }

        // ================================================================== su, buz, taş

        private static void Water(float[] top, float bottom, Color32 deep, Color32 light, Color32 foam, float turbulence, int s)
        {
            for (int X = 0; X < b.W; X++)
            {
                float x = (X + 0.5f) / S;
                float ty = At(top, x);
                int yTop = Mathf.Min(b.H - 1, Mathf.FloorToInt(ty * S));
                for (int Y = Mathf.Max(0, (int)(bottom * S)); Y <= yTop; Y++)
                {
                    float y = (Y + 0.5f) / S;
                    float t = Mathf.Clamp01((ty - y) / Mathf.Max(1f, ty - bottom));
                    Color32 c = Blend(light, deep, Mathf.Pow(t, 0.6f));
                    float r = Fbm(x * 0.008f + y * 0.002f, y * 0.22f, seed + s, 4);
                    if (r > 0.6f) c = Blend(c, Blend(light, Paper, 0.4f), (r - 0.6f) * 2.2f);
                    else if (r < 0.36f) c = Blend(c, deep, (0.36f - r) * 1.8f);
                    if (turbulence > 0f)
                    {
                        float f = Fbm(x * 0.03f, y * 0.12f, seed + s + 3, 3);
                        if (f > 0.74f - turbulence * 0.06f) c = Blend(c, foam, Mathf.Clamp01((f - 0.7f) * 4f) * turbulence * 0.8f);
                    }
                    b.Pixels[Y * b.W + X] = c;
                }
            }
        }

        /// <summary>Suya giren figürlerin bacaklarını örten yarı saydam su katmanı.</summary>
        private static void WaterOver(float x0, float x1, float y0, float y1, Color32 tone, float alpha, int s)
        {
            for (int Y = (int)(y0 * S); Y < (int)(y1 * S); Y++)
            for (int X = Mathf.Max(0, (int)(x0 * S)); X < Mathf.Min(b.W, (int)(x1 * S)); X++)
            {
                float x = X / (float)S, y = Y / (float)S;
                float wave = y1 - 4f * Mathf.Sin(x * 0.05f) - 3f * Fbm(x * 0.02f, 1f, seed + s, 3);
                if (y > wave) continue;
                float edgeFade = Smooth(x0, x0 + 60f, x) * Smooth(x1, x1 - 60f, x);
                Put(X, Y, tone, alpha * edgeFade);
                if (y > wave - 2f) Put(X, Y, Snow, 0.5f * edgeFade);
            }
        }

        private static void Foam(float x0, float x1, float y0, float y1, int s)
        {
            for (int i = 0; i < 260; i++)
            {
                float x = x0 + (float)rng.NextDouble() * (x1 - x0);
                float y = y0 + (float)rng.NextDouble() * (y1 - y0);
                float len = 10f + (float)rng.NextDouble() * 50f;
                Line(x, y, x + len, y + (float)rng.NextDouble() * 2f, 1.2f, Snow, 0.2f + (float)rng.NextDouble() * 0.35f);
            }
        }

        private static void IceEdge(float[] top, int s, bool below = false)
        {
            for (int X = 0; X < b.W; X++)
            {
                float x = X / (float)S;
                float ty = At(top, x);
                float w = 4f + 10f * Fbm(x * 0.02f, 4.4f, seed + s, 3);
                for (float d = 0f; d < w; d += 0.5f)
                    Put(X, (int)((below ? ty - d : ty + d) * S), Blend(Snow, Moon, 0.2f), 0.75f * (1f - d / w));
            }
        }

        private static void Stone(float x, float y, float r, Color32 tone)
        {
            Ellipse(x, y, r, r * 0.45f, tone);
            Ellipse(x + lightDir * r * 0.25f, y + r * 0.12f, r * 0.55f, r * 0.2f, Blend(tone, Paper, 0.15f), 0.7f);
            Ellipse(x - lightDir * r * 0.1f, y + r * 0.36f, r * 0.6f, r * 0.09f, Blend(Snow, tone, 0.35f), 0.75f);
        }

        private static void Pebbles(float[] top, int s)
        {
            Snowfield(top, 0f, Blend(Paper, Mustard, 0.15f), Blend(Paper, Ink, 0.45f), s);
            for (int i = 0; i < 900; i++)
            {
                float x = (float)rng.NextDouble() * Width;
                float y = (float)rng.NextDouble() * At(top, x);
                float r = 2f + (float)rng.NextDouble() * (7f - y / 60f);
                if (r < 1.2f) continue;
                Color32 c = Blend(Blend(Paper, Ink, 0.35f + (float)rng.NextDouble() * 0.4f), Petrol, (float)rng.NextDouble() * 0.3f);
                Ellipse(x, y, r, r * 0.6f, c, 0.85f);
                Ellipse(x + lightDir * r * 0.2f, y + r * 0.2f, r * 0.5f, r * 0.2f, Paper, 0.35f);
            }
        }

        // ================================================================== yollar ve kalabalık

        private static void Road(Vector2[] path, float wNear, float wFar, Color32 tone, Color32 edge)
        {
            float total = PathLength(path);
            for (float d = 0f; d < total; d += 0.8f)
            {
                Vector2 p = PathPoint(path, d);
                float t = d / total;
                float w = Mathf.Lerp(wNear, wFar, t);
                Ellipse(p.x, p.y, w * 1.25f, w * 0.5f, edge, 0.5f);
                Ellipse(p.x, p.y + w * 0.1f, w, w * 0.4f, tone);
            }
        }

        private static void Track(Vector2[] path, float wNear, float wFar)
        {
            float total = PathLength(path);
            for (float d = 0f; d < total; d += 1f)
            {
                Vector2 p = PathPoint(path, d);
                float w = Mathf.Lerp(wFar, wNear, d / total);
                float n = Noise(d * 0.05f, 2.2f, seed + 77);
                Ellipse(p.x, p.y, w, w * 0.22f, SnowShade, 0.25f + n * 0.25f);
            }
        }

        /// <summary>
        /// Yol boyunca kafile: yakından uzağa küçülen figürler; aralarında belli aralıklarla
        /// sedye çiftleri. Uzak figürler iki kat çözünürlükte bile birkaç piksel kalır.
        /// </summary>
        private static void Column(Vector2[] path, int count, float hStart, float hEnd, Color32 near, Color32 far, int stretcherEvery)
        {
            float total = PathLength(path);
            for (int i = 0; i < count; i++)
            {
                float t = (i + (float)rng.NextDouble() * 0.6f) / count;
                // Kafile bir çit gibi eşit aralıklı yürümez: yer yer kümelenir, yer yer açılır.
                float bunch = Fbm(t * 9f, 0.5f, seed + 301, 2);
                if (bunch < 0.34f && rng.NextDouble() < 0.7) continue;
                t = Mathf.Clamp01(t + (bunch - 0.5f) * 0.6f / count);
                Vector2 p = PathPoint(path, t * total);
                float h = Mathf.Lerp(hStart, hEnd, t) * (0.85f + 0.3f * (float)rng.NextDouble());
                Color32 tone = Blend(near, far, t);
                Vector2 q = PathPoint(path, Mathf.Min(total, t * total + 4f));
                int dir = q.x >= p.x ? 1 : -1;
                if (stretcherEvery > 0 && i % stretcherEvery == stretcherEvery - 1 && h > 9f)
                {
                    if (h > 40f) Stretcher(p.x, p.y, h, tone, dir, Blend(tone, Rust, 0.2f));
                    else
                    {
                        Tiny(p.x - dir * h * 0.3f, p.y, h, tone);
                        Tiny(p.x + dir * h * 0.3f, p.y, h, tone);
                        Line(p.x - dir * h * 0.42f, p.y + h * 0.55f, p.x + dir * h * 0.42f, p.y + h * 0.55f, Mathf.Max(1f, h * 0.12f), tone);
                    }
                }
                else if (h < 26f) Tiny(p.x, p.y, h, tone);
                else Person(p.x, p.y, h, tone, i % 3 == 0 ? Kind.Woman : Kind.Man, (i % 4 == 0) ? Carry.Bag : Carry.Walk, dir > 0 ? 1 + i % 2 * 3 : 0, 0.6f + 0.4f * (i % 2));
            }
        }

        private static void Tiny(float x, float y, float h, Color32 tone)
        {
            float lean = h * 0.06f;
            Poly(new[] { new Vector2(x - h * 0.13f, y + h * 0.18f), new Vector2(x + h * 0.13f, y + h * 0.18f), new Vector2(x + h * 0.10f + lean, y + h * 0.74f), new Vector2(x - h * 0.08f + lean, y + h * 0.76f) }, tone);
            Line(x - h * 0.05f, y, x - h * 0.04f, y + h * 0.2f, Mathf.Max(0.6f, h * 0.07f), tone);
            Line(x + h * 0.05f, y, x + h * 0.04f, y + h * 0.2f, Mathf.Max(0.6f, h * 0.07f), tone);
            Ellipse(x + lean * 1.2f, y + h * 0.86f, Mathf.Max(0.6f, h * 0.09f), Mathf.Max(0.6f, h * 0.1f), tone);
        }

        private static float PathLength(Vector2[] path)
        {
            float total = 0f;
            for (int i = 1; i < path.Length; i++) total += Vector2.Distance(path[i - 1], path[i]);
            return total;
        }

        private static Vector2 PathPoint(Vector2[] path, float d)
        {
            for (int i = 1; i < path.Length; i++)
            {
                float seg = Vector2.Distance(path[i - 1], path[i]);
                if (d <= seg) return Vector2.Lerp(path[i - 1], path[i], seg <= 0f ? 0f : d / seg);
                d -= seg;
            }
            return path[path.Length - 1];
        }

        // ================================================================== insanlar

        private static void Person(float x, float y, float h, Color32 tone, Kind kind, Carry carry, int variant, float gait = 1f)
        {
            Ellipse(x + lightDir * -h * 0.12f, y + 1f, h * 0.22f, h * 0.03f, Ink, 0.22f);
            HungerWinterSceneFactory.Person(b, rng, P(x), P(y), h * S, tone, kind, carry, variant, gait);
        }

        /// <summary>
        /// Sedye taşıyan çift: öndeki çeker, arkadaki iter; iki sırık ellerin arasında, üstünde
        /// battaniyeye sarılı bir yaralı. Yaralının yüzü çizilmez.
        /// </summary>
        private static void Stretcher(float x, float y, float h, Color32 tone, int dir, Color32 blanket)
        {
            float half = h * 0.5f;
            int vFront = dir > 0 ? 1 : 0;
            int vRear = dir > 0 ? 4 : 3;
            float xf = x + dir * half, xr = x - dir * half;
            Person(xr, y, h * 0.98f, tone, Kind.Man, Carry.Push, vRear, 0.8f);
            Person(xf, y, h, tone, Kind.Woman, Carry.Pull, vFront, 0.8f);
            float lean = 0.07f * dir;
            Vector2 rearHand = new Vector2(xr + dir * h * 0.20f + lean * h * 0.55f, y + h * 0.56f);
            Vector2 frontHand = new Vector2(xf - dir * h * 0.19f, y + h * 0.54f);
            Vector2 a = rearHand - new Vector2(dir * h * 0.05f, 0f);
            Vector2 c = frontHand + new Vector2(dir * h * 0.05f, 0f);
            Line(a.x, a.y, c.x, c.y, Mathf.Max(1f, h * 0.016f), tone);
            float mx = (a.x + c.x) * 0.5f, my = (a.y + c.y) * 0.5f;
            float len = Mathf.Abs(c.x - a.x) * 0.42f;
            Ellipse(mx, my + h * 0.045f, len, h * 0.05f, blanket);
            Ellipse(mx + dir * len * 0.85f, my + h * 0.06f, h * 0.04f, h * 0.035f, blanket);
            Ellipse(mx - dir * len * 0.2f, my + h * 0.075f, len * 0.5f, h * 0.02f, Blend(blanket, Paper, 0.25f), 0.6f);
        }

        private static void StretcherOnGround(float x, float y, float h, Color32 tone, Color32 blanket)
        {
            float len = h * 0.62f;
            Line(x - len, y + h * 0.05f, x + len, y + h * 0.05f, Mathf.Max(1f, h * 0.018f), tone);
            Line(x - len * 0.9f, y, x - len * 0.9f, y + h * 0.05f, Mathf.Max(1f, h * 0.02f), tone);
            Line(x + len * 0.9f, y, x + len * 0.9f, y + h * 0.05f, Mathf.Max(1f, h * 0.02f), tone);
            Lying(x, y + h * 0.07f, len * 1.5f, tone, blanket, 1);
        }

        /// <summary>Battaniye altında uyuyan biri: omuz ve kalçada kabaran yumuşak tümsek, yastık bohçasından çıkan baş.</summary>
        private static void Lying(float x, float y, float len, Color32 tone, Color32 blanket, int dir)
        {
            float d = dir;
            float hh = len * 0.13f;
            Ellipse(x + d * len * 0.47f, y + hh * 0.35f, len * 0.07f, hh * 0.35f, Blend(Paper, blanket, 0.55f));
            Ellipse(x + d * len * 0.46f, y + hh * 0.72f, len * 0.052f, len * 0.058f, tone);
            List<Vector2> pts = new List<Vector2>();
            for (int i = 0; i <= 16; i++)
            {
                float t = i / 16f;
                float bump = 0.55f + 0.45f * Mathf.Exp(-Mathf.Pow((t - 0.72f) / 0.16f, 2f)) + 0.25f * Mathf.Exp(-Mathf.Pow((t - 0.38f) / 0.14f, 2f));
                float taper = Mathf.Sin(Mathf.Clamp01(t * 1.08f) * Mathf.PI * 0.5f) * Mathf.Sin(Mathf.Clamp01((1f - t) * 3.2f) * Mathf.PI * 0.5f);
                pts.Add(new Vector2(x + d * (t - 0.5f) * len * 0.88f, y + hh * bump * (0.35f + 0.65f * taper) * 1.25f));
            }
            pts.Add(new Vector2(x + d * len * 0.4f, y));
            pts.Add(new Vector2(x - d * len * 0.44f, y));
            Poly(pts, blanket);
            // Işık tarafında battaniyenin üst kıvrımı ve bir iki kıvrım çizgisi.
            for (int i = 1; i < pts.Count - 3; i++)
                Line(pts[i - 1].x, pts[i - 1].y - 0.6f, pts[i].x, pts[i].y - 0.6f, Mathf.Max(0.8f, hh * 0.08f), Blend(blanket, Paper, 0.35f), 0.55f);
            Line(x - d * len * 0.05f, y + hh * 0.2f, x + d * len * 0.18f, y + hh * 0.75f, Mathf.Max(0.7f, hh * 0.05f), Blend(blanket, Soot, 0.35f), 0.5f);
            Ellipse(x, y, len * 0.46f, hh * 0.12f, Ink, 0.18f);
        }

        /// <summary>
        /// Oturan figür, battaniyeye sarılı: omuzlardan yere inen çan biçimi, battaniyenin
        /// altından kalkık dizler, başörtülü ya da kasketli baş. İsteğe bağlı olarak dizlerin
        /// üstünde açık bir defter ve onu tutan el.
        /// </summary>
        private static void Seated(float x, float y, float h, Color32 tone, bool scarf, int dir, Color32 blanket, bool book = false)
        {
            float d = dir;
            List<Vector2> bell = new List<Vector2>();
            for (int i = 0; i <= 14; i++)
            {
                float a = Mathf.PI * i / 14f;
                float rx = h * (0.17f + 0.02f * Mathf.Sin(a * 3f));
                float px = x - Mathf.Cos(a) * rx;
                float py = y + Mathf.Sin(a) * h * 0.58f;
                bell.Add(new Vector2(px + d * h * 0.03f * Mathf.Sin(a), py));
            }
            bell.Add(new Vector2(x + h * 0.19f, y));
            bell.Insert(0, new Vector2(x - h * 0.19f, y));
            Poly(bell, blanket);
            Line(x - d * h * 0.08f, y + h * 0.48f, x - d * h * 0.12f, y + h * 0.05f, Mathf.Max(0.8f, h * 0.012f), Blend(blanket, Soot, 0.35f), 0.55f);
            Line(x + d * h * 0.02f, y + h * 0.54f, x + d * h * 0.06f, y + h * 0.2f, Mathf.Max(0.8f, h * 0.012f), Blend(blanket, Soot, 0.35f), 0.45f);
            // Işık tarafında ince bir kenar.
            Line(x + lightDir * h * 0.15f, y + h * 0.1f, x + lightDir * h * 0.1f, y + h * 0.5f, Mathf.Max(0.8f, h * 0.015f), Blend(blanket, lightTone, 0.5f), 0.45f);
            // Baş.
            float hx = x + d * h * 0.04f, hy = y + h * 0.68f;
            float hr = h * 0.068f;
            Ellipse(hx, hy, hr * 0.9f, hr, tone);
            Ellipse(hx + d * hr * 0.75f, hy - hr * 0.15f, hr * 0.26f, hr * 0.3f, tone);
            if (scarf)
            {
                Ellipse(hx - d * hr * 0.12f, hy + hr * 0.1f, hr * 1.02f, hr * 1.1f, tone);
                Poly(new[] { new Vector2(hx - d * hr * 0.95f, hy - hr * 0.2f), new Vector2(hx + d * hr * 0.3f, hy - hr * 0.95f), new Vector2(hx - d * hr * 0.2f, hy - hr * 1.9f), new Vector2(hx - d * hr * 1.3f, hy - hr * 1.5f) }, tone);
            }
            else
            {
                Ellipse(hx + d * hr * 0.12f, hy + hr * 0.62f, hr * 1.02f, hr * 0.46f, tone);
                Ellipse(hx + d * hr * 0.95f, hy + hr * 0.42f, hr * 0.55f, hr * 0.13f, tone);
            }
            if (book)
            {
                float bx = x + d * h * 0.15f, by = y + h * 0.30f;
                Poly(new[] { new Vector2(bx - h * 0.08f, by - h * 0.01f), new Vector2(bx + h * 0.08f, by + h * 0.015f), new Vector2(bx + h * 0.075f, by + h * 0.06f), new Vector2(bx - h * 0.085f, by + h * 0.035f) }, Blend(Paper, Mustard, 0.15f));
                Line(bx, by, bx - h * 0.003f, by + h * 0.05f, Mathf.Max(0.7f, h * 0.006f), Ink, 0.55f);
                for (int i = 0; i < 3; i++) Line(bx + d * h * 0.012f, by + h * (0.012f + i * 0.012f), bx + d * h * 0.06f, by + h * (0.018f + i * 0.012f), 0.6f, Ink, 0.35f);
                Ellipse(bx - d * h * 0.06f, by + h * 0.035f, h * 0.022f, h * 0.02f, tone);
            }
        }

        /// <summary>Diz çökmüş figür: bir sedyenin ya da yatan birinin başında, öne eğik; bir eli aşağıda.</summary>
        private static void Kneel(float x, float y, float h, Color32 tone, int dir, bool book = false)
        {
            float d = dir;
            List<Vector2> body = new List<Vector2>
            {
                new Vector2(x - d * h * 0.22f, y), new Vector2(x + d * h * 0.24f, y), new Vector2(x + d * h * 0.25f, y + h * 0.10f),
                new Vector2(x + d * h * 0.08f, y + h * 0.20f), new Vector2(x + d * h * 0.16f, y + h * 0.42f), new Vector2(x + d * h * 0.20f, y + h * 0.56f),
                new Vector2(x + d * h * 0.12f, y + h * 0.63f), new Vector2(x - d * h * 0.02f, y + h * 0.60f), new Vector2(x - d * h * 0.10f, y + h * 0.42f),
                new Vector2(x - d * h * 0.12f, y + h * 0.20f), new Vector2(x - d * h * 0.20f, y + h * 0.08f)
            };
            Poly(body, tone);
            Line(x + lightDir * h * 0.14f, y + h * 0.25f, x + lightDir * h * 0.1f, y + h * 0.55f, Mathf.Max(0.8f, h * 0.012f), Blend(tone, lightTone, 0.5f), 0.4f);
            float hx = x + d * h * 0.18f, hy = y + h * 0.665f, hr = h * 0.062f;
            Ellipse(hx, hy, hr * 0.9f, hr, tone);
            Ellipse(hx + d * hr * 0.75f, hy - hr * 0.2f, hr * 0.26f, hr * 0.3f, tone);
            Ellipse(hx - d * hr * 0.12f, hy + hr * 0.1f, hr * 1.02f, hr * 1.1f, tone);
            Poly(new[] { new Vector2(hx - d * hr * 0.95f, hy - hr * 0.2f), new Vector2(hx + d * hr * 0.3f, hy - hr * 0.95f), new Vector2(hx - d * hr * 0.3f, hy - hr * 1.9f), new Vector2(hx - d * hr * 1.35f, hy - hr * 1.45f) }, tone);
            Vector2 shoulder = new Vector2(x + d * h * 0.14f, y + h * 0.55f);
            Vector2 hand = new Vector2(x + d * h * 0.36f, y + h * 0.20f);
            Line(shoulder.x, shoulder.y, x + d * h * 0.28f, y + h * 0.38f, h * 0.04f, tone);
            Line(x + d * h * 0.28f, y + h * 0.38f, hand.x, hand.y, h * 0.034f, tone);
            Ellipse(hand.x, hand.y, h * 0.024f, h * 0.026f, tone);
            if (book)
                Poly(new[] { new Vector2(x + d * h * 0.05f, y + h * 0.26f), new Vector2(x + d * h * 0.22f, y + h * 0.29f), new Vector2(x + d * h * 0.21f, y + h * 0.35f), new Vector2(x + d * h * 0.04f, y + h * 0.32f) }, Blend(Paper, Lamp, 0.35f));
        }

        // ================================================================== yapılar

        /// <summary>
        /// Yanmış taş ev, hacimli: ışığa dönük ön cephe ve gölgedeki yan duvar (üçgen alın
        /// duvarıyla). Duvar tepeleri basamak basamak yıkılmış; taşlar düzensiz sıralar hâlinde,
        /// boş pencerelerin üstünde is, çatıdan kalan iki üç kömürleşmiş mertek, alın duvarında
        /// baca ve yatay kırıklarda kar. Çatı yoktur; ev içinden gökyüzü görünür.
        /// </summary>
        private static void Ruin(float x, float baseY, float w, float h, float gable, Color32 stone, Color32 dark, float snow, int s)
        {
            System.Random r = new System.Random(seed * 17 + s);
            float side = w * 0.42f;
            float ox = -lightDir * side * 0.62f, oy = side * 0.30f;
            float frontX0 = lightDir > 0 ? x + Mathf.Abs(ox) : x;
            float frontX1 = frontX0 + w - Mathf.Abs(ox);
            float sideEdge = lightDir > 0 ? frontX0 : frontX1;
            Color32 lit = Blend(stone, lightTone, 0.12f);
            Color32 shade = Blend(stone, dark, 0.45f);

            // Ön cephenin basamaklı kırık tepesi.
            List<Vector2> front = new List<Vector2> { new Vector2(frontX0, baseY) };
            List<Vector2> steps = new List<Vector2>();
            float cursor = frontX0;
            int seg = 0;
            while (cursor < frontX1 - 1f)
            {
                float segW = Mathf.Min(frontX1 - cursor, (frontX1 - frontX0) * (0.14f + 0.2f * (float)r.NextDouble()));
                bool edgeSeg = seg == 0 || cursor + segW >= frontX1 - 1f;
                float keep = edgeSeg ? 0.9f + 0.1f * (float)r.NextDouble() : 0.45f + 0.5f * (float)r.NextDouble();
                float yTop = baseY + h * keep;
                steps.Add(new Vector2(cursor, yTop));
                steps.Add(new Vector2(cursor + segW, yTop));
                cursor += segW;
                seg++;
            }
            front.AddRange(steps);
            front.Add(new Vector2(frontX1, baseY));

            // Yan duvar: alın üçgeni, yarısı yıkılmış.
            float sBase0 = sideEdge, sBase1 = sideEdge + ox;
            float sTop0 = steps[lightDir > 0 ? 0 : steps.Count - 1].y;
            float gableKeep = 0.55f + 0.4f * (float)r.NextDouble();
            List<Vector2> sidePoly = new List<Vector2>
            {
                new Vector2(sBase0, baseY), new Vector2(sBase1, baseY + oy), new Vector2(sBase1, baseY + oy + h * 0.92f),
                new Vector2(Mathf.Lerp(sBase1, sBase0, 0.35f), baseY + oy * 0.65f + h + gable * gableKeep),
                new Vector2(Mathf.Lerp(sBase1, sBase0, 0.55f), baseY + oy * 0.45f + h + gable * gableKeep * 0.7f),
                new Vector2(sBase0, sTop0)
            };
            Poly(sidePoly, shade);
            // Arka duvarın içten görünen kenarı: ön cephe yıkık yerlerde arkada bir koyu şerit.
            Poly(new[] { new Vector2(frontX0 + ox, baseY + oy), new Vector2(frontX1 + ox, baseY + oy), new Vector2(frontX1 + ox, baseY + oy + h * 0.8f), new Vector2(frontX0 + ox, baseY + oy + h * 0.85f) }, Blend(shade, Soot, 0.35f));
            Poly(front, lit);

            // Taş örgü: düzensiz boyda taşlar, sıralar kaydırılmış.
            float row = Mathf.Max(3.5f, h / 12f);
            for (float yy = baseY; yy < baseY + h; yy += row)
            {
                float xx = frontX0 + (float)r.NextDouble() * row;
                while (xx < frontX1)
                {
                    float sw = row * (1.2f + 1.4f * (float)r.NextDouble());
                    float cx = Mathf.Min(frontX1, xx + sw);
                    float topHere = TopOfSteps(steps, (xx + cx) * 0.5f);
                    if (yy + row * 0.5f < topHere)
                    {
                        float yTop = Mathf.Min(yy + row, topHere);
                        Color32 tone = Shift(lit, r.Next(-12, 9));
                        Rect(xx + 0.6f, yy + 0.6f, cx - 0.6f, yTop - 0.6f, tone);
                        Line(xx, yy, cx, yy, 0.9f, dark, 0.35f);
                    }
                    xx = cx;
                }
            }
            // Pencereler: kemerli boşluk, üstünde is; kapı.
            int floors = h > 110f ? 2 : 1;
            int wins = Mathf.Max(1, (int)((frontX1 - frontX0) / 80f));
            for (int fl = 0; fl < floors; fl++)
                for (int i = 0; i < wins; i++)
                {
                    float wx = frontX0 + (frontX1 - frontX0) * (i + 0.5f) / wins + (fl == 0 && i == 0 ? (frontX1 - frontX0) * 0.08f : 0f);
                    float wy = baseY + h * (fl == 0 ? 0.30f : 0.64f);
                    float ww = Mathf.Min((frontX1 - frontX0) * 0.10f, 26f), wh = h * 0.16f;
                    if (wy + wh > TopOfSteps(steps, wx) - 4f) continue;
                    Poly(new[] { new Vector2(wx - ww * 0.75f, wy + wh), new Vector2(wx + ww * 0.75f, wy + wh), new Vector2(wx + ww * 1.2f, wy + wh + h * 0.22f), new Vector2(wx - ww * 1.0f, wy + wh + h * 0.26f) }, Soot, 0.32f);
                    Rect(wx - ww * 0.5f, wy, wx + ww * 0.5f, wy + wh, Blend(Soot, Ink, 0.25f));
                    Ellipse(wx, wy + wh, ww * 0.5f, ww * 0.35f, Blend(Soot, Ink, 0.25f));
                    Line(wx - ww * 0.65f, wy - 1f, wx + ww * 0.65f, wy - 1f, Mathf.Max(1.5f, h * 0.012f), Snow, 0.8f * snow);
                }
            float doorX = frontX0 + (frontX1 - frontX0) * 0.72f;
            float dw = Mathf.Min(34f, (frontX1 - frontX0) * 0.13f);
            Rect(doorX, baseY, doorX + dw, baseY + h * 0.24f, Blend(Soot, Ink, 0.2f));
            Ellipse(doorX + dw * 0.5f, baseY + h * 0.24f, dw * 0.5f, dw * 0.3f, Blend(Soot, Ink, 0.2f));

            // Kömürleşmiş mertekler: yan duvarın tepesinden ön cepheye eğik.
            int rafters = 2 + r.Next(2);
            for (int i = 0; i < rafters; i++)
            {
                float t = 0.2f + 0.3f * i + 0.1f * (float)r.NextDouble();
                Vector2 from = new Vector2(Mathf.Lerp(sBase0, sBase1, 0.5f), baseY + oy * 0.5f + h + gable * gableKeep * (0.5f - 0.15f * i));
                Vector2 to = new Vector2(Mathf.Lerp(frontX0, frontX1, t), TopOfSteps(steps, Mathf.Lerp(frontX0, frontX1, t)) - 2f);
                to = Vector2.Lerp(from, to, 0.45f + 0.25f * (float)r.NextDouble());
                Line(from.x, from.y, to.x, to.y, Mathf.Max(1.6f, w * 0.011f), Soot);
            }
            // Baca: alın duvarının üstünde.
            float chX = Mathf.Lerp(sBase1, sBase0, 0.45f);
            float chTop = baseY + oy * 0.6f + h + gable * gableKeep + h * 0.2f;
            float chW = Mathf.Max(7f, w * 0.05f);
            Rect(chX - chW * 0.5f, baseY + h * 0.7f, chX + chW * 0.5f, chTop, Blend(shade, dark, 0.2f));
            Rect(chX - chW * 0.65f, chTop - 3f, chX + chW * 0.65f, chTop, dark);
            Line(chX - chW * 0.6f, chTop + 1f, chX + chW * 0.6f, chTop + 1f, Mathf.Max(1.5f, chW * 0.3f), Snow, 0.85f * snow);

            // Yatay kırıklarda kar.
            if (snow > 0f)
                for (int i = 0; i + 1 < steps.Count; i += 2)
                    Line(steps[i].x + 1f, steps[i].y + 1f, steps[i + 1].x - 1f, steps[i + 1].y + 1f, Mathf.Max(1.8f, h * 0.022f), Snow, 0.9f * snow);
        }

        private static float TopOfSteps(List<Vector2> steps, float x)
        {
            for (int i = 0; i + 1 < steps.Count; i += 2)
                if (x >= steps[i].x && x <= steps[i + 1].x) return steps[i].y;
            return steps.Count > 0 ? steps[steps.Count - 1].y : 0f;
        }

        private static void Smoke(float x, float y, float strength)
        {
            for (int i = 0; i < 28; i++)
            {
                float t = i / 28f;
                float px = x + t * 120f + Mathf.Sin(t * 6f) * 16f;
                float py = y + t * 260f;
                float r = 8f + t * 40f;
                Ellipse(px, py, r, r * 0.8f, Blend(Paper, Ink, 0.35f), 0.10f * strength * (1f - t));
            }
        }

        /// <summary>
        /// Ahır, hacimli: alnı izleyiciye dönük; taş temel, dikey tahtalı gövde, dik çatı.
        /// Işıktan uzak yanda yan duvar ve karla örtülü çatı yüzü görünür; bölgenin dik
        /// çatısı en tanınır öğedir. İsteğe bağlı ışıklı kapı ve kara düşen ışık.
        /// </summary>
        private static void Barn(float x0, float x1, float baseY, float eaves, float peak, bool lit, Color32 tone, int s)
        {
            float w = x1 - x0;
            float side = w * 0.55f;
            float ox = -lightDir * side * 0.75f, oy = side * 0.26f;
            float mid = (x0 + x1) * 0.5f;
            float over = w * 0.07f;
            Color32 wall = tone;
            Color32 sideTone = Blend(tone, Night, 0.35f);
            Color32 stone = Blend(Blend(Paper, Ink, 0.55f), tone, 0.25f);
            float plinth = baseY + (eaves - baseY) * 0.26f;
            float sideX = lightDir > 0 ? x0 : x1;

            // Yan duvar ve temel.
            Poly(new[] { new Vector2(sideX, baseY), new Vector2(sideX + ox, baseY + oy), new Vector2(sideX + ox, eaves + oy), new Vector2(sideX, eaves) }, sideTone);
            Poly(new[] { new Vector2(sideX, baseY), new Vector2(sideX + ox, baseY + oy), new Vector2(sideX + ox, plinth + oy), new Vector2(sideX, plinth) }, Blend(stone, Night, 0.4f));
            for (float t = 0.1f; t < 1f; t += 0.12f)
                Line(sideX + ox * t, baseY + oy * t, sideX + ox * t, eaves + oy * t, 1.4f, Blend(sideTone, Soot, 0.4f), 0.5f);
            // Çatının yan yüzü: karla örtülü büyük düzlem.
            Vector2 eaveNear = new Vector2(sideX + (lightDir > 0 ? -over : over), eaves - over * 0.4f);
            Vector2 top = new Vector2(mid, peak);
            Poly(new[] { top, eaveNear, eaveNear + new Vector2(ox * 1.08f, oy * 1.08f), top + new Vector2(ox * 1.08f, oy * 1.08f) }, Blend(SnowShade, Night, lit ? 0.15f : 0.3f));
            for (int i = 1; i < 6; i++)
            {
                float t = i / 6f;
                Vector2 a = Vector2.Lerp(top, eaveNear, t);
                Line(a.x, a.y, a.x + ox * 1.08f, a.y + oy * 1.08f, 1.2f, Blend(SnowShade, Night, 0.4f), 0.25f);
            }
            // Ön cephe: taş temel, tahta gövde, alın üçgeni.
            Rect(x0, baseY, x1, eaves, wall);
            Poly(new[] { new Vector2(x0, eaves - 1f), new Vector2(x1, eaves - 1f), new Vector2(mid, peak - 8f) }, wall);
            for (float x = x0 + 12f; x < x1; x += 22f)
            {
                float topY = eaves + (peak - 8f - eaves) * (1f - Mathf.Abs(x - mid) / (w * 0.5f));
                Line(x, plinth, x, topY, 1.4f, Blend(wall, Soot, 0.45f), 0.55f);
            }
            Rect(x0, baseY, x1, plinth, stone);
            for (float yy = baseY + 8f; yy < plinth; yy += 9f) Line(x0, yy, x1, yy, 1f, Blend(stone, Soot, 0.4f), 0.4f);
            Rect(mid - w * 0.04f, (eaves + peak) * 0.5f - 10f, mid + w * 0.04f, (eaves + peak) * 0.5f + 12f, Blend(Soot, Night, 0.4f));
            // Ön saçak: alın kenarı boyunca kalın kar.
            Line(x0 - over, eaves - over * 0.4f, mid, peak, over * 0.45f, Blend(tone, Soot, 0.5f));
            Line(x1 + over, eaves - over * 0.4f, mid, peak, over * 0.45f, Blend(tone, Soot, 0.5f));
            Line(x0 - over, eaves - over * 0.4f + over * 0.28f, mid, peak + over * 0.28f, over * 0.3f, Snow);
            Line(x1 + over, eaves - over * 0.4f + over * 0.28f, mid, peak + over * 0.28f, over * 0.3f, Snow);
            if (lit)
            {
                float dw = w * 0.2f;
                float dx = mid - dw * 0.5f;
                float dh = (eaves - baseY) * 0.78f;
                Rect(dx, baseY, dx + dw, baseY + dh, Lamp);
                Rect(dx, baseY + dh * 0.72f, dx + dw, baseY + dh, Blend(Lamp, Rust, 0.25f));
                Glow(mid, baseY + dh * 0.4f, w * 0.8f, Lamp, 0.3f);
                Poly(new[] { new Vector2(dx, baseY), new Vector2(dx + dw, baseY), new Vector2(dx + dw * 2.2f, baseY - 230f), new Vector2(dx - dw * 1.2f, baseY - 230f) }, Lamp, 0.2f);
                Person(mid - dw * 0.1f, baseY + 2f, dh * 0.62f, Blend(Soot, Rust, 0.2f), Kind.Woman, Carry.None, 1);
            }
            else
                Rect(mid - w * 0.1f, baseY, mid + w * 0.1f, baseY + (eaves - baseY) * 0.78f, Blend(Soot, Night, 0.3f));
        }

        /// <summary>Sundurma: dört direk ve tek yöne eğik çatı; altı açık.</summary>
        private static void LeanTo(float x0, float x1, float baseY, float roofY, Color32 tone, int s, bool snowOnGround)
        {
            float lowY = roofY - 70f;
            for (int i = 0; i < 4; i++)
            {
                float x = Mathf.Lerp(x0 + 10f, x1 - 10f, i / 3f);
                float top = Mathf.Lerp(roofY, lowY, i / 3f);
                Line(x, baseY - 20f, x, top, 9f, tone);
            }
            Poly(new[] { new Vector2(x0 - 30f, roofY + 6f), new Vector2(x1 + 30f, lowY - 4f), new Vector2(x1 + 30f, lowY - 26f), new Vector2(x0 - 30f, roofY - 16f) }, Blend(tone, Soot, 0.3f));
            Poly(new[] { new Vector2(x0 - 32f, roofY + 20f), new Vector2(x1 + 32f, lowY + 8f), new Vector2(x1 + 30f, lowY - 4f), new Vector2(x0 - 30f, roofY + 6f) }, Blend(Snow, SnowShade, 0.3f));
        }

        /// <summary>Kuyu çatalı: çatallı direk, uzun kaldıraç sırığı, ip, kova ve taş bilezik.</summary>
        private static void SweepWell(float x, float baseY, float h, Color32 tone)
        {
            Line(x, baseY, x, baseY + h * 0.62f, h * 0.035f, tone);
            Line(x, baseY + h * 0.6f, x - h * 0.05f, baseY + h * 0.7f, h * 0.02f, tone);
            Line(x, baseY + h * 0.6f, x + h * 0.05f, baseY + h * 0.7f, h * 0.02f, tone);
            Vector2 pivot = new Vector2(x, baseY + h * 0.66f);
            Vector2 tip = new Vector2(x + h * 0.55f, baseY + h * 1.05f);
            Vector2 tail = new Vector2(x - h * 0.40f, baseY + h * 0.36f);
            Line(tail.x, tail.y, tip.x, tip.y, h * 0.018f, tone);
            Rect(tail.x - h * 0.03f, tail.y - h * 0.06f, tail.x + h * 0.03f, tail.y + h * 0.02f, tone);
            float wx = x + h * 0.55f;
            Line(tip.x, tip.y, wx, baseY + h * 0.26f, 1.2f, tone);
            Rect(wx - h * 0.03f, baseY + h * 0.2f, wx + h * 0.03f, baseY + h * 0.27f, tone);
            Rect(wx - h * 0.14f, baseY, wx + h * 0.14f, baseY + h * 0.12f, Blend(tone, Paper, 0.2f));
            Line(wx - h * 0.15f, baseY + h * 0.125f, wx + h * 0.15f, baseY + h * 0.125f, h * 0.02f, Snow, 0.85f);
        }

        /// <summary>Ambar: direklerin üstünde yükseltilmiş küçük tahıl odası.</summary>
        private static void Hambar(float x, float baseY, float h, Color32 tone)
        {
            float w = h * 0.9f;
            for (int i = 0; i < 4; i++) Line(x - w * 0.45f + i * w * 0.3f, baseY, x - w * 0.45f + i * w * 0.3f, baseY + h * 0.3f, h * 0.04f, Blend(tone, Soot, 0.4f));
            Rect(x - w * 0.5f, baseY + h * 0.3f, x + w * 0.5f, baseY + h * 0.8f, tone);
            for (float yy = baseY + h * 0.34f; yy < baseY + h * 0.8f; yy += h * 0.06f) Line(x - w * 0.5f, yy, x + w * 0.5f, yy, 1.2f, Blend(tone, Soot, 0.4f), 0.6f);
            Poly(new[] { new Vector2(x - w * 0.65f, baseY + h * 0.78f), new Vector2(x + w * 0.65f, baseY + h * 0.78f), new Vector2(x, baseY + h * 1.15f) }, Blend(tone, Soot, 0.35f));
            Poly(new[] { new Vector2(x - w * 0.66f, baseY + h * 0.82f), new Vector2(x, baseY + h * 1.18f), new Vector2(x + w * 0.66f, baseY + h * 0.82f), new Vector2(x, baseY + h * 1.08f) }, Snow);
        }

        private static void Fence(Vector2 a, Vector2 c, int posts, float hNear, Color32 tone)
        {
            for (int i = 0; i <= posts; i++)
            {
                Vector2 p = Vector2.Lerp(a, c, i / (float)posts);
                float h = hNear * (1f - 0.4f * i / posts);
                Line(p.x, p.y, p.x + 2f, p.y + h, 4f, tone);
                Ellipse(p.x + 2f, p.y + h + 2f, 4f, 2.5f, Snow, 0.9f);
            }
            Line(a.x, a.y + hNear * 0.7f, c.x, c.y + hNear * 0.42f, 2.5f, tone);
            Line(a.x, a.y + hNear * 0.35f, c.x, c.y + hNear * 0.21f, 2.5f, tone);
        }

        /// <summary>Kağnı: iki dolu teker, örgü kenarlı yatak, uzun oklu; isteğe bağlı öküz ve üstünde yük.</summary>
        private static void OxCart(float x, float y, float h, int dir, Color32 tone, bool ox, bool loaded)
        {
            float d = dir;
            float wr = h * 0.26f;
            float bedY = y + wr * 1.1f;
            float bedL = h * 0.9f;
            if (ox)
            {
                float ox0 = x + d * (bedL * 0.5f + h * 0.55f);
                Ellipse(ox0, y + h * 0.42f, h * 0.36f, h * 0.18f, tone);
                Ellipse(ox0 - d * h * 0.18f, y + h * 0.50f, h * 0.16f, h * 0.14f, tone);
                Ellipse(ox0 + d * h * 0.38f, y + h * 0.40f, h * 0.10f, h * 0.08f, tone);
                Line(ox0 + d * h * 0.36f, y + h * 0.46f, ox0 + d * h * 0.46f, y + h * 0.56f, h * 0.02f, tone);
                Line(ox0 + d * h * 0.36f, y + h * 0.46f, ox0 + d * h * 0.28f, y + h * 0.58f, h * 0.02f, tone);
                for (int i = 0; i < 4; i++)
                {
                    float lx = ox0 + d * h * (-0.26f + i * 0.17f);
                    Line(lx, y + h * 0.36f, lx + d * h * 0.01f * (i % 2 == 0 ? 1 : -1), y, h * 0.035f, tone);
                }
                Line(ox0 - d * h * 0.34f, y + h * 0.46f, ox0 - d * h * 0.40f, y + h * 0.25f, h * 0.015f, tone);
            }
            Line(x + d * bedL * 0.4f, bedY, x + d * (bedL * 0.5f + h * 0.62f), y + h * 0.5f, h * 0.025f, tone);
            Poly(new[] { new Vector2(x - bedL * 0.5f, bedY), new Vector2(x + bedL * 0.5f, bedY), new Vector2(x + bedL * 0.55f, bedY + h * 0.2f), new Vector2(x - bedL * 0.55f, bedY + h * 0.2f) }, tone);
            for (float xx = x - bedL * 0.5f; xx < x + bedL * 0.5f; xx += h * 0.06f)
                Line(xx, bedY + h * 0.02f, xx + h * 0.02f, bedY + h * 0.19f, 1.2f, Blend(tone, Paper, 0.25f), 0.5f);
            if (loaded)
            {
                Ellipse(x, bedY + h * 0.28f, bedL * 0.42f, h * 0.12f, Blend(tone, Rust, 0.3f));
                Ellipse(x - bedL * 0.15f, bedY + h * 0.34f, bedL * 0.18f, h * 0.08f, Blend(tone, Paper, 0.2f));
                Ellipse(x, bedY + h * 0.36f, bedL * 0.4f, h * 0.03f, Snow, 0.7f);
            }
            Ellipse(x - d * bedL * 0.08f, y + wr, wr, wr, Blend(tone, Soot, 0.2f));
            Ellipse(x - d * bedL * 0.08f, y + wr, wr * 0.82f, wr * 0.82f, Blend(tone, Paper, 0.12f));
            Ellipse(x - d * bedL * 0.08f, y + wr, wr * 0.18f, wr * 0.18f, Blend(tone, Soot, 0.4f));
        }

        /// <summary>
        /// Demiryolu köprüsü: taş ayaklar, kafes kiriş; orta açıklık iki parça hâlinde suya
        /// sarkar. Kopan uçların arasına bir kiriş ve tahta yürüme yolu atılmıştır; üstünde
        /// tek sıra yürüyenler ve bir sedye.
        /// </summary>
        private static void Bridge(float x0, float x1, float deckY, float scale, Color32 tone, bool crossing)
        {
            float span = x1 - x0;
            float depth = 70f * scale;
            float pierW = 46f * scale;
            float[] piers = { x0 + span * 0.18f, x0 + span * 0.42f, x0 + span * 0.64f, x0 + span * 0.88f };
            Color32 pierTone = Blend(tone, Paper, 0.18f);
            for (int i = 0; i < piers.Length; i++)
            {
                Poly(new[] { new Vector2(piers[i] - pierW * 0.6f, 120f), new Vector2(piers[i] + pierW * 0.6f, 120f), new Vector2(piers[i] + pierW * 0.45f, deckY - depth), new Vector2(piers[i] - pierW * 0.45f, deckY - depth) }, pierTone);
                for (float yy = 140f; yy < deckY - depth; yy += 16f * scale) Line(piers[i] - pierW * 0.5f, yy, piers[i] + pierW * 0.5f, yy, 1f, tone, 0.35f);
            }
            TrussSeg(new Vector2(x0 - 20f, deckY), new Vector2(piers[1], deckY), depth, (int)(10 * scale) + 4, tone);
            TrussSeg(new Vector2(piers[2], deckY), new Vector2(x1 + 20f, deckY), depth, (int)(10 * scale) + 4, tone);
            // Kopan orta açıklık: iki parça suya doğru eğik.
            float gap = piers[2] - piers[1];
            Vector2 mid = new Vector2(piers[1] + gap * 0.5f, deckY - depth * 2.8f);
            TrussSeg(new Vector2(piers[1], deckY), new Vector2(mid.x - 8f * scale, mid.y), depth, 6, Blend(tone, Night, 0.1f));
            TrussSeg(new Vector2(mid.x + 8f * scale, mid.y), new Vector2(piers[2], deckY), depth, 6, Blend(tone, Night, 0.1f));
            if (!crossing) return;
            // Kirişler ve tahta yol.
            Line(piers[1], deckY + 3f * scale, piers[2], deckY + 3f * scale, 5f * scale, Blend(Rust, Ink, 0.5f));
            for (float xx = piers[1]; xx < piers[2]; xx += 9f * scale) Line(xx, deckY + 1f * scale, xx, deckY + 7f * scale, 2f * scale, Blend(Mustard, Ink, 0.45f));
            Line(piers[1], deckY + 30f * scale, piers[2], deckY + 26f * scale, 1f, tone, 0.8f);
            // Tek sıra yürüyenler: solda düzensiz bir sıra, tahtanın üstünde bir sedye, sağda önden gidenler.
            float hMan = 48f * scale;
            Color32 ft = Blend(tone, Soot, 0.3f);
            float py = deckY + 6f * scale;
            float px = x0 + span * 0.03f;
            int k = 0;
            while (px < piers[1] - hMan * 0.5f)
            {
                if (hMan < 26f) Tiny(px, py, hMan, ft);
                else Person(px, py, hMan * (0.94f + 0.08f * (float)rng.NextDouble()), ft, k % 3 == 0 ? Kind.Woman : Kind.Man, k % 4 == 0 ? Carry.Bag : Carry.None, 1 + (k % 2) * 3, 0.4f);
                px += hMan * (0.42f + (float)rng.NextDouble() * 0.75f);
                k++;
            }
            float g0 = piers[1], g1 = piers[2];
            if (hMan >= 26f)
            {
                Person(Mathf.Lerp(g0, g1, 0.16f), py, hMan, ft, Kind.Man, Carry.Walk, 1, 0.7f);
                Stretcher(Mathf.Lerp(g0, g1, 0.52f), py, hMan * 1.05f, ft, 1, Blend(Rust, Ink, 0.4f));
                Person(Mathf.Lerp(g0, g1, 0.88f), py, hMan, ft, Kind.Woman, Carry.Walk, 4, 0.7f);
                for (int i = 0; i < 3; i++) Person(g1 + span * 0.06f + i * hMan * (0.8f + 0.3f * i), py, hMan, ft, i == 1 ? Kind.Woman : Kind.Man, Carry.Walk, 1 + i % 2 * 3, 0.7f);
            }
            else
                for (int i = 0; i < 6; i++) Tiny(Mathf.Lerp(g0, g1 + span * 0.1f, i / 5f), py, hMan, ft);
        }

        private static void TrussSeg(Vector2 a, Vector2 c, float depth, int panels, Color32 tone)
        {
            Vector2 along = c - a;
            Vector2 down = new Vector2(along.y, -along.x).normalized * depth;
            if (down.y > 0f) down = -down;
            float w = Mathf.Max(2f, depth * 0.07f);
            Line(a.x, a.y, c.x, c.y, w * 1.4f, tone);
            Line(a.x + down.x, a.y + down.y, c.x + down.x, c.y + down.y, w * 1.4f, tone);
            for (int i = 0; i <= panels; i++)
            {
                Vector2 p = a + along * (i / (float)panels);
                Line(p.x, p.y, p.x + down.x, p.y + down.y, w * 0.8f, tone);
                if (i < panels)
                {
                    Vector2 q = a + along * ((i + 1) / (float)panels);
                    if (i % 2 == 0) Line(p.x, p.y, q.x + down.x, q.y + down.y, w * 0.7f, tone);
                    else Line(p.x + down.x, p.y + down.y, q.x, q.y, w * 0.7f, tone);
                }
            }
        }

        private static void Lantern2(float x, float y, float h)
        {
            Glow(x, y, h * 5f, Lamp, 0.35f);
            Rect(x - h * 0.3f, y - h * 0.5f, x + h * 0.3f, y + h * 0.4f, Blend(Lamp, Paper, 0.35f));
            Rect(x - h * 0.38f, y + h * 0.4f, x + h * 0.38f, y + h * 0.52f, Soot);
            Rect(x - h * 0.34f, y - h * 0.6f, x + h * 0.34f, y - h * 0.5f, Soot);
            Line(x, y + h * 0.52f, x, y + h * 0.75f, 1.5f, Soot);
        }

        private static void Motes(float x0, float y0, float x1, float y1, int count, Color32 tone)
        {
            for (int i = 0; i < count; i++)
            {
                float x = x0 + (float)rng.NextDouble() * (x1 - x0);
                float y = y0 + (float)rng.NextDouble() * (y1 - y0);
                Ellipse(x, y, 1.2f, 1.2f, tone, 0.15f + (float)rng.NextDouble() * 0.3f);
            }
        }

        // ================================================================== ilkel çizim

        private static int P(float v) { return Mathf.RoundToInt(v * S); }

        private static void Put(int x, int y, Color32 c, float a)
        {
            if (a <= 0f) return;
            if (a >= 1f) b.Set(x, y, c);
            else b.BlendPx(x, y, c, a);
        }

        private static void Poly(IList<Vector2> pts, Color32 c, float a = 1f)
        {
            int n = pts.Count;
            if (n < 3) return;
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < n; i++) { minY = Mathf.Min(minY, pts[i].y); maxY = Mathf.Max(maxY, pts[i].y); }
            int y0 = Mathf.Max(0, Mathf.FloorToInt(minY * S));
            int y1 = Mathf.Min(b.H - 1, Mathf.CeilToInt(maxY * S));
            List<float> xs = new List<float>(8);
            for (int Y = y0; Y <= y1; Y++)
            {
                float yc = (Y + 0.5f) / S;
                xs.Clear();
                for (int i = 0; i < n; i++)
                {
                    Vector2 p = pts[i], q = pts[(i + 1) % n];
                    if ((p.y <= yc && q.y > yc) || (q.y <= yc && p.y > yc))
                        xs.Add(p.x + (yc - p.y) / (q.y - p.y) * (q.x - p.x));
                }
                xs.Sort();
                for (int k = 0; k + 1 < xs.Count; k += 2)
                {
                    int X0 = Mathf.Max(0, Mathf.CeilToInt(xs[k] * S - 0.5f));
                    int X1 = Mathf.Min(b.W - 1, Mathf.FloorToInt(xs[k + 1] * S - 0.5f));
                    for (int X = X0; X <= X1; X++) Put(X, Y, c, a);
                }
            }
        }

        private static void Ellipse(float cx, float cy, float rx, float ry, Color32 c, float a = 1f)
        {
            if (rx <= 0f || ry <= 0f) return;
            int x0 = Mathf.Max(0, Mathf.FloorToInt((cx - rx) * S)), x1 = Mathf.Min(b.W - 1, Mathf.CeilToInt((cx + rx) * S));
            int y0 = Mathf.Max(0, Mathf.FloorToInt((cy - ry) * S)), y1 = Mathf.Min(b.H - 1, Mathf.CeilToInt((cy + ry) * S));
            for (int Y = y0; Y <= y1; Y++)
            {
                float dy = ((Y + 0.5f) / S - cy) / ry;
                for (int X = x0; X <= x1; X++)
                {
                    float dx = ((X + 0.5f) / S - cx) / rx;
                    if (dx * dx + dy * dy <= 1f) Put(X, Y, c, a);
                }
            }
        }

        private static void Rect(float x0, float y0, float x1, float y1, Color32 c, float a = 1f)
        {
            int X0 = Mathf.Max(0, P(Mathf.Min(x0, x1))), X1 = Mathf.Min(b.W - 1, P(Mathf.Max(x0, x1)) - 1);
            int Y0 = Mathf.Max(0, P(Mathf.Min(y0, y1))), Y1 = Mathf.Min(b.H - 1, P(Mathf.Max(y0, y1)) - 1);
            for (int Y = Y0; Y <= Y1; Y++)
                for (int X = X0; X <= X1; X++) Put(X, Y, c, a);
        }

        /// <summary>Kalınlığı olan, uçları yuvarlak çizgi.</summary>
        private static void Line(float x0, float y0, float x1, float y1, float w, Color32 c, float a = 1f)
        {
            float r = Mathf.Max(0.5f, w * 0.5f);
            int X0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(x0, x1) - r) * S)), X1 = Mathf.Min(b.W - 1, Mathf.CeilToInt((Mathf.Max(x0, x1) + r) * S));
            int Y0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(y0, y1) - r) * S)), Y1 = Mathf.Min(b.H - 1, Mathf.CeilToInt((Mathf.Max(y0, y1) + r) * S));
            float dx = x1 - x0, dy = y1 - y0;
            float len2 = dx * dx + dy * dy;
            for (int Y = Y0; Y <= Y1; Y++)
            {
                float py = (Y + 0.5f) / S;
                for (int X = X0; X <= X1; X++)
                {
                    float px = (X + 0.5f) / S;
                    float t = len2 <= 0f ? 0f : Mathf.Clamp01(((px - x0) * dx + (py - y0) * dy) / len2);
                    float ex = px - (x0 + t * dx), ey = py - (y0 + t * dy);
                    if (ex * ex + ey * ey <= r * r) Put(X, Y, c, a);
                }
            }
        }

        private static void Glow(float cx, float cy, float r, Color32 c, float amount)
        {
            int x0 = Mathf.Max(0, P(cx - r)), x1 = Mathf.Min(b.W - 1, P(cx + r));
            int y0 = Mathf.Max(0, P(cy - r)), y1 = Mathf.Min(b.H - 1, P(cy + r));
            for (int Y = y0; Y <= y1; Y++)
                for (int X = x0; X <= x1; X++)
                {
                    float dx = (X / (float)S - cx) / r, dy = (Y / (float)S - cy) / r;
                    float d2 = dx * dx + dy * dy;
                    if (d2 < 1f) Put(X, Y, c, amount * Mathf.Exp(-d2 * 3.2f) * (1f - d2));
                }
        }

        /// <summary>Yumuşak kenarlı basamaklama: 0..1 değeri n düz tona ayırır.</summary>
        private static float Quant(float v, int n)
        {
            float t = Mathf.Clamp01(v) * n;
            float f = Mathf.Floor(t);
            float step = f + Smooth(0.42f, 0.58f, t - f);
            return Mathf.Clamp01(step / n * n / (n - 0.001f) - 0.5f / n);
        }

        private static float Smooth(float e0, float e1, float v)
        {
            float t = Mathf.Clamp01((v - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }

        // ================================================================== gürültü

        private static float Hash(int x, int y, int s)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + s * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }

        private static float Noise(float x, float y, int s)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            float u = fx * fx * (3f - 2f * fx), v = fy * fy * (3f - 2f * fy);
            float a = Hash(xi, yi, s), c = Hash(xi + 1, yi, s), d = Hash(xi, yi + 1, s), e = Hash(xi + 1, yi + 1, s);
            return Mathf.Lerp(Mathf.Lerp(a, c, u), Mathf.Lerp(d, e, u), v);
        }

        private static float Fbm(float x, float y, int s, int octaves)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum += amp * Noise(x, y, s + o * 31);
                norm += amp;
                amp *= 0.5f;
                x *= 2.03f;
                y *= 2.03f;
            }
            return sum / norm;
        }

        // ================================================================== bitirme

        private static Board Downsample(Board big)
        {
            Board small = new Board(Width, Height);
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    int r = 0, g = 0, bl = 0;
                    for (int j = 0; j < S; j++)
                        for (int i = 0; i < S; i++)
                        {
                            Color32 c = big.Pixels[(y * S + j) * big.W + x * S + i];
                            r += c.r; g += c.g; bl += c.b;
                        }
                    int n = S * S;
                    small.Pixels[y * Width + x] = new Color32((byte)(r / n), (byte)(g / n), (byte)(bl / n), 255);
                }
            return small;
        }

        /// <summary>Kâğıt dokusu, ince film greni ve yumuşak köşe kararması. Yırtık çerçeve yok.</summary>
        private static void Finish(Board f)
        {
            for (int y = 0; y < f.H; y++)
                for (int x = 0; x < f.W; x++)
                {
                    int i = y * f.W + x;
                    Color32 c = f.Pixels[i];
                    float paper = (Fbm(x * 0.01f, y * 0.01f, seed + 5000, 4) - 0.5f) * 14f;
                    float grain = (float)(rng.NextDouble() - 0.5) * 9f;
                    float dx = (x - f.W * 0.5f) / (f.W * 0.5f), dy = (y - f.H * 0.5f) / (f.H * 0.5f);
                    float vig = 1f - 0.22f * Mathf.Pow(Mathf.Clamp01((dx * dx * 0.8f + dy * dy) - 0.25f), 1.3f);
                    float add = paper + grain;
                    f.Pixels[i] = new Color32(ClampByte((int)(c.r * vig + add)), ClampByte((int)(c.g * vig + add)), ClampByte((int)(c.b * vig + add * 0.9f)), 255);
                }
        }

        // ================================================================== yardımcı uzantı

        /// <summary>Tek yönlü çizilmiş bir biçimi figürün baktığı yöne çevirir.</summary>
        private static Vector2[] Flip(this Vector2[] pts, float cx, int dir)
        {
            if (dir >= 0) return pts;
            Vector2[] result = new Vector2[pts.Length];
            for (int i = 0; i < pts.Length; i++) result[i] = new Vector2(cx - (pts[i].x - cx), pts[i].y);
            return result;
        }
    }
}
