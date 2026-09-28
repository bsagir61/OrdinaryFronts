using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OrdinaryFronts
{
    /// <summary>
    /// Harita masası (1.7): "Savaş Hikâyeleri" ekranının yeni katmanları.
    /// <list type="bullet">
    /// <item><b>İğneler.</b> Bölümler haritaya iğneyle tutturulmuş gibi durur; seçili iğnenin
    /// çevresinde yavaş bir nabız halkası atar, yarım kalmış bölümün iğnesinde küçük bir ayraç
    /// vardır.</item>
    /// <item><b>İplikler.</b> Oyuncunun gerçekten gördüğü bölümler arası kesişmeler, iki iğne
    /// arasında gerili kırmızı bir iplik olarak çizilir. İpliğin düğümü seçilince iki bölümün
    /// birbirine ne hatırlattığı okunur. Sayaç yoktur: kaç iplik olabileceği hiçbir yerde
    /// yazmaz (GDD §13.4).</item>
    /// <item><b>Zaman şeridi.</b> Haritanın altında Eylül 1939'dan Mayıs 1945'e bir şerit;
    /// bölümler geçtikleri ayda durur. A/D ya da LB/RB bölümler arasında zaman sırasıyla gezer.</item>
    /// <item><b>Son tanıklık.</b> Bölüm kartının görselinin üstünde, oyuncunun o bölümde verdiği
    /// son tanıklıktan ilk satır.</item>
    /// </list>
    /// </summary>
    public sealed partial class AppController
    {
        private const int TimelineStartMonth = 1939 * 12 + 8;   // Eylül 1939
        private const int TimelineEndMonth = 1945 * 12 + 4;     // Mayıs 1945

        private RectTransform mapRect;
        private Transform threadLayer;
        private GameObject mapNote;
        private TMP_Text mapNoteTitle;
        private TMP_Text mapNoteBody;
        private GameObject storyQuote;
        private TMP_Text storyQuoteKicker;
        private TMP_Text storyQuoteText;
        private TMP_Text timelineHintText;
        private readonly List<TimelineNode> timelineNodes = new List<TimelineNode>();
        private readonly List<ThreadInfo> mapThreads = new List<ThreadInfo>();
        private readonly Dictionary<string, StoryDatabase> mapStoryCache = new Dictionary<string, StoryDatabase>();
        private string mapStoryCacheLocale;
        private float mapPulseClock;

        private sealed class TimelineNode
        {
            public StoryCatalogEntry entry;
            public Image diamond;
            public TMP_Text label;
        }

        private sealed class ThreadInfo
        {
            public string a, b;
            public readonly List<KeyValuePair<string, string>> moments = new List<KeyValuePair<string, string>>();
            public Image knot;
        }

        // ================================================================== iğne

        /// <summary>
        /// İğne: haritaya düşen gölge, ince bir iğne ve pas renkli yuvarlak baş. Konum noktası
        /// iğnenin ucudur; baş biraz yukarıda durur. Etiket kâğıt bir fişin üstünde yazılır ki
        /// kıyı çizgilerinin ve ipliklerin üstünde de okunsun.
        /// </summary>
        private Button BuildMapPin(Transform mapTransform, StoryCatalogEntry entry, bool labelLeft, StoryMarker marker)
        {
            Vector2 uv = MapProjection.Project(entry.latitude, entry.longitude);
            GameObject anchor = CreateRect("Marker " + entry.storyId, mapTransform, uv, uv, new Vector2(-24f, -10f), new Vector2(24f, 44f));
            Image hit = AddImage(anchor, new Color(0f, 0f, 0f, 0f));
            hit.raycastTarget = true;
            Button button = anchor.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Automatic;
            button.navigation = navigation;
            button.onClick.AddListener(() => { audioManager.PlayConfirm(); SelectStory(entry); });

            Vector2 tip = new Vector2(0.5f, 10f / 54f);
            Vector2 head = new Vector2(0.5f, 36f / 54f);
            // Gölge: sağ alta düşen yumuşak elips.
            AddImage(CreateRect("Shadow", anchor.transform, tip, tip, new Vector2(-4f, -6f), new Vector2(16f, 2f)),
                new Color(0f, 0f, 0f, 0.28f), theme.mapMarker).raycastTarget = false;
            Image pulse = AddImage(CreateRect("Pulse", anchor.transform, head, head, new Vector2(-16f, -16f), new Vector2(16f, 16f)),
                new Color(theme.rust.r, theme.rust.g, theme.rust.b, 0f), InterludeSprite("il_ring") ?? theme.mapMarker);
            pulse.raycastTarget = false;
            // İğne gövdesi.
            AddImage(CreateRect("Needle", anchor.transform, tip, tip, new Vector2(-1f, 0f), new Vector2(1f, 26f)),
                new Color(theme.ink.r, theme.ink.g, theme.ink.b, 0.9f)).raycastTarget = false;
            AddImage(CreateRect("Head Rim", anchor.transform, head, head, new Vector2(-10f, -10f), new Vector2(10f, 10f)),
                new Color(theme.ink.r, theme.ink.g, theme.ink.b, 0.9f), theme.mapMarker).raycastTarget = false;
            Image headImage = AddImage(CreateRect("Head", anchor.transform, head, head, new Vector2(-8f, -8f), new Vector2(8f, 8f)),
                theme.rust, theme.mapMarker);
            headImage.raycastTarget = false;
            AddImage(CreateRect("Glint", anchor.transform, head, head, new Vector2(-5f, 1f), new Vector2(-1f, 5f)),
                new Color(1f, 1f, 1f, 0.55f), theme.mapMarker).raycastTarget = false;
            // Ayraç: yarım kalmış bölüm.
            GameObject bookmark = CreateRect("Bookmark", anchor.transform, head, head, new Vector2(7f, 4f), new Vector2(13f, 20f));
            AddImage(bookmark, theme.mustard).raycastTarget = false;
            AddImage(CreateRect("Notch", bookmark.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-2f, 0f), new Vector2(2f, 4f)),
                theme.agedPaper).raycastTarget = false;
            bookmark.SetActive(false);

            // Etiket fişi.
            Vector2 side = labelLeft ? new Vector2(0f, 0.62f) : new Vector2(1f, 0.62f);
            GameObject chip = CreateRect("Label Chip", anchor.transform, side, side,
                labelLeft ? new Vector2(-190f, -26f) : new Vector2(2f, -26f), labelLeft ? new Vector2(-2f, 26f) : new Vector2(190f, 26f));
            Image chipImage = AddImage(chip, new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.9f));
            chipImage.raycastTarget = false;
            TMP_Text name = CreateText("Name", chip.transform, entry.title, 21f, FontStyles.Bold, theme.ink,
                new Vector2(0f, 0.42f), Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f), labelLeft ? TextAlignmentOptions.BottomRight : TextAlignmentOptions.BottomLeft);
            name.overflowMode = TextOverflowModes.Overflow;
            name.enableWordWrapping = false;
            name.raycastTarget = false;
            TMP_Text period = (TMP_Text)AsDocument(CreateText("Period", chip.transform,
                localization == null ? entry.period : localization.ToUpper(entry.period), 12f, FontStyles.Bold, ArchiveLabel,
                Vector2.zero, new Vector2(1f, 0.42f), new Vector2(8f, 2f), new Vector2(-8f, 0f), labelLeft ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft));
            period.characterSpacing = 2f;
            period.overflowMode = TextOverflowModes.Overflow;
            period.enableWordWrapping = false;
            period.raycastTarget = false;
            // Fiş metne göre daraltılır.
            float width = Mathf.Max(name.GetPreferredValues(entry.title).x, period.GetPreferredValues(period.text).x) + 18f;
            RectTransform chipRect = chip.GetComponent<RectTransform>();
            chipRect.offsetMin = labelLeft ? new Vector2(-width - 2f, -26f) : new Vector2(2f, -26f);
            chipRect.offsetMax = labelLeft ? new Vector2(-2f, 26f) : new Vector2(width + 2f, 26f);

            marker.head = headImage;
            marker.pulse = pulse;
            marker.name = name;
            marker.chip = chipImage;
            marker.bookmark = bookmark;
            return button;
        }

        private void PaintMapPins()
        {
            for (int i = 0; i < storyMarkers.Count; i++)
            {
                StoryMarker m = storyMarkers[i];
                bool selected = m.entry == selectedStory;
                if (m.head != null) m.head.color = selected ? theme.rust : Color.Lerp(theme.rust, theme.agedPaper, 0.35f);
                if (m.name != null) m.name.color = selected ? theme.ink : new Color(theme.ink.r, theme.ink.g, theme.ink.b, 0.7f);
                if (m.chip != null) m.chip.color = new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, selected ? 0.96f : 0.78f);
                if (m.bookmark != null) m.bookmark.SetActive(HasUnfinishedSave(m.entry.storyId));
                if (m.head != null) m.head.transform.localScale = Vector3.one * (selected ? 1.2f : 1f);
            }
            for (int i = 0; i < timelineNodes.Count; i++)
            {
                bool selected = timelineNodes[i].entry == selectedStory;
                timelineNodes[i].diamond.color = selected ? theme.rust : new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.75f);
                timelineNodes[i].diamond.transform.localScale = Vector3.one * (selected ? 1.35f : 1f);
                timelineNodes[i].label.color = selected ? theme.agedPaper : new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.55f);
            }
        }

        // ================================================================== zaman şeridi

        private void BuildTimeline(Transform screen)
        {
            timelineNodes.Clear();
            GameObject strip = CreateRect("Timeline", screen, new Vector2(0.05f, 0.105f), new Vector2(0.66f, 0.178f), Vector2.zero, Vector2.zero);
            Color line = new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.55f);
            AddImage(CreateRect("Axis", strip.transform, new Vector2(0.02f, 0.5f), new Vector2(0.98f, 0.5f), new Vector2(0f, -1f), new Vector2(0f, 1f)), line).raycastTarget = false;
            for (int year = 1939; year <= 1945; year++)
            {
                for (int month = 0; month < 12; month++)
                {
                    int m = year * 12 + month;
                    if (m < TimelineStartMonth || m > TimelineEndMonth) continue;
                    float x = TimelineX(m);
                    bool jan = month == 0;
                    AddImage(CreateRect("Tick", strip.transform, new Vector2(x, 0.5f), new Vector2(x, 0.5f), new Vector2(-0.5f, jan ? -9f : -3f), new Vector2(0.5f, jan ? 9f : 3f)),
                        jan ? line : new Color(line.r, line.g, line.b, 0.35f)).raycastTarget = false;
                }
                float lx = TimelineX(Mathf.Max(TimelineStartMonth, year * 12 + (year == 1939 ? 8 : 0)));
                TMP_Text label = (TMP_Text)AsDocument(CreateText("Year " + year, strip.transform, year.ToString(), 13f, FontStyles.Bold,
                    new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.6f),
                    new Vector2(lx, 0f), new Vector2(lx, 0.4f), new Vector2(-2f, 0f), new Vector2(60f, 0f), TextAlignmentOptions.TopLeft));
                label.characterSpacing = 2f;
                label.raycastTarget = false;
            }
            // Bölüm düğümleri: tarihe göre.
            StoryCatalogEntry[] entries = catalog == null ? Array.Empty<StoryCatalogEntry>() : catalog.entries;
            for (int i = 0; i < entries.Length; i++)
            {
                StoryCatalogEntry entry = entries[i];
                if (entry == null || !entry.IsPlayable || entry.MonthIndex == int.MaxValue) continue;
                float x = TimelineX(entry.MonthIndex);
                GameObject node = CreateRect("Node " + entry.storyId, strip.transform, new Vector2(x, 0.5f), new Vector2(x, 0.5f), new Vector2(-18f, -18f), new Vector2(18f, 18f));
                Image hit = AddImage(node, new Color(0f, 0f, 0f, 0f));
                Button button = node.AddComponent<Button>();
                button.targetGraphic = hit;
                button.transition = Selectable.Transition.None;
                StoryCatalogEntry captured = entry;
                button.onClick.AddListener(() => { audioManager.PlayConfirm(); SelectStory(captured); });
                GameObject diamondObject = CreateRect("Diamond", node.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-6f, -6f), new Vector2(6f, 6f));
                diamondObject.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                Image diamond = AddImage(diamondObject, theme.agedPaper);
                diamond.raycastTarget = false;
                TMP_Text label = CreateText("Label", node.transform, entry.title, 15f, FontStyles.Bold, theme.agedPaper,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-90f, 0f), new Vector2(90f, 22f), TextAlignmentOptions.Bottom);
                label.overflowMode = TextOverflowModes.Overflow;
                label.enableWordWrapping = false;
                label.raycastTarget = false;
                timelineNodes.Add(new TimelineNode { entry = entry, diamond = diamond, label = label });
            }
            // Birbirine yakın iki bölümün adı üst üste binmesin: öndeki sola, arkadaki sağa yaslanır.
            List<TimelineNode> byTime = new List<TimelineNode>(timelineNodes);
            byTime.Sort((x, y) => x.entry.MonthIndex.CompareTo(y.entry.MonthIndex));
            for (int i = 0; i + 1 < byTime.Count; i++)
            {
                if (TimelineX(byTime[i + 1].entry.MonthIndex) - TimelineX(byTime[i].entry.MonthIndex) > 0.12f) continue;
                RectTransform first = byTime[i].label.rectTransform, second = byTime[i + 1].label.rectTransform;
                first.offsetMin = new Vector2(-180f, 0f);
                first.offsetMax = new Vector2(2f, 22f);
                byTime[i].label.alignment = TextAlignmentOptions.BottomRight;
                second.offsetMin = new Vector2(-2f, 0f);
                second.offsetMax = new Vector2(180f, 22f);
                byTime[i + 1].label.alignment = TextAlignmentOptions.BottomLeft;
            }
            timelineHintText = (TMP_Text)AsDocument(CreateText("Timeline Hint", screen, string.Empty, 13f, FontStyles.Bold,
                new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.55f),
                new Vector2(0.36f, 0.878f), new Vector2(0.66f, 0.915f), Vector2.zero, Vector2.zero, TextAlignmentOptions.BottomRight));
            timelineHintText.characterSpacing = 2f;
            RefreshTimelineHint();
        }

        private static float TimelineX(int monthIndex)
        {
            float t = Mathf.InverseLerp(TimelineStartMonth, TimelineEndMonth, monthIndex);
            return Mathf.Lerp(0.03f, 0.97f, t);
        }

        private void RefreshTimelineHint()
        {
            if (timelineHintText == null) return;
            string text = T(inputDevice == InputDevice.Gamepad ? UiKey.MapTimelineHintPad : UiKey.MapTimelineHintKeys);
            timelineHintText.text = localization == null ? text : localization.ToUpper(text);
        }

        /// <summary>A/D ya da LB/RB: bölümler arasında zaman sırasıyla gez. Yön tuşları haritada dolaşmaya kalır.</summary>
        private void UpdateMapTable()
        {
            mapPulseClock += Time.unscaledDeltaTime;
            for (int i = 0; i < storyMarkers.Count; i++)
            {
                StoryMarker m = storyMarkers[i];
                if (m.pulse == null) continue;
                bool selected = m.entry == selectedStory;
                float t = Mathf.Repeat(mapPulseClock / 1.8f, 1f);
                float a = selected && MotionAllowed ? (1f - t) * 0.8f : (selected ? 0.6f : 0f);
                m.pulse.color = new Color(theme.rust.r, theme.rust.g, theme.rust.b, a);
                m.pulse.transform.localScale = Vector3.one * (selected && MotionAllowed ? 0.8f + t * 0.9f : 1f);
            }
            int step = 0;
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.JoystickButton4)) step = -1;
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.JoystickButton5)) step = 1;
            if (step != 0) StepChapterInTime(step);
        }

        private void StepChapterInTime(int step)
        {
            List<StoryCatalogEntry> ordered = new List<StoryCatalogEntry>();
            for (int i = 0; i < storyMarkers.Count; i++) ordered.Add(storyMarkers[i].entry);
            ordered.Sort((x, y) => x.MonthIndex.CompareTo(y.MonthIndex));
            if (ordered.Count == 0) return;
            int index = Mathf.Max(0, ordered.IndexOf(selectedStory));
            int next = Mathf.Clamp(index + step, 0, ordered.Count - 1);
            if (next == index && selectedStory != null) return;
            audioManager.PlayPaper();
            SelectStory(ordered[next]);
            for (int i = 0; i < storyMarkers.Count; i++)
                if (storyMarkers[i].entry == ordered[next] && EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(storyMarkers[i].button.gameObject);
        }

        // ================================================================== iplikler

        private void BuildThreadLayer(Transform mapTransform)
        {
            threadLayer = CreateRect("Threads", mapTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).transform;
        }

        private void BuildMapNote(Transform mapTransform)
        {
            mapNote = CreateRect("Thread Note", mapTransform, new Vector2(0.018f, 0.025f), new Vector2(0.50f, 0.235f), Vector2.zero, Vector2.zero);
            AddImage(mapNote, new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.94f)).raycastTarget = false;
            AddImage(CreateRect("Rule", mapNote.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -4f), Vector2.zero), theme.rust).raycastTarget = false;
            mapNoteTitle = (TMP_Text)AsDocument(CreateText("Title", mapNote.transform, string.Empty, 13f, FontStyles.Bold, ArchiveLabel,
                new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left));
            mapNoteTitle.characterSpacing = 2f;
            mapNoteBody = CreateText("Body", mapNote.transform, string.Empty, 16f, FontStyles.Normal, theme.ink,
                new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.77f), Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            mapNoteBody.enableAutoSizing = true;
            mapNoteBody.fontSizeMin = 11f;
            mapNoteBody.fontSizeMax = 16f;
            mapNoteBody.lineSpacing = 2f;
        }

        private StoryDatabase MapStory(string storyId)
        {
            string locale = settings == null ? LocalizationService.DefaultLocale : settings.locale;
            if (mapStoryCacheLocale != locale)
            {
                mapStoryCache.Clear();
                mapStoryCacheLocale = locale;
            }
            StoryDatabase db;
            if (mapStoryCache.TryGetValue(storyId, out db)) return db;
            try
            {
                StoryRepository repository = new StoryRepository(null, locale, storyId);
                repository.Load();
                db = repository.Database;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Harita için bölüm okunamadı: " + storyId + " " + exception.Message);
                db = null;
            }
            mapStoryCache[storyId] = db;
            return db;
        }

        /// <summary>Görülmüş kesişmelerden iğne çiftlerine iplikler çıkarır.</summary>
        private void CollectThreads()
        {
            mapThreads.Clear();
            if (archiveService == null) return;
            Dictionary<string, StoryMarker> pins = new Dictionary<string, StoryMarker>();
            for (int i = 0; i < storyMarkers.Count; i++) pins[storyMarkers[i].entry.storyId] = storyMarkers[i];
            string[] seen = archiveService.SeenCrossings();
            for (int s = seen.Length - 1; s >= 0; s--)
            {
                int split = seen[s].IndexOf('>');
                if (split <= 0) continue;
                string storyId = seen[s].Substring(0, split), echoId = seen[s].Substring(split + 1);
                if (!pins.ContainsKey(storyId)) continue;
                StoryDatabase db = MapStory(storyId);
                EchoData echo = FindEcho(db, echoId);
                if (echo == null) continue;
                string other = null;
                for (int c = 0; c < echo.conditions.Length && other == null; c++)
                {
                    string otherStory, flag;
                    if (StoryVocabulary.IsArchiveType(echo.conditions[c].type) && StoryVocabulary.TrySplitArchiveKey(echo.conditions[c].key, out otherStory, out flag))
                        other = otherStory;
                }
                if (other == null || other == storyId || !pins.ContainsKey(other)) continue;
                string a = string.CompareOrdinal(storyId, other) < 0 ? storyId : other;
                string b = a == storyId ? other : storyId;
                ThreadInfo thread = mapThreads.Find(t => t.a == a && t.b == b);
                if (thread == null)
                {
                    thread = new ThreadInfo { a = a, b = b };
                    mapThreads.Add(thread);
                }
                thread.moments.Add(new KeyValuePair<string, string>(storyId, echo.text));
            }
        }

        private static EchoData FindEcho(StoryDatabase db, string echoId)
        {
            if (db == null || db.nodes == null) return null;
            for (int n = 0; n < db.nodes.Length; n++)
            {
                EchoData[] echoes = db.nodes[n] == null ? null : db.nodes[n].echoes;
                if (echoes == null) continue;
                for (int e = 0; e < echoes.Length; e++)
                    if (echoes[e] != null && echoes[e].id == echoId) return echoes[e];
            }
            return null;
        }

        /// <summary>İplikleri yeniden çizer: iki iğnenin başı arasında hafifçe sarkan bir eğri ve ortada bir düğüm.</summary>
        private void RefreshMapThreads()
        {
            if (threadLayer == null || mapRect == null) return;
            for (int i = threadLayer.childCount - 1; i >= 0; i--) Destroy(threadLayer.GetChild(i).gameObject);
            CollectThreads();
            Canvas.ForceUpdateCanvases();
            Vector2 size = mapRect.rect.size;
            if (size.x < 10f || size.y < 10f) size = new Vector2(1000f, 1000f / MapProjection.Aspect);
            Color threadColor = new Color(theme.rust.r * 0.9f, theme.rust.g * 0.8f, theme.rust.b * 0.8f, 0.85f);
            foreach (ThreadInfo thread in mapThreads)
            {
                StoryCatalogEntry ea = FindCatalogEntry(thread.a), eb = FindCatalogEntry(thread.b);
                if (ea == null || eb == null) continue;
                // İğne başları konum noktasının 26 px yukarısındadır.
                Vector2 pa = Vector2.Scale(MapProjection.Project(ea.latitude, ea.longitude), size) + new Vector2(0f, 26f);
                Vector2 pb = Vector2.Scale(MapProjection.Project(eb.latitude, eb.longitude), size) + new Vector2(0f, 26f);
                float sag = Vector2.Distance(pa, pb) * 0.08f;
                Vector2 control = (pa + pb) * 0.5f + new Vector2(0f, -sag);
                const int segments = 28;
                Vector2 prev = pa;
                for (int k = 1; k <= segments; k++)
                {
                    float t = k / (float)segments;
                    Vector2 p = (1 - t) * (1 - t) * pa + 2 * (1 - t) * t * control + t * t * pb;
                    Vector2 mid = (prev + p) * 0.5f;
                    Vector2 d = p - prev;
                    Vector2 anchor = new Vector2(mid.x / size.x, mid.y / size.y);
                    GameObject seg = CreateRect("Thread", threadLayer, anchor, anchor, Vector2.zero, Vector2.zero);
                    RectTransform rt = seg.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(d.magnitude + 1f, 2.6f);
                    rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                    AddImage(seg, threadColor).raycastTarget = false;
                    prev = p;
                }
                Vector2 knotPos = 0.25f * pa + 0.5f * control + 0.25f * pb;
                Vector2 knotUv = new Vector2(knotPos.x / size.x, knotPos.y / size.y);
                GameObject knot = CreateRect("Knot", threadLayer, knotUv, knotUv, new Vector2(-14f, -14f), new Vector2(14f, 14f));
                Image hit = AddImage(knot, new Color(0f, 0f, 0f, 0f));
                Button button = knot.AddComponent<Button>();
                button.targetGraphic = hit;
                button.transition = Selectable.Transition.None;
                AddImage(CreateRect("Rim", knot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-7f, -7f), new Vector2(7f, 7f)),
                    theme.agedPaper, theme.mapMarker).raycastTarget = false;
                thread.knot = AddImage(CreateRect("Dot", knot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-5f, -5f), new Vector2(5f, 5f)),
                    threadColor, theme.mapMarker);
                thread.knot.raycastTarget = false;
                ThreadInfo captured = thread;
                button.onClick.AddListener(() => { audioManager.PlayPaper(); ShowThreadNote(captured); });
                EventTrigger trigger = knot.AddComponent<EventTrigger>();
                AddTrigger(trigger, EventTriggerType.PointerEnter, () => ShowThreadNote(captured));
                AddTrigger(trigger, EventTriggerType.Select, () => ShowThreadNote(captured));
            }
            ShowThreadNote(null);
        }

        private StoryCatalogEntry FindCatalogEntry(string storyId)
        {
            for (int i = 0; i < storyMarkers.Count; i++)
                if (storyMarkers[i].entry.storyId == storyId) return storyMarkers[i].entry;
            return null;
        }

        /// <summary>İplik notu: seçili ipliğin iki bölümü ve birbirlerine hatırlattıkları. Seçim yoksa açıklama.</summary>
        private void ShowThreadNote(ThreadInfo thread)
        {
            if (mapNote == null) return;
            foreach (ThreadInfo t in mapThreads)
                if (t.knot != null) t.knot.transform.parent.localScale = Vector3.one * (t == thread ? 1.35f : 1f);
            if (thread == null)
            {
                mapNoteTitle.text = localization == null ? T(UiKey.MapThreadsTitle) : localization.ToUpper(T(UiKey.MapThreadsTitle));
                mapNoteBody.text = mapThreads.Count == 0 ? T(UiKey.MapThreadsEmpty) : T(UiKey.MapThreadsHint);
                return;
            }
            string titleA = CatalogTitle(thread.a) ?? thread.a, titleB = CatalogTitle(thread.b) ?? thread.b;
            string title = titleA + "  —  " + titleB;
            mapNoteTitle.text = localization == null ? title : localization.ToUpper(title);
            System.Text.StringBuilder body = new System.Text.StringBuilder();
            int shown = 0;
            for (int i = 0; i < thread.moments.Count && shown < 2; i++, shown++)
            {
                string where = CatalogTitle(thread.moments[i].Key) ?? thread.moments[i].Key;
                if (body.Length > 0) body.Append("\n\n");
                body.Append("<size=80%><b>").Append(localization == null ? where : localization.ToUpper(where)).Append("</b></size>\n");
                body.Append("<i>").Append(thread.moments[i].Value).Append("</i>");
            }
            mapNoteBody.text = body.ToString();
        }

        // ================================================================== son tanıklık

        private void BuildStoryQuote(Transform artTransform)
        {
            storyQuote = CreateRect("Last Testimony", artTransform, Vector2.zero, new Vector2(1f, 0.46f), Vector2.zero, Vector2.zero);
            AddImage(storyQuote, new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.78f)).raycastTarget = false;
            storyQuoteKicker = (TMP_Text)AsDocument(CreateText("Kicker", storyQuote.transform, string.Empty, 11f, FontStyles.Bold, theme.mustard,
                new Vector2(0.05f, 0.70f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left));
            storyQuoteKicker.characterSpacing = 2f;
            storyQuoteText = CreateText("Quote", storyQuote.transform, string.Empty, 15f, FontStyles.Italic, theme.agedPaper,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.72f), Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            storyQuoteText.enableAutoSizing = true;
            storyQuoteText.fontSizeMin = 10f;
            storyQuoteText.fontSizeMax = 15f;
            storyQuote.SetActive(false);
        }

        private void RefreshStoryQuote()
        {
            if (storyQuote == null) return;
            string mode;
            int[] lines;
            StoryDatabase db = selectedStory == null ? null : MapStory(selectedStory.storyId);
            TestimonyData testimony = db == null ? null : db.testimony;
            if (selectedStory == null || archiveService == null || testimony == null || !archiveService.TryGetTestimony(selectedStory.storyId, out mode, out lines))
            {
                storyQuote.SetActive(false);
                return;
            }
            TestimonyLine[] source = mode == StoryController.TestimonyDone ? testimony.done : testimony.undone;
            int index = lines[0];
            if (source == null || index < 0 || index >= source.Length || source[index] == null)
            {
                storyQuote.SetActive(false);
                return;
            }
            string kicker = T(UiKey.MapLastTestimony) + "  ·  " + testimony.kicker;
            storyQuoteKicker.text = localization == null ? kicker : localization.ToUpper(kicker);
            storyQuoteText.text = "“" + source[index].text.Trim() + "”";
            storyQuote.SetActive(true);
        }

        /// <summary>Harita açılırken: iplikler, iğneler ve kart arşivin son hâline göre.</summary>
        private void RefreshMapTable()
        {
            RefreshMapThreads();
            PaintMapPins();
            RefreshStoryQuote();
            RefreshTimelineHint();
        }
    }
}
