namespace DemocracySim.Engine.Core
{
    /// <summary>
    /// FAZ R (Motor Refactor): Her alt sistem bu arayüzü uygular.
    /// SimulationEngine.ProcessTurn() artık bu pipeline'ı çağırır.
    /// </summary>
    public interface ITurnSystem
    {
        /// <summary>Sistemin adı (log ve debug için).</summary>
        string SystemName { get; }

        /// <summary>Çalışma önceliği. Düşük = önce çalışır.</summary>
        int Priority { get; }

        /// <summary>Her turda çağrılır.</summary>
        void ProcessTurn(SimulationEngine engine);
    }
}