using System.Collections.Generic;
using UnityEngine;

namespace OrdinaryFronts.Editor
{
    /// <summary>
    /// Bölüm seçim ekranının Avrupa haritasını üretir.
    /// <para>
    /// Coğrafi bir atlas değil, 1940'ların bir askeri durum haritası ya da belediye arşivinin
    /// duvarındaki şematik harita: düşük ayrıntılı kıyı çizgisi, beş derecelik ızgara, kıyı
    /// gölgesi, denizde oyma izleri. Kıyılar elle yazılmış gerçek enlem/boylam noktalarından
    /// çizilir ve <see cref="MapProjection"/> ile aynı projeksiyondan geçer; böylece çalışma
    /// anında aynı projeksiyonla yerleştirilen bölüm işaretleri tam yerine oturur.
    /// </para>
    /// <para>
    /// Yer adları haritaya basılmaz; işaretler ve etiketler arayüzden gelir ki dil
    /// değiştiğinde çevrilebilsinler.
    /// </para>
    /// </summary>
    internal static class EuropeMapFactory
    {
        internal const string FileName = "map_europe.png";
        internal const int Width = 1300;

        private static readonly Color32 Soot = new Color32(0x17, 0x1B, 0x21, 0xFF);
        private static readonly Color32 Paper = new Color32(0xD8, 0xCF, 0xB6, 0xFF);
        private static readonly Color32 Rust = new Color32(0x9E, 0x44, 0x34, 0xFF);
        private static readonly Color32 Petrol = new Color32(0x3F, 0x64, 0x68, 0xFF);
        private static readonly Color32 Ink = new Color32(0x26, 0x25, 0x22, 0xFF);

        internal static Texture2D Render()
        {
            int height = Mathf.RoundToInt(Width / MapProjection.Aspect);
            LinocutSceneFactory.Board b = new LinocutSceneFactory.Board(Width, height);
            System.Random rng = new System.Random(19430101);

            // Deniz belirgin biçimde petrol, kara sıcak kâğıt: ikisi yan yana bir bakışta
            // ayrılmalı. İlk denemede ton farkı küçüktü ve harita tek renk kâğıt gibi duruyordu.
            Color32 sea = LinocutSceneFactory.Blend(Paper, Petrol, 0.34f);
            Color32 land = LinocutSceneFactory.Blend(Paper, Ink, 0.05f);
            Color32 coastShade = LinocutSceneFactory.Blend(land, Petrol, 0.30f);

            // Deniz: hafif petrol yıkaması, üstünde kesik yatay oyma izleri.
            for (int y = 0; y < b.H; y++)
                for (int x = 0; x < b.W; x++)
                    b.Set(x, y, LinocutSceneFactory.Shift(sea, rng.Next(-4, 5)));
            for (int i = 0; i < 900; i++)
            {
                int x = rng.Next(0, b.W);
                int y = rng.Next(0, b.H);
                LinocutSceneFactory.Stroke(b, x, y, x + rng.Next(18, 90), y, LinocutSceneFactory.Blend(sea, Petrol, 0.22f), 1, 0.35f);
            }

            // Kara maskesi.
            bool[] mask = new bool[b.W * b.H];
            foreach (Vector2[] polygon in Polygons()) FillPolygon(mask, b.W, b.H, polygon);

            // Kıyı gölgesi: maskeyi içe doğru aşındırıp aşınan halkaları koyulaştır. Dönem
            // haritalarındaki kıyı taraması budur ve karayı denizden okutan asıl şey de bu.
            bool[] inner = (bool[])mask.Clone();
            const int rings = 14;
            for (int r = 0; r < rings; r++)
            {
                bool[] next = Erode(inner, b.W, b.H);
                float t = 1f - r / (float)rings;
                for (int i = 0; i < mask.Length; i++)
                {
                    if (!inner[i] || next[i]) continue;
                    int x = i % b.W, y = i / b.W;
                    if (rng.Next(0, 100) < 22 + (int)(60f * t))
                        b.Set(x, y, LinocutSceneFactory.Shift(LinocutSceneFactory.Blend(land, coastShade, 0.35f + 0.65f * t), rng.Next(-5, 6)));
                    else
                        b.Set(x, y, LinocutSceneFactory.Shift(land, rng.Next(-5, 6)));
                }
                inner = next;
            }
            for (int i = 0; i < mask.Length; i++)
                if (inner[i]) b.Set(i % b.W, i / b.W, LinocutSceneFactory.Shift(land, rng.Next(-5, 6)));

            // Kara içinde seyrek yatay tarama; düz kâğıt alan bırakmaz.
            for (int i = 0; i < 1400; i++)
            {
                int x = rng.Next(0, b.W);
                int y = rng.Next(0, b.H);
                if (!mask[y * b.W + x]) continue;
                LinocutSceneFactory.Stroke(b, x, y, x + rng.Next(10, 46), y, LinocutSceneFactory.Blend(land, Ink, 0.16f), 1, 0.30f);
            }

            // Kıyı çizgisi.
            foreach (Vector2[] polygon in Polygons())
            {
                for (int i = 0; i < polygon.Length; i++)
                {
                    Vector2 a = ToPixel(polygon[i], b.W, b.H);
                    Vector2 c = ToPixel(polygon[(i + 1) % polygon.Length], b.W, b.H);
                    Line(b, (int)a.x, (int)a.y, (int)c.x, (int)c.y, Ink, 2);
                }
            }

            // Beş derecelik ızgara: askerî haritanın koordinat ağı.
            Color32 grid = LinocutSceneFactory.Blend(Ink, Paper, 0.35f);
            for (float lon = Mathf.Ceil(MapProjection.LonMin / 5f) * 5f; lon <= MapProjection.LonMax; lon += 5f)
            {
                int x = (int)(MapProjection.Project(MapProjection.LatMin, lon).x * b.W);
                for (int y = 0; y < b.H; y++) if ((y / 6) % 2 == 0) b.BlendPx(x, y, grid, 0.28f);
            }
            for (float lat = Mathf.Ceil(MapProjection.LatMin / 5f) * 5f; lat <= MapProjection.LatMax; lat += 5f)
            {
                int y = (int)(MapProjection.Project(lat, MapProjection.LonMin).y * b.H);
                for (int x = 0; x < b.W; x++) if ((x / 6) % 2 == 0) b.BlendPx(x, y, grid, 0.28f);
            }

            // Kâğıt ve baskı dokusu.
            LinocutSceneFactory.PaperTooth(b, rng);
            LinocutSceneFactory.Halftone(b, rng);
            LinocutSceneFactory.Grain(b, rng, 5);
            LinocutSceneFactory.Vignette(b, rng);
            LinocutSceneFactory.EdgeSpeckle(b, rng);

            Texture2D texture = new Texture2D(b.W, b.H, TextureFormat.RGBA32, false, false);
            texture.SetPixels32(b.Pixels);
            texture.Apply(false, false);
            return texture;
        }

        /// <summary>
        /// Uçları incelmeyen düz çizgi. Kıyı ardışık kenarlardan oluşur; incelen uçlar her
        /// kenarı ayrı bir kesik gibi gösteriyor ve kıyı zincir izi gibi okunuyordu.
        /// </summary>
        private static void Line(LinocutSceneFactory.Board b, int x0, int y0, int x1, int y1, Color32 color, int thickness)
        {
            int steps = Mathf.Max(Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)), 1);
            int half = thickness / 2;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                for (int oy = -half; oy <= half; oy++)
                    for (int ox = -half; ox <= half; ox++)
                        b.Set(x + ox, y + oy, color);
            }
        }

        private static Vector2 ToPixel(Vector2 lonLat, int width, int height)
        {
            Vector2 uv = MapProjection.Project(lonLat.y, lonLat.x);
            return new Vector2(uv.x * width, uv.y * height);
        }

        /// <summary>Çift-tek tarama doldurma; maskeye VEYA ile eklenir.</summary>
        private static void FillPolygon(bool[] mask, int width, int height, Vector2[] polygon)
        {
            int n = polygon.Length;
            Vector2[] px = new Vector2[n];
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                px[i] = ToPixel(polygon[i], width, height);
                minY = Mathf.Min(minY, px[i].y);
                maxY = Mathf.Max(maxY, px[i].y);
            }
            List<float> crossings = new List<float>();
            int yStart = Mathf.Max(0, Mathf.FloorToInt(minY));
            int yEnd = Mathf.Min(height - 1, Mathf.CeilToInt(maxY));
            for (int y = yStart; y <= yEnd; y++)
            {
                float sy = y + 0.5f;
                crossings.Clear();
                for (int i = 0; i < n; i++)
                {
                    Vector2 a = px[i], c = px[(i + 1) % n];
                    if (a.y == c.y) continue;
                    // Yarı açık aralık: alt uç dahil, üst uç hariç; köşeler iki kez sayılmaz.
                    if ((sy >= a.y && sy < c.y) || (sy >= c.y && sy < a.y))
                        crossings.Add(a.x + (sy - a.y) * (c.x - a.x) / (c.y - a.y));
                }
                crossings.Sort();
                for (int i = 0; i + 1 < crossings.Count; i += 2)
                {
                    int x0 = Mathf.Max(0, Mathf.RoundToInt(crossings[i]));
                    int x1 = Mathf.Min(width - 1, Mathf.RoundToInt(crossings[i + 1]));
                    for (int x = x0; x <= x1; x++) mask[y * width + x] = true;
                }
            }
        }

        private static bool[] Erode(bool[] mask, int width, int height)
        {
            bool[] result = new bool[mask.Length];
            for (int y = 1; y < height - 1; y++)
                for (int x = 1; x < width - 1; x++)
                {
                    int i = y * width + x;
                    if (!mask[i]) continue;
                    result[i] = mask[i - 1] && mask[i + 1] && mask[i - width] && mask[i + width];
                }
            return result;
        }

        // ------------------------------------------------------------------ kıyılar

        private static Vector2 P(float lon, float lat) { return new Vector2(lon, lat); }

        /// <summary>
        /// Kıyı çokgenleri (boylam, enlem). Anakara tek çokgendir: İberya'dan saat yönünün
        /// tersine Atlantik, Manş, Jutland, Baltık, Finlandiya, Botniya, İsveç, Norveç, harita
        /// kenarı, Karadeniz'in kuzey kıyısı, Ege, Adriyatik, İtalya, Riviera ve İspanya.
        /// Baltık ve Karadeniz bu izlemeyle kendiliğinden deniz kalır; Anadolu ve adalar
        /// ayrı çokgenlerdir.
        /// </summary>
        private static IEnumerable<Vector2[]> Polygons()
        {
            yield return new[]
            {
                // İberya
                P(-5.3f,36.1f), P(-6.3f,36.5f), P(-9.0f,37.0f), P(-9.2f,38.7f), P(-8.7f,41.2f), P(-9.3f,43.0f),
                P(-8.4f,43.4f), P(-3.8f,43.5f), P(-1.6f,43.4f),
                // Fransa Atlantik ve Manş
                P(-1.2f,44.7f), P(-1.2f,46.2f), P(-2.2f,47.2f), P(-4.5f,48.4f), P(-1.6f,49.7f), P(0.1f,49.5f), P(1.9f,51.0f),
                // Benelüks, Almanya kıyısı
                P(4.0f,52.0f), P(4.8f,53.0f), P(7.2f,53.5f), P(8.7f,53.9f),
                // Jutland
                P(8.4f,55.5f), P(8.1f,56.7f), P(10.6f,57.7f), P(10.2f,56.2f), P(9.5f,55.5f), P(9.4f,54.8f),
                // Baltık güney kıyısı
                P(10.1f,54.3f), P(10.7f,53.9f), P(12.1f,54.1f), P(13.1f,54.3f), P(14.3f,53.9f), P(18.6f,54.4f),
                P(20.5f,54.7f), P(21.1f,55.7f), P(24.1f,57.0f), P(24.5f,58.4f), P(24.7f,59.4f), P(28.2f,59.4f), P(30.3f,59.9f),
                // Finlandiya ve Botniya
                P(28.7f,60.7f), P(25.0f,60.2f), P(22.3f,60.4f), P(21.6f,63.1f), P(25.5f,65.0f), P(24.0f,66.5f),
                P(22.2f,65.6f), P(20.3f,63.8f), P(17.3f,62.4f), P(17.1f,60.7f), P(18.1f,59.3f), P(17.1f,58.6f),
                // İsveç güney ve batı
                P(16.4f,56.7f), P(15.6f,56.2f), P(13.8f,55.4f), P(13.0f,55.6f), P(12.7f,56.0f), P(12.0f,57.7f), P(10.7f,59.9f),
                // Norveç
                P(8.0f,58.1f), P(5.7f,59.0f), P(5.3f,60.4f), P(6.2f,62.5f), P(10.4f,63.4f), P(14.4f,67.3f), P(18.9f,69.6f), P(25.8f,71.2f),
                // Harita kenarı boyunca doğu, sonra Karadeniz'in kuzeydoğusuna
                P(44.0f,70.0f), P(44.0f,43.5f),
                // Karadeniz kuzey kıyısı
                P(39.7f,43.6f), P(37.0f,44.9f), P(36.5f,45.3f), P(34.2f,44.5f), P(33.5f,44.6f), P(32.5f,45.4f), P(30.7f,46.5f),
                P(29.7f,45.2f), P(28.6f,44.2f), P(27.9f,43.2f), P(27.5f,42.5f), P(28.9f,41.1f),
                // Trakya ve Yunanistan
                P(27.5f,41.0f), P(26.4f,40.4f), P(25.9f,40.8f), P(24.4f,40.9f), P(22.9f,40.6f), P(23.4f,39.9f), P(22.9f,39.4f),
                P(23.7f,38.0f), P(24.0f,37.6f), P(22.8f,37.6f), P(23.2f,36.4f), P(22.1f,37.0f), P(21.7f,36.9f), P(21.7f,38.2f),
                P(20.8f,38.9f), P(20.3f,39.5f),
                // Adriyatik doğu kıyısı
                P(19.5f,40.5f), P(19.4f,41.3f), P(19.1f,42.1f), P(18.1f,42.6f), P(16.4f,43.5f), P(15.2f,44.1f), P(14.4f,45.3f), P(13.8f,45.6f),
                // İtalya
                P(12.3f,45.4f), P(12.2f,44.4f), P(13.5f,43.6f), P(14.2f,42.5f), P(16.9f,41.1f), P(17.9f,40.6f), P(18.5f,40.1f),
                P(18.4f,39.8f), P(17.2f,40.5f), P(17.1f,39.1f), P(16.1f,38.0f), P(15.6f,38.1f), P(15.8f,40.0f), P(14.8f,40.7f),
                P(14.3f,40.8f), P(13.6f,41.2f), P(12.3f,41.7f), P(11.8f,42.1f), P(10.5f,42.9f), P(10.3f,43.5f), P(9.8f,44.1f),
                P(8.9f,44.4f), P(7.8f,43.8f),
                // Riviera ve İspanya Akdeniz kıyısı
                P(7.3f,43.7f), P(5.9f,43.1f), P(5.4f,43.3f), P(3.9f,43.5f), P(3.0f,42.7f), P(2.2f,41.4f), P(1.2f,41.1f),
                P(-0.4f,39.5f), P(-0.5f,38.3f), P(-1.0f,37.6f), P(-2.5f,36.8f), P(-4.4f,36.7f),
            };

            // Anadolu
            yield return new[]
            {
                P(29.1f,41.0f), P(31.0f,41.2f), P(33.0f,42.0f), P(35.2f,42.0f), P(36.3f,41.3f), P(39.7f,41.0f), P(44.0f,41.6f),
                P(44.0f,36.6f), P(36.2f,36.6f), P(34.6f,36.8f), P(32.8f,36.1f), P(30.7f,36.9f), P(29.1f,36.6f), P(28.3f,36.8f),
                P(27.4f,37.0f), P(26.9f,38.4f), P(26.7f,39.3f), P(26.4f,40.1f), P(27.9f,40.4f),
            };

            // Britanya
            yield return new[]
            {
                P(-5.7f,50.1f), P(-4.1f,50.4f), P(-1.1f,50.8f), P(1.3f,51.1f), P(0.7f,51.5f), P(1.7f,52.9f), P(0.0f,53.6f),
                P(-0.1f,54.1f), P(-1.4f,55.0f), P(-2.0f,55.9f), P(-3.0f,56.0f), P(-2.1f,57.1f), P(-3.5f,57.6f), P(-3.1f,58.4f),
                P(-5.0f,58.6f), P(-5.2f,57.9f), P(-6.2f,57.3f), P(-6.0f,56.4f), P(-5.7f,55.3f), P(-4.6f,55.5f), P(-3.5f,54.9f),
                P(-3.0f,53.4f), P(-4.6f,53.3f), P(-4.7f,52.8f), P(-4.7f,52.1f), P(-5.3f,51.7f), P(-3.9f,51.6f), P(-2.7f,51.5f),
                P(-4.9f,50.5f),
            };

            // İrlanda
            yield return new[]
            {
                P(-8.5f,51.9f), P(-7.1f,52.2f), P(-6.4f,52.3f), P(-6.2f,53.3f), P(-6.4f,54.0f), P(-5.9f,54.6f), P(-6.2f,55.2f),
                P(-7.3f,55.0f), P(-8.3f,55.2f), P(-8.5f,54.3f), P(-9.1f,53.3f), P(-10.3f,52.1f), P(-9.5f,51.6f),
            };

            // Sicilya, Sardunya, Korsika, Girit, Zelanda (Danimarka)
            yield return new[] { P(12.4f,38.0f), P(13.3f,38.2f), P(15.2f,38.2f), P(15.6f,38.0f), P(15.1f,37.3f), P(15.1f,36.7f), P(12.6f,37.6f) };
            yield return new[] { P(8.2f,41.1f), P(9.7f,40.9f), P(9.6f,39.2f), P(8.4f,38.9f), P(8.4f,40.0f) };
            yield return new[] { P(8.6f,42.9f), P(9.5f,42.8f), P(9.3f,41.4f), P(8.7f,41.5f) };
            yield return new[] { P(23.5f,35.5f), P(24.5f,35.6f), P(26.3f,35.3f), P(25.0f,34.9f), P(23.6f,35.2f) };
            yield return new[] { P(11.1f,55.7f), P(12.6f,55.9f), P(12.6f,55.2f), P(11.2f,55.2f) };
        }
    }
}
