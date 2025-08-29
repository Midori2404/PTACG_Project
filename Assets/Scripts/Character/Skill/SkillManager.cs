using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillManager : MonoBehaviour
{
    private PlayerAttribute playerAttribute;
    private PlayerController playerController;
    private List<ISkillEffect> playerSkills; // Store player existing skill
    private Dictionary<ISkillEffect, float> skillCooldowns = new Dictionary<ISkillEffect, float>();

    public PhotonView photonView;

    void Start()
    {
        photonView = GetComponent<PhotonView>();
        playerAttribute = GetComponent<PlayerAttribute>();
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (GetAvailableSkill())
            {
                foreach (var skill in playerSkills)
                {
                    if (skill is Dash && playerAttribute.GetPlayerSkillUpgrade()[0].isUnlocked == true)
                    {
                        ActivateSkill(playerSkills.IndexOf(skill));
                        break;
                    }
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            if (GetAvailableSkill())
            {
                foreach (var skill in playerSkills)
                {
                    if (skill is PrimarySkill)
                    {
                        ActivateSkill(playerSkills.IndexOf(skill));
                        break;
                    }
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            if (GetAvailableSkill())
            {
                foreach (var skill in playerSkills)
                {
                    if (skill is SecondarySkill)
                    {
                        ActivateSkill(playerSkills.IndexOf(skill));
                        break;
                    }
                }
            }
        }
    }

    public void ActivateSkill(int skillIndex)
    {
        if (!photonView.IsMine) return;
        ISkillEffect skill = playerSkills[skillIndex];

        if (skillCooldowns.ContainsKey(skill) && skillCooldowns[skill] > Time.time)
        {
            Debug.Log(skill.GetType().Name + " is on cooldown! Time left: " + (skillCooldowns[skill] - Time.time) + "s");
            return; // Stop if the skill is on cooldown
        }

        // Activate the skill
        skill.ApplyEffect(gameObject, gameObject);
        Debug.Log("Skill Activated for " + skill.GetType().Name);

        // Set cooldown based on skill type
        float cooldown = 5f; // Default fallback cooldown
        if (skill is Dash)
        {
            cooldown = playerController.dashingCooldown;
        }
        else if (skill is PrimarySkill primarySkill)
        {
            cooldown = GetPrimaryCooldown(primarySkill);
        }
        else if (skill is SecondarySkill secondarySkill)
        {
            cooldown = GetSecondaryCooldown(secondarySkill);
        }

        skillCooldowns[skill] = Time.time + cooldown;
        Debug.Log(skill.GetType().Name + " cooldown set for " + cooldown + " seconds");

        // Get the local SkillCooldownUI component and start the cooldown animation.
        SkillCooldownUI cooldownUI = GetComponentInChildren<SkillCooldownUI>();
        if (cooldownUI != null)
        {
            if (skill is Dash)
                cooldownUI.StartCooldown(PlayerSkillUpgradeType.Dash, cooldown);
            else if (skill is PrimarySkill)
                cooldownUI.StartCooldown(PlayerSkillUpgradeType.PrimarySkill, cooldown);
            else if (skill is SecondarySkill)
                cooldownUI.StartCooldown(PlayerSkillUpgradeType.SecondarySkill, cooldown);
        }
    }




    public bool GetAvailableSkill()
    {
        playerSkills = playerAttribute.GetPlayerSkills();
        if (playerSkills.Count > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private float GetPrimaryCooldown(PrimarySkill skill)
    {
        // Get the player's primary skill upgrade choice.
        PlayerSkillUpgrade primaryUpgrade = playerAttribute.GetPlayerSkillUpgrade()
            .Find(u => u.playerSkillUpgradeType == PlayerSkillUpgradeType.PrimarySkill);

        if (playerAttribute.GetPlayerClass() == PlayerClass.Warrior)
        {
            if (primaryUpgrade != null)
            {
                if (primaryUpgrade.optionOne)
                    return skill.mendCooldown;          // Option one: Mend
                else if (primaryUpgrade.optionTwo)
                    return skill.swordsmashCooldown;    // Option two: Sword Smash
            }
        }
        else if (playerAttribute.GetPlayerClass() == PlayerClass.Archer)
        {
            if (primaryUpgrade != null)
            {
                if (primaryUpgrade.optionOne)
                    return skill.attackSpeedBuffCooldown;  // Option one: Quick Draw
                else if (primaryUpgrade.optionTwo)
                    return skill.hemorrhageCooldown;         // Option two: Hemorrhage
            }
        }

        return 5f; // Fallback cooldown
    }

    private float GetSecondaryCooldown(SecondarySkill skill)
    {
        // Get the player's secondary skill upgrade choice.
        PlayerSkillUpgrade secondaryUpgrade = playerAttribute.GetPlayerSkillUpgrade()
            .Find(u => u.playerSkillUpgradeType == PlayerSkillUpgradeType.SecondarySkill);

        if (playerAttribute.GetPlayerClass() == PlayerClass.Warrior)
        {
            if (secondaryUpgrade != null)
            {
                if (secondaryUpgrade.optionOne)
                    return skill.frontSpikeCooldown;  // Option one: Front Spike Attack
                else if (secondaryUpgrade.optionTwo)
                    return skill.berserkCooldown;     // Option two: Berserk
            }
        }
        else if (playerAttribute.GetPlayerClass() == PlayerClass.Archer)
        {
            if (secondaryUpgrade != null)
            {
                if (secondaryUpgrade.optionOne)
                    return skill.rainArrowCooldown;   // Option one: Arrow Rain
                else if (secondaryUpgrade.optionTwo)
                    return skill.shockBlastCooldown;  // Option two: Shock Blast
            }
        }

        return 5f; // Fallback cooldown
    }


    // Helper Method
    public GameObject NetworkInstantiateSkillEffect(string folderPath, GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        // Build the resource path.
        string prefabPath = folderPath + "/" + prefab.name;
        object[] instantiationData = null;
        // (Optional: you can include instantiation data if needed.)

        // Instantiate using PhotonNetwork.
        GameObject instance = PhotonNetwork.Instantiate(prefabPath, position, rotation, 0, instantiationData);

        // If a parent is provided, call an RPC on the effect to set its parent.
        if (parent != null)
        {
            PhotonView parentPV = parent.GetComponent<PhotonView>();
            PhotonView effectPV = instance.GetComponent<PhotonView>();
            if (parentPV != null && effectPV != null)
            {
                // Ensure the effect prefab has the EffectParentSync component.
                EffectParentSync eps = instance.GetComponent<EffectParentSync>();
                if (eps == null)
                {
                    // Add it if it wasn't added already.
                    eps = instance.AddComponent<EffectParentSync>();
                }
                // Now, call the RPC so that all clients set the parent.
                effectPV.RPC("RPC_SetParent", RpcTarget.All, parentPV.ViewID, parentPV.transform.localScale);
            }
        }
        return instance;
    }








}
