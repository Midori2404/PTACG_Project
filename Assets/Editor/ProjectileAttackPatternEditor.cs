using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProjectileAttackPattern))]
public class ProjectileAttackPatternEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ProjectileAttackPattern attackPattern = (ProjectileAttackPattern)target;

        // Draw the default fields for shared variables
        attackPattern.projectilePrefab = (GameObject)EditorGUILayout.ObjectField("Projectile Prefab", attackPattern.projectilePrefab, typeof(GameObject), false);
        attackPattern.projectileSpeed = EditorGUILayout.FloatField("Projectile Speed", attackPattern.projectileSpeed);

        // Dropdown for attack type
        attackPattern.attackType = (ProjectileAttackType)EditorGUILayout.EnumPopup("Attack Type", attackPattern.attackType);

        // Show relevant fields based on the selected attack type
        switch (attackPattern.attackType)
        {
            case ProjectileAttackType.Circular:
                attackPattern.circularProjectilesPerWave = EditorGUILayout.IntField("Projectiles Per Wave", attackPattern.circularProjectilesPerWave);
                break;

            case ProjectileAttackType.Spiral:
                attackPattern.spiralRotationSpeed = EditorGUILayout.FloatField("Rotation Speed", attackPattern.spiralRotationSpeed);
                attackPattern.spiralProjectilesPerWave = EditorGUILayout.IntField("Projectiles Per Wave", attackPattern.spiralProjectilesPerWave);
                break;

            case ProjectileAttackType.Wave:
                attackPattern.waveSpreadAngle = EditorGUILayout.FloatField("Spread Angle", attackPattern.waveSpreadAngle);
                attackPattern.waveProjectilesPerWave = EditorGUILayout.IntField("Projectiles Per Wave", attackPattern.waveProjectilesPerWave);
                break;

            case ProjectileAttackType.Random:
                attackPattern.randomProjectilesPerWave = EditorGUILayout.IntField("Projectiles Per Wave", attackPattern.randomProjectilesPerWave);
                break;
        }

        // Save changes
        if (GUI.changed)
        {
            EditorUtility.SetDirty(attackPattern);
        }
    }
}
