using System;
using System.Collections.Generic;
using System.Linq;
using DemocracySim.Engine.Core;

namespace DemocracySim.Engine.Core
{
    public class CabinetManager
    {
        

        // Bildirimleri göndermek için bir callback kullanıyoruz
        public Action<string, bool> OnCabinetEvent;
                /// <summary>FAZ 2.5: Bakanı görevden al (15 sermaye maliyeti).</summary>
        public string DismissMinister(string actorId, SimulationEngine e)
        {
            var minister = e.Actors.FirstOrDefault(a => a.Id == actorId && a.Role == ActorRole.Minister);
            if (minister == null) return "Bakan bulunamadı.";

            float cost = 15f;
            if (e.PoliticalCapital < cost) return $"Yeterli sermaye yok ({cost:F0} gerekir).";

            e.PoliticalCapital -= cost;
            e.Actors.Remove(minister);

            // Diğer bakanlar tedirgin olur
            foreach (var a in e.Actors.Where(a => a.Role == ActorRole.Minister))
                a.Loyalty = Math.Max(0f, a.Loyalty - 5f);

            // Meşruiyet cezası
            e.Legitimacy.AdjustLegitimacy(-3f);

            return $"{minister.Name} görevden alındı. Diğer bakanlar tedirgin (-5 sadakat).";
        }

        /// <summary>FAZ 2.5: Sadakati düşen bakanlar istifa eder.</summary>
        public void ProcessResignations(SimulationEngine e)
        {
            var toResign = e.Actors
                .Where(a => a.Role == ActorRole.Minister && a.Loyalty < 20f)
                .ToList();

            foreach (var minister in toResign)
            {
                e.Actors.Remove(minister);
                e.Legitimacy.AdjustLegitimacy(-5f);
                SimLogger.Log($"[Kabine] {minister.Name} istifa etti! (Sadakat: %{minister.Loyalty:F0})", SimLogger.LogLevel.Warning);
            }
        }

        public void ProcessCabinetIntrigues(List<PoliticalActor> ministers, SimulationEngine engine)
        {
            if (ministers.Count < 2) return;

            if (SimRng.NextDouble() < 0.30)
            {
                var a = ministers[SimRng.Next(ministers.Count)];
                var b = ministers[SimRng.Next(ministers.Count)];
                if (a == b) return;

                if (Math.Abs(a.Ideology - b.Ideology) > 60f)
                {
                    a.Loyalty -= 1f;
                    b.Loyalty -= 1f;
                    engine.Legitimacy.AdjustLegitimacy(-0.5f);
                    // BİLDİRİM GÖNDER
                    OnCabinetEvent?.Invoke($"💥 KABİNE KRİZİ: {a.Name} ve {b.Name} arasındaki ideolojik tartışma hükümeti sarstı!", true);
                }
                else if (a.Ambition > 70f && b.Ambition > 70f)
                {
                    if (a.Influence > b.Influence) b.Loyalty -= 2f;
                    else a.Loyalty -= 2f;
                    // BİLDİRİM GÖNDER
                    OnCabinetEvent?.Invoke($"🤫 GÜÇ SAVAŞI: {a.Name} ve {b.Name} arasında gizli bir nüfuz mücadelesi var.", false);
                }
                else if (Math.Abs(a.Ideology - b.Ideology) < 20f)
                {
                    a.Loyalty += 0.5f;
                    b.Loyalty += 0.5f;
                    // BİLDİRİM GÖNDER
                    OnCabinetEvent?.Invoke($"🤝 İTTİFAK: {a.Name} ve {b.Name} yakınlaştı, ortak hareket ediyorlar.", false);
                }
            
            }
        }
        /// <summary>FAZ 3.5: Bakan istifa riski ve sızıntı.</summary>
public void ProcessResignationRisks(SimulationEngine e)
{
    foreach (var minister in e.Actors.Where(a => a.Role == ActorRole.Minister).ToList())
    {
        float resignChance = 0f;

        if (minister.Loyalty < 40f) resignChance += (40f - minister.Loyalty) * 0.02f;

        float ideoDiff = Math.Abs(minister.Ideology - e.PlayerGlobalAlignment);
        if (ideoDiff > 60f) resignChance += (ideoDiff - 60f) * 0.01f;

        if (e.Legitimacy.CurrentLegitimacy < 35f) resignChance += 0.05f;

        if (SimRng.NextDouble() < resignChance)
        {
            if (SimRng.NextDouble() < 0.5f)
            {
                e.Actors.Remove(minister);
                e.Legitimacy.AdjustLegitimacy(-5f);
                OnCabinetEvent?.Invoke($"📤 {minister.Name} istifa etti! (Sadakat: %{minister.Loyalty:F0})", true);
            }
            else
            {
                e.Legitimacy.AdjustLegitimacy(-8f);
                e.CorruptionLevel = Math.Min(100f, e.CorruptionLevel + 5f);
                OnCabinetEvent?.Invoke($"📰 {minister.Name} hakkında yolsuzluk sızıntısı! Meşruiyet -8.", true);
            }
        }
    }
}
    }
}
