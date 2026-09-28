using System.Collections.Generic;
using UnityEngine;
using static OrdinaryFronts.Editor.LinocutSceneFactory;

namespace OrdinaryFronts.Editor
{
    /// <summary>
    /// Ara sahnelerin hareketli parçalarını üretir: saydam zeminli silüet sprite'ları.
    /// Sahne arka planlarıyla aynı çizim kodunu kullanır (<see cref="HungerWinterSceneFactory"/>);
    /// fark, tek bir figürün ya da aracın kendi küçük tuvaline, dört yürüyüş karesiyle
    /// çizilmesidir. Böylece ara sahnede yürüyen figür, aynı düğümün arka planındaki figürle
    /// aynı elden çıkmış görünür.
    /// </summary>
    internal static class InterludeSpriteFactory
    {
        internal const string Folder = "Interludes";

        /// <summary>Üretilen dosyalar; anahtar = uzantısız dosya adı, sahne kodu bu adla erişir.</summary>
        internal static readonly string[] Keys =
        {
            "il_woman_0", "il_woman_1", "il_woman_2", "il_woman_3",
            "il_man_0", "il_man_1", "il_man_2", "il_man_3",
            "il_child_0", "il_child_1", "il_child_2", "il_child_3",
            "il_greet_0", "il_greet_1", "il_greet_2", "il_greet_3",
            "il_cart", "il_pram", "il_post", "il_streak", "il_puff", "il_lamp", "il_stretcher", "il_ring"
        };

        private static readonly float[] Gaits = { 1f, 0.3f, -0.9f, 0.3f };

        internal static Texture2D Render(string key)
        {
            skyTone = Paper;
            lightDir = -1f;
            lightTone = Blend(Mustard, Paper, 0.35f);
            System.Random rng = new System.Random(19450120 + key.GetHashCode() % 977);

            if (key.StartsWith("il_woman_")) return Figure(rng, HungerWinterSceneFactory.Kind.Woman, HungerWinterSceneFactory.Carry.Push, 1, Gaits[Frame(key)], 300f);
            if (key.StartsWith("il_man_")) return Figure(rng, HungerWinterSceneFactory.Kind.Man, HungerWinterSceneFactory.Carry.Pull, 4, Gaits[Frame(key)], 318f);
            if (key.StartsWith("il_child_")) return Figure(rng, HungerWinterSceneFactory.Kind.Child, HungerWinterSceneFactory.Carry.Walk, 2, Gaits[Frame(key)], 190f);
            if (key.StartsWith("il_greet_")) return Figure(rng, HungerWinterSceneFactory.Kind.Woman, HungerWinterSceneFactory.Carry.Push, 2, Gaits[Frame(key)], 286f);
            switch (key)
            {
                case "il_cart": return CartSprite(rng);
                case "il_pram": return PramSprite(rng);
                case "il_post": return PostSprite(rng);
                case "il_streak": return Streak(160, 6);
                case "il_lamp": return Lamp(rng);
                case "il_stretcher": return Stretcher(rng);
                case "il_ring": return RingSprite(128, 9f);
                default: return Puff(56);
            }
        }

        private static int Frame(string key)
        {
            return key[key.Length - 1] - '0';
        }

        private static Texture2D Figure(System.Random rng, HungerWinterSceneFactory.Kind kind, HungerWinterSceneFactory.Carry carry, int variant, float gait, float h)
        {
            Board b = new Board(240, 360);
            HungerWinterSceneFactory.Person(b, rng, 120, 12, h, Soot, kind, carry, variant, gait);
            return ToTexture(b);
        }

        private static Texture2D CartSprite(System.Random rng)
        {
            Board b = new Board(480, 220);
            Cart(b, rng, 150, 8, 270f);
            return ToTexture(b);
        }

        private static Texture2D PramSprite(System.Random rng)
        {
            Board b = new Board(220, 140);
            HungerWinterSceneFactory.Pram(b, rng, 30, 8, 170f);
            return ToTexture(b);
        }

        private static Texture2D PostSprite(System.Random rng)
        {
            Board b = new Board(24, 96);
            HungerWinterSceneFactory.KilometrePost(b, rng, 12, 4, 86);
            return ToTexture(b);
        }

        /// <summary>Duvar lambası: eğik kol, kafes ve alttaki ampul yuvası; ampulün kendisi sahnede ışıkla çizilir.</summary>
        private static Texture2D Lamp(System.Random rng)
        {
            Board b = new Board(120, 150);
            Stroke(b, 4, 140, 60, 118, Soot, 6, 1f);
            Stroke(b, 4, 146, 4, 120, Soot, 8, 1f);
            // Kafes: dış çerçeve ve dikey teller.
            for (int y = 40; y <= 116; y++)
            {
                int half = 26 + (int)(6f * Mathf.Sin((y - 40) / 76f * Mathf.PI));
                b.Set(60 - half, y, Soot); b.Set(60 - half + 1, y, Soot);
                b.Set(60 + half, y, Soot); b.Set(60 + half - 1, y, Soot);
            }
            for (int x = 34; x <= 86; x++) { b.Set(x, 116, Soot); b.Set(x, 117, Soot); b.Set(x, 40, Soot); b.Set(x, 41, Soot); }
            for (int i = -2; i <= 2; i++) Stroke(b, 60 + i * 12, 42, 60 + i * 12, 115, Blend(Soot, Paper, 0.15f), 1, 0.9f);
            FillTextured(b, rng, 46, 108, 74, 122, Soot, 0.1f);
            FillTextured(b, rng, 52, 30, 68, 40, Soot, 0.1f);
            return ToTexture(b);
        }

        /// <summary>Sedye, yandan: iki sırık, sarkan bez ve üstünde yatan gövdenin kabartısı.</summary>
        private static Texture2D Stretcher(System.Random rng)
        {
            Board b = new Board(360, 110);
            FillTextured(b, rng, 6, 44, 354, 52, Ink, 0.15f);
            FillTextured(b, rng, 6, 34, 354, 40, Blend(Ink, Soot, 0.5f), 0.15f);
            for (int x = 40; x < 320; x++)
            {
                float t = (x - 40) / 280f;
                int sag = (int)(10f * Mathf.Sin(t * Mathf.PI));
                for (int y = 34 - sag; y < 44; y++) b.Set(x, y, Shift(Ink, rng.Next(-4, 5)));
                int body = (int)(30f * Mathf.Sin(t * Mathf.PI) * (0.55f + 0.45f * Mathf.Sin(t * Mathf.PI * 2.4f + 0.6f)));
                for (int y = 52; y < 52 + body; y++) b.Set(x, y, Shift(Soot, rng.Next(-4, 5)));
            }
            FillEllipse(b, 296, 66, 13, 12, Soot);
            for (int i = 0; i < 80; i++)
            {
                int x = rng.Next(44, 316);
                Stroke(b, x, 44, x + rng.Next(-3, 4), 52 + rng.Next(4, 22), Blend(Soot, Paper, 0.12f), 1, 0.25f);
            }
            return ToTexture(b);
        }

        /// <summary>Kenarı yumuşatılmış beyaz halka; zamanlama ve ilerleme göstergeleri için.</summary>
        private static Texture2D RingSprite(int size, float thickness)
        {
            Board b = new Board(size, size);
            float c = (size - 1) * 0.5f;
            float outer = size * 0.5f - 1.5f;
            float inner = outer - thickness;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(outer - d + 0.5f) * Mathf.Clamp01(d - inner + 0.5f);
                    b.Set(x, y, new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f)));
                }
            return ToTexture(b);
        }

        /// <summary>Kar ve bulut şeridi: iki ucu sönen yatay beyaz bant.</summary>
        private static Texture2D Streak(int width, int height)
        {
            Board b = new Board(width, height);
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                float a = Mathf.Sin(u * Mathf.PI);
                for (int y = 0; y < height; y++)
                {
                    float v = Mathf.Abs(y / (float)(height - 1) * 2f - 1f);
                    byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt(255f * a * (1f - v * v)), 0, 255);
                    b.Set(x, y, new Color32(255, 255, 255, alpha));
                }
            }
            return ToTexture(b);
        }

        /// <summary>Yumuşak yuvarlak leke: bulut kütlesi ve nefes buharı için.</summary>
        private static Texture2D Puff(int size)
        {
            Board b = new Board(size, size);
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01(1f - d);
                    b.Set(x, y, new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * a * a)));
                }
            return ToTexture(b);
        }

        private static Texture2D ToTexture(Board b)
        {
            Texture2D texture = new Texture2D(b.W, b.H, TextureFormat.RGBA32, false, false);
            texture.SetPixels32(b.Pixels);
            texture.Apply(false, false);
            return texture;
        }
    }
}
