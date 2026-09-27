using System;
using UnityEngine;

// Walks an enemy along an EnemyPath at a constant speed while keeping it on the ground. Every enemy uses
// this; how an enemy looks while moving (animations, the Mimic's legs) belongs under its Visual child.
public class EnemyMovement : MonoBehaviour
{
    [Tooltip("Height kept above the ground, e.g. the Mimic hovers at 1")]
    public float groundOffset = 0f;
    public float heightSmoothTime = 0.15f;

    [Tooltip("Degrees per second the enemy turns to face where it is walking")]
    public float turnSpeed = 720f;

    public LayerMask groundLayer;

    // Horizontal only, the ground decides the height
    public Vector3 Velocity { get; private set; }
    public float DistanceTravelled { get; private set; }
    public float RemainingDistance => pathLength - DistanceTravelled;

    public event Action ReachedEnd;

    const float RayHeight = 10f;
    const float RayLength = 50f;

    EnemyPath path;
    float speed;
    float pathLength;
    int nextWaypoint;
    float heightVelocity;

    public void Init(EnemyPath path, float speed)
    {
        this.path = path;
        this.speed = speed;
        pathLength = path.Length;
        nextWaypoint = 1;
        DistanceTravelled = 0f;
        Velocity = Vector3.zero;
        heightVelocity = 0f;

        Vector3 position = path.GetWaypoint(0);
        if (TryGetGroundHeight(position, out float height))
        {
            position.y = height;
        }
        transform.position = position;

        Vector3 direction = path.GetWaypoint(1) - position;
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        enabled = true;
    }

    void Update()
    {
        if (path == null || Time.deltaTime <= 0f)
        {
            return;
        }

        Vector3 start = transform.position;
        Vector3 position = start;
        float step = speed * Time.deltaTime;

        // Loops so a fast enemy can pass more than one waypoint in a single frame
        while (step > 0f && nextWaypoint < path.Count)
        {
            Vector3 target = path.GetWaypoint(nextWaypoint);
            target.y = position.y;
            float distance = Vector3.Distance(position, target);

            if (distance > step)
            {
                position = Vector3.MoveTowards(position, target, step);
                DistanceTravelled += step;
                break;
            }

            position = target;
            DistanceTravelled += distance;
            step -= distance;
            nextWaypoint++;
        }

        Velocity = (position - start) / Time.deltaTime;

        if (TryGetGroundHeight(position, out float height))
        {
            position.y = Mathf.SmoothDamp(position.y, height, ref heightVelocity, heightSmoothTime);
        }
        transform.position = position;

        if (Velocity != Vector3.zero)
        {
            Quaternion facing = Quaternion.LookRotation(Velocity);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, turnSpeed * Time.deltaTime);
        }

        if (nextWaypoint >= path.Count)
        {
            path = null;
            ReachedEnd?.Invoke();
        }
    }

    bool TryGetGroundHeight(Vector3 position, out float height)
    {
        if (Physics.Raycast(position + Vector3.up * RayHeight, Vector3.down, out RaycastHit hit, RayLength, groundLayer))
        {
            height = hit.point.y + groundOffset;
            return true;
        }

        height = position.y;
        return false;
    }
}
