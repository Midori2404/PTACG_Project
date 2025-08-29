using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using System.Linq;
using Photon.Pun;

public class Archer : MonoBehaviour
{
    public const string ARCHER_PROJECTILES = "Projectiles";

    [Header("Shooting Settings")]
    [SerializeField] private GameObject arrowPrefab;
    public Transform firePoint;
    public Transform RainArrowFirePoint;
    public Transform castPoint;
    [SerializeField] private float arrowSpeed = 20f;
    [SerializeField] private float shootCooldown = 0.5f;
    [SerializeField] private AnimationClip lightAttackClip;
    [SerializeField] private AudioClip archerShootSound;


    private InputManager playerInputActions;
    private InputAction lightAttackAction;
    private PlayerController playerController;
    private PlayerAttribute playerAttribute;
    private PlayerAnimationController playerAnimationController;

    private Animator playerAnimator;
    [HideInInspector] public PhotonView photonView;

    // State
    private bool inCooldown;
    [HideInInspector] public bool inSkillState;
    [HideInInspector] public bool isCasting;
    [HideInInspector] public bool hemorrhageActive;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();

        // Initialize input actions
        playerInputActions = new InputManager();
        lightAttackAction = playerInputActions.Player.LightAttack;

        playerController = GetComponentInParent<PlayerController>();
        playerAttribute = GetComponent<PlayerAttribute>();
        playerAnimationController = GetComponentInParent<PlayerAnimationController>();
        playerAnimator = GetComponentInParent<Animator>();
    }

    private void OnEnable()
    {
        playerInputActions.Enable();
    }

    private void OnDisable()
    {
        playerInputActions.Disable();
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine) return; // Only allow the local player to shoot.

        if (lightAttackAction.IsPressed() && !inCooldown && !inSkillState && !isCasting)
        {
            //PlayShootAnimation();
            //StartCoroutine(ShootCooldown());
            StartShooting();
        }

        UpdateAnimatorParameters(playerController.movementInput);
    }

    public void Shoot()
    {
        if (!photonView.IsMine) return; // Only allow the local player to shoot.

        Vector3 shootingDirection = firePoint.forward;
        float projectileDamage = playerAttribute.GetPlayerStats().currentDamage;
        int effectTypeInt = hemorrhageActive ? (int)NegativeEffectType.Bleed : (int)NegativeEffectType.None;

        // Retrieve the PhotonView from the parent or attached component.
        PhotonView shooterPV = GetComponentInParent<PhotonView>();
        int shooterId = shooterPV != null ? shooterPV.ViewID : 0;

        // If hemorrhage is active, get the primary skill details.
        PrimarySkill primarySkill = hemorrhageActive ? GetPrimarySkill() : null;
        float bleedDamagePerTick = hemorrhageActive && primarySkill != null ? primarySkill.bleedDamagePerTick : 0f;
        float bleedDuration = hemorrhageActive && primarySkill != null ? primarySkill.bleedDuration : 0f;
        float bleedTickInterval = hemorrhageActive && primarySkill != null ? primarySkill.bleedTickInterval : 0f;

        // Prepare instantiation data array.
        object[] initData = new object[]
        {
            shootingDirection,
            projectileDamage,
            effectTypeInt,
            shooterId,
            bleedDamagePerTick,
            bleedDuration,
            bleedTickInterval
        };

        string arrowPath = ARCHER_PROJECTILES + "/" + arrowPrefab.name;
        // Ensure that your arrowPrefab is placed in a Resources folder.
        GameObject arrow = PhotonNetwork.Instantiate(arrowPath, firePoint.position, Quaternion.identity, 0, initData);

        PhotonView arrowPV = arrow.GetComponent<PhotonView>();
        if (arrowPV != null && shooterPV != null)
        {
            arrowPV.TransferOwnership(shooterPV.Owner);
        }
    }



    private IEnumerator WaitForAnimationToFinish()
    {
        float attackSpeedMultiplier = playerAttribute.GetPlayerStats().currentAttackSpeed;
        float animationDuration = lightAttackClip.length / attackSpeedMultiplier;
        float adjustedCooldown = shootCooldown / attackSpeedMultiplier; // Scale cooldown

        Debug.Log($"Animation Duration: {animationDuration}, Adjusted Cooldown: {adjustedCooldown}");

        yield return new WaitForSeconds(animationDuration);
        playerController.SetCanMove(true);

        yield return new WaitForSeconds(adjustedCooldown);
        inCooldown = false;
    }



    public void StartShooting()
    {
        if (inCooldown) return;

        inCooldown = true; // Lock input immediately
        playerController.SetCanMove(false);

        SfxManager.instance.photonView.RPC("RPC_PlaySoundFXClipDelayed", RpcTarget.All, archerShootSound.name, transform.position, 0.15f, 0.7f);

        // Apply attack speed scaling
        float attackSpeedMultiplier = playerAttribute.GetPlayerStats().currentAttackSpeed;
        playerAnimator.SetFloat("AttackSpeed", attackSpeedMultiplier);

        AttackAnimation(true); // ATTACKING BOOLEAN TRUE
        StartCoroutine(WaitForAnimationToFinish()); // Ensure attack animation actually starts
    }


    private void UpdateAnimatorParameters(Vector3 movementInput)
    {
        Vector3 localInput = transform.InverseTransformDirection(movementInput);

        if (localInput == Vector3.zero)
        {
            playerAnimator.applyRootMotion = true;
        }
        else playerAnimator.applyRootMotion = false;

        playerAnimator.SetFloat("VelX", localInput.x, 0.1f, Time.deltaTime);
        playerAnimator.SetFloat("VelY", localInput.z, 0.1f, Time.deltaTime);
    }

    public void AttackAnimation(bool isAttacking)
    {
        playerAnimator.applyRootMotion = false;
        //playerAnimator.SetBool("isAttacking", isAttacking);
        if (photonView.IsMine)
        {
            photonView.RPC("RPC_PlayShootAnimation", RpcTarget.All);
        }
    }

    [PunRPC]
    public void RPC_PlayShootAnimation()
    {
        playerAnimator.SetTrigger("attackTrigger");
    }

    [PunRPC]
    public void RPC_CastRainArrow()
    {
        CastRainArrow();
    }

    [PunRPC]
    public void RPC_CastShockBlast()
    {
        CastShockBlast();
    }


    public void CastRainArrow()
    {
        playerController.SetCanMove(false);
        playerAnimator.Play("RainArrowDrawUp");
    }

    public void CastShockBlast()
    {
        playerController.SetCanMove(false);
        playerAnimator.CrossFade("ShockBlastDrawArrow", 0.1f);
    }

    public void ClearSkillState()
    {
        inSkillState = false;
        playerController.SetCanMove(true);
    }

    private PrimarySkill GetPrimarySkill()
    {
        // Find the first skill that is a PrimarySkill
        return playerAttribute.GetPlayerSkills().OfType<PrimarySkill>().FirstOrDefault();
    }

}
