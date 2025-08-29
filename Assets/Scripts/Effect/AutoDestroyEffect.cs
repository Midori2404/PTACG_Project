using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

public class AutoDestroyEffect : MonoBehaviour
{
    [Tooltip("Duration for the effect before self-destruction.")]
    public float effectDuration = 1.4f;

    void Start()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.stopAction = ParticleSystemStopAction.Destroy;
            ps.Play();
        }
        // This destroys the object locally on each client.
        StartCoroutine(DestroyDelay(effectDuration));
    }

    IEnumerator DestroyDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject, 3f);
    }
}
