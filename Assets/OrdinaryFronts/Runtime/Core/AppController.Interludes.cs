using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OrdinaryFronts
{
    /// <summary>
    /// Ara sahneler: kısa, hareketli ve tek girdili anlar. Anlatı kartı "ne olduğunu"
    /// söyler; ara sahne oyuncuya o anda "ne yaptığını" yaptırır. Puan, süre, başarı yok:
    /// sahne oyuncunun elini bir iki bayrağa çevirir ve sonraki yankılar o bayraklara bakar.
    /// <para>
    /// İki zamanlama vardır. <b>Giriş sahnesi</b> düğüme girilirken, metinden önce oynar ve
    /// yalnız bayrak üretir (Afsluitdijk rüzgârı). <b>Karar sahnesi</b> (<c>chooses</c>)
    /// oyuncu seçim yapacağı anda oynar ve düğümün iki seçiminden birini oyuncunun
    /// hareketiyle verir (sığınak merdiveninde lamba mı sıra mı; kayan tahtada tutmak mı
    /// bırakmak mı). Düğüm yine tam iki seçimlidir; değişen, seçimin tuşla değil elle
    /// verilmesidir.
    /// </para>
    /// <para>
    /// Kurallar: her sahne Esc ile geçilir (karar sahnesi geçilirse seçim düğmelere döner);
    /// hareket azaltma açıkken oynanmaz; bir kez oynandığı kayda yazılır; hikâye grafına
    /// dokunmaz.
    /// </para>
    /// </summary>
    public sealed partial class AppController
    {
        private GameObject interludeScreen;
        private bool interludeActive;
        private bool interludeSkipRequested;
        private Coroutine interludeRoutine;

        /// <summary>QA: girdi yerine sürekli "birinci hareket" sayılır; duman koşusunda sahneyi ilerletmek için.</summary>
        private bool interludeForcePush;

        private void BuildInterludeScreen(Transform parent)
        {
            interludeScreen = CreateScreen("Interlude", parent);
            router.Register(AppScreen.Interlude, interludeScreen);
        }

        private bool InterludePending(StoryNode node)
        {
            if (node == null || !node.HasInterlude || storyController == null || storyController.State == null) return false;
            if (!StoryVocabulary.IsKnownInterludeKind(node.interlude.kind)) return false;
            return !storyController.State.HasSeenResult(StoryVocabulary.InterludeSeenKey(node.interlude.id));
        }

        /// <summary>Düğüme girilirken oynanacak giriş sahnesi var mı?</summary>
        private bool ShouldPlayInterlude(StoryNode node)
        {
            return InterludePending(node) && !node.interlude.chooses;
        }

        /// <summary>Seçim anında oynanacak karar sahnesi var mı?</summary>
        private bool ShouldPlayChoosingInterlude(StoryNode node)
        {
            return InterludePending(node) && node.interlude.chooses;
        }

        private IEnumerator RunInterludeThenRender(StoryNode node, bool animate)
        {
            yield return RunInterlude(node);
            interludeRoutine = null;
            RenderNode(node, animate);
        }

        /// <summary>
        /// Karar sahnesi: sahne oynar, oyuncunun hareketi seçimi verir ve olağan seçim
        /// akışı (iz, etki, kayıt, geçiş) o seçimle devam eder. Geçilirse seçim düğmelere
        /// döner; sahne bir daha açılmaz.
        /// </summary>
        private IEnumerator RunChoosingInterlude(StoryNode node)
        {
            transitionBusy = true;
            InterludeScene scene = null;
            yield return RunInterlude(node, s => scene = s);
            int index = scene == null ? -1 : scene.ChoiceIndex;
            if (index >= 0 && index < 2 && choiceButtons[index] != null && choiceButtons[index].interactable)
            {
                yield return AdvanceChoice(index);
                yield break;
            }
            RefreshChoiceKeyLabels(node);
            router.Show(AppScreen.Gameplay);
            transitionBusy = false;
        }

        private IEnumerator RunInterlude(StoryNode node)
        {
            return RunInterlude(node, null);
        }

        /// <summary>
        /// Sahneyi oynatır, sonucu kayda yazar ve ekranı temizler. Hareket azaltma açıksa
        /// yalnız "oynandı" işareti yazılır: oyuncu bir şey kaçırmaz, sonraki yankılar
        /// sonuçsuz kalır ve metin buna göre sessiz kalır.
        /// </summary>
        private IEnumerator RunInterlude(StoryNode node, Action<InterludeScene> report)
        {
            InterludeData data = node.interlude;
            if (settings != null && settings.reduceMotion)
            {
                storyController.RecordInterlude(data, null);
                yield break;
            }

            interludeActive = true;
            interludeSkipRequested = false;
            router.Show(AppScreen.Interlude);
            audioManager.PlayAmbienceFor(node.imageKey);

            InterludeScene scene = CreateInterludeScene(node);
            if (scene != null)
            {
                yield return scene.Run();
                storyController.RecordInterlude(data, scene.Result);
                if (report != null) report(scene);
                scene.Dispose();
            }
            else storyController.RecordInterlude(data, null);
            interludeActive = false;
        }

        private InterludeScene CreateInterludeScene(StoryNode node)
        {
            switch (StoryVocabulary.Normalize(node.interlude.kind))
            {
                case StoryVocabulary.InterludeKindWalk: return new WalkInterlude(this, node);
                case StoryVocabulary.InterludeKindLamp: return new LampInterlude(this, node);
                case StoryVocabulary.InterludeKindPlank: return new PlankInterlude(this, node);
                case StoryVocabulary.InterludeKindBoard: return new BoardInterlude(this, node);
                default: return null;
            }
        }

        /// <summary>
        /// Karar sahnesi bekleyen düğümde tuş etiketleri bunu söyler: iki seçenek görünür,
        /// hangisi olacağına oyuncunun eli karar verir. Sahne geçildikten sonra etiketler
        /// olağan hâline döner.
        /// </summary>
        private void RefreshChoiceKeyLabels(StoryNode node)
        {
            bool byHand = ShouldPlayChoosingInterlude(node);
            string suffix = byHand ? "     ·     " + (localization == null ? T(UiKey.InterludeChoiceHint) : localization.ToUpper(T(UiKey.InterludeChoiceHint))) : string.Empty;
            if (choiceKeyLabels[0] != null) choiceKeyLabels[0].text = InputGlyphs.Left(inputDevice, null) + suffix;
            if (choiceKeyLabels[1] != null) choiceKeyLabels[1].text = InputGlyphs.Right(inputDevice, null) + suffix;
        }

        // ------------------------------------------------------------------ girdi

        // Ara sahnelerin tek kontrol dili, oynanışla aynı: A/← sol seçenek, D/→ sağ seçenek;
        // BOŞLUK ya da sol tık "elinle yap" (it, bağla, tut). Sağ fare ve fare ekseni yok.

        private bool IlActionHeld
        {
            get { return interludeForcePush || Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0) || PadActionHeld; }
        }

        private bool IlActionPressed
        {
            get { return Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || PadActionDown; }
        }

        /// <summary>El sahneleri modu: kolay (geniş zamanlama) ya da eller serbest.</summary>
        private int HandMode { get { return settings == null ? 0 : Mathf.Clamp(settings.handScenes, 0, 2); } }
        private bool IlGentle { get { return HandMode >= SettingsData.HandScenesGentle; } }

        /// <summary>
        /// Eller serbest: basılı tutma ve zamanlama kendiliğinden yapılır. Karar anları asla
        /// kendiliğinden verilmez: iki seçenekten biri, boranın ortasında itmek, kayan tahtada
        /// tutmak oyuncunun tek bir basışını bekler.
        /// </summary>
        private bool IlAuto { get { return HandMode == SettingsData.HandScenesFree; } }

        private bool IlLeftHeld { get { return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || PadLeftHeld; } }
        private bool IlRightHeld { get { return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || PadRightHeld; } }
        private bool IlLeftPressed { get { return Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) || PadLeftDown; } }
        private bool IlRightPressed { get { return Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || PadRightDown; } }

        /// <summary>Ara sahnenin dinlediği herhangi bir tuş şu an basılı mı? (Girdi kilidi için.)</summary>
        private bool IlAnyHeld
        {
            get { return Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0) || PadActionHeld || IlLeftHeld || IlRightHeld; }
        }

        private Sprite InterludeSprite(string key)
        {
            Sprite sprite;
            return artIndex.TryGetValue(key, out sprite) ? sprite : null;
        }

        // ------------------------------------------------------------------ sahne tabanı

        /// <summary>
        /// Bütün ara sahnelerin ortak iskeleti.
        /// <list type="number">
        /// <item><b>Girdi kilidi.</b> Sahne açıldığında basılı olan tuşlar (seçimi açan D, kartı
        /// geçen boşluk) sahneye sızmaz: bütün tuşlar bir kez bırakılana kadar girdi yok sayılır.</item>
        /// <item><b>Talimat kartı.</b> Başlık, tek cümlelik "nasıl" ve tuş kapakları. Sahne,
        /// oyuncu ilk hareketi yapana kadar bekler; hiçbir şey oyuncu hazır olmadan başlamaz.
        /// Karar sahnelerinde kart düğümün iki seçeneğini A ve D kapaklarıyla gösterir.</item>
        /// <item><b>Anlık komutlar ve sonuç.</b> Ortada kısa, büyük yazı ("BIRAK!", "Tuttun.");
        /// sahne bitince yapılanın tek satırlık karşılığı bir an ekranda kalır.</item>
        /// <item><b>Çerçeve.</b> Açılış/kapanış solması, tarih·yer damgası, açılış satırı, altta
        /// o anki ipucu ve "Esc: geç", vinyet ve film greni.</item>
        /// </list>
        /// </summary>
        private abstract class InterludeScene
        {
            protected readonly AppController app;
            protected readonly StoryNode node;
            protected readonly InterludeData data;
            protected readonly ThemeConfig theme;
            protected readonly GameObject root;
            protected readonly CanvasGroup rootGroup;
            protected float elapsed;
            protected float playTime;

            private bool armed;
            private bool begun;
            private Image grain;
            private float grainTimer;
            private int grainFrame;
            private CanvasGroup captionGroup;
            private TMP_Text hint;
            private TMP_Text centre;
            private CanvasGroup centreGroup;
            private float centreUntil;
            private GameObject instructions;
            private CanvasGroup instructionsGroup;
            private RectTransform shakeTarget;
            private float shake;
            private int hintGlyphVersion;

            public string Result { get; protected set; }
            public int ChoiceIndex { get; protected set; }

            protected bool Begun { get { return begun; } }

            protected InterludeScene(AppController app, StoryNode node)
            {
                this.app = app;
                this.node = node;
                data = node.interlude;
                theme = app.theme;
                ChoiceIndex = -1;
                root = CreateRect("Scene", app.interludeScreen.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                rootGroup = root.AddComponent<CanvasGroup>();
                GameObject stage = CreateRect("Stage", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                shakeTarget = stage.GetComponent<RectTransform>();
                Stage = stage.transform;
                Build();
                BuildFrame();
            }

            /// <summary>Sahne öğelerinin ebeveyni; sarsıntı bunu oynatır, çerçeveyi değil.</summary>
            protected Transform Stage { get; private set; }

            protected abstract void Build();
            protected abstract void Step(float dt);
            protected abstract bool Finished { get; }
            protected abstract void Resolve();
            protected abstract string Title { get; }
            protected abstract string HowTo { get; }
            protected abstract string HintText { get; }

            /// <summary>Sahneyi başlatan ilk hareket; talimat kartı bununla kapanır.</summary>
            protected abstract bool StartPressed { get; }

            /// <summary>Karar sahneleri kartta iki seçeneği gösterir.</summary>
            protected virtual bool ShowsChoices { get { return false; } }

            /// <summary>Sahne bitince bir an görünen tek satır; boşsa gösterilmez.</summary>
            protected virtual string OutcomeText { get { return null; } }

            /// <summary>Sahnenin başladığı an; türler ilk durumlarını burada kurar.</summary>
            protected virtual void OnBegin() { }

            /// <summary>Kilit açıldı mı: sahne açıldığında basılı olan tuşlar bir kez bırakıldı.</summary>
            protected bool Armed { get { return armed; } }

            public void Dispose()
            {
                if (root != null) UnityEngine.Object.Destroy(root);
            }

            public IEnumerator Run()
            {
                rootGroup.alpha = 0f;
                for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
                {
                    rootGroup.alpha = t / 0.5f;
                    yield return null;
                }
                rootGroup.alpha = 1f;
                while (!app.interludeSkipRequested && !Finished)
                {
                    float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                    elapsed += dt;
                    if (!armed) armed = app.interludeForcePush || !app.IlAnyHeld;
                    if (!begun && armed && elapsed > 0.6f && (app.interludeForcePush ? elapsed > 2.2f : StartPressed))
                    {
                        begun = true;
                        OnBegin();
                    }
                    if (begun) playTime += dt;
                    Step(dt);
                    StepFrame(dt);
                    yield return null;
                }
                if (!app.interludeSkipRequested)
                {
                    Resolve();
                    string outcome = OutcomeText;
                    if (!string.IsNullOrEmpty(outcome))
                    {
                        ShowCentre(outcome, 1.8f);
                        for (float t = 0f; t < 1.8f && !app.interludeSkipRequested; t += Time.unscaledDeltaTime)
                        {
                            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                            elapsed += dt;
                            Step(dt);
                            StepFrame(dt);
                            yield return null;
                        }
                    }
                }
                for (float t = 0f; t < 0.45f; t += Time.unscaledDeltaTime)
                {
                    rootGroup.alpha = 1f - t / 0.45f;
                    yield return null;
                }
                rootGroup.alpha = 0f;
            }

            private void BuildFrame()
            {
                Image vignette = Fill("Vignette", Vector2.zero, Vector2.one, Color.white, root.transform);
                vignette.sprite = theme.vignette;
                vignette.color = new Color(1f, 1f, 1f, 0.85f);
                Sprite firstGrain = theme.introGrain != null && theme.introGrain.Length > 0 ? theme.introGrain[0] : null;
                grain = Fill("Grain", Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0.16f), root.transform);
                grain.sprite = firstGrain;
                grain.enabled = firstGrain != null;

                string stamp = ((node.date ?? string.Empty) + "   ·   " + (node.location ?? string.Empty)).Trim();
                Fill("Stamp Tag", new Vector2(0.03f, 0.895f), new Vector2(0.44f, 0.955f), new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.62f), root.transform);
                TMP_Text kicker = (TMP_Text)app.AsDocument(app.CreateText("Stamp", root.transform,
                    app.localization == null ? stamp : app.localization.ToUpper(stamp), 15f, FontStyles.Bold, theme.mustard,
                    new Vector2(0.04f, 0.90f), new Vector2(0.70f, 0.95f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left));
                kicker.characterSpacing = 3f;

                GameObject captionObject = CreateRect("Caption", root.transform, new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.885f), Vector2.zero, Vector2.zero);
                captionGroup = captionObject.AddComponent<CanvasGroup>();
                Fill("Caption Shade", new Vector2(0.05f, 0f), new Vector2(0.95f, 1f), new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.45f), captionObject.transform);
                TMP_Text caption = app.CreateText("Caption Text", captionObject.transform, data.caption ?? string.Empty, 32f, FontStyles.Normal, theme.agedPaper,
                    new Vector2(0.07f, 0f), new Vector2(0.93f, 1f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
                caption.enableAutoSizing = true;
                caption.fontSizeMin = 22f;
                caption.fontSizeMax = 32f;

                GameObject centreObject = CreateRect("Centre", root.transform, new Vector2(0.15f, 0.56f), new Vector2(0.85f, 0.70f), Vector2.zero, Vector2.zero);
                centreGroup = centreObject.AddComponent<CanvasGroup>();
                centreGroup.alpha = 0f;
                Fill("Centre Shade", new Vector2(0.12f, 0.1f), new Vector2(0.88f, 0.9f), new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.55f), centreObject.transform);
                centre = app.CreateText("Centre Text", centreObject.transform, string.Empty, 40f, FontStyles.Bold, theme.agedPaper,
                    new Vector2(0.14f, 0f), new Vector2(0.86f, 1f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
                centre.enableAutoSizing = true;
                centre.fontSizeMin = 24f;
                centre.fontSizeMax = 40f;

                hint = (TMP_Text)app.AsDocument(app.CreateText("Hint", root.transform, string.Empty, 15f, FontStyles.Bold,
                    new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.72f),
                    new Vector2(0.05f, 0.022f), new Vector2(0.95f, 0.07f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center));
                hint.characterSpacing = 2f;
                SetHint(HintText);

                BuildInstructions();
            }

            /// <summary>
            /// Talimat kartı: ekranın alt ortasında koyu bir pano. Başlık (daktilo, hardal),
            /// tek cümlelik "nasıl" (serif) ve tuş kapakları. Karar sahnelerinde iki sütun:
            /// A kapağı ve düğümün birinci seçeneği, D kapağı ve ikinci seçeneği.
            /// </summary>
            private void BuildInstructions()
            {
                bool choices = ShowsChoices && node.choices != null && node.choices.Length >= 2;
                instructions = CreateRect("Instructions", root.transform, new Vector2(0.22f, 0.10f), new Vector2(0.78f, choices ? 0.42f : 0.33f), Vector2.zero, Vector2.zero);
                instructionsGroup = instructions.AddComponent<CanvasGroup>();
                Fill("Panel", Vector2.zero, Vector2.one, new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.88f), instructions.transform);
                Fill("Rule", new Vector2(0f, 0.985f), Vector2.one, theme.rust, instructions.transform);
                string title = app.localization == null ? Title : app.localization.ToUpper(Title);
                TMP_Text titleText = (TMP_Text)app.AsDocument(app.CreateText("Title", instructions.transform, title, 18f, FontStyles.Bold, theme.mustard,
                    new Vector2(0.05f, choices ? 0.80f : 0.72f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center));
                titleText.characterSpacing = 6f;
                TMP_Text how = app.CreateText("How", instructions.transform, HowTo, 24f, FontStyles.Normal, theme.agedPaper,
                    new Vector2(0.06f, choices ? 0.54f : 0.14f), new Vector2(0.94f, choices ? 0.80f : 0.72f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
                how.enableAutoSizing = true;
                how.fontSizeMin = 17f;
                how.fontSizeMax = 24f;
                if (!choices) return;
                BuildChoiceColumn(new Vector2(0.04f, 0.07f), new Vector2(0.485f, 0.50f), InputGlyphs.Left(app.inputDevice, null), node.choices[0].text);
                BuildChoiceColumn(new Vector2(0.515f, 0.07f), new Vector2(0.96f, 0.50f), InputGlyphs.Right(app.inputDevice, null), node.choices[1].text);
            }

            private void BuildChoiceColumn(Vector2 min, Vector2 max, string key, string label)
            {
                GameObject column = CreateRect("Choice", instructions.transform, min, max, Vector2.zero, Vector2.zero);
                Fill("Box", Vector2.zero, Vector2.one, new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.08f), column.transform);
                Keycap(column.transform, key, new Vector2(0.04f, 0.28f), new Vector2(0.26f, 0.72f));
                TMP_Text text = app.CreateText("Label", column.transform, label, 20f, FontStyles.Bold, theme.agedPaper,
                    new Vector2(0.30f, 0.05f), new Vector2(0.97f, 0.95f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
                text.enableAutoSizing = true;
                text.fontSizeMin = 14f;
                text.fontSizeMax = 20f;
            }

            /// <summary>Tuş kapağı: ince kâğıt çerçeve, içinde daktilo harf.</summary>
            protected void Keycap(Transform parent, string key, Vector2 min, Vector2 max)
            {
                GameObject cap = CreateRect("Key", parent, min, max, Vector2.zero, Vector2.zero);
                Color line = new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.8f);
                Fill("Top", new Vector2(0f, 1f), Vector2.one, line, cap.transform).rectTransform.offsetMin = new Vector2(0f, -2f);
                Fill("Bottom", Vector2.zero, new Vector2(1f, 0f), line, cap.transform).rectTransform.offsetMax = new Vector2(0f, 4f);
                Fill("Left", Vector2.zero, new Vector2(0f, 1f), line, cap.transform).rectTransform.offsetMax = new Vector2(2f, 0f);
                Fill("Right", new Vector2(1f, 0f), Vector2.one, line, cap.transform).rectTransform.offsetMin = new Vector2(-2f, 0f);
                TMP_Text text = (TMP_Text)app.AsDocument(app.CreateText("Letter", cap.transform, key, 22f, FontStyles.Bold, theme.agedPaper,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center));
                text.overflowMode = TextOverflowModes.Overflow;
                text.enableWordWrapping = false;
            }

            private void StepFrame(float dt)
            {
                if (captionGroup != null)
                    captionGroup.alpha = elapsed < 4.2f ? Mathf.Clamp01((elapsed - 0.3f) / 0.8f) : Mathf.Clamp01(1f - (elapsed - 4.2f) / 0.9f);
                if (centreGroup != null)
                    centreGroup.alpha = Mathf.MoveTowards(centreGroup.alpha, elapsed < centreUntil ? 1f : 0f, dt * 5f);
                if (instructionsGroup != null)
                {
                    float target = begun ? 0f : Mathf.Clamp01((elapsed - 0.4f) / 0.5f);
                    instructionsGroup.alpha = Mathf.MoveTowards(instructionsGroup.alpha, target, dt * (begun ? 4f : 3f));
                    if (begun && instructionsGroup.alpha <= 0f) instructions.SetActive(false);
                }
                // Oyuncu sahnenin ortasında cihaz değiştirdiyse ipucu yeni cihazın diliyle yazılır.
                if (hintGlyphVersion != app.glyphVersion) SetHint(HintText);
                shake = Mathf.MoveTowards(shake, 0f, dt * 2.5f);
                shakeTarget.anchoredPosition = shake > 0f
                    ? new Vector2(Mathf.Sin(elapsed * 71f) * 9f * shake, Mathf.Cos(elapsed * 59f) * 6f * shake)
                    : Vector2.zero;
                if (grain != null && grain.enabled && theme.introGrain != null && theme.introGrain.Length > 0)
                {
                    grainTimer += dt;
                    if (grainTimer >= 0.14f)
                    {
                        grainTimer = 0f;
                        grainFrame = (grainFrame + 1) % theme.introGrain.Length;
                        grain.sprite = theme.introGrain[grainFrame];
                    }
                }
            }

            protected void Shake(float amount) { shake = Mathf.Max(shake, amount); }

            protected void ShowCentre(string text, float seconds)
            {
                if (centre == null) return;
                centre.text = text;
                centreUntil = elapsed + seconds;
            }

            protected void SetCentreColor(Color color) { if (centre != null) centre.color = color; }

            protected void SetHint(string text)
            {
                hintGlyphVersion = app.glyphVersion;
                if (hint != null) hint.text = text + "     ·     " + app.TG(UiKey.InterludeSkipHint);
            }

            // ------------------------------------------------------------ çizim yardımcıları

            protected Image Fill(string name, Vector2 anchorMin, Vector2 anchorMax, Color color, Transform parent = null)
            {
                Image image = CreateRect(name, parent ?? Stage, anchorMin, anchorMax, Vector2.zero, Vector2.zero).AddComponent<Image>();
                image.color = color;
                image.raycastTarget = false;
                return image;
            }

            /// <summary>Referans piksel konumuyla yerleştirilen sprite; çapa sol alt, pivot orta.</summary>
            protected RectTransform Strip(string name, Transform parent, Sprite sprite, Color color, Vector2 position, Vector2 size)
            {
                GameObject go = CreateRect(name, parent ?? Stage, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
                Image image = go.AddComponent<Image>();
                image.sprite = sprite;
                image.color = color;
                image.raycastTarget = false;
                return rect;
            }

            /// <summary>Ayak noktasıyla yerleştirilen figür; pivot alt orta, isteğe bağlı yatay çevirme.</summary>
            protected Image Figure(string name, Transform parent, Sprite sprite, Vector2 footPosition, Vector2 size, bool flipped)
            {
                GameObject go = CreateRect(name, parent ?? Stage, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = footPosition;
                rect.sizeDelta = size;
                if (flipped) rect.localScale = new Vector3(-1f, 1f, 1f);
                Image image = go.AddComponent<Image>();
                image.sprite = sprite;
                image.color = Color.white;
                image.raycastTarget = false;
                return image;
            }

            /// <summary>Radyal dolan ya da küçülen halka: zamanlama ve ilerleme göstergesi.</summary>
            protected Image Ring(string name, Transform parent, Vector2 position, float size, Color color, bool radialFill)
            {
                RectTransform rect = Strip(name, parent, S("il_ring"), color, position, new Vector2(size, size));
                Image image = rect.GetComponent<Image>();
                if (radialFill)
                {
                    image.type = Image.Type.Filled;
                    image.fillMethod = Image.FillMethod.Radial360;
                    image.fillOrigin = (int)Image.Origin360.Top;
                    image.fillClockwise = true;
                    image.fillAmount = 0f;
                }
                return image;
            }

            protected Sprite S(string key) { return app.InterludeSprite(key); }

            protected static void Drift(List<RectTransform> items, float dx, float wrapAt, float resetTo)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    Vector2 p = items[i].anchoredPosition;
                    p.x -= dx;
                    if (p.x + items[i].sizeDelta.x * 0.5f < resetTo) p.x = wrapAt + items[i].sizeDelta.x * 0.5f;
                    items[i].anchoredPosition = p;
                }
            }

            protected static float Rand01(int seed)
            {
                float v = Mathf.Sin(seed * 12.9898f + 78.233f) * 43758.5453f;
                return v - Mathf.Floor(v);
            }

            /// <summary>Dört karelik yürüyüş döngüsü; hareketsizken orta kare.</summary>
            protected void Animate(Image image, string[] frames, float phase, bool moving)
            {
                int frame = moving ? (int)(phase % 4f) : 1;
                Sprite sprite = S(frames[frame]);
                if (sprite != null && image.sprite != sprite) image.sprite = sprite;
            }

            /// <summary>Çeviri + tuş yer tutucuları: ipuçları oyuncunun cihazına göre yazılır.</summary>
            protected string L(string key) { return app.TG(key); }

            /// <summary>Bir el işinin ipucu; eller serbestken yerine "kendiliğinden" satırı.</summary>
            protected string HandHint(string key) { return app.IlAuto ? L(UiKey.IlAutoHint) : L(key); }

            /// <summary>Kolay ya da eller serbest modda standart değerin yerine geçen değer.</summary>
            protected float Ease(float standard, float gentle) { return app.IlGentle ? gentle : standard; }
        }
    }
}
