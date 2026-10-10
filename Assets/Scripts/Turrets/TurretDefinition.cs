using UnityEngine;

public enum TargetPriority
{
    [Tooltip("Closest to the base")]
    First,
    [Tooltip("Furthest from the base")]
    Last,
    Closest,
    [Tooltip("Most health left")]
    Strongest,
}

// Stats for one type of turret. Balance turrets by editing these assets, not their prefabs.
[CreateAssetMenu(fileName = "TurretDefinition", menuName = "Horror Defense/Turret Definition")]
public class TurretDefinition : ScriptableObject
{
    public string displayName;
    public Sprite icon;

    [Tooltip("The turret that gets spawned when this is built on a slot")]
    public Turret prefab;

    public int cost = 100;

    public float range = 12f;
    public float damage = 10f;

    [Tooltip("Seconds between shots")]
    public float fireInterval = 1f;

    [Tooltip("How many enemies each shot hits, 0 = every enemy in range")]
    public int maxTargets = 1;

    [Tooltip("Also damages everything this close to the target, 0 = only the target. Ignored when Max Targets is 0")]
    public float splashRadius = 0f;

    public TargetPriority priority = TargetPriority.First;

    [Header("Aiming")]
    [Tooltip("Degrees per second the tower and gun turn")]
    public float turnSpeed = 180f;

    [Tooltip("How many degrees off the target it can be and still fire")]
    public float aimTolerance = 3f;

    [Tooltip("Degrees the gun can look up. The models are made for 70")]
    public float maxPitch = 70f;

    [Header("Effects (all optional)")]
    [Tooltip("Played at the muzzle every shot. A particle system that doesn't play on awake")]
    public ParticleSystem muzzleFlashPrefab;

    [Tooltip("A line drawn from the muzzle to every enemy hit: a bullet trail, or an electric arc")]
    public LineEffect tracerPrefab;

    [Tooltip("Spawned on every enemy hit")]
    public GameObject impactPrefab;

    [Tooltip("A rocket that flies from the muzzle to the target. The damage doesn't wait for it, the explosion does")]
    public ProjectileVisual projectilePrefab;

    [Tooltip("Spawned where a splash shot lands, sized to the splash radius")]
    public GameObject explosionPrefab;

    [Tooltip("A ring that spreads from the turret to its range every shot")]
    public PulseRing pulsePrefab;
}
