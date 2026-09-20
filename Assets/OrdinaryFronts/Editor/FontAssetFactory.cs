using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace OrdinaryFronts.Editor
{
    /// <summary>
    /// Oyunun iki yazı tipi için TextMeshPro varlıklarını üretir.
    /// <para>
    /// Tipografi iki katmana ayrılır: <b>anlatılan</b> (PT Serif) ve <b>kayda geçen</b>
    /// (Courier Prime). Başlıklar, anlatı gövdesi ve final paragrafları serif; defter,
    /// final raporu, etiketler ve tarih/konum satırı daktilo yazı tipiyle çizilir. Ayrım
    /// oyunun kendi kurgusundan gelir: Milena bir defter tutar, Matthias'ın adı listelere
    /// yazılır; belge katmanının daktilo görünmesi bu yüzden yerindedir.
    /// </para>
    /// <para>
    /// Her iki aile de SIL Open Font License altındadır ve oyunla birlikte dağıtılabilir;
    /// lisans metinleri yazı tipi dosyalarının yanında durur. İkisi de Türkçenin tamamını
    /// (ş, ğ, ı, İ) ve karakter adlarındaki <c>ć</c> harfini kapsar, yani Türkçe metin artık
    /// yedek fonta düşmez. Ok işaretleri (← →) hiçbirinde yoktur; yedek zinciri onlar için
    /// korunur.
    /// </para>
    /// </summary>
    internal static class FontAssetFactory
    {
        internal const string FontRoot = "Assets/OrdinaryFronts/Art/Fonts";
        internal const string SerifAssetPath = FontRoot + "/OF Serif SDF.asset";
        internal const string MonoAssetPath = FontRoot + "/OF Mono SDF.asset";

        private const string SerifSource = FontRoot + "/PT_Serif-Web-Regular.ttf";
        private const string MonoSource = FontRoot + "/CourierPrime-Regular.ttf";
        private const string FallbackPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";

        internal static void EnsureAll()
        {
            EnsureFontAsset(SerifSource, SerifAssetPath, 90);
            EnsureFontAsset(MonoSource, MonoAssetPath, 90);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Kaynak yazı tipinden dinamik atlaslı bir TMP varlığı üretir. Dinamik mod, atlası
        /// çalışma anında gerektikçe doldurur; statik bir atlas hem büyük olur hem de sonradan
        /// eklenen bir dilin harflerini kaçırırdı.
        /// </summary>
        private static void EnsureFontAsset(string sourcePath, string assetPath, int samplingSize)
        {
            if (File.Exists(assetPath)) return;

            Font font = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (font == null)
            {
                Debug.LogWarning("Yazı tipi kaynağı bulunamadı, atlanıyor: " + sourcePath);
                return;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                font, samplingSize, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            if (fontAsset == null)
            {
                Debug.LogWarning("TMP yazı tipi varlığı üretilemedi: " + sourcePath);
                return;
            }

            fontAsset.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(fontAsset, assetPath);

            // Atlas dokusu ve materyal, varlığın alt nesneleri olarak saklanmazsa kaydedilmez
            // ve yazı tipi ilk yeniden derlemede boş çizer.
            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    if (fontAsset.atlasTextures[i] == null) continue;
                    fontAsset.atlasTextures[i].name = fontAsset.name + " Atlas";
                    AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[i], fontAsset);
                }
            }
            if (fontAsset.material != null)
            {
                fontAsset.material.name = fontAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            // Yedek zinciri: bu ailelerde bulunmayan işaretler (← →) buradan gelir.
            TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackPath);
            if (fallback != null)
            {
                fontAsset.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { fallback };
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            Debug.Log("ORDINARY_FRONTS_FONT_CREATED: " + assetPath);
        }
    }
}
