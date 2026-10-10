using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DemocracySim.Engine.Data;

/// <summary>
/// FAZ 5: Ana menü — tüm UI'sini programatik kurar. Sahne kurulumu gerekmez.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Sahne Adı")]
    public string gameplaySceneName = "SampleScene";

    private Canvas _canvas;
    private TextMeshProUGUI _statsText;
    private CreditsPanel _creditsPanel;
    private System.Action _onSettingsAction;

void Start()
{
    LocalizationManager.LoadSavedLanguage();
    BuildUI();

    _creditsPanel = FindAnyObjectByType<CreditsPanel>();

    // Settings panelini oluştur
    var settingsManagerGO = new GameObject("SettingsManager");
    settingsManagerGO.transform.SetParent(transform.parent, false);
    var settingsPanel = settingsManagerGO.AddComponent<SettingsPanel>();

    // Buton aksiyonunu Ata
    _onSettingsAction = () => settingsPanel.Open(); 
}

    private void BuildUI()
    {
        // 1) Canvas
        var canvasGO = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        _canvas = canvasGO.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10;
        var sc = canvasGO.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        sc.matchWidthOrHeight = 0.5f;

        // 2) Arka plan (koyu lacivert, hafif gradyan efekti)
        var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(_canvas.transform, false);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        var bgImage = bgGO.GetComponent<Image>();
        // Banner'ı arka plana yükle
var banner = Resources.Load<Sprite>("Textures/KapitalBanner");
if (banner != null)
{
    bgImage.sprite = banner;
    bgImage.type = Image.Type.Simple;
    bgImage.color = new Color(1f, 1f, 1f, 0.35f);   // hafif saydam
}
else
{
    bgImage.color = new Color(0.05f, 0.08f, 0.13f, 1f);   // fallback
}

        // 3) Dekoratif üst çizgi
        var topLineGO = new GameObject("TopLine", typeof(RectTransform), typeof(Image));
        topLineGO.transform.SetParent(_canvas.transform, false);
        var topLineRT = topLineGO.GetComponent<RectTransform>();
        topLineRT.anchorMin = new Vector2(0f, 0.95f);
        topLineRT.anchorMax = new Vector2(1f, 0.955f);
        topLineRT.offsetMin = Vector2.zero;
        topLineRT.offsetMax = Vector2.zero;
        topLineGO.GetComponent<Image>().color = new Color(0.95f, 0.72f, 0.30f, 0.4f);   // Altın, yarı saydam

        // 4) Alt çizgi
        var botLineGO = new GameObject("BottomLine", typeof(RectTransform), typeof(Image));
        botLineGO.transform.SetParent(_canvas.transform, false);
        var botLineRT = botLineGO.GetComponent<RectTransform>();
        botLineRT.anchorMin = new Vector2(0f, 0.045f);
        botLineRT.anchorMax = new Vector2(1f, 0.05f);
        botLineRT.offsetMin = Vector2.zero;
        botLineRT.offsetMax = Vector2.zero;
        botLineGO.GetComponent<Image>().color = new Color(0.95f, 0.72f, 0.30f, 0.4f);

        // 5) Büyük başlık — KAPITAL
        var titleGO = new GameObject("Title", typeof(RectTransform));
        titleGO.transform.SetParent(_canvas.transform, false);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 0.82f);
        titleRT.anchorMax = new Vector2(0.5f, 0.82f);
        titleRT.pivot = new Vector2(0.5f, 0.5f);
        titleRT.sizeDelta = new Vector2(1200, 100);
        titleRT.anchoredPosition = Vector2.zero;
        var titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "KAPITAL";
        titleText.fontSize = 96;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.95f, 0.72f, 0.30f);   // Altın
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.characterSpacing = 10f;

        // 6) Alt başlık
        var subtitleGO = new GameObject("Subtitle", typeof(RectTransform));
        subtitleGO.transform.SetParent(_canvas.transform, false);
        var subtitleRT = subtitleGO.GetComponent<RectTransform>();
        subtitleRT.anchorMin = new Vector2(0.5f, 0.73f);
        subtitleRT.anchorMax = new Vector2(0.5f, 0.73f);
        subtitleRT.pivot = new Vector2(0.5f, 0.5f);
        subtitleRT.sizeDelta = new Vector2(1000, 50);
        subtitleRT.anchoredPosition = Vector2.zero;
        var subtitleText = subtitleGO.AddComponent<TextMeshProUGUI>();
        subtitleText.text = "Bir ülkeyi yönet. Tarihe geç.";
        subtitleText.fontSize = 28;
        subtitleText.fontStyle = FontStyles.Italic;
        subtitleText.color = new Color(0.55f, 0.62f, 0.75f);   // Soluk mavi
        subtitleText.alignment = TextAlignmentOptions.Center;

        // 7) Menü butonları (dikey)
        var btnContainerGO = new GameObject("ButtonContainer", typeof(RectTransform));
        btnContainerGO.transform.SetParent(_canvas.transform, false);
        var btnContainerRT = btnContainerGO.GetComponent<RectTransform>();
        btnContainerRT.anchorMin = new Vector2(0.5f, 0.55f);
        btnContainerRT.anchorMax = new Vector2(0.5f, 0.55f);
        btnContainerRT.pivot = new Vector2(0.5f, 0.5f);
        btnContainerRT.sizeDelta = new Vector2(400, 450);
        btnContainerRT.anchoredPosition = Vector2.zero;

        var vlg = btnContainerGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // 8) Butonlar — sırayla
        CreateButton(btnContainerGO.transform, LocalizationManager.Get("new_game").ToUpper(), OnNewGame, true);
CreateButton(btnContainerGO.transform, LocalizationManager.Get("continue").ToUpper(), OnContinue, SaveLoadManager.SaveExists());
CreateButton(btnContainerGO.transform, LocalizationManager.Get("demo_game").ToUpper(), OnDemoGame, true);
CreateButton(btnContainerGO.transform, LocalizationManager.Get("credits").ToUpper(), OnCredits, true);
CreateButton(btnContainerGO.transform, LocalizationManager.Get("settings").ToUpper(), () => _onSettingsAction?.Invoke(), true);
CreateButton(btnContainerGO.transform, LocalizationManager.Get("quit").ToUpper(), OnQuit, true);
CreateButton(btnContainerGO.transform, "HOT-SEAT", OnHotSeat, true);
CreateButton(btnContainerGO.transform, "PBEM", OnPBEM, true);

        // 9) Alt bilgi — versiyon
        var versionGO = new GameObject("Version", typeof(RectTransform));
        versionGO.transform.SetParent(_canvas.transform, false);
        var versionRT = versionGO.GetComponent<RectTransform>();
        versionRT.anchorMin = new Vector2(0.5f, 0.02f);
        versionRT.anchorMax = new Vector2(0.5f, 0.02f);
        versionRT.pivot = new Vector2(0.5f, 0.5f);
        versionRT.sizeDelta = new Vector2(800, 30);
        versionRT.anchoredPosition = Vector2.zero;
        var versionText = versionGO.AddComponent<TextMeshProUGUI>();
        versionText.text = "DEMOCRACYSIM v1.0 • Unity 6";
        versionText.fontSize = 16;
        versionText.color = new Color(0.45f, 0.52f, 0.65f);
        versionText.alignment = TextAlignmentOptions.Center;

        // 10) İstatistik paneli (sağ üst köşe)
        BuildStatsPanel();

        // 11) EventSystem kontrolü
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            Debug.Log("[MainMenu] EventSystem eklendi.");
        }
    }

    private Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, bool interactable)
    {
        var btnGO = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.sizeDelta = new Vector2(400, 60);

        var btnImage = btnGO.GetComponent<Image>();
        btnImage.color = interactable 
            ? new Color(0.15f, 0.22f, 0.35f)   // Koyu mavi
            : new Color(0.10f, 0.13f, 0.18f, 0.5f);   // Soluk

        var btn = btnGO.GetComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.interactable = interactable;

        // Renk geçişleri
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.95f, 0.72f, 0.30f, 1f);   // Altın hover
        colors.pressedColor = new Color(0.75f, 0.55f, 0.20f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        colors.fadeDuration = 0.1f;
        btn.colors = colors;

        // Buton metni
        var btnTextGO = new GameObject("Text", typeof(RectTransform));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        var btnTextRT = btnTextGO.GetComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.offsetMin = Vector2.zero;
        btnTextRT.offsetMax = Vector2.zero;

        var btnText = btnTextGO.AddComponent<TextMeshProUGUI>();
        btnText.text = label;
        btnText.fontSize = 26;
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = new Color(0.92f, 0.94f, 0.97f);
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.characterSpacing = 5f;

        if (onClick != null) btn.onClick.AddListener(onClick);
        return btn;
    }
    void OnPBEM()
{
    AudioManager.Instance?.PlayClick();
    GameManager.LoadSaveOnStart = false;
    PlayerPrefs.SetInt("StartPBEM", 1);
    PlayerPrefs.Save();
    SceneManager.LoadScene(gameplaySceneName);
}

    private void BuildStatsPanel()
    {
        var statsGO = new GameObject("StatsPanel", typeof(RectTransform));
        statsGO.transform.SetParent(_canvas.transform, false);
        var statsRT = statsGO.GetComponent<RectTransform>();
        statsRT.anchorMin = new Vector2(0.02f, 0.10f);
        statsRT.anchorMax = new Vector2(0.35f, 0.30f);
        statsRT.offsetMin = Vector2.zero;
        statsRT.offsetMax = Vector2.zero;

        var statsBg = statsGO.AddComponent<Image>();
        statsBg.color = new Color(0.10f, 0.14f, 0.22f, 0.7f);

        var statsTitleGO = new GameObject("Title", typeof(RectTransform));
        statsTitleGO.transform.SetParent(statsGO.transform, false);
        var statsTitleRT = statsTitleGO.GetComponent<RectTransform>();
        statsTitleRT.anchorMin = new Vector2(0f, 0.85f);
        statsTitleRT.anchorMax = new Vector2(1f, 1f);
        statsTitleRT.offsetMin = new Vector2(15, 0);
        statsTitleRT.offsetMax = new Vector2(-15, 0);
        var statsTitle = statsTitleGO.AddComponent<TextMeshProUGUI>();
        statsTitle.text = "İSTATİSTİKLERİN";
        statsTitle.fontSize = 18;
        statsTitle.fontStyle = FontStyles.Bold;
        statsTitle.color = new Color(0.95f, 0.72f, 0.30f);
        statsTitle.alignment = TextAlignmentOptions.Left;

        var statsBodyGO = new GameObject("Body", typeof(RectTransform));
        statsBodyGO.transform.SetParent(statsGO.transform, false);
        var statsBodyRT = statsBodyGO.GetComponent<RectTransform>();
        statsBodyRT.anchorMin = new Vector2(0f, 0f);
        statsBodyRT.anchorMax = new Vector2(1f, 0.85f);
        statsBodyRT.offsetMin = new Vector2(15, 10);
        statsBodyRT.offsetMax = new Vector2(-15, 0);

        _statsText = statsBodyGO.AddComponent<TextMeshProUGUI>();
        var lines = GlobalStatsTracker.GetStatsLines();
        _statsText.text = string.Join("\n", lines);
        _statsText.fontSize = 16;
        _statsText.color = new Color(0.85f, 0.88f, 0.92f);
        _statsText.alignment = TextAlignmentOptions.TopLeft;
        _statsText.textWrappingMode = TextWrappingModes.Normal;
    }

    // ============================================================
    // BUTON AKSİYONLARI
    // ============================================================
    void OnNewGame()
    {
        AudioManager.Instance?.PlayClick();
        GameManager.LoadSaveOnStart = false;
        SceneManager.LoadScene(gameplaySceneName);
    }

    void OnContinue()
    {
        if (!SaveLoadManager.SaveExists()) return;
        AudioManager.Instance?.PlayClick();
        GameManager.LoadSaveOnStart = true;
        SceneManager.LoadScene(gameplaySceneName);
    }
    void OnHotSeat()
{
    AudioManager.Instance?.PlayClick();
    GameManager.LoadSaveOnStart = false;
    SceneManager.LoadScene(gameplaySceneName);
    // Oyun başladıktan sonra otomatik lobi açılır (PlayerPrefs ile işaret)
    PlayerPrefs.SetInt("StartHotSeat", 1);
    PlayerPrefs.Save();
}

    void OnDemoGame()
    {
        AudioManager.Instance?.PlayClick();
        PlayerPrefs.SetInt("Tutorial", 1);
        GameManager.LoadSaveOnStart = false;
        // Demo için özel bir işaret koy
        PlayerPrefs.SetInt("StartDemo", 1);
        SceneManager.LoadScene(gameplaySceneName);
    }

    void OnCredits()
    {
        AudioManager.Instance?.PlayClick();
        if (_creditsPanel == null) _creditsPanel = FindAnyObjectByType<CreditsPanel>();
        _creditsPanel?.Open();
    }

    void OnSettings()
    {
        AudioManager.Instance?.PlayClick();
        Debug.Log("[MainMenu] Ayarlar paneli henüz bağlanmadı.");
    }

    void OnQuit()
    {
        AudioManager.Instance?.PlayClick();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}