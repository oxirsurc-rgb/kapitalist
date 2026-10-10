using System;
using DemocracySim.Engine.Core;
using System.Collections.Generic;

namespace DemocracySim.Engine.World
{
    public class Country
    {
        public List<string> RecentActions { get; set; } = new List<string>();
        public DiplomaticMemory Memory { get; set; } = new DiplomaticMemory();
        public string Id { get; set; }
        public string Name { get; set; }
        public SimulationEngine Engine { get; set; }
        public float GlobalAlignment { get; set; } // -100 (Batı) ile +100 (Doğu) arası
        public bool IsPlayerControlled { get; set; }
        public string Continent { get; set; } = "";   // FAZ 4: bulaşma ve göç komşuluğu için

        public Country(string id, string name, bool isPlayer)
        {
            Id = id;
            Name = name;
            IsPlayerControlled = isPlayer;
            Engine = new SimulationEngine();
            Engine.OwnerCountryId = id; // YENİ: motor artık kendi ülkesinin kimliğini biliyor
            // Burada engine için temel kurulumlar (AllObjects yükleme vb.) yapılacak
        }
    
    public void RecordAction(string action)
        {
            if (string.IsNullOrEmpty(action)) return;
            RecentActions.Insert(0, action);   // En yeni başa
            while (RecentActions.Count > 5) RecentActions.RemoveAt(5);
        }
    }
}