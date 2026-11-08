using System;
using System.Collections.Generic;
using UnityEngine;

// Picks an enemy from Enemy.Active, turns the tower and gun towards it and fires a Projectile at it. All the
// numbers come from the TurretDefinition. The model only has to follow the package layout: a "tower" child
// that turns left and right with a "gun" child under it that tilts up and down. A model without them (the
// force field generator) doesn't aim, it just fires whenever something is in range.
// The turret never damages anything itself: the projectile does, when it collides with an enemy. Everything
// you see when it fires (TurretEffects) only reacts to the Fired event.
public class Turret : MonoBehaviour
{
    public TurretDefinition definition;

    [Tooltip("Leave empty to find the child called 'tower'")]
    public Transform tower;

    [Tooltip("Leave empty to find the child called 'gun'")]
    public Transform gun;

    [Tooltip("Where shots come from, used in turn. Leave empty to use the children of the gun (or of the turret, " +
             "when it has no gun) called 'Muzzle...'")]
    public Transform[] muzzles;

    // A shot left this muzzle
    public event Action<Transform> Fired;

    public Enemy Target { get; private set; }

    const float RetargetInterval = 0.25f;

    static readonly List<Enemy> Candidates = new List<Enemy>();

    Quaternion towerRest;
    Quaternion gunRest;
    float nextRetarget;
    float nextShot;
    int nextMuzzle;

    bool CanAim => tower != null || gun != null;

    void Awake()
    {
        if (tower == null)
        {
            tower = transform.Find("tower");
        }
        if (gun == null)
        {
            gun = tower != null ? tower.Find("gun") : transform.Find("gun");
        }

        if (tower != null)
        {
            towerRest = tower.localRotation;
        }
        if (gun != null)
        {
            gunRest = gun.localRotation;
        }

        if (muzzles == null || muzzles.Length == 0)
        {
            muzzles = FindMuzzles();
        }
    }

    Transform[] FindMuzzles()
    {
        List<Transform> found = new List<Transform>();
        Transform parent = gun != null ? gun : transform;
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith("Muzzle"))
            {
                found.Add(child);
            }
        }
        return found.ToArray();
    }

    // A model without muzzles fires from the gun, or from the turret itself
    Transform NextMuzzle()
    {
        if (muzzles.Length == 0)
        {
            return gun != null ? gun : transform;
        }

        Transform muzzle = muzzles[nextMuzzle % muzzles.Length];
        nextMuzzle++;
        return muzzle;
    }

    void Update()
    {
        if (definition == null)
        {
            return;
        }

        if (!IsValidTarget(Target) || Time.time >= nextRetarget)
        {
            Target = FindTarget();
            nextRetarget = Time.time + RetargetInterval;
        }

        if (Target == null)
        {
            return;
        }

        bool aimed = Aim(AimPoint(Target));
        if (aimed && Time.time >= nextShot)
        {
            Fire();
            nextShot = Time.time + definition.fireInterval;
        }
    }

    Vector3 AimPoint(Enemy enemy)
    {
        return enemy.Center;
    }

    bool IsValidTarget(Enemy enemy)
    {
        return enemy != null && enemy.IsAlive && InRange(enemy);
    }

    bool InRange(Enemy enemy)
    {
        return (AimPoint(enemy) - transform.position).sqrMagnitude <= definition.range * definition.range;
    }

    Enemy FindTarget()
    {
        Enemy best = null;
        float bestScore = float.MaxValue;

        foreach (Enemy enemy in Enemy.Active)
        {
            if (!InRange(enemy))
            {
                continue;
            }

            float score = Score(enemy);
            if (score < bestScore)
            {
                bestScore = score;
                best = enemy;
            }
        }
        return best;
    }

    // The smallest score is the best target
    float Score(Enemy enemy)
    {
        switch (definition.priority)
        {
            case TargetPriority.Last:
                return -enemy.RemainingDistance;
            case TargetPriority.Closest:
                return (enemy.transform.position - transform.position).sqrMagnitude;
            case TargetPriority.Strongest:
                return -enemy.Health;
            default:
                return enemy.RemainingDistance;
        }
    }

    // Returns true when the barrel points at the point, always true for a turret that doesn't aim
    bool Aim(Vector3 point)
    {
        if (!CanAim)
        {
            return true;
        }

        float step = definition.turnSpeed * Time.deltaTime;
        float yawError = 0f;
        float pitchError = 0f;

        if (tower != null)
        {
            // Turn around the turret's up axis only, the tower never tilts
            Vector3 flat = Vector3.ProjectOnPlane(point - tower.position, transform.up);
            if (flat.sqrMagnitude > 0.0001f)
            {
                Quaternion goal = Quaternion.LookRotation(flat, transform.up) * towerRest;
                tower.rotation = Quaternion.RotateTowards(tower.rotation, goal, step);
                yawError = Quaternion.Angle(tower.rotation, goal);
            }
        }

        if (gun != null)
        {
            // In the tower's frame the target is at some angle above or below the horizon
            Transform frame = tower != null ? tower : transform;
            Vector3 local = frame.InverseTransformPoint(point) - frame.InverseTransformPoint(gun.position);
            float horizontal = new Vector2(local.x, local.z).magnitude;
            float pitch = Mathf.Atan2(local.y, horizontal) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, 0f, definition.maxPitch);

            // The model's rest pose is already tilted, so set the pitch outright instead of adding to it
            Vector3 rest = gunRest.eulerAngles;
            Quaternion goal = Quaternion.Euler(-pitch, rest.y, rest.z);
            gun.localRotation = Quaternion.RotateTowards(gun.localRotation, goal, step);
            pitchError = Quaternion.Angle(gun.localRotation, goal);
        }

        return yawError <= definition.aimTolerance && pitchError <= definition.aimTolerance;
    }

    void Fire()
    {
        if (definition.projectile == null)
        {
            Debug.LogError($"{definition.name} has no projectile to fire", definition);
            enabled = false;
            return;
        }

        Transform muzzle = NextMuzzle();
        Fired?.Invoke(muzzle);

        // One projectile for each enemy this shot is aimed at
        PickTargets(Candidates);
        foreach (Enemy enemy in Candidates)
        {
            Vector3 toTarget = AimPoint(enemy) - muzzle.position;
            Quaternion facing = toTarget.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(toTarget) : muzzle.rotation;

            Projectile shot = Instantiate(definition.projectile, muzzle.position, facing);
            shot.Launch(enemy, definition, transform.position);
        }
        Candidates.Clear();
    }

    // Fills the list with the enemies this shot is aimed at: the target and then the next best ones
    void PickTargets(List<Enemy> into)
    {
        into.Clear();

        if (definition.maxTargets == 1)
        {
            into.Add(Target);
            return;
        }

        foreach (Enemy enemy in Enemy.Active)
        {
            if (InRange(enemy))
            {
                into.Add(enemy);
            }
        }

        if (definition.maxTargets > 0 && into.Count > definition.maxTargets)
        {
            into.Sort((a, b) => Score(a).CompareTo(Score(b)));
            into.RemoveRange(definition.maxTargets, into.Count - definition.maxTargets);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (definition == null)
        {
            return;
        }
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, definition.range);
    }
}
