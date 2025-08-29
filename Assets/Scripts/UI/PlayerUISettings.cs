using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerUISettings : MonoBehaviourPunCallbacks
{
    // Start is called before the first frame update

    public void BackToMainMenu()
    {
        PhotonNetwork.LeaveRoom();
    }
    public override void OnLeftLobby()
    {
        SceneManager.LoadScene(0);
    }
}
