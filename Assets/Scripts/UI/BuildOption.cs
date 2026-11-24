using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One turret in the BuildMenu's list: its picture, name and cost. It greys out when the player can't afford it.
// Pointing at it shows the turret's description and range; clicking it builds the turret.
public class BuildOption : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Button button;
    public Image icon;
    public TMP_Text nameLabel;
    public TMP_Text costLabel;

    public Color affordableCost = new Color(0.95f, 0.8f, 0.45f);
    public Color unaffordableCost = new Color(0.75f, 0.2f, 0.15f);

    public TurretDefinition Turret { get; private set; }

    public event Action<BuildOption> Clicked;
    public event Action<BuildOption> PointerEntered;
    public event Action<BuildOption> PointerExited;

    void Awake()
    {
        button.onClick.AddListener(() => Clicked?.Invoke(this));
    }

    public void Show(TurretDefinition turret)
    {
        Turret = turret;
        nameLabel.text = string.IsNullOrEmpty(turret.displayName) ? turret.name : turret.displayName;
        costLabel.text = UIFormat.Money(turret.cost);
        icon.sprite = turret.icon;
        icon.enabled = turret.icon != null;
    }

    public void SetAffordable(bool affordable)
    {
        button.interactable = affordable;
        costLabel.color = affordable ? affordableCost : unaffordableCost;
        icon.color = affordable ? Color.white : new Color(1f, 1f, 1f, 0.35f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEntered?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PointerExited?.Invoke(this);
    }
}
