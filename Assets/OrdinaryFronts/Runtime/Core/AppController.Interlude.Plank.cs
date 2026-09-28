using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OrdinaryFronts
{
    public sealed partial class AppController
    {
        /// <summary>
        /// Kayan tahta (Neretva): karar sahnesi. Yıkık köprünün kirişinde önde bir taşıyıcı,
        /// arkada sedyenin ucunu tutan Milena.
        /// <list type="number">
        /// <item><b>Denge.</b> Grubun üstündeki işaret ağırlığın nerede olduğunu gösterir. Islak
        /// kiriş onu yana iter; A/← ağırlığı sola, D/→ sağa verir. İşaret ortada kaldıkça
        /// yürürsünüz; kenara varırsa sendelersiniz (kısa bir duraklama ve gıcırtı — ceza değil).</item>
        /// <item><b>Kayma.</b> Ortada tahta kayar: zaman yavaşlar, öndeki diz çöker, sedye suya
        /// yatar. BOŞLUK ya da sol tıkla basıp basılı tutarsan sedyeyi tutarsın; hiçbir şey
        /// yapmazsan elin açılır. Kaymadan önce basılı olan tuş sayılmaz: tutmak, kaymadan
        /// sonra verilen bir karardır.</item>
        /// </list>
        /// Düğümün iki seçimi budur: ilk seçenek "tut", ikincisi "bırak".
        /// </summary>
        private sealed class PlankInterlude : InterludeScene
        {
            private const float CrossingSeconds = 18f;
            private const float SlipAt = 0.55f;
            private const float DecisionWindow = 2.8f;
            private const float HoldNeeded = 1.1f;
            private const float StumbleAngle = 18f;
            private const float BeamY = 0.50f * 1080f;
            private const float BeamLeft = 0.17f * 1920f;
            private const float BeamRight = 0.83f * 1920f;
            private static readonly float[] KickAt = { 0.14f, 0.28f, 0.42f };

            private readonly List<RectTransform> waterStreaks = new List<RectTransform>();
            private readonly List<RectTransform> mist = new List<RectTransform>();
            private readonly List<RectTransform> rain = new List<RectTransform>();
            private RectTransform group;
            private RectTransform beam;
            private Image rear, front, stretcher;
            private RectTransform frontRect, stretcherRect;
            private RectTransform level, levelMarker;
            private Image levelMarkerImage;
            private Image decisionRing;
            private readonly string[] rearFrames = { "il_woman_0", "il_woman_1", "il_woman_2", "il_woman_3" };
            private readonly string[] frontFrames = { "il_man_0", "il_man_1", "il_man_2", "il_man_3" };

            private float progress;
            private float tilt;
            private float tiltVel;
            private float freeze;
            private int kicks;
            private float phase;
            private bool slipped;
            private float slipClock;
            private bool holdStarted;
            private float heldSeconds;
            private int decided = -1;
            private float decidedClock;
            private float doneAt = -1f;

            public PlankInterlude(AppController app, StoryNode node) : base(app, node) { }

            protected override string Title { get { return L(UiKey.IlPlankTitle); } }
            protected override string HowTo { get { return L(UiKey.IlPlankHow); } }
            protected override string HintText { get { return slipped && decided < 0 ? (app.IlAuto ? L(UiKey.IlHoldTapHint) : L(UiKey.IlHoldHint)) : HandHint(UiKey.IlBalanceHint); } }
            protected override bool StartPressed { get { return app.IlLeftPressed || app.IlRightPressed || app.IlActionPressed; } }
            protected override bool Finished { get { return doneAt >= 0f && elapsed >= doneAt; } }
            protected override string OutcomeText { get { return decided == 0 ? L(UiKey.IlHeld) : L(UiKey.IlLetGo); } }

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
                for (int i = 0; i < 70; i++)
                {
                    float w = 70f + Rand01(i * 11) * 150f;
                    Strip("Wood", null, S("il_puff"), Color.Lerp(farWall, theme.sootNavy, 0.6f),
                        new Vector2(Rand01(i * 3) * 1920f, 0.62f * 1080f - 8f + Rand01(i * 7) * 14f), new Vector2(w, w * (0.45f + 0.3f * Rand01(i * 5))));
                }
                Fill("Water", Vector2.zero, new Vector2(1f, 0.30f), water);
                Sprite streak = S("il_streak");
                Color foam = Color.Lerp(theme.agedPaper, theme.petrol, 0.35f);
                for (int i = 0; i < 34; i++)
                    waterStreaks.Add(Strip("Water Streak", null, streak, new Color(foam.r, foam.g, foam.b, 0.16f + 0.30f * Rand01(i * 13)),
                        new Vector2(Rand01(i * 17) * 2100f - 100f, 20f + Rand01(i * 19) * 290f), new Vector2(80f + Rand01(i * 23) * 300f, 3f + Rand01(i * 29) * 5f)));
                for (int i = 0; i < 6; i++)
                    mist.Add(Strip("Mist", null, S("il_puff"), new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.10f + 0.08f * Rand01(i)),
                        new Vector2(Rand01(i * 5) * 1920f, 300f + Rand01(i * 9) * 120f), new Vector2(500f + Rand01(i * 3) * 500f, 160f + Rand01(i * 7) * 120f)));

                Strip("Left Cliff", null, null, nearWall, new Vector2(BeamLeft * 0.5f - 20f, 0.42f * 1080f), new Vector2(BeamLeft + 40f, 0.86f * 1080f));
                Strip("Right Cliff", null, null, nearWall, new Vector2((BeamRight + 1920f) * 0.5f + 20f, 0.42f * 1080f), new Vector2(1920f - BeamRight + 40f, 0.86f * 1080f));
                for (int i = 0; i < 6; i++)
                {
                    RectTransform bar = Strip("Truss", null, null, Color.Lerp(theme.ink, theme.sootNavy, 0.3f), new Vector2(BeamLeft + 60f + i * 90f, BeamY - 60f), new Vector2(120f, 5f));
                    bar.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 42f : -42f);
                    RectTransform bar2 = Strip("Truss", null, null, Color.Lerp(theme.ink, theme.sootNavy, 0.3f), new Vector2(BeamRight - 60f - i * 90f, BeamY - 60f), new Vector2(120f, 5f));
                    bar2.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? -42f : 42f);
                }
                beam = Strip("Beam", null, null, theme.ink, new Vector2((BeamLeft + BeamRight) * 0.5f, BeamY - 12f), new Vector2(BeamRight - BeamLeft, 16f));
                Strip("Planks", null, null, Color.Lerp(theme.ink, theme.mustard, 0.30f), new Vector2((BeamLeft + BeamRight) * 0.5f, BeamY - 2f), new Vector2(BeamRight - BeamLeft, 6f));
                for (int i = 0; i < 26; i++)
                    Strip("Plank Gap", null, null, new Color(theme.ink.r, theme.ink.g, theme.ink.b, 0.6f), new Vector2(BeamLeft + 20f + i * 48f + Rand01(i) * 10f, BeamY - 2f), new Vector2(3f, 6f));
                // Kayacak tahta: yolun ortasında biraz açık renkli, ıslak parlıyor.
                Strip("Loose Plank", null, null, Color.Lerp(theme.mustard, theme.agedPaper, 0.4f),
                    new Vector2(Mathf.Lerp(BeamLeft + 260f, BeamRight - 200f, SlipAt) + 180f, BeamY - 2f), new Vector2(60f, 6f));

                group = CreateRect("Group", Stage, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero).GetComponent<RectTransform>();
                group.pivot = new Vector2(0.5f, 0f);
                const float s = 0.62f;
                rear = Figure("Rear", group, S(rearFrames[1]), new Vector2(-150f, 0f), new Vector2(240f * s, 360f * s), false);
                stretcher = Figure("Stretcher", group, S("il_stretcher"), new Vector2(10f, 74f), new Vector2(360f * s * 1.15f, 110f * s * 1.15f), false);
                stretcherRect = stretcher.rectTransform;
                front = Figure("Front", group, S(frontFrames[1]), new Vector2(160f, 0f), new Vector2(240f * s, 360f * s), false);
                frontRect = front.rectTransform;

                // Yağmur: sahne havası; kaymada ağırlaşır.
                for (int i = 0; i < 70; i++)
                {
                    RectTransform r = Strip("Rain", null, streak, new Color(foam.r, foam.g, foam.b, 0.22f),
                        new Vector2(Rand01(i * 43) * 2000f, Rand01(i * 47) * 1080f), new Vector2(40f + 24f * Rand01(i), 2f));
                    r.localRotation = Quaternion.Euler(0f, 0f, 262f);
                    rain.Add(r);
                }

                // Ağırlık işareti: grubun üstünde yatay bir çizgi, ortada bir çentik ve kayan bir nokta.
                level = CreateRect("Level", Stage, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero).GetComponent<RectTransform>();
                level.pivot = Vector2.zero;
                level.sizeDelta = new Vector2(260f, 30f);
                Color line = new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.8f);
                Strip("Level Back", level, null, new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.55f), new Vector2(130f, 15f), new Vector2(280f, 34f));
                Strip("Level Line", level, null, line, new Vector2(130f, 15f), new Vector2(240f, 3f));
                Strip("Level Centre", level, null, line, new Vector2(130f, 15f), new Vector2(3f, 18f));
                Strip("Level Left", level, null, theme.rust, new Vector2(10f, 15f), new Vector2(4f, 18f));
                Strip("Level Right", level, null, theme.rust, new Vector2(250f, 15f), new Vector2(4f, 18f));
                levelMarker = Strip("Level Marker", level, theme.mapMarker, theme.agedPaper, new Vector2(130f, 15f), new Vector2(18f, 18f));
                levelMarkerImage = levelMarker.GetComponent<Image>();

                decisionRing = Ring("Decision", null, Vector2.zero, 150f, theme.agedPaper, true);
                decisionRing.gameObject.SetActive(false);
                PlaceGroup();
            }

            private float GroupX { get { return Mathf.Lerp(BeamLeft + 260f, BeamRight - 200f, progress); } }

            private void PlaceGroup()
            {
                float bob = Mathf.Sin(elapsed * 2.6f) * 3f;
                group.anchoredPosition = new Vector2(GroupX, BeamY + bob - 4f);
                group.localRotation = Quaternion.Euler(0f, 0f, -tilt * 0.6f);
                beam.anchoredPosition = new Vector2((BeamLeft + BeamRight) * 0.5f, BeamY - 12f + Mathf.Sin(elapsed * 2.6f) * 2f);
                level.anchoredPosition = new Vector2(GroupX - 130f, BeamY + 260f);
            }

            protected override void OnBegin()
            {
                SetHint(L(UiKey.IlBalanceHint));
            }

            protected override void Step(float dt)
            {
                // Kaymada zaman yavaşlar; su, yağmur ve sis yavaş akar.
                float world = slipped && decided < 0 ? 0.35f : 1f;
                Drift(waterStreaks, 120f * dt * world, 2100f, -200f);
                for (int i = 0; i < mist.Count; i++)
                {
                    Vector2 p = mist[i].anchoredPosition;
                    p.x += (12f + 6f * Rand01(i)) * dt * world;
                    if (p.x - mist[i].sizeDelta.x * 0.5f > 1920f) p.x = -mist[i].sizeDelta.x * 0.5f;
                    mist[i].anchoredPosition = p;
                }
                for (int i = 0; i < rain.Count; i++)
                {
                    Vector2 p = rain[i].anchoredPosition;
                    p += new Vector2(-120f, -1100f) * dt * world;
                    if (p.y < -40f) { p.y += 1120f; p.x = Rand01(i * 43 + (int)elapsed) * 2000f; }
                    rain[i].anchoredPosition = p;
                }

                if (!slipped)
                {
                    StepBalance(dt);
                    bool walking = Begun && freeze <= 0f;
                    if (walking) progress += dt / CrossingSeconds;
                    else if (freeze > 0f) freeze -= dt;
                    phase += (walking ? 4.4f : 0f) * dt;
                    Animate(rear, rearFrames, phase, walking);
                    Animate(front, frontFrames, phase + 0.5f, walking);
                    if (progress >= SlipAt) BeginSlip();
                }
                else StepSlip(dt);

                float lean = Mathf.Clamp(tilt / StumbleAngle, -1f, 1f);
                levelMarker.anchoredPosition = new Vector2(130f + lean * 112f, 15f);
                levelMarkerImage.color = Mathf.Abs(lean) > 0.7f ? theme.rust : theme.agedPaper;
                level.gameObject.SetActive(!slipped);
                PlaceGroup();
            }

            /// <summary>
            /// Denge: ağırlık kendiliğinden yana açılır (kararsız denge), ıslak kiriş rastgele
            /// iter, oyuncu karşı yöne verir. A sola (eksi), D sağa (artı). İşaret ağırlığın
            /// kendisidir: sağa kaydıysa A.
            /// </summary>
            private void StepBalance(float dt)
            {
                if (!Begun)
                {
                    tilt = Mathf.Sin(elapsed * 1.3f) * 2f;
                    return;
                }
                float axis = (app.IlRightHeld ? 1f : 0f) - (app.IlLeftHeld ? 1f : 0f);
                // QA: denge kendiliğinden tutulur ki yakalama yürüyüşü göstersin.
                if (app.interludeForcePush || app.IlAuto) axis = Mathf.Clamp(-tilt * 0.25f - tiltVel * 0.08f, -1f, 1f);
                float drift = (Mathf.PerlinNoise(elapsed * 0.8f, 3.1f) - 0.5f) * Ease(50f, 25f);
                if (kicks < KickAt.Length && progress >= KickAt[kicks])
                {
                    kicks++;
                    tiltVel += (kicks % 2 == 0 ? 1f : -1f) * Ease(42f, 22f);
                    Shake(0.35f);
                    app.audioManager.PlayCreak();
                }
                tiltVel += (drift + tilt * 1.2f + axis * 72f) * dt;
                tiltVel *= 1f - 2.0f * dt;
                tilt += tiltVel * dt;

                if (Mathf.Abs(tilt) > StumbleAngle)
                {
                    tilt = Mathf.Sign(tilt) * StumbleAngle * 0.3f;
                    tiltVel = 0f;
                    freeze = 0.8f;
                    Shake(0.8f);
                    app.audioManager.PlayCreak();
                    SetCentreColor(theme.agedPaper);
                    ShowCentre(L(UiKey.IlStumble), 0.9f);
                }
            }

            private void BeginSlip()
            {
                slipped = true;
                slipClock = 0f;
                tilt = -10f;
                tiltVel = 0f;
                Shake(1f);
                app.audioManager.PlayCreak();
                SetCentreColor(theme.rust);
                ShowCentre(L(UiKey.IlSlip), Ease(DecisionWindow, 4.2f));
                SetHint(HintText);
                Animate(rear, rearFrames, 1f, false);
                Animate(front, frontFrames, 1f, false);
                decisionRing.gameObject.SetActive(true);
            }

            /// <summary>
            /// Kayma: öndeki diz çöker, sedye suya doğru yatar. Kaymadan sonra basılıp basılı
            /// tutulan BOŞLUK / sol tık "tut" sayılır; toplam 1,1 saniye tutmak kararı verir.
            /// Pencere dolarsa el açılır.
            /// </summary>
            private void StepSlip(float dt)
            {
                slipClock += dt;
                if (decided < 0)
                {
                    if (!holdStarted && slipClock > 0.05f && (app.IlActionPressed || app.interludeForcePush)) holdStarted = true;
                    // Eller serbestken tek basış yeter; ama basış hep oyuncunundur.
                    bool holding = holdStarted && (app.IlActionHeld || app.IlAuto);
                    if (holding) heldSeconds += dt;
                    float fall = Mathf.Clamp01(slipClock / Ease(DecisionWindow, 4.2f));
                    float hold = Mathf.Clamp01(heldSeconds / Ease(HoldNeeded, 0.6f));
                    frontRect.anchoredPosition = new Vector2(160f, -28f * Mathf.Min(1f, slipClock * 3f));
                    frontRect.localRotation = Quaternion.Euler(0f, 0f, 14f * Mathf.Min(1f, slipClock * 3f));
                    float drop = Mathf.Lerp(fall, 0.25f, hold);
                    stretcherRect.localRotation = Quaternion.Euler(0f, 0f, -38f * drop);
                    stretcherRect.anchoredPosition = new Vector2(10f + 20f * drop, 74f - 36f * drop);
                    tilt = Mathf.Lerp(-16f * fall, -6f, hold);
                    decisionRing.rectTransform.anchoredPosition = group.anchoredPosition + new Vector2(10f, 110f);
                    decisionRing.fillAmount = 1f - fall;
                    decisionRing.color = holding ? theme.mustard : theme.agedPaper;
                    if (heldSeconds >= Ease(HoldNeeded, 0.6f)) Decide(0);
                    else if (slipClock >= Ease(DecisionWindow, 4.2f)) Decide(1);
                    return;
                }
                decidedClock += dt;
                float t = Mathf.Clamp01(decidedClock / 1.4f);
                if (decided == 0)
                {
                    stretcherRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-10f, 0f, t));
                    stretcherRect.anchoredPosition = Vector2.Lerp(new Vector2(15f, 65f), new Vector2(10f, 74f), t);
                    frontRect.anchoredPosition = Vector2.Lerp(new Vector2(160f, -28f), Vector2.zero, t);
                    frontRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(14f, 0f, t));
                    tilt = Mathf.Lerp(-6f, 0f, t);
                }
                else
                {
                    stretcherRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-38f, -85f, t));
                    stretcherRect.anchoredPosition = new Vector2(30f + 90f * t, 38f - 520f * t * t);
                    Color c = stretcher.color;
                    c.a = 1f - t * 0.7f;
                    stretcher.color = c;
                    tilt = Mathf.Lerp(-16f, 0f, t);
                }
            }

            private void Decide(int choice)
            {
                decided = choice;
                decidedClock = 0f;
                doneAt = elapsed + 1.0f;
                decisionRing.gameObject.SetActive(false);
                if (choice == 0)
                {
                    app.audioManager.PlayConfirm();
                    Shake(0.4f);
                }
                else app.audioManager.PlayCreak();
            }

            protected override void Resolve()
            {
                if (decided < 0) return;
                ChoiceIndex = decided;
                if (data.results != null && data.results.Length > decided) Result = data.results[decided];
            }
        }
    }
}
