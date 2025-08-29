using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireAttack : MonoBehaviour
{
    public Vector3 damageAreaScale;
    public float scaleSpeed = 1f;

    private HashSet<GameObject> affectedPlayers = new HashSet<GameObject>(); // Players in the damage area
    private Vector3 initialScale = new (0, 0, 0);

    private void Start()
    {
        damageAreaScale = transform.localScale;
        transform.localScale = initialScale;
        Initialize();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) 
        {
            transform.localScale = initialScale;
            Initialize();
        }
    }

    public void Initialize()
    {
        //StartCoroutine(ActivateDamageRegistration());
    }

    private void ApplyDamageOverTime(float damage)
    {
        foreach (var player in affectedPlayers)
        {
            if (player == null) continue; // Skip if the player is null (e.g., disconnected)

            if (player.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(damage); // Apply damage
            }
        }
    }

    public IEnumerator ActivateDamageRegistration(float damage, float attackRate, float duration)
    {
        GetComponent<Collider>().enabled = true;

        // Damage over time
        float startTime = Time.time; // Record the start time
        while (Time.time - startTime < duration)
        {
            if (affectedPlayers.Count > 0)
            {
                ApplyDamageOverTime(damage); // Apply damage to all players in the area
            }
            yield return new WaitForSeconds(attackRate);
        }

        // Reset Collider
        GetComponent<Collider>().enabled = false;
        transform.localScale = initialScale;
    }

    public IEnumerator ScaleDamageArea()
    {
        float progress = 0f;
        while (progress < 1f)
        {
            progress += Time.deltaTime * scaleSpeed; // Scale over time
            transform.localScale = Vector3.Lerp(initialScale, damageAreaScale, progress);
            yield return null;
        }
        transform.localScale = damageAreaScale; // Ensure final scale is correct
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsTargetValid(other))
        {
            affectedPlayers.Add(other.gameObject); // Add player to the list
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsTargetValid(other))
        {
            affectedPlayers.Remove(other.gameObject); // Remove player from the list
        }
    }

    private bool IsTargetValid(Collider other)
    {
        return other.CompareTag("Player");
    }
}
