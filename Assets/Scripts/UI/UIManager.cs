using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DemocracySim.Engine.Core;
using DemocracySim.Engine.World;
using DemocracySim.Engine.Legislative;

// DemocracySim arayüzü. Sahnede hiçbir şey bağlamaya gerek yok: bu bileşen bütün ekranı kendi kurar.
// Düzen: üst çubuk (durum + sonraki tur) | sol sekmeler | orta sayfa | sağ haber/günlük | alt eylem çubuğu.
[DefaultExecutionOrder(-100)] // GameManager.Awake'ten önce kurulsun
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    // GameManager'ın dinleyici bağladığı butonlar (burada üretilir)
    [HideInInspector] public Button nextTurnButton;
    [HideInInspector] public Button intelButton;
    [HideInInspector] public Button sabotageButton;
    [HideInInspector] public Button manipulateButton;
    [HideInInspector] public Button closeDiplomacyButton;

    // Diplomasi hedef ülkesi değişince çağrılır (GameManager bağlar)
    public Action OnDiplomacyTargetChanged;

    class Chip { public TextMeshProUGUI Label; public TextMeshProUGUI Value; }

    class Tab
    {
        public string Key, Title;
        public Button Button;
        public RectTransform Page, Content;
        public bool Dirty = true;
        public Action<RectTransform, SimulationEngine> Builder;
    }

    // ---- Kök nesneler ----
    Canvas canvas;
    RectTransform hudRoot, selectionRoot, selectionGrid, centerRoot, modalLayer, toastLayer, actionBar;

    // ---- Üst çubuk ----
    TextMeshProUGUI roleText, titleText;
    Chip chipCapital, chipLegit, chipInfl, chipElection, chipCoup;

    // ---- Yan panel ----
    TextMeshProUGUI newsText;
    TextMeshProUGUI worldNewsText;   // FAZ 0
    RectTransform logContent;
    ScrollRect logScroll;
    int logCount = 0;

    // ---- Sekmeler ----
    readonly List<Tab> tabs = new List<Tab>();
    string currentTab = "genel";

    // ---- Dünya sayfası ----
    internal RectTransform diplomacyTargets, relationBarHolder;
    internal TextMeshProUGUI relationLabel, networkLabel;
    internal string selectedCountry = "";

    // ---- Önbellek ----
    internal Country lastCountry;
    internal List<SimPolicy> policyCache = new List<SimPolicy>();
    internal Action<string, float> onPolicyChange;
    internal Action<string> onPolicyAdvice;
    Dictionary<string, float> cur = new Dictionary<string, float>();
    Dictionary<string, float> baseline = new Dictionary<string, float>();
    int baselineTurn = -1;

    // ---- Modal kuyruğu ----
    class ModalReq { public float Width; public Action<RectTransform> Build; }
    readonly Queue<ModalReq> modalQueue = new Queue<ModalReq>();
    GameObject modalObj;

    // =====================================================================
    // KURULUM
    // =====================================================================
    void Awake()
    {
        Instance = this;

        // Sahnede bu nesne eski Canvas'ın altında olabilir: o Canvas gizlenince biz de kapanırdık.
        // Bu yüzden önce sahne köküne al.
        if (transform.parent != null) transform.SetParent(null, false);

        BuildCanvas();
        DisableLegacyCanvases();
        DefineTabs();
        BuildSelection();
        BuildHud();
        BuildModalLayers();
        SwitchTab("genel");
        hudRoot.gameObject.SetActive(false);
        selectionRoot.gameObject.SetActive(false);
        // EK-6: Motor event'lerini dinle
EventBus.Subscribe<CrisisTriggeredEvent>(OnCrisisTriggered);
EventBus.Subscribe<NotificationEvent>(OnNotification);
EventBus.Subscribe<AchievementUnlockedEvent>(OnAchievementUnlocked);
LocalizationManager.OnLanguageChanged += OnLanguageChanged;

if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)            Debug.LogWarning("[UI] Sahnede EventSystem yok; butonlar tıklanamaz. GameObject > UI > Event System ekleyin.");
    }

    void BuildCanvas()
    {
        var go = new GameObject("HUD_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var sc = go.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        sc.matchWidthOrHeight = 0.5f;
    }

    // Eski sahne arayüzünü (Canvas) otomatik gizler; elle silmeniz gerekmez
    void DisableLegacyCanvases()
    {
                foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (c == canvas || c.transform.IsChildOf(transform)) continue;
            if (c.isRootCanvas && c.name == "Canvas") c.gameObject.SetActive(false);
        }
    }

                void DefineTabs()
    {
        tabs.Add(new Tab { Key = "genel", Title = LocalizationManager.Get("tab_general"), Builder = new OverviewPresenter(this).Build });
tabs.Add(new Tab { Key = "yasalar", Title = LocalizationManager.Get("tab_policies"), Builder = new PoliciesPresenter(this).Build });
tabs.Add(new Tab { Key = "ekonomi", Title = LocalizationManager.Get("tab_economy"), Builder = new EconomyPresenter(this).Build });
tabs.Add(new Tab { Key = "halk", Title = LocalizationManager.Get("tab_people"), Builder = new PeoplePresenter(this).Build });
tabs.Add(new Tab { Key = "kabine", Title = LocalizationManager.Get("tab_cabinet"), Builder = new CabinetPresenter(this).Build });
tabs.Add(new Tab { Key = "dunya", Title = LocalizationManager.Get("tab_world"), Builder = null });
tabs.Add(new Tab { Key = "fraksiyonlar", Title = LocalizationManager.Get("tab_factions"), Builder = new FactionsPresenter(this).Build });
tabs.Add(new Tab { Key = "golge", Title = LocalizationManager.Get("tab_shadow"), Builder = new ShadowCabinetPresenter(this).Build });
tabs.Add(new Tab { Key = "meclis", Title = LocalizationManager.Get("tab_parliament"), Builder = new ParliamentPresenter(this).Build });
tabs.Add(new Tab { Key = "kararlar", Title = LocalizationManager.Get("tab_decisions"), Builder = new DecisionLogPresenter(this).Build });
tabs.Add(new Tab { Key = "cikar", Title = LocalizationManager.Get("tab_interests"), Builder = new InterestGroupsPresenter(this).Build });
tabs.Add(new Tab { Key = "basarim", Title = LocalizationManager.Get("tab_achievements"), Builder = BuildAchievementsPage });
    }

    // ---------------------------------------------------------------------
    // Ülke seçim ekranı
    // ---------------------------------------------------------------------
    void BuildSelection()
    {
        var bg = HudKit.Box(canvas.transform, "Selection", HudTheme.Bg, false);
        selectionRoot = bg.rectTransform;
        HudKit.Fill(selectionRoot);

        var col = HudKit.NewRect(selectionRoot, "Column");
        col.anchorMin = col.anchorMax = new Vector2(0.5f, 0.5f);
        col.pivot = new Vector2(0.5f, 0.5f);
        col.sizeDelta = new Vector2(1240, 0);
        HudKit.VStack(col.gameObject, 18, 0, TextAnchor.UpperCenter);
        var fit = col.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        HudKit.Label(col, "KAPITAL", 88, HudTheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold);
        HudKit.Label(col, LocalizationManager.Get("select_country"), 32, HudTheme.Dim, TextAlignmentOptions.Center);

        selectionGrid = HudKit.NewRect(col, "Grid");
        var grid = selectionGrid.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(380, 190);
        grid.spacing = new Vector2(24, 24);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.UpperCenter;
        HudKit.Size(selectionGrid.gameObject, prefH: 420);
    }
    void BuildAchievementsPage(RectTransform c, SimulationEngine e)
{
    if (e.Achievements == null)
    {
        HudKit.Label(c, "Başarım verisi yok.", 24, HudTheme.Dim);
        return;
    }

    var am = e.Achievements;

    HudKit.Label(c, "BAŞARIMLAR", 28, HudTheme.Gold, 
        TextAlignmentOptions.Left, FontStyles.Bold);
    var allIds = AchievementManager.AllAchievements.Select(a => a.Id);
int globalUnlocked = DemocracySim.Engine.Data.GlobalAchievementTracker.CountUnlocked(allIds);
HudKit.Label(c, $"Kazanılan: {globalUnlocked} / {am.TotalAchievements} (global)", 
    22, HudTheme.Info);

    // İlerleme barı
    float progress = am.TotalAchievements > 0 
        ? (float)am.TotalUnlocked / am.TotalAchievements 
        : 0f;
    HudKit.Bar(c, progress, HudTheme.Gold, 12f);

    foreach (var ach in AchievementManager.AllAchievements)
    {
        bool done = DemocracySim.Engine.Data.GlobalAchievementTracker.IsUnlocked(ach.Id)
                || am.Unlocked.Contains(ach.Id);

    var card = Card(c, null);
        var top = HudKit.NewRect(card, "Top");
        HudKit.HStack(top.gameObject, 10, 0, TextAnchor.MiddleLeft, false, true);

        // İkon
        var iconColor = done ? HudTheme.Gold : HudTheme.Dim;
        var iconLabel = HudKit.Label(top, ach.Icon, 32, iconColor, 
            TextAlignmentOptions.Center, FontStyles.Bold);
        HudKit.Size(iconLabel.gameObject, prefW: 40);

        // İsim + açıklama
        var info = HudKit.NewRect(top, "Info");
        HudKit.VStack(info.gameObject, 2, 0);
        HudKit.Size(info.gameObject, flexW: 1);

        Color nameColor = done ? HudTheme.Gold : HudTheme.Dim;
        HudKit.Label(info, ach.Name, 24, nameColor, 
            TextAlignmentOptions.Left, FontStyles.Bold);
        HudKit.Label(info, ach.Description, 18, HudTheme.Text);

        // Durum
        var status = HudKit.Label(top, done ? "KAZANILDI" : "KİLİTLİ", 
            18, done ? HudTheme.Good : HudTheme.Bad, 
            TextAlignmentOptions.Right, FontStyles.Bold);
        HudKit.Size(status.gameObject, prefW: 110);
    }
}
    public void ShowWorldNews(List<string> worldActions)
{
    if (worldNewsText == null) return;
    if (worldActions == null || worldActions.Count == 0)
    {
        worldNewsText.text = "Bugün dünya sahnesinde önemli bir gelişme yok.";
        return;
    }
    var sb = new StringBuilder();
    foreach (var action in worldActions.Take(6))
        sb.AppendLine("• " + Clean(action));
    worldNewsText.text = sb.ToString();
}

private void OnLanguageChanged()
{
    // Tüm sekmeleri dirty yap
    MarkAllDirty();
    RebuildCurrent();
    Debug.Log($"[UI] Dil değişti: {LocalizationManager.CurrentLanguage}");
}

private void OnCrisisTriggered(CrisisTriggeredEvent e)
{
    AudioManager.Instance?.PlayCrisisWarning();
    // İsterseniz burada UI bildirimi de gösterebilirsiniz
}

private void OnNotification(NotificationEvent e)
{
    Notify(e.Message, e.IsWarning);
}

private void OnAchievementUnlocked(AchievementUnlockedEvent e)
{
    Notify($"🏆 {e.Name}: {e.Description}", false);
}
    public void ShowCountrySelection(List<(string title, string subtitle)> items, Action<int> onPick)
    {
        HudKit.ClearChildren(selectionGrid);
        if (items == null) return;
        for (int i = 0; i < items.Count; i++)
        {
            int index = i;
            var img = HudKit.Box(selectionGrid, "Country_" + i, HudTheme.PanelHi);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            StyleButton(btn);
            HudKit.VStack(img.gameObject, 6, 18, TextAnchor.MiddleCenter);
            HudKit.Label(img.transform, Clean(items[i].title), 42, HudTheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            HudKit.Label(img.transform, Clean(items[i].subtitle), 23, HudTheme.Dim, TextAlignmentOptions.Center);
            btn.onClick.AddListener(() => onPick?.Invoke(index));
        }
    }

    // ---------------------------------------------------------------------
    // Oyun ekranı
    // ---------------------------------------------------------------------
    void BuildHud()
    {
        var bg = HudKit.Box(canvas.transform, "HUD", HudTheme.Bg, false);
        hudRoot = bg.rectTransform;
        HudKit.Fill(hudRoot);
        BuildTopBar();
        BuildNav();
        BuildPages();
        BuildSidebar();
        BuildActionBarShell();
    }

    void BuildTopBar()
    {
        var bar = HudKit.Box(hudRoot, "TopBar", HudTheme.Panel);
        HudKit.Place(bar.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -100), new Vector2(-16, -12));
        HudKit.HStack(bar.gameObject, 14, 10, TextAnchor.MiddleLeft, false, true);

        var id = HudKit.NewRect(bar.transform, "Identity");
        HudKit.VStack(id.gameObject, 2, 6, TextAnchor.MiddleLeft);
        HudKit.Size(id.gameObject, prefW: 400);
        roleText = HudKit.Label(id, "", 21, HudTheme.Good, TextAlignmentOptions.Left, FontStyles.Bold);
        titleText = HudKit.Label(id, "", 32, HudTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);

        chipCapital = MakeChip(bar.transform, LocalizationManager.Get("hud_capital"));
chipLegit = MakeChip(bar.transform, LocalizationManager.Get("hud_legit"));
chipInfl = MakeChip(bar.transform, LocalizationManager.Get("hud_inflation"));
chipElection = MakeChip(bar.transform, LocalizationManager.Get("hud_election"));
chipCoup = MakeChip(bar.transform, LocalizationManager.Get("hud_coup"));

        var spacer = HudKit.NewRect(bar.transform, "Spacer");
        HudKit.Size(spacer.gameObject, flexW: 1);

        nextTurnButton = HudKit.MakeButton(bar.transform, LocalizationManager.Get("next_turn").ToUpper(), HudTheme.Gold, HudTheme.Dark, 28, null, 68);
        HudKit.Size(nextTurnButton.gameObject, prefW: 250);
    }

    Chip MakeChip(Transform parent, string label)
    {
        var box = HudKit.Box(parent, "Chip", HudTheme.PanelHi);
        HudKit.VStack(box.gameObject, 0, 8, TextAnchor.MiddleLeft);
        HudKit.Size(box.gameObject, prefW: 172);
        var chip = new Chip();
        chip.Label = HudKit.Label(box.transform, label, 15, HudTheme.Dim, TextAlignmentOptions.Left, FontStyles.Bold);
        chip.Value = HudKit.Label(box.transform, "-", 28, HudTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);
        return chip;
    }

    void BuildNav()
    {
        var nav = HudKit.Box(hudRoot, "Nav", HudTheme.Panel);
        HudKit.Place(nav.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(16, 96), new Vector2(236, -112));
        HudKit.VStack(nav.gameObject, 8, 12, TextAnchor.UpperCenter);
        foreach (var t in tabs)
        {
            string key = t.Key;
            t.Button = HudKit.MakeButton(nav.transform, t.Title, HudTheme.PanelHi, HudTheme.Text, 23, () => SwitchTab(key), 64);
        }
    }

    void BuildPages()
    {
        var center = HudKit.Box(hudRoot, "Center", HudTheme.Panel);
        HudKit.Place(center.rectTransform, Vector2.zero, Vector2.one, new Vector2(248, 96), new Vector2(-428, -112));
        centerRoot = center.rectTransform;
        foreach (var t in tabs)
        {
            RectTransform content;
            var page = HudKit.ScrollView(centerRoot, "Page_" + t.Key, out content);
            HudKit.Fill(page, 4, 4, 4, 4);
            t.Page = page;
            t.Content = content;
        }
        new WorldPresenter(this).Build(tabs.First(x => x.Key == "dunya").Content, null);
    }

       void BuildSidebar()
    {
        var side = HudKit.Box(hudRoot, "Sidebar", HudTheme.Panel);
        HudKit.Place(side.rectTransform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-428, 96), new Vector2(-16, -112));
        HudKit.VStack(side.gameObject, 10, 14);

        // FAZ 0: Dünya haberleri (AI ülkelerden)
        HudKit.Label(side.transform, "DÜNYA HABERLERİ", 19, HudTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);
        var worldNewsBox = HudKit.Box(side.transform, "WorldNews", HudTheme.PanelHi);
        HudKit.Size(worldNewsBox.gameObject, prefH: 150, minH: 150);
        worldNewsText = HudKit.Label(worldNewsBox.transform, "Henüz dünya haberi yok.", 17, HudTheme.Text);
        HudKit.Fill(worldNewsText.rectTransform, 10, 8, 10, 8);
        worldNewsText.overflowMode = TextOverflowModes.Ellipsis;

        // Günün haberleri
        HudKit.Label(side.transform, "GÜNÜN HABERLERİ", 19, HudTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);
        var newsBox = HudKit.Box(side.transform, "News", HudTheme.PanelHi);
        HudKit.Size(newsBox.gameObject, prefH: 300, minH: 300);
        newsText = HudKit.Label(newsBox.transform, "Henüz haber yok.", 20, HudTheme.Text);
        HudKit.Fill(newsText.rectTransform, 12, 10, 12, 10);
        newsText.overflowMode = TextOverflowModes.Ellipsis;

        // Olay günlüğü
        HudKit.Label(side.transform, "OLAY GÜNLÜĞÜ", 19, HudTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);
        var logRoot = HudKit.ScrollView(side.transform, "Log", out logContent, 8, 6);
        logScroll = logRoot.GetComponent<ScrollRect>();
        HudKit.Size(logRoot.gameObject, flexH: 1, minH: 200);
    }

    void BuildActionBarShell()
    {
        var bar = HudKit.Box(hudRoot, "ActionBar", HudTheme.Panel);
        HudKit.Place(bar.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 12), new Vector2(-16, 84));
        actionBar = bar.rectTransform;
        HudKit.HStack(bar.gameObject, 12, 8, TextAnchor.MiddleCenter, true, true);
    }

    void BuildModalLayers()
    {
        modalLayer = HudKit.NewRect(canvas.transform, "Modals");
        HudKit.Fill(modalLayer);

            var blocker = HudKit.NewRect(modalLayer, "Blocker");
    HudKit.Fill(blocker);
    var blockerImg = blocker.gameObject.AddComponent<Image>();
    blockerImg.color = new Color(0, 0, 0, 0);   // Başta görünmez
    blockerImg.raycastTarget = true;            // Tıklamaları yakalasın
    blocker.gameObject.SetActive(false);

        toastLayer = HudKit.NewRect(canvas.transform, "Toasts");
        toastLayer.anchorMin = toastLayer.anchorMax = new Vector2(0.5f, 1f);
        toastLayer.pivot = new Vector2(0.5f, 1f);
        toastLayer.anchoredPosition = new Vector2(0, -122);
        toastLayer.sizeDelta = new Vector2(860, 0);
        HudKit.VStack(toastLayer.gameObject, 8, 0, TextAnchor.UpperCenter);
        var fit = toastLayer.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    // Dünya / istihbarat sayfası (sabit; her turda yeniden kurulmaz)
       // FAZ 0: Seçili ülke detayları için alan tutucular
    internal RectTransform countryDetailHolder;


    // =====================================================================
    // GameManager'ın kullandığı genel arayüz (eski UIManager ile uyumlu)
    // =====================================================================
    public void ShowPanel(string panelName)
    {
        if (panelName == "Selection")
        {
            selectionRoot.gameObject.SetActive(true);
            hudRoot.gameObject.SetActive(false);
        }
        else if (panelName == "Gameplay")
        {
            selectionRoot.gameObject.SetActive(false);
            hudRoot.gameObject.SetActive(true);
            RebuildCurrent();
        }
    }

    public void SwitchTab(string tabKey)
    {
        currentTab = tabKey;
        foreach (var t in tabs)
        {
            bool on = t.Key == tabKey;
            if (t.Page != null) t.Page.gameObject.SetActive(on);
            if (t.Button != null)
            {
                t.Button.GetComponent<Image>().color = on ? HudTheme.Gold : HudTheme.PanelHi;
                var txt = t.Button.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.color = on ? HudTheme.Dark : HudTheme.Text;
            }
        }
        RebuildCurrent();
    }

    public void ToggleDiplomacy() { SwitchTab(currentTab == "dunya" ? "genel" : "dunya"); }
        // FAZ 3: Aksiyon callback'lerini sakla (rol değişince yeniden kullan)
    Action _onNewPolicy, _onLobby, _onState, _onRally, _onFoundParty, _onCandidateCampaign, _onMinistryMarket;
    bool _lastOpposition = false;

       public void CreateActionButtons(Action onNewPolicy, Action onLobby, Action onState, Action onRally,
    Action onFoundParty = null, Action onCandidateCampaign = null, Action onMinistryMarket = null)
{
    _onNewPolicy = onNewPolicy;
    _onLobby = onLobby;
    _onState = onState;
    _onRally = onRally;
    _onFoundParty = onFoundParty;
    _onCandidateCampaign = onCandidateCampaign;
    _onMinistryMarket = onMinistryMarket;
    RefreshActionBar(false);
}

    /// <summary>FAZ 3: Role göre aksiyon butonlarını yenile.</summary>
    public void RefreshActionBar(bool isOpposition)
    {
        if (_lastOpposition == isOpposition && actionBar.childCount > 0) return;
        _lastOpposition = isOpposition;

        HudKit.ClearChildren(actionBar);
        if (isOpposition)
{
    AddAction(LocalizationManager.Get("opp_center"), HudTheme.Action, _onState);
AddAction(LocalizationManager.Get("donation_campaign"), HudTheme.Action, _onLobby);
AddAction(LocalizationManager.Get("tab_shadow"), HudTheme.Action, () => SwitchTab("golge"));
AddAction(LocalizationManager.Get("protest"), HudTheme.Hex("8B2A2A"), _onRally);

    // EK-24: Adaylık krizi varsa önce onu göster
    bool inCrisis = lastCountry?.Engine != null
                    && CandidateManager.InCandidateCrisis(lastCountry.Engine);
        if (inCrisis)
    {
        bool canRun = CandidateManager.CanRun(lastCountry.Engine);
        string label = canRun ? "Adaylık Kampanyası" : "ADAYLIK KRIZI (UYARI)";
        Color bg = canRun ? HudTheme.Action : HudTheme.Bad;
        AddAction(label, bg, _onCandidateCampaign);
    }
    else
    {
        AddAction("Parti Kur", HudTheme.Gold, _onFoundParty);
    }

    AddAction("Protesto Düzenle", HudTheme.Hex("8B2A2A"), _onRally);
}
        else
{
   AddAction(LocalizationManager.Get("new_policy"), HudTheme.Action, _onNewPolicy);
AddAction(LocalizationManager.Get("ministry_market"), HudTheme.Info, _onMinistryMarket);
AddAction(LocalizationManager.Get("gov_center"), HudTheme.Action, _onState);
AddAction(LocalizationManager.Get("rally"), HudTheme.Hex("7A5A12"), _onRally);
}
    }

    void AddAction(string label, Color bg, Action a)
    {
        var b = HudKit.MakeButton(actionBar, label, bg, Color.white, 25, a, 56);
        HudKit.Size(b.gameObject, flexW: 1);
    }

    public void UpdateDashboard(Country playerCountry, int turn)
    {
        if (playerCountry == null) return;
        lastCountry = playerCountry;
        var e = playerCountry.Engine;
        bool opp = e.CurrentRole == SimulationEngine.PlayerRole.Opposition;

        // Önceki turla karşılaştırma için referans değerler
        if (turn != baselineTurn)
        {
            if (cur.Count > 0) baseline = new Dictionary<string, float>(cur);
            baselineTurn = turn;
        }
        cur = Snapshot(e);

        roleText.text = opp ? Colored("MUHALEFET", HudTheme.Bad) : Colored("HÜKÜMET", HudTheme.Good);
        titleText.text = Clean(playerCountry.Name) + "   |   " + 
                 DemocracySim.Engine.Core.PlayerProfile.PlayerName + 
                 "   |   Tur " + turn;

        float legit = e.Legitimacy.CurrentLegitimacy;
        chipCapital.Value.text = e.PoliticalCapital.ToString("F0");
        chipLegit.Label.text = opp ? "HALK DESTEĞİ" : "MEŞRUİYET";
        chipLegit.Value.text = legit.ToString("F1") + "%";
        chipLegit.Value.color = GoodHigh(legit, 55f, 35f);
        chipInfl.Value.text = "%" + e.Economy.Inflation.ToString("F1");
        chipInfl.Value.color = GoodLow(e.Economy.Inflation, 6f, 15f);
        chipElection.Label.text = opp ? "İKTİDARA KALAN" : "SEÇİME KALAN";
        chipElection.Value.text = e.TurnUntilElection + " tur";
        float coup = e.Army.CoupRiskPercent;
        chipCoup.Value.text = opp ? "%" + coup.ToString("F0") : e.Army.CoupRiskLabel;
        chipCoup.Value.color = GoodLow(coup, 20f, 50f);

        RefreshActionBar(opp);

        MarkAllDirty();
        RebuildCurrent();
    }

    public void RefreshDemographics(List<DemographicGroup> groups)
    {
        MarkDirty("genel");
        MarkDirty("halk");
    }

    public void RefreshPolicyList(List<SimPolicy> policies, Action<string, float> onValueChange, Action<string> onAdviceRequested = null)
    {
        policyCache = policies ?? new List<SimPolicy>();
        onPolicyChange = onValueChange;
        onPolicyAdvice = onAdviceRequested;
        MarkDirty("yasalar");
        RebuildCurrent();
    }

    // Eski API uyumu: kabine ve anketler artık UpdateDashboard ile motordan okunuyor
    public void RefreshCabinet(List<PoliticalActor> ministers) { MarkDirty("kabine"); RebuildCurrent(); }
    public void RefreshPolls(Dictionary<string, float> polls) { }

    public void ShowNews(List<NewsArticle> news)
    {
        if (newsText == null) return;
        if (news == null || news.Count == 0) { newsText.text = "Bugün önemli bir haber yok."; return; }
        var sb = new StringBuilder();
        foreach (var n in news)
        {
            Color c = n.ImpactOnLegitimacy >= 0 ? HudTheme.Good : HudTheme.Bad;
            sb.AppendLine(Colored(n.ImpactOnLegitimacy.ToString("+0.0;-0.0"), c) + "  " + Clean(n.Headline));
        }
        newsText.text = sb.ToString();
    }

    public void WriteLog(string message)
    {
        if (logContent == null) return;
        string text = Clean(message);
        if (string.IsNullOrEmpty(text)) return;

        var label = HudKit.Label(logContent, "> " + text, 19, LogColor(text));
        logCount++;
        if (logCount > 120 && logContent.childCount > 0)
        {
            var first = logContent.GetChild(0).gameObject;
            first.SetActive(false);
            Destroy(first);
            logCount--;
        }
        if (isActiveAndEnabled) StartCoroutine(ScrollLogToBottom());
    }

    IEnumerator ScrollLogToBottom()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (logScroll != null) logScroll.verticalNormalizedPosition = 0f;
    }

    static Color LogColor(string t)
    {
        string[] bad = { "İFŞA", "REDDEDİL", "KAYBET", "DARBE", "KRİZ", "İSYAN", "YAPTIRIM", "OYUN BİTTİ", "Yeterli sermaye yok" };
        string[] good = { "ZAFER", "GEÇTİ", "BAŞARI", "KAZAN" };
        foreach (var b in bad) if (t.Contains(b)) return HudTheme.Bad;
        foreach (var g in good) if (t.Contains(g)) return HudTheme.Good;
        return HudTheme.Text;
    }

    public void Notify(string message, bool isWarning = false)
    {
        if (toastLayer == null) return;
        string text = Clean(message);
        if (string.IsNullOrEmpty(text)) return;

        var box = HudKit.Box(toastLayer, "Toast", isWarning ? HudTheme.Hex("5A1F22") : HudTheme.Hex("1E3A5F"));
        box.raycastTarget = false;
        HudKit.VStack(box.gameObject, 0, 14, TextAnchor.MiddleCenter);
        HudKit.Label(box.transform, text, 23, Color.white, TextAlignmentOptions.Center);

        while (toastLayer.childCount > 4)
        {
            var old = toastLayer.GetChild(0).gameObject;
            old.SetActive(false);
            Destroy(old);
        }
        if (isActiveAndEnabled) StartCoroutine(ToastLife(box.gameObject));
    }

    IEnumerator ToastLife(GameObject go)
    {
        yield return new WaitForSecondsRealtime(4.5f);
        if (go != null) { go.SetActive(false); Destroy(go); }
    }

    // ---- Diplomasi ----
    public void PopulateCountryDropdown(List<Country> countries, Country playerCountry)
    {
        HudKit.ClearChildren(diplomacyTargets);
        selectedCountry = "";
        if (countries == null) return;
        var items = new List<(string key, string label)>();
        foreach (var c in countries)
        {
            if (playerCountry != null && c.Id == playerCountry.Id) continue;
            items.Add((c.Name, c.Name));
        }
        if (items.Count == 0) return;
        selectedCountry = items[0].key;
        ChipGroup(diplomacyTargets, items, selectedCountry, k =>
        {
            selectedCountry = k;
            if (OnDiplomacyTargetChanged != null) OnDiplomacyTargetChanged();
        }, true, 22f, 56f);
    }

    public string GetSelectedCountryName() { return selectedCountry; }

       public void UpdateDiplomacyInfo(float relationValue, float networkStrength)
    {
        if (relationLabel == null) return;
        Color col = relationValue >= 20f ? HudTheme.Good : (relationValue <= -20f ? HudTheme.Bad : HudTheme.Warn);
        relationLabel.text = "İlişki: " + Colored((relationValue >= 0 ? "+" : "") + relationValue.ToString("F0"), col);
        networkLabel.text = "Casus Ağı Gücü: %" + networkStrength.ToString("F0");
        HudKit.ClearChildren(relationBarHolder);
        HudKit.Bar(relationBarHolder, (relationValue + 100f) / 200f, col, 12f);

        // FAZ 0: Seçili AI ülkenin iç durumu
        new CountryDetailPresenter(this).Populate();
    }


    // FAZ 0: GameManager set edecek
    [System.NonSerialized] public Country selectedCountryDetail;

    // FAZ 0: AI ülkenin iç durumu + son eylemleri
    RectTransform countryDetailsHolder;
    
    void EnsureCountryDetailsHolder()
    {
        if (countryDetailsHolder != null) return;
        // World page içinde ülke detayları için bir alan yarat
        if (networkLabel == null || networkLabel.transform.parent == null) return;
        var parent = networkLabel.transform.parent;
        countryDetailsHolder = HudKit.NewRect(parent, "CountryDetails");
        HudKit.VStack(countryDetailsHolder.gameObject, 6, 0);
    }

    void UpdateSelectedCountryDetails()
    {
        if (lastCountry == null) return;
        // Seçili ülkeyi al
        string targetName = selectedCountry;
        if (string.IsNullOrEmpty(targetName)) return;
        
        // World sayfasında, ülkenin iç durumunu göster
        var worldPage = tabs.Find(x => x.Key == "dunya");
        if (worldPage == null || worldPage.Content == null) return;
        
        // Basit yaklaşım: Son ülke detaylarını ayrı bir kartta göster
        // (Şimdilik networkLabel altında inline)
    }

    // =====================================================================
    // POPUP'LAR (kuyruklu: aynı anda yalnızca biri görünür)
    // =====================================================================
    internal void OpenModal(float width, Action<RectTransform> build)
    {
        modalQueue.Enqueue(new ModalReq { Width = width, Build = build });
        if (modalObj == null) ShowNextModal();
    }

    void ShowNextModal()
    {
            if (modalObj != null || modalQueue.Count == 0) return;
    var req = modalQueue.Dequeue();

    // EK-FIX: %98 opak arka plan (önceden %82 şeffaftı, arkadaki içerik görünüyordu)
    var dim = HudKit.Box(modalLayer, "Modal", new Color(0.02f, 0.04f, 0.09f, 0.98f), false);
    HudKit.Fill(dim.rectTransform);
    dim.raycastTarget = true;   // Tıklamalar arkaya geçmesin
    modalObj = dim.gameObject;

        var card = HudKit.Box(dim.transform, "Card", HudTheme.PanelSolid);
        var rt = card.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(req.Width, 0);
        HudKit.VStack(rt.gameObject, 14, 30, TextAnchor.UpperCenter);
        var fit = rt.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        req.Build(rt);
    }

    public void CloseModal()
    {
        if (modalObj != null)
        {
            modalObj.SetActive(false);
            Destroy(modalObj);
            modalObj = null;
        }
        ShowNextModal();
    }

    public void HideChoicePopup() { CloseModal(); }

    Button ModalButton(RectTransform card, string label, Color bg, Color fg, Action onClick)
    {
        return HudKit.MakeButton(card, Clean(label), bg, fg, 24, onClick, 58);
    }

    public void ShowChoicePopup(string title, List<(string label, Action onClick)> options, bool allowCancel = true, string closeLabel = "Vazgeç")
    {
        OpenModal(780, card =>
        {
            var lines = Clean(title).Split('\n');
            HudKit.Label(card, lines[0], 38, HudTheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            for (int i = 1; i < lines.Length; i++)
                if (!string.IsNullOrWhiteSpace(lines[i]))
                    HudKit.Label(card, lines[i].Trim(), 24, HudTheme.Text, TextAlignmentOptions.Center);

            if (options != null)
            {
                foreach (var opt in options)
                {
                    var o = opt;
                    ModalButton(card, o.label, HudTheme.PanelHi, HudTheme.Text, () => { CloseModal(); if (o.onClick != null) o.onClick(); });
                }
            }
            if (allowCancel) ModalButton(card, closeLabel, HudTheme.Line, HudTheme.Dim, CloseModal);
        });
    }

/// <summary>
/// EK-22: Oyuncu isim belirleme modal'ı.
/// Senaryo seçildikten sonra çağrılır.
/// </summary>
public void ShowPlayerNamePrompt(Action<string> onConfirm)
{
    OpenModal(600, card =>
    {
        HudKit.Label(card, "ADINIZ", 38, HudTheme.Gold,
            TextAlignmentOptions.Center, FontStyles.Bold);
        HudKit.Label(card, "Ülkeyi yönetecek liderin adı ne olsun?",
            22, HudTheme.Dim, TextAlignmentOptions.Center);

        // Input field
        var inputGO = HudKit.NewRect(card, "NameInput");
        HudKit.Size(inputGO.gameObject, prefH: 56, minH: 56);
        var inputBg = HudKit.Box(inputGO, "Bg", HudTheme.PanelHi);
        HudKit.Fill(inputBg.rectTransform);
        var inputField = inputGO.gameObject.AddComponent<TMP_InputField>();

        var textArea = HudKit.NewRect(inputGO, "TextArea");
        HudKit.Fill(textArea, 12, 8, 12, 8);
        var placeholder = HudKit.Label(textArea, "Örn: Ahmet Yılmaz",
            22, HudTheme.Dim, TextAlignmentOptions.Left);
        placeholder.fontStyle = FontStyles.Italic;
        var text = HudKit.Label(textArea, "", 22, HudTheme.Text,
            TextAlignmentOptions.Left);
        inputField.textViewport = textArea;
        inputField.textComponent = text;
        inputField.placeholder = placeholder;
        inputField.characterLimit = 30;

        string selectedName = "";
        inputField.onValueChanged.AddListener(v => selectedName = v);

        ModalButton(card, "BAŞLA", HudTheme.Gold, HudTheme.Dark, () =>
        {
            string finalName = string.IsNullOrWhiteSpace(selectedName)
                ? "Başkan" : selectedName.Trim();
            CloseModal();
            onConfirm?.Invoke(finalName);
        });
    });
}
/// <summary>
/// EK-23: Parti kurma modal'ı — isim, slogan, ideoloji.
/// </summary>
public void ShowPartyFoundPrompt(string reason, Action<string, string, float> onConfirm)
{
    OpenModal(680, card =>
    {
        HudKit.Label(card, "PARTİ KUR", 38, HudTheme.Gold,
            TextAlignmentOptions.Center, FontStyles.Bold);
        HudKit.Label(card, $"Sebep:\n{reason}", 20, HudTheme.Warn,
            TextAlignmentOptions.Center);
        HudKit.Label(card, $"Maliyet: {PartyFounder.FoundCost:F0} sermaye + {PartyFounder.FundCost:F0} fon",
            22, HudTheme.Dim, TextAlignmentOptions.Center);

        // Parti adı
        HudKit.Label(card, "PARTİ ADI", 20, HudTheme.Gold,
            TextAlignmentOptions.Left, FontStyles.Bold);
        var nameInputGO = HudKit.NewRect(card, "PartyName");
        HudKit.Size(nameInputGO.gameObject, prefH: 50, minH: 50);
        var nameBg = HudKit.Box(nameInputGO, "Bg", HudTheme.PanelHi);
        HudKit.Fill(nameBg.rectTransform);
        var nameField = nameInputGO.gameObject.AddComponent<TMP_InputField>();
        var nameArea = HudKit.NewRect(nameInputGO, "TextArea");
        HudKit.Fill(nameArea, 12, 6, 12, 6);
        var namePlaceholder = HudKit.Label(nameArea, "Örn: Halkın Sesi Partisi",
            20, HudTheme.Dim, TextAlignmentOptions.Left);
        namePlaceholder.fontStyle = FontStyles.Italic;
        var nameText = HudKit.Label(nameArea, "", 20, HudTheme.Text,
            TextAlignmentOptions.Left);
        nameField.textViewport = nameArea;
        nameField.textComponent = nameText;
        nameField.placeholder = namePlaceholder;
        nameField.characterLimit = 40;
        string pName = "";
        nameField.onValueChanged.AddListener(v => pName = v);

        // Slogan
        HudKit.Label(card, "SLOGAN", 20, HudTheme.Gold,
            TextAlignmentOptions.Left, FontStyles.Bold);
        var sloganInputGO = HudKit.NewRect(card, "Slogan");
        HudKit.Size(sloganInputGO.gameObject, prefH: 50, minH: 50);
        var sloganBg = HudKit.Box(sloganInputGO, "Bg", HudTheme.PanelHi);
        HudKit.Fill(sloganBg.rectTransform);
        var sloganField = sloganInputGO.gameObject.AddComponent<TMP_InputField>();
        var sloganArea = HudKit.NewRect(sloganInputGO, "TextArea");
        HudKit.Fill(sloganArea, 12, 6, 12, 6);
        var sloganPlaceholder = HudKit.Label(sloganArea, "Örn: Değişim şimdi!",
            20, HudTheme.Dim, TextAlignmentOptions.Left);
        sloganPlaceholder.fontStyle = FontStyles.Italic;
        var sloganText = HudKit.Label(sloganArea, "", 20, HudTheme.Text,
            TextAlignmentOptions.Left);
        sloganField.textViewport = sloganArea;
        sloganField.textComponent = sloganText;
        sloganField.placeholder = sloganPlaceholder;
        sloganField.characterLimit = 60;
        string pSlogan = "";
        sloganField.onValueChanged.AddListener(v => pSlogan = v);

        // İdeoloji slider
        HudKit.Label(card, "İDEOLOJİ (Sol ←→ Sağ)", 20, HudTheme.Gold,
            TextAlignmentOptions.Left, FontStyles.Bold);
        var sliderGO = HudKit.NewRect(card, "IdeologySlider");
        HudKit.Size(sliderGO.gameObject, prefH: 40, minH: 40);
        var sliderBg = HudKit.Box(sliderGO, "Bg", HudTheme.PanelHi);
        HudKit.Fill(sliderBg.rectTransform);
        var slider = sliderGO.gameObject.AddComponent<Slider>();
        // Slider'ı basitçe yapalım — 3 buton ile seçim
        float[] ideologies = { -70f, -30f, 0f, 30f, 70f };
        string[] labels = { "Sol", "Merkez-Sol", "Merkez", "Merkez-Sağ", "Sağ" };
        float selectedIdeo = 0f;
        var idRow = HudKit.NewRect(card, "IdeologyRow");
        HudKit.HStack(idRow.gameObject, 6, 0, TextAnchor.MiddleCenter, true, true);
        foreach (var (ideo, lbl) in System.Linq.Enumerable.Zip(ideologies, labels, (i, l) => (i, l)))
        {
            float captured = ideo;
            var btn = HudKit.MakeButton(idRow, lbl, HudTheme.Panel, HudTheme.Text, 16, () =>
            {
                selectedIdeo = captured;
            }, 40);
            HudKit.Size(btn.gameObject, flexW: 1);
        }

        ModalButton(card, "PARTİYİ KUR", HudTheme.Gold, HudTheme.Dark, () =>
        {
            string finalName = string.IsNullOrWhiteSpace(pName) ? "Yeni Parti" : pName.Trim();
            string finalSlogan = string.IsNullOrWhiteSpace(pSlogan) ? "" : pSlogan.Trim();
            CloseModal();
            onConfirm?.Invoke(finalName, finalSlogan, selectedIdeo);
        });
        ModalButton(card, "Vazgeç", HudTheme.Line, HudTheme.Dim, CloseModal);
    });
}

    /// <summary>FAZ 4: Senaryo seçimi için özel modal — kartlar halinde gösterir.</summary>
public void ShowScenarioPicker(List<DemocracySim.Engine.World.Scenario> scenarios, Action<DemocracySim.Engine.World.Scenario> onPick)
{
    if (scenarios == null || scenarios.Count == 0) return;

    OpenModal(720, card =>
    {
        HudKit.Label(card, "SENARYO SEÇ", 38, HudTheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold);
        HudKit.Label(card, "Zorluk seviyesi ve başlangıç koşullarını belirle.", 20, HudTheme.Dim, TextAlignmentOptions.Center);

        foreach (var sc in scenarios)
        {
            var scenario = sc;
            var scCard = HudKit.Box(card, "ScenarioCard", HudTheme.PanelHi);
            HudKit.VStack(scCard.gameObject, 6, 14, TextAnchor.UpperLeft);
            HudKit.Size(scCard.gameObject, minH: 130);

            // Zorluk etiketi
            Color diffColor = scenario.Difficulty switch
            {
                "Kolay"     => HudTheme.Good,
                "Normal"    => HudTheme.Info,
                "Zor"       => HudTheme.Warn,
                "Çok Zor"   => HudTheme.Bad,
                _           => HudTheme.Dim
            };
            HudKit.Label(scCard.transform, $"[{scenario.Difficulty}]", 18, diffColor, 
                TextAlignmentOptions.Left, FontStyles.Bold);

            // Başlık
            HudKit.Label(scCard.transform, scenario.Name, 26, HudTheme.Text, 
                TextAlignmentOptions.Left, FontStyles.Bold);

            // Açıklama
            HudKit.Label(scCard.transform, scenario.Description, 19, HudTheme.Dim);

            // Özel not
            if (!string.IsNullOrEmpty(scenario.SpecialNote))
            {
                HudKit.Label(scCard.transform, "! " + scenario.SpecialNote, 18, HudTheme.Warn);
            }

            // Seç butonu
            var btn = HudKit.MakeButton(scCard.transform, "Bu Senaryoyu Seç", 
                HudTheme.Action, Color.white, 20, () =>
            {
                CloseModal();
                onPick?.Invoke(scenario);
            }, 48);
            HudKit.Size(btn.gameObject, prefH: 48);
        }

        // İptal butonu
        HudKit.MakeButton(card, "Vazgeç", HudTheme.Line, HudTheme.Dim, 22, CloseModal, 56);
    });
}

    public void DisplayEvent(SimulationEngine engine, GameEvent gameEvent, Action<EventChoice> onChoiceSelected)
    {
        if (gameEvent == null) return;
        OpenModal(840, card =>
        {
            HudKit.Label(card, Clean(gameEvent.Title), 40, HudTheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            HudKit.Label(card, Clean(gameEvent.Description), 25, HudTheme.Text, TextAlignmentOptions.Center);

            if (gameEvent.IsNotificationOnly)
            {
                ModalButton(card, "Tamam", HudTheme.Gold, HudTheme.Dark, () => { CloseModal(); if (onChoiceSelected != null) onChoiceSelected(null); });
                return;
            }
            foreach (var ch in gameEvent.Choices)
            {
                var choice = ch;
                ModalButton(card, choice.ChoiceText, HudTheme.PanelHi, HudTheme.Text, () => { CloseModal(); if (onChoiceSelected != null) onChoiceSelected(choice); });
            }
        });
    }

    // Yasa önerirken söylem (frame) seçimi
    public void ShowFramePicker(string policyName, float amount, Action<string> onPick)
    {
        OpenModal(760, card =>
        {
            HudKit.Label(card, "Söylem Seçin", 38, HudTheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            HudKit.Label(card, Clean(policyName) + ":  " + amount.ToString("+0;-0") + " birim", 26, HudTheme.Text, TextAlignmentOptions.Center);
            HudKit.Label(card, "Söylem, yasanın ideolojik konumunu kaydırır ve meclis oylamasını etkiler.", 20, HudTheme.Dim, TextAlignmentOptions.Center);
            string[] frames = { "Halk İçin", "Sermaye İçin", "Devlet İçin" };
            foreach (var f in frames)
            {
                var frame = f;
                ModalButton(card, frame, HudTheme.PanelHi, HudTheme.Text, () => { CloseModal(); if (onPick != null) onPick(frame); });
            }
            ModalButton(card, "Vazgeç", HudTheme.Line, HudTheme.Dim, CloseModal);
        });
    }

    public void ShowRallyPanel(List<DemographicGroup> groups, float cost, Action<string, string> onStart)
    {
        if (groups == null || groups.Count == 0) return;
        OpenModal(880, card =>
        {
            string selGroup = groups[0].Id;
            string selTheme = "Umut";
            TextMeshProUGUI desc = null;

            HudKit.Label(card, "SİYASİ MİTİNG", 40, HudTheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            HudKit.Label(card, "Maliyet: " + cost.ToString("F0") + " siyasi sermaye. Her miting seçim kampanyasına da yatırım sayılır.", 22, HudTheme.Dim, TextAlignmentOptions.Center);

            HudKit.Label(card, "HEDEF GRUP", 19, HudTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);
            var gi = new List<(string key, string label)>();
            foreach (var g in groups) gi.Add((g.Id, g.Name));
            ChipGroup(card, gi, selGroup, k => { selGroup = k; }, false, 19f, 66f);

            HudKit.Label(card, "TEMA", 19, HudTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);
            var themes = new List<(string key, string label)> { ("Umut", "Umut"), ("Öfke", "Öfke"), ("Güven", "Güven") };
            ChipGroup(card, themes, selTheme, k => { selTheme = k; if (desc != null) desc.text = ThemeDesc(k); }, false, 22f, 60f);
            desc = HudKit.Label(card, ThemeDesc(selTheme), 21, HudTheme.Text, TextAlignmentOptions.Center);

            ModalButton(card, "MİTİNGİ BAŞLAT", HudTheme.Gold, HudTheme.Dark, () => { CloseModal(); if (onStart != null) onStart(selGroup, selTheme); });
            ModalButton(card, "Vazgeç", HudTheme.Line, HudTheme.Dim, CloseModal);
        });
    }

    static string ThemeDesc(string theme)
    {
        switch (theme)
        {
            case "Öfke": return "Öfke: Etkisi büyüktür ama kutuplaştırır, huzursuzluğu artırır. Muhalefetteyken hükümete duyulan memnuniyeti düşürür.";
            case "Güven": return "Güven: İktidardayken meşruiyeti yükseltir. Muhalefetteyken kampanya yatırımını güçlendirir.";
            default: return "Umut: İktidardayken hedef grubun memnuniyetini artırır ve sokak huzursuzluğunu biraz düşürür.";
        }
    }

    public void ShowElectionResults(string winnerName, Dictionary<string, float> partyVotes, bool isPlayerWinner)
    {
        OpenModal(860, card =>
        {
            HudKit.Label(card, "SEÇİM SONUÇLARI", 40, HudTheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            HudKit.Label(card, isPlayerWinner ? "ZAFER" : "YENİLGİ", 64, isPlayerWinner ? HudTheme.Good : HudTheme.Bad, TextAlignmentOptions.Center, FontStyles.Bold);
            HudKit.Label(card, "Kazanan: " + Clean(winnerName), 27, HudTheme.Text, TextAlignmentOptions.Center);

            if (partyVotes != null)
            {
                foreach (var kv in partyVotes)
                {
                    bool mine = kv.Key.StartsWith("Sizin");
                    StatRow(card, Clean(kv.Key), "%" + kv.Value.ToString("F1"), kv.Value / 100f, mine ? HudTheme.Gold : HudTheme.Info, "");
                }
            }
            ModalButton(card, isPlayerWinner ? "Devleti Yönetmeye Devam Et" : "Muhalefet Görevine Başla", HudTheme.Gold, HudTheme.Dark, CloseModal);
        });
    }

    // =====================================================================
    // SAYFA İÇERİKLERİ
    // =====================================================================
    void MarkAllDirty() { foreach (var t in tabs) t.Dirty = true; }
    internal void MarkDirty(string key) { var t = tabs.Find(x => x.Key == key); if (t != null) t.Dirty = true; }

    internal void RebuildCurrent()
    {
        if (lastCountry == null) return;
        var t = tabs.Find(x => x.Key == currentTab);
        if (t == null || !t.Dirty || t.Builder == null || t.Content == null) return;
        HudKit.ClearChildren(t.Content);
        t.Builder(t.Content, lastCountry.Engine);
        t.Dirty = false;
    }



    internal string PolicyTags(SimulationEngine e, SimPolicy p)
    {
        var parts = new List<string>();
        parts.Add(p.IsActive ? Colored("AKTİF", HudTheme.Good) : "pasif");
        float a = p.IdeologicalAlignment;
        parts.Add(a < -20f ? "Sol" : (a > 20f ? "Sağ" : "Merkez"));
        if (e.ProposedPolicies.Contains(p)) parts.Add(Colored("MECLİS GÜNDEMİNDE", HudTheme.Warn));
        foreach (var pend in e.Universe.Pending)
            if (pend.PolicyId == p.Id) parts.Add(Colored("BÜROKRASİDE (" + pend.TurnsLeft + " tur)", HudTheme.Warn));
        return string.Join("   |   ", parts);
    }

    internal Button MiniButton(Transform parent, string label, Action a)
    {
        var b = HudKit.MakeButton(parent, label, HudTheme.Action, Color.white, 25, a, 56);
        HudKit.Size(b.gameObject, prefW: 66);
        return b;
    }




    // Yardımcı: HTML hex → Color
    internal Color HexToColor(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString(hex, out c);
        return c;
    }
    internal static string RoleTr(ActorRole r)
    {
        switch (r)
        {
            case ActorRole.Minister: return "Bakan";
            case ActorRole.OppositionLeader: return "Muhalefet lideri";
            case ActorRole.MP: return "Milletvekili";
            case ActorRole.Activist: return "Aktivist";
            case ActorRole.Oligarch: return "Oligark";
            default: return r.ToString();
        }
    }

    internal static string FactionTr(PoliticalFaction f)
    {
        switch (f)
        {
            case PoliticalFaction.Technocrat: return "Teknokrat";
            case PoliticalFaction.Ideologue: return "İdeolog";
            default: return "Sadık";
        }
    }

    internal static string TraitTr(ActorTrait t)
    {
        switch (t)
        {
            case ActorTrait.Ambitious: return "hırslı";
            case ActorTrait.Populist: return "popülist";
            case ActorTrait.Cautious: return "temkinli";
            case ActorTrait.Corrupt: return "yolsuz";
            case ActorTrait.LoyalistsHeart: return "sadık";
            default: return t.ToString();
        }
    }

    // =====================================================================
    // KÜÇÜK BİLEŞENLER
    // =====================================================================
    internal RectTransform Row(Transform parent, float spacing = 14f)
    {
        var r = HudKit.NewRect(parent, "Row");
        HudKit.HStack(r.gameObject, spacing, 0, TextAnchor.UpperLeft, true, true);
        return r;
    }

    internal RectTransform Card(Transform parent, string title, float prefH = -1f, float flexW = -1f)
    {
        var box = HudKit.Box(parent, "Card", HudTheme.PanelHi);
        HudKit.VStack(box.gameObject, 8, 16, TextAnchor.UpperLeft);
        HudKit.Size(box.gameObject, prefH: prefH, flexW: flexW);
        if (!string.IsNullOrEmpty(title))
            HudKit.Label(box.transform, title, 19, HudTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);
        return box.rectTransform;
    }

    internal void StatCard(Transform row, string label, string value, float v01, Color color, string delta)
    {
        var card = Card(row, null);
        HudKit.Label(card, label, 17, HudTheme.Dim, TextAlignmentOptions.Left, FontStyles.Bold);
        var valueLabel = HudKit.Label(card, value, 40, color, TextAlignmentOptions.Left, FontStyles.Bold);
        HudKit.Bar(card, v01, color, 10f);
        var d = HudKit.Label(card, string.IsNullOrEmpty(delta) ? " " : delta + " son turdan", 17, HudTheme.Dim);
        d.richText = true;
    }

   internal void StatRow(Transform parent, string name, string value, float v01, Color color, string delta)
{
    StatRow(parent, name, value, v01, color, delta, null);
}

    // FAZ 1: Trend göstergeli StatRow overload
        // FAZ 1: Trend göstergeli StatRow overload + tıklanabilirlik
    internal void StatRow(Transform parent, string name, string value, float v01, Color color, string delta, SimObject sourceObj)
    {
        var wrap = HudKit.NewRect(parent, "StatRow");
        HudKit.VStack(wrap.gameObject, 4, 0);

        var top = HudKit.NewRect(wrap, "Top");
        HudKit.HStack(top.gameObject, 8, 0, TextAnchor.MiddleLeft, false, true);
        var n = HudKit.Label(top, name, 22, HudTheme.Text);
        HudKit.Size(n.gameObject, flexW: 1);
        var d = HudKit.Label(top, delta, 19, HudTheme.Dim, TextAlignmentOptions.Right);
        HudKit.Size(d.gameObject, prefW: 80);

        // FAZ 1: Trend oku
        if (sourceObj != null)
        {
            int trend = sourceObj.GetTrendDirection();
            if (trend != 0)
            {
                string arrow = trend > 0 ? "▲" : "▼";
                Color trendColor = trend > 0 ? HudTheme.Warn : HudTheme.Good;
                var trendLabel = HudKit.Label(top, arrow, 20, trendColor, TextAlignmentOptions.Center, FontStyles.Bold);
                HudKit.Size(trendLabel.gameObject, prefW: 24);
            }
            else
            {
                var trendLabel = HudKit.Label(top, "—", 20, HudTheme.Dim, TextAlignmentOptions.Center);
                HudKit.Size(trendLabel.gameObject, prefW: 24);
            }
        }

        var v = HudKit.Label(top, value, 22, color, TextAlignmentOptions.Right, FontStyles.Bold);
        HudKit.Size(v.gameObject, prefW: 130);

        // FAZ 1: Tıklanabilir detay butonu (sourceObj varsa)
        if (sourceObj != null)
        {
            var btn = HudKit.MakeButton(top, "?", HudTheme.Action, Color.white, 18, () =>
            {
                ShowObjectDetail(sourceObj);
            }, 32);
            HudKit.Size(btn.gameObject, prefW: 36);
        }

        HudKit.Bar(wrap, v01, color, 10f);

        // FAZ 1: Mini sparkline
        if (sourceObj != null && sourceObj.History.Count >= 5)
        {
            DrawSparkline(wrap, sourceObj.History, color);
        }
    }


    // FAZ 1: Mini sparkline grafiği
    /// <summary>FAZ 3.5: Gelişmiş sparkline — ortalama çizgisi + min/max etiketi.</summary>
internal void DrawSparkline(Transform parent, List<float> history, Color color)
{
    if (history == null || history.Count < 2) return;

    var holder = HudKit.NewRect(parent, "Sparkline");
    HudKit.Size(holder.gameObject, prefH: 20, minH: 20);

    var h = holder.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
    h.spacing = 1;
    h.padding = new RectOffset(0, 0, 0, 0);
    h.childAlignment = TextAnchor.LowerLeft;
    h.childControlWidth = false;
    h.childControlHeight = true;
    h.childForceExpandWidth = false;
    h.childForceExpandHeight = false;

    float min = history.Min();
    float max = history.Max();
    float avg = history.Average();
    float range = Math.Max(0.01f, max - min);

    int startIdx = Math.Max(0, history.Count - 20);
    for (int i = startIdx; i < history.Count; i++)
    {
        float norm = (history[i] - min) / range;
        float height = 3f + norm * 15f;

        // Ortalama çizgisinin altındaysa farklı renk
        Color barColor = history[i] < avg ? HudTheme.Bad : color;

        var bar = HudKit.Box(holder, "Bar", barColor);
        HudKit.Size(bar.gameObject, prefW: 4, prefH: height, minH: height);
        bar.raycastTarget = false;
    }

    // Min/Max/Avg etiketi
    var label = HudKit.Label(holder, $"min:{min:F0} max:{max:F0} ort:{avg:F0}", 
        14, HudTheme.Dim, TextAlignmentOptions.Right);
    HudKit.Size(label.gameObject, prefW: 180);
}

    void ChipGroup(Transform parent, IList<(string key, string label)> items, string initialKey, Action<string> onSelect,
               bool vertical, float fontSize = 21f, float height = 56f)
    {
        var holder = HudKit.NewRect(parent, "Chips");
        if (vertical) HudKit.VStack(holder.gameObject, 8, 0);
        else HudKit.HStack(holder.gameObject, 8, 0, TextAnchor.MiddleCenter, true, true);

        var buttons = new List<(string key, Image img, TextMeshProUGUI txt)>();
        Action<string> paint = sel =>
        {
            foreach (var b in buttons)
            {
                bool on = b.key == sel;
                b.img.color = on ? HudTheme.Gold : HudTheme.Panel;
                b.txt.color = on ? HudTheme.Dark : HudTheme.Text;
            }
        };

        foreach (var it in items)
        {
            string key = it.key;
            var btn = HudKit.MakeButton(holder, Clean(it.label), HudTheme.Panel, HudTheme.Text, fontSize, null, height);
            buttons.Add((key, btn.GetComponent<Image>(), btn.GetComponentInChildren<TextMeshProUGUI>()));
            btn.onClick.AddListener(() => { paint(key); if (onSelect != null) onSelect(key); });
        }
        paint(initialKey);
    }

    static void StyleButton(Button btn)
    {
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        cb.pressedColor = new Color(0.68f, 0.68f, 0.68f, 1f);
        cb.selectedColor = Color.white;
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
    }
        // FAZ 0: AI ülkelerin son eylemlerini dünya haberleri olarak göster
  
    int _worldNewsStamp;

    // =====================================================================
    // YARDIMCILAR
    // =====================================================================
    Dictionary<string, float> Snapshot(SimulationEngine e)
    {
        var d = new Dictionary<string, float>();
        d["legit"] = e.Legitimacy.CurrentLegitimacy;
        d["capital"] = e.PoliticalCapital;
        d["unrest"] = e.Universe.Unrest;
        d["army"] = e.Army.ArmySatisfaction;
        d["infl"] = e.Economy.Inflation;
        d["debt"] = e.Economy.NationalDebt;
        d["credit"] = e.Economy.CreditRating;
        foreach (var o in e.AllObjects) d["obj:" + o.Id] = o.ActualValue;
        foreach (var g in e.Demographics) d["grp:" + g.Id] = g.Satisfaction;
        foreach (var a in e.Actors) d["act:" + a.Id] = a.Loyalty;
        return d;
    }

    internal string DeltaStr(string key, bool higherIsBetter, bool neutral = false)
    {
        float now;
        if (!cur.TryGetValue(key, out now)) return "";
        float before;
        if (!baseline.TryGetValue(key, out before)) return "";
        float d = now - before;
        if (Mathf.Abs(d) < 0.05f) return "";
        Color col = neutral ? HudTheme.Dim : (((d > 0f) == higherIsBetter) ? HudTheme.Good : HudTheme.Bad);
        return Colored(d.ToString("+0.0;-0.0"), col);
    }

    internal static bool IsRadical(SimulationEngine e, string groupId)
    {
        bool r;
        return e.Universe.Radicalized.TryGetValue(groupId, out r) && r;
    }

    internal static string PolicyName(SimulationEngine e, string policyId)
    {
        var p = e.AllObjects.OfType<SimPolicy>().FirstOrDefault(x => x.Id == policyId);
        return p != null ? p.Name : policyId;
    }

    internal static Color GoodHigh(float v, float good, float warn)
    {
        return v >= good ? HudTheme.Good : (v >= warn ? HudTheme.Warn : HudTheme.Bad);
    }

    internal static Color GoodLow(float v, float good, float warn)
    {
        return v <= good ? HudTheme.Good : (v <= warn ? HudTheme.Warn : HudTheme.Bad);
    }

    static string Colored(string text, Color c)
    {
        return "<color=" + HudTheme.Tag(c) + ">" + text + "</color>";
    }

    // Yazı tipinde bulunmayan emoji / ok / blok karakterlerini temizler (kare olarak görünmesinler)
    internal void ShowObjectDetail(SimObject obj) { new ObjectDetailPresenter(this).Show(obj); }

    public static string Clean(string s)
{
    if (string.IsNullOrEmpty(s)) return "";
    var sb = new StringBuilder(s.Length);
    for (int i = 0; i < s.Length; i++)
    {
        char ch = s[i];
        if (char.IsSurrogate(ch)) continue;
        if (ch >= '\u2190' && ch <= '\u2BFF') continue;      // oklar, semboller, uyarı işaretleri
        if (ch == '\uFE0F' || ch == '\u200D' || ch == '\u20E3') continue;
        if (ch == '\u26A0') continue;                        // ⚠ warning sign açıkça
        sb.Append(ch);
    }
    return sb.ToString().Trim();
}
}