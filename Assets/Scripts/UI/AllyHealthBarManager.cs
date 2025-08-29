using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class AllyHealthBarManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public GameObject allyHealthBarPrefab; // Assigned in Inspector
    public Transform allyHealthPanel;      // The panel where health bars are added

    // Dictionary to track added health bars using PhotonView.ViewID as the key.
    private Dictionary<int, AllyHealthBarUI> allyHealthBars = new Dictionary<int, AllyHealthBarUI>();

    void Awake()
    {
        Debug.Log("AllyHealthBarManager Awake called");
    }

    void Start()
    {
        // Start polling for remote players.
        StartCoroutine(PollForRemotePlayers());
    }

    /// <summary>
    /// Periodically checks for new player objects in the scene.
    /// </summary>
    IEnumerator PollForRemotePlayers()
    {
        while (true)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject player in players)
            {
                PhotonView pv = player.GetComponent<PhotonView>();
                // Skip if this is the local player or if we've already added this player's health bar.
                if (pv != null && !pv.IsMine && !allyHealthBars.ContainsKey(pv.ViewID))
                {
                    Debug.Log("Detected new remote player: " + pv.Owner.NickName);
                    AddAllyHealthBar(pv);
                }
            }
            // Wait before checking again.
            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// Instantiates and initializes a health bar for a remote player.
    /// </summary>
    /// <param name="pv">PhotonView of the remote player.</param>
    void AddAllyHealthBar(PhotonView pv)
    {
        if (allyHealthBarPrefab == null || allyHealthPanel == null)
        {
            Debug.LogError("AllyHealthBarPrefab or AllyHealthPanel is not assigned in the Inspector.");
            return;
        }

        GameObject newBarObj = Instantiate(allyHealthBarPrefab, allyHealthPanel);
        AllyHealthBarUI healthBarUI = newBarObj.GetComponent<AllyHealthBarUI>();

        // Assume the player's PlayerAttribute holds the health info.
        PlayerAttribute pa = pv.GetComponent<PlayerAttribute>();
        if (pa != null)
        {
            string allyName = pv.Owner.NickName;
            float maxHealth = pa.GetPlayerStats().baseHealth;
            healthBarUI.Initialize(allyName, maxHealth);
            allyHealthBars[pv.ViewID] = healthBarUI;
        }
        else
        {
            Debug.LogWarning("PlayerAttribute component missing on player with PhotonView: " + pv.ViewID);
        }
    }

    void Update()
    {
        // Optionally, update all ally health bars continuously.
        foreach (KeyValuePair<int, AllyHealthBarUI> kvp in allyHealthBars)
        {
            int viewID = kvp.Key;
            AllyHealthBarUI barUI = kvp.Value;
            PhotonView pv = PhotonView.Find(viewID);
            if (pv != null)
            {
                PlayerAttribute pa = pv.GetComponent<PlayerAttribute>();
                if (pa != null)
                {
                    float currentHealth = pa.GetPlayerStats().currentHealth;
                    float maxHealth = pa.GetPlayerStats().baseHealth;
                    barUI.UpdateHealth(currentHealth, maxHealth);
                }
            }
        }
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        List<int> keysToRemove = new List<int>();
        foreach (KeyValuePair<int, AllyHealthBarUI> kvp in allyHealthBars)
        {
            PhotonView pv = PhotonView.Find(kvp.Key);
            if (pv != null && pv.Owner.ActorNumber == otherPlayer.ActorNumber)
            {
                Destroy(kvp.Value.gameObject);
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (int key in keysToRemove)
        {
            allyHealthBars.Remove(key);
        }
    }
}
