using TMPro;
using UnityEngine;

/// <summary>
/// Put on texts that sit on a light paper surface. Game scripts and localization strings still write
/// neon colors (e.g. &lt;color=#32FF64&gt;) meant for the old dark UI; this keeps them readable by mapping
/// them to ink tones, and turns [E]-style hints into key caps.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class CozyInkText : MonoBehaviour
{
    public bool formatKeys = true;
    public bool remapVertexColor = true;

    private TMP_Text tmp;
    private string lastProcessed;
    private Color lastColor = new Color(-1f, -1f, -1f, -1f);

    private void Awake()
    {
        tmp = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        Process();
    }

    private void LateUpdate()
    {
        Process();
    }

    public void Process()
    {
        if (tmp == null) tmp = GetComponent<TMP_Text>();
        if (tmp == null) return;

        string current = tmp.text;
        if (current != null && !ReferenceEquals(current, lastProcessed))
        {
            string fixedText = CozyText.ForPaper(current, formatKeys);
            if (!string.Equals(fixedText, current, System.StringComparison.Ordinal))
            {
                tmp.text = fixedText;
            }
            lastProcessed = tmp.text;
        }

        if (remapVertexColor && tmp.color != lastColor)
        {
            Color mapped = CozyText.RemapColorForPaper(tmp.color);
            if (mapped != tmp.color) tmp.color = mapped;
            lastColor = tmp.color;
        }
    }
}
