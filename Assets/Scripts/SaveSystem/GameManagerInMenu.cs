using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManagerInMenu : MonoBehaviour
{
    private int playerCoins = 0;
    public TextMeshProUGUI playerCoinsText;
    // Start is called before the first frame update
    void Start()
    {
        // Load coins when the game starts
        playerCoins = SaveSystem.LoadCoins();
        Debug.Log("Coins Loaded: " + playerCoins);
        UpdateText();
    }

    // Update is called once per frame
    void Update()
    {
        // Example: Press 'S' to save and 'L' to load for testing
        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveSystem.SaveCoins(playerCoins);
            Debug.Log("Saved");
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            playerCoins = SaveSystem.LoadCoins();
            Debug.Log("Coins After Loading: " + playerCoins);
            UpdateText();
        }

        // Example: Add coins with 'C' key
        if (Input.GetKeyDown(KeyCode.C))
        {
            playerCoins += 10;
            Debug.Log("Coins Added: " + playerCoins);
            UpdateText();
        }
    }

    void UpdateText()
    {
        playerCoinsText.text = playerCoins.ToString();
    }
}
