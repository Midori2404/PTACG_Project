using Cinemachine;
using Photon.Pun.Demo.Cockpit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    AudioManager audioManager;
    public CinemachineVirtualCamera CurrentCamera;

    [Header("UI Panels")]
    public GameObject menuUIPanel;
    public GameObject insideRoomUIPanel;
    public GameObject settingsPanel;

    [Header("Cameras")]
    [SerializeField] private CinemachineVirtualCamera mainCamera;
    [SerializeField] private CinemachineVirtualCamera inRoomCamera;
    [SerializeField] private CinemachineVirtualCamera settingsCamera;

    //[SerializeField] private AudioClip[] soundClip;

    void Awake()
    {
        
    }

    void Start()
    {
        audioManager = AudioManager.instance;
        CurrentCamera.Priority++;
        //SfxManager.instance.playSoundSfxClip(soundClip, transform, 1f);
    }

    void Update()
    {
        if (insideRoomUIPanel.activeSelf)
        {
            UpdateCamera(inRoomCamera);
        }
        else if (settingsPanel.activeSelf)
        {
            UpdateCamera(settingsCamera);
        }
        else
        {
            UpdateCamera(mainCamera);
        }
    }

    public void UpdateCamera(CinemachineVirtualCamera camera)
    {
        CurrentCamera.Priority--;
        CurrentCamera = camera;
        CurrentCamera.Priority++;
    }

    public void ChangeScene()
    {
        Debug.Log("Pressed");
        SceneManager.LoadScene("Level 1");
		PlayerPrefs.SetInt("AttackDamage", 0);
        PlayerPrefs.SetInt("Defense", 0);
        PlayerPrefs.SetInt("Score", 0);
        PlayerPrefs.SetInt("ScoreCoins", 0);
        PlayerPrefs.SetInt("ScoreGems", 0);
        PlayerPrefs.SetInt("ScoreStars", 0);
        audioManager.PlayInstance("ButtonClick");
    }

    public void OpenSettings()
    {
        audioManager.PlayInstance("ButtonClick");
    }

    public void ExitPlay()
    {
        Debug.Log("Exit");
        Application.Quit();

        audioManager.PlayInstance("ButtonClick");
    }

    public void HoverSound()
    {
        audioManager.PlayInstance("HoverSound");
    }

    public void SelectSound()
    {
        audioManager.PlayInstance("ButtonClick");
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("MainMenu(BackupScene)");
    }

	public void Reset()
	{
		PlayerPrefs.DeleteAll();
	}
	public void Level1()
	{
		SceneManager.LoadScene("Level 1");
		Time.timeScale = 1;
		PlayerPrefs.SetInt("AttackDamage", 0);
        PlayerPrefs.SetInt("Defense", 0);
        PlayerPrefs.SetInt("Score", 0);
        PlayerPrefs.SetInt("ScoreCoins", 0);
        PlayerPrefs.SetInt("ScoreGems", 0);
        PlayerPrefs.SetInt("ScoreStars", 0);
        audioManager.PlayInstance("ButtonClick");
	}
	public void Level2()
	{
		SceneManager.LoadScene("Level 2");
		Time.timeScale = 1;
		PlayerPrefs.SetInt("AttackDamage", 0);
        PlayerPrefs.SetInt("Defense", 0);
        PlayerPrefs.SetInt("Score", 0);
        PlayerPrefs.SetInt("ScoreCoins", 0);
        PlayerPrefs.SetInt("ScoreGems", 0);
        PlayerPrefs.SetInt("ScoreStars", 0);
        audioManager.PlayInstance("ButtonClick");
	}
	public void Level3()
	{
		SceneManager.LoadScene("Level 3");
		Time.timeScale = 1;
		PlayerPrefs.SetInt("AttackDamage", 0);
        PlayerPrefs.SetInt("Defense", 0);
        PlayerPrefs.SetInt("Score", 0);
        PlayerPrefs.SetInt("ScoreCoins", 0);
        PlayerPrefs.SetInt("ScoreGems", 0);
        PlayerPrefs.SetInt("ScoreStars", 0);
        audioManager.PlayInstance("ButtonClick");
	}
	public void Level4()
	{
		SceneManager.LoadScene("Level 4");
		Time.timeScale = 1;
		PlayerPrefs.SetInt("AttackDamage", 0);
        PlayerPrefs.SetInt("Defense", 0);
        PlayerPrefs.SetInt("Score", 0);
        PlayerPrefs.SetInt("ScoreCoins", 0);
        PlayerPrefs.SetInt("ScoreGems", 0);
        PlayerPrefs.SetInt("ScoreStars", 0);
        audioManager.PlayInstance("ButtonClick");
	}
	public void Level5()
	{
		SceneManager.LoadScene("Level 5");
		Time.timeScale = 1;
		PlayerPrefs.SetInt("AttackDamage", 0);
        PlayerPrefs.SetInt("Defense", 0);
        PlayerPrefs.SetInt("Score", 0);
        PlayerPrefs.SetInt("ScoreCoins", 0);
        PlayerPrefs.SetInt("ScoreGems", 0);
        PlayerPrefs.SetInt("ScoreStars", 0);
        audioManager.PlayInstance("ButtonClick");
	}
	public void Level6()
	{
		SceneManager.LoadScene("Level 6");
		Time.timeScale = 1;
		PlayerPrefs.SetInt("AttackDamage", 0);
        PlayerPrefs.SetInt("Defense", 0);
        PlayerPrefs.SetInt("Score", 0);
        PlayerPrefs.SetInt("ScoreCoins", 0);
        PlayerPrefs.SetInt("ScoreGems", 0);
        PlayerPrefs.SetInt("ScoreStars", 0);
        audioManager.PlayInstance("ButtonClick");
	}
}
