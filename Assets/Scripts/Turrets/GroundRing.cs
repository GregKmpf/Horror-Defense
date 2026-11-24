using UnityEngine;

// A circle drawn on the ground around this object: a closed LineRenderer whose points sit on the first thing on
// the Ground layer below them, so it follows slopes and bumps instead of floating or sinking. The turret slots
// use it to show where a turret can go, and the build UI to show a turret's range.
// Purely visual. It's built when the scene starts and again whenever its radius changes.
public class GroundRing : MonoBehaviour
{
    [Tooltip("A transparent material that uses vertex colours, e.g. URP Particles/Unlit. Its texture is " +
             "repeated around the ring")]
    public Material material;

    [Tooltip("Metres")]
    [SerializeField] float radius = 2f;

    [Tooltip("Metres")]
    [SerializeField] float width = 0.2f;

    [SerializeField] Color color = Color.white;

    [Tooltip("About how many metres of ring one repeat of the texture covers. It's rounded so the texture " +
             "repeats a whole number of times and has no seam")]
    public float tileLength = 0.8f;

    [Tooltip("Metres between two points of the ring. Smaller follows the ground more closely")]
    [Min(0.1f)]
    public float spacing = 0.5f;

    [Tooltip("Lifts the ring off the ground so it doesn't flicker into it")]
    public float heightAboveGround = 0.1f;

    public LayerMask groundLayer;

    static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");

    LineRenderer line;
    MaterialPropertyBlock block;
    bool visible = true;

    public float Radius
    {
        get => radius;
        set
        {
            if (!Mathf.Approximately(radius, value))
            {
                radius = value;
                Rebuild();
            }
        }
    }

    public float Width
    {
        get => width;
        set
        {
            width = value;
            if (line != null)
            {
                line.widthMultiplier = width;
            }
        }
    }

    public Color Color
    {
        get => color;
        set
        {
            color = value;
            if (line != null)
            {
                line.startColor = line.endColor = color;
            }
        }
    }

    public bool Visible
    {
        get => visible;
        set
        {
            visible = value;
            if (line != null)
            {
                line.enabled = visible;
            }
        }
    }

    void Start()
    {
        Rebuild();
    }

    // Puts the ring back on the ground around this object, e.g. after moving it
    public void Rebuild()
    {
        if (!Application.isPlaying || material == null)
        {
            return;
        }
        if (line == null)
        {
            CreateLine();
        }

        int count = Mathf.Max(12, Mathf.CeilToInt(2f * Mathf.PI * radius / spacing));
        Vector3 center = transform.position;
        Vector3[] points = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            float angle = i * 2f * Mathf.PI / count;
            points[i] = OnGround(center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
        }
        line.positionCount = count;
        line.SetPositions(points);

        // In Tile mode the texture's U grows by one per metre; scale it so it fits around a whole number of times
        float circumference = 2f * Mathf.PI * radius;
        int repeats = Mathf.Max(1, Mathf.RoundToInt(circumference / Mathf.Max(tileLength, 0.01f)));
        block.SetVector(BaseMapST, new Vector4(repeats / circumference, 1f, 0f, 0f));
        line.SetPropertyBlock(block);
    }

    void CreateLine()
    {
        GameObject child = new GameObject("Ring");
        child.transform.SetParent(transform, false);

        // With TransformZ alignment the ribbon faces the object's Z axis, so pointing it straight down lays the
        // ribbon flat on the ground. The points are in world space, so the rotation doesn't move them
        child.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        line = child.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.alignment = LineAlignment.TransformZ;
        line.textureMode = LineTextureMode.Tile;
        line.sharedMaterial = material;
        line.widthMultiplier = width;
        line.startColor = line.endColor = color;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        line.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        line.enabled = visible;
        block = new MaterialPropertyBlock();
    }

    // From high above, so it lands on the top of the bridge and not on the riverbed under it
    Vector3 OnGround(Vector3 point)
    {
        Vector3 origin = new Vector3(point.x, 1000f, point.z);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2000f, groundLayer))
        {
            point.y = hit.point.y;
        }
        point.y += heightAboveGround;
        return point;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = color;
        Vector3 center = transform.position;
        Vector3 previous = center + Vector3.right * radius;
        for (int i = 1; i <= 32; i++)
        {
            float angle = i * 2f * Mathf.PI / 32;
            Vector3 next = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
