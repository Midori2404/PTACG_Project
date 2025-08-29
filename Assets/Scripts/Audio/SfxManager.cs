using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class SfxManager : MonoBehaviour
{
    public static SfxManager instance;
    [SerializeField] private AudioSource SfxObject;
    [HideInInspector] public PhotonView photonView;
    //public Audio[][] allAudioArrays;

    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            PhotonNetwork.Destroy(gameObject);
        }

        photonView = GetComponent<PhotonView>();
    }



    public void PlaySoundFXClip(AudioClip audioClip, Transform spawnTransform, float volume = 1f)
    {
        //spawn object 
        AudioSource audioSource = Instantiate(SfxObject, spawnTransform.position, Quaternion.identity);

        //assign the audioclip
        audioSource.clip = audioClip;

        //assign volume
        audioSource.volume = volume;

        //play sound
        audioSource.Play();

        //get length of sound
        float clipLength = audioSource.clip.length;

        // destory the clip after playing'
        Destroy(audioSource.gameObject, clipLength);
    }

    [PunRPC]
    public void RPC_PlaySoundFXClip(string clipName, Vector3 position, float volume)
    {
        AudioClip clip = Resources.Load<AudioClip>("Audio/" + clipName); // Adjust path as needed
        if (clip == null)
        {
            Debug.LogWarning($"AudioClip '{clipName}' not found in Resources/Audio/");
            return;
        }

        AudioSource audioSource = Instantiate(SfxObject, position, Quaternion.identity);
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
        Destroy(audioSource.gameObject, clip.length);
    }



    public void PlayRandomSoundFXClip(AudioClip[] audioClip, Transform spawnTransform, float volume = 1f)
    {
        int rand = Random.Range(0, audioClip.Length);


        //spawn object 
        AudioSource audioSource = Instantiate(SfxObject, spawnTransform.position, Quaternion.identity);

        //assign the audioclip
        audioSource.clip = audioClip[rand];

        //assign volume
        audioSource.volume = volume;

        //play sound
        audioSource.Play();

        //get length of sound
        float clipLength = audioSource.clip.length;

        // destory the clip after playing'
        Destroy(audioSource.gameObject, clipLength);
    }

    [PunRPC]
    public void RPC_PlaySoundFXClipDelayed(string clipName, Vector3 position, float delay, float volume = 1f)
    {
        StartCoroutine(PlayDelayedSoundRoutine(clipName, position, delay, volume));
    }

    private IEnumerator PlayDelayedSoundRoutine(string clipName, Vector3 position, float delay, float volume)
    {
        yield return new WaitForSeconds(delay);

        AudioClip clip = Resources.Load<AudioClip>("Audio/" + clipName); // Ensure clip is in Resources/Audio/
        if (clip == null)
        {
            Debug.LogWarning($"AudioClip '{clipName}' not found in Resources/Audio/");
            yield break;
        }

        AudioSource audioSource = Instantiate(SfxObject, position, Quaternion.identity);
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
        Destroy(audioSource.gameObject, clip.length);
    }

}
