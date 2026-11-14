using System;
using System.Collections.Generic;
using UnityEngine;

// What a Turret fires. It flies towards its target and only deals damage when its trigger touches an enemy's
// collider (OnTriggerEnter), so it hits the first living enemy in its way, which is usually the target. If the
// target dies before it arrives it carries on in a straight line and can still hit someone else. With
// Only Hits Target it flies through everyone else instead (the shock generator's bolts).
// It is destroyed when it hits, or when it leaves the valid area: further from its turret than the turret's
// range (plus a margin), or flying for too long.
// Speed, damage and splash come from the TurretDefinition. What you see when it lands is ProjectileEffects.
// Projectiles are on the Projectile layer, which only collides with the Enemy layer.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class Projectile : MonoBehaviour
{
    // The valid area is a sphere around the turret this many times its range
    const float RangeMargin = 1.25f;

    // Removed after this many seconds whatever happens
    const float MaxLifetime = 4f;

    [Tooltip("Flies through other enemies and only hits its own target while the target is alive")]
    public bool onlyHitsTarget;

    // It touched this enemy at this point and dealt its damage
    public event Action<Enemy, Vector3> Hit;

    // Its splash damage went off at this point, with this radius
    public event Action<Vector3, float> Exploded;

    // It left the valid area without hitting anything
    public event Action Expired;

    public Enemy Target { get; private set; }

    static readonly List<Enemy> Splashed = new List<Enemy>();
    static int enemyMask;

    Rigidbody body;
    CapsuleCollider hitbox;
    float radius;

    float damage;
    float splashRadius;
    float speed;
    Vector3 origin;
    float maxDistance;
    float expireTime;

    Vector3 direction;
    Vector3 lastPosition;
    bool homing;
    bool finished;

    void Awake()
    {
        // Moved by code, never pushed around by physics
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        hitbox = GetComponent<CapsuleCollider>();
        hitbox.isTrigger = true;
        hitbox.direction = 2; // along Z, the way it flies
        radius = hitbox.radius;

        if (enemyMask == 0)
        {
            enemyMask = LayerMask.GetMask("Enemy");
        }
    }

    public void Launch(Enemy target, TurretDefinition definition, Vector3 turretPosition)
    {
        Target = target;
        damage = definition.damage;
        splashRadius = definition.maxTargets != 0 ? definition.splashRadius : 0f;
        speed = definition.projectileSpeed;
        origin = turretPosition;
        maxDistance = definition.range * RangeMargin;
        expireTime = Time.time + MaxLifetime;

        lastPosition = transform.position;
        homing = true;
        direction = transform.forward;
        if (target != null)
        {
            Vector3 toTarget = target.Center - transform.position;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                direction = toTarget.normalized;
            }
        }
        transform.rotation = Quaternion.LookRotation(direction);

        // Physics only checks where the projectile is at each step, so a fast one could jump over an enemy
        // between two steps. Stretching the trigger back over the distance of one step closes that gap.
        float step = speed * Time.fixedDeltaTime;
        hitbox.height = step + 2f * radius;
        hitbox.center = new Vector3(0f, 0f, -step * 0.5f);
    }

    void FixedUpdate()
    {
        if (finished)
        {
            return;
        }

        // Hits from the last step have already been handled, so this can't throw one away
        if ((body.position - origin).sqrMagnitude > maxDistance * maxDistance || Time.time >= expireTime)
        {
            Expired?.Invoke();
            Finish();
            return;
        }

        // Follows the target while it lives, then keeps going the same way. It also stops steering once it
        // reaches the target, so if it somehow misses it flies on instead of circling back.
        float step = speed * Time.fixedDeltaTime;
        if (homing && Target != null && Target.IsAlive)
        {
            Vector3 toTarget = Target.Center - body.position;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                direction = toTarget.normalized;
            }
            if (toTarget.sqrMagnitude <= step * step)
            {
                homing = false;
            }
        }

        lastPosition = body.position;
        body.MovePosition(lastPosition + direction * step);
        body.MoveRotation(Quaternion.LookRotation(direction));
    }

    void OnTriggerEnter(Collider other)
    {
        if (finished)
        {
            return;
        }

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null || !enemy.IsAlive)
        {
            return;
        }
        if (onlyHitsTarget && enemy != Target && Target != null && Target.IsAlive)
        {
            return;
        }

        // The spot on the enemy facing where the projectile came from
        Vector3 point = other.ClosestPoint(lastPosition);

        if (splashRadius > 0f)
        {
            Splash(enemy, point);
        }
        else
        {
            enemy.TakeDamage(damage);
        }

        Hit?.Invoke(enemy, point);
        if (splashRadius > 0f)
        {
            Exploded?.Invoke(point, splashRadius);
        }
        Finish();
    }

    // Damages the enemy that was hit and every other enemy whose collider is inside the blast
    void Splash(Enemy hit, Vector3 point)
    {
        Splashed.Clear();
        Splashed.Add(hit);

        foreach (Collider other in Physics.OverlapSphere(point, splashRadius, enemyMask, QueryTriggerInteraction.Collide))
        {
            Enemy enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null && enemy.IsAlive && !Splashed.Contains(enemy))
            {
                Splashed.Add(enemy);
            }
        }

        // Collected first because TakeDamage can kill an enemy
        foreach (Enemy enemy in Splashed)
        {
            enemy.TakeDamage(damage);
        }
        Splashed.Clear();
    }

    void Finish()
    {
        finished = true;
        hitbox.enabled = false;
        Destroy(gameObject);
    }
}
