using System.Collections.Generic;
using UnityEngine;

// Everything you see when a turret fires. It only listens to the Turret's events and never changes damage or
// targeting, so a turret without it works the same, it is just invisible. What to spawn comes from the
// TurretDefinition.
[RequireComponent(typeof(Turret))]
public class TurretEffects : MonoBehaviour
{
    // One flash per muzzle, made the first time it fires and played again for every shot after
    readonly Dictionary<Transform, ParticleSystem> flashes = new Dictionary<Transform, ParticleSystem>();

    Turret turret;
    Transform muzzle;

    TurretDefinition Definition => turret.definition;

    void Awake()
    {
        turret = GetComponent<Turret>();
    }

    void OnEnable()
    {
        turret.Fired += OnFired;
        turret.Hit += OnHit;
        turret.Exploded += OnExploded;
    }

    void OnDisable()
    {
        turret.Fired -= OnFired;
        turret.Hit -= OnHit;
        turret.Exploded -= OnExploded;
    }

    void OnFired(Transform from)
    {
        muzzle = from;

        if (Definition.muzzleFlashPrefab != null)
        {
            if (!flashes.TryGetValue(from, out ParticleSystem flash))
            {
                flash = Instantiate(Definition.muzzleFlashPrefab, from);
                flash.transform.localPosition = Vector3.zero;
                flash.transform.localRotation = Quaternion.identity;
                flashes.Add(from, flash);
            }
            flash.Play(true);
        }

        if (Definition.pulsePrefab != null)
        {
            PulseRing ring = Instantiate(Definition.pulsePrefab);
            ring.Setup(turret.transform.position, Definition.range);
        }
    }

    void OnHit(Enemy enemy, Vector3 point)
    {
        if (Definition.tracerPrefab != null)
        {
            Vector3 start = muzzle != null ? muzzle.position : turret.transform.position + Vector3.up;
            LineEffect line = Instantiate(Definition.tracerPrefab);
            line.Show(start, point);
        }

        if (Definition.impactPrefab != null)
        {
            Instantiate(Definition.impactPrefab, point, Quaternion.identity);
        }
    }

    void OnExploded(Vector3 point, float radius)
    {
        // The rocket's flight is only for show, the explosion waits for it to land
        if (Definition.projectilePrefab != null && muzzle != null)
        {
            ProjectileVisual rocket = Instantiate(Definition.projectilePrefab, muzzle.position, muzzle.rotation);
            rocket.Launch(point, () => Explode(point, radius));
        }
        else
        {
            Explode(point, radius);
        }
    }

    void Explode(Vector3 point, float radius)
    {
        if (Definition.explosionPrefab == null)
        {
            return;
        }

        // The prefab is made for a radius of 1
        GameObject explosion = Instantiate(Definition.explosionPrefab, point, Quaternion.identity);
        explosion.transform.localScale = Vector3.one * radius;
    }
}
