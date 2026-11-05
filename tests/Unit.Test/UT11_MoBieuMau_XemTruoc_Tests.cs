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

/// <summary>Du lieu gia lap rieng cho UT-11..UT-13; khong phu thuoc UT truoc.</summary>
internal static class UT111213
{
    internal const int KH01=101, KH02=102, NCC01=103, NC01=901;
    internal static AppDbContext Db()
    {
        var options=new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("UT111213_"+Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w=>w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options,new HttpContextAccessor());
    }
    internal static async Task Seed(AppDbContext db,bool kh01Active=true,bool kh02Active=true)
    {
        db.Accounts.AddRange(
            new Account{Id=KH01,Name="KH01",Email="ut111213-kh01@example.test",Role="Traveler",IsActive=kh01Active},
            new Account{Id=KH02,Name="KH02",Email="ut111213-kh02@example.test",Role="Traveler",IsActive=kh02Active},
            new Account{Id=NCC01,Name="NCC01",Email="ut111213-ncc01@example.test",Role="Provider",IsActive=true});
        await db.SaveChangesAsync();
    }
    internal static DemandInput Input(string scope="Connected",DateTime? begin=null)
    {
        var date=begin??DateTime.Today.AddDays(30);
        return new DemandInput{Destination="Đà Lạt",Description="Tìm bạn đồng hành",Date=date,
            EndDate=date.AddDays(4),Budget=2000000m,People=2,TravelStyle="Tự túc",Wishes="Cùng trải nghiệm",SharingScope=scope};
    }
    internal static Companion Demand(int id=NC01,string scope="Public",bool published=true,int owner=KH02)
    {
        var d=Input(scope);
        return new Companion{Id=id,OwnerId=owner,Name="KH02",Destination=d.Destination,Description=d.Description,
            Date=d.Date,EndDate=d.EndDate,Budget=d.Budget,People=d.People,TravelStyle=d.TravelStyle,
            Wishes=d.Wishes??"",SharingScope=scope,IsPublished=published,Version=new byte[8]};
    }
    internal static Task Check(string id,string step,string title,string input,string expected,
        Func<Action<string>,Task> action) => Report(id,step,title,input,expected,action);
    private static async Task Report(string id,string step,string title,string input,string expected,
        Func<Action<string>,Task> action)
    {
        string actual="CHUA CO DU LIEU THUC TE",problem="";
        try{await action(s=>actual=s);}
        catch(Exception ex){problem=ex.Message;throw;}
        finally
        {
            var folder=Path.Combine(AppContext.BaseDirectory,"TestResults");
            Directory.CreateDirectory(folder);
            var file=Path.Combine(folder,"UnitTest_Details.jsonl");
            var line=JsonSerializer.Serialize(new{id,step,title,input,expected,actual,problem});
            for(int i=0;i<100;i++)
            {
                try
                {
                    using var stream=new FileStream(file,FileMode.Append,FileAccess.Write,FileShare.Read);
                    using var writer=new StreamWriter(stream);
                    await writer.WriteLineAsync(line);
                    break;
                }
                catch(IOException) when(i<99){await Task.Delay(20);}
            }
        }
    }
}

/// <summary>UT-11: MoBieuMauAsync, XemTruocAsync. Bang 4.190, 4.191.</summary>
public sealed class UT11_MoBieuMau_XemTruoc_Tests
{
    [Fact(DisplayName="UT-11a | XL1 | Bieu mau mac dinh KhiKetNoi")]
    public Task UT11a_DefaultSharingScope()=>UT111213.Check("UT-11a","XL1","Pham vi mac dinh KhiKetNoi",
        "Mo bieu mau tao nhu cau moi","phamVi=KhiKetNoi (Connected)",async observe=>
    {
        // Controller.Compose(null) khoi tao new DemandInput().
        var form=new DemandInput();
        observe($"SharingScope={form.SharingScope}");
        Assert.Equal("Connected",form.SharingScope);
    });

    [Fact(DisplayName="UT-11b | XL1 | Xem truoc KhiKetNoi an thong tin rieng")]
    public Task UT11b_ConnectedPreview()=>UT111213.Check("UT-11b","XL1","Xem truoc pham vi KhiKetNoi",
        "KH01 xem truoc nhu cau SharingScope=Connected",
        "Khach chua dang nhap: Khong hien thi; da dang nhap chua ket noi: Description,Budget,Wishes=null",async observe=>
    {
        await using var db=UT111213.Db();await UT111213.Seed(db);
        var preview=await new CommunityManager(db).Preview(UT111213.KH01,UT111213.Input("Connected"));
        var card=preview.RegisteredView;
        observe($"PublicView={(preview.PublicView==null?"NULL":"CO")}; RegisteredView Limited={card.Limited}, Description={card.Description??"NULL"}, Budget={card.Budget?.ToString()??"NULL"}, Wishes={card.Wishes??"NULL"}; ghi chu Khong hien thi chua co text o service");
        Assert.Null(preview.PublicView);
        Assert.True(card.Limited);Assert.Null(card.Description);Assert.Null(card.Budget);Assert.Null(card.Wishes);
        // Chuoi 'Khong hien thi' la cach hien thi View, khong nam trong Preview DTO.
        Assert.Fail("UT-11b: Chua kiem chung duoc nhan 'Khong hien thi' cua goc nhin chua dang nhap o Razor View.");
    });

    [Fact(DisplayName="UT-11c | XL1 | Xem truoc CongKhai")]
    public Task UT11c_PublicPreview()=>UT111213.Check("UT-11c","XL1","Xem truoc pham vi CongKhai",
        "KH01 xem truoc nhu cau SharingScope=Public","Goc nhin chua dang nhap duoc hien thi du nhu cau",async observe=>
    {
        await using var db=UT111213.Db();await UT111213.Seed(db);
        var preview=await new CommunityManager(db).Preview(UT111213.KH01,UT111213.Input("Public"));
        var card=preview.PublicView;
        observe($"PublicView={(card==null?"NULL":"CO")}; Limited={card?.Limited}; Description={card?.Description??"NULL"}; Budget={card?.Budget}");
        Assert.NotNull(card);Assert.False(card.Limited);Assert.Equal("Tìm bạn đồng hành",card.Description);
        Assert.Equal(2000000m,card.Budget);
    });

    [Fact(DisplayName="UT-11d | XL2 | Xem truoc khong SaveChanges")]
    public Task UT11d_PreviewReadOnly()=>UT111213.Check("UT-11d","XL2","Xem truoc khong ghi CSDL",
        "Gọi Preview voi nhu cau chua luu","So ban ghi va EntityState khong doi; khong SaveChanges",async observe=>
    {
        await using var db=UT111213.Db();await UT111213.Seed(db);
        var before=await db.Companions.CountAsync();
        var preview=await new CommunityManager(db).Preview(UT111213.KH01,UT111213.Input("Public"));
        var after=await db.Companions.CountAsync();
        var pending=db.ChangeTracker.Entries<Companion>().Count(x=>x.State!=EntityState.Unchanged&&x.State!=EntityState.Detached);
        observe($"Before={before}; After={after}; ModifiedOrAdded={pending}; PublicView={(preview.PublicView==null?"NULL":"CO")}");
        Assert.Equal(before,after);Assert.Equal(0,pending);
    });
}
