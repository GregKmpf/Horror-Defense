using System.Collections.Generic;
using UnityEngine;

// Grows the Mimic's legs onto the ground around it as it moves. Purely visual: it sits on the Mimic's
// Visual child and follows the Enemy, which does the actual moving.
public class MimicLegs : MonoBehaviour
{
    public MimicLeg legPrefab;
    public int footholdCount = 12;
    public float minPlacementRadius = 3f;
    public float placementRadius = 6f;
    public float maxLegDistance = 9f;

    [Tooltip("New feet are placed at least this far inside Max Leg Distance, so they don't retract straight away")]
    public float placementMargin = 0.5f;

    [Tooltip("While moving, new feet are placed within this many degrees around the direction of travel")]
    [Range(0f, 360f)]
    public float placementArc = 120f;

    public float balanceThreshold = 1f;
    public float rebalanceInterval = 0.2f;

    List<MimicLeg> legs = new List<MimicLeg>();
    Enemy enemy;
    float rebalanceTimer;

    void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
        enemy.Died += RetractAll;
    }

    void OnValidate()
    {
        minPlacementRadius = Mathf.Clamp(minPlacementRadius, 0f, placementRadius);
    }

    void Update()
    {
        if (!enemy.IsAlive)
        {
            return;
        }

        for (int i = legs.Count - 1; i >= 0; i--)
        {
            if (Vector3.Distance(transform.position, legs[i].Foot) > maxLegDistance || !CanReach(legs[i].Foot))
            {
                legs[i].Retract();
                legs.RemoveAt(i);
            }
        }

        rebalanceTimer -= Time.deltaTime;
        if (rebalanceTimer <= 0f)
        {
            rebalanceTimer = rebalanceInterval;
            if (IsUnbalanced())
            {
                RetractFarthestLeg();
            }
        }

        if (legs.Count < footholdCount)
        {
            if (TryFindFoothold(out Vector3 point))
            {
                MimicLeg leg = Instantiate(legPrefab, transform);
                leg.Setup(transform, point);
                legs.Add(leg);
            }
        }
    }

    void RetractAll(Enemy _)
    {
        foreach (MimicLeg leg in legs)
        {
            leg.Retract();
        }
        legs.Clear();
    }

    Vector3 FeetCenter()
    {
        Vector3 sum = Vector3.zero;
        foreach (MimicLeg leg in legs)
        {
            sum += leg.Foot;
        }
        return sum / legs.Count;
    }

    bool IsUnbalanced()
    {
        if (legs.Count < footholdCount)
        {
            return false;
        }

        Vector3 offset = FeetCenter() - transform.position;
        offset.y = 0f;
        return offset.magnitude > balanceThreshold;
    }

    void RetractFarthestLeg()
    {
        int farthest = 0;
        for (int i = 1; i < legs.Count; i++)
        {
            float distance = Vector3.Distance(transform.position, legs[i].Foot);
            float farthestDistance = Vector3.Distance(transform.position, legs[farthest].Foot);
            if (distance > farthestDistance)
            {
                farthest = i;
            }
        }

        legs[farthest].Retract();
        legs.RemoveAt(farthest);
    }

    // How far from the body, along the ground, new feet can go. Max Leg Distance is measured in 3D from the
    // body, so the hover height eats into it: hovering at 3 m, a 9 m leg only reaches about 8.5 m along the ground.
    void GetPlacementRadii(out float min, out float max)
    {
        float reach = maxLegDistance - placementMargin;
        float height = enemy.Movement.groundOffset;
        max = Mathf.Min(placementRadius, Mathf.Sqrt(Mathf.Max(reach * reach - height * height, 0f)));
        min = Mathf.Min(minPlacementRadius, max);
    }

    // While moving, new feet go in an arc ahead, so each one stays planted while the body walks past it
    // instead of falling out of reach straight away. Standing still, they go all around.
    Vector3 GetHeading(out float halfArc)
    {
        Vector3 velocity = enemy.Movement.Velocity;
        if (velocity.sqrMagnitude > 0.01f)
        {
            halfArc = placementArc * 0.5f;
            return velocity.normalized;
        }

        halfArc = 180f;
        return transform.forward;
    }

    bool TryFindFoothold(out Vector3 point)
    {
        GetPlacementRadii(out float minRadius, out float maxRadius);
        Vector3 heading = GetHeading(out float halfArc);

        // Uniformly distributed over the part of the ring inside the arc
        float radius = Mathf.Sqrt(Random.Range(minRadius * minRadius, maxRadius * maxRadius));
        Vector3 direction = Quaternion.AngleAxis(Random.Range(-halfArc, halfArc), Vector3.up) * heading;
        Vector3 rayStart = transform.position + direction * radius + Vector3.up * 10f;

        // The distance check catches slopes, where the ground is farther below the body than the hover height
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 50f, enemy.Movement.groundLayer)
            && Vector3.Distance(transform.position, hit.point) <= maxLegDistance - placementMargin
            && CanReach(hit.point))
        {
            point = hit.point;
            return true;
        }

        point = Vector3.zero;
        return false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        foreach (MimicLeg leg in legs)
        {
            Gizmos.DrawSphere(leg.Foot, 0.15f);
        }

        if (legs.Count > 0)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(FeetCenter(), 0.25f);
        }

        if (enemy != null && enemy.Movement != null)
        {
            // Edges of the area new feet are placed in, and how far legs can reach
            GetPlacementRadii(out float minRadius, out float maxRadius);
            Vector3 heading = GetHeading(out float halfArc);
            Gizmos.color = Color.cyan;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 direction = Quaternion.AngleAxis(side * halfArc, Vector3.up) * heading;
                Gizmos.DrawLine(transform.position + direction * minRadius, transform.position + direction * maxRadius);
            }
            Gizmos.color = new Color(0f, 0.5f, 1f);
            Gizmos.DrawWireSphere(transform.position, maxLegDistance);
        }
    }

    bool CanReach(Vector3 foot)
    {
        Vector3 target = foot + Vector3.up * 0.3f;
        return !Physics.Linecast(transform.position, target, enemy.Movement.groundLayer);
    }
}
