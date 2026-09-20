using System.Collections.Generic;
using UnityEngine;
using static OrdinaryFronts.Editor.LinocutSceneFactory;

namespace OrdinaryFronts.Editor
{
    /// <summary>
    /// Amsterdam 1945 bölümünün sahne arka planlarını üretir. Teknik ve palet
    /// <see cref="LinocutSceneFactory"/> ile aynıdır; yalnız konu değişir: dağ ve kanyon
    /// yerine düz polder, donmuş kanal, otuz iki kilometrelik set ve Frizya çiftliği.
    /// <para>
    /// Düz arazi silüet için hem kolay hem tehlikelidir: derinlik dağ sırtlarından değil,
    /// ufka kaçan tek bir çizgiden (yol, set, hendek) ve o çizgi boyunca küçülen tekrarlı
    /// öğelerden (kavak, çit kazığı, yürüyen insan) kurulur. Her sahnede bu perspektif
    /// çizgisi vardır.
    /// </para>
    /// <para>
    /// İnsan ve ağaç bu dosyada yeniden çizilir. Yugoslavya sahnelerinin çubuk figürü ve
    /// blok dallı ağacı bu bölümün yakın planlarında yetersiz kalıyordu: figür artık omuz
    /// kavisi, palto boyu (dize ya da bileğe), başlık türü ve taşıdığı yükle ayrışan bir
    /// silüettir; ağaç ise özyineli dallanmayla çizilir ve inceldikçe dal kalınlığı düşer.
    /// </para>
    /// </summary>
    internal static class HungerWinterSceneFactory
    {
        internal static readonly string[] FileNames =
        {
            "bg_frozen_canal.png",
            "bg_polder_road.png",
            "bg_afsluitdijk.png",
            "bg_frisian_farm.png",
            "bg_canal_night.png"
        };

        /// <summary>Silüet türü: palto boyu ve başlık buradan gelir.</summary>
        internal enum Kind { Man, Woman, Child, HatMan }

        /// <summary>Taşınan yük ya da kol duruşu.</summary>
        internal enum Carry { None, Pot, Bag, Pull, Walk, HoldHand }

        internal static Texture2D Render(int scene)
        {
            Board board = new Board(Width, Height);
            System.Random random = new System.Random(19450115 + scene * 733);

            switch (scene)
            {
                case 0: DrawFrozenCanal(board, random); break;
                case 1: DrawPolderRoad(board, random); break;
                case 2: DrawAfsluitdijk(board, random); break;
                case 3: DrawFrisianFarm(board, random); break;
                default: DrawCanalNight(board, random); break;
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
        /// Donmuş kanal, karşı kıyıda basamaklı cepheler, bu kıyıda merkez mutfak kuyruğu.
        /// Işık sağdan ve alçak; cephelerin çatı çizgisinde kar, buzda pencere yansımaları.
        /// </summary>
        internal static void DrawFrozenCanal(Board b, System.Random rng)
        {
            skyTone = Blend(Paper, Petrol, 0.08f);
            lightDir = 1f;
            lightTone = Blend(Paper, Mustard, 0.35f);

            SkyWash(b, rng, Petrol, 0.16f, Paper, 0f, 0.60f);
            HorizonGlow(b, (int)(b.W * 0.88f), (int)(b.H * 0.62f), (int)(b.H * 0.42f), Mustard, 0.16f);
            CloudBank(b, rng, (int)(b.H * 0.80f), (int)(b.W * 0.05f), (int)(b.W * 0.55f), (int)(b.H * 0.07f), 0.22f);
            CloudBank(b, rng, (int)(b.H * 0.90f), (int)(b.W * 0.50f), (int)(b.W * 0.50f), (int)(b.H * 0.05f), 0.16f);
            SkyStreaks(b, rng, 0.66f, 0.98f, Petrol, 9);

            // Karşı kıyının cepheleri: tek bir silüet bandı, her ev kendi alınlığıyla.
            float[] facades = new float[b.W];
            int quayTop = (int)(b.H * 0.415f);
            for (int x = 0; x < b.W; x++) facades[x] = quayTop;
            int cursor = -20;
            int house = 0;
            while (cursor < b.W + 40)
            {
                int width = 92 + rng.Next(0, 60);
                int top = (int)(b.H * (0.52f + 0.14f * (float)rng.NextDouble()));
                GableProfile(facades, cursor, cursor + width, top, house % 5, rng);
                cursor += width + 2;
                house++;
            }
            Color32 facadeTone = Blend(Ink, skyTone, 0.16f);
            Layer(b, rng, facades, facadeTone, 0f, 0, 0f, 0);
            SnowCap(b, facades, Blend(Paper, skyTone, 0.20f), 4, 0.9f);
            Windows(b, rng, facades, quayTop + 16, 44, 34, Blend(facadeTone, Paper, 0.10f), 0.08f);

            // Karşı rıhtım duvarı ve buz.
            FillTextured(b, rng, 0, (int)(b.H * 0.385f), b.W, quayTop + 2, Blend(Ink, Petrol, 0.34f), 0.2f);
            for (int x = 0; x < b.W; x += 46) Stroke(b, x, (int)(b.H * 0.385f), x, quayTop, Blend(Ink, Petrol, 0.55f), 1, 0.5f);
            int iceTop = (int)(b.H * 0.385f);
            int iceBottom = (int)(b.H * 0.215f);
            Color32 ice = Blend(Paper, Petrol, 0.26f);
            for (int y = iceBottom; y < iceTop; y++)
            {
                float t = (y - iceBottom) / (float)(iceTop - iceBottom);
                Color32 tone = Blend(ice, Blend(Petrol, Ink, 0.30f), 0.30f * t);
                for (int x = 0; x < b.W; x++) b.Set(x, y, Shift(tone, rng.Next(-4, 5)));
            }
            float[] iceLine = new float[b.W];
            for (int x = 0; x < b.W; x++) iceLine[x] = iceTop;
            WaterStreaks(b, rng, iceLine, 90);
            for (int i = 0; i < 26; i++)
            {
                int x = rng.Next(0, b.W);
                int y0 = iceTop - rng.Next(4, 30);
                Stroke(b, x, y0, x + rng.Next(-3, 4), y0 - rng.Next(30, 110), Blend(Mustard, Paper, 0.45f), 2, 0.22f);
            }

            // Bu kıyı: karlı rıhtım, kenar taşı.
            FillTextured(b, rng, 0, 0, b.W, iceBottom + 1, Blend(Paper, Petrol, 0.13f), 0.25f);
            FillTextured(b, rng, 0, iceBottom - 6, b.W, iceBottom + 2, Blend(Ink, Petrol, 0.30f), 0.2f);
            for (int i = 0; i < 70; i++)
            {
                int x = rng.Next(0, b.W);
                int y = rng.Next(4, iceBottom - 10);
                Stroke(b, x, y, x + rng.Next(20, 90), y + rng.Next(-2, 3), Blend(Paper, Petrol, 0.30f), 1, 0.35f);
            }

            Elm(b, rng, (int)(b.W * 0.245f), iceBottom - 4, (int)(b.H * 0.80f), Soot);
            Elm(b, rng, (int)(b.W * 0.665f), iceBottom - 2, (int)(b.H * 0.70f), Soot);

            // Kuyruk: kümeler hâlinde, farklı boy ve başlıkta; herkes bir kap taşıyor.
            int footY = (int)(b.H * 0.105f);
            Kind[] kinds = { Kind.Woman, Kind.Man, Kind.Woman, Kind.HatMan, Kind.Woman, Kind.Child, Kind.Man, Kind.Woman, Kind.Woman, Kind.HatMan, Kind.Man };
            Carry[] carries = { Carry.Pot, Carry.Bag, Carry.Pot, Carry.None, Carry.HoldHand, Carry.None, Carry.Pot, Carry.Bag, Carry.Pot, Carry.Pot, Carry.None };
            int qx = (int)(b.W * 0.07f);
            for (int i = 0; i < kinds.Length; i++)
            {
                float h = kinds[i] == Kind.Child ? 118f : 172f + rng.Next(0, 30);
                int fy = footY + rng.Next(-3, 4);
                FigureShadow(b, qx, fy, h, 0.16f);
                Person(b, rng, qx, fy, h, Soot, kinds[i], carries[i], 1);
                qx += kinds[i] == Kind.Child ? 62 : 84 + rng.Next(0, 54);
                if (i == 3 || i == 7) qx += 70;
            }

            // Ön plan: kadraja soldan giren ağaç gövdesi, sağda el arabası ve onu çeken adam.
            Elm(b, rng, (int)(b.W * 0.015f), -20, (int)(b.H * 1.25f), Soot);
            Cart(b, rng, (int)(b.W * 0.80f), (int)(b.H * 0.02f), 250f);
            FigureShadow(b, (int)(b.W * 0.745f), (int)(b.H * 0.02f), 240f, 0.16f);
            Person(b, rng, (int)(b.W * 0.745f), (int)(b.H * 0.02f), 240f, Soot, Kind.Man, Carry.Pull, 1);
        }

        /// <summary>
        /// Beemster: dümdüz kar, ufka kaçan yol, yol boyunca küçülen kavaklar. Güneş solda
        /// ve alçak; yürüyenlerin gölgesi sağa uzar. Ufukta bir yel değirmeni ve çiftlik.
        /// </summary>
        internal static void DrawPolderRoad(Board b, System.Random rng)
        {
            skyTone = Blend(Paper, Mustard, 0.06f);
            lightDir = -1f;
            lightTone = Blend(Mustard, Paper, 0.30f);

            SkyWash(b, rng, Petrol, 0.20f, Mustard, 0.22f, 0.335f);
            HorizonGlow(b, (int)(b.W * 0.16f), (int)(b.H * 0.36f), (int)(b.H * 0.44f), Mustard, 0.30f);
            CloudBank(b, rng, (int)(b.H * 0.62f), (int)(b.W * 0.30f), (int)(b.W * 0.75f), (int)(b.H * 0.09f), 0.26f);
            CloudBank(b, rng, (int)(b.H * 0.80f), (int)(-b.W * 0.05f), (int)(b.W * 0.60f), (int)(b.H * 0.07f), 0.20f);
            CloudBank(b, rng, (int)(b.H * 0.47f), (int)(b.W * 0.55f), (int)(b.W * 0.50f), (int)(b.H * 0.035f), 0.18f);
            SkyStreaks(b, rng, 0.40f, 0.95f, Petrol, 12);

            int horizon = (int)(b.H * 0.335f);
            Vector2 vp = new Vector2(b.W * 0.62f, horizon);

            float[] treeline = new float[b.W];
            float[] tl = ValueNoise(b.W, rng, 120);
            for (int x = 0; x < b.W; x++) treeline[x] = horizon + 3f + tl[x] * 9f;
            Layer(b, rng, treeline, Blend(Petrol, Soot, 0.30f), 0.62f, 0, 0f, 1);
            Windmill(b, rng, (int)(b.W * 0.30f), horizon, (int)(b.H * 0.19f), Blend(Blend(Petrol, Soot, 0.45f), skyTone, 0.40f));
            FarmSilhouette(b, rng, (int)(b.W * 0.86f), horizon, (int)(b.W * 0.11f), (int)(b.H * 0.075f), Blend(Blend(Petrol, Soot, 0.45f), skyTone, 0.45f));

            FillTextured(b, rng, 0, 0, b.W, horizon + 1, Blend(Paper, Petrol, 0.10f), 0.2f);
            Color32 ditchIce = Blend(Paper, Petrol, 0.34f);
            Wedge(b, rng, new Vector2(b.W * 0.58f, 0f), new Vector2(b.W * 0.74f, 0f), vp, ditchIce, 0.15f);
            Wedge(b, rng, new Vector2(-b.W * 0.30f, 0f), new Vector2(-b.W * 0.18f, 0f), vp, ditchIce, 0.15f);
            Wedge(b, rng, new Vector2(b.W * 0.04f, 0f), new Vector2(b.W * 0.56f, 0f), vp, Blend(Paper, Ink, 0.22f), 0.3f);
            PerspectiveLine(b, rng, new Vector2(b.W * 0.17f, 0f), vp, Blend(Ink, Paper, 0.28f), 5, 0.55f);
            PerspectiveLine(b, rng, new Vector2(b.W * 0.42f, 0f), vp, Blend(Ink, Paper, 0.28f), 5, 0.55f);
            for (int i = 0; i < 120; i++)
            {
                float t = (float)rng.NextDouble();
                t *= t;
                int y = (int)Mathf.Lerp(0f, horizon - 4, t);
                int x0 = (int)Mathf.Lerp(Mathf.Lerp(b.W * 0.04f, vp.x, t), Mathf.Lerp(b.W * 0.56f, vp.x, t), (float)rng.NextDouble());
                Stroke(b, x0, y, x0 + rng.Next(6, 40), y + rng.Next(-1, 2), Blend(Ink, Paper, 0.45f), 1, 0.35f * (1f - t));
            }
            // Ufka yaklaştıkça pus.
            for (int y = (int)(horizon * 0.55f); y < horizon; y++)
            {
                float t = (y - horizon * 0.55f) / (horizon * 0.45f);
                for (int x = 0; x < b.W; x++) b.BlendPx(x, y, skyTone, 0.30f * t * t);
            }

            for (int i = 0; i < 10; i++)
            {
                float t = Mathf.Pow(i / 9f, 1.25f);
                int x = (int)Mathf.Lerp(b.W * 0.86f, vp.x + 26f, t);
                int y = (int)Mathf.Lerp(b.H * 0.005f, horizon, t);
                int h = (int)Mathf.Lerp(b.H * 0.84f, b.H * 0.04f, t);
                Color32 tone = Blend(Soot, skyTone, 0.55f * t);
                Poplar(b, rng, x, y, h, tone);
            }
            Fence(b, rng, new Vector2(b.W * 0.01f, b.H * 0.04f), vp, 24, 74f);

            // Uzaktaki yürüyenler: yolun içinde, kareli dağılım.
            for (int i = 0; i < 22; i++)
            {
                float u = (float)rng.NextDouble();
                float t = 0.22f + 0.76f * Mathf.Sqrt(u);
                float lane = 0.15f + 0.70f * (float)rng.NextDouble();
                int x = (int)Mathf.Lerp(Mathf.Lerp(b.W * 0.04f, vp.x, t), Mathf.Lerp(b.W * 0.56f, vp.x, t), lane);
                int y = (int)Mathf.Lerp(b.H * 0.03f, horizon, t);
                float h = Mathf.Lerp(150f, 5f, Mathf.Pow(t, 0.9f));
                Color32 tone = Blend(Soot, skyTone, 0.40f * t);
                if (h > 34f) Person(b, rng, x, y, h, tone, (Kind)(i % 3), i % 2 == 0 ? Carry.Walk : Carry.Bag, i);
                else Mark(b, rng, x, y, h, tone, false);
            }

            // Öndeki grup: el arabası, çeken adam, arkada kadın ve çocuk el ele.
            int footY = (int)(b.H * 0.06f);
            Cart(b, rng, (int)(b.W * 0.44f), footY, 236f);
            FigureShadow(b, (int)(b.W * 0.385f), footY, 262f, 0.2f);
            Person(b, rng, (int)(b.W * 0.385f), footY, 262f, Soot, Kind.Man, Carry.Pull, 3);
            FigureShadow(b, (int)(b.W * 0.605f), footY + 4, 246f, 0.2f);
            Person(b, rng, (int)(b.W * 0.605f), footY + 4, 246f, Soot, Kind.Woman, Carry.HoldHand, 1);
            Person(b, rng, (int)(b.W * 0.648f), footY + 2, 150f, Soot, Kind.Child, Carry.Walk, 2);

            float[] bank = ForegroundSlope(b, rng, 0.06f, 0.012f, 0.0f);
            Layer(b, rng, bank, Blend(Paper, Petrol, 0.20f), 0f, 6, 0.10f, 2);
            Scatter(b, rng, bank, 28, 0.012f, Blend(Soot, Petrol, 0.30f));
        }

        /// <summary>
        /// Afsluitdijk: iki su arasında ufka kaçan tek yol. Solda IJsselmeer'in buzu,
        /// sağda Wadden'in gri suyu; yolda uzaklaştıkça küçülen bir insan zinciri.
        /// Gök sahnenin üçte ikisidir ve rüzgârı gösteren büyük bulut kütleleri taşır.
        /// </summary>
        internal static void DrawAfsluitdijk(Board b, System.Random rng)
        {
            skyTone = Blend(Paper, Petrol, 0.12f);
            lightDir = -1f;
            lightTone = Blend(Paper, Mustard, 0.25f);

            int horizon = (int)(b.H * 0.365f);
            SkyWash(b, rng, Petrol, 0.30f, Paper, 0f, 0.365f);
            HorizonGlow(b, (int)(b.W * 0.10f), horizon + 10, (int)(b.H * 0.36f), Mustard, 0.22f);
            CloudBank(b, rng, (int)(b.H * 0.86f), (int)(-b.W * 0.10f), (int)(b.W * 0.80f), (int)(b.H * 0.11f), 0.34f);
            CloudBank(b, rng, (int)(b.H * 0.70f), (int)(b.W * 0.35f), (int)(b.W * 0.80f), (int)(b.H * 0.09f), 0.30f);
            CloudBank(b, rng, (int)(b.H * 0.56f), (int)(-b.W * 0.05f), (int)(b.W * 0.55f), (int)(b.H * 0.05f), 0.22f);
            CloudBank(b, rng, (int)(b.H * 0.46f), (int)(b.W * 0.45f), (int)(b.W * 0.65f), (int)(b.H * 0.035f), 0.18f);
            SkyStreaks(b, rng, 0.38f, 0.97f, Petrol, 18);

            Vector2 vp = new Vector2(b.W * 0.52f, horizon);
            Color32 iceTone = Blend(Paper, Petrol, 0.24f);
            Color32 seaTone = Blend(Petrol, Soot, 0.28f);
            for (int y = 0; y < horizon; y++)
            {
                float t = y / (float)horizon;
                for (int x = 0; x < b.W; x++)
                {
                    bool left = x < vp.x;
                    Color32 tone = left ? Blend(iceTone, skyTone, 0.55f * t) : Blend(seaTone, skyTone, 0.60f * t);
                    b.Set(x, y, Shift(tone, rng.Next(-4, 5)));
                }
            }
            float[] waterLine = new float[b.W];
            for (int x = 0; x < b.W; x++) waterLine[x] = horizon;
            WaterStreaks(b, rng, waterLine, 320);
            for (int i = 0; i < 60; i++)
            {
                int x = rng.Next((int)vp.x, b.W);
                int y = rng.Next(0, horizon - 20);
                Stroke(b, x, y, x + rng.Next(10, 60), y + 1, Blend(Paper, Petrol, 0.15f), 1, 0.45f * (1f - y / (float)horizon));
            }

            Wedge(b, rng, new Vector2(-b.W * 0.10f, 0f), new Vector2(b.W * 1.10f, 0f), vp, Blend(Ink, Petrol, 0.30f), 0.25f);
            Wedge(b, rng, new Vector2(-b.W * 0.10f, 0f), new Vector2(b.W * 0.30f, 0f), vp, Blend(Ink, Petrol, 0.42f), 0.25f);
            Wedge(b, rng, new Vector2(b.W * 0.30f, 0f), new Vector2(b.W * 0.68f, 0f), vp, Blend(Paper, Ink, 0.36f), 0.3f);
            PerspectiveLine(b, rng, new Vector2(b.W * 0.30f, 0f), vp, Blend(Paper, Petrol, 0.20f), 3, 0.45f);
            PerspectiveLine(b, rng, new Vector2(b.W * 0.68f, 0f), vp, Blend(Ink, Soot, 0.4f), 3, 0.55f);
            for (int i = 0; i < 260; i++)
            {
                float t = (float)rng.NextDouble();
                t *= t;
                int y = (int)Mathf.Lerp(0f, horizon - 6, t);
                bool left = rng.Next(0, 2) == 0;
                float x0 = left ? Mathf.Lerp(-b.W * 0.10f, vp.x, t) : Mathf.Lerp(b.W * 0.68f, vp.x, t);
                float x1 = left ? Mathf.Lerp(b.W * 0.30f, vp.x, t) : Mathf.Lerp(b.W * 1.10f, vp.x, t);
                int x = (int)Mathf.Lerp(x0, x1, (float)rng.NextDouble());
                int s = Mathf.Max(1, (int)Mathf.Lerp(10f, 1f, t));
                Color32 tone = left ? Blend(Ink, Paper, 0.18f) : Blend(Soot, Petrol, 0.15f);
                FillTextured(b, rng, x, y, x + s + rng.Next(0, s), y + s / 2 + 1, tone, 0.2f);
            }
            for (int i = 0; i < 140; i++)
            {
                float t = (float)rng.NextDouble();
                t *= t;
                int y = (int)Mathf.Lerp(2f, horizon - 6, t);
                int x = (int)Mathf.Lerp(Mathf.Lerp(b.W * 0.30f, vp.x, t), Mathf.Lerp(b.W * 0.68f, vp.x, t), (float)rng.NextDouble());
                Stroke(b, x, y, x + rng.Next(8, 60), y + rng.Next(0, 2), Paper, 1, 0.40f * (1f - t));
            }
            // Uzağa doğru pus: set ve sular ufukta göğe karışır.
            for (int y = (int)(horizon * 0.5f); y < horizon; y++)
            {
                float t = (y - horizon * 0.5f) / (horizon * 0.5f);
                for (int x = 0; x < b.W; x++) b.BlendPx(x, y, skyTone, 0.38f * t * t);
            }

            Color32 far = Blend(Blend(Ink, Petrol, 0.35f), skyTone, 0.55f);
            FillTextured(b, rng, (int)vp.x - 34, horizon, (int)vp.x + 38, horizon + 12, far, 0.1f);
            FillTextured(b, rng, (int)vp.x - 6, horizon, (int)vp.x + 2, horizon + 32, far, 0.1f);
            FillTextured(b, rng, (int)vp.x + 16, horizon, (int)vp.x + 22, horizon + 20, far, 0.1f);

            // Yürüyenler: kareli dağılım uzağa yığar; yakında birkaç okunur figür.
            for (int i = 0; i < 40; i++)
            {
                float u = (float)rng.NextDouble();
                float t = 0.14f + 0.84f * Mathf.Sqrt(u);
                float lane = 0.1f + 0.8f * (float)rng.NextDouble();
                int x = (int)Mathf.Lerp(Mathf.Lerp(b.W * 0.34f, vp.x, t), Mathf.Lerp(b.W * 0.64f, vp.x, t), lane);
                int y = (int)Mathf.Lerp(b.H * 0.02f, horizon - 1, t);
                float h = Mathf.Lerp(150f, 4f, Mathf.Pow(t, 0.85f));
                Color32 tone = Blend(Soot, skyTone, 0.45f * t);
                if (h > 34f) Person(b, rng, x, y, h, tone, (Kind)(i % 4), i % 3 == 0 ? Carry.Bag : Carry.Walk, i);
                else Mark(b, rng, x, y, h, tone, false);
            }

            int footY = (int)(b.H * 0.03f);
            Cart(b, rng, (int)(b.W * 0.40f), footY, 250f);
            FigureShadow(b, (int)(b.W * 0.345f), footY, 268f, 0.16f);
            Person(b, rng, (int)(b.W * 0.345f), footY, 268f, Soot, Kind.Man, Carry.Pull, 0);
            Person(b, rng, (int)(b.W * 0.578f), footY + 2, 254f, Soot, Kind.Woman, Carry.Walk, 1);
            Pram(b, rng, (int)(b.W * 0.618f), footY, 150f);
            Person(b, rng, (int)(b.W * 0.672f), footY + 1, 232f, Soot, Kind.Woman, Carry.None, 2);
            KilometrePost(b, rng, (int)(b.W * 0.745f), footY + 8, 58);
        }

        /// <summary>
        /// Frizya çiftliği akşamüstü: kar, büyük saz çatı, aydınlık pencereler, kapıda fener.
        /// Işık sağdan ve alçak; ufuk boyunca hardal, üstte lacivert. Söğütler ve samanlık
        /// orta planı doldurur, sağdan giren çit ve kapı ön planı kurar.
        /// </summary>
        internal static void DrawFrisianFarm(Board b, System.Random rng)
        {
            skyTone = Blend(Paper, Petrol, 0.42f);
            lightDir = 1f;
            lightTone = Mustard;

            int ground = (int)(b.H * 0.30f);
            SkyWash(b, rng, Blend(Petrol, Soot, 0.55f), 0.62f, Mustard, 0.38f, 0.30f);
            HorizonGlow(b, (int)(b.W * 0.80f), ground + 8, (int)(b.H * 0.44f), Mustard, 0.30f);
            CloudBank(b, rng, (int)(b.H * 0.78f), (int)(-b.W * 0.05f), (int)(b.W * 0.70f), (int)(b.H * 0.08f), 0.26f);
            CloudBank(b, rng, (int)(b.H * 0.56f), (int)(b.W * 0.10f), (int)(b.W * 0.55f), (int)(b.H * 0.04f), 0.20f);
            SkyStreaks(b, rng, 0.36f, 0.60f, Blend(Mustard, Paper, 0.4f), 14);

            float[] snow = new float[b.W];
            float[] sn = ValueNoise(b.W, rng, 9);
            for (int x = 0; x < b.W; x++) snow[x] = ground + (sn[x] - 0.5f) * 10f;
            Layer(b, rng, snow, Blend(Paper, Petrol, 0.30f), 0.10f, 10, 0.06f, 1);
            for (int i = 0; i < 40; i++)
            {
                int x = rng.Next(0, b.W);
                Stroke(b, x, ground + 1, x + rng.Next(30, 120), ground + 1, Blend(Petrol, Soot, 0.4f), 1, 0.5f);
            }
            // Karda tarla izleri: sağ alttaki kapıdan eve doğru yaklaşan çizgiler.
            Vector2 doorFoot = new Vector2(b.W * 0.58f, ground - 2);
            for (int i = 0; i < 9; i++)
            {
                float u = i / 8f;
                Vector2 from = new Vector2(Mathf.Lerp(b.W * 0.55f, b.W * 1.05f, u), 0f);
                PerspectiveLine(b, rng, from, doorFoot, Blend(Paper, Petrol, 0.50f), 2, 0.30f);
            }
            for (int i = 0; i < 160; i++)
            {
                int x = rng.Next(0, b.W);
                int y = rng.Next(0, ground - 8);
                Stroke(b, x, y, x + rng.Next(10, 70), y + rng.Next(-1, 2), Blend(Paper, Petrol, 0.42f), 1, 0.25f);
            }

            Haystack(b, rng, (int)(b.W * 0.125f), ground - 3, (int)(b.W * 0.062f), (int)(b.H * 0.135f));

            int wallL = (int)(b.W * 0.24f);
            int wallR = (int)(b.W * 0.64f);
            int eaves = (int)(b.H * 0.40f);
            int ridge = (int)(b.H * 0.74f);
            int ridgeL = (int)(b.W * 0.36f);
            int ridgeR = (int)(b.W * 0.53f);
            Color32 wall = Blend(Ink, Rust, 0.18f);
            FillTextured(b, rng, wallL, ground - 4, wallR, eaves + 2, wall, 0.2f);
            for (int y = ground + 6; y < eaves; y += 9)
                Stroke(b, wallL, y, wallR, y, Blend(wall, Soot, 0.35f), 1, 0.35f);
            for (int y = eaves; y <= ridge; y++)
            {
                float t = (y - eaves) / (float)(ridge - eaves);
                int x0 = (int)Mathf.Lerp(wallL - 18, ridgeL, t);
                int x1 = (int)Mathf.Lerp(wallR + 18, ridgeR, t);
                for (int x = x0; x <= x1; x++)
                {
                    float across = (x - x0) / (float)Mathf.Max(1, x1 - x0);
                    Color32 face = Blend(Blend(Soot, Ink, 0.5f), Blend(Ink, Mustard, 0.30f), Mathf.Clamp01(across * 1.2f - 0.35f));
                    b.Set(x, y, Shift(face, rng.Next(-5, 6)));
                }
            }
            for (int i = 0; i < 260; i++)
            {
                int y = eaves + rng.Next(0, ridge - eaves);
                float t = (y - eaves) / (float)(ridge - eaves);
                int x0 = (int)Mathf.Lerp(wallL - 18, ridgeL, t);
                int x1 = (int)Mathf.Lerp(wallR + 18, ridgeR, t);
                int x = rng.Next(x0, x1);
                Stroke(b, x, y, x + rng.Next(-3, 4), y - rng.Next(10, 40), Blend(Ink, Mustard, 0.16f), 1, 0.22f);
            }
            Stroke(b, ridgeL, ridge + 1, ridgeR, ridge + 1, Blend(Paper, skyTone, 0.25f), 5, 0.85f);
            Stroke(b, wallL - 18, eaves + 1, wallR + 18, eaves + 1, Blend(Paper, skyTone, 0.30f), 3, 0.5f);
            Stroke(b, wallL - 18, eaves, ridgeL, ridge, Blend(Paper, skyTone, 0.30f), 2, 0.35f);
            FillTextured(b, rng, (int)(b.W * 0.485f), ridge - 6, (int)(b.W * 0.505f), ridge + 40, Blend(Ink, Rust, 0.3f), 0.2f);
            int hL = wallR - 6;
            int hR = (int)(b.W * 0.76f);
            int hTop = (int)(b.H * 0.50f);
            FillTextured(b, rng, hL, ground - 4, hR, hTop, wall, 0.2f);
            for (int y = ground + 6; y < hTop; y += 9)
                Stroke(b, hL, y, hR, y, Blend(wall, Soot, 0.35f), 1, 0.35f);
            for (int y = hTop; y <= hTop + (hR - hL) / 2; y++)
            {
                int inset = y - hTop;
                for (int x = hL + inset; x <= hR - inset; x++) b.Set(x, y, Shift(Blend(Soot, Ink, 0.5f), rng.Next(-5, 6)));
            }
            Stroke(b, hL, hTop, (hL + hR) / 2, hTop + (hR - hL) / 2, Blend(Paper, skyTone, 0.30f), 2, 0.5f);
            Color32 warm = Blend(Mustard, Paper, 0.45f);
            LitWindow(b, rng, (int)(b.W * 0.30f), ground + 26, 30, 44, warm);
            LitWindow(b, rng, (int)(b.W * 0.44f), ground + 26, 30, 44, warm);
            LitWindow(b, rng, (int)(b.W * 0.70f), ground + 30, 26, 40, warm);
            int doorX = (int)(b.W * 0.58f);
            FillTextured(b, rng, doorX - 24, ground - 2, doorX + 24, ground + 84, Blend(warm, Mustard, 0.25f), 0.15f);
            for (int y = 0; y < ground; y++)
            {
                float t = y / (float)ground;
                int half = (int)(30 + (1f - t) * 260f);
                for (int x = doorX - half; x <= doorX + half; x++)
                {
                    float f = 1f - Mathf.Abs(x - doorX) / (float)half;
                    b.BlendPx(x, y, warm, 0.30f * f * f * (0.25f + 0.75f * t));
                }
            }
            HorizonGlow(b, doorX, ground + 20, (int)(b.H * 0.14f), Mustard, 0.30f);
            Lantern(b, (int)(b.W * 0.615f), ground + 96);

            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f;
                int x = (int)Mathf.Lerp(b.W * 0.045f, b.W * 0.205f, t);
                int y = (int)Mathf.Lerp(b.H * 0.02f, ground - 6, t);
                int h = (int)Mathf.Lerp(b.H * 0.30f, b.H * 0.10f, t);
                PollardWillow(b, rng, x, y, h, Blend(Soot, skyTone, 0.22f * t));
            }
            Elm(b, rng, (int)(b.W * 0.88f), ground - 2, (int)(b.H * 0.66f), Soot);

            Cart(b, rng, (int)(b.W * 0.43f), ground - 8, 210f);
            FigureShadow(b, (int)(b.W * 0.628f), ground - 6, 226f, 0.14f);
            Person(b, rng, (int)(b.W * 0.628f), ground - 6, 226f, Soot, Kind.Woman, Carry.HoldHand, 1);
            Person(b, rng, (int)(b.W * 0.664f), ground - 6, 142f, Soot, Kind.Child, Carry.None, 2);

            float[] drift = ForegroundSlope(b, rng, 0.12f, 0.02f, 1f);
            Layer(b, rng, drift, Blend(Paper, Petrol, 0.36f), 0f, 4, 0.08f, 2);
            Fence(b, rng, new Vector2(b.W * 1.02f, b.H * 0.02f), new Vector2(b.W * 0.72f, b.H * 0.26f), 9, 110f);
        }

        /// <summary>
        /// Bloemgracht gece: dar cepheler siyah, birkaç pencere sıcak, bir kapı açık ve ışığı
        /// kara vuruyor. Işık kaynağı kapının kendisi; ay yalnız çatı çizgisini gösterir.
        /// </summary>
        internal static void DrawCanalNight(Board b, System.Random rng)
        {
            skyTone = Blend(Soot, Petrol, 0.34f);
            lightDir = -1f;
            lightTone = Mustard;

            SkyWash(b, rng, Soot, 0.78f, Blend(Petrol, Soot, 0.5f), 0.62f, 0.45f);
            HorizonGlow(b, (int)(b.W * 0.78f), (int)(b.H * 0.88f), (int)(b.H * 0.30f), Blend(Paper, Petrol, 0.4f), 0.16f);
            Circle(b, (int)(b.W * 0.78f), (int)(b.H * 0.88f), 22, Blend(Paper, Mustard, 0.18f), 0.92f);
            CloudBank(b, rng, (int)(b.H * 0.86f), (int)(b.W * 0.55f), (int)(b.W * 0.50f), (int)(b.H * 0.03f), 0.30f);
            CloudBank(b, rng, (int)(b.H * 0.72f), (int)(-b.W * 0.05f), (int)(b.W * 0.65f), (int)(b.H * 0.05f), 0.22f);

            // Cepheler: dar kanal evleri, üç dört kat, yüksek pencereler.
            float[] facades = new float[b.W];
            int street = (int)(b.H * 0.09f);
            for (int x = 0; x < b.W; x++) facades[x] = street;
            int cursor = -30;
            int house = 0;
            List<int> edges = new List<int>();
            while (cursor < b.W + 40)
            {
                int width = 118 + rng.Next(0, 70);
                int top = (int)(b.H * (0.60f + 0.26f * (float)rng.NextDouble()));
                GableProfile(facades, cursor, cursor + width, top, (house + 2) % 5, rng);
                edges.Add(cursor);
                cursor += width + 3;
                house++;
            }
            Color32 facadeTone = Blend(Ink, Petrol, 0.22f);
            Layer(b, rng, facades, facadeTone, 0f, 0, 0f, 0);
            SnowCap(b, facades, Blend(Paper, Petrol, 0.40f), 4, 0.75f);
            Windows(b, rng, facades, street + 150, 108, 48, Blend(facadeTone, Soot, 0.35f), 0.05f);
            for (int i = 0; i < edges.Count; i++)
                Stroke(b, edges[i] - 1, street, edges[i] - 1, (int)facades[Mathf.Clamp(edges[i] + 4, 0, b.W - 1)], Blend(facadeTone, Paper, 0.10f), 1, 0.6f);

            // Kaldırım: karlı, mavi; kanal kenarında bağlama babaları ve buz.
            FillTextured(b, rng, 0, 0, b.W, street + 1, Blend(Paper, Petrol, 0.58f), 0.2f);
            FillTextured(b, rng, 0, 0, b.W, (int)(b.H * 0.03f), Blend(Petrol, Soot, 0.60f), 0.15f);
            for (int x = (int)(b.W * 0.08f); x < b.W; x += 260)
                FillTextured(b, rng, x - 7, (int)(b.H * 0.03f), x + 7, (int)(b.H * 0.03f) + 26, Soot, 0.1f);

            // Açık kapı ve ışığı.
            int doorX = (int)(b.W * 0.46f);
            int doorTop = street + 150;
            FillTextured(b, rng, doorX - 40, street - 2, doorX + 40, doorTop, Blend(Mustard, Paper, 0.45f), 0.12f);
            Stroke(b, doorX - 40, doorTop, doorX + 40, doorTop, Blend(Mustard, Paper, 0.7f), 3, 0.8f);
            for (int y = 0; y < street; y++)
            {
                float t = y / (float)street;
                int half = (int)(44 + (1f - t) * 230f);
                for (int x = doorX - half; x <= doorX + half; x++)
                {
                    float f = 1f - Mathf.Abs(x - doorX) / (float)half;
                    b.BlendPx(x, y, Blend(Mustard, Paper, 0.45f), 0.75f * f * f * (0.30f + 0.70f * t));
                }
            }
            HorizonGlow(b, doorX, street + 60, (int)(b.H * 0.26f), Mustard, 0.22f);
            LitWindow(b, rng, (int)(b.W * 0.20f), street + 160, 22, 48, Blend(Mustard, Paper, 0.45f));
            LitWindow(b, rng, (int)(b.W * 0.71f), street + 268, 22, 48, Blend(Mustard, Paper, 0.45f));

            // Kapıdaki anne, arabadan inen çocuk, arabayı tutan adam ve bebek arabalı kız.
            Person(b, rng, doorX + 56, street - 2, 214f, Soot, Kind.Woman, Carry.None, 0);
            Cart(b, rng, (int)(b.W * 0.235f), street - 14, 236f);
            Person(b, rng, (int)(b.W * 0.18f), street - 14, 250f, Soot, Kind.Man, Carry.Pull, 3);
            Person(b, rng, (int)(b.W * 0.395f), street - 10, 140f, Soot, Kind.Child, Carry.None, 2);
            Person(b, rng, (int)(b.W * 0.615f), street - 8, 236f, Soot, Kind.Woman, Carry.None, 1);
            Pram(b, rng, (int)(b.W * 0.655f), street - 12, 140f);

            Stroke(b, (int)(b.W * 0.80f), street, (int)(b.W * 0.80f), street + 250, Soot, 6, 1f);
            FillTextured(b, rng, (int)(b.W * 0.80f) - 12, street + 250, (int)(b.W * 0.80f) + 12, street + 290, Soot, 0.1f);
            Elm(b, rng, (int)(b.W * 0.905f), street - 2, (int)(b.H * 0.96f), Soot);
        }

        // --------------------------------------------------------------- insan ve ağaç

        /// <summary>
        /// İnsan silüeti. Baş ve başlık, kavisli omuz, boyu türe göre değişen palto, kollar
        /// ve taşınan yük, bacaklar ve bot. Işık tarafına kaydırılmış açık bir kopya önce
        /// basılır; üstüne gövde gelince kenarda ince bir ışık kalır.
        /// </summary>
        internal static void Person(Board b, System.Random rng, int x, int footY, float h, Color32 tone, Kind kind, Carry carry, int variant, float gait = 1f)
        {
            if (h < 8f) return;
            int rim = Mathf.Max(1, (int)(h * 0.012f));
            if (h > 60f)
            {
                Color32 rimTone = Blend(tone, Blend(lightTone, Paper, 0.30f), 0.55f);
                PersonShape(b, x + (int)(lightDir * rim), footY, h, rimTone, kind, carry, variant, gait);
            }
            PersonShape(b, x, footY, h, tone, kind, carry, variant, gait);
        }

        /// <param name="gait">Adım açıklığı çarpanı; yürüyüş karelerinde -1..1 arasında değişir, eksi değer öteki bacağı öne alır.</param>
        internal static void PersonShape(Board b, int x, int footY, float h, Color32 tone, Kind kind, Carry carry, int variant, float gait = 1f)
        {
            bool woman = kind == Kind.Woman;
            bool child = kind == Kind.Child;
            float headR = h * (child ? 0.058f : 0.050f);
            float headY = footY + h * 0.93f;
            float shoulderY = footY + h * 0.82f;
            float shoulderHalf = h * (woman ? 0.098f : child ? 0.092f : 0.110f);
            float hemY = footY + h * (woman ? 0.11f : 0.42f);
            float hemHalf = shoulderHalf * (woman ? 1.32f : 1.10f);
            int lean = variant % 3 == 0 ? -1 : 1;

            // Baş ve başlık.
            FillEllipse(b, x, (int)headY, (int)headR, (int)(headR * 1.12f), tone);
            switch (kind)
            {
                case Kind.Man:
                case Kind.Child:
                    for (int dy = 0; dy < Mathf.Max(1, (int)(headR * 0.45f)); dy++)
                        for (int dx = -(int)(headR * 1.05f); dx <= (int)(headR * 1.05f); dx++)
                            b.Set(x + dx, (int)(headY + headR * 0.85f) + dy, tone);
                    for (int dx = 0; dx <= (int)(headR * 1.45f); dx++)
                        for (int dy = 0; dy < Mathf.Max(1, (int)(headR * 0.22f)); dy++)
                            b.Set(x + lean * dx, (int)(headY + headR * 0.80f) + dy, tone);
                    break;
                case Kind.HatMan:
                    for (int dy = 0; dy < Mathf.Max(1, (int)(headR * 0.25f)); dy++)
                        for (int dx = -(int)(headR * 1.8f); dx <= (int)(headR * 1.8f); dx++)
                            b.Set(x + dx, (int)(headY + headR * 0.75f) + dy, tone);
                    for (int dy = 0; dy < (int)(headR * 1.1f); dy++)
                        for (int dx = -(int)(headR * 0.95f); dx <= (int)(headR * 0.95f); dx++)
                            b.Set(x + dx, (int)(headY + headR * 0.75f) + dy, tone);
                    break;
                case Kind.Woman:
                    for (int dy = 0; dy < (int)(headR * 2.4f); dy++)
                    {
                        float t = dy / (headR * 2.4f);
                        int half = (int)(headR * (0.95f + 0.55f * t * t));
                        int y = (int)(headY + headR * 1.2f) - dy;
                        for (int dx = -half; dx <= half; dx++) b.Set(x + dx, y, tone);
                    }
                    break;
            }
            FillRectI(b, x - (int)(h * 0.02f), (int)(shoulderY - h * 0.01f), x + (int)(h * 0.02f) + 1, (int)(headY - headR * 0.5f), tone);
            FillEllipse(b, x, (int)(shoulderY + h * 0.03f), (int)(h * 0.055f), (int)(h * 0.028f), tone);

            FillEllipse(b, x, (int)shoulderY, (int)shoulderHalf, (int)(h * 0.035f), tone);
            for (int y = (int)hemY; y <= (int)shoulderY; y++)
            {
                float t = (shoulderY - y) / Mathf.Max(1f, shoulderY - hemY);
                float half = Mathf.Lerp(hemHalf, shoulderHalf, Mathf.Pow(t, 0.8f));
                int skew = (int)(lean * h * 0.006f * (1f - t));
                for (int dx = -(int)half; dx <= (int)half; dx++) b.Set(x + dx + skew, y, tone);
            }

            float legW = h * 0.024f;
            float legGap = h * 0.022f;
            float stride = (carry == Carry.Walk || carry == Carry.Pull ? h * 0.05f : h * 0.006f) * gait;
            for (int dy = 0; dy < (int)(hemY - footY); dy++)
            {
                float t = dy / Mathf.Max(1f, hemY - footY);
                int lx = x - (int)(legGap + legW) - (int)(stride * (woman ? 0.6f : 1f - t));
                int rx = x + (int)(legGap + legW) + (int)(stride * (woman ? 0f : (1f - t) * 0.4f));
                for (int k = -(int)legW; k <= (int)legW; k++)
                {
                    b.Set(lx + k, footY + dy, tone);
                    b.Set(rx + k, footY + dy, tone);
                }
            }
            int bootH = Mathf.Max(1, (int)(h * 0.03f));
            int bootL = (int)(h * 0.05f);
            FillRectI(b, x - (int)(legGap + legW * 2) - (int)stride - bootL / 2, footY, x - (int)legGap - (int)stride + bootL / 2, footY + bootH, tone);
            FillRectI(b, x + (int)legGap - bootL / 2 + (int)(stride * 0.4f), footY, x + (int)(legGap + legW * 2) + bootL / 2 + (int)(stride * 0.4f), footY + bootH, tone);

            int armW = Mathf.Max(1, (int)(h * 0.020f));
            int armTopY = (int)(shoulderY - h * 0.02f);
            int handY = (int)(footY + h * 0.46f);
            int armOut = (int)(shoulderHalf * 0.85f);
            switch (carry)
            {
                case Carry.Pot:
                {
                    int potY = (int)(footY + h * 0.52f);
                    Limb(b, x - armOut, armTopY, x - (int)(h * 0.05f), potY + (int)(h * 0.04f), armW, tone);
                    Limb(b, x + armOut, armTopY, x + (int)(h * 0.05f), potY + (int)(h * 0.04f), armW, tone);
                    FillRectI(b, x - (int)(h * 0.075f), potY - (int)(h * 0.07f), x + (int)(h * 0.075f), potY + (int)(h * 0.02f), tone);
                    Stroke(b, x - (int)(h * 0.09f), potY - (int)(h * 0.01f), x + (int)(h * 0.09f), potY - (int)(h * 0.01f), tone, Mathf.Max(1, (int)(h * 0.012f)), 1f);
                    break;
                }
                case Carry.Bag:
                    Limb(b, x - armOut, armTopY, x - (int)(shoulderHalf * 1.05f), handY, armW, tone);
                    Limb(b, x + armOut, armTopY, x + (int)(shoulderHalf * 1.05f), handY, armW, tone);
                    FillRectI(b, x + (int)(shoulderHalf * 0.75f), handY - (int)(h * 0.11f), x + (int)(shoulderHalf * 1.45f), handY + (int)(h * 0.01f), tone);
                    break;
                case Carry.Pull:
                    Limb(b, x - armOut, armTopY, x - (int)(h * 0.19f), (int)(footY + h * 0.56f), armW, tone);
                    Limb(b, x + armOut, armTopY, x + (int)(shoulderHalf * 0.9f), handY, armW, tone);
                    break;
                case Carry.HoldHand:
                    Limb(b, x - armOut, armTopY, x - (int)(shoulderHalf * 1.0f), handY, armW, tone);
                    Limb(b, x + armOut, armTopY, x + (int)(shoulderHalf * 1.9f), (int)(footY + h * 0.40f), armW, tone);
                    break;
                default:
                    Limb(b, x - armOut, armTopY, x - (int)(shoulderHalf * 1.02f), handY, armW, tone);
                    Limb(b, x + armOut, armTopY, x + (int)(shoulderHalf * 1.02f), handY, armW, tone);
                    break;
            }
        }

        /// <summary>
        /// Kışlık yapraksız ağaç: özyineli dallanma. Gövde konikleşir; her dal iki üç kola
        /// ayrılır, kalınlık ve uzunluk derinlikle düşer, uç dallar tek piksel kalır.
        /// </summary>
        internal static void Elm(Board b, System.Random rng, int x, int baseY, int height, Color32 tone)
        {
            float trunkW = Mathf.Max(3f, height * 0.045f);
            float trunkH = height * 0.36f;
            for (int dy = 0; dy < (int)trunkH; dy++)
            {
                float t = dy / trunkH;
                int w = Mathf.Max(1, (int)(trunkW * (1f - 0.45f * t)));
                int sway = (int)(Mathf.Sin(dy * 0.02f) * trunkW * 0.25f);
                for (int dx = -w; dx <= w; dx++) b.Set(x + dx + sway, baseY + dy, Shift(tone, rng.Next(-4, 5)));
            }
            for (int dy = 0; dy < (int)(trunkW * 1.2f); dy++)
            {
                int w = (int)(trunkW * (1.6f - dy / (trunkW * 1.2f) * 0.6f));
                for (int dx = -w; dx <= w; dx++) b.Set(x + dx, baseY + dy, tone);
            }
            int topX = x + (int)(Mathf.Sin(trunkH * 0.02f) * trunkW * 0.25f);
            Branch(b, rng, topX, baseY + (int)trunkH, Mathf.PI * 0.5f, height * 0.34f, trunkW * 0.55f, 0, tone);
        }

        internal static void Branch(Board b, System.Random rng, int x, int y, float angle, float length, float width, int depth, Color32 tone)
        {
            if (length < 4f || depth > 7) return;
            int ex = x + (int)(Mathf.Cos(angle) * length);
            int ey = y + (int)(Mathf.Sin(angle) * length);
            int w = Mathf.Max(1, (int)width);
            int mx = x + (int)(Mathf.Cos(angle + 0.12f) * length * 0.5f);
            int my = y + (int)(Mathf.Sin(angle + 0.12f) * length * 0.5f);
            Stroke(b, x, y, mx, my, tone, w, 1f);
            Stroke(b, mx, my, ex, ey, tone, Mathf.Max(1, (int)(width * 0.8f)), 1f);
            int forks = depth < 2 ? 3 : 2 + (rng.Next(0, 3) == 0 ? 1 : 0);
            for (int i = 0; i < forks; i++)
            {
                float spread = 0.32f + 0.30f * (float)rng.NextDouble();
                float a = angle + (i - (forks - 1) * 0.5f) * spread + ((float)rng.NextDouble() - 0.5f) * 0.25f;
                if (Mathf.Sin(a) < 0.05f) a = angle + (a - angle) * 0.4f;
                float l = length * (0.62f + 0.16f * (float)rng.NextDouble());
                Branch(b, rng, ex, ey, a, l, width * 0.62f, depth + 1, tone);
            }
        }

        /// <summary>
        /// Bulut kütlesi: üst üste binmiş elipsler; altı gölgeli, üstü ışığa dönük. Eski
        /// bulut çizimi geniş göklerde kayboluyordu; bu kütle okunur.
        /// </summary>
        internal static void CloudBank(Board b, System.Random rng, int y, int x0, int width, int height, float alpha)
        {
            Color32 shade = Blend(skyTone, Petrol, 0.30f);
            Color32 lit = Blend(skyTone, Blend(lightTone, Paper, 0.5f), 0.55f);
            int count = 6 + width / 120;
            for (int i = 0; i < count; i++)
            {
                float u = (float)rng.NextDouble();
                int cx = x0 + (int)(u * width);
                int rx = (int)(width * (0.10f + 0.12f * (float)rng.NextDouble()));
                int ry = Mathf.Max(2, (int)(height * (0.55f + 0.7f * (float)rng.NextDouble())));
                int cy = y + rng.Next(-height / 3, height / 3 + 1);
                for (int dy = -ry; dy <= ry; dy++)
                {
                    float fy = dy / (float)ry;
                    int half = (int)(rx * Mathf.Sqrt(Mathf.Max(0f, 1f - fy * fy)));
                    for (int dx = -half; dx <= half; dx++)
                    {
                        float edge = 1f - (dx * dx) / (float)(half * half + 1);
                        float a = alpha * Mathf.Clamp01(edge * 1.6f);
                        Color32 tone = fy > 0.35f ? lit : shade;
                        if (fy > 0.35f) a *= 0.7f;
                        b.BlendPx(cx + dx, cy + dy, tone, a);
                    }
                }
            }
        }

        /// <summary>Samanlık: kubbe biçimli yığın, tepesinde direk, yüzeyinde saman izleri.</summary>
        internal static void Haystack(Board b, System.Random rng, int cx, int baseY, int halfW, int height)
        {
            Color32 dark = Blend(Ink, Mustard, 0.14f);
            Color32 lit = Blend(Ink, Mustard, 0.34f);
            for (int dy = 0; dy < height; dy++)
            {
                float t = dy / (float)height;
                int half = (int)(halfW * Mathf.Pow(1f - t * t, 0.55f));
                for (int dx = -half; dx <= half; dx++)
                {
                    float across = (dx + half) / (float)Mathf.Max(1, 2 * half);
                    float litAmount = lightDir > 0f ? Mathf.Clamp01(across * 1.4f - 0.5f) : Mathf.Clamp01(0.9f - across * 1.4f);
                    b.Set(cx + dx, baseY + dy, Shift(Blend(dark, lit, litAmount), rng.Next(-5, 6)));
                }
            }
            for (int i = 0; i < 140; i++)
            {
                int dy = rng.Next(0, height);
                float t = dy / (float)height;
                int half = (int)(halfW * Mathf.Pow(1f - t * t, 0.55f));
                int x = cx + rng.Next(-half, half + 1);
                Stroke(b, x, baseY + dy, x + rng.Next(-4, 5), baseY + dy - rng.Next(6, 22), Blend(Ink, Mustard, 0.12f), 1, 0.35f);
            }
            Stroke(b, cx, baseY + height - 4, cx, baseY + height + (int)(height * 0.18f), Soot, 3, 1f);
        }

        /// <summary>Çit: kazıklar ufka doğru küçülür ve sıklaşır; en yakın kazık boyu verilir.</summary>
        internal static void Fence(Board b, System.Random rng, Vector2 from, Vector2 to, int posts, float nearHeight)
        {
            int prevX = 0, prevTop = 0, prevMid = 0;
            for (int i = 0; i < posts; i++)
            {
                float t = Mathf.Pow(i / (float)(posts - 1), 0.62f);
                int x = (int)Mathf.Lerp(from.x, to.x, t);
                int y = (int)Mathf.Lerp(from.y, to.y, t);
                int h = (int)Mathf.Lerp(nearHeight, 6f, t);
                int w = Mathf.Max(1, (int)Mathf.Lerp(nearHeight * 0.09f, 1f, t));
                for (int dy = 0; dy < h; dy++)
                    for (int dx = -w; dx <= w; dx++)
                    {
                        bool litSide = lightDir > 0 ? dx >= w - 1 : dx <= -w + 1;
                        Color32 tone = litSide ? Blend(Ink, Blend(lightTone, Paper, 0.35f), 0.40f) : Ink;
                        b.Set(x + dx, y + dy, Shift(tone, rng.Next(-5, 6)));
                    }
                if (i > 0)
                {
                    Stroke(b, prevX, prevTop, x, y + (int)(h * 0.80f), Blend(Ink, Petrol, 0.22f), Mathf.Max(1, w / 2), 0.8f);
                    Stroke(b, prevX, prevMid, x, y + (int)(h * 0.45f), Blend(Ink, Petrol, 0.22f), Mathf.Max(1, w / 2), 0.65f);
                }
                prevX = x;
                prevTop = y + (int)(h * 0.80f);
                prevMid = y + (int)(h * 0.45f);
            }
        }

        // --------------------------------------------------------------- yardımcılar

        /// <summary>
        /// Amsterdam alınlığı: basamaklı, boyunlu, çan biçimli, düz veya üçgen. Dizi bir
        /// silüet üst çizgisidir; ev eve bindirilerek tek bir bant elde edilir.
        /// </summary>
        internal static void GableProfile(float[] top, int x0, int x1, int roof, int kind, System.Random rng)
        {
            int width = Mathf.Max(1, x1 - x0);
            int shoulder = roof - 26 - rng.Next(0, 22);
            for (int x = Mathf.Max(0, x0); x < Mathf.Min(top.Length, x1); x++)
            {
                float t = (x - x0) / (float)width;
                float u = Mathf.Abs(t - 0.5f) * 2f;
                float y;
                switch (kind)
                {
                    case 0: y = Mathf.Lerp(roof, shoulder, Mathf.Floor(u * 4f) / 4f); break;
                    case 1: y = u < 0.30f ? roof : shoulder; break;
                    case 2: y = Mathf.Lerp(roof, shoulder, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - 0.15f) / 0.75f))); break;
                    case 3: y = shoulder + 8; break;
                    default: y = Mathf.Lerp(roof, shoulder, u); break;
                }
                top[x] = y;
            }
        }

        internal static void SnowCap(Board b, float[] top, Color32 tone, int thickness, float alpha)
        {
            for (int x = 1; x < b.W - 1; x++)
            {
                float slope = Mathf.Abs(top[x + 1] - top[x - 1]);
                float f = Mathf.Clamp01(1f - slope / 6f);
                if (f <= 0f) continue;
                int y = (int)top[x];
                for (int k = 0; k < thickness; k++) b.BlendPx(x, y - k, tone, alpha * f * (1f - k / (float)thickness));
            }
        }

        internal static void Windows(Board b, System.Random rng, float[] top, int firstRow, int rowStep, int colStep, Color32 dark, float litChance)
        {
            Color32 warm = Blend(Mustard, Paper, 0.45f);
            for (int x = 18; x < b.W - 18; x += colStep)
            {
                for (int y = firstRow; y < b.H; y += rowStep)
                {
                    int w = colStep / 3;
                    int h = rowStep / 2;
                    if (y + h + 10 > top[x] || y + h + 10 > top[Mathf.Min(b.W - 1, x + w)]) break;
                    bool lit = rng.NextDouble() < litChance;
                    Color32 tone = lit ? warm : dark;
                    for (int dy = 0; dy < h; dy++)
                        for (int dx = 0; dx < w; dx++)
                            b.Set(x + dx, y + dy, Shift(tone, rng.Next(-4, 5)));
                    if (lit) HorizonGlow(b, x + w / 2, y + h / 2, h, warm, 0.10f);
                    else Stroke(b, x + w / 2, y, x + w / 2, y + h, Blend(dark, Soot, 0.5f), 1, 0.6f);
                }
            }
        }

        internal static void LitWindow(Board b, System.Random rng, int x, int y, int w, int h, Color32 warm)
        {
            FillTextured(b, rng, x - w / 2, y, x + w / 2, y + h, warm, 0.12f);
            Stroke(b, x, y, x, y + h, Blend(warm, Ink, 0.5f), 2, 0.7f);
            Stroke(b, x - w / 2, y + h / 2, x + w / 2, y + h / 2, Blend(warm, Ink, 0.5f), 2, 0.7f);
            HorizonGlow(b, x, y + h / 2, h, warm, 0.16f);
        }

        internal static void Wedge(Board b, System.Random rng, Vector2 baseA, Vector2 baseB, Vector2 apex, Color32 tone, float variation)
        {
            int yTop = (int)apex.y;
            int amount = Mathf.Max(1, (int)(variation * 40f));
            for (int y = 0; y < yTop; y++)
            {
                float t = (y - baseA.y) / Mathf.Max(1f, apex.y - baseA.y);
                int x0 = (int)Mathf.Lerp(baseA.x, apex.x, t);
                int x1 = (int)Mathf.Lerp(baseB.x, apex.x, t);
                if (x1 < x0) { int s = x0; x0 = x1; x1 = s; }
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(b.W - 1, x1); x++) b.Set(x, y, Shift(tone, rng.Next(-amount, amount + 1)));
            }
        }

        internal static void PerspectiveLine(Board b, System.Random rng, Vector2 from, Vector2 vp, Color32 tone, int thickness, float alpha)
        {
            int steps = (int)Mathf.Abs(vp.y - from.y);
            for (int i = 0; i < steps; i += 2)
            {
                float t = i / (float)steps;
                int x = (int)Mathf.Lerp(from.x, vp.x, t);
                int y = (int)Mathf.Lerp(from.y, vp.y, t);
                int w = Mathf.Max(1, (int)(thickness * (1f - t)));
                for (int dx = -w; dx <= w; dx++) b.BlendPx(x + dx, y, tone, alpha * (1f - t * 0.6f));
            }
        }

        /// <summary>Kavak: uzun, dar bir taç ve ince gövde; polderin dikey vurgusu.</summary>
        internal static void Poplar(Board b, System.Random rng, int x, int baseY, int height, Color32 tone)
        {
            if (height < 6) { Stroke(b, x, baseY, x, baseY + height, tone, 1, 1f); return; }
            int trunk = Mathf.Max(1, height / 40);
            Stroke(b, x, baseY, x, baseY + (int)(height * 0.25f), tone, trunk, 1f);
            int crownBase = baseY + (int)(height * 0.18f);
            int crownH = height - (int)(height * 0.18f);
            int rx = Mathf.Max(2, (int)(height * 0.062f));
            Color32 inner = Blend(tone, skyTone, 0.08f);
            for (int dy = 0; dy < crownH; dy++)
            {
                float t = dy / (float)crownH;
                float w = rx * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.8f));
                int half = Mathf.Max(0, (int)w);
                for (int dx = -half; dx <= half; dx++)
                {
                    float edge = Mathf.Abs(dx) / Mathf.Max(1f, half);
                    if (edge > 0.82f && rng.Next(0, 3) == 0) continue;
                    b.Set(x + dx, crownBase + dy, Shift(edge > 0.6f ? tone : inner, rng.Next(-6, 7)));
                }
            }
            if (height > 160)
            {
                // Büyük kavakta gövde tacın içinde okunur.
                Stroke(b, x, crownBase, x, crownBase + (int)(crownH * 0.85f), Blend(tone, skyTone, 0.18f), Mathf.Max(1, trunk / 2), 0.5f);
            }
        }

        internal static void PollardWillow(Board b, System.Random rng, int x, int baseY, int height, Color32 tone)
        {
            int trunkH = (int)(height * 0.45f);
            int trunkW = Mathf.Max(2, height / 9);
            for (int dy = 0; dy < trunkH; dy++)
            {
                int w = trunkW - (int)(dy / (float)trunkH * trunkW * 0.25f);
                for (int dx = -w; dx <= w; dx++) b.Set(x + dx, baseY + dy, Shift(tone, rng.Next(-5, 6)));
            }
            int knot = baseY + trunkH;
            FillEllipse(b, x, knot, trunkW + 3, trunkW / 2 + 2, tone);
            int shoots = 10 + height / 12;
            for (int i = 0; i < shoots; i++)
            {
                float a = Mathf.Lerp(-1.25f, 1.25f, (float)rng.NextDouble());
                int len = (int)(height * (0.40f + 0.25f * (float)rng.NextDouble()));
                Stroke(b, x + rng.Next(-trunkW, trunkW + 1), knot, x + (int)(Mathf.Sin(a) * len), knot + (int)(Mathf.Cos(a) * len), tone, Mathf.Max(1, height / 90), 0.85f);
            }
        }

        internal static void Windmill(Board b, System.Random rng, int x, int baseY, int height, Color32 tone)
        {
            int bodyW = Mathf.Max(4, height / 5);
            for (int dy = 0; dy < (int)(height * 0.62f); dy++)
            {
                int w = bodyW - (int)(dy / (height * 0.62f) * bodyW * 0.35f);
                for (int dx = -w; dx <= w; dx++) b.Set(x + dx, baseY + dy, tone);
            }
            int capY = baseY + (int)(height * 0.62f);
            FillEllipse(b, x, capY, bodyW - 1, Mathf.Max(2, height / 14), tone);
            int hubY = capY + Mathf.Max(2, height / 18);
            int sail = (int)(height * 0.48f);
            for (int i = 0; i < 4; i++)
            {
                float a = 0.55f + i * Mathf.PI * 0.5f;
                int ex = x + (int)(Mathf.Cos(a) * sail);
                int ey = hubY + (int)(Mathf.Sin(a) * sail);
                Stroke(b, x, hubY, ex, ey, tone, 2, 1f);
                int px = (int)(Mathf.Cos(a + Mathf.PI * 0.5f) * sail * 0.10f);
                int py = (int)(Mathf.Sin(a + Mathf.PI * 0.5f) * sail * 0.10f);
                int sx = x + (int)(Mathf.Cos(a) * sail * 0.25f);
                int sy = hubY + (int)(Mathf.Sin(a) * sail * 0.25f);
                Stroke(b, sx + px, sy + py, ex + px, ey + py, tone, 1, 0.8f);
                Stroke(b, sx + px, sy + py, sx, sy, tone, 1, 0.8f);
                Stroke(b, ex + px, ey + py, ex, ey, tone, 1, 0.8f);
            }
        }

        internal static void FarmSilhouette(Board b, System.Random rng, int cx, int baseY, int width, int height, Color32 tone)
        {
            int x0 = cx - width / 2;
            int x1 = cx + width / 2;
            int eaves = baseY + height / 4;
            for (int y = baseY; y <= baseY + height; y++)
            {
                float t = Mathf.Clamp01((y - eaves) / (float)Mathf.Max(1, baseY + height - eaves));
                int inset = (int)(t * width * 0.42f);
                for (int x = x0 + inset; x <= x1 - inset; x++) b.Set(x, y, tone);
            }
            for (int dy = 0; dy < height / 3; dy++) for (int dx = 0; dx < 4; dx++) b.Set(cx + width / 5 + dx, baseY + height + dy, tone);
        }

        internal static void Pram(Board b, System.Random rng, int x, int y, float scale)
        {
            int w = (int)(scale * 0.62f);
            int bodyY = y + (int)(scale * 0.30f);
            int h = (int)(scale * 0.26f);
            for (int dx = 0; dx < w; dx++)
            {
                float t = dx / (float)w;
                int rise = (int)(Mathf.Sin(t * Mathf.PI) * h * 0.35f);
                for (int dy = 0; dy < h + rise; dy++) b.Set(x + dx, bodyY + dy, Shift(Ink, rng.Next(-5, 6)));
            }
            Wheel(b, x + (int)(w * 0.22f), y + (int)(scale * 0.17f), (int)(scale * 0.17f));
            Wheel(b, x + (int)(w * 0.78f), y + (int)(scale * 0.17f), (int)(scale * 0.17f));
            Stroke(b, x + w, bodyY + h, x + w + (int)(scale * 0.22f), bodyY + h + (int)(scale * 0.30f), Ink, Mathf.Max(2, (int)(scale * 0.03f)), 1f);
        }

        internal static void KilometrePost(Board b, System.Random rng, int x, int y, int height)
        {
            FillTextured(b, rng, x - 5, y, x + 5, y + height, Blend(Paper, Ink, 0.15f), 0.15f);
            FillTextured(b, rng, x - 6, y + height - 10, x + 6, y + height, Ink, 0.15f);
            Stroke(b, x - 5, y, x - 5, y + height, Blend(Ink, Paper, 0.3f), 1, 0.7f);
        }
    }
}
