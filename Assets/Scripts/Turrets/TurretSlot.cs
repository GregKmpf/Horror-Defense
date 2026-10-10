using System.Collections.Generic;
using UnityEngine;

// A predefined spot where a turret can be built. Place them by hand in the scene, the turret appears at
// the slot's position and facing.
public class TurretSlot : MonoBehaviour
{
    // Every slot in the loaded scenes, for the build UI to highlight and pick from
    public static readonly List<TurretSlot> All = new List<TurretSlot>();

    [Tooltip("Only for testing: builds this turret when the scene starts, without paying for it")]
    public TurretDefinition startingTurret;

    public Turret Occupant { get; private set; }
    public bool IsFree => Occupant == null;

    void OnEnable()
    {
        All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    void Start()
    {
        if (IsFree && startingTurret != null)
        {
            Build(startingTurret);
        }
    }

    // Does not charge for it, the economy decides whether the player can afford the build
    public Turret Build(TurretDefinition definition)
    {
        if (!IsFree)
        {
            Debug.LogWarning($"{name} already has a turret", this);
            return null;
        }
        if (definition == null || definition.prefab == null)
        {
            Debug.LogError($"{name} was asked to build a turret without a definition and prefab", this);
            return null;
        }

        Occupant = Instantiate(definition.prefab, transform.position, transform.rotation, transform);
        Occupant.definition = definition;
        return Occupant;
    }

    // Removes the turret and leaves the slot free
    public void Clear()
    {
        if (Occupant != null)
        {
            Destroy(Occupant.gameObject);
            Occupant = null;
        }
    }

    // Statics survive between play sessions when domain reload is turned off in the Enter Play Mode settings
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        All.Clear();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = IsFree ? new Color(0.2f, 1f, 0.4f, 0.9f) : new Color(1f, 0.3f, 0.2f, 0.9f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(new Vector3(0f, 0.05f, 0f), new Vector3(2f, 0.1f, 2f));
        Gizmos.DrawLine(Vector3.up * 0.05f, new Vector3(0f, 0.05f, 1.2f));
    }
}
