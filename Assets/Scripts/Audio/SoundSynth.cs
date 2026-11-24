using UnityEngine;

// Makes every sound in the game from code, so the project has no audio files to import. Each method fills a
// buffer of samples with oscillators, filtered noise and envelopes, and wraps it in an AudioClip.
// GameAudio calls these once, when the game starts, and keeps the clips.
public static class SoundSynth
{
    const int Rate = 44100;
    const int MusicRate = 22050;
    const float TwoPi = Mathf.PI * 2f;

    // Fixed seed: the sounds come out the same every run
    static System.Random random = new System.Random(1234);

    static float Noise()
    {
        return (float)(random.NextDouble() * 2.0 - 1.0);
    }

    // How much a one pole low-pass filter moves towards the input each sample
    static float LowPass(float cutoff, int rate = Rate)
    {
        return 1f - Mathf.Exp(-TwoPi * cutoff / rate);
    }

    // Scales the buffer so its loudest sample is the peak, and fades the last few milliseconds so it never clicks
    static AudioClip ToClip(string name, float[] data, float peak, int rate = Rate)
    {
        float max = 0f;
        foreach (float sample in data)
        {
            max = Mathf.Max(max, Mathf.Abs(sample));
        }
        float gain = max > 0f ? peak / max : 0f;

        int fade = Mathf.Min(data.Length, rate / 200);
        for (int i = 0; i < data.Length; i++)
        {
            data[i] *= gain;
            int fromEnd = data.Length - 1 - i;
            if (fromEnd < fade)
            {
                data[i] *= fromEnd / (float)fade;
            }
        }

        AudioClip clip = AudioClip.Create(name, data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float[] Buffer(float seconds, int rate = Rate)
    {
        return new float[Mathf.Max(1, Mathf.RoundToInt(seconds * rate))];
    }

    // ---------- Turrets ----------

    // A dry, short crack, quiet on its own because it plays ten times a second
    public static AudioClip Minigun()
    {
        float[] d = Buffer(0.08f);
        float low = 0f;
        float a = LowPass(2500f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Rate;
            float n = Noise();
            low += a * (n - low);
            float crack = (n - low * 0.6f) * Mathf.Exp(-t * 70f);
            float body = Mathf.Sin(TwoPi * 160f * t) * Mathf.Exp(-t * 55f);
            d[i] = crack * 0.8f + body * 0.6f;
        }
        return ToClip("Minigun", d, 0.9f);
    }

    // A boom: filtered noise and a sine falling in pitch
    static AudioClip Boom(string name, float seconds, float noiseCutoff, float noiseDecay, float startPitch,
        float endPitch, float pitchDrop, float bodyDecay, float crack)
    {
        float[] d = Buffer(seconds);
        float low = 0f;
        float a = LowPass(noiseCutoff);
        float phase = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Rate;
            float n = Noise();
            low += a * (n - low);
            float pitch = endPitch + (startPitch - endPitch) * Mathf.Exp(-t * pitchDrop);
            phase += TwoPi * pitch / Rate;
            d[i] = low * 2.5f * Mathf.Exp(-t * noiseDecay)
                   + Mathf.Sin(phase) * Mathf.Exp(-t * bodyDecay)
                   + n * crack * Mathf.Exp(-t * 90f);
        }
        return ToClip(name, d, 1f);
    }

    public static AudioClip Cannon()
    {
        return Boom("Cannon", 0.5f, 1400f, 11f, 150f, 45f, 7f, 8f, 0.5f);
    }

    public static AudioClip Heavy()
    {
        return Boom("Heavy", 0.3f, 2600f, 17f, 240f, 80f, 12f, 16f, 0.6f);
    }

    public static AudioClip Artillery()
    {
        return Boom("Artillery", 1.2f, 650f, 3.8f, 95f, 32f, 3f, 3.2f, 0.8f);
    }

    // A whoosh: noise whose filter opens up as it fades in and out
    public static AudioClip Rocket()
    {
        float[] d = Buffer(0.7f);
        float low = 0f;
        float phase = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Rate;
            float cutoff = Mathf.Lerp(300f, 3500f, Mathf.Clamp01(t / 0.35f));
            low += LowPass(cutoff) * (Noise() - low);
            float envelope = Mathf.Clamp01(t / 0.04f) * Mathf.Exp(-t * 4.5f);
            phase += TwoPi * (90f + t * 120f) / Rate;
            d[i] = (low * 2f + Mathf.Sin(phase) * 0.35f) * envelope;
        }
        return ToClip("Rocket", d, 0.9f);
    }

    // Electric zap: a square wave that jumps to a random pitch every few milliseconds, with crackles
    public static AudioClip Shock()
    {
        float[] d = Buffer(0.4f);
        float phase = 0f;
        float pitch = 120f;
        int jumpEvery = Rate / 90;
        float crackle = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Rate;
            if (i % jumpEvery == 0)
            {
                pitch = 70f + (float)random.NextDouble() * 380f;
            }
            phase += TwoPi * pitch / Rate;
            float square = Mathf.Sign(Mathf.Sin(phase)) * 0.5f;
            if (random.NextDouble() < 0.004)
            {
                crackle = 1f;
            }
            crackle *= 0.993f;
            float hiss = Noise() * crackle;
            float whine = Mathf.Sin(TwoPi * 1300f * t) * Mathf.Exp(-t * 20f) * 0.4f;
            d[i] = (square + hiss + whine) * Mathf.Exp(-t * 7f);
        }
        return ToClip("Shock", d, 0.8f);
    }

    // ---------- Hits and enemies ----------

    // Rumble whose filter closes as it fades
    public static AudioClip Explosion()
    {
        float[] d = Buffer(1f);
        float low = 0f;
        float phase = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Rate;
            float cutoff = 300f + 2700f * Mathf.Exp(-t * 6f);
            low += LowPass(cutoff) * (Noise() - low);
            phase += TwoPi * (30f + 35f * Mathf.Exp(-t * 5f)) / Rate;
            d[i] = (low * 3f + Mathf.Sin(phase) * 0.8f) * Mathf.Exp(-t * 4.5f) * Mathf.Clamp01(t / 0.004f);
        }
        return ToClip("Explosion", d, 1f);
    }

    // A screech that falls in pitch: frequency modulation, a fast vibrato and a breathy hiss
    public static AudioClip EnemyDeath()
    {
        float duration = 0.75f;
        float[] d = Buffer(duration);
        float carrier = 0f;
        float modulator = 0f;
        float band = 0f;
        float bandLow = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Rate;
            float pitch = 130f + 750f * Mathf.Exp(-t * 2.6f);
            pitch *= 1f + 0.06f * Mathf.Sin(TwoPi * 19f * t);
            modulator += TwoPi * pitch * 1.41f / Rate;
            float index = 3.5f * (1f - t / duration);
            carrier += TwoPi * pitch / Rate;
            float voice = Mathf.Sin(carrier + index * Mathf.Sin(modulator));

            // Crude band-pass around 1.5 kHz for the hiss
            float n = Noise();
            band += LowPass(2200f) * (n - band);
            bandLow += LowPass(900f) * (band - bandLow);
            float hiss = (band - bandLow) * 2.5f;

            float envelope = Mathf.Clamp01(t / 0.02f) * Mathf.Exp(-t * 4f);
            d[i] = (voice * 0.7f + hiss) * envelope;
        }
        return ToClip("EnemyDeath", d, 0.85f);
    }

    // A heavy metallic thud on the barn: a low thump and a few out of tune partials, like struck sheet metal
    public static AudioClip BaseHit()
    {
        float[] d = Buffer(0.9f);
        float[] partials = { 211f, 347f, 563f, 829f };
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Rate;
            float thump = Mathf.Sin(TwoPi * (55f + 60f * Mathf.Exp(-t * 18f)) * t) * Mathf.Exp(-t * 7f);
            float metal = 0f;
            for (int p = 0; p < partials.Length; p++)
            {
                metal += Mathf.Sin(TwoPi * partials[p] * t) * Mathf.Exp(-t * (5f + p * 3f)) / (p + 1);
            }
            float knock = Noise() * Mathf.Exp(-t * 120f);
            d[i] = thump * 1.2f + metal * 0.5f + knock * 0.6f;
        }
        return ToClip("BaseHit", d, 1f);
    }

    // ---------- Interface ----------

    // Two mechanical clunks, the turret locking into place
    public static AudioClip Build()
    {
        float[] d = Buffer(0.38f);
        float low = 0f;
        float a = LowPass(1800f);
        float[] starts = { 0f, 0.13f };
        float[] pitches = { 260f, 390f };
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Rate;
            low += a * (Noise() - low);
            float sample = 0f;
            for (int k = 0; k < starts.Length; k++)
            {
                float local = t - starts[k];
                if (local >= 0f)
                {
                    float envelope = Mathf.Exp(-local * 30f);
                    sample += (low * 2f + Mathf.Sin(TwoPi * pitches[k] * local)) * envelope;
                }
            }
            d[i] = sample;
        }
        return ToClip("Build", d, 0.8f);
    }

    // Two falling coin-like pings
    public static AudioClip Sell()
    {
        float[] d = Buffer(0.45f);
        float[] starts = { 0f, 0.09f };
        float[] pitches = { 1568f, 1175f };
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Rate;
            float sample = 0f;
            for (int k = 0; k < starts.Length; k++)
            {
                float local = t - starts[k];
                if (local >= 0f)
                {
                    float envelope = Mathf.Clamp01(local / 0.003f) * Mathf.Exp(-local * 11f);
                    sample += (Mathf.Sin(TwoPi * pitches[k] * local)
                               + 0.3f * Mathf.Sin(TwoPi * pitches[k] * 2.7f * local)) * envelope;
                }
            }
            d[i] = sample;
        }
        return ToClip("Sell", d, 0.6f);
    }

    public static AudioClip Click()
    {
        float[] d = Buffer(0.06f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Rate;
            d[i] = Mathf.Sin(TwoPi * 820f * t) * Mathf.Exp(-t * 80f) + Noise() * 0.3f * Mathf.Exp(-t * 300f);
        }
        return ToClip("Click", d, 0.5f);
    }

    // ---------- Game events ----------

    // A dread horn that swells, two slightly clashing low notes, and a heartbeat under it
    public static AudioClip WaveStart()
    {
        float duration = 2.6f;
        float[] d = Buffer(duration);
        float[] notes = { 55f, 58.27f, 82.41f, 110f };
        float[] levels = { 1f, 0.5f, 0.45f, 0.3f };
        float low = 0f;
        float a = LowPass(700f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Rate;
            float saw = 0f;
            for (int k = 0; k < notes.Length; k++)
            {
                float cycle = notes[k] * t;
                saw += (2f * (cycle - Mathf.Floor(cycle)) - 1f) * levels[k];
            }
            low += a * (saw - low);
            float swell = Mathf.Clamp01(t / 0.5f) * Mathf.Clamp01((duration - t) / 1.2f);

            float beat = 0f;
            float[] beats = { 0.1f, 0.35f, 1.1f, 1.35f };
            foreach (float start in beats)
            {
                float local = t - start;
                if (local >= 0f)
                {
                    beat += Mathf.Sin(TwoPi * 48f * local) * Mathf.Exp(-local * 22f);
                }
            }
            d[i] = low * swell + beat * 0.9f;
        }
        return ToClip("WaveStart", d, 0.9f);
    }

    // A bell arpeggio rising into a major chord
    public static AudioClip Victory()
    {
        float duration = 3f;
        float[] d = Buffer(duration);
        float[] notes = { 220f, 261.63f, 329.63f, 440f, 554.37f, 659.25f };
        float[] starts = { 0f, 0.22f, 0.44f, 0.66f, 0.9f, 0.9f };
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Rate;
            float sample = 0f;
            for (int k = 0; k < notes.Length; k++)
            {
                float local = t - starts[k];
                if (local >= 0f)
                {
                    sample += Bell(notes[k], local, 2.2f);
                }
            }
            d[i] = sample;
        }
        return ToClip("Victory", d, 0.8f);
    }

    // A low drone sliding down, with a tritone over it, that wavers and dies out
    public static AudioClip Defeat()
    {
        float duration = 3.5f;
        float[] d = Buffer(duration);
        float low = 0f;
        float a = LowPass(900f);
        float phaseA = 0f;
        float phaseB = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Rate;
            float slide = Mathf.Lerp(1f, 0.5f, Mathf.SmoothStep(0f, 1f, t / duration));
            phaseA += 110f * slide / Rate;
            phaseB += 155.56f * slide / Rate;
            float saw = (2f * (phaseA - Mathf.Floor(phaseA)) - 1f) + 0.7f * (2f * (phaseB - Mathf.Floor(phaseB)) - 1f);
            low += a * (saw - low);
            float tremolo = 0.75f + 0.25f * Mathf.Sin(TwoPi * 5f * t);
            float envelope = Mathf.Clamp01(t / 0.08f) * Mathf.Clamp01((duration - t) / 1.5f);
            d[i] = low * tremolo * envelope;
        }
        return ToClip("Defeat", d, 0.9f);
    }

    // A struck bell: a few inharmonic partials that decay at different speeds
    static float Bell(float pitch, float t, float decay)
    {
        return Mathf.Sin(TwoPi * pitch * t) * Mathf.Exp(-t * decay)
               + 0.5f * Mathf.Sin(TwoPi * pitch * 2.76f * t) * Mathf.Exp(-t * decay * 2f)
               + 0.25f * Mathf.Sin(TwoPi * pitch * 5.4f * t) * Mathf.Exp(-t * decay * 3.5f);
    }

    // ---------- Music ----------

    // A slow horror ambience that loops without a seam: a beating low drone, wind, and a few distant bells.
    // Every oscillator does a whole number of cycles in the loop, the bells wrap around its end, and the wind
    // crossfades its tail into its start.
    public static AudioClip Music()
    {
        const float loop = 32f;
        int length = Mathf.RoundToInt(loop * MusicRate);
        int fade = MusicRate * 2;
        float[] d = new float[length];

        // Wind: low-passed noise whose filter is swept by a slow LFO, made longer than the loop to crossfade
        float[] wind = new float[length + fade];
        float low = 0f;
        float lower = 0f;
        for (int i = 0; i < wind.Length; i++)
        {
            float t = i / (float)MusicRate;
            float sweep = 0.5f + 0.5f * Mathf.Sin(TwoPi * Fit(1f / 16f, loop) * t);
            low += LowPass(250f + 650f * sweep, MusicRate) * (Noise() - low);
            lower += LowPass(180f, MusicRate) * (low - lower);
            wind[i] = (low - lower * 0.5f) * (0.5f + 0.5f * sweep);
        }
        for (int i = 0; i < length; i++)
        {
            float w = wind[i];
            if (i < fade)
            {
                float k = i / (float)fade;
                w = wind[i] * k + wind[length + i] * (1f - k);
            }
            d[i] = w * 1.4f;
        }

        // Drone: two close notes beat against each other, with a fifth and a tritone fading in and out
        float[] pitches = { Fit(55f, loop), Fit(55.35f, loop), Fit(82.4f, loop), Fit(77.8f, loop), Fit(110f, loop) };
        float[] levels = { 0.45f, 0.4f, 0.18f, 0.12f, 0.08f };
        float swayA = Fit(1f / 8f, loop);
        float swayB = Fit(1f / 10.67f, loop);
        for (int i = 0; i < length; i++)
        {
            float t = i / (float)MusicRate;
            float drone = 0f;
            for (int k = 0; k < pitches.Length; k++)
            {
                float level = levels[k];
                if (k == 2)
                {
                    level *= 0.5f + 0.5f * Mathf.Sin(TwoPi * swayA * t);
                }
                else if (k == 3)
                {
                    level *= 0.5f + 0.5f * Mathf.Sin(TwoPi * swayB * t + 1f);
                }
                drone += Mathf.Sin(TwoPi * pitches[k] * t) * level;
            }
            d[i] += drone;
        }

        // Distant bells in A minor, with a dissonant F; written modulo the loop so they ring across its end
        float[] bellTimes = { 1.5f, 6f, 9.5f, 14f, 19f, 22.5f, 27f, 30.5f };
        float[] bellNotes = { 659.25f, 698.46f, 523.25f, 493.88f, 440f, 698.46f, 587.33f, 329.63f };
        int ring = MusicRate * 5;
        for (int b = 0; b < bellTimes.Length; b++)
        {
            int start = Mathf.RoundToInt(bellTimes[b] * MusicRate);
            for (int j = 0; j < ring; j++)
            {
                float local = j / (float)MusicRate;
                d[(start + j) % length] += Bell(bellNotes[b], local, 0.9f) * 0.12f * Mathf.Clamp01(local / 0.01f);
            }
        }

        // No end fade here: the loop has no seam to hide
        float max = 0f;
        foreach (float sample in d)
        {
            max = Mathf.Max(max, Mathf.Abs(sample));
        }
        float gain = max > 0f ? 0.7f / max : 0f;
        for (int i = 0; i < length; i++)
        {
            d[i] *= gain;
        }

        AudioClip clip = AudioClip.Create("Music", length, 1, MusicRate, false);
        clip.SetData(d, 0);
        return clip;
    }

    // The closest frequency that does a whole number of cycles in the loop, so it ends where it starts
    static float Fit(float frequency, float loop)
    {
        return Mathf.Max(1f, Mathf.Round(frequency * loop)) / loop;
    }
}
