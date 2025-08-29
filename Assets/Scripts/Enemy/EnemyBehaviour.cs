using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : MonoBehaviour, IDamageable
{
    // Folder Path
    public const string ENEMY_PROJECTILES = "Projectiles";

    [Header("Attributes")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float health;
    [SerializeField] private float movementSpeed;
    [SerializeField] private float attackRate;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackDamage; // Damage dealt per attack
    [SerializeField] private float attackAngle = 60f; // Fixed cone angle
    [SerializeField] private EnemyType enemyType;
    [SerializeField] private GameObject projectile;
    [SerializeField] private Vector3 projectileFirePointOffset;

    [Header("Flinch Settings")]
    public bool canFlinch = true;
    public float flinchDuration = 0.3f;
    public float flinchCooldown = 1.5f; // Time before flinching can happen again
    private bool isFlinching = false;

    [Header("State")]
    private Transform target;
    private bool isDead;
    private bool isChasing;
    private bool attackExecuted = false;  // New flag
    private bool isAttacking;

    private Coroutine attackCoroutine;
    private float lastAttackTime = -Mathf.Infinity;
    private float lastProjectileTime = -Mathf.Infinity;


    public event EventHandler OnEnemyDefeated;

    [Header("World Space Health Bar UI")]
    [Tooltip("Reference to the world space health bar UI component on this enemy.")]
    [SerializeField] private WorldSpaceHealthBarUI worldSpaceHealthBar;

    [Header("Death Sound")]
    public AudioClip deathSound;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private PhotonView photonView;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        photonView = GetComponent<PhotonView>();

        // Find initial target
        FindTarget();
    }

    void Update()
    {
        if (isDead) return;

        // Ensure we have a valid target
        if (target == null || target.GetComponent<PlayerAttribute>().isDead)
        {
            FindTarget(); // Find a new valid target if current is dead or missing
        }

        if (target == null) return; // Stop if no valid target found

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget <= attackRange)
        {
            StopChase(); // Stop moving when within attack range
            Attack();
        }
        else
        {
            if (!isAttacking) // Only chase if not attacking
            {
                StopAttack();
                ChaseTarget();
            }
        }
    }

    [PunRPC]
    public void RPC_InitializeAttributes(float _maxHealth, float _movementSpeed, float _attackDamage, float _attackRate, float _attackRange, int _enemyType)
    {
        maxHealth = _maxHealth;
        health = _maxHealth;
        movementSpeed = _movementSpeed;
        attackDamage = _attackDamage;
        attackRate = _attackRate;
        attackRange = _attackRange;
        enemyType = (EnemyType)_enemyType;

        // Set agent speed, update UI, etc.
        agent.speed = movementSpeed;
        if (worldSpaceHealthBar != null)
        {
            worldSpaceHealthBar.SetHealth(health, maxHealth);
        }
    }


    [PunRPC]
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        health -= amount;

        worldSpaceHealthBar.SetHealth(health, maxHealth);

        if (health <= 0)
        {
            health = 0;
            isDead = true;
            SfxManager.instance.photonView.RPC("RPC_PlaySoundFXClip", RpcTarget.All, deathSound.name, transform.position, 0.7f);
            OnEnemyDefeated?.Invoke(this, EventArgs.Empty);
            Despawning();
        }
        else
        {
            // Optional: Play damage animation or effect
            StartCoroutine(Flinch());
        }
    }


    public void FindTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player"); // Get all players
        if (players.Length == 0)
        {
            // No player found
            animator.SetBool("isIdle", true);
            agent.isStopped = true;
            target = null;
            return;
        }

        Transform nearestPlayer = null;
        float shortestDistance = Mathf.Infinity;

        foreach (GameObject player in players)
        {
            // Check if player has an "isDead" property (modify based on your player script)
            PlayerAttribute playerAttribute = player.GetComponent<PlayerAttribute>(); // Change to your actual player script
            if (playerAttribute != null && playerAttribute.isDead) continue; // Skip dead players

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestPlayer = player.transform;
            }
        }

        target = nearestPlayer;
    }



    public void Attack()
    {
        if (!isAttacking)
        {
            isAttacking = true;
            attackCoroutine = StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            if (!RotateTowardsTarget(target.position))
            {
                yield return null; // Wait for alignment
                continue;
            }

            // Reset the flag at the beginning of the cycle.
            attackExecuted = false;

            // Perform Attack
            // Only the master sets the trigger.
            if (PhotonNetwork.IsMasterClient)
            {
                animator.SetBool("isIdle", false);
                SetAttackTrigger(); // This triggers the attack animation.
                // (The animation event should call PerformConeAttack.)
            }

            // if (enemyType == EnemyType.Melee)
            // {
            //     PerformConeAttack();
            // }
            // else if (enemyType == EnemyType.Ranged)
            // {
            //     ShootProjectile();
            // }

            yield return new WaitForSeconds(attackRate);

            // Check if target is still within range
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            if (distanceToTarget > attackRange)
            {
                StopAttack();
                ChaseTarget();
                yield break;
            }

            animator.SetBool("isIdle", true); // Stay idle until the next attack
        }
    }

    private void SetAttackTrigger()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_SetAttackTrigger", RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_SetAttackTrigger()
    {
        animator.SetTrigger("Attack");
    }

    private bool RotateTowardsTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Smoothly rotate toward the target
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Time.deltaTime * 500f);

        // Check if the rotation is close enough to consider aligned
        float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
        return angleDifference < 5f; // Consider aligned if within 5 degrees
    }

    // New public method called by the animation event
    public void PerformConeAttack()
    {
        // Only let the master trigger the attack
        if (!PhotonNetwork.IsMasterClient)
            return;

        // Check if we are allowed to attack again
        if (Time.time - lastAttackTime < attackRate)
            return;

        // Update the timestamp so further calls are ignored within the attackRate window.
        lastAttackTime = Time.time;

        // Trigger the RPC so that all clients process the attack.
        photonView.RPC("RPC_PerformConeAttack", RpcTarget.All);
    }


    [PunRPC]
    private void RPC_PerformConeAttack()
    {
        InternalPerformConeAttack();
    }

    // Extract the actual logic into a separate method
    private void InternalPerformConeAttack()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);
        foreach (var collider in hitColliders)
        {
            if (collider.TryGetComponent<IDamageable>(out var damageable) && collider.CompareTag("Player"))
            {
                Vector3 directionToTarget = (collider.transform.position - transform.position).normalized;
                float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

                if (angleToTarget <= attackAngle / 2)
                {
                    // Apply damage to the target
                    PhotonView playerPV = collider.GetComponent<PhotonView>();
                    playerPV.RPC("TakeDamage", RpcTarget.All, attackDamage);
                }
            }
        }
    }


    public void ShootProjectile()
    {
        // Only the master client should execute shooting logic.
        if (!PhotonNetwork.IsMasterClient)
            return;

        // Check if the cooldown has elapsed
        if (Time.time - lastProjectileTime < attackRate)
            return;

        // Update the timestamp so that subsequent calls within attackRate are ignored.
        lastProjectileTime = Time.time;

        if (projectile != null)
        {
            // Calculate the spawn position and direction
            Vector3 spawnPosition = transform.position + projectileFirePointOffset + transform.forward * 1.5f;
            Vector3 shootDirection = transform.forward;

            // Prepare instantiation data:
            // [0]: shootDirection, [1]: attackDamage, [2]: NegativeEffectType (as int),
            // [3]: shooter's PhotonView ID, [4-6]: additional parameters (set to 0 here)
            object[] instantiationData = new object[] { shootDirection, attackDamage, (int)NegativeEffectType.None, photonView.ViewID, 0f, 0f, 0f };

            // Construct the resource path from the constant and projectile prefab name.
            string resourcePath = ENEMY_PROJECTILES + "/" + projectile.name;

            // Instantiate the projectile across the network using PhotonNetwork.Instantiate
            PhotonNetwork.Instantiate(resourcePath, spawnPosition, Quaternion.LookRotation(shootDirection), 0, instantiationData);
        }
    }






    public void ChaseTarget()
    {
        if (!isChasing)
        {
            isChasing = true;
            agent.isStopped = false;
            animator.SetBool("isIdle", false);
            animator.SetBool("isMoving", true);
            StartCoroutine(ChaseRoutine());
        }
    }

    private IEnumerator ChaseRoutine()
    {
        while (isChasing && target != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            agent.SetDestination(target.position);
            yield return null;
        }
    }

    private void StopAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
            isAttacking = false;
        }
    }

    private void StopChase()
    {
        isChasing = false;
        agent.isStopped = true;
        animator.SetBool("isMoving", false);
        animator.SetBool("isIdle", true);
    }

    IEnumerator Flinch()
    {
        if (isFlinching) yield break; // Prevents overlapping flinches

        isFlinching = true;
        animator.Play("Take Damage");
        StopChase();
        yield return new WaitForSeconds(flinchDuration);
        isFlinching = false;
        ChaseTarget(); // Resume chasing after flinching

        // Add a cooldown before enemy can flinch again
        yield return new WaitForSeconds(flinchCooldown);
    }

    public void Despawning()
    {
        StopAllCoroutines();

        isDead = true;

        StopChase();

        // Disable Collider
        if (TryGetComponent<Collider>(out var collider)) collider.enabled = false;

        // Play dying animation
        animator.SetBool("isDead", true);

        // Destroy the enemy after a delay
        StartCoroutine(DelayedDestroy(gameObject, 3f)); // Example delay of 3 seconds
    }

    IEnumerator DelayedDestroy(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PhotonNetwork.IsMasterClient && obj != null && obj.GetComponent<PhotonView>() != null)
        {
            PhotonNetwork.Destroy(obj);
        }
    }

    private void OnDrawGizmos()
    {
        // Draw attack range as a red sphere
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw cone boundaries
        Vector3 forward = transform.forward * attackRange;
        Vector3 leftBoundary = Quaternion.Euler(0, -attackAngle / 2, 0) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, attackAngle / 2, 0) * forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, forward);
        Gizmos.DrawRay(transform.position, leftBoundary);
        Gizmos.DrawRay(transform.position, rightBoundary);

        // Fill cone for visualization
        Gizmos.color = new Color(1, 1, 0, 0.2f); // Transparent yellow
        for (float angle = -attackAngle / 2; angle <= attackAngle / 2; angle += 1f)
        {
            Vector3 direction = Quaternion.Euler(0, angle, 0) * forward;
            Gizmos.DrawRay(transform.position, direction);
        }
    }
}