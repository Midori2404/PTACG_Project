using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class TrapSpawner : MonoBehaviour
{
    [Header("Trap Spawner Configuration")]
    [SerializeField] private List<TrapTypeControl> trapTypeControls; // List of trap types and their spawn amounts
    [SerializeField] private Transform[] spawnPoints; // Array of spawn points
    [SerializeField] private float spawnInterval = 5f; // Time between spawns

    private void Start()
    {
        StartCoroutine(SpawnTraps());
    }

    private IEnumerator SpawnTraps()
    {
        if (trapTypeControls.Count == 0 || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No trap types or spawn points assigned!");
            yield break;
        }

        List<Transform> shuffledSpawnPoints = new List<Transform>(spawnPoints);
        ShuffleList(shuffledSpawnPoints);
        List<TrapTypeControl> availableTraps = new List<TrapTypeControl>(trapTypeControls);

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (availableTraps.Count == 0) break;

            TrapTypeControl selectedTrap = GetRandomAvailableTrap(availableTraps);
            if (selectedTrap == null) continue;

            Transform spawnPoint = shuffledSpawnPoints[i];
            SpawnTrapAt(selectedTrap, spawnPoint);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnTrapAt(TrapTypeControl trapType, Transform spawnPoint)
    {
        if (trapType.trapAttribute.enemyPrefab != null)
        {
            string trapPrefabPath = "Traps/" + trapType.trapAttribute.enemyPrefab.name;
            //GameObject enemy = Instantiate(trapType.trapAttribute.enemyPrefab, spawnPoint.position, Quaternion.identity); // without photon
            GameObject enemy = PhotonNetwork.Instantiate(trapPrefabPath, spawnPoint.position, Quaternion.identity);
            
            enemy.GetComponent<EnemyBehaviour>();
            ApplyAttributes(enemy, trapType.trapAttribute);
            
            trapType.trapAmount--;
            if (trapType.trapAmount <= 0)
            {
                trapTypeControls.Remove(trapType);
            }
        }
        else
        {
            Debug.LogWarning($"Trap prefab missing for {trapType.trapAttribute.name}");
        }
    }

    private TrapTypeControl GetRandomAvailableTrap(List<TrapTypeControl> availableTraps)
    {
        availableTraps.RemoveAll(t => t.trapAmount <= 0);
        if (availableTraps.Count == 0) return null;
        return availableTraps[Random.Range(0, availableTraps.Count)];
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
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
}

[System.Serializable]
public class TrapTypeControl
{
    public EnemyAttribute trapAttribute;
    public int trapAmount;
}