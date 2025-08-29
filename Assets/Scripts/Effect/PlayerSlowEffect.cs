using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.DebugUI.Table;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class PlayerSlowEffect : MonoBehaviour
{
    private Dictionary<int, float> playerOriginalSpeeds = new Dictionary<int, float>();

    private void Start()
    {
        // Subscribe to the scene change event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // Unsubscribe when object is destroyed to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Everfrost_h")
        {
            ApplySlownessToPlayer();
        }
    }

    private void ApplySlownessToPlayer()
    {
        GameObject player = PhotonNetwork.LocalPlayer.TagObject as GameObject;

        if (player != null)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            PlayerController playerController = player.GetComponent<PlayerController>();

            if (pv != null && pv.IsMine && playerController != null)
            {
                int playerID = pv.ViewID;

                // Store the original move speed if not already stored
                if (!playerOriginalSpeeds.ContainsKey(playerID))
                {
                    playerOriginalSpeeds[playerID] = playerController.moveSpeed;
                }

                // Apply slowness effect
                playerController.moveSpeed = playerOriginalSpeeds[playerID] * 0.85f;
            }
        }
    }


    /*private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Everfrost_h")
        {
            ApplySlownessToPlayer();
        }
        else
        {
            RestorePlayerSpeed();
        }
    } 


    private void RestorePlayerSpeed()
    {
        GameObject player = PhotonNetwork.LocalPlayer.TagObject as GameObject;

        if (player != null)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            PlayerController playerController = player.GetComponent<PlayerController>();

            if (pv != null && pv.IsMine && playerController != null)
            {
                int playerID = pv.ViewID;

                if (playerOriginalSpeeds.ContainsKey(playerID))
                {
                    playerController.moveSpeed = playerOriginalSpeeds[playerID];
                    playerOriginalSpeeds.Remove(playerID);
                }
            }
        }
    }*/
}