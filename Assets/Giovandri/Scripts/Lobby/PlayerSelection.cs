using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerSelection : MonoBehaviour
{
    public GameObject[] SelectablePlayers;
    public string playerSelectionCode; // Now using a string instead of an int

    private Dictionary<string, int> playerIndexMapping = new Dictionary<string, int>()
    {
        { PlayerClass.Warrior.ToString(), 0 },
        { PlayerClass.Archer.ToString(), 1 }
    };

    // Start is called before the first frame update
    void Start()
    {
        playerSelectionCode = PlayerClass.Warrior.ToString(); // Default to warrior
        ActivatePlayer(playerSelectionCode);
    }

    private void ActivatePlayer(string selectionCode)
    {
        if (!playerIndexMapping.ContainsKey(selectionCode)) return;

        int index = playerIndexMapping[selectionCode];

        // Deactivate all characters
        foreach (GameObject selectablePlayer in SelectablePlayers)
        {
            selectablePlayer.SetActive(false);
        }

        // Activate selected character
        SelectablePlayers[index].SetActive(true);

        //setting up player selection custom property
        ExitGames.Client.Photon.Hashtable playerSelectionProp = new ExitGames.Client.Photon.Hashtable()
             { {MultiplayerRoguelike.PLAYER_SELECTION_CODE, playerSelectionCode } }; //CHANGE
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerSelectionProp);
        Debug.Log(playerSelectionProp);
    }

    public void OnWarrior01ButtonClicked()
    {
        playerSelectionCode = PlayerClass.Warrior.ToString();
        ActivatePlayer(playerSelectionCode);
    }

    public void OnArcher01ButtonClicked()
    {
        playerSelectionCode = PlayerClass.Archer.ToString();
        ActivatePlayer(playerSelectionCode);
    }

}
