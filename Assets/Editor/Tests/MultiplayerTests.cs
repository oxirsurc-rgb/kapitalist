using NUnit.Framework;
using DemocracySim.Engine.Core;
using DemocracySim.Engine.Core.Commands;
using DemocracySim.Engine.Core.Multiplayer;

namespace DemocracySim.Tests
{
    /// <summary>
    /// FAZ 23: Multiplayer altyapı testleri.
    /// </summary>
    public class MultiplayerTests
    {
        [Test]
        public void CommandQueue_OrdersDeterministically()
        {
            var q = new CommandQueue();
            q.Enqueue(new SimCommand(1, "usa", CommandType.ProposePolicy, "tax_income"));
            q.Enqueue(new SimCommand(1, "chn", CommandType.ProposePolicy, "tax_corporate"));
            q.Enqueue(new SimCommand(1, "usa", CommandType.NextTurn));

            var turn1 = q.GetForTurn(1);
            Assert.AreEqual(3, turn1.Count);
            // Sıra: chn → usa(NextTurn) → usa(ProposePolicy)
            Assert.AreEqual("chn", turn1[0].CountryId);
            Assert.AreEqual("usa", turn1[1].CountryId);
            Assert.AreEqual(CommandType.ProposePolicy, turn1[1].Type);  
            Assert.AreEqual(CommandType.NextTurn, turn1[2].Type);
        }

        [Test]
        public void Lockstep_AllPlayersReady_TriggersSimulation()
        {
            var lockstep = new LockstepManager();
            lockstep.StartGame(new System.Collections.Generic.List<string> { "usa", "chn" }, 1234u);

            Assert.IsFalse(lockstep.AllPlayersReady());

            lockstep.SubmitCommand(new SimCommand(0, "usa", CommandType.NextTurn));
            Assert.IsFalse(lockstep.AllPlayersReady());

            lockstep.SubmitCommand(new SimCommand(0, "chn", CommandType.NextTurn));
            Assert.IsTrue(lockstep.AllPlayersReady(), "Tüm oyuncular hazır olmalı");
        }

        [Test]
        public void Replay_SerializeDeserialize_RoundTrip()
        {
            var q1 = new CommandQueue();
            q1.Enqueue(new SimCommand(0, "usa", CommandType.ProposePolicy, "tax_income"));
            q1.Enqueue(new SimCommand(1, "chn", CommandType.NextTurn));
            string json = q1.Serialize();

            var q2 = new CommandQueue();
            q2.Deserialize(json);

            Assert.AreEqual(q1.Count, q2.Count);
            Assert.AreEqual(q1.All[0].CountryId, q2.All[0].CountryId);
            Assert.AreEqual(q1.All[1].Type, q2.All[1].Type);
        }
    }
}