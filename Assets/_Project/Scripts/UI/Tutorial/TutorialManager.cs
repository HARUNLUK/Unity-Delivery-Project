using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// First-hours tutorials as small, non-blocking coach cards (plus one modal for the day-end receipt).
/// Tips appear once, when their moment comes: first parcel, first drive, first delivery, first day end,
/// affordable branch upgrade, low fuel, low vehicle condition. F1 opens a list to replay any of them.
/// The scene UI is built by Tools > Delivery Game > Cozy UI > Screens > Tutorials.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    public const string SeenPrefix = "Tutorial_Seen_";
    private const float PollInterval = 0.25f;
    private const float LowFuelRatio = 0.20f;
    private const float LowConditionRatio = 0.30f;

    [Header("--- MASTER SWITCH ---")]
    public bool tutorialsEnabled = true;

    [Header("--- COACH CARD (side, non-blocking) ---")]
    public GameObject coachRoot;
    public Image coachBadge;
    public Image coachIcon;
    public TextMeshProUGUI coachLabel;
    public TextMeshProUGUI coachTitle;
    public TextMeshProUGUI coachBody;
    public TextMeshProUGUI coachSkipHint;
    public GameObject coachTimerRoot;
    public Image coachTimerFill;

    [Header("--- MODAL (day-end receipt tip) ---")]
    public GameObject modalRoot;
    public Image modalBadge;
    public Image modalIcon;
    public TextMeshProUGUI modalTitle;
    public TextMeshProUGUI modalSubtitle;
    public Transform modalRows;      // children: Badge(Icon), Text
    public Button modalOkButton;

    [Header("--- GUIDE LIST (F1) ---")]
    public GameObject guideRoot;
    public Transform guideGrid;      // one child per tutorial: Badge(Icon), Texts/Title, Texts/Sub, Chip/Text
    public Button guideCloseButton;

    private readonly List<string> queue = new List<string>();
    private TutorialDef current;
    private int page;
    private float pageTimer;
    private bool currentIsReplay;
    private bool guideOpen;
    private bool modalReplay;
    private float pollTimer;
    private float savedTimeScale = 1f;

    /// <summary>True while the F1 list or a replayed modal owns the screen (player input must stay off).</summary>
    public bool IsBlockingOpen => guideOpen || modalReplay;

    private int escapeConsumedFrame = -1;

    /// <summary>ESC was used this frame to close a tutorial window, so the pause menu must not open with it.</summary>
    public bool EscapeConsumedThisFrame => escapeConsumedFrame == Time.frameCount;

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        HideAll();
        BindButtons();
    }

    private void OnEnable()
    {
        VanInventory.OnCargoDelivered += HandleCargoDelivered;
        DayTimeManager.OnShiftEnded += HandleShiftEnded;
        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDisable()
    {
        VanInventory.OnCargoDelivered -= HandleCargoDelivered;
        DayTimeManager.OnShiftEnded -= HandleShiftEnded;
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        if (guideOpen) CloseGuide();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void BindButtons()
    {
        if (modalOkButton != null)
        {
            modalOkButton.onClick.RemoveAllListeners();
            modalOkButton.onClick.AddListener(OnModalOk);
        }
        if (guideCloseButton != null)
        {
            guideCloseButton.onClick.RemoveAllListeners();
            guideCloseButton.onClick.AddListener(CloseGuide);
        }
        if (guideGrid != null)
        {
            for (int i = 0; i < guideGrid.childCount && i < TutorialCatalog.All.Length; i++)
            {
                Button b = guideGrid.GetChild(i).GetComponent<Button>();
                if (b == null) continue;
                string id = TutorialCatalog.All[i].id;
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => Replay(id));
            }
        }
    }

    private void HideAll()
    {
        if (coachRoot != null) coachRoot.SetActive(false);
        if (modalRoot != null) modalRoot.SetActive(false);
        if (guideRoot != null) guideRoot.SetActive(false);
    }

    // ---------------------------------------------------------------
    // Progress (PlayerPrefs)
    // ---------------------------------------------------------------

    public static bool IsSeen(string id) => PlayerPrefs.GetInt(SeenPrefix + id, 0) == 1;

    private static void MarkSeen(string id)
    {
        PlayerPrefs.SetInt(SeenPrefix + id, 1);
        PlayerPrefs.Save();
    }

    /// <summary>New game: every tutorial can appear again.</summary>
    public static void ResetProgress()
    {
        foreach (TutorialDef d in TutorialCatalog.All) PlayerPrefs.DeleteKey(SeenPrefix + d.id);
        PlayerPrefs.Save();
        if (Instance != null)
        {
            Instance.queue.Clear();
            Instance.EndCurrent();
        }
    }

    // ---------------------------------------------------------------
    // Triggering
    // ---------------------------------------------------------------

    /// <summary>Queues a tutorial once (ignored when already seen, queued or showing).</summary>
    public void Trigger(string id)
    {
        if (!tutorialsEnabled || IsSeen(id)) return;
        if (current != null && current.id == id) return;
        if (queue.Contains(id)) return;

        TutorialDef def = TutorialCatalog.Get(id);
        if (def == null) return;

        // The day-end tip belongs to a screen that is about to appear: it jumps the queue.
        if (def.kind == TutorialKind.Modal) queue.Insert(0, id);
        else queue.Add(id);
    }

    /// <summary>Plays a tutorial again on request (F1 list), whether or not it was seen.</summary>
    public void Replay(string id)
    {
        TutorialDef def = TutorialCatalog.Get(id);
        if (def == null) return;

        CloseGuide(keepCursorFree: def.kind == TutorialKind.Modal);
        queue.Remove(id);
        EndCurrent();
        StartDef(def, isReplay: true);
    }

    private void HandleCargoDelivered(CargoItem item, bool isCorrect)
    {
        Trigger(TutorialCatalog.FirstDelivery);
    }

    private void HandleShiftEnded()
    {
        Trigger(TutorialCatalog.DayEnd);
    }

    private void HandleLanguageChanged(string lang)
    {
        if (current != null) RenderPage();
        if (guideOpen) RefreshGuideRows();
    }

    private bool IsPlaying()
    {
        return GameMenuManager.Instance == null || GameMenuManager.Instance.CurrentState == GameFlowState.Playing;
    }

    private void EvaluateTriggers()
    {
        if (!tutorialsEnabled || !IsPlaying()) return;
        if (DayTimeManager.Instance != null && DayTimeManager.Instance.IsShiftEnded) return;

        FPSPlayerController player = FPSPlayerController.Instance;
        bool holding = player != null && player.grabber != null && player.grabber.IsHoldingObject;
        bool inVehicle = player != null && !player.IsOnFoot && player.currentVehicle != null;

        // 1. First parcel of the day
        if (!IsSeen(TutorialCatalog.Pickup))
        {
            if (holding) MarkSeen(TutorialCatalog.Pickup);
            else if (player != null && player.IsOnFoot && PhysicalCargoPackage.AllPackages.Count > 0) Trigger(TutorialCatalog.Pickup);
        }

        // 2. Holding a parcel: where it has to go
        if (holding && !IsSeen(TutorialCatalog.Clue)) Trigger(TutorialCatalog.Clue);

        // 2b. First parcel left at a drop-off point (parcels are dropped physically; the notebook shows them as "At the door")
        if (!IsSeen(TutorialCatalog.FirstDelivery))
        {
            var packages = PhysicalCargoPackage.AllPackages;
            for (int i = 0; i < packages.Count; i++)
            {
                PhysicalCargoPackage pkg = packages[i];
                if (pkg == null || pkg.isBeingCarried || pkg.isInVehicleBed) continue;
                if (pkg.FindNearbyDeliveryPoint() == null) continue;
                Trigger(TutorialCatalog.FirstDelivery);
                break;
            }
        }

        // 3. First time in a vehicle
        if (inVehicle && !IsSeen(TutorialCatalog.Vehicle)) Trigger(TutorialCatalog.Vehicle);

        // 4. Vehicle warnings
        if (inVehicle)
        {
            DrivableVehicle v = player.currentVehicle;
            if (!IsSeen(TutorialCatalog.LowFuel) && v.maxFuel > 0f && v.currentFuel / v.maxFuel < LowFuelRatio) Trigger(TutorialCatalog.LowFuel);
            if (!IsSeen(TutorialCatalog.OutOfFuel) && !v.HasFuel) Trigger(TutorialCatalog.OutOfFuel);
            if (!IsSeen(TutorialCatalog.LowCondition) && v.ConditionPercentage < LowConditionRatio) Trigger(TutorialCatalog.LowCondition);
        }

        // 5. First affordable branch upgrade (level 1 -> 2)
        if (!IsSeen(TutorialCatalog.BranchUpgrade) && BranchManager.Instance != null && BranchManager.Instance.HasNextTier &&
            BranchManager.Instance.CurrentBranchLevel == 1 && PlayerEconomyManager.Instance != null &&
            PlayerEconomyManager.Instance.CurrentLiveBalance >= BranchManager.Instance.NextTier.upgradeCost)
        {
            Trigger(TutorialCatalog.BranchUpgrade);
        }
    }

    // ---------------------------------------------------------------
    // Update loop
    // ---------------------------------------------------------------

    private void Update()
    {
        HandleGuideKey();
        if (guideOpen) return;

        pollTimer -= Time.unscaledDeltaTime;
        if (pollTimer <= 0f)
        {
            pollTimer = PollInterval;
            EvaluateTriggers();
        }

        // Day-end tip preempts a coach card once the receipt is on screen.
        if (current != null && current.kind == TutorialKind.Coach && !currentIsReplay && queue.Count > 0)
        {
            TutorialDef next = TutorialCatalog.Get(queue[0]);
            if (next != null && next.kind == TutorialKind.Modal && CanShow(next)) EndCurrent();
        }

        // "Pick up the parcel" waits for the player, so any newer tip (e.g. they went straight to the van) replaces it.
        if (current != null && !currentIsReplay && current.id == TutorialCatalog.Pickup && queue.Count > 0) EndCurrent();

        if (current == null)
        {
            for (int i = 0; i < queue.Count; i++)
            {
                TutorialDef def = TutorialCatalog.Get(queue[i]);
                if (def == null) { queue.RemoveAt(i); i--; continue; }
                if (!CanShow(def)) continue;
                queue.RemoveAt(i);
                StartDef(def, isReplay: false);
                break;
            }
        }

        if (current != null) Tick();
    }

    private bool CanShow(TutorialDef def)
    {
        if (def.kind == TutorialKind.Modal)
        {
            return DaySummaryManager.Instance != null && DaySummaryManager.Instance.IsSummaryOpen;
        }
        return IsPlaying() && !FPSPlayerController.IsAnyUIOpen();
    }

    private void StartDef(TutorialDef def, bool isReplay)
    {
        current = def;
        currentIsReplay = isReplay;
        page = 0;
        if (!isReplay) MarkSeen(def.id);

        if (def.kind == TutorialKind.Modal)
        {
            if (isReplay)
            {
                // Replayed outside the receipt screen: freeze the game and free the cursor while it is open.
                modalReplay = true;
                savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                FPSPlayerController.LockCursor(false);
            }
            ShowModal(def);
            return;
        }

        pageTimer = def.pages.Count > 0 ? def.pages[0].seconds : 10f;
        if (coachRoot != null) coachRoot.SetActive(true);
        RenderPage();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayNotification();
    }

    private void Tick()
    {
        if (current.kind == TutorialKind.Modal)
        {
            if (WasEnterPressed()) OnModalOk();
            return;
        }

        // Card sits out while another screen (tablet, menu, shop) is open, and comes back afterwards.
        bool uiOpen = FPSPlayerController.IsAnyUIOpen() || !IsPlaying();
        if (coachRoot != null && coachRoot.activeSelf == uiOpen) coachRoot.SetActive(!uiOpen);
        if (uiOpen) return;

        if (WasEnterPressed())
        {
            NextPage();
            return;
        }

        // Goal-based tip: done as soon as the player picks the parcel up.
        if (current.id == TutorialCatalog.Pickup)
        {
            FPSPlayerController p = FPSPlayerController.Instance;
            if (p != null && p.grabber != null && p.grabber.IsHoldingObject) { EndCurrent(); return; }
        }

        pageTimer -= Time.unscaledDeltaTime;
        UpdateTimerBar();
        if (pageTimer <= 0f) NextPage();
    }

    private void NextPage()
    {
        if (current == null) return;
        page++;
        if (page >= current.pages.Count)
        {
            EndCurrent();
            return;
        }
        pageTimer = current.pages[page].seconds;
        RenderPage();
    }

    private void EndCurrent()
    {
        if (current != null && current.kind == TutorialKind.Modal && modalReplay) EndModalReplay();
        current = null;
        page = 0;
        if (coachRoot != null) coachRoot.SetActive(false);
        if (modalRoot != null) modalRoot.SetActive(false);
    }

    private void EndModalReplay()
    {
        modalReplay = false;
        Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;
        FPSPlayerController.LockCursor(true);
    }

    private void OnModalOk()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        EndCurrent();
    }

    // ---------------------------------------------------------------
    // Rendering
    // ---------------------------------------------------------------

    private void RenderPage()
    {
        if (current == null || current.kind != TutorialKind.Coach) return;
        page = Mathf.Clamp(page, 0, Mathf.Max(0, current.pages.Count - 1));
        TutorialPage p = current.pages[page];

        SetBadge(coachBadge, coachIcon, current.badge, current.icon);

        if (coachLabel != null)
        {
            string label = TutorialTexts.Get("tut_label");
            coachLabel.text = current.pages.Count > 1 ? $"{label}  ·  {page + 1}/{current.pages.Count}" : label;
        }
        if (coachTitle != null) coachTitle.text = TutorialTexts.Get(p.titleKey ?? current.titleKey);
        if (coachBody != null) coachBody.text = ResolveKeys(TutorialTexts.Get(p.textKey));
        if (coachSkipHint != null) coachSkipHint.text = ResolveKeys(TutorialTexts.Get("tut_skip"));
        if (coachTimerRoot != null) coachTimerRoot.SetActive(current.showTimer);
        UpdateTimerBar();

        if (coachRoot != null) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)coachRoot.transform);
    }

    private void UpdateTimerBar()
    {
        if (coachTimerFill == null || current == null || !current.showTimer) return;
        float total = Mathf.Max(0.1f, current.pages[Mathf.Clamp(page, 0, current.pages.Count - 1)].seconds);
        RectTransform rt = coachTimerFill.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(Mathf.Clamp01(pageTimer / total), 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void ShowModal(TutorialDef def)
    {
        if (modalRoot == null) { current = null; return; }
        modalRoot.SetActive(true);
        modalRoot.transform.SetAsLastSibling();

        SetBadge(modalBadge, modalIcon, def.badge, def.icon);
        if (modalTitle != null) modalTitle.text = TutorialTexts.Get(def.titleKey);
        if (modalSubtitle != null) modalSubtitle.text = TutorialTexts.Get(def.subtitleKey);

        if (modalRows != null)
        {
            for (int i = 0; i < modalRows.childCount; i++)
            {
                Transform row = modalRows.GetChild(i);
                bool used = i < def.rows.Count;
                row.gameObject.SetActive(used);
                if (!used) continue;

                TutorialRow r = def.rows[i];
                Transform badge = row.Find("Badge");
                if (badge != null)
                {
                    badge.TryGetComponent(out Image badgeImg);
                    Transform icon = badge.Find("Icon");
                    Image iconImg = icon != null ? icon.GetComponent<Image>() : null;
                    SetBadge(badgeImg, iconImg, r.badge, r.icon);
                }
                Transform text = row.Find("Text");
                if (text != null && text.TryGetComponent(out TextMeshProUGUI tmp)) tmp.text = ResolveKeys(TutorialTexts.Get(r.textKey));
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)modalRoot.transform);
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayNotification();
    }

    private static void SetBadge(Image badge, Image icon, TutorialBadge kind, string iconName)
    {
        Color bg, fg;
        switch (kind)
        {
            case TutorialBadge.Sky: bg = CozyTheme.Sky; fg = Color.white; break;
            case TutorialBadge.Mint: bg = CozyTheme.Mint; fg = Color.white; break;
            case TutorialBadge.Red: bg = CozyTheme.Red; fg = Color.white; break;
            case TutorialBadge.Orange: bg = CozyTheme.OrangeTint; fg = CozyTheme.OrangeInk; break;
            default: bg = CozyTheme.Honey; fg = CozyTheme.Ink; break;
        }
        if (badge != null) badge.color = bg;
        if (icon != null)
        {
            icon.color = fg;
            if (CozyAssets.Instance != null)
            {
                Sprite s = CozyAssets.Instance.Icon(iconName);
                if (s != null) icon.sprite = s;
            }
        }
    }

    // ---------------------------------------------------------------
    // Key names follow the player's key bindings: {Interact} -> [E]
    // ---------------------------------------------------------------

    private static readonly Regex KeyToken = new Regex(@"\{(\w+)\}", RegexOptions.Compiled);

    public static string ResolveKeys(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;
        return KeyToken.Replace(text, m =>
        {
            if (!System.Enum.TryParse(m.Groups[1].Value, out GameAction action)) return m.Value;
            InputBindingData binding = KeyBindingManager.GetBinding(action);
            string name = binding.IsAssigned ? binding.GetDisplayName() : "?";
            int paren = name.IndexOf('(');
            if (paren > 0) name = name.Substring(0, paren).Trim(); // "Sağ Tık (Mouse 1)" -> "Sağ Tık"
            return "[" + name + "]";
        });
    }

    // ---------------------------------------------------------------
    // F1 guide list
    // ---------------------------------------------------------------

    public void CloseAnyOpenTutorial()
    {
        if (guideOpen)
        {
            escapeConsumedFrame = Time.frameCount;
            GameMenuManager.ConsumeEscape();
            CloseGuide();
        }
        else if (modalReplay)
        {
            escapeConsumedFrame = Time.frameCount;
            GameMenuManager.ConsumeEscape();
            OnModalOk();
        }
    }

    private void HandleGuideKey()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.f1Key.wasPressedThisFrame)
        {
            if (guideOpen) CloseGuide();
            else if (CanOpenGuide()) OpenGuide();
        }
        else if (kb.escapeKey.wasPressedThisFrame)
        {
            if (guideOpen)
            {
                escapeConsumedFrame = Time.frameCount;
                GameMenuManager.ConsumeEscape();
                CloseGuide();
            }
            else if (modalReplay)
            {
                escapeConsumedFrame = Time.frameCount;
                GameMenuManager.ConsumeEscape();
                OnModalOk();
            }
        }
    }

    private bool CanOpenGuide()
    {
        if (guideRoot == null || modalReplay) return false;
        if (!IsPlaying()) return false;
        if (FPSPlayerController.IsAnyUIOpen()) return false;
        if (DayTimeManager.Instance != null && DayTimeManager.Instance.IsShiftEnded) return false;
        return true;
    }

    public void OpenGuide()
    {
        if (guideRoot == null) return;
        guideOpen = true;
        savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        if (coachRoot != null) coachRoot.SetActive(false);
        guideRoot.SetActive(true);
        guideRoot.transform.SetAsLastSibling();
        RefreshGuideRows();
        FPSPlayerController.LockCursor(false);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMenuOpen();
    }

    public void CloseGuide() => CloseGuide(false);

    private void CloseGuide(bool keepCursorFree)
    {
        if (!guideOpen) return;
        guideOpen = false;
        Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;
        if (guideRoot != null) guideRoot.SetActive(false);
        if (!keepCursorFree) FPSPlayerController.LockCursor(true);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMenuClose();
    }

    private void RefreshGuideRows()
    {
        if (guideGrid == null) return;
        for (int i = 0; i < guideGrid.childCount && i < TutorialCatalog.All.Length; i++)
        {
            TutorialDef def = TutorialCatalog.All[i];
            Transform row = guideGrid.GetChild(i);

            Transform badge = row.Find("Badge");
            if (badge != null)
            {
                badge.TryGetComponent(out Image badgeImg);
                Transform icon = badge.Find("Icon");
                SetBadge(badgeImg, icon != null ? icon.GetComponent<Image>() : null, def.badge, def.icon);
            }

            SetText(row, "Texts/Title", TutorialTexts.Get(def.titleKey));
            string sub = TutorialTexts.Get(def.subKey);
            if (def.pages.Count > 1) sub += "  ·  " + string.Format(TutorialTexts.Get("tut_pages"), def.pages.Count);
            SetText(row, "Texts/Sub", sub);

            bool seen = IsSeen(def.id);
            Transform chip = row.Find("Chip");
            if (chip != null)
            {
                CozyTheme.GetTone(seen ? CozyTheme.Tone.Mint : CozyTheme.Tone.Honey, out Color bg, out Color fg);
                if (chip.TryGetComponent(out Image chipImg)) chipImg.color = bg;
                Transform t = chip.Find("Text");
                if (t != null && t.TryGetComponent(out TextMeshProUGUI tmp)) { tmp.text = TutorialTexts.Get(seen ? "tut_guide_seen" : "tut_guide_new"); tmp.color = fg; }
            }
        }
    }

    private static void SetText(Transform root, string path, string value)
    {
        Transform t = root.Find(path);
        if (t != null && t.TryGetComponent(out TextMeshProUGUI tmp)) tmp.text = value;
    }

    private static bool WasEnterPressed()
    {
        Keyboard kb = Keyboard.current;
        return kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
    }
}
