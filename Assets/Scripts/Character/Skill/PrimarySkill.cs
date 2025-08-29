using System.Collections;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Photon.Pun;

[CreateAssetMenu(menuName = "Skill Effects/Primary Skill")]
public class PrimarySkill : ISkillEffect
{
    public const string WARRIOR_SKILL_EFFECTS = "Player/Warrior Skill VFX";
    public const string ARCHER_SKILL_EFFECTS = "Player/Archer Skill VFX";

    #region Warrior Primary Skills
    [Header("Warrior (Mend)")]
    public float attackBuff = 15f;
    public float attackBuffDuration = 10f;
    public GameObject attackBuffEffect; // Networked prefab: "AttackBuffEffect"
    public Vector3 attackBuffEffectOffset;

    public float instantHealthRecoverPercentage = 5f;
    public float healthRegenerationPercentage = 2f;
    public float healthRegenerationDuration = 5f;
    public GameObject instantHealthRecoverEffect; // Networked prefab: "InstantHealthRecoverEffect"
    public Vector3 instantHealthRecoverEffectOffset;
    public GameObject healthRegenerationEffect;   // Networked prefab: "HealthRegenerationEffect"
    public Vector3 healthRegenerationEffectOffset;
    public AudioClip mendSoundEffect;

    public float mendCooldown = 15f;

    [Header("Warrior (Sword Smash)")]
    public float swordsmashDamageMultiplier = 2.5f;
    public float swordSmashRadius = 5f;
    public float swordsmashCastTime = 2f;
    public float swordsmashCooldown = 15f;
    public Vector3 skillEffectOffset;
    public GameObject hitImpactEffect; // Networked prefab: "HitImpactEffect"
    public AudioClip hitImpactSoundEffect;
    #endregion

    #region Archer Primary Skills
    [Header("Archer (Quick Draw)")]
    public float attackSpeedBuff = 50f;
    public float attackSpeedBuffDuration = 10f;
    public float attackSpeedBuffCooldown = 15f;
    public AudioClip quickDrawSoundEffect;

    [Header("Archer (Hemorrhage)")]
    public float bleedDuration = 6f;
    public float bleedDamagePerTick = 10f;
    public float bleedTickInterval = 1f;
    public float hemorrhageBuffDuration = 10f;
    public float hemorrhageCooldown = 15f;
    public GameObject hemorrhageEffectOnBow; // Networked prefab: "HemorrhageEffectOnBow"
    public AudioClip hemorrhageSoundEffect;
    #endregion

    public override void ApplyEffect(GameObject user, GameObject target)
    {
        if (!user.CompareTag("Player"))
            return;

        PlayerAttribute playerAttribute = user.GetComponent<PlayerAttribute>();
        List<PlayerSkillUpgrade> playerSkillUpgrades = playerAttribute.GetPlayerSkillUpgrade();

        if (playerAttribute.GetPlayerClass() == PlayerClass.Warrior)
        {
            PlayerSkillUpgrade primarySkillUpgrade = playerSkillUpgrades
                .FirstOrDefault(upgrade => upgrade.playerSkillUpgradeType == PlayerSkillUpgradeType.PrimarySkill);

            if (primarySkillUpgrade.optionOne) // Rage (Mend)
            {
                Mend(playerAttribute);
                SfxManager.instance.photonView.RPC("RPC_PlaySoundFXClip", RpcTarget.All, mendSoundEffect.name, playerAttribute.transform.position, 1f);
            }
            else if (primarySkillUpgrade.optionTwo) // Sword Smash
            {
                SwordSmash(playerAttribute);
            }
        }
        else if (playerAttribute.GetPlayerClass() == PlayerClass.Archer)
        {
            PlayerSkillUpgrade primarySkillUpgrade = playerSkillUpgrades
                .FirstOrDefault(upgrade => upgrade.playerSkillUpgradeType == PlayerSkillUpgradeType.PrimarySkill);

            if (primarySkillUpgrade.optionOne) // Quick Draw
            {
                SfxManager.instance.photonView.RPC("RPC_PlaySoundFXClip", RpcTarget.All, quickDrawSoundEffect.name, playerAttribute.transform.position, 1f);

                float calculatedAttackSpeed = CalculateAttackSpeed(playerAttribute.GetPlayerStats().currentAttackSpeed);
                playerAttribute.SetStat(Stat.AttackSpeed, calculatedAttackSpeed);
                playerAttribute.skillEffectBarManager.ApplyBuff("PrimaryQuickDrawBuff", attackSpeedBuffDuration, PlayerSkillUpgradeType.PrimarySkill);
                CoroutineRunner.Instance.StartCoroutine(RemoveStatBuff(playerAttribute, attackSpeedBuffDuration, -calculatedAttackSpeed, Stat.AttackSpeed));
            }
            else if (primarySkillUpgrade.optionTwo) // Hemorrhage
            {
                Hemmorhage(user);
                SfxManager.instance.photonView.RPC("RPC_PlaySoundFXClip", RpcTarget.All, hemorrhageSoundEffect.name, playerAttribute.transform.position, 1f);
            }
        }
    }

    #region Warrior (Sword Smash) Functions
    public void SwordSmash(PlayerAttribute playerAttribute)
    {
        // Make the player immune while performing the skill.
        playerAttribute.currentDamageReduction = 100;
        CoroutineRunner.Instance.StartCoroutine(RemoveDamageReductionBuff(playerAttribute, swordsmashCastTime + 1f));
        CoroutineRunner.Instance.StartCoroutine(PerformingSwordSmash(playerAttribute));
    }

    public IEnumerator PerformingSwordSmash(PlayerAttribute playerAttribute)
    {
        CharacterCombo characterCombo = playerAttribute.GetComponent<CharacterCombo>();
        characterCombo.photonView.RPC("RPC_PlaySwordSmashAnimation", RpcTarget.All);

        yield return new WaitForSeconds(swordsmashCastTime);

        SfxManager.instance.photonView.RPC("RPC_PlaySoundFXClip", RpcTarget.All, hitImpactSoundEffect.name, playerAttribute.transform.position, 0.6f);
        CameraShaker.Instance.ShakeCamera(CameraShaker.SHAKE_TYPE_EXPLOSION, 0.3f);
        
        // Delegate network instantiation via SkillManager and set as child of player.
        SkillManager skillManager = playerAttribute.GetComponent<SkillManager>();
        if (skillManager != null)
        {
            skillManager.NetworkInstantiateSkillEffect(WARRIOR_SKILL_EFFECTS, hitImpactEffect,
                playerAttribute.transform.position + playerAttribute.transform.forward * skillEffectOffset.z,
                Quaternion.identity);
        }

        Collider[] hitEnemies = Physics.OverlapSphere(playerAttribute.transform.position, swordSmashRadius, characterCombo.enemyLayer);
        foreach (Collider enemy in hitEnemies)
        {
            IDamageable damageable = enemy.GetComponent<IDamageable>();
            PhotonView enemyPhotonView = enemy.GetComponent<PhotonView>();

            if (damageable != null && enemyPhotonView != null)
            {
                // Apply damage via RPC
                enemyPhotonView.RPC("TakeDamage", RpcTarget.All, playerAttribute.GetPlayerStats().currentDamage * swordsmashDamageMultiplier);
                Debug.Log("Dealing damage to enemy using Sword Smash");
            }
        }
    }
    #endregion

    #region Warrior (Mend) Functions
    public void Mend(PlayerAttribute playerAttribute)
    {
        float calculatedDamage = CalculateDamage(playerAttribute.GetPlayerStats().currentDamage);
        playerAttribute.SetStat(Stat.Damage, calculatedDamage);

        SkillManager skillManager = playerAttribute.GetComponent<SkillManager>();
        if (skillManager != null)
        {
            // Instantiate attack buff effect as a child of the player.
            GameObject abEffect = skillManager.NetworkInstantiateSkillEffect(
                WARRIOR_SKILL_EFFECTS,
                attackBuffEffect,
                playerAttribute.transform.position + attackBuffEffectOffset,
                Quaternion.identity,
                playerAttribute.transform);
            // (No need to manually configure particle duration here if AutoDestroyEffect is attached.)
        }
        playerAttribute.skillEffectBarManager.ApplyBuff("PrimaryAttackBuff", attackBuffDuration, PlayerSkillUpgradeType.PrimarySkill);
        CoroutineRunner.Instance.StartCoroutine(RemoveStatBuff(playerAttribute, attackBuffDuration, -calculatedDamage, Stat.Damage));

        // Instant Health Recover
        playerAttribute.Heal(playerAttribute.rangedBaseHealth * instantHealthRecoverPercentage / 100);
        if (skillManager != null)
        {
            skillManager.NetworkInstantiateSkillEffect(
                WARRIOR_SKILL_EFFECTS,
                instantHealthRecoverEffect,
                playerAttribute.transform.position + instantHealthRecoverEffectOffset,
                Quaternion.identity,
                playerAttribute.transform);
            skillManager.NetworkInstantiateSkillEffect(
                WARRIOR_SKILL_EFFECTS,
                healthRegenerationEffect,
                playerAttribute.transform.position + healthRegenerationEffectOffset,
                Quaternion.identity,
                playerAttribute.transform);
        }

        TemporaryRegeneration(playerAttribute);
    }


    public void TemporaryRegeneration(PlayerAttribute playerAttribute)
    {
        SkillManager skillManager = playerAttribute.GetComponent<SkillManager>();
        if (skillManager != null)
        {
            GameObject regenEffect = skillManager.NetworkInstantiateSkillEffect(WARRIOR_SKILL_EFFECTS, healthRegenerationEffect, playerAttribute.transform.position + healthRegenerationEffectOffset, Quaternion.identity, playerAttribute.transform);
            if (regenEffect != null)
            {
                ParticleSystem ps = regenEffect.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.duration = healthRegenerationDuration;
                    main.stopAction = ParticleSystemStopAction.Destroy;
                    ps.Play();
                    CoroutineRunner.Instance.StartCoroutine(StopParticleEffect(ps, healthRegenerationDuration));
                }
            }
        }
        playerAttribute.skillEffectBarManager.ApplyBuff("PrimaryHealthRegenetation", healthRegenerationDuration, PlayerSkillUpgradeType.PrimarySkill);
        CoroutineRunner.Instance.StartCoroutine(PerformRegeneration(playerAttribute));
    }

    public IEnumerator PerformRegeneration(PlayerAttribute playerAttribute)
    {
        float elapsedTime = 0f;
        while (elapsedTime < healthRegenerationDuration)
        {
            yield return new WaitForSeconds(1f);
            elapsedTime += 1f;
            playerAttribute.Heal(playerAttribute.rangedBaseHealth * healthRegenerationPercentage / 100);
        }
    }
    #endregion

    #region Archer (Hemorrhage) Functions
    public void Hemmorhage(GameObject user)
    {
        Archer archer = user.GetComponent<Archer>();
        PlayerAttribute playerAttribute = user.GetComponent<PlayerAttribute>();
        playerAttribute.skillEffectBarManager.ApplyBuff("PrimaryHemorhageBuff", hemorrhageBuffDuration, PlayerSkillUpgradeType.PrimarySkill);
        CoroutineRunner.Instance.StartCoroutine(ActivateBleedBuff(archer));
    }

    public IEnumerator ActivateBleedBuff(Archer archer)
    {
        archer.hemorrhageActive = true;
        SkillManager skillManager = archer.GetComponent<SkillManager>();
        if (skillManager != null)
        {
            skillManager.NetworkInstantiateSkillEffect(ARCHER_SKILL_EFFECTS, hemorrhageEffectOnBow, archer.castPoint.position, archer.castPoint.rotation, archer.castPoint);
        }
        yield return new WaitForSeconds(hemorrhageBuffDuration);
        archer.hemorrhageActive = false;
    }
    #endregion

    public float CalculateDamage(float damage)
    {
        return damage * attackBuff / 100;
    }

    public float CalculateAttackSpeed(float attackSpeed)
    {
        return attackSpeed * attackSpeedBuff / 100;
    }

    IEnumerator RemoveStatBuff(PlayerAttribute playerAttribute, float buffDuration, float buffValue, string stat)
    {
        yield return new WaitForSeconds(buffDuration);
        playerAttribute.SetStat(stat, buffValue);
    }

    public IEnumerator RemoveDamageReductionBuff(PlayerAttribute playerAttribute, float buffDuration)
    {
        yield return new WaitForSeconds(buffDuration);
        playerAttribute.currentDamageReduction = 0;
    }

    public IEnumerator StopParticleEffect(ParticleSystem particleSystem, float duration)
    {
        yield return new WaitForSeconds(duration);
        particleSystem.Stop();
    }
}
