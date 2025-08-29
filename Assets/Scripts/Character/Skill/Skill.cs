using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Skill System/Skill")]
public class Skill : ScriptableObject
{
    public string skillName;
    public float cooldown;

    // List of effects that this skill applies
    public List<ISkillEffect> skillEffects = new List<ISkillEffect>();

    public void AddEffect(ISkillEffect effect)
    {
        skillEffects.Add(effect);
    }

    public void Activate(GameObject user, GameObject target)
    {
        foreach (ISkillEffect effect in skillEffects)
        {
            effect.ApplyEffect(user, target);
        }
    }
}
