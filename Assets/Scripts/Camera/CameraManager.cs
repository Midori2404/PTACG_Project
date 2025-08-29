using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Cinemachine;
using UnityEngine.SceneManagement;

public class CameraManager : MonoBehaviourPunCallbacks
{
    [Header("Cameras")]
    public Camera mainCamera;
    public CinemachineVirtualCamera virtualCamera;
        
    public PhotonView photonView;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    private void Start()
    {

        if (photonView.IsMine)
        {
            EnableCamera();
        }
        else
        {
            DisableCamera();
        }
    }

    private void EnableCamera()
    {
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(true);
        }

        if (virtualCamera != null)
        {
            virtualCamera.enabled = true;
        }
    }

    private void DisableCamera()
    {
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(false);
        }

        if (virtualCamera != null)
        {
            virtualCamera.enabled = false;
        }
    }

     public void BackToMainMenu()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        // Load the Main Menu scene.
        SceneManager.LoadScene("MainMenu");
    }
}