using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OrdinaryFronts
{
    public sealed partial class AppController
    {
        /// <summary>
        /// Kayan tahta (Neretva): karar sahnesi. Yıkık köprünün üstüne atılmış kirişte, önde
        /// bir taşıyıcı, arkada sedyenin ucunu tutan Milena. Kiriş ıslak; ağırlık sağa sola
        /// kayar ve oyuncu A/← D/→ ile (ya da fare tuşunu basılı tutup sağa sola götürerek)
        /// dengeyi tutar. Ortada tahta kayar, öndeki diz çöker, sedye suya doğru yatar ve iki
        /// saniyelik bir an açılır: <b>tut</b> (basılı kal) ya da <b>bırak</b> (elini çek).
        /// Düğümün iki seçimi budur; hesap yapılmaz, el karar verir. Sendelemek sayılmaz,
        /// ceza değildir; yalnız kirişin ne kadar ıslak olduğunu hatırlatır.
        /// </summary>
        private sealed class PlankInterlude : InterludeScene
        {
            private const float CrossingSeconds = 20f;
            private const float SlipAt = 0.52f;
            private const float DecisionWindow = 2.4f;
            private const float StumbleAngle = 20f;
            private const float BeamY = 0.50f * 1080f;
            private const float BeamLeft = 0.17f * 1920f;
            private const float BeamRight = 0.83f * 1920f;
            private static readonly float[] KickAt = { 0.14f, 0.30f, 0.42f };

            private readonly List<RectTransform> waterStreaks = new List<RectTransform>();
            private readonly List<RectTransform> mist = new List<RectTransform>();
            private RectTransform group;
            private RectTransform beam;
            private Image rear, front, stretcher;
            private RectTransform frontRect, stretcherRect;
            private readonly string[] rearFrames = { "il_woman_0", "il_woman_1", "il_woman_2", "il_woman_3" };
            private readonly string[] frontFrames = { "il_man_0", "il_man_1", "il_man_2", "il_man_3" };

            private float progress;
            private float tilt;
            private float tiltVel;
            private float freeze;
            private int kicks;
            private int stumbles;
            private float phase;
            private bool slipped;
            private float slipClock;
            private float heldSeconds;
            private int decided = -1;
            private float doneAt = -1f;
            private float shake;

            public PlankInterlude(AppController app, StoryNode node) : base(app, node) { }

            protected override string HintText { get { return app.T(UiKey.InterludePlankHint); } }
            protected override bool Finished { get { return doneAt >= 0f && elapsed >= doneAt; } }

            protected override void Build()
            {
                Color sky = Color.Lerp(theme.agedPaper, theme.mustard, 0.10f);
                Color farWall = Color.Lerp(theme.petrol, theme.sootNavy, 0.45f);
                Color nearWall = Color.Lerp(theme.sootNavy, theme.ink, 0.4f);
                Color water = Color.Lerp(theme.petrol, theme.ink, 0.35f);

                Fill("Sky", Vector2.zero, Vector2.one, sky);
                Image haze = Fill("Haze", new Vector2(0f, 0.45f), new Vector2(1f, 0.95f), new Color(theme.petrol.r, theme.petrol.g, theme.petrol.b, 0.30f));
                haze.sprite = theme.introTextScrim;
                Fill("Far Wall", new Vector2(0f, 0.30f), new Vector2(1f, 0.62f), farWall);
                Fill("Far Wall Foot", new Vector2(0f, 0.30f), new Vector2(1f, 0.44f), Color.Lerp(farWall, theme.sootNavy, 0.35f));
                // Karşı yamacın sırtında orman: ufuk çizgisine oturan yumuşak koyu kütleler.
                for (int i = 0; i < 70; i++)
                {
                    float w = 70f + Rand01(i * 11) * 150f;
                    Strip("Wood", root.transform, S("il_puff"), Color.Lerp(farWall, theme.sootNavy, 0.6f),
                        new Vector2(Rand01(i * 3) * 1920f, 0.62f * 1080f - 8f + Rand01(i * 7) * 14f), new Vector2(w, w * (0.45f + 0.3f * Rand01(i * 5))));
                }
                Fill("Water", Vector2.zero, new Vector2(1f, 0.30f), water);
                Sprite streak = S("il_streak");
                Color foam = Color.Lerp(theme.agedPaper, theme.petrol, 0.35f);
                for (int i = 0; i < 34; i++)
                    waterStreaks.Add(Strip("Water Streak", root.transform, streak, new Color(foam.r, foam.g, foam.b, 0.16f + 0.30f * Rand01(i * 13)),
                        new Vector2(Rand01(i * 17) * 2100f - 100f, 20f + Rand01(i * 19) * 290f), new Vector2(80f + Rand01(i * 23) * 300f, 3f + Rand01(i * 29) * 5f)));
                for (int i = 0; i < 6; i++)
                    mist.Add(Strip("Mist", root.transform, S("il_puff"), new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.10f + 0.08f * Rand01(i)),
                        new Vector2(Rand01(i * 5) * 1920f, 300f + Rand01(i * 9) * 120f), new Vector2(500f + Rand01(i * 3) * 500f, 160f + Rand01(i * 7) * 120f)));

                // Yakın yamaçlar: iki koyu kütle, kirişin dayandığı yerler.
                Strip("Left Cliff", root.transform, null, nearWall, new Vector2(BeamLeft * 0.5f - 20f, 0.42f * 1080f), new Vector2(BeamLeft + 40f, 0.86f * 1080f));
                Strip("Right Cliff", root.transform, null, nearWall, new Vector2((BeamRight + 1920f) * 0.5f + 20f, 0.42f * 1080f), new Vector2(1920f - BeamRight + 40f, 0.86f * 1080f));
                // Yıkık köprünün kalan kafesi: iki yanda çapraz çizgiler.
                for (int i = 0; i < 6; i++)
                {
                    RectTransform bar = Strip("Truss", root.transform, null, Color.Lerp(theme.ink, theme.sootNavy, 0.3f),
                        new Vector2(BeamLeft + 60f + i * 90f, BeamY - 60f), new Vector2(120f, 5f));
                    bar.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 42f : -42f);
                    RectTransform bar2 = Strip("Truss", root.transform, null, Color.Lerp(theme.ink, theme.sootNavy, 0.3f),
                        new Vector2(BeamRight - 60f - i * 90f, BeamY - 60f), new Vector2(120f, 5f));
                    bar2.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? -42f : 42f);
                }
                // Kiriş ve üstündeki ıslak tahtalar.
                beam = Strip("Beam", root.transform, null, theme.ink, new Vector2((BeamLeft + BeamRight) * 0.5f, BeamY - 12f), new Vector2(BeamRight - BeamLeft, 16f));
                Strip("Planks", root.transform, null, Color.Lerp(theme.ink, theme.mustard, 0.30f), new Vector2((BeamLeft + BeamRight) * 0.5f, BeamY - 2f), new Vector2(BeamRight - BeamLeft, 6f));
                for (int i = 0; i < 26; i++)
                    Strip("Plank Gap", root.transform, null, new Color(theme.ink.r, theme.ink.g, theme.ink.b, 0.6f),
                        new Vector2(BeamLeft + 20f + i * 48f + Rand01(i) * 10f, BeamY - 2f), new Vector2(3f, 6f));

                // Grup: arkada Milena, ortada sedye, önde taşıyıcı. Kök nokta ayak hizası.
                group = CreateRect("Group", root.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero).GetComponent<RectTransform>();
                group.pivot = new Vector2(0.5f, 0f);
                const float s = 0.62f;
                rear = Figure("Rear", group, S(rearFrames[1]), new Vector2(-150f, 0f), new Vector2(240f * s, 360f * s), true);
                stretcher = Figure("Stretcher", group, S("il_stretcher"), new Vector2(10f, 74f), new Vector2(360f * s * 1.15f, 110f * s * 1.15f), false);
                stretcherRect = stretcher.rectTransform;
                front = Figure("Front", group, S(frontFrames[1]), new Vector2(160f, 0f), new Vector2(240f * s, 360f * s), false);
                frontRect = front.rectTransform;
                PlaceGroup();
            }

            private void PlaceGroup()
            {
                float x = Mathf.Lerp(BeamLeft + 120f, BeamRight - 140f, progress);
                float bob = Mathf.Sin(elapsed * 2.6f) * 3f;
                group.anchoredPosition = new Vector2(x + Rand01((int)(elapsed * 60f)) * shake * 6f, BeamY + bob - 4f);
                group.localRotation = Quaternion.Euler(0f, 0f, -tilt * 0.55f);
                beam.anchoredPosition = new Vector2((BeamLeft + BeamRight) * 0.5f, BeamY - 12f + Mathf.Sin(elapsed * 2.6f) * 2f);
            }

            protected override void Step(float dt)
            {
                Drift(waterStreaks, 120f * dt, 2100f, -200f);
                for (int i = 0; i < mist.Count; i++)
                {
                    Vector2 p = mist[i].anchoredPosition;
                    p.x += (12f + 6f * Rand01(i)) * dt;
                    if (p.x - mist[i].sizeDelta.x * 0.5f > 1920f) p.x = -mist[i].sizeDelta.x * 0.5f;
                    mist[i].anchoredPosition = p;
                }
                shake = Mathf.MoveTowards(shake, 0f, dt * 3f);

                if (!slipped)
                {
                    StepBalance(dt);
                    bool walking = freeze <= 0f;
                    if (walking) progress += dt / CrossingSeconds;
                    else freeze -= dt;
                    phase += (walking ? 4.4f : 0f) * dt;
                    Animate(rear, rearFrames, phase, walking);
                    Animate(front, frontFrames, phase + 0.5f, walking);
                    if (progress >= SlipAt) BeginSlip();
                }
                else StepSlip(dt);

                PlaceGroup();
            }

            /// <summary>
            /// Denge: ağırlık kendiliğinden yana açılır (kararsız denge), ıslak kiriş
            /// rastgele iter, oyuncu karşı yöne bastırır. Açı eşiği aşarsa sendeleme:
            /// kısa donma, geri sıçrama, gıcırtı.
            /// </summary>
            private void StepBalance(float dt)
            {
                float axis = 0f;
                if (app.InterludePrimaryHeld) axis += 1f;
                if (app.InterludeSecondaryHeld) axis -= 1f;
                if (Input.GetMouseButton(0) || Input.GetMouseButton(1)) axis = app.InterludeMouseAxis * 1.4f;
                axis = Mathf.Clamp(axis, -1f, 1f);

                float wobble = (Mathf.PerlinNoise(elapsed * 0.9f, 3.1f) - 0.5f) * 70f;
                if (kicks < KickAt.Length && progress >= KickAt[kicks])
                {
                    kicks++;
                    tiltVel += (kicks % 2 == 0 ? 1f : -1f) * 55f;
                    shake = 0.6f;
                }
                tiltVel += (wobble + tilt * 1.1f + axis * -95f) * dt;
                tiltVel *= 1f - 1.6f * dt;
                tilt += tiltVel * dt;

                if (Mathf.Abs(tilt) > StumbleAngle)
                {
                    stumbles++;
                    tilt = Mathf.Sign(tilt) * StumbleAngle * 0.35f;
                    tiltVel = 0f;
                    freeze = 0.7f;
                    shake = 1f;
                    app.audioManager.PlayCreak();
                }
            }

            private void BeginSlip()
            {
                slipped = true;
                slipClock = 0f;
                shake = 1f;
                tilt = -14f;
                tiltVel = -40f;
                app.audioManager.PlayCreak();
                ShowCentre(app.T(UiKey.InterludeHoldOrRelease), DecisionWindow + 0.4f);
                Animate(rear, rearFrames, 1f, false);
                Animate(front, frontFrames, 1f, false);
            }

            /// <summary>
            /// Kayma: öndeki diz çöker, sedye suya doğru yatar. Pencere boyunca herhangi bir
            /// tuşu basılı tutmak "tut", tutmamak "bırak" sayılır. Sonra bir saniyelik bedel:
            /// tutanda sedye yerine gelir, bırakanda sedye kirişten kayar.
            /// </summary>
            private void StepSlip(float dt)
            {
                slipClock += dt;
                bool holding = app.InterludePrimaryHeld || app.InterludeSecondaryHeld || Input.GetMouseButton(0) || Input.GetMouseButton(1);
                if (decided < 0)
                {
                    if (holding) heldSeconds += dt;
                    float fall = Mathf.Clamp01(slipClock / DecisionWindow);
                    // Öndeki taşıyıcı diz üstünde: alçalır ve öne eğilir.
                    frontRect.anchoredPosition = new Vector2(160f, -28f * fall);
                    frontRect.localRotation = Quaternion.Euler(0f, 0f, 14f * fall);
                    stretcherRect.localRotation = Quaternion.Euler(0f, 0f, (holding ? -18f : -34f) * fall);
                    stretcherRect.anchoredPosition = new Vector2(10f + 16f * fall, 58f - (holding ? 10f : 26f) * fall);
                    tilt = Mathf.Lerp(tilt, holding ? -8f : -16f, dt * 3f);
                    if (slipClock >= DecisionWindow)
                    {
                        decided = heldSeconds >= 1.0f ? 0 : 1;
                        doneAt = elapsed + 1.6f;
                        if (decided == 1) app.audioManager.PlayCreak();
                    }
                    return;
                }
                float t = Mathf.Clamp01((slipClock - DecisionWindow) / 1.4f);
                if (decided == 0)
                {
                    // Tutuldu: sedye yükselir, denge geri gelir.
                    stretcherRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-18f, -4f, t));
                    stretcherRect.anchoredPosition = Vector2.Lerp(new Vector2(26f, 48f), new Vector2(10f, 56f), t);
                    tilt = Mathf.Lerp(-8f, -2f, t);
                }
                else
                {
                    // Bırakıldı: sedye kirişten kayar ve düşer; Milena ayakta kalır.
                    stretcherRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-34f, -80f, t));
                    stretcherRect.anchoredPosition = new Vector2(26f + 90f * t, 32f - 520f * t * t);
                    Color c = stretcher.color;
                    c.a = 1f - t * 0.6f;
                    stretcher.color = c;
                    tilt = Mathf.Lerp(-16f, 0f, t);
                }
            }

            protected override void Resolve()
            {
                ChoiceIndex = decided < 0 ? 1 : decided;
                if (data.results != null && data.results.Length > ChoiceIndex) Result = data.results[ChoiceIndex];
            }
        }
    }
}
