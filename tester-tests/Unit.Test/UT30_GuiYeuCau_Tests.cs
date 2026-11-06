using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

// Sử dụng lớp UT293031 trong UT29.
public sealed class UT30_GuiYeuCau_Tests
{
    [Fact(DisplayName = "UT-30a | KT1 | Lượt gửi trùng")]
    public Task UT30a_Test() => UT293031.Check("UT-30a", "KT1", "Lượt gửi trùng", "Đã có Booking KH01 cùng Operation", "Trả mã yêu cầu cũ, không thêm bản ghi và không thông báo", async observe =>
    {
        await using var db = UT232425.Db();
        await UT293031.Seed(db);
        var manager = UT293031.Manager(db);
        await UT293031.AddBooking(db);
        var old=await db.Bookings.SingleAsync();
        var before=await db.Notifications.CountAsync();
        var t=UT293031.Terms();t.Operation=old.OperationId;
        var message=await manager.Book(UT293031.Khach,t);
        var after=await db.Bookings.CountAsync();var n=await db.Notifications.CountAsync();
        observe($"Message={message};BookingId={old.Id};BookingCount={after};NotificationsDelta={n-before}");
        Assert.Equal(1,after);Assert.Equal(before,n);
        Assert.Fail("Theo Bảng 4.220 KT1 phải trả mã yêu cầu đã có; Book chỉ trả thông báo dạng string, không trả mã yêu cầu.");
    });
    [Fact(DisplayName = "UT-30b | KT3 | Dịch vụ ngừng nhận sau xem trước")]
    public Task UT30b_Test() => UT293031.Check("UT-30b", "KT3", "Dịch vụ ngừng nhận sau xem trước", "Preview thành công, sau đó IsAccepting=false", "Lỗi 0701-02; không ghi Booking", async observe =>
    {
        await using var db = UT232425.Db();
        await UT293031.Seed(db);
        var manager = UT293031.Manager(db);
        var t=await UT293031.Prepared(manager);
        var s=await db.Services.SingleAsync(x=>x.Id==UT293031.DichVu);s.IsAccepting=false;await db.SaveChangesAsync();
        string actualCode="(không có lỗi)";
        try { await manager.Book(UT293031.Khach,t); }
        catch(BusinessFault ex) { actualCode=ex.Code; }
        var n=await db.Bookings.CountAsync();
        observe($"Code={actualCode};BookingCount={n}");
        Assert.Equal("0701-02",actualCode);Assert.Equal(0,n);
    });
    [Fact(DisplayName = "UT-30c | XL1,XL2 | Gửi yêu cầu hợp lệ")]
    public Task UT30c_Test() => UT293031.Check("UT-30c", "XL1,XL2", "Gửi yêu cầu hợp lệ", "KH01 gửi DV02; Operation có token hợp lệ", "Chờ phản hồi; phiên bản 1; thông báo YeuCauDatMoi cho NCC; không báo đã đặt thành công", async observe =>
    {
        await using var db = UT232425.Db();
        await UT293031.Seed(db);
        var manager = UT293031.Manager(db);
        var t=await UT293031.Prepared(manager);var message=await manager.Book(UT293031.Khach,t);
        var bookings=await db.Bookings.AsNoTracking().ToListAsync();
        var notifications=await db.Notifications.AsNoTracking().Where(x=>x.OwnerId==UT293031.NhaCungCap).ToListAsync();
        var b=Assert.Single(bookings);
        observe($"Message={message};Status={b.Status};Version={b.ConditionsVersion};Operation={b.OperationId};Notifications={string.Join(" | ",notifications.Select(x=>x.Title))}");
        Assert.Equal("Chờ phản hồi",b.Status);Assert.Equal(1,b.ConditionsVersion);
        Assert.Equal(t.Operation,b.OperationId);
        Assert.DoesNotContain("Đã đặt thành công",message,StringComparison.OrdinalIgnoreCase);
        Assert.Contains(notifications,x=>x.Title.Contains("YeuCauDatMoi",StringComparison.OrdinalIgnoreCase));
    });
    [Fact(DisplayName = "UT-30d | XL3 | Trùng chỉ mục SQL 2601")]
    public Task UT30d_Test() => UT293031.Check("UT-30d", "XL3", "Trùng chỉ mục SQL 2601", "SaveChanges gặp SQL 2601 trên UX_YeuCauDatDichVu_LuotGui", "Đọc lại mã yêu cầu đã cam kết, không báo lỗi", async observe =>
    {
        await using var db = UT232425.Db();
        await UT293031.Seed(db);
        var manager = UT293031.Manager(db);
        var t=await UT293031.Prepared(manager);
        var count=await db.Bookings.CountAsync();
        observe($"InMemoryProvider={db.Database.ProviderName};BookingCountBefore={count};Khong_the_gia_lap_SQL_2601");
        Assert.Fail("EF Core InMemory không phát sinh lỗi SQL Server 2601 từ unique index. Cần DbContext giả lập SaveChanges hoặc SQL Server integration test để xác minh XL3.");
    });
}
