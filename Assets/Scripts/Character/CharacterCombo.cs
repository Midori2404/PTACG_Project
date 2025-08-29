using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Photon.Pun;
using UnityEngine;

[System.Serializable]
public class MeleeParticleEffect
{
    public GameObject particleEffect;
    public Vector3 position;
    public Vector3 rotation;
    public float timeDelay;
}

public class CharacterCombo : MonoBehaviour
{
    [Header("Attack Settings")]
    public Animator animator; // Reference to Animator
    public string[] attackAnimations; // Array of attack animation names
    public Transform attackPoint;
    public float attackRadius = 1.5f;
    public float attackRange = 2f;
    public LayerMask enemyLayer;
    public GameObject swordHitImpact;

    public Vector3 offset = new Vector3(1f, 0, 1f);
    public float radius;

    private HashSet<Collider> hitEnemies = new HashSet<Collider>();

    [Header("Combo Settings")]
    public float comboResetTime = 1.0f; // Time to reset combo if no input
    public float inputBufferTime = 0.2f; // Time window to buffer next input
    private float inputBufferTimer = 0.0f;
    private bool inputBuffered = false;

    private int currentComboIndex = 0; // Current attack in the combo
    private bool isAttacking = false; // Is the player currently attacking?
    private float comboTimer = 0.0f; // Timer to reset combo

    public PlayerController playerController;
    public PhotonView photonView;

    [Header("Sword Slash VFX")]
    public List<MeleeParticleEffect> meleeParticleEffects;

    [Header("Sword Swing Sound Effect")]
    public AudioClip swordSwingSound;

    [Header("Debug")]
    public bool enableDebug = true;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    private void Update()
    {
        if (!photonView.IsMine) return; // Only allow the local player

        HandleInput();
        UpdateComboTimer();

        // Update animator parameters
        UpdateAnimatorParameters(playerController.movementInput);
    }

    private void HandleInput()
    {
        if (Input.GetButtonDown("Fire1")) // Replace "Fire1" with your attack input
        {
            if (isAttacking)
            {
                BufferInput();
            }
            else
            {
                StartCombo();
            }
        }
    }

    private void StartCombo()
    {
        if (enableDebug) Debug.Log("Starting Combo...");
        isAttacking = true;
        playerController.SetCanMove(false); // Disable movement
        TriggerAttackAnimation(currentComboIndex);
    }

    private void BufferInput()
    {
        if (enableDebug) Debug.Log("Input Buffered!");
        inputBuffered = true;
        inputBufferTimer = inputBufferTime;
    }

    private void UpdateComboTimer()
    {
        if (isAttacking)
        {
            comboTimer += Time.deltaTime;

            // Check if the combo should reset
            if (comboTimer > comboResetTime)
            {
                ResetCombo();
            }

            // Decrease buffer timer
            if (inputBuffered)
            {
                inputBufferTimer -= Time.deltaTime;
                if (inputBufferTimer <= 0.0f)
                {
                    inputBuffered = false;
                }
            }
        }
    }

    public void OnAnimationEvent(string eventName)
    {
        if (!photonView.IsMine) return;

        if (eventName == "ComboWindow")
        {
            inputBuffered = true;
            // nextAttack = false;
            inputBufferTimer = inputBufferTime;
        }
        else if (eventName == "ComboEnd")
        {
            if (inputBuffered)
            {
                AdvanceCombo();
            }
            else
            {
                ResetCombo();
            }
        }
    }

    public void OnAttackAnimation(int velocity)
    {
        if (velocity > 0)
        {
            StartCoroutine(MoveForward(velocity));
        }
    }

    public void OnLungeAnimation(int velocity)
    {
        if (!photonView.IsMine) return;
        
        if (velocity > 0)
            StartCoroutine(Lunge(velocity));
    }

    private void AdvanceCombo()
    {
        if (enableDebug) Debug.Log("Advancing Combo...");

        inputBuffered = false; // Clear buffered input
        inputBufferTimer = 0.0f;

        currentComboIndex++;
        if (currentComboIndex >= attackAnimations.Length)
        {
            currentComboIndex = 0; // Loop or reset combo
        }

        TriggerAttackAnimation(currentComboIndex);
    }

    private void ResetCombo()
    {
        if (enableDebug) Debug.Log("Resetting Combo...");
        isAttacking = false;
        currentComboIndex = 0;
        comboTimer = 0.0f;
        inputBuffered = false;
        playerController.SetCanMove(true); // Re-enable movement

        // Reset Animator speed to default
        animator.speed = 1.0f;
    }

    // Called via Animation Event
    public void PerformMeleeAttack()
    {
        if (!photonView.IsMine)
            return;
            
        hitEnemies.Clear(); // Clear previous hits

        // 1. **Hitbox Detection (Precise)**
        Collider[] enemiesInRange = Physics.OverlapSphere(attackPoint.position, attackRadius, enemyLayer);
        foreach (Collider enemy in enemiesInRange)
        {
            if (!hitEnemies.Contains(enemy))
            {
                hitEnemies.Add(enemy);
                ApplyDamage(enemy);

                CinemachineImpulseSource cinemachineImpulseSource = GetComponent<CinemachineImpulseSource>();
                CameraShaker.Instance.ShakeCamera(CameraShaker.SHAKE_TYPE_RECOIL, 0.2f);

                // Spawn hit impact effect
                if (swordHitImpact != null)
                {
                    Instantiate(swordHitImpact, attackPoint.position, Quaternion.identity);
                }
            }
        }

        // 2. **Raycast Detection (Frame Perfect)**
        RaycastHit[] hits = Physics.RaycastAll(attackPoint.position, transform.forward, attackRange, enemyLayer);
        foreach (RaycastHit hit in hits)
        {
            if (!hitEnemies.Contains(hit.collider))
            {
                hitEnemies.Add(hit.collider);
                ApplyDamage(hit.collider);
            }
        }
    }

    void ApplyDamage(Collider enemy)
    {
        IDamageable damageable = enemy.GetComponent<IDamageable>();
        if (damageable != null)
        {
            float damageAmount = playerController.GetComponent<PlayerAttribute>().GetPlayerStats().currentDamage;
            PhotonView enemyPhotonView = enemy.GetComponent<PhotonView>();
            if (enemyPhotonView != null)
            {
                // This RPC method should be marked with [PunRPC] on the enemy script.
                enemyPhotonView.RPC("TakeDamage", RpcTarget.All, damageAmount);
            }
            else
            {
                damageable.TakeDamage(damageAmount);
            }
        }
        else
        {
            Debug.LogWarning("No IDamageable component found!");
        }
    }


    private IEnumerator MoveForward(float velocity)
    {
        float duration = 0.3f; // Duration to apply the force
        float timer = 0f;

        Rigidbody rb = playerController.GetRigidbodyComponent();

        while (timer < duration)
        {
            Vector3 forwardDirection = playerController.transform.forward;
            rb.AddForce(forwardDirection * velocity, ForceMode.VelocityChange);

            timer += Time.deltaTime;
            yield return null; // Wait for the next frame
        }
    }

    private IEnumerator Lunge(float initialVelocity)
    {
        float duration = 1.5f; // Duration to apply the force
        float timer = 0f;
        // initialVelocity = 10f; // Initial velocity
        float finalVelocity = 0f; // Final velocity

        Rigidbody rb = playerController.GetRigidbodyComponent();
        playerController.SetCanMove(false); // Disable movement

        while (timer < duration)
        {
            float t = timer / duration; // Normalized time (0 to 1)
            float currentVelocity = Mathf.Lerp(initialVelocity, finalVelocity, t); // Linearly interpolate velocity

            Vector3 forwardDirection = playerController.transform.forward;
            rb.AddForce(forwardDirection * currentVelocity, ForceMode.VelocityChange);

            timer += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // playerController.SetCanMove(true); // Re-enable movement
    }

    private void TriggerAttackAnimation(int index)
    {
        if (!photonView.IsMine) return;

        if (index < 0 || index >= attackAnimations.Length)
        {
            Debug.LogWarning("Invalid animation index!");
            return;
        }


        // Update animator speed based on player's current attack speed
        UpdateAnimatorSpeed();

        string animationName = attackAnimations[index];
        photonView.RPC("RPC_PlayAttackAnimation", RpcTarget.All, animationName);
        StartCoroutine(PlaySlashVFX(index));

        SfxManager.instance.photonView.RPC("RPC_PlaySoundFXClip", RpcTarget.All, swordSwingSound.name, transform.position, 0.7f);

        // Reset combo timer
        comboTimer = 0.0f;
    }

    private void UpdateAnimatorSpeed()
    {
        PlayerAttribute playerAttribute = playerController.GetComponent<PlayerAttribute>();
        PlayerStats playerStats = playerAttribute.GetPlayerStats();
        animator.speed = playerStats.currentAttackSpeed;
    }

    public void UseSkillAnimation()
    {
        // Trigger ultimate animation
        animator.SetTrigger("SwordSmash");
    }

    public void Berserk()
    {
        // Trigger ultimate animation
        animator.SetTrigger("Berserk");
    }

    [PunRPC]
    public void RPC_PlaySwordSmashAnimation()
    {
        animator.SetTrigger("SwordSmash");
    }

    [PunRPC]
    public void RPC_PlayBerserkAnimation()
    {
        animator.SetTrigger("Berserk");
    }

    [PunRPC]
    public void RPC_PlayAttackAnimation(string animationName)
    {
        animator.CrossFadeInFixedTime(animationName, 0.25f);
    }



    private void UpdateAnimatorParameters(Vector3 movementInput)
    {
        Vector3 localInput = transform.InverseTransformDirection(movementInput);

        // if (localInput == Vector3.zero)
        // {
        //     animator.applyRootMotion = true;
        // }
        // else animator.applyRootMotion = false;

        animator.SetFloat("VelX", localInput.x, 0.1f, Time.deltaTime);
        animator.SetFloat("VelY", localInput.z, 0.1f, Time.deltaTime);
    }

    // For Animation Event
    public void AllowPlayerMove()
    {
        playerController.SetCanMove(true);
    }

    public void DisallowPlayerMove()
    {
        playerController.SetCanMove(false);
    }

    public IEnumerator PlaySlashVFX(int index)
    {
        yield return new WaitForSeconds(meleeParticleEffects[index].timeDelay);
        Vector3 spawnPos = transform.position + meleeParticleEffects[index].position;
        Quaternion spawnRot = transform.rotation * Quaternion.Euler(meleeParticleEffects[index].rotation);
        // Pass the index along with position and rotation:
        photonView.RPC("RPC_SpawnSlashEffect", RpcTarget.All, index, spawnPos, spawnRot);
    }

    [PunRPC]
    private void RPC_SpawnSlashEffect(int index, Vector3 pos, Quaternion rot)
    {
        if (meleeParticleEffects != null && index >= 0 && index < meleeParticleEffects.Count && meleeParticleEffects[index].particleEffect != null)
        {
            // Instead of instantiating, use the pre-existing effect object
            GameObject effectObj = meleeParticleEffects[index].particleEffect;
            effectObj.transform.position = pos;
            effectObj.transform.rotation = rot;
            ParticleSystem ps = effectObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }
            else
            {
                Debug.LogWarning("No ParticleSystem found on the slash effect object!");
            }
        }
    }



    void OnDrawGizmos()
    {
        // Draw Hitbox for debugging
        if (attackPoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(attackPoint.position, attackPoint.position + transform.forward * attackRange);
        }
    }
}