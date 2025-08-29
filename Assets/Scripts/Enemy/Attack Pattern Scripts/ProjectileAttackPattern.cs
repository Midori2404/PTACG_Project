using UnityEngine;

public enum ProjectileAttackType
{
    Circular,
    Spiral,
    Wave,
    Random
}

[CreateAssetMenu(fileName = "ProjectileAttack", menuName = "AttackPatterns/Projectile")]
public class ProjectileAttackPattern : ScriptableObject
{
    public ProjectileAttackType attackType; // Dropdown for attack type

    // Shared variables
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;

    // Variables for Circular pattern
    public int circularProjectilesPerWave = 10;

    // Variables for Spiral pattern
    public float spiralRotationSpeed = 50f;
    public int spiralProjectilesPerWave = 10;

    // Variables for Wave pattern
    public float waveSpreadAngle = 45f;
    public int waveProjectilesPerWave = 10;

    // Variables for Random pattern
    public int randomProjectilesPerWave = 10;

    public void Execute(GameObject boss)
    {
        switch (attackType)
        {
            case ProjectileAttackType.Circular:
                ExecuteCircularPattern(boss);
                break;
            case ProjectileAttackType.Spiral:
                ExecuteSpiralPattern(boss);
                break;
            case ProjectileAttackType.Wave:
                ExecuteWavePattern(boss);
                break;
            case ProjectileAttackType.Random:
                ExecuteRandomPattern(boss);
                break;
        }
    }

    private void ExecuteCircularPattern(GameObject boss)
    {
        for (int i = 0; i < circularProjectilesPerWave; i++)
        {
            float angle = i * (360f / circularProjectilesPerWave);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            LaunchProjectile(boss.transform.position, direction);
        }
    }

    private void ExecuteSpiralPattern(GameObject boss)
    {
        float baseAngle = Time.time * spiralRotationSpeed;
        for (int i = 0; i < spiralProjectilesPerWave; i++)
        {
            float angle = baseAngle + i * (360f / spiralProjectilesPerWave);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            LaunchProjectile(boss.transform.position, direction);
        }
    }

    private void ExecuteWavePattern(GameObject boss)
    {
        for (int i = 0; i < waveProjectilesPerWave; i++)
        {
            float offset = Mathf.Lerp(-waveSpreadAngle, waveSpreadAngle, (float)i / (waveProjectilesPerWave - 1));
            Vector3 direction = Quaternion.Euler(0, offset, 0) * Vector3.forward;

            LaunchProjectile(boss.transform.position, direction);
        }
    }

    private void ExecuteRandomPattern(GameObject boss)
    {
        for (int i = 0; i < randomProjectilesPerWave; i++)
        {
            Vector3 randomDirection = new Vector3(
                Random.Range(-1f, 1f),
                0,
                Random.Range(-1f, 1f)
            ).normalized;

            LaunchProjectile(boss.transform.position, randomDirection);
        }
    }

    private void LaunchProjectile(Vector3 origin, Vector3 direction)
    {
        GameObject projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        rb.velocity = direction * projectileSpeed;
    }
}
