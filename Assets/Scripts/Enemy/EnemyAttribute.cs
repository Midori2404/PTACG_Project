using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyType
{
    Melee,
    Ranged
}

[CreateAssetMenu(fileName = "NewEnemyAttribute", menuName = "Enemy/Attribute")]
public class EnemyAttribute : ScriptableObject
{
    public EnemyType enemyType; // Dropdown for enemy type
    public GameObject enemyPrefab; // Prefab for the enemy
    public float maxHealth;
    public float movementSpeed;
    public float attackDamage;
    public float attackRate;
    public float attackRange;

    [Header("Ranged Specific")]
    public GameObject projectilePrefab; // Projectile prefab for ranged enemies
}
