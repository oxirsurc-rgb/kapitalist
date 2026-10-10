using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DemocracySim.Engine.Data;
using static LocalizationManager;
public class SettingsPanel : MonoBehaviour
{
    private Canvas _canvas;
    private RectTransform _panelRoot;

    void Awake()
    {
        BuildUI();
    }

    private void BuildUI()
    {
        // Canvas oluştur
        var canvasGO = new GameObject("SettingsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        _canvas = canvasGO.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 200;
        var sc = canvasGO.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        sc.matchWidthOrHeight = 0.5f;

        // Arka plan
        var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(_canvas.transform, false);
        _panelRoot = bgGO.GetComponent<RectTransform>();
        _panelRoot.anchorMin = Vector2.zero;
        _panelRoot.anchorMax = Vector2.one;
        _panelRoot.offsetMin = Vector2.zero;
        _panelRoot.offsetMax = Vector2.zero;
        bgGO.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.95f);

        // Başlık
        var titleGO = new GameObject("Title", typeof(RectTransform));
        titleGO.transform.SetParent(_panelRoot, false);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 0.85f);
        titleRT.anchorMax = new Vector2(0.5f, 0.85f);
        titleRT.pivot = new Vector2(0.5f, 0.5f);
        titleRT.sizeDelta = new Vector2(600, 60);
        titleRT.anchoredPosition = Vector2.zero;
        var titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "AYARLAR";
        titleText.fontSize = 48;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.95f, 0.72f, 0.30f);
        titleText.alignment = TextAlignmentOptions.Center;

        // Dil Seçimi
        CreateLanguageButtons();

        // Kapat Butonu
        var closeBtnGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnGO.transform.SetParent(_panelRoot, false);
        var closeRT = closeBtnGO.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(0.5f, 0.1f);
        closeRT.anchorMax = new Vector2(0.5f, 0.1f);
        closeRT.pivot = new Vector2(0.5f, 0.5f);
        closeRT.sizeDelta = new Vector2(300, 60);
        closeRT.anchoredPosition = Vector2.zero;
        var closeImg = closeBtnGO.GetComponent<Image>();
        closeImg.color = new Color(0.55f, 0.35f, 0.15f);
        var closeBtn = closeBtnGO.GetComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener(Close);

        var closeTextGO = new GameObject("Text", typeof(RectTransform));
        closeTextGO.transform.SetParent(closeBtnGO.transform, false);
        var closeTextRT = closeTextGO.GetComponent<RectTransform>();
        closeTextRT.anchorMin = Vector2.zero;
        closeTextRT.anchorMax = Vector2.one;
        closeTextRT.offsetMin = Vector2.zero;
        closeTextRT.offsetMax = Vector2.zero;
        var closeText = closeTextGO.AddComponent<TextMeshProUGUI>();
        closeText.text = "KAPAT";
        closeText.fontSize = 24;
        closeText.fontStyle = FontStyles.Bold;
        closeText.color = Color.white;
        closeText.alignment = TextAlignmentOptions.Center;

        _panelRoot.gameObject.SetActive(false);
        CreateModList();
    }

    private void CreateLanguageButtons()
    {
        var containerGO = new GameObject("LanguageContainer", typeof(RectTransform));
        containerGO.transform.SetParent(_panelRoot, false);
        var containerRT = containerGO.GetComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(0.5f, 0.6f);
        containerRT.anchorMax = new Vector2(0.5f, 0.6f);
        containerRT.pivot = new Vector2(0.5f, 0.5f);
        containerRT.sizeDelta = new Vector2(500, 400);
        containerRT.anchoredPosition = Vector2.zero;

        var vlg = containerGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        


        string[] languageNames = { "TÜRKÇE", "ENGLISH", "DEUTSCH", "中文", "PORTUGUÊS" };
        Language[] languages = { Language.Turkish, Language.English, Language.German, Language.Chinese, Language.Portuguese
        
        
}; 


        for (int i = 0; i < languages.Length; i++)
        {
            var lang = languages[i];
            var btnGO = new GameObject(languageNames[i], typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(containerGO.transform, false);
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.sizeDelta = new Vector2(500, 50);

            var btnImg = btnGO.GetComponent<Image>();
            btnImg.color = new Color(0.15f, 0.22f, 0.35f);

            var btn = btnGO.GetComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() => {
    LocalizationManager.SetLanguage(lang);   // Bu event yayınlar
    // UI kendi yenilenir (OnLanguageChanged event'i ile)
    Close();
});

            var btnTextGO = new GameObject("Text", typeof(RectTransform));
            btnTextGO.transform.SetParent(btnGO.transform, false);
            var btnTextRT = btnTextGO.GetComponent<RectTransform>();
            btnTextRT.anchorMin = Vector2.zero;
            btnTextRT.anchorMax = Vector2.one;
            btnTextRT.offsetMin = Vector2.zero;
            btnTextRT.offsetMax = Vector2.zero;
            var btnText = btnTextGO.AddComponent<TextMeshProUGUI>();
            btnText.text = languageNames[i];
            btnText.fontSize = 24;
            btnText.fontStyle = FontStyles.Bold;
            btnText.color = Color.white;
            btnText.alignment = TextAlignmentOptions.Center;
        }
    }

    private void CreateModList()
{
    if (ModManager.LoadedMods.Count == 0) return;

    var containerGO = new GameObject("ModContainer", typeof(RectTransform));
    containerGO.transform.SetParent(_panelRoot, false);
    var containerRT = containerGO.GetComponent<RectTransform>();
    containerRT.anchorMin = new Vector2(0.15f, 0.2f);
    containerRT.anchorMax = new Vector2(0.85f, 0.4f);
    containerRT.offsetMin = Vector2.zero;
    containerRT.offsetMax = Vector2.zero;

    var title = HudKit.Label(containerGO.transform, "YÜKLÜ MODLAR", 22, new Color(0.95f, 0.72f, 0.30f));
    var vlg = containerGO.AddComponent<VerticalLayoutGroup>();
    vlg.spacing = 8;
    vlg.padding = new RectOffset(10, 10, 10, 10);

    foreach (var mod in ModManager.LoadedMods)
    {
        var m = mod;
        var rowGO = new GameObject($"Mod_{m.Id}", typeof(RectTransform));
        rowGO.transform.SetParent(containerGO.transform, false);
        var rowRT = rowGO.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, 40);

        var label = HudKit.Label(rowGO.transform, $"{m.Name} v{m.Version} — {m.Author}", 18, Color.white);

        // Toggle
        var toggleGO = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
        toggleGO.transform.SetParent(rowGO.transform, false);
        var toggle = toggleGO.GetComponent<Toggle>();
        toggle.isOn = m.Enabled;
        toggle.onValueChanged.AddListener(v => ModManager.SetModEnabled(m.Id, v));
    }
}
   

    public void Open()
    {
        if (_panelRoot != null) _panelRoot.gameObject.SetActive(true);
        AudioManager.Instance?.PlayClick();
    }

    public void Close()
    {
        if (_panelRoot != null) _panelRoot.gameObject.SetActive(false);
        AudioManager.Instance?.PlayClick();
    }
}