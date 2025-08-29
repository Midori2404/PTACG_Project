using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SkillCooldownUI : MonoBehaviour
{
    [Header("HUD Skill Cooldown Images")]
    public Image dashSkillIconCooldown;
    public Image primarySkillIconCooldown;
    public Image secondarySkillIconCooldown;

    /// <summary>
    /// Starts the cooldown animation for the given skill type.
    /// </summary>
    /// <param name="skillType">The skill type (Dash, PrimarySkill, or SecondarySkill).</param>
    /// <param name="duration">The duration of the cooldown in seconds.</param>
    public void StartCooldown(PlayerSkillUpgradeType skillType, float duration)
    {
        switch (skillType)
        {
            case PlayerSkillUpgradeType.Dash:
                StartCoroutine(CooldownRoutine(dashSkillIconCooldown, duration));
                break;
            case PlayerSkillUpgradeType.PrimarySkill:
                StartCoroutine(CooldownRoutine(primarySkillIconCooldown, duration));
                break;
            case PlayerSkillUpgradeType.SecondarySkill:
                StartCoroutine(CooldownRoutine(secondarySkillIconCooldown, duration));
                break;
        }
    }

    private IEnumerator CooldownRoutine(Image cooldownImage, float duration)
    {
        if (cooldownImage == null)
            yield break;

        float elapsed = 0f;
        // Start with full fill.
        cooldownImage.fillAmount = 1f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Gradually reduce the fill amount from 1 to 0 over the duration.
            cooldownImage.fillAmount = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }
        cooldownImage.fillAmount = 0f;
    }
}
