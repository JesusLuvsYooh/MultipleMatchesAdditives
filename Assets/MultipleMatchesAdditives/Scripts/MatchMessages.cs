using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine.SceneManagement;

namespace MultipleMatchesAdditives
{
    [Serializable]
    public class SubSceneList
    {
        public int sceneMatchID;
        public Scene subScene;
        public int playerCount;
        public int playerCountMax;
        public int subSceneNumber;
    }

    [Serializable]
    public class PlayerList
    {
        public int connectionId;
        public int playerMatchID;
        public int playerMap;
    }

    public enum AuthOperation : byte
    {
        List,
        Reject,
        Accept,
        Join
    }

    public struct AuthRequestMessage : NetworkMessage
    {
        public AuthOperation authOperation;
        public float clientRequestVersion;
        public int clientMatchID;
        public int clientRequestMap;
    }

    public struct AuthResponseMessage : NetworkMessage
    {
        public AuthOperation authOperation;
        public byte serverResponseStatusNumber;
        public List<SubSceneList> subSceneListAuth;
    }

    
}