using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

// Shared fixtures: UT23_GhiKetQuaTinhHuong_Tests.cs, UT29_MoDatDichVu_KiemTraTruocGui_Tests.cs.
internal static class UT323334
{
    internal const int Id = 29001;
    internal static async Task<(TuTuc.Data.AppDbContext db, MarketplaceManager manager)> Setup(string state = "Chờ phản hồi", int version = 1)
    {
        var db = UT232425.Db();
        await UT293031.Seed(db);
        await UT293031.AddBooking(db, state, id: Id);
        var b = await db.Bookings.SingleAsync(x => x.Id == Id);
        b.ConditionsVersion = version;
        b.CustomerAcceptedVersion = version;
        b.ProviderAcceptedVersion = version;
        b.RespondedAt = null;
        await db.SaveChangesAsync();
        return (db, UT293031.Manager(db));
    }
    internal static Task Check(string id, string step, string title, string input, string expected, Func<Action<string>, Task> test)
        => UT293031.Check(id, step, title, input, expected, test);
    internal static async Task<string> RespondCode(MarketplaceManager manager, int provider, int id, string decision,
        int conditionsVersion = 1, string? reason = null, BookingTermsInput? offer = null, string? version = null)
    {
        try { return "NO_ERROR: " + await manager.Respond(provider, id, decision, version ?? UT232425.Version, conditionsVersion, reason, offer); }
        catch (BusinessFault e) { return e.Code; }
    }
    internal static async Task<string> ReplyCode(MarketplaceManager manager, int customer, int id, bool accept, int version = 1)
    {
        try { return "NO_ERROR: " + await manager.ReplyOffer(customer, id, accept, UT232425.Version, version); }
        catch (BusinessFault e) { return e.Code; }
    }
}
public sealed class UT32_ChapNhanYeuCau_Tests
{
    [Fact(DisplayName="UT-32a | KT1 | NCC khác phản hồi")]
    public Task UT32a_Test()=>UT323334.Check("UT-32a","KT1","NCC khác phản hồi","NCC04 phản hồi booking của NCC01","0701-04",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Chờ phản hồi",1);
        await using (db)
        {
            var code=await UT323334.RespondCode(manager,UT293031.NhaCungCapKhac,UT323334.Id,"Đã xác nhận");observe($"Code={code}");Assert.Equal("0701-04",code);
        }
    });
    [Fact(DisplayName="UT-32b | KT2 | Yêu cầu đã xác nhận")]
    public Task UT32b_Test()=>UT323334.Check("UT-32b","KT2","Yêu cầu đã xác nhận","Status=Đã xác nhận","0701-05",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Đã xác nhận",1);
        await using (db)
        {
            var code=await UT323334.RespondCode(manager,UT293031.NhaCungCap,UT323334.Id,"Đã xác nhận");observe($"Code={code}");Assert.Equal("0701-05",code);
        }
    });
    [Fact(DisplayName="UT-32c | KT2 | Phiên bản yêu cầu đã đổi")]
    public Task UT32c_Test()=>UT323334.Check("UT-32c","KT2","Phiên bản yêu cầu đã đổi","ConditionsVersion=2; tham số version=1","0701-05",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Chờ phản hồi",2);
        await using (db)
        {
            var code=await UT323334.RespondCode(manager,UT293031.NhaCungCap,UT323334.Id,"Đã xác nhận",1);observe($"Code={code}");Assert.Equal("0701-05",code);
        }
    });
    [Fact(DisplayName="UT-32d | KT3 | Ngừng tiếp nhận nhưng chọn chấp nhận")]
    public Task UT32d_Test()=>UT323334.Check("UT-32d","KT3","Ngừng tiếp nhận nhưng chọn chấp nhận","Service.IsAccepting=false; quyết định Đã xác nhận","0701-02",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Chờ phản hồi",1);
        await using (db)
        {
            var service=await db.Services.SingleAsync(x=>x.Id==UT293031.DichVu);service.IsAccepting=false;await db.SaveChangesAsync();var code=await UT323334.RespondCode(manager,UT293031.NhaCungCap,UT323334.Id,"Đã xác nhận");observe($"Code={code}");Assert.Equal("0701-02",code);
        }
    });
    [Fact(DisplayName="UT-32e | KT3 | Ngừng tiếp nhận vẫn được từ chối")]
    public Task UT32e_Test()=>UT323334.Check("UT-32e","KT3","Ngừng tiếp nhận vẫn được từ chối","Service.IsAccepting=false; quyết định Từ chối; lý do hợp lệ","Không lỗi; trạng thái Từ chối",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Chờ phản hồi",1);
        await using (db)
        {
            var service=await db.Services.SingleAsync(x=>x.Id==UT293031.DichVu);service.IsAccepting=false;await db.SaveChangesAsync();var code=await UT323334.RespondCode(manager,UT293031.NhaCungCap,UT323334.Id,"Từ chối",reason:"Hết lịch phục vụ");var item=await db.Bookings.AsNoTracking().SingleAsync(x=>x.Id==UT323334.Id);observe($"Return={code};Status={item.Status}");Assert.StartsWith("NO_ERROR:",code);Assert.Equal("Từ chối",item.Status);
        }
    });
    [Fact(DisplayName="UT-32f | KT4 | Từ chối không có lý do")]
    public Task UT32f_Test()=>UT323334.Check("UT-32f","KT4","Từ chối không có lý do","Decision=Từ chối; reason trống","0701-06",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Chờ phản hồi",1);
        await using (db)
        {
            var code=await UT323334.RespondCode(manager,UT293031.NhaCungCap,UT323334.Id,"Từ chối",reason:"");observe($"Code={code}");Assert.Equal("0701-06",code);
        }
    });
    [Fact(DisplayName="UT-32g | KT5 | Đề xuất điều kiện không đổi")]
    public Task UT32g_Test()=>UT323334.Check("UT-32g","KT5","Đề xuất điều kiện không đổi","Điều kiện đề xuất bằng điều kiện hiện tại","0701-07",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Chờ phản hồi",1);
        await using (db)
        {
            var offer=UT293031.Terms(new DateTime(2026,12,21),new DateTime(2026,12,22));offer.Total=1800000m;var code=await UT323334.RespondCode(manager,UT293031.NhaCungCap,UT323334.Id,"Đề xuất khác",reason:"Đề xuất lại điều kiện",offer:offer);observe($"Code={code}");Assert.Equal("0701-07",code);
        }
    });
    [Fact(DisplayName="UT-32h | XL1 | Chấp nhận hợp lệ")]
    public Task UT32h_Test()=>UT323334.Check("UT-32h","XL1","Chấp nhận hợp lệ","Status=Chờ phản hồi; CustomerAcceptedVersion=1","Đã xác nhận; thời điểm phản hồi và thông báo",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Chờ phản hồi",1);
        await using (db)
        {
            var code=await UT323334.RespondCode(manager,UT293031.NhaCungCap,UT323334.Id,"Đã xác nhận");var item=await db.Bookings.AsNoTracking().SingleAsync(x=>x.Id==UT323334.Id);var notices=await db.Notifications.AsNoTracking().Where(x=>x.OwnerId==UT293031.Khach).ToListAsync();observe($"Return={code};Status={item.Status};RespondedAt={item.RespondedAt};CustomerNotices={notices.Count}");Assert.StartsWith("NO_ERROR:",code);Assert.Equal("Đã xác nhận",item.Status);Assert.NotNull(item.RespondedAt);Assert.Contains(notices,n=>n.Title.Contains("YeuCauDaXacNhan",StringComparison.OrdinalIgnoreCase));
        }
    });
    [Fact(DisplayName="UT-32i | XL2 | Từ chối hợp lệ")]
    public Task UT32i_Test()=>UT323334.Check("UT-32i","XL2","Từ chối hợp lệ","Decision=Từ chối; reason=Hết phòng","Từ chối; lưu lý do; gửi thông báo",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Chờ phản hồi",1);
        await using (db)
        {
            var code=await UT323334.RespondCode(manager,UT293031.NhaCungCap,UT323334.Id,"Từ chối",reason:"Hết phòng");var item=await db.Bookings.AsNoTracking().SingleAsync(x=>x.Id==UT323334.Id);var notices=await db.Notifications.AsNoTracking().Where(x=>x.OwnerId==UT293031.Khach).ToListAsync();observe($"Return={code};Status={item.Status};Reason={item.ResponseReason};Notices={string.Join("|",notices.Select(x=>x.Title))}");Assert.StartsWith("NO_ERROR:",code);Assert.Equal("Từ chối",item.Status);Assert.Equal("Hết phòng",item.ResponseReason);Assert.Contains(notices,n=>n.Title.Contains("YeuCauBiTuChoi",StringComparison.OrdinalIgnoreCase));
        }
    });
    [Fact(DisplayName="UT-32j | XL3 | Đề xuất khác và lưu phiên bản")]
    public Task UT32j_Test()=>UT323334.Check("UT-32j","XL3","Đề xuất khác và lưu phiên bản","Ngày cũ=21/12; ngày mới=22/12; thêm phiên bản","Status=Đề xuất khác; ConditionsVersion=2; lưu nhật ký điều kiện cũ và thông báo",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Chờ phản hồi",1);
        await using (db)
        {
            var offer=UT293031.Terms(new DateTime(2026,12,22),new DateTime(2026,12,23));offer.Total=1800000m;var code=await UT323334.RespondCode(manager,UT293031.NhaCungCap,UT323334.Id,"Đề xuất khác",reason:"Đổi lịch",offer:offer);var item=await db.Bookings.AsNoTracking().SingleAsync(x=>x.Id==UT323334.Id);var logs=await db.BookingConditionVersions.AsNoTracking().Where(x=>x.BookingId==UT323334.Id).ToListAsync();var notices=await db.Notifications.AsNoTracking().Where(x=>x.OwnerId==UT293031.Khach).ToListAsync();observe($"Return={code};Status={item.Status};Date={item.Date:dd/MM};Version={item.ConditionsVersion};HistoryCount={logs.Count};Notices={notices.Count}");Assert.StartsWith("NO_ERROR:",code);Assert.Equal("Đề xuất khác",item.Status);Assert.Equal(2,item.ConditionsVersion);Assert.Equal(new DateTime(2026,12,22),item.Date);Assert.Contains(logs,l=>l.Snapshot.Contains("2026-12-21"));Assert.Contains(notices,n=>n.Title.Contains("YeuCauCoDeXuatKhac",StringComparison.OrdinalIgnoreCase));
        }
    });
}
