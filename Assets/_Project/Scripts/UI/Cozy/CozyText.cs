using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Text helpers for the paper UI: turns legacy neon rich-text colors into readable ink tones
/// and draws [E]-style key hints as small dark key caps.
/// </summary>
public static class CozyText
{
    // Key caps are written with this exact color so the remapper can recognise and skip them.
    public const string KeyCapTextHex = "FFF7E9";

    private static readonly Regex ColorTag = new Regex(@"<(color=)?#([0-9A-Fa-f]{6})([0-9A-Fa-f]{2})?>", RegexOptions.Compiled);
    private static readonly Regex KeyToken = new Regex(@"\[([^\[\]<>]{1,14})\]", RegexOptions.Compiled);

    private static readonly string[] KnownKeyWords =
    {
        "sol tık", "sağ tık", "lmb", "rmb", "tab", "esc", "space", "boşluk", "shift", "l-shift", "enter", "ctrl", "alt",
        "left click", "right click", "mouse 0", "mouse 1"
    };

    /// <summary>Full treatment for text that sits on a light paper surface.</summary>
    public static string ForPaper(string text, bool formatKeys)
    {
        if (string.IsNullOrEmpty(text)) return text;
        string result = RemapColorTags(text);
        if (formatKeys) result = FormatKeys(result);
        return result;
    }

    public static string RemapColorTags(string text)
    {
        if (text.IndexOf('#') < 0) return text;
        return ColorTag.Replace(text, m =>
        {
            string hex = m.Groups[2].Value.ToUpperInvariant();
            if (hex == KeyCapTextHex) return m.Value;
            Color c = CozyTheme.Hex(hex);
            Color mapped = RemapColorForPaper(c);
            if (mapped == c) return m.Value;
            return "<color=#" + CozyTheme.ToHex(mapped) + ">";
        });
    }

    /// <summary>Bright / neon / white colors become ink tones that read on cream paper; dark colors stay.</summary>
    public static Color RemapColorForPaper(Color c)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);

        if (s < 0.22f)
        {
            if (v > 0.9f) return new Color(CozyTheme.Ink.r, CozyTheme.Ink.g, CozyTheme.Ink.b, c.a);
            if (v > 0.55f) return new Color(CozyTheme.InkSoft.r, CozyTheme.InkSoft.g, CozyTheme.InkSoft.b, c.a);
            return c;
        }

        if (v <= 0.62f) return c; // already a dark, readable tone

        float deg = h * 360f;
        Color target;
        if (deg < 15f || deg >= 340f) target = CozyTheme.RedInk;
        else if (deg < 38f) target = CozyTheme.OrangeInk;
        else if (deg < 70f) target = CozyTheme.HoneyInk;
        else if (deg < 170f) target = CozyTheme.MintInk;
        else if (deg < 260f) target = CozyTheme.SkyInk;
        else target = CozyTheme.PurpleInk;

        target.a = c.a;
        return target;
    }

    /// <summary>"[E] Koliyi al" -> dark key cap "E" followed by the text.</summary>
    public static string FormatKeys(string text)
    {
        if (text.IndexOf('[') < 0) return text;
        return KeyToken.Replace(text, m =>
        {
            string inner = m.Groups[1].Value.Trim();
            if (!LooksLikeKey(inner)) return m.Value;
            return "<mark=#3B2C24><color=#" + KeyCapTextHex + "><b> " + inner.ToUpperInvariant() + " </b></color></mark>";
        });
    }

    private static bool LooksLikeKey(string inner)
    {
        if (inner.Length == 0) return false;
        if (inner.IndexOf('$') >= 0 || inner.IndexOf('%') >= 0) return false;
        if (inner.Length <= 3) return true; // E, F, F1, TAB-like tokens
        string lower = inner.ToLowerInvariant();
        foreach (string k in KnownKeyWords)
        {
            if (lower == k || lower.StartsWith(k + " ")) return true;
        }
        return false;
    }

    public static string Money(int amount)
    {
        string abs = "$" + Mathf.Abs(amount).ToString("N0");
        return amount < 0 ? "−" + abs : abs;
    }
}
