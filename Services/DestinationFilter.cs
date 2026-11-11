using TuTuc.Models;

namespace TuTuc.Services;

// Lọc địa điểm và dịch vụ theo điểm đến của chuyến đi (góp ý A14: chọn Đà Lạt thì không hiện nơi khác).
// Quy tắc:
//   1. Tìm địa điểm trùng tên điểm đến (ví dụ "Đà Lạt") để biết khu vực của nó ("Lâm Đồng").
//   2. Địa điểm thuộc điểm đến = địa điểm cùng khu vực đó (Đồi chè Cầu Đất, Chợ đêm Đà Lạt...).
//   3. Dịch vụ thuộc điểm đến = dịch vụ có nơi phục vụ trùng điểm đến hoặc trùng tên một địa điểm ở bước 2.
public static class DestinationFilter
{
    public static List<Place> PlacesAt(IEnumerable<Place> all, string destination)
    {
        var d = destination.Trim();
        var list = all.ToList();
        var regions = list.Where(p => Same(p.Name, d) || Same(p.Region, d)).Select(p => p.Region).ToHashSet();
        return list.Where(p => regions.Contains(p.Region) || Same(p.Name, d)).ToList();
    }

    public static List<TravelService> ServicesAt(IEnumerable<TravelService> all, IEnumerable<Place> placesAt, string destination)
    {
        var names = placesAt.Select(p => p.Name).Append(destination.Trim()).ToList();
        return all.Where(s => names.Any(n => Same(s.Location, n))).ToList();
    }

    // Dùng cho ô thứ hai của form thêm hoạt động: dịch vụ nào hiện khi khách đã chọn một địa điểm.
    // Dịch vụ phục vụ ngay tại địa điểm đó, hoặc phục vụ chung cho cả điểm đến (ví dụ thuê xe ở Đà Lạt).
    public static bool ServesPlace(TravelService s, Place p, string destination) =>
        Same(s.Location, p.Name) || Same(s.Location, destination);

    // Hai tên được coi là cùng nơi khi tên này chứa tên kia, không phân biệt hoa thường.
    public static bool Same(string a, string b)
    {
        a = a.Trim();
        b = b.Trim();
        if (a.Length == 0 || b.Length == 0) return false;
        return a.Contains(b, StringComparison.OrdinalIgnoreCase) || b.Contains(a, StringComparison.OrdinalIgnoreCase);
    }
}
