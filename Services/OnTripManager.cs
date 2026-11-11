using Microsoft.EntityFrameworkCore;
using TuTuc.Data;
using TuTuc.Models;

namespace TuTuc.Services;

// Mục tiêu 4 (IS4): khi đang ở điểm đến, khách có nhu cầu mới (ăn uống, di chuyển, chỗ ở, hoạt động).
// Trang "Hỗ trợ tại điểm đến" gom vào một chỗ:
//   1. Lịch trình hôm nay của chuyến đi.
//   2. Dịch vụ và địa điểm ngay tại điểm đến theo nhu cầu vừa phát sinh.
//   3. Lối tắt: thêm vào lịch trình, gửi yêu cầu dịch vụ, ghi nhận tình huống.
public class OnTripManager(AppDbContext db)
{
    public static readonly string[] Needs = ["Ăn uống", "Di chuyển", "Lưu trú", "Trải nghiệm"];

    public async Task<OnTripVm> Build(int me, int? tripId, string? need)
    {
        var today = JourneyRules.Today;
        var trips = await db.Trips.AsNoTracking()
            .Where(x => x.OwnerId == me && x.End >= today.AddDays(-1))
            .OrderBy(x => x.Start).ToListAsync();

        var vm = new OnTripVm { Trips = trips, Need = Needs.Contains(need) ? need! : "" };
        if (trips.Count == 0) return vm;

        // Ưu tiên chuyến đang diễn ra (chuyến có nhiều hoạt động hôm nay nhất), sau đó là chuyến gần nhất.
        var activitiesToday = await db.Activities.AsNoTracking()
            .Where(x => x.Start.Date == today && trips.Select(t => t.Id).Contains(x.TripId))
            .GroupBy(x => x.TripId).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
        vm.Trip = trips.FirstOrDefault(x => x.Id == tripId)
                  ?? trips.Where(x => x.Start.Date <= today && x.End.Date >= today)
                          .OrderByDescending(x => activitiesToday.GetValueOrDefault(x.Id)).FirstOrDefault()
                  ?? trips[0];
        var trip = vm.Trip;
        vm.IsOngoing = trip.Start.Date <= today && trip.End.Date >= today;
        vm.Day = vm.IsOngoing ? today : trip.Start.Date;

        vm.TodayActivities = await db.Activities.AsNoTracking()
            .Where(x => x.TripId == trip.Id && x.Start.Date == vm.Day)
            .OrderBy(x => x.Start).ToListAsync();
        vm.OpenIncidents = await db.Incidents.AsNoTracking()
            .CountAsync(x => x.TripId == trip.Id && x.Status != "Đã giải quyết" && x.Status != "Dừng xử lý");

        // Dịch vụ đã duyệt tại điểm đến của chuyến đi.
        var rows = await (from s in db.Services.AsNoTracking()
                          join p in db.Accounts.AsNoTracking() on s.OwnerId equals p.Id
                          where s.ApprovalStatus == "Đã duyệt" && s.IsPublished && p.ProviderStatus != "Đã thu hồi"
                          select new { s, p }).ToListAsync();
        var destination = trip.Destination.Trim();
        var local = rows
            .Where(x => x.s.Location.Contains(destination, StringComparison.OrdinalIgnoreCase)
                        || destination.Contains(x.s.Location, StringComparison.OrdinalIgnoreCase))
            .Where(x => vm.Need == "" || x.s.Category == vm.Need)
            .ToList();
        vm.Services = local
            .Select(x => new OnTripService
            {
                Service = x.s,
                CanRequest = ProviderRules.CanReceiveNew(x.s, x.p),
            })
            .OrderByDescending(x => x.CanRequest)
            .ThenBy(x => x.Service.Price)
            .ToList();

        // Địa điểm tham khảo tại cùng khu vực (không áp dụng khi tìm lưu trú/di chuyển).
        if (vm.Need is "" or "Trải nghiệm" or "Ăn uống")
            vm.Places = await db.Places.AsNoTracking()
                .Where(x => x.Name.Contains(destination) || x.Region.Contains(destination) || x.Description.Contains(destination))
                .OrderBy(x => x.Name).Take(6).ToListAsync();
        return vm;
    }
}

public class OnTripVm
{
    public List<Trip> Trips { get; set; } = [];
    public Trip? Trip { get; set; }
    public bool IsOngoing { get; set; }
    public DateTime Day { get; set; }
    public string Need { get; set; } = "";
    public List<TripActivity> TodayActivities { get; set; } = [];
    public int OpenIncidents { get; set; }
    public List<OnTripService> Services { get; set; } = [];
    public List<Place> Places { get; set; } = [];
}

public class OnTripService
{
    public TravelService Service { get; set; } = null!;
    public bool CanRequest { get; set; }
}
