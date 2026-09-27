using UnityEngine;

public class MimicLeg : MonoBehaviour
{
    public int segments = 12;
    public float arcHeightMin = 1f;
    public float arcHeightMax = 1.3f;
    public float kneePosition = 0.3f;
    public float strandSpread = 0.6f;
    public float tipSpread = 0.4f;
    public float growTime = 0.3f;
    public float wobbleAmount = 0.3f;
    public float wobbleSpeed = 0.45f;

    public Vector3 Foot { get; private set; }

    LineRenderer[] strands;
    Vector3[] strandOffsets;
    Vector3[] tipOffsets;
    float[] strandSeeds;
    Transform body;
    float progress;
    bool retracting;
    float arcHeight;

    void Awake()
    {
        strands = GetComponentsInChildren<LineRenderer>();
        strandOffsets = new Vector3[strands.Length];
        tipOffsets = new Vector3[strands.Length];
        strandSeeds = new float[strands.Length];

        for (int s = 0; s < strands.Length; s++)
        {
            strands[s].positionCount = segments + 1;
            strandOffsets[s] = Random.insideUnitSphere * strandSpread;
            strandSeeds[s] = Random.Range(0f, 1000f);

            Vector2 tip = Random.insideUnitCircle * tipSpread;
            tipOffsets[s] = new Vector3(tip.x, 0f, tip.y);
        }

        arcHeight = Random.Range(arcHeightMin, arcHeightMax);
    }

    public void Setup(Transform body, Vector3 foot)
    {
        this.body = body;
        Foot = foot;
    }

    public void Retract()
    {
        retracting = true;
    }

    void LateUpdate()
    {
        float target = retracting ? 0f : 1f;
        progress = Mathf.MoveTowards(progress, target, Time.deltaTime / growTime);

        if (retracting && progress <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float reach = Mathf.SmoothStep(0f, 1f, progress);
        Vector3 start = body.position;
        Vector3 knee = Vector3.Lerp(start, Foot, kneePosition) + Vector3.up * arcHeight;

        for (int s = 0; s < strands.Length; s++)
        {
            Vector3 control = knee + strandOffsets[s] + Wobble(strandSeeds[s]);
            Vector3 end = Foot + tipOffsets[s];

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments * reach;
                strands[s].SetPosition(i, Bezier(start, control, end, t));
            }
        }
    }

    Vector3 Wobble(float seed)
    {
        float time = Time.time * wobbleSpeed;
        float x = Mathf.PerlinNoise(time, seed) - 0.5f;
        float y = Mathf.PerlinNoise(time, seed + 10f) - 0.5f;
        float z = Mathf.PerlinNoise(time, seed + 20f) - 0.5f;
        return new Vector3(x, y, z) * 2f * wobbleAmount;
    }

    static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        Vector3 ab = Vector3.Lerp(a, b, t);
        Vector3 bc = Vector3.Lerp(b, c, t);
        return Vector3.Lerp(ab, bc, t);
    }
}
