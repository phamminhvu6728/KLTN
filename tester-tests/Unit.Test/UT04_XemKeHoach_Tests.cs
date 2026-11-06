using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TuTuc.Data;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

/// <summary>
/// UT-04 / SUC02.01 / Bảng 4.180 - XemKeHoachAsync.
/// Nguồn kỳ vọng: kế hoạch kiểm thử; đối tượng thực thi hiện tại: JourneyManager.Detail.
/// Không sửa kỳ vọng cho phù hợp hiện trạng mã nguồn Dev.
/// </summary>
public sealed class UT04_XemKeHoach_Tests
{
    private const int KH01 = 101;
    private const int KH02 = 102;
    private const int KHP01 = 401;
    private static readonly DateTime FirstDay = new(2026, 12, 5);

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("UT04_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options, new HttpContextAccessor());
    }

    private static async Task SeedTrip(AppDbContext db, decimal? budget = 3_000_000m)
    {
        db.Accounts.AddRange(
            new Account { Id = KH01, Name = "KH01", Email = "ut04-kh01@example.test", Role = "Traveler", IsActive = true },
            new Account { Id = KH02, Name = "KH02", Email = "ut04-kh02@example.test", Role = "Traveler", IsActive = true });
        db.Trips.Add(new Trip
        {
            Id = KHP01, OwnerId = KH01, Name = "Kế hoạch Đà Lạt",
            Destination = "Đà Lạt", Start = FirstDay, End = FirstDay.AddDays(3),
            Budget = budget, Status = "Nháp"
        });
        await db.SaveChangesAsync();
    }

    private static TripActivity Activity(int id, int dayOffset, int hour, int sortOrder,
        decimal? cost = null, bool hasTime = true, int minutes = 60)
    {
        var start = FirstDay.AddDays(dayOffset).AddHours(hour);
        return new TripActivity
        {
            Id = id, TripId = KHP01, Title = "Hoạt động " + id,
            Start = start, End = start.AddMinutes(minutes),
            SortOrder = sortOrder, HasTime = hasTime, Cost = cost
        };
    }

    // Cùng định dạng JSONL mà run-tests.ps1 hiện tại đang đọc.
    private static async Task Check(string id, string title, string step,
        string input, string expected, Func<Action<string>, Task> execute)
    {
        string actual = "Chưa thu thập được kết quả thực tế";
        string problem = "";
        try
        {
            await execute(value => actual = value);
        }
        catch (Exception exception)
        {
            problem = exception.Message;
            throw;
        }
        finally
        {
            string folder = Path.Combine(AppContext.BaseDirectory, "TestResults");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "UnitTest_Details.jsonl");
            string line = JsonSerializer.Serialize(new { id, title, step, input, expected, actual, problem });
            // Các nhóm UT có thể ghi đồng thời: thử lại nếu file đang bị khóa.
            for (int attempt = 0; attempt < 20; attempt++)
            {
                try
                {
                    using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
                    using var writer = new StreamWriter(stream);
                    await writer.WriteLineAsync(line);
                    break;
                }
                catch (IOException) when (attempt < 19)
                {
                    await Task.Delay(30);
                }
            }
        }
    }

    [Fact(DisplayName = "UT-04a | KT1 | KH02 xem KHP01 cua KH01 => loi 0201-02")]
    public Task UT04a_OtherOwner() => Check(
        "UT-04a", "Người khác mở kế hoạch", "KT1",
        "Người gọi=KH02; kế hoạch=KHP01 thuộc KH01",
        "Từ chối truy cập với mã lỗi 0201-02",
        async record =>
        {
            await using var db = CreateDb();
            await SeedTrip(db);
            JourneyDetails? result = await new JourneyManager(db).Detail(KH02, KHP01);
            string? errorCode = null; // Detail hiện trả JourneyDetails? thay vì một kết quả mang mã lỗi.
            record($"ketQua={(result == null ? "NULL" : "JourneyDetails")}; maLoi={(errorCode ?? "KHONG CO")}");
            Assert.Null(result);
            Assert.Equal("0201-02", errorCode);
        });

    [Fact(DisplayName = "UT-04b | XL1 | sap xep hoat dong theo Ngay ThuTu Gio")]
    public Task UT04b_OrderActivities() => Check(
        "UT-04b", "Sắp xếp hoạt động", "XL1",
        "Hoạt động nạp lộn thứ tự ngày, ThuTu và giờ",
        "Danh sách trả theo Ngay tăng dần, ThuTu tăng dần, Gio tăng dần",
        async record =>
        {
            await using var db = CreateDb();
            await SeedTrip(db);
            db.Activities.AddRange(
                Activity(21, 1, 8, 1),
                Activity(22, 0, 15, 2),
                Activity(23, 0, 10, 1),
                Activity(24, 0, 8, 1));
            await db.SaveChangesAsync();
            var result = await new JourneyManager(db).Detail(KH01, KHP01);
            Assert.NotNull(result);
            var actual = result!.Trip.Activities.Select(x => x.Id).ToArray();
            var expected = result.Trip.Activities
                .OrderBy(x => x.Start.Date).ThenBy(x => x.SortOrder).ThenBy(x => x.Start.TimeOfDay)
                .Select(x => x.Id).ToArray();
            record($"thuTuTraVe=[{string.Join(",", actual)}]; thuTuTheoThietKe=[{string.Join(",", expected)}]");
            Assert.Equal(expected, actual);
        });

    [Fact(DisplayName = "UT-04c | XL2 | tong chi phi NULL tinh la 0")]
    public Task UT04c_TotalCost() => Check(
        "UT-04c", "Tổng hợp chi phí", "XL2",
        "Chi phí các hoạt động: 1500000; NULL; 600000",
        "tongChiPhi=2100000 (NULL tính 0)",
        async record =>
        {
            await using var db = CreateDb();
            await SeedTrip(db);
            db.Activities.AddRange(
                Activity(31, 0, 8, 1, 1_500_000m),
                Activity(32, 1, 8, 1, null),
                Activity(33, 2, 8, 1, 600_000m));
            await db.SaveChangesAsync();
            var result = await new JourneyManager(db).Detail(KH01, KHP01);
            Assert.NotNull(result);
            decimal calculated = result!.Trip.Activities.Sum(x => x.Cost ?? 0m);
            var totalProperty = result.GetType().GetProperty("TongChiPhi") ?? result.GetType().GetProperty("TotalCost");
            object? exposed = totalProperty?.GetValue(result);
            record($"tongTuDanhSach={calculated}; tongChiPhiTraVe={(exposed?.ToString() ?? "KHONG CO TRUONG")}");
            Assert.Equal(2_100_000m, calculated);
            Assert.NotNull(exposed); // Thiết kế yêu cầu đầu ra tổng chi phí; phép tính riêng chưa chứng minh đầu ra.
            Assert.Equal(2_100_000m, Convert.ToDecimal(exposed));
        });

    [Fact(DisplayName = "UT-04d | XL3 | canh bao trung gio, khong tinh Gio NULL")]
    public Task UT04d_OverlapWarning() => Check(
        "UT-04d", "Cảnh báo trùng giờ", "XL3",
        "Hai hoạt động 05/12 lúc 08:00 trùng nhau; hai hoạt động 06/12 có Gio=NULL",
        "Chỉ có 1 cảnh báo TrungGio của cặp 05/12; cặp Gio NULL không bị tính trùng",
        async record =>
        {
            await using var db = CreateDb();
            await SeedTrip(db);
            db.Activities.AddRange(
                Activity(41, 0, 8, 1, hasTime: true, minutes: 120),
                Activity(42, 0, 8, 2, hasTime: true, minutes: 60),
                Activity(43, 1, 0, 1, hasTime: false),
                Activity(44, 1, 0, 2, hasTime: false));
            await db.SaveChangesAsync();
            var result = await new JourneyManager(db).Detail(KH01, KHP01);
            Assert.NotNull(result);
            var overlaps = result!.Warnings.Where(x => x.Contains("Trùng giờ", StringComparison.OrdinalIgnoreCase)).ToArray();
            record($"soCanhBaoTrungGio={overlaps.Length}; noiDung=[{string.Join(" | ", overlaps)}]");
            Assert.Single(overlaps);
            Assert.Contains("05/12", overlaps[0]);
            Assert.DoesNotContain("06/12", overlaps[0]);
        });

    [Fact(DisplayName = "UT-04e | XL3 | vuot ngan sach va ngan sach NULL")]
    public Task UT04e_BudgetWarnings() => Check(
        "UT-04e", "Cảnh báo vượt ngân sách", "XL3",
        "Tổng 3100000; ngân sách 3000000; sau đó ngân sách NULL",
        "Có VuotNganSach khi ngân sách 3000000; không có cảnh báo khi ngân sách NULL",
        async record =>
        {
            await using var db = CreateDb();
            await SeedTrip(db);
            db.Activities.AddRange(Activity(51, 0, 8, 1, 1_500_000m), Activity(52, 1, 8, 1, 1_600_000m));
            await db.SaveChangesAsync();
            var manager = new JourneyManager(db);
            var withBudget = await manager.Detail(KH01, KHP01);
            Assert.NotNull(withBudget);
            var warningBefore = withBudget!.Warnings.Where(x => x.Contains("ngân sách", StringComparison.OrdinalIgnoreCase)).ToArray();
            var trip = await db.Trips.SingleAsync(x => x.Id == KHP01);
            trip.Budget = null;
            await db.SaveChangesAsync();
            var withoutBudget = await manager.Detail(KH01, KHP01);
            Assert.NotNull(withoutBudget);
            var warningAfter = withoutBudget!.Warnings.Where(x => x.Contains("ngân sách", StringComparison.OrdinalIgnoreCase)).ToArray();
            record($"tong=3100000; canhBaoKhiNganSach3000000=[{string.Join(" | ", warningBefore)}]; soCanhBaoKhiNganSachNULL={warningAfter.Length}");
            Assert.Single(warningBefore);
            Assert.Empty(warningAfter);
        });

    [Fact(DisplayName = "UT-04f | XL4 | tra du bon nhom ngay 05 den 08/12")]
    public Task UT04f_FourDayGroups() => Check(
        "UT-04f", "Bốn nhóm ngày của kế hoạch", "XL4",
        "KHP01 ngày 05–08/12/2026; chỉ có 1 hoạt động ngày 05/12",
        "Trả đúng 4 nhóm ngày 05,06,07,08/12; ba nhóm cuối rỗng",
        async record =>
        {
            await using var db = CreateDb();
            await SeedTrip(db);
            db.Activities.Add(Activity(61, 0, 8, 1));
            await db.SaveChangesAsync();
            var result = await new JourneyManager(db).Detail(KH01, KHP01);
            Assert.NotNull(result);
            // Đọc các nhóm ngày trong đầu ra thực tế (không tự dựng nhóm thay cho Dev).
            var daysProperty = result!.GetType().GetProperty("NhomNgay")
                ?? result.GetType().GetProperty("DayGroups")
                ?? result.GetType().GetProperty("Days");
            var dayGroups = daysProperty?.GetValue(result) as IEnumerable;
            var groups = dayGroups?.Cast<object>().ToArray();
            record($"kieuKetQua={result.GetType().Name}; coNhomNgay={(groups != null)}; soNhom={groups?.Length.ToString() ?? "KHONG CO"}; soHoatDong={result.Trip.Activities.Count}");
            Assert.NotNull(groups);
            Assert.Equal(4, groups!.Length);
            // Kiểm tra tên/trạng thái mỗi nhóm cần bổ sung khi API của Dev công bố cấu trúc nhóm ngày.
        });
}
