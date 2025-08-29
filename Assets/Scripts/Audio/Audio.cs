using UnityEngine.Audio;
using UnityEngine;


[System.Serializable]
public class Audio
{
    [HideInInspector] private AudioSource _source;
    
    public AudioMixerGroup audioMixer;
    public AudioClip clip;

    public string Clipname;
    public bool loop;
    public bool PlayOnAwake;

    [Range(.3f,1f)]
    public float volume;
    [Range (.1f,3f)]
    public float Pitch;

    public void SetSource(AudioSource Source)
    {
        _source = Source;
        _source.clip = clip;
        _source.volume = volume;
        _source.pitch = Pitch;
        _source.loop = loop;
        _source.playOnAwake = PlayOnAwake;
        _source.outputAudioMixerGroup = audioMixer;
    }

    public void Play()
    {
        if (_source != null)
        {
            _source.Play();
        }
    }

    public void Stop()
    {
        if (_source != null)
        {
            _source.Stop();
        }
    }
}
