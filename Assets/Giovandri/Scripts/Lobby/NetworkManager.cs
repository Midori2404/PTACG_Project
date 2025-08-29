using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using Photon.Realtime;
using TMPro;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Menu UI")]
    public GameObject MenuUIPanel;
    public GameObject SettingsUIPanel;

    [Header("Login UI")]
    public GameObject LoginUIPanel;
    public TMP_InputField PlayerNameInput;

    [Header("Connecting Info Panel")]
    public GameObject ConnectingInfoUIPanel;

    [Header("GameOptions Panel")]
    public GameObject GameOptionsUIPanel;

    [Header("Create Room Panel")]
    public GameObject CreateRoomUIPanel;
    public TMP_InputField RoomNameInputField;
    public string GameMode = "EnchantedForest_e";
    public int maxPlayers = 2;

    [Header("Creating Room Info Panel")]
    public GameObject CreatingRoomInfoUIPanel;

    [Header("Inside Room Panel")]
    public GameObject InsideRoomUIPanel;
    public TMP_Text RoomInfoText;
    public GameObject PlayerListPrefab;
    public GameObject PlayerListContent;
    public GameObject StartGameButton;
    public TMP_Text GameModeText;
    public Image PanelBackground;
    public Sprite[] LevelsPreview;

    [Header("Room List UI Panel")]
    public GameObject RoomList_UI_Panel;
    public GameObject roomListEntryPrefab;
    public GameObject roomListParentGameobject;

    [Header("Join Random Room Panel")]
    public GameObject JoinRandomRoomUIPanel;

    private Dictionary<string, RoomInfo> cachedRoomList;
    private Dictionary<string, GameObject> roomListGameobjects;

    public Dictionary<int, GameObject> playerListGameObjects;

    #region UNITY Methods
    // Start is called before the first frame update
    void Start()
    {
        ActivatePanel(MenuUIPanel.name);

        cachedRoomList = new Dictionary<string, RoomInfo>();
        roomListGameobjects = new Dictionary<string, GameObject>();

        PhotonNetwork.AutomaticallySyncScene = true;
    }


    #endregion

    #region UI Callback Methods
    public void OnLoginButtonClicked()
    {
        string playerName = PlayerNameInput.text;
        if (!string.IsNullOrEmpty(playerName))
        {
            ActivatePanel(ConnectingInfoUIPanel.name);
            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.LocalPlayer.NickName = playerName;
                PhotonNetwork.ConnectUsingSettings();
            }
        }
        else
        {
            Debug.Log("Player name is invalid");
        }
    }

    public void OnLogoutButtonClicked()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect(); // Disconnect from Photon server
        }
        Debug.Log("Player is logging out");
    }

    public void OnCancelButtonClicked()
    {
        ActivatePanel(GameOptionsUIPanel.name);
    }

    public void OnBackToMenuButtonClick()
    {
        ActivatePanel(MenuUIPanel.name);
    }

    public void OnCreateRoomButtonClicked()
    {
        ActivatePanel(CreatingRoomInfoUIPanel.name);
        if (GameMode != null)
        {
            string roomName = RoomNameInputField.text;
            if (string.IsNullOrEmpty(roomName))
            {
                roomName = "Room" + Random.Range(1000, 10000);
            }
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = maxPlayers;
            string[] roomPropsInLobby = { "gm" }; //gm = game mode

            //two game modes
            //1. racing = "rc"
            //2. death race = "dr"

            ExitGames.Client.Photon.Hashtable customRoomProperties = new ExitGames.Client.Photon.Hashtable() { { "gm", GameMode } };

            roomOptions.CustomRoomPropertiesForLobby = roomPropsInLobby;
            roomOptions.CustomRoomProperties = customRoomProperties;

            PhotonNetwork.CreateRoom(roomName, roomOptions);
        }
    }
    public void OnJoinGameButtonClicked()
    {
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }

        ActivatePanel(RoomList_UI_Panel.name);
    }

    public void OnJoinRandomRoomButtonClicked(string _gameMode)
    {
        GameMode = _gameMode;
        ExitGames.Client.Photon.Hashtable expectedCustomRoomProperties = new ExitGames.Client.Photon.Hashtable() { { "gm", _gameMode } };
        PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, 0);
    }

    public void OnBackButtonClicked()
    {
        ActivatePanel(GameOptionsUIPanel.name);
    }

    public void OnLeaveGameButtonClicked()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void OnStartGameButtonClicked()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("gm"))
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsValue("EnchantedForest_e"))
            {
                PhotonNetwork.LoadLevel("EnchantedForest_e");
            }
            else if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsValue("EnchantedForest_h"))
            {
                PhotonNetwork.LoadLevel("EnchantedForest_h");
            }
            else if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsValue("Woodland_e"))
            {
                PhotonNetwork.LoadLevel("Woodland_e");
            }
            else if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsValue("Woodland_h"))
            {
                PhotonNetwork.LoadLevel("Woodland_h");
            }
            else if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsValue("Everfrost_e"))
            {
                PhotonNetwork.LoadLevel("Everfrost_e");
            }
            else if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsValue("Everfrost_h"))
            {
                PhotonNetwork.LoadLevel("Everfrost_h");
            }
            else if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsValue("AbandonedRuins_e"))
            {
                PhotonNetwork.LoadLevel("AbandonedRuins_e");
            }
            else if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsValue("AbandonedRuins_h"))
            {
                PhotonNetwork.LoadLevel("AbandonedRuins_h");
            }
        }
    }

    #endregion

    #region Photon Callbacks
    public override void OnConnected()
    {
        Debug.Log("We connected to internet");
    }

    public override void OnConnectedToMaster()
    {
        ActivatePanel(GameOptionsUIPanel.name);
        Debug.Log(PhotonNetwork.LocalPlayer.NickName + " is connected to Photon.");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        PlayerNameInput.text = ""; // Clear input field
        PhotonNetwork.LocalPlayer.NickName = ""; // Reset nickname
        ActivatePanel(LoginUIPanel.name); // Ensure UI updates after disconnect
        Debug.Log($"Disconnected from Photon: {cause}");
    }

    public override void OnCreatedRoom()
    {
        Debug.Log(PhotonNetwork.CurrentRoom.Name + " is created.");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log(PhotonNetwork.LocalPlayer.NickName + " joined to "+ PhotonNetwork.CurrentRoom.Name+ "Player count:"+   
                   PhotonNetwork.CurrentRoom.PlayerCount);

        ActivatePanel(InsideRoomUIPanel.name);
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("gm"))
        {
            RoomInfoText.text = "Room name: " + PhotonNetwork.CurrentRoom.Name + " " +
                " Players/Max.Players: " +
                PhotonNetwork.CurrentRoom.PlayerCount + " / " +
                PhotonNetwork.CurrentRoom.MaxPlayers;

            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsValue("EnchantedForest_e"))
            {
                GameModeText.text = "Enchanted Forest (Easy)";
                PanelBackground.sprite = LevelsPreview[0];
            }
            else if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsValue("EnchantedForest_h"))
            {
                GameModeText.text = "Enchanted Forest (Hard)";
                PanelBackground.sprite = LevelsPreview[1];
            }
            else if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsValue("Woodland_e"))
            {
                GameModeText.text = "Woodland (Easy)";
                PanelBackground.sprite = LevelsPreview[2];
            }
            else if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsValue("Woodland_h"))
            {
                GameModeText.text = "Woodland (Hard)";
                PanelBackground.sprite = LevelsPreview[3];
            }
            else if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsValue("Everfrost_e"))
            {
                GameModeText.text = "Everfrost (Easy)";
                PanelBackground.sprite = LevelsPreview[4];
            }
            else if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsValue("Everfrost_h"))
            {
                GameModeText.text = "Everfrost (Hard)";
                PanelBackground.sprite = LevelsPreview[5];
            }
            else if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsValue("AbandonedRuins_e"))
            {
                GameModeText.text = "Abandoned Ruins (Easy)";
                PanelBackground.sprite = LevelsPreview[6];
            }
            else if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsValue("AbandonedRuins_h"))
            {
                GameModeText.text = "Abandoned Ruins (Hard)";
                PanelBackground.sprite = LevelsPreview[7];
            }

            if (playerListGameObjects == null)
            {
                playerListGameObjects = new Dictionary<int, GameObject>();
            }

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                GameObject playerListGameObject = Instantiate(PlayerListPrefab);
                playerListGameObject.transform.SetParent(PlayerListContent.transform);
                RectTransform rectTransform = playerListGameObject.GetComponent<RectTransform>();

                rectTransform.anchoredPosition3D = Vector3.zero; // Reset position relative to parent
                rectTransform.localRotation = Quaternion.identity; // Reset rotation
                rectTransform.localScale = Vector3.one; // Reset scale

                playerListGameObject.GetComponent<PlayerListEntryInitializer>().Initialize(player.ActorNumber, player.NickName);

                object isPlayerReady;
                if (player.CustomProperties.TryGetValue(MultiplayerRoguelike.PLAYER_READY, out isPlayerReady)) //CHANGE
                {
                    playerListGameObject.GetComponent<PlayerListEntryInitializer>().SetPlayerReady((bool)isPlayerReady);
                }

                playerListGameObjects.Add(player.ActorNumber, playerListGameObject);
            }
        }
        StartGameButton.SetActive(false);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        ClearRoomListView();

        foreach (RoomInfo room in roomList)
        {
            Debug.Log(room.Name);

            if (!room.IsOpen || !room.IsVisible || room.RemovedFromList)
            {
                if (cachedRoomList.ContainsKey(room.Name))
                {
                    cachedRoomList.Remove(room.Name);
                }
            }
            else
            {
                //update cachedRoom list
                if (cachedRoomList.ContainsKey(room.Name))
                {
                    cachedRoomList[room.Name] = room;
                }
                //add the new room to the cached room list
                else
                {
                    cachedRoomList.Add(room.Name, room);
                }
            }
        }

        foreach (RoomInfo room in cachedRoomList.Values)
        {
            GameObject roomListEntryGameobject = Instantiate(roomListEntryPrefab);
            roomListEntryGameobject.transform.SetParent(roomListParentGameobject.transform);

            RectTransform rectTransform = roomListEntryGameobject.GetComponent<RectTransform>();

            rectTransform.anchoredPosition3D = Vector3.zero; // Reset position relative to parent
            rectTransform.localRotation = Quaternion.identity; // Reset rotation
            rectTransform.localScale = Vector3.one; // Reset scale

            roomListEntryGameobject.transform.Find("RoomNameText").GetComponent<TMP_Text>().text = room.Name;
            roomListEntryGameobject.transform.Find("RoomPlayersText").GetComponent<TMP_Text>().text = room.PlayerCount + " / " + room.MaxPlayers;
            roomListEntryGameobject.transform.Find("JoinRoomButton").GetComponent<Button>().onClick.AddListener(() => OnJoinRoomButtonClicked(room.Name));

            roomListGameobjects.Add(room.Name, roomListEntryGameobject);
        }
    }

    public override void OnLeftLobby()
    {
        ClearRoomListView();
        cachedRoomList.Clear();
    }

    public override void OnPlayerPropertiesUpdate(Player target, ExitGames.Client.Photon.Hashtable changedProps)
    {
        GameObject playerListGameObject;
        if (playerListGameObjects.TryGetValue(target.ActorNumber, out playerListGameObject))
        {
            object isPlayerReady;
            if (changedProps.TryGetValue(MultiplayerRoguelike.PLAYER_READY, out isPlayerReady)) //CHANGE
            {
                playerListGameObject.GetComponent<PlayerListEntryInitializer>().SetPlayerReady((bool)isPlayerReady);
            }
        }
        StartGameButton.SetActive(CheckPlayersReady());
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        RoomInfoText.text = "Room name: " + PhotonNetwork.CurrentRoom.Name + " " +
                  " Players/Max.Players: " +
                  PhotonNetwork.CurrentRoom.PlayerCount + " / " +
                  PhotonNetwork.CurrentRoom.MaxPlayers;

        GameObject playerListGameObject = Instantiate(PlayerListPrefab);
        playerListGameObject.transform.SetParent(PlayerListContent.transform);

        RectTransform rectTransform = playerListGameObject.GetComponent<RectTransform>();

        rectTransform.anchoredPosition3D = Vector3.zero; // Reset position relative to parent
        rectTransform.localRotation = Quaternion.identity; // Reset rotation
        rectTransform.localScale = Vector3.one; // Reset scale

        playerListGameObject.GetComponent<PlayerListEntryInitializer>().Initialize(newPlayer.ActorNumber, newPlayer.NickName);

        playerListGameObjects.Add(newPlayer.ActorNumber, playerListGameObject);

        StartGameButton.SetActive(CheckPlayersReady());
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RoomInfoText.text = "Room name: " + PhotonNetwork.CurrentRoom.Name + " " +
                " Players/Max.Players: " +
                PhotonNetwork.CurrentRoom.PlayerCount + " / " +
                PhotonNetwork.CurrentRoom.MaxPlayers;

        Destroy(playerListGameObjects[otherPlayer.ActorNumber].gameObject);
        playerListGameObjects.Remove(otherPlayer.ActorNumber);
        StartGameButton.SetActive(CheckPlayersReady());
    }

    public override void OnLeftRoom()
    {
        ActivatePanel(GameOptionsUIPanel.name);

        foreach (GameObject playerListGameobject in playerListGameObjects.Values)
        {
            Destroy(playerListGameobject);
        }
        playerListGameObjects.Clear();
        playerListGameObjects = null;
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == newMasterClient.ActorNumber)
        {
            StartGameButton.SetActive(CheckPlayersReady());
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log(message);

        //if there is no room, create one
        if (GameMode != null)
        {
            string roomName = RoomNameInputField.text;
            if (string.IsNullOrEmpty(roomName))
            {
                roomName = "Room" + Random.Range(1000, 10000);
            }
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = maxPlayers;
            string[] roomPropsInLobby = { "gm" }; //gm = game mode

            //two game modes
            //1. racing = "rc"
            //2. death race = "dr"

            ExitGames.Client.Photon.Hashtable customRoomProperties = new ExitGames.Client.Photon.Hashtable() { { "gm", GameMode } };

            roomOptions.CustomRoomPropertiesForLobby = roomPropsInLobby;
            roomOptions.CustomRoomProperties = customRoomProperties;

            PhotonNetwork.CreateRoom(roomName, roomOptions);
        }

    }

    #endregion

    #region Public Methods
    public void ActivatePanel(string panelNameToBeActivated)
    {
        MenuUIPanel.SetActive(MenuUIPanel.name.Equals(panelNameToBeActivated));
        SettingsUIPanel.SetActive(SettingsUIPanel.name.Equals(panelNameToBeActivated));
        LoginUIPanel.SetActive(LoginUIPanel.name.Equals(panelNameToBeActivated));
        ConnectingInfoUIPanel.SetActive(ConnectingInfoUIPanel.name.Equals(panelNameToBeActivated));
        CreatingRoomInfoUIPanel.SetActive(CreatingRoomInfoUIPanel.name.Equals(panelNameToBeActivated));
        CreateRoomUIPanel.SetActive(CreateRoomUIPanel.name.Equals(panelNameToBeActivated));
        GameOptionsUIPanel.SetActive(GameOptionsUIPanel.name.Equals(panelNameToBeActivated));
        JoinRandomRoomUIPanel.SetActive(JoinRandomRoomUIPanel.name.Equals(panelNameToBeActivated));
        InsideRoomUIPanel.SetActive(InsideRoomUIPanel.name.Equals(panelNameToBeActivated));
    }

    public void SetGameMode(int index)
    {

        switch (index)
        {
            case 0: GameMode = "EnchantedForest_e"; break;
            case 1: GameMode = "EnchantedForest_h"; break;
            case 2: GameMode = "Woodland_e"; break;
            case 3: GameMode = "Woodland_h"; break;
            case 4: GameMode = "Everfrost_e"; break;
            case 5: GameMode = "Everfrost_h"; break;
            case 6: GameMode = "AbandonedRuins_e"; break;
            case 7: GameMode = "AbandonedRuins_h"; break;
        }
    }

    #endregion

    #region Private Methods
    private bool CheckPlayersReady()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return false;
        }
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            object isPlayerReady;
            if (player.CustomProperties.TryGetValue(MultiplayerRoguelike.PLAYER_READY, out isPlayerReady)) //CHANGE
            {
                if (!(bool)isPlayerReady)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        return true;
    }
    private void OnJoinRoomButtonClicked(string _roomName)
    {
        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
        }

        PhotonNetwork.JoinRoom(_roomName);
    }

    private void ClearRoomListView()
    {
        foreach (var roomListGameobject in roomListGameobjects.Values)
        {
            Destroy(roomListGameobject);
        }

        roomListGameobjects.Clear();
    }

    #endregion

}

