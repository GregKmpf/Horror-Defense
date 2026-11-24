using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Spawns the waves of a DifficultyDefinition along the scene's EnemyPaths. It doesn't decide when the game is
// won or lost: GameManager calls Begin, listens to AllWavesCleared and calls Stop when the game ends.
// Each path starts at its own entrance. A wave group either walks one path (its Path index) or sends each
// enemy down the next path in turn, so a wave can come from one side or from all of them at once.
// A wave is cleared once every enemy it spawned has died or reached the base, then the next one starts after
// the difficulty's Time Between Waves (or right away, with StartNextWaveNow).
// Enemies placed by hand in the scene aren't counted, so remove test enemies from the scene before playing.
public class WaveManager : MonoBehaviour
{
    [Tooltip("The routes spawned enemies walk, each from its own entrance. Wave groups pick one by its index " +
             "in this list. Leave empty to use every EnemyPath in the scene, sorted by name")]
    public EnemyPath[] paths;

    [Tooltip("Optional parent for spawned enemies, to keep the hierarchy tidy")]
    public Transform enemyParent;

    // 0-based index of the wave that started, and how many waves there are
    public event Action<int, int> WaveStarted;

    // 0-based index of the wave whose enemies are all gone
    public event Action<int> WaveCleared;

    public event Action AllWavesCleared;

    public DifficultyDefinition Difficulty { get; private set; }
    public int WaveCount => Difficulty != null && Difficulty.waves != null ? Difficulty.waves.Length : 0;

    // 1-based for the HUD ("Wave 2/4"), 0 before the first wave
    public int CurrentWave { get; private set; }

    // Spawned by this manager and still alive
    public int EnemiesAlive => alive.Count;

    // Still to be spawned in the current wave
    public int EnemiesToSpawn { get; private set; }

    // True while counting down to the next wave, see TimeUntilNextWave
    public bool IsWaitingForNextWave { get; private set; }
    public float TimeUntilNextWave { get; private set; }

    public bool IsRunning => routine != null;

    public IReadOnlyList<EnemyPath> Paths => paths;

    readonly HashSet<Enemy> alive = new HashSet<Enemy>();
    Coroutine routine;
    bool skipWait;

    // The path the next "in turn" enemy walks. It carries on between groups and waves, so each one starts
    // where the last left off instead of always at the first path
    int nextPath;

    void Awake()
    {
        if (paths == null || paths.Length == 0)
        {
            paths = FindObjectsByType<EnemyPath>();
            Array.Sort(paths, (a, b) => string.CompareOrdinal(a.name, b.name));
        }
    }

    void OnEnable()
    {
        Enemy.AnyDied += Forget;
        Enemy.AnyReachedBase += Forget;
    }

    void OnDisable()
    {
        Enemy.AnyDied -= Forget;
        Enemy.AnyReachedBase -= Forget;
    }

    public void Begin(DifficultyDefinition difficulty)
    {
        if (difficulty == null || difficulty.waves == null || difficulty.waves.Length == 0)
        {
            Debug.LogError($"{name} was given a difficulty without waves", this);
            return;
        }
        if (paths == null || paths.Length == 0)
        {
            Debug.LogError($"{name} can't find an EnemyPath in the scene", this);
            return;
        }
        foreach (EnemyPath path in paths)
        {
            if (path == null || path.Count < 2)
            {
                Debug.LogError($"Every path of {name} needs at least two waypoints, check its Paths list", this);
                return;
            }
        }

        WarnAboutMissingPaths(difficulty);

        Stop();
        Difficulty = difficulty;
        CurrentWave = 0;
        nextPath = 0;
        alive.Clear();
        routine = StartCoroutine(Run());
    }

    void WarnAboutMissingPaths(DifficultyDefinition difficulty)
    {
        for (int w = 0; w < difficulty.waves.Length; w++)
        {
            WaveGroup[] groups = difficulty.waves[w]?.groups;
            if (groups == null)
            {
                continue;
            }
            for (int g = 0; g < groups.Length; g++)
            {
                if (groups[g] != null && groups[g].path >= paths.Length)
                {
                    Debug.LogWarning($"Group {g + 1} of wave {w + 1} in {difficulty.name} walks path {groups[g].path}, " +
                                     $"but {name} only has {paths.Length} (0 to {paths.Length - 1}). " +
                                     "It will take turns between every path instead", difficulty);
                }
            }
        }
    }

    // Stops spawning. Enemies already on the path stay where they are
    public void Stop()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        IsWaitingForNextWave = false;
        TimeUntilNextWave = 0f;
        EnemiesToSpawn = 0;
    }

    // Ends the countdown before the next wave (or the first one), for a "next wave" button
    public void StartNextWaveNow()
    {
        if (IsWaitingForNextWave)
        {
            skipWait = true;
        }
    }

    IEnumerator Run()
    {
        yield return Countdown(Difficulty.timeBeforeFirstWave);

        for (int i = 0; i < Difficulty.waves.Length; i++)
        {
            CurrentWave = i + 1;
            WaveStarted?.Invoke(i, Difficulty.waves.Length);

            yield return SpawnWave(Difficulty.waves[i]);

            while (alive.Count > 0)
            {
                yield return null;

                // In case something destroys an enemy without it dying or reaching the base
                alive.RemoveWhere(IsGone);
            }

            WaveCleared?.Invoke(i);

            if (i < Difficulty.waves.Length - 1)
            {
                yield return Countdown(Difficulty.timeBetweenWaves);
            }
        }

        routine = null;
        AllWavesCleared?.Invoke();
    }

    IEnumerator Countdown(float seconds)
    {
        IsWaitingForNextWave = true;
        skipWait = false;
        TimeUntilNextWave = seconds;

        while (TimeUntilNextWave > 0f && !skipWait)
        {
            yield return null;
            TimeUntilNextWave -= Time.deltaTime;
        }

        TimeUntilNextWave = 0f;
        IsWaitingForNextWave = false;
        skipWait = false;
    }

    IEnumerator SpawnWave(Wave wave)
    {
        if (wave?.groups == null)
        {
            yield break;
        }

        EnemiesToSpawn = 0;
        foreach (WaveGroup group in wave.groups)
        {
            EnemiesToSpawn += group?.count ?? 0;
        }

        foreach (WaveGroup group in wave.groups)
        {
            if (group == null)
            {
                continue;
            }
            if (group.prefab == null)
            {
                Debug.LogError($"A group in {Difficulty.name} has no enemy prefab", Difficulty);
                EnemiesToSpawn -= group.count;
                continue;
            }

            if (group.delay > 0f)
            {
                yield return new WaitForSeconds(group.delay);
            }

            for (int n = 0; n < group.count; n++)
            {
                Spawn(group.prefab, PickPath(group));
                EnemiesToSpawn--;

                if (n < group.count - 1 && group.spawnInterval > 0f)
                {
                    yield return new WaitForSeconds(group.spawnInterval);
                }
            }
        }

        EnemiesToSpawn = 0;
    }

    // The group's own path, or the next one in turn when it has none (-1) or its index is out of the list
    EnemyPath PickPath(WaveGroup group)
    {
        if (group.path >= 0 && group.path < paths.Length)
        {
            return paths[group.path];
        }

        EnemyPath path = paths[nextPath % paths.Length];
        nextPath++;
        return path;
    }

    void Spawn(Enemy prefab, EnemyPath path)
    {
        Enemy enemy = Instantiate(prefab, path.GetWaypoint(0), Quaternion.identity, enemyParent);
        enemy.Init(path, Difficulty.healthMultiplier, Difficulty.speedMultiplier);

        // Init logs why it failed (no EnemyDefinition, usually). Don't let a broken enemy hold up the wave
        if (!enemy.IsAlive)
        {
            Destroy(enemy.gameObject);
            return;
        }
        alive.Add(enemy);
    }

    void Forget(Enemy enemy)
    {
        alive.Remove(enemy);
    }

    static readonly Predicate<Enemy> IsGone = enemy => enemy == null;
}
