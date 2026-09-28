using System.Collections.Generic;
using UnityEngine;

namespace OrdinaryFronts.Editor
{
    /// <summary>
    /// Bölüm seçim ekranının Avrupa haritası (1.7 yenileme).
    /// <para>
    /// İlk harita birkaç düzine noktalık kaba çokgenlerden, kesik yatay taramalardan ve yırtık
    /// bir kenardan oluşuyordu; İtalya bir çubuk, Baltık bir göl gibi duruyordu. Bu üretici,
    /// 1940'ların bir kartografya bürosundan çıkmış gibi duran bir harita çizer:
    /// </para>
    /// <list type="bullet">
    /// <item>Elle yazılmış, yarım derece ayrıntılı kıyılar; adalar; Ladoga, Onega, Vänern,
    /// Vättern ve Peipus gölleri; Karadeniz, Azak ve Marmara.</item>
    /// <item>Denizde kıyıya paralel eski usul dalga çizgileri (uzaklık alanından), karada kıyı
    /// boyunca ince bir iç gölge.</item>
    /// <item>Başlıca nehirler (kaynaktan ağza kalınlaşan) ve dağ sıraları için tarama.</item>
    /// <item>Beş derecelik ince ızgara, derece bantlı çift çerçeve, pusula gülü, kâğıt dokusu.</item>
    /// </list>
    /// Siyasi sınır çizilmez (GDD §12): 1940'larda hangi sınırın "geçerli" olduğu başlı başına
    /// bir iddiadır. Yer adı da basılmaz; bölüm işaretleri arayüzden gelir ve çevrilir.
    /// Kenar yumuşatma için kara maskesi iki kat çözünürlükte örneklenir.
    /// </summary>
    internal static class EuropeMapFactory
    {
        internal const string FileName = "map_europe.png";
        internal const int Width = 1600;
        private const int SS = 2;

        private static readonly Color32 Paper = new Color32(0xDD, 0xD3, 0xB8, 0xFF);
        private static readonly Color32 PaperDark = new Color32(0xC9, 0xBC, 0x9A, 0xFF);
        private static readonly Color32 Ink = new Color32(0x2A, 0x28, 0x24, 0xFF);
        private static readonly Color32 SeaLight = new Color32(0xB9, 0xC4, 0xB8, 0xFF);
        private static readonly Color32 SeaDeep = new Color32(0x8E, 0xA4, 0xA0, 0xFF);
        private static readonly Color32 Water = new Color32(0x4E, 0x6E, 0x70, 0xFF);
        private static readonly Color32 Rust = new Color32(0x9E, 0x44, 0x34, 0xFF);

        private static int W, H;

        /// <summary>Geliştirme önizlemesi: Logs/ScenePreview altına yazar, oyun varlıklarına dokunmaz.</summary>
        public static void Preview()
        {
            string dir = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, "Logs", "ScenePreview");
            System.IO.Directory.CreateDirectory(dir);
            System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
            Texture2D texture = Render();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, FileName), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            Debug.Log("SCENE_PREVIEW map " + watch.ElapsedMilliseconds + " ms");
        }

        internal static Texture2D Render()
        {
            W = Width;
            H = Mathf.RoundToInt(Width / MapProjection.Aspect);
            int sw = W * SS, sh = H * SS;

            // Kara örtüsü: iki kat çözünürlükte maske, sonra piksel başına kapsama.
            bool[] fine = new bool[sw * sh];
            int seedPoly = 0;
            foreach (Vector2[] poly in Land()) Fill(fine, sw, sh, Coast(poly, seedPoly++), true);
            foreach (Vector2[] poly in Waters()) Fill(fine, sw, sh, Coast(poly, seedPoly++), false);
            float[] cover = new float[W * H];
            bool[] land = new bool[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int n = 0;
                    for (int j = 0; j < SS; j++)
                        for (int i = 0; i < SS; i++)
                            if (fine[(y * SS + j) * sw + x * SS + i]) n++;
                    cover[y * W + x] = n / (float)(SS * SS);
                    land[y * W + x] = n * 2 >= SS * SS;
                }

            float[] seaDist = Blur(Distance(land, false), 3, 3);  // denizde kıyıya uzaklık; basamaklar yumuşatılır
            float[] landDist = Distance(land, true);  // karada kıyıya uzaklık
            Color32[] px = new Color32[W * H];
            float[] relief = Relief(land);

            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    float stain = Fbm(x * 0.006f, y * 0.006f, 11, 4);
                    float fibre = Noise(x * 0.9f, y * 0.12f, 12) * 0.5f + Noise(x * 0.07f, y * 0.9f, 13) * 0.5f;

                    // Deniz: kıyıdan açığa koyulaşan yıkama ve kıyıya paralel dalga çizgileri.
                    float d = seaDist[i];
                    Color32 sea = Blend(SeaLight, SeaDeep, Mathf.Clamp01(d / 160f));
                    float wave = 0f;
                    float[] rings = { 5f, 11f, 18f, 27f };
                    for (int r = 0; r < rings.Length; r++)
                    {
                        float k = Mathf.Abs(d - rings[r]);
                        if (k < 0.8f) wave = Mathf.Max(wave, (1f - k / 0.8f) * (0.42f - r * 0.08f));
                    }
                    sea = Blend(sea, Water, wave);
                    // Açık denizde ince yatay oyma izleri.
                    if (d > 40f && Mathf.Repeat(y + Noise(x * 0.01f, y * 0.05f, 14) * 4f, 7f) < 0.8f)
                        sea = Blend(sea, Water, 0.10f * Smooth(40f, 90f, d));

                    // Kara: sıcak kâğıt, kıyı boyunca ince iç gölge.
                    float ld = landDist[i];
                    Color32 ground = Blend(Paper, PaperDark, 0.35f * stain + 0.15f * Smooth(3f, 0f, ld));
                    ground = Blend(ground, Ink, 0.10f * Smooth(2.5f, 0.5f, ld));
                    // Dağların çevresinde yumuşak kabartma gölgesi.
                    ground = Blend(ground, new Color32(0x8C, 0x7A, 0x5C, 0xFF), relief[i]);

                    Color32 c = Blend(sea, ground, cover[i]);
                    // Kıyı çizgisi: kapsama geçişinde mürekkep.
                    float edge = Mathf.Min(cover[i], 1f - cover[i]) * 2f;
                    float coastDist = land[i] ? ld : d;
                    float coast = Mathf.Max(edge, Smooth(1.3f, 0.3f, coastDist));
                    c = Blend(c, Ink, 0.85f * coast);
                    // Kâğıt: lif ve lekeler.
                    c = Blend(c, PaperDark, 0.06f + 0.10f * stain);
                    c = Shift(c, (int)((fibre - 0.5f) * 10f));
                    px[i] = c;
                }

            foreach (Ridge r in Ridges()) Hachure(px, land, r);
            foreach (River r in Rivers()) DrawRiver(px, land, r);
            Graticule(px, land);
            Compass(px, Project(-5.5f, 62.2f), 62f);
            Neatline(px);

            Texture2D texture = new Texture2D(W, H, TextureFormat.RGBA32, false, false);
            // Pano alttan yukarı sayılır, doku da öyle.
            texture.SetPixels32(px);
            texture.Apply(false, false);
            return texture;
        }

        // ================================================================== geometri

        private static Vector2 Project(float lon, float lat)
        {
            Vector2 uv = MapProjection.Project(lat, lon);
            return new Vector2(uv.x * W, uv.y * H);
        }

        private static void Fill(bool[] mask, int mw, int mh, Vector2[] poly, bool value)
        {
            int n = poly.Length;
            Vector2[] p = new Vector2[n];
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                Vector2 q = Project(poly[i].x, poly[i].y) * SS;
                p[i] = q;
                minY = Mathf.Min(minY, q.y);
                maxY = Mathf.Max(maxY, q.y);
            }
            List<float> xs = new List<float>();
            for (int y = Mathf.Max(0, (int)minY); y <= Mathf.Min(mh - 1, (int)maxY + 1); y++)
            {
                float yc = y + 0.5f;
                xs.Clear();
                for (int i = 0; i < n; i++)
                {
                    Vector2 a = p[i], b = p[(i + 1) % n];
                    if ((a.y <= yc && b.y > yc) || (b.y <= yc && a.y > yc))
                        xs.Add(a.x + (yc - a.y) / (b.y - a.y) * (b.x - a.x));
                }
                xs.Sort();
                for (int k = 0; k + 1 < xs.Count; k += 2)
                {
                    int x0 = Mathf.Max(0, Mathf.CeilToInt(xs[k] - 0.5f));
                    int x1 = Mathf.Min(mw - 1, Mathf.FloorToInt(xs[k + 1] - 0.5f));
                    for (int x = x0; x <= x1; x++) mask[y * mw + x] = value;
                }
            }
        }

        /// <summary>
        /// Kıyıyı doğallaştırır: üç tur köşe kesme (Chaikin) ve kıyıya dik küçük, gürültüden
        /// gelen girinti çıkıntı. Elle yazılmış yarım derecelik noktalar düz kenarlar bırakıyordu.
        /// </summary>
        private static Vector2[] Coast(Vector2[] poly, int seed)
        {
            List<Vector2> pts = new List<Vector2>(poly);
            for (int iter = 0; iter < 3; iter++)
            {
                List<Vector2> next = new List<Vector2>(pts.Count * 2);
                for (int i = 0; i < pts.Count; i++)
                {
                    Vector2 a = pts[i], b = pts[(i + 1) % pts.Count];
                    next.Add(Vector2.Lerp(a, b, 0.25f));
                    next.Add(Vector2.Lerp(a, b, 0.75f));
                }
                pts = next;
            }
            Vector2[] result = new Vector2[pts.Count];
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 prev = pts[(i - 1 + pts.Count) % pts.Count], next = pts[(i + 1) % pts.Count];
                Vector2 tangent = (next - prev).normalized;
                Vector2 normal = new Vector2(-tangent.y, tangent.x);
                bool outside = !MapProjection.IsInside(pts[i].y, pts[i].x);
                float n = outside ? 0f : (Fbm(pts[i].x * 1.7f, pts[i].y * 1.7f, 70 + seed, 3) - 0.5f) * 0.28f
                    + (Noise(pts[i].x * 9f, pts[i].y * 9f, 90 + seed) - 0.5f) * 0.07f;
                result[i] = pts[i] + normal * n;
            }
            return result;
        }

        private static float[] Blur(float[] src, int radius, int passes)
        {
            float[] a = (float[])src.Clone(), b = new float[src.Length];
            for (int p = 0; p < passes; p++)
            {
                for (int y = 0; y < H; y++)
                {
                    float acc = 0f; int cnt = 0;
                    for (int x = -radius; x < W + radius; x++)
                    {
                        int add = x + radius, rem = x - radius - 1;
                        if (add >= 0 && add < W) { acc += a[y * W + add]; cnt++; }
                        if (rem >= 0 && rem < W) { acc -= a[y * W + rem]; cnt--; }
                        if (x >= 0 && x < W) b[y * W + x] = acc / Mathf.Max(1, cnt);
                    }
                }
                for (int x = 0; x < W; x++)
                {
                    float acc = 0f; int cnt = 0;
                    for (int y = -radius; y < H + radius; y++)
                    {
                        int add = y + radius, rem = y - radius - 1;
                        if (add >= 0 && add < H) { acc += b[add * W + x]; cnt++; }
                        if (rem >= 0 && rem < H) { acc -= b[rem * W + x]; cnt--; }
                        if (y >= 0 && y < H) a[y * W + x] = acc / Mathf.Max(1, cnt);
                    }
                }
            }
            // Kıyıya çok yakın değerler bulanıklıktan önceki hâlinde kalsın: kıyı çizgisi kaymasın.
            for (int i = 0; i < a.Length; i++) if (src[i] < 2f) a[i] = src[i];
            return a;
        }

        /// <summary>Dağ sırtlarına uzaklıktan türeyen yumuşak kabartma: sırtın hemen yanı en koyu.</summary>
        private static float[] Relief(bool[] land)
        {
            float[] r = new float[W * H];
            List<Vector2[]> segs = new List<Vector2[]>();
            List<float> widths = new List<float>();
            foreach (Ridge ridge in Ridges())
                for (int s = 0; s + 1 < ridge.pts.Length; s++)
                {
                    segs.Add(new[] { Project(ridge.pts[s].x, ridge.pts[s].y), Project(ridge.pts[s + 1].x, ridge.pts[s + 1].y) });
                    widths.Add(ridge.width);
                }
            for (int k = 0; k < segs.Count; k++)
            {
                Vector2 a = segs[k][0], b = segs[k][1];
                float reach = widths[k] * 5f;
                int x0 = Mathf.Max(0, (int)(Mathf.Min(a.x, b.x) - reach)), x1 = Mathf.Min(W - 1, (int)(Mathf.Max(a.x, b.x) + reach));
                int y0 = Mathf.Max(0, (int)(Mathf.Min(a.y, b.y) - reach)), y1 = Mathf.Min(H - 1, (int)(Mathf.Max(a.y, b.y) + reach));
                Vector2 ab = b - a;
                float len2 = Mathf.Max(1e-4f, ab.sqrMagnitude);
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        int i = y * W + x;
                        if (!land[i]) continue;
                        Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                        Vector2 q = a + ab * t;
                        float d = Vector2.Distance(p, q);
                        // Işık kuzeybatıdan: sırtın güneydoğu yanı daha koyu.
                        Vector2 off = p - q;
                        float side = off.x * 0.6f - off.y * 0.8f > 0f ? 1f : 0.55f;
                        float v = Mathf.Exp(-d / (widths[k] * 1.9f)) * 0.30f * side;
                        v *= 0.75f + 0.5f * Noise(x * 0.08f, y * 0.08f, 55);
                        if (v > r[i]) r[i] = v;
                    }
            }
            return r;
        }

        /// <summary>İki geçişli yaklaşık Öklid uzaklığı (3-4 chamfer); hedef sınıfın dışındaki piksellere.</summary>
        private static float[] Distance(bool[] land, bool insideLand)
        {
            const float big = 1e6f;
            float[] d = new float[W * H];
            for (int i = 0; i < d.Length; i++) d[i] = land[i] == insideLand ? big : 0f;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    if (d[i] == 0f) continue;
                    float v = d[i];
                    if (x > 0) v = Mathf.Min(v, d[i - 1] + 1f);
                    if (y > 0)
                    {
                        v = Mathf.Min(v, d[i - W] + 1f);
                        if (x > 0) v = Mathf.Min(v, d[i - W - 1] + 1.414f);
                        if (x < W - 1) v = Mathf.Min(v, d[i - W + 1] + 1.414f);
                    }
                    d[i] = v;
                }
            for (int y = H - 1; y >= 0; y--)
                for (int x = W - 1; x >= 0; x--)
                {
                    int i = y * W + x;
                    if (d[i] == 0f) continue;
                    float v = d[i];
                    if (x < W - 1) v = Mathf.Min(v, d[i + 1] + 1f);
                    if (y < H - 1)
                    {
                        v = Mathf.Min(v, d[i + W] + 1f);
                        if (x < W - 1) v = Mathf.Min(v, d[i + W + 1] + 1.414f);
                        if (x > 0) v = Mathf.Min(v, d[i + W - 1] + 1.414f);
                    }
                    d[i] = v;
                }
            return d;
        }

        // ================================================================== ayrıntılar

        private struct Ridge { public Vector2[] pts; public float width; }
        private struct River { public Vector2[] pts; }

        /// <summary>Dağ taraması: sırt boyunca, sırta dik kısa, uçlara doğru incelen çizgiler.</summary>
        private static void Hachure(Color32[] px, bool[] land, Ridge r)
        {
            System.Random rng = new System.Random(r.pts.Length * 977 + (int)(r.pts[0].x * 13));
            for (int s = 0; s + 1 < r.pts.Length; s++)
            {
                Vector2 a = Project(r.pts[s].x, r.pts[s].y), b = Project(r.pts[s + 1].x, r.pts[s + 1].y);
                Vector2 dir = (b - a).normalized;
                Vector2 nrm = new Vector2(-dir.y, dir.x);
                float len = Vector2.Distance(a, b);
                for (float t = 0f; t < len; t += 2.6f)
                {
                    float along = (s + t / len) / (r.pts.Length - 1);
                    float taper = Mathf.Sin(Mathf.Clamp01(along) * Mathf.PI) * 0.7f + 0.3f;
                    Vector2 c = a + dir * t;
                    float h = r.width * 1.7f * taper * (0.55f + 0.45f * (float)rng.NextDouble());
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 p0 = c + nrm * side * 1.2f;
                        Vector2 p1 = c + nrm * side * h + dir * (float)(rng.NextDouble() - 0.5) * 1.5f;
                        // Işık kuzeybatıdan: güneydoğu yüzü koyu.
                        float shade = side * nrm.y + side * -nrm.x * 0.5f < 0f ? 0.55f : 0.30f;
                        Stroke(px, land, p0, p1, 0.75f, Ink, shade * taper);
                    }
                }
                // Sırt çizgisi.
                Stroke(px, land, a, b, 0.9f, Ink, 0.5f);
            }
        }

        private static void DrawRiver(Color32[] px, bool[] land, River r)
        {
            // Açık çizgide köşe kesme ve hafif kıvrım: nehir cetvelle çizilmiş gibi durmasın.
            List<Vector2> pts = new List<Vector2>(r.pts);
            for (int iter = 0; iter < 3; iter++)
            {
                List<Vector2> next = new List<Vector2> { pts[0] };
                for (int i = 0; i + 1 < pts.Count; i++)
                {
                    next.Add(Vector2.Lerp(pts[i], pts[i + 1], 0.25f));
                    next.Add(Vector2.Lerp(pts[i], pts[i + 1], 0.75f));
                }
                next.Add(pts[pts.Count - 1]);
                pts = next;
            }
            for (int i = 1; i + 1 < pts.Count; i++)
            {
                Vector2 t = (pts[i + 1] - pts[i - 1]).normalized;
                pts[i] += new Vector2(-t.y, t.x) * (Noise(pts[i].x * 6f, pts[i].y * 6f, 120) - 0.5f) * 0.12f;
            }
            int n = pts.Count;
            for (int s = 0; s + 1 < n; s++)
            {
                Vector2 a = Project(pts[s].x, pts[s].y), b = Project(pts[s + 1].x, pts[s + 1].y);
                float w = Mathf.Lerp(0.9f, 2.1f, s / (float)(n - 1));
                Stroke(px, land, a, b, w, Water, 0.95f);
            }
        }

        private static void Graticule(Color32[] px, bool[] land)
        {
            for (float lon = -10f; lon <= 40f; lon += 5f)
            {
                Vector2 a = Project(lon, MapProjection.LatMin), b = Project(lon, MapProjection.LatMax);
                for (float y = 0f; y < H; y += 1f)
                    if (Mathf.Repeat(y, 9f) < 5f) Put(px, (int)a.x, (int)y, Ink, 0.16f);
            }
            for (float lat = 35f; lat <= 65f; lat += 5f)
            {
                Vector2 a = Project(MapProjection.LonMin, lat);
                for (float x = 0f; x < W; x += 1f)
                    if (Mathf.Repeat(x, 9f) < 5f) Put(px, (int)x, (int)a.y, Ink, 0.16f);
            }
        }

        /// <summary>Çift çerçeve ve aralarında derece bandı: her derece bir siyah-beyaz dilim.</summary>
        private static void Neatline(Color32[] px)
        {
            const int outer = 6, band = 8;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int e = Mathf.Min(Mathf.Min(x, W - 1 - x), Mathf.Min(y, H - 1 - y));
                    if (e < outer) { px[y * W + x] = Blend(Paper, PaperDark, 0.4f); continue; }
                    if (e == outer || e == outer + band) { px[y * W + x] = Blend(px[y * W + x], Ink, 0.9f); continue; }
                    if (e > outer && e < outer + band)
                    {
                        // Derece dilimi: yatay kenarda boylam, dikey kenarda enlem.
                        bool horizontalEdge = Mathf.Min(y, H - 1 - y) == e;
                        float deg = horizontalEdge
                            ? MapProjection.LonMin + x / (float)W * (MapProjection.LonMax - MapProjection.LonMin)
                            : MapProjection.LatMin + y / (float)H * (MapProjection.LatMax - MapProjection.LatMin);
                        bool dark = Mathf.FloorToInt(deg) % 2 == 0;
                        px[y * W + x] = dark ? Blend(Paper, Ink, 0.75f) : Blend(Paper, PaperDark, 0.2f);
                    }
                    else if (e == outer + band + 3) px[y * W + x] = Blend(px[y * W + x], Ink, 0.55f);
                }
        }

        /// <summary>Pusula gülü: dört uzun, dört kısa kol; kuzey kolu pas renginde.</summary>
        private static void Compass(Color32[] px, Vector2 c, float r)
        {
            bool[] none = null;
            RingLine(px, c, r * 0.72f, Ink, 0.6f);
            RingLine(px, c, r * 0.66f, Ink, 0.35f);
            for (int k = 0; k < 8; k++)
            {
                float ang = k * Mathf.PI / 4f + Mathf.PI / 2f;
                bool major = k % 2 == 0;
                float len = major ? r : r * 0.55f;
                float half = major ? r * 0.13f : r * 0.09f;
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                Vector2 side = new Vector2(-dir.y, dir.x);
                Vector2 tip = c + dir * len;
                Color32 dark = k == 0 ? Rust : Ink;
                // Her kolun bir yarısı koyu, öbürü açık: kabartma etkisi.
                Tri(px, c, tip, c + side * half, dark, 0.9f);
                Tri(px, c, tip, c - side * half, Blend(Paper, dark, 0.35f), 0.95f);
                Stroke(px, none, c, tip, 0.6f, Ink, 0.8f);
            }
            Disc(px, c, r * 0.07f, Ink);
            // Kuzey işareti: kuzey kolunun ucunda küçük bir üçgen şapka.
            Vector2 n = c + new Vector2(0f, r * 1.12f);
            Tri(px, n + new Vector2(-r * 0.08f, 0f), n + new Vector2(r * 0.08f, 0f), n + new Vector2(0f, r * 0.16f), Rust, 1f);
        }

        // ================================================================== ilkel çizim

        private static void Put(Color32[] px, int x, int y, Color32 c, float a)
        {
            if ((uint)x >= (uint)W || (uint)y >= (uint)H || a <= 0f) return;
            int i = y * W + x;
            px[i] = Blend(px[i], c, Mathf.Clamp01(a));
        }

        /// <summary>Kenarı yumuşatılmış çizgi; <paramref name="land"/> verilmişse yalnız karada çizer.</summary>
        private static void Stroke(Color32[] px, bool[] land, Vector2 a, Vector2 b, float w, Color32 c, float alpha)
        {
            float r = w * 0.5f + 0.6f;
            int x0 = Mathf.Max(0, (int)(Mathf.Min(a.x, b.x) - r - 1)), x1 = Mathf.Min(W - 1, (int)(Mathf.Max(a.x, b.x) + r + 1));
            int y0 = Mathf.Max(0, (int)(Mathf.Min(a.y, b.y) - r - 1)), y1 = Mathf.Min(H - 1, (int)(Mathf.Max(a.y, b.y) + r + 1));
            Vector2 ab = b - a;
            float len2 = Mathf.Max(1e-4f, ab.sqrMagnitude);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    if (land != null && !land[y * W + x]) continue;
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                    float dist = Vector2.Distance(p, a + ab * t);
                    float cov = Mathf.Clamp01(w * 0.5f + 0.5f - dist);
                    if (cov > 0f) Put(px, x, y, c, alpha * cov);
                }
        }

        private static void Tri(Color32[] px, Vector2 a, Vector2 b, Vector2 c, Color32 col, float alpha)
        {
            int x0 = Mathf.Max(0, (int)Mathf.Min(a.x, Mathf.Min(b.x, c.x))), x1 = Mathf.Min(W - 1, (int)Mathf.Max(a.x, Mathf.Max(b.x, c.x)) + 1);
            int y0 = Mathf.Max(0, (int)Mathf.Min(a.y, Mathf.Min(b.y, c.y))), y1 = Mathf.Min(H - 1, (int)Mathf.Max(a.y, Mathf.Max(b.y, c.y)) + 1);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    int hits = 0;
                    for (int j = 0; j < 2; j++)
                        for (int i = 0; i < 2; i++)
                            if (Inside(new Vector2(x + 0.25f + i * 0.5f, y + 0.25f + j * 0.5f), a, b, c)) hits++;
                    if (hits > 0) Put(px, x, y, col, alpha * hits / 4f);
                }
        }

        private static bool Inside(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
            float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }

        private static void Disc(Color32[] px, Vector2 c, float r, Color32 col)
        {
            for (int y = (int)(c.y - r - 1); y <= (int)(c.y + r + 1); y++)
                for (int x = (int)(c.x - r - 1); x <= (int)(c.x + r + 1); x++)
                    Put(px, x, y, col, Mathf.Clamp01(r + 0.5f - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c)));
        }

        private static void RingLine(Color32[] px, Vector2 c, float r, Color32 col, float alpha)
        {
            for (int y = (int)(c.y - r - 2); y <= (int)(c.y + r + 2); y++)
                for (int x = (int)(c.x - r - 2); x <= (int)(c.x + r + 2); x++)
                {
                    float d = Mathf.Abs(Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) - r);
                    if (d < 1.1f) Put(px, x, y, col, alpha * (1.1f - d));
                }
        }

        private static Color32 Blend(Color32 a, Color32 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32((byte)(a.r + (b.r - a.r) * t), (byte)(a.g + (b.g - a.g) * t), (byte)(a.b + (b.b - a.b) * t), 255);
        }

        private static Color32 Shift(Color32 c, int v)
        {
            return new Color32((byte)Mathf.Clamp(c.r + v, 0, 255), (byte)Mathf.Clamp(c.g + v, 0, 255), (byte)Mathf.Clamp(c.b + v, 0, 255), 255);
        }

        private static float Smooth(float e0, float e1, float v)
        {
            float t = Mathf.Clamp01((v - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }

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
            return Mathf.Lerp(Mathf.Lerp(Hash(xi, yi, s), Hash(xi + 1, yi, s), u), Mathf.Lerp(Hash(xi, yi + 1, s), Hash(xi + 1, yi + 1, s), u), v);
        }

        private static float Fbm(float x, float y, int s, int oct)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int o = 0; o < oct; o++) { sum += amp * Noise(x, y, s + o * 31); norm += amp; amp *= 0.5f; x *= 2.03f; y *= 2.03f; }
            return sum / norm;
        }

        // ================================================================== coğrafya (boylam, enlem)

        private static Vector2[] P(params float[] v)
        {
            Vector2[] pts = new Vector2[v.Length / 2];
            for (int i = 0; i < pts.Length; i++) pts[i] = new Vector2(v[i * 2], v[i * 2 + 1]);
            return pts;
        }

        internal static IEnumerable<Vector2[]> Land()
        {
            // Anakara: kuzeydoğudan başlayıp Beyaz Deniz, İskandinavya, Baltık, Kuzey Denizi,
            // Atlantik, Akdeniz ve Anadolu kıyısı boyunca dolanır; harita dışına taşan köşeler kapatır.
            yield return P(
                46f, 66.5f, 41.5f, 66.2f, 40.6f, 65.0f, 39.6f, 64.6f, 37.6f, 63.9f, 36.2f, 64.3f, 34.9f, 64.5f, 34.7f, 65.3f, 33.8f, 66.2f,
                13.3f, 66.8f, 13.0f, 66.0f, 12.3f, 65.5f, 11.1f, 64.8f, 10.4f, 64.4f, 9.6f, 63.8f, 8.3f, 63.4f, 7.0f, 62.9f, 5.8f, 62.4f,
                5.0f, 61.9f, 4.9f, 61.2f, 5.0f, 60.5f, 5.2f, 59.7f, 5.5f, 59.0f, 5.6f, 58.6f, 6.5f, 58.1f, 7.5f, 58.0f, 8.5f, 58.3f,
                9.5f, 58.9f, 10.3f, 59.1f, 10.6f, 59.8f, 10.8f, 59.2f, 11.2f, 59.0f, 11.6f, 58.6f, 11.2f, 58.3f, 11.8f, 57.7f, 12.4f, 56.9f,
                12.9f, 56.2f, 12.6f, 55.6f, 13.2f, 55.4f, 14.2f, 55.4f, 14.4f, 56.0f, 15.9f, 56.1f, 16.5f, 56.6f, 16.5f, 57.4f, 16.6f, 58.3f,
                17.0f, 58.7f, 18.1f, 59.1f, 18.8f, 59.4f, 18.8f, 60.1f, 17.8f, 60.6f, 17.3f, 61.2f, 17.3f, 62.1f, 18.0f, 62.7f, 19.0f, 63.3f,
                20.3f, 63.7f, 21.4f, 64.4f, 21.5f, 65.0f, 22.3f, 65.8f, 24.2f, 65.8f, 25.3f, 65.1f, 24.6f, 64.8f, 23.5f, 64.0f, 22.3f, 63.4f,
                21.2f, 62.9f, 21.4f, 62.1f, 21.5f, 61.3f, 21.4f, 60.7f, 22.3f, 60.4f, 22.9f, 59.9f, 24.0f, 60.1f, 25.1f, 60.2f, 26.5f, 60.4f,
                27.9f, 60.5f, 28.7f, 60.6f, 29.5f, 60.2f, 30.2f, 59.9f, 29.1f, 59.9f, 28.2f, 59.7f, 27.5f, 59.4f, 25.6f, 59.6f, 24.1f, 59.4f,
                23.5f, 59.2f, 23.4f, 58.6f, 23.9f, 58.3f, 24.4f, 58.3f, 24.3f, 57.8f, 24.3f, 57.2f, 23.8f, 57.0f, 23.0f, 57.1f, 22.6f, 57.7f,
                21.6f, 57.4f, 21.0f, 56.8f, 21.1f, 56.0f, 21.1f, 55.3f, 20.6f, 55.0f, 19.8f, 54.5f, 19.4f, 54.4f, 18.6f, 54.4f, 18.8f, 54.8f,
                17.5f, 54.8f, 16.5f, 54.5f, 15.6f, 54.2f, 14.3f, 53.9f, 13.7f, 54.1f, 13.3f, 54.5f, 12.6f, 54.4f, 11.9f, 54.2f, 11.0f, 53.9f,
                10.8f, 54.3f, 10.0f, 54.5f, 9.9f, 55.0f, 10.4f, 55.5f, 10.2f, 56.1f, 10.6f, 56.5f, 10.3f, 56.9f, 10.6f, 57.7f, 9.9f, 57.5f,
                8.6f, 57.1f, 8.1f, 56.6f, 8.2f, 55.6f, 8.6f, 54.9f, 8.8f, 54.3f, 8.9f, 53.9f, 8.3f, 53.6f, 7.1f, 53.6f, 6.3f, 53.4f,
                5.2f, 53.2f, 4.8f, 52.9f, 4.6f, 52.4f, 4.1f, 51.9f, 3.5f, 51.4f, 2.6f, 51.1f, 1.6f, 50.9f, 1.6f, 50.2f, 1.0f, 49.9f,
                0.2f, 49.6f, -0.3f, 49.3f, -1.3f, 49.4f, -1.9f, 49.7f, -1.6f, 48.7f, -2.7f, 48.5f, -3.6f, 48.8f, -4.6f, 48.5f, -4.4f, 48.0f,
                -3.2f, 47.6f, -2.2f, 47.2f, -1.6f, 46.4f, -1.2f, 45.7f, -1.2f, 44.7f, -1.4f, 44.0f, -1.8f, 43.4f, -2.9f, 43.4f, -4.5f, 43.4f,
                -6.0f, 43.6f, -7.7f, 43.7f, -8.9f, 43.3f, -9.3f, 42.9f, -8.9f, 42.1f, -8.8f, 41.3f, -9.0f, 40.5f, -9.4f, 39.4f, -9.4f, 38.7f,
                -8.9f, 38.5f, -8.8f, 37.9f, -9.0f, 37.0f, -8.0f, 37.0f, -7.3f, 37.2f, -6.4f, 36.8f, -6.0f, 36.3f, -5.6f, 36.0f, -5.3f, 36.2f,
                -4.4f, 36.7f, -3.0f, 36.8f, -2.1f, 36.7f, -1.6f, 37.3f, -0.7f, 37.6f, -0.5f, 38.3f, 0.2f, 38.8f, -0.3f, 39.5f, 0.0f, 40.0f,
                0.9f, 40.7f, 1.0f, 41.1f, 2.2f, 41.4f, 3.2f, 41.9f, 3.2f, 42.4f, 3.0f, 43.0f, 3.6f, 43.4f, 4.6f, 43.4f, 5.4f, 43.2f,
                6.4f, 43.1f, 7.0f, 43.5f, 7.6f, 43.8f, 8.3f, 44.1f, 8.9f, 44.4f, 9.8f, 44.1f, 10.3f, 43.5f, 10.5f, 42.9f, 11.1f, 42.5f,
                12.0f, 41.9f, 12.8f, 41.3f, 13.7f, 41.2f, 14.2f, 40.8f, 14.5f, 40.6f, 15.0f, 40.2f, 15.6f, 40.0f, 15.8f, 39.6f, 16.0f, 38.8f,
                15.7f, 38.2f, 15.9f, 37.95f, 16.1f, 38.0f, 16.6f, 38.4f, 16.5f, 38.8f, 17.1f, 39.0f, 17.1f, 39.4f, 16.5f, 39.7f, 16.9f, 40.4f,
                17.5f, 40.3f, 18.4f, 40.1f, 18.5f, 40.4f, 17.9f, 40.7f, 17.0f, 41.1f, 16.0f, 41.4f, 15.9f, 41.9f, 15.1f, 41.95f, 14.2f, 42.4f,
                13.6f, 43.3f, 12.6f, 44.0f, 12.3f, 44.6f, 12.3f, 45.3f, 13.0f, 45.7f, 13.7f, 45.6f, 13.6f, 45.1f, 13.9f, 44.8f, 14.3f, 45.3f,
                14.9f, 45.1f, 15.2f, 44.3f, 15.9f, 43.6f, 16.5f, 43.5f, 17.4f, 43.0f, 18.0f, 42.7f, 18.6f, 42.4f, 19.1f, 42.0f, 19.4f, 41.9f,
                19.5f, 41.3f, 19.4f, 40.5f, 19.9f, 40.0f, 20.1f, 39.6f, 20.7f, 39.0f, 21.1f, 38.4f, 21.7f, 38.3f, 22.3f, 38.1f, 21.3f, 37.7f,
                21.7f, 36.9f, 22.4f, 36.4f, 22.8f, 36.8f, 23.2f, 36.5f, 22.9f, 37.5f, 23.4f, 37.9f, 24.0f, 37.7f, 24.0f, 38.2f, 23.2f, 38.7f,
                22.8f, 39.3f, 23.1f, 39.6f, 22.6f, 40.0f, 22.6f, 40.6f, 23.4f, 40.2f, 24.0f, 40.4f, 24.4f, 40.9f, 25.4f, 40.9f, 26.1f, 40.8f,
                26.7f, 40.4f, 26.2f, 40.0f, 26.8f, 39.4f, 26.8f, 38.7f, 26.3f, 38.4f, 27.2f, 37.9f, 27.3f, 37.1f, 28.0f, 36.8f, 29.1f, 36.6f,
                30.5f, 36.3f, 31.5f, 36.7f, 32.8f, 36.1f, 34.0f, 36.3f, 35.0f, 36.8f, 36.2f, 36.6f, 36.0f, 35.8f, 35.9f, 34.0f, 46f, 34.0f);

            // Britanya ve İrlanda.
            yield return P(-5.7f, 50.1f, -5.0f, 50.0f, -3.5f, 50.3f, -2.0f, 50.6f, -0.8f, 50.8f, 0.4f, 50.8f, 1.4f, 51.2f, 1.0f, 51.8f,
                1.7f, 52.6f, 0.4f, 52.9f, 0.2f, 53.5f, -0.4f, 54.3f, -1.2f, 54.6f, -1.6f, 55.6f, -2.1f, 56.0f, -2.8f, 56.1f, -2.6f, 56.5f,
                -1.8f, 57.5f, -3.3f, 57.7f, -4.1f, 57.6f, -3.0f, 58.6f, -5.0f, 58.6f, -5.3f, 58.1f, -5.8f, 57.3f, -5.7f, 56.6f, -5.6f, 56.0f,
                -5.3f, 55.7f, -4.9f, 55.0f, -5.1f, 54.8f, -3.4f, 54.9f, -3.0f, 54.2f, -3.4f, 53.4f, -4.6f, 53.3f, -4.2f, 52.9f, -4.1f, 52.3f,
                -5.2f, 51.8f, -4.2f, 51.5f, -3.0f, 51.5f, -3.4f, 51.2f, -4.6f, 51.0f);
            yield return P(-6.3f, 52.2f, -7.5f, 52.1f, -9.3f, 51.6f, -10.2f, 51.9f, -9.8f, 52.6f, -9.4f, 53.2f, -10.1f, 53.5f, -9.7f, 54.1f,
                -8.2f, 54.5f, -8.4f, 55.1f, -7.2f, 55.3f, -5.9f, 54.8f, -5.9f, 54.2f, -6.2f, 53.3f, -6.0f, 52.6f);
            // Kuzey Afrika kıyısı.
            yield return P(-12f, 34.0f, -6.9f, 34.0f, -6.3f, 35.0f, -5.8f, 35.8f, -5.3f, 35.9f, -4.0f, 35.2f, -2.9f, 35.3f, -1.3f, 35.3f,
                0.0f, 35.9f, 1.5f, 36.6f, 3.0f, 36.8f, 5.0f, 36.8f, 6.5f, 37.1f, 8.0f, 36.9f, 9.8f, 37.3f, 10.3f, 36.8f, 11.1f, 37.0f,
                10.6f, 36.4f, 10.9f, 35.6f, 11.1f, 35.2f, 11.2f, 34.0f);
            // Adalar.
            yield return P(11.1f, 55.2f, 12.2f, 55.4f, 12.6f, 55.7f, 12.3f, 56.1f, 11.7f, 55.95f, 11.1f, 55.7f);          // Sjælland
            yield return P(9.7f, 55.5f, 10.4f, 55.6f, 10.8f, 55.3f, 10.6f, 55.0f, 9.9f, 55.1f);                        // Fyn
            yield return P(18.2f, 57.0f, 18.9f, 57.4f, 19.1f, 57.9f, 18.6f, 57.8f, 18.1f, 57.5f);                       // Gotland
            yield return P(16.4f, 56.2f, 16.9f, 56.9f, 16.7f, 57.1f, 16.4f, 56.6f);                                     // Öland
            yield return P(21.8f, 58.3f, 22.5f, 58.2f, 23.2f, 58.6f, 22.2f, 58.6f, 21.9f, 58.5f);                       // Saaremaa
            yield return P(14.7f, 55.1f, 15.1f, 55.0f, 15.2f, 55.3f, 14.8f, 55.3f);                                     // Bornholm
            yield return P(8.6f, 41.4f, 9.3f, 41.4f, 9.5f, 42.2f, 9.4f, 43.0f, 8.7f, 42.6f, 8.6f, 41.9f);               // Korsika
            yield return P(8.4f, 39.0f, 9.0f, 38.9f, 9.6f, 39.2f, 9.8f, 40.5f, 9.3f, 41.2f, 8.4f, 40.9f, 8.2f, 40.2f, 8.5f, 39.6f); // Sardinya
            yield return P(12.4f, 37.8f, 13.3f, 37.1f, 14.3f, 37.0f, 15.1f, 36.7f, 15.3f, 37.5f, 15.6f, 38.3f, 14.6f, 38.0f, 13.4f, 38.2f, 12.4f, 38.1f); // Sicilya
            yield return P(23.5f, 35.3f, 24.3f, 35.4f, 25.5f, 35.3f, 26.3f, 35.2f, 26.1f, 35.0f, 24.8f, 34.9f, 23.6f, 35.2f); // Girit
            yield return P(32.3f, 35.0f, 33.0f, 35.4f, 34.6f, 35.7f, 34.0f, 35.0f, 33.0f, 34.6f, 32.4f, 34.8f);          // Kıbrıs
            yield return P(2.4f, 39.5f, 3.1f, 39.3f, 3.4f, 39.7f, 3.1f, 39.95f, 2.4f, 39.6f);                          // Mallorca
            yield return P(27.8f, 36.0f, 28.2f, 36.2f, 28.1f, 36.45f, 27.7f, 36.2f);                                   // Rodos
            yield return P(26.0f, 39.0f, 26.6f, 39.0f, 26.4f, 39.4f, 25.9f, 39.3f);                                    // Midilli
            yield return P(-5.6f, 56.8f, -6.4f, 57.1f, -6.1f, 57.6f, -5.7f, 57.3f);                                    // Skye
            yield return P(-6.2f, 58.0f, -6.9f, 57.8f, -7.4f, 57.4f, -6.6f, 58.5f);                                    // Hebridler
        }

        internal static IEnumerable<Vector2[]> Waters()
        {
            // Karadeniz ve Azak.
            yield return P(28.0f, 41.6f, 29.1f, 41.2f, 31.3f, 41.1f, 33.3f, 42.0f, 35.2f, 42.0f, 36.8f, 41.4f, 38.4f, 40.9f, 40.2f, 41.0f,
                41.5f, 41.5f, 41.7f, 42.7f, 40.3f, 43.1f, 38.8f, 44.3f, 37.4f, 44.9f, 36.6f, 45.2f, 35.4f, 45.0f, 33.9f, 44.4f, 33.4f, 44.6f,
                33.6f, 45.1f, 32.5f, 45.4f, 33.6f, 46.0f, 31.8f, 46.3f, 30.8f, 46.5f, 30.2f, 45.8f, 29.7f, 45.3f, 29.6f, 44.8f, 28.8f, 44.5f,
                28.6f, 43.7f, 28.0f, 43.2f, 27.9f, 42.5f);
            yield return P(35.0f, 45.4f, 36.6f, 45.35f, 38.2f, 46.2f, 39.3f, 47.2f, 38.2f, 47.1f, 37.3f, 47.0f, 35.8f, 46.6f, 35.2f, 46.2f, 34.8f, 45.8f);
            yield return P(26.7f, 40.4f, 27.5f, 40.3f, 29.0f, 40.4f, 29.1f, 40.95f, 28.0f, 41.0f, 27.0f, 40.6f);         // Marmara
            // Göller.
            yield return P(30.5f, 59.95f, 31.4f, 60.1f, 32.6f, 60.5f, 32.9f, 61.1f, 32.3f, 61.3f, 31.4f, 61.6f, 30.6f, 61.7f, 30.0f, 61.2f, 29.9f, 60.6f, 30.3f, 60.2f); // Ladoga
            yield return P(34.5f, 61.1f, 35.7f, 61.3f, 36.4f, 61.8f, 35.9f, 62.6f, 35.3f, 62.9f, 34.8f, 62.4f, 34.9f, 61.8f, 34.4f, 61.5f); // Onega
            yield return P(12.4f, 58.4f, 13.2f, 58.6f, 14.0f, 59.1f, 13.2f, 59.3f, 12.5f, 59.2f, 12.4f, 58.8f);          // Vänern
            yield return P(14.1f, 57.8f, 14.7f, 58.3f, 14.9f, 58.8f, 14.5f, 58.9f, 14.2f, 58.4f);                        // Vättern
            yield return P(27.2f, 57.9f, 27.8f, 58.2f, 27.8f, 58.9f, 27.2f, 59.0f, 27.0f, 58.5f);                        // Peipus
            yield return P(17.2f, 46.7f, 17.9f, 46.9f, 18.1f, 47.05f, 17.4f, 46.85f);                                    // Balaton
            yield return P(6.2f, 46.2f, 6.9f, 46.35f, 6.8f, 46.5f, 6.4f, 46.45f);                                        // Cenevre
            yield return P(26.5f, 61.3f, 28.2f, 61.4f, 29.0f, 61.9f, 28.2f, 62.1f, 27.0f, 61.8f);                        // Saimaa
        }

        private static IEnumerable<Ridge> Ridges()
        {
            yield return new Ridge { width = 7f, pts = P(6.8f, 44.2f, 7.0f, 45.2f, 6.9f, 46.0f, 8.0f, 46.5f, 9.5f, 46.5f, 10.8f, 47.0f, 12.5f, 47.1f, 14.0f, 47.3f, 15.8f, 47.6f) };
            yield return new Ridge { width = 5f, pts = P(-1.7f, 43.1f, 0.0f, 42.8f, 1.5f, 42.6f, 2.8f, 42.4f) };
            yield return new Ridge { width = 5.5f, pts = P(17.5f, 48.9f, 19.0f, 49.3f, 21.0f, 49.3f, 23.0f, 48.8f, 24.5f, 47.9f, 25.4f, 47.2f, 25.8f, 46.4f, 26.1f, 45.6f, 25.0f, 45.4f, 23.0f, 45.4f, 22.2f, 45.0f) };
            yield return new Ridge { width = 5.5f, pts = P(14.8f, 45.5f, 15.8f, 44.6f, 17.0f, 43.9f, 18.3f, 43.3f, 19.3f, 42.7f, 20.2f, 41.8f) };
            yield return new Ridge { width = 4f, pts = P(22.6f, 43.5f, 24.0f, 42.8f, 26.0f, 42.7f) };
            yield return new Ridge { width = 4.5f, pts = P(8.5f, 44.4f, 10.5f, 44.2f, 12.5f, 43.3f, 13.8f, 42.2f, 15.0f, 41.2f, 15.8f, 40.3f, 16.1f, 39.1f) };
            yield return new Ridge { width = 5.5f, pts = P(14.2f, 66.5f, 13.5f, 64.0f, 12.5f, 63.0f, 10.5f, 62.3f, 8.3f, 61.6f, 7.2f, 60.5f, 7.2f, 59.5f) };
            yield return new Ridge { width = 4f, pts = P(20.9f, 40.6f, 21.3f, 39.6f, 21.8f, 38.9f) };
            yield return new Ridge { width = 6f, pts = P(37.5f, 44.5f, 40.0f, 43.5f, 42.5f, 42.6f) };
            yield return new Ridge { width = 3.5f, pts = P(-7.0f, 43.0f, -5.0f, 43.1f, -3.5f, 43.1f) };
            yield return new Ridge { width = 3.5f, pts = P(-5.0f, 57.0f, -4.0f, 57.2f) };
            yield return new Ridge { width = 5f, pts = P(29.5f, 37.0f, 31.5f, 37.3f, 33.5f, 37.2f, 35.5f, 37.6f) };
            yield return new Ridge { width = 3.5f, pts = P(-4.0f, 37.1f, -2.8f, 37.1f) };
            yield return new Ridge { width = 3.5f, pts = P(2.5f, 45.8f, 2.9f, 45.1f) };
        }

        private static IEnumerable<River> Rivers()
        {
            yield return new River { pts = P(9.5f, 47.5f, 8.6f, 47.6f, 7.6f, 47.6f, 7.8f, 48.6f, 8.4f, 49.3f, 8.3f, 50.0f, 7.6f, 50.4f, 7.0f, 50.9f, 6.8f, 51.4f, 6.2f, 51.8f, 5.2f, 51.9f, 4.3f, 51.9f) };
            yield return new River { pts = P(15.5f, 50.7f, 15.0f, 50.1f, 14.3f, 50.2f, 14.0f, 50.7f, 13.2f, 51.1f, 12.3f, 51.8f, 11.8f, 52.2f, 11.4f, 52.9f, 10.5f, 53.4f, 9.9f, 53.55f, 8.8f, 53.9f) };
            yield return new River { pts = P(17.7f, 49.7f, 17.5f, 50.6f, 16.9f, 51.1f, 15.6f, 51.8f, 14.6f, 52.4f, 14.4f, 53.4f) };
            yield return new River { pts = P(18.9f, 49.6f, 19.9f, 50.0f, 21.7f, 50.5f, 21.2f, 51.4f, 21.0f, 52.2f, 19.9f, 52.6f, 18.6f, 53.1f, 18.8f, 54.3f) };
            yield return new River { pts = P(8.5f, 47.9f, 10.0f, 48.4f, 11.4f, 48.8f, 13.4f, 48.6f, 14.3f, 48.3f, 16.4f, 48.2f, 17.1f, 48.1f, 18.8f, 47.8f, 19.0f, 47.5f, 18.9f, 46.0f, 19.4f, 45.2f, 20.4f, 44.8f, 21.5f, 44.7f, 22.6f, 44.6f, 22.9f, 43.8f, 24.0f, 43.7f, 25.6f, 43.6f, 27.3f, 44.1f, 28.0f, 44.9f, 28.7f, 45.2f, 29.7f, 45.2f) };
            yield return new River { pts = P(4.4f, 48.1f, 3.9f, 48.6f, 2.4f, 48.8f, 1.5f, 49.1f, 0.2f, 49.45f) };
            yield return new River { pts = P(4.0f, 45.8f, 3.1f, 47.0f, 2.2f, 47.8f, 1.2f, 47.4f, 0.0f, 47.3f, -1.5f, 47.2f, -2.1f, 47.3f) };
            yield return new River { pts = P(6.5f, 46.4f, 5.8f, 45.9f, 4.8f, 45.7f, 4.8f, 44.9f, 4.7f, 43.8f, 4.6f, 43.4f) };
            yield return new River { pts = P(7.6f, 45.0f, 9.1f, 45.1f, 10.6f, 45.0f, 11.9f, 45.0f, 12.4f, 44.95f) };
            yield return new River { pts = P(32.0f, 55.8f, 30.5f, 54.5f, 30.3f, 53.2f, 30.5f, 51.5f, 30.5f, 50.4f, 31.5f, 49.4f, 33.4f, 49.0f, 34.9f, 48.4f, 35.2f, 47.8f, 34.1f, 46.9f, 33.0f, 46.7f, 32.1f, 46.6f) };
            yield return new River { pts = P(18.4f, 43.6f, 18.1f, 43.5f, 17.9f, 43.3f, 17.6f, 43.1f, 17.45f, 43.0f) };
            yield return new River { pts = P(28.5f, 55.6f, 27.5f, 55.8f, 26.4f, 56.2f, 25.2f, 56.6f, 24.1f, 56.97f) };
            yield return new River { pts = P(-2.0f, 40.4f, -3.6f, 40.0f, -5.0f, 39.8f, -6.9f, 39.6f, -8.5f, 39.4f, -9.1f, 38.8f) };
            yield return new River { pts = P(-4.0f, 43.0f, -2.5f, 42.5f, -1.2f, 41.8f, 0.3f, 41.2f, 0.9f, 40.7f) };
            yield return new River { pts = P(33.0f, 57.2f, 35.9f, 56.9f, 37.4f, 57.1f, 39.9f, 57.7f, 41.0f, 57.5f, 42.5f, 56.3f) };
            yield return new River { pts = P(32.3f, 60.9f, 31.5f, 61.0f) };                        // Svir (Onega–Ladoga, kısaltılmış)
            yield return new River { pts = P(30.9f, 59.95f, 30.3f, 59.9f) };                       // Neva
            yield return new River { pts = P(28.8f, 61.3f, 29.6f, 61.0f, 29.95f, 60.8f) };         // Vuoksi
        }
    }
}
