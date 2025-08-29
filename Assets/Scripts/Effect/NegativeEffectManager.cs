using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;

public class NegativeEffectManager : MonoBehaviour
{
    public PhotonView photonView;
    private Dictionary<GameObject, ActiveEffect> activeEffects = new Dictionary<GameObject, ActiveEffect>(); // Updated dictionary type

    public List<NegativeEffect> negativeEffects;
    public static NegativeEffectManager Instance;

    public void Awake()
    {
        Instance = this;
        photonView = GetComponent<PhotonView>();
    }

    [PunRPC]
    public void RPC_ApplyNegativeEffect(int targetID, float damagePerTick, float duration, float tickInterval, int effectTypeInt)
    {
        GameObject target = PhotonView.Find(targetID)?.gameObject;
        if (target == null) return;

        // Only run this RPC on the owner of the target
        PhotonView targetPV = target.GetComponent<PhotonView>();
        if (targetPV != null && !targetPV.IsMine)
        {
            return;
        }

        NegativeEffectType effectType = (NegativeEffectType)effectTypeInt;

        // Prevent stacking if the target already has this effect
        if (activeEffects.ContainsKey(target) && activeEffects[target].effectType == effectType)
        {
            return; // Do nothing if the effect is already active
        }

        GameObject particleEffect = GetParticleEffect(effectType);
        if (particleEffect != null)
        {
            Coroutine effectCoroutine = StartCoroutine(HandleNegativeEffect(target, damagePerTick, duration, tickInterval, particleEffect, effectType));
            activeEffects[target] = new ActiveEffect(effectCoroutine, effectType); // Correct storage
        }
    }

    private GameObject GetParticleEffect(NegativeEffectType effectType)
    {
        foreach (var effect in negativeEffects)
        {
            if (effect.negativeEffectType == effectType)
            {
                return effect.particleEffectPrefab;
            }
        }
        return null;
    }

    private IEnumerator HandleNegativeEffect(GameObject target, float damagePerTick, float duration, float tickInterval, GameObject particleEffect, NegativeEffectType effectType)
    {
        if (target == null) yield break;

        string effectPath = "_Particles_Auras/" + particleEffect.name;
        GameObject effectInstance = PhotonNetwork.Instantiate(effectPath, target.transform.position + new Vector3(0, 1f, 0), Quaternion.identity);

        // Wait one frame to ensure PhotonView initialization
        yield return null;

        PhotonView targetPV = target.GetComponent<PhotonView>();
        if (targetPV != null)
        {
            EffectParentSync eps = effectInstance.GetComponent<EffectParentSync>();
            if (eps != null)
            {
                // Pass the target's local scale so it syncs across clients.
                eps.photonView.RPC("RPC_SetParent", RpcTarget.AllBuffered, targetPV.ViewID, target.transform.localScale);
            }
            else
            {
                Debug.LogWarning("Effect instance missing EffectParentSync component");
            }
        }

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            if (target != null)
            {
                target.TryGetComponent(out PhotonView tPV);
                if (tPV != null)
                {
                    tPV.RPC("TakeDamage", RpcTarget.All, damagePerTick);
                }
            }
            else
            {
                yield break;
            }
            elapsedTime += tickInterval;
            yield return new WaitForSeconds(tickInterval);
        }

        if (activeEffects.ContainsKey(target) && activeEffects[target].effectType == effectType)
        {
            activeEffects.Remove(target);
        }

        if (effectInstance != null)
        {
            PhotonNetwork.Destroy(effectInstance);
        }
    }


    private class ActiveEffect
    {
        public Coroutine effectCoroutine;
        public NegativeEffectType effectType;

        public ActiveEffect(Coroutine coroutine, NegativeEffectType type)
        {
            effectCoroutine = coroutine;
            effectType = type;
        }
    }


    // New public function to apply slow effect via RPC
    public void ApplySlowEffect(GameObject target, float slowMultiplier, float duration)
    {
        PhotonView targetPV = target.GetComponent<PhotonView>();
        if (targetPV != null)
        {
            photonView.RPC("RPC_ApplySlowEffect", RpcTarget.All, targetPV.ViewID, slowMultiplier, duration);
        }
    }

    [PunRPC]
    public void RPC_ApplySlowEffect(int targetID, float slowMultiplier, float duration)
    {
        GameObject target = PhotonView.Find(targetID)?.gameObject;
        if (target == null) return;

        // Instantiate the slow particle effect from Resources/_Particles_Auras/SlowEffect
        string slowEffectPath = "_Particles_Auras/SlowEffect"; // Make sure the prefab is placed here
        GameObject slowEffectInstance = PhotonNetwork.Instantiate(slowEffectPath, target.transform.position, Quaternion.identity);

        // Parent the slow effect to the target so it follows
        slowEffectInstance.transform.SetParent(target.transform, worldPositionStays: false);

        // Apply the slow effect by reducing the player's moveSpeed
        PlayerController playerController = target.GetComponent<PlayerController>();
        if (playerController != null)
        {
            float originalSpeed = playerController.moveSpeed;
            // Multiply current speed by slowMultiplier (e.g., 0.5 for 50% speed)
            playerController.moveSpeed = originalSpeed * slowMultiplier;

            // Start a coroutine to restore the original speed after the duration
            StartCoroutine(RemoveSlowEffect(target, originalSpeed, duration, slowEffectInstance));
        }
    }

    private IEnumerator RemoveSlowEffect(GameObject target, float originalSpeed, float duration, GameObject slowEffectInstance)
    {
        yield return new WaitForSeconds(duration);

        PlayerController playerController = target.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Restore the original move speed
            playerController.moveSpeed = originalSpeed;
        }

        // Destroy the slow particle effect (only if this client owns it)
        if (slowEffectInstance != null && slowEffectInstance.GetComponent<PhotonView>().IsMine)
        {
            PhotonNetwork.Destroy(slowEffectInstance);
        }
    }

}

[System.Serializable]
public class NegativeEffect
{
    public NegativeEffectType negativeEffectType;
    public GameObject particleEffectPrefab;
}

public enum NegativeEffectType
{
    None,
    Bleed,
    Poison,
    Burn,
    Paralyze
}
