using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TuTuc.Data;
using TuTuc.Models;
using Xunit;

namespace Integration.Test;

public class IT01_ThemHoatDong_Tests
{
    [Fact]
    public async Task IT_01_ThemHoatDong_VaoKeHoachDaChapNhan()
    {
        const string id = "IT-01";
        const string title = "GD-01 | Them hoat dong vao ke hoach da chap nhan";
        const string precondition = "KH01 dang nhap; KHP01=DaChapNhan; 1 hoat dong; R1";
        const string input = "KHP01; R1; 05/12/2026 14:00; DD01; chi phi 50000";
        const string expected = "HTTP 302 ve ke hoach va thong bao chuyen Nhap; them HD ThuTu=2, DD01, 50000; KH=Nhap; RowVersion doi; UpdatedAt moi";

        var facts = new List<string>();
        var errors = new List<string>();
        void Check(bool result, string name, string actual)
        {
            facts.Add($"{name}={actual}");
            if (!result) errors.Add(name + ": " + actual);
        }

        try
        {
            using var factory = new IntegrationTestFactory();
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

            int tripId, placeId;
            byte[] r1;
            DateTime oldUpdatedAt;
            var day = new DateTime(2026, 12, 5);

            // Chuan bi CSDL that theo tien dieu kien cua IT-01.
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user = await db.Accounts.SingleAsync(x => x.Email == "khach@demo.vn");
                var place = new Place
                {
                    Name = "Hồ Xuân Hương", Region = "Đà Lạt",
                    Category = "Tham quan", Description = "Du lieu IT-01"
                };
                db.Places.Add(place);
                var trip = new Trip
                {
                    OwnerId = user.Id, Name = "Đà Lạt cuối năm - IT01",
                    Destination = "Đà Lạt", Start = day, End = day.AddDays(3),
                    Budget = 3000000, Status = "Đã chấp nhận",
                    Activities = new List<TripActivity>
                    {
                        new() { Title = "Hoat dong ban dau", Start = day.AddHours(8),
                            End = day.AddHours(9), HasTime = true, SortOrder = 1, Cost = 100000 }
                    }
                };
                db.Trips.Add(trip);
                await db.SaveChangesAsync();
                tripId = trip.Id;
                placeId = place.Id;
                r1 = trip.Version.ToArray();
                oldUpdatedAt = trip.UpdatedAt;
            }

            // HTTP dang nhap va token AntiForgery that.
            var loginHtml = await client.GetStringAsync("/Account/Login");
            var loginToken = GetToken(loginHtml);
            var loginResponse = await client.PostAsync("/Account/Login", Form(new()
            {
                ["Email"] = "khach@demo.vn", ["Password"] = "Demo12345",
                ["__RequestVerificationToken"] = loginToken
            }));
            Check(loginResponse.StatusCode == HttpStatusCode.Redirect,
                "Dang nhap", $"HTTP {(int)loginResponse.StatusCode}");

            var page = await client.GetAsync($"/Journey/Details/{tripId}");
            Check(page.StatusCode == HttpStatusCode.OK,
                "Mo ke hoach", $"HTTP {(int)page.StatusCode}");
            var token = GetToken(await page.Content.ReadAsStringAsync());

            // Goi Controller -> Service -> DbContext -> SQL Server.
            var response = await client.PostAsync("/Journey/Activity", Form(new()
            {
                ["TripId"] = tripId.ToString(), ["Id"] = "0",
                ["Version"] = Convert.ToBase64String(r1),
                ["SourceType"] = "Place", ["SourceId"] = placeId.ToString(),
                ["Start"] = "2026-12-05T14:00:00", ["End"] = "2026-12-05T15:00:00",
                ["HasTime"] = "true", ["SortOrder"] = "1", ["Cost"] = "50000",
                ["__RequestVerificationToken"] = token
            }));
            Check(response.StatusCode == HttpStatusCode.Redirect,
                "HTTP", ((int)response.StatusCode).ToString());
            var location = response.Headers.Location?.ToString() ?? "(khong co)";
            Check(location.Contains($"/Journey/Details/{tripId}", StringComparison.OrdinalIgnoreCase),
                "Chuyen huong", location);

            // Doc thong bao tu trang sau redirect (TempData), khong gia lap.
            if (response.StatusCode == HttpStatusCode.Redirect)
            {
                var redirect = await client.GetAsync(location);
                var html = await redirect.Content.ReadAsStringAsync();
                Console.WriteLine("URL: " + location);
                Console.WriteLine("CO THONG BAO: " +
                    html.Contains("Kế hoạch đã chuyển về Nháp"));
                Check(html.Contains("Kế hoạch đã chuyển về Nháp, hãy chấp nhận lại",
                    StringComparison.OrdinalIgnoreCase), "Thong bao theo dac ta",
                    html.Contains("Đã lưu hoạt động", StringComparison.OrdinalIgnoreCase)
                        ? "Ung dung tra 'Da luu hoat dong' (khac dac ta)"
                        : "Khong tim thay thong bao dac ta");
            }

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var trip = await db.Trips.AsNoTracking()
                    .Include(x => x.Activities).SingleAsync(x => x.Id == tripId);
                var activity = trip.Activities.SingleOrDefault(x => x.PlaceId == placeId);
                Check(trip.Activities.Count == 2, "So hoat dong", trip.Activities.Count.ToString());
                Check(activity != null, "Them HD DD01", activity == null ? "Khong co" : "Da ghi SQL");
                if (activity != null)
                {
                    Check(activity.SortOrder == 2, "ThuTu", activity.SortOrder.ToString());
                    Check(activity.Start == day.AddHours(14), "Ngay gio", activity.Start.ToString("dd/MM/yyyy HH:mm"));
                    Check(activity.Title == "Hồ Xuân Hương", "NoiDung", activity.Title);
                    Check(activity.Cost == 50000, "ChiPhi", activity.Cost?.ToString() ?? "NULL");
                    Check(activity.PlaceId == placeId && activity.TravelServiceId == null,
                        "Lien ket DD01", $"PlaceId={activity.PlaceId}; ServiceId={activity.TravelServiceId}");
                }
                Check(trip.Status == "Nháp", "Trang thai KH", trip.Status);
                Check(!r1.SequenceEqual(trip.Version), "RowVersion", r1.SequenceEqual(trip.Version) ? "Khong doi" : "Da doi");
                Check(trip.UpdatedAt > oldUpdatedAt, "ThoiDiemCapNhat",
                    $"Cu={oldUpdatedAt:O}; Moi={trip.UpdatedAt:O}");
            }
        }
        catch (Exception ex)
        {
            errors.Add("Ngoai le: " + ex.GetType().Name + " - " + ex.Message);
        }
        finally
        {
            IntegrationTestReporter.Save(id, title, precondition, input,
                expected, string.Join("; ", facts), string.Join(" | ", errors));
        }
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }

    private static FormUrlEncodedContent Form(Dictionary<string, string> fields) => new(fields);

    private static string GetToken(string html)
    {
        var match = Regex.Match(html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]*)\"");
        if (!match.Success) throw new InvalidOperationException("Khong tim thay Antiforgery token.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
}
