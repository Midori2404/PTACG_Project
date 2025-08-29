using System.IO;
using UnityEngine;

public static class SaveSystem
{
    // Define a custom path for saving
    private static string customSavePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "PlayerSaveFile");

    public static void SaveCoins(int coins)
    {
        // Ensure the folder exists
        if (!Directory.Exists(customSavePath))
        {
            Directory.CreateDirectory(customSavePath);
        }

        // Define the file path
        string filePath = Path.Combine(customSavePath, "playerdata.json");

        // Create a data object and convert it to JSON
        PlayerData data = new PlayerData(coins);
        string json = JsonUtility.ToJson(data);

        // Write JSON to the file
        File.WriteAllText(filePath, json);

        Debug.Log("Game Saved at: " + filePath);
    }

    public static int LoadCoins()
    {
        // Define the file path
        string filePath = Path.Combine(customSavePath, "playerdata.json");

        // Check if the save file exists
        if (File.Exists(filePath))
        {
            // Read the file and deserialize the data
            string json = File.ReadAllText(filePath);
            PlayerData data = JsonUtility.FromJson<PlayerData>(json);
            Debug.Log("Game Loaded! Coins: " + data.coins);
            return data.coins;
        }
        else
        {
            Debug.LogWarning("Save file not found. Returning default coin value of 0.");
            return 0;
        }
    }
}

[System.Serializable]
public class PlayerData
{
    public int coins;

    public PlayerData(int coins)
    {
        this.coins = coins;
    }
}
