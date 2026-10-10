using DemocracySim.Engine.Core.Commands;
using DemocracySim.Engine.Core.Multiplayer;

/// <summary>
/// FAZ 23.3: Ağ katmanı ile LockstepManager arasında köprü.
/// NetworkCommandSync ve MirrorAutoBootstrap bu proxy üzerinden Manager'a erişir.
/// </summary>
public static class LockstepManagerProxy
{
    public static LockstepManager Manager;

    public static void SubmitCommand(SimCommand cmd)
    {
        Manager?.SubmitCommand(cmd);
    }
}