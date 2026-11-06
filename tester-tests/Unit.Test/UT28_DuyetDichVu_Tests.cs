using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

public sealed class UT28_DuyetDichVu_Tests
{
    [Fact(DisplayName="UT-28a | KT1 | Từ chối thiếu lý do")]
    public Task UT28a_Test()=>UT262728.Check("UT-28a","KT1","Từ chối thiếu lý do","Decision=Từ chối;Note=''","Lỗi 0602-02; không đổi bản cập nhật",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db);
        await UT262728.AddRevision(db);
        var msg=await UT232425.Market(db).Review(UT262728.Admin,UT262728.Rev1,"Từ chối","",UT232425.Version);
        var r=await db.ServiceRevisions.AsNoTracking().SingleAsync();
        observe($"Message={msg}; Status={r.Status}");
        Assert.Equal("Chờ duyệt",r.Status);Assert.Contains("0602-02",msg);
    });
    [Fact(DisplayName="UT-28b | KT1 | Cần sửa chỉ có khoảng trắng")]
    public Task UT28b_Test()=>UT262728.Check("UT-28b","KT1","Cần sửa chỉ có khoảng trắng","Decision=Cần sửa;Note='   '","Lỗi 0602-02; không đổi bản cập nhật",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db);
        await UT262728.AddRevision(db);
        var msg=await UT232425.Market(db).Review(UT262728.Admin,UT262728.Rev1,"Cần sửa","   ",UT232425.Version);
        var r=await db.ServiceRevisions.AsNoTracking().SingleAsync();
        observe($"Message={msg}; Status={r.Status}");
        Assert.Equal("Chờ duyệt",r.Status);Assert.Contains("0602-02",msg);
    });
    [Fact(DisplayName="UT-28c | KT2 | Bản đã duyệt")]
    public Task UT28c_Test()=>UT262728.Check("UT-28c","KT2","Bản đã duyệt","Revision.Status=Đã duyệt","Lỗi 0602-03",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db);
        await UT262728.AddRevision(db,3,"Đã duyệt");
        var msg=await UT232425.Market(db).Review(UT262728.Admin,UT262728.Rev1,"Đã duyệt",null,UT232425.Version);
        observe($"Message={msg}");Assert.Contains("0602-03",msg);
    });
    [Fact(DisplayName="UT-28d | KT2 | Đang duyệt bản cũ khi đã có bản mới")]
    public Task UT28d_Test()=>UT262728.Check("UT-28d","KT2","Đang duyệt bản cũ khi đã có bản mới","v3 Chờ duyệt, có v4","Lỗi 0602-03",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db);
        await UT262728.AddRevision(db,3);
        await UT262728.AddRevision(db,4,"Từ chối",970000,id:UT262728.Rev2);
        var msg=await UT232425.Market(db).Review(UT262728.Admin,UT262728.Rev1,"Đã duyệt",null,UT232425.Version);
        observe($"Message={msg}");Assert.Contains("0602-03",msg);
    });
    [Fact(DisplayName="UT-28e | KT3 | Duyệt khi NCC tạm đình chỉ")]
    public Task UT28e_Test()=>UT262728.Check("UT-28e","KT3","Duyệt khi NCC tạm đình chỉ","NCC01=TamDinhChi;Decision=DaDuyet","Lỗi 0602-04",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db,ownerStatus:"Tạm đình chỉ");
        await UT262728.AddRevision(db);
        var msg=await UT232425.Market(db).Review(UT262728.Admin,UT262728.Rev1,"Đã duyệt",null,UT232425.Version);
        var r=await db.ServiceRevisions.AsNoTracking().SingleAsync();
        observe($"Message={msg}; Status={r.Status}");Assert.Equal("Chờ duyệt",r.Status);Assert.Contains("0602-04",msg);
    });
    [Fact(DisplayName="UT-28f | KT3 | Từ chối NCC tạm đình chỉ vẫn hợp lệ")]
    public Task UT28f_Test()=>UT262728.Check("UT-28f","KT3","Từ chối NCC tạm đình chỉ vẫn hợp lệ","NCC01=TamDinhChi;Decision=TuChoi;LyDo hợp lệ","Quyết định được lưu là Từ chối",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db,ownerStatus:"Tạm đình chỉ");
        await UT262728.AddRevision(db);
        var msg=await UT232425.Market(db).Review(UT262728.Admin,UT262728.Rev1,"Từ chối","Nội dung chưa đáp ứng",UT232425.Version);
        var r=await db.ServiceRevisions.AsNoTracking().SingleAsync();
        observe($"Message={msg}; Status={r.Status}; Note={r.ReviewNote}");
        Assert.Equal("Từ chối",r.Status);Assert.Equal("Nội dung chưa đáp ứng",r.ReviewNote);
    });
    [Fact(DisplayName="UT-28g | XL1,XL2,XL4,XL5 | Duyệt bản cập nhật hợp lệ")]
    public Task UT28g_Test()=>UT262728.Check("UT-28g","XL1,XL2,XL4,XL5","Duyệt bản cập nhật hợp lệ","v3 ChoDuyet; admin AD01","Duyệt v3; công bố; ghi SK04; thông báo đúng loại",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db);
        await UT262728.AddRevision(db);
        var beforeNotifications=await db.Notifications.CountAsync();
        var msg=await UT232425.Market(db).Review(UT262728.Admin,UT262728.Rev1,"Đã duyệt",null,UT232425.Version);
        var r=await db.ServiceRevisions.AsNoTracking().SingleAsync();
        var service=await db.Services.AsNoTracking().SingleAsync(x=>x.Id==UT232425.DV01);
        var notifications=await db.Notifications.AsNoTracking().Where(x=>x.OwnerId==UT232425.NCC01).ToListAsync();
        observe($"Message={msg}; RevisionStatus={r.Status}; Reviewer={r.ReviewedById}; Time={r.ReviewedAt}; PublishedVersion={service.PublishedRevisionNumber}; Price={service.Price}; Notifications={notifications.Count}; Titles={string.Join(",",notifications.Select(x=>x.Title))}");
        Assert.Equal("Đã duyệt",r.Status);Assert.Equal(UT262728.Admin,r.ReviewedById);Assert.NotNull(r.ReviewedAt);
        Assert.Equal(3,service.PublishedRevisionNumber);Assert.Equal(950000m,service.Price);Assert.True(service.IsPublished);
        Assert.True(notifications.Count>0);Assert.Contains(notifications,x=>x.Title.Contains("DichVuDaDuyet",StringComparison.Ordinal));
        Assert.Fail("Chưa xác nhận được sự kiện SK04 và lời gọi CongBo đúng một lần theo thiết kế.");
    });
    [Fact(DisplayName="UT-28h | XL2 | Khôi phục tiếp nhận khi duyệt lại")]
    public Task UT28h_Test()=>UT262728.Check("UT-28h","XL2","Khôi phục tiếp nhận khi duyệt lại","DV01 KhongTiepNhanMoi; BlockedByDecisionId đã có; NCC đang hoạt động","IsAccepting=true; BlockedByDecisionId=NULL",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db,accepting:false);
        var service=await db.Services.SingleAsync(x=>x.Id==UT232425.DV01);
        service.BlockedByDecisionId=901;await db.SaveChangesAsync();
        await UT262728.AddRevision(db);
        var msg=await UT232425.Market(db).Review(UT262728.Admin,UT262728.Rev1,"Đã duyệt",null,UT232425.Version);
        var updated=await db.Services.AsNoTracking().SingleAsync(x=>x.Id==UT232425.DV01);
        observe($"Message={msg}; IsAccepting={updated.IsAccepting}; BlockedByDecisionId={updated.BlockedByDecisionId}");
        Assert.True(updated.IsAccepting);Assert.Null(updated.BlockedByDecisionId);
    });
    [Fact(DisplayName="UT-28i | XL3 | Yêu cầu sửa dịch vụ mới")]
    public Task UT28i_Test()=>UT262728.Check("UT-28i","XL3","Yêu cầu sửa dịch vụ mới","PublishedRevisionNumber=NULL; Decision=CanSua","Trạng thái Cần sửa; thông báo DichVuCanSua kèm lý do",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db,"Chờ duyệt",null);
        await UT262728.AddRevision(db,1);
        var msg=await UT232425.Market(db).Review(UT262728.Admin,UT262728.Rev1,"Cần sửa","Thiếu mô tả",UT232425.Version);
        var s=await db.Services.AsNoTracking().SingleAsync(x=>x.Id==UT232425.DV01);
        var n=await db.Notifications.AsNoTracking().Where(x=>x.OwnerId==UT232425.NCC01).ToListAsync();
        observe($"Message={msg}; Status={s.ApprovalStatus}; Notifications={string.Join(",",n.Select(x=>x.Title))}");
        Assert.Equal("Cần sửa",s.ApprovalStatus);Assert.Contains(n,x=>x.Title.Contains("DichVuCanSua",StringComparison.Ordinal)&&x.Description.Contains("Thiếu mô tả"));
    });
    [Fact(DisplayName="UT-28j | XL3 | Từ chối bản cập nhật của dịch vụ công bố")]
    public Task UT28j_Test()=>UT262728.Check("UT-28j","XL3","Từ chối bản cập nhật của dịch vụ công bố","DV01 đã duyệt giá 800000; v3 Chờ duyệt","Nội dung công bố và trạng thái Đã duyệt không đổi",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db);
        await UT262728.AddRevision(db);
        var msg=await UT232425.Market(db).Review(UT262728.Admin,UT262728.Rev1,"Từ chối","Không phù hợp",UT232425.Version);
        var s=await db.Services.AsNoTracking().SingleAsync(x=>x.Id==UT232425.DV01);
        var r=await db.ServiceRevisions.AsNoTracking().SingleAsync();
        observe($"Message={msg}; Revision={r.Status}; ServiceStatus={s.ApprovalStatus}; Price={s.Price}; PublishedRevision={s.PublishedRevisionNumber}");
        Assert.Equal("Từ chối",r.Status);Assert.Equal("Đã duyệt",s.ApprovalStatus);Assert.Equal(800000m,s.Price);Assert.Equal(2,s.PublishedRevisionNumber);
    });
}
