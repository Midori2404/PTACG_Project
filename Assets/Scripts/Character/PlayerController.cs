using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using Photon.Pun;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f; // Walking speed
    public float groundDrag = 5f;
    public float airMultiplier;

    [Header("Ground Check")]
    public float playerHeight = 2f;
    public LayerMask whatIsGround;
    private bool grounded;
    public float raycastLength;

    [Header("Slope Handling")]
    public float maxSlopeAngle = 45f;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;
    private Rigidbody rb;
    private Animator animator;

    [Header("Dash Settings")]
    public float dashForce = 20f;
    public float dashDuration = 0.2f;
    public float dashingCooldown = 1.5f;
    private bool isDashing = false;
    private bool canDash;
    public GameObject dashVFX;
    public GameObject dashTrailVFX;
    public AudioClip audioClip;

    public Vector3 movementInput;
    private bool canMove = true; // Flag to control movement
    private bool isOnSlope = false;
    private Vector3 slopeNormal;

    private PlayerAttribute playerAttribute;
    public Camera _camera; // Reference to the main camera

    private PhotonView photonView;

    private void Start()
    {
        photonView = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        animator = GetComponent<Animator>();
        playerAttribute = GetComponent<PlayerAttribute>();
    }

    private void Update()
    {
        if (!photonView.IsMine)
            return;
        // Ground check using Raycast
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        GetInput();
        SpeedControl();

        // Apply drag when grounded
        rb.drag = grounded ? groundDrag : 0;

        if (Input.GetKeyDown(KeyCode.Space) && canMove && !isDashing && canDash)
        {
            StartCoroutine(Dash());
        }


    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine || playerAttribute.isDead) return; // Prevents movement updates for other players.

        if (playerAttribute.GetPlayerClass() == PlayerClass.Warrior)
        {
            if (canMove && !isDashing)
            {
                MovePlayer();
                RotatePlayer();
            }
        }
        if (playerAttribute.GetPlayerClass() == PlayerClass.Archer && !GetComponent<Archer>().inSkillState)
        {
            RotatePlayer();
            if (canMove && !isDashing)
            {
                MovePlayer();
            }
        }
    }

    private void OnEnable()
    {
        canDash = true;
    }

    private void GetInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        movementInput = new Vector3(horizontalInput, 0, verticalInput).normalized;
    }

    private void MovePlayer()
    {
        // Calculate movement direction relative to the camera
        Vector3 forward = _camera.transform.forward;
        Vector3 right = _camera.transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        moveDirection = (forward * verticalInput + right * horizontalInput).normalized; // Normalize the movement direction

        // Move on slopes
        if (OnSlope() && !exitingSlope)
        {
            rb.velocity = GetSlopeMoveDirection() * moveSpeed;

            if (rb.velocity.y > 0)
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        }
        // Move on flat ground
        else if (grounded)
        {
            rb.velocity = moveDirection * moveSpeed;
        }
        // Move in air (reduced control)
        else if (!grounded)
        {
            rb.velocity = new Vector3(moveDirection.x * moveSpeed, rb.velocity.y, moveDirection.z * moveSpeed);
        }

        // Prevent sliding on slopes
        rb.useGravity = !OnSlope();

        // Prevent sliding when grounded and not moving
        if (grounded && movementInput == Vector3.zero)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
    }

    private void SpeedControl()
    {
        // Limit speed on slopes
        if (OnSlope() && !exitingSlope)
        {
            if (rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;
        }
        // Limit speed on flat ground or in air
        else
        {
            Vector3 flatVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            if (flatVelocity.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVelocity.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            isOnSlope = angle < maxSlopeAngle && angle != 0;
            slopeNormal = slopeHit.normal;
            return isOnSlope;
        }
        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }

    private void RotatePlayer()
    {
        Vector3 targetPoint = GetMousePositionHitPoint();
        if (targetPoint != null)
        {
            Vector3 direction = targetPoint - transform.position;
            direction.y = 0; // Ignore the vertical axis for rotation

            // Smoothly rotate the character to face the target direction
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            }
        }
    }

    public void SetCanMove(bool value)
    {
        if (photonView != null && photonView.IsMine)
        {
            canMove = value;
            rb.velocity = Vector3.zero;
        }
        else if (photonView == null)
        {
            canMove = value;
            rb.velocity = Vector3.zero;
        }

    }

    private IEnumerator Dash()
    {
        SfxManager.instance.photonView.RPC("RPC_PlaySoundFXClip", RpcTarget.All, audioClip.name, transform.position, 1f);

        // sets the canDash to false because the player is dashing
        canDash = false;
        isDashing = true;

        // Activate VFX
        if (dashVFX != null)
        {
            dashVFX.SetActive(true);
        }

        // yield return new WaitForSeconds(dashDuration);
        float startTime = Time.time;

        // Lock the dash direction at the start
        Vector3 dashDirection = movementInput.normalized;
        if (isOnSlope)
        {
            dashDirection = Vector3.ProjectOnPlane(dashDirection, slopeNormal);
        }

        // NOTICE: Disable root motion during dash if the animation uses root motion
        animator.applyRootMotion = false;

        // Maintain velocity in the dash direction
        while (Time.time < startTime + dashDuration)
        {
            rb.velocity = dashDirection * dashForce;
            yield return null; // Wait for the next frame
        }

        // Stop the dash smoothly
        rb.velocity = Vector3.zero;
        isDashing = false;

        // Deactivate VFX
        if (dashVFX != null)
        {
            dashVFX.SetActive(false);
        }

        // Puts a cooldown on the dash 
        yield return new WaitForSeconds(dashingCooldown);

        // After the cooldown the player can dash again
        canDash = true;

        // Re-enable root motion after dash
        animator.applyRootMotion = true;
    }

    public void PlayMoveSpeedTrail(float duration)
    {
        if (dashTrailVFX != null)
        {
            ParticleSystem ps = dashTrailVFX.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = duration;
            ps.Play();
        }
    }


    public Vector3 GetMousePositionHitPoint()
    {
        // Get the mouse position in screen space
        Vector3 mousePosition = Input.mousePosition;

        // Raycast from the camera to the mouse position
        Ray ray = _camera.ScreenPointToRay(mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, raycastLength, whatIsGround))
        {
            // Return the point where the ray hits the ground
            return hitInfo.point;
        }

        // Return null if no valid hit point is found
        return Vector3.zero;
    }

    public Rigidbody GetRigidbodyComponent()
    {
        return rb;
    }


    private void OnDrawGizmosSelected()
    {
        // Set Raycast color
        Gizmos.color = Color.red;

        // Define Raycast Start Position
        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = Vector3.down * (playerHeight * 0.5f + 0.2f); // Same as the raycast range

        // Draw the Raycast
        Gizmos.DrawRay(rayOrigin, rayDirection);
    }

    #region Revive & Fallen Animation
    public void PlayDeadAnimation()
    {
        photonView.RPC("RPC_PlayDeadAnimation", RpcTarget.All, "Dead");
    }

    [PunRPC]
    public void RPC_PlayDeadAnimation(string animationName)
    {
        animator.CrossFade(animationName, 0.1f);
    }

    public void PlayRiseAnimation()
    {
        photonView.RPC("RPC_PlayRiseAnimation", RpcTarget.All, "Rise");
    }

    [PunRPC]
    public void RPC_PlayRiseAnimation(string animationName)
    {
        animator.CrossFade(animationName, 0.1f);
    }

    #endregion

}