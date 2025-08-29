using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;

[RequireComponent(typeof(PhotonView))]
public class PlayerSetup : MonoBehaviourPunCallbacks
{
    public Camera PlayerCamera;
    public TextMeshProUGUI PlayerNameText;

    // Start is called before the first frame update
    void Start()
    {
        if (photonView.IsMine)
        {
            //enable Movement script and camera
            GetComponent<ArcherCharacterController>().enabled = true;
            GetComponent<PlayerAnimationController>().enabled = true;

            if(GetComponentInChildren<Archer>() != null) GetComponentInChildren<Archer>().enabled = true;
            //if(GetComponentInChildren<Swordsman>() != null) GetComponentInChildren<Swordsman>().enabled = true;

            PlayerCamera.enabled = true;
        }
        else
        {
            //Player is remote. Disable Movement script and camera.
            GetComponent<ArcherCharacterController>().enabled = false;
            GetComponent<PlayerAnimationController>().enabled = false;
            GetComponentInChildren<Archer>().enabled = false;
            PlayerCamera.enabled = false;
        }

        SetPlayerUI();
    }

    private void SetPlayerUI()
    {
        if (PlayerNameText != null)
        {
            PlayerNameText.text = photonView.Owner.NickName;
        }
    }

}
