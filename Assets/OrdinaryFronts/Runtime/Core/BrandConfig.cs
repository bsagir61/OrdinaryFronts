using UnityEngine;

namespace OrdinaryFronts
{
    [CreateAssetMenu(menuName = "Ordinary Fronts/Brand Config", fileName = "BrandConfig")]
    public sealed class BrandConfig : ScriptableObject
    {
        [SerializeField] private string productName = "Ordinary Fronts";
        [SerializeField] private string applicationName = "Ordinary Fronts Demo";
        [SerializeField] private string companyName = "Berat Sağır";
        [SerializeField] private string version = "1.1.1";

        /// <summary>Oyun içinde gösterilen ad. Yalnız ana menüde görünür.</summary>
        public string ProductName { get { return productName; } }

        /// <summary>
        /// İşletim sistemine ve mağazaya giden ad. Demo, tam sürümden ayrı bir uygulama
        /// olarak yayınlandığı için buradaki "Demo" eki kasıtlıdır: pencere başlığını ve
        /// kayıt klasörünü ayırır, böylece demo kayıtları ileride tam sürümünkiyle çakışmaz.
        /// </summary>
        public string ApplicationName { get { return applicationName; } }

        /// <summary>
        /// Kayıt yolunun bir parçasıdır (<c>AppData/LocalLow/&lt;şirket&gt;/&lt;uygulama&gt;</c>)
        /// ve exe dosya özelliklerinde görünür. Yayından sonra değiştirilirse mevcut
        /// oyuncuların kayıtları erişilemez hâle gelir.
        /// </summary>
        public string CompanyName { get { return companyName; } }

        public string Version { get { return version; } }

        public void ApplyCanonicalDefaults()
        {
            productName = "Ordinary Fronts";
            applicationName = "Ordinary Fronts Demo";
            companyName = "Berat Sağır";
            version = "1.1.1";
        }
    }
}
