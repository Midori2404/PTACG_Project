using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class HealthPickupRNG : MonoBehaviour
{
    [Header("Health Pickup Prefab")]
    [SerializeField] private GameObject healthPickupPrefab; // Assign health pickup prefab in Inspector

    [Header("Health Pickup RNG Settings in %")]
    [SerializeField] [Range(0f, 1f)] private float dropChance = 0.3f; // 30% chance by default

    private EnemyBehaviour enemyBehaviour;

    void Start()
    {
        enemyBehaviour = GetComponent<EnemyBehaviour>();
        if (enemyBehaviour != null)
        {
            enemyBehaviour.OnEnemyDefeated += HandleEnemyDefeated;
        }
    }

    private void HandleEnemyDefeated(object sender, System.EventArgs e)
    {
        if (PhotonNetwork.IsMasterClient) // Ensure only master client spawns the pickup
        {
            float rng = Random.value; // Generates a random float between 0 and 1
            if (rng <= dropChance)
            {
                InstantiateHealthPickup();
            }
        }
    }

    private void InstantiateHealthPickup()
    {
        if (healthPickupPrefab != null)
        {
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f; // Slightly above ground
            PhotonNetwork.Instantiate("Pickups/" + healthPickupPrefab.name, spawnPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("Health Pickup Prefab is not assigned!");
        }
    }
}