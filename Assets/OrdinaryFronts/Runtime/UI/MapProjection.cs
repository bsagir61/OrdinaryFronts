using UnityEngine;

namespace OrdinaryFronts
{
    /// <summary>
    /// Bölüm seçim haritasının projeksiyonu. Editörde haritayı çizen üretici ile çalışma
    /// anında işaretleri yerleştiren kod aynı sınıfı kullanır; iki taraf ayrı sabitlerle
    /// çalışsa Hamburg denizin ortasına düşerdi.
    /// <para>
    /// Basit eş-aralıklı projeksiyon: boylam, orta enlemin kosinüsüyle sıkıştırılır ki
    /// Avrupa yassılmasın. Dönem haritalarının çoğu da bu kadar iddiasızdır; sahne
    /// illüstrasyonlarına yakışan şey coğrafi hassasiyet değil, arşiv duvarındaki bir
    /// haritanın okunurluğudur.
    /// </para>
    /// </summary>
    public static class MapProjection
    {
        public const float LonMin = -11f;
        public const float LonMax = 42f;
        public const float LatMin = 35f;
        public const float LatMax = 65f;

        private static readonly float MidLatCos = Mathf.Cos((LatMin + LatMax) * 0.5f * Mathf.Deg2Rad);

        /// <summary>Haritanın doğal en/boy oranı (genişlik / yükseklik).</summary>
        public static float Aspect
        {
            get { return ((LonMax - LonMin) * MidLatCos) / (LatMax - LatMin); }
        }

        /// <summary>
        /// Enlem/boylamı harita dokusunun 0..1 aralığındaki (u, v) konumuna çevirir.
        /// v alttan yukarı ölçülür; Unity RectTransform çapalarıyla doğrudan kullanılır.
        /// </summary>
        public static Vector2 Project(float latitude, float longitude)
        {
            float u = (longitude - LonMin) / (LonMax - LonMin);
            float v = (latitude - LatMin) / (LatMax - LatMin);
            return new Vector2(u, v);
        }

        public static bool IsInside(float latitude, float longitude)
        {
            return latitude >= LatMin && latitude <= LatMax && longitude >= LonMin && longitude <= LonMax;
        }
    }
}
