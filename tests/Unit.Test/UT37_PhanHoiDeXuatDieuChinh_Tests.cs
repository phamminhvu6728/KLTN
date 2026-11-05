using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;
namespace TuTuc.Tests.Unit;

public sealed class UT37_PhanHoiDeXuatDieuChinh_Tests
{
    [Fact(DisplayName="UT-37a | KT2 | Chấp thuận khi còn chờ phản hồi")]
    public Task UT37a_Test()=>UT353637.Check("UT-37a","KT2","Chấp thuận khi còn chờ phản hồi","Change=Chờ phản hồi;accept=true","0801-06",async observe=>
    {
        var (db,m)=await UT353637.Setup(changeState:"Chờ phản hồi");await using(db){var r=await m.ReplyChangeOffer(UT293031.Khach,UT353637.ChangeId,true,UT232425.Version,1);var c=await db.BookingChanges.AsNoTracking().SingleAsync();observe($"Return={r};Change={c.Status}");Assert.Equal("Chờ phản hồi",c.Status);Assert.Contains("0801-06",r);}
    });
    [Fact(DisplayName="UT-37b | XL1 | Khách chấp thuận điều kiện NCC đề xuất")]
    public Task UT37b_Test()=>UT353637.Check("UT-37b","XL1","Khách chấp thuận điều kiện NCC đề xuất","Change=Đề xuất khác;3 người;accept=true","Áp dụng vào booking;Chấp nhận;Thông báo NCC",async observe=>
    {
        var (db,m)=await UT353637.Setup(changeState:"Đề xuất khác");await using(db){var r=await m.ReplyChangeOffer(UT293031.Khach,UT353637.ChangeId,true,UT232425.Version,1);var b=await db.Bookings.AsNoTracking().SingleAsync(x=>x.Id==UT353637.BookingId);var c=await db.BookingChanges.AsNoTracking().SingleAsync();var notes=await db.Notifications.AsNoTracking().Where(x=>x.OwnerId==UT293031.NhaCungCap).ToListAsync();observe($"Return={r};People={b.People};Change={c.Status};Notices={notes.Count}");Assert.Equal(3,b.People);Assert.Equal("Chấp nhận",c.Status);Assert.Contains(notes,n=>n.Title.Contains("DeNghiDieuChinhDuocChapNhan"));}
    });
    [Fact(DisplayName="UT-37c | XL2 | Rút khi còn chờ phản hồi")]
    public Task UT37c_Test()=>UT353637.Check("UT-37c","XL2","Rút khi còn chờ phản hồi","Change=Chờ phản hồi;accept=false","Đã rút;RespondedAt=BayGioUtc;booking không đổi",async observe=>
    {
        var (db,m)=await UT353637.Setup(changeState:"Chờ phản hồi");await using(db){var r=await m.ReplyChangeOffer(UT293031.Khach,UT353637.ChangeId,false,UT232425.Version,1);var b=await db.Bookings.AsNoTracking().SingleAsync(x=>x.Id==UT353637.BookingId);var c=await db.BookingChanges.AsNoTracking().SingleAsync();observe($"Return={r};Change={c.Status};At={c.RespondedAt};Booking={b.Status}");Assert.Equal("Đã rút",c.Status);Assert.NotNull(c.RespondedAt);Assert.Equal("Đã xác nhận",b.Status);}
    });
    [Fact(DisplayName="UT-37d | XL2 | Rút khi đã có phản hồi từ NCC")]
    public Task UT37d_Test()=>UT353637.Check("UT-37d","XL2","Rút khi đã có phản hồi từ NCC","Change=Đề xuất khác;RespondedAt cũ","Đã rút;giữ nguyên RespondedAt",async observe=>
    {
        var (db,m)=await UT353637.Setup(changeState:"Đề xuất khác");await using(db){var c=await db.BookingChanges.SingleAsync();var at=new DateTime(2026,11,2,12,0,0,DateTimeKind.Utc);c.RespondedAt=at;await db.SaveChangesAsync();var r=await m.ReplyChangeOffer(UT293031.Khach,UT353637.ChangeId,false,UT232425.Version,1);var after=await db.BookingChanges.AsNoTracking().SingleAsync();observe($"Return={r};Status={after.Status};Before={at:O};After={after.RespondedAt:O}");Assert.Equal("Đã rút",after.Status);Assert.Equal(at,after.RespondedAt);}
    });
}
