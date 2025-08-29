using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyAttribute))]
public class EnemyAttributeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EnemyAttribute enemyAttribute = (EnemyAttribute)target;

        // Draw default fields
        enemyAttribute.enemyType = (EnemyType)EditorGUILayout.EnumPopup("Enemy Type", enemyAttribute.enemyType);
        enemyAttribute.enemyPrefab = (GameObject)EditorGUILayout.ObjectField("Enemy Prefab", enemyAttribute.enemyPrefab, typeof(GameObject), false);
        enemyAttribute.maxHealth = EditorGUILayout.FloatField("Max Health", enemyAttribute.maxHealth);
        enemyAttribute.movementSpeed = EditorGUILayout.FloatField("Movement Speed", enemyAttribute.movementSpeed);
        enemyAttribute.attackDamage = EditorGUILayout.FloatField("Attack Damage", enemyAttribute.attackDamage);
        enemyAttribute.attackRate = EditorGUILayout.FloatField("Attack Rate", enemyAttribute.attackRate);
        enemyAttribute.attackRange = EditorGUILayout.FloatField("Attack Range", enemyAttribute.attackRange);

        // Show projectilePrefab only for Ranged type
        if (enemyAttribute.enemyType == EnemyType.Ranged)
        {
            enemyAttribute.projectilePrefab = (GameObject)EditorGUILayout.ObjectField("Projectile Prefab", enemyAttribute.projectilePrefab, typeof(GameObject), false);
        }

        // Apply changes
        if (GUI.changed)
        {
            EditorUtility.SetDirty(enemyAttribute);
        }
    }
}