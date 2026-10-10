using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace DemocracySim.Engine.UI
{
/// <summary>
/// FAZ 13: Veri görselleştirme araç seti.
/// HudKit'e ek olarak line/pie/radar chart üretir.
/// Tümü runtime'da UGUI Image + RectTransform ile çizilir.
/// </summary>
public static class ChartKit
{
    // ═══════════════════════════════════════════════════════════════
    // 1) LINE CHART — Zaman serisi
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Zaman serisi grafiği. data.Count nokta çizer, min/max otomatik ölçekler.
    /// </summary>
    public static RectTransform DrawLineChart(Transform parent, IList<float> data, Color lineColor,
        float height = 120f, string title = null, bool showMinMax = true)
    {
        var holder = HudKit.NewRect(parent, "LineChart");
        HudKit.VStack(holder.gameObject, 4, 8);
        HudKit.Size(holder.gameObject, prefH: height + (title != null ? 30 : 10), minH: height);

        if (!string.IsNullOrEmpty(title))
        {
            var titleLabel = HudKit.Label(holder, title, 18, HudTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);
            HudKit.Size(titleLabel.gameObject, prefH: 24);
        }

        // Ana grafik alanı
        var plot = HudKit.Box(holder, "Plot", HudTheme.PanelHi);
        HudKit.Size(plot.gameObject, prefH: height, minH: height);

        if (data == null || data.Count < 2)
        {
            var empty = HudKit.Label(plot.transform, "Yetersiz veri", 16, HudTheme.Dim,
                TextAlignmentOptions.Center);
            HudKit.Fill(empty.rectTransform);
            return holder;
        }

        float min = data.Min();
        float max = data.Max();
        float range = Math.Max(0.01f, max - min);

        // Hafif padding (üstten/alttan %10 boşluk)
        float pad = range * 0.1f;
        min -= pad; max += pad;
        range = max - min;

        // Yatay grid çizgileri (5 çizgi)
        for (int i = 0; i <= 4; i++)
        {
            var line = HudKit.Box(plot.transform, "Grid", new Color(1, 1, 1, 0.05f));
            var rt = line.rectTransform;
            rt.anchorMin = new Vector2(0, i / 4f);
            rt.anchorMax = new Vector2(1, i / 4f);
            rt.offsetMin = new Vector2(0, -0.5f);
            rt.offsetMax = new Vector2(0, 0.5f);
            line.raycastTarget = false;
        }

        // Veri noktaları arasında çizgiler
        int count = data.Count;
        float width = plot.rectTransform.rect.width;
        if (width <= 0) width = 400f;   // fallback (layout henüz hesaplanmamışsa)

        for (int i = 0; i < count - 1; i++)
        {
            float x1 = (i / (float)(count - 1)) * width;
            float y1 = ((data[i] - min) / range) * height;
            float x2 = ((i + 1) / (float)(count - 1)) * width;
            float y2 = ((data[i + 1] - min) / range) * height;

            DrawLineSegment(plot.transform, new Vector2(x1, y1), new Vector2(x2, y2), lineColor, 2f);
        }

        // Veri noktaları (küçük daireler)
        for (int i = 0; i < count; i++)
        {
            float x = (i / (float)(count - 1)) * width;
            float y = ((data[i] - min) / range) * height;

            var dot = HudKit.Box(plot.transform, "Dot", lineColor, true, true);
            var rt = dot.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(6, 6);
            dot.raycastTarget = false;
        }

        // Min/Max etiketleri
        if (showMinMax)
        {
            var minLabel = HudKit.Label(plot.transform, min.ToString("F0"), 12, HudTheme.Dim,
                TextAlignmentOptions.Left);
            HudKit.Fill(minLabel.rectTransform, 4, 4, 4, 4);
            minLabel.rectTransform.anchorMin = new Vector2(0, 0);
            minLabel.rectTransform.anchorMax = new Vector2(0.3f, 0.2f);

            var maxLabel = HudKit.Label(plot.transform, max.ToString("F0"), 12, HudTheme.Dim,
                TextAlignmentOptions.Right);
            HudKit.Fill(maxLabel.rectTransform, 4, 4, 4, 4);
            maxLabel.rectTransform.anchorMin = new Vector2(0.7f, 0.8f);
            maxLabel.rectTransform.anchorMax = new Vector2(1, 1);
        }

        return holder;
    }

    /// <summary>İki nokta arasında döndürülmüş bir çizgi çizer.</summary>
    private static void DrawLineSegment(Transform parent, Vector2 from, Vector2 to, Color color, float thickness)
    {
        var go = HudKit.NewRect(parent, "Segment");
        var img = go.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        var rt = go;
        Vector2 diff = to - from;
        float length = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = from;
        rt.sizeDelta = new Vector2(length, thickness);
        rt.localRotation = Quaternion.Euler(0, 0, angle);
    }

    // ═══════════════════════════════════════════════════════════════
    // 2) PIE CHART — Kategori dağılımı
    // ═══════════════════════════════════════════════════════════════
    public class PieSlice
    {
        public string Label;
        public float Value;
        public Color Color;

        public PieSlice(string label, float value, Color color)
        {
            Label = label; Value = value; Color = color;
        }
    }

    /// <summary>
    /// Pasta grafiği. Segmentler saat yönünde sıralanır.
    /// </summary>
    public static RectTransform DrawPieChart(Transform parent, List<PieSlice> slices,
        float diameter = 200f, string title = null)
    {
        var holder = HudKit.NewRect(parent, "PieChart");
        HudKit.VStack(holder.gameObject, 6, 8);

        if (!string.IsNullOrEmpty(title))
        {
            var titleLabel = HudKit.Label(holder, title, 18, HudTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);
            HudKit.Size(titleLabel.gameObject, prefH: 24);
        }

        if (slices == null || slices.Count == 0)
        {
            var empty = HudKit.Label(holder, "Veri yok", 16, HudTheme.Dim);
            return holder;
        }

        // Row: pie solda, legend sağda
        var row = HudKit.NewRect(holder, "Row");
        HudKit.HStack(row.gameObject, 16, 0, TextAnchor.MiddleLeft, false, false);
        HudKit.Size(row.gameObject, prefH: diameter);

        // Pie container
        var pieRoot = HudKit.NewRect(row, "PieRoot");
        HudKit.Size(pieRoot.gameObject, prefW: diameter, prefH: diameter, minH: diameter);

        var pieBg = HudKit.Box(pieRoot, "Bg", HudTheme.PanelHi, true, false);
        HudKit.Fill(pieBg.rectTransform);

        // Basit yaklaşım: her segment için bir kutu + toplam boyut
        // (Gerçek daire dilimleri için custom mesh gerekir — bunun yerine yatay stacked bar kullanıyoruz.)
        float total = slices.Sum(s => s.Value);
        if (total <= 0) total = 1;

        var barRoot = HudKit.NewRect(pieRoot, "StackedBar");
        HudKit.Fill(barRoot, 12, 12, 12, 12);

        var vstack = barRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        vstack.spacing = 4;
        vstack.childControlWidth = true;
        vstack.childControlHeight = true;
        vstack.childForceExpandWidth = true;
        vstack.childForceExpandHeight = false;

        foreach (var s in slices)
        {
            float pct = s.Value / total;
            var rowGo = HudKit.NewRect(barRoot, "SliceRow");
            HudKit.HStack(rowGo.gameObject, 6, 0, TextAnchor.MiddleLeft, true, false);
            HudKit.Size(rowGo.gameObject, prefH: Math.Max(20, pct * (diameter - 60)));

            var bar = HudKit.Box(rowGo, "Bar", s.Color);
            HudKit.Size(bar.gameObject, flexW: 1, prefH: 24);

            var barLabel = HudKit.Label(bar.transform, $"{s.Label}  ({pct * 100:F0}%)",
                14, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
            HudKit.Fill(barLabel.rectTransform, 6, 0, 6, 0);
        }

        return holder;
    }

    // ═══════════════════════════════════════════════════════════════
    // 3) RADAR CHART — Çok boyutlu karşılaştırma
    // ═══════════════════════════════════════════════════════════════
    public class RadarAxis
    {
        public string Label;
        public float Value01;   // 0-1

        public RadarAxis(string label, float value01)
        {
            Label = label; Value01 = Mathf.Clamp01(value01);
        }
    }

    /// <summary>
    /// Radar (örümcek) grafiği — N eksenli. Karşılaştırma için 2 set destekler.
    /// </summary>
    public static RectTransform DrawRadarChart(Transform parent, List<RadarAxis> axesA,
        List<RadarAxis> axesB = null, float radius = 100f, string labelA = null, string labelB = null)
    {
        var holder = HudKit.NewRect(parent, "RadarChart");
        HudKit.VStack(holder.gameObject, 4, 8);
        HudKit.Size(holder.gameObject, prefH: radius * 2 + 40);

        if (axesA == null || axesA.Count < 3)
        {
            HudKit.Label(holder, "En az 3 eksen gerekli", 16, HudTheme.Dim);
            return holder;
        }

        var root = HudKit.NewRect(holder, "Root");
        HudKit.Size(root.gameObject, prefH: radius * 2, minH: radius * 2);

        var bg = HudKit.Box(root, "Bg", HudTheme.PanelHi, true, true);
        HudKit.Fill(bg.rectTransform);

        Vector2 center = Vector2.zero;
        int n = axesA.Count;

        // Arka plan poligonu (grid)
        for (int ring = 1; ring <= 4; ring++)
        {
            float ringR = radius * (ring / 4f);
            for (int i = 0; i < n; i++)
            {
                float a1 = -Mathf.PI / 2 + (i * 2 * Mathf.PI / n);
                float a2 = -Mathf.PI / 2 + ((i + 1) % n * 2 * Mathf.PI / n);
                Vector2 p1 = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * ringR;
                Vector2 p2 = center + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * ringR;

                var edge = HudKit.Box(root, "GridEdge", new Color(1, 1, 1, 0.08f));
                edge.raycastTarget = false;
                var rt = edge.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                Vector2 diff = p2 - p1;
                rt.sizeDelta = new Vector2(diff.magnitude, 1);
                rt.anchoredPosition = (p1 + p2) / 2;
                rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);
            }
        }

        // Eksen çizgileri + etiketler
        for (int i = 0; i < n; i++)
        {
            float a = -Mathf.PI / 2 + (i * 2 * Mathf.PI / n);
            Vector2 outer = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;

            var axis = HudKit.Box(root, "Axis", new Color(1, 1, 1, 0.15f));
            axis.raycastTarget = false;
            var rt = axis.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.sizeDelta = new Vector2(radius, 1);
            rt.anchoredPosition = center;
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(outer.y, outer.x) * Mathf.Rad2Deg);

            // Etiket
            Vector2 labelPos = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (radius + 18);
            var lbl = HudKit.Label(root, axesA[i].Label, 11, HudTheme.Text, TextAlignmentOptions.Center);
            var lrt = lbl.rectTransform;
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.anchoredPosition = labelPos;
            lrt.sizeDelta = new Vector2(70, 16);
        }

        // Veri poligonu A
        DrawRadarPolygon(root, axesA, radius, n, HudTheme.Gold, 2f);

        // Veri poligonu B (opsiyonel)
        if (axesB != null && axesB.Count == n)
        {
            DrawRadarPolygon(root, axesB, radius, n, HudTheme.Info, 2f);
        }

        // Legend
        if (!string.IsNullOrEmpty(labelA))
        {
            var legend = HudKit.NewRect(holder, "Legend");
            HudKit.HStack(legend.gameObject, 12, 0, TextAnchor.MiddleCenter, true, false);
            HudKit.Size(legend.gameObject, prefH: 20);

            var dotA = HudKit.Box(legend, "DotA", HudTheme.Gold, true, true);
            HudKit.Size(dotA.gameObject, prefW: 12, prefH: 12);
            HudKit.Label(legend, labelA, 14, HudTheme.Text);

            if (!string.IsNullOrEmpty(labelB))
            {
                var dotB = HudKit.Box(legend, "DotB", HudTheme.Info, true, true);
                HudKit.Size(dotB.gameObject, prefW: 12, prefH: 12);
                HudKit.Label(legend, labelB, 14, HudTheme.Text);
            }
        }

        return holder;
    }

    private static void DrawRadarPolygon(Transform root, List<RadarAxis> axes, float radius, int n, Color color, float thickness)
    {
        for (int i = 0; i < n; i++)
        {
            float a1 = -Mathf.PI / 2 + (i * 2 * Mathf.PI / n);
            float a2 = -Mathf.PI / 2 + ((i + 1) % n * 2 * Mathf.PI / n);

            Vector2 p1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius * axes[i].Value01;
            Vector2 p2 = new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * radius * axes[(i + 1) % n].Value01;

            DrawLineSegment(root, p1, p2, color, thickness);

            // Nokta
            var dot = HudKit.Box(root, "Dot", color, true, true);
            dot.raycastTarget = false;
            var rt = dot.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = p1;
            rt.sizeDelta = new Vector2(6, 6);
        }
    }
}
}