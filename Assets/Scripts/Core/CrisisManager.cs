using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core
{
    public class CrisisManager
    {
        public List<CrisisEvent> PotentialCrises { get; set; } = new List<CrisisEvent>();

        // YENİ: Motorda sadece "iskelet" olarak duran bu sistem artık gerçek
        // kriz örnekleriyle dolduruldu. SimulationEngine.ProcessTurn() içinde
        // her tur CheckForCrisis() çağrılıyor (bkz. SimulationEngine.cs).
        public CrisisManager()
        {
            PotentialCrises.Add(new CrisisEvent
            {
                Id = "fuel_crisis",
                Title = "YAKIT KRİZİ",
                Description = "Enflasyon kontrolden çıktı, akaryakıt fiyatları tavan yaptı! Halk sokaklarda.",
                TriggerCondition = (e) => e.Economy.Inflation > 15f,
                Options = new List<CrisisOption>
                {
                    new CrisisOption
                    {
                        Label = "Akaryakıta sübvansiyon uygula",
                        Effect = (e) => {
                            e.Economy.AdjustInflation(-5f);
                            e.Economy.AdjustDebt(200f);
                            e.Legitimacy.AdjustLegitimacy(8f);
                        },
                        ResultText = "Halk rahatladı ama devlet borcu büyüdü."
                    },
                    new CrisisOption
                    {
                        Label = "Piyasayı kendi haline bırak",
                        Effect = (e) => { e.Legitimacy.AdjustLegitimacy(-12f); },
                        ResultText = "Piyasalar zamanla dengelendi ama halk hükümete kızgın."
                    }
                }
            });

                       PotentialCrises.Add(new CrisisEvent
            {
                Id = "cabinet_crisis",
                Title = "KABİNEDE GÜVEN BUNALIMI",
                Description = "Bakanlar kurulu içinde ciddi bir anlaşmazlık patlak verdi, istifalar gündemde!",
                TriggerCondition = (e) => e.Actors.Count > 0 && e.Actors.Average(a => a.Satisfaction) < 30f,
                Options = new List<CrisisOption>
                {
                    new CrisisOption
                    {
                        Label = "Kabineyi yeniden yapılandır",
                        Effect = (e) => {
                            foreach (var a in e.Actors) a.Satisfaction = Math.Clamp(a.Satisfaction + 15f, 0f, 100f);
                            e.PoliticalCapital -= 15f;
                        },
                        ResultText = "Kabine toparlandı ama size pahalıya patladı."
                    },
                    new CrisisOption
                    {
                        Label = "Görmezden gel",
                        Effect = (e) => { foreach (var a in e.Actors) a.Loyalty -= 10f; },
                        ResultText = "Anlaşmazlık büyüdü, kabinede sadakat zedelendi."
                    }
                }
            });

            PotentialCrises.Add(new CrisisEvent
            {
                Id = "border_incident",
                Title = "SINIR OLAYI",
                Description = "Komşu ülkeyle sınırda gerginlik yaşandı, kamuoyu sert bir tepki bekliyor.",
                TriggerCondition = (e) => e.Army.ArmySatisfaction < 40f,
                Options = new List<CrisisOption>
                {
                    new CrisisOption
                    {
                        Label = "Diplomatik yollarla çöz",
                        Effect = (e) => { e.Legitimacy.AdjustLegitimacy(-5f); },
                        ResultText = "Gerginlik yatıştı ama bazı çevreler 'zayıf' olduğunuzu düşünüyor."
                    },
                    new CrisisOption
                    {
                        Label = "Askeri güç göster",
                        Effect = (e) => { e.Army.UpdateArmy(e, 75f); e.Legitimacy.AdjustLegitimacy(6f); },
                        ResultText = "Ordu memnun, halk gururlu ama uluslararası baskı arttı."
                    }
                }
            });
        }

        public CrisisEvent? CheckForCrisis(SimulationEngine engine)
        {
            // Koşulu sağlanan krizler her turda değil, %40 ihtimalle patlak verir (aksi halde her tur aynı kriz çıkardı).
            // Eski %10'luk "koşulsuz rastgele kriz" kaldırıldı: enflasyon %2 iken "enflasyon kontrolden çıktı" demesi anlamsızdı.
            foreach (var crisis in PotentialCrises)
            {
                if (crisis.TriggerCondition != null && crisis.TriggerCondition(engine) && SimRng.NextDouble() < 0.4)
                    return crisis;
            }
            return null;
        }
                /// <summary>FAZ 2.5: Kriz için rastgele bir bakan seç (isim belirtmek için).</summary>
        private PoliticalActor GetRandomMinister(SimulationEngine engine)
        {
            var ministers = engine.Actors.Where(a => a.Role == ActorRole.Minister).ToList();
            if (ministers.Count == 0) return null;
            return ministers[SimRng.Next(ministers.Count)];
        }
    }
}
