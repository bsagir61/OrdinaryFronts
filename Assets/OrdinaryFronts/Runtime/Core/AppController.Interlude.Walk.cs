using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OrdinaryFronts
{
    public sealed partial class AppController
    {
        /// <summary>
        /// Rüzgâra karşı yürüyüş (Afsluitdijk). Yandan görünüş: gök, IJsselmeer, setin şevi,
        /// yol. Grup yerinde yürür, dünya sola akar, kilometre taşları geçer. Bora geldiğinde
        /// kar şeritleri çoğalır, figürler öne eğilir ve hız düşer; oyuncu bırakırsa grup
        /// durur ve bekler. Sonuç: her borada iten "yürüdü", herhangi birinde bekleyen
        /// "bekledi".
        /// </summary>
        private sealed class WalkInterlude : InterludeScene
        {
            private const string ResultPushed = "il_wind_pushed";
            private const string ResultWaited = "il_wind_waited";
            private const float PixelsPerKm = 2400f;
            private const float BaseSpeed = 130f;
            private const float GustSpeedFactor = 0.42f;
            private const float GustDuration = 3.6f;
            private const float StartKm = 10.5f;
            private const float TargetDistanceKm = 1.55f;
            private static readonly float[] GustAtKm = { 0.22f, 0.58f, 0.94f, 1.28f };

            private readonly List<RectTransform> cloudStreaks = new List<RectTransform>();
            private readonly List<RectTransform> seaStreaks = new List<RectTransform>();
            private readonly List<Snow> snow = new List<Snow>();
            private readonly List<RectTransform> posts = new List<RectTransform>();
            private readonly List<Walker> walkers = new List<Walker>();
            private RectTransform group;

            private float distanceKm;
            private int nextGust;
            private float gustRemaining;
            private float gustPushedSeconds;
            private float gustReleasedSeconds;
            private bool anyGustWaited;
            private bool everyGustPushed = true;
            private int gustsSeen;

            private sealed class Snow { public RectTransform rect; public Image image; public float speed; public float depth; }
            private sealed class Walker { public Image image; public RectTransform rect; public string[] frames; public float phase; public Vector2 origin; }

            public WalkInterlude(AppController app, StoryNode node) : base(app, node) { }

            protected override string HintText { get { return app.T(UiKey.InterludePushHint); } }
            protected override bool Finished { get { return distanceKm >= TargetDistanceKm; } }

            protected override void Build()
            {
                Color sky = Color.Lerp(theme.agedPaper, theme.petrol, 0.22f);
                Color skyHigh = Color.Lerp(theme.petrol, theme.sootNavy, 0.35f);
                Color sea = Color.Lerp(theme.agedPaper, theme.petrol, 0.42f);
                Color slope = Color.Lerp(theme.ink, theme.petrol, 0.32f);
                Color road = Color.Lerp(theme.agedPaper, theme.ink, 0.36f);
                Color near = Color.Lerp(theme.sootNavy, theme.ink, 0.5f);

                Fill("Sky", Vector2.zero, Vector2.one, sky);
                Image high = Fill("Sky High", new Vector2(0f, 0.55f), Vector2.one, new Color(skyHigh.r, skyHigh.g, skyHigh.b, 0.55f));
                high.sprite = theme.introTextScrim;
                Fill("Sea", new Vector2(0f, 0.30f), new Vector2(1f, 0.52f), sea);
                Fill("Horizon", new Vector2(0f, 0.515f), new Vector2(1f, 0.52f), new Color(theme.ink.r, theme.ink.g, theme.ink.b, 0.35f));
                Fill("Slope", new Vector2(0f, 0.19f), new Vector2(1f, 0.305f), slope);
                Fill("Road", new Vector2(0f, 0.105f), new Vector2(1f, 0.19f), road);
                Fill("Road Edge", new Vector2(0f, 0.186f), new Vector2(1f, 0.19f), new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.45f));
                Fill("Near Slope", Vector2.zero, new Vector2(1f, 0.105f), near);

                Sprite streak = S("il_streak");
                Color cloud = Color.Lerp(sky, theme.petrol, 0.45f);
                for (int i = 0; i < 9; i++)
                    cloudStreaks.Add(Strip("Cloud", root.transform, streak, new Color(cloud.r, cloud.g, cloud.b, 0.35f + 0.25f * Rand01(i)),
                        new Vector2(Rand01(i * 7) * 2200f - 200f, 620f + Rand01(i * 3) * 400f), new Vector2(700f + Rand01(i * 5) * 900f, 26f + Rand01(i * 11) * 40f)));
                Color foam = Color.Lerp(theme.agedPaper, Color.white, 0.4f);
                for (int i = 0; i < 26; i++)
                    seaStreaks.Add(Strip("Sea Streak", root.transform, streak, new Color(foam.r, foam.g, foam.b, 0.18f + 0.25f * Rand01(i * 13)),
                        new Vector2(Rand01(i * 17) * 2100f - 100f, 330f + Rand01(i * 19) * 210f), new Vector2(90f + Rand01(i * 23) * 260f, 3f + Rand01(i * 29) * 4f)));

                for (float km = Mathf.Ceil(StartKm); km <= StartKm + TargetDistanceKm + 1f; km += 1f) SpawnPost(km);

                group = CreateRect("Group", root.transform, new Vector2(0.40f, 0.135f), new Vector2(0.40f, 0.135f), Vector2.zero, Vector2.zero).GetComponent<RectTransform>();
                const float s = 0.66f;
                // Soldan sağa: Greet ve bebek arabası, Kees, arabayı arkadan iten Truus, araba,
                // önde kolu tutan Jan. Araba sprite'ı yatay çevrildiği için kolu Jan'a bakar.
                AddWalker(null, new[] { "il_greet_0", "il_greet_1", "il_greet_2", "il_greet_3" }, new Vector2(-560f, 0f), new Vector2(240f * s, 360f * s), true);
                AddWalker("il_pram", null, new Vector2(-468f, -4f), new Vector2(220f * s, 140f * s), false);
                AddWalker(null, new[] { "il_child_0", "il_child_1", "il_child_2", "il_child_3" }, new Vector2(-330f, 0f), new Vector2(240f * s, 360f * s), false);
                AddWalker(null, new[] { "il_woman_0", "il_woman_1", "il_woman_2", "il_woman_3" }, new Vector2(-196f, 0f), new Vector2(240f * s, 360f * s), true);
                AddWalker("il_cart", null, new Vector2(0f, -8f), new Vector2(480f * s, 220f * s), true);
                AddWalker(null, new[] { "il_man_0", "il_man_1", "il_man_2", "il_man_3" }, new Vector2(196f, 0f), new Vector2(240f * s, 360f * s), false);

                for (int i = 0; i < 46; i++)
                {
                    Snow flake = new Snow();
                    flake.depth = Rand01(i * 31);
                    flake.rect = Strip("Snow", root.transform, streak, new Color(1f, 1f, 1f, 0f),
                        new Vector2(Rand01(i * 37) * 2100f, 90f + Rand01(i * 41) * 560f), new Vector2(60f + flake.depth * 160f, 2f + flake.depth * 3f));
                    flake.image = flake.rect.GetComponent<Image>();
                    flake.speed = 700f + flake.depth * 900f;
                    snow.Add(flake);
                }
            }

            private void AddWalker(string still, string[] frames, Vector2 offset, Vector2 size, bool flipped)
            {
                Image image = Figure(still ?? frames[0], group, S(still ?? frames[1]), offset, size, flipped);
                walkers.Add(new Walker { image = image, rect = image.rectTransform, frames = frames, origin = offset, phase = walkers.Count * 0.37f });
            }

            private void SpawnPost(float km)
            {
                float x = 0.40f * 1920f + 260f + (km - StartKm) * PixelsPerKm;
                RectTransform rect = Strip("Post", root.transform, S("il_post"), Color.white, new Vector2(x, 0.168f * 1080f), new Vector2(24f, 96f));
                rect.pivot = new Vector2(0.5f, 0f);
                TMP_Text label = (TMP_Text)app.AsDocument(app.CreateText("Km", rect, ((int)km).ToString(), 16f, FontStyles.Bold, theme.ink,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-30f, 2f), new Vector2(30f, 26f), TextAlignmentOptions.Center));
                label.overflowMode = TextOverflowModes.Overflow;
                posts.Add(rect);
            }

            protected override void Step(float dt)
            {
                bool pushing = app.InterludePrimaryHeld;
                bool gust = gustRemaining > 0f;
                if (!gust && nextGust < GustAtKm.Length && distanceKm >= GustAtKm[nextGust])
                {
                    gustRemaining = GustDuration;
                    gust = true;
                    nextGust++;
                    gustsSeen++;
                    gustPushedSeconds = 0f;
                    gustReleasedSeconds = 0f;
                    app.audioManager.PlayGust();
                }
                if (gust)
                {
                    gustRemaining -= dt;
                    if (pushing) gustPushedSeconds += dt; else gustReleasedSeconds += dt;
                    if (gustRemaining <= 0f)
                    {
                        float pushedFraction = gustPushedSeconds / Mathf.Max(0.01f, gustPushedSeconds + gustReleasedSeconds);
                        if (pushedFraction < 0.6f) everyGustPushed = false;
                        if (gustReleasedSeconds >= 1.2f) anyGustWaited = true;
                    }
                }
                float gustStrength = gust ? Mathf.Sin(Mathf.Clamp01(1f - gustRemaining / GustDuration) * Mathf.PI) : 0f;
                float speed = pushing ? BaseSpeed * (gust ? Mathf.Lerp(1f, GustSpeedFactor, gustStrength) : 1f) : 0f;
                float dx = speed * dt;
                distanceKm += dx / PixelsPerKm;

                for (int i = 0; i < posts.Count; i++) posts[i].anchoredPosition += new Vector2(-dx, 0f);
                float wind = 1f + gustStrength * 2.4f;
                Drift(cloudStreaks, (38f + 20f * gustStrength) * dt, 2300f, -300f);
                Drift(seaStreaks, (140f * wind + dx * 0.4f) * dt, 2200f, -200f);

                int visible = 10 + (int)(36f * gustStrength);
                for (int i = 0; i < snow.Count; i++)
                {
                    Snow flake = snow[i];
                    bool on = i < visible;
                    Color c = flake.image.color;
                    c.a = Mathf.MoveTowards(c.a, on ? 0.30f + 0.45f * flake.depth : 0f, dt * 2.5f);
                    flake.image.color = c;
                    if (c.a <= 0.001f && !on) continue;
                    Vector2 p = flake.rect.anchoredPosition;
                    p.x -= (flake.speed * wind + dx * 2f) * dt;
                    p.y -= (20f + 60f * gustStrength) * dt * flake.depth;
                    if (p.x < -200f) { p.x = 2100f + Rand01(i + gustsSeen * 7) * 300f; p.y = 90f + Rand01(i * 41 + gustsSeen) * 560f; }
                    flake.rect.anchoredPosition = p;
                    flake.rect.localRotation = Quaternion.Euler(0f, 0f, -6f * gustStrength);
                }

                bool moving = speed > 1f;
                float lean = pushing ? -7f * gustStrength : -1.5f * gustStrength;
                float frameRate = moving ? 6.2f * Mathf.Clamp(speed / BaseSpeed, 0.5f, 1f) : 0f;
                for (int i = 0; i < walkers.Count; i++)
                {
                    Walker w = walkers[i];
                    w.phase += frameRate * dt;
                    if (w.frames != null) Animate(w.image, w.frames, w.phase, moving);
                    float bob = moving ? Mathf.Abs(Mathf.Sin(w.phase * Mathf.PI * 0.5f)) * 5f : 0f;
                    w.rect.anchoredPosition = w.origin + new Vector2(0f, bob);
                    w.rect.localRotation = Quaternion.Euler(0f, 0f, w.frames == null ? lean * 0.35f : lean);
                }
            }

            protected override void Resolve()
            {
                if (gustsSeen == 0 || (everyGustPushed && !anyGustWaited)) Result = ResultPushed;
                else if (anyGustWaited) Result = ResultWaited;
                else Result = everyGustPushed ? ResultPushed : ResultWaited;
            }
        }
    }
}
