using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public enum GameAction
{
    MoveForward,
    MoveBackward,
    MoveLeft,
    MoveRight,
    Sprint,
    Jump,
    Interact,
    Vehicle,
    Tablet,
    Camera,
    ThrowCargo,
    DropCargo
}

public enum InputDeviceType
{
    Keyboard,
    Mouse
}

public enum CustomMouseButton
{
    None = 0,
    Left = 1,
    Right = 2,
    Middle = 3,
    Forward = 4, // Mouse 4 / Browser Forward
    Back = 5     // Mouse 5 / Browser Back
}

[Serializable]
public struct InputBindingData : IEquatable<InputBindingData>
{
    public InputDeviceType device;
    public Key keyboardKey;
    public CustomMouseButton mouseButton;

    public static InputBindingData FromKey(Key key) => new InputBindingData { device = InputDeviceType.Keyboard, keyboardKey = key, mouseButton = CustomMouseButton.None };
    public static InputBindingData FromMouse(CustomMouseButton btn) => new InputBindingData { device = InputDeviceType.Mouse, keyboardKey = Key.None, mouseButton = btn };
    public static InputBindingData None => new InputBindingData { device = InputDeviceType.Keyboard, keyboardKey = Key.None, mouseButton = CustomMouseButton.None };

    public bool IsAssigned => (device == InputDeviceType.Keyboard && keyboardKey != Key.None) ||
                              (device == InputDeviceType.Mouse && mouseButton != CustomMouseButton.None);

    public string GetDisplayName()
    {
        if (device == InputDeviceType.Mouse)
        {
            switch (mouseButton)
            {
                case CustomMouseButton.Left: return LocalizationManager.Get("mouse_btn_left", "Sol Tık (Mouse 0)");
                case CustomMouseButton.Right: return LocalizationManager.Get("mouse_btn_right", "Sağ Tık (Mouse 1)");
                case CustomMouseButton.Middle: return LocalizationManager.Get("mouse_btn_middle", "Orta Tık (Mouse 2)");
                case CustomMouseButton.Forward: return LocalizationManager.Get("mouse_btn_forward", "Fare İleri (Mouse 4)");
                case CustomMouseButton.Back: return LocalizationManager.Get("mouse_btn_back", "Fare Geri (Mouse 5)");
                default: return LocalizationManager.Get("btn_unassigned", "Atanmadı");
            }
        }
        else
        {
            if (keyboardKey == Key.None) return LocalizationManager.Get("btn_unassigned", "Atanmadı");
            return KeyBindingManager.GetKeyDisplayName(keyboardKey);
        }
    }

    public bool Equals(InputBindingData other)
    {
        if (device != other.device) return false;
        if (device == InputDeviceType.Mouse) return mouseButton == other.mouseButton && mouseButton != CustomMouseButton.None;
        if (device == InputDeviceType.Keyboard) return keyboardKey == other.keyboardKey && keyboardKey != Key.None;
        return false;
    }

    public override bool Equals(object obj)
    {
        return obj is InputBindingData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)device, (int)keyboardKey, (int)mouseButton);
    }
}

/// <summary>
/// Central manager for configurable keybindings (Keyboard & Mouse).
/// Automatically frees/unbinds conflicting keys when reassigning, and persists in PlayerPrefs.
/// </summary>
public static class KeyBindingManager
{
    private const string PREFS_DEV_PREFIX = "Settings_InputDev_";
    private const string PREFS_KEY_PREFIX = "Settings_InputKey_";
    private const string PREFS_MOUSE_PREFIX = "Settings_InputMouse_";

    private static readonly Dictionary<GameAction, InputBindingData> defaultBindings = new Dictionary<GameAction, InputBindingData>
    {
        { GameAction.MoveForward, InputBindingData.FromKey(Key.W) },
        { GameAction.MoveBackward, InputBindingData.FromKey(Key.S) },
        { GameAction.MoveLeft, InputBindingData.FromKey(Key.A) },
        { GameAction.MoveRight, InputBindingData.FromKey(Key.D) },
        { GameAction.Sprint, InputBindingData.FromKey(Key.LeftShift) },
        { GameAction.Jump, InputBindingData.FromKey(Key.Space) },
        { GameAction.Interact, InputBindingData.FromKey(Key.E) },
        { GameAction.Vehicle, InputBindingData.FromKey(Key.F) },
        { GameAction.Tablet, InputBindingData.FromKey(Key.Tab) },
        { GameAction.Camera, InputBindingData.FromKey(Key.V) },
        { GameAction.ThrowCargo, InputBindingData.FromMouse(CustomMouseButton.Left) },
        { GameAction.DropCargo, InputBindingData.FromMouse(CustomMouseButton.Right) }
    };

    private static readonly Dictionary<GameAction, (string locKey, string fallback)> actionLocalizationMap = new Dictionary<GameAction, (string, string)>
    {
        { GameAction.MoveForward, ("action_move_forward", "İleri Yürüme / Gaza Bas") },
        { GameAction.MoveBackward, ("action_move_backward", "Geri Yürüme / Fren") },
        { GameAction.MoveLeft, ("action_move_left", "Sola Dön / Sol Direksiyon") },
        { GameAction.MoveRight, ("action_move_right", "Sağa Dön / Sağ Direksiyon") },
        { GameAction.Sprint, ("action_sprint", "Hızlı Koşma (Sprint)") },
        { GameAction.Jump, ("action_jump", "Zıplama / Araç El Freni") },
        { GameAction.Interact, ("action_interact", "Etkileşim / Kargo Alma / Dükkan") },
        { GameAction.Vehicle, ("action_vehicle", "Araca Binme / Araçtan İnme") },
        { GameAction.Tablet, ("action_tablet", "Kargo Tableti / GPS Harita") },
        { GameAction.Camera, ("action_camera", "Kamera Modu (FPS / TPS)") },
        { GameAction.ThrowCargo, ("action_throw_cargo", "Koli Fırlatma (Şarjlı)") },
        { GameAction.DropCargo, ("action_drop_cargo", "Koliyi Yavaşça Bırakma") }
    };

    private static Dictionary<GameAction, InputBindingData> currentBindings = new Dictionary<GameAction, InputBindingData>();
    private static bool isInitialized = false;

    public static event Action OnBindingsChanged;

    public static void EnsureInitialized()
    {
        if (isInitialized) return;
        LoadAllBindings();
        isInitialized = true;
    }

    public static string GetActionDescription(GameAction action)
    {
        if (actionLocalizationMap.TryGetValue(action, out var item))
        {
            return LocalizationManager.Get(item.locKey, item.fallback);
        }
        return action.ToString();
    }

    public static InputBindingData GetBinding(GameAction action)
    {
        EnsureInitialized();
        if (currentBindings.TryGetValue(action, out InputBindingData binding))
        {
            return binding;
        }
        return defaultBindings.TryGetValue(action, out InputBindingData def) ? def : InputBindingData.None;
    }

    public static Key GetKey(GameAction action)
    {
        var binding = GetBinding(action);
        return binding.device == InputDeviceType.Keyboard ? binding.keyboardKey : Key.None;
    }

    /// <summary>
    /// Sets a new binding for an action.
    /// If another action was already using this exact binding, it is automatically unbound (boşa çıkarılır).
    /// </summary>
    public static void SetBinding(GameAction action, InputBindingData newBinding)
    {
        EnsureInitialized();
        if (!newBinding.IsAssigned) return;

        // 1. Unbind any conflicting action that already had this key/button
        List<GameAction> conflictedActions = new List<GameAction>();
        foreach (var kvp in currentBindings)
        {
            if (kvp.Key != action && kvp.Value.Equals(newBinding))
            {
                conflictedActions.Add(kvp.Key);
            }
        }

        foreach (var conflict in conflictedActions)
        {
            currentBindings[conflict] = InputBindingData.None;
            SaveBindingToPrefs(conflict, InputBindingData.None);
            Debug.Log($"<color=#FFAA00>[KeyBindingManager] '{newBinding.GetDisplayName()}' tuşu daha önce '{GetActionDescription(conflict)}' eyleminde kullanılıyordu; boşa çıkarıldı ve '{GetActionDescription(action)}' eylemine atandı.</color>");
        }

        // 2. Apply new binding to target action
        currentBindings[action] = newBinding;
        SaveBindingToPrefs(action, newBinding);
        PlayerPrefs.Save();

        OnBindingsChanged?.Invoke();
    }

    public static void SetKey(GameAction action, Key newKey)
    {
        if (newKey == Key.None || newKey == Key.Escape) return;
        SetBinding(action, InputBindingData.FromKey(newKey));
    }

    public static void SetMouse(GameAction action, CustomMouseButton mouseBtn)
    {
        if (mouseBtn == CustomMouseButton.None) return;
        SetBinding(action, InputBindingData.FromMouse(mouseBtn));
    }

    public static void ResetToDefaults()
    {
        EnsureInitialized();
        foreach (var kvp in defaultBindings)
        {
            currentBindings[kvp.Key] = kvp.Value;
            SaveBindingToPrefs(kvp.Key, kvp.Value);
        }
        PlayerPrefs.Save();
        OnBindingsChanged?.Invoke();
    }

    public static void LoadAllBindings()
    {
        currentBindings.Clear();
        foreach (var kvp in defaultBindings)
        {
            currentBindings[kvp.Key] = LoadBindingFromPrefs(kvp.Key, kvp.Value);
        }
    }

    private static void SaveBindingToPrefs(GameAction action, InputBindingData binding)
    {
        string actName = action.ToString();
        PlayerPrefs.SetInt(PREFS_DEV_PREFIX + actName, (int)binding.device);
        PlayerPrefs.SetInt(PREFS_KEY_PREFIX + actName, (int)binding.keyboardKey);
        PlayerPrefs.SetInt(PREFS_MOUSE_PREFIX + actName, (int)binding.mouseButton);
    }

    private static InputBindingData LoadBindingFromPrefs(GameAction action, InputBindingData defaultVal)
    {
        string actName = action.ToString();
        if (!PlayerPrefs.HasKey(PREFS_DEV_PREFIX + actName)) return defaultVal;

        InputDeviceType dev = (InputDeviceType)PlayerPrefs.GetInt(PREFS_DEV_PREFIX + actName, (int)defaultVal.device);
        Key key = (Key)PlayerPrefs.GetInt(PREFS_KEY_PREFIX + actName, (int)defaultVal.keyboardKey);
        CustomMouseButton mouse = (CustomMouseButton)PlayerPrefs.GetInt(PREFS_MOUSE_PREFIX + actName, (int)defaultVal.mouseButton);

        return new InputBindingData { device = dev, keyboardKey = key, mouseButton = mouse };
    }

    public static string GetKeyDisplayName(Key key)
    {
        switch (key)
        {
            case Key.LeftShift: return "L-Shift";
            case Key.RightShift: return "R-Shift";
            case Key.LeftCtrl: return "L-Ctrl";
            case Key.RightCtrl: return "R-Ctrl";
            case Key.LeftAlt: return "L-Alt";
            case Key.RightAlt: return "R-Alt";
            case Key.Space: return "Space (Boşluk)";
            case Key.Tab: return "Tab";
            case Key.Enter: return "Enter";
            case Key.Escape: return "Esc";
            case Key.Backspace: return "Geri Sil";
            case Key.CapsLock: return "Caps Lock";
            case Key.UpArrow: return "Yukarı Ok";
            case Key.DownArrow: return "Aşağı Ok";
            case Key.LeftArrow: return "Sol Ok";
            case Key.RightArrow: return "Sağ Ok";
            case Key.Digit0: return "0";
            case Key.Digit1: return "1";
            case Key.Digit2: return "2";
            case Key.Digit3: return "3";
            case Key.Digit4: return "4";
            case Key.Digit5: return "5";
            case Key.Digit6: return "6";
            case Key.Digit7: return "7";
            case Key.Digit8: return "8";
            case Key.Digit9: return "9";
            case Key.Numpad0: return "Num 0";
            case Key.Numpad1: return "Num 1";
            case Key.Numpad2: return "Num 2";
            case Key.Numpad3: return "Num 3";
            case Key.Numpad4: return "Num 4";
            case Key.Numpad5: return "Num 5";
            case Key.Numpad6: return "Num 6";
            case Key.Numpad7: return "Num 7";
            case Key.Numpad8: return "Num 8";
            case Key.Numpad9: return "Num 9";
            default:
                string s = key.ToString();
                return s.Length == 1 ? s.ToUpper() : s;
        }
    }

    #region --- INPUT POLLING HELPERS ---

    public static bool IsPressed(GameAction action)
    {
        EnsureInitialized();
        if (!currentBindings.TryGetValue(action, out InputBindingData binding)) return false;
        if (!binding.IsAssigned) return false;

        if (binding.device == InputDeviceType.Mouse)
        {
            if (Mouse.current == null) return false;
            switch (binding.mouseButton)
            {
                case CustomMouseButton.Left: return Mouse.current.leftButton.isPressed;
                case CustomMouseButton.Right: return Mouse.current.rightButton.isPressed;
                case CustomMouseButton.Middle: return Mouse.current.middleButton.isPressed;
                case CustomMouseButton.Forward: return Mouse.current.forwardButton.isPressed;
                case CustomMouseButton.Back: return Mouse.current.backButton.isPressed;
                default: return false;
            }
        }
        else
        {
            if (Keyboard.current == null) return false;
            KeyControl kc = Keyboard.current[binding.keyboardKey];
            if (kc != null && kc.isPressed) return true;

            // Fallback secondary keys
            if (action == GameAction.MoveForward && Keyboard.current.upArrowKey.isPressed) return true;
            if (action == GameAction.MoveBackward && Keyboard.current.downArrowKey.isPressed) return true;
            if (action == GameAction.MoveLeft && Keyboard.current.leftArrowKey.isPressed) return true;
            if (action == GameAction.MoveRight && Keyboard.current.rightArrowKey.isPressed) return true;
            if (action == GameAction.Sprint && Keyboard.current.rightShiftKey.isPressed) return true;

            return false;
        }
    }

    public static bool WasPressedThisFrame(GameAction action)
    {
        EnsureInitialized();
        if (!currentBindings.TryGetValue(action, out InputBindingData binding)) return false;
        if (!binding.IsAssigned) return false;

        if (binding.device == InputDeviceType.Mouse)
        {
            if (Mouse.current == null) return false;
            switch (binding.mouseButton)
            {
                case CustomMouseButton.Left: return Mouse.current.leftButton.wasPressedThisFrame;
                case CustomMouseButton.Right: return Mouse.current.rightButton.wasPressedThisFrame;
                case CustomMouseButton.Middle: return Mouse.current.middleButton.wasPressedThisFrame;
                case CustomMouseButton.Forward: return Mouse.current.forwardButton.wasPressedThisFrame;
                case CustomMouseButton.Back: return Mouse.current.backButton.wasPressedThisFrame;
                default: return false;
            }
        }
        else
        {
            if (Keyboard.current == null) return false;
            KeyControl kc = Keyboard.current[binding.keyboardKey];
            if (kc != null && kc.wasPressedThisFrame) return true;

            if (action == GameAction.MoveForward && Keyboard.current.upArrowKey.wasPressedThisFrame) return true;
            if (action == GameAction.MoveBackward && Keyboard.current.downArrowKey.wasPressedThisFrame) return true;
            if (action == GameAction.MoveLeft && Keyboard.current.leftArrowKey.wasPressedThisFrame) return true;
            if (action == GameAction.MoveRight && Keyboard.current.rightArrowKey.wasPressedThisFrame) return true;
            if (action == GameAction.Jump && Keyboard.current.spaceKey.wasPressedThisFrame) return true;

            return false;
        }
    }

    public static bool WasReleasedThisFrame(GameAction action)
    {
        EnsureInitialized();
        if (!currentBindings.TryGetValue(action, out InputBindingData binding)) return false;
        if (!binding.IsAssigned) return false;

        if (binding.device == InputDeviceType.Mouse)
        {
            if (Mouse.current == null) return false;
            switch (binding.mouseButton)
            {
                case CustomMouseButton.Left: return Mouse.current.leftButton.wasReleasedThisFrame;
                case CustomMouseButton.Right: return Mouse.current.rightButton.wasReleasedThisFrame;
                case CustomMouseButton.Middle: return Mouse.current.middleButton.wasReleasedThisFrame;
                case CustomMouseButton.Forward: return Mouse.current.forwardButton.wasReleasedThisFrame;
                case CustomMouseButton.Back: return Mouse.current.backButton.wasReleasedThisFrame;
                default: return false;
            }
        }
        else
        {
            if (Keyboard.current == null) return false;
            KeyControl kc = Keyboard.current[binding.keyboardKey];
            if (kc != null && kc.wasReleasedThisFrame) return true;

            return false;
        }
    }

    #endregion
}
