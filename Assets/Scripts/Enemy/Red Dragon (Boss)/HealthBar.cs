using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider slider;
    private float bossMaxHealth;

    public void SetMaxHealth(float maxHealth)
    {
        bossMaxHealth = maxHealth;
        slider.maxValue = 1f;
        slider.value = 1f;
    }

    public void SetHealth(float health)
    {
        slider.value = health/bossMaxHealth;
    }
}
