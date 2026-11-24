using System;
using UnityEngine;

// The speed the game runs at while it's being played, 1x or faster, picked in the HUD.
// Pausing (PauseMenu) and the end of the game (GameManager) stop time with Time.timeScale = 0. Whatever lets time
// run again calls Resume, so the game goes back to the chosen speed instead of 1x.
public static class GameSpeed
{
    public static float Current { get; private set; } = 1f;

    public static event Action<float> Changed;

    // While time is stopped this only remembers the speed, so it can't unpause the game
    public static void Set(float speed)
    {
        Current = Mathf.Max(speed, 0.01f);
        if (Time.timeScale > 0f)
        {
            Time.timeScale = Current;
        }
        Changed?.Invoke(Current);
    }

    public static void Resume()
    {
        Time.timeScale = Current;
    }

    // Back to 1x, for a new game
    public static void Reset()
    {
        Current = 1f;
        Time.timeScale = 1f;
        Changed?.Invoke(Current);
    }

    // Statics survive between play sessions when domain reload is turned off in the Enter Play Mode settings
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Current = 1f;
        Changed = null;
    }
}
