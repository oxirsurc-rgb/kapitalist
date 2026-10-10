using UnityEngine;
using System.Collections.Generic;

namespace DemocracySim.Engine.Core
{
    public enum NodeState { RUNNING, SUCCESS, FAILURE }

    public abstract class BTNode
    {
        public abstract NodeState Evaluate();
    }

    public class Sequence : BTNode
    {
        protected List<BTNode> nodes = new List<BTNode>();
        public Sequence(List<BTNode> nodes) { this.nodes = nodes; }
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
    }

    public class Selector : BTNode
    {
        protected List<BTNode> nodes = new List<BTNode>();
        public Selector(List<BTNode> nodes) { this.nodes = nodes; }
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
    }

    public class ActionNode : BTNode
    {
        private System.Func<NodeState> action;
        public ActionNode(System.Func<NodeState> action) { this.action = action; }
        public override NodeState Evaluate() => action();
    }
}
