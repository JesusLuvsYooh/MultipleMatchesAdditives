using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

namespace MultipleMatchesAdditives
{
    public class MatchManager : NetworkBehaviour
    {
        //public GameObject sceneObjects;
        public GameObject clientObjectsToInstantiate;
        private GameObject clientObjectsReference;
        public MultiSceneNetManager networkManager;

        /// <summary>
        /// Called on every NetworkBehaviour when it is activated on a client.
        /// <para>Objects on the host have this function called, as there is a local client on the host. The values of SyncVars on object are guaranteed to be initialized correctly with the latest state from the server when this function is called on the client.</para>
        /// </summary>
        public override void OnStartClient()
        {
            // work around to stop player host from instantiating on each subscene creation
           // Debug.Log("MatchManager OnStartClient");

            if (NetworkManager.singleton.mode == NetworkManagerMode.Host)
            {
                // Debug.Log("OnStartClient Host");
                networkManager = NetworkManager.singleton.GetComponent<MultiSceneNetManager>();

                if (networkManager.subSceneList[0].sceneMatchID == networkManager.playerList[0].playerMatchID)
                {
                    //Debug.Log("OnStartClient is Host and belongs to this Match.");
                    SetupMap();
                }
            }
            else
            {
                SetupMap();
            }
        }

        public void Awake()
        {
            //Debug.Log("MatchManager Awake");

            if (NetworkManager.singleton.mode == NetworkManagerMode.Host)
            {
                // Debug.Log("OnStartClient Host");
                networkManager = NetworkManager.singleton.GetComponent<MultiSceneNetManager>();

                if (networkManager.subSceneList.Count > 0 && networkManager.playerList.Count > 0 && networkManager.subSceneList[0].sceneMatchID == networkManager.playerList[0].playerMatchID)
                {
                    //Debug.Log("OnStartClient is Host and belongs to this Match.");
                    SetupMap();
                }
            }
            else
            {
                SetupMap();
            }
        }

        private void SetupMap()
        {
            //Debug.Log("OnStartClient SetupMap");
            networkManager.canvasController.offlineCamera.SetActive(false);

            //clientObjectsReference = Instantiate(clientObjectsToInstantiate);
            //SceneManager.MoveGameObjectToScene(clientObjectsReference, SceneManager.GetSceneAt(SceneManager.sceneCount - 1));

            //sceneObjects.SetActive(true);
        }
    }
}