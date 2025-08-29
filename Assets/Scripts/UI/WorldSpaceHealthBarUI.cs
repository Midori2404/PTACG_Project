using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class WorldSpaceHealthBarUI : MonoBehaviour, IPunObservable
{
    [Header("Health Bar Components")]
    [Tooltip("Image used as the main health bar fill.")]
    [SerializeField] private Image mainFillImage;
    [Tooltip("Image used as the delayed damage indicator.")]
    [SerializeField] private Image delayedFillImage;

    [Header("Animation Settings")]
    [Tooltip("Time to smoothly animate the main fill.")]
    [SerializeField] private float mainFillSmoothTime = 0.5f;
    [Tooltip("Delay before the delayed indicator starts updating.")]
    [SerializeField] private float delayedFillDelay = 0.3f;
    [Tooltip("Time to smoothly animate the delayed fill.")]
    [SerializeField] private float delayedFillSmoothTime = 0.5f;

    [Header("Color Gradient")]
    [Tooltip("Gradient that defines the health bar color (full health = green, low health = red).")]
    [SerializeField] private Gradient healthGradient;

    // These coroutines handle the smooth animations.
    private Coroutine mainFillCoroutine;
    private Coroutine delayedFillCoroutine;

    // Health values to be synchronized.
    public float currentHealth = 100f;
    public float maxHealth = 100f;

    /// <summary>
    /// Sets the health values locally and updates the UI.
    /// Only the local player should call this method.
    /// </summary>
    /// <param name="current">The current health value.</param>
    /// <param name="max">The maximum health value.</param>
    public void SetHealth(float current, float max)
    {
        currentHealth = current;
        maxHealth = max;
        UpdateHealthBar(currentHealth, maxHealth);
    }

    /// <summary>
    /// Updates the health bar UI using smooth transitions and a delayed damage indicator.
    /// </summary>
    /// <param name="current">Current health value.</param>
    /// <param name="max">Maximum health value.</param>
    public void UpdateHealthBar(float current, float max)
    {
        float targetFill = Mathf.Clamp01(current / max);

        // Stop any running animations.
        if (mainFillCoroutine != null)
        {
            StopCoroutine(mainFillCoroutine);
        }
        if (delayedFillCoroutine != null)
        {
            StopCoroutine(delayedFillCoroutine);
        }

        // Animate the main fill (with color gradient update).
        mainFillCoroutine = StartCoroutine(AnimateFill(mainFillImage, targetFill, mainFillSmoothTime, true));

        // Animate the delayed fill after a short delay.
        delayedFillCoroutine = StartCoroutine(DelayedFillUpdate(targetFill));
    }

    // Smoothly interpolates the fill amount and optionally updates the color.
    private IEnumerator AnimateFill(Image image, float targetFill, float duration, bool updateColor)
    {
        // Prevent division by zero.
        if (duration <= 0f)
        {
            image.fillAmount = targetFill;
            if (updateColor && healthGradient != null)
            {
                image.color = healthGradient.Evaluate(image.fillAmount);
            }
            yield break;
        }

        float startFill = image.fillAmount;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
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


    // Delays the update of the delayed damage indicator.
    private IEnumerator DelayedFillUpdate(float targetFill)
    {
        yield return new WaitForSeconds(delayedFillDelay);
        yield return AnimateFill(delayedFillImage, targetFill, delayedFillSmoothTime, false);
    }

    /// <summary>
    /// Photon callback to sync health values across the network.
    /// The local owner writes its health, and remote players receive it.
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Only the local owner sends its health.
            stream.SendNext(currentHealth);
            stream.SendNext(maxHealth);
        }
        else
        {
            // Remote clients receive the health values and update the UI.
            currentHealth = (float)stream.ReceiveNext();
            maxHealth = (float)stream.ReceiveNext();
            UpdateHealthBar(currentHealth, maxHealth);
        }
    }
}
