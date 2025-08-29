using System.Collections.Generic;
using UnityEngine;

public class BossAttackManager : MonoBehaviour
{
    public List<ProjectileAttackPattern> projectileAttackPatterns; // Assignable in Inspector
    public float attackInterval = 3f; // Time between attacks

    private void Start()
    {
        InvokeRepeating(nameof(PerformRandomProjectileAttack), attackInterval, attackInterval);
    }

    private void PerformRandomProjectileAttack()
    {
        if (projectileAttackPatterns.Count == 0) return;

        // Choose a random pattern and execute it
        int randomIndex = Random.Range(0, projectileAttackPatterns.Count);
        ProjectileAttackPattern selectedPattern = projectileAttackPatterns[randomIndex];

        selectedPattern.Execute(gameObject);
    }
}
