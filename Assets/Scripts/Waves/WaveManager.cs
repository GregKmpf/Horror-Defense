using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Spawns the waves of a DifficultyDefinition along an EnemyPath. It doesn't decide when the game is won or
// lost: GameManager calls Begin, listens to AllWavesCleared and calls Stop when the game ends.
// A wave is cleared once every enemy it spawned has died or reached the base, then the next one starts after
// the difficulty's Time Between Waves (or right away, with StartNextWaveNow).
// Enemies placed by hand in the scene aren't counted, so remove test enemies from the scene before playing.
public class WaveManager : MonoBehaviour
{
    [Tooltip("The route spawned enemies walk. Leave empty to use the first EnemyPath in the scene")]
    public EnemyPath path;

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

    readonly HashSet<Enemy> alive = new HashSet<Enemy>();
    Coroutine routine;
    bool skipWait;

    void Awake()
    {
        if (path == null)
        {
            path = FindAnyObjectByType<EnemyPath>();
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
        if (path == null || path.Count < 2)
        {
            Debug.LogError($"{name} needs an EnemyPath with at least two waypoints", this);
            return;
        }

        Stop();
        Difficulty = difficulty;
        CurrentWave = 0;
        alive.Clear();
        routine = StartCoroutine(Run());
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
                Spawn(group.prefab);
                EnemiesToSpawn--;

                if (n < group.count - 1 && group.spawnInterval > 0f)
                {
                    yield return new WaitForSeconds(group.spawnInterval);
                }
            }
        }

        EnemiesToSpawn = 0;
    }

    void Spawn(Enemy prefab)
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
