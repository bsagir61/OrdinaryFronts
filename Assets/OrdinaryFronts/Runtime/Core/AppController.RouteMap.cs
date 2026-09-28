using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OrdinaryFronts
{
    /// <summary>
    /// Yol haritası ekranı. Duraklatma menüsünden ve final ekranından açılır. Soldan sağa
    /// karar sırası; perdeler arka planda soluk bantlar. Pas renkli kalın çizgi bu oynanış,
    /// kâğıt renkli çizgi önceki oynanışlarda yürünmüş yollar, kesik çizgi ve boş halka
    /// yürünmüş bir durağın bir adım ötesindeki yürünmemiş yol. Daha ötesi, sayı, yüzde ya
    /// da kilit gösterilmez (bkz. <see cref="RouteMap"/>).
    /// </summary>
    public sealed partial class AppController
    {
        private RectTransform routePlot;
        private TMP_Text routeTitle;
        private TMP_Text routeDetail;
        private AppScreen routeReturn = AppScreen.Pause;
        private readonly List<GameObject> routeObjects = new List<GameObject>();

        private void BuildRouteMapScreen(Transform parent)
        {
            GameObject screen = CreateScreen("Route Map", parent);
            router.Register(AppScreen.RouteMap, screen);
            AddImage(CreateRect("Dim", screen.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero),
                new Color(theme.sootNavy.r, theme.sootNavy.g, theme.sootNavy.b, 0.96f)).raycastTarget = false;

            routeTitle = CreateText("Title", screen.transform, string.Empty, 40f, FontStyles.Bold, theme.agedPaper,
                new Vector2(0.04f, 0.885f), new Vector2(0.76f, 0.96f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            routeTitle.characterSpacing = 2f;
            AddImage(CreateRect("Rule", screen.transform, new Vector2(0.04f, 0.875f), new Vector2(0.16f, 0.882f), Vector2.zero, Vector2.zero), theme.rust).raycastTarget = false;
            CreateButton("Back", screen.transform, T(UiKey.SettingsBack), CloseRouteMap, new Vector2(0.80f, 0.895f), new Vector2(0.96f, 0.955f));

            routePlot = CreateRect("Plot", screen.transform, new Vector2(0.04f, 0.25f), new Vector2(0.96f, 0.84f), Vector2.zero, Vector2.zero).GetComponent<RectTransform>();

            // Açıklama: üç işaret ve anlamları.
            GameObject legend = CreateRect("Legend", screen.transform, new Vector2(0.04f, 0.035f), new Vector2(0.245f, 0.20f), Vector2.zero, Vector2.zero);
            LegendRow(legend.transform, 0.72f, theme.rust, 5f, false, UiKey.RouteLegendCurrent);
            LegendRow(legend.transform, 0.44f, new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.6f), 3f, false, UiKey.RouteLegendWalked);
            LegendRow(legend.transform, 0.16f, new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.35f), 2f, true, UiKey.RouteLegendUntaken);

            GameObject panel = CreatePaperPanel("Detail Panel", screen.transform, new Vector2(0.26f, 0.035f), new Vector2(0.96f, 0.20f));
            routeDetail = CreateText("Detail", panel.transform, string.Empty, 22f, FontStyles.Normal, theme.ink,
                new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.92f), Vector2.zero, Vector2.zero, TextAlignmentOptions.MidlineLeft);
            routeDetail.enableAutoSizing = true;
            routeDetail.fontSizeMin = 15f;
            routeDetail.fontSizeMax = 22f;
        }

        private void LegendRow(Transform parent, float y, Color color, float thickness, bool dashed, string key)
        {
            GameObject row = CreateRect("Row", parent, new Vector2(0f, y - 0.12f), new Vector2(1f, y + 0.12f), Vector2.zero, Vector2.zero);
            if (dashed)
                for (int i = 0; i < 3; i++)
                    AddImage(CreateRect("Dash", row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * 16f, -thickness * 0.5f), new Vector2(i * 16f + 9f, thickness * 0.5f)), color).raycastTarget = false;
            else
                AddImage(CreateRect("Line", row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, -thickness * 0.5f), new Vector2(42f, thickness * 0.5f)), color).raycastTarget = false;
            TMP_Text text = (TMP_Text)AsDocument(CreateText("Label", row.transform, T(key), 15f, FontStyles.Bold,
                new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, 0.8f),
                Vector2.zero, Vector2.one, new Vector2(56f, 0f), Vector2.zero, TextAlignmentOptions.MidlineLeft));
            text.characterSpacing = 1f;
        }

        private void OpenRouteMap(AppScreen returnTo)
        {
            if (storyController == null || storyController.Story == null || storyController.State == null) return;
            routeReturn = returnTo;
            audioManager.PlayPaper();
            router.Show(AppScreen.RouteMap);
            Canvas.ForceUpdateCanvases();
            GameObject focus = PopulateRouteMap();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(inputDevice == InputDevice.Gamepad ? focus : null);
        }

        private void CloseRouteMap()
        {
            audioManager.PlayBack();
            router.Show(routeReturn);
            SelectFirstButton(router.Get(routeReturn));
        }

        /// <summary>Haritayı sıfırdan çizer ve oyun kolu için odaklanacak durağı döndürür.</summary>
        private GameObject PopulateRouteMap()
        {
            for (int i = 0; i < routeObjects.Count; i++) if (routeObjects[i] != null) Destroy(routeObjects[i]);
            routeObjects.Clear();

            StoryDatabase story = storyController.Story;
            GameState state = storyController.State;
            string[] walked = archiveService == null ? Array.Empty<string>() : archiveService.WalkedSteps(story.storyId);
            RouteMap map = RouteMap.Build(story, walked, state.path, state.currentNodeId);

            string chapterTitle = story.storyId;
            if (catalog != null && catalog.entries != null)
                for (int i = 0; i < catalog.entries.Length; i++)
                    if (catalog.entries[i] != null && catalog.entries[i].storyId == story.storyId) chapterTitle = catalog.entries[i].title;
            routeTitle.text = T(UiKey.RouteTitle) + "   ·   " + chapterTitle;
            routeDetail.text = T(UiKey.RouteHint);

            Vector2 size = routePlot.rect.size;
            if (size.x < 100f || size.y < 100f) size = new Vector2(1766f, 637f);
            const float pad = 60f;
            Dictionary<RouteStop, Vector2> positions = new Dictionary<RouteStop, Vector2>();
            foreach (RouteStop stop in map.stops)
            {
                float x = pad + (map.maxDepth <= 0 ? 0.5f : stop.depth / (float)map.maxDepth) * (size.x - pad * 2f);
                float spacing = Mathf.Min(84f, (size.y - 90f) / Mathf.Max(1, stop.rowCount));
                float y = size.y * 0.47f + ((stop.rowCount - 1) * 0.5f - stop.row) * spacing;
                positions[stop] = new Vector2(x, y);
            }

            DrawActBands(map, positions, size);
            foreach (RouteEdge edge in map.edges) DrawRouteEdge(edge, positions[edge.from], positions[edge.to]);

            GameObject focus = null;
            RouteStop deepestCurrent = null;
            foreach (RouteStop stop in map.stops)
            {
                GameObject button = DrawRouteStop(stop, positions[stop], state.currentNodeId);
                if (stop.state == RouteStopState.Current && (deepestCurrent == null || stop.depth > deepestCurrent.depth))
                {
                    deepestCurrent = stop;
                    focus = button;
                }
            }
            return focus;
        }

        /// <summary>
        /// Perde bantları: yalnız haritada durağı olan perdeler, en küçük ve en büyük sütun
        /// arasında soluk bir zemin ve üstte perdenin adı. Henüz varılmamış perdenin adı yazılmaz.
        /// </summary>
        private void DrawActBands(RouteMap map, Dictionary<RouteStop, Vector2> positions, Vector2 size)
        {
            List<string> order = new List<string>();
            Dictionary<string, Vector2> ranges = new Dictionary<string, Vector2>();
            Dictionary<string, bool> reached = new Dictionary<string, bool>();
            foreach (RouteStop stop in map.stops)
            {
                string act = stop.node.act ?? string.Empty;
                float x = positions[stop].x;
                if (!ranges.ContainsKey(act))
                {
                    order.Add(act);
                    ranges[act] = new Vector2(x, x);
                    reached[act] = false;
                }
                ranges[act] = new Vector2(Mathf.Min(ranges[act].x, x), Mathf.Max(ranges[act].y, x));
                if (stop.state != RouteStopState.Untaken) reached[act] = true;
            }
            for (int i = 0; i < order.Count; i++)
            {
                Vector2 range = ranges[order[i]];
                float left = range.x - 36f;
                float right = range.y + 36f;
                GameObject band = CreateRect("Act Band", routePlot, Vector2.zero, Vector2.zero, new Vector2(left, 0f), new Vector2(right, size.y));
                AddImage(band, new Color(theme.agedPaper.r, theme.agedPaper.g, theme.agedPaper.b, i % 2 == 0 ? 0.035f : 0.06f)).raycastTarget = false;
                routeObjects.Add(band);
                if (!reached[order[i]]) continue;
                string name = localization == null ? order[i] : localization.ToUpper(order[i]);
                TMP_Text header = (TMP_Text)AsDocument(CreateText("Act", band.transform, name, 15f, FontStyles.Bold, theme.mustard,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -34f), new Vector2(-4f, -6f), TextAlignmentOptions.TopLeft));
                header.characterSpacing = 4f;
                header.overflowMode = TextOverflowModes.Overflow;
                header.enableWordWrapping = false;
            }
        }

        private void DrawRouteEdge(RouteEdge edge, Vector2 a, Vector2 b)
        {
            Vector2 d = b - a;
            float length = d.magnitude;
            if (length < 1f) return;
            Vector2 dir = d / length;
            const float gap = 16f;
            a += dir * gap;
            length -= gap * 2f;
            if (length <= 2f) return;
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            Color paper = theme.agedPaper;
            if (edge.current) Segment(a, dir, 0f, length, 4f, theme.rust, angle);
            else if (edge.walked) Segment(a, dir, 0f, length, 3f, new Color(paper.r, paper.g, paper.b, 0.55f), angle);
            else
                for (float s = 0f; s < length; s += 18f)
                    Segment(a, dir, s, Mathf.Min(9f, length - s), 2f, new Color(paper.r, paper.g, paper.b, 0.30f), angle);
        }

        private void Segment(Vector2 origin, Vector2 dir, float offset, float length, float thickness, Color color, float angle)
        {
            GameObject line = CreateRect("Edge", routePlot, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            RectTransform rect = line.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = origin + dir * offset;
            rect.sizeDelta = new Vector2(length, thickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
            AddImage(line, color).raycastTarget = false;
            routeObjects.Add(line);
        }

        private GameObject DrawRouteStop(RouteStop stop, Vector2 position, string currentNodeId)
        {
            GameObject root = CreateRect("Stop " + stop.node.id, routePlot, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(38f, 38f);
            routeObjects.Add(root);

            Image hit = AddImage(root, new Color(0f, 0f, 0f, 0f));
            Button button = root.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => ShowRouteDetail(stop));
            EventTrigger trigger = root.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.PointerEnter, () => ShowRouteDetail(stop));
            AddTrigger(trigger, EventTriggerType.Select, () => ShowRouteDetail(stop));

            Color paper = theme.agedPaper;
            bool reached = stop.state != RouteStopState.Untaken;
            if (stop.IsEnding)
            {
                GameObject diamond = CreateRect("Seal", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-11f, -11f), new Vector2(11f, 11f));
                diamond.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0f, 0f, 45f);
                AddImage(diamond, reached ? theme.mustard : new Color(paper.r, paper.g, paper.b, 0.25f)).raycastTarget = false;
                if (stop.state == RouteStopState.Current) Ring(root.transform, 38f, theme.rust);
                if (reached) StopLabel(root.transform, stop.node.ending.title, theme.mustard);
            }
            else if (stop.state == RouteStopState.Untaken)
            {
                Ring(root.transform, 16f, new Color(paper.r, paper.g, paper.b, 0.45f));
            }
            else
            {
                if (stop.state == RouteStopState.Current) Ring(root.transform, 28f, theme.rust);
                AddImage(CreateRect("Dot", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-7f, -7f), new Vector2(7f, 7f)),
                    stop.state == RouteStopState.Current ? paper : new Color(paper.r, paper.g, paper.b, 0.7f), theme.mapMarker).raycastTarget = false;
                if (stop.node.id == currentNodeId) StopLabel(root.transform, T(UiKey.RouteHere), theme.rust);
            }
            return root;
        }

        private void Ring(Transform parent, float size, Color color)
        {
            Image ring = AddImage(CreateRect("Ring", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-size * 0.5f, -size * 0.5f), new Vector2(size * 0.5f, size * 0.5f)),
                color, InterludeSprite("il_ring"));
            ring.raycastTarget = false;
        }

        private void StopLabel(Transform parent, string text, Color color)
        {
            TMP_Text label = (TMP_Text)AsDocument(CreateText("Label", parent, localization == null ? text : localization.ToUpper(text), 14f, FontStyles.Bold, color,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-130f, -32f), new Vector2(130f, -6f), TextAlignmentOptions.Top));
            label.characterSpacing = 2f;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action action)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        /// <summary>
        /// Ayrıntı: yürünmemiş durakta yalnız "yürünmemiş bir yol" (nereye çıktığı söylenmez);
        /// ulaşılmış finalde başlığı; diğer duraklarda yer, tarih ve orada verilen kararlar —
        /// bu oynanıştaki ve son yürüyüşteki.
        /// </summary>
        private void ShowRouteDetail(RouteStop stop)
        {
            if (routeDetail == null || stop == null) return;
            if (stop.state == RouteStopState.Untaken)
            {
                routeDetail.text = T(UiKey.RouteUntaken);
                return;
            }
            StoryNode node = stop.node;
            if (node.IsEnding)
            {
                routeDetail.text = "<b>" + node.ending.title + "</b>\n" + T(UiKey.RouteEndingReached);
                return;
            }
            System.Text.StringBuilder text = new System.Text.StringBuilder();
            string meta = (node.date ?? string.Empty) + "   ·   " + (node.location ?? string.Empty);
            text.Append(LabelMarkup(localization == null ? meta : localization.ToUpper(meta)));
            string thisRun = ChoiceTextFromPath(node, storyController.State.path);
            string lastRun = archiveService == null ? null : ChoiceText(node, archiveService.PreviousChoice(storyController.Story.storyId, node.id));
            if (!string.IsNullOrEmpty(thisRun)) text.Append('\n').Append(T(UiKey.RouteThisRun)).Append(": ").Append(thisRun);
            if (!string.IsNullOrEmpty(lastRun) && lastRun != thisRun) text.Append('\n').Append(T(UiKey.RouteLastRun)).Append(": ").Append(lastRun);
            if (string.IsNullOrEmpty(thisRun) && node.id == storyController.State.currentNodeId) text.Append('\n').Append(T(UiKey.RouteHere));
            routeDetail.text = text.ToString();
        }

        private static string ChoiceTextFromPath(StoryNode node, string[] path)
        {
            if (path == null || node.choices == null) return null;
            for (int i = path.Length - 1; i >= 0; i--)
            {
                string step = path[i];
                int split = step == null ? -1 : step.IndexOf(RouteMap.StepSeparator);
                if (split <= 0 || step.Substring(0, split) != node.id) continue;
                return ChoiceText(node, step.Substring(split + 1));
            }
            return null;
        }

        private static string ChoiceText(StoryNode node, string choiceId)
        {
            if (string.IsNullOrEmpty(choiceId) || node.choices == null) return null;
            for (int i = 0; i < node.choices.Length; i++)
                if (node.choices[i] != null && node.choices[i].id == choiceId) return node.choices[i].text;
            return null;
        }
    }
}
