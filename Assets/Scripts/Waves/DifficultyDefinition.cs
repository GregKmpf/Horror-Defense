using System;
using UnityEngine;

// One group of identical enemies inside a wave, spawned one after another
[Serializable]
public class WaveGroup
{
    public Enemy prefab;

    [Min(1)]
    public int count = 5;

    [Tooltip("Seconds between two enemies of this group")]
    [Min(0f)]
    public float spawnInterval = 1.5f;

    [Tooltip("Seconds to wait before the first enemy of this group")]
    [Min(0f)]
    public float delay = 0f;

    [Tooltip("Where the group comes from: the index of a path in the WaveManager's Paths list (0 is the first). " +
             "-1 sends each enemy down the next path in turn, so the group comes from every entrance")]
    [Min(-1)]
    public int path = -1;
}

// A wave's groups spawn in order. The next wave only starts once every enemy of this one has died or
// reached the base.
[Serializable]
public class Wave
{
    public WaveGroup[] groups;
}

// Everything that changes between difficulty levels: the waves, the stat multipliers and the time limit.
// The menu picks one and hands it to GameManager.SelectedDifficulty before loading the game scene.
[CreateAssetMenu(fileName = "Difficulty", menuName = "Horror Defense/Difficulty Definition")]
public class DifficultyDefinition : ScriptableObject
{
    public string displayName;

    [TextArea]
    public string description;

    [Header("Mission")]
    [Tooltip("Seconds to clear every wave. The player loses when it runs out")]
    [Min(1f)]
    public float timeLimit = 300f;

    [Tooltip("Seconds before the first wave, for the player to build")]
    [Min(0f)]
    public float timeBeforeFirstWave = 10f;

    [Tooltip("Seconds between a wave being cleared and the next one starting")]
    [Min(0f)]
    public float timeBetweenWaves = 8f;

    [Header("Enemies")]
    [Tooltip("Multiplies every enemy's Max Health from its EnemyDefinition")]
    [Min(0.01f)]
    public float healthMultiplier = 1f;

    [Tooltip("Multiplies every enemy's Move Speed from its EnemyDefinition")]
    [Min(0.01f)]
    public float speedMultiplier = 1f;

    public Wave[] waves;

    public int EnemyCount
    {
        get
        {
            int total = 0;
            if (waves == null)
            {
                return total;
            }
            foreach (Wave wave in waves)
            {
                if (wave?.groups == null)
                {
                    continue;
                }
                foreach (WaveGroup group in wave.groups)
                {
                    total += group?.count ?? 0;
                }
            }
            return total;
        }
    }
}
