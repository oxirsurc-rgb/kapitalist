using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Legislative
{
    /// <summary>
    /// FAZ 2: Siyasi parti — mecliste sandalyesi olan bir aktör.
    /// </summary>
    [Serializable]
    public class Party
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public float Ideology { get; set; }              // -100 (sol) .. +100 (sağ)
        public int Seats { get; set; }                    // Meclis sandalyesi
        public List<string> MemberIds { get; set; } = new List<string>();
        public bool IsInGovernment { get; set; } = false;
        public float Discipline { get; set; } = 0.85f;   // Parti disiplini (0-1)
        public float Satisfaction { get; set; } = 50f;   // İktidardan memnuniyet

        public Party() { }

        public Party(string id, string name, float ideology, int seats)
        {
            Id = id; Name = name; Ideology = ideology; Seats = seats;
        }

        /// <summary>Parti sol/sağ/merkez etiketini döndürür.</summary>
        public string SideLabel()
        {
            if (Ideology < -40f) return "Sol";
            if (Ideology < -10f) return "Merkez-Sol";
            if (Ideology > 40f) return "Sağ";
            if (Ideology > 10f) return "Merkez-Sağ";
            return "Merkez";
        }

        /// <summary>Parti rengi (UI için HTML hex).</summary>
        public string ColorHex()
        {
            return Id switch
            {
                "workers"      => "#E5534B",   // Kırmızı
                "conservative" => "#4C9BE8",   // Mavi
                "nationalist"  => "#8B2A2A",   // Koyu kırmızı
                "liberal"      => "#F2B84B",   // Sarı
                "green"        => "#4CC38A",   // Yeşil
                "player"       => "#F2B84B",   // Oyuncu partisi — altın
                _              => "#93A1BA"
            };
        }
    }
}