using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using System.Linq;
using UnityEngine.TextCore.Text;
using Photon.Pun;
using ExitGames.Client.Photon.StructWrapping;

[CreateAssetMenu(menuName = "Skill Effects/Secondary Skill")]
public class SecondarySkill : ISkillEffect
{
    public const string WARRIOR_SKILL_EFFECTS = "Player/Warrior Skill VFX";
    public const string ARCHER_SKILL_EFFECTS = "Player/Archer Skill VFX";

    #region Fields
    [Header("Front Spike Attack (Warrior)")]
    public float frontSpikeDamageMultiplier;
    public float frontSpikeAngle;
    public float frontSpikeCastTime;
    public float frontSpikeCooldown;
    public GameObject frontSpikeEffect; // Assigned in Inspector.
    public Vector3 skillEffectOffset;

    // Side effect parameters…
    public float frontSpikeParalyzeDuration;
    public float frontSpikeTickInterval;
    public float frontSpikeDamagePerTick;
    public float frontSpikeSlowDuration;
    public float frontSpikeSlowAmount;

    // Sound Effects
    public AudioClip hitImpactSoundEffect;
    public AudioClip electricSoundEffect;

    [Header("Berserk (Warrior)")]
    public float healthToSacrifice;
    public float berserkCooldown;
    public float berserkCastTime;
    public float berserkDuration;
    public float berserkAttackBoost;
    public float berserkSpeedBoost;
    public GameObject berserkActivationEffect; // Assigned in Inspector.
    public GameObject[] berserkAfterEffects;     // Assigned in Inspector.
    public Vector3 berserkAfterEffectsOffset;

    // Sound Effect
    public AudioClip berserkSound;

    [Header("Thundering Rain Arrow (Archer)")]
    public float rainArrowCooldown;
    public float rainArrowDuration;
    public float rainArrowRadius;
    public float rainArrowDamagePerTick;
    public float castTime;
    public GameObject rainArrowEffect;      // Assigned in Inspector.
    public GameObject rainArrowIndicator;   // Assigned in Inspector.
    public GameObject rainArrowCastPrefab;  // Assigned in Inspector.
    public GameObject rainArrowReleasedEffect; // Assigned in Inspector.

    public AudioClip rainArrowCastSoundEffect;
    public AudioClip rainArrowSoundEffect;

    private GameObject activeIndicator;

    [Header("Shock Blast (Archer)")]
    public float shockBlastCooldown;
    public float shockBlastDuration;
    public int shockBlastBullets;
    public float shockBlastDamage;
    public float shockBlastDelay;
    public float shockBlastCastTime;
    public GameObject shockBlastProjectile; // Assigned in Inspector.
    public GameObject[] shockBlastCastPrefabs; // Assigned in Inspector.
    public GameObject shockBlastReleasedEffect; // Assigned in Inspector.
    public GameObject shockBlastCastAura;
    public GameObject shockBlastIndicator; // Assigned in Inspector.

    public AudioClip shockBlastCastSound;
    #endregion

    public override void ApplyEffect(GameObject user, GameObject target)
    {
        if (!user.CompareTag("Player"))
            return;

        PlayerAttribute playerAttribute = user.GetComponent<PlayerAttribute>();
        List<PlayerSkillUpgrade> playerSkillUpgrades = playerAttribute.GetPlayerSkillUpgrade();

        if (playerAttribute.GetPlayerClass() == PlayerClass.Warrior)
        {
            PlayerSkillUpgrade secondarySkillUpgrade = playerSkillUpgrades
                .FirstOrDefault(upgrade => upgrade.playerSkillUpgradeType == PlayerSkillUpgradeType.SecondarySkill);

            if (secondarySkillUpgrade.optionOne) // AOE Spike Attack
            {
                SpikeAttack(user);
            }
            else if (secondarySkillUpgrade.optionTwo) // Berserk
            {
                CoroutineRunner.Instance.StartCoroutine(Berserk(user));
            }
        }
        else if (playerAttribute.GetPlayerClass() == PlayerClass.Archer)
        {
            PlayerSkillUpgrade secondarySkillUpgrade = playerSkillUpgrades
                .FirstOrDefault(upgrade => upgrade.playerSkillUpgradeType == PlayerSkillUpgradeType.SecondarySkill);

            if (secondarySkillUpgrade.optionOne) // Arrow Rain
            {
                ArrowRain(user);
            }
            else if (secondarySkillUpgrade.optionTwo) // Shock Blast
            {
                ShockBlast(user);
            }
        }
    }

    #region Front Spike Skill (Warrior)
    public void SpikeAttack(GameObject user)
    {
        if (user.TryGetComponent(out PlayerAttribute playerAttribute))
            CoroutineRunner.Instance.StartCoroutine(PerformSpikeAttack(playerAttribute));
    }

    public IEnumerator PerformSpikeAttack(PlayerAttribute playerAttribute)
    {
        playerAttribute.currentDamageReduction = 100;
        CoroutineRunner.Instance.StartCoroutine(RemoveDamageReductionBuff(playerAttribute, frontSpikeCastTime + 1f));

        CharacterCombo characterCombo = playerAttribute.GetComponent<CharacterCombo>();
        characterCombo.photonView.RPC("RPC_PlaySwordSmashAnimation", RpcTarget.All);

        yield return new WaitForSeconds(frontSpikeCastTime);

        SfxManager.instance.photonView.RPC("RPC_PlaySoundFXClip", RpcTarget.All, hitImpactSoundEffect.name, playerAttribute.transform.position, 1f);
        CameraShaker.Instance.ShakeCamera(CameraShaker.SHAKE_TYPE_EXPLOSION, 0.4f);

        Vector3 playerPosition = playerAttribute.transform.position;
        Vector3 forwardDirection = playerAttribute.transform.forward;
        float attackRadius = (20f + skillEffectOffset.z) / 2f;

        SkillManager skillManager = playerAttribute.GetComponent<SkillManager>();
        if (skillManager != null)
        {
            // Instantiate as child of the player.
            skillManager.NetworkInstantiateSkillEffect(WARRIOR_SKILL_EFFECTS, frontSpikeEffect,
                playerPosition + forwardDirection * skillEffectOffset.z,
                Quaternion.LookRotation(forwardDirection));
        }

        Collider[] hitEnemies = Physics.OverlapSphere(playerPosition, attackRadius, LayerMask.GetMask("Enemy"));
        foreach (Collider enemy in hitEnemies)
        {
            Vector3 direction = (enemy.transform.position - playerPosition).normalized;
            float angle = Vector3.Angle(forwardDirection, direction);
            PhotonView enemyPhotonView = enemy.GetComponent<PhotonView>();

            if (angle <= frontSpikeAngle && enemyPhotonView != null)
            {
                IDamageable damageable = enemy.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    enemyPhotonView.RPC("TakeDamage", RpcTarget.All, playerAttribute.GetPlayerStats().currentDamage * frontSpikeDamageMultiplier);
                    Debug.Log("Dealing damage to enemy using Spike Attack");
                    // NegativeEffectManager.Instance.ApplyNegativeEffect(enemy.gameObject, frontSpikeDamagePerTick, frontSpikeParalyzeDuration, frontSpikeTickInterval, NegativeEffectType.Paralyze);
                    object[] effectData = new object[] { enemyPhotonView.ViewID, frontSpikeDamagePerTick, frontSpikeParalyzeDuration, frontSpikeTickInterval, (int)NegativeEffectType.Paralyze };
                    NegativeEffectManager.Instance.photonView.RPC("RPC_ApplyNegativeEffect", RpcTarget.All, effectData);
                }
            }
        }
    }
    #endregion

    #region Berserk (Warrior)
    public IEnumerator Berserk(GameObject user)
    {
        SkillManager skillManager = user.GetComponent<SkillManager>();
        CharacterCombo characterCombo = user.GetComponent<CharacterCombo>();
        characterCombo.photonView.RPC("RPC_PlayBerserkAnimation", RpcTarget.All);

        yield return new WaitForSeconds(berserkCastTime);

        SfxManager.instance.photonView.RPC("RPC_PlaySoundFXClip", RpcTarget.All, berserkSound.name, characterCombo.transform.position, 1f);
        CameraShaker.Instance.ShakeCamera(CameraShaker.SHAKE_TYPE_EXPLOSION, 0.2f);

        if (berserkActivationEffect != null && skillManager != null)
        {
            GameObject effect = skillManager.NetworkInstantiateSkillEffect(WARRIOR_SKILL_EFFECTS, berserkActivationEffect,
                user.transform.position,
                Quaternion.Euler(-90, 0, 0), user.transform);
            if (effect != null)
                Destroy(effect, 1.4f);
        }

        if (berserkAfterEffects != null && skillManager != null)
        {
            foreach (var prefab in berserkAfterEffects)
            {
                GameObject effect = skillManager.NetworkInstantiateSkillEffect(WARRIOR_SKILL_EFFECTS, prefab,
                    user.transform.position + berserkAfterEffectsOffset,
                    Quaternion.identity, user.transform);
                if (effect != null)
                {
                    ParticleSystem ps = effect.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        var main = ps.main;
                        main.duration = berserkDuration;
                        main.loop = false;
                        ps.Play();
                        CoroutineRunner.Instance.StartCoroutine(StopAndDestroyEffect(ps, effect, berserkDuration));
                    }
                }
            }
        }

        PlayerAttribute playerAttribute = user.GetComponent<PlayerAttribute>();

        playerAttribute.skillEffectBarManager.ApplyBuff("SecondaryBerserkBuff", berserkDuration, PlayerSkillUpgradeType.SecondarySkill);

        float currentHealth = playerAttribute.GetPlayerStats().currentHealth;
        float healthToSacrificeAmount = currentHealth * healthToSacrifice;
        if (currentHealth - healthToSacrificeAmount < 1)
            healthToSacrificeAmount = currentHealth - 1;
        playerAttribute.SetStat(Stat.Health, -healthToSacrificeAmount);

        float attackIncrease = playerAttribute.GetPlayerStats().currentDamage * berserkAttackBoost;
        playerAttribute.SetStat(Stat.Damage, attackIncrease);

        float speedIncrease = playerAttribute.GetPlayerStats().currentSpeed * berserkSpeedBoost;
        playerAttribute.SetStat(Stat.Speed, speedIncrease);

        CoroutineRunner.Instance.StartCoroutine(RemoveBerserkBuff(playerAttribute, berserkDuration, attackIncrease, speedIncrease));
    }
    #endregion

    #region Archer Skills Function
    public void ArrowRain(GameObject user)
    {
        PlayerController playerController = user.GetComponent<PlayerController>();
        CoroutineRunner.Instance.StartCoroutine(HandleArrowRain(playerController));
    }

    private IEnumerator HandleArrowRain(PlayerController playerController)
    {
        Archer archer = playerController.TryGetComponent<Archer>(out archer) ? archer : null;
        SkillManager skillManager = playerController.GetComponent<SkillManager>();
        if (skillManager != null)
        {
            string indicatorPath = ARCHER_SKILL_EFFECTS + "/" + rainArrowIndicator.name;
            activeIndicator = skillManager.NetworkInstantiateSkillEffect(ARCHER_SKILL_EFFECTS, rainArrowIndicator, Vector3.zero, Quaternion.identity);
        }

        while (!Input.GetMouseButtonDown(0))
        {
            Vector3? hitPoint = playerController.GetMousePositionHitPoint();
            if (hitPoint.HasValue && activeIndicator != null)
                activeIndicator.transform.position = hitPoint.Value;
            yield return null;
        }

        Vector3 indicatorPos = activeIndicator.transform.position;
        PhotonNetwork.Destroy(activeIndicator);

        if (archer != null)
        {
            archer.inSkillState = true;
            archer.photonView.RPC("RPC_CastRainArrow", RpcTarget.All);
            SfxManager.instance.photonView.RPC("RPC_PlaySoundFXClip", RpcTarget.All, rainArrowCastSoundEffect.name, playerController.transform.position, 1f);
            CameraShaker.Instance.ShakeCamera(CameraShaker.SHAKE_TYPE_RECOIL, 0.2f);
        }

        if (skillManager != null)
        {
            string castPrefabPath = ARCHER_SKILL_EFFECTS + "/" + rainArrowCastPrefab.name;
            skillManager.NetworkInstantiateSkillEffect(ARCHER_SKILL_EFFECTS, rainArrowCastPrefab, archer.castPoint.position, archer.castPoint.rotation, archer.castPoint);

            string releasedPrefabPath = ARCHER_SKILL_EFFECTS + "/" + rainArrowReleasedEffect.name;
            skillManager.NetworkInstantiateSkillEffect(ARCHER_SKILL_EFFECTS, rainArrowReleasedEffect, archer.RainArrowFirePoint.position, archer.RainArrowFirePoint.rotation, archer.RainArrowFirePoint);

            yield return new WaitForSeconds(castTime);
            SfxManager.instance.photonView.RPC("RPC_PlaySoundFXClip", RpcTarget.All, rainArrowSoundEffect.name, playerController.transform.position, 1f);

            archer.ClearSkillState();

            string rainEffectPath = ARCHER_SKILL_EFFECTS + "/" + rainArrowEffect.name;
            GameObject rainEffectInstance = skillManager.NetworkInstantiateSkillEffect(ARCHER_SKILL_EFFECTS, rainArrowEffect, indicatorPos, Quaternion.identity);

            // Retrieve the PhotonView from the instantiated particle effect.
            PhotonView pv = rainEffectInstance.GetComponent<PhotonView>();
            if (pv != null)
            {
                // Pass the desired duration (or any other parameters you need) to the RPC.
                pv.RPC("RPC_SetupAndPlay", RpcTarget.All, rainArrowDuration, ParticleSystemStopAction.Destroy);
            }


            float elapsedTime = 0f;
            while (elapsedTime < rainArrowDuration)
            {
                Collider[] hitEnemies = Physics.OverlapSphere(rainEffectInstance.transform.position, rainArrowRadius, LayerMask.GetMask("Enemy"));
                foreach (Collider enemy in hitEnemies)
                {
                    IDamageable damageable = enemy.GetComponent<IDamageable>();
                    PhotonView enemyPhotonView = enemy.GetComponent<PhotonView>();
                    if (damageable != null && enemyPhotonView != null)
                    {
                        enemyPhotonView.RPC("TakeDamage", RpcTarget.All, rainArrowDamagePerTick);
                        Debug.Log("Dealing damage to enemy using Rain Arrow");
                    }
                }
                elapsedTime += 0.5f;
                yield return new WaitForSeconds(0.5f);
            }

            PhotonNetwork.Destroy(rainEffectInstance);
        }
    }
    #endregion

    #region Shock Blast (Archer)
    public void ShockBlast(GameObject user)
    {
        Archer archer = user.GetComponent<Archer>();
        // archer.inSkillState = true;
        CoroutineRunner.Instance.StartCoroutine(HandleShockBlast(archer));
    }

    private IEnumerator HandleShockBlast(Archer archer)
    {
        PlayerController playerController = archer.GetComponent<PlayerController>();
        playerController.SetCanMove(false);

        SkillManager skillManager = archer.GetComponent<SkillManager>();
        string indicatorPath = ARCHER_SKILL_EFFECTS + "/" + shockBlastIndicator.name;
        GameObject indicator = null;
        if (skillManager != null)
            indicator = skillManager.NetworkInstantiateSkillEffect(ARCHER_SKILL_EFFECTS, shockBlastIndicator, archer.transform.position, Quaternion.identity, archer.transform);

        float duration = shockBlastDuration;
        int bulletsLeft = shockBlastBullets;

        while (duration > 0 && bulletsLeft > 0)
        {
            Vector3? hitPoint = playerController.GetMousePositionHitPoint();
            if (hitPoint.HasValue && indicator != null)
            {
                Vector3 direction = (hitPoint.Value - archer.transform.position).normalized;
                indicator.transform.position = archer.transform.position;
                indicator.transform.rotation = Quaternion.LookRotation(direction);
            }

            if (Input.GetButtonDown("Fire1") && !archer.isCasting)
            {
                archer.isCasting = true;
                CoroutineRunner.Instance.StartCoroutine(ShootShockBlastProjectile(archer, playerController));
                SfxManager.instance.photonView.RPC("RPC_PlaySoundFXClip", RpcTarget.All, shockBlastCastSound.name, archer.transform.position, 1f);
                bulletsLeft--;
            }

            if ((Input.GetButton("Horizontal") || Input.GetButton("Vertical")) && !archer.isCasting && duration < shockBlastDuration - shockBlastDelay)
            {
                playerController.SetCanMove(true);
                archer.ClearSkillState();
                PhotonNetwork.Destroy(indicator);
                yield break;
            }

            duration -= Time.deltaTime;
            yield return null;
        }

        playerController.SetCanMove(true);
        archer.ClearSkillState();
        if (indicator != null)
            PhotonNetwork.Destroy(indicator);
    }

    private IEnumerator ShootShockBlastProjectile(Archer archer, PlayerController playerController)
    {
        archer.photonView.RPC("RPC_CastShockBlast", RpcTarget.All);

        SkillManager skillManager = archer.GetComponent<SkillManager>();
        if (skillManager != null)
        {
            skillManager.NetworkInstantiateSkillEffect(ARCHER_SKILL_EFFECTS, shockBlastCastAura, archer.transform.position, archer.transform.rotation, archer.transform);

            foreach (var prefab in shockBlastCastPrefabs)
            {
                skillManager.NetworkInstantiateSkillEffect(ARCHER_SKILL_EFFECTS, prefab, archer.castPoint.position, archer.castPoint.rotation, archer.castPoint);
            }
        }

        yield return new WaitForSeconds(shockBlastCastTime);
        archer.isCasting = false;

        Vector3 direction = (playerController.GetMousePositionHitPoint() - archer.transform.position).normalized;
        if (skillManager != null)
        {
            string projectilePath = ARCHER_SKILL_EFFECTS + "/" + shockBlastProjectile.name;
            // Prepare instantiation data so that the Projectile's Start() method can auto-initialize.
            object[] instantiationData = new object[]
            {
                direction,               // shootDirection
                shockBlastDamage,        // projectileDamage
                (int)NegativeEffectType.None, // effectType as int
                archer.GetComponent<PhotonView>().ViewID, // shooter PhotonView ID
                0f,                      // damagePerTick (if applicable)
                0f,                      // dotDuration
                0f                       // tickInterval
            };

            // Instantiate the projectile via PhotonNetwork.Instantiate so that all clients get it.
            GameObject projectile = PhotonNetwork.Instantiate(projectilePath, archer.firePoint.position, Quaternion.LookRotation(direction), 0, instantiationData);
            
            CameraShaker.Instance.ShakeCamera(CameraShaker.SHAKE_TYPE_RECOIL, 0.2f);
        }
    }
    #endregion

    private IEnumerator RemoveBerserkBuff(PlayerAttribute playerAttribute, float duration, float attackIncrease, float speedIncrease)
    {
        yield return new WaitForSeconds(duration);
        playerAttribute.SetStat(Stat.Damage, -attackIncrease);
        playerAttribute.SetStat(Stat.Speed, -speedIncrease);
    }

    IEnumerator RemoveDamageReductionBuff(PlayerAttribute playerAttribute, float buffDuration)
    {
        yield return new WaitForSeconds(buffDuration);
        playerAttribute.currentDamageReduction = 0;
    }

    private IEnumerator StopAndDestroyEffect(ParticleSystem ps, GameObject effect, float duration)
    {
        yield return new WaitForSeconds(duration);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(effect, ps.main.startLifetime.constant);
    }
}
