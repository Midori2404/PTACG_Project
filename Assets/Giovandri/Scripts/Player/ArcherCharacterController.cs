using Cinemachine;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class ArcherCharacterController : MonoBehaviourPunCallbacks
{
    #region INSPECTOR VARIABLES
    [SerializeField] private float movementOffset = 45f;

    [Header("Player Movement Variables")]
    [SerializeField] private float playerSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Dash")]
    [SerializeField] private float dashingCooldown = 1.5f;
    [SerializeField] private float dashingTime = 0.2f;
    [SerializeField] private float dashingSpeed = 8f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private Camera mainCamera;

    [Header("Camera")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private AudioListener audioListener;

    [SerializeField] private LayerMask layerMask;
    [SerializeField] private Transform groundCheckPoint;

    #endregion

    #region PRIVATE VARIABLES
    //MOVEMENT
    private Vector3 moveInput;
    private Vector3 movementDirection;

    //DASH
    public bool canDash;
    private bool dashInput;

    private InputManager playerInputActions;
    private CharacterController characterController;

    //GRAVITY
    private float verticalVelocity = 0f; // Tracks the player's vertical movement

    #endregion

    #region PLAYER STATES
    public bool isAttacking = false;
    public bool isDashing = false;
    public bool isGrounded;


    #endregion

    #region PROPERTIES
    public Vector3 MoveInput => moveInput;

    #endregion

    private void Awake()
    {
        playerInputActions = new InputManager();
        characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        //if (photonView.IsMine)
        //{
        //    audioListener.enabled = true;
        //    virtualCamera.Priority = 1;
        //}
        //else
        //    virtualCamera.Priority = 0;
    }

    private void OnEnable()
    {
        playerInputActions.Player.Enable();
        canDash = true;
    }

    private void OnDisable()
    {
        playerInputActions.Player.Disable();
    }

    private void FixedUpdate()
    {
        //if (!photonView.IsMine) return;

        // Check if the player is grounded
        isGrounded = Physics.CheckSphere(groundCheckPoint.position, 0.3f, layerMask);

        ApplyGravity(); // Handle gravity

        HandleMouseRotation(); //RotateToMouse(); //Rotate Player to Cursor

        if (!isAttacking) // Prevent movement when attacking
        {
            GatherMovementInput();
            //MapMovement();
            PlayerMovement();
        }

        if (dashInput && canDash && isGrounded)
        {
            StartCoroutine(DashCooldown());
        }

    }

    private IEnumerator DashCooldown()
    {
        canDash = false;

        isDashing = true;
        yield return new WaitForSeconds(dashingTime);

        isDashing = false;
        yield return new WaitForSeconds(dashingCooldown);
        canDash = true;

    }

    private void MapMovement()
    {
        // Make the player move relative to the camera's direction
        if (moveInput == Vector3.zero) return;

        // Use the isometric matrix to adjust the movement direction
        Matrix4x4 isometricMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, movementOffset, 0));
        Vector3 multipliedMatrix = isometricMatrix.MultiplyPoint3x4(moveInput);

        // Apply the movement without affecting rotation
        movementDirection = multipliedMatrix.normalized;
    }

    private void PlayerMovement()
    {
        if (isDashing)
        {
            // Move forward in the current facing direction during a dash
            characterController.Move(movementDirection * playerSpeed * 3f * Time.deltaTime);
            Debug.Log("DASHING");
            return;
        }

        // Prevent horizontal movement if not grounded
        Vector3 moveDirection = isGrounded ? (movementDirection * playerSpeed * moveInput.magnitude * Time.deltaTime) : (movementDirection * playerSpeed / 3 * moveInput.magnitude * Time.deltaTime);
        moveDirection.y = verticalVelocity * Time.deltaTime;

        characterController.Move(moveDirection);
    }

    private void GatherMovementInput()
    {
        // Read movement input from the Player Input Actions
        Vector2 input = playerInputActions.Player.Move.ReadValue<Vector2>();
        moveInput = new Vector3(input.x, 0, input.y);

        // Convert input to camera-relative movement direction
        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        movementDirection = (forward * moveInput.z + right * moveInput.x).normalized;

        dashInput = playerInputActions.Player.Dash.IsPressed();
    }

    /*
    private void RotateToMouse()
    {
        // Get the mouse position in screen space
        Vector3 mousePosition = Input.mousePosition;

        // Raycast from the camera to the mouse position
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, 150f, layerMask))
        {
            // Get the point where the ray hits the ground
            Vector3 targetPoint = hitInfo.point;

            // Calculate the direction from the character to the target point
            Vector3 direction = targetPoint - transform.position;
            direction.y = 0; // Ignore the vertical axis for rotation

            // Smoothly rotate the character to face the target direction
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }*/

    private void HandleMouseRotation()
    {
        // Raycast from the mouse position to the world
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 150f))
        {
            // Calculate the direction to look at
            Vector3 lookDirection = hit.point - transform.position;
            lookDirection.y = 0; // Keep the character upright

            if (lookDirection.magnitude > 0.1f)
            {
                // Smoothly rotate the character to face the mouse
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

                // Apply rotation without affecting the position
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }
    }

    private void ApplyGravity()
    {
        if (isGrounded)
        {
            verticalVelocity = 0f; // Reset vertical velocity if on the ground
        }
        else if (!isGrounded)
        {
            verticalVelocity += gravity; // Apply gravity
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the ground check sphere in the editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheckPoint.position, 0.4f);
    }

    public CinemachineVirtualCamera GetPlayerCinemachineCamera()
    {
        return virtualCamera;
    }
}