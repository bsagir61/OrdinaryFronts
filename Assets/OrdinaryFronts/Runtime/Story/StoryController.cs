using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrdinaryFronts
{
    public sealed class StoryController
    {
        private StoryRepository repository;
        private readonly SaveService saveService;
        private readonly ArchiveService archive;

        public StoryDatabase Story { get { return repository.Database; } }
        public GameState State { get; private set; }
        public StoryNode CurrentNode { get { return State == null ? null : repository.GetNode(State.currentNodeId); } }

        public StoryController(StoryRepository repository, SaveService saveService, ArchiveService archive = null)
        {
            this.repository = repository;
            this.saveService = saveService;
            this.archive = archive;
        }

        /// <summary>Antolojinin kalıcı belleği; sağlanmamışsa kesişmeler ve önceki oynanış izi kapalıdır.</summary>
        public ArchiveService Archive { get { return archive; } }

        public void Initialize()
        {
            if (repository.Database == null) repository.Load();
        }

        public StoryNode StartNew()
        {
            Initialize();
            State = GameState.Create(repository.Database);
            StoryNode node = CurrentNode;
            if (node == null) throw new InvalidOperationException("Başlangıç düğümü bulunamadı.");
            State.activeChapter = node.act;
            saveService.Save(State);
            return node;
        }

        public bool TryContinue(out string userMessageKey)
        {
            Initialize();
            GameState loaded;
            if (!saveService.TryLoad(out loaded, out userMessageKey)) return false;
            if (loaded.storyId != repository.Database.storyId || repository.GetNode(loaded.currentNodeId) == null)
            {
                userMessageKey = UiKey.SaveIncompatible;
                return false;
            }
            State = loaded;
            return true;
        }

        public ChoiceOutcome Choose(int index)
        {
            StoryNode source = CurrentNode;
            if (source == null || source.IsEnding) throw new InvalidOperationException("Bu düğümde seçim yapılamaz.");
            if (source.choices == null || index < 0 || index >= source.choices.Length)
                throw new ArgumentOutOfRangeException("index");
            ChoiceData choice = source.choices[index];
            if (!ConditionEvaluator.EvaluateAll(choice.conditions, State, archive))
                throw new InvalidOperationException("Bu seçim mevcut durumda kullanılamıyor.");

            EffectResolver.Apply(choice.effects, State);
            // Arşiv, kararı verildiği anda hatırlar; bölüm yarım kalsa bile bir sonraki
            // oynanışta oyuncu bu düğümde ne yaptığını defterinde okuyabilir.
            if (archive != null) archive.RememberChoice(State.storyId, source.id, choice.id);
            // İz, kararın alındığı düğümün bölümüyle etiketlenir; final raporu buna göre gruplar.
            State.AddTrace(source.act, choice.trace);
            State.currentNodeId = choice.nextNodeId;
            StoryNode destination = CurrentNode;
            if (destination == null) throw new InvalidOperationException("Seçimin hedef düğümü bulunamadı: " + choice.nextNodeId);
            State.activeChapter = destination.act;
            State.completed = destination.IsEnding;
            saveService.Save(State);
            // Bölüm bittiğinde bu oynanışın bayrakları arşive geçer: diğer bölümlerdeki
            // kesişmeler yalnız tamamlanmış oynanışlara bakar.
            if (destination.IsEnding && archive != null) archive.RememberCompletion(State, destination.ending.id);

            return new ChoiceOutcome
            {
                source = source,
                choice = choice,
                destination = destination
            };
        }

        /// <summary>
        /// Ara sahnenin bittiğini ve (varsa) sonucunu kayda yazar. Sonuç bayrağı, sahnenin
        /// tanımındaki sonuç listesinde olmalıdır; değilse yazılmaz ve hata bırakılır, çünkü
        /// doğrulayıcı yankıları yalnız ilan edilmiş sonuçlara bağlamaya izin verir.
        /// </summary>
        public void RecordInterlude(InterludeData interlude, string resultFlag)
        {
            if (interlude == null || State == null) return;
            State.MarkResultSeen(StoryVocabulary.InterludeSeenKey(interlude.id));
            if (!string.IsNullOrWhiteSpace(resultFlag))
            {
                bool declared = false;
                if (interlude.results != null)
                    for (int i = 0; i < interlude.results.Length; i++) if (interlude.results[i] == resultFlag) declared = true;
                if (declared) State.SetFlag(resultFlag, true);
                else Debug.LogError("Ara sahne ilan edilmemiş bir sonuç üretti: " + interlude.id + " -> " + resultFlag);
            }
            saveService.Save(State);
        }

        /// <summary>
        /// Görünür hâle gelen yankı satırlarını döndürür. Başa eklenen "önceki kararın yankısı"
        /// ifadesi arayüz metnidir ve sunum katmanında dile göre eklenir.
        /// </summary>
        public string[] ConsumeEchoes(StoryNode node)
        {
            if (node == null || State == null || node.echoes == null) return Array.Empty<string>();
            List<string> lines = new List<string>();
            bool changed = false;
            for (int i = 0; i < node.echoes.Length; i++)
            {
                EchoData echo = node.echoes[i];
                if (echo == null || string.IsNullOrWhiteSpace(echo.text) || State.HasSeenResult(echo.id)) continue;
                if (!ConditionEvaluator.EvaluateAll(echo.conditions, State, archive)) continue;
                lines.Add(echo.text.Trim());
                State.MarkResultSeen(echo.id);
                changed = true;
            }
            if (changed) saveService.Save(State);
            return lines.ToArray();
        }

        /// <summary>
        /// Bu düğümde bir önceki oynanışta seçilen seçeneğin metni. Yalnız daha önce
        /// bu düğümden geçilmişse ve o seçenek hâlâ düğümde varsa döner; hiçbir sıra ya da
        /// "doğru cevap" ima etmez, yalnız kayıttır.
        /// </summary>
        public string PreviousChoiceText(StoryNode node)
        {
            if (node == null || archive == null || State == null || node.choices == null) return null;
            string choiceId = archive.PreviousChoice(State.storyId, node.id);
            if (string.IsNullOrWhiteSpace(choiceId)) return null;
            for (int i = 0; i < node.choices.Length; i++)
                if (node.choices[i] != null && node.choices[i].id == choiceId) return node.choices[i].text;
            return null;
        }

        /// <summary>
        /// Yapılmayanlar: rota boyunca geçilen düğümlerde <b>alınmamış</b> ve <c>omission</c>
        /// metni yazılmış seçenekler. Rapor, yapılanların yanına bırakılanları da koyar.
        /// Hangi düğümlerden geçildiği izlerden değil arşivden okunur; çünkü iz kararın
        /// cümlesini saklar, düğümü değil.
        /// </summary>
        public string[] BuildOmissions()
        {
            List<string> result = new List<string>();
            if (State == null || archive == null || repository.Database == null || repository.Database.nodes == null) return result.ToArray();
            StoryNode[] nodes = repository.Database.nodes;
            for (int i = 0; i < nodes.Length; i++)
            {
                StoryNode node = nodes[i];
                if (node == null || node.IsEnding || node.choices == null) continue;
                string taken = archive.PreviousChoice(State.storyId, node.id);
                if (string.IsNullOrWhiteSpace(taken)) continue;
                // Yalnız bu oynanışta gerçekten geçilmiş düğümler: izlerde o düğümün alınan
                // seçiminin cümlesi bulunmalı.
                bool passedThisRun = false;
                for (int c = 0; c < node.choices.Length && !passedThisRun; c++)
                    if (node.choices[c] != null && node.choices[c].id == taken && HasTrace(node.choices[c].trace)) passedThisRun = true;
                if (!passedThisRun) continue;
                for (int c = 0; c < node.choices.Length; c++)
                {
                    ChoiceData other = node.choices[c];
                    if (other == null || other.id == taken || string.IsNullOrWhiteSpace(other.omission)) continue;
                    result.Add(other.omission.Trim());
                }
            }
            return result.ToArray();
        }

        private bool HasTrace(string trace)
        {
            if (State == null || State.traces == null || string.IsNullOrWhiteSpace(trace)) return false;
            string wanted = trace.Trim();
            for (int i = 0; i < State.traces.Length; i++)
                if (State.traces[i] != null && State.traces[i].text == wanted) return true;
            return false;
        }

        /// <summary>
        /// Dil değiştiğinde oyuncunun ilerlemesini koruyarak hikâye kaynağını değiştirir.
        /// Düğüm kimlikleri diller arasında aynı olduğu için mevcut durum geçerli kalır.
        /// </summary>
        public void SwapRepository(StoryRepository next)
        {
            if (next == null) throw new ArgumentNullException("next");
            repository = next;
            Initialize();
        }

        /// <summary>
        /// Final raporu. Eskiden yalnız son beş iz gösteriliyordu; bu, 14-18 kararlık bir
        /// rotanın sonundaki birbirine benzer bürokratik adımları listeliyor, oyunun asıl
        /// belirleyici kararlarını (kimi kurtardığın, neyi kayda geçirdiğin, kimi bulduğun)
        /// hiç göstermiyordu. Rapor artık bölümlere göre gruplanır ve her bölümden o
        /// bölümün ilk ve son kararı alınır: özet, rotanın tamamını kapsar.
        /// </summary>
        public TraceEntry[] BuildEndingTraces(EndingData ending)
        {
            List<TraceEntry> result = new List<TraceEntry>();
            if (State != null && State.traces != null)
            {
                List<string> actOrder = new List<string>();
                Dictionary<string, List<TraceEntry>> byAct = new Dictionary<string, List<TraceEntry>>();
                for (int i = 0; i < State.traces.Length; i++)
                {
                    TraceEntry entry = State.traces[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.text)) continue;
                    string act = entry.act ?? string.Empty;
                    if (!byAct.ContainsKey(act))
                    {
                        byAct.Add(act, new List<TraceEntry>());
                        actOrder.Add(act);
                    }
                    byAct[act].Add(entry);
                }

                for (int i = 0; i < actOrder.Count; i++)
                {
                    List<TraceEntry> inAct = byAct[actOrder[i]];
                    result.Add(inAct[0]);
                    if (inAct.Count > 1) result.Add(inAct[inAct.Count - 1]);
                }
            }

            // Hiç iz toplanmadıysa (ör. bozuk kayıt) final kendi yedek satırlarını verir.
            if (result.Count == 0 && ending != null && ending.traceFallbacks != null)
            {
                for (int i = 0; i < ending.traceFallbacks.Length && result.Count < 3; i++)
                    if (!string.IsNullOrWhiteSpace(ending.traceFallbacks[i]))
                        result.Add(new TraceEntry { act = string.Empty, text = ending.traceFallbacks[i] });
            }
            return result.ToArray();
        }
    }

    public sealed class ChoiceOutcome
    {
        public StoryNode source;
        public ChoiceData choice;
        public StoryNode destination;
    }
}
