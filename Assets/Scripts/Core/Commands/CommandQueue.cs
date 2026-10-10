using System;
using System.Collections.Generic;
using System.Linq;

namespace DemocracySim.Engine.Core.Commands
{
    /// <summary>
    /// FAZ 23: Komut kuyruğu. Deterministik sıralama sağlar.
    /// </summary>
    public class CommandQueue
    {
        private readonly List<SimCommand> _commands = new List<SimCommand>();
        private int _processedUpTo = -1;

        public int Count => _commands.Count;
        public IReadOnlyList<SimCommand> All => _commands;

        /// <summary>Komut ekle (yeni).</summary>
        public void Enqueue(SimCommand cmd)
        {
            // Deterministik sıra: turn → country_id → type
            _commands.Add(cmd);
            _commands.Sort((a, b) =>
            {
                int cmp = a.Turn.CompareTo(b.Turn);
                if (cmp != 0) return cmp;
                cmp = string.Compare(a.CountryId, b.CountryId, StringComparison.Ordinal);
                if (cmp != 0) return cmp;
                return ((int)a.Type).CompareTo((int)b.Type);
            });
        }

        /// <summary>Belirli tur için komutları al.</summary>
        public List<SimCommand> GetForTurn(int turn)
        {
            return _commands.Where(c => c.Turn == turn).ToList();
        }

        /// <summary>Komutu işlendi olarak işaretle.</summary>
        public void MarkProcessed(int index)
        {
            if (index > _processedUpTo) _processedUpTo = index;
        }

        /// <summary>Tüm kuyruğu temizle.</summary>
        public void Clear()
        {
            _commands.Clear();
            _processedUpTo = -1;
        }

        /// <summary>Replay için kaydet.</summary>
        public string Serialize()
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(_commands, Newtonsoft.Json.Formatting.Indented);
            return json;
        }

        /// <summary>Replay'den yükle.</summary>
        public void Deserialize(string json)
        {
            _commands.Clear();
            var loaded = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SimCommand>>(json);
            if (loaded != null) _commands.AddRange(loaded);
        }

        /// <summary>Özet rapor.</summary>
        public string GetSummary()
        {
            var lines = new List<string>();
            lines.Add($"Toplam komut: {_commands.Count}");
            foreach (var g in _commands.GroupBy(c => c.Type))
                lines.Add($"  {g.Key}: {g.Count()}");
            return string.Join("\n", lines);
        }
    }
}