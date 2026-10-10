using System;
using System.Collections.Generic;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// Tüm SimObject'lere hızlı erişim için merkezi dizin.
    /// 
    /// Neden?
    /// - FirstOrDefault/Find: O(n) — her çağrıda liste taraması
    /// - Registry.Get: O(1) — sözlük erişimi
    /// - 37 OfType<SimPolicy> çağrısı yerine tek tipli liste
    /// 
    /// KULLANIM:
    ///   engine.Registry.Get("gdp")              // SimObject
    ///   engine.Registry.GetPolicy("tax_income") // SimPolicy
    ///   engine.Registry.GetValue("gdp", 50f)    // float (fallback ile)
    ///   engine.Registry.Policies                // IReadOnlyList<SimPolicy>
    ///   ObjectRegistry.Ids.Gdp                  // "gdp" sabiti
    /// </summary>
    public class ObjectRegistry
    {
        private readonly Dictionary<string, SimObject> _byId = new Dictionary<string, SimObject>(StringComparer.Ordinal);
        private readonly List<SimObject> _all = new List<SimObject>();
        private readonly List<SimPolicy> _policies = new List<SimPolicy>();
        private readonly List<SimStatistic> _statistics = new List<SimStatistic>();

        public IReadOnlyList<SimObject> All => _all;
        public IReadOnlyList<SimPolicy> Policies => _policies;
        public IReadOnlyList<SimStatistic> Statistics => _statistics;
        public int Count => _all.Count;

        /// <summary>Yeni bir nesne kaydet. Çift kayıt olursa atlar ve uyarır.</summary>
        public void Register(SimObject obj)
        {
            if (obj == null) return;
            if (string.IsNullOrEmpty(obj.Id))
            {
                SimLogger.Log($"[Registry] Boş Id'li nesne atlandı: {obj.Name}", SimLogger.LogLevel.Warning);
                return;
            }
            if (_byId.ContainsKey(obj.Id))
            {
                SimLogger.Log($"[Registry] Çift kayıt: {obj.Id} ({obj.Name}) — atlandı.", SimLogger.LogLevel.Warning);
                return;
            }

            _byId[obj.Id] = obj;
            _all.Add(obj);

            if (obj is SimPolicy p) _policies.Add(p);
            else if (obj is SimStatistic s) _statistics.Add(s);
        }

        /// <summary>Tüm dizini temizle (yeni oyun veya test için).</summary>
        public void Clear()
        {
            _byId.Clear();
            _all.Clear();
            _policies.Clear();
            _statistics.Clear();
        }

        /// <summary>Id ile SimObject getir. Yoksa null.</summary>
        public SimObject Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _byId.TryGetValue(id, out var o) ? o : null;
        }

        /// <summary>Id ile SimPolicy getir (yoksa null).</summary>
        public SimPolicy GetPolicy(string id) => Get(id) as SimPolicy;

        /// <summary>Id ile SimStatistic getir (yoksa null).</summary>
        public SimStatistic GetStat(string id) => Get(id) as SimStatistic;

        /// <summary>Id ile değer getir (yoksa fallback).</summary>
        public float GetValue(string id, float fallback = 0f)
            => _byId.TryGetValue(id, out var o) ? o.ActualValue : fallback;

        /// <summary>Id var mı?</summary>
        public bool Has(string id) => !string.IsNullOrEmpty(id) && _byId.ContainsKey(id);

        /// <summary>Sık kullanılan kimlikler — kodda "gdp" yazmak yerine ObjectRegistry.Ids.Gdp kullanın.</summary>
        public static class Ids
        {
            public const string Gdp = "gdp";
            public const string Unemployment = "unemployment";
            public const string Inflation = "inflation";
            public const string CrimeRate = "crime_rate";
            public const string EducationLevel = "education_level";
            public const string HealthcareQuality = "healthcare_quality";
            public const string EnvironmentQuality = "environment_quality";
            public const string Infrastructure = "infrastructure";
            public const string Inequality = "inequality";
            public const string PovertyRate = "poverty_rate";
            public const string Happiness = "happiness";
            public const string TechLevel = "tech_level";
            public const string MilitaryStrength = "military_strength";
            public const string EnergySecurity = "energy_security";
            public const string LifeExpectancy = "life_expectancy";
            public const string RndBudget = "rnd_budget";
            public const string EducationBudget = "edu_budget";
            public const string MilitaryBudget = "military_budget";
            public const string TaxIncome = "tax_income";
            public const string HealthcareBudget = "healthcare_budget";
            public const string InfrastructureBudget = "infrastructure_budget";
        }
    }
}