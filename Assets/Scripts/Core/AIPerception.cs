using System;
using System.Collections.Generic;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ 17: AI Perception — AI ülkesinin oyuncuyu "görmesi".
    /// Her AI ülkesi, oyuncunun son eylemlerini izler ve buna göre tepki verir.
    /// </summary>
    public class AIPerception
    {
        public string CountryId { get; set; }

        // Algılanan son olaylar (maksimum 10)
        private readonly Queue<PerceivedEvent> _events = new Queue<PerceivedEvent>();
        private const int MaxEvents = 10;

        // Oyuncu hakkında algılar
        public float PerceivedThreatLevel { get; private set; } = 0f;    // 0-100
        public float PerceivedAggression { get; private set; } = 0f;     // 0-100
        public string LastPerceivedAction { get; private set; } = "";

        public AIPerception(string countryId)
        {
            CountryId = countryId;
        }

        /// <summary>Bir olay algıla. Oyuncu bir şey yaptığında çağrılır.</summary>
        public void PerceiveEvent(PerceivedEvent evt)
        {
            _events.Enqueue(evt);
            while (_events.Count > MaxEvents)
                _events.Dequeue();

            LastPerceivedAction = evt.Description;

            // Tehdit seviyesini güncelle
            switch (evt.Type)
            {
                case PerceivedEventType.PlayerEspionage:
                    PerceivedThreatLevel += 15f;
                    break;
                case PerceivedEventType.PlayerSanction:
                    PerceivedThreatLevel += 25f;
                    break;
                case PerceivedEventType.PlayerTrade:
                    PerceivedThreatLevel -= 10f;
                    PerceivedAggression -= 5f;
                    break;
                case PerceivedEventType.PlayerMilitaryBuild:
                    PerceivedThreatLevel += 20f;
                    PerceivedAggression += 10f;
                    break;
                case PerceivedEventType.PlayerDiplomacy:
                    PerceivedThreatLevel -= 8f;
                    break;
            }

            PerceivedThreatLevel = Math.Clamp(PerceivedThreatLevel, 0f, 100f);
            PerceivedAggression = Math.Clamp(PerceivedAggression, 0f, 100f);
        }

        /// <summary>Her tur sönümlenme (event'ler eskir).</summary>
        // ✅ YENİ:
public void ProcessTurn(int currentTurn)
{
    PerceivedThreatLevel *= 0.9f;
    PerceivedAggression *= 0.95f;

    // 5 turdan eski olayları temizle
    while (_events.Count > 0 && currentTurn - _events.Peek().Turn > 5)
        _events.Dequeue();
}

        /// <summary>AI'ın bu oyuncuya karşı tutumu.</summary>
        public string GetAttitude()
        {
            if (PerceivedThreatLevel > 70f) return "HOSTILE";
            if (PerceivedThreatLevel > 40f) return "SUSPICIOUS";
            if (PerceivedThreatLevel > 20f) return "CAUTIOUS";
            return "NEUTRAL";
        }

        public List<string> GetReport()
        {
            return new List<string>
            {
                $"Algılanan Tehdit: %{PerceivedThreatLevel:F0}",
                $"Algılanan Saldırganlık: %{PerceivedAggression:F0}",
                $"Tutum: {GetAttitude()}",
                $"Son Eylem: {LastPerceivedAction}"
            };
        }
    }

    public enum PerceivedEventType
    {
        PlayerEspionage,
        PlayerSanction,
        PlayerTrade,
        PlayerMilitaryBuild,
        PlayerDiplomacy,
        Other
    }

    public class PerceivedEvent
    {
        public PerceivedEventType Type { get; set; }
        public string Description { get; set; }
        public int Turn { get; set; }
    }

    
}