using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [SerializeField] private float healAmount = 20f; // Amount of health restored

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Ensure the colliding object is a player
        {
            PlayerAttribute playerAttribute = other.GetComponent<PlayerAttribute>();

            if (playerAttribute != null)
            {
                playerAttribute.Heal(healAmount);
                PhotonNetwork.Destroy(gameObject); // Destroy the pickup after use
            }
        }
    }
}