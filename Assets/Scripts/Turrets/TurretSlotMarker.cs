using UnityEngine;

// Shows a TurretSlot on the ground: a dashed ring around where the turret goes. Purely visual, it only listens
// to the slot's events.
// A free slot's ring glows faintly and slowly breathes, so the player sees where they can build. Pointing at a
// slot or selecting it lights the ring up. Once a turret stands on the slot the ring hides, and only comes back
// while the slot is pointed at or selected.
[RequireComponent(typeof(GroundRing))]
public class TurretSlotMarker : MonoBehaviour
{
    [Tooltip("Leave empty to use the TurretSlot this is under")]
    public TurretSlot slot;

    [Header("Free")]
    public Color freeColor = new Color(0.95f, 0.85f, 0.6f, 0.6f);
    public float freeWidth = 0.3f;

    [Tooltip("How far the free ring's alpha swings up and down")]
    public float pulseAmount = 0.15f;

    [Tooltip("Breaths per second")]
    public float pulseSpeed = 0.4f;

    [Header("Pointed at or selected")]
    public Color hoveredColor = new Color(1f, 0.72f, 0.3f, 0.85f);
    public Color selectedColor = new Color(1f, 0.82f, 0.4f, 1f);
    public float highlightWidth = 0.45f;

    GroundRing ring;

    bool Breathing => slot.IsFree && slot.Highlight == SlotHighlight.None;

    void Awake()
    {
        ring = GetComponent<GroundRing>();
        if (slot == null)
        {
            slot = GetComponentInParent<TurretSlot>();
        }
    }

    void OnEnable()
    {
        if (slot == null)
        {
            Debug.LogError($"{name} isn't under a TurretSlot", this);
            enabled = false;
            return;
        }
        slot.Changed += Refresh;
        slot.HighlightChanged += Refresh;
        Refresh(slot);
    }

    void OnDisable()
    {
        if (slot != null)
        {
            slot.Changed -= Refresh;
            slot.HighlightChanged -= Refresh;
        }
    }

    // Unscaled, so the slots keep breathing while the game is paused or over
    void Update()
    {
        if (Breathing)
        {
            Color color = freeColor;
            color.a = Mathf.Clamp01(freeColor.a + Mathf.Sin(Time.unscaledTime * pulseSpeed * 2f * Mathf.PI) * pulseAmount);
            ring.Color = color;
        }
    }

    void Refresh(TurretSlot _)
    {
        switch (slot.Highlight)
        {
            case SlotHighlight.Selected:
                ring.Visible = true;
                ring.Color = selectedColor;
                ring.Width = highlightWidth;
                break;
            case SlotHighlight.Hovered:
                ring.Visible = true;
                ring.Color = hoveredColor;
                ring.Width = highlightWidth;
                break;
            default:
                ring.Visible = slot.IsFree;
                ring.Color = freeColor;
                ring.Width = freeWidth;
                break;
        }
    }
}
