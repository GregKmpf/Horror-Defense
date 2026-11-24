using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Lets the player point at and select TurretSlots with the mouse. Pointing at a slot lights it up, a left click
// selects it (clicking it again lets go), and a right click or a click on anything else lets go.
// The mouse is ignored over the UI, while the game is paused, and once it has ended (which also lets go).
// It only keeps the slots' highlights and says which one is selected; BuildMenu opens the panel for it.
public class SlotPicker : MonoBehaviour
{
    [Tooltip("Leave empty to use the main camera")]
    public Camera viewCamera;

    public float maxDistance = 500f;

    public TurretSlot Hovered { get; private set; }
    public TurretSlot Selected { get; private set; }

    // The selected slot changed. Null when nothing is selected
    public event Action<TurretSlot> SelectionChanged;

    int slotMask;

    static bool GameOver => GameManager.Instance != null && !GameManager.Instance.IsPlaying;

    void Awake()
    {
        slotMask = LayerMask.GetMask(TurretSlot.LayerName);
        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }
    }

    void Update()
    {
        if (GameOver)
        {
            SetHovered(null);
            Select(null);
            return;
        }

        Mouse mouse = Mouse.current;
        if (PauseMenu.Pausado || mouse == null || viewCamera == null)
        {
            SetHovered(null);
            return;
        }

        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        TurretSlot pointed = overUI ? null : SlotAt(mouse.position.ReadValue());
        SetHovered(pointed);

        if (mouse.rightButton.wasPressedThisFrame)
        {
            Select(null);
        }
        else if (mouse.leftButton.wasPressedThisFrame && !overUI)
        {
            Select(pointed == Selected ? null : pointed);
        }
    }

    public void Select(TurretSlot slot)
    {
        if (slot == Selected)
        {
            return;
        }

        TurretSlot previous = Selected;
        Selected = slot;
        Refresh(previous);
        Refresh(Selected);
        SelectionChanged?.Invoke(Selected);
    }

    TurretSlot SlotAt(Vector2 screenPoint)
    {
        Ray ray = viewCamera.ScreenPointToRay(screenPoint);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, slotMask, QueryTriggerInteraction.Collide))
        {
            return hit.collider.GetComponentInParent<TurretSlot>();
        }
        return null;
    }

    void SetHovered(TurretSlot slot)
    {
        if (slot == Hovered)
        {
            return;
        }

        TurretSlot previous = Hovered;
        Hovered = slot;
        Refresh(previous);
        Refresh(Hovered);
    }

    void Refresh(TurretSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        if (slot == Selected)
        {
            slot.SetHighlight(SlotHighlight.Selected);
        }
        else if (slot == Hovered)
        {
            slot.SetHighlight(SlotHighlight.Hovered);
        }
        else
        {
            slot.SetHighlight(SlotHighlight.None);
        }
    }
}
