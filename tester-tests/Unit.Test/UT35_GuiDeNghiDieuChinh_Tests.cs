using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;
namespace TuTuc.Tests.Unit;

// Common deterministic fixture. Depends on UT23 and UT29 test helpers already in the project.
internal static class UT353637
{
    internal const int BookingId = 353637;
    internal const int ChangeId = 353638;
    internal static async Task<(TuTuc.Data.AppDbContext db, MarketplaceManager manager)> Setup(string bookingState = "Đã xác nhận", string? changeState = null, string kind = "Điều chỉnh")
    {
        var db = UT232425.Db();
        await UT293031.Seed(db);
        await UT293031.AddBooking(db, bookingState, id: BookingId);
        if (changeState != null)
        {
            db.BookingChanges.Add(new BookingChange {
                Id = ChangeId, BookingId = BookingId, RequesterId = UT293031.Khach,
                Kind = kind, Status = changeState, BaseConditionsVersion = 1,
                Date = new DateTime(2026,12,22), EndDate = new DateTime(2026,12,23),
                People = 3, Guests = 2, Total = 1800000m,
                Reason = "Đổi kế hoạch", LogicalVersion = 1,
                Version = UT232425.VersionBytes.ToArray()
            });
            await db.SaveChangesAsync();
        }
        return (db, UT293031.Manager(db));
    }
    internal static BookingTermsInput Terms(int people = 3, string reason = "Đổi lịch")
    {
        var t = UT293031.Terms(new DateTime(2026,12,21),new DateTime(2026,12,22));
        t.People = people; t.Guests = 2; t.Total = 1800000m;
        t.ConditionsVersion = 1; t.Version = UT232425.Version; t.Reason = reason;
        return t;
    }
    internal static Task Check(string id, string step, string title, string input, string expected, Func<Action<string>,Task> body)
        => UT293031.Check(id,step,title,input,expected,body);
}

public sealed class UT35_GuiDeNghiDieuChinh_Tests
{
    [Fact(DisplayName="UT-35a | KT1 | Phiên bản cũ")]
    public Task UT35a_Test()=>UT353637.Check("UT-35a","KT1","Phiên bản cũ","Version=không hợp lệ","0801-03",async observe=>
    {
        var (db,m)=await UT353637.Setup(); await using(db){var t=UT353637.Terms();t.Version="INVALID";var r=await m.ProposeChange(UT293031.Khach,UT353637.BookingId,"Điều chỉnh",t);var count=await db.BookingChanges.CountAsync();observe($"Return={r};Changes={count}");Assert.Equal(0,count);Assert.Contains("0801-03",r); }
    });
    [Fact(DisplayName="UT-35b | KT2 | Rút yêu cầu đã xác nhận")]
    public Task UT35b_Test()=>UT353637.Check("UT-35b","KT2","Rút yêu cầu đã xác nhận","Status=Đã xác nhận","0801-02",async observe=>
    {
        var (db,m)=await UT353637.Setup();await using(db){var r=await m.Withdraw(UT293031.Khach,UT353637.BookingId,UT232425.Version);observe($"Return={r}");Assert.Contains("0801-02",r);}
    });
    [Fact(DisplayName="UT-35c | KT2 | Sửa khi đã đề xuất khác")]
    public Task UT35c_Test()=>UT353637.Check("UT-35c","KT2","Sửa khi đã đề xuất khác","Status=Đề xuất khác","0801-02",async observe=>
    {
        var (db,m)=await UT353637.Setup("Đề xuất khác");await using(db){var r=await m.EditPending(UT293031.Khach,UT353637.BookingId,UT353637.Terms());observe($"Return={r}");Assert.Contains("0801-02",r);}
    });
    [Fact(DisplayName="UT-35d | KT2 | Thay đổi khi chưa xác nhận")]
    public Task UT35d_Test()=>UT353637.Check("UT-35d","KT2","Thay đổi khi chưa xác nhận","Status=Chờ phản hồi","0801-02",async observe=>
    {
        var (db,m)=await UT353637.Setup("Chờ phản hồi");await using(db){var r=await m.ProposeChange(UT293031.Khach,UT353637.BookingId,"Điều chỉnh",UT353637.Terms());observe($"Return={r}");Assert.Contains("0801-02",r);}
    });
    [Fact(DisplayName="UT-35e | KT3 | Đã có đề nghị hoạt động")]
    public Task UT35e_Test()=>UT353637.Check("UT-35e","KT3","Đã có đề nghị hoạt động","Change=Đề xuất khác","0801-04",async observe=>
    {
        var (db,m)=await UT353637.Setup(changeState:"Đề xuất khác");await using(db){var r=await m.ProposeChange(UT293031.Khach,UT353637.BookingId,"Điều chỉnh",UT353637.Terms());observe($"Return={r};Changes={await db.BookingChanges.CountAsync()}");Assert.Equal(1,await db.BookingChanges.CountAsync());Assert.Contains("0801-04",r);}
    });
    [Fact(DisplayName="UT-35f | KT4 | Đề nghị không thay đổi điều kiện")]
    public Task UT35f_Test()=>UT353637.Check("UT-35f","KT4","Đề nghị không thay đổi điều kiện","Giữ ngày;người=2;khách=2;tổng=1800000","0801-05",async observe=>
    {
        var (db,m)=await UT353637.Setup();await using(db){var t=UT353637.Terms(2);var r=await m.ProposeChange(UT293031.Khach,UT353637.BookingId,"Điều chỉnh",t);observe($"Return={r};Changes={await db.BookingChanges.CountAsync()}");Assert.Empty(db.BookingChanges);Assert.Contains("0801-05",r);}
    });
    [Fact(DisplayName="UT-35g | XL1 | Rút yêu cầu hợp lệ")]
    public Task UT35g_Test()=>UT353637.Check("UT-35g","XL1","Rút yêu cầu hợp lệ","Chờ phản hồi;KH01","Không tiếp tục; lưu lý do; thông báo YeuCauDaRut",async observe=>
    {
        var (db,m)=await UT353637.Setup("Chờ phản hồi");await using(db){var r=await m.Withdraw(UT293031.Khach,UT353637.BookingId,UT232425.Version);var b=await db.Bookings.AsNoTracking().SingleAsync(x=>x.Id==UT353637.BookingId);var notices=await db.Notifications.AsNoTracking().Where(x=>x.OwnerId==UT293031.NhaCungCap).ToListAsync();observe($"Return={r};Status={b.Status};Reason={b.ResponseReason};Notices={string.Join("|",notices.Select(x=>x.Title))}");Assert.Equal("Không tiếp tục",b.Status);Assert.False(string.IsNullOrWhiteSpace(b.ResponseReason));Assert.Contains(notices,n=>n.Title.Contains("YeuCauDaRut"));}
    });
    [Fact(DisplayName="UT-35h | XL2 | Sửa số người từ 2 thành 3")]
    public Task UT35h_Test()=>UT353637.Check("UT-35h","XL2","Sửa số người từ 2 thành 3","Chờ phản hồi;people=3","People=3; ConditionsVersion=2; thông báo YeuCauDaSua",async observe=>
    {
        var (db,m)=await UT353637.Setup("Chờ phản hồi");await using(db){var r=await m.EditPending(UT293031.Khach,UT353637.BookingId,UT353637.Terms(3));var b=await db.Bookings.AsNoTracking().SingleAsync(x=>x.Id==UT353637.BookingId);var notices=await db.Notifications.AsNoTracking().Where(x=>x.OwnerId==UT293031.NhaCungCap).ToListAsync();observe($"Return={r};People={b.People};Version={b.ConditionsVersion};Notices={notices.Count}");Assert.Equal(3,b.People);Assert.Equal(2,b.ConditionsVersion);Assert.Contains(notices,n=>n.Title.Contains("YeuCauDaSua"));}
    });
    [Fact(DisplayName="UT-35i | XL3 | Gửi đề nghị thay đổi số người")]
    public Task UT35i_Test()=>UT353637.Check("UT-35i","XL3","Gửi đề nghị thay đổi số người","Đã xác nhận;people=3","Change Chờ phản hồi; OldConditions; booking giữ nguyên; thông báo",async observe=>
    {
        var (db,m)=await UT353637.Setup();await using(db){var r=await m.ProposeChange(UT293031.Khach,UT353637.BookingId,"Điều chỉnh",UT353637.Terms());var c=await db.BookingChanges.AsNoTracking().SingleOrDefaultAsync();var b=await db.Bookings.AsNoTracking().SingleAsync(x=>x.Id==UT353637.BookingId);var notices=await db.Notifications.AsNoTracking().Where(x=>x.OwnerId==UT293031.NhaCungCap).ToListAsync();observe($"Return={r};Change={c?.Status};Old={c?.OldConditions};People={b.People};Notices={string.Join("|",notices.Select(x=>x.Title))}");Assert.NotNull(c);Assert.Equal("Chờ phản hồi",c.Status);Assert.False(string.IsNullOrWhiteSpace(c.OldConditions));Assert.Equal(2,b.People);Assert.Contains(notices,n=>n.Title.Contains("DeNghiDieuChinhMoi"));}
    });
    [Fact(DisplayName="UT-35j | XL3 | Đề nghị hủy thỏa thuận")]
    public Task UT35j_Test()=>UT353637.Check("UT-35j","XL3","Đề nghị hủy thỏa thuận","Hủy;Lý do=Đổi kế hoạch","Kind Huy; các giá trị mới NULL",async observe=>
    {
        var (db,m)=await UT353637.Setup();await using(db){var t=UT353637.Terms(reason:"Đổi kế hoạch");var r=await m.ProposeChange(UT293031.Khach,UT353637.BookingId,"Hủy",t);var c=await db.BookingChanges.AsNoTracking().SingleOrDefaultAsync();observe($"Return={r};Kind={c?.Kind};Date={c?.Date:dd/MM/yyyy};People={c?.People};Total={c?.Total}");Assert.NotNull(c);Assert.Equal("Hủy",c.Kind);Assert.Equal(default(DateTime),c.Date);Assert.Equal(0,c.People);}
    });
    [Fact(DisplayName="UT-35k | XL4 | Trùng đề nghị chờ khi lưu")]
    public Task UT35k_Test()=>UT353637.Check("UT-35k","XL4","Trùng đề nghị chờ khi lưu","Đã có đề nghị đang chờ; môi trường InMemory","SQL 2601 phải ánh xạ lỗi 0801-04",async observe=>
    {
        var (db,m)=await UT353637.Setup(changeState:"Chờ phản hồi");await using(db){var r=await m.ProposeChange(UT293031.Khach,UT353637.BookingId,"Điều chỉnh",UT353637.Terms());observe($"Return={r};Note=InMemory không phát sinh SqlException 2601");Assert.Fail("CHUA XAC MINH: Cần giả lập SaveChanges ném SQL 2601 hoặc chạy riêng với SQL Server để kiểm mã 0801-04.");}
    });
}
