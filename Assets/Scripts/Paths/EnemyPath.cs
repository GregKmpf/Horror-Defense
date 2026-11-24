using UnityEngine;

// A pre-determined route for enemies, from an entrance to the base. Its child objects are the waypoints,
// visited in hierarchy order. Only their horizontal position matters: enemies follow the ground on their own.
// A scene can have several, one per entrance; the WaveManager decides which one each enemy walks.
public class EnemyPath : MonoBehaviour
{
    [Tooltip("Only for the Scene view, to tell paths apart where they meet")]
    public Color gizmoColor = Color.yellow;

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
                Gizmos.color = gizmoColor;
                Gizmos.DrawLine(GetWaypoint(i - 1), point);
            }

            Gizmos.color = i == 0 ? Color.green : i == Count - 1 ? Color.red : gizmoColor;
            Gizmos.DrawSphere(point, 0.5f);
        }
    }
}
