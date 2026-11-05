using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;
namespace TuTuc.Tests.Unit;

public sealed class UT36_PhanHoiDieuChinh_Tests
{
    [Fact(DisplayName="UT-36a | KT1 | NCC khác phản hồi")]
    public Task UT36a_Test()=>UT353637.Check("UT-36a","KT1","NCC khác phản hồi","NCC04;Change của NCC01","0801-01",async observe=>
    {
        var (db,m)=await UT353637.Setup(changeState:"Chờ phản hồi");await using(db){var r=await m.ReplyChange(UT293031.NhaCungCapKhac,UT353637.ChangeId,"Chấp nhận",UT232425.Version,1,null);observe($"Return={r}");Assert.Contains("0801-01",r);}
    });
    [Fact(DisplayName="UT-36b | KT1 | NCC tạm đình chỉ vẫn được xử lý")]
    public Task UT36b_Test()=>UT353637.Check("UT-36b","KT1","NCC tạm đình chỉ vẫn được xử lý","ProviderStatus=Tạm đình chỉ;Change hợp lệ","Được phản hồi thỏa thuận đã có",async observe=>
    {
        var (db,m)=await UT353637.Setup(changeState:"Chờ phản hồi");await using(db){var account=await db.Accounts.SingleAsync(x=>x.Id==UT293031.NhaCungCap);account.ProviderStatus="Tạm đình chỉ";await db.SaveChangesAsync();var r=await m.ReplyChange(UT293031.NhaCungCap,UT353637.ChangeId,"Từ chối",UT232425.Version,1,"Không sắp được phòng");var c=await db.BookingChanges.AsNoTracking().SingleAsync();observe($"Return={r};Status={c.Status}");Assert.Equal("Từ chối",c.Status);}
    });
    [Fact(DisplayName="UT-36c | KT2 | Đề nghị đã rút")]
    public Task UT36c_Test()=>UT353637.Check("UT-36c","KT2","Đề nghị đã rút","Change=Đã rút","0801-06",async observe=>
    {
        var (db,m)=await UT353637.Setup(changeState:"Đã rút");await using(db){var r=await m.ReplyChange(UT293031.NhaCungCap,UT353637.ChangeId,"Chấp nhận",UT232425.Version,1,null);observe($"Return={r}");Assert.Contains("0801-06",r);}
    });
    [Fact(DisplayName="UT-36d | KT3 | Yêu cầu đã hủy")]
    public Task UT36d_Test()=>UT353637.Check("UT-36d","KT3","Yêu cầu đã hủy","Booking=Đã hủy","0801-06",async observe=>
    {
        var (db,m)=await UT353637.Setup("Đã hủy","Chờ phản hồi");await using(db){var r=await m.ReplyChange(UT293031.NhaCungCap,UT353637.ChangeId,"Chấp nhận",UT232425.Version,1,null);observe($"Return={r}");Assert.Contains("0801-06",r);}
    });
    [Fact(DisplayName="UT-36e | KT4 | Từ chối không có lý do")]
    public Task UT36e_Test()=>UT353637.Check("UT-36e","KT4","Từ chối không có lý do","Decision=Từ chối;reason=trống","0801-07",async observe=>
    {
        var (db,m)=await UT353637.Setup(changeState:"Chờ phản hồi");await using(db){var r=await m.ReplyChange(UT293031.NhaCungCap,UT353637.ChangeId,"Từ chối",UT232425.Version,1,"");observe($"Return={r}");Assert.Contains("0801-07",r);}
    });
    [Fact(DisplayName="UT-36f | KT4 | Đề xuất khác cho đề nghị hủy")]
    public Task UT36f_Test()=>UT353637.Check("UT-36f","KT4","Đề xuất khác cho đề nghị hủy","Kind=Hủy;decision=Đề xuất khác","0801-05",async observe=>
    {
        var (db,m)=await UT353637.Setup(changeState:"Chờ phản hồi",kind:"Hủy");await using(db){var r=await m.ReplyChange(UT293031.NhaCungCap,UT353637.ChangeId,"Đề xuất khác",UT232425.Version,1,"Thử đổi lịch",UT353637.Terms(4));observe($"Return={r}");Assert.Contains("0801-05",r);}
    });
    [Fact(DisplayName="UT-36g | XL1 | Chấp nhận chỉ đổi số người")]
    public Task UT36g_Test()=>UT353637.Check("UT-36g","XL1","Chấp nhận chỉ đổi số người","Change=3 người;không đổi ngày","People=3; giữ Date;Change Chấp nhận; thông báo",async observe=>
    {
        var (db,m)=await UT353637.Setup(changeState:"Chờ phản hồi");await using(db){var r=await m.ReplyChange(UT293031.NhaCungCap,UT353637.ChangeId,"Chấp nhận",UT232425.Version,1,null);var b=await db.Bookings.AsNoTracking().SingleAsync(x=>x.Id==UT353637.BookingId);var c=await db.BookingChanges.AsNoTracking().SingleAsync();var notes=await db.Notifications.AsNoTracking().Where(x=>x.OwnerId==UT293031.Khach).ToListAsync();observe($"Return={r};People={b.People};Date={b.Date:dd/MM};Status={c.Status};At={c.RespondedAt};Notices={string.Join("|",notes.Select(x=>x.Title))}");Assert.Equal(3,b.People);Assert.Equal(new DateTime(2026,12,21),b.Date);Assert.Equal("Chấp nhận",c.Status);Assert.NotNull(c.RespondedAt);Assert.Contains(notes,n=>n.Title.Contains("DeNghiDieuChinhDuocChapNhan"));}
    });
    [Fact(DisplayName="UT-36h | XL2 | Chấp nhận hủy")]
    public Task UT36h_Test()=>UT353637.Check("UT-36h","XL2","Chấp nhận hủy","Kind=Hủy;decision=Chấp nhận","Booking Đã hủy; không dữ liệu hoàn tiền",async observe=>
    {
        var (db,m)=await UT353637.Setup(changeState:"Chờ phản hồi",kind:"Hủy");await using(db){var r=await m.ReplyChange(UT293031.NhaCungCap,UT353637.ChangeId,"Chấp nhận",UT232425.Version,1,null);var b=await db.Bookings.AsNoTracking().SingleAsync(x=>x.Id==UT353637.BookingId);observe($"Return={r};Status={b.Status};Reason={b.ResponseReason}");Assert.Equal("Đã hủy",b.Status);Assert.Equal("Đổi kế hoạch",b.ResponseReason);}
    });
    [Fact(DisplayName="UT-36i | XL3 | Từ chối có lý do")]
    public Task UT36i_Test()=>UT353637.Check("UT-36i","XL3","Từ chối có lý do","Reason=Không sắp được phòng","Change Từ chối; lưu lý do; booking không đổi",async observe=>
    {
        var (db,m)=await UT353637.Setup(changeState:"Chờ phản hồi");await using(db){var r=await m.ReplyChange(UT293031.NhaCungCap,UT353637.ChangeId,"Từ chối",UT232425.Version,1,"Không sắp được phòng");var c=await db.BookingChanges.AsNoTracking().SingleAsync();var b=await db.Bookings.AsNoTracking().SingleAsync(x=>x.Id==UT353637.BookingId);observe($"Return={r};Change={c.Status};Reason={c.DecisionReason};Booking={b.Status}");Assert.Equal("Từ chối",c.Status);Assert.Equal("Không sắp được phòng",c.DecisionReason);Assert.Equal("Đã xác nhận",b.Status);}
    });
    [Fact(DisplayName="UT-36j | XL4 | NCC đề xuất số người 4")]
    public Task UT36j_Test()=>UT353637.Check("UT-36j","XL4","NCC đề xuất số người 4","Decision=Đề xuất khác;people=4","Change Đề xuất khác;giữ booking; thông báo",async observe=>
    {
        var (db,m)=await UT353637.Setup(changeState:"Chờ phản hồi");await using(db){var alternative=UT353637.Terms(4);var r=await m.ReplyChange(UT293031.NhaCungCap,UT353637.ChangeId,"Đề xuất khác",UT232425.Version,1,"Thêm người",alternative);var c=await db.BookingChanges.AsNoTracking().SingleAsync();var b=await db.Bookings.AsNoTracking().SingleAsync(x=>x.Id==UT353637.BookingId);var notes=await db.Notifications.AsNoTracking().Where(x=>x.OwnerId==UT293031.Khach).ToListAsync();observe($"Return={r};Change={c.Status};People={c.People};BookingPeople={b.People};Notices={string.Join("|",notes.Select(x=>x.Title))}");Assert.Equal("Đề xuất khác",c.Status);Assert.Equal(4,c.People);Assert.Equal(2,b.People);Assert.Contains(notes,n=>n.Title.Contains("DeNghiDieuChinhCoDeXuatKhac"));}
    });
}
