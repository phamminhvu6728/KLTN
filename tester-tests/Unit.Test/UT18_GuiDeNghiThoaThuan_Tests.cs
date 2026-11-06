using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

public sealed class UT18_GuiDeNghiThoaThuan_Tests
{
    [Fact(DisplayName="UT-18a | KT1 | ngayVe truoc ngayDi")]
    public Task UT18a_InvalidDates() => UT171819.Check("UT-18a","KT1","Ngày về trước ngày đi",
        "ngayDi=10/12; ngayVe=09/12", "Lỗi 0402-04; không ghi phiên bản", async observe =>
    {
        await using var db = UT171819.Db(); await UT171819.Seed(db);
        var input = UT171819.Input(); input.End = input.Start.AddDays(-1);
        var outcome = await new CommunityManager(db).Propose(UT171819.KH01, input);
        var count = await db.CompanionAgreements.CountAsync();
        observe($"Success={outcome.Success}; Message={outcome.Message}; Count={count}; ErrorCode=KHONG CO");
        Assert.False(outcome.Success); Assert.Equal(0, count);
        Assert.Contains("0402-04", outcome.Message);
    });

    [Fact(DisplayName="UT-18b | KT3 | Ben con lai da ngung")]
    public Task UT18b_OtherInactive() => UT171819.Check("UT-18b","KT3","Bên còn lại đã ngừng hoạt động",
        "KH02.IsActive=false; KH01 đề xuất", "Lỗi 0402-05; không thêm phiên bản", async observe =>
    {
        await using var db = UT171819.Db(); await UT171819.Seed(db, ownerActive:false);
        var outcome = await new CommunityManager(db).Propose(UT171819.KH01,UT171819.Input());
        var count = await db.CompanionAgreements.CountAsync();
        observe($"Success={outcome.Success}; Message={outcome.Message}; Count={count}; ErrorCode=KHONG CO");
        Assert.False(outcome.Success); Assert.Equal(0,count);
        Assert.Contains("0402-05",outcome.Message);
    });

    [Fact(DisplayName="UT-18c | KT4 | Phien ban co so da cu")]
    public Task UT18c_StaleRevision() => UT171819.Check("UT-18c","KT4","Phiên bản cơ sở cũ",
        "phienBanCoSo=1; MAX=2", "Lỗi 0402-06", async observe =>
    {
        await using var db=UT171819.Db(); await UT171819.Seed(db);
        db.CompanionAgreements.Add(UT171819.Agreement(601,2,"Đã xác nhận")); await db.SaveChangesAsync();
        var outcome=await new CommunityManager(db).Propose(UT171819.KH01, UT171819.Input(1));
        observe($"Success={outcome.Success}; Message={outcome.Message}; StatusCode={outcome.StatusCode}; ErrorCode=KHONG CO");
        Assert.False(outcome.Success); Assert.Contains("0402-06",outcome.Message);
    });

    [Fact(DisplayName="UT-18d | KT5 | Noi dung trung ban dang co hieu luc")]
    public Task UT18d_SameContent() => UT171819.Check("UT-18d","KT5","Nội dung trùng bản đã xác nhận",
        "v1 đã xác nhận, đề nghị trùng các trường", "Lỗi 0402-07; không sinh bản mới", async observe =>
    {
        await using var db=UT171819.Db(); await UT171819.Seed(db);
        var current=UT171819.Agreement(601,1,"Đã xác nhận");db.CompanionAgreements.Add(current);await db.SaveChangesAsync();
        var input=UT171819.Input(1);
        input.Destination=current.Destination;input.Budget=current.Budget;input.CostSharing=current.CostSharing;
        input.Scope=current.Scope;input.Content=current.Content;input.Start=current.Start;input.End=current.End;
        var outcome=await new CommunityManager(db).Propose(UT171819.KH01,input);
        var count=await db.CompanionAgreements.CountAsync();
        observe($"Success={outcome.Success}; Message={outcome.Message}; Count={count}; ErrorCode=KHONG CO");
        Assert.False(outcome.Success);Assert.Equal(1,count);Assert.Contains("0402-07",outcome.Message);
    });

    [Fact(DisplayName="UT-18e | XL1/XL2 | Thay the v2 cho xac nhan bang v3")]
    public Task UT18e_ReplacePending() => UT171819.Check("UT-18e","XL1,XL2","Thay thế bản chờ bằng phiên bản mới",
        "v2 ChoXacNhan; gui v3, nguoi de xuat KH01", "v2 DaThayThe, lý do; SaveChanges lần 1 trước khi thêm v3; v3 ChoXacNhan, loại Nội dung, người đề xuất KH01",async observe =>
    {
        await using var db=UT171819.Db();await UT171819.Seed(db);
        var pending=UT171819.Agreement(602,2);db.CompanionAgreements.Add(pending);await db.SaveChangesAsync();
        var outcome=await new CommunityManager(db).Propose(UT171819.KH01, UT171819.Input(2,602,UT171819.Version));
        var versions=await db.CompanionAgreements.AsNoTracking().OrderBy(x=>x.Revision).ToListAsync();
        observe($"Success={outcome.Success}; Message={outcome.Message}; Versions={string.Join(",",versions.Select(x=>$"v{x.Revision}:{x.Status}:{x.Reason}:{x.ProposerId}"))}; SaveChangesOrder=CHUA THE CHUNG MINH EF INMEMORY");
        Assert.True(outcome.Success);Assert.Equal(2,versions.Count);
        Assert.Equal("Đã thay thế",versions[0].Status);Assert.Contains("đề xuất",versions[0].Reason);
        Assert.Equal(3,versions[1].Revision);Assert.Equal("Chờ xác nhận",versions[1].Status);
        Assert.Equal("Nội dung",versions[1].Kind);Assert.Equal(UT171819.KH01,versions[1].ProposerId);
        Assert.Fail("UT-18e: Chưa kiểm chứng được thứ tự 2 lần SaveChanges; EF InMemory không theo dõi được việc lưu lần 1 trước khi thêm v3.");
    });

    [Fact(DisplayName="UT-18f | XL3/XL4 | Giu v1 da xac nhan va thong bao ben kia")]
    public Task UT18f_KeepActiveAndNotify() => UT171819.Check("UT-18f","XL3,XL4","Bảo toàn bản đang hiệu lực và gửi thông báo",
        "v1 DaXacNhan, gui v2", "v1 vẫn DaXacNhan; thông báo ThoaThuanChoXacNhan cho KH02",async observe =>
    {
        await using var db=UT171819.Db();await UT171819.Seed(db);
        db.CompanionAgreements.Add(UT171819.Agreement(601,1,"Đã xác nhận"));await db.SaveChangesAsync();
        var outcome=await new CommunityManager(db).Propose(UT171819.KH01,UT171819.Input(1));
        var versions=await db.CompanionAgreements.AsNoTracking().OrderBy(x=>x.Revision).ToListAsync();
        var notifications=await db.Notifications.AsNoTracking().Where(x=>x.OwnerId==UT171819.KH02).ToListAsync();
        observe($"Success={outcome.Success}; States={string.Join(",",versions.Select(x=>$"v{x.Revision}:{x.Status}"))}; Notifications={notifications.Count}; Types={string.Join(",",notifications.Select(x=>x.Type))}");
        Assert.True(outcome.Success);Assert.Equal("Đã xác nhận",versions.Single(x=>x.Revision==1).Status);
        Assert.Equal("Chờ xác nhận",versions.Single(x=>x.Revision==2).Status);
        Assert.Single(notifications);Assert.Equal("ThoaThuanChoXacNhan",notifications[0].Type);
    });
}
