using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class SkillEffectBar : MonoBehaviour
{
    // Reference to the UI Image acting as the buff bar (with Fill Method set to "Horizontal").
    [SerializeField] private Image buffBarImage;

    // The current active coroutine.
    private Coroutine buffCoroutine;

    // Call this method whenever the buff is applied or refreshed.
    public void ActivateBuff(float duration)
    {
        // If a buff is already active, stop it.
        if (buffCoroutine != null)
        {
            StopCoroutine(buffCoroutine);
        }

        // Reset the bar to full and show it.
        buffBarImage.fillAmount = 1f;

        // Start a coroutine to update the bar over the buff duration.
        buffCoroutine = StartCoroutine(UpdateBuffBar(duration));
    }

    private IEnumerator UpdateBuffBar(float duration)
    {
        float timeRemaining = duration;

        while (timeRemaining > 0f)
        {
            timeRemaining -= Time.deltaTime;
            buffBarImage.fillAmount = timeRemaining / duration;
            yield return null;
        }

        // Hide the buff bar when finished.
        gameObject.SetActive(false);
        buffCoroutine = null;
    }
}
