using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FAZ 5: Credits ekranı — kendi UI'sini programatik kurar. Sahne kurulumu gerekmez.
/// </summary>
public class CreditsPanel : MonoBehaviour
{
    [Header("İçerik")]
    [TextArea(10, 30)]
    public string creditsContent;

    private Canvas _canvas;
    private RectTransform _panelRoot;
    private TextMeshProUGUI _creditsText;

    void Awake()
    {
        BuildUI();
    }

    private void BuildUI()
    {
        // 1) Canvas (yoksa oluştur)
        _canvas = GetComponentInChildren<Canvas>();
        if (_canvas == null)
        {
            var canvasGO = new GameObject("CreditsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            _canvas = canvasGO.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;  // Ana menünün üstünde
            var sc = canvasGO.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            sc.matchWidthOrHeight = 0.5f;
        }

        // 2) Arka plan (yarı saydam siyah, tam ekran)
        var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(_canvas.transform, false);
        _panelRoot = bgGO.GetComponent<RectTransform>();
        _panelRoot.anchorMin = Vector2.zero;
        _panelRoot.anchorMax = Vector2.one;
        _panelRoot.offsetMin = Vector2.zero;
        _panelRoot.offsetMax = Vector2.zero;
        var bgImage = bgGO.GetComponent<Image>();
        bgImage.color = new Color(0.05f, 0.08f, 0.12f, 0.96f);

        // 3) İçerik metni (üst kısım)
        var textGO = new GameObject("CreditsText", typeof(RectTransform));
        textGO.transform.SetParent(_panelRoot, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.1f, 0.15f);
        textRT.anchorMax = new Vector2(0.9f, 0.92f);
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        _creditsText = textGO.AddComponent<TextMeshProUGUI>();
        _creditsText.text = string.IsNullOrEmpty(creditsContent) ? BuildDefaultCredits() : creditsContent;
        _creditsText.fontSize = 22;
        _creditsText.color = new Color(0.92f, 0.94f, 0.97f);
        _creditsText.alignment = TextAlignmentOptions.Top;
        _creditsText.textWrappingMode = TextWrappingModes.Normal;
        _creditsText.overflowMode = TextOverflowModes.Overflow;

        // 4) KAPAT butonu (alt kısım)
        var btnGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(_panelRoot, false);
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0.05f);
        btnRT.anchorMax = new Vector2(0.5f, 0.05f);
        btnRT.pivot = new Vector2(0.5f, 0.5f);
        btnRT.sizeDelta = new Vector2(300, 60);
        btnRT.anchoredPosition = Vector2.zero;
        var btnImage = btnGO.GetComponent<Image>();
        btnImage.color = new Color(0.55f, 0.35f, 0.15f);  // Altın/kahve
        var btn = btnGO.GetComponent<Button>();
        btn.targetGraphic = btnImage;

        var btnTextGO = new GameObject("Text", typeof(RectTransform));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        var btnTextRT = btnTextGO.GetComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.offsetMin = Vector2.zero;
        btnTextRT.offsetMax = Vector2.zero;
        var btnText = btnTextGO.AddComponent<TextMeshProUGUI>();
        btnText.text = "KAPAT";
        btnText.fontSize = 24;
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;

        btn.onClick.AddListener(Close);

        // Başlangıçta gizli
        _panelRoot.gameObject.SetActive(false);
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

private string BuildDefaultCredits()
{
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("<size=48><b>KAPİTAL</b></size>"); // Buraya oyunun adını yaz
    sb.AppendLine("<line-height=150%>");
    sb.AppendLine("<color=#F2B84B>═══════════════════════════════</color>");
    sb.AppendLine();
    sb.AppendLine("<b>YAPIMCI</b>");
    sb.AppendLine("  [xXLARGEarx]");
    sb.AppendLine();
    sb.AppendLine();
    sb.AppendLine("<b>TEŞEKKÜRLER</b>");
    sb.AppendLine("  Oynayan herkese teşekkürler.");
    sb.AppendLine();
    sb.AppendLine("<color=#F2B84B>═══════════════════════════════</color>");
    sb.AppendLine("<size=18>SÜRÜM 1.0</size>");
    return sb.ToString();
}
}