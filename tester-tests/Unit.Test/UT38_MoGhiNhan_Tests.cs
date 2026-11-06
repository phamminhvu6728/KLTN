using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Data;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

// Fixtures chung cho UT-38..UT-40. Can cac helper UT232425 va UT293031.
internal static class UT383940
{
    internal const int BookingId = 383940;
    internal const string Main = "main";
    internal const string Extra = "option-1";
    internal static Task Check(string id, string step, string title, string input, string expected,
        Func<Action<string>, Task> run) => UT293031.Check(id, step, title, input, expected, run);

    internal static async Task<(AppDbContext db, MarketplaceManager manager)> Setup(
        string status = "Đã xác nhận", DateTime? serviceDate = null, bool twoItems = true)
    {
        var db = UT232425.Db();
        await UT293031.Seed(db);
        await UT293031.AddBooking(db, status, id: BookingId);
        var booking = await db.Bookings.SingleAsync(x => x.Id == BookingId);
        booking.Date = serviceDate ?? new DateTime(2026, 11, 3);
        booking.EndDate = booking.Date;
        booking.Options = twoItems ? "Bữa sáng" : "";
        await db.SaveChangesAsync();
        return (db, UT293031.Manager(db));
    }

    internal static List<BookingResultInput> Inputs(string main = "Hoàn thành", string extra = "Chưa xác định") =>
        new()
        {
            new BookingResultInput { ItemKey = Main, Outcome = main, Note = "Đã đối chiếu hạng mục chính" },
            new BookingResultInput { ItemKey = Extra, Outcome = extra, Note = "Đã đối chiếu hạng mục bổ sung" }
        };

    internal static async Task AddResult(AppDbContext db, string key, string outcome,
        string verification = "Đã ghi nhận", string feedback = "", DateTime? recordedAt = null)
    {
        db.BookingItemResults.Add(new BookingItemResult
        {
            BookingId = BookingId, ItemKey = key,
            ItemTitle = key == Main ? "Dịch vụ chính" : "Bữa sáng",
            Outcome = outcome, Note = "Ghi nhận ban đầu",
            VerificationStatus = verification, Feedback = feedback,
            RecordedAt = recordedAt ?? new DateTime(2026, 11, 2, 17, 0, 0, DateTimeKind.Utc),
            Version = UT232425.VersionBytes.ToArray()
        });
        await db.SaveChangesAsync();
    }
}

public sealed class UT38_MoGhiNhan_Tests
{
    [Fact(DisplayName = "UT-38a | KT1 | NCC khac mo ghi nhan")]
    public Task UT38a_Test() => UT383940.Check("UT-38a", "KT1", "NCC khác mở ghi nhận",
        "NCC04 mở thỏa thuận DV02 của NCC01", "Lỗi 0901-01", async observe =>
    {
        var (db, manager) = await UT383940.Setup(); await using (db)
        {
            var before = await db.BookingItemResults.CountAsync();
            var returned = await manager.RecordResults(UT293031.NhaCungCapKhac, UT383940.BookingId,
                UT232425.Version, UT383940.Inputs());
            var after = await db.BookingItemResults.CountAsync();
            observe($"Return={returned}; RowsBefore={before}; RowsAfter={after}");
            Assert.Equal(before, after);
            Assert.Contains("0901-01", returned);
        }
    });

    [Fact(DisplayName = "UT-38b | KT2 | Thoa thuan da huy")]
    public Task UT38b_Test() => UT383940.Check("UT-38b", "KT2", "Thỏa thuận đã hủy",
        "Booking.Status=Đã hủy", "Lỗi 0901-02", async observe =>
    {
        var (db, manager) = await UT383940.Setup("Đã hủy"); await using (db)
        {
            var returned = await manager.RecordResults(UT293031.NhaCungCap, UT383940.BookingId,
                UT232425.Version, UT383940.Inputs());
            var count = await db.BookingItemResults.CountAsync();
            observe($"Return={returned}; ResultRows={count}");
            Assert.Equal(0, count);
            Assert.Contains("0901-02", returned);
        }
    });

    [Fact(DisplayName = "UT-38c | KT3 bien | Ngay phuc vu 04/11")]
    public Task UT38c_Test() => UT383940.Check("UT-38c", "KT3 (biên)", "Ngày bắt đầu 04/11/2026, chưa đến",
        "HomNay=03/11/2026; Date=04/11/2026", "Lỗi 0901-03; không ghi kết quả", async observe =>
    {
        var (db, manager) = await UT383940.Setup(serviceDate: new DateTime(2026, 11, 4)); await using (db)
        {
            var returned = await manager.RecordResults(UT293031.NhaCungCap, UT383940.BookingId,
                UT232425.Version, UT383940.Inputs());
            var count = await db.BookingItemResults.CountAsync();
            observe($"Return={returned}; ResultRows={count}");
            Assert.Equal(0, count);
            Assert.Contains("0901-03", returned);
        }
    });

    [Fact(DisplayName = "UT-38d | KT3 bien | Ngay phuc vu 03/11")]
    public Task UT38d_Test() => UT383940.Check("UT-38d", "KT3 (biên)", "Đúng ngày phục vụ được ghi nhận",
        "HomNay=03/11/2026; Date=03/11/2026", "Cho phép vào luồng ghi nhận", async observe =>
    {
        var (db, manager) = await UT383940.Setup(); await using (db)
        {
            var returned = await manager.RecordResults(UT293031.NhaCungCap, UT383940.BookingId,
                UT232425.Version, UT383940.Inputs());
            var count = await db.BookingItemResults.CountAsync();
            observe($"Return={returned}; ResultRows={count}");
            Assert.Equal(2, count);
            Assert.Contains("Đã lưu kết quả", returned);
        }
    });

    [Fact(DisplayName = "UT-38e | XL2 | Chi mo nhap hang muc chua chot")]
    public Task UT38e_Test() => UT383940.Check("UT-38e", "XL2", "Hạng mục đã chốt chỉ xem",
        "main=Hoàn thành/Đã ghi nhận; option-1=Chưa xác định", "main chỉ xem; option-1 nhập được", async observe =>
    {
        var (db, manager) = await UT383940.Setup(); await using (db)
        {
            await UT383940.AddResult(db, UT383940.Main, "Hoàn thành");
            await UT383940.AddResult(db, UT383940.Extra, "Chưa xác định", "Chờ kiểm tra");
            var rows = await db.BookingItemResults.AsNoTracking().OrderBy(x => x.ItemKey).ToListAsync();
            var editable = MarketplaceManager.Items(await db.Bookings.SingleAsync(x => x.Id == UT383940.BookingId))
                .Where(item => !rows.Any(r => r.ItemKey == item.Key && r.Outcome != "Chưa xác định"
                    && r.VerificationStatus != "Chờ kiểm tra")).Select(x => x.Key).ToList();
            observe($"Rows={string.Join(";", rows.Select(x => $"{x.ItemKey}:{x.Outcome}/{x.VerificationStatus}"))}; " +
                    $"KeysEditableAccordingToRecordResults={string.Join(",", editable)}; FormOutput=không có");
            Assert.Equal(new[] { UT383940.Extra }, editable);
            Assert.Fail("CHƯA XÁC MINH: Dev chưa có đầu ra MoGhiNhanAsync thể hiện ô chỉ xem/ô nhập trên biểu mẫu. " +
                        "Điều kiện lọc hạng mục trong RecordResults chỉ chứng minh được một phần XL2.");
        }
    });
}
