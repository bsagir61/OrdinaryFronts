using UnityEngine;

namespace OrdinaryFronts
{
    [CreateAssetMenu(menuName = "Ordinary Fronts/Theme Config", fileName = "ThemeConfig")]
    public sealed class ThemeConfig : ScriptableObject
    {
        public Color sootNavy = new Color32(0x17, 0x1B, 0x21, 0xFF);
        public Color agedPaper = new Color32(0xD8, 0xCF, 0xB6, 0xFF);
        public Color rust = new Color32(0x9E, 0x44, 0x34, 0xFF);
        public Color petrol = new Color32(0x3F, 0x64, 0x68, 0xFF);
        public Color mustard = new Color32(0xA8, 0x8B, 0x4A, 0xFF);
        public Color ink = new Color32(0x26, 0x25, 0x22, 0xFF);
        public Sprite paperPanel;
        public Sprite buttonPanel;
        public Sprite vignette;

        /// <summary>Açılış kurgusunda çok düşük opaklıkta döndürülen arşiv filmi grenleri.</summary>
        public Sprite[] introGrain = new Sprite[0];

        /// <summary>Bölüm seçim ekranının Avrupa haritası; işaretler bunun üstüne yerleşir.</summary>
        public Sprite europeMap;

        /// <summary>Harita işaretlerinin yuvarlak diski; halka ve nokta bununla boyanır.</summary>
        public Sprite mapMarker;

        /// <summary>Açılış metninin arkasındaki yumuşak dikey gradyan.</summary>
        public Sprite introTextScrim;

        /// <summary>
        /// Anlatı katmanı: başlıklar, düğüm gövdesi ve final paragrafları. Serif.
        /// </summary>
        public TMPro.TMP_FontAsset serifFont;

        /// <summary>
        /// Belge katmanı: kayıt defteri, final raporu, etiketler, tarih/konum satırı. Daktilo.
        /// </summary>
        public TMPro.TMP_FontAsset monoFont;

        [Range(0.18f, 0.35f)] public float transitionDuration = 0.26f;
        /// <summary>Açılış kartları arasındaki geçiş; UI geçişlerinden bilinçli olarak daha yavaştır.</summary>
        [Range(0.30f, 0.9f)] public float introFadeDuration = 0.45f;

        public void ApplyCanonicalDefaults()
        {
            sootNavy = new Color32(0x17, 0x1B, 0x21, 0xFF);
            agedPaper = new Color32(0xD8, 0xCF, 0xB6, 0xFF);
            rust = new Color32(0x9E, 0x44, 0x34, 0xFF);
            petrol = new Color32(0x3F, 0x64, 0x68, 0xFF);
            mustard = new Color32(0xA8, 0x8B, 0x4A, 0xFF);
            ink = new Color32(0x26, 0x25, 0x22, 0xFF);
            transitionDuration = 0.26f;
            introFadeDuration = 0.45f;
        }
    }
}
