using UnityEngine;

namespace OrdinaryFronts
{
    public static class EffectResolver
    {
        public static void Apply(EffectData[] effects, GameState state)
        {
            if (effects == null || state == null) return;
            for (int i = 0; i < effects.Length; i++) Apply(effects[i], state);
        }

        public static void Apply(EffectData effect, GameState state)
        {
            if (effect == null || state == null) return;
            string type = StoryVocabulary.Normalize(effect.type);
            string operation = StoryVocabulary.EffectOperationOrDefault(effect.op);

            // Bilinmeyen bir operasyonda sessizce "set" davranışına düşmek, yazım hatası olan
            // bir "add" etkisini fark edilmeden durum sıfırlamaya çevirir. Bunun yerine etkiyi
            // uygulamadan hata bırakıyoruz; StoryGraphValidator aynı hatayı açılışta yakalar.
            if (!StoryVocabulary.IsKnownEffectOperation(operation))
            {
                Debug.LogError("Bilinmeyen etki operasyonu; etki uygulanmadı. type=" + effect.type + " op=" + effect.op + " key=" + effect.key);
                return;
            }

            bool add = operation == StoryVocabulary.OperationAdd;
            if (type == StoryVocabulary.TypeRelation)
            {
                int current = state.GetRelation(effect.key);
                state.SetRelation(effect.key, add ? current + effect.intValue : effect.intValue);
            }
            else if (StoryVocabulary.IsFlagType(type))
            {
                // "add" bayraklarda "aç" kısayoludur: boolValue ne olursa olsun bayrağı true yazar.
                // Bir bayrağı kapatmak için op="set" ve boolValue=false kullanılmalıdır.
                state.SetFlag(effect.key, effect.boolValue || add);
            }
            else
            {
                Debug.LogError("Bilinmeyen etki türü; etki uygulanmadı. type=" + effect.type + " key=" + effect.key);
            }
        }
    }
}
