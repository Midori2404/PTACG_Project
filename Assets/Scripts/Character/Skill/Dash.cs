using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;

[CreateAssetMenu(menuName = "Skill Effects/Dash Effect")]
public class Dash : ISkillEffect
{
    public const string ARCHER_SKILL_EFFECTS = "Player/Archer Skill VFX";
    [Header("Default")]
    public float moveSpeedBuff;
    public float moveSpeedBuffDuration;

    [Header("Warrior")]
    public float damageReduction;
    public float damageReductionDuration;

    [Header("Archer")]
    public float temporaryShield;
    public float temporaryShieldDuration;
    public GameObject temporaryShieldEffect;
    public AudioClip tempraryShieldSound;

    public override void ApplyEffect(GameObject user, GameObject target)
    {
        if (user.CompareTag("Player"))
        {
            PlayerAttribute playerAttribute = user.GetComponent<PlayerAttribute>();
            List<PlayerSkillUpgrade> playerSkillUpgrades = playerAttribute.GetPlayerSkillUpgrade();

            if (playerAttribute.GetPlayerClass() == PlayerClass.Warrior)
            {
                PlayerSkillUpgrade dashSkillUpgrade = playerSkillUpgrades
                    .FirstOrDefault(upgrade => upgrade.playerSkillUpgradeType == PlayerSkillUpgradeType.Dash);

                if (dashSkillUpgrade.optionOne) // Move Speed Buff
                {
                    MoveSpeedBuff(playerAttribute);
                }
                else if (dashSkillUpgrade.optionTwo) // Damage Reduction
                {
                    DamageReduction(playerAttribute);
                }
            }
            else if (playerAttribute.GetPlayerClass() == PlayerClass.Archer)
            {
                PlayerSkillUpgrade dashSkillUpgrade = playerSkillUpgrades
                    .FirstOrDefault(upgrade => upgrade.playerSkillUpgradeType == PlayerSkillUpgradeType.Dash);

                if (dashSkillUpgrade.optionOne) // Move Speed Buff
                {
                    MoveSpeedBuff(playerAttribute);
                }
                else if (dashSkillUpgrade.optionTwo) // Temporary Shield
                {
                    TemporaryShield(playerAttribute);
                }
            }
        }
    }

    public void MoveSpeedBuff(PlayerAttribute playerAttribute)
    {
        // Play the trail effect via the PlayerController (which might be local);
        // additionally, network a buff effect so all players see it.
        PlayerController playerController = playerAttribute.GetComponent<PlayerController>();
        playerController.PlayMoveSpeedTrail(moveSpeedBuffDuration);

        playerAttribute.skillEffectBarManager.ApplyBuff("DashSpeedBuff", moveSpeedBuffDuration, PlayerSkillUpgradeType.Dash);

        float originalSpeed = playerAttribute.GetPlayerStats().currentSpeed;
        playerAttribute.SetStat(Stat.Speed, moveSpeedBuff);
        CoroutineRunner.Instance.StartCoroutine(RemoveBuff(playerAttribute, originalSpeed));
    }

    public void DamageReduction(PlayerAttribute playerAttribute)
    {
        playerAttribute.currentDamageReduction = damageReduction;
        playerAttribute.skillEffectBarManager.ApplyBuff("DashDamageReduction", damageReductionDuration, PlayerSkillUpgradeType.Dash);
        CoroutineRunner.Instance.StartCoroutine(RemoveDamageReduction(playerAttribute));
    }

    public void TemporaryShield(PlayerAttribute playerAttribute)
    {
        playerAttribute.temporaryShield = temporaryShield;
        playerAttribute.UpdateHealthUI();
        SkillManager skillManager = playerAttribute.GetComponent<SkillManager>();
        skillManager.NetworkInstantiateSkillEffect(ARCHER_SKILL_EFFECTS, temporaryShieldEffect, playerAttribute.transform.position, Quaternion.identity, playerAttribute.transform);
        SfxManager.instance.RPC_PlaySoundFXClip(tempraryShieldSound.name, playerAttribute.transform.position, 1f);
        playerAttribute.skillEffectBarManager.ApplyBuff("DashTempShield", temporaryShieldDuration, PlayerSkillUpgradeType.Dash);
        CoroutineRunner.Instance.StartCoroutine(RemoveTemporaryShield(playerAttribute));
    }

    IEnumerator RemoveBuff(PlayerAttribute playerAttribute, float originalSpeed)
    {
        yield return new WaitForSeconds(moveSpeedBuffDuration);
        // Restore original speed (adjusting by subtracting the buff value)
        playerAttribute.SetStat(Stat.Speed, originalSpeed - playerAttribute.GetPlayerStats().currentSpeed);
    }

    IEnumerator RemoveDamageReduction(PlayerAttribute playerAttribute)
    {
        yield return new WaitForSeconds(damageReductionDuration);
        playerAttribute.currentDamageReduction = 0;
    }

    IEnumerator RemoveTemporaryShield(PlayerAttribute playerAttribute)
    {
        yield return new WaitForSeconds(temporaryShieldDuration);
        playerAttribute.temporaryShield = 0;
        playerAttribute.UpdateHealthUI();
    }
}
