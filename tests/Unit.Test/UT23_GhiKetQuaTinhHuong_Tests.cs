using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TuTuc.Controllers;
using TuTuc.Data;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

// Ho tro du lieu gia va ghi bao cao chung UT-23..UT-25.
internal static class UT232425
{
    internal const int KH01=101, KHP01=401, TH01=601, NCC01=301, NCC02=302, NCC03=303, NCC04=304;
    internal const int DV01=801, DV02=802, DV03=803;
    internal static readonly byte[] VersionBytes = {1,2,3,4,5,6,7,8};
    internal static string Version => Convert.ToBase64String(VersionBytes);
    internal static AppDbContext Db()
    {
        var options=new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("UT232425_"+Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w=>w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
        return new AppDbContext(options,new HttpContextAccessor());
    }
    internal static MarketplaceManager Market(AppDbContext db)
        =>new(db,new EphemeralDataProtectionProvider());
    internal static async Task Seed(AppDbContext db)
    {
        db.Accounts.AddRange(
          new Account{Id=KH01,Name="KH01",Email="ut23-kh01@example.test",Role="Traveler",IsActive=true},
          new Account{Id=NCC01,Name="NCC01",Email="ut24-ncc01@example.test",Role="Provider",IsActive=true,VerificationStatus="Đã xác minh",ProviderStatus="Đang hoạt động"},
          new Account{Id=NCC02,Name="NCC02",Email="ut24-ncc02@example.test",Role="Provider",IsActive=true,VerificationStatus="Chờ xác minh",ProviderStatus=""},
          new Account{Id=NCC03,Name="NCC03",Email="ut24-ncc03@example.test",Role="Provider",IsActive=true,VerificationStatus="Đã xác minh",ProviderStatus="Tạm đình chỉ"},
          new Account{Id=NCC04,Name="NCC04",Email="ut24-ncc04@example.test",Role="Provider",IsActive=true,VerificationStatus="Đã xác minh",ProviderStatus="Đang hoạt động"});
        db.Trips.Add(new Trip{Id=KHP01,OwnerId=KH01,Name="Kế hoạch",Destination="Đà Lạt",Start=new DateTime(2026,12,5),End=new DateTime(2026,12,8),Version=VersionBytes.ToArray()});
        db.Services.AddRange(
          new TravelService{Id=DV01,OwnerId=NCC01,Name="Phòng đôi view đồi",Location="Đà Lạt",Category="Lưu trú",Price=800000,ApprovalStatus="Nháp",IsPublished=false,Version=VersionBytes.ToArray()},
          new TravelService{Id=DV02,OwnerId=NCC01,Name="Dịch vụ đã duyệt",Location="Đà Lạt",Category="Lưu trú",Price=900000,ApprovalStatus="Đã duyệt",IsPublished=true,Version=VersionBytes.ToArray()},
          new TravelService{Id=DV03,OwnerId=NCC04,Name="Dịch vụ NCC khác",Location="Huế",Category="Lưu trú",Price=700000,ApprovalStatus="Nháp",IsPublished=false,Version=VersionBytes.ToArray()});
        await db.SaveChangesAsync();
    }
    internal static async Task SeedIncident(AppDbContext db,string status="Chưa giải quyết",bool hasPlan=false)
    {
        db.Incidents.Add(new Incident{Id=TH01,TripId=KHP01,OwnerId=KH01,Category="Thời tiết",Description="Mưa lớn",Status=status,Plan=hasPlan?"Đổi tuyến đường":"",Version=VersionBytes.ToArray()});
        await db.SaveChangesAsync();
    }
    internal static IncidentResultInput ResultInput(string outcome,string? resolution="")
        =>new(){Id=TH01,Version=Version,Outcome=outcome,Resolution=resolution};
    internal static ServiceEditVm Input(string operation="Submit") => new()
    {
        Name="Phòng tiêu chuẩn", Description="Phòng lưu trú dành cho hai người",Category="Lưu trú",Location="Đà Lạt",
        Price=800000,Operation=operation
    };
    internal static ProviderController Provider(AppDbContext db,int me)
    {
        var controller=new ProviderController(db,Market(db));
        var context=new DefaultHttpContext();
        context.User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,me.ToString()),new Claim(ClaimTypes.Role,"Provider")},"unit-test"));
        controller.ControllerContext=new ControllerContext{HttpContext=context};
        controller.TempData=new TempDataDictionary(context,new MemoryTempDataProvider());
        return controller;
    }
    private sealed class MemoryTempDataProvider : ITempDataProvider
    {
        private Dictionary<string,object> values=new();
        public IDictionary<string,object> LoadTempData(HttpContext context)=>values;
        public void SaveTempData(HttpContext context,IDictionary<string,object> v)=>values=new Dictionary<string,object>(v);
    }
    internal static async Task Check(string id,string step,string title,string input,string expected,Func<Action<string>,Task> body)
    {
        string actual="Chưa có dữ liệu thực tế",problem="";
        try{await body(s=>actual=s);}
        catch(Exception ex){problem=ex.Message;throw;}
        finally
        {
            var directory=Path.Combine(AppContext.BaseDirectory,"TestResults");Directory.CreateDirectory(directory);
            var path=Path.Combine(directory,"UnitTest_Details.jsonl");
            var line=JsonSerializer.Serialize(new{id,step,title,input,expected,actual,problem});
            for(int attempt=0;attempt<100;attempt++)
            {
                try
                {
                    using var stream=new FileStream(path,FileMode.Append,FileAccess.Write,FileShare.Read);
                    using var writer=new StreamWriter(stream);
                    await writer.WriteLineAsync(line);break;
                }
                catch(IOException) when(attempt<99){await Task.Delay(20);}
            }
        }
    }
}

public sealed class UT23_GhiKetQuaTinhHuong_Tests
{
    [Fact(DisplayName="UT-23a | KT1 | Tinh huong da ket thuc")]
    public Task UT23a_Test()=>UT232425.Check("UT-23a","KT1","Tinh huong da ket thuc","TH01=DaGiaiQuyet","Lỗi 0501-04; không đổi dữ liệu",async observe=>
    {
        await using var db=UT232425.Db();await UT232425.Seed(db);await UT232425.SeedIncident(db,"Đã giải quyết");
        var r=await new JourneyManager(db).Result(UT232425.KH01,UT232425.ResultInput("Chưa giải quyết"));
        var item=await db.Incidents.AsNoTracking().SingleAsync(x=>x.Id==UT232425.TH01);
        observe($"Success={r.Success};Code={r.Code};Message={r.Message};Status={item.Status}");
        Assert.False(r.Success);Assert.Equal("0501-04",r.Code);Assert.Equal("Đã giải quyết",item.Status);
    });
    [Fact(DisplayName="UT-23b | KT2 | Da giai quyet thieu ket qua")]
    public Task UT23b_Test()=>UT232425.Check("UT-23b","KT2","Da giai quyet thieu ket qua","Outcome=DaGiaiQuyet;Resolution=rỗng","Lỗi 0501-06; không đổi trạng thái",async observe=>
    {
        await using var db=UT232425.Db();await UT232425.Seed(db);await UT232425.SeedIncident(db);
        var r=await new JourneyManager(db).Result(UT232425.KH01,UT232425.ResultInput("Đã giải quyết",""));
        var item=await db.Incidents.AsNoTracking().SingleAsync(x=>x.Id==UT232425.TH01);
        observe($"Success={r.Success};Code={r.Code};Message={r.Message};Status={item.Status}");
        Assert.False(r.Success);Assert.Equal("0501-06",r.Code);Assert.Equal("Chưa giải quyết",item.Status);
    });
    [Fact(DisplayName="UT-23c | KT2 | Giai quyet mot phan thieu ket qua")]
    public Task UT23c_Test()=>UT232425.Check("UT-23c","KT2","Giai quyet mot phan thieu ket qua","Outcome=GiaiQuyetMotPhan;Resolution=rỗng","Lỗi 0501-06",async observe=>
    {
        await using var db=UT232425.Db();await UT232425.Seed(db);await UT232425.SeedIncident(db);
        var r=await new JourneyManager(db).Result(UT232425.KH01,UT232425.ResultInput("Giải quyết một phần",""));
        observe($"Success={r.Success};Code={r.Code};Message={r.Message}");
        Assert.False(r.Success);Assert.Equal("0501-06",r.Code);
    });
    [Fact(DisplayName="UT-23d | XL1,XL2 | Luu ket qua da giai quyet")]
    public Task UT23d_Test()=>UT232425.Check("UT-23d","XL1,XL2","Luu ket qua da giai quyet","Resolution=Đã đổi sang homestay khác","DaGiaiQuyet;KetQuaXuLy lưu đúng nội dung",async observe=>
    {
        await using var db=UT232425.Db();await UT232425.Seed(db);await UT232425.SeedIncident(db);
        var content="Đã đổi sang homestay khác";
        var r=await new JourneyManager(db).Result(UT232425.KH01,UT232425.ResultInput("Đã giải quyết",content));
        var item=await db.Incidents.AsNoTracking().SingleAsync(x=>x.Id==UT232425.TH01);
        observe($"Success={r.Success};Status={item.Status};Resolution={item.Resolution};Message={r.Message}");
        Assert.True(r.Success);Assert.Equal("Đã giải quyết",item.Status);Assert.Equal(content,item.Resolution);
    });
    [Fact(DisplayName="UT-23e | XL1 | Chua giai quyet khi da co phuong an")]
    public Task UT23e_Test()=>UT232425.Check("UT-23e","XL1","Chua giai quyet khi da co phuong an","TH01=DaChonPhuongAn;Outcome=ChuaGiaiQuyet","Giữ DaChonPhuongAn",async observe=>
    {
        await using var db=UT232425.Db();await UT232425.Seed(db);await UT232425.SeedIncident(db,"Đã chọn phương án",true);
        var r=await new JourneyManager(db).Result(UT232425.KH01,UT232425.ResultInput("Chưa giải quyết"));
        var item=await db.Incidents.AsNoTracking().SingleAsync(x=>x.Id==UT232425.TH01);
        observe($"Success={r.Success};Status={item.Status};Plan={item.Plan}");
        Assert.True(r.Success);Assert.Equal("Đã chọn phương án",item.Status);
    });
    [Fact(DisplayName="UT-23f | XL1 | Chua giai quyet khi chua co phuong an")]
    public Task UT23f_Test()=>UT232425.Check("UT-23f","XL1","Chua giai quyet khi chua co phuong an","TH01=ChuaGiaiQuyet;Plan rỗng","Giữ ChuaGiaiQuyet",async observe=>
    {
        await using var db=UT232425.Db();await UT232425.Seed(db);await UT232425.SeedIncident(db);
        var r=await new JourneyManager(db).Result(UT232425.KH01,UT232425.ResultInput("Chưa giải quyết"));
        var item=await db.Incidents.AsNoTracking().SingleAsync(x=>x.Id==UT232425.TH01);
        observe($"Success={r.Success};Status={item.Status};Plan={item.Plan}");
        Assert.True(r.Success);Assert.Equal("Chưa giải quyết",item.Status);
    });
    [Fact(DisplayName="UT-23g | XL1 | Dung xu ly khong can ket qua")]
    public Task UT23g_Test()=>UT232425.Check("UT-23g","XL1","Dung xu ly khong can ket qua","Outcome=DungXuLy;Resolution rỗng","DungXuLy hợp lệ",async observe=>
    {
        await using var db=UT232425.Db();await UT232425.Seed(db);await UT232425.SeedIncident(db);
        var r=await new JourneyManager(db).Result(UT232425.KH01,UT232425.ResultInput("Dừng xử lý",""));
        var item=await db.Incidents.AsNoTracking().SingleAsync(x=>x.Id==UT232425.TH01);
        observe($"Success={r.Success};Status={item.Status};Resolution={item.Resolution}");
        Assert.True(r.Success);Assert.Equal("Dừng xử lý",item.Status);
    });
}
