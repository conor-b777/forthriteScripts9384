using UnityEngine;
using Unity.Netcode;

public class NetDebug : NetworkBehaviour
{ 
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.H))
        {
            Debug.Log("Hostname: " + NetworkManager.ConnectedHostname);
        }
    }
}
