using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class EnemySpawner : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Normal Enemies Spawner Configuration")]
    [SerializeField] private List<EnemyTypeControl> enemyTypeControls; // List of enemy types and their spawn amounts
    [SerializeField] private Transform[] spawnPoints; // Array of spawn points
    [SerializeField] private float spawnInterval = 5f; // Time between spawns

    [Header("Monsters On Defeat Properties")]
    private int totalEnemiesToSpawn; // Total enemies to spawn from all types
    private int currentSpawnCount = 0; // Counter for spawned enemies
    private int remainingEnemies; // Remaining enemies in the scene
    [SerializeField] private int monsterKillCounts;
    public int MonsterKillCount => monsterKillCounts; // Public getter for kill count
    public TextMeshProUGUI monsterKillCountsText;

    public event EventHandler OnEnemyCountChanged; // No usage yet
    public event EventHandler OnAllEnemiesDefeated;

    [Header("Enemy Scaling Configuration")]
    [Range(1, 6)]
    public int enemyIncrementPerRoom; // Number of additional enemies per type after clearing a room

    [Header("Boss Spawn Configuration")]
    public GameObject bossPrefab;

    private PhotonView photonView;

    public static EnemySpawner Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        photonView = GetComponent<PhotonView>();
    }

    public IEnumerator SpawnEnemies()
    {
        ScaleEnemyAmounts();

        while (currentSpawnCount < totalEnemiesToSpawn)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                SpawnRandomEnemy();
            }
            yield return new WaitForSeconds(spawnInterval);
        }

        Debug.Log("All enemies have been spawned. Stopping spawner.");
    }

    [PunRPC]
    void SpawnRandomEnemy()
    {
        if (enemyTypeControls.Count == 0 || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No enemy types or spawn points assigned!");
            return;
        }

        EnemyTypeControl selectedType = null;
        int attempts = 0; // Fail-safe counter
        while (selectedType == null && attempts < enemyTypeControls.Count)
        {
            int randomIndex = UnityEngine.Random.Range(0, enemyTypeControls.Count);
            if (enemyTypeControls[randomIndex].enemyAmount > 0)
            {
                selectedType = enemyTypeControls[randomIndex];
            }
            attempts++;
        }

        if (selectedType == null)
        {
            Debug.LogWarning("No valid enemy types to spawn!");
            return;
        }

        Transform randomSpawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        Vector3 enemySpawnPointRadius = randomSpawnPoint.GetComponent<EnemySpawnPoint>().GetRandomSpawnPosition();

        if (selectedType.enemyAttribute.enemyPrefab != null)
        {
            string enemyPrefabPath = "Enemy/" + selectedType.enemyAttribute.enemyPrefab.name;
            GameObject enemy = PhotonNetwork.Instantiate(enemyPrefabPath, enemySpawnPointRadius, Quaternion.identity);
            enemy.GetComponent<EnemyBehaviour>().OnEnemyDefeated += HandleEnemyDefeated;
            ApplyAttributes(enemy, selectedType.enemyAttribute);

            selectedType.enemyAmount--;
            currentSpawnCount++;
        }
        else
        {
            Debug.LogWarning($"Enemy prefab missing for {selectedType.enemyAttribute.name}");
        }
    }

    void ApplyAttributes(GameObject enemy, EnemyAttribute attributes)
    {
        if (enemy.TryGetComponent<EnemyBehaviour>(out var enemyScript))
        {
            enemy.GetComponent<PhotonView>().RPC("RPC_InitializeAttributes", RpcTarget.All,
            attributes.maxHealth,
            attributes.movementSpeed,
            attributes.attackDamage,
            attributes.attackRate,
            attributes.attackRange,
            (int)attributes.enemyType);
        }
        else
        {
            Debug.LogWarning("Spawned enemy does not have an Enemy script!");
        }
    }

    public void SpawnBoss()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Instantiate(bossPrefab.name, BattleStateManager.Instance.GetRoom().transform.position, Quaternion.identity);
        }
    }

    void HandleEnemyDefeated(object sender, EventArgs e)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            remainingEnemies--;
            monsterKillCounts++;
            UpdateMonsterKillCountsText();

            OnEnemyCountChanged?.Invoke(this, EventArgs.Empty); // No usage yet

            if (remainingEnemies <= 0)
            {
                OnAllEnemiesDefeated?.Invoke(this, EventArgs.Empty);
                Debug.Log("All enemies defeated!");
            }
        }
    }

    void UpdateMonsterKillCountsText()
    {
        if (monsterKillCountsText != null)
        {
            monsterKillCountsText.text = $"Monsters Killed: {monsterKillCounts}";
        }
    }

    void ScaleEnemyAmounts()
    {
        ResetCount();

        foreach (var typeControl in enemyTypeControls)
        {
            typeControl.baseEnemyAmount += enemyIncrementPerRoom; // Increment the base amount
            typeControl.enemyAmount = typeControl.baseEnemyAmount; // Update current amount
            totalEnemiesToSpawn += typeControl.enemyAmount;
        }

        remainingEnemies = totalEnemiesToSpawn;

        Debug.Log("Enemy amounts scaled for the next room.");
    }

    public int GetTotalEnemyCount()
    {
        return totalEnemiesToSpawn;
    }

    public int GetRemainingEnemyCount()
    {
        return remainingEnemies;
    }

    public void ResetCount()
    {
        currentSpawnCount = 0;
        totalEnemiesToSpawn = 0;
        remainingEnemies = 0;
    }

    public Transform[] GetCurrentEnemySpawnPoint()
    {
        return spawnPoints;
    }

    public void SetEnemySpawnPoint(Transform[] spawnPoints)
    {
        this.spawnPoints = spawnPoints;
    }

    // Implement IPunObservable to synchronize monsterKillCounts
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // The master client sends the current kill count.
            stream.SendNext(monsterKillCounts);
        }
        else
        {
            // Other clients receive and update the kill count.
            monsterKillCounts = (int)stream.ReceiveNext();

            // Update the UI text immediately when the new value is received.
            if (monsterKillCountsText != null)
            {
                monsterKillCountsText.text = $"Monsters Killed: {monsterKillCounts}";
            }
        }
    }

}

[System.Serializable]
public class EnemyTypeControl
{
    public EnemyAttribute enemyAttribute;
    public int baseEnemyAmount; // Initial amount
    public int enemyAmount;     // Current amount
}