using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private ArcherCharacterController archerCharacterController; // Reference to ArcherCharacterController script

    private Animator playerAnimator;

    private Vector3 moveInput;

    [SerializeField] private float lerpSpeed = 10f; // Speed of transition to idle

    private void Awake()
    {
        // Get the Animator component
        playerAnimator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        MovementAnimation();
    }

    private void MovementAnimation()
    {
        moveInput = archerCharacterController.MoveInput;
        //Debug.Log($"MoveInput: X={moveInput.x}, Z={moveInput.z}");
        
        if (moveInput == Vector3.zero)
        {
            playerAnimator.applyRootMotion = true;

            // Gradually reduce velocityX and velocityZ to 0 for a smooth idle transition
            float currentVelocityX = playerAnimator.GetFloat("velocityX");
            float currentVelocityZ = playerAnimator.GetFloat("velocityZ");

            playerAnimator.SetFloat("velocityX", Mathf.Lerp(currentVelocityX, 0f, Time.deltaTime * lerpSpeed));
            playerAnimator.SetFloat("velocityZ", Mathf.Lerp(currentVelocityZ, 0f, Time.deltaTime * lerpSpeed));

            // Set running state to false when close to idle
            if (Mathf.Approximately(currentVelocityX, 0f) && Mathf.Approximately(currentVelocityZ, 0f))
            {
                playerAnimator.SetBool("isRunning", false);
            }
            
            return;
        }
        else
        {
            // Directly set animator parameters based on input
            playerAnimator.applyRootMotion = false;

            playerAnimator.SetBool("isRunning", true);
            playerAnimator.SetFloat("velocityX", Mathf.Lerp(playerAnimator.GetFloat("velocityX"), Mathf.Round(moveInput.x), Time.deltaTime * lerpSpeed));
            playerAnimator.SetFloat("velocityZ", Mathf.Lerp(playerAnimator.GetFloat("velocityZ"), Mathf.Round(moveInput.z), Time.deltaTime * lerpSpeed));
        }
    }

    public void AttackAnimation(bool isAttacking)
    {
        playerAnimator.applyRootMotion = false;
        playerAnimator.SetBool("isAttacking", isAttacking);
    }
}
