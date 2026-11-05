using System;
using UnityEngine;

// A rocket that only looks like one: the damage was already dealt when it left the turret. It flies to the
// point where the shot lands and tells whoever launched it when it gets there.
public class ProjectileVisual : MonoBehaviour
{
    public float speed = 40f;

    [Tooltip("How long the trail lingers after the rocket arrives")]
    public float trailTime = 0.3f;

    Vector3 target;
    Action arrived;
    bool flying;

    public void Launch(Vector3 target, Action arrived)
    {
        this.target = target;
        this.arrived = arrived;
        flying = true;

        Vector3 direction = target - transform.position;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    void Update()
    {
        if (!flying)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if (transform.position != target)
        {
            return;
        }

        flying = false;
        arrived?.Invoke();

        // Hide the rocket itself, the trail fades on its own before the object goes
        foreach (Renderer part in GetComponentsInChildren<MeshRenderer>())
        {
            part.enabled = false;
        }
        Destroy(gameObject, trailTime);
    }
}
