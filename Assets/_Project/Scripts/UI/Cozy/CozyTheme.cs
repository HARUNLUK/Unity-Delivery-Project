using UnityEngine;

/// <summary>
/// "Kurye Defteri" UI design tokens: warm paper surfaces, cocoa ink, four meaning colors.
/// Every screen (HUD, tablet, menus, shops) takes its colors from here.
/// </summary>
public static class CozyTheme
{
    // --- Surfaces ---
    public static readonly Color Paper = Hex("FFF7EA");
    public static readonly Color PaperEdge = Hex("E8D4B8");
    public static readonly Color Kraft = Hex("F1DDBE");
    public static readonly Color KraftDeep = Hex("D9B98C");
    public static readonly Color KraftStrip = Hex("E0BF8E");
    public static readonly Color TabIdle = Hex("E5CCA6");
    public static readonly Color Well = Hex("F8EBD6");
    public static readonly Color RowFace = Hex("FFFDF8");
    public static readonly Color Line = Hex("E6D3B7");
    public static readonly Color Track = Hex("EADCC6");
    public static readonly Color Scrim = new Color(0.17f, 0.12f, 0.23f, 0.55f);
    public static readonly Color ScrimStrong = new Color(0.12f, 0.08f, 0.17f, 0.78f);
    public static readonly Color SoftShadow = new Color(0.16f, 0.10f, 0.20f, 0.30f);

    // --- Ink ---
    public static readonly Color Ink = Hex("3B2C24");
    public static readonly Color InkSoft = Hex("7A6558");
    public static readonly Color White = Color.white;

    // --- Meaning colors (face / lip / tint / ink) ---
    public static readonly Color Mint = Hex("2F9B6F");
    public static readonly Color MintLip = Hex("217552");
    public static readonly Color MintTint = Hex("DDF3E7");
    public static readonly Color MintInk = Hex("1F7A53");

    public static readonly Color Sky = Hex("3E8FD0");
    public static readonly Color SkyLip = Hex("2C6DA3");
    public static readonly Color SkyTint = Hex("DCEBFA");
    public static readonly Color SkyInk = Hex("25639A");

    public static readonly Color Honey = Hex("F4B63F");
    public static readonly Color HoneyLip = Hex("C98E22");
    public static readonly Color HoneyTint = Hex("FFF0C2");
    public static readonly Color HoneyInk = Hex("8A6410");

    public static readonly Color Red = Hex("D9534A");
    public static readonly Color RedLip = Hex("A93A33");
    public static readonly Color RedTint = Hex("FBD9D5");
    public static readonly Color RedInk = Hex("B03A30");

    public static readonly Color OrangeTint = Hex("FFE3C7");
    public static readonly Color OrangeInk = Hex("B8561B");

    public static readonly Color PurpleInk = Hex("6B4A9A");

    public static readonly Color Off = Hex("E9E0D3");
    public static readonly Color OffLip = Hex("D6CABB");
    public static readonly Color OffInk = Hex("A5968A");

    // --- Radii (Unity px at 1920x1080) ---
    public const float RadiusPanel = 28f;
    public const float RadiusButton = 20f;
    public const float RadiusRow = 18f;
    public const float RadiusKey = 10f;

    public const float LipPanel = 7f;
    public const float LipButton = 6f;

    public enum ButtonStyle { Primary, Info, Warning, Danger, Soft, Ghost, Disabled }

    public enum Tone { Kraft, Mint, Sky, Honey, Red, Orange, Outline, Dark }

    public struct ButtonColors
    {
        public Color face;
        public Color lip;
        public Color text;
    }

    public static ButtonColors GetButtonColors(ButtonStyle style)
    {
        switch (style)
        {
            case ButtonStyle.Primary: return new ButtonColors { face = Mint, lip = MintLip, text = White };
            case ButtonStyle.Info: return new ButtonColors { face = Sky, lip = SkyLip, text = White };
            case ButtonStyle.Warning: return new ButtonColors { face = Honey, lip = HoneyLip, text = Ink };
            case ButtonStyle.Danger: return new ButtonColors { face = Red, lip = RedLip, text = White };
            case ButtonStyle.Soft: return new ButtonColors { face = RowFace, lip = KraftDeep, text = Ink };
            case ButtonStyle.Ghost: return new ButtonColors { face = new Color(1f, 1f, 1f, 0f), lip = new Color(0f, 0f, 0f, 0f), text = InkSoft };
            default: return new ButtonColors { face = Off, lip = OffLip, text = OffInk };
        }
    }

    public static void GetTone(Tone tone, out Color background, out Color foreground)
    {
        switch (tone)
        {
            case Tone.Mint: background = MintTint; foreground = MintInk; break;
            case Tone.Sky: background = SkyTint; foreground = SkyInk; break;
            case Tone.Honey: background = HoneyTint; foreground = HoneyInk; break;
            case Tone.Red: background = RedTint; foreground = RedInk; break;
            case Tone.Orange: background = OrangeTint; foreground = OrangeInk; break;
            case Tone.Outline: background = new Color(1f, 1f, 1f, 0f); foreground = InkSoft; break;
            case Tone.Dark: background = Ink; foreground = Paper; break;
            default: background = Kraft; foreground = Ink; break;
        }
    }

    public static Tone ToneFor(CargoType type)
    {
        switch (type)
        {
            case CargoType.Express: return Tone.Sky;
            case CargoType.Fragile: return Tone.Orange;
            case CargoType.Explosive: return Tone.Red;
            default: return Tone.Kraft;
        }
    }

    public static string IconFor(CargoType type)
    {
        switch (type)
        {
            case CargoType.Express: return "bolt";
            case CargoType.Fragile: return "fragile";
            case CargoType.Explosive: return "flame";
            default: return "box";
        }
    }

    /// <summary>Solid badge color (circle behind a white icon) for a cargo type.</summary>
    public static void BadgeFor(CargoType type, out Color background, out Color foreground)
    {
        switch (type)
        {
            case CargoType.Express: background = Sky; foreground = White; break;
            case CargoType.Fragile: background = OrangeTint; foreground = OrangeInk; break;
            case CargoType.Explosive: background = Red; foreground = White; break;
            default: background = Kraft; foreground = Ink; break;
        }
    }

    /// <summary>Pure C# parse (safe inside static initializers that may run off the main thread).</summary>
    public static Color Hex(string hex)
    {
        int v = System.Convert.ToInt32(hex.Substring(0, 6), 16);
        float a = hex.Length >= 8 ? System.Convert.ToInt32(hex.Substring(6, 2), 16) / 255f : 1f;
        return new Color(((v >> 16) & 0xFF) / 255f, ((v >> 8) & 0xFF) / 255f, (v & 0xFF) / 255f, a);
    }

    public static string ToHex(Color c)
    {
        int r = Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255);
        int g = Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255);
        int b = Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255);
        return r.ToString("X2") + g.ToString("X2") + b.ToString("X2");
    }
}
