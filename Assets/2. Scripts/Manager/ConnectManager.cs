using Unity.Netcode;
using UnityEngine;

public class ConnectManager : NetworkBehaviour
{
    
    void Awake()
    {
        NetworkManager.Singleton.StartHost();
    }

}
