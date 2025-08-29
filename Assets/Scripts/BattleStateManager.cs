using ExitGames.Client.Photon;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleState
{
    Roaming,
    InBattle
}


public class BattleStateManager : MonoBehaviour
{
    public static BattleStateManager Instance { get; private set; }
    [Header("Player Current State")]
    public BattleState currentState;

    [Header("Boss Room Music")]
    public AudioClip bossMusic;

    private RoomBehaviour roomBehaviour;
    private EnemySpawner enemySpawner;

    private void Awake()
    {
        Instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        enemySpawner = GetComponent<EnemySpawner>();

        enemySpawner.OnAllEnemiesDefeated += EnemySpawner_OnAllEnemiesDefeated;
    }

    private void EnemySpawner_OnAllEnemiesDefeated(object sender, System.EventArgs e)
    {
        roomBehaviour.isCleared = true;
        roomBehaviour.UnlockDoors();

        // Only the master client should initiate the revival RPC.
        if (PhotonNetwork.IsMasterClient)
        {
            ReviveFallenPlayers();
        }
    }

    /// <summary>
    /// All the spawn points are in fixed position. To ensure the enemies are spawned correctly for each room,
    /// set the parent position of all spawn points to the room origin (center of the room).
    /// </summary>
    public void ConfigureStateAndSpawner(RoomBehaviour roomBehaviour)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        // Set Spawner position to be in that room
        this.roomBehaviour = roomBehaviour;
        transform.position = roomBehaviour.transform.position;

        if (roomBehaviour.isSpawnRoom || (roomBehaviour.spawned && roomBehaviour.isCleared))
        {
            roomBehaviour.UnlockDoors();
            return;
        }
        else if (!roomBehaviour.isBossRoom && !roomBehaviour.isCleared && !roomBehaviour.spawned)
        {
            // Start Spawning Enemies
            StartCoroutine(enemySpawner.SpawnEnemies());
            roomBehaviour.spawned = true;
        }
        else if (roomBehaviour.isBossRoom)
        {
            AudioManager.instance.PlayMusicForScene(bossMusic.name);
            
            // Start Spawning Boss
            enemySpawner.SpawnBoss();
        }


        //Lock Room
        roomBehaviour.LockDoors();
    }

    /// <summary>
    /// Notifies all players (via RPC) that the room is cleared.
    /// </summary>
    private void ReviveFallenPlayers()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            FallenSpectate fs = player.GetComponent<FallenSpectate>();
            if (fs != null)
            {
                // This RPC will be received by all clients for this player's FallenSpectate component.
                fs.photonView.RPC("RPC_OnRoomCleared", RpcTarget.All);
            }
        }
    }

    public RoomBehaviour GetRoom()
    {
        return roomBehaviour;
    }
}
