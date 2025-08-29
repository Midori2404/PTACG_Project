using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using System;
using Photon.Pun;

public class RoomBehaviour : MonoBehaviour, IPunObservable
{
    [Header("0 - Up,\n1 - Down,\n2 - Right,\n3 - Left")]
    public GameObject[] walls; // 0 - Up 1 -Down 2 - Right 3- Left

    [Header("Teleportation, Destination & Pathway Connector (DoorBehaviour)")]
    public GameObject[] doors;

    [Header("Room States")]
    public bool spawned;
    public bool isCleared;
    public bool isSpawnRoom;
    public bool isBossRoom;

    [Header("Room Minimap Icon")]
    public GameObject roomUI;

    [Header("Camera Confiner 3D")]
    [SerializeField] Collider cameraConfinerCollider;
    [SerializeField] GameObject[] playerCamera;

    [Header("Room Teleport Destinations")]
    public Transform[] teleportDestinations; // 0 - Up 1 - Down 2 - Right 3 - Left

    [Header("Enemy Spawn Points for This Room")]
    [SerializeField] private Transform[] enemySpawnPoints;

    [Header("List of DoorBehaviours in this room")]
    [SerializeField] private DoorBehaviour[] doorsBehaviour;

    [Header("References")]
    private PhotonView photonView;
    public static RoomBehaviour currentActiveRoom = null; // Add this to track the active room

    public EventHandler OnRoomEntered;


    public void Awake()
    {
        doorsBehaviour = GetComponentsInChildren<DoorBehaviour>();
        photonView = GetComponent<PhotonView>();

        OnRoomEntered += UpdateCameraConfiner;
        OnRoomEntered += UpdateEnemySpawner;
        OnRoomEntered += UpdateMinimap;
    }

    

    public void UpdateRoom(bool[] status)
    {
        for (int i = 0; i < status.Length; i++)
        {
            doors[i].SetActive(status[i]);

            if (walls == null)
                return;

            walls[i].SetActive(!status[i]);
        }
    }

    public void UpdateCameraConfiner(object sender, EventArgs e)
    {
        playerCamera = GameObject.FindGameObjectsWithTag("CameraConfiner");
        foreach (GameObject cameraConfiner in playerCamera) 
        {
            cameraConfiner.GetComponent<CinemachineConfiner>().m_BoundingVolume = cameraConfinerCollider;
        }
    }

    private void UpdateEnemySpawner(object sender, EventArgs e)
    {
        EnemySpawner.Instance.SetEnemySpawnPoint(enemySpawnPoints);
    }
    private void UpdateMinimap(object sender, EventArgs e)
    {
        // Update minimap and sync to all client
        DungeonGenerator.Instance.photonView.RPC("UpdateMinimapRPC", RpcTarget.All, transform.position.x, transform.position.z, roomUI.GetComponent<Image>().color.r, roomUI.GetComponent<Image>().color.g, roomUI.GetComponent<Image>().color.b);
    }

    public void LockDoors()
    {
        foreach (DoorBehaviour door in doorsBehaviour)
        {
            door.gameObject.SetActive(false);
        }
    }

    public void UnlockDoors()
    {
        foreach (DoorBehaviour door in doorsBehaviour)
        {
            door.gameObject.SetActive(true);
        }
    }

    public GameObject GetRoom()
    {
        return roomUI;
    }

    public void SetRoom(GameObject room)
    {
        roomUI = room;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Revert color of previous room's tile if it's not this one
            if (currentActiveRoom != null && currentActiveRoom != this)
            {
                currentActiveRoom.roomUI.GetComponent<Image>().color = Color.blue;
            }

            // Set current room's tile color to green
            roomUI.GetComponent<Image>().color = Color.green;
            currentActiveRoom = this;

            OnRoomEntered?.Invoke(this, new EventArgs());
            BattleStateManager.Instance.ConfigureStateAndSpawner(this);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Send data to other players
            stream.SendNext(doorsBehaviour.Length);
            foreach (var door in doorsBehaviour)
            {
                stream.SendNext(door.gameObject.activeSelf);
            }
        }
        else
        {
            // Receive data from other players
            int length = (int)stream.ReceiveNext();
            for (int i = 0; i < length; i++)
            {
                bool isActive = (bool)stream.ReceiveNext();
                doorsBehaviour[i].gameObject.SetActive(isActive);
            }
        }
    }
}