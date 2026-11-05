using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

public sealed class UT19_XacNhanThoaThuan_Tests
{
    [Fact(DisplayName="UT-19a | KT1 | Khong duoc tu xac nhan")]
    public Task UT19a_SelfApprove() => UT171819.Check("UT-19a","KT1","Người đề xuất tự xác nhận",
        "KH01 đề xuất v1; KH01 xác nhận", "Lỗi 0402-08; bản vẫn ChoXacNhan",async observe =>
    {
        await using var db=UT171819.Db();await UT171819.Seed(db);
        db.CompanionAgreements.Add(UT171819.Agreement(601,1));await db.SaveChangesAsync();
        var outcome=await new CommunityManager(db).Agreement(UT171819.KH01,601,1,UT171819.Version,"accept",null);
        var state=(await db.CompanionAgreements.SingleAsync(x=>x.Id==601)).Status;
        observe($"Success={outcome.Success}; Message={outcome.Message}; State={state}; ErrorCode=KHONG CO");
        Assert.False(outcome.Success);Assert.Equal("Chờ xác nhận",state);Assert.Contains("0402-08",outcome.Message);
    });

    [Fact(DisplayName="UT-19b | KT2 | Phien ban khong khop v3")]
    public Task UT19b_StaleRevision() => UT171819.Check("UT-19b","KT2","Phiên bản gửi lên không khớp",
        "Bản chờ v3; tham số phienBan=2", "Lỗi 0402-06",async observe =>
    {
        await using var db=UT171819.Db();await UT171819.Seed(db);
        db.CompanionAgreements.Add(UT171819.Agreement(603,3));await db.SaveChangesAsync();
        var outcome=await new CommunityManager(db).Agreement(UT171819.KH02,603,2,UT171819.Version,"accept",null);
        observe($"Success={outcome.Success}; Message={outcome.Message}; ErrorCode=KHONG CO");
        Assert.False(outcome.Success);Assert.Contains("0402-06",outcome.Message);
    });

    [Fact(DisplayName="UT-19c | KT3 | Nguoi con lai da ngung")]
    public Task UT19c_InactiveParty() => UT171819.Check("UT-19c","KT3","Xác nhận khi một bên ngừng tài khoản",
        "KH01 không hoạt động; KH02 xác nhận v1", "Lỗi 0402-05; bản vẫn chờ",async observe =>
    {
        await using var db=UT171819.Db();await UT171819.Seed(db,senderActive:false);
        db.CompanionAgreements.Add(UT171819.Agreement(601,1));await db.SaveChangesAsync();
        var outcome=await new CommunityManager(db).Agreement(UT171819.KH02,601,1,UT171819.Version,"accept",null);
        var state=(await db.CompanionAgreements.AsNoTracking().SingleAsync(x=>x.Id==601)).Status;
        observe($"Success={outcome.Success}; Message={outcome.Message}; State={state}; ErrorCode=KHONG CO");
        Assert.False(outcome.Success);Assert.Equal("Chờ xác nhận",state);Assert.Contains("0402-05",outcome.Message);
    });

    [Fact(DisplayName="UT-19d | KT4 | Tu choi nhung khong co ly do")]
    public Task UT19d_RejectEmptyReason() => UT171819.Check("UT-19d","KT4","Từ chối thiếu lý do",
        "KH02 chọn TuChoi, lyDo rỗng", "Lỗi 0402-09; không thay đổi bản",async observe =>
    {
        await using var db=UT171819.Db();await UT171819.Seed(db);
        db.CompanionAgreements.Add(UT171819.Agreement(601,1));await db.SaveChangesAsync();
        var outcome=await new CommunityManager(db).Agreement(UT171819.KH02,601,1,UT171819.Version,"reject"," ");
        var state=(await db.CompanionAgreements.AsNoTracking().SingleAsync(x=>x.Id==601)).Status;
        observe($"Success={outcome.Success}; Message={outcome.Message}; State={state}; ErrorCode=KHONG CO");
        Assert.False(outcome.Success);Assert.Equal("Chờ xác nhận",state);Assert.Contains("0402-09",outcome.Message);
    });

    [Fact(DisplayName="UT-19e | XL1 | Xac nhan noi dung moi thay the ban cu")]
    public Task UT19e_ConfirmContent() => UT171819.Check("UT-19e","XL1","Xác nhận nội dung mới",
        "v1 DaXacNhan, v2 ChoXacNhan; KH02 xác nhận v2", "v1 DaThayThe; v2 DaXacNhan, MaNguoiXacNhan, ThoiDiemXacNhan; thông báo hai bên",async observe =>
    {
        await using var db=UT171819.Db();await UT171819.Seed(db);
        db.CompanionAgreements.Add(UT171819.Agreement(601,1,"Đã xác nhận"));
        db.CompanionAgreements.Add(UT171819.Agreement(602,2));await db.SaveChangesAsync();
        var before=DateTime.UtcNow;
        var outcome=await new CommunityManager(db).Agreement(UT171819.KH02,602,2,UT171819.Version,"accept",null);
        var records=await db.CompanionAgreements.AsNoTracking().OrderBy(x=>x.Revision).ToListAsync();
        var notifications=await db.Notifications.AsNoTracking().ToListAsync();
        observe($"Success={outcome.Success}; v1={records[0].Status}; v2={records[1].Status}; RespondedBy={records[1].RespondedBy}; RespondedAt={records[1].RespondedAt:o}; Notices={notifications.Count}; Types={string.Join(",",notifications.Select(x=>x.Type))}");
        Assert.True(outcome.Success);Assert.Equal("Đã thay thế",records[0].Status);
        Assert.Equal("Đã xác nhận",records[1].Status);Assert.Equal(UT171819.KH02,records[1].RespondedBy);
        Assert.True(records[1].RespondedAt >= before);Assert.Equal(2,notifications.Count);
        Assert.All(notifications,x=>Assert.Equal("ThoaThuanDaXacNhan",x.Type));
    });

    [Fact(DisplayName="UT-19f | XL2 | Hai ben dong y cung ngung")]
    public Task UT19f_ConfirmStop() => UT171819.Check("UT-19f","XL2","Xác nhận đề nghị cùng ngừng",
        "v1 DaXacNhan; v2 loại CungNgung ChoXacNhan; KH02 xác nhận", "v1 và v2 DaNgung, có thời điểm kết thúc; thông báo ThoaThuanDaNgung",async observe =>
    {
        await using var db=UT171819.Db();await UT171819.Seed(db);
        db.CompanionAgreements.Add(UT171819.Agreement(601,1,"Đã xác nhận"));
        db.CompanionAgreements.Add(UT171819.Agreement(602,2,"Chờ xác nhận","Cùng ngừng"));await db.SaveChangesAsync();
        var outcome=await new CommunityManager(db).Agreement(UT171819.KH02,602,2,UT171819.Version,"accept",null);
        var records=await db.CompanionAgreements.AsNoTracking().OrderBy(x=>x.Revision).ToListAsync();
        var notifications=await db.Notifications.AsNoTracking().ToListAsync();
        observe($"Success={outcome.Success}; States={string.Join(",",records.Select(x=>$"v{x.Revision}:{x.Status}:{x.EndedAt:o}"))}; NotifyTypes={string.Join(",",notifications.Select(x=>x.Type))}");
        Assert.True(outcome.Success);Assert.All(records,x=>Assert.Equal("Đã ngừng",x.Status));
        Assert.All(records,x=>Assert.NotNull(x.EndedAt));
        Assert.Contains(notifications,x=>x.Type=="ThoaThuanDaNgung");
    });

    [Fact(DisplayName="UT-19g | XL3 | Tu choi co ly do; ban da xac nhan giu nguyen")]
    public Task UT19g_RejectWithReason() => UT171819.Check("UT-19g","XL3","Từ chối có lý do, giữ nguyên thỏa thuận cũ",
        "v1 DaXacNhan; v2 ChoXacNhan; KH02 từ chối: Không đồng ý", "v2 TuChoi và có lý do; v1 giữ DaXacNhan; thông báo ThoaThuanBiTuChoi cho KH01",async observe =>
    {
        await using var db=UT171819.Db();await UT171819.Seed(db);
        db.CompanionAgreements.Add(UT171819.Agreement(601,1,"Đã xác nhận"));
        db.CompanionAgreements.Add(UT171819.Agreement(602,2));await db.SaveChangesAsync();
        var outcome=await new CommunityManager(db).Agreement(UT171819.KH02,602,2,UT171819.Version,"reject","Không đồng ý");
        var records=await db.CompanionAgreements.AsNoTracking().OrderBy(x=>x.Revision).ToListAsync();
        var notices=await db.Notifications.AsNoTracking().Where(x=>x.OwnerId==UT171819.KH01).ToListAsync();
        observe($"Success={outcome.Success}; v1={records[0].Status}; v2={records[1].Status}; Reason={records[1].Reason}; Notices={notices.Count}; Types={string.Join(",",notices.Select(x=>x.Type))}");
        Assert.True(outcome.Success);Assert.Equal("Đã xác nhận",records[0].Status);
        Assert.Equal("Từ chối",records[1].Status);Assert.Equal("Không đồng ý",records[1].Reason);
        Assert.Single(notices);Assert.Equal("ThoaThuanBiTuChoi",notices[0].Type);
    });
}
