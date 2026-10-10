using System;
using System.Linq;
using System.Collections.Generic;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ Geliştirme (Democracy 4 & Suzerain İlhamı):
    /// 1. Kamuoyu Anket ve Hata Payı Sistemi (Polling System with Margin of Error)
    /// 2. Ekonomik Doktrin Tutarlılığı ve Kredi Derecelendirme Sistemi (Fiscal Doctrine & Credit Rating)
    /// </summary>
    public static class PollingAndDoctrine
    {
        public struct PollResult
        {
            public float GoverningSupport;
            public float OppositionSupport;
            public float Undecided;
            public float MarginOfError;
            public string Summary;
        }

        /// <summary>
        /// Gerçek seçmen memnuniyetinden anket sonucu ve hata payı üretir.
        /// Medya etkisi ve huzursuzluk arttıkça hata payı (volatilite) artar.
        /// </summary>
        public static PollResult ConductPoll(SimulationEngine e)
        {
            float trueApproval = e.Legitimacy.CurrentLegitimacy;
            float unrest = e.Universe.Unrest;

            // Hata payı: taban %2.5 + huzursuzluk/medya sapması
            float margin = 2.5f + (unrest * 0.05f);
            margin = Math.Clamp(margin, 2.0f, 6.0f);

            // Rastgele örneklem gürültüsü
            float noise = (float)((SimRng.NextDouble() * 2.0 - 1.0) * (margin * 0.7f));
            float polledGoverning = Math.Clamp(trueApproval + noise, 5f, 95f);

            // Muhalefet ve kararsızlar
            float remaining = 100f - polledGoverning;
            float undecided = Math.Clamp(remaining * (0.15f + (unrest * 0.002f)), 4f, 25f);
            float polledOpposition = Math.Max(0f, remaining - undecided);

            string summary = $"[ANKET] Kamuoyu Yoklaması: İktidar %{polledGoverning:F1} (±%{margin:F1}) | Muhalefet %{polledOpposition:F1} | Kararsız %{undecided:F1}";

            return new PollResult
            {
                GoverningSupport = polledGoverning,
                OppositionSupport = polledOpposition,
                Undecided = undecided,
                MarginOfError = margin,
                Summary = summary
            };
        }

        /// <summary>
        /// Ekonomik politikaların tutarlılığını ve borç baskısını değerlendirir (Suzerain döngüsü).
        /// </summary>
        public static void EvaluateFiscalDiscipline(SimulationEngine e)
        {
            float debt = e.Economy.NationalDebt;
            var gdpObj = e.Registry.Get(ObjectRegistry.Ids.Gdp);
            float gdp = gdpObj != null ? gdpObj.ActualValue : 50f;

            // Borç/GSYİH baskı oranı
            float debtPressure = debt / Math.Max(10f, gdp * 50f);

            if (debtPressure > 1.2f && e.CurrentTurn % 4 == 0)
            {
                e.Legitimacy.AdjustLegitimacy(-2f);
                e.Universe.Messages.Add(new UniverseMessage
                {
                    Text = $"[MALİ BASKI] BORÇ UYARISI: Yüksek borç yükü (oran: %{debtPressure * 100:F0}) kredi notunu zorluyor. Uluslararası faiz maliyeti arttı!",
                    IsWarning = true
                });
            }
        }
    }

    /// <summary>Öncelik 85: Kamuoyu yoklaması (Seçim öncesi trend takibi).</summary>
    public class PollingSystem : ITurnSystem
    {
        public string SystemName => "PollingSystem";
        public int Priority => 85;

        public void ProcessTurn(SimulationEngine e)
        {
            // Sadece oyuncunun ülkesinde ve her 3 turda bir veya seçim yaklaştığında anket yayınla
            if (!e.IsPlayerCountry) return;

            if (e.CurrentTurn % 3 == 0 || e.TurnUntilElection <= 3)
            {
                var poll = PollingAndDoctrine.ConductPoll(e);
                e.Universe.Messages.Add(new UniverseMessage
                {
                    Text = poll.Summary,
                    IsWarning = false
                });
            }

            // Mali disiplin ve borç kontrolü
            PollingAndDoctrine.EvaluateFiscalDiscipline(e);
        }
    }
}
