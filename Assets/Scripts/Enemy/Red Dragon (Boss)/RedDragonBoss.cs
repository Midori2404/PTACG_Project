using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class RedDragonBoss : MonoBehaviourPunCallbacks, IDamageable
{
    // Resource Folder Paths
    public const string BOSS_PROJECTILES = "Boss/Boss_Projectiles";
    public const string BOSS_HIT_EFFECTS = "Boss/Boss_Hit_Effects";
    public const string BOSS_SKILL_EFFECTS = "Boss/Boss_Skill_Effects";
    public const string BOSS_MINIONS = "Boss/Boss_Minions";

    [Header("Components")]
    private Rigidbody rb;
    private BoxCollider boxCollider;
    private Animator animator;
    private RedDragonAnimationConstraint redDragonAnimationConstraint;
    private PhotonView photonView;

    [Header("Movement")]
    public float moveSpeed = 5f;
    private Transform target;
    private float checkInterval = 1f;
    private float nextCheckTime = 0f;

    [Header("Combat")]
    public Transform firePoint;
    public BossStats bossStats;

    [Header("Abilities")]
    public FireBall fireBall;
    public RainingMeteor rainingMeteor;
    public Minions minionsSummon;

    [Header("State")]
    public bool isCasting = false;
    public bool isDead = false;

    [Header("UI")]
    public HealthBar healthBar;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        redDragonAnimationConstraint = GetComponent<RedDragonAnimationConstraint>();
        photonView = GetComponent<PhotonView>();
        boxCollider = GetComponent<BoxCollider>();
    }

    private void Start()
    {
        // Initialize health bar if assigned
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(bossStats.maxHealth);
            healthBar.SetHealth(bossStats.currentHealth);
        }

    }

    private void FixedUpdate()
    {
        if (!PhotonNetwork.IsMasterClient || isCasting || isDead) return; // Stop movement while casting

        if (Time.time >= nextCheckTime)
        {
            FindClosestPlayer();
            nextCheckTime = Time.time + checkInterval;
        }

        if (target != null)
        {
            MoveTowardsTarget();

            // Execute the first available attack
            if (!isCasting)
            {
                ExecuteNextAvailableAttack();
            }
        }
        else
        {
            UpdateAnimation(Vector3.zero);
        }
    }

    private void ExecuteNextAvailableAttack()
    {
        if (Time.time >= fireBall.nextFireballTime)
        {
            ShootFireball();
        }
        else 
        if (Time.time >= rainingMeteor.nextMeteorTime)
        {
            SummonMeteorShower();
        }
        else if (Time.time >= minionsSummon.nextSummonTime)
        {
            SummonMinions();
        }
    }


    #region Movement
    private void MoveTowardsTarget()
    {
        if (isCasting || isDead || target == null) return; // Stop moving while casting or dead

        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0; // Lock vertical movement

        // Move using Transform (for kinematic Rigidbody)
        transform.position += direction * moveSpeed * Time.fixedDeltaTime;

        // Rotate towards target (Y-axis only)
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f);
        }

        UpdateAnimation(direction);
        photonView.RPC("SyncBossState", RpcTarget.Others, transform.position, transform.rotation, direction);
    }



    #endregion

    #region Find Player
    private void FindClosestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform closestPlayer = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject player in players)
        {
            PlayerAttribute playerAttribute = player.GetComponent<PlayerAttribute>();
            if (playerAttribute != null && !playerAttribute.isDead) // Check if player is alive
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player.transform;
                    redDragonAnimationConstraint.UpdateAllConstraints(closestPlayer);
                }
            }
        }

        if (closestPlayer != target)
        {
            target = closestPlayer;
            if (target != null)
            {
                photonView.RPC("SyncTarget", RpcTarget.Others, target.position);
            }
        }
        else if (target == null)
        {
            // If the target becomes null, find a new target immediately
            return;
        }
    }

    [PunRPC]
    private void SyncTarget(Vector3 targetPos)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (Vector3.Distance(player.transform.position, targetPos) < 1f)
            {
                target = player.transform;
                redDragonAnimationConstraint.UpdateAllConstraints(target);
                break;
            }
        }
    }

    #endregion


    #region Fireball

    private void ShootFireball()
    {
        if (target == null || isCasting) return;

        isCasting = true; // Stop movement & rotation
        ResetMovementAnimation(); // Reset movement animation parameters

        // CrossFade into "Projectile Attack" animation
        animator.CrossFade(fireBall.PROJECTILE_ATTACK, 0.1f);

        // Call RPC to sync animation across clients
        photonView.RPC("SyncBossAnimation", RpcTarget.Others, fireBall.PROJECTILE_ATTACK);

        // Start casting sequence
        StartCoroutine(CastFireball());
    }

    private IEnumerator CastFireball()
    {
        if (!PhotonNetwork.IsMasterClient) yield break;

        yield return new WaitForSeconds(fireBall.castTime);

        if (target == null)
        {
            isCasting = false;
            yield break;
        }

        // Play Sound Effect
        SfxManager.instance.photonView.RPC("RPC_PlaySoundFXClip", RpcTarget.All, fireBall.castFireballSound.name, transform.position, 0.7f);

        // Predict target movement for better accuracy
        Vector3 targetPosition = target.position + (target.forward * 1.5f);

        // Update the fireball's cooldown immediately before spawning the projectile
        fireBall.SetNextFireballTime();

        // Only the master client spawns the fireball.
        SpawnFireball(firePoint.position, firePoint.rotation, targetPosition);

        // (Remove the RPC call that spawns it on others)

        yield return new WaitForSeconds(0.5f); // Animation smoothing delay
        isCasting = false; // Allow movement and further attacks
    }

    private void SpawnFireball(Vector3 spawnPos, Quaternion spawnRot, Vector3 targetPos)
    {
        string fireballPath = fireBall.GetProjectilePath();
        GameObject fireballPrefab = Resources.Load<GameObject>(fireballPath);
        if (fireballPrefab == null)
        {
            Debug.LogError("Fireball prefab not found in Resources at: " + fireballPath);
            return;
        }

        // Calculate shooting direction and set up instantiation data for the Projectile.
        Vector3 shootDirection = (targetPos - spawnPos).normalized;
        // You can use the boss's damage, set effect type to None, and pass zero values for DOT parameters.
        float projectileDamage = bossStats.damage;
        NegativeEffectType effectType = NegativeEffectType.None;
        // Pass the boss's PhotonView ID as the owner.
        int shooterViewID = photonView.ViewID;
        object[] instantiationData = new object[]
        {
            shootDirection,
            projectileDamage,
            (int)effectType,
            shooterViewID,
            0f, // damagePerTick
            0f, // dotDuration
            0f  // tickInterval
        };

        // Instantiate the fireball via PhotonNetwork.Instantiate so it's networked.
        GameObject fireball = PhotonNetwork.Instantiate(fireballPath, spawnPos, spawnRot, 0, instantiationData);

        // The Projectile component on the fireball will now read these parameters in Awake() and initialize itself.
        // (The Projectile script should handle its own destruction after 'lifetime'.)
    }



    #endregion

    #region Meteor Shower

    public void SummonMeteorShower()
    {
        if (Time.time >= rainingMeteor.nextMeteorTime && !isCasting)
        {
            StartCoroutine(CastMeteorShower());
            rainingMeteor.SetNextMeteorTime();
        }
    }

    private IEnumerator CastMeteorShower()
    {
        isCasting = true; // Stop movement during cast
        ResetMovementAnimation(); // Reset movement animation parameters

        // Play animation
        animator.CrossFade(rainingMeteor.AnimationName, 0.1f);
        photonView.RPC("SyncBossAnimation", RpcTarget.Others, rainingMeteor.AnimationName);

        CameraShaker.Instance.StartRumble(CameraShaker.SHAKE_TYPE_RUMBLE, 0.8f, 0.08f);

        yield return new WaitForSeconds(rainingMeteor.castTime); // Wait for casting animation

        CameraShaker.Instance.StopRumble();

        // Activate safe zones across all clients
        photonView.RPC("ActivateSafeAreas", RpcTarget.All);

        // Play meteor particle effect across all clients
        // photonView.RPC("SpawnMeteorShower", RpcTarget.All);
        SpawnMeteorShower();

        yield return new WaitForSeconds(rainingMeteor.rainingMeteorDuration); // Wait for duration

        isCasting = false; // Allow movement again
    }

    [PunRPC]
    private void SpawnMeteorShower()
    {
        if (rainingMeteor.meteor != null)
        {
            // Build the path using the prefab's name.
            string meteorPath = BOSS_SKILL_EFFECTS + "/" + rainingMeteor.meteor.name;

            // Generate a random seed on the master.
            int seed = Random.Range(0, int.MaxValue);

            // Pass the seed in instantiation data.
            object[] instData = new object[] { seed };

            // Only the master calls PhotonNetwork.Instantiate. It will replicate to all clients.
            GameObject meteorInstance = PhotonNetwork.Instantiate(meteorPath, rainingMeteor.meteorSpawnLocation.position, Quaternion.identity, 0, instData);
        }
    }


    [PunRPC]
    private void ActivateSafeAreas()
    {
        if (rainingMeteor.safeAreasPoint.Length == 0) return;

        foreach (Transform safeArea in rainingMeteor.safeAreasPoint)
        {
            GameObject safeAreaObj = Instantiate(rainingMeteor.safeBubblesPrefab, safeArea.position, Quaternion.identity);
        }
    }



    #endregion

    #region Summon Minions

    public void SummonMinions()
    {
        if (Time.time >= minionsSummon.nextSummonTime && !isCasting)
        {
            StartCoroutine(CastSummonMinions());
            minionsSummon.SetNextSummonTime();
        }
    }

    private IEnumerator CastSummonMinions()
    {
        isCasting = true; // Stop movement
        ResetMovementAnimation(); // Reset movement animation parameters


        // Play summoning animation
        animator.CrossFade(minionsSummon.AnimationName, 0.1f);
        photonView.RPC("SyncBossAnimation", RpcTarget.Others, minionsSummon.AnimationName);

        // Spawn summoning circle at boss
        photonView.RPC("SpawnSummoningCircle", RpcTarget.All, transform.position);

        photonView.RPC("SpawnMinionEffects", RpcTarget.All);


        yield return new WaitForSeconds(minionsSummon.castTime); // Wait for casting time

        // Spawn minions and their effects
        photonView.RPC("SpawnMinions", RpcTarget.All);

        yield return new WaitForSeconds(0.5f); // Small delay after summon

        isCasting = false; // Resume movement
    }

    [PunRPC]
    private void SpawnSummoningCircle(Vector3 bossPosition)
    {
        if (minionsSummon.magicCircleEffect != null)
        {
            GameObject magicCircle = Instantiate(minionsSummon.magicCircleEffect, bossPosition + new Vector3(0, 0.1f, 0), Quaternion.Euler(-90, 0, 0));
            Destroy(magicCircle, 6.1f); // Summoning circle lasts 5 seconds
        }
    }

    [PunRPC]
    private void SpawnMinionEffects()
    {
        foreach (Transform spawnPoint in minionsSummon.spawnPoints)
        {
            if (minionsSummon.summonEffect != null)
            {
                GameObject summonEffectInstance = Instantiate(minionsSummon.summonEffect, spawnPoint.position + new Vector3(0, 1, 0), Quaternion.identity);
                Destroy(summonEffectInstance, 7f); // Effect disappears after 3 seconds
            }
        }
    }

    [PunRPC]
    private void SpawnMinions()
    {
        foreach (Transform spawnPoint in minionsSummon.spawnPoints)
        {
            // Load minion prefab dynamically
            string minionPath = minionsSummon.GetMinionPrefabPath();
            GameObject minionPrefab = Resources.Load<GameObject>(minionPath);

            if (minionPrefab == null)
            {
                Debug.LogError("Minion prefab not found at: " + minionPath);
                return;
            }

            // Use the full resource path for PhotonNetwork.Instantiate
            GameObject minion = PhotonNetwork.Instantiate(minionPath, spawnPoint.position, Quaternion.identity);

            // Initialize the minion attributes using EnemyAttribute via EnemyBehaviour
            EnemyBehaviour enemyBehaviour = minion.GetComponent<EnemyBehaviour>();
            if (enemyBehaviour != null)
            {
                EnemyAttribute attributes = minionsSummon.minionAttribute;
                enemyBehaviour.GetComponent<PhotonView>().RPC("RPC_InitializeAttributes", RpcTarget.All,
        attributes.maxHealth,
    attributes.movementSpeed,
    attributes.attackDamage,
    attributes.attackRate,
    attributes.attackRange,
    (int)attributes.enemyType);

            }
        }
    }





    #endregion

    #region Taking Damage & Death
    // Update the TakeDamage() method to update the health bar:
    [PunRPC]
    public void TakeDamage(float amount)
    {
        if (isDead) return; // Ignore damage if already dead

        bossStats.currentHealth -= amount;

        // Clamp current health to non-negative value
        if (bossStats.currentHealth < 0)
            bossStats.currentHealth = 0;

        // Sync updated health bar across all clients
        photonView.RPC("UpdateHealthBarRPC", RpcTarget.All, bossStats.currentHealth);

        if (bossStats.currentHealth <= 0)
        {
            bossStats.currentHealth = 0;
            Die();
        }
    }



    private void Die()
    {
        if (isDead) return;
        isDead = true;
        isCasting = true; // Prevent further attacks

        // Play death animation
        animator.CrossFade("Die", 0.1f);
        photonView.RPC("SyncBossDeath", RpcTarget.Others);

        // Disable boss collider
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }

        // Destroy all minions
        photonView.RPC("KillAllMinions", RpcTarget.All);

        // Remove the boss after a delay
        StartCoroutine(DestroyBoss());

        GameManager.Instance.ShowVictoryScreen();
    }

    [PunRPC]
    private void UpdateHealthBarRPC(float currentHealth)
    {
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }
    }

    [PunRPC]
    private void SyncBossDeath()
    {
        isDead = true;
        isCasting = true;
        animator.CrossFade("Die", 0.1f);
        if (boxCollider != null) boxCollider.enabled = false;
    }

    [PunRPC]
    private void KillAllMinions()
    {
        GameObject[] minions = GameObject.FindGameObjectsWithTag("Enemy"); // Assuming minions are tagged "Enemy"
        foreach (GameObject minion in minions)
        {
            if (minion.GetComponent<EnemyBehaviour>() != null)
            {
                minion.GetComponent<EnemyBehaviour>().TakeDamage(9999); // Instantly kill
            }
        }
    }

    private IEnumerator DestroyBoss()
    {
        yield return new WaitForSeconds(15f); // Allow death animation to play

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }


    #endregion

    #region Animation
    private void UpdateAnimation(Vector3 movementDirection)
    {
        Vector3 localDirection = transform.InverseTransformDirection(movementDirection);
        animator.SetFloat("ForwardBackward", localDirection.z, 0.1f, Time.deltaTime);
        animator.SetFloat("StrafeLeftRight", localDirection.x, 0.1f, Time.deltaTime);
    }

    private void ResetMovementAnimation()
    {
        animator.SetFloat("ForwardBackward", 0f);
        animator.SetFloat("StrafeLeftRight", 0f);
    }


    [PunRPC]
    private void SyncBossState(Vector3 pos, Quaternion rot, Vector3 movementDir)
    {
        transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * 10f);
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, Time.deltaTime * 10f);
        UpdateAnimation(movementDir);
    }

    [PunRPC]
    private void SyncBossAnimation(string animationName)
    {
        animator.CrossFade(animationName, 0.1f);
    }

    #endregion
}



[System.Serializable]
public class BossStats
{
    public float maxHealth = 1000f;
    public float currentHealth = 1000f;
    public float damage = 30f;
    public float moveSpeed = 5f;
}



[System.Serializable]
public class FireBall
{
    public GameObject projectile; // Fireball prefab reference (assigned in Inspector)
    public AudioClip castFireballSound;
    public float speed = 10f;
    public float lifetime = 5f;

    [Header("Casting Time & Animation Name")]
    public float castTime = 0.8f;
    public string PROJECTILE_ATTACK = "Projectile Attack";

    [Header("Cooldown System")]
    public float fireballCooldown = 3f;
    public float nextFireballTime = 0f;

    // Get the prefab name dynamically
    public string GetProjectilePath()
    {
        return RedDragonBoss.BOSS_PROJECTILES + "/" + projectile.name;
    }

    public void SetNextFireballTime()
    {
        nextFireballTime = Time.time + fireballCooldown;
    }
}



[System.Serializable]
public class RainingMeteor
{
    [Header("Prefabs & Particles")]
    public GameObject meteor;
    public GameObject meteorImpactPrefab;
    public Transform meteorSpawnLocation;
    public GameObject safeBubblesPrefab;
    public Transform[] safeAreasPoint;

    [Header("Ability Properties")]
    public float rainingMeteorDuration = 6f;

    [Header("Casting Time & Animation Name")]
    public float castTime = 0.3f;
    public string AnimationName = "Cast Spell";

    [Header("Cooldown System")]
    public float meteorCooldown = 3f;
    public float nextMeteorTime = 0f;

    public string GetHitEffectPath()
    {
        return RedDragonBoss.BOSS_HIT_EFFECTS + "/" + meteorImpactPrefab.name;
    }

    public void SetNextMeteorTime()
    {
        nextMeteorTime = Time.time + meteorCooldown;
    }
}



[System.Serializable]
public class Minions
{
    [Header("Summon Effect Prefabs")]
    public GameObject magicCircleEffect;
    public GameObject summonEffect;

    [Header("Minion Properties")]
    public EnemyAttribute minionAttribute;
    public Transform[] spawnPoints;

    [Header("Casting Time & Animation Name")]
    public float castTime = 0.3f;
    public string AnimationName = "Cast Spell";

    [Header("Cooldown System")]
    public float summonCooldown = 60f;
    public float nextSummonTime = 0f;

    public string GetMagicCirclePath()
    {
        return RedDragonBoss.BOSS_SKILL_EFFECTS + "/" + magicCircleEffect.name;
    }

    public string GetSummonEffectPath()
    {
        return RedDragonBoss.BOSS_SKILL_EFFECTS + "/" + summonEffect.name;
    }

    public string GetMinionPrefabPath()
    {
        return RedDragonBoss.BOSS_MINIONS + "/" + minionAttribute.enemyPrefab.name;
    }

    public void SetNextSummonTime()
    {
        nextSummonTime = Time.time + summonCooldown;
    }

}