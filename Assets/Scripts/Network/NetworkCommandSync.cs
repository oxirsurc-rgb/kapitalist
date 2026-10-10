using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DemocracySim.Engine.Core.Commands;
using DemocracySim.Engine.Core.Multiplayer;

/// <summary>
/// FAZ 23.3: Lockstep P2P multiplayer komut senkronizasyonu.
/// Sunucu başladığında otomatik spawn edilir (NetworkManager.playerPrefab yerine).
/// </summary>
public class NetworkCommandSync : NetworkBehaviour
{
    public static NetworkCommandSync Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[Net] NetworkCommandSync: Sunucu tarafı hazır.");
        if (LockstepManagerProxy.Manager == null)
            LockstepManagerProxy.Manager = new LockstepManager();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("[Net] NetworkCommandSync: İstemci tarafı hazır.");
        if (!isServer && LockstepManagerProxy.Manager == null)
            LockstepManagerProxy.Manager = new LockstepManager();
    }

    /// <summary>Yerel oyuncu bir komut ürettiğinde çağır.</summary>
    [Command(requiresAuthority = false)]
    public void CmdSubmitCommand(int turn, string countryId, int cmdType, string targetId, float value)
    {
        var cmd = new SimCommand(turn, countryId, (CommandType)cmdType, targetId, value);
        LockstepManagerProxy.SubmitCommand(cmd);
        Debug.Log($"[Net] Sunucuya ulaştı: {cmd}");
    }
}