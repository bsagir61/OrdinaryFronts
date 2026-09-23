using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OrdinaryFronts
{
    public sealed partial class AppController
    {
        /// <summary>
        /// Sığınak merdiveni (Hamburg): karar sahnesi. Sol üstte giriş sahanlığı ve sönük acil
        /// durum lambası; merdiven sağa, aşağıdaki sığınak kapısına iner; basamaklarda
        /// karanlıkta duran insanlar.
        /// <para>
        /// Önce seçim: talimat kartı düğümün iki seçeneğini gösterir. <b>A</b> lambaya,
        /// <b>D</b> sıraya döner; seçim o an verilir ve geri alınmaz (düğümün metnindeki gibi,
        /// ikisi birden yapılamaz). Sonra seçilen iş elle yapılır:
        /// </para>
        /// <list type="bullet">
        /// <item><b>Lamba.</b> BOŞLUK / sol tık basılı tutarak kabloyu bağla; lambanın çevresindeki
        /// halka dolar. Üç kez kıvılcım uyarısı gelir (ampul beyaz çakar, "BIRAK!"): o an elini
        /// çekersen kıvılcım geçer; tutmaya devam edersen elin yanar, ilerleme biraz geri gider.</item>
        /// <item><b>Sıra.</b> Kenardaki kişi elini uzatır; elin üstünde daralan bir halka ve sabit
        /// bir hedef halka belirir. Halka hedefe oturduğunda BOŞLUK / sol tık: tuttun, iner.
        /// Erken ya da geç basmak yalnız yeniden denemektir. Dördüncüden sonra gerisi elden ele gelir.</item>
        /// </list>
        /// Kaybetmek yok; iş ne kadar sürerse sürsün sonunda yapılır.
        /// </summary>
        private sealed class LampInterlude : InterludeScene
        {
            private const float RepairSeconds = 5.5f;
            private const int GuideTarget = 4;
            private const float EdgeU = 0.28f;
            private const float SlotSpacing = 0.14f;
            private const float TopX = 420f, TopY = 520f, BottomX = 1480f, BottomY = 170f, DoorX = 1710f;
            private const float LampX = 600f, LampY = 790f;
            private const float LandingScale = 700f;
            private static readonly float[] SparkAt = { 0.28f, 0.58f, 0.84f };
            private const float SparkWarning = 0.9f;
            private const float RingCycle = 1.25f;

            private readonly List<Person> people = new List<Person>();
            private readonly List<Spark> sparks = new List<Spark>();
            private Image darkness, glow, bulb, doorGlow;
            private Image progressRing, guideRing, guideTarget;
            private RectTransform lampRect;

            private int task = -1;
            private float progress;
            private int sparkIndex;
            private float warning = -1f;
            private bool releasedDuringWarning;
            private float stun;
            private float lampLevel;
            private float flash;
            private int guided;
            private float ringTime;
            private float ringPause;
            private float doneAt = -1f;
            private bool released;

            private sealed class Person
            {
                public Image image;
                public RectTransform rect;
                public string[] frames;
                public float u;           // yol: <0 sahanlık, 0..1 merdiven, >1 zemin
                public bool descending;
                public bool gone;
                public float phase;
                public float scale;
            }

            private sealed class Spark { public RectTransform rect; public Image image; public Vector2 velocity; public float life; }

            public LampInterlude(AppController app, StoryNode node) : base(app, node) { }

            protected override string Title { get { return L(UiKey.IlLampTitle); } }
            protected override string HowTo { get { return L(UiKey.IlLampHow); } }
            protected override string HintText { get { return L(UiKey.IlChooseHint); } }
            protected override bool ShowsChoices { get { return true; } }
            protected override bool StartPressed { get { return app.IlLeftPressed || app.IlRightPressed; } }
            protected override bool Finished { get { return doneAt >= 0f && elapsed >= doneAt; } }
            protected override string OutcomeText { get { return task == 0 ? L(UiKey.InterludeLampLit) : L(UiKey.InterludeStairsDone); } }

            protected override void Build()
            {
                Color wall = Color.Lerp(theme.sootNavy, theme.agedPaper, 0.20f);
                Color stone = Color.Lerp(theme.ink, theme.agedPaper, 0.30f);
                Color tread = new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.45f);

                Fill("Wall", Vector2.zero, Vector2.one, wall);
                for (int i = 0; i < 16; i++)
                    Strip("Plaster", null, S("il_streak"), new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.05f + 0.05f * Rand01(i)),
                        new Vector2(Rand01(i * 5) * 1920f, 300f + Rand01(i * 9) * 700f), new Vector2(300f + Rand01(i * 3) * 700f, 6f + Rand01(i * 7) * 10f));
                // Duvar boyunca iki boru: sığınağın sanayi yüzü ve ölçek.
                Strip("Pipe", null, null, Color.Lerp(theme.ink, theme.rust, 0.25f), new Vector2(960f, 960f), new Vector2(1920f, 14f));
                Strip("Pipe", null, null, Color.Lerp(theme.ink, theme.agedPaper, 0.15f), new Vector2(960f, 930f), new Vector2(1920f, 8f));

                // Sığınak kapısı: merdivenin sonunda, içerideki sıcak ışık.
                Strip("Door", null, null, Color.Lerp(theme.ink, theme.sootNavy, 0.3f), new Vector2(DoorX, BottomY + 150f), new Vector2(200f, 300f));
                Strip("Door Light", null, null, Color.Lerp(theme.mustard, theme.agedPaper, 0.35f), new Vector2(DoorX, BottomY + 140f), new Vector2(150f, 280f));

                // Sahanlık, merdiven, zemin.
                Strip("Landing", null, null, stone, new Vector2(TopX * 0.5f, TopY * 0.5f), new Vector2(TopX + 4f, TopY));
                Strip("Landing Edge", null, null, tread, new Vector2(TopX * 0.5f, TopY - 1f), new Vector2(TopX + 4f, 3f));
                const int steps = 9;
                for (int i = 0; i < steps; i++)
                {
                    float t0 = i / (float)steps;
                    float t1 = (i + 1) / (float)steps;
                    float x0 = Mathf.Lerp(TopX, BottomX, t0);
                    float x1 = Mathf.Lerp(TopX, BottomX, t1);
                    float top = Mathf.Lerp(TopY, BottomY, t1);
                    Strip("Step", null, null, Color.Lerp(stone, theme.sootNavy, 0.05f * i), new Vector2((x0 + x1) * 0.5f, top * 0.5f), new Vector2(x1 - x0 + 2f, top));
                    Strip("Tread", null, null, tread, new Vector2((x0 + x1) * 0.5f, top - 1f), new Vector2(x1 - x0, 3f));
                }
                Strip("Floor", null, null, Color.Lerp(stone, theme.sootNavy, 0.35f), new Vector2((BottomX + 1920f) * 0.5f, BottomY * 0.5f), new Vector2(1920f - BottomX + 4f, BottomY));
                RectTransform rail = Strip("Rail", null, null, Color.Lerp(theme.ink, theme.agedPaper, 0.2f),
                    new Vector2((TopX + BottomX) * 0.5f, (TopY + BottomY) * 0.5f + 150f),
                    new Vector2(Vector2.Distance(new Vector2(TopX, TopY), new Vector2(BottomX, BottomY)) + 60f, 5f));
                rail.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(BottomY - TopY, BottomX - TopX) * Mathf.Rad2Deg);

                // İnsanlar: kenarda bir, arkasında iki, sahanlıkta üç.
                string[][] kinds =
                {
                    new[] { "il_woman_0", "il_woman_1", "il_woman_2", "il_woman_3" },
                    new[] { "il_child_0", "il_child_1", "il_child_2", "il_child_3" },
                    new[] { "il_man_0", "il_man_1", "il_man_2", "il_man_3" },
                    new[] { "il_greet_0", "il_greet_1", "il_greet_2", "il_greet_3" },
                    new[] { "il_man_0", "il_man_1", "il_man_2", "il_man_3" },
                    new[] { "il_woman_0", "il_woman_1", "il_woman_2", "il_woman_3" },
                    new[] { "il_child_0", "il_child_1", "il_child_2", "il_child_3" },
                };
                for (int i = 0; i < kinds.Length; i++)
                {
                    Person p = new Person { frames = kinds[i], phase = i * 0.7f, u = EdgeU - SlotSpacing * i };
                    p.scale = kinds[i][0].StartsWith("il_child") ? 0.46f : 0.60f;
                    p.image = Figure("Person", null, S(kinds[i][1]), Vector2.zero, new Vector2(240f * p.scale, 360f * p.scale), false);
                    p.rect = p.image.rectTransform;
                    people.Add(p);
                    Place(p);
                }

                // Karanlık, lambanın ışığı ve kapının sıcaklığı insanların üstünde değil, arkasında
                // olmalıydı; ama tek katmanlı arayüzde "arkası" yok. Bu yüzden karanlık figürleri de
                // örter, ışık halesi ise karanlığı açar: sonuç, lamba yandıkça silüetlerin belirmesi.
                darkness = Fill("Darkness", Vector2.zero, Vector2.one, new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.42f));
                doorGlow = Strip("Door Glow", null, S("il_puff"), new Color(theme.mustard.r, theme.mustard.g, theme.mustard.b, 0.22f), new Vector2(DoorX, BottomY + 160f), new Vector2(700f, 700f)).GetComponent<Image>();
                glow = Strip("Glow", null, S("il_puff"), new Color(theme.mustard.r, theme.mustard.g, theme.mustard.b, 0f), new Vector2(LampX, LampY - 30f), new Vector2(1300f, 1300f)).GetComponent<Image>();
                lampRect = Strip("Lamp", null, S("il_lamp"), Color.white, new Vector2(LampX, LampY + 10f), new Vector2(120f, 150f));
                lampRect.localScale = new Vector3(-1f, 1f, 1f);
                bulb = Strip("Bulb", null, theme.mapMarker, new Color(theme.mustard.r, theme.mustard.g, theme.mustard.b, 0.2f), new Vector2(LampX, LampY - 30f), new Vector2(22f, 22f)).GetComponent<Image>();
                progressRing = Ring("Progress", null, new Vector2(LampX, LampY - 10f), 190f, theme.mustard, true);
                progressRing.gameObject.SetActive(false);
                guideTarget = Ring("Guide Target", null, Vector2.zero, 64f, new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.55f), false);
                guideRing = Ring("Guide Ring", null, Vector2.zero, 64f, theme.mustard, false);
                guideTarget.gameObject.SetActive(false);
                guideRing.gameObject.SetActive(false);
            }

            private Vector2 PathPoint(float u)
            {
                if (u <= 0f) return new Vector2(TopX + u * LandingScale, TopY);
                if (u <= 1f) return new Vector2(Mathf.Lerp(TopX, BottomX, u), Mathf.Lerp(TopY, BottomY, u));
                return new Vector2(BottomX + (u - 1f) * LandingScale, BottomY);
            }

            private void Place(Person p)
            {
                Vector2 foot = PathPoint(p.u);
                float bob = p.descending ? Mathf.Abs(Mathf.Sin(p.phase * Mathf.PI * 0.5f)) * 4f : 0f;
                p.rect.anchoredPosition = foot + new Vector2(0f, bob);
            }

            private Person EdgePerson()
            {
                for (int i = 0; i < people.Count; i++)
                    if (!people[i].descending && !people[i].gone && Mathf.Abs(people[i].u - EdgeU) < 0.004f) return people[i];
                return null;
            }

            protected override void OnBegin()
            {
                task = app.IlRightPressed ? 1 : 0;
                ChoiceIndex = task;
                app.audioManager.PlayConfirm();
                SetCentreColor(theme.agedPaper);
                ShowCentre(node.choices != null && node.choices.Length > task ? node.choices[task].text : string.Empty, 1.4f);
                if (task == 0) progressRing.gameObject.SetActive(true);
                SetHint(task == 0 ? L(UiKey.IlWireHint) : L(UiKey.IlGuideHint));
            }

            protected override void Step(float dt)
            {
                if (task == 0) StepLamp(dt);
                else if (task == 1) StepQueue(dt);

                // Lamba ışığı: onarım ilerledikçe kesik kesik güçlenir; yanınca sabitlenir.
                float target = progress >= 1f ? 1f
                    : 0.12f + progress * 0.55f * (0.5f + 0.5f * Mathf.PerlinNoise(elapsed * 7f, 0.3f));
                if (progress < 1f && Mathf.PerlinNoise(elapsed * 3f, 7f) > 0.8f) target *= 0.3f;
                lampLevel = Mathf.MoveTowards(lampLevel, target, dt * (progress >= 1f ? 1.4f : 5f));
                flash = Mathf.MoveTowards(flash, 0f, dt * 3f);
                Color bulbColor = Color.Lerp(theme.mustard, Color.white, flash);
                bulb.color = new Color(bulbColor.r, bulbColor.g, bulbColor.b, 0.2f + 0.8f * Mathf.Max(lampLevel, flash));
                glow.color = new Color(theme.mustard.r, theme.mustard.g, theme.mustard.b, 0.05f + 0.40f * lampLevel + 0.25f * flash);
                glow.rectTransform.sizeDelta = Vector2.one * (1000f + 900f * lampLevel);
                darkness.color = new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, Mathf.Lerp(0.42f, 0.04f, lampLevel));
                lampRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 1.7f) * 1.5f + flash * Mathf.Sin(elapsed * 60f) * 3f);
                doorGlow.color = new Color(theme.mustard.r, theme.mustard.g, theme.mustard.b, 0.18f + 0.06f * Mathf.Sin(elapsed * 1.3f));

                // Sıra: kimse bir yere gitmediği sürece hepsi bekler; biri indiğinde arkadakiler bir yer ilerler.
                int slot = 0;
                for (int i = 0; i < people.Count; i++)
                {
                    Person p = people[i];
                    if (p.gone) continue;
                    bool moving = false;
                    if (p.descending || released)
                    {
                        p.descending = true;
                        p.u += 0.24f * dt;
                        moving = true;
                        if (PathPoint(p.u).x > DoorX)
                        {
                            p.gone = true;
                            p.image.enabled = false;
                        }
                    }
                    else
                    {
                        float targetU = EdgeU - SlotSpacing * slot;
                        slot++;
                        if (p.u < targetU - 0.001f)
                        {
                            p.u = Mathf.MoveTowards(p.u, targetU, 0.16f * dt);
                            moving = true;
                        }
                        else p.u = targetU;
                    }
                    if (moving) p.phase += 5.5f * dt;
                    Animate(p.image, p.frames, p.phase, moving);
                    Place(p);
                }

                for (int i = sparks.Count - 1; i >= 0; i--)
                {
                    Spark s = sparks[i];
                    s.life -= dt;
                    s.velocity += new Vector2(0f, -700f) * dt;
                    s.rect.anchoredPosition += s.velocity * dt;
                    s.image.color = new Color(1f, 0.9f, 0.6f, Mathf.Clamp01(s.life / 0.5f));
                    if (s.life <= 0f)
                    {
                        UnityEngine.Object.Destroy(s.rect.gameObject);
                        sparks.RemoveAt(i);
                    }
                }
            }

            private void StepLamp(float dt)
            {
                if (progress >= 1f) return;
                bool holding = app.IlActionHeld;
                if (stun > 0f) stun -= dt;
                if (warning >= 0f)
                {
                    warning += dt;
                    flash = Mathf.Max(flash, 0.5f + 0.5f * Mathf.Sin(warning * 40f));
                    if (!holding) releasedDuringWarning = true;
                    if (warning >= SparkWarning)
                    {
                        warning = -1f;
                        sparkIndex++;
                        if (releasedDuringWarning)
                        {
                            SetCentreColor(theme.agedPaper);
                            ShowCentre(L(UiKey.IlSparkPassed), 1f);
                        }
                        else
                        {
                            progress = Mathf.Max(0f, progress - 0.10f);
                            stun = 0.7f;
                            Shake(0.9f);
                            BurstSparks(14);
                            SetCentreColor(theme.rust);
                            ShowCentre(L(UiKey.IlSparkJolt), 1.1f);
                        }
                    }
                }
                else if (holding && stun <= 0f && Begun)
                {
                    progress = Mathf.Min(1f, progress + dt / RepairSeconds);
                    if (Rand01((int)(elapsed * 30f)) > 0.93f) BurstSparks(1);
                    if (sparkIndex < SparkAt.Length && progress >= SparkAt[sparkIndex])
                    {
                        warning = 0f;
                        releasedDuringWarning = false;
                        app.audioManager.PlaySpark();
                        SetCentreColor(theme.rust);
                        ShowCentre(L(UiKey.IlSparkWarn), SparkWarning);
                    }
                }
                progressRing.fillAmount = progress;
                progressRing.color = warning >= 0f ? Color.Lerp(theme.rust, Color.white, flash) : theme.mustard;
                if (progress >= 1f)
                {
                    released = true;
                    progressRing.gameObject.SetActive(false);
                    app.audioManager.PlayConfirm();
                    doneAt = elapsed + 1.2f;
                }
            }

            private void StepQueue(float dt)
            {
                if (guided >= GuideTarget) return;
                Person edge = EdgePerson();
                bool show = edge != null && Begun && elapsed > 1.2f;
                guideRing.gameObject.SetActive(show);
                guideTarget.gameObject.SetActive(show);
                if (!show)
                {
                    ringTime = 0f;
                    return;
                }
                // El: kişinin önünde, göğüs hizasında.
                Vector2 hand = edge.rect.anchoredPosition + new Vector2(62f * edge.scale / 0.6f, 150f * edge.scale / 0.6f);
                guideTarget.rectTransform.anchoredPosition = hand;
                guideRing.rectTransform.anchoredPosition = hand;
                if (ringPause > 0f)
                {
                    ringPause -= dt;
                    guideRing.color = new Color(theme.rust.r, theme.rust.g, theme.rust.b, 0.5f);
                    return;
                }
                ringTime += dt;
                float t = ringTime / RingCycle;
                float scale = Mathf.Lerp(2.8f, 0.55f, t);
                guideRing.rectTransform.sizeDelta = Vector2.one * 64f * scale;
                bool inWindow = scale <= 1.25f && scale >= 0.8f;
                guideRing.color = inWindow ? Color.Lerp(theme.mustard, Color.white, 0.5f) : theme.mustard;
                if (app.IlActionPressed || (app.interludeForcePush && inWindow))
                {
                    if (inWindow)
                    {
                        edge.descending = true;
                        guided++;
                        app.audioManager.PlayConfirm();
                        SetCentreColor(theme.agedPaper);
                        ShowCentre(L(UiKey.IlCaught) + "  " + guided + " / " + GuideTarget, 0.9f);
                        ringTime = 0f;
                        if (guided >= GuideTarget)
                        {
                            released = true;
                            guideRing.gameObject.SetActive(false);
                            guideTarget.gameObject.SetActive(false);
                            doneAt = elapsed + 1.4f;
                        }
                    }
                    else
                    {
                        SetCentreColor(theme.agedPaper);
                        ShowCentre(scale > 1.25f ? L(UiKey.IlEarly) : L(UiKey.IlLate), 0.8f);
                        ringTime = 0f;
                        ringPause = 0.5f;
                    }
                }
                else if (t >= 1f) ringTime = 0f;
            }

            private void BurstSparks(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    RectTransform rect = Strip("Spark", null, S("il_puff"), Color.white, new Vector2(LampX, LampY - 30f), new Vector2(7f, 7f));
                    float a = Rand01((int)(elapsed * 100f) + i * 13) * Mathf.PI * 2f;
                    float v = 160f + 260f * Rand01((int)(elapsed * 70f) + i * 7);
                    sparks.Add(new Spark { rect = rect, image = rect.GetComponent<Image>(), velocity = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * v, life = 0.5f });
                }
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
