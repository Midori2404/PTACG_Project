using UnityEngine;
using Photon.Pun;

public class EffectParentSync : MonoBehaviourPun
{
    [PunRPC]
    public void RPC_SetParent(int parentPhotonViewID, Vector3 targetLocalScale)
    {
        PhotonView parentPV = PhotonView.Find(parentPhotonViewID);
        if (parentPV != null)
        {
            transform.SetParent(parentPV.transform);
            transform.localScale = targetLocalScale;
        }
        else
        {
            Debug.LogWarning("EffectParentSync: Parent with ViewID " + parentPhotonViewID + " not found.");
        }
    }
}
