using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SkillSelection : MonoBehaviour
{
    public static SkillSelection Instance { get; private set; }
    [Header("Kill Counts to Unlock")]
    [Header("Kill Requirements for Skill Unlocks")]
    [SerializeField] private int dashKillRequirement = 5;
    [SerializeField] private int primaryKillRequirement = 10;
    [SerializeField] private int secondaryKillRequirement = 15;


    [Header("Buttons")]
    public Button skillSelectionWindow;
    public Button skillOptionOne;
    public Button skillOptionTwo;

    public Button dashSkillTypeBtn;
    public Button primarySkillTypeBtn;
    public Button secondarySkillTypeBtn;

    [Header("Skill Name & Description Texts")]
    public TextMeshProUGUI currentSkillType;
    public TextMeshProUGUI skillNameOne;
    public TextMeshProUGUI skillNameTwo;
    public TextMeshProUGUI skillNameOneOnDescription;
    public TextMeshProUGUI skillNameTwoOnDescription;
    public TextMeshProUGUI skillDescriptionOne;
    public TextMeshProUGUI skillDescriptionTwo;

    [Header("Skill Icon Placeholder")]
    public Image skillTypePlaceholder;
    public Image skillSpriteOptionOne;
    public Image skillSpriteOptionTwo;

    [Header("Skills Icon")]
    // Sprites are already assigned in the Inspector based on the character's chosen role.
    public Sprite dashSkillIcon;
    public Sprite dashSkillTypeOne;
    public Sprite dashSkillTypeTwo;
    public Sprite primarySkillIcon;
    public Sprite primarySkillTypeOne;
    public Sprite primarySkillTypeTwo;
    public Sprite secondarySkillIcon;
    public Sprite SecondarySkillTypeOne;
    public Sprite SecondarySkillTypeTwo;

    [Header("HUD Skills Icon")]
    public Image dashSkillPlaceholder;
    public Image primarySkillPlaceholder;
    public Image secondarySkillPlaceholder;

    [Header("References")]
    public PlayerAttribute playerAttribute;
    public List<PlayerSkillUpgrade> playerSkillUpgrade;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // When the window is clicked, refresh the display.
        skillSelectionWindow.onClick.AddListener(UpdateDisplay);

        // Upgrade option buttons trigger selection.
        skillOptionOne.onClick.AddListener(() => SelectSkillOption(skillOptionOne, skillOptionTwo));
        skillOptionTwo.onClick.AddListener(() => SelectSkillOption(skillOptionTwo, skillOptionOne));

        // Skill type buttons are always interactable for preview.
        dashSkillTypeBtn.onClick.AddListener(() => UpdateDisplay(PlayerSkillUpgradeType.Dash));
        primarySkillTypeBtn.onClick.AddListener(() => UpdateDisplay(PlayerSkillUpgradeType.PrimarySkill));
        secondarySkillTypeBtn.onClick.AddListener(() => UpdateDisplay(PlayerSkillUpgradeType.SecondarySkill));

        // Initialize top-level buttons as interactable.
        dashSkillTypeBtn.interactable = true;
        primarySkillTypeBtn.interactable = true;
        secondarySkillTypeBtn.interactable = true;

        UpdateDisplay(); // Initial update.
        UpdateDisplay(PlayerSkillUpgradeType.Dash);
    }

    // UpdateDisplay refreshes the list of upgrades (for all skill types) but does not lock preview buttons.
    public void UpdateDisplay()
    {
        playerSkillUpgrade = playerAttribute.GetPlayerSkillUpgrade();
        // All top-level skill type buttons remain interactable for preview.
        UpdateDisplay(PlayerSkillUpgradeType.Dash);
    }

    // When a skill type is selected, update the detailed UI and set the icons.
    public void UpdateDisplay(PlayerSkillUpgradeType skillType)
    {
        // Always update the skill icons.
        switch (skillType)
        {
            case PlayerSkillUpgradeType.Dash:
                skillTypePlaceholder.sprite = dashSkillIcon;
                skillSpriteOptionOne.sprite = dashSkillTypeOne;
                skillSpriteOptionTwo.sprite = dashSkillTypeTwo;
                break;
            case PlayerSkillUpgradeType.PrimarySkill:
                skillTypePlaceholder.sprite = primarySkillIcon;
                skillSpriteOptionOne.sprite = primarySkillTypeOne;
                skillSpriteOptionTwo.sprite = primarySkillTypeTwo;
                break;
            case PlayerSkillUpgradeType.SecondarySkill:
                skillTypePlaceholder.sprite = secondarySkillIcon;
                skillSpriteOptionOne.sprite = SecondarySkillTypeOne;
                skillSpriteOptionTwo.sprite = SecondarySkillTypeTwo;
                break;
        }

        // Determine the kill requirement for the current skill type.
        int requiredKillCount = 0;
        switch (skillType)
        {
            case PlayerSkillUpgradeType.Dash:
                requiredKillCount = dashKillRequirement;
                break;
            case PlayerSkillUpgradeType.PrimarySkill:
                requiredKillCount = primaryKillRequirement;
                break;
            case PlayerSkillUpgradeType.SecondarySkill:
                requiredKillCount = secondaryKillRequirement;
                break;
        }

        // Check if the kill requirement is met.
        if (EnemySpawner.Instance != null && EnemySpawner.Instance.MonsterKillCount < requiredKillCount)
        {
            currentSkillType.text = skillType.ToString() + " (Locked)";
            // Display the actual names even if locked:
            playerSkillUpgrade = playerAttribute.GetPlayerSkillUpgrade();
            PlayerSkillUpgrade skillUpgrade = playerSkillUpgrade.Find(s => s.playerSkillUpgradeType == skillType);
            if (skillUpgrade != null)
            {
                skillNameOne.text = skillUpgrade.skillNameOne;
                skillNameTwo.text = skillUpgrade.skillNameTwo;
                skillNameOneOnDescription.text = skillUpgrade.skillNameOne;
                skillNameTwoOnDescription.text = skillUpgrade.skillNameTwo;
            }
            else
            {
                skillNameOne.text = "Locked";
                skillNameTwo.text = "Locked";
                skillNameOneOnDescription.text = "Locked";
                skillNameTwoOnDescription.text = "Locked";
            }
            skillDescriptionOne.text = "Must reach " + requiredKillCount + " monster kills to unlock.";
            skillDescriptionTwo.text = "Must reach " + requiredKillCount + " monster kills to unlock.";
            skillOptionOne.interactable = false;
            skillOptionTwo.interactable = false;
            return;
        }

        // Otherwise, update the UI elements with the actual upgrade data.
        playerSkillUpgrade = playerAttribute.GetPlayerSkillUpgrade();
        PlayerSkillUpgrade skillUpgradeData = playerSkillUpgrade.Find(s => s.playerSkillUpgradeType == skillType);

        if (skillUpgradeData != null)
        {
            currentSkillType.text = skillType.ToString();
            // Use the skill names from the PlayerAttribute data.
            skillNameOne.text = skillUpgradeData.skillNameOne;
            skillNameTwo.text = skillUpgradeData.skillNameTwo;
            skillNameOneOnDescription.text = skillUpgradeData.skillNameOne;
            skillNameTwoOnDescription.text = skillUpgradeData.skillNameTwo;
            skillDescriptionOne.text = skillUpgradeData.skillDescriptionOne;
            skillDescriptionTwo.text = skillUpgradeData.skillDescriptionTwo;

            bool isSkillUnlocked = IsSkillUnlocked(skillType);
            bool isPreviousUnlocked = true;
            if (skillType != PlayerSkillUpgradeType.Dash)
            {
                isPreviousUnlocked = IsPreviousSkillUnlocked(skillType);
            }

            if (skillType == PlayerSkillUpgradeType.Dash)
            {
                skillOptionOne.interactable = !isSkillUnlocked;
                skillOptionTwo.interactable = !isSkillUnlocked;
            }
            else
            {
                skillOptionOne.interactable = !isSkillUnlocked && isPreviousUnlocked;
                skillOptionTwo.interactable = !isSkillUnlocked && isPreviousUnlocked;
            }
        }
        else
        {
            currentSkillType.text = skillType.ToString() + " (Locked)";
            skillNameOneOnDescription.text = "Locked";
            skillNameTwoOnDescription.text = "Locked";
            skillDescriptionOne.text = "Unlock previous skill first.";
            skillDescriptionTwo.text = "Unlock previous skill first.";
            skillOptionOne.interactable = false;
            skillOptionTwo.interactable = false;
        }
    }



    // Checks if the previous skill (in the tree order) is unlocked.
    private bool IsPreviousSkillUnlocked(PlayerSkillUpgradeType currentSkillType)
    {
        PlayerSkillUpgradeType previousSkillType = PlayerSkillUpgradeType.Dash;
        if (currentSkillType == PlayerSkillUpgradeType.PrimarySkill)
        {
            previousSkillType = PlayerSkillUpgradeType.Dash;
        }
        else if (currentSkillType == PlayerSkillUpgradeType.SecondarySkill)
        {
            previousSkillType = PlayerSkillUpgradeType.PrimarySkill;
        }
        return IsSkillUnlocked(previousSkillType);
    }

    // Determines whether the given skill type is unlocked (i.e. has an option chosen).
    private bool IsSkillUnlocked(PlayerSkillUpgradeType skillType)
    {
        PlayerSkillUpgrade skill = playerSkillUpgrade.Find(s => s.playerSkillUpgradeType == skillType);
        return skill != null && (skill.optionOne || skill.optionTwo);
    }

    // Called when an upgrade option is selected.
    private void SelectSkillOption(Button selected, Button other)
    {
        int requiredKillCount = 0;
        switch (GetCurrentSkillType())
        {
            case PlayerSkillUpgradeType.Dash:
                requiredKillCount = dashKillRequirement;
                break;
            case PlayerSkillUpgradeType.PrimarySkill:
                requiredKillCount = primaryKillRequirement;
                break;
            case PlayerSkillUpgradeType.SecondarySkill:
                requiredKillCount = secondaryKillRequirement;
                break;
        }

        if (EnemySpawner.Instance != null && EnemySpawner.Instance.MonsterKillCount < requiredKillCount)
        {
            Debug.Log("Not enough kills to unlock " + GetCurrentSkillType() + ". Requires " + requiredKillCount + " monster kills.");
            return;
        }

        selected.interactable = false;
        other.interactable = false;

        PlayerSkillUpgrade skillUpgrade = playerSkillUpgrade.Find(s => s.playerSkillUpgradeType == GetCurrentSkillType());
        if (skillUpgrade != null)
        {
            if (selected == skillOptionOne)
            {
                skillUpgrade.optionOne = true;
                skillUpgrade.optionTwo = false;
            }
            else if (selected == skillOptionTwo)
            {
                skillUpgrade.optionOne = false;
                skillUpgrade.optionTwo = true;
            }
            skillUpgrade.isUnlocked = true;
            UpdateHUDSkillIcon(GetCurrentSkillType(), skillUpgrade);
        }
    }


    // Updates the HUD skill icon for the given skill type based on the player's selection.
    private void UpdateHUDSkillIcon(PlayerSkillUpgradeType skillType, PlayerSkillUpgrade skillUpgrade)
    {
        switch (skillType)
        {
            case PlayerSkillUpgradeType.Dash:
                if (skillUpgrade.optionOne)
                    dashSkillPlaceholder.sprite = dashSkillTypeOne;
                else if (skillUpgrade.optionTwo)
                    dashSkillPlaceholder.sprite = dashSkillTypeTwo;
                break;
            case PlayerSkillUpgradeType.PrimarySkill:
                if (skillUpgrade.optionOne)
                    primarySkillPlaceholder.sprite = primarySkillTypeOne;
                else if (skillUpgrade.optionTwo)
                    primarySkillPlaceholder.sprite = primarySkillTypeTwo;
                break;
            case PlayerSkillUpgradeType.SecondarySkill:
                if (skillUpgrade.optionOne)
                    secondarySkillPlaceholder.sprite = SecondarySkillTypeOne;
                else if (skillUpgrade.optionTwo)
                    secondarySkillPlaceholder.sprite = SecondarySkillTypeTwo;
                break;
        }
    }

    private PlayerSkillUpgradeType GetCurrentSkillType()
    {
        return (PlayerSkillUpgradeType)System.Enum.Parse(typeof(PlayerSkillUpgradeType), currentSkillType.text.Split(' ')[0]);
    }
}
