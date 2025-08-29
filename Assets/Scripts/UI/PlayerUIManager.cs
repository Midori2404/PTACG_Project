using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviourPunCallbacks
{
    [Header("GameOver Panel")]
    public GameObject gameOverPanel;

    [Header("Victory Panel")]
    public GameObject victoryGamePanel;


    // Start is called before the first frame update
    void Start()
    {
        gameOverPanel.SetActive(false);
        victoryGamePanel.SetActive(false);
    }

    public void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
    }

    public void ShowVictoryScreen()
    {
        victoryGamePanel.SetActive(true);
    }

    public void BackToMainMenu()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        // Load the Main Menu scene.
        SceneManager.LoadScene(0);
    }
}
