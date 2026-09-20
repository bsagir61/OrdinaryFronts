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
            if (choiceKeyLabels[0] != null) choiceKeyLabels[0].text = "A  /  ←" + suffix;
            if (choiceKeyLabels[1] != null) choiceKeyLabels[1].text = "D  /  →" + suffix;
        }

        // ------------------------------------------------------------------ girdi

        /// <summary>Birinci hareket: D / → / boşluk / sol fare (itmek, tutmak, sağa dengelemek).</summary>
        private bool InterludePrimaryHeld
        {
            get
            {
                if (interludeForcePush) return true;
                return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);
            }
        }

        /// <summary>İkinci hareket: A / ← / sağ fare (sola dengelemek, lambaya dönmek).</summary>
        private bool InterludeSecondaryHeld
        {
            get { return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || Input.GetMouseButton(1); }
        }

        private bool InterludePrimaryPressed
        {
            get { return Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0); }
        }

        /// <summary>Fare yatayı: -1 sol kenar, +1 sağ kenar; denge sahnesi bunu da okur.</summary>
        private float InterludeMouseAxis
        {
            get { return Mathf.Clamp((Input.mousePosition.x / Mathf.Max(1f, Screen.width)) * 2f - 1f, -1f, 1f); }
        }

        private Sprite InterludeSprite(string key)
        {
            Sprite sprite;
            return artIndex.TryGetValue(key, out sprite) ? sprite : null;
        }

        // ------------------------------------------------------------------ sahne tabanı

        /// <summary>
        /// Bütün ara sahnelerin ortak iskeleti: tam ekran kök, açılış/kapanış solması, üstte
        /// tarih·yer damgası ve açılış satırı, altta tuş ipucu, üstüne oyunun kendi vinyet ve
        /// gren katmanı. Türler yalnız <see cref="Build"/>, <see cref="Step"/>,
        /// <see cref="Finished"/> ve <see cref="Resolve"/> yazar.
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

            private Image grain;
            private float grainTimer;
            private int grainFrame;
            private CanvasGroup captionGroup;
            private TMP_Text hint;
            private TMP_Text centre;
            private CanvasGroup centreGroup;
            private float centreUntil;

            /// <summary>Üretilen bayrak; yok ise null.</summary>
            public string Result { get; protected set; }

            /// <summary>Karar sahnesinde verilen seçim; -1 ise verilmedi.</summary>
            public int ChoiceIndex { get; protected set; }

            protected InterludeScene(AppController app, StoryNode node)
            {
                this.app = app;
                this.node = node;
                data = node.interlude;
                theme = app.theme;
                ChoiceIndex = -1;
                root = CreateRect("Scene", app.interludeScreen.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                rootGroup = root.AddComponent<CanvasGroup>();
                Build();
                BuildFrame();
            }

            protected abstract void Build();
            protected abstract void Step(float dt);
            protected abstract bool Finished { get; }
            protected abstract void Resolve();
            protected abstract string HintText { get; }

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
                    Step(dt);
                    StepFrame(dt);
                    yield return null;
                }
                if (!app.interludeSkipRequested) Resolve();
                for (float t = 0f; t < 0.45f; t += Time.unscaledDeltaTime)
                {
                    rootGroup.alpha = 1f - t / 0.45f;
                    yield return null;
                }
                rootGroup.alpha = 0f;
            }

            /// <summary>Damga, açılış satırı, ipucu, vinyet ve gren; sahnenin üstüne çizilir.</summary>
            private void BuildFrame()
            {
                Image vignette = Fill("Vignette", Vector2.zero, Vector2.one, Color.white);
                vignette.sprite = theme.vignette;
                vignette.color = new Color(1f, 1f, 1f, 0.85f);
                Sprite firstGrain = theme.introGrain != null && theme.introGrain.Length > 0 ? theme.introGrain[0] : null;
                grain = Fill("Grain", Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0.16f));
                grain.sprite = firstGrain;
                grain.enabled = firstGrain != null;

                string stamp = ((node.date ?? string.Empty) + "   ·   " + (node.location ?? string.Empty)).Trim();
                // Damganın arkasında koyu bir etiket: açık gökte de okunur.
                Fill("Stamp Tag", new Vector2(0.03f, 0.895f), new Vector2(0.44f, 0.955f), new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.62f));
                TMP_Text kicker = (TMP_Text)app.AsDocument(app.CreateText("Stamp", root.transform,
                    app.localization == null ? stamp : app.localization.ToUpper(stamp), 15f, FontStyles.Bold, theme.mustard,
                    new Vector2(0.04f, 0.90f), new Vector2(0.70f, 0.95f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left));
                kicker.characterSpacing = 3f;

                GameObject captionObject = CreateRect("Caption", root.transform, new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.885f), Vector2.zero, Vector2.zero);
                captionGroup = captionObject.AddComponent<CanvasGroup>();
                TMP_Text caption = app.CreateText("Caption Text", captionObject.transform, data.caption ?? string.Empty, 34f, FontStyles.Normal, theme.agedPaper,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
                caption.enableAutoSizing = true;
                caption.fontSizeMin = 22f;
                caption.fontSizeMax = 34f;

                GameObject centreObject = CreateRect("Centre", root.transform, new Vector2(0.15f, 0.56f), new Vector2(0.85f, 0.70f), Vector2.zero, Vector2.zero);
                centreGroup = centreObject.AddComponent<CanvasGroup>();
                centreGroup.alpha = 0f;
                centre = app.CreateText("Centre Text", centreObject.transform, string.Empty, 40f, FontStyles.Bold, theme.agedPaper,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
                centre.enableAutoSizing = true;
                centre.fontSizeMin = 26f;
                centre.fontSizeMax = 40f;

                hint = (TMP_Text)app.AsDocument(app.CreateText("Hint", root.transform,
                    HintText + "     ·     " + app.T(UiKey.InterludeSkipHint), 15f, FontStyles.Bold,
                    new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.66f),
                    new Vector2(0.05f, 0.025f), new Vector2(0.95f, 0.075f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center));
                hint.characterSpacing = 2f;
            }

            private void StepFrame(float dt)
            {
                if (captionGroup != null)
                    captionGroup.alpha = elapsed < 4.2f ? Mathf.Clamp01((elapsed - 0.3f) / 0.8f) : Mathf.Clamp01(1f - (elapsed - 4.2f) / 0.9f);
                if (centreGroup != null)
                    centreGroup.alpha = Mathf.MoveTowards(centreGroup.alpha, elapsed < centreUntil ? 1f : 0f, dt * 4f);
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

            /// <summary>Ekranın ortasında kısa süreli büyük yazı: "tut ya da bırak" gibi anlık komutlar.</summary>
            protected void ShowCentre(string text, float seconds)
            {
                if (centre == null) return;
                centre.text = text;
                centreUntil = elapsed + seconds;
            }

            protected void SetHint(string text)
            {
                if (hint != null) hint.text = text + "     ·     " + app.T(UiKey.InterludeSkipHint);
            }

            // ------------------------------------------------------------ çizim yardımcıları

            protected Image Fill(string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
            {
                Image image = CreateRect(name, root.transform, anchorMin, anchorMax, Vector2.zero, Vector2.zero).AddComponent<Image>();
                image.color = color;
                image.raycastTarget = false;
                return image;
            }

            /// <summary>Referans piksel konumuyla yerleştirilen sprite; çapa sol alt, pivot orta.</summary>
            protected RectTransform Strip(string name, Transform parent, Sprite sprite, Color color, Vector2 position, Vector2 size)
            {
                GameObject go = CreateRect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
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
                GameObject go = CreateRect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
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
        }
    }
}
