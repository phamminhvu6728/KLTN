using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TuTuc.Data;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

// Can UT171819 trong UT17_MoTaoThoaThuan_Tests.cs.
public sealed class UT20_Rut_DeNghiNgung_Tests
{
    [Fact(DisplayName="UT-20a | Rut KT2 | Chua co thoa thuan da xac nhan")]
    public Task UT20a_NoActive() => UT171819.Check("UT-20a","Rút KT2","Rút khi chưa có thỏa thuận hiệu lực","KN01 chưa có phiên bản Đã xác nhận","Lỗi 0402-10; không thêm bản ghi",async observe=>
    {
        await using var db=UT171819.Db();await UT171819.Seed(db);
        var result=await new CommunityManager(db).Withdraw(UT171819.KH01,UT171819.KN01,UT171819.Version,null);
        var count=await db.CompanionAgreements.CountAsync();
        observe($"Success={result.Success}; Message={result.Message}; Count={count}; ErrorCode=KHONG CO");
        Assert.False(result.Success);Assert.Equal(0,count);Assert.Contains("0402-10",result.Message);
    });

    [Fact(DisplayName="UT-20b | Rut KT3 | Ben kia ngung van duoc rut")]
    public Task UT20b_InactiveOther() => UT171819.Check("UT-20b","Rút KT3","Rút khi bên còn lại đã ngừng","KH02 IsActive=false; v1 DaXacNhan; KH01 rút","Được rút; v1 MotBenDaRut",async observe=>
    {
        await using var db=UT171819.Db();await UT171819.Seed(db,ownerActive:false);
        db.CompanionAgreements.Add(UT171819.Agreement(601,1,"Đã xác nhận"));await db.SaveChangesAsync();
        var result=await new CommunityManager(db).Withdraw(UT171819.KH01,UT171819.KN01,UT171819.Version,"Đổi kế hoạch");
        var a=await db.CompanionAgreements.AsNoTracking().SingleAsync(x=>x.Id==601);
        observe($"Success={result.Success}; State={a.Status}; EndedBy={a.EndedBy}; EndedAt={a.EndedAt:o}; Message={result.Message}");
        Assert.True(result.Success);Assert.Equal("Một bên đã rút",a.Status);Assert.Equal(UT171819.KH01,a.EndedBy);Assert.NotNull(a.EndedAt);
    });

    [Fact(DisplayName="UT-20c | Rut XL1-XL4 | Ket thuc phien ban, thay the ban cho va thong bao")]
    public Task UT20c_WithdrawAllEffects() => UT171819.Check("UT-20c","Rút XL1-XL4","Rút thành công, thay thế bản chờ","v1 DaXacNhan; v2 ChoXacNhan; KH01 rút","v1 MotBenDaRut, thời điểm; v2 DaThayThe với lý do; thông báo ThoaThuanMotBenRut; không tác động đặt dịch vụ",async observe=>
    {
        await using var db=UT171819.Db();await UT171819.Seed(db);
        db.CompanionAgreements.AddRange(UT171819.Agreement(601,1,"Đã xác nhận"),UT171819.Agreement(602,2));await db.SaveChangesAsync();
        var beforeBookings=await db.Bookings.CountAsync();
        var result=await new CommunityManager(db).Withdraw(UT171819.KH01,UT171819.KN01,UT171819.Version,"Không thể tiếp tục");
        var items=await db.CompanionAgreements.AsNoTracking().OrderBy(x=>x.Revision).ToListAsync();
        var notices=await db.Notifications.AsNoTracking().Where(x=>x.OwnerId==UT171819.KH02).ToListAsync();
        var afterBookings=await db.Bookings.CountAsync();
        observe($"Success={result.Success}; v1={items[0].Status}/{items[0].EndedBy}/{items[0].EndedAt:o}; v2={items[1].Status}/{items[1].Reason}; Notices={notices.Count}/{string.Join(";",notices.Select(x=>x.Title))}; BookingCount={beforeBookings}->{afterBookings}");
        Assert.True(result.Success);Assert.Equal("Một bên đã rút",items[0].Status);Assert.Equal(UT171819.KH01,items[0].EndedBy);Assert.NotNull(items[0].EndedAt);
        Assert.Equal("Đã thay thế",items[1].Status);Assert.Contains("kết thúc",items[1].Reason,StringComparison.OrdinalIgnoreCase);
        Assert.Equal(beforeBookings,afterBookings);
        Assert.Contains(notices,n=>n.Type=="ThoaThuanMotBenRut");
        // Khong the chung minh "khong goi interface dat dich vu" bang DbSet Bookings; assertion ve call can mock/spy.
        Assert.Fail("Chưa thể xác minh số lời gọi IYeuCauDatDichVu bằng DbContext hiện tại.");
    });

    [Fact(DisplayName="UT-20d | Ngung KT3 | Dang co ban cho xac nhan")]
    public Task UT20d_Pending() => UT171819.Check("UT-20d","Ngừng KT3","Đề nghị ngừng khi đang có bản chờ","v1 Đã xác nhận, v2 Chờ xác nhận","Lỗi 0402-11; không thêm bản",async observe=>
    {
        await using var db=UT171819.Db();await UT171819.Seed(db);
        db.CompanionAgreements.AddRange(UT171819.Agreement(601,1,"Đã xác nhận"),UT171819.Agreement(602,2));await db.SaveChangesAsync();
        var result=await new CommunityManager(db).EndProposal(UT171819.KH01,UT171819.KN01,2);
        var count=await db.CompanionAgreements.CountAsync();
        observe($"Success={result.Success}; Message={result.Message}; Count={count}; ErrorCode=KHONG CO");
        Assert.False(result.Success);Assert.Equal(2,count);Assert.Contains("0402-11",result.Message);
    });

    [Fact(DisplayName="UT-20e | Ngung KT2 | Chua co ban da xac nhan")]
    public Task UT20e_NoActive() => UT171819.Check("UT-20e","Ngừng KT2","Đề nghị ngừng khi chưa có bản hiệu lực","KN01 chưa có phiên bản","Lỗi 0402-10; không thêm bản",async observe=>
    {
        await using var db=UT171819.Db();await UT171819.Seed(db);
        var result=await new CommunityManager(db).EndProposal(UT171819.KH01,UT171819.KN01,0);
        var count=await db.CompanionAgreements.CountAsync();
        observe($"Success={result.Success}; Message={result.Message}; Count={count}; ErrorCode=KHONG CO");
        Assert.False(result.Success);Assert.Equal(0,count);Assert.Contains("0402-10",result.Message);
    });

    [Fact(DisplayName="UT-20f | Ngung XL1-XL2 | Tao phien ban v3")]
    public Task UT20f_CreateStopProposal() => UT171819.Check("UT-20f","Ngừng XL1-XL2","Đề nghị cùng ngừng hợp lệ","v2 DaXacNhan; MAX=2; KH01 đề nghị","Tạo v3 loại DeNghiNgung, ChoXacNhan, sao chép bản v2; thông báo DeNghiCungNgung",async observe=>
    {
        await using var db=UT171819.Db();await UT171819.Seed(db);
        var active=UT171819.Agreement(601,2,"Đã xác nhận");db.CompanionAgreements.Add(active);await db.SaveChangesAsync();
        var result=await new CommunityManager(db).EndProposal(UT171819.KH01,UT171819.KN01,2);
        var latest=await db.CompanionAgreements.AsNoTracking().OrderByDescending(x=>x.Revision).FirstAsync();
        var notices=await db.Notifications.AsNoTracking().Where(x=>x.OwnerId==UT171819.KH02).ToListAsync();
        observe($"Success={result.Success}; v={latest.Revision}; Kind={latest.Kind}; State={latest.Status}; Destination={latest.Destination}; Notifies={string.Join(";",notices.Select(x=>x.Title))}");
        Assert.True(result.Success);Assert.Equal(3,latest.Revision);Assert.Equal("DeNghiNgung",latest.Kind);Assert.Equal("Chờ xác nhận",latest.Status);
        Assert.Equal(active.Start,latest.Start);Assert.Equal(active.End,latest.End);Assert.Equal(active.Destination,latest.Destination);
        Assert.Contains(notices,n=>n.Type=="DeNghiCungNgung");
    });
}
