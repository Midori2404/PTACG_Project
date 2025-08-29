using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEditor.Rendering;

public class SwampEffect : MonoBehaviour
{
    [SerializeField] private float speedMultiplayer = 0.7f;
    // Dictionary to store each player's original movement speed
    private Dictionary<int, float> playerOriginalSpeeds = new Dictionary<int, float>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Ensure it's a player
        {
            PhotonView pv = other.GetComponent<PhotonView>();

            if (pv != null && pv.IsMine) // Only apply to the local player
            {
                PlayerController playerController = other.GetComponent<PlayerController>();

                if (playerController != null)
                {
                    int playerID = pv.ViewID; // Unique ID for each player

                    // Store original move speed if not already stored
                    if (!playerOriginalSpeeds.ContainsKey(playerID))
                    {
                        playerOriginalSpeeds[playerID] = playerController.moveSpeed;
                    }

                    playerController.moveSpeed = playerOriginalSpeeds[playerID] * speedMultiplayer;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) // Ensure it's a player
        {
            PhotonView pv = other.GetComponent<PhotonView>();

            if (pv != null && pv.IsMine) // Only restore for local player
            {
                PlayerController playerController = other.GetComponent<PlayerController>();

                if (playerController != null)
                {
                    int playerID = pv.ViewID; // Unique ID for each player

                    // Restore the player's original move speed if it was stored
                    if (playerOriginalSpeeds.ContainsKey(playerID))
                    {
                        playerController.moveSpeed = playerOriginalSpeeds[playerID];
                        playerOriginalSpeeds.Remove(playerID); // Remove from dictionary to free memory
                    }
                }
            }
        }
    }
}
