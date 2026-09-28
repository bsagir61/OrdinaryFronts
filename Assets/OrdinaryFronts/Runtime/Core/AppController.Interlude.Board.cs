using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OrdinaryFronts
{
    public sealed partial class AppController
    {
        /// <summary>
        /// Köy santrali (Karelya): karar sahnesi. Duvarda tahta bir santral panosu; delik
        /// sıraları ve her deliğin üstünde küçük bir kapak lambası. İki lamba aynı anda düşmüş,
        /// yanıp sönüyor; tezgâhta tek bir kablo var.
        /// <para>
        /// Önce seçim: talimat kartı düğümün iki seçeneğini gösterir. <b>A</b> soldaki,
        /// <b>D</b> sağdaki lambaya döner; seçim o an verilir. Sonra el işi:
        /// </para>
        /// <list type="bullet">
        /// <item><b>Fiş.</b> Kablonun ucu seçilen deliğin önünden sallanarak geçer; delikle üst
        /// üste geldiğinde BOŞLUK / sol tık: fiş oturur. Erken ya da geç basmak yalnız yeniden
        /// denemektir.</item>
        /// <item><b>Zil.</b> Tezgâhın yanındaki kolu çevirmek için BOŞLUK / sol tık basılı tut;
        /// kolun çevresindeki halka dolar ve hattın öbür ucu çalar.</item>
        /// </list>
        /// Bu sırada öbür lamba yanıp sönmeyi sürdürür; bağlantı kurulunca bir süre daha yanar,
        /// sonra söner. Kaybetmek yok: seçimin bedeli, cevapsız kalan lambanın sönmesidir.
        /// </summary>
        private sealed class BoardInterlude : InterludeScene
        {
            private const int Columns = 8;
            private const float GridX = 512f, GridStep = 128f;
            private static readonly float[] Rows = { 780f, 680f, 580f, 480f };
            private const float DeskY = 250f;
            private const float CrankX = 1640f, CrankY = 330f;
            private const float SwingHalf = 190f, SwingPeriod = 1.7f, PlugWindow = 20f;
            private const float CrankSeconds = 2.4f;

            private readonly Vector2[] targets = { new Vector2(GridX + GridStep * 1f, 680f), new Vector2(GridX + GridStep * 6f, 580f) };
            private readonly Image[] shutters = new Image[2];
            private readonly Image[] glows = new Image[2];
            private RectTransform plug, cord, crankHandle;
            private Image crankRing, targetRing;
            private readonly List<RectTransform> flakes = new List<RectTransform>();

            private int task = -1;
            private bool plugged;
            private float swingTime;
            private float missPause;
            private float crank;
            private float otherFadeAt = -1f;
            private float doneAt = -1f;

            public BoardInterlude(AppController app, StoryNode node) : base(app, node) { }

            protected override string Title { get { return L(UiKey.IlBoardTitle); } }
            protected override string HowTo { get { return L(UiKey.IlBoardHow); } }
            protected override string HintText { get { return task < 0 ? L(UiKey.IlChooseHint) : !plugged ? HandHint(UiKey.IlPlugHint) : HandHint(UiKey.IlCrankHint); } }
            protected override bool ShowsChoices { get { return true; } }
            protected override bool StartPressed { get { return app.IlLeftPressed || app.IlRightPressed; } }
            protected override bool Finished { get { return doneAt >= 0f && elapsed >= doneAt; } }
            protected override string OutcomeText { get { return L(UiKey.InterludeBoardDone); } }

            protected override void Build()
            {
                Color wall = Color.Lerp(theme.sootNavy, theme.agedPaper, 0.14f);
                Color wood = Color.Lerp(theme.rust, theme.ink, 0.58f);
                Color face = Color.Lerp(theme.ink, theme.sootNavy, 0.35f);
                Color brass = Color.Lerp(theme.mustard, theme.agedPaper, 0.25f);

                Fill("Wall", Vector2.zero, Vector2.one, wall);
                for (int i = 0; i < 12; i++)
                    Strip("Plank", null, null, new Color(theme.ink.r, theme.ink.g, theme.ink.b, 0.10f + 0.06f * Rand01(i)),
                        new Vector2(80f + i * 160f, 540f), new Vector2(3f, 1080f));

                // Pencere: karlı gece, uzakta iki ışık.
                Strip("Window Frame", null, null, wood, new Vector2(1700f, 830f), new Vector2(300f, 270f));
                Strip("Window", null, null, Color.Lerp(theme.sootNavy, theme.petrol, 0.35f), new Vector2(1700f, 830f), new Vector2(270f, 240f));
                Strip("Snow Field", null, null, Color.Lerp(theme.agedPaper, theme.petrol, 0.35f), new Vector2(1700f, 740f), new Vector2(270f, 60f));
                Strip("Far Light", null, theme.mapMarker, theme.mustard, new Vector2(1640f, 790f), new Vector2(8f, 8f));
                Strip("Far Light", null, theme.mapMarker, new Color(theme.mustard.r, theme.mustard.g, theme.mustard.b, 0.6f), new Vector2(1750f, 782f), new Vector2(6f, 6f));
                Strip("Mullion", null, null, wood, new Vector2(1700f, 830f), new Vector2(8f, 240f));
                Strip("Transom", null, null, wood, new Vector2(1700f, 850f), new Vector2(270f, 8f));
                for (int i = 0; i < 14; i++)
                    flakes.Add(Strip("Flake", null, S("il_puff"), new Color(1f, 1f, 1f, 0.7f),
                        new Vector2(1575f + Rand01(i * 3) * 250f, 720f + Rand01(i * 7) * 220f), new Vector2(5f, 5f)));

                // Pano ve delik sıraları.
                Strip("Cabinet", null, null, wood, new Vector2(960f, 610f), new Vector2(1160f, 560f));
                Strip("Face", null, null, face, new Vector2(960f, 625f), new Vector2(1100f, 470f));
                for (int r = 0; r < Rows.Length; r++)
                    for (int c = 0; c < Columns; c++)
                    {
                        Vector2 p = new Vector2(GridX + GridStep * c, Rows[r]);
                        Strip("Shutter", null, null, Color.Lerp(face, brass, 0.35f), p + new Vector2(0f, 38f), new Vector2(46f, 16f));
                        Ring("Jack Ring", null, p, 34f, brass, false);
                        Strip("Jack", null, theme.mapMarker, Color.Lerp(theme.ink, Color.black, 0.5f), p, new Vector2(16f, 16f));
                    }
                for (int i = 0; i < 2; i++)
                {
                    glows[i] = Strip("Lamp Glow", null, S("il_puff"), new Color(theme.mustard.r, theme.mustard.g, theme.mustard.b, 0f), targets[i] + new Vector2(0f, 38f), new Vector2(150f, 110f)).GetComponent<Image>();
                    shutters[i] = Strip("Dropped Shutter", null, null, theme.mustard, targets[i] + new Vector2(0f, 30f), new Vector2(46f, 18f)).GetComponent<Image>();
                }

                // Tezgâh: fiş sırası ve kablolar.
                Strip("Desk", null, null, Color.Lerp(wood, theme.ink, 0.25f), new Vector2(960f, DeskY * 0.5f + 10f), new Vector2(1500f, DeskY + 20f));
                Strip("Desk Edge", null, null, Color.Lerp(wood, theme.agedPaper, 0.25f), new Vector2(960f, DeskY + 18f), new Vector2(1500f, 6f));
                for (int c = 0; c < Columns; c++)
                {
                    float x = GridX + GridStep * c;
                    Strip("Plug Seat", null, theme.mapMarker, Color.Lerp(theme.ink, Color.black, 0.4f), new Vector2(x, DeskY), new Vector2(20f, 12f));
                    Strip("Plug Head", null, null, brass, new Vector2(x, DeskY + 22f), new Vector2(12f, 30f));
                }
                cord = Strip("Cord", null, null, Color.Lerp(theme.ink, theme.rust, 0.25f), Vector2.zero, new Vector2(10f, 7f));
                cord.gameObject.SetActive(false);
                plug = Strip("Plug", null, null, brass, Vector2.zero, new Vector2(16f, 40f));
                plug.gameObject.SetActive(false);
                targetRing = Ring("Target", null, targets[0], 64f, new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.55f), false);
                targetRing.gameObject.SetActive(false);

                // Zil kolu.
                Ring("Crank Base", null, new Vector2(CrankX, CrankY), 96f, brass, false);
                crankHandle = Strip("Crank", null, null, brass, new Vector2(CrankX, CrankY), new Vector2(14f, 110f));
                crankHandle.pivot = new Vector2(0.5f, 0.1f);
                Strip("Crank Knob", crankHandle, theme.mapMarker, Color.Lerp(theme.ink, theme.rust, 0.3f), new Vector2(7f, 100f), new Vector2(24f, 24f));
                crankRing = Ring("Crank Progress", null, new Vector2(CrankX, CrankY), 170f, theme.mustard, true);
                crankRing.gameObject.SetActive(false);

                // Tavandan sarkan ampul.
                Strip("Bulb Glow", null, S("il_puff"), new Color(theme.mustard.r, theme.mustard.g, theme.mustard.b, 0.20f), new Vector2(960f, 990f), new Vector2(900f, 500f));
                Strip("Bulb Wire", null, null, theme.ink, new Vector2(960f, 1060f), new Vector2(3f, 60f));
                Strip("Bulb", null, theme.mapMarker, Color.Lerp(theme.mustard, Color.white, 0.4f), new Vector2(960f, 1025f), new Vector2(22f, 22f));
            }

            protected override void OnBegin()
            {
                task = app.IlRightPressed ? 1 : 0;
                ChoiceIndex = task;
                app.audioManager.PlayConfirm();
                SetCentreColor(theme.agedPaper);
                ShowCentre(node.choices != null && node.choices.Length > task ? node.choices[task].text : string.Empty, 1.4f);
                targetRing.rectTransform.anchoredPosition = targets[task];
                targetRing.gameObject.SetActive(true);
                plug.gameObject.SetActive(true);
                cord.gameObject.SetActive(true);
                SetHint(HintText);
            }

            protected override void Step(float dt)
            {
                // Lambalar: seçilmeden önce ikisi, sonra yalnız cevapsız kalan yanıp söner.
                for (int i = 0; i < 2; i++)
                {
                    bool answered = task == i && plugged;
                    bool dark = task >= 0 && i != task && otherFadeAt >= 0f && elapsed >= otherFadeAt;
                    float blink = 0.5f + 0.5f * Mathf.Sin(elapsed * 6f + i * Mathf.PI);
                    float level = answered ? 0.35f : dark ? 0f : 0.25f + 0.75f * blink;
                    glows[i].color = new Color(theme.mustard.r, theme.mustard.g, theme.mustard.b, 0.55f * level);
                    shutters[i].color = Color.Lerp(Color.Lerp(theme.ink, theme.mustard, 0.25f), Color.Lerp(theme.mustard, Color.white, 0.3f), level);
                    shutters[i].rectTransform.anchoredPosition = targets[i] + new Vector2(0f, dark || answered ? 38f : 30f);
                }
                for (int i = 0; i < flakes.Count; i++)
                {
                    Vector2 p = flakes[i].anchoredPosition;
                    p += new Vector2(Mathf.Sin(elapsed + i) * 6f, -22f - 8f * Rand01(i)) * dt;
                    if (p.y < 720f) p.y = 950f;
                    flakes[i].anchoredPosition = p;
                }
                if (task < 0) return;

                Vector2 target = targets[task];
                Vector2 seat = new Vector2(target.x, DeskY + 30f);
                if (!plugged)
                {
                    if (missPause > 0f) missPause -= dt;
                    else swingTime += dt;
                    float x = target.x + Mathf.Sin(swingTime * Mathf.PI * 2f / Ease(SwingPeriod, 2.6f)) * SwingHalf;
                    Vector2 tip = new Vector2(x, target.y - 18f);
                    plug.anchoredPosition = tip;
                    SetCord(seat, tip);
                    bool inWindow = Mathf.Abs(x - target.x) <= Ease(PlugWindow, 50f);
                    targetRing.color = inWindow ? Color.Lerp(theme.mustard, Color.white, 0.5f) : new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.55f);
                    if (Begun && playTime > 0.6f && missPause <= 0f && (app.IlActionPressed || ((app.interludeForcePush || app.IlAuto) && inWindow)))
                    {
                        if (inWindow)
                        {
                            plugged = true;
                            plug.anchoredPosition = new Vector2(target.x, target.y - 18f);
                            SetCord(seat, plug.anchoredPosition);
                            targetRing.gameObject.SetActive(false);
                            crankRing.gameObject.SetActive(true);
                            app.audioManager.PlayConfirm();
                            Shake(0.25f);
                            SetCentreColor(theme.agedPaper);
                            ShowCentre(L(UiKey.IlPlugged), 0.9f);
                            SetHint(HintText);
                        }
                        else
                        {
                            missPause = 0.45f;
                            SetCentreColor(theme.agedPaper);
                            ShowCentre(L(UiKey.IlBoardMiss), 0.8f);
                        }
                    }
                    return;
                }

                if (crank < 1f)
                {
                    if (app.IlActionHeld || app.IlAuto)
                    {
                        crank = Mathf.Min(1f, crank + dt / Ease(CrankSeconds, 1.3f));
                        crankHandle.localRotation = Quaternion.Euler(0f, 0f, -crank * 360f * 4f);
                        if (Rand01((int)(elapsed * 20f)) > 0.9f) Shake(0.08f);
                    }
                    crankRing.fillAmount = crank;
                    if (crank >= 1f)
                    {
                        crankRing.gameObject.SetActive(false);
                        app.audioManager.PlayConfirm();
                        otherFadeAt = elapsed + 1.2f;
                        doneAt = elapsed + 2.2f;
                        SetCentreColor(theme.agedPaper);
                        ShowCentre(L(UiKey.IlBoardOther), 1.2f);
                    }
                }
            }

            private void SetCord(Vector2 from, Vector2 to)
            {
                Vector2 d = to - from;
                cord.anchoredPosition = (from + to) * 0.5f;
                cord.sizeDelta = new Vector2(d.magnitude, 7f);
                cord.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }

            protected override void Resolve()
            {
                if (task < 0) return;
                ChoiceIndex = task;
                if (data.results != null && data.results.Length > task) Result = data.results[task];
            }
        }
    }
}
