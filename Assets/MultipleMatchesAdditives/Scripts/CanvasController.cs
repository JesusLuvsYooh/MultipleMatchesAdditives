using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace MultipleMatchesAdditives
{
    public class CanvasController : MonoBehaviour
    {
        public MultiSceneNetManager networkManager;

        [Header("GUI References")]
        public GameObject matchList;
        public GameObject matchPrefab;
        public Button createButton;
        public Button joinButton;
        public ToggleGroup toggleGroup;
        public InputField inputFieldServerID;
        public Button exitButton;
        public GameObject ListGroup;
        public GameObject ServerGroup;
        public GameObject ClientGroup;
        public GameObject OtherGroup;
        public Text statusText;
        public Toggle hudToggle;
        public Dropdown mapDropdown;
        public GameObject offlineCamera;

        /// <summary>
        /// GUID of a match the local player has selected in the Toggle Group match list
        /// </summary>
        private int selectedMatch = 0;
        //internal int selectedMap = 0;

        public void Start()
        {
            networkManager = FindObjectOfType<MultiSceneNetManager>();
            networkManager.canvasController = this;

            inputFieldServerID.onValueChanged.AddListener(delegate { OnInputFieldServerIDChanged(); });
            hudToggle.onValueChanged.AddListener(delegate { OnHUDToggleClicked(); });
            mapDropdown.onValueChanged.AddListener(delegate { MapDropdownChanged(); });

            RefreshMatchList();
            //RefreshHUD();
        }

        public void OnHUDToggleClicked()
        {
            //canvasController.SelectMatch(toggleButton.isOn ? matchId : 0);
            hudToggle.GetComponent<Image>().color = hudToggle.isOn ? new Color(0f, 0f, 0f, 1f) : new Color(0.172549f, 0.172549f, 0.172549f, 1f);
            RefreshHUD();
        }

        public void MapDropdownChanged()
        {
            networkManager.networkAuthenticatorCustom.mapRequested = mapDropdown.value;
            RefreshHUD();
        }

        private void OnInputFieldServerIDChanged()
        {
            int.TryParse(inputFieldServerID.text, out networkManager.networkAuthenticatorCustom.matchId);
            //Debug.Log(gameObject.name + ": inputfield match id-" + networkManager.networkAuthenticatorCustom.matchId);
            if (inputFieldServerID.text != "")
            {
                selectedMatch = networkManager.networkAuthenticatorCustom.matchId;
                joinButton.interactable = true;
            }
            else
            {
                selectedMatch = 0;
                networkManager.networkAuthenticatorCustom.matchId = 0;
                joinButton.interactable = false;
            }
        }

        public void SelectMatch(int matchId)
        {
            Debug.Log(gameObject.name + ": SelectMatch 1-" + matchId);
            if (matchId == 0)
            {
                selectedMatch = 0;
                joinButton.interactable = false;
                Debug.Log(gameObject.name + ": SelectMatch 2-" + matchId);
            }
            else
            {
                foreach (SubSceneList _subSceneList in networkManager.subSceneList)
                {
                    if (_subSceneList.sceneMatchID == matchId)
                    {
                        selectedMatch = matchId;
                        joinButton.interactable = _subSceneList.playerCount < _subSceneList.playerCountMax;
                        break;
                    }
                }
                Debug.Log(gameObject.name + ": SelectMatch 3-" + matchId);
            }
            inputFieldServerID.text = selectedMatch.ToString();
            RefreshHUD();
        }

        public void RequestJoinMatch()
        {
            Debug.Log(gameObject.name + ": RequestJoinMatch:" + selectedMatch);
            UpdateStatusText("Requested to join: " + selectedMatch + ".");
            if (selectedMatch == 0)
                return;
            networkManager.networkAuthenticatorCustom.JoinMatch(selectedMatch);
            RefreshHUD();
        }

        public void RefreshMatchList()
        {
            foreach (Transform child in matchList.transform)
                Destroy(child.gameObject);

            joinButton.interactable = false;

            GameObject newMatch = Instantiate(matchPrefab, Vector3.zero, Quaternion.identity);
            newMatch.transform.SetParent(matchList.transform, false);
            newMatch.GetComponent<Image>().color -= new Color(0f, 0f, 0f, 1f);

            foreach (SubSceneList _subSceneList in networkManager.subSceneList)
            {
                newMatch = Instantiate(matchPrefab, Vector3.zero, Quaternion.identity);
                newMatch.transform.SetParent(matchList.transform, false);
                newMatch.GetComponent<MatchGUI>().SetMatchInfo(_subSceneList);

                Toggle toggle = newMatch.GetComponent<Toggle>();
                toggle.enabled = true;
                toggle.interactable = true;
                toggle = newMatch.GetComponent<Toggle>();
                toggle.group = toggleGroup;

                if (_subSceneList.sceneMatchID == selectedMatch)
                {
                    toggle.isOn = true;
                }
            }
            //Debug.Log(gameObject.name + ": RefreshMatchList.");
            RefreshHUD();
        }

        public void ButtonGetList()
        {
            Instantiate(networkManager.canvasLoader);
            networkManager.networkAuthenticatorCustom.requestingList = true;
            networkManager.StartClient();
            RefreshHUD();
        }

        public void ButtonMatchmake()
        {
            Instantiate(networkManager.canvasLoader);
            networkManager.networkAuthenticatorCustom.requestingList = false;
            networkManager.StartClient();
            RefreshHUD();
        }

        public void ButtonExit()
        {
            Instantiate(networkManager.canvasLoader);
            networkManager.networkAuthenticatorCustom.requestingList = false;
            networkManager.StopHost();
            RefreshHUD();
        }

        public void ButtonStartServer()
        {
            Instantiate(networkManager.canvasLoader);
            networkManager.networkAuthenticatorCustom.requestingList = false;
            networkManager.StartServer();
            RefreshHUD();
        }

        public void ButtonStartHost()
        {
            Instantiate(networkManager.canvasLoader);
            networkManager.networkAuthenticatorCustom.requestingList = false;
            networkManager.StartHost();
            RefreshHUD();
        }

        public void UpdateStatusText(string _text)
        {
            if (statusText.text.Length > 100)
            {
                statusText.text = "";
            }
            statusText.text += _text + " ";
        }

        public void RefreshHUD()
        {
            //Debug.Log(gameObject.name + ": RefreshHUD");
            ListGroup.SetActive(false);
            ServerGroup.SetActive(false);
            ClientGroup.SetActive(false);
            OtherGroup.SetActive(false);
            exitButton.gameObject.SetActive(false);

            if (networkManager == null) return;

            if (hudToggle.isOn)
            {
                OtherGroup.SetActive(true);
                if (networkManager.subSceneList.Count > 0 || NetworkServer.active)
                {
                    ListGroup.SetActive(true);
                    //refreshButton.gameObject.SetActive(true);
                    //statusText.text = "List displayed. ";
                    //Debug.Log(gameObject.name + ": List displayed.");
                }

                if (!NetworkClient.active && !NetworkServer.active)
                {
                    ClientGroup.SetActive(true);
                    //statusText.text = " Client ready. ";
                   // Debug.Log(gameObject.name + ": Client Ready.");
                }

                if (!NetworkServer.active && !NetworkClient.active)
                {
#if UNITY_EDITOR || UNITY_SERVER
                    ServerGroup.SetActive(true);
                    //statusText.text = " Server ready. ";
                    //Debug.Log(gameObject.name + ": Server Ready.");
#endif
                }

                if (NetworkServer.active || NetworkClient.active)
                {
                    exitButton.gameObject.SetActive(true);
                }

                if (!NetworkServer.active && !NetworkClient.isConnected)
                {
                    if (NetworkClient.active)
                    {
                        //UpdateStatusText("Connecting to " + NetworkManager.singleton.networkAddress + ".");
                        //statusText.text = "Connecting to " + NetworkManager.singleton.networkAddress + "..";
                    }
                }
                else
                {
                    // Host
                    if (NetworkServer.active && NetworkClient.active)
                    {
                        //statusText.text += " Host ready.";
                       // Debug.Log(gameObject.name + ": Host.");
                    }
                    // Server only
                    else if (NetworkServer.active)
                    {
                        //statusText.text = " Server ";
                        //Debug.Log(gameObject.name + ": Server.");
                    }
                    // Client only
                    else if (NetworkClient.isConnected)
                    {
                        //UpdateStatusText("Connected to "+NetworkManager.singleton.networkAddress +" via "+Transport.active+".");
                        //statusText.text = $" Client: connected to {NetworkManager.singleton.networkAddress} via {Transport.active}";
                    }
                }
            }
        }
    }
}
