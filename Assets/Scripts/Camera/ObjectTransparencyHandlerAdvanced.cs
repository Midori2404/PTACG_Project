using System.Collections.Generic;
using UnityEngine;

public class ObjectTransparencyHandlerAdvanced : MonoBehaviour
{
    [Tooltip("Transform of the player object")]
    public Transform playerTransform;

    [Tooltip("Desired transparency level (0 = fully transparent, 1 = fully opaque)")]
    [Range(0f, 1f)]
    public float transparentAlpha = 0.3f;

    [Tooltip("Radius for sphere cast (helps capture wider obstacles)")]
    public float sphereCastRadius = 0.5f;

    [Tooltip("Layer mask for obstacles to consider (exclude player, etc.)")]
    public LayerMask obstacleMask;

    // Stores original material arrays for each renderer.
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    // Stores cloned (instance) material arrays that are modified for transparency.
    private Dictionary<Renderer, Material[]> clonedMaterials = new Dictionary<Renderer, Material[]>();

    void Update()
    {
        // Calculate direction and distance from camera to player.
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        float distance = Vector3.Distance(playerTransform.position, transform.position);

        // Perform a sphere cast from the camera to the player.
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, sphereCastRadius, direction, distance, obstacleMask);
        HashSet<Renderer> hitRenderers = new HashSet<Renderer>();

        // Process all hits.
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            Renderer rend = hit.collider.GetComponent<Renderer>();
            // Ignore the player object.
            if (rend != null && hit.collider.gameObject != playerTransform.gameObject)
            {
                hitRenderers.Add(rend);

                // If the renderer hasn't been processed yet, clone its materials.
                if (!clonedMaterials.ContainsKey(rend))
                {
                    // Store the original materials.
                    originalMaterials[rend] = rend.materials;

                    Material[] clones = new Material[rend.materials.Length];
                    for (int i = 0; i < rend.materials.Length; i++)
                    {
                        clones[i] = new Material(rend.materials[i]);
                        // Set the cloned material to transparent mode.
                        SetMaterialTransparent(clones[i]);
                        // Apply the desired transparency.
                        Color col = clones[i].color;
                        col.a = transparentAlpha;
                        clones[i].color = col;
                    }
                    clonedMaterials[rend] = clones;
                    rend.materials = clones;
                }
                else
                {
                    // If already processed, update the transparency if needed.
                    Material[] clones = clonedMaterials[rend];
                    for (int i = 0; i < clones.Length; i++)
                    {
                        Color col = clones[i].color;
                        col.a = transparentAlpha;
                        clones[i].color = col;
                    }
                }
            }
        }

        // For objects no longer obstructing the view, revert to the original materials.
        List<Renderer> keys = new List<Renderer>(clonedMaterials.Keys);
        foreach (Renderer rend in keys)
        {
            if (!hitRenderers.Contains(rend))
            {
                // Reassign the original materials.
                if (originalMaterials.TryGetValue(rend, out Material[] origMats))
                {
                    rend.materials = origMats;
                }
                // Clean up the cloned materials to avoid memory leaks.
                foreach (Material mat in clonedMaterials[rend])
                {
                    Destroy(mat);
                }
                clonedMaterials.Remove(rend);
                originalMaterials.Remove(rend);
            }
        }
    }

    // Helper function to set a material to transparent mode.
    void SetMaterialTransparent(Material mat)
    {
        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 2); // 2 corresponds to Fade mode in Standard Shader.
        }
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    // Helper function to revert a material to opaque mode.
    void SetMaterialOpaque(Material mat)
    {
        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 0); // 0 corresponds to Opaque mode.
        }
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt("_ZWrite", 1);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = -1;
    }
}
