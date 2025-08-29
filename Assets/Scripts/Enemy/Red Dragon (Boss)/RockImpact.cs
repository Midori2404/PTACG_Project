using Cinemachine;
using UnityEngine;

public class RockImpact : MonoBehaviour
{
    public float damageRadius = 2f; // Radius of damage
    public float damage = 50; // Damage dealt
    public GameObject impactEffectPrefab; // Assign your particle effect prefab in the inspector
    public GameObject groundCrackEffectPrefab;
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Collideable"))
        {
            // Spawn the particle effect at the point of impact
            if (impactEffectPrefab != null)
            {
                // Create an explosion effect
                GameObject particleEffect = Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);

                // Create an ground crack
                Instantiate(groundCrackEffectPrefab, transform.position, Quaternion.identity);

                // Shake Camera
                // CameraShaker.Instance.CameraShake(GetComponent<CinemachineImpulseSource>(), 0.1f);

                // Destroy the GameObject after the particle system finishes
                ParticleSystem ps = particleEffect.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    Destroy(gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
                }
            }

            // Deal damage to nearby objects
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, damageRadius);

            foreach (Collider hit in hitColliders)
            {
                // Check if the object has a health component
                if (hit.gameObject.tag == "Player")
                {
                    IDamageable player = hit.GetComponent<IDamageable>();
                    player?.TakeDamage(damage);
                }
                
            }

            // Destroy the rock after impact
            Destroy(gameObject);
        }
    }
}
