using System.Net;
using System.Text.RegularExpressions;
using Integration.Test;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TuTuc.Data;
using TuTuc.Models;
using Xunit;

namespace Integration.Test;

public class IT02_RowVersionCu_Tests
{
    [Fact]
    public async Task IT_02_ThemHoatDong_BangRowVersionCu()
    {
        const string id = "IT-02";
        const string title = "GD-01 | Tu choi RowVersion cu";
        const string pre = "KH01 da dang nhap; KHP01 co R1; sau lan them dau da sinh R2";
        const string input = "Gui them hoat dong 16:00 bang RowVersion R1 cu";
        const string expected = "HTTP 409; thong bao theo dac ta; so HD khong doi; khong co HD 16:00";
        var actual = new List<string>();
        var errors = new List<string>();
        void Check(bool passed, string name, string observed)
        {
            actual.Add($"{name}={observed}");
            if (!passed) errors.Add($"{name}: {observed}");
        }

        try
        {
            using var factory = new IntegrationTestFactory();
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
            int tripId, placeId;
            byte[] r1;
            var day = new DateTime(2026, 12, 5);
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = await db.Accounts.SingleAsync(x => x.Email == "khach@demo.vn");
                var place = new Place { Name = "Hồ Xuân Hương", Region = "Đà Lạt", Category = "Tham quan", Description = "IT-02" };
                var trip = new Trip { OwnerId = user.Id, Name = "KHP01 - IT02", Destination = "Đà Lạt", Start = day, End = day.AddDays(3), Budget = 3_000_000, Status = "Đã chấp nhận",
                    Activities = new List<TripActivity> { new() { Title = "HD dau", Start = day.AddHours(8), End = day.AddHours(9), HasTime = true, SortOrder = 1 } } };
                db.Places.Add(place); db.Trips.Add(trip);
                await db.SaveChangesAsync();
                tripId = trip.Id; placeId = place.Id; r1 = trip.Version.ToArray();
            }
            var loginHtml = await client.GetStringAsync("/Account/Login");
            var login = await client.PostAsync("/Account/Login", Fields(new()
            {
                ["Email"] = "khach@demo.vn", ["Password"] = "Demo12345", ["__RequestVerificationToken"] = Token(loginHtml)
            }));
            Check((int)login.StatusCode == 302, "Dang nhap", $"HTTP {(int)login.StatusCode}");
            var detailHtml = await client.GetStringAsync($"/Journey/Details/{tripId}");
            var antiforgery = Token(detailHtml);
            Dictionary<string, string> Input(DateTime hour, string version) => new()
            {
                ["TripId"] = tripId.ToString(), ["Id"] = "0", ["Version"] = version,
                ["SourceType"] = "Place", ["SourceId"] = placeId.ToString(),
                ["Start"] = hour.ToString("yyyy-MM-ddTHH:mm:ss"),
                ["End"] = hour.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                ["HasTime"] = "true", ["SortOrder"] = "1", ["Cost"] = "50000",
                ["__RequestVerificationToken"] = antiforgery
            };
            var first = await client.PostAsync("/Journey/Activity", Fields(Input(day.AddHours(14), Convert.ToBase64String(r1))));
            Check((int)first.StatusCode == 302, "Lan 1 hop le", $"HTTP {(int)first.StatusCode}");
            int beforeCount;
            byte[] r2;
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var trip = await db.Trips.AsNoTracking().Include(x => x.Activities).SingleAsync(x => x.Id == tripId);
                beforeCount = trip.Activities.Count; r2 = trip.Version.ToArray();
            }
            Check(!r1.SequenceEqual(r2), "R1 thanh R2", r1.SequenceEqual(r2) ? "Khong doi" : "Da doi");
            Check(beforeCount == 2, "Tien DK so HD", beforeCount.ToString());
            var second = await client.PostAsync("/Journey/Activity", Fields(Input(day.AddHours(16), Convert.ToBase64String(r1))));
            Check((int)second.StatusCode == 409, "HTTP tu choi R1", $"HTTP {(int)second.StatusCode}");
            var html = await second.Content.ReadAsStringAsync();
            Check(html.Contains("Kế hoạch đã được thay đổi ở phiên khác, vui lòng tải lại", StringComparison.OrdinalIgnoreCase),
                "Thong bao theo dac ta", html.Contains("Phiên bản kế hoạch không hợp lệ", StringComparison.OrdinalIgnoreCase)
                    ? "Web bao: Phien ban ke hoach khong hop le" : "Khong tim thay thong bao dac ta");
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var trip = await db.Trips.AsNoTracking().Include(x => x.Activities).SingleAsync(x => x.Id == tripId);
                Check(trip.Activities.Count == beforeCount, "So HD khong doi", trip.Activities.Count.ToString());
                Check(!trip.Activities.Any(x => x.Start == day.AddHours(16)), "Khong co HD 16:00", trip.Activities.Any(x => x.Start == day.AddHours(16)) ? "Co HD sai" : "Khong co");
                Check(trip.Version.SequenceEqual(r2), "Version khong doi sau loi", trip.Version.SequenceEqual(r2) ? "Giu R2" : "Da doi");
            }
        }
        catch (Exception ex) { errors.Add("Ngoai le: " + ex.GetType().Name + " - " + ex.Message); }
        finally { IntegrationTestReporter.Save(id, title, pre, input, expected, string.Join("; ", actual), string.Join(" | ", errors)); }
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }

    private static FormUrlEncodedContent Fields(Dictionary<string, string> values) => new(values);
    private static string Token(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]*)\"");
        if (!match.Success) throw new InvalidOperationException("Khong tim thay token Antiforgery");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
}
