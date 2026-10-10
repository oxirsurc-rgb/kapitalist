using System;
using System.Collections.Generic;
using DemocracySim.Engine.Core;

namespace DemocracySim.Engine.World
{
    public class DiplomacyManager
    {
        public Dictionary<string, float> Relations { get; set; } = new Dictionary<string, float>();

        public void SetRelation(string id1, string id2, float value)
        {
            string key = GetKey(id1, id2);
            Relations[key] = value;
        }

        public float GetRelation(string id1, string id2)
        {
            string key = GetKey(id1, id2);
            return Relations.ContainsKey(key) ? Relations[key] : 0f;
        }

        private string GetKey(string id1, string id2)
        {
            return string.Compare(id1, id2) < 0 ? $"{id1}_{id2}" : $"{id2}_{id1}";
        }

        // --- YENİ: AKTİF DİPLOMATİK AKSİYONLAR ---

        public void ImproveRelation(string id1, string id2, float amount)
        {
            float current = GetRelation(id1, id2);
            SetRelation(id1, id2, Math.Clamp(current + amount, -100f, 100f));
        }

        public void DamageRelation(string id1, string id2, float amount)
        {
            float current = GetRelation(id1, id2);
            SetRelation(id1, id2, Math.Clamp(current - amount, -100f, 100f));
        }

        public void UpdateGlobalRelations(WorldManager world)
        {
            foreach (var c1 in world.Countries)
            {
                foreach (var c2 in world.Countries)
                {
                    if (c1 == c2) continue;
                    float diff = Math.Abs(c1.GlobalAlignment - c2.GlobalAlignment);
                    if (diff < 20f) ImproveRelation(c1.Id, c2.Id, 0.1f);
                }
            }
        }
    }
}
