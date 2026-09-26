using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Handles transition screen, level announcement card, and timing when upgrading the branch.
/// Completely isolated: does NOT modify or style any other UI panels in the scene.
/// </summary>
public class BranchUpgradeTransitionUI : MonoBehaviour
{
    public static BranchUpgradeTransitionUI Instance { get; private set; }

    [Header("--- PANEL & CANVAS GROUP ---")]
    [Tooltip("Root GameObject for the transition overlay")]
    public GameObject panelRoot;

    [Tooltip("CanvasGroup used for smooth alpha fade in/out")]
    public CanvasGroup canvasGroup;

    [Header("--- UI TEXT ELEMENTS ---")]
    [Tooltip("Top badge text (e.g. '★ ŞUBE YÜKSELTİLDİ ★')")]
    public TextMeshProUGUI badgeText;

    [Tooltip("Tier title text (e.g. 'LEVEL 2: REGIONAL HUB')")]
    public TextMeshProUGUI titleText;

    [Tooltip("Tier details and perks text")]
    public TextMeshProUGUI detailsText;

    [Header("--- TIMING CONFIGURATION ---")]
    [Tooltip("Duration of fade out to black (seconds)")]
    public float fadeOutDuration = 0.5f;

    [Tooltip("Duration to stay on black / card display before fading back in (seconds)")]
    public float holdBlackDuration = 0.85f;

    [Tooltip("Duration of fade in back to game (seconds)")]
    public float fadeInDuration = 0.75f;

    [Header("--- CONFIRMATION ---")]
    [Tooltip("The level-up card stays until the player presses Enter (after Hold Black Duration).")]
    public bool requireEnterToContinue = true;

    [Tooltip("Optional hint label under the card text. Created automatically from the badge label when empty.")]
    public TextMeshProUGUI continueHintText;

    [Header("--- KURYE DEFTERI CARD (optional) ---")]
    [Tooltip("When set, the card shows structured stat tiles and perk rows instead of one details paragraph.")]
    public TextMeshProUGUI capValueText;
    public TextMeshProUGUI rentValueText;
    [Tooltip("Container whose children are perk rows; each row has a child named Text.")]
    public Transform perkRows;
    [Tooltip("CanvasGroup of the card itself (the backdrop is the panel root's Image).")]
    public CanvasGroup cardGroup;

    private static bool _isTransitioning = false;
    public static bool IsTransitioning => _isTransitioning;

    private Coroutine activeTransitionCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (panelRoot == null && Instance.panelRoot != null)
            {
                Destroy(this);
                return;
            }
            if (Instance.panelRoot == null && panelRoot != null)
            {
                Destroy(Instance);
                Instance = this;
            }
        }
        else
        {
            Instance = this;
        }

        EnsureUIComponents();
        HideImmediate();
    }

    private void Start()
    {
        EnsureUIComponents();
        HideImmediate();
    }

    public void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        _isTransitioning = false;
    }

    /// <summary>
    /// Executes the full upgrade sequence with fade-out, blackout action (teleport & visuals), hold, and fade-in.
    /// </summary>
    public void PlayUpgradeSequence(int oldLevel, int newLevel, BranchTier newTier, Action onBlackoutAction, Action onCompleteAction = null)
    {
        EnsureUIComponents();

        if (activeTransitionCoroutine != null)
        {
            StopCoroutine(activeTransitionCoroutine);
        }

        activeTransitionCoroutine = StartCoroutine(UpgradeSequenceRoutine(oldLevel, newLevel, newTier, onBlackoutAction, onCompleteAction));
    }

    private IEnumerator UpgradeSequenceRoutine(int oldLevel, int newLevel, BranchTier newTier, Action onBlackoutAction, Action onCompleteAction)
    {
        _isTransitioning = true;

        // 1. Close tablet or modal if open
        if (CargoTabletUI.Instance != null && CargoTabletUI.Instance.IsTabletOpen)
        {
            CargoTabletUI.Instance.CloseTablet();
        }

        if (CommercialHubUIManager.Instance != null && CommercialHubUIManager.Instance.IsAnyPanelOpen)
        {
            CommercialHubUIManager.Instance.CloseAllPanels();
        }

        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.HidePrompt();
            InteractionPromptHUD.Instance.SuppressPrompts(fadeOutDuration + holdBlackDuration + fadeInDuration + 0.5f);
        }

        // 2. Safe cleanup of player state (vehicle / cargo)
        FPSPlayerController player = FPSPlayerController.Instance != null ? FPSPlayerController.Instance : FindAnyObjectByType<FPSPlayerController>();
        if (player != null)
        {
            if (player.grabber != null && player.grabber.IsHoldingObject)
            {
                player.grabber.ReleaseObject(Vector3.zero);
            }

            if (!player.IsOnFoot && player.currentVehicle != null)
            {
                player.currentVehicle.ExitVehicle();
            }

            player.SetOnFootActive(false);
        }

        // 3. Prepare UI Elements
        string tierNameStr = newTier != null ? newTier.GetLocalizedName() : $"Tier {newLevel}";
        int cap = newTier != null ? newTier.dailyPackageCapacity : (newLevel * 6);
        int rent = newTier != null ? newTier.dailyRent : (newLevel * 60);
        string desc = newTier != null ? newTier.GetLocalizedDescription() : "";
        string perks = newTier != null ? newTier.GetFormattedPerksText() : "";
        if (!string.IsNullOrEmpty(perks))
        {
            if (!string.IsNullOrEmpty(desc)) desc += "\n";
            desc += perks;
        }

        bool cardMode = capValueText != null;

        if (badgeText != null)
        {
            badgeText.text = cardMode
                ? LocalizationManager.Get("cozy_branch_trans_badge", "Şube yükseltildi")
                : LocalizationManager.Get("branch_trans_badge");
        }

        if (titleText != null)
        {
            titleText.text = cardMode
                ? LocalizationManager.GetFormat("cozy_branch_trans_title", newLevel)
                : LocalizationManager.GetFormat("branch_trans_title", newLevel, tierNameStr.ToUpper());
        }

        if (cardMode)
        {
            capValueText.text = LocalizationManager.GetFormat("cozy_trans_cap_value", cap);
            if (rentValueText != null) rentValueText.text = LocalizationManager.GetFormat("cozy_trans_rent_value", rent);
            FillPerkRows(newTier);
        }
        else if (detailsText != null)
        {
            detailsText.text = LocalizationManager.GetFormat("branch_trans_details", cap, rent, desc);
        }

        // 4. Activate Panel. With a separate backdrop the black screen and the card fade independently:
        //    the backdrop goes black and later fades away, while the card stays until the player confirms.
        Image backdrop = panelRoot != null ? panelRoot.GetComponent<Image>() : null;
        CanvasGroup cardGroup = ResolveCardGroup();
        bool split = backdrop != null && cardGroup != null && canvasGroup != null;

        if (panelRoot != null) panelRoot.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            if (split)
            {
                canvasGroup.alpha = 1f;
                cardGroup.alpha = 0f;
                yield return FadeBackdrop(backdrop, 0f, 1f, fadeOutDuration);
            }
            else
            {
                float elapsed = 0f;
                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeOutDuration);
                    yield return null;
                }
                canvasGroup.alpha = 1f;
            }
        }

        // 5. Blackout Execution: Swap visuals, relocate player outside, aim camera
        onBlackoutAction?.Invoke();

        // Play level up celebration sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLevelUp();
        }

        // The card appears on the black screen
        if (split)
        {
            float cardElapsed = 0f;
            while (cardElapsed < 0.35f)
            {
                cardElapsed += Time.unscaledDeltaTime;
                cardGroup.alpha = Mathf.Clamp01(cardElapsed / 0.35f);
                yield return null;
            }
            cardGroup.alpha = 1f;
        }

        float holdElapsed = 0f;
        while (holdElapsed < holdBlackDuration)
        {
            holdElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 6. The black screen fades away (transparent backdrop) while the card stays in front.
        if (split)
        {
            yield return FadeBackdrop(backdrop, 1f, 0f, fadeInDuration);
        }

        // The card stays up until the player confirms with Enter (no automatic dismissal).
        if (requireEnterToContinue)
        {
            TextMeshProUGUI hint = EnsureContinueHint();
            if (hint != null)
            {
                hint.text = LocalizationManager.Get("branch_trans_continue", "Devam etmek için [Enter]");
                hint.gameObject.SetActive(true);
            }

            float pulse = 0f;
            while (!WasConfirmPressed())
            {
                if (hint != null)
                {
                    pulse += Time.unscaledDeltaTime;
                    hint.alpha = 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(pulse * 4f));
                }
                yield return null;
            }

            if (hint != null) hint.gameObject.SetActive(false);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        }

        // The card fades out
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            float duration = split ? 0.3f : fadeInDuration;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / duration));
                yield return null;
            }
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (backdrop != null) SetBackdropAlpha(backdrop, 1f); // ready for the next upgrade
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        // 7. Restore player controls
        if (player != null)
        {
            player.SetOnFootActive(true);
            FPSPlayerController.LockCursor(true);
        }

        _isTransitioning = false;

        if (InteractionPromptHUD.Instance != null)
        {
            InteractionPromptHUD.Instance.ShowPrompt(LocalizationManager.GetFormat("prompt_branch_active_new", newLevel, tierNameStr), 4.0f);
        }

        onCompleteAction?.Invoke();
        activeTransitionCoroutine = null;
    }

    private CanvasGroup ResolveCardGroup()
    {
        if (cardGroup != null) return cardGroup;
        if (panelRoot == null) return null;
        Transform card = panelRoot.transform.Find("Card");
        if (card == null) return null;
        cardGroup = card.GetComponent<CanvasGroup>();
        if (cardGroup == null) cardGroup = card.gameObject.AddComponent<CanvasGroup>();
        return cardGroup;
    }

    private static void SetBackdropAlpha(Image image, float alpha)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    private static IEnumerator FadeBackdrop(Image image, float from, float to, float duration)
    {
        float elapsed = 0f;
        SetBackdropAlpha(image, from);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetBackdropAlpha(image, Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration))));
            yield return null;
        }
        SetBackdropAlpha(image, to);
    }

    private void FillPerkRows(BranchTier tier)
    {
        if (perkRows == null) return;
        string[] perks = tier != null ? tier.GetLocalizedPerks() : new string[0];
        for (int i = 0; i < perkRows.childCount; i++)
        {
            Transform row = perkRows.GetChild(i);
            bool used = perks != null && i < perks.Length && !string.IsNullOrWhiteSpace(perks[i]);
            row.gameObject.SetActive(used);
            if (!used) continue;
            Transform text = row.Find("Text");
            if (text != null && text.TryGetComponent(out TextMeshProUGUI tmp)) tmp.text = perks[i].Trim();
        }
    }

    private static bool WasConfirmPressed()
    {
        Keyboard kb = Keyboard.current;
        return kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
    }

    /// <summary>Uses the assigned hint label, or clones the badge label at the bottom of the card.</summary>
    private TextMeshProUGUI EnsureContinueHint()
    {
        if (continueHintText != null) return continueHintText;
        if (badgeText == null) return null;

        GameObject clone = Instantiate(badgeText.gameObject, badgeText.transform.parent);
        clone.name = "ContinueHint";
        continueHintText = clone.GetComponent<TextMeshProUGUI>();
        continueHintText.color = CozyTheme.InkSoft;
        clone.transform.SetAsLastSibling();
        return continueHintText;
    }

    public void EnsureUIComponents()
    {
        if (panelRoot != null && canvasGroup != null && titleText != null) return;

        // Check if existing in hierarchy
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();

        if (canvas == null) return;

        Transform existing = canvas.transform.Find("BranchUpgradeTransitionPanel");
        if (existing != null)
        {
            panelRoot = existing.gameObject;
            canvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = panelRoot.AddComponent<CanvasGroup>();

            badgeText = panelRoot.transform.Find("Card/BadgeText")?.GetComponent<TextMeshProUGUI>();
            titleText = panelRoot.transform.Find("Card/TitleText")?.GetComponent<TextMeshProUGUI>();
            detailsText = panelRoot.transform.Find("Card/DetailsText")?.GetComponent<TextMeshProUGUI>();

            if (titleText != null) return;
        }

        // Auto-build procedural fallback if missing (strictly on its own panel, does NOT touch other panels)
        BuildDefaultTransitionPanel(canvas);
    }

    private void BuildDefaultTransitionPanel(Canvas canvas)
    {
        if (canvas == null) return;

        GameObject root = new GameObject("BranchUpgradeTransitionPanel");
        root.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;

        canvasGroup = root.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // Fullscreen Dark Background Overlay
        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.05f, 0.08f, 0.98f);

        // Center Card
        GameObject card = new GameObject("Card");
        card.transform.SetParent(root.transform, false);
        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(900, 320);

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.08f, 0.11f, 0.17f, 0.95f);

        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.8f, 1f, 0.4f);
        outline.effectDistance = new Vector2(2, -2);

        // 1. Badge Text
        GameObject badgeObj = new GameObject("BadgeText");
        badgeObj.transform.SetParent(card.transform, false);
        RectTransform badgeRect = badgeObj.AddComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0f, 1f);
        badgeRect.anchorMax = new Vector2(1f, 1f);
        badgeRect.pivot = new Vector2(0.5f, 1f);
        badgeRect.anchoredPosition = new Vector2(0, -25);
        badgeRect.sizeDelta = new Vector2(0, 35);

        badgeText = badgeObj.AddComponent<TextMeshProUGUI>();
        badgeText.text = "ŞUBE YÜKSELTİLDİ - BRANCH UPGRADE";
        badgeText.fontSize = 20;
        badgeText.fontStyle = FontStyles.Bold;
        badgeText.alignment = TextAlignmentOptions.Center;
        badgeText.color = new Color(0.3f, 1f, 0.6f);

        // 2. Title Text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(card.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -70);
        titleRect.sizeDelta = new Vector2(0, 60);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "LEVEL 2: REGIONAL HUB";
        titleText.fontSize = 36;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        // Decorative Accent Line
        GameObject lineObj = new GameObject("AccentLine");
        lineObj.transform.SetParent(card.transform, false);
        RectTransform lineRect = lineObj.AddComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.15f, 1f);
        lineRect.anchorMax = new Vector2(0.85f, 1f);
        lineRect.pivot = new Vector2(0.5f, 1f);
        lineRect.anchoredPosition = new Vector2(0, -135);
        lineRect.sizeDelta = new Vector2(0, 3);
        Image lineImg = lineObj.AddComponent<Image>();
        lineImg.color = new Color(0.2f, 0.8f, 1f, 0.7f);

        // 3. Details Text
        GameObject detailsObj = new GameObject("DetailsText");
        detailsObj.transform.SetParent(card.transform, false);
        RectTransform detailsRect = detailsObj.AddComponent<RectTransform>();
        detailsRect.anchorMin = new Vector2(0f, 0f);
        detailsRect.anchorMax = new Vector2(1f, 1f);
        detailsRect.offsetMin = new Vector2(40, 30);
        detailsRect.offsetMax = new Vector2(-40, -150);

        detailsText = detailsObj.AddComponent<TextMeshProUGUI>();
        detailsText.text = "<b>Kapasite:</b> 8 Paket/Gün  |  <b>Kira:</b> $120/Gün\n<size=85%>Genişletilmiş lojistik alanı ve artırılmış teslimat hacmi.</size>";
        detailsText.fontSize = 20;
        detailsText.alignment = TextAlignmentOptions.Center;
        detailsText.color = new Color(0.85f, 0.92f, 1f);

        panelRoot = root;
        panelRoot.SetActive(false);
    }
}
