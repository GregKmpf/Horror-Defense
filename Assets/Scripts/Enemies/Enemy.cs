using System;
using System.Collections.Generic;
using UnityEngine;

// The part of an enemy every other system talks to: the wave manager spawns it and calls Init, towers pick
// targets from Active and call TakeDamage, the economy and the base listen to AnyDied and AnyReachedBase.
// Anything specific to one enemy goes under the prefab's Visual child and only reacts to the events here.
[RequireComponent(typeof(EnemyMovement))]
public class Enemy : MonoBehaviour
{
    // Every living enemy, for towers to choose targets from
    public static readonly List<Enemy> Active = new List<Enemy>();

    // These outlive scenes, so subscribe in OnEnable and unsubscribe in OnDisable
    public static event Action<Enemy> AnyDied;
    public static event Action<Enemy> AnyReachedBase;

    public EnemyDefinition definition;

    [Tooltip("How long death visuals play before the enemy is removed")]
    public float deathDuration = 0f;

    [Tooltip("Only for enemies placed by hand in a scene, the wave manager passes the path to Init instead")]
    public EnemyPath startingPath;

    public event Action<Enemy, float> Damaged;
    public event Action<Enemy> Died;

    public float Health { get; private set; }
    public bool IsAlive { get; private set; }
    public EnemyMovement Movement { get; private set; }

    // For "target first" towers: the smallest value is the closest to the base
    public float RemainingDistance => Movement.RemainingDistance;

    void Awake()
    {
        Movement = GetComponent<EnemyMovement>();
        Movement.ReachedEnd += ReachBase;
    }

    void Start()
    {
        if (!IsAlive && startingPath != null)
        {
            Init(startingPath);
        }
    }

    public void Init(EnemyPath path)
    {
        if (definition == null)
        {
            Debug.LogError($"{name} has no EnemyDefinition assigned", this);
            return;
        }
        if (path == null || path.Count < 2)
        {
            Debug.LogError($"{name} needs an EnemyPath with at least two waypoints", this);
            return;
        }

        if (!IsAlive)
        {
            Active.Add(this);
        }
        IsAlive = true;
        Health = definition.maxHealth;
        Movement.Init(path, definition.moveSpeed);
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
        {
            return;
        }

        Health = Mathf.Max(Health - amount, 0f);
        Damaged?.Invoke(this, amount);

        if (Health <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Deactivate();
        Died?.Invoke(this);
        AnyDied?.Invoke(this);
        Destroy(gameObject, deathDuration);
    }

    void ReachBase()
    {
        if (!IsAlive)
        {
            return;
        }

        Deactivate();
        AnyReachedBase?.Invoke(this);
        Destroy(gameObject);
    }

    void Deactivate()
    {
        IsAlive = false;
        Active.Remove(this);
        Movement.enabled = false;
    }

    void OnDestroy()
    {
        Active.Remove(this);
    }

    // Statics survive between play sessions when domain reload is turned off in the Enter Play Mode settings
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Active.Clear();
        AnyDied = null;
        AnyReachedBase = null;
    }
}
