using Microsoft.EntityFrameworkCore;
using TuTuc.Data;
using TuTuc.Models;

namespace TuTuc.Services;

// Mục tiêu 3 (IS3): giúp khách nhận ra người có điểm tương đồng.
// Hệ thống chấm "mức độ phù hợp" của từng nhu cầu đồng hành so với tiêu chí khách nhập:
//   Điểm đến trùng       : 35 điểm
//   Thời gian giao nhau  : 25 điểm
//   Ngân sách phù hợp    : 15 điểm
//   Cùng hình thức đi    : 10 điểm
//   Có sở thích chung    : 15 điểm
// Chỉ tính những tiêu chí có dữ liệu, rồi quy ra phần trăm.
// Sở thích chỉ được so khi người đăng cho phép xem hồ sơ (không lộ thông tin ngoài phạm vi chia sẻ).
public class CompanionSuggestion(AppDbContext db)
{
    public async Task Apply(CommunityPage page, int me)
    {
        var s = page.Search;
        var myInterests = me > 0
            ? await db.UserProfiles.AsNoTracking().Where(x => x.OwnerId == me).Select(x => x.Interests).SingleOrDefaultAsync()
            : null;

        foreach (var card in page.Demands)
            Score(card, s, myInterests);

        // Có tiêu chí thì đưa nhu cầu phù hợp nhất lên đầu.
        if (page.Demands.Any(x => x.MatchScore.HasValue))
            page.Demands = page.Demands.OrderByDescending(x => x.MatchScore ?? 0).ThenBy(x => x.Date).ToList();
    }

    public static void Score(DemandCard card, CommunitySearch s, string? myInterests)
    {
        int earned = 0, possible = 0;
        var reasons = new List<string>();

        if (!string.IsNullOrWhiteSpace(s.Q))
        {
            possible += 35;
            if (Same(card.Destination, s.Q)) { earned += 35; reasons.Add("Cùng điểm đến " + card.Destination); }
        }

        if (s.From.HasValue || s.To.HasValue)
        {
            possible += 25;
            var from = s.From ?? s.To!.Value;
            var to = s.To ?? s.From!.Value;
            var overlap = (Min(card.EndDate, to) - Max(card.Date, from)).Days + 1;
            if (overlap > 0) { earned += 25; reasons.Add($"Trùng {overlap} ngày đi"); }
        }

        if (s.MaxBudget.HasValue && card.Budget.HasValue)
        {
            possible += 15;
            if (card.Budget == 0 || card.Budget <= s.MaxBudget) { earned += 15; reasons.Add("Ngân sách trong khả năng"); }
        }

        if (!string.IsNullOrEmpty(s.TravelStyle))
        {
            possible += 10;
            if (card.TravelStyle == s.TravelStyle) { earned += 10; reasons.Add("Cùng hình thức " + card.TravelStyle); }
        }

        var common = CommonInterests(myInterests, card.Interests);
        if (!string.IsNullOrWhiteSpace(myInterests) && card.Interests != null)
        {
            possible += 15;
            if (common.Count > 0) { earned += 15; reasons.Add("Cùng sở thích: " + string.Join(", ", common)); }
        }

        // Chỉ tính phần trăm khi khách đã nhập ít nhất một tiêu chí tìm kiếm;
        // nếu chưa nhập gì thì chỉ hiện lý do (ví dụ sở thích chung) để tránh hiểu nhầm là "phù hợp 100%".
        var hasCriteria = !string.IsNullOrWhiteSpace(s.Q) || s.From.HasValue || s.To.HasValue || s.MaxBudget.HasValue || !string.IsNullOrEmpty(s.TravelStyle);
        card.MatchScore = !hasCriteria || possible == 0 ? null : (int)Math.Round(earned * 100.0 / possible);
        card.MatchReasons = reasons;
    }

    // Gợi ý tiêu chí tìm kiếm lấy từ các chuyến đi sắp tới của khách.
    public async Task<List<Trip>> UpcomingTrips(int me) =>
        await db.Trips.AsNoTracking()
            .Where(x => x.OwnerId == me && x.End >= JourneyRules.Today)
            .OrderBy(x => x.Start).Take(4).ToListAsync();

    static bool Same(string a, string b) =>
        a.Contains(b.Trim(), StringComparison.OrdinalIgnoreCase) || b.Trim().Contains(a, StringComparison.OrdinalIgnoreCase);

    static List<string> CommonInterests(string? mine, string? theirs)
    {
        static IEnumerable<string> Split(string? s) =>
            (s ?? "").Split([',', ';', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(x => x.ToLowerInvariant());
        return Split(mine).Intersect(Split(theirs)).Take(3).ToList();
    }

    static DateTime Min(DateTime a, DateTime b) => a < b ? a.Date : b.Date;
    static DateTime Max(DateTime a, DateTime b) => a > b ? a.Date : b.Date;
}
