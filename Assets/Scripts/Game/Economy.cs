using System;
using UnityEngine;

// The player's money. It starts at the difficulty's Starting Money, grows by an enemy's Bounty (EnemyDefinition)
// each time one is killed, and pays for turrets (their TurretDefinition's Cost). Selling a turret gives back part of
// its cost.
// Runs after GameManager, which picks the difficulty in its Start.
[DefaultExecutionOrder(10)]
public class Economy : MonoBehaviour
{
    public static Economy Instance { get; private set; }

    [Tooltip("Share of a turret's cost given back when it's sold")]
    [Range(0f, 1f)]
    public float sellRefund = 0.7f;

    [Tooltip("Used when there's no GameManager or difficulty, e.g. in a test scene")]
    [Min(0)]
    public int fallbackStartingMoney = 300;

    public int Money { get; private set; }

    public event Action<int> Changed;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        Enemy.AnyDied += Reward;
    }

    void OnDisable()
    {
        Enemy.AnyDied -= Reward;
    }

    void Start()
    {
        DifficultyDefinition difficulty = GameManager.Instance != null ? GameManager.Instance.Difficulty : null;
        Set(difficulty != null ? difficulty.startingMoney : fallbackStartingMoney);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool CanAfford(int amount)
    {
        return amount >= 0 && Money >= amount;
    }

    // Takes the money if there's enough, and says whether it did
    public bool TrySpend(int amount)
    {
        if (!CanAfford(amount))
        {
            return false;
        }
        Set(Money - amount);
        return true;
    }

    public void Add(int amount)
    {
        if (amount > 0)
        {
            Set(Money + amount);
        }
    }

    public int SellValue(TurretDefinition turret)
    {
        return Mathf.RoundToInt(turret.cost * sellRefund);
    }

    // Kills after the game has ended don't pay
    void Reward(Enemy enemy)
    {
        if (enemy.definition != null && (GameManager.Instance == null || GameManager.Instance.IsPlaying))
        {
            Add(enemy.definition.bounty);
        }
    }

    void Set(int money)
    {
        Money = money;
        Changed?.Invoke(Money);
    }

    // Statics survive between play sessions when domain reload is turned off in the Enter Play Mode settings
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
    }
}
