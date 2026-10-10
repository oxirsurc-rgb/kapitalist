using System;
using System.Collections.Generic;
using System.Linq;
using DemocracySim.Engine.Data;

namespace DemocracySim.Engine.Core
{
    // Bir seçeneğin metnini ve sonucunu tutan sınıf
    public class EventChoice
    {
        public string ChoiceText { get; set; }
        public Action<SimulationEngine> Effect { get; set; }
        public string ResultText { get; set; }
    }

    // Bir olayı (Event) tanımlayan ana sınıf
    // FAZ 3: Olay kapsamı — hangi rolde çıkabilir?
    public enum EventScope
    {
        Both,           // Her iki rol
        Government,     // Sadece iktidar
        Opposition      // Sadece muhalefet
    }

    // Bir olayı (Event) tanımlayan ana sınıf
    public class GameEvent
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<EventChoice> Choices { get; set; } = new List<EventChoice>();
        public float TriggerChance { get; set; }
        public EventScope Scope { get; set; } = EventScope.Both;   // YENİ
        public bool IsNotificationOnly => Choices == null || Choices.Count == 0;
    }

    // FAZ 2 Adım 4: Zincir olayları için bekleyen kayıt
    public class PendingChain
    {
        public string EventId { get; set; }
        public int TurnsLeft { get; set; }
    }

    public class EventManager
    {
        public List<GameEvent> EventPool { get; set; } = new List<GameEvent>();

        // FAZ 2 Adım 4: Zincir olayları için bekleyenler listesi
        private List<PendingChain> _pendingChains = new List<PendingChain>();

        // FAZ 2 Adım 4: Zincir haritaları (CSV'den yüklenir)
        private Dictionary<string, string> _chainMap = new Dictionary<string, string>();
        private Dictionary<string, int> _chainDelay = new Dictionary<string, int>();

        // =====================================================
        // CONSTRUCTOR
        // =====================================================
        public EventManager()
        {
            // Sıra önemli:
            // 1. Önce zincirleri yükle (CSV'den chains.json)
            LoadChainsFromJson();
            
            // 2. Sonra olayları yükle (CSV'den events.json)
            LoadEventsFromJson();

            // 3. Hiç olay yüklenemezse yedek hardcoded olaylar
            if (EventPool.Count == 0)
            {
                SimLogger.Log("[EventManager] events.json bos, varsayilan olaylar yukleniyor.");
                LoadDefaultEvents();
            }
        }

        // =====================================================
        // CSV / JSON YÜKLEME
        // =====================================================
        private void LoadEventsFromJson()
        {
            try
            {
                var eventsData = DataManager.LoadEvents();
                foreach (var d in eventsData)
                {
                    if (string.IsNullOrEmpty(d.Id)) continue;

                                       var ev = new GameEvent
                    {
                        Id = d.Id,
                        Title = d.Title,
                        Description = d.Description,
                        TriggerChance = d.TriggerChance,
                        Scope = ParseScope(d.Scope)   // YENİ
                    };

                    if (!string.IsNullOrWhiteSpace(d.Choice1Text))
                        ev.Choices.Add(BuildChoice(d.Choice1Text, d.Choice1Effect, d.Choice1Result));
                    if (!string.IsNullOrWhiteSpace(d.Choice2Text))
                        ev.Choices.Add(BuildChoice(d.Choice2Text, d.Choice2Effect, d.Choice2Result));
                    if (!string.IsNullOrWhiteSpace(d.Choice3Text))
                        ev.Choices.Add(BuildChoice(d.Choice3Text, d.Choice3Effect, d.Choice3Result));

                    EventPool.Add(ev);
                }
                SimLogger.Log($"[EventManager] events.json'dan {EventPool.Count} olay yuklendi.");
            }
            catch (Exception e)
            {
                SimLogger.Log($"[EventManager] JSON yukleme hatasi: {e.Message}", SimLogger.LogLevel.Error);
            }
        }

        private void LoadChainsFromJson()
        {
            try
            {
                var chainsData = DataManager.LoadChains();
                foreach (var c in chainsData)
                {
                    if (string.IsNullOrEmpty(c.FromEventId)) continue;
                    _chainMap[c.FromEventId] = c.ToEventId;
                    _chainDelay[c.FromEventId] = c.DelayTurns;
                }
                SimLogger.Log($"[EventManager] {_chainMap.Count} zincir yuklendi.");
            }
            catch (Exception e)
            {
                SimLogger.Log($"[EventManager] Chain yukleme hatasi: {e.Message}", SimLogger.LogLevel.Error);
            }
        }
                private EventScope ParseScope(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return EventScope.Both;
            switch (s.Trim().ToLower())
            {
                case "government": return EventScope.Government;
                case "opposition": return EventScope.Opposition;
                case "both":       return EventScope.Both;
                default:           return EventScope.Both;
            }
        }

        // =====================================================
        // SEÇENEK OLUŞTURMA
        // =====================================================
        private EventChoice BuildChoice(string text, string effectCode, string result)
        {
            return new EventChoice
            {
                ChoiceText = text,
                ResultText = result,
                Effect = (SimulationEngine e) => ApplyEffectCode(e, effectCode)
            };
        }

        /// <summary>
        /// CSV'de yazilan basit etki kodlarini uygular.
        /// Format: "legitimacy+10;gdp-3;capital-20"
        /// </summary>
        private void ApplyEffectCode(SimulationEngine e, string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;

            var parts = code.Split(';');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                int signIdx = trimmed.IndexOfAny(new[] { '+', '-' });
                if (signIdx < 0) continue;

                string key = trimmed.Substring(0, signIdx).Trim();
                string valStr = trimmed.Substring(signIdx);
                float val = 0f;
                float.TryParse(valStr.Replace(',', '.'),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out val);

                ApplySingleEffect(e, key, val);
            }
        }

        private void ApplySingleEffect(SimulationEngine e, string key, float val)
{
    switch (key.ToLower())
    {
        case "legitimacy":  e.Legitimacy.AdjustLegitimacy(val); break;
        case "capital":     e.PoliticalCapital = Math.Max(0f, e.PoliticalCapital + val); break;
        case "inflation":   e.Economy.AdjustInflation(val); break;
        case "debt":        e.Economy.AdjustDebt(val); break;
        case "unrest":      e.Universe.Unrest = Math.Clamp(e.Universe.Unrest + val, 0f, 100f); break;
        case "sanction":    e.Universe.SanctionLevel = Math.Clamp(e.Universe.SanctionLevel + val, 0f, 100f); break;
        case "coup_risk":   e.Army.LoadState(
                                e.Army.ArmySatisfaction,
                                e.Army.MilitaryStrength,
                                Math.Clamp(e.Army.LoyaltyToLeader + val, 0f, 100f)); break;
        case "army":        e.Army.LoadState(
                                Math.Clamp(e.Army.ArmySatisfaction + val, 0f, 100f),
                                e.Army.MilitaryStrength, e.Army.LoyaltyToLeader); break;
        case "intel":       e.Intel.LoadState(
                                Math.Clamp(e.Intel.NetworkStrength + val, 0f, 100f),
                                e.Intel.AgencyBudget, e.Intel.TechLevel); break;

        // EK-11: Yeni demografik gruplar (events_pack2.json'da kullanılıyor)
        case "workers":
        case "capitalists":
        case "intellectuals":
        case "conservatives":
        case "youth":
        case "rural":
        case "retirees":
        case "students":
            var grp = e.Demographics.Find(g => g.Id == key.ToLower());
            if (grp != null) grp.AdjustSatisfaction(val);
            else SimLogger.Log($"[EventSystem] Grup bulunamadı: '{key}'", SimLogger.LogLevel.Warning);
            break;

        case "nationalist_grp":
            var ng = e.Demographics.Find(g => g.Id == "conservatives");
            ng?.AdjustSatisfaction(val);
            break;

        // EK-11: Bakan yolsuzluk olayı — rastgele bir bakanı görevden al
        case "dismiss_minister":
        case "dismiss_random_minister":
            {
                var ministers = e.Actors.Where(a => a.Role == ActorRole.Minister).ToList();
                if (ministers.Count > 0)
                {
                    // Deterministik RNG kullan
                    var rng = KapitalistRng.For("event_dismiss");
                    var target = ministers[rng.Next(ministers.Count)];
                    e.Actors.Remove(target);
                    SimLogger.Log($"[Olay] {target.Name} görevden alındı (yolsuzluk).", SimLogger.LogLevel.Warning);
                    // Kullanıcıya haber ver
                    e.Universe.Messages.Add(new UniverseMessage
                    {
                        Text = $"🏛️ YOLSUZLUK: {target.Name} görevden alındı! Kabine küçüldü.",
                        IsWarning = false
                    });
                }
                else
                {
                    SimLogger.Log("[Olay] Görevden alınacak bakan yok.", SimLogger.LogLevel.Warning);
                }
            }
            break;

        // EK-11: Aktif yasa iptali (dava, anayasa mahkemesi vb.)
        case "revert_policy":
            {
                var active = e.AllObjects.OfType<SimPolicy>().Where(p => p.IsActive).ToList();
                if (active.Count > 0)
                {
                    var rng = KapitalistRng.For("event_revert");
                    var target = active[rng.Next(active.Count)];
                    target.IsActive = false;
                    e.Universe.Messages.Add(new UniverseMessage
                    {
                        Text = $"📜 {target.Name} yasası iptal edildi.",
                        IsWarning = true
                    });
                }
            }
            break;

        // EK-11: Bilinmeyen anahtarlar için doğru handling
        case "education_budget":
        case "healthcare_budget":
        case "military_budget":
        case "infrastructure_budget":
        case "tax_income":
        case "tax_corporate":
        case "rnd_budget":
            // Politika değeri — GetEffectiveValue yerine ActualValue'yu set et
            var policy = e.AllObjects.OfType<SimPolicy>().FirstOrDefault(p => p.Id == key.ToLower());
            if (policy != null)
            {
                policy.ActualValue = Math.Clamp(policy.ActualValue + val, policy.MinValue, policy.MaxValue);
                policy.IsActive = true;
                if (policy.Intensity <= 0f) policy.Intensity = 1f;
            }
            else
            {
                SimLogger.Log($"[EventSystem] Politika bulunamadı: '{key}'", SimLogger.LogLevel.Warning);
            }
            break;

        case "environment":
        case "environment_quality":
            var env = e.Registry.Get("environment_quality");
            if (env != null) env.ActualValue = Math.Clamp(env.ActualValue + val, env.MinValue, env.MaxValue);
            break;

        case "coalition":
            // Koalisyon ortaklarının memnuniyeti
            if (e.Universe.Partners != null)
                foreach (var p in e.Universe.Partners) 
                    p.Satisfaction = Math.Clamp(p.Satisfaction + val, 0f, 100f);
            break;

        case "deep_state":
            e.DeepStateStability = Math.Clamp(e.DeepStateStability + val, 0f, 100f);
            break;

        case "corruption":
            e.CorruptionLevel = Math.Clamp(e.CorruptionLevel + val, 0f, 100f);
            break;

        case "credit":
        case "credit_rating":
            e.Economy.AdjustDebt(-val * 10f);   // Credit rating yerine borç değişimi
            break;

        case "media_trust":
            // MediaManager'da TrustInGovernment alanı var
            if (e.MediaOutlets != null)
                foreach (var o in e.MediaOutlets.Outlets)
                    o.TrustInGovernment = Math.Clamp(o.TrustInGovernment + val, 0f, 100f);
            break;

        case "migration":
            // Göç yönetimi
            if (e.Migration != null)
                e.Migration.IntegrationScore = Math.Clamp(e.Migration.IntegrationScore + val, 0f, 100f);
            break;

        case "turn_until_election":
            e.TurnUntilElection = Math.Max(0, e.TurnUntilElection + (int)val);
            break;

        case "nationalist":
            // nationalist = conservatives grubu (alias)
            var natGrp = e.Demographics.Find(g => g.Id == "conservatives");
            natGrp?.AdjustSatisfaction(val);
            break;

        case "opposition":
            // Muhalefet desteği — hükümet meşruiyetini düşürür
            e.Legitimacy.AdjustLegitimacy(-val * 0.5f);
            break;

        default:
            var obj = e.Registry.Get(key.ToLower());
            if (obj != null)
            {
                // EK-12: Motor bypass'ı düzelt — hem ActualValue hem EquilibriumValue'ya uygula
                float delta = val;
                obj.ActualValue = Math.Clamp(obj.ActualValue + delta, obj.MinValue, obj.MaxValue);
                obj.EquilibriumValue = Math.Clamp(obj.EquilibriumValue + delta, obj.MinValue, obj.MaxValue);
            }
            else
            {
                // EK-11: Bilinmeyen anahtar — artık sessizce yutma, log yaz!
                SimLogger.Log($"[EventSystem] BİLİNMEYEN effect anahtarı: '{key}' (değer: {val}). " +
                              "Bu effect hiçbir şey yapmayacak! events.json'u kontrol edin.",
                              SimLogger.LogLevel.Error);
            }
            break;
    }
}

        // =====================================================
        // ZİNCİR SİSTEMİ (FAZ 2 Adım 4)
        // =====================================================

        /// <summary>Bu olayin bir zincir devami var mi?</summary>
        public bool HasChain(string eventId)
        {
            return !string.IsNullOrEmpty(eventId) && _chainMap.ContainsKey(eventId);
        }

        /// <summary>Bir olay secildiginde zincir devamini planla.</summary>
        public void ScheduleChainFromEvent(string eventId)
        {
            if (!_chainMap.TryGetValue(eventId, out string toEventId)) return;
            int delay = _chainDelay.TryGetValue(eventId, out int d) ? d : 4;
            ScheduleChainEvent(toEventId, delay);
        }

        /// <summary>X tur sonra tetiklenecek bir olay planla.</summary>
        public void ScheduleChainEvent(string eventId, int delayTurns)
        {
            _pendingChains.Add(new PendingChain { EventId = eventId, TurnsLeft = delayTurns });
            SimLogger.Log($"[EventManager] Zincir planlandi: {eventId} -> {delayTurns} tur sonra");
        }

        // =====================================================
        // OLAY KONTROLÜ
        // =====================================================        /// <summary>FAZ 3: Role göre olay seçer. Muhalefetteyken Government olayları çıkmaz.</summary>
        public GameEvent CheckForEvent(SimulationEngine e)
        {
            bool isOpposition = e != null && e.CurrentRole == SimulationEngine.PlayerRole.Opposition;

            // 1) Önce bekleyen zincirleri kontrol et
            for (int i = _pendingChains.Count - 1; i >= 0; i--)
            {
                _pendingChains[i].TurnsLeft--;
                if (_pendingChains[i].TurnsLeft <= 0)
                {
                    var chainEvent = EventPool.FirstOrDefault(ev => ev.Id == _pendingChains[i].EventId);
                    _pendingChains.RemoveAt(i);
                    if (chainEvent != null && IsEventAllowedForRole(chainEvent, isOpposition))
                    {
                        SimLogger.Log($"[EventManager] Zincir tetiklendi: {chainEvent.Title}");
                        return chainEvent;
                    }
                }
            }

            // 2) Role uygun olayları filtrele
            var possibleEvents = EventPool
                .Where(ev => IsEventAllowedForRole(ev, isOpposition))
                .Where(ev => SimRng.NextDouble() < ev.TriggerChance)
                .ToList();

            if (possibleEvents.Count > 0)
            {
                return possibleEvents[SimRng.Next(possibleEvents.Count)];
            }
            return null;
        }
                // Geriye dönük uyumluluk (parametre almayan eski çağrılar için)
        public GameEvent CheckForEvent()
        {
            return CheckForEvent(null);
        }

        /// <summary>Bu olay oyuncunun rolüne uygun mu?</summary>
        private bool IsEventAllowedForRole(GameEvent ev, bool isOpposition)
        {
            if (ev.Scope == EventScope.Both) return true;
            if (isOpposition && ev.Scope == EventScope.Opposition) return true;
            if (!isOpposition && ev.Scope == EventScope.Government) return true;
            return false;
        }

        // =====================================================
        // YEDEK: Hardcoded olaylar (CSV yuklenmezse)
        // =====================================================
        private void LoadDefaultEvents()
        {
            EventPool.Add(new GameEvent {
                Id = "strike_event",
                Title = "MADEN GREVI",
                Description = "Maden iscileri dusuk ucretler nedeniyle uretimi durdurdu!",
                TriggerChance = 0.15f,
                Choices = new List<EventChoice> {
                    new EventChoice {
                        ChoiceText = "Talepleri kabul et",
                        Effect = (e) => {
                            e.Legitimacy.AdjustLegitimacy(10f);
                            e.PoliticalCapital -= 20f;
                        },
                        ResultText = "Isciler mutlu oldu, butce sarsildi."
                    },
                    new EventChoice {
                        ChoiceText = "Orduyu kullanarak bastir",
                        Effect = (e) => {
                            e.Legitimacy.AdjustLegitimacy(-20f);
                            e.Army.UpdateArmy(e, 70f);
                        },
                        ResultText = "Duzen saglandi ama halk nefret etti."
                    }
                }
            });
        }
    }
}