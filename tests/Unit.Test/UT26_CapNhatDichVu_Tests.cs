using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

// Requires UT23_GhiKetQuaTinhHuong_Tests.cs (UT232425 shared fixture/reporter).
internal static class UT262728
{
    internal const int Admin = 900;
    internal const int Rev1 = 1001;
    internal const int Rev2 = 1002;
    internal static async Task Init(TuTuc.Data.AppDbContext db,
        string approval="Đã duyệt", int? published=2, bool accepting=true,
        string ownerStatus="Đang hoạt động")
    {
        await UT232425.Seed(db);
        db.Accounts.Add(new Account {Id=Admin,Name="AD01",Email="ut262728-admin@example.test",Role="Admin",IsActive=true});
        var provider=await db.Accounts.SingleAsync(x=>x.Id==UT232425.NCC01);
        provider.ProviderStatus=ownerStatus;
        var service=await db.Services.SingleAsync(x=>x.Id==UT232425.DV01);
        service.ApprovalStatus=approval;
        service.PublishedRevisionNumber=published;
        service.IsPublished=published.HasValue;
        service.IsAccepting=accepting;
        service.Description="Mo ta cu";
        service.Price=800000;
        await db.SaveChangesAsync();
    }
    internal static async Task<ServiceRevision> AddRevision(TuTuc.Data.AppDbContext db,
        int number=3, string status="Chờ duyệt",decimal price=950000,string description="Mo ta moi", int id=Rev1)
    {
        var r=new ServiceRevision {Id=id,TravelServiceId=UT232425.DV01,
            RevisionNumber=number,Name="Phòng đôi view đồi",Description=description,
            Category="Lưu trú",Location="Đà Lạt",Price=price,
            Status=status,Version=UT232425.VersionBytes.ToArray()};
        db.ServiceRevisions.Add(r);
        await db.SaveChangesAsync();
        return r;
    }
    internal static ServiceEditVm UpdateInput(decimal price=950000)
    {
        return new ServiceEditVm {Id=UT232425.DV01,Version=UT232425.Version,
            Name="Phòng đôi view đồi",Description="Mo ta moi",Category="Lưu trú",
            Location="Đà Lạt",Price=price,Operation="Submit"};
    }
    internal static Task Check(string id,string step,string title,string input,string expected,
        Func<Action<string>,Task> body)=>UT232425.Check(id,step,title,input,expected,body);
}

public sealed class UT26_CapNhatDichVu_Tests
{
    [Fact(DisplayName="UT-26a | KT3 | Không cập nhật dịch vụ nháp")]
    public Task UT26a_Test()=>UT262728.Check("UT-26a","KT3","Không cập nhật dịch vụ nháp","DV01=Nháp","Lỗi 0601-02",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db,"Nháp",null);
        var message=await UT232425.Market(db).SubmitService(UT232425.NCC01,UT262728.UpdateInput());
        observe($"Message={message}; ErrorCodePresent={message.Contains("0601-02")}");
        Assert.Contains("0601-02",message);
    });
    [Fact(DisplayName="UT-26b | KT4 | Đã có bản chờ duyệt")]
    public Task UT26b_Test()=>UT262728.Check("UT-26b","KT4","Đã có bản chờ duyệt","DV01 có bản v3 Chờ duyệt","Lỗi 0601-05; không tạo thêm bản",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db);
        await UT262728.AddRevision(db);
        var before=await db.ServiceRevisions.CountAsync();
        var message=await UT232425.Market(db).SubmitService(UT232425.NCC01,UT262728.UpdateInput());
        var after=await db.ServiceRevisions.CountAsync();
        observe($"Message={message}; Count={before}->{after}");
        Assert.Equal(before,after);Assert.Contains("0601-05",message);
    });
    [Fact(DisplayName="UT-26c | KT6 | Nội dung trùng bản công bố")]
    public Task UT26c_Test()=>UT262728.Check("UT-26c","KT6","Nội dung trùng bản công bố","DV01 nội dung mới trùng bản hiện tại","Lỗi 0601-06; không tạo bản cập nhật",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db);
        var input=UT262728.UpdateInput(800000);input.Description="Mo ta cu";
        var message=await UT232425.Market(db).SubmitService(UT232425.NCC01,input);
        var count=await db.ServiceRevisions.CountAsync();
        observe($"Message={message}; Revisions={count}");
        Assert.Equal(0,count);Assert.Contains("0601-06",message);
    });
    [Fact(DisplayName="UT-26d | KT7 | RowVersion đã cũ")]
    public Task UT26d_Test()=>UT262728.Check("UT-26d","KT7","RowVersion đã cũ","Version khác dữ liệu thực","Lỗi 0601-07; không cập nhật",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db);
        var input=UT262728.UpdateInput();input.Version=Convert.ToBase64String(new byte[]{8,7,6,5,4,3,2,1});
        var message=await UT232425.Market(db).SubmitService(UT232425.NCC01,input);
        var count=await db.ServiceRevisions.CountAsync();
        observe($"Message={message}; Revisions={count}");
        Assert.Empty(await db.ServiceRevisions.ToListAsync());Assert.Contains("0601-07",message);
    });
    [Fact(DisplayName="UT-26e | XL1-XL3 | Tạo bản cập nhật v4, giữ nội dung công bố")]
    public Task UT26e_Test()=>UT262728.Check("UT-26e","XL1-XL3","Tạo bản cập nhật v4, giữ nội dung công bố","PhienBanCongBo=2, MAX=3, giá mới 950000","Bản v4 Chờ duyệt; nội dung công bố giữ giá 800000",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db);
        await UT262728.AddRevision(db,3,"Từ chối",900000);
        var before=(await db.Services.AsNoTracking().SingleAsync(x=>x.Id==UT232425.DV01)).UpdatedAt;
        var msg=await UT232425.Market(db).SubmitService(UT232425.NCC01,UT262728.UpdateInput());
        var s=await db.Services.AsNoTracking().SingleAsync(x=>x.Id==UT232425.DV01);
        var r=await db.ServiceRevisions.AsNoTracking().SingleAsync(x=>x.RevisionNumber==4);
        observe($"Message={msg}; Revision={r.RevisionNumber}/{r.Status}; NewPrice={r.Price}; PublishedPrice={s.Price}; Updated={s.UpdatedAt:o}");
        Assert.Equal(4,r.RevisionNumber);Assert.Equal("Chờ duyệt",r.Status);Assert.Equal(950000m,r.Price);
        Assert.Equal(800000m,s.Price);Assert.Equal("Đã duyệt",s.ApprovalStatus);Assert.True(s.UpdatedAt>=before);
    });
    [Fact(DisplayName="UT-26f | XL3 | Bản chưa từng duyệt bị từ chối")]
    public Task UT26f_Test()=>UT262728.Check("UT-26f","XL3","Bản chưa từng duyệt bị từ chối","DV01 Từ chối, chưa có bản công bố","Dịch vụ cập nhật nội dung mới; trạng thái Chờ duyệt",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db,"Từ chối",null);
        var msg=await UT232425.Market(db).SubmitService(UT232425.NCC01,UT262728.UpdateInput());
        var s=await db.Services.AsNoTracking().SingleAsync(x=>x.Id==UT232425.DV01);
        var r=await db.ServiceRevisions.AsNoTracking().SingleAsync();
        observe($"Message={msg}; Status={s.ApprovalStatus}; Price={s.Price}; Revision={r.RevisionNumber}/{r.Status}");
        Assert.Equal(950000m,s.Price);Assert.Equal("Chờ duyệt",s.ApprovalStatus);Assert.Equal("Chờ duyệt",r.Status);
    });
    [Fact(DisplayName="UT-26g | KT3 | Được cập nhật dịch vụ ngừng công bố")]
    public Task UT26g_Test()=>UT262728.Check("UT-26g","KT3","Được cập nhật dịch vụ ngừng công bố","DV01 Ngừng công bố; NCC được xác minh lại","Cho phép tạo bản cập nhật Chờ duyệt",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db,"Ngừng công bố",2);
        var msg=await UT232425.Market(db).SubmitService(UT232425.NCC01,UT262728.UpdateInput());
        var revisions=await db.ServiceRevisions.AsNoTracking().ToListAsync();
        observe($"Message={msg}; Revisions={revisions.Count}; Status={revisions.FirstOrDefault()?.Status}");
        Assert.StartsWith("Đã gửi",msg);Assert.Single(revisions);Assert.Equal("Chờ duyệt",revisions[0].Status);
    });
}
