using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityEngine;

namespace DemocracySim.Engine.Data
{
    /// <summary>
    /// EK-27: Mod sistemi.
    /// Her mod, kendi klasöründe mod.json + veri dosyalarıyla gelir.
    /// Base oyun dosyaları ile birleştirilir; base her zaman öncelikli değildir — mod override edebilir.
    /// </summary>
    public static class ModManager
    {
        public class ModInfo
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Version { get; set; }
            public string Author { get; set; }
            public string Description { get; set; }
            public string GameVersion { get; set; }
            public string FolderPath { get; set; }
            public bool Enabled { get; set; } = true;
        }

        public static List<ModInfo> LoadedMods { get; private set; } = new List<ModInfo>();

        private static string ModsDir => Path.Combine(Application.streamingAssetsPath, "mods");

        /// <summary>Başlangıçta çağrılır: tüm modları tarar ve yükler.</summary>
        public static void DiscoverMods()
        {
            LoadedMods.Clear();

            if (!Directory.Exists(ModsDir))
            {
                Directory.CreateDirectory(ModsDir);
                Debug.Log($"[Mod] Mods klasörü oluşturuldu: {ModsDir}");
                return;
            }

            foreach (var dir in Directory.GetDirectories(ModsDir))
            {
                string modJsonPath = Path.Combine(dir, "mod.json");
                if (!File.Exists(modJsonPath)) continue;

                try
                {
                    var info = JsonSerializer.Deserialize<ModInfo>(File.ReadAllText(modJsonPath));
                    if (info == null || string.IsNullOrEmpty(info.Id)) continue;

                    info.FolderPath = dir;
                    info.Enabled = PlayerPrefs.GetInt($"mod_{info.Id}_enabled", 1) == 1;
                    LoadedMods.Add(info);

                    Debug.Log($"[Mod] Bulundu: {info.Name} v{info.Version} ({info.Id}) — {(info.Enabled ? "aktif" : "kapalı")}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Mod] {dir} okunamadı: {e.Message}");
                }
            }

            Debug.Log($"[Mod] Toplam {LoadedMods.Count} mod bulundu.");
        }

        /// <summary>Modu etkinleştir/devre dışı bırak.</summary>
        public static void SetModEnabled(string modId, bool enabled)
        {
            var mod = LoadedMods.FirstOrDefault(m => m.Id == modId);
            if (mod == null) return;
            mod.Enabled = enabled;
            PlayerPrefs.SetInt($"mod_{modId}_enabled", enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Bir moddaki veri dosyasının yolu.</summary>
        public static string GetModDataPath(string modId, string dataFileName)
        {
            var mod = LoadedMods.FirstOrDefault(m => m.Id == modId);
            if (mod == null) return null;
            string path = Path.Combine(mod.FolderPath, dataFileName);
            return File.Exists(path) ? path : null;
        }

        /// <summary>Aktif modlar için veri dosyalarını topla (base + modlar).</summary>
        public static List<string> CollectActiveDataFiles(string dataFileName)
        {
            var paths = new List<string>();

            // 1) Base oyun dosyası
            string basePath = Path.Combine(DataManager.DataRootPath, dataFileName);
            if (File.Exists(basePath)) paths.Add(basePath);

            // 2) Aktif modlar
            foreach (var mod in LoadedMods.Where(m => m.Enabled))
            {
                string modPath = Path.Combine(mod.FolderPath, dataFileName);
                if (File.Exists(modPath)) paths.Add(modPath);
            }

            return paths;
        }

        /// <summary>Rapor: yüklü modların özeti.</summary>
        public static List<string> GetReport()
        {
            var lines = new List<string>();
            lines.Add($"=== YÜKLÜ MODLAR ({LoadedMods.Count}) ===");
            foreach (var m in LoadedMods)
            {
                string status = m.Enabled ? "[AKTIF]" : "[KAPALI]";
                lines.Add($"{status} {m.Name} v{m.Version} — {m.Author}");
                lines.Add($"    {m.Description}");
            }
            if (LoadedMods.Count == 0)
                lines.Add("(Mod yok)");
            return lines;
        }
    }
}