namespace TuTuc.Services;

// Local illustration assets; presentation only, independent of booking rules.
public static class TravelPresentation
{
    public static string Cover(string? destination)
    {
        var name = destination ?? "";
        if (name.Contains("Ninh", StringComparison.OrdinalIgnoreCase) || name.Contains("Tràng", StringComparison.OrdinalIgnoreCase)) return "/images/ninhbinh.jpg";
        if (name.Contains("Hội An", StringComparison.OrdinalIgnoreCase) || name.Contains("Hạ Long", StringComparison.OrdinalIgnoreCase)) return "/images/vietnam.jpg";
        return "/images/dalat.jpg";
    }
}
