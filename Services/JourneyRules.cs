using TuTuc.Models;

namespace TuTuc.Services;

// Pure rules: independent of HTTP and SQL, so testers can exercise each boundary.
public static class JourneyRules
{
    public static DateTime Today => DateTime.UtcNow.AddHours(7).Date;

    public static string ValidateTrip(TripInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 150)
            return "Tên chuyến đi bắt buộc, tối đa 150 ký tự.";
        if (string.IsNullOrWhiteSpace(input.Destination) || input.Destination.Length > 200)
            return "Điểm đến bắt buộc, tối đa 200 ký tự.";
        if (input.Start == default || input.End == default || input.End.Date < input.Start.Date)
            return "Ngày bắt đầu và kết thúc chưa hợp lệ.";
        if (input.Budget < 0 || input.Budget > 1_000_000_000)
            return "Ngân sách phải từ 0 đến 1 tỷ đồng.";
        return "";
    }

    public static string ValidateActivity(ActivityInput input, Trip trip)
    {
        if (input.Start == default || input.End == default || input.Start.Date < trip.Start.Date ||
            input.Start.Date > trip.End.Date || input.End.Date > trip.End.Date ||
            input.End.Date < input.Start.Date || input.HasTime && input.End <= input.Start)
            return "Ngày hoạt động phải nằm trong chuyến đi; giờ kết thúc phải sau giờ bắt đầu.";
        if (input.Cost < 0 || input.Cost > 1_000_000_000 || input.SortOrder < 1)
            return "Chi phí từ 0 đến 1 tỷ đồng; thứ tự phải từ 1 trở lên.";
        if ((input.Title ?? "").Length > 300 || (input.Location ?? "").Length > 300 || (input.Notes ?? "").Length > 1000)
            return "Nội dung và địa điểm tối đa 300 ký tự; ghi chú tối đa 1000 ký tự.";
        if (input.SourceType is not ("Manual" or "Place" or "Service"))
            return "Loại hoạt động không hợp lệ.";
        if (input.SourceType == "Manual" && string.IsNullOrWhiteSpace(input.Title))
            return "Nhập nội dung hoạt động hoặc chọn một mục tham khảo.";
        if (input.SourceType != "Manual" && (!input.SourceId.HasValue || input.SourceId <= 0))
            return "Chọn một nguồn tham khảo hợp lệ.";
        return "";
    }
}
