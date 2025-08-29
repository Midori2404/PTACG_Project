using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class PlayerAttribute : MonoBehaviour, IDamageable, IPunObservable
{
    [Header("Melee Attributes")]
    public float meleeBaseHealth;
    public float meleeBaseDamage;
    public float meleeBaseAttackSpeed;
    public float meleeBaseSpeed;


    [Header("Ranged Attributes")]
    public float rangedBaseHealth;
    public float rangedBaseDamage;
    public float rangedBaseAttackSpeed;
    public float rangedBaseSpeed;

    [Header("Status & Others")]
    public float currentDamageReduction;
    public float temporaryShield;
    public int monsterKillCount;
    public bool isDead;

    // References
    private FallenSpectate fallenSpectate;
    [SerializeField] private PlayerStats playerStats;
    private GameManager gameManager;
    private PlayerController playerController;
    private PhotonView photonView;

    [SerializeField] private PlayerClass playerClass;
    [SerializeField] private List<PlayerSkillUpgrade> playerSkillUpgrades; // To register player chosen skill
    [SerializeField] private List<ISkillEffect> playerSkills; // To store player existing skill

    [Header("Health UI (On-Screen)")]
    [Tooltip("Reference to the on-screen (HUD) health bar attached to the player.")]
    [SerializeField] private OnScreenHealthBarUI onScreenHealthBar;
    public SkillEffectBarManager skillEffectBarManager;


    // Start is called before the first frame update
    void Awake()
    {
        gameManager = GameManager.Instance;
        fallenSpectate = GetComponent<FallenSpectate>();
        photonView = GetComponent<PhotonView>();
        playerController = GetComponent<PlayerController>();

        if (!photonView.IsMine)
        {
            onScreenHealthBar.gameObject.SetActive(false);
        }
    }

    public void InitializeStat(string role)
    {
        if (!photonView.IsMine)
            return;

        if (role == PlayerClass.Warrior.ToString())
        {
            playerClass = PlayerClass.Warrior;
            playerStats = new PlayerStats(meleeBaseHealth, meleeBaseDamage, meleeBaseAttackSpeed, meleeBaseSpeed);
        }
        else if (role == PlayerClass.Archer.ToString())
        {
            playerClass = PlayerClass.Archer;
            playerStats = new PlayerStats(rangedBaseHealth, rangedBaseDamage, rangedBaseAttackSpeed, rangedBaseSpeed);
        }
        else
        {
            Debug.LogError("Invalid role!");
        }

        UpdateHealthUI();
    }

    public void SetStat(string stat, float value)
    {
        if (stat == Stat.Health)
        {
            playerStats.currentHealth += value;
        }
        else if (stat == Stat.Damage)
        {
            playerStats.currentDamage += value;
        }
        else if (stat == Stat.AttackSpeed)
        {
            playerStats.currentAttackSpeed += value;
        }
        else if (stat == Stat.Speed)
        {
            playerStats.currentSpeed += value;
            playerController.moveSpeed = playerStats.currentSpeed;
        }
        else
        {
            Debug.LogError("Invalid stat!");
        }
    }

    public void SetMaxHealth(float value)
    {
        rangedBaseHealth += value;
        playerStats.currentHealth += value;
        UpdateHealthUI();
    }

    [PunRPC]
    public void TakeDamage(float amount)
    {
        if (photonView.IsMine)
        {
            float actualDamage = amount * (1 - currentDamageReduction / 100);

            if (temporaryShield > 0)
            {
                if (actualDamage <= temporaryShield)
                {
                    temporaryShield -= actualDamage;
                    actualDamage = 0;
                }
                else
                {
                    actualDamage -= temporaryShield;
                    temporaryShield = 0;
                }
            }

            playerStats.currentHealth -= actualDamage;
            UpdateHealthUI();

            if (playerStats.currentHealth <= 0)
            {
                Die();
            }
        }
    }


    public void Heal(float amount)
    {
        playerStats.currentHealth += amount;

        if (playerStats.currentHealth > playerStats.baseHealth)
        {
            playerStats.currentHealth = playerStats.baseHealth;
        }

        UpdateHealthUI();
    }

    public void Die()
    {
        isDead = true;
        playerController.PlayDeadAnimation();
        fallenSpectate.EnterSpectateMode();
        gameManager.CheckAllPlayersFallen();
        Debug.Log("Player died!");
    }

    /// <summary>
    /// Update the on-screen health bar.
    /// Only update on-screen UI for the local player.
    /// </summary>
    public void UpdateHealthUI()
    {
        if (photonView.IsMine && onScreenHealthBar != null)
        {
            onScreenHealthBar.UpdateHealthBar(playerStats.currentHealth, playerStats.baseHealth);
            onScreenHealthBar.UpdateShieldBar(temporaryShield, playerStats.baseHealth); // Ensure you have a maxTemporaryShield variable set
        }
    }


    // Photon synchronization of health.
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(playerStats.currentHealth);
            stream.SendNext(playerStats.baseHealth);
        }
        else
        {
            playerStats.currentHealth = (float)stream.ReceiveNext();
            playerStats.baseHealth = (float)stream.ReceiveNext();
            UpdateHealthUI();
        }
    }

    public PlayerClass GetPlayerClass()
    {
        return playerClass;
    }

    public PlayerStats GetPlayerStats()
    {
        return playerStats;
    }

    public List<PlayerSkillUpgrade> GetPlayerSkillUpgrade()
    {
        return playerSkillUpgrades;
    }
    public List<ISkillEffect> GetPlayerSkills()
    {
        return playerSkills;
    }

}




public enum PlayerClass
{
    Warrior,
    Archer
}

public class Stat
{
    public const string Health = "Health";
    public const string Damage = "Damage";
    public const string AttackSpeed = "AttackSpeed";
    public const string Speed = "Speed";
}

[System.Serializable]
public class PlayerStats
{
    [HideInInspector] public float baseHealth;

    // Current stats (value changes during in-game progress)
    public float currentHealth;
    public float currentDamage;
    public float currentAttackSpeed;
    public float currentSpeed;

    public PlayerStats(float health, float damage, float attackSpeed, float speed)
    {
        baseHealth = health;
        currentHealth = health;
        currentDamage = damage;
        currentAttackSpeed = attackSpeed;
        currentSpeed = speed;
    }
}

public enum PlayerSkillUpgradeType
{
    Dash,
    PrimarySkill,
    SecondarySkill
}

[System.Serializable]
public class PlayerSkillUpgrade
{
    public PlayerSkillUpgradeType playerSkillUpgradeType;
    public string skillNameOne;
    public string skillNameTwo;
    public string skillDescriptionOne;
    public string skillDescriptionTwo;
    public bool isUnlocked;
    public bool optionOne;
    public bool optionTwo;
}
