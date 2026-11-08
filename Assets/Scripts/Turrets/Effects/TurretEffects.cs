using System.Collections.Generic;
using UnityEngine;

// What you see when a turret fires. It only listens to the Turret's Fired event and never changes damage or
// targeting, so a turret without it works the same. What happens when a shot lands is on the projectile
// (ProjectileEffects). What to spawn comes from the TurretDefinition.
[RequireComponent(typeof(Turret))]
public class TurretEffects : MonoBehaviour
{
    // One flash per muzzle, made the first time it fires and played again for every shot after
    readonly Dictionary<Transform, ParticleSystem> flashes = new Dictionary<Transform, ParticleSystem>();

    Turret turret;

    TurretDefinition Definition => turret.definition;

    void Awake()
    {
        turret = GetComponent<Turret>();
    }

    void OnEnable()
    {
        turret.Fired += OnFired;
    }

    void OnDisable()
    {
        turret.Fired -= OnFired;
    }

    void OnFired(Transform from)
    {
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
}
