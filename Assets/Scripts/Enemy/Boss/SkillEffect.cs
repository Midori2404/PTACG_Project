using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

public class SkillEffect : MonoBehaviourPun
{
    public float skillDamage;
    public float explosionRadius;
    private ParticleSystem part;
    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    public void Initialize(float damage, float radius)
    {
        skillDamage = damage;
        explosionRadius = radius;
    }

    private void Start()
    {
        part = GetComponent<ParticleSystem>();
        
        // Ensure the meteor effect uses the same random seed on all clients.
        if (photonView != null && photonView.InstantiationData != null && photonView.InstantiationData.Length > 0)
        {
            int seed = (int)photonView.InstantiationData[0];
            var mainModule = part.main;
            part.useAutoRandomSeed = false;
            part.randomSeed = (uint)seed;
        }
        
        // Optionally, if your prefab is not set to Play On Awake, you can trigger it manually:
        part.Play();
    }

    private void OnParticleCollision(GameObject other)
    {
        int numCollisionEvents = part.GetCollisionEvents(other, collisionEvents);

        for (int i = 0; i < numCollisionEvents; i++)
        {
            Vector3 hitPoint = collisionEvents[i].intersection; // Get exact hit point

            Collider[] hitColliders = Physics.OverlapSphere(hitPoint, explosionRadius);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Player"))
                {
                    PlayerAttribute playerAttribute = hitCollider.GetComponent<PlayerAttribute>();
                    if (playerAttribute != null)
                    {
                        // Damage is applied via RPC so that all clients see the same effect.
                        playerAttribute.GetComponent<PhotonView>().RPC("TakeDamage", RpcTarget.All, skillDamage);
                    }
                }
            }

            // Optionally draw the explosion radius for debugging.
            // DrawExplosionRadius(hitPoint);
        }
    }

    private void DrawExplosionRadius(Vector3 position)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * explosionRadius * 2; // Adjust size based on radius
        sphere.GetComponent<Collider>().enabled = false;

        Renderer sphereRenderer = sphere.GetComponent<Renderer>();
        if (sphereRenderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(1, 0, 0, 0.5f); // Semi-transparent red
            sphereRenderer.material = mat;
        }

        Destroy(sphere, 1f); // Destroy after 1 second
    }
}
