using System;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
//using static MultipleMatchesAdditives.MultiSceneNetManager;

namespace MultipleMatchesAdditives
{
    public class MatchGUI : MonoBehaviour
    {
        int matchId;
        private MultiSceneNetManager networkManager;
        [Header("GUI Elements")]
        public Image image;
        public Toggle toggleButton;
        public Text matchID;
        public Text matchScene;
        public Text playerCount;

        public CanvasController canvasController;

        public void Awake()
        {
            canvasController = FindObjectOfType<CanvasController>();
            networkManager = canvasController.networkManager;
            toggleButton.onValueChanged.AddListener(delegate { OnToggleClicked(); });
        }

        public void OnToggleClicked()
        {
            canvasController.SelectMatch(toggleButton.isOn ? matchId : 0);
            image.color = toggleButton.isOn ? new Color(0f, 0f, 0f, 1f) : new Color(0.172549f, 0.172549f, 0.172549f, 1f);
        }

        public int GetMatchId() => matchId;

        public void SetMatchInfo(SubSceneList infos)
        {
            matchId = infos.sceneMatchID;
            matchID.text = $"{infos.sceneMatchID}";
            if (infos.subScene.name.Length > 15)
            {
                matchScene.text = ".." + infos.subScene.name[15..];
            }
            else
            {
                matchScene.text = $"{infos.subScene.name}";
            }
            playerCount.text = $"{infos.playerCount}/{ infos.playerCountMax}";
        }
    }
}
