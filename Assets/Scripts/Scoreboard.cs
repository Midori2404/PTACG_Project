using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Scoreboard : MonoBehaviourPunCallbacks
{
    public GameObject scoreboard;
    public GameObject playerScoreboardEntryPrefab;
    public Transform scoreboardContent;

    private Dictionary<string, PlayerScoreboardEntry> playerEntries = new Dictionary<string, PlayerScoreboardEntry>();

    // Start is called before the first frame update
    void Start()
    {
        // Initialize the scoreboard with existing players if needed
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleScoreboard();
        }
        else if (Input.GetKeyUp(KeyCode.Tab))
        {
            ToggleScoreboard();
        }
    }

    public void ToggleScoreboard()
    {
        scoreboard.SetActive(!scoreboard.activeSelf);
    }

    [PunRPC]
    public void UpdatePlayerScore(string playerName, int killCount)
    {
        if (playerEntries.ContainsKey(playerName))
        {
            playerEntries[playerName].UpdateKillCount(killCount);
        }
        else
        {
            GameObject entryObject = Instantiate(playerScoreboardEntryPrefab, scoreboardContent);
            PlayerScoreboardEntry entry = entryObject.GetComponent<PlayerScoreboardEntry>();
            entry.SetPlayerName(playerName);
            entry.UpdateKillCount(killCount);
            playerEntries.Add(playerName, entry);
        }
    }

    public void OnPlayerKill(string playerName)
    {
        int newKillCount = playerEntries.ContainsKey(playerName) ? playerEntries[playerName].GetKillCount() + 1 : 1;
        photonView.RPC("UpdatePlayerScore", RpcTarget.All, playerName, newKillCount);
    }
}

public class PlayerScoreboardEntry : MonoBehaviour
{
    public TMP_Text playerNameText;
    public TMP_Text killCountText;

    public void SetPlayerName(string playerName)
    {
        playerNameText.text = playerName;
    }

    public void UpdateKillCount(int killCount)
    {
        killCountText.text = killCount.ToString();
    }

    public int GetKillCount()
    {
        return int.Parse(killCountText.text);
    }
}
