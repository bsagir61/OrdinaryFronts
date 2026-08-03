namespace OrdinaryFronts
{
    public static class EffectResolver
    {
        public static void Apply(EffectData[] effects, GameState state)
        {
            if (effects == null || state == null) return;
            for (int i = 0; i < effects.Length; i++) Apply(effects[i], state);
            state.stats.Clamp();
        }

        public static void Apply(EffectData effect, GameState state)
        {
            if (effect == null || state == null) return;
            string type = (effect.type ?? string.Empty).Trim().ToLowerInvariant();
            string operation = (effect.op ?? "set").Trim().ToLowerInvariant();
            if (type == "stat")
            {
                int current = state.stats.Get(effect.key);
                state.stats.Set(effect.key, operation == "add" ? current + effect.intValue : effect.intValue);
            }
            else if (type == "relation")
            {
                int current = state.GetRelation(effect.key);
                state.SetRelation(effect.key, operation == "add" ? current + effect.intValue : effect.intValue);
            }
            else if (type == "flag" || type == "echo")
            {
                state.SetFlag(effect.key, effect.boolValue || operation == "add");
            }
        }
    }
}
