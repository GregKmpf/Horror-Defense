using System.Collections.Generic;
using UnityEngine;

// Draws an EnemyPath on the ground as a faint trail, so the player sees where enemies will come from and can
// plan where to build. Purely visual: it's built once when the scene starts and nothing else reads it.
// The trail is a LineRenderer lying flat on the first thing on the Ground layer under the path, the same
// surface enemies walk on (terrain, the bridge's walkway). It fades in at the entrance and out at the base,
// and its texture slowly crawls towards the base.
[RequireComponent(typeof(EnemyPath))]
public class EnemyPathTrail : MonoBehaviour
{
    [Tooltip("A transparent material that uses vertex colours, e.g. URP Particles/Unlit. Its texture is " +
             "repeated along the trail")]
    public Material material;

    public Color color = new Color(0.85f, 0.1f, 0.06f, 0.8f);

    [Tooltip("Metres")]
    public float width = 1.6f;

    [Tooltip("Metres of trail covered by one repeat of the texture")]
    public float tileLength = 4f;

    [Tooltip("Metres per second the texture crawls towards the base. 0 keeps it still")]
    public float crawlSpeed = 0.5f;

    [Tooltip("Metres at each end over which the trail fades in and out")]
    public float fadeLength = 8f;

    [Tooltip("Metres between two points of the trail. Smaller follows bumps in the ground more closely")]
    [Min(0.1f)]
    public float spacing = 0.75f;

    [Tooltip("Lifts the trail off the ground so it doesn't flicker into it")]
    public float heightAboveGround = 0.08f;

    public LayerMask groundLayer;

    static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");

    LineRenderer line;
    MaterialPropertyBlock block;

    void Start()
    {
        EnemyPath path = GetComponent<EnemyPath>();
        if (material == null || path.Count < 2)
        {
            return;
        }

        List<Vector3> points = SamplePoints(path);

        GameObject trail = new GameObject("Trail");
        trail.transform.SetParent(transform, false);

        // With TransformZ alignment the ribbon faces the object's Z axis, so pointing it straight down lays the
        // ribbon flat on the ground. The points are in world space, so the rotation doesn't move them
        trail.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        line = trail.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.alignment = LineAlignment.TransformZ;
        line.textureMode = LineTextureMode.Tile;
        line.numCornerVertices = 4;
        line.widthMultiplier = width;
        line.sharedMaterial = material;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        line.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());
        line.colorGradient = Fade(TotalLength(points));

        block = new MaterialPropertyBlock();
        ApplyTextureOffset(0f);
    }

    void Update()
    {
        if (line != null && crawlSpeed != 0f)
        {
            ApplyTextureOffset(Time.time * crawlSpeed);
        }
    }

    // In Tile mode the texture's U grows by one per metre, so scaling it by 1 / Tile Length repeats it every
    // Tile Length metres. Moving the offset back makes the texture travel forwards, from the entrance to the base
    void ApplyTextureOffset(float metres)
    {
        float scale = 1f / Mathf.Max(tileLength, 0.01f);
        block.SetVector(BaseMapST, new Vector4(scale, 1f, -metres * scale, 0f));
        line.SetPropertyBlock(block);
    }

    List<Vector3> SamplePoints(EnemyPath path)
    {
        List<Vector3> points = new List<Vector3>();
        for (int i = 1; i < path.Count; i++)
        {
            Vector3 from = path.GetWaypoint(i - 1);
            Vector3 to = path.GetWaypoint(i);
            from.y = to.y = 0f;

            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) / spacing));

            // The first point of each segment is the last of the previous one, so it's only added once
            for (int s = i == 1 ? 0 : 1; s <= steps; s++)
            {
                points.Add(OnGround(Vector3.Lerp(from, to, s / (float)steps)));
            }
        }
        return points;
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

    static float TotalLength(List<Vector3> points)
    {
        float length = 0f;
        for (int i = 1; i < points.Count; i++)
        {
            length += Vector3.Distance(points[i - 1], points[i]);
        }
        return length;
    }

    Gradient Fade(float length)
    {
        float edge = length > 0f ? Mathf.Clamp(fadeLength / length, 0f, 0.5f) : 0f;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(color.a, edge),
                new GradientAlphaKey(color.a, 1f - edge),
                new GradientAlphaKey(0f, 1f),
            });
        return gradient;
    }
}
