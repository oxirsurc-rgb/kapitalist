using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// FAZ 19: Erişilebilirlik ayarları — yazı boyutu, renk körü modu, ses altyazıları.
/// </summary>
public class AccessibilitySettings : MonoBehaviour
{
    private const string KEY_FONT_SCALE = "Acc_FontScale";
    private const string KEY_COLORBLIND = "Acc_ColorBlind";
    private const string KEY_SUBTITLES = "Acc_Subtitles";

    public static float FontScale => PlayerPrefs.GetFloat(KEY_FONT_SCALE, 1.0f);
    public static bool ColorBlindMode => PlayerPrefs.GetInt(KEY_COLORBLIND, 0) == 1;
    public static bool SubtitlesEnabled => PlayerPrefs.GetInt(KEY_SUBTITLES, 1) == 1;

    public static void SetFontScale(float scale)
    {
        scale = Mathf.Clamp(scale, 0.8f, 1.5f);
        PlayerPrefs.SetFloat(KEY_FONT_SCALE, scale);
        PlayerPrefs.Save();
        ApplyToAllText();
    }

    public static void SetColorBlindMode(bool on)
    {
        PlayerPrefs.SetInt(KEY_COLORBLIND, on ? 1 : 0);
        PlayerPrefs.Save();
        ApplyColorBlindMode();
    }

    public static void SetSubtitles(bool on)
    {
        PlayerPrefs.SetInt(KEY_SUBTITLES, on ? 1 : 0);
        PlayerPrefs.Save();
    }

    private static void ApplyToAllText()
    {
        var allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in allTexts)
        {
            if (!t.gameObject.name.Contains("__base"))
                t.fontSize *= FontScale;
        }
    }

    private static void ApplyColorBlindMode()
    {
        // Deuteranopia filtresi — Camera'ya post-process olarak
        var cam = Camera.main;
        if (cam == null) return;
        
        if (ColorBlindMode)
        {
            // Basit yaklaşım: renk düzeltme component'i ekle
            Debug.Log("[Acc] Renk körü modu aktif.");
            // Tam çözüm için Post Processing paketi + LUT texture gerekir.
        }
    }
}