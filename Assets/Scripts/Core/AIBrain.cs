using System;
using System.Collections.Generic;
using System.Linq;
using DemocracySim.Engine.Core;

namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ 17: Hibrit AI Beyni.
    /// Behavior Tree iskeleti + Utility AI puanlama.
    /// Her AI ülkesi için bir instance oluşturulur.
    /// </summary>
    public class AIBrain
    {
        public string CountryId { get; set; }
        public AIPersonality Personality { get; set; }
        public Blackboard BB { get; private set; } = new Blackboard();

        private BTNode _root;
        private readonly SimulationEngine _engine;

        public AIBrain(string countryId, AIPersonality personality, SimulationEngine engine)
        {
            CountryId = countryId;
            Personality = personality;
            _engine = engine;
            BuildTree();
        }

        /// <summary>Her tur çağrılır. BT'yi çalıştırır.</summary>
        public NodeState ProcessTurn()
        {
            // Blackboard'a güncel durumu yaz
            RefreshBlackboard();

            // Reset + Evaluate
            _root.Reset();
            var result = _root.Evaluate();

            return result;
        }

        private void RefreshBlackboard()
        {
            var e = _engine;
            BB.Set("Legitimacy", e.Legitimacy.CurrentLegitimacy);
            BB.Set("Unrest", e.Universe.Unrest);
            BB.Set("GDP", e.Registry.GetValue(ObjectRegistry.Ids.Gdp, 50f));
            BB.Set("Army", e.Army.ArmySatisfaction);
            BB.Set("Corruption", e.CorruptionLevel);
            BB.Set("Sanctions", e.Universe.SanctionLevel);
            BB.Set("Inflation", (float)e.Economy.Inflation);
            BB.Set("TurnsToElection", e.TurnUntilElection);
            BB.Set("PoliticalCapital", e.PoliticalCapital);
            BB.Set("PersonalityType", Personality.Type);
        }

        /// <summary>BT ağacını oluşturur — Selector + UtilitySelector hibrit.</summary>
        private void BuildTree()
        {
            // ═══════════════════════════════════════════════════════
            // KÖK: Selector — önce acil durumlar, sonra normal karar
            // ═══════════════════════════════════════════════════════
            _root = new Selector("ROOT",
                BuildEmergencyBranch(),   // 1. Acil kriz müdahalesi
                BuildElectionBranch(),    // 2. Seçim yaklaşıyorsa
                BuildNormalBranch()       // 3. Normal yönetim
            );
        }

        /// <summary>1. ACİL DAL: Legitimacy düşük, Unrest yüksek, iflas riski.</summary>
        private BTNode BuildEmergencyBranch()
        {
            return new Sequence("EMERGENCY",
                new ConditionNode("IsEmergency", () =>
                {
                    float legit = BB.Get<float>("Legitimacy");
                    float unrest = BB.Get<float>("Unrest");
                    return legit < 30f || unrest > 70f;
                }),
                new UtilitySelectorNode("EmergencyAction", new List<(BTNode, Func<float>)>
                {
                    (new ActionNode("Suppress", () => { /* askeri bastırma */ return NodeState.SUCCESS; }),
                     () => Personality.WeightStability * 100f),

                    (new ActionNode("Concede", () => { /* taviz ver */ return NodeState.SUCCESS; }),
                     () => Personality.PopulismTendency * 80f),

                    (new ActionNode("Surveillance", () => { /* gözetim artır */ return NodeState.SUCCESS; }),
                     () => Personality.Aggression * 60f)
                })
            );
        }

        /// <summary>2. SEÇİM DALI: Seçime 4 tur veya az kaldıysa.</summary>
        private BTNode BuildElectionBranch()
        {
            return new Sequence("ELECTION",
                new ConditionNode("ElectionNear", () =>
                    BB.Get<int>("TurnsToElection") <= 4),
                new UtilitySelectorNode("ElectionAction", new List<(BTNode, Func<float>)>
                {
                    (new ActionNode("Rally", () => { /* miting */ return NodeState.SUCCESS; }),
                     () => 80f),

                    (new ActionNode("PopulistPromise", () => { /* vaat */ return NodeState.SUCCESS; }),
                     () => Personality.PopulismTendency * 100f)
                })
            );
        }

        /// <summary>3. NORMAL DAL: Rutin yönetim — Utility AI puanlar.</summary>
        private BTNode BuildNormalBranch()
        {
            return new UtilitySelectorNode("NormalAction", new List<(BTNode, Func<float>)>
            {
                (new ActionNode("ProposePolicy", () => ProposeBestPolicy()),
                 () => ScorePolicyProposal()),

                (new ActionNode("ImproveDiplomacy", () => { /* diplomasi */ return NodeState.SUCCESS; }),
                 () => ScoreDiplomacy()),

                (new ActionNode("InvestIntel", () => { /* istihbarat */ return NodeState.SUCCESS; }),
                 () => ScoreIntel())
            });
        }

        private NodeState ProposeBestPolicy()
        {
            // Mevcut UtilityAI1 skorlamasını kullan
            float urgency = UtilityAI.ScoreAction(
                BB.Get<float>("Legitimacy"),
                BB.Get<float>("Unrest"),
                BB.Get<float>("GDP"),
                BB.Get<float>("Corruption"),
                BB.Get<float>("Army"),
                BB.Get<float>("Sanctions"),
                Personality.Type);

            SimLogger.Log($"[AIBrain {CountryId}] Politika öneriliyor (aciliyet: {urgency:F2})");
            return NodeState.SUCCESS;
        }

        private float ScorePolicyProposal()
        {
            return UtilityAI.ScoreAction(
                BB.Get<float>("Legitimacy"),
                BB.Get<float>("Unrest"),
                BB.Get<float>("GDP"),
                BB.Get<float>("Corruption"),
                BB.Get<float>("Army"),
                BB.Get<float>("Sanctions"),
                Personality.Type) * 100f;
        }

        private float ScoreDiplomacy()
        {
            // Yaptırım yüksekse diplomasiye ağırlık ver
            return BB.Get<float>("Sanctions") * 0.8f + 20f;
        }

        private float ScoreIntel()
        {
            // Huzursuzluk yüksekse istihbarata ağırlık ver
            return BB.Get<float>("Unrest") * 0.6f + 10f;
        }
    }
}