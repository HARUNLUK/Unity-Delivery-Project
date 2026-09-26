using System.Collections.Generic;

public enum TutorialKind { Coach, Modal }

public enum TutorialBadge { Honey, Sky, Mint, Red, Orange }

public class TutorialPage
{
    public string titleKey;   // null = use the tutorial's own title
    public string textKey;
    public float seconds;
}

public class TutorialRow
{
    public string icon;
    public TutorialBadge badge;
    public string textKey;
}

public class TutorialDef
{
    public string id;
    public string icon;
    public TutorialBadge badge;
    public TutorialKind kind = TutorialKind.Coach;
    public string titleKey;
    public string subKey;            // one-line "when does this appear" for the F1 guide list
    public string subtitleKey;       // modal only
    public bool showTimer = true;    // countdown bar; off for goal-based tips (they end when the goal is met)
    public List<TutorialPage> pages = new List<TutorialPage>();
    public List<TutorialRow> rows = new List<TutorialRow>();   // modal only
}

/// <summary>
/// The first-hours tutorial set. Order here is the order in the F1 guide list.
/// Every text lives in the localization files (keys tut_*), see TutorialTexts.
/// </summary>
public static class TutorialCatalog
{
    public const string Pickup = "pickup";
    public const string Clue = "clue";
    public const string Vehicle = "vehicle";
    public const string FirstDelivery = "first_delivery";
    public const string DayEnd = "day_end";
    public const string BranchUpgrade = "branch_upgrade";
    public const string LowFuel = "low_fuel";
    public const string LowCondition = "low_condition";

    public static readonly TutorialDef[] All =
    {
        new TutorialDef
        {
            id = Pickup, icon = "box", badge = TutorialBadge.Honey, titleKey = "tut_pickup_title", subKey = "tut_pickup_sub",
            showTimer = false, // stays until the player picks the parcel up
            pages = { new TutorialPage { textKey = "tut_pickup_p1", seconds = 90f } }
        },
        new TutorialDef
        {
            id = Clue, icon = "pin", badge = TutorialBadge.Sky, titleKey = "tut_clue_title", subKey = "tut_clue_sub",
            pages =
            {
                new TutorialPage { textKey = "tut_clue_p1", seconds = 13f },
                new TutorialPage { titleKey = "tut_clue_p2_title", textKey = "tut_clue_p2", seconds = 10f },
            }
        },
        new TutorialDef
        {
            id = Vehicle, icon = "truck", badge = TutorialBadge.Mint, titleKey = "tut_vehicle_title", subKey = "tut_vehicle_sub",
            pages =
            {
                new TutorialPage { textKey = "tut_vehicle_p1", seconds = 11f },
                new TutorialPage { titleKey = "tut_vehicle_p2_title", textKey = "tut_vehicle_p2", seconds = 12f },
            }
        },
        new TutorialDef
        {
            id = FirstDelivery, icon = "check", badge = TutorialBadge.Mint, titleKey = "tut_first_delivery_title", subKey = "tut_first_delivery_sub",
            pages = { new TutorialPage { textKey = "tut_first_delivery_p1", seconds = 16f } }
        },
        new TutorialDef
        {
            id = DayEnd, icon = "receipt", badge = TutorialBadge.Honey, kind = TutorialKind.Modal,
            titleKey = "tut_day_end_title", subtitleKey = "tut_day_end_subtitle", subKey = "tut_day_end_sub",
            rows =
            {
                new TutorialRow { icon = "check", badge = TutorialBadge.Mint, textKey = "tut_day_end_r1" },
                new TutorialRow { icon = "x", badge = TutorialBadge.Red, textKey = "tut_day_end_r2" },
                new TutorialRow { icon = "home", badge = TutorialBadge.Honey, textKey = "tut_day_end_r3" },
            }
        },
        new TutorialDef
        {
            id = BranchUpgrade, icon = "star", badge = TutorialBadge.Honey, titleKey = "tut_branch_upgrade_title", subKey = "tut_branch_upgrade_sub",
            pages = { new TutorialPage { textKey = "tut_branch_upgrade_p1", seconds = 18f } }
        },
        new TutorialDef
        {
            id = LowFuel, icon = "fuel", badge = TutorialBadge.Red, titleKey = "tut_low_fuel_title", subKey = "tut_low_fuel_sub",
            pages = { new TutorialPage { textKey = "tut_low_fuel_p1", seconds = 15f } }
        },
        new TutorialDef
        {
            id = LowCondition, icon = "wrench", badge = TutorialBadge.Orange, titleKey = "tut_low_condition_title", subKey = "tut_low_condition_sub",
            pages = { new TutorialPage { textKey = "tut_low_condition_p1", seconds = 15f } }
        },
    };

    public static TutorialDef Get(string id)
    {
        foreach (TutorialDef d in All)
        {
            if (d.id == id) return d;
        }
        return null;
    }
}
