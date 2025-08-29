using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    public GameObject[] aoeIndicators; // Prefabs for AoE effects
    public Transform arenaCenter; // Center of the boss fight area
    public Transform player; // Player transform for tracking attacks
    public float arenaRadius = 10f; // Max distance for random attacks
    public float attackInterval = 3f; // Time between random attacks
    public float delayBetweenAttacks = 1.0f; // Delay between attack repetitions

    private void Start()
    {
        InvokeRepeating(nameof(PerformRandomAttack), attackInterval, attackInterval); // Periodic random attacks
    }

    private void Update()
    {
        // Trigger multiple attack patterns with keys 1-7
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartCoroutine(PerformMultipleAttacks(0, 5)); // Circular pattern 5 times
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            StartCoroutine(PerformMultipleAttacks(1, 1)); // Spiral pattern 5 times
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            StartCoroutine(PerformMultipleAttacks(2, 5)); // Random pattern 5 times
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            StartCoroutine(PerformMultipleAttacks(3, 5)); // Wave pattern 5 times
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            StartCoroutine(PerformMultipleAttacks(4, 2)); // Perform AoE sequence
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            StartCoroutine(PerformMultipleAttacks(5, 1)); // Expand AoE once
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            StartCoroutine(PerformMultipleAttacks(6, 1)); // Track player with AoE once
        }
    }

    // Periodic random attack
    private void PerformRandomAttack()
    {
        int randomIndex = Random.Range(0, aoeIndicators.Length); // Select an AoE pattern
        GameObject aoe = Instantiate(aoeIndicators[randomIndex]);

        Vector3 randomPosition = GetRandomPosition();
        aoe.transform.position = randomPosition;

        StartCoroutine(ExpandAoE(aoe, 2f, 3f)); // Expanding AoE over 2 seconds
    }

    // Multiple attack pattern
    private IEnumerator PerformMultipleAttacks(int pattern, int repetitions)
    {
        for (int i = 0; i < repetitions; i++)
        {
            ChooseAttackPattern(pattern); // Trigger the chosen attack pattern
            yield return new WaitForSeconds(delayBetweenAttacks); // Wait for the specified delay
        }
    }

    // Select an attack pattern
    void ChooseAttackPattern(int pat)
    {
        switch (pat)
        {
            case 0: ShootCircularPattern(); break;
            case 1: ShootSpiralPattern(); break;
            case 2: ShootRandomPattern(); break;
            case 3: ShootWavePattern(); break;
            case 4: StartCoroutine(PerformAoESequence()); break;
            case 5:
                GameObject expandAoE = Instantiate(aoeIndicators[0]);
                StartCoroutine(ExpandAoE(expandAoE, 3f, 5f));
                break;
            case 6:
                GameObject trackAoE = Instantiate(aoeIndicators[0]);
                StartCoroutine(TrackPlayer(trackAoE, player, 3f));
                break;
        }
    }

    // Attack patterns
    void ShootCircularPattern()
    {
        Debug.Log("Performing Circular Attack Pattern");
        GameObject aoe = Instantiate(aoeIndicators[0]); // Example: use the first indicator
        StartCoroutine(ExpandAoE(aoe, 2f, 3f)); // Expanding AoE
    }

    void ShootSpiralPattern()
    {
        Debug.Log("Performing Multi-Line Spiral Attack Pattern");

        int numberOfLines = 6; // Number of spiral lines (e.g., 6 lines evenly spaced)
        int numberOfAoEsPerLine = 20; // Number of AoEs per line
        float angleBetweenLines = 360f / numberOfLines; // Angle separating each line
        float distanceIncrement = arenaRadius / numberOfAoEsPerLine; // Distance step for each AoE
        float aoeLifetime = 2f; // Time before AoE is destroyed
        float aoeSize = 1.5f; // Size of the AoEs

        for (int line = 0; line < numberOfLines; line++)
        {
            float baseAngle = angleBetweenLines * line; // Starting angle for this line

            for (int i = 0; i < numberOfAoEsPerLine; i++)
            {
                float angleInRadians = Mathf.Deg2Rad * (baseAngle + i * 5f); // Slight angle increase per AoE for curve
                float distance = i * distanceIncrement; // Distance from the center

                // Calculate position for this AoE
                Vector3 spiralPosition = arenaCenter.position +
                                         new Vector3(Mathf.Cos(angleInRadians) * distance, 0, Mathf.Sin(angleInRadians) * distance);

                // Instantiate the AoE and set its size
                GameObject aoe = Instantiate(aoeIndicators[0], spiralPosition, aoeIndicators[0].transform.localRotation); // Use first AoE prefab
                aoe.transform.localScale = Vector3.one * aoeSize; // Adjust the size of the AoE

                // Destroy the AoE after its lifetime
                Destroy(aoe, aoeLifetime);
            }
        }
    }





    void ShootRandomPattern()
    {
        Debug.Log("Performing Random Attack Pattern");
        for (int i = 0; i < 4; i++) // Spawn 4 random AoEs
        {
            GameObject aoe = Instantiate(aoeIndicators[0]);
            aoe.transform.position = GetRandomPosition();
            StartCoroutine(ExpandAoE(aoe, 2f, 2f));
        }
    }

    void ShootWavePattern()
    {
        Debug.Log("Performing Wave Attack Pattern");
        for (int i = 0; i < 5; i++) // Example: Wave of 5 AoEs in a line
        {
            GameObject aoe = Instantiate(aoeIndicators[0]);
            aoe.transform.position = arenaCenter.position + new Vector3(i * 2, 0, 0);
            StartCoroutine(ExpandAoE(aoe, 2f, 2f));
        }
    }

    // AoE Sequence
    private IEnumerator PerformAoESequence()
    {
        Debug.Log("Performing AoE Sequence");
        PerformRandomAttack(); // First attack
        yield return new WaitForSeconds(1f); // Delay between attacks
        PerformRandomAttack(); // Second attack
    }

    // Expanding AoE
    private IEnumerator ExpandAoE(GameObject aoe, float duration, float maxSize)
    {
        float timer = 0;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = new Vector3(maxSize, maxSize, maxSize);
        while (timer < duration)
        {
            timer += Time.deltaTime;
            aoe.transform.localScale = Vector3.Lerp(startScale, endScale, timer / duration);
            yield return null;
        }
        Destroy(aoe);
    }

    // AoE tracking the player
    private IEnumerator TrackPlayer(GameObject aoe, Transform player, float duration)
    {
        Debug.Log("Tracking Player with AoE");
        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            aoe.transform.position = player.position;
            yield return null;
        }
        Destroy(aoe); // Trigger damage or other effects
    }

    // Generate random position within arena
    private Vector3 GetRandomPosition()
    {
        Vector2 randomPoint = Random.insideUnitCircle * arenaRadius; // Random point in a circle
        return new Vector3(randomPoint.x, 0, randomPoint.y) + arenaCenter.position;
    }
}
