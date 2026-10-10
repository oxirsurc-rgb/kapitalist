using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using DemocracySim.Engine.Core;

namespace DemocracySim.Engine.Data
{
    /// <summary>
    /// Atomik dosya yazma yardımcıları.
    /// 1) .tmp dosyasına yaz
    /// 2) File.Move ile hedefi değiştir
    /// 3) Yazma sırasında çökme olsa bile eski kayıt bozulmaz.
    /// 
    /// KULLANIM: SaveLoadManager.SaveGameInternal içinde
    ///   AtomicSave.WriteAllText(path, json);
    /// </summary>
    public static class AtomicSave
    {
        /// <summary>JSON'u atomik olarak diske yazar (senkron).</summary>
        public static void WriteAllText(string targetPath, string content)
        {
            if (string.IsNullOrEmpty(targetPath))
                throw new ArgumentException("targetPath boş olamaz", nameof(targetPath));

            string dir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string tmpPath = targetPath + ".tmp";
            string bakPath = targetPath + ".bak";

            try
            {
                // 1) Geçici dosyaya yaz (flush + close)
                using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var sw = new StreamWriter(fs, new System.Text.UTF8Encoding(false)))
                {
                    sw.Write(content);
                    sw.Flush();
                    fs.Flush(true);   // OS buffer'ını da boşalt
                }

                // 2) Eski kaydı yedekle (varsa)
                if (File.Exists(targetPath))
                {
                    if (File.Exists(bakPath)) File.Delete(bakPath);
                    File.Move(targetPath, bakPath);
                }

                // 3) Geçici dosyayı hedefe taşı
                File.Move(tmpPath, targetPath);
            }
            catch (Exception ex)
            {
                SimLogger.Log($"[AtomicSave] Yazma hatası: {ex.Message}", SimLogger.LogLevel.Error);
                // Temizlik: yarım kalan tmp dosyasını sil
                try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
                throw;
            }
        }

        /// <summary>Arka plan thread'inde atomik yazma (main thread'i bloklamaz).</summary>
        public static Task WriteAllTextAsync(string targetPath, string content)
        {
            return Task.Run(() =>
            {
                try { WriteAllText(targetPath, content); }
                catch (Exception ex)
                {
                    Debug.LogError($"[AtomicSave] Async yazma hatası: {ex.Message}");
                }
            });
        }

        /// <summary>Kayıt dosyasını oku; ana dosya bozuksa .bak'dan dener.</summary>
        public static string ReadAllTextWithFallback(string targetPath)
        {
            // 1) Ana dosyayı dene
            if (File.Exists(targetPath))
            {
                try
                {
                    return File.ReadAllText(targetPath);
                }
                catch (Exception ex)
                {
                    SimLogger.Log($"[AtomicSave] Ana kayıt okunamadı: {ex.Message}", SimLogger.LogLevel.Warning);
                }
            }

            // 2) .bak'dan dene
            string bakPath = targetPath + ".bak";
            if (File.Exists(bakPath))
            {
                try
                {
                    SimLogger.Log("[AtomicSave] Yedekten yükleniyor...", SimLogger.LogLevel.Warning);
                    return File.ReadAllText(bakPath);
                }
                catch (Exception ex)
                {
                    SimLogger.Log($"[AtomicSave] Yedek de okunamadı: {ex.Message}", SimLogger.LogLevel.Error);
                }
            }

            return null;
        }
    }
}