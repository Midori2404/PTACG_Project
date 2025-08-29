using System.Collections;
using UnityEngine;

public class BossAttack : MonoBehaviour
{
    public GameObject projectilePrefab;
    public float shootInterval = 2f;
    public float projectileSpeed = 10f;
    public int projectilesPerWave = 10;

    private int currentAttackPattern = 0;
    float delayBetweenAttacks = 1f;

    void Start()
    {
        // Switch attack patterns periodically
        //InvokeRepeating(nameof(ChooseAttackPattern), 1f, shootInterval);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartCoroutine(PerformMultipleAttacks(0, 5)); // Perform pattern 1 five times
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            StartCoroutine(PerformMultipleAttacks(1, 5)); // Perform pattern 1 five times

        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            StartCoroutine(PerformMultipleAttacks(2, 5)); // Perform pattern 1 five times

        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            StartCoroutine(PerformMultipleAttacks(3, 5)); // Perform pattern 1 five times

        }

    }

    private IEnumerator PerformMultipleAttacks(int pattern, int repetitions)
    {
        for (int i = 0; i < repetitions; i++)
        {
            ChooseAttackPattern(pattern); // Trigger the chosen attack pattern
            yield return new WaitForSeconds(delayBetweenAttacks); // Wait for delay
        }
    }

    void ChooseAttackPattern(int pat)
    {
        //currentAttackPattern = Random.Range(0, 4); // Randomly pick an attack pattern
        switch (pat)
        {
            case 0: ShootCircularPattern(); break;
            case 1: ShootSpiralPattern(); break;
            case 2: ShootRandomPattern(); break;
            case 3: ShootWavePattern(); break;
        }
    }

    void ShootCircularPattern()
    {
        for (int i = 0; i < projectilesPerWave; i++)
        {
            float angle = i * (360f / projectilesPerWave);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            LaunchProjectile(direction);
        }
    }

    void ShootSpiralPattern()
    {
        float baseAngle = Time.time * 50f; // Rotate over time for a spiral effect
        for (int i = 0; i < projectilesPerWave; i++)
        {
            float angle = baseAngle + i * (360f / projectilesPerWave);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            LaunchProjectile(direction);
        }
    }

    void ShootRandomPattern()
    {
        for (int i = 0; i < projectilesPerWave; i++)
        {
            Vector3 randomDirection = new Vector3(
                Random.Range(-1f, 1f),
                0,
                Random.Range(-1f, 1f)
            ).normalized;

            LaunchProjectile(randomDirection);
        }
    }

    void ShootWavePattern()
    {
        float spreadAngle = 45f; // Spread angle for wave pattern
        for (int i = 0; i < projectilesPerWave; i++)
        {
            float offset = Mathf.Lerp(-spreadAngle, spreadAngle, (float)i / (projectilesPerWave - 1));
            Vector3 direction = Quaternion.Euler(0, offset, 0) * Vector3.forward;

            LaunchProjectile(direction);
        }
    }

    void LaunchProjectile(Vector3 direction)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        rb.velocity = direction * projectileSpeed;
    }
}
