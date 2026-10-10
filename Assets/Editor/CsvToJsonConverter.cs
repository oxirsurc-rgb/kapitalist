using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// CSV → JSON dönüştürücü.
/// Unity menüsünde: Tools → DemocracySim → CSV → JSON Dönüştür
/// 
/// CSV dosyalarını Assets/DataCsv/ altına koy, aracı çalıştır,
/// JSON'lar Assets/data/ altına otomatik yazılır.
/// 
/// Kullanım:
/// 1. Unity menüsünden Tools → DemocracySim → CSV → JSON Dönüştür
/// 2. Tüm CSV dosyaları işlenir
/// 3. Hata varsa Console'da kırmızı uyarı çıkar
/// </summary>
public class CsvToJsonConverter : EditorWindow
{
    private const string CsvFolder = "Assets/DataCsv";
    private const string JsonFolder = "Assets/data";

    [MenuItem("Tools/DemocracySim/CSV → JSON Dönüştür")]
    public static void ShowWindow()
    {
        GetWindow<CsvToJsonConverter>("CSV → JSON");
    }

    private void OnGUI()
    {
        GUILayout.Label("CSV → JSON Dönüştürücü", EditorStyles.boldLabel);
        GUILayout.Space(8);
        GUILayout.Label($"CSV klasörü: {CsvFolder}", EditorStyles.miniLabel);
        GUILayout.Label($"JSON klasörü: {JsonFolder}", EditorStyles.miniLabel);
        GUILayout.Space(12);

        if (GUILayout.Button("Tümünü Dönüştür", GUILayout.Height(40)))
        {
            ConvertAll();
        }

               GUILayout.Space(8);
        if (GUILayout.Button("objects.csv → objects.json")) ConvertObjects();
        if (GUILayout.Button("actors.csv → actors.json"))   ConvertActors();
        if (GUILayout.Button("effects.csv → effects.json")) ConvertEffects();
        if (GUILayout.Button("events.csv → events.json"))   ConvertEvents();
        if (GUILayout.Button("chains.csv → chains.json"))   ConvertChains();
        if (GUILayout.Button("chains_pack2.csv → chains_pack2.json"))   ConvertChainsPack2();
        if (GUILayout.Button("events_pack2.csv → events_pack2.json"))   ConvertEventsPack2();


        GUILayout.Space(12);
        if (GUILayout.Button("⚠️ Tüm CSV'leri Doğrula (K1 + Y7)", GUILayout.Height(35)))
        {
            ValidateAllCSVs();
        }
    }
    private static void ConvertEventsPack2()
{
    string csvPath = Path.Combine(CsvFolder, "events_pack2.csv");
    if (!File.Exists(csvPath)) { Debug.LogWarning($"[CSV] {csvPath} bulunamadı, atlanıyor."); return; }
    
    var allEvents = new List<Dictionary<string, object>>();
    
  
    
    var rows = ParseCsv(File.ReadAllText(csvPath));
    if (rows.Count < 2) return;
    
    var headers = rows[0];
    for (int i = 1; i < rows.Count; i++)
    {
        var row = rows[i];
        if (row.Count == 0 || string.IsNullOrWhiteSpace(GetField(row, headers, "Id"))) continue;
        
        var ev = new Dictionary<string, object>
        {
            ["Id"] = GetField(row, headers, "Id"),
            ["Title"] = GetField(row, headers, "Title"),
            ["Description"] = GetField(row, headers, "Description"),
            ["TriggerChance"] = ParseFloat(GetField(row, headers, "TriggerChance")),
            ["Scope"] = GetField(row, headers, "Scope"),
            ["Choice1Text"] = GetField(row, headers, "Choice1Text"),
            ["Choice1Effect"] = GetField(row, headers, "Choice1Effect"),
            ["Choice1Result"] = GetField(row, headers, "Choice1Result"),
            ["Choice2Text"] = GetField(row, headers, "Choice2Text"),
            ["Choice2Effect"] = GetField(row, headers, "Choice2Effect"),
            ["Choice2Result"] = GetField(row, headers, "Choice2Result"),
            ["Choice3Text"] = GetField(row, headers, "Choice3Text"),
            ["Choice3Effect"] = GetField(row, headers, "Choice3Effect"),
            ["Choice3Result"] = GetField(row, headers, "Choice3Result")
        };
        allEvents.Add(ev);
    }
    

    WriteJson(Path.Combine(JsonFolder, "events_pack2.json"), allEvents);
    Debug.Log($"[CSV→JSON] events_pack2.json: {allEvents.Count} olay yazıldı.");
}

private static void ConvertChainsPack2()
{
    string csvPath = Path.Combine(CsvFolder, "chains_pack2.csv");
    if (!File.Exists(csvPath)) { Debug.LogWarning($"[CSV] {csvPath} bulunamadı, atlanıyor."); return; }
    
    var rows = ParseCsv(File.ReadAllText(csvPath));
    if (rows.Count < 2) return;
    
    var headers = rows[0];
    var chains = new List<Dictionary<string, object>>();
    
    for (int i = 1; i < rows.Count; i++)
    {
        var row = rows[i];
        if (row.Count == 0 || string.IsNullOrWhiteSpace(GetField(row, headers, "FromEventId"))) continue;
        
        chains.Add(new Dictionary<string, object>
        {
            ["FromEventId"] = GetField(row, headers, "FromEventId"),
            ["ToEventId"] = GetField(row, headers, "ToEventId"),
            ["DelayTurns"] = (int)ParseFloat(GetField(row, headers, "DelayTurns"))
        });
    }
    
    WriteJson(Path.Combine(JsonFolder, "chains_pack2.json"), chains);
    Debug.Log($"[CSV→JSON] chains_pack2.json: {chains.Count} zincir yazıldı.");
}
            /// <summary>K1: Tüm CSV'lerde işaret ve tekrar hatalarını bul.</summary>
        private static void ValidateAllCSVs()
        {
            int errors = 0;
            int warnings = 0;

            // 1) Effects.csv kontrolü
            string effPath = Path.Combine(CsvFolder, "effects.csv");
            if (File.Exists(effPath))
            {
                var rows = ParseCsv(File.ReadAllText(effPath));
                var headers = rows.Count > 0 ? rows[0] : new List<string>();

                var seen = new HashSet<string>();
                for (int i = 1; i < rows.Count; i++)
                {
                    var row = rows[i];
                    string sourceId = GetField(row, headers, "SourceId");
                    string targetId = GetField(row, headers, "TargetId");
                    float strength = ParseFloat(GetField(row, headers, "Strength"));
                    string type = GetField(row, headers, "Type");

                    // Çift negatif kontrolü
                    if (strength < 0f)
                    {
                        Debug.LogWarning($"[K1] effects.csv satır {i + 1}: Negatif Strength → {sourceId} → {targetId} = {strength}");
                        warnings++;
                    }

                    // Çift kayıt kontrolü (Y7)
                    string key = $"{sourceId}->{targetId}";
                    if (!seen.Add(key))
                    {
                        Debug.LogError($"[Y7] effects.csv satır {i + 1}: Çift kayıt! {key}");
                        errors++;
                    }
                }
            }

            Debug.Log($"[CSV Doğrulama] {errors} hata, {warnings} uyarı bulundu.");
        }

    // =========================================================
    // ANA DÖNÜŞTÜRÜCÜ
    // =========================================================
    private static void ConvertAll()
    {
        try
        {
            EnsureFolders();
            ValidateAllCSVs();
            ConvertObjects();
            ConvertActors();
            ConvertEffects();
            ConvertEventsPack2();
            ConvertChainsPack2(); 
            ConvertEvents();
            ConvertChains();
            AssetDatabase.Refresh();
            Debug.Log("[CSV→JSON] ✅ Tüm dosyalar başarıyla dönüştürüldü.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CSV→JSON] ❌ Dönüştürme hatası: {e.Message}\n{e.StackTrace}");
        }
    }

    private static void EnsureFolders()
    {
        if (!Directory.Exists(CsvFolder)) Directory.CreateDirectory(CsvFolder);
        if (!Directory.Exists(JsonFolder)) Directory.CreateDirectory(JsonFolder);
    }

    // =========================================================
    // OBJECTS.CSV → OBJECTS.JSON
    // =========================================================
    // Sütunlar: Id,Name,Type,InitialValue,MinValue,MaxValue,IdeologicalAlignment,Frames,GroupImpacts
    // Frames format: "Halk İçin:-20|Sermaye İçin:+20"
    // GroupImpacts format: "workers:8|capitalists:-4"
    private static void ConvertObjects()
    {
        string csvPath = Path.Combine(CsvFolder, "objects.csv");
        if (!File.Exists(csvPath)) { Debug.LogWarning($"[CSV] {csvPath} bulunamadı, atlanıyor."); return; }

        var rows = ParseCsv(File.ReadAllText(csvPath));
        if (rows.Count < 2) { Debug.LogWarning("[CSV] objects.csv boş."); return; }

        var headers = rows[0];
        var objects = new List<Dictionary<string, object>>();
        var seenObjectIds = new HashSet<string>();
        

        for (int i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Count == 0 || string.IsNullOrWhiteSpace(GetField(row, headers, "Id"))) continue;
            if (!seenObjectIds.Add(GetField(row, headers, "Id"))) { Debug.LogError($"[Y7] objects.csv çift Id atlandı: {GetField(row, headers, "Id")}"); continue; }

            var obj = new Dictionary<string, object>
            {
                ["Id"] = GetField(row, headers, "Id"),
                ["Name"] = GetField(row, headers, "Name"),
                ["Type"] = GetField(row, headers, "Type"),
                ["InitialValue"] = ParseFloat(GetField(row, headers, "InitialValue")),
                ["MinValue"] = ParseFloat(GetField(row, headers, "MinValue")),
                ["MaxValue"] = ParseFloat(GetField(row, headers, "MaxValue")),
                ["IdeologicalAlignment"] = ParseFloat(GetField(row, headers, "IdeologicalAlignment")),
                ["BudgetType"] = GetField(row, headers, "BudgetType"),
            
            };

            string framesStr = GetField(row, headers, "Frames");
            if (!string.IsNullOrWhiteSpace(framesStr))
                obj["Frames"] = ParseKeyValuePairs(framesStr);

            string impactsStr = GetField(row, headers, "GroupImpacts");
            if (!string.IsNullOrWhiteSpace(impactsStr))
                obj["GroupImpacts"] = ParseKeyValuePairs(impactsStr);

            objects.Add(obj);
        }

        WriteJson(Path.Combine(JsonFolder, "objects.json"), objects);
        Debug.Log($"[CSV→JSON] objects.json: {objects.Count} nesne yazıldı.");
    }
               // =========================================================
        // CHAINS.CSV → CHAINS.JSON
        // =========================================================
        private static void ConvertChains()
        {
            string csvPath = Path.Combine(CsvFolder, "chains.csv");
            if (!File.Exists(csvPath)) { Debug.LogWarning($"[CSV] {csvPath} bulunamadi, atlaniyor."); return; }

            var rows = ParseCsv(File.ReadAllText(csvPath));
            if (rows.Count < 2) { Debug.LogWarning("[CSV] chains.csv bos."); return; }

            var headers = rows[0];
            var chains = new List<Dictionary<string, object>>();

            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count == 0 || string.IsNullOrWhiteSpace(GetField(row, headers, "FromEventId"))) continue;

                var chain = new Dictionary<string, object>
                {
                    ["FromEventId"] = GetField(row, headers, "FromEventId"),
                    ["ToEventId"] = GetField(row, headers, "ToEventId"),
                    ["DelayTurns"] = (int)ParseFloat(GetField(row, headers, "DelayTurns"))
                };
                chains.Add(chain);
            }

            WriteJson(Path.Combine(JsonFolder, "chains.json"), chains);
            Debug.Log($"[CSV→JSON] chains.json: {chains.Count} zincir yazildi.");
        }

    // =========================================================
    // ACTORS.CSV → ACTORS.JSON
    // =========================================================
    // Sütunlar: Id,Name,Role,Ideology,Ambition,Loyalty,Competence,Influence
    private static void ConvertActors()
    {
        string csvPath = Path.Combine(CsvFolder, "actors.csv");
        if (!File.Exists(csvPath)) { Debug.LogWarning($"[CSV] {csvPath} bulunamadı, atlanıyor."); return; }

        var rows = ParseCsv(File.ReadAllText(csvPath));
        if (rows.Count < 2) { Debug.LogWarning("[CSV] actors.csv boş."); return; }

        var headers = rows[0];
        var actors = new List<Dictionary<string, object>>();

        for (int i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Count == 0 || string.IsNullOrWhiteSpace(GetField(row, headers, "Id"))) continue;

            var actor = new Dictionary<string, object>
            {
                ["Id"] = GetField(row, headers, "Id"),
                ["Name"] = GetField(row, headers, "Name"),
                ["Role"] = GetField(row, headers, "Role"),
                ["Ideology"] = ParseFloat(GetField(row, headers, "Ideology")),
                ["Ambition"] = ParseFloat(GetField(row, headers, "Ambition")),
                ["Loyalty"] = ParseFloat(GetField(row, headers, "Loyalty")),
                ["Competence"] = ParseFloat(GetField(row, headers, "Competence")),
                ["Influence"] = ParseFloat(GetField(row, headers, "Influence"))
            };
            actors.Add(actor);
        }

        WriteJson(Path.Combine(JsonFolder, "actors.json"), actors);
        Debug.Log($"[CSV→JSON] actors.json: {actors.Count} aktör yazıldı.");
    }

    // =========================================================
    // EFFECTS.CSV → EFFECTS.JSON
    // =========================================================
    // Sütunlar: SourceId,TargetId,Strength,Type,Curve
    private static void ConvertEffects()
    {
        string csvPath = Path.Combine(CsvFolder, "effects.csv");
        if (!File.Exists(csvPath)) { Debug.LogWarning($"[CSV] {csvPath} bulunamadı, atlanıyor."); return; }

        var rows = ParseCsv(File.ReadAllText(csvPath));
        if (rows.Count < 2) { Debug.LogWarning("[CSV] effects.csv boş."); return; }

        var headers = rows[0];
        var effects = new List<Dictionary<string, object>>();
        var seenEffects = new HashSet<string>();
        var knownIds = LoadKnownObjectIds();

        for (int i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Count == 0 || string.IsNullOrWhiteSpace(GetField(row, headers, "SourceId"))) continue;

                     string sourceId = GetField(row, headers, "SourceId");
            string targetId = GetField(row, headers, "TargetId");
            float strength = ParseFloat(GetField(row, headers, "Strength"));
            string type = GetField(row, headers, "Type");
            string curve = string.IsNullOrWhiteSpace(GetField(row, headers, "Curve")) ? "Linear" : GetField(row, headers, "Curve");

            // K1 FIX: Yön yalnızca Type'tan gelir; Strength negatifse mutlak değer alınır, Type DEĞİŞMEZ.
            // (Type'ı çevirmek çift negatifin bozuk etkisini aynen korurdu.)
            if (strength < 0f)
            {
                strength = Mathf.Abs(strength);
                Debug.LogWarning($"[CSV FIX] {sourceId} → {targetId}: Negatif Strength mutlak değere çevrildi ({strength}), Type={type} korundu.");
            }

            // Y7: çift kayıt  |  Y8: bilinmeyen / tanımsız Id
            if (!seenEffects.Add($"{sourceId}->{targetId}"))
            { Debug.LogError($"[Y7] Çift kayıt atlandı: {sourceId} → {targetId}"); continue; }
            if (knownIds != null && (!knownIds.Contains(sourceId) || !knownIds.Contains(targetId)))
            { Debug.LogError($"[Y8] Tanımsız Id atlandı: {sourceId} → {targetId}"); continue; }

            if (string.IsNullOrEmpty(sourceId) || string.IsNullOrEmpty(targetId))
            {
                Debug.LogError($"[CSV HATA] Boş SourceId veya TargetId atlandı");
                continue;
            }

            var eff = new Dictionary<string, object>
            {
                ["SourceId"] = sourceId,
                ["TargetId"] = targetId,
                ["Strength"] = strength,
                ["Type"] = type,
                ["Curve"] = curve
            };
            effects.Add(eff);
        }

        WriteJson(Path.Combine(JsonFolder, "effects.json"), effects);
        Debug.Log($"[CSV→JSON] effects.json: {effects.Count} etki yazıldı.");
    }

    // =========================================================
    // EVENTS.CSV → events.json (opsiyonel, henüz kod desteklemiyor)
    // =========================================================
private static void ConvertEvents()
{
    string csvPath = Path.Combine(CsvFolder, "events.csv");
    if (!File.Exists(csvPath)) { Debug.LogWarning($"[CSV] {csvPath} bulunamadı, atlanıyor."); return; }

    var rows = ParseCsv(File.ReadAllText(csvPath));
    if (rows.Count < 2) { Debug.LogWarning("[CSV] events.csv boş."); return; }

    var headers = rows[0];
    var events = new List<Dictionary<string, object>>();

    for (int i = 1; i < rows.Count; i++)
    {
        var row = rows[i];
        if (row.Count == 0 || string.IsNullOrWhiteSpace(GetField(row, headers, "Id"))) continue;

                      var ev = new Dictionary<string, object>
                {
                    ["Id"] = GetField(row, headers, "Id"),
                    ["Title"] = GetField(row, headers, "Title"),
                    ["Description"] = GetField(row, headers, "Description"),
                    ["TriggerChance"] = ParseFloat(GetField(row, headers, "TriggerChance")),
                    ["Scope"] = GetField(row, headers, "Scope"),   // YENİ
                    ["Choice1Text"] = GetField(row, headers, "Choice1Text"),
                    ["Choice1Effect"] = GetField(row, headers, "Choice1Effect"),
                    ["Choice1Result"] = GetField(row, headers, "Choice1Result"),
                    ["Choice2Text"] = GetField(row, headers, "Choice2Text"),
                    ["Choice2Effect"] = GetField(row, headers, "Choice2Effect"),
                    ["Choice2Result"] = GetField(row, headers, "Choice2Result"),
                    ["Choice3Text"] = GetField(row, headers, "Choice3Text"),
                    ["Choice3Effect"] = GetField(row, headers, "Choice3Effect"),
                    ["Choice3Result"] = GetField(row, headers, "Choice3Result")
                };
        events.Add(ev);
    }

    WriteJson(Path.Combine(JsonFolder, "events.json"), events);
    Debug.Log($"[CSV→JSON] events.json: {events.Count} olay yazıldı.");
}

    // =========================================================
    // YARDIMCILAR
    // =========================================================

    /// <summary>objects.csv'deki Id kümesi (yoksa null → Id kontrolü yapılmaz).</summary>
    private static HashSet<string> LoadKnownObjectIds()
    {
        string p = Path.Combine(CsvFolder, "objects.csv");
        if (!File.Exists(p)) return null;
        var rows = ParseCsv(File.ReadAllText(p));
        if (rows.Count < 2) return null;
        var ids = new HashSet<string>();
        for (int i = 1; i < rows.Count; i++) ids.Add(GetField(rows[i], rows[0], "Id"));
        return ids;
    }

    /// <summary>Basit CSV parser. Tırnaklı alanları destekler.</summary>
    private static List<List<string>> ParseCsv(string content)
    {
        var rows = new List<List<string>>();
        var current = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < content.Length && content[i + 1] == '"') { field.Append('"'); i++; }
                    else inQuotes = false;
                }
                else field.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',') { current.Add(field.ToString()); field.Clear(); }
                else if (c == '\n')
                {
                    current.Add(field.ToString()); field.Clear();
                    if (current.Any(x => !string.IsNullOrWhiteSpace(x))) rows.Add(current);
                    current = new List<string>();
                }
                else if (c == '\r') { /* yoksay */ }
                else field.Append(c);
            }
        }
        if (field.Length > 0 || current.Count > 0)
        {
            current.Add(field.ToString());
            if (current.Any(x => !string.IsNullOrWhiteSpace(x))) rows.Add(current);
        }
        return rows;
    }

    private static string GetField(List<string> row, List<string> headers, string name)
    {
        int idx = headers.FindIndex(h => h.Trim().Equals(name, System.StringComparison.OrdinalIgnoreCase));
        if (idx < 0 || idx >= row.Count) return "";
        return row[idx].Trim();
    }

    private static float ParseFloat(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0f;
        s = s.Replace(',', '.');
        return float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : 0f;
    }

    private static Dictionary<string, float> ParseKeyValuePairs(string s)
    {
        var dict = new Dictionary<string, float>();
        if (string.IsNullOrWhiteSpace(s)) return dict;
        var pairs = s.Split('|');
        foreach (var pair in pairs)
        {
            var kv = pair.Split(':');
            if (kv.Length != 2) continue;
            dict[kv[0].Trim()] = ParseFloat(kv[1]);
        }
        return dict;
    }

    private static void WriteJson(string path, object data)
{
    string json = MiniJson.Serialize(data);
    File.WriteAllText(path, json);

    // K4 FIX: Aynı dosyayı StreamingAssets'e de yaz (build uyumu için)
    string fileName = Path.GetFileName(path);
    string streamingPath = Path.Combine("Assets/StreamingAssets/data", fileName);
    string streamingDir = Path.GetDirectoryName(streamingPath);
    if (!string.IsNullOrEmpty(streamingDir) && !Directory.Exists(streamingDir))
        Directory.CreateDirectory(streamingDir);
    File.WriteAllText(streamingPath, json);
}

    // =========================================================
    // MINI JSON SERIALIZER (Newtonsoft bağımlılığı olmadan)
    // =========================================================
    private static class MiniJson
    {
        public static string Serialize(object obj, int indent = 0)
        {
            if (obj == null) return "null";
            if (obj is string s) return "\"" + Escape(s) + "\"";
            if (obj is bool b) return b ? "true" : "false";
            if (obj is float f) return f.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (obj is double d) return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (obj is int i) return i.ToString();
            if (obj is long l) return l.ToString();

            if (obj is Dictionary<string, object> dict)
            {
                var sb = new StringBuilder();
                sb.Append("{");
                bool first = true;
                foreach (var kv in dict)
                {
                    if (!first) sb.Append(",");
                    first = false;
                    sb.Append("\"").Append(Escape(kv.Key)).Append("\":");
                    sb.Append(Serialize(kv.Value));
                }
                sb.Append("}");
                return sb.ToString();
            }

            if (obj is Dictionary<string, float> dictF)
            {
                var sb = new StringBuilder();
                sb.Append("{");
                bool first = true;
                foreach (var kv in dictF)
                {
                    if (!first) sb.Append(",");
                    first = false;
                    sb.Append("\"").Append(Escape(kv.Key)).Append("\":");
                    sb.Append(kv.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                sb.Append("}");
                return sb.ToString();
            }

            if (obj is System.Collections.IEnumerable list && !(obj is string))
            {
                var sb = new StringBuilder();
                sb.Append("[");
                bool first = true;
                foreach (var item in list)
                {
                    if (!first) sb.Append(",");
                    first = false;
                    sb.Append(Serialize(item));
                }
                sb.Append("]");
                return sb.ToString();
            }

            return "null";
        }

        private static string Escape(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
    }
}