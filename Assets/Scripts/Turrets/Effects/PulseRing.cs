using UnityEngine;

// A flat ring on the ground that spreads out from where it was spawned and fades away.
[RequireComponent(typeof(LineRenderer))]
public class PulseRing : MonoBehaviour
{
    public float lifetime = 0.45f;

    [Tooltip("Height above where it was spawned, so it isn't buried in the ground")]
    public float lift = 0.2f;

    const int Segments = 64;

    LineRenderer line;
    Vector3 centre;
    float maxRadius;
    float age;
    float startWidth;
    Color startColor;
    Color endColor;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = Segments;
        startWidth = line.widthMultiplier;
        startColor = line.startColor;
        endColor = line.endColor;
    }

    public void Setup(Vector3 centre, float maxRadius)
    {
        this.centre = centre + Vector3.up * lift;
        this.maxRadius = maxRadius;
        SetRadius(0f);
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

        // Quick at first, slowing as it reaches the edge of the range
        SetRadius(maxRadius * (1f - (1f - t) * (1f - t)));

        float fade = 1f - t;
        line.widthMultiplier = startWidth * fade;
        line.startColor = startColor * fade;
        line.endColor = endColor * fade;
    }

    void SetRadius(float radius)
    {
        for (int i = 0; i < Segments; i++)
        {
            float angle = i / (float)Segments * Mathf.PI * 2f;
            line.SetPosition(i, centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
        }
    }
}
