using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class BossCutsceneManager : MonoBehaviour
{
    // public GameObject virtualCameraFollow;
    // public GameObject virtualCameraZoomOut;
    // public GameObject particleEffect;
    // public float roarDuration;

    // private RedDragonBoss redDragonBoss;
    // private BossPhaseManager bossPhaseManager;
    // private RedDragonAnimationConstraint redDragonAnimationConstraint;
    // private CinemachineImpulseSource cinemachineImpulseSource;

    // // Start is called before the first frame update
    // void Start()
    // {
    //     DisableCutsceneCamera();
    //     redDragonBoss = GetComponent<RedDragonBoss>();
    //     cinemachineImpulseSource = GetComponent<CinemachineImpulseSource>();
    //     bossPhaseManager = GetComponent<BossPhaseManager>();
    //     redDragonAnimationConstraint = GetComponent<RedDragonAnimationConstraint>();

    //     bossPhaseManager.OnPhaseChange += BossCutsceneManager_StartCinematic;
    // }

    // private void BossCutsceneManager_StartCinematic(object sender, EventArgs e)
    // {
    //     StartCoroutine(StartCinematic());
    // }

    // IEnumerator StartCinematic()
    // {
    //     virtualCameraFollow.SetActive(true);
    //     redDragonBoss.currentState = RedDragonBoss.State.Cutscene;
    //     redDragonAnimationConstraint.SmoothlySetConstraintWeights(0f, 1f);
    //     redDragonBoss.StopAttacking();
    //     yield return redDragonBoss.MoveToCenter();

    //     // Wait until completed done return to center
    //     yield return new WaitForSeconds(1f);

    //     Instantiate(particleEffect, transform.position + new Vector3(0, 0.3f, 0), Quaternion.identity);

    //     virtualCameraZoomOut.SetActive(true);
    //     redDragonBoss.currentState = RedDragonBoss.State.Roar;
    // }

    // /// <summary>
    // /// This function is meant to be called using animation event
    // /// </summary>
    // public void StartRoaring()
    // {
    //     StartCoroutine(RoarEffect());
    // }

    // IEnumerator RoarEffect()
    // {
    //     float elapsed = 0f; // Tracks elapsed time

    //     while (elapsed < roarDuration)
    //     {
    //         CameraShaker.Instance.CameraShake(cinemachineImpulseSource, 0.5f);
    //         yield return new WaitForSeconds(0.2f); // Pause between shakes
    //         elapsed += 0.2f; // Increment elapsed time by the wait duration
    //     }

    //     DisableCutsceneCamera();
    //     StartAttacking();
    // }
    
    // void DisableCutsceneCamera()
    // {
    //     virtualCameraFollow.SetActive(false);
    //     virtualCameraZoomOut.SetActive(false);
    // }

    // void StartAttacking()
    // {
    //     redDragonAnimationConstraint.SmoothlyResetToInitialWeights(2f);
    //     redDragonBoss.currentState = RedDragonBoss.State.None;
    //     redDragonBoss.StartAttacking();
    // }
}
