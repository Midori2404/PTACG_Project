using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.Demo.PunBasics;


public class GameManager : MonoBehaviourPunCallbacks
{
    [SerializeField] PlayerUIManager playerUIManager;
    public List<GameObject> activePlayers = new List<GameObject>();
    public static GameManager Instance { get; private set; }
    private bool gameOverTriggered = false;

    private void Awake()
    {
        Instance = this;
        playerUIManager = FindObjectOfType<PlayerUIManager>();
    }

    // void Start()
    // {
    //     if (PhotonNetwork.IsConnectedAndReady)
    //     {
    //         if (playerPrefab != null)
    //         {
    //             int randomPoint = Random.Range(-20, 20);

    //             // Get the prefab name with its relative path inside Resources
    //             string prefabPath = "Player/" + playerPrefab.name;

    //             // var player = PhotonNetwork.Instantiate(prefabPath, new Vector3(randomPoint, 25, randomPoint), Quaternion.identity);
    //             // activePlayers.Add(player);
    //         }
    //     }
    // }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            foreach (var player in PhotonNetwork.PlayerList)
            {
                Debug.Log("Player: " + player.NickName);
            }
        }
    }

    public void InitializePlayerClass(int playerID)
    {
        if (PhotonNetwork.CurrentRoom.Players.TryGetValue(playerID, out Player player))
        {
            if (player.CustomProperties.TryGetValue(MultiplayerRoguelike.PLAYER_SELECTION_CODE, out object playerClass))
            {
                Debug.LogWarning("Player " + player.NickName + " has chosen " + playerClass);

                // Find the player GameObject using the OwnerActorNr property
                GameObject playerObject = activePlayers.Find(p => p.GetComponentInChildren<PhotonView>().OwnerActorNr == playerID);

                if (playerObject != null)
                {
                    PlayerAttribute playerAttribute = playerObject.GetComponentInChildren<PlayerAttribute>();
                    if (playerAttribute != null)
                    {
                        playerAttribute.InitializeStat(playerClass.ToString());
                    }
                    else
                    {
                        Debug.LogError("PlayerAttribute component not found on player GameObject.");
                    }
                }
                else
                {
                    Debug.LogError("Player GameObject not found for player ID: " + playerID);
                }
            }
            else
            {
                Debug.LogError("Player class not found for player: " + player.NickName);
            }
        }
        else
        {
            Debug.LogError("Player not found with ID: " + playerID);
        }
    }

    public void CheckAllPlayersFallen()
    {
        if (gameOverTriggered) return;

        // Dynamically grab all players in the scene.
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        bool allFallen = true;

        // If there are no players (shouldn't normally happen), we can also trigger game over.
        if (players.Length == 0)
        {
            allFallen = true;
        }
        else
        {
            foreach (GameObject player in players)
            {
                FallenSpectate fs = player.GetComponent<FallenSpectate>();
                if (fs != null)
                {
                    // If any player is not fallen, we set the flag to false and break.
                    if (!fs.IsFallen)
                    {
                        allFallen = false;
                        break;
                    }
                }
                else
                {
                    // If a player doesn't have a FallenSpectate component, assume they are active.
                    allFallen = false;
                    break;
                }
            }
        }

        if (allFallen)
        {
            Debug.Log("All players have fallen. Triggering Game Over.");
            ShowGameOverPanel();
            gameOverTriggered = true;
        }
    }


    private void SavePlayerData(GameObject player)
    {
        // Save player data to the database
    }

    public void ShowVictoryScreen()
    {
        playerUIManager.ShowVictoryScreen();
    }

    public void ShowGameOverPanel()
    {
        playerUIManager.ShowGameOver();
    }

}
