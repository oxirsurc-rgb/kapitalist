using System;
using System.Collections.Generic;

namespace DemocracySim.Engine.Core
{
    public enum OperationType { Intel, Sabotage, Manipulate }

    public class IntelligenceManager
    {
        public float NetworkStrength { get; private set; } = 30f; // %0 - %100
        public float AgencyBudget { get; private set; } = 50f;
        public float TechLevel { get; private set; } = 10f; // Casusluk teknolojisi

        // Kayıt/Yükleme sistemi için: özel setter'lı alanları dışarıdan geri yükleyebilmek amacıyla
        public void LoadState(float networkStrength, float agencyBudget, float techLevel)
        {
            NetworkStrength = networkStrength;
            AgencyBudget = agencyBudget;
            TechLevel = techLevel;
        }

        public void UpdateAgency(float budget)
        {
            // Bütçe arttıkça ağ gücü ve teknoloji gelişir
            float growth = (budget - 50f) * 0.05f;
            NetworkStrength = Math.Clamp(NetworkStrength + growth, 0f, 100f);
            
            if (NetworkStrength > 80f) TechLevel += 0.1f;
            TechLevel = Math.Clamp(TechLevel, 0f, 100f);
        }

        // Operasyon Başarı Şansını Hesapla
        public float CalculateSuccessChance(OperationType type, float targetDefenses)
        {
            float baseChance = type switch
            {
                OperationType.Intel => 60f,
                OperationType.Sabotage => 40f,
                OperationType.Manipulate => 30f,
                _ => 50f
            };

            // Başarı = (Kendi Gücün - Hedefin Savunması) + Temel Şans
            return Math.Clamp(baseChance + (NetworkStrength - targetDefenses), 5f, 95f);
        }
    }
}
