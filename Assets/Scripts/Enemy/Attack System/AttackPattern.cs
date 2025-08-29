using UnityEngine;

public abstract class AttackPattern : ScriptableObject, IAttackPattern
{
    public abstract void Execute(GameObject boss); // Execute pattern logic
}
