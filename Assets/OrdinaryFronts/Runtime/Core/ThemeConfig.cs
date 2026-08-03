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
        [Range(0.18f, 0.35f)] public float transitionDuration = 0.26f;

        public void ApplyCanonicalDefaults()
        {
            sootNavy = new Color32(0x17, 0x1B, 0x21, 0xFF);
            agedPaper = new Color32(0xD8, 0xCF, 0xB6, 0xFF);
            rust = new Color32(0x9E, 0x44, 0x34, 0xFF);
            petrol = new Color32(0x3F, 0x64, 0x68, 0xFF);
            mustard = new Color32(0xA8, 0x8B, 0x4A, 0xFF);
            ink = new Color32(0x26, 0x25, 0x22, 0xFF);
            transitionDuration = 0.26f;
        }
    }
}
