using System;
using System.Collections.Generic;

namespace OrdinaryFronts
{
    public sealed class StoryController
    {
        private StoryRepository repository;
        private readonly SaveService saveService;

        public StoryDatabase Story { get { return repository.Database; } }
        public GameState State { get; private set; }
        public StoryNode CurrentNode { get { return State == null ? null : repository.GetNode(State.currentNodeId); } }

        public StoryController(StoryRepository repository, SaveService saveService)
        {
            this.repository = repository;
            this.saveService = saveService;
        }

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
            if (!ConditionEvaluator.EvaluateAll(choice.conditions, State))
                throw new InvalidOperationException("Bu seçim mevcut durumda kullanılamıyor.");

            EffectResolver.Apply(choice.effects, State);
            State.AddTrace(choice.trace);
            State.currentNodeId = choice.nextNodeId;
            StoryNode destination = CurrentNode;
            if (destination == null) throw new InvalidOperationException("Seçimin hedef düğümü bulunamadı: " + choice.nextNodeId);
            State.activeChapter = destination.act;
            State.completed = destination.IsEnding;
            saveService.Save(State);

            return new ChoiceOutcome
            {
                source = source,
                choice = choice,
                destination = destination
            };
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
                if (!ConditionEvaluator.EvaluateAll(echo.conditions, State)) continue;
                lines.Add(echo.text.Trim());
                State.MarkResultSeen(echo.id);
                changed = true;
            }
            if (changed) saveService.Save(State);
            return lines.ToArray();
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

        public string[] BuildEndingTraces(EndingData ending)
        {
            List<string> result = new List<string>();
            if (State != null && State.traces != null)
            {
                int start = Math.Max(0, State.traces.Length - 5);
                for (int i = start; i < State.traces.Length; i++)
                    if (!string.IsNullOrWhiteSpace(State.traces[i])) result.Add(State.traces[i]);
            }
            if (ending != null && ending.traceFallbacks != null)
            {
                for (int i = 0; i < ending.traceFallbacks.Length && result.Count < 3; i++)
                    if (!string.IsNullOrWhiteSpace(ending.traceFallbacks[i]) && !result.Contains(ending.traceFallbacks[i]))
                        result.Add(ending.traceFallbacks[i]);
            }
            while (result.Count > 5) result.RemoveAt(0);
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
