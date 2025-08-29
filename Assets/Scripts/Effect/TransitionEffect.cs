using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TransitionEffect : MonoBehaviour
{
    public static TransitionEffect Instance { get; private set; }
    [SerializeField] private Image fadeImage; // The Image component for the fade effect

    private void Awake()
    {
        Instance = this;
        if (fadeImage == null)
        {
            Debug.LogError("Fade Image is not assigned. Please assign an Image component in the inspector.");
        }
    }

    /// <summary>
    /// Fades in by increasing the alpha value of the Image.
    /// </summary>
    public IEnumerator FadeIn(float fadeDuration)
    {
        float elapsedTime = 0f;
        Color color = fadeImage.color;

        while (elapsedTime < fadeDuration)
        {
            color.a = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            fadeImage.color = color;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        color.a = 0f;
        fadeImage.color = color;
    }

    /// <summary>
    /// Fades out by decreasing the alpha value of the Image.
    /// </summary>
    public IEnumerator FadeOut(float fadeDuration)
    {
        float elapsedTime = 0f;
        Color color = fadeImage.color;

        while (elapsedTime < fadeDuration)
        {
            color.a = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            fadeImage.color = color;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        color.a = 1f;
        fadeImage.color = color;
    }

    /// <summary>
    /// Performs a fade-in followed by a fade-out.
    /// </summary>
    public IEnumerator FadeInAndOut(float fadeDuration, float delayDuration)
    {
        yield return StartCoroutine(FadeOut(fadeDuration));
        yield return new WaitForSeconds(delayDuration); // Optional delay between fade-in and fade-out
        yield return StartCoroutine(FadeIn(fadeDuration));
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
            StartCoroutine(FadeIn(1f));
        else if (Input.GetKeyDown(KeyCode.Y))
            StartCoroutine(FadeOut(1f));
        else if (Input.GetKeyDown(KeyCode.U))
            StartCoroutine(FadeInAndOut(1f, 1f));
    }
}
