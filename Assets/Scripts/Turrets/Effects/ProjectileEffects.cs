using UnityEngine;

// What you see when a projectile lands. Like TurretEffects it only listens to events, so a projectile without
// it deals the same damage. Its trails and particles are let go when it is destroyed, so they fade out
// instead of vanishing with it.
[RequireComponent(typeof(Projectile))]
public class ProjectileEffects : MonoBehaviour
{
    [Tooltip("Spawned where it hits an enemy")]
    public GameObject impactPrefab;

    [Tooltip("Spawned where its splash damage goes off. Made for a radius of 1 and scaled to the splash radius")]
    public GameObject explosionPrefab;

    Projectile projectile;

    void Awake()
    {
        projectile = GetComponent<Projectile>();
    }

    void OnEnable()
    {
        projectile.Hit += OnHit;
        projectile.Exploded += OnExploded;
        projectile.Expired += ReleaseTrails;
    }

    void OnDisable()
    {
        projectile.Hit -= OnHit;
        projectile.Exploded -= OnExploded;
        projectile.Expired -= ReleaseTrails;
    }

    void OnHit(Enemy enemy, Vector3 point)
    {
        if (impactPrefab != null)
        {
            Instantiate(impactPrefab, point, Quaternion.identity);
        }
        ReleaseTrails();
    }

    void OnExploded(Vector3 point, float radius)
    {
        if (explosionPrefab == null)
        {
            return;
        }

        GameObject explosion = Instantiate(explosionPrefab, point, Quaternion.identity);
        explosion.transform.localScale = Vector3.one * radius;
    }

    // Unparents the trails and particles so they fade out on their own once the projectile is gone
    void ReleaseTrails()
    {
        foreach (TrailRenderer trail in GetComponentsInChildren<TrailRenderer>())
        {
            trail.transform.SetParent(null, true);
            trail.emitting = false;
            Destroy(trail.gameObject, trail.time);
        }

        foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>())
        {
            particles.transform.SetParent(null, true);
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(particles.gameObject, particles.main.startLifetime.constantMax);
        }
    }
}
