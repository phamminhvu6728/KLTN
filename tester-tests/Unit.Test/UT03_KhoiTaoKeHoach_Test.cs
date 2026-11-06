using System;
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
/// UT-03 / SUC02.01 / Bang 4.179.
/// Test oracles are taken from the approved unit test plan, NOT from developer behavior.
/// Current implementation entry point: JourneyManager.Save(userId, TripInput).
/// </summary>
public sealed class UT03_KhoiTaoKeHoach_Tests
{
    private const int KH01 = 101;

    private sealed class TestClock : IDongHo
    {
        public DateTime UtcNow => new(2026, 11, 3, 8, 0, 0, DateTimeKind.Utc);
        public DateTime Today => new(2026, 11, 3);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("UT03_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options, new HttpContextAccessor());
    }

    private static async Task AddTraveler(AppDbContext db)
    {
        db.Accounts.Add(new Account
        {
            Id = KH01,
            Name = "Khach du lich KH01",
            Email = "ut03-kh01@example.test",
            Role = "Traveler",
            IsActive = true
        });
        await db.SaveChangesAsync();
    }

    private static TripInput NewInput(DateTime start, DateTime end) => new()
    {
        Id = 0,
        Name = "Ke hoach Da Lat",
        Destination = "Da Lat",
        Start = start,
        End = end,
        Budget = 3_000_000m
    };

    // Compatible with run-tests.ps1: one JSONL observation per test, without
    // changing run-tests.ps1 / TestReporter.cs / Unit.Test.csproj.
    private static async Task Check(
        string id, string title, string step, string input, string expected,
        Func<Action<string>, Task> test)
    {
        string actual = "Chua ghi nhan duoc ket qua thuc te";
        string problem = "";
        try
        {
            await test(text => actual = text);
        }
        catch (Exception ex)
        {
            problem = ex.Message;
            throw;
        }
        finally
        {
            string directory = Path.Combine(AppContext.BaseDirectory, "TestResults");
            Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, "UnitTest_Details.jsonl");
            string line = JsonSerializer.Serialize(new
            {
                id, title, step, input, expected, actual, problem
            });
            lock (typeof(UT03_KhoiTaoKeHoach_Tests))
            {
                File.AppendAllText(file, line + Environment.NewLine);
            }
        }
    }

    [Fact(DisplayName = "UT-03a | KT1 | ngayKT 04/12 truoc ngayBD 05/12 => 0201-01")]
    public Task UT03a_InvalidEndDate() => Check(
        "UT-03a", "Ngay ket thuc truoc ngay bat dau", "KT1",
        "KH01; ngayBD=05/12/2026; ngayKT=04/12/2026; keHoachMoi",
        "Loi 0201-01; khong co ke hoach moi",
        async record =>
        {
            await using var db = CreateDb();
            await AddTraveler(db);
            var manager = new JourneyManager(db, new TestClock());
            int before = await db.Trips.CountAsync();

            JourneyOutcome result = await manager.Save(
                KH01, NewInput(new DateTime(2026, 12, 5), new DateTime(2026, 12, 4)));

            int after = await db.Trips.CountAsync();
            record($"Success={result.Success}; Code={result.Code}; " +
                   $"StatusCode={result.StatusCode}; Message={result.Message}; " +
                   $"soKeHoachTruoc={before}; soKeHoachSau={after}");

            Assert.False(result.Success);
            Assert.Equal("0201-01", result.Code);
            Assert.Equal(before, after);
        });

    [Fact(DisplayName = "UT-03b | XL1 XL2 | tao 1 ke hoach Nhap, chu KH01, tra ma moi")]
    public Task UT03b_ValidNewTrip() => Check(
        "UT-03b", "Tao ke hoach moi hop le", "XL1, XL2",
        "Nguoi goi=KH01; ngayBD=05/12/2026; ngayKT=08/12/2026; nganSach=3000000",
        "Tao dung 1 ke hoach Nhap; MaTaiKhoan=KH01 lay tu nguoi goi; tra ma ke hoach moi",
        async record =>
        {
            await using var db = CreateDb();
            await AddTraveler(db);
            var manager = new JourneyManager(db, new TestClock());
            int before = await db.Trips.CountAsync();

            JourneyOutcome result = await manager.Save(
                KH01, NewInput(new DateTime(2026, 12, 5), new DateTime(2026, 12, 8)));

            var trips = await db.Trips.AsNoTracking().ToListAsync();
            var created = trips.SingleOrDefault();
            record($"Success={result.Success}; maTraVe={result.Id}; " +
                   $"soKeHoachMoi={trips.Count - before}; " +
                   $"maKeHoachLuu={created?.Id.ToString() ?? "NULL"}; " +
                   $"maChuSoHuu={created?.OwnerId.ToString() ?? "NULL"}; " +
                   $"trangThai={created?.Status ?? "NULL"}; Message={result.Message}");

            Assert.True(result.Success, "He thong phai chap nhan du lieu hop le.");
            Assert.Equal(before + 1, trips.Count);
            Assert.NotNull(created);
            Assert.Equal(KH01, created!.OwnerId);
            Assert.Equal("Nháp", created.Status);
            Assert.True(created.Id > 0, "Ma ke hoach moi phai duoc tao.");
            Assert.Equal(created.Id, result.Id);
        });
}
