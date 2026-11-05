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

// Shared fixtures and JSONL output for UT14–UT16. Keep this file when running UT15/16.
internal static class UT141516
{
    internal const int KH01=201, KH02=202, KH03=203, NC01=991;
    internal static AppDbContext Db()
    {
        var options=new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("UT141516_"+Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w=>w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options,new HttpContextAccessor());
    }
    internal static async Task Seed(AppDbContext db,bool senderActive=true,bool ownerActive=true)
    {
        db.Accounts.AddRange(
            new Account{Id=KH01,Name="KH01",Email="ut14-kh01@example.test",Role="Traveler",IsActive=senderActive},
            new Account{Id=KH02,Name="KH02",Email="ut14-kh02@example.test",Role="Traveler",IsActive=ownerActive},
            new Account{Id=KH03,Name="KH03",Email="ut14-kh03@example.test",Role="Traveler",IsActive=true});
        await db.SaveChangesAsync();
    }
    internal static Companion Demand(int id=NC01,bool published=true)
    {
        var start=DateTime.Today.AddDays(30);
        return new Companion{Id=id,OwnerId=KH02,Name="KH02",Destination="Đà Lạt",Description="Tìm bạn đồng hành",
            Date=start,EndDate=start.AddDays(4),Budget=2000000m,People=2,TravelStyle="Tự túc",
            SharingScope="Public",IsPublished=published,Version=new byte[]{1,2,3,4,5,6,7,8}};
    }
    internal static Connection Connection(int id=998,string status="Chờ phản hồi",int sender=KH01,string message="Xin đồng hành")
       => new Connection{Id=id,CompanionId=NC01,SenderId=sender,Message=message,
           Status=status,Version=new byte[]{1,2,3,4,5,6,7,8}};
    internal static Task Check(string id,string step,string title,string input,string expected,Func<Action<string>,Task> action)
        => Report(id,step,title,input,expected,action);
    private static async Task Report(string id,string step,string title,string input,string expected,Func<Action<string>,Task> action)
    {
        var actual="CHUA CO DU LIEU THUC TE";var problem="";
        try { await action(s=>actual=s); }
        catch(Exception ex) { problem=ex.Message; throw; }
        finally
        {
            var folder=Path.Combine(AppContext.BaseDirectory,"TestResults");Directory.CreateDirectory(folder);
            var file=Path.Combine(folder,"UnitTest_Details.jsonl");
            var json=JsonSerializer.Serialize(new{id,step,title,input,expected,actual,problem});
            for(var attempt=0;attempt<100;attempt++)
            {
                try
                {
                    using var stream=new FileStream(file,FileMode.Append,FileAccess.Write,FileShare.Read);
                    using var writer=new StreamWriter(stream);
                    await writer.WriteLineAsync(json);
                    break;
                }
                catch(IOException) when(attempt<99) { await Task.Delay(20); }
            }
        }
    }
}

public sealed class UT14_GuiDeNghi_Tests
{
    [Fact(DisplayName="UT-14a | KT1 | DungCongBo truoc khi gui => 0401-01; khong thong bao")]
    public Task UT14a_Unpublished()=>UT141516.Check("UT-14a","KT1","Kiem tra lai nhu cau truoc khi gui",
        "NC01.IsPublished=false; KH01 gui de nghi", "Loi 0401-01; khong tao de nghi; khong Tao thong bao",async record=>
    {
        await using var db=UT141516.Db();await UT141516.Seed(db);
        db.Companions.Add(UT141516.Demand(published:false));await db.SaveChangesAsync();
        var before=await db.Notifications.CountAsync();
        var r=await new CommunityManager(db).Connect(UT141516.KH01,UT141516.NC01,"Xin đồng hành");
        var connections=await db.Connections.CountAsync();var notifications=await db.Notifications.CountAsync();
        record($"Success={r.Success}; Message={r.Message}; StatusCode={r.StatusCode}; Connections={connections}; Notifications={before}->{notifications}; ErrorCode=KHONG CO");
        Assert.False(r.Success);Assert.Equal(0,connections);Assert.Equal(before,notifications);
        Assert.Contains("0401-01",r.Message);
    });

    [Fact(DisplayName="UT-14b | XL1/XL2 | Gui loi nhan 1000 ky tu; thong bao dung noi dung")]
    public Task UT14b_ValidMessage()=>UT141516.Check("UT-14b","XL1, XL2","Gui de nghi hop le va thong bao",
        "KH01 gui NC01; loiNhan dung 1000 ky tu", "De nghi ChoPhanHoi, MaNguoiGui=KH01; 1 thong bao DeNghiKetNoiMoi cho KH02; URL /dong-hanh/de-nghi/xem-de-nghi/{ma}",async record=>
    {
        await using var db=UT141516.Db();await UT141516.Seed(db);
        db.Companions.Add(UT141516.Demand());await db.SaveChangesAsync();
        var message=new string('A',1000);
        var r=await new CommunityManager(db).Connect(UT141516.KH01,UT141516.NC01,message);
        var cs=await db.Connections.AsNoTracking().ToListAsync();
        var ns=await db.Notifications.AsNoTracking().Where(n=>n.OwnerId==UT141516.KH02).ToListAsync();
        var c=cs.SingleOrDefault();var n=ns.SingleOrDefault();
        record($"Success={r.Success}; Id={r.Id}; Connections={cs.Count}; Status={c?.Status}; SenderId={c?.SenderId}; MessageLength={c?.Message.Length}; NotifyCount={ns.Count}; NotifyType={n?.Type}; NotifyURL={n?.Url}");
        Assert.True(r.Success);Assert.Single(cs);Assert.NotNull(c);
        Assert.Equal(UT141516.KH01,c.SenderId);Assert.Equal("Chờ phản hồi",c.Status);
        Assert.Equal(message,c.Message);Assert.Equal(c.Id,r.Id);
        Assert.Single(ns);Assert.NotNull(n);Assert.Equal("DeNghiKetNoiMoi",n.Type);
        Assert.Equal($"/dong-hanh/de-nghi/xem-de-nghi/{c.Id}",n.Url);
    });

    [Fact(DisplayName="UT-14c | XL3 | SQL 2601 => 0401-03")]
    public Task UT14c_UniqueRace()=>UT141516.Check("UT-14c","XL3","Xu ly loi trung chi muc 2601",
        "SaveChanges nem SQL 2601 khi them de nghi trung", "Loi 0401-03",async record=>
    {
        await using var db=UT141516.Db();await UT141516.Seed(db);
        db.Companions.Add(UT141516.Demand());
        db.Connections.Add(UT141516.Connection());await db.SaveChangesAsync();
        var r=await new CommunityManager(db).Connect(UT141516.KH01,UT141516.NC01,"Gui trung");
        record($"Success={r.Success}; Message={r.Message}; StatusCode={r.StatusCode}; ErrorCode=KHONG CO; SQL2601=KHONG DUOC INJECT (EF InMemory)");
        Assert.Fail("UT-14c CHUA THUC THI DUNG DIEU KIEN: EF InMemory khong gia lap duoc DbUpdateException SQL 2601; can repository/mock hay seam cua DbContext va kiem tra ma 0401-03. Khong duoc xem ket qua chan trung so bo la PASS.");
    });
}
