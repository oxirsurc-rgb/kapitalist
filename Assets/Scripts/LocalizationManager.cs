using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UnityEngine;

public enum Language { Turkish = 0, English = 1, German = 2, Chinese = 3, Portuguese = 4 }

/// <summary>
/// FAZ 8: JSON tabanlı çoklu dil sistemi.
/// Dil dosyaları Assets/StreamingAssets/data/localization/ altında.
/// Runtime'da dil değiştirilebilir.
/// </summary>
public static class LocalizationManager
{
    public static Language CurrentLanguage { get; private set; } = Language.Turkish;

    // EK-26: Runtime'da değişince tüm UI güncellensin
    public static event Action OnLanguageChanged;

    private static readonly Dictionary<string, string> _current = new Dictionary<string, string>();
    private static readonly Language[] AllLanguages = { Language.Turkish, Language.English,
        Language.German, Language.Chinese, Language.Portuguese };

    private static string[] LanguageCodes = { "tr", "en", "de", "zh", "pt" };

    private static string LocalizationDir =>
        Path.Combine(Application.streamingAssetsPath, "data", "localization");

    /// <summary>Kayıtlı dili yükle (başlangıçta çağrılır).</summary>
    public static void LoadSavedLanguage()
    {
        // Oyun her zaman English başlasın
        SetLanguage(Language.English);
    }

    /// <summary>Dili değiştir + dosyayı yükle + event yayınla.</summary>
    public static void SetLanguage(Language lang)
    {
        CurrentLanguage = lang;
        PlayerPrefs.SetInt("Language", (int)lang);
        PlayerPrefs.Save();
        LoadLanguageFile(lang);
        OnLanguageChanged?.Invoke();
    }

    /// <summary>JSON dosyasını yükle.</summary>
    private static void LoadLanguageFile(Language lang)
    {
        _current.Clear();
        string code = LanguageCodes[(int)lang];
        string path = Path.Combine(LocalizationDir, $"{code}.json");

        // Eğer StreamingAssets'te yoksa Resources fallback
        if (!File.Exists(path))
        {
            var asset = Resources.Load<TextAsset>($"localization/{code}");
            if (asset != null)
            {
                ParseAndLoad(asset.text);
                return;
            }
            Debug.LogWarning($"[Loc] Dil dosyası bulunamadı: {path}");
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            ParseAndLoad(json);
            Debug.Log($"[Loc] {lang} yüklendi ({_current.Count} anahtar).");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Loc] Yükleme hatası: {e.Message}");
        }
    }

    private static void ParseAndLoad(string json)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (dict == null) return;
            foreach (var kv in dict)
                _current[kv.Key] = kv.Value;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Loc] Parse hatası: {e.Message}");
        }
    }

    /// <summary>Çeviriyi döndür. Yoksa anahtarı döndürür.</summary>
    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        return _current.TryGetValue(key, out var value) ? value : key;
    }

    /// <summary>Format'lı çeviri.</summary>
    public static string Get(string key, params object[] args)
    {
        string template = Get(key);
        try { return string.Format(template, args); }
        catch { return template; }
    }

    public static bool Has(string key) => _current.ContainsKey(key);

    /// <summary>Eksik çeviri kontrolü — geliştirme için.</summary>
    public static List<string> ValidateAllLanguages()
    {
        var missing = new List<string>();
        string basePath = Path.Combine(LocalizationDir, "tr.json");
        if (!File.Exists(basePath)) return missing;

        var trKeys = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(basePath)).Keys;

        foreach (var lang in AllLanguages)
        {
            if (lang == Language.Turkish) continue;
            string path = Path.Combine(LocalizationDir, $"{LanguageCodes[(int)lang]}.json");
            if (!File.Exists(path))
            {
                missing.Add($"{lang}: Dosya yok");
                continue;
            }

            var keys = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)).Keys;
            var missingKeys = new HashSet<string>(trKeys);
            missingKeys.ExceptWith(keys);
            if (missingKeys.Count > 0)
                missing.Add($"{lang}: {missingKeys.Count} eksik anahtar ({string.Join(", ", System.Linq.Enumerable.Take(missingKeys, 5))}...)");
        }
        return missing;
    }
}