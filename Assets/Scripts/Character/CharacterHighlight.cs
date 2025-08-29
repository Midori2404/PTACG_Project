using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class CharacterHighlight : MonoBehaviourPunCallbacks
{
    public Image triangle;
    
    void Start()
    {
        // Only the owner (or a designated authority) sets the color and broadcasts it.
        if (photonView.IsMine)
        {
            // Decide the color based on a condition (e.g., MasterClient vs. others)
            Color highlightColor = PhotonNetwork.IsMasterClient ? Color.white : Color.blue;
            // Send an RPC to all clients (buffered so new joiners get the update)
            photonView.RPC("RPC_UpdateColor", RpcTarget.AllBuffered, highlightColor.r, highlightColor.g, highlightColor.b, highlightColor.a);
        }
    }

    [PunRPC]
    void RPC_UpdateColor(float r, float g, float b, float a)
    {
        Color newColor = new Color(r, g, b, a);
        if (triangle != null)
        {
            triangle.color = newColor;
        }
    }
}
