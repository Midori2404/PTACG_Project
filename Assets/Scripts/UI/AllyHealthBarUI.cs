using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AllyHealthBarUI : MonoBehaviour
{
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TMP_Text allyNameText;

    /// <summary>
    /// Call this to initialize the ally health bar with the ally’s name and starting health.
    /// </summary>
    /// <param name="allyName">The display name of the ally.</param>
    /// <param name="maxHealth">The ally’s maximum health.</param>
    public void Initialize(string allyName, float maxHealth)
    {
        allyNameText.text = allyName;
        UpdateHealth(maxHealth, maxHealth);
    }

    /// <summary>
    /// Updates the health bar fill.
    /// </summary>
    /// <param name="currentHealth">Current health value.</param>
    /// <param name="maxHealth">Maximum health value.</param>
    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        float fill = Mathf.Clamp01(currentHealth / maxHealth);
        healthFillImage.fillAmount = fill;
    }
}
