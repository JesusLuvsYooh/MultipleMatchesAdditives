// www.StephenAllenGames.co.uk  JesusLuvsYooh

using System;
using System.Collections;
using UnityEngine;
using Mirror;
using kcp2k;
using MAS;
using UnityEngine.Assertions.Must;

namespace MultipleMatchesAdditives
{

    public class HeadlessArgs : MonoBehaviour
    {
#if UNITY_SERVER && !UNITY_EDITOR

    private NetworkAuthenticatorCustom NA;
    private MultiSceneNetManager NM;
    private KcpTransport KCP;
    private string[] clArgs;

    private string GetArgValue(string name)
    {
        for (int i = 0; i < clArgs.Length; i++)
        {
            if (clArgs[i] == name && clArgs.Length > i + 1)
            {
                if (!clArgs[i + 1].StartsWith("-"))
                {
                    return clArgs[i + 1];
                }
            }
        }
        return null;
    }
    private void Start()
    {
        //Debug.Log("file s/c framerate ip port");
        Debug.Log(gameObject.name + ": CLA starting.");

        // grab these for future reference
        NM = NetworkManager.singleton.GetComponent<MultiSceneNetManager>();
        NA = NM.GetComponent<NetworkAuthenticatorCustom>();
        KCP = NM.GetComponent<KcpTransport>();

        NetworkManager.singleton.autoStartServerBuild = false;

        clArgs = Environment.GetCommandLineArgs();

        if (clArgs == null || clArgs.Length <= 1 || string.IsNullOrWhiteSpace(GetArgValue("-mode")))
        {
            Debug.Log(gameObject.name + ": No CLA, start default server.");
            NetworkManager.singleton.StartServer();
        }
        else
        {
            Debug.Log(gameObject.name + ": frame");
            if (!string.IsNullOrWhiteSpace(GetArgValue("-frameRate")))
            {
                Application.targetFrameRate = int.Parse(GetArgValue("-frameRate"));
                NetworkManager.singleton.sendRate = int.Parse(GetArgValue("-frameRate"));
            }
            
            Debug.Log(gameObject.name + ": port");
            if (!string.IsNullOrWhiteSpace(GetArgValue("-port")))
            {
                Debug.Log(gameObject.name + ":" + GetArgValue("-port")+"-");
                KCP.Port = ushort.Parse(GetArgValue("-port"));
            }
            
            Debug.Log(gameObject.name + ": mode");
            // we keep this for last, to allow setup first
            if (GetArgValue("-mode") == "server")
            {
                Debug.Log(gameObject.name + ": maxConnections");
                if (!string.IsNullOrWhiteSpace(GetArgValue("-maxConnections")))
                {
                    NA.maxClientConnections = int.Parse(GetArgValue("-maxConnections"));
                }
                Debug.Log(gameObject.name + ": maxMatches");
                if (!string.IsNullOrWhiteSpace(GetArgValue("-maxMatches")))
                {
                    NM.maxMatches = int.Parse(GetArgValue("-maxMatches"));
                }
                Debug.Log(gameObject.name + ": maxPlayersPerMatch");
                if (!string.IsNullOrWhiteSpace(GetArgValue("-maxPlayersPerMatch")))
                {
                    NM.maxPlayersPerMatch = int.Parse(GetArgValue("-maxPlayersPerMatch"));
                }

                NetworkManager.singleton.StartServer();
                Debug.Log(gameObject.name + ": Started Server");
            }
            else if (GetArgValue("-mode") == "client")
            {
                Debug.Log(gameObject.name + ": ip");
                if (!string.IsNullOrWhiteSpace(GetArgValue("-ip")))
                {
                    NetworkManager.singleton.networkAddress = GetArgValue("-ip");
                }

                // random delay for client, to stop too many connections at same time
                StartCoroutine(StartClientHeadless());
            }
            
        }
    }

    IEnumerator StartClientHeadless()
    {
        Debug.Log(gameObject.name + ": StartClientHeadless");
        yield return new WaitForSeconds(UnityEngine.Random.Range(0.0f, 3.0f));
        NetworkManager.singleton.StartClient();
        Debug.Log(gameObject.name + ": Started Client");
    }
#endif
    }
}