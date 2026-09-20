using System;
using UnityEngine;

namespace OrdinaryFronts.Editor
{
    /// <summary>
    /// Yugoslavya bölümünün sahne arka planlarını üretir.
    /// <para>
    /// İlk denemeler katmanlı ufuk bantlarından ibaretti ve mevcut illüstrasyonların yanında
    /// düz durdu. Farkı doku değil, dört yapısal karar yaratıyor; bu dosya o dördünü uygular:
    /// </para>
    /// <list type="number">
    /// <item><b>Işık yönü.</b> Her sahnenin tek bir baskın kaynağı vardır. Sırtların ışığa
    /// bakan yüzü <see cref="RimLight"/> ile kenarlanır, yapıların aydınlık ve gölge yüzleri
    /// ayrı tonda çizilir, figürler zemine gölge düşürür. Düz dolgu bir çıkartma gibi
    /// duruyordu; ton ayrımı onu hacme çeviriyor.</item>
    /// <item><b>Kadraja giren ön plan.</b> Her sahnede kenardan giren büyük ve koyu bir kütle
    /// bulunur. Derinlik zinciri (ön - orta - arka) ancak böyle kuruluyor.</item>
    /// <item><b>Okunabilir figür.</b> Kalabalık uzakta küçük kalır, fakat her sahnede
    /// 240-340 piksel boyunda, paltosu, başlığı ve yükü çizilmiş birkaç figür vardır.
    /// Ölçeği ve konuyu veren şey onlardır.</item>
    /// <item><b>Orta plan yoğunluğu.</b> Çit kazığı, kaya, çalı gibi küçük öğeler sahneyi
    /// "boş arazi" olmaktan çıkarır.</item>
    /// </list>
    /// <para>
    /// Bunların üstüne baskı dokusu gelir: kütle içi ton kırılması, tırtıklı silüet kenarı,
    /// forma paralel oyma izleri, mürekkep yoğunluğu dalgalanması ve kâğıt lifi. Palet
    /// <see cref="ThemeConfig"/> ile birebir aynıdır ve sahne tohumları sabittir.
    /// </para>
    /// </summary>
    internal static class LinocutSceneFactory
    {
        internal const int Width = 1672;
        internal const int Height = 941;

        internal static readonly Color32 Soot = new Color32(0x17, 0x1B, 0x21, 0xFF);
        internal static readonly Color32 Paper = new Color32(0xD8, 0xCF, 0xB6, 0xFF);
        internal static readonly Color32 Rust = new Color32(0x9E, 0x44, 0x34, 0xFF);
        internal static readonly Color32 Petrol = new Color32(0x3F, 0x64, 0x68, 0xFF);
        internal static readonly Color32 Mustard = new Color32(0xA8, 0x8B, 0x4A, 0xFF);
        internal static readonly Color32 Ink = new Color32(0x26, 0x25, 0x22, 0xFF);

        internal static readonly string[] FileNames =
        {
            "bg_mountain_column.png",
            "bg_burned_village.png",
            "bg_typhus_barn.png",
            "bg_river_gorge.png",
            "bg_broken_bridge.png"
        };

        /// <summary>Sahnenin gök rengi; pus hesabı buna göre yapılır.</summary>
        internal static Color32 skyTone = Paper;

        /// <summary>Baskın ışık yönü: -1 soldan, +1 sağdan.</summary>
        internal static float lightDir = 1f;

        /// <summary>Işığın rengi; kenar ışığı ve aydınlık yüzler bundan türer.</summary>
        internal static Color32 lightTone = Mustard;

        internal static Texture2D Render(int scene)
        {
            Board board = new Board(Width, Height);
            System.Random random = new System.Random(19430300 + scene * 617);

            switch (scene)
            {
                case 0: DrawMountainColumn(board, random); break;
                case 1: DrawBurnedVillage(board, random); break;
                case 2: DrawTyphusBarn(board, random); break;
                case 3: DrawRiverGorge(board, random); break;
                default: DrawBrokenBridge(board, random); break;
            }

            InkDensity(board, random);
            PaperTooth(board, random);
            Halftone(board, random);
            Grain(board, random, 5);
            Vignette(board, random);
            EdgeSpeckle(board, random);

            Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
            texture.SetPixels32(board.Pixels);
            texture.Apply(false, false);
            return texture;
        }

        // ------------------------------------------------------------------ sahneler

        /// <summary>
        /// Kar sırtlarını aşan yaralı kolonu. Alçak güneş sağda; sırtların sağ yüzleri kenar
        /// ışığı alır, figür gölgeleri sola uzanır. Kadraja soldan giren kayalık yamaç
        /// derinliği kurar ve öndeki üç figür ölçeği verir.
        /// </summary>
        internal static void DrawMountainColumn(Board b, System.Random rng)
        {
            skyTone = Blend(Paper, Mustard, 0.18f);
            lightDir = 1f;
            lightTone = Blend(Mustard, Paper, 0.45f);

            SkyWash(b, rng, Petrol, 0.16f, Mustard, 0.34f, 0.44f);
            HorizonGlow(b, (int)(b.W * 0.80f), (int)(b.H * 0.46f), (int)(b.H * 0.52f), Mustard, 0.34f);
            CloudMass(b, rng, 0.56f, 0.86f, 6);
            SkyStreaks(b, rng, 0.46f, 0.96f, Petrol, 16);

            float[] far = Ridge(b.W, rng, b.H * 0.640f, b.H * 0.105f, 5);
            Layer(b, rng, far, Blend(Petrol, Soot, 0.18f), 0.64f, 2, 0.05f, 1);
            RimLight(b, far, Blend(lightTone, skyTone, 0.35f), 4, 0.55f);

            float[] mid = Ridge(b.W, rng, b.H * 0.505f, b.H * 0.088f, 5);
            Layer(b, rng, mid, Blend(Petrol, Soot, 0.44f), 0.34f, 16, 0.16f, 2);
            RimLight(b, mid, Blend(lightTone, Petrol, 0.45f), 5, 0.75f);
            PineRidge(b, rng, mid, 26, 0.034f, 0.30f);

            float[] saddle = Ridge(b.W, rng, b.H * 0.395f, b.H * 0.036f, 4);
            Layer(b, rng, saddle, Blend(Paper, Petrol, 0.26f), 0.08f, 30, 0.18f, 3);
            SnowStreaks(b, rng, saddle, 150);

            // Uzaktaki kolon yalnız küçük işaretlerdir; ölçeği öndeki figürler verir.
            // Kolon yalnız uzak işaretler olarak kalır. Figür anatomisi bu üreticinin
            // yapamadığı şeydir; büyük ölçekte çizilen figürler korkuluğa benziyordu.
            // 22 pikselin altında siluet bir insan izi olarak okunur, anatomi sorulmaz.
            // Uzak kolon: seyrek ve ince. Eşit aralıklı kalın izler kesik çizgi gibi
            // duruyordu, bu yüzden hem aralık hem yükseklik düzensizleştirildi.
            const int walkers = 22;
            for (int i = 0; i < walkers; i++)
            {
                float t = 0.34f + 0.62f * i / (walkers - 1f);
                int x = (int)(t * b.W) + rng.Next(-18, 19);
                int y = (int)saddle[Mathf.Clamp(x, 0, b.W - 1)] - 2 + rng.Next(-2, 2);
                Mark(b, rng, x, y, 9f + rng.Next(0, 5), Blend(Soot, Petrol, 0.20f), false);
            }
            Scatter(b, rng, saddle, 40, 0.010f, Blend(Soot, Petrol, 0.35f));

            float[] near = ForegroundSlope(b, rng, 0.225f, 0.135f, 0.02f);
            Layer(b, rng, near, Soot, 0f, 24, 0.24f, 3);
            RimLight(b, near, Blend(Petrol, Paper, 0.30f), 3, 0.42f);
            PineRidge(b, rng, near, 12, 0.070f, 0f);
            Scatter(b, rng, near, 26, 0.016f, Ink);

            // Sahnenin konusunu yapı taşır, figür değil. Dört ayrı denemede figür her
            // ölçekte kazık ya da korkuluk olarak okundu; buna karşılık çit, yol izi ve
            // yol taşı hem derinlik hem insan varlığı bilgisini figürsüz veriyor.
            FenceLine(b, rng, (int)(b.W * 0.020f), (int)near[Mathf.Clamp((int)(b.W * 0.020f), 0, b.W - 1)] - 4,
                (int)(b.W * 0.560f), (int)saddle[Mathf.Clamp((int)(b.W * 0.560f), 0, b.W - 1)] - 2, 17);
            Waymarker(b, rng, (int)(b.W * 0.235f), (int)near[Mathf.Clamp((int)(b.W * 0.235f), 0, b.W - 1)] - 6, 128);
            FallenTrunk(b, rng, (int)(b.W * 0.760f), (int)near[Mathf.Clamp((int)(b.W * 0.760f), 0, b.W - 1)] - 4, 230, 22);

            // Karda kağnı izleri: ön plandan eyere doğru daralarak gider, gözü kolona bağlar.
            for (int i = 0; i < 2; i++)
            {
                int x0 = (int)(b.W * (0.320f + i * 0.040f));
                int y0 = (int)near[Mathf.Clamp(x0, 0, b.W - 1)] + 2;
                int x1 = (int)(b.W * (0.540f + i * 0.010f));
                int y1 = (int)saddle[Mathf.Clamp(x1, 0, b.W - 1)] - 5;
                // Tek düz çizgi kablo gibi okunuyordu; iz üç kırıkla ilerler ve söner.
                int mx = (x0 + x1) / 2 + rng.Next(-24, 25);
                int my = (y0 + y1) / 2 + rng.Next(-8, 9);
                Stroke(b, x0, y0, mx, my, Blend(Soot, Petrol, 0.22f), 3 - i, 0.26f);
                Stroke(b, mx, my, x1, y1, Blend(Soot, Petrol, 0.22f), 2, 0.20f);
            }
        }

        /// <summary>
        /// Yakılmış köy. Alçak güneş solda; sağ kenardan kadraja giren yıkık duvar ve baca
        /// ön planı kurar, gölgeler sağa uzanır.
        /// </summary>
        internal static void DrawBurnedVillage(Board b, System.Random rng)
        {
            skyTone = Blend(Paper, Rust, 0.08f);
            lightDir = -1f;
            lightTone = Blend(Mustard, Rust, 0.30f);

            SkyWash(b, rng, Petrol, 0.22f, Rust, 0.18f, 0.42f);
            HorizonGlow(b, (int)(b.W * 0.16f), (int)(b.H * 0.44f), (int)(b.H * 0.50f), Rust, 0.26f);
            CloudMass(b, rng, 0.50f, 0.80f, 5);
            SkyStreaks(b, rng, 0.44f, 0.96f, Petrol, 14);

            float[] farHills = Ridge(b.W, rng, b.H * 0.600f, b.H * 0.070f, 5);
            Layer(b, rng, farHills, Blend(Petrol, Soot, 0.16f), 0.66f, 2, 0.05f, 1);
            RimLight(b, farHills, Blend(lightTone, skyTone, 0.40f), 3, 0.5f);

            float[] hills = Ridge(b.W, rng, b.H * 0.510f, b.H * 0.062f, 5);
            Layer(b, rng, hills, Blend(Petrol, Soot, 0.36f), 0.40f, 12, 0.12f, 2);
            RimLight(b, hills, Blend(lightTone, Petrol, 0.40f), 4, 0.6f);
            PineRidge(b, rng, hills, 26, 0.030f, 0.36f);

            float[] ground = Ridge(b.W, rng, b.H * 0.350f, b.H * 0.024f, 4);
            Layer(b, rng, ground, Blend(Ink, Paper, 0.28f), 0.14f, 24, 0.16f, 2);

            RuinedHouse(b, rng, (int)(b.W * 0.120f), (int)(b.H * 0.325f), 214, 132, 0.74f);
            RuinedHouse(b, rng, (int)(b.W * 0.330f), (int)(b.H * 0.318f), 150, 92, -1f);
            RuinedHouse(b, rng, (int)(b.W * 0.470f), (int)(b.H * 0.330f), 186, 150, 0.22f);
            BareTree(b, rng, (int)(b.W * 0.268f), (int)(b.H * 0.340f), 176);
            Scatter(b, rng, ground, 46, 0.012f, Blend(Ink, Petrol, 0.25f));

            // Sağ kenardan giren büyük yıkıntı: ön plan kütlesi ve ölçek anahtarı.
            BigRuin(b, rng, (int)(b.W * 0.735f), (int)(b.W * 1.04f), (int)(b.H * 0.135f), (int)(b.H * 0.640f));

            float[] near = ForegroundSlope(b, rng, 0.215f, 0.055f, 0.30f);
            Layer(b, rng, near, Soot, 0f, 22, 0.20f, 3);
            RimLight(b, near, Blend(Petrol, Paper, 0.26f), 3, 0.38f);

            int cartX = (int)(b.W * 0.360f);
            int cartY = (int)near[Mathf.Clamp(cartX, 0, b.W - 1)] - 8;
            Cart(b, rng, cartX, cartY, 96f);
            Mark(b, rng, cartX - 44, cartY + 4, 20f, Ink, false);
            Mark(b, rng, cartX + 132, cartY - 2, 18f, Ink, false);
            FallenTrunk(b, rng, (int)(b.W * 0.640f), (int)near[Mathf.Clamp((int)(b.W * 0.640f), 0, b.W - 1)] - 6, 210, 22);
        }

        /// <summary>
        /// Tifüs ahırı, gece. Tek kaynak kapıdan taşan ışıktır: kar üzerinde bir koni açar,
        /// sedyeleri yandan aydınlatır ve öndeki fenerli figürü tam siluete çevirir.
        /// </summary>
        internal static void DrawTyphusBarn(Board b, System.Random rng)
        {
            skyTone = Blend(Paper, Soot, 0.76f);
            lightDir = 1f;
            lightTone = Mustard;

            for (int y = 0; y < b.H; y++)
            {
                float v = y / (float)(b.H - 1);
                Color32 color = Blend(Blend(Paper, Soot, 0.76f), Soot, 0.26f * (1f - v));
                for (int x = 0; x < b.W; x++) b.Set(x, y, color);
            }
            SkyStreaks(b, rng, 0.58f, 0.96f, Blend(Petrol, Paper, 0.34f), 12);
            CloudMass(b, rng, 0.62f, 0.88f, 4);

            float[] ridge = Ridge(b.W, rng, b.H * 0.600f, b.H * 0.062f, 5);
            Layer(b, rng, ridge, Blend(Soot, Petrol, 0.24f), 0.14f, 10, 0.14f, 2);
            PineRidge(b, rng, ridge, 22, 0.032f, 0.10f);

            float[] snow = Ridge(b.W, rng, b.H * 0.420f, b.H * 0.026f, 4);
            Layer(b, rng, snow, Blend(Petrol, Paper, 0.46f), 0.06f, 26, 0.20f, 3);

            int barnL = (int)(b.W * 0.520f);
            int barnR = (int)(b.W * 1.03f);
            int barnBase = (int)(b.H * 0.330f);
            int eaves = (int)(b.H * 0.800f);
            Barn(b, rng, barnL, barnR, barnBase, eaves);

            int doorL = (int)(b.W * 0.605f);
            int doorR = (int)(b.W * 0.712f);
            int doorTop = (int)(b.H * 0.630f);
            FillTextured(b, rng, doorL, barnBase, doorR, doorTop, Blend(Mustard, Paper, 0.40f), 0.14f);
            for (int i = 0; i < 6; i++)
            {
                int y = barnBase + (int)((doorTop - barnBase) * (0.12f + i * 0.15f));
                Stroke(b, doorL, y, doorR, y, Blend(Mustard, Ink, 0.35f), 2, 0.30f);
            }
            FillTextured(b, rng, doorL - 12, barnBase, doorL, doorTop + 10, Ink, 0.2f);
            FillTextured(b, rng, doorR, barnBase, doorR + 12, doorTop + 10, Ink, 0.2f);
            FillTextured(b, rng, doorL - 12, doorTop, doorR + 12, doorTop + 12, Ink, 0.2f);

            int cx = (doorL + doorR) / 2;
            for (int y = 0; y < barnBase; y++)
            {
                float t = y / (float)barnBase;
                int half = (int)((doorR - doorL) * 0.5f + (barnBase - y) * 1.55f);
                for (int x = cx - half; x < cx + half; x++)
                {
                    float across = 1f - Mathf.Abs(x - cx) / (float)half;
                    b.BlendPx(x, y, Mustard, 0.62f * t * t * across);
                }
            }
            for (int i = 0; i < 18; i++)
            {
                int x = barnL + 30 + rng.Next(0, barnR - barnL - 60);
                int y0 = barnBase + rng.Next(0, (int)(b.H * 0.18f));
                FillTextured(b, rng, x, y0, x + 2 + rng.Next(0, 2), y0 + rng.Next(30, 160), Blend(Mustard, Soot, 0.46f), 0.3f);
            }

            for (int i = 0; i < 6; i++)
            {
                int x = (int)(b.W * (0.045f + i * 0.078f));
                StretcherOnGround(b, rng, x, (int)snow[Mathf.Clamp(x, 0, b.W - 1)] - 8, 140f, Soot, i);
            }
            for (int i = 0; i < 4; i++)
            {
                int x = (int)(b.W * (0.075f + i * 0.105f));
                StretcherOnGround(b, rng, x, (int)(b.H * 0.268f), 190f, Ink, i + 30);
            }

            float[] near = ForegroundSlope(b, rng, 0.135f, 0.030f, 0.55f);
            Layer(b, rng, near, Ink, 0f, 16, 0.09f, 3);

            Mark(b, rng, cx - 22, barnBase + 6, 42f, Ink, false);
            int lanternX = (int)(b.W * 0.310f);
            int lanternY = (int)near[Mathf.Clamp(lanternX, 0, b.W - 1)] - 10;
            // Fener ön planda tek başına durur; taşıyan figür çizilmez.
            HorizonGlow(b, lanternX, lanternY + 60, 120, Mustard, 0.30f);
            Lantern(b, lanternX, lanternY + 40);
            Mark(b, rng, lanternX - 54, lanternY + 2, 46f, Soot, false);
        }

        /// <summary>
        /// Neretva kanyonu. Işık sağ üstten; sol duvar kadraja giren ön plan kütlesidir ve
        /// hemen önündeki kaya çıkıntısında iki büyük figür aşağı bakar.
        /// </summary>
        internal static void DrawRiverGorge(Board b, System.Random rng)
        {
            skyTone = Blend(Paper, Mustard, 0.13f);
            lightDir = 1f;
            lightTone = Blend(Mustard, Paper, 0.40f);

            SkyWash(b, rng, Petrol, 0.19f, Mustard, 0.24f, 0.60f);
            HorizonGlow(b, (int)(b.W * 0.86f), (int)(b.H * 0.80f), (int)(b.H * 0.44f), Mustard, 0.28f);
            CloudMass(b, rng, 0.74f, 0.94f, 5);
            SkyStreaks(b, rng, 0.64f, 0.96f, Petrol, 14);

            float[] rim = Ridge(b.W, rng, b.H * 0.765f, b.H * 0.072f, 5);
            Layer(b, rng, rim, Blend(Petrol, Soot, 0.14f), 0.66f, 2, 0.04f, 1);
            RimLight(b, rim, Blend(lightTone, skyTone, 0.40f), 3, 0.5f);
            PineRidge(b, rng, rim, 30, 0.024f, 0.64f);

            float[] step1 = Ridge(b.W, rng, b.H * 0.620f, b.H * 0.058f, 5);
            Layer(b, rng, step1, Blend(Petrol, Soot, 0.28f), 0.48f, 10, 0.13f, 2);
            RimLight(b, step1, Blend(lightTone, Petrol, 0.40f), 4, 0.55f);

            float[] step2 = Ridge(b.W, rng, b.H * 0.480f, b.H * 0.052f, 6);
            Layer(b, rng, step2, Blend(Petrol, Soot, 0.46f), 0.28f, 18, 0.16f, 2);
            RimLight(b, step2, Blend(lightTone, Petrol, 0.30f), 4, 0.6f);

            float[] step3 = Ridge(b.W, rng, b.H * 0.335f, b.H * 0.044f, 6);
            Layer(b, rng, step3, Blend(Petrol, Soot, 0.68f), 0.12f, 22, 0.18f, 3);
            RimLight(b, step3, Blend(lightTone, Petrol, 0.22f), 3, 0.5f);

            float[] water = Ridge(b.W, rng, b.H * 0.175f, b.H * 0.018f, 5);
            Layer(b, rng, water, Blend(Petrol, Paper, 0.56f), 0f, 0, 0f, 2);
            WaterStreaks(b, rng, water, 300);

            int[] wallEdge = new int[b.H];
            float[] wobble = ValueNoise(b.H, rng, 16);
            float[] fine = ValueNoise(b.H, rng, 90);
            for (int y = 0; y < b.H; y++)
            {
                float v = y / (float)(b.H - 1);
                wallEdge[y] = (int)(b.W * (0.340f - 0.245f * v) + (wobble[y] - 0.5f) * 66f + (fine[y] - 0.5f) * 14f);
                for (int x = 0; x < wallEdge[y]; x++)
                {
                    float d = (wallEdge[y] - x) / (float)Mathf.Max(1, wallEdge[y]);
                    b.Set(x, y, Shift(Blend(Soot, Petrol, 0.12f * (1f - d)), rng.Next(-6, 7)));
                }
                // Işığa bakan kenar: duvarın sağ yüzü ince bir çizgi hâlinde aydınlanır.
                for (int k = 0; k < 3; k++)
                    b.BlendPx(wallEdge[y] - k, y, Blend(Petrol, Paper, 0.42f), 0.5f - k * 0.13f);
            }
            for (int i = 0; i < 110; i++)
            {
                int y = rng.Next(0, b.H);
                int x0 = rng.Next(0, Mathf.Max(2, wallEdge[y] - 20));
                Stroke(b, x0, y, x0 + rng.Next(50, 200), y + rng.Next(-9, 10), Blend(Soot, Petrol, 0.38f), 2 + rng.Next(0, 2), 0.55f);
            }

            int[] px = { (int)(b.W * 0.075f), (int)(b.W * 0.268f), (int)(b.W * 0.100f), (int)(b.W * 0.300f), (int)(b.W * 0.158f) };
            int[] py = { (int)(b.H * 0.860f), (int)(b.H * 0.706f), (int)(b.H * 0.552f), (int)(b.H * 0.398f), (int)(b.H * 0.248f) };
            for (int i = 0; i < px.Length - 1; i++)
            {
                Stroke(b, px[i], py[i], px[i + 1], py[i + 1], Blend(Soot, Paper, 0.28f), 10, 0.85f);
                const int steps = 5;
                for (int s = 0; s < steps; s++)
                {
                    float t = (s + 0.5f) / steps;
                    int x = (int)Mathf.Lerp(px[i], px[i + 1], t);
                    int y = (int)Mathf.Lerp(py[i], py[i + 1], t);
                    float scale = 15f + 9f * (1f - py[i] / (float)b.H);
                    Mark(b, rng, x, y, scale, Ink, i == 2 && s == 2);
                }
            }

            float[] ledge = ForegroundSlope(b, rng, 0.200f, 0.050f, 0.02f);
            Layer(b, rng, ledge, Soot, 0f, 20, 0.22f, 3);
            RimLight(b, ledge, Blend(Petrol, Paper, 0.34f), 4, 0.5f);
            Scatter(b, rng, ledge, 20, 0.018f, Ink);
            int hx = (int)(b.W * 0.150f);
            int hy = (int)ledge[Mathf.Clamp(hx, 0, b.W - 1)] - 10;
            Mark(b, rng, hx, hy, 34f, Ink, false);
            Mark(b, rng, hx + 40, hy - 6, 30f, Ink, false);
        }

        /// <summary>
        /// Yıkık köprü. Işık soldan; ön planda sol alt köşede bekleyen büyük figürler ve
        /// yerdeki sedye, arkada geçidi tek sıra kullanan kolon.
        /// </summary>
        internal static void DrawBrokenBridge(Board b, System.Random rng)
        {
            skyTone = Blend(Paper, Mustard, 0.11f);
            lightDir = -1f;
            lightTone = Blend(Mustard, Paper, 0.42f);

            SkyWash(b, rng, Petrol, 0.23f, Mustard, 0.19f, 0.54f);
            HorizonGlow(b, (int)(b.W * 0.18f), (int)(b.H * 0.60f), (int)(b.H * 0.46f), Mustard, 0.26f);
            CloudMass(b, rng, 0.66f, 0.90f, 5);
            SkyStreaks(b, rng, 0.58f, 0.96f, Petrol, 15);

            float[] farRim = Ridge(b.W, rng, b.H * 0.710f, b.H * 0.068f, 5);
            Layer(b, rng, farRim, Blend(Petrol, Soot, 0.16f), 0.64f, 2, 0.05f, 1);
            RimLight(b, farRim, Blend(lightTone, skyTone, 0.40f), 3, 0.5f);
            PineRidge(b, rng, farRim, 28, 0.026f, 0.62f);

            float[] backWall = Ridge(b.W, rng, b.H * 0.570f, b.H * 0.052f, 6);
            Layer(b, rng, backWall, Blend(Petrol, Soot, 0.42f), 0.34f, 14, 0.14f, 2);
            RimLight(b, backWall, Blend(lightTone, Petrol, 0.38f), 4, 0.55f);

            float[] shadow = Ridge(b.W, rng, b.H * 0.430f, b.H * 0.028f, 5);
            Layer(b, rng, shadow, Blend(Soot, Petrol, 0.14f), 0.06f, 20, 0.30f, 2);

            float[] water = Ridge(b.W, rng, b.H * 0.108f, b.H * 0.011f, 4);
            Layer(b, rng, water, Blend(Petrol, Paper, 0.50f), 0f, 0, 0f, 2);
            WaterStreaks(b, rng, water, 220);

            float[] leftWob = ValueNoise(b.H, rng, 14);
            float[] rightWob = ValueNoise(b.H, rng, 14);
            int bankTop = (int)(b.H * 0.60f);
            for (int y = 0; y < bankTop; y++)
            {
                float v = y / (float)bankTop;
                int leftEdge = (int)(b.W * (0.230f - 0.140f * v) + (leftWob[y] - 0.5f) * 44f);
                for (int x = 0; x < leftEdge; x++) b.Set(x, y, Shift(Soot, rng.Next(-6, 7)));
                for (int k = 0; k < 3; k++) b.BlendPx(leftEdge + k, y, Blend(Petrol, Paper, 0.34f), 0.34f - k * 0.10f);
                int rightEdge = (int)(b.W * (0.780f + 0.140f * v) - (rightWob[y] - 0.5f) * 40f);
                for (int x = rightEdge; x < b.W; x++) b.Set(x, y, Shift(Soot, rng.Next(-6, 7)));
            }
            for (int i = 0; i < 90; i++)
            {
                int y = rng.Next(0, bankTop);
                int x0 = rng.Next(0, (int)(b.W * 0.15f));
                Stroke(b, x0, y, x0 + rng.Next(45, 150), y + rng.Next(-8, 9), Blend(Soot, Petrol, 0.36f), 2, 0.5f);
                int x1 = rng.Next((int)(b.W * 0.85f), b.W);
                Stroke(b, x1, y, x1 - rng.Next(45, 150), y + rng.Next(-8, 9), Blend(Soot, Petrol, 0.28f), 2, 0.5f);
            }

            int deckY = (int)(b.H * 0.520f);
            Pier(b, rng, (int)(b.W * 0.100f), (int)(b.W * 0.146f), (int)(b.H * 0.055f), deckY);
            Pier(b, rng, (int)(b.W * 0.430f), (int)(b.W * 0.476f), (int)(b.H * 0.045f), deckY);
            Pier(b, rng, (int)(b.W * 0.852f), (int)(b.W * 0.898f), (int)(b.H * 0.055f), deckY);

            FillTextured(b, rng, 0, deckY, (int)(b.W * 0.480f), deckY + 15, Ink, 0.25f);
            Truss(b, rng, 0, (int)(b.W * 0.480f), deckY, 76);
            FillTextured(b, rng, (int)(b.W * 0.706f), deckY, b.W, deckY + 15, Ink, 0.25f);
            Truss(b, rng, (int)(b.W * 0.706f), b.W, deckY, 76);

            Stroke(b, (int)(b.W * 0.480f), deckY + 7, (int)(b.W * 0.620f), (int)(b.H * 0.225f), Ink, 17, 1f);
            Stroke(b, (int)(b.W * 0.620f), (int)(b.H * 0.225f), (int)(b.W * 0.666f), (int)(b.H * 0.160f), Ink, 11, 1f);
            for (int i = 0; i < 8; i++)
            {
                float t = i / 7f;
                int x = (int)Mathf.Lerp(b.W * 0.483f, b.W * 0.617f, t);
                int y = (int)Mathf.Lerp(deckY + 7, b.H * 0.225f, t);
                Stroke(b, x, y, x + 32, y + 44, Ink, 6, 1f);
                Stroke(b, x, y + 44, x + 32, y, Ink, 4, 1f);
            }

            int gapL = (int)(b.W * 0.460f);
            int gapR = (int)(b.W * 0.722f);
            int plankY = deckY + 19;
            FillTextured(b, rng, gapL, plankY, gapR, plankY + 7, Blend(Ink, Mustard, 0.30f), 0.3f);
            FillTextured(b, rng, gapL, plankY + 12, gapR, plankY + 17, Blend(Ink, Mustard, 0.18f), 0.3f);
            for (int x = gapL; x < gapR; x += 18)
                FillTextured(b, rng, x, plankY, x + 8, plankY + 17, Blend(Ink, Mustard, 0.38f), 0.35f);
            Stroke(b, gapL + 12, plankY, (int)(b.W * 0.590f), (int)(b.H * 0.355f), Ink, 5, 1f);
            Stroke(b, gapR - 12, plankY, (int)(b.W * 0.590f), (int)(b.H * 0.355f), Ink, 5, 1f);

            int[] offsets = { 8, 32, 56, 84, 116, 148, 180, 212, 244 };
            for (int i = 0; i < offsets.Length; i++)
                Mark(b, rng, gapL + offsets[i], plankY + 17, 26f, Soot, i == 4);
            for (int i = 0; i < 14; i++)
            {
                int x = (int)(b.W * 0.140f) + i * 20 + rng.Next(-5, 6);
                Mark(b, rng, x, deckY + 15, 22f + rng.Next(0, 5), Soot, false);
            }

            float[] near = ForegroundSlope(b, rng, 0.170f, 0.045f, 0.06f);
            Layer(b, rng, near, Ink, 0f, 18, 0.12f, 3);
            RimLight(b, near, Blend(Petrol, Paper, 0.28f), 3, 0.40f);
            Scatter(b, rng, near, 22, 0.015f, Soot);

            int wx = (int)(b.W * 0.088f);
            int wy = (int)near[Mathf.Clamp(wx, 0, b.W - 1)] - 10;
            StretcherOnGround(b, rng, wx + 70, wy, 112f, Soot, 1);
            Mark(b, rng, wx, wy + 4, 40f, Soot, false);
            Mark(b, rng, wx + 44, wy - 6, 34f, Soot, false);
        }

        // ------------------------------------------------------------- ışık ve kompozisyon

        /// <summary>
        /// Bir sırtın ışığa bakan yüzüne kenar ışığı. Eğim yönü ışık yönüyle uyuşan
        /// noktalarda çizilir; kütlenin hangi taraftan aydınlandığını okutan işaret budur.
        /// </summary>
        internal static void RimLight(Board b, float[] top, Color32 tone, int thickness, float alpha)
        {
            for (int x = 1; x < b.W - 1; x++)
            {
                float slope = top[x + 1] - top[x - 1];
                float facing = -slope * lightDir;
                if (facing <= 0f) continue;
                float strength = Mathf.Clamp01(facing / 3.5f);
                int yTop = (int)top[x];
                for (int k = 0; k < thickness; k++)
                    b.BlendPx(x, yTop - k, tone, alpha * strength * (1f - k / (float)thickness));
            }
        }

        /// <summary>Işık kaynağının yumuşak halesi.</summary>
        internal static void HorizonGlow(Board b, int cx, int cy, int radius, Color32 tone, float strength)
        {
            for (int y = Mathf.Max(0, cy - radius); y < Mathf.Min(b.H, cy + radius); y++)
            {
                for (int x = Mathf.Max(0, cx - radius); x < Mathf.Min(b.W, cx + radius); x++)
                {
                    float dx = (x - cx) / (float)radius;
                    float dy = (y - cy) / (float)radius;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d >= 1f) continue;
                    float f = (1f - d) * (1f - d);
                    b.BlendPx(x, y, tone, strength * f);
                }
            }
        }

        /// <summary>
        /// Kadraja bir kenardan giren ön plan yamacı. Sabit yükseklikli bir bant yerine tek
        /// yöne yükselen bir eğri kullanılır; derinlik zincirini kuran şey budur.
        /// </summary>
        internal static float[] ForegroundSlope(Board b, System.Random rng, float highSide, float lowSide, float peakAt)
        {
            float[] noise = ValueNoise(b.W, rng, 9);
            float[] fine = ValueNoise(b.W, rng, 44);
            float[] result = new float[b.W];
            for (int x = 0; x < b.W; x++)
            {
                float t = x / (float)(b.W - 1);
                float fall = Mathf.Clamp01(Mathf.Abs(t - peakAt) / 0.85f);
                float h = Mathf.Lerp(highSide, lowSide, fall * fall);
                result[x] = b.H * (h + (noise[x] - 0.5f) * 0.035f + (fine[x] - 0.5f) * 0.010f);
            }
            return result;
        }

        /// <summary>Sırt üstüne serpilen kazık, kaya ve çalı; "boş arazi" hissini kırar.</summary>
        internal static void Scatter(Board b, System.Random rng, float[] top, int count, float scale, Color32 tone)
        {
            for (int i = 0; i < count; i++)
            {
                int x = rng.Next(4, b.W - 4);
                int y = (int)top[x] - 2;
                int h = (int)(b.H * scale * (0.5f + (float)rng.NextDouble()));
                if (h < 2) continue;
                int kind = rng.Next(0, 3);
                if (kind == 0)
                {
                    for (int dy = 0; dy < h; dy++) b.Set(x, y + dy, tone);
                    b.Set(x + 1, y + h - 1, tone);
                }
                else if (kind == 1)
                {
                    int w = h;
                    for (int dx = -w; dx <= w; dx++)
                    {
                        int hh = (int)(h * 0.6f * Mathf.Sqrt(Mathf.Max(0f, 1f - (dx / (float)w) * (dx / (float)w))));
                        for (int dy = 0; dy < hh; dy++) b.Set(x + dx, y + dy, tone);
                    }
                }
                else
                {
                    for (int k = 0; k < 5; k++)
                        Stroke(b, x, y, x + rng.Next(-h, h + 1), y + rng.Next(h / 2, h + 1), tone, 1, 0.85f);
                }
            }
        }

        // ------------------------------------------------------------------ figürler

        /// <summary>
        /// Uzak insan izi. Bilinçli olarak anatomi taşımaz: omuzdan aşağı incelen bir gövde,
        /// bir baş ve iki kısa bacak. Bu üreticinin çizemediği şey insan figürüdür; büyük
        /// ölçekte denendiğinde sonuç korkuluğa benziyordu. 25 pikselin altında siluet
        /// "orada biri var" bilgisini verir ve anatomi sorulmaz. Sahnelerin insan ölçeği
        /// bu yüzden figürden değil, yapıdan ve araziden gelir.
        /// </summary>
        internal static void Mark(Board b, System.Random rng, int x, int footY, float h, Color32 tone, bool carrying)
        {
            if (h < 4f) return;
            int hi = Mathf.Max(4, (int)h);
            int headR = Mathf.Max(1, hi / 9);
            int bodyTop = footY + hi - headR * 2;
            int legTop = footY + (int)(hi * 0.34f);

            for (int y = legTop; y <= bodyTop; y++)
            {
                float t = (bodyTop - y) / (float)Mathf.Max(1, bodyTop - legTop);
                int half = Mathf.Max(1, (int)(hi * (0.075f + 0.045f * t)));
                for (int dx = -half; dx <= half; dx++) b.Set(x + dx, y, tone);
            }
            FillEllipse(b, x, footY + hi - headR, headR, headR, tone);
            int spread = Mathf.Max(1, (int)(hi * 0.055f));
            for (int dy = 0; dy < legTop - footY; dy++)
            {
                b.Set(x - spread, legTop - dy, tone);
                b.Set(x + spread, legTop - dy, tone);
                if (hi <= 16) continue;
                b.Set(x - spread + 1, legTop - dy, tone);
                b.Set(x + spread - 1, legTop - dy, tone);
            }
            if (!carrying) return;
            // Taşınan sedye: iki iz arasına gerilmiş yatay bir çubuk.
            int deck = footY + (int)(hi * 0.46f);
            int reach = (int)(hi * 0.95f);
            for (int dy = 0; dy < Mathf.Max(1, hi / 12); dy++)
                for (int dx = -(int)(hi * 0.12f); dx < reach; dx++)
                    b.Set(x + dx, deck + dy, tone);
            Mark(b, rng, x + reach - (int)(hi * 0.12f), footY, h, tone, false);
        }

        /// <summary>
        /// Perspektifi taşıyan çit hattı: kazıklar uzaklaştıkça küçülür ve sıklaşır.
        /// Bir manzarada derinliği en ucuz kuran öğe budur.
        /// </summary>
        internal static void FenceLine(Board b, System.Random rng, int x0, int y0, int x1, int y1, int posts)
        {
            int prevX = 0;
            int prevTop = 0;
            for (int i = 0; i < posts; i++)
            {
                // Kareli dağılım: yakın kazıklar seyrek, uzaktakiler sık.
                float t = Mathf.Pow(i / (float)(posts - 1), 0.62f);
                int x = (int)Mathf.Lerp(x0, x1, t);
                int y = (int)Mathf.Lerp(y0, y1, t);
                int h = (int)Mathf.Lerp(46f, 8f, t);
                int w = Mathf.Max(1, (int)Mathf.Lerp(5f, 1f, t));
                for (int dy = 0; dy < h; dy++)
                    for (int dx = -w; dx <= w; dx++)
                    {
                        Color32 tone = dx >= w - 1 ? Blend(Ink, Blend(lightTone, Paper, 0.35f), 0.40f) : Ink;
                        b.Set(x + dx, y + dy, Shift(tone, rng.Next(-5, 6)));
                    }
                if (i > 0)
                {
                    Stroke(b, prevX, prevTop, x, y + (int)(h * 0.78f), Blend(Ink, Petrol, 0.22f), 2, 0.75f);
                    Stroke(b, prevX, prevTop - (int)(h * 0.30f), x, y + (int)(h * 0.42f), Blend(Ink, Petrol, 0.22f), 2, 0.6f);
                }
                prevX = x;
                prevTop = y + (int)(h * 0.78f);
            }
        }

        /// <summary>
        /// Yol taşı: üst üste yığılmış taşlar ve üzerine çakılmış tahta haç. Dağ yollarında
        /// bunlar yön ve mesafe işaretiydi; sahneye insan varlığını figür çizmeden getirir.
        /// </summary>
        internal static void Waymarker(Board b, System.Random rng, int x, int y, int height)
        {
            int courses = 5;
            for (int i = 0; i < courses; i++)
            {
                float t = i / (float)courses;
                int half = (int)(height * (0.20f - 0.10f * t));
                int ch = (int)(height * 0.11f);
                int yy = y + (int)(height * 0.52f * t);
                for (int dx = -half; dx <= half; dx++)
                {
                    // Işığa bakan yüz açık, gölge yüz koyu.
                    float across = (dx + half) / (float)Mathf.Max(1, half * 2);
                    float lit = lightDir > 0f ? across * 0.30f - 0.04f : 0.26f - across * 0.30f;
                    Color32 tone = Blend(Soot, Blend(Petrol, Paper, 0.14f), Mathf.Clamp01(lit));
                    for (int dy = 0; dy < ch; dy++) b.Set(x + dx, yy + dy, Shift(tone, rng.Next(-7, 8)));
                }
                Stroke(b, x - half, yy, x + half, yy, Blend(Soot, Petrol, 0.35f), 2, 0.5f);
            }
            int postTop = y + height;
            for (int dy = (int)(height * 0.50f); dy < height; dy++)
                for (int dx = -2; dx <= 2; dx++) b.Set(x + dx, y + dy, Ink);
            for (int dx = -(int)(height * 0.11f); dx <= (int)(height * 0.11f); dx++)
                for (int dy = 0; dy < 4; dy++) b.Set(x + dx, postTop - (int)(height * 0.14f) + dy, Ink);
        }

        /// <summary>Devrik gövde: ön planda ölçek ve doku veren, figüre ihtiyaç duymayan öğe.</summary>
        internal static void FallenTrunk(Board b, System.Random rng, int x, int y, int length, int thickness)
        {
            for (int dx = 0; dx < length; dx++)
            {
                float t = dx / (float)length;
                int th = (int)(thickness * (1f - t * 0.45f));
                int lift = (int)(t * thickness * 0.35f);
                for (int dy = 0; dy < th; dy++)
                {
                    Color32 tone = dy > th - Mathf.Max(1, thickness / 5)
                        ? Blend(Ink, Blend(lightTone, Paper, 0.35f), 0.40f)
                        : Ink;
                    b.Set(x + dx, y + lift + dy, Shift(tone, rng.Next(-6, 7)));
                }
                if (rng.Next(0, 11) == 0)
                    Stroke(b, x + dx, y + lift, x + dx + rng.Next(10, 40), y + lift + rng.Next(-3, 4),
                        Blend(Ink, Petrol, 0.35f), 1, 0.5f);
            }
            // Kırık uçtaki dallar.
            for (int i = 0; i < 4; i++)
                Stroke(b, x + length - 6, y + thickness / 2, x + length + rng.Next(20, 70),
                    y + thickness / 2 + rng.Next(-34, 35), Ink, 3, 1f);
        }

        internal enum Pose { Stand, Walk, CarryBag, CarryBundle, Pull, Lantern }

        /// <summary>
        /// Figür. Işık tarafına kaydırılmış açık bir kopya önce çizilir; üstüne gövde
        /// basılınca ışık tarafında ince bir kenar kalır ve siluet hacim kazanır.
        /// </summary>
        internal static void Figure(Board b, System.Random rng, int x, int footY, float h, Color32 tone, Pose pose, int variant)
        {
            if (h < 6f) return;
            int rim = Mathf.Max(1, (int)(h * 0.012f));
            Color32 rimTone = Blend(tone, Blend(lightTone, Paper, 0.30f), 0.55f);
            if (h > 40f) FigureShape(b, x + (int)(lightDir * rim), footY, h, rimTone, pose, variant);
            FigureShape(b, x, footY, h, tone, pose, variant);
        }

        /// <summary>
        /// Baş, başlık, omuz, aşağı doğru genişleyen palto, kollar, bacaklar ve poza göre
        /// yük. Çubuk figür ölçek vermiyordu; palto silueti kolonun kim olduğunu okutuyor.
        /// </summary>
        /// <summary>
        /// Oranlar bilinçli olarak dardır. Önceki değerler (etek yarı genişliği boyun 0,162'si,
        /// şapka siperi 0,177'si) figürü korkuluğa çeviriyordu; insan silueti omuzda boyun
        /// yaklaşık 0,075'i, etekte 0,095'i kadar geniştir.
        /// </summary>
        internal static void FigureShape(Board b, int x, int footY, float h, Color32 tone, Pose pose, int variant)
        {
            int headR = Mathf.Max(1, (int)(h * 0.044f));
            int headY = footY + (int)(h * 0.925f);
            int shoulderY = footY + (int)(h * 0.800f);
            int hemY = footY + (int)(h * 0.400f);
            int lean = (variant % 4 == 0) ? -1 : 1;
            int headX = x + (int)(lean * h * 0.008f);

            FillEllipse(b, headX, headY, headR, (int)(headR * 1.15f), tone);
            if (h > 46f)
            {
                // Kasket: dar bir siper, başın önüne doğru.
                int brim = (int)(headR * 1.25f);
                for (int dx = -brim; dx <= brim; dx++)
                    for (int dy = 0; dy < Mathf.Max(1, (int)(h * 0.007f)); dy++)
                        b.Set(headX + dx + (int)(lean * headR * 0.4f), headY + headR - dy, tone);
            }
            for (int dy = shoulderY; dy < headY - headR / 2; dy++)
                for (int dx = -Mathf.Max(1, (int)(h * 0.016f)); dx <= Mathf.Max(1, (int)(h * 0.016f)); dx++)
                    b.Set(headX + dx, dy, tone);

            // Palto: omuzda dar, etekte hafif genişleyen.
            for (int y = hemY; y <= shoulderY; y++)
            {
                float t = (shoulderY - y) / (float)Mathf.Max(1, shoulderY - hemY);
                int half = Mathf.Max(1, (int)(h * (0.075f + 0.020f * t)));
                for (int dx = -half; dx <= half; dx++) b.Set(x + dx, y, tone);
            }

            int stride = pose == Pose.Walk ? (int)(h * 0.055f) : (int)(h * 0.018f);
            int legW = Mathf.Max(1, (int)(h * 0.019f));
            int legOffset = Mathf.Max(1, (int)(h * 0.030f));
            for (int dy = 0; dy < hemY - footY; dy++)
            {
                float t = dy / (float)Mathf.Max(1, hemY - footY);
                int lx = x - legOffset - (int)(stride * t);
                int rx = x + legOffset + (int)(stride * t);
                for (int k = -legW; k <= legW; k++)
                {
                    b.Set(lx + k, hemY - dy, tone);
                    b.Set(rx + k, hemY - dy, tone);
                }
            }
            if (h > 46f)
            {
                int bootOut = Mathf.Max(2, (int)(h * 0.030f));
                for (int dy = 0; dy < Mathf.Max(1, (int)(h * 0.018f)); dy++)
                {
                    for (int k = -legW; k <= bootOut; k++) b.Set(x - legOffset - stride + k, footY + dy, tone);
                    for (int k = -bootOut; k <= legW; k++) b.Set(x + legOffset + stride + k, footY + dy, tone);
                }
            }

            int armTop = shoulderY - (int)(h * 0.012f);
            int handY = armTop - (int)(h * 0.30f);
            int armW = Mathf.Max(1, (int)(h * 0.016f));
            int armOut = (int)(h * 0.072f);
            switch (pose)
            {
                case Pose.CarryBag:
                    Limb(b, x - armOut, armTop, x - (int)(h * 0.085f), handY, armW, tone);
                    Limb(b, x + armOut, armTop, x + (int)(h * 0.080f), handY, armW, tone);
                    FillRectI(b, x + (int)(h * 0.058f), handY - (int)(h * 0.055f), x + (int)(h * 0.118f), handY, tone);
                    break;
                case Pose.CarryBundle:
                    Limb(b, x - armOut, armTop, x - (int)(h * 0.040f), armTop + (int)(h * 0.040f), armW, tone);
                    Limb(b, x + armOut, armTop, x + (int)(h * 0.040f), armTop + (int)(h * 0.040f), armW, tone);
                    FillEllipse(b, x, armTop + (int)(h * 0.032f), (int)(h * 0.072f), (int)(h * 0.048f), tone);
                    break;
                case Pose.Pull:
                    Limb(b, x - armOut, armTop, x - (int)(h * 0.165f), armTop - (int)(h * 0.020f), armW, tone);
                    Limb(b, x + armOut, armTop, x + (int)(h * 0.062f), handY, armW, tone);
                    break;
                case Pose.Lantern:
                    Limb(b, x - armOut, armTop, x - (int)(h * 0.082f), handY, armW, tone);
                    Limb(b, x + armOut, armTop, x + (int)(h * 0.155f), armTop - (int)(h * 0.050f), armW, tone);
                    break;
                default:
                    Limb(b, x - armOut, armTop, x - (int)(h * 0.086f), handY, armW, tone);
                    Limb(b, x + armOut, armTop, x + (int)(h * 0.086f), handY, armW, tone);
                    break;
            }
        }

        internal static void Limb(Board b, int x0, int y0, int x1, int y1, int width, Color32 tone)
        {
            int steps = Mathf.Max(Mathf.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0)), 1);
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = (int)Mathf.Lerp(x0, x1, t);
                int y = (int)Mathf.Lerp(y0, y1, t);
                for (int dy = -width; dy <= width; dy++)
                    for (int dx = -width; dx <= width; dx++)
                        b.Set(x + dx, y + dy, tone);
            }
        }

        /// <summary>Figürün zemine düşen gölgesi; ışığın tersine uzar ve uçta söner.</summary>
        internal static void FigureShadow(Board b, int x, int footY, float h, float alpha)
        {
            int length = (int)(h * 0.95f);
            int direction = lightDir > 0f ? -1 : 1;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)length;
                int thickness = Mathf.Max(1, (int)(h * 0.055f * (1f - t * 0.75f)));
                int px = x + direction * i;
                for (int dy = -thickness / 2; dy <= thickness / 2; dy++)
                    b.BlendPx(px, footY + dy, Soot, alpha * (1f - t) * (1f - t));
            }
        }

        /// <summary>Sedye taşıyan iki figür ve aralarındaki örtülü gövde.</summary>
        internal static void StretcherPair(Board b, System.Random rng, int x, int y, float h)
        {
            int gap = (int)(h * 0.62f);
            int deck = y + (int)(h * 0.42f);
            Color32 tone = h > 120f ? Ink : Soot;
            for (int dy = 0; dy < Mathf.Max(2, (int)(h * 0.030f)); dy++)
                for (int dx = -(int)(h * 0.10f); dx < gap + (int)(h * 0.10f); dx++)
                    b.Set(x + dx, deck + dy, tone);
            for (int dx = 0; dx < gap; dx++)
            {
                float t = dx / (float)gap;
                int hh = (int)(h * 0.115f * Mathf.Sin(t * Mathf.PI));
                for (int dy = 0; dy < hh; dy++) b.Set(x + dx, deck + (int)(h * 0.030f) + dy, tone);
                if (hh > 5 && rng.Next(0, 8) == 0)
                    Stroke(b, x + dx, deck, x + dx, deck + hh, Blend(tone, Paper, 0.28f), 1, 0.35f);
            }
            Figure(b, rng, x, y, h, tone, Pose.Stand, 1);
            Figure(b, rng, x + gap, y, h, tone, Pose.Stand, 2);
        }

        internal static void StretcherOnGround(Board b, System.Random rng, int x, int y, float scale, Color32 color, int variant)
        {
            int length = (int)(scale * 1.10f);
            int poleOut = (int)(scale * 0.16f);
            // Işık tarafındaki kenar aydınlanır: yerdeki sedye düz bir tümsek olmaktan çıkar.
            Color32 lit = Blend(color, Blend(lightTone, Paper, 0.35f), 0.45f);
            for (int dx = 0; dx < length; dx++)
            {
                float t = dx / (float)length;
                float profile = Mathf.Sin(Mathf.Clamp01(t * 1.08f) * Mathf.PI);
                int hh = (int)(scale * 0.155f * (0.35f + profile));
                for (int dy = 0; dy < hh; dy++)
                {
                    bool nearTop = dy > hh - Mathf.Max(1, (int)(scale * 0.02f));
                    b.Set(x + dx, y + (int)(scale * 0.045f) + dy, nearTop ? lit : color);
                }
                if (hh > 6 && rng.Next(0, 9) == 0)
                    Stroke(b, x + dx, y + (int)(scale * 0.045f), x + dx, y + (int)(scale * 0.045f) + hh, Blend(color, Paper, 0.30f), 1, 0.4f);
            }
            for (int dy = 0; dy < Mathf.Max(2, (int)(scale * 0.045f)); dy++)
                for (int dx = -poleOut; dx < length + poleOut; dx++)
                    b.Set(x + dx, y + dy, color);
            int headX = variant % 2 == 0 ? x + (int)(scale * 0.10f) : x + length - (int)(scale * 0.10f);
            FillEllipse(b, headX, y + (int)(scale * 0.19f), Mathf.Max(2, (int)(scale * 0.075f)), Mathf.Max(2, (int)(scale * 0.070f)), color);
            for (int dy = 0; dy < (int)(scale * 0.07f); dy++)
            {
                b.Set(x - poleOut, y - dy, color);
                b.Set(x - poleOut + 1, y - dy, color);
                b.Set(x + length + poleOut - 1, y - dy, color);
                b.Set(x + length + poleOut - 2, y - dy, color);
            }
        }

        // ------------------------------------------------------------------ yapılar

        /// <summary>
        /// Kadraja giren büyük yıkıntı: iki tonlu cephe, kırık üst hat, taş sıraları, is
        /// lekeli pencere ve ayakta kalan baca.
        /// </summary>
        internal static void BigRuin(Board b, System.Random rng, int x0, int x1, int baseY, int topY)
        {
            int width = x1 - x0;
            int height = topY - baseY;
            int[] profile = new int[width];
            int cursor = 0;
            int level = height;
            while (cursor < width)
            {
                int chunk = 18 + rng.Next(0, 46);
                level = Mathf.Clamp(level + rng.Next(-(int)(height * 0.18f), (int)(height * 0.15f)), (int)(height * 0.55f), height);
                for (int dx = cursor; dx < Mathf.Min(width, cursor + chunk); dx++) profile[dx] = level;
                cursor += chunk;
            }
            for (int dx = 0; dx < width; dx++)
            {
                float t = dx / (float)width;
                Color32 face = Blend(Soot, Blend(Petrol, Paper, 0.30f), Mathf.Clamp01(0.20f - t * 0.26f));
                for (int dy = 0; dy < profile[dx]; dy++) b.Set(x0 + dx, baseY + dy, Shift(face, rng.Next(-6, 7)));
            }
            for (int dy = 12; dy < height; dy += 22)
                for (int dx = 0; dx < width; dx++)
                    if (dy < profile[dx] - 4 && rng.Next(0, 7) > 0) b.BlendPx(x0 + dx, baseY + dy, Petrol, 0.14f);
            for (int dx = 0; dx < width; dx += 30)
                for (int dy = 0; dy < height; dy++)
                    if (dy < profile[dx] - 4 && (dy / 22) % 2 == 0 && rng.Next(0, 5) > 0)
                        b.BlendPx(x0 + dx + rng.Next(-1, 2), baseY + dy, Petrol, 0.11f);

            int wx = x0 + (int)(width * 0.16f);
            int ww = (int)(width * 0.20f);
            int wy = baseY + (int)(height * 0.20f);
            int wh = (int)(height * 0.34f);
            FillTextured(b, rng, wx, wy, wx + ww, wy + wh, Blend(Ink, Soot, 0.4f), 0.2f);
            for (int dy = 0; dy < wh; dy++)
                for (int dx = -3; dx < ww + 3; dx++)
                    if (rng.Next(0, 3) == 0) b.BlendPx(wx + dx, wy + wh + dy, Ink, 0.24f * (1f - dy / (float)wh));

            int cx = x0 + (int)(width * 0.62f);
            int cw = (int)(width * 0.11f);
            int foot = baseY + profile[Mathf.Clamp((int)(width * 0.62f), 0, width - 1)] - 18;
            int chimneyTop = baseY + (int)(height * 1.42f);
            FillTextured(b, rng, cx, foot, cx + cw, chimneyTop, Soot, 0.22f);
            for (int dy = foot; dy < chimneyTop; dy += 20)
                Stroke(b, cx, dy, cx + cw, dy + rng.Next(-2, 3), Blend(Soot, Petrol, 0.28f), 2, 0.35f);
            FillTextured(b, rng, cx - 7, chimneyTop - 12, cx + cw + 7, chimneyTop, Blend(Soot, Petrol, 0.30f), 0.3f);
        }

        /// <summary>Ahır: iki tonlu cephe, dikey tahtalar, saçak ve eğimli çatı.</summary>
        internal static void Barn(Board b, System.Random rng, int x0, int x1, int baseY, int eaves)
        {
            int width = x1 - x0;
            for (int dx = 0; dx < width; dx++)
            {
                float t = dx / (float)width;
                Color32 face = Blend(Soot, Blend(Petrol, Paper, 0.22f), Mathf.Clamp01(0.16f - t * 0.20f));
                for (int y = baseY; y < eaves; y++) b.Set(x0 + dx, y, Shift(face, rng.Next(-6, 7)));
            }
            for (int x = x0 + 8; x < x1; x += 22 + rng.Next(0, 9))
            {
                Color32 plank = Blend(Soot, Petrol, 0.16f + 0.18f * (float)rng.NextDouble());
                for (int y = baseY; y < eaves; y++)
                    if (rng.Next(0, 9) > 0) b.BlendPx(x, y, plank, 0.55f);
            }
            FillTextured(b, rng, x0 - 14, eaves, x1, eaves + 14, Ink, 0.2f);
            for (int dx = 0; dx < width; dx++)
            {
                float t = dx / (float)width;
                int roof = eaves + 14 + (int)((Height * 0.115f) * (1f - Mathf.Abs(t - 0.28f) * 1.65f));
                for (int y = eaves + 14; y < roof; y++) b.Set(x0 + dx, y, Shift(Blend(Soot, Ink, 0.5f), rng.Next(-6, 7)));
            }
            Gouge(b, rng, x0, baseY, x1, eaves, Blend(Soot, Petrol, 0.44f), 26, 0.9f);
        }

        internal static void RuinedHouse(Board b, System.Random rng, int x, int baseY, int width, int height, float chimneyAt)
        {
            int[] profile = new int[width];
            int cursor = 0;
            int level = height - rng.Next(0, (int)(height * 0.18f));
            while (cursor < width)
            {
                int chunk = 10 + rng.Next(0, 30);
                level = Mathf.Clamp(level + rng.Next(-(int)(height * 0.22f), (int)(height * 0.20f)), (int)(height * 0.42f), height);
                for (int dx = cursor; dx < Mathf.Min(width, cursor + chunk); dx++) profile[dx] = level;
                cursor += chunk;
            }
            for (int dx = 0; dx < width; dx++)
            {
                float t = dx / (float)width;
                float lit = lightDir < 0f ? 0.16f - t * 0.20f : t * 0.20f - 0.04f;
                Color32 face = Blend(Soot, Petrol, Mathf.Clamp01(lit));
                for (int dy = 0; dy < profile[dx]; dy++) b.Set(x + dx, baseY + dy, Shift(face, rng.Next(-6, 7)));
            }
            for (int dy = 9; dy < height; dy += 15)
                for (int dx = 0; dx < width; dx++)
                    if (dy < profile[dx] - 3 && rng.Next(0, 7) > 0) b.BlendPx(x + dx, baseY + dy, Petrol, 0.13f);

            int windows = Mathf.Max(1, width / 95);
            for (int i = 0; i < windows; i++)
            {
                int wx = x + (int)(width * (0.16f + i * (0.68f / windows)));
                FillTextured(b, rng, wx, baseY + height / 6, wx + width / 10, baseY + height / 6 + (int)(height * 0.32f), Blend(Ink, Soot, 0.35f), 0.22f);
            }
            if (chimneyAt < 0f) return;
            int cx = x + (int)(width * chimneyAt);
            int cw = Mathf.Max(11, width / 12);
            int foot = baseY + profile[Mathf.Clamp((int)(width * chimneyAt), 0, width - 1)] - 12;
            int top = baseY + (int)(height * 1.55f);
            FillTextured(b, rng, cx, foot, cx + cw, top, Soot, 0.24f);
            FillTextured(b, rng, cx - 5, top - 9, cx + cw + 5, top, Blend(Soot, Petrol, 0.32f), 0.3f);
        }

        internal static void Pier(Board b, System.Random rng, int x0, int x1, int y0, int y1)
        {
            int width = x1 - x0;
            for (int dx = 0; dx < width; dx++)
            {
                float t = dx / (float)width;
                float lit = lightDir < 0f ? 0.22f - t * 0.26f : t * 0.26f - 0.04f;
                Color32 face = Blend(Ink, Petrol, Mathf.Clamp01(lit));
                for (int y = y0; y < y1; y++) b.Set(x0 + dx, y, Shift(face, rng.Next(-6, 7)));
            }
            FillTextured(b, rng, x0 - 7, y1 - 14, x1 + 7, y1, Ink, 0.22f);
            for (int y = y0; y < y1; y += 19)
                Stroke(b, x0, y, x1, y + rng.Next(-2, 3), Blend(Ink, Petrol, 0.32f), 2, 0.35f);
        }

        internal static void Truss(Board b, System.Random rng, int x0, int x1, int deckY, int depth)
        {
            FillTextured(b, rng, x0, deckY - depth, x1, deckY - depth + 9, Ink, 0.2f);
            for (int x = x0; x < x1; x += 62)
            {
                Stroke(b, x, deckY, x + 31, deckY - depth, Ink, 6, 1f);
                Stroke(b, x + 31, deckY - depth, x + 62, deckY, Ink, 6, 1f);
                Stroke(b, x, deckY, x, deckY - depth, Ink, 4, 1f);
                Stroke(b, x + 31, deckY, x + 31, deckY - depth, Ink, 3, 0.75f);
            }
        }

        internal static void Cart(Board b, System.Random rng, int x, int y, float scale)
        {
            int w = (int)(scale * 1.15f);
            int h = (int)(scale * 0.40f);
            int bodyY = y + (int)(scale * 0.28f);
            for (int dx = 0; dx < w; dx++)
            {
                float t = dx / (float)w;
                float lit = lightDir < 0f ? 0.20f - t * 0.24f : t * 0.24f - 0.04f;
                Color32 face = Blend(Ink, Petrol, Mathf.Clamp01(lit));
                for (int dy = 0; dy < h; dy++) b.Set(x + dx, bodyY + dy, Shift(face, rng.Next(-5, 6)));
            }
            for (int i = 0; i < 5; i++)
                Stroke(b, x, bodyY + 3 + i * (h / 5), x + w, bodyY + 3 + i * (h / 5), Blend(Ink, Petrol, 0.34f), 2, 0.4f);
            Wheel(b, x + (int)(w * 0.24f), y + (int)(scale * 0.24f), (int)(scale * 0.24f));
            Wheel(b, x + (int)(w * 0.78f), y + (int)(scale * 0.24f), (int)(scale * 0.24f));
            Stroke(b, x, bodyY + h / 2, x - (int)(scale * 0.52f), bodyY + h, Ink, Mathf.Max(2, (int)(scale * 0.045f)), 1f);
        }

        internal static void Wheel(Board b, int cx, int cy, int radius)
        {
            if (radius < 3) return;
            for (int a = 0; a < 360; a += 2)
            {
                float r = a * Mathf.Deg2Rad;
                for (int k = 0; k < 3; k++)
                    b.Set(cx + (int)(Mathf.Cos(r) * (radius - k)), cy + (int)(Mathf.Sin(r) * (radius - k)), Ink);
            }
            for (int a = 0; a < 360; a += 45)
            {
                float r = a * Mathf.Deg2Rad;
                Stroke(b, cx, cy, cx + (int)(Mathf.Cos(r) * radius), cy + (int)(Mathf.Sin(r) * radius), Ink, 2, 1f);
            }
        }

        internal static void Lantern(Board b, int x, int y)
        {
            System.Random fixedSeed = new System.Random(31);
            FillTextured(b, fixedSeed, x - 9, y, x + 9, y + 26, Ink, 0.2f);
            FillTextured(b, fixedSeed, x - 6, y + 3, x + 6, y + 23, Blend(Mustard, Paper, 0.50f), 0.15f);
            FillTextured(b, fixedSeed, x - 11, y + 26, x + 11, y + 31, Ink, 0.2f);
            Stroke(b, x, y + 31, x, y + 46, Ink, 3, 1f);
        }

        internal static void BareTree(Board b, System.Random rng, int x, int baseY, int height)
        {
            int trunk = Mathf.Max(2, height / 20);
            for (int dy = 0; dy < height; dy++)
            {
                int w = (int)(trunk * (1f - dy / (float)height * 0.60f));
                int sway = (int)(Mathf.Sin(dy * 0.014f) * 6f);
                for (int dx = -w; dx <= w; dx++) b.Set(x + dx + sway, baseY + dy, Soot);
            }
            for (int i = 0; i < 6; i++)
            {
                int y = baseY + (int)(height * (0.50f + 0.46f * i / 5f));
                int dir = i % 2 == 0 ? 1 : -1;
                int len = (int)(height * (0.28f - 0.032f * i));
                int mx = x + dir * len;
                int my = y + (int)(len * 0.80f);
                Stroke(b, x, y, mx, my, Soot, Mathf.Max(2, trunk - i / 2), 1f);
                Stroke(b, mx, my, mx + dir * (int)(len * 0.45f), my + (int)(len * 0.55f), Soot, Mathf.Max(1, trunk - 1 - i / 2), 1f);
                Stroke(b, mx, my, mx + dir * (int)(len * 0.16f), my + (int)(len * 0.72f), Soot, Mathf.Max(1, trunk - 2), 1f);
            }
        }

        internal static void PineRidge(Board b, System.Random rng, float[] top, int count, float spread, float haze)
        {
            Color32 tone = Blend(Blend(Petrol, Soot, 0.72f), skyTone, haze);
            for (int i = 0; i < count; i++)
            {
                int x = (int)(b.W * ((i + 0.5f) / count) + rng.Next(-26, 27));
                if (x < 2 || x >= b.W - 2) continue;
                Pine(b, rng, x, (int)top[x] - 3, (int)(b.H * spread * (0.50f + 0.95f * (float)rng.NextDouble())), tone);
                if (rng.Next(0, 3) != 0) continue;
                int sx = x + rng.Next(14, 44);
                if (sx < b.W - 2) Pine(b, rng, sx, (int)top[Mathf.Clamp(sx, 0, b.W - 1)] - 3, (int)(b.H * spread * 0.42f), tone);
            }
        }

        internal static void Pine(Board b, System.Random rng, int x, int baseY, int height, Color32 color)
        {
            if (height < 8) return;
            int trunk = Mathf.Max(1, height / 18);
            for (int dy = 0; dy < height / 4; dy++)
                for (int dx = -trunk; dx <= trunk; dx++) b.Set(x + dx, baseY + dy, color);

            int tiers = 4 + rng.Next(0, 3);
            float lean = (float)rng.NextDouble() * 0.20f - 0.10f;
            for (int i = 0; i < tiers; i++)
            {
                float t = i / (float)tiers;
                int y0 = baseY + (int)(height * (0.08f + t * 0.74f));
                int half = (int)(height * (0.24f + 0.10f * (float)rng.NextDouble()) * (1f - t * 0.74f));
                int tierHeight = (int)(height * 0.34f * (1f - t * 0.38f));
                if (tierHeight < 2 || half < 1) continue;
                for (int dy = 0; dy < tierHeight; dy++)
                {
                    int w = (int)(half * (1f - dy / (float)tierHeight));
                    int shift = (int)(lean * dy);
                    for (int dx = -w; dx <= w; dx++) b.Set(x + dx + shift, y0 + dy, color);
                    if (w < 2 || rng.Next(0, 3) != 0) continue;
                    int spur = 1 + rng.Next(0, 3);
                    b.Set(x - w - spur + shift, y0 + dy, color);
                    b.Set(x + w + spur + shift, y0 + dy, color);
                }
            }
        }

        // ------------------------------------------------------ katman ve doku çekirdeği

        internal static void Layer(Board b, System.Random rng, float[] top, Color32 tone, float haze,
            int gouges, float gougeDepth, int chatter)
        {
            Color32 hazed = Blend(tone, skyTone, haze);
            float[] broad = ValueNoise(b.W, rng, 7);
            float[] fine = ValueNoise(b.W, rng, 61);
            float[] edge = ValueNoise(b.W, rng, 150);

            int[] edgeY = new int[b.W];
            for (int x = 0; x < b.W; x++)
            {
                int jitter = chatter <= 0 ? 0 : (int)((edge[x] - 0.5f) * 2f * chatter) + (rng.Next(0, 22) == 0 ? rng.Next(-chatter * 3, chatter * 3) : 0);
                edgeY[x] = Mathf.Clamp((int)top[x] + jitter, 0, b.H - 1);
            }

            for (int x = 0; x < b.W; x++)
            {
                int yTop = edgeY[x];
                int columnShift = (int)((broad[x] - 0.5f) * 16f + (fine[x] - 0.5f) * 7f);
                for (int y = 0; y <= yTop; y++)
                {
                    float depth = (yTop - y) / (float)Mathf.Max(1, yTop);
                    b.Set(x, y, Shift(hazed, columnShift - (int)(depth * 9f) + rng.Next(-3, 4)));
                }
            }

            for (int x = 0; x < b.W; x++)
            {
                if (rng.Next(0, 3) != 0) continue;
                int height = 1 + rng.Next(0, 3 + chatter);
                for (int k = 1; k <= height; k++)
                    if (rng.Next(0, 2) == 0) b.Set(x, edgeY[x] + k, Shift(hazed, rng.Next(-6, 7)));
            }

            if (gouges <= 0) return;
            Color32 gougeTone = Blend(hazed, skyTone, 0.30f);
            for (int i = 0; i < gouges; i++)
            {
                int x = rng.Next(0, b.W);
                int depthPx = (int)(edgeY[x] * gougeDepth);
                if (depthPx < 4) continue;
                int y = edgeY[x] - rng.Next(2, depthPx);
                int direction = rng.Next(0, 2) == 0 ? 1 : -1;
                Stroke(b, x, y, x + direction * (40 + rng.Next(0, 240)), y + rng.Next(-7, 8), gougeTone,
                    1 + rng.Next(0, 3), 0.30f + 0.34f * (float)rng.NextDouble());
            }
        }

        internal static void FillTextured(Board b, System.Random rng, int x0, int y0, int x1, int y1, Color32 tone, float variation)
        {
            int amount = Mathf.Max(1, (int)(variation * 40f));
            for (int y = Mathf.Max(0, y0); y < Mathf.Min(b.H, y1); y++)
                for (int x = Mathf.Max(0, x0); x < Mathf.Min(b.W, x1); x++)
                    b.Set(x, y, Shift(tone, rng.Next(-amount, amount + 1)));
        }

        internal static void FillRectI(Board b, int x0, int y0, int x1, int y1, Color32 tone)
        {
            for (int y = Mathf.Max(0, y0); y < Mathf.Min(b.H, y1); y++)
                for (int x = Mathf.Max(0, x0); x < Mathf.Min(b.W, x1); x++)
                    b.Set(x, y, tone);
        }

        internal static void FillEllipse(Board b, int cx, int cy, int rx, int ry, Color32 tone)
        {
            if (rx < 1) rx = 1;
            if (ry < 1) ry = 1;
            for (int y = -ry; y <= ry; y++)
                for (int x = -rx; x <= rx; x++)
                    if ((x * x) / (float)(rx * rx) + (y * y) / (float)(ry * ry) <= 1f)
                        b.Set(cx + x, cy + y, tone);
        }

        internal static void Gouge(Board b, System.Random rng, int x0, int y0, int x1, int y1, Color32 tone, int count, float alpha)
        {
            for (int i = 0; i < count; i++)
            {
                int x = rng.Next(x0, Mathf.Max(x0 + 1, x1));
                int y = rng.Next(y0, Mathf.Max(y0 + 1, y1));
                Stroke(b, x, y, x + 30 + rng.Next(0, 150), y + rng.Next(-5, 6), tone, 1 + rng.Next(0, 2), alpha * 0.45f);
            }
        }

        internal static void Stroke(Board b, int x0, int y0, int x1, int y1, Color32 color, int thickness, float alpha)
        {
            int steps = Mathf.Max(Mathf.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0)), 1);
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float taper = Mathf.Min(1f, Mathf.Min(t, 1f - t) * steps / 14f);
                int width = Mathf.Max(1, (int)(thickness * (0.45f + 0.55f * taper)));
                int x = (int)Mathf.Lerp(x0, x1, t);
                int y = (int)Mathf.Lerp(y0, y1, t);
                int half = width / 2;
                for (int oy = -half; oy <= half; oy++)
                    for (int ox = -half; ox <= half; ox++)
                    {
                        if (alpha >= 1f) b.Set(x + ox, y + oy, color);
                        else b.BlendPx(x + ox, y + oy, color, alpha * taper);
                    }
            }
        }

        internal static void SkyWash(Board b, System.Random rng, Color32 high, float highAmount, Color32 low, float lowAmount, float horizon)
        {
            float[] drift = ValueNoise(b.W, rng, 5);
            for (int y = 0; y < b.H; y++)
            {
                float v = y / (float)(b.H - 1);
                Color32 color = Paper;
                if (v > horizon) color = Blend(color, high, highAmount * Mathf.InverseLerp(horizon, 1f, v));
                else color = Blend(color, low, lowAmount * (1f - Mathf.InverseLerp(0f, horizon, v)));
                for (int x = 0; x < b.W; x++) b.Set(x, y, Shift(color, (int)((drift[x] - 0.5f) * 9f)));
            }
        }

        internal static void SkyStreaks(Board b, System.Random rng, float from, float to, Color32 color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float t = (float)rng.NextDouble();
                int y = (int)Mathf.Lerp(b.H * from, b.H * to, t * t);
                int thickness = 2 + rng.Next(0, 4);
                int x = rng.Next(-200, b.W);
                int span = 300 + rng.Next(0, (int)(b.W * 0.7f));
                float alpha = 0.08f + 0.15f * (float)rng.NextDouble();
                while (x < b.W && span > 0)
                {
                    int run = 26 + rng.Next(0, 120);
                    Stroke(b, x, y, x + run, y + rng.Next(-2, 3), color, thickness, alpha);
                    x += run + 12 + rng.Next(0, 70);
                    span -= run;
                }
            }
        }

        /// <summary>
        /// Bulut kütlesi: altı oyulmuş, üstü ışığa dönük yatay yığın. Gökyüzüne biçim veren
        /// şey düz bir yıkama değil, bu kütlelerdir.
        /// </summary>
        internal static void CloudMass(Board b, System.Random rng, float from, float to, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int y = (int)Mathf.Lerp(b.H * from, b.H * to, (float)rng.NextDouble());
                int x0 = rng.Next(-260, (int)(b.W * 0.85f));
                int width = 340 + rng.Next(0, 760);
                int height = 22 + rng.Next(0, 60);
                float[] under = ValueNoise(Mathf.Max(2, width), rng, 5);
                float[] over = ValueNoise(Mathf.Max(2, width), rng, 8);
                Color32 shade = Blend(skyTone, Petrol, 0.22f + 0.12f * (float)rng.NextDouble());
                Color32 lit = Blend(skyTone, lightTone, 0.40f);
                for (int dx = 0; dx < width; dx++)
                {
                    int depth = Mathf.Max(1, (int)(height * (0.30f + 0.70f * under[dx])));
                    for (int dy = 0; dy < depth; dy++)
                    {
                        if (rng.Next(0, 9) == 0) continue;
                        b.BlendPx(x0 + dx, y + dy, shade, 0.34f * (1f - dy / (float)depth));
                    }
                    int crown = Mathf.Max(1, (int)(height * 0.36f * over[dx]));
                    for (int dy = 0; dy < crown; dy++)
                        b.BlendPx(x0 + dx, y + depth + dy, lit, 0.22f * (1f - dy / (float)crown));
                }
            }
        }

        internal static void SnowStreaks(Board b, System.Random rng, float[] top, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int x = rng.Next(0, b.W);
                int y = (int)top[Mathf.Clamp(x, 0, b.W - 1)] - rng.Next(2, 52);
                Stroke(b, x, y, x + 30 + rng.Next(0, 210), y, Paper, 1 + rng.Next(0, 2), 0.24f);
            }
        }

        internal static void WaterStreaks(Board b, System.Random rng, float[] top, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int x = rng.Next(0, b.W);
                int y = rng.Next(0, (int)top[Mathf.Clamp(x, 0, b.W - 1)]);
                Color32 tone = rng.Next(0, 2) == 0 ? Blend(Paper, Petrol, 0.22f) : Blend(Petrol, Soot, 0.30f);
                Stroke(b, x, y, x + rng.Next(22, 130), y, tone, 1 + rng.Next(0, 2), 0.45f);
            }
        }

        // ------------------------------------------------------------------ baskı dokusu

        internal static void InkDensity(Board b, System.Random rng)
        {
            float[] fx = ValueNoise(b.W, rng, 4);
            float[] fy = ValueNoise(b.H, rng, 3);
            for (int y = 0; y < b.H; y++)
                for (int x = 0; x < b.W; x++)
                    b.Pixels[y * b.W + x] = Shift(b.Pixels[y * b.W + x], (int)(((fx[x] + fy[y]) * 0.5f - 0.5f) * 22f));
        }

        internal static void PaperTooth(Board b, System.Random rng)
        {
            for (int i = 0; i < 26000; i++)
            {
                int x = rng.Next(0, b.W);
                int y = rng.Next(0, b.H);
                int len = 2 + rng.Next(0, 7);
                Color32 tone = rng.Next(0, 2) == 0 ? Paper : Ink;
                for (int k = 0; k < len; k++) b.BlendPx(x + k, y, tone, 0.05f);
            }
        }

        internal static void Halftone(Board b, System.Random rng)
        {
            int seed = rng.Next(0, 17);
            for (int y = 10 + seed; y < b.H; y += 17)
                for (int x = 10 + seed * 2; x < b.W; x += 17)
                    if (((x / 17) + (y / 17) + seed) % 3 == 0) Circle(b, x, y, 2, Paper, 0.07f);
        }

        internal static void Grain(Board b, System.Random rng, int amount)
        {
            for (int i = 0; i < b.Pixels.Length; i++)
                b.Pixels[i] = Shift(b.Pixels[i], rng.Next(-amount, amount + 1));
        }

        internal static void Vignette(Board b, System.Random rng)
        {
            float[] jitter = ValueNoise(Mathf.Max(b.W, b.H), rng, 26);
            for (int y = 0; y < b.H; y++)
            {
                for (int x = 0; x < b.W; x++)
                {
                    float ex = Mathf.Min(x, b.W - 1 - x) / (b.W * 0.5f);
                    float ey = Mathf.Min(y, b.H - 1 - y) / (b.H * 0.5f);
                    float edge = Mathf.Min(ex, ey);
                    float wobble = 0.035f * jitter[(x + y) % jitter.Length];
                    float amount = Mathf.Clamp01((0.100f + wobble - edge) / 0.100f);
                    if (amount > 0f) b.BlendPx(x, y, Ink, amount * amount * 0.70f);
                }
            }
        }

        internal static void EdgeSpeckle(Board b, System.Random rng)
        {
            for (int i = 0; i < 900; i++)
            {
                int x = rng.Next(0, b.W);
                int y = rng.Next(0, b.H);
                float ex = Mathf.Min(x, b.W - 1 - x) / (b.W * 0.5f);
                float ey = Mathf.Min(y, b.H - 1 - y) / (b.H * 0.5f);
                float edge = Mathf.Min(ex, ey);
                if (edge > 0.12f) continue;
                Circle(b, x, y, rng.Next(1, 3), Ink, 0.32f * (1f - edge / 0.12f));
            }
        }

        // ------------------------------------------------------------------ altyapı

        internal static float[] ValueNoise(int width, System.Random rng, int points)
        {
            float[] control = new float[points + 2];
            for (int i = 0; i < control.Length; i++) control[i] = (float)rng.NextDouble();
            float[] result = new float[width];
            for (int x = 0; x < width; x++)
            {
                float t = x / (float)width * points;
                int i = (int)t;
                float f = t - i;
                float smooth = (1f - Mathf.Cos(f * Mathf.PI)) * 0.5f;
                result[x] = Mathf.Lerp(control[i], control[i + 1], smooth);
            }
            return result;
        }

        internal static float[] Ridge(int width, System.Random rng, float baseY, float amplitude, int octaves)
        {
            float[] result = new float[width];
            float weight = 1f;
            float total = 0f;
            for (int o = 0; o < octaves; o++)
            {
                float[] layer = ValueNoise(width, rng, 3 + o * 5);
                for (int x = 0; x < width; x++) result[x] += (layer[x] - 0.5f) * weight;
                total += weight;
                weight *= 0.52f;
            }
            for (int x = 0; x < width; x++) result[x] = baseY + result[x] / total * amplitude * 2f;
            return result;
        }

        internal static void Circle(Board b, int cx, int cy, int radius, Color32 color, float alpha = 1f)
        {
            for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                    if (x * x + y * y <= radius * radius)
                    {
                        if (alpha >= 1f) b.Set(cx + x, cy + y, color);
                        else b.BlendPx(cx + x, cy + y, color, alpha);
                    }
        }

        internal static Color32 Blend(Color32 a, Color32 b, float amount)
        {
            amount = Mathf.Clamp01(amount);
            return new Color32(
                (byte)(a.r + (b.r - a.r) * amount),
                (byte)(a.g + (b.g - a.g) * amount),
                (byte)(a.b + (b.b - a.b) * amount),
                255);
        }

        internal static Color32 Shift(Color32 color, int amount)
        {
            return new Color32(ClampByte(color.r + amount), ClampByte(color.g + amount), ClampByte(color.b + amount), 255);
        }

        internal static byte ClampByte(int value)
        {
            return (byte)(value < 0 ? 0 : (value > 255 ? 255 : value));
        }

        internal sealed class Board
        {
            internal readonly int W;
            internal readonly int H;
            internal readonly Color32[] Pixels;

            internal Board(int width, int height)
            {
                W = width;
                H = height;
                Pixels = new Color32[width * height];
            }

            internal void Set(int x, int y, Color32 color)
            {
                if ((uint)x >= (uint)W || (uint)y >= (uint)H) return;
                Pixels[y * W + x] = color;
            }

            internal void BlendPx(int x, int y, Color32 color, float amount)
            {
                if ((uint)x >= (uint)W || (uint)y >= (uint)H) return;
                if (amount <= 0f) return;
                if (amount > 1f) amount = 1f;
                int index = y * W + x;
                Color32 current = Pixels[index];
                Pixels[index] = new Color32(
                    (byte)(current.r + (color.r - current.r) * amount),
                    (byte)(current.g + (color.g - current.g) * amount),
                    (byte)(current.b + (color.b - current.b) * amount),
                    255);
            }
        }
    }
}
