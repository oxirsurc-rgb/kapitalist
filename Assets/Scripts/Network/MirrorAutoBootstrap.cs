using UnityEngine;
using Mirror;
using DemocracySim.Engine.Core.Multiplayer;
using kcp2k;

/// <summary>
/// FAZ 23.3: Mirror'ı kod içinde otomatik başlatır.
/// Sahneye hiçbir şey eklemeye gerek yok — bu component sahnede tek başına yeter.
/// </summary>
public class MirrorAutoBootstrap : MonoBehaviour
{
    private static bool _initialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (_initialized) return;
        _initialized = true;

        var go = new GameObject("[MirrorAutoBootstrap]");
        DontDestroyOnLoad(go);
        go.AddComponent<MirrorAutoBootstrap>();
    }

    void Awake()
    {
                // 1) NetworkManager yoksa oluştur
        if (NetworkManager.singleton == null)
        {
            // Kritik: GameObject'i ÖNCE inactive yap
            // yoksa AddComponent<NetworkManager>() çağrıldığı an Awake() çalışır ve
            // transport henüz eklenmemiş olur → hata.
            var nmGO = new GameObject("[NetworkManager]");
            nmGO.SetActive(false);              // ← ÖNCE DEAKTİF ET
            DontDestroyOnLoad(nmGO);

            // 1.a) Transport'u ÖNCE ekle
            var transport = nmGO.AddComponent<KcpTransport>();

            // 1.b) NetworkManager'ı SONRA ekle
            var nm = nmGO.AddComponent<NetworkManager>();

            // 1.c) Transport'u ata
            nm.transport = transport;

            // 1.d) Player prefab = boş bir GameObject
            var playerGO = new GameObject("NetworkPlayer");
            playerGO.AddComponent<NetworkIdentity>();
            playerGO.SetActive(false);
            DontDestroyOnLoad(playerGO);
            nm.playerPrefab = playerGO;

            // 1.e) Şimdi aktif et → Awake tetiklenir, transport hazır
            nmGO.SetActive(true);

            Debug.Log("[Mirror] NetworkManager otomatik oluşturuldu.");
        }

        // 2) NetworkGameManager ekle
        if (NetworkGameManager.Instance == null)
        {
            gameObject.AddComponent<NetworkGameManager>();
        }

        // 3) LockstepManager'ı hazırla
        if (LockstepManagerProxy.Manager == null)
        {
            LockstepManagerProxy.Manager = new LockstepManager();
            Debug.Log("[Mirror] LockstepManager hazır.");
        }

        Debug.Log("[Mirror] Hazır. Host için 'H' tuşuna, Client için 'C' tuşuna bas.");
    }

    // Test için klavye kısayolları (sonra silebilirsin)
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            NetworkManager.singleton.StartHost();
            Debug.Log("[Mirror] HOST başladı. Port: 7777");
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            NetworkManager.singleton.StartClient();
            Debug.Log("[Mirror] CLIENT başladı.");
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            NetworkManager.singleton.StopHost();
            NetworkManager.singleton.StopClient();
            Debug.Log("[Mirror] Durduruldu.");
        }
    }
}