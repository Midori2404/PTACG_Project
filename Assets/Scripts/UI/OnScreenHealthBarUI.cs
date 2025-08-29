using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class OnScreenHealthBarUI : MonoBehaviour
{
    [Header("Health Bar Components")]
    [Tooltip("Image used as the main health bar fill.")]
    [SerializeField] private Image mainFillImage;
    [Tooltip("Image used as the delayed damage indicator.")]
    [SerializeField] private Image delayedFillImage;

    [Header("Shield Bar Component")]
    [Tooltip("Image used as the temporary shield bar fill (should be layered above the main fill).")]
    [SerializeField] private Image temporaryShieldFillImage;
    [Tooltip("Time to smoothly animate the temporary shield fill.")]
    [SerializeField] private float shieldFillSmoothTime = 0.5f;

    [Header("Animation Settings")]
    [Tooltip("Time to smoothly animate the main fill.")]
    [SerializeField] private float mainFillSmoothTime = 0.5f;
    [Tooltip("Delay before the delayed indicator starts updating.")]
    [SerializeField] private float delayedFillDelay = 0.3f;
    [Tooltip("Time to smoothly animate the delayed fill.")]
    [SerializeField] private float delayedFillSmoothTime = 0.5f;

    [Header("Color Gradient")]
    [Tooltip("Gradient that defines the health bar color (full health = left, low health = right).")]
    [SerializeField] private Gradient healthGradient;

    [Header("Shake Settings")]
    [Tooltip("RectTransform of the UI element that will shake.")]
    [SerializeField] private RectTransform healthBarContainer;
    [Tooltip("Duration of the shake effect.")]
    [SerializeField] private float shakeDuration = 0.3f;
    [Tooltip("Magnitude of the shake effect.")]
    [SerializeField] private float shakeMagnitude = 10f;

    private Coroutine mainFillCoroutine;
    private Coroutine delayedFillCoroutine;
    private Coroutine shieldFillCoroutine;
    private Vector3 originalPosition;

    private void Awake()
    {
        // If no container is assigned, use the current GameObject's RectTransform.
        if (healthBarContainer == null)
        {
            healthBarContainer = GetComponent<RectTransform>();
        }
        if (healthBarContainer != null)
        {
            originalPosition = healthBarContainer.anchoredPosition;
        }
    }

    void Update()
    {
        // For testing purposes you can press Alpha0 or Alpha9 to update health and shield values.
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            UpdateHealthBar(20f, 100f);
            UpdateShieldBar(10f, 20f); // Test: current shield 10, max shield 20.
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            UpdateHealthBar(100f, 100f);
            UpdateShieldBar(0f, 20f);
        }
    }

    /// <summary>
    /// Updates the main health bar based on current and maximum health.
    /// </summary>
    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        float targetFill = Mathf.Clamp01(currentHealth / maxHealth);

        // Trigger a shake effect if the health is decreasing.
        if (mainFillImage.fillAmount > targetFill)
        {
            StartCoroutine(ShakeUI());
        }

        // Stop any running animations.
        if (mainFillCoroutine != null)
        {
            StopCoroutine(mainFillCoroutine);
        }
        if (delayedFillCoroutine != null)
        {
            StopCoroutine(delayedFillCoroutine);
        }

        // Animate the main fill and delayed fill.
        mainFillCoroutine = StartCoroutine(AnimateFill(mainFillImage, targetFill, mainFillSmoothTime, true));
        delayedFillCoroutine = StartCoroutine(DelayedFillUpdate(targetFill));
    }

    /// <summary>
    /// Updates the temporary shield bar based on current and maximum shield.
    /// </summary>
    public void UpdateShieldBar(float currentShield, float maxShield)
    {
        if (temporaryShieldFillImage == null)
            return;

        float targetFill = Mathf.Clamp01(currentShield / maxShield);

        if (shieldFillCoroutine != null)
        {
            StopCoroutine(shieldFillCoroutine);
        }
        shieldFillCoroutine = StartCoroutine(AnimateFill(temporaryShieldFillImage, targetFill, shieldFillSmoothTime, false));
    }

    // Smoothly animates an Image's fill amount.
    private IEnumerator AnimateFill(Image image, float targetFill, float duration, bool updateColor)
    {
        float startFill = image.fillAmount;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            image.fillAmount = Mathf.Lerp(startFill, targetFill, t);

            if (updateColor && healthGradient != null)
            {
                image.color = healthGradient.Evaluate(image.fillAmount);
            }
            yield return null;
        }
        image.fillAmount = targetFill;
        if (updateColor && healthGradient != null)
        {
            image.color = healthGradient.Evaluate(image.fillAmount);
        }
    }

    // Delays and then smoothly updates the delayed damage indicator.
    private IEnumerator DelayedFillUpdate(float targetFill)
    {
        yield return new WaitForSeconds(delayedFillDelay);
        yield return AnimateFill(delayedFillImage, targetFill, delayedFillSmoothTime, false);
    }

    // Shakes the health bar container to provide visual feedback when taking damage.
    private IEnumerator ShakeUI()
    {
        if (healthBarContainer == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            Vector2 randomOffset = Random.insideUnitCircle * shakeMagnitude;
            healthBarContainer.anchoredPosition = originalPosition + (Vector3)randomOffset;
            yield return null;
        }
        healthBarContainer.anchoredPosition = originalPosition;
    }
}
