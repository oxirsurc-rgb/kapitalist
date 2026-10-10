using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using UnityEngine;
using DemocracySim.Engine.Core;
using DemocracySim.Engine.World;

namespace DemocracySim.Engine.Data
{
    public static class DataManager
    {
        private static JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            IncludeFields = true
        };

        // YOLU GÜNCELLEDİK: Artık doğrudan Assets/data klasörüne bakıyor
public static string DataRootPath { get; set; } = 
    Application.isEditor 
        ? Path.Combine(Application.dataPath, "data")
        : Path.Combine(Application.streamingAssetsPath, "data");
        public static List<CountryProfile> LoadCountryProfiles()
        {
            string path = Path.Combine(DataRootPath, "countries.json");
            try 
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<List<CountryProfile>>(json, _options);
                }
                SimLogger.Log($"HATA: {path} konumunda countries.json bulunamadı!", SimLogger.LogLevel.Error);
                return new List<CountryProfile>();
            }
            catch (Exception e)
            {
                SimLogger.Log($"JSON Yükleme Hatası: {e.Message}", SimLogger.LogLevel.Error);
                return new List<CountryProfile>();
            }
        }

        public static void AddDefaultDemographics(SimulationEngine engine, CountryProfile profile)
{
    if (engine == null || profile == null || engine.Demographics.Count > 0) return;
    
    // Mevcut 4 grup
    engine.Demographics.Add(new DemographicGroup("workers", "İşçi Sınıfı", profile.workerInfluence));
    engine.Demographics.Add(new DemographicGroup("capitalists", "Sermaye Sahipleri", profile.capitalistInfluence));
    engine.Demographics.Add(new DemographicGroup("intellectuals", "Entelektüeller", profile.intellectualInfluence));
    engine.Demographics.Add(new DemographicGroup("conservatives", "Muhafazakarlar", profile.conservativeInfluence));
    
    // FAZ 3: 4 yeni grup
    engine.Demographics.Add(new DemographicGroup("youth", "Gençler", 0.10f));
    engine.Demographics.Add(new DemographicGroup("rural", "Kırsal Kesim", 0.08f));
    engine.Demographics.Add(new DemographicGroup("retirees", "Emekliler", 0.07f));
    engine.Demographics.Add(new DemographicGroup("students", "Öğrenciler", 0.05f));
}
     public static List<EventData> LoadEvents()
{
    var allEvents = new List<EventData>();
    
    // Ana paket
    string path1 = Path.Combine(DataRootPath, "events.json");
    if (File.Exists(path1))
    {
        try 
        {
            var json = File.ReadAllText(path1);
            var events = JsonSerializer.Deserialize<List<EventData>>(json, _options);
            if (events != null) allEvents.AddRange(events);
        }
        catch (Exception e) { SimLogger.Log($"events.json hatası: {e.Message}", SimLogger.LogLevel.Error); }
    }
    
    // FAZ 4: Ek paket
    string path2 = Path.Combine(DataRootPath, "events_pack2.json");
    if (File.Exists(path2))
    {
        try 
        {
            var json = File.ReadAllText(path2);
            var events = JsonSerializer.Deserialize<List<EventData>>(json, _options);
            if (events != null) allEvents.AddRange(events);
        }
        catch (Exception e) { SimLogger.Log($"events_pack2.json hatası: {e.Message}", SimLogger.LogLevel.Error); }
    }
    
    SimLogger.Log($"[DataManager] Toplam {allEvents.Count} olay yüklendi.");
    return allEvents;
}
        // =====================================================
        // FAZ 2 Adım 4: Chains yükleyici
        // =====================================================
       public static List<ChainData> LoadChains()
{
    var allChains = new List<ChainData>();
    
    string path1 = Path.Combine(DataRootPath, "chains.json");
    if (File.Exists(path1))
    {
        try {
            var json = File.ReadAllText(path1);
            var chains = JsonSerializer.Deserialize<List<ChainData>>(json, _options);
            if (chains != null) allChains.AddRange(chains);
        } catch (Exception e) { SimLogger.Log($"chains.json hatası: {e.Message}", SimLogger.LogLevel.Error); }
    }
    
    // FAZ 4: Ek zincir paketi
    string path2 = Path.Combine(DataRootPath, "chains_pack2.json");
    if (File.Exists(path2))
    {
        try {
            var json = File.ReadAllText(path2);
            var chains = JsonSerializer.Deserialize<List<ChainData>>(json, _options);
            if (chains != null) allChains.AddRange(chains);
        } catch (Exception e) { SimLogger.Log($"chains_pack2.json hatası: {e.Message}", SimLogger.LogLevel.Error); }
    }
    
    SimLogger.Log($"[DataManager] Toplam {allChains.Count} zincir yüklendi.");
    return allChains;
}

       

        public class EventData
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public float TriggerChance { get; set; }
            public string Scope { get; set; }        // YENİ: "Government" | "Opposition" | "Both"
            public string Choice1Text { get; set; }
            public string Choice1Effect { get; set; }
            public string Choice1Result { get; set; }
            public string Choice2Text { get; set; }
            public string Choice2Effect { get; set; }
            public string Choice2Result { get; set; }
            public string Choice3Text { get; set; }
            public string Choice3Effect { get; set; }
            public string Choice3Result { get; set; }
        }
      
        public class ChainData
        {
            public string FromEventId { get; set; }
            public string ToEventId { get; set; }
            public int DelayTurns { get; set; }
        }

        public static void LoadWorld(SimulationEngine engine, string folderPath)
{
    // EK-27: Base + mod dosyalarını topla
    var objectPaths = ModManager.CollectActiveDataFiles("objects.json");
    var actorPaths = ModManager.CollectActiveDataFiles("actors.json");
    var effectPaths = ModManager.CollectActiveDataFiles("effects.json");

    // A. Objeleri Yükle (base + modlar, son mod kazanır)
    foreach (var path in objectPaths)
    {
        try
        {
            string json = File.ReadAllText(path);
            var objData = JsonSerializer.Deserialize<List<ObjectData>>(json, _options);
            if (objData == null) continue;
            foreach (var d in objData)
            {
                if (string.IsNullOrEmpty(d.Type)) continue;

                // Aynı ID varsa override et
                var existing = engine.Registry.Get(d.Id);
                if (existing != null) continue;   // Base zaten yükledi

                SimObject obj = d.Type == "Policy"
                    ? new SimPolicy(d.Id, d.Name, d.InitialValue)
                    : new SimStatistic(d.Id, d.Name, d.InitialValue);
                obj.MinValue = d.MinValue;
                obj.MaxValue = d.MaxValue;
                obj.IdeologicalAlignment = d.IdeologicalAlignment;
                obj.TargetValue = d.InitialValue;

                if (obj is SimPolicy policy)
                {
                    if (d.Frames != null) foreach (var frame in d.Frames) policy.Frames[frame.Key] = frame.Value;
                    if (d.GroupImpacts != null) policy.GroupImpacts = new Dictionary<string, float>(d.GroupImpacts);
                    if (!string.IsNullOrEmpty(d.BudgetType) &&
                        Enum.TryParse<PolicyBudgetType>(d.BudgetType, true, out var bt))
                        policy.BudgetType = bt;
                }
                engine.AddObject(obj);
            }
        }
        catch (Exception e) { SimLogger.Log($"[DataManager] {path} hatası: {e.Message}", SimLogger.LogLevel.Error); }
    }

    // B. Aktörleri Yükle
    foreach (var path in actorPaths)
    {
        try
        {
            string json = File.ReadAllText(path);
            var actorData = JsonSerializer.Deserialize<List<ActorData>>(json, _options);
            if (actorData == null) continue;
            foreach (var d in actorData)
            {
                if (string.IsNullOrEmpty(d.Role)) continue;
                if (!Enum.TryParse<ActorRole>(d.Role, true, out ActorRole role)) continue;
                if (engine.Actors.Any(a => a.Id == d.Id)) continue;   // Çift kayıt

                var actor = new PoliticalActor(d.Id, d.Name, role);
                actor.Ideology = d.Ideology;
                actor.Ambition = d.Ambition;
                actor.Loyalty = d.Loyalty;
                actor.Competence = d.Competence;
                actor.Influence = d.Influence;
                engine.AddActor(actor);
            }
        }
        catch (Exception e) { SimLogger.Log($"[DataManager] {path} hatası: {e.Message}", SimLogger.LogLevel.Error); }
    }

    // C. Etkileri Yükle
    foreach (var path in effectPaths)
    {
        try
        {
            string json = File.ReadAllText(path);
            var effData = JsonSerializer.Deserialize<List<EffectData>>(json, _options);
            if (effData == null) continue;
            foreach (var d in effData)
            {
                if (string.IsNullOrEmpty(d.Curve)) continue;
                if (!Enum.TryParse<CurveType>(d.Curve, true, out CurveType curve)) continue;

                engine.AddEffect(d.SourceId, d.TargetId, d.Strength,
                    d.Type == "Positive" ? EffectType.Positive : EffectType.Negative,
                    curve);
            }
        }
        catch (Exception e) { SimLogger.Log($"[DataManager] {path} hatası: {e.Message}", SimLogger.LogLevel.Error); }
    }
}

        public class ObjectData
        {
            public string Id, Name, Type;
            public float InitialValue, MinValue, MaxValue, IdeologicalAlignment;
            public Dictionary<string, float> Frames;
            public Dictionary<string, float> GroupImpacts;
            public string BudgetType;
        }        public class ActorData { public string Id, Name, Role; public float Ideology, Ambition, Loyalty, Competence, Influence; }
        public class EffectData { public string SourceId, TargetId, Type, Curve; public float Strength; }
    }
}
