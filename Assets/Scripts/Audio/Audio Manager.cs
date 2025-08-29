using UnityEngine.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    [SerializeField] public Audio[] MusicSounds, SFXSounds;
    public static AudioManager instance;
    public Audio[][] allAudioArrays;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        allAudioArrays = new Audio[][] { MusicSounds, SFXSounds };

        for (int i = 0; i < allAudioArrays.Length; i++)
        {
            foreach (Audio audio in allAudioArrays[i])
            {
                GameObject play = new GameObject("Sound: " + i + " " + audio.Clipname);
                play.transform.SetParent(this.transform);
                audio.SetSource(play.AddComponent<AudioSource>());
            }
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
        Scene currentScene = SceneManager.GetActiveScene();
        PlayMusicForScene(currentScene.name);
    }
        


    public void PlayInstance(string name)
    {
        foreach (Audio[] audioArray in allAudioArrays)
        {
            foreach (Audio audio in audioArray)
            {
                if (audio.Clipname == name)
                {
                    audio.Play();
                    return;
                }
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        
        PlayMusicForScene(scene.name);
    }

    public void PlayMusicForScene(string sceneName)
    {
        StopAllAudio();
        foreach (Audio[] audioArray in allAudioArrays)
        {
            foreach (Audio audio in audioArray)
            {
                if (audio.clip && sceneName == audio.Clipname)
                {
                    audio.Play();
                    return;
                }
            }
        }
    }

    public void StopAllAudio()
    {
        foreach (Audio[] audioArray in allAudioArrays)
        {
            foreach (Audio audio in audioArray)
            {
                audio.Stop();
            }
        }
    }
}

