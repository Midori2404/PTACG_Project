using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class BurnChallenge : MonoBehaviour
{
    [Header("Burn Settings")]
    public float burnDamagePerTick = 5f;       // Damage per tick while burning
    public float tickInterval = 1f;            // Time between each tick
    public float burnDuration = 5f;            // Total burn duration
    public float intervalBetweenBurns = 60f;   // How often (in seconds) the burn is applied

    public static BurnChallenge Instance;
    public bool isGameOver;

    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(BurnRoutine());
    }

    private IEnumerator BurnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(intervalBetweenBurns);
            if (!isGameOver)
                BurnAllPlayers();
            else
                yield break;
        }
    }

    public void BurnAllPlayers()
    {
        // Find all players (make sure they are tagged "Player")
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine) // Only apply on the local player's instance
            {
                // Call the RPC to apply the burn effect.
                // NegativeEffectType.Burn corresponds to the appropriate enum value.
                NegativeEffectManager.Instance.photonView.RPC("RPC_ApplyNegativeEffect", RpcTarget.All,
                    pv.ViewID, burnDamagePerTick, burnDuration, tickInterval, (int)NegativeEffectType.Burn);
            }
        }
    }
}
