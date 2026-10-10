using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public void StartHost()
    {
        if (NetworkManager.singleton == null)
        {
            Debug.LogError("[Net] NetworkManager bulunamadı!");
            return;
        }
        NetworkManager.singleton.StartHost();
        Debug.Log("[Net] Host başladı. Port: 7777");
    }

    public void StartClient(string ip = "localhost")
    {
        if (NetworkManager.singleton == null)
        {
            Debug.LogError("[Net] NetworkManager bulunamadı!");
            return;
        }
        NetworkManager.singleton.networkAddress = ip;
        NetworkManager.singleton.StartClient();
        Debug.Log($"[Net] Client bağlanıyor: {ip}");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[Net] Sunucu hazır.");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("[Net] Client bağlandı.");
    }
}