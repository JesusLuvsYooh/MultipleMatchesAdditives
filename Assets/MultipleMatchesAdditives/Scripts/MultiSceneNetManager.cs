using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

namespace MultipleMatchesAdditives
{
    [AddComponentMenu("")]
    public class MultiSceneNetManager : NetworkManager
    {
        public CanvasController canvasController;
        public NetworkAuthenticatorCustom networkAuthenticatorCustom;
        public GameObject canvasLoader;

        [Header("Spawner Setup")]
        [Tooltip("Reward Prefab for the Spawner")]
        public GameObject rewardPrefab;

        [Header("MultiScene Setup")]
        public int instances = 3;
        public int instancesCurrent = 0;
        public int maxPlayersPerMatch = 2;
        public int maxMatches = 10;

        [Scene]
        //public string gameScene;
        public string[] sceneArray;

        // This is set true after server loads all subscene instances
        bool subscenesLoaded;
        public bool subSceneHasSpace = false;
        public int subSceneWithSpaceID = -1;
        public int subSceneMatchID = -1;
        public int subSceneMap = -1;

        // subscenes are added to this list as they're loaded
        public List<SubSceneList> subSceneList = new List<SubSceneList>();

        private PlayerList currentPlayerFromList;
        //public List<SubSceneList> subSceneListFiltered = new List<SubSceneList>();
        private SubSceneList matchedSubSceneFromList;

        public List<PlayerList> playerList = new List<PlayerList>();

        public static new MultiSceneNetManager singleton { get; private set; }

        /// <summary>
        /// Runs on both Server and Client
        /// Networking is NOT initialized when this fires
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            singleton = this;
            networkAuthenticatorCustom = GetComponent<NetworkAuthenticatorCustom>();
            canvasController = FindObjectOfType<CanvasController>();
        }

        #region Server System Callbacks

        /// <summary>
        /// Called on the server when a client adds a new player with NetworkClient.AddPlayer.
        /// <para>The default implementation for this function creates a new player object from the playerPrefab.</para>
        /// </summary>
        /// <param name="conn">Connection from client.</param>
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            StartCoroutine(OnServerAddPlayerDelayed(conn));
            canvasController.RefreshHUD();
        }

        // This delay is mostly for the host player that loads too fast for the
        // server to have subscenes async loaded from OnStartServer ahead of it.
        IEnumerator OnServerAddPlayerDelayed(NetworkConnectionToClient conn)
        {
            subSceneHasSpace = false;
            subSceneWithSpaceID = -1;
            subscenesLoaded = false;
            subSceneMatchID = -1;
            subSceneMap = -1;

            foreach (PlayerList _playerList in playerList)
            {
                if (_playerList.connectionId == conn.connectionId)
                {
                    currentPlayerFromList = _playerList;
                    //Debug.Log(gameObject.name + " - Found matching player.");
                    break;
                }
            }

           // Debug.Log(gameObject.name + " - currentPlayerFromList.playerMatchID:" + currentPlayerFromList.playerMatchID);
            if (currentPlayerFromList.playerMatchID > 0)
            {
                subSceneMatchID = currentPlayerFromList.playerMatchID;
                //Debug.Log(gameObject.name + " - currentPlayerFromList.playerMatchID > 0.");
            }

            if (currentPlayerFromList.playerMap > 0 && currentPlayerFromList.playerMap < sceneArray.Length)
            {
                subSceneMap = currentPlayerFromList.playerMap;
                //Debug.Log(gameObject.name + " - currentPlayerFromList.playerMap > 0 && currentPlayerFromList.playerMap < sceneArray.Length.");
            }
            //Debug.Log(gameObject.name + " - subSceneList count: " + subSceneList.Count);
            foreach (SubSceneList _subSceneList in subSceneList)
            {
                if (//_subSceneList.sceneMatchID == subSceneMatchID &&
                    (_subSceneList.sceneMatchID == currentPlayerFromList.playerMatchID || currentPlayerFromList.playerMatchID <= 0) &&
                    _subSceneList.playerCount < _subSceneList.playerCountMax &&
                    _subSceneList.playerCount < maxPlayersPerMatch &&
                    (_subSceneList.subSceneNumber == currentPlayerFromList.playerMap || currentPlayerFromList.playerMap <= 0))
                {
                    subSceneHasSpace = true;
                    subSceneWithSpaceID = _subSceneList.sceneMatchID;
                    subSceneMap = _subSceneList.subSceneNumber;
                    currentPlayerFromList.playerMatchID = _subSceneList.sceneMatchID;
                    currentPlayerFromList.playerMap = _subSceneList.subSceneNumber;
                    // matchedSubSceneFromList = _subSceneList;
                    //Debug.Log(gameObject.name + " - foreach _subSceneList: " + _subSceneList.subSceneNumber);
                    break;
                }
            }
            if (subSceneHasSpace)
            {
                subscenesLoaded = true;
                Debug.Log(gameObject.name + " - subSceneHasSpace");
            }
            else
            {
                Debug.Log(gameObject.name + " - ServerCreateSubScene");
                StartCoroutine(ServerCreateSubScene(conn));
            }
            // wait for server to async load all subscenes for game instances
            while (!subscenesLoaded)
            {
                yield return null;
            }
            // Send Scene message to client to additively load the game scene
            conn.Send(new SceneMessage { sceneName = sceneArray[subSceneMap], sceneOperation = SceneOperation.LoadAdditive });

            // Wait for end of frame before adding the player to ensure Scene Message goes first
            yield return new WaitForEndOfFrame();

            base.OnServerAddPlayer(conn);

            PlayerScore playerScore = conn.identity.GetComponent<PlayerScore>();
            playerScore.playerNumber = conn.connectionId; //clientIndex;
            playerScore.scoreIndex = 0; //clientIndex / subScenes.Count;
            playerScore.matchIndex = subSceneWithSpaceID; //clientIndex % subScenes.Count;
            //MatchManager matchManager = FindObjectOfType<MatchManager>();

            // Do this only on server, not on clients
            // This is what allows the NetworkSceneChecker on player and scene objects
            // to isolate matches per scene instance on server.
            //print("subScenes.Count : " + subScenes.Count);
            if (subSceneList.Count > 0)
            {
                //SceneManager.MoveGameObjectToScene(conn.identity.gameObject, matchedSubSceneFromList.subScene);
                //matchedSubSceneFromList.playerCount += 1;
                foreach (SubSceneList _subSceneList in subSceneList)
                {
                    if (_subSceneList.sceneMatchID == subSceneWithSpaceID)
                    {
                        SceneManager.MoveGameObjectToScene(conn.identity.gameObject, _subSceneList.subScene);
                        _subSceneList.playerCount += 1;
                        break;
                    }
                }
            }
            //Debug.Log(gameObject.name + " - Added player.");
            canvasController.RefreshMatchList();
            canvasController.RefreshHUD();
        }

        #endregion

        #region Start & Stop Callbacks

        /// <summary>
        /// This is invoked when a server is started - including when a host is started.
        /// <para>StartServer has multiple signatures, but they all cause this hook to be called.</para>
        /// </summary>
        public override void OnStartServer()
        {
            // StartCoroutine(ServerLoadSubScenes());
            canvasController.RefreshHUD();
        }


        public bool HasSpaceInServer()
        {
            if (subSceneList.Count > 0)
            {
                foreach (SubSceneList _scene in subSceneList)
                {
                    if (_scene.playerCount < maxPlayersPerMatch)
                    {
                        Debug.Log(gameObject.name + " - Space in Match.");
                        return true;
                    }
                }

                if (subSceneList.Count >= maxMatches)
                {
                    Debug.Log(gameObject.name + " - No spaces in Match.");
                    return false;
                }
            }
            return true;
        }

        IEnumerator ServerCreateSubScene(NetworkConnectionToClient conn)
        {
            if (currentPlayerFromList.playerMap > 0 && currentPlayerFromList.playerMap < sceneArray.Length)
            {
                subSceneMap = currentPlayerFromList.playerMap;
                Debug.Log(gameObject.name + " - currentPlayerFromList.playerMap > 0 && currentPlayerFromList.playerMap < sceneArray.Length.");
            }
            else// if (subSceneMap <= 0 || subSceneMap >= sceneArray.Length)
            {
                subSceneMap = UnityEngine.Random.Range(1, sceneArray.Length);
                //Debug.Log(gameObject.name + " - else rando.");
            }

            yield return SceneManager.LoadSceneAsync(sceneArray[subSceneMap], new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics3D });

            Scene _newScene = SceneManager.GetSceneAt(subSceneList.Count + 1);
            subSceneList.Add(new SubSceneList()
            {
                sceneMatchID = UnityEngine.Random.Range(0, int.MaxValue),
                subScene = _newScene,
                playerCount = 0,
                playerCountMax = maxPlayersPerMatch,
                subSceneNumber = subSceneMap
            });
            Spawner.InitialSpawn(_newScene);
            subSceneWithSpaceID = subSceneList[subSceneList.Count - 1].sceneMatchID;
            subSceneHasSpace = true;

            currentPlayerFromList.playerMatchID = subSceneWithSpaceID;
            currentPlayerFromList.playerMap = subSceneMap;

            subscenesLoaded = true;
            canvasController.UpdateStatusText("Server created subscene.");
            canvasController.RefreshHUD();
        }

        /// <summary>
        /// This is called when a server is stopped - including when a host is stopped.
        /// </summary>
        public override void OnStopServer()
        {
            //if (subSceneMap <= 0 || subSceneMap >= sceneArray.Length)
            //{
            //    subSceneMap = UnityEngine.Random.Range(1, sceneArray.Length);
            //}
            //Scene _newScene = SceneManager.GetSceneAt(subSceneList.Count + 1);
            //SceneManager.sceneCount - 1
            // NetworkServer.SendToAll(new SceneMessage { sceneName = sceneArray[subSceneMap], sceneOperation = SceneOperation.UnloadAdditive });
            //NetworkServer.SendToAll(new SceneMessage { sceneName = sceneArray[subSceneMap], sceneOperation = SceneOperation.UnloadAdditive });
            StartCoroutine(ServerUnloadSubScenes());
            canvasController.RefreshHUD();
        }

        // Unload the subScenes and unused assets and clear the subScenes list.
        IEnumerator ServerUnloadSubScenes()
        {
            for (int index = 0; index < subSceneList.Count; index++)
                if (subSceneList[index].subScene.IsValid())
                    yield return SceneManager.UnloadSceneAsync(subSceneList[index].subScene);

            subSceneList.Clear();
            subscenesLoaded = false;

            yield return Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// This is called when a client is stopped.
        /// </summary>
        public override void OnStopClient()
        {
            // Make sure we're not in ServerOnly mode now after stopping host client
            if (mode == NetworkManagerMode.Offline)
            {
                StartCoroutine(ClientUnloadSubScenes());
            }
            canvasController.RefreshHUD();
        }

        // Unload all but the active scene, which is the "container" scene
        IEnumerator ClientUnloadSubScenes()
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
                if (SceneManager.GetSceneAt(index) != SceneManager.GetActiveScene())
                    yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(index));
        }

        #endregion

        /// <summary>
        /// Called on the server when a client disconnects.
        /// <para>This is called on the Server when a Client disconnects from the Server. Use an override to decide what should happen when a disconnection is detected.</para>
        /// </summary>
        /// <param name="conn">Connection from client.</param>
        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            StartCoroutine(HandleDisconnect(conn.connectionId));

            base.OnServerDisconnect(conn);

            canvasController.RefreshHUD();
        }

        IEnumerator HandleDisconnect(int _connectionId)
        {
            Debug.Log("Client disconnected connectionId: " + _connectionId);
            canvasController.UpdateStatusText("Disconnected.");

            int playersMatchIDLocation = -1;

            foreach (PlayerList _playerList in playerList)
            {
                if (_playerList.connectionId == _connectionId)
                {
                    playersMatchIDLocation = _playerList.playerMatchID;
                    playerList.Remove(_playerList);
                    break;
                }
            }

            Debug.Log("Decrease or Remove subscene playersMatchIDLocation: " + playersMatchIDLocation);
            foreach (SubSceneList _subSceneList in subSceneList)
            {
                if (_subSceneList.sceneMatchID == playersMatchIDLocation)
                {
                    _subSceneList.playerCount -= 1;
                    if (_subSceneList.playerCount <= 0)
                    {
                        yield return SceneManager.UnloadSceneAsync(_subSceneList.subScene);
                        yield return Resources.UnloadUnusedAssets();
                        subSceneList.Remove(_subSceneList);
                    }
                    break;
                }
            }
            canvasController.RefreshMatchList();
        }

        /// <summary>
        /// Called on the server when a scene is completed loaded, when the scene load was initiated by the server with ServerChangeScene().
        /// </summary>
        /// <param name="sceneName">The name of the new scene.</param>
        public override void OnServerSceneChanged(string sceneName)
        {
            canvasController.RefreshHUD();
        }

        /// <summary>
        /// Called on clients when a scene has completed loaded, when the scene load was initiated by the server.
        /// <para>Scene changes can cause player objects to be destroyed. The default implementation of OnClientSceneChanged in the NetworkManager is to add a player object for the connection if no player object exists.</para>
        /// </summary>
        public override void OnClientSceneChanged()
        {
            base.OnClientSceneChanged();
            canvasController.RefreshHUD();
        }

        /// <summary>
        /// Called on the server when a new client connects.
        /// <para>Unity calls this on the Server when a Client connects to the Server. Use an override to tell the NetworkManager what to do when a client connects to the server.</para>
        /// </summary>
        /// <param name="conn">Connection from client.</param>
        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            canvasController.RefreshHUD();
        }

        /// <summary>
        /// Called on the server when a client is ready.
        /// <para>The default implementation of this function calls NetworkServer.SetClientReady() to continue the network setup process.</para>
        /// </summary>
        /// <param name="conn">Connection from client.</param>
        public override void OnServerReady(NetworkConnectionToClient conn)
        {
            base.OnServerReady(conn);
            canvasController.RefreshHUD();
        }

        /// <summary>
        /// Called on the client when connected to a server.
        /// <para>The default implementation of this function sets the client as ready and adds a player. Override the function to dictate what happens when the client connects.</para>
        /// </summary>
        public override void OnClientConnect()
        {
            base.OnClientConnect();
            canvasController.RefreshHUD();
        }

        /// <summary>
        /// Called on clients when disconnected from a server.
        /// <para>This is called on the client when it disconnects from the server. Override this function to decide what happens when the client disconnects.</para>
        /// </summary>
        public override void OnClientDisconnect()
        {
            canvasController.RefreshHUD();
        }

        /// <summary>
        /// Called on clients when a servers tells the client it is no longer ready.
        /// <para>This is commonly used when switching scenes.</para>
        /// </summary>
        public override void OnClientNotReady()
        {
            canvasController.RefreshHUD();
        }

        /// <summary>
        /// Called on client when transport raises an error.</summary>
        /// </summary>
        /// <param name="transportError">TransportError enum.</param>
        /// <param name="message">String message of the error.</param>
        public override void OnClientError(TransportError transportError, string message)
        {
            canvasController.RefreshHUD();
        }

        /// <summary>
        /// This is invoked when the client is started.
        /// </summary>
        public override void OnStartClient()
        {
            canvasController.RefreshHUD();
        }


    }
}
