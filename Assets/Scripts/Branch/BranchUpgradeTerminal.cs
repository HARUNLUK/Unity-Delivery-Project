using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class BranchUpgradeTerminal : MonoBehaviour
{
    [Header("--- VISUAL FEEDBACK (OPTIONAL) ---")]
    [Tooltip("3D text showing terminal title or status")]
    public TextMeshPro screenText;

    [Tooltip("Status light on the terminal")]
    public Light terminalLight;

    private Collider col;

    private void Awake()
    {
        col = GetComponent<Collider>();
        EnsureVisualComponents();
    }

    private void Start()
    {
        EnsureVisualComponents();
        UpdateTerminalVisuals();
    }

    private void OnEnable()
    {
        BranchManager.OnBranchUpgraded += HandleBranchUpgraded;
        BranchManager.OnBranchReset += HandleBranchReset;
        UpdateTerminalVisuals();
    }

    private void OnDisable()
    {
        BranchManager.OnBranchUpgraded -= HandleBranchUpgraded;
        BranchManager.OnBranchReset -= HandleBranchReset;
    }

    private void Update()
    {
        // Periodically refresh terminal screen in case BranchManager or money updates
        if (Time.frameCount % 45 == 0)
        {
            UpdateTerminalVisuals();
        }
    }

    public void EnsureVisualComponents()
    {
        if (screenText == null)
        {
            screenText = GetComponentInChildren<TextMeshPro>(true);
        }

        if (terminalLight == null)
        {
            terminalLight = GetComponentInChildren<Light>(true);
        }
    }

    private void HandleBranchUpgraded(int lvl, BranchTier tier)
    {
        UpdateTerminalVisuals();
    }

    private void HandleBranchReset()
    {
        UpdateTerminalVisuals();
    }

    public string GetPromptText()
    {
        if (BranchManager.Instance == null)
        {
            return "<color=#32FF64>[E] Branch Terminal</color>";
        }

        BranchTier current = BranchManager.Instance.CurrentTier;
        BranchTier next = BranchManager.Instance.NextTier;

        if (next != null)
        {
            return $"<color=#32FF64>[E] Open Branch Terminal</color> ➔ Level {next.tierLevel}: {next.tierName} (${next.upgradeCost})";
        }
        else
        {
            string tName = current != null ? current.tierName : "Maximum";
            return $"<color=#32FFFF>[E] Open Branch Terminal</color> ★ {tName} (Max Level) ★";
        }
    }

    /// <summary>
    /// Called when the player presses [E] looking at this terminal.
    /// Opens the Tablet on the Branch Office management tab!
    /// </summary>
    public void InteractTerminal()
    {
        if (CargoTabletUI.Instance != null)
        {
            CargoTabletUI.Instance.OpenTablet();
            CargoTabletUI.Instance.SwitchTab(TabletTab.BranchOffice);
            if (InteractionPromptHUD.Instance != null)
            {
                InteractionPromptHUD.Instance.ShowPrompt("<color=#32FFFF>★ BRANCH DASHBOARD OPENED ★</color>");
            }
        }
        else
        {
            // Direct upgrade fallback if tablet is absent
            TryInteractUpgrade();
        }

        UpdateTerminalVisuals();
    }

    public bool TryInteractUpgrade()
    {
        if (BranchManager.Instance == null) return false;
        bool upgraded = BranchManager.Instance.TryUpgradeBranch();
        UpdateTerminalVisuals();
        return upgraded;
    }

    public void UpdateTerminalVisuals()
    {
        EnsureVisualComponents();

        if (BranchManager.Instance == null)
        {
            if (screenText != null)
            {
                screenText.text = "<b>BRANCH OFFICE</b>\n<color=#32FFFF>Ready...</color>";
            }
            return;
        }

        BranchTier current = BranchManager.Instance.CurrentTier;
        BranchTier next = BranchManager.Instance.NextTier;

        if (screenText != null)
        {
            if (current != null && next != null)
            {
                screenText.text = $"<b>BRANCH OFFICE</b>\n<color=#32FFFF>Current: Level {current.tierLevel} ({current.tierName})</color>\n<size=75%>Next: {next.tierName}\nPrice: ${next.upgradeCost} (Level {next.requiredPlayerLevel})</size>";
            }
            else if (current != null)
            {
                screenText.text = $"<b>BRANCH OFFICE</b>\n<color=#32FF64>★ MAXIMUM LEVEL ★\n{current.tierName}</color>";
            }
        }

        if (terminalLight != null)
        {
            terminalLight.color = (next != null) ? new Color(0.2f, 0.8f, 1.0f) : new Color(0.2f, 1.0f, 0.4f);
        }
    }
}
