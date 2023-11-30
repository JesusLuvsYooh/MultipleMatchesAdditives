using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
//using static MultipleMatchesAdditives.MultiSceneNetManager;

/*
    Documentation: https://mirror-networking.gitbook.io/docs/components/network-authenticators
    API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkAuthenticator.html
*/
namespace MultipleMatchesAdditives
{
    public class NetworkAuthenticatorCustom : NetworkAuthenticator
    {
        
        [Tooltip("Reject if not using same version number, increase when previous releases will not work with current release.")]
        public float majorVersionNumber = 0;

        [Tooltip("Max overall server connections limit, set NetworkManager connections to a higher number, such as maxClientConnections+1 to reserve space for authentication features.")]
        public int maxClientConnections = 100;

        private byte responseStatusNumber = 0;
        private bool rejectConnection = false;
        public bool requestingList = false;
        public int matchId = 0;
        public int mapRequested = 0;

        private MultiSceneNetManager networkManager;

        public List<SubSceneList> subSceneListNew = new List<SubSceneList>();

        readonly HashSet<NetworkConnection> connectionsPendingDisconnect = new HashSet<NetworkConnection>();

        #region Server

        public void Awake()
        {
            networkManager = GetComponent<MultiSceneNetManager>();
        }

        /// <summary>
        /// Called on server from StartServer to initialize the Authenticator
        /// <para>Server message handlers should be registered in this method.</para>
        /// </summary>
        public override void OnStartServer()
        {
            // register a handler for the authentication request we expect from client
            NetworkServer.RegisterHandler<AuthRequestMessage>(OnAuthRequestMessage, false);
            networkManager.canvasController.RefreshHUD();
        }

        /// <summary>
        /// Called on server from OnServerConnectInternal when a client needs to authenticate
        /// </summary>
        /// <param name="conn">Connection to client.</param>
        public override void OnServerAuthenticate(NetworkConnectionToClient conn)
        {
            networkManager.canvasController.RefreshHUD();
        }

        /// <summary>
        /// Called on server when the client's AuthRequestMessage arrives
        /// </summary>
        /// <param name="conn">Connection to client.</param>
        /// <param name="msg">The message payload</param>
        public void OnAuthRequestMessage(NetworkConnectionToClient conn, AuthRequestMessage msg)
        {
            Debug.Log("Authentication Request: " + msg.clientRequestVersion);
            // default as true incase anything is wrong
            rejectConnection = true;
            responseStatusNumber = 0;
            matchId = 0;

            if (connectionsPendingDisconnect.Contains(conn))
                return;

            if (NetworkServer.connections.Count - 1 < maxClientConnections && msg.clientRequestVersion == majorVersionNumber && networkManager.HasSpaceInServer())
            {
                // space available and version matches
                responseStatusNumber = 1;
                rejectConnection = false;
                networkManager.canvasController.UpdateStatusText("Space available and version matches.");
            }
            else if (NetworkServer.connections.Count >= maxClientConnections)
            {
                // no connection space
                responseStatusNumber = 9;
                rejectConnection = true;
                networkManager.canvasController.UpdateStatusText("No connection space.");
            }
            else if (networkManager.HasSpaceInServer() == false)
            {
                // no server space
                responseStatusNumber = 10;
                rejectConnection = true;
                networkManager.canvasController.UpdateStatusText("No server space.");
            }
            //else if (msg.clientRequestVersion == majorVersionNumber)
            //{
            //    // version same
            //    responseStatusNumber = 11;
            //    rejectConnection = false;
            //}
            else if (msg.clientRequestVersion < majorVersionNumber)
            {
                // client lower version
                responseStatusNumber = 12;
                rejectConnection = true;
                networkManager.canvasController.UpdateStatusText("Version out of date.");
            }
            else if (msg.clientRequestVersion > majorVersionNumber)
            {
                // client higher version
                responseStatusNumber = 13;
                rejectConnection = true;
                networkManager.canvasController.UpdateStatusText("Client version too new.");
            }
            else
            {
                // other error c
                responseStatusNumber = 0;
                rejectConnection = true;
                networkManager.canvasController.UpdateStatusText("Authentication Request Passed.");
            }
            
            Debug.Log("Authentication Request - responseStatusNumber: " + responseStatusNumber + " - rejectConnection: " + rejectConnection);

            if (rejectConnection)
            {
                connectionsPendingDisconnect.Add(conn);
                // create and send msg to client so it knows to disconnect
                AuthResponseMessage authResponseMessage = new AuthResponseMessage
                {
                    authOperation = AuthOperation.Reject,
                    serverResponseStatusNumber = responseStatusNumber
                    //, subSceneInfos = openSubScenes.Values.ToArray()
                };

                conn.Send(authResponseMessage);
                conn.isAuthenticated = false;
                // disconnect the client after 1 second so that response message gets delivered
                StartCoroutine(DelayedDisconnect(conn, 1f));
                networkManager.canvasController.UpdateStatusText("Authentication Request Rejected.");
                return;
            }

            switch (msg.authOperation)
            {
                case AuthOperation.Join:
                {
                        Debug.Log("AuthOperation.Join");

                        AuthResponseMessage authResponseMessage = new AuthResponseMessage
                        {
                            authOperation = AuthOperation.Accept,
                            serverResponseStatusNumber = responseStatusNumber
                        };
                        conn.Send(authResponseMessage);
                        ServerAccept(conn);

                        networkManager.playerList.Add(new PlayerList()
                        {
                            connectionId = conn.connectionId,
                            playerMatchID = msg.clientMatchID,
                            playerMap = msg.clientRequestMap
                        });
                        break;

                    }
                case AuthOperation.List:
                    {
                        subSceneListNew = networkManager.subSceneList;
                        Debug.Log("Authentication List openSubScenes count:" + subSceneListNew.Count);

                        AuthResponseMessage authResponseMessage = new AuthResponseMessage
                        {
                            authOperation = AuthOperation.List
                             , serverResponseStatusNumber = responseStatusNumber
                             , subSceneListAuth = networkManager.subSceneList
                        };
                        conn.Send(authResponseMessage);
                        conn.isAuthenticated = false;
                        StartCoroutine(DelayedDisconnect(conn, 1f));
                        break;
                    }
            }

            networkManager.canvasController.RefreshHUD();
        }

        IEnumerator DelayedDisconnect(NetworkConnectionToClient conn, float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            // Reject the unsuccessful authentication
            ServerReject(conn);
            yield return null;
            // remove conn from pending connections
            connectionsPendingDisconnect.Remove(conn);
            networkManager.canvasController.RefreshHUD();
        }

        /// <summary>
        /// Called when server stops, used to unregister message handlers if needed.
        /// </summary>
        public override void OnStopServer()
        {
            // Unregister the handler for the authentication request
            NetworkServer.UnregisterHandler<AuthRequestMessage>();
            networkManager.canvasController.RefreshHUD();
        }

        #endregion

        #region Client

        /// <summary>
        /// Called on client from StartClient to initialize the Authenticator
        /// <para>Client message handlers should be registered in this method.</para>
        /// </summary>
        public override void OnStartClient()
        {
            // register a handler for the authentication response we expect from server
            NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponseMessage, false);
            networkManager.canvasController.RefreshHUD();
        }

        public struct AuthResponseServerList : NetworkMessage
        {
            public SubSceneList subSceneList;
        }

        /// <summary>
        /// Called on client from OnClientConnectInternal when a client needs to authenticate
        /// </summary>
        public override void OnClientAuthenticate()
        {
            AuthRequestMessage authRequestMessage;
            if (requestingList)
            {
                authRequestMessage = new AuthRequestMessage
                {
                    authOperation = AuthOperation.List
                     , clientRequestVersion = majorVersionNumber
                };
            }
            else
            {
                authRequestMessage = new AuthRequestMessage
                {
                    authOperation = AuthOperation.Join
                    ,clientRequestVersion = majorVersionNumber,
                    clientMatchID = matchId,
                    clientRequestMap = mapRequested
                };
            }

            NetworkClient.connection.Send(authRequestMessage);
            requestingList = false;
            networkManager.canvasController.RefreshHUD();
        }

        /// <summary>
        /// Called on client when the server's AuthResponseMessage arrives
        /// </summary>
        /// <param name="msg">The message payload</param>
        public void OnAuthResponseMessage(AuthResponseMessage msg)
        {
            //Debug.Log($"Authentication Response N: {msg.serverResponseStatusNumber}");

            switch (msg.authOperation)
            {
                case AuthOperation.Accept:
                    {
                        Debug.Log("Authentication: space available and version matches");
                        networkManager.canvasController.UpdateStatusText("Space available and version matches.");
                        ClientAccept();
                        break;
                    }
                case AuthOperation.List:
                    {
                        Debug.Log("Authentication: auth op");
                        networkManager.subSceneList.Clear();
                        networkManager.subSceneList = msg.subSceneListAuth;
                        networkManager.canvasController.RefreshMatchList();
                        break;
                    }
                case AuthOperation.Reject:
                    {
                        Debug.Log("Authentication: auth Reject");
                        if (msg.serverResponseStatusNumber == 9)
                        {
                            Debug.Log("Authentication: No server space");
                            networkManager.canvasController.UpdateStatusText("No server space.");
                        }
                        else if (msg.serverResponseStatusNumber == 10)
                        {
                            Debug.Log("Authentication: No match space");
                            networkManager.canvasController.UpdateStatusText("No match space.");
                        }
                        //else if (msg.serverResponseStatusNumber == 11)
                        //{
                        //    Debug.Log("Authentication: Version same");
                        //    networkManager.canvasController.UpdateStatusText("Version matches.");
                        //}
                        else if (msg.serverResponseStatusNumber == 12)
                        {
                            Debug.Log("Authentication: Client lower version");
                            networkManager.canvasController.UpdateStatusText("Version too low.");
                        }
                        else if (msg.serverResponseStatusNumber == 13)
                        {
                            Debug.Log("Authentication: Client higher version");
                            networkManager.canvasController.UpdateStatusText("Version too high.");
                        }
                        else
                        {
                            Debug.Log("Authentication: Other rejection");
                            networkManager.canvasController.UpdateStatusText("Authentication Request Rejected.");
                        }
                        ClientReject();
                        break;
                    }
            }
            //Debug.Log("Authentication: End");
            networkManager.canvasController.RefreshHUD();
        }

        /// <summary>
        /// Called when client stops, used to unregister message handlers if needed.
        /// </summary>
        public override void OnStopClient()
        {
            Instantiate(networkManager.canvasLoader);
            // Unregister the handler for the authentication response
            NetworkClient.UnregisterHandler<AuthResponseMessage>();
            networkManager.canvasController.RefreshHUD();
        }

        #endregion

        public void JoinMatch(int _matchId)
        {
            Debug.Log(gameObject.name + ": RequestJoinMatch-" + _matchId);
            Instantiate(networkManager.canvasLoader);
            if (_matchId == 0)
                return;
            matchId = _matchId;
            networkManager.StartClient();
            networkManager.canvasController.RefreshHUD();
        }
    }

}