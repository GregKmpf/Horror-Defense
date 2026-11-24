using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    Won,
    Lost,
}

public enum GameEndReason
{
    None,
    AllWavesCleared,
    BaseDestroyed,
    TimeUp,
}

// Runs one match: starts the waves of the chosen difficulty, counts down the time limit and ends the game.
// Victory: every wave is cleared. Defeat: the base (Objetivo) is destroyed or the time runs out.
// When the game ends it freezes time (Time.timeScale = 0) and fires GameEnded, plus the Inspector events, for
// the end screens to show up. Restart and the menu put the time scale back.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Set by the menu before loading the game scene. Survives the scene load because it is static
    public static DifficultyDefinition SelectedDifficulty;

    [Tooltip("Used when the scene is played directly, without going through the menu")]
    public DifficultyDefinition defaultDifficulty;

    [Tooltip("Leave empty to use the WaveManager on this object or in the scene")]
    public WaveManager waves;

    [Tooltip("Leave empty to use Objetivo.Instancia")]
    public Objetivo baseToDefend;

    [Tooltip("Scene loaded by GoToMenu. Leave empty while there is no menu")]
    public string menuScene = "";

    [Header("Events")]
    public UnityEvent onVictory;
    public UnityEvent onDefeat;

    public event Action<GameState, GameEndReason> GameEnded;

    public DifficultyDefinition Difficulty { get; private set; }
    public GameState State { get; private set; } = GameState.Playing;
    public GameEndReason EndReason { get; private set; } = GameEndReason.None;
    public bool IsPlaying => State == GameState.Playing;

    public float TimeRemaining { get; private set; }
    public float TimeElapsed { get; private set; }

    // For the end screen and the ranking
    public int EnemiesKilled { get; private set; }
    public int EnemiesReachedBase { get; private set; }

    void Awake()
    {
        Instance = this;
        GameSpeed.Reset();

        if (waves == null)
        {
            waves = GetComponent<WaveManager>();
        }
        if (waves == null)
        {
            waves = FindAnyObjectByType<WaveManager>();
        }
    }

    void OnEnable()
    {
        Enemy.AnyDied += CountKill;
        Enemy.AnyReachedBase += CountLeak;
    }

    void OnDisable()
    {
        Enemy.AnyDied -= CountKill;
        Enemy.AnyReachedBase -= CountLeak;
    }

    // In Start rather than Awake so Objetivo.Instancia is already set
    void Start()
    {
        if (baseToDefend == null)
        {
            baseToDefend = Objetivo.Instancia;
        }

        Difficulty = SelectedDifficulty != null ? SelectedDifficulty : defaultDifficulty;
        if (Difficulty == null)
        {
            Debug.LogError($"{name} has no difficulty: set Default Difficulty or start from the menu", this);
            enabled = false;
            return;
        }
        if (waves == null)
        {
            Debug.LogError($"{name} can't find a WaveManager", this);
            enabled = false;
            return;
        }

        if (baseToDefend != null)
        {
            baseToDefend.aoSerDestruido.AddListener(OnBaseDestroyed);
        }
        else
        {
            Debug.LogWarning($"{name} can't find the Objetivo, the base can't be destroyed", this);
        }
        waves.AllWavesCleared += OnAllWavesCleared;

        State = GameState.Playing;
        EndReason = GameEndReason.None;
        TimeRemaining = Difficulty.timeLimit;
        TimeElapsed = 0f;
        waves.Begin(Difficulty);
    }

    void OnDestroy()
    {
        if (baseToDefend != null)
        {
            baseToDefend.aoSerDestruido.RemoveListener(OnBaseDestroyed);
        }
        if (waves != null)
        {
            waves.AllWavesCleared -= OnAllWavesCleared;
        }
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (!IsPlaying)
        {
            return;
        }

        TimeElapsed += Time.deltaTime;
        TimeRemaining = Mathf.Max(TimeRemaining - Time.deltaTime, 0f);
        if (TimeRemaining <= 0f)
        {
            End(GameState.Lost, GameEndReason.TimeUp);
        }
    }

    void OnAllWavesCleared()
    {
        End(GameState.Won, GameEndReason.AllWavesCleared);
    }

    void OnBaseDestroyed()
    {
        End(GameState.Lost, GameEndReason.BaseDestroyed);
    }

    void CountKill(Enemy enemy)
    {
        if (IsPlaying)
        {
            EnemiesKilled++;
        }
    }

    void CountLeak(Enemy enemy)
    {
        if (IsPlaying)
        {
            EnemiesReachedBase++;
        }
    }

    void End(GameState result, GameEndReason reason)
    {
        if (!IsPlaying)
        {
            return;
        }

        State = result;
        EndReason = reason;
        waves.Stop();
        Time.timeScale = 0f;

        Debug.Log($"Game over: {result} ({reason}) in {TimeElapsed:0.0}s, " +
                  $"{EnemiesKilled} killed, {EnemiesReachedBase} reached the base");

        GameEnded?.Invoke(result, reason);
        if (result == GameState.Won)
        {
            onVictory?.Invoke();
        }
        else
        {
            onDefeat?.Invoke();
        }
    }

    // For the end screen buttons
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMenu()
    {
        if (string.IsNullOrEmpty(menuScene))
        {
            Debug.LogWarning($"{name} has no Menu Scene set", this);
            return;
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuScene);
    }

    // Statics survive between play sessions when domain reload is turned off in the Enter Play Mode settings
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
        SelectedDifficulty = null;
    }
}
