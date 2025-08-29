using UnityEngine;
using System.Linq;
using Photon.Pun;
using Unity.Mathematics;
using System.Collections;
using System.Collections.Generic;

public class Projectile : MonoBehaviour
{
    // Folder Path
    public const string PROJECTILE_FLASHES = "_Particles_Flashes";
    public const string PROJECTILES_HIT_IMPACT = "_Particles_Hit_Effects";
    private PhotonView photonView;


    [SerializeField] private float damage;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private GameObject[] flashEffect;
    [SerializeField] private GameObject[] hitImpactEffect;
    [SerializeField] private GameObject owner; // The shooter (player or enemy)
    [SerializeField] private bool isExplosive = false; // Determines if projectile causes an explosion
    [SerializeField] private float explosionRadius = 3f;

    private Vector3 direction;
    private NegativeEffectType currentEffectType;
    private float damagePerTick = 0f;
    private float dotDuration = 0f;
    private float tickInterval = 0f;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    void Start()
    {
        // Check if instantiated via PhotonNetwork and read the parameters
        if (PhotonView.Get(this).InstantiationData != null)
        {
            object[] data = PhotonView.Get(this).InstantiationData;
            Vector3 shootDirection = (Vector3)data[0];
            float projectileDamage = (float)data[1];
            NegativeEffectType effectType = (NegativeEffectType)(int)data[2];
            // Retrieve the shooter via its PhotonView ID (passed as data[3])
            int shooterViewID = (int)data[3];
            GameObject shooter = PhotonView.Find(shooterViewID)?.gameObject;
            float damagePerTick = (float)data[4];
            float dotDuration = (float)data[5];
            float tickInterval = (float)data[6];

            // Pass the shooter into the Initialize method
            Initialize(shootDirection, projectileDamage, effectType, shooter, damagePerTick, dotDuration, tickInterval);
        }

        // Instantiate flash effects...
        foreach (var effectPrefab in flashEffect)
        {
            if (effectPrefab != null)
            {
                GameObject effectInstance = Instantiate(effectPrefab, transform.position, Quaternion.identity);
                effectInstance.transform.forward = transform.forward;
                var flashPs = effectInstance.GetComponent<ParticleSystem>();
                if (flashPs != null)
                {
                    Destroy(effectInstance, flashPs.main.duration);
                }
                else
                {
                    var flashPsParts = effectInstance.transform.GetChild(0).GetComponent<ParticleSystem>();
                    Destroy(effectInstance, flashPsParts.main.duration);
                }
            }
        }

        Destroy(gameObject, lifetime);
    }


    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    public void Initialize(Vector3 shootDirection, float projectileDamage, NegativeEffectType effectType,
        GameObject shooter, float damagePerTick = 0f, float dotDuration = 0f, float tickInterval = 0f)
    {
        Debug.Log("Projectile Initialized");
        direction = shootDirection.normalized;
        damage = projectileDamage;
        currentEffectType = effectType;
        this.damagePerTick = damagePerTick;
        this.dotDuration = dotDuration;
        this.tickInterval = tickInterval;
        owner = shooter;

        transform.rotation = Quaternion.LookRotation(direction);

        IgnoreTeammateCollisions();
    }

    private void IgnoreTeammateCollisions()
    {
        if (owner == null) return;

        Collider projectileCollider = GetComponent<Collider>();
        Collider ownerCollider = owner.GetComponent<Collider>();

        if (projectileCollider != null && ownerCollider != null)
        {
            Physics.IgnoreCollision(projectileCollider, ownerCollider);
        }

        // Ignore collisions with all friendly objects.
        // This assumes friendly objects share the same tag as the owner (e.g., "Player" or "Enemy").
        string ownerTag = owner.tag;
        Collider[] friendlyColliders = GameObject.FindGameObjectsWithTag(ownerTag)
                                        .Select(obj => obj.GetComponent<Collider>())
                                        .Where(col => col != null && col != ownerCollider)
                                        .ToArray();

        foreach (Collider friendly in friendlyColliders)
        {
            Physics.IgnoreCollision(projectileCollider, friendly);
        }
    }

    private bool hasBeenDestroyed = false;

    private void OnCollisionEnter(Collision collision)
    {
        // Sync hit effect on collision via RPC.
        photonView.RPC("RPC_SpawnHitEffect", RpcTarget.All, transform.position, transform.rotation);

        // Skip friendly collisions.
        if (owner != null && collision.gameObject.CompareTag(owner.tag))
        {
            return;
        }

        if (isExplosive)
        {
            Explode();
        }
        else
        {
            DealDirectDamage(collision.gameObject);
        }

        // Only the owner should destroy the projectile, and only once.
        if (photonView.IsMine && !hasBeenDestroyed)
        {
            hasBeenDestroyed = true;
            try
            {
                PhotonNetwork.Destroy(gameObject);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Failed to destroy projectile: " + ex.Message);
            }
        }
    }



    [PunRPC]
    private void RPC_SpawnHitEffect(Vector3 position, Quaternion rotation)
    {
        SpawnHitEffect(position, rotation);
    }

    private void SpawnHitEffect(Vector3 position, Quaternion rotation)
    {
        if (hitImpactEffect != null && hitImpactEffect.Length > 0)
        {
            // Choose a hit effect from your array (randomly, for example)
            int randomIndex = UnityEngine.Random.Range(0, hitImpactEffect.Length);
            GameObject effectPrefab = hitImpactEffect[randomIndex];

            // Option 1: If the prefab is already assigned, instantiate directly:
            // Instantiate(effectPrefab, position, rotation);

            // Option 2: If you prefer to load from the Resources folder using your folder path:
            string prefabPath = PROJECTILES_HIT_IMPACT + "/" + effectPrefab.name;
            GameObject pe = Resources.Load<GameObject>(prefabPath);
            GameObject go = Instantiate(pe, position + new Vector3(0, 0.1f, 0), pe.transform.localRotation);

            Destroy(go, 4f);
        }
    }


    private void Explode()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        
        CameraShaker.Instance.ShakeCamera(CameraShaker.SHAKE_TYPE_EXPLOSION, 0.7f);

        // Use a HashSet to ensure we only process each damageable target once.
        HashSet<GameObject> damagedTargets = new HashSet<GameObject>();

        foreach (var collider in hitColliders)
        {
            // Get the damageable component from the parent.
            IDamageable damageable = collider.GetComponentInParent<IDamageable>();
            if (damageable == null)
                continue;

            // Cast to MonoBehaviour to access the gameObject.
            MonoBehaviour damageableMB = damageable as MonoBehaviour;
            if (damageableMB == null)
                continue;

            GameObject targetObj = damageableMB.gameObject;

            // Skip if this target is the owner, is not valid, or has already been processed.
            if (targetObj == owner || !IsValidTarget(targetObj) || damagedTargets.Contains(targetObj))
                continue;

            // Mark the target as processed.
            damagedTargets.Add(targetObj);

            PhotonView targetPV = targetObj.GetComponent<PhotonView>();
            if (targetPV != null)
            {
                targetPV.RPC("TakeDamage", RpcTarget.All, damage);
            }
            else
            {
                damageable.TakeDamage(damage);
            }

            // Uncomment if you want to apply negative effects from explosions:
            // if (currentEffectType != NegativeEffectType.None)
            // {
            //     NegativeEffectManager.Instance.ApplyNegativeEffect(targetObj, damagePerTick, dotDuration, tickInterval, currentEffectType);
            // }
        }
    }


    private void DealDirectDamage(GameObject target)
    {
        if (target == owner) return;

        IDamageable damageable = target.GetComponent<IDamageable>();
        PhotonView targetPV = target.GetComponent<PhotonView>();

        if (damageable != null && IsValidTarget(target))
        {
            PhotonView enemyPhotonView = target.GetComponent<PhotonView>();
            if (enemyPhotonView != null)
            {
                // This RPC method should be marked with [PunRPC] on the enemy script.
                enemyPhotonView.RPC("TakeDamage", RpcTarget.All, damage);
            }
            else
            {
                damageable.TakeDamage(damage);
            }

            if (currentEffectType != NegativeEffectType.None)
            {
                object[] effectData = new object[] { targetPV.ViewID, damagePerTick, dotDuration, tickInterval, (int)currentEffectType };
                NegativeEffectManager.Instance.photonView.RPC("RPC_ApplyNegativeEffect", RpcTarget.All, effectData);
            }
        }
    }

    private bool IsValidTarget(GameObject target)
    {
        // Ensure owner is assigned
        if (owner == null) return false;

        // Prevent projectile from damaging its owner
        if (target == owner) return false;

        // Get the tags of the owner and target
        string ownerTag = owner.tag;
        string targetTag = target.tag;

        // Only allow damage if the tags are different:
        // e.g., owner is "Player" and target is "Enemy", or vice versa.
        return ownerTag != targetTag;
    }

    public IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        PhotonNetwork.Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        if (isExplosive)
        {
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawSphere(transform.position, explosionRadius);
        }
    }
}
