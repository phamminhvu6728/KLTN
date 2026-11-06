using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TuTuc.Data;
using TuTuc.Models;
using Xunit;

namespace Integration.Test;

public class IT03_XoaHoatDongCoTinhHuong_Tests
{
    [Fact]
    public async Task IT_03_KhongXoaHoatDongDaGanTinhHuong()
    {
        const string id = "IT-03";
        const string title = "FK | Xoa HD da gan tinh huong va HD chua gan";
        const string pre = "KH01; KHP01 co HD1 gan TH01; HD2 khong gan tinh huong";
        const string input = "(a) Xoa HD1; (b) doc Version moi va xoa HD2";
        const string expected = "(a) HTTP 422, loi 0201-06, HD1 con; (b) HTTP 302, HD2 mat, RowVersion doi";
        var facts = new List<string>(); var errors = new List<string>();
        void Check(bool ok, string name, string observed)
        {
            facts.Add($"{name}={observed}");
            if (!ok) errors.Add($"{name}: {observed}");
        }
        try
        {
            using var factory = new IntegrationTestFactory();
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
            int tripId, hd1, hd2;
            byte[] original;
            var day = new DateTime(2026, 12, 5);
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = await db.Accounts.SingleAsync(x => x.Email == "khach@demo.vn");
                var trip = new Trip { OwnerId = user.Id, Name = "KHP01 - IT03", Destination = "Đà Lạt", Start = day, End = day.AddDays(3), Status = "Đã chấp nhận",
                    Activities = new List<TripActivity> {
                        new() { Title = "HD1 da gan TH01", Start = day.AddHours(8), End = day.AddHours(9), HasTime = true, SortOrder = 1 },
                        new() { Title = "HD2 tu do", Start = day.AddHours(10), End = day.AddHours(11), HasTime = true, SortOrder = 2 }
                    } };
                db.Trips.Add(trip); await db.SaveChangesAsync();
                var incident = new Incident { OwnerId = user.Id, TripId = trip.Id, Category = "Thời tiết", Description = "TH01 - IT03", Status = "Chưa giải quyết" };
                db.Incidents.Add(incident); await db.SaveChangesAsync();
                db.IncidentActivities.Add(new IncidentActivity { IncidentId = incident.Id, ActivityId = trip.Activities[0].Id });
                await db.SaveChangesAsync();
                tripId = trip.Id; hd1 = trip.Activities[0].Id; hd2 = trip.Activities[1].Id; original = trip.Version.ToArray();
            }
            var loginHtml = await client.GetStringAsync("/Account/Login");
            var login = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string,string>
            { ["Email"] = "khach@demo.vn", ["Password"] = "Demo12345", ["__RequestVerificationToken"] = Token(loginHtml) }));
            Check((int)login.StatusCode == 302, "Dang nhap", $"HTTP {(int)login.StatusCode}");
            var detailHtml = await client.GetStringAsync($"/Journey/Details/{tripId}");
            var antiforgery = Token(detailHtml);
            async Task<HttpResponseMessage> Remove(int activityId, byte[] version) => await client.PostAsync("/Journey/RemoveActivity", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["tripId"] = tripId.ToString(), ["id"] = activityId.ToString(),
                ["version"] = Convert.ToBase64String(version), ["__RequestVerificationToken"] = antiforgery
            }));

            var responseA = await Remove(hd1, original);
            Check((int)responseA.StatusCode == 422, "(a) HTTP", $"HTTP {(int)responseA.StatusCode}");
            Check(responseA.Headers.TryGetValues("X-Business-Error", out var code) && code.Contains("0201-06"), "(a) Ma loi", responseA.Headers.TryGetValues("X-Business-Error", out var current) ? string.Join(",", current) : "Khong co");
            var htmlA = await responseA.Content.ReadAsStringAsync();
            Check(htmlA.Contains("Hoạt động đã gắn với tình huống phát sinh nên không thể bỏ", StringComparison.OrdinalIgnoreCase), "(a) Thong bao", htmlA.Contains("không thể bỏ", StringComparison.OrdinalIgnoreCase) ? "Co thong bao khac" : "Khong tim thay");
            byte[] versionB;
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var trip = await db.Trips.AsNoTracking().Include(x => x.Activities).SingleAsync(x => x.Id == tripId);
                Check(trip.Activities.Any(x => x.Id == hd1), "(a) HD1 con", trip.Activities.Any(x => x.Id == hd1) ? "Con" : "Da mat");
                versionB = trip.Version.ToArray();
                Check(original.SequenceEqual(versionB), "(a) Version giu nguyen", original.SequenceEqual(versionB) ? "Giu nguyen" : "Da doi");
            }
            var responseB = await Remove(hd2, versionB);
            Check((int)responseB.StatusCode == 302, "(b) HTTP", $"HTTP {(int)responseB.StatusCode}");
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var trip = await db.Trips.AsNoTracking().Include(x => x.Activities).SingleAsync(x => x.Id == tripId);
                Check(trip.Activities.Any(x => x.Id == hd1), "(b) HD1 van con", trip.Activities.Any(x => x.Id == hd1) ? "Con" : "Da mat");
                Check(!trip.Activities.Any(x => x.Id == hd2), "(b) HD2 bi xoa", trip.Activities.Any(x => x.Id == hd2) ? "Van con" : "Da xoa");
                Check(trip.Activities.Count == 1, "(b) Tong HD", trip.Activities.Count.ToString());
                Check(!trip.Version.SequenceEqual(versionB), "(b) Version thay doi", trip.Version.SequenceEqual(versionB) ? "Khong doi" : "Da doi");
            }
        }
        catch (Exception ex) { errors.Add("Ngoai le: " + ex.GetType().Name + " - " + ex.Message); }
        finally { IntegrationTestReporter.Save(id, title, pre, input, expected, string.Join("; ", facts), string.Join(" | ", errors)); }
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }
    private static string Token(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]*)\"");
        if (!match.Success) throw new InvalidOperationException("Khong tim thay Antiforgery token");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
}
