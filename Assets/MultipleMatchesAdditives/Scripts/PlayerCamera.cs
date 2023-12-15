using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Collections;

// This sets up the scene camera for the local player

namespace MultipleMatchesAdditives
{
    public class PlayerCamera : NetworkBehaviour
    {
        private Camera mainCam;
        private Camera offlineCam;
        public MultiSceneNetManager networkManager;

        public override void OnStartLocalPlayer()
        {
            //SetupCamera();
            //mainCam = Camera.main;
            StartCoroutine(DelayedSetup());
            
        }

        public override void OnStopLocalPlayer()
        {
            if (offlineCam != null)
            {
                offlineCam.gameObject.SetActive(true);
                //mainCam.transform.SetParent(null);
                //SceneManager.MoveGameObjectToScene(mainCam.gameObject, SceneManager.GetActiveScene());
                //mainCam.orthographic = true;
                //mainCam.orthographicSize = 15f;
                //mainCam.transform.localPosition = new Vector3(0f, 70f, 0f);
                //mainCam.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            }
        }

        IEnumerator DelayedSetup()
        {
            yield return new WaitForSeconds(1.0f);
            SetupCamera();
        }

        private void SetupCamera()
        {
            //if (mainCam == null)
            //{
            //mainCam = Camera.main;
            //mainCam = FindObjectOfType<MatchManager>().cameraObject.GetComponent<Camera>();

            if (networkManager == null)
            {
                networkManager = FindObjectOfType<MultiSceneNetManager>();
                offlineCam = networkManager.canvasController.offlineCamera.GetComponent<Camera>();
                mainCam = FindObjectOfType<MatchManager>().cameraObject.GetComponent<Camera>();
                offlineCam.gameObject.SetActive(false);
            }
            //}

            if (mainCam != null)
            {
                // configure and make camera a child of player with 3rd person offset
                mainCam.orthographic = false;
                mainCam.transform.SetParent(transform);
                mainCam.transform.localPosition = new Vector3(0f, 3f, -8f);
                mainCam.transform.localEulerAngles = new Vector3(10f, 0f, 0f);
            }
        }
    }
}
