using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using UnityEngine.ProBuilder.Shapes;
using UnityEngine.TextCore.Text;
using System.Collections;

public class FallenSpectate : MonoBehaviourPunCallbacks, IPunObservable
{
    // Whether this player is currently spectating (i.e., has fallen).
    private bool isSpectating = false;
    // Current target the camera is following.
    private Transform spectateTarget;
    // Cached components.
    private PlayerController playerController;
    private PlayerAttribute playerAttribute;
    private SkillManager skillManager;
    private Archer archer;
    private CharacterCombo characterCombo;

    // Offset used if needed (not used when relying on Cinemachine follow).
    public Vector3 cameraOffset = new Vector3(0, 5, -10);

    // List of valid spectate targets.
    private List<Transform> spectateTargets = new List<Transform>();
    private int currentTargetIndex = 0;

    // Reference to the Game Over Panel (local canvas).
    public GameObject gameOverPanel;

    // Reference to the local CameraManager.
    private CameraManager cameraManager;

    public bool IsFallen
    {
        get { return isSpectating; }
    }


    void Start()
    {
        // Only the local player should run this.
        if (!photonView.IsMine)
            return;

        playerController = GetComponent<PlayerController>();
        playerAttribute = GetComponent<PlayerAttribute>();
        skillManager = GetComponent<SkillManager>();

        // Get the CameraManager attached to the local player (or in its children).
        cameraManager = GetComponentInChildren<CameraManager>();

        // Ensure the game over panel is hidden at start.
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (!photonView.IsMine)
            return;

        // Allow switching spectate target with arrow keys if spectating.
        if (isSpectating)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                SwitchTarget(1);
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                SwitchTarget(-1);
            }
        }
    }

    /// <summary>
    /// Call this method when the player has fallen (or died) to enter spectate mode.
    /// </summary>
    public void EnterSpectateMode()
    {
        if (isSpectating)
            return;

        isSpectating = true;

        // Disable player controls.
        if (playerController != null && skillManager != null)
        {
            playerController.enabled = false;
            skillManager.enabled = false;

            if (TryGetComponent(out Archer archer))
            {
                archer.enabled = false;
            }
            else if (TryGetComponent(out CharacterCombo characterCombo))
            {
                characterCombo.enabled = false;
            }
        }
        // Optionally disable other components (e.g. colliders).

        // Find all available players to spectate.
        GetSpectateTargets();

        // If there are valid targets, set the initial spectate target.
        if (spectateTargets.Count > 0)
        {
            currentTargetIndex = 0;
            spectateTarget = spectateTargets[currentTargetIndex];
            Debug.Log("Spectating: " + spectateTarget.name);
            // Update the Cinemachine virtual camera to follow the spectate target.
            if (cameraManager != null && cameraManager.virtualCamera != null)
            {
                cameraManager.virtualCamera.Follow = spectateTarget;
            }
        }
        else
        {
            // No targets available; show game over.
            ShowGameOverPanel();
        }
    }

    /// <summary>
    /// Populates the spectateTargets list with all alive players (excluding self).
    /// </summary>
    private void GetSpectateTargets()
    {
        spectateTargets.Clear();
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player == this.gameObject)
                continue;

            PlayerAttribute pa = player.GetComponent<PlayerAttribute>();
            if (pa != null)
            {
                if (pa.GetPlayerStats().currentHealth > 0)
                {
                    spectateTargets.Add(player.transform);
                }
            }
        }
    }

    /// <summary>
    /// Switches the spectate target based on the input direction.
    /// </summary>
    /// <param name="direction">1 for next, -1 for previous.</param>
    private void SwitchTarget(int direction)
    {
        if (spectateTargets.Count == 0)
            return;

        currentTargetIndex = (currentTargetIndex + direction + spectateTargets.Count) % spectateTargets.Count;
        spectateTarget = spectateTargets[currentTargetIndex];
        Debug.Log("Switched spectate target to: " + spectateTarget.name);
        if (cameraManager != null && cameraManager.virtualCamera != null)
        {
            cameraManager.virtualCamera.Follow = spectateTarget;
        }
    }

    /// <summary>
    /// Displays the game over panel when no spectate targets are available.
    /// </summary>
    private void ShowGameOverPanel()
    {
        Debug.Log("No spectate targets available. Game Over!");
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Call this method from the UI button to quit the game scene and return to the main menu.
    /// </summary>
    public void QuitGame()
    {
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Called externally (via RPC) when the room is cleared.
    /// Each client checks if its fallen player should be revived.
    /// </summary>
    [PunRPC]
    public void RPC_OnRoomCleared()
    {
        // Only the local owner should handle the revive logic.
        if (!photonView.IsMine)
            return;
        OnRoomCleared();
    }


    /// <summary>
    /// Called when the room is cleared.
    /// If the player is fallen and at least one other player is alive, revives the player.
    /// </summary>
    public void OnRoomCleared()
    {
        if (!isSpectating)
            return;

        if (IsAnyOtherPlayerAlive())
        {
            Debug.Log("Room cleared and other players are alive. Reviving fallen player.");
            RevivePlayer();
        }
        else
        {
            Debug.Log("Room cleared but no other alive players remain.");
            ShowGameOverPanel();
        }
    }

    /// <summary>
    /// Checks if at least one other player is alive (by verifying that their PlayerController is enabled).
    /// </summary>
    /// <returns>True if at least one other player is alive; otherwise, false.</returns>
    private bool IsAnyOtherPlayerAlive()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player == this.gameObject)
                continue;

            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null && pc.enabled)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Revives the fallen player by restoring health, re-enabling controls and colliders,
    /// and resetting the camera to follow the player again.
    /// </summary>
    private void RevivePlayer()
    {
        // Restore player's health.
        var stats = playerAttribute.GetPlayerStats();
        stats.currentHealth = stats.baseHealth / 2f;

        playerController.enabled = true;
        playerController.PlayRiseAnimation();

        StartCoroutine(RiseUpDelay());

        // Reset the virtual camera to follow the player.
        if (cameraManager != null && cameraManager.virtualCamera != null)
        {
            cameraManager.virtualCamera.Follow = transform;
        }

        Debug.Log("Player revived and control restored.");
    }

    public IEnumerator RiseUpDelay()
    {
        yield return new WaitForSeconds(2.5f);
        // Mark the player as no longer dead.
        playerAttribute.isDead = false;

        // Re-enable player controls.
        if (playerController != null && skillManager != null)
        {
            skillManager.enabled = true;

            if (TryGetComponent(out Archer archer))
            {
                archer.enabled = true;
            }
            else if (TryGetComponent(out CharacterCombo characterCombo))
            {
                characterCombo.enabled = true;
            }
        }

        // Re-enable collider if it was disabled.
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }

        // Exit spectate mode.
        isSpectating = false;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Send the current state to other clients.
            stream.SendNext(isSpectating);
        }
        else
        {
            // Receive the state from the owner.
            isSpectating = (bool)stream.ReceiveNext();
        }
    }
}
