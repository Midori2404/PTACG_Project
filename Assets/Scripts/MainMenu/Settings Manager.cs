using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Audio;
using UnityEngine.UI;


public class SettingsManager : MonoBehaviour
{
    [SerializeField] private AudioMixer _audiomixer;
    public TMPro.TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown GraphicSet;
    public Slider Master;
    public Slider Music;
    public Slider SFX;
    //public GameObject? UI;


    Resolution[] resolutions;

    private void Awake()
    {
       /*if (UI != null)
       {
           DontDestroyOnLoad(UI);
       }*/
    }

    void Start()
    {
        //Graphic
        GraphicSet.value = PlayerPrefs.GetInt("quality level", 4);


        //Resolution
        Debug.Log(Screen.currentResolution);

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        int currentResIndex = 0;
        List<string> Options = new List<string>();
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height + " + " + resolutions[i].refreshRateRatio + "hz";
            Options.Add(option);

            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
            {
                currentResIndex = i;
            }
        }

        resolutionDropdown.AddOptions(Options);
        resolutionDropdown.value = currentResIndex;
        resolutionDropdown.RefreshShownValue();

        //Volume Slider
        if (PlayerPrefs.HasKey("MasterVolume"))
        {
            float masterVolume = PlayerPrefs.GetFloat("MasterVolume");
            Master.value = masterVolume;
            MasterVolume(masterVolume);
        }
        else
        {
            Master.value = 1f; // Default value
        }

        if (PlayerPrefs.HasKey("MusicVolume"))
        {
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume");
            Music.value = musicVolume;
            MusicVolume(musicVolume);
        }
        else
        {
            Music.value = 1f; // Default value
        }

        if (PlayerPrefs.HasKey("SFXVolume"))
        {
            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume");
            SFX.value = sfxVolume;
            SfXVolume(sfxVolume);
        }
        else
        {
            SFX.value = 1f; // Default value
        }
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void GraphicsSet(int QualityIndex)
    {
        Debug.Log(QualityIndex.ToString());
        QualitySettings.SetQualityLevel(QualityIndex, false);
    }


    //Audio Mixer
    public void MasterVolume(float volume)
    {
        _audiomixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20f);
        PlayerPrefs.SetFloat("MasterVolume", volume);
        //PlayerPrefs.Save();
    }

    public void MusicVolume(float volume)
    {
        _audiomixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20f);
        PlayerPrefs.SetFloat("MusicVolume", volume);
        //PlayerPrefs.Save();
    }

    public void SfXVolume(float volume)
    {
        _audiomixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20f);
        PlayerPrefs.SetFloat("SFXVolume", volume);
       // PlayerPrefs.Save();
    }
}


