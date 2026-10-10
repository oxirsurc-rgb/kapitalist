using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DemocracySim.Engine.Core;
using DemocracySim.Engine.Core.Multiplayer;

/// <summary>
/// FAZ 23.1: Hot-Seat lobi ekranı ve "sıra sende" ekranı.
/// Layout ve seçim mantığı basitleştirildi.
/// </summary>
public class HotSeatUIPanel : MonoBehaviour
{
    private Canvas _canvas;
    private RectTransform _panelRoot;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _subtitleText;
    private RectTransform _contentRoot;

    // Lobi state
    private readonly List<PlayerSlot> _tempPlayers = new List<PlayerSlot>();
    private int _playerCount = 2;
    private Action<SessionData> _onSessionCreated;
    private List<string> _lastCountryOptions;

    void Awake()
    {
        BuildUI();
    }

    // ═══════════════════════════════════════════════════════════
    // UI KURULUM (bir kez)
    // ═══════════════════════════════════════════════════════════
    private void BuildUI()
    {
        // Canvas
        var canvasGO = new GameObject("HotSeatCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        _canvas = canvasGO.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 999;   // UIManager'ın üstünde
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
        bgGO.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.09f, 0.98f);

        // Başlık
        var titleGO = new GameObject("Title", typeof(RectTransform));
        titleGO.transform.SetParent(_panelRoot, false);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 0.88f);
        titleRT.anchorMax = new Vector2(0.5f, 0.88f);
        titleRT.pivot = new Vector2(0.5f, 0.5f);
        titleRT.sizeDelta = new Vector2(1400, 80);
        titleRT.anchoredPosition = Vector2.zero;
        _titleText = titleGO.AddComponent<TextMeshProUGUI>();
        _titleText.fontSize = 52;
        _titleText.fontStyle = FontStyles.Bold;
        _titleText.color = new Color(0.95f, 0.72f, 0.30f);
        _titleText.alignment = TextAlignmentOptions.Center;

        // Alt başlık
        var subGO = new GameObject("Subtitle", typeof(RectTransform));
        subGO.transform.SetParent(_panelRoot, false);
        var subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.5f, 0.81f);
        subRT.anchorMax = new Vector2(0.5f, 0.81f);
        subRT.pivot = new Vector2(0.5f, 0.5f);
        subRT.sizeDelta = new Vector2(1200, 40);
        subRT.anchoredPosition = Vector2.zero;
        _subtitleText = subGO.AddComponent<TextMeshProUGUI>();
        _subtitleText.fontSize = 24;
        _subtitleText.color = new Color(0.65f, 0.72f, 0.85f);
        _subtitleText.alignment = TextAlignmentOptions.Center;

        // İçerik alanı
        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(_panelRoot, false);
        _contentRoot = contentGO.GetComponent<RectTransform>();
        _contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _contentRoot.pivot = new Vector2(0.5f, 0.5f);
        _contentRoot.sizeDelta = new Vector2(1000, 600);
        _contentRoot.anchoredPosition = new Vector2(0, -60);

        _panelRoot.gameObject.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════
    // LOBİ EKRANI
    // ═══════════════════════════════════════════════════════════
    public void ShowLobby(int playerCount, List<string> countryOptions, Action<SessionData> onSessionCreated)
    {
        _playerCount = Mathf.Clamp(playerCount, 2, 4);
        _tempPlayers.Clear();
        _onSessionCreated = onSessionCreated;
        _lastCountryOptions = countryOptions;

        _titleText.text = "HOT-SEAT OYUN";
        _subtitleText.text = $"{_playerCount} oyuncu — sırayla oynayın";

        RebuildLobby();
        _panelRoot.gameObject.SetActive(true);
    }

    private void RebuildLobby()
    {
        // Temizle
        for (int i = _contentRoot.childCount - 1; i >= 0; i--)
            Destroy(_contentRoot.GetChild(i).gameObject);

        float rowHeight = 70f;
        float rowSpacing = 10f;
        float y = 0f;

        // Her oyuncu için satır
        for (int i = 0; i < _playerCount; i++)
        {
            var slot = _tempPlayers.FirstOrDefault(p => p.Index == i);
            CreatePlayerRow(i, y, rowHeight, slot);
            y -= (rowHeight + rowSpacing);
        }

        // Oyuncu sayısı değiştir
        y -= 10;
        CreateCountRow(y);
        y -= 70;

        // Başlat butonu
        y -= 20;
        CreateActionButton(y, "OYUNU BAŞLAT", new Color(0.95f, 0.72f, 0.30f),
            new Color(0.1f, 0.08f, 0.02f), StartHotSeatGame);
        y -= 70;

        // İptal
        CreateActionButton(y, "İptal", new Color(0.3f, 0.3f, 0.3f), Color.white, Close);
    }

    private void CreatePlayerRow(int index, float y, float height, PlayerSlot slot)
    {
        // Satır kabı
        var rowGO = new GameObject($"Player_{index}", typeof(RectTransform), typeof(Image));
        rowGO.transform.SetParent(_contentRoot, false);
        var rowRT = rowGO.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0.5f, 0.5f);
        rowRT.anchorMax = new Vector2(0.5f, 0.5f);
        rowRT.pivot = new Vector2(0.5f, 0.5f);
        rowRT.sizeDelta = new Vector2(900, height);
        rowRT.anchoredPosition = new Vector2(0, y);
        rowGO.GetComponent<Image>().color = new Color(0.10f, 0.14f, 0.22f, 1f);

        // Oyuncu adı (sol)
        var nameGO = new GameObject("Name", typeof(RectTransform));
        nameGO.transform.SetParent(rowGO.transform, false);
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 0.5f);
        nameRT.anchorMax = new Vector2(0, 0.5f);
        nameRT.pivot = new Vector2(0, 0.5f);
        nameRT.anchoredPosition = new Vector2(20, 0);
        nameRT.sizeDelta = new Vector2(280, 50);
        var nameText = nameGO.AddComponent<TextMeshProUGUI>();
        nameText.text = $"Oyuncu {index + 1}";
        nameText.fontSize = 22;
        nameText.color = Color.white;
        nameText.alignment = TextAlignmentOptions.Left;

        // Ülke Seç butonu
        var btn = CreateButton(rowGO.transform, "Ülke Seç",
            new Color(0.15f, 0.22f, 0.35f), Color.white,
            () => { ShowCountryPicker(index); });
        var btnRT = btn.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0, 0.5f);
        btnRT.anchorMax = new Vector2(0, 0.5f);
        btnRT.pivot = new Vector2(0, 0.5f);
        btnRT.anchoredPosition = new Vector2(320, 0);
        btnRT.sizeDelta = new Vector2(180, 45);

        // Seçili ülke göstergesi (sağda)
        var countryGO = new GameObject("CountryLabel", typeof(RectTransform));
        countryGO.transform.SetParent(rowGO.transform, false);
        var countryRT = countryGO.GetComponent<RectTransform>();
        countryRT.anchorMin = new Vector2(0, 0.5f);
        countryRT.anchorMax = new Vector2(0, 0.5f);
        countryRT.pivot = new Vector2(0, 0.5f);
        countryRT.anchoredPosition = new Vector2(520, 0);
        countryRT.sizeDelta = new Vector2(350, 50);
        var cl = countryGO.AddComponent<TextMeshProUGUI>();
        if (slot != null)
        {
            cl.text = slot.CountryName;
            cl.color = Color.white;
            cl.fontStyle = FontStyles.Bold;
        }
        else
        {
            cl.text = "(seçilmedi)";
            cl.color = new Color(0.7f, 0.7f, 0.7f);
            cl.fontStyle = FontStyles.Italic;
        }
        cl.fontSize = 22;
        cl.alignment = TextAlignmentOptions.Left;
    }

    private void CreateCountRow(float y)
    {
        var countGO = new GameObject("CountRow", typeof(RectTransform));
        countGO.transform.SetParent(_contentRoot, false);
        var countRT = countGO.GetComponent<RectTransform>();
        countRT.anchorMin = new Vector2(0.5f, 0.5f);
        countRT.anchorMax = new Vector2(0.5f, 0.5f);
        countRT.pivot = new Vector2(0.5f, 0.5f);
        countRT.sizeDelta = new Vector2(400, 60);
        countRT.anchoredPosition = new Vector2(0, y);

        var btn = CreateButton(countGO.transform, $"Oyuncu Sayısı: {_playerCount}  (değiştir)",
            new Color(0.15f, 0.22f, 0.35f), Color.white,
            () =>
            {
                _playerCount = _playerCount >= 4 ? 2 : _playerCount + 1;
                RebuildLobby();
            });
        var btnRT = btn.GetComponent<RectTransform>();
        btnRT.anchorMin = Vector2.zero;
        btnRT.anchorMax = Vector2.one;
        btnRT.offsetMin = Vector2.zero;
        btnRT.offsetMax = Vector2.zero;
    }

    private void CreateActionButton(float y, string label, Color bg, Color fg, Action onClick)
    {
        var btn = CreateButton(_contentRoot, label, bg, fg, onClick);
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(500, 60);
        rt.anchoredPosition = new Vector2(0, y);
    }

    // ═══════════════════════════════════════════════════════════
    // ÜLKE SEÇİCİ (INLINE — UIManager'a bağımlı değil)
    // ═══════════════════════════════════════════════════════════
    private void ShowCountryPicker(int playerIndex)
    {
        _titleText.text = "ÜLKE SEÇ";
        _subtitleText.text = $"Oyuncu {playerIndex + 1} için bir ülke seç";

        for (int i = _contentRoot.childCount - 1; i >= 0; i--)
            Destroy(_contentRoot.GetChild(i).gameObject);

        float y = 0;
        float rowH = 60;
        float spacing = 8;

        // Grid: 4 sütun
        int columns = 4;
        int col = 0;
        int row = 0;

        foreach (var countryName in _lastCountryOptions)
        {
            string cName = countryName;

            bool takenByOther = _tempPlayers.Any(p =>
                p.CountryId == cName && p.Index != playerIndex);

            var btnGO = new GameObject($"Country_{cName}",
                typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(_contentRoot, false);
            var rt = btnGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);

            float totalW = columns * 220 + (columns - 1) * 10;
            float startX = -totalW / 2f + 110;
            rt.anchoredPosition = new Vector2(startX + col * 230, -(row * (rowH + spacing)) - 20);
            rt.sizeDelta = new Vector2(220, rowH);

            var img = btnGO.GetComponent<Image>();
            img.color = takenByOther
                ? new Color(0.15f, 0.15f, 0.15f, 0.5f)
                : new Color(0.15f, 0.22f, 0.35f);

            var btn = btnGO.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = !takenByOther;

            var txtGO = new GameObject("Text", typeof(RectTransform));
            txtGO.transform.SetParent(btnGO.transform, false);
            var trt = txtGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            var txt = txtGO.AddComponent<TextMeshProUGUI>();
            txt.text = cName + (takenByOther ? "  (alındı)" : "");
            txt.fontSize = 20;
            txt.fontStyle = FontStyles.Bold;
            txt.color = takenByOther ? new Color(0.6f, 0.6f, 0.6f) : Color.white;
            txt.alignment = TextAlignmentOptions.Center;

            if (!takenByOther)
            {
                btn.onClick.AddListener(() =>
                {
                    // Slotu güncelle
                    var existing = _tempPlayers.FirstOrDefault(p => p.Index == playerIndex);
                    if (existing == null)
                    {
                        existing = new PlayerSlot(playerIndex,
                            $"Oyuncu {playerIndex + 1}", cName, cName);
                        _tempPlayers.Add(existing);
                    }
                    else
                    {
                        existing.CountryId = cName;
                        existing.CountryName = cName;
                    }

                    RebuildLobby();
                    _titleText.text = "HOT-SEAT OYUN";
                    _subtitleText.text = $"{_playerCount} oyuncu — sırayla oynayın";
                });
            }

            col++;
            if (col >= columns)
            {
                col = 0;
                row++;
            }
        }

        // Geri butonu
        float bottomY = -((row + 1) * (rowH + spacing)) - 30;
        var backBtn = CreateButton(_contentRoot, "Geri",
            new Color(0.3f, 0.3f, 0.3f), Color.white, () =>
            {
                RebuildLobby();
                _titleText.text = "HOT-SEAT OYUN";
                _subtitleText.text = $"{_playerCount} oyuncu — sırayla oynayın";
            });
        var backRT = backBtn.GetComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0.5f, 1f);
        backRT.anchorMax = new Vector2(0.5f, 1f);
        backRT.pivot = new Vector2(0.5f, 1f);
        backRT.sizeDelta = new Vector2(300, 50);
        backRT.anchoredPosition = new Vector2(0, bottomY);
    }

    // ═══════════════════════════════════════════════════════════
    // OYUNU BAŞLAT
    // ═══════════════════════════════════════════════════════════
    private void StartHotSeatGame()
    {
        if (_tempPlayers.Count < _playerCount)
        {
            Debug.LogWarning($"[HotSeat] {_playerCount} oyuncunun hepsi ülke seçmedi! " +
                             $"Seçilen: {_tempPlayers.Count}");
            _subtitleText.text = $"⚠️ Lütfen tüm oyuncular için ülke seçin ({_tempPlayers.Count}/{_playerCount})";
            _subtitleText.color = new Color(0.9f, 0.4f, 0.4f);
            return;
        }

        // Index'leri düzelt
        for (int i = 0; i < _tempPlayers.Count; i++)
        {
            _tempPlayers[i].Index = i;
            _tempPlayers[i].PlayerName = $"Oyuncu {i + 1}";
        }

        uint seed = (uint)DateTime.UtcNow.Ticks;
        var session = SessionManager.CreateHotSeat(_tempPlayers, seed);
        if (session == null) return;

        SimLogger.Log($"[HotSeat] Oyun başlıyor: {_playerCount} oyuncu, seed {seed}");
        Close();
        _onSessionCreated?.Invoke(session);
    }

    // ═══════════════════════════════════════════════════════════
    // PASS SCREEN (Sıra sende)
    // ═══════════════════════════════════════════════════════════
    public void ShowPassScreen(PlayerSlot nextPlayer, Action onConfirmed)
    {
        _titleText.text = "SIRA DEĞİŞİYOR";
        _subtitleText.text = "Cihazı sıradaki oyuncuya verin";
        _subtitleText.color = new Color(0.65f, 0.72f, 0.85f);

        for (int i = _contentRoot.childCount - 1; i >= 0; i--)
            Destroy(_contentRoot.GetChild(i).gameObject);

        // Oyuncu adı
        var nameGO = new GameObject("NextPlayer", typeof(RectTransform));
        nameGO.transform.SetParent(_contentRoot, false);
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0.5f, 0.5f);
        nameRT.anchorMax = new Vector2(0.5f, 0.5f);
        nameRT.pivot = new Vector2(0.5f, 0.5f);
        nameRT.sizeDelta = new Vector2(900, 100);
        nameRT.anchoredPosition = new Vector2(0, 60);
        var nameText = nameGO.AddComponent<TextMeshProUGUI>();
        nameText.text = nextPlayer.PlayerName;
        nameText.fontSize = 64;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = new Color(0.95f, 0.72f, 0.30f);
        nameText.alignment = TextAlignmentOptions.Center;

        // Ülke
        var countryGO = new GameObject("Country", typeof(RectTransform));
        countryGO.transform.SetParent(_contentRoot, false);
        var countryRT = countryGO.GetComponent<RectTransform>();
        countryRT.anchorMin = new Vector2(0.5f, 0.5f);
        countryRT.anchorMax = new Vector2(0.5f, 0.5f);
        countryRT.pivot = new Vector2(0.5f, 0.5f);
        countryRT.sizeDelta = new Vector2(900, 60);
        countryRT.anchoredPosition = new Vector2(0, -10);
        var countryText = countryGO.AddComponent<TextMeshProUGUI>();
        countryText.text = nextPlayer.CountryName;
        countryText.fontSize = 36;
        countryText.color = Color.white;
        countryText.alignment = TextAlignmentOptions.Center;

        // HAZIRIM butonu
        var btn = CreateButton(_contentRoot, "HAZIRIM",
            new Color(0.30f, 0.80f, 0.50f), Color.white, () =>
            {
                Close();
                onConfirmed?.Invoke();
            });
        var btnRT = btn.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0.5f);
        btnRT.anchorMax = new Vector2(0.5f, 0.5f);
        btnRT.pivot = new Vector2(0.5f, 0.5f);
        btnRT.sizeDelta = new Vector2(400, 80);
        btnRT.anchoredPosition = new Vector2(0, -100);

        _panelRoot.gameObject.SetActive(true);
    }

    // ═══════════════════════════════════════════════════════════
    // YARDIMCILAR
    // ═══════════════════════════════════════════════════════════
    private Button CreateButton(Transform parent, string label, Color bg, Color fg, Action onClick)
    {
        var btnGO = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);
        var rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 50);

        var img = btnGO.GetComponent<Image>();
        img.color = bg;

        var btn = btnGO.GetComponent<Button>();
        btn.targetGraphic = img;

        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(btnGO.transform, false);
        var trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        var txt = textGO.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 22;
        txt.fontStyle = FontStyles.Bold;
        txt.color = fg;
        txt.alignment = TextAlignmentOptions.Center;

        if (onClick != null) btn.onClick.AddListener(() => onClick());
        return btn;
    }

    public void Close()
    {
        if (_panelRoot != null) _panelRoot.gameObject.SetActive(false);
    }

    public bool IsOpen => _panelRoot != null && _panelRoot.gameObject.activeSelf;
}