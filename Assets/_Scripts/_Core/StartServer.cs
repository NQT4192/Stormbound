using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class StartServer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
#if UNITY_SERVER
        Debug.Log("Running as headless server............... ");
        NetworkManager.singleton.StartServer();
#endif
    }
}