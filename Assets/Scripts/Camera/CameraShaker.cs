using UnityEngine;
using Cinemachine;
using Photon.Pun;
using System.Collections.Generic;
using System.Collections;

public class CameraShaker : MonoBehaviourPunCallbacks
{
    public static string SHAKE_TYPE_RECOIL = "ImpulseSource_Recoil";
    public static string SHAKE_TYPE_RUMBLE = "ImpulseSource_Rumble";
    public static string SHAKE_TYPE_EXPLOSION = "ImpulseSource_Explosion";
    public static CameraShaker Instance { get; private set; }
    private Coroutine rumbleCoroutine;


    private Dictionary<string, CinemachineImpulseSource> impulseSources;

    private void Awake()
    {
        Instance = this;

        DontDestroyOnLoad(gameObject);

        impulseSources = new Dictionary<string, CinemachineImpulseSource>();

        // Automatically find and store impulse sources by name
        foreach (var source in GetComponentsInChildren<CinemachineImpulseSource>())
        {
            impulseSources[source.gameObject.name] = source;
        }
    }

    public void ShakeCamera(string type, float magnitude = 1f)
    {
        if (impulseSources.TryGetValue(type, out CinemachineImpulseSource source))
        {
            source.m_DefaultVelocity = Vector3.down * magnitude;
            source.GenerateImpulse();
        }
        else
        {
            Debug.LogWarning($"No impulse source found for type: {type}");
        }
    }

    /// <summary>
    /// Starts a continuous rumble using the specified source.
    /// </summary>
    public void StartRumble(string sourceName, float magnitude = 1f, float interval = 0.1f)
    {
        StopRumble(); // Ensure no overlap
        rumbleCoroutine = StartCoroutine(RumbleRoutine(sourceName, magnitude, interval));
    }

    /// <summary>
    /// Stops the rumble.
    /// </summary>
    public void StopRumble()
    {
        if (rumbleCoroutine != null)
        {
            StopCoroutine(rumbleCoroutine);
            rumbleCoroutine = null;
        }
    }

    private IEnumerator RumbleRoutine(string sourceName, float magnitude, float interval)
    {
        while (true)
        {
            ShakeCamera(sourceName, magnitude);
            yield return new WaitForSeconds(interval);
        }
    }
}
