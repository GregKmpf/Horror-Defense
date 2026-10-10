using System.Collections.Generic;
using UnityEngine;

// Picks an enemy from Enemy.Active, turns the tower and gun towards it and damages it. All the numbers come
// from the TurretDefinition. The model only has to follow the package layout: a "tower" child that turns
// left and right with a "gun" child under it that tilts up and down. A model without them (the force field
// generator) doesn't aim, it just fires whenever something is in range.
public class Turret : MonoBehaviour
{
    public TurretDefinition definition;

    [Tooltip("Leave empty to find the child called 'tower'")]
    public Transform tower;

    [Tooltip("Leave empty to find the child called 'gun'")]
    public Transform gun;

    [Tooltip("Where shots come from. Leave empty to use the gun")]
    public Transform muzzle;

    [Tooltip("Height above an enemy's feet that the turret aims at")]
    public float aimHeight = 1f;

    public Enemy Target { get; private set; }

    const float RetargetInterval = 0.25f;

    static readonly List<Enemy> Candidates = new List<Enemy>();

    Quaternion towerRest;
    Quaternion gunRest;
    float nextRetarget;
    float nextShot;

    bool CanAim => tower != null || gun != null;
    Vector3 Origin => muzzle != null ? muzzle.position : gun != null ? gun.position : transform.position;

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
        return enemy.transform.position + Vector3.up * aimHeight;
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
        Vector3 origin = Origin;

        PickTargets(Candidates);
        foreach (Enemy enemy in Candidates)
        {
            if (definition.splashRadius > 0f && definition.maxTargets != 0)
            {
                Splash(enemy, origin);
            }
            else
            {
                Hit(enemy, origin);
            }
        }
        Candidates.Clear();
    }

    // Fills the list with the enemies this shot hits: the target and then the next best ones
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

    void Hit(Enemy enemy, Vector3 origin)
    {
        Debug.DrawLine(origin, AimPoint(enemy), Color.yellow, 0.1f);
        enemy.TakeDamage(definition.damage);
    }

    void Splash(Enemy centre, Vector3 origin)
    {
        Vector3 point = centre.transform.position;
        Debug.DrawLine(origin, AimPoint(centre), Color.red, 0.1f);

        // TakeDamage can kill an enemy and remove it from Active, so go over a copy
        List<Enemy> nearby = new List<Enemy>(Enemy.Active);
        float radiusSqr = definition.splashRadius * definition.splashRadius;
        foreach (Enemy enemy in nearby)
        {
            if ((enemy.transform.position - point).sqrMagnitude <= radiusSqr)
            {
                enemy.TakeDamage(definition.damage);
            }
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
