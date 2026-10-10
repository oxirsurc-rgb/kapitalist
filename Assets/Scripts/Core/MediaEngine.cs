using System;
using System.Collections.Generic;

namespace DemocracySim.Engine.Core
{
    public class NewsArticle
    {
        public string Headline { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public float ImpactOnLegitimacy { get; set; }
    }

    public class MediaEngine
    {
        public List<NewsArticle> GenerateTurnNews(SimulationEngine engine)
        {
            var news = new List<NewsArticle>();
            foreach (var obj in engine.AllObjects) {
                float delta = obj.ActualValue - obj.TargetValue;
                if (Math.Abs(delta) > 2f && obj.TopImpactor != null) {
                    string direction = obj.TopImpactor.Type == EffectType.Positive ? "yükselişte" : "düşüşte";
                    news.Add(new NewsArticle {
                        Headline = $"{obj.Name} {direction}!",
                        Content = $"{obj.TopImpactor.Source.Name} nedeniyle {obj.Name} değerinde değişim gözlendi.",
                        ImpactOnLegitimacy = (obj.IdeologicalAlignment * obj.ActualValue > 0) ? 0.5f : -0.5f
                    });
                }
            }
            return news;
        }
    }
}