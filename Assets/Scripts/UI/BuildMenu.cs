using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The panel at the bottom of the screen for the slot selected with the SlotPicker.
// - A free slot lists every turret on sale with its cost, greyed out when the player can't afford it. Clicking
//   one pays for it (Economy) and builds it, and the panel switches to the new turret.
// - A slot with a turret shows what it does, lets the player choose which enemy it shoots at first, and sell it
//   for part of its cost.
// While a slot is selected a ring on the ground shows the range: of the turret under the mouse in the list, or of
// the turret on the slot.
public class BuildMenu : MonoBehaviour
{
    public SlotPicker picker;

    [Tooltip("The turrets the player can build, in the order they're listed")]
    public TurretDefinition[] turrets;

    [Header("Free slot")]
    public GameObject buildPanel;

    [Tooltip("A card in the build panel's list, copied for each turret. Left inactive")]
    public BuildOption optionTemplate;

    [Tooltip("Describes the turret under the mouse, or tells the player to pick one")]
    public TMP_Text buildInfo;

    [Header("Slot with a turret")]
    public GameObject turretPanel;
    public Image turretIcon;
    public TMP_Text turretName;
    public TMP_Text turretInfo;

    [Tooltip("One per TargetPriority, in the order of the enum: First, Last, Closest, Strongest")]
    public Button[] priorityButtons;

    public Color priorityOn = new Color(0.45f, 0.1f, 0.08f);
    public Color priorityOff = new Color(0.09f, 0.071f, 0.059f);

    public Button sellButton;
    public TMP_Text sellLabel;

    [Header("Both")]
    public Button[] closeButtons;

    [Tooltip("Shows the range on the ground. Not under the Canvas")]
    public GroundRing rangeRing;

    const string PickHint = "Escolha uma torre para construir aqui.";

    readonly List<BuildOption> options = new List<BuildOption>();
    TurretSlot slot;
    TurretDefinition previewed;

    void Awake()
    {
        optionTemplate.gameObject.SetActive(false);
        foreach (TurretDefinition turret in turrets)
        {
            if (turret == null)
            {
                continue;
            }
            BuildOption option = Instantiate(optionTemplate, optionTemplate.transform.parent);
            option.gameObject.SetActive(true);
            option.name = turret.name;
            option.Show(turret);
            option.Clicked += Build;
            option.PointerEntered += Preview;
            option.PointerExited += EndPreview;
            options.Add(option);
        }

        for (int i = 0; i < priorityButtons.Length; i++)
        {
            TargetPriority priority = (TargetPriority)i;
            priorityButtons[i].onClick.AddListener(() => SetPriority(priority));
            priorityButtons[i].GetComponentInChildren<TMP_Text>().text = UIFormat.Priority(priority);
        }

        sellButton.onClick.AddListener(Sell);
        foreach (Button close in closeButtons)
        {
            close.onClick.AddListener(() => picker.Select(null));
        }
    }

    void OnEnable()
    {
        picker.SelectionChanged += Open;
        Open(picker.Selected);
    }

    void OnDisable()
    {
        picker.SelectionChanged -= Open;
        if (Economy.Instance != null)
        {
            Economy.Instance.Changed -= RefreshPrices;
        }
    }

    // Economy might only exist after this is enabled
    void Start()
    {
        if (Economy.Instance != null)
        {
            Economy.Instance.Changed += RefreshPrices;
        }
        RefreshPrices(Money);
    }

    static int Money => Economy.Instance != null ? Economy.Instance.Money : 0;

    void Open(TurretSlot selected)
    {
        slot = selected;
        previewed = null;
        buildPanel.SetActive(slot != null && slot.IsFree);
        turretPanel.SetActive(slot != null && !slot.IsFree);

        if (slot == null)
        {
            rangeRing.Visible = false;
            return;
        }

        if (slot.IsFree)
        {
            buildInfo.text = PickHint;
            RefreshPrices(Money);
        }
        else
        {
            ShowTurret();
        }
        ShowRange();
    }

    void ShowTurret()
    {
        Turret turret = slot.Occupant;
        TurretDefinition definition = turret.definition;

        turretIcon.sprite = definition.icon;
        turretIcon.enabled = definition.icon != null;
        turretName.text = string.IsNullOrEmpty(definition.displayName) ? definition.name : definition.displayName;
        turretInfo.text = $"{definition.description}\n<color=#B8AC98>{UIFormat.Stats(definition)}</color>";

        for (int i = 0; i < priorityButtons.Length; i++)
        {
            priorityButtons[i].image.color = (int)turret.Priority == i ? priorityOn : priorityOff;
        }

        int refund = Economy.Instance != null ? Economy.Instance.SellValue(definition) : 0;
        sellLabel.text = $"VENDER  +{UIFormat.Money(refund)}";
    }

    void RefreshPrices(int money)
    {
        foreach (BuildOption option in options)
        {
            option.SetAffordable(Economy.Instance == null || Economy.Instance.CanAfford(option.Turret.cost));
        }
    }

    void Build(BuildOption option)
    {
        if (slot == null || !slot.IsFree)
        {
            return;
        }
        if (Economy.Instance != null && !Economy.Instance.TrySpend(option.Turret.cost))
        {
            return;
        }

        slot.Build(option.Turret);
        Open(slot);
    }

    void Sell()
    {
        if (slot == null || slot.IsFree)
        {
            return;
        }

        if (Economy.Instance != null)
        {
            Economy.Instance.Add(Economy.Instance.SellValue(slot.Occupant.definition));
        }
        slot.Clear();
        Open(slot);
    }

    void SetPriority(TargetPriority priority)
    {
        if (slot != null && !slot.IsFree)
        {
            slot.Occupant.Priority = priority;
            ShowTurret();
        }
    }

    void Preview(BuildOption option)
    {
        previewed = option.Turret;
        string cost = Economy.Instance == null || Economy.Instance.CanAfford(previewed.cost)
            ? ""
            : "  <color=#C0392B>Dinheiro insuficiente</color>";
        buildInfo.text = $"<b>{option.nameLabel.text}</b>  {previewed.description}{cost}\n" +
                         $"<color=#B8AC98>{UIFormat.Stats(previewed)}</color>";
        ShowRange();
    }

    void EndPreview(BuildOption option)
    {
        if (previewed == option.Turret)
        {
            previewed = null;
            buildInfo.text = PickHint;
            ShowRange();
        }
    }

    void ShowRange()
    {
        TurretDefinition shown = previewed;
        if (shown == null && slot != null && !slot.IsFree)
        {
            shown = slot.Occupant.definition;
        }

        rangeRing.Visible = slot != null && shown != null;
        if (!rangeRing.Visible)
        {
            return;
        }

        rangeRing.transform.position = slot.transform.position;
        if (Mathf.Approximately(rangeRing.Radius, shown.range))
        {
            rangeRing.Rebuild();
        }
        else
        {
            rangeRing.Radius = shown.range;
        }
    }
}
