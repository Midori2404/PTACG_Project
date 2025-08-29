using UnityEngine;

public abstract class ISkillEffect : ScriptableObject
{
    public abstract void ApplyEffect(GameObject user, GameObject target);
}
