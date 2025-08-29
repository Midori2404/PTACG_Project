using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using Photon.Pun;

public class DoorBehaviour : MonoBehaviourPunCallbacks
{
    [Header("0 - Up,\n1 - Down,\n2 - Right,\n3 - Left")]
    public int doorIndex; // 0 - Up, 1 - Down, 2 - Right, 3 - Left

    [Header("Debug")]
    public RoomBehaviour currentRoom;
    public RoomBehaviour connectedRoom; // The room this door leads to

    [Header("Teleportation Settings")]
    [SerializeField] private float baseTeleportDelay = 1f; // Teleport delay when all players are on the portal
    [SerializeField] private float singlePlayerExtraDelay = 3f; // Extra delay when only one player is on the portal
    [SerializeField] private string playerTag = "Player"; // Tag to identify players
    [SerializeField] private TextMeshProUGUI timerText; // TextMeshProUGUI to display the timer

    [SerializeField] private List<GameObject> playersOnPortal = new List<GameObject>(); // List of players on the portal
    private bool isTeleporting = false; // Prevents multiple teleportation processes
    private float elapsedTime = 0f; // Tracks the elapsed time of the current countdown
    private float currentDelay = 0f; // Current teleport delay based on player count

    private void Start()
    {
        timerText = GameObject.FindGameObjectWithTag("Notification").GetComponent<TextMeshProUGUI>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && !playersOnPortal.Contains(other.gameObject))
        {
            playersOnPortal.Add(other.gameObject);
            CheckTeleportation();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) && playersOnPortal.Contains(other.gameObject))
        {
            playersOnPortal.Remove(other.gameObject);
            CheckTeleportation();
        }
    }

    private void CheckTeleportation()
    {
        if (playersOnPortal.Count > 0)
        {
            // Calculate the new delay based on the current number of players
            currentDelay = playersOnPortal.Count == 1
                ? baseTeleportDelay + singlePlayerExtraDelay
                : baseTeleportDelay;

            // If teleportation isn't already running, start it
            if (!isTeleporting)
            {
                StartCoroutine(TeleportAfterDelay());
            }
        }
        else
        {
            // If no players are on the portal, reset everything
            StopAllCoroutines();
            isTeleporting = false;
            elapsedTime = 0f;
            UpdateTimerDisplay(0); // Reset the timer display
        }
    }

    private IEnumerator TeleportAfterDelay()
    {
        isTeleporting = true;

        while (elapsedTime < currentDelay)
        {
            // If no players are on the portal, cancel teleportation
            if (playersOnPortal.Count == 0)
            {
                isTeleporting = false;
                elapsedTime = 0f; // Reset elapsed time
                UpdateTimerDisplay(0); // Reset the timer display
                yield break;
            }

            elapsedTime += Time.deltaTime;

            // Recalculate the delay dynamically based on player count
            currentDelay = playersOnPortal.Count == 1
                ? baseTeleportDelay + singlePlayerExtraDelay
                : baseTeleportDelay;

            // Update the timer display
            UpdateTimerDisplay(currentDelay - elapsedTime);

            yield return null;
        }

        // Teleport all players on the portal
        photonView.RPC("TeleportPlayers", RpcTarget.All);
    }

    [PunRPC]
    private void TeleportPlayers()
    {
        foreach (GameObject go in GameManager.Instance.activePlayers)
        {
            GameObject player = go.GetComponentInChildren<PlayerController>().gameObject; // TEMPORARY
            if (player != null)
            {
                Vector3 teleportPosition = connectedRoom.teleportDestinations[GetOppositeDoor(doorIndex)].position;
                CharacterController characterController = player.GetComponentInChildren<CharacterController>();

                if (characterController != null)
                {
                    characterController.enabled = false;
                    player.transform.position = teleportPosition;
                    characterController.enabled = true;
                }
                else
                {
                    player.transform.position = teleportPosition;
                }
            }
        }

        // Reset state after teleportation
        isTeleporting = false;
        elapsedTime = 0f;
        UpdateTimerDisplay(0); // Reset the timer display
    }

    private void UpdateTimerDisplay(float timeRemaining)
    {
        if (timerText == null) return;

        if (playersOnPortal.Count == 0 || timeRemaining <= 0f)
        {
            timerText.text = ""; // Hide when no player is present or time ran out
        }
        else
        {
            timerText.text = $"Entering Room in: {timeRemaining:F1}s";
        }
    }


    private int GetOppositeDoor(int index)
    {
        // Map the door to its opposite: 0->1, 1->0, 2->3, 3->2
        return (index % 2 == 0) ? index + 1 : index - 1;
    }

    private void OnDisable()
    {
        if (timerText == null)
        {
            // Try to find it again if not yet assigned
            GameObject notification = GameObject.FindGameObjectWithTag("Notification");
            if (notification != null)
            {
                timerText = notification.GetComponent<TextMeshProUGUI>();
            }
        }

        if (timerText != null)
        {
            timerText.text = "";
        }
    }

}
