using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DemocracySim.Engine.Core.Multiplayer;

public class PBEMUIPanel : MonoBehaviour
{
    private Canvas _canvas;
    private RectTransform _panelRoot;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _statusText;
    private RectTransform _contentRoot;

    void Awake() { BuildUI(); }

    private void BuildUI()
    {
        var canvasGO = new GameObject("PBEMCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        _canvas = canvasGO.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 998;
        var sc = canvasGO.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);

        var bgGO = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(_canvas.transform, false);
        _panelRoot = bgGO.GetComponent<RectTransform>();
        _panelRoot.anchorMin = Vector2.zero;
        _panelRoot.anchorMax = Vector2.one;
        _panelRoot.offsetMin = Vector2.zero;
        _panelRoot.offsetMax = Vector2.zero;
        bgGO.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.09f, 0.98f);

        var titleGO = new GameObject("Title", typeof(RectTransform));
        titleGO.transform.SetParent(_panelRoot, false);
        var tRT = titleGO.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0.5f, 0.88f);
        tRT.anchorMax = new Vector2(0.5f, 0.88f);
        tRT.sizeDelta = new Vector2(1400, 80);
        _titleText = titleGO.AddComponent<TextMeshProUGUI>();
        _titleText.fontSize = 52;
        _titleText.fontStyle = FontStyles.Bold;
        _titleText.color = new Color(0.95f, 0.72f, 0.30f);
        _titleText.alignment = TextAlignmentOptions.Center;

        var statusGO = new GameObject("Status", typeof(RectTransform));
        statusGO.transform.SetParent(_panelRoot, false);
        var sRT = statusGO.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0.5f, 0.10f);
        sRT.anchorMax = new Vector2(0.5f, 0.30f);
        sRT.sizeDelta = new Vector2(1200, 200);
        _statusText = statusGO.AddComponent<TextMeshProUGUI>();
        _statusText.fontSize = 22;
        _statusText.color = new Color(0.85f, 0.90f, 0.95f);
        _statusText.alignment = TextAlignmentOptions.Center;

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(_panelRoot, false);
        _contentRoot = contentGO.GetComponent<RectTransform>();
        _contentRoot.anchorMin = new Vector2(0.5f, 0.55f);
        _contentRoot.anchorMax = new Vector2(0.5f, 0.55f);
        _contentRoot.sizeDelta = new Vector2(900, 400);

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 16;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        _panelRoot.gameObject.SetActive(false);
    }

    public void Show()
    {
        _panelRoot.gameObject.SetActive(true);
        _titleText.text = "PBEM OTURUMU";
        RebuildContent();
    }

    public void Close() { _panelRoot.gameObject.SetActive(false); }

    private void RebuildContent()
    {
        for (int i = _contentRoot.childCount - 1; i >= 0; i--)
            Destroy(_contentRoot.GetChild(i).gameObject);

        // Oturum bilgisi
        var session = SessionManager.ActiveSession;
        if (session != null)
        {
            _statusText.text =
                $"Oturum: {session.SessionName}\n" +
                $"Tur: {session.CurrentTurn}\n" +
                $"Sırada: {session.CurrentPlayer?.PlayerName} ({session.CurrentPlayer?.CountryName})";
        }
        else
        {
            _statusText.text = "Aktif oturum yok.";
        }

        // Butonlar
        CreateButton("+ Yeni PBEM Oturumu", new Color(0.15f, 0.35f, 0.25f), StartNewPBEM);
        CreateButton("💾 Oturumu Kaydet", new Color(0.20f, 0.35f, 0.55f), SaveCurrentSession);
        CreateButton("📂 Oturum Yükle (son dosya)", new Color(0.30f, 0.45f, 0.30f), LoadLatestSession);
        CreateButton("Kapat", new Color(0.3f, 0.3f, 0.3f), Close);
    }

    private void CreateButton(string label, Color bg, Action onClick)
    {
        var btnGO = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(_contentRoot, false);
        var rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600, 56);

        var img = btnGO.GetComponent<Image>();
        img.color = bg;
        var btn = btnGO.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var txtGO = new GameObject("Text", typeof(RectTransform));
        txtGO.transform.SetParent(btnGO.transform, false);
        var tRT = txtGO.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = Vector2.zero;
        tRT.offsetMax = Vector2.zero;
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 22;
        txt.fontStyle = FontStyles.Bold;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
    }

    private void StartNewPBEM()
    {
        // İlk oyuncunun ülkesini al, session oluştur
        var gm = FindAnyObjectByType<GameManager>();
        if (gm == null) { _statusText.text = "GameManager bulunamadı."; return; }

        // Basit yaklaşım: 2 oyuncu, ilk ikisi
        var profiles = new List<PlayerSlot>
        {
            new PlayerSlot(0, "Oyuncu 1", "tur", "Türkiye"),
            new PlayerSlot(1, "Oyuncu 2", "usa", "USA")
        };

        uint seed = (uint)DateTime.Now.Ticks;
        var session = SessionManager.CreateHotSeat(profiles, seed);
        if (session != null)
        {
            session.Mode = MultiplayerMode.PBEM;
            session.SessionName = "PBEM Oturumu";
            _statusText.text = "PBEM oturumu başlatıldı!";
            RebuildContent();
        }
    }

    private void SaveCurrentSession()
    {
        string path = SessionManager.SaveSession("pbem_manual.json");
        if (path != null)
        {
            _statusText.text = $"✅ Kaydedildi:\n{path}\n\n" +
                               $"Bu dosyayı sıradaki oyuncuya gönderin.";
        }
        else
        {
            _statusText.text = "❌ Kaydetme başarısız. Aktif oturum yok mu?";
        }
    }

    private void LoadLatestSession()
    {
        string dir = Path.Combine(Application.persistentDataPath, "sessions");
        if (!Directory.Exists(dir)) { _statusText.text = "Sessions klasörü yok."; return; }

        var file = new DirectoryInfo(dir).GetFiles("*.session.json")
            .OrderByDescending(f => f.LastWriteTime)
            .FirstOrDefault();

        if (file == null) { _statusText.text = "Hiç oturum dosyası yok."; return; }

        var session = SessionManager.LoadSession(file.FullName);
        if (session == null) { _statusText.text = "Dosya bozuk."; return; }

        _statusText.text = $"✅ Yüklendi:\n{file.Name}\n\nOyuna devam etmek için 'Kapat'a bas.";
        RebuildContent();
    }
}