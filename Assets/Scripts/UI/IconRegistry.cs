using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DemocracySim.Engine.UI
{
    public static class IconRegistry
    {
        private static Dictionary<string, string> _iconMap = new Dictionary<string, string>();
        private static string IconFolderPath = "Assets/Textures/UI/ArchitectIcons/Pas7Studio_Architect Icons Pack_v1_0_0";

        static IconRegistry()
        {
            InitializeMap();
        }

        private static void InitializeMap()
        {
            // Sektörlerle ikon isimlerini eşleştiriyoruz
            // Not: Gerçek dosya isimleri klasördeki isimlerle birebir aynı olmalı
            _iconMap = new Dictionary<string, string>
            {
                { "Economy", "economy_icon" },
                { "Health", "health_icon" },
                { "Defense", "defense_icon" },
                { "Education", "education_icon" },
                { "Justice", "justice_icon" },
                { "ForeignAffairs", "world_icon" },
                { "Interior", "government_icon" },
                { "Energy", "energy_icon" },
                { "Agriculture", "farm_icon" },
                { "Industry", "industry_icon" },
                { "Technology", "tech_icon" },
                { "Environment", "nature_icon" }
            };
        }

        public static UnityEngine.Object GetIcon(string sector)
        {
            if (string.IsNullOrEmpty(sector)) return null;
            
            if (_iconMap.TryGetValue(sector, out string fileName))
            {
                // Unity Resources veya Assets içinden yükleme
                // Basitlik için Assets path üzerinden yükleme denemesi
                string fullPath = $"{IconFolderPath}/{fileName}";
                // Not: Runtime'da Assets path çalışmaz, ancak Editor'de veya 
                // Resources klasörüne taşındığında çalışır. 
                // Şimdilik referans dönüyoruz.
                return Resources.Load<UnityEngine.Texture2D>($"UI/Icons/{fileName}");
            }
            return null;
        }

        public static string GetIconPath(string sector)
        {
            return _iconMap.TryGetValue(sector, out string fileName) ? fileName : "default_icon";
        }
    }
}
