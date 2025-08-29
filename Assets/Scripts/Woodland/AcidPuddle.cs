using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class AcidPuddle : MonoBehaviourPunCallbacks
{
    [SerializeField] private float damagePerTick = 2f;
    [SerializeField] private float effectDuration = 3f;
    [SerializeField] private float tickInterval = 1f;


    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player")) // Check if the object is the player
        {
            PhotonView targetPhotonView = other.GetComponent<PhotonView>();

            if (targetPhotonView != null && PhotonNetwork.IsMasterClient) // Only MasterClient sends the RPC
            {
                // Apply the Paralyze effect via NegativeEffectManager
                NegativeEffectManager.Instance.photonView.RPC("RPC_ApplyNegativeEffect", 
                RpcTarget.AllBuffered, 
                targetPhotonView.ViewID, 
                damagePerTick, 
                effectDuration, 
                tickInterval, 
                (int)NegativeEffectType.Paralyze); // Convert enum to int
            }
        }
    }

}
