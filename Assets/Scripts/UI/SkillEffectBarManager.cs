using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillEffectBarManager : MonoBehaviour
{
    // Containers for different skill types.
    [SerializeField] private Transform dashContainer;
    [SerializeField] private Transform primaryContainer;
    [SerializeField] private Transform secondaryContainer;

    // Buff bar prefab reference.
    [SerializeField] private GameObject buffBarPrefab;

    // Active buff bars keyed by buffId.
    private Dictionary<string, SkillEffectBar> activeBuffBars = new Dictionary<string, SkillEffectBar>();

    public void ApplyBuff(string buffId, float duration, PlayerSkillUpgradeType skillType)
    {
        // Determine the right container based on the skill type.
        Transform container = GetContainerForSkillType(skillType);
        if (activeBuffBars.ContainsKey(buffId))
        {
            // Refresh the buff if already active.
            activeBuffBars[buffId].gameObject.SetActive(true);
            activeBuffBars[buffId].ActivateBuff(duration);
        }
        else
        {
            // Instantiate the buff bar prefab as a child of the correct container.
            GameObject newBuffBar = Instantiate(buffBarPrefab, container);
            SkillEffectBar controller = newBuffBar.GetComponent<SkillEffectBar>();
            controller.ActivateBuff(duration);
            activeBuffBars.Add(buffId, controller);
        }
    }

    private Transform GetContainerForSkillType(PlayerSkillUpgradeType skillType)
    {
        switch (skillType)
        {
            case PlayerSkillUpgradeType.Dash:
                return dashContainer;
            case PlayerSkillUpgradeType.PrimarySkill:
                return primaryContainer;
            case PlayerSkillUpgradeType.SecondarySkill:
                return secondaryContainer;
            default:
                Debug.LogWarning("Invalid skill type!");
                return transform; // Fallback to own transform.
        }
    }

    public void RemoveBuff(string buffId)
    {
        if (activeBuffBars.ContainsKey(buffId))
        {
            Destroy(activeBuffBars[buffId].gameObject);
            activeBuffBars.Remove(buffId);
        }
    }
}
