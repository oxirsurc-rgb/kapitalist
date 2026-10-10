using System;
using System.Collections.Generic;
using DemocracySim.Engine.Core;
using UnityEngine;

namespace DemocracySim.Engine.Data
{
    /// <summary>
    /// FAZ 5: Global başarım takibi. PlayerPrefs'te saklanır, oyunlar arası kalıcıdır.
    /// Her başarım ID'si bir bool olarak saklanır.
    /// </summary>
    public static class GlobalAchievementTracker
    {
        private const string Prefix = "Ach_";

        /// <summary>Bir başarım global olarak kazanıldı mı?</summary>
        public static bool IsUnlocked(string achievementId)
        {
            return PlayerPrefs.GetInt(Prefix + achievementId, 0) == 1;
        }

        /// <summary>Başarımı global olarak kaydet. Zaten kayıtlıysa false döner.</summary>
        public static bool Unlock(string achievementId)
        {
            if (IsUnlocked(achievementId)) return false;
            PlayerPrefs.SetInt(Prefix + achievementId, 1);
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>Kaç başarım kazanıldı (global).</summary>
        public static int CountUnlocked(IEnumerable<string> allIds)
        {
            int count = 0;
            foreach (var id in allIds)
                if (IsUnlocked(id)) count++;
            return count;
        }

        /// <summary>Tüm başarımları sıfırla (test için).</summary>
        public static void ResetAll(IEnumerable<string> allIds)
        {
            foreach (var id in allIds)
                PlayerPrefs.DeleteKey(Prefix + id);
            PlayerPrefs.Save();
        }

        /// <summary>Oyun içi AchievementManager'dan global'e senkronize et.</summary>
        public static void SyncFromEngine(AchievementManager engineAch)
        {
            if (engineAch == null) return;
            foreach (var id in engineAch.Unlocked)
            {
                if (Unlock(id))
                    Debug.Log($"[Global Ach] Yeni başarım global kaydedildi: {id}");
            }
        }

        /// <summary>Tüm başarımların global durumunu döner.</summary>
        public static Dictionary<string, bool> GetAllStates(IEnumerable<string> allIds)
        {
            var dict = new Dictionary<string, bool>();
            foreach (var id in allIds)
                dict[id] = IsUnlocked(id);
            return dict;
        }
    }
}