using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Trail_manager : MonoBehaviourPun
{
    [Header("target SkinnedMeshRenderer")]
    public GameObject game_obj_target;

    private Vector3 v3_position_game_obj_target_before;

    [Header("how many trails")]
    public int trail_count;

    [Header("The initial transparency of the tail")]
    [Range(0f, 1f)]
    public float trail_alpha;

    [Header("The time between each tail, in seconds")]
    public float trail_interval_time;

    [Header("The speed at which each tail disappears")]
    public float trail_disappear_speed;

    [Header("Trail Color")]
    [ColorUsageAttribute(true, true, 0f, 8f, 0.125f, 3f)]
    public Color color_trail;

    private List<GameObject> trailObjects = new List<GameObject>();

    private void Awake()
    {
        if (!photonView.IsMine) return;

        if (trail_count > 0 && game_obj_target != null)
        {
            for (int i = 0; i < trail_count; i++)
            {
                GameObject trail = new GameObject("trail" + i);
                trail.transform.SetParent(transform);
                trail.AddComponent<MeshFilter>();
                trail.AddComponent<MeshRenderer>();
                trail.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                Material mat = new Material(Shader.Find("EasyGameStudio/trail"));
                mat.SetTexture("main_texture", game_obj_target.GetComponent<SkinnedMeshRenderer>().material.mainTexture);
                mat.SetColor("color_fresnel_emission", color_trail);
                trail.GetComponent<MeshRenderer>().material = mat;

                Trail_control trail_control = trail.AddComponent<Trail_control>();
                trail.SetActive(false);

                trailObjects.Add(trail);
            }
        }
    }

    private void OnEnable()
    {
        if (!photonView.IsMine) return;
        if (trailObjects.Count > 0)
        {
            StartCoroutine(trail_start());
        }
    }

    private void OnDisable()
    {
        if (!photonView.IsMine) return;
        StopCoroutine(trail_start());

        foreach (var trail in trailObjects)
        {
            trail.SetActive(false);
        }
    }

    [PunRPC]
    private void ActivateTrail(int index, Vector3 position, Quaternion rotation)
    {
        if (index >= 0 && index < trailObjects.Count)
        {
            GameObject trail = trailObjects[index];
            trail.transform.position = position;
            trail.transform.rotation = rotation;

            if (!trail.activeSelf)
                trail.SetActive(true);

            trail.GetComponent<Trail_control>().init(trail_disappear_speed, game_obj_target.GetComponent<SkinnedMeshRenderer>(), trail_alpha);
        }
    }

    IEnumerator trail_start()
    {
        while (true)
        {
            for (int i = 0; i < trailObjects.Count; i++)
            {
                if (v3_position_game_obj_target_before != game_obj_target.transform.position)
                {
                    photonView.RPC("ActivateTrail", RpcTarget.All, i, game_obj_target.transform.position, game_obj_target.transform.rotation);
                }

                v3_position_game_obj_target_before = game_obj_target.transform.position;
                yield return new WaitForSeconds(trail_interval_time);
            }
        }
    }
}
