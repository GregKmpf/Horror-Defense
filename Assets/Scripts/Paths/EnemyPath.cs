using UnityEngine;

// A pre-determined route for enemies. Its child objects are the waypoints, visited in hierarchy order.
// Only their horizontal position matters: enemies follow the ground on their own.
public class EnemyPath : MonoBehaviour
{
    public int Count => transform.childCount;

    public float Length
    {
        get
        {
            float length = 0f;
            for (int i = 1; i < Count; i++)
            {
                length += HorizontalDistance(GetWaypoint(i - 1), GetWaypoint(i));
            }
            return length;
        }
    }

    public Vector3 GetWaypoint(int index)
    {
        return transform.GetChild(index).position;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = b.y;
        return Vector3.Distance(a, b);
    }

    void OnDrawGizmos()
    {
        for (int i = 0; i < Count; i++)
        {
            Vector3 point = GetWaypoint(i);

            if (i > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(GetWaypoint(i - 1), point);
            }

            Gizmos.color = i == 0 ? Color.green : i == Count - 1 ? Color.red : Color.yellow;
            Gizmos.DrawSphere(point, 0.5f);
        }
    }
}
