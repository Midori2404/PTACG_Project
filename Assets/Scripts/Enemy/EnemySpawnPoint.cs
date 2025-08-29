using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [SerializeField] private float spawnRadius = 5f; // Default radius for visualization
    [SerializeField] private Color gizmoColor = Color.red; // Gizmo color for customization

    private void OnValidate()
    {
        // Clamp the spawn radius to a minimum of 0
        spawnRadius = Mathf.Max(0, spawnRadius);
    }

    // Draws a wire sphere in the Scene view to visualize the spawn radius
    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor; // Set the gizmo color
        Gizmos.DrawWireSphere(transform.position, spawnRadius); // Draw the wire sphere
    }

    public float GetSpawnRadius()
    {
        return spawnRadius;
    }

    public Vector3 GetRandomSpawnPosition()
    {
        Vector3 randomizer = Random.insideUnitSphere * spawnRadius + transform.position;
        randomizer.y = 0;
        return randomizer;
    }
}

