using UnityEngine;

namespace OrdinaryFronts
{
    [CreateAssetMenu(menuName = "Ordinary Fronts/Brand Config", fileName = "BrandConfig")]
    public sealed class BrandConfig : ScriptableObject
    {
        [SerializeField] private string productName = "Ordinary Fronts";

        public string ProductName { get { return productName; } }

        public void ApplyCanonicalDefaults()
        {
            productName = "Ordinary Fronts";
        }
    }
}
