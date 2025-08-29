using System;
using UnityEngine;

public class BossPhaseManager : MonoBehaviour
{
    [Header("Phases")]
    public BossPhase[] phases;    // Array of phases
    public int currentPhaseIndex = 0; // Current phase index
    public EventHandler OnPhaseChange;

    public void CheckPhaseTransition(float currentHealth, float maxHealth)
    {
        for (int i = currentPhaseIndex; i < phases.Length; i++) // Start from the current phase index
        {
            // Skip already triggered phases
            if (phases[i].isTriggered)
                continue;

            // Check if health threshold is met
            if (currentHealth <= phases[i].triggerHealthPercentage * maxHealth / 100f)
            {
                EnterPhase(i);
                break; // Stop checking further phases
            }
        }
    }

    private void EnterPhase(int phaseIndex)
    {
        currentPhaseIndex = phaseIndex;

        // Mark this phase as triggered
        phases[phaseIndex].isTriggered = true;

        Debug.Log($"Entering Phase: {phases[phaseIndex].phaseName}");

        // Trigger phase change event
        OnPhaseChange?.Invoke(this, EventArgs.Empty);

        // Execute phase logic (Uncomment if using a specific logic per phase)
        // phases[phaseIndex].OnPhaseEnter(this);
    }

}

[System.Serializable]
public class BossPhase
{
    public string phaseName; // Name of the phase
    public float triggerHealthPercentage; // Trigger when health <= this percentage
    public bool isTriggered;
}
