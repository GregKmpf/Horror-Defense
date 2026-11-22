using UnityEngine;

// Makes a light flicker like an old bulb or a lantern: a constant wobble in brightness, plus the odd moment
// where it almost goes out. Can also blink on and off at a steady rate, for warning lights like the radio
// tower's. Runs on unscaled time, so the lights keep going while the game is paused or over.
[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    public enum Pattern
    {
        Flicker,
        Blink,
    }

    public Pattern pattern = Pattern.Flicker;

    [Header("Flicker")]
    [Tooltip("How much the brightness wobbles, as a fraction of the light's intensity")]
    [Range(0f, 1f)] public float wobble = 0.15f;
    [Tooltip("How fast the brightness wobbles")]
    public float wobbleSpeed = 4f;
    [Tooltip("Average seconds between the moments where the light almost goes out, 0 for never")]
    public float dropoutInterval = 10f;
    [Tooltip("How long each of those moments lasts, in seconds")]
    public float dropoutDuration = 0.3f;

    [Header("Blink")]
    [Tooltip("Seconds for one full on and off cycle")]
    public float blinkPeriod = 2f;
    [Tooltip("Fraction of the cycle the light stays on")]
    [Range(0f, 1f)] public float blinkOnFraction = 0.4f;

    Light lamp;
    float baseIntensity;
    float seed;
    float nextDropout;
    float dropoutEnd;

    void Awake()
    {
        lamp = GetComponent<Light>();
        baseIntensity = lamp.intensity;
        // Lights placed together should not flicker in sync
        seed = Random.value * 100f;
    }

    void OnEnable()
    {
        dropoutEnd = 0f;
        ScheduleDropout(Time.unscaledTime);
    }

    void OnDisable()
    {
        lamp.intensity = baseIntensity;
    }

    void Update()
    {
        float factor = pattern == Pattern.Blink ? Blink() : Flicker();
        lamp.intensity = baseIntensity * factor;
    }

    float Flicker()
    {
        float time = Time.unscaledTime;

        // Perlin noise goes from 0 to 1, so this wobbles between 1 - wobble and 1 + wobble
        float factor = 1f + (Mathf.PerlinNoise(seed, time * wobbleSpeed) - 0.5f) * 2f * wobble;

        if (dropoutInterval > 0f && time >= nextDropout)
        {
            dropoutEnd = time + dropoutDuration;
            ScheduleDropout(dropoutEnd);
        }

        // A random level every frame makes the dropout stutter instead of just dimming
        if (time < dropoutEnd)
        {
            factor *= Random.Range(0.05f, 0.5f);
        }

        return factor;
    }

    float Blink()
    {
        float phase = Mathf.Repeat(Time.unscaledTime + seed, blinkPeriod);
        return phase < blinkPeriod * blinkOnFraction ? 1f : 0f;
    }

    void ScheduleDropout(float from)
    {
        nextDropout = from + dropoutInterval * Random.Range(0.5f, 1.5f);
    }
}
