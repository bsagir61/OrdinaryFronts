using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OrdinaryFronts
{
    public sealed partial class AppController
    {
        /// <summary>
        /// Sığınak merdiveni (Hamburg): karar sahnesi. Elektrik kesik; sağ üstte sahanlık ve
        /// sönük acil durum lambası, sola aşağı inen taş merdiven, basamaklarda bekleyen
        /// insanlar. İki iş aynı anda açıktır ve oyuncu hangisini yaptığını seçmez, yapar:
        /// <list type="bullet">
        /// <item><b>Lamba</b> — A/← ya da sağ fare basılı: kabloyu bağlamak. Işık kesik kesik
        /// güçlenir, iki kez kıvılcım atar; sonunda yanar ve sıra kendi kendine iner.</item>
        /// <item><b>Sıra</b> — D/→/boşluk/sol fare: karanlık basamağın kenarında duraksayan
        /// kişinin elini tutup indirmek. Dördüncü kişi indiğinde gerisi elden ele gelir.</item>
        /// </list>
        /// Hangisi önce tamamlanırsa düğümün o seçimi verilir. Süre dolarsa (bir dakika) daha
        /// çok ilerleyen iş sayılır; kaybetmek yoktur.
        /// </summary>
        private sealed class LampInterlude : InterludeScene
        {
            private const float RepairSeconds = 5.5f;
            private const int GuideTarget = 4;
            private const float TimeLimit = 60f;
            private const float StairTopX = 1560f, StairTopY = 600f, StairBottomX = 406f, StairBottomY = 232f;
            private const float EdgeS = 0.30f;

            private readonly List<Person> people = new List<Person>();
            private Image darkness;
            private Image glow;
            private Image bulb;
            private RectTransform lampRect;

            private float repair;
            private float sparkPause;
            private int sparksFired;
            private int guided;
            private int arrived;
            private float lampLevel;
            private float flicker;
            private float doneAt = -1f;
            private int decided = -1;
            private float queueGap;

            private sealed class Person
            {
                public Image image;
                public RectTransform rect;
                public string[] frames;
                public float s;          // merdiven boyunca konum, 0 sahanlık - 1 zemin
                public float landingX;   // sahanlıktaysa x
                public bool onLanding;
                public bool descending;
                public bool released;    // lamba yandı, kendi iniyor
                public float phase;
                public float hesitate;
            }

            public LampInterlude(AppController app, StoryNode node) : base(app, node) { }

            protected override string HintText { get { return app.T(UiKey.InterludeLampHint); } }
            protected override bool Finished { get { return doneAt >= 0f && elapsed >= doneAt; } }

            protected override void Build()
            {
                Color wall = Color.Lerp(theme.sootNavy, theme.ink, 0.35f);
                Color stone = Color.Lerp(theme.ink, theme.agedPaper, 0.10f);
                Color tread = new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.30f);

                Fill("Wall", Vector2.zero, Vector2.one, wall);
                // Sıva izleri: duvarda seyrek yatay çizgiler.
                for (int i = 0; i < 14; i++)
                    Strip("Plaster", root.transform, S("il_streak"), new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.05f + 0.04f * Rand01(i)),
                        new Vector2(Rand01(i * 5) * 1920f, 300f + Rand01(i * 9) * 700f), new Vector2(300f + Rand01(i * 3) * 700f, 6f + Rand01(i * 7) * 10f));

                // Sahanlık ve merdiven: her basamak zemine kadar dolu bir blok, üstünde açık bir kenar.
                Strip("Landing", root.transform, null, stone, new Vector2((StairTopX + 1920f) * 0.5f, StairTopY * 0.5f), new Vector2(1920f - StairTopX + 40f, StairTopY));
                Strip("Landing Edge", root.transform, null, tread, new Vector2((StairTopX + 1920f) * 0.5f, StairTopY - 1f), new Vector2(1920f - StairTopX + 40f, 3f));
                const int steps = 9;
                for (int i = 0; i < steps; i++)
                {
                    float t0 = i / (float)steps;
                    float t1 = (i + 1) / (float)steps;
                    float x0 = Mathf.Lerp(StairTopX, StairBottomX, t1);
                    float x1 = Mathf.Lerp(StairTopX, StairBottomX, t0);
                    float top = Mathf.Lerp(StairTopY, StairBottomY, t0);
                    Strip("Step", root.transform, null, Color.Lerp(stone, theme.sootNavy, 0.06f * i), new Vector2((x0 + x1) * 0.5f, top * 0.5f), new Vector2(x1 - x0 + 2f, top));
                    Strip("Tread", root.transform, null, tread, new Vector2((x0 + x1) * 0.5f, top - 1f), new Vector2(x1 - x0, 3f));
                }
                Strip("Floor", root.transform, null, Color.Lerp(stone, theme.sootNavy, 0.5f), new Vector2(StairBottomX * 0.5f, StairBottomY * 0.5f), new Vector2(StairBottomX, StairBottomY));
                // Korkuluk: merdivene paralel ince çizgi.
                RectTransform rail = Strip("Rail", root.transform, null, Color.Lerp(theme.ink, theme.agedPaper, 0.25f),
                    new Vector2((StairTopX + StairBottomX) * 0.5f, (StairTopY + StairBottomY) * 0.5f + 150f), new Vector2(Vector2.Distance(new Vector2(StairTopX, StairTopY), new Vector2(StairBottomX, StairBottomY)) + 60f, 5f));
                rail.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(StairTopY - StairBottomY, StairTopX - StairBottomX) * Mathf.Rad2Deg);

                // Karanlık ve ışık halesi taş ve duvarın üstüne; lamba ve insanlar onların üstüne.
                // Böylece silüetler keskin kalır, ışık yalnız zemini boyar.
                darkness = Fill("Darkness", Vector2.zero, Vector2.one, new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.50f));
                glow = Strip("Glow", root.transform, S("il_puff"), new Color(theme.mustard.r, theme.mustard.g, theme.mustard.b, 0f), new Vector2(1700f, 760f), new Vector2(1100f, 1100f)).GetComponent<Image>();
                lampRect = Strip("Lamp", root.transform, S("il_lamp"), Color.white, new Vector2(1700f, 830f), new Vector2(120f, 150f));
                bulb = Strip("Bulb", root.transform, theme.mapMarker, new Color(theme.mustard.r, theme.mustard.g, theme.mustard.b, 0f), new Vector2(1700f, 790f), new Vector2(22f, 22f)).GetComponent<Image>();

                // Bekleyenler: basamak kenarında bir, arkasında iki, sahanlıkta üç.
                string[][] kinds =
                {
                    new[] { "il_woman_0", "il_woman_1", "il_woman_2", "il_woman_3" },
                    new[] { "il_man_0", "il_man_1", "il_man_2", "il_man_3" },
                    new[] { "il_child_0", "il_child_1", "il_child_2", "il_child_3" },
                    new[] { "il_greet_0", "il_greet_1", "il_greet_2", "il_greet_3" },
                    new[] { "il_woman_0", "il_woman_1", "il_woman_2", "il_woman_3" },
                    new[] { "il_man_0", "il_man_1", "il_man_2", "il_man_3" },
                    new[] { "il_child_0", "il_child_1", "il_child_2", "il_child_3" },
                };
                float[] s = { EdgeS, 0.16f, 0.03f };
                for (int i = 0; i < kinds.Length; i++)
                {
                    Person p = new Person { frames = kinds[i], phase = i * 0.7f };
                    p.onLanding = i >= s.Length;
                    p.s = p.onLanding ? 0f : s[i];
                    p.landingX = StairTopX + 90f + (i - s.Length) * 110f;
                    float scale = kinds[i][0].StartsWith("il_child") ? 0.46f : 0.60f;
                    p.image = Figure("Person", root.transform, S(kinds[i][1]), Vector2.zero, new Vector2(240f * scale, 360f * scale), true);
                    p.rect = p.image.rectTransform;
                    Place(p);
                    people.Add(p);
                }

                // Ön plan: Matthias'ın gölgesi kadraja soldan alttan girer; oyuncunun kendisi.
                Fill("Foreground Shadow", Vector2.zero, new Vector2(0.12f, 0.55f), new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.65f));
            }

            private static Vector2 StairPoint(float s)
            {
                return new Vector2(Mathf.Lerp(StairTopX, StairBottomX, s), Mathf.Lerp(StairTopY, StairBottomY, s));
            }

            private void Place(Person p)
            {
                Vector2 foot = p.onLanding ? new Vector2(p.landingX, StairTopY) : StairPoint(p.s);
                float bob = p.descending ? Mathf.Abs(Mathf.Sin(p.phase * Mathf.PI * 0.5f)) * 4f : (p.hesitate > 0f ? Mathf.Sin(elapsed * 9f) * 2f : 0f);
                p.rect.anchoredPosition = foot + new Vector2(0f, bob);
            }

            protected override void Step(float dt)
            {
                bool repairing = decided < 0 && (app.InterludeSecondaryHeld || app.interludeForcePush);
                bool guidePressed = decided < 0 && app.InterludePrimaryPressed;

                // --- lamba
                if (repairing && repair < 1f)
                {
                    if (sparkPause > 0f) sparkPause -= dt;
                    else repair = Mathf.Min(1f, repair + dt / RepairSeconds);
                    int sparkStage = repair >= 0.66f ? 2 : (repair >= 0.33f ? 1 : 0);
                    if (sparkStage > sparksFired && repair < 1f)
                    {
                        sparksFired = sparkStage;
                        sparkPause = 0.55f;
                        flicker = 1f;
                        app.audioManager.PlaySpark();
                    }
                }
                flicker = Mathf.MoveTowards(flicker, 0f, dt * 2.2f);
                float target = repair >= 1f ? 1f : repair * (0.55f + 0.45f * Mathf.PerlinNoise(elapsed * 6f, 0.3f)) * (1f - flicker * 0.9f);
                if (repair < 1f && Mathf.PerlinNoise(elapsed * 3f, 7f) > 0.82f) target *= 0.2f;
                lampLevel = Mathf.MoveTowards(lampLevel, target, dt * (repair >= 1f ? 1.5f : 5f));
                glow.color = new Color(theme.mustard.r, theme.mustard.g, theme.mustard.b, 0.06f + 0.42f * lampLevel);
                glow.rectTransform.sizeDelta = Vector2.one * (1100f + 900f * lampLevel);
                bulb.color = new Color(theme.mustard.r, theme.mustard.g, theme.mustard.b, 0.15f + 0.85f * lampLevel);
                darkness.color = new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, Mathf.Lerp(0.50f, 0.06f, lampLevel));
                lampRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 1.7f) * 1.5f);

                if (repair >= 1f && decided < 0)
                {
                    decided = 0;
                    for (int i = 0; i < people.Count; i++) people[i].released = true;
                    ShowCentre(app.T(UiKey.InterludeLampLit), 3f);
                    doneAt = elapsed + 4.2f;
                }

                // --- sıra
                Person atEdge = null;
                for (int i = 0; i < people.Count; i++)
                    if (!people[i].descending && !people[i].onLanding && Mathf.Abs(people[i].s - EdgeS) < 0.01f) atEdge = people[i];
                if (atEdge != null) atEdge.hesitate = 1f;
                if (guidePressed && atEdge != null)
                {
                    atEdge.descending = true;
                    atEdge.hesitate = 0f;
                    guided++;
                    queueGap = 1.1f;
                    if (guided >= GuideTarget && decided < 0)
                    {
                        decided = 1;
                        ShowCentre(app.T(UiKey.InterludeStairsDone), 3f);
                        doneAt = elapsed + 3.6f;
                    }
                }
                if (queueGap > 0f) queueGap -= dt;

                // Sıradakiler kenara doğru kayar; sahanlıktakiler merdivene geçer.
                bool edgeFree = atEdge == null && queueGap <= 0f;
                if (edgeFree)
                {
                    Person next = null;
                    for (int i = 0; i < people.Count; i++)
                    {
                        Person p = people[i];
                        if (p.descending) continue;
                        if (!p.onLanding && (next == null || p.s > next.s)) next = p;
                    }
                    if (next == null)
                        for (int i = 0; i < people.Count; i++)
                            if (people[i].onLanding && !people[i].descending && (next == null || people[i].landingX < next.landingX)) next = people[i];
                    if (next != null)
                    {
                        if (next.onLanding)
                        {
                            next.landingX -= 140f * dt;
                            if (next.landingX <= StairTopX + 6f) { next.onLanding = false; next.s = 0f; }
                        }
                        else next.s = Mathf.MoveTowards(next.s, EdgeS, 0.16f * dt);
                        next.phase += 5f * dt;
                    }
                }

                for (int i = 0; i < people.Count; i++)
                {
                    Person p = people[i];
                    bool moving = p.descending || p.released;
                    if (p.released && !p.descending)
                    {
                        // Lamba yandı: sahanlıktakiler merdivene yürür, basamaktakiler iner.
                        if (p.onLanding) { p.landingX -= 160f * dt; if (p.landingX <= StairTopX + 6f) { p.onLanding = false; p.s = 0f; } }
                        else p.descending = true;
                    }
                    if (p.descending)
                    {
                        p.s += 0.22f * dt;
                        if (p.s >= 1f) { p.s = 1f; p.descending = false; p.released = false; arrived++; p.image.enabled = false; }
                    }
                    if (moving) p.phase += 6f * dt;
                    Animate(p.image, p.frames, p.phase, moving);
                    Place(p);
                }

                if (decided < 0 && elapsed >= TimeLimit)
                {
                    decided = repair >= guided / (float)GuideTarget ? 0 : 1;
                    doneAt = elapsed + 1.5f;
                }
            }

            protected override void Resolve()
            {
                ChoiceIndex = decided < 0 ? (repair >= guided / (float)GuideTarget ? 0 : 1) : decided;
                if (data.results != null && data.results.Length > ChoiceIndex) Result = data.results[ChoiceIndex];
            }
        }
    }
}
