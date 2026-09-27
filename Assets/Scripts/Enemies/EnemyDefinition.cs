using UnityEngine;

// Stats for one type of enemy. Balance enemies by editing these assets, not their prefabs.
[CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Horror Defense/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{
    public string displayName;
    public Sprite icon;
    public float maxHealth = 100f;
    public float moveSpeed = 3f;

    [Tooltip("Money awarded when the enemy is killed")]
    public int bounty = 10;

    [Tooltip("Base health lost when the enemy reaches the end of its path")]
    public int baseDamage = 1;
}
