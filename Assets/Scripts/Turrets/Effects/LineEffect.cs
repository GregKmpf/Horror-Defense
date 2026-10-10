using UnityEngine;

// A line between two points that fades out and removes itself. With some jitter it is an electric arc,
// without any it is a bullet trail.
[RequireComponent(typeof(LineRenderer))]
public class LineEffect : MonoBehaviour
{
    public float lifetime = 0.1f;

    [Tooltip("How far the line wanders off the straight path, 0 = straight")]
    public float jitter = 0f;

    [Tooltip("Points in the line, needs more than 2 for the jitter to show")]
    public int points = 2;

    LineRenderer line;
    Vector3 from;
    Vector3 to;
    float age;
    float startWidth;
    Color startColor;
    Color endColor;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        startWidth = line.widthMultiplier;
        startColor = line.startColor;
        endColor = line.endColor;
    }

    public void Show(Vector3 from, Vector3 to)
    {
        this.from = from;
        this.to = to;
        Rebuild();
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = age / lifetime;
        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // Additive materials fade by going dark, and the line also gets thinner
        float fade = 1f - t;
        line.widthMultiplier = startWidth * fade;
        line.startColor = startColor * fade;
        line.endColor = endColor * fade;

        if (jitter > 0f)
        {
            Rebuild();
        }
    }

    void Rebuild()
    {
        int count = Mathf.Max(points, 2);
        line.positionCount = count;

        for (int i = 0; i < count; i++)
        {
            Vector3 point = Vector3.Lerp(from, to, i / (float)(count - 1));
            bool end = i == 0 || i == count - 1;
            if (jitter > 0f && !end)
            {
                point += Random.insideUnitSphere * jitter;
            }
            line.SetPosition(i, point);
        }
    }
}
