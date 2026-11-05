using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;
// Yêu cầu: giữ file UT23_GhiKetQuaTinhHuong_Tests.cs (lớp hỗ trợ UT232425).
internal sealed class UT293031Clock : IDongHo
{
    public DateTime UtcNow => new DateTime(2026, 11, 2, 17, 0, 0, DateTimeKind.Utc);
    public DateTime Today => new DateTime(2026, 11, 3);
}
internal static class UT293031
{
    internal const int Khach = UT232425.KH01;
    internal const int KhachKhac = 102;
    internal const int NhaCungCap = UT232425.NCC01;
    internal const int NhaCungCapKhac = UT232425.NCC04;
    internal const int DichVu = UT232425.DV02;
    internal static MarketplaceManager Manager(TuTuc.Data.AppDbContext db)
        => new(db, new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider(), new UT293031Clock());
    internal static async Task Seed(TuTuc.Data.AppDbContext db)
    {
        await UT232425.Seed(db);
        db.Accounts.Add(new Account { Id = KhachKhac, Name = "KH02", Role = "Traveler", IsActive = true,
            Email = "ut29-kh02@example.test" });
        var service = await db.Services.SingleAsync(x => x.Id == DichVu);
        service.IsAccepting = true;
        service.ApprovalStatus = "Đã duyệt";
        service.IsPublished = true;
        await db.SaveChangesAsync();
    }
    internal static BookingTermsInput Terms(DateTime? date = null, DateTime? end = null) => new()
    {
        Service = DichVu, Date = date ?? new DateTime(2026, 12, 21), EndDate = end,
        People = 2, Guests = 2, Operation = Guid.Empty
    };
    internal static async Task<BookingTermsInput> Prepared(MarketplaceManager manager, int owner = Khach)
    {
        var t = Terms();
        var preview = await manager.Preview(owner, t);
        Assert.NotNull(preview.Preview);
        Assert.False(string.IsNullOrEmpty(t.PreviewToken));
        return t;
    }
    internal static async Task AddBooking(TuTuc.Data.AppDbContext db, string state = "Chờ phản hồi", int owner = Khach, int serviceId = DichVu, int id = 29001)
    {
        db.Bookings.Add(new Booking
        {
            Id = id, OwnerId = owner, TravelServiceId = serviceId, SnapshotName = "Dịch vụ đã duyệt",
            Date = new DateTime(2026, 12, 21), EndDate = new DateTime(2026, 12, 22),
            People = 2, Guests = 2, Total = 1800000m, OperationId = Guid.NewGuid(),
            Status = state, ConditionsVersion = 1, CustomerAcceptedVersion = 1,
            Version = UT232425.VersionBytes.ToArray(), CreatedAt = new DateTime(2026, 11, 3)
        });
        await db.SaveChangesAsync();
    }
    internal static Task Check(string id, string step, string title, string input, string expected, Func<Action<string>,Task> body)
        => UT232425.Check(id, step, title, input, expected, body);
}

public sealed class UT29_MoDatDichVu_KiemTraTruocGui_Tests
{
    [Fact(DisplayName = "UT-29a | Mở KT1 | Dịch vụ chưa duyệt")]
    public Task UT29a_Test() => UT293031.Check("UT-29a", "Mở KT1", "Dịch vụ chưa duyệt", "DV01=Nháp", "Lỗi 0701-01", async observe =>
    {
        await using var db = UT232425.Db();
        await UT293031.Seed(db);
        var manager = UT293031.Manager(db);
        var t = UT293031.Terms(); t.Service = UT232425.DV01;
        var r = await manager.Preview(UT293031.Khach,t);
        observe($"Code={r.Code}; Error={r.Error}; Preview={(r.Preview != null)}");
        Assert.Null(r.Preview); Assert.Equal("0701-01",r.Code);
    });
    [Fact(DisplayName = "UT-29b | Mở KT2 | Dịch vụ không tiếp nhận")]
    public Task UT29b_Test() => UT293031.Check("UT-29b", "Mở KT2", "Dịch vụ không tiếp nhận", "DV02 IsAccepting=false", "Lỗi 0701-02", async observe =>
    {
        await using var db = UT232425.Db();
        await UT293031.Seed(db);
        var manager = UT293031.Manager(db);
        var s = await db.Services.SingleAsync(x=>x.Id==UT293031.DichVu);
        s.IsAccepting=false; await db.SaveChangesAsync();
        var r=await manager.Preview(UT293031.Khach,UT293031.Terms());
        observe($"Code={r.Code}; Error={r.Error}; Preview={(r.Preview!=null)}");
        Assert.Null(r.Preview); Assert.Equal("0701-02",r.Code);
    });
    [Fact(DisplayName = "UT-29c | Mở XL2 | Mở hai lần tạo mã khác nhau")]
    public Task UT29c_Test() => UT293031.Check("UT-29c", "Mở XL2", "Mở hai lần tạo mã khác nhau", "2 lần Preview cùng khách, Operation=Guid.Empty", "2 Operation khác nhau, đều khác Guid.Empty", async observe =>
    {
        await using var db = UT232425.Db();
        await UT293031.Seed(db);
        var manager = UT293031.Manager(db);
        var t1=UT293031.Terms();var t2=UT293031.Terms();
        var p1=await manager.Preview(UT293031.Khach,t1);
        var p2=await manager.Preview(UT293031.Khach,t2);
        observe($"Preview1={p1.Preview!=null};Guid1={t1.Operation};Preview2={p2.Preview!=null};Guid2={t2.Operation}");
        Assert.NotNull(p1.Preview);Assert.NotNull(p2.Preview);
        Assert.NotEqual(Guid.Empty,t1.Operation);Assert.NotEqual(Guid.Empty,t2.Operation);
        Assert.NotEqual(t1.Operation,t2.Operation);
    });
    [Fact(DisplayName = "UT-29d | Kiểm tra KT1 | Ngày kết thúc trước ngày bắt đầu")]
    public Task UT29d_Test() => UT293031.Check("UT-29d", "Kiểm tra KT1", "Ngày kết thúc trước ngày bắt đầu", "ngayBD=05/12/2026;ngayKT=04/12/2026", "Lỗi 0701-03", async observe =>
    {
        await using var db = UT232425.Db();
        await UT293031.Seed(db);
        var manager = UT293031.Manager(db);
        var r=await manager.Preview(UT293031.Khach,UT293031.Terms(new DateTime(2026,12,5),new DateTime(2026,12,4)));
        observe($"Code={r.Code};Error={r.Error};Preview={r.Preview!=null}");
        Assert.Null(r.Preview);Assert.Equal("0701-03",r.Code);
    });
    [Fact(DisplayName = "UT-29e | Kiểm tra XL1 | Ngày yêu cầu giao thời gian tạm ngừng")]
    public Task UT29e_Test() => UT293031.Check("UT-29e", "Kiểm tra XL1", "Ngày yêu cầu giao thời gian tạm ngừng", "TamNgung 20-22/12; yêu cầu 21-23/12", "Có cảnh báo TamNgung", async observe =>
    {
        await using var db = UT232425.Db();
        await UT293031.Seed(db);
        var manager = UT293031.Manager(db);
        var s=await db.Services.SingleAsync(x=>x.Id==UT293031.DichVu);
        s.ClosureStart=new DateTime(2026,12,20);s.ClosureEnd=new DateTime(2026,12,22);
        s.ClosureReason="Bảo trì";s.ClosureScope="Toàn bộ";await db.SaveChangesAsync();
        var r=await manager.Preview(UT293031.Khach,UT293031.Terms(new DateTime(2026,12,21),new DateTime(2026,12,23)));
        var w=r.Preview?.Warnings??new System.Collections.Generic.List<string>();
        observe($"Preview={r.Preview!=null};Warnings={string.Join(" | ",w)}");
        Assert.NotNull(r.Preview);Assert.Contains(w,x=>x.Contains("Tạm ngừng",StringComparison.OrdinalIgnoreCase));
    });
    [Fact(DisplayName = "UT-29f | Kiểm tra XL1 biên | Yêu cầu sau ngày tạm ngừng")]
    public Task UT29f_Test() => UT293031.Check("UT-29f", "Kiểm tra XL1 biên", "Yêu cầu sau ngày tạm ngừng", "TamNgung 20-22/12; yêu cầu 23-25/12", "Không có cảnh báo TamNgung", async observe =>
    {
        await using var db = UT232425.Db();
        await UT293031.Seed(db);
        var manager = UT293031.Manager(db);
        var s=await db.Services.SingleAsync(x=>x.Id==UT293031.DichVu);
        s.ClosureStart=new DateTime(2026,12,20);s.ClosureEnd=new DateTime(2026,12,22);
        s.ClosureReason="Bảo trì";s.ClosureScope="Toàn bộ";await db.SaveChangesAsync();
        var r=await manager.Preview(UT293031.Khach,UT293031.Terms(new DateTime(2026,12,23),new DateTime(2026,12,25)));
        var w=r.Preview?.Warnings??new System.Collections.Generic.List<string>();
        observe($"Preview={r.Preview!=null};Warnings={string.Join(" | ",w)}");
        Assert.NotNull(r.Preview);Assert.DoesNotContain(w,x=>x.Contains("Tạm ngừng",StringComparison.OrdinalIgnoreCase));
    });
    [Fact(DisplayName = "UT-29g | Kiểm tra XL1 | Ngày kết thúc không nhập")]
    public Task UT29g_Test() => UT293031.Check("UT-29g", "Kiểm tra XL1", "Ngày kết thúc không nhập", "TamNgung 20-22/12; ngayBD=22/12;ngayKT=NULL", "Có cảnh báo TamNgung; dùng ngayBD cho cả hai đầu", async observe =>
    {
        await using var db = UT232425.Db();
        await UT293031.Seed(db);
        var manager = UT293031.Manager(db);
        var s=await db.Services.SingleAsync(x=>x.Id==UT293031.DichVu);
        s.ClosureStart=new DateTime(2026,12,20);s.ClosureEnd=new DateTime(2026,12,22);
        s.ClosureReason="Bảo trì";s.ClosureScope="Toàn bộ";await db.SaveChangesAsync();
        var r=await manager.Preview(UT293031.Khach,UT293031.Terms(new DateTime(2026,12,22),null));
        var w=r.Preview?.Warnings??new System.Collections.Generic.List<string>();
        observe($"EndDate=NULL;Warnings={string.Join(" | ",w)}");
        Assert.NotNull(r.Preview);Assert.Contains(w,x=>x.Contains("Tạm ngừng",StringComparison.OrdinalIgnoreCase));
    });
    [Fact(DisplayName = "UT-29h | Kiểm tra XL1 | Bản cập nhật đang chờ duyệt")]
    public Task UT29h_Test() => UT293031.Check("UT-29h", "Kiểm tra XL1", "Bản cập nhật đang chờ duyệt", "DV02 có revision Chờ duyệt", "Cảnh báo Chờ duyệt + không bảo đảm còn chỗ; không SaveChanges", async observe =>
    {
        await using var db = UT232425.Db();
        await UT293031.Seed(db);
        var manager = UT293031.Manager(db);
        db.ServiceRevisions.Add(new ServiceRevision{ TravelServiceId=UT293031.DichVu,
            RevisionNumber=2,Status="Chờ duyệt",Name="Dịch vụ mới",Price=950000m });
        await db.SaveChangesAsync();
        var before=db.ChangeTracker.Entries().Count(x=>x.State!=EntityState.Unchanged&&x.State!=EntityState.Detached);
        var r=await manager.Preview(UT293031.Khach,UT293031.Terms());
        var w=r.Preview?.Warnings??new System.Collections.Generic.List<string>();
        var after=db.ChangeTracker.Entries().Count(x=>x.State!=EntityState.Unchanged&&x.State!=EntityState.Detached);
        observe($"Warnings={string.Join(" | ",w)};TrackedChangesBefore={before};After={after}");
        Assert.NotNull(r.Preview);Assert.Contains(w,x=>x.Contains("chờ xét duyệt",StringComparison.OrdinalIgnoreCase));
        Assert.Contains(w,x=>x.Contains("không bảo đảm còn chỗ",StringComparison.OrdinalIgnoreCase));
        Assert.Equal(before,after);
    });
}
