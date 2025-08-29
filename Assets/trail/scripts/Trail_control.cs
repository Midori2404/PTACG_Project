using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Trail_control : MonoBehaviourPun
{
    private float disappear_speed;
    public SkinnedMeshRenderer skinned_mesh_renderer;
    private Mesh baked_mesh_result;
    private Material material;
    private float alpha;

    public void init(float disappear_speed, SkinnedMeshRenderer skinned_mesh_renderer, float alpha)
    {
        this.disappear_speed = disappear_speed;
        this.skinned_mesh_renderer = skinned_mesh_renderer;
        this.alpha = alpha;

        if (this.baked_mesh_result == null)
        {
            this.baked_mesh_result = new Mesh();
        }

        this.skinned_mesh_renderer.BakeMesh(this.baked_mesh_result);
        GetComponent<MeshFilter>().mesh = this.baked_mesh_result;

        this.material = GetComponent<MeshRenderer>().material;
    }

    void Update()
    {
        if (material != null)
        {
            alpha = Mathf.Lerp(alpha, 0, disappear_speed * Time.deltaTime);
            material.SetFloat("alpha", alpha);

            if (alpha < 0.01f)
            {
                gameObject.SetActive(false);
            }
        }
    }
}