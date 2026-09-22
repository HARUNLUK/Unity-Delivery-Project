using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using TMPro;

/// <summary>
/// Vehicle Driving & Cargo Transport Tutorial Guide UI.
/// Triggered the first time the player enters a vehicle.
/// Same schema as DeliveryTutorialUI (cleaned without extra close/toggle buttons).
/// </summary>
public class VehicleTutorialUI : MonoBehaviour
{
    public static VehicleTutorialUI Instance { get; private set; }

    public const string PREF_VEHICLE_TUTORIAL_SEEN = "Vehicle_Tutorial_Seen";

    [Header("--- TUTORIAL ACTIVATION SETTINGS ---")]
    [Tooltip("If true, automatically opens the first time the player enters any drivable vehicle")]
    public bool autoShowOnFirstVehicleEntry = true;

    [Tooltip("Shortcut key to toggle/reopen the vehicle driving tutorial at any time")]
    public KeyCode toggleKey = KeyCode.F2;

    [Header("--- CUSTOM IMAGE (ARAÇ / SÜRÜŞ RESMİ) ---")]
    [Tooltip("Custom vehicle illustration or screenshot sprite")]
    public Sprite tutorialImage;

    [Tooltip("If true, keeps original aspect ratio. If false (default), stretches to fill the image frame.")]
    public bool preserveImageAspect = false;

    [Header("--- INSPECTOR UI REFERENCES (SAME SCHEMA AS GAME START TUTORIAL) ---")]
    public GameObject tutorialPanelRoot;
    public Image vehicleImageUI;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI leftCaptionText;
    public TextMeshProUGUI objectiveBodyText;
    public Button startButton;
    public CanvasGroup panelCanvasGroup;

    private bool isTutorialOpen = false;
    public bool IsOpen => isTutorialOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureReferences();
        BindButtons();
        UpdateContent();

        if (tutorialPanelRoot != null)
        {
            tutorialPanelRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
        BindButtons();
        DrivableVehicle.OnPlayerEnteredVehicle += HandlePlayerEnteredVehicle;
        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDisable()
    {
        DrivableVehicle.OnPlayerEnteredVehicle -= HandlePlayerEnteredVehicle;
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(string newLang)
    {
        UpdateContent();
    }

    private void Start()
    {
        EnsureReferences();
        BindButtons();
        UpdateContent();
        if (tutorialPanelRoot != null)
        {
            tutorialPanelRoot.SetActive(false);
        }
    }

    private void HandlePlayerEnteredVehicle(DrivableVehicle vehicle)
    {
        if (!autoShowOnFirstVehicleEntry) return;

        // Check if already seen
        bool alreadySeen = PlayerPrefs.GetInt(PREF_VEHICLE_TUTORIAL_SEEN, 0) == 1;
        if (alreadySeen) return;

        // Check if currently in main menu
        if (GameMenuManager.Instance != null && GameMenuManager.Instance.CurrentState != GameFlowState.Playing)
        {
            return;
        }

        // Delay slightly for smooth camera blend into car seat
        StartCoroutine(DelayedShowTutorial(0.35f));
    }

    private IEnumerator DelayedShowTutorial(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (isTutorialOpen) yield break;

        ShowTutorial();
    }

    private void Update()
    {
        // Don't process tutorial keys or remain open if in main menu
        if (GameMenuManager.Instance != null && GameMenuManager.Instance.CurrentState == GameFlowState.MainMenu)
        {
            if (isTutorialOpen) HideTutorial();
            return;
        }

        bool togglePressed = false;
        bool escapePressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            togglePressed = (toggleKey == KeyCode.F2 && Keyboard.current.f2Key.wasPressedThisFrame)
                || (toggleKey == KeyCode.F1 && Keyboard.current.f1Key.wasPressedThisFrame);
            escapePressed = Keyboard.current.escapeKey.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (toggleKey != KeyCode.None) togglePressed |= Input.GetKeyDown(toggleKey);
            escapePressed |= Input.GetKeyDown(KeyCode.Escape);
        }
        catch { }
#endif

        if (togglePressed)
        {
            if (isTutorialOpen) HideTutorial();
            else ShowTutorial();
            return;
        }

        if (isTutorialOpen && escapePressed)
        {
            HideTutorial();
            return;
        }

        if (isTutorialOpen)
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    public void ShowTutorial()
    {
        EnsureReferences();
        BindButtons();

        if (tutorialPanelRoot == null) return;

        UpdateContent();

        tutorialPanelRoot.SetActive(true);
        tutorialPanelRoot.transform.SetAsLastSibling();
        isTutorialOpen = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuOpen();
        }

        if (panelCanvasGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(panelCanvasGroup, 0f, 1f, 0.22f));
        }
    }

    public void HideTutorial()
    {
        if (!isTutorialOpen && (tutorialPanelRoot == null || !tutorialPanelRoot.activeSelf)) return;

        PlayerPrefs.SetInt(PREF_VEHICLE_TUTORIAL_SEEN, 1);
        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuClose();
        }

        if (panelCanvasGroup != null && gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(panelCanvasGroup, panelCanvasGroup.alpha, 0f, 0.18f, () =>
            {
                if (tutorialPanelRoot != null) tutorialPanelRoot.SetActive(false);
                isTutorialOpen = false;
                RestoreCursorState();
            }));
        }
        else
        {
            if (tutorialPanelRoot != null) tutorialPanelRoot.SetActive(false);
            isTutorialOpen = false;
            RestoreCursorState();
        }
    }

    public void ToggleTutorial()
    {
        if (isTutorialOpen) HideTutorial();
        else ShowTutorial();
    }

    private void RestoreCursorState()
    {
        if (FPSPlayerController.IsAnyUIOpen())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void SetTutorialImage(Sprite sprite)
    {
        tutorialImage = sprite;
        if (vehicleImageUI != null && sprite != null)
        {
            vehicleImageUI.sprite = sprite;
        }
    }

    public void BindButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(HideTutorial);
            startButton.onClick.AddListener(HideTutorial);
        }
    }

    public void UpdateContent()
    {
        bool isTr = LocalizationManager.IsTurkish;

        if (titleText != null)
        {
            titleText.text = LocalizationManager.Get("vehicle_tutorial_title", 
                isTr ? "<b>ARAÇ SÜRÜŞ VE TAŞIMA REHBERİ</b>" : "<b>VEHICLE DRIVING & CARGO GUIDE</b>");
        }

        if (leftCaptionText != null)
        {
            leftCaptionText.text = LocalizationManager.Get("vehicle_tutorial_caption", 
                isTr ? "<b>Teslimat Aracı (Pickup / Van)</b>\n<size=15><color=#A0C8FF>Kargolarınızı kasa veya bagaj kısmına yükleyin, güvenli ve hızlı şekilde taşıyın.</color></size>" 
                     : "<b>Delivery Vehicle (Pickup / Van)</b>\n<size=15><color=#A0C8FF>Load cargo onto the bed, drive safely and deliver without damage.</color></size>");
        }

        if (objectiveBodyText != null)
        {
            string fallbackBody = isTr
                ? "<color=#FFE600><b>1. SÜRÜŞ KONTROLLERİ</b></color>\n" +
                  "• <color=#32FFFF>[W / A / S / D]</color> veya Yön Tuşları ile gaz, fren ve direksiyon kontrolü.\n" +
                  "• <color=#32FFFF>[Boşluk / Space]</color> El freni ile keskin virajları dönebilir veya kaymayı durdurabilirsiniz.\n" +
                  "• <color=#32FFFF>[V]</color> tuşu ile araç içi (FPS) ve araç dışı (TPS) kamera açıları arasında geçiş yapabilirsiniz.\n" +
                  "• <color=#32FFFF>[E]</color> veya <color=#32FFFF>[F]</color> tuşu ile dilediğiniz an araçtan inip binebilirsiniz.\n\n" +

                  "<color=#FFE600><b>2. KARGO GÜVENLİĞİ VE YÜKLEME</b></color>\n" +
                  "• Kargoları aracın arkasındaki kasaya düzenli yerleştirin.\n" +
                  "• Sert virajlarda, çukurlarda ve yüksek hızda kargolar kasadan düşebilir veya hasar alabilir!\n\n" +

                  "<color=#FFE600><b>3. YAKIT VE ARAÇ DURUMU</b></color>\n" +
                  "• Ekrandaki yakıt göstergesine dikkat edin. Yakıtınız azalınca <color=#FFAA22>Benzin İstasyonuna</color> uğrayın.\n" +
                  "• Çarpışmalarda araç kondisyonu düşer. Hasar aldığınızda <color=#32FF64>Tamirhanede</color> aracınızı onarın.\n\n" +

                  "<color=#A0C8FF><i>İpucu: Teslimat adreslerini Tabletinizden [TAB] veya yol kenarındaki tabelalardan takip edebilirsiniz.</i></color>"
                : "<color=#FFE600><b>1. DRIVING CONTROLS</b></color>\n" +
                  "• <color=#32FFFF>[W / A / S / D]</color> or Arrow Keys for throttle, brake, and steering.\n" +
                  "• <color=#32FFFF>[Spacebar]</color> Handbrake for sharp corners and emergency stops.\n" +
                  "• <color=#32FFFF>[V]</color> key toggles between interior (cockpit) and exterior (third-person) camera views.\n" +
                  "• <color=#32FFFF>[E]</color> or <color=#32FFFF>[F]</color> to enter or exit the vehicle anytime.\n\n" +

                  "<color=#FFE600><b>2. CARGO SAFETY & LOADING</b></color>\n" +
                  "• Stack packages carefully in the rear cargo bed.\n" +
                  "• Beware of high speeds and sharp turns: unsecured cargo can bounce out or break!\n\n" +

                  "<color=#FFE600><b>3. FUEL & VEHICLE CONDITION</b></color>\n" +
                  "• Monitor your fuel gauge. Visit the <color=#FFAA22>Fuel Station</color> before running out.\n" +
                  "• Collisions degrade condition. Visit the <color=#32FF64>Garage Workshop</color> to repair damage.\n\n" +

                  "<color=#A0C8FF><i>Tip: Track destination addresses via your Tablet [TAB] or roadside signs.</i></color>";

            objectiveBodyText.text = LocalizationManager.Get("vehicle_tutorial_body", fallbackBody);
        }

        if (startButton != null)
        {
            var btnText = startButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = LocalizationManager.Get("vehicle_tutorial_btn_start", 
                    isTr ? "<b>ANLADIM, SÜRÜŞE BAŞLA</b>" : "<b>GOT IT, START DRIVING</b>");
            }
        }

        // Also update F2 hint text if present in bottom bar
        if (tutorialPanelRoot != null)
        {
            var hintTexts = tutorialPanelRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in hintTexts)
            {
                if (t != null && (t.gameObject.name.Contains("F2_Hint") || t.gameObject.name.Contains("F1_Hint") || t.gameObject.name.Contains("Hint")))
                {
                    t.text = LocalizationManager.Get("vehicle_tutorial_f2_hint", 
                        isTr ? "<color=#64A0FF>[F2]</color> <color=#A0B0C0>Tuşu ile sürüş rehberini istediğiniz zaman tekrar açabilirsiniz.</color>" 
                             : "<color=#64A0FF>[F2]</color> <color=#A0B0C0>Key to reopen the driving guide at any time.</color>");
                    break;
                }
            }
        }

        if (vehicleImageUI != null && tutorialImage != null)
        {
            vehicleImageUI.sprite = tutorialImage;
            vehicleImageUI.preserveAspect = preserveImageAspect;
        }
    }

    public void EnsureReferences()
    {
        if (tutorialPanelRoot == null)
        {
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform foundRoot = canvas.transform.Find("VehicleTutorial_Modal_Root");
                if (foundRoot != null)
                {
                    tutorialPanelRoot = foundRoot.gameObject;
                }
            }
        }

        if (tutorialPanelRoot != null)
        {
            if (panelCanvasGroup == null) panelCanvasGroup = tutorialPanelRoot.GetComponent<CanvasGroup>();
            if (titleText == null) titleText = FindTMPRecursive(tutorialPanelRoot.transform, "Header_Title_Text", "TitleText", "HeaderTitle", "Title");
            if (leftCaptionText == null) leftCaptionText = FindTMPRecursive(tutorialPanelRoot.transform, "Left_Caption_Text", "CaptionText", "LeftCaption");
            if (objectiveBodyText == null) objectiveBodyText = FindTMPRecursive(tutorialPanelRoot.transform, "Objective_Body_Text", "BodyText", "ObjectiveText");
            if (vehicleImageUI == null) vehicleImageUI = FindImageRecursive(tutorialPanelRoot.transform, "Delivery_Point_Image", "VehicleImage", "TutorialImage", "Image");
            if (startButton == null) startButton = FindButtonRecursive(tutorialPanelRoot.transform, "Start_Game_Button", "StartButton", "OkButton", "ConfirmButton");
        }
    }

    private static TextMeshProUGUI FindTMPRecursive(Transform root, params string[] names)
    {
        if (root == null) return null;
        var list = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var n in names)
        {
            foreach (var t in list)
            {
                if (t != null && t.gameObject.name.Equals(n, System.StringComparison.OrdinalIgnoreCase)) return t;
            }
        }
        foreach (var n in names)
        {
            foreach (var t in list)
            {
                if (t != null && t.gameObject.name.IndexOf(n, System.StringComparison.OrdinalIgnoreCase) >= 0) return t;
            }
        }
        return null;
    }

    private static Image FindImageRecursive(Transform root, params string[] names)
    {
        if (root == null) return null;
        var list = root.GetComponentsInChildren<Image>(true);
        foreach (var n in names)
        {
            foreach (var img in list)
            {
                if (img != null && img.gameObject.name.Equals(n, System.StringComparison.OrdinalIgnoreCase)) return img;
            }
        }
        return null;
    }

    private static Button FindButtonRecursive(Transform root, params string[] names)
    {
        if (root == null) return null;
        var list = root.GetComponentsInChildren<Button>(true);
        foreach (var n in names)
        {
            foreach (var b in list)
            {
                if (b != null && b.gameObject.name.Equals(n, System.StringComparison.OrdinalIgnoreCase)) return b;
            }
        }
        return null;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration, Action onComplete = null)
    {
        float elapsed = 0f;
        cg.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        cg.alpha = to;
        onComplete?.Invoke();
    }
}
