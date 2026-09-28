using System.Collections.Generic;
using UnityEngine;
using static OrdinaryFronts.Editor.LinocutSceneFactory;
using Kind = OrdinaryFronts.Editor.HungerWinterSceneFactory.Kind;
using Carry = OrdinaryFronts.Editor.HungerWinterSceneFactory.Carry;

namespace OrdinaryFronts.Editor
{
    /// <summary>
    /// Karelya 1940 sahneleri (1.5): Ladoga kıyısında alçak, ormanlı tepeler; donmuş koy;
    /// aşı boyası kütük evler, huşlar, ahşap kilise ve tsasouna; atlı kızaklar. Dinar
    /// dağlarının yerine burada yumuşak sırtlar ve geniş buz vardır; ölçeği tepeler değil,
    /// ufka uzanan düzlük ve üstündeki küçük kızak sırası verir.
    /// </summary>
    internal static partial class PaintedSceneFactory
    {
        private static readonly Color32 FaluRed = new Color32(0x86, 0x3A, 0x2C, 0xFF);
        private static readonly Color32 Ochre = new Color32(0xB4, 0x93, 0x55, 0xFF);
        private static readonly Color32 LogGrey = new Color32(0x6B, 0x62, 0x57, 0xFF);
        private static readonly Color32 BirchBark = new Color32(0xE2, 0xDD, 0xD0, 0xFF);

        private static void KareliaScene(string key)
        {
            switch (key)
            {
                case "village_exchange": VillageExchange(); break;
                case "karelian_farm": KarelianFarm(); break;
                case "orthodox_chapel": OrthodoxChapel(); break;
                case "churchyard": Churchyard(); break;
                case "evacuation_road": EvacuationRoad(); break;
                case "school_night": SchoolNight(); break;
                case "frozen_bay": FrozenBay(); break;
                default: BorderStation(); break;
            }
        }

        // ================================================================== sahneler

        /// <summary>Santral odası, gece: pencerede karlı köy, duvarda pano, panonun başında Aino.</summary>
        private static void VillageExchange()
        {
            lightDir = 1f;
            lightTone = Lamp;
            Color32 wall = Blend(Blend(Petrol, Paper, 0.30f), Night, 0.45f);
            for (int Y = 0; Y < b.H; Y++)
                for (int X = 0; X < b.W; X++)
                {
                    float x = X / (float)S, y = Y / (float)S;
                    float plank = Mathf.Repeat(x, 58f) < 1.6f ? 0.45f : 0f;
                    float n = Noise(x * 0.01f, y * 0.004f, seed + 1);
                    b.Pixels[Y * b.W + X] = Blend(Blend(wall, Night, 0.2f * n), Soot, plank);
                }
            // Zemin tahtaları.
            Rect(0f, 0f, 1672f, 150f, Blend(Blend(Rust, Night, 0.55f), Soot, 0.2f));
            for (int i = 0; i < 9; i++) Line(0f, 18f + i * 16f, 1672f, 18f + i * 16f, 1.2f, Soot, 0.35f);
            Rect(0f, 148f, 1672f, 158f, Blend(Rust, Night, 0.4f));

            // Pencere: karlı gece, köyün evleri, iki ışık.
            float wx0 = 110f, wx1 = 520f, wy0 = 380f, wy1 = 800f;
            for (int Y = P(wy0); Y < P(wy1); Y++)
                for (int X = P(wx0); X < P(wx1); X++)
                {
                    float t = (Y / (float)S - wy0) / (wy1 - wy0);
                    b.Pixels[Y * b.W + X] = Blend(Blend(Night, Petrol, 0.35f), Night, t);
                }
            Rect(wx0, wy0, wx1, wy0 + 90f, Blend(Moon, SnowShade, 0.35f));
            float[] roofs = { 150f, 260f, 380f, 470f };
            for (int i = 0; i < roofs.Length; i++)
            {
                float hx = roofs[i], hy = wy0 + 80f;
                Rect(hx - 30f, hy, hx + 30f, hy + 34f, Blend(Night, Soot, 0.4f));
                Poly(new[] { new Vector2(hx - 38f, hy + 32f), new Vector2(hx + 38f, hy + 32f), new Vector2(hx, hy + 62f) }, Blend(Moon, Night, 0.4f));
                if (i % 2 == 0) Rect(hx - 8f, hy + 10f, hx + 6f, hy + 24f, Lamp, 0.9f);
            }
            for (int i = 0; i < 60; i++) Ellipse(wx0 + (float)rng.NextDouble() * (wx1 - wx0), wy0 + 90f + (float)rng.NextDouble() * (wy1 - wy0 - 90f), 1.4f, 1.4f, Moon, 0.6f);
            Color32 frame = Blend(Paper, Night, 0.25f);
            Rect(wx0 - 14f, wy0 - 14f, wx1 + 14f, wy0, frame);
            Rect(wx0 - 14f, wy1, wx1 + 14f, wy1 + 14f, frame);
            Rect(wx0 - 14f, wy0 - 14f, wx0, wy1 + 14f, frame);
            Rect(wx1, wy0 - 14f, wx1 + 14f, wy1 + 14f, frame);
            Rect((wx0 + wx1) * 0.5f - 6f, wy0, (wx0 + wx1) * 0.5f + 6f, wy1, frame);
            Rect(wx0, 590f, wx1, 600f, frame);
            Rect(wx0 - 30f, wy0 - 30f, wx1 + 30f, wy0 - 14f, Blend(Paper, Night, 0.35f));
            for (int i = 0; i < 40; i++)
            {
                float fx = wx0 + (float)rng.NextDouble() * 60f, fy = wy0 + (float)rng.NextDouble() * 60f;
                Ellipse(fx, fy, 6f + (float)rng.NextDouble() * 10f, 4f, Blend(Moon, Paper, 0.3f), 0.25f);
            }

            // Duvar saati ve takvim.
            Ellipse(640f, 770f, 46f, 46f, Blend(Rust, Night, 0.35f));
            Ellipse(640f, 770f, 38f, 38f, Blend(Paper, Night, 0.1f));
            Line(640f, 770f, 640f, 800f, 3f, Ink);
            Line(640f, 770f, 654f, 766f, 4f, Ink);
            Rect(600f, 560f, 680f, 660f, Blend(Paper, Night, 0.15f));
            Rect(600f, 640f, 680f, 660f, Blend(Rust, Paper, 0.3f));
            Line(625f, 595f, 640f, 625f, 4f, Ink, 0.8f);
            Line(648f, 595f, 660f, 625f, 4f, Ink, 0.8f);

            // Santral panosu.
            float cx0 = 760f, cx1 = 1500f, cy0 = 230f, cy1 = 880f;
            Rect(cx0, cy0, cx1, cy1, Blend(FaluRed, Night, 0.45f));
            Rect(cx0 + 30f, cy0 + 40f, cx1 - 30f, cy1 - 40f, Blend(Ink, Night, 0.35f));
            Line(cx0, cy1, cx1, cy1, 10f, Blend(FaluRed, Paper, 0.15f));
            Color32 brass = Blend(Mustard, Paper, 0.2f);
            int cols = 10, rows = 5;
            List<Vector2> jacks = new List<Vector2>();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    float x = cx0 + 90f + c * 64f, y = cy1 - 110f - r * 105f;
                    bool lit = (r == 1 && c == 2) || (r == 2 && c == 7);
                    Rect(x - 18f, y + 26f, x + 18f, y + 40f, lit ? Lamp : Blend(Ink, brass, 0.35f));
                    if (lit) Glow(x, y + 33f, 60f, Lamp, 0.5f);
                    Ellipse(x, y, 11f, 11f, brass);
                    Ellipse(x, y, 6f, 6f, Soot);
                    jacks.Add(new Vector2(x, y));
                }
            // Tezgâh, fişler ve takılı kablolar.
            Rect(cx0 - 20f, cy0 - 30f, cx1 + 20f, cy0 + 12f, Blend(FaluRed, Night, 0.3f));
            Line(cx0 - 20f, cy0 + 12f, cx1 + 20f, cy0 + 12f, 3f, Blend(Paper, FaluRed, 0.5f), 0.7f);
            for (int c = 0; c < cols; c++)
            {
                float x = cx0 + 90f + c * 64f;
                Rect(x - 5f, cy0 + 12f, x + 5f, cy0 + 40f, brass);
            }
            int[] plugged = { 3, 12, 27, 41 };
            for (int i = 0; i < plugged.Length; i++)
            {
                Vector2 j = jacks[plugged[i]];
                Vector2 seat = new Vector2(cx0 + 90f + (i * 3 + 1) * 64f, cy0 + 40f);
                Vector2 mid = (j + seat) * 0.5f + new Vector2(0f, -60f);
                Line(seat.x, seat.y, mid.x, mid.y, 4f, Blend(Ink, Rust, 0.3f));
                Line(mid.x, mid.y, j.x, j.y, 4f, Blend(Ink, Rust, 0.3f));
            }
            // Lamba.
            Line(1130f, 941f, 1130f, 900f, 2.5f, Soot);
            Glow(1130f, 880f, 520f, Lamp, 0.45f);
            Poly(new[] { new Vector2(1090f, 890f), new Vector2(1170f, 890f), new Vector2(1150f, 910f), new Vector2(1110f, 910f) }, Blend(Rust, Night, 0.3f));
            Ellipse(1130f, 885f, 12f, 8f, Blend(Lamp, Paper, 0.4f));

            // Aino, taburede, sırtı yarı dönük; kulaklık, bir elinde fiş.
            Operator(1090f, 40f, 560f, Blend(Night, Soot, 0.8f));
            Motes(760f, 400f, 1500f, 900f, 60, Blend(Lamp, Paper, 0.3f));
        }

        /// <summary>Kettunen çiftliği: aşı boyası kütük ev, ambar, huşlar, yüklü kızak; ufukta Ladoga'nın buzu.</summary>
        private static void KarelianFarm()
        {
            lightDir = 1f;
            lightTone = Blend(Mustard, Paper, 0.35f);
            Color32 haze = Blend(Paper, Petrol, 0.12f);
            Sky(Blend(Paper, Petrol, 0.38f), Blend(Paper, Mustard, 0.25f), 470f, Blend(Mustard, Rust, 0.3f), 1450f, 520f, 520f, 0.45f, 0.6f);
            // Ladoga: ufka uzanan buz ve karşı kıyının ince orman çizgisi.
            Snowfield(Line2(new Vector2(-10f, 470f), new Vector2(1700f, 466f), 201, 1f), 400f, Blend(Snow, haze, 0.3f), Blend(SnowShade, haze, 0.3f), 201);
            Range(Profile(478f, 10f, 0.01f, 202, 0.2f), 466f, Blend(Ink, Petrol, 0.4f), Blend(Ink, Paper, 0.3f), Snow, SnowShade, 2f, 0.1f, haze, 0.55f, haze, 0f, 202, 1f, 1f);
            float[] hills = Profile(440f, 50f, 0.004f, 203, 0.3f);
            for (int x = 0; x < hills.Length; x++) if (x > 900) hills[x] = Mathf.Lerp(hills[x], 420f, Mathf.Clamp01((x - 900f) / 200f));
            Range(hills, 380f, Blend(Ink, Petrol, 0.4f), Blend(Ink, Paper, 0.3f), Snow, SnowShade, 6f, 0.2f, haze, 0.3f, haze, 20f, 203, 1f, 5f);
            float[] yard = Profile(390f, 12f, 0.004f, 204, 0.2f);
            Snowfield(yard, 0f, Snow, SnowShade, 204);

            // Ambar (gri kütük) ve ana ev (aşı boyası, iki kat).
            LogHouse(300f, 300f, 330f, 170f, 110f, LogGrey, false, 205);
            Birch(760f, 280f, 330f);
            LogHouse(900f, 250f, 460f, 290f, 170f, FaluRed, true, 206);
            Birch(1420f, 240f, 360f);
            Birch(1500f, 250f, 280f);
            SweepWell(1560f, 230f, 280f, Blend(Ink, Soot, 0.4f));
            RailFence(new Vector2(-20f, 120f), new Vector2(520f, 250f), Blend(Ink, LogGrey, 0.4f));

            // Avluda yüklü kızak ve at; baltasıyla baba; Aino.
            Sleigh(640f, 150f, 220f, 1, Blend(Soot, Ink, 0.35f), true);
            Horse(900f, 150f, 230f, 1, Blend(Soot, Rust, 0.18f));
            Rect(1160f, 90f, 1210f, 130f, Blend(LogGrey, Ink, 0.3f));
            Ellipse(1185f, 130f, 26f, 8f, Blend(Paper, LogGrey, 0.4f));
            Person(1260f, 70f, 290f, Blend(Soot, Ink, 0.35f), Kind.Man, Carry.None, 0);
            Line(1205f, 260f, 1178f, 150f, 6f, Blend(Ink, Rust, 0.35f));
            Poly(new[] { new Vector2(1168f, 158f), new Vector2(1196f, 150f), new Vector2(1190f, 132f), new Vector2(1170f, 138f) }, Blend(Paper, Ink, 0.5f));
            Person(420f, 40f, 320f, Blend(Soot, Ink, 0.3f), Kind.Woman, Carry.None, 1);
            Drift(0f, 0f, 1672f, 40f, 207);
        }

        /// <summary>Tsasouna ve Anni'nin kulübesi: soğan kubbeli küçük ahşap şapel, ladin ormanı, alacakaranlık.</summary>
        private static void OrthodoxChapel()
        {
            lightDir = -1f;
            lightTone = Blend(Rust, Mustard, 0.5f);
            Color32 dusk = Blend(Blend(Rust, Petrol, 0.35f), Paper, 0.35f);
            Sky(Blend(Night, Petrol, 0.45f), dusk, 420f, Blend(Rust, Mustard, 0.45f), 220f, 460f, 520f, 0.5f, 0.45f);
            float[] ridge = Profile(430f, 60f, 0.004f, 211, 0.4f);
            Range(ridge, 300f, Blend(Night, Petrol, 0.4f), Blend(dusk, Ink, 0.4f), Blend(Snow, dusk, 0.3f), Blend(SnowShade, Night, 0.3f), 6f, 0.2f, dusk, 0.3f, dusk, 30f, 211, 1f, 4f);
            for (int i = 0; i < 26; i++)
            {
                float x = (float)rng.NextDouble() * 1700f - 20f;
                if (x > 520f && x < 1180f) continue;
                Pine(x, 300f + (float)rng.NextDouble() * 20f, 240f + (float)rng.NextDouble() * 220f, Blend(Night, Petrol, 0.3f), 0.35f);
            }
            float[] ground = Profile(310f, 10f, 0.004f, 212, 0.2f);
            Snowfield(ground, 0f, Blend(Snow, dusk, 0.25f), Blend(SnowShade, Night, 0.35f), 212);
            Track(new[] { new Vector2(260f, -20f), new Vector2(520f, 120f), new Vector2(760f, 240f), new Vector2(880f, 300f) }, 40f, 10f);

            Tsasouna(760f, 300f, 300f, Blend(LogGrey, Night, 0.25f));
            LogHouse(1080f, 280f, 250f, 150f, 90f, Blend(LogGrey, Night, 0.3f), false, 213);
            Rect(1160f, 330f, 1200f, 370f, Lamp, 0.95f);
            Glow(1180f, 350f, 180f, Lamp, 0.3f);
            Smoke(1250f, 460f, 0.8f);
            Person(1150f, 280f, 120f, Blend(Night, Soot, 0.5f), Kind.Woman, Carry.None, 0);
            Person(520f, 60f, 310f, Blend(Night, Soot, 0.65f), Kind.Woman, Carry.Walk, 1, 0.7f);
            Drift(0f, 0f, 1672f, 40f, 214, 0.2f);
        }

        /// <summary>Kilise mezarlığı: ahşap kilise ve ayrı çan kulesi, karın içinde tahta haçlar, huşlar.</summary>
        private static void Churchyard()
        {
            lightDir = 1f;
            lightTone = Blend(Paper, Mustard, 0.2f);
            Color32 haze = Blend(Paper, Ink, 0.1f);
            Sky(Blend(Paper, Ink, 0.3f), Blend(Paper, Petrol, 0.08f), 460f, Paper, 1200f, 700f, 600f, 0.25f, 0.8f);
            Range(Profile(470f, 40f, 0.004f, 221, 0.3f), 380f, Blend(Ink, Petrol, 0.35f), Blend(Ink, Paper, 0.35f), Snow, SnowShade, 6f, 0.2f, haze, 0.45f, haze, 30f, 221, 1f, 5f);
            float[] ground = Profile(380f, 10f, 0.004f, 222, 0.2f);
            Snowfield(ground, 0f, Snow, SnowShade, 222);

            WoodenChurch(560f, 360f, 520f, Blend(Paper, LogGrey, 0.35f));
            BellTower(1260f, 350f, 330f, Blend(LogGrey, FaluRed, 0.25f));
            Birch(250f, 330f, 380f);
            Birch(1480f, 300f, 420f);
            Birch(1560f, 310f, 330f);

            // Mezarlar: sıra sıra tahta haçlar, bazılarının üstünde küçük çatı; araları derin kar.
            for (int row = 0; row < 4; row++)
                for (int i = 0; i < 9; i++)
                {
                    float x = 120f + i * 175f + row * 40f + (float)rng.NextDouble() * 30f;
                    float y = 300f - row * 70f;
                    float h = 60f + row * 26f;
                    GraveCross(x, y, h, Blend(Ink, LogGrey, 0.3f), (i + row) % 3 == 0);
                }
            Kneel(760f, 60f, 260f, Blend(Soot, Ink, 0.3f), 1);
            GraveCross(880f, 60f, 190f, Blend(Ink, LogGrey, 0.25f), true);
            Glow(860f, 90f, 90f, Lamp, 0.45f);
            Ellipse(860f, 78f, 6f, 10f, Blend(Lamp, Paper, 0.4f));
            Person(560f, 40f, 300f, Blend(Soot, Ink, 0.35f), Kind.Man, Carry.None, 1);
            StoneWall(0f, 1672f, 26f);
        }

        /// <summary>Batıya giden yol: ladin ormanının arasından geçen atlı kızak sırası, uzakta sürü.</summary>
        private static void EvacuationRoad()
        {
            lightDir = -1f;
            lightTone = Blend(Paper, Petrol, 0.2f);
            Color32 haze = Blend(Paper, Petrol, 0.2f);
            Sky(Blend(Paper, Petrol, 0.5f), Blend(Paper, Petrol, 0.15f), 460f, Paper, 600f, 700f, 600f, 0.25f, 0.85f);
            Range(Profile(470f, 30f, 0.005f, 231, 0.3f), 400f, Blend(Ink, Petrol, 0.4f), Blend(Ink, Paper, 0.3f), Snow, SnowShade, 4f, 0.2f, haze, 0.5f, haze, 20f, 231, 1f, 3f);
            Snowfield(Line2(new Vector2(-10f, 420f), new Vector2(1700f, 420f), 232, 4f), 0f, Snow, SnowShade, 232);
            // Yol: ufukta daralan açık şerit.
            Poly(new[] { new Vector2(575f, 420f), new Vector2(630f, 420f), new Vector2(1180f, -10f), new Vector2(320f, -10f) }, Blend(SnowShade, Paper, 0.35f), 0.7f);
            Track(new[] { new Vector2(600f, 420f), new Vector2(560f, 250f), new Vector2(520f, 80f), new Vector2(500f, -20f) }, 34f, 6f);
            Track(new[] { new Vector2(610f, 420f), new Vector2(700f, 250f), new Vector2(820f, 80f), new Vector2(880f, -20f) }, 34f, 6f);
            // İki yanda derinliğe kaçan ladinler.
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 26; i++)
                {
                    float t = i / 25f;
                    float h = Mathf.Lerp(90f, 620f, t * t) * (0.85f + 0.3f * (float)rng.NextDouble());
                    float y = Mathf.Lerp(420f, 10f, t) + row * 12f;
                    float back = row * Mathf.Lerp(30f, 180f, t);
                    Color32 tone = Blend(Blend(Soot, Petrol, 0.3f), haze, 0.45f * (1f - t) + row * 0.08f);
                    Pine(Mathf.Lerp(540f, -40f, t) - back - (float)rng.NextDouble() * 30f, y, h, tone, 0.55f);
                    Pine(Mathf.Lerp(690f, 1720f, t) + back + (float)rng.NextDouble() * 30f, y, h * 0.92f, tone, 0.55f);
                }
            // Uzakta yolu kesen sürü.
            for (int i = 0; i < 22; i++) Cow(560f + i * 6f + (float)rng.NextDouble() * 4f, 408f + (i % 3) * 3f, 12f, Blend(Ink, haze, 0.3f), 1);
            // Kızak sırası: uzaktan yakına.
            float[] ts = { 0.1f, 0.25f, 0.42f, 0.62f, 0.85f };
            for (int i = 0; i < ts.Length; i++)
            {
                float t = ts[i];
                float y = Mathf.Lerp(410f, 30f, t);
                float x = Mathf.Lerp(605f, 720f, t);
                float h = Mathf.Lerp(24f, 220f, t * t * 0.9f + 0.1f * t);
                Color32 tone = Blend(Blend(Soot, Ink, 0.35f), haze, 0.5f * (1f - t));
                Sleigh(x - h * 1.0f, y, h, 1, tone, true);
                Horse(x + h * 0.25f, y, h * 1.05f, 1, tone);
                if (h > 60f) Person(x - h * 1.8f, y - 8f, h * 1.3f, tone, i % 2 == 0 ? Kind.Woman : Kind.Man, Carry.Walk, 1 + (i % 2) * 3, 0.7f);
            }
            Snowfall(160, 1.4f);
        }

        /// <summary>Yol üstündeki okul, gece: samanla döşenmiş sınıf, duvara yaslı sıralar, kara tahta, kızaran soba.</summary>
        private static void SchoolNight()
        {
            lightDir = 1f;
            lightTone = Blend(Lamp, Rust, 0.3f);
            Vector2 vp = new Vector2(836f, 440f);
            float bx0 = 520f, bx1 = 1150f, by0 = 200f, by1 = 620f;
            Color32 wall = Blend(Blend(Paper, Petrol, 0.25f), Night, 0.55f);
            Color32 floor = Blend(Blend(Mustard, Rust, 0.3f), Night, 0.45f);
            for (int Y = 0; Y < b.H; Y++)
                for (int X = 0; X < b.W; X++)
                {
                    float x = X / (float)S, y = Y / (float)S;
                    Color32 c;
                    if (x >= bx0 && x <= bx1 && y >= by0 && y <= by1) c = Blend(wall, Night, 0.1f);
                    else
                    {
                        float dx = x - vp.x, dy = y - vp.y;
                        float sx = dx / (dx > 0 ? (bx1 - vp.x) : (bx0 - vp.x));
                        float sy = dy / (dy > 0 ? (by1 - vp.y) : (by0 - vp.y));
                        if (sy >= sx && dy < 0)
                        {
                            float n = Noise(dx / -dy * 26f, 1f / -dy * 800f, seed + 3);
                            c = Blend(floor, Blend(Mustard, Paper, 0.2f), n * 0.35f);
                        }
                        else if (sy >= sx) c = Blend(wall, Night, 0.45f);
                        else
                        {
                            float v = dy / Mathf.Abs(dx);
                            float plank = Mathf.Repeat(v * 30f, 3.2f) < 0.12f ? 0.35f : 0f;
                            c = Blend(Blend(wall, Night, Mathf.Clamp01(0.4f - Mathf.Abs(dx) / 1800f)), Soot, plank);
                        }
                    }
                    b.Pixels[Y * b.W + X] = c;
                }
            // Kara tahta, tebeşirle bir kesir problemi.
            Rect(640f, 380f, 1030f, 560f, Blend(Ink, Petrol, 0.35f));
            Rect(630f, 370f, 1040f, 380f, Blend(Rust, Night, 0.3f));
            Chalk(695f, 490f, 30f, 3); Line(685f, 470f, 725f, 470f, 2f, Paper, 0.7f); Chalk(695f, 445f, 30f, 4);
            Line(745f, 470f, 765f, 470f, 2f, Paper, 0.7f); Line(755f, 460f, 755f, 480f, 2f, Paper, 0.7f);
            Chalk(790f, 490f, 30f, 1); Line(780f, 470f, 820f, 470f, 2f, Paper, 0.7f); Chalk(790f, 445f, 30f, 8);
            Line(840f, 465f, 860f, 465f, 2f, Paper, 0.7f); Line(840f, 475f, 860f, 475f, 2f, Paper, 0.7f);
            // Buz tutmuş yan pencereler.
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 2; i++)
                {
                    float k = 1.3f + i * 0.8f;
                    float x = vp.x + side * (bx1 - vp.x) * k;
                    float yb = vp.y + (by0 - vp.y) * k * 0.55f, yt = vp.y + (by1 - vp.y) * k * 0.75f;
                    float w = 60f * k;
                    Poly(new[] { new Vector2(x, yb), new Vector2(x + side * w, yb - 30f * k), new Vector2(x + side * w, yt + 20f * k), new Vector2(x, yt) }, Blend(Night, Petrol, 0.45f));
                    Poly(new[] { new Vector2(x, yb), new Vector2(x + side * w, yb - 30f * k), new Vector2(x + side * w, yb - 10f * k), new Vector2(x, yb + 20f * k) }, Blend(Moon, Paper, 0.3f), 0.5f);
                }
            // Duvara yaslanmış sıralar.
            for (int i = 0; i < 4; i++)
            {
                float x = 250f + i * 70f;
                Poly(new[] { new Vector2(x, 180f), new Vector2(x + 50f, 190f), new Vector2(x + 55f, 330f), new Vector2(x + 5f, 320f) }, Blend(Rust, Night, 0.5f));
                Line(x + 5f, 250f, x + 55f, 260f, 4f, Blend(Rust, Night, 0.3f));
            }
            // Soba: dökme demir, kızaran kapak, boru.
            Glow(1240f, 200f, 420f, Blend(Lamp, Rust, 0.3f), 0.55f);
            Rect(1180f, 60f, 1300f, 320f, Blend(Soot, Ink, 0.2f));
            Ellipse(1240f, 320f, 62f, 14f, Blend(Soot, Ink, 0.1f));
            Rect(1205f, 120f, 1275f, 180f, Blend(Lamp, Rust, 0.35f));
            Line(1240f, 320f, 1240f, 760f, 22f, Blend(Soot, Ink, 0.2f));
            Line(1240f, 760f, 1050f, 800f, 22f, Blend(Soot, Ink, 0.2f));
            // Samanda uyuyanlar ve oturanlar.
            Color32 dark = Blend(Night, Soot, 0.55f);
            for (int i = 0; i < 7; i++)
            {
                float x = 160f + i * 150f + (float)rng.NextDouble() * 30f;
                if (x > 1120f && x < 1360f) continue;
                Lying(x, 40f + (i % 2) * 70f, 160f - (i % 2) * 30f, dark, Blend(Blend(Rust, Petrol, (i % 3) * 0.4f), Night, 0.3f), i % 2 == 0 ? 1 : -1);
            }
            Seated(1440f, 80f, 250f, Blend(Night, Soot, 0.7f), true, -1, Blend(Rust, Night, 0.35f));
            Seated(1560f, 70f, 230f, Blend(Night, Soot, 0.7f), false, -1, Blend(Petrol, Night, 0.4f));
            Motes(900f, 100f, 1500f, 700f, 80, Blend(Lamp, Paper, 0.3f));
        }

        /// <summary>Koyun buzu: geniş ve düz beyaz; adalar, karşı kıyı, ırmak ağzında koyu bir su izi, buzu geçen kızak sırası.</summary>
        private static void FrozenBay()
        {
            lightDir = 1f;
            lightTone = Blend(Paper, Mustard, 0.3f);
            Color32 haze = Blend(Paper, Petrol, 0.18f);
            Sky(Blend(Paper, Petrol, 0.45f), Blend(Paper, Mustard, 0.18f), 420f, Blend(Paper, Mustard, 0.35f), 1250f, 520f, 700f, 0.4f, 0.75f);
            Range(Profile(420f, 14f, 0.008f, 241, 0.2f), 395f, Blend(Ink, Petrol, 0.35f), Blend(Ink, Paper, 0.3f), Snow, SnowShade, 2f, 0.1f, haze, 0.55f, haze, 0f, 241, 1f, 1f);
            Snowfield(Line2(new Vector2(-10f, 398f), new Vector2(1700f, 398f), 242, 0.5f), 0f, Blend(Snow, Paper, 0.1f), Blend(SnowShade, Petrol, 0.1f), 242);
            // Adalar: ormanlı tümsekler.
            float[] islandX = { 300f, 980f, 1400f };
            float[] islandW = { 180f, 120f, 240f };
            for (int i = 0; i < islandX.Length; i++)
            {
                float[] top = new float[Width + 2];
                for (int x = 0; x < top.Length; x++)
                {
                    float d = Mathf.Abs(x - islandX[i]) / islandW[i];
                    float bump = Fbm(x * 0.03f, i * 3.1f, seed + 250, 3);
                    top[x] = d < 1f ? 400f + (22f + i * 5f) * Mathf.Pow(1f - d * d, 0.7f) * (0.7f + 0.6f * bump) : -1f;
                }
                Range(top, 396f, Blend(Ink, Petrol, 0.4f), Blend(Ink, Paper, 0.3f), Snow, SnowShade, 3f, 0.15f, haze, 0.4f, haze, 0f, 243 + i, 1f, 1f);
                for (int k = 0; k < (int)(islandW[i] / 14f); k++)
                {
                    float px = islandX[i] + ((float)rng.NextDouble() * 2f - 1f) * islandW[i] * 0.85f;
                    if (At(top, px) < 400f) continue;
                    Pine(px, At(top, px) - 6f, 16f + (float)rng.NextDouble() * 22f, Blend(Blend(Ink, Petrol, 0.45f), haze, 0.35f), 0.3f);
                }
            }
            // Irmak ağzı: buzun inceldiği koyu şerit.
            List<Vector2> lead = new List<Vector2>();
            for (int i = 0; i <= 20; i++)
            {
                float t = i / 20f;
                float w = Mathf.Lerp(26f, 2f, t) * (0.6f + 0.8f * Noise(t * 9f, 1.5f, seed + 260));
                lead.Add(new Vector2(Mathf.Lerp(-10f, 440f, t), Mathf.Lerp(236f, 330f, t) + w));
            }
            for (int i = 20; i >= 0; i--)
            {
                float t = i / 20f;
                float w = Mathf.Lerp(22f, 2f, t) * (0.6f + 0.8f * Noise(t * 9f, 4.5f, seed + 261));
                lead.Add(new Vector2(Mathf.Lerp(-10f, 440f, t), Mathf.Lerp(236f, 330f, t) - w));
            }
            Poly(lead, Blend(River, Night, 0.45f), 0.95f);
            for (int i = 0; i < 12; i++)
            {
                float t = (float)rng.NextDouble();
                Line(Mathf.Lerp(-10f, 440f, t), Mathf.Lerp(236f, 330f, t), Mathf.Lerp(-10f, 440f, t) + 30f, Mathf.Lerp(236f, 330f, t) + 4f, 1.2f, Moon, 0.35f);
            }
            // Rüzgârın çizdiği ince izler.
            for (int i = 0; i < 90; i++)
            {
                float x = (float)rng.NextDouble() * 1700f, y = (float)rng.NextDouble() * 380f;
                float len = 30f + (float)rng.NextDouble() * (140f - y * 0.25f);
                Line(x, y, x + len, y + len * 0.02f, 1.2f, SnowShade, 0.25f);
            }
            // Kızak sırası: sağ önden sol uzağa.
            Track(new[] { new Vector2(1720f, 40f), new Vector2(1300f, 150f), new Vector2(900f, 260f), new Vector2(620f, 360f), new Vector2(520f, 396f) }, 30f, 3f);
            float[] ts = { 0.12f, 0.3f, 0.5f, 0.72f, 0.9f };
            for (int i = 0; i < ts.Length; i++)
            {
                float t = ts[i];
                float x = Mathf.Lerp(560f, 1560f, t), y = Mathf.Lerp(380f, 70f, t);
                float h = Mathf.Lerp(18f, 190f, t * t);
                Color32 tone = Blend(Blend(Soot, Ink, 0.35f), haze, 0.55f * (1f - t));
                Sleigh(x + h * 0.95f, y, h, -1, tone, true);
                Horse(x - h * 0.3f, y, h * 1.05f, -1, tone);
                if (h > 50f) Person(x + h * 1.9f, y - 6f, h * 1.3f, tone, i % 2 == 0 ? Kind.Man : Kind.Woman, Carry.Walk, 0, 0.7f);
            }
        }

        /// <summary>Yeni sınırın bu yanındaki istasyon, akşam: aşı sarısı istasyon binası, yük vagonları, lokomotifin buharı.</summary>
        private static void BorderStation()
        {
            lightDir = -1f;
            lightTone = Lamp;
            Color32 dusk = Blend(Blend(Petrol, Rust, 0.25f), Paper, 0.3f);
            Sky(Blend(Night, Petrol, 0.4f), dusk, 440f, Blend(Rust, Mustard, 0.4f), 1300f, 460f, 520f, 0.45f, 0.55f);
            Range(Profile(450f, 30f, 0.005f, 251, 0.3f), 380f, Blend(Night, Petrol, 0.4f), Blend(dusk, Ink, 0.4f), Blend(Snow, dusk, 0.3f), Blend(SnowShade, Night, 0.3f), 4f, 0.2f, dusk, 0.3f, dusk, 20f, 251, 1f, 3f);
            Snowfield(Line2(new Vector2(-10f, 400f), new Vector2(1700f, 400f), 252, 3f), 0f, Blend(Snow, dusk, 0.3f), Blend(SnowShade, Night, 0.35f), 252);
            // Raylar.
            Line(-10f, 250f, 1700f, 290f, 3f, Blend(Ink, Paper, 0.3f));
            Line(-10f, 225f, 1700f, 268f, 3f, Blend(Ink, Paper, 0.3f));
            // İstasyon binası.
            Rect(1150f, 280f, 1560f, 470f, Blend(Ochre, Night, 0.3f));
            Poly(new[] { new Vector2(1120f, 465f), new Vector2(1590f, 465f), new Vector2(1500f, 560f), new Vector2(1210f, 560f) }, Blend(Rust, Night, 0.45f));
            Poly(new[] { new Vector2(1120f, 470f), new Vector2(1590f, 470f), new Vector2(1560f, 490f), new Vector2(1150f, 490f) }, Blend(Snow, dusk, 0.3f));
            for (int i = 0; i < 4; i++)
            {
                float x = 1190f + i * 95f;
                Rect(x, 330f, x + 44f, 400f, i == 1 || i == 2 ? Lamp : Blend(Night, Soot, 0.4f), 0.95f);
                if (i == 1 || i == 2) Glow(x + 22f, 360f, 120f, Lamp, 0.25f);
            }
            // Lokomotif ve buhar.
            float lx = 180f;
            Rect(lx, 250f, lx + 300f, 360f, Blend(Soot, Ink, 0.1f));
            Rect(lx + 200f, 250f, lx + 310f, 420f, Blend(Soot, Ink, 0.15f));
            Rect(lx + 40f, 360f, lx + 80f, 430f, Blend(Soot, Ink, 0.1f));
            for (int i = 0; i < 4; i++) Ellipse(lx + 40f + i * 70f, 245f, 32f, 32f, Blend(Soot, Rust, 0.15f));
            for (int i = 0; i < 140; i++)
            {
                float t = (float)rng.NextDouble();
                float px = lx + 60f + t * 360f + ((float)rng.NextDouble() - 0.5f) * (20f + t * 120f);
                float py = 440f + t * 240f + Mathf.Sin(t * 4f) * 24f + ((float)rng.NextDouble() - 0.5f) * (16f + t * 70f);
                float r = 14f + t * 50f * (float)rng.NextDouble() + 10f;
                Ellipse(px, py, r, r * 0.75f, Blend(Paper, dusk, 0.25f + 0.3f * t), 0.07f * (1f - t * 0.5f));
            }
            // Yük vagonları: açık kapılar, tebeşirle köy adları.
            for (int i = 0; i < 5; i++)
            {
                float x = lx + 330f + i * 170f;
                Rect(x, 250f, x + 160f, 380f, Blend(FaluRed, Night, 0.4f));
                Poly(new[] { new Vector2(x - 4f, 378f), new Vector2(x + 164f, 378f), new Vector2(x + 150f, 396f), new Vector2(x + 10f, 396f) }, Blend(Snow, dusk, 0.35f));
                Rect(x + 55f, 262f, x + 105f, 360f, i % 2 == 0 ? Blend(Lamp, Rust, 0.4f) : Blend(Night, Soot, 0.5f));
                Line(x + 15f, 330f, x + 45f, 334f, 2f, Paper, 0.7f);
                Line(x + 115f, 332f, x + 148f, 328f, 2f, Paper, 0.7f);
                Ellipse(x + 30f, 245f, 16f, 16f, Soot);
                Ellipse(x + 130f, 245f, 16f, 16f, Soot);
            }
            // Peronda insanlar, bohçalar, bir kızak.
            Color32 dark = Blend(Night, Soot, 0.7f);
            Sleigh(1300f, 120f, 170f, -1, dark, true);
            Horse(1120f, 120f, 180f, -1, dark);
            for (int i = 0; i < 9; i++)
            {
                float x = 220f + i * 105f + (float)rng.NextDouble() * 30f;
                float h = 170f + (float)rng.NextDouble() * 40f;
                Person(x, 140f - (i % 3) * 20f, h, dark, i % 3 == 1 ? Kind.Man : Kind.Woman, i % 2 == 0 ? Carry.Bag : Carry.None, i % 2, 0.5f);
            }
            Person(400f, 20f, 330f, Blend(Night, Soot, 0.8f), Kind.Man, Carry.None, 1);
            Line(1600f, 120f, 1600f, 420f, 6f, dark);
            Rect(1580f, 420f, 1620f, 450f, Blend(Lamp, Paper, 0.3f));
            Glow(1600f, 435f, 160f, Lamp, 0.4f);
            Snowfall(120, 1.2f);
        }

        // ================================================================== Karelya parçaları

        /// <summary>At: sağlam gövde, eğik boyun, uzun baş, dört bacak ve kuyruk; boynunda koşum halkası.</summary>
        private static void Horse(float x, float y, float h, int dir, Color32 tone)
        {
            float d = dir;
            Ellipse(x, y + h * 0.60f, h * 0.40f, h * 0.19f, tone);
            Ellipse(x - d * h * 0.28f, y + h * 0.62f, h * 0.16f, h * 0.17f, tone);
            Ellipse(x + d * h * 0.26f, y + h * 0.64f, h * 0.17f, h * 0.18f, tone);
            Poly(new[] { new Vector2(x + d * h * 0.22f, y + h * 0.72f), new Vector2(x + d * h * 0.36f, y + h * 0.60f), new Vector2(x + d * h * 0.58f, y + h * 0.86f), new Vector2(x + d * h * 0.46f, y + h * 0.98f) }, tone);
            Poly(new[] { new Vector2(x + d * h * 0.46f, y + h * 0.98f), new Vector2(x + d * h * 0.56f, y + h * 0.90f), new Vector2(x + d * h * 0.74f, y + h * 0.72f), new Vector2(x + d * h * 0.70f, y + h * 0.66f), new Vector2(x + d * h * 0.52f, y + h * 0.80f) }, tone);
            Poly(new[] { new Vector2(x + d * h * 0.46f, y + h * 0.97f), new Vector2(x + d * h * 0.48f, y + h * 1.06f), new Vector2(x + d * h * 0.52f, y + h * 0.97f) }, tone);
            for (int i = 0; i < 4; i++)
            {
                float lx = x + d * h * (-0.30f + i * 0.19f) + (i == 1 || i == 3 ? d * h * 0.03f : 0f);
                float bend = i % 2 == 0 ? 0.02f : -0.02f;
                Line(lx, y + h * 0.55f, lx + d * h * bend, y + h * 0.20f, h * 0.06f, tone);
                Line(lx + d * h * bend, y + h * 0.20f, lx, y + h * 0.02f, h * 0.045f, tone);
                Ellipse(lx, y + h * 0.02f, h * 0.04f, h * 0.025f, tone);
            }
            Poly(new[] { new Vector2(x - d * h * 0.40f, y + h * 0.68f), new Vector2(x - d * h * 0.50f, y + h * 0.50f), new Vector2(x - d * h * 0.47f, y + h * 0.28f), new Vector2(x - d * h * 0.42f, y + h * 0.50f) }, tone);
            Line(x + d * h * 0.36f, y + h * 0.82f, x + d * h * 0.44f, y + h * 0.62f, h * 0.04f, Blend(tone, Rust, 0.35f));
            Line(x + d * h * 0.02f, y + h * 0.70f, x + lightDir * h * 0.2f, y + h * 0.76f, Mathf.Max(0.8f, h * 0.012f), Blend(tone, lightTone, 0.4f), 0.5f);
        }

        /// <summary>Kızak: kıvrık burunlu iki demir, alçak sandık, üstünde bağlı yük; öne uzanan oklar.</summary>
        private static void Sleigh(float x, float y, float h, int dir, Color32 tone, bool loaded)
        {
            float d = dir;
            float len = h * 1.1f;
            Line(x - d * len * 0.5f, y + h * 0.04f, x + d * len * 0.45f, y + h * 0.04f, Mathf.Max(1f, h * 0.025f), tone);
            Line(x + d * len * 0.45f, y + h * 0.04f, x + d * len * 0.55f, y + h * 0.16f, Mathf.Max(1f, h * 0.025f), tone);
            for (int i = 0; i < 3; i++)
            {
                float px = x + d * len * (-0.4f + i * 0.38f);
                Line(px, y + h * 0.04f, px, y + h * 0.16f, Mathf.Max(1f, h * 0.02f), tone);
            }
            Poly(new[] { new Vector2(x - d * len * 0.48f, y + h * 0.15f), new Vector2(x + d * len * 0.42f, y + h * 0.15f), new Vector2(x + d * len * 0.46f, y + h * 0.32f), new Vector2(x - d * len * 0.50f, y + h * 0.32f) }, tone);
            Line(x + d * len * 0.4f, y + h * 0.22f, x + d * (len * 0.5f + h * 0.62f), y + h * 0.48f, Mathf.Max(1f, h * 0.018f), tone);
            if (!loaded) return;
            Color32 sack = Blend(tone, Paper, 0.22f), bundle = Blend(tone, Rust, 0.3f), chest = Blend(tone, Mustard, 0.2f);
            Rect(x - d * len * 0.44f, y + h * 0.31f, x - d * len * 0.12f, y + h * 0.52f, chest);
            Line(x - d * len * 0.44f, y + h * 0.45f, x - d * len * 0.12f, y + h * 0.45f, Mathf.Max(0.8f, h * 0.012f), Blend(chest, Soot, 0.4f), 0.7f);
            Ellipse(x - d * len * 0.02f, y + h * 0.40f, len * 0.13f, h * 0.10f, sack);
            Ellipse(x + d * len * 0.16f, y + h * 0.38f, len * 0.12f, h * 0.085f, sack);
            Ellipse(x + d * len * 0.06f, y + h * 0.52f, len * 0.14f, h * 0.09f, bundle);
            Ellipse(x + d * len * 0.30f, y + h * 0.40f, len * 0.09f, h * 0.08f, bundle);
            Ellipse(x - d * len * 0.28f, y + h * 0.56f, len * 0.16f, h * 0.035f, Snow, 0.7f);
            Ellipse(x + d * len * 0.06f, y + h * 0.60f, len * 0.12f, h * 0.025f, Snow, 0.6f);
            Line(x - d * len * 0.46f, y + h * 0.33f, x + d * len * 0.36f, y + h * 0.50f, Mathf.Max(0.8f, h * 0.012f), Blend(tone, Paper, 0.35f), 0.7f);
        }

        /// <summary>İnek: uzakta sürünün bir parçası; gövde, baş, dört bacak.</summary>
        private static void Cow(float x, float y, float h, Color32 tone, int dir)
        {
            float d = dir;
            Ellipse(x, y + h * 0.55f, h * 0.45f, h * 0.22f, tone);
            Ellipse(x + d * h * 0.46f, y + h * 0.58f, h * 0.14f, h * 0.10f, tone);
            for (int i = 0; i < 4; i++) Line(x + d * h * (-0.32f + i * 0.2f), y + h * 0.45f, x + d * h * (-0.32f + i * 0.2f), y, Mathf.Max(0.6f, h * 0.07f), tone);
            Ellipse(x, y + h * 0.75f, h * 0.35f, h * 0.05f, Snow, 0.6f);
        }

        /// <summary>Huş: beyaz, kara lekeli gövde; kışın mor-kahve ince dallardan bir taç.</summary>
        private static void Birch(float x, float baseY, float h)
        {
            Color32 crown = Blend(Blend(Ink, Rust, 0.25f), Paper, 0.15f);
            HungerWinterSceneFactory.Branch(b, rng, P(x), P(baseY + h * 0.35f), Mathf.PI * 0.5f, h * 0.32f * S, h * 0.018f * S, 0, crown);
            float w = Mathf.Max(3f, h * 0.035f);
            Poly(new[] { new Vector2(x - w, baseY), new Vector2(x + w, baseY), new Vector2(x + w * 0.5f, baseY + h * 0.62f), new Vector2(x - w * 0.5f, baseY + h * 0.62f) }, BirchBark);
            Line(x - lightDir * w * 0.7f, baseY, x - lightDir * w * 0.4f, baseY + h * 0.6f, w * 0.5f, Blend(BirchBark, SnowShade, 0.5f), 0.7f);
            for (int i = 0; i < 12; i++)
            {
                float yy = baseY + h * 0.04f + i * h * 0.05f;
                float ww = w * (1f - yy / (baseY + h) * 0.3f);
                Line(x - ww * 0.8f, yy, x - ww * 0.8f + ww * (0.4f + (float)rng.NextDouble()), yy + 1f, Mathf.Max(1f, h * 0.006f), Soot, 0.8f);
            }
            Rect(x - w * 1.1f, baseY, x + w * 1.1f, baseY + h * 0.05f, Blend(Ink, Soot, 0.3f));
        }

        /// <summary>
        /// Kütük ev: yatay kütük çizgileri, beyaz pencere kasaları, dik ve karlı çatı; iki katlı
        /// olan aşı boyalıdır ve alnında oymalı bir kuşak taşır. Işıktan uzak yanda yan duvar.
        /// </summary>
        private static void LogHouse(float x, float baseY, float w, float h, float roof, Color32 tone, bool twoStorey, int s)
        {
            float side = w * 0.35f;
            float ox = -lightDir * side * 0.7f, oy = side * 0.26f;
            float sideX = lightDir > 0 ? x : x + w;
            Color32 sideTone = Blend(tone, Night, 0.35f);
            Poly(new[] { new Vector2(sideX, baseY), new Vector2(sideX + ox, baseY + oy), new Vector2(sideX + ox, baseY + h + oy), new Vector2(sideX, baseY + h) }, sideTone);
            Poly(new[] { new Vector2(x + w * 0.5f, baseY + h + roof), new Vector2(lightDir > 0 ? x - 12f : x + w + 12f, baseY + h - 6f), new Vector2((lightDir > 0 ? x - 12f : x + w + 12f) + ox, baseY + h - 6f + oy), new Vector2(x + w * 0.5f + ox, baseY + h + roof + oy) }, Blend(Snow, SnowShade, 0.35f));
            Rect(x, baseY, x + w, baseY + h, tone);
            Poly(new[] { new Vector2(x, baseY + h - 1f), new Vector2(x + w, baseY + h - 1f), new Vector2(x + w * 0.5f, baseY + h + roof - 6f) }, tone);
            for (float yy = baseY + 6f; yy < baseY + h; yy += 11f) Line(x, yy, x + w, yy, 1.4f, Blend(tone, Soot, 0.45f), 0.55f);
            for (float t = 0.1f; t < 1f; t += 0.14f) Line(sideX + ox * t, baseY + oy * t, sideX + ox * t, baseY + h + oy * t, 1.2f, Blend(sideTone, Soot, 0.4f), 0.4f);
            // Çatının ön saçağı: kalın kar.
            Line(x - 14f, baseY + h - 8f, x + w * 0.5f, baseY + h + roof, 10f, Blend(tone, Soot, 0.55f));
            Line(x + w + 14f, baseY + h - 8f, x + w * 0.5f, baseY + h + roof, 10f, Blend(tone, Soot, 0.55f));
            Line(x - 14f, baseY + h - 2f, x + w * 0.5f, baseY + h + roof + 6f, 7f, Snow);
            Line(x + w + 14f, baseY + h - 2f, x + w * 0.5f, baseY + h + roof + 6f, 7f, Snow);
            // Pencereler: beyaz kasa, koyu cam.
            int floors = twoStorey ? 2 : 1;
            int wins = Mathf.Max(2, (int)(w / 110f));
            for (int f = 0; f < floors; f++)
                for (int i = 0; i < wins; i++)
                {
                    float wx = x + w * (i + 0.5f) / wins;
                    float wy = baseY + h * (twoStorey ? (f == 0 ? 0.18f : 0.62f) : 0.32f);
                    float ww = Mathf.Min(34f, w * 0.08f), wh = h * (twoStorey ? 0.22f : 0.34f);
                    Rect(wx - ww * 0.5f - 4f, wy - 4f, wx + ww * 0.5f + 4f, wy + wh + 4f, Blend(Paper, Snow, 0.4f));
                    Rect(wx - ww * 0.5f, wy, wx + ww * 0.5f, wy + wh, Blend(Night, Petrol, 0.35f));
                    Line(wx, wy, wx, wy + wh, 2f, Blend(Paper, Snow, 0.4f));
                    Line(wx - ww * 0.5f, wy + wh * 0.5f, wx + ww * 0.5f, wy + wh * 0.5f, 2f, Blend(Paper, Snow, 0.4f));
                }
            if (twoStorey)
            {
                for (int i = 0; i < 18; i++)
                {
                    float t = i / 17f;
                    float px = Mathf.Lerp(x + 20f, x + w - 20f, t);
                    Poly(new[] { new Vector2(px - 8f, baseY + h - 2f), new Vector2(px + 8f, baseY + h - 2f), new Vector2(px, baseY + h - 16f) }, Blend(Paper, Snow, 0.3f), 0.85f);
                }
            }
            Rect(x + w * 0.44f, baseY, x + w * 0.56f, baseY + h * (twoStorey ? 0.36f : 0.6f), Blend(tone, Soot, 0.55f));
            Rect(x + w * 0.72f, baseY + h, x + w * 0.72f + 18f, baseY + h + roof * 0.7f, Blend(LogGrey, Soot, 0.3f));
            Rect(x + w * 0.72f - 3f, baseY + h + roof * 0.7f, x + w * 0.72f + 21f, baseY + h + roof * 0.7f + 6f, Snow);
        }

        /// <summary>Tsasouna: küçük kütük şapel, dik çatı, küçük soğan kubbe ve üç kollu haç.</summary>
        private static void Tsasouna(float x, float baseY, float h, Color32 tone)
        {
            float w = h * 0.55f;
            Rect(x - w * 0.5f, baseY, x + w * 0.5f, baseY + h * 0.42f, tone);
            for (float yy = baseY + 6f; yy < baseY + h * 0.42f; yy += 9f) Line(x - w * 0.5f, yy, x + w * 0.5f, yy, 1.2f, Blend(tone, Soot, 0.45f), 0.55f);
            Poly(new[] { new Vector2(x - w * 0.62f, baseY + h * 0.40f), new Vector2(x + w * 0.62f, baseY + h * 0.40f), new Vector2(x, baseY + h * 0.72f) }, Blend(tone, Soot, 0.4f));
            Poly(new[] { new Vector2(x - w * 0.64f, baseY + h * 0.42f), new Vector2(x, baseY + h * 0.74f), new Vector2(x + w * 0.64f, baseY + h * 0.42f), new Vector2(x, baseY + h * 0.68f) }, Blend(Snow, SnowShade, 0.3f));
            Rect(x - w * 0.08f, baseY + h * 0.70f, x + w * 0.08f, baseY + h * 0.80f, tone);
            Ellipse(x, baseY + h * 0.86f, w * 0.13f, h * 0.075f, Blend(tone, Petrol, 0.25f));
            Poly(new[] { new Vector2(x - w * 0.05f, baseY + h * 0.91f), new Vector2(x + w * 0.05f, baseY + h * 0.91f), new Vector2(x, baseY + h * 0.97f) }, Blend(tone, Petrol, 0.25f));
            Line(x, baseY + h * 0.96f, x, baseY + h * 1.1f, 3f, Blend(Mustard, Ink, 0.3f));
            Line(x - w * 0.06f, baseY + h * 1.06f, x + w * 0.06f, baseY + h * 1.06f, 2.5f, Blend(Mustard, Ink, 0.3f));
            Line(x - w * 0.09f, baseY + h * 1.02f, x + w * 0.09f, baseY + h * 1.02f, 2.5f, Blend(Mustard, Ink, 0.3f));
            Line(x - w * 0.05f, baseY + h * 0.975f, x + w * 0.05f, baseY + h * 0.995f, 2.5f, Blend(Mustard, Ink, 0.3f));
            Rect(x - w * 0.12f, baseY, x + w * 0.12f, baseY + h * 0.26f, Blend(Soot, Night, 0.3f));
            Glow(x, baseY + h * 0.1f, h * 0.3f, Lamp, 0.2f);
        }

        /// <summary>Ahşap kilise: uzun gövde, dik çatı, bir ucunda sivri külahlı kule.</summary>
        private static void WoodenChurch(float x, float baseY, float w, Color32 tone)
        {
            float h = w * 0.3f;
            LogHouse(x, baseY, w, h, w * 0.26f, tone, false, 900);
            float tx = x + w * 0.1f, tw = w * 0.16f;
            Rect(tx, baseY, tx + tw, baseY + h + w * 0.34f, Blend(tone, Paper, 0.1f));
            for (float yy = baseY + 8f; yy < baseY + h + w * 0.34f; yy += 12f) Line(tx, yy, tx + tw, yy, 1.2f, Blend(tone, Soot, 0.35f), 0.4f);
            Poly(new[] { new Vector2(tx - 6f, baseY + h + w * 0.34f), new Vector2(tx + tw + 6f, baseY + h + w * 0.34f), new Vector2(tx + tw * 0.5f, baseY + h + w * 0.62f) }, Blend(Ink, Petrol, 0.3f));
            Line(tx + tw * 0.5f, baseY + h + w * 0.62f, tx + tw * 0.5f, baseY + h + w * 0.68f, 3f, Blend(Mustard, Ink, 0.3f));
            Line(tx + tw * 0.5f - 7f, baseY + h + w * 0.655f, tx + tw * 0.5f + 7f, baseY + h + w * 0.655f, 3f, Blend(Mustard, Ink, 0.3f));
            Rect(tx + tw * 0.35f, baseY + h + w * 0.2f, tx + tw * 0.65f, baseY + h + w * 0.28f, Blend(Soot, Night, 0.3f));
        }

        /// <summary>Çan kulesi: kiliseden ayrı, piramit çatılı, üstte açık çan katı.</summary>
        private static void BellTower(float x, float baseY, float h, Color32 tone)
        {
            float w = h * 0.34f;
            Poly(new[] { new Vector2(x - w * 0.55f, baseY), new Vector2(x + w * 0.55f, baseY), new Vector2(x + w * 0.42f, baseY + h * 0.55f), new Vector2(x - w * 0.42f, baseY + h * 0.55f) }, tone);
            for (float yy = baseY + 8f; yy < baseY + h * 0.55f; yy += 10f) Line(x - w * 0.5f, yy, x + w * 0.5f, yy, 1.1f, Blend(tone, Soot, 0.4f), 0.45f);
            Rect(x - w * 0.42f, baseY + h * 0.55f, x + w * 0.42f, baseY + h * 0.72f, Blend(tone, Soot, 0.2f));
            Rect(x - w * 0.3f, baseY + h * 0.57f, x + w * 0.3f, baseY + h * 0.70f, Blend(Night, Soot, 0.3f));
            Poly(new[] { new Vector2(x - w * 0.05f, baseY + h * 0.685f), new Vector2(x + w * 0.05f, baseY + h * 0.685f), new Vector2(x + w * 0.08f, baseY + h * 0.61f), new Vector2(x + w * 0.12f, baseY + h * 0.595f), new Vector2(x - w * 0.12f, baseY + h * 0.595f), new Vector2(x - w * 0.08f, baseY + h * 0.61f) }, Blend(Mustard, Ink, 0.35f));
            Poly(new[] { new Vector2(x - w * 0.56f, baseY + h * 0.72f), new Vector2(x + w * 0.56f, baseY + h * 0.72f), new Vector2(x, baseY + h) }, Blend(Ink, Petrol, 0.25f));
            Poly(new[] { new Vector2(x - w * 0.56f, baseY + h * 0.73f), new Vector2(x, baseY + h * 1.01f), new Vector2(x - w * 0.1f, baseY + h * 0.8f) }, Blend(Snow, SnowShade, 0.3f), 0.9f);
        }

        /// <summary>Tahta mezar haçı; bazılarının üstünde küçük bir çatı. Dibi karda.</summary>
        private static void GraveCross(float x, float y, float h, Color32 tone, bool roofed)
        {
            Line(x, y, x, y + h, Mathf.Max(2f, h * 0.06f), tone);
            Line(x - h * 0.28f, y + h * 0.7f, x + h * 0.28f, y + h * 0.7f, Mathf.Max(2f, h * 0.05f), tone);
            if (roofed) Poly(new[] { new Vector2(x - h * 0.2f, y + h * 0.95f), new Vector2(x + h * 0.2f, y + h * 0.95f), new Vector2(x, y + h * 1.1f) }, tone);
            Line(x - h * 0.28f, y + h * 0.72f, x + h * 0.28f, y + h * 0.72f, Mathf.Max(1f, h * 0.025f), Snow, 0.9f);
            Ellipse(x, y + h * 0.05f, h * 0.25f, h * 0.08f, Snow);
        }

        /// <summary>Karelya'nın eğik sırıklı çiti.</summary>
        private static void RailFence(Vector2 a, Vector2 c, Color32 tone)
        {
            int n = 14;
            for (int i = 0; i <= n; i++)
            {
                Vector2 p = Vector2.Lerp(a, c, i / (float)n);
                float s = 1f - 0.4f * i / n;
                Line(p.x - 12f * s, p.y, p.x + 10f * s, p.y + 70f * s, 3f * s, tone);
                Line(p.x + 12f * s, p.y, p.x - 10f * s, p.y + 70f * s, 3f * s, tone);
            }
            for (int k = 0; k < 3; k++)
                Line(a.x, a.y + 20f + k * 14f, c.x, c.y + (20f + k * 14f) * 0.6f, 2.5f, tone);
        }

        /// <summary>Alçak taş duvar: ön planda karla örtülü iri taşlar.</summary>
        private static void StoneWall(float x0, float x1, float y)
        {
            for (float x = x0; x < x1; x += 40f + (float)rng.NextDouble() * 20f)
            {
                float r = 24f + (float)rng.NextDouble() * 14f;
                Ellipse(x, y, r, r * 0.6f, Blend(Ink, LogGrey, 0.4f + (float)rng.NextDouble() * 0.3f));
                Ellipse(x, y + r * 0.4f, r * 0.8f, r * 0.22f, Snow, 0.9f);
            }
        }

        /// <summary>
        /// Santralci, arkadan: taburede oturmuş, panoya dönük; saçı topuz, başında kulaklık
        /// kuşağı; sağ kolu yukarıda bir fişi deliğe uzatıyor, sol eli tezgâhta.
        /// </summary>
        private static void Operator(float x, float y, float h, Color32 tone)
        {
            Color32 wood = Blend(Rust, Night, 0.35f);
            Line(x - h * 0.13f, y, x - h * 0.11f, y + h * 0.30f, h * 0.02f, wood);
            Line(x + h * 0.13f, y, x + h * 0.11f, y + h * 0.30f, h * 0.02f, wood);
            Rect(x - h * 0.16f, y + h * 0.29f, x + h * 0.16f, y + h * 0.33f, wood);
            // Etek taburenin üstünden sarkar.
            Poly(new[] { new Vector2(x - h * 0.19f, y + h * 0.20f), new Vector2(x + h * 0.19f, y + h * 0.20f), new Vector2(x + h * 0.15f, y + h * 0.40f), new Vector2(x - h * 0.15f, y + h * 0.40f) }, tone);
            Line(x - h * 0.08f, y + h * 0.20f, x - h * 0.09f, y + h * 0.02f, h * 0.028f, tone);
            Ellipse(x - h * 0.09f, y + h * 0.012f, h * 0.035f, h * 0.013f, tone);
            // Sırt: omuzlara doğru genişler, bele doğru daralır.
            Poly(new[] { new Vector2(x - h * 0.11f, y + h * 0.40f), new Vector2(x + h * 0.11f, y + h * 0.40f), new Vector2(x + h * 0.15f, y + h * 0.62f), new Vector2(x + h * 0.10f, y + h * 0.68f), new Vector2(x - h * 0.10f, y + h * 0.68f), new Vector2(x - h * 0.15f, y + h * 0.62f) }, tone);
            // Sağ kol: yukarı ve öne, elinde fiş; sol kol: tezgâha.
            Vector2 sh = new Vector2(x + h * 0.12f, y + h * 0.64f);
            Vector2 el = new Vector2(x + h * 0.22f, y + h * 0.78f);
            Vector2 hand = new Vector2(x + h * 0.25f, y + h * 0.93f);
            Line(sh.x, sh.y, el.x, el.y, h * 0.04f, tone);
            Line(el.x, el.y, hand.x, hand.y, h * 0.034f, tone);
            Ellipse(hand.x, hand.y, h * 0.024f, h * 0.028f, tone);
            Line(hand.x, hand.y + h * 0.01f, hand.x + h * 0.01f, hand.y + h * 0.05f, h * 0.012f, Blend(Mustard, Paper, 0.2f));
            Line(hand.x, hand.y - h * 0.02f, hand.x - h * 0.02f, y + h * 0.36f, h * 0.008f, Blend(Ink, Rust, 0.3f));
            Line(x - h * 0.12f, y + h * 0.64f, x - h * 0.2f, y + h * 0.50f, h * 0.04f, tone);
            Line(x - h * 0.2f, y + h * 0.50f, x - h * 0.16f, y + h * 0.42f, h * 0.034f, tone);
            // Baş: arkadan; topuz ve kulaklık kuşağı.
            float hx = x, hy = y + h * 0.76f;
            Rect(hx - h * 0.03f, y + h * 0.66f, hx + h * 0.03f, hy - h * 0.03f, tone);
            Ellipse(hx, hy, h * 0.055f, h * 0.065f, tone);
            Ellipse(hx, hy - h * 0.035f, h * 0.035f, h * 0.028f, Blend(tone, Rust, 0.15f));
            Line(hx - h * 0.058f, hy + h * 0.005f, hx + h * 0.058f, hy + h * 0.005f, h * 0.01f, Blend(Ink, Paper, 0.3f));
            Ellipse(hx + h * 0.058f, hy, h * 0.016f, h * 0.022f, Blend(Ink, Paper, 0.3f));
            Ellipse(hx - h * 0.058f, hy, h * 0.016f, h * 0.022f, Blend(Ink, Paper, 0.3f));
            // Lamba tarafında ince kenar ışığı.
            Line(x + h * 0.14f, y + h * 0.44f, x + h * 0.15f, y + h * 0.62f, h * 0.008f, Blend(tone, Lamp, 0.55f), 0.55f);
            Line(hx + h * 0.05f, hy - h * 0.03f, hx + h * 0.05f, hy + h * 0.04f, h * 0.008f, Blend(tone, Lamp, 0.55f), 0.55f);
        }

        /// <summary>Tebeşirle yazılmış basit rakam (1, 3, 4, 7, 8): çizgilerden kurulur.</summary>
        private static void Chalk(float x, float y, float h, int digit)
        {
            Color32 c = Paper;
            float w = h * 0.45f, t = 2f, a = 0.7f;
            switch (digit)
            {
                case 1:
                    Line(x + w * 0.5f, y - h * 0.3f, x + w * 0.5f, y + h * 0.5f, t, c, a);
                    Line(x + w * 0.2f, y + h * 0.3f, x + w * 0.5f, y + h * 0.5f, t, c, a);
                    break;
                case 3:
                    Line(x, y + h * 0.5f, x + w, y + h * 0.5f, t, c, a);
                    Line(x + w, y + h * 0.5f, x + w * 0.4f, y + h * 0.1f, t, c, a);
                    Line(x + w * 0.4f, y + h * 0.1f, x + w, y - h * 0.1f, t, c, a);
                    Line(x + w, y - h * 0.1f, x, y - h * 0.3f, t, c, a);
                    break;
                case 4:
                    Line(x + w * 0.7f, y - h * 0.3f, x + w * 0.7f, y + h * 0.5f, t, c, a);
                    Line(x + w * 0.7f, y + h * 0.5f, x, y, t, c, a);
                    Line(x, y, x + w, y, t, c, a);
                    break;
                case 8:
                    Ellipse(x + w * 0.5f, y + h * 0.3f, w * 0.4f, h * 0.2f, c, 0.25f);
                    Ellipse(x + w * 0.5f, y - h * 0.1f, w * 0.48f, h * 0.22f, c, 0.25f);
                    break;
                default:
                    Line(x, y + h * 0.5f, x + w, y + h * 0.5f, t, c, a);
                    Line(x + w, y + h * 0.5f, x + w * 0.3f, y - h * 0.3f, t, c, a);
                    break;
            }
        }

        private static void Snowfall(int count, float size)
        {
            for (int i = 0; i < count; i++)
            {
                float x = (float)rng.NextDouble() * Width, y = (float)rng.NextDouble() * Height;
                Ellipse(x, y, size, size, Snow, 0.35f + (float)rng.NextDouble() * 0.4f);
            }
        }
    }
}
