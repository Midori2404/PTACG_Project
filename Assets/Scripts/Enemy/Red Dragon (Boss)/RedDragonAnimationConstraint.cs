using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class RedDragonAnimationConstraint : MonoBehaviour
{
    public RigBuilder rigBuilder;                   // Reference to the RigBuilder in your scene
    public List<MultiAimConstraint> aimConstraints; // List of all Multi-Aim Constraints

    private Dictionary<MultiAimConstraint, float> initialWeights = new Dictionary<MultiAimConstraint, float>();
    // Start is called before the first frame update
    void Awake()
    {
        rigBuilder = GetComponent<RigBuilder>();
        // Cache the initial weights of all constraints
        CacheInitialWeights();
    }

    void CacheInitialWeights()
    {
        foreach (var constraint in aimConstraints)
        {
            if (constraint != null && !initialWeights.ContainsKey(constraint))
            {
                initialWeights.Add(constraint, constraint.weight);
            }
        }
    }

    public void UpdateAllConstraints(Transform currentTarget)
    {
        if (currentTarget == null) return;

        foreach (var constraint in aimConstraints)
        {
            if (constraint == null) continue;

            // Clear existing sources
            var data = constraint.data.sourceObjects;
            data.Clear();

            // Add the new target as the source
            data.Add(new WeightedTransform(currentTarget, 1f));

            // Apply the updated source objects
            constraint.data.sourceObjects = data;
        }

        rigBuilder.Build();
    }

    // Function to smoothly transition the weight of all Multi-Aim Constraints
    public void SmoothlySetConstraintWeights(float targetWeight, float duration)
    {
        StartCoroutine(SmoothWeightTransition(targetWeight, duration));
    }

    private IEnumerator SmoothWeightTransition(float targetWeight, float duration)
    {
        float elapsedTime = 0f;

        // Use cached initial weights
        Dictionary<MultiAimConstraint, float> startWeights = new Dictionary<MultiAimConstraint, float>(initialWeights);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            foreach (var constraint in aimConstraints)
            {
                if (constraint != null && startWeights.ContainsKey(constraint))
                {
                    float startWeight = startWeights[constraint];
                    constraint.weight = Mathf.Lerp(startWeight, targetWeight, t);
                }
            }

            yield return null;
        }

        // Ensure all weights are set to the exact target value at the end
        foreach (var constraint in aimConstraints)
        {
            if (constraint != null)
                constraint.weight = targetWeight;
        }
    }

    // Smoothly reset all weights to their initial values
    public void SmoothlyResetToInitialWeights(float duration)
    {
        StartCoroutine(SmoothResetTransition(duration));
    }

    private IEnumerator SmoothResetTransition(float duration)
    {
        float elapsedTime = 0f;

        // Capture the current weights
        Dictionary<MultiAimConstraint, float> currentWeights = new Dictionary<MultiAimConstraint, float>();
        foreach (var constraint in aimConstraints)
        {
            if (constraint != null)
            {
                currentWeights[constraint] = constraint.weight;
            }
        }

        // Perform the transition
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            foreach (var constraint in aimConstraints)
            {
                if (constraint != null && initialWeights.ContainsKey(constraint))
                {
                    float startWeight = currentWeights[constraint];
                    float targetWeight = initialWeights[constraint];
                    constraint.weight = Mathf.Lerp(startWeight, targetWeight, t);
                }
            }

            yield return null; // Wait for the next frame
        }

        // Ensure final weight is exactly the initial weight
        foreach (var constraint in aimConstraints)
        {
            if (constraint != null && initialWeights.ContainsKey(constraint))
            {
                constraint.weight = initialWeights[constraint];
            }
        }
    }

}

