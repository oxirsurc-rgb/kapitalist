using UnityEngine;
using System.Collections.Generic;

namespace DemocracySim.Engine.Core
{
    // ═══════════════════════════════════════════════════════════════
    // FAZ 17: Behavior Tree — Production-Ready
    // Blackboard destekli, Decorator'lı, hibrit AI için hazır
    // ═══════════════════════════════════════════════════════════════

    public enum NodeState { RUNNING, SUCCESS, FAILURE }

    /// <summary>BT düğümleri arasında paylaşılan veri (Blackboard deseni).</summary>
    public class Blackboard
    {
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>();

        public void Set<T>(string key, T value) => _data[key] = value;

        public T Get<T>(string key, T fallback = default)
        {
            return _data.TryGetValue(key, out var v) && v is T t ? t : fallback;
        }

        public bool Has(string key) => _data.ContainsKey(key);
        public void Clear() => _data.Clear();
    }

    // ═══════════════════════════════════════════════════════════════
    // TEMEL DÜĞÜMLER
    // ═══════════════════════════════════════════════════════════════

    public abstract class BTNode
    {
        public string Name { get; set; }
        protected Blackboard BB { get; set; }

        public void SetBlackboard(Blackboard bb) { BB = bb; }

        public abstract NodeState Evaluate();
        public virtual void Reset() { }
    }

    /// <summary>Sequence: Tüm çocuklar başarılıysa SUCCESS. Biri fail ederse FAILURE.</summary>
    public class Sequence : BTNode
    {
        protected List<BTNode> nodes;

        public Sequence(string name, params BTNode[] children)
        {
            Name = name;
            nodes = new List<BTNode>(children);
            foreach (var n in nodes) n.SetBlackboard(null); // BB sonradan set edilir
        }

        public override NodeState Evaluate()
        {
            foreach (var node in nodes)
            {
                switch (node.Evaluate())
                {
                    case NodeState.FAILURE: return NodeState.FAILURE;
                    case NodeState.RUNNING: return NodeState.RUNNING;
                }
            }
            return NodeState.SUCCESS;
        }

        public override void Reset()
        {
            foreach (var n in nodes) n.Reset();
        }
    }

    /// <summary>Selector: Çocuklardan biri başarılıysa SUCCESS. Hepsi fail ederse FAILURE.</summary>
    public class Selector : BTNode
    {
        protected List<BTNode> nodes;

        public Selector(string name, params BTNode[] children)
        {
            Name = name;
            nodes = new List<BTNode>(children);
        }

        public override NodeState Evaluate()
        {
            foreach (var node in nodes)
            {
                switch (node.Evaluate())
                {
                    case NodeState.SUCCESS: return NodeState.SUCCESS;
                    case NodeState.RUNNING: return NodeState.RUNNING;
                }
            }
            return NodeState.FAILURE;
        }

        public override void Reset()
        {
            foreach (var n in nodes) n.Reset();
        }
    }

    /// <summary>Action: Leaf düğüm — bir eylem çalıştırır.</summary>
    public class ActionNode : BTNode
    {
        private readonly System.Func<NodeState> _action;

        public ActionNode(string name, System.Func<NodeState> action)
        {
            Name = name;
            _action = action;
        }

        public override NodeState Evaluate() => _action?.Invoke() ?? NodeState.FAILURE;
    }

    // ═══════════════════════════════════════════════════════════════
    // DECORATOR'LAR
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Bir koşul doğruysa çocuğu çalıştırır.</summary>
    public class ConditionNode : BTNode
    {
        private readonly System.Func<bool> _condition;

        public ConditionNode(string name, System.Func<bool> condition)
        {
            Name = name;
            _condition = condition;
        }

        public override NodeState Evaluate()
        {
            return _condition?.Invoke() == true ? NodeState.SUCCESS : NodeState.FAILURE;
        }
    }

    /// <summary>Çocuğun sonucunu tersine çevirir.</summary>
    public class InverterNode : BTNode
    {
        private readonly BTNode _child;

        public InverterNode(BTNode child) { _child = child; }

        public override NodeState Evaluate()
        {
            var result = _child.Evaluate();
            if (result == NodeState.SUCCESS) return NodeState.FAILURE;
            if (result == NodeState.FAILURE) return NodeState.SUCCESS;
            return NodeState.RUNNING;
        }
    }

    /// <summary>Utility AI selector — çocukları puanlayıp en yükseği seçer.</summary>
    public class UtilitySelectorNode : BTNode
    {
        private readonly List<(BTNode node, System.Func<float> scorer)> _options;

        public UtilitySelectorNode(string name, List<(BTNode, System.Func<float>)> options)
        {
            Name = name;
            _options = options;
        }

        public override NodeState Evaluate()
        {
            BTNode best = null;
            float bestScore = float.MinValue;

            foreach (var (node, scorer) in _options)
            {
                float score = scorer?.Invoke() ?? 0f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = node;
                }
            }

            if (best == null) return NodeState.FAILURE;

            // Log: hangi seçenek seçildi
            SimLogger.Log($"[BT] {Name} → {best.Name} (skor: {bestScore:F2})");

            return best.Evaluate();
        }

        public override void Reset()
        {
            foreach (var (node, _) in _options) node.Reset();
        }
    }
}