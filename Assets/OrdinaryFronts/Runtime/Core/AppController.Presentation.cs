using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OrdinaryFronts
{
    /// <summary>
    /// 1.1 sunum katmanı. Hikâyeye, karara ya da kayda dokunmaz; yalnız görüneni değiştirir:
    /// <list type="bullet">
    /// <item><b>Arka plan geçişi ve yavaş kamera.</b> Sahne değiştiğinde eski görsel yenisinin
    /// üstünde söner; görsel sürekli, çok yavaş bir nefes (ölçek ve kayma) içinde durur.</item>
    /// <item><b>Atmosfer.</b> Her sahnenin kendi havası vardır: kar, kül ve kıvılcım, toz,
    /// yağmur, sis, buhar; üstüne sahneye özgü hafif bir renk ayarı.</item>
    /// <item><b>Perde kartları.</b> Bölüm başında ve perde değiştiğinde tam ekran başlık:
    /// "PERDE II · Kül", tarih ve yer.</item>
    /// <item><b>Mürekkep açılışı.</b> Anlatı metni kâğıda mürekkep yayılır gibi, yumuşak bir
    /// ön cephe ile belirir; herhangi bir tuş tamamlar.</item>
    /// <item><b>Canlı ana menü.</b> Bölümlerin görselleri yer ve tarih altyazısıyla dönüşümlü
    /// geçer.</item>
    /// <item><b>Mühürlü final.</b> Final raporu bir dosya gibi kapanır: bölüm künyesi ve
    /// "DOSYA KAPANDI" mührü.</item>
    /// </list>
    /// Hareket azaltma açıkken bütün hareket durur: geçişler kesme olur, parçacıklar kapanır,
    /// perde kartı hareketsiz görünür, metin anında gelir.
    /// </summary>
    public sealed partial class AppController
    {
        private const float BackgroundCrossfadeSeconds = 1.1f;
        private const float DioramaSlideSeconds = 9f;

        private Image backgroundFade;
        private float backgroundFadeAlpha;
        private float presentationClock;

        private RectTransform atmosphereRoot;
        private CanvasGroup atmosphereGroup;
        private Image atmosphereGrade;
        private readonly List<AtmosphereParticle> atmosphereParticles = new List<AtmosphereParticle>();
        private string atmosphereAppliedKey;
        private string atmospherePendingKey;
        private float atmosphereFade = 1f;

        private GameObject actCard;
        private CanvasGroup actCardGroup;
        private TMP_Text actNumberText;
        private TMP_Text actNameText;
        private TMP_Text actMetaText;
        private RectTransform actRule;
        private bool actCardActive;
        private bool actCardSkip;
        private float actCardArmTime;
        private bool pendingActCard;
        private TMP_Text actQuestionText;

        private Coroutine revealRoutine;
        private bool bodyRevealing;
        private bool revealFinishRequested;

        private readonly List<StoryCatalogEntry> dioramaSlides = new List<StoryCatalogEntry>();
        private int dioramaIndex;
        private float dioramaTimer;
        private TMP_Text dioramaCaption;
        private CanvasGroup dioramaCaptionGroup;

        private RectTransform endingStamp;
        private CanvasGroup endingStampGroup;
        private TMP_Text endingMetaText;
        private Coroutine stampRoutine;

        private bool MotionAllowed { get { return settings == null || !settings.reduceMotion; } }

        // ================================================================ arka plan

        /// <summary>Eski görseli taşıyan katman; arka planın çocuğudur ki aynı kamerayla hareket etsin.</summary>
        private void BuildBackgroundCrossfade(GameObject background)
        {
            backgroundFade = AddImage(CreateRect("Background Crossfade", background.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), new Color(1f, 1f, 1f, 0f));
            backgroundFade.raycastTarget = false;
            backgroundFadeAlpha = 0f;
        }

        private void BeginBackgroundCrossfade(Sprite previous)
        {
            if (backgroundFade == null || previous == null || !MotionAllowed) return;
            backgroundFade.sprite = previous;
            backgroundFadeAlpha = 1f;
            backgroundFade.color = Color.white;
        }

        /// <summary>
        /// Yavaş kamera: kesintisiz bir nefes. Sahne değişiminde sıfırlanmaz; sıfırlansaydı
        /// eski görsel sönerken ölçek bir anda atlar, geçiş sarsılırdı.
        /// </summary>
        private float KenBurnsScale { get { return 1.028f + 0.022f * Mathf.Sin(presentationClock * Mathf.PI * 2f / 42f); } }

        private Vector2 KenBurnsDrift
        {
            get { return new Vector2(Mathf.Sin(presentationClock / 23f) * 14f, Mathf.Cos(presentationClock / 31f) * 8f); }
        }

        // ================================================================ atmosfer

        private sealed class AtmosphereParticle
        {
            public RectTransform rect;
            public Image image;
            public Vector2 position;
            public Vector2 velocity;
            public float phase;
            public float wobble;
            public float twinkle;
            public float alpha;
            public Color color;
            public bool align;
        }

        private sealed class AtmosphereLayer
        {
            public string sprite;
            public int count;
            public Color color;
            public float alphaMin, alphaMax;
            public Vector2 sizeMin, sizeMax;
            public Vector2 velocityMin, velocityMax;
            public float wobble;
            public float twinkle;
            public bool align;
        }

        private sealed class AtmospherePreset
        {
            public Color grade;
            public AtmosphereLayer[] layers = Array.Empty<AtmosphereLayer>();
        }

        private void BuildAtmosphere(Transform parent)
        {
            GameObject rootObject = CreateRect("Atmosphere", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            atmosphereRoot = rootObject.GetComponent<RectTransform>();
            atmosphereGrade = AddImage(CreateRect("Grade", rootObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), new Color(0f, 0f, 0f, 0f));
            atmosphereGrade.raycastTarget = false;
            GameObject particlesObject = CreateRect("Particles", rootObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            atmosphereGroup = particlesObject.AddComponent<CanvasGroup>();
            atmosphereGroup.blocksRaycasts = false;
            atmosphereGroup.interactable = false;
            atmosphereParticles.Clear();
            atmosphereAppliedKey = null;
            atmospherePendingKey = null;
            atmosphereFade = 1f;
        }

        private void RequestAtmosphere(string key)
        {
            if (atmosphereRoot == null || key == atmosphereAppliedKey) return;
            atmospherePendingKey = key;
        }

        /// <summary>
        /// Sahne havaları. Renkler paletten türer; ayar katmanı düşük opaklıkta kalır ki
        /// üretilmiş ve elle çizilmiş görseller aynı ışık altında dursun.
        /// </summary>
        private AtmospherePreset PresetFor(string key)
        {
            Color white = new Color(0.96f, 0.95f, 0.92f, 1f);
            Color ash = Color.Lerp(theme.agedPaper, theme.ink, 0.45f);
            Color ember = Color.Lerp(theme.rust, theme.mustard, 0.55f);
            Color dust = Color.Lerp(theme.mustard, theme.agedPaper, 0.55f);
            Color rain = Color.Lerp(theme.agedPaper, theme.petrol, 0.30f);
            Color fog = theme.agedPaper;

            AtmosphereLayer snowLight = Layer("il_puff", 115, white, 0.45f, 0.95f, new Vector2(5f, 5f), new Vector2(12f, 12f), new Vector2(-30f, -85f), new Vector2(10f, -40f), 22f, 0f, false);
            AtmosphereLayer snowWind = Layer("il_puff", 95, white, 0.35f, 0.9f, new Vector2(4f, 4f), new Vector2(10f, 10f), new Vector2(-300f, -130f), new Vector2(-170f, -70f), 14f, 0f, false);
            AtmosphereLayer snowGust = Layer("il_streak", 16, white, 0.08f, 0.22f, new Vector2(90f, 2f), new Vector2(220f, 3f), new Vector2(-780f, -60f), new Vector2(-520f, -30f), 0f, 0f, true);
            AtmosphereLayer snowHeavy = Layer("il_puff", 125, white, 0.40f, 0.95f, new Vector2(4f, 4f), new Vector2(12f, 12f), new Vector2(-460f, -170f), new Vector2(-280f, -90f), 12f, 0f, false);
            AtmosphereLayer ashFall = Layer("il_puff", 55, ash, 0.35f, 0.75f, new Vector2(3f, 3f), new Vector2(8f, 7f), new Vector2(-18f, -45f), new Vector2(28f, -16f), 34f, 0f, false);
            AtmosphereLayer embers = Layer("il_puff", 24, ember, 0.55f, 1f, new Vector2(3f, 3f), new Vector2(6f, 6f), new Vector2(-14f, 30f), new Vector2(18f, 80f), 20f, 5f, false);
            AtmosphereLayer motes = Layer("il_puff", 42, dust, 0.12f, 0.45f, new Vector2(2f, 2f), new Vector2(5f, 5f), new Vector2(-9f, -7f), new Vector2(9f, 7f), 12f, 1.4f, false);
            AtmosphereLayer rainFall = Layer("il_streak", 115, rain, 0.14f, 0.34f, new Vector2(30f, 1.5f), new Vector2(64f, 2.5f), new Vector2(-150f, -1250f), new Vector2(-90f, -900f), 0f, 0f, true);
            AtmosphereLayer mist = Layer("il_puff", 7, fog, 0.05f, 0.11f, new Vector2(420f, 160f), new Vector2(820f, 300f), new Vector2(6f, -2f), new Vector2(18f, 2f), 0f, 0f, false);
            AtmosphereLayer steam = Layer("il_puff", 10, fog, 0.06f, 0.12f, new Vector2(140f, 120f), new Vector2(300f, 240f), new Vector2(-6f, 18f), new Vector2(10f, 40f), 10f, 0f, false);

            switch (key)
            {
                case "bombed_street": return Preset(theme.rust, 0.07f, ashFall, embers);
                case "burned_village": return Preset(theme.rust, 0.05f, ashFall, Layer("il_puff", 30, white, 0.25f, 0.6f, new Vector2(4f, 4f), new Vector2(9f, 9f), new Vector2(-25f, -70f), new Vector2(10f, -35f), 20f, 0f, false));
                case "aid_registry": return Preset(theme.mustard, 0.05f, motes, Layer("il_puff", 16, ash, 0.2f, 0.5f, new Vector2(3f, 3f), new Vector2(6f, 6f), new Vector2(-10f, -30f), new Vector2(14f, -12f), 26f, 0f, false));
                case "shelter_stairs":
                case "barn_interior":
                case "village_exchange":
                case "school_night": return Preset(theme.mustard, 0.06f, motes);
                case "karelian_farm":
                case "churchyard": return Preset(theme.petrol, 0.05f, snowLight);
                case "orthodox_chapel":
                case "border_station": return Preset(theme.sootNavy, 0.08f, Layer("il_puff", 60, white, 0.2f, 0.55f, new Vector2(3f, 3f), new Vector2(8f, 8f), new Vector2(-25f, -60f), new Vector2(10f, -30f), 18f, 0f, false));
                case "evacuation_road":
                case "frozen_bay": return Preset(theme.petrol, 0.07f, snowWind, snowGust);
                case "typhus_barn":
                case "burned_farm": return Preset(theme.mustard, 0.05f, Layer("il_puff", 40, white, 0.25f, 0.6f, new Vector2(4f, 4f), new Vector2(9f, 9f), new Vector2(-25f, -70f), new Vector2(10f, -35f), 20f, 0f, false));
                case "night_camp":
                case "barn_night":
                case "bank_night": return Preset(theme.sootNavy, 0.10f, Layer("il_puff", 55, white, 0.2f, 0.55f, new Vector2(3f, 3f), new Vector2(8f, 8f), new Vector2(-25f, -60f), new Vector2(10f, -30f), 18f, 0f, false));
                case "pine_forest": return Preset(theme.sootNavy, 0.08f, snowLight);
                case "snow_ridge": return Preset(theme.petrol, 0.08f, snowHeavy, snowGust);
                case "stream_ford":
                case "far_bank": return Preset(theme.petrol, 0.06f, mist);
                case "train_platform": return Preset(theme.agedPaper, 0.04f, mist, steam);
                case "harbor_dawn":
                case "shipyard_evening": return Preset(theme.agedPaper, 0.035f, mist);
                case "river_gorge":
                case "broken_bridge": return Preset(theme.petrol, 0.10f, rainFall, mist);
                case "mountain_column":
                case "polder_road": return Preset(theme.petrol, 0.07f, snowWind, snowGust);
                case "afsluitdijk": return Preset(theme.petrol, 0.08f, snowHeavy, snowGust);
                case "frozen_canal":
                case "frisian_farm": return Preset(theme.petrol, 0.06f, snowLight);
                case "canal_night": return Preset(theme.sootNavy, 0.10f, Layer("il_puff", 60, white, 0.22f, 0.6f, new Vector2(4f, 4f), new Vector2(9f, 9f), new Vector2(-25f, -70f), new Vector2(10f, -35f), 20f, 0f, false));
                default: return Preset(theme.agedPaper, 0f);
            }
        }

        private static AtmosphereLayer Layer(string sprite, int count, Color color, float alphaMin, float alphaMax, Vector2 sizeMin, Vector2 sizeMax,
            Vector2 velocityMin, Vector2 velocityMax, float wobble, float twinkle, bool align)
        {
            return new AtmosphereLayer
            {
                sprite = sprite, count = count, color = color, alphaMin = alphaMin, alphaMax = alphaMax, sizeMin = sizeMin, sizeMax = sizeMax,
                velocityMin = velocityMin, velocityMax = velocityMax, wobble = wobble, twinkle = twinkle, align = align
            };
        }

        private static AtmospherePreset Preset(Color grade, float gradeAlpha, params AtmosphereLayer[] layers)
        {
            return new AtmospherePreset { grade = new Color(grade.r, grade.g, grade.b, gradeAlpha), layers = layers };
        }

        private void ApplyAtmosphere(string key)
        {
            atmosphereAppliedKey = key;
            for (int i = 0; i < atmosphereParticles.Count; i++)
                if (atmosphereParticles[i].rect != null) Destroy(atmosphereParticles[i].rect.gameObject);
            atmosphereParticles.Clear();
            AtmospherePreset preset = PresetFor(key ?? string.Empty);
            atmosphereGrade.color = preset.grade;

            Vector2 area = AtmosphereArea;
            System.Random random = new System.Random((key ?? string.Empty).GetHashCode());
            for (int l = 0; l < preset.layers.Length; l++)
            {
                AtmosphereLayer layer = preset.layers[l];
                Sprite sprite = InterludeSprite(layer.sprite);
                for (int i = 0; i < layer.count; i++)
                {
                    float r() { return (float)random.NextDouble(); }
                    GameObject go = CreateRect("P", atmosphereGroup.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                    RectTransform rect = go.GetComponent<RectTransform>();
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = Vector2.Lerp(layer.sizeMin, layer.sizeMax, r());
                    Image image = go.AddComponent<Image>();
                    image.sprite = sprite;
                    image.raycastTarget = false;
                    AtmosphereParticle p = new AtmosphereParticle
                    {
                        rect = rect,
                        image = image,
                        position = new Vector2(r() * area.x, r() * area.y),
                        velocity = new Vector2(Mathf.Lerp(layer.velocityMin.x, layer.velocityMax.x, r()), Mathf.Lerp(layer.velocityMin.y, layer.velocityMax.y, r())),
                        phase = r() * 100f,
                        wobble = layer.wobble * (0.5f + r()),
                        twinkle = layer.twinkle,
                        alpha = Mathf.Lerp(layer.alphaMin, layer.alphaMax, r()),
                        color = layer.color,
                        align = layer.align
                    };
                    if (p.align) rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(p.velocity.y, p.velocity.x) * Mathf.Rad2Deg);
                    atmosphereParticles.Add(p);
                    PlaceParticle(p, 0f);
                }
            }
        }

        private Vector2 AtmosphereArea
        {
            get
            {
                Vector2 size = atmosphereRoot == null ? new Vector2(1920f, 1080f) : atmosphereRoot.rect.size;
                if (size.x < 10f || size.y < 10f) size = new Vector2(1920f, 1080f);
                return size;
            }
        }

        private void PlaceParticle(AtmosphereParticle p, float wobbleOffset)
        {
            p.rect.anchoredPosition = p.position + new Vector2(wobbleOffset, 0f);
            float twinkle = p.twinkle > 0f ? 0.55f + 0.45f * Mathf.Sin(p.phase * p.twinkle) : 1f;
            p.image.color = new Color(p.color.r, p.color.g, p.color.b, p.alpha * twinkle);
        }

        private void UpdateAtmosphere(float dt)
        {
            if (atmosphereRoot == null) return;
            bool motion = MotionAllowed;
            atmosphereGroup.gameObject.SetActive(motion);

            // Hava değişimi: eski hava söner, yenisi kurulur ve belirir.
            if (atmospherePendingKey != null || (atmospherePendingKey == null && atmosphereAppliedKey == null && currentBackgroundKey != null))
            {
                string wanted = atmospherePendingKey ?? currentBackgroundKey;
                atmosphereFade = motion ? Mathf.MoveTowards(atmosphereFade, 0f, dt / 0.45f) : 0f;
                if (atmosphereFade <= 0f)
                {
                    ApplyAtmosphere(wanted);
                    atmospherePendingKey = null;
                }
            }
            else atmosphereFade = motion ? Mathf.MoveTowards(atmosphereFade, 1f, dt / 0.9f) : 1f;
            atmosphereGroup.alpha = atmosphereFade;
            if (!motion) return;

            Vector2 area = AtmosphereArea;
            for (int i = 0; i < atmosphereParticles.Count; i++)
            {
                AtmosphereParticle p = atmosphereParticles[i];
                p.phase += dt;
                p.position += p.velocity * dt;
                float margin = Mathf.Max(p.rect.sizeDelta.x, p.rect.sizeDelta.y) * 0.6f + 20f;
                if (p.position.x < -margin) p.position.x += area.x + margin * 2f;
                else if (p.position.x > area.x + margin) p.position.x -= area.x + margin * 2f;
                if (p.position.y < -margin) p.position.y += area.y + margin * 2f;
                else if (p.position.y > area.y + margin) p.position.y -= area.y + margin * 2f;
                PlaceParticle(p, p.wobble > 0f ? Mathf.Sin(p.phase * 1.3f) * p.wobble : 0f);
            }
        }

        // ================================================================ perde kartı

        private void BuildActCard(Transform gameplayScreen)
        {
            actCard = CreateRect("Act Card", gameplayScreen, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            actCardGroup = actCard.AddComponent<CanvasGroup>();
            actCardGroup.alpha = 0f;
            actCardGroup.blocksRaycasts = false;
            AddImage(CreateRect("Veil", actCard.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero),
                new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.80f)).raycastTarget = false;
            AddImage(CreateRect("Vignette", actCard.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), Color.white, theme.vignette).raycastTarget = false;

            actNumberText = (TMP_Text)AsDocument(CreateText("Act Number", actCard.transform, string.Empty, 22f, FontStyles.Bold, theme.mustard,
                new Vector2(0.2f, 0.60f), new Vector2(0.8f, 0.66f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center));
            actNumberText.characterSpacing = 14f;
            actNameText = CreateText("Act Name", actCard.transform, string.Empty, 104f, FontStyles.Bold, theme.agedPaper,
                new Vector2(0.1f, 0.44f), new Vector2(0.9f, 0.60f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            actNameText.overflowMode = TextOverflowModes.Overflow;
            actNameText.enableWordWrapping = false;
            actRule = CreateRect("Act Rule", actCard.transform, new Vector2(0.5f, 0.425f), new Vector2(0.5f, 0.425f), new Vector2(-60f, -2f), new Vector2(60f, 2f)).GetComponent<RectTransform>();
            AddImage(actRule.gameObject, theme.rust).raycastTarget = false;
            actMetaText = (TMP_Text)AsDocument(CreateText("Act Meta", actCard.transform, string.Empty, 20f, FontStyles.Normal,
                new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.72f),
                new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.41f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center));
            actMetaText.characterSpacing = 3f;
            // Perdenin sorusu (1.3): oyun sorar, cevaplamaz. Soru yoksa alan boş kalır.
            actQuestionText = CreateText("Act Question", actCard.transform, string.Empty, 30f, FontStyles.Italic,
                new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.9f),
                new Vector2(0.16f, 0.22f), new Vector2(0.84f, 0.32f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            actQuestionText.enableAutoSizing = true;
            actQuestionText.fontSizeMin = 22f;
            actQuestionText.fontSizeMax = 30f;
            actCard.SetActive(false);
        }

        /// <summary>Perdenin bölüm içindeki sırası: düğümlerin dosyadaki ilk görünüşüne göre.</summary>
        private int ActOrdinal(string act)
        {
            StoryDatabase story = storyController == null ? null : storyController.Story;
            if (story == null || story.nodes == null || string.IsNullOrEmpty(act)) return 0;
            List<string> order = new List<string>();
            for (int i = 0; i < story.nodes.Length; i++)
            {
                string a = story.nodes[i] == null ? null : story.nodes[i].act;
                if (!string.IsNullOrEmpty(a) && !order.Contains(a)) order.Add(a);
            }
            return order.IndexOf(act) + 1;
        }

        private static string Roman(int value)
        {
            string[] numerals = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
            return value >= 0 && value < numerals.Length ? numerals[value] : value.ToString();
        }

        /// <summary>
        /// Perde kartı: örtü açılır, perde numarası ve adı belirir, pas çizgisi uzar, tarih
        /// ve yer altında durur. Bir süre sonra söner. Herhangi bir tuş ya da tıklama geçer.
        /// </summary>
        private IEnumerator PlayActCard(StoryNode node)
        {
            if (actCard == null || node == null || string.IsNullOrEmpty(node.act)) yield break;
            bool motion = MotionAllowed;
            int ordinal = ActOrdinal(node.act);
            string label = T(UiKey.ActLabel);
            string number = ordinal > 0 ? label + " " + Roman(ordinal) : label;
            actNumberText.text = localization == null ? number : localization.ToUpper(number);
            actNameText.text = node.act;
            string meta = ((node.date ?? string.Empty) + "   ·   " + (node.location ?? string.Empty)).Trim();
            actMetaText.text = localization == null ? meta : localization.ToUpper(meta);
            ActData actData = storyController == null || storyController.Story == null ? null : storyController.Story.FindAct(node.act);
            string question = actData == null ? null : actData.question;
            if (actQuestionText != null) actQuestionText.text = string.IsNullOrWhiteSpace(question) ? string.Empty : question.Trim();

            // Anlatı kartı perde kartının arkasında görünmez; çağıran taraf onu sonra açar.
            if (storyCardGroup != null) storyCardGroup.alpha = 0f;
            actCard.SetActive(true);
            actCard.transform.SetAsLastSibling();
            actCardActive = true;
            actCardSkip = false;
            actCardArmTime = Time.unscaledTime + 0.35f;
            if (audioManager != null) audioManager.PlayActTone();

            float fadeIn = motion ? 0.55f : 0f;
            // Soru okunacak kadar kalır; atlanabilir.
            float hold = actQuestionText != null && actQuestionText.text.Length > 0 ? 3.6f : 1.9f;
            float fadeOut = motion ? 0.55f : 0f;
            for (float t = 0f; t < fadeIn && !actCardSkip; t += Time.unscaledDeltaTime)
            {
                float p = Mathf.SmoothStep(0f, 1f, t / fadeIn);
                actCardGroup.alpha = p;
                actNameText.characterSpacing = Mathf.Lerp(22f, 6f, p);
                actRule.sizeDelta = new Vector2(Mathf.Lerp(20f, 180f, p), 4f);
                yield return null;
            }
            actCardGroup.alpha = 1f;
            actNameText.characterSpacing = 6f;
            actRule.sizeDelta = new Vector2(180f, 4f);
            for (float t = 0f; t < hold && !actCardSkip; t += Time.unscaledDeltaTime) yield return null;
            for (float t = 0f; t < fadeOut; t += Time.unscaledDeltaTime)
            {
                actCardGroup.alpha = 1f - t / fadeOut;
                yield return null;
            }
            actCardGroup.alpha = 0f;
            actCard.SetActive(false);
            actCardActive = false;
        }

        private IEnumerator ActCardThenShowCard(StoryNode node, bool animate)
        {
            transitionBusy = true;
            storyCardGroup.alpha = 0f;
            yield return PlayActCard(node);
            yield return ShowCard(animate);
        }

        // ================================================================ mürekkep açılışı

        private void StartBodyReveal()
        {
            StopBodyReveal();
            if (!MotionAllowed || storyBodyText == null || string.IsNullOrEmpty(storyBodyText.text)) return;
            revealRoutine = StartCoroutine(RevealBody());
        }

        private void StopBodyReveal()
        {
            if (revealRoutine != null) StopCoroutine(revealRoutine);
            revealRoutine = null;
            if (bodyRevealing && storyBodyText != null) storyBodyText.ForceMeshUpdate();
            bodyRevealing = false;
            revealFinishRequested = false;
        }

        /// <summary>
        /// Metin, soldan sağa ve satır satır ilerleyen yumuşak bir cephe ile belirir. Süre
        /// uzunluğa göre 0,5–1,5 saniye; seçim düğmeleri bu sırada da çalışır.
        /// </summary>
        private IEnumerator RevealBody()
        {
            bodyRevealing = true;
            revealFinishRequested = false;
            TMP_Text text = storyBodyText;
            text.ForceMeshUpdate();
            int count = text.textInfo.characterCount;
            float duration = Mathf.Clamp(count / 420f, 0.5f, 1.5f);
            const float soft = 18f;
            for (float t = 0f; t < duration && !revealFinishRequested; t += Time.unscaledDeltaTime)
            {
                ApplyRevealAlpha(text, (t / duration) * (count + soft), soft);
                yield return null;
            }
            text.ForceMeshUpdate();
            bodyRevealing = false;
            revealFinishRequested = false;
            revealRoutine = null;
        }

        private static void ApplyRevealAlpha(TMP_Text text, float front, float soft)
        {
            text.ForceMeshUpdate();
            TMP_TextInfo info = text.textInfo;
            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible) continue;
                float a = Mathf.Clamp01((front - i) / soft);
                Color32[] colors = info.meshInfo[character.materialReferenceIndex].colors32;
                int v = character.vertexIndex;
                byte alpha = (byte)(character.color.a * a);
                for (int k = 0; k < 4; k++) colors[v + k].a = alpha;
            }
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        // ================================================================ ana menü

        private void BuildMenuDiorama(Transform menuScreen)
        {
            GameObject captionObject = CreateRect("Diorama Caption", menuScreen, new Vector2(0.55f, 0.045f), new Vector2(0.955f, 0.10f), Vector2.zero, Vector2.zero);
            dioramaCaptionGroup = captionObject.AddComponent<CanvasGroup>();
            AddImage(CreateRect("Caption Rule", captionObject.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-4f, -16f), new Vector2(0f, 16f)), theme.rust).raycastTarget = false;
            dioramaCaption = (TMP_Text)AsDocument(CreateText("Caption", captionObject.transform, string.Empty, 17f, FontStyles.Bold,
                new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.82f),
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-16f, 0f), TextAlignmentOptions.Right));
            dioramaCaption.characterSpacing = 4f;
        }

        private void RefreshDioramaSlides()
        {
            dioramaSlides.Clear();
            StoryCatalogEntry[] entries = catalog == null ? Array.Empty<StoryCatalogEntry>() : catalog.entries;
            for (int i = 0; i < entries.Length; i++)
            {
                StoryCatalogEntry entry = entries[i];
                if (entry == null || !entry.IsPlayable || string.IsNullOrEmpty(entry.imageKey) || !artIndex.ContainsKey(entry.imageKey)) continue;
                dioramaSlides.Add(entry);
            }
        }

        /// <summary>Ana menü açıldığında ilk bölümün görseli ve künyesi; sonra dönüşümlü geçiş.</summary>
        private void StartMenuDiorama()
        {
            RefreshDioramaSlides();
            dioramaTimer = 0f;
            if (dioramaSlides.Count == 0)
            {
                SetBackground("harbor_dawn");
                if (dioramaCaption != null) dioramaCaption.text = string.Empty;
                return;
            }
            dioramaIndex = Mathf.Clamp(dioramaIndex, 0, dioramaSlides.Count - 1);
            ShowDioramaSlide(dioramaSlides[dioramaIndex]);
        }

        private void ShowDioramaSlide(StoryCatalogEntry entry)
        {
            SetBackground(entry.imageKey);
            string caption = (entry.title ?? string.Empty) + "   ·   " + (entry.period ?? string.Empty);
            if (dioramaCaption != null) dioramaCaption.text = localization == null ? caption : localization.ToUpper(caption);
        }

        private void UpdateMenuDiorama(float dt)
        {
            if (router == null || router.Current != AppScreen.MainMenu || dioramaSlides.Count < 2 || !MotionAllowed)
            {
                if (dioramaCaptionGroup != null) dioramaCaptionGroup.alpha = 1f;
                return;
            }
            dioramaTimer += dt;
            if (dioramaTimer >= DioramaSlideSeconds)
            {
                dioramaTimer = 0f;
                dioramaIndex = (dioramaIndex + 1) % dioramaSlides.Count;
                ShowDioramaSlide(dioramaSlides[dioramaIndex]);
            }
            if (dioramaCaptionGroup != null)
            {
                float fadeIn = Mathf.Clamp01((dioramaTimer - 0.3f) / 0.9f);
                float fadeOut = Mathf.Clamp01((DioramaSlideSeconds - dioramaTimer) / 0.6f);
                dioramaCaptionGroup.alpha = Mathf.Min(fadeIn, fadeOut);
            }
        }

        /// <summary>Katalogdaki dönemlerden en erken ve en geç yıl: "1943–1945".</summary>
        private string AnthologyYears()
        {
            int min = int.MaxValue, max = int.MinValue;
            StoryCatalogEntry[] entries = catalog == null ? Array.Empty<StoryCatalogEntry>() : catalog.entries;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] == null || !entries[i].IsPlayable || string.IsNullOrEmpty(entries[i].period)) continue;
                foreach (Match match in Regex.Matches(entries[i].period, @"\b(19\d\d)\b"))
                {
                    int year = int.Parse(match.Value);
                    if (year < min) min = year;
                    if (year > max) max = year;
                }
            }
            if (min == int.MaxValue) return string.Empty;
            return min == max ? min.ToString() : min + "–" + max;
        }

        // ================================================================ final mührü

        private void BuildEndingStamp(Transform panel)
        {
            endingMetaText = (TMP_Text)AsDocument(CreateText("Ending Meta", panel, string.Empty, 17f, FontStyles.Bold, ArchiveLabel,
                new Vector2(0.07f, 0.925f), new Vector2(0.72f, 0.965f), Vector2.zero, Vector2.zero, TextAlignmentOptions.BottomLeft));
            endingMetaText.characterSpacing = 4f;

            GameObject stamp = CreateRect("Stamp", panel, new Vector2(0.815f, 0.875f), new Vector2(0.815f, 0.875f), new Vector2(-165f, -52f), new Vector2(165f, 52f));
            endingStamp = stamp.GetComponent<RectTransform>();
            endingStamp.localRotation = Quaternion.Euler(0f, 0f, -8f);
            endingStampGroup = stamp.AddComponent<CanvasGroup>();
            endingStampGroup.alpha = 0f;
            Color ink = theme.rust;
            StampBorder(stamp.transform, ink, 0f, 5f);
            StampBorder(stamp.transform, ink, 11f, 2f);
            TMP_Text word = (TMP_Text)AsDocument(CreateText("Stamp Word", stamp.transform, T(UiKey.EndingStamp), 30f, FontStyles.Bold, ink,
                Vector2.zero, Vector2.one, new Vector2(18f, 12f), new Vector2(-18f, -12f), TextAlignmentOptions.Center));
            word.characterSpacing = 6f;
            word.enableAutoSizing = true;
            word.fontSizeMin = 18f;
            word.fontSizeMax = 30f;
        }

        private void StampBorder(Transform stamp, Color color, float inset, float thickness)
        {
            AddImage(CreateRect("Top", stamp, new Vector2(0f, 1f), Vector2.one, new Vector2(inset, -inset - thickness), new Vector2(-inset, -inset)), color).raycastTarget = false;
            AddImage(CreateRect("Bottom", stamp, Vector2.zero, new Vector2(1f, 0f), new Vector2(inset, inset), new Vector2(-inset, inset + thickness)), color).raycastTarget = false;
            AddImage(CreateRect("Left", stamp, Vector2.zero, new Vector2(0f, 1f), new Vector2(inset, inset), new Vector2(inset + thickness, -inset)), color).raycastTarget = false;
            AddImage(CreateRect("Right", stamp, new Vector2(1f, 0f), Vector2.one, new Vector2(-inset - thickness, inset), new Vector2(-inset, -inset)), color).raycastTarget = false;
        }

        private void PrepareEndingPresentation()
        {
            if (endingMetaText != null)
            {
                StoryCatalogEntry entry = null;
                StoryCatalogEntry[] entries = catalog == null ? Array.Empty<StoryCatalogEntry>() : catalog.entries;
                for (int i = 0; i < entries.Length; i++)
                    if (entries[i] != null && entries[i].storyId == activeStoryId) entry = entries[i];
                string meta = entry == null ? string.Empty : (entry.title + "   ·   " + entry.period);
                endingMetaText.text = localization == null ? meta : localization.ToUpper(meta);
            }
            if (stampRoutine != null) StopCoroutine(stampRoutine);
            stampRoutine = StartCoroutine(StampEnding());
        }

        /// <summary>Mühür kısa bir gecikmeyle iner: büyükten küçüğe, bir tık sesiyle ve hafif sarsıntıyla.</summary>
        private IEnumerator StampEnding()
        {
            if (endingStamp == null) yield break;
            endingStamp.localScale = Vector3.one;
            if (!MotionAllowed)
            {
                endingStampGroup.alpha = 0.88f;
                yield break;
            }
            endingStampGroup.alpha = 0f;
            for (float t = 0f; t < 0.75f; t += Time.unscaledDeltaTime) yield return null;
            const float drop = 0.16f;
            for (float t = 0f; t < drop; t += Time.unscaledDeltaTime)
            {
                float p = t / drop;
                endingStamp.localScale = Vector3.one * Mathf.Lerp(1.9f, 1f, p * p);
                endingStampGroup.alpha = Mathf.Lerp(0f, 0.9f, p);
                yield return null;
            }
            endingStamp.localScale = Vector3.one;
            endingStampGroup.alpha = 0.9f;
            if (audioManager != null) audioManager.PlayStamp();
            Vector2 origin = endingStamp.anchoredPosition;
            for (float t = 0f; t < 0.14f; t += Time.unscaledDeltaTime)
            {
                endingStamp.anchoredPosition = origin + new Vector2(Mathf.Sin(t * 180f) * 3f * (1f - t / 0.14f), 0f);
                yield return null;
            }
            endingStamp.anchoredPosition = origin;
            // Mürekkep kâğıda oturur: opaklık biraz düşer.
            for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
            {
                endingStampGroup.alpha = Mathf.Lerp(0.9f, 0.8f, t / 0.6f);
                yield return null;
            }
            stampRoutine = null;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// QA yakalamaları kararların hemen ardından alınır; geçişler o anda yarıdadır.
        /// Yakalamadan önce süren geçiş bitirilir ve bekleyen hava hemen kurulur.
        /// </summary>
        private void SettlePresentationForQa()
        {
            backgroundFadeAlpha = 0f;
            if (backgroundFade != null) backgroundFade.color = new Color(1f, 1f, 1f, 0f);
            if (atmosphereRoot == null) return;
            string wanted = atmospherePendingKey ?? (atmosphereAppliedKey == null ? currentBackgroundKey : null);
            if (wanted != null) ApplyAtmosphere(wanted);
            atmospherePendingKey = null;
            atmosphereFade = 1f;
            atmosphereGroup.alpha = 1f;
        }
#endif

        // ================================================================ kare döngüsü

        private void UpdatePresentation()
        {
            float dt = Time.unscaledDeltaTime;
            if (MotionAllowed) presentationClock += dt;
            if (backgroundFade != null)
            {
                if (backgroundFadeAlpha > 0f)
                {
                    backgroundFadeAlpha = MotionAllowed ? Mathf.MoveTowards(backgroundFadeAlpha, 0f, dt / BackgroundCrossfadeSeconds) : 0f;
                    float eased = Mathf.SmoothStep(0f, 1f, backgroundFadeAlpha);
                    backgroundFade.color = new Color(1f, 1f, 1f, eased);
                }
                else if (backgroundFade.color.a > 0f) backgroundFade.color = new Color(1f, 1f, 1f, 0f);
            }
            UpdateAtmosphere(dt);
            UpdateMenuDiorama(dt);
        }

        /// <summary>
        /// Sunum katmanının tuş yakalaması. Perde kartı ve mürekkep açılışı sırasında ilk
        /// basış onları tamamlar ve seçim olarak sayılmaz.
        /// </summary>
        private bool ConsumePresentationInput()
        {
            if (actCardActive)
            {
                if (Time.unscaledTime >= actCardArmTime && Input.anyKeyDown) actCardSkip = true;
                return true;
            }
            if (bodyRevealing && router.Current == AppScreen.Gameplay)
            {
                bool pressed = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.LeftArrow)
                    || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)
                    || PadLeftDown || PadRightDown || PadActionDown;
                if (pressed)
                {
                    revealFinishRequested = true;
                    return true;
                }
            }
            return false;
        }
    }
}
