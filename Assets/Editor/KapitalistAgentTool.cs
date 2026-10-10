#if UNITY_EDITOR
// KapitalistAgentTool — Yapay zeka asistanının oyunu OYNAMASI için MCP aracı
// Nereye konur: Assets/Editor/KapitalistAgentTool.cs
// Gerekenler : MCP for Unity paketi kurulu olmalı.
//
// MCP'de tek araç görünür: kapitalist_play  (parametre: action)
//   play | stop | open_scene | screen | click | type | status
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using DemocracySim.Engine.Core;

namespace Kapitalist.AgentBridge
{
    [McpForUnityTool("kapitalist_play",
        Description = "Kapitalist oyununu OYNAR. action=play|stop|open_scene|screen|click|type|status. " +
                      "Önce play, sonra screen ile ekranı oku (DURUM, METİNLER, numaralı BUTONLAR). " +
                      "Butona basmak için click + id (+ label). Buton numaraları her ekranda değişir.")]
    public static class KapitalistAgentTool
    {
        public class Parameters
        {
            [ToolParameter("play | stop | open_scene | screen | click | type | status")]
            public string action { get; set; }

            [ToolParameter("click için: screen çıktısındaki buton numarası", Required = false)]
            public int? id { get; set; }

            [ToolParameter("click için: butonun yazısı (doğrulama; eski ekrana yanlış tıklamayı önler)", Required = false)]
            public string label { get; set; }

            [ToolParameter("type için: yazı alanı numarası (g0 -> 0)", Required = false)]
            public int? input { get; set; }

            [ToolParameter("type için: yazılacak metin", Required = false)]
            public string text { get; set; }

            [ToolParameter("open_scene için: Assets/... .unity yolu", Required = false)]
            public string path { get; set; }

            [ToolParameter("screen/click yanıtına sayfa metinleri eklensin mi (varsayılan true)", Required = false)]
            public bool? includeTexts { get; set; }

            [ToolParameter("metin üst sınırı karakter (varsayılan 5000)", Required = false)]
            public int? maxChars { get; set; }
        }

        static readonly List<Button> Buttons = new List<Button>();
        static readonly List<string> ButtonLabels = new List<string>();
        static readonly List<TMP_InputField> Inputs = new List<TMP_InputField>();

        public static object HandleCommand(JObject @params)
        {
            try
            {
                string action = ((string)@params["action"] ?? "screen").Trim().ToLowerInvariant();
                bool includeTexts = @params.Value<bool?>("includeTexts") ?? true;
                int maxChars = @params.Value<int?>("maxChars") ?? 5000;

                switch (action)
                {
                    case "play":
                        if (EditorApplication.isPlaying) return Ok("Zaten Play modunda.");
                        EditorApplication.isPlaying = true;
                        return Ok("Play moduna geçiliyor. 3-10 sn bekle, sonra action=screen çağır.");

                    case "stop":
                        EditorApplication.isPlaying = false;
                        return Ok("Play modundan çıkılıyor.");

                    case "open_scene":
                    {
                        if (EditorApplication.isPlaying) return Err("Önce action=stop ile Play'den çık.");
                        string path = (string)@params["path"];
                        if (string.IsNullOrEmpty(path)) return Err("path gerekli. Örn: Assets/Scenes/MainMenu.unity");
                        EditorSceneManager.OpenScene(path);
                        return Ok("Sahne açıldı: " + path);
                    }

                    case "status":
                        return Ok(Status());

                    case "screen":
                        if (!EditorApplication.isPlaying) return Err("Play modunda değil. action=play çağır (sahne açık olmalı).");
                        return Ok("ekran", BuildScreen(includeTexts, maxChars));

                    case "click":
                    {
                        if (!EditorApplication.isPlaying) return Err("Play modunda değil.");
                        int? id = @params.Value<int?>("id");
                        if (id == null) return Err("id gerekli.");
                        if (Buttons.Count == 0) return Err("Önce action=screen çağır.");
                        if (id < 0 || id >= Buttons.Count) return Err($"Geçersiz id. 0..{Buttons.Count - 1} arası olmalı.");

                        var b = Buttons[id.Value];
                        if (b == null || !b.isActiveAndEnabled || !b.IsInteractable())
                            return Err("Bu buton artık yok/pasif; ekran değişmiş. action=screen ile yenile.");

                        string expect = (string)@params["label"];
                        string actual = ButtonLabels[id.Value];
                        if (!string.IsNullOrEmpty(expect) && actual.IndexOf(expect, StringComparison.OrdinalIgnoreCase) < 0)
                            return Err($"Eski ekran: [{id}] artık '{actual}'. action=screen ile yenile.");

                        string error = null;
                        try { b.onClick.Invoke(); }
                        catch (Exception ex) { error = ex.GetType().Name + ": " + ex.Message; }

                        string msg = $"Tıklandı: [{id}] {actual}" + (error != null ? $"  ⚠ HATA: {error}" : "");
                        return Ok(msg, BuildScreen(includeTexts, maxChars));
                    }

                    case "type":
                    {
                        if (!EditorApplication.isPlaying) return Err("Play modunda değil.");
                        int? idx = @params.Value<int?>("input");
                        string text = (string)@params["text"] ?? "";
                        if (idx == null || idx < 0 || idx >= Inputs.Count) return Err("Geçersiz input no. Önce action=screen çağır.");
                        Inputs[idx.Value].text = text;
                        return Ok($"Yazıldı: g{idx} = '{text}'", BuildScreen(includeTexts, maxChars));
                    }

                    default:
                        return Err("Bilinmeyen action. play | stop | open_scene | screen | click | type | status");
                }
            }
            catch (Exception ex)
            {
                return Err(ex.GetType().Name + ": " + ex.Message);
            }
        }

        static string BuildScreen(bool includeTexts, int maxChars)
        {
            Buttons.Clear(); ButtonLabels.Clear(); Inputs.Clear();

            var modal = FindModal();
            var roots = new List<Transform>();
            if (modal != null) roots.Add(modal);
            else
            {
                var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude)
                    .Where(c => c.isRootCanvas && c.enabled)
                    .OrderByDescending(c => c.sortingOrder).ThenBy(c => c.name);
                foreach (var c in canvases) roots.Add(c.transform);
            }

            var sb = new StringBuilder();
            sb.AppendLine("DURUM: " + Status());
            sb.AppendLine("EKRAN: " + (modal != null ? "POPUP açık (arkadaki ekrana tıklanamaz; önce bunu çöz)" : "ana ekran") + ActiveTab());

            if (includeTexts)
            {
                sb.AppendLine("METİNLER:");
                var t = new StringBuilder();
                string last = null;
                bool truncated = false;
                foreach (var r in roots)
                {
                    foreach (var tmp in r.GetComponentsInChildren<TMP_Text>(false))
                    {
                        if (tmp.GetComponentInParent<Button>() != null) continue;
                        if (tmp.GetComponentInParent<TMP_InputField>() != null) continue;
                        string s = Clean(tmp.text);
                        if (s.Length == 0 || s == last) continue;
                        last = s;
                        t.AppendLine("- " + s);
                        if (t.Length > maxChars) { truncated = true; break; }
                    }
                    if (truncated) break;
                }
                sb.Append(t);
                if (truncated) sb.AppendLine("... (kısaltıldı; daha fazlası için maxChars artır)");
            }

            foreach (var r in roots)
                foreach (var f in r.GetComponentsInChildren<TMP_InputField>(false))
                    Inputs.Add(f);
            if (Inputs.Count > 0)
            {
                sb.AppendLine("YAZI ALANLARI (action=type, input=no):");
                for (int i = 0; i < Inputs.Count; i++)
                {
                    string ph = Inputs[i].placeholder is TMP_Text p ? Clean(p.text) : "";
                    sb.AppendLine($"  g{i}: mevcut='{Clean(Inputs[i].text)}'  ipucu='{ph}'");
                }
            }

            foreach (var r in roots)
                foreach (var b in r.GetComponentsInChildren<Button>(false))
                {
                    if (!b.IsInteractable()) continue;
                    Buttons.Add(b);
                    ButtonLabels.Add(LabelOf(b));
                }

            sb.AppendLine("BUTONLAR (action=click, id=no):");
            for (int i = 0; i < Buttons.Count; i++)
            {
                string ctx = ContextOf(Buttons[i]);
                sb.AppendLine($"  [{i}] {ButtonLabels[i]}" + (ctx.Length > 0 ? $"   ‹{ctx}›" : ""));
            }
            if (Buttons.Count == 0) sb.AppendLine("  (tıklanabilir buton yok — sahne yükleniyor olabilir, 2 sn sonra tekrar dene)");
            return sb.ToString();
        }

        static Transform FindModal()
        {
            var ui = UIManager.Instance;
            if (ui == null) return null;
            foreach (var t in ui.GetComponentsInChildren<Transform>(false))
                if (t.name == "Modal" && t.gameObject.activeInHierarchy) return t;
            return null;
        }

        static string ActiveTab()
        {
            try
            {
                var ui = UIManager.Instance; if (ui == null) return "";
                var f = typeof(UIManager).GetField("currentTab", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                var v = f?.GetValue(ui) as string;
                return string.IsNullOrEmpty(v) ? "" : $"  | sekme={v}";
            }
            catch { return ""; }
        }

        static string ContextOf(Button b)
        {
            try
            {
                var p = b.transform.parent;
                for (int depth = 0; depth < 2 && p != null; depth++, p = p.parent)
                {
                    foreach (var t in p.GetComponentsInChildren<TMP_Text>(false))
                    {
                        if (t.transform.IsChildOf(b.transform)) continue;
                        string s = Clean(t.text);
                        if (s.Length > 0) return s.Length > 60 ? s.Substring(0, 60) + "…" : s;
                    }
                }
            }
            catch { }
            return "";
        }

        static string LabelOf(Button b)
        {
            var parts = b.GetComponentsInChildren<TMP_Text>(false).Select(x => Clean(x.text)).Where(x => x.Length > 0);
            string s = string.Join(" / ", parts);
            if (s.Length == 0) s = b.gameObject.name;
            return s.Length > 90 ? s.Substring(0, 90) + "…" : s;
        }

        static string Status()
        {
            if (!EditorApplication.isPlaying) return "Play modunda değil";
            try
            {
                var gm = UnityEngine.Object.FindAnyObjectByType<GameManager>();
                if (gm == null) return "GameManager yok (menü sahnesinde olabilirsin)";
                if (gm.playerCountry == null) return $"oyun başlamadı (durum={gm.currentState}) — ülke seçimi bekleniyor";

                var e = gm.playerCountry.Engine;
                bool over = false;
                var f = typeof(GameManager).GetField("gameOver", BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null) over = (bool)f.GetValue(gm);

                return $"{gm.playerCountry.Name} | rol={e.CurrentRole} | sermaye={e.PoliticalCapital:F0} | " +
                       $"meşruiyet={e.Legitimacy.CurrentLegitimacy:F1} | enflasyon={e.Economy.Inflation:F1} | " +
                       $"seçime={e.TurnUntilElection} tur | darbe riski={e.Army.CoupRiskPercent:F0}% | " +
                       $"huzursuzluk={e.Universe.Unrest:F0} | meclis gündemi={e.ProposedPolicies.Count} yasa" +
                       (over ? " | OYUN BİTTİ" : "");
            }
            catch (Exception ex) { return "durum okunamadı: " + ex.Message; }
        }

        static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = Regex.Replace(s, "<.*?>", "");
            s = s.Replace("\r", " ").Replace("\n", " ");
            return Regex.Replace(s, @"\s+", " ").Trim();
        }

        static object Ok(string message, string screen = null) =>
            new SuccessResponse(message, screen == null ? null : new { screen });

        static object Err(string message) => new ErrorResponse(message);
    }
}
#endif