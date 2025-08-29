using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class SpawnPlayerManager : MonoBehaviour
{
    public GameObject[] PlayerPrefabs;
    public Transform[] InstantiatePositions;

    // Dictionary to map player selection codes to prefab indices
    private Dictionary<string, int> playerIndexMapping = new Dictionary<string, int>()
    {
        { PlayerClass.Warrior.ToString(), 0 },
        { PlayerClass.Archer.ToString(), 1 }
    };

    // Singleton Implementation
    public static SpawnPlayerManager instance = null;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            object playerSelectionCode;
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(MultiplayerRoguelike.PLAYER_SELECTION_CODE, out playerSelectionCode))
            {
                string selectedCode = playerSelectionCode as string;

                Debug.Log(selectedCode);

                if (!string.IsNullOrEmpty(selectedCode) && playerIndexMapping.ContainsKey(selectedCode))
                {
                    int prefabIndex = playerIndexMapping[selectedCode]; // Get the prefab index

                    int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
                    Vector3 instantiatePosition = InstantiatePositions[actorNumber - 1].position;

                    // string prefabPath = "Player/" + playerPrefab.name;

                    GameObject playerObj = PhotonNetwork.Instantiate("Player/" + PlayerPrefabs[prefabIndex].name, instantiatePosition, Quaternion.identity);

                    // Initialize Player Stat based on class selected
                    GameManager.Instance.activePlayers.Add(playerObj);
                    GameManager.Instance.InitializePlayerClass(PhotonNetwork.LocalPlayer.ActorNumber);
                }
                else
                {
                    Debug.LogError("Invalid player selection code: " + selectedCode);
                }
            }
        }
    }
}
